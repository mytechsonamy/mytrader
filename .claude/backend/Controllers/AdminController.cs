using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Application.Interfaces;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(/* Roles = "Admin" */)]
public class AdminController : ControllerBase
{
    private readonly ISymbolService _symbols;
    public AdminController(ISymbolService symbols) { _symbols = symbols; }

    [HttpPost("symbols/track")]
    public async Task<ActionResult> Track([FromBody] TrackSymbol req)
    {
        var sym = await _symbols.GetOrCreateAsync(req.Ticker, req.Venue ?? "BINANCE");
        await _symbols.SetTrackedAsync(sym.Id, true);
        return Ok(sym);
    }

    [HttpPost("indicators/register")]
    public ActionResult RegisterIndicator([FromBody] RegisterIndicator req)
    {
        // TODO: Add to IndicatorRegistry (not included here)
        return Accepted();
    }

    [HttpPost("strategies/{id:guid}/backtest")]
    public ActionResult KickOffBacktest(Guid id)
    {
        // Enqueue job to backtest a strategy across tracked symbols
        return Accepted(new { strategyId = id, status = "enqueued" });
    }
}

public record TrackSymbol(string Ticker, string? Venue);
public record RegisterIndicator(string Name, string Code);
