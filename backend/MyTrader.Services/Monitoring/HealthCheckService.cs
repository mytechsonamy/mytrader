using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Interfaces;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Services.Monitoring;

public class HealthCheckService : IHealthCheckService
{
    private readonly TradingDbContext _dbContext;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IPerformanceMetricsService _metricsService;
    
    // Health thresholds
    private const int DatabaseTimeoutMs = 5000;
    private const long MemoryWarningThresholdMb = 800;
    private const long MemoryCriticalThresholdMb = 1000;
    private const double WebSocketUptimeWarningThreshold = 0.90; // 90%
    private const double WebSocketUptimeCriticalThreshold = 0.80; // 80%

    public HealthCheckService(
        TradingDbContext dbContext,
        ILogger<HealthCheckService> logger,
        IPerformanceMetricsService metricsService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<PlatformHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new PlatformHealthResult
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Check all components in parallel
            var tasks = new[]
            {
                CheckDatabaseHealthAsync(cancellationToken),
                CheckWebSocketHealthAsync(cancellationToken),
                CheckSignalRHealthAsync(cancellationToken),
                CheckMemoryHealthAsync(cancellationToken)
            };

            var componentResults = await Task.WhenAll(tasks);

            foreach (var componentResult in componentResults)
            {
                result.Components[componentResult.ComponentName] = componentResult;
            }

            // Determine overall health status
            var unhealthyComponents = componentResults.Count(c => !c.IsHealthy);
            var degradedComponents = componentResults.Count(c => c.Status == "Degraded");

            if (unhealthyComponents > 0)
            {
                result.IsHealthy = false;
                result.Status = "Unhealthy";
            }
            else if (degradedComponents > 0)
            {
                result.IsHealthy = true;
                result.Status = "Degraded";
            }
            else
            {
                result.IsHealthy = true;
                result.Status = "Healthy";
            }

            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;

            result.Metadata["total_components"] = componentResults.Length;
            result.Metadata["healthy_components"] = componentResults.Count(c => c.IsHealthy);
            result.Metadata["unhealthy_components"] = unhealthyComponents;
            result.Metadata["degraded_components"] = degradedComponents;

            _logger.LogInformation(
                "Health check completed: Status={Status}, ResponseTime={ResponseTime}ms, Healthy={Healthy}/{Total}",
                result.Status,
                result.ResponseTime.TotalMilliseconds,
                result.Metadata["healthy_components"],
                result.Metadata["total_components"]);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.Status = "Unhealthy";
            result.ResponseTime = stopwatch.Elapsed;
            result.Metadata["error"] = ex.Message;

            _logger.LogError(ex, "Health check failed with exception");
        }

        return result;
    }

    public async Task<ComponentHealthStatus> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new ComponentHealthStatus
        {
            ComponentName = "Database",
            LastChecked = DateTime.UtcNow
        };

        try
        {
            // Test database connectivity with timeout
            using var timeoutCts = new CancellationTokenSource(DatabaseTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var canConnect = await _dbContext.Database.CanConnectAsync(linkedCts.Token);
            
            if (!canConnect)
            {
                status.IsHealthy = false;
                status.Status = "Unhealthy";
                status.Message = "Cannot connect to database";
                status.Errors.Add("Database connection failed");
                return status;
            }

            // Check for pending migrations
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(linkedCts.Token);
            var pendingCount = pendingMigrations.Count();

            if (pendingCount > 0)
            {
                status.Warnings.Add($"{pendingCount} pending migration(s) detected");
                status.Metrics["pending_migrations"] = pendingCount;
            }

            // Get database metrics from performance service
            var dbMetrics = _metricsService.GetDatabaseMetrics();
            
            stopwatch.Stop();
            status.ResponseTime = stopwatch.Elapsed;
            status.IsHealthy = true;
            status.Status = pendingCount > 0 ? "Degraded" : "Healthy";
            status.Message = pendingCount > 0 
                ? $"Connected with {pendingCount} pending migration(s)" 
                : "Connected and healthy";

            // Add metrics
            status.Metrics["connection_test_time_ms"] = stopwatch.Elapsed.TotalMilliseconds;
            status.Metrics["total_operations"] = dbMetrics.TotalOperations;
            status.Metrics["successful_operations"] = dbMetrics.SuccessfulOperations;
            status.Metrics["failed_operations"] = dbMetrics.FailedOperations;
            status.Metrics["average_duration_ms"] = dbMetrics.AverageDurationMs;
            
            if (dbMetrics.FailedOperations > 0)
            {
                var failureRate = (double)dbMetrics.FailedOperations / dbMetrics.TotalOperations;
                status.Metrics["failure_rate"] = failureRate;
                
                if (failureRate > 0.1) // More than 10% failure rate
                {
                    status.Warnings.Add($"High database failure rate: {failureRate:P1}");
                    status.Status = "Degraded";
                }
            }

            _logger.LogDebug("Database health check: {Status}, ResponseTime={ResponseTime}ms", 
                status.Status, status.ResponseTime.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            status.IsHealthy = false;
            status.Status = "Unhealthy";
            status.Message = $"Database health check timed out after {DatabaseTimeoutMs}ms";
            status.ResponseTime = stopwatch.Elapsed;
            status.Errors.Add("Health check timeout");

            _logger.LogWarning("Database health check timed out after {Timeout}ms", DatabaseTimeoutMs);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            status.IsHealthy = false;
            status.Status = "Unhealthy";
            status.Message = $"Database health check failed: {ex.Message}";
            status.ResponseTime = stopwatch.Elapsed;
            status.Errors.Add(ex.Message);

            _logger.LogError(ex, "Database health check failed");
        }

        return status;
    }

    public async Task<ComponentHealthStatus> CheckWebSocketHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new ComponentHealthStatus
        {
            ComponentName = "WebSocket",
            LastChecked = DateTime.UtcNow
        };

        try
        {
            // Get WebSocket metrics from performance service
            var wsMetrics = _metricsService.GetWebSocketMetrics();
            
            stopwatch.Stop();
            status.ResponseTime = stopwatch.Elapsed;

            // Calculate success rate as uptime indicator
            var uptimePercentage = wsMetrics.ConnectionSuccessRate;

            status.Metrics["active_connections"] = wsMetrics.ActiveConnections;
            status.Metrics["total_connections"] = wsMetrics.TotalConnections;
            status.Metrics["total_messages"] = wsMetrics.TotalMessages;
            status.Metrics["failed_messages"] = wsMetrics.FailedMessages;
            status.Metrics["uptime_percentage"] = uptimePercentage;
            status.Metrics["messages_per_minute"] = wsMetrics.MessagesPerMinute;
            status.Metrics["average_message_processing_ms"] = wsMetrics.AverageMessageProcessingMs;

            // Determine health status based on uptime
            if (wsMetrics.ActiveConnections == 0 && wsMetrics.TotalConnections > 0)
            {
                status.IsHealthy = false;
                status.Status = "Unhealthy";
                status.Message = "No active WebSocket connections";
                status.Errors.Add("All WebSocket connections are disconnected");
            }
            else if (uptimePercentage < WebSocketUptimeCriticalThreshold)
            {
                status.IsHealthy = false;
                status.Status = "Unhealthy";
                status.Message = $"WebSocket uptime critically low: {uptimePercentage:P1}";
                status.Errors.Add($"Uptime below critical threshold ({WebSocketUptimeCriticalThreshold:P0})");
            }
            else if (uptimePercentage < WebSocketUptimeWarningThreshold)
            {
                status.IsHealthy = true;
                status.Status = "Degraded";
                status.Message = $"WebSocket uptime below target: {uptimePercentage:P1}";
                status.Warnings.Add($"Uptime below warning threshold ({WebSocketUptimeWarningThreshold:P0})");
            }
            else
            {
                status.IsHealthy = true;
                status.Status = "Healthy";
                status.Message = $"WebSocket connections healthy, uptime: {uptimePercentage:P1}";
            }

            // Check for high message failure rate
            if (wsMetrics.TotalMessages > 0)
            {
                var failureRate = (double)wsMetrics.FailedMessages / wsMetrics.TotalMessages;
                if (failureRate > 0.2) // More than 20% failure rate
                {
                    status.Warnings.Add($"High WebSocket message failure rate: {failureRate:P1}");
                    if (status.Status == "Healthy")
                    {
                        status.Status = "Degraded";
                    }
                }
            }

            _logger.LogDebug("WebSocket health check: {Status}, Uptime={Uptime:P1}, Active={Active}", 
                status.Status, uptimePercentage, wsMetrics.ActiveConnections);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            status.IsHealthy = false;
            status.Status = "Unhealthy";
            status.Message = $"WebSocket health check failed: {ex.Message}";
            status.ResponseTime = stopwatch.Elapsed;
            status.Errors.Add(ex.Message);

            _logger.LogError(ex, "WebSocket health check failed");
        }

        await Task.CompletedTask;
        return status;
    }

    public async Task<ComponentHealthStatus> CheckSignalRHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new ComponentHealthStatus
        {
            ComponentName = "SignalR",
            LastChecked = DateTime.UtcNow
        };

        try
        {
            // Get API metrics which include SignalR hub activity
            var apiMetrics = _metricsService.GetApiMetrics();
            
            stopwatch.Stop();
            status.ResponseTime = stopwatch.Elapsed;

            // SignalR hubs are part of the API, so we check API health
            status.IsHealthy = true;
            status.Status = "Healthy";
            status.Message = "SignalR hubs operational";

            status.Metrics["total_requests"] = apiMetrics.TotalRequests;
            status.Metrics["successful_requests"] = apiMetrics.SuccessfulRequests;
            status.Metrics["failed_requests"] = apiMetrics.FailedRequests;
            status.Metrics["average_response_time_ms"] = apiMetrics.AverageResponseTimeMs;

            // Check for high error rate
            if (apiMetrics.TotalRequests > 0)
            {
                var errorRate = (double)apiMetrics.FailedRequests / apiMetrics.TotalRequests;
                status.Metrics["error_rate"] = errorRate;

                if (errorRate > 0.1) // More than 10% error rate
                {
                    status.Warnings.Add($"High SignalR error rate: {errorRate:P1}");
                    status.Status = "Degraded";
                }
            }

            // Check for slow response times
            if (apiMetrics.AverageResponseTimeMs > 1000)
            {
                status.Warnings.Add($"Slow average response time: {apiMetrics.AverageResponseTimeMs:F0}ms");
                status.Status = "Degraded";
            }

            _logger.LogDebug("SignalR health check: {Status}, AvgResponseTime={ResponseTime}ms", 
                status.Status, apiMetrics.AverageResponseTimeMs);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            status.IsHealthy = false;
            status.Status = "Unhealthy";
            status.Message = $"SignalR health check failed: {ex.Message}";
            status.ResponseTime = stopwatch.Elapsed;
            status.Errors.Add(ex.Message);

            _logger.LogError(ex, "SignalR health check failed");
        }

        await Task.CompletedTask;
        return status;
    }

    public async Task<ComponentHealthStatus> CheckMemoryHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new ComponentHealthStatus
        {
            ComponentName = "Memory",
            LastChecked = DateTime.UtcNow
        };

        try
        {
            // Get memory metrics from performance service
            var memoryMetrics = _metricsService.GetMemoryMetrics();
            
            stopwatch.Stop();
            status.ResponseTime = stopwatch.Elapsed;

            var currentMemoryMb = memoryMetrics.CurrentMemoryBytes / (1024.0 * 1024.0);
            var peakMemoryMb = memoryMetrics.PeakMemoryBytes / (1024.0 * 1024.0);
            var averageMemoryMb = memoryMetrics.AverageMemoryBytes / (1024.0 * 1024.0);
            
            status.Metrics["current_memory_mb"] = currentMemoryMb;
            status.Metrics["peak_memory_mb"] = peakMemoryMb;
            status.Metrics["average_memory_mb"] = averageMemoryMb;
            status.Metrics["gc_memory_mb"] = memoryMetrics.GcMemoryBytes / (1024.0 * 1024.0);
            status.Metrics["gc_gen0_collections"] = memoryMetrics.Gen0Collections;
            status.Metrics["gc_gen1_collections"] = memoryMetrics.Gen1Collections;
            status.Metrics["gc_gen2_collections"] = memoryMetrics.Gen2Collections;

            // Determine health status based on memory usage
            if (currentMemoryMb >= MemoryCriticalThresholdMb)
            {
                status.IsHealthy = false;
                status.Status = "Unhealthy";
                status.Message = $"Memory usage critical: {currentMemoryMb:F0}MB";
                status.Errors.Add($"Memory usage exceeds critical threshold ({MemoryCriticalThresholdMb}MB)");
            }
            else if (currentMemoryMb >= MemoryWarningThresholdMb)
            {
                status.IsHealthy = true;
                status.Status = "Degraded";
                status.Message = $"Memory usage elevated: {currentMemoryMb:F0}MB";
                status.Warnings.Add($"Memory usage exceeds warning threshold ({MemoryWarningThresholdMb}MB)");
            }
            else
            {
                status.IsHealthy = true;
                status.Status = "Healthy";
                status.Message = $"Memory usage normal: {currentMemoryMb:F0}MB";
            }

            // Check for excessive Gen2 collections (potential memory pressure)
            if (memoryMetrics.Gen2Collections > 100)
            {
                status.Warnings.Add($"High Gen2 GC collections: {memoryMetrics.Gen2Collections}");
            }

            _logger.LogDebug("Memory health check: {Status}, Current={Current}MB, Peak={Peak}MB", 
                status.Status, currentMemoryMb, peakMemoryMb);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            status.IsHealthy = false;
            status.Status = "Unhealthy";
            status.Message = $"Memory health check failed: {ex.Message}";
            status.ResponseTime = stopwatch.Elapsed;
            status.Errors.Add(ex.Message);

            _logger.LogError(ex, "Memory health check failed");
        }

        await Task.CompletedTask;
        return status;
    }
}
