using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IHealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Get overall platform health status
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        try
        {
            var healthResult = await _healthCheckService.CheckHealthAsync(cancellationToken);

            // Return appropriate HTTP status code based on health
            var statusCode = healthResult.Status switch
            {
                "Healthy" => 200,
                "Degraded" => 200, // Still operational
                "Unhealthy" => 503, // Service Unavailable
                _ => 500
            };

            return StatusCode(statusCode, healthResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check endpoint failed");
            return StatusCode(500, new
            {
                isHealthy = false,
                status = "Unhealthy",
                message = "Health check failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get database health status
    /// </summary>
    [HttpGet("database")]
    [Authorize]
    public async Task<IActionResult> GetDatabaseHealth(CancellationToken cancellationToken)
    {
        try
        {
            var healthStatus = await _healthCheckService.CheckDatabaseHealthAsync(cancellationToken);
            
            var statusCode = healthStatus.IsHealthy ? 200 : 503;
            return StatusCode(statusCode, healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get WebSocket health status
    /// </summary>
    [HttpGet("websocket")]
    [Authorize]
    public async Task<IActionResult> GetWebSocketHealth(CancellationToken cancellationToken)
    {
        try
        {
            var healthStatus = await _healthCheckService.CheckWebSocketHealthAsync(cancellationToken);
            
            var statusCode = healthStatus.IsHealthy ? 200 : 503;
            return StatusCode(statusCode, healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket health check failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get SignalR health status
    /// </summary>
    [HttpGet("signalr")]
    [Authorize]
    public async Task<IActionResult> GetSignalRHealth(CancellationToken cancellationToken)
    {
        try
        {
            var healthStatus = await _healthCheckService.CheckSignalRHealthAsync(cancellationToken);
            
            var statusCode = healthStatus.IsHealthy ? 200 : 503;
            return StatusCode(statusCode, healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR health check failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get memory health status
    /// </summary>
    [HttpGet("memory")]
    [Authorize]
    public async Task<IActionResult> GetMemoryHealth(CancellationToken cancellationToken)
    {
        try
        {
            var healthStatus = await _healthCheckService.CheckMemoryHealthAsync(cancellationToken);
            
            var statusCode = healthStatus.IsHealthy ? 200 : 503;
            return StatusCode(statusCode, healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Simple liveness probe for container orchestration
    /// </summary>
    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult GetLiveness()
    {
        return Ok(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Readiness probe - checks if service is ready to accept traffic
    /// </summary>
    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken)
    {
        try
        {
            // Check critical components for readiness
            var dbHealth = await _healthCheckService.CheckDatabaseHealthAsync(cancellationToken);
            
            if (!dbHealth.IsHealthy)
            {
                return StatusCode(503, new
                {
                    status = "not_ready",
                    reason = "Database not available",
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new
            {
                status = "not_ready",
                reason = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
