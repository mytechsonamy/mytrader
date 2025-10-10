-- TEST QUERY: Simulate Frontend Dashboard Data Request
-- This query replicates what the dashboard will request from the API
-- Should return popular symbols with real market data

-- ==============================================
-- MAIN DASHBOARD DATA QUERY
-- ==============================================

-- This simulates the API call: GET /api/market-data/popular
WITH popular_symbols_with_data AS (
    SELECT
        s.id as symbol_id,
        s.ticker,
        s.display,
        s.asset_class,
        s.venue as market,
        s.quote_currency as currency,
        s.is_popular,
        s.is_tracked,
        -- Latest market data for each symbol
        (
            SELECT json_build_object(
                'price', md.close,
                'open', md.open,
                'high', md.high,
                'low', md.low,
                'volume', md.volume,
                'timestamp', md.timestamp,
                'change', COALESCE(md.close - md.open, 0),
                'changePercent', CASE
                    WHEN md.open > 0 THEN ROUND(((md.close - md.open) / md.open * 100)::numeric, 2)
                    ELSE 0
                END
            )
            FROM market_data md
            WHERE md.symbol = s.ticker
            ORDER BY md.timestamp DESC
            LIMIT 1
        ) as latest_data
    FROM symbols s
    WHERE s.is_active = true
        AND s.is_popular = true
)
SELECT
    ticker as symbol,
    display as display_name,
    asset_class,
    market,
    currency,
    latest_data->>'price' as current_price,
    latest_data->>'changePercent' as change_percent,
    latest_data->>'volume' as volume,
    latest_data->>'timestamp' as last_updated,
    CASE
        WHEN latest_data IS NULL THEN 'NO_DATA'
        WHEN (latest_data->>'timestamp')::timestamp < NOW() - INTERVAL '1 hour' THEN 'STALE'
        ELSE 'FRESH'
    END as data_status
FROM popular_symbols_with_data
ORDER BY
    asset_class,
    CASE WHEN latest_data IS NOT NULL THEN 0 ELSE 1 END,  -- Show symbols with data first
    (latest_data->>'timestamp')::timestamp DESC NULLS LAST
LIMIT 20;

-- ==============================================
-- ASSET CLASS OVERVIEW QUERY
-- ==============================================

-- This simulates the API call: GET /api/market-data/overview
SELECT
    'ASSET_CLASS_OVERVIEW' as query_type,
    s.asset_class,
    COUNT(s.id) as total_symbols,
    COUNT(CASE WHEN s.is_popular THEN 1 END) as popular_symbols,
    COUNT(CASE WHEN s.is_tracked THEN 1 END) as tracked_symbols,
    COUNT(md.symbol) as symbols_with_data,
    MAX(md.latest_update) as most_recent_data,
    ROUND(AVG(md.latest_price), 2) as avg_price
FROM symbols s
LEFT JOIN (
    SELECT
        symbol,
        MAX(timestamp) as latest_update,
        (array_agg(close ORDER BY timestamp DESC))[1] as latest_price
    FROM market_data
    GROUP BY symbol
) md ON s.ticker = md.symbol
WHERE s.is_active = true
GROUP BY s.asset_class
ORDER BY total_symbols DESC;

-- ==============================================
-- TOP MOVERS QUERY (For Dashboard Widget)
-- ==============================================

-- Simulate top gainers and losers
WITH symbol_performance AS (
    SELECT
        s.ticker,
        s.display,
        s.asset_class,
        md.close,
        md.open,
        md.volume,
        md.timestamp,
        CASE
            WHEN md.open > 0 THEN
                ROUND(((md.close - md.open) / md.open * 100)::numeric, 2)
            ELSE 0
        END as change_percent
    FROM symbols s
    INNER JOIN LATERAL (
        SELECT close, open, volume, timestamp
        FROM market_data md_inner
        WHERE md_inner.symbol = s.ticker
        ORDER BY md_inner.timestamp DESC
        LIMIT 1
    ) md ON true
    WHERE s.is_active = true
        AND s.is_popular = true
        AND md.close > 0
        AND md.open > 0
)
-- Top 5 Gainers
(
    SELECT
        'TOP_GAINERS' as category,
        ticker,
        display,
        asset_class,
        close as price,
        change_percent,
        volume,
        timestamp
    FROM symbol_performance
    WHERE change_percent > 0
    ORDER BY change_percent DESC
    LIMIT 5
)
UNION ALL
-- Top 5 Losers
(
    SELECT
        'TOP_LOSERS' as category,
        ticker,
        display,
        asset_class,
        close as price,
        change_percent,
        volume,
        timestamp
    FROM symbol_performance
    WHERE change_percent < 0
    ORDER BY change_percent ASC
    LIMIT 5
);

-- ==============================================
-- WEBSOCKET DATA FRESHNESS CHECK
-- ==============================================

-- Check which symbols should be receiving real-time updates
SELECT
    'WEBSOCKET_FRESHNESS' as check_type,
    s.ticker,
    s.asset_class,
    s.is_tracked,
    md.timestamp as last_update,
    EXTRACT(EPOCH FROM (NOW() - md.timestamp))/60 as minutes_since_update,
    CASE
        WHEN md.timestamp IS NULL THEN 'NO_DATA'
        WHEN md.timestamp >= NOW() - INTERVAL '5 minutes' THEN 'REALTIME'
        WHEN md.timestamp >= NOW() - INTERVAL '30 minutes' THEN 'RECENT'
        WHEN md.timestamp >= NOW() - INTERVAL '2 hours' THEN 'STALE'
        ELSE 'VERY_STALE'
    END as data_freshness
FROM symbols s
LEFT JOIN (
    SELECT symbol, MAX(timestamp) as timestamp
    FROM market_data
    GROUP BY symbol
) md ON s.ticker = md.symbol
WHERE s.is_active = true
    AND s.is_tracked = true
ORDER BY md.timestamp DESC NULLS LAST;

-- ==============================================
-- PERFORMANCE TEST QUERIES
-- ==============================================

-- Test query performance for dashboard endpoints
-- These should complete in <100ms

-- 1. Quick symbol lookup (used by autocomplete)
EXPLAIN ANALYZE
SELECT ticker, display, asset_class
FROM symbols
WHERE is_active = true
    AND (
        ticker ILIKE 'BTC%'
        OR display ILIKE 'Bitcoin%'
    )
LIMIT 10;

-- 2. Latest prices for multiple symbols (batch API call)
EXPLAIN ANALYZE
SELECT
    s.ticker,
    md.close,
    md.timestamp
FROM symbols s
INNER JOIN LATERAL (
    SELECT close, timestamp
    FROM market_data md_inner
    WHERE md_inner.symbol = s.ticker
    ORDER BY md_inner.timestamp DESC
    LIMIT 1
) md ON true
WHERE s.ticker IN ('AAPL', 'BTCUSD', 'AKBNK', 'MSFT', 'ETHUSD')
    AND s.is_active = true;

-- ==============================================
-- EXPECTED RESULTS AFTER FIX
-- ==============================================

-- After implementing PostgreSQL fix, these queries should return:

-- 1. popular_symbols_with_data: 15-20 rows with real prices
-- 2. ASSET_CLASS_OVERVIEW: 4+ asset classes with data
-- 3. TOP_GAINERS/TOP_LOSERS: 5 symbols each with calculated percentages
-- 4. WEBSOCKET_FRESHNESS: Mix of REALTIME/RECENT/STALE status
-- 5. Performance tests: Query times <50ms each

-- If any query returns empty results, the database initialization
-- needs to be run: POST /api/database/initialize