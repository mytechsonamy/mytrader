using System;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Queue;

public record QueueBacktestRequest(
    [Required] Guid StrategyId,
    [Required] Guid SymbolId,
    Guid? ConfigurationId,
    int Priority = 50,
    string TriggerType = "Manual",
    Dictionary<string, object>? Parameters = null,
    DateTime? ScheduledFor = null
);

public record BacktestQueueResponse(
    Guid Id,
    Guid UserId,
    Guid StrategyId,
    Guid SymbolId,
    Guid? ConfigurationId,
    string TriggerType,
    string Status,
    int Priority,
    int RetryCount,
    int MaxRetries,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? ScheduledFor,
    TimeSpan? EstimatedDuration,
    TimeSpan? ActualDuration,
    string? ErrorMessage,
    Guid? ResultId,
    QueueItemDetails Details
);

public record QueueItemDetails(
    string StrategyName,
    string SymbolTicker,
    string SymbolVenue,
    int QueuePosition,
    TimeSpan? EstimatedTimeToStart
);

public record QueueStatsResponse(
    int TotalQueued,
    int TotalRunning,
    int TotalCompletedToday,
    int TotalFailedToday,
    TimeSpan AverageCompletionTime,
    TimeSpan EstimatedWaitTime,
    QueueResourceUsage ResourceUsage
);

public record QueueResourceUsage(
    int ActiveWorkers,
    int MaxWorkers,
    double CpuUsagePercent,
    double MemoryUsagePercent,
    int QueuedJobs,
    int RunningJobs
);

public record CancelBacktestRequest(
    Guid QueueId,
    string? Reason = null
);

public record RetryBacktestRequest(
    Guid QueueId,
    int? NewPriority = null,
    bool ResetRetryCount = false
);

public record UpdateQueueItemRequest(
    Guid QueueId,
    int? Priority = null,
    DateTime? ScheduledFor = null,
    Dictionary<string, object>? Parameters = null
);

public record QueueFilterRequest(
    Guid? UserId = null,
    Guid? StrategyId = null,
    Guid? SymbolId = null,
    string? Status = null,
    string? TriggerType = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null,
    int Skip = 0,
    int Take = 50,
    string OrderBy = "Priority",
    bool Descending = true
);

public record BulkQueueOperation(
    List<Guid> QueueIds,
    string Operation, // "Cancel", "Retry", "UpdatePriority", "Reschedule"
    Dictionary<string, object>? Parameters = null
);