using Microsoft.Extensions.Logging;
using MyTrader.Core.Interfaces;
using MyTrader.Core.DTOs;

namespace MyTrader.Services.Market;

/// <summary>
/// Service for monitoring and tracking market status
/// </summary>
public class MarketStatusService : IMarketStatusService
{
    private readonly ILogger<MarketStatusService> _logger;
    private readonly IMarketDataRouter _marketDataRouter;
    private readonly Dictionary<string, MarketStatus> _marketStatuses;
    private Timer? _monitoringTimer;
    private readonly object _lock = new();

    public event EventHandler<MarketStatusChangedEventArgs>? OnMarketStatusChanged;

    public MarketStatusService(
        ILogger<MarketStatusService> logger,
        IMarketDataRouter marketDataRouter)
    {
        _logger = logger;
        _marketDataRouter = marketDataRouter;
        _marketStatuses = new Dictionary<string, MarketStatus>();
    }

    public async Task<List<MarketStatusDto>> GetAllMarketStatusesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                // Update all market statuses
                var activeMarkets = _marketDataRouter.GetActiveMarkets();
                var statusList = new List<MarketStatusDto>();

                foreach (var market in activeMarkets)
                {
                    var status = _marketDataRouter.GetMarketStatus(market);
                    _marketStatuses[market] = status;

                    statusList.Add(new MarketStatusDto
                    {
                        Code = market,
                        Status = status.Status,
                        IsOpen = status.IsOpen,
                        NextOpen = status.NextOpen,
                        NextClose = status.NextClose,
                        Timezone = status.TimeZone,
                        LastUpdate = status.LastUpdate,
                        StatusUpdatedAt = status.LastUpdate
                    });
                }

                return statusList;
            }
        }, cancellationToken);
    }

    public async Task<MarketStatusDto?> GetMarketStatusAsync(string marketCode, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_marketStatuses.TryGetValue(marketCode, out var status))
                {
                    return new MarketStatusDto
                    {
                        Code = marketCode,
                        Status = status.Status,
                        IsOpen = status.IsOpen,
                        NextOpen = status.NextOpen,
                        NextClose = status.NextClose,
                        Timezone = status.TimeZone,
                        LastUpdate = status.LastUpdate,
                        StatusUpdatedAt = status.LastUpdate
                    };
                }

                // If not cached, get fresh status from router
                var freshStatus = _marketDataRouter.GetMarketStatus(marketCode);
                _marketStatuses[marketCode] = freshStatus;

                return new MarketStatusDto
                {
                    Code = marketCode,
                    Status = freshStatus.Status,
                    IsOpen = freshStatus.IsOpen,
                    NextOpen = freshStatus.NextOpen,
                    NextClose = freshStatus.NextClose,
                    Timezone = freshStatus.TimeZone,
                    LastUpdate = freshStatus.LastUpdate,
                    StatusUpdatedAt = freshStatus.LastUpdate
                };
            }
        }, cancellationToken);
    }

    public async Task<bool> UpdateMarketStatusAsync(string marketCode, string status, string? statusMessage = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_marketStatuses.TryGetValue(marketCode, out var currentStatus))
                {
                    var oldStatus = currentStatus.Status;
                    currentStatus.Status = status;
                    currentStatus.LastUpdate = DateTime.UtcNow;
                    _marketStatuses[marketCode] = currentStatus;

                    // Raise event if status changed
                    if (oldStatus != status)
                    {
                        OnMarketStatusChanged?.Invoke(this, new MarketStatusChangedEventArgs
                        {
                            MarketCode = marketCode,
                            PreviousStatus = oldStatus,
                            NewStatus = status,
                            Timestamp = DateTime.UtcNow,
                            StatusMessage = statusMessage
                        });
                    }

                    return true;
                }

                return false;
            }
        }, cancellationToken);
    }

    public async Task<bool> IsMarketOpenAsync(string marketCode, CancellationToken cancellationToken = default)
    {
        var status = await GetMarketStatusAsync(marketCode, cancellationToken);
        return status?.IsOpen ?? false;
    }

    public async Task<MarketTimingDto?> GetMarketTimingAsync(string marketCode, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_marketStatuses.TryGetValue(marketCode, out var status))
                {
                    var timeUntilOpen = status.NextOpen.HasValue
                        ? status.NextOpen.Value - DateTime.UtcNow
                        : (TimeSpan?)null;

                    var timeUntilClose = status.NextClose.HasValue
                        ? status.NextClose.Value - DateTime.UtcNow
                        : (TimeSpan?)null;

                    return new MarketTimingDto
                    {
                        MarketCode = marketCode,
                        Status = status.Status,
                        NextOpen = status.NextOpen,
                        NextClose = status.NextClose,
                        TimeUntilOpen = timeUntilOpen,
                        TimeUntilClose = timeUntilClose,
                        Timezone = status.TimeZone,
                        NextSessionType = status.IsOpen ? "CLOSE" : "OPEN"
                    };
                }

                return null;
            }
        }, cancellationToken);
    }

    public Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting market status monitoring");

        // Initialize market statuses
        var activeMarkets = _marketDataRouter.GetActiveMarkets();
        lock (_lock)
        {
            foreach (var market in activeMarkets)
            {
                var status = _marketDataRouter.GetMarketStatus(market);
                _marketStatuses[market] = status;
                _logger.LogInformation("Initial status for {Market}: {Status}", market, status.Status);
            }
        }

        // Start monitoring timer - check every minute
        _monitoringTimer = new Timer(
            CheckMarketStatusChanges,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));

        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping market status monitoring");

        _monitoringTimer?.Dispose();
        _monitoringTimer = null;

        return Task.CompletedTask;
    }

    private void CheckMarketStatusChanges(object? state)
    {
        try
        {
            var activeMarkets = _marketDataRouter.GetActiveMarkets();

            foreach (var market in activeMarkets)
            {
                var newStatus = _marketDataRouter.GetMarketStatus(market);

                lock (_lock)
                {
                    if (_marketStatuses.TryGetValue(market, out var oldStatus))
                    {
                        // Check if status changed
                        if (oldStatus.Status != newStatus.Status)
                        {
                            _logger.LogInformation(
                                "Market status changed for {Market}: {OldStatus} -> {NewStatus}",
                                market, oldStatus.Status, newStatus.Status);

                            // Update cached status
                            _marketStatuses[market] = newStatus;

                            // Raise event
                            OnMarketStatusChanged?.Invoke(this, new MarketStatusChangedEventArgs
                            {
                                MarketCode = market,
                                PreviousStatus = oldStatus.Status,
                                NewStatus = newStatus.Status,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            // Just update the cached status (times may have changed)
                            _marketStatuses[market] = newStatus;
                        }
                    }
                    else
                    {
                        // First time seeing this market
                        _marketStatuses[market] = newStatus;
                        _logger.LogInformation("New market tracked: {Market} - Status: {Status}",
                            market, newStatus.Status);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking market status changes");
        }
    }

}
