# Market Status Indicator Requirements Specification
**Document Version:** 1.0
**Date:** 2025-10-09
**Status:** Draft for Review
**Author:** Business Analyst
**Project:** myTrader - Market Status Visibility Enhancement

---

## Executive Summary

This document defines comprehensive requirements for implementing market status indicators across the myTrader platform. The goal is to eliminate user confusion about when markets are open/closed and whether displayed prices are real-time or stale.

### Problem Statement

**Current State:**
- Stock prices display without market status context (BIST, NASDAQ, NYSE)
- No last update timestamp shown on price displays
- Users cannot determine if data is real-time or stale
- Different exchanges have different trading hours, causing confusion
- No visual indicators for market open/closed state

**Impact:**
- User confusion about data freshness
- Potential misunderstanding of trading opportunities
- Reduced trust in data accuracy
- Support requests about "why prices not updating"

**Success Criteria:**
- Users can see market status at a glance (within 2 seconds)
- Clear indication of last price update time
- No confusion about stale data (measured by support ticket reduction >50%)
- Consistent UX across mobile and web platforms

---

## 1. Market Hours Definition

### 1.1 BIST (Borsa Istanbul)

**Exchange Code:** `BIST`
**Timezone:** Europe/Istanbul (UTC+3 year-round, no DST)
**Country Code:** TR
**Primary Currency:** TRY

**Trading Sessions:**

| Session Type | Start Time (Local) | End Time (Local) | Status |
|--------------|-------------------|------------------|---------|
| Pre-Market   | 09:40             | 10:00            | PRE_MARKET |
| Regular      | 10:00             | 18:00            | OPEN |
| Post-Market  | N/A               | N/A              | N/A |

**Trading Days:**
- Monday through Friday
- Closed on weekends (Saturday, Sunday)

**2025 Holiday Calendar:**
```
2025-01-01 : New Year's Day
2025-04-23 : National Sovereignty and Children's Day
2025-05-01 : Labor Day
2025-05-19 : Youth and Sports Day
2025-07-15 : Democracy and National Unity Day
2025-08-30 : Victory Day
2025-10-29 : Republic Day
[Religious holidays to be added based on lunar calendar]
```

**Data Provider:** Yahoo Finance (BIST symbols end with .IS)
**Update Frequency:**
- During OPEN: 15-minute delayed (free tier)
- During CLOSED: No updates

**Implementation Notes:**
- Turkey does not observe daylight saving time (constant UTC+3)
- Pre-market session has limited liquidity
- No after-hours trading available

---

### 1.2 NASDAQ (US Market)

**Exchange Code:** `NASDAQ`
**Timezone:** America/New_York (UTC-5 EST / UTC-4 EDT)
**Country Code:** US
**Primary Currency:** USD

**Trading Sessions:**

| Session Type | Start Time (ET) | End Time (ET) | Status |
|--------------|-----------------|---------------|---------|
| Pre-Market   | 04:00           | 09:30         | PRE_MARKET |
| Regular      | 09:30           | 16:00         | OPEN |
| After-Hours  | 16:00           | 20:00         | AFTER_HOURS |

**Trading Days:**
- Monday through Friday
- Closed on weekends and holidays

**2025-2026 Holiday Calendar:**
```
2025-01-01 : New Year's Day
2025-01-20 : Martin Luther King Jr. Day
2025-02-17 : Presidents' Day
2025-04-18 : Good Friday
2025-05-26 : Memorial Day
2025-06-19 : Juneteenth National Independence Day
2025-07-04 : Independence Day
2025-09-01 : Labor Day
2025-11-27 : Thanksgiving Day
2025-11-28 : Day after Thanksgiving (Early Close 13:00 ET)
2025-12-24 : Christmas Eve (Early Close 13:00 ET)
2025-12-25 : Christmas Day

[2026 holidays included in system for forward-looking calculations]
```

**Data Provider:** Alpaca Markets API (primary), Yahoo Finance (fallback)
**Update Frequency:**
- During OPEN: Real-time (Alpaca IEX feed) or 15-min delayed (Yahoo)
- During PRE_MARKET/AFTER_HOURS: Limited data availability
- During CLOSED: No updates

**Implementation Notes:**
- Daylight Saving Time transitions: 2nd Sunday in March (spring forward), 1st Sunday in November (fall back)
- Pre-market and after-hours have reduced liquidity
- Early close days (1pm ET) on day before Thanksgiving and Christmas Eve

---

### 1.3 NYSE (New York Stock Exchange)

**Exchange Code:** `NYSE`
**Timezone:** America/New_York (UTC-5 EST / UTC-4 EDT)
**Country Code:** US
**Primary Currency:** USD

**Trading Sessions:**

| Session Type | Start Time (ET) | End Time (ET) | Status |
|--------------|-----------------|---------------|---------|
| Pre-Market   | 04:00           | 09:30         | PRE_MARKET |
| Regular      | 09:30           | 16:00         | OPEN |
| After-Hours  | 16:00           | 20:00         | AFTER_HOURS |

**Trading Days & Holidays:**
- Same as NASDAQ (synchronized US market holidays)

**Data Provider:** Alpaca Markets API (primary), Yahoo Finance (fallback)
**Update Frequency:** Same as NASDAQ

**Implementation Notes:**
- NYSE and NASDAQ share the same holiday calendar
- Early close days apply to NYSE as well

---

### 1.4 CRYPTO (24/7 Markets)

**Exchange Code:** `CRYPTO` / `BINANCE`
**Timezone:** UTC (no timezone conversion needed)
**Country Code:** GLOBAL
**Primary Currency:** USDT, BTC, ETH

**Trading Sessions:**

| Session Type | Start Time | End Time | Status |
|--------------|-----------|----------|---------|
| Continuous   | 00:00     | 23:59    | OPEN |

**Trading Days:**
- 24 hours a day, 7 days a week, 365 days a year
- Never closes

**Holidays:**
- None (cryptocurrency markets don't observe holidays)

**Data Provider:** Binance WebSocket API
**Update Frequency:**
- Real-time streaming (WebSocket ticker updates)
- Update interval: Every 1-2 seconds per symbol

**Implementation Notes:**
- Always shows as OPEN
- No market status indicator needed (always green)
- Data staleness check: Alert if no update received for >30 seconds

---

## 2. Market Status States

### 2.1 Status Enum Definition

```typescript
enum MarketStatus {
  OPEN = "OPEN",                 // Active regular trading session
  CLOSED = "CLOSED",             // Market closed (outside all sessions)
  PRE_MARKET = "PRE_MARKET",     // Pre-market trading (US markets only)
  AFTER_HOURS = "AFTER_HOURS",   // After-hours trading (US markets only)
  HOLIDAY = "HOLIDAY",           // Market closed for holiday
  HALTED = "HALTED",             // Trading temporarily suspended
  UNKNOWN = "UNKNOWN"            // Status cannot be determined
}
```

### 2.2 Status Definitions

| Status | Description | When Applied | Data Updates |
|--------|-------------|--------------|--------------|
| **OPEN** | Market is in regular trading session | During standard trading hours on trading days | Real-time or 15-min delayed |
| **CLOSED** | Market is closed | Outside trading hours, weekends | No updates expected |
| **PRE_MARKET** | Pre-market trading session | Before market open (US markets) | Limited updates |
| **AFTER_HOURS** | Extended hours trading | After market close (US markets) | Limited updates |
| **HOLIDAY** | Market closed for holiday | Recognized holidays | No updates |
| **HALTED** | Trading temporarily suspended | Circuit breaker triggered | No updates until resumed |
| **UNKNOWN** | Status cannot be determined | System error or missing data | Indeterminate |

### 2.3 Status Priority Rules

When multiple conditions apply, status is determined in this priority order:

1. **HALTED** (if trading halt flag received from provider)
2. **HOLIDAY** (if current date is in holiday calendar)
3. **WEEKEND** (treated as CLOSED with reason "Weekend")
4. **PRE_MARKET** (if within pre-market hours)
5. **OPEN** (if within regular hours)
6. **AFTER_HOURS** (if within after-hours window)
7. **CLOSED** (default when outside all trading windows)
8. **UNKNOWN** (fallback if calculation fails)

---

## 3. UI/UX Requirements

### 3.1 Visual Design Standards

#### Color Coding

| Status | Color (Hex) | Indicator | Usage Context |
|--------|------------|-----------|---------------|
| OPEN | `#10b981` (Green 500) | Solid dot | Market actively trading |
| PRE_MARKET | `#f59e0b` (Amber 500) | Pulsing dot | Pre-market session |
| AFTER_HOURS | `#f59e0b` (Amber 500) | Pulsing dot | After-hours session |
| CLOSED | `#ef4444` (Red 500) | Solid dot | Market closed |
| HOLIDAY | `#ef4444` (Red 500) | Dot + text | Holiday closure |
| UNKNOWN | `#6b7280` (Gray 500) | Hollow dot | Status unavailable |

**Accessibility Requirements:**
- Color alone must not convey information (use icon + text + color)
- Minimum contrast ratio: 4.5:1 (WCAG AA standard)
- Status text must be screen reader friendly

#### Typography & Spacing

**Mobile:**
- Status indicator dot: 8px diameter
- Status text: 11px, font-weight 600
- Timestamp text: 10px, font-weight 400
- Padding: 4px vertical, 8px horizontal

**Web:**
- Status indicator dot: 10px diameter
- Status text: 12px, font-weight 600
- Timestamp text: 11px, font-weight 400
- Padding: 6px vertical, 12px horizontal

---

### 3.2 Mobile UI Specifications

#### 3.2.1 Dashboard Accordion Header

**Location:** Next to asset class name (e.g., "Hisse Senetleri")

**Compact View:**
```
[Dot] Piyasa: Açık
```

**Components:**
- Status dot (8px, colored)
- Text: "Piyasa: {status}"
- Position: Right side of accordion header
- Tap behavior: Show detailed market timing info in tooltip

**Status Text Translations (Turkish):**
```
OPEN → "Açık"
CLOSED → "Kapalı"
PRE_MARKET → "Açılış Öncesi"
AFTER_HOURS → "Kapanış Sonrası"
HOLIDAY → "Tatil"
```

**Example Layout:**
```
┌─────────────────────────────────────────┐
│ 🏢 Hisse Senetleri (5)    [●] Piyasa: Açık│
│                                         │
│   AAPL  $150.25  +2.5%                 │
│   ...                                   │
└─────────────────────────────────────────┘
```

#### 3.2.2 Individual Stock/Symbol Card

**Location:** Below price, above change percentage

**Layout:**
```
AAPL
$150.25
Son Güncelleme: 15:45
+2.5% • +$3.75
```

**Timestamp Format:**
- If < 1 minute ago: "Az önce"
- If < 60 minutes ago: "X dakika önce"
- If same day: "HH:MM"
- If previous day: "Dün HH:MM"
- If older: "DD MMM HH:MM"

**Market Closed Warning:**
```
AAPL
$150.25
Piyasa Kapalı - Son: 16:00
+2.5% • +$3.75
```

#### 3.2.3 Market Status Tooltip (Tap-to-Show)

**Trigger:** Tap on market status indicator
**Display:** Modal bottom sheet

**Content:**
```
┌─────────────────────────────────────────┐
│ NYSE - Piyasa Durumu                    │
│                                         │
│ Durum: [●] Kapalı                       │
│ Açılış: Yarın 09:30 EST                │
│ Kapanış: --                             │
│ Zaman Dilimi: America/New_York         │
│ Yerel Saat: 20:15                       │
│                                         │
│ Kapanış Nedeni: Günlük İşlem Saatleri  │
│                                         │
│ [Tamam]                                 │
└─────────────────────────────────────────┘
```

---

### 3.3 Web UI Specifications

#### 3.3.1 Dashboard Market Overview

**Location:** Top of dashboard, horizontal bar

**Layout:**
```
┌────────────────────────────────────────────────────────────────┐
│ Piyasa Durumları                                               │
│                                                                │
│  [●] BIST         [●] NASDAQ      [●] NYSE       [●] CRYPTO   │
│  Açık             Kapalı          Kapalı         Açık (24/7)   │
│  Kapanış: 18:00   Açılış: 09:30   Açılış: 09:30  --           │
└────────────────────────────────────────────────────────────────┘
```

**Interaction:**
- Hover: Show tooltip with detailed timing
- Click: No action (informational only)

#### 3.3.2 Asset Class Accordion Header

**Layout:**
```
▼ 🏢 Hisse Senetleri (5 symbol)           [●] NASDAQ: Kapalı
```

**Status Badge:**
- Position: Right side, aligned with accordion title
- Format: `[Dot] {Market}: {Status}`
- Hover: Show next open/close time tooltip

#### 3.3.3 Symbol Card (Grid/List View)

**Grid View:**
```
┌──────────────────────┐
│ AAPL                 │
│ $150.25              │
│ +2.5% (+$3.75)       │
│ 15:45 • NASDAQ       │
│ [●] Piyasa Kapalı    │
└──────────────────────┘
```

**List View:**
```
AAPL  Apple Inc.  $150.25  +2.5%  15:45  [●] Kapalı
```

#### 3.3.4 Market Status Tooltip (Hover)

**Trigger:** Hover over market status badge
**Display:** Floating tooltip

**Content:**
```
┌─────────────────────────────────────────┐
│ NASDAQ Market Status                    │
│                                         │
│ Status: CLOSED                          │
│ Next Open: Tomorrow 09:30 EST          │
│ Trading Hours: 09:30 - 16:00 EST      │
│ Current Time: 20:15 EST                │
│                                         │
│ Closure Reason: After trading hours    │
└─────────────────────────────────────────┘
```

---

### 3.4 Data Staleness Indicators

#### 3.4.1 Staleness Thresholds

| Market Status | Warning Threshold | Error Threshold | Action |
|---------------|------------------|-----------------|--------|
| OPEN (Crypto) | 30 seconds | 60 seconds | Show warning icon |
| OPEN (Stock) | 20 minutes | 30 minutes | Show warning icon |
| PRE_MARKET | 5 minutes | 10 minutes | Show warning icon |
| AFTER_HOURS | 5 minutes | 10 minutes | Show warning icon |
| CLOSED | N/A | N/A | No warning (expected) |

#### 3.4.2 Warning UI

**Visual Indicator:**
- Yellow warning triangle icon (⚠)
- Text: "Veri güncel olmayabilir"
- Placement: Next to last update timestamp

**Tooltip on Hover/Tap:**
```
Data Connection Issue
Last update received: 25 minutes ago
Expected update frequency: 15 minutes

The price shown may be outdated.
```

---

## 4. API Contract Updates

### 4.1 Enhanced MarketStatusDto

**Endpoint:** `GET /api/markets/status`
**Response:**

```typescript
interface MarketStatusDto {
  marketId: string;              // UUID
  marketCode: string;            // "BIST" | "NASDAQ" | "NYSE" | "BINANCE"
  marketName: string;            // "Borsa Istanbul"
  marketNameTurkish?: string;    // "Borsa İstanbul"

  // Current Status
  status: MarketStatus;          // Enum: OPEN, CLOSED, PRE_MARKET, etc.
  isOpen: boolean;               // Quick boolean check

  // Timing Information
  currentTime: string;           // ISO 8601 in market timezone
  lastUpdateTime: string;        // ISO 8601 UTC when status last calculated
  nextOpenTime?: string;         // ISO 8601 UTC (null if always open)
  nextCloseTime?: string;        // ISO 8601 UTC (null if always open)

  // Timezone Info
  timezone: string;              // IANA timezone ID (e.g., "America/New_York")
  timezoneOffset: number;        // Current UTC offset in hours (e.g., -5)

  // Session Details
  currentSessionType?: string;   // "REGULAR" | "PRE_MARKET" | "AFTER_HOURS"
  tradingDay: string;            // ISO 8601 date (YYYY-MM-DD)

  // Holiday Information
  isHoliday: boolean;
  holidayName?: string;          // e.g., "Thanksgiving Day"

  // Closure Reason
  closureReason?: string;        // Human-readable reason if closed

  // Trading Hours (static reference)
  regularHours: {
    open: string;                // Local time "09:30"
    close: string;               // Local time "16:00"
  };
  preMarketHours?: {
    open: string;
    close: string;
  };
  afterHoursSession?: {
    open: string;
    close: string;
  };
}
```

**Example Response:**

```json
{
  "marketId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "marketCode": "NASDAQ",
  "marketName": "NASDAQ Stock Market",
  "marketNameTurkish": "NASDAQ Borsası",
  "status": "CLOSED",
  "isOpen": false,
  "currentTime": "2025-10-09T20:15:00-04:00",
  "lastUpdateTime": "2025-10-10T00:15:00Z",
  "nextOpenTime": "2025-10-10T13:30:00Z",
  "nextCloseTime": "2025-10-10T20:00:00Z",
  "timezone": "America/New_York",
  "timezoneOffset": -4,
  "currentSessionType": null,
  "tradingDay": "2025-10-09",
  "isHoliday": false,
  "closureReason": "After trading hours",
  "regularHours": {
    "open": "09:30",
    "close": "16:00"
  },
  "preMarketHours": {
    "open": "04:00",
    "close": "09:30"
  },
  "afterHoursSession": {
    "open": "16:00",
    "close": "20:00"
  }
}
```

---

### 4.2 Enhanced UnifiedMarketDataDto

**Endpoint:** `GET /api/market-data/unified`
**Updated Fields:**

```typescript
interface UnifiedMarketDataDto {
  // Existing fields...
  symbolId: string;
  symbol: string;
  price: number;
  change: number;
  changePercent: number;

  // NEW: Market Status Integration
  marketStatus: MarketStatus;           // Current market status
  isMarketOpen: boolean;                // Quick boolean
  lastUpdateTime: string;               // ISO 8601 UTC
  dataTimestamp: string;                // ISO 8601 UTC (from provider)
  receivedTimestamp: string;            // ISO 8601 UTC (when received)

  // NEW: Data Quality Indicators
  dataSource: string;                   // "ALPACA" | "YAHOO" | "BINANCE"
  isRealtime: boolean;                  // true/false
  dataDelayMinutes: number;             // 0 for realtime, 15 for delayed
  dataStalenessSeconds: number;         // Seconds since last update

  // NEW: Next Market Event
  nextMarketEvent?: {
    eventType: "OPEN" | "CLOSE";
    eventTime: string;                  // ISO 8601 UTC
    timeUntilEventSeconds: number;      // Countdown in seconds
  };

  // Existing fields continue...
  volume?: number;
  high?: number;
  low?: number;
  // ...
}
```

---

### 4.3 New Endpoints

#### 4.3.1 Get Market Status for All Markets

**Endpoint:** `GET /api/markets/status/all`
**Method:** GET
**Authentication:** Optional (public endpoint)

**Response:**
```typescript
interface MarketStatusCollectionDto {
  requestTimestamp: string;           // ISO 8601 UTC
  markets: MarketStatusDto[];         // Array of market statuses
  summary: {
    totalMarkets: number;
    openMarkets: number;
    closedMarkets: number;
  };
}
```

#### 4.3.2 Get Market Status for Specific Market

**Endpoint:** `GET /api/markets/status/{marketCode}`
**Method:** GET
**Parameters:**
- `marketCode` (path): "BIST" | "NASDAQ" | "NYSE" | "BINANCE"

**Response:** Single `MarketStatusDto`

#### 4.3.3 Get Market Timing for Symbol

**Endpoint:** `GET /api/symbols/{symbolId}/market-timing`
**Method:** GET
**Authentication:** Optional

**Response:**
```typescript
interface SymbolMarketTimingDto {
  symbolId: string;
  symbol: string;
  marketCode: string;
  marketStatus: MarketStatusDto;
  recommendedAction: string;  // "Market open - trade now" | "Market closed - opens in 8h 15m"
}
```

---

### 4.4 SignalR Hub Updates

#### 4.4.1 Market Status Change Events

**Hub:** `MarketDataHub`
**Event:** `OnMarketStatusChanged`

**Message Format:**
```typescript
{
  type: "market_status_update",
  data: {
    marketCode: string;
    previousStatus: MarketStatus;
    newStatus: MarketStatus;
    timestamp: string;            // ISO 8601 UTC
    affectedSymbols: string[];    // Array of symbol IDs
    message: string;              // "NASDAQ market has opened"
  }
}
```

**Client Subscription:**
```typescript
connection.on("OnMarketStatusChanged", (message) => {
  // Update UI with new market status
  updateMarketStatusIndicators(message.data);
});
```

#### 4.4.2 Data Staleness Warnings

**Event:** `OnDataStalenessWarning`

**Message Format:**
```typescript
{
  type: "data_staleness_warning",
  data: {
    symbolIds: string[];
    marketCode: string;
    lastUpdateTime: string;
    stalenessSeconds: number;
    severity: "WARNING" | "ERROR";
    message: "No price updates received for 25 minutes"
  }
}
```

---

## 5. Business Rules

### 5.1 Real-time Update Frequency

| Market Status | Expected Update Frequency | Actual Implementation |
|---------------|--------------------------|----------------------|
| OPEN (Crypto) | 1-2 seconds | Binance WebSocket streaming |
| OPEN (Stock - Realtime) | 1-5 seconds | Alpaca WebSocket streaming |
| OPEN (Stock - Delayed) | 15 minutes | Yahoo Finance polling |
| PRE_MARKET | 1-5 minutes | Limited provider support |
| AFTER_HOURS | 1-5 minutes | Limited provider support |
| CLOSED | No updates | Stop polling/streaming |

### 5.2 Data Staleness Rules

**Rule 1: Crypto Markets**
- If market status = OPEN AND no update for >30 seconds: Show warning
- If market status = OPEN AND no update for >60 seconds: Show error + "Connection Lost"

**Rule 2: Stock Markets (Regular Hours)**
- If market status = OPEN AND realtime=true AND no update for >30 seconds: Show warning
- If market status = OPEN AND realtime=false AND no update for >20 minutes: Show warning
- Never show staleness warning when market status = CLOSED

**Rule 3: Extended Hours**
- If market status = PRE_MARKET or AFTER_HOURS AND no update for >5 minutes: Show warning
- Message: "Limited trading data available during extended hours"

### 5.3 Market Status Update Rules

**Rule 1: Status Check Frequency**
- Backend recalculates market status every 1 minute (cron job)
- Frontend refreshes market status from API every 5 minutes
- Immediate update when receiving SignalR market status change event

**Rule 2: Status Transition Notifications**
- Notify users when market transitions: CLOSED → PRE_MARKET → OPEN → AFTER_HOURS → CLOSED
- Show browser/mobile notification: "NASDAQ market has opened"
- Only notify for markets the user is actively watching

**Rule 3: Holiday Calendar Updates**
- Holiday calendars must be updated annually by January 1st
- System should log warning if current date is within 30 days of last holiday in calendar
- Administrator notification when holiday calendar needs update

### 5.4 Timezone Handling Rules

**Rule 1: Display Times**
- Backend always stores and transmits times in UTC (ISO 8601 format)
- Frontend converts to user's local timezone for display
- Market status tooltips show both local time AND market time

**Rule 2: Daylight Saving Time**
- US markets (NASDAQ/NYSE): Handle DST transitions automatically
- BIST: No DST (always UTC+3)
- System must handle DST transition edge cases (e.g., 2am-3am on transition day)

**Rule 3: Cross-Timezone Display**
- If user in Turkey (UTC+3) viewing NASDAQ (EST UTC-5):
  - Show NASDAQ times in EST with "(22:30 EST / 05:30 Türkiye)" format
  - Use relative time when possible: "Opens in 8 hours 15 minutes"

---

## 6. User Stories

### 6.1 Stock Market User Stories

#### US-001: View BIST Market Status on Dashboard
**As a** Turkish stock trader
**I want to** see BIST market status on the dashboard
**So that** I know if I can trade BIST stocks now

**Acceptance Criteria:**
1. Given BIST market is OPEN (10:00-18:00 TRT weekday)
   - When I view dashboard
   - Then I see green dot with "Açık" text next to "Hisse Senetleri"

2. Given BIST market is CLOSED (after 18:00 or weekend)
   - When I view dashboard
   - Then I see red dot with "Kapalı" text
   - And I see next opening time: "Yarın 10:00"

3. Given BIST market is in PRE_MARKET (09:40-10:00)
   - When I view dashboard
   - Then I see yellow dot with "Açılış Öncesi" text
   - And I see countdown: "20 dakika içinde açılış"

**Test Data:**
- Test at 11:00 TRT on Monday (OPEN)
- Test at 19:00 TRT on Monday (CLOSED)
- Test at 09:45 TRT on Monday (PRE_MARKET)
- Test at 14:00 TRT on Saturday (CLOSED - Weekend)
- Test on Republic Day 2025-10-29 (CLOSED - Holiday)

---

#### US-002: View US Stock Market Status During Pre-Market
**As a** trader interested in US stocks
**I want to** see when US markets are in pre-market session
**So that** I can prepare for market open

**Acceptance Criteria:**
1. Given current time is 06:00 EST (pre-market session)
   - When I view NASDAQ stocks
   - Then I see yellow pulsing dot with "Pre-Market" status
   - And I see countdown to market open: "Opens in 3h 30m"

2. Given market transitions from PRE_MARKET to OPEN at 09:30
   - When status changes
   - Then I receive notification: "NASDAQ market has opened"
   - And status indicator changes to green "Open"

**Test Data:**
- Test at 05:00 EST (pre-market, 4.5 hours before open)
- Test at 09:29 EST (1 minute before open)
- Test status transition at exactly 09:30 EST

---

#### US-003: Understand Why Prices Not Updating During Market Close
**As a** user viewing stock prices at night
**I want to** see clear indication that market is closed
**So that** I understand why prices are not updating

**Acceptance Criteria:**
1. Given NASDAQ market is CLOSED at 20:00 EST
   - When I view AAPL stock
   - Then I see "Piyasa Kapalı - Son Güncelleme: 16:00" below price
   - And I see next open time: "Yarın 09:30 EST"

2. Given I tap on market status indicator
   - When tooltip opens
   - Then I see detailed explanation:
     - Market: NYSE
     - Status: Closed
     - Reason: After trading hours
     - Next Open: Tomorrow 09:30 EST
     - Trading Hours: 09:30 - 16:00 EST

**Test Data:**
- Test viewing AAPL at 21:00 EST on Tuesday
- Test viewing TSLA at 08:00 EST on Saturday

---

### 6.2 Crypto Market User Stories

#### US-004: View Crypto Market Always-Open Status
**As a** cryptocurrency trader
**I want to** see that crypto markets are always open
**So that** I know I can trade anytime

**Acceptance Criteria:**
1. Given crypto market (Binance) is always open
   - When I view crypto section
   - Then I see green dot with "Açık (24/7)" text
   - And I do NOT see "next open/close" times

2. Given I am viewing BTCUSDT at 3:00 AM on Sunday
   - When I check market status
   - Then status shows OPEN
   - And prices update in real-time every 1-2 seconds

**Test Data:**
- Test at various times: 02:00 UTC Sunday, 14:00 UTC Wednesday, 23:30 UTC Friday
- Verify WebSocket continues streaming on weekends

---

### 6.3 Cross-Market User Stories

#### US-005: Compare Market Status Across Multiple Markets
**As a** multi-asset trader
**I want to** see status of all markets at once
**So that** I know which markets I can trade in now

**Acceptance Criteria:**
1. Given I am on web dashboard
   - When I view "Market Overview" section at top
   - Then I see status for all markets:
     - [●] BIST: Açık (Kapanış: 18:00)
     - [●] NASDAQ: Kapalı (Açılış: 09:30)
     - [●] NYSE: Kapalı (Açılış: 09:30)
     - [●] CRYPTO: Açık (24/7)

2. Given I hover over any market status
   - When tooltip appears
   - Then I see detailed timing information

**Test Data:**
- Test at 15:00 TRT = 08:00 EST (BIST open, US closed)
- Test at 17:00 TRT = 10:00 EST (both open)

---

#### US-006: Receive Notification When Market Opens
**As a** active trader
**I want to** receive notification when my watched markets open
**So that** I don't miss trading opportunities

**Acceptance Criteria:**
1. Given I am subscribed to NASDAQ market status
   - When market status changes from CLOSED to OPEN at 09:30
   - Then I receive browser notification: "NASDAQ market has opened"
   - And in-app notification banner appears for 5 seconds

2. Given I am subscribed to BIST market status
   - When market status changes from PRE_MARKET to OPEN at 10:00
   - Then I receive mobile push notification (if mobile app)

**Test Data:**
- Mock status change: CLOSED → PRE_MARKET at 09:40 EST
- Mock status change: PRE_MARKET → OPEN at 09:30 EST
- Mock status change: OPEN → AFTER_HOURS at 16:00 EST

---

### 6.4 Data Quality User Stories

#### US-007: Understand Data Delay for Stocks
**As a** user viewing stock prices
**I want to** know if data is real-time or delayed
**So that** I make informed trading decisions

**Acceptance Criteria:**
1. Given stock data is 15-minute delayed (Yahoo Finance)
   - When I view stock price
   - Then I see badge: "15 dk gecikme" (15 min delayed)
   - And last update time: "15:30" (actual time is 15:45)

2. Given stock data is real-time (Alpaca)
   - When I view stock price
   - Then I see badge: "Gerçek zamanlı" (Real-time)
   - And I see recent timestamp: "Az önce" or "2 dakika önce"

**Test Data:**
- Yahoo Finance feed: 15-minute delay
- Alpaca feed: Real-time with <5 second delay

---

#### US-008: Detect Stale Data During Market Hours
**As a** trader during active market hours
**I want to** be warned if price data is stale
**So that** I know there may be a connection issue

**Acceptance Criteria:**
1. Given NASDAQ market is OPEN
   - And last price update was 25 minutes ago
   - When I view stock
   - Then I see yellow warning icon with "Veri güncel olmayabilir"

2. Given I tap on warning icon
   - When tooltip opens
   - Then I see message:
     - "Data Connection Issue"
     - "Last update: 25 minutes ago"
     - "Expected frequency: 15 minutes"

3. Given crypto market is OPEN
   - And no WebSocket update for 45 seconds
   - When I view crypto symbol
   - Then I see red error icon with "Bağlantı koptu"

**Test Data:**
- Simulate no updates for 30 minutes during OPEN
- Simulate WebSocket disconnect for crypto
- Verify no warning shown during CLOSED hours

---

## 7. Acceptance Criteria Summary

### 7.1 Functional Requirements

| ID | Requirement | Priority | Status |
|----|-------------|----------|---------|
| FR-001 | Display market status indicator on dashboard accordions | MUST | Pending |
| FR-002 | Show last update timestamp for each symbol | MUST | Pending |
| FR-003 | Calculate market status based on timezone-aware rules | MUST | Existing (backend) |
| FR-004 | Display next open/close time when market closed | MUST | Pending |
| FR-005 | Show market status tooltip with detailed info | SHOULD | Pending |
| FR-006 | Send SignalR event when market status changes | SHOULD | Pending |
| FR-007 | Show data staleness warnings during market hours | MUST | Pending |
| FR-008 | Display data source and delay information | SHOULD | Pending |
| FR-009 | Handle holiday calendar for each market | MUST | Existing (backend) |
| FR-010 | Support multiple timezones (TRT, EST, UTC) | MUST | Existing (backend) |

### 7.2 Non-Functional Requirements

| ID | Requirement | Target | Measurement |
|----|-------------|--------|-------------|
| NFR-001 | Market status update latency | <5 seconds | Time from market open to UI update |
| NFR-002 | API response time for status endpoint | <200ms | p95 response time |
| NFR-003 | Frontend status indicator render time | <100ms | Time to paint indicator |
| NFR-004 | Status accuracy | 99.9% | Correct status calculation |
| NFR-005 | Timezone conversion accuracy | 100% | No DST edge case failures |
| NFR-006 | Mobile battery impact | <2% additional | Battery drain from status checks |
| NFR-007 | Accessibility compliance | WCAG 2.1 AA | Screen reader compatibility |
| NFR-008 | Browser compatibility | 95% coverage | Chrome, Safari, Firefox, Edge |

---

## 8. Technical Implementation Notes

### 8.1 Backend Implementation

**Existing Components (Already Implemented):**
- ✅ `MarketHoursService`: Calculates market status with timezone awareness
- ✅ `MarketStatusService`: Manages market status state and change events
- ✅ `TradingSession` model: Defines trading hours per market
- ✅ Holiday calendars for BIST, NASDAQ, NYSE (2025-2026)
- ✅ Timezone handling (TRT, EST with DST)

**New/Enhanced Components Needed:**
- 🔄 Enhance `UnifiedMarketDataDto` with market status fields
- 🔄 Create `/api/markets/status/all` endpoint
- 🔄 Add `OnMarketStatusChanged` SignalR event
- 🔄 Implement data staleness detection logic
- 🆕 Create `MarketStatusBroadcastService` for SignalR events
- 🆕 Add cron job to check market status every 1 minute

**Database Changes:**
- ✅ No schema changes required (models already support required fields)
- 🔄 Update `markets` table with current status values
- 🔄 Populate `trading_sessions` table with session data

---

### 8.2 Frontend Implementation

**Mobile (React Native):**
- ✅ `MarketStatusIndicator` component exists (needs integration)
- ✅ `MarketStatusContext` exists (needs activation)
- 🔄 Integrate indicator into `AssetClassAccordion` headers
- 🆕 Add timestamp display to symbol cards
- 🆕 Implement staleness warning UI
- 🆕 Add market status tooltip modal

**Web (React):**
- 🆕 Create `MarketStatusBadge` component
- 🆕 Create `MarketOverviewBar` component for dashboard top
- 🆕 Add market status column to symbol table
- 🆕 Implement hover tooltips with Radix UI
- 🆕 Add timestamp formatting utilities

**Shared Services:**
- 🔄 Update `websocketService` to handle market status events
- 🔄 Update `multiAssetApi` to fetch market status data
- 🆕 Create `marketStatusService` for status management
- 🆕 Add timestamp formatting utilities
- 🆕 Implement staleness detection client-side

---

### 8.3 Testing Requirements

**Unit Tests:**
- ✅ `MarketHoursService` tests exist
- 🆕 Test market status calculation at boundary times (09:29, 09:30, 09:31)
- 🆕 Test timezone conversion accuracy
- 🆕 Test DST transition handling
- 🆕 Test holiday detection
- 🆕 Test staleness detection logic

**Integration Tests:**
- 🆕 Test `/api/markets/status` endpoint
- 🆕 Test SignalR market status events
- 🆕 Test market status update when crossing time boundaries
- 🆕 Test data flow: backend status → SignalR → frontend UI update

**E2E Tests:**
- 🆕 Test market status indicator visibility on dashboard
- 🆕 Test tooltip interaction (hover/tap)
- 🆕 Test staleness warning appearance
- 🆕 Test notification when market opens

**Manual Testing Checklist:**
- [ ] View dashboard at BIST open time (10:00 TRT)
- [ ] View dashboard at US market open time (09:30 EST)
- [ ] View dashboard on weekend (all closed except crypto)
- [ ] View dashboard on US holiday (US closed, BIST may be open)
- [ ] Simulate WebSocket disconnect and verify staleness warning
- [ ] Test mobile vs web layout differences
- [ ] Test Turkish language translations
- [ ] Test screen reader accessibility

---

## 9. Dependencies and Constraints

### 9.1 External Dependencies

| Dependency | Purpose | Risk | Mitigation |
|------------|---------|------|------------|
| Yahoo Finance API | Stock price data (15-min delayed) | Rate limiting, downtime | Implement retry logic, caching |
| Alpaca Markets API | Real-time US stock data | API quota, cost | Monitor usage, fallback to Yahoo |
| Binance WebSocket | Real-time crypto data | Connection drops | Auto-reconnect, exponential backoff |
| NodaTime / TimeZoneInfo | Timezone calculations | DST edge cases | Extensive testing around DST transitions |

### 9.2 Technical Constraints

1. **Data Provider Limitations:**
   - Yahoo Finance: 15-minute delay, rate limits (2000 requests/hour)
   - Alpaca: Free tier limited symbols, paid tier required for full market
   - Binance: WebSocket connection limits (1024 streams max)

2. **Performance Constraints:**
   - Market status calculation: <50ms per market
   - Status broadcast to all connected clients: <500ms
   - Database holiday calendar size: <1000 holidays per market

3. **Browser/Mobile Constraints:**
   - Mobile: Battery drain from frequent status checks
   - Web: Multiple tabs open may cause duplicate status requests
   - Offline mode: Status unknown when no connection

### 9.3 Business Constraints

1. **Holiday Calendar Maintenance:**
   - Must be updated manually each year by January 1st
   - No automated holiday prediction (requires human verification)
   - Responsibility: Platform Administrator

2. **Market Coverage:**
   - Initial release: BIST, NASDAQ, NYSE, Crypto (Binance)
   - Future expansion: LSE, TSE, HKEX (out of scope for v1.0)

3. **Language Support:**
   - Primary: Turkish (TR)
   - Secondary: English (EN)
   - Status text must be localized

---

## 10. Traceability Matrix

| Requirement ID | User Story | Test Case | Code Module | Status |
|---------------|------------|-----------|-------------|---------|
| FR-001 | US-001, US-002 | TC-UI-001 | AssetClassAccordion.tsx | Pending |
| FR-002 | US-003, US-007 | TC-UI-002 | SymbolCard.tsx | Pending |
| FR-003 | US-001, US-002 | TC-BE-001 | MarketHoursService.cs | Existing |
| FR-004 | US-003 | TC-UI-003 | MarketStatusTooltip.tsx | Pending |
| FR-005 | US-003, US-005 | TC-UI-004 | MarketStatusBadge.tsx | Pending |
| FR-006 | US-006 | TC-RT-001 | MarketStatusBroadcastService.cs | Pending |
| FR-007 | US-008 | TC-UI-005 | StalenessWarning.tsx | Pending |
| FR-008 | US-007 | TC-UI-006 | DataSourceBadge.tsx | Pending |
| FR-009 | US-001, US-003 | TC-BE-002 | MarketHoursService.cs | Existing |
| FR-010 | US-001, US-002 | TC-BE-003 | MarketHoursService.cs | Existing |

---

## 11. Regulatory and Compliance Considerations

### 11.1 Financial Data Disclaimers

**Requirement:** Display disclaimers for delayed data

**Disclaimer Text (Turkish):**
```
Gösterilen fiyatlar 15 dakika gecikmeli olup, yalnızca bilgilendirme amaçlıdır.
Yatırım kararları vermeden önce güncel piyasa verilerini kontrol ediniz.
```

**Disclaimer Text (English):**
```
Prices shown are delayed by 15 minutes and are for informational purposes only.
Please verify current market data before making investment decisions.
```

**Placement:**
- Footer of mobile app
- Footnote on web dashboard
- Tooltip when tapping "15 dk gecikme" badge

### 11.2 Data Provider Attribution

**Requirement:** Attribute data sources

**Attribution Text:**
```
Market data provided by:
- Crypto: Binance API
- US Stocks: Alpaca Markets / Yahoo Finance
- BIST: Yahoo Finance
```

**Placement:** Settings → About → Data Sources

### 11.3 GDPR Compliance

**Requirement:** User timezone detection

- Store user timezone preference (not automatic detection without consent)
- Allow user to manually set timezone in settings
- Default to UTC if user hasn't set preference

---

## 12. Release Plan

### 12.1 Phase 1: Backend Foundation (Week 1-2)

**Deliverables:**
- ✅ Market status calculation logic (already exists)
- 🔄 Enhanced API endpoints for market status
- 🔄 SignalR market status change events
- 🔄 Data staleness detection logic
- 🔄 Unit tests for market status service

**Acceptance Criteria:**
- API returns correct market status for all markets
- Status changes trigger SignalR events
- Holiday calendar accurate for 2025-2026

---

### 12.2 Phase 2: Mobile UI Integration (Week 3-4)

**Deliverables:**
- 🔄 Market status indicator in accordion headers
- 🔄 Last update timestamp on symbol cards
- 🔄 Market status tooltip modal
- 🔄 Staleness warning UI
- 🔄 Integration with WebSocket service

**Acceptance Criteria:**
- Users can see market status at a glance
- Timestamp updates in real-time
- Tooltip shows detailed market info
- Staleness warnings appear when appropriate

---

### 12.3 Phase 3: Web UI Integration (Week 5-6)

**Deliverables:**
- 🆕 Market overview bar component
- 🆕 Market status badges on accordions
- 🆕 Hover tooltips for market details
- 🆕 Symbol table column for last update
- 🆕 Responsive design for mobile web

**Acceptance Criteria:**
- Web UI matches mobile functionality
- Hover interactions work smoothly
- Layout adapts to screen size

---

### 12.4 Phase 4: Testing and Refinement (Week 7-8)

**Deliverables:**
- 🆕 E2E test suite for market status
- 🆕 Accessibility audit and fixes
- 🆕 Performance optimization
- 🆕 User acceptance testing
- 🆕 Documentation updates

**Acceptance Criteria:**
- All tests passing (unit, integration, E2E)
- WCAG 2.1 AA compliance
- API response time <200ms p95
- User feedback incorporated

---

## 13. Success Metrics

### 13.1 User Experience Metrics

| Metric | Baseline | Target | Measurement Method |
|--------|----------|--------|-------------------|
| Support tickets about "prices not updating" | 20/month | <5/month | Ticket tracking |
| User confusion rate | Unknown | <5% | User survey |
| Time to understand market status | Unknown | <2 seconds | Eye tracking study |
| Feature adoption rate | 0% | >80% | Analytics tracking |

### 13.2 Technical Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Market status API response time | <200ms p95 | APM monitoring |
| Status indicator render time | <100ms | Performance profiling |
| SignalR event delivery time | <1 second | Event timestamping |
| Frontend error rate | <0.1% | Error tracking |
| Backend error rate | <0.01% | Logging |

### 13.3 Business Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| User session duration | +15% increase | Analytics |
| Trading activity during market hours | +20% increase | Transaction logs |
| User retention (7-day) | +10% increase | Cohort analysis |
| Net Promoter Score (NPS) | +5 points | User survey |

---

## 14. Risks and Mitigation Strategies

### 14.1 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **API rate limits exceeded** | High | High | Implement caching, request throttling, fallback providers |
| **Timezone calculation errors** | Medium | High | Extensive testing, use battle-tested libraries (NodaTime) |
| **SignalR connection drops** | Medium | Medium | Auto-reconnect, exponential backoff, show connection status |
| **Mobile battery drain** | Low | Medium | Optimize polling frequency, use push notifications |
| **DST transition edge cases** | Medium | High | Test thoroughly around DST transition dates |

### 14.2 Business Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **Holiday calendar outdated** | Medium | High | Set up automated reminders, admin dashboard warning |
| **User confusion about delayed data** | High | Medium | Clear disclaimers, prominent "15 min delayed" badge |
| **Localization errors** | Low | Low | Native speaker review of Turkish translations |
| **Performance degradation** | Low | High | Load testing, performance monitoring, CDN usage |

---

## 15. Glossary

| Term | Definition |
|------|------------|
| **Market Status** | Current state of a trading market (OPEN, CLOSED, PRE_MARKET, etc.) |
| **Trading Session** | Defined period when a market is open for trading |
| **Pre-Market** | Trading session before regular market hours (US markets only) |
| **After-Hours** | Trading session after regular market close (US markets only) |
| **Data Staleness** | Age of market data, measured from last update timestamp |
| **Real-time Data** | Market data with <5 second delay from actual trade execution |
| **Delayed Data** | Market data with 15-minute delay (typical for free tier APIs) |
| **Holiday Calendar** | List of dates when a market is closed for official holidays |
| **Timezone Offset** | Difference in hours from UTC (e.g., EST = UTC-5) |
| **DST** | Daylight Saving Time (clock adjustment in spring/fall) |
| **Ticker Symbol** | Unique identifier for a tradable asset (e.g., AAPL, BTCUSDT) |
| **Exchange** | Trading venue where securities are bought/sold (NASDAQ, BIST, etc.) |

---

## 16. Appendices

### Appendix A: API Endpoint Reference

**Base URL:** `https://api.mytrader.com/v1`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/markets/status/all` | GET | Optional | Get status for all markets |
| `/markets/status/{code}` | GET | Optional | Get status for specific market |
| `/symbols/{id}/market-timing` | GET | Optional | Get market timing for symbol |
| `/market-data/unified` | GET | Required | Get unified market data with status |

### Appendix B: SignalR Event Reference

| Event | Payload | Frequency |
|-------|---------|-----------|
| `OnMarketStatusChanged` | `MarketStatusChangeDto` | On status change |
| `OnDataStalenessWarning` | `StalenessWarningDto` | When staleness detected |
| `OnMarketTimingUpdate` | `MarketTimingDto` | Every 5 minutes |

### Appendix C: Color Palette Reference

```
Open Green:       #10b981 (rgb(16, 185, 129))
Warning Amber:    #f59e0b (rgb(245, 158, 11))
Closed Red:       #ef4444 (rgb(239, 68, 68))
Unknown Gray:     #6b7280 (rgb(107, 114, 128))
Background White: #ffffff (rgb(255, 255, 255))
Text Dark:        #1f2937 (rgb(31, 41, 55))
Text Light:       #6b7280 (rgb(107, 114, 128))
```

### Appendix D: Timezone Reference

| Market | IANA ID | Windows ID | UTC Offset |
|--------|---------|------------|------------|
| BIST | Europe/Istanbul | Turkey Standard Time | UTC+3 |
| NASDAQ | America/New_York | Eastern Standard Time | UTC-5/-4 |
| NYSE | America/New_York | Eastern Standard Time | UTC-5/-4 |
| Crypto | UTC | UTC | UTC+0 |

---

## Document Approval

| Role | Name | Date | Signature |
|------|------|------|-----------|
| **Business Analyst** | [Your Name] | 2025-10-09 | [Pending] |
| **Product Owner** | [TBD] | [TBD] | [Pending] |
| **Tech Lead** | [TBD] | [TBD] | [Pending] |
| **UX Designer** | [TBD] | [TBD] | [Pending] |
| **QA Lead** | [TBD] | [TBD] | [Pending] |

---

## Change Log

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-09 | Business Analyst | Initial draft |

---

**END OF DOCUMENT**
