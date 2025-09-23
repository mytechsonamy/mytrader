# MyTrader Data Import System

Comprehensive data import system for importing Stock_Scrapper data into myTrader. Supports BIST detailed format (32 columns) and standard OHLCV format with batch processing, validation, duplicate checking, and progress tracking.

## Features

- **Multiple Data Formats**: Supports BIST detailed format and standard OHLCV format
- **Batch Processing**: Processes large files efficiently with configurable batch sizes
- **Data Validation**: Validates CSV format and data integrity before import
- **Duplicate Detection**: Identifies and removes duplicate records with priority-based deduplication
- **Progress Tracking**: Real-time progress reporting with processing rates
- **Error Handling**: Comprehensive error handling with transaction rollback support
- **Symbol Management**: Automatically creates symbols for new tickers
- **Data Quality Scoring**: Calculates data quality scores for imported records

## Components

### 1. Core Service (`IDataImportService`)

The main service interface with these operations:

- `ImportFromCsvAsync()` - Import single CSV file
- `ImportFromDirectoryAsync()` - Import all CSV files from directory
- `ImportAllMarketsAsync()` - Import all markets from Stock_Scrapper DATA directory
- `ValidateCsvFileAsync()` - Validate CSV file format
- `GetDuplicateRecordsAsync()` - Find duplicate records
- `CleanDuplicateRecordsAsync()` - Clean duplicate records
- `GetImportStatisticsAsync()` - Get import statistics

### 2. Web API (`DataImportController`)

REST API endpoints for data import operations:

- `POST /api/dataimport/import-csv` - Import single file
- `POST /api/dataimport/import-directory` - Import directory
- `POST /api/dataimport/import-all-markets` - Import all markets
- `POST /api/dataimport/validate-csv` - Validate file
- `GET /api/dataimport/duplicates/{symbol}` - Get duplicates
- `POST /api/dataimport/clean-duplicates` - Clean duplicates
- `GET /api/dataimport/statistics` - Get statistics

### 3. Command-Line Tool (`MyTrader.DataImportTool`)

Console application for batch operations:

```bash
# Import single file
mytrader-import import-file --file /path/to/file.csv --source BIST

# Import directory
mytrader-import import-directory --directory /path/to/csv/files --source CRYPTO

# Import all markets
mytrader-import import-all --stock-scrapper-path /path/to/Stock_Scrapper/DATA

# Validate file
mytrader-import validate --file /path/to/file.csv

# Clean duplicates
mytrader-import clean-duplicates --symbol THYAO --start-date 2024-01-01 --end-date 2024-12-31 --dry-run false

# Get statistics
mytrader-import statistics --start-date 2024-01-01 --end-date 2024-12-31
```

### 4. Background Service (`DataImportBackgroundService`)

Automated import service that can be scheduled:

- Configurable import intervals
- Automatic duplicate cleanup
- Progress monitoring
- Error handling and retry logic

## Data Formats

### BIST Format (32 columns)
```csv
HGDG_HS_KODU,Tarih,DuzeltilmisKapanis,AcilisFiyati,EnDusuk,EnYuksek,Hacim,END_ENDEKS_KODU,END_TARIH,END_SEANS,END_DEGER,DD_DOVIZ_KODU,DD_DT_KODU,DD_TARIH,DD_DEGER,DOLAR_BAZLI_FIYAT,ENDEKS_BAZLI_FIYAT,DOLAR_HACIM,SERMAYE,HG_KAPANIS,HG_AOF,HG_MIN,HG_MAX,PD,PD_USD,HAO_PD,HAO_PD_USD,HG_HACIM,DOLAR_BAZLI_MIN,DOLAR_BAZLI_MAX,DOLAR_BAZLI_AOF,HisseKodu
```

### Standard Format (8 columns)
```csv
HisseKodu,Tarih,AcilisFiyati,EnYuksek,EnDusuk,KapanisFiyati,DuzeltilmisKapanis,Hacim
```

## Usage Examples

### 1. Import Stock_Scrapper Data via API

```javascript
// Import all markets
const response = await fetch('/api/dataimport/import-all-markets', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    stockScrapperDataPath: '/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/'
  })
});

const result = await response.json();
console.log('Import results:', result.data);
```

### 2. Import Single Market Directory

```javascript
// Import BIST directory
const response = await fetch('/api/dataimport/import-directory', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    directoryPath: '/path/to/Stock_Scrapper/DATA/BIST/',
    dataSource: 'BIST'
  })
});
```

### 3. Validate CSV File

```javascript
const response = await fetch('/api/dataimport/validate-csv', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    filePath: '/path/to/THYAO.csv'
  })
});

const validation = await response.json();
if (validation.data.isValid) {
  console.log('File is valid:', validation.data.dataFormat);
} else {
  console.log('Validation errors:', validation.data.errors);
}
```

### 4. Command-Line Import

```bash
# Run from MyTrader.Tools directory
cd backend/MyTrader.Tools

# Build the tool
dotnet build

# Import all markets from Stock_Scrapper
dotnet run -- import-all --stock-scrapper-path "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/"

# Import specific market
dotnet run -- import-directory --directory "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/BIST/" --source BIST

# Validate a file
dotnet run -- validate --file "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/BIST/THYAO.csv"

# Check for duplicates
dotnet run -- clean-duplicates --symbol THYAO --start-date 2024-01-01 --end-date 2024-12-31 --dry-run true
```

### 5. Using C# Service Directly

```csharp
// Inject IDataImportService in your service
public class MyService
{
    private readonly IDataImportService _importService;

    public MyService(IDataImportService importService)
    {
        _importService = importService;
    }

    public async Task ImportDataAsync()
    {
        var progress = new Progress<DataImportProgressDto>(p =>
        {
            Console.WriteLine($"Progress: {p.Operation} - {p.ProcessingRate:F1} records/sec");
        });

        var result = await _importService.ImportAllMarketsAsync(
            "/path/to/Stock_Scrapper/DATA/",
            progress,
            CancellationToken.None);

        foreach (var marketResult in result)
        {
            Console.WriteLine($"{marketResult.Key}: {marketResult.Value.RecordsImported} records imported");
        }
    }
}
```

## Configuration

### API Configuration (Program.cs)

```csharp
// Register data import service
builder.Services.AddScoped<IDataImportService, InfrastructureDataImportService>();
```

### Background Service Configuration

```json
{
  "DataImport": {
    "AutoImportEnabled": true,
    "StockScrapperDataPath": "/path/to/Stock_Scrapper/DATA/",
    "InitialDelayMinutes": 1,
    "IntervalMinutes": 60,
    "AutoCleanupDuplicates": true,
    "MaxConcurrentCleanups": 5,
    "SkipIfRecentDataExists": true,
    "RecentDataThresholdHours": 24
  }
}
```

### Database Configuration

The service uses the existing `HistoricalMarketData` table with these key features:

- Composite primary key for partitioning
- Comprehensive indexes for time-series queries
- Support for both BIST and standard formats
- JSON fields for extended metadata
- Data quality scoring
- Source priority for deduplication

## Data Quality Features

### Validation
- Required field validation
- Date format validation
- Numeric field validation
- Data consistency checks
- Column header validation

### Deduplication
- Priority-based source ranking (BIST > YAHOO > BINANCE > etc.)
- Date + Symbol + Timeframe uniqueness
- Automatic cleanup options
- Dry-run capability

### Quality Scoring
- OHLCV completeness (50 points)
- Data consistency (30 points)
- Additional fields (20 points)
- Final score: 0-100

## Performance Characteristics

### Batch Processing
- Default batch size: 1000 records
- Configurable batch sizes
- Memory-efficient streaming
- Progress reporting every 100 records

### Processing Rates
- BIST format: ~800-1200 records/second
- Standard format: ~1000-1500 records/second
- Concurrent processing support
- Database connection pooling

### Memory Usage
- Streaming CSV processing
- Batch-based database operations
- Configurable memory limits
- Garbage collection optimization

## Error Handling

### File-Level Errors
- Missing files
- Permission errors
- Format validation failures
- Encoding issues

### Record-Level Errors
- Invalid date formats
- Missing required fields
- Data type conversion errors
- Business rule violations

### Database Errors
- Connection failures
- Transaction rollbacks
- Constraint violations
- Timeout handling

## Monitoring and Logging

### Progress Tracking
- Real-time progress callbacks
- Processing rate monitoring
- ETA calculations
- File and record counters

### Logging
- Structured logging with Serilog
- Configurable log levels
- Error context preservation
- Performance metrics

### Statistics
- Import operation counts
- Record counts by source/symbol
- Data quality metrics
- Performance statistics

## Best Practices

1. **Always validate files before importing**
2. **Use dry-run for duplicate cleanup first**
3. **Monitor progress for large imports**
4. **Check data quality scores after import**
5. **Regular duplicate cleanup maintenance**
6. **Use background service for automated imports**
7. **Monitor disk space for large datasets**
8. **Backup database before major imports**

## Troubleshooting

### Common Issues

1. **File not found**: Check file paths and permissions
2. **Format validation fails**: Verify CSV structure matches expected format
3. **Duplicate key errors**: Run duplicate cleanup first
4. **Memory issues**: Reduce batch size or available memory
5. **Slow performance**: Check database indexes and connection pool

### Debug Commands

```bash
# Check file format
dotnet run -- validate --file /path/to/file.csv

# Check for duplicates
dotnet run -- clean-duplicates --symbol SYMBOL --start-date 2024-01-01 --end-date 2024-12-31 --dry-run true

# Get import statistics
dotnet run -- statistics --start-date 2024-01-01 --end-date 2024-12-31
```