using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace MyTrader.Infrastructure.Services;

public interface IDatabaseRetryPolicyService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "DatabaseOperation");
    Task ExecuteAsync(Func<Task> operation, string operationName = "DatabaseOperation");
    bool IsTransientError(Exception exception);
    TimeSpan CalculateDelay(int attemptNumber);
}

public class DatabaseRetryPolicyConfiguration
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public List<Type> RetriableExceptions { get; set; } = new()
    {
        typeof(DbException),
        typeof(TimeoutException),
        typeof(InvalidOperationException),
        typeof(TaskCanceledException)
    };
}

public class DatabaseRetryPolicyService : IDatabaseRetryPolicyService
{
    private readonly DatabaseRetryPolicyConfiguration _config;
    private readonly ILogger<DatabaseRetryPolicyService> _logger;
    
    // Known transient error patterns
    private readonly HashSet<string> _transientErrorMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        "timeout",
        "connection",
        "network",
        "temporary",
        "deadlock",
        "lock timeout",
        "connection pool",
        "server is not available",
        "transport-level error",
        "broken pipe",
        "connection reset"
    };

    private readonly HashSet<int> _transientSqlErrorNumbers = new()
    {
        2,      // Timeout
        53,     // Network path not found
        121,    // Semaphore timeout
        1205,   // Deadlock victim
        1222,   // Lock request timeout
        8645,   // Timeout waiting for memory resource
        8651,   // Low memory condition
        40197,  // Service has encountered an error processing your request
        40501,  // Service is currently busy
        40613,  // Database is currently unavailable
        49918,  // Cannot process request. Not enough resources to process request
        49919,  // Cannot process create or update request. Too many create or update operations in progress
        49920   // Cannot process request. Too many operations in progress
    };

    public DatabaseRetryPolicyService(
        DatabaseRetryPolicyConfiguration? config = null,
        ILogger<DatabaseRetryPolicyService>? logger = null)
    {
        _config = config ?? new DatabaseRetryPolicyConfiguration();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseRetryPolicyService>.Instance;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "DatabaseOperation")
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= _config.MaxRetries)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var result = await operation();
                var duration = DateTime.UtcNow - startTime;

                if (attempt > 0)
                {
                    _logger.LogInformation("Database operation '{OperationName}' succeeded on attempt {Attempt} after {Duration}ms",
                        operationName, attempt + 1, duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogDebug("Database operation '{OperationName}' completed in {Duration}ms",
                        operationName, duration.TotalMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (!IsTransientError(ex))
                {
                    _logger.LogError(ex, "Non-transient error in database operation '{OperationName}' - not retrying", operationName);
                    throw;
                }

                if (attempt > _config.MaxRetries)
                {
                    _logger.LogError(ex, "Database operation '{OperationName}' failed after {MaxRetries} attempts", 
                        operationName, _config.MaxRetries + 1);
                    break;
                }

                var delay = CalculateDelay(attempt);
                _logger.LogWarning(ex, "Database operation '{OperationName}' failed on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms. Error: {ErrorMessage}",
                    operationName, attempt, _config.MaxRetries + 1, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay);
            }
        }

        throw lastException ?? new InvalidOperationException($"Database operation '{operationName}' failed after all retries");
    }

    public async Task ExecuteAsync(Func<Task> operation, string operationName = "DatabaseOperation")
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true; // Dummy return value
        }, operationName);
    }

    public bool IsTransientError(Exception exception)
    {
        // Check exception type
        if (_config.RetriableExceptions.Any(type => type.IsAssignableFrom(exception.GetType())))
        {
            // Additional checks for specific exception types
            switch (exception)
            {
                case DbException dbEx:
                    return IsTransientDbException(dbEx);
                
                case InvalidOperationException invOpEx:
                    return IsTransientInvalidOperationException(invOpEx);
                
                case TaskCanceledException:
                case TimeoutException:
                    return true;
                
                default:
                    return IsTransientByMessage(exception.Message);
            }
        }

        // Check inner exceptions
        if (exception.InnerException != null)
        {
            return IsTransientError(exception.InnerException);
        }

        return false;
    }

    public TimeSpan CalculateDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
            return TimeSpan.Zero;

        // Exponential backoff with jitter
        var baseDelayMs = _config.BaseDelay.TotalMilliseconds;
        var exponentialDelay = baseDelayMs * Math.Pow(_config.BackoffMultiplier, attemptNumber - 1);
        
        // Add jitter (±25% randomization)
        var random = new Random();
        var jitter = 1.0 + (random.NextDouble() - 0.5) * 0.5; // 0.75 to 1.25
        var delayWithJitter = exponentialDelay * jitter;
        
        // Cap at maximum delay
        var finalDelay = Math.Min(delayWithJitter, _config.MaxDelay.TotalMilliseconds);
        
        return TimeSpan.FromMilliseconds(finalDelay);
    }

    private bool IsTransientDbException(DbException dbException)
    {
        // Check SQL error numbers (for SQL Server, PostgreSQL has different error codes)
        if (dbException.Data.Contains("SqlState") || dbException.Data.Contains("ErrorCode"))
        {
            // PostgreSQL error codes
            var sqlState = dbException.Data["SqlState"]?.ToString();
            if (!string.IsNullOrEmpty(sqlState))
            {
                return IsTransientPostgreSqlState(sqlState);
            }
        }

        // Check error message for transient patterns
        return IsTransientByMessage(dbException.Message);
    }

    private bool IsTransientPostgreSqlState(string sqlState)
    {
        // PostgreSQL transient error states
        var transientStates = new HashSet<string>
        {
            "08000", // Connection exception
            "08003", // Connection does not exist
            "08006", // Connection failure
            "08001", // SQL client unable to establish SQL connection
            "08004", // SQL server rejected establishment of SQL connection
            "40001", // Serialization failure
            "40P01", // Deadlock detected
            "53000", // Insufficient resources
            "53100", // Disk full
            "53200", // Out of memory
            "53300", // Too many connections
            "57P01", // Admin shutdown
            "57P02", // Crash shutdown
            "57P03", // Cannot connect now
        };

        return transientStates.Contains(sqlState);
    }

    private bool IsTransientInvalidOperationException(InvalidOperationException exception)
    {
        var message = exception.Message.ToLowerInvariant();
        
        // Common EF Core transient error patterns
        var transientPatterns = new[]
        {
            "the connection is broken",
            "connection pool",
            "timeout expired",
            "transport-level error",
            "existing connection was forcibly closed",
            "an existing connection was forcibly closed by the remote host"
        };

        return transientPatterns.Any(pattern => message.Contains(pattern));
    }

    private bool IsTransientByMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return false;

        var lowerMessage = message.ToLowerInvariant();
        return _transientErrorMessages.Any(pattern => lowerMessage.Contains(pattern));
    }
}

public static class DatabaseRetryPolicyExtensions
{
    public static IServiceCollection AddDatabaseRetryPolicy(
        this IServiceCollection services,
        Action<DatabaseRetryPolicyConfiguration>? configureOptions = null)
    {
        var config = new DatabaseRetryPolicyConfiguration();
        configureOptions?.Invoke(config);

        services.AddSingleton(config);
        // MUST BE SINGLETON because ResilientWebSocketManager (which uses this) is singleton
        services.AddSingleton<IDatabaseRetryPolicyService, DatabaseRetryPolicyService>();

        return services;
    }
}