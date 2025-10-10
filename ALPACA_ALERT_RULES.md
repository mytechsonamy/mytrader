# Alpaca Streaming Integration - Alert Rules & Notification Strategy

**Document Version:** 1.0
**Date:** 2025-10-09
**Owner:** SRE Team
**Alert Platform:** Prometheus Alertmanager (compatible with PagerDuty, Slack, Email)

---

## Executive Summary

This document defines comprehensive alerting rules for the Alpaca streaming integration, covering critical service failures, performance degradations, and business-impacting events. The alerting strategy balances rapid incident response with alert fatigue prevention through multi-tier severity levels and intelligent routing.

---

## 1. Alert Severity Classification

### 1.1 Severity Levels

| Severity | Response Time | Notification Channel | On-Call Action | Examples |
|----------|---------------|---------------------|----------------|----------|
| **CRITICAL** | <5 minutes | PagerDuty (page) | Immediate response required | Both sources down, P95 >10s for 5+ min |
| **HIGH** | <30 minutes | PagerDuty (push notification) | Respond within 30 minutes | Alpaca disconnected >1 min, P95 >5s |
| **MEDIUM** | <2 hours | Slack channel | Review during business hours | Failover frequency >3/hour, Validation <99% |
| **LOW** | <24 hours | Email digest | Review in daily standup | Configuration drift, Alpaca reconnected |
| **INFO** | No action | Logs only | Monitor trends | State changes, Symbol subscriptions |

### 1.2 Alert Lifecycle

```
┌─────────────┐
│   PENDING   │  (Alert condition met, waiting for "for" duration)
└──────┬──────┘
       │
       ↓ (condition persists)
┌─────────────┐
│   FIRING    │  (Alert actively firing, notification sent)
└──────┬──────┘
       │
       ├──→ (condition clears) ──→ RESOLVED (recovery notification sent)
       │
       └──→ (escalation timeout) ──→ ESCALATED (page supervisor)
```

---

## 2. Critical Alerts (Page Immediately)

### ALERT-C1: Both Data Sources Unavailable

**Condition:** Service in BOTH_UNAVAILABLE state for >60 seconds
**Impact:** Complete loss of stock data streaming
**Business Impact:** Users cannot see price updates

**Prometheus Rule:**
```yaml
- alert: AlpacaBothSourcesUnavailable
  expr: datasource_router_state{state="BOTH_UNAVAILABLE"} == 1
  for: 1m
  labels:
    severity: critical
    component: datasource_router
    slo: SLO-A1
    page: true
  annotations:
    summary: "Both Alpaca and Yahoo data sources unavailable"
    description: |
      CRITICAL: DataSourceRouter is in BOTH_UNAVAILABLE state.
      Both Alpaca streaming and Yahoo fallback are not functioning.
      Users cannot receive stock price updates.

      Current State: {{ $labels.state }}
      Duration: {{ $value }}s

      IMMEDIATE ACTION REQUIRED:
      1. Check Alpaca status page: https://status.alpaca.markets
      2. Verify Yahoo Finance API: curl https://query1.finance.yahoo.com/v8/finance/chart/AAPL
      3. Check backend logs: kubectl logs -l app=mytrader-api --tail=100
      4. Verify network connectivity
      5. Check PostgreSQL connection (symbols database)
    runbook: "https://wiki.internal/runbooks/alpaca-both-sources-down"
    dashboard: "https://grafana.internal/d/alpaca-reliability"
```

**Notification Example:**
```
🚨 CRITICAL ALERT - AlpacaBothSourcesUnavailable

Summary: Both Alpaca and Yahoo data sources unavailable
Duration: 1m 23s
Severity: CRITICAL

Impact:
- 47 connected users cannot see price updates
- All 30 stock symbols affected
- Service availability SLO at risk

Immediate Actions:
1. Check external status pages
2. Verify network connectivity
3. Review backend logs

Runbook: https://wiki.internal/runbooks/alpaca-both-sources-down
```

**Escalation:**
- **0-5 min:** Page primary on-call engineer
- **5-15 min:** Page secondary on-call engineer
- **15+ min:** Page engineering manager + SRE lead

---

### ALERT-C2: Extreme Latency (P95 >10s)

**Condition:** P95 end-to-end latency >10 seconds for 5+ minutes
**Impact:** Severe user experience degradation
**Business Impact:** Users see stale prices, potential churn

**Prometheus Rule:**
```yaml
- alert: AlpacaExtremeLatency
  expr: |
    histogram_quantile(0.95,
      sum(rate(alpaca_message_latency_seconds_bucket[5m])) by (le)
    ) > 10
  for: 5m
  labels:
    severity: critical
    component: latency
    slo: SLO-L1
    page: true
  annotations:
    summary: "Alpaca P95 latency exceeds 10 seconds"
    description: |
      CRITICAL: End-to-end message latency is {{ $value | humanizeDuration }}.

      This is 5x our SLO target of 2 seconds.
      Users are experiencing severe delays in price updates.

      Possible Causes:
      - Database overload (check pgBadger)
      - SignalR hub bottleneck (check connection count)
      - Network issues (check latency to Alpaca)
      - Memory pressure (check available memory)

      IMMEDIATE ACTION REQUIRED:
      1. Check system resources: top, free -m
      2. Check database connections: SELECT count(*) FROM pg_stat_activity;
      3. Check SignalR hub health: GET /api/health/realtime
      4. Review recent deployments (possible regression)
      5. Consider scaling horizontally
    runbook: "https://wiki.internal/runbooks/alpaca-extreme-latency"
    dashboard: "https://grafana.internal/d/alpaca-performance"
```

**Auto-Remediation:**
```yaml
# Optional: Trigger auto-scaling via webhook
webhooks:
  - url: "https://api.internal/autoscale/trigger"
    method: POST
    body: |
      {
        "service": "mytrader-api",
        "action": "scale_out",
        "reason": "Extreme latency detected",
        "alert_name": "AlpacaExtremeLatency"
      }
```

---

### ALERT-C3: Memory Usage Critical (>90%)

**Condition:** Application memory usage >90% for 5+ minutes
**Impact:** Risk of OOM kill, service crash
**Business Impact:** Service downtime imminent

**Prometheus Rule:**
```yaml
- alert: AlpacaMemoryCritical
  expr: |
    (process_resident_memory_bytes{job="mytrader-api"} / 1024 / 1024 / 1024) > 0.9
  for: 5m
  labels:
    severity: critical
    component: resources
    page: true
  annotations:
    summary: "Alpaca service memory usage exceeds 90%"
    description: |
      CRITICAL: Memory usage is {{ $value | humanizePercentage }} of available memory.

      Risk of OOM (Out of Memory) kill is high.
      Service may crash within minutes.

      IMMEDIATE ACTION REQUIRED:
      1. Check for memory leaks: kubectl top pod
      2. Review AlpacaStreamingService connection pool
      3. Check DataSourceRouter message buffers
      4. Restart service if necessary (with rollback plan)
      5. Investigate recent code changes
    runbook: "https://wiki.internal/runbooks/alpaca-memory-critical"
```

---

### ALERT-C4: Authentication Failures (5+ consecutive)

**Condition:** 5 or more consecutive Alpaca authentication failures
**Impact:** Cannot establish WebSocket connection
**Business Impact:** Loss of real-time data source

**Prometheus Rule:**
```yaml
- alert: AlpacaAuthFailure
  expr: alpaca_consecutive_auth_failures >= 5
  for: 1m
  labels:
    severity: critical
    component: alpaca_streaming
    page: true
  annotations:
    summary: "Alpaca authentication failing repeatedly"
    description: |
      CRITICAL: {{ $value }} consecutive authentication failures.

      Possible Causes:
      - Invalid API keys (expired or revoked)
      - Alpaca service outage
      - Network connectivity issues
      - Rate limiting

      IMMEDIATE ACTION REQUIRED:
      1. Verify API keys in configuration (DO NOT log keys)
      2. Check Alpaca status page: https://status.alpaca.markets
      3. Test authentication manually: curl -X POST ...
      4. Check for rate limiting (429 responses)
      5. Rotate API keys if compromised
    runbook: "https://wiki.internal/runbooks/alpaca-auth-failure"
```

---

## 3. High Severity Alerts (Notify Within 30 Minutes)

### ALERT-H1: Alpaca Disconnected (>1 minute)

**Condition:** Alpaca connection lost for >1 minute
**Impact:** Fallback to Yahoo (delayed data)
**Business Impact:** Users see "DELAYED" badge

**Prometheus Rule:**
```yaml
- alert: AlpacaDisconnected
  expr: alpaca_connection_status{state="Connected"} == 0
  for: 1m
  labels:
    severity: high
    component: alpaca_streaming
    slo: SLO-A2
  annotations:
    summary: "Alpaca WebSocket connection lost"
    description: |
      HIGH: Alpaca streaming connection is down.

      Current State: Disconnected
      Fallback: Yahoo Finance active (60s polling)
      Users Impact: Seeing "DELAYED DATA" badge

      DataSourceRouter has switched to FALLBACK_ACTIVE state.
      Service continues operating with degraded performance.

      ACTION REQUIRED (within 30 minutes):
      1. Check Alpaca status page
      2. Review AlpacaStreamingService logs
      3. Verify reconnection attempts are happening
      4. Monitor for automatic recovery (expected within 5 minutes)
      5. Escalate to CRITICAL if >10 minutes
    runbook: "https://wiki.internal/runbooks/alpaca-disconnected"
    dashboard: "https://grafana.internal/d/alpaca-operations"
```

---

### ALERT-H2: High Latency (P95 >5s)

**Condition:** P95 end-to-end latency >5 seconds for 5+ minutes
**Impact:** Degraded user experience
**Business Impact:** Users notice delays

**Prometheus Rule:**
```yaml
- alert: AlpacaHighLatency
  expr: |
    histogram_quantile(0.95,
      sum(rate(alpaca_message_latency_seconds_bucket[5m])) by (le)
    ) > 5
  for: 5m
  labels:
    severity: high
    component: latency
    slo: SLO-L1
  annotations:
    summary: "Alpaca P95 latency exceeds 5 seconds"
    description: |
      HIGH: P95 latency is {{ $value | humanizeDuration }} (target: <2s).

      Current Performance:
      - P50: {{ humanizeDuration .P50 }}
      - P95: {{ $value | humanizeDuration }}
      - P99: {{ humanizeDuration .P99 }}

      ACTION REQUIRED (within 30 minutes):
      1. Check performance dashboard for bottleneck
      2. Review database query performance
      3. Check SignalR hub latency
      4. Verify network latency to Alpaca
      5. Consider caching optimizations
    runbook: "https://wiki.internal/runbooks/alpaca-high-latency"
```

---

### ALERT-H3: Validation Failure Rate High (>5%)

**Condition:** Message validation failure rate >5% for 1+ minute
**Impact:** Data quality issues
**Business Impact:** Users see incorrect/missing prices

**Prometheus Rule:**
```yaml
- alert: AlpacaValidationFailureHigh
  expr: |
    (
      sum(rate(datasource_router_validation_failures_total[1m]))
      / sum(rate(datasource_router_messages_processed_total[1m]))
    ) * 100 > 5
  for: 1m
  labels:
    severity: high
    component: data_quality
    slo: SLO-Q1
  annotations:
    summary: "High rate of data validation failures"
    description: |
      HIGH: {{ $value | humanizePercentage }} of messages failing validation.

      Top Validation Errors:
      {{- range query "topk(5, sum by (reason) (rate(datasource_router_validation_failures_total[1m])))" }}
      - {{ .Labels.reason }}: {{ .Value | humanize }} failures/sec
      {{- end }}

      ACTION REQUIRED (within 30 minutes):
      1. Review validation failure logs
      2. Check for data format changes from Alpaca
      3. Verify circuit breaker thresholds
      4. Check for market anomalies (e.g., halts, extreme volatility)
      5. Update validation rules if needed
    runbook: "https://wiki.internal/runbooks/alpaca-validation-failures"
```

---

## 4. Medium Severity Alerts (Review Within 2 Hours)

### ALERT-M1: Failover Frequency High (>3 per hour)

**Condition:** More than 3 failover activations in 1 hour
**Impact:** Service instability
**Business Impact:** Intermittent delays for users

**Prometheus Rule:**
```yaml
- alert: AlpacaFailoverFrequencyHigh
  expr: sum(increase(datasource_router_failover_total[1h])) > 3
  for: 5m
  labels:
    severity: medium
    component: datasource_router
    slo: SLO-R2
  annotations:
    summary: "Frequent failover activations detected"
    description: |
      MEDIUM: {{ $value }} failover activations in the last hour.

      This indicates connection instability between Alpaca and backend.

      Recent Failovers:
      {{- range query "datasource_router_failover_events{timestamp > (time() - 3600)}" }}
      - {{ .Labels.timestamp }}: {{ .Labels.reason }}
      {{- end }}

      ACTION REQUIRED (within 2 hours):
      1. Check Alpaca connection stability
      2. Review network connectivity
      3. Verify reconnection backoff strategy
      4. Check for configuration issues
      5. Monitor for pattern (time-based, load-based)
    runbook: "https://wiki.internal/runbooks/alpaca-failover-frequency"
```

---

### ALERT-M2: Error Rate Elevated (>1%)

**Condition:** Overall error rate >1% for 5+ minutes
**Impact:** Increased log noise, potential data loss
**Business Impact:** Intermittent issues for users

**Prometheus Rule:**
```yaml
- alert: AlpacaErrorRateElevated
  expr: |
    (
      sum(rate(alpaca_errors_total[5m]))
      / sum(rate(alpaca_messages_received_total[5m]))
    ) * 100 > 1
  for: 5m
  labels:
    severity: medium
    component: error_rate
  annotations:
    summary: "Elevated error rate in Alpaca processing"
    description: |
      MEDIUM: Error rate is {{ $value | humanizePercentage }}.

      Error Breakdown:
      {{- range query "topk(5, sum by (type) (rate(alpaca_errors_total[5m])))" }}
      - {{ .Labels.type }}: {{ .Value | humanize }} errors/sec
      {{- end }}

      ACTION REQUIRED (within 2 hours):
      1. Review error logs for patterns
      2. Check for parsing errors (schema changes?)
      3. Verify data source health
      4. Monitor for escalation
      5. Create bug ticket if systematic issue
    runbook: "https://wiki.internal/runbooks/alpaca-error-rate"
```

---

### ALERT-M3: Cross-Source Price Discrepancy High (>10 symbols)

**Condition:** More than 10 symbols with >5% price delta
**Impact:** Data inconsistency
**Business Impact:** Users may see conflicting prices

**Prometheus Rule:**
```yaml
- alert: AlpacaPriceDiscrepancyHigh
  expr: count(datasource_router_price_delta_percent > 5) > 10
  for: 5m
  labels:
    severity: medium
    component: data_quality
    slo: SLO-Q2
  annotations:
    summary: "High number of symbols with cross-source price discrepancy"
    description: |
      MEDIUM: {{ $value }} symbols have >5% price difference between Alpaca and Yahoo.

      Top Discrepancies:
      {{- range query "topk(10, datasource_router_price_delta_percent)" }}
      - {{ .Labels.symbol }}: {{ .Value | humanizePercentage }} difference
      {{- end }}

      ACTION REQUIRED (within 2 hours):
      1. Verify this is not due to market events
      2. Check Alpaca data feed latency
      3. Check Yahoo data staleness
      4. Review data source timestamps
      5. Document findings for SLO review
    runbook: "https://wiki.internal/runbooks/alpaca-price-discrepancy"
```

---

## 5. Low Severity Alerts (Review Within 24 Hours)

### ALERT-L1: SLO Error Budget Low (<20% remaining)

**Condition:** Error budget for any SLO <20% remaining
**Impact:** Risk of SLO violation
**Business Impact:** SLO at risk this month

**Prometheus Rule:**
```yaml
- alert: AlpacaSLOErrorBudgetLow
  expr: alpaca_slo_error_budget_remaining_percent < 20
  for: 1h
  labels:
    severity: low
    component: slo
  annotations:
    summary: "SLO error budget below 20%"
    description: |
      LOW: Error budget for {{ .Labels.slo_name }} is {{ $value | humanizePercentage }}.

      Current Status:
      - SLO Target: {{ .Labels.slo_target }}
      - Current Value: {{ .Labels.current_value }}
      - Error Budget Remaining: {{ $value | humanizePercentage }}

      If error budget is exhausted, trigger feature freeze per SLO policy.

      ACTION REQUIRED (within 24 hours):
      1. Review SLO dashboard for trends
      2. Identify top contributors to error budget consumption
      3. Plan reliability improvements for next sprint
      4. Communicate to engineering team
      5. Consider reducing feature velocity
    runbook: "https://wiki.internal/runbooks/slo-error-budget-low"
    dashboard: "https://grafana.internal/d/alpaca-reliability"
```

---

### ALERT-L2: Alpaca Connection Flapping (>5 reconnections per hour)

**Condition:** More than 5 connection/disconnection cycles per hour
**Impact:** Connection instability
**Business Impact:** Intermittent fallback to delayed data

**Prometheus Rule:**
```yaml
- alert: AlpacaConnectionFlapping
  expr: sum(increase(alpaca_reconnection_total[1h])) > 5
  for: 10m
  labels:
    severity: low
    component: alpaca_streaming
  annotations:
    summary: "Alpaca connection unstable (flapping)"
    description: |
      LOW: {{ $value }} reconnection events in the last hour.

      This indicates unstable connectivity to Alpaca WebSocket.

      ACTION REQUIRED (within 24 hours):
      1. Check network stability metrics
      2. Review Alpaca status page for historical outages
      3. Verify reconnection backoff logic
      4. Consider adjusting timeout values
      5. Open support ticket with Alpaca if persistent
    runbook: "https://wiki.internal/runbooks/alpaca-connection-flapping"
```

---

## 6. Info Alerts (Logged Only, No Notification)

### ALERT-I1: Alpaca Connection Recovered

**Condition:** Connection restored after disconnection
**Impact:** Service restored to normal
**Business Impact:** Users back to real-time data

**Prometheus Rule:**
```yaml
- alert: AlpacaConnectionRecovered
  expr: |
    alpaca_connection_status{state="Connected"} == 1
    AND
    changes(alpaca_connection_status[5m]) > 0
  for: 1m
  labels:
    severity: info
    component: alpaca_streaming
  annotations:
    summary: "Alpaca connection recovered"
    description: |
      INFO: Alpaca streaming connection has been restored.

      Downtime: {{ .Labels.downtime_duration }}
      Recovery Time: {{ .Labels.recovery_time }}

      DataSourceRouter has transitioned to PRIMARY_ACTIVE state.
      Users are now receiving real-time data.

      NO ACTION REQUIRED: Logged for historical tracking.
    runbook: "https://wiki.internal/runbooks/alpaca-recovery"
```

---

### ALERT-I2: Router State Transition

**Condition:** DataSourceRouter changes state
**Impact:** Service behavior change
**Business Impact:** Data source changes

**Prometheus Rule:**
```yaml
- alert: AlpacaRouterStateChange
  expr: changes(datasource_router_state[2m]) > 0
  for: 30s
  labels:
    severity: info
    component: datasource_router
  annotations:
    summary: "DataSourceRouter state transitioned"
    description: |
      INFO: DataSourceRouter changed from {{ .Labels.previous_state }} to {{ .Labels.current_state }}.

      Reason: {{ .Labels.transition_reason }}
      Timestamp: {{ .Labels.transition_timestamp }}

      NO ACTION REQUIRED: Logged for historical tracking.
```

---

### ALERT-I3: Symbol Subscription Changed

**Condition:** Number of subscribed symbols changed
**Impact:** Symbol list updated
**Business Impact:** None (operational change)

**Prometheus Rule:**
```yaml
- alert: AlpacaSymbolSubscriptionChanged
  expr: changes(alpaca_subscribed_symbols_count[5m]) > 0
  for: 1m
  labels:
    severity: info
    component: alpaca_streaming
  annotations:
    summary: "Alpaca symbol subscriptions updated"
    description: |
      INFO: Symbol subscription count changed from {{ .Labels.previous_count }} to {{ $value }}.

      NO ACTION REQUIRED: Logged for tracking.
```

---

## 7. Alert Routing & Notification Channels

### 7.1 Alertmanager Configuration

**alertmanager.yml:**
```yaml
global:
  resolve_timeout: 5m
  slack_api_url: 'https://hooks.slack.com/services/YOUR/WEBHOOK/URL'
  pagerduty_url: 'https://events.pagerduty.com/v2/enqueue'

route:
  receiver: 'default'
  group_by: ['alertname', 'severity']
  group_wait: 10s
  group_interval: 5m
  repeat_interval: 4h

  routes:
    # CRITICAL: Page on-call immediately
    - match:
        severity: critical
      receiver: pagerduty-critical
      group_wait: 0s
      repeat_interval: 5m
      continue: true

    - match:
        severity: critical
      receiver: slack-critical
      continue: true

    # HIGH: Push notification + Slack
    - match:
        severity: high
      receiver: pagerduty-high
      group_wait: 30s
      repeat_interval: 30m
      continue: true

    - match:
        severity: high
      receiver: slack-high
      continue: false

    # MEDIUM: Slack only
    - match:
        severity: medium
      receiver: slack-medium
      group_wait: 5m
      repeat_interval: 2h

    # LOW: Email digest
    - match:
        severity: low
      receiver: email-digest
      group_wait: 1h
      repeat_interval: 24h

    # INFO: Logs only (no notification)
    - match:
        severity: info
      receiver: 'null'

receivers:
  - name: 'default'
    slack_configs:
      - channel: '#alerts'
        title: 'Default Alert'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'

  - name: 'pagerduty-critical'
    pagerduty_configs:
      - service_key: 'YOUR_PAGERDUTY_SERVICE_KEY'
        severity: 'critical'
        description: '{{ .GroupLabels.alertname }}: {{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'
        details:
          firing: '{{ .Alerts.Firing | len }}'
          resolved: '{{ .Alerts.Resolved | len }}'

  - name: 'slack-critical'
    slack_configs:
      - channel: '#alerts-critical'
        color: 'danger'
        title: '🚨 CRITICAL ALERT'
        text: |
          {{ range .Alerts }}
          *Alert:* {{ .Labels.alertname }}
          *Summary:* {{ .Annotations.summary }}
          *Description:* {{ .Annotations.description }}
          *Runbook:* {{ .Annotations.runbook }}
          *Dashboard:* {{ .Annotations.dashboard }}
          {{ end }}

  - name: 'pagerduty-high'
    pagerduty_configs:
      - service_key: 'YOUR_PAGERDUTY_SERVICE_KEY'
        severity: 'error'
        description: '{{ .GroupLabels.alertname }}: {{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'

  - name: 'slack-high'
    slack_configs:
      - channel: '#alerts-high'
        color: 'warning'
        title: '⚠️ HIGH SEVERITY ALERT'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'

  - name: 'slack-medium'
    slack_configs:
      - channel: '#alerts-medium'
        color: '#FFA500'
        title: 'ℹ️ Medium Alert'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'

  - name: 'email-digest'
    email_configs:
      - to: 'sre-team@mytrader.com'
        from: 'alerts@mytrader.com'
        smarthost: 'smtp.gmail.com:587'
        subject: 'Daily Alert Digest - {{ .GroupLabels.alertname }}'
        html: |
          <h2>Daily Alert Summary</h2>
          {{ range .Alerts }}
          <p><strong>{{ .Labels.alertname }}</strong>: {{ .Annotations.summary }}</p>
          {{ end }}

  - name: 'null'
    # No notification sent (info alerts logged only)

inhibit_rules:
  # Suppress high/medium alerts if critical alert is firing
  - source_match:
      severity: 'critical'
    target_match_re:
      severity: 'high|medium'
    equal: ['alertname', 'component']

  # Suppress AlpacaDisconnected if BothSourcesUnavailable is firing
  - source_match:
      alertname: 'AlpacaBothSourcesUnavailable'
    target_match:
      alertname: 'AlpacaDisconnected'

  # Suppress HighLatency if ExtremeLatency is firing
  - source_match:
      alertname: 'AlpacaExtremeLatency'
    target_match:
      alertname: 'AlpacaHighLatency'
```

---

### 7.2 Slack Message Formatting

**Example Slack Message (Critical Alert):**
```
🚨 CRITICAL ALERT - AlpacaBothSourcesUnavailable

Summary: Both Alpaca and Yahoo data sources unavailable
Severity: CRITICAL
Duration: 1m 23s
Component: datasource_router

Impact:
• 47 users affected
• All 30 stock symbols offline
• Service availability SLO at risk

Immediate Actions:
1. Check Alpaca status page
2. Verify Yahoo Finance API
3. Review backend logs
4. Check database connectivity

📊 Dashboard: https://grafana.internal/d/alpaca-reliability
📖 Runbook: https://wiki.internal/runbooks/alpaca-both-sources-down
```

---

## 8. Alert Testing & Validation

### 8.1 Alert Test Procedure

**Before deploying alerts to production:**

1. **Syntax Validation:**
```bash
promtool check rules alpaca-alerts.yml
```

2. **Unit Tests:**
```bash
promtool test rules alpaca-alerts-test.yml
```

**alpaca-alerts-test.yml:**
```yaml
rule_files:
  - alpaca-alerts.yml

evaluation_interval: 1m

tests:
  - interval: 1m
    input_series:
      - series: 'datasource_router_state{state="BOTH_UNAVAILABLE"}'
        values: '0 0 1 1 1 1'

    alert_rule_test:
      - eval_time: 5m
        alertname: AlpacaBothSourcesUnavailable
        exp_alerts:
          - exp_labels:
              severity: critical
              component: datasource_router
            exp_annotations:
              summary: "Both Alpaca and Yahoo data sources unavailable"
```

3. **Manual Testing:**
```bash
# Trigger test alert
curl -X POST http://localhost:9093/api/v1/alerts \
  -H 'Content-Type: application/json' \
  -d '[{
    "labels": {
      "alertname": "AlpacaBothSourcesUnavailable",
      "severity": "critical"
    },
    "annotations": {
      "summary": "TEST ALERT - Both sources unavailable"
    },
    "startsAt": "2025-10-09T10:00:00Z"
  }]'
```

4. **End-to-End Testing:**
   - Manually trigger conditions (e.g., disconnect Alpaca)
   - Verify alert fires within expected time
   - Verify notification sent to correct channel
   - Verify runbook link works
   - Verify alert resolves when condition clears

---

### 8.2 Alert Quality Metrics

**Track these metrics to improve alert effectiveness:**

| Metric | Target | Calculation |
|--------|--------|-------------|
| **Alert Actionability** | >80% | (Alerts requiring action / Total alerts) × 100% |
| **Alert Precision** | >90% | (True positive alerts / All firing alerts) × 100% |
| **Alert Recall** | >95% | (Detected incidents / All incidents) × 100% |
| **Mean Time to Acknowledge (MTTA)** | <5 minutes | Average time from alert firing to acknowledgment |
| **False Positive Rate** | <10% | (False positive alerts / Total alerts) × 100% |
| **Alert Fatigue Index** | <20% | (Ignored alerts / Total alerts) × 100% |

**Monthly Alert Review:**
```sql
-- Query Prometheus for alert statistics
SELECT
  alertname,
  COUNT(*) as total_fires,
  AVG(duration_seconds) as avg_duration,
  SUM(CASE WHEN resolved_by = 'timeout' THEN 1 ELSE 0 END) as auto_resolved,
  SUM(CASE WHEN resolved_by = 'manual' THEN 1 ELSE 0 END) as manually_resolved
FROM alert_history
WHERE timestamp > NOW() - INTERVAL '30 days'
GROUP BY alertname
ORDER BY total_fires DESC;
```

---

## 9. Alert Maintenance

### 9.1 Alert Tuning Process

**When to tune alerts:**
- Alert fires >10 times per day (too sensitive)
- Alert hasn't fired in 90 days (may be obsolete)
- False positive rate >10% (threshold too strict)
- Missed incidents (threshold too loose)

**Tuning Steps:**
1. Collect data on alert behavior (30-day window)
2. Analyze false positives and false negatives
3. Propose new thresholds with justification
4. Test in staging environment
5. Gradually roll out to production
6. Monitor for 7 days
7. Document changes in changelog

---

### 9.2 Alert Changelog

| Date | Alert | Change | Reason | Owner |
|------|-------|--------|--------|-------|
| 2025-10-09 | AlpacaBothSourcesUnavailable | Initial creation | Alpaca integration launch | SRE Team |
| 2025-10-09 | AlpacaExtremeLatency | Threshold: 10s for 5m | Based on SLO definition | SRE Team |
| TBD | AlpacaHighLatency | Adjust threshold to 3s | Too many false positives | TBD |

---

## 10. Incident Response Integration

### 10.1 Alert-to-Incident Workflow

```
Alert Fires (CRITICAL)
    ↓
PagerDuty Notification Sent
    ↓
On-Call Engineer Acknowledges (within 5 min)
    ↓
Incident Created (if not auto-resolved in 10 min)
    ↓
Runbook Followed
    ↓
Incident Resolved
    ↓
Postmortem Created (for critical incidents)
    ↓
Action Items Added to Backlog
    ↓
Alert Rules Updated (if needed)
```

---

### 10.2 Postmortem Template

**For all CRITICAL alerts lasting >15 minutes:**

```markdown
# Incident Postmortem: [Alert Name]

## Incident Summary
- **Date:** 2025-10-09
- **Duration:** 23 minutes
- **Severity:** Critical
- **Impact:** 47 users unable to see stock prices

## Timeline
- 10:15:32 - Alert fired: AlpacaBothSourcesUnavailable
- 10:16:00 - On-call engineer acknowledged
- 10:18:00 - Investigation started
- 10:25:00 - Root cause identified (network firewall rule)
- 10:32:00 - Fix deployed
- 10:38:32 - Alert resolved

## Root Cause
Network firewall rule blocking WebSocket connections to Alpaca.

## Resolution
Removed blocking firewall rule, connection restored.

## Action Items
1. [ ] Add firewall rule to infrastructure-as-code (Owner: DevOps, Due: 2025-10-15)
2. [ ] Add pre-deployment firewall rule checks (Owner: SRE, Due: 2025-10-20)
3. [ ] Improve runbook with firewall troubleshooting (Owner: SRE, Due: 2025-10-12)

## Lessons Learned
- Firewall changes should be version-controlled
- Need better visibility into network-level issues
```

---

## Document Approval

| Role | Name | Date |
|------|------|------|
| SRE Lead | TBD | 2025-10-09 |
| On-Call Lead | TBD | 2025-10-09 |
| Engineering Manager | TBD | 2025-10-09 |

---

**Next Review Date:** 2025-11-09 (Monthly)

**End of Document**
