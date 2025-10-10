-- =====================================================================
-- Migration: Consolidate Duplicate Markets Tables
-- Version: 1.0
-- Date: 2025-10-10
-- Author: Data Architecture Manager
-- Description: Merge "Markets" table functionality into existing 'markets' table
--              to resolve case-sensitivity naming conflict
-- =====================================================================

BEGIN;

-- =====================================================================
-- PHASE 1: Extend markets table schema
-- =====================================================================

ALTER TABLE markets
ADD COLUMN IF NOT EXISTS regular_market_open TIME,
ADD COLUMN IF NOT EXISTS regular_market_close TIME,
ADD COLUMN IF NOT EXISTS pre_market_open TIME,
ADD COLUMN IF NOT EXISTS pre_market_close TIME,
ADD COLUMN IF NOT EXISTS post_market_open TIME,
ADD COLUMN IF NOT EXISTS post_market_close TIME,
ADD COLUMN IF NOT EXISTS trading_days INTEGER[] DEFAULT '{1,2,3,4,5}',
ADD COLUMN IF NOT EXISTS current_status VARCHAR(20),
ADD COLUMN IF NOT EXISTS next_open_time TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS next_close_time TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS status_last_updated TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS is_holiday_today BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS holiday_name VARCHAR(200),
ADD COLUMN IF NOT EXISTS enable_data_fetching BOOLEAN DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS data_fetch_interval INTEGER DEFAULT 5,
ADD COLUMN IF NOT EXISTS data_fetch_interval_closed INTEGER DEFAULT 300;

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_markets_current_status ON markets(current_status);
CREATE INDEX IF NOT EXISTS idx_markets_next_open_time ON markets(next_open_time);

DO $$ BEGIN RAISE NOTICE 'Phase 1 Complete: Schema extended'; END $$;

-- =====================================================================
-- PHASE 2: Create market_holidays table (lowercase)
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

DO $$ BEGIN RAISE NOTICE 'Phase 2 Complete: market_holidays table created'; END $$;

-- =====================================================================
-- PHASE 3: Migrate data from "Markets" to markets
-- =====================================================================

-- Update existing markets (BIST, NASDAQ, NYSE)
UPDATE markets m
SET
    regular_market_open = MM."RegularMarketOpen",
    regular_market_close = MM."RegularMarketClose",
    pre_market_open = MM."PreMarketOpen",
    pre_market_close = MM."PreMarketClose",
    post_market_open = MM."PostMarketOpen",
    post_market_close = MM."PostMarketClose",
    trading_days = MM."TradingDays",
    current_status = MM."CurrentStatus",
    next_open_time = MM."NextOpenTime",
    next_close_time = MM."NextCloseTime",
    status_last_updated = MM."StatusLastUpdated",
    is_holiday_today = MM."IsHolidayToday",
    holiday_name = MM."HolidayName",
    enable_data_fetching = MM."EnableDataFetching",
    data_fetch_interval = MM."DataFetchInterval",
    data_fetch_interval_closed = MM."DataFetchIntervalClosed",
    updated_at = NOW()
FROM "Markets" MM
WHERE UPPER(m.code) = UPPER(MM."MarketCode")
  AND MM."MarketCode" IN ('BIST', 'NASDAQ', 'NYSE');

-- Insert CRYPTO market if not exists
INSERT INTO markets (
    "Id", code, name, "AssetClassId", country_code, timezone, primary_currency,
    regular_market_open, regular_market_close, trading_days,
    current_status, enable_data_fetching, data_fetch_interval,
    data_fetch_interval_closed, is_active, status, created_at, updated_at,
    has_realtime_data, data_delay_minutes, display_order
)
SELECT
    gen_random_uuid(),
    'CRYPTO',
    'Cryptocurrency Markets',
    (SELECT "Id" FROM asset_classes WHERE code = 'CRYPTO' LIMIT 1),
    'GLOBAL',
    'UTC',
    'USD',
    NULL,
    NULL,
    '{1,2,3,4,5,6,7}',
    'OPEN',
    TRUE,
    5,
    5,
    TRUE,
    'OPEN',
    NOW(),
    NOW(),
    TRUE,
    0,
    4
WHERE NOT EXISTS (SELECT 1 FROM markets WHERE code = 'CRYPTO');

DO $$ BEGIN RAISE NOTICE 'Phase 3 Complete: Data migrated from Markets to markets'; END $$;

-- =====================================================================
-- PHASE 4: Migrate holiday data
-- =====================================================================

INSERT INTO market_holidays (market_id, holiday_date, holiday_name, is_recurring, recurring_month, recurring_day, created_at)
SELECT
    m."Id",
    mh."HolidayDate",
    mh."HolidayName",
    mh."IsRecurring",
    mh."RecurringMonth",
    mh."RecurringDay",
    mh."CreatedAt"
FROM "MarketHolidays" mh
INNER JOIN "Markets" MM ON mh."MarketId" = MM."Id"
INNER JOIN markets m ON UPPER(m.code) = UPPER(MM."MarketCode")
ON CONFLICT (market_id, holiday_date) DO NOTHING;

DO $$ BEGIN RAISE NOTICE 'Phase 4 Complete: Holiday data migrated'; END $$;

-- =====================================================================
-- PHASE 5: Drop old function and create new one
-- =====================================================================

DROP FUNCTION IF EXISTS update_market_status();

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

        SELECT TRUE, mh.holiday_name
        INTO v_is_holiday, v_holiday_name
        FROM market_holidays mh
        WHERE mh.market_id = v_market."Id"
          AND mh.holiday_date = v_local_date
        LIMIT 1;

        v_is_holiday := COALESCE(v_is_holiday, FALSE);

        IF v_market.code = 'CRYPTO' THEN
            v_new_status := 'OPEN';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSIF v_is_holiday THEN
            v_new_status := 'HOLIDAY';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSIF NOT (v_day_of_week = ANY(v_market.trading_days)) THEN
            v_new_status := 'CLOSED';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSE
            IF v_market.regular_market_open IS NOT NULL AND v_market.regular_market_close IS NOT NULL THEN
                IF v_local_time >= v_market.regular_market_open AND v_local_time < v_market.regular_market_close THEN
                    v_new_status := 'OPEN';
                    v_next_close := (v_local_date || ' ' || v_market.regular_market_close)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_open := NULL;
                ELSIF v_market.pre_market_open IS NOT NULL AND
                      v_local_time >= v_market.pre_market_open AND v_local_time < v_market.regular_market_open THEN
                    v_new_status := 'PRE_MARKET';
                    v_next_open := (v_local_date || ' ' || v_market.regular_market_open)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_close := NULL;
                ELSIF v_market.post_market_open IS NOT NULL AND
                      v_local_time >= v_market.post_market_open AND v_local_time < v_market.post_market_close THEN
                    v_new_status := 'POST_MARKET';
                    v_next_close := (v_local_date || ' ' || v_market.post_market_close)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_open := NULL;
                ELSE
                    v_new_status := 'CLOSED';
                    v_next_open := (v_local_date + INTERVAL '1 day' || ' ' || v_market.regular_market_open)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_close := NULL;
                END IF;
            ELSE
                v_new_status := 'CLOSED';
                v_next_open := NULL;
                v_next_close := NULL;
            END IF;
        END IF;

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
            v_market.status::VARCHAR(20),
            v_new_status::VARCHAR(20),
            v_next_open,
            v_next_close;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

DO $$ BEGIN RAISE NOTICE 'Phase 5 Complete: update_market_status() function recreated'; END $$;

-- =====================================================================
-- PHASE 6: Recreate view
-- =====================================================================

DROP VIEW IF EXISTS "vw_MarketStatus";

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

DO $$ BEGIN RAISE NOTICE 'Phase 6 Complete: View recreated'; END $$;

-- =====================================================================
-- PHASE 7: Update initial market status
-- =====================================================================

SELECT * FROM update_market_status();

DO $$ BEGIN RAISE NOTICE 'Phase 7 Complete: Market status updated'; END $$;

-- =====================================================================
-- PHASE 8: Drop duplicate tables
-- =====================================================================

DROP TABLE IF EXISTS "MarketHolidays" CASCADE;
DROP TABLE IF EXISTS "Markets" CASCADE;

DO $$ BEGIN RAISE NOTICE 'Phase 8 Complete: Duplicate tables dropped'; END $$;

-- =====================================================================
-- VERIFICATION
-- =====================================================================

DO $$
DECLARE
    markets_count INTEGER;
    holidays_count INTEGER;
    crypto_exists BOOLEAN;
BEGIN
    SELECT COUNT(*) INTO markets_count FROM markets WHERE is_active = TRUE;
    SELECT COUNT(*) INTO holidays_count FROM market_holidays;
    SELECT EXISTS(SELECT 1 FROM markets WHERE code = 'CRYPTO') INTO crypto_exists;

    RAISE NOTICE '=== Migration Verification ===';
    RAISE NOTICE 'Active markets: %', markets_count;
    RAISE NOTICE 'Total holidays: %', holidays_count;
    RAISE NOTICE 'CRYPTO market exists: %', crypto_exists;
    RAISE NOTICE '=============================';

    IF markets_count < 3 THEN
        RAISE EXCEPTION 'Migration failed: Expected at least 3 active markets, found %', markets_count;
    END IF;
END $$;

COMMIT;

-- =====================================================================
-- MIGRATION COMPLETE
-- =====================================================================

SELECT
    'Migration 20251010_ConsolidateMarketsTables completed successfully' AS status,
    COUNT(*) AS total_markets,
    (SELECT COUNT(*) FROM market_holidays) AS total_holidays
FROM markets;
