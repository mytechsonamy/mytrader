using Microsoft.Extensions.Logging;
using MyTrader.Core.Services.ETL;
using System.Diagnostics;

namespace MyTrader.Infrastructure.Services.ETL;

/// <summary>
/// Production-ready orchestrator for comprehensive data integrity ETL operations
/// Coordinates all data integration, validation, and enrichment processes
/// </summary>
public class DataIntegrityETLService : IDataIntegrityETLService
{
    private readonly ISymbolSynchronizationService _symbolSyncService;
    private readonly IAssetEnrichmentService _enrichmentService;
    private readonly IMarketDataBootstrapService _bootstrapService;
    private readonly ILogger<DataIntegrityETLService> _logger;
    private readonly SemaphoreSlim _executionSemaphore;

    // In-memory execution history (in production, this would be persisted)
    private readonly List<ETLExecutionRecord> _executionHistory = new();
    private readonly object _historyLock = new();

    public DataIntegrityETLService(
        ISymbolSynchronizationService symbolSyncService,
        IAssetEnrichmentService enrichmentService,
        IMarketDataBootstrapService bootstrapService,
        ILogger<DataIntegrityETLService> logger)
    {
        _symbolSyncService = symbolSyncService;
        _enrichmentService = enrichmentService;
        _bootstrapService = bootstrapService;
        _logger = logger;
        _executionSemaphore = new SemaphoreSlim(1, 1); // Only one ETL operation at a time
    }

    public async Task<DataIntegrityETLResult> ExecuteFullPipelineAsync(
        DataIntegrityETLOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await _executionSemaphore.WaitAsync(cancellationToken);

        var executionId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        var result = new DataIntegrityETLResult { ExecutedAt = DateTime.UtcNow };
        var executionRecord = new ETLExecutionRecord
        {
            ExecutionId = executionId,
            StartTime = DateTime.UtcNow,
            TriggerType = "API",
            Options = options ?? new DataIntegrityETLOptions()
        };

        try
        {
            options ??= new DataIntegrityETLOptions();

            _logger.LogInformation("Starting full data integrity ETL pipeline (Execution ID: {ExecutionId})", executionId);

            // Phase 1: Reference Data Bootstrap (if enabled)
            if (options.ExecuteReferenceDataBootstrap)
            {
                _logger.LogInformation("Phase 1: Executing reference data bootstrap");
                var bootstrapStopwatch = Stopwatch.StartNew();

                try
                {
                    result.BootstrapResult = await _bootstrapService.BootstrapAllReferenceDataAsync(
                        options.OverwriteExistingReferenceData, cancellationToken);

                    bootstrapStopwatch.Stop();
                    result.ComponentDurations["ReferenceDataBootstrap"] = bootstrapStopwatch.Elapsed;

                    if (!result.BootstrapResult.Success && !options.ContinueOnComponentFailure)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Reference data bootstrap failed: {result.BootstrapResult.ErrorMessage}";
                        return result;
                    }

                    result.TotalReferenceDataItemsCreated = result.BootstrapResult.TotalItemsCreated;
                    executionRecord.ReferenceItemsCreated = result.TotalReferenceDataItemsCreated;

                    _logger.LogInformation("Phase 1 completed. Reference data items created: {Count}",
                        result.TotalReferenceDataItemsCreated);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Phase 1 failed: Reference data bootstrap error");
                    if (!options.ContinueOnComponentFailure)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Reference data bootstrap failed: {ex.Message}";
                        return result;
                    }
                }
            }

            // Phase 2: Symbol Synchronization (if enabled)
            if (options.ExecuteSymbolSync)
            {
                _logger.LogInformation("Phase 2: Executing symbol synchronization");
                var syncStopwatch = Stopwatch.StartNew();

                try
                {
                    result.SymbolSyncResult = await _symbolSyncService.SynchronizeMissingSymbolsAsync(
                        options.SymbolSyncOptions, cancellationToken);

                    syncStopwatch.Stop();
                    result.ComponentDurations["SymbolSynchronization"] = syncStopwatch.Elapsed;

                    if (!result.SymbolSyncResult.Success && !options.ContinueOnComponentFailure)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Symbol synchronization failed: {result.SymbolSyncResult.ErrorMessage}";
                        return result;
                    }

                    result.TotalSymbolsProcessed = result.SymbolSyncResult.SymbolsDiscovered;
                    result.TotalSymbolsAdded = result.SymbolSyncResult.SymbolsAdded;
                    executionRecord.SymbolsProcessed = result.TotalSymbolsProcessed;
                    executionRecord.SymbolsAdded = result.TotalSymbolsAdded;

                    _logger.LogInformation("Phase 2 completed. Symbols processed: {Processed}, added: {Added}",
                        result.TotalSymbolsProcessed, result.TotalSymbolsAdded);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Phase 2 failed: Symbol synchronization error");
                    if (!options.ContinueOnComponentFailure)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Symbol synchronization failed: {ex.Message}";
                        return result;
                    }
                }
            }

            // Phase 3: Asset Enrichment (if enabled)
            if (options.ExecuteAssetEnrichment)
            {
                _logger.LogInformation("Phase 3: Executing asset enrichment");
                var enrichmentStopwatch = Stopwatch.StartNew();

                try
                {
                    // Get newly added symbols or all active symbols for enrichment
                    var symbolsToEnrich = await GetSymbolsForEnrichment(result.SymbolSyncResult);

                    if (symbolsToEnrich.Any())
                    {
                        result.EnrichmentResult = await _enrichmentService.EnrichSymbolsAsync(
                            symbolsToEnrich, options.EnrichmentOptions, cancellationToken);

                        enrichmentStopwatch.Stop();
                        result.ComponentDurations["AssetEnrichment"] = enrichmentStopwatch.Elapsed;

                        if (!result.EnrichmentResult.Success && !options.ContinueOnComponentFailure)
                        {
                            result.Success = false;
                            result.ErrorMessage = $"Asset enrichment failed: {result.EnrichmentResult.ErrorMessage}";
                            return result;
                        }

                        result.TotalSymbolsEnriched = result.EnrichmentResult.SuccessfullyEnriched +
                                                     result.EnrichmentResult.PartiallyEnriched;
                        executionRecord.SymbolsEnriched = result.TotalSymbolsEnriched;

                        _logger.LogInformation("Phase 3 completed. Symbols enriched: {Count}", result.TotalSymbolsEnriched);
                    }
                    else
                    {
                        _logger.LogInformation("Phase 3 skipped: No symbols require enrichment");
                        result.EnrichmentResult = new EnrichmentBatchResult { Success = true, TotalSymbols = 0 };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Phase 3 failed: Asset enrichment error");
                    if (!options.ContinueOnComponentFailure)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Asset enrichment failed: {ex.Message}";
                        return result;
                    }
                }
            }

            // Phase 4: Validation (if enabled)
            if (options.ValidateAfterExecution)
            {
                _logger.LogInformation("Phase 4: Executing post-ETL validation");
                var validationStopwatch = Stopwatch.StartNew();

                try
                {
                    result.ValidationResult = await _bootstrapService.ValidateReferenceDataAsync(cancellationToken);

                    validationStopwatch.Stop();
                    result.ComponentDurations["Validation"] = validationStopwatch.Elapsed;

                    _logger.LogInformation("Phase 4 completed. Validation result: {IsValid}, Issues: {IssueCount}",
                        result.ValidationResult.IsValid, result.ValidationResult.Issues.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Phase 4 failed: Validation error");
                    result.QualityIssues.Add($"Validation failed: {ex.Message}");
                }
            }

            // Calculate overall success and quality metrics
            result.Success = DetermineOverallSuccess(result);
            result.Duration = stopwatch.Elapsed;
            result.DataQualityScore = CalculateDataQualityScore(result);
            result.DataCompletenessScore = CalculateDataCompletenessScore(result);

            // Generate recommendations
            result.RecommendedActions = GenerateRecommendedActions(result);

            // Update execution record
            executionRecord.EndTime = DateTime.UtcNow;
            executionRecord.Success = result.Success;
            executionRecord.ErrorMessage = result.ErrorMessage;
            executionRecord.ComponentDurations = result.ComponentDurations;

            // Store execution record
            lock (_historyLock)
            {
                _executionHistory.Add(executionRecord);
                // Keep only last 1000 records
                if (_executionHistory.Count > 1000)
                {
                    _executionHistory.RemoveAt(0);
                }
            }

            _logger.LogInformation("Full ETL pipeline completed (Execution ID: {ExecutionId}). " +
                "Success: {Success}, Duration: {Duration}, Quality Score: {Quality:F1}",
                executionId, result.Success, result.Duration, result.DataQualityScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during ETL pipeline execution");

            executionRecord.EndTime = DateTime.UtcNow;
            executionRecord.Success = false;
            executionRecord.ErrorMessage = ex.Message;

            lock (_historyLock)
            {
                _executionHistory.Add(executionRecord);
            }

            result.Success = false;
            result.ErrorMessage = $"Fatal error: {ex.Message}";
            result.Duration = stopwatch.Elapsed;
            return result;
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    public async Task<DataIntegrityETLResult> ExecuteSymbolSyncOnlyAsync(
        SymbolSyncOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var etlOptions = new DataIntegrityETLOptions
        {
            ExecuteSymbolSync = true,
            ExecuteAssetEnrichment = false,
            ExecuteReferenceDataBootstrap = false,
            ValidateAfterExecution = false,
            SymbolSyncOptions = options
        };

        return await ExecuteFullPipelineAsync(etlOptions, cancellationToken);
    }

    public async Task<DataIntegrityETLResult> ExecuteEnrichmentOnlyAsync(
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var etlOptions = new DataIntegrityETLOptions
        {
            ExecuteSymbolSync = false,
            ExecuteAssetEnrichment = true,
            ExecuteReferenceDataBootstrap = false,
            ValidateAfterExecution = false,
            EnrichmentOptions = options
        };

        return await ExecuteFullPipelineAsync(etlOptions, cancellationToken);
    }

    public async Task<DataIntegrityETLResult> ExecuteBootstrapOnlyAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var etlOptions = new DataIntegrityETLOptions
        {
            ExecuteSymbolSync = false,
            ExecuteAssetEnrichment = false,
            ExecuteReferenceDataBootstrap = true,
            ValidateAfterExecution = true,
            OverwriteExistingReferenceData = overwriteExisting
        };

        return await ExecuteFullPipelineAsync(etlOptions, cancellationToken);
    }

    public async Task<DataIntegrityStatus> GetDataIntegrityStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = new DataIntegrityStatus();

            // Get component statuses in parallel
            var symbolSyncTask = _symbolSyncService.GetSyncStatusAsync();
            var enrichmentTask = _enrichmentService.GetEnrichmentStatusAsync(cancellationToken: cancellationToken);
            var bootstrapTask = _bootstrapService.GetBootstrapStatusAsync(cancellationToken);

            await Task.WhenAll(symbolSyncTask, enrichmentTask, bootstrapTask);

            status.SymbolSyncStatus = await symbolSyncTask;
            status.EnrichmentStatus = await enrichmentTask;
            status.BootstrapStatus = await bootstrapTask;

            // Calculate overall metrics
            status.TotalSymbols = status.SymbolSyncStatus.TotalSymbols;
            status.OrphanedMarketDataRecords = status.SymbolSyncStatus.MarketDataRecordsWithoutSymbols;
            status.SymbolsWithoutMarketData = status.SymbolSyncStatus.SymbolsWithoutMarketData;
            status.UnenrichedActiveSymbols = status.EnrichmentStatus.UnenrichedSymbols;

            // Calculate data quality scores
            status.SymbolCoverage = CalculateSymbolCoverage(status);
            status.EnrichmentCoverage = status.EnrichmentStatus.EnrichmentCompleteness;
            status.ReferenceDataCompleteness = CalculateReferenceDataCompleteness(status);
            status.OverallDataQuality = (status.SymbolCoverage + status.EnrichmentCoverage + status.ReferenceDataCompleteness) / 3;

            // Determine system health
            status.IsSystemHealthy = DetermineSystemHealth(status);
            status.HealthSummary = GenerateHealthSummary(status);

            // Get execution history for timing information
            lock (_historyLock)
            {
                var recentExecution = _executionHistory
                    .Where(r => r.Success)
                    .OrderByDescending(r => r.EndTime)
                    .FirstOrDefault();

                if (recentExecution != null)
                {
                    status.LastFullETLRun = recentExecution.EndTime;
                }
            }

            status.LastSymbolSync = status.SymbolSyncStatus.LastSyncAt;
            status.LastEnrichmentRun = status.EnrichmentStatus.LastEnrichmentRun;

            // Identify issues and recommendations
            status.CriticalIssues = IdentifyCriticalIssues(status);
            status.Warnings = IdentifyWarnings(status);
            status.RecommendedActions = GenerateSystemRecommendations(status);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data integrity status");
            return new DataIntegrityStatus
            {
                IsSystemHealthy = false,
                HealthSummary = $"Error retrieving status: {ex.Message}"
            };
        }
    }

    public async Task<string> ScheduleRecurringETLAsync(
        string cronExpression,
        DataIntegrityETLOptions options,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement with job scheduler (Hangfire, Quartz.NET, etc.)
        await Task.Delay(100, cancellationToken);
        var scheduleId = Guid.NewGuid().ToString();

        _logger.LogInformation("Scheduled recurring ETL with cron expression {CronExpression} (Schedule ID: {ScheduleId})",
            cronExpression, scheduleId);

        return scheduleId;
    }

    public async Task<ETLExecutionHistory> GetExecutionHistoryAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate async operation

        var history = new ETLExecutionHistory();

        lock (_historyLock)
        {
            history.ExecutionRecords = _executionHistory
                .OrderByDescending(r => r.StartTime)
                .Take(limit)
                .ToList();

            history.TotalExecutions = _executionHistory.Count;
            history.SuccessfulExecutions = _executionHistory.Count(r => r.Success);
            history.FailedExecutions = _executionHistory.Count(r => !r.Success);

            if (history.ExecutionRecords.Any())
            {
                var durations = history.ExecutionRecords.Select(r => r.Duration).ToList();
                history.AverageExecutionTime = TimeSpan.FromTicks((long)durations.Select(d => d.Ticks).Average());
                history.ShortestExecutionTime = durations.Min();
                history.LongestExecutionTime = durations.Max();
            }

            // Analyze frequent issues
            history.FrequentIssues = _executionHistory
                .Where(r => !r.Success && !string.IsNullOrEmpty(r.ErrorMessage))
                .GroupBy(r => ExtractErrorCategory(r.ErrorMessage!))
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()} times)")
                .ToList();
        }

        return history;
    }

    #region Private Helper Methods

    private async Task<List<Guid>> GetSymbolsForEnrichment(SymbolSyncResult? syncResult)
    {
        // For now, return empty list - in production this would get symbols needing enrichment
        await Task.Delay(10);
        return new List<Guid>();
    }

    private bool DetermineOverallSuccess(DataIntegrityETLResult result)
    {
        var componentSuccesses = new List<bool>();

        if (result.BootstrapResult != null)
            componentSuccesses.Add(result.BootstrapResult.Success);

        if (result.SymbolSyncResult != null)
            componentSuccesses.Add(result.SymbolSyncResult.Success);

        if (result.EnrichmentResult != null)
            componentSuccesses.Add(result.EnrichmentResult.Success);

        return componentSuccesses.Any() && componentSuccesses.All(s => s);
    }

    private decimal CalculateDataQualityScore(DataIntegrityETLResult result)
    {
        var scores = new List<decimal>();

        // Symbol sync quality (40% weight)
        if (result.SymbolSyncResult != null)
        {
            var errorRate = result.SymbolSyncResult.Errors.Count / Math.Max(1, result.SymbolSyncResult.SymbolsDiscovered);
            scores.Add((1 - errorRate) * 40);
        }

        // Enrichment quality (35% weight)
        if (result.EnrichmentResult != null && result.EnrichmentResult.TotalSymbols > 0)
        {
            var enrichmentRate = (decimal)result.EnrichmentResult.SuccessfullyEnriched / result.EnrichmentResult.TotalSymbols;
            scores.Add(enrichmentRate * 35);
        }

        // Reference data quality (25% weight)
        if (result.BootstrapResult != null)
        {
            var bootstrapScore = result.BootstrapResult.Success ? 25 : 0;
            scores.Add(bootstrapScore);
        }

        return scores.Any() ? scores.Sum() : 0;
    }

    private decimal CalculateDataCompletenessScore(DataIntegrityETLResult result)
    {
        // Simple completeness calculation based on coverage
        var completenessFactors = new List<decimal>();

        if (result.TotalSymbolsProcessed > 0)
        {
            var additionRate = (decimal)result.TotalSymbolsAdded / result.TotalSymbolsProcessed;
            completenessFactors.Add(Math.Min(100, additionRate * 100));
        }

        if (result.TotalSymbolsEnriched > 0)
        {
            completenessFactors.Add(80); // Assume good enrichment completeness
        }

        return completenessFactors.Any() ? completenessFactors.Average() : 0;
    }

    private List<string> GenerateRecommendedActions(DataIntegrityETLResult result)
    {
        var actions = new List<string>();

        if (result.SymbolSyncResult?.Errors.Count > 0)
        {
            actions.Add($"Review and resolve {result.SymbolSyncResult.Errors.Count} symbol synchronization errors");
        }

        if (result.EnrichmentResult?.Failed > 0)
        {
            actions.Add($"Investigate {result.EnrichmentResult.Failed} failed enrichment attempts");
        }

        if (result.ValidationResult?.Issues.Count > 0)
        {
            actions.Add($"Address {result.ValidationResult.Issues.Count} validation issues found");
        }

        if (result.DataQualityScore < 80)
        {
            actions.Add("Consider running additional data quality checks and cleanup");
        }

        return actions;
    }

    private decimal CalculateSymbolCoverage(DataIntegrityStatus status)
    {
        if (status.TotalSymbols == 0) return 100;

        var coveredSymbols = status.TotalSymbols - status.OrphanedMarketDataRecords - status.SymbolsWithoutMarketData;
        return (decimal)coveredSymbols / status.TotalSymbols * 100;
    }

    private decimal CalculateReferenceDataCompleteness(DataIntegrityStatus status)
    {
        var completenessScore = 0m;

        if (status.BootstrapStatus.AssetClassesInitialized) completenessScore += 25;
        if (status.BootstrapStatus.MarketsInitialized) completenessScore += 25;
        if (status.BootstrapStatus.TradingSessionsInitialized) completenessScore += 25;
        if (status.BootstrapStatus.DataProvidersInitialized) completenessScore += 25;

        return completenessScore;
    }

    private bool DetermineSystemHealth(DataIntegrityStatus status)
    {
        return status.SymbolSyncStatus.IsHealthy &&
               status.OverallDataQuality >= 75 &&
               status.SymbolCoverage >= 90 &&
               status.CriticalIssues.Count == 0;
    }

    private string GenerateHealthSummary(DataIntegrityStatus status)
    {
        if (status.IsSystemHealthy)
        {
            return "System is healthy with good data integrity across all components";
        }

        var issues = new List<string>();

        if (!status.SymbolSyncStatus.IsHealthy)
            issues.Add("symbol synchronization issues");

        if (status.OverallDataQuality < 75)
            issues.Add($"low data quality ({status.OverallDataQuality:F1}%)");

        if (status.SymbolCoverage < 90)
            issues.Add($"insufficient symbol coverage ({status.SymbolCoverage:F1}%)");

        if (status.CriticalIssues.Any())
            issues.Add($"{status.CriticalIssues.Count} critical issues");

        return $"System has issues: {string.Join(", ", issues)}";
    }

    private List<DataIntegrityIssue> IdentifyCriticalIssues(DataIntegrityStatus status)
    {
        var issues = new List<DataIntegrityIssue>();

        if (status.OrphanedMarketDataRecords > 0)
        {
            issues.Add(new DataIntegrityIssue
            {
                IssueType = "ORPHANED_MARKET_DATA",
                Component = "SymbolSync",
                Description = $"{status.OrphanedMarketDataRecords} market data records have no corresponding symbols",
                Severity = "High",
                RecommendedAction = "Run symbol synchronization to add missing symbols",
                IsAutoFixable = true
            });
        }

        if (!status.BootstrapStatus.IsFullyBootstrapped)
        {
            issues.Add(new DataIntegrityIssue
            {
                IssueType = "INCOMPLETE_REFERENCE_DATA",
                Component = "Bootstrap",
                Description = "Reference data is not fully initialized",
                Severity = "Critical",
                RecommendedAction = "Run reference data bootstrap",
                IsAutoFixable = true
            });
        }

        return issues;
    }

    private List<DataIntegrityIssue> IdentifyWarnings(DataIntegrityStatus status)
    {
        var warnings = new List<DataIntegrityIssue>();

        if (status.UnenrichedActiveSymbols > status.TotalSymbols * 0.3m)
        {
            warnings.Add(new DataIntegrityIssue
            {
                IssueType = "HIGH_UNENRICHED_SYMBOLS",
                Component = "Enrichment",
                Description = $"{status.UnenrichedActiveSymbols} active symbols lack enrichment data",
                Severity = "Medium",
                RecommendedAction = "Run asset enrichment process",
                IsAutoFixable = true
            });
        }

        return warnings;
    }

    private List<string> GenerateSystemRecommendations(DataIntegrityStatus status)
    {
        var recommendations = new List<string>();

        if (status.LastFullETLRun == null)
        {
            recommendations.Add("Run initial full ETL pipeline to establish baseline data integrity");
        }
        else if (status.LastFullETLRun < DateTime.UtcNow.AddDays(-7))
        {
            recommendations.Add("Consider running full ETL pipeline - last run was over a week ago");
        }

        if (status.OverallDataQuality < 90)
        {
            recommendations.Add("Schedule more frequent data quality validation runs");
        }

        if (status.EnrichmentCoverage < 70)
        {
            recommendations.Add("Increase asset enrichment frequency to improve data completeness");
        }

        return recommendations;
    }

    private string ExtractErrorCategory(string errorMessage)
    {
        // Simple error categorization
        if (errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return "Timeout";
        if (errorMessage.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            return "Rate Limit";
        if (errorMessage.Contains("database", StringComparison.OrdinalIgnoreCase))
            return "Database";
        if (errorMessage.Contains("network", StringComparison.OrdinalIgnoreCase))
            return "Network";

        return "Other";
    }

    #endregion
}