using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Application.Interfaces;
using MyTrader.Contracts;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("strategies")]
[Authorize]
public class StrategiesController : ControllerBase
{
    private readonly IBacktestService _backtests;
    public StrategiesController(IBacktestService backtests) => _backtests = backtests;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub")!);

    [HttpPost("{id:guid}/backtest")]
    public async Task<ActionResult<BacktestResponse>> StartBacktest(Guid id, [FromBody] BacktestRequest req)
    {
        var (btId, status) = await _backtests.StartAsync(GetUserId(), id, req);
        return Accepted(new BacktestResponse(btId, status));
    }
}
