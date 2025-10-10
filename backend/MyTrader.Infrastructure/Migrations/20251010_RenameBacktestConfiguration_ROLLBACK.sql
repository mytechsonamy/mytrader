-- =====================================================================
-- Rollback: Rename backtest_configuration back to BacktestConfiguration
-- Version: 1.0
-- Date: 2025-10-10
-- =====================================================================

BEGIN;

-- Rename the table back
ALTER TABLE backtest_configuration RENAME TO "BacktestConfiguration";

-- Rename the primary key constraint back
ALTER INDEX pk_backtest_configuration RENAME TO "PK_BacktestConfiguration";

-- Verify
DO $$
DECLARE
    table_exists BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT FROM pg_tables
        WHERE schemaname = 'public'
        AND tablename = 'BacktestConfiguration'
    ) INTO table_exists;

    IF NOT table_exists THEN
        RAISE EXCEPTION 'Rollback failed: BacktestConfiguration table does not exist';
    END IF;

    RAISE NOTICE '✓ Rollback successful: backtest_configuration renamed back to BacktestConfiguration';
END $$;

COMMIT;

SELECT 'Rollback complete: backtest_configuration renamed back to BacktestConfiguration' AS status;
