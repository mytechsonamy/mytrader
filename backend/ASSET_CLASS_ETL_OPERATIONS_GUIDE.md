# AssetClass Data Integrity ETL Pipeline - Operations Guide

## Overview

The AssetClass Data Integrity ETL Pipeline is a production-ready batch processing system designed to fix AssetClass values in the `market_data` table by mapping them from the corresponding `symbols` table entries. This guide covers operational procedures, monitoring, and troubleshooting for maintaining data integrity.

## System Architecture

### Core Components

1. **IMarketDataAssetClassCorrectionService** - Main ETL orchestrator
2. **AssetClassCorrectionMonitor** - Operational monitoring and alerting
3. **AssetClassCorrectionController** - REST API endpoints
4. **SQL Migration Scripts** - Database correction procedures

### Data Flow

```
[market_data] → [Integrity Analysis] → [Batch Processing] → [Validation] → [Quality Reporting]
     ↓                    ↓                      ↓              ↓              ↓
[symbols] ← [Asset Mapping] ← [Error Handling] ← [Backup] ← [Rollback] ← [Monitoring]
```

## API Endpoints

### Core Operations

| Endpoint | Method | Purpose | SLA |
|----------|---------|---------|-----|
| `/api/etl/asset-class-correction/analyze` | GET | Analyze current data integrity issues | < 30 seconds |
| `/api/etl/asset-class-correction/preview` | POST | Preview corrections without execution | < 1 minute |
| `/api/etl/asset-class-correction/execute` | POST | Execute full correction pipeline | < 2 hours |
| `/api/etl/asset-class-correction/validate` | POST | Validate corrections post-execution | < 5 minutes |
| `/api/etl/asset-class-correction/status` | GET | Get current operation status | < 5 seconds |
| `/api/etl/asset-class-correction/rollback` | POST | Rollback last correction operation | < 30 minutes |
| `/api/etl/asset-class-correction/health` | GET | Health check for monitoring | < 2 seconds |
| `/api/etl/asset-class-correction/metrics` | GET | Operational metrics | < 5 seconds |

### Request Examples

#### Execute Correction (Standard)
```json
POST /api/etl/asset-class-correction/execute
{
    "batchSize": 1000,
    "createBackup": true,
    "onlyProcessNullAssetClass": true,
    "validateAfterEachBatch": true,
    "dryRun": false
}
```

#### Execute Correction (Conservative)
```json
POST /api/etl/asset-class-correction/execute
{
    "batchSize": 500,
    "maxRecordsToProcess": 10000,
    "createBackup": true,
    "onlyProcessNullAssetClass": true,
    "overwriteExistingValues": false,
    "validateAfterEachBatch": true,
    "timeout": "01:00:00",
    "dryRun": false
}
```

#### Rollback Operation
```json
POST /api/etl/asset-class-correction/rollback
{
    "correctionId": "your-operation-id-here",
    "reason": "Data quality issues detected",
    "validateAfterRollback": true
}
```

## Operational Procedures

### Daily Operations

#### 1. Health Check (Every 15 minutes)
```bash
curl -X GET "https://api.mytrader.com/api/etl/asset-class-correction/health" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Expected Response:** HTTP 200 with `isHealthy: true`

#### 2. Data Quality Assessment (Daily)
```bash
curl -X GET "https://api.mytrader.com/api/etl/asset-class-correction/analyze" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Quality Thresholds:**
- Completeness Score: ≥ 95%
- Integrity Score: ≥ 90%
- Orphaned Records: < 5%

#### 3. Proactive Correction (If needed)
Run preview first, then execute if acceptable:
```bash
# Preview
curl -X POST "https://api.mytrader.com/api/etl/asset-class-correction/preview" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"batchSize": 1000, "dryRun": false}'

# Execute (if preview looks good)
curl -X POST "https://api.mytrader.com/api/etl/asset-class-correction/execute" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"batchSize": 1000, "createBackup": true}'
```

### Weekly Operations

#### 1. Backup Cleanup
Remove backup tables older than 30 days:
```sql
-- List backup tables
SELECT tablename FROM pg_tables
WHERE tablename LIKE 'market_data_backup_%'
ORDER BY tablename DESC;

-- Drop old backups (adjust date as needed)
DROP TABLE IF EXISTS market_data_backup_YYYYMMDD_HHMMSS;
```

#### 2. Performance Analysis
Review ETL execution history:
```bash
curl -X GET "https://api.mytrader.com/api/etl/asset-class-correction/metrics" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Monthly Operations

#### 1. Full Data Integrity Audit
```sql
-- Comprehensive integrity check
SELECT
    COUNT(*) as total_records,
    COUNT(*) FILTER (WHERE asset_class IS NULL) as null_asset_class,
    COUNT(*) FILTER (WHERE asset_class = '') as empty_asset_class,
    COUNT(*) FILTER (WHERE asset_class IS NOT NULL AND asset_class != '') as valid_asset_class,
    ROUND(
        (COUNT(*) FILTER (WHERE asset_class IS NOT NULL AND asset_class != '')::DECIMAL / COUNT(*)) * 100,
        2
    ) as completeness_percentage
FROM market_data;
```

#### 2. Trending Analysis
Monitor data quality trends over time and adjust alerting thresholds if needed.

## Monitoring and Alerting

### Key Metrics

| Metric | Threshold | Action |
|--------|-----------|--------|
| Operation Success Rate | < 95% | Investigate failures |
| Data Quality Score | < 90% | Schedule correction |
| Operation Duration | > 2 hours | Check performance |
| Error Rate | > 5% | Review error patterns |
| Records Needing Correction | > 10% of total | Plan correction run |

### Alert Categories

#### High Severity
- **OPERATION_FAILED**: Correction operation completely failed
- **SLA_VIOLATION**: Operation exceeded maximum duration
- **SERVICE_DEGRADED**: Multiple component failures

#### Medium Severity
- **HIGH_ERROR_RATE**: Error rate above 10%
- **LOW_DATA_QUALITY**: Quality score below 90%
- **DATA_QUALITY_DEGRADED**: Significant quality decrease

#### Low Severity
- **HIGH_ORPHANED_RECORDS**: >10% orphaned records
- **PERFORMANCE_DEGRADATION**: Slower than expected execution

### Grafana Dashboard Queries

#### Operations per Day
```promql
increase(assetclass_operations_started_total[24h])
```

#### Success Rate
```promql
rate(assetclass_operations_completed_total[5m]) / rate(assetclass_operations_started_total[5m]) * 100
```

#### Average Duration
```promql
rate(assetclass_operation_duration_seconds_sum[5m]) / rate(assetclass_operation_duration_seconds_count[5m])
```

#### Data Quality Score
```promql
assetclass_data_quality_score
```

## Troubleshooting Guide

### Common Issues and Solutions

#### Issue: "Another operation is already running"
**Symptom:** HTTP 409 response when starting correction

**Diagnosis:**
```bash
curl -X GET "https://api.mytrader.com/api/etl/asset-class-correction/status"
```

**Solution:**
1. Check if operation is genuinely running
2. If stuck, wait for timeout or restart service
3. If urgent, contact development team for manual intervention

#### Issue: High Error Rate During Processing
**Symptom:** Many record-level failures during batch processing

**Diagnosis:**
1. Check error patterns in logs
2. Verify database connectivity
3. Check for data consistency issues

**Solution:**
```bash
# Stop current operation if safe
# Analyze specific errors
curl -X GET "https://api.mytrader.com/api/etl/asset-class-correction/analyze"

# Run with smaller batch size
curl -X POST "https://api.mytrader.com/api/etl/asset-class-correction/execute" \
  -d '{"batchSize": 100, "createBackup": true}'
```

#### Issue: Poor Data Quality After Correction
**Symptom:** Validation shows low quality score after correction

**Diagnosis:**
```bash
curl -X POST "https://api.mytrader.com/api/etl/asset-class-correction/validate"
```

**Solution:**
1. Check for orphaned market data records
2. Verify symbol table completeness
3. Consider rollback if quality is critically low:
```bash
curl -X POST "https://api.mytrader.com/api/etl/asset-class-correction/rollback" \
  -d '{"correctionId": "OPERATION_ID"}'
```

#### Issue: Operation Timeout
**Symptom:** Operation exceeds maximum duration and fails

**Diagnosis:**
- Large dataset requiring processing
- Database performance issues
- Network connectivity problems

**Solution:**
1. Run with `maxRecordsToProcess` limit:
```json
{
    "maxRecordsToProcess": 50000,
    "batchSize": 500,
    "timeout": "03:00:00"
}
```

2. Process in multiple smaller runs
3. Schedule during low-traffic periods

### Emergency Procedures

#### Complete Rollback
```bash
# Get the latest operation ID
OPERATION_ID=$(curl -s -X GET "https://api.mytrader.com/api/etl/asset-class-correction/status" | jq -r '.data.recentOperations[0].correctionId')

# Rollback
curl -X POST "https://api.mytrader.com/api/etl/asset-class-correction/rollback" \
  -H "Content-Type: application/json" \
  -d "{\"correctionId\": \"$OPERATION_ID\"}"
```

#### Manual Database Recovery
If API rollback fails, use SQL migration rollback procedure:
```sql
-- Find backup table
SELECT tablename FROM pg_tables
WHERE tablename LIKE 'market_data_backup_%'
ORDER BY tablename DESC LIMIT 1;

-- Restore from backup (replace with actual table name)
UPDATE market_data
SET asset_class = backup.asset_class,
    updated_at = CURRENT_TIMESTAMP
FROM market_data_backup_YYYYMMDD_HHMMSS backup
WHERE market_data.id = backup.id;
```

## Performance Optimization

### Batch Size Tuning
- **Small datasets (< 10K records)**: Use batch size 500-1000
- **Medium datasets (10K-100K records)**: Use batch size 1000-2000
- **Large datasets (> 100K records)**: Use batch size 2000-5000

### Optimal Scheduling
- **Peak hours**: Avoid large corrections
- **Off-hours (2-6 AM)**: Ideal for large operations
- **Maintenance windows**: Best for high-risk operations

### Database Optimization
```sql
-- Ensure proper indexes exist
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_symbol_asset_class
ON market_data(symbol, asset_class) WHERE asset_class IS NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_symbols_ticker_asset_class
ON symbols(ticker, asset_class);
```

## Security Considerations

### Access Control
- ETL endpoints require authentication
- Role-based access for different operations
- Audit logging for all ETL operations

### Data Protection
- Automatic backup creation before changes
- Rollback capability for all operations
- Data validation at multiple checkpoints

### Compliance
- All operations logged with timestamps
- Change tracking with before/after values
- Retention policies for backup data

## Maintenance Schedule

### Daily (Automated)
- [ ] Health checks every 15 minutes
- [ ] Data quality monitoring
- [ ] Alert threshold evaluation

### Weekly (Manual)
- [ ] Review operation success rates
- [ ] Clean up old backup tables
- [ ] Performance trend analysis

### Monthly (Manual)
- [ ] Comprehensive data audit
- [ ] Update alert thresholds if needed
- [ ] Review and optimize batch sizes
- [ ] Documentation updates

### Quarterly (Manual)
- [ ] Full system performance review
- [ ] Disaster recovery testing
- [ ] Security audit of ETL operations
- [ ] Capacity planning review

## Contact Information

### On-Call Escalation
1. **Level 1**: ETL Operations Team
2. **Level 2**: Data Engineering Team
3. **Level 3**: Platform Engineering Team

### Emergency Contacts
- **Data Quality Issues**: data-quality@mytrader.com
- **Performance Issues**: platform-ops@mytrader.com
- **Security Concerns**: security@mytrader.com

---

**Last Updated**: 2025-09-24
**Document Version**: 1.0
**Next Review**: 2025-12-24