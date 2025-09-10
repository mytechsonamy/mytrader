using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Market;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(/* Roles = "Admin" */)]
public class AdminController : ControllerBase
{
    private readonly ISymbolService _symbolService;
    
    public AdminController(ISymbolService symbolService) 
    {
        _symbolService = symbolService;
    }

    [HttpPost("symbols/track")]
    public async Task<ActionResult> Track([FromBody] TrackSymbol req)
    {
        var symbol = await _symbolService.GetOrCreateAsync(req.Ticker, req.Venue ?? "BINANCE");
        await _symbolService.SetTrackedAsync(symbol.Id, true);
        return Ok(symbol);
    }

    [HttpPost("indicators/register")]
    public ActionResult RegisterIndicator([FromBody] RegisterIndicator req)
    {
        // TODO: Add to IndicatorRegistry (not included here)
        return Accepted(new { message = "Indicator registration accepted", name = req.Name });
    }

    [HttpPost("strategies/{id:guid}/backtest")]
    public ActionResult KickOffBacktest(Guid id)
    {
        // TODO: Enqueue job to backtest a strategy across tracked symbols
        return Accepted(new { strategyId = id, status = "enqueued" });
    }

    [HttpGet("symbols")]
    public async Task<ActionResult> GetAllSymbols()
    {
        var symbols = await _symbolService.GetTrackedAsync();
        return Ok(symbols);
    }
}

public record TrackSymbol(string Ticker, string? Venue);
public record RegisterIndicator(string Name, string Code);