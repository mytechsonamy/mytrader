# Historical Market Data Schema Documentation

## Overview

The Historical Market Data schema is designed to support comprehensive storage and analysis of financial time-series data across multiple asset classes, with special emphasis on both standard OHLCV data and BIST (Borsa Istanbul) specific detailed information.

## Architecture Overview

### Design Principles

1. **Hybrid Approach**: Supports both standard OHLCV and BIST detailed formats in a single unified schema
2. **Performance Optimized**: Time-based partitioning, strategic indexing, and pre-computed aggregations
3. **Data Quality**: Built-in validation, scoring, and integrity constraints
4. **Scalability**: Horizontal partitioning, automatic maintenance, and efficient storage
5. **Extensibility**: JSON columns for flexible metadata and future enhancements

### Key Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Historical Market Data                   │
│                       (Partitioned)                        │
├─────────────────────────────────────────────────────────────┤
│ • Standard OHLCV Data                                       │
│ • BIST Specific Fields (32+ columns)                       │
│ • Technical Indicators                                      │
│ • Data Quality Metrics                                     │
│ • Flexible JSON Metadata                                   │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                  Market Data Summaries                     │
│                    (Pre-aggregated)                        │
├─────────────────────────────────────────────────────────────┤
│ • Performance Metrics                                       │
│ • Volatility Analysis                                      │
│ • Risk Metrics                                             │
│ • Ranking & Percentiles                                    │
└─────────────────────────────────────────────────────────────┘
```

## Table Structure

### 1. HistoricalMarketData

Primary table for storing time-series market data with support for multiple formats.

#### Core Fields

| Field | Type | Description | Required |
|-------|------|-------------|----------|
| `id` | UUID | Primary key | ✓ |
| `symbol_id` | UUID | Foreign key to symbols table | ✓ |
| `symbol_ticker` | VARCHAR(50) | Denormalized ticker for performance | ✓ |
| `data_source` | VARCHAR(20) | Data provider (BIST, BINANCE, etc.) | ✓ |
| `trade_date` | DATE | Trading date (partition key) | ✓ |
| `timeframe` | VARCHAR(10) | Data frequency (DAILY, 1h, etc.) | ✓ |

#### Standard OHLCV Data

| Field | Type | Description |
|-------|------|-------------|
| `open_price` | DECIMAL(18,8) | Opening price |
| `high_price` | DECIMAL(18,8) | Highest price |
| `low_price` | DECIMAL(18,8) | Lowest price |
| `close_price` | DECIMAL(18,8) | Closing price |
| `adjusted_close_price` | DECIMAL(18,8) | Adjusted for splits/dividends |
| `volume` | DECIMAL(38,18) | Trading volume |
| `vwap` | DECIMAL(18,8) | Volume Weighted Average Price |

#### BIST Specific Data

| Field | Type | Description |
|-------|------|-------------|
| `bist_code` | VARCHAR(20) | BIST legacy code |
| `previous_close` | DECIMAL(18,8) | Previous day's close |
| `price_change` | DECIMAL(18,8) | Absolute price change |
| `price_change_percent` | DECIMAL(10,4) | Percentage price change |
| `trading_value` | DECIMAL(38,18) | Total trading value |
| `transaction_count` | BIGINT | Number of transactions |
| `market_cap` | DECIMAL(38,18) | Market capitalization |
| `free_float_market_cap` | DECIMAL(38,18) | Free float market cap |
| `shares_outstanding` | DECIMAL(38,18) | Total shares outstanding |
| `free_float_shares` | DECIMAL(38,18) | Free float shares |

#### Technical Indicators

| Field | Type | Description |
|-------|------|-------------|
| `rsi` | DECIMAL(10,4) | Relative Strength Index |
| `macd` | DECIMAL(18,8) | MACD line |
| `macd_signal` | DECIMAL(18,8) | MACD signal line |
| `bollinger_upper` | DECIMAL(18,8) | Bollinger Band upper |
| `bollinger_lower` | DECIMAL(18,8) | Bollinger Band lower |
| `sma_20` | DECIMAL(18,8) | 20-period Simple Moving Average |
| `sma_50` | DECIMAL(18,8) | 50-period Simple Moving Average |
| `sma_200` | DECIMAL(18,8) | 200-period Simple Moving Average |

#### Data Quality & Metadata

| Field | Type | Description |
|-------|------|-------------|
| `data_quality_score` | INTEGER | Quality score (0-100) |
| `extended_data` | JSONB | Flexible additional data |
| `source_metadata` | JSONB | Data source information |
| `data_flags` | INTEGER | Bit flags for data characteristics |
| `source_priority` | INTEGER | Priority for deduplication (1=highest) |

### 2. MarketDataSummary

Pre-aggregated summaries for fast analytical queries.

#### Performance Metrics

| Field | Type | Description |
|-------|------|-------------|
| `total_return_percent` | DECIMAL(10,4) | Total return for period |
| `avg_daily_return_percent` | DECIMAL(10,4) | Average daily return |
| `volatility` | DECIMAL(10,6) | Standard deviation of returns |
| `annualized_volatility` | DECIMAL(10,6) | Annualized volatility |
| `sharpe_ratio` | DECIMAL(10,4) | Risk-adjusted return |
| `max_drawdown_percent` | DECIMAL(10,4) | Maximum drawdown |
| `beta` | DECIMAL(10,4) | Market beta coefficient |

#### Volume Statistics

| Field | Type | Description |
|-------|------|-------------|
| `total_volume` | DECIMAL(38,18) | Total volume for period |
| `avg_daily_volume` | DECIMAL(38,18) | Average daily volume |
| `total_trading_value` | DECIMAL(38,18) | Total trading value |
| `avg_daily_trading_value` | DECIMAL(38,18) | Average daily trading value |

## Partitioning Strategy

### Monthly Partitioning (HistoricalMarketData)

```sql
-- Partition naming: historical_market_data_YYYY_MM
historical_market_data_2024_01  -- January 2024
historical_market_data_2024_02  -- February 2024
...
```

### Yearly Partitioning (MarketDataSummary)

```sql
-- Partition naming: market_data_summaries_YYYY
market_data_summaries_2024      -- Year 2024
market_data_summaries_2025      -- Year 2025
```

### Partition Management

```sql
-- Create future partitions automatically
SELECT ensure_partitions_exist();

-- Drop old partitions (keep 24 months)
SELECT drop_old_partitions(24);

-- Archive old data to separate tablespace
SELECT archive_old_partitions(12, 'archive_data');
```

## Index Strategy

### Primary Performance Indexes

1. **Time-Series Primary Index**
   ```sql
   CREATE INDEX idx_historical_market_data_primary
   ON historical_market_data (symbol_ticker, timeframe, trade_date DESC);
   ```

2. **Symbol-Based Queries**
   ```sql
   CREATE INDEX idx_historical_market_data_symbol_date
   ON historical_market_data (symbol_id, trade_date DESC, timeframe);
   ```

3. **Volume Analysis**
   ```sql
   CREATE INDEX idx_historical_market_data_volume
   ON historical_market_data (trade_date DESC, volume DESC)
   WHERE volume IS NOT NULL;
   ```

### BIST Specific Indexes

```sql
CREATE INDEX idx_historical_market_data_bist
ON historical_market_data (bist_code, trade_date DESC)
WHERE bist_code IS NOT NULL;
```

### Technical Analysis Indexes

```sql
CREATE INDEX idx_historical_market_data_technical
ON historical_market_data (trade_date DESC, rsi, macd)
WHERE rsi IS NOT NULL OR macd IS NOT NULL;
```

## Data Quality Framework

### Quality Score Calculation

```sql
CREATE OR REPLACE FUNCTION calculate_data_quality_score(data historical_market_data)
RETURNS INTEGER AS $$
DECLARE
    score INTEGER := 100;
BEGIN
    -- Deduct for missing essential fields
    IF data.open_price IS NULL THEN score := score - 15; END IF;
    IF data.high_price IS NULL THEN score := score - 15; END IF;
    IF data.low_price IS NULL THEN score := score - 15; END IF;
    IF data.close_price IS NULL THEN score := score - 20; END IF;
    IF data.volume IS NULL THEN score := score - 10; END IF;

    -- Bonus for additional data
    IF data.trading_value IS NOT NULL THEN score := score + 5; END IF;
    IF data.transaction_count IS NOT NULL THEN score := score + 5; END IF;
    IF data.market_cap IS NOT NULL THEN score := score + 5; END IF;

    RETURN GREATEST(0, LEAST(100, score));
END;
$$ LANGUAGE plpgsql;
```

### Data Validation Rules

1. **Price Consistency**: High >= Max(Open, Close), Low <= Min(Open, Close)
2. **Volume Validation**: Non-negative values
3. **Date Validation**: Trade date <= Current date
4. **Technical Indicators**: RSI between 0-100
5. **Currency Validation**: Valid ISO currency codes

### Quality Monitoring

```sql
-- Check data quality issues
SELECT * FROM audit_data_quality() LIMIT 20;

-- Monitor quality by source
SELECT data_source, AVG(data_quality_score) as avg_quality
FROM historical_market_data
GROUP BY data_source
ORDER BY avg_quality DESC;
```

## Usage Examples

### 1. Basic Price Query

```sql
-- Get daily prices for AAPL in 2024
SELECT trade_date, open_price, high_price, low_price, close_price, volume
FROM historical_market_data
WHERE symbol_ticker = 'AAPL'
  AND timeframe = 'DAILY'
  AND trade_date BETWEEN '2024-01-01' AND '2024-12-31'
ORDER BY trade_date DESC;
```

### 2. BIST Specific Query

```sql
-- Get BIST data with Turkish market details
SELECT trade_date, close_price, trading_value, transaction_count,
       market_cap, usd_try_rate
FROM historical_market_data
WHERE data_source = 'BIST'
  AND symbol_ticker = 'THYAO'
  AND trade_date >= CURRENT_DATE - INTERVAL '30 days'
ORDER BY trade_date DESC;
```

### 3. Technical Analysis Query

```sql
-- Find oversold stocks (RSI < 30)
SELECT DISTINCT symbol_ticker, trade_date, close_price, rsi
FROM historical_market_data
WHERE trade_date = CURRENT_DATE - INTERVAL '1 day'
  AND rsi < 30
  AND rsi IS NOT NULL
ORDER BY rsi ASC;
```

### 4. Volume Analysis

```sql
-- Top 10 most traded stocks by volume
SELECT symbol_ticker, SUM(volume) as total_volume,
       AVG(close_price) as avg_price
FROM historical_market_data
WHERE trade_date >= CURRENT_DATE - INTERVAL '7 days'
  AND timeframe = 'DAILY'
GROUP BY symbol_ticker
ORDER BY total_volume DESC
LIMIT 10;
```

### 5. Performance Analysis with Summaries

```sql
-- Best performing stocks this month
SELECT symbol_ticker, total_return_percent, volatility, sharpe_ratio
FROM market_data_summaries
WHERE period_type = 'MONTHLY'
  AND period_start = DATE_TRUNC('month', CURRENT_DATE)
  AND quality_score >= 80
ORDER BY total_return_percent DESC
LIMIT 20;
```

### 6. Cross-Market Correlation

```sql
-- Compare BIST vs US market performance
WITH bist_performance AS (
    SELECT trade_date, AVG(price_change_percent) as bist_avg_change
    FROM historical_market_data
    WHERE data_source = 'BIST'
      AND trade_date >= CURRENT_DATE - INTERVAL '30 days'
    GROUP BY trade_date
),
us_performance AS (
    SELECT trade_date, AVG(price_change_percent) as us_avg_change
    FROM historical_market_data
    WHERE data_source IN ('YAHOO', 'IEX')
      AND market_code IN ('NASDAQ', 'NYSE')
      AND trade_date >= CURRENT_DATE - INTERVAL '30 days'
    GROUP BY trade_date
)
SELECT b.trade_date, b.bist_avg_change, u.us_avg_change,
       CORR(b.bist_avg_change, u.us_avg_change) OVER (
           ORDER BY b.trade_date ROWS BETWEEN 9 PRECEDING AND CURRENT ROW
       ) as rolling_correlation_10d
FROM bist_performance b
JOIN us_performance u ON b.trade_date = u.trade_date
ORDER BY b.trade_date DESC;
```

## Entity Framework Usage

### Repository Pattern

```csharp
public class HistoricalDataRepository
{
    private readonly TradingDbContext _context;

    public async Task<List<HistoricalMarketData>> GetPriceHistoryAsync(
        string symbolTicker,
        DateOnly startDate,
        DateOnly endDate,
        string timeframe = "DAILY")
    {
        return await _context.HistoricalMarketData
            .Where(h => h.SymbolTicker == symbolTicker
                     && h.TradeDate >= startDate
                     && h.TradeDate <= endDate
                     && h.Timeframe == timeframe)
            .OrderByDescending(h => h.TradeDate)
            .ToListAsync();
    }

    public async Task<List<MarketDataSummary>> GetPerformanceRankingAsync(
        string periodType = "MONTHLY",
        int limit = 50)
    {
        var latestPeriod = await _context.MarketDataSummaries
            .Where(s => s.PeriodType == periodType)
            .MaxAsync(s => s.PeriodStart);

        return await _context.MarketDataSummaries
            .Where(s => s.PeriodType == periodType
                     && s.PeriodStart == latestPeriod
                     && s.QualityScore >= 80)
            .OrderByDescending(s => s.TotalReturnPercent)
            .Take(limit)
            .ToListAsync();
    }
}
```

### Bulk Data Insertion

```csharp
public async Task BulkInsertHistoricalDataAsync(
    List<HistoricalMarketData> data)
{
    // Use EF Core bulk extensions for performance
    await _context.BulkInsertAsync(data, options => {
        options.BatchSize = 1000;
        options.IgnoreOnInsertExpression = e => new { e.Id, e.CreatedAt };
    });
}
```

## Performance Optimization

### Query Performance Tips

1. **Always include date range** in WHERE clauses for partition pruning
2. **Use symbol_ticker** instead of joins when possible
3. **Limit result sets** with LIMIT or TOP clauses
4. **Use summary tables** for analytical queries
5. **Consider materialized views** for complex aggregations

### Index Maintenance

```sql
-- Monitor index usage
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read
FROM pg_stat_user_indexes
WHERE tablename LIKE 'historical_market_data%'
ORDER BY idx_scan DESC;

-- Rebuild indexes monthly
REINDEX TABLE historical_market_data;
```

### Partition Maintenance

```sql
-- Daily maintenance (run via cron)
SELECT ensure_partitions_exist();
ANALYZE historical_market_data;

-- Monthly cleanup
SELECT drop_old_partitions(24);
```

## Data Privacy & Compliance

### PII Identification

- **No direct PII** stored in market data tables
- **Audit trails** maintain data lineage
- **Access controls** via application layer

### Data Retention

- **Default retention**: 24 months in active partitions
- **Archive strategy**: Move to cold storage after 12 months
- **Regulatory compliance**: Configurable retention periods

### Data Masking for Non-Production

```sql
-- Mask sensitive data for development environments
UPDATE historical_market_data
SET extended_data = NULL,
    source_metadata = NULL
WHERE created_at < CURRENT_DATE - INTERVAL '1 year';
```

## Monitoring & Alerting

### Key Metrics to Track

1. **Data Ingestion Rate**: Records per minute
2. **Query Performance**: Average query time < 100ms
3. **Data Quality Score**: Average > 85%
4. **Partition Growth**: Size and count monitoring
5. **Index Hit Ratio**: > 95%

### Alert Conditions

```sql
-- Data quality alerts
SELECT COUNT(*) as low_quality_records
FROM historical_market_data
WHERE data_quality_score < 70
  AND trade_date >= CURRENT_DATE - INTERVAL '1 day';

-- Missing data alerts
SELECT symbol_ticker, MAX(trade_date) as last_update
FROM historical_market_data
WHERE timeframe = 'DAILY'
GROUP BY symbol_ticker
HAVING MAX(trade_date) < CURRENT_DATE - INTERVAL '2 days';
```

## Backup & Recovery

### Backup Strategy

1. **Full backup**: Weekly full database backup
2. **Incremental backup**: Daily incremental backups
3. **Partition-level backup**: Monthly archives
4. **Point-in-time recovery**: WAL archiving enabled

### Recovery Procedures

```sql
-- Restore specific partition
RESTORE TABLE historical_market_data_2024_01
FROM '/backup/path/historical_market_data_2024_01.backup';

-- Verify data integrity after restore
SELECT COUNT(*), MIN(trade_date), MAX(trade_date)
FROM historical_market_data_2024_01;
```

## Migration Checklist

### Pre-Migration

- [ ] Review existing data volume and structure
- [ ] Plan partition strategy based on data volume
- [ ] Identify required indexes for your queries
- [ ] Set up monitoring and alerting

### Migration Steps

1. [ ] Run migration script
2. [ ] Create initial partitions
3. [ ] Import existing data with quality validation
4. [ ] Create performance indexes
5. [ ] Set up automated maintenance jobs
6. [ ] Configure monitoring and alerts
7. [ ] Test query performance
8. [ ] Update application code

### Post-Migration

- [ ] Monitor partition sizes and performance
- [ ] Track data quality metrics
- [ ] Optimize indexes based on usage patterns
- [ ] Set up regular maintenance procedures

## Troubleshooting

### Common Issues

1. **Partition pruning not working**
   - Ensure WHERE clause includes trade_date
   - Check constraint exclusion setting

2. **Slow queries**
   - Verify appropriate indexes exist
   - Use EXPLAIN ANALYZE to identify bottlenecks
   - Consider query rewriting

3. **Data quality issues**
   - Run audit_data_quality() function
   - Check data source configurations
   - Validate data transformation logic

4. **Partition management**
   - Monitor disk space usage
   - Ensure automated partition creation works
   - Check maintenance job logs

### Performance Tuning

```sql
-- Check query plans
EXPLAIN (ANALYZE, BUFFERS, COSTS, VERBOSE)
SELECT * FROM historical_market_data
WHERE symbol_ticker = 'AAPL' AND trade_date >= '2024-01-01';

-- Monitor table statistics
SELECT
    schemaname, tablename, n_tup_ins, n_tup_upd, n_tup_del,
    last_vacuum, last_autovacuum, last_analyze, last_autoanalyze
FROM pg_stat_user_tables
WHERE tablename LIKE 'historical_market_data%';
```

## Conclusion

This historical market data schema provides a robust, scalable foundation for financial data analysis with built-in support for multiple data formats, comprehensive data quality management, and optimized performance for time-series queries. The hybrid approach successfully accommodates both standard OHLCV data and BIST's detailed format requirements while maintaining excellent query performance through strategic partitioning and indexing.