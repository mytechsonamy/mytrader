using MyTrader.Core.DTOs;
using MyTrader.Core.Models;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// Service interface for importing Stock_Scrapper data into myTrader system
/// Supports BIST detailed format and standard OHLCV format
/// </summary>
public interface IDataImportService
{
    /// <summary>
    /// Import data from a CSV file with automatic format detection
    /// </summary>
    /// <param name="filePath">Full path to the CSV file</param>
    /// <param name="dataSource">Data source identifier (BIST, CRYPTO, NASDAQ, NYSE)</param>
    /// <param name="progressCallback">Optional progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics</returns>
    Task<DataImportResultDto> ImportFromCsvAsync(
        string filePath,
        string dataSource,
        IProgress<DataImportProgressDto>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import data from a directory containing multiple CSV files
    /// </summary>
    /// <param name="directoryPath">Directory containing CSV files</param>
    /// <param name="dataSource">Data source identifier (BIST, CRYPTO, NASDAQ, NYSE)</param>
    /// <param name="progressCallback">Optional progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated import results</returns>
    Task<DataImportResultDto> ImportFromDirectoryAsync(
        string directoryPath,
        string dataSource,
        IProgress<DataImportProgressDto>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import all data from Stock_Scrapper DATA directory
    /// </summary>
    /// <param name="stockScrapperDataPath">Path to Stock_Scrapper DATA directory</param>
    /// <param name="progressCallback">Optional progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated import results for all markets</returns>
    Task<Dictionary<string, DataImportResultDto>> ImportAllMarketsAsync(
        string stockScrapperDataPath,
        IProgress<DataImportProgressDto>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a CSV file format and return validation results
    /// </summary>
    /// <param name="filePath">Full path to the CSV file</param>
    /// <returns>Validation result with detected format and errors</returns>
    Task<DataValidationResultDto> ValidateCsvFileAsync(string filePath);

    /// <summary>
    /// Get duplicate records for a specific symbol and date range
    /// </summary>
    /// <param name="symbolTicker">Symbol ticker</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="dataSource">Data source filter</param>
    /// <returns>List of duplicate records</returns>
    Task<List<HistoricalMarketData>> GetDuplicateRecordsAsync(
        string symbolTicker,
        DateOnly startDate,
        DateOnly endDate,
        string? dataSource = null);

    /// <summary>
    /// Clean duplicate records using priority-based deduplication
    /// </summary>
    /// <param name="symbolTicker">Symbol ticker to clean</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="dryRun">If true, only report what would be deleted</param>
    /// <returns>Cleanup result</returns>
    Task<DataCleanupResultDto> CleanDuplicateRecordsAsync(
        string symbolTicker,
        DateOnly startDate,
        DateOnly endDate,
        bool dryRun = true);

    /// <summary>
    /// Get import statistics for monitoring
    /// </summary>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <returns>Import statistics</returns>
    Task<DataImportStatsDto> GetImportStatisticsAsync(
        DateOnly startDate,
        DateOnly endDate);
}