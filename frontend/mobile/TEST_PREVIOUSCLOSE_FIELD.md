# Testing Instructions: previousClose Field Debug

## Quick Start

```bash
# Terminal 1: Start Backend
cd backend/MyTrader.Api
dotnet run

# Terminal 2: Start Mobile App
cd frontend/mobile
npm start
# Then press 'i' for iOS simulator or 'a' for Android emulator
```

## What to Look For

When the app starts and connects to the backend, you'll see EXTENSIVE debug logging. Look for these patterns:

### 1. WebSocket Connection (First Thing You'll See)

```
[WebSocketService] Creating SignalR connection to: http://...
[WebSocketService] SignalR connection established
[PriceContext] Connection status changed: connected
```

### 2. Symbol Subscription

```
[PriceContext] Subscribing to STOCK symbols: ["JPM", "AAPL", "MSFT", ...]
[PriceContext] Successfully subscribed to STOCK price updates
```

### 3. Price Updates (The Critical Part)

For EVERY stock price update, you should see this sequence:

```
[WebSocketService] ======= RAW ReceivePriceUpdate =======
[WebSocketService] RAW data type: object
[WebSocketService] RAW data keys: ["symbol", "price", "change", "change24h", "previousClose", "volume", "timestamp", "assetClass"]
                                                                          ^^^^^^^^^^^^^^^^
                                                                          THIS IS THE KEY FIELD!
[WebSocketService] RAW data: {
  "symbol": "JPM",
  "price": 229.55,
  "change": -0.87,
  "change24h": -0.87,
  "previousClose": 230.42,    <--- SHOULD BE HERE
  "volume": 1234567,
  "timestamp": "2025-10-10T...",
  "assetClass": "STOCK"
}
[WebSocketService] Has previousClose?: true    <--- SHOULD BE TRUE
[WebSocketService] previousClose value: 230.42 <--- SHOULD HAVE A NUMBER

[PriceContext] ======= RECEIVED FROM WEBSOCKET SERVICE =======
[PriceContext] ✅ STOCK WITH previousClose: JPM = 230.42  <--- SUCCESS MESSAGE
```

## What Each Scenario Means

### ✅ GOOD - previousClose Present Throughout

```
[WebSocketService] previousClose value: 230.42
[WebSocketService] PARSED previousClose: 230.42
[PriceContext] ✅ STOCK WITH previousClose: JPM = 230.42
[priceFormatting.normalizeMarketData] INPUT previousClose: 230.42
[priceFormatting.normalizeMarketData] OUTPUT previousClose: 230.42
```

**Meaning**: Everything is working! The field is being transmitted and preserved correctly.

**Action**: The issue was already fixed, or it only happens under specific conditions. Try refreshing the app or waiting for more updates.

---

### ❌ BAD - Missing from RAW WebSocket Data

```
[WebSocketService] RAW data keys: ["symbol", "price", "change", "volume", "timestamp", "assetClass"]
                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                    NO previousClose IN THE LIST!

[WebSocketService] Has previousClose?: false
[WebSocketService] previousClose value: undefined
```

**Meaning**: The mobile app is NOT receiving previousClose from the backend at all.

**Root Cause**: One of these:
1. Mobile is connecting to the wrong hub URL
2. Mobile is not subscribed to the correct asset class or symbols
3. Backend is sending to different event name than mobile is listening to

**Action**: Check the hub URL and subscription in the console output. Look for:
```
[WebSocketService] Creating SignalR connection to: <WHAT URL?>
[PriceContext] Subscribing to STOCK symbols: <WHICH SYMBOLS?>
```

---

### ❌ BAD - Lost During Parsing

```
[WebSocketService] RAW previousClose value: 230.42
[WebSocketService] PARSED previousClose: undefined   <--- LOST HERE!
```

**Meaning**: The data arrives correctly but is stripped during JSON parsing.

**Root Cause**: The `safeParseMessageData()` function is incorrectly processing the data.

**Action**: This is a code bug in websocketService.ts that needs to be fixed.

---

### ❌ BAD - Lost in PriceContext

```
[WebSocketService] PARSED previousClose: 230.42
[PriceContext] ❌ STOCK WITHOUT previousClose: JPM   <--- LOST HERE!
[PriceContext] Field check: {
  'previousClose' in data: false,  <--- WHY IS THIS FALSE?
}
```

**Meaning**: The WebSocket service has the data but it's not reaching PriceContext.

**Root Cause**: Event emission issue or data access problem between layers.

**Action**: This is a code bug in how events are emitted/received.

---

### ❌ BAD - Lost During Normalization

```
[PriceContext] ✅ STOCK WITH previousClose: JPM = 230.42
[priceFormatting.normalizeMarketData] INPUT previousClose: 230.42
[priceFormatting.normalizeMarketData] OUTPUT previousClose: undefined  <--- LOST HERE!
```

**Meaning**: The data reaches normalization but is converted to undefined.

**Root Cause**: Bug in the normalizeMarketData function logic.

**Action**: This is a code bug in priceFormatting.ts.

---

## Information to Provide

After running the app, please provide:

1. **Connection Info**: What hub URL is the app connecting to?
   ```
   [WebSocketService] Creating SignalR connection to: <COPY THIS>
   ```

2. **Subscription Info**: Which symbols is it subscribing to?
   ```
   [PriceContext] Subscribing to STOCK symbols: <COPY THIS>
   ```

3. **ONE Complete Stock Update Log**: Copy the ENTIRE debug output for ONE stock symbol (JPM, AAPL, etc.), from:
   ```
   [WebSocketService] ======= RAW ReceivePriceUpdate =======
   ```
   Through to:
   ```
   [priceFormatting.normalizeMarketData] OUTPUT previousClose: ...
   ```

4. **Error Count**: How many times do you see this error?
   ```
   [PriceContext] ❌ STOCK WITHOUT previousClose
   ```

## Quick Diagnosis

Based on what you see, here's the quick diagnosis guide:

| What You See | Root Cause | Fix Needed |
|--------------|------------|------------|
| `Has previousClose?: false` in RAW data | Not received from backend | Check hub URL and subscription |
| `RAW: 230.42` but `PARSED: undefined` | Parsing bug | Fix safeParseMessageData() |
| `PARSED: 230.42` but `❌ STOCK WITHOUT` | Event emission bug | Fix event handling |
| `INPUT: 230.42` but `OUTPUT: undefined` | Normalization bug | Fix normalizeMarketData() |
| `✅ STOCK WITH previousClose` for all | WORKING! | Issue was already fixed |

## Expected Timeline

- App startup: ~5 seconds
- First subscription: ~2 seconds after connection
- First stock update: ~10 seconds (depending on market hours and Yahoo Finance polling)

**Note**: If markets are closed, stock updates might be delayed or infrequent. You can verify the backend is sending updates by checking backend console output.

## Fallback: Check Backend Logs

If mobile logs are unclear, also check the backend console for:

```
📊 Stock Update: JPM - Price: $229.55, PreviousClose: $230.42, Change%: -0.38%, Source: YAHOO
Broadcasting price update: STOCK JPM = 229.55
Successfully broadcasted price update for JPM to X groups
```

This confirms the backend IS sending the data.

## After Testing

Once you provide the console output, I will:
1. Identify the EXACT line where previousClose is lost
2. Implement the specific fix needed
3. Verify the fix resolves the issue
4. Remove the debug logging

The comprehensive logging ensures we find the root cause in ONE test run instead of hours of guessing.
