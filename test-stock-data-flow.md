# Stock Data Flow Validation Test

## Testing Instructions

After the fix has been applied, follow these steps to validate stock data is flowing correctly.

---

## Fix Applied

### File: `frontend/mobile/src/context/PriceContext.tsx`

**Changes:**
1. ✅ Added case-insensitive field name handling (Symbol vs symbol)
2. ✅ Enhanced logging to show all field names received
3. ✅ Added specific logging for STOCK asset class updates
4. ✅ Applied fixes to both `price_update` and `batch_price_update` handlers

**Key Improvements:**
```typescript
// Now handles both cases:
const rawSymbol = data.symbol || data.Symbol;
const rawAssetClass = data.assetClass || data.AssetClass || 'CRYPTO';
const rawPrice = data.price || data.Price;
```

---

## Validation Steps

### Step 1: Start Backend (if not already running)

```bash
cd backend/MyTrader.Api
dotnet run
```

**Expected output:**
```
Now listening on: http://192.168.68.102:5002
```

### Step 2: Verify Backend is Broadcasting Stock Prices

Watch backend logs for stock price broadcasts:

```bash
# Look for these log messages:
Broadcasting price update: STOCK AAPL = 258.06
Broadcasting price update: STOCK MSFT = 450.50
Successfully broadcasted price update for AAPL to X groups
```

✅ If you see these messages, backend is working correctly.

---

### Step 3: Start Mobile App

```bash
cd frontend/mobile
npm start
# Or
npx expo start
```

---

### Step 4: Open Developer Console

In Expo Developer Tools or Metro Bundler, open the console/logs.

---

### Step 5: Watch for Connection Messages

Look for these log messages in order:

```
1. [PriceContext] Initializing WebSocket connection...
2. SignalR connection established
3. [PriceContext] Connection status changed: connected
4. [PriceContext] Fetching symbols and subscribing...
5. [PriceContext] Loaded X crypto symbols: BTCUSDT, ETHUSDT, ...
6. [PriceContext] Loaded X stock symbols: AAPL, MSFT, GOOGL, ...
7. [PriceContext] Subscribing to CRYPTO symbols: [...]
8. [PriceContext] Successfully subscribed to CRYPTO price updates
9. [PriceContext] Subscribing to STOCK symbols: [...]
10. [PriceContext] Successfully subscribed to STOCK price updates
```

✅ If all these appear, WebSocket connection and subscription is working.

---

### Step 6: Watch for Price Update Messages

Within 60 seconds (backend polls every 60s), you should see:

```javascript
// For each stock price update:
[PriceContext] RAW price_update - All fields: ['Type', 'AssetClass', 'Symbol', 'Price', 'Change24h', 'Volume', ...]
[PriceContext] RAW price_update: {
  symbol: 'AAPL',
  assetClass: 'STOCK',
  price: 258.06,
  volume: 50000000,
  ...
}
[PriceContext] Normalized price_update: {
  symbolId: 'AAPL',
  symbol: 'AAPL',
  assetClass: 'STOCK',
  price: 258.06,
  ...
}
[PriceContext] ✅ Stock price updated: AAPL = $258.06
```

✅ If you see these messages with `assetClass: 'STOCK'`, price updates are being received!

---

### Step 7: Verify State Updates

Look for:

```
[PriceContext] enhancedPrices state updated: 15 items
[PriceContext] Sample prices: [
  { id: 'BTCUSDT', symbol: 'BTCUSDT', price: 45000 },
  { id: 'ETHUSDT', symbol: 'ETHUSDT', price: 3500 },
  { id: 'AAPL', symbol: 'AAPL', price: 258.06 }  // ✅ Stock data!
]
```

✅ If you see stock symbols (AAPL, MSFT, etc.) in the sample prices, state is being updated correctly!

---

### Step 8: Check Dashboard Screen

Look for these log messages from DashboardScreen:

```
[Dashboard] Loaded 10 total stock symbols
[Dashboard] Filtered stocks - BIST: 0, NASDAQ: 5, NYSE: 5
```

✅ If symbols are being filtered correctly, dashboard is processing stock data.

---

### Step 9: Visual Verification in App

1. Navigate to Dashboard screen in the mobile app
2. Look for these sections:
   - 🚀 Kripto (should show crypto prices)
   - 🏢 BIST Hisseleri (should show BIST stocks if available)
   - 🇺🇸 NASDAQ Hisseleri (should show NASDAQ stocks)
   - 🗽 NYSE Hisseleri (should show NYSE stocks)

3. **Expected Behavior:**
   - Sections expand/collapse when tapped
   - Stock symbols appear with names (e.g., "AAPL - Apple Inc.")
   - Prices are displayed (e.g., "$258.06")
   - Percent change is shown with color (green for positive, red for negative)
   - Volume is formatted (e.g., "50.0M")

4. **NO MORE "Veri yok" message** for stock sections with data!

---

## Troubleshooting

### Issue 1: No Stock Symbols Loaded

**Symptom:**
```
[PriceContext] Loaded 0 stock symbols:
```

**Fix:**
Check API response manually:
```bash
curl "http://192.168.68.102:5002/api/symbol-preferences/defaults?assetClass=STOCK" | jq '.'
```

Expected response should include `market` or `marketName` field for each symbol.

---

### Issue 2: Symbols Loaded But No Price Updates

**Symptom:**
```
[PriceContext] Loaded 10 stock symbols: AAPL, MSFT, ...
[PriceContext] Successfully subscribed to STOCK price updates
# But no "RAW price_update" messages for stocks
```

**Diagnosis:**
1. Check backend is broadcasting:
   ```bash
   # Backend logs should show:
   Broadcasting price update: STOCK AAPL = 258.06
   ```

2. If backend is broadcasting but frontend not receiving:
   - Verify SignalR hub connection is established
   - Check for WebSocket errors in console
   - Restart mobile app

---

### Issue 3: Symbols All Filtered Out (Empty Sections)

**Symptom:**
```
[Dashboard] Loaded 10 total stock symbols
[Dashboard] Filtered stocks - BIST: 0, NASDAQ: 0, NYSE: 0
```

**Diagnosis:**
Check if `marketName` field is populated:

Add temporary logging to DashboardScreen.tsx before line 205:
```typescript
console.log('Stock symbols with markets:', allStocks.map(s => ({
  symbol: s.symbol,
  marketName: s.marketName,
  market: (s as any).market
})));
```

**Fix:**
If `marketName` is undefined, the API response doesn't include the field. Contact backend team to add `market` or `marketName` field to symbol response.

---

### Issue 4: Field Names Still Not Matching

**Symptom:**
```
[PriceContext] RAW price_update - All fields: ['unknownField1', 'unknownField2', ...]
```

**Fix:**
1. Copy the exact field names from the log
2. Update PriceContext.tsx to handle those specific field names
3. Report field names to team for backend standardization

---

## Success Criteria

✅ **All checks passed if:**

1. Backend logs show: "Broadcasting price update: STOCK AAPL = $258.06"
2. Frontend logs show: "[PriceContext] Loaded 10 stock symbols"
3. Frontend logs show: "[PriceContext] ✅ Stock price updated: AAPL = $258.06"
4. Frontend logs show: "[PriceContext] enhancedPrices state updated: 15 items" (crypto + stocks)
5. Dashboard shows stock symbols with prices (NO "Veri yok")
6. Prices update automatically every 60 seconds

---

## Performance Monitoring

Expected update frequency:
- **Crypto**: Real-time (multiple updates per second via Binance WebSocket)
- **Stocks**: Every 60 seconds (Yahoo Finance polling)

Monitor for:
- Memory usage (should be stable)
- Console log volume (should be reasonable)
- UI responsiveness (should remain smooth)

---

## Next Steps After Validation

1. ✅ If stock data appears: **SUCCESS!** Fix is working.
2. ❌ If still not working: Collect logs and diagnostic info:
   - Full console log output
   - Backend log output
   - API response from `/api/symbol-preferences/defaults?assetClass=STOCK`
   - Screenshot of dashboard

3. Consider enabling database writes (currently disabled):
   - Add `asset_class` column to `market_data` table
   - Enable line 190 in `YahooFinancePollingService.cs`
   - This will persist stock prices to database

---

## Database Persistence (Optional Next Step)

Currently database writes are DISABLED. To enable:

### Migration Required:
```sql
ALTER TABLE market_data
ADD COLUMN asset_class VARCHAR(50) NOT NULL DEFAULT 'CRYPTO';

CREATE INDEX idx_market_data_asset_class ON market_data(asset_class);
```

### Enable Writes:
File: `backend/MyTrader.Services/Market/YahooFinancePollingService.cs` line 188-190

Change:
```csharp
// NOTE: Database save disabled
// await SaveToMarketDataTableAsync(priceUpdate, dbContext, cancellationToken);
```

To:
```csharp
await SaveToMarketDataTableAsync(priceUpdate, dbContext, cancellationToken);
```

---

## Contact

If issues persist after following this guide, provide:
1. Console log output (first 100 lines after app start)
2. Backend log output (grep for "STOCK")
3. API response from symbol endpoint
4. Mobile app screenshot

---

Generated: 2025-10-09
Test Duration: ~5 minutes
Expected Result: Stock data visible in mobile app
