# Alpaca Streaming Integration - Monitoring Dashboards

**Document Version:** 1.0
**Date:** 2025-10-09
**Owner:** SRE Team
**Dashboard Platform:** Grafana 10.x (compatible with Application Insights, Datadog)

---

## Executive Summary

This document provides complete specifications for 4 monitoring dashboards covering real-time operations, performance analysis, reliability tracking, and business insights for the Alpaca streaming integration.

---

## Dashboard 1: Real-Time Operations Dashboard

### Purpose
Provide at-a-glance operational status for on-call engineers and support teams.

### Audience
- On-call engineers
- NOC (Network Operations Center)
- Support team

### Refresh Rate
5 seconds (auto-refresh)

### Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│                  Alpaca Real-Time Operations                         │
│                  Last Update: 2025-10-09 10:30:15                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │ Alpaca Connection│  │  Router State    │  │  Active Symbols  │  │
│  │                  │  │                  │  │                  │  │
│  │   🟢 CONNECTED   │  │  PRIMARY_ACTIVE  │  │       30         │  │
│  │   Authenticated  │  │                  │  │                  │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  │
│                                                                       │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │  Message Rate    │  │  Connected Clients│ │  Last Failover   │  │
│  │                  │  │                  │  │                  │  │
│  │  120 msg/min     │  │       45         │  │  2 hours ago     │  │
│  │  ↑ 15%          │  │  ↑ 5            │  │  (Auto-recovered)│  │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │          Message Rate (Last 5 Minutes)                       │  │
│  │                                                              │  │
│  │  150 ┤                                                      │  │
│  │      │              ╭─╮                                     │  │
│  │  120 ┤          ╭───╯ ╰─╮                                  │  │
│  │      │      ╭───╯       ╰──╮                               │  │
│  │   90 ┤  ╭───╯              ╰───╮                           │  │
│  │      │╭─╯                      ╰───╮                       │  │
│  │   60 ┼────────────────────────────────────                 │  │
│  │      10:25  10:26  10:27  10:28  10:29  10:30             │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │          Active Symbols (Real-Time Updates)                  │  │
│  ├────────┬──────────┬──────────┬──────────┬──────────────────┤  │
│  │ Symbol │  Price   │  Change  │  Volume  │  Last Update     │  │
│  ├────────┼──────────┼──────────┼──────────┼──────────────────┤  │
│  │ AAPL   │ $150.25  │ +1.5% ↑  │ 1.2M     │ 2 sec ago        │  │
│  │ GOOGL  │ $2,850.50│ +0.8% ↑  │ 850K     │ 3 sec ago        │  │
│  │ MSFT   │ $380.75  │ -0.2% ↓  │ 950K     │ 1 sec ago        │  │
│  │ TSLA   │ $245.30  │ +2.1% ↑  │ 2.5M     │ 5 sec ago        │  │
│  │ AMZN   │ $3,150.00│ +1.0% ↑  │ 780K     │ 4 sec ago        │  │
│  │ ...    │ ...      │ ...      │ ...      │ ...              │  │
│  └────────┴──────────┴──────────┴──────────┴──────────────────┘  │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

---

### Panel 1.1: Alpaca Connection Status (Stat Panel)

**Metric:** `alpaca_connection_status`
**Visualization:** Stat with icon and color coding

**Query (Prometheus):**
```promql
alpaca_connection_status{state="Connected"}
```

**Thresholds:**
- 🟢 Green: Value = 1 (Connected + Authenticated)
- 🟡 Yellow: Value = 0.5 (Connected but not authenticated)
- 🔴 Red: Value = 0 (Disconnected)

**Display:**
```
🟢 CONNECTED
Authenticated
Uptime: 2h 15m
```

---

### Panel 1.2: Router State (Stat Panel)

**Metric:** `datasource_router_state`
**Visualization:** Stat with color-coded background

**Query (Prometheus):**
```promql
datasource_router_state
```

**Thresholds:**
- 🟢 Green: Value = 0 (PRIMARY_ACTIVE)
- 🟡 Yellow: Value = 1 (FALLBACK_ACTIVE)
- 🔴 Red: Value = 2 (BOTH_UNAVAILABLE)

**Display:**
```
PRIMARY_ACTIVE
Using Alpaca (Real-time)
Since: 10:15:00
```

---

### Panel 1.3: Active Symbols (Stat Panel)

**Metric:** `alpaca_subscribed_symbols_count`
**Visualization:** Stat with sparkline

**Query (Prometheus):**
```promql
sum(alpaca_subscribed_symbols_count)
```

**Display:**
```
30
Max: 30 (Free Tier)
─╮  ╭─
 ╰──╯
```

---

### Panel 1.4: Message Rate (Stat Panel with Trend)

**Metric:** `rate(alpaca_messages_received_total[1m])`
**Visualization:** Stat with trend arrow

**Query (Prometheus):**
```promql
sum(rate(alpaca_messages_received_total[1m])) * 60
```

**Display:**
```
120 msg/min
↑ 15% vs 5m ago
[Sparkline: ─╮╭─]
```

---

### Panel 1.5: Connected SignalR Clients (Stat Panel)

**Metric:** `signalr_total_connections`
**Visualization:** Stat

**Query (Prometheus):**
```promql
sum(signalr_total_connections{hub=~"MarketData|Dashboard"})
```

**Display:**
```
45 clients
↑ 5 in last 5m
```

---

### Panel 1.6: Last Failover (Stat Panel)

**Metric:** `datasource_router_last_failover_timestamp`
**Visualization:** Stat with time-since calculation

**Query (Prometheus):**
```promql
time() - datasource_router_last_failover_timestamp
```

**Display:**
```
2 hours ago
(Auto-recovered)
Failovers today: 1
```

---

### Panel 1.7: Message Rate Time Series (Graph Panel)

**Metric:** `rate(alpaca_messages_received_total[1m])`
**Visualization:** Time series graph with area fill

**Query (Prometheus):**
```promql
sum(rate(alpaca_messages_received_total[1m])) by (type) * 60
```

**Legend:**
- Trades (blue)
- Quotes (green)
- Bars (orange)

**Y-Axis:** Messages per minute
**X-Axis:** Last 5 minutes (auto-scrolling)

---

### Panel 1.8: Active Symbols Table (Table Panel)

**Metrics:** Multiple
**Visualization:** Table with auto-refresh

**Query (Prometheus + API):**
```promql
# Pull latest price data from API endpoint
GET /api/symbols/active?source=alpaca
```

**Columns:**
1. Symbol (sortable)
2. Price (formatted as currency)
3. Change % (color-coded: green +, red -)
4. Volume (formatted: 1.2M)
5. Last Update (relative time: "2 sec ago")

**Row Limit:** 10 (scrollable)
**Sort Default:** By last update (most recent first)

---

## Dashboard 2: Performance Dashboard

### Purpose
Analyze latency, throughput, and system resource utilization.

### Audience
- Performance engineers
- Backend developers
- SRE team

### Refresh Rate
30 seconds

### Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│                  Alpaca Performance Dashboard                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────┐ │
│  │  P50 Latency │  │  P95 Latency │  │  P99 Latency │  │  Error  │ │
│  │              │  │              │  │              │  │  Rate   │ │
│  │   0.8s       │  │   1.2s ✅    │  │   3.5s       │  │  0.2%   │ │
│  │              │  │  SLO: <2s    │  │              │  │         │ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └─────────┘ │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │          End-to-End Latency Distribution (Last 1 Hour)       │  │
│  │                                                              │  │
│  │   5s ┤                                                      │  │
│  │      │                                        ╭─            │  │
│  │   4s ┤                                 ╭──────╯             │  │
│  │      │                          ╭──────╯                    │  │
│  │   3s ┤                   ╭──────╯                           │  │
│  │      │            ╭──────╯  [P99]                           │  │
│  │   2s ┤     ╭──────╯ [P95 SLO Target]                        │  │
│  │      │╭────╯ [P95]                                          │  │
│  │   1s ┼────────────────────────────────────                 │  │
│  │      P0   P25   P50   P75   P95   P99   P100              │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │          Latency Breakdown (Component-Level)                 │  │
│  │                                                              │  │
│  │  ┌────────────────┐  25ms  AlpacaStreamingService           │  │
│  │  ├────────────────┤  15ms  DataSourceRouter                 │  │
│  │  ├────────────────┤  30ms  MultiAssetBroadcastService       │  │
│  │  ├────────────────┤  20ms  SignalR Hub                      │  │
│  │  └────────────────┘  10ms  Network (WebSocket)              │  │
│  │                     ─────                                    │  │
│  │                     100ms  Total                             │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌─────────────────────────────┐  ┌──────────────────────────────┐ │
│  │     Throughput              │  │     Resource Usage           │ │
│  │                             │  │                              │ │
│  │  500 ┤      ╭─╮             │  │  CPU:  8% ✅                 │ │
│  │      │  ╭───╯ ╰─╮           │  │  Memory: 551 MB ✅           │ │
│  │  300 ┤╭─╯       ╰───╮       │  │  Threads: 12                 │ │
│  │      ╰──────────────────    │  │  DB Conn: 5/20               │ │
│  │    00:00  00:15  00:30      │  │                              │ │
│  └─────────────────────────────┘  └──────────────────────────────┘ │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

---

### Panel 2.1: P50/P95/P99 Latency (Stat Panels)

**Metric:** `histogram_quantile()`
**Visualization:** 3 stat panels side-by-side

**Query (Prometheus):**
```promql
# P50
histogram_quantile(0.50,
  sum(rate(alpaca_message_latency_seconds_bucket[1h])) by (le)
)

# P95 (with SLO comparison)
histogram_quantile(0.95,
  sum(rate(alpaca_message_latency_seconds_bucket[1h])) by (le)
)

# P99
histogram_quantile(0.99,
  sum(rate(alpaca_message_latency_seconds_bucket[1h])) by (le)
)
```

**Thresholds (P95):**
- 🟢 Green: <1 second
- 🟡 Yellow: 1-2 seconds
- 🔴 Red: >2 seconds

---

### Panel 2.2: Error Rate (Stat Panel)

**Metric:** `rate(alpaca_errors_total[1h])`
**Visualization:** Stat with percentage

**Query (Prometheus):**
```promql
(
  sum(rate(alpaca_errors_total[1h]))
  / sum(rate(alpaca_messages_received_total[1h]))
) * 100
```

**Thresholds:**
- 🟢 Green: <0.5%
- 🟡 Yellow: 0.5% - 1%
- 🔴 Red: >1%

---

### Panel 2.3: End-to-End Latency Distribution (Graph Panel)

**Metric:** `alpaca_message_latency_seconds`
**Visualization:** Heatmap or histogram

**Query (Prometheus):**
```promql
sum(rate(alpaca_message_latency_seconds_bucket[1h])) by (le)
```

**Display:** Percentile chart (0th to 100th percentile)
**Markers:**
- P50 line
- P95 line (with SLO target at 2s)
- P99 line

---

### Panel 2.4: Latency Breakdown (Bar Gauge Panel)

**Metrics:** Multiple component latencies
**Visualization:** Horizontal bar gauge (stacked)

**Query (Prometheus):**
```promql
# Component latencies
avg(alpaca_streaming_processing_duration_ms)
avg(datasource_router_processing_duration_ms)
avg(multiasset_broadcast_duration_ms)
avg(signalr_broadcast_duration_ms)
```

**Display:**
- Stacked horizontal bars showing contribution
- Labels with milliseconds
- Total at the end

---

### Panel 2.5: Throughput (Time Series Graph)

**Metric:** `rate(alpaca_messages_received_total[1m])`
**Visualization:** Time series graph

**Query (Prometheus):**
```promql
sum(rate(alpaca_messages_received_total[1m])) by (type) * 60
```

**Y-Axis:** Messages per minute
**X-Axis:** Last 30 minutes
**Legend:** By message type (trades, quotes, bars)

---

### Panel 2.6: Resource Usage (Stat Panel with Gauges)

**Metrics:** System resource usage
**Visualization:** Multiple gauge panels

**Query (Prometheus):**
```promql
# CPU
rate(process_cpu_seconds_total[1m]) * 100

# Memory
process_resident_memory_bytes / 1024 / 1024

# Threads
process_num_threads

# DB Connections
datasource_router_db_connections_active
```

**Thresholds:**
- CPU: Green <15%, Yellow 15-50%, Red >50%
- Memory: Green <700 MB, Yellow 700-1000 MB, Red >1000 MB

---

## Dashboard 3: Reliability Dashboard

### Purpose
Track uptime, failovers, error rates, and SLO compliance.

### Audience
- SRE team
- Engineering management
- On-call engineers

### Refresh Rate
1 minute

### Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│                  Alpaca Reliability Dashboard                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌──────────────┐  │
│  │  Uptime    │  │  Failovers │  │  MTTR      │  │  SLO Status  │  │
│  │  (30 days) │  │  (Today)   │  │  (Mean)    │  │              │  │
│  │  99.8% ✅  │  │     1      │  │   8.5s ✅  │  │  4/4 Met ✅  │  │
│  │  SLO: 99.5%│  │  SLO: <5   │  │  SLO: <10s │  │              │  │
│  └────────────┘  └────────────┘  └────────────┘  └──────────────┘  │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │          Uptime Trend (30-Day Rolling)                       │  │
│  │                                                              │  │
│  │ 100% ┼────────────────────────────────────────────          │  │
│  │      │╭─────────────────────────────────────────╮           │  │
│  │ 99.5%┤│[SLO Target]────────────────────────────│           │  │
│  │      │╰────╮                                   ╭╯           │  │
│  │ 99.0%┤     ╰───╮                           ╭──╯            │  │
│  │      │         ╰───────────────────────────╯                │  │
│  │ 98.5%┼────────────────────────────────────────────          │  │
│  │      Oct 1  Oct 8  Oct 15  Oct 22  Oct 29                  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │          Failover Events Timeline                            │  │
│  ├──────────────┬──────────┬──────────┬──────────┬─────────────┤  │
│  │  Timestamp   │  Trigger │ Recovery │ Duration │  Impact     │  │
│  ├──────────────┼──────────┼──────────┼──────────┼─────────────┤  │
│  │ 10:15:32 ✅  │ Alpaca   │ Auto     │  8.2s    │  None       │  │
│  │              │ timeout  │          │          │             │  │
│  │ 08:42:18 ✅  │ Alpaca   │ Auto     │  9.1s    │  None       │  │
│  │              │ disconn  │          │          │             │  │
│  │ Oct 8 14:05  │ Manual   │ Manual   │  5 min   │  None       │  │
│  │              │ test     │          │          │             │  │
│  └──────────────┴──────────┴──────────┴──────────┴─────────────┘  │
│                                                                       │
│  ┌─────────────────────────────┐  ┌──────────────────────────────┐ │
│  │     Error Rate by Type      │  │     Circuit Breaker          │ │
│  │                             │  │                              │ │
│  │  Auth: 0 errors             │  │  Activations: 2 (today)      │ │
│  │  Conn: 1 errors             │  │  TSLA: 20% movement ✅       │ │
│  │  Parse: 0 errors            │  │  GME: 25% movement ⚠️        │ │
│  │  Valid: 15 warnings         │  │                              │ │
│  └─────────────────────────────┘  └──────────────────────────────┘ │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

---

### Panel 3.1: Uptime (30 days) (Stat Panel)

**Metric:** `datasource_router_uptime_percent`
**Visualization:** Stat with SLO comparison

**Query (Prometheus):**
```promql
(
  sum(rate(datasource_router_state_duration_seconds{state!="BOTH_UNAVAILABLE"}[30d]))
  / sum(rate(datasource_router_state_duration_seconds[30d]))
) * 100
```

**Thresholds:**
- 🟢 Green: ≥99.5%
- 🟡 Yellow: 99.0% - 99.5%
- 🔴 Red: <99.0%

**Display:**
```
99.8%
✅ SLO Met
Target: 99.5%
Error Budget: 90% remaining
```

---

### Panel 3.2: Failovers (Today) (Stat Panel)

**Metric:** `datasource_router_failover_count`
**Visualization:** Stat with daily count

**Query (Prometheus):**
```promql
sum(increase(datasource_router_failover_total[1d]))
```

**Thresholds:**
- 🟢 Green: ≤2
- 🟡 Yellow: 3-5
- 🔴 Red: >5

---

### Panel 3.3: MTTR (Mean Time to Recover) (Stat Panel)

**Metric:** `datasource_router_recovery_time_seconds`
**Visualization:** Stat with mean calculation

**Query (Prometheus):**
```promql
avg(datasource_router_recovery_time_seconds)
```

**Thresholds:**
- 🟢 Green: <10 seconds
- 🟡 Yellow: 10-20 seconds
- 🔴 Red: >20 seconds

---

### Panel 3.4: SLO Status (Stat Panel)

**Metric:** Multiple SLOs
**Visualization:** Single stat showing compliance

**Query (Prometheus):**
```promql
# Count SLOs met vs total
count(
  (slo_availability >= 99.5) OR
  (slo_latency_p95 < 2) OR
  (slo_validation_success >= 99) OR
  (slo_failover_frequency <= 5)
)
```

**Display:**
```
4/4 SLOs Met ✅
All targets achieved
```

---

### Panel 3.5: Uptime Trend (30-Day Rolling) (Graph Panel)

**Metric:** `datasource_router_uptime_percent`
**Visualization:** Time series with SLO target line

**Query (Prometheus):**
```promql
(
  sum(rate(datasource_router_state_duration_seconds{state!="BOTH_UNAVAILABLE"}[30d]))
  / sum(rate(datasource_router_state_duration_seconds[30d]))
) * 100
```

**Y-Axis:** Percentage (98% - 100%)
**X-Axis:** Last 30 days
**Markers:**
- SLO target line at 99.5% (red dashed)
- Current value (blue line)

---

### Panel 3.6: Failover Events Timeline (Table Panel)

**Metrics:** Failover event logs
**Visualization:** Table with sortable columns

**Query (Log Query / API):**
```sql
SELECT
  timestamp,
  trigger_reason,
  recovery_type,
  duration_seconds,
  impact_assessment
FROM failover_events
WHERE timestamp > NOW() - INTERVAL '7 days'
ORDER BY timestamp DESC;
```

**Columns:**
1. Timestamp (formatted: "Oct 9, 10:15:32")
2. Trigger (icon + text: "Alpaca timeout")
3. Recovery (icon: Auto ✅ / Manual 👤)
4. Duration (formatted: "8.2s")
5. Impact (color-coded: None 🟢 / Minor 🟡 / Major 🔴)

---

### Panel 3.7: Error Rate by Type (Stat List Panel)

**Metrics:** Multiple error counters
**Visualization:** Stat list

**Query (Prometheus):**
```promql
sum(increase(alpaca_errors_total{type="authentication"}[1d]))
sum(increase(alpaca_errors_total{type="connection"}[1d]))
sum(increase(alpaca_errors_total{type="parse"}[1d]))
sum(increase(alpaca_errors_total{type="validation"}[1d]))
```

**Display:**
- Each error type as a row
- Count for today
- Sparkline showing trend

---

### Panel 3.8: Circuit Breaker Activations (Stat List Panel)

**Metrics:** Circuit breaker events
**Visualization:** Stat list

**Query (Prometheus):**
```promql
sum by (symbol) (increase(datasource_router_circuit_breaker_total[1d]))
```

**Display:**
- Symbol name
- Activation count today
- Price movement percentage
- Status icon (✅ valid / ⚠️ investigate)

---

## Dashboard 4: Business Insights Dashboard

### Purpose
Provide business metrics and usage analytics.

### Audience
- Product management
- Business analysts
- Engineering leadership

### Refresh Rate
5 minutes

### Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│                  Alpaca Business Insights Dashboard                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌──────────────┐  │
│  │  Alpaca    │  │  Real-time │  │  Active    │  │  Peak Usage  │  │
│  │  Usage %   │  │  Users     │  │  Symbols   │  │  Hour        │  │
│  │  87%       │  │    42      │  │    30      │  │  10-11 AM    │  │
│  │  vs 13%    │  │  vs 5      │  │  (Max)     │  │  EST         │  │
│  │  Yahoo     │  │  Delayed   │  │            │  │              │  │
│  └────────────┘  └────────────┘  └────────────┘  └──────────────┘  │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │          Data Source Usage (7-Day Trend)                     │  │
│  │                                                              │  │
│  │ 100% ┤                                                      │  │
│  │      │ Alpaca ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓      │  │
│  │  75% ┤                                                      │  │
│  │      │                                                      │  │
│  │  50% ┤                                                      │  │
│  │      │                                                      │  │
│  │  25% ┤                                                      │  │
│  │      │ Yahoo ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░      │  │
│  │   0% ┼────────────────────────────────────────────          │  │
│  │      Oct 3   Oct 5   Oct 7   Oct 9                         │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │          Most Active Symbols (by Message Count)              │  │
│  ├────────┬──────────┬──────────────┬──────────────────────────┤  │
│  │ Symbol │ Messages │  Subscribers │  Avg Price Change        │  │
│  ├────────┼──────────┼──────────────┼──────────────────────────┤  │
│  │ TSLA   │  5,240   │     28       │  +2.1%                   │  │
│  │ AAPL   │  4,850   │     35       │  +1.5%                   │  │
│  │ NVDA   │  4,120   │     22       │  +3.2%                   │  │
│  │ AMZN   │  3,980   │     18       │  +1.0%                   │  │
│  │ GOOGL  │  3,750   │     20       │  +0.8%                   │  │
│  └────────┴──────────┴──────────────┴──────────────────────────┘  │
│                                                                       │
│  ┌─────────────────────────────┐  ┌──────────────────────────────┐ │
│  │     Peak Usage Hours        │  │     Cost Tracking            │ │
│  │                             │  │                              │ │
│  │  200 ┤      ╭─╮             │  │  Alpaca: Free Tier           │ │
│  │      │  ╭───╯ ╰─╮           │  │  Current: 30 symbols         │ │
│  │  100 ┤╭─╯       ╰───╮       │  │  Limit: 30 symbols           │ │
│  │      ╰──────────────────    │  │                              │ │
│  │    9AM 10AM 11AM 12PM 1PM   │  │  Estimated Cost: $0/mo       │ │
│  └─────────────────────────────┘  └──────────────────────────────┘ │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

---

### Panel 4.1: Alpaca Usage Percentage (Stat Panel)

**Metric:** Data source distribution
**Visualization:** Stat with pie chart

**Query (Prometheus):**
```promql
(
  sum(rate(alpaca_messages_received_total[7d]))
  / (
    sum(rate(alpaca_messages_received_total[7d])) +
    sum(rate(yahoo_messages_received_total[7d]))
  )
) * 100
```

**Display:**
```
87% Alpaca
vs 13% Yahoo
[Pie chart visualization]
```

---

### Panel 4.2: Real-time vs Delayed Users (Stat Panel)

**Metric:** User connection distribution
**Visualization:** Stat

**Query (Prometheus):**
```promql
sum(signalr_connections{data_source="alpaca"})
sum(signalr_connections{data_source="yahoo"})
```

**Display:**
```
42 Real-time (Alpaca)
5 Delayed (Yahoo fallback)
```

---

### Panel 4.3: Active Symbols (Stat Panel)

**Metric:** Symbol subscription count
**Visualization:** Stat with max indicator

**Query (Prometheus):**
```promql
sum(alpaca_subscribed_symbols_count)
```

**Display:**
```
30 symbols
(Max: 30 Free Tier)
100% utilization
```

---

### Panel 4.4: Peak Usage Hour (Stat Panel)

**Metric:** Message rate by hour
**Visualization:** Stat

**Query (Prometheus):**
```promql
max_over_time(
  sum(rate(alpaca_messages_received_total[1h])) by (hour)
[7d])
```

**Display:**
```
Peak: 10-11 AM EST
250 msg/min
(Market Open)
```

---

### Panel 4.5: Data Source Usage (7-Day Trend) (Stacked Area Graph)

**Metric:** Message count by source
**Visualization:** Stacked area chart

**Query (Prometheus):**
```promql
sum(rate(alpaca_messages_received_total[1h])) by (source)
sum(rate(yahoo_messages_received_total[1h])) by (source)
```

**Display:**
- Alpaca (blue, top)
- Yahoo (orange, bottom)
- Shows percentage distribution over time

---

### Panel 4.6: Most Active Symbols (Table Panel)

**Metrics:** Symbol activity statistics
**Visualization:** Table

**Query (Prometheus + API):**
```promql
topk(10,
  sum(increase(alpaca_messages_received_total[1h])) by (symbol)
)
```

**Columns:**
1. Symbol
2. Messages (last 1 hour)
3. Subscribers (unique users)
4. Avg Price Change (%)

---

### Panel 4.7: Peak Usage Hours (Bar Chart)

**Metric:** Message rate by hour of day
**Visualization:** Bar chart

**Query (Prometheus):**
```promql
avg_over_time(
  sum(rate(alpaca_messages_received_total[1h])) by (hour(timestamp))
[7d])
```

**X-Axis:** Hour of day (9 AM - 4 PM EST)
**Y-Axis:** Messages per minute

---

### Panel 4.8: Cost Tracking (Stat Panel)

**Metric:** API usage and cost
**Visualization:** Stat with breakdown

**Query (API + Configuration):**
```promql
# Symbol count
sum(alpaca_subscribed_symbols_count)

# Tier information
alpaca_tier_info
```

**Display:**
```
Alpaca: Free Tier
Current: 30 symbols
Limit: 30 symbols
Estimated Cost: $0/mo

Upgrade to Unlimited:
$99/mo (500 symbols)
```

---

## 5. Dashboard Export & Implementation

### 5.1 Grafana Dashboard JSON Export

Each dashboard should be exported as JSON for version control and easy deployment.

**Export Process:**
1. Configure dashboard in Grafana UI
2. Click "Dashboard settings" → "JSON Model"
3. Copy JSON and save to repo
4. Add to version control

**File Naming:**
```
dashboards/alpaca-operations.json
dashboards/alpaca-performance.json
dashboards/alpaca-reliability.json
dashboards/alpaca-business.json
```

---

### 5.2 Deployment via Provisioning

**Grafana Provisioning File:**
```yaml
# grafana/provisioning/dashboards/alpaca.yaml
apiVersion: 1

providers:
  - name: 'Alpaca Dashboards'
    orgId: 1
    folder: 'Alpaca Streaming'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 30
    allowUiUpdates: true
    options:
      path: /etc/grafana/provisioning/dashboards/alpaca
```

---

### 5.3 Variables Configuration

**Dashboard Variables (Template Variables):**

```yaml
variables:
  - name: datasource
    type: datasource
    query: prometheus

  - name: refresh_rate
    type: interval
    options:
      - 5s
      - 10s
      - 30s
      - 1m
      - 5m
    current: 30s

  - name: time_range
    type: custom
    options:
      - Last 5 minutes
      - Last 15 minutes
      - Last 1 hour
      - Last 24 hours
      - Last 7 days
    current: Last 1 hour
```

---

## 6. Dashboard Access Control

### 6.1 Role-Based Access

| Dashboard | Viewer | Editor | Admin |
|-----------|--------|--------|-------|
| Operations | All engineers, Support | SRE, On-call | SRE Lead |
| Performance | All engineers | SRE, Backend devs | SRE Lead |
| Reliability | SRE, On-call, Managers | SRE | SRE Lead |
| Business | All engineers, Product | Product Manager | VP Engineering |

---

### 6.2 Alerting Integration

Each dashboard should link to related alerts:

**Alert Links:**
```json
{
  "links": [
    {
      "title": "Related Alerts",
      "url": "/alerting/list?search=alpaca",
      "icon": "bell"
    },
    {
      "title": "Runbook",
      "url": "https://wiki/runbooks/alpaca",
      "icon": "doc"
    }
  ]
}
```

---

## 7. Maintenance & Updates

### 7.1 Dashboard Review Schedule

**Frequency:** Monthly
**Participants:** SRE Team + Backend Engineers
**Agenda:**
1. Review dashboard usage analytics
2. Identify unused panels
3. Add new metrics based on incidents
4. Update thresholds if SLOs change
5. Incorporate user feedback

---

### 7.2 Version Control

**Git Repository:**
```
dashboards/
├── README.md
├── alpaca-operations.json (v1.2)
├── alpaca-performance.json (v1.1)
├── alpaca-reliability.json (v1.0)
└── alpaca-business.json (v1.0)
```

**Changelog:**
```markdown
## v1.2 - 2025-10-15
- Added circuit breaker panel to reliability dashboard
- Updated latency thresholds based on SLO review

## v1.1 - 2025-10-10
- Added symbol subscription count
- Fixed time range variable
```

---

## 8. Testing Checklist

Before deploying dashboards to production:

- [ ] All panels render without errors
- [ ] All queries return data (no empty panels)
- [ ] Thresholds are configured correctly (colors match expected values)
- [ ] Refresh rates are appropriate for each dashboard
- [ ] Variables work correctly (datasource, time range)
- [ ] Links to runbooks and alerts are valid
- [ ] Dashboard is accessible by intended audience
- [ ] Mobile view is acceptable (responsive layout)
- [ ] Annotations appear for deployments and incidents
- [ ] Dashboard loads in <3 seconds

---

## 9. Appendix A: Query Optimization Tips

### 9.1 Use Recording Rules

For expensive queries that run frequently:

**prometheus.rules.yml:**
```yaml
groups:
  - name: alpaca_precomputed
    interval: 30s
    rules:
      - record: alpaca:message_rate:1m
        expr: sum(rate(alpaca_messages_received_total[1m])) * 60

      - record: alpaca:latency:p95
        expr: histogram_quantile(0.95, sum(rate(alpaca_message_latency_seconds_bucket[5m])) by (le))
```

Then use in dashboards:
```promql
alpaca:message_rate:1m
alpaca:latency:p95
```

---

### 9.2 Efficient Range Queries

Use appropriate range selectors:
- **5m**: Real-time dashboards (operations)
- **1h**: Performance analysis
- **24h**: Reliability trends
- **7d**: Business insights

Avoid overly long ranges that slow down queries.

---

## 10. Appendix B: Alternative Platforms

### 10.1 Application Insights Dashboards

For Azure-hosted environments, use Application Insights workbooks:

**Query Language:** Kusto Query Language (KQL)

**Example Query:**
```kql
customMetrics
| where name == "alpaca_connection_status"
| summarize avg(value) by bin(timestamp, 5m)
| render timechart
```

---

### 10.2 Datadog Dashboards

For Datadog users:

**Dashboard JSON:** Similar structure to Grafana
**Query Language:** Datadog Query Language

**Example Widget:**
```json
{
  "definition": {
    "type": "timeseries",
    "requests": [{
      "q": "avg:alpaca.message_rate{*}",
      "display_type": "line"
    }],
    "title": "Alpaca Message Rate"
  }
}
```

---

## Document Approval

| Role | Name | Date |
|------|------|------|
| SRE Lead | TBD | 2025-10-09 |
| Backend Lead | TBD | 2025-10-09 |
| Product Manager | TBD | 2025-10-09 |

---

**End of Document**
