# Alpaca Streaming Integration - Operations Runbook

**Document Version:** 1.0
**Date:** 2025-10-09
**Owner:** SRE Team
**Audience:** On-call engineers, Support team, DevOps

---

## Executive Summary

This runbook provides step-by-step procedures for responding to incidents, troubleshooting common issues, and performing operational tasks for the Alpaca streaming integration.

---

## Table of Contents

1. [Quick Reference](#1-quick-reference)
2. [Incident Response Procedures](#2-incident-response-procedures)
3. [Troubleshooting Guides](#3-troubleshooting-guides)
4. [Operational Tasks](#4-operational-tasks)
5. [Escalation Procedures](#5-escalation-procedures)
6. [Contact Information](#6-contact-information)

---

## 1. Quick Reference

### 1.1 Critical Commands

```bash
# Check service health
curl http://localhost:5002/api/health/alpaca
curl http://localhost:5002/api/health/datasource
curl http://localhost:5002/api/health/stocks

# View recent logs (last 100 lines)
kubectl logs -l app=mytrader-api --tail=100
# Or without Kubernetes:
tail -100 /var/log/mytrader/application.log

# Check Alpaca connection status
curl http://localhost:5002/api/health/alpaca | jq '.alpacaStatus.connected'

# Force manual failover to Yahoo
curl -X POST http://localhost:5002/api/health/failover

# Force Alpaca reconnection
curl -X POST http://localhost:5002/api/health/alpaca/reconnect

# Check database connectivity
psql -h localhost -p 5434 -U postgres -d mytrader_db -c "SELECT 1;"

# View DataSourceRouter state
kubectl logs -l app=mytrader-api | grep "DataSourceRouter.*state"
```

---

### 1.2 Key Endpoints

| Endpoint | Purpose | Expected Response Time |
|----------|---------|------------------------|
| `/api/health/alpaca` | Alpaca connection status | <50ms |
| `/api/health/datasource` | Router state + metrics | <50ms |
| `/api/health/stocks` | Combined health check | <50ms |
| `/health` | Overall system health | <100ms |
| `/api/symbols` | List of active symbols | <200ms |

---

### 1.3 External Status Pages

- **Alpaca Status:** https://status.alpaca.markets
- **Yahoo Finance:** No official status page (check https://downdetector.com/status/yahoo)
- **PostgreSQL:** https://www.postgresql.org/support/

---

## 2. Incident Response Procedures

### RUNBOOK-001: Both Data Sources Unavailable

**Alert:** AlpacaBothSourcesUnavailable (CRITICAL)
**Impact:** Complete loss of stock data streaming
**Response Time:** <5 minutes

#### Symptoms
- DataSourceRouter in BOTH_UNAVAILABLE state
- No stock price updates in frontend
- Health endpoint returns "Unhealthy"
- Users report seeing no data

#### Diagnostic Steps

**Step 1: Verify the alert is legitimate**
```bash
# Check health endpoint
curl http://localhost:5002/api/health/stocks

# Expected output if both down:
# {
#   "status": "Unhealthy",
#   "currentDataSource": "None",
#   "alpaca": { "connected": false },
#   "yahoo": { "healthy": false }
# }
```

**Step 2: Check Alpaca status**
```bash
# Check Alpaca connection
curl http://localhost:5002/api/health/alpaca

# Expected fields to check:
# - alpacaStatus.connected: false
# - alpacaStatus.lastError: "<error message>"
```

**Step 3: Check external status pages**
```bash
# Open in browser:
open https://status.alpaca.markets

# Check for:
# - Service outages
# - Maintenance windows
# - Degraded performance
```

**Step 4: Check Yahoo Finance**
```bash
# Test Yahoo Finance API directly
curl "https://query1.finance.yahoo.com/v8/finance/chart/AAPL"

# Expected: JSON response with price data
# If error: Yahoo Finance is down
```

**Step 5: Check backend logs**
```bash
# View last 200 lines for errors
kubectl logs -l app=mytrader-api --tail=200 | grep -i error

# Look for:
# - "Authentication failed"
# - "Connection timeout"
# - "Network unreachable"
# - "Database connection failed"
```

**Step 6: Check database connectivity**
```bash
# Test PostgreSQL connection
psql -h localhost -p 5434 -U postgres -d mytrader_db -c "SELECT COUNT(*) FROM symbols WHERE asset_class='STOCK';"

# Expected: Returns count of stock symbols
# If error: Database is down (escalate)
```

#### Resolution Steps

**Scenario A: Alpaca down, Yahoo should work**

```bash
# 1. Check Yahoo service logs
kubectl logs -l app=mytrader-api | grep "YahooFinancePollingService"

# 2. If Yahoo service stopped, restart backend
kubectl rollout restart deployment/mytrader-api

# 3. Monitor recovery
watch -n 5 'curl -s http://localhost:5002/api/health/datasource | jq .connectionState'
```

**Scenario B: Both external services down (rare)**

```bash
# 1. Verify this is truly an external issue
ping 8.8.8.8  # Check internet connectivity

# 2. Check firewall rules
sudo iptables -L | grep -i alpaca

# 3. Check DNS resolution
nslookup stream.data.alpaca.markets
nslookup query1.finance.yahoo.com

# 4. If local network issue, contact network team
# 5. If external services down, notify users and wait for recovery
```

**Scenario C: Configuration issue**

```bash
# 1. Check configuration
kubectl get configmap mytrader-config -o yaml

# 2. Verify Alpaca API keys (DO NOT log keys)
kubectl get secret alpaca-credentials -o jsonpath='{.data.api-key}' | base64 -d | wc -c
# Expected: >20 characters

# 3. If keys are invalid, rotate them
kubectl create secret generic alpaca-credentials \
  --from-literal=api-key="NEW_KEY" \
  --from-literal=api-secret="NEW_SECRET" \
  --dry-run=client -o yaml | kubectl apply -f -

# 4. Restart service
kubectl rollout restart deployment/mytrader-api
```

#### Communication Template

**Initial Notification (within 5 minutes):**
```
INCIDENT DECLARED: Stock Data Unavailable

Status: CRITICAL
Start Time: 10:15:32 UTC
Impact: All users unable to see stock prices (30 symbols affected)

Investigation:
- Alpaca streaming: DOWN
- Yahoo Finance fallback: DOWN
- Root cause: Under investigation

Next Update: In 15 minutes or when resolved

Incident Commander: [Your Name]
```

**Resolution Notification:**
```
INCIDENT RESOLVED: Stock Data Restored

Status: RESOLVED
Start Time: 10:15:32 UTC
End Time: 10:38:45 UTC
Duration: 23 minutes
Impact: 47 users affected

Root Cause: Network firewall rule blocking WebSocket connections

Resolution:
- Firewall rule removed
- Alpaca connection restored
- DataSourceRouter in PRIMARY_ACTIVE state

Next Steps:
- Postmortem scheduled for tomorrow 2 PM
- Monitoring for 24 hours
```

#### Escalation Criteria
- **Immediate:** If database is down (escalate to database team)
- **15 minutes:** If no progress, escalate to senior engineer
- **30 minutes:** If unresolved, escalate to engineering manager
- **1 hour:** Escalate to VP Engineering + notify product team

---

### RUNBOOK-002: Alpaca Disconnected (Fallback Active)

**Alert:** AlpacaDisconnected (HIGH)
**Impact:** Degraded performance (60s polling instead of real-time)
**Response Time:** <30 minutes

#### Symptoms
- DataSourceRouter in FALLBACK_ACTIVE state
- Frontend shows "DELAYED DATA" badge
- Alpaca connection status: Disconnected
- Yahoo Finance polling active

#### Diagnostic Steps

**Step 1: Check connection state**
```bash
curl http://localhost:5002/api/health/alpaca | jq '.alpacaStatus'

# Expected output:
# {
#   "connected": false,
#   "authenticated": false,
#   "lastError": "Connection timeout",
#   "consecutiveFailures": 3
# }
```

**Step 2: Check Alpaca status page**
```bash
open https://status.alpaca.markets
# Look for active incidents
```

**Step 3: Review reconnection attempts**
```bash
kubectl logs -l app=mytrader-api --tail=50 | grep "Alpaca.*reconnect"

# Look for:
# - "Attempting reconnection (attempt 3/unlimited)"
# - "Reconnection failed: <reason>"
# - "WebSocket connection established"
```

#### Resolution Steps

**Scenario A: Alpaca service outage (external)**

```bash
# 1. Confirm outage on status page
# 2. Monitor for automatic recovery
watch -n 10 'curl -s http://localhost:5002/api/health/alpaca | jq .alpacaStatus.connected'

# 3. Service should auto-recover when Alpaca is back
# 4. After recovery, verify PRIMARY_ACTIVE state
curl http://localhost:5002/api/health/datasource | jq .connectionState
# Expected: "PRIMARY_ACTIVE"
```

**Scenario B: Authentication failure**

```bash
# 1. Check auth error
kubectl logs -l app=mytrader-api | grep "Authentication failed"

# 2. Verify API keys are valid (test manually)
# DO NOT log keys in production!

# 3. If keys are invalid/expired, rotate them
# (Follow key rotation procedure in RUNBOOK-008)

# 4. Force reconnection after key update
curl -X POST http://localhost:5002/api/health/alpaca/reconnect
```

**Scenario C: Network connectivity issue**

```bash
# 1. Test connectivity to Alpaca
curl -v wss://stream.data.alpaca.markets/v2/iex

# 2. Check firewall rules
sudo iptables -L | grep -i alpaca

# 3. Check corporate proxy settings (if applicable)
env | grep -i proxy

# 4. Check DNS resolution
nslookup stream.data.alpaca.markets
```

**Scenario D: Service restart needed**

```bash
# 1. If reconnection logic appears stuck, force reconnection
curl -X POST http://localhost:5002/api/health/alpaca/reconnect

# 2. If still not connecting after 5 minutes, restart service
kubectl rollout restart deployment/mytrader-api

# 3. Monitor startup logs
kubectl logs -l app=mytrader-api --tail=100 -f | grep "Alpaca"

# Expected:
# - "Connecting to Alpaca WebSocket"
# - "Successfully connected to Alpaca WebSocket"
# - "Alpaca authentication successful"
# - "Subscription confirmed"
```

#### Expected Recovery Time
- **Automatic recovery:** 30 seconds to 5 minutes (after Alpaca service restored)
- **Manual intervention:** 5-10 minutes (reconnection + 10s grace period)
- **Service restart:** 2-3 minutes

---

### RUNBOOK-003: Extreme Latency (P95 >10s)

**Alert:** AlpacaExtremeLatency (CRITICAL)
**Impact:** Severe user experience degradation
**Response Time:** <5 minutes

#### Symptoms
- P95 end-to-end latency >10 seconds
- Users report stale prices
- Frontend appears slow/frozen
- High SignalR connection count

#### Diagnostic Steps

**Step 1: Confirm latency issue**
```bash
# Check performance dashboard
open https://grafana.internal/d/alpaca-performance

# Or query Prometheus directly
curl 'http://prometheus:9090/api/v1/query?query=histogram_quantile(0.95,sum(rate(alpaca_message_latency_seconds_bucket[5m]))by(le))'
```

**Step 2: Identify bottleneck**
```bash
# Check component-level latency
curl http://localhost:5002/api/metrics | grep latency

# Key metrics to check:
# - alpaca_streaming_processing_duration_ms (should be <50ms)
# - datasource_router_processing_duration_ms (should be <20ms)
# - multiasset_broadcast_duration_ms (should be <30ms)
# - signalr_broadcast_duration_ms (should be <20ms)
```

**Step 3: Check system resources**
```bash
# CPU usage
top -bn1 | head -20

# Memory usage
free -m

# Disk I/O
iostat -x 1 5

# Network latency
ping -c 5 stream.data.alpaca.markets
```

**Step 4: Check database performance**
```bash
# Active queries
psql -h localhost -p 5434 -U postgres -d mytrader_db -c "
SELECT pid, now() - pg_stat_activity.query_start AS duration, query
FROM pg_stat_activity
WHERE state = 'active' AND now() - pg_stat_activity.query_start > interval '1 second'
ORDER BY duration DESC;
"

# Slow queries log
tail -50 /var/log/postgresql/postgresql.log | grep "duration:"
```

#### Resolution Steps

**Scenario A: Database bottleneck**

```bash
# 1. Identify slow queries (from Step 4)
# 2. Kill long-running queries if blocking
psql -h localhost -p 5434 -U postgres -d mytrader_db -c "SELECT pg_terminate_backend(<PID>);"

# 3. Check for missing indexes
psql -h localhost -p 5434 -U postgres -d mytrader_db -c "
SELECT schemaname, tablename, attname, n_distinct, correlation
FROM pg_stats
WHERE tablename = 'market_data' AND n_distinct > 1000;
"

# 4. If needed, add index (during low-traffic hours)
# psql -c "CREATE INDEX CONCURRENTLY idx_market_data_timestamp ON market_data(timestamp DESC);"
```

**Scenario B: High SignalR connection count**

```bash
# 1. Check connection count
curl http://localhost:5002/api/health/realtime | jq '.checks[].data.TotalConnections'

# 2. If >500 connections, consider scaling horizontally
kubectl scale deployment/mytrader-api --replicas=3

# 3. Verify load balancing is working
kubectl get pods -l app=mytrader-api
```

**Scenario C: Memory pressure**

```bash
# 1. Check memory usage
kubectl top pod -l app=mytrader-api

# 2. If memory >80%, check for leaks
kubectl exec -it <pod-name> -- /bin/sh
ps aux --sort=-%mem | head -10

# 3. Restart service to clear memory (if safe)
kubectl rollout restart deployment/mytrader-api

# 4. Monitor memory after restart
watch -n 10 'kubectl top pod -l app=mytrader-api'
```

**Scenario D: Network latency to Alpaca**

```bash
# 1. Measure latency
ping -c 20 stream.data.alpaca.markets

# 2. If latency >200ms consistently, check routing
traceroute stream.data.alpaca.markets

# 3. Contact network team if routing issue
# 4. Consider switching to fallback temporarily
curl -X POST http://localhost:5002/api/health/failover
```

#### Performance Tuning Checklist
- [ ] Database connection pool size: 20 (check config)
- [ ] SignalR max connections per server: 1000
- [ ] Message throttling enabled: 20 updates/sec/symbol
- [ ] Database indexes present on market_data (timestamp, symbol, asset_class)
- [ ] GC tuning: Server GC mode enabled in .csproj

---

### RUNBOOK-004: High Validation Failure Rate

**Alert:** AlpacaValidationFailureHigh (HIGH)
**Impact:** Data quality issues, potential incorrect prices
**Response Time:** <30 minutes

#### Symptoms
- Validation failure rate >5%
- Logs show validation errors
- Circuit breaker may be triggering

#### Diagnostic Steps

**Step 1: Check validation failure rate**
```bash
# Query validation failures
curl http://localhost:5002/api/metrics | grep validation_failures

# Check logs for validation errors
kubectl logs -l app=mytrader-api --tail=100 | grep "Validation failed"
```

**Step 2: Identify validation failure reasons**
```bash
# Common validation failures:
kubectl logs -l app=mytrader-api --tail=500 | grep "Validation failed" | \
  awk -F'Validation failed: ' '{print $2}' | sort | uniq -c | sort -rn

# Expected reasons:
# - "Invalid price: 0"
# - "Invalid volume: -1"
# - "Future timestamp"
# - "Circuit breaker triggered"
```

**Step 3: Check for circuit breaker activations**
```bash
kubectl logs -l app=mytrader-api | grep "Circuit breaker triggered"

# Look for symbols with large price movements
# Example: "Circuit breaker triggered: TSLA price movement 25% exceeds 20% threshold"
```

#### Resolution Steps

**Scenario A: Circuit breaker triggered (expected behavior)**

```bash
# 1. Verify this is due to legitimate market volatility
# Check external sources (Yahoo Finance, Google Finance)

# 2. If legitimate (e.g., earnings report, news event):
#    - Document in incident log
#    - Circuit breaker is working correctly
#    - No action needed

# 3. If invalid data from Alpaca:
#    - Report to Alpaca support
#    - Monitor for fix
```

**Scenario B: Data format changed (Alpaca API update)**

```bash
# 1. Check Alpaca changelog
open https://docs.alpaca.markets/changelog

# 2. Review recent messages
kubectl logs -l app=mytrader-api | grep "Received Alpaca message" | tail -10

# 3. If schema changed, update parsing logic
#    - Update AlpacaStreamingService.cs
#    - Update AlpacaMessages.cs DTOs
#    - Deploy hotfix

# 4. Temporary workaround: Switch to Yahoo fallback
curl -X POST http://localhost:5002/api/health/failover
```

**Scenario C: Invalid price/volume data**

```bash
# 1. Identify affected symbols
kubectl logs -l app=mytrader-api | grep "Invalid price" | \
  awk -F'symbol: ' '{print $2}' | awk '{print $1}' | sort | uniq

# 2. Check if specific to certain symbols
# 3. If widespread, likely Alpaca issue (escalate to Alpaca support)
# 4. If specific symbols, add to monitoring watchlist
```

#### Validation Rule Thresholds

| Rule | Current Threshold | Justification |
|------|-------------------|---------------|
| Price > 0 | >0 | Prices cannot be negative or zero |
| Volume >= 0 | ≥0 | Volume cannot be negative |
| Timestamp not in future | <current_time + 5min | Allow small clock drift |
| Circuit breaker | ±20% price movement | Protect against bad data |
| Cross-source delta | <5% difference | Alpaca vs Yahoo consistency |

---

### RUNBOOK-005: Frequent Failovers

**Alert:** AlpacaFailoverFrequencyHigh (MEDIUM)
**Impact:** Service instability, intermittent delays
**Response Time:** <2 hours

#### Symptoms
- More than 3 failover activations in 1 hour
- DataSourceRouter flapping between PRIMARY_ACTIVE and FALLBACK_ACTIVE
- Users intermittently see "DELAYED" badge

#### Diagnostic Steps

**Step 1: Review failover history**
```bash
# Check failover count
curl http://localhost:5002/api/health/datasource | jq '.fallbackCount'

# View failover timeline
kubectl logs -l app=mytrader-api | grep "DataSourceRouter.*state transition"

# Example output:
# 10:15:32 - State transition: PRIMARY_ACTIVE → FALLBACK_ACTIVE (reason: Alpaca timeout)
# 10:18:45 - State transition: FALLBACK_ACTIVE → PRIMARY_ACTIVE (reason: Alpaca recovered)
# 10:22:10 - State transition: PRIMARY_ACTIVE → FALLBACK_ACTIVE (reason: Alpaca timeout)
```

**Step 2: Identify pattern**
```bash
# Check if failovers are time-based (e.g., hourly)
kubectl logs -l app=mytrader-api | grep "State transition" | \
  awk '{print $1}' | cut -d':' -f1 | uniq -c

# Check if load-based (during high traffic)
kubectl logs -l app=mytrader-api | grep "State transition" | \
  xargs -I{} sh -c 'echo {} && curl -s http://localhost:5002/api/metrics | grep message_rate'
```

**Step 3: Check Alpaca connection stability**
```bash
# View consecutive failures
kubectl logs -l app=mytrader-api | grep "Alpaca health check failed"

# Check reconnection attempts
kubectl logs -l app=mytrader-api | grep "Attempting reconnection"
```

#### Resolution Steps

**Scenario A: Network instability**

```bash
# 1. Check network metrics
ping -c 100 stream.data.alpaca.markets | tail -4

# Look for packet loss
# Expected: 0% packet loss
# If >1% loss: Network issue

# 2. Check latency variance
ping -c 50 stream.data.alpaca.markets | awk -F'time=' '{print $2}' | awk '{print $1}' | \
  awk '{sum+=$1; sumsq+=$1*$1} END {print "Avg: " sum/NR " Stddev: " sqrt(sumsq/NR - (sum/NR)^2)}'

# High stddev (>50ms) indicates unstable connection

# 3. Contact network team if issue persists
```

**Scenario B: Alpaca service degradation**

```bash
# 1. Check Alpaca status page for degraded performance
open https://status.alpaca.markets

# 2. Monitor message rate from Alpaca
kubectl logs -l app=mytrader-api | grep "Messages received:" | tail -20

# 3. If Alpaca is unstable:
#    - Switch to Yahoo temporarily
curl -X POST http://localhost:5002/api/health/failover
#    - Monitor Alpaca status for resolution
#    - Switch back when stable
```

**Scenario C: Configuration tuning needed**

```bash
# Current settings:
# - MessageTimeoutSeconds: 30
# - MaxConsecutiveFailures: 3
# - FallbackActivationDelaySeconds: 10

# If failovers are too sensitive, consider:
# 1. Increasing MessageTimeoutSeconds to 60
# 2. Increasing MaxConsecutiveFailures to 5

# Update configuration
kubectl edit configmap mytrader-config

# Restart service
kubectl rollout restart deployment/mytrader-api
```

#### Recommended Configuration Adjustments

| Setting | Current | Recommended (if flapping) | Reason |
|---------|---------|---------------------------|--------|
| MessageTimeoutSeconds | 30 | 60 | Allow more time for slow messages |
| MaxConsecutiveFailures | 3 | 5 | Tolerate brief connection hiccups |
| PrimaryRecoveryGracePeriodSeconds | 10 | 20 | Ensure stable before switching back |
| ReconnectBaseDelayMs | 1000 | 2000 | Slower reconnection attempts |

---

## 3. Troubleshooting Guides

### GUIDE-001: Alpaca Connection Won't Establish

**Symptoms:**
- AlpacaStreamingService shows "Disconnected"
- Logs show "Connection timeout" or "Connection refused"
- Health endpoint shows connected: false

**Troubleshooting Steps:**

1. **Verify Configuration**
```bash
# Check if feature flags are enabled
kubectl get configmap mytrader-config -o yaml | grep -A5 "Alpaca"
# Expected:
# Alpaca:
#   Streaming:
#     Enabled: true

kubectl get configmap mytrader-config -o yaml | grep "EnableAlpacaStreaming"
# Expected: EnableAlpacaStreaming: true
```

2. **Check API Keys**
```bash
# Verify keys exist (DO NOT display keys)
kubectl get secret alpaca-credentials
# Expected: api-key and api-secret present

# Test authentication manually (use test keys, not production)
curl -X POST wss://stream.data.alpaca.markets/v2/iex \
  -H "Content-Type: application/json" \
  -d '{"action":"auth","key":"TEST_KEY","secret":"TEST_SECRET"}'
```

3. **Network Connectivity**
```bash
# Test WebSocket connectivity
telnet stream.data.alpaca.markets 443

# Test DNS resolution
nslookup stream.data.alpaca.markets

# Test HTTPS connectivity
curl -I https://stream.data.alpaca.markets
```

4. **Firewall / Proxy**
```bash
# Check for corporate proxy
env | grep -i proxy

# Test with proxy bypass (if applicable)
export NO_PROXY=stream.data.alpaca.markets
kubectl rollout restart deployment/mytrader-api

# Check firewall rules
sudo iptables -L | grep -i alpaca
```

---

### GUIDE-002: Messages Not Reaching Frontend

**Symptoms:**
- Alpaca connected and authenticated
- Logs show messages received
- Frontend not updating

**Troubleshooting Steps:**

1. **Verify SignalR Connectivity**
```bash
# Check SignalR hub health
curl http://localhost:5002/api/health/realtime

# Expected:
# {
#   "status": "Healthy",
#   "ActiveHubs": 1,
#   "TotalConnections": >0
# }
```

2. **Check Message Flow**
```bash
# Verify AlpacaStreamingService is receiving messages
kubectl logs -l app=mytrader-api | grep "Received Alpaca message" | tail -10

# Verify DataSourceRouter is routing messages
kubectl logs -l app=mytrader-api | grep "DataSourceRouter.*routed" | tail -10

# Verify MultiAssetBroadcastService is broadcasting
kubectl logs -l app=mytrader-api | grep "Broadcasting.*stock" | tail -10

# Verify SignalR is sending to clients
kubectl logs -l app=mytrader-api | grep "SignalR.*sent" | tail -10
```

3. **Check Frontend Connection**
```bash
# Open browser developer console
# Navigate to Network tab
# Filter by "ws" or "websocket"
# Look for SignalR connection
# Expected: Status 101 (Switching Protocols)

# Check console logs for:
# - "SignalR connected"
# - "Price update received: AAPL"
```

4. **Verify Symbol Subscriptions**
```bash
# Check which symbols Alpaca is subscribed to
curl http://localhost:5002/api/health/alpaca | jq '.alpacaStatus.subscribedSymbols'

# Check which symbols frontend is interested in
# (View browser console logs)
```

---

## 4. Operational Tasks

### TASK-001: Enable Alpaca Streaming (Feature Activation)

**When:** First deployment or feature flag activation
**Duration:** 10 minutes
**Risk:** Medium (existing Yahoo polling unaffected)

**Prerequisites:**
- [ ] Valid Alpaca API keys obtained
- [ ] Keys stored in secret management system
- [ ] Staging tested successfully
- [ ] Rollback plan ready

**Procedure:**

1. **Store API Keys**
```bash
# Production keys (NEVER log these)
kubectl create secret generic alpaca-credentials \
  --from-literal=api-key="YOUR_ALPACA_API_KEY" \
  --from-literal=api-secret="YOUR_ALPACA_API_SECRET" \
  --namespace=production

# Verify secret created
kubectl get secret alpaca-credentials -n production
```

2. **Update Configuration**
```bash
# Edit configmap
kubectl edit configmap mytrader-config -n production

# Set these values:
# Alpaca:
#   Streaming:
#     Enabled: true
#     WebSocketUrl: wss://stream.data.alpaca.markets/v2/iex
# FeatureFlags:
#   EnableAlpacaStreaming: true

# Save and exit
```

3. **Deploy Configuration**
```bash
# Restart backend to pick up changes
kubectl rollout restart deployment/mytrader-api -n production

# Monitor rollout
kubectl rollout status deployment/mytrader-api -n production
```

4. **Verify Activation**
```bash
# Wait for pods to be ready (2-3 minutes)
kubectl get pods -l app=mytrader-api -n production

# Check Alpaca connection
curl http://production-api/api/health/alpaca

# Expected:
# {
#   "status": "Healthy",
#   "alpacaStatus": {
#     "connected": true,
#     "authenticated": true
#   }
# }

# Verify router state
curl http://production-api/api/health/datasource

# Expected:
# {
#   "connectionState": "PRIMARY_ACTIVE"
# }
```

5. **Monitor for 30 Minutes**
```bash
# Watch logs for errors
kubectl logs -l app=mytrader-api -n production --tail=100 -f | grep -i error

# Monitor metrics
open https://grafana.internal/d/alpaca-operations

# Check user impact (should be zero issues)
open https://grafana.internal/d/alpaca-business
```

6. **Rollback if Issues**
```bash
# If problems arise, disable feature flag
kubectl edit configmap mytrader-config -n production
# Set: EnableAlpacaStreaming: false

# Restart
kubectl rollout restart deployment/mytrader-api -n production

# System reverts to Yahoo polling only
```

---

### TASK-002: Rotate Alpaca API Keys

**When:** Every 90 days or if compromised
**Duration:** 15 minutes
**Risk:** Low (zero-downtime rotation)

**Procedure:**

1. **Generate New Keys**
```
- Log in to Alpaca dashboard: https://app.alpaca.markets
- Navigate to API Keys section
- Click "Generate New Key Pair"
- Copy new keys (NEVER log or email)
```

2. **Store New Keys (do not activate yet)**
```bash
# Create new secret (different name)
kubectl create secret generic alpaca-credentials-new \
  --from-literal=api-key="NEW_KEY" \
  --from-literal=api-secret="NEW_SECRET" \
  --namespace=production
```

3. **Update Configuration to Use New Secret**
```bash
# Edit deployment to use new secret
kubectl edit deployment mytrader-api -n production

# Find the secret reference:
# env:
#   - name: ALPACA_API_KEY
#     valueFrom:
#       secretKeyRef:
#         name: alpaca-credentials  # Change to: alpaca-credentials-new
#         key: api-key

# Save and exit (triggers rolling update)
```

4. **Monitor Rolling Update**
```bash
# Watch pods restart with new keys
kubectl rollout status deployment/mytrader-api -n production

# New pods should connect to Alpaca with new keys
# Old pods gracefully shut down
```

5. **Verify New Keys Work**
```bash
# Check connection status
curl http://production-api/api/health/alpaca

# Expected: connected: true, authenticated: true

# If authentication fails:
#   - Verify new keys are correct
#   - Check Alpaca dashboard for key status
```

6. **Revoke Old Keys (after 24 hours)**
```
- Log in to Alpaca dashboard
- Revoke old key pair
- Delete old secret: kubectl delete secret alpaca-credentials -n production
```

---

## 5. Escalation Procedures

### 5.1 Escalation Matrix

| Incident Severity | Initial Response | 15 Minutes | 30 Minutes | 1 Hour |
|-------------------|------------------|------------|------------|--------|
| **CRITICAL** | Primary on-call | Secondary on-call + SRE lead | Engineering manager + VP Eng | Incident commander + Exec team |
| **HIGH** | Primary on-call | Secondary on-call | SRE lead | Engineering manager |
| **MEDIUM** | Primary on-call | - | SRE lead (if unresolved) | - |
| **LOW** | Primary on-call | - | - | - |

---

### 5.2 Escalation Contacts

| Role | Primary | Secondary | Availability |
|------|---------|-----------|--------------|
| **SRE On-Call** | PagerDuty | PagerDuty | 24/7 |
| **Backend On-Call** | PagerDuty | PagerDuty | 24/7 |
| **SRE Lead** | [Name] | [Name] | Business hours + critical escalations |
| **Engineering Manager** | [Name] | [Name] | Business hours + critical escalations |
| **Database Team** | [Email/Slack] | - | Business hours |
| **Network Team** | [Email/Slack] | - | Business hours |

---

## 6. Contact Information

### 6.1 Internal Contacts

- **SRE Team Slack:** #sre-team
- **Alpaca Alerts:** #alerts-alpaca
- **On-Call Schedule:** https://pagerduty.com/schedules/alpaca
- **Runbook Wiki:** https://wiki.internal/runbooks/alpaca
- **Dashboard:** https://grafana.internal/d/alpaca-operations

---

### 6.2 External Contacts

- **Alpaca Support:** support@alpaca.markets
- **Alpaca Status:** https://status.alpaca.markets
- **Alpaca Documentation:** https://docs.alpaca.markets

---

## 7. Post-Incident Activities

### 7.1 Incident Log Template

```markdown
## Incident: [Alert Name]

**Date:** 2025-10-09
**Severity:** CRITICAL
**Duration:** 23 minutes
**Impact:** 47 users unable to see stock prices

### Timeline
- 10:15:32 - Alert fired
- 10:16:00 - On-call acknowledged
- 10:18:00 - Investigation started
- 10:25:00 - Root cause identified
- 10:32:00 - Fix deployed
- 10:38:32 - Alert resolved

### Root Cause
Network firewall rule blocking WebSocket connections.

### Resolution
Removed firewall rule, connection restored.

### Action Items
1. [ ] Add firewall rule to IaC (Owner: DevOps, Due: 2025-10-15)
2. [ ] Add firewall checks to runbook (Owner: SRE, Due: 2025-10-12)
```

---

## Document Maintenance

### Review Schedule
- **Monthly:** Review runbook with on-call rotation
- **After Incidents:** Update based on lessons learned
- **Quarterly:** Full review with engineering team

### Changelog

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-09 | SRE Team | Initial runbook for Alpaca streaming |

---

**End of Runbook**
