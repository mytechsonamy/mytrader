# Previous Close Display Fix - Validation Guide

## Issue Summary
Mobile frontend was NOT displaying "Önceki Kapanış" (Previous Close) field for stocks despite backend broadcasting the data correctly.

## Root Cause
Field name case sensitivity mismatch between backend (PascalCase: `PreviousClose`) and frontend (camelCase: `previousClose`).

## Fixes Implemented

### 1. Enhanced Field Mapping in PriceContext.tsx
**File**: `frontend/mobile/src/context/PriceContext.tsx`
**Changes**:
- Added robust case-insensitive field extraction using nullish coalescing (`??`)
- Handles: `previousClose`, `PreviousClose`, `prevClose`, `PrevClose`
- Applied to both `price_update` and `batch_price_update` handlers

### 2. Debug Logging Added
**Locations**:
- `PriceContext.tsx`: Comprehensive logging at data reception and normalization
- `AssetCard.tsx`: Component-level rendering diagnostics

**What to look for in logs**:
```
✅ Stock AAPL HAS previousClose: 254.04
```

vs error case:
```
❌ MISSING previousClose for STOCK: AAPL
```

### 3. Backend Verification
**Confirmed Working**:
- `MultiAssetPriceUpdate.cs`: Field exists (line 38)
- `MultiAssetDataBroadcastService.cs`: Field populated (lines 206, 247)
- `StockPriceData.cs`: Correct percentage calculation
- `YahooFinanceProvider.cs`: Proper formula: `((current - previousClose) / previousClose) * 100`

## Testing Instructions

### Step 1: Start Backend
```bash
cd backend/MyTrader.Api
dotnet run
```

**Expected**: Backend starts on port 5002, begins polling Yahoo Finance

### Step 2: Start Mobile App
```bash
cd frontend/mobile
npm start
```

### Step 3: Monitor Console Logs

**Look for these log patterns**:

1. **WebSocket Connection**:
```
[PriceContext] Connection status changed: connected
[PriceContext] Subscribing to STOCK symbols: [AAPL, MSFT, GOOGL, ...]
[PriceContext] Successfully subscribed to STOCK price updates
```

2. **Price Updates Received**:
```
[PriceContext] RAW price_update - All fields: ['Symbol', 'Price', 'PreviousClose', 'Change24h', ...]
[PriceContext] RAW price_update: {
  symbol: 'AAPL',
  assetClass: 'STOCK',
  price: 256.26,
  previousClose: 254.04,
  ...
}
```

3. **Normalization Success**:
```
[PriceContext] Normalized price_update: {
  symbolId: 'AAPL',
  symbol: 'AAPL',
  assetClass: 'STOCK',
  price: 256.26,
  previousClose: 254.04,
  changePercent: 0.87
}
[PriceContext] ✅ Stock AAPL HAS previousClose: 254.04
```

4. **Component Rendering**:
```
[AssetCard] Rendering AAPL: {
  hasMarketData: true,
  price: 256.26,
  previousClose: 254.04,
  previousCloseType: 'number',
  previousCloseUndefined: false,
  previousCloseNull: false,
  willShowPreviousClose: true
}
```

### Step 4: Visual Validation

**Check Mobile UI for**:

#### Compact View (Dashboard Accordion):
- Stock symbol (e.g., "AAPL")
- Current price (e.g., "$256,26")
- Percentage change (e.g., "+0.87%")
- **Previous Close** (e.g., "Önc: $254,04") ← THIS SHOULD NOW BE VISIBLE

#### Full View (Stock Detail):
- "Önceki Kapanış: $254,04" ← THIS SHOULD BE VISIBLE

### Step 5: Test All Stock Types

**BIST Stocks** (Turkish stocks):
- GARAN, THYAO, SISE
- Should show "Önc: ₺XXX,XX"

**NASDAQ Stocks** (US tech):
- AAPL, MSFT, GOOGL
- Should show "Önc: $XXX,XX"

**NYSE Stocks** (US traditional):
- Should also show "Önc: $XXX,XX"

## Success Criteria

### ✅ Pass Conditions:
1. All stock cards display "Önc: $XXX,XX" or "Önc: ₺XXX,XX"
2. Console shows "✅ Stock {SYMBOL} HAS previousClose" messages
3. No "❌ MISSING previousClose" errors in console
4. Percentage calculation is correct (not showing price difference amount)
5. Crypto/Binance data unchanged (no regressions)

### ❌ Fail Conditions:
1. "Önc:" field still not visible
2. Console shows "❌ MISSING previousClose for STOCK" errors
3. `previousClose` is `undefined` or `null` in logs
4. Percentage shows as dollar amount instead of percentage

## Troubleshooting

### If "Önc:" Still Not Showing:

1. **Check WebSocket Connection**:
   ```
   Look for: [PriceContext] Connection status changed: connected
   ```

2. **Verify Data Reception**:
   ```
   Search logs for: RAW price_update - All fields
   Check if 'PreviousClose' or 'previousClose' is in the array
   ```

3. **Check Normalization**:
   ```
   Search logs for: previousClose: 254.04
   Should NOT be: previousClose: undefined
   ```

4. **Verify Component Props**:
   ```
   Search logs for: [AssetCard] Rendering {SYMBOL}
   willShowPreviousClose: should be true
   ```

### Common Issues:

**Issue**: WebSocket not connecting
**Solution**: Ensure backend running on port 5002, check `config.ts` for correct URL

**Issue**: Data received but field still undefined
**Solution**: Check backend logs - may be sending different field name case

**Issue**: Field visible but showing wrong value
**Solution**: Backend percentage calculation - should be already fixed

## Additional Validation

### Manual API Test:
```bash
curl -s http://localhost:5002/api/dashboard/overview | python3 -m json.tool | grep -A 3 -B 3 "previousClose"
```

**Expected**: Should see `"previousClose": 254.04` or `"PreviousClose": 254.04`

### Database Verification:
Check if previous close is being saved (optional):
```bash
# If needed - backend logs should show the data
```

## Files Changed

1. `frontend/mobile/src/context/PriceContext.tsx` - Enhanced field mapping + logging
2. `frontend/mobile/src/components/dashboard/AssetCard.tsx` - Added debug logging

## Backend Files (Already Correct - No Changes):
- `backend/MyTrader.Core/Models/MultiAssetPriceUpdate.cs` ✅
- `backend/MyTrader.Api/Services/MultiAssetDataBroadcastService.cs` ✅
- `backend/MyTrader.Services/Market/YahooFinanceProvider.cs` ✅

## Next Steps After Validation

1. **If Tests Pass**: User provides screenshot confirming "Önc:" field visible
2. **If Tests Fail**: Check specific failure scenario in logs, iterate on fix
3. **Final Commit**: Only after user confirms visual validation

## Notes

- **DO NOT TOUCH CRYPTO**: Binance/crypto functionality must remain unchanged
- **Percentage Calculation**: Already verified correct - using `(change / previousClose) * 100`
- **Field Case Handling**: Now robust - handles PascalCase and camelCase
