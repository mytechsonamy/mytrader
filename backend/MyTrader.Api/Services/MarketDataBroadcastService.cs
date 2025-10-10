using Microsoft.AspNetCore.SignalR;
using MyTrader.Api.Hubs;
using MyTrader.Services.Market;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.Services;

public class MarketDataBroadcastService : IHostedService
{
    private readonly IBinanceWebSocketService _binanceService;
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly IMarketDataRouter _marketDataRouter;
    private readonly ILogger<MarketDataBroadcastService> _logger;

    public MarketDataBroadcastService(
        IBinanceWebSocketService binanceService, 
        IHubContext<MarketDataHub> hubContext,
        IMarketDataRouter marketDataRouter,
        ILogger<MarketDataBroadcastService> logger)
    {
        _binanceService = binanceService;
        _hubContext = hubContext;
        _marketDataRouter = marketDataRouter;
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

            // Get all routing groups for this symbol (market-specific, asset class, etc.)
            var routingGroups = _marketDataRouter.GetRoutingGroups(priceData.Symbol);

            // Calculate percentage change
            var changePercent = priceData.PriceChange;

            var updateData = new
            {
                symbol = priceData.Symbol,
                price = priceData.Price,
                change = changePercent,
                volume = priceData.Volume,
                timestamp = priceData.Timestamp,
                market = _marketDataRouter.DetermineMarket(priceData.Symbol),
                assetClass = _marketDataRouter.ClassifyAssetClass(priceData.Symbol)
            };

            // DEBUG: Log what we're sending to clients
            _logger.LogWarning($"[PRICE DEBUG] Sending to clients: Symbol={updateData.symbol}, Price={updateData.price}, Volume={updateData.volume}, Change={updateData.change}%");

            // Broadcast to all relevant groups
            foreach (var groupName in routingGroups)
            {
                await _hubContext.Clients.Group(groupName)
                    .SendAsync("PriceUpdate", updateData);

                // Also send detailed market data update
                await _hubContext.Clients.Group(groupName)
                    .SendAsync("MarketDataUpdate", priceData);
            }

            _logger.LogDebug($"Broadcasted {priceData.Symbol} to groups: {string.Join(", ", routingGroups)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting price update for {priceData.Symbol}");
        }
    }
}