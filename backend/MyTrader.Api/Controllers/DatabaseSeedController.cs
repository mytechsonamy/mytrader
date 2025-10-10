using Microsoft.AspNetCore.Mvc;
using MyTrader.Api.Services;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseSeedController : ControllerBase
{
    private readonly DatabaseSeederService _seederService;
    private readonly ILogger<DatabaseSeedController> _logger;

    public DatabaseSeedController(DatabaseSeederService seederService, ILogger<DatabaseSeedController> logger)
    {
        _seederService = seederService;
        _logger = logger;
    }

    /// <summary>
    /// Seed the database with asset classes, markets, and symbols
    /// </summary>
    [HttpPost("seed-all")]
    public async Task<IActionResult> SeedDatabase()
    {
        try
        {
            _logger.LogInformation("Starting database seed operation");
            await _seederService.SeedAllDataAsync();
            return Ok(new { Message = "Database seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed database");
            return StatusCode(500, new { Message = "Failed to seed database", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get database counts for verification
    /// </summary>
    [HttpGet("counts")]
    public async Task<IActionResult> GetDatabaseCounts()
    {
        try
        {
            var counts = await _seederService.GetDatabaseCountsAsync();
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database counts");
            return StatusCode(500, new { Message = "Failed to get database counts", Error = ex.Message });
        }
    }
}