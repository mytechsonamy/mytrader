using Microsoft.AspNetCore.SignalR;
using MyTrader.Core.Interfaces;
using System.Diagnostics;

namespace MyTrader.Api.Hubs;

/// <summary>
/// Base hub class with automatic subscription cleanup, coordination, and error handling
/// </summary>
public abstract class BaseHub : Hub
{
    protected readonly ILogger Logger;
    protected readonly IHubCoordinationService? HubCoordination;
    protected abstract string HubName { get; }
    
    // Error tracking
    private int _errorCount = 0;
    private DateTime _lastErrorTime = DateTime.MinValue;
    private const int MaxErrorsPerMinute = 10;

    protected BaseHub(
        ILogger logger,
        IHubCoordinationService? hubCoordination = null)
    {
        Logger = logger;
        HubCoordination = hubCoordination;
    }

    public override async Task OnConnectedAsync()
    {
        Logger.LogInformation("Client connected to {HubName}: {ConnectionId}", HubName, Context.ConnectionId);

        // Register connection with coordination service
        if (HubCoordination != null)
        {
            await HubCoordination.RegisterConnectionAsync(HubName, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Logger.LogInformation(
            "Client disconnected from {HubName}: {ConnectionId}, Exception: {Exception}",
            HubName, Context.ConnectionId, exception?.Message);

        // Automatic cleanup: Unregister connection and remove from all groups
        if (HubCoordination != null)
        {
            try
            {
                // Get all groups this connection was in
                var groups = await HubCoordination.GetConnectionGroupsAsync(HubName, Context.ConnectionId);
                
                Logger.LogDebug(
                    "Cleaning up {GroupCount} group subscriptions for connection {ConnectionId} in hub {HubName}",
                    groups.Count, Context.ConnectionId, HubName);

                // Remove from all groups
                foreach (var group in groups)
                {
                    try
                    {
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
                        await HubCoordination.RemoveFromGroupAsync(HubName, Context.ConnectionId, group);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex,
                            "Error removing connection {ConnectionId} from group {GroupName}",
                            Context.ConnectionId, group);
                    }
                }

                // Unregister connection
                await HubCoordination.UnregisterConnectionAsync(HubName, Context.ConnectionId);
                
                Logger.LogInformation(
                    "Successfully cleaned up connection {ConnectionId} from {HubName}",
                    Context.ConnectionId, HubName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Error during automatic cleanup for connection {ConnectionId} in hub {HubName}",
                    Context.ConnectionId, HubName);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Helper method to add connection to a group with tracking
    /// </summary>
    protected async Task AddToTrackedGroupAsync(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        if (HubCoordination != null)
        {
            await HubCoordination.AddToGroupAsync(HubName, Context.ConnectionId, groupName);
        }
        
        Logger.LogDebug(
            "Added connection {ConnectionId} to group {GroupName} in hub {HubName}",
            Context.ConnectionId, groupName, HubName);
    }

    /// <summary>
    /// Helper method to remove connection from a group with tracking
    /// </summary>
    protected async Task RemoveFromTrackedGroupAsync(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        if (HubCoordination != null)
        {
            await HubCoordination.RemoveFromGroupAsync(HubName, Context.ConnectionId, groupName);
        }
        
        Logger.LogDebug(
            "Removed connection {ConnectionId} from group {GroupName} in hub {HubName}",
            Context.ConnectionId, groupName, HubName);
    }

    /// <summary>
    /// Get all groups this connection is subscribed to
    /// </summary>
    protected async Task<List<string>> GetConnectionGroupsAsync()
    {
        if (HubCoordination != null)
        {
            return await HubCoordination.GetConnectionGroupsAsync(HubName, Context.ConnectionId);
        }
        
        return new List<string>();
    }

    /// <summary>
    /// Health check method for all hubs
    /// </summary>
    public async Task Ping()
    {
        try
        {
            await Clients.Caller.SendAsync("Pong", new
            {
                hubName = HubName,
                timestamp = DateTime.UtcNow,
                connectionId = Context.ConnectionId,
                status = "healthy"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error responding to ping from client {ConnectionId} in hub {HubName}",
                Context.ConnectionId, HubName);
        }
    }

    /// <summary>
    /// Get subscription info for debugging
    /// </summary>
    public async Task GetSubscriptionInfo()
    {
        try
        {
            var groups = await GetConnectionGroupsAsync();
            
            await Clients.Caller.SendAsync("SubscriptionInfo", new
            {
                hubName = HubName,
                connectionId = Context.ConnectionId,
                groups = groups,
                groupCount = groups.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting subscription info for connection {ConnectionId} in hub {HubName}",
                Context.ConnectionId, HubName);
        }
    }

    /// <summary>
    /// Execute an action with error handling and isolation
    /// </summary>
    protected async Task<T?> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> action,
        string operationName,
        T? defaultValue = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Check error rate
            if (IsErrorRateTooHigh())
            {
                Logger.LogWarning(
                    "Error rate too high for hub {HubName}, operation {OperationName} throttled",
                    HubName, operationName);
                
                await SendErrorToCallerAsync(
                    "RateLimitExceeded",
                    "Too many errors, please try again later",
                    operationName);
                
                return defaultValue;
            }

            var result = await action();
            
            stopwatch.Stop();
            Logger.LogDebug(
                "Operation {OperationName} completed in {ElapsedMs}ms for hub {HubName}",
                operationName, stopwatch.ElapsedMilliseconds, HubName);
            
            return result;
        }
        catch (HubException)
        {
            // Re-throw hub exceptions (already handled)
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordError();
            
            Logger.LogError(ex,
                "Error in operation {OperationName} for hub {HubName}, connection {ConnectionId}. Duration: {ElapsedMs}ms",
                operationName, HubName, Context.ConnectionId, stopwatch.ElapsedMilliseconds);
            
            await SendErrorToCallerAsync(
                "OperationFailed",
                $"Operation {operationName} failed",
                operationName,
                ex.Message);
            
            return defaultValue;
        }
    }

    /// <summary>
    /// Execute an action with error handling (void return)
    /// </summary>
    protected async Task ExecuteWithErrorHandlingAsync(
        Func<Task> action,
        string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Check error rate
            if (IsErrorRateTooHigh())
            {
                Logger.LogWarning(
                    "Error rate too high for hub {HubName}, operation {OperationName} throttled",
                    HubName, operationName);
                
                await SendErrorToCallerAsync(
                    "RateLimitExceeded",
                    "Too many errors, please try again later",
                    operationName);
                
                return;
            }

            await action();
            
            stopwatch.Stop();
            Logger.LogDebug(
                "Operation {OperationName} completed in {ElapsedMs}ms for hub {HubName}",
                operationName, stopwatch.ElapsedMilliseconds, HubName);
        }
        catch (HubException)
        {
            // Re-throw hub exceptions (already handled)
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordError();
            
            Logger.LogError(ex,
                "Error in operation {OperationName} for hub {HubName}, connection {ConnectionId}. Duration: {ElapsedMs}ms",
                operationName, HubName, Context.ConnectionId, stopwatch.ElapsedMilliseconds);
            
            await SendErrorToCallerAsync(
                "OperationFailed",
                $"Operation {operationName} failed",
                operationName,
                ex.Message);
        }
    }

    /// <summary>
    /// Send error message to caller
    /// </summary>
    protected async Task SendErrorToCallerAsync(
        string errorCode,
        string message,
        string? operation = null,
        string? details = null)
    {
        try
        {
            await Clients.Caller.SendAsync("Error", new
            {
                hubName = HubName,
                errorCode = errorCode,
                message = message,
                operation = operation,
                details = details,
                timestamp = DateTime.UtcNow,
                connectionId = Context.ConnectionId
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to send error message to caller for hub {HubName}, connection {ConnectionId}",
                HubName, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Check if error rate is too high
    /// </summary>
    private bool IsErrorRateTooHigh()
    {
        var now = DateTime.UtcNow;
        
        // Reset counter if more than a minute has passed
        if ((now - _lastErrorTime).TotalMinutes >= 1)
        {
            _errorCount = 0;
            _lastErrorTime = now;
            return false;
        }
        
        return _errorCount >= MaxErrorsPerMinute;
    }

    /// <summary>
    /// Record an error occurrence
    /// </summary>
    private void RecordError()
    {
        var now = DateTime.UtcNow;
        
        // Reset counter if more than a minute has passed
        if ((now - _lastErrorTime).TotalMinutes >= 1)
        {
            _errorCount = 0;
            _lastErrorTime = now;
        }
        
        _errorCount++;
    }

    /// <summary>
    /// Get hub health status
    /// </summary>
    public async Task GetHealthStatus()
    {
        try
        {
            var stats = HubCoordination != null
                ? await HubCoordination.GetHubStatsAsync(HubName)
                : null;

            await Clients.Caller.SendAsync("HealthStatus", new
            {
                hubName = HubName,
                isHealthy = !IsErrorRateTooHigh(),
                errorCount = _errorCount,
                lastErrorTime = _lastErrorTime,
                connectionCount = stats?.TotalConnections ?? 0,
                groupCount = stats?.TotalGroups ?? 0,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error getting health status for hub {HubName}, connection {ConnectionId}",
                HubName, Context.ConnectionId);
        }
    }
}
