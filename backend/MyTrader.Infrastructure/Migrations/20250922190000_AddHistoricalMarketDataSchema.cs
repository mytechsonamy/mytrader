using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add comprehensive historical market data schema
    /// Supports both BIST detailed data and standard OHLCV formats
    /// Includes partitioning, indexing, and data quality features
    /// </summary>
    public partial class AddHistoricalMarketDataSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // === CREATE HISTORICAL MARKET DATA TABLE ===
            migrationBuilder.Sql(@"
                CREATE TABLE historical_market_data (
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
            ");

            // === CREATE MARKET DATA SUMMARIES TABLE ===
            migrationBuilder.Sql(@"
                CREATE TABLE market_data_summaries (
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
            ");

            // === CREATE FOREIGN KEY CONSTRAINTS ===
            migrationBuilder.Sql(@"
                ALTER TABLE historical_market_data
                ADD CONSTRAINT fk_historical_market_data_symbol
                FOREIGN KEY (symbol_id) REFERENCES symbols(id)
                ON DELETE CASCADE;

                ALTER TABLE market_data_summaries
                ADD CONSTRAINT fk_market_data_summaries_symbol
                FOREIGN KEY (symbol_id) REFERENCES symbols(id)
                ON DELETE CASCADE;
            ");

            // === CREATE INITIAL PARTITIONS FOR CURRENT AND NEXT 3 MONTHS ===
            migrationBuilder.Sql(@"
                -- Function to create monthly partitions
                CREATE OR REPLACE FUNCTION create_monthly_partition(
                    table_name TEXT,
                    start_date DATE
                ) RETURNS VOID AS $$
                DECLARE
                    partition_name TEXT;
                    end_date DATE;
                BEGIN
                    partition_name := table_name || '_' || TO_CHAR(start_date, 'YYYY_MM');
                    end_date := start_date + INTERVAL '1 month';

                    EXECUTE format(
                        'CREATE TABLE IF NOT EXISTS %I PARTITION OF %I
                         FOR VALUES FROM (%L) TO (%L)',
                        partition_name, table_name, start_date, end_date
                    );
                END;
                $$ LANGUAGE plpgsql;

                -- Function to create yearly partitions
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
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create initial partitions
            migrationBuilder.Sql(@"
                -- Create monthly partitions for historical data (current + 3 months)
                SELECT create_monthly_partition('historical_market_data', DATE_TRUNC('month', CURRENT_DATE + (i || ' months')::INTERVAL))
                FROM generate_series(0, 3) i;

                -- Create yearly partitions for summaries (current + 1 year)
                SELECT create_yearly_partition('market_data_summaries', DATE_TRUNC('year', CURRENT_DATE + (i || ' years')::INTERVAL))
                FROM generate_series(0, 1) i;
            ");

            // === CREATE PERFORMANCE INDEXES ===
            migrationBuilder.Sql(@"
                -- Primary time-series index for historical data
                CREATE INDEX CONCURRENTLY idx_historical_market_data_primary
                ON historical_market_data (symbol_ticker, timeframe, trade_date DESC);

                -- Symbol-based queries
                CREATE INDEX CONCURRENTLY idx_historical_market_data_symbol_date
                ON historical_market_data (symbol_id, trade_date DESC, timeframe);

                -- Date partitioning support
                CREATE INDEX CONCURRENTLY idx_historical_market_data_date_source
                ON historical_market_data (trade_date DESC, data_source, source_priority);

                -- Data deduplication index
                CREATE INDEX CONCURRENTLY idx_historical_market_data_dedup
                ON historical_market_data (symbol_ticker, trade_date, timeframe, data_source, source_priority);

                -- BIST specific index
                CREATE INDEX CONCURRENTLY idx_historical_market_data_bist
                ON historical_market_data (bist_code, trade_date DESC)
                WHERE bist_code IS NOT NULL;

                -- Volume analysis index
                CREATE INDEX CONCURRENTLY idx_historical_market_data_volume
                ON historical_market_data (trade_date DESC, volume DESC)
                WHERE volume IS NOT NULL;

                -- Technical indicators index
                CREATE INDEX CONCURRENTLY idx_historical_market_data_technical
                ON historical_market_data (trade_date DESC, rsi, macd)
                WHERE rsi IS NOT NULL OR macd IS NOT NULL;

                -- Intraday data index
                CREATE INDEX CONCURRENTLY idx_historical_market_data_intraday
                ON historical_market_data (symbol_ticker, timestamp DESC)
                WHERE timestamp IS NOT NULL;

                -- JSONB indexes for flexible queries
                CREATE INDEX CONCURRENTLY idx_historical_market_data_extended_gin
                ON historical_market_data USING GIN (extended_data)
                WHERE extended_data IS NOT NULL;

                CREATE INDEX CONCURRENTLY idx_historical_market_data_source_meta_gin
                ON historical_market_data USING GIN (source_metadata)
                WHERE source_metadata IS NOT NULL;
            ");

            // === CREATE SUMMARY TABLE INDEXES ===
            migrationBuilder.Sql(@"
                -- Primary lookup index for summaries
                CREATE INDEX CONCURRENTLY idx_market_data_summaries_primary
                ON market_data_summaries (symbol_ticker, period_type, period_start DESC);

                -- Performance ranking index
                CREATE INDEX CONCURRENTLY idx_market_data_summaries_performance
                ON market_data_summaries (period_type, period_start DESC, total_return_percent DESC NULLS LAST);

                -- Volume ranking index
                CREATE INDEX CONCURRENTLY idx_market_data_summaries_volume
                ON market_data_summaries (period_type, period_start DESC, avg_daily_volume DESC NULLS LAST);

                -- Quality filtering index
                CREATE INDEX CONCURRENTLY idx_market_data_summaries_quality
                ON market_data_summaries (period_type, quality_score DESC, period_start DESC)
                WHERE quality_score >= 80;
            ");

            // === CREATE DATA VALIDATION FUNCTIONS ===
            migrationBuilder.Sql(@"
                -- Data quality calculation function
                CREATE OR REPLACE FUNCTION calculate_data_quality_score(data historical_market_data)
                RETURNS INTEGER AS $$
                DECLARE
                    score INTEGER := 100;
                BEGIN
                    -- Deduct points for missing essential fields
                    IF data.open_price IS NULL THEN score := score - 15; END IF;
                    IF data.high_price IS NULL THEN score := score - 15; END IF;
                    IF data.low_price IS NULL THEN score := score - 15; END IF;
                    IF data.close_price IS NULL THEN score := score - 20; END IF;
                    IF data.volume IS NULL THEN score := score - 10; END IF;

                    -- Bonus points for additional data
                    IF data.trading_value IS NOT NULL THEN score := score + 5; END IF;
                    IF data.transaction_count IS NOT NULL THEN score := score + 5; END IF;
                    IF data.market_cap IS NOT NULL THEN score := score + 5; END IF;

                    RETURN GREATEST(0, LEAST(100, score));
                END;
                $$ LANGUAGE plpgsql;

                -- Validation trigger function
                CREATE OR REPLACE FUNCTION validate_historical_data()
                RETURNS TRIGGER AS $$
                BEGIN
                    -- Calculate data quality score if not provided
                    IF NEW.data_quality_score IS NULL THEN
                        NEW.data_quality_score := calculate_data_quality_score(NEW);
                    END IF;

                    -- Update timestamp
                    NEW.updated_at := CURRENT_TIMESTAMP;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                -- Create triggers
                CREATE TRIGGER trg_historical_market_data_validate
                    BEFORE INSERT OR UPDATE ON historical_market_data
                    FOR EACH ROW
                    EXECUTE FUNCTION validate_historical_data();
            ");

            // === CREATE MAINTENANCE PROCEDURES ===
            migrationBuilder.Sql(@"
                -- Automatic partition creation function
                CREATE OR REPLACE FUNCTION ensure_partitions_exist()
                RETURNS VOID AS $$
                DECLARE
                    current_month DATE;
                    future_month DATE;
                    current_year DATE;
                    future_year DATE;
                BEGIN
                    current_month := DATE_TRUNC('month', CURRENT_DATE);

                    FOR i IN 0..3 LOOP
                        future_month := current_month + (i || ' months')::INTERVAL;
                        PERFORM create_monthly_partition('historical_market_data', future_month);
                    END LOOP;

                    current_year := DATE_TRUNC('year', CURRENT_DATE);

                    FOR i IN 0..1 LOOP
                        future_year := current_year + (i || ' years')::INTERVAL;
                        PERFORM create_yearly_partition('market_data_summaries', future_year);
                    END LOOP;
                END;
                $$ LANGUAGE plpgsql;

                -- Create maintenance log table
                CREATE TABLE IF NOT EXISTS partition_maintenance_log (
                    id SERIAL PRIMARY KEY,
                    maintenance_date DATE NOT NULL,
                    operation VARCHAR(50) NOT NULL,
                    details TEXT,
                    duration_seconds INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // === CREATE MONITORING VIEWS ===
            migrationBuilder.Sql(@"
                -- Partition information view
                CREATE OR REPLACE VIEW v_partition_info AS
                SELECT
                    schemaname,
                    tablename as partition_name,
                    CASE
                        WHEN tablename LIKE '%_____' AND RIGHT(tablename, 4) ~ '^\\d{4}$' THEN 'YEARLY'
                        WHEN tablename LIKE '%______' AND RIGHT(tablename, 7) ~ '^\\d{4}_\\d{2}$' THEN 'MONTHLY'
                        ELSE 'UNKNOWN'
                    END as partition_type,
                    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size,
                    pg_total_relation_size(schemaname||'.'||tablename) as size_bytes
                FROM pg_tables
                WHERE tablename LIKE 'historical_market_data_%'
                   OR tablename LIKE 'market_data_summaries_%'
                ORDER BY size_bytes DESC;

                -- Data quality statistics view
                CREATE OR REPLACE VIEW v_historical_data_quality AS
                SELECT
                    data_source,
                    COUNT(*) as total_records,
                    AVG(data_quality_score) as avg_quality_score,
                    COUNT(*) FILTER (WHERE data_quality_score < 80) as low_quality_count,
                    COUNT(DISTINCT symbol_ticker) as unique_symbols,
                    MIN(trade_date) as earliest_date,
                    MAX(trade_date) as latest_date
                FROM historical_market_data
                GROUP BY data_source
                ORDER BY avg_quality_score DESC;
            ");

            // === SET TABLE PROPERTIES FOR OPTIMIZATION ===
            migrationBuilder.Sql(@"
                -- Set parallel workers for large scans
                ALTER TABLE historical_market_data SET (parallel_workers = 4);
                ALTER TABLE market_data_summaries SET (parallel_workers = 2);

                -- Set autovacuum parameters for high-volume tables
                ALTER TABLE historical_market_data SET (
                    autovacuum_vacuum_scale_factor = 0.1,
                    autovacuum_analyze_scale_factor = 0.05
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // === DROP VIEWS ===
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_historical_data_quality CASCADE;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_partition_info CASCADE;");

            // === DROP TRIGGERS AND FUNCTIONS ===
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_historical_market_data_validate ON historical_market_data;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS validate_historical_data() CASCADE;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS calculate_data_quality_score(historical_market_data) CASCADE;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS ensure_partitions_exist() CASCADE;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS create_monthly_partition(TEXT, DATE) CASCADE;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS create_yearly_partition(TEXT, DATE) CASCADE;");

            // === DROP MAINTENANCE LOG TABLE ===
            migrationBuilder.Sql("DROP TABLE IF EXISTS partition_maintenance_log CASCADE;");

            // === DROP INDEXES ===
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_primary;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_symbol_date;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_date_source;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_dedup;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_bist;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_volume;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_technical;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_intraday;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_extended_gin;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_historical_market_data_source_meta_gin;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_market_data_summaries_primary;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_market_data_summaries_performance;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_market_data_summaries_volume;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_market_data_summaries_quality;");

            // === DROP PARTITIONS FIRST ===
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    partition_name TEXT;
                BEGIN
                    -- Drop all historical data partitions
                    FOR partition_name IN
                        SELECT tablename
                        FROM pg_tables
                        WHERE tablename LIKE 'historical_market_data_%'
                    LOOP
                        EXECUTE 'DROP TABLE IF EXISTS ' || partition_name || ' CASCADE';
                    END LOOP;

                    -- Drop all summary partitions
                    FOR partition_name IN
                        SELECT tablename
                        FROM pg_tables
                        WHERE tablename LIKE 'market_data_summaries_%'
                    LOOP
                        EXECUTE 'DROP TABLE IF EXISTS ' || partition_name || ' CASCADE';
                    END LOOP;
                END $$;
            ");

            // === DROP MAIN TABLES ===
            migrationBuilder.Sql("DROP TABLE IF EXISTS historical_market_data CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS market_data_summaries CASCADE;");
        }
    }
}