using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Progress information for data import operations
/// </summary>
public class DataImportProgressDto
{
    /// <summary>
    /// Current operation description
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Current file being processed
    /// </summary>
    public string? CurrentFile { get; set; }

    /// <summary>
    /// Files processed so far
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// Total files to process
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Records processed in current file
    /// </summary>
    public int RecordsProcessed { get; set; }

    /// <summary>
    /// Total records in current file
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    public decimal ProgressPercentage => TotalFiles > 0 ?
        (decimal)(FilesProcessed * 100) / TotalFiles : 0;

    /// <summary>
    /// File progress percentage (0-100)
    /// </summary>
    public decimal FileProgressPercentage => TotalRecords > 0 ?
        (decimal)(RecordsProcessed * 100) / TotalRecords : 0;

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Processing rate (records per second)
    /// </summary>
    public decimal ProcessingRate { get; set; }
}

/// <summary>
/// Result of a data import operation
/// </summary>
public class DataImportResultDto
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Overall result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Data source processed
    /// </summary>
    public string DataSource { get; set; } = string.Empty;

    /// <summary>
    /// Number of files processed
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// Total records processed
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Records successfully imported
    /// </summary>
    public long RecordsImported { get; set; }

    /// <summary>
    /// Records skipped (duplicates)
    /// </summary>
    public long RecordsSkipped { get; set; }

    /// <summary>
    /// Records with validation errors
    /// </summary>
    public long RecordsWithErrors { get; set; }

    /// <summary>
    /// New symbols created during import
    /// </summary>
    public int NewSymbolsCreated { get; set; }

    /// <summary>
    /// Date range of imported data
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Date range of imported data
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Processing time
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Average processing rate (records per second)
    /// </summary>
    public decimal AvgProcessingRate { get; set; }

    /// <summary>
    /// List of validation errors encountered
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings encountered
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Detailed per-file results
    /// </summary>
    public List<FileImportResultDto> FileResults { get; set; } = new();

    /// <summary>
    /// Symbols processed with their statistics
    /// </summary>
    public Dictionary<string, SymbolImportStatsDto> SymbolStats { get; set; } = new();
}

/// <summary>
/// Import result for a single file
/// </summary>
public class FileImportResultDto
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Symbol extracted from file name
    /// </summary>
    public string? SymbolTicker { get; set; }

    /// <summary>
    /// Detected data format
    /// </summary>
    public string DataFormat { get; set; } = string.Empty;

    /// <summary>
    /// Whether file processing was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Records processed in this file
    /// </summary>
    public int RecordsProcessed { get; set; }

    /// <summary>
    /// Records imported from this file
    /// </summary>
    public int RecordsImported { get; set; }

    /// <summary>
    /// Records skipped in this file
    /// </summary>
    public int RecordsSkipped { get; set; }

    /// <summary>
    /// Processing time for this file
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// File-specific errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Date range in this file
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Date range in this file
    /// </summary>
    public DateOnly? EndDate { get; set; }
}

/// <summary>
/// Import statistics for a specific symbol
/// </summary>
public class SymbolImportStatsDto
{
    /// <summary>
    /// Symbol ticker
    /// </summary>
    public string SymbolTicker { get; set; } = string.Empty;

    /// <summary>
    /// Total records imported for this symbol
    /// </summary>
    public long RecordsImported { get; set; }

    /// <summary>
    /// Date range start
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Date range end
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Whether this is a new symbol
    /// </summary>
    public bool IsNewSymbol { get; set; }

    /// <summary>
    /// Data quality score (0-100)
    /// </summary>
    public int DataQualityScore { get; set; }

    /// <summary>
    /// Number of gaps in data
    /// </summary>
    public int DataGaps { get; set; }

    /// <summary>
    /// Symbol-specific errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Validation result for CSV file
/// </summary>
public class DataValidationResultDto
{
    /// <summary>
    /// Whether the file is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Detected data format
    /// </summary>
    public string DataFormat { get; set; } = string.Empty;

    /// <summary>
    /// Detected symbol ticker from filename or data
    /// </summary>
    public string? SymbolTicker { get; set; }

    /// <summary>
    /// Number of data rows found
    /// </summary>
    public int DataRowCount { get; set; }

    /// <summary>
    /// Column headers found
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Expected column headers for detected format
    /// </summary>
    public List<string> ExpectedHeaders { get; set; } = new();

    /// <summary>
    /// Missing required columns
    /// </summary>
    public List<string> MissingColumns { get; set; } = new();

    /// <summary>
    /// Extra columns found
    /// </summary>
    public List<string> ExtraColumns { get; set; } = new();

    /// <summary>
    /// Date range in the file
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Date range in the file
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Sample data rows for verification
    /// </summary>
    public List<Dictionary<string, string>> SampleRows { get; set; } = new();
}

/// <summary>
/// Result of duplicate record cleanup
/// </summary>
public class DataCleanupResultDto
{
    /// <summary>
    /// Whether cleanup was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Symbol processed
    /// </summary>
    public string SymbolTicker { get; set; } = string.Empty;

    /// <summary>
    /// Date range processed
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Date range processed
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Whether this was a dry run
    /// </summary>
    public bool WasDryRun { get; set; }

    /// <summary>
    /// Total duplicate records found
    /// </summary>
    public int DuplicatesFound { get; set; }

    /// <summary>
    /// Records that would be/were deleted
    /// </summary>
    public int RecordsDeleted { get; set; }

    /// <summary>
    /// Records retained (highest priority)
    /// </summary>
    public int RecordsRetained { get; set; }

    /// <summary>
    /// Processing time
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Duplicate groups found
    /// </summary>
    public List<DuplicateGroupDto> DuplicateGroups { get; set; } = new();
}

/// <summary>
/// Information about a group of duplicate records
/// </summary>
public class DuplicateGroupDto
{
    /// <summary>
    /// Date of the duplicates
    /// </summary>
    public DateOnly TradeDate { get; set; }

    /// <summary>
    /// Number of duplicates for this date
    /// </summary>
    public int DuplicateCount { get; set; }

    /// <summary>
    /// Data sources involved
    /// </summary>
    public List<string> DataSources { get; set; } = new();

    /// <summary>
    /// Source priority chosen (highest priority kept)
    /// </summary>
    public string RetainedSource { get; set; } = string.Empty;

    /// <summary>
    /// Sources that were removed
    /// </summary>
    public List<string> RemovedSources { get; set; } = new();
}

/// <summary>
/// Import statistics for monitoring
/// </summary>
public class DataImportStatsDto
{
    /// <summary>
    /// Date range start
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Date range end
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Total records imported in period
    /// </summary>
    public long TotalRecordsImported { get; set; }

    /// <summary>
    /// Records by data source
    /// </summary>
    public Dictionary<string, long> RecordsBySource { get; set; } = new();

    /// <summary>
    /// Records by symbol (top 100)
    /// </summary>
    public Dictionary<string, long> RecordsBySymbol { get; set; } = new();

    /// <summary>
    /// Import operations in period
    /// </summary>
    public int ImportOperations { get; set; }

    /// <summary>
    /// Average records per operation
    /// </summary>
    public decimal AvgRecordsPerOperation { get; set; }

    /// <summary>
    /// Data quality statistics
    /// </summary>
    public DataQualityStatsDto QualityStats { get; set; } = new();

    /// <summary>
    /// Performance statistics
    /// </summary>
    public PerformanceStatsDto PerformanceStats { get; set; } = new();
}

/// <summary>
/// Data quality statistics
/// </summary>
public class DataQualityStatsDto
{
    /// <summary>
    /// Average data quality score
    /// </summary>
    public decimal AvgQualityScore { get; set; }

    /// <summary>
    /// Records with missing required fields
    /// </summary>
    public long RecordsWithMissingData { get; set; }

    /// <summary>
    /// Records with data anomalies
    /// </summary>
    public long RecordsWithAnomalies { get; set; }

    /// <summary>
    /// Duplicate records found
    /// </summary>
    public long DuplicateRecords { get; set; }

    /// <summary>
    /// Data completeness percentage
    /// </summary>
    public decimal CompletenessPercentage { get; set; }
}

/// <summary>
/// Performance statistics
/// </summary>
public class PerformanceStatsDto
{
    /// <summary>
    /// Average processing rate (records/second)
    /// </summary>
    public decimal AvgProcessingRate { get; set; }

    /// <summary>
    /// Peak processing rate (records/second)
    /// </summary>
    public decimal PeakProcessingRate { get; set; }

    /// <summary>
    /// Average file processing time
    /// </summary>
    public TimeSpan AvgFileProcessingTime { get; set; }

    /// <summary>
    /// Total processing time
    /// </summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>
    /// Memory usage patterns
    /// </summary>
    public string MemoryUsagePattern { get; set; } = string.Empty;
}

/// <summary>
/// Raw data row for BIST detailed format (32 columns)
/// </summary>
public class BistDataRowDto
{
    [Required]
    public string HGDG_HS_KODU { get; set; } = string.Empty;

    [Required]
    public string Tarih { get; set; } = string.Empty;

    public string? DuzeltilmisKapanis { get; set; }
    public string? AcilisFiyati { get; set; }
    public string? EnDusuk { get; set; }
    public string? EnYuksek { get; set; }
    public string? Hacim { get; set; }
    public string? END_ENDEKS_KODU { get; set; }
    public string? END_TARIH { get; set; }
    public string? END_SEANS { get; set; }
    public string? END_DEGER { get; set; }
    public string? DD_DOVIZ_KODU { get; set; }
    public string? DD_DT_KODU { get; set; }
    public string? DD_TARIH { get; set; }
    public string? DD_DEGER { get; set; }
    public string? DOLAR_BAZLI_FIYAT { get; set; }
    public string? ENDEKS_BAZLI_FIYAT { get; set; }
    public string? DOLAR_HACIM { get; set; }
    public string? SERMAYE { get; set; }
    public string? HG_KAPANIS { get; set; }
    public string? HG_AOF { get; set; }
    public string? HG_MIN { get; set; }
    public string? HG_MAX { get; set; }
    public string? PD { get; set; }
    public string? PD_USD { get; set; }
    public string? HAO_PD { get; set; }
    public string? HAO_PD_USD { get; set; }
    public string? HG_HACIM { get; set; }
    public string? DOLAR_BAZLI_MIN { get; set; }
    public string? DOLAR_BAZLI_MAX { get; set; }
    public string? DOLAR_BAZLI_AOF { get; set; }
    public string? HisseKodu { get; set; }
}

/// <summary>
/// Raw data row for standard OHLCV format
/// </summary>
public class StandardDataRowDto
{
    [Required]
    public string HisseKodu { get; set; } = string.Empty;

    [Required]
    public string Tarih { get; set; } = string.Empty;

    public string? AcilisFiyati { get; set; }
    public string? EnYuksek { get; set; }
    public string? EnDusuk { get; set; }
    public string? KapanisFiyati { get; set; }
    public string? DuzeltilmisKapanis { get; set; }
    public string? Hacim { get; set; }
}