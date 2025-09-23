using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Background service for automated data import operations
/// Can be triggered via configuration or run on a schedule
/// </summary>
public class DataImportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataImportBackgroundService> _logger;
    private readonly DataImportConfiguration _configuration;

    public DataImportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DataImportBackgroundService> logger,
        IOptions<DataImportConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for initial delay if configured
        if (_configuration.InitialDelayMinutes > 0)
        {
            _logger.LogInformation("Waiting {InitialDelayMinutes} minutes before starting data import service",
                _configuration.InitialDelayMinutes);
            await Task.Delay(TimeSpan.FromMinutes(_configuration.InitialDelayMinutes), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_configuration.AutoImportEnabled && !string.IsNullOrEmpty(_configuration.StockScrapperDataPath))
                {
                    await PerformScheduledImportAsync(stoppingToken);
                }

                // Wait for the next interval
                var delay = TimeSpan.FromMinutes(_configuration.IntervalMinutes);
                _logger.LogDebug("Waiting {IntervalMinutes} minutes until next import check", _configuration.IntervalMinutes);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data import background service");
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task PerformScheduledImportAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dataImportService = scope.ServiceProvider.GetRequiredService<IDataImportService>();

        _logger.LogInformation("Starting scheduled data import from {StockScrapperDataPath}",
            _configuration.StockScrapperDataPath);

        var progressCallback = new Progress<DataImportProgressDto>(progress =>
        {
            _logger.LogInformation("Import progress: {Operation} - {FilesProcessed}/{TotalFiles} files, Rate: {ProcessingRate:F1} records/sec",
                progress.Operation, progress.FilesProcessed, progress.TotalFiles, progress.ProcessingRate);
        });

        try
        {
            var results = await dataImportService.ImportAllMarketsAsync(
                _configuration.StockScrapperDataPath!,
                progressCallback,
                cancellationToken);

            var totalImported = results.Values.Sum(r => r.RecordsImported);
            var totalErrors = results.Values.Sum(r => r.Errors.Count);

            _logger.LogInformation("Scheduled import completed. Total records imported: {TotalImported}, Errors: {TotalErrors}",
                totalImported, totalErrors);

            // Log results per market
            foreach (var kvp in results)
            {
                var market = kvp.Key;
                var result = kvp.Value;

                if (result.Success)
                {
                    _logger.LogInformation("Market {Market}: {RecordsImported} records imported, {FilesProcessed} files processed",
                        market, result.RecordsImported, result.FilesProcessed);
                }
                else
                {
                    _logger.LogWarning("Market {Market} failed: {Message}. Errors: {Errors}",
                        market, result.Message, string.Join("; ", result.Errors));
                }
            }

            // Perform cleanup if enabled
            if (_configuration.AutoCleanupDuplicates)
            {
                await PerformDuplicateCleanupAsync(dataImportService, results, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled data import");
        }
    }

    private async Task PerformDuplicateCleanupAsync(
        IDataImportService dataImportService,
        Dictionary<string, DataImportResultDto> importResults,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting duplicate cleanup for imported data");

        var cleanupTasks = new List<Task>();

        foreach (var result in importResults.Values.Where(r => r.Success))
        {
            foreach (var symbolStats in result.SymbolStats.Values)
            {
                if (symbolStats.StartDate.HasValue && symbolStats.EndDate.HasValue)
                {
                    cleanupTasks.Add(CleanupSymbolDuplicatesAsync(
                        dataImportService,
                        symbolStats.SymbolTicker,
                        symbolStats.StartDate.Value,
                        symbolStats.EndDate.Value,
                        cancellationToken));
                }
            }
        }

        // Run cleanup for all symbols concurrently (with some limit)
        var semaphore = new SemaphoreSlim(_configuration.MaxConcurrentCleanups);
        var limitedTasks = cleanupTasks.Select(async task =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await task;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(limitedTasks);

        _logger.LogInformation("Duplicate cleanup completed");
    }

    private async Task CleanupSymbolDuplicatesAsync(
        IDataImportService dataImportService,
        string symbolTicker,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            // First check if there are duplicates
            var duplicates = await dataImportService.GetDuplicateRecordsAsync(
                symbolTicker, startDate, endDate);

            if (duplicates.Any())
            {
                _logger.LogInformation("Found {DuplicateCount} duplicates for {SymbolTicker}, cleaning up",
                    duplicates.Count, symbolTicker);

                var cleanupResult = await dataImportService.CleanDuplicateRecordsAsync(
                    symbolTicker, startDate, endDate, dryRun: false);

                if (cleanupResult.Success)
                {
                    _logger.LogDebug("Cleaned {RecordsDeleted} duplicate records for {SymbolTicker}",
                        cleanupResult.RecordsDeleted, symbolTicker);
                }
                else
                {
                    _logger.LogWarning("Failed to clean duplicates for {SymbolTicker}: {Message}",
                        symbolTicker, cleanupResult.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning duplicates for symbol {SymbolTicker}", symbolTicker);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data import background service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Configuration for data import background service
/// </summary>
public class DataImportConfiguration
{
    /// <summary>
    /// Whether automatic import is enabled
    /// </summary>
    public bool AutoImportEnabled { get; set; } = false;

    /// <summary>
    /// Path to Stock_Scrapper DATA directory
    /// </summary>
    public string? StockScrapperDataPath { get; set; }

    /// <summary>
    /// Initial delay before starting (minutes)
    /// </summary>
    public int InitialDelayMinutes { get; set; } = 1;

    /// <summary>
    /// Interval between import checks (minutes)
    /// </summary>
    public int IntervalMinutes { get; set; } = 60; // Check every hour

    /// <summary>
    /// Whether to automatically clean duplicates after import
    /// </summary>
    public bool AutoCleanupDuplicates { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent cleanup operations
    /// </summary>
    public int MaxConcurrentCleanups { get; set; } = 5;

    /// <summary>
    /// Markets to import (empty means all)
    /// </summary>
    public List<string> MarketsToImport { get; set; } = new();

    /// <summary>
    /// Whether to skip import if recent data exists
    /// </summary>
    public bool SkipIfRecentDataExists { get; set; } = true;

    /// <summary>
    /// Consider data recent if it's within this many hours
    /// </summary>
    public int RecentDataThresholdHours { get; set; } = 24;
}