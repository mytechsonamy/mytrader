-- Populate Complete Data for myTrader Platform
-- Asset Classes → Markets → Symbols

-- Step 1: Insert Asset Classes
INSERT INTO asset_classes ("Id", code, name, name_tr, description, primary_currency,
    default_price_precision, default_quantity_precision, supports_24_7_trading,
    supports_fractional, min_trade_amount, is_active, display_order, created_at, updated_at)
VALUES
    (gen_random_uuid(), 'STOCK', 'Stocks', 'Hisse Senetleri', 'Company shares traded on stock exchanges',
        'USD', 2, 0, false, false, 1.00, true, 1, NOW(), NOW()),
    (gen_random_uuid(), 'CRYPTO', 'Cryptocurrency', 'Kripto Para', 'Digital cryptocurrencies',
        'USD', 2, 8, true, true, 10.00, true, 2, NOW(), NOW())
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    name_tr = EXCLUDED.name_tr,
    updated_at = NOW();

-- Step 2: Insert Markets (using asset class IDs)
INSERT INTO markets ("Id", code, name, name_tr, "AssetClassId", country_code, timezone,
    primary_currency, status, is_active, has_realtime_data, data_delay_minutes, display_order,
    created_at, updated_at)
SELECT
    gen_random_uuid(),
    m.code,
    m.name,
    m.name_tr,
    ac."Id",
    m.country_code,
    m.timezone,
    m.primary_currency,
    'OPEN',
    true,
    m.has_realtime_data,
    m.data_delay_minutes,
    m.display_order,
    NOW(),
    NOW()
FROM (
    VALUES
        ('NASDAQ', 'NASDAQ Stock Market', 'NASDAQ Borsası', 'STOCK', 'US', 'America/New_York', 'USD', false, 15, 1),
        ('NYSE', 'New York Stock Exchange', 'New York Borsası', 'STOCK', 'US', 'America/New_York', 'USD', false, 15, 2),
        ('BIST', 'Borsa Istanbul', 'Borsa İstanbul', 'STOCK', 'TR', 'Europe/Istanbul', 'TRY', false, 15, 3),
        ('BINANCE', 'Binance Exchange', 'Binance', 'CRYPTO', 'GLOBAL', 'UTC', 'USD', true, 0, 4)
) AS m(code, name, name_tr, asset_class_code, country_code, timezone, primary_currency, has_realtime_data, data_delay_minutes, display_order)
JOIN asset_classes ac ON ac.code = m.asset_class_code
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    name_tr = EXCLUDED.name_tr,
    updated_at = NOW();

-- Step 3: Insert Stock Symbols
INSERT INTO symbols ("Id", ticker, venue, asset_class, asset_class_id, market_id,
    full_name, full_name_tr, display, country, base_currency, quote_currency,
    is_active, is_tracked, is_popular, display_order, created_at, updated_at)
SELECT
    gen_random_uuid(),
    s.ticker,
    s.venue,
    'STOCK',
    ac."Id",
    m."Id",
    s.full_name,
    s.full_name_tr,
    s.ticker,
    s.country,
    s.currency,
    s.currency,
    true,
    true,
    true,
    s.display_order,
    NOW(),
    NOW()
FROM (
    VALUES
        -- NASDAQ Stocks
        ('AAPL', 'NASDAQ', 'Apple Inc.', 'Apple Inc.', 'US', 'USD', 1),
        ('MSFT', 'NASDAQ', 'Microsoft Corporation', 'Microsoft Corporation', 'US', 'USD', 2),
        ('NVDA', 'NASDAQ', 'NVIDIA Corporation', 'NVIDIA Corporation', 'US', 'USD', 3),
        ('TSLA', 'NASDAQ', 'Tesla Inc.', 'Tesla Inc.', 'US', 'USD', 4),
        ('GOOGL', 'NASDAQ', 'Alphabet Inc.', 'Alphabet Inc.', 'US', 'USD', 5),

        -- NYSE Stocks
        ('JPM', 'NYSE', 'JPMorgan Chase & Co.', 'JPMorgan Chase & Co.', 'US', 'USD', 6),
        ('BA', 'NYSE', 'The Boeing Company', 'The Boeing Company', 'US', 'USD', 7),

        -- BIST Stocks (Turkish stocks need .IS suffix for Yahoo Finance)
        ('THY.IS', 'BIST', 'Türk Hava Yolları', 'Türk Hava Yolları', 'TR', 'TRY', 8),
        ('GARAN.IS', 'BIST', 'Garanti Bankası', 'Garanti Bankası', 'TR', 'TRY', 9),
        ('SISE.IS', 'BIST', 'Şişe Cam', 'Şişe Cam', 'TR', 'TRY', 10),
        ('ISCTR.IS', 'BIST', 'İş Bankası (C)', 'İş Bankası (C)', 'TR', 'TRY', 11)
) AS s(ticker, venue, full_name, full_name_tr, country, currency, display_order)
JOIN asset_classes ac ON ac.code = 'STOCK'
JOIN markets m ON m.code = s.venue
ON CONFLICT (ticker, venue) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    full_name_tr = EXCLUDED.full_name_tr,
    is_active = true,
    is_tracked = true,
    is_popular = true,
    updated_at = NOW();

-- Verification Queries
SELECT 'Asset Classes:' as info;
SELECT code, name, name_tr FROM asset_classes ORDER BY display_order;

SELECT 'Markets:' as info;
SELECT m.code, m.name, m.name_tr, ac.code as asset_class
FROM markets m
JOIN asset_classes ac ON m."AssetClassId" = ac."Id"
ORDER BY m.display_order;

SELECT 'Stock Symbols:' as info;
SELECT s.ticker, s.venue, s.full_name, s.country
FROM symbols s
WHERE s.asset_class = 'STOCK'
ORDER BY s.display_order;
