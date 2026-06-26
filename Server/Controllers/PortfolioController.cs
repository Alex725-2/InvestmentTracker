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
                    AssetTypeName = p.Security.AssetType.Name,
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
                .Select(p => new TopPositionDto
                {
                    Ticker = p.Security.Ticker,
                    CurrentPrice = _context.Quotes
                        .Where(q => q.SecurityId == p.SecurityId)
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Price)
                        .FirstOrDefault() ?? 0m,
                    ChangePercent = null,
                    TotalValue = p.Quantity * (_context.Quotes
                        .Where(q => q.SecurityId == p.SecurityId)
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Price)
                        .FirstOrDefault() ?? 0m)
                })
                .OrderByDescending(p => p.TotalValue)
                .Take(5)
                .ToListAsync();

            return Ok(positions);
        }

        // История портфеля
        [HttpGet("history")]
        public async Task<ActionResult<List<HistoryPointDto>>> GetHistory()
        {
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