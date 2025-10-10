using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MyTrader.Infrastructure.Services;

public interface IDatabaseConnectionPoolManager
{
    Task<ConnectionPoolStatus> GetPoolStatusAsync();
    Task OptimizePoolAsync();
    void MonitorPoolHealth();
    event EventHandler<PoolHealthChangedEventArgs> PoolHealthChanged;
}

public class ConnectionPoolStatus
{
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int TotalConnections { get; set; }
    public int MaxPoolSize { get; set; }
    public double UtilizationPercentage { get; set; }
    public TimeSpan AverageConnectionTime { get; set; }
    public int ConnectionsCreatedPerSecond { get; set; }
    public int ConnectionsClosedPerSecond { get; set; }
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class PoolHealthChangedEventArgs : EventArgs
{
    public ConnectionPoolStatus PoolStatus { get; set; } = new();
    public string HealthChange { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DatabaseConnectionPoolManager : IDatabaseConnectionPoolManager
{
    private readonly DbContext _context;
    private readonly ILogger<DatabaseConnectionPoolManager> _logger;
    private readonly Timer _monitoringTimer;
    
    // Pool monitoring metrics
    private readonly ConcurrentQueue<DateTime> _connectionCreationTimes = new();
    private readonly ConcurrentQueue<DateTime> _connectionClosureTimes = new();
    private readonly ConcurrentQueue<TimeSpan> _connectionDurations = new();
    
    private volatile ConnectionPoolStatus _lastPoolStatus = new();
    private volatile bool _isMonitoring = false;

    public event EventHandler<PoolHealthChangedEventArgs>? PoolHealthChanged;

    public DatabaseConnectionPoolManager(DbContext context, ILogger<DatabaseConnectionPoolManager> logger)
    {
        _context = context;
        _logger = logger;
        
        // Start monitoring timer (every 30 seconds)
        _monitoringTimer = new Timer(MonitorPoolHealthCallback, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("Database Connection Pool Manager initialized");
    }

    public async Task<ConnectionPoolStatus> GetPoolStatusAsync()
    {
        try
        {
            var status = new ConnectionPoolStatus();
            
            // Skip detailed monitoring for in-memory database
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                status.IsHealthy = true;
                status.Status = "InMemory";
                status.MaxPoolSize = 1;
                status.TotalConnections = 1;
                status.ActiveConnections = 1;
                status.UtilizationPercentage = 100;
                return status;
            }

            // Get connection string to extract pool settings
            var connectionString = _context.Database.GetConnectionString();
            if (!string.IsNullOrEmpty(connectionString))
            {
                status.MaxPoolSize = ExtractMaxPoolSizeFromConnectionString(connectionString);
            }

            // Estimate current pool usage (this is approximate since EF Core doesn't expose detailed pool metrics)
            var connectionTest = await TestConnectionPoolAsync();
            status.IsHealthy = connectionTest.IsSuccessful;
            status.AverageConnectionTime = connectionTest.ConnectionTime;
            
            // Calculate metrics from recent activity
            status.ConnectionsCreatedPerSecond = CalculateConnectionsPerSecond(_connectionCreationTimes);
            status.ConnectionsClosedPerSecond = CalculateConnectionsPerSecond(_connectionClosureTimes);
            
            // Estimate utilization based on recent activity
            status.UtilizationPercentage = CalculateUtilizationPercentage(status);
            
            status.Status = status.IsHealthy ? "Healthy" : "Degraded";
            
            // Add detailed metrics
            status.Metrics = new Dictionary<string, object>
            {
                ["LastConnectionTest"] = connectionTest.Timestamp,
                ["AverageConnectionDuration"] = GetAverageConnectionDuration(),
                ["RecentConnectionAttempts"] = _connectionCreationTimes.Count,
                ["PoolEfficiency"] = CalculatePoolEfficiency()
            };

            _lastPoolStatus = status;
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection pool status");
            
            return new ConnectionPoolStatus
            {
                IsHealthy = false,
                Status = "Error",
                Metrics = new Dictionary<string, object> { ["Error"] = ex.Message }
            };
        }
    }

    public async Task OptimizePoolAsync()
    {
        try
        {
            _logger.LogInformation("Starting connection pool optimization");
            
            // Skip optimization for in-memory database
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                _logger.LogInformation("Skipping pool optimization for in-memory database");
                return;
            }

            var currentStatus = await GetPoolStatusAsync();
            
            // Log current pool status
            _logger.LogInformation("Current pool status: Utilization={Utilization}%, Healthy={IsHealthy}, MaxSize={MaxSize}",
                currentStatus.UtilizationPercentage, currentStatus.IsHealthy, currentStatus.MaxPoolSize);

            // Perform optimization based on current metrics
            if (currentStatus.UtilizationPercentage > 80)
            {
                _logger.LogWarning("High pool utilization detected ({Utilization}%). Consider increasing pool size or optimizing queries.",
                    currentStatus.UtilizationPercentage);
            }
            
            if (currentStatus.AverageConnectionTime > TimeSpan.FromSeconds(5))
            {
                _logger.LogWarning("Slow connection times detected (avg: {AvgTime}ms). Check network and database performance.",
                    currentStatus.AverageConnectionTime.TotalMilliseconds);
            }

            // Clear old metrics to prevent memory buildup
            ClearOldMetrics();
            
            _logger.LogInformation("Connection pool optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection pool optimization");
        }
    }

    public void MonitorPoolHealth()
    {
        if (_isMonitoring)
            return;

        _isMonitoring = true;
        
        _ = Task.Run(async () =>
        {
            try
            {
                var status = await GetPoolStatusAsync();
                
                // Check for health changes
                if (status.IsHealthy != _lastPoolStatus.IsHealthy)
                {
                    var healthChange = status.IsHealthy ? "Pool health improved" : "Pool health degraded";
                    
                    _logger.LogInformation("Connection pool health changed: {HealthChange}", healthChange);
                    
                    PoolHealthChanged?.Invoke(this, new PoolHealthChangedEventArgs
                    {
                        PoolStatus = status,
                        HealthChange = healthChange
                    });
                }
                
                _lastPoolStatus = status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring pool health");
            }
            finally
            {
                _isMonitoring = false;
            }
        });
    }

    private void MonitorPoolHealthCallback(object? state)
    {
        MonitorPoolHealth();
    }

    private async Task<(bool IsSuccessful, TimeSpan ConnectionTime, DateTime Timestamp)> TestConnectionPoolAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow;
        
        try
        {
            // Simple connection test
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            
            stopwatch.Stop();
            var connectionTime = stopwatch.Elapsed;
            
            // Record successful connection
            _connectionCreationTimes.Enqueue(timestamp);
            _connectionDurations.Enqueue(connectionTime);
            
            return (true, connectionTime, timestamp);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Connection pool test failed in {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return (false, stopwatch.Elapsed, timestamp);
        }
    }

    private int ExtractMaxPoolSizeFromConnectionString(string connectionString)
    {
        try
        {
            // Parse PostgreSQL connection string for Max Pool Size
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    var value = keyValue[1].Trim();
                    
                    if (key == "maximum pool size" || key == "max pool size" || key == "maxpoolsize")
                    {
                        if (int.TryParse(value, out var maxPoolSize))
                        {
                            return maxPoolSize;
                        }
                    }
                }
            }
            
            // Default PostgreSQL pool size
            return 100;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing connection string for pool size");
            return 100; // Default fallback
        }
    }

    private int CalculateConnectionsPerSecond(ConcurrentQueue<DateTime> timeQueue)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-60); // Last minute
        var recentConnections = 0;
        
        // Count connections in the last minute
        foreach (var time in timeQueue)
        {
            if (time > cutoff)
                recentConnections++;
        }
        
        return recentConnections / 60; // Per second average
    }

    private double CalculateUtilizationPercentage(ConnectionPoolStatus status)
    {
        if (status.MaxPoolSize <= 0)
            return 0;

        // Estimate based on recent activity and connection times
        var recentActivity = _connectionCreationTimes.Count;
        var estimatedActiveConnections = Math.Min(recentActivity / 10, status.MaxPoolSize);
        
        return (double)estimatedActiveConnections / status.MaxPoolSize * 100;
    }

    private TimeSpan GetAverageConnectionDuration()
    {
        var durations = _connectionDurations.ToArray();
        if (durations.Length == 0)
            return TimeSpan.Zero;
            
        var averageTicks = durations.Average(d => d.Ticks);
        return TimeSpan.FromTicks((long)averageTicks);
    }

    private double CalculatePoolEfficiency()
    {
        var avgDuration = GetAverageConnectionDuration();
        if (avgDuration == TimeSpan.Zero)
            return 100;

        // Efficiency based on connection speed (faster = more efficient)
        var targetConnectionTime = TimeSpan.FromMilliseconds(100); // 100ms target
        var efficiency = Math.Max(0, 100 - (avgDuration.TotalMilliseconds / targetConnectionTime.TotalMilliseconds * 100));
        
        return Math.Min(100, efficiency);
    }

    private void ClearOldMetrics()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10); // Keep last 10 minutes
        
        // Clear old connection creation times
        while (_connectionCreationTimes.TryPeek(out var time) && time < cutoff)
        {
            _connectionCreationTimes.TryDequeue(out _);
        }
        
        // Clear old connection closure times
        while (_connectionClosureTimes.TryPeek(out var time) && time < cutoff)
        {
            _connectionClosureTimes.TryDequeue(out _);
        }
        
        // Keep only last 100 connection durations
        while (_connectionDurations.Count > 100)
        {
            _connectionDurations.TryDequeue(out _);
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}