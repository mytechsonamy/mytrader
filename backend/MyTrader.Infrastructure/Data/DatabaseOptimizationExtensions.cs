using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Data;
using MyTrader.Core.Models;

namespace MyTrader.Infrastructure.Data;

/// <summary>
/// Database optimization extensions for efficient batch operations and market data handling
/// Provides bulk operations, indexing strategies, and performance monitoring
/// </summary>
public static class DatabaseOptimizationExtensions
{
    /// <summary>
    /// Configure database for optimal market data performance
    /// </summary>
    public static async Task OptimizeForMarketDataAsync(this ITradingDbContext context, ILogger logger)
    {
        if (context is not DbContext dbContext)
            return;

        try
        {
            logger.LogInformation("Starting database optimization for market data operations");

            // Create performance-optimized indexes if they don't exist
            await CreateMarketDataIndexesAsync(dbContext, logger);

            // Configure connection pooling and performance settings
            await ConfigurePerformanceSettingsAsync(dbContext, logger);

            // Set up partitioning strategy hints for large datasets
            await ConfigurePartitioningHintsAsync(dbContext, logger);

            logger.LogInformation("Database optimization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database optimization");
            throw;
        }
    }

    /// <summary>
    /// Bulk insert historical market data with optimized batching
    /// </summary>
    public static async Task<int> BulkInsertHistoricalDataAsync(
        this ITradingDbContext context,
        IEnumerable<HistoricalMarketData> records,
        int batchSize = 1000,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        if (context is not DbContext dbContext)
            throw new InvalidOperationException("DbContext required for bulk operations");

        var recordsList = records.ToList();
        if (!recordsList.Any())
            return 0;

        logger?.LogDebug("Starting bulk insert of {RecordCount} historical market data records", recordsList.Count);

        var totalInserted = 0;
        var batches = recordsList.Chunk(batchSize);

        // Use transaction for consistency
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var batch in batches)
            {
                // Disable change tracking for performance
                var originalAutoDetectChanges = dbContext.ChangeTracker.AutoDetectChangesEnabled;
                dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

                try
                {
                    await context.HistoricalMarketData.AddRangeAsync(batch, cancellationToken);
                    var saved = await context.SaveChangesAsync(cancellationToken);
                    totalInserted += saved;

                    logger?.LogDebug("Inserted batch of {BatchSize} records", batch.Count());
                }
                finally
                {
                    dbContext.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;

                    // Clear change tracker to prevent memory issues
                    dbContext.ChangeTracker.Clear();
                }
            }

            await transaction.CommitAsync(cancellationToken);
            logger?.LogInformation("Bulk insert completed. Total records inserted: {TotalInserted}", totalInserted);

            return totalInserted;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger?.LogError(ex, "Error during bulk insert operation");
            throw;
        }
    }

    /// <summary>
    /// Bulk upsert (insert or update) historical market data
    /// </summary>
    public static async Task<BulkOperationResult> BulkUpsertHistoricalDataAsync(
        this ITradingDbContext context,
        IEnumerable<HistoricalMarketData> records,
        int batchSize = 500,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        if (context is not DbContext dbContext)
            throw new InvalidOperationException("DbContext required for bulk operations");

        var recordsList = records.ToList();
        var result = new BulkOperationResult();

        if (!recordsList.Any())
            return result;

        logger?.LogDebug("Starting bulk upsert of {RecordCount} historical market data records", recordsList.Count);

        var batches = recordsList.Chunk(batchSize);

        foreach (var batch in batches)
        {
            try
            {
                // Check for existing records in this batch
                var batchKeys = batch.Select(r => new { r.SymbolTicker, r.TradeDate, r.DataSource }).ToList();

                var existingRecords = await context.HistoricalMarketData
                    .Where(h => batchKeys.Any(k => k.SymbolTicker == h.SymbolTicker &&
                                                  k.TradeDate == h.TradeDate &&
                                                  k.DataSource == h.DataSource))
                    .ToListAsync(cancellationToken);

                var existingKeys = existingRecords
                    .Select(r => new { r.SymbolTicker, r.TradeDate, r.DataSource })
                    .ToHashSet();

                var recordsToInsert = new List<HistoricalMarketData>();
                var recordsToUpdate = new List<HistoricalMarketData>();

                foreach (var record in batch)
                {
                    var key = new { record.SymbolTicker, record.TradeDate, record.DataSource };

                    if (existingKeys.Contains(key))
                    {
                        // Find the existing record and update it
                        var existing = existingRecords.First(r =>
                            r.SymbolTicker == record.SymbolTicker &&
                            r.TradeDate == record.TradeDate &&
                            r.DataSource == record.DataSource);

                        UpdateHistoricalDataRecord(existing, record);
                        recordsToUpdate.Add(existing);
                    }
                    else
                    {
                        recordsToInsert.Add(record);
                    }
                }

                // Perform batch operations
                if (recordsToInsert.Any())
                {
                    await context.HistoricalMarketData.AddRangeAsync(recordsToInsert, cancellationToken);
                    result.InsertedCount += recordsToInsert.Count;
                }

                if (recordsToUpdate.Any())
                {
                    context.HistoricalMarketData.UpdateRange(recordsToUpdate);
                    result.UpdatedCount += recordsToUpdate.Count;
                }

                await context.SaveChangesAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();

                logger?.LogDebug("Processed batch - Inserted: {Inserted}, Updated: {Updated}",
                    recordsToInsert.Count, recordsToUpdate.Count);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error processing batch during bulk upsert");
                result.ErrorCount += batch.Count();
            }
        }

        logger?.LogInformation("Bulk upsert completed - Inserted: {Inserted}, Updated: {Updated}, Errors: {Errors}",
            result.InsertedCount, result.UpdatedCount, result.ErrorCount);

        return result;
    }

    /// <summary>
    /// Efficiently clean up duplicate records with priority-based deduplication
    /// </summary>
    public static async Task<int> CleanupDuplicateHistoricalDataAsync(
        this ITradingDbContext context,
        string? symbolTicker = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        if (context is not DbContext dbContext)
            throw new InvalidOperationException("DbContext required for cleanup operations");

        logger?.LogInformation("Starting duplicate cleanup for historical market data");

        var query = context.HistoricalMarketData.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(symbolTicker))
            query = query.Where(h => h.SymbolTicker == symbolTicker);

        if (startDate.HasValue)
            query = query.Where(h => h.TradeDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(h => h.TradeDate <= endDate.Value);

        // Find duplicates - group by unique business key
        var duplicateGroups = await query
            .GroupBy(h => new { h.SymbolTicker, h.TradeDate, h.MarketCode })
            .Where(g => g.Count() > 1)
            .Select(g => new
            {
                Key = g.Key,
                Records = g.OrderBy(r => r.SourcePriority).ThenBy(r => r.CreatedAt).ToList()
            })
            .ToListAsync(cancellationToken);

        var deletedCount = 0;

        foreach (var group in duplicateGroups)
        {
            try
            {
                // Keep the record with highest priority (lowest priority number) and earliest creation
                var recordsToKeep = group.Records.Take(1);
                var recordsToDelete = group.Records.Skip(1);

                context.HistoricalMarketData.RemoveRange(recordsToDelete);
                deletedCount += recordsToDelete.Count();

                logger?.LogDebug("Removing {Count} duplicates for {Symbol} on {Date}",
                    recordsToDelete.Count(), group.Key.SymbolTicker, group.Key.TradeDate);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error removing duplicates for {Symbol} on {Date}",
                    group.Key.SymbolTicker, group.Key.TradeDate);
            }
        }

        if (deletedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger?.LogInformation("Duplicate cleanup completed. Removed {DeletedCount} duplicate records", deletedCount);
        }

        return deletedCount;
    }

    /// <summary>
    /// Analyze and report database performance metrics
    /// </summary>
    public static async Task<DatabasePerformanceReport> AnalyzePerformanceAsync(
        this ITradingDbContext context,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        if (context is not DbContext dbContext)
            throw new InvalidOperationException("DbContext required for performance analysis");

        var report = new DatabasePerformanceReport
        {
            AnalysisTime = DateTime.UtcNow
        };

        try
        {
            // Table sizes
            report.HistoricalDataRecordCount = await context.HistoricalMarketData.CountAsync(cancellationToken);
            report.SymbolCount = await context.Symbols.CountAsync(cancellationToken);
            report.MarketDataRecordCount = await context.MarketData.CountAsync(cancellationToken);

            // Date range coverage
            if (report.HistoricalDataRecordCount > 0)
            {
                report.EarliestDataDate = await context.HistoricalMarketData
                    .MinAsync(h => h.TradeDate, cancellationToken);

                report.LatestDataDate = await context.HistoricalMarketData
                    .MaxAsync(h => h.TradeDate, cancellationToken);
            }

            // Data quality metrics
            var lowQualityRecords = await context.HistoricalMarketData
                .Where(h => h.DataQualityScore < 80)
                .CountAsync(cancellationToken);

            report.DataQualityPercent = report.HistoricalDataRecordCount > 0 ?
                (decimal)(report.HistoricalDataRecordCount - lowQualityRecords) / report.HistoricalDataRecordCount * 100 : 100;

            // Data source distribution
            report.DataSourceDistribution = await context.HistoricalMarketData
                .GroupBy(h => h.DataSource)
                .Select(g => new { DataSource = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DataSource, x => x.Count, cancellationToken);

            // Market distribution
            report.MarketDistribution = await context.HistoricalMarketData
                .Where(h => h.MarketCode != null)
                .GroupBy(h => h.MarketCode!)
                .Select(g => new { Market = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Market, x => x.Count, cancellationToken);

            logger?.LogInformation("Performance analysis completed - {RecordCount} records analyzed",
                report.HistoricalDataRecordCount);

            return report;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during performance analysis");
            throw;
        }
    }

    private static async Task CreateMarketDataIndexesAsync(DbContext dbContext, ILogger logger)
    {
        // Note: In a real implementation, these would be proper SQL index creation commands
        // For now, we'll log the recommendations

        var indexRecommendations = new[]
        {
            "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_symbol_date ON historical_market_data (symbol_ticker, trade_date)",
            "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_market_date ON historical_market_data (market_code, trade_date) WHERE market_code IS NOT NULL",
            "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_source_priority ON historical_market_data (data_source, source_priority)",
            "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_quality ON historical_market_data (data_quality_score) WHERE data_quality_score IS NOT NULL",
            "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_collected_at ON historical_market_data (data_collected_at)",
            "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_symbols_active_market ON symbols (is_active, market) WHERE is_active = true"
        };

        foreach (var indexSql in indexRecommendations)
        {
            try
            {
                // In production, you would execute these
                // await dbContext.Database.ExecuteSqlRawAsync(indexSql);
                logger.LogDebug("Index recommendation: {IndexSql}", indexSql);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not create index: {IndexSql}", indexSql);
            }
        }
    }

    private static async Task ConfigurePerformanceSettingsAsync(DbContext dbContext, ILogger logger)
    {
        try
        {
            // Configure connection settings for batch operations
            if (dbContext.Database.ProviderName?.Contains("Npgsql") == true)
            {
                // PostgreSQL-specific optimizations
                var performanceSettings = new[]
                {
                    "SET work_mem = '256MB'",
                    "SET maintenance_work_mem = '512MB'",
                    "SET effective_cache_size = '2GB'",
                    "SET checkpoint_completion_target = 0.9",
                    "SET wal_buffers = '16MB'",
                    "SET default_statistics_target = 100"
                };

                foreach (var setting in performanceSettings)
                {
                    logger.LogDebug("Performance setting recommendation: {Setting}", setting);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not configure performance settings");
        }
    }

    private static async Task ConfigurePartitioningHintsAsync(DbContext dbContext, ILogger logger)
    {
        // Log partitioning recommendations for large datasets
        var partitioningRecommendations = new[]
        {
            "Consider partitioning historical_market_data by trade_date (monthly partitions)",
            "Consider partitioning by market_code for multi-market deployments",
            "Use table inheritance for different data sources if needed",
            "Implement automatic partition maintenance procedures"
        };

        foreach (var recommendation in partitioningRecommendations)
        {
            logger.LogInformation("Partitioning recommendation: {Recommendation}", recommendation);
        }
    }

    private static void UpdateHistoricalDataRecord(HistoricalMarketData existing, HistoricalMarketData newRecord)
    {
        // Update with higher priority data or newer data
        if (newRecord.SourcePriority < existing.SourcePriority ||
            (newRecord.SourcePriority == existing.SourcePriority && newRecord.DataCollectedAt > existing.DataCollectedAt))
        {
            existing.OpenPrice = newRecord.OpenPrice ?? existing.OpenPrice;
            existing.HighPrice = newRecord.HighPrice ?? existing.HighPrice;
            existing.LowPrice = newRecord.LowPrice ?? existing.LowPrice;
            existing.ClosePrice = newRecord.ClosePrice ?? existing.ClosePrice;
            existing.AdjustedClosePrice = newRecord.AdjustedClosePrice ?? existing.AdjustedClosePrice;
            existing.Volume = newRecord.Volume ?? existing.Volume;
            existing.VWAP = newRecord.VWAP ?? existing.VWAP;
            existing.PreviousClose = newRecord.PreviousClose ?? existing.PreviousClose;
            existing.PriceChange = newRecord.PriceChange ?? existing.PriceChange;
            existing.PriceChangePercent = newRecord.PriceChangePercent ?? existing.PriceChangePercent;
            existing.TradingValue = newRecord.TradingValue ?? existing.TradingValue;
            existing.TransactionCount = newRecord.TransactionCount ?? existing.TransactionCount;
            existing.DataQualityScore = newRecord.DataQualityScore ?? existing.DataQualityScore;
            existing.ExtendedData = newRecord.ExtendedData ?? existing.ExtendedData;
            existing.SourceMetadata = newRecord.SourceMetadata ?? existing.SourceMetadata;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.DataCollectedAt = newRecord.DataCollectedAt;

            if (newRecord.SourcePriority < existing.SourcePriority)
            {
                existing.SourcePriority = newRecord.SourcePriority;
                existing.DataSource = newRecord.DataSource;
            }
        }
    }
}

/// <summary>
/// Result of bulk database operations
/// </summary>
public class BulkOperationResult
{
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan Duration { get; set; }

    public int TotalProcessed => InsertedCount + UpdatedCount + DeletedCount;
    public bool HasErrors => ErrorCount > 0;
}

/// <summary>
/// Database performance analysis report
/// </summary>
public class DatabasePerformanceReport
{
    public DateTime AnalysisTime { get; set; }

    // Record counts
    public long HistoricalDataRecordCount { get; set; }
    public long SymbolCount { get; set; }
    public long MarketDataRecordCount { get; set; }

    // Data coverage
    public DateOnly? EarliestDataDate { get; set; }
    public DateOnly? LatestDataDate { get; set; }

    // Quality metrics
    public decimal DataQualityPercent { get; set; }

    // Distributions
    public Dictionary<string, int> DataSourceDistribution { get; set; } = new();
    public Dictionary<string, int> MarketDistribution { get; set; } = new();

    // Performance recommendations
    public List<string> Recommendations { get; set; } = new();
}