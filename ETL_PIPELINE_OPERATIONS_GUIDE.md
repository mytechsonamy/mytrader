# MyTrader ETL Pipeline Operations Guide

## Executive Summary

This guide provides comprehensive operational procedures for the MyTrader Data Integrity ETL Pipeline, a production-ready system designed to maintain data consistency, quality, and completeness across the trading platform's database.

### Pipeline Components

1. **Symbol Synchronization Service** - Discovers and synchronizes missing symbols
2. **Asset Enrichment Pipeline** - Enriches symbols with external metadata
3. **Market Data Bootstrap Service** - Initializes reference data
4. **Data Integrity Orchestrator** - Coordinates all ETL operations

## Quick Start

### Initial System Setup

```bash
# 1. Bootstrap reference data (run once)
curl -X POST "https://api.mytrader.com/api/etl/bootstrap-reference-data?overwriteExisting=false" \
  -H "Authorization: Bearer YOUR_TOKEN"

# 2. Sync missing symbols
curl -X POST "https://api.mytrader.com/api/etl/sync-symbols" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "batchSize": 1000,
    "maxConcurrency": 5,
    "autoEnrichMetadata": true
  }'

# 3. Enrich assets with metadata
curl -X POST "https://api.mytrader.com/api/etl/enrich-assets" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "enabledSources": ["YAHOO_FINANCE", "COINMARKETCAP"],
    "maxConcurrency": 3,
    "overwriteExistingData": false
  }'
```

### Daily Operations

```bash
# Run full ETL pipeline (recommended daily)
curl -X POST "https://api.mytrader.com/api/etl/execute-full-pipeline" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "executeSymbolSync": true,
    "executeAssetEnrichment": true,
    "executeReferenceDataBootstrap": false,
    "validateAfterExecution": true,
    "continueOnComponentFailure": true
  }'

# Check system health
curl -X GET "https://api.mytrader.com/api/etl/status" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Operational Procedures

### 1. Symbol Synchronization

**Purpose**: Discover and add missing symbols from market_data table to symbols table.

**When to Run**:
- Daily as part of full ETL pipeline
- After importing new market data
- When orphaned market data records are detected

**Configuration Options**:
```json
{
  "batchSize": 1000,           // Records per batch
  "maxConcurrency": 5,         // Parallel batches
  "autoEnrichMetadata": true,  // Auto-enrich new symbols
  "skipExistingSymbols": true, // Skip already processed
  "assetClassFilter": ["CRYPTO", "STOCK"],
  "venueFilter": ["BINANCE", "NASDAQ"],
  "maxProcessingTime": "01:00:00"
}
```

**Success Criteria**:
- `MarketDataRecordsWithoutSymbols` = 0
- `SymbolsAdded` > 0 (if missing symbols existed)
- `Success` = true
- No critical errors in logs

**Failure Recovery**:
1. Check logs for specific error patterns
2. Verify database connectivity
3. Check for schema changes in market_data table
4. Run validation: `POST /api/etl/validate-cleanup-symbols`
5. Retry with smaller batch size if memory issues

### 2. Asset Enrichment

**Purpose**: Enrich symbols with metadata from external APIs (CoinMarketCap, Yahoo Finance, etc.).

**When to Run**:
- Daily for newly added symbols
- Weekly for stale data refresh
- Before important trading decisions

**Configuration Options**:
```json
{
  "enabledSources": ["COINMARKETCAP", "YAHOO_FINANCE", "ALPHA_VANTAGE"],
  "maxConcurrency": 3,
  "batchSize": 50,
  "overwriteExistingData": false,
  "rateLimitDelay": "00:00:00.500",
  "maxRetries": 3,
  "requestTimeout": "00:00:30",
  "includeCompanyInfo": true,
  "includeMarketCap": true,
  "includeSectorInfo": true,
  "includePriceInfo": true
}
```

**Success Criteria**:
- `SuccessfullyEnriched` + `PartiallyEnriched` > 80% of target symbols
- No critical API failures
- Rate limits respected
- Data quality score > 70

**Rate Limiting Best Practices**:
- Monitor API call quotas
- Implement exponential backoff
- Use multiple API keys if available
- Cache responses when possible

**Failure Recovery**:
1. Check API key validity and quotas
2. Verify external service health
3. Review rate limiting configuration
4. Consider fallback data sources

### 3. Reference Data Bootstrap

**Purpose**: Initialize and maintain asset classes, markets, trading sessions, and data providers.

**When to Run**:
- Once during initial setup
- When adding new markets or asset classes
- After major system upgrades

**Components Initialized**:
- **Asset Classes**: CRYPTO, STOCK, STOCK_BIST, FOREX, COMMODITY, ETF
- **Markets**: BINANCE, BIST, NASDAQ, NYSE, FOREX_MARKET
- **Trading Sessions**: Market-specific trading hours
- **Data Providers**: API configurations for external services

**Validation Checks**:
```bash
# Validate reference data integrity
curl -X POST "https://api.mytrader.com/api/etl/validate-reference-data" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Check bootstrap status
curl -X GET "https://api.mytrader.com/api/etl/bootstrap-status" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 4. Full Pipeline Execution

**Purpose**: Orchestrate all ETL components in proper dependency order.

**Execution Order**:
1. Reference Data Bootstrap (if enabled)
2. Symbol Synchronization (if enabled)
3. Asset Enrichment (if enabled)
4. Post-execution Validation (if enabled)

**Configuration Example**:
```json
{
  "executeSymbolSync": true,
  "executeAssetEnrichment": true,
  "executeReferenceDataBootstrap": false,
  "validateAfterExecution": true,
  "continueOnComponentFailure": true,
  "executeInParallel": false,
  "maxExecutionTime": "04:00:00",
  "sendNotificationOnCompletion": true,
  "sendNotificationOnFailure": true
}
```

## Monitoring and Alerting

### Health Check Endpoints

```bash
# Overall system health
GET /api/etl/health

# Component-specific status
GET /api/etl/status
GET /api/etl/symbol-sync-status
GET /api/etl/enrichment-status
GET /api/etl/bootstrap-status
```

### Key Metrics to Monitor

**System Health Indicators**:
- `OverallDataQuality` > 75%
- `SymbolCoverage` > 90%
- `EnrichmentCoverage` > 70%
- `MarketDataRecordsWithoutSymbols` = 0

**Performance Metrics**:
- Average execution time < 2 hours
- Success rate > 95%
- API error rate < 5%
- Memory usage < 8GB

**Data Quality Metrics**:
- Symbol validation issues < 1%
- Enrichment success rate > 80%
- Data completeness score > 85%

### Alert Thresholds

**Critical Alerts** (Page on-call):
- Full pipeline failure
- Database connectivity loss
- Security breach indicators
- Data corruption detected

**Warning Alerts** (Email/Slack):
- SLA breach (execution time > 2 hours)
- High error rate (> 10%)
- Low data quality score (< 70%)
- External API failures

**Info Alerts** (Log only):
- Successful pipeline completion
- Performance degradation
- Configuration changes

## Troubleshooting

### Common Issues and Solutions

#### 1. High Memory Usage
**Symptoms**: OutOfMemoryException, slow performance
**Solution**:
```bash
# Reduce batch size
curl -X POST ".../sync-symbols" -d '{"batchSize": 500}'

# Reduce concurrency
curl -X POST ".../sync-symbols" -d '{"maxConcurrency": 2}'

# Check for memory leaks in logs
```

#### 2. API Rate Limiting
**Symptoms**: HTTP 429 errors, enrichment failures
**Solution**:
```bash
# Increase rate limit delay
curl -X POST ".../enrich-assets" -d '{"rateLimitDelay": "00:00:01"}'

# Reduce concurrent requests
curl -X POST ".../enrich-assets" -d '{"maxConcurrency": 1}'

# Check API quota usage
curl -X GET ".../enrichment-sources-status"
```

#### 3. Database Deadlocks
**Symptoms**: SqlException, transaction timeouts
**Solution**:
- Reduce batch size
- Implement retry with exponential backoff
- Check for long-running transactions
- Review index usage

#### 4. Orphaned Market Data
**Symptoms**: `MarketDataRecordsWithoutSymbols` > 0
**Solution**:
```bash
# Run symbol synchronization
curl -X POST ".../sync-symbols"

# Validate and cleanup
curl -X POST ".../validate-cleanup-symbols"
```

#### 5. Low Data Quality Score
**Symptoms**: `DataQualityScore` < 70
**Solution**:
- Review enrichment source configuration
- Check API key validity
- Validate symbol ticker formats
- Run data cleanup procedures

### Log Analysis

**Log Locations**:
- Application logs: `/var/log/mytrader/etl/`
- Performance logs: `/var/log/mytrader/performance/`
- Error logs: `/var/log/mytrader/errors/`

**Key Log Patterns**:
```bash
# Find ETL job executions
grep "Starting.*ETL pipeline" /var/log/mytrader/etl/*.log

# Find performance issues
grep "Duration.*exceeded" /var/log/mytrader/performance/*.log

# Find API errors
grep "API.*failed\|HTTP.*[45][0-9][0-9]" /var/log/mytrader/etl/*.log

# Find database errors
grep "SqlException\|Database.*timeout" /var/log/mytrader/errors/*.log
```

### Performance Optimization

#### Database Optimization
```sql
-- Index optimization for symbol lookup
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_symbol_lookup
ON market_data (symbol) WHERE symbol IS NOT NULL;

-- Index for orphaned records query
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_symbols_ticker_lookup
ON symbols (ticker) WHERE is_active = true;

-- Analyze table statistics
ANALYZE market_data;
ANALYZE symbols;
```

#### Configuration Tuning
```json
{
  "symbolSync": {
    "batchSize": 2000,        // Increase for better throughput
    "maxConcurrency": 8,      // Scale with CPU cores
    "connectionPoolSize": 20   // Match concurrency
  },
  "enrichment": {
    "maxConcurrency": 4,      // Balance API limits
    "rateLimitDelay": "00:00:00.200",
    "requestTimeout": "00:00:15"
  }
}
```

## Maintenance Procedures

### Daily Maintenance
1. Check system health dashboard
2. Review overnight ETL execution logs
3. Monitor API quota usage
4. Validate data quality metrics

### Weekly Maintenance
1. Run comprehensive data validation
2. Review and clear old logs
3. Update API keys if needed
4. Performance trend analysis

### Monthly Maintenance
1. Review and update SLA thresholds
2. Audit security configurations
3. Update external API configurations
4. Capacity planning review

### Quarterly Maintenance
1. Full system security audit
2. Update operational runbooks
3. Review disaster recovery procedures
4. Performance benchmarking

## Disaster Recovery

### Backup Procedures
```sql
-- Backup critical ETL metadata tables
pg_dump -t symbols -t asset_classes -t markets -t data_providers mytrader_db > etl_backup.sql

-- Backup ETL execution history
pg_dump -t etl_execution_history mytrader_db > etl_history_backup.sql
```

### Recovery Procedures
1. **Database Corruption**:
   - Restore from latest backup
   - Run reference data bootstrap
   - Execute incremental symbol sync
   - Validate data integrity

2. **Configuration Loss**:
   - Restore configuration from version control
   - Re-bootstrap reference data
   - Validate all components

3. **API Access Loss**:
   - Implement fallback data sources
   - Use cached data where available
   - Schedule retry when service restored

## Security Considerations

### API Key Management
- Store API keys in secure key vault
- Rotate keys quarterly
- Monitor for unauthorized usage
- Use least-privilege access

### Data Privacy
- Mask sensitive data in logs
- Encrypt data at rest and in transit
- Implement audit logging
- Regular security scans

### Access Control
- Implement role-based access
- Require authentication for all ETL endpoints
- Log all administrative actions
- Regular access reviews

## Performance Benchmarks

### Expected Performance
- **Full Pipeline**: < 2 hours for 100K symbols
- **Symbol Sync**: < 30 minutes for 50K new symbols
- **Asset Enrichment**: < 1 hour for 10K symbols
- **Reference Bootstrap**: < 5 minutes

### Scaling Guidelines
- **Small Deployment** (< 10K symbols): 2-4 CPU cores, 4GB RAM
- **Medium Deployment** (10K-100K symbols): 4-8 CPU cores, 8GB RAM
- **Large Deployment** (> 100K symbols): 8+ CPU cores, 16GB+ RAM

## Contact Information

**Operations Team**: ops-team@mytrader.com
**Development Team**: dev-team@mytrader.com
**On-Call**: +1-555-ETL-HELP
**Incident Management**: incidents@mytrader.com

---

*This document is version-controlled. Last updated: $(date)*
*For the latest version, see: https://docs.mytrader.com/etl-operations*