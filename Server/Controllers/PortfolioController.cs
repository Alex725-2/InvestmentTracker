using InvestmentTracker.Server.Data;
using InvestmentTracker.Server.Services;
using InvestmentTracker.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InvestmentTracker.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PortfolioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PortfolioController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<ActionResult<List<PortfolioItemDto>>> GetMyPortfolio()
        {
            var userId = GetUserId();
            var items = await _context.PortfolioItems
                .Where(p => p.UserId == userId)
                .Include(p => p.Security)
                .Include(p => p.Account)
                .Select(p => new PortfolioItemDto
                {
                    Id = p.Id,
                    SecurityId = p.SecurityId,
                    //SecurityTicker = p.Security.Ticker,
                    AccountId = p.AccountId,
                    //AccountNumber = p.Account.AccountNumber,
                    SecurityTicker = p.Security != null ? p.Security.Ticker : "N/A",
                    AccountNumber = p.Account != null ? p.Account.AccountNumber : "N/A",
                    Quantity = p.Quantity,
                    AveragePurchasePrice = p.AveragePurchasePrice,

                    // Котировки из таблицы Quotes, по сути из MOEX
                    CurrentPrice = _context.Quotes
                        .Where(q => q.SecurityId == p.SecurityId)
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Price)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("top5")]
        public async Task<ActionResult<List<TopPositionDto>>> GetTop5()
        {
            var userId = GetUserId();

            // Получаем позиции с текущей стоимостью
            var positions = await _context.PortfolioItems
                .Where(p => p.UserId == userId)
                .Include(p => p.Security)
                .Select(p => new
                {
                    p.Security.Ticker,
                    p.Quantity,
                    MarketValue = p.Quantity * (_context.Quotes
                        .Where(q => q.SecurityId == p.SecurityId)
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Price)
                        .FirstOrDefault() ?? 0m)
                })
                .OrderByDescending(p => p.MarketValue) // <-- убрали ?? 0m, так как MarketValue уже decimal
                .Take(5)
                .ToListAsync();

            var moexService = HttpContext.RequestServices.GetRequiredService<MoexService>();
            var result = new List<TopPositionDto>();

            foreach (var pos in positions)
            {
                decimal currentPrice = pos.Quantity > 0 ? pos.MarketValue / pos.Quantity : 0;
                decimal? changePct = await moexService.GetLastChangePercentAsync(pos.Ticker);

                result.Add(new TopPositionDto
                {
                    Ticker = pos.Ticker,
                    CurrentPrice = currentPrice,
                    ChangePercent = changePct,
                    TotalValue = pos.MarketValue
                });
            }

            return Ok(result);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary()
        {
            var userId = GetUserId();

            // Все позиции пользователя с текущей стоимостью
            var positions = await _context.PortfolioItems
                .Where(p => p.UserId == userId)
                .Include(p => p.Security)
                .ThenInclude(s => s.AssetType)
                .Select(p => new
                {
                    p.Security.Ticker,
                    p.Security.AssetType.Name,
                    p.Quantity,
                    p.AveragePurchasePrice,
                    CurrentPrice = _context.Quotes
                        .Where(q => q.SecurityId == p.SecurityId)
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Price)
                        .FirstOrDefault() ?? 0m
                })
                .ToListAsync();

            var totalMarketValue = positions.Sum(p => p.Quantity * p.CurrentPrice);
            var totalCost = positions.Sum(p => p.Quantity * p.AveragePurchasePrice);
            var totalPnL = totalMarketValue - totalCost;

            // Today PnL: для простоты возьмём изменение последней цены к вчерашнему закрытию (если есть)
            // Пока поставим 0, потом можно доработать.
            decimal todayPnL = 0;

            // Распределение по типам активов
            var allocation = positions
                .GroupBy(p => p.Name)
                .Select(g => new
                {
                    AssetTypeName = g.Key,
                    TotalValue = g.Sum(p => p.Quantity * p.CurrentPrice)
                })
                .Select(a => new AssetTypeAllocationDto
                {
                    AssetTypeName = a.AssetTypeName,
                    TotalValue = a.TotalValue,
                    Percentage = totalMarketValue > 0 ? a.TotalValue / totalMarketValue * 100 : 0
                })
                .OrderByDescending(a => a.TotalValue)
                .ToList();

            return Ok(new DashboardSummaryDto
            {
                TotalMarketValue = totalMarketValue,
                TotalCost = totalCost,
                TotalPnL = totalPnL,
                TodayPnL = todayPnL,
                Allocation = allocation
            });
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<HistoryPointDto>>> GetHistory()
        {
            // Пока возвращаем данные за последние 7 дней на основе Quotes (упрощённо)
            var userId = GetUserId();
            var fromDate = DateTime.UtcNow.AddDays(-7);
            var history = await _context.Quotes
                .Where(q => q.Date >= fromDate)
                .Join(_context.PortfolioItems.Where(p => p.UserId == userId),
                      q => q.SecurityId,
                      p => p.SecurityId,
                      (q, p) => new { q.Date, Value = p.Quantity * q.Price })
                .GroupBy(x => x.Date.Date)
                .Select(g => new HistoryPointDto { Date = g.Key, TotalValue = g.Sum(x => x.Value) })
                .OrderBy(x => x.Date)
                .ToListAsync();
            return Ok(history);
        }
    }
}