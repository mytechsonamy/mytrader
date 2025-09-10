using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Application.Interfaces;
using MyTrader.Contracts;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) { _authService = authService; }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"));

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var userId = GetUserId();
        var ua = Request.Headers["User-Agent"].ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (ok, tokens) = await _authService.RefreshTokenAsync(userId, request.RefreshToken, ua, ip);
        if (!ok || tokens is null) return Unauthorized();
        return Ok(tokens);
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult> Sessions()
    {
        var userId = GetUserId();
        var list = await _authService.ListSessionsAsync(userId);
        return Ok(list);
    }

    [HttpDelete("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = GetUserId();
        await _authService.RevokeAllAsync(userId);
        return NoContent();
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<IActionResult> Revoke(Guid sessionId)
    {
        var userId = GetUserId();
        var ok = await _authService.RevokeSessionAsync(userId, sessionId);
        return ok ? NoContent() : NotFound();
    }
}
