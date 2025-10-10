-- =============================================================================
-- BIST (Borsa Istanbul) OPTIMIZED DATABASE SCHEMA
-- Performance-optimized schema for Turkish stock market data delivery
-- Integrates with existing myTrader infrastructure
-- =============================================================================

-- === BIST MARKET CONFIGURATION ===

-- Ensure BIST market exists with proper configuration
INSERT INTO markets (id, code, name, name_tr, asset_class_id, country_code, timezone, primary_currency,
                    default_commission_rate, has_realtime_data, data_delay_minutes, status, display_order, created_at, updated_at)
SELECT
    '11111111-1111-1111-1111-111111111111'::uuid,
    'BIST',
    'Borsa Istanbul',
    'Borsa İstanbul',
    (SELECT id FROM asset_classes WHERE code = 'STOCK_BIST' LIMIT 1),
    'TR',
    'Europe/Istanbul',
    'TRY',
    0.002, -- 0.2% commission
    true,
    0, -- Real-time data
    'OPEN',
    1,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM markets WHERE code = 'BIST');

-- === BIST SPECIFIC INDEXES FOR ULTRA-FAST QUERIES ===

-- 1. BIST symbol lookup index (sub-10ms response time)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_symbols_bist_ticker_active
ON symbols (ticker, is_active, is_popular)
WHERE (asset_class = 'STOCK_BIST' OR asset_class = 'BIST')
    AND is_active = true;

-- 2. BIST market data current day index (dashboard queries)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_bist_current_day
ON historical_market_data (symbol_ticker, close_price, price_change_percent, volume, market_cap)
WHERE data_source = 'BIST'
    AND trade_date = CURRENT_DATE
    AND timeframe = 'DAILY';

-- 3. BIST volume and market cap ranking index
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_bist_ranking
ON historical_market_data (trade_date DESC, volume DESC, market_cap DESC)
WHERE data_source = 'BIST'
    AND volume > 0
    AND market_cap > 0;

-- 4. BIST price change performance index
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_historical_bist_performance
ON historical_market_data (trade_date DESC, price_change_percent DESC, volume DESC)
WHERE data_source = 'BIST'
    AND price_change_percent IS NOT NULL;

-- 5. BIST sector analysis index
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_symbols_bist_sector
ON symbols (sector, industry, market_cap DESC, volume_24h DESC)
WHERE (asset_class = 'STOCK_BIST' OR asset_class = 'BIST')
    AND is_active = true
    AND sector IS NOT NULL;

-- === BIST OPTIMIZED MATERIALIZED VIEWS ===

-- 1. BIST Real-time Dashboard View (Updated every 30 seconds)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_bist_dashboard AS
SELECT
    s.id as symbol_id,
    s.ticker,
    s.display as name,
    s.full_name,
    s.sector,
    s.industry,
    hmd.close_price as price,
    hmd.previous_close,
    hmd.price_change,
    hmd.price_change_percent as change_percent,
    hmd.volume,
    hmd.trading_value,
    hmd.high_price as high24h,
    hmd.low_price as low24h,
    hmd.market_cap,
    hmd.transaction_count,
    hmd.updated_at as last_updated,
    'BIST' as asset_class,
    'TRY' as currency,
    -- Performance ranking
    ROW_NUMBER() OVER (ORDER BY hmd.volume DESC NULLS LAST) as volume_rank,
    ROW_NUMBER() OVER (ORDER BY hmd.price_change_percent DESC NULLS LAST) as gainer_rank,
    ROW_NUMBER() OVER (ORDER BY hmd.price_change_percent ASC NULLS LAST) as loser_rank,
    ROW_NUMBER() OVER (ORDER BY hmd.market_cap DESC NULLS LAST) as market_cap_rank
FROM symbols s
JOIN historical_market_data hmd ON s.id = hmd.symbol_id
WHERE s.is_active = true
    AND (s.asset_class = 'STOCK_BIST' OR s.asset_class = 'BIST')
    AND hmd.trade_date = CURRENT_DATE
    AND hmd.timeframe = 'DAILY'
    AND hmd.data_source = 'BIST'
    AND hmd.data_quality_score >= 80
    AND hmd.close_price > 0;

-- Create unique index for concurrent refresh
CREATE UNIQUE INDEX IF NOT EXISTS mv_bist_dashboard_pk
ON mv_bist_dashboard (symbol_id);

-- 2. BIST Top Movers View (Updated every 5 minutes)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_bist_top_movers AS
WITH bist_movers AS (
    SELECT
        s.id as symbol_id,
        s.ticker,
        s.display as name,
        s.sector,
        hmd.close_price as price,
        hmd.price_change,
        hmd.price_change_percent,
        hmd.volume,
        hmd.market_cap,
        hmd.updated_at as last_updated,
        'TRY' as currency
    FROM symbols s
    JOIN historical_market_data hmd ON s.id = hmd.symbol_id
    WHERE s.is_active = true
        AND (s.asset_class = 'STOCK_BIST' OR s.asset_class = 'BIST')
        AND hmd.trade_date = CURRENT_DATE
        AND hmd.timeframe = 'DAILY'
        AND hmd.data_source = 'BIST'
        AND hmd.price_change_percent IS NOT NULL
        AND hmd.volume > 1000000 -- Minimum liquidity filter
)
SELECT
    symbol_id, ticker, name, sector, price, price_change, price_change_percent,
    volume, market_cap, last_updated, currency,
    'GAINER' as mover_type,
    ROW_NUMBER() OVER (ORDER BY price_change_percent DESC) as rank
FROM bist_movers
WHERE price_change_percent > 0
ORDER BY price_change_percent DESC
LIMIT 20

UNION ALL

SELECT
    symbol_id, ticker, name, sector, price, price_change, price_change_percent,
    volume, market_cap, last_updated, currency,
    'LOSER' as mover_type,
    ROW_NUMBER() OVER (ORDER BY price_change_percent ASC) as rank
FROM bist_movers
WHERE price_change_percent < 0
ORDER BY price_change_percent ASC
LIMIT 20

UNION ALL

SELECT
    symbol_id, ticker, name, sector, price, price_change, price_change_percent,
    volume, market_cap, last_updated, currency,
    'VOLUME' as mover_type,
    ROW_NUMBER() OVER (ORDER BY volume DESC) as rank
FROM bist_movers
ORDER BY volume DESC
LIMIT 20;

-- Create unique index for concurrent refresh
CREATE UNIQUE INDEX IF NOT EXISTS mv_bist_top_movers_pk
ON mv_bist_top_movers (symbol_id, mover_type);

-- 3. BIST Sector Performance View (Updated hourly)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_bist_sectors AS
SELECT
    s.sector,
    COUNT(s.id) as stock_count,
    AVG(hmd.price_change_percent) as avg_change_percent,
    SUM(hmd.volume) as total_volume,
    SUM(hmd.market_cap) as total_market_cap,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY hmd.price_change_percent) as median_change,
    COUNT(CASE WHEN hmd.price_change_percent > 0 THEN 1 END) as gainers,
    COUNT(CASE WHEN hmd.price_change_percent < 0 THEN 1 END) as losers,
    MAX(hmd.updated_at) as last_updated
FROM symbols s
JOIN historical_market_data hmd ON s.id = hmd.symbol_id
WHERE s.is_active = true
    AND (s.asset_class = 'STOCK_BIST' OR s.asset_class = 'BIST')
    AND s.sector IS NOT NULL
    AND hmd.trade_date = CURRENT_DATE
    AND hmd.timeframe = 'DAILY'
    AND hmd.data_source = 'BIST'
GROUP BY s.sector
HAVING COUNT(s.id) >= 3 -- At least 3 stocks in sector
ORDER BY avg_change_percent DESC;

-- Create unique index
CREATE UNIQUE INDEX IF NOT EXISTS mv_bist_sectors_pk
ON mv_bist_sectors (sector);

-- === BIST OPTIMIZED QUERY FUNCTIONS ===

-- 1. Get BIST Market Overview (< 100ms response time)
CREATE OR REPLACE FUNCTION get_bist_market_overview()
RETURNS TABLE (
    total_stocks INTEGER,
    total_volume DECIMAL(38,18),
    total_market_cap DECIMAL(38,18),
    avg_change_percent DECIMAL(10,4),
    gainers_count INTEGER,
    losers_count INTEGER,
    unchanged_count INTEGER,
    last_updated TIMESTAMP
)
LANGUAGE SQL
STABLE
AS $$
    SELECT
        COUNT(*)::INTEGER as total_stocks,
        COALESCE(SUM(volume), 0) as total_volume,
        COALESCE(SUM(market_cap), 0) as total_market_cap,
        COALESCE(AVG(change_percent), 0) as avg_change_percent,
        COUNT(CASE WHEN change_percent > 0 THEN 1 END)::INTEGER as gainers_count,
        COUNT(CASE WHEN change_percent < 0 THEN 1 END)::INTEGER as losers_count,
        COUNT(CASE WHEN change_percent = 0 THEN 1 END)::INTEGER as unchanged_count,
        MAX(last_updated) as last_updated
    FROM mv_bist_dashboard;
$$;

-- 2. Get BIST Stocks Data (batch retrieval for frontend)
CREATE OR REPLACE FUNCTION get_bist_stocks_data(
    symbols_filter TEXT[] DEFAULT NULL,
    limit_count INTEGER DEFAULT 50
)
RETURNS TABLE (
    symbol VARCHAR(50),
    name VARCHAR(200),
    price DECIMAL(18,8),
    change DECIMAL(18,8),
    change_percent DECIMAL(10,4),
    volume DECIMAL(38,18),
    high24h DECIMAL(18,8),
    low24h DECIMAL(18,8),
    last_updated TIMESTAMP,
    asset_class VARCHAR(20),
    currency VARCHAR(12)
)
LANGUAGE SQL
STABLE
AS $$
    SELECT
        ticker,
        name,
        price,
        price_change,
        change_percent,
        volume,
        high24h,
        low24h,
        last_updated,
        asset_class,
        currency
    FROM mv_bist_dashboard
    WHERE (symbols_filter IS NULL OR ticker = ANY(symbols_filter))
    ORDER BY volume_rank
    LIMIT limit_count;
$$;

-- 3. Get BIST Top Movers (optimized for dashboard)
CREATE OR REPLACE FUNCTION get_bist_top_movers()
RETURNS TABLE (
    gainers JSONB,
    losers JSONB,
    most_active JSONB
)
LANGUAGE SQL
STABLE
AS $$
    WITH movers_data AS (
        SELECT
            mover_type,
            jsonb_agg(
                jsonb_build_object(
                    'symbol', ticker,
                    'name', name,
                    'price', price,
                    'change', price_change,
                    'changePercent', price_change_percent,
                    'volume', volume,
                    'sector', sector
                ) ORDER BY rank
            ) as data
        FROM mv_bist_top_movers
        GROUP BY mover_type
    )
    SELECT
        (SELECT data FROM movers_data WHERE mover_type = 'GAINER'),
        (SELECT data FROM movers_data WHERE mover_type = 'LOSER'),
        (SELECT data FROM movers_data WHERE mover_type = 'VOLUME');
$$;

-- 4. Get Individual BIST Stock Data (< 10ms response time)
CREATE OR REPLACE FUNCTION get_bist_stock_data(stock_symbol TEXT)
RETURNS TABLE (
    symbol VARCHAR(50),
    name VARCHAR(200),
    full_name VARCHAR(200),
    sector VARCHAR(100),
    industry VARCHAR(100),
    price DECIMAL(18,8),
    previous_close DECIMAL(18,8),
    change DECIMAL(18,8),
    change_percent DECIMAL(10,4),
    volume DECIMAL(38,18),
    high24h DECIMAL(18,8),
    low24h DECIMAL(18,8),
    market_cap DECIMAL(38,18),
    trading_value DECIMAL(38,18),
    transaction_count BIGINT,
    last_updated TIMESTAMP,
    currency VARCHAR(12)
)
LANGUAGE SQL
STABLE
AS $$
    SELECT
        ticker,
        name,
        full_name,
        sector,
        industry,
        price,
        previous_close,
        price_change,
        change_percent,
        volume,
        high24h,
        low24h,
        market_cap,
        trading_value,
        transaction_count,
        last_updated,
        currency
    FROM mv_bist_dashboard
    WHERE ticker = stock_symbol;
$$;

-- 5. Get BIST Sector Performance
CREATE OR REPLACE FUNCTION get_bist_sector_performance()
RETURNS TABLE (
    sector VARCHAR(100),
    stock_count INTEGER,
    avg_change_percent DECIMAL(10,4),
    total_volume DECIMAL(38,18),
    total_market_cap DECIMAL(38,18),
    gainers INTEGER,
    losers INTEGER,
    performance_rank INTEGER
)
LANGUAGE SQL
STABLE
AS $$
    SELECT
        sector,
        stock_count,
        avg_change_percent,
        total_volume,
        total_market_cap,
        gainers,
        losers,
        ROW_NUMBER() OVER (ORDER BY avg_change_percent DESC)::INTEGER as performance_rank
    FROM mv_bist_sectors
    ORDER BY avg_change_percent DESC;
$$;

-- === BIST DATA REFRESH PROCEDURES ===

-- Auto-refresh BIST materialized views
CREATE OR REPLACE FUNCTION refresh_bist_views()
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
    start_time TIMESTAMP;
    result_text TEXT := '';
BEGIN
    start_time := clock_timestamp();

    -- Refresh BIST dashboard view (highest priority)
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_bist_dashboard;
    result_text := result_text || 'Dashboard refreshed. ';

    -- Refresh top movers view
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_bist_top_movers;
    result_text := result_text || 'Top movers refreshed. ';

    -- Refresh sector view
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_bist_sectors;
    result_text := result_text || 'Sectors refreshed. ';

    result_text := result_text || 'Total time: ' ||
                   EXTRACT(EPOCH FROM (clock_timestamp() - start_time))::TEXT || 's';

    RETURN result_text;
END;
$$;

-- Schedule automatic refresh (call this from your application every 30 seconds for dashboard)
CREATE OR REPLACE FUNCTION schedule_bist_refresh()
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    -- This would be called by your .NET background service
    PERFORM refresh_bist_views();
END;
$$;

-- === BIST PERFORMANCE MONITORING ===

-- Monitor BIST query performance
CREATE OR REPLACE VIEW v_bist_query_performance AS
SELECT
    query,
    calls,
    total_time,
    mean_time,
    min_time,
    max_time,
    100.0 * shared_blks_hit / nullif(shared_blks_hit + shared_blks_read, 0) AS hit_percent
FROM pg_stat_statements
WHERE query LIKE '%mv_bist_%'
   OR query LIKE '%get_bist_%'
   OR (query LIKE '%historical_market_data%' AND query LIKE '%BIST%')
ORDER BY total_time DESC;

-- Check BIST data freshness
CREATE OR REPLACE FUNCTION check_bist_data_freshness()
RETURNS TABLE (
    view_name TEXT,
    last_refresh TIMESTAMP,
    age_minutes INTEGER,
    needs_refresh BOOLEAN
)
LANGUAGE SQL
STABLE
AS $$
    SELECT
        'mv_bist_dashboard' as view_name,
        (SELECT MAX(last_updated) FROM mv_bist_dashboard),
        EXTRACT(EPOCH FROM (NOW() - (SELECT MAX(last_updated) FROM mv_bist_dashboard)))/60,
        EXTRACT(EPOCH FROM (NOW() - (SELECT MAX(last_updated) FROM mv_bist_dashboard)))/60 > 1

    UNION ALL

    SELECT
        'mv_bist_top_movers',
        (SELECT MAX(last_updated) FROM mv_bist_top_movers),
        EXTRACT(EPOCH FROM (NOW() - (SELECT MAX(last_updated) FROM mv_bist_top_movers)))/60,
        EXTRACT(EPOCH FROM (NOW() - (SELECT MAX(last_updated) FROM mv_bist_top_movers)))/60 > 5;
$$;

-- === SAMPLE BIST SYMBOLS DATA ===

-- Insert sample BIST symbols for testing (popular Turkish stocks)
INSERT INTO symbols (id, ticker, venue, asset_class, asset_class_id, market_id, quote_currency,
                    full_name, full_name_tr, display, sector, industry, country, is_active,
                    is_tracked, is_popular, price_precision, quantity_precision, created_at, updated_at)
SELECT
    gen_random_uuid(),
    symbol_data.ticker,
    'BIST',
    'STOCK_BIST',
    (SELECT id FROM asset_classes WHERE code = 'STOCK_BIST' LIMIT 1),
    (SELECT id FROM markets WHERE code = 'BIST' LIMIT 1),
    'TRY',
    symbol_data.full_name,
    symbol_data.full_name_tr,
    symbol_data.ticker,
    symbol_data.sector,
    symbol_data.industry,
    'TR',
    true,
    true,
    true,
    2, -- 2 decimal places for TRY
    0, -- Whole shares only
    NOW(),
    NOW()
FROM (VALUES
    ('THYAO', 'Turkish Airlines', 'Türk Hava Yolları', 'Transportation', 'Airlines'),
    ('AKBNK', 'Akbank', 'Akbank', 'Financial Services', 'Banking'),
    ('ISCTR', 'İş Bankası', 'Türkiye İş Bankası', 'Financial Services', 'Banking'),
    ('ASELS', 'ASELSAN', 'ASELSAN', 'Technology', 'Defense Electronics'),
    ('BIMAS', 'BİM', 'BİM Birleşik Mağazalar', 'Consumer Defensive', 'Discount Stores'),
    ('EREGL', 'Ereğli Demir Çelik', 'Ereğli Demir ve Çelik Fabrikaları', 'Basic Materials', 'Steel'),
    ('KRDMD', 'Kardemir', 'Kardemir Karabük Demir Çelik', 'Basic Materials', 'Steel'),
    ('SASA', 'SASA Polyester', 'SASA Polyester Sanayi', 'Basic Materials', 'Chemicals'),
    ('TOASO', 'Tofaş', 'Tofaş Türk Otomobil Fabrikası', 'Consumer Cyclical', 'Auto Manufacturers'),
    ('PETKM', 'Petkim', 'Petkim Petrokimya Holding', 'Basic Materials', 'Chemicals')
) AS symbol_data(ticker, full_name, full_name_tr, sector, industry)
WHERE NOT EXISTS (
    SELECT 1 FROM symbols WHERE ticker = symbol_data.ticker AND venue = 'BIST'
);

-- === PERFORMANCE BENCHMARKS ===

/*
Expected Performance Targets:
- get_bist_stocks_data(): < 50ms for 50 stocks
- get_bist_stock_data(): < 10ms for single stock
- get_bist_market_overview(): < 100ms
- get_bist_top_movers(): < 75ms
- Dashboard view refresh: < 5 seconds
- Top movers refresh: < 2 seconds

Usage Example:
SELECT * FROM get_bist_stocks_data(ARRAY['THYAO', 'AKBNK'], 10);
SELECT * FROM get_bist_stock_data('THYAO');
SELECT * FROM get_bist_market_overview();
SELECT * FROM get_bist_top_movers();
*/