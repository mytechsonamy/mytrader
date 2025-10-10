-- =====================================================================
-- Rollback: Rename user_device back to UserDevice
-- Version: 1.0
-- Date: 2025-10-10
-- =====================================================================

BEGIN;

-- Rename the table back
ALTER TABLE user_device RENAME TO "UserDevice";

-- Rename the primary key constraint back
ALTER INDEX pk_user_device RENAME TO "PK_UserDevice";

-- Rename the index back
ALTER INDEX ix_user_device_user_id RENAME TO "IX_UserDevice_UserId";

-- Verify
DO $$
DECLARE
    table_exists BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT FROM pg_tables
        WHERE schemaname = 'public'
        AND tablename = 'UserDevice'
    ) INTO table_exists;

    IF NOT table_exists THEN
        RAISE EXCEPTION 'Rollback failed: UserDevice table does not exist';
    END IF;

    RAISE NOTICE '✓ Rollback successful: user_device renamed back to UserDevice';
END $$;

COMMIT;

SELECT 'Rollback complete: user_device renamed back to UserDevice' AS status;
