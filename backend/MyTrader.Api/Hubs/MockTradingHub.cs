using Microsoft.AspNetCore.SignalR;

namespace MyTrader.Api.Hubs;

public class MockTradingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
        
        // Send initial mock data
        await Clients.Caller.SendAsync("initial", new
        {
            status = "connected",
            message = "Connected to mock trading hub",
            timestamp = DateTime.UtcNow
        });

        // Send mock market data
        await SendMockMarketData();
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    private async Task SendMockMarketData()
    {
        var mockData = new Dictionary<string, object>
        {
            ["BTC"] = new { 
                symbol = "BTCUSDT", 
                display_name = "Bitcoin",
                price = 65430.50m + Random.Shared.Next(-1000, 1000), 
                change = Math.Round((decimal)(Random.Shared.NextDouble() * 10 - 5), 2),
                signal = GetRandomSignal(),
                indicators = new { RSI = 45.2, MACD = 0.5, BB_UPPER = 66000, BB_LOWER = 64000 },
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            ["ETH"] = new { 
                symbol = "ETHUSDT", 
                display_name = "Ethereum",
                price = 3542.80m + Random.Shared.Next(-200, 200), 
                change = Math.Round((decimal)(Random.Shared.NextDouble() * 6 - 3), 2),
                signal = GetRandomSignal(),
                indicators = new { RSI = 62.1, MACD = -0.3, BB_UPPER = 3600, BB_LOWER = 3500 },
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            ["XRP"] = new { 
                symbol = "XRPUSDT", 
                display_name = "Ripple",
                price = 0.5847m + (decimal)(Random.Shared.NextDouble() * 0.1 - 0.05), 
                change = Math.Round((decimal)(Random.Shared.NextDouble() * 4 - 2), 2),
                signal = GetRandomSignal(),
                indicators = new { RSI = 51.7, MACD = 0.1, BB_UPPER = 0.59, BB_LOWER = 0.57 },
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            ["BNB"] = new { 
                symbol = "BNBUSDT", 
                display_name = "Binance Coin",
                price = 598.75m + Random.Shared.Next(-50, 50), 
                change = Math.Round((decimal)(Random.Shared.NextDouble() * 5 - 2.5), 2),
                signal = GetRandomSignal(),
                indicators = new { RSI = 48.9, MACD = 0.7, BB_UPPER = 610, BB_LOWER = 585 },
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            ["SOL"] = new { 
                symbol = "SOLUSDT", 
                display_name = "Solana",
                price = 132.45m + Random.Shared.Next(-20, 20), 
                change = Math.Round((decimal)(Random.Shared.NextDouble() * 8 - 4), 2),
                signal = GetRandomSignal(),
                indicators = new { RSI = 42.8, MACD = 0.9, BB_UPPER = 140, BB_LOWER = 125 },
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        };

        await Clients.Caller.SendAsync("market", mockData);
        
        // Send individual symbol updates
        foreach (var kvp in mockData)
        {
            await Clients.Caller.SendAsync("symbol_update", kvp.Key, kvp.Value);
        }
    }

    private string GetRandomSignal()
    {
        string[] signals = { "BUY", "SELL", "NEUTRAL" };
        return signals[Random.Shared.Next(signals.Length)];
    }

    public async Task SubscribeToSymbol(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Symbol_{symbol}");
        Console.WriteLine($"Client {Context.ConnectionId} subscribed to {symbol}");
    }

    public async Task UnsubscribeFromSymbol(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Symbol_{symbol}");
        Console.WriteLine($"Client {Context.ConnectionId} unsubscribed from {symbol}");
    }
}