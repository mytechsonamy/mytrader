# Alpaca Streaming Market Data Integration - Requirements Specification

**Document Version:** 1.0
**Date:** October 9, 2025
**Prepared By:** Business Analyst
**Target Audience:** Engineering Teams, QA, SRE, Product Management

---

## Executive Summary

This document specifies the requirements for integrating Alpaca's WebSocket streaming API as the **primary real-time data source** for NASDAQ/NYSE stock market data in the MyTrader platform. The integration establishes Alpaca as the master source for real-time price updates delivered to frontend applications, while maintaining Yahoo Finance as both a fallback mechanism and the persistence layer for historical data storage.

### Key Objectives

1. **Real-time streaming**: Replace polling-based Yahoo Finance with Alpaca WebSocket streaming (sub-second latency)
2. **Reliability**: Implement automatic fallback to Yahoo Finance when Alpaca is unavailable
3. **Data consistency**: Ensure seamless transitions between data sources without frontend disruption
4. **Persistence**: Continue using Yahoo Finance for 5-minute database writes
5. **Monitoring**: Comprehensive health checks and observability

### Business Impact

| Metric | Current (Yahoo Polling) | Target (Alpaca Streaming) | Improvement |
|--------|------------------------|---------------------------|-------------|
| Price update frequency | 60 seconds | <1 second | 60x faster |
| API costs | Low | Medium | Acceptable trade-off for real-time data |
| Data freshness | 60s delay | Real-time | Professional-grade UX |
| System complexity | Low | Medium | Manageable with proper architecture |

---

## 1. Background & Context

### 1.1 Current Architecture

**Existing Implementation:**
- **Crypto**: Binance WebSocket → BinanceWebSocketService → MultiAssetDataBroadcastService → SignalR → Frontend (WORKING)
- **Stocks**: Yahoo Finance polling (every 60s) → YahooFinancePollingService → MultiAssetDataBroadcastService → SignalR → Frontend (WORKING)
- **Database**: market_data table stores both crypto and stock data with asset_class column

**Current Data Flow (Stocks):**
```
Yahoo Finance API (REST, 60s poll)
    ↓
YahooFinancePollingService
    ↓ (event: StockPriceUpdated)
MultiAssetDataBroadcastService
    ↓ (SignalR broadcast)
MarketDataHub / DashboardHub
    ↓ (WebSocket)
Frontend (React/React Native)
```

### 1.2 Problem Statement

**Why Now?**
- Current 60-second polling creates stale price data unacceptable for professional trading simulation
- Competitors offer sub-second updates creating UX disadvantage
- User feedback indicates demand for real-time market data

**Success Metrics (KPIs):**
- Price update latency: <2 seconds (target: <1 second) from market event to frontend display
- System availability: >99.5% uptime for stock price streaming
- Fallback activation: <5 seconds to switch from Alpaca to Yahoo when failure detected
- Zero data loss during source transitions

---

## 2. Alpaca Streaming API Analysis

### 2.1 Authentication & Connection

**Alpaca WebSocket Endpoint:**
- **Production**: `wss://stream.data.alpaca.markets/v2/iex` (IEX feed for free tier)
- **Production (SIP)**: `wss://stream.data.alpaca.markets/v2/sip` (paid tier, all exchanges)
- **Sandbox**: `wss://stream.data.sandbox.alpaca.markets/v2/iex`

**Authentication Mechanism:**
```json
{
  "action": "auth",
  "key": "YOUR_API_KEY",
  "secret": "YOUR_API_SECRET"
}
```

**Connection Flow:**
1. Connect to WebSocket endpoint
2. Send authentication message within 10 seconds
3. Receive auth confirmation: `[{"T":"success","msg":"authenticated"}]`
4. Subscribe to symbols
5. Start receiving real-time messages

### 2.2 Message Format & Data Schema

**Subscription Message:**
```json
{
  "action": "subscribe",
  "trades": ["AAPL", "GOOGL"],
  "quotes": ["AAPL", "GOOGL"],
  "bars": ["AAPL"]
}
```

**Real-time Trade Message (Type: 't'):**
```json
{
  "T": "t",           // Type: trade
  "S": "AAPL",        // Symbol
  "i": 123456,        // Trade ID
  "x": "V",           // Exchange
  "p": 150.25,        // Price
  "s": 100,           // Size
  "t": "2025-10-09T14:30:00.123456Z", // Timestamp
  "c": ["@"],         // Conditions
  "z": "C"            // Tape
}
```

**Real-time Quote Message (Type: 'q'):**
```json
{
  "T": "q",           // Type: quote
  "S": "AAPL",        // Symbol
  "bx": "V",          // Bid exchange
  "bp": 150.20,       // Bid price
  "bs": 200,          // Bid size
  "ax": "Q",          // Ask exchange
  "ap": 150.25,       // Ask price
  "as": 300,          // Ask size
  "t": "2025-10-09T14:30:00.123456Z", // Timestamp
  "c": ["R"],         // Conditions
  "z": "C"            // Tape
}
```

**Bar Message (Type: 'b' - 1min aggregates):**
```json
{
  "T": "b",           // Type: bar
  "S": "AAPL",        // Symbol
  "o": 150.10,        // Open
  "h": 150.30,        // High
  "l": 150.05,        // Low
  "c": 150.25,        // Close
  "v": 12500,         // Volume
  "t": "2025-10-09T14:30:00Z", // Start time
  "n": 250,           // Number of trades
  "vw": 150.20        // VWAP
}
```

### 2.3 Data Field Mapping

| Alpaca Field | Yahoo Finance Equivalent | MyTrader Field | Notes |
|--------------|--------------------------|----------------|-------|
| `p` (trade price) | `regularMarketPrice` | `Price` | Primary price field |
| `bp`/`ap` | N/A | `BidPrice`/`AskPrice` | Alpaca advantage |
| `v` (bar volume) | `regularMarketVolume` | `Volume` | Aggregate volume |
| `o`/`h`/`l`/`c` | `open`/`dayHigh`/`dayLow`/`close` | OHLC fields | Bar data |
| `t` (timestamp) | Server time | `Timestamp` | ISO 8601 format |
| N/A | `regularMarketChangePercent` | `PriceChangePercent` | Calculate from previous close |

**Derived Fields (must calculate):**
- **PriceChange**: `current_price - previous_close`
- **PriceChangePercent**: `(PriceChange / previous_close) * 100`
- **Previous Close**: Fetch once at market open or from database

### 2.4 Rate Limits & Connection Constraints

| Constraint | Free Tier (IEX) | Unlimited Plan (SIP) |
|------------|-----------------|---------------------|
| Concurrent connections | 1 per account | 30 per account |
| Symbol subscriptions | 30 symbols max | Unlimited |
| Message rate | Limited to feed | Unlimited |
| Data delay | Real-time | Real-time |
| Authentication window | 10 seconds | 10 seconds |
| Reconnection policy | Exponential backoff | Exponential backoff |

**Rate Limit Handling:**
- Alpaca does not have explicit rate limits for streaming once connected
- Connection establishment: Respect 10-second auth window
- Subscription changes: No documented limit, but batch subscriptions

**Symbol Prioritization Strategy:**
- If >30 symbols needed (free tier), prioritize by:
  1. User portfolio holdings
  2. User watchlist
  3. Volume leaders (top 20 by daily volume)

### 2.5 Error Handling & Reconnection

**Error Message Format:**
```json
{
  "T": "error",
  "code": 406,
  "msg": "connection limit exceeded"
}
```

**Common Error Codes:**
- `400`: Invalid request (malformed JSON, invalid action)
- `401`: Unauthorized (invalid API keys)
- `402`: Auth timeout (>10 seconds)
- `403`: Forbidden (insufficient permissions)
- `404`: Symbol not found
- `406`: Connection limit exceeded
- `409`: Conflicting connection (duplicate connection)
- `500`: Internal server error

**Reconnection Strategy:**
```
Attempt 1: Wait 1 second
Attempt 2: Wait 2 seconds
Attempt 3: Wait 4 seconds
Attempt 4: Wait 8 seconds
Attempt 5: Wait 16 seconds
Max wait: 60 seconds (cap exponential backoff)
Max attempts: Unlimited (continuous retry)
```

---

## 3. Functional Requirements

### FR-001: Alpaca WebSocket Service Implementation

**Priority:** HIGH
**Status:** NEW

**Description:**
Implement a dedicated `AlpacaStreamingService` that establishes and maintains a persistent WebSocket connection to Alpaca's market data feed for NASDAQ/NYSE symbols.

**Acceptance Criteria:**
1. Service connects to Alpaca WebSocket endpoint on startup
2. Authentication completes within 10 seconds using configured API keys
3. Service subscribes to trades and quotes for active stock symbols from database
4. Service receives and parses real-time trade/quote/bar messages
5. Service emits `StockPriceUpdated` events matching existing `StockPriceData` structure
6. Service reconnects automatically on disconnection with exponential backoff
7. Service logs all connection state changes (connected, authenticated, subscribed, disconnected, error)
8. Service exposes health status endpoint

**Data Structure (Event):**
```csharp
public class StockPriceData
{
    public string Symbol { get; set; }
    public AssetClassCode AssetClass { get; set; } = AssetClassCode.STOCK;
    public string Market { get; set; } // "NASDAQ" or "NYSE"
    public decimal Price { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = "ALPACA"; // NEW
}
```

**Dependencies:**
- Configuration: `appsettings.json` Alpaca section (API keys, endpoint)
- Database: Symbols table (WHERE AssetClass='STOCK' AND IsActive=true)
- External: Alpaca WebSocket API

**Out of Scope:**
- Crypto streaming (already handled by Binance)
- Historical data fetching (Yahoo Finance continues this)
- Order execution (not part of this feature)

---

### FR-002: Data Source Router (Alpaca Primary, Yahoo Fallback)

**Priority:** HIGH
**Status:** NEW

**Description:**
Implement a routing mechanism that uses Alpaca as the primary real-time source and automatically switches to Yahoo Finance polling when Alpaca becomes unavailable.

**Acceptance Criteria:**
1. Router maintains state: `PRIMARY_ACTIVE`, `FALLBACK_ACTIVE`, `BOTH_UNAVAILABLE`
2. Router subscribes to both AlpacaStreamingService events and YahooFinancePollingService events
3. In `PRIMARY_ACTIVE` state:
   - Forward Alpaca events to MultiAssetDataBroadcastService
   - Ignore Yahoo Finance events (but Yahoo continues polling for DB persistence)
4. In `FALLBACK_ACTIVE` state:
   - Forward Yahoo Finance events to MultiAssetDataBroadcastService
   - Set `Source = "YAHOO_FALLBACK"` in events
   - Send user notification "Real-time data temporarily unavailable, using delayed data"
5. Switch to `FALLBACK_ACTIVE` when:
   - Alpaca connection lost for >10 seconds
   - Alpaca health check fails 3 consecutive times
   - No messages received from Alpaca for >30 seconds
6. Switch back to `PRIMARY_ACTIVE` when:
   - Alpaca connection restored and authenticated
   - Alpaca subscription confirmed
   - At least 1 message received successfully
   - Grace period of 10 seconds elapsed (avoid flapping)
7. Router logs all state transitions with timestamp and reason
8. Router exposes current state via health endpoint

**State Transition Rules:**
```
STARTUP → PRIMARY_ACTIVE (if Alpaca connects successfully)
STARTUP → FALLBACK_ACTIVE (if Alpaca fails to connect after 30s)

PRIMARY_ACTIVE → FALLBACK_ACTIVE (on Alpaca failure conditions)
FALLBACK_ACTIVE → PRIMARY_ACTIVE (on Alpaca recovery + 10s grace period)

Any State → BOTH_UNAVAILABLE (if both sources fail)
BOTH_UNAVAILABLE → PRIMARY_ACTIVE or FALLBACK_ACTIVE (on recovery)
```

**Data Consistency Requirement:**
- Price values must be within 5% between sources (sanity check)
- If discrepancy detected, log warning but don't block updates
- Track source changes per symbol in memory for debugging

---

### FR-003: Multi-Asset Broadcast Service Integration

**Priority:** HIGH
**Status:** MODIFY_EXISTING

**Description:**
Modify the existing `MultiAssetDataBroadcastService` to accept events from the new Data Source Router without requiring changes to SignalR hubs or frontend clients.

**Acceptance Criteria:**
1. MultiAssetDataBroadcastService accepts `StockPriceData` events with `Source` field
2. Service continues existing behavior:
   - Convert to `MultiAssetPriceUpdate` format
   - Broadcast to SignalR groups: `STOCK_{symbol}`, `AssetClass_STOCK`
   - Apply throttling (max 20 updates/sec/symbol)
3. Service adds `Source` metadata to broadcasts:
   ```csharp
   Metadata = new Dictionary<string, object>
   {
       { "source", priceUpdate.Source }, // "ALPACA" or "YAHOO_FALLBACK"
       { "originalTimestamp", priceUpdate.Timestamp }
   }
   ```
4. Service logs source changes per symbol (first Alpaca update after Yahoo, vice versa)
5. No changes required to existing event signatures or hub methods

**Backward Compatibility:**
- All existing crypto functionality unaffected
- All existing Yahoo stock functionality continues (as fallback)
- Frontend receives same message format (Source field optional for legacy clients)

---

### FR-004: Yahoo Finance Persistence Layer

**Priority:** MEDIUM
**Status:** MODIFY_EXISTING

**Description:**
Ensure Yahoo Finance continues polling every 5 minutes and writing to the `market_data` table for historical persistence, independent of real-time streaming.

**Acceptance Criteria:**
1. YahooFinancePollingService continues existing behavior:
   - Poll every 5 minutes (configurable)
   - Write to market_data table
   - Fire `StockPriceUpdated` events (for fallback routing)
2. Service adds metadata to database records:
   ```sql
   INSERT INTO market_data (symbol, timeframe, timestamp, open, high, low, close, volume, asset_class, source)
   VALUES ('AAPL', '5MIN', NOW(), ..., 'YAHOO');
   ```
3. Service does NOT depend on Alpaca availability
4. Service logs sync status every cycle (success count, failure count)

**Rationale:**
- Alpaca streaming provides real-time frontend updates
- Yahoo polling provides historical data for backtesting
- Separation of concerns: real-time vs persistence

---

### FR-005: Health Monitoring & Observability

**Priority:** MEDIUM
**Status:** NEW

**Description:**
Implement comprehensive health checks and monitoring for Alpaca streaming integration.

**Acceptance Criteria:**
1. Health endpoint `/api/health/alpaca` returns:
   ```json
   {
     "status": "Healthy",
     "connectionState": "PRIMARY_ACTIVE",
     "alpacaStatus": {
       "connected": true,
       "authenticated": true,
       "subscribedSymbols": 25,
       "lastMessageReceived": "2025-10-09T14:30:00Z",
       "messagesPerMinute": 120,
       "connectionUptime": "00:45:30"
     },
     "yahooStatus": {
       "lastSync": "2025-10-09T14:25:00Z",
       "successRate": 98.5,
       "symbolCount": 30
     },
     "fallbackCount": 2,
     "lastFallback": "2025-10-09T12:15:00Z",
     "timestamp": "2025-10-09T14:30:05Z"
   }
   ```
2. Metrics exposed:
   - Alpaca connection uptime %
   - Alpaca message rate (msg/sec)
   - Fallback activation count
   - Time in fallback mode (cumulative)
   - Yahoo sync success rate
3. Alerts triggered:
   - Alpaca disconnected for >60 seconds
   - Fallback active for >10 minutes
   - Both sources unavailable
   - Price discrepancy >5% between sources

**Monitoring Tools Integration:**
- Prometheus metrics export (if using Prometheus)
- Application Insights (if using Azure)
- Log aggregation (structured JSON logs)

---

### FR-006: Configuration Management

**Priority:** MEDIUM
**Status:** MODIFY_EXISTING

**Description:**
Extend `appsettings.json` configuration to support Alpaca streaming with feature flags.

**Acceptance Criteria:**
1. Configuration structure:
   ```json
   {
     "Alpaca": {
       "Streaming": {
         "Enabled": true,
         "WebSocketUrl": "wss://stream.data.alpaca.markets/v2/iex",
         "ApiKey": "PK***",
         "ApiSecret": "***",
         "ReconnectMaxAttempts": -1,
         "ReconnectBaseDelayMs": 1000,
         "ReconnectMaxDelayMs": 60000,
         "AuthTimeoutSeconds": 10,
         "MessageTimeoutSeconds": 30,
         "HealthCheckIntervalSeconds": 60,
         "MaxSymbols": 30,
         "SubscribeToTrades": true,
         "SubscribeToQuotes": true,
         "SubscribeToBars": false
       },
       "Fallback": {
         "EnableYahooFallback": true,
         "FallbackActivationDelaySeconds": 10,
         "PrimaryRecoveryGracePeriodSeconds": 10,
         "MaxConsecutiveFailures": 3
       }
     }
   }
   ```
2. Configuration validation on startup:
   - API keys not empty
   - WebSocket URL valid
   - Delay values positive integers
3. Hot reload support (optional): Configuration changes apply without restart

**Environment-Specific Config:**
- Development: Use paper trading keys, log debug messages
- Production: Use live keys, log warnings/errors only

---

### FR-007: Symbol Management Integration

**Priority:** LOW
**Status:** MODIFY_EXISTING

**Description:**
Ensure Alpaca streaming service dynamically loads symbols from the database and supports hot-reload when symbols are added/removed.

**Acceptance Criteria:**
1. On startup, AlpacaStreamingService queries:
   ```sql
   SELECT ticker, venue FROM symbols
   WHERE asset_class = 'STOCK'
     AND is_active = true
     AND is_tracked = true
   ORDER BY is_popular DESC, volume_24h DESC
   LIMIT 30;  -- Respect Alpaca free tier limit
   ```
2. Service subscribes to all active stock symbols
3. Service supports hot-reload (without restart):
   - Periodic refresh every 5 minutes
   - Admin API endpoint `/api/admin/alpaca/reload-symbols` triggers immediate reload
4. When symbols change:
   - Unsubscribe from removed symbols
   - Subscribe to new symbols
   - Log symbol list changes

**Priority Logic (if >30 symbols):**
1. User portfolio holdings (from user_portfolios + portfolio_positions)
2. User watchlists (if implemented)
3. Popular symbols (is_popular = true)
4. High volume symbols (ORDER BY volume_24h DESC)

---

## 4. Non-Functional Requirements

### NFR-001: Performance

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Price update latency (market → frontend) | <2 seconds (P95) | Timestamp diff: Alpaca message time → frontend received time |
| Message throughput | >500 msg/sec | Alpaca service counter |
| Broadcast latency (service → SignalR) | <100ms (P95) | Internal timing logs |
| Memory footprint (Alpaca service) | <200 MB | Process monitor |
| CPU usage (Alpaca service) | <10% avg | Process monitor |
| Database write latency (Yahoo) | <500ms (P95) | EF Core timing |

**Performance Testing:**
- Load test: 30 symbols × 1 update/sec = 30 msg/sec sustained for 1 hour
- Spike test: Simulate market open (high volume) burst
- Stress test: 100 symbols (simulate future unlimited plan)

---

### NFR-002: Reliability & Availability

| Metric | Target | Recovery Strategy |
|--------|--------|------------------|
| Overall system availability | >99.5% | Automatic fallback to Yahoo |
| Alpaca connection uptime | >98% | Exponential backoff reconnection |
| Fallback activation time | <5 seconds | Health check every 5 seconds |
| Recovery time (Alpaca → Primary) | <30 seconds | Grace period + validation |
| Data loss during fallback | 0% | Buffer last 10 updates per symbol |
| Message processing success rate | >99.9% | Retry failed messages 3x |

**Resilience Patterns:**
1. **Circuit Breaker**: Open after 3 consecutive Alpaca failures, close after successful recovery
2. **Retry with Backoff**: Reconnection attempts with exponential backoff
3. **Graceful Degradation**: Switch to Yahoo fallback instead of failure
4. **Health Checks**: Active monitoring every 60 seconds

---

### NFR-003: Data Quality & Consistency

| Requirement | Specification | Validation |
|-------------|---------------|------------|
| Price accuracy | Match Alpaca raw data exactly | Automated test comparing raw JSON to parsed values |
| Timestamp precision | Millisecond precision | Validate ISO 8601 parsing |
| Symbol format consistency | Uppercase, no spaces | Normalize on ingestion |
| Data freshness | Timestamp within 2 seconds of current time | Alert if staleness detected |
| Cross-source consistency | Alpaca vs Yahoo price delta <5% | Log warnings for discrepancies |

**Data Quality Checks:**
- Validate price >0
- Validate volume ≥0
- Validate timestamp not in future
- Validate symbol exists in database
- Validate price change calculation accuracy

---

### NFR-004: Security

| Requirement | Implementation | Verification |
|-------------|----------------|--------------|
| API key storage | Encrypted in appsettings.json or Azure Key Vault | Penetration test |
| API key transmission | TLS 1.2+ for WebSocket connection | Network trace |
| API key rotation | Support key rotation without downtime | Manual test procedure |
| Access control | Only authorized services access Alpaca credentials | Code review |
| Audit logging | Log all auth attempts, subscriptions | Log analysis |

**Security Considerations:**
- Never log API keys (sanitize logs)
- Use different keys for dev/staging/prod
- Rotate keys every 90 days
- Monitor for unauthorized access attempts

---

### NFR-005: Scalability

| Dimension | Current | Target (12 months) | Strategy |
|-----------|---------|-------------------|----------|
| Symbols supported | 30 (free tier) | 500 (unlimited tier) | Upgrade Alpaca plan |
| Concurrent users | 50 | 500 | SignalR scale-out with Redis backplane |
| Messages per second | 30 | 500 | Vertical scaling + message batching |
| Database writes per minute | 6 (30 symbols / 5 min) | 100 | Batch inserts, indexed tables |

**Scalability Strategy:**
- **Phase 1** (Current): Single server, 30 symbols, free tier
- **Phase 2** (Q2 2026): Multi-server, Redis backplane, 100 symbols, paid tier
- **Phase 3** (Q4 2026): Kubernetes deployment, 500 symbols, unlimited tier

---

### NFR-006: Observability

**Logging Requirements:**
- **Debug level**: Message payloads, subscription changes (dev only)
- **Info level**: Connection state changes, fallback activations, symbol reloads
- **Warning level**: Reconnection attempts, price discrepancies, slow messages
- **Error level**: Auth failures, subscription errors, parsing failures

**Structured Logging Format:**
```json
{
  "timestamp": "2025-10-09T14:30:00.123Z",
  "level": "INFO",
  "service": "AlpacaStreamingService",
  "event": "ConnectionEstablished",
  "details": {
    "endpoint": "wss://stream.data.alpaca.markets/v2/iex",
    "symbolCount": 25,
    "authDuration": "1.2s"
  },
  "correlationId": "abc-123-def-456"
}
```

**Tracing:**
- Trace each price update from Alpaca message → SignalR broadcast
- Distributed tracing with correlation IDs
- OpenTelemetry support (optional)

---

## 5. System Architecture

### 5.1 Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        MyTrader Backend                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Real-Time Data Flow (Primary)                │  │
│  │                                                            │  │
│  │   Alpaca WebSocket (wss://...)                            │  │
│  │         ↓                                                  │  │
│  │   AlpacaStreamingService                                   │  │
│  │         ↓ (events: StockPriceUpdated)                     │  │
│  │   DataSourceRouter ←─────────────┐                        │  │
│  │         ↓                         │                        │  │
│  │   MultiAssetDataBroadcastService  │                        │  │
│  │         ↓                         │                        │  │
│  │   MarketDataHub / DashboardHub    │                        │  │
│  │         ↓ (SignalR WebSocket)     │                        │  │
│  │   Frontend (React/React Native)   │                        │  │
│  └────────────────────────────────────┼───────────────────────┘  │
│                                       │                           │
│  ┌───────────────────────────────────┼───────────────────────┐  │
│  │       Fallback Data Flow (Backup) │                       │  │
│  │                                    │                       │  │
│  │   Yahoo Finance API (REST)         │                       │  │
│  │         ↓ (polling every 60s)      │                       │  │
│  │   YahooFinancePollingService       │                       │  │
│  │         ↓ (events: StockPriceUpdated)                     │  │
│  │   DataSourceRouter ────────────────┘                       │  │
│  │                                                            │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │          Persistence Layer (Independent)                   │  │
│  │                                                            │  │
│  │   YahooFinancePollingService                               │  │
│  │         ↓ (every 5 minutes)                                │  │
│  │   market_data table (PostgreSQL)                           │  │
│  │         ↓                                                  │  │
│  │   Historical data for backtesting                          │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                 Health & Monitoring                        │  │
│  │                                                            │  │
│  │   HealthCheckService                                       │  │
│  │         ↓                                                  │  │
│  │   /api/health/alpaca (HTTP endpoint)                       │  │
│  │   Prometheus metrics                                       │  │
│  │   Application Insights                                     │  │
│  └────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Data Flow Diagrams

#### Normal Operation (Alpaca Primary)

```
┌─────────────┐
│ Alpaca WSS  │ Trade: AAPL @ $150.25
└──────┬──────┘
       │
       ↓ <1s latency
┌──────────────────────────┐
│ AlpacaStreamingService   │
│ - Parse JSON message     │
│ - Calculate % change     │
│ - Emit StockPriceUpdated │
└──────┬───────────────────┘
       │
       ↓ Event
┌──────────────────────────┐
│ DataSourceRouter         │
│ State: PRIMARY_ACTIVE    │
│ Action: Forward event    │
└──────┬───────────────────┘
       │
       ↓ Event
┌──────────────────────────┐
│ MultiAssetDataBroadcast  │
│ - Apply throttling       │
│ - Add metadata           │
│ - Broadcast to groups    │
└──────┬───────────────────┘
       │
       ↓ SignalR
┌──────────────────────────┐
│ MarketDataHub            │
│ Groups: STOCK_AAPL       │
│        AssetClass_STOCK  │
└──────┬───────────────────┘
       │
       ↓ WebSocket
┌──────────────────────────┐
│ Frontend Client          │
│ Display: $150.25 +0.5%   │
│ Source: ALPACA (badge)   │
└──────────────────────────┘
```

#### Fallback Scenario (Alpaca Disconnected)

```
┌─────────────┐
│ Alpaca WSS  │ ✗ Connection lost
└──────┬──────┘
       │
       ↓ No messages for 30s
┌──────────────────────────┐
│ AlpacaStreamingService   │
│ - Health check fails     │
│ - Attempt reconnect      │
│ - Status: DISCONNECTED   │
└──────┬───────────────────┘
       │
       ↓ Failure detected
┌──────────────────────────┐
│ DataSourceRouter         │
│ State: FALLBACK_ACTIVE   │
│ Action: Switch to Yahoo  │
│ Notification: "Delayed"  │
└──────┬───────────────────┘
       │
       ↓ Poll 60s
┌──────────────────────────┐
│ YahooFinancePollingService
│ - Fetch AAPL price       │
│ - Emit StockPriceUpdated │
│ - Source: YAHOO_FALLBACK │
└──────┬───────────────────┘
       │
       ↓ Event
┌──────────────────────────┐
│ DataSourceRouter         │
│ Action: Forward event    │
└──────┬───────────────────┘
       │
       ↓ Event
┌──────────────────────────┐
│ MultiAssetDataBroadcast  │
│ - Broadcast with source  │
└──────┬───────────────────┘
       │
       ↓ SignalR
┌──────────────────────────┐
│ Frontend Client          │
│ Display: $150.20 +0.4%   │
│ Badge: "DELAYED DATA"    │
│ Color: Yellow warning    │
└──────────────────────────┘
```

#### Recovery Scenario (Alpaca Restored)

```
┌─────────────┐
│ Alpaca WSS  │ ✓ Connection restored
└──────┬──────┘
       │
       ↓ Auth successful
┌──────────────────────────┐
│ AlpacaStreamingService   │
│ - Subscribe to symbols   │
│ - Receive messages       │
│ - Status: CONNECTED      │
└──────┬───────────────────┘
       │
       ↓ Messages flowing
┌──────────────────────────┐
│ DataSourceRouter         │
│ - Wait 10s grace period  │
│ - Validate message rate  │
│ - State: PRIMARY_ACTIVE  │
│ - Stop forwarding Yahoo  │
│ - Notification: "Restored"
└──────┬───────────────────┘
       │
       ↓ Resume primary flow
┌──────────────────────────┐
│ Frontend Client          │
│ Badge: "REAL-TIME"       │
│ Color: Green success     │
└──────────────────────────┘
```

### 5.3 State Machine (DataSourceRouter)

```
                 ┌─────────────────┐
        ┌────────│     STARTUP     │────────┐
        │        └─────────────────┘        │
        │                                   │
        │ Alpaca OK                         │ Alpaca Fail
        │                                   │
        ↓                                   ↓
┌──────────────────┐              ┌──────────────────┐
│ PRIMARY_ACTIVE   │              │ FALLBACK_ACTIVE  │
│                  │              │                  │
│ - Forward Alpaca │              │ - Forward Yahoo  │
│ - Ignore Yahoo   │              │ - Retry Alpaca   │
│ - Health check   │              │ - Show warning   │
└──────────────────┘              └──────────────────┘
        │                                   │
        │ Alpaca fail                       │ Alpaca recover
        │ (3 failures                       │ (+ 10s grace)
        │  OR 30s silence)                  │
        │                                   │
        └──────────────────┬────────────────┘
                           │
                           │ Both fail
                           ↓
                  ┌──────────────────┐
                  │ BOTH_UNAVAILABLE │
                  │                  │
                  │ - Show error     │
                  │ - Cache last     │
                  │ - Retry both     │
                  └──────────────────┘
                           │
                           │ Any recover
                           │
                           └──→ (return to active state)
```

---

## 6. Integration Points

### 6.1 Existing Services to Modify

| Service | Location | Modification Type | Complexity |
|---------|----------|-------------------|------------|
| **MultiAssetDataBroadcastService** | `/backend/MyTrader.Api/Services/` | Minor - Add source tracking | Low |
| **YahooFinancePollingService** | `/backend/MyTrader.Services/Market/` | Minor - Continue as-is, add metadata | Low |
| **MarketDataHub** | `/backend/MyTrader.Api/Hubs/` | None - No changes needed | N/A |
| **DashboardHub** | `/backend/MyTrader.Api/Hubs/` | None - No changes needed | N/A |
| **Program.cs** | `/backend/MyTrader.Api/` | Minor - Register new services | Low |

### 6.2 New Services to Create

| Service | Purpose | Interfaces | Dependencies |
|---------|---------|------------|--------------|
| **AlpacaStreamingService** | WebSocket connection to Alpaca | `IAlpacaStreamingService`, `IHostedService` | Alpaca WebSocket API, ISymbolManagementService, ILogger |
| **DataSourceRouter** | Route events from primary/fallback | `IDataSourceRouter`, `IHostedService` | AlpacaStreamingService, YahooFinancePollingService, ILogger |
| **AlpacaHealthMonitor** | Monitor Alpaca connection health | `IHealthCheck` | AlpacaStreamingService, ILogger |

### 6.3 Database Schema Changes

**No schema changes required.** Existing `market_data` table supports all necessary fields.

**Optional Enhancement (Future):**
Add `source` column to track data origin:
```sql
ALTER TABLE market_data ADD COLUMN source VARCHAR(20) DEFAULT 'YAHOO';
CREATE INDEX idx_market_data_source ON market_data(source, timestamp);
```

### 6.4 Configuration Changes

**File:** `/backend/MyTrader.Api/appsettings.json`

**Additions:**
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
      "ReconnectMaxDelayMs": 60000,
      "MessageTimeoutSeconds": 30
    },
    "Fallback": {
      "EnableYahooFallback": true,
      "FallbackActivationDelaySeconds": 10,
      "PrimaryRecoveryGracePeriodSeconds": 10,
      "MaxConsecutiveFailures": 3
    }
  }
}
```

### 6.5 Frontend Impact

**No frontend code changes required** (backward compatible).

**Optional UI Enhancement:**
- Add data source indicator badge: "LIVE" (green) vs "DELAYED" (yellow)
- Display source in hover tooltip: "Source: Alpaca (real-time)" vs "Source: Yahoo Finance (delayed)"
- Show connectivity status icon in header

**Example Enhancement (React):**
```jsx
<PriceBadge
  price={150.25}
  change={0.5}
  source={marketData.source} // "ALPACA" or "YAHOO_FALLBACK"
  isRealTime={marketData.source === "ALPACA"}
/>
```

---

## 7. Risk Analysis & Mitigation

### 7.1 Technical Risks

| Risk | Probability | Impact | Mitigation Strategy | Owner |
|------|-------------|--------|---------------------|-------|
| **Alpaca API rate limits exceeded** | Medium | High | Implement symbol prioritization; upgrade to paid tier if needed | Backend Engineer |
| **WebSocket connection instability** | Medium | Medium | Exponential backoff reconnection; automatic fallback to Yahoo | Backend Engineer |
| **Both Alpaca and Yahoo unavailable** | Low | High | Cache last 100 updates per symbol; display stale data with warning | Backend Engineer |
| **Price discrepancy between sources** | Medium | Low | Log warnings; implement sanity checks (±5% delta) | Backend Engineer |
| **Performance degradation (high message rate)** | Low | Medium | Message throttling; batch processing; load testing | Performance Engineer |
| **Authentication failure on reconnect** | Low | High | Retry with exponential backoff; alert on 5+ consecutive failures | Backend Engineer |
| **Symbol subscription limit (30 symbols)** | High | Medium | Prioritize user portfolios; implement symbol rotation; upgrade plan | Product Manager |

### 7.2 Operational Risks

| Risk | Probability | Impact | Mitigation Strategy | Owner |
|------|-------------|--------|---------------------|-------|
| **Alpaca service outage** | Low | Medium | Automatic fallback to Yahoo; monitoring alerts | SRE |
| **Increased API costs** | Medium | Low | Monitor usage; set budget alerts; negotiate enterprise plan | Product Manager |
| **Configuration errors (wrong API keys)** | Medium | High | Validation on startup; integration tests with test keys | Backend Engineer |
| **Deployment rollback needed** | Low | High | Feature flag to disable Alpaca; rollback plan documented | DevOps |
| **Monitoring gaps (missed alerts)** | Medium | Medium | Comprehensive health checks; test alert delivery | SRE |

### 7.3 Data Quality Risks

| Risk | Probability | Impact | Mitigation Strategy | Owner |
|------|-------------|--------|---------------------|-------|
| **Incorrect price change calculation** | Low | High | Unit tests for all calculations; validate against external sources | Backend Engineer |
| **Missing previous close data** | Medium | Medium | Fetch from database on startup; cache from previous day | Backend Engineer |
| **Stale timestamps** | Low | Medium | Validate timestamp freshness; alert if >5s old | Backend Engineer |
| **Symbol format mismatch** | Low | Medium | Normalize symbols on ingestion; maintain mapping table | Data Engineer |

### 7.4 Security Risks

| Risk | Probability | Impact | Mitigation Strategy | Owner |
|------|-------------|--------|---------------------|-------|
| **API key exposure in logs** | Medium | Critical | Sanitize logs; never log credentials; code review | AppSec Guardian |
| **Unauthorized API access** | Low | High | Use Azure Key Vault; rotate keys every 90 days | AppSec Guardian |
| **Man-in-the-middle attack** | Low | Critical | Enforce TLS 1.2+; certificate pinning (optional) | AppSec Guardian |

### 7.5 Compliance & Legal Risks

| Risk | Probability | Impact | Mitigation Strategy | Owner |
|------|-------------|--------|---------------------|-------|
| **Market data redistribution violations** | Low | Critical | Review Alpaca terms of service; legal approval required | Legal/Product Manager |
| **Data retention policy non-compliance** | Low | Medium | Document data retention; implement automated cleanup | Data Architecture Manager |

---

## 8. Testing Strategy

### 8.1 Unit Testing

**Scope:** Individual service components
**Tools:** xUnit, Moq, FluentAssertions
**Owner:** Backend Engineer

| Test Category | Test Cases | Priority |
|---------------|------------|----------|
| **AlpacaStreamingService** | - Parse trade message<br>- Parse quote message<br>- Parse bar message<br>- Calculate price change %<br>- Handle missing previous close<br>- Normalize symbols<br>- Handle invalid JSON<br>- Handle empty messages | High |
| **DataSourceRouter** | - Route Alpaca events in PRIMARY_ACTIVE<br>- Route Yahoo events in FALLBACK_ACTIVE<br>- Transition PRIMARY → FALLBACK<br>- Transition FALLBACK → PRIMARY<br>- Respect grace period<br>- Handle state machine edge cases | High |
| **AlpacaHealthMonitor** | - Detect connection failure<br>- Detect message timeout<br>- Calculate uptime percentage<br>- Return health status JSON | Medium |

**Code Coverage Target:** >80%

---

### 8.2 Integration Testing

**Scope:** Service interactions
**Tools:** xUnit, TestContainers, WireMock
**Owner:** Integration Test Specialist

| Test Scenario | Test Steps | Expected Result | Priority |
|---------------|------------|-----------------|----------|
| **End-to-End Alpaca Flow** | 1. Start AlpacaStreamingService<br>2. Mock Alpaca WebSocket responses<br>3. Verify events emitted<br>4. Verify SignalR broadcasts | Price updates reach frontend in <2s | High |
| **Fallback Activation** | 1. Start both services<br>2. Simulate Alpaca disconnection<br>3. Wait 10s<br>4. Verify router switches to Yahoo | Router in FALLBACK_ACTIVE, Yahoo events forwarded | High |
| **Primary Recovery** | 1. Start in FALLBACK_ACTIVE<br>2. Restore Alpaca connection<br>3. Wait 10s grace period<br>4. Verify router switches to Alpaca | Router in PRIMARY_ACTIVE, Alpaca events forwarded | High |
| **Both Sources Fail** | 1. Disconnect Alpaca<br>2. Fail Yahoo API<br>3. Verify error state | Router in BOTH_UNAVAILABLE, cached data served | Medium |
| **Symbol Hot Reload** | 1. Add new symbol to database<br>2. Trigger reload<br>3. Verify subscription updated | New symbol receives updates | Medium |
| **Price Discrepancy Detection** | 1. Send Alpaca price $150<br>2. Send Yahoo price $130<br>3. Verify warning logged | Warning: >5% discrepancy | Low |

**Test Data:**
- Use Alpaca sandbox environment for safe testing
- Mock Yahoo API responses with predictable data
- Test with 5 symbols (below free tier limit)

---

### 8.3 Manual Testing

**Scope:** User-facing functionality
**Tools:** Browser, Postman, Mobile simulators
**Owner:** QA Manual Tester

| Test Case | Steps | Expected Result | Status |
|-----------|-------|-----------------|--------|
| **Real-Time Price Display** | 1. Open dashboard<br>2. Observe AAPL price<br>3. Compare with external source | Price updates <2s, matches market | Pending |
| **Data Source Badge** | 1. Open dashboard<br>2. Check price badge | Green "LIVE" badge visible | Pending |
| **Fallback Warning** | 1. Stop Alpaca service<br>2. Wait 10s<br>3. Check notification | Yellow "DELAYED" badge, user notified | Pending |
| **Recovery Notification** | 1. Restart Alpaca service<br>2. Wait 20s<br>3. Check notification | Green "LIVE" badge, user notified | Pending |
| **Mobile Compatibility** | 1. Open mobile app<br>2. Navigate to portfolio<br>3. Verify prices update | Real-time updates on mobile | Pending |
| **Cross-Browser Testing** | 1. Test on Chrome, Firefox, Safari<br>2. Verify WebSocket connection | Works on all major browsers | Pending |

**Test Environments:**
- **Development**: localhost with mock data
- **Staging**: Alpaca sandbox + test database
- **Pre-Production**: Alpaca live (paper trading) + production-like database

---

### 8.4 Performance Testing

**Scope:** Load, stress, scalability
**Tools:** k6, JMeter, Application Insights
**Owner:** Performance Engineer

| Test Scenario | Configuration | Success Criteria | Priority |
|---------------|---------------|------------------|----------|
| **Baseline Load** | 30 symbols, 1 update/sec, 1 hour | <2s latency (P95), <5% errors | High |
| **Burst Load** | 30 symbols, 10 updates/sec burst (5 min) | <3s latency (P95), <10% errors | High |
| **Concurrent Users** | 50 users, 30 symbols, 1 hour | <2s latency (P95), all users served | High |
| **Memory Leak Test** | 30 symbols, 24 hours | <500 MB memory, no leaks | Medium |
| **Database Write Performance** | Yahoo polling 30 symbols, 5 min interval | <500ms write latency (P95) | Medium |
| **Failover Performance** | Simulate Alpaca failure every 10 min | <5s fallback time, <10% dropped updates | Medium |

**Performance Benchmarks:**
- Latency: P50 <1s, P95 <2s, P99 <5s
- Throughput: >500 msg/sec
- Error rate: <0.1%
- CPU: <15% average
- Memory: <300 MB

---

### 8.5 Security Testing

**Scope:** Authentication, authorization, data protection
**Tools:** OWASP ZAP, Burp Suite, manual review
**Owner:** AppSec Guardian

| Test Case | Method | Expected Result | Priority |
|-----------|--------|-----------------|----------|
| **API Key Exposure** | Search logs, network traces, error messages | No API keys exposed | Critical |
| **TLS Configuration** | Inspect WebSocket connection | TLS 1.2+ enforced | High |
| **Credential Storage** | Review appsettings.json, code | Keys encrypted or in vault | High |
| **Unauthorized Access** | Attempt connection without valid keys | Connection refused | High |
| **Log Injection** | Send malicious symbol names | Logs sanitized, no injection | Medium |

---

### 8.6 Acceptance Testing

**Scope:** End-to-end user scenarios
**Owner:** Product Manager + QA Team

**User Story 1: Real-Time Price Viewing**
```gherkin
Feature: Real-time stock price updates via Alpaca

  Scenario: User views real-time stock prices on dashboard
    Given the user is logged in
    And Alpaca streaming is connected
    When the user navigates to the dashboard
    And AAPL stock is in the user's portfolio
    Then the user sees AAPL price updates within 2 seconds
    And the price badge displays "LIVE" in green
    And the price changes reflect market movements accurately

  Scenario: User experiences fallback to delayed data
    Given the user is viewing the dashboard
    And Alpaca connection is lost
    When 10 seconds elapse
    Then the user sees a notification "Real-time data unavailable, using delayed data"
    And the price badge displays "DELAYED" in yellow
    And prices continue updating every 60 seconds via Yahoo Finance
```

**User Story 2: Portfolio Value Calculation**
```gherkin
Feature: Portfolio value calculation with real-time prices

  Scenario: Portfolio value updates in real-time
    Given the user has a portfolio with 10 AAPL shares
    And AAPL price is $150.00
    When AAPL price increases to $151.00 (via Alpaca stream)
    Then the portfolio value increases by $10.00 within 2 seconds
    And the change is displayed with a green indicator
```

---

## 9. Deployment Strategy

### 9.1 Phased Rollout

**Phase 1: Development (Week 1-2)**
- Implement AlpacaStreamingService with mock data
- Implement DataSourceRouter with state machine
- Unit tests for all components
- Integration tests with TestContainers

**Phase 2: Staging (Week 3)**
- Deploy to staging environment
- Connect to Alpaca sandbox
- Manual testing by QA team
- Performance testing with 10 symbols

**Phase 3: Beta Release (Week 4)**
- Deploy to production with **feature flag disabled** by default
- Enable for 5% of users (beta testers)
- Monitor metrics: latency, error rate, fallback count
- Gather user feedback

**Phase 4: Gradual Rollout (Week 5-6)**
- Enable for 25% of users (monitor 24 hours)
- Enable for 50% of users (monitor 24 hours)
- Enable for 100% of users
- Monitor performance and stability

**Phase 5: Optimization (Week 7-8)**
- Optimize based on production metrics
- Upgrade to Alpaca paid tier if needed
- Scale infrastructure if necessary

### 9.2 Feature Flag Configuration

**File:** `appsettings.json`
```json
{
  "FeatureFlags": {
    "EnableAlpacaStreaming": false  // Toggle without redeploy
  }
}
```

**Conditional Service Registration (Program.cs):**
```csharp
if (configuration.GetValue<bool>("FeatureFlags:EnableAlpacaStreaming"))
{
    services.AddSingleton<IAlpacaStreamingService, AlpacaStreamingService>();
    services.AddSingleton<IDataSourceRouter, DataSourceRouter>();
}
```

### 9.3 Rollback Plan

**Trigger Conditions:**
- Alpaca connection failure rate >10%
- Fallback activation >20% of time
- User-reported price inaccuracies
- System performance degradation (P95 latency >5s)

**Rollback Steps:**
1. Set `FeatureFlags:EnableAlpacaStreaming = false` in production config
2. Restart services (or use hot reload if supported)
3. Verify Yahoo Finance polling is active
4. Monitor for 30 minutes to ensure stability
5. Notify stakeholders via Slack/Email

**Rollback Time:** <5 minutes (no code deployment needed)

### 9.4 Monitoring & Alerts

**Alert Rules (PagerDuty/Azure Monitor):**

| Alert | Condition | Severity | Action |
|-------|-----------|----------|--------|
| Alpaca Disconnected | Connection down >60s | Warning | Notify on-call engineer |
| Both Sources Down | PRIMARY and FALLBACK unavailable | Critical | Page on-call engineer |
| High Fallback Time | In FALLBACK_ACTIVE >10 min | Warning | Investigate Alpaca issues |
| Price Latency High | P95 latency >5s | Warning | Scale infrastructure |
| Error Rate High | Error rate >5% | Critical | Disable feature flag |

**Dashboard Metrics (Grafana/Application Insights):**
- Alpaca connection uptime %
- Message rate (msg/sec)
- Latency (P50, P95, P99)
- Fallback activation count
- Error rate by type
- Active symbols count

---

## 10. Documentation Requirements

### 10.1 Technical Documentation

| Document | Audience | Content | Owner |
|----------|----------|---------|-------|
| **API Integration Guide** | Backend Engineers | Alpaca WebSocket API usage, authentication, message formats | Backend Engineer |
| **Service Architecture Doc** | All Engineers | Component diagram, data flow, state machine | Solution Architect |
| **Configuration Guide** | DevOps | appsettings.json structure, environment variables, feature flags | Backend Engineer |
| **Runbook** | SRE/On-Call | Troubleshooting steps, rollback procedure, alert response | SRE |
| **Database Schema** | Data Engineers | market_data table structure, indexes, sample queries | Data Architecture Manager |

### 10.2 User-Facing Documentation

| Document | Audience | Content | Owner |
|----------|----------|---------|-------|
| **Release Notes** | End Users | New feature announcement, benefits, UI changes | Product Manager |
| **FAQ** | End Users | Common questions (e.g., "What is real-time data?", "Why delayed badge?") | Product Manager |
| **Help Article** | End Users | How to interpret data source badges, troubleshooting | Technical Writer |

### 10.3 Code Documentation

**Required Code Comments:**
- XML documentation for all public interfaces/classes
- Algorithm explanations (e.g., price change calculation)
- State transition logic (DataSourceRouter)
- Configuration parameter descriptions

**Example:**
```csharp
/// <summary>
/// Streams real-time stock market data from Alpaca WebSocket API.
/// Handles authentication, subscription management, and reconnection logic.
/// </summary>
/// <remarks>
/// This service maintains a persistent WebSocket connection to Alpaca.
/// On disconnection, it attempts exponential backoff reconnection.
/// Emits StockPriceUpdated events consumed by DataSourceRouter.
/// </remarks>
public class AlpacaStreamingService : IAlpacaStreamingService, IHostedService
{
    // Implementation
}
```

---

## 11. Dependencies & Prerequisites

### 11.1 External Dependencies

| Dependency | Version | Purpose | License | Cost |
|------------|---------|---------|---------|------|
| Alpaca Market Data API | v2 | Real-time stock streaming | Proprietary | Free tier: $0/month<br>Unlimited: $99/month |
| WebSocketSharp (or similar) | Latest | WebSocket client library | MIT | Free |
| Newtonsoft.Json | 13.x | JSON parsing | MIT | Free |
| System.Text.Json | .NET 9 | JSON parsing (alternative) | MIT | Free |

### 11.2 Infrastructure Prerequisites

| Resource | Requirement | Justification |
|----------|-------------|---------------|
| **Server CPU** | 2 cores minimum | WebSocket connection + message processing |
| **Server Memory** | 4 GB minimum | Message buffering + .NET runtime |
| **Network Bandwidth** | 10 Mbps | 30 symbols × 1 msg/sec × 1 KB = ~30 KB/sec |
| **Persistent Storage** | 50 GB | Database growth (5 min interval writes) |
| **TLS Certificate** | Valid cert for wss:// | Alpaca requires TLS 1.2+ |

### 11.3 Development Prerequisites

| Prerequisite | Version | Purpose |
|--------------|---------|---------|
| .NET SDK | 9.0 | Backend development |
| PostgreSQL | 14+ | Database |
| Docker | 20+ | Local testing with containers |
| Alpaca Account | Free tier | API keys for development |

---

## 12. Success Criteria & Acceptance

### 12.1 Definition of Done

**Technical Acceptance:**
- [ ] AlpacaStreamingService implemented and passing unit tests (>80% coverage)
- [ ] DataSourceRouter implemented with all state transitions tested
- [ ] Integration tests passing (Alpaca flow, fallback, recovery)
- [ ] Performance tests passing (latency <2s P95)
- [ ] Security review completed (no API key exposure)
- [ ] Code review approved by 2+ engineers
- [ ] Documentation complete (technical docs, runbook, API guide)
- [ ] Deployed to staging and tested by QA
- [ ] Feature flag configured and tested

**Business Acceptance:**
- [ ] Product Manager validates real-time price updates
- [ ] UX team approves data source badge design
- [ ] Legal approves Alpaca terms of service
- [ ] Stakeholders approve phased rollout plan

### 12.2 Go-Live Checklist

**Pre-Deployment:**
- [ ] Alpaca production API keys obtained and stored in Key Vault
- [ ] Configuration reviewed (appsettings.json, environment variables)
- [ ] Database indexes optimized (market_data table)
- [ ] Monitoring dashboards configured (Grafana/Application Insights)
- [ ] Alert rules configured (PagerDuty/Azure Monitor)
- [ ] Rollback plan documented and tested
- [ ] On-call rotation staffed

**Deployment:**
- [ ] Deploy to production with feature flag disabled
- [ ] Verify health endpoints return 200 OK
- [ ] Enable feature flag for 5% of users
- [ ] Monitor for 1 hour (latency, error rate, fallback count)
- [ ] If stable, increase to 25%, then 50%, then 100%

**Post-Deployment:**
- [ ] Verify real-time price updates visible in frontend
- [ ] Check logs for errors (should be <1%)
- [ ] Confirm fallback mechanism works (simulate Alpaca disconnection)
- [ ] Review performance metrics (latency within SLA)
- [ ] Gather user feedback (support tickets, surveys)

### 12.3 Success Metrics (90-Day Review)

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Average price update latency | <2s (P95) | TBD | Pending |
| Alpaca connection uptime | >98% | TBD | Pending |
| Fallback activation frequency | <5 times/day | TBD | Pending |
| User satisfaction (NPS) | >8/10 | TBD | Pending |
| System availability | >99.5% | TBD | Pending |
| API cost per month | <$100 | TBD | Pending |

---

## 13. Open Questions & Assumptions

### 13.1 Open Questions

| Question | Impact | Owner | Target Resolution Date |
|----------|--------|-------|------------------------|
| Should we display bid/ask prices in the UI? | Medium | UX Team | Week 2 |
| Do we need historical streaming data replay for late-joining users? | Low | Product Manager | Week 3 |
| Should we support extended hours trading data? | Medium | Product Manager | Week 4 |
| What is the acceptable cost threshold for Alpaca paid tier? | High | Finance/Product Manager | Week 1 |
| Should we cache previous close prices in Redis for faster startup? | Low | Backend Engineer | Week 5 |

### 13.2 Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Alpaca free tier (30 symbols) sufficient for MVP | High - Need paid tier immediately | Budget allocated for paid tier upgrade |
| Yahoo Finance will remain available for fallback | Medium - Need alternative fallback | Evaluate Alpha Vantage as backup |
| Existing SignalR infrastructure can handle increased message rate | Medium - Performance degradation | Load testing before production rollout |
| Users accept yellow "DELAYED" badge without confusion | Low - User confusion | User education via help article |
| .NET 9 WebSocket client is stable enough for production | Low - Connection issues | Use proven library like WebSocketSharp |

---

## 14. Appendices

### Appendix A: Glossary

| Term | Definition |
|------|------------|
| **Alpaca** | Financial API provider offering commission-free trading and market data |
| **IEX** | Investors Exchange, a stock exchange providing real-time data via Alpaca |
| **SIP** | Securities Information Processor, consolidated data from all US exchanges |
| **Trade** | A completed stock transaction (buyer + seller match) |
| **Quote** | Current bid (buy) and ask (sell) prices for a stock |
| **Bar** | Aggregated OHLCV data for a time period (e.g., 1-minute bar) |
| **OHLCV** | Open, High, Low, Close, Volume - standard candlestick data |
| **P95 Latency** | 95th percentile latency - 95% of requests complete faster than this value |
| **Circuit Breaker** | Design pattern that prevents cascading failures by stopping requests to unhealthy services |
| **Exponential Backoff** | Retry strategy with increasing wait times (1s, 2s, 4s, 8s, ...) |

### Appendix B: Alpaca API Reference Links

- **Official Documentation**: https://docs.alpaca.markets/docs/streaming-market-data
- **WebSocket Streams**: https://docs.alpaca.markets/docs/websocket-streaming
- **Authentication Guide**: https://docs.alpaca.markets/docs/authentication
- **Message Schemas**: https://docs.alpaca.markets/docs/real-time-stock-pricing-data
- **Rate Limits**: https://docs.alpaca.markets/docs/limits
- **Status Page**: https://status.alpaca.markets/

### Appendix C: Sample Alpaca Messages

**Authentication Success:**
```json
[
  {
    "T": "success",
    "msg": "authenticated"
  }
]
```

**Subscription Confirmation:**
```json
[
  {
    "T": "subscription",
    "trades": ["AAPL", "GOOGL"],
    "quotes": ["AAPL", "GOOGL"],
    "bars": []
  }
]
```

**Trade Message:**
```json
[
  {
    "T": "t",
    "S": "AAPL",
    "i": 52983525029461,
    "x": "V",
    "p": 150.25,
    "s": 100,
    "t": "2025-10-09T14:30:00.123456Z",
    "c": ["@", "F", "T"],
    "z": "C"
  }
]
```

**Error Message:**
```json
[
  {
    "T": "error",
    "code": 406,
    "msg": "connection limit exceeded"
  }
]
```

### Appendix D: Configuration Example

**Full appsettings.json Alpaca Section:**
```json
{
  "Alpaca": {
    "Streaming": {
      "Enabled": true,
      "WebSocketUrl": "wss://stream.data.alpaca.markets/v2/iex",
      "ApiKey": "${ALPACA_API_KEY}",
      "ApiSecret": "${ALPACA_API_SECRET}",
      "ReconnectMaxAttempts": -1,
      "ReconnectBaseDelayMs": 1000,
      "ReconnectMaxDelayMs": 60000,
      "AuthTimeoutSeconds": 10,
      "MessageTimeoutSeconds": 30,
      "HealthCheckIntervalSeconds": 60,
      "MaxSymbols": 30,
      "SubscribeToTrades": true,
      "SubscribeToQuotes": true,
      "SubscribeToBars": false,
      "EnableDetailedLogging": false
    },
    "Fallback": {
      "EnableYahooFallback": true,
      "FallbackActivationDelaySeconds": 10,
      "PrimaryRecoveryGracePeriodSeconds": 10,
      "MaxConsecutiveFailures": 3,
      "NotifyUsersOnFallback": true,
      "NotifyUsersOnRecovery": true
    }
  },
  "FeatureFlags": {
    "EnableAlpacaStreaming": false
  }
}
```

### Appendix E: Contact List

| Role | Name | Email | Slack |
|------|------|-------|-------|
| Product Manager | TBD | pm@mytrader.com | @pm |
| Backend Engineer | TBD | backend@mytrader.com | @backend-team |
| Frontend Engineer | TBD | frontend@mytrader.com | @frontend-team |
| QA Lead | TBD | qa@mytrader.com | @qa-team |
| SRE Lead | TBD | sre@mytrader.com | @sre-team |
| Security Engineer | TBD | security@mytrader.com | @appsec |

---

## Document Approval

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Business Analyst | [Your Name] | _____________ | 2025-10-09 |
| Product Manager | _____________ | _____________ | ________ |
| Technical Lead | _____________ | _____________ | ________ |
| SRE Lead | _____________ | _____________ | ________ |
| Security Lead | _____________ | _____________ | ________ |

---

**Document History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-09 | Business Analyst | Initial requirements specification |

---

**End of Document**
