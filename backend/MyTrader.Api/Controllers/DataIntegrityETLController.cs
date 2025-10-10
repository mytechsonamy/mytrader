using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs;
using MyTrader.Core.Services.ETL;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Enterprise-grade Data Integrity ETL Controller
/// Provides comprehensive data synchronization, enrichment, and validation operations
/// with operational monitoring and SLA management
/// </summary>
[ApiController]
[Route("api/etl")]
// [Authorize] // Temporarily disabled for testing
public class DataIntegrityETLController : ControllerBase
{
    private readonly IDataIntegrityETLService _etlService;
    private readonly ISymbolSynchronizationService _symbolSyncService;
    private readonly IAssetEnrichmentService _enrichmentService;
    private readonly IMarketDataBootstrapService _bootstrapService;
    private readonly ILogger<DataIntegrityETLController> _logger;

    public DataIntegrityETLController(
        IDataIntegrityETLService etlService,
        ISymbolSynchronizationService symbolSyncService,
        IAssetEnrichmentService enrichmentService,
        IMarketDataBootstrapService bootstrapService,
        ILogger<DataIntegrityETLController> logger)
    {
        _etlService = etlService;
        _symbolSyncService = symbolSyncService;
        _enrichmentService = enrichmentService;
        _bootstrapService = bootstrapService;
        _logger = logger;
    }

    /// <summary>
    /// Execute complete data integrity ETL pipeline
    /// </summary>
    [HttpPost("execute-full-pipeline")]
    [ProducesResponseType(typeof(ApiResponse<DataIntegrityETLResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<DataIntegrityETLResult>>> ExecuteFullPipelineAsync(
        [FromBody] DataIntegrityETLOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting full data integrity ETL pipeline via API");

            var result = await _etlService.ExecuteFullPipelineAsync(options, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Full ETL pipeline completed successfully. Duration: {Duration}, Quality Score: {QualityScore:F1}",
                    result.Duration, result.DataQualityScore);

                return Ok(ApiResponse<DataIntegrityETLResult>.SuccessResult(result,
                    $"ETL pipeline completed successfully in {result.Duration.TotalMinutes:F1} minutes"));
            }
            else
            {
                _logger.LogWarning("Full ETL pipeline completed with issues: {ErrorMessage}", result.ErrorMessage);

                return Ok(ApiResponse<DataIntegrityETLResult>.SuccessResult(result,
                    $"ETL pipeline completed with issues: {result.ErrorMessage}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing full ETL pipeline");
            return StatusCode(500, ApiResponse<DataIntegrityETLResult>.ErrorResult($"ETL execution failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Execute symbol synchronization only
    /// </summary>
    [HttpPost("sync-symbols")]
    [ProducesResponseType(typeof(ApiResponse<DataIntegrityETLResult>), 200)]
    public async Task<ActionResult<ApiResponse<DataIntegrityETLResult>>> SyncSymbolsAsync(
        [FromBody] SymbolSyncOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting symbol synchronization via API");

            var result = await _etlService.ExecuteSymbolSyncOnlyAsync(options, cancellationToken);

            var message = result.Success ?
                $"Symbol sync completed. Added: {result.TotalSymbolsAdded}, Processed: {result.TotalSymbolsProcessed}" :
                $"Symbol sync completed with issues: {result.ErrorMessage}";

            return Ok(ApiResponse<DataIntegrityETLResult>.SuccessResult(result, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing symbol synchronization");
            return StatusCode(500, ApiResponse<DataIntegrityETLResult>.ErrorResult($"Symbol sync failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Execute asset enrichment only
    /// </summary>
    [HttpPost("enrich-assets")]
    [ProducesResponseType(typeof(ApiResponse<DataIntegrityETLResult>), 200)]
    public async Task<ActionResult<ApiResponse<DataIntegrityETLResult>>> EnrichAssetsAsync(
        [FromBody] EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting asset enrichment via API");

            var result = await _etlService.ExecuteEnrichmentOnlyAsync(options, cancellationToken);

            var message = result.Success ?
                $"Asset enrichment completed. Enriched: {result.TotalSymbolsEnriched} symbols" :
                $"Asset enrichment completed with issues: {result.ErrorMessage}";

            return Ok(ApiResponse<DataIntegrityETLResult>.SuccessResult(result, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing asset enrichment");
            return StatusCode(500, ApiResponse<DataIntegrityETLResult>.ErrorResult($"Asset enrichment failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Execute reference data bootstrap only
    /// </summary>
    [HttpPost("bootstrap-reference-data")]
    [ProducesResponseType(typeof(ApiResponse<DataIntegrityETLResult>), 200)]
    public async Task<ActionResult<ApiResponse<DataIntegrityETLResult>>> BootstrapReferenceDataAsync(
        [FromQuery] bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting reference data bootstrap via API (overwrite: {Overwrite})", overwriteExisting);

            var result = await _etlService.ExecuteBootstrapOnlyAsync(overwriteExisting, cancellationToken);

            var message = result.Success ?
                $"Reference data bootstrap completed. Created: {result.TotalReferenceDataItemsCreated} items" :
                $"Reference data bootstrap completed with issues: {result.ErrorMessage}";

            return Ok(ApiResponse<DataIntegrityETLResult>.SuccessResult(result, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing reference data bootstrap");
            return StatusCode(500, ApiResponse<DataIntegrityETLResult>.ErrorResult($"Reference data bootstrap failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get comprehensive data integrity status
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous] // Allow monitoring systems to check status
    [ProducesResponseType(typeof(ApiResponse<DataIntegrityStatus>), 200)]
    public async Task<ActionResult<ApiResponse<DataIntegrityStatus>>> GetDataIntegrityStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _etlService.GetDataIntegrityStatusAsync(cancellationToken);

            var message = status.IsSystemHealthy ?
                "Data integrity system is healthy" :
                $"Data integrity system has issues: {status.HealthSummary}";

            return Ok(ApiResponse<DataIntegrityStatus>.SuccessResult(status, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data integrity status");
            return StatusCode(500, ApiResponse<DataIntegrityStatus>.ErrorResult($"Failed to get status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get detailed status of symbol synchronization
    /// </summary>
    [HttpGet("symbol-sync-status")]
    [ProducesResponseType(typeof(ApiResponse<SymbolSyncStatus>), 200)]
    public async Task<ActionResult<ApiResponse<SymbolSyncStatus>>> GetSymbolSyncStatusAsync()
    {
        try
        {
            var status = await _symbolSyncService.GetSyncStatusAsync();

            var message = status.IsHealthy ?
                $"Symbol sync is healthy. {status.TotalSymbols} symbols, {status.MarketDataRecordsWithoutSymbols} orphaned records" :
                $"Symbol sync has issues. Health score: {status.SyncHealthScore:F1}%";

            return Ok(ApiResponse<SymbolSyncStatus>.SuccessResult(status, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbol sync status");
            return StatusCode(500, ApiResponse<SymbolSyncStatus>.ErrorResult($"Failed to get symbol sync status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get asset enrichment status
    /// </summary>
    [HttpGet("enrichment-status")]
    [ProducesResponseType(typeof(ApiResponse<EnrichmentStatus>), 200)]
    public async Task<ActionResult<ApiResponse<EnrichmentStatus>>> GetEnrichmentStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _enrichmentService.GetEnrichmentStatusAsync(cancellationToken: cancellationToken);

            var message = $"Enrichment status: {status.EnrichmentCompleteness:F1}% complete. " +
                         $"{status.FullyEnrichedSymbols} fully enriched, {status.UnenrichedSymbols} unenriched symbols";

            return Ok(ApiResponse<EnrichmentStatus>.SuccessResult(status, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enrichment status");
            return StatusCode(500, ApiResponse<EnrichmentStatus>.ErrorResult($"Failed to get enrichment status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get reference data bootstrap status
    /// </summary>
    [HttpGet("bootstrap-status")]
    [ProducesResponseType(typeof(ApiResponse<BootstrapStatus>), 200)]
    public async Task<ActionResult<ApiResponse<BootstrapStatus>>> GetBootstrapStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _bootstrapService.GetBootstrapStatusAsync(cancellationToken);

            var message = status.IsFullyBootstrapped ?
                "Reference data is fully bootstrapped" :
                $"Reference data missing components: {string.Join(", ", status.MissingComponents)}";

            return Ok(ApiResponse<BootstrapStatus>.SuccessResult(status, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bootstrap status");
            return StatusCode(500, ApiResponse<BootstrapStatus>.ErrorResult($"Failed to get bootstrap status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validate reference data integrity
    /// </summary>
    [HttpPost("validate-reference-data")]
    [ProducesResponseType(typeof(ApiResponse<ReferenceDataValidationResult>), 200)]
    public async Task<ActionResult<ApiResponse<ReferenceDataValidationResult>>> ValidateReferenceDataAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting reference data validation via API");

            var result = await _bootstrapService.ValidateReferenceDataAsync(cancellationToken);

            var message = result.IsValid ?
                "Reference data validation passed with no issues" :
                $"Reference data validation found {result.Issues.Count} issues";

            return Ok(ApiResponse<ReferenceDataValidationResult>.SuccessResult(result, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reference data");
            return StatusCode(500, ApiResponse<ReferenceDataValidationResult>.ErrorResult($"Validation failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get ETL execution history
    /// </summary>
    [HttpGet("execution-history")]
    [ProducesResponseType(typeof(ApiResponse<ETLExecutionHistory>), 200)]
    public async Task<ActionResult<ApiResponse<ETLExecutionHistory>>> GetExecutionHistoryAsync(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (limit <= 0 || limit > 200)
                limit = 50;

            var history = await _etlService.GetExecutionHistoryAsync(limit, cancellationToken);

            var message = $"Retrieved {history.ExecutionRecords.Count} execution records. " +
                         $"Success rate: {history.SuccessRate:F1}%";

            return Ok(ApiResponse<ETLExecutionHistory>.SuccessResult(history, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution history");
            return StatusCode(500, ApiResponse<ETLExecutionHistory>.ErrorResult($"Failed to get execution history: {ex.Message}"));
        }
    }

    /// <summary>
    /// Schedule recurring ETL operations
    /// </summary>
    [HttpPost("schedule-recurring")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    public async Task<ActionResult<ApiResponse<string>>> ScheduleRecurringETLAsync(
        [FromBody] ScheduleETLRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CronExpression))
            {
                return BadRequest(ApiResponse<string>.ErrorResult("Cron expression is required"));
            }

            _logger.LogInformation("Scheduling recurring ETL with cron expression: {CronExpression}", request.CronExpression);

            var scheduleId = await _etlService.ScheduleRecurringETLAsync(
                request.CronExpression,
                request.Options ?? new DataIntegrityETLOptions(),
                cancellationToken);

            return Ok(ApiResponse<string>.SuccessResult(scheduleId,
                $"ETL scheduled successfully with ID: {scheduleId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling recurring ETL");
            return StatusCode(500, ApiResponse<string>.ErrorResult($"Failed to schedule ETL: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get enrichment source health status
    /// </summary>
    [HttpGet("enrichment-sources-status")]
    [ProducesResponseType(typeof(ApiResponse<List<EnrichmentSourceStatus>>), 200)]
    public async Task<ActionResult<ApiResponse<List<EnrichmentSourceStatus>>>> GetEnrichmentSourcesStatusAsync()
    {
        try
        {
            var sources = await _enrichmentService.GetSourceStatusAsync();

            var healthySources = sources.Count(s => s.IsHealthy);
            var message = $"{healthySources}/{sources.Count} enrichment sources are healthy";

            return Ok(ApiResponse<List<EnrichmentSourceStatus>>.SuccessResult(sources, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enrichment sources status");
            return StatusCode(500, ApiResponse<List<EnrichmentSourceStatus>>.ErrorResult($"Failed to get sources status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validate and cleanup symbol data quality
    /// </summary>
    [HttpPost("validate-cleanup-symbols")]
    [ProducesResponseType(typeof(ApiResponse<SymbolValidationResult>), 200)]
    public async Task<ActionResult<ApiResponse<SymbolValidationResult>>> ValidateAndCleanupSymbolsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting symbol validation and cleanup via API");

            var result = await _symbolSyncService.ValidateAndCleanSymbolsAsync(cancellationToken);

            var message = result.Success ?
                $"Symbol validation completed. Validated: {result.TotalSymbolsValidated}, Fixed: {result.SymbolsFixed} issues" :
                $"Symbol validation failed: {result.ErrorMessage}";

            return Ok(ApiResponse<SymbolValidationResult>.SuccessResult(result, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating and cleaning up symbols");
            return StatusCode(500, ApiResponse<SymbolValidationResult>.ErrorResult($"Symbol validation failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Health check endpoint for monitoring systems
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 503)]
    public async Task<ActionResult> HealthCheckAsync()
    {
        try
        {
            var status = await _etlService.GetDataIntegrityStatusAsync();

            var healthInfo = new
            {
                Status = status.IsSystemHealthy ? "Healthy" : "Degraded",
                Timestamp = DateTime.UtcNow,
                Components = new
                {
                    SymbolSync = status.SymbolSyncStatus.IsHealthy ? "Healthy" : "Degraded",
                    Enrichment = status.EnrichmentStatus.EnrichmentCompleteness > 70 ? "Healthy" : "Degraded",
                    ReferenceData = status.BootstrapStatus.IsFullyBootstrapped ? "Healthy" : "Degraded"
                },
                Metrics = new
                {
                    OverallDataQuality = $"{status.OverallDataQuality:F1}%",
                    SymbolCoverage = $"{status.SymbolCoverage:F1}%",
                    EnrichmentCoverage = $"{status.EnrichmentCoverage:F1}%"
                },
                Issues = new
                {
                    Critical = status.CriticalIssues.Count,
                    Warnings = status.Warnings.Count
                }
            };

            if (status.IsSystemHealthy)
            {
                return Ok(healthInfo);
            }
            else
            {
                return StatusCode(503, healthInfo); // Service Unavailable
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in health check");
            return StatusCode(503, new
            {
                Status = "Error",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model for scheduling recurring ETL operations
/// </summary>
public class ScheduleETLRequest
{
    public required string CronExpression { get; set; }
    public DataIntegrityETLOptions? Options { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}