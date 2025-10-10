-- Migration: Create Markets Table for Trading Hours and Holidays
-- Date: 2025-10-10
-- Purpose: Store market trading hours, holidays, and status to optimize data fetching

-- =============================================
-- Create Markets Table
-- =============================================

CREATE TABLE IF NOT EXISTS "Markets" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MarketCode" VARCHAR(50) NOT NULL UNIQUE,  -- BIST, NASDAQ, NYSE, CRYPTO
    "MarketName" VARCHAR(200) NOT NULL,        -- Borsa Istanbul, NASDAQ, etc.
    "Timezone" VARCHAR(100) NOT NULL,          -- Europe/Istanbul, America/New_York, UTC
    "Country" VARCHAR(100),                    -- Turkey, USA, Global
    "Currency" VARCHAR(10),                    -- TRY, USD

    -- Trading Hours (stored in market's local timezone)
    "RegularMarketOpen" TIME,                  -- 10:00:00 for BIST, 09:30:00 for US
    "RegularMarketClose" TIME,                 -- 18:00:00 for BIST, 16:00:00 for US
    "PreMarketOpen" TIME,                      -- 04:00:00 for US pre-market
    "PreMarketClose" TIME,                     -- 09:30:00 for US pre-market
    "PostMarketOpen" TIME,                     -- 16:00:00 for US post-market
    "PostMarketClose" TIME,                    -- 20:00:00 for US post-market

    -- Trading Days
    "TradingDays" INTEGER[] NOT NULL DEFAULT '{1,2,3,4,5}',  -- Monday=1 to Sunday=7

    -- Current Status (calculated and cached)
    "CurrentStatus" VARCHAR(20),               -- OPEN, CLOSED, PRE_MARKET, POST_MARKET, HOLIDAY
    "NextOpenTime" TIMESTAMPTZ,                -- Next opening time in UTC
    "NextCloseTime" TIMESTAMPTZ,               -- Next closing time in UTC
    "StatusLastUpdated" TIMESTAMPTZ,           -- When status was last calculated

    -- Holiday Management
    "IsHolidayToday" BOOLEAN DEFAULT FALSE,    -- Is today a holiday?
    "HolidayName" VARCHAR(200),                -- Name of current holiday (if any)

    -- Data Fetching Optimization
    "EnableDataFetching" BOOLEAN DEFAULT TRUE, -- Should we fetch data for this market?
    "DataFetchInterval" INTEGER DEFAULT 5,     -- Seconds between data fetches when open
    "DataFetchIntervalClosed" INTEGER DEFAULT 300, -- Seconds between checks when closed

    -- Metadata
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ DEFAULT NOW(),

    -- Indexes
    CONSTRAINT "Markets_MarketCode_Key" UNIQUE ("MarketCode")
);

CREATE INDEX IF NOT EXISTS "IX_Markets_MarketCode" ON "Markets"("MarketCode");
CREATE INDEX IF NOT EXISTS "IX_Markets_CurrentStatus" ON "Markets"("CurrentStatus");
CREATE INDEX IF NOT EXISTS "IX_Markets_NextOpenTime" ON "Markets"("NextOpenTime");

-- =============================================
-- Create Market Holidays Table
-- =============================================

CREATE TABLE IF NOT EXISTS "MarketHolidays" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MarketId" UUID NOT NULL REFERENCES "Markets"("Id") ON DELETE CASCADE,
    "HolidayDate" DATE NOT NULL,
    "HolidayName" VARCHAR(200) NOT NULL,
    "IsRecurring" BOOLEAN DEFAULT FALSE,        -- Annual holiday (e.g., New Year)
    "RecurringMonth" INTEGER,                   -- For recurring holidays
    "RecurringDay" INTEGER,                     -- For recurring holidays
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),

    CONSTRAINT "MarketHolidays_Unique" UNIQUE ("MarketId", "HolidayDate")
);

CREATE INDEX IF NOT EXISTS "IX_MarketHolidays_MarketId_Date" ON "MarketHolidays"("MarketId", "HolidayDate");

-- =============================================
-- Insert Initial Market Data
-- =============================================

INSERT INTO "Markets" (
    "MarketCode", "MarketName", "Timezone", "Country", "Currency",
    "RegularMarketOpen", "RegularMarketClose",
    "PreMarketOpen", "PreMarketClose",
    "PostMarketOpen", "PostMarketClose",
    "TradingDays", "CurrentStatus", "EnableDataFetching",
    "DataFetchInterval", "DataFetchIntervalClosed"
) VALUES
-- BIST (Borsa Istanbul)
(
    'BIST',
    'Borsa Istanbul',
    'Europe/Istanbul',
    'Turkey',
    'TRY',
    '10:00:00'::TIME,  -- Market open 10:00 Turkey Time
    '18:00:00'::TIME,  -- Market close 18:00 Turkey Time
    NULL,              -- No pre-market
    NULL,
    NULL,              -- No post-market
    NULL,
    '{1,2,3,4,5}',     -- Monday to Friday
    'CLOSED',
    TRUE,
    5,                 -- Fetch every 5 seconds when open
    300                -- Check every 5 minutes when closed
),
-- NASDAQ
(
    'NASDAQ',
    'NASDAQ Stock Market',
    'America/New_York',
    'USA',
    'USD',
    '09:30:00'::TIME,  -- Regular market open
    '16:00:00'::TIME,  -- Regular market close
    '04:00:00'::TIME,  -- Pre-market open
    '09:30:00'::TIME,  -- Pre-market close
    '16:00:00'::TIME,  -- Post-market open
    '20:00:00'::TIME,  -- Post-market close
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
-- CRYPTO (24/7)
(
    'CRYPTO',
    'Cryptocurrency Markets',
    'UTC',
    'Global',
    'USD',
    NULL,              -- 24/7 - no specific hours
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    '{1,2,3,4,5,6,7}', -- All days
    'OPEN',
    TRUE,
    5,                 -- Always fetch real-time
    5                  -- Same interval (always open)
)
ON CONFLICT ("MarketCode") DO NOTHING;

-- =============================================
-- Insert Common Turkish Holidays for BIST
-- =============================================

-- Get BIST market ID
DO $$
DECLARE
    v_bist_id UUID;
BEGIN
    SELECT "Id" INTO v_bist_id FROM "Markets" WHERE "MarketCode" = 'BIST';

    -- 2025 Turkish National Holidays
    INSERT INTO "MarketHolidays" ("MarketId", "HolidayDate", "HolidayName", "IsRecurring", "RecurringMonth", "RecurringDay")
    VALUES
    -- Fixed Annual Holidays
    (v_bist_id, '2025-01-01', 'Yılbaşı', TRUE, 1, 1),
    (v_bist_id, '2025-04-23', '23 Nisan Ulusal Egemenlik ve Çocuk Bayramı', TRUE, 4, 23),
    (v_bist_id, '2025-05-01', 'İşçi Bayramı', TRUE, 5, 1),
    (v_bist_id, '2025-05-19', 'Atatürk''ü Anma, Gençlik ve Spor Bayramı', TRUE, 5, 19),
    (v_bist_id, '2025-08-30', 'Zafer Bayramı', TRUE, 8, 30),
    (v_bist_id, '2025-10-29', 'Cumhuriyet Bayramı', TRUE, 10, 29),

    -- Religious Holidays (dates change annually)
    (v_bist_id, '2025-03-31', 'Ramazan Bayramı 1. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-04-01', 'Ramazan Bayramı 2. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-04-02', 'Ramazan Bayramı 3. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-07', 'Kurban Bayramı 1. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-08', 'Kurban Bayramı 2. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-09', 'Kurban Bayramı 3. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-10', 'Kurban Bayramı 4. Gün', FALSE, NULL, NULL)
    ON CONFLICT ("MarketId", "HolidayDate") DO NOTHING;
END $$;

-- =============================================
-- Insert Common US Holidays for NASDAQ/NYSE
-- =============================================

DO $$
DECLARE
    v_nasdaq_id UUID;
    v_nyse_id UUID;
BEGIN
    SELECT "Id" INTO v_nasdaq_id FROM "Markets" WHERE "MarketCode" = 'NASDAQ';
    SELECT "Id" INTO v_nyse_id FROM "Markets" WHERE "MarketCode" = 'NYSE';

    -- 2025 US Market Holidays
    INSERT INTO "MarketHolidays" ("MarketId", "HolidayDate", "HolidayName", "IsRecurring", "RecurringMonth", "RecurringDay")
    VALUES
    -- NASDAQ Holidays
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

    -- NYSE Holidays (same as NASDAQ)
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

-- =============================================
-- Create Function to Update Market Status
-- =============================================

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
        -- Get current time in market's timezone
        v_current_time := NOW();
        v_local_time := (v_current_time AT TIME ZONE v_market."Timezone")::TIME;
        v_local_date := (v_current_time AT TIME ZONE v_market."Timezone")::DATE;
        v_day_of_week := EXTRACT(ISODOW FROM v_local_date); -- Monday=1, Sunday=7

        -- Check if today is a holiday
        SELECT TRUE, mh."HolidayName"
        INTO v_is_holiday, v_holiday_name
        FROM "MarketHolidays" mh
        WHERE mh."MarketId" = v_market."Id"
          AND mh."HolidayDate" = v_local_date
        LIMIT 1;

        v_is_holiday := COALESCE(v_is_holiday, FALSE);

        -- Determine market status
        IF v_market."MarketCode" = 'CRYPTO' THEN
            -- Crypto markets are always open
            v_new_status := 'OPEN';
            v_next_open := NULL;
            v_next_close := NULL;

        ELSIF v_is_holiday THEN
            v_new_status := 'HOLIDAY';
            -- Find next trading day
            v_next_open := NULL; -- Would need complex calculation
            v_next_close := NULL;

        ELSIF NOT (v_day_of_week = ANY(v_market."TradingDays")) THEN
            -- Weekend or non-trading day
            v_new_status := 'CLOSED';
            v_next_open := NULL; -- Would calculate next Monday
            v_next_close := NULL;

        ELSE
            -- Regular trading day - check time ranges
            IF v_market."RegularMarketOpen" IS NOT NULL AND v_market."RegularMarketClose" IS NOT NULL THEN
                IF v_local_time >= v_market."RegularMarketOpen" AND v_local_time < v_market."RegularMarketClose" THEN
                    v_new_status := 'OPEN';
                    -- Next close is today at close time
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
                    -- Next open is tomorrow at open time (simplified)
                    v_next_open := (v_local_date + INTERVAL '1 day' || ' ' || v_market."RegularMarketOpen")::TIMESTAMP AT TIME ZONE v_market."Timezone";
                    v_next_close := NULL;
                END IF;
            ELSE
                v_new_status := 'CLOSED';
                v_next_open := NULL;
                v_next_close := NULL;
            END IF;
        END IF;

        -- Update market status
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

        -- Return results
        RETURN QUERY SELECT
            v_market."MarketCode",
            v_market."CurrentStatus"::VARCHAR(20),
            v_new_status::VARCHAR(20),
            v_next_open,
            v_next_close;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- Run Initial Status Update
-- =============================================

SELECT * FROM update_market_status();

-- =============================================
-- Create View for Easy Market Status Queries
-- =============================================

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

-- =============================================
-- Migration Complete
-- =============================================

-- Summary
SELECT
    'Migration Complete' AS "Status",
    COUNT(*) AS "MarketsCreated"
FROM "Markets";

SELECT
    m."MarketCode",
    m."MarketName",
    m."CurrentStatus",
    m."EnableDataFetching",
    COUNT(mh."Id") AS "HolidaysConfigured"
FROM "Markets" m
LEFT JOIN "MarketHolidays" mh ON mh."MarketId" = m."Id"
GROUP BY m."MarketCode", m."MarketName", m."CurrentStatus", m."EnableDataFetching"
ORDER BY m."MarketCode";
