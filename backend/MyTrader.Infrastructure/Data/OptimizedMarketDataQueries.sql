-- =============================================================================
-- OPTIMIZED MARKET DATA QUERIES FOR MYTRADER APPLICATION
-- Performance-focused queries for frontend dashboard consumption
-- =============================================================================

-- === MATERIALIZED VIEWS FOR DASHBOARD PERFORMANCE ===

-- 1. Real-time Market Overview (Updated every 30 seconds)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_market_overview AS
SELECT
    ac.code as asset_class,
    COUNT(s.id) as total_symbols,
    AVG(hmd.price_change_percent) as avg_change_percent,
    SUM(hmd.volume) as total_volume,
    COUNT(CASE WHEN hmd.price_change_percent > 0 THEN 1 END) as gainers_count,
    COUNT(CASE WHEN hmd.price_change_percent < 0 THEN 1 END) as losers_count,
    MAX(hmd.trade_date) as last_update
FROM symbols s
JOIN asset_classes ac ON s.asset_class_id = ac.id
LEFT JOIN historical_market_data hmd ON s.id = hmd.symbol_id
    AND hmd.trade_date = CURRENT_DATE
    AND hmd.timeframe = 'DAILY'
WHERE s.is_active = true
    AND s.is_popular = true
GROUP BY ac.code;

-- Create unique index for fast refresh
CREATE UNIQUE INDEX IF NOT EXISTS mv_market_overview_pk
ON mv_market_overview (asset_class);

-- 2. Top Movers Cache (Updated every 5 minutes)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_top_movers AS
WITH ranked_symbols AS (
    SELECT
        s.id as symbol_id,
        s.ticker,
        s.display as display_name,
        s.asset_class,
        hmd.close_price as current_price,
        hmd.price_change_percent,
        hmd.volume,
        hmd.market_cap,
        ROW_NUMBER() OVER (
            PARTITION BY s.asset_class
            ORDER BY hmd.price_change_percent DESC NULLS LAST
        ) as gainer_rank,
        ROW_NUMBER() OVER (
            PARTITION BY s.asset_class
            ORDER BY hmd.price_change_percent ASC NULLS LAST
        ) as loser_rank,
        ROW_NUMBER() OVER (
            PARTITION BY s.asset_class
            ORDER BY hmd.volume DESC NULLS LAST
        ) as volume_rank
    FROM symbols s
    JOIN historical_market_data hmd ON s.id = hmd.symbol_id
    WHERE s.is_active = true
        AND hmd.trade_date = CURRENT_DATE
        AND hmd.timeframe = 'DAILY'
        AND hmd.data_quality_score >= 80
)
SELECT
    symbol_id,
    ticker,
    display_name,
    asset_class,
    current_price,
    price_change_percent,
    volume,
    market_cap,
    CASE
        WHEN gainer_rank <= 10 THEN 'GAINER'
        WHEN loser_rank <= 10 THEN 'LOSER'
        WHEN volume_rank <= 10 THEN 'VOLUME'
    END as mover_type,
    LEAST(gainer_rank, loser_rank, volume_rank) as rank_position
FROM ranked_symbols
WHERE gainer_rank <= 10 OR loser_rank <= 10 OR volume_rank <= 10
ORDER BY asset_class, mover_type, rank_position;

-- Create unique index for fast refresh
CREATE UNIQUE INDEX IF NOT EXISTS mv_top_movers_pk
ON mv_top_movers (symbol_id, mover_type);

-- 3. Popular Symbols for Frontend Dropdown (Updated hourly)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_popular_symbols AS
SELECT
    s.id,
    s.ticker,
    s.display,
    s.asset_class,
    s.venue,
    COALESCE(hmd.close_price, s.last_price) as current_price,
    COALESCE(hmd.price_change_percent, 0) as change_percent,
    s.volume_24h,
    s.market_cap,
    s.trading_view_symbol,
    -- Frontend-specific formatting
    CASE
        WHEN s.asset_class = 'CRYPTO' THEN REPLACE(s.ticker, '-USD', '')
        WHEN s.asset_class = 'CRYPTO' THEN REPLACE(s.ticker, 'USDT', '')
        ELSE s.ticker
    END as clean_ticker,
    CASE
        WHEN s.asset_class = 'CRYPTO' THEN 4
        WHEN s.asset_class = 'STOCK' THEN 2
        ELSE 2
    END as precision
FROM symbols s
LEFT JOIN historical_market_data hmd ON s.id = hmd.symbol_id
    AND hmd.trade_date = CURRENT_DATE
    AND hmd.timeframe = 'DAILY'
WHERE s.is_active = true
    AND s.is_popular = true
    AND s.volume_24h > 0
ORDER BY
    s.asset_class,
    s.volume_24h DESC NULLS LAST,
    s.market_cap DESC NULLS LAST
LIMIT 100;

-- Create unique index
CREATE UNIQUE INDEX IF NOT EXISTS mv_popular_symbols_pk
ON mv_popular_symbols (id);

-- === OPTIMIZED BATCH QUERY FUNCTIONS ===

-- 1. Fast batch market data retrieval
CREATE OR REPLACE FUNCTION get_batch_market_data(
    symbol_ids UUID[],
    include_extended_data BOOLEAN DEFAULT false
)
RETURNS TABLE (
    symbol_id UUID,
    ticker VARCHAR(50),
    asset_class VARCHAR(20),
    current_price DECIMAL(18,8),
    price_change DECIMAL(18,8),
    price_change_percent DECIMAL(10,4),
    volume DECIMAL(38,18),
    market_cap DECIMAL(38,18),
    last_update TIMESTAMP,
    extended_data JSONB
)
LANGUAGE SQL
STABLE
AS $$
    SELECT
        s.id,
        s.ticker,
        s.asset_class,
        COALESCE(hmd.close_price, s.last_price),
        COALESCE(hmd.price_change, 0),
        COALESCE(hmd.price_change_percent, 0),
        COALESCE(hmd.volume, s.volume_24h),
        COALESCE(hmd.market_cap, s.market_cap),
        COALESCE(hmd.updated_at, s.updated_at),
        CASE
            WHEN include_extended_data THEN hmd.extended_data
            ELSE NULL
        END
    FROM symbols s
    LEFT JOIN historical_market_data hmd ON s.id = hmd.symbol_id
        AND hmd.trade_date = CURRENT_DATE
        AND hmd.timeframe = 'DAILY'
    WHERE s.id = ANY(symbol_ids)
        AND s.is_active = true;
$$;

-- 2. Fast symbol search with ranking
CREATE OR REPLACE FUNCTION search_symbols(
    search_term TEXT,
    asset_class_filter VARCHAR(20) DEFAULT NULL,
    limit_count INTEGER DEFAULT 20
)
RETURNS TABLE (
    symbol_id UUID,
    ticker VARCHAR(50),
    display_name VARCHAR(200),
    asset_class VARCHAR(20),
    current_price DECIMAL(18,8),
    volume_24h DECIMAL(38,18),
    search_rank REAL
)
LANGUAGE SQL
STABLE
AS $$
    WITH symbol_matches AS (
        SELECT
            s.id,
            s.ticker,
            s.display,
            s.asset_class,
            COALESCE(hmd.close_price, s.last_price) as price,
            s.volume_24h,
            -- Text search ranking
            GREATEST(
                similarity(s.ticker, search_term) * 2.0,
                similarity(s.display, search_term) * 1.5,
                CASE WHEN s.ticker ILIKE search_term || '%' THEN 1.0 ELSE 0 END,
                CASE WHEN s.display ILIKE '%' || search_term || '%' THEN 0.5 ELSE 0 END
            ) as rank_score
        FROM symbols s
        LEFT JOIN historical_market_data hmd ON s.id = hmd.symbol_id
            AND hmd.trade_date = CURRENT_DATE
            AND hmd.timeframe = 'DAILY'
        WHERE s.is_active = true
            AND (asset_class_filter IS NULL OR s.asset_class = asset_class_filter)
            AND (
                s.ticker ILIKE '%' || search_term || '%'
                OR s.display ILIKE '%' || search_term || '%'
                OR similarity(s.ticker, search_term) > 0.3
                OR similarity(s.display, search_term) > 0.3
            )
    )
    SELECT
        id, ticker, display, asset_class,
        price, volume_24h, rank_score
    FROM symbol_matches
    WHERE rank_score > 0
    ORDER BY rank_score DESC, volume_24h DESC NULLS LAST
    LIMIT limit_count;
$$;

-- 3. Dashboard overview aggregation
CREATE OR REPLACE FUNCTION get_dashboard_overview()
RETURNS TABLE (
    total_symbols INTEGER,
    active_markets INTEGER,
    total_market_cap DECIMAL(38,18),
    total_volume_24h DECIMAL(38,18),
    top_gainer_ticker VARCHAR(50),
    top_gainer_change DECIMAL(10,4),
    top_loser_ticker VARCHAR(50),
    top_loser_change DECIMAL(10,4),
    market_status VARCHAR(20),
    last_update TIMESTAMP
)
LANGUAGE SQL
STABLE
AS $$
    WITH market_stats AS (
        SELECT
            COUNT(DISTINCT s.id) as symbol_count,
            COUNT(DISTINCT s.venue) as market_count,
            SUM(hmd.market_cap) as total_mcap,
            SUM(hmd.volume) as total_vol,
            MAX(hmd.updated_at) as last_updated
        FROM symbols s
        JOIN historical_market_data hmd ON s.id = hmd.symbol_id
        WHERE s.is_active = true
            AND hmd.trade_date = CURRENT_DATE
            AND hmd.timeframe = 'DAILY'
    ),
    top_mover AS (
        SELECT
            s.ticker as gainer_ticker,
            hmd.price_change_percent as gainer_change
        FROM symbols s
        JOIN historical_market_data hmd ON s.id = hmd.symbol_id
        WHERE s.is_active = true
            AND hmd.trade_date = CURRENT_DATE
            AND hmd.timeframe = 'DAILY'
            AND hmd.price_change_percent IS NOT NULL
        ORDER BY hmd.price_change_percent DESC
        LIMIT 1
    ),
    bottom_mover AS (
        SELECT
            s.ticker as loser_ticker,
            hmd.price_change_percent as loser_change
        FROM symbols s
        JOIN historical_market_data hmd ON s.id = hmd.symbol_id
        WHERE s.is_active = true
            AND hmd.trade_date = CURRENT_DATE
            AND hmd.timeframe = 'DAILY'
            AND hmd.price_change_percent IS NOT NULL
        ORDER BY hmd.price_change_percent ASC
        LIMIT 1
    )
    SELECT
        ms.symbol_count::INTEGER,
        ms.market_count::INTEGER,
        ms.total_mcap,
        ms.total_vol,
        tm.gainer_ticker,
        tm.gainer_change,
        bm.loser_ticker,
        bm.loser_change,
        CASE
            WHEN EXTRACT(HOUR FROM NOW() AT TIME ZONE 'UTC') BETWEEN 9 AND 16
            THEN 'OPEN'::VARCHAR(20)
            ELSE 'CLOSED'::VARCHAR(20)
        END,
        ms.last_updated
    FROM market_stats ms
    CROSS JOIN top_mover tm
    CROSS JOIN bottom_mover bm;
$$;

-- === REFRESH PROCEDURES FOR MATERIALIZED VIEWS ===

-- Auto-refresh materialized views
CREATE OR REPLACE FUNCTION refresh_market_data_views()
RETURNS VOID
LANGUAGE SQL
AS $$
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_market_overview;
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_top_movers;
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_popular_symbols;
$$;

-- === PERFORMANCE MONITORING QUERIES ===

-- Query performance analysis
CREATE OR REPLACE VIEW v_query_performance AS
SELECT
    query,
    calls,
    total_time,
    mean_time,
    min_time,
    max_time,
    stddev_time,
    rows,
    100.0 * shared_blks_hit / nullif(shared_blks_hit + shared_blks_read, 0) AS hit_percent
FROM pg_stat_statements
WHERE query LIKE '%historical_market_data%'
   OR query LIKE '%symbols%'
   OR query LIKE '%market_data%'
ORDER BY total_time DESC;

-- Index usage monitoring
CREATE OR REPLACE VIEW v_index_usage AS
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch,
    pg_size_pretty(pg_relation_size(indexrelid)) as size
FROM pg_stat_user_indexes
WHERE tablename IN ('historical_market_data', 'symbols', 'market_data_summaries')
    AND idx_scan > 0
ORDER BY idx_scan DESC;

-- === CLEANUP AND MAINTENANCE ===

-- Archive old data (run weekly)
CREATE OR REPLACE FUNCTION archive_old_market_data(days_to_keep INTEGER DEFAULT 730)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    archived_count INTEGER;
BEGIN
    -- Move data older than specified days to archive table
    WITH archived_data AS (
        DELETE FROM historical_market_data
        WHERE trade_date < CURRENT_DATE - INTERVAL '1 day' * days_to_keep
        RETURNING *
    )
    INSERT INTO historical_market_data_archive
    SELECT * FROM archived_data;

    GET DIAGNOSTICS archived_count = ROW_COUNT;

    -- Update statistics
    ANALYZE historical_market_data;

    RETURN archived_count;
END;
$$;

-- Performance maintenance (run nightly)
CREATE OR REPLACE FUNCTION maintain_market_data_performance()
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
    result_text TEXT := '';
BEGIN
    -- Refresh materialized views
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_market_overview;
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_top_movers;
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_popular_symbols;
    result_text := result_text || 'Materialized views refreshed. ';

    -- Update table statistics
    ANALYZE historical_market_data;
    ANALYZE symbols;
    ANALYZE market_data_summaries;
    result_text := result_text || 'Statistics updated. ';

    -- Cleanup old temporary data
    DELETE FROM historical_market_data
    WHERE data_quality_score < 50
      AND trade_date < CURRENT_DATE - INTERVAL '30 days';
    result_text := result_text || 'Low quality data cleaned. ';

    RETURN result_text;
END;
$$;