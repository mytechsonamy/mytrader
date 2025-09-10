using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public ActionResult GetMe()
    {
        // Replace with actual user repo/service
        var id = GetUserId();
        var dto = new { id, email = User.Identity?.Name, preferences = new { baseCurrency = "USD", theme = "dark" } };
        return Ok(dto);
    }

    [HttpPatch("me")]
    public ActionResult PatchMe([FromBody] PatchUserRequest req)
    {
        // Persist preferences and profile fields
        return NoContent();
    }
}

public record PatchUserRequest(string? DisplayName, string? BaseCurrency, string? Theme);
