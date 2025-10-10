-- =====================================================================
-- Migration: Populate Market Holidays
-- Version: 1.0
-- Date: 2025-10-10
-- Description: Populate market_holidays table with data from MarketHoursService
-- =====================================================================

BEGIN;

-- =====================================================================
-- PHASE 1: Get Market IDs
-- =====================================================================

DO $$
DECLARE
    v_bist_id UUID;
    v_nasdaq_id UUID;
    v_nyse_id UUID;
BEGIN
    SELECT "Id" INTO v_bist_id FROM markets WHERE code = 'BIST';
    SELECT "Id" INTO v_nasdaq_id FROM markets WHERE code = 'NASDAQ';
    SELECT "Id" INTO v_nyse_id FROM markets WHERE code = 'NYSE';

    RAISE NOTICE 'Market IDs retrieved:';
    RAISE NOTICE '  BIST: %', v_bist_id;
    RAISE NOTICE '  NASDAQ: %', v_nasdaq_id;
    RAISE NOTICE '  NYSE: %', v_nyse_id;

    -- =====================================================================
    -- PHASE 2: Insert BIST Holidays (Turkey)
    -- =====================================================================

    -- Turkish National Holidays 2025
    INSERT INTO market_holidays (market_id, holiday_date, holiday_name, is_recurring, recurring_month, recurring_day)
    VALUES
    (v_bist_id, '2025-01-01', 'Yılbaşı', TRUE, 1, 1),
    (v_bist_id, '2025-04-23', '23 Nisan Ulusal Egemenlik ve Çocuk Bayramı', TRUE, 4, 23),
    (v_bist_id, '2025-05-01', 'İşçi Bayramı', TRUE, 5, 1),
    (v_bist_id, '2025-05-19', 'Atatürk''ü Anma, Gençlik ve Spor Bayramı', TRUE, 5, 19),
    (v_bist_id, '2025-07-15', 'Demokrasi ve Milli Birlik Günü', TRUE, 7, 15),
    (v_bist_id, '2025-08-30', 'Zafer Bayramı', TRUE, 8, 30),
    (v_bist_id, '2025-10-29', 'Cumhuriyet Bayramı', TRUE, 10, 29),
    -- Religious holidays (dates change each year based on lunar calendar)
    (v_bist_id, '2025-03-31', 'Ramazan Bayramı 1. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-04-01', 'Ramazan Bayramı 2. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-04-02', 'Ramazan Bayramı 3. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-07', 'Kurban Bayramı 1. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-08', 'Kurban Bayramı 2. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-09', 'Kurban Bayramı 3. Gün', FALSE, NULL, NULL),
    (v_bist_id, '2025-06-10', 'Kurban Bayramı 4. Gün', FALSE, NULL, NULL),
    -- 2026 holidays for forward planning
    (v_bist_id, '2026-01-01', 'Yılbaşı', TRUE, 1, 1),
    (v_bist_id, '2026-04-23', '23 Nisan Ulusal Egemenlik ve Çocuk Bayramı', TRUE, 4, 23),
    (v_bist_id, '2026-05-01', 'İşçi Bayramı', TRUE, 5, 1),
    (v_bist_id, '2026-05-19', 'Atatürk''ü Anma, Gençlik ve Spor Bayramı', TRUE, 5, 19),
    (v_bist_id, '2026-07-15', 'Demokrasi ve Milli Birlik Günü', TRUE, 7, 15),
    (v_bist_id, '2026-08-30', 'Zafer Bayramı', TRUE, 8, 30),
    (v_bist_id, '2026-10-29', 'Cumhuriyet Bayramı', TRUE, 10, 29)
    ON CONFLICT (market_id, holiday_date) DO NOTHING;

    RAISE NOTICE 'BIST holidays inserted';

    -- =====================================================================
    -- PHASE 3: Insert US Market Holidays (NASDAQ & NYSE)
    -- =====================================================================

    -- 2025 US Market Holidays
    INSERT INTO market_holidays (market_id, holiday_date, holiday_name, is_recurring, recurring_month, recurring_day)
    VALUES
    -- NASDAQ 2025
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
    -- NYSE 2025
    (v_nyse_id, '2025-01-01', 'New Year''s Day', TRUE, 1, 1),
    (v_nyse_id, '2025-01-20', 'Martin Luther King Jr. Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-02-17', 'Presidents'' Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-04-18', 'Good Friday', FALSE, NULL, NULL),
    (v_nyse_id, '2025-05-26', 'Memorial Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-06-19', 'Juneteenth', TRUE, 6, 19),
    (v_nyse_id, '2025-07-04', 'Independence Day', TRUE, 7, 4),
    (v_nyse_id, '2025-09-01', 'Labor Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-11-27', 'Thanksgiving Day', FALSE, NULL, NULL),
    (v_nyse_id, '2025-12-25', 'Christmas Day', TRUE, 12, 25),
    -- 2026 US Market Holidays
    (v_nasdaq_id, '2026-01-01', 'New Year''s Day', TRUE, 1, 1),
    (v_nasdaq_id, '2026-01-19', 'Martin Luther King Jr. Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2026-02-16', 'Presidents'' Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2026-04-03', 'Good Friday', FALSE, NULL, NULL),
    (v_nasdaq_id, '2026-05-25', 'Memorial Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2026-06-19', 'Juneteenth', TRUE, 6, 19),
    (v_nasdaq_id, '2026-07-03', 'Independence Day (observed)', FALSE, NULL, NULL),
    (v_nasdaq_id, '2026-09-07', 'Labor Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2026-11-26', 'Thanksgiving Day', FALSE, NULL, NULL),
    (v_nasdaq_id, '2026-12-25', 'Christmas Day', TRUE, 12, 25),
    (v_nyse_id, '2026-01-01', 'New Year''s Day', TRUE, 1, 1),
    (v_nyse_id, '2026-01-19', 'Martin Luther King Jr. Day', FALSE, NULL, NULL),
    (v_nyse_id, '2026-02-16', 'Presidents'' Day', FALSE, NULL, NULL),
    (v_nyse_id, '2026-04-03', 'Good Friday', FALSE, NULL, NULL),
    (v_nyse_id, '2026-05-25', 'Memorial Day', FALSE, NULL, NULL),
    (v_nyse_id, '2026-06-19', 'Juneteenth', TRUE, 6, 19),
    (v_nyse_id, '2026-07-03', 'Independence Day (observed)', FALSE, NULL, NULL),
    (v_nyse_id, '2026-09-07', 'Labor Day', FALSE, NULL, NULL),
    (v_nyse_id, '2026-11-26', 'Thanksgiving Day', FALSE, NULL, NULL),
    (v_nyse_id, '2026-12-25', 'Christmas Day', TRUE, 12, 25)
    ON CONFLICT (market_id, holiday_date) DO NOTHING;

    RAISE NOTICE 'US market holidays inserted';
END $$;

-- =====================================================================
-- VERIFICATION
-- =====================================================================

SELECT
    'Holiday population completed' AS status,
    (SELECT COUNT(*) FROM market_holidays WHERE market_id = (SELECT "Id" FROM markets WHERE code = 'BIST')) AS bist_holidays,
    (SELECT COUNT(*) FROM market_holidays WHERE market_id = (SELECT "Id" FROM markets WHERE code = 'NASDAQ')) AS nasdaq_holidays,
    (SELECT COUNT(*) FROM market_holidays WHERE market_id = (SELECT "Id" FROM markets WHERE code = 'NYSE')) AS nyse_holidays;

COMMIT;

-- =====================================================================
-- MIGRATION COMPLETE
-- =====================================================================

SELECT
    m.code AS market,
    COUNT(mh.id) AS total_holidays,
    COUNT(*) FILTER (WHERE mh.holiday_date >= CURRENT_DATE) AS upcoming_holidays,
    COUNT(*) FILTER (WHERE mh.is_recurring = TRUE) AS recurring_holidays
FROM markets m
LEFT JOIN market_holidays mh ON mh.market_id = m."Id"
WHERE m.is_active = TRUE
GROUP BY m.code
ORDER BY m.code;
