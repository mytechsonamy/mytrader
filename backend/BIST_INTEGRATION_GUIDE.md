# BIST Market Data Integration Guide

## Overview

This guide provides complete instructions for integrating high-performance BIST (Borsa Istanbul) market data into your myTrader application. The integration delivers sub-100ms response times through optimized database queries, intelligent caching, and efficient API design.

## Features

### ✅ **Performance Optimized**
- Sub-10ms individual stock queries
- Sub-50ms batch market data retrieval
- Sub-100ms market overview queries
- Sub-75ms top movers queries

### ✅ **Intelligent Caching**
- Multi-level caching strategy
- Automatic cache warming
- Performance-aware cache invalidation
- Memory usage optimization

### ✅ **Comprehensive API**
- RESTful endpoints matching Alpaca format
- Real-time market status
- Sector performance analysis
- Advanced search capabilities

### ✅ **Production Ready**
- Performance monitoring and alerting
- Health checks and diagnostics
- Automated background refresh
- Error handling and resilience

## Quick Start

### 1. Database Setup

Run the BIST optimization schema:

```bash
# Apply the BIST schema and indexes
psql -d mytrader -f MyTrader.Infrastructure/Data/BistOptimizedSchema.sql
```

### 2. Service Configuration

Add to your `appsettings.json`:

```json
{
  "BistConfiguration": {
    "EnableCaching": true,
    "CacheExpirySeconds": 30,
    "MaxConcurrentQueries": 10,
    "EnablePerformanceLogging": true,
    "DefaultSymbols": [
      "THYAO", "AKBNK", "ISCTR", "ASELS", "BIMAS",
      "EREGL", "KRDMD", "SASA", "TOASO", "PETKM"
    ]
  },
  "BistPerformance": {
    "EnableMetricsCollection": true,
    "LogMetricsSummary": true,
    "MetricsIntervalMinutes": 5,
    "AutoResetIntervalHours": 24,
    "MinCacheHitRatio": 0.8,
    "MaxErrorRate": 0.05
  }
}
```

### 3. Service Registration

In your `Program.cs`:

```csharp
using MyTrader.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add BIST integration
builder.Services.AddBistIntegration(builder.Configuration);

var app = builder.Build();

// Verify BIST integration health on startup
var healthCheck = await app.Services.VerifyBistIntegration();
if (!healthCheck.OverallHealthy)
{
    app.Logger.LogWarning("BIST integration issues: {Issues}",
        string.Join(", ", healthCheck.Issues));
}
```

### 4. Frontend Integration

The BIST endpoints use the same `MarketDataDto` format as Alpaca:

```typescript
// Frontend usage example
const bistData = await fetch('/api/market-data/bist?symbols=THYAO,AKBNK')
  .then(res => res.json());

// Response format matches existing Alpaca integration
interface MarketDataDto {
  symbol: string;
  name: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  high24h: number;
  low24h: number;
  lastUpdated: string;
  assetClass: "BIST";
  currency: "TRY";
}
```

## API Endpoints

### Core Market Data

| Endpoint | Description | Target Response Time |
|----------|-------------|---------------------|
| `GET /api/market-data/bist` | All BIST stocks | < 50ms |
| `GET /api/market-data/bist/{symbol}` | Individual stock | < 10ms |
| `GET /api/market-data/bist/overview` | Market overview | < 100ms |
| `GET /api/market-data/bist/top-movers` | Top gainers/losers | < 75ms |

### Advanced Features

| Endpoint | Description |
|----------|-------------|
| `GET /api/market-data/bist/sectors` | Sector performance |
| `GET /api/market-data/bist/search?q={query}` | Symbol search |
| `GET /api/market-data/bist/{symbol}/history` | Historical data |
| `GET /api/market-data/bist/status` | Market open/closed |

### Monitoring & Health

| Endpoint | Description | Auth Required |
|----------|-------------|---------------|
| `GET /api/market-data/bist/health` | Service health | ✅ |
| `POST /api/market-data/bist/refresh` | Manual cache refresh | ✅ |

## Example Usage

### 1. Get Top BIST Stocks

```bash
curl "https://yourapi.com/api/market-data/bist?limit=20"
```

```json
{
  "success": true,
  "data": [
    {
      "symbol": "THYAO",
      "name": "Turkish Airlines",
      "price": 85.50,
      "change": 2.30,
      "changePercent": 2.76,
      "volume": 15420000,
      "high24h": 86.00,
      "low24h": 82.75,
      "lastUpdated": "2024-01-15T14:30:00Z",
      "assetClass": "BIST",
      "currency": "TRY"
    }
  ],
  "message": "Retrieved 20 BIST stocks"
}
```

### 2. Get Market Overview

```bash
curl "https://yourapi.com/api/market-data/bist/overview"
```

```json
{
  "success": true,
  "data": {
    "totalStocks": 485,
    "totalVolume": 25750000000,
    "totalMarketCap": 8420000000000,
    "avgChangePercent": 1.25,
    "gainersCount": 287,
    "losersCount": 165,
    "unchangedCount": 33,
    "lastUpdated": "2024-01-15T14:30:00Z",
    "marketStatus": "OPEN",
    "currency": "TRY"
  }
}
```

### 3. Search Stocks

```bash
curl "https://yourapi.com/api/market-data/bist/search?q=türk"
```

```json
{
  "success": true,
  "data": [
    {
      "symbol": "THYAO",
      "name": "Turkish Airlines",
      "fullName": "Türk Hava Yolları",
      "sector": "Transportation",
      "currentPrice": 85.50,
      "changePercent": 2.76,
      "searchRank": 0.95
    }
  ]
}
```

## Performance Benchmarks

### Target Performance (99th percentile)

| Operation | Target | Typical |
|-----------|--------|---------|
| Individual stock | 10ms | 3-5ms |
| Batch 50 stocks | 50ms | 15-25ms |
| Market overview | 100ms | 35-45ms |
| Top movers | 75ms | 25-35ms |
| Search | 50ms | 10-20ms |

### Cache Performance

| Metric | Target | Typical |
|--------|--------|---------|
| Cache hit ratio | >80% | 85-95% |
| Cache warming | <5s | 2-3s |
| Memory usage | <100MB | 40-60MB |

## Database Optimization

### Materialized Views

The system uses three key materialized views:

1. **`mv_bist_dashboard`** - Real-time stock data (refreshed every 30s)
2. **`mv_bist_top_movers`** - Top gainers/losers (refreshed every 5min)
3. **`mv_bist_sectors`** - Sector performance (refreshed hourly)

### Index Strategy

Critical indexes for performance:

```sql
-- Ultra-fast symbol lookup
CREATE INDEX idx_symbols_bist_ticker_active
ON symbols (ticker, is_active, is_popular)
WHERE asset_class = 'BIST';

-- Current day data queries
CREATE INDEX idx_historical_bist_current_day
ON historical_market_data (symbol_ticker, close_price, volume)
WHERE data_source = 'BIST' AND trade_date = CURRENT_DATE;

-- Performance ranking
CREATE INDEX idx_historical_bist_ranking
ON historical_market_data (trade_date DESC, volume DESC, market_cap DESC)
WHERE data_source = 'BIST';
```

## Monitoring and Diagnostics

### Health Check

```bash
curl -H "Authorization: Bearer {token}" \
  "https://yourapi.com/api/market-data/bist/health"
```

### Performance Metrics

The system automatically logs performance metrics every 5 minutes:

```json
{
  "timestamp": "2024-01-15T14:30:00Z",
  "cache": {
    "hit_ratio": 0.89,
    "total_hits": 1247,
    "total_misses": 153
  },
  "operations": [
    {
      "operation": "GetBistStockData",
      "total_queries": 342,
      "avg_time_ms": 4.2,
      "p95_time_ms": 8.5,
      "error_rate": 0.0,
      "healthy": true
    }
  ],
  "health": {
    "overall_healthy": true,
    "cache_healthy": true,
    "performance_healthy": true,
    "error_rate_healthy": true,
    "issues": []
  }
}
```

## Production Deployment

### 1. Database Preparation

```bash
# Run optimization script
psql -d mytrader_production -f BistOptimizedSchema.sql

# Verify indexes
psql -d mytrader_production -c "
SELECT schemaname, tablename, indexname, idx_scan
FROM pg_stat_user_indexes
WHERE tablename LIKE '%bist%' OR tablename IN ('symbols', 'historical_market_data')
ORDER BY idx_scan DESC;"
```

### 2. Environment Configuration

Production `appsettings.Production.json`:

```json
{
  "BistConfiguration": {
    "EnableCaching": true,
    "CacheExpirySeconds": 15,
    "MaxConcurrentQueries": 20,
    "EnablePerformanceLogging": false
  },
  "BistPerformance": {
    "EnableMetricsCollection": true,
    "LogMetricsSummary": false,
    "MetricsIntervalMinutes": 15,
    "MinCacheHitRatio": 0.85,
    "MaxErrorRate": 0.02
  },
  "Logging": {
    "LogLevel": {
      "MyTrader.Infrastructure.Services.BistMarketDataService": "Warning",
      "MyTrader.Infrastructure.Monitoring.BistPerformanceMonitor": "Information"
    }
  }
}
```

### 3. Monitoring Setup

Set up alerts for:

- Cache hit ratio < 80%
- P95 query time > threshold
- Error rate > 2%
- Memory usage > 200MB

### 4. Load Testing

```bash
# Test individual stock endpoint
ab -n 1000 -c 50 "https://yourapi.com/api/market-data/bist/THYAO"

# Test batch endpoint
ab -n 500 -c 25 "https://yourapi.com/api/market-data/bist?limit=50"

# Test overview endpoint
ab -n 200 -c 10 "https://yourapi.com/api/market-data/bist/overview"
```

Expected results:
- 95% of requests < target response time
- 0% error rate
- Consistent performance under load

## Troubleshooting

### Common Issues

**Slow Queries**
```bash
# Check query performance
SELECT query, calls, mean_time, total_time
FROM pg_stat_statements
WHERE query LIKE '%bist%'
ORDER BY total_time DESC;
```

**Low Cache Hit Ratio**
```bash
# Check cache configuration
curl -H "Authorization: Bearer {token}" \
  "https://yourapi.com/api/market-data/bist/health"
```

**Memory Issues**
- Reduce cache size limit
- Increase compaction percentage
- Monitor GC pressure

### Performance Tuning

**Database Level**
```sql
-- Update table statistics
ANALYZE symbols;
ANALYZE historical_market_data;

-- Refresh materialized views
SELECT refresh_bist_views();
```

**Application Level**
- Adjust cache expiry times
- Tune concurrent query limits
- Monitor memory allocation

## Integration with Frontend

### React Dashboard Component

```typescript
import { useEffect, useState } from 'react';

interface BistMarketData {
  symbol: string;
  name: string;
  price: number;
  changePercent: number;
  volume: number;
}

export function BistDashboard() {
  const [bistData, setBistData] = useState<BistMarketData[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchBistData = async () => {
      try {
        const response = await fetch('/api/market-data/bist?limit=20');
        const result = await response.json();
        setBistData(result.data);
      } catch (error) {
        console.error('Failed to fetch BIST data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchBistData();
    const interval = setInterval(fetchBistData, 30000); // Refresh every 30s

    return () => clearInterval(interval);
  }, []);

  if (loading) return <div>Loading BIST data...</div>;

  return (
    <div className="bist-dashboard">
      <h2>BIST Stocks</h2>
      <div className="stock-grid">
        {bistData.map(stock => (
          <div key={stock.symbol} className="stock-card">
            <h3>{stock.symbol}</h3>
            <p>{stock.name}</p>
            <div className="price">
              ₺{stock.price.toFixed(2)}
              <span className={stock.changePercent >= 0 ? 'positive' : 'negative'}>
                {stock.changePercent >= 0 ? '+' : ''}{stock.changePercent.toFixed(2)}%
              </span>
            </div>
            <div className="volume">
              Volume: {stock.volume.toLocaleString()}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

## Support and Maintenance

### Regular Maintenance Tasks

**Daily**
- Monitor performance metrics
- Check error rates
- Verify cache health

**Weekly**
- Review slow query log
- Update table statistics
- Archive old data if needed

**Monthly**
- Performance benchmark testing
- Index usage analysis
- Capacity planning review

### Getting Help

For implementation questions or performance issues:

1. Check application logs for BIST-related errors
2. Review performance metrics in monitoring dashboard
3. Verify database query performance
4. Check cache hit ratios and memory usage

The integration is designed to be self-monitoring and self-healing, with comprehensive logging and metrics to help diagnose any issues quickly.