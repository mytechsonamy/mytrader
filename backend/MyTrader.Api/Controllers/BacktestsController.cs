using Microsoft.AspNetCore.Mvc;
using MyTrader.Api.Services;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/backtests")] 
public class BacktestsController : ControllerBase
{
    private readonly BacktestServiceSimple _service;

    public BacktestsController(BacktestServiceSimple service)
    {
        _service = service;
    }

    [HttpPost("run")] 
    public async Task<ActionResult<BacktestRunResult>> Run([FromBody] MyTrader.Api.Services.BacktestRunRequest req, CancellationToken ct)
    {
        var result = await _service.RunAsync(req, ct);
        if (result.Message != "ok")
            return BadRequest(result);
        return Ok(result);
    }
}

