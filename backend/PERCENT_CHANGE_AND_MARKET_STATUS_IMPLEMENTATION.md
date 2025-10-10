# Percent Change Fix & Market Status Implementation Summary

**Date:** 2025-10-10
**Status:** Percent Change Fix COMPLETE ✅ | Market Status Implementation PENDING
**Priority:** CRITICAL

---

## ✅ COMPLETED: Task 1 - Percent Change Calculation Fix

### Problem
The percent change calculation was using `previousClose` as the denominator instead of `openPrice`.

**Incorrect Formula:**
```csharp
priceChangePercent = (priceChange / previousClose) * 100
```

**Correct Formula:**
```csharp
priceChange = currentPrice - previousClose  // Keep this
priceChangePercent = (priceChange / openPrice) * 100  // Use openPrice
```

### Files Modified

#### 1. AlpacaStreamingService.cs ✅
**Location:** `backend/MyTrader.Infrastructure/Services/AlpacaStreamingService.cs`

**Changes:**
- Added `_openPriceCache` dictionary to cache open prices
- **ProcessBarMessage** (line ~502): Now uses `bar.O` as denominator and caches it
- **ProcessTradeMessage** (line ~423): Now uses cached `openPrice` with fallback to `previousClose`
- **ProcessQuoteMessage** (line ~462): Now uses cached `openPrice` with fallback to `previousClose`

```csharp
// Added cache
private readonly ConcurrentDictionary<string, decimal> _openPriceCache = new();

// ProcessBarMessage example
var openPrice = bar.O;
var priceChange = bar.C - previousClose;
var priceChangePercent = openPrice > 0 ? (priceChange / openPrice) * 100 : 0;
_openPriceCache[bar.S] = openPrice;  // Cache for Trade/Quote messages
```

#### 2. YahooFinanceProvider.cs ✅
**Location:** `backend/MyTrader.Services/Market/YahooFinanceProvider.cs`

**Changes (line ~167):**
```csharp
if (previousClose.HasValue && openPrice.HasValue && openPrice.Value > 0)
{
    priceChange = currentPrice.Value - previousClose.Value;
    priceChangePercent = (priceChange.Value / openPrice.Value) * 100;  // Use openPrice
}
else if (previousClose.HasValue && previousClose.Value > 0)
{
    // Fallback if openPrice not available
    priceChange = currentPrice.Value - previousClose.Value;
    priceChangePercent = (priceChange.Value / previousClose.Value) * 100;
}
```

#### 3. DataSourceRouter.cs - NO CHANGES NEEDED ✅
**Location:** `backend/MyTrader.Core/Services/DataSourceRouter.cs`

**Rationale:** The circuit breaker validation (line 332) correctly uses `previousClose` because it's checking for anomalous price movements, not calculating the display percent change.

---

## ⏳ PENDING: Task 2 - Market Status Checks

### Overview
Stock market data services should pause when markets are closed and resume when they open.

### Database Schema
The migration `20251010_CreateMarketsTable.sql` added the `Markets` table with these relevant columns:

```sql
CREATE TABLE "Markets" (
    "MarketCode" VARCHAR(50) NOT NULL,
    "RegularMarketOpen" TIME,
    "RegularMarketClose" TIME,
    "CurrentStatus" VARCHAR(20),        -- 'OPEN', 'CLOSED', 'PRE_MARKET', 'POST_MARKET', 'HOLIDAY'
    "EnableDataFetching" BOOLEAN,
    "DataFetchInterval" INTEGER,        -- Seconds between fetches when OPEN
    "DataFetchIntervalClosed" INTEGER,  -- Seconds between checks when CLOSED
    ...
);

-- Helper view
CREATE VIEW "vw_MarketStatus" AS ...
```

**Market Codes:**
- `NASDAQ` - Stock market (pause when closed)
- `NYSE` - Stock market (pause when closed)
- `BIST` - Stock market (pause when closed)
- `CRYPTO` - Crypto market (ALWAYS OPEN - 24/7)

### Implementation Requirements

#### Services to Modify (STOCKS ONLY)

1. **AlpacaStreamingService.cs** ⚠️
   - Check market status before connecting/subscribing
   - Pause WebSocket when market closed
   - Resume when market opens
   - Query: `SELECT "CurrentStatus", "EnableDataFetching" FROM "Markets" WHERE "MarketCode" IN ('NASDAQ', 'NYSE')`

2. **YahooFinanceProvider.cs** ⚠️
   - Check market status before fetching prices
   - Skip polling when market closed
   - Resume when market opens

3. **YahooFinanceIntradayScheduledService.cs** ⚠️
   - Check market status in scheduled job
   - Use `DataFetchInterval` when OPEN
   - Use `DataFetchIntervalClosed` when CLOSED

#### Services NOT to Modify (CRYPTO 24/7)

✅ **BinanceWebSocketService** - NO CHANGES
✅ **Any Crypto Services** - NO CHANGES
✅ Crypto markets run 24/7, always fetch data

### Implementation Pattern

```csharp
// Add to service class
private async Task<bool> ShouldFetchDataAsync(string marketCode)
{
    using var scope = _scopeFactory.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ITradingDbContext>();

    var market = await dbContext.Markets
        .Where(m => m.Code == marketCode)
        .Select(m => new { m.Status, m.EnableDataFetching })
        .FirstOrDefaultAsync();

    if (market == null) return true;  // Default to fetch if market not found

    if (market.Status != "OPEN")
    {
        _logger.LogInformation("Market {MarketCode} is {Status}, pausing data fetch",
            marketCode, market.Status);
        return false;
    }

    if (!market.EnableDataFetching)
    {
        _logger.LogInformation("Data fetching disabled for market {MarketCode}", marketCode);
        return false;
    }

    return true;
}

// Usage in AlpacaStreamingService
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        // Check market status before fetching
        if (await ShouldFetchDataAsync("NASDAQ"))
        {
            // Fetch data
        }
        else
        {
            // Wait longer interval when closed
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            continue;
        }

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
}
```

### Logging Requirements

**When Market Closes:**
```csharp
_logger.LogInformation("Market {MarketCode} is {Status}, pausing data fetch",
    marketCode, status);
```

**When Market Opens:**
```csharp
_logger.LogInformation("Market {MarketCode} opened, resuming data fetch", marketCode);
```

**For Crypto (No Change):**
```csharp
_logger.LogDebug("CRYPTO market always open, continuing 24/7 data fetch");
```

---

## 📊 Task 3 - Market Status Counters

### Requirement
Update market overview statistics to show:
- Number of open markets
- Number of closed markets
- Use `vw_MarketStatus` view

### Implementation Location
Likely in:
- `DashboardController.cs`
- `MarketStatusController.cs`
- `DashboardHub.cs` (SignalR)

### Query Example
```sql
SELECT
    COUNT(*) FILTER (WHERE "CurrentStatus" = 'OPEN') as "OpenMarkets",
    COUNT(*) FILTER (WHERE "CurrentStatus" = 'CLOSED') as "ClosedMarkets",
    COUNT(*) FILTER (WHERE "CurrentStatus" = 'HOLIDAY') as "HolidayMarkets",
    COUNT(*) FILTER (WHERE "CurrentStatus" = 'PRE_MARKET') as "PreMarkets",
    COUNT(*) FILTER (WHERE "CurrentStatus" = 'POST_MARKET') as "PostMarkets"
FROM "vw_MarketStatus";
```

---

## 🧪 Testing & Validation

### Test Scenarios

#### 1. Percent Change Calculation ✅
- [x] AlpacaStreamingService BAR messages use openPrice
- [x] AlpacaStreamingService TRADE messages use cached openPrice
- [x] AlpacaStreamingService QUOTE messages use cached openPrice
- [x] YahooFinanceProvider uses openPrice when available
- [x] Fallback to previousClose if openPrice unavailable

#### 2. Market Status Checks ⏳
- [ ] NASDAQ/NYSE services pause when market closed
- [ ] NASDAQ/NYSE services resume when market opens
- [ ] BIST services pause when market closed
- [ ] CRYPTO services remain 24/7 (unaffected)
- [ ] Appropriate logging for status transitions
- [ ] Correct fetch intervals used (5s open, 300s closed)

#### 3. Market Status Counters ⏳
- [ ] Dashboard shows correct open/closed count
- [ ] Counts update in real-time via SignalR
- [ ] All market statuses represented (OPEN, CLOSED, HOLIDAY, etc.)

### Manual Test Commands

```bash
# Test percent change calculation
# Check logs for "Processed bar" messages showing correct percentages

# Test market status
# 1. Check current market status
psql -d mytrader -c "SELECT * FROM vw_MarketStatus;"

# 2. Manually update market status to test pause/resume
psql -d mytrader -c "UPDATE Markets SET status = 'CLOSED' WHERE code = 'NASDAQ';"
# Check logs: Should show "Market NASDAQ is CLOSED, pausing data fetch"

# 3. Resume market
psql -d mytrader -c "UPDATE Markets SET status = 'OPEN' WHERE code = 'NASDAQ';"
# Check logs: Should show "Market NASDAQ opened, resuming data fetch"

# 4. Verify crypto unaffected
# CRYPTO market should continue fetching regardless of time/status
```

---

## 🚨 Critical Notes

### ✅ DO:
- Pause STOCK market data fetching (NASDAQ, NYSE, BIST) when closed
- Use `openPrice` as denominator for percent change
- Cache open prices in AlpacaStreamingService for Trade/Quote messages
- Log all market status transitions
- Use appropriate fetch intervals based on market status

### ❌ DON'T:
- Modify Binance or any crypto services (they run 24/7)
- Change DataSourceRouter circuit breaker logic (it's correct)
- Break existing functionality while adding new features
- Forget fallback logic when openPrice unavailable

---

## 📁 File Reference

### Modified Files (Percent Change Fix)
1. `backend/MyTrader.Infrastructure/Services/AlpacaStreamingService.cs`
2. `backend/MyTrader.Services/Market/YahooFinanceProvider.cs`

### Files Needing Modification (Market Status)
1. `backend/MyTrader.Infrastructure/Services/AlpacaStreamingService.cs`
2. `backend/MyTrader.Services/Market/YahooFinanceProvider.cs`
3. `backend/MyTrader.Infrastructure/Services/YahooFinanceIntradayScheduledService.cs`
4. Dashboard/Market status counter services

### Database Files
1. `backend/MyTrader.Infrastructure/Migrations/20251010_CreateMarketsTable.sql`
2. View: `vw_MarketStatus`

---

## 🎯 Next Steps

1. ✅ **DONE:** Fix percent change calculation (use openPrice)
2. ⏳ **TODO:** Update C# Market model to include new columns from migration
3. ⏳ **TODO:** Implement market status checks in AlpacaStreamingService
4. ⏳ **TODO:** Implement market status checks in YahooFinance services
5. ⏳ **TODO:** Add market status counters to dashboard
6. ⏳ **TODO:** Test all changes with real market data
7. ⏳ **TODO:** Verify crypto services unaffected

---

## 📞 Questions for Clarification

1. **Market Model Update:** The C# `Market` model needs columns added:
   - `CurrentStatus` (or map to existing `Status`)
   - `EnableDataFetching` (new)
   - `DataFetchInterval` (new)
   - `DataFetchIntervalClosed` (new)

2. **Migration Status:** Has `20251010_CreateMarketsTable.sql` been applied to production?

3. **Market Status Update:** How is the `CurrentStatus` field updated?
   - Scheduled job?
   - External service?
   - Call to `update_market_status()` function?

---

**Implementation Completed By:** Claude Code (Orchestrator)
**Date:** 2025-10-10
**Status:** Percent Change ✅ COMPLETE | Market Status ⏳ PENDING
