using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvestmentTracker.Server.Data;
using InvestmentTracker.Server.Models;
using InvestmentTracker.Shared.Models;

namespace InvestmentTracker.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        [HttpGet]
        public async Task<ActionResult<List<AccountDto>>> GetMyAccounts()
        {
            var userId = GetUserId();
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Include(a => a.Broker)
                .Include(a => a.Currency)
                .Select(a => new AccountDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    BrokerId = a.BrokerId,
                    BrokerName = a.Broker.Name,
                    AccountNumber = a.AccountNumber,
                    Comment = a.Comment,
                    CommissionRate = a.CommissionRate,
                    CurrencyId = a.CurrencyId,
                    CurrencyCode = a.Currency.Code
                })
                .ToListAsync();

            return Ok(accounts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AccountDto>> GetById(int id)
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .Include(a => a.Broker)
                .Include(a => a.Currency)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null) return NotFound();

            return Ok(new AccountDto
            {
                Id = account.Id,
                UserId = account.UserId,
                BrokerId = account.BrokerId,
                BrokerName = account.Broker.Name,
                AccountNumber = account.AccountNumber,
                Comment = account.Comment,
                CommissionRate = account.CommissionRate,
                CurrencyId = account.CurrencyId,
                CurrencyCode = account.Currency.Code
            });
        }

        [HttpPost]
        public async Task<ActionResult<AccountDto>> Create(AccountDto dto)
        {
            var userId = GetUserId();
            var account = new Account
            {
                UserId = userId,
                BrokerId = dto.BrokerId,
                AccountNumber = dto.AccountNumber,
                Comment = dto.Comment,
                CommissionRate = dto.CommissionRate,
                CurrencyId = dto.CurrencyId
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            dto.Id = account.Id;
            dto.UserId = userId;

            return CreatedAtAction(nameof(GetById), new { id = account.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, AccountDto dto)
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null) return NotFound();

            account.BrokerId = dto.BrokerId;
            account.AccountNumber = dto.AccountNumber;
            account.Comment = dto.Comment;
            account.CommissionRate = dto.CommissionRate;
            account.CurrencyId = dto.CurrencyId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null) return NotFound();

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}