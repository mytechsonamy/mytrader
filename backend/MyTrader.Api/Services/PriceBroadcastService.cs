using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Api.Hubs;
using MyTrader.Services.Market;
using MyTrader.Services.Trading;

namespace MyTrader.Api.Services;

public class PriceBroadcastService : BackgroundService
{
    private readonly ILogger<PriceBroadcastService> _logger;
    private readonly IHubContext<TradingHub> _hubContext;
    private readonly IBinanceWebSocketService _binanceService;
    private readonly ITechnicalIndicatorService _indicatorService;

    public PriceBroadcastService(
        ILogger<PriceBroadcastService> logger,
        IHubContext<TradingHub> hubContext,
        IBinanceWebSocketService binanceService,
        ITechnicalIndicatorService indicatorService)
    {
        _logger = logger;
        _hubContext = hubContext;
        _binanceService = binanceService;
        _indicatorService = indicatorService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to price updates from Binance WebSocket
        _binanceService.PriceUpdated += OnPriceUpdated;
        
        _logger.LogInformation("Price broadcast service started");
        
        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async void OnPriceUpdated(PriceUpdateData priceData)
    {
        try
        {
            // Calculate technical indicators for this price update
            var indicators = await _indicatorService.CalculateIndicatorsAsync(priceData.Symbol, priceData.Price);

            // Broadcast price update with indicators to all connected clients
            await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", new
            {
                symbol = priceData.Symbol,
                price = priceData.Price,
                change = priceData.PriceChange,
                volume = priceData.Volume,
                timestamp = priceData.Timestamp.ToString("O")
            });

            // Broadcast technical indicators
            await _hubContext.Clients.All.SendAsync("ReceiveSignalUpdate", new
            {
                symbol = priceData.Symbol,
                signal = indicators.Signal,
                trend = indicators.Trend,
                indicators = new
                {
                    rsi = indicators.RSI,
                    macd = indicators.MACD,
                    macdSignal = indicators.MACDSignal,
                    macdHistogram = indicators.MACDHistogram,
                    bbUpper = indicators.BBUpper,
                    bbMiddle = indicators.BBMiddle,
                    bbLower = indicators.BBLower,
                    sma20 = indicators.SMA20,
                    sma50 = indicators.SMA50,
                    ema12 = indicators.EMA12,
                    ema26 = indicators.EMA26
                },
                timestamp = indicators.Timestamp.ToString("O")
            });

            _logger.LogDebug("Broadcasted price and indicators for {Symbol}: Price={Price}, RSI={RSI}, Signal={Signal}", 
                priceData.Symbol, priceData.Price, indicators.RSI, indicators.Signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting price update for {Symbol}", priceData.Symbol);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _binanceService.PriceUpdated -= OnPriceUpdated;
        _logger.LogInformation("Price broadcast service stopped");
        await base.StopAsync(cancellationToken);
    }
}