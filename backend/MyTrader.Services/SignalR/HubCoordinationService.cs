using Microsoft.Extensions.Logging;
using MyTrader.Core.Interfaces;
using System.Collections.Concurrent;

namespace MyTrader.Services.SignalR;

/// <summary>
/// Service for coordinating message routing and subscription management across SignalR hubs
/// </summary>
public class HubCoordinationService : IHubCoordinationService
{
    private readonly ILogger<HubCoordinationService> _logger;
    
    // Hub -> ConnectionId -> Groups
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>> _hubConnections;
    
    // Hub -> Last Activity
    private readonly ConcurrentDictionary<string, DateTime> _hubActivity;
    
    private readonly object _lock = new();

    public HubCoordinationService(ILogger<HubCoordinationService> logger)
    {
        _logger = logger;
        _hubConnections = new ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>>();
        _hubActivity = new ConcurrentDictionary<string, DateTime>();
    }

    public Task RegisterConnectionAsync(string hubName, string connectionId, CancellationToken cancellationToken = default)
    {
        var connections = _hubConnections.GetOrAdd(hubName, _ => new ConcurrentDictionary<string, HashSet<string>>());
        connections.TryAdd(connectionId, new HashSet<string>());
        
        _hubActivity[hubName] = DateTime.UtcNow;
        
        _logger.LogDebug("Registered connection {ConnectionId} to hub {HubName}", connectionId, hubName);
        
        return Task.CompletedTask;
    }

    public Task UnregisterConnectionAsync(string hubName, string connectionId, CancellationToken cancellationToken = default)
    {
        if (_hubConnections.TryGetValue(hubName, out var connections))
        {
            if (connections.TryRemove(connectionId, out var groups))
            {
                _logger.LogDebug(
                    "Unregistered connection {ConnectionId} from hub {HubName} (was in {GroupCount} groups)",
                    connectionId, hubName, groups.Count);
            }
            
            _hubActivity[hubName] = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }

    public Task AddToGroupAsync(string hubName, string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (_hubConnections.TryGetValue(hubName, out var connections))
        {
            if (connections.TryGetValue(connectionId, out var groups))
            {
                lock (_lock)
                {
                    groups.Add(groupName);
                }
                
                _logger.LogDebug(
                    "Added connection {ConnectionId} to group {GroupName} in hub {HubName}",
                    connectionId, groupName, hubName);
            }
            else
            {
                _logger.LogWarning(
                    "Connection {ConnectionId} not found in hub {HubName} when adding to group {GroupName}",
                    connectionId, hubName, groupName);
            }
            
            _hubActivity[hubName] = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string hubName, string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (_hubConnections.TryGetValue(hubName, out var connections))
        {
            if (connections.TryGetValue(connectionId, out var groups))
            {
                lock (_lock)
                {
                    groups.Remove(groupName);
                }
                
                _logger.LogDebug(
                    "Removed connection {ConnectionId} from group {GroupName} in hub {HubName}",
                    connectionId, groupName, hubName);
            }
            
            _hubActivity[hubName] = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }

    public Task<List<string>> GetConnectionGroupsAsync(string hubName, string connectionId, CancellationToken cancellationToken = default)
    {
        if (_hubConnections.TryGetValue(hubName, out var connections))
        {
            if (connections.TryGetValue(connectionId, out var groups))
            {
                lock (_lock)
                {
                    return Task.FromResult(groups.ToList());
                }
            }
        }
        
        return Task.FromResult(new List<string>());
    }

    public Task<List<string>> GetHubConnectionsAsync(string hubName, CancellationToken cancellationToken = default)
    {
        if (_hubConnections.TryGetValue(hubName, out var connections))
        {
            return Task.FromResult(connections.Keys.ToList());
        }
        
        return Task.FromResult(new List<string>());
    }

    public string GetHubGroupName(string hubName, string groupName)
    {
        // Prefix group names with hub name to prevent conflicts
        return $"{hubName}:{groupName}";
    }

    public Task<bool> IsConnectionRegisteredAsync(string hubName, string connectionId, CancellationToken cancellationToken = default)
    {
        if (_hubConnections.TryGetValue(hubName, out var connections))
        {
            return Task.FromResult(connections.ContainsKey(connectionId));
        }
        
        return Task.FromResult(false);
    }

    public Task<HubConnectionStats> GetHubStatsAsync(string hubName, CancellationToken cancellationToken = default)
    {
        var stats = new HubConnectionStats
        {
            HubName = hubName
        };

        if (_hubConnections.TryGetValue(hubName, out var connections))
        {
            stats.TotalConnections = connections.Count;
            
            // Count unique groups and their member counts
            var groupCounts = new Dictionary<string, int>();
            
            foreach (var connectionGroups in connections.Values)
            {
                lock (_lock)
                {
                    foreach (var group in connectionGroups)
                    {
                        if (!groupCounts.ContainsKey(group))
                        {
                            groupCounts[group] = 0;
                        }
                        groupCounts[group]++;
                    }
                }
            }
            
            stats.TotalGroups = groupCounts.Count;
            stats.GroupMemberCounts = groupCounts;
        }

        if (_hubActivity.TryGetValue(hubName, out var lastActivity))
        {
            stats.LastActivity = lastActivity;
        }

        return Task.FromResult(stats);
    }

    public Task<List<string>> GetActiveHubsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_hubConnections.Keys.ToList());
    }

    public Task CleanupStaleConnectionsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var staleHubs = new List<string>();

        foreach (var kvp in _hubActivity)
        {
            if (kvp.Value < cutoffTime)
            {
                staleHubs.Add(kvp.Key);
            }
        }

        foreach (var hubName in staleHubs)
        {
            if (_hubConnections.TryGetValue(hubName, out var connections))
            {
                var staleConnections = connections.Where(c => 
                {
                    // Consider connections stale if they have no groups
                    lock (_lock)
                    {
                        return c.Value.Count == 0;
                    }
                }).Select(c => c.Key).ToList();

                foreach (var connectionId in staleConnections)
                {
                    connections.TryRemove(connectionId, out _);
                    _logger.LogInformation(
                        "Cleaned up stale connection {ConnectionId} from hub {HubName}",
                        connectionId, hubName);
                }
            }
        }

        return Task.CompletedTask;
    }
}
