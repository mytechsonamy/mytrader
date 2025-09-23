using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Services.BatchProcessing;

namespace MyTrader.Infrastructure.Services.BatchProcessing;

/// <summary>
/// Native .NET implementation of batch job orchestrator using IHostedService
/// Replaces Hangfire with built-in .NET background services
/// </summary>
public class NativeBatchJobOrchestrator : BackgroundService, IBatchJobOrchestrator
{
    private readonly IJobStore _jobStore;
    private readonly IRecurringJobStore _recurringJobStore;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<NativeBatchJobOrchestrator> _logger;
    private readonly Timer _jobProcessorTimer;
    private readonly Timer _recurringJobTimer;
    private readonly Timer _cleanupTimer;

    public NativeBatchJobOrchestrator(
        IJobStore jobStore,
        IRecurringJobStore recurringJobStore,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<NativeBatchJobOrchestrator> logger)
    {
        _jobStore = jobStore;
        _recurringJobStore = recurringJobStore;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        // Timer to process enqueued jobs every 5 seconds
        _jobProcessorTimer = new Timer(ProcessEnqueuedJobs, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        // Timer to check recurring jobs every minute
        _recurringJobTimer = new Timer(ProcessRecurringJobs, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        // Timer to cleanup old jobs every hour
        _cleanupTimer = new Timer(CleanupOldJobs, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    public async Task<string> EnqueueMarketImportJobAsync(MarketImportJobRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enqueuing market import job for {Market}", request.Market);

        var jobId = await _jobStore.CreateJobAsync("MarketImport", request, JobState.Enqueued);

        await _jobStore.UpdateJobAsync(jobId, job =>
        {
            job.SlaTarget = request.SlaTarget;
            job.JobMetadata["market"] = request.Market;
            job.JobMetadata["dataPath"] = request.DataPath;
            job.JobMetadata["batchSize"] = request.BatchSize;
            job.JobMetadata["maxRetries"] = request.MaxRetries;
            job.JobMetadata["maxConcurrency"] = request.MaxConcurrency;
        });

        _logger.LogInformation("Market import job {JobId} enqueued for {Market}", jobId, request.Market);
        return jobId;
    }

    public async Task<BatchJobBatchResult> EnqueueAllMarketsImportAsync(AllMarketsImportRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enqueuing all markets import from {Path}", request.StockScrapperDataPath);

        var result = new BatchJobBatchResult
        {
            Success = true,
            ScheduledAt = DateTime.UtcNow,
            EstimatedDuration = request.GlobalSlaTarget
        };

        try
        {
            // Create parent coordination job
            var parentJobId = await _jobStore.CreateJobAsync("AllMarketsImport", request, JobState.Enqueued);
            result.ParentJobId = parentJobId;

            // Discover markets from data path
            var markets = DiscoverMarkets(request.StockScrapperDataPath);

            foreach (var market in markets)
            {
                var marketConfig = request.MarketConfigs.GetValueOrDefault(market, new MarketConfig());
                var marketRequest = new MarketImportJobRequest
                {
                    Market = market,
                    DataPath = Path.Combine(request.StockScrapperDataPath, market),
                    BatchSize = marketConfig.BatchSize,
                    MaxRetries = 3,
                    SlaTarget = marketConfig.SlaTarget,
                    MaxConcurrency = marketConfig.MaxConcurrency,
                    EnableDuplicateCleanup = marketConfig.EnableDuplicateCleanup,
                    IncludeFiles = marketConfig.IncludeFiles,
                    ExcludeFiles = marketConfig.ExcludeFiles,
                    JobPriority = request.JobPriority
                };

                var jobId = await EnqueueMarketImportJobAsync(marketRequest, cancellationToken);
                result.JobIds.Add(jobId);
                result.MarketJobIds[market] = jobId;

                // Link child jobs to parent
                await _jobStore.UpdateJobAsync(jobId, job =>
                {
                    job.ParentJobId = parentJobId;
                });

                await _jobStore.UpdateJobAsync(parentJobId, parentJob =>
                {
                    parentJob.ChildJobIds.Add(jobId);
                });
            }

            result.Message = $"Successfully enqueued {result.JobIds.Count} market import jobs";
            _logger.LogInformation("All markets import batch enqueued with {JobCount} jobs", result.JobIds.Count);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed to enqueue all markets import: {ex.Message}";
            _logger.LogError(ex, "Failed to enqueue all markets import");
        }

        return result;
    }

    public async Task<BatchJobStatus> GetJobStatusAsync(string jobId)
    {
        var job = await _jobStore.GetJobAsync(jobId);
        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} not found");
        }

        return job;
    }

    public async Task<BatchJobMonitoringStats> GetJobMonitoringStatsAsync(DateTime startDate, DateTime endDate)
    {
        var jobs = await _jobStore.GetJobsAsync(startDate, endDate);

        var stats = new BatchJobMonitoringStats
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalJobs = jobs.Count,
            SuccessfulJobs = jobs.Count(j => j.State == JobState.Succeeded),
            FailedJobs = jobs.Count(j => j.State == JobState.Failed),
            RetryingJobs = jobs.Count(j => j.State == JobState.Retrying),
            SlaBreachedJobs = jobs.Count(j => j.IsSlaBreached),
            TotalRecordsProcessed = jobs.Sum(j => j.RecordsProcessed),
            AverageProcessingRate = jobs.Any() ? jobs.Average(j => j.ProcessingRate) : 0
        };

        if (jobs.Any(j => j.Duration.HasValue))
        {
            var durations = jobs.Where(j => j.Duration.HasValue).Select(j => j.Duration!.Value).ToList();
            stats.AverageJobDuration = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
            stats.P95JobDuration = durations.OrderBy(d => d).Skip((int)(durations.Count * 0.95)).FirstOrDefault();
        }

        // Group by market
        foreach (var job in jobs)
        {
            var market = job.JobMetadata.GetValueOrDefault("market", "unknown").ToString() ?? "unknown";
            stats.JobsByMarket[market] = stats.JobsByMarket.GetValueOrDefault(market, 0) + 1;
        }

        return stats;
    }

    public async Task<bool> CancelJobAsync(string jobId)
    {
        return await _jobStore.CancelJobAsync(jobId);
    }

    public async Task<string> RetryJobAsync(string jobId, RetryJobOptions? options = null)
    {
        var originalJob = await _jobStore.GetJobAsync(jobId);
        if (originalJob == null)
        {
            throw new InvalidOperationException($"Job {jobId} not found");
        }

        options ??= new RetryJobOptions();

        // Create new job for retry
        var retryJobId = await _jobStore.CreateJobAsync(originalJob.JobType, originalJob.JobMetadata, JobState.Enqueued);

        await _jobStore.UpdateJobAsync(retryJobId, job =>
        {
            job.JobMetadata["originalJobId"] = jobId;
            job.JobMetadata["isRetry"] = true;
            job.JobMetadata["retryOptions"] = JsonSerializer.Serialize(options);
            job.RetryCount = options.ResetRetryCount ? 0 : originalJob.RetryCount + 1;
        });

        _logger.LogInformation("Created retry job {RetryJobId} for original job {OriginalJobId}", retryJobId, jobId);
        return retryJobId;
    }

    public async Task<List<BatchJobFailure>> GetFailedJobsAsync(int pageSize, int page)
    {
        return await _jobStore.GetFailedJobsAsync(pageSize, page);
    }

    public async Task<string> ScheduleRecurringAllMarketsImportAsync(AllMarketsImportRequest importRequest, string cronExpression, string? timeZone)
    {
        return await _recurringJobStore.ScheduleRecurringJobAsync("AllMarketsImport", cronExpression, importRequest, timeZone);
    }

    public async Task<bool> RemoveRecurringJobAsync(string recurringJobId)
    {
        return await _recurringJobStore.RemoveRecurringJobAsync(recurringJobId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Native Batch Job Orchestrator started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Native Batch Job Orchestrator stopped");
    }

    private async void ProcessEnqueuedJobs(object? state)
    {
        try
        {
            var enqueuedJobs = await _jobStore.GetJobsByStateAsync(JobState.Enqueued);

            foreach (var job in enqueuedJobs.Take(5)) // Process max 5 jobs at a time
            {
                _ = Task.Run(async () => await ProcessJobAsync(job.JobId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing enqueued jobs");
        }
    }

    private async void ProcessRecurringJobs(object? state)
    {
        try
        {
            var recurringJobs = await _recurringJobStore.GetActiveRecurringJobsAsync();
            var now = DateTime.UtcNow;

            foreach (var recurringJob in recurringJobs.Where(j => j.NextExecutionAt <= now))
            {
                // Execute recurring job
                var jobId = await _jobStore.CreateJobAsync(recurringJob.JobType, recurringJob.Parameters, JobState.Enqueued);

                await _jobStore.UpdateJobAsync(jobId, job =>
                {
                    job.JobMetadata["recurringJobId"] = recurringJob.Id;
                    job.JobMetadata["isRecurring"] = true;
                });

                // Update next execution time (simple implementation - should use proper cron parser)
                recurringJob.LastExecutionAt = now;
                recurringJob.NextExecutionAt = now.AddHours(1); // Placeholder: should calculate from cron

                _logger.LogInformation("Triggered recurring job {RecurringJobId}, created job {JobId}",
                    recurringJob.Id, jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing recurring jobs");
        }
    }

    private async void CleanupOldJobs(object? state)
    {
        try
        {
            await _jobStore.CleanupOldJobsAsync(TimeSpan.FromDays(7));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old jobs");
        }
    }

    private async Task ProcessJobAsync(string jobId)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            await _jobStore.UpdateJobAsync(jobId, job =>
            {
                job.State = JobState.Processing;
                job.StartedAt = DateTime.UtcNow;
            });

            var job = await _jobStore.GetJobAsync(jobId);
            if (job == null) return;

            switch (job.JobType)
            {
                case "MarketImport":
                    await ProcessMarketImportJob(scope, job);
                    break;
                case "AllMarketsImport":
                    await ProcessAllMarketsImportJob(scope, job);
                    break;
                default:
                    _logger.LogWarning("Unknown job type: {JobType}", job.JobType);
                    break;
            }

            await _jobStore.UpdateJobAsync(jobId, job =>
            {
                job.State = JobState.Succeeded;
                job.CompletedAt = DateTime.UtcNow;
                job.ProgressPercentage = 100;
            });

            _logger.LogInformation("Job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed with error: {Error}", jobId, ex.Message);

            await _jobStore.UpdateJobAsync(jobId, job =>
            {
                job.State = JobState.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.ErrorMessage = ex.Message;
                job.JobMetadata["stackTrace"] = ex.StackTrace;
            });
        }
    }

    private async Task ProcessMarketImportJob(IServiceScope scope, BatchJobStatus job)
    {
        var dataImportService = scope.ServiceProvider.GetRequiredService<IDataImportService>();
        var parametersJson = job.JobMetadata["parameters"].ToString();
        var request = JsonSerializer.Deserialize<MarketImportJobRequest>(parametersJson!);

        _logger.LogInformation("Processing market import for {Market} from {DataPath}",
            request!.Market, request.DataPath);

        // Simulate progress updates
        for (int i = 0; i <= 100; i += 10)
        {
            await _jobStore.UpdateJobAsync(job.JobId, j =>
            {
                j.ProgressPercentage = i;
                j.CurrentOperation = $"Processing {request.Market} data - {i}% complete";
                j.RecordsProcessed = i * 10; // Mock data
                j.RecordsTotal = 1000; // Mock data
                j.ProcessingRate = 100; // Mock rate
            });

            await Task.Delay(100); // Simulate work
        }

        // In a real implementation, this would call the actual data import service
        // await dataImportService.ImportMarketDataAsync(request);
    }

    private async Task ProcessAllMarketsImportJob(IServiceScope scope, BatchJobStatus job)
    {
        _logger.LogInformation("Processing all markets import coordination job {JobId}", job.JobId);

        // Monitor child jobs and update parent progress
        var childJobs = new List<BatchJobStatus>();
        foreach (var childJobId in job.ChildJobIds)
        {
            var childJob = await _jobStore.GetJobAsync(childJobId);
            if (childJob != null)
            {
                childJobs.Add(childJob);
            }
        }

        // Calculate overall progress
        var overallProgress = childJobs.Any() ? (int)childJobs.Average(j => j.ProgressPercentage) : 100;
        var completedJobs = childJobs.Count(j => j.State == JobState.Succeeded || j.State == JobState.Failed);

        await _jobStore.UpdateJobAsync(job.JobId, j =>
        {
            j.ProgressPercentage = overallProgress;
            j.CurrentOperation = $"Coordinating {childJobs.Count} market imports - {completedJobs} completed";
            j.RecordsProcessed = childJobs.Sum(cj => cj.RecordsProcessed);
            j.RecordsTotal = childJobs.Sum(cj => cj.RecordsTotal);
        });
    }

    private List<string> DiscoverMarkets(string stockScrapperDataPath)
    {
        try
        {
            if (!Directory.Exists(stockScrapperDataPath))
            {
                _logger.LogWarning("Stock scrapper data path does not exist: {Path}", stockScrapperDataPath);
                return new List<string>();
            }

            var markets = Directory.GetDirectories(stockScrapperDataPath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>()
                .ToList();

            _logger.LogInformation("Discovered {MarketCount} markets: {Markets}",
                markets.Count, string.Join(", ", markets));

            return markets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering markets from {Path}", stockScrapperDataPath);
            return new List<string>();
        }
    }

    public override void Dispose()
    {
        _jobProcessorTimer?.Dispose();
        _recurringJobTimer?.Dispose();
        _cleanupTimer?.Dispose();
        base.Dispose();
    }
}