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
    public class BrokersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BrokersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<BrokerDto>>> GetAll()
        {
            var brokers = await _context.Brokers
                .Select(b => new BrokerDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    SiteUrl = b.SiteUrl,
                    DefaultCommissionRate = b.DefaultCommissionRate,
                    IsApproved = b.IsApproved
                })
                .ToListAsync();

            return Ok(brokers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BrokerDto>> GetById(int id)
        {
            var broker = await _context.Brokers.FindAsync(id);
            if (broker == null) return NotFound();

            return Ok(new BrokerDto
            {
                Id = broker.Id,
                Name = broker.Name,
                SiteUrl = broker.SiteUrl,
                DefaultCommissionRate = broker.DefaultCommissionRate,
                IsApproved = broker.IsApproved
            });
        }

        [HttpPost]
        public async Task<ActionResult<BrokerDto>> Create(BrokerDto dto)
        {
            var broker = new Broker
            {
                Name = dto.Name,
                SiteUrl = dto.SiteUrl,
                DefaultCommissionRate = dto.DefaultCommissionRate,
                IsApproved = false
            };

            _context.Brokers.Add(broker);
            await _context.SaveChangesAsync();

            dto.Id = broker.Id;
            dto.IsApproved = broker.IsApproved;

            return CreatedAtAction(nameof(GetById), new { id = broker.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BrokerDto dto)
        {
            var broker = await _context.Brokers.FindAsync(id);
            if (broker == null) return NotFound();

            broker.Name = dto.Name;
            broker.SiteUrl = dto.SiteUrl;
            broker.DefaultCommissionRate = dto.DefaultCommissionRate;
            // IsApproved менять может только админ, но пока разрешим всем для простоты
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var broker = await _context.Brokers.FindAsync(id);
            if (broker == null) return NotFound();

            _context.Brokers.Remove(broker);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}