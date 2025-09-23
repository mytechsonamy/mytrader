using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs;
using MyTrader.Core.Services.BatchProcessing;

namespace MyTrader.Infrastructure.Services.BatchProcessing;

/// <summary>
/// Memory-based job store for development and small deployments
/// Production environments should use a persistent store (database, Redis, etc.)
/// </summary>
public class MemoryJobStore : IJobStore, IRecurringJobStore
{
    private readonly ConcurrentDictionary<string, BatchJobStatus> _jobs = new();
    private readonly ConcurrentDictionary<string, RecurringJobInfo> _recurringJobs = new();
    private readonly ILogger<MemoryJobStore> _logger;

    public MemoryJobStore(ILogger<MemoryJobStore> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateJobAsync(string jobType, object parameters, JobState initialState = JobState.Enqueued)
    {
        var jobId = Guid.NewGuid().ToString();
        var job = new BatchJobStatus
        {
            JobId = jobId,
            JobType = jobType,
            State = initialState,
            CreatedAt = DateTime.UtcNow,
            ProgressPercentage = 0,
            RecordsProcessed = 0,
            RecordsTotal = 0,
            ProcessingRate = 0,
            RetryCount = 0,
            JobMetadata = new Dictionary<string, object>
            {
                ["parameters"] = JsonSerializer.Serialize(parameters),
                ["created_by"] = "system"
            }
        };

        _jobs.TryAdd(jobId, job);
        _logger.LogInformation("Created job {JobId} of type {JobType}", jobId, jobType);

        return Task.FromResult(jobId);
    }

    public Task UpdateJobAsync(string jobId, Action<BatchJobStatus> updateAction)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            updateAction(job);
            _logger.LogDebug("Updated job {JobId} - State: {State}, Progress: {Progress}%",
                jobId, job.State, job.ProgressPercentage);
        }
        else
        {
            _logger.LogWarning("Attempted to update non-existent job {JobId}", jobId);
        }

        return Task.CompletedTask;
    }

    public Task<BatchJobStatus?> GetJobAsync(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<List<BatchJobStatus>> GetJobsAsync(DateTime startDate, DateTime endDate)
    {
        var jobs = _jobs.Values
            .Where(j => j.CreatedAt >= startDate && j.CreatedAt <= endDate)
            .OrderByDescending(j => j.CreatedAt)
            .ToList();

        return Task.FromResult(jobs);
    }

    public Task<List<BatchJobFailure>> GetFailedJobsAsync(int pageSize, int page)
    {
        var failures = _jobs.Values
            .Where(j => j.State == JobState.Failed)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new BatchJobFailure
            {
                JobId = j.JobId,
                JobType = j.JobType,
                Market = j.JobMetadata.GetValueOrDefault("market", "unknown").ToString() ?? "unknown",
                FailedAt = j.CompletedAt ?? DateTime.UtcNow,
                ErrorMessage = j.ErrorMessage ?? "Unknown error",
                StackTrace = j.JobMetadata.GetValueOrDefault("stackTrace")?.ToString(),
                RetryCount = j.RetryCount,
                IsRetryable = j.RetryCount < 3,
                NextRetryAt = j.NextRetryAt,
                JobParameters = j.JobMetadata
            })
            .ToList();

        return Task.FromResult(failures);
    }

    public Task<bool> CancelJobAsync(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            if (job.State == JobState.Enqueued || job.State == JobState.Processing)
            {
                job.State = JobState.Cancelled;
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Cancelled job {JobId}", jobId);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<List<BatchJobStatus>> GetJobsByStateAsync(JobState state)
    {
        var jobs = _jobs.Values
            .Where(j => j.State == state)
            .OrderBy(j => j.CreatedAt)
            .ToList();

        return Task.FromResult(jobs);
    }

    public Task CleanupOldJobsAsync(TimeSpan retentionPeriod)
    {
        var cutoffDate = DateTime.UtcNow - retentionPeriod;
        var jobsToRemove = _jobs.Values
            .Where(j => j.CompletedAt.HasValue && j.CompletedAt < cutoffDate)
            .Select(j => j.JobId)
            .ToList();

        foreach (var jobId in jobsToRemove)
        {
            _jobs.TryRemove(jobId, out _);
        }

        if (jobsToRemove.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old jobs older than {CutoffDate}",
                jobsToRemove.Count, cutoffDate);
        }

        return Task.CompletedTask;
    }

    // Recurring Job Store Implementation

    public Task<string> ScheduleRecurringJobAsync(string jobType, string cronExpression, object parameters, string? timeZone = null)
    {
        var recurringJobId = Guid.NewGuid().ToString();
        var recurringJob = new RecurringJobInfo
        {
            Id = recurringJobId,
            JobType = jobType,
            CronExpression = cronExpression,
            Parameters = parameters,
            TimeZone = timeZone,
            CreatedAt = DateTime.UtcNow,
            NextExecutionAt = CalculateNextExecution(cronExpression, timeZone),
            IsActive = true
        };

        _recurringJobs.TryAdd(recurringJobId, recurringJob);
        _logger.LogInformation("Scheduled recurring job {RecurringJobId} of type {JobType} with cron {CronExpression}",
            recurringJobId, jobType, cronExpression);

        return Task.FromResult(recurringJobId);
    }

    public Task<bool> RemoveRecurringJobAsync(string recurringJobId)
    {
        var removed = _recurringJobs.TryRemove(recurringJobId, out _);
        if (removed)
        {
            _logger.LogInformation("Removed recurring job {RecurringJobId}", recurringJobId);
        }

        return Task.FromResult(removed);
    }

    public Task<List<RecurringJobInfo>> GetActiveRecurringJobsAsync()
    {
        var activeJobs = _recurringJobs.Values
            .Where(j => j.IsActive)
            .ToList();

        return Task.FromResult(activeJobs);
    }

    public Task<DateTime?> GetNextExecutionTimeAsync(string recurringJobId)
    {
        if (_recurringJobs.TryGetValue(recurringJobId, out var job))
        {
            return Task.FromResult(job.NextExecutionAt);
        }

        return Task.FromResult<DateTime?>(null);
    }

    private DateTime? CalculateNextExecution(string cronExpression, string? timeZone)
    {
        // Simple implementation - in production, use a proper cron parser like NCrontab or Quartz.NET
        // For now, just schedule for next hour as a placeholder
        return DateTime.UtcNow.AddHours(1);
    }
}