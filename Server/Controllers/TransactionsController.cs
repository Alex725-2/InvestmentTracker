using InvestmentTracker.Server.Data;
using InvestmentTracker.Server.Models;
using InvestmentTracker.Server.Services;
using InvestmentTracker.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InvestmentTracker.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly MoexService _moexService;

        public TransactionsController(ApplicationDbContext context, MoexService moexService)
        {
            _context = context;
            _moexService = moexService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<ActionResult<List<TransactionDto>>> GetAll()
        {
            var userId = GetUserId();
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Security)
                .Include(t => t.Account)
                .OrderByDescending(t => t.Date)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    SecurityId = t.SecurityId,
                    SecurityTicker = t.Security.Ticker,
                    AccountId = t.AccountId,
                    AccountNumber = t.Account.AccountNumber,
                    Date = t.Date,
                    Type = t.Type,
                    Quantity = t.Quantity,
                    Price = t.Price,
                    Commission = t.Commission
                })
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>> GetById(int id)
        {
            var userId = GetUserId();
            var t = await _context.Transactions
                .Include(t => t.Security)
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (t == null) return NotFound();

            return Ok(new TransactionDto
            {
                Id = t.Id,
                SecurityId = t.SecurityId,
                SecurityTicker = t.Security.Ticker,
                AccountId = t.AccountId,
                AccountNumber = t.Account.AccountNumber,
                Date = t.Date,
                Type = t.Type,
                Quantity = t.Quantity,
                Price = t.Price,
                Commission = t.Commission
            });
        }

        [HttpPost]
        public async Task<ActionResult<TransactionDto>> Create(TransactionDto dto)
        {
            var userId = GetUserId();

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            if (account == null)
                return BadRequest("Account not found or not yours.");

            var security = await _context.Securities.FindAsync(dto.SecurityId);
            if (security == null)
                return BadRequest("Security not found.");

            if (dto.Type == TransactionType.Sell)
            {
                var position = await _context.PortfolioItems
                    .FirstOrDefaultAsync(p => p.UserId == userId
                        && p.SecurityId == dto.SecurityId
                        && p.AccountId == dto.AccountId);

                if (position == null || position.Quantity < dto.Quantity)
                    return BadRequest("Insufficient quantity for sale.");
            }

            var transactionDate = DateTime.ParseExact(dto.DateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
            var transaction = new Transaction
            {
                UserId = userId,
                SecurityId = dto.SecurityId,
                AccountId = dto.AccountId,
                Date = transactionDate,
                Type = dto.Type,
                Quantity = dto.Quantity,
                Price = Math.Round(dto.Price, 2),
                Commission = Math.Round(dto.Commission, 2)
            };

            _context.Transactions.Add(transaction);

            var portfolioItem = await _context.PortfolioItems
                .FirstOrDefaultAsync(p => p.UserId == userId
                    && p.SecurityId == dto.SecurityId
                    && p.AccountId == dto.AccountId);

            if (dto.Type == TransactionType.Buy)
            {
                if (portfolioItem == null)
                {
                    portfolioItem = new PortfolioItem
                    {
                        UserId = userId,
                        SecurityId = dto.SecurityId,
                        AccountId = dto.AccountId,
                        Quantity = dto.Quantity,
                        AveragePurchasePrice = dto.Price
                    };
                    _context.PortfolioItems.Add(portfolioItem);
                }
                else
                {
                    var totalCost = portfolioItem.Quantity * portfolioItem.AveragePurchasePrice
                                    + dto.Quantity * dto.Price;
                    portfolioItem.Quantity += dto.Quantity;
                    portfolioItem.AveragePurchasePrice = totalCost / portfolioItem.Quantity;
                }
            }
            else
            {
                portfolioItem!.Quantity -= dto.Quantity;
            }

            var currentPrice = await _moexService.GetCurrentPriceAsync(security.Ticker);
            if (currentPrice.HasValue)
            {
                var quote = new Quote
                {
                    SecurityId = security.Id,
                    Date = DateTime.UtcNow,
                    Price = currentPrice.Value,
                    Source = "MOEX_ISS"
                };
                _context.Quotes.Add(quote);
            }

            await _context.SaveChangesAsync();

            dto.Id = transaction.Id;
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null) return NotFound();

            var portfolioItem = await _context.PortfolioItems
                .FirstOrDefaultAsync(p => p.UserId == userId
                    && p.SecurityId == transaction.SecurityId
                    && p.AccountId == transaction.AccountId);

            if (portfolioItem == null)
                return BadRequest("Portfolio position not found.");

            if (transaction.Type == TransactionType.Buy)
            {
                if (portfolioItem.Quantity < transaction.Quantity)
                    return BadRequest("Cannot undo buy – insufficient quantity.");

                if (portfolioItem.Quantity == transaction.Quantity)
                {
                    _context.PortfolioItems.Remove(portfolioItem);
                }
                else
                {
                    var totalCost = portfolioItem.Quantity * portfolioItem.AveragePurchasePrice
                                    - transaction.Quantity * transaction.Price;
                    portfolioItem.Quantity -= transaction.Quantity;
                    portfolioItem.AveragePurchasePrice = totalCost / portfolioItem.Quantity;
                }
            }
            else
            {
                portfolioItem.Quantity += transaction.Quantity;
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}