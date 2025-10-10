# Alpaca Monitoring - Quick Reference Guide

**Last Updated:** 2025-10-09
**For:** On-call Engineers, Support Team

---

## Emergency Quick Links

### 🔴 Critical Alerts
- **PagerDuty:** https://mytrader.pagerduty.com
- **Grafana Operations Dashboard:** https://grafana.internal/d/alpaca-operations
- **Runbook:** `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/ALPACA_RUNBOOK.md`

### 📞 Emergency Contacts
- **SRE On-Call:** PagerDuty auto-pages
- **Backend On-Call:** PagerDuty auto-pages
- **Engineering Manager:** [Phone/Slack]
- **Alpaca Support:** support@alpaca.markets

---

## Quick Health Check

```bash
# Backend API health
curl http://localhost:5002/health

# Alpaca connection
curl http://localhost:5002/api/health/alpaca

# DataSourceRouter status
curl http://localhost:5002/api/health/datasource

# All stock data sources
curl http://localhost:5002/api/health/stocks
```

**Expected Response (Healthy):**
```json
{
  "status": "Healthy",
  "alpacaStatus": {
    "connected": true,
    "authenticated": true
  }
}
```

---

## Common Issues & Quick Fixes

### Issue 1: Both Sources Down ⛔
**Alert:** AlpacaBothSourcesUnavailable (CRITICAL)

**Quick Check:**
```bash
# Is Alpaca down?
curl http://localhost:5002/api/health/alpaca | jq '.alpacaStatus.connected'

# Is Yahoo down?
curl http://localhost:5002/api/health/datasource | jq '.yahooStatus.isHealthy'
```

**Quick Fix:**
1. Check external status pages:
   - Alpaca: https://status.alpaca.markets
   - Yahoo: https://downdetector.com/status/yahoo
2. If both external services are up, restart backend:
   ```bash
   kubectl rollout restart deployment/mytrader-api
   ```
3. If persistent, escalate immediately

**Full Procedure:** RUNBOOK-001 in ALPACA_RUNBOOK.md

---

### Issue 2: Alpaca Disconnected (Fallback Active) ⚠️
**Alert:** AlpacaDisconnected (HIGH)

**Quick Check:**
```bash
curl http://localhost:5002/api/health/datasource | jq '.connectionState'
# Expected: "FALLBACK_ACTIVE"
```

**Quick Fix:**
1. Check if Alpaca is reconnecting:
   ```bash
   kubectl logs -l app=mytrader-api --tail=50 | grep "reconnect"
   ```
2. Force reconnection:
   ```bash
   curl -X POST http://localhost:5002/api/health/alpaca/reconnect
   ```
3. Monitor for automatic recovery (should occur within 5 minutes)

**Full Procedure:** RUNBOOK-002 in ALPACA_RUNBOOK.md

---

### Issue 3: High Latency 🐢
**Alert:** AlpacaHighLatency (HIGH)

**Quick Check:**
```bash
# Check Prometheus for P95 latency
curl 'http://prometheus:9090/api/v1/query?query=histogram_quantile(0.95,sum(rate(alpaca_message_latency_seconds_bucket[5m]))by(le))'
```

**Quick Fix:**
1. Check system resources:
   ```bash
   top -bn1 | head -20  # CPU
   free -m              # Memory
   ```
2. Check database:
   ```bash
   psql -h localhost -p 5434 -U postgres -d mytrader_db -c "SELECT count(*) FROM pg_stat_activity WHERE state='active';"
   ```
3. If database overloaded, consider scaling

**Full Procedure:** RUNBOOK-003 in ALPACA_RUNBOOK.md

---

## Key Metrics at a Glance

### Connection Status
```bash
# Prometheus query
alpaca_connection_status{state="Connected"}
# Expected: 1 (connected), 0 (disconnected)
```

### Message Rate
```bash
# Prometheus query
sum(rate(alpaca_messages_received_total[1m])) * 60
# Expected: 50-200 messages/minute (depends on symbols)
```

### P95 Latency
```bash
# Prometheus query
histogram_quantile(0.95, sum(rate(alpaca_message_latency_seconds_bucket[5m])) by (le))
# Expected: <2 seconds (SLO target)
```

### Error Rate
```bash
# Prometheus query
(sum(rate(alpaca_errors_total[5m])) / sum(rate(alpaca_messages_received_total[5m]))) * 100
# Expected: <1%
```

---

## Dashboard URLs

| Dashboard | Purpose | URL |
|-----------|---------|-----|
| **Operations** | Real-time status | https://grafana.internal/d/alpaca-operations |
| **Performance** | Latency, throughput | https://grafana.internal/d/alpaca-performance |
| **Reliability** | Uptime, failovers | https://grafana.internal/d/alpaca-reliability |
| **Business** | Usage, costs | https://grafana.internal/d/alpaca-business |

---

## SLO Quick Reference

| SLO | Target | Current Status | Action Required |
|-----|--------|----------------|-----------------|
| Service Availability | ≥99.5% | Check dashboard | If <99.5%, escalate |
| Alpaca Uptime | ≥98% | Check dashboard | If <98%, investigate |
| P95 Latency | <2s | Check dashboard | If >2s, optimize |
| Validation Success | ≥99% | Check dashboard | If <99%, check logs |

**Check SLO Dashboard:** https://grafana.internal/d/alpaca-reliability

---

## Manual Failover Procedures

### Force Failover to Yahoo
```bash
# Use when Alpaca is unstable but you need immediate stability
curl -X POST http://localhost:5002/api/health/failover

# Verify state
curl http://localhost:5002/api/health/datasource | jq '.connectionState'
# Expected: "FALLBACK_ACTIVE"
```

### Force Reconnection to Alpaca
```bash
# Use after external issue is resolved
curl -X POST http://localhost:5002/api/health/alpaca/reconnect

# Wait for recovery (10-30 seconds)
sleep 30

# Verify state
curl http://localhost:5002/api/health/datasource | jq '.connectionState'
# Expected: "PRIMARY_ACTIVE"
```

---

## Log Queries (Loki/Grafana)

### View Errors (Last 1 Hour)
```logql
{app="mytrader-api", level="ERROR"} |= "Alpaca"
```

### View State Transitions
```logql
{app="mytrader-api"} |= "DataSourceRouter.*state transition"
```

### View Validation Failures
```logql
{app="mytrader-api"} |= "Validation failed"
```

---

## Escalation Path

| Time Elapsed | Action | Contact |
|--------------|--------|---------|
| **0-5 min** | Primary on-call responds | PagerDuty auto-pages |
| **5-15 min** | Secondary on-call engaged | PagerDuty escalates |
| **15-30 min** | Engineering Manager notified | Manual escalation |
| **30+ min** | Incident Commander assigned | VP Engineering |

---

## Performance Thresholds

| Metric | Green ✅ | Yellow ⚠️ | Red 🔴 |
|--------|---------|-----------|--------|
| **P95 Latency** | <1s | 1-2s | >2s |
| **Error Rate** | <0.5% | 0.5-1% | >1% |
| **Memory Usage** | <700 MB | 700-900 MB | >900 MB |
| **CPU Usage** | <15% | 15-50% | >50% |
| **Failover Frequency** | <2/day | 2-5/day | >5/day |

---

## Useful Commands Cheat Sheet

```bash
# View recent logs
kubectl logs -l app=mytrader-api --tail=100

# Follow logs in real-time
kubectl logs -l app=mytrader-api --tail=50 -f

# Search logs for errors
kubectl logs -l app=mytrader-api --tail=500 | grep -i error

# Check pod status
kubectl get pods -l app=mytrader-api

# Restart backend
kubectl rollout restart deployment/mytrader-api

# Check Prometheus targets
curl http://prometheus:9090/api/v1/targets

# Query Grafana API (dashboard list)
curl -H "Authorization: Bearer YOUR_TOKEN" http://grafana:3000/api/dashboards/home
```

---

## Configuration Quick Reference

### Alpaca Configuration Location
**File:** `backend/MyTrader.Api/appsettings.json`

```json
{
  "Alpaca": {
    "Streaming": {
      "Enabled": true,
      "WebSocketUrl": "wss://stream.data.alpaca.markets/v2/iex",
      "MaxSymbols": 30,
      "MessageTimeoutSeconds": 30
    }
  },
  "FeatureFlags": {
    "EnableAlpacaStreaming": true
  }
}
```

### Disable Alpaca (Emergency Rollback)
```bash
# Edit configmap
kubectl edit configmap mytrader-config

# Set: EnableAlpacaStreaming: false
# Save and restart
kubectl rollout restart deployment/mytrader-api
```

---

## Document References

| Document | Location | Purpose |
|----------|----------|---------|
| **Full Runbook** | `ALPACA_RUNBOOK.md` | Detailed incident procedures |
| **SLO Definitions** | `ALPACA_SLO_DEFINITIONS.md` | SLO targets and error budgets |
| **Dashboards** | `ALPACA_MONITORING_DASHBOARDS.md` | Dashboard specifications |
| **Alert Rules** | `ALPACA_ALERT_RULES.md` | Alert definitions |
| **Architecture** | `ALPACA_MONITORING_ARCHITECTURE.md` | Implementation details |

---

## Training Resources

- **Runbook Walkthrough Video:** [Link TBD]
- **Dashboard Demo:** [Link TBD]
- **On-Call Training Slides:** [Link TBD]
- **Incident Response Drill:** Schedule monthly drills

---

**Keep this guide handy during on-call shifts!**

**Last Updated:** 2025-10-09 | **Next Review:** 2025-11-09
