-- =============================================================================
-- HISTORICAL MARKET DATA INDEXING STRATEGY
-- Optimized for time-series queries, symbol-based filtering, and analytics
-- =============================================================================

-- === PRIMARY INDEXES FOR PARTITIONING AND PERFORMANCE ===

-- 1. Primary composite index for time-series queries (MOST IMPORTANT)
-- Supports: date range queries, symbol filtering, timeframe filtering
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_primary
ON historical_market_data (symbol_ticker, timeframe, trade_date DESC);

-- 2. Symbol-based queries with date ordering
-- Supports: single symbol analysis, backtesting queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_symbol_date
ON historical_market_data (symbol_id, trade_date DESC, timeframe);

-- 3. Date partitioning support index
-- Supports: time-based maintenance, data archival
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_date_source
ON historical_market_data (trade_date DESC, data_source, source_priority);

-- === PERFORMANCE OPTIMIZATION INDEXES ===

-- 4. Market-wide analysis index
-- Supports: cross-symbol analysis, market correlation studies
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_market_date
ON historical_market_data (market_code, data_source, trade_date DESC)
WHERE market_code IS NOT NULL;

-- 5. Data quality and deduplication index
-- Supports: data cleanup, source priority queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_dedup
ON historical_market_data (symbol_ticker, trade_date, timeframe, data_source, source_priority);

-- 6. Volume-based queries index
-- Supports: volume analysis, liquidity filtering
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_volume
ON historical_market_data (trade_date DESC, volume DESC)
WHERE volume IS NOT NULL;

-- 7. Price movement analysis index
-- Supports: volatility analysis, price change queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_price_change
ON historical_market_data (trade_date DESC, price_change_percent DESC)
WHERE price_change_percent IS NOT NULL;

-- === BIST SPECIFIC INDEXES ===

-- 8. BIST code mapping index
-- Supports: BIST legacy data integration
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_bist
ON historical_market_data (bist_code, trade_date DESC)
WHERE bist_code IS NOT NULL;

-- 9. BIST detailed data index
-- Supports: Turkish market specific analytics
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_bist_detailed
ON historical_market_data (trade_date DESC, trading_value DESC, transaction_count DESC)
WHERE data_source = 'BIST';

-- === TECHNICAL ANALYSIS INDEXES ===

-- 10. Technical indicators index
-- Supports: screener queries, technical analysis
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_technical
ON historical_market_data (trade_date DESC, rsi, macd)
WHERE rsi IS NOT NULL OR macd IS NOT NULL;

-- 11. Moving averages index
-- Supports: trend analysis, moving average strategies
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_sma
ON historical_market_data (trade_date DESC, sma_20, sma_50, sma_200)
WHERE sma_20 IS NOT NULL;

-- === INTRADAY DATA INDEXES ===

-- 12. Intraday timestamp index
-- Supports: minute/hour based analysis
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_intraday
ON historical_market_data (symbol_ticker, timestamp DESC)
WHERE timestamp IS NOT NULL;

-- 13. Intraday BRIN index for timestamp (PostgreSQL specific)
-- Highly efficient for time range queries on large datasets
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_timestamp_brin
ON historical_market_data USING BRIN (timestamp)
WHERE timestamp IS NOT NULL;

-- === PARTIAL INDEXES FOR SPECIFIC USE CASES ===

-- 14. Active symbols only index
-- Supports: current market data queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_active
ON historical_market_data (symbol_ticker, trade_date DESC)
WHERE data_quality_score >= 80;

-- 15. Latest data index
-- Supports: current price queries, real-time updates
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_latest
ON historical_market_data (symbol_ticker, updated_at DESC)
WHERE trade_date >= CURRENT_DATE - INTERVAL '30 days';

-- === GIN INDEXES FOR JSONB COLUMNS ===

-- 16. Extended data search index
-- Supports: flexible metadata queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_extended_gin
ON historical_market_data USING GIN (extended_data)
WHERE extended_data IS NOT NULL;

-- 17. Source metadata search index
-- Supports: data provenance queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_source_meta_gin
ON historical_market_data USING GIN (source_metadata)
WHERE source_metadata IS NOT NULL;

-- =============================================================================
-- MARKET DATA SUMMARIES INDEXES
-- =============================================================================

-- 1. Primary lookup index for summaries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_summaries_primary
ON market_data_summaries (symbol_ticker, period_type, period_start DESC);

-- 2. Performance ranking index
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_summaries_performance
ON market_data_summaries (period_type, period_start DESC, total_return_percent DESC NULLS LAST);

-- 3. Volume ranking index
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_summaries_volume
ON market_data_summaries (period_type, period_start DESC, avg_daily_volume DESC NULLS LAST);

-- 4. Volatility analysis index
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_summaries_volatility
ON market_data_summaries (period_type, period_start DESC, volatility DESC NULLS LAST);

-- 5. Quality filtering index
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_summaries_quality
ON market_data_summaries (period_type, quality_score DESC, period_start DESC)
WHERE quality_score >= 80;

-- =============================================================================
-- MAINTENANCE INDEXES
-- =============================================================================

-- 1. Data cleanup index
-- Supports: duplicate detection, data quality maintenance
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_cleanup
ON historical_market_data (data_collected_at, data_quality_score)
WHERE data_quality_score < 90;

-- 2. Archival index
-- Supports: old data archival processes
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_market_data_archival
ON historical_market_data (trade_date, created_at)
WHERE trade_date < CURRENT_DATE - INTERVAL '2 years';

-- =============================================================================
-- INDEX USAGE STATISTICS QUERY
-- =============================================================================

/*
-- Use this query to monitor index usage and performance:

SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE tablename IN ('historical_market_data', 'market_data_summaries')
ORDER BY idx_scan DESC;

-- Monitor table bloat and maintenance needs:
SELECT
    schemaname,
    tablename,
    n_tup_ins as inserts,
    n_tup_upd as updates,
    n_tup_del as deletes,
    n_dead_tup as dead_tuples,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables
WHERE tablename IN ('historical_market_data', 'market_data_summaries');
*/

-- =============================================================================
-- PERFORMANCE TUNING RECOMMENDATIONS
-- =============================================================================

/*
1. Partitioning Strategy:
   - Partition by trade_date (monthly or quarterly)
   - Consider list partitioning by data_source for mixed datasets

2. Maintenance Schedule:
   - VACUUM ANALYZE daily during off-hours
   - REINDEX monthly for heavily updated tables
   - Update statistics weekly

3. Query Optimization:
   - Always include symbol_ticker and trade_date in WHERE clauses
   - Use LIMIT for large result sets
   - Consider materialized views for complex aggregations

4. Hardware Recommendations:
   - SSD storage for index files
   - Sufficient RAM for index caching
   - Consider separate tablespace for indexes

5. PostgreSQL Configuration:
   - work_mem: 256MB+ for analytical queries
   - effective_cache_size: 75% of system RAM
   - random_page_cost: 1.1 for SSD storage
   - seq_page_cost: 1.0
*/