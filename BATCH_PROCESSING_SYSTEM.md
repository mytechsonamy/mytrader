# MyTrader Enterprise Batch Processing System

## Overview

MyTrader'ın enterprise-grade batch processing sistemi, Stock_Scrapper verilerinin güvenilir, ölçeklenebilir ve izlenebilir şekilde işlenmesi için tasarlanmıştır. Hangfire tabanlı bu sistem, SLA yönetimi, dead letter queue, retry mekanizmaları ve kapsamlı monitoring ile operasyonel mükemmellik sağlar.

## Key Features

### 🚀 Core Capabilities
- **Market-Specific Processing**: BIST, Crypto, NASDAQ, NYSE için optimize edilmiş job'lar
- **Parallel Processing**: Intelligent resource allocation ile paralel işleme
- **SLA Management**: Real-time SLA monitoring ve breach alerting
- **Dead Letter Queue**: Failed job'lar için manuel müdahale sistemi
- **Retry Mechanism**: Exponential backoff ile automatic retry
- **Progress Tracking**: Real-time progress monitoring ve reporting

### 📊 Performance Targets
- **BIST**: 800+ records/second (complex format)
- **Crypto**: 1500+ records/second
- **NASDAQ**: 1500+ records/second
- **NYSE**: 1500+ records/second
- **Total Capacity**: 220+ CSV files parallel processing

## Architecture

### Job Orchestration
```
┌─────────────────────────────────────────────────────────────────┐
│                    IBatchJobOrchestrator                        │
├─────────────────────────────────────────────────────────────────┤
│ • Market Import Jobs                                            │
│ • All Markets Coordination                                     │
│ • SLA Monitoring                                              │
│ • Dead Letter Queue Management                                │
│ • Retry Logic                                                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Hangfire Job Processors                     │
├─────────────────────────────────────────────────────────────────┤
│ MarketImportJobProcessor     │ AllMarketsJobCoordinator        │
│ • Single market processing  │ • Multi-market coordination     │
│ • Progress reporting        │ • Resource allocation           │
│ • Error handling           │ • Dependency management         │
│                           │                                │
│ RetryJobProcessor          │ SlaMonitoringService           │
│ • Exponential backoff     │ • Real-time monitoring         │
│ • Dead letter handling    │ • Alert generation             │
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow
```
Stock_Scrapper/DATA/
├── BIST/           (110+ files) → Priority 1 → SLA: 45min
├── Crypto/         (12+ files)  → Priority 2 → SLA: 20min
├── NASDAQ/         (50+ files)  → Priority 3 → SLA: 25min
└── NYSE/           (50+ files)  → Priority 4 → SLA: 25min
```

## API Endpoints

### Core Job Management
```http
# Start market-specific import
POST /api/batch-processing/markets/{market}/import
Content-Type: application/json
{
  "market": "BIST",
  "dataPath": "/path/to/BIST",
  "batchSize": 800,
  "slaTarget": "00:45:00",
  "maxConcurrency": 3,
  "enableDuplicateCleanup": true
}

# Start all markets import
POST /api/batch-processing/all-markets/import
Content-Type: application/json
{
  "stockScrapperDataPath": "/path/to/Stock_Scrapper/DATA",
  "runMarketsInParallel": true,
  "globalMaxConcurrency": 8,
  "globalSlaTarget": "02:00:00"
}

# Get job status
GET /api/batch-processing/jobs/{jobId}/status

# Cancel job
POST /api/batch-processing/jobs/{jobId}/cancel

# Retry failed job
POST /api/batch-processing/jobs/{jobId}/retry
Content-Type: application/json
{
  "maxRetries": 3,
  "initialDelay": "00:01:00",
  "backoffMultiplier": 2.0
}
```

### Monitoring & Analytics
```http
# Get monitoring statistics
GET /api/batch-processing/monitoring/stats?startDate=2024-01-01&endDate=2024-01-31

# Get failed jobs (dead letter queue)
GET /api/batch-processing/dead-letter-queue?page=1&pageSize=50

# System health check
GET /api/batch-processing/health
```

### Scheduling
```http
# Schedule recurring import
POST /api/batch-processing/schedules/all-markets
Content-Type: application/json
{
  "importRequest": { /* AllMarketsImportRequest */ },
  "cronExpression": "0 2 * * *",
  "timeZone": "UTC"
}

# Remove recurring job
DELETE /api/batch-processing/schedules/{recurringJobId}
```

## Hangfire Dashboard

Access: `http://localhost:5000/hangfire`

Features:
- Real-time job monitoring
- Queue management
- Performance metrics
- Historical job data
- Manual job triggering

## Configuration

### appsettings.json
```json
{
  "BatchProcessing": {
    "StockScrapperDataPath": "/path/to/Stock_Scrapper/DATA",
    "GlobalSettings": {
      "MaxConcurrentJobs": 8,
      "DefaultBatchSize": 1000,
      "GlobalSlaTarget": "02:00:00"
    },
    "MarketConfigurations": {
      "BIST": {
        "BatchSize": 800,
        "MaxConcurrency": 3,
        "SlaTarget": "00:45:00",
        "ExpectedRecordsPerSecond": 800
      }
    },
    "SlaMonitoring": {
      "CheckInterval": "00:05:00",
      "CriticalComplianceThreshold": 80.0,
      "AlertingEnabled": true
    }
  }
}
```

## SLA Management

### Performance Targets
| Market | Batch Size | Concurrency | SLA Target | Expected Rate |
|--------|------------|-------------|------------|---------------|
| BIST   | 800        | 3           | 45 min     | 800 rec/sec   |
| Crypto | 1500       | 4           | 20 min     | 1500 rec/sec  |
| NASDAQ | 1500       | 4           | 25 min     | 1500 rec/sec  |
| NYSE   | 1500       | 4           | 25 min     | 1500 rec/sec  |

### SLA Monitoring
- **Real-time tracking**: 5-minute check intervals
- **Breach alerts**: Immediate notifications for SLA violations
- **Compliance metrics**: 80% minimum SLA compliance rate
- **Performance degradation**: Alerts for processing rate drops

## Error Handling & Recovery

### Retry Strategy
```
Attempt 1: Immediate
Attempt 2: 1 minute delay
Attempt 3: 2 minutes delay
Attempt 4: 4 minutes delay
Max delay: 30 minutes
```

### Dead Letter Queue
- Non-retryable errors (file not found, access denied)
- Max retry attempts exceeded
- Manual intervention required
- Operator notification system

### Monitoring Alerts
- SLA breach notifications
- High error rate alerts (>10%)
- Performance degradation warnings
- Resource utilization alerts
- Critical compliance rate alerts (<80%)

## Operational Runbook

### Starting Daily Import
```bash
# Via API
curl -X POST "http://localhost:5000/api/batch-processing/all-markets/import" \
  -H "Content-Type: application/json" \
  -d '{
    "stockScrapperDataPath": "/path/to/Stock_Scrapper/DATA",
    "runMarketsInParallel": true,
    "globalSlaTarget": "02:00:00"
  }'

# Via Hangfire Dashboard
1. Navigate to /hangfire
2. Go to "Recurring Jobs"
3. Click "Trigger Now" on daily import job
```

### Troubleshooting SLA Breaches

1. **Check Hangfire Dashboard** (`/hangfire`)
   - Job status and progress
   - Error messages and stack traces
   - Resource utilization

2. **Review Job Logs**
   - Application logs in Serilog/Loki
   - Performance metrics
   - Error patterns

3. **Common Issues & Solutions**
   - **Database connection timeouts**: Scale connection pool
   - **Large file processing**: Increase batch size
   - **Memory pressure**: Reduce concurrent jobs
   - **Network issues**: Check data source availability

### Performance Tuning

#### Increase Processing Speed
```json
{
  "MarketConfigurations": {
    "BIST": {
      "BatchSize": 1200,        // Increase from 800
      "MaxConcurrency": 4       // Increase from 3
    }
  }
}
```

#### Reduce Memory Usage
```json
{
  "GlobalSettings": {
    "MaxConcurrentJobs": 4,     // Reduce from 8
    "DefaultBatchSize": 500     // Reduce from 1000
  }
}
```

### Backup & Recovery
- Job metadata stored in PostgreSQL
- Automatic job history retention (30 days)
- Failed job recovery via dead letter queue
- Configuration backup in source control

## Monitoring Dashboards

### Key Metrics
- **SLA Compliance Rate**: Target >95%
- **Average Processing Rate**: Target >1000 rec/sec
- **Error Rate**: Target <5%
- **Job Success Rate**: Target >98%
- **Queue Length**: Monitor for bottlenecks

### Alerting Thresholds
- SLA breach: Immediate alert
- Error rate >10%: High priority alert
- Processing rate <500 rec/sec: Medium priority alert
- Compliance rate <80%: Critical alert

## Development Guidelines

### Adding New Market
1. Add market configuration in `DefaultMarketConfigs`
2. Update `MarketPriority` dictionary
3. Create market-specific validation logic
4. Add SLA targets and performance expectations
5. Update documentation and monitoring

### Custom Job Types
1. Implement processor class
2. Register in DI container
3. Add orchestrator methods
4. Update monitoring and alerting
5. Add API endpoints

## Production Deployment

### Prerequisites
- PostgreSQL database for Hangfire storage
- Sufficient memory (8GB+ recommended)
- CPU cores for parallel processing
- Access to Stock_Scrapper data directory

### Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<postgres-connection>
BatchProcessing__StockScrapperDataPath=<data-path>
BatchProcessing__SlaMonitoring__AlertingEnabled=true
```

### Scaling Considerations
- Horizontal scaling: Multiple API instances
- Database scaling: Connection pooling
- Storage scaling: SSD for data files
- Monitoring: External observability tools

## Files Created

### Core Services
- `/backend/MyTrader.Core/Services/BatchProcessing/IBatchJobOrchestrator.cs`
- `/backend/MyTrader.Core/DTOs/BatchProcessingDtos.cs`
- `/backend/MyTrader.Infrastructure/Services/BatchProcessing/HangfireBatchJobOrchestrator.cs`
- `/backend/MyTrader.Infrastructure/Services/BatchProcessing/MarketImportJobProcessor.cs`
- `/backend/MyTrader.Infrastructure/Services/BatchProcessing/AllMarketsJobCoordinator.cs`
- `/backend/MyTrader.Infrastructure/Services/BatchProcessing/RetryJobProcessor.cs`
- `/backend/MyTrader.Infrastructure/Services/BatchProcessing/SlaMonitoringService.cs`

### API & Configuration
- `/backend/MyTrader.Api/Controllers/BatchProcessingController.cs`
- `/backend/MyTrader.Api/Middleware/HangfireAuthorizationFilter.cs`
- `/backend/MyTrader.Api/appsettings.BatchProcessing.json`

### Framework Integration
- Updated `Program.cs` with Hangfire configuration
- Added NuGet packages for Hangfire support
- Integrated SLA monitoring background service

## Support

For operational issues:
1. Check Hangfire dashboard: `/hangfire`
2. Review application logs
3. Monitor SLA compliance metrics
4. Use dead letter queue for failed jobs
5. Contact development team for system issues