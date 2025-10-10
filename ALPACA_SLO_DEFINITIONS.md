# Alpaca Streaming Integration - SLO Definitions

**Document Version:** 1.0
**Date:** 2025-10-09
**Owner:** SRE Team
**Review Cycle:** Quarterly

---

## Executive Summary

This document defines Service Level Objectives (SLOs), Service Level Indicators (SLIs), and error budgets for the Alpaca streaming integration in the MyTrader platform. These SLOs balance reliability with development velocity while ensuring excellent user experience.

---

## 1. Service Overview

### 1.1 Service Description

The Alpaca Streaming Service provides real-time stock market data via WebSocket streaming, with automatic fallback to Yahoo Finance for resilience. The service is critical for delivering professional-grade trading simulation experiences.

### 1.2 Service Dependencies

**Upstream Dependencies:**
- Alpaca Market Data API (WebSocket)
- Yahoo Finance REST API (fallback)
- PostgreSQL database (symbol management)

**Downstream Dependencies:**
- MultiAssetDataBroadcastService
- SignalR hubs (MarketDataHub, DashboardHub)
- Web and mobile frontends

---

## 2. Service Level Indicators (SLIs)

### 2.1 Availability SLIs

#### SLI-A1: Service Availability
**Definition:** Percentage of time the stock data streaming service is operational and delivering data.

**Measurement:**
```
Availability = (Total Time - Downtime) / Total Time × 100%

Where downtime = time in BOTH_UNAVAILABLE state
```

**Data Source:** DataSourceRouter state machine logs
**Measurement Window:** Rolling 30 days
**Good Event:** Service in PRIMARY_ACTIVE or FALLBACK_ACTIVE state
**Bad Event:** Service in BOTH_UNAVAILABLE state

---

#### SLI-A2: Alpaca Connection Uptime
**Definition:** Percentage of time the Alpaca WebSocket connection is active and authenticated.

**Measurement:**
```
Alpaca Uptime = Connected Time / Total Time × 100%
```

**Data Source:** AlpacaStreamingService connection state
**Measurement Window:** Rolling 7 days
**Good Event:** Connection state = Connected + Authenticated
**Bad Event:** Connection state = Disconnected or Error

---

### 2.2 Latency SLIs

#### SLI-L1: End-to-End Message Latency (P95)
**Definition:** Time from market event timestamp to frontend delivery (95th percentile).

**Measurement:**
```
Latency = Frontend Received Time - Alpaca Message Timestamp
```

**Data Source:** Distributed tracing (OpenTelemetry) or timestamp logging
**Measurement Window:** Rolling 24 hours
**Good Event:** Latency < 2 seconds
**Bad Event:** Latency >= 2 seconds

---

#### SLI-L2: WebSocket-to-SignalR Latency (P95)
**Definition:** Time from WebSocket message receipt to SignalR broadcast (95th percentile).

**Measurement:**
```
Latency = SignalR Broadcast Time - WebSocket Receipt Time
```

**Data Source:** Internal service timing logs
**Measurement Window:** Rolling 24 hours
**Good Event:** Latency < 100ms
**Bad Event:** Latency >= 100ms

---

### 2.3 Data Quality SLIs

#### SLI-Q1: Data Validation Success Rate
**Definition:** Percentage of messages that pass validation rules.

**Measurement:**
```
Validation Success Rate = Valid Messages / Total Messages × 100%
```

**Data Source:** DataSourceRouter validation logs
**Measurement Window:** Rolling 24 hours
**Good Event:** Message passes all validation rules
**Bad Event:** Message rejected (price ≤ 0, volume < 0, future timestamp, >20% circuit breaker)

---

#### SLI-Q2: Cross-Source Price Consistency
**Definition:** Percentage of symbols with Alpaca/Yahoo price delta <5%.

**Measurement:**
```
Consistency Rate = Symbols Within 5% Delta / Total Symbols × 100%
```

**Data Source:** DataSourceRouter price comparison logs
**Measurement Window:** Rolling 1 hour (during market hours)
**Good Event:** |Alpaca Price - Yahoo Price| / Yahoo Price < 0.05
**Bad Event:** Price delta >= 5%

---

### 2.4 Resilience SLIs

#### SLI-R1: Failover Recovery Time
**Definition:** Time to switch from Alpaca failure to Yahoo fallback (mean).

**Measurement:**
```
Recovery Time = Fallback Activation Time - Failure Detection Time
```

**Data Source:** DataSourceRouter state transition logs
**Measurement Window:** Per incident
**Good Event:** Recovery time < 10 seconds
**Bad Event:** Recovery time >= 10 seconds

---

#### SLI-R2: Primary Restoration Time
**Definition:** Time to restore Alpaca as primary source after reconnection.

**Measurement:**
```
Restoration Time = PRIMARY_ACTIVE Transition Time - Connection Restored Time
```

**Data Source:** DataSourceRouter state transition logs
**Measurement Window:** Per incident
**Good Event:** Restoration time < 30 seconds (includes 10s grace period)
**Bad Event:** Restoration time >= 30 seconds

---

## 3. Service Level Objectives (SLOs)

### 3.1 Availability SLOs

#### SLO-A1: Overall Service Availability
**Objective:** 99.5% availability over 30-day rolling window

**Target:** ≥ 99.5%
**Error Budget:** 0.5% (3.6 hours per 30 days)

**Justification:**
- Allows for planned maintenance windows
- Tolerates brief outages during both-source failures
- Balances cost with user expectations

**Measurement:**
```sql
SELECT
  (COUNT(*) FILTER (WHERE state != 'BOTH_UNAVAILABLE') * 100.0 / COUNT(*)) AS availability_percent
FROM datasource_router_state_log
WHERE timestamp > NOW() - INTERVAL '30 days';
```

**Alert Thresholds:**
- **Warning:** <99.7% (40% error budget consumed)
- **Critical:** <99.5% (error budget exhausted)

---

#### SLO-A2: Alpaca Connection Uptime
**Objective:** 98% uptime over 7-day rolling window

**Target:** ≥ 98%
**Error Budget:** 2% (3.36 hours per 7 days)

**Justification:**
- Alpaca is external dependency beyond our control
- Automatic fallback to Yahoo provides resilience
- 98% allows for brief connection issues

**Measurement:**
```sql
SELECT
  (SUM(EXTRACT(EPOCH FROM connected_duration)) * 100.0 /
   SUM(EXTRACT(EPOCH FROM total_duration))) AS uptime_percent
FROM alpaca_connection_log
WHERE timestamp > NOW() - INTERVAL '7 days';
```

**Alert Thresholds:**
- **Warning:** <99% (50% error budget consumed)
- **Critical:** <98% (error budget exhausted)

---

### 3.2 Latency SLOs

#### SLO-L1: End-to-End Latency (P95)
**Objective:** <2 seconds at P95 over 24-hour rolling window

**Target:** P95 < 2 seconds
**Stretch Goal:** P95 < 1 second

**Justification:**
- Users expect near-real-time updates
- 2 seconds is acceptable for trading simulation
- 1 second provides professional-grade experience

**Measurement:**
```sql
SELECT
  PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY latency_ms) AS p95_latency_ms
FROM message_latency_log
WHERE timestamp > NOW() - INTERVAL '24 hours';
```

**Alert Thresholds:**
- **Warning:** P95 > 3 seconds
- **Critical:** P95 > 5 seconds

---

#### SLO-L2: WebSocket-to-SignalR Latency (P95)
**Objective:** <100ms at P95 over 24-hour rolling window

**Target:** P95 < 100ms
**Stretch Goal:** P95 < 50ms

**Justification:**
- Internal processing should be fast
- 100ms allows for database lookups and transformations
- Avoids bottleneck in pipeline

**Measurement:**
```sql
SELECT
  PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY processing_duration_ms) AS p95_latency_ms
FROM service_processing_log
WHERE timestamp > NOW() - INTERVAL '24 hours';
```

**Alert Thresholds:**
- **Warning:** P95 > 150ms
- **Critical:** P95 > 200ms

---

### 3.3 Data Quality SLOs

#### SLO-Q1: Data Validation Success Rate
**Objective:** >99% validation success rate over 24-hour rolling window

**Target:** ≥ 99%
**Error Budget:** 1% of messages can be invalid

**Justification:**
- Market data can have occasional anomalies
- Circuit breaker will trigger on large price movements
- 1% tolerance avoids false positives

**Measurement:**
```sql
SELECT
  (COUNT(*) FILTER (WHERE validation_passed = true) * 100.0 / COUNT(*)) AS success_rate_percent
FROM message_validation_log
WHERE timestamp > NOW() - INTERVAL '24 hours';
```

**Alert Thresholds:**
- **Warning:** <99.5% (50% error budget consumed)
- **Critical:** <99% (error budget exhausted)

---

#### SLO-Q2: Cross-Source Price Consistency
**Objective:** >95% of symbols within 5% price delta over 1-hour rolling window

**Target:** ≥ 95%
**Error Budget:** 5% of symbols can have >5% delta

**Justification:**
- IEX (Alpaca) and Yahoo may have different data sources
- Brief delays can cause temporary discrepancies
- 5% tolerance accounts for legitimate differences

**Measurement:**
```sql
SELECT
  (COUNT(*) FILTER (WHERE price_delta_percent < 5.0) * 100.0 / COUNT(*)) AS consistency_rate_percent
FROM cross_source_price_comparison
WHERE timestamp > NOW() - INTERVAL '1 hour';
```

**Alert Thresholds:**
- **Warning:** <97% (40% error budget consumed)
- **Critical:** <95% (error budget exhausted)

---

### 3.4 Resilience SLOs

#### SLO-R1: Failover Recovery Time (Mean)
**Objective:** <10 seconds mean recovery time per incident

**Target:** Mean < 10 seconds
**Stretch Goal:** Mean < 5 seconds

**Justification:**
- Quick failover minimizes data gap
- 10 seconds allows for health check intervals
- Ensures continuous data flow

**Measurement:**
```sql
SELECT
  AVG(EXTRACT(EPOCH FROM (fallback_activated_at - failure_detected_at))) AS mean_recovery_seconds
FROM failover_incidents
WHERE timestamp > NOW() - INTERVAL '30 days';
```

**Alert Thresholds:**
- **Warning:** Mean > 15 seconds
- **Critical:** Mean > 20 seconds

---

#### SLO-R2: Failover Frequency
**Objective:** <5 failover activations per day

**Target:** ≤ 5 per day
**Stretch Goal:** ≤ 2 per day

**Justification:**
- Frequent failovers indicate instability
- 5 per day allows for brief connection issues
- More than 5 suggests systemic problem

**Measurement:**
```sql
SELECT
  COUNT(*) AS failover_count
FROM failover_incidents
WHERE DATE(timestamp) = CURRENT_DATE;
```

**Alert Thresholds:**
- **Warning:** >5 per day
- **Critical:** >10 per day

---

## 4. Error Budget Policy

### 4.1 Error Budget Calculation

**Formula:**
```
Error Budget = (1 - SLO Target) × Time Window

Example (SLO-A1):
Error Budget = (1 - 0.995) × 30 days × 24 hours = 3.6 hours per 30 days
```

### 4.2 Error Budget Tracking

**Monitoring Frequency:** Real-time with 5-minute aggregation

**Dashboard Metrics:**
- Current error budget remaining (hours and %)
- Error budget burn rate (projected exhaustion date)
- Historical error budget consumption trend

### 4.3 Error Budget Policy Actions

| Error Budget Remaining | Action | Owner |
|------------------------|--------|-------|
| **100% - 60%** | Normal operations, continue feature development | Engineering Team |
| **60% - 40%** | Monitor closely, prioritize reliability in planning | Tech Lead |
| **40% - 20%** | Reduce feature velocity, focus on reliability improvements | Engineering Manager |
| **20% - 0%** | Feature freeze, all hands on reliability | Engineering Manager + SRE |
| **0% (exhausted)** | Incident declared, postmortem required, freeze until restored | VP Engineering |

---

## 5. Multi-Window, Multi-Burn-Rate Alerting

### 5.1 SLO-A1 Alerting (Service Availability)

**Alert Logic:** Trigger when error budget is consuming too fast

**Burn Rate Thresholds:**

| Alert Severity | Burn Rate | Time Window | Error Budget Consumed | Action |
|----------------|-----------|-------------|----------------------|--------|
| **Critical** | 14.4x | 1 hour | 2% in 1 hour (40% of budget in 20 hours) | Page on-call |
| **High** | 6x | 6 hours | 5% in 6 hours (100% in 5 days) | Notify team |
| **Medium** | 3x | 24 hours | 10% in 24 hours (100% in 10 days) | Create ticket |
| **Low** | 1x | 7 days | Normal consumption | Monitor only |

**Alert Configuration (Prometheus):**
```yaml
groups:
  - name: alpaca_slo_availability
    interval: 1m
    rules:
      - alert: AlpacaServiceAvailabilityCritical
        expr: |
          (
            1 - (
              sum(rate(datasource_router_state_duration_seconds{state!="BOTH_UNAVAILABLE"}[1h]))
              / sum(rate(datasource_router_state_duration_seconds[1h]))
            )
          ) > (14.4 * (1 - 0.995))
        for: 2m
        labels:
          severity: critical
          slo: SLO-A1
        annotations:
          summary: "Alpaca service availability burning error budget at 14.4x rate"
          description: "Service availability is {{ $value | humanizePercentage }} in the last hour, consuming error budget at critical rate."
          runbook: https://wiki/runbooks/alpaca-availability-critical
```

---

### 5.2 SLO-L1 Alerting (End-to-End Latency)

**Alert Logic:** Trigger when P95 latency exceeds SLO target

**Latency Thresholds:**

| Alert Severity | P95 Latency | Time Window | Action |
|----------------|-------------|-------------|--------|
| **Critical** | >5 seconds | 5 minutes | Page on-call |
| **High** | >3 seconds | 15 minutes | Notify team |
| **Medium** | >2 seconds | 30 minutes | Create ticket |
| **Low** | >1 second | 1 hour | Monitor only |

**Alert Configuration (Prometheus):**
```yaml
- alert: AlpacaLatencyP95Critical
  expr: |
    histogram_quantile(0.95,
      sum(rate(alpaca_message_latency_seconds_bucket[5m])) by (le)
    ) > 5
  for: 5m
  labels:
    severity: critical
    slo: SLO-L1
  annotations:
    summary: "Alpaca P95 latency exceeds 5 seconds"
    description: "P95 latency is {{ $value }}s, exceeding critical threshold of 5s."
    runbook: https://wiki/runbooks/alpaca-latency-critical
```

---

## 6. SLO Reporting

### 6.1 Daily SLO Report

**Distribution:** Engineering team Slack channel
**Frequency:** Daily at 9 AM
**Content:**
- Current SLO attainment for each objective
- Error budget remaining (% and hours)
- Trend vs. previous day
- Top 3 contributors to error budget consumption

**Example:**
```
📊 Alpaca SLO Daily Report - 2025-10-09

SLO-A1 (Service Availability): ✅ 99.8% (Target: 99.5%)
  Error Budget: 90% remaining (3.24 hours of 3.6 hours)
  Trend: ↑ 0.1% vs yesterday

SLO-L1 (P95 Latency): ✅ 1.2s (Target: <2s)
  Error Budget: 85% remaining
  Trend: ↓ 0.3s vs yesterday

SLO-Q1 (Validation Success): ⚠️ 98.9% (Target: 99%)
  Error Budget: 10% remaining (approaching threshold)
  Trend: ↓ 0.2% vs yesterday
  Top Error: Circuit breaker triggered for TSLA (20% price movement)

Action Items:
- Investigate TSLA circuit breaker trigger (likely valid)
- Monitor SLO-Q1 closely today
```

---

### 6.2 Monthly SLO Review

**Distribution:** Engineering leadership
**Frequency:** First Monday of each month
**Content:**
- SLO attainment summary for previous month
- Error budget consumption analysis
- Incidents that impacted SLOs
- Proposed SLO adjustments (if needed)
- Reliability improvements planned

**Document Template:** [Link to template]

---

## 7. SLO Maintenance

### 7.1 SLO Review Cycle

**Frequency:** Quarterly
**Participants:** SRE, Engineering Manager, Product Manager
**Agenda:**
1. Review SLO attainment trends
2. Assess if SLOs are too strict or too loose
3. Evaluate new SLIs/SLOs needed
4. Update error budget policy if needed
5. Communicate changes to team

---

### 7.2 SLO Amendment Process

**When to Amend:**
- Consistent SLO attainment >99.9% (too easy)
- Consistent SLO attainment <90% (too strict)
- User feedback indicates different expectations
- Business requirements change
- Service architecture changes

**Process:**
1. Proposal drafted by SRE with data analysis
2. Review with engineering team
3. Approval from Engineering Manager + Product Manager
4. Update documentation
5. Recalibrate monitoring and alerts
6. Communicate to stakeholders

---

## 8. Appendix A: SLO Dashboard Queries

### A.1 Service Availability (SLO-A1)

**Prometheus Query:**
```promql
# Current availability (30-day rolling)
(
  sum(rate(datasource_router_state_duration_seconds{state!="BOTH_UNAVAILABLE"}[30d]))
  / sum(rate(datasource_router_state_duration_seconds[30d]))
) * 100
```

**Grafana Visualization:** Stat panel with threshold markers at 99.5% (red), 99.7% (yellow), 100% (green)

---

### A.2 Alpaca Connection Uptime (SLO-A2)

**Prometheus Query:**
```promql
# Current uptime (7-day rolling)
(
  sum(alpaca_connection_state{state="Connected"})
  / count(alpaca_connection_state)
) * 100
```

**Grafana Visualization:** Time series graph with SLO target line at 98%

---

### A.3 End-to-End Latency (SLO-L1)

**Prometheus Query:**
```promql
# P95 latency (24-hour rolling)
histogram_quantile(0.95,
  sum(rate(alpaca_message_latency_seconds_bucket[24h])) by (le)
)
```

**Grafana Visualization:** Time series graph with SLO target line at 2 seconds

---

## 9. Appendix B: Error Budget Burn Rate Calculator

**Formula:**
```
Burn Rate = Actual Error Rate / SLO Error Rate

Example:
SLO-A1: 99.5% availability (0.5% error rate)
Actual: 99.0% availability (1.0% error rate)
Burn Rate = 1.0% / 0.5% = 2x

Interpretation: Error budget will be exhausted in 15 days (30 days / 2x)
```

**Online Calculator:** [Link to internal tool]

---

## 10. Document Approval

| Role | Name | Approval Date |
|------|------|---------------|
| SRE Lead | TBD | 2025-10-09 |
| Engineering Manager | TBD | 2025-10-09 |
| Product Manager | TBD | 2025-10-09 |

---

**Document Version History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-09 | SRE Team | Initial SLO definitions for Alpaca streaming |

---

**Next Review Date:** 2026-01-09 (Quarterly)

**End of Document**
