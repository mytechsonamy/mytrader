using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Market;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/symbols")]
public class SymbolsController : ControllerBase
{
    private readonly ISymbolService _symbolService;
    
    public SymbolsController(ISymbolService symbolService) 
    {
        _symbolService = symbolService;
    }

    [HttpGet]
    [Authorize]
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

    [HttpGet("test")]
    [AllowAnonymous]
    public ActionResult GetTest()
    {
        return Ok(new { message = "Test endpoint working", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Public endpoint for market data overview
    /// </summary>
    [HttpGet("market-overview")]
    [AllowAnonymous]
    public async Task<ActionResult> GetMarketOverview()
    {
        try
        {
            var allSymbols = await _symbolService.GetActiveSymbolsAsync();
            var trackedSymbols = await _symbolService.GetTrackedAsync();
            
            // Group by venue
            var venueBreakdown = allSymbols
                .GroupBy(s => s.Venue)
                .Select(g => new
                {
                    Venue = g.Key,
                    Count = g.Count(),
                    TrackedCount = g.Count(s => s.IsTracked),
                    Symbols = g.Select(s => s.Ticker).Take(3).ToArray()
                })
                .ToArray();

            var overview = new
            {
                TotalSymbols = allSymbols.Count,
                TrackedSymbols = trackedSymbols.Count,
                VenueBreakdown = venueBreakdown,
                MarketSentiment = "Neutral", // Mock data
                LastUpdated = DateTime.UtcNow
            };

            return Ok(new { success = true, data = overview });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Failed to get market overview" });
        }
    }

    [HttpGet("tracked")]
    [Authorize]
    public async Task<ActionResult> GetTracked()
    {
        var list = await _symbolService.GetTrackedAsync();
        return Ok(list);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult> Create([FromBody] CreateSymbolRequest req)
    {
        var created = await _symbolService.GetOrCreateAsync(req.Ticker, req.Venue ?? "BINANCE", req.BaseCcy, req.QuoteCcy);
        return CreatedAtAction(nameof(GetTracked), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
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