using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InvestmentTracker.Server.Services;
using InvestmentTracker.Shared.Models;

namespace InvestmentTracker.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class MarketController : ControllerBase
    {
        private readonly MoexService _moex;

        public MarketController(MoexService moex)
        {
            _moex = moex;
        }

        [HttpGet("snapshot")]
        public async Task<ActionResult<MarketSnapshotDto>> GetSnapshot()
        {
            var index = await _moex.GetIndexInfoAsync("IMOEX");
            var status = await _moex.GetMarketStatusAsync();

            // Популярные ликвидные акции для отбора движений
            var tickers = new[] { "SBER", "GAZP", "LKOH", "VTBR", "GMKN", "TATN", "ROSN", "NVTK", "ALRS", "CHMF" };
            var movers = new List<MoverDto>();
            foreach (var t in tickers)
            {
                var info = await _moex.GetStockMoverAsync(t);
                if (info != null) movers.Add(info);
            }

            var gainers = movers.Where(m => m.ChangePct > 0).OrderByDescending(m => m.ChangePct).Take(3).ToList();
            var losers = movers.Where(m => m.ChangePct < 0).OrderBy(m => m.ChangePct).Take(3).ToList();

            return Ok(new MarketSnapshotDto
            {
                IndexValue = index?.Value,
                IndexChangePct = index?.ChangePct,
                TradingStatus = status?.Status ?? "—",
                SessionCloseTime = status?.CloseTime ?? "—",
                TopGainers = gainers,
                TopLosers = losers
            });
        }
    }
}