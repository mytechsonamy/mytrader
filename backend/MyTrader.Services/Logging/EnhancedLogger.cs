using Microsoft.Extensions.Logging;
using MyTrader.Core.Interfaces;

namespace MyTrader.Services.Logging;

/// <summary>
/// Enhanced logger implementation with structured logging
/// </summary>
public class EnhancedLogger : IEnhancedLogger
{
    private readonly ILogger<EnhancedLogger> _logger;

    public EnhancedLogger(ILogger<EnhancedLogger> logger)
    {
        _logger = logger;
    }

    public void LogError(
        Exception exception,
        string message,
        string? correlationId = null,
        Dictionary<string, object>? context = null)
    {
        using var scope = CreateScope(correlationId, context);
        _logger.LogError(exception, message);
    }

    public void LogWarning(
        string message,
        string? correlationId = null,
        Dictionary<string, object>? context = null)
    {
        using var scope = CreateScope(correlationId, context);
        _logger.LogWarning(message);
    }

    public void LogInformation(
        string message,
        string? correlationId = null,
        Dictionary<string, object>? context = null)
    {
        using var scope = CreateScope(correlationId, context);
        _logger.LogInformation(message);
    }

    public void LogDebug(
        string message,
        string? correlationId = null,
        Dictionary<string, object>? context = null)
    {
        using var scope = CreateScope(correlationId, context);
        _logger.LogDebug(message);
    }

    public void LogBusinessEvent(
        string eventName,
        string? userId = null,
        Dictionary<string, object>? properties = null)
    {
        var context = new Dictionary<string, object>
        {
            { "EventType", "Business" },
            { "EventName", eventName }
        };

        if (userId != null)
            context["UserId"] = userId;

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                context[prop.Key] = prop.Value;
            }
        }

        using var scope = CreateScope(null, context);
        _logger.LogInformation("Business event: {EventName}", eventName);
    }

    public void LogPerformance(
        string operationName,
        long durationMs,
        bool success,
        Dictionary<string, object>? metadata = null)
    {
        var context = new Dictionary<string, object>
        {
            { "EventType", "Performance" },
            { "Operation", operationName },
            { "DurationMs", durationMs },
            { "Success", success }
        };

        if (metadata != null)
        {
            foreach (var item in metadata)
            {
                context[item.Key] = item.Value;
            }
        }

        using var scope = CreateScope(null, context);
        
        if (durationMs > 1000) // Slow operation
        {
            _logger.LogWarning(
                "Slow operation: {Operation} took {DurationMs}ms",
                operationName, durationMs);
        }
        else
        {
            _logger.LogInformation(
                "Operation: {Operation} completed in {DurationMs}ms",
                operationName, durationMs);
        }
    }

    public void LogSecurityEvent(
        string eventType,
        string? userId = null,
        string? ipAddress = null,
        Dictionary<string, object>? details = null)
    {
        var context = new Dictionary<string, object>
        {
            { "EventType", "Security" },
            { "SecurityEventType", eventType }
        };

        if (userId != null)
            context["UserId"] = userId;

        if (ipAddress != null)
            context["IpAddress"] = ipAddress;

        if (details != null)
        {
            foreach (var detail in details)
            {
                context[detail.Key] = detail.Value;
            }
        }

        using var scope = CreateScope(null, context);
        _logger.LogWarning("Security event: {EventType}", eventType);
    }

    private IDisposable? CreateScope(string? correlationId, Dictionary<string, object>? context)
    {
        var scopeData = new Dictionary<string, object>();

        if (correlationId != null)
            scopeData["CorrelationId"] = correlationId;

        if (context != null)
        {
            foreach (var item in context)
            {
                scopeData[item.Key] = item.Value;
            }
        }

        return scopeData.Count > 0 ? _logger.BeginScope(scopeData) : null;
    }
}
