using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MyTrader.Realtime;

[Authorize]
public class TradingHub : Hub
{
    public override Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        return base.OnConnectedAsync();
    }
}
