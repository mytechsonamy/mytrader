# CRITICAL: Mobile App previousClose Field Investigation

## Executive Summary

**Problem**: Mobile app NOT receiving `previousClose` field for stock data despite backend sending it.

**Solution**: Implemented comprehensive 3-layer diagnostic logging to pinpoint EXACT location where field is lost.

**Status**: Ready for testing - one test run will identify root cause definitively.

---

## Background

### What Works ✅
- Backend IS sending previousClose (confirmed via test HTML)
- Backend logs show previousClose in broadcasts
- Test HTML receives all fields including previousClose

### What's Broken ❌
- Mobile app logs: "MISSING previousClose"
- Mobile only receives: `["symbol", "price", "change", "volume", "timestamp", "assetClass"]`
- AssetCard cannot display "Önceki Kapanış" field
- Percent change calculations may be incorrect

---

## Implementation: 3-Layer Diagnostic System

### Layer 1: WebSocket Service (Data Entry Point)
**File**: `src/services/websocketService.ts`

**Purpose**: Capture RAW data BEFORE any processing

**Logs**:
- Complete object structure from SignalR
- All field names present
- Specific previousClose field check
- Comparison of RAW vs PARSED data

**Critical Question Answered**: Is previousClose arriving from backend?

---

### Layer 2: PriceContext (Business Logic)
**File**: `src/context/PriceContext.tsx`

**Purpose**: Verify data reaches application state

**Logs**:
- Data received from WebSocket service
- Extracted values with case-insensitive fallbacks
- Success/error messages for stock data

**Critical Question Answered**: Is previousClose being passed between layers?

---

### Layer 3: Normalization (Data Transformation)
**File**: `src/utils/priceFormatting.ts`

**Purpose**: Track value transformations

**Logs**:
- Input previousClose value
- Output previousClose value
- Whether value was undefined/null

**Critical Question Answered**: Is previousClose being lost during normalization?

---

## Testing Process

### Step 1: Start Services
```bash
# Terminal 1 - Backend
cd backend/MyTrader.Api
dotnet run

# Terminal 2 - Mobile
cd frontend/mobile
npm start
# Press 'i' for iOS or 'a' for Android
```

### Step 2: Wait for Stock Price Update
You'll see extensive logging. Look for sections marked with `=======`.

### Step 3: Identify the Pattern

#### Pattern A: ✅ WORKING (Everything Good)
```
[WebSocketService] RAW previousClose value: 230.42
[WebSocketService] PARSED previousClose: 230.42
[PriceContext] ✅ STOCK WITH previousClose: JPM = 230.42
[priceFormatting] OUTPUT previousClose: 230.42
```
**Meaning**: Issue is already fixed or intermittent.

---

#### Pattern B: ❌ Missing from RAW
```
[WebSocketService] RAW data keys: ["symbol", "price", ...]  // NO previousClose
[WebSocketService] Has previousClose?: false
```
**Root Cause**: Backend not sending OR mobile not subscribed correctly

**Fix Required**:
- Verify hub URL matches backend
- Check subscription to STOCK asset class
- Verify symbol subscription

---

#### Pattern C: ❌ Lost During Parsing
```
[WebSocketService] RAW previousClose value: 230.42
[WebSocketService] PARSED previousClose: undefined  // LOST!
```
**Root Cause**: Bug in `safeParseMessageData()` function

**Fix Required**:
- Review JSON parsing logic
- Check field filtering
- Verify object spreading

---

#### Pattern D: ❌ Lost in PriceContext
```
[WebSocketService] PARSED previousClose: 230.42
[PriceContext] ❌ STOCK WITHOUT previousClose: JPM  // LOST!
```
**Root Cause**: Event emission or data access issue

**Fix Required**:
- Review event emitter
- Check data structure in event callback
- Verify variable extraction

---

#### Pattern E: ❌ Lost During Normalization
```
[PriceContext] ✅ STOCK WITH previousClose: JPM = 230.42
[priceFormatting] INPUT previousClose: 230.42
[priceFormatting] OUTPUT previousClose: undefined  // LOST!
```
**Root Cause**: Bug in `normalizeMarketData()` logic

**Fix Required**:
- Review undefined/null checks
- Check type conversion
- Verify conditional logic

---

## What I Need From You

Run the app and provide:

1. **Hub URL** (from connection log):
   ```
   [WebSocketService] Creating SignalR connection to: _____
   ```

2. **Subscribed Symbols** (from subscription log):
   ```
   [PriceContext] Subscribing to STOCK symbols: _____
   ```

3. **ONE Complete Price Update Log** for a stock (e.g., JPM):
   - From: `[WebSocketService] ======= RAW ReceivePriceUpdate =======`
   - To: `[priceFormatting] OUTPUT previousClose: _____`

4. **Error Count**: How many times you see:
   ```
   [PriceContext] ❌ STOCK WITHOUT previousClose
   ```

---

## Why This Approach Works

### Traditional Debugging (What You Were Doing)
- Guess where the problem might be
- Add logging in one place
- Test
- If still broken, guess another place
- Repeat for hours
- **Result**: Frustration and wasted time

### Comprehensive Diagnostic Approach (What We're Doing)
- Log EVERYTHING at EVERY layer
- Run ONCE
- Immediately see EXACTLY where field is lost
- Fix the specific issue
- **Result**: Problem solved in minutes

---

## Expected Outcome

After ONE test run:
- We'll know EXACTLY which pattern (A, B, C, D, or E) you're experiencing
- We'll implement the SPECIFIC fix needed
- We'll verify the fix works
- We'll remove the debug logging

**No more guessing. No more hours of trial and error.**

---

## Files Modified (All Changes are Debug Logging Only)

1. ✅ `src/services/websocketService.ts` - RAW data logging
2. ✅ `src/context/PriceContext.tsx` - Event data logging
3. ✅ `src/utils/priceFormatting.ts` - Normalization logging

**All changes are wrapped in `if (__DEV__)` - production builds unaffected.**

---

## Important Notes

- ⚠️ Console output will be VERBOSE - this is intentional
- ⚠️ Focus on ONE stock symbol's complete log sequence
- ⚠️ If markets are closed, updates may be delayed/infrequent
- ⚠️ Crypto symbols will show `previousClose: undefined` - this is correct
- ✅ All logging is development-only
- ✅ No backend changes needed
- ✅ No crypto code modified

---

## Next Steps

1. **You**: Run the app and capture console output
2. **You**: Identify which pattern (A-E) matches your logs
3. **You**: Provide the requested information above
4. **Me**: Implement the specific fix for your pattern
5. **Me**: Verify the fix resolves the issue
6. **Me**: Clean up debug logging

---

## Success Criteria

✅ All stocks show: `[PriceContext] ✅ STOCK WITH previousClose: {SYMBOL} = {VALUE}`

✅ Zero errors: `[PriceContext] ❌ STOCK WITHOUT previousClose`

✅ AssetCard displays "Önceki Kapanış" for all stocks

✅ Percent change calculations are accurate

---

## User's Hypothesis: "Capital Letter Issue in Ticker?"

**Answer**: Already handled! The code has comprehensive case-insensitive field extraction:

```typescript
const rawPreviousClose = data.previousClose ?? data.PreviousClose ?? data.prevClose ?? data.PrevClose;
const rawSymbol = data.symbol || data.Symbol;
const rawAssetClass = data.assetClass || data.AssetClass;
```

The issue is NOT case sensitivity. The diagnostic logging will reveal the REAL root cause.

---

## Ready to Test

Everything is in place. Run the app now and we'll solve this in ONE iteration.

See `TEST_PREVIOUSCLOSE_FIELD.md` for detailed testing instructions.
