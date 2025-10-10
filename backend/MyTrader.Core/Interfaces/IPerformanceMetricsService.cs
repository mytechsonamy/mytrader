namespace MyTrader.Core.Interfaces;

/// <summary>
/// Service for collecting and tracking performance metrics
/// </summary>
public interface IPerformanceMetricsService
{
    /// <summary>
    /// Record a database operation metric
    /// </summary>
    void RecordDatabaseOperation(string operationName, long durationMs, bool success);

    /// <summary>
    /// Record a WebSocket operation metric
    /// </summary>
    void RecordWebSocketOperation(string operationType, long durationMs, bool success);

    /// <summary>
    /// Record an API request metric
    /// </summary>
    void RecordApiRequest(string endpoint, string method, int statusCode, long durationMs);

    /// <summary>
    /// Record memory usage snapshot
    /// </summary>
    void RecordMemoryUsage(long totalMemoryBytes, long gcMemoryBytes);

    /// <summary>
    /// Get database operation statistics
    /// </summary>
    DatabaseMetrics GetDatabaseMetrics();

    /// <summary>
    /// Get WebSocket operation statistics
    /// </summary>
    WebSocketMetrics GetWebSocketMetrics();

    /// <summary>
    /// Get API request statistics
    /// </summary>
    ApiMetrics GetApiMetrics();

    /// <summary>
    /// Get memory usage statistics
    /// </summary>
    MemoryMetrics GetMemoryMetrics();

    /// <summary>
    /// Get all metrics summary
    /// </summary>
    PerformanceMetricsSummary GetMetricsSummary();

    /// <summary>
    /// Reset all metrics
    /// </summary>
    void ResetMetrics();
}

/// <summary>
/// Database operation metrics
/// </summary>
public class DatabaseMetrics
{
    public long TotalOperations { get; set; }
    public long SuccessfulOperations { get; set; }
    public long FailedOperations { get; set; }
    public double AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, OperationStats> OperationBreakdown { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// WebSocket operation metrics
/// </summary>
public class WebSocketMetrics
{
    public long TotalConnections { get; set; }
    public long ActiveConnections { get; set; }
    public long TotalMessages { get; set; }
    public long FailedMessages { get; set; }
    public double AverageMessageProcessingMs { get; set; }
    public long MessagesPerMinute { get; set; }
    public double ConnectionSuccessRate { get; set; }
    public Dictionary<string, OperationStats> OperationBreakdown { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// API request metrics
/// </summary>
public class ApiMetrics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public long RequestsPerMinute { get; set; }
    public Dictionary<string, EndpointStats> EndpointBreakdown { get; set; } = new();
    public Dictionary<int, long> StatusCodeDistribution { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Memory usage metrics
/// </summary>
public class MemoryMetrics
{
    public long CurrentMemoryBytes { get; set; }
    public long PeakMemoryBytes { get; set; }
    public long AverageMemoryBytes { get; set; }
    public long GcMemoryBytes { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Operation statistics
/// </summary>
public class OperationStats
{
    public long Count { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public double AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public DateTime LastExecuted { get; set; }
}

/// <summary>
/// Endpoint statistics
/// </summary>
public class EndpointStats
{
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public long MinResponseTimeMs { get; set; }
    public long MaxResponseTimeMs { get; set; }
    public Dictionary<int, long> StatusCodes { get; set; } = new();
    public DateTime LastAccessed { get; set; }
}

/// <summary>
/// Performance metrics summary
/// </summary>
public class PerformanceMetricsSummary
{
    public DatabaseMetrics Database { get; set; } = new();
    public WebSocketMetrics WebSocket { get; set; } = new();
    public ApiMetrics Api { get; set; } = new();
    public MemoryMetrics Memory { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Uptime { get; set; }
}
