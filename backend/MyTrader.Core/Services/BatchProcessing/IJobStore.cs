using MyTrader.Core.DTOs;

namespace MyTrader.Core.Services.BatchProcessing;

/// <summary>
/// Interface for job tracking and storage
/// </summary>
public interface IJobStore
{
    /// <summary>
    /// Store a job with its metadata
    /// </summary>
    Task<string> CreateJobAsync(string jobType, object parameters, JobState initialState = JobState.Enqueued);

    /// <summary>
    /// Update job status and metadata
    /// </summary>
    Task UpdateJobAsync(string jobId, Action<BatchJobStatus> updateAction);

    /// <summary>
    /// Get job status by ID
    /// </summary>
    Task<BatchJobStatus?> GetJobAsync(string jobId);

    /// <summary>
    /// Get all jobs within a date range
    /// </summary>
    Task<List<BatchJobStatus>> GetJobsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get failed jobs for retry processing
    /// </summary>
    Task<List<BatchJobFailure>> GetFailedJobsAsync(int pageSize, int page);

    /// <summary>
    /// Mark job as cancelled
    /// </summary>
    Task<bool> CancelJobAsync(string jobId);

    /// <summary>
    /// Get jobs by state
    /// </summary>
    Task<List<BatchJobStatus>> GetJobsByStateAsync(JobState state);

    /// <summary>
    /// Clean up old completed jobs
    /// </summary>
    Task CleanupOldJobsAsync(TimeSpan retentionPeriod);
}

/// <summary>
/// Interface for recurring job scheduling
/// </summary>
public interface IRecurringJobStore
{
    /// <summary>
    /// Schedule a recurring job
    /// </summary>
    Task<string> ScheduleRecurringJobAsync(string jobType, string cronExpression, object parameters, string? timeZone = null);

    /// <summary>
    /// Remove a recurring job
    /// </summary>
    Task<bool> RemoveRecurringJobAsync(string recurringJobId);

    /// <summary>
    /// Get all active recurring jobs
    /// </summary>
    Task<List<RecurringJobInfo>> GetActiveRecurringJobsAsync();

    /// <summary>
    /// Get next execution time for a recurring job
    /// </summary>
    Task<DateTime?> GetNextExecutionTimeAsync(string recurringJobId);
}

/// <summary>
/// Recurring job information
/// </summary>
public class RecurringJobInfo
{
    public required string Id { get; set; }
    public required string JobType { get; set; }
    public required string CronExpression { get; set; }
    public required object Parameters { get; set; }
    public string? TimeZone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastExecutionAt { get; set; }
    public DateTime? NextExecutionAt { get; set; }
    public bool IsActive { get; set; } = true;
}