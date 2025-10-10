-- =====================================================================
-- ROLLBACK: Consolidate Duplicate Markets Tables
-- Version: 1.0
-- Date: 2025-10-10
-- Description: Rollback consolidation migration and restore original dual-table structure
-- =====================================================================

BEGIN;

RAISE NOTICE 'Starting rollback of markets table consolidation...';

-- =====================================================================
-- PHASE 1: Drop new structures
-- =====================================================================

DROP VIEW IF EXISTS vw_market_status CASCADE;
DROP FUNCTION IF EXISTS update_market_status() CASCADE;

RAISE NOTICE 'Phase 1 Complete: New view and function dropped';

-- =====================================================================
-- PHASE 2: Backup market_holidays data before dropping
-- =====================================================================

CREATE TEMP TABLE IF NOT EXISTS temp_market_holidays_backup AS
SELECT * FROM market_holidays;

RAISE NOTICE 'Phase 2 Complete: Holiday data backed up to temp table';

-- =====================================================================
-- PHASE 3: Drop market_holidays table
-- =====================================================================

DROP TABLE IF EXISTS market_holidays CASCADE;

RAISE NOTICE 'Phase 3 Complete: market_holidays table dropped';

-- =====================================================================
-- PHASE 4: Remove added columns from markets table
-- =====================================================================

ALTER TABLE markets
DROP COLUMN IF EXISTS regular_market_open,
DROP COLUMN IF EXISTS regular_market_close,
DROP COLUMN IF EXISTS pre_market_open,
DROP COLUMN IF EXISTS pre_market_close,
DROP COLUMN IF EXISTS post_market_open,
DROP COLUMN IF EXISTS post_market_close,
DROP COLUMN IF EXISTS trading_days,
DROP COLUMN IF EXISTS current_status,
DROP COLUMN IF EXISTS next_open_time,
DROP COLUMN IF EXISTS next_close_time,
DROP COLUMN IF EXISTS status_last_updated,
DROP COLUMN IF EXISTS is_holiday_today,
DROP COLUMN IF EXISTS holiday_name,
DROP COLUMN IF EXISTS enable_data_fetching,
DROP COLUMN IF EXISTS data_fetch_interval,
DROP COLUMN IF EXISTS data_fetch_interval_closed;

-- Drop indexes
DROP INDEX IF EXISTS idx_markets_current_status;
DROP INDEX IF EXISTS idx_markets_next_open_time;

RAISE NOTICE 'Phase 4 Complete: Added columns removed from markets table';

-- =====================================================================
-- PHASE 5: Remove CRYPTO market if it was added by consolidation
-- =====================================================================

-- Only delete if created_at matches migration date
DELETE FROM markets
WHERE code = 'CRYPTO'
  AND created_at >= '2025-10-10'::DATE;

RAISE NOTICE 'Phase 5 Complete: CRYPTO market removed (if added by migration)';

-- =====================================================================
-- PHASE 6: Recreate "Markets" table (capitalized)
-- =====================================================================

CREATE TABLE IF NOT EXISTS "Markets" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MarketCode" VARCHAR(50) NOT NULL UNIQUE,
    "MarketName" VARCHAR(200) NOT NULL,
    "Timezone" VARCHAR(100) NOT NULL,
    "Country" VARCHAR(100),
    "Currency" VARCHAR(10),
    "RegularMarketOpen" TIME,
    "RegularMarketClose" TIME,
    "PreMarketOpen" TIME,
    "PreMarketClose" TIME,
    "PostMarketOpen" TIME,
    "PostMarketClose" TIME,
    "TradingDays" INTEGER[] NOT NULL DEFAULT '{1,2,3,4,5}',
    "CurrentStatus" VARCHAR(20),
    "NextOpenTime" TIMESTAMPTZ,
    "NextCloseTime" TIMESTAMPTZ,
    "StatusLastUpdated" TIMESTAMPTZ,
    "IsHolidayToday" BOOLEAN DEFAULT FALSE,
    "HolidayName" VARCHAR(200),
    "EnableDataFetching" BOOLEAN DEFAULT TRUE,
    "DataFetchInterval" INTEGER DEFAULT 5,
    "DataFetchIntervalClosed" INTEGER DEFAULT 300,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT "Markets_MarketCode_Key" UNIQUE ("MarketCode")
);

CREATE INDEX IF NOT EXISTS "IX_Markets_MarketCode" ON "Markets"("MarketCode");
CREATE INDEX IF NOT EXISTS "IX_Markets_CurrentStatus" ON "Markets"("CurrentStatus");
CREATE INDEX IF NOT EXISTS "IX_Markets_NextOpenTime" ON "Markets"("NextOpenTime");

RAISE NOTICE 'Phase 6 Complete: "Markets" table recreated';

-- =====================================================================
-- PHASE 7: Recreate "MarketHolidays" table
-- =====================================================================

CREATE TABLE IF NOT EXISTS "MarketHolidays" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MarketId" UUID NOT NULL REFERENCES "Markets"("Id") ON DELETE CASCADE,
    "HolidayDate" DATE NOT NULL,
    "HolidayName" VARCHAR(200) NOT NULL,
    "IsRecurring" BOOLEAN DEFAULT FALSE,
    "RecurringMonth" INTEGER,
    "RecurringDay" INTEGER,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT "MarketHolidays_Unique" UNIQUE ("MarketId", "HolidayDate")
);

CREATE INDEX IF NOT EXISTS "IX_MarketHolidays_MarketId_Date" ON "MarketHolidays"("MarketId", "HolidayDate");

RAISE NOTICE 'Phase 7 Complete: "MarketHolidays" table recreated';

-- =====================================================================
-- PHASE 8: Restore data to "Markets" table
-- =====================================================================

INSERT INTO "Markets" (
    "MarketCode", "MarketName", "Timezone", "Country", "Currency",
    "RegularMarketOpen", "RegularMarketClose",
    "PreMarketOpen", "PreMarketClose",
    "PostMarketOpen", "PostMarketClose",
    "TradingDays", "CurrentStatus", "EnableDataFetching",
    "DataFetchInterval", "DataFetchIntervalClosed"
) VALUES
-- BIST
(
    'BIST',
    'Borsa Istanbul',
    'Europe/Istanbul',
    'Turkey',
    'TRY',
    '10:00:00'::TIME,
    '18:00:00'::TIME,
    NULL,
    NULL,
    NULL,
    NULL,
    '{1,2,3,4,5}',
    'CLOSED',
    TRUE,
    5,
    300
),
-- NASDAQ
(
    'NASDAQ',
    'NASDAQ Stock Market',
    'America/New_York',
    'USA',
    'USD',
    '09:30:00'::TIME,
    '16:00:00'::TIME,
    '04:00:00'::TIME,
    '09:30:00'::TIME,
    '16:00:00'::TIME,
    '20:00:00'::TIME,
    '{1,2,3,4,5}',
    'CLOSED',
    TRUE,
    5,
    300
),
-- NYSE
(
    'NYSE',
    'New York Stock Exchange',
    'America/New_York',
    'USA',
    'USD',
    '09:30:00'::TIME,
    '16:00:00'::TIME,
    '04:00:00'::TIME,
    '09:30:00'::TIME,
    '16:00:00'::TIME,
    '20:00:00'::TIME,
    '{1,2,3,4,5}',
    'CLOSED',
    TRUE,
    5,
    300
),
-- CRYPTO
(
    'CRYPTO',
    'Cryptocurrency Markets',
    'UTC',
    'Global',
    'USD',
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    '{1,2,3,4,5,6,7}',
    'OPEN',
    TRUE,
    5,
    5
)
ON CONFLICT ("MarketCode") DO NOTHING;

RAISE NOTICE 'Phase 8 Complete: Data restored to "Markets" table';

-- =====================================================================
-- PHASE 9: Restore holiday data
-- =====================================================================

-- Get market IDs
DO $$
DECLARE
    v_bist_id UUID;
    v_nasdaq_id UUID;
    v_nyse_id UUID;
BEGIN
    SELECT "Id" INTO v_bist_id FROM "Markets" WHERE "MarketCode" = 'BIST';
    SELECT "Id" INTO v_nasdaq_id FROM "Markets" WHERE "MarketCode" = 'NASDAQ';
    SELECT "Id" INTO v_nyse_id FROM "Markets" WHERE "MarketCode" = 'NYSE';

    -- 2025 Turkish National Holidays for BIST
    INSERT INTO "MarketHolidays" ("MarketId", "HolidayDate", "HolidayName", "IsRecurring", "RecurringMonth", "RecurringDay")
    VALUES
    (v_bist_id, '2025-01-01', 'Yılbaşı', TRUE, 1, 1),
    (v_bist_id, '2025-04-23', '23 Nisan Ulusal Egemenlik ve Çocuk Bayramı', TRUE, 4, 23),
    (v_bist_id, '2025-05-01', 'İşçi Bayramı', TRUE, 5, 1),
    (v_bist_id, '2025-05-19', 'Atatürk''ü Anma, Gençlik ve Spor Bayramı', TRUE, 5, 19),
    (v_bist_id, '2025-08-30', 'Zafer Bayramı', TRUE, 8, 30),
    (v_bist_id, '2025-10-29', 'Cumhuriyet Bayramı', TRUE, 10, 29),
    (v_bist_id, '2025-03-31', 'Ramazan Bayramı 1. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-04-01', 'Ramazan Bayramı 2. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-04-02', 'Ramazan Bayramı 3. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-07', 'Kurban Bayramı 1. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-08', 'Kurban Bayramı 2. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-09', 'Kurban Bayramı 3. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-10', 'Kurban Bayramı 4. Gün', FALSE, NULL, NULL)
    ON CONFLICT ("MarketId", "HolidayDate") DO NOTHING;

    -- 2025 US Market Holidays
    INSERT INTO "MarketHolidays" ("MarketId", "HolidayDate", "HolidayName", "IsRecurring", "RecurringMonth", "RecurringDay")
    VALUES
    -- NASDAQ
    (v_nasdaq_id, '2025-01-01', 'New Year''s Day', TRUE, 1, 1),
    (v_nasdaq_id, '2025-01-20', 'Martin Luther King Jr. Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2025-02-17', 'Presidents'' Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2025-04-18', 'Good Friday', FALSE, NULL, NULL),
    (v_nasdaq_id, '2025-05-26', 'Memorial Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2025-06-19', 'Juneteenth', TRUE, 6, 19),
    (v_nasdaq_id, '2025-07-04', 'Independence Day', TRUE, 7, 4),
    (v_nasdaq_id, '2025-09-01', 'Labor Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2025-11-27', 'Thanksgiving Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2025-12-25', 'Christmas Day', TRUE, 12, 25),
    -- NYSE
    (v_nyse_id, '2025-01-01', 'New Year''s Day', TRUE, 1, 1),
    (v_nyse_id, '2025-01-20', 'Martin Luther King Jr. Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-02-17', 'Presidents'' Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-04-18', 'Good Friday', FALSE, NULL, NULL),
    (v_nyse_id, '2025-05-26', 'Memorial Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-06-19', 'Juneteenth', TRUE, 6, 19),
    (v_nyse_id, '2025-07-04', 'Independence Day', TRUE, 7, 4),
    (v_nyse_id, '2025-09-01', 'Labor Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-11-27', 'Thanksgiving Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-12-25', 'Christmas Day', TRUE, 12, 25)
    ON CONFLICT ("MarketId", "HolidayDate") DO NOTHING;
END $$;

RAISE NOTICE 'Phase 9 Complete: Holiday data restored';

-- =====================================================================
-- PHASE 10: Recreate update_market_status() function
-- =====================================================================

CREATE OR REPLACE FUNCTION update_market_status()
RETURNS TABLE (
    "MarketCode" VARCHAR(50),
    "OldStatus" VARCHAR(20),
    "NewStatus" VARCHAR(20),
    "NextOpen" TIMESTAMPTZ,
    "NextClose" TIMESTAMPTZ
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
    FOR v_market IN SELECT * FROM "Markets" WHERE "IsActive" = TRUE
    LOOP
        v_current_time := NOW();
        v_local_time := (v_current_time AT TIME ZONE v_market."Timezone")::TIME;
        v_local_date := (v_current_time AT TIME ZONE v_market."Timezone")::DATE;
        v_day_of_week := EXTRACT(ISODOW FROM v_local_date);

        SELECT TRUE, mh."HolidayName"
        INTO v_is_holiday, v_holiday_name
        FROM "MarketHolidays" mh
        WHERE mh."MarketId" = v_market."Id"
          AND mh."HolidayDate" = v_local_date
        LIMIT 1;

        v_is_holiday := COALESCE(v_is_holiday, FALSE);

        IF v_market."MarketCode" = 'CRYPTO' THEN
            v_new_status := 'OPEN';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSIF v_is_holiday THEN
            v_new_status := 'HOLIDAY';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSIF NOT (v_day_of_week = ANY(v_market."TradingDays")) THEN
            v_new_status := 'CLOSED';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSE
            IF v_market."RegularMarketOpen" IS NOT NULL AND v_market."RegularMarketClose" IS NOT NULL THEN
                IF v_local_time >= v_market."RegularMarketOpen" AND v_local_time < v_market."RegularMarketClose" THEN
                    v_new_status := 'OPEN';
                    v_next_close := (v_local_date || ' ' || v_market."RegularMarketClose")::TIMESTAMP AT TIME ZONE v_market."Timezone";
                    v_next_open := NULL;
                ELSIF v_market."PreMarketOpen" IS NOT NULL AND
                      v_local_time >= v_market."PreMarketOpen" AND v_local_time < v_market."RegularMarketOpen" THEN
                    v_new_status := 'PRE_MARKET';
                    v_next_open := (v_local_date || ' ' || v_market."RegularMarketOpen")::TIMESTAMP AT TIME ZONE v_market."Timezone";
                    v_next_close := NULL;
                ELSIF v_market."PostMarketOpen" IS NOT NULL AND
                      v_local_time >= v_market."PostMarketOpen" AND v_local_time < v_market."PostMarketClose" THEN
                    v_new_status := 'POST_MARKET';
                    v_next_close := (v_local_date || ' ' || v_market."PostMarketClose")::TIMESTAMP AT TIME ZONE v_market."Timezone";
                    v_next_open := NULL;
                ELSE
                    v_new_status := 'CLOSED';
                    v_next_open := (v_local_date + INTERVAL '1 day' || ' ' || v_market."RegularMarketOpen")::TIMESTAMP AT TIME ZONE v_market."Timezone";
                    v_next_close := NULL;
                END IF;
            ELSE
                v_new_status := 'CLOSED';
                v_next_open := NULL;
                v_next_close := NULL;
            END IF;
        END IF;

        UPDATE "Markets"
        SET
            "CurrentStatus" = v_new_status,
            "NextOpenTime" = v_next_open,
            "NextCloseTime" = v_next_close,
            "IsHolidayToday" = v_is_holiday,
            "HolidayName" = v_holiday_name,
            "StatusLastUpdated" = v_current_time,
            "UpdatedAt" = v_current_time
        WHERE "Id" = v_market."Id";

        RETURN QUERY SELECT
            v_market."MarketCode",
            v_market."CurrentStatus"::VARCHAR(20),
            v_new_status::VARCHAR(20),
            v_next_open,
            v_next_close;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE 'Phase 10 Complete: update_market_status() function recreated';

-- =====================================================================
-- PHASE 11: Recreate vw_MarketStatus view
-- =====================================================================

CREATE OR REPLACE VIEW "vw_MarketStatus" AS
SELECT
    m."MarketCode",
    m."MarketName",
    m."Timezone",
    m."CurrentStatus",
    m."NextOpenTime",
    m."NextCloseTime",
    m."IsHolidayToday",
    m."HolidayName",
    m."EnableDataFetching",
    CASE
        WHEN m."CurrentStatus" = 'OPEN' THEN m."DataFetchInterval"
        ELSE m."DataFetchIntervalClosed"
    END AS "RecommendedFetchInterval",
    m."StatusLastUpdated",
    EXTRACT(EPOCH FROM (NOW() - m."StatusLastUpdated")) AS "SecondsSinceUpdate"
FROM "Markets" m
WHERE m."IsActive" = TRUE;

COMMENT ON VIEW "vw_MarketStatus" IS 'Current market status with recommended data fetch intervals';

RAISE NOTICE 'Phase 11 Complete: vw_MarketStatus view recreated';

-- =====================================================================
-- PHASE 12: Update market status
-- =====================================================================

SELECT * FROM update_market_status();

RAISE NOTICE 'Phase 12 Complete: Market status updated';

-- =====================================================================
-- VERIFICATION
-- =====================================================================

DO $$
DECLARE
    markets_count INTEGER;
    Markets_count INTEGER;
    holidays_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO markets_count FROM markets WHERE is_active = TRUE;
    SELECT COUNT(*) INTO Markets_count FROM "Markets" WHERE "IsActive" = TRUE;
    SELECT COUNT(*) INTO holidays_count FROM "MarketHolidays";

    RAISE NOTICE '=== Rollback Verification ===';
    RAISE NOTICE 'markets (lowercase) count: %', markets_count;
    RAISE NOTICE 'Markets (capitalized) count: %', Markets_count;
    RAISE NOTICE 'MarketHolidays count: %', holidays_count;
    RAISE NOTICE '============================';

    IF Markets_count < 3 THEN
        RAISE EXCEPTION 'Rollback failed: Expected at least 3 active Markets, found %', Markets_count;
    END IF;
END $$;

COMMIT;

-- =====================================================================
-- ROLLBACK COMPLETE
-- =====================================================================

RAISE NOTICE '========================================';
RAISE NOTICE 'Rollback Complete';
RAISE NOTICE 'Restored original dual-table structure';
RAISE NOTICE '========================================';

SELECT
    'Rollback completed successfully' AS status,
    (SELECT COUNT(*) FROM markets) AS lowercase_markets_count,
    (SELECT COUNT(*) FROM "Markets") AS capitalized_Markets_count,
    (SELECT COUNT(*) FROM "MarketHolidays") AS holidays_count;
