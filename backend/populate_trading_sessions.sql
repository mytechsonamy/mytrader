-- Populate Trading Sessions for all Markets
-- day_of_week: 0=Sunday, 1=Monday, 2=Tuesday, 3=Wednesday, 4=Thursday, 5=Friday, 6=Saturday

-- NASDAQ Trading Sessions (Monday-Friday, 09:30-16:00 EST)
INSERT INTO trading_sessions ("Id", "MarketId", session_name, session_type, day_of_week,
    start_time, end_time, spans_midnight, is_primary, is_trading_enabled,
    volume_multiplier, is_active, display_order, created_at, updated_at)
SELECT
    gen_random_uuid(),
    m."Id",
    'Regular Trading',
    'REGULAR',
    d.day_of_week,
    '09:30:00'::time,
    '16:00:00'::time,
    false,
    true,
    true,
    1.0,
    true,
    d.day_of_week,
    NOW(),
    NOW()
FROM markets m
CROSS JOIN (VALUES (1), (2), (3), (4), (5)) AS d(day_of_week)
WHERE m.code = 'NASDAQ';

-- NYSE Trading Sessions (Monday-Friday, 09:30-16:00 EST)
INSERT INTO trading_sessions ("Id", "MarketId", session_name, session_type, day_of_week,
    start_time, end_time, spans_midnight, is_primary, is_trading_enabled,
    volume_multiplier, is_active, display_order, created_at, updated_at)
SELECT
    gen_random_uuid(),
    m."Id",
    'Regular Trading',
    'REGULAR',
    d.day_of_week,
    '09:30:00'::time,
    '16:00:00'::time,
    false,
    true,
    true,
    1.0,
    true,
    d.day_of_week,
    NOW(),
    NOW()
FROM markets m
CROSS JOIN (VALUES (1), (2), (3), (4), (5)) AS d(day_of_week)
WHERE m.code = 'NYSE';

-- BIST Trading Sessions (Monday-Friday, 09:30-18:00 Turkey Time)
INSERT INTO trading_sessions ("Id", "MarketId", session_name, session_type, day_of_week,
    start_time, end_time, spans_midnight, is_primary, is_trading_enabled,
    volume_multiplier, is_active, display_order, created_at, updated_at)
SELECT
    gen_random_uuid(),
    m."Id",
    'Regular Trading',
    'REGULAR',
    d.day_of_week,
    '09:30:00'::time,
    '18:00:00'::time,
    false,
    true,
    true,
    1.0,
    true,
    d.day_of_week,
    NOW(),
    NOW()
FROM markets m
CROSS JOIN (VALUES (1), (2), (3), (4), (5)) AS d(day_of_week)
WHERE m.code = 'BIST';

-- BINANCE Trading Sessions (24/7, all days of week)
INSERT INTO trading_sessions ("Id", "MarketId", session_name, session_type, day_of_week,
    start_time, end_time, spans_midnight, is_primary, is_trading_enabled,
    volume_multiplier, is_active, display_order, created_at, updated_at)
SELECT
    gen_random_uuid(),
    m."Id",
    '24/7 Trading',
    'CONTINUOUS',
    d.day_of_week,
    '00:00:00'::time,
    '23:59:59'::time,
    false,
    true,
    true,
    1.0,
    true,
    d.day_of_week,
    NOW(),
    NOW()
FROM markets m
CROSS JOIN (VALUES (0), (1), (2), (3), (4), (5), (6)) AS d(day_of_week)
WHERE m.code = 'BINANCE';

-- Verification: Show all trading sessions grouped by market
SELECT
    m.code as market,
    m.name,
    ts.session_name,
    ts.day_of_week,
    CASE ts.day_of_week
        WHEN 0 THEN 'Sunday'
        WHEN 1 THEN 'Monday'
        WHEN 2 THEN 'Tuesday'
        WHEN 3 THEN 'Wednesday'
        WHEN 4 THEN 'Thursday'
        WHEN 5 THEN 'Friday'
        WHEN 6 THEN 'Saturday'
    END as day_name,
    ts.start_time,
    ts.end_time,
    ts.is_trading_enabled
FROM trading_sessions ts
JOIN markets m ON ts."MarketId" = m."Id"
ORDER BY m.code, ts.day_of_week;
