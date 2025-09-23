using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.DTOs;
using System.Net;
using System.Text.Json;

namespace MyTrader.Core.Services;

/// <summary>
/// Comprehensive error handling and retry mechanism for Yahoo Finance data operations
/// Provides exponential backoff, circuit breaker, and dead letter queue patterns
/// </summary>
public class YahooFinanceErrorHandlingService
{
    private readonly ILogger<YahooFinanceErrorHandlingService> _logger;
    private readonly ErrorHandlingConfiguration _config;
    private readonly Dictionary<string, CircuitBreakerState> _circuitBreakers = new();
    private readonly Dictionary<string, RetryContext> _retryContexts = new();
    private readonly object _lockObject = new();

    public YahooFinanceErrorHandlingService(
        ILogger<YahooFinanceErrorHandlingService> logger,
        IOptions<ErrorHandlingConfiguration> configuration)
    {
        _logger = logger;
        _config = configuration.Value;
    }

    /// <summary>
    /// Execute operation with comprehensive error handling and retry logic
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        string operationKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) where T : class
    {
        var circuitBreakerKey = GetCircuitBreakerKey(operationKey);

        // Check circuit breaker state
        if (IsCircuitBreakerOpen(circuitBreakerKey))
        {
            _logger.LogWarning("Circuit breaker is open for operation {OperationKey}, rejecting request", operationKey);
            throw new CircuitBreakerOpenException($"Circuit breaker is open for {operationKey}");
        }

        var retryContext = GetRetryContext(operationKey);
        var attempt = 0;
        var maxAttempts = _config.MaxRetryAttempts;

        while (attempt < maxAttempts)
        {
            attempt++;
            retryContext.TotalAttempts++;

            try
            {
                _logger.LogDebug("Executing operation {OperationKey}, attempt {Attempt}/{MaxAttempts}",
                    operationKey, attempt, maxAttempts);

                var result = await operation(cancellationToken);

                // Reset retry context on success
                ResetRetryContext(operationKey);

                // Record success in circuit breaker
                RecordCircuitBreakerSuccess(circuitBreakerKey);

                _logger.LogDebug("Operation {OperationKey} completed successfully on attempt {Attempt}",
                    operationKey, attempt);

                return result;
            }
            catch (Exception ex)
            {
                var errorInfo = AnalyzeError(ex);
                retryContext.LastError = errorInfo;

                _logger.LogWarning(ex, "Operation {OperationKey} failed on attempt {Attempt}/{MaxAttempts}. " +
                    "Error type: {ErrorType}, Retryable: {IsRetryable}",
                    operationKey, attempt, maxAttempts, errorInfo.ErrorType, errorInfo.IsRetryable);

                // Record failure in circuit breaker
                RecordCircuitBreakerFailure(circuitBreakerKey, errorInfo);

                // Check if we should retry
                if (!errorInfo.IsRetryable || attempt >= maxAttempts)
                {
                    // Send to dead letter queue if configured
                    if (_config.EnableDeadLetterQueue)
                    {
                        await SendToDeadLetterQueueAsync(operationKey, errorInfo, retryContext, cancellationToken);
                    }

                    _logger.LogError(ex, "Operation {OperationKey} failed permanently after {Attempts} attempts",
                        operationKey, attempt);

                    throw new OperationFailedException(operationKey, attempt, ex);
                }

                // Calculate delay before retry
                var delay = CalculateRetryDelay(attempt, errorInfo);

                _logger.LogInformation("Retrying operation {OperationKey} in {Delay}ms (attempt {Attempt}/{MaxAttempts})",
                    operationKey, delay.TotalMilliseconds, attempt, maxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Retry loop completed unexpectedly");
    }

    /// <summary>
    /// Execute batch operation with error isolation per item
    /// </summary>
    public async Task<BatchOperationResult<T>> ExecuteBatchWithErrorHandlingAsync<T>(
        string operationKey,
        IEnumerable<string> items,
        Func<string, CancellationToken, Task<T>> itemOperation,
        CancellationToken cancellationToken = default) where T : class
    {
        var result = new BatchOperationResult<T>
        {
            OperationKey = operationKey,
            StartTime = DateTime.UtcNow
        };

        var semaphore = new SemaphoreSlim(_config.MaxConcurrentOperations, _config.MaxConcurrentOperations);
        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var itemKey = $"{operationKey}:{item}";
                var itemResult = await ExecuteWithRetryAsync(itemKey, ct => itemOperation(item, ct), cancellationToken);

                lock (result)
                {
                    result.SuccessfulItems[item] = itemResult;
                }
            }
            catch (Exception ex)
            {
                lock (result)
                {
                    result.FailedItems[item] = ex.Message;
                }

                _logger.LogError(ex, "Batch item {Item} failed in operation {OperationKey}", item, operationKey);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;
        result.TotalItems = items.Count();
        result.SuccessfulCount = result.SuccessfulItems.Count;
        result.FailedCount = result.FailedItems.Count;
        result.SuccessRate = result.TotalItems > 0 ? (decimal)result.SuccessfulCount / result.TotalItems * 100 : 100;

        _logger.LogInformation("Batch operation {OperationKey} completed. " +
            "Success: {SuccessCount}/{TotalCount} ({SuccessRate:F1}%), Duration: {Duration}",
            operationKey, result.SuccessfulCount, result.TotalItems, result.SuccessRate, result.Duration);

        return result;
    }

    /// <summary>
    /// Get current error statistics for monitoring
    /// </summary>
    public ErrorStatistics GetErrorStatistics()
    {
        lock (_lockObject)
        {
            var stats = new ErrorStatistics
            {
                CircuitBreakers = _circuitBreakers.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new CircuitBreakerInfo
                    {
                        State = kvp.Value.State,
                        FailureCount = kvp.Value.FailureCount,
                        LastFailureTime = kvp.Value.LastFailureTime,
                        LastSuccessTime = kvp.Value.LastSuccessTime
                    }),
                RetryContexts = _retryContexts.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new RetryInfo
                    {
                        TotalAttempts = kvp.Value.TotalAttempts,
                        LastErrorType = kvp.Value.LastError?.ErrorType,
                        LastErrorTime = kvp.Value.LastError?.Timestamp
                    })
            };

            return stats;
        }
    }

    private ErrorInfo AnalyzeError(Exception exception)
    {
        var errorInfo = new ErrorInfo
        {
            Exception = exception,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case HttpRequestException httpEx:
                errorInfo.ErrorType = ErrorType.Network;
                errorInfo.IsRetryable = true;
                errorInfo.RetryAfter = TimeSpan.FromSeconds(30);
                break;

            case TaskCanceledException timeoutEx when timeoutEx.InnerException is TimeoutException:
                errorInfo.ErrorType = ErrorType.Timeout;
                errorInfo.IsRetryable = true;
                errorInfo.RetryAfter = TimeSpan.FromSeconds(60);
                break;

            case WebException webEx:
                errorInfo.ErrorType = ErrorType.Http;
                errorInfo.IsRetryable = IsRetryableHttpError(webEx);
                errorInfo.RetryAfter = GetRetryAfterFromException(webEx);
                break;

            case JsonException:
                errorInfo.ErrorType = ErrorType.DataFormat;
                errorInfo.IsRetryable = false; // Data format errors are usually not retryable
                break;

            case UnauthorizedAccessException:
                errorInfo.ErrorType = ErrorType.Authentication;
                errorInfo.IsRetryable = false; // Auth errors need manual intervention
                break;

            case ArgumentException:
            case InvalidOperationException:
                errorInfo.ErrorType = ErrorType.Configuration;
                errorInfo.IsRetryable = false; // Config errors need fixes
                break;

            case CircuitBreakerOpenException:
                errorInfo.ErrorType = ErrorType.CircuitBreakerOpen;
                errorInfo.IsRetryable = false; // Circuit breaker will handle retry timing
                break;

            default:
                errorInfo.ErrorType = ErrorType.Unknown;
                errorInfo.IsRetryable = false; // Conservative approach for unknown errors
                break;
        }

        return errorInfo;
    }

    private bool IsRetryableHttpError(WebException webException)
    {
        if (webException.Response is HttpWebResponse httpResponse)
        {
            var statusCode = httpResponse.StatusCode;

            // Retryable HTTP status codes
            return statusCode == HttpStatusCode.InternalServerError ||
                   statusCode == HttpStatusCode.BadGateway ||
                   statusCode == HttpStatusCode.ServiceUnavailable ||
                   statusCode == HttpStatusCode.GatewayTimeout ||
                   statusCode == HttpStatusCode.TooManyRequests ||
                   statusCode == HttpStatusCode.RequestTimeout;
        }

        return true; // Network-level errors are generally retryable
    }

    private TimeSpan GetRetryAfterFromException(WebException webException)
    {
        if (webException.Response is HttpWebResponse httpResponse)
        {
            var retryAfterHeader = httpResponse.Headers["Retry-After"];
            if (!string.IsNullOrEmpty(retryAfterHeader) && int.TryParse(retryAfterHeader, out var seconds))
            {
                return TimeSpan.FromSeconds(Math.Min(seconds, _config.MaxRetryDelay.TotalSeconds));
            }
        }

        return TimeSpan.FromSeconds(30); // Default retry after
    }

    private TimeSpan CalculateRetryDelay(int attempt, ErrorInfo errorInfo)
    {
        // Use error-specific delay if available
        if (errorInfo.RetryAfter.HasValue)
        {
            return errorInfo.RetryAfter.Value;
        }

        // Exponential backoff with jitter
        var baseDelay = TimeSpan.FromMilliseconds(_config.BaseRetryDelayMs);
        var exponentialDelay = TimeSpan.FromMilliseconds(
            baseDelay.TotalMilliseconds * Math.Pow(_config.ExponentialBackoffMultiplier, attempt - 1));

        // Add jitter to avoid thundering herd
        var jitter = TimeSpan.FromMilliseconds(
            Random.Shared.NextDouble() * _config.JitterMaxMs);

        var totalDelay = exponentialDelay + jitter;

        // Cap at maximum delay
        return totalDelay > _config.MaxRetryDelay ? _config.MaxRetryDelay : totalDelay;
    }

    private string GetCircuitBreakerKey(string operationKey)
    {
        // Group operations by base operation type for circuit breaker
        var parts = operationKey.Split(':');
        return parts[0]; // Use the base operation name
    }

    private bool IsCircuitBreakerOpen(string circuitBreakerKey)
    {
        lock (_lockObject)
        {
            if (!_circuitBreakers.TryGetValue(circuitBreakerKey, out var state))
            {
                return false;
            }

            if (state.State == CircuitBreakerStateEnum.Open)
            {
                // Check if we should try to half-open
                if (DateTime.UtcNow - state.LastFailureTime > _config.CircuitBreakerTimeout)
                {
                    state.State = CircuitBreakerStateEnum.HalfOpen;
                    _logger.LogInformation("Circuit breaker {Key} moved to half-open state", circuitBreakerKey);
                    return false;
                }
                return true;
            }

            return false;
        }
    }

    private void RecordCircuitBreakerSuccess(string circuitBreakerKey)
    {
        lock (_lockObject)
        {
            if (_circuitBreakers.TryGetValue(circuitBreakerKey, out var state))
            {
                state.FailureCount = 0;
                state.LastSuccessTime = DateTime.UtcNow;

                if (state.State == CircuitBreakerStateEnum.HalfOpen)
                {
                    state.State = CircuitBreakerStateEnum.Closed;
                    _logger.LogInformation("Circuit breaker {Key} closed after successful operation", circuitBreakerKey);
                }
            }
        }
    }

    private void RecordCircuitBreakerFailure(string circuitBreakerKey, ErrorInfo errorInfo)
    {
        lock (_lockObject)
        {
            if (!_circuitBreakers.TryGetValue(circuitBreakerKey, out var state))
            {
                state = new CircuitBreakerState { State = CircuitBreakerStateEnum.Closed };
                _circuitBreakers[circuitBreakerKey] = state;
            }

            state.FailureCount++;
            state.LastFailureTime = DateTime.UtcNow;

            // Open circuit breaker if failure threshold reached
            if (state.FailureCount >= _config.CircuitBreakerFailureThreshold &&
                state.State == CircuitBreakerStateEnum.Closed)
            {
                state.State = CircuitBreakerStateEnum.Open;
                _logger.LogWarning("Circuit breaker {Key} opened after {FailureCount} failures",
                    circuitBreakerKey, state.FailureCount);
            }
        }
    }

    private RetryContext GetRetryContext(string operationKey)
    {
        lock (_lockObject)
        {
            if (!_retryContexts.TryGetValue(operationKey, out var context))
            {
                context = new RetryContext();
                _retryContexts[operationKey] = context;
            }
            return context;
        }
    }

    private void ResetRetryContext(string operationKey)
    {
        lock (_lockObject)
        {
            _retryContexts.Remove(operationKey);
        }
    }

    private async Task SendToDeadLetterQueueAsync(
        string operationKey,
        ErrorInfo errorInfo,
        RetryContext retryContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var deadLetterEntry = new DeadLetterEntry
            {
                OperationKey = operationKey,
                ErrorType = errorInfo.ErrorType,
                ErrorMessage = errorInfo.Exception?.Message ?? "Unknown error",
                StackTrace = errorInfo.Exception?.StackTrace,
                TotalAttempts = retryContext.TotalAttempts,
                FirstFailureTime = retryContext.FirstFailureTime,
                LastFailureTime = errorInfo.Timestamp,
                Timestamp = DateTime.UtcNow
            };

            // In a real implementation, this would send to a message queue or database
            _logger.LogError("Dead letter queue entry: {DeadLetterEntry}",
                JsonSerializer.Serialize(deadLetterEntry, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send entry to dead letter queue for operation {OperationKey}", operationKey);
        }
    }
}

/// <summary>
/// Configuration for error handling service
/// </summary>
public class ErrorHandlingConfiguration
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int BaseRetryDelayMs { get; set; } = 1000;
    public double ExponentialBackoffMultiplier { get; set; } = 2.0;
    public int JitterMaxMs { get; set; } = 500;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public bool EnableDeadLetterQueue { get; set; } = true;
    public int MaxConcurrentOperations { get; set; } = 10;
}

/// <summary>
/// Error information analysis result
/// </summary>
public class ErrorInfo
{
    public Exception? Exception { get; set; }
    public ErrorType ErrorType { get; set; }
    public bool IsRetryable { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Error types for categorization
/// </summary>
public enum ErrorType
{
    Unknown,
    Network,
    Http,
    Timeout,
    Authentication,
    Authorization,
    RateLimit,
    DataFormat,
    Configuration,
    CircuitBreakerOpen
}

/// <summary>
/// Circuit breaker state tracking
/// </summary>
public class CircuitBreakerState
{
    public CircuitBreakerStateEnum State { get; set; } = CircuitBreakerStateEnum.Closed;
    public int FailureCount { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime LastSuccessTime { get; set; }
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitBreakerStateEnum
{
    Closed,
    Open,
    HalfOpen
}

/// <summary>
/// Retry context for tracking attempts
/// </summary>
public class RetryContext
{
    public int TotalAttempts { get; set; }
    public DateTime FirstFailureTime { get; set; } = DateTime.UtcNow;
    public ErrorInfo? LastError { get; set; }
}

/// <summary>
/// Batch operation result
/// </summary>
public class BatchOperationResult<T>
{
    public string OperationKey { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }

    public int TotalItems { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public decimal SuccessRate { get; set; }

    public Dictionary<string, T> SuccessfulItems { get; set; } = new();
    public Dictionary<string, string> FailedItems { get; set; } = new();
}

/// <summary>
/// Error statistics for monitoring
/// </summary>
public class ErrorStatistics
{
    public Dictionary<string, CircuitBreakerInfo> CircuitBreakers { get; set; } = new();
    public Dictionary<string, RetryInfo> RetryContexts { get; set; } = new();
}

/// <summary>
/// Circuit breaker information for monitoring
/// </summary>
public class CircuitBreakerInfo
{
    public CircuitBreakerStateEnum State { get; set; }
    public int FailureCount { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime LastSuccessTime { get; set; }
}

/// <summary>
/// Retry information for monitoring
/// </summary>
public class RetryInfo
{
    public int TotalAttempts { get; set; }
    public ErrorType? LastErrorType { get; set; }
    public DateTime? LastErrorTime { get; set; }
}

/// <summary>
/// Dead letter queue entry
/// </summary>
public class DeadLetterEntry
{
    public string OperationKey { get; set; } = string.Empty;
    public ErrorType ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public int TotalAttempts { get; set; }
    public DateTime FirstFailureTime { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Custom exceptions
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}

public class OperationFailedException : Exception
{
    public string OperationKey { get; }
    public int AttemptCount { get; }

    public OperationFailedException(string operationKey, int attemptCount, Exception innerException)
        : base($"Operation {operationKey} failed after {attemptCount} attempts", innerException)
    {
        OperationKey = operationKey;
        AttemptCount = attemptCount;
    }
}