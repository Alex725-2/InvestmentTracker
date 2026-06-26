using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvestmentTracker.Server.Data;
using InvestmentTracker.Shared.Models;

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

        // Основной список позиций (страница портфолио) – временно AllowAnonymous для демо
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<PortfolioItemDto>>> Get()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                var demoUser = await _context.Users.FirstOrDefaultAsync();
                if (demoUser == null) return Ok(new List<PortfolioItemDto>());
                userId = demoUser.Id;
            }

            var items = await _context.PortfolioItems
                .Where(p => p.UserId == userId)
                .Select(p => new PortfolioItemDto
                {
                    Id = p.Id,
                    SecurityId = p.SecurityId,
                    SecurityTicker = p.Security.Ticker,
                    AccountId = p.AccountId,
                    AccountNumber = p.Account.AccountNumber,
                    Quantity = p.Quantity,
                    AveragePurchasePrice = p.AveragePurchasePrice,
                    CurrentPrice = _context.Quotes
                        .Where(q => q.SecurityId == p.SecurityId)
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Price)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(items);
        }

        // Сводка для дашборда
        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary()
        {
            var userId = GetUserId();

            var positions = await _context.PortfolioItems
                .Where(p => p.UserId == userId)
                .Include(p => p.Security)
                .ThenInclude(s => s.AssetType)
                .Select(p => new
                {
                    p.Security.Ticker,
                    AssetTypeName = p.Security.AssetType != null ? p.Security.AssetType.Name : "Unknown",
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
            decimal todayPnL = 0;

            var allocation = positions
                .GroupBy(p => p.AssetTypeName)
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

        // Топ-5 позиций
        [HttpGet("top5")]
        public async Task<ActionResult<List<TopPositionDto>>> GetTop5()
        {
            var userId = GetUserId();

            var positions = await _context.PortfolioItems
                .Where(p => p.UserId == userId)
                .Select(p => new
                {
                    Ticker = p.Security.Ticker,
                    Quantity = p.Quantity,
                    CurrentPrice = _context.Quotes
                        .Where(q => q.SecurityId == p.SecurityId)
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Price)
                        .FirstOrDefault() ?? 0m
                })
                .ToListAsync();

            // Получаем MoexService из DI
            var moexService = HttpContext.RequestServices.GetRequiredService<Services.MoexService>();

            var top5 = new List<TopPositionDto>();
            foreach (var pos in positions)
            {
                var change = await moexService.GetChangePercentSafeAsync(pos.Ticker);
                top5.Add(new TopPositionDto
                {
                    Ticker = pos.Ticker,
                    CurrentPrice = pos.CurrentPrice,
                    ChangePercent = change,
                    TotalValue = pos.Quantity * pos.CurrentPrice
                });
            }

            var result = top5
                .OrderByDescending(p => p.TotalValue)
                .Take(5)
                .ToList();

            return Ok(result);
        }

        // История портфеля
        [HttpGet("history")]
        public async Task<ActionResult<List<HistoryPointDto>>> GetHistory()
        {
            var userId = GetUserId();
            var fromDate = DateTime.UtcNow.AddDays(-7);

            // Загружаем все котировки, связанные с позициями пользователя
            var quotes = await _context.Quotes
                .Where(q => q.Date >= fromDate)
                .Where(q => _context.PortfolioItems
                    .Any(p => p.UserId == userId && p.SecurityId == q.SecurityId))
                .Select(q => new
                {
                    q.Date,
                    Value = q.Price * _context.PortfolioItems
                        .Where(p => p.UserId == userId && p.SecurityId == q.SecurityId)
                        .Select(p => p.Quantity)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Группируем в памяти
            var history = quotes
                .GroupBy(x => x.Date.Date)
                .Select(g => new HistoryPointDto
                {
                    Date = g.Key,
                    TotalValue = g.Sum(x => x.Value)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(history);
        }
    }
}