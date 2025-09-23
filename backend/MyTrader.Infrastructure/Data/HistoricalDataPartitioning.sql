-- =============================================================================
-- HISTORICAL MARKET DATA PARTITIONING STRATEGY
-- Time-based partitioning for optimal performance and maintenance
-- Supports both date-based and hybrid partitioning approaches
-- =============================================================================

-- === PARTITIONING OVERVIEW ===
/*
Strategy: Monthly partitioning by trade_date with automatic partition management
Benefits:
- Query performance: Partition pruning for date range queries
- Maintenance: Easy archival and backup of old data
- Parallel processing: Different partitions can be processed concurrently
- Index efficiency: Smaller indexes per partition

Partition Naming Convention:
- historical_market_data_YYYY_MM (e.g., historical_market_data_2024_01)
- market_data_summaries_YYYY (yearly partitions for summaries)
*/

-- === DROP EXISTING TABLE IF RECREATING ===
-- WARNING: Only use this during initial setup
-- DROP TABLE IF EXISTS historical_market_data CASCADE;

-- === CREATE PARTITIONED MAIN TABLE ===

-- 1. Create partitioned historical market data table
CREATE TABLE IF NOT EXISTS historical_market_data (
    id UUID DEFAULT gen_random_uuid(),
    symbol_id UUID NOT NULL,
    symbol_ticker VARCHAR(50) NOT NULL,
    data_source VARCHAR(20) NOT NULL,
    market_code VARCHAR(20),
    trade_date DATE NOT NULL,
    timeframe VARCHAR(10) NOT NULL DEFAULT 'DAILY',
    timestamp TIMESTAMP,

    -- Standard OHLCV data
    open_price DECIMAL(18,8),
    high_price DECIMAL(18,8),
    low_price DECIMAL(18,8),
    close_price DECIMAL(18,8),
    adjusted_close_price DECIMAL(18,8),
    volume DECIMAL(38,18),
    vwap DECIMAL(18,8),

    -- BIST specific data
    bist_code VARCHAR(20),
    previous_close DECIMAL(18,8),
    price_change DECIMAL(18,8),
    price_change_percent DECIMAL(10,4),
    trading_value DECIMAL(38,18),
    transaction_count BIGINT,
    market_cap DECIMAL(38,18),
    free_float_market_cap DECIMAL(38,18),
    shares_outstanding DECIMAL(38,18),
    free_float_shares DECIMAL(38,18),

    -- Index and currency data
    index_value DECIMAL(18,8),
    index_change_percent DECIMAL(10,4),
    usd_try_rate DECIMAL(18,8),
    eur_try_rate DECIMAL(18,8),

    -- Technical indicators
    rsi DECIMAL(10,4),
    macd DECIMAL(18,8),
    macd_signal DECIMAL(18,8),
    bollinger_upper DECIMAL(18,8),
    bollinger_lower DECIMAL(18,8),
    sma_20 DECIMAL(18,8),
    sma_50 DECIMAL(18,8),
    sma_200 DECIMAL(18,8),

    -- Metadata
    currency VARCHAR(12) DEFAULT 'USD',
    data_quality_score INTEGER,
    extended_data JSONB,
    source_metadata JSONB,
    data_flags INTEGER DEFAULT 0,
    source_priority INTEGER DEFAULT 10,

    -- Audit fields
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_collected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    -- Partition key must be included in primary key
    PRIMARY KEY (id, trade_date)
) PARTITION BY RANGE (trade_date);

-- 2. Create partitioned summaries table (yearly partitions)
CREATE TABLE IF NOT EXISTS market_data_summaries (
    id UUID DEFAULT gen_random_uuid(),
    symbol_id UUID NOT NULL,
    symbol_ticker VARCHAR(50) NOT NULL,
    period_type VARCHAR(10) NOT NULL,
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    trading_days INTEGER,

    -- Price statistics
    period_open DECIMAL(18,8),
    period_close DECIMAL(18,8),
    period_high DECIMAL(18,8),
    period_low DECIMAL(18,8),
    period_vwap DECIMAL(18,8),
    total_return_percent DECIMAL(10,4),
    avg_daily_return_percent DECIMAL(10,4),
    volatility DECIMAL(10,6),
    annualized_volatility DECIMAL(10,6),
    sharpe_ratio DECIMAL(10,4),
    max_drawdown_percent DECIMAL(10,4),
    beta DECIMAL(10,4),

    -- Volume statistics
    total_volume DECIMAL(38,18),
    avg_daily_volume DECIMAL(38,18),
    total_trading_value DECIMAL(38,18),
    avg_daily_trading_value DECIMAL(38,18),
    total_transactions BIGINT,
    avg_daily_transactions BIGINT,

    -- Price levels
    support_level DECIMAL(18,8),
    resistance_level DECIMAL(18,8),
    week_52_high DECIMAL(18,8),
    week_52_low DECIMAL(18,8),

    -- Technical indicators summary
    avg_rsi DECIMAL(10,4),
    avg_macd DECIMAL(18,8),
    days_above_sma20_percent DECIMAL(10,2),
    days_above_sma50_percent DECIMAL(10,2),

    -- Market comparison
    vs_market_percent DECIMAL(10,4),
    market_correlation DECIMAL(10,6),

    -- Ranking metrics
    performance_percentile INTEGER,
    volume_percentile INTEGER,
    quality_score INTEGER DEFAULT 100,

    -- Audit fields
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    calculated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id, period_start)
) PARTITION BY RANGE (period_start);

-- === AUTOMATIC PARTITION MANAGEMENT ===

-- 3. Function to create monthly partitions for historical data
CREATE OR REPLACE FUNCTION create_monthly_partition(
    table_name TEXT,
    start_date DATE
) RETURNS VOID AS $$
DECLARE
    partition_name TEXT;
    end_date DATE;
BEGIN
    -- Calculate partition name and end date
    partition_name := table_name || '_' || TO_CHAR(start_date, 'YYYY_MM');
    end_date := start_date + INTERVAL '1 month';

    -- Create partition if it doesn't exist
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS %I PARTITION OF %I
         FOR VALUES FROM (%L) TO (%L)',
        partition_name, table_name, start_date, end_date
    );

    -- Create partition-specific indexes for optimal performance
    EXECUTE format(
        'CREATE INDEX IF NOT EXISTS %I ON %I (symbol_ticker, trade_date DESC)',
        partition_name || '_symbol_date_idx', partition_name
    );

    EXECUTE format(
        'CREATE INDEX IF NOT EXISTS %I ON %I (trade_date DESC, data_source)',
        partition_name || '_date_source_idx', partition_name
    );

    RAISE NOTICE 'Created partition: %', partition_name;
END;
$$ LANGUAGE plpgsql;

-- 4. Function to create yearly partitions for summaries
CREATE OR REPLACE FUNCTION create_yearly_partition(
    table_name TEXT,
    start_date DATE
) RETURNS VOID AS $$
DECLARE
    partition_name TEXT;
    end_date DATE;
BEGIN
    partition_name := table_name || '_' || TO_CHAR(start_date, 'YYYY');
    end_date := start_date + INTERVAL '1 year';

    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS %I PARTITION OF %I
         FOR VALUES FROM (%L) TO (%L)',
        partition_name, table_name, start_date, end_date
    );

    -- Create partition-specific indexes
    EXECUTE format(
        'CREATE INDEX IF NOT EXISTS %I ON %I (symbol_ticker, period_start DESC)',
        partition_name || '_symbol_period_idx', partition_name
    );

    RAISE NOTICE 'Created partition: %', partition_name;
END;
$$ LANGUAGE plpgsql;

-- 5. Automatic partition creation function
CREATE OR REPLACE FUNCTION ensure_partitions_exist()
RETURNS VOID AS $$
DECLARE
    current_month DATE;
    future_month DATE;
    current_year DATE;
    future_year DATE;
BEGIN
    -- Create monthly partitions for historical data (current + 3 months ahead)
    current_month := DATE_TRUNC('month', CURRENT_DATE);

    FOR i IN 0..3 LOOP
        future_month := current_month + (i || ' months')::INTERVAL;
        PERFORM create_monthly_partition('historical_market_data', future_month);
    END LOOP;

    -- Create yearly partitions for summaries (current + 1 year ahead)
    current_year := DATE_TRUNC('year', CURRENT_DATE);

    FOR i IN 0..1 LOOP
        future_year := current_year + (i || ' years')::INTERVAL;
        PERFORM create_yearly_partition('market_data_summaries', future_year);
    END LOOP;

    RAISE NOTICE 'Partition maintenance completed';
END;
$$ LANGUAGE plpgsql;

-- 6. Initialize current partitions
SELECT ensure_partitions_exist();

-- === PARTITION MAINTENANCE PROCEDURES ===

-- 7. Drop old partitions procedure
CREATE OR REPLACE FUNCTION drop_old_partitions(
    retention_months INTEGER DEFAULT 24
) RETURNS INTEGER AS $$
DECLARE
    partition_record RECORD;
    cutoff_date DATE;
    dropped_count INTEGER := 0;
BEGIN
    cutoff_date := CURRENT_DATE - (retention_months || ' months')::INTERVAL;

    -- Find and drop old historical data partitions
    FOR partition_record IN
        SELECT schemaname, tablename
        FROM pg_tables
        WHERE tablename LIKE 'historical_market_data_%'
          AND tablename ~ '\d{4}_\d{2}$'
          AND TO_DATE(RIGHT(tablename, 7), 'YYYY_MM') < cutoff_date
    LOOP
        EXECUTE format('DROP TABLE IF EXISTS %I.%I CASCADE',
                      partition_record.schemaname,
                      partition_record.tablename);

        RAISE NOTICE 'Dropped old partition: %', partition_record.tablename;
        dropped_count := dropped_count + 1;
    END LOOP;

    RETURN dropped_count;
END;
$$ LANGUAGE plpgsql;

-- 8. Archive old partitions to separate tablespace
CREATE OR REPLACE FUNCTION archive_old_partitions(
    archive_months INTEGER DEFAULT 12,
    archive_tablespace TEXT DEFAULT 'archive_data'
) RETURNS INTEGER AS $$
DECLARE
    partition_record RECORD;
    cutoff_date DATE;
    archived_count INTEGER := 0;
    new_table_name TEXT;
BEGIN
    cutoff_date := CURRENT_DATE - (archive_months || ' months')::INTERVAL;

    FOR partition_record IN
        SELECT schemaname, tablename
        FROM pg_tables
        WHERE tablename LIKE 'historical_market_data_%'
          AND tablename ~ '\d{4}_\d{2}$'
          AND TO_DATE(RIGHT(tablename, 7), 'YYYY_MM') < cutoff_date
    LOOP
        new_table_name := 'archived_' || partition_record.tablename;

        -- Move partition to archive tablespace
        EXECUTE format(
            'CREATE TABLE %I TABLESPACE %I AS SELECT * FROM %I.%I',
            new_table_name,
            archive_tablespace,
            partition_record.schemaname,
            partition_record.tablename
        );

        -- Drop original partition
        EXECUTE format('DROP TABLE IF EXISTS %I.%I CASCADE',
                      partition_record.schemaname,
                      partition_record.tablename);

        RAISE NOTICE 'Archived partition: % -> %', partition_record.tablename, new_table_name;
        archived_count := archived_count + 1;
    END LOOP;

    RETURN archived_count;
END;
$$ LANGUAGE plpgsql;

-- === PARTITION MONITORING AND STATISTICS ===

-- 9. Partition information view
CREATE OR REPLACE VIEW v_partition_info AS
SELECT
    schemaname,
    tablename as partition_name,
    CASE
        WHEN tablename LIKE '%_____' AND RIGHT(tablename, 4) ~ '^\d{4}$' THEN 'YEARLY'
        WHEN tablename LIKE '%______' AND RIGHT(tablename, 7) ~ '^\d{4}_\d{2}$' THEN 'MONTHLY'
        ELSE 'UNKNOWN'
    END as partition_type,
    CASE
        WHEN tablename LIKE '%_____' AND RIGHT(tablename, 4) ~ '^\d{4}$' THEN
            TO_DATE(RIGHT(tablename, 4), 'YYYY')
        WHEN tablename LIKE '%______' AND RIGHT(tablename, 7) ~ '^\d{4}_\d{2}$' THEN
            TO_DATE(RIGHT(tablename, 7), 'YYYY_MM')
        ELSE NULL
    END as partition_date,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size,
    pg_total_relation_size(schemaname||'.'||tablename) as size_bytes
FROM pg_tables
WHERE tablename LIKE 'historical_market_data_%'
   OR tablename LIKE 'market_data_summaries_%'
ORDER BY partition_date DESC;

-- 10. Partition statistics function
CREATE OR REPLACE FUNCTION get_partition_stats()
RETURNS TABLE (
    partition_name TEXT,
    record_count BIGINT,
    earliest_date DATE,
    latest_date DATE,
    size_mb NUMERIC,
    compression_ratio NUMERIC
) AS $$
DECLARE
    partition_record RECORD;
BEGIN
    FOR partition_record IN
        SELECT tablename
        FROM pg_tables
        WHERE (tablename LIKE 'historical_market_data_%'
               OR tablename LIKE 'market_data_summaries_%')
          AND tablename ~ '\d{4}(_\d{2})?$'
    LOOP
        RETURN QUERY
        EXECUTE format('
            SELECT %L::TEXT,
                   COUNT(*)::BIGINT,
                   MIN(trade_date)::DATE,
                   MAX(trade_date)::DATE,
                   (pg_total_relation_size(%L) / 1024.0 / 1024.0)::NUMERIC(10,2),
                   CASE
                       WHEN pg_total_relation_size(%L) > 0 THEN
                           (pg_relation_size(%L)::NUMERIC / pg_total_relation_size(%L))
                       ELSE 0
                   END::NUMERIC(5,4)
            FROM %I',
            partition_record.tablename,
            partition_record.tablename,
            partition_record.tablename,
            partition_record.tablename,
            partition_record.tablename,
            partition_record.tablename
        );
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- === AUTOMATED MAINTENANCE SCHEDULING ===

-- 11. Daily maintenance procedure
CREATE OR REPLACE FUNCTION daily_partition_maintenance()
RETURNS VOID AS $$
BEGIN
    -- Ensure future partitions exist
    PERFORM ensure_partitions_exist();

    -- Update table statistics
    ANALYZE historical_market_data;
    ANALYZE market_data_summaries;

    -- Log maintenance activity
    INSERT INTO partition_maintenance_log (
        maintenance_date,
        operation,
        details,
        created_at
    ) VALUES (
        CURRENT_DATE,
        'DAILY_MAINTENANCE',
        'Partition check and statistics update completed',
        CURRENT_TIMESTAMP
    );

    RAISE NOTICE 'Daily partition maintenance completed';
END;
$$ LANGUAGE plpgsql;

-- 12. Create maintenance log table
CREATE TABLE IF NOT EXISTS partition_maintenance_log (
    id SERIAL PRIMARY KEY,
    maintenance_date DATE NOT NULL,
    operation VARCHAR(50) NOT NULL,
    details TEXT,
    duration_seconds INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- === PERFORMANCE OPTIMIZATION FOR PARTITIONS ===

-- 13. Parallel query configuration for partitions
-- Enable constraint exclusion for partition pruning
SET constraint_exclusion = partition;

-- Enable parallel workers for large partition scans
ALTER TABLE historical_market_data SET (parallel_workers = 4);
ALTER TABLE market_data_summaries SET (parallel_workers = 2);

-- 14. Partition-wise joins configuration
SET enable_partitionwise_join = on;
SET enable_partitionwise_aggregate = on;

-- === BACKUP AND RESTORE PROCEDURES ===

-- 15. Partition backup procedure
CREATE OR REPLACE FUNCTION backup_partition(
    partition_name TEXT,
    backup_path TEXT
) RETURNS BOOLEAN AS $$
BEGIN
    -- Create backup using pg_dump
    EXECUTE format(
        'COPY (SELECT * FROM %I) TO %L WITH (FORMAT CSV, HEADER)',
        partition_name, backup_path || '/' || partition_name || '.csv'
    );

    RAISE NOTICE 'Backup created for partition: %', partition_name;
    RETURN TRUE;
EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'Backup failed for partition %: %', partition_name, SQLERRM;
        RETURN FALSE;
END;
$$ LANGUAGE plpgsql;

-- === QUERY OPTIMIZATION EXAMPLES ===

/*
-- Example: Efficient date range query with partition pruning
EXPLAIN (ANALYZE, BUFFERS)
SELECT symbol_ticker, trade_date, close_price
FROM historical_market_data
WHERE trade_date BETWEEN '2024-01-01' AND '2024-01-31'
  AND symbol_ticker = 'AAPL'
ORDER BY trade_date DESC;

-- Example: Cross-partition aggregate query
SELECT
    DATE_TRUNC('month', trade_date) as month,
    COUNT(*) as record_count,
    AVG(close_price) as avg_close
FROM historical_market_data
WHERE trade_date >= '2023-01-01'
  AND symbol_ticker IN ('AAPL', 'GOOGL', 'MSFT')
GROUP BY DATE_TRUNC('month', trade_date)
ORDER BY month;

-- Example: Partition-wise join
SELECT h.symbol_ticker, h.trade_date, h.close_price, s.total_return_percent
FROM historical_market_data h
JOIN market_data_summaries s ON h.symbol_id = s.symbol_id
WHERE h.trade_date BETWEEN '2024-01-01' AND '2024-01-31'
  AND s.period_start <= '2024-01-01'
  AND s.period_end >= '2024-01-31';
*/

-- === MAINTENANCE SCHEDULE SETUP ===

/*
-- Set up cron job for daily maintenance (requires pg_cron extension)
-- Run daily at 2 AM
SELECT cron.schedule('partition-maintenance', '0 2 * * *', 'SELECT daily_partition_maintenance();');

-- Set up monthly cleanup (first day of month at 3 AM)
SELECT cron.schedule(
    'partition-cleanup',
    '0 3 1 * *',
    'SELECT drop_old_partitions(24);' -- Keep 24 months
);
*/

-- === MONITORING QUERIES ===

/*
-- Monitor partition sizes and growth
SELECT * FROM v_partition_info ORDER BY size_bytes DESC;

-- Check partition statistics
SELECT * FROM get_partition_stats() ORDER BY latest_date DESC;

-- Monitor maintenance log
SELECT * FROM partition_maintenance_log ORDER BY created_at DESC LIMIT 10;

-- Check partition pruning effectiveness
SELECT
    schemaname,
    tablename,
    seq_scan,
    seq_tup_read,
    idx_scan,
    idx_tup_fetch
FROM pg_stat_user_tables
WHERE tablename LIKE 'historical_market_data_%'
ORDER BY seq_scan DESC;
*/