using MyTrader.Core.DTOs;

namespace MyTrader.Core.Services.BatchProcessing;

/// <summary>
/// Interface for batch job orchestration with SLA management and monitoring
/// </summary>
public interface IBatchJobOrchestrator
{
    /// <summary>
    /// Enqueue data import job for specific market with SLA requirements
    /// </summary>
    Task<string> EnqueueMarketImportJobAsync(
        MarketImportJobRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueue parallel import jobs for all markets
    /// </summary>
    Task<BatchJobBatchResult> EnqueueAllMarketsImportAsync(
        AllMarketsImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job status and progress information
    /// </summary>
    Task<BatchJobStatus> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Get comprehensive job monitoring statistics
    /// </summary>
    Task<BatchJobMonitoringStats> GetJobMonitoringStatsAsync(
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Cancel running job
    /// </summary>
    Task<bool> CancelJobAsync(string jobId);

    /// <summary>
    /// Retry failed job with exponential backoff
    /// </summary>
    Task<string> RetryJobAsync(string jobId, RetryJobOptions? options = null);

    /// <summary>
    /// Get failed jobs for dead letter queue processing
    /// </summary>
    Task<List<BatchJobFailure>> GetFailedJobsAsync(
        int pageSize = 50,
        int pageNumber = 1);

    /// <summary>
    /// Reschedule all markets import with cron expression
    /// </summary>
    Task<string> ScheduleRecurringAllMarketsImportAsync(
        AllMarketsImportRequest request,
        string cronExpression,
        string? timeZone = null);

    /// <summary>
    /// Remove recurring job
    /// </summary>
    Task<bool> RemoveRecurringJobAsync(string recurringJobId);
}