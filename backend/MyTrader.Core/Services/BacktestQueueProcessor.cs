using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MyTrader.Core.Services;

public class BacktestQueueProcessor : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BacktestQueueProcessor> _logger;
    private Timer? _processingTimer;
    private Timer? _cleanupTimer;
    
    // Resource management
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly ConcurrentDictionary<Guid, BacktestTask> _runningTasks;
    private readonly int _maxConcurrentBacktests;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);
    
    // Performance monitoring
    private readonly ConcurrentQueue<TimeSpan> _recentCompletionTimes;
    private readonly int _maxRecentTimes = 100;
    
    public BacktestQueueProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<BacktestQueueProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _maxConcurrentBacktests = GetMaxConcurrentBacktests();
        _concurrencyLimiter = new SemaphoreSlim(_maxConcurrentBacktests, _maxConcurrentBacktests);
        _runningTasks = new ConcurrentDictionary<Guid, BacktestTask>();
        _recentCompletionTimes = new ConcurrentQueue<TimeSpan>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BacktestQueueProcessor starting with {MaxConcurrent} max concurrent backtests", 
            _maxConcurrentBacktests);
        
        _processingTimer = new Timer(ProcessQueue, null, TimeSpan.Zero, _processingInterval);
        _cleanupTimer = new Timer(CleanupOldJobs, null, _cleanupInterval, _cleanupInterval);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BacktestQueueProcessor stopping");
        
        _processingTimer?.Change(Timeout.Infinite, 0);
        _cleanupTimer?.Change(Timeout.Infinite, 0);

        // Wait for running tasks to complete with timeout
        var runningTasksList = _runningTasks.Values.Select(t => t.Task).ToArray();
        if (runningTasksList.Length > 0)
        {
            _logger.LogInformation("Waiting for {Count} running backtests to complete", runningTasksList.Length);
            
            try
            {
                await Task.WhenAll(runningTasksList).WaitAsync(TimeSpan.FromMinutes(2), cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Some backtests did not complete within shutdown timeout");
            }
        }
    }

    private async void ProcessQueue(object? state)
    {
        if (_runningTasks.Count >= _maxConcurrentBacktests)
        {
            return; // Already at capacity
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var queueService = scope.ServiceProvider.GetRequiredService<IBacktestQueueService>();
            
            // Process as many items as we have capacity for
            var availableSlots = _maxConcurrentBacktests - _runningTasks.Count;
            var processedCount = 0;
            
            for (int i = 0; i < availableSlots; i++)
            {
                var queueItem = await queueService.GetNextQueuedItemAsync();
                if (queueItem == null)
                    break; // No more items ready to process
                
                if (await _concurrencyLimiter.WaitAsync(100)) // Don't block too long
                {
                    _ = Task.Run(() => ProcessBacktestAsync(queueItem));
                    processedCount++;
                }
                else
                {
                    break; // Can't acquire slot
                }
            }
            
            if (processedCount > 0)
            {
                _logger.LogDebug("Started {Count} new backtests, {Running} now running", 
                    processedCount, _runningTasks.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing backtest queue");
        }
    }

    private async Task ProcessBacktestAsync(BacktestQueue queueItem)
    {
        var stopwatch = Stopwatch.StartNew();
        var backtestTask = new BacktestTask(queueItem.Id, Task.CompletedTask);
        
        try
        {
            _runningTasks[queueItem.Id] = backtestTask;
            
            using var scope = _scopeFactory.CreateScope();
            var queueService = scope.ServiceProvider.GetRequiredService<IBacktestQueueService>();
            var backtestEngine = scope.ServiceProvider.GetRequiredService<IBacktestEngine>();
            
            _logger.LogInformation("Starting backtest {QueueId} for strategy {StrategyId} on symbol {SymbolId}", 
                queueItem.Id, queueItem.StrategyId, queueItem.SymbolId);
            
            // Mark as running
            await queueService.MarkRunningAsync(queueItem.Id);
            
            // Create backtest request from queue item
            var backtestRequest = new BacktestRequest
            {
                UserId = queueItem.UserId,
                StrategyId = queueItem.StrategyId,
                SymbolId = queueItem.SymbolId,
                ConfigurationId = queueItem.ConfigurationId ?? Guid.Empty,
                StartDate = DateTime.UtcNow.AddDays(-90), // Default 3 months
                EndDate = DateTime.UtcNow,
                Timeframe = "1h" // Default timeframe
            };

            // Execute the backtest
            var result = await backtestEngine.RunBacktestAsync(backtestRequest);
            
            // Mark as completed
            stopwatch.Stop();
            await queueService.MarkCompletedAsync(queueItem.Id, result.Id, stopwatch.Elapsed);
            
            // Track completion time for performance monitoring
            RecordCompletionTime(stopwatch.Elapsed);
            
            _logger.LogInformation("Completed backtest {QueueId} in {Duration}ms, result ID: {ResultId}", 
                queueItem.Id, stopwatch.ElapsedMilliseconds, result.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            using var scope = _scopeFactory.CreateScope();
            var queueService = scope.ServiceProvider.GetRequiredService<IBacktestQueueService>();
            
            var shouldRetry = DetermineIfShouldRetry(ex, queueItem.RetryCount);
            await queueService.MarkFailedAsync(queueItem.Id, ex.Message, shouldRetry);
            
            _logger.LogError(ex, "Backtest {QueueId} failed after {Duration}ms (retry count: {RetryCount})", 
                queueItem.Id, stopwatch.ElapsedMilliseconds, queueItem.RetryCount);
        }
        finally
        {
            _runningTasks.TryRemove(queueItem.Id, out _);
            _concurrencyLimiter.Release();
        }
    }

    private async void CleanupOldJobs(object? state)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var queueService = scope.ServiceProvider.GetRequiredService<IBacktestQueueService>();
            
            await queueService.CleanupCompletedJobsAsync(30); // Keep 30 days of history
            
            _logger.LogDebug("Completed queue cleanup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during queue cleanup");
        }
    }

    private bool DetermineIfShouldRetry(Exception ex, int currentRetryCount)
    {
        // Don't retry if we've exceeded max retries
        if (currentRetryCount >= 3)
            return false;
        
        // Retry for transient errors
        return ex switch
        {
            TimeoutException => true,
            HttpRequestException => true,
            InvalidOperationException when ex.Message.Contains("Insufficient market data") => false,
            ArgumentException => false, // Bad configuration, don't retry
            _ => true // Retry unknown errors
        };
    }

    private void RecordCompletionTime(TimeSpan duration)
    {
        _recentCompletionTimes.Enqueue(duration);
        
        // Keep only recent completion times
        while (_recentCompletionTimes.Count > _maxRecentTimes)
        {
            _recentCompletionTimes.TryDequeue(out _);
        }
    }

    public TimeSpan GetAverageCompletionTime()
    {
        var times = _recentCompletionTimes.ToArray();
        if (times.Length == 0)
            return TimeSpan.FromMinutes(5); // Default estimate
        
        var avgTicks = times.Select(t => t.Ticks).Average();
        return new TimeSpan((long)avgTicks);
    }

    public QueueProcessorStats GetStats()
    {
        return new QueueProcessorStats
        {
            RunningTasks = _runningTasks.Count,
            MaxConcurrentTasks = _maxConcurrentBacktests,
            AverageCompletionTime = GetAverageCompletionTime(),
            CompletionTimeSamples = _recentCompletionTimes.Count,
            AvailableSlots = _maxConcurrentBacktests - _runningTasks.Count
        };
    }

    private int GetMaxConcurrentBacktests()
    {
        // Determine max concurrent backtests based on system resources
        var coreCount = Environment.ProcessorCount;
        var availableMemoryGB = GC.GetTotalMemory(false) / (1024 * 1024 * 1024);
        
        // Conservative approach: 1 backtest per 2 cores, max 8
        var maxBasedOnCores = Math.Max(1, coreCount / 2);
        var maxBasedOnMemory = Math.Max(1, (int)availableMemoryGB / 2);
        
        var maxConcurrent = Math.Min(Math.Min(maxBasedOnCores, maxBasedOnMemory), 8);
        
        _logger.LogInformation("Determined max concurrent backtests: {Max} (cores: {Cores}, memory: {Memory}GB)", 
            maxConcurrent, coreCount, availableMemoryGB);
        
        return maxConcurrent;
    }

    public void Dispose()
    {
        _processingTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _concurrencyLimiter?.Dispose();
    }
}

public record BacktestTask(Guid QueueId, Task Task);

public class QueueProcessorStats
{
    public int RunningTasks { get; set; }
    public int MaxConcurrentTasks { get; set; }
    public TimeSpan AverageCompletionTime { get; set; }
    public int CompletionTimeSamples { get; set; }
    public int AvailableSlots { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
}

