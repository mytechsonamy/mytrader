using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Data;
using MyTrader.Core.Enums;
using MyTrader.Core.Models;
using MyTrader.Core.Services;
using System.Collections.Concurrent;
using StockPriceData = MyTrader.Core.Models.StockPriceData; // Use unified StockPriceData

namespace MyTrader.Services.Market;

/// <summary>
/// Background service that polls Yahoo Finance for stock prices every minute
/// and broadcasts updates via event for SignalR integration
/// </summary>
public class YahooFinancePollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<YahooFinancePollingService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, StockPriceData> _latestPrices = new(StringComparer.OrdinalIgnoreCase);

    // Event for price updates - MultiAssetDataBroadcastService will subscribe to this
    public event Action<StockPriceData>? StockPriceUpdated;

    public YahooFinancePollingService(
        IServiceScopeFactory scopeFactory,
        ILogger<YahooFinancePollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yahoo Finance Polling Service starting - polling every {Interval} minute(s)",
            _pollingInterval.TotalMinutes);

        // Wait before first poll to let system initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAllStockSymbolsAsync(stoppingToken);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Yahoo Finance Polling Service shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Yahoo Finance polling loop");
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        _logger.LogInformation("Yahoo Finance Polling Service stopped");
    }

    private async Task PollAllStockSymbolsAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("=== Starting stock price polling cycle ===");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITradingDbContext>();

            // Get all active stock symbols (only asset_class='STOCK' with asset_class_id populated)
            var stockSymbols = await dbContext.Symbols
                .Where(s => s.IsActive &&
                           s.IsTracked &&
                           s.AssetClass == "STOCK" &&
                           s.AssetClassId != null)
                .OrderBy(s => s.Ticker) // Simple alphabetical ordering
                .ToListAsync(cancellationToken);

            if (!stockSymbols.Any())
            {
                _logger.LogWarning("No active stock symbols found for polling");
                return;
            }

            _logger.LogInformation("Polling {Count} stock symbols: {Symbols}",
                stockSymbols.Count, string.Join(", ", stockSymbols.Select(s => $"{s.Ticker}({s.Venue})")));

            var successCount = 0;
            var failureCount = 0;

            foreach (var symbol in stockSymbols)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await PollSymbolPriceAsync(symbol, dbContext, cancellationToken);
                    successCount++;
                    await Task.Delay(300, cancellationToken); // Rate limit: 300ms between requests
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to poll {Symbol}", symbol.Ticker);
                    failureCount++;
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "=== Polling cycle completed in {Duration}s - Success: {Success}, Failed: {Failed} ===",
                duration.TotalSeconds, successCount, failureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stock price polling cycle");
        }
    }

    private async Task PollSymbolPriceAsync(
        Core.Models.Symbol symbol,
        ITradingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine market (BIST, NASDAQ, NYSE)
            var market = symbol.Venue?.ToUpperInvariant() ?? "NASDAQ";

            _logger.LogDebug("Polling {Symbol} from {Market}", symbol.Ticker, market);

            // ✅ FIX: Use YahooFinanceProvider which correctly returns PreviousClose from API
            using var apiScope = _scopeFactory.CreateScope();
            var httpClientFactory = apiScope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = apiScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var providerLogger = loggerFactory.CreateLogger<YahooFinanceProvider>();

            var yahooProvider = new YahooFinanceProvider(providerLogger, httpClientFactory, market);
            var marketDataList = await yahooProvider.GetPricesAsync(new List<string> { symbol.Ticker }, cancellationToken);

            if (marketDataList == null || !marketDataList.Any())
            {
                _logger.LogWarning("Failed to get price for {Symbol} ({Market}): No data returned",
                    symbol.Ticker, market);
                return;
            }

            var marketData = marketDataList[0];

            // ✅ Use the correct price change values from YahooFinanceProvider
            // (which uses actual PreviousClose from Yahoo API, not previous poll)
            var price = marketData.Price ?? 0;
            var priceChange = marketData.PriceChange ?? 0;
            var priceChangePercent = marketData.PriceChangePercent ?? 0;

            // Use generic STOCK asset class for frontend compatibility
            var assetClassCode = AssetClassCode.STOCK;

            // Create price update with correct values from provider
            var priceUpdate = new StockPriceData
            {
                Symbol = symbol.Ticker,
                AssetClass = assetClassCode,
                Market = market,
                Price = price,
                PreviousClose = marketData.PreviousClose, // ✅ Actual previous close from API
                PriceChange = priceChange, // ✅ Calculated from actual previous close
                PriceChangePercent = priceChangePercent, // ✅ Calculated from actual previous close
                Volume = (long)(marketData.Volume ?? 0),
                Timestamp = DateTime.UtcNow,
                Source = "YAHOO_POLLING",
                QualityScore = 80
            };

            // Cache latest price
            _latestPrices.AddOrUpdate(symbol.Ticker, priceUpdate, (_, _) => priceUpdate);

            // Save to database
            await SaveToMarketDataTableAsync(priceUpdate, dbContext, cancellationToken);

            // Fire event for SignalR broadcast
            StockPriceUpdated?.Invoke(priceUpdate);

            _logger.LogInformation("✓ {Symbol} ({Market}): ${Price:F2} {Change}",
                symbol.Ticker, market, price,
                priceChangePercent >= 0 ? $"+{priceChangePercent:F2}%" : $"{priceChangePercent:F2}%");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling {Symbol}", symbol.Ticker);
            throw;
        }
    }

    private async Task SaveToMarketDataTableAsync(
        StockPriceData priceData,
        ITradingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var marketData = new MarketData
            {
                Id = Guid.NewGuid(),
                Symbol = priceData.Symbol,
                Timeframe = "1MIN",
                Timestamp = priceData.Timestamp,
                Open = priceData.Price,
                High = priceData.Price,
                Low = priceData.Price,
                Close = priceData.Price,
                Volume = priceData.Volume,
                AssetClass = priceData.AssetClass.ToString() // Store asset class (STOCK)
            };

            await dbContext.MarketData.AddAsync(marketData, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ Saved {Symbol} ({AssetClass}) to market_data table",
                priceData.Symbol, priceData.AssetClass);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save {Symbol} to database (non-fatal)", priceData.Symbol);
            // Don't throw - we still want to broadcast even if DB save fails
        }
    }

    public StockPriceData? GetLatestPrice(string symbol)
    {
        return _latestPrices.TryGetValue(symbol, out var price) ? price : null;
    }

    public IReadOnlyCollection<StockPriceData> GetAllLatestPrices()
    {
        return _latestPrices.Values.ToList();
    }
}
