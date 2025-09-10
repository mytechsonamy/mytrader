using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Application.Interfaces;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("backtests")]
[Authorize]
public class BacktestsController : ControllerBase
{
    private readonly IBacktestService _svc;
    public BacktestsController(IBacktestService svc) => _svc = svc;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub")!);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> Get(Guid id)
    {
        var res = await _svc.GetResultAsync(GetUserId(), id);
        return res is null ? NotFound() : Ok(res);
    }
}
