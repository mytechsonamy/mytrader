using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Market;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/symbols")]
[Authorize]
public class SymbolsController : ControllerBase
{
    private readonly ISymbolService _symbolService;
    
    public SymbolsController(ISymbolService symbolService) 
    {
        _symbolService = symbolService;
    }

    [HttpGet]
    public async Task<ActionResult> GetSymbols()
    {
        // Return symbols in the format expected by frontend
        var trackedSymbols = await _symbolService.GetTrackedAsync();
        var symbols = trackedSymbols.ToDictionary(
            s => s.Ticker.Replace("USDT", ""), // Use clean symbol as key (BTC, ETH, etc.)
            s => new
            {
                symbol = s.Ticker,
                display_name = s.Display,
                precision = 2, // Default precision
                strategy_type = "quality_over_quantity" // Default strategy
            }
        );
        
        return Ok(new { symbols, interval = "1m" });
    }

    [HttpGet("tracked")]
    public async Task<ActionResult> GetTracked()
    {
        var list = await _symbolService.GetTrackedAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateSymbolRequest req)
    {
        var created = await _symbolService.GetOrCreateAsync(req.Ticker, req.Venue ?? "BINANCE", req.BaseCcy, req.QuoteCcy);
        return CreatedAtAction(nameof(GetTracked), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult> Patch(Guid id, [FromBody] PatchSymbolRequest req)
    {
        var success = await _symbolService.SetTrackedAsync(id, req.IsTracked);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var symbol = await _symbolService.GetByIdAsync(id);
        return symbol != null ? Ok(symbol) : NotFound();
    }
}

public record CreateSymbolRequest(string Ticker, string? Venue, string? BaseCcy, string? QuoteCcy);
public record PatchSymbolRequest(bool IsTracked);