# Alpaca Streaming Integration - Executive Summary

**Document Version:** 1.0
**Date:** October 9, 2025
**Full Specification:** ALPACA_STREAMING_REQUIREMENTS_SPECIFICATION.md

---

## Overview

Integrate Alpaca WebSocket streaming as the **primary real-time data source** for NASDAQ/NYSE stocks, replacing the current 60-second Yahoo Finance polling with sub-second updates. Yahoo Finance remains as both a fallback mechanism and the persistence layer.

---

## Business Objectives

| Objective | Current State | Target State | Impact |
|-----------|---------------|--------------|--------|
| **Price Freshness** | 60-second delay | <1 second real-time | 60x improvement |
| **User Experience** | Stale prices | Professional-grade real-time | Competitive advantage |
| **System Reliability** | Single source (Yahoo) | Primary + Fallback | Zero downtime |
| **Data Quality** | Polling accuracy | Streaming accuracy | Better price tracking |

---

## Key Success Metrics (KPIs)

- **Latency**: <2 seconds (P95) from market event to frontend display
- **Availability**: >99.5% uptime for stock price streaming
- **Fallback Speed**: <5 seconds to switch from Alpaca to Yahoo on failure
- **Data Loss**: 0% during source transitions

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                  REAL-TIME PATH (PRIMARY)                │
│                                                           │
│  Alpaca WebSocket → AlpacaStreamingService →             │
│  DataSourceRouter → MultiAssetDataBroadcast →            │
│  SignalR Hubs → Frontend (<1s latency)                   │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                 FALLBACK PATH (BACKUP)                   │
│                                                           │
│  Yahoo Finance API → YahooFinancePollingService →        │
│  DataSourceRouter → MultiAssetDataBroadcast →            │
│  SignalR Hubs → Frontend (60s polling)                   │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              PERSISTENCE PATH (INDEPENDENT)              │
│                                                           │
│  Yahoo Finance API → YahooFinancePollingService →        │
│  market_data table (every 5 minutes)                     │
└─────────────────────────────────────────────────────────┘
```

---

## Core Components

### 1. AlpacaStreamingService (NEW)
- Maintains persistent WebSocket connection to Alpaca
- Authenticates with API keys
- Subscribes to real-time trades/quotes
- Emits `StockPriceUpdated` events
- Handles reconnection with exponential backoff

### 2. DataSourceRouter (NEW)
- Routes events from primary (Alpaca) or fallback (Yahoo)
- State machine: `PRIMARY_ACTIVE`, `FALLBACK_ACTIVE`, `BOTH_UNAVAILABLE`
- Automatic switching based on health checks
- Grace period to prevent flapping

### 3. MultiAssetDataBroadcastService (MODIFY)
- Accepts events from DataSourceRouter
- Adds source metadata (`ALPACA` or `YAHOO_FALLBACK`)
- Broadcasts to SignalR hubs (no changes to hubs)

### 4. YahooFinancePollingService (CONTINUE AS-IS)
- Continues 5-minute polling for persistence
- Fires events for fallback routing
- Independent of Alpaca availability

---

## Fallback Mechanism

### Trigger Conditions (Switch to Yahoo)
- Alpaca connection lost for >10 seconds
- No messages received for >30 seconds
- 3 consecutive health check failures

### Recovery Conditions (Switch back to Alpaca)
- Alpaca connection restored and authenticated
- Subscription confirmed
- At least 1 message received successfully
- 10-second grace period elapsed

### User Experience During Fallback
- Yellow "DELAYED" badge replaces green "LIVE" badge
- Notification: "Real-time data temporarily unavailable, using delayed data"
- Prices continue updating every 60 seconds via Yahoo

---

## Data Field Mapping

| Alpaca Field | MyTrader Field | Notes |
|--------------|----------------|-------|
| `p` (trade price) | `Price` | Primary price field |
| `bp`/`ap` (bid/ask) | `BidPrice`/`AskPrice` | Alpaca advantage (Yahoo doesn't provide) |
| `v` (bar volume) | `Volume` | Aggregate volume |
| `o`/`h`/`l`/`c` | OHLC fields | Bar data (1-minute aggregates) |
| `t` (timestamp) | `Timestamp` | ISO 8601 format |

**Calculated Fields:**
- `PriceChange` = current_price - previous_close
- `PriceChangePercent` = (PriceChange / previous_close) * 100

---

## Rate Limits & Constraints

### Alpaca Free Tier (IEX Feed)
- **Concurrent connections**: 1 per account
- **Symbol subscriptions**: 30 symbols max
- **Data delay**: Real-time (no delay)
- **Cost**: $0/month

### Alpaca Unlimited Plan (SIP Feed)
- **Concurrent connections**: 30 per account
- **Symbol subscriptions**: Unlimited
- **Data delay**: Real-time
- **Cost**: $99/month

**Symbol Prioritization Strategy (if >30 symbols):**
1. User portfolio holdings
2. User watchlists
3. Popular symbols (is_popular = true)
4. High volume symbols (ORDER BY volume_24h DESC)

---

## Integration Points

### Services to Modify
- ✅ **MultiAssetDataBroadcastService**: Add source tracking (LOW complexity)
- ✅ **YahooFinancePollingService**: Continue as-is, add metadata (LOW complexity)
- ✅ **Program.cs**: Register new services (LOW complexity)

### Services to Create
- 🆕 **AlpacaStreamingService**: WebSocket connection management
- 🆕 **DataSourceRouter**: Primary/fallback routing logic
- 🆕 **AlpacaHealthMonitor**: Health checks and status endpoint

### No Changes Required
- ❌ **MarketDataHub**: Zero changes
- ❌ **DashboardHub**: Zero changes
- ❌ **Frontend**: Backward compatible (optional UI enhancements)
- ❌ **Database Schema**: Zero changes

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Alpaca rate limits exceeded | Symbol prioritization; upgrade to paid tier |
| WebSocket instability | Exponential backoff; automatic fallback |
| Both sources unavailable | Cache last 100 updates per symbol; display stale data with warning |
| Price discrepancy between sources | Log warnings; sanity check (±5% delta) |
| Authentication failure | Retry with backoff; alert on 5+ failures |
| Symbol limit (30 symbols) | Prioritize user portfolios; implement rotation |

---

## Testing Strategy

### Unit Tests (>80% coverage)
- AlpacaStreamingService: Parse messages, calculate changes
- DataSourceRouter: State transitions, routing logic
- AlpacaHealthMonitor: Health checks, status reporting

### Integration Tests
- End-to-end Alpaca flow (WebSocket → Frontend)
- Fallback activation (Alpaca failure → Yahoo takeover)
- Primary recovery (Alpaca restore → Switch back)
- Both sources fail scenario
- Symbol hot reload

### Performance Tests
- Baseline: 30 symbols, 1 update/sec, 1 hour
- Burst: 10 updates/sec for 5 minutes
- 50 concurrent users
- 24-hour memory leak test

### Manual Testing
- Real-time price display
- Data source badge
- Fallback warning
- Recovery notification
- Mobile compatibility
- Cross-browser testing

---

## Deployment Plan

### Phase 1: Development (Week 1-2)
- Implement services with mock data
- Unit tests + integration tests
- Code review

### Phase 2: Staging (Week 3)
- Deploy to staging
- Connect to Alpaca sandbox
- QA testing

### Phase 3: Beta Release (Week 4)
- Deploy to production with **feature flag disabled**
- Enable for 5% of users
- Monitor metrics

### Phase 4: Gradual Rollout (Week 5-6)
- 25% → 50% → 100% of users
- 24-hour monitoring between steps

### Phase 5: Optimization (Week 7-8)
- Optimize based on production metrics
- Consider Alpaca paid tier upgrade

---

## Rollback Plan

### Trigger Conditions
- Alpaca connection failure rate >10%
- Fallback active >20% of time
- User-reported price inaccuracies
- System performance degradation (P95 latency >5s)

### Rollback Steps (< 5 minutes)
1. Set `FeatureFlags:EnableAlpacaStreaming = false`
2. Restart services (or hot reload)
3. Verify Yahoo Finance polling active
4. Monitor for 30 minutes
5. Notify stakeholders

---

## Monitoring & Alerts

### Health Endpoint
```
GET /api/health/alpaca
```

**Response:**
```json
{
  "status": "Healthy",
  "connectionState": "PRIMARY_ACTIVE",
  "alpacaStatus": {
    "connected": true,
    "subscribedSymbols": 25,
    "messagesPerMinute": 120
  },
  "yahooStatus": {
    "lastSync": "2025-10-09T14:25:00Z",
    "successRate": 98.5
  }
}
```

### Critical Alerts
- ⚠️ Alpaca disconnected for >60 seconds
- 🔴 Both sources unavailable
- ⚠️ Fallback active for >10 minutes
- ⚠️ Price discrepancy >5% between sources

---

## Cost Analysis

| Item | Free Tier | Unlimited Tier |
|------|-----------|----------------|
| Alpaca Subscription | $0/month | $99/month |
| Symbols Supported | 30 | Unlimited |
| Infrastructure | $0 (existing) | $0 (existing) |
| **Total Monthly Cost** | **$0** | **$99** |

**Decision Point:** Start with free tier (30 symbols sufficient for MVP). Upgrade if user demand requires >30 symbols.

---

## Configuration Example

```json
{
  "Alpaca": {
    "Streaming": {
      "Enabled": true,
      "WebSocketUrl": "wss://stream.data.alpaca.markets/v2/iex",
      "ApiKey": "${ALPACA_API_KEY}",
      "ApiSecret": "${ALPACA_API_SECRET}",
      "MaxSymbols": 30,
      "SubscribeToTrades": true,
      "SubscribeToQuotes": true,
      "ReconnectBaseDelayMs": 1000,
      "MessageTimeoutSeconds": 30
    },
    "Fallback": {
      "EnableYahooFallback": true,
      "FallbackActivationDelaySeconds": 10,
      "PrimaryRecoveryGracePeriodSeconds": 10,
      "MaxConsecutiveFailures": 3
    }
  },
  "FeatureFlags": {
    "EnableAlpacaStreaming": false
  }
}
```

---

## Success Criteria (90-Day Review)

| Metric | Target | Measurement |
|--------|--------|-------------|
| Price update latency | <2s (P95) | Application Insights |
| Alpaca connection uptime | >98% | Health endpoint logs |
| Fallback activation frequency | <5 times/day | DataSourceRouter metrics |
| User satisfaction (NPS) | >8/10 | User survey |
| System availability | >99.5% | Uptime monitoring |
| API cost per month | <$100 | Alpaca billing dashboard |

---

## Key Decisions Required

### Immediate (Week 1)
1. ✅ Approve Alpaca terms of service (Legal)
2. ✅ Obtain Alpaca API keys (DevOps)
3. ✅ Approve phased rollout plan (Product Manager)

### Before Production (Week 4)
4. ⏳ Decide on data source badge design (UX Team)
5. ⏳ Approve monitoring alert thresholds (SRE)
6. ⏳ Finalize symbol prioritization logic (Product Manager)

### Post-Launch (Week 8)
7. ⏳ Decide on Alpaca paid tier upgrade (Finance + Product Manager)
8. ⏳ Evaluate bid/ask price display in UI (UX Team)

---

## Open Questions

| Question | Impact | Owner | Target Date |
|----------|--------|-------|-------------|
| Should we display bid/ask prices in the UI? | Medium | UX Team | Week 2 |
| Do we need historical streaming data replay? | Low | Product Manager | Week 3 |
| Should we support extended hours trading data? | Medium | Product Manager | Week 4 |
| What is acceptable Alpaca cost threshold? | High | Finance/PM | Week 1 |

---

## Team Responsibilities

| Role | Responsibility |
|------|---------------|
| **Backend Engineer** | Implement AlpacaStreamingService, DataSourceRouter, AlpacaHealthMonitor |
| **Frontend Engineer** | Optional: Add data source badge UI enhancement |
| **QA Manual Tester** | Execute manual test cases, report bugs |
| **Integration Test Specialist** | Write and execute integration tests |
| **Performance Engineer** | Conduct load/stress testing, optimize bottlenecks |
| **SRE Observability Architect** | Configure monitoring, alerts, dashboards |
| **AppSec Guardian** | Security review, API key management audit |
| **Data Architecture Manager** | Validate market_data table performance |
| **Product Manager** | Approve requirements, prioritize features, user communication |

---

## Next Steps

1. **Approval**: Get stakeholder sign-off on requirements (by Week 1 Friday)
2. **Planning**: Backend engineer creates implementation plan (by Week 2 Monday)
3. **Kickoff**: Team kickoff meeting (Week 2 Tuesday)
4. **Development**: Start Phase 1 implementation (Week 2)
5. **Review**: Daily standups to track progress

---

## Quick Reference Links

- **Full Specification**: [ALPACA_STREAMING_REQUIREMENTS_SPECIFICATION.md](./ALPACA_STREAMING_REQUIREMENTS_SPECIFICATION.md)
- **Alpaca API Docs**: https://docs.alpaca.markets/docs/streaming-market-data
- **Project Board**: (Add Jira/GitHub Projects link here)
- **Slack Channel**: #alpaca-integration (create if not exists)

---

**Document Owner:** Business Analyst
**Last Updated:** October 9, 2025
**Next Review Date:** After Phase 3 (Beta Release)

---

## Appendix: Visual State Machine

```
              STARTUP
                 │
       ┌─────────┴─────────┐
       │                   │
  Alpaca OK           Alpaca Fail
       │                   │
       ↓                   ↓
 PRIMARY_ACTIVE      FALLBACK_ACTIVE
       │                   │
       │ Alpaca fail       │ Alpaca recover
       │ (3 failures       │ (+ 10s grace)
       │  OR 30s silence)  │
       └─────────┬─────────┘
                 │
                 ↓
        BOTH_UNAVAILABLE
                 │
                 │ Any recover
                 └──→ (return to active state)
```

---

**End of Executive Summary**
