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
    public class AssetTypesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssetTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<AssetTypeDto>>> GetAll()
        {
            var types = await _context.AssetTypes
                .Select(t => new AssetTypeDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();

            return Ok(types);
        }
    }
}