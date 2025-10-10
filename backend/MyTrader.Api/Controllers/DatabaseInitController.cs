using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Api.Services;
using MyTrader.Core.DTOs;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Controller for database initialization and health checks
/// </summary>
[ApiController]
[Route("api/database")]
[Route("api/v1/database")]
public class DatabaseInitController : ControllerBase
{
    private readonly DatabaseInitializationService _initService;
    private readonly ILogger<DatabaseInitController> _logger;

    public DatabaseInitController(
        DatabaseInitializationService initService,
        ILogger<DatabaseInitController> logger)
    {
        _initService = initService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize database with reference data and sample market data
    /// </summary>
    [HttpPost("initialize")]
    [Authorize] // Require authentication for initialization
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<string>>> InitializeDatabase()
    {
        try
        {
            _logger.LogInformation("Manual database initialization requested");

            await _initService.InitializeAsync();

            return Ok(ApiResponse<string>.SuccessResult(
                "Database initialized successfully",
                "Database has been initialized with reference data and sample market data"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to initialize database: " + ex.Message, 500));
        }
    }

    /// <summary>
    /// Get database initialization status
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous] // Allow public access for status
    [ProducesResponseType(typeof(ApiResponse<DatabaseInitializationStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<DatabaseInitializationStatus>>> GetDatabaseStatus()
    {
        try
        {
            var status = await _initService.GetStatusAsync();

            return Ok(ApiResponse<DatabaseInitializationStatus>.SuccessResult(
                status,
                status.IsHealthy
                    ? "Database is healthy and initialized"
                    : "Database has issues: " + (status.ErrorMessage ?? "Unknown issue")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database status");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to get database status: " + ex.Message, 500));
        }
    }

    /// <summary>
    /// Get database health summary for monitoring
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 503)]
    public async Task<ActionResult> GetDatabaseHealth()
    {
        try
        {
            var status = await _initService.GetStatusAsync();

            var healthInfo = new
            {
                status = status.IsHealthy ? "healthy" : "unhealthy",
                timestamp = DateTime.UtcNow,
                details = new
                {
                    symbols = status.SymbolCount,
                    marketData = status.MarketDataCount,
                    assetClasses = status.AssetClassCount,
                    markets = status.MarketCount,
                    latestData = status.LatestMarketDataTime,
                    dataFreshness = status.IsMarketDataFresh ? "fresh" : "stale"
                },
                checks = new
                {
                    hasSymbols = status.SymbolCount > 10,
                    hasMarketData = status.MarketDataCount > 0,
                    hasReferenceData = status.AssetClassCount > 3 && status.MarketCount > 3,
                    dataIsFresh = status.IsMarketDataFresh
                }
            };

            if (status.IsHealthy)
            {
                return Ok(healthInfo);
            }
            else
            {
                return StatusCode(503, healthInfo); // Service Unavailable
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Reset database (development only)
    /// </summary>
    [HttpPost("reset")]
    [Authorize] // Require authentication
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<string>>> ResetDatabase()
    {
        try
        {
            // Only allow in development environment
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            if (!isDevelopment)
            {
                return BadRequest(ApiResponse<object>.ErrorResult(
                    "Database reset is only allowed in development environment", 400));
            }

            _logger.LogWarning("Database reset requested - this will delete all data!");

            // First initialize (which will recreate tables)
            await _initService.InitializeAsync();

            return Ok(ApiResponse<string>.SuccessResult(
                "Database reset and reinitialized successfully",
                "All data has been cleared and the database has been reinitialized with fresh data"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset database");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to reset database: " + ex.Message, 500));
        }
    }

    /// <summary>
    /// Check if database initialization is required
    /// </summary>
    [HttpGet("initialization-required")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult> IsInitializationRequired()
    {
        try
        {
            var status = await _initService.GetStatusAsync();

            var initRequired = !status.IsHealthy ||
                              status.SymbolCount < 10 ||
                              status.MarketDataCount == 0;

            return Ok(new
            {
                initializationRequired = initRequired,
                reason = initRequired
                    ? (status.SymbolCount < 10 ? "Insufficient symbols" :
                       status.MarketDataCount == 0 ? "No market data" :
                       "Database not healthy")
                    : null,
                currentStatus = new
                {
                    symbols = status.SymbolCount,
                    marketData = status.MarketDataCount,
                    healthy = status.IsHealthy
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check initialization requirement");
            return Ok(new
            {
                initializationRequired = true,
                reason = "Unable to check database status: " + ex.Message
            });
        }
    }
}