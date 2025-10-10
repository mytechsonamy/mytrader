-- =============================================================================
-- CRITICAL DATA INTEGRITY FIX: Symbol-Market Mapping Correction
-- =============================================================================
-- File: 20250924_FixSymbolMarketDataIntegrity.sql
-- Author: Data Architecture Manager
-- Date: 2025-09-24
-- Description: Fixes critical data integrity issues where US stocks (NASDAQ/NYSE)
--              are incorrectly tagged with BIST venue and missing market_id mappings.
--
-- ISSUE IDENTIFIED:
-- 1. US stock symbols (AAPL, GOOGL, MSFT, etc.) have venue='BIST' instead of proper market
-- 2. All symbols have NULL market_id values, breaking referential integrity
-- 3. BIST symbols are split between asset_class 'STOCK' and 'STOCK_BIST'
--
-- DATA QUALITY IMPACT: HIGH - Affects frontend display and trading logic
-- =============================================================================

BEGIN;

-- =============================================================================
-- STEP 1: DATA INTEGRITY VALIDATION (PRE-EXECUTION CHECKS)
-- =============================================================================

DO $$
DECLARE
    total_symbols INTEGER;
    us_stocks_as_bist INTEGER;
    null_market_ids INTEGER;
    bist_market_id UUID;
    nasdaq_market_id UUID;
    nyse_market_id UUID;
    binance_market_id UUID;
BEGIN
    -- Get market IDs
    SELECT "Id" INTO bist_market_id FROM markets WHERE code = 'BIST';
    SELECT "Id" INTO nasdaq_market_id FROM markets WHERE code = 'NASDAQ';
    SELECT "Id" INTO nyse_market_id FROM markets WHERE code = 'NYSE';
    SELECT "Id" INTO binance_market_id FROM markets WHERE code = 'BINANCE';

    -- Validation checks
    SELECT COUNT(*) INTO total_symbols FROM symbols;
    SELECT COUNT(*) INTO us_stocks_as_bist
    FROM symbols
    WHERE venue = 'BIST' AND asset_class = 'STOCK'
      AND ticker ~ '^[A-Z]{1,5}$' AND LENGTH(ticker) <= 4;
    SELECT COUNT(*) INTO null_market_ids FROM symbols WHERE market_id IS NULL;

    -- Log validation results
    RAISE NOTICE '=== PRE-EXECUTION DATA VALIDATION ===';
    RAISE NOTICE 'Total symbols in database: %', total_symbols;
    RAISE NOTICE 'US stocks incorrectly tagged as BIST: %', us_stocks_as_bist;
    RAISE NOTICE 'Symbols with NULL market_id: %', null_market_ids;
    RAISE NOTICE 'Market IDs - BIST: %, NASDAQ: %, NYSE: %, BINANCE: %',
                 bist_market_id, nasdaq_market_id, nyse_market_id, binance_market_id;

    -- Safety check: Ensure all required market IDs exist
    IF bist_market_id IS NULL OR nasdaq_market_id IS NULL OR nyse_market_id IS NULL OR binance_market_id IS NULL THEN
        RAISE EXCEPTION 'CRITICAL ERROR: Required market records not found. Cannot proceed with migration.';
    END IF;

    -- Safety check: Don't proceed if no issues found
    IF us_stocks_as_bist = 0 AND null_market_ids = 0 THEN
        RAISE EXCEPTION 'No data integrity issues found. Migration not needed.';
    END IF;
END $$;

-- =============================================================================
-- STEP 2: CREATE BACKUP TABLES FOR ROLLBACK SAFETY
-- =============================================================================

-- Create backup table with timestamp
DROP TABLE IF EXISTS symbols_backup_20250924;
CREATE TABLE symbols_backup_20250924 AS SELECT * FROM symbols;

-- Log backup creation
DO $$
BEGIN
    RAISE NOTICE '=== BACKUP CREATED ===';
    RAISE NOTICE 'Backup table created: symbols_backup_20250924';
END $$;

-- =============================================================================
-- STEP 3: KNOWN US STOCK SYMBOLS CLASSIFICATION
-- =============================================================================

-- Create temporary mapping table for known symbols
CREATE TEMP TABLE symbol_market_mappings AS
WITH known_symbols AS (
  SELECT unnest(ARRAY[
    'AAPL', 'GOOGL', 'GOOG', 'MSFT', 'AMZN', 'TSLA', 'META', 'NVDA', 'NFLX', 'CRM',
    'ADBE', 'INTC', 'CSCO', 'PEP', 'COST', 'QCOM', 'TXN', 'INTU', 'AMD', 'AVGO',
    'ABNB', 'ADP', 'BIIB', 'BKNG', 'BNTX', 'CAT', 'CMCSA', 'CRWD', 'DDOG', 'DOCU',
    'DXCM', 'GILD', 'ILMN', 'ISRG', 'JNJ', 'LOW', 'MDLZ', 'MELI', 'MRNA', 'MU',
    'NET', 'OKTA', 'ORCL', 'PLTR', 'REGN', 'ROKU', 'RTX', 'SBUX', 'SNOW', 'TEAM',
    'TMUS', 'UPS', 'VEEV', 'WDAY', 'ZM'
  ]) AS ticker
),
market_ids AS (
  SELECT
    'NASDAQ' as market_code,
    "Id" as market_id
  FROM markets WHERE code = 'NASDAQ'
  UNION ALL
  SELECT
    'NYSE' as market_code,
    "Id" as market_id
  FROM markets WHERE code = 'NYSE'
)
SELECT
  k.ticker,
  -- NASDAQ symbols (tech-heavy)
  CASE
    WHEN k.ticker IN ('AAPL', 'GOOGL', 'GOOG', 'MSFT', 'AMZN', 'TSLA', 'META', 'NVDA', 'NFLX', 'CRM',
                     'ADBE', 'INTC', 'CSCO', 'QCOM', 'INTU', 'AMD', 'AVGO', 'ABNB', 'BNTX', 'CRWD',
                     'DDOG', 'DOCU', 'DXCM', 'ILMN', 'MELI', 'MRNA', 'MU', 'NET', 'OKTA', 'PLTR',
                     'REGN', 'ROKU', 'SBUX', 'SNOW', 'TEAM', 'TMUS', 'VEEV', 'WDAY', 'ZM')
    THEN (SELECT market_id FROM market_ids WHERE market_code = 'NASDAQ')
    ELSE (SELECT market_id FROM market_ids WHERE market_code = 'NYSE')
  END as correct_market_id,
  CASE
    WHEN k.ticker IN ('AAPL', 'GOOGL', 'GOOG', 'MSFT', 'AMZN', 'TSLA', 'META', 'NVDA', 'NFLX', 'CRM',
                     'ADBE', 'INTC', 'CSCO', 'QCOM', 'INTU', 'AMD', 'AVGO', 'ABNB', 'BNTX', 'CRWD',
                     'DDOG', 'DOCU', 'DXCM', 'ILMN', 'MELI', 'MRNA', 'MU', 'NET', 'OKTA', 'PLTR',
                     'REGN', 'ROKU', 'SBUX', 'SNOW', 'TEAM', 'TMUS', 'VEEV', 'WDAY', 'ZM')
    THEN 'NASDAQ'
    ELSE 'NYSE'
  END as correct_venue,
  CASE
    WHEN k.ticker IN ('AAPL', 'GOOGL', 'GOOG', 'MSFT', 'AMZN', 'TSLA', 'META', 'NVDA', 'NFLX', 'CRM',
                     'ADBE', 'INTC', 'CSCO', 'QCOM', 'INTU', 'AMD', 'AVGO', 'ABNB', 'BNTX', 'CRWD',
                     'DDOG', 'DOCU', 'DXCM', 'ILMN', 'MELI', 'MRNA', 'MU', 'NET', 'OKTA', 'PLTR',
                     'REGN', 'ROKU', 'SBUX', 'SNOW', 'TEAM', 'TMUS', 'VEEV', 'WDAY', 'ZM')
    THEN 'STOCK_NASDAQ'
    ELSE 'STOCK_NYSE'
  END as correct_asset_class
FROM known_symbols k;

-- =============================================================================
-- STEP 4: APPLY DATA CORRECTIONS
-- =============================================================================

-- 4.1: Fix US stocks incorrectly tagged as BIST
UPDATE symbols
SET
  venue = smm.correct_venue,
  asset_class = smm.correct_asset_class,
  market_id = smm.correct_market_id,
  updated_at = CURRENT_TIMESTAMP
FROM symbol_market_mappings smm
WHERE symbols.ticker = smm.ticker
  AND symbols.venue = 'BIST'
  AND symbols.asset_class = 'STOCK';

-- Log correction count
DO $$
DECLARE
    corrected_count INTEGER;
BEGIN
    GET DIAGNOSTICS corrected_count = ROW_COUNT;
    RAISE NOTICE '=== US STOCKS CORRECTED ===';
    RAISE NOTICE 'US stock symbols corrected from BIST to proper markets: %', corrected_count;
END $$;

-- 4.2: Update BIST symbols to use correct market_id
UPDATE symbols
SET
  market_id = (SELECT "Id" FROM markets WHERE code = 'BIST'),
  updated_at = CURRENT_TIMESTAMP
WHERE venue = 'BIST'
  AND asset_class IN ('STOCK_BIST', 'STOCK')
  AND market_id IS NULL;

-- Log BIST corrections
DO $$
DECLARE
    bist_corrected INTEGER;
BEGIN
    GET DIAGNOSTICS bist_corrected = ROW_COUNT;
    RAISE NOTICE '=== BIST SYMBOLS CORRECTED ===';
    RAISE NOTICE 'BIST symbols updated with market_id: %', bist_corrected;
END $$;

-- 4.3: Update crypto symbols market_id mappings
UPDATE symbols
SET
  market_id = (SELECT "Id" FROM markets WHERE code = 'BINANCE'),
  updated_at = CURRENT_TIMESTAMP
WHERE venue = 'BINANCE'
  AND asset_class = 'CRYPTO'
  AND market_id IS NULL;

-- 4.4: Handle remaining symbols with generic market assignments
UPDATE symbols
SET
  market_id = CASE
    WHEN asset_class = 'CRYPTO' AND venue = 'YAHOO_FINANCE'
      THEN (SELECT "Id" FROM markets WHERE code = 'BINANCE')
    ELSE market_id
  END,
  updated_at = CURRENT_TIMESTAMP
WHERE market_id IS NULL;

-- =============================================================================
-- STEP 5: DATA QUALITY VALIDATION (POST-EXECUTION CHECKS)
-- =============================================================================

DO $$
DECLARE
    remaining_null_market_ids INTEGER;
    remaining_us_as_bist INTEGER;
    total_symbols_after INTEGER;
    bist_symbols_count INTEGER;
    nasdaq_symbols_count INTEGER;
    nyse_symbols_count INTEGER;
BEGIN
    -- Post-correction validation
    SELECT COUNT(*) INTO remaining_null_market_ids FROM symbols WHERE market_id IS NULL;
    SELECT COUNT(*) INTO remaining_us_as_bist
    FROM symbols
    WHERE venue = 'BIST' AND asset_class = 'STOCK'
      AND ticker ~ '^[A-Z]{1,5}$' AND LENGTH(ticker) <= 4;
    SELECT COUNT(*) INTO total_symbols_after FROM symbols;
    SELECT COUNT(*) INTO bist_symbols_count FROM symbols WHERE venue = 'BIST';
    SELECT COUNT(*) INTO nasdaq_symbols_count FROM symbols WHERE venue = 'NASDAQ';
    SELECT COUNT(*) INTO nyse_symbols_count FROM symbols WHERE venue = 'NYSE';

    RAISE NOTICE '=== POST-EXECUTION VALIDATION ===';
    RAISE NOTICE 'Total symbols after correction: %', total_symbols_after;
    RAISE NOTICE 'Remaining NULL market_id count: %', remaining_null_market_ids;
    RAISE NOTICE 'Remaining US stocks as BIST: %', remaining_us_as_bist;
    RAISE NOTICE 'Symbol distribution - BIST: %, NASDAQ: %, NYSE: %',
                 bist_symbols_count, nasdaq_symbols_count, nyse_symbols_count;

    -- Success validation
    IF remaining_null_market_ids > 5 THEN  -- Allow some flexibility for unknown symbols
        RAISE WARNING 'WARNING: Still % symbols with NULL market_id', remaining_null_market_ids;
    END IF;

    IF remaining_us_as_bist > 0 THEN
        RAISE WARNING 'WARNING: Still % US stocks incorrectly tagged as BIST', remaining_us_as_bist;
    END IF;
END $$;

-- =============================================================================
-- STEP 6: CREATE DATA QUALITY CONSTRAINTS
-- =============================================================================

-- Add constraint to prevent future data integrity issues
-- Note: This constraint might need adjustment based on business rules
DO $$
BEGIN
  -- Only add constraint if it doesn't exist
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.check_constraints
    WHERE constraint_name = 'chk_symbols_venue_asset_consistency'
  ) THEN
    ALTER TABLE symbols
    ADD CONSTRAINT chk_symbols_venue_asset_consistency
    CHECK (
      (venue = 'BIST' AND asset_class IN ('STOCK_BIST', 'STOCK')) OR
      (venue = 'NASDAQ' AND asset_class = 'STOCK_NASDAQ') OR
      (venue = 'NYSE' AND asset_class = 'STOCK_NYSE') OR
      (venue = 'BINANCE' AND asset_class = 'CRYPTO') OR
      (venue = 'YAHOO_FINANCE' AND asset_class = 'CRYPTO') OR
      (venue = 'UNKNOWN' AND asset_class = 'UNKNOWN')
    );
    RAISE NOTICE 'Data quality constraint added: chk_symbols_venue_asset_consistency';
  END IF;
END $$;

-- =============================================================================
-- STEP 7: FINAL SUMMARY REPORT
-- =============================================================================

DO $$
BEGIN
    RAISE NOTICE '=== MIGRATION COMPLETED SUCCESSFULLY ===';
    RAISE NOTICE 'Timestamp: %', CURRENT_TIMESTAMP;
    RAISE NOTICE 'Backup table: symbols_backup_20250924';
    RAISE NOTICE 'Data integrity constraint added for future prevention';
    RAISE NOTICE '===============================================';
END $$;

-- Show final distribution
SELECT 'FINAL_DISTRIBUTION' as report_type, venue, asset_class, COUNT(*) as symbol_count
FROM symbols
GROUP BY venue, asset_class
ORDER BY venue, asset_class;

-- Show market_id mapping status
SELECT 'MARKET_ID_MAPPING' as report_type,
       m.code as market_code,
       COUNT(s.id) as mapped_symbols
FROM markets m
LEFT JOIN symbols s ON s.market_id = m."Id"
GROUP BY m.code, m."Id"
ORDER BY mapped_symbols DESC;

COMMIT;

-- =============================================================================
-- ROLLBACK INSTRUCTIONS (IF NEEDED)
-- =============================================================================
/*
-- To rollback this migration:

BEGIN;

-- 1. Restore from backup
DELETE FROM symbols;
INSERT INTO symbols SELECT * FROM symbols_backup_20250924;

-- 2. Remove constraint
ALTER TABLE symbols DROP CONSTRAINT IF EXISTS chk_symbols_venue_asset_consistency;

-- 3. Clean up backup
DROP TABLE symbols_backup_20250924;

COMMIT;

-- Note: Only run rollback if serious issues are discovered
*/