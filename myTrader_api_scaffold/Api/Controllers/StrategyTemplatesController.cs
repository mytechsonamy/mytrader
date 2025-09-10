using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Application.Interfaces;
using MyTrader.Contracts;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("strategy-templates")]
[Authorize]
public class StrategyTemplatesController : ControllerBase
{
    private readonly IStrategyService _svc;
    public StrategyTemplatesController(IStrategyService svc) => _svc = svc;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub")!);

    [HttpPost]
    public async Task<ActionResult<StrategyTemplateResponse>> Create([FromBody] CreateStrategyTemplateRequest req)
    {
        var res = await _svc.CreateTemplateAsync(GetUserId(), req);
        return CreatedAtAction(nameof(Create), new { id = res.Id }, res);
    }
}
