# Yahoo Finance 5-Minute Data Collection System

## Overview

This implementation provides a comprehensive 5-minute intraday data collection system for the MyTrader project. It extends the existing Yahoo Finance integration to support real-time data collection during market hours for multiple asset classes (STOCK, CRYPTO, BIST).

## Key Features

### 1. Enhanced Yahoo Finance API Service
- **File**: `MyTrader.Core/Services/YahooFinanceApiService.cs`
- **New Method**: `GetIntradayDataAsync()` - Supports 5-minute interval data fetching
- **Enhanced Features**:
  - Support for multiple timeframes (1m, 2m, 5m, 15m, 30m, 1h)
  - JSON response parsing for intraday chart data
  - Proper error handling and rate limiting

### 2. Intraday Data Collection Service
- **File**: `MyTrader.Core/Services/YahooFinanceIntradayDataService.cs`
- **Purpose**: Handles 5-minute data collection with market hours awareness
- **Key Features**:
  - Market hours logic for BIST (10:00-18:00 TRT), NYSE/NASDAQ (9:30-16:00 ET), CRYPTO (24/7)
  - Batch processing for efficient API usage
  - Intelligent data validation and deduplication
  - Configurable retry mechanisms

### 3. Scheduled Background Service
- **File**: `MyTrader.Infrastructure/Services/YahooFinanceIntradayScheduledService.cs`
- **Purpose**: Runs every 5 minutes during market hours
- **Key Features**:
  - Precise timing (executes on 5-minute marks)
  - Market hours validation before execution
  - Timeout protection and failure tracking
  - Automatic alerting on consecutive failures

### 4. API Controller Extensions
- **File**: `MyTrader.Api/Controllers/YahooFinanceSyncController.cs`
- **New Endpoints**:
  - `POST /api/yahoofinancesync/intraday/sync` - Manual trigger
  - `GET /api/yahoofinancesync/intraday/status` - Service status
  - `GET /api/yahoofinancesync/intraday/market-hours` - Market hours status
  - `GET /api/yahoofinancesync/intraday/data/{market}/{symbol}` - Recent data
  - `GET /api/yahoofinancesync/intraday/test/{market}/{symbol}` - API connectivity test

### 5. Health Monitoring System
- **File**: `MyTrader.Api/Controllers/HealthController.cs`
- **Purpose**: Comprehensive health monitoring and system status
- **Endpoints**:
  - `GET /api/health` - Overall system health
  - `GET /api/health/intraday` - Intraday service health
  - `GET /api/health/market-hours` - Market status
  - `GET /api/health/ready` - Kubernetes readiness probe
  - `GET /api/health/live` - Kubernetes liveness probe

### 6. Service Registration & Configuration
- **File**: `MyTrader.Infrastructure/Extensions/YahooFinanceServiceExtensions.cs`
- **New Methods**:
  - `AddYahooFinanceIntradaySync()` - Register intraday services
  - `AddYahooFinanceCompleteSync()` - Register complete system
- **Health Checks**: Integrated health monitoring for both daily and intraday services

## Database Integration

### Data Storage
- Uses existing `HistoricalMarketData` table
- Sets `Timeframe = '5MIN'` for 5-minute data
- Includes precise timestamp for each data point
- Maintains data quality scores and validation

### Performance Considerations
- Indexes on `(SymbolTicker, MarketCode, Timeframe, Timestamp)`
- Efficient querying for recent data retrieval
- Deduplication logic to prevent duplicate records

## Market Hours Logic

### BIST (Turkey)
- **Trading Hours**: 10:00 AM - 6:00 PM Turkey Time (TRT)
- **Days**: Monday - Friday
- **Time Zone**: Turkey Standard Time

### NYSE/NASDAQ (US Markets)
- **Trading Hours**: 9:30 AM - 4:00 PM Eastern Time (ET)
- **Days**: Monday - Friday
- **Time Zone**: Eastern Standard Time

### CRYPTO
- **Trading Hours**: 24/7
- **No market hour restrictions**

## Configuration

### Sample Configuration (appsettings.json)
```json
{
  "YahooFinance": {
    "IntradaySync": {
      "BatchSize": 5,
      "BatchDelayMs": 500,
      "LookbackMinutes": 15,
      "MaxSymbolsPerMarket": 1000,
      "EnableBistSync": true,
      "EnableUSMarketsSync": true,
      "EnableCryptoSync": true
    },
    "IntradaySchedule": {
      "EnableIntradaySync": true,
      "MaxExecutionDuration": "00:04:00",
      "AlertAfterConsecutiveFailures": 3
    }
  }
}
```

## Error Handling & Resilience

### Retry Mechanisms
- Exponential backoff for API failures
- Configurable retry attempts (default: 2)
- Circuit breaker pattern for persistent failures

### Rate Limiting
- Respects Yahoo Finance API limits
- Configurable request intervals
- Batch processing to minimize API calls

### Failure Recovery
- Continues processing other symbols on individual failures
- Tracks consecutive failures for alerting
- Automatic service health monitoring

## Monitoring & Alerting

### Health Checks
- Service availability monitoring
- Performance metrics tracking
- Market hours validation
- Database connectivity checks

### Alerting
- Configurable webhook notifications
- Consecutive failure thresholds
- Performance degradation alerts

### Logging
- Structured logging with Serilog compatibility
- Debug, Information, Warning, Error levels
- Performance timing logs

## Performance Specifications

### Target Metrics
- **Collection Frequency**: Every 5 minutes during market hours
- **API Response Time**: < 2 seconds per symbol
- **Processing Time**: Complete within 4 minutes
- **Success Rate**: > 70% of symbols per market
- **Data Latency**: < 5 minutes from market time

### Resource Usage
- **Batch Size**: 5 symbols per batch (configurable)
- **Concurrent Requests**: Limited by rate limiting
- **Memory**: Minimal footprint with proper disposal
- **Database**: Efficient bulk inserts

## Deployment

### Service Registration
```csharp
// In Program.cs or Startup.cs
services.AddYahooFinanceCompleteSync(configuration);
```

### Required Dependencies
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Http
- Microsoft.EntityFrameworkCore
- System.Text.Json

### Health Check Integration
```csharp
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");
```

## API Usage Examples

### Manual Trigger
```bash
POST /api/yahoofinancesync/intraday/sync
```

### Get Service Status
```bash
GET /api/yahoofinancesync/intraday/status
```

### Check Market Hours
```bash
GET /api/yahoofinancesync/intraday/market-hours
```

### Get Recent Data
```bash
GET /api/yahoofinancesync/intraday/data/NYSE/AAPL?hours=2
```

## Files Created/Modified

### New Files
1. `MyTrader.Core/Services/YahooFinanceIntradayDataService.cs`
2. `MyTrader.Infrastructure/Services/YahooFinanceIntradayScheduledService.cs`
3. `MyTrader.Api/Controllers/HealthController.cs`
4. `YahooFinance5MinuteConfig.json` (sample configuration)

### Modified Files
1. `MyTrader.Core/Services/YahooFinanceApiService.cs` (added intraday methods)
2. `MyTrader.Api/Controllers/YahooFinanceSyncController.cs` (added intraday endpoints)
3. `MyTrader.Infrastructure/Extensions/YahooFinanceServiceExtensions.cs` (added intraday services)

## Security Considerations

### API Security
- Requires authentication for sync operations
- Rate limiting to prevent abuse
- Input validation for all parameters

### Data Protection
- No sensitive data exposure in logs
- Secure webhook configurations
- Proper error message sanitization

## Scalability

### Horizontal Scaling
- Stateless service design
- Database-based coordination
- Container-ready implementation

### Performance Tuning
- Configurable batch sizes
- Adjustable retry policies
- Market-specific optimizations

## Troubleshooting

### Common Issues
1. **Service Not Starting**: Check configuration validation
2. **API Failures**: Verify Yahoo Finance connectivity
3. **No Data Collection**: Check market hours logic
4. **Performance Issues**: Adjust batch sizes and timeouts

### Debugging
- Enable detailed logging
- Use health check endpoints
- Monitor service status API
- Check consecutive failure counts

## Future Enhancements

### Potential Improvements
1. **Multiple Intervals**: Support for 1-minute, 15-minute data
2. **Real-time Streaming**: WebSocket integration
3. **Advanced Caching**: Redis-based caching layer
4. **Machine Learning**: Anomaly detection for data quality
5. **Cross-Market Analysis**: Correlation monitoring

This implementation provides a robust, scalable, and maintainable solution for real-time 5-minute data collection that integrates seamlessly with the existing MyTrader architecture.