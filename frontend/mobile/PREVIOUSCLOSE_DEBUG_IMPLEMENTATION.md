# Mobile App previousClose Field Investigation - Implementation Summary

## Problem Statement

The mobile React Native app is NOT receiving the `previousClose` field for stock data, despite:
- ✅ Backend IS sending it (confirmed by test HTML)
- ✅ Backend logs show previousClose in the legacyUpdate object
- ❌ Mobile PriceContext logs show: "MISSING previousClose" errors
- ❌ Mobile only receives: `["symbol", "price", "change", "volume", "timestamp", "assetClass"]`

## Investigation Approach

Added comprehensive diagnostic logging at THREE critical points in the data flow:

### 1. WebSocket Service Layer (ENTRY POINT)
**File**: `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/frontend/mobile/src/services/websocketService.ts`

**Changes**: Added detailed logging in `ReceivePriceUpdate` event handler (lines 200-229)

**What it logs**:
- RAW data type and structure BEFORE any processing
- Complete list of object keys
- Full JSON serialization of the data
- Explicit checks for previousClose field (case-insensitive)
- Comparison of RAW vs PARSED data

**Why it matters**:
- This is the FIRST point where mobile receives data from SignalR
- If previousClose is missing here, the problem is in:
  - Backend not sending it (unlikely - HTML test proves otherwise)
  - Mobile connecting to wrong hub/event
  - SignalR client library stripping the field

### 2. PriceContext Layer (BUSINESS LOGIC)
**File**: `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/frontend/mobile/src/context/PriceContext.tsx`

**Changes**: Enhanced logging in `price_update` event subscription (lines 123-158)

**What it logs**:
- Complete object structure received from WebSocket service
- All field names present in the data
- Extracted values with case-insensitive fallbacks
- Specific error when previousClose is missing for STOCK assetClass
- Success message when previousClose IS present

**Why it matters**:
- This is where the data enters the application state
- If previousClose is in WebSocket but missing here, the problem is in:
  - Event emission in WebSocket service
  - Data transformation between layers
  - TypeScript type mismatches

### 3. Normalization Layer (DATA TRANSFORMATION)
**File**: `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/frontend/mobile/src/utils/priceFormatting.ts`

**Changes**: Added debug logging in `normalizeMarketData` function (lines 168-173)

**What it logs**:
- INPUT previousClose value (what comes in)
- OUTPUT previousClose value (what goes out)
- Whether the value was undefined/null

**Why it matters**:
- This function transforms raw data into normalized format
- If previousClose is lost here, the problem is in:
  - Normalization logic (lines 164-166)
  - Type conversion issues
  - Conditional logic incorrectly setting to undefined

## Expected Debug Output

### Successful Flow (Stock with previousClose)

```javascript
// 1. RAW WebSocket Data
[WebSocketService] ======= RAW ReceivePriceUpdate =======
[WebSocketService] RAW data keys: ["symbol", "price", "change", "change24h", "previousClose", "volume", "timestamp", "assetClass"]
[WebSocketService] Has previousClose?: true
[WebSocketService] previousClose value: 230.42

// 2. Parsed Data (after safeParseMessageData)
[WebSocketService] PARSED previousClose: 230.42

// 3. PriceContext Receipt
[PriceContext] ======= RECEIVED FROM WEBSOCKET SERVICE =======
[PriceContext] ✅ STOCK WITH previousClose: JPM = 230.42

// 4. Normalization
[priceFormatting.normalizeMarketData] INPUT previousClose: 230.42
[priceFormatting.normalizeMarketData] OUTPUT previousClose: 230.42
```

### Failed Flow (previousClose Missing)

The logs will show EXACTLY where the field disappears:

**Scenario A**: Missing from RAW data
```javascript
[WebSocketService] RAW data keys: ["symbol", "price", "change", "volume", "timestamp", "assetClass"]
[WebSocketService] Has previousClose?: false
// ROOT CAUSE: Backend not sending OR mobile not subscribed correctly
```

**Scenario B**: Lost during parsing
```javascript
[WebSocketService] RAW previousClose value: 230.42
[WebSocketService] PARSED previousClose: undefined
// ROOT CAUSE: safeParseMessageData() function issue
```

**Scenario C**: Lost in PriceContext
```javascript
[WebSocketService] PARSED previousClose: 230.42
[PriceContext] ❌ STOCK WITHOUT previousClose: JPM
// ROOT CAUSE: Event emission or data access issue
```

**Scenario D**: Lost during normalization
```javascript
[PriceContext] ✅ STOCK WITH previousClose: JPM = 230.42
[priceFormatting.normalizeMarketData] INPUT previousClose: 230.42
[priceFormatting.normalizeMarketData] OUTPUT previousClose: undefined
// ROOT CAUSE: normalizeMarketData() logic issue
```

## How to Test

### 1. Start Backend
```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/backend/MyTrader.Api
dotnet run
```

### 2. Start Mobile App
```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/frontend/mobile
npm start
```

Then open in iOS Simulator or Android Emulator.

### 3. Monitor Console Output

Look for the debug sections marked with `=======`. For each stock price update (JPM, AAPL, MSFT, etc.), you should see:
- RAW data including previousClose
- PARSED data preserving previousClose
- PriceContext receiving previousClose
- Normalization preserving previousClose

### 4. Compare with Test HTML

Open the test HTML file in browser and compare:
```
file:///Users/mustafayildirim/Documents/Personal%20Documents/Projects/myTrader/test-stock-websocket-fields.html
```

If HTML receives previousClose but mobile doesn't, the issue is definitively in the mobile code.

## Root Cause Hypotheses

### Hypothesis 1: Different Event Subscription ⚠️ MOST LIKELY
**Evidence**:
- Test HTML subscribes to "ReceivePriceUpdate" on `/dashboardHub`
- Mobile also subscribes to "ReceivePriceUpdate" via websocketService
- BUT mobile might be connecting to a different hub URL

**Check**:
- Compare mobile hub URL in config vs test HTML
- Verify mobile is joining the correct SignalR groups
- Check if mobile is subscribed to STOCK asset class

**Debug Logs to Check**:
```javascript
[WebSocketService] RAW data keys: [...]
```
If this shows previousClose, hypothesis is FALSE.
If this is missing previousClose, hypothesis is TRUE.

### Hypothesis 2: safeParseMessageData() Stripping Field
**Evidence**:
- This function parses JSON and might be selective about fields
- Located in websocketService.ts lines 609-643

**Check**:
- Compare RAW vs PARSED logs
- Check if JSON.parse is removing fields

**Debug Logs to Check**:
```javascript
[WebSocketService] RAW previousClose value: 230.42
[WebSocketService] PARSED previousClose: undefined
```
If values differ, hypothesis is TRUE.

### Hypothesis 3: Case Sensitivity Issue
**Evidence**:
- Backend sends `previousClose` (lowercase)
- Mobile code has fallbacks for `PreviousClose` (uppercase)

**Status**: UNLIKELY - code already handles both cases

**Code Evidence**:
```typescript
const rawPreviousClose = data.previousClose ?? data.PreviousClose ?? data.prevClose ?? data.PrevClose;
```

### Hypothesis 4: Normalization Function Issue
**Evidence**:
- normalizeMarketData() might be converting valid values to undefined
- Condition: `(data.previousClose !== undefined && data.previousClose !== null)`

**Check**:
- If previousClose is `0`, this condition might fail
- If previousClose is `NaN`, it might be normalized to undefined

**Debug Logs to Check**:
```javascript
[priceFormatting.normalizeMarketData] INPUT previousClose: 230.42
[priceFormatting.normalizeMarketData] OUTPUT previousClose: undefined
```

## Files Modified

1. `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/frontend/mobile/src/services/websocketService.ts`
   - Added RAW and PARSED data logging in ReceivePriceUpdate handler
   - Lines 200-229

2. `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/frontend/mobile/src/context/PriceContext.tsx`
   - Enhanced price_update event logging
   - Added detailed field checks and error reporting
   - Lines 123-158

3. `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/frontend/mobile/src/utils/priceFormatting.ts`
   - Added input/output logging in normalizeMarketData
   - Lines 168-173

## Next Steps

1. **Run the mobile app** and capture the console logs
2. **Identify the exact point** where previousClose is lost (use the scenarios above)
3. **Implement the fix** based on the root cause identified
4. **Verify the fix** by checking that:
   - Stock symbols show previousClose in logs
   - AssetCard displays "Önceki Kapanış" field
   - Percent change calculations are accurate
5. **Remove debug logging** once the issue is resolved (keep in git history for reference)

## Success Criteria

✅ All stock price updates show: `[PriceContext] ✅ STOCK WITH previousClose: {SYMBOL} = {VALUE}`

✅ No errors: `[PriceContext] ❌ STOCK WITHOUT previousClose`

✅ AssetCard components display previousClose field for all stocks

✅ Percent change calculations match backend values

## Important Notes

- **DO NOT** modify backend code - it's confirmed working
- **DO NOT** modify crypto/Binance code - only fix stock data
- Focus **ONLY** on the mobile React Native app
- All debug logging is wrapped in `if (__DEV__)` - won't affect production builds
- The comprehensive logging will generate a lot of console output - this is intentional
- User has been working on this for hours - find root cause QUICKLY with these logs

## User Feedback Expected

After running the app with these debug logs, the user should provide:
- Console output for at least one stock symbol (e.g., JPM)
- Whether previousClose appears in RAW, PARSED, PriceContext, and normalization logs
- Any error messages or unexpected values

With this information, the exact root cause will be identified and fixed immediately.
