using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Api.Controllers;

/// <summary>
/// API controller for data import operations from Stock_Scrapper
/// Provides endpoints for importing CSV data, validation, and monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Allow anonymous access for data import operations
public class DataImportController : ControllerBase
{
    private readonly IDataImportService _dataImportService;
    private readonly ILogger<DataImportController> _logger;

    public DataImportController(
        IDataImportService dataImportService,
        ILogger<DataImportController> logger)
    {
        _dataImportService = dataImportService;
        _logger = logger;
    }

    /// <summary>
    /// Import data from a single CSV file
    /// </summary>
    /// <param name="request">Import request with file path and data source</param>
    /// <returns>Import result with statistics</returns>
    [HttpPost("import-csv")]
    public async Task<ActionResult<ApiResponse<DataImportResultDto>>> ImportFromCsvAsync(
        [FromBody] ImportCsvRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting CSV import for file: {FilePath}, DataSource: {DataSource}",
                request.FilePath, request.DataSource);

            var result = await _dataImportService.ImportFromCsvAsync(
                request.FilePath,
                request.DataSource,
                cancellationToken: HttpContext.RequestAborted);

            return Ok(new ApiResponse<DataImportResultDto>
            {
                Success = result.Success,
                Data = result,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV import from {FilePath}", request.FilePath);
            return StatusCode(500, new ApiResponse<DataImportResultDto>
            {
                Success = false,
                Message = $"Import failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Import data from a directory containing CSV files
    /// </summary>
    /// <param name="request">Import request with directory path and data source</param>
    /// <returns>Import result with aggregated statistics</returns>
    [HttpPost("import-directory")]
    public async Task<ActionResult<ApiResponse<DataImportResultDto>>> ImportFromDirectoryAsync(
        [FromBody] ImportDirectoryRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting directory import for: {DirectoryPath}, DataSource: {DataSource}",
                request.DirectoryPath, request.DataSource);

            var result = await _dataImportService.ImportFromDirectoryAsync(
                request.DirectoryPath,
                request.DataSource,
                cancellationToken: HttpContext.RequestAborted);

            return Ok(new ApiResponse<DataImportResultDto>
            {
                Success = result.Success,
                Data = result,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during directory import from {DirectoryPath}", request.DirectoryPath);
            return StatusCode(500, new ApiResponse<DataImportResultDto>
            {
                Success = false,
                Message = $"Directory import failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Import all markets from Stock_Scrapper DATA directory
    /// </summary>
    /// <param name="request">Import request with Stock_Scrapper DATA directory path</param>
    /// <returns>Import results for all markets</returns>
    [HttpPost("import-all-markets")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, DataImportResultDto>>>> ImportAllMarketsAsync(
        [FromBody] ImportAllMarketsRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting import of all markets from: {StockScrapperDataPath}",
                request.StockScrapperDataPath);

            var results = await _dataImportService.ImportAllMarketsAsync(
                request.StockScrapperDataPath,
                cancellationToken: HttpContext.RequestAborted);

            var allSuccessful = results.Values.All(r => r.Success);

            return Ok(new ApiResponse<Dictionary<string, DataImportResultDto>>
            {
                Success = allSuccessful,
                Data = results,
                Message = allSuccessful ?
                    "All markets imported successfully" :
                    "Some markets failed to import - check individual results"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during all markets import from {StockScrapperDataPath}",
                request.StockScrapperDataPath);
            return StatusCode(500, new ApiResponse<Dictionary<string, DataImportResultDto>>
            {
                Success = false,
                Message = $"All markets import failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Start batch ETL process for Stock_Scrapper data with progress monitoring
    /// </summary>
    /// <param name="request">Batch ETL request with configurations</param>
    /// <returns>Batch job ID for tracking progress</returns>
    [HttpPost("start-batch-etl")]
    public async Task<ActionResult<ApiResponse<BatchEtlJobDto>>> StartBatchEtlAsync(
        [FromBody] StartBatchEtlRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting batch ETL process for Stock_Scrapper data from: {StockScrapperDataPath}",
                request.StockScrapperDataPath);

            var jobId = Guid.NewGuid();
            var startTime = DateTime.UtcNow;

            // Create progress tracking for SignalR or background task
            var batchJob = new BatchEtlJobDto
            {
                JobId = jobId,
                Status = BatchEtlStatus.Running,
                StartTime = startTime,
                Configuration = request,
                TotalFilesExpected = await CountExpectedFilesAsync(request.StockScrapperDataPath),
                Progress = new BatchEtlProgressDto
                {
                    Operation = "Initializing batch ETL process",
                    CurrentMarket = "",
                    MarketsProcessed = 0,
                    TotalMarkets = 4, // BIST, Crypto, NASDAQ, NYSE
                    FilesProcessed = 0,
                    TotalFiles = 0,
                    RecordsProcessed = 0,
                    RecordsImported = 0,
                    ProcessingRate = 0,
                    EstimatedTimeRemaining = TimeSpan.Zero
                }
            };

            // Start background task for batch processing
            _ = Task.Run(async () => await ProcessBatchEtlAsync(jobId, request));

            return Ok(new ApiResponse<BatchEtlJobDto>
            {
                Success = true,
                Data = batchJob,
                Message = "Batch ETL process started successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting batch ETL process");
            return StatusCode(500, new ApiResponse<BatchEtlJobDto>
            {
                Success = false,
                Message = $"Failed to start batch ETL: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get batch ETL job status and progress
    /// </summary>
    /// <param name="jobId">Batch job identifier</param>
    /// <returns>Current job status and progress</returns>
    [HttpGet("batch-etl-status/{jobId}")]
    public async Task<ActionResult<ApiResponse<BatchEtlJobDto>>> GetBatchEtlStatusAsync(
        [FromRoute] Guid jobId)
    {
        try
        {
            // In a real implementation, this would retrieve from cache/database
            // For demo purposes, return a sample status
            var batchJob = await GetBatchJobStatusAsync(jobId);

            if (batchJob == null)
            {
                return NotFound(new ApiResponse<BatchEtlJobDto>
                {
                    Success = false,
                    Message = "Batch job not found"
                });
            }

            return Ok(new ApiResponse<BatchEtlJobDto>
            {
                Success = true,
                Data = batchJob,
                Message = "Job status retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch ETL status for job {JobId}", jobId);
            return StatusCode(500, new ApiResponse<BatchEtlJobDto>
            {
                Success = false,
                Message = $"Error retrieving job status: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Validate a CSV file format without importing
    /// </summary>
    /// <param name="request">Validation request with file path</param>
    /// <returns>Validation result with format detection and errors</returns>
    [HttpPost("validate-csv")]
    public async Task<ActionResult<ApiResponse<DataValidationResultDto>>> ValidateCsvFileAsync(
        [FromBody] ValidateCsvRequestDto request)
    {
        try
        {
            _logger.LogDebug("Validating CSV file: {FilePath}", request.FilePath);

            var result = await _dataImportService.ValidateCsvFileAsync(request.FilePath);

            return Ok(new ApiResponse<DataValidationResultDto>
            {
                Success = result.IsValid,
                Data = result,
                Message = result.IsValid ? "File validation successful" : "File validation failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating CSV file {FilePath}", request.FilePath);
            return StatusCode(500, new ApiResponse<DataValidationResultDto>
            {
                Success = false,
                Message = $"Validation failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get duplicate records for a symbol and date range
    /// </summary>
    /// <param name="symbolTicker">Symbol ticker</param>
    /// <param name="startDate">Start date (YYYY-MM-DD)</param>
    /// <param name="endDate">End date (YYYY-MM-DD)</param>
    /// <param name="dataSource">Optional data source filter</param>
    /// <returns>List of duplicate records</returns>
    [HttpGet("duplicates/{symbolTicker}")]
    public async Task<ActionResult<ApiResponse<List<HistoricalMarketData>>>> GetDuplicateRecordsAsync(
        [FromRoute] string symbolTicker,
        [FromQuery, Required] string startDate,
        [FromQuery, Required] string endDate,
        [FromQuery] string? dataSource = null)
    {
        try
        {
            if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            {
                return BadRequest(new ApiResponse<List<HistoricalMarketData>>
                {
                    Success = false,
                    Message = "Invalid date format. Use YYYY-MM-DD format."
                });
            }

            var duplicates = await _dataImportService.GetDuplicateRecordsAsync(
                symbolTicker, start, end, dataSource);

            return Ok(new ApiResponse<List<HistoricalMarketData>>
            {
                Success = true,
                Data = duplicates,
                Message = $"Found {duplicates.Count} duplicate records"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting duplicates for {SymbolTicker}", symbolTicker);
            return StatusCode(500, new ApiResponse<List<HistoricalMarketData>>
            {
                Success = false,
                Message = $"Error retrieving duplicates: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Clean duplicate records for a symbol and date range
    /// </summary>
    /// <param name="request">Cleanup request with symbol, date range, and dry run option</param>
    /// <returns>Cleanup result with statistics</returns>
    [HttpPost("clean-duplicates")]
    public async Task<ActionResult<ApiResponse<DataCleanupResultDto>>> CleanDuplicateRecordsAsync(
        [FromBody] CleanDuplicatesRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting duplicate cleanup for {SymbolTicker} from {StartDate} to {EndDate} (DryRun: {DryRun})",
                request.SymbolTicker, request.StartDate, request.EndDate, request.DryRun);

            var result = await _dataImportService.CleanDuplicateRecordsAsync(
                request.SymbolTicker,
                request.StartDate,
                request.EndDate,
                request.DryRun);

            return Ok(new ApiResponse<DataCleanupResultDto>
            {
                Success = result.Success,
                Data = result,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning duplicates for {SymbolTicker}", request.SymbolTicker);
            return StatusCode(500, new ApiResponse<DataCleanupResultDto>
            {
                Success = false,
                Message = $"Cleanup failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get import statistics for monitoring and analytics
    /// </summary>
    /// <param name="startDate">Start date (YYYY-MM-DD)</param>
    /// <param name="endDate">End date (YYYY-MM-DD)</param>
    /// <returns>Import statistics</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<DataImportStatsDto>>> GetImportStatisticsAsync(
        [FromQuery, Required] string startDate,
        [FromQuery, Required] string endDate)
    {
        try
        {
            if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            {
                return BadRequest(new ApiResponse<DataImportStatsDto>
                {
                    Success = false,
                    Message = "Invalid date format. Use YYYY-MM-DD format."
                });
            }

            var stats = await _dataImportService.GetImportStatisticsAsync(start, end);

            return Ok(new ApiResponse<DataImportStatsDto>
            {
                Success = true,
                Data = stats,
                Message = "Statistics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import statistics");
            return StatusCode(500, new ApiResponse<DataImportStatsDto>
            {
                Success = false,
                Message = $"Error retrieving statistics: {ex.Message}"
            });
        }
    }

    #region Private Helper Methods

    private async Task<int> CountExpectedFilesAsync(string stockScrapperDataPath)
    {
        var totalFiles = 0;
        var markets = new[] { "BIST", "Crypto", "NASDAQ", "NYSE" };

        foreach (var market in markets)
        {
            var marketPath = Path.Combine(stockScrapperDataPath, market);
            if (Directory.Exists(marketPath))
            {
                totalFiles += Directory.GetFiles(marketPath, "*.csv", SearchOption.TopDirectoryOnly).Length;
            }
        }

        return totalFiles;
    }

    private async Task ProcessBatchEtlAsync(Guid jobId, StartBatchEtlRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting background batch ETL process for job {JobId}", jobId);

            var progress = new Progress<DataImportProgressDto>(progressUpdate =>
            {
                _logger.LogInformation("Batch ETL Progress: {Operation} - {FilesProcessed}/{TotalFiles} files",
                    progressUpdate.Operation, progressUpdate.FilesProcessed, progressUpdate.TotalFiles);

                // In real implementation, update cache/database and notify via SignalR
                // UpdateBatchJobProgress(jobId, progressUpdate);
            });

            var results = await _dataImportService.ImportAllMarketsAsync(
                request.StockScrapperDataPath,
                progress);

            var allSuccessful = results.Values.All(r => r.Success);
            var totalRecordsImported = results.Values.Sum(r => r.RecordsImported);

            _logger.LogInformation("Batch ETL job {JobId} completed. Success: {AllSuccessful}, Records imported: {TotalRecordsImported}",
                jobId, allSuccessful, totalRecordsImported);

            // Update final status
            // UpdateBatchJobStatus(jobId, allSuccessful ? BatchEtlStatus.Completed : BatchEtlStatus.Failed, results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch ETL job {JobId} failed", jobId);
            // UpdateBatchJobStatus(jobId, BatchEtlStatus.Failed, null, ex.Message);
        }
    }

    private async Task<BatchEtlJobDto?> GetBatchJobStatusAsync(Guid jobId)
    {
        // In real implementation, retrieve from cache/database
        // For demo purposes, return a sample job
        return new BatchEtlJobDto
        {
            JobId = jobId,
            Status = BatchEtlStatus.Running,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            Progress = new BatchEtlProgressDto
            {
                Operation = "Processing BIST market data",
                CurrentMarket = "BIST",
                MarketsProcessed = 1,
                TotalMarkets = 4,
                FilesProcessed = 25,
                TotalFiles = 220,
                RecordsProcessed = 50000,
                RecordsImported = 48500,
                ProcessingRate = 950,
                EstimatedTimeRemaining = TimeSpan.FromMinutes(15)
            }
        };
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Request DTO for importing a single CSV file
/// </summary>
public class ImportCsvRequestDto
{
    /// <summary>
    /// Full path to the CSV file
    /// </summary>
    [Required]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Data source identifier (BIST, CRYPTO, NASDAQ, NYSE)
    /// </summary>
    [Required]
    public string DataSource { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for importing from a directory
/// </summary>
public class ImportDirectoryRequestDto
{
    /// <summary>
    /// Directory path containing CSV files
    /// </summary>
    [Required]
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Data source identifier (BIST, CRYPTO, NASDAQ, NYSE)
    /// </summary>
    [Required]
    public string DataSource { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for importing all markets
/// </summary>
public class ImportAllMarketsRequestDto
{
    /// <summary>
    /// Path to Stock_Scrapper DATA directory
    /// </summary>
    [Required]
    public string StockScrapperDataPath { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for starting batch ETL process
/// </summary>
public class StartBatchEtlRequestDto
{
    /// <summary>
    /// Path to Stock_Scrapper DATA directory
    /// </summary>
    [Required]
    public string StockScrapperDataPath { get; set; } = string.Empty;

    /// <summary>
    /// Maximum retry attempts for failed files
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Batch size for database operations
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Enable parallel processing
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// Maximum degree of parallelism
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Skip validation for faster processing
    /// </summary>
    public bool SkipValidation { get; set; } = false;

    /// <summary>
    /// Clean duplicates after import
    /// </summary>
    public bool CleanDuplicatesAfterImport { get; set; } = true;
}

/// <summary>
/// Batch ETL job status and progress tracking
/// </summary>
public class BatchEtlJobDto
{
    /// <summary>
    /// Unique job identifier
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Current job status
    /// </summary>
    public BatchEtlStatus Status { get; set; }

    /// <summary>
    /// Job start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Job completion time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total processing time
    /// </summary>
    public TimeSpan? TotalProcessingTime => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// Job configuration
    /// </summary>
    public StartBatchEtlRequestDto? Configuration { get; set; }

    /// <summary>
    /// Expected total files to process
    /// </summary>
    public int TotalFilesExpected { get; set; }

    /// <summary>
    /// Current progress information
    /// </summary>
    public BatchEtlProgressDto? Progress { get; set; }

    /// <summary>
    /// Final results by market (available when completed)
    /// </summary>
    public Dictionary<string, DataImportResultDto>? Results { get; set; }

    /// <summary>
    /// Error message if job failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed error logs
    /// </summary>
    public List<string> ErrorLogs { get; set; } = new List<string>();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public BatchEtlMetricsDto? Metrics { get; set; }
}

/// <summary>
/// Real-time progress information for batch ETL
/// </summary>
public class BatchEtlProgressDto
{
    /// <summary>
    /// Current operation description
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Currently processing market
    /// </summary>
    public string CurrentMarket { get; set; } = string.Empty;

    /// <summary>
    /// Currently processing file
    /// </summary>
    public string? CurrentFile { get; set; }

    /// <summary>
    /// Markets processed so far
    /// </summary>
    public int MarketsProcessed { get; set; }

    /// <summary>
    /// Total markets to process
    /// </summary>
    public int TotalMarkets { get; set; }

    /// <summary>
    /// Files processed so far
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// Total files to process
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Records processed so far
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Records successfully imported
    /// </summary>
    public long RecordsImported { get; set; }

    /// <summary>
    /// Records skipped (duplicates, errors)
    /// </summary>
    public long RecordsSkipped { get; set; }

    /// <summary>
    /// Current processing rate (records/second)
    /// </summary>
    public decimal ProcessingRate { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Overall completion percentage (0-100)
    /// </summary>
    public decimal CompletionPercentage => TotalFiles > 0 ?
        (decimal)FilesProcessed / TotalFiles * 100 : 0;
}

/// <summary>
/// Performance and quality metrics for batch ETL
/// </summary>
public class BatchEtlMetricsDto
{
    /// <summary>
    /// Average processing rate across all files
    /// </summary>
    public decimal AvgProcessingRate { get; set; }

    /// <summary>
    /// Peak processing rate achieved
    /// </summary>
    public decimal PeakProcessingRate { get; set; }

    /// <summary>
    /// Overall data quality score (0-100)
    /// </summary>
    public decimal OverallQualityScore { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public decimal SuccessRatePercentage { get; set; }

    /// <summary>
    /// Memory usage statistics
    /// </summary>
    public BatchEtlResourceUsageDto? ResourceUsage { get; set; }

    /// <summary>
    /// Database performance metrics
    /// </summary>
    public BatchEtlDatabaseMetricsDto? DatabaseMetrics { get; set; }
}

/// <summary>
/// Resource usage metrics during batch ETL
/// </summary>
public class BatchEtlResourceUsageDto
{
    /// <summary>
    /// Peak memory usage in MB
    /// </summary>
    public long PeakMemoryUsageMB { get; set; }

    /// <summary>
    /// Average CPU usage percentage
    /// </summary>
    public decimal AvgCpuUsagePercentage { get; set; }

    /// <summary>
    /// Peak CPU usage percentage
    /// </summary>
    public decimal PeakCpuUsagePercentage { get; set; }

    /// <summary>
    /// Total disk I/O operations
    /// </summary>
    public long TotalDiskIOOperations { get; set; }
}

/// <summary>
/// Database performance metrics during batch ETL
/// </summary>
public class BatchEtlDatabaseMetricsDto
{
    /// <summary>
    /// Total database connection time
    /// </summary>
    public TimeSpan TotalConnectionTime { get; set; }

    /// <summary>
    /// Average query execution time
    /// </summary>
    public TimeSpan AvgQueryExecutionTime { get; set; }

    /// <summary>
    /// Total database operations
    /// </summary>
    public long TotalDatabaseOperations { get; set; }

    /// <summary>
    /// Failed database operations
    /// </summary>
    public long FailedDatabaseOperations { get; set; }
}

/// <summary>
/// Batch ETL job status enumeration
/// </summary>
public enum BatchEtlStatus
{
    /// <summary>
    /// Job is queued and waiting to start
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Job is currently running
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job completed with errors
    /// </summary>
    CompletedWithErrors = 3,

    /// <summary>
    /// Job failed
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Job was cancelled
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Job is paused
    /// </summary>
    Paused = 6
}

/// <summary>
/// Request DTO for validating a CSV file
/// </summary>
public class ValidateCsvRequestDto
{
    /// <summary>
    /// Full path to the CSV file to validate
    /// </summary>
    [Required]
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for cleaning duplicate records
/// </summary>
public class CleanDuplicatesRequestDto
{
    /// <summary>
    /// Symbol ticker to clean
    /// </summary>
    [Required]
    public string SymbolTicker { get; set; } = string.Empty;

    /// <summary>
    /// Start date for cleanup range
    /// </summary>
    [Required]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date for cleanup range
    /// </summary>
    [Required]
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// If true, only report what would be deleted without actually deleting
    /// </summary>
    public bool DryRun { get; set; } = true;
}

#endregion