-- =====================================================================
-- Migration: Data-Driven Symbol Management
-- Version: 1.0
-- Date: 2025-01-08
-- Author: Data Architecture Manager
-- Description: Transition from hard-coded symbols to database-driven
--              symbol management with user preferences and broadcast control
-- =====================================================================

-- =====================================================================
-- PHASE 1: SCHEMA ENHANCEMENTS
-- =====================================================================

-- Add new columns to symbols table
-- These columns enable data-driven broadcast control and user defaults
ALTER TABLE symbols
ADD COLUMN IF NOT EXISTS broadcast_priority INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS last_broadcast_at TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS data_provider_id UUID,
ADD COLUMN IF NOT EXISTS is_default_symbol BOOLEAN DEFAULT FALSE;

-- Add foreign key constraint for data_provider_id
ALTER TABLE symbols
ADD CONSTRAINT fk_symbols_data_provider
FOREIGN KEY (data_provider_id)
REFERENCES data_providers(id)
ON DELETE SET NULL;

-- Add documentation comments
COMMENT ON COLUMN symbols.broadcast_priority IS 'Broadcasting priority (0-100): Higher values get more frequent updates';
COMMENT ON COLUMN symbols.last_broadcast_at IS 'Last WebSocket broadcast timestamp for rate limiting';
COMMENT ON COLUMN symbols.data_provider_id IS 'Primary data provider for this symbol (NULL = use market default provider)';
COMMENT ON COLUMN symbols.is_default_symbol IS 'System default symbol shown to new/anonymous users';

-- =====================================================================
-- PHASE 2: DATA INTEGRITY CONSTRAINTS
-- =====================================================================

-- Ensure broadcast priority is within valid range (0-100)
ALTER TABLE symbols
ADD CONSTRAINT chk_broadcast_priority
CHECK (broadcast_priority >= 0 AND broadcast_priority <= 100);

-- Ensure logical consistency: default symbols must be active
ALTER TABLE symbols
ADD CONSTRAINT chk_default_symbol_active
CHECK (
    (is_default_symbol = FALSE) OR
    (is_default_symbol = TRUE AND is_active = TRUE)
);

-- =====================================================================
-- PHASE 3: PERFORMANCE INDEXES
-- =====================================================================

-- Index 1: Broadcast queries (most critical - used every 100ms by WebSocket service)
-- Supports: WHERE is_active=TRUE AND is_tracked=TRUE ORDER BY broadcast_priority DESC
CREATE INDEX IF NOT EXISTS idx_symbols_broadcast_active
ON symbols(is_active, is_tracked, broadcast_priority DESC, last_broadcast_at)
WHERE is_active = TRUE AND is_tracked = TRUE;

-- Index 2: Default symbol queries (anonymous users)
-- Supports: WHERE is_default_symbol=TRUE AND is_active=TRUE ORDER BY display_order
CREATE INDEX IF NOT EXISTS idx_symbols_defaults
ON symbols(is_default_symbol, display_order)
WHERE is_default_symbol = TRUE AND is_active = TRUE;

-- Index 3: Market-provider relationship queries
-- Supports: JOINs on market_id and data_provider_id for routing
CREATE INDEX IF NOT EXISTS idx_symbols_market_provider
ON symbols(market_id, data_provider_id, is_active)
WHERE is_active = TRUE;

-- Index 4: User preference joins (existing table)
-- Supports: WHERE user_id=X AND is_visible=TRUE ORDER BY display_order
CREATE INDEX IF NOT EXISTS idx_user_prefs_visible
ON user_dashboard_preferences(user_id, is_visible, display_order)
WHERE is_visible = TRUE;

-- Index 5: Asset class filtering with popularity
-- Supports: WHERE asset_class_id=X AND is_active=TRUE ORDER BY is_popular DESC
CREATE INDEX IF NOT EXISTS idx_symbols_asset_class_active
ON symbols(asset_class_id, market_id, is_active, is_popular)
WHERE is_active = TRUE;

-- Index 6: Data provider assignment tracking
-- Supports: Queries to find symbols without provider assignment
CREATE INDEX IF NOT EXISTS idx_symbols_no_provider
ON symbols(market_id, is_active)
WHERE data_provider_id IS NULL AND is_active = TRUE;

-- =====================================================================
-- PHASE 4: DATA QUALITY TRIGGER
-- =====================================================================

-- Ensure at least one default symbol exists at all times
CREATE OR REPLACE FUNCTION ensure_default_symbols()
RETURNS TRIGGER AS $$
DECLARE
    default_count INT;
BEGIN
    -- Count active default symbols
    SELECT COUNT(*) INTO default_count
    FROM symbols
    WHERE is_default_symbol = TRUE
      AND is_active = TRUE;

    -- Prevent deletion/deactivation if this would leave zero defaults
    IF default_count = 0 THEN
        RAISE EXCEPTION 'Cannot remove last default symbol. At least one active default symbol must exist.';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Attach trigger to symbols table
DROP TRIGGER IF EXISTS trg_ensure_default_symbols ON symbols;
CREATE TRIGGER trg_ensure_default_symbols
AFTER UPDATE OR DELETE ON symbols
FOR EACH STATEMENT
EXECUTE FUNCTION ensure_default_symbols();

-- =====================================================================
-- PHASE 5: AUTO-UPDATE TRIGGER
-- =====================================================================

-- Auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_symbols_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_symbols_updated_at ON symbols;
CREATE TRIGGER trg_symbols_updated_at
BEFORE UPDATE ON symbols
FOR EACH ROW
EXECUTE FUNCTION update_symbols_timestamp();

-- =====================================================================
-- PHASE 6: LINK SYMBOLS TO DATA PROVIDERS
-- =====================================================================

-- Link Binance crypto symbols to BINANCE data provider
UPDATE symbols s
SET data_provider_id = dp.id
FROM data_providers dp
INNER JOIN markets m ON dp.market_id = m.id
WHERE s.market_id = m.id
  AND m.code = 'BINANCE'
  AND dp.code LIKE '%BINANCE%'
  AND dp.is_active = TRUE
  AND s.data_provider_id IS NULL;

-- Link BIST stocks to BIST data provider
UPDATE symbols s
SET data_provider_id = dp.id
FROM data_providers dp
INNER JOIN markets m ON dp.market_id = m.id
WHERE s.market_id = m.id
  AND m.code = 'BIST'
  AND dp.code LIKE '%BIST%'
  AND dp.is_active = TRUE
  AND s.data_provider_id IS NULL;

-- Link US stocks to Yahoo Finance provider
UPDATE symbols s
SET data_provider_id = dp.id
FROM data_providers dp
INNER JOIN markets m ON dp.market_id = m.id
WHERE s.market_id = m.id
  AND m.code IN ('NASDAQ', 'NYSE')
  AND dp.code LIKE '%YAHOO%'
  AND dp.is_active = TRUE
  AND s.data_provider_id IS NULL;

-- =====================================================================
-- PHASE 7: CONFIGURE DEFAULT SYMBOLS (9 PRIMARY CRYPTO ASSETS)
-- =====================================================================

-- Mark current active crypto symbols as default based on priority
UPDATE symbols
SET
    is_default_symbol = TRUE,
    broadcast_priority = CASE ticker
        WHEN 'BTCUSDT' THEN 100  -- Bitcoin: Highest priority
        WHEN 'ETHUSDT' THEN 95   -- Ethereum
        WHEN 'XRPUSDT' THEN 90   -- Ripple
        WHEN 'SOLUSDT' THEN 85   -- Solana
        WHEN 'AVAXUSDT' THEN 80  -- Avalanche
        WHEN 'SUIUSDT' THEN 75   -- Sui
        WHEN 'ENAUSDT' THEN 70   -- Ethena
        WHEN 'UNIUSDT' THEN 65   -- Uniswap
        WHEN 'BNBUSDT' THEN 60   -- Binance Coin
        ELSE broadcast_priority  -- Keep existing priority for others
    END,
    is_active = TRUE,
    is_tracked = TRUE,
    display_order = CASE ticker
        WHEN 'BTCUSDT' THEN 1
        WHEN 'ETHUSDT' THEN 2
        WHEN 'XRPUSDT' THEN 3
        WHEN 'SOLUSDT' THEN 4
        WHEN 'AVAXUSDT' THEN 5
        WHEN 'SUIUSDT' THEN 6
        WHEN 'ENAUSDT' THEN 7
        WHEN 'UNIUSDT' THEN 8
        WHEN 'BNBUSDT' THEN 9
        ELSE display_order
    END
WHERE ticker IN (
    'BTCUSDT',  -- Bitcoin
    'ETHUSDT',  -- Ethereum
    'XRPUSDT',  -- Ripple
    'SOLUSDT',  -- Solana
    'AVAXUSDT', -- Avalanche
    'SUIUSDT',  -- Sui
    'ENAUSDT',  -- Ethena
    'UNIUSDT',  -- Uniswap
    'BNBUSDT'   -- Binance Coin
);

-- =====================================================================
-- PHASE 8: DEACTIVATE OLD SYMBOLS
-- =====================================================================

-- Deactivate deprecated symbols (ADA, MATIC, DOT, LINK, LTC)
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
AND is_active = TRUE;  -- Only update if currently active

-- =====================================================================
-- PHASE 9: DATA QUALITY VERIFICATION
-- =====================================================================

-- Create verification view for monitoring
CREATE OR REPLACE VIEW v_symbol_data_quality AS
SELECT
    'Total Active Symbols' AS metric,
    COUNT(*)::TEXT AS value,
    'Should be > 0' AS expected
FROM symbols
WHERE is_active = TRUE

UNION ALL

SELECT
    'Default Symbols' AS metric,
    COUNT(*)::TEXT AS value,
    'Should be exactly 9' AS expected
FROM symbols
WHERE is_default_symbol = TRUE AND is_active = TRUE

UNION ALL

SELECT
    'Symbols Without Provider' AS metric,
    COUNT(*)::TEXT AS value,
    'Should be 0 or low' AS expected
FROM symbols
WHERE is_active = TRUE
  AND data_provider_id IS NULL

UNION ALL

SELECT
    'Tracked Symbols' AS metric,
    COUNT(*)::TEXT AS value,
    'Should match active symbols' AS expected
FROM symbols
WHERE is_tracked = TRUE

UNION ALL

SELECT
    'Broadcast Priority Range' AS metric,
    CONCAT(MIN(broadcast_priority), '-', MAX(broadcast_priority))::TEXT AS value,
    'Should be 0-100' AS expected
FROM symbols
WHERE is_active = TRUE

UNION ALL

SELECT
    'Orphaned User Preferences' AS metric,
    COUNT(*)::TEXT AS value,
    'Should be 0' AS expected
FROM user_dashboard_preferences udp
LEFT JOIN symbols s ON udp.symbol_id = s.id
WHERE s.id IS NULL;

-- Grant access to verification view
GRANT SELECT ON v_symbol_data_quality TO PUBLIC;

-- =====================================================================
-- PHASE 10: MIGRATION VERIFICATION
-- =====================================================================

DO $$
DECLARE
    default_count INT;
    active_count INT;
    provider_missing_count INT;
BEGIN
    -- Verify default symbols
    SELECT COUNT(*) INTO default_count
    FROM symbols
    WHERE is_default_symbol = TRUE AND is_active = TRUE;

    IF default_count != 9 THEN
        RAISE WARNING 'Expected 9 default symbols, found %', default_count;
    END IF;

    -- Verify active symbols
    SELECT COUNT(*) INTO active_count
    FROM symbols
    WHERE is_active = TRUE;

    IF active_count < 9 THEN
        RAISE EXCEPTION 'Critical: Less than 9 active symbols found';
    END IF;

    -- Check provider assignments
    SELECT COUNT(*) INTO provider_missing_count
    FROM symbols
    WHERE is_active = TRUE AND data_provider_id IS NULL;

    IF provider_missing_count > 0 THEN
        RAISE WARNING '% active symbols missing data provider assignment', provider_missing_count;
    END IF;

    RAISE NOTICE 'Migration verification complete:';
    RAISE NOTICE '  - Default symbols: %', default_count;
    RAISE NOTICE '  - Active symbols: %', active_count;
    RAISE NOTICE '  - Symbols without provider: %', provider_missing_count;
END $$;

-- =====================================================================
-- MIGRATION COMPLETE
-- =====================================================================

-- Log migration completion
INSERT INTO migration_log (migration_name, executed_at, status)
VALUES ('20250108_DataDrivenSymbols', CURRENT_TIMESTAMP, 'SUCCESS')
ON CONFLICT DO NOTHING;

-- Display completion message
DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Migration 20250108_DataDrivenSymbols.sql';
    RAISE NOTICE 'Status: COMPLETED SUCCESSFULLY';
    RAISE NOTICE 'Timestamp: %', CURRENT_TIMESTAMP;
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Next Steps:';
    RAISE NOTICE '1. Verify data with: SELECT * FROM v_symbol_data_quality;';
    RAISE NOTICE '2. Update BinanceWebSocketService to use database symbols';
    RAISE NOTICE '3. Update MultiAssetDataBroadcastService';
    RAISE NOTICE '4. Deploy frontend changes to consume dynamic symbols';
    RAISE NOTICE '========================================';
END $$;
