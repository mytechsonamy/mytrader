using Microsoft.AspNetCore.SignalR;
using MyTrader.Services.Market;

namespace MyTrader.Api.Hubs;

public class MarketDataHub : Hub
{
    private readonly IBinanceWebSocketService _binanceService;
    private readonly ILogger<MarketDataHub> _logger;

    public MarketDataHub(IBinanceWebSocketService binanceService, ILogger<MarketDataHub> logger)
    {
        _binanceService = binanceService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected to MarketDataHub: {Context.ConnectionId}");
        
        await Clients.Caller.SendAsync("ConnectionStatus", new
        {
            status = "connected",
            message = "Connected to real-time market data",
            timestamp = DateTime.UtcNow
        });
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected from MarketDataHub: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToPriceUpdates(object symbolData)
    {
        List<string> symbols;
        
        // Handle both string and List<string> inputs
        if (symbolData is string singleSymbol)
        {
            symbols = new List<string> { singleSymbol };
        }
        else if (symbolData is List<string> symbolList)
        {
            symbols = symbolList;
        }
        else if (symbolData is string[] symbolArray)
        {
            symbols = symbolArray.ToList();
        }
        else
        {
            _logger.LogWarning($"Invalid symbol data type from client {Context.ConnectionId}: {symbolData?.GetType()}");
            return;
        }
        
        _logger.LogInformation($"Client {Context.ConnectionId} subscribing to symbols: {string.Join(", ", symbols)}");
        
        // Add client to groups for each symbol
        foreach (var symbol in symbols)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Symbol_{symbol}");
        }
        
        await Clients.Caller.SendAsync("SubscriptionConfirmed", symbols);
    }

    public async Task UnsubscribeFromPriceUpdates(List<string> symbols)
    {
        _logger.LogInformation($"Client {Context.ConnectionId} unsubscribing from symbols: {string.Join(", ", symbols)}");
        
        // Remove client from groups for each symbol
        foreach (var symbol in symbols)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Symbol_{symbol}");
        }
        
        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", symbols);
    }
}