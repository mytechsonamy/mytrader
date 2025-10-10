using Microsoft.AspNetCore.SignalR;
using MyTrader.Api.Hubs;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.Services;

/// <summary>
/// Hosted service that monitors market status and broadcasts changes via SignalR
/// </summary>
public class MarketStatusBroadcastService : IHostedService
{
    private readonly IMarketStatusService _marketStatusService;
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly ILogger<MarketStatusBroadcastService> _logger;

    public MarketStatusBroadcastService(
        IMarketStatusService marketStatusService,
        IHubContext<MarketDataHub> hubContext,
        ILogger<MarketStatusBroadcastService> logger)
    {
        _marketStatusService = marketStatusService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MarketStatusBroadcastService starting");

        // Subscribe to market status changes
        _marketStatusService.OnMarketStatusChanged += OnMarketStatusChanged;

        // Start monitoring
        await _marketStatusService.StartMonitoringAsync(cancellationToken);

        // Broadcast initial market statuses
        await BroadcastAllMarketStatuses();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MarketStatusBroadcastService stopping");

        // Unsubscribe from market status changes
        _marketStatusService.OnMarketStatusChanged -= OnMarketStatusChanged;

        // Stop monitoring
        await _marketStatusService.StopMonitoringAsync();
    }

    private async void OnMarketStatusChanged(object? sender, MarketStatusChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation(
                "Broadcasting market status change: {Market} - {OldStatus} -> {NewStatus}",
                e.MarketCode, e.PreviousStatus, e.NewStatus);

            var statusUpdate = new
            {
                market = e.MarketCode,
                status = e.NewStatus,
                previousStatus = e.PreviousStatus,
                timestamp = e.Timestamp,
                statusMessage = e.StatusMessage
            };

            // Broadcast to market-specific group
            await _hubContext.Clients.Group($"MarketStatus_{e.MarketCode}")
                .SendAsync("MarketStatusChanged", statusUpdate);

            // Also broadcast to all markets group
            await _hubContext.Clients.Group("MarketStatus_All")
                .SendAsync("MarketStatusChanged", statusUpdate);

            // Broadcast to general market data group
            await _hubContext.Clients.Group("MarketData_All")
                .SendAsync("MarketStatusUpdate", statusUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting market status change for {Market}", e.MarketCode);
        }
    }

    private async Task BroadcastAllMarketStatuses()
    {
        try
        {
            var allStatuses = await _marketStatusService.GetAllMarketStatusesAsync();

            var statusData = allStatuses.Select(status => new
            {
                market = status.Code,
                status = status.Status,
                isOpen = status.IsOpen,
                nextOpen = status.NextOpen,
                nextClose = status.NextClose,
                timeZone = status.Timezone,
                lastUpdate = status.LastUpdate
            }).ToList();

            _logger.LogInformation("Broadcasting initial market statuses for {Count} markets", statusData.Count);

            // Broadcast to all clients
            await _hubContext.Clients.All.SendAsync("AllMarketStatuses", new
            {
                markets = statusData,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting all market statuses");
        }
    }
}
