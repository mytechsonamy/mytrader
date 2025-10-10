-- =====================================================================
-- ROLLBACK: Add Trading Hours and Status Columns to markets Table
-- Version: 1.0
-- Date: 2025-10-10
-- Description: Rollback migration that added trading hours and status columns
-- =====================================================================

BEGIN;

RAISE NOTICE 'Starting rollback of market trading hours and status migration...';

-- =====================================================================
-- PHASE 1: Drop View and Function
-- =====================================================================

DROP VIEW IF EXISTS vw_market_status CASCADE;
DROP FUNCTION IF EXISTS update_market_status() CASCADE;

RAISE NOTICE 'Phase 1 Complete: View and function dropped';

-- =====================================================================
-- PHASE 2: Drop Indexes
-- =====================================================================

DROP INDEX IF EXISTS idx_markets_current_status;
DROP INDEX IF EXISTS idx_markets_next_open_time;
DROP INDEX IF EXISTS idx_markets_enable_data_fetching;

RAISE NOTICE 'Phase 2 Complete: Indexes dropped';

-- =====================================================================
-- PHASE 3: Remove Added Columns
-- =====================================================================

ALTER TABLE markets
DROP COLUMN IF EXISTS regular_market_open,
DROP COLUMN IF EXISTS regular_market_close,
DROP COLUMN IF EXISTS pre_market_open,
DROP COLUMN IF EXISTS pre_market_close,
DROP COLUMN IF EXISTS post_market_open,
DROP COLUMN IF EXISTS post_market_close,
DROP COLUMN IF EXISTS trading_days,
DROP COLUMN IF EXISTS current_status,
DROP COLUMN IF EXISTS next_open_time,
DROP COLUMN IF EXISTS next_close_time,
DROP COLUMN IF EXISTS status_last_updated,
DROP COLUMN IF EXISTS is_holiday_today,
DROP COLUMN IF EXISTS holiday_name,
DROP COLUMN IF EXISTS enable_data_fetching,
DROP COLUMN IF EXISTS data_fetch_interval,
DROP COLUMN IF EXISTS data_fetch_interval_closed;

RAISE NOTICE 'Phase 3 Complete: Columns removed from markets table';

-- =====================================================================
-- PHASE 4: Remove CRYPTO market if it was added by this migration
-- =====================================================================

DELETE FROM markets
WHERE code = 'CRYPTO'
  AND created_at >= '2025-10-10'::DATE;

RAISE NOTICE 'Phase 4 Complete: CRYPTO market removed (if added by migration)';

-- =====================================================================
-- VERIFICATION
-- =====================================================================

DO $$
DECLARE
    columns_exist BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'markets'
          AND column_name = 'current_status'
    ) INTO columns_exist;

    RAISE NOTICE '=== Rollback Verification ===';
    RAISE NOTICE 'Status columns still exist: %', columns_exist;
    RAISE NOTICE '============================';

    IF columns_exist THEN
        RAISE EXCEPTION 'Rollback failed: Status columns still exist';
    END IF;
END $$;

COMMIT;

-- =====================================================================
-- ROLLBACK COMPLETE
-- =====================================================================

RAISE NOTICE '========================================';
RAISE NOTICE 'Rollback Complete';
RAISE NOTICE 'Market trading hours and status columns removed';
RAISE NOTICE '========================================';

SELECT
    'Rollback completed successfully' AS status,
    COUNT(*) AS total_markets_remaining
FROM markets
WHERE is_active = TRUE;
