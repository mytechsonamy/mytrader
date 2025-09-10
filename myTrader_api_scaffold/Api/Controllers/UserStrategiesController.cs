using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Application.Interfaces;
using MyTrader.Contracts;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("user-strategies")]
[Authorize]
public class UserStrategiesController : ControllerBase
{
    private readonly IStrategyService _svc;
    public UserStrategiesController(IStrategyService svc) => _svc = svc;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub")!);

    [HttpPost]
    public async Task<ActionResult<UserStrategyResponse>> Create([FromBody] CreateUserStrategyRequest req)
    {
        var res = await _svc.CreateUserStrategyAsync(GetUserId(), req);
        return CreatedAtAction(nameof(Create), new { id = res.Id }, res);
    }
}
