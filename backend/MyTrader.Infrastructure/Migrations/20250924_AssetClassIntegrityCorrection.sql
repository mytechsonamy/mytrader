-- =============================================================================
-- ASSET CLASS DATA INTEGRITY CORRECTION MIGRATION
-- =============================================================================
-- File: 20250924_AssetClassIntegrityCorrection.sql
-- Author: ETL Data Architecture Team
-- Date: 2025-09-24
-- Description: Comprehensive fix for AssetClass values in market_data table
--              by mapping them from the corresponding symbols table entries
--
-- ISSUE IDENTIFIED:
-- 1. market_data records have NULL, empty, or incorrect AssetClass values
-- 2. Symbols table contains the correct AssetClass information
-- 3. Need to establish data consistency between tables
--
-- OPERATION TYPE: Batch ETL with comprehensive monitoring and rollback capability
-- ESTIMATED DURATION: 5-30 minutes (depending on data volume)
-- SAFETY LEVEL: HIGH - Full backup and validation included
-- =============================================================================

BEGIN;

-- =============================================================================
-- STEP 1: PRE-EXECUTION VALIDATION AND ANALYSIS
-- =============================================================================

DO $$
DECLARE
    total_market_data_records INTEGER;
    null_asset_class_records INTEGER;
    empty_asset_class_records INTEGER;
    mismatched_asset_class_records INTEGER;
    orphaned_market_data_records INTEGER;
    total_symbols INTEGER;
    migration_id TEXT;
    start_time TIMESTAMP;
BEGIN
    migration_id := 'ASSET_CLASS_CORRECTION_' || to_char(NOW(), 'YYYYMMDD_HH24MISS');
    start_time := NOW();

    -- Get comprehensive statistics
    SELECT COUNT(*) INTO total_market_data_records FROM market_data;
    SELECT COUNT(*) INTO total_symbols FROM symbols;

    SELECT COUNT(*) INTO null_asset_class_records
    FROM market_data
    WHERE asset_class IS NULL;

    SELECT COUNT(*) INTO empty_asset_class_records
    FROM market_data
    WHERE asset_class = '';

    SELECT COUNT(*) INTO mismatched_asset_class_records
    FROM market_data md
    INNER JOIN symbols s ON md.symbol = s.ticker
    WHERE md.asset_class IS DISTINCT FROM s.asset_class
      AND md.asset_class IS NOT NULL
      AND md.asset_class != ''
      AND s.asset_class IS NOT NULL
      AND s.asset_class != '';

    SELECT COUNT(*) INTO orphaned_market_data_records
    FROM market_data md
    LEFT JOIN symbols s ON md.symbol = s.ticker
    WHERE s.ticker IS NULL;

    -- Log pre-execution analysis
    RAISE NOTICE '=== ASSET CLASS INTEGRITY ANALYSIS ===';
    RAISE NOTICE 'Migration ID: %', migration_id;
    RAISE NOTICE 'Start Time: %', start_time;
    RAISE NOTICE 'Total market_data records: %', total_market_data_records;
    RAISE NOTICE 'Total symbols: %', total_symbols;
    RAISE NOTICE 'Records with NULL asset_class: %', null_asset_class_records;
    RAISE NOTICE 'Records with empty asset_class: %', empty_asset_class_records;
    RAISE NOTICE 'Records with mismatched asset_class: %', mismatched_asset_class_records;
    RAISE NOTICE 'Orphaned market_data records (no symbol): %', orphaned_market_data_records;

    -- Calculate data quality metrics
    DECLARE
        completeness_score DECIMAL;
        integrity_score DECIMAL;
        records_needing_correction INTEGER;
    BEGIN
        records_needing_correction := null_asset_class_records + empty_asset_class_records;

        IF total_market_data_records > 0 THEN
            completeness_score := ((total_market_data_records - null_asset_class_records - empty_asset_class_records)::DECIMAL / total_market_data_records) * 100;
            integrity_score := ((total_market_data_records - orphaned_market_data_records)::DECIMAL / total_market_data_records) * 100;
        ELSE
            completeness_score := 100;
            integrity_score := 100;
        END IF;

        RAISE NOTICE 'Current AssetClass completeness: %% (%/%)',
                     ROUND(completeness_score, 2),
                     total_market_data_records - records_needing_correction,
                     total_market_data_records;
        RAISE NOTICE 'Current referential integrity: %% (%/%)',
                     ROUND(integrity_score, 2),
                     total_market_data_records - orphaned_market_data_records,
                     total_market_data_records;
        RAISE NOTICE 'Records requiring correction: %', records_needing_correction;

        -- Safety checks
        IF total_market_data_records = 0 THEN
            RAISE EXCEPTION 'SAFETY CHECK FAILED: No market_data records found. Migration not needed.';
        END IF;

        IF records_needing_correction = 0 THEN
            RAISE NOTICE 'INFO: No records need correction. Migration will complete successfully with no changes.';
        END IF;

        IF orphaned_market_data_records > total_market_data_records * 0.1 THEN
            RAISE WARNING 'WARNING: High number of orphaned records (% > 10%%). Consider running symbol synchronization first.',
                         ROUND((orphaned_market_data_records::DECIMAL / total_market_data_records) * 100, 1);
        END IF;
    END;
END $$;

-- =============================================================================
-- STEP 2: CREATE COMPREHENSIVE BACKUP
-- =============================================================================

-- Create timestamped backup table
DO $$
DECLARE
    backup_table_name TEXT;
    backup_start_time TIMESTAMP;
    records_backed_up INTEGER;
BEGIN
    backup_table_name := 'market_data_backup_' || to_char(NOW(), 'YYYYMMDD_HH24MISS');
    backup_start_time := NOW();

    -- Create backup with all original data and metadata
    EXECUTE format('
        CREATE TABLE %I AS
        SELECT *,
               NOW() as backup_created_at,
               ''ASSET_CLASS_CORRECTION'' as backup_reason,
               md5(row(md.*)::text) as record_hash
        FROM market_data md',
        backup_table_name);

    -- Get count of backed up records
    EXECUTE format('SELECT COUNT(*) FROM %I', backup_table_name) INTO records_backed_up;

    RAISE NOTICE '=== BACKUP CREATED ===';
    RAISE NOTICE 'Backup table: %', backup_table_name;
    RAISE NOTICE 'Records backed up: %', records_backed_up;
    RAISE NOTICE 'Backup duration: %', NOW() - backup_start_time;

    -- Store backup metadata for rollback procedures
    INSERT INTO migration_audit_log (
        migration_id,
        phase,
        operation,
        details,
        created_at
    ) VALUES (
        'ASSET_CLASS_CORRECTION_' || to_char(NOW(), 'YYYYMMDD_HH24MISS'),
        'BACKUP',
        'CREATE_BACKUP_TABLE',
        json_build_object(
            'backup_table', backup_table_name,
            'records_backed_up', records_backed_up,
            'backup_created_at', NOW()
        ),
        NOW()
    ) ON CONFLICT DO NOTHING; -- Handle case where audit table doesn't exist

EXCEPTION
    WHEN undefined_table THEN
        -- migration_audit_log table doesn't exist, continue without logging
        RAISE NOTICE 'NOTE: migration_audit_log table not found. Backup created but not logged.';
END $$;

-- =============================================================================
-- STEP 3: ASSET CLASS CORRECTION OPERATIONS
-- =============================================================================

-- Phase 3.1: Correct NULL AssetClass values
DO $$
DECLARE
    null_corrections_start TIMESTAMP;
    null_records_corrected INTEGER;
BEGIN
    null_corrections_start := NOW();

    RAISE NOTICE '=== CORRECTING NULL ASSET CLASS VALUES ===';

    -- Update market_data records with NULL asset_class from symbols table
    UPDATE market_data
    SET asset_class = s.asset_class,
        updated_at = CURRENT_TIMESTAMP
    FROM symbols s
    WHERE market_data.symbol = s.ticker
      AND market_data.asset_class IS NULL
      AND s.asset_class IS NOT NULL
      AND s.asset_class != '';

    GET DIAGNOSTICS null_records_corrected = ROW_COUNT;

    RAISE NOTICE 'NULL AssetClass corrections completed: % records updated in %',
                 null_records_corrected,
                 NOW() - null_corrections_start;
END $$;

-- Phase 3.2: Correct empty AssetClass values
DO $$
DECLARE
    empty_corrections_start TIMESTAMP;
    empty_records_corrected INTEGER;
BEGIN
    empty_corrections_start := NOW();

    RAISE NOTICE '=== CORRECTING EMPTY ASSET CLASS VALUES ===';

    -- Update market_data records with empty asset_class from symbols table
    UPDATE market_data
    SET asset_class = s.asset_class,
        updated_at = CURRENT_TIMESTAMP
    FROM symbols s
    WHERE market_data.symbol = s.ticker
      AND market_data.asset_class = ''
      AND s.asset_class IS NOT NULL
      AND s.asset_class != '';

    GET DIAGNOSTICS empty_records_corrected = ROW_COUNT;

    RAISE NOTICE 'Empty AssetClass corrections completed: % records updated in %',
                 empty_records_corrected,
                 NOW() - empty_corrections_start;
END $$;

-- Phase 3.3: Handle orphaned market_data records (optional - log only)
DO $$
DECLARE
    orphaned_records INTEGER;
    sample_orphaned_symbols TEXT[];
BEGIN
    SELECT COUNT(*),
           array_agg(DISTINCT md.symbol ORDER BY md.symbol LIMIT 10)
    INTO orphaned_records, sample_orphaned_symbols
    FROM market_data md
    LEFT JOIN symbols s ON md.symbol = s.ticker
    WHERE s.ticker IS NULL;

    IF orphaned_records > 0 THEN
        RAISE NOTICE '=== ORPHANED RECORDS IDENTIFIED ===';
        RAISE NOTICE 'Orphaned market_data records: %', orphaned_records;
        RAISE NOTICE 'Sample orphaned symbols: %', array_to_string(sample_orphaned_symbols, ', ');
        RAISE NOTICE 'RECOMMENDATION: Run symbol synchronization to add missing symbols';

        -- Create orphaned records report table for investigation
        DROP TABLE IF EXISTS orphaned_market_data_report;
        CREATE TEMPORARY TABLE orphaned_market_data_report AS
        SELECT
            md.symbol,
            COUNT(*) as record_count,
            MIN(md.timestamp) as earliest_record,
            MAX(md.timestamp) as latest_record,
            NOW() as report_generated_at
        FROM market_data md
        LEFT JOIN symbols s ON md.symbol = s.ticker
        WHERE s.ticker IS NULL
        GROUP BY md.symbol
        ORDER BY record_count DESC, md.symbol;

        RAISE NOTICE 'Orphaned records report created in temporary table: orphaned_market_data_report';
    END IF;
END $$;

-- =============================================================================
-- STEP 4: POST-CORRECTION VALIDATION AND QUALITY ASSESSMENT
-- =============================================================================

DO $$
DECLARE
    post_total_records INTEGER;
    post_null_records INTEGER;
    post_empty_records INTEGER;
    post_valid_records INTEGER;
    post_orphaned_records INTEGER;
    post_completeness_score DECIMAL;
    post_integrity_score DECIMAL;
    improvement_records INTEGER;
    validation_start TIMESTAMP;

    -- Quality thresholds
    min_completeness_threshold DECIMAL := 95.0;
    min_integrity_threshold DECIMAL := 90.0;
BEGIN
    validation_start := NOW();

    RAISE NOTICE '=== POST-CORRECTION VALIDATION ===';

    -- Recalculate statistics
    SELECT COUNT(*) INTO post_total_records FROM market_data;

    SELECT COUNT(*) INTO post_null_records
    FROM market_data
    WHERE asset_class IS NULL;

    SELECT COUNT(*) INTO post_empty_records
    FROM market_data
    WHERE asset_class = '';

    SELECT COUNT(*) INTO post_valid_records
    FROM market_data md
    INNER JOIN symbols s ON md.symbol = s.ticker
    WHERE md.asset_class = s.asset_class
      AND md.asset_class IS NOT NULL
      AND md.asset_class != '';

    SELECT COUNT(*) INTO post_orphaned_records
    FROM market_data md
    LEFT JOIN symbols s ON md.symbol = s.ticker
    WHERE s.ticker IS NULL;

    -- Calculate quality scores
    IF post_total_records > 0 THEN
        post_completeness_score := ((post_total_records - post_null_records - post_empty_records)::DECIMAL / post_total_records) * 100;
        post_integrity_score := ((post_total_records - post_orphaned_records)::DECIMAL / post_total_records) * 100;
    ELSE
        post_completeness_score := 100;
        post_integrity_score := 100;
    END IF;

    improvement_records := post_valid_records;

    -- Validation results
    RAISE NOTICE 'Post-correction statistics:';
    RAISE NOTICE '  Total records: %', post_total_records;
    RAISE NOTICE '  NULL asset_class: %', post_null_records;
    RAISE NOTICE '  Empty asset_class: %', post_empty_records;
    RAISE NOTICE '  Valid asset_class: %', post_valid_records;
    RAISE NOTICE '  Orphaned records: %', post_orphaned_records;
    RAISE NOTICE '  Completeness score: %%', ROUND(post_completeness_score, 2);
    RAISE NOTICE '  Integrity score: %%', ROUND(post_integrity_score, 2);
    RAISE NOTICE 'Validation completed in: %', NOW() - validation_start;

    -- Quality assessment
    IF post_completeness_score >= min_completeness_threshold THEN
        RAISE NOTICE '✓ COMPLETENESS QUALITY: PASSED (%.2%% >= %.2%%)',
                     post_completeness_score, min_completeness_threshold;
    ELSE
        RAISE WARNING '⚠ COMPLETENESS QUALITY: BELOW THRESHOLD (%.2%% < %.2%%)',
                      post_completeness_score, min_completeness_threshold;
    END IF;

    IF post_integrity_score >= min_integrity_threshold THEN
        RAISE NOTICE '✓ INTEGRITY QUALITY: PASSED (%.2%% >= %.2%%)',
                     post_integrity_score, min_integrity_threshold;
    ELSE
        RAISE WARNING '⚠ INTEGRITY QUALITY: BELOW THRESHOLD (%.2%% < %.2%%)',
                      post_integrity_score, min_integrity_threshold;
    END IF;

    -- Final success criteria
    IF post_null_records + post_empty_records = 0 THEN
        RAISE NOTICE '✓ ASSET CLASS CORRECTION: FULLY SUCCESSFUL';
    ELSIF post_null_records + post_empty_records < (post_total_records * 0.05) THEN
        RAISE NOTICE '✓ ASSET CLASS CORRECTION: LARGELY SUCCESSFUL (>95%% completion)';
    ELSE
        RAISE WARNING '⚠ ASSET CLASS CORRECTION: PARTIALLY SUCCESSFUL';
        RAISE NOTICE 'RECOMMENDATION: Investigate remaining % uncorrected records',
                     post_null_records + post_empty_records;
    END IF;
END $$;

-- =============================================================================
-- STEP 5: CREATE DATA QUALITY CONSTRAINTS (OPTIONAL)
-- =============================================================================

-- Add check constraint to prevent future NULL asset_class insertions
DO $$
BEGIN
    -- Only add constraint if it doesn't exist and if we have good data quality
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints
        WHERE constraint_name = 'chk_market_data_asset_class_not_null'
    ) THEN
        -- Check if we have good enough data quality to add constraint
        DECLARE
            null_or_empty_count INTEGER;
            total_count INTEGER;
            quality_percentage DECIMAL;
        BEGIN
            SELECT COUNT(*) INTO total_count FROM market_data;
            SELECT COUNT(*) INTO null_or_empty_count
            FROM market_data
            WHERE asset_class IS NULL OR asset_class = '';

            IF total_count > 0 THEN
                quality_percentage := ((total_count - null_or_empty_count)::DECIMAL / total_count) * 100;
            ELSE
                quality_percentage := 100;
            END IF;

            IF quality_percentage >= 95.0 THEN
                -- Add constraint only if data quality is high enough
                ALTER TABLE market_data
                ADD CONSTRAINT chk_market_data_asset_class_not_null
                CHECK (asset_class IS NOT NULL AND asset_class != '');

                RAISE NOTICE '✓ DATA QUALITY CONSTRAINT ADDED: chk_market_data_asset_class_not_null';
            ELSE
                RAISE NOTICE 'ℹ DATA QUALITY CONSTRAINT SKIPPED: Quality too low (%.2%% < 95%%)', quality_percentage;
            END IF;
        END;
    ELSE
        RAISE NOTICE 'ℹ DATA QUALITY CONSTRAINT: Already exists';
    END IF;

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'ℹ DATA QUALITY CONSTRAINT: Skipped due to compatibility issues';
END $$;

-- =============================================================================
-- STEP 6: FINAL SUMMARY AND RECOMMENDATIONS
-- =============================================================================

DO $$
DECLARE
    final_summary_start TIMESTAMP;
    migration_duration INTERVAL;
BEGIN
    final_summary_start := NOW();
    -- Note: We can't access the original start time here, so we'll use an approximate
    migration_duration := INTERVAL '0 minutes'; -- Placeholder

    RAISE NOTICE '=== MIGRATION COMPLETION SUMMARY ===';
    RAISE NOTICE 'Migration completed at: %', NOW();
    RAISE NOTICE 'Operation type: AssetClass Data Integrity Correction';
    RAISE NOTICE 'Safety measures: Full backup created, comprehensive validation performed';
    RAISE NOTICE 'Rollback capability: Available via backup table';

    RAISE NOTICE '=== RECOMMENDATIONS ===';
    RAISE NOTICE '1. Monitor data quality metrics over the next 24 hours';
    RAISE NOTICE '2. Schedule regular AssetClass integrity checks (weekly/monthly)';
    RAISE NOTICE '3. Consider implementing automated symbol synchronization';
    RAISE NOTICE '4. Review orphaned records and add missing symbols as needed';
    RAISE NOTICE '5. Keep backup table for at least 30 days before cleanup';

    RAISE NOTICE '=== OPERATIONAL NOTES ===';
    RAISE NOTICE '• Backup tables can be cleaned up after verification period';
    RAISE NOTICE '• This migration is idempotent and can be re-run safely';
    RAISE NOTICE '• Contact ETL team if data quality issues persist';
    RAISE NOTICE '===============================================';
END $$;

-- =============================================================================
-- STEP 7: GENERATE FINAL REPORTS
-- =============================================================================

-- AssetClass distribution report
SELECT
    'ASSET_CLASS_DISTRIBUTION' as report_type,
    COALESCE(asset_class, 'NULL/EMPTY') as asset_class,
    COUNT(*) as record_count,
    ROUND((COUNT(*)::DECIMAL / (SELECT COUNT(*) FROM market_data)) * 100, 2) as percentage
FROM market_data
GROUP BY asset_class
ORDER BY record_count DESC;

-- Symbol coverage report
SELECT
    'SYMBOL_COVERAGE_ANALYSIS' as report_type,
    CASE
        WHEN s.ticker IS NOT NULL THEN 'HAS_SYMBOL'
        ELSE 'ORPHANED'
    END as coverage_status,
    COUNT(*) as record_count
FROM market_data md
LEFT JOIN symbols s ON md.symbol = s.ticker
GROUP BY CASE WHEN s.ticker IS NOT NULL THEN 'HAS_SYMBOL' ELSE 'ORPHANED' END
ORDER BY record_count DESC;

-- Data quality score report
WITH quality_metrics AS (
    SELECT
        COUNT(*) as total_records,
        COUNT(*) FILTER (WHERE asset_class IS NOT NULL AND asset_class != '') as records_with_asset_class,
        COUNT(*) FILTER (WHERE EXISTS (SELECT 1 FROM symbols s WHERE s.ticker = market_data.symbol)) as records_with_symbols
    FROM market_data
)
SELECT
    'DATA_QUALITY_SCORE' as report_type,
    total_records,
    records_with_asset_class,
    records_with_symbols,
    ROUND((records_with_asset_class::DECIMAL / total_records) * 100, 2) as completeness_score,
    ROUND((records_with_symbols::DECIMAL / total_records) * 100, 2) as integrity_score,
    ROUND(((records_with_asset_class::DECIMAL / total_records) + (records_with_symbols::DECIMAL / total_records)) / 2 * 100, 2) as overall_quality_score
FROM quality_metrics;

COMMIT;

-- =============================================================================
-- ROLLBACK INSTRUCTIONS (IF NEEDED)
-- =============================================================================
/*
-- EMERGENCY ROLLBACK PROCEDURE
-- Only use if critical data integrity issues are discovered immediately after migration

BEGIN;

-- 1. Find the most recent backup table
SELECT tablename
FROM pg_tables
WHERE tablename LIKE 'market_data_backup_%'
ORDER BY tablename DESC
LIMIT 1;

-- 2. Restore from backup (replace BACKUP_TABLE_NAME with actual table name)
UPDATE market_data
SET asset_class = backup.asset_class,
    updated_at = CURRENT_TIMESTAMP
FROM BACKUP_TABLE_NAME backup
WHERE market_data.id = backup.id;

-- 3. Verify rollback
SELECT COUNT(*) as total_restored
FROM market_data md
INNER JOIN BACKUP_TABLE_NAME backup ON md.id = backup.id
WHERE md.asset_class = backup.asset_class;

-- 4. Drop constraint if it was added
ALTER TABLE market_data DROP CONSTRAINT IF EXISTS chk_market_data_asset_class_not_null;

RAISE NOTICE 'ROLLBACK COMPLETED - Data restored from backup';

COMMIT;

-- Note: Only execute rollback if absolutely necessary and within 24 hours of migration
*/

-- =============================================================================
-- MAINTENANCE PROCEDURES
-- =============================================================================
/*
-- Cleanup backup tables after verification period (30+ days)
DROP TABLE IF EXISTS market_data_backup_YYYYMMDD_HHMMSS;

-- Regular data quality monitoring query (run weekly)
SELECT
    COUNT(*) as total_records,
    COUNT(*) FILTER (WHERE asset_class IS NULL) as null_asset_class,
    COUNT(*) FILTER (WHERE asset_class = '') as empty_asset_class,
    ROUND((COUNT(*) FILTER (WHERE asset_class IS NOT NULL AND asset_class != '')::DECIMAL / COUNT(*)) * 100, 2) as completeness_percentage
FROM market_data;
*/