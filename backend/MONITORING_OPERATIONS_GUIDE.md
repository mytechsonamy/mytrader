# myTrader Monitoring & Observability Operations Guide

## Overview

This guide provides comprehensive instructions for monitoring, maintaining, and troubleshooting the myTrader application's observability infrastructure. It covers health checks, metrics, alerting, and operational procedures.

## Quick Start

### Accessing Monitoring Endpoints

- **Health Dashboard**: `http://localhost:5055/health-ui`
- **Health API**: `http://localhost:5055/health`
- **Prometheus Metrics**: `http://localhost:5055/metrics`
- **API Info**: `http://localhost:5055/`

### Key Health Check Endpoints

- **Overall Health**: `GET /health`
- **Critical Services**: `GET /health/critical`
- **Database Health**: `GET /health/database`
- **Real-time Services**: `GET /health/realtime`

## Service Level Objectives (SLOs)

### Availability SLOs

| Service | SLO | Measurement Window | Error Budget |
|---------|-----|-------------------|-------------|
| API Endpoints | 99.9% | Rolling 30 days | 43.2 minutes/month |
| Database | 99.95% | Rolling 30 days | 21.6 minutes/month |
| SignalR Hubs | 99.5% | Rolling 30 days | 3.6 hours/month |
| Market Data Feed | 99.0% | Rolling 30 days | 7.2 hours/month |

### Performance SLOs

| Service | Metric | Target | Measurement |
|---------|--------|--------|-------------|
| API Response Time | p95 latency | < 2 seconds | Rolling 7 days |
| Database Queries | p95 latency | < 1 second | Rolling 7 days |
| Market Data Freshness | Data age | < 5 minutes (crypto) | Point-in-time |
| SignalR Message Delivery | p95 latency | < 500ms | Rolling 1 hour |

### Data Quality SLOs

| Metric | Target | Measurement |
|--------|--------|-------------|
| Market Data Accuracy | > 99.9% | Daily validation |
| API Success Rate | > 95% | Rolling 1 hour |
| WebSocket Uptime | > 99% | Rolling 24 hours |

## Monitoring Architecture

### Health Check System

The application implements a multi-layered health check system:

1. **Built-in ASP.NET Core Health Checks**
   - PostgreSQL database connectivity
   - Memory usage monitoring
   - System resource checks

2. **Custom Health Checks**
   - `SignalRHealthCheck`: Monitors SignalR hub connectivity and performance
   - `WebSocketHealthCheck`: Tracks WebSocket connections and data flow
   - `MarketDataHealthCheck`: Validates market data freshness and quality
   - `ApiEndpointHealthCheck`: Monitors API performance and error rates

3. **Background Monitoring**
   - `HealthMonitoringService`: Continuously monitors health checks
   - Integrates with alerting system for proactive notifications

### Metrics Collection

#### Prometheus Metrics

The system exports metrics in Prometheus format at `/metrics`:

**System Metrics:**
- `mytrader_memory_bytes`: Total memory usage
- `mytrader_gc_heap_bytes`: Garbage collector heap size
- `mytrader_uptime_seconds`: Application uptime

**API Metrics:**
- `mytrader_api_requests_total`: Total API requests by endpoint
- `mytrader_api_errors_total`: Total API errors by endpoint
- `mytrader_api_request_duration_seconds`: Request duration histogram

**Database Metrics:**
- `mytrader_database_connected`: Database connection status
- `mytrader_market_data_records`: Market data record counts by asset class
- `mytrader_database_query_duration_seconds`: Database query performance

**Real-time Metrics:**
- `mytrader_signalr_connections`: Active SignalR connections by hub
- `mytrader_websocket_messages_total`: WebSocket messages received
- `mytrader_websocket_reconnections_total`: WebSocket reconnection count

**Market Data Metrics:**
- `mytrader_market_data_freshness_seconds`: Age of most recent data
- `mytrader_market_data_quality_score`: Data quality score (0-1)
- `mytrader_market_symbols_count`: Number of tracked symbols

#### Health Status Metrics

- `mytrader_health_check_status`: Health check results (-1=degraded, 0=unhealthy, 1=healthy)

### Alerting System

#### Alert Rules Configuration

The system includes pre-configured alert rules for critical scenarios:

1. **Database Connection Alert**
   - **Condition**: Database health check fails
   - **Severity**: Critical
   - **Repeat**: Every 5 minutes

2. **API High Error Rate Alert**
   - **Condition**: Error rate > 5%
   - **Severity**: Error
   - **Repeat**: Every 15 minutes

3. **Market Data Stale Alert**
   - **Condition**: Market data age > threshold
   - **Severity**: Warning
   - **Repeat**: Every 10 minutes

4. **SignalR Connections Down Alert**
   - **Condition**: SignalR hubs unhealthy
   - **Severity**: Error
   - **Repeat**: Every 5 minutes

5. **WebSocket Disconnected Alert**
   - **Condition**: WebSocket connections unhealthy
   - **Severity**: Warning
   - **Repeat**: Every 10 minutes

6. **High Memory Usage Alert**
   - **Condition**: Memory usage > 1GB
   - **Severity**: Warning
   - **Repeat**: Every 15 minutes

#### Notification Channels

**Console Notifications:**
- Real-time alerts displayed in console
- Minimum severity: Warning

**Log File Notifications:**
- All alerts logged to application logs
- Structured logging with context

**Webhook Notifications:**
- Configurable webhook endpoints (Slack, Discord, etc.)
- JSON payload with alert details
- Minimum severity: Configurable

## Operational Procedures

### Daily Operations Checklist

1. **Check System Health**
   ```bash
   curl http://localhost:5055/health | jq
   ```

2. **Review Critical Alerts**
   - Check console output for recent alerts
   - Review application logs for warnings/errors

3. **Validate Market Data**
   ```bash
   curl http://localhost:5055/health/database | jq
   ```

4. **Monitor Performance**
   ```bash
   curl http://localhost:5055/metrics | grep mytrader_api_request_duration
   ```

### Weekly Operations Checklist

1. **SLO Compliance Review**
   - Calculate availability metrics
   - Review error budget consumption
   - Identify trends in performance degradation

2. **Capacity Planning**
   - Review memory usage trends
   - Monitor database growth
   - Assess connection counts

3. **Alert Tuning**
   - Review alert frequency and accuracy
   - Adjust thresholds based on actual performance
   - Remove noisy alerts

### Incident Response Procedures

#### Critical Service Outage (SLO Breach)

1. **Immediate Response (< 5 minutes)**
   - Check overall system health: `GET /health`
   - Identify affected services from health check results
   - Check recent deployments or configuration changes

2. **Assessment (< 15 minutes)**
   - Review application logs for error patterns
   - Check database connectivity and performance
   - Validate external service dependencies

3. **Resolution Actions**
   - **Database Issues**: Check connection strings, restart if needed
   - **API Performance**: Check for resource constraints, scale if possible
   - **SignalR Issues**: Restart application, check connection limits
   - **Market Data Issues**: Verify external API connections, check rate limits

4. **Recovery Validation**
   - Confirm all health checks return to healthy status
   - Verify SLO metrics are recovering
   - Monitor for 30 minutes to ensure stability

#### High Error Rate Alert

1. **Investigation Steps**
   ```bash
   # Check API endpoint health
   curl http://localhost:5055/health/api | jq

   # Get detailed metrics
   curl http://localhost:5055/metrics | grep error_rate

   # Review recent logs
   tail -n 100 /path/to/logs/mytrader.log | grep ERROR
   ```

2. **Common Causes & Solutions**
   - **Authentication errors**: Check JWT configuration
   - **Database timeouts**: Check connection pool settings
   - **External API failures**: Verify third-party service status
   - **Rate limiting**: Check for unusual traffic patterns

#### Market Data Staleness

1. **Check Data Sources**
   ```bash
   # Verify WebSocket health
   curl http://localhost:5055/health/realtime | jq

   # Check market data freshness
   curl http://localhost:5055/api/health/intraday | jq
   ```

2. **Resolution Steps**
   - Verify external API connectivity (Binance, Yahoo Finance)
   - Check WebSocket connection status
   - Restart data ingestion services if needed
   - Validate database write permissions

### Performance Optimization

#### API Response Time Optimization

1. **Identify Slow Endpoints**
   ```bash
   curl http://localhost:5055/metrics | grep duration | sort -n
   ```

2. **Database Query Optimization**
   - Review query execution plans
   - Check for missing indexes
   - Monitor connection pool usage

3. **Caching Strategies**
   - Implement response caching for static data
   - Use in-memory caching for frequently accessed data

#### Memory Usage Optimization

1. **Monitor GC Metrics**
   ```bash
   curl http://localhost:5055/metrics | grep gc_collections
   ```

2. **Optimization Actions**
   - Review object lifetimes and disposal patterns
   - Optimize data structures for memory efficiency
   - Implement connection pooling

### Troubleshooting Guide

#### Health Check Failures

**SignalR Health Check Failures:**
- Check hub registration in Program.cs
- Verify CORS configuration for WebSocket connections
- Check for authentication issues

**WebSocket Health Check Failures:**
- Verify Binance WebSocket service is running
- Check external network connectivity
- Review connection timeout settings

**Market Data Health Check Failures:**
- Validate database table structure
- Check for data ingestion service issues
- Verify external API rate limits

**Database Health Check Failures:**
- Check PostgreSQL service status
- Verify connection string configuration
- Check database disk space and resources

#### Connection Issues

**SignalR Connection Problems:**
- Enable detailed logging for SignalR
- Check client-side connection configuration
- Verify hub method names and signatures

**WebSocket Connection Problems:**
- Check network firewall rules
- Verify WebSocket upgrade headers
- Review proxy configuration if applicable

#### Performance Issues

**Slow API Response Times:**
- Check database query performance
- Review endpoint implementation for inefficiencies
- Monitor system resource usage

**High Memory Usage:**
- Force garbage collection and monitor trends
- Review object disposal patterns
- Check for memory leaks in background services

### Monitoring Configuration

#### Environment Variables

```bash
# Logging configuration
SERILOG__MINIMUMLEVEL=Information
SERILOG__WRITETO__0__NAME=Console
SERILOG__WRITETO__1__NAME=File
SERILOG__WRITETO__1__ARGS__PATH=logs/mytrader-.log

# Health check intervals
HEALTH_CHECK_INTERVAL_SECONDS=30
ALERT_CHECK_INTERVAL_SECONDS=60

# Alert thresholds
ALERT_API_ERROR_RATE_THRESHOLD=0.05
ALERT_MEMORY_THRESHOLD_MB=1024
ALERT_RESPONSE_TIME_THRESHOLD_MS=5000
```

#### Alert Configuration

To enable webhook notifications, update `appsettings.json`:

```json
{
  "AlertingConfiguration": {
    "WebhookNotifications": {
      "Enabled": true,
      "Url": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
      "MinSeverity": "Warning",
      "Headers": {
        "Content-Type": "application/json"
      }
    }
  }
}
```

## Integration with External Monitoring

### Prometheus Integration

1. **Configure Prometheus** to scrape `/metrics` endpoint
2. **Set up Grafana** dashboards using the exported metrics
3. **Create alerting rules** in Prometheus AlertManager

### Example Grafana Queries

```promql
# API Request Rate
rate(mytrader_api_requests_total[5m])

# Error Rate
rate(mytrader_api_errors_total[5m]) / rate(mytrader_api_requests_total[5m])

# Response Time P95
histogram_quantile(0.95, rate(mytrader_api_request_duration_seconds_bucket[5m]))

# Memory Usage
mytrader_memory_bytes / 1024 / 1024

# SignalR Connection Count
mytrader_signalr_connections
```

### Log Aggregation

The application uses Serilog with Loki integration:
- Logs are sent to Grafana Loki at `http://localhost:3100`
- Structured logging with consistent field names
- Log levels: Debug, Information, Warning, Error, Critical

## Security Considerations

### Monitoring Endpoint Security

- Health check endpoints should be accessible internally only
- Consider authentication for sensitive metrics endpoints
- Use HTTPS in production environments
- Rate limit monitoring endpoints to prevent abuse

### Sensitive Data in Metrics

- Avoid exposing user data in metric labels
- Sanitize error messages in alerts
- Use generic identifiers instead of personal information

## Backup and Recovery

### Configuration Backup

Regularly backup:
- Alert rule configurations
- Dashboard definitions
- Monitoring endpoint configurations
- Log aggregation settings

### Recovery Procedures

1. **Health Check Service Recovery**
   - Restart application if health checks fail to start
   - Verify database connectivity before service startup
   - Check dependency injection configuration

2. **Metrics Export Recovery**
   - Validate Prometheus endpoint accessibility
   - Restart metrics collection services
   - Verify metric format compatibility

## Support and Escalation

### Alert Severity Levels

- **Critical**: System is down or major functionality unavailable
- **Error**: Significant degradation affecting users
- **Warning**: Minor issues or approaching thresholds
- **Info**: Informational alerts and recovery notifications

### Escalation Matrix

| Severity | Initial Response | Escalation Time | Escalation Target |
|----------|-----------------|-----------------|-------------------|
| Critical | Immediate | 15 minutes | Engineering Manager |
| Error | 15 minutes | 1 hour | Senior Developer |
| Warning | 1 hour | 4 hours | Development Team |
| Info | Best effort | N/A | Monitoring Review |

---

This guide should be reviewed and updated quarterly to ensure accuracy and relevance to current system architecture and operational requirements.