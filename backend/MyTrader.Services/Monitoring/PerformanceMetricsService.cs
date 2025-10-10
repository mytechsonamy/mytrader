using System.Collections.Concurrent;
using System.Diagnostics;
using MyTrader.Core.Interfaces;

namespace MyTrader.Services.Monitoring;

/// <summary>
/// Performance metrics collection service
/// Thread-safe implementation for high-throughput scenarios
/// </summary>
public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly ConcurrentDictionary<string, OperationStats> _databaseOperations = new();
    private readonly ConcurrentDictionary<string, OperationStats> _webSocketOperations = new();
    private readonly ConcurrentDictionary<string, EndpointStats> _apiEndpoints = new();
    private readonly ConcurrentQueue<long> _memorySnapshots = new();
    
    private long _totalDbOperations;
    private long _successfulDbOperations;
    private long _totalWsOperations;
    private long _successfulWsOperations;
    private long _totalApiRequests;
    private long _successfulApiRequests;
    
    private readonly DateTime _startTime = DateTime.UtcNow;
    private long _peakMemory;
    private long _currentMemory;

    public void RecordDatabaseOperation(string operationName, long durationMs, bool success)
    {
        Interlocked.Increment(ref _totalDbOperations);
        if (success)
            Interlocked.Increment(ref _successfulDbOperations);

        _databaseOperations.AddOrUpdate(
            operationName,
            _ => new OperationStats
            {
                Count = 1,
                SuccessCount = success ? 1 : 0,
                FailureCount = success ? 0 : 1,
                AverageDurationMs = durationMs,
                MinDurationMs = durationMs,
                MaxDurationMs = durationMs,
                LastExecuted = DateTime.UtcNow
            },
            (_, existing) =>
            {
                var newCount = existing.Count + 1;
                var newAvg = ((existing.AverageDurationMs * existing.Count) + durationMs) / newCount;
                
                return new OperationStats
                {
                    Count = newCount,
                    SuccessCount = existing.SuccessCount + (success ? 1 : 0),
                    FailureCount = existing.FailureCount + (success ? 0 : 1),
                    AverageDurationMs = newAvg,
                    MinDurationMs = Math.Min(existing.MinDurationMs, durationMs),
                    MaxDurationMs = Math.Max(existing.MaxDurationMs, durationMs),
                    LastExecuted = DateTime.UtcNow
                };
            });
    }

    public void RecordWebSocketOperation(string operationType, long durationMs, bool success)
    {
        Interlocked.Increment(ref _totalWsOperations);
        if (success)
            Interlocked.Increment(ref _successfulWsOperations);

        _webSocketOperations.AddOrUpdate(
            operationType,
            _ => new OperationStats
            {
                Count = 1,
                SuccessCount = success ? 1 : 0,
                FailureCount = success ? 0 : 1,
                AverageDurationMs = durationMs,
                MinDurationMs = durationMs,
                MaxDurationMs = durationMs,
                LastExecuted = DateTime.UtcNow
            },
            (_, existing) =>
            {
                var newCount = existing.Count + 1;
                var newAvg = ((existing.AverageDurationMs * existing.Count) + durationMs) / newCount;
                
                return new OperationStats
                {
                    Count = newCount,
                    SuccessCount = existing.SuccessCount + (success ? 1 : 0),
                    FailureCount = existing.FailureCount + (success ? 0 : 1),
                    AverageDurationMs = newAvg,
                    MinDurationMs = Math.Min(existing.MinDurationMs, durationMs),
                    MaxDurationMs = Math.Max(existing.MaxDurationMs, durationMs),
                    LastExecuted = DateTime.UtcNow
                };
            });
    }

    public void RecordApiRequest(string endpoint, string method, int statusCode, long durationMs)
    {
        Interlocked.Increment(ref _totalApiRequests);
        if (statusCode >= 200 && statusCode < 300)
            Interlocked.Increment(ref _successfulApiRequests);

        var key = $"{method}:{endpoint}";
        
        _apiEndpoints.AddOrUpdate(
            key,
            _ => new EndpointStats
            {
                Endpoint = endpoint,
                Method = method,
                TotalRequests = 1,
                AverageResponseTimeMs = durationMs,
                MinResponseTimeMs = durationMs,
                MaxResponseTimeMs = durationMs,
                StatusCodes = new Dictionary<int, long> { { statusCode, 1 } },
                LastAccessed = DateTime.UtcNow
            },
            (_, existing) =>
            {
                var newCount = existing.TotalRequests + 1;
                var newAvg = ((existing.AverageResponseTimeMs * existing.TotalRequests) + durationMs) / newCount;
                
                var statusCodes = new Dictionary<int, long>(existing.StatusCodes);
                statusCodes[statusCode] = statusCodes.GetValueOrDefault(statusCode) + 1;
                
                return new EndpointStats
                {
                    Endpoint = endpoint,
                    Method = method,
                    TotalRequests = newCount,
                    AverageResponseTimeMs = newAvg,
                    MinResponseTimeMs = Math.Min(existing.MinResponseTimeMs, durationMs),
                    MaxResponseTimeMs = Math.Max(existing.MaxResponseTimeMs, durationMs),
                    StatusCodes = statusCodes,
                    LastAccessed = DateTime.UtcNow
                };
            });
    }

    public void RecordMemoryUsage(long totalMemoryBytes, long gcMemoryBytes)
    {
        Interlocked.Exchange(ref _currentMemory, totalMemoryBytes);
        
        // Update peak memory
        long currentPeak;
        do
        {
            currentPeak = Interlocked.Read(ref _peakMemory);
            if (totalMemoryBytes <= currentPeak)
                break;
        } while (Interlocked.CompareExchange(ref _peakMemory, totalMemoryBytes, currentPeak) != currentPeak);

        // Keep last 100 snapshots for average calculation
        _memorySnapshots.Enqueue(totalMemoryBytes);
        while (_memorySnapshots.Count > 100)
        {
            _memorySnapshots.TryDequeue(out _);
        }
    }

    public DatabaseMetrics GetDatabaseMetrics()
    {
        var total = Interlocked.Read(ref _totalDbOperations);
        var successful = Interlocked.Read(ref _successfulDbOperations);

        var allStats = _databaseOperations.Values.ToList();
        var avgDuration = allStats.Any() ? allStats.Average(s => s.AverageDurationMs) : 0;
        var minDuration = allStats.Any() ? allStats.Min(s => s.MinDurationMs) : 0;
        var maxDuration = allStats.Any() ? allStats.Max(s => s.MaxDurationMs) : 0;

        return new DatabaseMetrics
        {
            TotalOperations = total,
            SuccessfulOperations = successful,
            FailedOperations = total - successful,
            AverageDurationMs = avgDuration,
            MinDurationMs = minDuration,
            MaxDurationMs = maxDuration,
            SuccessRate = total > 0 ? (double)successful / total * 100 : 100,
            OperationBreakdown = new Dictionary<string, OperationStats>(_databaseOperations),
            LastUpdated = DateTime.UtcNow
        };
    }

    public WebSocketMetrics GetWebSocketMetrics()
    {
        var total = Interlocked.Read(ref _totalWsOperations);
        var successful = Interlocked.Read(ref _successfulWsOperations);

        var allStats = _webSocketOperations.Values.ToList();
        var avgDuration = allStats.Any() ? allStats.Average(s => s.AverageDurationMs) : 0;

        return new WebSocketMetrics
        {
            TotalConnections = total,
            ActiveConnections = 0, // Would need to track separately
            TotalMessages = total,
            FailedMessages = total - successful,
            AverageMessageProcessingMs = avgDuration,
            MessagesPerMinute = CalculateRatePerMinute(total),
            ConnectionSuccessRate = total > 0 ? (double)successful / total * 100 : 100,
            OperationBreakdown = new Dictionary<string, OperationStats>(_webSocketOperations),
            LastUpdated = DateTime.UtcNow
        };
    }

    public ApiMetrics GetApiMetrics()
    {
        var total = Interlocked.Read(ref _totalApiRequests);
        var successful = Interlocked.Read(ref _successfulApiRequests);

        var allEndpoints = _apiEndpoints.Values.ToList();
        var avgResponseTime = allEndpoints.Any() ? allEndpoints.Average(e => e.AverageResponseTimeMs) : 0;

        var statusCodeDist = new Dictionary<int, long>();
        foreach (var endpoint in allEndpoints)
        {
            foreach (var (code, count) in endpoint.StatusCodes)
            {
                statusCodeDist[code] = statusCodeDist.GetValueOrDefault(code) + count;
            }
        }

        return new ApiMetrics
        {
            TotalRequests = total,
            SuccessfulRequests = successful,
            FailedRequests = total - successful,
            AverageResponseTimeMs = avgResponseTime,
            RequestsPerMinute = CalculateRatePerMinute(total),
            EndpointBreakdown = new Dictionary<string, EndpointStats>(_apiEndpoints),
            StatusCodeDistribution = statusCodeDist,
            LastUpdated = DateTime.UtcNow
        };
    }

    public MemoryMetrics GetMemoryMetrics()
    {
        var snapshots = _memorySnapshots.ToArray();
        var avgMemory = snapshots.Any() ? (long)snapshots.Average() : 0;

        return new MemoryMetrics
        {
            CurrentMemoryBytes = Interlocked.Read(ref _currentMemory),
            PeakMemoryBytes = Interlocked.Read(ref _peakMemory),
            AverageMemoryBytes = avgMemory,
            GcMemoryBytes = GC.GetTotalMemory(false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            LastUpdated = DateTime.UtcNow
        };
    }

    public PerformanceMetricsSummary GetMetricsSummary()
    {
        return new PerformanceMetricsSummary
        {
            Database = GetDatabaseMetrics(),
            WebSocket = GetWebSocketMetrics(),
            Api = GetApiMetrics(),
            Memory = GetMemoryMetrics(),
            GeneratedAt = DateTime.UtcNow,
            Uptime = DateTime.UtcNow - _startTime
        };
    }

    public void ResetMetrics()
    {
        _databaseOperations.Clear();
        _webSocketOperations.Clear();
        _apiEndpoints.Clear();
        _memorySnapshots.Clear();
        
        Interlocked.Exchange(ref _totalDbOperations, 0);
        Interlocked.Exchange(ref _successfulDbOperations, 0);
        Interlocked.Exchange(ref _totalWsOperations, 0);
        Interlocked.Exchange(ref _successfulWsOperations, 0);
        Interlocked.Exchange(ref _totalApiRequests, 0);
        Interlocked.Exchange(ref _successfulApiRequests, 0);
        Interlocked.Exchange(ref _peakMemory, 0);
        Interlocked.Exchange(ref _currentMemory, 0);
    }

    private long CalculateRatePerMinute(long total)
    {
        var uptime = DateTime.UtcNow - _startTime;
        if (uptime.TotalMinutes < 1)
            return total;
        
        return (long)(total / uptime.TotalMinutes);
    }
}
