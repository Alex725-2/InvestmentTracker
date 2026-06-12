using InvestmentTracker.Server.Data;
using InvestmentTracker.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Server.Services
{
    public class QuoteUpdateService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QuoteUpdateService> _logger;

        public QuoteUpdateService(IServiceScopeFactory scopeFactory, ILogger<QuoteUpdateService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task UpdateAllQuotesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var moexService = scope.ServiceProvider.GetRequiredService<MoexService>();

            var securities = await context.Securities.ToListAsync();
            _logger.LogInformation("Found {Count} securities to update", securities.Count);

            foreach (var security in securities)
            {
                try
                {
                    _logger.LogInformation("Fetching price for {Ticker}", security.Ticker);
                    var price = await moexService.GetCurrentPriceAsync(security.Ticker);
                    if (price.HasValue)
                    {
                        _logger.LogInformation("Price for {Ticker}: {Price}", security.Ticker, price);
                        var quote = new Quote
                        {
                            SecurityId = security.Id,
                            Date = DateTime.UtcNow,
                            Price = price.Value,
                            Source = "MOEX_ISS"
                        };
                        context.Quotes.Add(quote);
                    }
                    else
                    {
                        _logger.LogWarning("No price data for {Ticker}", security.Ticker);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update quote for {Ticker}", security.Ticker);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}