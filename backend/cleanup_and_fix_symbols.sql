-- 1. First, let's see what we have (for reference)
SELECT ticker, asset_class, venue, asset_class_id, market_id, created_at
FROM symbols
WHERE ticker IN ('AAPL', 'MSFT', 'GOOGL', 'NVDA', 'JPM', 'THYAO', 'GARAN', 'AKBNK', 'ISCTR', 'TUPRS', 'BAC', 'V', 'WMT', 'JNJ', 'AMZN')
ORDER BY ticker, created_at;

-- 2. Get asset_class IDs we'll need
SELECT "Id", code FROM asset_classes WHERE code IN ('STOCK', 'STOCK_BIST', 'STOCK_NASDAQ', 'STOCK_NYSE');

-- 3. Delete duplicate entries (keep only asset_class='STOCK' with asset_class_id populated)
-- Delete STOCK_BIST duplicates (keep only properly configured STOCK entries)
DELETE FROM symbols
WHERE ticker IN ('THYAO', 'GARAN', 'AKBNK', 'ISCTR', 'TUPRS')
  AND asset_class = 'STOCK_BIST'
  AND asset_class_id IS NULL;

-- Delete STOCK_NASDAQ duplicates (keep only properly configured STOCK entries)
DELETE FROM symbols
WHERE ticker IN ('AAPL', 'MSFT', 'GOOGL', 'NVDA', 'AMZN')
  AND asset_class = 'STOCK_NASDAQ'
  AND asset_class_id IS NULL;

-- Delete STOCK_NYSE duplicates (keep only properly configured STOCK entries)
DELETE FROM symbols
WHERE ticker IN ('JPM', 'BAC', 'V', 'WMT', 'JNJ')
  AND asset_class = 'STOCK_NYSE'
  AND asset_class_id IS NULL;

-- 4. Update BIST symbols to remove .IS suffix from ticker
UPDATE symbols
SET ticker = REPLACE(ticker, '.IS', '')
WHERE ticker LIKE '%.IS'
  AND asset_class = 'STOCK'
  AND venue = 'BIST';

-- 5. After removing .IS, check for any duplicates and keep the one with asset_class_id
-- Delete any BIST symbols without asset_class_id if there's a duplicate with asset_class_id
DELETE FROM symbols s1
WHERE s1.venue = 'BIST'
  AND s1.asset_class_id IS NULL
  AND EXISTS (
    SELECT 1 FROM symbols s2
    WHERE s2.ticker = s1.ticker
      AND s2.venue = 'BIST'
      AND s2.asset_class_id IS NOT NULL
  );

-- 6. Get the STOCK asset_class ID
DO $$
DECLARE
    stock_asset_class_id uuid;
BEGIN
    SELECT "Id" INTO stock_asset_class_id FROM asset_classes WHERE code = 'STOCK' LIMIT 1;

    -- Update all STOCK symbols to have the correct asset_class_id if missing
    UPDATE symbols
    SET asset_class_id = stock_asset_class_id,
        updated_at = NOW()
    WHERE asset_class = 'STOCK'
      AND asset_class_id IS NULL;
END $$;

-- 7. Verify the cleanup
SELECT
    s.ticker,
    s.asset_class,
    s.venue,
    s.asset_class_id IS NOT NULL as has_asset_class_id,
    m.code as market_code,
    ac.code as asset_class_code,
    s.is_active,
    s.is_tracked
FROM symbols s
LEFT JOIN markets m ON s.market_id = m."Id"
LEFT JOIN asset_classes ac ON s.asset_class_id = ac."Id"
WHERE s.asset_class = 'STOCK'
  AND s.is_active = true
ORDER BY s.venue, s.ticker;
