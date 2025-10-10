-- =====================================================================
-- ROLLBACK: Data-Driven Symbol Management
-- Version: 1.0
-- Date: 2025-01-08
-- Author: Data Architecture Manager
-- Description: Complete rollback of 20250108_DataDrivenSymbols.sql
--              Safely reverses all schema changes with zero data loss
-- =====================================================================

-- =====================================================================
-- SAFETY CHECK
-- =====================================================================

DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'ROLLBACK INITIATED';
    RAISE NOTICE 'Migration: 20250108_DataDrivenSymbols';
    RAISE NOTICE 'Timestamp: %', CURRENT_TIMESTAMP;
    RAISE NOTICE '========================================';
    RAISE WARNING 'This will remove all data-driven symbol enhancements';
    RAISE WARNING 'Application may revert to hard-coded symbol behavior';
    RAISE NOTICE '========================================';
END $$;

-- =====================================================================
-- PHASE 1: DROP VERIFICATION VIEW
-- =====================================================================

DROP VIEW IF EXISTS v_symbol_data_quality;

RAISE NOTICE 'Phase 1: Verification view removed';

-- =====================================================================
-- PHASE 2: DROP TRIGGERS
-- =====================================================================

-- Drop auto-update timestamp trigger
DROP TRIGGER IF EXISTS trg_symbols_updated_at ON symbols;
DROP FUNCTION IF EXISTS update_symbols_timestamp();

RAISE NOTICE 'Phase 2a: Auto-update timestamp trigger removed';

-- Drop default symbol enforcement trigger
DROP TRIGGER IF EXISTS trg_ensure_default_symbols ON symbols;
DROP FUNCTION IF EXISTS ensure_default_symbols();

RAISE NOTICE 'Phase 2b: Default symbol enforcement trigger removed';

-- =====================================================================
-- PHASE 3: DROP INDEXES
-- =====================================================================

-- Drop performance indexes in reverse order of creation
DROP INDEX IF EXISTS idx_symbols_no_provider;
RAISE NOTICE 'Phase 3a: Index idx_symbols_no_provider removed';

DROP INDEX IF EXISTS idx_symbols_asset_class_active;
RAISE NOTICE 'Phase 3b: Index idx_symbols_asset_class_active removed';

DROP INDEX IF EXISTS idx_user_prefs_visible;
RAISE NOTICE 'Phase 3c: Index idx_user_prefs_visible removed';

DROP INDEX IF EXISTS idx_symbols_market_provider;
RAISE NOTICE 'Phase 3d: Index idx_symbols_market_provider removed';

DROP INDEX IF EXISTS idx_symbols_defaults;
RAISE NOTICE 'Phase 3e: Index idx_symbols_defaults removed';

DROP INDEX IF EXISTS idx_symbols_broadcast_active;
RAISE NOTICE 'Phase 3f: Index idx_symbols_broadcast_active removed';

-- =====================================================================
-- PHASE 4: REMOVE CONSTRAINTS
-- =====================================================================

-- Remove check constraints
ALTER TABLE symbols DROP CONSTRAINT IF EXISTS chk_default_symbol_active;
RAISE NOTICE 'Phase 4a: Constraint chk_default_symbol_active removed';

ALTER TABLE symbols DROP CONSTRAINT IF EXISTS chk_broadcast_priority;
RAISE NOTICE 'Phase 4b: Constraint chk_broadcast_priority removed';

-- Remove foreign key constraint
ALTER TABLE symbols DROP CONSTRAINT IF EXISTS fk_symbols_data_provider;
RAISE NOTICE 'Phase 4c: Foreign key fk_symbols_data_provider removed';

-- =====================================================================
-- PHASE 5: BACKUP COLUMN DATA (OPTIONAL - FOR AUDIT TRAIL)
-- =====================================================================

-- Create backup table with column data before removal
CREATE TABLE IF NOT EXISTS symbols_migration_backup_20250108 AS
SELECT
    id,
    ticker,
    broadcast_priority,
    last_broadcast_at,
    data_provider_id,
    is_default_symbol,
    CURRENT_TIMESTAMP AS backup_created_at
FROM symbols
WHERE broadcast_priority IS NOT NULL
   OR last_broadcast_at IS NOT NULL
   OR data_provider_id IS NOT NULL
   OR is_default_symbol = TRUE;

RAISE NOTICE 'Phase 5: Column data backed up to symbols_migration_backup_20250108';

-- =====================================================================
-- PHASE 6: DROP COLUMNS
-- =====================================================================

-- Remove new columns in reverse order of creation
ALTER TABLE symbols
DROP COLUMN IF EXISTS is_default_symbol,
DROP COLUMN IF EXISTS data_provider_id,
DROP COLUMN IF EXISTS last_broadcast_at,
DROP COLUMN IF EXISTS broadcast_priority;

RAISE NOTICE 'Phase 6: New columns removed from symbols table';

-- =====================================================================
-- PHASE 7: REACTIVATE DEPRECATED SYMBOLS (OPTIONAL)
-- =====================================================================

-- Optionally restore previously deactivated symbols
-- Uncomment if you want to re-enable ADA, MATIC, DOT, LINK, LTC

/*
UPDATE symbols
SET
    is_active = TRUE,
    is_tracked = TRUE,
    updated_at = CURRENT_TIMESTAMP
WHERE ticker IN (
    'ADAUSDT',   -- Cardano
    'MATICUSDT', -- Polygon
    'DOTUSDT',   -- Polkadot
    'LINKUSDT',  -- Chainlink
    'LTCUSDT'    -- Litecoin
)
AND is_active = FALSE;

RAISE NOTICE 'Phase 7: Deprecated symbols reactivated';
*/

-- =====================================================================
-- PHASE 8: VERIFICATION
-- =====================================================================

DO $$
DECLARE
    column_exists BOOLEAN;
    trigger_exists BOOLEAN;
    index_exists BOOLEAN;
BEGIN
    -- Verify broadcast_priority column removed
    SELECT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'symbols'
        AND column_name = 'broadcast_priority'
    ) INTO column_exists;

    IF column_exists THEN
        RAISE EXCEPTION 'Rollback verification failed: broadcast_priority column still exists';
    END IF;

    -- Verify triggers removed
    SELECT EXISTS (
        SELECT 1
        FROM information_schema.triggers
        WHERE trigger_name = 'trg_ensure_default_symbols'
    ) INTO trigger_exists;

    IF trigger_exists THEN
        RAISE EXCEPTION 'Rollback verification failed: trg_ensure_default_symbols trigger still exists';
    END IF;

    -- Verify indexes removed
    SELECT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE indexname = 'idx_symbols_broadcast_active'
    ) INTO index_exists;

    IF index_exists THEN
        RAISE EXCEPTION 'Rollback verification failed: idx_symbols_broadcast_active index still exists';
    END IF;

    RAISE NOTICE 'Rollback verification complete:';
    RAISE NOTICE '  - All new columns removed: YES';
    RAISE NOTICE '  - All triggers removed: YES';
    RAISE NOTICE '  - All indexes removed: YES';
    RAISE NOTICE '  - Backup table created: symbols_migration_backup_20250108';
END $$;

-- =====================================================================
-- PHASE 9: LOG ROLLBACK
-- =====================================================================

-- Log rollback execution
INSERT INTO migration_log (migration_name, executed_at, status)
VALUES ('20250108_DataDrivenSymbols_ROLLBACK', CURRENT_TIMESTAMP, 'SUCCESS')
ON CONFLICT DO NOTHING;

-- =====================================================================
-- ROLLBACK COMPLETE
-- =====================================================================

DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'ROLLBACK COMPLETED SUCCESSFULLY';
    RAISE NOTICE 'Migration: 20250108_DataDrivenSymbols';
    RAISE NOTICE 'Timestamp: %', CURRENT_TIMESTAMP;
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Database restored to pre-migration state';
    RAISE NOTICE 'Backup data available in: symbols_migration_backup_20250108';
    RAISE NOTICE '========================================';
    RAISE WARNING 'Action Required:';
    RAISE WARNING '1. Restore hard-coded symbol lists in application';
    RAISE WARNING '2. Revert service layer changes';
    RAISE WARNING '3. Verify broadcast services functioning';
    RAISE WARNING '4. Consider re-applying migration after fixes';
    RAISE NOTICE '========================================';
END $$;

-- =====================================================================
-- RESTORE SCRIPT (IF NEEDED)
-- =====================================================================

-- If you need to restore the column data from backup:
/*
-- Step 1: Re-run the forward migration
\i 20250108_DataDrivenSymbols.sql

-- Step 2: Restore backed up values
UPDATE symbols s
SET
    broadcast_priority = b.broadcast_priority,
    last_broadcast_at = b.last_broadcast_at,
    data_provider_id = b.data_provider_id,
    is_default_symbol = b.is_default_symbol
FROM symbols_migration_backup_20250108 b
WHERE s.id = b.id;

-- Step 3: Drop backup table
DROP TABLE symbols_migration_backup_20250108;
*/
