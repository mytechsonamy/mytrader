using Microsoft.AspNetCore.SignalR;
using MyTrader.Core.Interfaces;
using MyTrader.Core.DTOs.Portfolio;
using MyTrader.API.Hubs;

namespace MyTrader.API.Services;

public class PortfolioMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<PortfolioHub> _portfolioHubContext;
    private readonly ILogger<PortfolioMonitoringService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30); // Update every 30 seconds

    public PortfolioMonitoringService(
        IServiceProvider serviceProvider,
        IHubContext<PortfolioHub> portfolioHubContext,
        ILogger<PortfolioMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _portfolioHubContext = portfolioHubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Portfolio Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorPortfolios();
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring portfolios");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Portfolio Monitoring Service stopped");
    }

    private async Task MonitorPortfolios()
    {
        using var scope = _serviceProvider.CreateScope();
        var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();

        try
        {
            // Get all portfolios (simplified - in real implementation you'd track active connections)
            var sampleUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
            var portfolios = await portfolioService.GetUserPortfoliosAsync(sampleUserId);

            foreach (var portfolio in portfolios)
            {
                // Simulate real-time P&L updates
                var randomPnL = GenerateRandomPnLUpdate(portfolio.TotalPnL);
                
                await _portfolioHubContext.NotifyPnLUpdate(
                    sampleUserId.ToString(),
                    randomPnL.totalPnL,
                    randomPnL.dailyPnL,
                    randomPnL.returnPercent);

                // Simulate position updates for each position
                foreach (var position in portfolio.Positions)
                {
                    var updatedPosition = new PortfolioPositionDto
                    {
                        Id = position.Id,
                        Symbol = position.Symbol,
                        SymbolName = position.SymbolName,
                        Quantity = position.Quantity,
                        AveragePrice = position.AveragePrice,
                        CurrentPrice = GenerateRandomPrice(position.CurrentPrice),
                        MarketValue = position.MarketValue,
                        UnrealizedPnL = GenerateRandomPnL(position.UnrealizedPnL),
                        UnrealizedPnLPercent = position.UnrealizedPnLPercent,
                        CostBasis = position.CostBasis,
                        Weight = position.Weight,
                        LastTradedAt = DateTime.UtcNow
                    };

                    await _portfolioHubContext.NotifyPositionUpdate(
                        sampleUserId.ToString(),
                        updatedPosition);

                    // Also send market data updates
                    var priceChange = updatedPosition.CurrentPrice - position.CurrentPrice;
                    var priceChangePercent = position.CurrentPrice > 0 
                        ? (priceChange / position.CurrentPrice) * 100 
                        : 0;

                    await _portfolioHubContext.NotifyMarketDataUpdate(
                        sampleUserId.ToString(),
                        position.Symbol,
                        updatedPosition.CurrentPrice,
                        priceChange,
                        priceChangePercent);
                }

                // Update portfolio summary
                var updatedPortfolio = new PortfolioSummaryDto
                {
                    Id = portfolio.Id,
                    Name = portfolio.Name,
                    BaseCurrency = portfolio.BaseCurrency,
                    InitialCapital = portfolio.InitialCapital,
                    CurrentValue = portfolio.CurrentValue + randomPnL.dailyPnL,
                    CashBalance = portfolio.CashBalance,
                    TotalPnL = randomPnL.totalPnL,
                    DailyPnL = randomPnL.dailyPnL,
                    TotalReturnPercent = randomPnL.returnPercent,
                    LastUpdated = DateTime.UtcNow,
                    Positions = portfolio.Positions
                };

                await _portfolioHubContext.NotifyPortfolioUpdate(
                    sampleUserId.ToString(),
                    updatedPortfolio);
            }

            _logger.LogDebug("Portfolio monitoring update completed for {PortfolioCount} portfolios", portfolios.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during portfolio monitoring cycle");
        }
    }

    private (decimal totalPnL, decimal dailyPnL, decimal returnPercent) GenerateRandomPnLUpdate(decimal currentPnL)
    {
        var random = new Random();
        var dailyChange = (decimal)(random.NextDouble() * 200 - 100); // -100 to +100
        var newTotalPnL = currentPnL + dailyChange;
        var returnPercent = newTotalPnL / 10000 * 100; // Assuming 10k initial capital
        
        return (newTotalPnL, dailyChange, returnPercent);
    }

    private decimal GenerateRandomPrice(decimal currentPrice)
    {
        var random = new Random();
        var changePercent = (decimal)(random.NextDouble() * 0.04 - 0.02); // -2% to +2%
        return currentPrice * (1 + changePercent);
    }

    private decimal GenerateRandomPnL(decimal currentPnL)
    {
        var random = new Random();
        var changePercent = (decimal)(random.NextDouble() * 0.1 - 0.05); // -5% to +5%
        return currentPnL * (1 + changePercent);
    }
}