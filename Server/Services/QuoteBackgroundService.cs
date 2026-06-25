using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestmentTracker.Server.Services
{
    public class QuoteBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QuoteBackgroundService> _logger;

        public QuoteBackgroundService(IServiceScopeFactory scopeFactory, ILogger<QuoteBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Quote background service starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var quoteService = scope.ServiceProvider.GetRequiredService<QuoteUpdateService>();
                    await quoteService.UpdateAllQuotesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background quote update.");
                }

                // Ждём 15 минут
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}