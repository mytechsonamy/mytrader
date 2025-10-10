-- =====================================================================
-- Data Quality Validation for Data-Driven Symbol Management
-- Version: 1.0
-- Date: 2025-01-08
-- Author: Data Architecture Manager
-- Description: Comprehensive validation queries to verify data integrity
--              and system health after migration
-- =====================================================================

-- =====================================================================
-- VALIDATION SUITE 1: SYMBOL DATA INTEGRITY
-- =====================================================================

\echo '========================================'
\echo 'VALIDATION SUITE 1: SYMBOL DATA INTEGRITY'
\echo '========================================'

-- Test 1.1: Verify default symbols count
SELECT
    'Default Symbols Count' AS test_name,
    COUNT(*) AS actual,
    9 AS expected,
    CASE WHEN COUNT(*) = 9 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols
WHERE is_default_symbol = TRUE AND is_active = TRUE;

-- Test 1.2: Verify all default symbols are active and tracked
SELECT
    'Default Symbols Active & Tracked' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols
WHERE is_default_symbol = TRUE
  AND (is_active = FALSE OR is_tracked = FALSE);

-- Test 1.3: Verify broadcast priority range
SELECT
    'Broadcast Priority Range' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols
WHERE broadcast_priority < 0 OR broadcast_priority > 100;

-- Test 1.4: Check for symbols without market assignment
SELECT
    'Symbols Without Market' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols
WHERE is_active = TRUE AND market_id IS NULL;

-- Test 1.5: Check for symbols without data provider
SELECT
    'Active Symbols Without Provider' AS test_name,
    COUNT(*) AS actual,
    '0 or low' AS expected,
    CASE
        WHEN COUNT(*) = 0 THEN 'PASS'
        WHEN COUNT(*) <= 5 THEN 'WARNING'
        ELSE 'FAIL'
    END AS status
FROM symbols
WHERE is_active = TRUE AND data_provider_id IS NULL;

-- =====================================================================
-- VALIDATION SUITE 2: REFERENTIAL INTEGRITY
-- =====================================================================

\echo '========================================'
\echo 'VALIDATION SUITE 2: REFERENTIAL INTEGRITY'
\echo '========================================'

-- Test 2.1: Orphaned symbols (invalid asset_class_id)
SELECT
    'Orphaned Symbols (Asset Class)' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols s
LEFT JOIN asset_classes ac ON s.asset_class_id = ac.id
WHERE s.asset_class_id IS NOT NULL AND ac.id IS NULL;

-- Test 2.2: Orphaned symbols (invalid market_id)
SELECT
    'Orphaned Symbols (Market)' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols s
LEFT JOIN markets m ON s.market_id = m.id
WHERE s.market_id IS NOT NULL AND m.id IS NULL;

-- Test 2.3: Orphaned symbols (invalid data_provider_id)
SELECT
    'Orphaned Symbols (Data Provider)' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols s
LEFT JOIN data_providers dp ON s.data_provider_id = dp.id
WHERE s.data_provider_id IS NOT NULL AND dp.id IS NULL;

-- Test 2.4: Orphaned user preferences
SELECT
    'Orphaned User Preferences (Symbol)' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM user_dashboard_preferences udp
LEFT JOIN symbols s ON udp.symbol_id = s.id
WHERE s.id IS NULL;

-- Test 2.5: Orphaned user preferences (User)
SELECT
    'Orphaned User Preferences (User)' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM user_dashboard_preferences udp
LEFT JOIN users u ON udp.user_id = u.id
WHERE u.id IS NULL;

-- =====================================================================
-- VALIDATION SUITE 3: INDEX EFFECTIVENESS
-- =====================================================================

\echo '========================================'
\echo 'VALIDATION SUITE 3: INDEX EFFECTIVENESS'
\echo '========================================'

-- Test 3.1: Verify critical indexes exist
SELECT
    'Critical Indexes Exist' AS test_name,
    COUNT(*) AS actual,
    6 AS expected,
    CASE WHEN COUNT(*) >= 6 THEN 'PASS' ELSE 'FAIL' END AS status
FROM pg_indexes
WHERE tablename = 'symbols'
  AND indexname IN (
    'idx_symbols_broadcast_active',
    'idx_symbols_defaults',
    'idx_symbols_market_provider',
    'idx_symbols_asset_class_active',
    'idx_symbols_no_provider'
  )
UNION ALL
SELECT
    'User Preferences Index Exists' AS test_name,
    COUNT(*) AS actual,
    1 AS expected,
    CASE WHEN COUNT(*) >= 1 THEN 'PASS' ELSE 'FAIL' END AS status
FROM pg_indexes
WHERE tablename = 'user_dashboard_preferences'
  AND indexname = 'idx_user_prefs_visible';

-- Test 3.2: Index usage statistics (requires pg_stat_statements extension)
-- Uncomment if pg_stat_statements is enabled
/*
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan AS index_scans,
    idx_tup_read AS tuples_read,
    idx_tup_fetch AS tuples_fetched
FROM pg_stat_user_indexes
WHERE tablename IN ('symbols', 'user_dashboard_preferences')
ORDER BY idx_scan DESC;
*/

-- =====================================================================
-- VALIDATION SUITE 4: BUSINESS LOGIC CONSTRAINTS
-- =====================================================================

\echo '========================================'
\echo 'VALIDATION SUITE 4: BUSINESS LOGIC'
\echo '========================================'

-- Test 4.1: Inactive default symbols (should not exist)
SELECT
    'Inactive Default Symbols' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols
WHERE is_default_symbol = TRUE AND is_active = FALSE;

-- Test 4.2: Duplicate symbols (same ticker + venue)
SELECT
    'Duplicate Symbols' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM (
    SELECT ticker, venue, COUNT(*) AS cnt
    FROM symbols
    GROUP BY ticker, venue
    HAVING COUNT(*) > 1
) duplicates;

-- Test 4.3: Deprecated symbols still active
SELECT
    'Deprecated Symbols Still Active' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols
WHERE ticker IN ('ADAUSDT', 'MATICUSDT', 'DOTUSDT', 'LINKUSDT', 'LTCUSDT')
  AND venue = 'BINANCE'
  AND is_active = TRUE;

-- Test 4.4: Verify user preferences unique constraint
SELECT
    'User Preference Duplicates' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM (
    SELECT user_id, symbol_id, COUNT(*) AS cnt
    FROM user_dashboard_preferences
    GROUP BY user_id, symbol_id
    HAVING COUNT(*) > 1
) duplicates;

-- =====================================================================
-- VALIDATION SUITE 5: PERFORMANCE QUERIES
-- =====================================================================

\echo '========================================'
\echo 'VALIDATION SUITE 5: QUERY PERFORMANCE'
\echo '========================================'

-- Test 5.1: Default symbols query (anonymous users)
EXPLAIN ANALYZE
SELECT id, ticker, display, current_price, price_change_24h
FROM symbols
WHERE is_default_symbol = TRUE
  AND is_active = TRUE
ORDER BY display_order;

-- Test 5.2: Broadcast list query (WebSocket service)
EXPLAIN ANALYZE
SELECT
    s.id,
    s.ticker,
    s.market_id,
    s.data_provider_id,
    s.broadcast_priority,
    dp.websocket_url,
    dp.connection_status
FROM symbols s
INNER JOIN markets m ON s.market_id = m.id
LEFT JOIN data_providers dp ON s.data_provider_id = dp.id
WHERE s.is_active = TRUE
  AND s.is_tracked = TRUE
  AND m.is_active = TRUE
ORDER BY s.broadcast_priority DESC, s.last_broadcast_at ASC NULLS FIRST
LIMIT 100;

-- Test 5.3: User preferences query
EXPLAIN ANALYZE
SELECT
    s.id,
    s.ticker,
    s.display,
    s.current_price,
    s.price_change_24h,
    udp.display_order,
    udp.is_pinned,
    udp.custom_alias
FROM symbols s
INNER JOIN user_dashboard_preferences udp ON s.id = udp.symbol_id
WHERE udp.user_id = (SELECT id FROM users LIMIT 1)  -- Sample user
  AND udp.is_visible = TRUE
  AND s.is_active = TRUE
ORDER BY udp.is_pinned DESC, udp.display_order;

-- =====================================================================
-- VALIDATION SUITE 6: DATA COMPLETENESS
-- =====================================================================

\echo '========================================'
\echo 'VALIDATION SUITE 6: DATA COMPLETENESS'
\echo '========================================'

-- Test 6.1: Default symbols have all required fields
SELECT
    'Default Symbols Missing Display Name' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols
WHERE is_default_symbol = TRUE
  AND (display IS NULL OR display = '');

-- Test 6.2: Default symbols have price precision
SELECT
    'Default Symbols Missing Price Precision' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM symbols
WHERE is_default_symbol = TRUE
  AND price_precision IS NULL;

-- Test 6.3: Markets have data providers
SELECT
    'Markets Without Data Providers' AS test_name,
    COUNT(*) AS actual,
    0 AS expected,
    CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS status
FROM markets m
WHERE m.is_active = TRUE
  AND NOT EXISTS (
    SELECT 1 FROM data_providers dp
    WHERE dp.market_id = m.id AND dp.is_active = TRUE
  );

-- =====================================================================
-- VALIDATION SUITE 7: SUMMARY REPORT
-- =====================================================================

\echo '========================================'
\echo 'VALIDATION SUMMARY REPORT'
\echo '========================================'

SELECT
    'Total Symbols' AS metric,
    COUNT(*)::TEXT AS value
FROM symbols
UNION ALL
SELECT
    'Active Symbols' AS metric,
    COUNT(*)::TEXT AS value
FROM symbols
WHERE is_active = TRUE
UNION ALL
SELECT
    'Default Symbols' AS metric,
    COUNT(*)::TEXT AS value
FROM symbols
WHERE is_default_symbol = TRUE AND is_active = TRUE
UNION ALL
SELECT
    'Tracked Symbols' AS metric,
    COUNT(*)::TEXT AS value
FROM symbols
WHERE is_tracked = TRUE
UNION ALL
SELECT
    'Symbols with Providers' AS metric,
    COUNT(*)::TEXT AS value
FROM symbols
WHERE data_provider_id IS NOT NULL
UNION ALL
SELECT
    'Active Markets' AS metric,
    COUNT(*)::TEXT AS value
FROM markets
WHERE is_active = TRUE
UNION ALL
SELECT
    'Active Data Providers' AS metric,
    COUNT(*)::TEXT AS value
FROM data_providers
WHERE is_active = TRUE
UNION ALL
SELECT
    'User Preferences' AS metric,
    COUNT(*)::TEXT AS value
FROM user_dashboard_preferences
UNION ALL
SELECT
    'Broadcast Priority Range' AS metric,
    CONCAT(MIN(broadcast_priority), ' - ', MAX(broadcast_priority))::TEXT AS value
FROM symbols
WHERE is_active = TRUE;

-- =====================================================================
-- DETAILED SYMBOL LISTING
-- =====================================================================

\echo '========================================'
\echo 'DEFAULT SYMBOLS DETAIL'
\echo '========================================'

SELECT
    ticker,
    display,
    COALESCE(m.code, 'NO_MARKET') AS market,
    COALESCE(dp.code, 'NO_PROVIDER') AS provider,
    broadcast_priority,
    is_active,
    is_tracked,
    is_default_symbol,
    display_order
FROM symbols s
LEFT JOIN markets m ON s.market_id = m.id
LEFT JOIN data_providers dp ON s.data_provider_id = dp.id
WHERE is_default_symbol = TRUE
ORDER BY display_order;

-- =====================================================================
-- DEPRECATED SYMBOLS CHECK
-- =====================================================================

\echo '========================================'
\echo 'DEPRECATED SYMBOLS STATUS'
\echo '========================================'

SELECT
    ticker,
    display,
    is_active,
    is_tracked,
    is_default_symbol,
    CASE
        WHEN is_active = FALSE AND is_tracked = FALSE THEN 'PROPERLY DEACTIVATED'
        WHEN is_active = TRUE THEN 'WARNING: STILL ACTIVE'
        ELSE 'NEEDS REVIEW'
    END AS status
FROM symbols
WHERE ticker IN ('ADAUSDT', 'MATICUSDT', 'DOTUSDT', 'LINKUSDT', 'LTCUSDT')
  AND venue = 'BINANCE';

-- =====================================================================
-- VALIDATION COMPLETE
-- =====================================================================

\echo '========================================'
\echo 'DATA QUALITY VALIDATION COMPLETE'
\echo '========================================'
\echo 'Review results above for any FAIL or WARNING statuses'
\echo 'All PASS statuses indicate healthy data state'
\echo '========================================'
