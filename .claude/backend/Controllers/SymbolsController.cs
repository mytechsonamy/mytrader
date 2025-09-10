using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Application.Interfaces;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/symbols")]
[Authorize]
public class SymbolsController : ControllerBase
{
    private readonly ISymbolService _svc;
    public SymbolsController(ISymbolService svc) => _svc = svc;

    [HttpGet("tracked")]
    public async Task<ActionResult> GetTracked()
    {
        var list = await _svc.GetTrackedAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateSymbolRequest req)
    {
        var created = await _svc.GetOrCreateAsync(req.Ticker, req.Venue ?? "BINANCE", req.BaseCcy, req.QuoteCcy);
        return CreatedAtAction(nameof(GetTracked), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult> Patch(Guid id, [FromBody] PatchSymbolRequest req)
    {
        var ok = await _svc.SetTrackedAsync(id, req.IsTracked);
        return ok ? NoContent() : NotFound();
    }
}

public record CreateSymbolRequest(string Ticker, string? Venue, string? BaseCcy, string? QuoteCcy);
public record PatchSymbolRequest(bool IsTracked);
