using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Controller for performance metrics and monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly IPerformanceMetricsService _metricsService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IPerformanceMetricsService metricsService,
        ILogger<MetricsController> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all performance metrics summary
    /// </summary>
    [HttpGet("summary")]
    public IActionResult GetMetricsSummary()
    {
        try
        {
            var summary = _metricsService.GetMetricsSummary();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics summary");
            return StatusCode(500, new { error = "Failed to get metrics summary" });
        }
    }

    /// <summary>
    /// Get database operation metrics
    /// </summary>
    [HttpGet("database")]
    public IActionResult GetDatabaseMetrics()
    {
        try
        {
            var metrics = _metricsService.GetDatabaseMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database metrics");
            return StatusCode(500, new { error = "Failed to get database metrics" });
        }
    }

    /// <summary>
    /// Get WebSocket operation metrics
    /// </summary>
    [HttpGet("websocket")]
    public IActionResult GetWebSocketMetrics()
    {
        try
        {
            var metrics = _metricsService.GetWebSocketMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WebSocket metrics");
            return StatusCode(500, new { error = "Failed to get WebSocket metrics" });
        }
    }

    /// <summary>
    /// Get API request metrics
    /// </summary>
    [HttpGet("api")]
    public IActionResult GetApiMetrics()
    {
        try
        {
            var metrics = _metricsService.GetApiMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API metrics");
            return StatusCode(500, new { error = "Failed to get API metrics" });
        }
    }

    /// <summary>
    /// Get memory usage metrics
    /// </summary>
    [HttpGet("memory")]
    public IActionResult GetMemoryMetrics()
    {
        try
        {
            var metrics = _metricsService.GetMemoryMetrics();
            
            // Add current snapshot
            var currentMemory = GC.GetTotalMemory(false);
            _metricsService.RecordMemoryUsage(currentMemory, currentMemory);
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory metrics");
            return StatusCode(500, new { error = "Failed to get memory metrics" });
        }
    }

    /// <summary>
    /// Reset all metrics (admin only)
    /// </summary>
    [HttpPost("reset")]
    public IActionResult ResetMetrics()
    {
        try
        {
            _metricsService.ResetMetrics();
            _logger.LogInformation("Performance metrics reset");
            
            return Ok(new
            {
                message = "Metrics reset successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting metrics");
            return StatusCode(500, new { error = "Failed to reset metrics" });
        }
    }
}
