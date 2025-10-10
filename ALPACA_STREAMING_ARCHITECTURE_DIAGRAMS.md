# Alpaca Streaming Integration - Architecture Diagrams

**Document Version:** 1.0
**Date:** October 9, 2025
**Related Documents:**
- ALPACA_STREAMING_REQUIREMENTS_SPECIFICATION.md
- ALPACA_STREAMING_EXECUTIVE_SUMMARY.md

---

## 1. High-Level System Architecture

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                                EXTERNAL SYSTEMS                                 │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│   ┌──────────────────┐              ┌──────────────────┐                      │
│   │  Alpaca WebSocket│              │ Yahoo Finance API│                      │
│   │  wss://stream... │              │   query1.finance │                      │
│   │                  │              │   .yahoo.com     │                      │
│   │  Real-time feed  │              │   REST API       │                      │
│   │  <1s latency     │              │   60s polling    │                      │
│   └────────┬─────────┘              └────────┬─────────┘                      │
│            │                                  │                                 │
└────────────┼──────────────────────────────────┼─────────────────────────────────┘
             │                                  │
             │ WSS                              │ HTTPS
             │                                  │
┌────────────┼──────────────────────────────────┼─────────────────────────────────┐
│            │         MYTRADER BACKEND         │                                 │
│            ↓                                  ↓                                 │
│  ┌─────────────────────┐          ┌─────────────────────┐                     │
│  │ AlpacaStreaming     │          │ YahooFinance        │                     │
│  │ Service             │          │ PollingService      │                     │
│  │                     │          │                     │                     │
│  │ - Auth & Connect    │          │ - Poll every 60s    │                     │
│  │ - Subscribe symbols │          │ - Fire events       │                     │
│  │ - Parse messages    │          │ - Write to DB       │                     │
│  │ - Emit events       │          │   (every 5 min)     │                     │
│  │ - Reconnect logic   │          │                     │                     │
│  └──────────┬──────────┘          └──────────┬──────────┘                     │
│             │                                 │                                 │
│             │ Event: StockPriceUpdated        │ Event: StockPriceUpdated        │
│             │                                 │                                 │
│             └─────────────┬───────────────────┘                                 │
│                           ↓                                                     │
│                 ┌─────────────────────┐                                         │
│                 │ DataSourceRouter    │                                         │
│                 │                     │                                         │
│                 │ State Machine:      │                                         │
│                 │ - PRIMARY_ACTIVE    │                                         │
│                 │ - FALLBACK_ACTIVE   │                                         │
│                 │ - BOTH_UNAVAILABLE  │                                         │
│                 │                     │                                         │
│                 │ Logic:              │                                         │
│                 │ - Route primary     │                                         │
│                 │ - Detect failures   │                                         │
│                 │ - Switch fallback   │                                         │
│                 │ - Recover primary   │                                         │
│                 └──────────┬──────────┘                                         │
│                            │                                                    │
│                            │ Event: StockPriceUpdated (with Source metadata)   │
│                            ↓                                                    │
│                 ┌─────────────────────┐                                         │
│                 │ MultiAssetData      │                                         │
│                 │ BroadcastService    │                                         │
│                 │                     │                                         │
│                 │ - Throttle updates  │                                         │
│                 │ - Add metadata      │                                         │
│                 │ - Format messages   │                                         │
│                 │ - Broadcast groups  │                                         │
│                 └──────────┬──────────┘                                         │
│                            │                                                    │
│                            │ SignalR SendAsync                                  │
│                            ↓                                                    │
│           ┌────────────────────────────────────┐                               │
│           │   SignalR Hubs                     │                               │
│           │   ┌──────────────┐ ┌─────────────┐│                               │
│           │   │MarketDataHub │ │DashboardHub ││                               │
│           │   │              │ │             ││                               │
│           │   │Groups:       │ │Groups:      ││                               │
│           │   │STOCK_AAPL    │ │AssetClass_  ││                               │
│           │   │STOCK_GOOGL   │ │STOCK        ││                               │
│           │   └──────────────┘ └─────────────┘│                               │
│           └────────────────┬───────────────────┘                               │
│                            │                                                    │
│                            │ WebSocket (SignalR protocol)                      │
│                            ↓                                                    │
│                   ┌─────────────────┐                                          │
│                   │ PostgreSQL DB   │                                          │
│                   │                 │                                          │
│                   │ market_data     │                                          │
│                   │ table           │                                          │
│                   │                 │                                          │
│                   │ (5-min writes)  │                                          │
│                   └─────────────────┘                                          │
└────────────────────────────────────────────────────────────────────────────────┘
             │
             │ WebSocket (SignalR)
             ↓
┌────────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND CLIENTS                                   │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│   ┌──────────────────┐              ┌──────────────────┐                      │
│   │  React Web App   │              │ React Native App │                      │
│   │                  │              │                  │                      │
│   │  - Dashboard     │              │  - Dashboard     │                      │
│   │  - Portfolio     │              │  - Portfolio     │                      │
│   │  - Strategies    │              │  - Strategies    │                      │
│   │                  │              │                  │                      │
│   │  Display:        │              │  Display:        │                      │
│   │  $150.25 +0.5%   │              │  $150.25 +0.5%   │                      │
│   │  Badge: LIVE     │              │  Badge: LIVE     │                      │
│   └──────────────────┘              └──────────────────┘                      │
│                                                                                 │
└────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Data Flow - Normal Operation (Alpaca Primary)

```
TIME: T+0s (Market Event)
┌─────────────────────────────────────────┐
│ NASDAQ Exchange: AAPL traded @ $150.25  │
└──────────────────┬──────────────────────┘
                   │
                   │ Market feed
                   ↓
TIME: T+0.1s
┌─────────────────────────────────────────┐
│ Alpaca WebSocket Server                 │
│ - Aggregates exchange feeds             │
│ - Broadcasts to subscribers             │
└──────────────────┬──────────────────────┘
                   │
                   │ WSS message: {"T":"t","S":"AAPL","p":150.25,...}
                   ↓
TIME: T+0.3s
┌─────────────────────────────────────────┐
│ AlpacaStreamingService                  │
│ 1. Receive WebSocket message            │
│ 2. Parse JSON                           │
│ 3. Extract: Symbol=AAPL, Price=150.25   │
│ 4. Calculate:                           │
│    - PriceChange = 150.25 - 150.00 = 0.25
│    - PriceChangePercent = 0.17%        │
│ 5. Create StockPriceData object         │
│ 6. Emit event: StockPriceUpdated        │
└──────────────────┬──────────────────────┘
                   │
                   │ Event: StockPriceData { Symbol=AAPL, Price=150.25, Source=ALPACA }
                   ↓
TIME: T+0.4s
┌─────────────────────────────────────────┐
│ DataSourceRouter                        │
│ 1. Receive event from Alpaca            │
│ 2. Check current state: PRIMARY_ACTIVE  │
│ 3. Decision: Forward event              │
│ 4. Log: "Routing AAPL from ALPACA"      │
│ 5. Forward to MultiAssetDataBroadcast   │
└──────────────────┬──────────────────────┘
                   │
                   │ Event: StockPriceData (unchanged)
                   ↓
TIME: T+0.5s
┌─────────────────────────────────────────┐
│ MultiAssetDataBroadcastService          │
│ 1. Receive event                        │
│ 2. Check throttle (last update >50ms?)  │
│ 3. Convert to MultiAssetPriceUpdate:    │
│    {                                    │
│      Type: "PriceUpdate",               │
│      AssetClass: "STOCK",               │
│      Symbol: "AAPL",                    │
│      Price: 150.25,                     │
│      Change24h: 0.17,                   │
│      Source: "ALPACA",                  │
│      Timestamp: "2025-10-09T14:30:00Z"  │
│    }                                    │
│ 4. Broadcast to SignalR groups:         │
│    - Group: "STOCK_AAPL"                │
│    - Group: "AssetClass_STOCK"          │
└──────────────────┬──────────────────────┘
                   │
                   │ SignalR: SendAsync("PriceUpdate", data)
                   ↓
TIME: T+0.7s
┌─────────────────────────────────────────┐
│ MarketDataHub / DashboardHub            │
│ 1. Receive broadcast request            │
│ 2. Find clients in group "STOCK_AAPL"   │
│ 3. Send to 25 connected clients         │
│ 4. Find clients in group "AssetClass_   │
│    STOCK"                               │
│ 5. Send to 50 connected clients         │
└──────────────────┬──────────────────────┘
                   │
                   │ WebSocket frames (SignalR protocol)
                   ↓
TIME: T+1.0s
┌─────────────────────────────────────────┐
│ Frontend Clients (React/React Native)   │
│ 1. Receive SignalR message              │
│ 2. Parse PriceUpdate                    │
│ 3. Update state: setPrice(150.25)       │
│ 4. Re-render component:                 │
│    <PriceDisplay                        │
│      price={150.25}                     │
│      change={+0.17}                     │
│      badge="LIVE"                       │
│      color="green"                      │
│    />                                   │
│ 5. User sees updated price              │
└─────────────────────────────────────────┘

TOTAL LATENCY: ~1.0 second (market event → user screen)
```

---

## 3. Data Flow - Fallback Scenario (Alpaca Failed)

```
TIME: T+0s (Alpaca Disconnects)
┌─────────────────────────────────────────┐
│ Alpaca WebSocket Server                 │
│ ✗ Connection lost (network issue)       │
└──────────────────┬──────────────────────┘
                   │
                   │ No messages received
                   ↓
TIME: T+30s (Timeout detected)
┌─────────────────────────────────────────┐
│ AlpacaStreamingService                  │
│ 1. Detect: No messages for 30 seconds   │
│ 2. Health check fails                   │
│ 3. Status: DISCONNECTED                 │
│ 4. Attempt reconnection (backoff)       │
│ 5. Log: "Alpaca connection lost"        │
└──────────────────┬──────────────────────┘
                   │
                   │ No events emitted
                   ↓
TIME: T+35s (Fallback triggered)
┌─────────────────────────────────────────┐
│ DataSourceRouter                        │
│ 1. Detect: 3 consecutive Alpaca failures│
│ 2. Decision: ACTIVATE FALLBACK          │
│ 3. State transition:                    │
│    PRIMARY_ACTIVE → FALLBACK_ACTIVE     │
│ 4. Log: "Switching to Yahoo fallback"   │
│ 5. Send notification to frontend        │
│ 6. Start forwarding Yahoo events        │
└──────────────────┬──────────────────────┘
                   │
                   │ Notification: { type: "DataSourceChange", source: "YAHOO_FALLBACK" }
                   ↓
TIME: T+36s
┌─────────────────────────────────────────┐
│ Frontend Clients                        │
│ 1. Receive notification                 │
│ 2. Display yellow badge: "DELAYED"      │
│ 3. Show toast: "Real-time data          │
│    temporarily unavailable, using       │
│    delayed data"                        │
└─────────────────────────────────────────┘

TIME: T+60s (Yahoo polling cycle)
┌─────────────────────────────────────────┐
│ Yahoo Finance API                       │
│ GET /v8/finance/chart/AAPL              │
│ Response: { regularMarketPrice: 150.30 }│
└──────────────────┬──────────────────────┘
                   │
                   │ HTTP response
                   ↓
┌─────────────────────────────────────────┐
│ YahooFinancePollingService              │
│ 1. Parse response                       │
│ 2. Calculate price change               │
│ 3. Create StockPriceData object         │
│    Source: "YAHOO_FALLBACK"             │
│ 4. Emit event: StockPriceUpdated        │
└──────────────────┬──────────────────────┘
                   │
                   │ Event: StockPriceData { Symbol=AAPL, Price=150.30, Source=YAHOO_FALLBACK }
                   ↓
┌─────────────────────────────────────────┐
│ DataSourceRouter                        │
│ 1. Receive event from Yahoo             │
│ 2. Check state: FALLBACK_ACTIVE         │
│ 3. Decision: Forward event              │
│ 4. Log: "Routing AAPL from YAHOO"       │
└──────────────────┬──────────────────────┘
                   │
                   │ → MultiAssetDataBroadcast → SignalR → Frontend
                   ↓
┌─────────────────────────────────────────┐
│ Frontend Clients                        │
│ Display:                                │
│ $150.30 +0.37%                          │
│ Badge: DELAYED (yellow)                 │
│ Last updated: 1 minute ago              │
└─────────────────────────────────────────┘

FALLBACK LATENCY: ~60 seconds (polling interval)
```

---

## 4. Data Flow - Recovery Scenario (Alpaca Restored)

```
TIME: T+0s (Alpaca reconnects)
┌─────────────────────────────────────────┐
│ AlpacaStreamingService                  │
│ 1. Reconnection attempt succeeds        │
│ 2. Send auth message                    │
│ 3. Receive auth success                 │
│ 4. Subscribe to symbols                 │
│ 5. Start receiving messages             │
│ 6. Status: CONNECTED                    │
└──────────────────┬──────────────────────┘
                   │
                   │ Event: StockPriceUpdated (ALPACA source)
                   ↓
TIME: T+5s (Messages flowing)
┌─────────────────────────────────────────┐
│ DataSourceRouter                        │
│ 1. Receive Alpaca events                │
│ 2. Check state: FALLBACK_ACTIVE         │
│ 3. Decision: WAIT (grace period)        │
│ 4. Validate message rate: OK            │
│ 5. Validate message quality: OK         │
│ 6. Timer: Grace period 10 seconds       │
└──────────────────┬──────────────────────┘
                   │
                   │ (Still forwarding Yahoo events during grace period)
                   ↓
TIME: T+15s (Grace period elapsed)
┌─────────────────────────────────────────┐
│ DataSourceRouter                        │
│ 1. Grace period complete                │
│ 2. Alpaca validated: Stable             │
│ 3. Decision: RECOVER PRIMARY            │
│ 4. State transition:                    │
│    FALLBACK_ACTIVE → PRIMARY_ACTIVE     │
│ 5. Log: "Recovering to Alpaca primary"  │
│ 6. Send notification to frontend        │
│ 7. Stop forwarding Yahoo events         │
│ 8. Resume forwarding Alpaca events      │
└──────────────────┬──────────────────────┘
                   │
                   │ Notification: { type: "DataSourceChange", source: "ALPACA" }
                   ↓
┌─────────────────────────────────────────┐
│ Frontend Clients                        │
│ 1. Receive notification                 │
│ 2. Display green badge: "LIVE"          │
│ 3. Show toast: "Real-time data restored"│
│ 4. Resume <1s price updates             │
└─────────────────────────────────────────┘

RECOVERY TIME: ~15 seconds (including 10s grace period)
```

---

## 5. State Machine Diagram (DataSourceRouter)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         STATE MACHINE: DataSourceRouter                     │
└─────────────────────────────────────────────────────────────────────────────┘

                                ┌──────────┐
                                │ STARTUP  │
                                └────┬─────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                                 │
          Alpaca connects                     Alpaca fails to connect
          & authenticated                     (after 30s timeout)
                    │                                 │
                    ↓                                 ↓
         ┌──────────────────────┐          ┌──────────────────────┐
         │  PRIMARY_ACTIVE      │          │  FALLBACK_ACTIVE     │
         │                      │          │                      │
         │  Actions:            │          │  Actions:            │
         │  - Forward Alpaca    │          │  - Forward Yahoo     │
         │  - Ignore Yahoo      │          │  - Show warning      │
         │  - Health check      │          │  - Retry Alpaca      │
         │  - Monitor latency   │          │  - Monitor recovery  │
         │                      │          │                      │
         │  Display:            │          │  Display:            │
         │  Badge: LIVE (green) │          │  Badge: DELAYED (⚠️) │
         │  Latency: <1s        │          │  Latency: 60s        │
         └──────────┬───────────┘          └──────────┬───────────┘
                    │                                  │
                    │                                  │
        ┌───────────┼──────────┐           ┌──────────┼───────────┐
        │                      │           │                       │
  Alpaca failure:        Yahoo still  Alpaca recovers         Alpaca still
  - Connection lost      working          & validated         disconnected
  - 30s silence                           + grace period
  - 3 consecutive                         (10s)
    health check fails                                │
        │                      │           │                       │
        │                      │           │                       │
        └──────────┬───────────┘           └───────────┬──────────┘
                   │                                   │
                   │                                   │
                   └───────────┐       ┌───────────────┘
                               │       │
                               ↓       ↑
                               │       │
                               │  Recovery
                               │  with grace
                               │
                Both sources    │
                fail            │
                               ↓
                    ┌──────────────────────┐
                    │ BOTH_UNAVAILABLE     │
                    │                      │
                    │ Actions:             │
                    │ - Show error         │
                    │ - Cache last values  │
                    │ - Display stale data │
                    │ - Retry both sources │
                    │ - Alert operations   │
                    │                      │
                    │ Display:             │
                    │ Badge: ERROR (🔴)    │
                    │ Message: "Data       │
                    │  temporarily         │
                    │  unavailable"        │
                    └──────────┬───────────┘
                               │
                               │ Any source recovers
                               │
                    ┌──────────┴───────────┐
                    │                      │
              Alpaca recovers        Yahoo recovers
                    │                      │
                    ↓                      ↓
         ┌──────────────────────┐  ┌──────────────────────┐
         │  PRIMARY_ACTIVE      │  │  FALLBACK_ACTIVE     │
         └──────────────────────┘  └──────────────────────┘


STATE TRANSITION CONDITIONS:

STARTUP → PRIMARY_ACTIVE:
  - Alpaca connects successfully
  - Authentication succeeds within 10s
  - Subscription confirmed

STARTUP → FALLBACK_ACTIVE:
  - Alpaca fails to connect after 30s timeout
  - OR authentication fails

PRIMARY_ACTIVE → FALLBACK_ACTIVE:
  - Alpaca connection lost for >10 seconds
  - OR no messages received for >30 seconds
  - OR 3 consecutive health check failures
  - AND Yahoo is available

FALLBACK_ACTIVE → PRIMARY_ACTIVE:
  - Alpaca connection restored
  - AND authentication succeeds
  - AND subscription confirmed
  - AND at least 1 message received successfully
  - AND 10-second grace period elapsed

PRIMARY_ACTIVE → BOTH_UNAVAILABLE:
  - Alpaca fails AND Yahoo fails

FALLBACK_ACTIVE → BOTH_UNAVAILABLE:
  - Yahoo fails (Alpaca already failed)

BOTH_UNAVAILABLE → PRIMARY_ACTIVE:
  - Alpaca recovers (preferred path)

BOTH_UNAVAILABLE → FALLBACK_ACTIVE:
  - Yahoo recovers (Alpaca still down)
```

---

## 6. Sequence Diagram - Alpaca Subscription Flow

```
Client               AlpacaStreaming    Alpaca        Database    DataSource
(App Start)          Service            WebSocket     (PostgreSQL) Router
   │                      │                  │              │          │
   │──Start Service──────>│                  │              │          │
   │                      │                  │              │          │
   │                      │──Load Symbols────────────>      │          │
   │                      │  (SELECT ticker FROM symbols    │          │
   │                      │   WHERE asset_class='STOCK'     │          │
   │                      │   AND is_active=true LIMIT 30)  │          │
   │                      │                  │              │          │
   │                      │<─────Return Symbols────────     │          │
   │                      │  [AAPL, GOOGL, MSFT, ...]       │          │
   │                      │                  │              │          │
   │                      │──Connect WSS────>│              │          │
   │                      │                  │              │          │
   │                      │<───Connected─────│              │          │
   │                      │                  │              │          │
   │                      │──Auth Message────>              │          │
   │                      │  {action: "auth",│              │          │
   │                      │   key: "***",    │              │          │
   │                      │   secret: "***"} │              │          │
   │                      │                  │              │          │
   │                      │<───Auth Success──│              │          │
   │                      │  {T:"success",   │              │          │
   │                      │   msg:"auth'd"}  │              │          │
   │                      │                  │              │          │
   │                      │──Subscribe───────>              │          │
   │                      │  {action: "sub", │              │          │
   │                      │   trades: [...], │              │          │
   │                      │   quotes: [...]} │              │          │
   │                      │                  │              │          │
   │                      │<───Sub Confirmed─│              │          │
   │                      │  {T:"subscription│              │          │
   │                      │   trades:[...]}  │              │          │
   │                      │                  │              │          │
   │                      │──Register Router────────────────────────>  │
   │                      │  (Subscribe to   │              │          │
   │                      │   StockPriceUpdated event)      │          │
   │                      │                  │              │          │
   │<─Service Ready───────│                  │              │          │
   │                      │                  │              │          │
   │                      │                  │              │          │
   ├─────────────── NORMAL OPERATION ─────────────────────────────────┤
   │                      │                  │              │          │
   │                      │<───Trade Msg─────│              │          │
   │                      │  {T:"t",S:"AAPL",│              │          │
   │                      │   p:150.25,...}  │              │          │
   │                      │                  │              │          │
   │                      │──Parse & Emit────────────────────────────>│
   │                      │  StockPriceUpdated(Symbol=AAPL,Price=...)│
   │                      │                  │              │          │
   │                      │                  │              │<─Route───│
   │                      │                  │              │  (Forward │
   │                      │                  │              │   event)  │
   │                      │                  │              │          │
   │<─────────────── Price Update Broadcast ──────────────────────────│
   │  (via SignalR)       │                  │              │          │
   │                      │                  │              │          │
```

---

## 7. Component Interaction Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       COMPONENT INTERACTIONS                             │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┐
│ AlpacaStreaming      │───────────────────┐
│ Service              │                   │
│                      │                   │ Subscribe to
│ Responsibilities:    │                   │ events
│ - WebSocket mgmt     │                   │
│ - Auth & reconnect   │                   ↓
│ - Parse messages     │         ┌──────────────────────┐
│ - Emit events        │         │ DataSourceRouter     │
└──────────┬───────────┘         │                      │
           │                     │ Responsibilities:    │
           │ Event:              │ - Route events       │
           │ StockPriceUpdated   │ - State machine      │
           │ (Source=ALPACA)     │ - Health monitoring  │
           │                     │ - Fallback logic     │
           └────────────────────>│                      │
                                 └──────────┬───────────┘
                                            │
                                            │ Event:
                                            │ StockPriceUpdated
                                            │ (with routing
┌──────────────────────┐                   │  decision)
│ YahooFinance         │                   │
│ PollingService       │                   │
│                      │                   │
│ Responsibilities:    │                   │
│ - Poll REST API      │                   │
│ - Write to DB        │                   ↓
│ - Emit events        │         ┌──────────────────────┐
└──────────┬───────────┘         │ MultiAssetData       │
           │                     │ BroadcastService     │
           │ Event:              │                      │
           │ StockPriceUpdated   │ Responsibilities:    │
           │ (Source=YAHOO)      │ - Throttle updates   │
           │                     │ - Format messages    │
           └────────────────────>│ - SignalR broadcast  │
                                 └──────────┬───────────┘
                                            │
                                            │ SignalR
                                            │ SendAsync()
                                            │
                                            ↓
                         ┌─────────────────────────────────┐
                         │ SignalR Hubs                    │
                         │ (MarketDataHub, DashboardHub)   │
                         │                                 │
                         │ Responsibilities:               │
                         │ - Manage connections            │
                         │ - Manage groups                 │
                         │ - Send to clients               │
                         └─────────────────────────────────┘
                                            │
                                            │ WebSocket
                                            │ frames
                                            ↓
                         ┌─────────────────────────────────┐
                         │ Frontend Clients                │
                         │ (React Web, React Native)       │
                         └─────────────────────────────────┘


DEPENDENCY DIAGRAM:

AlpacaStreamingService
  ↓ depends on
  - Alpaca WebSocket API (external)
  - ISymbolManagementService (database access)
  - ILogger

DataSourceRouter
  ↓ depends on
  - AlpacaStreamingService (events)
  - YahooFinancePollingService (events)
  - ILogger

MultiAssetDataBroadcastService
  ↓ depends on
  - DataSourceRouter (events)
  - IHubContext<DashboardHub>
  - IHubContext<MarketDataHub>
  - ILogger

YahooFinancePollingService
  ↓ depends on
  - Yahoo Finance API (external)
  - ITradingDbContext (database)
  - ILogger
```

---

## 8. Database Interaction Diagram

```
┌────────────────────────────────────────────────────────────────────────┐
│                     DATABASE OPERATIONS                                 │
└────────────────────────────────────────────────────────────────────────┘

READ OPERATIONS (Symbol Loading):

┌──────────────────────┐
│ AlpacaStreaming      │
│ Service              │
└──────────┬───────────┘
           │
           │ On Startup & Every 5 minutes:
           │
           ↓
┌──────────────────────────────────────────────────────────────────┐
│ SELECT ticker, venue                                             │
│ FROM symbols                                                     │
│ WHERE asset_class = 'STOCK'                                      │
│   AND is_active = true                                           │
│   AND is_tracked = true                                          │
│ ORDER BY                                                         │
│   CASE WHEN ticker IN (SELECT symbol FROM portfolio_positions    │
│                        WHERE user_id = @current_user)            │
│        THEN 1                                                    │
│        WHEN is_popular = true THEN 2                             │
│        ELSE 3                                                    │
│   END,                                                           │
│   volume_24h DESC                                                │
│ LIMIT 30;  -- Alpaca free tier limit                            │
└──────────────────────────────────────────────────────────────────┘
           │
           │ Returns: [AAPL, GOOGL, MSFT, ...]
           │
           ↓
┌──────────────────────┐
│ Subscribe to symbols │
│ via Alpaca WebSocket │
└──────────────────────┘


WRITE OPERATIONS (Persistence):

┌──────────────────────┐
│ YahooFinance         │
│ PollingService       │
└──────────┬───────────┘
           │
           │ Every 5 minutes:
           │
           ↓
┌──────────────────────────────────────────────────────────────────┐
│ INSERT INTO market_data (                                        │
│   id,                                                            │
│   symbol,                                                        │
│   timeframe,                                                     │
│   timestamp,                                                     │
│   open,                                                          │
│   high,                                                          │
│   low,                                                           │
│   close,                                                         │
│   volume,                                                        │
│   asset_class                                                    │
│ ) VALUES (                                                       │
│   gen_random_uuid(),                                             │
│   'AAPL',                                                        │
│   '5MIN',                                                        │
│   NOW(),                                                         │
│   150.10,  -- open = current price                              │
│   150.25,  -- high = current price                              │
│   150.05,  -- low = current price                               │
│   150.20,  -- close = current price                             │
│   1000000, -- volume from API                                    │
│   'STOCK'                                                        │
│ )                                                                │
│ ON CONFLICT (symbol, timeframe, timestamp) DO UPDATE             │
│ SET close = EXCLUDED.close,                                      │
│     high = GREATEST(market_data.high, EXCLUDED.high),            │
│     low = LEAST(market_data.low, EXCLUDED.low),                  │
│     volume = EXCLUDED.volume;                                    │
└──────────────────────────────────────────────────────────────────┘


INDEX USAGE:

market_data table has index:
  idx_market_data_symbol_timeframe_timestamp (symbol, timeframe, timestamp)

This supports efficient queries for:
  - Historical data retrieval (backtesting)
  - Chart data generation
  - Performance analysis
```

---

## 9. Error Handling Flow

```
┌────────────────────────────────────────────────────────────────────────┐
│                        ERROR SCENARIOS & HANDLING                       │
└────────────────────────────────────────────────────────────────────────┘

SCENARIO 1: Alpaca Authentication Failure
┌──────────────────────┐
│ AlpacaStreaming      │
│ Service              │
└──────────┬───────────┘
           │
           │ 1. Connect to WebSocket
           ↓
      ┌─────────┐
      │ Alpaca  │
      └────┬────┘
           │
           │ 2. Send auth {key, secret}
           ↓
      ┌─────────┐
      │ Alpaca  │
      └────┬────┘
           │
           │ 3. Response: {T:"error", code:401, msg:"unauthorized"}
           ↓
┌──────────────────────┐
│ AlpacaStreaming      │
│ Service              │
└──────────┬───────────┘
           │
           │ 4. Catch error
           │ 5. Log: "Alpaca auth failed: unauthorized"
           │ 6. Retry with backoff (1s, 2s, 4s, 8s, ...)
           │ 7. After 5 failures: Alert operations
           │ 8. Status: DISCONNECTED
           ↓
┌──────────────────────┐
│ DataSourceRouter     │
└──────────┬───────────┘
           │
           │ 9. Detect: Alpaca unavailable
           │ 10. Transition: STARTUP → FALLBACK_ACTIVE
           │ 11. Use Yahoo as primary source
           ↓
     (Continue with Yahoo)


SCENARIO 2: Message Timeout (30s no data)
┌──────────────────────┐
│ AlpacaStreaming      │
│ Service              │
└──────────┬───────────┘
           │
           │ Last message: T+0s
           │ Current time: T+30s
           │
           │ 1. Timer expires: MessageTimeoutSeconds = 30
           │ 2. Health check fails
           │ 3. Log: "Alpaca message timeout (30s)"
           │ 4. Status: STALE
           ↓
┌──────────────────────┐
│ DataSourceRouter     │
└──────────┬───────────┘
           │
           │ 5. Detect: Stale data
           │ 6. Decision: Switch to fallback
           │ 7. Transition: PRIMARY_ACTIVE → FALLBACK_ACTIVE
           ↓
     (Yahoo takes over)


SCENARIO 3: Invalid Message (Parse Error)
┌──────────────────────┐
│ Alpaca WebSocket     │
└──────────┬───────────┘
           │
           │ Message: {T:"t",S:"AAPL",p:"invalid",...}
           ↓
┌──────────────────────┐
│ AlpacaStreaming      │
│ Service              │
└──────────┬───────────┘
           │
           │ 1. Try: Parse JSON
           │ 2. Try: Extract price field
           │ 3. Catch: FormatException (price not numeric)
           │ 4. Log warning: "Failed to parse AAPL price: invalid"
           │ 5. Skip this message
           │ 6. Continue processing next message
           │ 7. Do NOT trigger fallback (transient error)
           ↓
     (Continue normal operation)


SCENARIO 4: Both Sources Fail
┌──────────────────────┐  ┌──────────────────────┐
│ AlpacaStreaming      │  │ YahooFinance         │
│ Service              │  │ PollingService       │
└──────────┬───────────┘  └──────────┬───────────┘
           │                         │
           │ Disconnected            │ HTTP 503 Service Unavailable
           ↓                         ↓
┌──────────────────────────────────────────────────┐
│ DataSourceRouter                                 │
└──────────┬───────────────────────────────────────┘
           │
           │ 1. Detect: Both sources unavailable
           │ 2. Transition: → BOTH_UNAVAILABLE
           │ 3. Log critical: "All market data sources failed"
           │ 4. Alert operations (PagerDuty)
           │ 5. Actions:
           │    - Serve cached data (last 100 updates per symbol)
           │    - Display error badge: "Data unavailable"
           │    - Show staleness: "Last updated: 5 minutes ago"
           │    - Continue retry attempts for both sources
           ↓
┌──────────────────────┐
│ Frontend Clients     │
└──────────┬───────────┘
           │
           │ Display:
           │ - Red badge: "ERROR"
           │ - Message: "Market data temporarily unavailable"
           │ - Show cached prices with timestamp
           │ - Disable trading actions
           ↓
     (Wait for source recovery)
```

---

## 10. Monitoring Dashboard Layout

```
┌────────────────────────────────────────────────────────────────────────┐
│                   ALPACA STREAMING DASHBOARD                            │
│                   (Grafana / Application Insights)                      │
└────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────┬─────────────────────────────────┐
│ CONNECTION STATUS                   │ PERFORMANCE METRICS              │
│                                     │                                  │
│ ┌─────────────────────────────────┐ │ ┌─────────────────────────────┐ │
│ │ Current State: PRIMARY_ACTIVE   │ │ │ P50 Latency: 0.8s           │ │
│ │ ● Connected                     │ │ │ P95 Latency: 1.5s           │ │
│ │                                 │ │ │ P99 Latency: 2.3s           │ │
│ │ Alpaca: ✅ Healthy              │ │ │                             │ │
│ │ Yahoo:  ✅ Healthy              │ │ │ ┌───────────────────────┐   │ │
│ │                                 │ │ │ │  Latency Chart       │   │ │
│ │ Last Message: 2s ago            │ │ │ │  (last 1 hour)       │   │ │
│ │ Messages/min: 120               │ │ │ │  ─────────────       │   │ │
│ │ Uptime: 99.8%                   │ │ │ │   ────────────       │   │ │
│ └─────────────────────────────────┘ │ │ └───────────────────────┘   │ │
│                                     │ └─────────────────────────────┘ │
└─────────────────────────────────────┴─────────────────────────────────┘

┌─────────────────────────────────────┬─────────────────────────────────┐
│ MESSAGE THROUGHPUT                  │ ERROR METRICS                    │
│                                     │                                  │
│ ┌─────────────────────────────────┐ │ ┌─────────────────────────────┐ │
│ │ ┌───────────────────────────┐   │ │ │ Total Errors: 5             │ │
│ │ │ Messages/sec Chart        │   │ │ │ Error Rate: 0.01%           │ │
│ │ │ (last 15 minutes)         │   │ │ │                             │ │
│ │ │                          │   │ │ │ By Type:                    │ │
│ │ │   ▂▃▅▆▇█▇▆▅▃▂           │   │ │ │ - Parse errors: 2           │ │
│ │ │                          │   │ │ │ - Timeout errors: 1         │ │
│ │ │ Current: 2.3 msg/sec     │   │ │ │ - Auth errors: 0            │ │
│ │ │ Peak: 5.1 msg/sec        │   │ │ │ - Connection errors: 2      │ │
│ │ │ Average: 2.0 msg/sec     │   │ │ │                             │ │
│ │ └───────────────────────────┘   │ │ └─────────────────────────────┘ │
│ └─────────────────────────────────┘ │                                  │
└─────────────────────────────────────┴─────────────────────────────────┘

┌─────────────────────────────────────┬─────────────────────────────────┐
│ FALLBACK HISTORY                    │ SYMBOL DISTRIBUTION              │
│                                     │                                  │
│ Last 24 hours:                      │ ┌─────────────────────────────┐ │
│ - Fallback activations: 2           │ │ Active Symbols: 25/30       │ │
│ - Total fallback time: 15 min       │ │                             │ │
│ - Average recovery: 12s             │ │ Top by message count:       │ │
│                                     │ │ 1. AAPL:  150 msg/min       │ │
│ Timeline:                           │ │ 2. GOOGL: 145 msg/min       │ │
│ ┌─────────────────────────────────┐ │ │ 3. MSFT:  140 msg/min       │ │
│ │ 08:00 ──────────────────        │ │ │ 4. TSLA:  138 msg/min       │ │
│ │ 10:30 ───⚠️─────────────        │ │ │ 5. AMZN:  135 msg/min       │ │
│ │       (Fallback 8 min)          │ │ │                             │ │
│ │ 14:15 ──────⚠️──────────        │ │ └─────────────────────────────┘ │
│ │       (Fallback 7 min)          │ │                                  │
│ └─────────────────────────────────┘ │                                  │
└─────────────────────────────────────┴─────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ ALERTS (Last 24 hours)                                                   │
│                                                                          │
│ ⚠️ 14:15 - Alpaca connection lost, switched to fallback (Resolved: 14:22)│
│ ⚠️ 10:30 - High latency detected (P95: 3.2s) (Resolved: 10:38)          │
│ ℹ️  08:00 - Alpaca connection restored from overnight maintenance       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

**End of Architecture Diagrams Document**

These diagrams provide a comprehensive visual representation of the Alpaca streaming integration architecture. Use them in design reviews, implementation planning, and team onboarding.
