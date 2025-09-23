using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs;
using MyTrader.Core.Services.BatchProcessing;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Enterprise-grade batch processing controller for Stock_Scrapper data import orchestration
/// Provides comprehensive job management, monitoring, and SLA tracking capabilities
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BatchProcessingController : ControllerBase
{
    private readonly IBatchJobOrchestrator _batchJobOrchestrator;
    private readonly ILogger<BatchProcessingController> _logger;

    public BatchProcessingController(
        IBatchJobOrchestrator batchJobOrchestrator,
        ILogger<BatchProcessingController> logger)
    {
        _batchJobOrchestrator = batchJobOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Start import job for a specific market
    /// </summary>
    [HttpPost("markets/{market}/import")]
    public async Task<ActionResult<ApiResponse<string>>> StartMarketImportAsync(
        string market,
        [FromBody] MarketImportJobRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting market import job for {Market} from API request", market);

            // Validate market parameter matches request
            if (!string.Equals(market, request.Market, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<string>.ErrorResult($"Market parameter '{market}' does not match request market '{request.Market}'"));
            }

            // Validate data path exists
            if (!Directory.Exists(request.DataPath))
            {
                return BadRequest(ApiResponse<string>.ErrorResult($"Data path does not exist: {request.DataPath}"));
            }

            var jobId = await _batchJobOrchestrator.EnqueueMarketImportJobAsync(request, cancellationToken);

            _logger.LogInformation("Successfully enqueued market import job {JobId} for market {Market}", jobId, market);

            return Ok(ApiResponse<string>.SuccessResult(jobId, $"Market import job enqueued successfully for {market}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start market import job for {Market}", market);
            return StatusCode(500, ApiResponse<string>.ErrorResult($"Failed to start market import: {ex.Message}"));
        }
    }

    /// <summary>
    /// Start parallel import jobs for all available markets
    /// </summary>
    [HttpPost("all-markets/import")]
    public async Task<ActionResult<ApiResponse<BatchJobBatchResult>>> StartAllMarketsImportAsync(
        [FromBody] AllMarketsImportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting all markets import job from {Path}", request.StockScrapperDataPath);

            // Validate stock scrapper data path exists
            if (!Directory.Exists(request.StockScrapperDataPath))
            {
                return BadRequest(ApiResponse<BatchJobBatchResult>.ErrorResult(
                    $"Stock scrapper data path does not exist: {request.StockScrapperDataPath}"));
            }

            var result = await _batchJobOrchestrator.EnqueueAllMarketsImportAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Successfully enqueued all markets import with {JobCount} jobs. Parent job: {ParentJobId}",
                    result.JobIds.Count, result.ParentJobId);

                return Ok(ApiResponse<BatchJobBatchResult>.SuccessResult(result,
                    $"Successfully enqueued {result.JobIds.Count} market import jobs"));
            }
            else
            {
                _logger.LogWarning("Failed to enqueue all markets import: {Message}", result.Message);
                return BadRequest(ApiResponse<BatchJobBatchResult>.ErrorResult(result.Message ?? "Failed to enqueue jobs"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start all markets import");
            return StatusCode(500, ApiResponse<BatchJobBatchResult>.ErrorResult($"Failed to start all markets import: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get detailed status and progress information for a specific job
    /// </summary>
    [HttpGet("jobs/{jobId}/status")]
    public async Task<ActionResult<ApiResponse<BatchJobStatus>>> GetJobStatusAsync(string jobId)
    {
        try
        {
            var status = await _batchJobOrchestrator.GetJobStatusAsync(jobId);

            return Ok(ApiResponse<BatchJobStatus>.SuccessResult(status, "Job status retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);
            return StatusCode(500, ApiResponse<BatchJobStatus>.ErrorResult($"Failed to get job status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get comprehensive monitoring statistics for jobs within a date range
    /// </summary>
    [HttpGet("monitoring/stats")]
    public async Task<ActionResult<ApiResponse<BatchJobMonitoringStats>>> GetMonitoringStatsAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            // Validate date range
            if (startDate >= endDate)
            {
                return BadRequest(ApiResponse<BatchJobMonitoringStats>.ErrorResult("Start date must be before end date"));
            }

            if ((endDate - startDate).TotalDays > 90)
            {
                return BadRequest(ApiResponse<BatchJobMonitoringStats>.ErrorResult("Date range cannot exceed 90 days"));
            }

            var stats = await _batchJobOrchestrator.GetJobMonitoringStatsAsync(startDate, endDate);

            return Ok(ApiResponse<BatchJobMonitoringStats>.SuccessResult(stats,
                $"Monitoring statistics retrieved for period {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get monitoring stats for period {StartDate} to {EndDate}", startDate, endDate);
            return StatusCode(500, ApiResponse<BatchJobMonitoringStats>.ErrorResult($"Failed to get monitoring stats: {ex.Message}"));
        }
    }

    /// <summary>
    /// Cancel a running job
    /// </summary>
    [HttpPost("jobs/{jobId}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelJobAsync(string jobId)
    {
        try
        {
            var success = await _batchJobOrchestrator.CancelJobAsync(jobId);

            if (success)
            {
                _logger.LogInformation("Successfully cancelled job {JobId}", jobId);
                return Ok(ApiResponse<bool>.SuccessResult(true, $"Job {jobId} cancelled successfully"));
            }
            else
            {
                _logger.LogWarning("Failed to cancel job {JobId} - job may not exist or already completed", jobId);
                return BadRequest(ApiResponse<bool>.ErrorResult($"Failed to cancel job {jobId} - job may not exist or already completed"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error cancelling job: {ex.Message}"));
        }
    }

    /// <summary>
    /// Retry a failed job with custom retry options
    /// </summary>
    [HttpPost("jobs/{jobId}/retry")]
    public async Task<ActionResult<ApiResponse<string>>> RetryJobAsync(
        string jobId,
        [FromBody] RetryJobOptions? options = null)
    {
        try
        {
            var newJobId = await _batchJobOrchestrator.RetryJobAsync(jobId, options);

            _logger.LogInformation("Successfully scheduled retry job {NewJobId} for original job {OriginalJobId}",
                newJobId, jobId);

            return Ok(ApiResponse<string>.SuccessResult(newJobId,
                $"Retry job {newJobId} scheduled successfully for original job {jobId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry job {JobId}", jobId);
            return StatusCode(500, ApiResponse<string>.ErrorResult($"Failed to retry job: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get failed jobs for dead letter queue processing
    /// </summary>
    [HttpGet("dead-letter-queue")]
    public async Task<ActionResult<ApiResponse<PagedResult<BatchJobFailure>>>> GetFailedJobsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 200) pageSize = 200; // Cap page size

            var failures = await _batchJobOrchestrator.GetFailedJobsAsync(pageSize, page);

            var result = new PagedResult<BatchJobFailure>
            {
                Items = failures,
                Page = page,
                PageSize = pageSize,
                TotalItems = failures.Count, // This would typically come from a count query
                TotalPages = (int)Math.Ceiling((double)failures.Count / pageSize)
            };

            return Ok(ApiResponse<PagedResult<BatchJobFailure>>.SuccessResult(result,
                $"Retrieved {failures.Count} failed jobs (page {page})"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed jobs for page {Page}", page);
            return StatusCode(500, ApiResponse<PagedResult<BatchJobFailure>>.ErrorResult($"Failed to get failed jobs: {ex.Message}"));
        }
    }

    /// <summary>
    /// Schedule recurring all markets import with cron expression
    /// </summary>
    [HttpPost("schedules/all-markets")]
    public async Task<ActionResult<ApiResponse<string>>> ScheduleRecurringAllMarketsImportAsync(
        [FromBody] ScheduleRecurringImportRequest request)
    {
        try
        {
            // Validate cron expression format (basic validation)
            if (string.IsNullOrWhiteSpace(request.CronExpression))
            {
                return BadRequest(ApiResponse<string>.ErrorResult("Cron expression is required"));
            }

            var recurringJobId = await _batchJobOrchestrator.ScheduleRecurringAllMarketsImportAsync(
                request.ImportRequest,
                request.CronExpression,
                request.TimeZone);

            _logger.LogInformation("Successfully scheduled recurring all markets import {RecurringJobId} with cron {CronExpression}",
                recurringJobId, request.CronExpression);

            return Ok(ApiResponse<string>.SuccessResult(recurringJobId,
                $"Recurring import scheduled successfully with ID {recurringJobId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule recurring all markets import with cron {CronExpression}",
                request.CronExpression);
            return StatusCode(500, ApiResponse<string>.ErrorResult($"Failed to schedule recurring import: {ex.Message}"));
        }
    }

    /// <summary>
    /// Remove a recurring job schedule
    /// </summary>
    [HttpDelete("schedules/{recurringJobId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveRecurringJobAsync(string recurringJobId)
    {
        try
        {
            var success = await _batchJobOrchestrator.RemoveRecurringJobAsync(recurringJobId);

            if (success)
            {
                _logger.LogInformation("Successfully removed recurring job {RecurringJobId}", recurringJobId);
                return Ok(ApiResponse<bool>.SuccessResult(true, $"Recurring job {recurringJobId} removed successfully"));
            }
            else
            {
                return BadRequest(ApiResponse<bool>.ErrorResult($"Failed to remove recurring job {recurringJobId}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing recurring job {RecurringJobId}", recurringJobId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error removing recurring job: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get system health status for native batch processing infrastructure
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<BatchProcessingHealthStatus>>> GetHealthStatusAsync()
    {
        try
        {
            // Get actual job counts from the native orchestrator
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow;
            var stats = await _batchJobOrchestrator.GetJobMonitoringStatsAsync(startDate, endDate);

            var health = new BatchProcessingHealthStatus
            {
                IsHealthy = true,
                Timestamp = DateTime.UtcNow,
                BackgroundServiceStatus = "Native .NET Background Service (Active)", // Updated status
                QueueLengths = new Dictionary<string, int>
                {
                    ["processing"] = stats.TotalJobs - stats.SuccessfulJobs - stats.FailedJobs,
                    ["completed"] = stats.SuccessfulJobs,
                    ["failed"] = stats.FailedJobs,
                    ["retrying"] = stats.RetryingJobs
                },
                WorkerCount = Environment.ProcessorCount * 2,
                FailedJobCount = stats.FailedJobs,
                ProcessingJobCount = stats.TotalJobs - stats.SuccessfulJobs - stats.FailedJobs
            };

            return Ok(ApiResponse<BatchProcessingHealthStatus>.SuccessResult(health, "Native batch processing system is healthy"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get batch processing health status");

            // Return degraded health status
            var degradedHealth = new BatchProcessingHealthStatus
            {
                IsHealthy = false,
                Timestamp = DateTime.UtcNow,
                BackgroundServiceStatus = "Native .NET Background Service (Error)",
                QueueLengths = new Dictionary<string, int>(),
                WorkerCount = 0,
                FailedJobCount = 0,
                ProcessingJobCount = 0,
                ErrorMessage = ex.Message
            };

            return StatusCode(500, ApiResponse<BatchProcessingHealthStatus>.ErrorResult($"Failed to get health status: {ex.Message}"));
        }
    }
}

/// <summary>
/// Request model for scheduling recurring imports
/// </summary>
public class ScheduleRecurringImportRequest
{
    public required AllMarketsImportRequest ImportRequest { get; set; }
    public required string CronExpression { get; set; }
    public string? TimeZone { get; set; }
}

/// <summary>
/// Paged result wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Batch processing system health status
/// </summary>
public class BatchProcessingHealthStatus
{
    public bool IsHealthy { get; set; }
    public DateTime Timestamp { get; set; }
    public string BackgroundServiceStatus { get; set; } = string.Empty;
    public Dictionary<string, int> QueueLengths { get; set; } = new();
    public int WorkerCount { get; set; }
    public int FailedJobCount { get; set; }
    public int ProcessingJobCount { get; set; }
    public string? ErrorMessage { get; set; }
}