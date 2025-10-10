-- =====================================================================
-- Migration: Add Trading Hours and Status Columns to markets Table
-- Version: 1.0
-- Date: 2025-10-10
-- Description: Add missing columns from Markets table to markets table
--              to support market hours tracking and data fetching control
-- =====================================================================

BEGIN;

-- =====================================================================
-- PHASE 1: Add Trading Hours Columns
-- =====================================================================

ALTER TABLE markets
ADD COLUMN IF NOT EXISTS regular_market_open TIME,
ADD COLUMN IF NOT EXISTS regular_market_close TIME,
ADD COLUMN IF NOT EXISTS pre_market_open TIME,
ADD COLUMN IF NOT EXISTS pre_market_close TIME,
ADD COLUMN IF NOT EXISTS post_market_open TIME,
ADD COLUMN IF NOT EXISTS post_market_close TIME,
ADD COLUMN IF NOT EXISTS trading_days INTEGER[] DEFAULT '{1,2,3,4,5}';

DO $$ BEGIN RAISE NOTICE 'Phase 1 Complete: Trading hours columns added'; END $$;

-- =====================================================================
-- PHASE 2: Add Market Status Tracking Columns
-- =====================================================================

ALTER TABLE markets
ADD COLUMN IF NOT EXISTS current_status VARCHAR(20) DEFAULT 'CLOSED',
ADD COLUMN IF NOT EXISTS next_open_time TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS next_close_time TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS status_last_updated TIMESTAMPTZ DEFAULT NOW(),
ADD COLUMN IF NOT EXISTS is_holiday_today BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS holiday_name VARCHAR(200);

DO $$ BEGIN RAISE NOTICE 'Phase 2 Complete: Market status columns added'; END $$;

-- =====================================================================
-- PHASE 3: Add Data Fetching Control Columns
-- =====================================================================

ALTER TABLE markets
ADD COLUMN IF NOT EXISTS enable_data_fetching BOOLEAN DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS data_fetch_interval INTEGER DEFAULT 5,
ADD COLUMN IF NOT EXISTS data_fetch_interval_closed INTEGER DEFAULT 300;

DO $$ BEGIN RAISE NOTICE 'Phase 3 Complete: Data fetching control columns added'; END $$;

-- =====================================================================
-- PHASE 4: Create Indexes for Performance
-- =====================================================================

CREATE INDEX IF NOT EXISTS idx_markets_current_status
ON markets(current_status) WHERE is_active = TRUE;

CREATE INDEX IF NOT EXISTS idx_markets_next_open_time
ON markets(next_open_time) WHERE is_active = TRUE;

CREATE INDEX IF NOT EXISTS idx_markets_enable_data_fetching
ON markets(enable_data_fetching, current_status) WHERE is_active = TRUE;

DO $$ BEGIN RAISE NOTICE 'Phase 4 Complete: Indexes created'; END $$;

-- =====================================================================
-- PHASE 5: Populate Trading Hours for Existing Markets
-- =====================================================================

-- BIST Trading Hours
UPDATE markets
SET
    regular_market_open = '10:00:00'::TIME,
    regular_market_close = '18:00:00'::TIME,
    pre_market_open = '09:40:00'::TIME,
    pre_market_close = '10:00:00'::TIME,
    post_market_open = NULL,
    post_market_close = NULL,
    trading_days = '{1,2,3,4,5}',
    current_status = 'CLOSED',
    enable_data_fetching = TRUE,
    data_fetch_interval = 60,
    data_fetch_interval_closed = 300,
    status_last_updated = NOW()
WHERE code = 'BIST';

-- NASDAQ Trading Hours
UPDATE markets
SET
    regular_market_open = '09:30:00'::TIME,
    regular_market_close = '16:00:00'::TIME,
    pre_market_open = '04:00:00'::TIME,
    pre_market_close = '09:30:00'::TIME,
    post_market_open = '16:00:00'::TIME,
    post_market_close = '20:00:00'::TIME,
    trading_days = '{1,2,3,4,5}',
    current_status = 'CLOSED',
    enable_data_fetching = TRUE,
    data_fetch_interval = 60,
    data_fetch_interval_closed = 300,
    status_last_updated = NOW()
WHERE code = 'NASDAQ';

-- NYSE Trading Hours
UPDATE markets
SET
    regular_market_open = '09:30:00'::TIME,
    regular_market_close = '16:00:00'::TIME,
    pre_market_open = '04:00:00'::TIME,
    pre_market_close = '09:30:00'::TIME,
    post_market_open = '16:00:00'::TIME,
    post_market_close = '20:00:00'::TIME,
    trading_days = '{1,2,3,4,5}',
    current_status = 'CLOSED',
    enable_data_fetching = TRUE,
    data_fetch_interval = 60,
    data_fetch_interval_closed = 300,
    status_last_updated = NOW()
WHERE code = 'NYSE';

-- Add CRYPTO market if it doesn't exist
INSERT INTO markets (
    "Id",
    code,
    name,
    name_tr,
    description,
    "AssetClassId",
    country_code,
    timezone,
    primary_currency,
    status,
    is_active,
    has_realtime_data,
    data_delay_minutes,
    display_order,
    regular_market_open,
    regular_market_close,
    trading_days,
    current_status,
    enable_data_fetching,
    data_fetch_interval,
    data_fetch_interval_closed,
    created_at,
    updated_at
)
SELECT
    gen_random_uuid(),
    'CRYPTO',
    'Cryptocurrency Markets',
    'Kripto Para Piyasaları',
    '24/7 cryptocurrency trading',
    "Id",
    'GLOBAL',
    'UTC',
    'USD',
    'OPEN',
    TRUE,
    TRUE,
    0,
    4,
    NULL,
    NULL,
    '{1,2,3,4,5,6,7}',
    'OPEN',
    TRUE,
    1,
    1,
    NOW(),
    NOW()
FROM asset_classes
WHERE code = 'CRYPTO'
ON CONFLICT (code) DO UPDATE
SET
    regular_market_open = NULL,
    regular_market_close = NULL,
    pre_market_open = NULL,
    pre_market_close = NULL,
    post_market_open = NULL,
    post_market_close = NULL,
    trading_days = '{1,2,3,4,5,6,7}',
    current_status = 'OPEN',
    enable_data_fetching = TRUE,
    data_fetch_interval = 1,
    data_fetch_interval_closed = 1,
    status_last_updated = NOW();

DO $$ BEGIN RAISE NOTICE 'Phase 5 Complete: Trading hours populated for all markets'; END $$;

-- =====================================================================
-- PHASE 5A: Create market_holidays Table
-- =====================================================================

CREATE TABLE IF NOT EXISTS market_holidays (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    market_id UUID NOT NULL REFERENCES markets("Id") ON DELETE CASCADE,
    holiday_date DATE NOT NULL,
    holiday_name VARCHAR(200) NOT NULL,
    is_recurring BOOLEAN DEFAULT FALSE,
    recurring_month INTEGER,
    recurring_day INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT market_holidays_unique UNIQUE (market_id, holiday_date)
);

CREATE INDEX IF NOT EXISTS idx_market_holidays_market_id_date
ON market_holidays(market_id, holiday_date);

DO $$ BEGIN RAISE NOTICE 'Phase 5A Complete: market_holidays table created'; END $$;

-- =====================================================================
-- PHASE 6: Create Function to Update Market Status
-- =====================================================================

DROP FUNCTION IF EXISTS update_market_status() CASCADE;

CREATE OR REPLACE FUNCTION update_market_status()
RETURNS TABLE (
    market_code VARCHAR(50),
    old_status VARCHAR(20),
    new_status VARCHAR(20),
    next_open TIMESTAMPTZ,
    next_close TIMESTAMPTZ
) AS $$
DECLARE
    v_market RECORD;
    v_current_time TIMESTAMPTZ;
    v_local_time TIME;
    v_local_date DATE;
    v_day_of_week INTEGER;
    v_new_status VARCHAR(20);
    v_next_open TIMESTAMPTZ;
    v_next_close TIMESTAMPTZ;
    v_is_holiday BOOLEAN;
    v_holiday_name VARCHAR(200);
BEGIN
    FOR v_market IN SELECT * FROM markets WHERE is_active = TRUE
    LOOP
        v_current_time := NOW();
        v_local_time := (v_current_time AT TIME ZONE v_market.timezone)::TIME;
        v_local_date := (v_current_time AT TIME ZONE v_market.timezone)::DATE;
        v_day_of_week := EXTRACT(ISODOW FROM v_local_date);

        -- Check for holidays (if market_holidays table exists and has data)
        SELECT TRUE, mh.holiday_name
        INTO v_is_holiday, v_holiday_name
        FROM market_holidays mh
        WHERE mh.market_id = v_market."Id"
          AND mh.holiday_date = v_local_date
        LIMIT 1;

        v_is_holiday := COALESCE(v_is_holiday, FALSE);

        -- Crypto markets are always open
        IF v_market.code = 'CRYPTO' THEN
            v_new_status := 'OPEN';
            v_next_open := NULL;
            v_next_close := NULL;
        -- Check if today is a holiday
        ELSIF v_is_holiday THEN
            v_new_status := 'HOLIDAY';
            v_next_open := NULL;
            v_next_close := NULL;
        -- Check if today is a trading day
        ELSIF NOT (v_day_of_week = ANY(v_market.trading_days)) THEN
            v_new_status := 'CLOSED';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSE
            -- Check market hours
            IF v_market.regular_market_open IS NOT NULL AND v_market.regular_market_close IS NOT NULL THEN
                IF v_local_time >= v_market.regular_market_open AND v_local_time < v_market.regular_market_close THEN
                    v_new_status := 'OPEN';
                    v_next_close := (v_local_date + v_market.regular_market_close) AT TIME ZONE v_market.timezone;
                    v_next_open := NULL;
                ELSIF v_market.pre_market_open IS NOT NULL AND
                      v_local_time >= v_market.pre_market_open AND v_local_time < v_market.regular_market_open THEN
                    v_new_status := 'PRE_MARKET';
                    v_next_open := (v_local_date + v_market.regular_market_open) AT TIME ZONE v_market.timezone;
                    v_next_close := NULL;
                ELSIF v_market.post_market_open IS NOT NULL AND
                      v_local_time >= v_market.post_market_open AND v_local_time < v_market.post_market_close THEN
                    v_new_status := 'POST_MARKET';
                    v_next_close := (v_local_date + v_market.post_market_close) AT TIME ZONE v_market.timezone;
                    v_next_open := NULL;
                ELSE
                    v_new_status := 'CLOSED';
                    v_next_open := ((v_local_date + INTERVAL '1 day') + v_market.regular_market_open) AT TIME ZONE v_market.timezone;
                    v_next_close := NULL;
                END IF;
            ELSE
                v_new_status := 'CLOSED';
                v_next_open := NULL;
                v_next_close := NULL;
            END IF;
        END IF;

        -- Update market status
        UPDATE markets
        SET
            current_status = v_new_status,
            next_open_time = v_next_open,
            next_close_time = v_next_close,
            is_holiday_today = v_is_holiday,
            holiday_name = v_holiday_name,
            status_last_updated = v_current_time,
            updated_at = v_current_time
        WHERE "Id" = v_market."Id";

        RETURN QUERY SELECT
            v_market.code,
            v_market.current_status::VARCHAR(20),
            v_new_status::VARCHAR(20),
            v_next_open,
            v_next_close;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

DO $$ BEGIN RAISE NOTICE 'Phase 6 Complete: update_market_status() function created'; END $$;

-- =====================================================================
-- PHASE 7: Create View for Market Status
-- =====================================================================

CREATE OR REPLACE VIEW vw_market_status AS
SELECT
    m.code AS market_code,
    m.name AS market_name,
    m.timezone,
    m.current_status,
    m.next_open_time,
    m.next_close_time,
    m.is_holiday_today,
    m.holiday_name,
    m.enable_data_fetching,
    CASE
        WHEN m.current_status = 'OPEN' THEN m.data_fetch_interval
        ELSE m.data_fetch_interval_closed
    END AS recommended_fetch_interval,
    m.status_last_updated,
    EXTRACT(EPOCH FROM (NOW() - m.status_last_updated)) AS seconds_since_update
FROM markets m
WHERE m.is_active = TRUE;

COMMENT ON VIEW vw_market_status IS 'Current market status with recommended data fetch intervals';

DO $$ BEGIN RAISE NOTICE 'Phase 7 Complete: vw_market_status view created'; END $$;

-- =====================================================================
-- PHASE 8: Initial Market Status Update
-- =====================================================================

SELECT * FROM update_market_status();

DO $$ BEGIN RAISE NOTICE 'Phase 8 Complete: Initial market status updated'; END $$;

-- =====================================================================
-- VERIFICATION
-- =====================================================================

DO $$
DECLARE
    markets_count INTEGER;
    columns_added BOOLEAN;
BEGIN
    SELECT COUNT(*) INTO markets_count
    FROM markets
    WHERE is_active = TRUE
      AND regular_market_open IS NOT NULL;

    SELECT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'markets'
          AND column_name = 'current_status'
    ) INTO columns_added;

    RAISE NOTICE '=== Migration Verification ===';
    RAISE NOTICE 'Markets with trading hours: %', markets_count;
    RAISE NOTICE 'Status columns added: %', columns_added;
    RAISE NOTICE '============================';

    IF NOT columns_added THEN
        RAISE EXCEPTION 'Migration failed: Status columns not added';
    END IF;
END $$;

COMMIT;

-- =====================================================================
-- MIGRATION COMPLETE
-- =====================================================================

SELECT
    'Migration 20251010_AddMarketTradingHoursAndStatus completed successfully' AS status,
    COUNT(*) AS total_markets,
    COUNT(*) FILTER (WHERE current_status = 'OPEN') AS open_markets,
    COUNT(*) FILTER (WHERE current_status = 'CLOSED') AS closed_markets
FROM markets
WHERE is_active = TRUE;
