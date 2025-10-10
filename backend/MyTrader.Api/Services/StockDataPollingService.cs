using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyTrader.Api.Hubs;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Services.Market;
using System.Collections.Concurrent;

namespace MyTrader.Api.Services;

/// <summary>
/// Background service that polls Yahoo Finance for stock data and broadcasts via SignalR
/// Polls BIST, NASDAQ, and NYSE symbols every 1 minute
/// </summary>
public class StockDataPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MarketDataHub> _marketDataHubContext;
    private readonly IHubContext<DashboardHub> _dashboardHubContext;
    private readonly ILogger<StockDataPollingService> _logger;
    private readonly Dictionary<string, IMarketDataProvider> _providers;
    private readonly ConcurrentDictionary<string, DateTime> _lastBroadcastTimes;
    private readonly PeriodicTimer _pollingTimer;
    
    private const int PollingIntervalSeconds = 60; // 1 minute
    private long _totalPolls;
    private long _successfulPolls;
    private long _failedPolls;

    public StockDataPollingService(
        IServiceScopeFactory scopeFactory,
        IHubContext<MarketDataHub> marketDataHubContext,
        IHubContext<DashboardHub> dashboardHubContext,
        IHttpClientFactory httpClientFactory,
        ILogger<StockDataPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _marketDataHubContext = marketDataHubContext;
        _dashboardHubContext = dashboardHubContext;
        _logger = logger;
        _lastBroadcastTimes = new ConcurrentDictionary<string, DateTime>();
        _pollingTimer = new PeriodicTimer(TimeSpan.FromSeconds(PollingIntervalSeconds));

        // Initialize providers for each market
        _providers = new Dictionary<string, IMarketDataProvider>
        {
            ["BIST"] = new YahooFinanceProvider(
                logger as ILogger<YahooFinanceProvider> ?? 
                    new LoggerFactory().CreateLogger<YahooFinanceProvider>(),
                httpClientFactory,
                "BIST"),
            ["NASDAQ"] = new YahooFinanceProvider(
                logger as ILogger<YahooFinanceProvider> ?? 
                    new LoggerFactory().CreateLogger<YahooFinanceProvider>(),
                httpClientFactory,
                "NASDAQ"),
            ["NYSE"] = new YahooFinanceProvider(
                logger as ILogger<YahooFinanceProvider> ?? 
                    new LoggerFactory().CreateLogger<YahooFinanceProvider>(),
                httpClientFactory,
                "NYSE")
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StockDataPollingService starting - will poll every {Interval} seconds", 
            PollingIntervalSeconds);

        // Wait a bit before starting to allow other services to initialize
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        try
        {
            // ✅ FIX: Poll immediately on startup to get initial data quickly
            _logger.LogInformation("Performing initial stock data poll on startup");
            await PollAllMarketsAsync(stoppingToken);
            
            // Then continue with regular polling interval
            while (await _pollingTimer.WaitForNextTickAsync(stoppingToken))
            {
                await PollAllMarketsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("StockDataPollingService stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in StockDataPollingService");
            throw;
        }
    }

    private async Task PollAllMarketsAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _totalPolls);
        _logger.LogInformation("Starting stock data polling cycle {PollNumber}", _totalPolls);

        var pollTasks = new List<Task>();

        foreach (var (market, provider) in _providers)
        {
            pollTasks.Add(PollMarketAsync(market, provider, cancellationToken));
        }

        try
        {
            await Task.WhenAll(pollTasks);
            Interlocked.Increment(ref _successfulPolls);
            
            _logger.LogInformation(
                "Completed polling cycle {PollNumber} - Success: {Success}, Failed: {Failed}",
                _totalPolls, _successfulPolls, _failedPolls);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedPolls);
            _logger.LogError(ex, "Error in polling cycle {PollNumber}", _totalPolls);
        }
    }

    private async Task PollMarketAsync(string market, IMarketDataProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Polling {Market} market via {Provider}", market, provider.ProviderName);

            // Check if provider is available
            if (!await provider.IsAvailableAsync())
            {
                _logger.LogWarning("{Provider} is not available for {Market}", provider.ProviderName, market);
                return;
            }

            // Get tracked symbols for this market from database
            var symbols = await GetTrackedSymbolsForMarketAsync(market, cancellationToken);
            
            if (!symbols.Any())
            {
                _logger.LogDebug("No tracked symbols found for {Market}", market);
                return;
            }

            _logger.LogInformation("Fetching prices for {Count} symbols in {Market}", symbols.Count, market);

            // Fetch prices from provider
            var prices = await provider.GetPricesAsync(symbols, cancellationToken);

            if (!prices.Any())
            {
                _logger.LogWarning("No prices returned from {Provider} for {Market}", provider.ProviderName, market);
                return;
            }

            _logger.LogInformation("Received {Count} prices from {Provider} for {Market}", 
                prices.Count, provider.ProviderName, market);

            // Broadcast prices via SignalR
            await BroadcastPricesAsync(market, prices, cancellationToken);

            // Update market status
            var marketStatus = await provider.GetMarketStatusAsync(market);
            await BroadcastMarketStatusAsync(marketStatus, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling {Market} market", market);
        }
    }

    private async Task<List<string>> GetTrackedSymbolsForMarketAsync(string market, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyTrader.Infrastructure.Data.TradingDbContext>();

            // Get tracked symbols for this market
            // Note: Market.Code property maps to 'code' column in database
            var symbols = await dbContext.Symbols
                .Include(s => s.Market)
                .Where(s => s.Market.Code == market && s.IsTracked)
                .Select(s => s.Ticker)
                .ToListAsync(cancellationToken);

            return symbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tracked symbols for {Market}", market);
            return new List<string>();
        }
    }

    private async Task BroadcastPricesAsync(string market, List<UnifiedMarketDataDto> prices, CancellationToken cancellationToken)
    {
        try
        {
            var broadcastTasks = new List<Task>();

            foreach (var price in prices)
            {
                // Create price update message
                var priceUpdate = new
                {
                    type = "PriceUpdate",
                    assetClass = "STOCK",
                    market = market,
                    symbol = price.Ticker,
                    price = price.Price,
                    change = price.PriceChange,
                    changePercent = price.PriceChangePercent,
                    volume = price.Volume,
                    timestamp = price.DataTimestamp,
                    isRealTime = price.IsRealTime,
                    dataDelayMinutes = price.DataDelayMinutes,
                    marketStatus = price.MarketStatus,
                    isMarketOpen = price.IsMarketOpen,
                    source = price.DataProvider
                };

                // Broadcast to symbol-specific group
                var symbolGroup = $"STOCK_{price.Ticker}";
                broadcastTasks.Add(_marketDataHubContext.Clients.Group(symbolGroup)
                    .SendAsync("PriceUpdate", priceUpdate, cancellationToken));
                broadcastTasks.Add(_dashboardHubContext.Clients.Group(symbolGroup)
                    .SendAsync("PriceUpdate", priceUpdate, cancellationToken));

                // Broadcast to market-specific group
                var marketGroup = $"Market_{market}";
                broadcastTasks.Add(_marketDataHubContext.Clients.Group(marketGroup)
                    .SendAsync("PriceUpdate", priceUpdate, cancellationToken));
                broadcastTasks.Add(_dashboardHubContext.Clients.Group(marketGroup)
                    .SendAsync("PriceUpdate", priceUpdate, cancellationToken));

                // Legacy format for backward compatibility
                var legacyUpdate = new
                {
                    symbol = price.Ticker,
                    price = price.Price,
                    change = price.PriceChange,
                    volume = price.Volume,
                    timestamp = price.DataTimestamp,
                    assetClass = "STOCK"
                };

                broadcastTasks.Add(_marketDataHubContext.Clients.Group(symbolGroup)
                    .SendAsync("ReceivePriceUpdate", legacyUpdate, cancellationToken));
                broadcastTasks.Add(_dashboardHubContext.Clients.Group(symbolGroup)
                    .SendAsync("ReceivePriceUpdate", legacyUpdate, cancellationToken));

                // Update last broadcast time
                _lastBroadcastTimes.AddOrUpdate(price.Ticker, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
            }

            await Task.WhenAll(broadcastTasks);

            _logger.LogDebug("Broadcasted {Count} price updates for {Market}", prices.Count, market);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting prices for {Market}", market);
        }
    }

    private async Task BroadcastMarketStatusAsync(MarketStatusDto marketStatus, CancellationToken cancellationToken)
    {
        try
        {
            var statusUpdate = new
            {
                type = "MarketStatus",
                market = marketStatus.Code,
                status = marketStatus.Status,
                isOpen = marketStatus.IsOpen,
                timestamp = marketStatus.LastUpdate
            };

            var marketGroup = $"Market_{marketStatus.Code}";

            await Task.WhenAll(
                _marketDataHubContext.Clients.Group(marketGroup)
                    .SendAsync("MarketStatusUpdate", statusUpdate, cancellationToken),
                _dashboardHubContext.Clients.Group(marketGroup)
                    .SendAsync("MarketStatusUpdate", statusUpdate, cancellationToken),
                _marketDataHubContext.Clients.All
                    .SendAsync("MarketStatusUpdate", statusUpdate, cancellationToken),
                _dashboardHubContext.Clients.All
                    .SendAsync("MarketStatusUpdate", statusUpdate, cancellationToken)
            );

            _logger.LogDebug("Broadcasted market status for {Market}: {Status}", 
                marketStatus.Code, marketStatus.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting market status for {Market}", marketStatus.Code);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "StockDataPollingService stopping - Total Polls: {Total}, Successful: {Success}, Failed: {Failed}",
            _totalPolls, _successfulPolls, _failedPolls);

        _pollingTimer.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
