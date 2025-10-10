-- =====================================================================
-- Default Data Population for Data-Driven Symbols
-- Version: 1.0
-- Date: 2025-01-08
-- Author: Data Architecture Manager
-- Description: Comprehensive default data setup for symbols, markets,
--              data providers, and asset classes
-- =====================================================================

-- This script is IDEMPOTENT - safe to run multiple times

-- =====================================================================
-- SECTION 1: ENSURE ASSET CLASSES EXIST
-- =====================================================================

-- Insert or update CRYPTO asset class
INSERT INTO asset_classes (
    id,
    code,
    name,
    name_tr,
    description,
    primary_currency,
    default_price_precision,
    default_quantity_precision,
    supports_24_7_trading,
    supports_fractional,
    min_trade_amount,
    regulatory_class,
    is_active,
    display_order
)
VALUES (
    gen_random_uuid(),
    'CRYPTO',
    'Cryptocurrency',
    'Kripto Para',
    'Digital cryptocurrencies and tokens traded 24/7',
    'USD',
    8,
    8,
    TRUE,
    TRUE,
    10.00,
    'unregulated',
    TRUE,
    1
)
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    name_tr = EXCLUDED.name_tr,
    description = EXCLUDED.description,
    is_active = TRUE,
    updated_at = CURRENT_TIMESTAMP;

-- =====================================================================
-- SECTION 2: ENSURE MARKETS EXIST
-- =====================================================================

-- Insert or update BINANCE market
INSERT INTO markets (
    id,
    code,
    name,
    name_tr,
    description,
    asset_class_id,
    country_code,
    timezone,
    primary_currency,
    market_maker,
    api_base_url,
    websocket_url,
    default_commission_rate,
    min_commission,
    status,
    is_active,
    has_realtime_data,
    data_delay_minutes,
    display_order
)
SELECT
    gen_random_uuid(),
    'BINANCE',
    'Binance',
    'Binance',
    'Global cryptocurrency exchange with real-time WebSocket data',
    ac.id,
    'GLOBAL',
    'UTC',
    'USDT',
    'BINANCE',
    'https://api.binance.com',
    'wss://stream.binance.com:9443',
    0.001,
    0.00,
    'OPEN',
    TRUE,
    TRUE,
    0,
    1
FROM asset_classes ac
WHERE ac.code = 'CRYPTO'
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    websocket_url = EXCLUDED.websocket_url,
    is_active = TRUE,
    updated_at = CURRENT_TIMESTAMP;

-- =====================================================================
-- SECTION 3: ENSURE DATA PROVIDERS EXIST
-- =====================================================================

-- Insert or update BINANCE WebSocket data provider
INSERT INTO data_providers (
    id,
    code,
    name,
    description,
    market_id,
    provider_type,
    feed_type,
    endpoint_url,
    websocket_url,
    auth_type,
    rate_limit_per_minute,
    timeout_seconds,
    max_retries,
    retry_delay_ms,
    data_delay_minutes,
    connection_status,
    is_active,
    is_primary,
    priority
)
SELECT
    gen_random_uuid(),
    'BINANCE_WS',
    'Binance WebSocket',
    'Real-time cryptocurrency prices via Binance WebSocket API',
    m.id,
    'REALTIME',
    'WEBSOCKET',
    'https://api.binance.com/api/v3',
    'wss://stream.binance.com:9443/ws',
    'NONE',
    1200,
    30,
    3,
    1000,
    0,
    'CONNECTED',
    TRUE,
    TRUE,
    1
FROM markets m
WHERE m.code = 'BINANCE'
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    websocket_url = EXCLUDED.websocket_url,
    is_active = TRUE,
    is_primary = TRUE,
    updated_at = CURRENT_TIMESTAMP;

-- =====================================================================
-- SECTION 4: INSERT DEFAULT SYMBOLS (9 PRIMARY CRYPTO ASSETS)
-- =====================================================================

-- Get required foreign key IDs
DO $$
DECLARE
    v_asset_class_id UUID;
    v_market_id UUID;
    v_provider_id UUID;
BEGIN
    -- Retrieve IDs for foreign keys
    SELECT id INTO v_asset_class_id FROM asset_classes WHERE code = 'CRYPTO';
    SELECT id INTO v_market_id FROM markets WHERE code = 'BINANCE';
    SELECT id INTO v_provider_id FROM data_providers WHERE code = 'BINANCE_WS';

    -- Insert default symbols if they don't exist
    -- Bitcoin (BTC)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'BTCUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'BTC', 'USDT', 'Bitcoin', 'Bitcoin', 'The first and most valuable cryptocurrency',
        TRUE, TRUE, TRUE, TRUE,
        100, 2, 6,
        1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 100,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 1,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    -- Ethereum (ETH)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'ETHUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'ETH', 'USDT', 'Ethereum', 'Ethereum', 'Smart contract platform and second-largest cryptocurrency',
        TRUE, TRUE, TRUE, TRUE,
        95, 2, 6,
        2, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 95,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 2,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    -- Ripple (XRP)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'XRPUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'XRP', 'USDT', 'Ripple', 'XRP', 'Digital payment protocol for financial institutions',
        TRUE, TRUE, TRUE, TRUE,
        90, 4, 6,
        3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 90,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 3,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    -- Solana (SOL)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'SOLUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'SOL', 'USDT', 'Solana', 'Solana', 'High-performance blockchain with fast transaction speeds',
        TRUE, TRUE, TRUE, TRUE,
        85, 2, 6,
        4, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 85,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 4,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    -- Avalanche (AVAX)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'AVAXUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'AVAX', 'USDT', 'Avalanche', 'Avalanche', 'Layer-1 blockchain platform for decentralized applications',
        TRUE, TRUE, TRUE, TRUE,
        80, 2, 6,
        5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 80,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 5,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    -- Sui (SUI)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'SUIUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'SUI', 'USDT', 'Sui', 'Sui', 'Next-generation layer-1 blockchain from Mysten Labs',
        TRUE, TRUE, TRUE, TRUE,
        75, 4, 6,
        6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 75,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 6,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    -- Ethena (ENA)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'ENAUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'ENA', 'USDT', 'Ethena', 'Ethena', 'Synthetic dollar protocol built on Ethereum',
        TRUE, TRUE, TRUE, TRUE,
        70, 4, 6,
        7, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 70,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 7,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    -- Uniswap (UNI)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'UNIUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'UNI', 'USDT', 'Uniswap', 'Uniswap', 'Leading decentralized exchange protocol on Ethereum',
        TRUE, TRUE, TRUE, TRUE,
        65, 4, 6,
        8, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 65,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 8,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    -- Binance Coin (BNB)
    INSERT INTO symbols (
        id, ticker, venue, asset_class, asset_class_id, market_id, data_provider_id,
        base_currency, quote_currency, full_name, display, description,
        is_active, is_tracked, is_popular, is_default_symbol,
        broadcast_priority, price_precision, quantity_precision,
        display_order, created_at, updated_at
    )
    VALUES (
        gen_random_uuid(), 'BNBUSDT', 'BINANCE', 'CRYPTO', v_asset_class_id, v_market_id, v_provider_id,
        'BNB', 'USDT', 'Binance Coin', 'BNB', 'Native cryptocurrency of Binance exchange and BNB Chain',
        TRUE, TRUE, TRUE, TRUE,
        60, 2, 6,
        9, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (ticker, venue) DO UPDATE SET
        is_default_symbol = TRUE,
        broadcast_priority = 60,
        is_active = TRUE,
        is_tracked = TRUE,
        display_order = 9,
        data_provider_id = EXCLUDED.data_provider_id,
        updated_at = CURRENT_TIMESTAMP;

    RAISE NOTICE 'Default symbols population complete: 9 symbols configured';
END $$;

-- =====================================================================
-- SECTION 5: DEACTIVATE OLD/DEPRECATED SYMBOLS
-- =====================================================================

-- Deactivate symbols that are no longer in default list
UPDATE symbols
SET
    is_active = FALSE,
    is_tracked = FALSE,
    is_default_symbol = FALSE,
    broadcast_priority = 0,
    updated_at = CURRENT_TIMESTAMP
WHERE ticker IN (
    'ADAUSDT',   -- Cardano
    'MATICUSDT', -- Polygon
    'DOTUSDT',   -- Polkadot
    'LINKUSDT',  -- Chainlink
    'LTCUSDT'    -- Litecoin
)
AND venue = 'BINANCE'
AND is_active = TRUE;  -- Only deactivate if currently active

-- =====================================================================
-- SECTION 6: VERIFICATION
-- =====================================================================

DO $$
DECLARE
    default_count INT;
    active_count INT;
    provider_count INT;
BEGIN
    -- Count default symbols
    SELECT COUNT(*) INTO default_count
    FROM symbols
    WHERE is_default_symbol = TRUE AND is_active = TRUE;

    -- Count active symbols
    SELECT COUNT(*) INTO active_count
    FROM symbols
    WHERE is_active = TRUE;

    -- Count data providers
    SELECT COUNT(*) INTO provider_count
    FROM data_providers
    WHERE is_active = TRUE;

    RAISE NOTICE '========================================';
    RAISE NOTICE 'Default Data Population Complete';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Default symbols configured: %', default_count;
    RAISE NOTICE 'Total active symbols: %', active_count;
    RAISE NOTICE 'Active data providers: %', provider_count;
    RAISE NOTICE '========================================';

    IF default_count != 9 THEN
        RAISE WARNING 'Expected 9 default symbols, found %', default_count;
    END IF;

    IF provider_count = 0 THEN
        RAISE WARNING 'No active data providers found';
    END IF;
END $$;

-- =====================================================================
-- POPULATION COMPLETE
-- =====================================================================
