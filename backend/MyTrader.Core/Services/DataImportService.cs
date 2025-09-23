using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using MyTrader.Core.Data;

namespace MyTrader.Core.Services;

/// <summary>
/// Comprehensive data import service for Stock_Scrapper data
/// Supports BIST detailed format and standard OHLCV format with batch processing,
/// validation, duplicate checking, and progress tracking
/// </summary>
public class DataImportService : IDataImportService
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger<DataImportService> _logger;

    // Configuration constants
    private const int BATCH_SIZE = 1000;
    private const int PROGRESS_REPORT_INTERVAL = 100;
    private const int MAX_VALIDATION_ROWS = 10;
    private const int LARGE_BATCH_THRESHOLD = 10000; // Use bulk operations for large datasets
    private const int DUPLICATE_CHECK_BATCH_SIZE = 5000; // Optimize duplicate checking for large sets

    // Data source priorities for deduplication
    private static readonly Dictionary<string, int> DataSourcePriorities = new()
    {
        { "BIST", 1 },      // Highest priority
        { "YAHOO", 2 },
        { "BINANCE", 3 },
        { "NASDAQ", 4 },
        { "NYSE", 5 },
        { "CRYPTO", 6 }     // Lowest priority
    };

    // Expected column headers for different formats
    private static readonly Dictionary<string, List<string>> ExpectedHeaders = new()
    {
        ["BIST"] = new List<string>
        {
            "HGDG_HS_KODU", "Tarih", "DuzeltilmisKapanis", "AcilisFiyati", "EnDusuk", "EnYuksek", "Hacim",
            "END_ENDEKS_KODU", "END_TARIH", "END_SEANS", "END_DEGER", "DD_DOVIZ_KODU", "DD_DT_KODU",
            "DD_TARIH", "DD_DEGER", "DOLAR_BAZLI_FIYAT", "ENDEKS_BAZLI_FIYAT", "DOLAR_HACIM", "SERMAYE",
            "HG_KAPANIS", "HG_AOF", "HG_MIN", "HG_MAX", "PD", "PD_USD", "HAO_PD", "HAO_PD_USD",
            "HG_HACIM", "DOLAR_BAZLI_MIN", "DOLAR_BAZLI_MAX", "DOLAR_BAZLI_AOF", "HisseKodu"
        },
        ["STANDARD"] = new List<string>
        {
            "HisseKodu", "Tarih", "AcilisFiyati", "EnYuksek", "EnDusuk", "KapanisFiyati", "DuzeltilmisKapanis", "Hacim"
        }
    };

    public DataImportService(ITradingDbContext dbContext, ILogger<DataImportService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DataImportResultDto> ImportFromCsvAsync(
        string filePath,
        string dataSource,
        IProgress<DataImportProgressDto>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new DataImportResultDto
        {
            DataSource = dataSource,
            FilesProcessed = 1
        };

        try
        {
            _logger.LogInformation("Starting CSV import from {FilePath} for data source {DataSource}", filePath, dataSource);

            // Validate file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }

            // Validate file format
            var validation = await ValidateCsvFileAsync(filePath);
            if (!validation.IsValid)
            {
                result.Success = false;
                result.Message = "File validation failed";
                result.Errors.AddRange(validation.Errors);
                return result;
            }

            var fileName = Path.GetFileName(filePath);
            var symbolTicker = ExtractSymbolFromFileName(fileName);

            progressCallback?.Report(new DataImportProgressDto
            {
                Operation = $"Processing file {fileName}",
                CurrentFile = fileName,
                FilesProcessed = 0,
                TotalFiles = 1
            });

            // Get or create symbol
            var symbol = await GetOrCreateSymbolAsync(symbolTicker, dataSource, cancellationToken);
            if (symbol == null)
            {
                throw new InvalidOperationException($"Could not create symbol for ticker: {symbolTicker}");
            }

            // Process the file
            var fileResult = await ProcessSingleFileAsync(
                filePath,
                symbol,
                dataSource,
                validation.DataFormat,
                progressCallback,
                cancellationToken);

            // Update result
            result.Success = fileResult.Success;
            result.Message = fileResult.Success ? "Import completed successfully" : "Import completed with errors";
            result.RecordsProcessed = fileResult.RecordsProcessed;
            result.RecordsImported = fileResult.RecordsImported;
            result.RecordsSkipped = fileResult.RecordsSkipped;
            result.RecordsWithErrors = fileResult.RecordsProcessed - fileResult.RecordsImported - fileResult.RecordsSkipped;
            result.FileResults.Add(fileResult);
            result.Errors.AddRange(fileResult.Errors);

            // Update symbol statistics
            result.SymbolStats[symbolTicker] = new SymbolImportStatsDto
            {
                SymbolTicker = symbolTicker,
                RecordsImported = fileResult.RecordsImported,
                StartDate = fileResult.StartDate,
                EndDate = fileResult.EndDate,
                IsNewSymbol = symbol.CreatedAt > DateTime.UtcNow.AddMinutes(-1),
                DataQualityScore = CalculateDataQualityScore(fileResult)
            };

            if (fileResult.StartDate.HasValue)
            {
                result.StartDate = result.StartDate.HasValue ?
                    DateOnly.FromDateTime(new[] { result.StartDate.Value.ToDateTime(TimeOnly.MinValue), fileResult.StartDate.Value.ToDateTime(TimeOnly.MinValue) }.Min()) :
                    fileResult.StartDate;
            }

            if (fileResult.EndDate.HasValue)
            {
                result.EndDate = result.EndDate.HasValue ?
                    DateOnly.FromDateTime(new[] { result.EndDate.Value.ToDateTime(TimeOnly.MinValue), fileResult.EndDate.Value.ToDateTime(TimeOnly.MinValue) }.Max()) :
                    fileResult.EndDate;
            }

            _logger.LogInformation("CSV import completed. Records imported: {RecordsImported}, Skipped: {RecordsSkipped}, Errors: {RecordsWithErrors}",
                result.RecordsImported, result.RecordsSkipped, result.RecordsWithErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV import from {FilePath}", filePath);
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
            result.AvgProcessingRate = result.ProcessingTime.TotalSeconds > 0 ?
                (decimal)(result.RecordsProcessed / result.ProcessingTime.TotalSeconds) : 0;
        }

        return result;
    }

    public async Task<DataImportResultDto> ImportFromDirectoryAsync(
        string directoryPath,
        string dataSource,
        IProgress<DataImportProgressDto>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new DataImportResultDto
        {
            DataSource = dataSource
        };

        try
        {
            _logger.LogInformation("Starting directory import from {DirectoryPath} for data source {DataSource}", directoryPath, dataSource);

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            var csvFiles = Directory.GetFiles(directoryPath, "*.csv", SearchOption.TopDirectoryOnly);
            if (csvFiles.Length == 0)
            {
                result.Success = true;
                result.Message = "No CSV files found in directory";
                return result;
            }

            result.FilesProcessed = csvFiles.Length;
            var processedFiles = 0;
            var totalRecordsProcessed = 0L;

            foreach (var csvFile in csvFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fileName = Path.GetFileName(csvFile);
                var symbolTicker = ExtractSymbolFromFileName(fileName);

                progressCallback?.Report(new DataImportProgressDto
                {
                    Operation = $"Processing file {fileName}",
                    CurrentFile = fileName,
                    FilesProcessed = processedFiles,
                    TotalFiles = csvFiles.Length
                });

                try
                {
                    // Validate file format
                    var validation = await ValidateCsvFileAsync(csvFile);
                    if (!validation.IsValid)
                    {
                        result.Warnings.Add($"Skipping invalid file {fileName}: {string.Join(", ", validation.Errors)}");
                        continue;
                    }

                    // Get or create symbol
                    var symbol = await GetOrCreateSymbolAsync(symbolTicker, dataSource, cancellationToken);
                    if (symbol == null)
                    {
                        result.Warnings.Add($"Could not create symbol for {symbolTicker} in file {fileName}");
                        continue;
                    }

                    // Process the file
                    var fileResult = await ProcessSingleFileAsync(
                        csvFile,
                        symbol,
                        dataSource,
                        validation.DataFormat,
                        progressCallback,
                        cancellationToken);

                    result.FileResults.Add(fileResult);
                    result.RecordsProcessed += fileResult.RecordsProcessed;
                    result.RecordsImported += fileResult.RecordsImported;
                    result.RecordsSkipped += fileResult.RecordsSkipped;
                    result.Errors.AddRange(fileResult.Errors);

                    // Update symbol statistics
                    if (!result.SymbolStats.ContainsKey(symbolTicker))
                    {
                        result.SymbolStats[symbolTicker] = new SymbolImportStatsDto
                        {
                            SymbolTicker = symbolTicker,
                            IsNewSymbol = symbol.CreatedAt > DateTime.UtcNow.AddMinutes(-1)
                        };
                    }

                    var symbolStats = result.SymbolStats[symbolTicker];
                    symbolStats.RecordsImported += fileResult.RecordsImported;
                    symbolStats.StartDate = symbolStats.StartDate.HasValue && fileResult.StartDate.HasValue ?
                        DateOnly.FromDateTime(new[] { symbolStats.StartDate.Value.ToDateTime(TimeOnly.MinValue), fileResult.StartDate.Value.ToDateTime(TimeOnly.MinValue) }.Min()) :
                        fileResult.StartDate ?? symbolStats.StartDate;
                    symbolStats.EndDate = symbolStats.EndDate.HasValue && fileResult.EndDate.HasValue ?
                        DateOnly.FromDateTime(new[] { symbolStats.EndDate.Value.ToDateTime(TimeOnly.MinValue), fileResult.EndDate.Value.ToDateTime(TimeOnly.MinValue) }.Max()) :
                        fileResult.EndDate ?? symbolStats.EndDate;

                    // Update overall date range
                    if (fileResult.StartDate.HasValue)
                    {
                        result.StartDate = result.StartDate.HasValue ?
                            DateOnly.FromDateTime(new[] { result.StartDate.Value.ToDateTime(TimeOnly.MinValue), fileResult.StartDate.Value.ToDateTime(TimeOnly.MinValue) }.Min()) :
                            fileResult.StartDate;
                    }

                    if (fileResult.EndDate.HasValue)
                    {
                        result.EndDate = result.EndDate.HasValue ?
                            DateOnly.FromDateTime(new[] { result.EndDate.Value.ToDateTime(TimeOnly.MinValue), fileResult.EndDate.Value.ToDateTime(TimeOnly.MinValue) }.Max()) :
                            fileResult.EndDate;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {FileName}", fileName);
                    result.Errors.Add($"Error processing {fileName}: {ex.Message}");
                }

                processedFiles++;
                totalRecordsProcessed += result.RecordsProcessed;

                // Report progress
                progressCallback?.Report(new DataImportProgressDto
                {
                    Operation = $"Completed {processedFiles}/{csvFiles.Length} files",
                    FilesProcessed = processedFiles,
                    TotalFiles = csvFiles.Length,
                    ProcessingRate = stopwatch.Elapsed.TotalSeconds > 0 ? (decimal)(totalRecordsProcessed / stopwatch.Elapsed.TotalSeconds) : 0
                });
            }

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success ?
                $"Successfully processed {processedFiles} files" :
                $"Processed {processedFiles} files with {result.Errors.Count} errors";
            result.RecordsWithErrors = result.RecordsProcessed - result.RecordsImported - result.RecordsSkipped;

            _logger.LogInformation("Directory import completed. Files: {FilesProcessed}, Records imported: {RecordsImported}",
                processedFiles, result.RecordsImported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during directory import from {DirectoryPath}", directoryPath);
            result.Success = false;
            result.Message = $"Directory import failed: {ex.Message}";
            result.Errors.Add(ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
            result.AvgProcessingRate = result.ProcessingTime.TotalSeconds > 0 ?
                (decimal)(result.RecordsProcessed / result.ProcessingTime.TotalSeconds) : 0;
        }

        return result;
    }

    public async Task<Dictionary<string, DataImportResultDto>> ImportAllMarketsAsync(
        string stockScrapperDataPath,
        IProgress<DataImportProgressDto>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, DataImportResultDto>();
        var markets = new[] { "BIST", "Crypto", "NASDAQ", "NYSE" };

        _logger.LogInformation("Starting import of all markets from {StockScrapperDataPath}", stockScrapperDataPath);

        for (int i = 0; i < markets.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var market = markets[i];
            var marketPath = Path.Combine(stockScrapperDataPath, market);

            progressCallback?.Report(new DataImportProgressDto
            {
                Operation = $"Processing market {market}",
                FilesProcessed = i,
                TotalFiles = markets.Length
            });

            if (Directory.Exists(marketPath))
            {
                var result = await ImportFromDirectoryAsync(marketPath, market, progressCallback, cancellationToken);
                results[market] = result;

                _logger.LogInformation("Completed import for market {Market}. Records imported: {RecordsImported}",
                    market, result.RecordsImported);
            }
            else
            {
                _logger.LogWarning("Market directory not found: {MarketPath}", marketPath);
                results[market] = new DataImportResultDto
                {
                    Success = false,
                    DataSource = market,
                    Message = $"Directory not found: {marketPath}"
                };
            }
        }

        return results;
    }

    public async Task<DataValidationResultDto> ValidateCsvFileAsync(string filePath)
    {
        var result = new DataValidationResultDto();

        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            });

            // Read header
            await csv.ReadAsync();
            csv.ReadHeader();
            result.Headers = csv.HeaderRecord?.ToList() ?? new List<string>();

            // Detect format based on headers
            result.DataFormat = DetectDataFormat(result.Headers);
            result.ExpectedHeaders = ExpectedHeaders.GetValueOrDefault(result.DataFormat, new List<string>());

            // Check for missing/extra columns
            result.MissingColumns = result.ExpectedHeaders.Except(result.Headers).ToList();
            result.ExtraColumns = result.Headers.Except(result.ExpectedHeaders).ToList();

            // Validate essential columns exist
            var essentialColumns = result.DataFormat == "BIST"
                ? new[] { "HGDG_HS_KODU", "Tarih", "HisseKodu" }
                : new[] { "HisseKodu", "Tarih" };

            var missingEssential = essentialColumns.Except(result.Headers).ToList();
            if (missingEssential.Any())
            {
                result.Errors.Add($"Missing essential columns: {string.Join(", ", missingEssential)}");
            }

            // Extract symbol from filename as fallback
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            result.SymbolTicker = ExtractSymbolFromFileName(fileName);

            // Read sample data and validate format
            var sampleRows = new List<Dictionary<string, string>>();
            var rowCount = 0;
            DateOnly? minDate = null, maxDate = null;

            while (await csv.ReadAsync() && rowCount < MAX_VALIDATION_ROWS)
            {
                var row = new Dictionary<string, string>();
                foreach (var header in result.Headers)
                {
                    row[header] = csv.GetField(header) ?? string.Empty;
                }
                sampleRows.Add(row);

                // Validate date format
                var dateField = row.GetValueOrDefault("Tarih", "");
                if (!string.IsNullOrEmpty(dateField))
                {
                    if (TryParseDate(dateField, out var date))
                    {
                        minDate = minDate.HasValue ? DateOnly.FromDateTime(new[] { minDate.Value.ToDateTime(TimeOnly.MinValue), date.ToDateTime(TimeOnly.MinValue) }.Min()) : date;
                        maxDate = maxDate.HasValue ? DateOnly.FromDateTime(new[] { maxDate.Value.ToDateTime(TimeOnly.MinValue), date.ToDateTime(TimeOnly.MinValue) }.Max()) : date;
                    }
                    else
                    {
                        result.Warnings.Add($"Invalid date format in row {rowCount + 1}: {dateField}");
                    }
                }

                rowCount++;
            }

            // Count total rows
            while (await csv.ReadAsync())
            {
                rowCount++;
            }

            result.DataRowCount = rowCount;
            result.SampleRows = sampleRows;
            result.StartDate = minDate;
            result.EndDate = maxDate;

            // Validate symbol extraction
            if (string.IsNullOrEmpty(result.SymbolTicker))
            {
                result.Warnings.Add("Could not extract symbol ticker from filename");
            }

            // Set validation result
            result.IsValid = result.Errors.Count == 0 && !string.IsNullOrEmpty(result.SymbolTicker);

            if (result.IsValid)
            {
                _logger.LogDebug("CSV validation successful for {FilePath}. Format: {DataFormat}, Rows: {DataRowCount}",
                    filePath, result.DataFormat, result.DataRowCount);
            }
            else
            {
                _logger.LogWarning("CSV validation failed for {FilePath}. Errors: {Errors}",
                    filePath, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating CSV file {FilePath}", filePath);
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    public async Task<List<HistoricalMarketData>> GetDuplicateRecordsAsync(
        string symbolTicker,
        DateOnly startDate,
        DateOnly endDate,
        string? dataSource = null)
    {
        // Split the complex query into simpler parts to avoid LINQ translation issues
        var baseQuery = _dbContext.HistoricalMarketData
            .Where(h => h.SymbolTicker == symbolTicker &&
                       h.TradeDate >= startDate &&
                       h.TradeDate <= endDate);

        if (!string.IsNullOrEmpty(dataSource))
        {
            baseQuery = baseQuery.Where(h => h.DataSource == dataSource);
        }

        // Get all records first
        var allRecords = await baseQuery
            .OrderBy(h => h.TradeDate)
            .ThenBy(h => h.DataSource)
            .ToListAsync();

        // Find duplicates in memory to avoid complex LINQ translation
        var duplicateGroups = allRecords
            .GroupBy(h => new { h.SymbolTicker, h.TradeDate, h.Timeframe })
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        return duplicateGroups;
    }

    public async Task<DataCleanupResultDto> CleanDuplicateRecordsAsync(
        string symbolTicker,
        DateOnly startDate,
        DateOnly endDate,
        bool dryRun = true)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new DataCleanupResultDto
        {
            SymbolTicker = symbolTicker,
            StartDate = startDate,
            EndDate = endDate,
            WasDryRun = dryRun
        };

        try
        {
            _logger.LogInformation("Starting duplicate cleanup for {SymbolTicker} from {StartDate} to {EndDate} (DryRun: {DryRun})",
                symbolTicker, startDate, endDate, dryRun);

            // Get duplicate groups using simpler queries to avoid LINQ translation issues
            var allRecords = await _dbContext.HistoricalMarketData
                .Where(h => h.SymbolTicker == symbolTicker &&
                           h.TradeDate >= startDate &&
                           h.TradeDate <= endDate)
                .ToListAsync();

            var duplicateGroups = allRecords
                .GroupBy(h => new { h.SymbolTicker, h.TradeDate, h.Timeframe })
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    Key = g.Key,
                    Records = g.ToList()
                })
                .ToList();

            result.DuplicatesFound = duplicateGroups.Sum(g => g.Records.Count);

            foreach (var group in duplicateGroups)
            {
                var records = group.Records.OrderBy(r => GetSourcePriority(r.DataSource)).ToList();
                var recordToKeep = records.First(); // Highest priority (lowest number)
                var recordsToRemove = records.Skip(1).ToList();

                var duplicateGroup = new DuplicateGroupDto
                {
                    TradeDate = group.Key.TradeDate,
                    DuplicateCount = records.Count,
                    DataSources = records.Select(r => r.DataSource).Distinct().ToList(),
                    RetainedSource = recordToKeep.DataSource,
                    RemovedSources = recordsToRemove.Select(r => r.DataSource).ToList()
                };

                result.DuplicateGroups.Add(duplicateGroup);
                result.RecordsRetained++;
                result.RecordsDeleted += recordsToRemove.Count;

                if (!dryRun)
                {
                    _dbContext.HistoricalMarketData.RemoveRange(recordsToRemove);
                }
            }

            if (!dryRun && result.RecordsDeleted > 0)
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Deleted {RecordsDeleted} duplicate records for {SymbolTicker}",
                    result.RecordsDeleted, symbolTicker);
            }

            result.Success = true;
            result.Message = dryRun ?
                $"Found {result.DuplicatesFound} duplicates in {result.DuplicateGroups.Count} groups (would delete {result.RecordsDeleted})" :
                $"Cleaned {result.RecordsDeleted} duplicate records, retained {result.RecordsRetained}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during duplicate cleanup for {SymbolTicker}", symbolTicker);
            result.Success = false;
            result.Message = $"Cleanup failed: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<DataImportStatsDto> GetImportStatisticsAsync(DateOnly startDate, DateOnly endDate)
    {
        var stats = new DataImportStatsDto
        {
            StartDate = startDate,
            EndDate = endDate
        };

        try
        {
            // Get all records for the period first to avoid complex LINQ translations
            var allRecords = await _dbContext.HistoricalMarketData
                .Where(h => h.TradeDate >= startDate && h.TradeDate <= endDate)
                .Select(h => new { h.DataSource, h.SymbolTicker, h.DataQualityScore })
                .ToListAsync();

            // Calculate statistics in memory to avoid LINQ translation issues
            stats.TotalRecordsImported = allRecords.Count;

            // Records by source
            stats.RecordsBySource = allRecords
                .GroupBy(h => h.DataSource)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            // Records by symbol (top 100)
            stats.RecordsBySymbol = allRecords
                .GroupBy(h => h.SymbolTicker)
                .OrderByDescending(g => g.Count())
                .Take(100)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            // Quality statistics
            var qualityScores = allRecords
                .Where(h => h.DataQualityScore.HasValue)
                .Select(h => h.DataQualityScore!.Value)
                .ToList();

            stats.QualityStats.AvgQualityScore = qualityScores.Any() ?
                (decimal)qualityScores.Average() : 0;

            stats.QualityStats.CompletenessPercentage = allRecords.Any() ?
                (decimal)allRecords.Count(h => h.DataQualityScore >= 80) / allRecords.Count * 100 : 0;

            // Performance stats (simplified for this implementation)
            stats.PerformanceStats.AvgProcessingRate = 1000; // Placeholder
            stats.PerformanceStats.TotalProcessingTime = TimeSpan.FromSeconds(stats.TotalRecordsImported / 1000);

            _logger.LogDebug("Generated import statistics for period {StartDate} to {EndDate}. Total records: {TotalRecords}",
                startDate, endDate, stats.TotalRecordsImported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating import statistics");
            throw;
        }

        return stats;
    }

    #region Private Helper Methods

    private async Task<FileImportResultDto> ProcessSingleFileAsync(
        string filePath,
        Symbol symbol,
        string dataSource,
        string dataFormat,
        IProgress<DataImportProgressDto>? progressCallback,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new FileImportResultDto
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            SymbolTicker = symbol.Ticker,
            DataFormat = dataFormat
        };

        var batch = new List<HistoricalMarketData>();
        var recordCount = 0;

        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            });

            await csv.ReadAsync();
            csv.ReadHeader();

            DateOnly? minDate = null, maxDate = null;

            while (await csv.ReadAsync() && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var marketData = dataFormat == "BIST" ?
                        ParseBistRecord(csv, symbol, dataSource) :
                        ParseStandardRecord(csv, symbol, dataSource);

                    if (marketData != null)
                    {
                        // Update date range
                        minDate = minDate.HasValue ? DateOnly.FromDateTime(new[] { minDate.Value.ToDateTime(TimeOnly.MinValue), marketData.TradeDate.ToDateTime(TimeOnly.MinValue) }.Min()) : marketData.TradeDate;
                        maxDate = maxDate.HasValue ? DateOnly.FromDateTime(new[] { maxDate.Value.ToDateTime(TimeOnly.MinValue), marketData.TradeDate.ToDateTime(TimeOnly.MinValue) }.Max()) : marketData.TradeDate;

                        batch.Add(marketData);

                        if (batch.Count >= BATCH_SIZE)
                        {
                            var imported = await SaveBatchAsync(batch, cancellationToken);
                            result.RecordsImported += imported;
                            result.RecordsSkipped += batch.Count - imported;
                            batch.Clear();
                        }
                    }

                    recordCount++;

                    // Report progress
                    if (recordCount % PROGRESS_REPORT_INTERVAL == 0)
                    {
                        progressCallback?.Report(new DataImportProgressDto
                        {
                            Operation = $"Processing {result.FileName}",
                            CurrentFile = result.FileName,
                            RecordsProcessed = recordCount,
                            ProcessingRate = stopwatch.Elapsed.TotalSeconds > 0 ?
                                (decimal)(recordCount / stopwatch.Elapsed.TotalSeconds) : 0
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing record {RecordCount} in file {FileName}", recordCount + 1, result.FileName);
                    result.Errors.Add($"Row {recordCount + 1}: {ex.Message}");
                }
            }

            // Save remaining batch
            if (batch.Any())
            {
                var imported = await SaveBatchAsync(batch, cancellationToken);
                result.RecordsImported += imported;
                result.RecordsSkipped += batch.Count - imported;
            }

            result.RecordsProcessed = recordCount;
            result.Success = result.Errors.Count == 0;
            result.StartDate = minDate;
            result.EndDate = maxDate;

            _logger.LogDebug("Processed file {FileName}. Records: {RecordsProcessed}, Imported: {RecordsImported}, Skipped: {RecordsSkipped}",
                result.FileName, result.RecordsProcessed, result.RecordsImported, result.RecordsSkipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file {FilePath}", filePath);
            result.Success = false;
            result.Errors.Add($"File processing error: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
        }

        return result;
    }

    private async Task<int> SaveBatchAsync(List<HistoricalMarketData> batch, CancellationToken cancellationToken)
    {
        try
        {
            if (!batch.Any())
                return 0;

            // For in-memory database, implement efficient duplicate checking
            // Use simpler queries that can be translated properly
            var dedupedBatch = await DeduplicateBatchAsync(batch, cancellationToken);

            if (dedupedBatch.Any())
            {
                await _dbContext.HistoricalMarketData.AddRangeAsync(dedupedBatch, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return dedupedBatch.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving batch of {BatchSize} records", batch.Count);

            // Handle unique constraint violations gracefully for duplicates
            if (ex.InnerException?.Message?.Contains("duplicate") == true ||
                ex.InnerException?.Message?.Contains("unique") == true ||
                ex.Message.Contains("duplicate key") ||
                ex.Message.Contains("UNIQUE constraint"))
            {
                _logger.LogWarning("Duplicate records detected in batch, attempting individual inserts...");
                return await SaveBatchIndividuallyAsync(batch, cancellationToken);
            }

            throw;
        }
    }

    private HistoricalMarketData? ParseBistRecord(CsvReader csv, Symbol symbol, string dataSource)
    {
        try
        {
            var record = new HistoricalMarketData
            {
                SymbolId = symbol.Id,
                SymbolTicker = symbol.Ticker,
                DataSource = dataSource,
                MarketCode = "BIST",
                Timeframe = "DAILY",
                Currency = "TRY",
                SourcePriority = GetSourcePriority(dataSource),
                DataCollectedAt = DateTime.UtcNow
            };

            // Parse date
            var dateStr = csv.GetField("Tarih");
            if (!TryParseDate(dateStr, out var tradeDate))
            {
                _logger.LogWarning("Invalid date format: {DateString}", dateStr);
                return null;
            }
            record.TradeDate = tradeDate;

            // Parse BIST specific fields
            record.BistCode = csv.GetField("HGDG_HS_KODU");

            // Parse price data
            record.OpenPrice = TryParseDecimal(csv.GetField("AcilisFiyati"));
            record.HighPrice = TryParseDecimal(csv.GetField("EnYuksek"));
            record.LowPrice = TryParseDecimal(csv.GetField("EnDusuk"));
            record.ClosePrice = TryParseDecimal(csv.GetField("HG_KAPANIS"));
            record.AdjustedClosePrice = TryParseDecimal(csv.GetField("DuzeltilmisKapanis"));
            record.Volume = TryParseDecimal(csv.GetField("Hacim"));

            // Parse BIST extended data
            record.TradingValue = TryParseDecimal(csv.GetField("SERMAYE"));
            record.MarketCap = TryParseDecimal(csv.GetField("SERMAYE"));
            record.IndexValue = TryParseDecimal(csv.GetField("END_DEGER"));
            record.UsdTryRate = TryParseDecimal(csv.GetField("DD_DEGER"));

            // Calculate derived fields
            if (record.ClosePrice.HasValue && record.OpenPrice.HasValue)
            {
                record.PreviousClose = record.OpenPrice; // Approximation
                record.PriceChange = record.ClosePrice - record.OpenPrice;
                record.PriceChangePercent = record.OpenPrice > 0 ?
                    (record.PriceChange / record.OpenPrice) * 100 : 0;
            }

            // Set data quality score
            record.DataQualityScore = CalculateDataQualityScore(record);

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing BIST record");
            return null;
        }
    }

    private HistoricalMarketData? ParseStandardRecord(CsvReader csv, Symbol symbol, string dataSource)
    {
        try
        {
            var record = new HistoricalMarketData
            {
                SymbolId = symbol.Id,
                SymbolTicker = symbol.Ticker,
                DataSource = dataSource,
                MarketCode = GetMarketCodeFromDataSource(dataSource),
                Timeframe = "DAILY",
                Currency = GetCurrencyFromDataSource(dataSource),
                SourcePriority = GetSourcePriority(dataSource),
                DataCollectedAt = DateTime.UtcNow
            };

            // Parse date
            var dateStr = csv.GetField("Tarih");
            if (!TryParseDate(dateStr, out var tradeDate))
            {
                _logger.LogWarning("Invalid date format: {DateString}", dateStr);
                return null;
            }
            record.TradeDate = tradeDate;

            // Parse standard OHLCV data
            record.OpenPrice = TryParseDecimal(csv.GetField("AcilisFiyati"));
            record.HighPrice = TryParseDecimal(csv.GetField("EnYuksek"));
            record.LowPrice = TryParseDecimal(csv.GetField("EnDusuk"));
            record.ClosePrice = TryParseDecimal(csv.GetField("KapanisFiyati"));
            record.AdjustedClosePrice = TryParseDecimal(csv.GetField("DuzeltilmisKapanis"));
            record.Volume = TryParseDecimal(csv.GetField("Hacim"));

            // Calculate derived fields
            if (record.ClosePrice.HasValue && record.OpenPrice.HasValue)
            {
                record.PriceChange = record.ClosePrice - record.OpenPrice;
                record.PriceChangePercent = record.OpenPrice > 0 ?
                    (record.PriceChange / record.OpenPrice) * 100 : 0;
            }

            // Set data quality score
            record.DataQualityScore = CalculateDataQualityScore(record);

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing standard record");
            return null;
        }
    }

    private async Task<Symbol?> GetOrCreateSymbolAsync(string ticker, string dataSource, CancellationToken cancellationToken)
    {
        try
        {
            // Try to find existing symbol
            var existingSymbol = await _dbContext.Symbols
                .FirstOrDefaultAsync(s => s.Ticker == ticker, cancellationToken);

            if (existingSymbol != null)
            {
                return existingSymbol;
            }

            // Create new symbol
            var newSymbol = new Symbol
            {
                Ticker = ticker,
                Venue = GetVenueFromDataSource(dataSource),
                AssetClass = GetAssetClassFromDataSource(dataSource),
                Display = ticker,
                IsActive = true,
                IsTracked = true,
                Country = GetCountryFromDataSource(dataSource),
                QuoteCurrency = GetCurrencyFromDataSource(dataSource)
            };

            _dbContext.Symbols.Add(newSymbol);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new symbol: {Ticker} for data source {DataSource}", ticker, dataSource);
            return newSymbol;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating symbol {Ticker} for data source {DataSource}", ticker, dataSource);
            return null;
        }
    }

    private static string ExtractSymbolFromFileName(string fileName)
    {
        // Remove extension and common suffixes
        var symbolName = Path.GetFileNameWithoutExtension(fileName);

        // Remove common patterns like "_data", "_historical", etc.
        var patterns = new[] { "_data", "_historical", "_hist", "_daily" };
        foreach (var pattern in patterns)
        {
            if (symbolName.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                symbolName = symbolName[..^pattern.Length];
            }
        }

        return symbolName.ToUpperInvariant();
    }

    private static string DetectDataFormat(List<string> headers)
    {
        // Check if this is BIST format (has BIST-specific columns)
        var bistColumns = new[] { "HGDG_HS_KODU", "END_ENDEKS_KODU", "DD_DOVIZ_KODU", "SERMAYE" };
        if (bistColumns.Any(col => headers.Contains(col)))
        {
            return "BIST";
        }

        return "STANDARD";
    }

    private static bool TryParseDate(string? dateStr, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(dateStr))
            return false;

        // Try common date formats
        var formats = new[]
        {
            "yyyy-MM-dd",
            "dd.MM.yyyy",
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "yyyy/MM/dd",
            "yyyyMMdd"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                date = DateOnly.FromDateTime(parsedDate);
                return true;
            }
        }

        return DateTime.TryParse(dateStr, out var fallbackDate) &&
               (date = DateOnly.FromDateTime(fallbackDate)) != default;
    }

    private static decimal? TryParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Replace common decimal separators
        value = value.Replace(',', '.');

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static int GetSourcePriority(string dataSource)
    {
        return DataSourcePriorities.GetValueOrDefault(dataSource.ToUpperInvariant(), 10);
    }

    private static string GetVenueFromDataSource(string dataSource) => dataSource.ToUpperInvariant() switch
    {
        "BIST" => "BIST",
        "NASDAQ" => "NASDAQ",
        "NYSE" => "NYSE",
        "CRYPTO" => "BINANCE",
        _ => dataSource.ToUpperInvariant()
    };

    private static string GetAssetClassFromDataSource(string dataSource) => dataSource.ToUpperInvariant() switch
    {
        "BIST" => "STOCK_BIST",
        "NASDAQ" => "STOCK_NASDAQ",
        "NYSE" => "STOCK_NYSE",
        "CRYPTO" => "CRYPTO",
        _ => "STOCK"
    };

    private static string GetMarketCodeFromDataSource(string dataSource) => dataSource.ToUpperInvariant() switch
    {
        "BIST" => "BIST",
        "NASDAQ" => "NASDAQ",
        "NYSE" => "NYSE",
        "CRYPTO" => "CRYPTO",
        _ => dataSource.ToUpperInvariant()
    };

    private static string GetCurrencyFromDataSource(string dataSource) => dataSource.ToUpperInvariant() switch
    {
        "BIST" => "TRY",
        "CRYPTO" => "USDT",
        _ => "USD"
    };

    private static string GetCountryFromDataSource(string dataSource) => dataSource.ToUpperInvariant() switch
    {
        "BIST" => "TR",
        "CRYPTO" => "GLOBAL",
        _ => "US"
    };

    private static int CalculateDataQualityScore(HistoricalMarketData record)
    {
        var score = 0;
        var maxScore = 0;

        // Check required OHLCV fields
        maxScore += 50;
        if (record.OpenPrice.HasValue) score += 10;
        if (record.HighPrice.HasValue) score += 10;
        if (record.LowPrice.HasValue) score += 10;
        if (record.ClosePrice.HasValue) score += 10;
        if (record.Volume.HasValue) score += 10;

        // Check data consistency
        maxScore += 30;
        if (record.HighPrice >= record.LowPrice) score += 10;
        if (record.HighPrice >= record.OpenPrice && record.HighPrice >= record.ClosePrice) score += 10;
        if (record.LowPrice <= record.OpenPrice && record.LowPrice <= record.ClosePrice) score += 10;

        // Check additional fields
        maxScore += 20;
        if (record.AdjustedClosePrice.HasValue) score += 5;
        if (record.PriceChange.HasValue) score += 5;
        if (record.PriceChangePercent.HasValue) score += 5;
        if (record.TradingValue.HasValue) score += 5;

        return maxScore > 0 ? (score * 100) / maxScore : 100;
    }

    private static int CalculateDataQualityScore(FileImportResultDto fileResult)
    {
        if (fileResult.RecordsProcessed == 0)
            return 100;

        var importRate = (decimal)fileResult.RecordsImported / fileResult.RecordsProcessed;
        var errorRate = (decimal)fileResult.Errors.Count / fileResult.RecordsProcessed;

        var score = (int)((importRate * 80) + ((1 - errorRate) * 20));
        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Efficiently deduplicate a batch of records using simple queries that can be translated to SQL
    /// </summary>
    private async Task<List<HistoricalMarketData>> DeduplicateBatchAsync(
        List<HistoricalMarketData> batch,
        CancellationToken cancellationToken)
    {
        if (!batch.Any())
            return new List<HistoricalMarketData>();

        // For large batches, use bulk optimization
        if (batch.Count > LARGE_BATCH_THRESHOLD)
        {
            return await DeduplicateLargeBatchAsync(batch, cancellationToken);
        }

        var dedupedBatch = new List<HistoricalMarketData>();

        // Group batch by symbol, date, and timeframe to handle internal duplicates
        var batchGroups = batch
            .GroupBy(b => new { b.SymbolTicker, b.TradeDate, b.Timeframe })
            .ToList();

        foreach (var group in batchGroups)
        {
            // For each group, keep the record with highest source priority (lowest number)
            var bestRecord = group
                .OrderBy(r => GetSourcePriority(r.DataSource))
                .ThenByDescending(r => r.DataQualityScore ?? 0)
                .First();

            // Check if this record already exists in the database
            var existingRecord = await _dbContext.HistoricalMarketData
                .Where(h => h.SymbolTicker == bestRecord.SymbolTicker &&
                           h.TradeDate == bestRecord.TradeDate &&
                           h.Timeframe == bestRecord.Timeframe)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingRecord == null)
            {
                // No existing record, safe to add
                dedupedBatch.Add(bestRecord);
            }
            else
            {
                // Record exists, check if we should replace it
                var existingPriority = GetSourcePriority(existingRecord.DataSource);
                var newPriority = GetSourcePriority(bestRecord.DataSource);

                if (newPriority < existingPriority ||
                    (newPriority == existingPriority && (bestRecord.DataQualityScore ?? 0) > (existingRecord.DataQualityScore ?? 0)))
                {
                    // New record has better priority or quality, replace the existing one
                    _dbContext.HistoricalMarketData.Remove(existingRecord);
                    dedupedBatch.Add(bestRecord);
                }
                // Otherwise, skip this record as existing one is better
            }
        }

        return dedupedBatch;
    }

    /// <summary>
    /// Optimized deduplication for large batches to minimize database round trips
    /// </summary>
    private async Task<List<HistoricalMarketData>> DeduplicateLargeBatchAsync(
        List<HistoricalMarketData> batch,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing large batch of {BatchSize} records with optimized deduplication", batch.Count);

        // Step 1: Handle internal duplicates in the batch
        var internallyDedupedBatch = batch
            .GroupBy(b => new { b.SymbolTicker, b.TradeDate, b.Timeframe })
            .Select(g => g
                .OrderBy(r => GetSourcePriority(r.DataSource))
                .ThenByDescending(r => r.DataQualityScore ?? 0)
                .First())
            .ToList();

        // Step 2: Get date range for efficient existing records lookup
        var minDate = internallyDedupedBatch.Min(b => b.TradeDate);
        var maxDate = internallyDedupedBatch.Max(b => b.TradeDate);
        var symbols = internallyDedupedBatch.Select(b => b.SymbolTicker).Distinct().ToList();

        // Step 3: Bulk fetch existing records
        var existingRecordsMap = new Dictionary<string, HistoricalMarketData>();

        foreach (var symbol in symbols)
        {
            var existingRecords = await _dbContext.HistoricalMarketData
                .Where(h => h.SymbolTicker == symbol &&
                           h.TradeDate >= minDate &&
                           h.TradeDate <= maxDate)
                .ToListAsync(cancellationToken);

            foreach (var record in existingRecords)
            {
                var key = $"{record.SymbolTicker}_{record.TradeDate:yyyy-MM-dd}_{record.Timeframe}";
                existingRecordsMap[key] = record;
            }
        }

        // Step 4: Process batch against existing records
        var finalBatch = new List<HistoricalMarketData>();
        var recordsToRemove = new List<HistoricalMarketData>();

        foreach (var newRecord in internallyDedupedBatch)
        {
            var key = $"{newRecord.SymbolTicker}_{newRecord.TradeDate:yyyy-MM-dd}_{newRecord.Timeframe}";

            if (!existingRecordsMap.TryGetValue(key, out var existingRecord))
            {
                // No existing record, safe to add
                finalBatch.Add(newRecord);
            }
            else
            {
                // Record exists, check if we should replace it
                var existingPriority = GetSourcePriority(existingRecord.DataSource);
                var newPriority = GetSourcePriority(newRecord.DataSource);

                if (newPriority < existingPriority ||
                    (newPriority == existingPriority && (newRecord.DataQualityScore ?? 0) > (existingRecord.DataQualityScore ?? 0)))
                {
                    // New record has better priority or quality, replace the existing one
                    recordsToRemove.Add(existingRecord);
                    finalBatch.Add(newRecord);
                }
                // Otherwise, skip this record as existing one is better
            }
        }

        // Step 5: Remove records that need to be replaced
        if (recordsToRemove.Any())
        {
            _dbContext.HistoricalMarketData.RemoveRange(recordsToRemove);
        }

        _logger.LogInformation("Large batch deduplication complete. Final batch size: {FinalSize}, Records to remove: {RemoveCount}",
            finalBatch.Count, recordsToRemove.Count);

        return finalBatch;
    }

    /// <summary>
    /// Save records individually when batch insert fails due to duplicates
    /// </summary>
    private async Task<int> SaveBatchIndividuallyAsync(
        List<HistoricalMarketData> batch,
        CancellationToken cancellationToken)
    {
        var savedCount = 0;

        foreach (var record in batch)
        {
            try
            {
                // Check if record already exists
                var existingRecord = await _dbContext.HistoricalMarketData
                    .Where(h => h.SymbolTicker == record.SymbolTicker &&
                               h.TradeDate == record.TradeDate &&
                               h.Timeframe == record.Timeframe)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingRecord == null)
                {
                    // Record doesn't exist, add it
                    await _dbContext.HistoricalMarketData.AddAsync(record, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    savedCount++;
                }
                else
                {
                    // Check if we should update the existing record
                    var existingPriority = GetSourcePriority(existingRecord.DataSource);
                    var newPriority = GetSourcePriority(record.DataSource);

                    if (newPriority < existingPriority ||
                        (newPriority == existingPriority && (record.DataQualityScore ?? 0) > (existingRecord.DataQualityScore ?? 0)))
                    {
                        // Remove existing and add new record with better data
                        _dbContext.HistoricalMarketData.Remove(existingRecord);
                        await _dbContext.HistoricalMarketData.AddAsync(record, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        savedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save individual record for {SymbolTicker} on {TradeDate}",
                    record.SymbolTicker, record.TradeDate);
                // Continue with next record
            }
        }

        return savedCount;
    }

    /// <summary>
    /// Check if records exist for the given criteria using simple translatable queries
    /// </summary>
    private async Task<bool> RecordsExistAsync(
        string symbolTicker,
        DateOnly tradeDate,
        string timeframe,
        CancellationToken cancellationToken)
    {
        return await _dbContext.HistoricalMarketData
            .AnyAsync(h => h.SymbolTicker == symbolTicker &&
                          h.TradeDate == tradeDate &&
                          h.Timeframe == timeframe, cancellationToken);
    }

    /// <summary>
    /// Get existing records for a symbol and date range using efficient queries
    /// </summary>
    private async Task<Dictionary<string, HistoricalMarketData>> GetExistingRecordsMapAsync(
        string symbolTicker,
        DateOnly startDate,
        DateOnly endDate,
        string timeframe,
        CancellationToken cancellationToken)
    {
        var existingRecords = await _dbContext.HistoricalMarketData
            .Where(h => h.SymbolTicker == symbolTicker &&
                       h.TradeDate >= startDate &&
                       h.TradeDate <= endDate &&
                       h.Timeframe == timeframe)
            .ToListAsync(cancellationToken);

        return existingRecords.ToDictionary(
            r => $"{r.SymbolTicker}_{r.TradeDate:yyyy-MM-dd}_{r.Timeframe}",
            r => r);
    }

    #endregion
}