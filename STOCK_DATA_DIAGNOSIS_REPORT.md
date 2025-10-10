# Stock Data Flow Diagnosis Report

## Executive Summary

**Issue**: Stock data is not appearing in mobile frontend despite backend successfully polling and broadcasting prices.

**Status**: ROOT CAUSE IDENTIFIED - Multiple potential issues found in the data flow

**Priority**: CRITICAL

---

## Architecture Overview

```
Yahoo Finance API
        ↓
YahooFinancePollingService (polls every 60s)
        ↓ (fires StockPriceUpdated event)
MultiAssetDataBroadcastService
        ↓ (broadcasts via SignalR)
DashboardHub + MarketDataHub
        ↓ (groups: STOCK_AAPL, AssetClass_STOCK)
Mobile WebSocket Connection
        ↓ (event listeners: PriceUpdate, price_update)
PriceContext (enhancedPrices state)
        ↓
DashboardScreen (filters by marketName)
        ↓
UI Display
```

---

## Diagnostic Findings

### ✅ WORKING Components

1. **Backend Polling**
   - File: `YahooFinancePollingService.cs`
   - Status: ✅ Working - polls every 60 seconds
   - Evidence: Backend logs show "Broadcasting price update: STOCK AAPL = 258.06"

2. **Event System**
   - File: `YahooFinancePollingService.cs` line 69
   - Status: ✅ Event `StockPriceUpdated` fires correctly

3. **SignalR Broadcasting Service**
   - File: `MultiAssetDataBroadcastService.cs`
   - Status: ✅ Subscribed to `StockPriceUpdated` event (line 69)
   - Status: ✅ Broadcasting to both DashboardHub and MarketDataHub

4. **SignalR Group Structure**
   - Groups created: `STOCK_{symbol}` and `AssetClass_STOCK`
   - Example: `STOCK_AAPL`, `AssetClass_STOCK`

5. **Hub Auto-Subscription**
   - File: `DashboardHub.cs` lines 51-54
   - Status: ✅ Auto-subscribes clients to `AssetClass_STOCK` on connection
   - This means clients SHOULD receive stock updates automatically!

6. **Frontend Hub Connection**
   - File: `websocketService.ts` line 118
   - Status: ✅ Correctly connects to `/hubs/dashboard`

7. **Frontend Event Listeners**
   - File: `websocketService.ts` lines 200-236
   - Status: ✅ Listening for:
     - `PriceUpdate` (line 213)
     - `ReceivePriceUpdate` (line 200)
     - `MarketDataUpdate` (line 226)
     - `ReceiveBatchPriceUpdate` (line 238)

8. **Frontend Symbol Fetching**
   - File: `PriceContext.tsx` lines 271-273
   - Status: ✅ Fetching STOCK symbols from API
   - API: `/api/symbol-preferences/defaults?assetClass=STOCK`

---

### ❌ POTENTIAL ISSUES

#### Issue 1: Event Data Format Mismatch

**Location**: Backend broadcasts → Frontend receives

**Backend Sends** (MultiAssetDataBroadcastService.cs lines 148-166):
```csharp
var multiAssetUpdate = new MultiAssetPriceUpdate
{
    Type = "PriceUpdate",
    AssetClass = stockUpdate.AssetClass,  // "STOCK"
    Symbol = stockUpdate.Symbol,           // "AAPL"
    Price = stockUpdate.Price,
    // ... other fields
};
```

**Frontend Expects** (PriceContext.tsx lines 139-165):
```typescript
const normalizedData: UnifiedMarketDataDto = {
    symbolId: data.symbolId || data.id || data.symbol,  // ⚠️ No 'symbolId' in backend data!
    symbol: data.symbol || data.symbolId,
    displayName: data.displayName || data.name || data.symbol,
    assetClass: data.assetClass || 'CRYPTO',
    // ...
};
```

**Problem**: Backend sends `Symbol` (capital S) but frontend expects `symbolId` or `symbol` (lowercase s).

**Fix**: Ensure field name consistency.

---

#### Issue 2: marketName Field Missing

**Location**: Symbol data transformation

**Frontend Filtering** (DashboardScreen.tsx lines 205-215):
```typescript
bistSymbols = allStocks.filter(s =>
  s.marketName?.toUpperCase() === 'BIST'
);
```

**API Response** (multiAssetApi.ts line 549-552):
```typescript
const transformedSymbols = symbols.map((symbol: any) => ({
  ...symbol,
  marketName: symbol.marketName || symbol.market,
}));
```

**Problem**: Backend may be sending `market` field, but transformation might not be working.

**Test Needed**: Verify API response contains `market` or `marketName` field.

---

#### Issue 3: WebSocket Message Not Reaching State

**Location**: WebSocket event → PriceContext state update

**Event Handler** (PriceContext.tsx lines 123-193):
- Receives `price_update` event
- Normalizes data
- Calls `setEnhancedPrices`

**Potential Issue**:
- Normalization might fail due to field name mismatch
- Console logs show data arriving but state not updating

**Debug Needed**: Add logging to track:
1. Raw SignalR message received
2. After normalization
3. State update success

---

#### Issue 4: Symbol ID vs Symbol Name Confusion

**Location**: State indexing and lookup

**State Structure** (PriceContext.tsx line 177-189):
```typescript
setEnhancedPrices(prev => {
  const updated = {
    ...prev,
    [normalizedData.symbolId]: normalizedData  // Indexed by symbolId
  };
  // Also index by symbol for lookup flexibility
  if (normalizedData.symbol) {
    updated[normalizedData.symbol] = normalizedData;  // Also indexed by symbol
  }
  return updated;
});
```

**Problem**:
- Backend sends `Symbol: "AAPL"`
- Frontend tries to create `symbolId` from various fields
- If `symbolId` is undefined, data won't be stored correctly

---

## Recommended Fix Strategy

### Phase 1: Immediate Debug (5 minutes)

Add comprehensive logging to track the exact data flow:

**File**: `frontend/mobile/src/context/PriceContext.tsx`

Add after line 123 (inside `price_update` handler):
```typescript
console.log('[PriceContext] RAW SignalR message:', JSON.stringify(data, null, 2));
console.log('[PriceContext] Field names:', Object.keys(data));
console.log('[PriceContext] AssetClass:', data.assetClass || data.AssetClass);
console.log('[PriceContext] Symbol:', data.symbol || data.Symbol);
```

### Phase 2: Fix Field Name Mapping (10 minutes)

**File**: `frontend/mobile/src/context/PriceContext.tsx` lines 139-165

Change to handle both uppercase and lowercase field names:
```typescript
const normalizedData: UnifiedMarketDataDto = {
  symbolId: data.symbolId || data.id || data.symbol || data.Symbol,
  symbol: data.symbol || data.Symbol || data.symbolId,
  displayName: data.displayName || data.name || data.symbol || data.Symbol,
  assetClass: (data.assetClass || data.AssetClass || 'CRYPTO') as AssetClassType,
  market: data.market || data.Market || 'UNKNOWN',
  price: priceNormalized.price,
  // ... rest of fields
};
```

### Phase 3: Verify API Response (5 minutes)

**Test**: Call API manually and check response structure

```bash
curl "http://192.168.68.102:5002/api/symbol-preferences/defaults?assetClass=STOCK"
```

Expected response should include `market` or `marketName` field for each symbol.

### Phase 4: Test Data Flow (5 minutes)

1. Start mobile app with new logging
2. Watch console for:
   - "RAW SignalR message" logs
   - "Field names" logs
   - "enhancedPrices state updated" logs
3. Verify stock symbols appear in logs
4. Check if state contains stock data

---

## Quick Verification Commands

### Backend Test (verify broadcasting)
```bash
# Check backend logs for:
grep "Broadcasting price update: STOCK" backend.log
```

### Frontend Test (verify receiving)
```bash
# In mobile app console, check for:
[PriceContext] RAW price_update:
[PriceContext] Loaded X stock symbols:
[PriceContext] enhancedPrices state updated: X items
```

### API Test (verify symbol data)
```bash
curl -s "http://192.168.68.102:5002/api/symbol-preferences/defaults?assetClass=STOCK" | jq '.'
```

---

## Next Steps

1. ✅ **Confirmed**: DashboardHub auto-subscribes clients to `AssetClass_STOCK`
2. ✅ **Confirmed**: Backend is broadcasting to correct groups
3. ⚠️ **Need to verify**: Field name case sensitivity (Symbol vs symbol)
4. ⚠️ **Need to verify**: API response includes market/marketName field
5. ⚠️ **Need to test**: Add logging and observe actual data flow

---

## Expected Outcome After Fix

1. Mobile app console shows: "Loaded 10 stock symbols: AAPL, MSFT, ..."
2. Mobile app console shows: "RAW price_update: { Symbol: 'AAPL', Price: 258.06, ... }"
3. Mobile app console shows: "enhancedPrices state updated: 15 items" (5 crypto + 10 stocks)
4. Dashboard sections show stock data instead of "Veri yok"
5. Stock prices update in real-time every 60 seconds

---

## Database Issue (Secondary)

**Note**: Database writes are currently DISABLED in `YahooFinancePollingService.cs` line 188-190.

**Reason**: `market_data` table doesn't have `asset_class` column.

**Fix Required** (if database persistence needed):
1. Add migration to add `asset_class` column
2. Enable database writes in YahooFinancePollingService
3. Update queries to filter by asset_class

**Priority**: LOW (frontend display is more critical than database persistence)

---

## Contact Points for Issues

- **Backend Broadcasting**: `MultiAssetDataBroadcastService.cs`
- **Frontend Reception**: `PriceContext.tsx`
- **Data Transformation**: `multiAssetApi.ts`
- **UI Display**: `DashboardScreen.tsx`
- **SignalR Hub**: `DashboardHub.cs` and `MarketDataHub.cs`

---

Generated: 2025-10-09
Status: Ready for implementation
