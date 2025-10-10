using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.HealthChecks;

/// <summary>
/// Health check for SignalR hubs
/// </summary>
public class SignalRHubHealthCheck : IHealthCheck
{
    private readonly IHubCoordinationService _hubCoordination;
    private readonly ILogger<SignalRHubHealthCheck> _logger;

    public SignalRHubHealthCheck(
        IHubCoordinationService hubCoordination,
        ILogger<SignalRHubHealthCheck> logger)
    {
        _hubCoordination = hubCoordination;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeHubs = await _hubCoordination.GetActiveHubsAsync(cancellationToken);
            var totalConnections = 0;
            var hubDetails = new Dictionary<string, object>();

            foreach (var hubName in activeHubs)
            {
                var stats = await _hubCoordination.GetHubStatsAsync(hubName, cancellationToken);
                totalConnections += stats.TotalConnections;
                
                hubDetails[hubName] = new
                {
                    connections = stats.TotalConnections,
                    groups = stats.TotalGroups,
                    lastActivity = stats.LastActivity
                };
            }

            var data = new Dictionary<string, object>
            {
                { "ActiveHubs", activeHubs.Count },
                { "TotalConnections", totalConnections },
                { "HubDetails", hubDetails }
            };

            return HealthCheckResult.Healthy(
                $"SignalR hubs healthy: {activeHubs.Count} hubs, {totalConnections} connections",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR hub health check failed");
            return HealthCheckResult.Unhealthy(
                "SignalR hub health check failed",
                ex);
        }
    }
}
