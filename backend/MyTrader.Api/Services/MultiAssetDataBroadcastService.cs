using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using MyTrader.Api.Hubs;
using MyTrader.Core.Models;
using MyTrader.Core.Enums;
using MarketStatus = MyTrader.Core.Enums.MarketStatus;
using MyTrader.Services.Market;
using System.Collections.Concurrent;

namespace MyTrader.Api.Services;

/// <summary>
/// Enhanced broadcast service that connects the existing Binance service to SignalR hubs
/// Supports multi-asset data broadcasting with enhanced features
/// </summary>
public class MultiAssetDataBroadcastService : IHostedService, IDisposable
{
    private readonly IBinanceWebSocketService _binanceService;
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MultiAssetDataBroadcastService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _lastBroadcastTimes;
    private readonly Timer _metricsTimer;
    private bool _disposed;

    // Broadcast throttling configuration
    private readonly TimeSpan _minBroadcastInterval = TimeSpan.FromMilliseconds(50); // Max 20 updates per second per symbol
    private readonly int _maxConcurrentBroadcasts = 10;
    private readonly SemaphoreSlim _broadcastSemaphore;

    // Metrics
    private long _totalBroadcasts;
    private long _throttledUpdates;
    private long _failedBroadcasts;

    public MultiAssetDataBroadcastService(
        IBinanceWebSocketService binanceService,
        IHubContext<MarketDataHub> hubContext,
        IServiceScopeFactory scopeFactory,
        ILogger<MultiAssetDataBroadcastService> logger)
    {
        _binanceService = binanceService;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _lastBroadcastTimes = new ConcurrentDictionary<string, DateTime>();
        _broadcastSemaphore = new SemaphoreSlim(_maxConcurrentBroadcasts, _maxConcurrentBroadcasts);

        // Initialize metrics timer (log metrics every 60 seconds)
        _metricsTimer = new Timer(LogMetrics, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MultiAssetDataBroadcastService starting - connecting multi-asset data to SignalR");

        try
        {
            // Subscribe to Binance service events
            _binanceService.PriceUpdated += OnBinancePriceUpdated;

            // Start metrics timer
            _metricsTimer.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

            _logger.LogInformation("MultiAssetDataBroadcastService started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MultiAssetDataBroadcastService");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MultiAssetDataBroadcastService stopping");

        try
        {
            // Stop metrics timer
            _metricsTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // Unsubscribe from Binance service events
            _binanceService.PriceUpdated -= OnBinancePriceUpdated;

            // Wait for any ongoing broadcasts to complete
            for (int i = 0; i < _maxConcurrentBroadcasts; i++)
            {
                await _broadcastSemaphore.WaitAsync(cancellationToken);
            }

            _logger.LogInformation("MultiAssetDataBroadcastService stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MultiAssetDataBroadcastService");
            throw;
        }
    }

    private async void OnBinancePriceUpdated(PriceUpdateData priceUpdate)
    {
        if (ShouldThrottleUpdate(priceUpdate.Symbol))
        {
            Interlocked.Increment(ref _throttledUpdates);
            return;
        }

        // Convert to multi-asset format for enhanced broadcasting
        var multiAssetUpdate = new MultiAssetPriceUpdate
        {
            Type = "PriceUpdate",
            AssetClass = AssetClassCode.CRYPTO,
            Symbol = priceUpdate.Symbol,
            Price = priceUpdate.Price,
            Change24h = priceUpdate.PriceChange,
            Volume = priceUpdate.Volume,
            MarketStatus = MyTrader.Core.Enums.MarketStatus.OPEN, // Crypto markets are always open
            Timestamp = priceUpdate.Timestamp,
            Source = "BINANCE",
            Metadata = new Dictionary<string, object>
            {
                { "exchange", "BINANCE" },
                { "originalTimestamp", priceUpdate.Timestamp }
            }
        };

        _ = Task.Run(async () => await BroadcastPriceUpdateAsync(multiAssetUpdate));
    }

    private async Task BroadcastPriceUpdateAsync(MultiAssetPriceUpdate priceUpdate)
    {
        if (!await _broadcastSemaphore.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogWarning("Broadcast semaphore timeout for price update: {Symbol}", priceUpdate.Symbol);
            Interlocked.Increment(ref _failedBroadcasts);
            return;
        }

        try
        {
            _logger.LogDebug("Broadcasting price update: {AssetClass} {Symbol} = {Price}",
                priceUpdate.AssetClass, priceUpdate.Symbol, priceUpdate.Price);

            var broadcastTasks = new List<Task>();

            // Send to symbol-specific group
            var symbolGroup = $"{priceUpdate.AssetClass}_{priceUpdate.Symbol}";
            broadcastTasks.Add(_hubContext.Clients.Group(symbolGroup).SendAsync("PriceUpdate", priceUpdate));

            // Send to asset class group for dashboard updates
            var assetClassGroup = $"AssetClass_{priceUpdate.AssetClass}";
            broadcastTasks.Add(_hubContext.Clients.Group(assetClassGroup).SendAsync("PriceUpdate", priceUpdate));

            // Legacy format for backward compatibility (crypto only)
            if (priceUpdate.AssetClass == AssetClassCode.CRYPTO)
            {
                var legacyUpdate = new
                {
                    symbol = priceUpdate.Symbol,
                    price = priceUpdate.Price,
                    change = priceUpdate.Change24h,
                    volume = priceUpdate.Volume,
                    timestamp = priceUpdate.Timestamp
                };

                broadcastTasks.Add(_hubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}")
                    .SendAsync("PriceUpdate", legacyUpdate));

                broadcastTasks.Add(_hubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}")
                    .SendAsync("MarketDataUpdate", priceUpdate));
            }

            await Task.WhenAll(broadcastTasks);

            // Update last broadcast time
            _lastBroadcastTimes.AddOrUpdate(priceUpdate.Symbol, DateTime.UtcNow, (_, _) => DateTime.UtcNow);

            Interlocked.Increment(ref _totalBroadcasts);

            _logger.LogDebug("Successfully broadcasted price update for {Symbol} to {GroupCount} groups",
                priceUpdate.Symbol, broadcastTasks.Count);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedBroadcasts);
            _logger.LogError(ex, "Error broadcasting price update for {AssetClass} {Symbol}",
                priceUpdate.AssetClass, priceUpdate.Symbol);
        }
        finally
        {
            _broadcastSemaphore.Release();
        }
    }


    private bool ShouldThrottleUpdate(string symbol)
    {
        if (!_lastBroadcastTimes.TryGetValue(symbol, out var lastBroadcast))
        {
            return false;
        }

        var timeSinceLastBroadcast = DateTime.UtcNow - lastBroadcast;
        return timeSinceLastBroadcast < _minBroadcastInterval;
    }

    private void LogMetrics(object? state)
    {
        try
        {
            var totalBroadcasts = Interlocked.Read(ref _totalBroadcasts);
            var throttledUpdates = Interlocked.Read(ref _throttledUpdates);
            var failedBroadcasts = Interlocked.Read(ref _failedBroadcasts);

            _logger.LogInformation(
                "MultiAssetDataBroadcast Metrics - Total: {TotalBroadcasts}, Throttled: {ThrottledUpdates}, Failed: {FailedBroadcasts}, Active Symbols: {ActiveSymbols}",
                totalBroadcasts, throttledUpdates, failedBroadcasts, _lastBroadcastTimes.Count);

            // Reset counters for next interval
            Interlocked.Exchange(ref _totalBroadcasts, 0);
            Interlocked.Exchange(ref _throttledUpdates, 0);
            Interlocked.Exchange(ref _failedBroadcasts, 0);

            // Clean up old broadcast times (symbols not seen in last 5 minutes)
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
            var keysToRemove = _lastBroadcastTimes
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _lastBroadcastTimes.TryRemove(key, out _);
            }

            if (keysToRemove.Any())
            {
                _logger.LogDebug("Cleaned up {Count} inactive symbols from broadcast tracking", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging metrics");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Stop the service if it's running
            StopAsync(CancellationToken.None).GetAwaiter().GetResult();

            // Dispose timers and semaphore
            _metricsTimer?.Dispose();
            _broadcastSemaphore?.Dispose();

            _disposed = true;
            _logger.LogInformation("MultiAssetDataBroadcastService disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing MultiAssetDataBroadcastService");
        }
    }
}