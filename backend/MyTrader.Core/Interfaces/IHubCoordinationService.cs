namespace MyTrader.Core.Interfaces;

/// <summary>
/// Service for coordinating message routing and subscription management across SignalR hubs
/// </summary>
public interface IHubCoordinationService
{
    /// <summary>
    /// Register a hub connection
    /// </summary>
    Task RegisterConnectionAsync(string hubName, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregister a hub connection
    /// </summary>
    Task UnregisterConnectionAsync(string hubName, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a connection to a group with hub-specific naming
    /// </summary>
    Task AddToGroupAsync(string hubName, string connectionId, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a connection from a group
    /// </summary>
    Task RemoveFromGroupAsync(string hubName, string connectionId, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all groups for a connection
    /// </summary>
    Task<List<string>> GetConnectionGroupsAsync(string hubName, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections in a hub
    /// </summary>
    Task<List<string>> GetHubConnectionsAsync(string hubName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get hub-specific group name to prevent conflicts
    /// </summary>
    string GetHubGroupName(string hubName, string groupName);

    /// <summary>
    /// Check if a connection is registered
    /// </summary>
    Task<bool> IsConnectionRegisteredAsync(string hubName, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connection statistics for a hub
    /// </summary>
    Task<HubConnectionStats> GetHubStatsAsync(string hubName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active hubs
    /// </summary>
    Task<List<string>> GetActiveHubsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up stale connections
    /// </summary>
    Task CleanupStaleConnectionsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}

/// <summary>
/// Hub connection statistics
/// </summary>
public class HubConnectionStats
{
    public string HubName { get; set; } = string.Empty;
    public int TotalConnections { get; set; }
    public int TotalGroups { get; set; }
    public Dictionary<string, int> GroupMemberCounts { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}
