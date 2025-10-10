-- PHASE 2 DATA ARCHITECTURE VALIDATION SCRIPT
-- Run this after implementing the PostgreSQL fix to verify data integrity

-- ==============================================
-- 1. VERIFY REFERENCE DATA EXISTS
-- ==============================================

-- Check Asset Classes
SELECT 'ASSET_CLASSES' as table_name, code, name, is_active, created_at
FROM asset_classes
ORDER BY display_order;

-- Check Markets
SELECT 'MARKETS' as table_name, code, name, country_code, status, is_active
FROM markets
ORDER BY display_order;

-- Check Symbols by Asset Class
SELECT
    'SYMBOLS_BY_ASSET_CLASS' as check_name,
    asset_class,
    COUNT(*) as symbol_count,
    COUNT(CASE WHEN is_active THEN 1 END) as active_count,
    COUNT(CASE WHEN is_popular THEN 1 END) as popular_count
FROM symbols
GROUP BY asset_class
ORDER BY symbol_count DESC;

-- ==============================================
-- 2. VERIFY MARKET DATA POPULATION
-- ==============================================

-- Check Market Data Availability
SELECT
    'MARKET_DATA_OVERVIEW' as check_name,
    COUNT(*) as total_records,
    COUNT(DISTINCT symbol) as unique_symbols,
    MIN(timestamp) as earliest_data,
    MAX(timestamp) as latest_data,
    COUNT(CASE WHEN timestamp >= NOW() - INTERVAL '1 hour' THEN 1 END) as recent_records
FROM market_data;

-- Market Data by Symbol (Top 10 by record count)
SELECT
    'TOP_SYMBOLS_BY_DATA' as check_name,
    symbol,
    asset_class,
    COUNT(*) as record_count,
    MAX(timestamp) as latest_timestamp,
    MAX(close) as latest_price
FROM market_data
GROUP BY symbol, asset_class
ORDER BY record_count DESC
LIMIT 10;

-- Recent Market Data (last 2 hours)
SELECT
    'RECENT_MARKET_DATA' as check_name,
    symbol,
    asset_class,
    timestamp,
    close as price,
    volume
FROM market_data
WHERE timestamp >= NOW() - INTERVAL '2 hours'
ORDER BY timestamp DESC
LIMIT 20;

-- ==============================================
-- 3. DATA QUALITY CHECKS
-- ==============================================

-- Check for NULL values in critical fields
SELECT
    'DATA_QUALITY_NULL_CHECK' as check_name,
    'symbols' as table_name,
    COUNT(*) as total_records,
    COUNT(CASE WHEN ticker IS NULL THEN 1 END) as null_ticker,
    COUNT(CASE WHEN asset_class IS NULL THEN 1 END) as null_asset_class,
    COUNT(CASE WHEN venue IS NULL THEN 1 END) as null_venue
FROM symbols
UNION ALL
SELECT
    'DATA_QUALITY_NULL_CHECK' as check_name,
    'market_data' as table_name,
    COUNT(*) as total_records,
    COUNT(CASE WHEN symbol IS NULL THEN 1 END) as null_symbol,
    COUNT(CASE WHEN close IS NULL OR close = 0 THEN 1 END) as null_or_zero_close,
    COUNT(CASE WHEN timestamp IS NULL THEN 1 END) as null_timestamp
FROM market_data;

-- Check for duplicate market data records
SELECT
    'DUPLICATE_MARKET_DATA' as check_name,
    symbol,
    timeframe,
    timestamp,
    COUNT(*) as duplicate_count
FROM market_data
GROUP BY symbol, timeframe, timestamp
HAVING COUNT(*) > 1
ORDER BY duplicate_count DESC;

-- ==============================================
-- 4. PERFORMANCE VALIDATION QUERIES
-- ==============================================

-- Test main dashboard queries (should be <100ms)
EXPLAIN ANALYZE
SELECT
    s.ticker,
    s.asset_class,
    md.close as price,
    md.volume,
    md.timestamp as last_updated
FROM symbols s
LEFT JOIN LATERAL (
    SELECT close, volume, timestamp
    FROM market_data md_inner
    WHERE md_inner.symbol = s.ticker
    ORDER BY md_inner.timestamp DESC
    LIMIT 1
) md ON true
WHERE s.is_active = true
    AND s.is_popular = true
ORDER BY s.asset_class, md.timestamp DESC;

-- Test asset class grouping query
EXPLAIN ANALYZE
SELECT
    s.asset_class,
    COUNT(s.ticker) as symbol_count,
    COUNT(md.symbol) as symbols_with_data,
    MAX(md.timestamp) as latest_update
FROM symbols s
LEFT JOIN market_data md ON s.ticker = md.symbol
WHERE s.is_active = true
GROUP BY s.asset_class
ORDER BY symbol_count DESC;

-- ==============================================
-- 5. WEBSOCKET DATA VALIDATION
-- ==============================================

-- Check symbols that should have real-time updates
SELECT
    'REALTIME_SYMBOLS' as check_name,
    s.ticker,
    s.asset_class,
    s.is_tracked,
    md.latest_timestamp,
    CASE
        WHEN md.latest_timestamp >= NOW() - INTERVAL '10 minutes' THEN 'FRESH'
        WHEN md.latest_timestamp >= NOW() - INTERVAL '1 hour' THEN 'STALE'
        ELSE 'VERY_STALE'
    END as data_freshness
FROM symbols s
LEFT JOIN (
    SELECT symbol, MAX(timestamp) as latest_timestamp
    FROM market_data
    GROUP BY symbol
) md ON s.ticker = md.symbol
WHERE s.is_active = true
    AND s.is_tracked = true
ORDER BY data_freshness, s.asset_class;

-- ==============================================
-- 6. EXPECTED RESULTS SUMMARY
-- ==============================================

-- Overall health check - this should return healthy status
SELECT
    'OVERALL_HEALTH_CHECK' as check_name,
    CASE
        WHEN (
            (SELECT COUNT(*) FROM asset_classes WHERE is_active = true) >= 4
            AND (SELECT COUNT(*) FROM markets WHERE is_active = true) >= 4
            AND (SELECT COUNT(*) FROM symbols WHERE is_active = true) >= 20
            AND (SELECT COUNT(*) FROM market_data) >= 10
            AND (SELECT COUNT(*) FROM market_data WHERE timestamp >= NOW() - INTERVAL '2 hours') >= 5
        ) THEN 'HEALTHY'
        ELSE 'UNHEALTHY'
    END as status,
    (SELECT COUNT(*) FROM asset_classes WHERE is_active = true) as asset_classes,
    (SELECT COUNT(*) FROM markets WHERE is_active = true) as markets,
    (SELECT COUNT(*) FROM symbols WHERE is_active = true) as symbols,
    (SELECT COUNT(*) FROM market_data) as market_data_records,
    (SELECT COUNT(*) FROM market_data WHERE timestamp >= NOW() - INTERVAL '2 hours') as recent_records;

-- ==============================================
-- 7. TROUBLESHOOTING QUERIES
-- ==============================================

-- If no market data, check service status
-- Run these if the above queries show no data:

-- Check if symbols exist but no market data
SELECT
    'SYMBOLS_WITHOUT_DATA' as issue_type,
    s.ticker,
    s.asset_class,
    s.venue,
    s.is_active,
    s.is_tracked
FROM symbols s
LEFT JOIN market_data md ON s.ticker = md.symbol
WHERE s.is_active = true
    AND md.symbol IS NULL
ORDER BY s.asset_class, s.ticker;

-- Check if background services are populating data
-- Look for very recent inserts (last 10 minutes)
SELECT
    'RECENT_INSERTS' as check_name,
    symbol,
    timestamp,
    close,
    'Market data inserted in last 10 minutes' as note
FROM market_data
WHERE timestamp >= NOW() - INTERVAL '10 minutes'
ORDER BY timestamp DESC;

-- Final verification - Dashboard data query
-- This is what the frontend actually requests
SELECT
    'FRONTEND_DATA_SIMULATION' as check_name,
    s.ticker as symbol,
    s.display as display_name,
    s.asset_class,
    md.close as price,
    COALESCE(
        ROUND(((md.close - md.open) / NULLIF(md.open, 0) * 100)::numeric, 2),
        0
    ) as change_percent,
    md.volume,
    md.timestamp as last_updated
FROM symbols s
INNER JOIN LATERAL (
    SELECT open, close, volume, timestamp
    FROM market_data md_inner
    WHERE md_inner.symbol = s.ticker
    ORDER BY md_inner.timestamp DESC
    LIMIT 1
) md ON true
WHERE s.is_active = true
    AND s.is_popular = true
    AND md.close > 0
ORDER BY s.asset_class, md.timestamp DESC
LIMIT 20;