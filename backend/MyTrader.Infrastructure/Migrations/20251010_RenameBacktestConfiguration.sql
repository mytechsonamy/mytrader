-- =====================================================================
-- Migration: Rename BacktestConfiguration to backtest_configuration
-- Version: 1.0
-- Date: 2025-10-10
-- Description: Rename BacktestConfiguration table to follow lowercase naming convention
-- =====================================================================

BEGIN;

-- Rename the table
ALTER TABLE "BacktestConfiguration" RENAME TO backtest_configuration;

-- Rename the primary key constraint
ALTER INDEX "PK_BacktestConfiguration" RENAME TO pk_backtest_configuration;

-- Verify
DO $$
DECLARE
    table_exists BOOLEAN;
    old_table_exists BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT FROM pg_tables
        WHERE schemaname = 'public'
        AND tablename = 'backtest_configuration'
    ) INTO table_exists;

    SELECT EXISTS (
        SELECT FROM pg_tables
        WHERE schemaname = 'public'
        AND tablename = 'BacktestConfiguration'
    ) INTO old_table_exists;

    IF NOT table_exists OR old_table_exists THEN
        RAISE EXCEPTION 'Migration failed: Table rename verification failed';
    END IF;

    RAISE NOTICE '✓ BacktestConfiguration successfully renamed to backtest_configuration';
END $$;

COMMIT;

SELECT 'BacktestConfiguration renamed to backtest_configuration' AS status;
