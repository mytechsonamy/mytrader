# MyTrader Data Import Service - Usage Examples

This document provides practical examples for using the MyTrader Data Import Service to import Stock_Scrapper data.

## Service Registration

First, ensure the service is registered in your DI container:

```csharp
// In Program.cs
builder.Services.AddScoped<IDataImportService, InfrastructureDataImportService>();
```

## 1. Using the REST API

### Import All Markets from Stock_Scrapper

```bash
curl -X POST "https://localhost:7001/api/dataimport/import-all-markets" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "stockScrapperDataPath": "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/"
  }'
```

### Import BIST Directory

```bash
curl -X POST "https://localhost:7001/api/dataimport/import-directory" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "directoryPath": "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/BIST/",
    "dataSource": "BIST"
  }'
```

### Import Single File

```bash
curl -X POST "https://localhost:7001/api/dataimport/import-csv" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "filePath": "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/BIST/THYAO.csv",
    "dataSource": "BIST"
  }'
```

### Validate CSV File

```bash
curl -X POST "https://localhost:7001/api/dataimport/validate-csv" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "filePath": "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/BIST/THYAO.csv"
  }'
```

### Check for Duplicates

```bash
curl -X GET "https://localhost:7001/api/dataimport/duplicates/THYAO?startDate=2024-01-01&endDate=2024-12-31" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Clean Duplicates (Dry Run)

```bash
curl -X POST "https://localhost:7001/api/dataimport/clean-duplicates" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "symbolTicker": "THYAO",
    "startDate": "2024-01-01",
    "endDate": "2024-12-31",
    "dryRun": true
  }'
```

### Get Import Statistics

```bash
curl -X GET "https://localhost:7001/api/dataimport/statistics?startDate=2024-01-01&endDate=2024-12-31" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 2. Using the Service Directly in C#

### Basic Import Example

```csharp
public class ImportExample
{
    private readonly IDataImportService _importService;
    private readonly ILogger<ImportExample> _logger;

    public ImportExample(IDataImportService importService, ILogger<ImportExample> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    public async Task ImportAllMarketsAsync()
    {
        var stockScrapperPath = "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/";

        // Create progress reporter
        var progress = new Progress<DataImportProgressDto>(p =>
        {
            _logger.LogInformation("Import Progress: {Operation} - {FilesProcessed}/{TotalFiles} files ({ProgressPercentage:F1}%) - {ProcessingRate:F1} records/sec",
                p.Operation, p.FilesProcessed, p.TotalFiles, p.ProgressPercentage, p.ProcessingRate);
        });

        try
        {
            var results = await _importService.ImportAllMarketsAsync(
                stockScrapperPath,
                progress,
                CancellationToken.None);

            foreach (var marketResult in results)
            {
                var market = marketResult.Key;
                var result = marketResult.Value;

                if (result.Success)
                {
                    _logger.LogInformation("Market {Market}: Successfully imported {RecordsImported} records from {FilesProcessed} files",
                        market, result.RecordsImported, result.FilesProcessed);

                    if (result.SymbolStats.Any())
                    {
                        _logger.LogInformation("Top symbols imported:");
                        foreach (var symbolStat in result.SymbolStats.OrderByDescending(s => s.Value.RecordsImported).Take(5))
                        {
                            _logger.LogInformation("  {Symbol}: {Records} records ({StartDate} to {EndDate})",
                                symbolStat.Key, symbolStat.Value.RecordsImported, symbolStat.Value.StartDate, symbolStat.Value.EndDate);
                        }
                    }
                }
                else
                {
                    _logger.LogError("Market {Market} failed: {Message}", market, result.Message);
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("  Error: {Error}", error);
                    }
                }
            }

            // Calculate totals
            var totalRecords = results.Values.Sum(r => r.RecordsImported);
            var totalFiles = results.Values.Sum(r => r.FilesProcessed);
            var totalErrors = results.Values.Sum(r => r.Errors.Count);

            _logger.LogInformation("Import completed! Total records: {TotalRecords}, Files: {TotalFiles}, Errors: {TotalErrors}",
                totalRecords, totalFiles, totalErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during market import");
            throw;
        }
    }
}
```

### Import with Validation

```csharp
public async Task ImportWithValidationAsync(string filePath, string dataSource)
{
    // First validate the file
    var validation = await _importService.ValidateCsvFileAsync(filePath);

    if (!validation.IsValid)
    {
        _logger.LogError("File validation failed for {FilePath}: {Errors}",
            filePath, string.Join(", ", validation.Errors));
        return;
    }

    _logger.LogInformation("File validated successfully: {DataFormat}, {DataRowCount} rows, Symbol: {SymbolTicker}",
        validation.DataFormat, validation.DataRowCount, validation.SymbolTicker);

    // Proceed with import
    var result = await _importService.ImportFromCsvAsync(filePath, dataSource);

    if (result.Success)
    {
        _logger.LogInformation("Import successful: {RecordsImported}/{RecordsProcessed} records imported",
            result.RecordsImported, result.RecordsProcessed);
    }
    else
    {
        _logger.LogError("Import failed: {Message}", result.Message);
    }
}
```

### Duplicate Management Example

```csharp
public async Task ManageDuplicatesAsync(string symbolTicker, DateOnly startDate, DateOnly endDate)
{
    // Check for duplicates
    var duplicates = await _importService.GetDuplicateRecordsAsync(symbolTicker, startDate, endDate);

    if (duplicates.Any())
    {
        _logger.LogWarning("Found {DuplicateCount} duplicate records for {Symbol}",
            duplicates.Count, symbolTicker);

        // Perform dry run cleanup first
        var dryRunResult = await _importService.CleanDuplicateRecordsAsync(
            symbolTicker, startDate, endDate, dryRun: true);

        _logger.LogInformation("Dry run results: {DuplicatesFound} duplicates found, would delete {RecordsToDelete}",
            dryRunResult.DuplicatesFound, dryRunResult.RecordsDeleted);

        foreach (var group in dryRunResult.DuplicateGroups)
        {
            _logger.LogInformation("  {Date}: {Count} duplicates, would retain {RetainedSource}",
                group.TradeDate, group.DuplicateCount, group.RetainedSource);
        }

        // Perform actual cleanup
        var cleanupResult = await _importService.CleanDuplicateRecordsAsync(
            symbolTicker, startDate, endDate, dryRun: false);

        if (cleanupResult.Success)
        {
            _logger.LogInformation("Cleanup successful: {RecordsDeleted} duplicates removed, {RecordsRetained} retained",
                cleanupResult.RecordsDeleted, cleanupResult.RecordsRetained);
        }
        else
        {
            _logger.LogError("Cleanup failed: {Message}", cleanupResult.Message);
        }
    }
    else
    {
        _logger.LogInformation("No duplicates found for {Symbol}", symbolTicker);
    }
}
```

## 3. Background Service Configuration

### Enable Automated Import

Add to `appsettings.json`:

```json
{
  "DataImport": {
    "AutoImportEnabled": true,
    "StockScrapperDataPath": "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/",
    "InitialDelayMinutes": 5,
    "IntervalMinutes": 120,
    "AutoCleanupDuplicates": true,
    "MaxConcurrentCleanups": 3,
    "MarketsToImport": ["BIST", "CRYPTO"],
    "SkipIfRecentDataExists": true,
    "RecentDataThresholdHours": 24
  }
}
```

### Register Background Service

```csharp
// In Program.cs
builder.Services.Configure<DataImportConfiguration>(
    builder.Configuration.GetSection("DataImport"));
builder.Services.AddHostedService<DataImportBackgroundService>();
```

## 4. JavaScript/TypeScript Frontend Usage

### React Hook for Import Operations

```typescript
import { useState, useCallback } from 'react';

interface ImportProgress {
  operation: string;
  filesProcessed: number;
  totalFiles: number;
  progressPercentage: number;
  processingRate: number;
}

interface ImportResult {
  success: boolean;
  message: string;
  recordsImported: number;
  recordsProcessed: number;
  processingTime: string;
  symbolStats: Record<string, any>;
}

export const useDataImport = () => {
  const [isImporting, setIsImporting] = useState(false);
  const [progress, setProgress] = useState<ImportProgress | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);

  const importAllMarkets = useCallback(async (stockScrapperPath: string) => {
    setIsImporting(true);
    setResult(null);

    try {
      const response = await fetch('/api/dataimport/import-all-markets', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('authToken')}`
        },
        body: JSON.stringify({ stockScrapperDataPath })
      });

      const data = await response.json();

      if (data.success) {
        setResult(data.data);
      } else {
        throw new Error(data.message);
      }
    } catch (error) {
      console.error('Import failed:', error);
      throw error;
    } finally {
      setIsImporting(false);
    }
  }, []);

  const validateFile = useCallback(async (filePath: string) => {
    const response = await fetch('/api/dataimport/validate-csv', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('authToken')}`
      },
      body: JSON.stringify({ filePath })
    });

    const data = await response.json();
    return data.data;
  }, []);

  return {
    isImporting,
    progress,
    result,
    importAllMarkets,
    validateFile
  };
};
```

### Import Progress Component

```typescript
import React from 'react';

interface ImportProgressProps {
  progress: ImportProgress | null;
  result: ImportResult | null;
  isImporting: boolean;
}

export const ImportProgressComponent: React.FC<ImportProgressProps> = ({
  progress,
  result,
  isImporting
}) => {
  if (!isImporting && !result) return null;

  return (
    <div className="import-progress">
      {isImporting && progress && (
        <div className="progress-info">
          <h3>Import in Progress</h3>
          <p>{progress.operation}</p>
          <div className="progress-bar">
            <div
              className="progress-fill"
              style={{ width: `${progress.progressPercentage}%` }}
            />
          </div>
          <p>
            Files: {progress.filesProcessed}/{progress.totalFiles}
            ({progress.progressPercentage.toFixed(1)}%)
          </p>
          <p>Rate: {progress.processingRate.toFixed(1)} records/sec</p>
        </div>
      )}

      {result && (
        <div className={`import-result ${result.success ? 'success' : 'error'}`}>
          <h3>Import {result.success ? 'Completed' : 'Failed'}</h3>
          <p>{result.message}</p>
          {result.success && (
            <>
              <p>Records imported: {result.recordsImported:toLocaleString()}</p>
              <p>Processing time: {result.processingTime}</p>
              {Object.keys(result.symbolStats).length > 0 && (
                <div className="symbol-stats">
                  <h4>Top Symbols</h4>
                  {Object.entries(result.symbolStats)
                    .sort(([,a], [,b]) => b.recordsImported - a.recordsImported)
                    .slice(0, 10)
                    .map(([symbol, stats]) => (
                      <div key={symbol} className="symbol-stat">
                        {symbol}: {stats.recordsImported.toLocaleString()} records
                      </div>
                    ))}
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
};
```

## 5. Testing Examples

### Unit Test for Import Service

```csharp
[Test]
public async Task ImportFromCsvAsync_ValidFile_ShouldImportSuccessfully()
{
    // Arrange
    var mockDbContext = new Mock<ITradingDbContext>();
    var mockLogger = new Mock<ILogger<DataImportService>>();
    var service = new DataImportService(mockDbContext.Object, mockLogger.Object);

    var testCsvPath = CreateTestCsvFile();

    // Act
    var result = await service.ImportFromCsvAsync(testCsvPath, "BIST");

    // Assert
    Assert.IsTrue(result.Success);
    Assert.Greater(result.RecordsImported, 0);
    Assert.AreEqual("BIST", result.DataSource);
}

[Test]
public async Task ValidateCsvFileAsync_BistFormat_ShouldDetectCorrectly()
{
    // Arrange
    var testFile = CreateBistFormatCsvFile();

    // Act
    var result = await _importService.ValidateCsvFileAsync(testFile);

    // Assert
    Assert.IsTrue(result.IsValid);
    Assert.AreEqual("BIST", result.DataFormat);
    Assert.IsNotNull(result.SymbolTicker);
}
```

## 6. Performance Monitoring

### Add Custom Metrics

```csharp
public class ImportMetricsService
{
    private readonly ILogger<ImportMetricsService> _logger;

    public async Task TrackImportMetricsAsync(DataImportResultDto result)
    {
        var metrics = new
        {
            RecordsImported = result.RecordsImported,
            ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds,
            ProcessingRate = result.AvgProcessingRate,
            ErrorRate = (double)result.RecordsWithErrors / result.RecordsProcessed,
            SuccessRate = (double)result.RecordsImported / result.RecordsProcessed
        };

        _logger.LogInformation("Import Metrics: {@Metrics}", metrics);

        // Send to monitoring system (e.g., Application Insights, Grafana)
        // await _telemetryClient.TrackEventAsync("DataImport", metrics);
    }
}
```

## 7. Error Handling Best Practices

### Robust Import with Retry Logic

```csharp
public async Task<DataImportResultDto> ImportWithRetryAsync(string filePath, string dataSource, int maxRetries = 3)
{
    var retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            return await _importService.ImportFromCsvAsync(filePath, dataSource);
        }
        catch (Exception ex) when (retryCount < maxRetries - 1)
        {
            retryCount++;
            var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff

            _logger.LogWarning("Import attempt {RetryCount} failed, retrying in {Delay}s: {Error}",
                retryCount, delay.TotalSeconds, ex.Message);

            await Task.Delay(delay);
        }
    }

    throw new InvalidOperationException($"Import failed after {maxRetries} attempts");
}
```

This comprehensive guide covers all the main usage patterns for the MyTrader Data Import Service. The service is designed to be flexible, reliable, and easy to integrate into various scenarios.