using Microsoft.AspNetCore.SignalR;
using MyTrader.Core.DTOs.Portfolio;

namespace MyTrader.API.Hubs;

public class PortfolioHub : Hub
{
    public async Task JoinPortfolioGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Portfolio_{userId}");
        await Clients.Group($"Portfolio_{userId}").SendAsync("PortfolioConnectionEstablished", new
        {
            UserId = userId,
            ConnectionId = Context.ConnectionId,
            Message = "Real-time portfolio monitoring connected"
        });
    }

    public async Task LeavePortfolioGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Portfolio_{userId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Auto-remove from all groups when disconnected
        await base.OnDisconnectedAsync(exception);
    }
}

// Extension methods for easier SignalR notifications
public static class PortfolioHubExtensions
{
    public static async Task NotifyPortfolioUpdate(this IHubContext<PortfolioHub> hubContext, 
        string userId, PortfolioSummaryDto portfolio)
    {
        await hubContext.Clients.Group($"Portfolio_{userId}")
            .SendAsync("PortfolioUpdated", portfolio);
    }

    public static async Task NotifyPositionUpdate(this IHubContext<PortfolioHub> hubContext,
        string userId, PortfolioPositionDto position)
    {
        await hubContext.Clients.Group($"Portfolio_{userId}")
            .SendAsync("PositionUpdated", position);
    }

    public static async Task NotifyNewTransaction(this IHubContext<PortfolioHub> hubContext,
        string userId, TransactionDto transaction)
    {
        await hubContext.Clients.Group($"Portfolio_{userId}")
            .SendAsync("TransactionExecuted", transaction);
    }

    public static async Task NotifyPnLUpdate(this IHubContext<PortfolioHub> hubContext,
        string userId, decimal totalPnL, decimal dailyPnL, decimal totalReturnPercent)
    {
        await hubContext.Clients.Group($"Portfolio_{userId}")
            .SendAsync("PnLUpdated", new
            {
                TotalPnL = totalPnL,
                DailyPnL = dailyPnL,
                TotalReturnPercent = totalReturnPercent,
                UpdatedAt = DateTime.UtcNow
            });
    }

    public static async Task NotifyMarketDataUpdate(this IHubContext<PortfolioHub> hubContext,
        string userId, string symbol, decimal currentPrice, decimal priceChange, decimal priceChangePercent)
    {
        await hubContext.Clients.Group($"Portfolio_{userId}")
            .SendAsync("MarketDataUpdated", new
            {
                Symbol = symbol,
                CurrentPrice = currentPrice,
                PriceChange = priceChange,
                PriceChangePercent = priceChangePercent,
                UpdatedAt = DateTime.UtcNow
            });
    }
}