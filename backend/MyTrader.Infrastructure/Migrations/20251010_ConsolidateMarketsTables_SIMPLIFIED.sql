-- =====================================================================
-- Migration: Simplified Markets Table Consolidation
-- Version: 2.0 (Simplified)
-- Date: 2025-10-10
-- Description: Drop duplicate "Markets" and "MarketHolidays" tables
--              The lowercase 'markets' table already contains the correct data
-- =====================================================================

BEGIN;

-- =====================================================================
-- PHASE 1: Verify markets table has required data
-- =====================================================================

DO $$
DECLARE
    markets_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO markets_count FROM markets WHERE is_active = TRUE;

    IF markets_count < 3 THEN
        RAISE EXCEPTION 'Cannot proceed: markets table only has % active markets, expected at least 3', markets_count;
    END IF;

    RAISE NOTICE 'Verification passed: Found % active markets in lowercase table', markets_count;
END $$;

-- =====================================================================
-- PHASE 2: Drop duplicate capital letter tables
-- =====================================================================

-- Drop MarketHolidays first (has foreign key to Markets)
DROP TABLE IF EXISTS "MarketHolidays" CASCADE;

-- Drop Markets table
DROP TABLE IF EXISTS "Markets" CASCADE;

DO $$ BEGIN RAISE NOTICE 'Duplicate tables dropped successfully'; END $$;

-- =====================================================================
-- PHASE 3: Verify cleanup
-- =====================================================================

DO $$
DECLARE
    capital_markets_exists BOOLEAN;
    capital_holidays_exists BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT FROM pg_tables
        WHERE schemaname = 'public'
        AND tablename = 'Markets'
    ) INTO capital_markets_exists;

    SELECT EXISTS (
        SELECT FROM pg_tables
        WHERE schemaname = 'public'
        AND tablename = 'MarketHolidays'
    ) INTO capital_holidays_exists;

    IF capital_markets_exists OR capital_holidays_exists THEN
        RAISE EXCEPTION 'Cleanup failed: Capital letter tables still exist';
    END IF;

    RAISE NOTICE '✓ Verification passed: All duplicate tables removed';
END $$;

COMMIT;

-- =====================================================================
-- MIGRATION COMPLETE
-- =====================================================================

SELECT
    'Migration 20251010_ConsolidateMarketsTables_SIMPLIFIED completed successfully' AS status,
    COUNT(*) AS total_markets_remaining
FROM markets;
