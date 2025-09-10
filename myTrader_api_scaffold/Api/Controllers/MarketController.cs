using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Application.Interfaces;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/market")]
[Authorize]
public class MarketController : ControllerBase
{
    private readonly IMarketDataService _svc;
    public MarketController(IMarketDataService svc) => _svc = svc;

    [HttpGet("candles")]
    public async Task<ActionResult> GetCandles([FromQuery] string symbol, [FromQuery] string tf, [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        var res = await _svc.GetCandlesAsync(symbol, tf, from, to);
        return Ok(res);
    }
}
