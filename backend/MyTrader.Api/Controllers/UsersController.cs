using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/user")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly TradingDbContext _context;

    public UsersController(TradingDbContext context)
    {
        _context = context;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<ActionResult> GetMe()
    {
        var userId = GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            return NotFound();

        var dto = new { 
            id = user.Id, 
            email = user.Email, 
            firstName = user.FirstName,
            lastName = user.LastName,
            phone = user.Phone,
            preferences = new { 
                baseCurrency = "USD", 
                initialCapital = user.DefaultInitialCapital,
                theme = "dark" 
            } 
        };
        
        return Ok(dto);
    }

    [HttpGet("profile")]
    public async Task<ActionResult> GetProfile()
    {
        // Alias for GetMe to match frontend expectations
        return await GetMe();
    }

    [HttpPatch("me")]
    public async Task<ActionResult> PatchMe([FromBody] PatchUserRequest req)
    {
        var userId = GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            return NotFound();

        // Update allowed fields
        if (!string.IsNullOrEmpty(req.DisplayName))
        {
            var parts = req.DisplayName.Split(' ', 2);
            user.FirstName = parts[0];
            if (parts.Length > 1)
                user.LastName = parts[1];
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpGet("layout-preferences")]
    public async Task<ActionResult> GetLayoutPreferences()
    {
        var userId = GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            return NotFound();

        // For now, return empty preferences - in production this would be stored in UserNotificationPreferences or similar table
        return Ok(new { asset_order = new string[0] });
    }

    [HttpPost("layout-preferences")]
    public async Task<ActionResult> SaveLayoutPreferences([FromBody] LayoutPreferencesRequest req)
    {
        var userId = GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            return NotFound();

        // For now, just acknowledge the request - in production this would be stored in UserNotificationPreferences or similar table
        // This would involve creating/updating a UserNotificationPreferences record with the asset_order as JSON
        
        return Ok(new { success = true });
    }
}

public record PatchUserRequest(string? DisplayName, string? BaseCurrency, string? Theme);
public record LayoutPreferencesRequest(string[] asset_order);