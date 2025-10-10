namespace MyTrader.Core.Interfaces;

/// <summary>
/// Enhanced logger with structured logging and correlation ID support
/// </summary>
public interface IEnhancedLogger
{
    /// <summary>
    /// Log an error with correlation ID and context
    /// </summary>
    void LogError(
        Exception exception,
        string message,
        string? correlationId = null,
        Dictionary<string, object>? context = null);

    /// <summary>
    /// Log a warning with context
    /// </summary>
    void LogWarning(
        string message,
        string? correlationId = null,
        Dictionary<string, object>? context = null);

    /// <summary>
    /// Log information with context
    /// </summary>
    void LogInformation(
        string message,
        string? correlationId = null,
        Dictionary<string, object>? context = null);

    /// <summary>
    /// Log debug information
    /// </summary>
    void LogDebug(
        string message,
        string? correlationId = null,
        Dictionary<string, object>? context = null);

    /// <summary>
    /// Log a business event (for analytics)
    /// </summary>
    void LogBusinessEvent(
        string eventName,
        string? userId = null,
        Dictionary<string, object>? properties = null);

    /// <summary>
    /// Log a performance metric
    /// </summary>
    void LogPerformance(
        string operationName,
        long durationMs,
        bool success,
        Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Log a security event
    /// </summary>
    void LogSecurityEvent(
        string eventType,
        string? userId = null,
        string? ipAddress = null,
        Dictionary<string, object>? details = null);
}

/// <summary>
/// Log context for structured logging
/// </summary>
public class LogContext
{
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? Operation { get; set; }
    public string? Component { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
