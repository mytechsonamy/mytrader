-- =====================================================================
-- Migration: Rename UserDevice to user_device
-- Version: 1.0
-- Date: 2025-10-10
-- Description: Rename UserDevice table to follow lowercase naming convention
-- =====================================================================

BEGIN;

-- Rename the table
ALTER TABLE "UserDevice" RENAME TO user_device;

-- Rename the primary key constraint
ALTER INDEX "PK_UserDevice" RENAME TO pk_user_device;

-- Rename the index
ALTER INDEX "IX_UserDevice_UserId" RENAME TO ix_user_device_user_id;

-- Verify
DO $$
DECLARE
    table_exists BOOLEAN;
    old_table_exists BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT FROM pg_tables
        WHERE schemaname = 'public'
        AND tablename = 'user_device'
    ) INTO table_exists;

    SELECT EXISTS (
        SELECT FROM pg_tables
        WHERE schemaname = 'public'
        AND tablename = 'UserDevice'
    ) INTO old_table_exists;

    IF NOT table_exists OR old_table_exists THEN
        RAISE EXCEPTION 'Migration failed: Table rename verification failed';
    END IF;

    RAISE NOTICE '✓ UserDevice successfully renamed to user_device';
END $$;

COMMIT;

SELECT 'UserDevice renamed to user_device' AS status;
