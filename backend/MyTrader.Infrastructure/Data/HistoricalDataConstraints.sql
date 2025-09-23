-- =============================================================================
-- HISTORICAL MARKET DATA INTEGRITY CONSTRAINTS AND VALIDATION RULES
-- Ensures data quality, consistency, and business rule compliance
-- =============================================================================

-- === PRIMARY KEY AND UNIQUENESS CONSTRAINTS ===

-- 1. Prevent duplicate records for same symbol/date/timeframe/source
ALTER TABLE historical_market_data
ADD CONSTRAINT uq_historical_market_data_unique_record
UNIQUE (symbol_ticker, trade_date, timeframe, data_source);

-- 2. Ensure summary periods don't overlap for same symbol/type
ALTER TABLE market_data_summaries
ADD CONSTRAINT uq_market_data_summaries_period
UNIQUE (symbol_id, period_type, period_start, period_end);

-- === CHECK CONSTRAINTS FOR DATA VALIDATION ===

-- 3. Price validation - prevent negative prices and extreme values
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_prices_positive
CHECK (
    (open_price IS NULL OR open_price >= 0) AND
    (high_price IS NULL OR high_price >= 0) AND
    (low_price IS NULL OR low_price >= 0) AND
    (close_price IS NULL OR close_price >= 0) AND
    (adjusted_close_price IS NULL OR adjusted_close_price >= 0) AND
    (previous_close IS NULL OR previous_close >= 0)
);

-- 4. OHLC price relationship validation
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_ohlc_relationship
CHECK (
    -- If all OHLC values exist, validate relationships
    (open_price IS NULL OR high_price IS NULL OR low_price IS NULL OR close_price IS NULL) OR
    (high_price >= GREATEST(open_price, close_price) AND
     low_price <= LEAST(open_price, close_price) AND
     high_price >= low_price)
);

-- 5. Volume validation - prevent negative volume
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_volume_positive
CHECK (
    (volume IS NULL OR volume >= 0) AND
    (trading_value IS NULL OR trading_value >= 0) AND
    (transaction_count IS NULL OR transaction_count >= 0)
);

-- 6. Market cap validation
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_market_cap
CHECK (
    (market_cap IS NULL OR market_cap >= 0) AND
    (free_float_market_cap IS NULL OR free_float_market_cap >= 0) AND
    (shares_outstanding IS NULL OR shares_outstanding > 0) AND
    (free_float_shares IS NULL OR free_float_shares >= 0)
);

-- 7. Technical indicators validation
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_technical_indicators
CHECK (
    (rsi IS NULL OR (rsi >= 0 AND rsi <= 100)) AND
    (data_quality_score IS NULL OR (data_quality_score >= 0 AND data_quality_score <= 100))
);

-- 8. Date and time validation
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_dates
CHECK (
    trade_date <= CURRENT_DATE AND
    (timestamp IS NULL OR timestamp >= '1900-01-01'::timestamp) AND
    created_at <= CURRENT_TIMESTAMP AND
    updated_at <= CURRENT_TIMESTAMP AND
    data_collected_at <= CURRENT_TIMESTAMP
);

-- 9. Timeframe validation
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_timeframe
CHECK (
    timeframe IN ('1m', '5m', '15m', '30m', '1h', '4h', '1d', 'DAILY', 'WEEKLY', 'MONTHLY', 'YEARLY')
);

-- 10. Data source validation
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_source
CHECK (
    data_source IN ('BIST', 'BINANCE', 'YAHOO', 'ALPHA_VANTAGE', 'IEX', 'POLYGON', 'FINNHUB', 'MANUAL') AND
    source_priority BETWEEN 1 AND 100
);

-- 11. Currency validation
ALTER TABLE historical_market_data
ADD CONSTRAINT chk_historical_market_data_currency
CHECK (
    currency IN ('USD', 'EUR', 'TRY', 'GBP', 'JPY', 'AUD', 'CAD', 'CHF', 'BTC', 'ETH', 'USDT')
);

-- === SUMMARY TABLE CONSTRAINTS ===

-- 12. Summary period validation
ALTER TABLE market_data_summaries
ADD CONSTRAINT chk_market_data_summaries_period_valid
CHECK (
    period_end >= period_start AND
    trading_days >= 0 AND
    trading_days <= 366 AND
    period_type IN ('WEEKLY', 'MONTHLY', 'QUARTERLY', 'YEARLY')
);

-- 13. Summary statistics validation
ALTER TABLE market_data_summaries
ADD CONSTRAINT chk_market_data_summaries_stats
CHECK (
    (total_return_percent IS NULL OR total_return_percent BETWEEN -100 AND 10000) AND
    (volatility IS NULL OR volatility >= 0) AND
    (max_drawdown_percent IS NULL OR max_drawdown_percent BETWEEN -100 AND 0) AND
    (sharpe_ratio IS NULL OR sharpe_ratio BETWEEN -10 AND 10) AND
    (beta IS NULL OR beta BETWEEN -5 AND 5) AND
    quality_score BETWEEN 0 AND 100
);

-- 14. Summary price validation
ALTER TABLE market_data_summaries
ADD CONSTRAINT chk_market_data_summaries_prices
CHECK (
    (period_open IS NULL OR period_open >= 0) AND
    (period_close IS NULL OR period_close >= 0) AND
    (period_high IS NULL OR period_high >= 0) AND
    (period_low IS NULL OR period_low >= 0) AND
    (period_vwap IS NULL OR period_vwap >= 0) AND
    -- High >= Max(Open, Close) AND Low <= Min(Open, Close)
    (period_open IS NULL OR period_close IS NULL OR period_high IS NULL OR period_low IS NULL OR
     (period_high >= GREATEST(period_open, period_close) AND
      period_low <= LEAST(period_open, period_close)))
);

-- 15. Summary volume validation
ALTER TABLE market_data_summaries
ADD CONSTRAINT chk_market_data_summaries_volume
CHECK (
    (total_volume IS NULL OR total_volume >= 0) AND
    (avg_daily_volume IS NULL OR avg_daily_volume >= 0) AND
    (total_trading_value IS NULL OR total_trading_value >= 0) AND
    (avg_daily_trading_value IS NULL OR avg_daily_trading_value >= 0) AND
    (total_transactions IS NULL OR total_transactions >= 0) AND
    (avg_daily_transactions IS NULL OR avg_daily_transactions >= 0)
);

-- === FOREIGN KEY CONSTRAINTS ===

-- 16. Symbol reference integrity
ALTER TABLE historical_market_data
ADD CONSTRAINT fk_historical_market_data_symbol
FOREIGN KEY (symbol_id) REFERENCES symbols(id)
ON DELETE CASCADE;

ALTER TABLE market_data_summaries
ADD CONSTRAINT fk_market_data_summaries_symbol
FOREIGN KEY (symbol_id) REFERENCES symbols(id)
ON DELETE CASCADE;

-- === BUSINESS RULE VALIDATION FUNCTIONS ===

-- 17. Price consistency validation function
CREATE OR REPLACE FUNCTION validate_price_consistency()
RETURNS TRIGGER AS $$
BEGIN
    -- Validate adjusted close vs close price relationship
    IF NEW.adjusted_close_price IS NOT NULL AND NEW.close_price IS NOT NULL THEN
        -- Adjusted close should not be more than 50% different from close
        -- unless there's a significant corporate action
        IF ABS(NEW.adjusted_close_price - NEW.close_price) / NEW.close_price > 0.5 THEN
            -- Check if this is a known split/dividend date
            IF NOT (NEW.data_flags & 3) > 0 THEN -- Check split or dividend flags
                RAISE WARNING 'Large adjustment factor detected for % on %: close=%, adj_close=%',
                    NEW.symbol_ticker, NEW.trade_date, NEW.close_price, NEW.adjusted_close_price;
            END IF;
        END IF;
    END IF;

    -- Validate volume vs trading value consistency for BIST data
    IF NEW.data_source = 'BIST' AND NEW.volume IS NOT NULL AND NEW.trading_value IS NOT NULL AND NEW.close_price IS NOT NULL THEN
        -- Trading value should be approximately volume * average price
        -- Allow 20% tolerance for intraday variations
        DECLARE
            estimated_value DECIMAL;
            vwap_price DECIMAL;
        BEGIN
            vwap_price := COALESCE(NEW.vwap, (NEW.high_price + NEW.low_price + NEW.close_price) / 3);
            estimated_value := NEW.volume * vwap_price;

            IF ABS(NEW.trading_value - estimated_value) / estimated_value > 0.2 THEN
                RAISE WARNING 'Volume/Value inconsistency for % on %: volume=%, value=%, estimated=%',
                    NEW.symbol_ticker, NEW.trade_date, NEW.volume, NEW.trading_value, estimated_value;
            END IF;
        END;
    END IF;

    -- Update data quality score based on completeness
    IF NEW.data_quality_score IS NULL THEN
        NEW.data_quality_score := calculate_data_quality_score(NEW);
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 18. Data quality calculation function
CREATE OR REPLACE FUNCTION calculate_data_quality_score(data historical_market_data)
RETURNS INTEGER AS $$
DECLARE
    score INTEGER := 100;
BEGIN
    -- Deduct points for missing essential fields
    IF data.open_price IS NULL THEN score := score - 15; END IF;
    IF data.high_price IS NULL THEN score := score - 15; END IF;
    IF data.low_price IS NULL THEN score := score - 15; END IF;
    IF data.close_price IS NULL THEN score := score - 20; END IF; -- Close is most important
    IF data.volume IS NULL THEN score := score - 10; END IF;

    -- Deduct points for data inconsistencies
    IF data.open_price IS NOT NULL AND data.high_price IS NOT NULL AND data.open_price > data.high_price THEN
        score := score - 20;
    END IF;
    IF data.low_price IS NOT NULL AND data.close_price IS NOT NULL AND data.low_price > data.close_price THEN
        score := score - 20;
    END IF;

    -- Bonus points for additional data (BIST specific)
    IF data.trading_value IS NOT NULL THEN score := score + 5; END IF;
    IF data.transaction_count IS NOT NULL THEN score := score + 5; END IF;
    IF data.market_cap IS NOT NULL THEN score := score + 5; END IF;

    -- Ensure score stays within bounds
    RETURN GREATEST(0, LEAST(100, score));
END;
$$ LANGUAGE plpgsql;

-- 19. Create triggers for validation
CREATE TRIGGER trg_historical_market_data_validate
    BEFORE INSERT OR UPDATE ON historical_market_data
    FOR EACH ROW
    EXECUTE FUNCTION validate_price_consistency();

-- 20. Update timestamps trigger
CREATE OR REPLACE FUNCTION update_historical_data_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at := CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_historical_market_data_timestamp
    BEFORE UPDATE ON historical_market_data
    FOR EACH ROW
    EXECUTE FUNCTION update_historical_data_timestamp();

CREATE TRIGGER trg_market_data_summaries_timestamp
    BEFORE UPDATE ON market_data_summaries
    FOR EACH ROW
    EXECUTE FUNCTION update_historical_data_timestamp();

-- === DATA CLEANUP AND MAINTENANCE PROCEDURES ===

-- 21. Remove duplicate data procedure
CREATE OR REPLACE FUNCTION remove_duplicate_market_data()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    -- Remove duplicates keeping the highest quality record
    WITH duplicates AS (
        SELECT id,
               ROW_NUMBER() OVER (
                   PARTITION BY symbol_ticker, trade_date, timeframe, data_source
                   ORDER BY data_quality_score DESC NULLS LAST,
                           source_priority ASC,
                           created_at DESC
               ) as rn
        FROM historical_market_data
    )
    DELETE FROM historical_market_data
    WHERE id IN (
        SELECT id FROM duplicates WHERE rn > 1
    );

    GET DIAGNOSTICS deleted_count = ROW_COUNT;

    RAISE NOTICE 'Removed % duplicate records', deleted_count;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- 22. Data quality audit procedure
CREATE OR REPLACE FUNCTION audit_data_quality()
RETURNS TABLE (
    symbol_ticker TEXT,
    trade_date DATE,
    issues TEXT[],
    quality_score INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        h.symbol_ticker,
        h.trade_date,
        ARRAY_AGG(
            CASE
                WHEN h.open_price IS NULL THEN 'Missing open price'
                WHEN h.high_price IS NULL THEN 'Missing high price'
                WHEN h.low_price IS NULL THEN 'Missing low price'
                WHEN h.close_price IS NULL THEN 'Missing close price'
                WHEN h.volume IS NULL THEN 'Missing volume'
                WHEN h.high_price < h.low_price THEN 'Invalid OHLC relationship'
                WHEN h.volume = 0 AND h.trading_value > 0 THEN 'Volume/Value mismatch'
                ELSE NULL
            END
        ) FILTER (WHERE CASE
            WHEN h.open_price IS NULL THEN 'Missing open price'
            WHEN h.high_price IS NULL THEN 'Missing high price'
            WHEN h.low_price IS NULL THEN 'Missing low price'
            WHEN h.close_price IS NULL THEN 'Missing close price'
            WHEN h.volume IS NULL THEN 'Missing volume'
            WHEN h.high_price < h.low_price THEN 'Invalid OHLC relationship'
            WHEN h.volume = 0 AND h.trading_value > 0 THEN 'Volume/Value mismatch'
            ELSE NULL
        END IS NOT NULL) as issues,
        h.data_quality_score
    FROM historical_market_data h
    WHERE h.data_quality_score < 90
       OR h.open_price IS NULL
       OR h.high_price IS NULL
       OR h.low_price IS NULL
       OR h.close_price IS NULL
       OR (h.high_price IS NOT NULL AND h.low_price IS NOT NULL AND h.high_price < h.low_price)
    GROUP BY h.symbol_ticker, h.trade_date, h.data_quality_score
    ORDER BY h.trade_date DESC, h.data_quality_score ASC;
END;
$$ LANGUAGE plpgsql;

-- === PERFORMANCE MONITORING ===

-- 23. Table statistics view
CREATE OR REPLACE VIEW v_historical_data_stats AS
SELECT
    'historical_market_data'::text as table_name,
    COUNT(*) as total_records,
    COUNT(DISTINCT symbol_ticker) as unique_symbols,
    COUNT(DISTINCT data_source) as data_sources,
    MIN(trade_date) as earliest_date,
    MAX(trade_date) as latest_date,
    AVG(data_quality_score) as avg_quality_score,
    COUNT(*) FILTER (WHERE data_quality_score < 80) as low_quality_records,
    pg_size_pretty(pg_total_relation_size('historical_market_data')) as table_size
FROM historical_market_data

UNION ALL

SELECT
    'market_data_summaries'::text,
    COUNT(*),
    COUNT(DISTINCT symbol_ticker),
    COUNT(DISTINCT period_type),
    MIN(period_start)::date,
    MAX(period_end)::date,
    AVG(quality_score),
    COUNT(*) FILTER (WHERE quality_score < 80),
    pg_size_pretty(pg_total_relation_size('market_data_summaries'))
FROM market_data_summaries;

-- === USAGE EXAMPLES ===

/*
-- Check data quality issues:
SELECT * FROM audit_data_quality() LIMIT 20;

-- Remove duplicates:
SELECT remove_duplicate_market_data();

-- Monitor table statistics:
SELECT * FROM v_historical_data_stats;

-- Find symbols with missing recent data:
SELECT symbol_ticker, MAX(trade_date) as last_date
FROM historical_market_data
WHERE timeframe = 'DAILY'
GROUP BY symbol_ticker
HAVING MAX(trade_date) < CURRENT_DATE - INTERVAL '7 days'
ORDER BY last_date ASC;
*/