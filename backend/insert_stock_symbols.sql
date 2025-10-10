-- Insert Stock Symbols for myTrader Platform
-- US Stocks: AAPL, MSFT, NVDA, TSLA, JPM, BA, GOOGL
-- BIST Stocks: THY, GARAN, SISE, ISCTR

-- First, ensure we have the markets table populated
INSERT INTO markets ("Code", "Name", "Timezone", "IsActive", "OpenTime", "CloseTime")
VALUES
    ('NASDAQ', 'NASDAQ Stock Market', 'America/New_York', true, '09:30:00', '16:00:00'),
    ('NYSE', 'New York Stock Exchange', 'America/New_York', true, '09:30:00', '16:00:00'),
    ('BIST', 'Borsa Istanbul', 'Europe/Istanbul', true, '09:30:00', '18:00:00'),
    ('BINANCE', 'Binance Cryptocurrency Exchange', 'UTC', true, '00:00:00', '23:59:59')
ON CONFLICT ("Code") DO NOTHING;

-- Insert US Stock Symbols
INSERT INTO symbols ("Symbol", "Name", "AssetClass", "Exchange", "MarketCode", "IsActive", "Currency", "Country")
VALUES
    -- NASDAQ Stocks
    ('AAPL', 'Apple Inc.', 'STOCK', 'NASDAQ', 'NASDAQ', true, 'USD', 'US'),
    ('MSFT', 'Microsoft Corporation', 'STOCK', 'NASDAQ', 'NASDAQ', true, 'USD', 'US'),
    ('NVDA', 'NVIDIA Corporation', 'STOCK', 'NASDAQ', 'NASDAQ', true, 'USD', 'US'),
    ('TSLA', 'Tesla Inc.', 'STOCK', 'NASDAQ', 'NASDAQ', true, 'USD', 'US'),
    ('GOOGL', 'Alphabet Inc.', 'STOCK', 'NASDAQ', 'NASDAQ', true, 'USD', 'US'),

    -- NYSE Stocks
    ('JPM', 'JPMorgan Chase & Co.', 'STOCK', 'NYSE', 'NYSE', true, 'USD', 'US'),
    ('BA', 'The Boeing Company', 'STOCK', 'NYSE', 'NYSE', true, 'USD', 'US'),

    -- BIST Stocks (Turkish stocks need .IS suffix for Yahoo Finance)
    ('THY.IS', 'Türk Hava Yolları', 'STOCK', 'BIST', 'BIST', true, 'TRY', 'TR'),
    ('GARAN.IS', 'Garanti Bankası', 'STOCK', 'BIST', 'BIST', true, 'TRY', 'TR'),
    ('SISE.IS', 'Şişe Cam', 'STOCK', 'BIST', 'BIST', true, 'TRY', 'TR'),
    ('ISCTR.IS', 'İş Bankası (C)', 'STOCK', 'BIST', 'BIST', true, 'TRY', 'TR')
ON CONFLICT ("Symbol") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "AssetClass" = EXCLUDED."AssetClass",
    "Exchange" = EXCLUDED."Exchange",
    "MarketCode" = EXCLUDED."MarketCode",
    "IsActive" = EXCLUDED."IsActive",
    "Currency" = EXCLUDED."Currency",
    "Country" = EXCLUDED."Country";

-- Verify insertion
SELECT "Symbol", "Name", "Exchange", "MarketCode", "Currency"
FROM symbols
WHERE "AssetClass" = 'STOCK'
ORDER BY "Exchange", "Symbol";
