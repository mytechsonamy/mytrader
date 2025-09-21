using Microsoft.AspNetCore.SignalR;
using MyTrader.Api.Hubs;
using MyTrader.Services.Market;

namespace MyTrader.Api.Services;

public class MarketDataBroadcastService : IHostedService
{
    private readonly IBinanceWebSocketService _binanceService;
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly ILogger<MarketDataBroadcastService> _logger;

    public MarketDataBroadcastService(
        IBinanceWebSocketService binanceService, 
        IHubContext<MarketDataHub> hubContext, 
        ILogger<MarketDataBroadcastService> logger)
    {
        _binanceService = binanceService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MarketDataBroadcastService starting - connecting Binance data to SignalR");
        
        // Subscribe to Binance price updates
        _binanceService.PriceUpdated += OnPriceUpdated;
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MarketDataBroadcastService stopping");
        
        // Unsubscribe from Binance price updates
        _binanceService.PriceUpdated -= OnPriceUpdated;
        
        return Task.CompletedTask;
    }

    private async void OnPriceUpdated(PriceUpdateData priceData)
    {
        try
        {
            _logger.LogDebug($"Broadcasting price update: {priceData.Symbol} = {priceData.Price}");
            
            // Calculate percentage change
            var changePercent = priceData.PriceChange;
            
            // Send to all clients subscribed to this symbol
            await _hubContext.Clients.Group($"Symbol_{priceData.Symbol}")
                .SendAsync("PriceUpdate", new
                {
                    symbol = priceData.Symbol,
                    price = priceData.Price,
                    change = changePercent,
                    volume = priceData.Volume,
                    timestamp = priceData.Timestamp
                });
            
            // Also send individual price update
            await _hubContext.Clients.Group($"Symbol_{priceData.Symbol}")
                .SendAsync("MarketDataUpdate", priceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting price update for {priceData.Symbol}");
        }
    }
}