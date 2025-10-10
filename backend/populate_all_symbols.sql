-- Populate ALL Symbols (Crypto + Stocks)
-- This script populates 7 crypto symbols and 11 stock symbols

-- First, get required IDs
DO $$
DECLARE
    v_crypto_asset_class_id UUID;
    v_stock_asset_class_id UUID;
    v_binance_market_id UUID;
    v_nasdaq_market_id UUID;
    v_nyse_market_id UUID;
    v_bist_market_id UUID;
BEGIN
    -- Get asset class IDs
    SELECT "Id" INTO v_crypto_asset_class_id FROM asset_classes WHERE code = 'CRYPTO';
    SELECT "Id" INTO v_stock_asset_class_id FROM asset_classes WHERE code = 'STOCK';

    -- Get market IDs
    SELECT "Id" INTO v_binance_market_id FROM markets WHERE code = 'BINANCE';
    SELECT "Id" INTO v_nasdaq_market_id FROM markets WHERE code = 'NASDAQ';
    SELECT "Id" INTO v_nyse_market_id FROM markets WHERE code = 'NYSE';
    SELECT "Id" INTO v_bist_market_id FROM markets WHERE code = 'BIST';

    -- ========================================
    -- CRYPTO SYMBOLS (7 coins)
    -- ========================================

    -- Insert 7 crypto symbols
    INSERT INTO symbols ("Id", ticker, venue, asset_class, asset_class_id, market_id,
        base_currency, quote_currency, full_name, full_name_tr, display,
        is_active, is_tracked, is_popular, display_order, created_at, updated_at)
    VALUES
        (gen_random_uuid(), 'BTCUSDT', 'BINANCE', 'CRYPTO', v_crypto_asset_class_id, v_binance_market_id,
         'BTC', 'USDT', 'Bitcoin', 'Bitcoin', 'BTC/USDT',
         TRUE, TRUE, TRUE, 1, NOW(), NOW()),
        (gen_random_uuid(), 'ETHUSDT', 'BINANCE', 'CRYPTO', v_crypto_asset_class_id, v_binance_market_id,
         'ETH', 'USDT', 'Ethereum', 'Ethereum', 'ETH/USDT',
         TRUE, TRUE, TRUE, 2, NOW(), NOW()),
        (gen_random_uuid(), 'XRPUSDT', 'BINANCE', 'CRYPTO', v_crypto_asset_class_id, v_binance_market_id,
         'XRP', 'USDT', 'Ripple', 'Ripple', 'XRP/USDT',
         TRUE, TRUE, TRUE, 3, NOW(), NOW()),
        (gen_random_uuid(), 'SOLUSDT', 'BINANCE', 'CRYPTO', v_crypto_asset_class_id, v_binance_market_id,
         'SOL', 'USDT', 'Solana', 'Solana', 'SOL/USDT',
         TRUE, TRUE, TRUE, 4, NOW(), NOW()),
        (gen_random_uuid(), 'AVAXUSDT', 'BINANCE', 'CRYPTO', v_crypto_asset_class_id, v_binance_market_id,
         'AVAX', 'USDT', 'Avalanche', 'Avalanche', 'AVAX/USDT',
         TRUE, TRUE, TRUE, 5, NOW(), NOW()),
        (gen_random_uuid(), 'SUIUSDT', 'BINANCE', 'CRYPTO', v_crypto_asset_class_id, v_binance_market_id,
         'SUI', 'USDT', 'Sui', 'Sui', 'SUI/USDT',
         TRUE, TRUE, TRUE, 6, NOW(), NOW()),
        (gen_random_uuid(), 'ENAUSDT', 'BINANCE', 'CRYPTO', v_crypto_asset_class_id, v_binance_market_id,
         'ENA', 'USDT', 'Ethena', 'Ethena', 'ENA/USDT',
         TRUE, TRUE, TRUE, 7, NOW(), NOW());

    -- ========================================
    -- STOCK SYMBOLS (11 stocks)
    -- ========================================

    -- NASDAQ Stocks (5)
    INSERT INTO symbols ("Id", ticker, venue, asset_class, asset_class_id, market_id,
        base_currency, quote_currency, full_name, full_name_tr, display, country,
        is_active, is_tracked, is_popular, display_order, created_at, updated_at)
    VALUES
        (gen_random_uuid(), 'AAPL', 'NASDAQ', 'STOCK', v_stock_asset_class_id, v_nasdaq_market_id,
         'USD', 'USD', 'Apple Inc.', 'Apple Inc.', 'AAPL', 'US',
         TRUE, TRUE, TRUE, 101, NOW(), NOW()),
        (gen_random_uuid(), 'MSFT', 'NASDAQ', 'STOCK', v_stock_asset_class_id, v_nasdaq_market_id,
         'USD', 'USD', 'Microsoft Corporation', 'Microsoft Corporation', 'MSFT', 'US',
         TRUE, TRUE, TRUE, 102, NOW(), NOW()),
        (gen_random_uuid(), 'NVDA', 'NASDAQ', 'STOCK', v_stock_asset_class_id, v_nasdaq_market_id,
         'USD', 'USD', 'NVIDIA Corporation', 'NVIDIA Corporation', 'NVDA', 'US',
         TRUE, TRUE, TRUE, 103, NOW(), NOW()),
        (gen_random_uuid(), 'TSLA', 'NASDAQ', 'STOCK', v_stock_asset_class_id, v_nasdaq_market_id,
         'USD', 'USD', 'Tesla Inc.', 'Tesla Inc.', 'TSLA', 'US',
         TRUE, TRUE, TRUE, 104, NOW(), NOW()),
        (gen_random_uuid(), 'GOOGL', 'NASDAQ', 'STOCK', v_stock_asset_class_id, v_nasdaq_market_id,
         'USD', 'USD', 'Alphabet Inc.', 'Alphabet Inc.', 'GOOGL', 'US',
         TRUE, TRUE, TRUE, 105, NOW(), NOW());

    -- NYSE Stocks (2)
    INSERT INTO symbols ("Id", ticker, venue, asset_class, asset_class_id, market_id,
        base_currency, quote_currency, full_name, full_name_tr, display, country,
        is_active, is_tracked, is_popular, display_order, created_at, updated_at)
    VALUES
        (gen_random_uuid(), 'JPM', 'NYSE', 'STOCK', v_stock_asset_class_id, v_nyse_market_id,
         'USD', 'USD', 'JPMorgan Chase & Co.', 'JPMorgan Chase & Co.', 'JPM', 'US',
         TRUE, TRUE, TRUE, 106, NOW(), NOW()),
        (gen_random_uuid(), 'BA', 'NYSE', 'STOCK', v_stock_asset_class_id, v_nyse_market_id,
         'USD', 'USD', 'The Boeing Company', 'The Boeing Company', 'BA', 'US',
         TRUE, TRUE, TRUE, 107, NOW(), NOW());

    -- BIST Stocks (4)
    INSERT INTO symbols ("Id", ticker, venue, asset_class, asset_class_id, market_id,
        base_currency, quote_currency, full_name, full_name_tr, display, country,
        is_active, is_tracked, is_popular, display_order, created_at, updated_at)
    VALUES
        (gen_random_uuid(), 'THY.IS', 'BIST', 'STOCK', v_stock_asset_class_id, v_bist_market_id,
         'TRY', 'TRY', 'Türk Hava Yolları', 'Türk Hava Yolları', 'THYAO', 'TR',
         TRUE, TRUE, TRUE, 108, NOW(), NOW()),
        (gen_random_uuid(), 'GARAN.IS', 'BIST', 'STOCK', v_stock_asset_class_id, v_bist_market_id,
         'TRY', 'TRY', 'Garanti Bankası', 'Garanti Bankası', 'GARAN', 'TR',
         TRUE, TRUE, TRUE, 109, NOW(), NOW()),
        (gen_random_uuid(), 'SISE.IS', 'BIST', 'STOCK', v_stock_asset_class_id, v_bist_market_id,
         'TRY', 'TRY', 'Şişe Cam', 'Şişe Cam', 'SISE', 'TR',
         TRUE, TRUE, TRUE, 110, NOW(), NOW()),
        (gen_random_uuid(), 'ISCTR.IS', 'BIST', 'STOCK', v_stock_asset_class_id, v_bist_market_id,
         'TRY', 'TRY', 'İş Bankası (C)', 'İş Bankası (C)', 'ISCTR', 'TR',
         TRUE, TRUE, TRUE, 111, NOW(), NOW());

    RAISE NOTICE 'Symbol population complete!';
    RAISE NOTICE '  - 7 crypto symbols inserted';
    RAISE NOTICE '  - 11 stock symbols inserted';
    RAISE NOTICE '  Total: 18 symbols';
END $$;

-- Verification
SELECT 'Crypto symbols:' as info;
SELECT ticker, venue, full_name FROM symbols WHERE asset_class = 'CRYPTO' ORDER BY display_order;

SELECT 'Stock symbols:' as info;
SELECT ticker, venue, full_name, country FROM symbols WHERE asset_class = 'STOCK' ORDER BY display_order;

SELECT 'Summary:' as info;
SELECT
    asset_class,
    COUNT(*) as symbol_count,
    COUNT(*) FILTER (WHERE is_active) as active_count,
    COUNT(*) FILTER (WHERE is_tracked) as tracked_count
FROM symbols
GROUP BY asset_class
ORDER BY asset_class;
