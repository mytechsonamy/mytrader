using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Infrastructure.Data;
using System.Collections.Concurrent;

namespace MyTrader.Infrastructure.Services;

public interface IEnhancedDbConnectionManager
{
    Task<bool> EnsureConnectionAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3);
    Task ApplyPendingMigrationsAsync();
    bool IsHealthy { get; }
    event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
    ConnectionHealthStatus GetHealthStatus();
}

public class ConnectionStatusChangedEventArgs : EventArgs
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ConnectionHealthStatus
{
    public bool IsHealthy { get; set; }
    public DateTime LastSuccessfulConnection { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public int ConsecutiveFailures { get; set; }
    public TimeSpan Uptime { get; set; }
    public string Status { get; set; } = "Unknown"; // "Connected", "Reconnecting", "Failed", "CircuitOpen"
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class EnhancedDbConnectionManager : IEnhancedDbConnectionManager
{
    private readonly TradingDbContext _context;
    private readonly ILogger<EnhancedDbConnectionManager> _logger;
    private readonly IDatabaseRetryPolicyService _retryPolicyService;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly Timer _healthCheckTimer;
    private readonly object _timeLock = new();
    
    private volatile bool _isHealthy = false;
    private volatile int _consecutiveFailures = 0;
    private DateTime _lastSuccessfulConnection = DateTime.MinValue;
    private DateTime _lastHeartbeat = DateTime.MinValue;
    private readonly DateTime _startTime = DateTime.UtcNow;
    
    // Circuit breaker state
    private volatile CircuitBreakerState _circuitState = CircuitBreakerState.Closed;
    private DateTime _circuitOpenedAt = DateTime.MinValue;
    private readonly TimeSpan _circuitOpenTimeout = TimeSpan.FromMinutes(1);
    private readonly int _failureThreshold = 5;
    private readonly int _successThreshold = 3;
    private volatile int _successCount = 0;
    
    // Performance metrics
    private readonly ConcurrentQueue<TimeSpan> _recentOperationTimes = new();
    private readonly ConcurrentDictionary<string, long> _operationCounts = new();

    public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

    public bool IsHealthy => _isHealthy && _circuitState != CircuitBreakerState.Open;

    public EnhancedDbConnectionManager(
        TradingDbContext context, 
        ILogger<EnhancedDbConnectionManager> logger,
        IDatabaseRetryPolicyService retryPolicyService)
    {
        _context = context;
        _logger = logger;
        _retryPolicyService = retryPolicyService;
        _connectionSemaphore = new SemaphoreSlim(1, 1);
        
        // Start health check timer (every 30 seconds)
        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("Enhanced Database Connection Manager initialized");
    }

    public async Task<bool> EnsureConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_circuitState == CircuitBreakerState.Open)
        {
            DateTime circuitOpenedAt;
            lock (_timeLock)
            {
                circuitOpenedAt = _circuitOpenedAt;
            }
            
            if (DateTime.UtcNow - circuitOpenedAt < _circuitOpenTimeout)
            {
                _logger.LogWarning("Circuit breaker is open, rejecting connection attempt");
                return false;
            }
            
            // Transition to half-open to test connection
            _circuitState = CircuitBreakerState.HalfOpen;
            _successCount = 0;
            _logger.LogInformation("Circuit breaker transitioning to half-open state");
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Check if database is in-memory (skip connection tests)
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                _isHealthy = true;
                _lastSuccessfulConnection = DateTime.UtcNow;
                _lastHeartbeat = DateTime.UtcNow;
                _consecutiveFailures = 0;
                RecordSuccess();
                return true;
            }

            // Test database connection
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            
            var operationTime = DateTime.UtcNow - startTime;
            RecordOperationTime(operationTime);
            
            _isHealthy = true;
            lock (_timeLock)
            {
                _lastSuccessfulConnection = DateTime.UtcNow;
                _lastHeartbeat = DateTime.UtcNow;
            }
            _consecutiveFailures = 0;
            
            RecordSuccess();
            
            _logger.LogDebug("Database connection verified successfully in {Duration}ms", operationTime.TotalMilliseconds);
            
            OnConnectionStatusChanged(true, "Connected", null);
            return true;
        }
        catch (Exception ex)
        {
            _isHealthy = false;
            _consecutiveFailures++;
            
            RecordFailure();
            
            _logger.LogError(ex, "Database connection failed (attempt {ConsecutiveFailures})", _consecutiveFailures);
            
            OnConnectionStatusChanged(false, "Failed", ex.Message);
            return false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
    {
        if (!IsHealthy)
        {
            await EnsureConnectionAsync();
        }

        if (_circuitState == CircuitBreakerState.Open)
        {
            throw new InvalidOperationException("Circuit breaker is open - database operations are temporarily disabled");
        }

        // Use the dedicated retry policy service for better retry logic
        return await _retryPolicyService.ExecuteAsync(async () =>
        {
            IncrementOperationCount("ExecuteWithRetry");
            
            var startTime = DateTime.UtcNow;
            var result = await operation();
            var operationTime = DateTime.UtcNow - startTime;
            
            RecordOperationTime(operationTime);
            
            // Operation succeeded
            _consecutiveFailures = 0;
            RecordSuccess();
            
            _logger.LogDebug("Database operation completed successfully in {Duration}ms", 
                operationTime.TotalMilliseconds);
            
            return result;
        }, "EnhancedDbOperation");
    }

    public async Task ApplyPendingMigrationsAsync()
    {
        try
        {
            _logger.LogInformation("Checking for pending database migrations");
            
            // Skip migrations for in-memory database
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("In-memory database ensured created");
                return;
            }

            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                
                await _context.Database.MigrateAsync();
                
                _logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                _logger.LogInformation("No pending migrations found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    public ConnectionHealthStatus GetHealthStatus()
    {
        DateTime lastSuccessfulConnection, lastHeartbeat, circuitOpenedAt;
        lock (_timeLock)
        {
            lastSuccessfulConnection = _lastSuccessfulConnection;
            lastHeartbeat = _lastHeartbeat;
            circuitOpenedAt = _circuitOpenedAt;
        }

        return new ConnectionHealthStatus
        {
            IsHealthy = IsHealthy,
            LastSuccessfulConnection = lastSuccessfulConnection,
            LastHeartbeat = lastHeartbeat,
            ConsecutiveFailures = _consecutiveFailures,
            Uptime = DateTime.UtcNow - _startTime,
            Status = _circuitState switch
            {
                CircuitBreakerState.Open => "CircuitOpen",
                CircuitBreakerState.HalfOpen => "Reconnecting",
                _ => _isHealthy ? "Connected" : "Failed"
            },
            Metrics = new Dictionary<string, object>
            {
                ["CircuitState"] = _circuitState.ToString(),
                ["AverageOperationTime"] = GetAverageOperationTime(),
                ["OperationCounts"] = _operationCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["RecentOperations"] = _recentOperationTimes.Count
            }
        };
    }

    private void PerformHealthCheck(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await EnsureConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed");
            }
        });
    }

    private void RecordSuccess()
    {
        if (_circuitState == CircuitBreakerState.HalfOpen)
        {
            _successCount++;
            if (_successCount >= _successThreshold)
            {
                _circuitState = CircuitBreakerState.Closed;
                _logger.LogInformation("Circuit breaker closed - connection restored");
            }
        }
    }

    private void RecordFailure()
    {
        if (_circuitState == CircuitBreakerState.Closed && _consecutiveFailures >= _failureThreshold)
        {
            _circuitState = CircuitBreakerState.Open;
            lock (_timeLock)
            {
                _circuitOpenedAt = DateTime.UtcNow;
            }
            _logger.LogWarning("Circuit breaker opened due to {FailureCount} consecutive failures", _consecutiveFailures);
        }
    }

    private void RecordOperationTime(TimeSpan duration)
    {
        _recentOperationTimes.Enqueue(duration);
        
        // Keep only last 100 operation times
        while (_recentOperationTimes.Count > 100)
        {
            _recentOperationTimes.TryDequeue(out _);
        }
    }

    private void IncrementOperationCount(string operationType)
    {
        _operationCounts.AddOrUpdate(operationType, 1, (key, value) => value + 1);
    }

    private double GetAverageOperationTime()
    {
        var times = _recentOperationTimes.ToArray();
        return times.Length > 0 ? times.Average(t => t.TotalMilliseconds) : 0;
    }

    private void OnConnectionStatusChanged(bool isHealthy, string status, string? errorMessage)
    {
        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
        {
            IsHealthy = isHealthy,
            Status = status,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        });
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _connectionSemaphore?.Dispose();
    }
}

public enum CircuitBreakerState
{
    Closed,    // Normal operation
    Open,      // Failing fast
    HalfOpen   // Testing if service recovered
}