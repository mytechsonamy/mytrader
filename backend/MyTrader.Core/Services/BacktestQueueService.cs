using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Data;
using MyTrader.Core.DTOs.Queue;
using MyTrader.Core.Models;
using System.Text.Json;

namespace MyTrader.Core.Services;

public interface IBacktestQueueService
{
    // Queue Management
    Task<BacktestQueueResponse> EnqueueAsync(Guid userId, QueueBacktestRequest request);
    Task<BacktestQueueResponse> GetQueueItemAsync(Guid queueId);
    Task<List<BacktestQueueResponse>> GetUserQueueAsync(Guid userId, QueueFilterRequest? filter = null);
    Task<QueueStatsResponse> GetQueueStatsAsync();
    
    // Queue Operations
    Task<bool> CancelAsync(Guid queueId, string? reason = null);
    Task<bool> RetryAsync(Guid queueId, int? newPriority = null);
    Task<bool> UpdatePriorityAsync(Guid queueId, int priority);
    Task<bool> RescheduleAsync(Guid queueId, DateTime scheduledFor);
    Task<int> BulkOperationAsync(BulkQueueOperation operation);
    
    // Queue Processing
    Task<BacktestQueue?> GetNextQueuedItemAsync();
    Task<bool> MarkRunningAsync(Guid queueId);
    Task<bool> MarkCompletedAsync(Guid queueId, Guid resultId, TimeSpan duration);
    Task<bool> MarkFailedAsync(Guid queueId, string errorMessage, bool shouldRetry = true);
    
    // Queue Analytics
    Task<List<BacktestQueueResponse>> GetQueueHistoryAsync(Guid userId, int days = 7);
    Task<Dictionary<string, object>> GetQueueAnalyticsAsync();
    Task CleanupCompletedJobsAsync(int daysToKeep = 30);
}

public class BacktestQueueService : IBacktestQueueService
{
    private readonly ITradingDbContext _context;
    private readonly ILogger<BacktestQueueService> _logger;
    private readonly IBacktestEngine _backtestEngine;
    
    // Configuration
    private readonly int _maxConcurrentBacktests = 3;
    private readonly TimeSpan _defaultEstimatedDuration = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, int> _triggerTypePriorities = new()
    {
        { "Manual", 50 },
        { "Scheduled", 30 },
        { "NewSymbol", 70 },
        { "PerformanceDegradation", 80 },
        { "MarketEvent", 90 },
        { "Optimization", 40 }
    };

    public BacktestQueueService(
        ITradingDbContext context,
        ILogger<BacktestQueueService> logger,
        IBacktestEngine backtestEngine)
    {
        _context = context;
        _logger = logger;
        _backtestEngine = backtestEngine;
    }

    public async Task<BacktestQueueResponse> EnqueueAsync(Guid userId, QueueBacktestRequest request)
    {
        _logger.LogInformation("Enqueueing backtest for user {UserId}, strategy {StrategyId}, symbol {SymbolId}", 
            userId, request.StrategyId, request.SymbolId);

        // Check if similar item already in queue
        var existingItem = await _context.BacktestQueue
            .FirstOrDefaultAsync(q => 
                q.UserId == userId &&
                q.StrategyId == request.StrategyId &&
                q.SymbolId == request.SymbolId &&
                q.ConfigurationId == request.ConfigurationId &&
                (q.Status == "Queued" || q.Status == "Running"));

        if (existingItem != null)
        {
            _logger.LogWarning("Similar backtest already in queue: {QueueId}", existingItem.Id);
            return await MapToResponse(existingItem);
        }

        // Determine priority based on trigger type
        var priority = request.Priority;
        if (_triggerTypePriorities.ContainsKey(request.TriggerType))
        {
            priority = Math.Max(priority, _triggerTypePriorities[request.TriggerType]);
        }

        var queueItem = new BacktestQueue
        {
            UserId = userId,
            StrategyId = request.StrategyId,
            SymbolId = request.SymbolId,
            ConfigurationId = request.ConfigurationId,
            TriggerType = request.TriggerType,
            Priority = priority,
            ScheduledFor = request.ScheduledFor,
            EstimatedDuration = _defaultEstimatedDuration,
            Parameters = request.Parameters != null ? JsonSerializer.Serialize(request.Parameters) : null
        };

        _context.BacktestQueue.Add(queueItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Backtest queued with ID {QueueId}, priority {Priority}", queueItem.Id, priority);

        return await MapToResponse(queueItem);
    }

    public async Task<BacktestQueue?> GetNextQueuedItemAsync()
    {
        // Check current running count
        var runningCount = await _context.BacktestQueue.CountAsync(q => q.Status == "Running");
        if (runningCount >= _maxConcurrentBacktests)
        {
            return null;
        }

        // Get the highest priority queued item that's ready to run
        var now = DateTime.UtcNow;
        return await _context.BacktestQueue
            .Include(q => q.Strategy)
            .Include(q => q.Symbol)
            .Include(q => q.User)
            .Where(q => 
                q.Status == "Queued" && 
                (q.ScheduledFor == null || q.ScheduledFor <= now) &&
                q.RetryCount < q.MaxRetries)
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> MarkRunningAsync(Guid queueId)
    {
        var item = await _context.BacktestQueue.FindAsync(queueId);
        if (item == null) return false;

        item.Status = "Running";
        item.StartedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Queue item {QueueId} marked as running", queueId);
        return true;
    }

    public async Task<bool> MarkCompletedAsync(Guid queueId, Guid resultId, TimeSpan duration)
    {
        var item = await _context.BacktestQueue.FindAsync(queueId);
        if (item == null) return false;

        item.Status = "Completed";
        item.CompletedAt = DateTime.UtcNow;
        item.ResultId = resultId;
        item.ActualDuration = duration;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Queue item {QueueId} marked as completed with result {ResultId}", queueId, resultId);
        return true;
    }

    public async Task<bool> MarkFailedAsync(Guid queueId, string errorMessage, bool shouldRetry = true)
    {
        var item = await _context.BacktestQueue.FindAsync(queueId);
        if (item == null) return false;

        item.ErrorMessage = errorMessage;
        item.RetryCount++;

        if (shouldRetry && item.RetryCount < item.MaxRetries)
        {
            item.Status = "Queued";
            item.StartedAt = null;
            // Reduce priority slightly for retries
            item.Priority = Math.Max(1, item.Priority - 5);
            _logger.LogWarning("Queue item {QueueId} failed, will retry ({RetryCount}/{MaxRetries})", 
                queueId, item.RetryCount, item.MaxRetries);
        }
        else
        {
            item.Status = "Failed";
            item.CompletedAt = DateTime.UtcNow;
            _logger.LogError("Queue item {QueueId} failed permanently: {Error}", queueId, errorMessage);
        }
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BacktestQueueResponse> GetQueueItemAsync(Guid queueId)
    {
        var item = await _context.BacktestQueue
            .Include(q => q.Strategy)
            .Include(q => q.Symbol)
            .FirstOrDefaultAsync(q => q.Id == queueId);
        
        if (item == null)
            throw new ArgumentException($"Queue item {queueId} not found");
            
        return await MapToResponse(item);
    }

    public async Task<List<BacktestQueueResponse>> GetUserQueueAsync(Guid userId, QueueFilterRequest? filter = null)
    {
        var query = _context.BacktestQueue
            .Include(q => q.Strategy)
            .Include(q => q.Symbol)
            .Where(q => q.UserId == userId);

        // Apply filters
        if (filter != null)
        {
            if (filter.StrategyId.HasValue)
                query = query.Where(q => q.StrategyId == filter.StrategyId);
            if (filter.SymbolId.HasValue)
                query = query.Where(q => q.SymbolId == filter.SymbolId);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(q => q.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.TriggerType))
                query = query.Where(q => q.TriggerType == filter.TriggerType);
            if (filter.CreatedAfter.HasValue)
                query = query.Where(q => q.CreatedAt >= filter.CreatedAfter);
            if (filter.CreatedBefore.HasValue)
                query = query.Where(q => q.CreatedAt <= filter.CreatedBefore);
        }

        // Apply ordering
        query = filter?.OrderBy switch
        {
            "CreatedAt" => filter.Descending ? query.OrderByDescending(q => q.CreatedAt) : query.OrderBy(q => q.CreatedAt),
            "Priority" => filter.Descending ? query.OrderByDescending(q => q.Priority) : query.OrderBy(q => q.Priority),
            "Status" => filter.Descending ? query.OrderByDescending(q => q.Status) : query.OrderBy(q => q.Status),
            _ => query.OrderByDescending(q => q.Priority).ThenByDescending(q => q.CreatedAt)
        };

        var items = await query
            .Skip(filter?.Skip ?? 0)
            .Take(filter?.Take ?? 50)
            .ToListAsync();

        var responses = new List<BacktestQueueResponse>();
        foreach (var item in items)
        {
            responses.Add(await MapToResponse(item));
        }

        return responses;
    }

    public async Task<QueueStatsResponse> GetQueueStatsAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;

        var stats = await _context.BacktestQueue
            .Where(q => q.CreatedAt >= todayStart.AddDays(-1))
            .GroupBy(q => 1)
            .Select(g => new
            {
                TotalQueued = g.Count(q => q.Status == "Queued"),
                TotalRunning = g.Count(q => q.Status == "Running"),
                TotalCompletedToday = g.Count(q => q.Status == "Completed" && q.CompletedAt >= todayStart),
                TotalFailedToday = g.Count(q => q.Status == "Failed" && q.CompletedAt >= todayStart),
                AvgDuration = g.Where(q => q.ActualDuration.HasValue).Any() 
                    ? g.Where(q => q.ActualDuration.HasValue).Average(q => q.ActualDuration!.Value.TotalMinutes)
                    : (double?)null
            })
            .FirstOrDefaultAsync();

        var averageCompletionTime = stats?.AvgDuration.HasValue == true 
            ? TimeSpan.FromMinutes(stats.AvgDuration.Value) 
            : TimeSpan.Zero;

        var queuedCount = stats?.TotalQueued ?? 0;
        var estimatedWaitTime = queuedCount > 0 ? averageCompletionTime.Multiply(queuedCount / _maxConcurrentBacktests) : TimeSpan.Zero;

        return new QueueStatsResponse(
            TotalQueued: stats?.TotalQueued ?? 0,
            TotalRunning: stats?.TotalRunning ?? 0,
            TotalCompletedToday: stats?.TotalCompletedToday ?? 0,
            TotalFailedToday: stats?.TotalFailedToday ?? 0,
            AverageCompletionTime: averageCompletionTime,
            EstimatedWaitTime: estimatedWaitTime,
            ResourceUsage: new QueueResourceUsage(
                ActiveWorkers: stats?.TotalRunning ?? 0,
                MaxWorkers: _maxConcurrentBacktests,
                CpuUsagePercent: 0, // TODO: Implement real resource monitoring
                MemoryUsagePercent: 0,
                QueuedJobs: stats?.TotalQueued ?? 0,
                RunningJobs: stats?.TotalRunning ?? 0
            )
        );
    }

    public async Task<bool> CancelAsync(Guid queueId, string? reason = null)
    {
        var item = await _context.BacktestQueue.FindAsync(queueId);
        if (item == null || item.Status == "Completed" || item.Status == "Cancelled") 
            return false;

        item.Status = "Cancelled";
        item.CompletedAt = DateTime.UtcNow;
        item.ErrorMessage = reason ?? "Cancelled by user";
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Queue item {QueueId} cancelled: {Reason}", queueId, reason);
        return true;
    }

    public async Task<bool> RetryAsync(Guid queueId, int? newPriority = null)
    {
        var item = await _context.BacktestQueue.FindAsync(queueId);
        if (item == null || item.Status != "Failed") 
            return false;

        item.Status = "Queued";
        item.StartedAt = null;
        item.CompletedAt = null;
        item.ErrorMessage = null;
        item.RetryCount = 0;
        
        if (newPriority.HasValue)
            item.Priority = newPriority.Value;
            
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Queue item {QueueId} queued for retry", queueId);
        return true;
    }

    public async Task<bool> UpdatePriorityAsync(Guid queueId, int priority)
    {
        var item = await _context.BacktestQueue.FindAsync(queueId);
        if (item == null || item.Status != "Queued") 
            return false;

        item.Priority = Math.Clamp(priority, 1, 100);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> RescheduleAsync(Guid queueId, DateTime scheduledFor)
    {
        var item = await _context.BacktestQueue.FindAsync(queueId);
        if (item == null || item.Status != "Queued") 
            return false;

        item.ScheduledFor = scheduledFor;
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<int> BulkOperationAsync(BulkQueueOperation operation)
    {
        var items = await _context.BacktestQueue
            .Where(q => operation.QueueIds.Contains(q.Id))
            .ToListAsync();

        var affected = 0;
        foreach (var item in items)
        {
            var success = operation.Operation switch
            {
                "Cancel" => await CancelItemAsync(item, operation.Parameters?.GetValueOrDefault("reason")?.ToString()),
                "Retry" => await RetryItemAsync(item, operation.Parameters),
                "UpdatePriority" => await UpdateItemPriorityAsync(item, operation.Parameters),
                "Reschedule" => await RescheduleItemAsync(item, operation.Parameters),
                _ => false
            };
            
            if (success) affected++;
        }

        return affected;
    }

    public async Task<List<BacktestQueueResponse>> GetQueueHistoryAsync(Guid userId, int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        
        var items = await _context.BacktestQueue
            .Include(q => q.Strategy)
            .Include(q => q.Symbol)
            .Where(q => q.UserId == userId && q.CreatedAt >= cutoff)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var responses = new List<BacktestQueueResponse>();
        foreach (var item in items)
        {
            responses.Add(await MapToResponse(item));
        }

        return responses;
    }

    public async Task<Dictionary<string, object>> GetQueueAnalyticsAsync()
    {
        var analytics = new Dictionary<string, object>();
        
        // Performance metrics
        var completedJobs = await _context.BacktestQueue
            .Where(q => q.Status == "Completed" && q.CompletedAt >= DateTime.UtcNow.AddDays(-7))
            .ToListAsync();

        if (completedJobs.Count > 0)
        {
            analytics["avgCompletionTime"] = completedJobs
                .Where(j => j.ActualDuration.HasValue)
                .Average(j => j.ActualDuration!.Value.TotalMinutes);
            
            analytics["successRate"] = (double)completedJobs.Count / 
                await _context.BacktestQueue.CountAsync(q => 
                    (q.Status == "Completed" || q.Status == "Failed") && 
                    q.CompletedAt >= DateTime.UtcNow.AddDays(-7)) * 100;
        }

        // Trigger type distribution
        var triggerStats = await _context.BacktestQueue
            .Where(q => q.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .GroupBy(q => q.TriggerType)
            .Select(g => new { TriggerType = g.Key, Count = g.Count() })
            .ToListAsync();

        analytics["triggerTypeDistribution"] = triggerStats.ToDictionary(t => t.TriggerType, t => t.Count);

        return analytics;
    }

    public async Task CleanupCompletedJobsAsync(int daysToKeep = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldJobs = await _context.BacktestQueue
            .Where(q => 
                (q.Status == "Completed" || q.Status == "Failed" || q.Status == "Cancelled") &&
                q.CompletedAt < cutoff)
            .ToListAsync();

        if (oldJobs.Count > 0)
        {
            _context.BacktestQueue.RemoveRange(oldJobs);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} old queue items", oldJobs.Count);
        }
    }

    private async Task<BacktestQueueResponse> MapToResponse(BacktestQueue item)
    {
        // Calculate queue position for queued items
        var queuePosition = 0;
        var estimatedTimeToStart = TimeSpan.Zero;
        
        if (item.Status == "Queued")
        {
            queuePosition = await _context.BacktestQueue
                .CountAsync(q => 
                    q.Status == "Queued" && 
                    (q.Priority > item.Priority || (q.Priority == item.Priority && q.CreatedAt < item.CreatedAt))) + 1;
            
            estimatedTimeToStart = _defaultEstimatedDuration.Multiply(Math.Max(0, queuePosition - _maxConcurrentBacktests));
        }

        var details = new QueueItemDetails(
            StrategyName: item.Strategy?.Name ?? "Unknown",
            SymbolTicker: item.Symbol?.Ticker ?? "Unknown",
            SymbolVenue: item.Symbol?.Venue ?? "Unknown", 
            QueuePosition: queuePosition,
            EstimatedTimeToStart: estimatedTimeToStart > TimeSpan.Zero ? estimatedTimeToStart : null
        );

        return new BacktestQueueResponse(
            Id: item.Id,
            UserId: item.UserId,
            StrategyId: item.StrategyId,
            SymbolId: item.SymbolId,
            ConfigurationId: item.ConfigurationId,
            TriggerType: item.TriggerType,
            Status: item.Status,
            Priority: item.Priority,
            RetryCount: item.RetryCount,
            MaxRetries: item.MaxRetries,
            CreatedAt: item.CreatedAt,
            StartedAt: item.StartedAt,
            CompletedAt: item.CompletedAt,
            ScheduledFor: item.ScheduledFor,
            EstimatedDuration: item.EstimatedDuration,
            ActualDuration: item.ActualDuration,
            ErrorMessage: item.ErrorMessage,
            ResultId: item.ResultId,
            Details: details
        );
    }

    private async Task<bool> CancelItemAsync(BacktestQueue item, string? reason)
    {
        if (item.Status == "Completed" || item.Status == "Cancelled") 
            return false;

        item.Status = "Cancelled";
        item.CompletedAt = DateTime.UtcNow;
        item.ErrorMessage = reason ?? "Cancelled";
        
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> RetryItemAsync(BacktestQueue item, Dictionary<string, object>? parameters)
    {
        if (item.Status != "Failed") return false;

        item.Status = "Queued";
        item.StartedAt = null;
        item.CompletedAt = null;
        item.ErrorMessage = null;
        item.RetryCount = 0;

        if (parameters?.ContainsKey("priority") == true && int.TryParse(parameters["priority"].ToString(), out int priority))
            item.Priority = priority;

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> UpdateItemPriorityAsync(BacktestQueue item, Dictionary<string, object>? parameters)
    {
        if (item.Status != "Queued") return false;
        if (parameters?.ContainsKey("priority") != true) return false;
        if (!int.TryParse(parameters["priority"].ToString(), out int priority)) return false;

        item.Priority = Math.Clamp(priority, 1, 100);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> RescheduleItemAsync(BacktestQueue item, Dictionary<string, object>? parameters)
    {
        if (item.Status != "Queued") return false;
        if (parameters?.ContainsKey("scheduledFor") != true) return false;
        if (!DateTime.TryParse(parameters["scheduledFor"].ToString(), out DateTime scheduledFor)) return false;

        item.ScheduledFor = scheduledFor;
        await _context.SaveChangesAsync();
        return true;
    }
}