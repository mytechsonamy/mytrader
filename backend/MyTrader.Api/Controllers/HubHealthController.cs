using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Controller for SignalR hub health monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HubHealthController : ControllerBase
{
    private readonly IHubCoordinationService _hubCoordination;
    private readonly ILogger<HubHealthController> _logger;

    public HubHealthController(
        IHubCoordinationService hubCoordination,
        ILogger<HubHealthController> logger)
    {
        _hubCoordination = hubCoordination;
        _logger = logger;
    }

    /// <summary>
    /// Get health status for all hubs
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllHubsHealth()
    {
        try
        {
            var activeHubs = await _hubCoordination.GetActiveHubsAsync();
            var hubHealths = new List<object>();

            foreach (var hubName in activeHubs)
            {
                var stats = await _hubCoordination.GetHubStatsAsync(hubName);
                hubHealths.Add(new
                {
                    hubName = hubName,
                    totalConnections = stats.TotalConnections,
                    totalGroups = stats.TotalGroups,
                    lastActivity = stats.LastActivity,
                    isHealthy = stats.TotalConnections >= 0, // Basic health check
                    groupMemberCounts = stats.GroupMemberCounts
                });
            }

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                totalHubs = activeHubs.Count,
                hubs = hubHealths
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hub health status");
            return StatusCode(500, new { error = "Failed to get hub health status" });
        }
    }

    /// <summary>
    /// Get health status for a specific hub
    /// </summary>
    [HttpGet("{hubName}")]
    public async Task<IActionResult> GetHubHealth(string hubName)
    {
        try
        {
            var stats = await _hubCoordination.GetHubStatsAsync(hubName);
            
            return Ok(new
            {
                hubName = hubName,
                totalConnections = stats.TotalConnections,
                totalGroups = stats.TotalGroups,
                lastActivity = stats.LastActivity,
                isHealthy = stats.TotalConnections >= 0,
                groupMemberCounts = stats.GroupMemberCounts,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health status for hub {HubName}", hubName);
            return StatusCode(500, new { error = $"Failed to get health status for hub {hubName}" });
        }
    }

    /// <summary>
    /// Get connection details for a specific hub
    /// </summary>
    [HttpGet("{hubName}/connections")]
    public async Task<IActionResult> GetHubConnections(string hubName)
    {
        try
        {
            var connections = await _hubCoordination.GetHubConnectionsAsync(hubName);
            
            return Ok(new
            {
                hubName = hubName,
                connectionCount = connections.Count,
                connections = connections,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connections for hub {HubName}", hubName);
            return StatusCode(500, new { error = $"Failed to get connections for hub {hubName}" });
        }
    }

    /// <summary>
    /// Cleanup stale connections across all hubs
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupStaleConnections([FromQuery] int maxAgeMinutes = 30)
    {
        try
        {
            var maxAge = TimeSpan.FromMinutes(maxAgeMinutes);
            await _hubCoordination.CleanupStaleConnectionsAsync(maxAge);
            
            _logger.LogInformation("Cleaned up stale connections older than {MaxAgeMinutes} minutes", maxAgeMinutes);
            
            return Ok(new
            {
                message = "Cleanup completed successfully",
                maxAgeMinutes = maxAgeMinutes,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up stale connections");
            return StatusCode(500, new { error = "Failed to cleanup stale connections" });
        }
    }
}
