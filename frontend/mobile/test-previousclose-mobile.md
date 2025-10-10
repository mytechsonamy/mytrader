# Mobile App previousClose Field Debug Test

## Purpose
Diagnose why mobile app is not receiving `previousClose` field for stock data even though backend is sending it.

## Changes Made

### 1. WebSocket Service Debug Logging (websocketService.ts)
Added comprehensive RAW data logging in the `ReceivePriceUpdate` event handler:
- Logs RAW data type and structure BEFORE any processing
- Logs all object keys
- Logs full JSON dump of the data
- Specifically checks for `previousClose` and `PreviousClose` fields
- Compares RAW vs PARSED data

### 2. PriceContext Debug Logging (PriceContext.tsx)
Enhanced logging in the `price_update` event handler:
- Logs complete object structure received from WebSocket service
- Logs extracted values with case-insensitive field checks
- Specific error logging when previousClose is missing for stocks
- Success logging when previousClose IS present for stocks

### 3. Normalization Function Debug (priceFormatting.ts)
Added logging to track data transformation:
- Logs INPUT previousClose value
- Logs OUTPUT previousClose value after normalization
- Tracks if value was undefined/null

## Expected Debug Output Flow

When a stock price update is received, you should see:

```
[WebSocketService] ======= RAW ReceivePriceUpdate =======
[WebSocketService] RAW data type: object
[WebSocketService] RAW data keys: ["symbol", "price", "change", "change24h", "previousClose", "volume", "timestamp", "assetClass"]
[WebSocketService] RAW data: {
  "symbol": "JPM",
  "price": 229.55,
  "change": -0.87,
  "change24h": -0.87,
  "previousClose": 230.42,
  "volume": 1234567,
  "timestamp": "2025-10-10T...",
  "assetClass": "STOCK"
}
[WebSocketService] Has previousClose?: true
[WebSocketService] Has PreviousClose?: false
[WebSocketService] previousClose value: 230.42
[WebSocketService] PreviousClose value: undefined
[WebSocketService] ==========================================
[WebSocketService] PARSED data keys: ["symbol", "price", "change", "change24h", "previousClose", "volume", "timestamp", "assetClass"]
[WebSocketService] PARSED previousClose: 230.42
[WebSocketService] PARSED PreviousClose: undefined

[PriceContext] ======= RECEIVED FROM WEBSOCKET SERVICE =======
[PriceContext] RAW price_update - All fields: ["symbol", "price", "change", "change24h", "previousClose", "volume", "timestamp", "assetClass"]
[PriceContext] RAW price_update FULL OBJECT: { ... }
[PriceContext] Extracted values: {
  symbol: "JPM",
  assetClass: "STOCK",
  price: 229.55,
  previousClose: 230.42,
  ...
}
[PriceContext] ✅ STOCK WITH previousClose: JPM = 230.42
[PriceContext] ================================================

[priceFormatting.normalizeMarketData] INPUT previousClose: 230.42
[priceFormatting.normalizeMarketData] OUTPUT previousClose: 230.42
[priceFormatting.normalizeMarketData] Was undefined/null?: false
```

## What to Look For

### If previousClose IS in RAW data but MISSING in PriceContext:
- **Problem**: WebSocket service is filtering/stripping the field
- **Location**: Check `safeParseMessageData()` function in websocketService.ts

### If previousClose is MISSING in RAW data:
- **Problem**: Backend is not sending it OR mobile is connecting to wrong hub/event
- **Check**:
  - Backend logs confirming the legacyUpdate object includes previousClose
  - Mobile is connecting to correct hub URL (should be /hubs/dashboard or /dashboardHub)
  - Mobile is subscribed to correct asset class

### If previousClose is LOST during normalization:
- **Problem**: The normalizeMarketData function is converting it to undefined
- **Check**: The condition at line 164-166 of priceFormatting.ts

## Testing Instructions

1. **Start Backend**:
   ```bash
   cd backend/MyTrader.Api
   dotnet run
   ```

2. **Start Mobile App**:
   ```bash
   cd frontend/mobile
   npm start
   # Then open in iOS simulator or Android emulator
   ```

3. **Watch Console Output**:
   - Look for the debug output sections marked with `=======`
   - For stocks (JPM, AAPL, MSFT), you should see previousClose values
   - For crypto (BTCUSDT, ETHUSDT), previousClose will be undefined (expected)

4. **Compare with Test HTML**:
   - Open `test-stock-websocket-fields.html` in browser
   - Verify it receives the same fields
   - If HTML receives previousClose but mobile doesn't, the problem is in mobile code

## Root Cause Hypotheses

### Hypothesis 1: Case Sensitivity in Field Names
- Backend sends `previousClose` (lowercase)
- Mobile might be looking for `PreviousClose` (uppercase)
- **Status**: Code already handles this with fallback checks
- **Unlikely**: Case-insensitive extraction is already implemented

### Hypothesis 2: Different Hub/Event Subscription
- Test HTML connects to specific hub and event
- Mobile might be connecting to different hub or listening to different event
- **Status**: TO BE VERIFIED by debug logs
- **Check**: Compare hub URL and event names

### Hypothesis 3: Data Transformation in WebSocket Service
- `safeParseMessageData()` might be stripping fields
- JSON.parse might be losing data
- **Status**: TO BE VERIFIED by RAW vs PARSED comparison

### Hypothesis 4: Stock Data Not Being Sent
- Backend might only be sending crypto data
- Stock symbols might not be properly subscribed
- **Status**: Backend logs show stock data IS being sent
- **Unlikely**: Test HTML confirms backend is working

## Next Steps After Testing

1. Review the debug logs to identify exactly where previousClose is lost
2. If found in RAW but lost in PARSED: Fix `safeParseMessageData()`
3. If not in RAW: Check subscription and hub connection
4. If lost in normalization: Fix `normalizeMarketData()`
5. Remove debug logging after fix is confirmed

## Success Criteria

- Mobile app logs show `✅ STOCK WITH previousClose` for all stock symbols
- AssetCard components display "Önceki Kapanış" field with correct values
- No `❌ STOCK WITHOUT previousClose` errors in console
- Stock percent change calculations are accurate
