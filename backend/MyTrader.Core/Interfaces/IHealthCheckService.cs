using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyTrader.Core.Interfaces;

public interface IHealthCheckService
{
    Task<PlatformHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<ComponentHealthStatus> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default);
    Task<ComponentHealthStatus> CheckWebSocketHealthAsync(CancellationToken cancellationToken = default);
    Task<ComponentHealthStatus> CheckSignalRHealthAsync(CancellationToken cancellationToken = default);
    Task<ComponentHealthStatus> CheckMemoryHealthAsync(CancellationToken cancellationToken = default);
}

public class PlatformHealthResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty; // "Healthy", "Degraded", "Unhealthy"
    public DateTime Timestamp { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, ComponentHealthStatus> Components { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ComponentHealthStatus
{
    public string ComponentName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty; // "Healthy", "Degraded", "Unhealthy"
    public string? Message { get; set; }
    public DateTime LastChecked { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
