using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MyTrader.Api.Hubs;

public class TradingHub : Hub
{
    // Method to send price updates to all connected clients
    public async Task BroadcastPriceUpdate(string symbol, decimal price, decimal change = 0)
    {
        await Clients.All.SendAsync("ReceivePriceUpdate", new { symbol, price, change, timestamp = DateTime.UtcNow.ToString("O") });
    }

    // Method to send signal updates to all connected clients
    public async Task BroadcastSignalUpdate(string symbol, string signal, object indicators)
    {
        await Clients.All.SendAsync("ReceiveSignalUpdate", new { symbol, signal, indicators, timestamp = DateTime.UtcNow.ToString("O") });
    }

    // Method to send market data batch updates
    public async Task BroadcastMarketData(Dictionary<string, object> symbols)
    {
        await Clients.All.SendAsync("ReceiveMarketData", new { symbols, timestamp = DateTime.UtcNow.ToString("O") });
    }
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? 
                     Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                     Context.User?.FindFirst("user_id")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? 
                     Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                     Context.User?.FindFirst("user_id")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}