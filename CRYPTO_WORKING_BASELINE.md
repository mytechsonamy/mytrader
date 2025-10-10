# CRYPTO WORKING BASELINE - DO NOT BREAK THIS FUNCTIONALITY

**Generated**: 2025-10-09
**Purpose**: Document working crypto accordion functionality as regression baseline before fixing stock data flow

---

## EXECUTIVE SUMMARY

**CRYPTO ACCORDIONS ARE WORKING CORRECTLY**
**STOCK ACCORDIONS ARE NOT RECEIVING DATA**

### Current Status
- Crypto symbols: **9 symbols** (BTC, ETH, BNB, SOL, XRP, AVAX, ENA, UNI, SUI)
- Crypto WebSocket: **Connected and broadcasting price updates**
- Crypto UI: **Accordion displays prices correctly**
- Stock symbols: **3 symbols loaded** (GARAN, SISE, THYAO)
- Stock WebSocket: **Subscribed but NO price updates received**
- Stock UI: **Accordion shows "No data" / loading state**

---

## 1. BACKEND API - CRYPTO SYMBOLS (WORKING)

### Endpoint Test Results

```bash
# ✅ WORKING: Crypto symbols API
curl -s "http://192.168.68.102:8080/api/symbols?exchange=BINANCE" | jq '.'
```

**Response Structure**:
```json
{
  "symbols": {
    "BTC": {
      "symbol": "BTCUSDT",
      "display_name": "Bitcoin",
      "venue": "BINANCE",
      "market": "BINANCE",
      "marketName": "BINANCE",
      "baseCurrency": "BTC",
      "quoteCurrency": "USDT",
      "precision": 8
    },
    // ... 8 more crypto symbols
  },
  "interval": "1m"
}
```

**Key Observation**: Crypto symbols return with:
- `symbol`: "BTCUSDT" (includes USDT suffix)
- `marketName`: "BINANCE"
- `market`: "BINANCE"

### Stock Symbols API (Also Works)

```bash
# ✅ WORKING: Stock symbols API
curl -s "http://192.168.68.102:8080/api/symbols?exchange=BIST" | jq '.'
```

**Response Structure**:
```json
{
  "symbols": {
    "GARAN": {
      "symbol": "GARAN",
      "display_name": "Garanti BBVA",
      "venue": "BIST",
      "market": "BIST",
      "marketName": "BIST",
      "baseCurrency": "GARAN",
      "quoteCurrency": "TRY",
      "precision": 2
    },
    // ... 2 more stock symbols
  },
  "interval": "1m"
}
```

**Key Observation**: Stock symbols return with:
- `symbol`: "GARAN" (NO suffix)
- `marketName`: "BIST"
- `market`: "BIST"

---

## 2. BACKEND WEBSOCKET - CRYPTO PRICE UPDATES (WORKING)

### WebSocket Connection Status

```bash
docker logs mytrader_api --tail 200 | grep -i "binance\|websocket" | tail -20
```

**Evidence of Working Crypto WebSocket**:
```log
[14:34:18] Successfully connected to WebSocket wss://stream.binance.com:9443/stream?streams=bnbusdt@ticker/...
[14:34:18] Binance WebSocket connection status changed: Connected (Connected: True)
[14:34:19] [RAW BINANCE DATA] Symbol=BTCUSDT, c=121420.64000000, P=-0.929
[14:34:19] Received ticker data for BTCUSDT: Price=121420.64000000, Change=-0.929%
[14:34:19] [RAW BINANCE DATA] Symbol=ETHUSDT, c=4317.89000000, P=-3.711
[14:34:20] Received ticker data for ETHUSDT: Price=4318.74000000, Change=-3.689%
```

**Crypto Symbols Being Broadcast**:
- BTCUSDT, ETHUSDT, BNBUSDT, SOLUSDT, XRPUSDT, AVAXUSDT, ENAUSDT, UNIUSDT, SUIUSDT

**Broadcast Frequency**: Real-time (sub-second updates)

**Key Characteristics**:
- Binance WebSocket: `wss://stream.binance.com:9443/stream`
- Symbol format: `btcusdt@ticker` (lowercase with USDT suffix)
- Price field: `c` (close price)
- Change field: `P` (percent change)
- Update type: Individual ticker events per symbol

---

## 3. MOBILE APP - CRYPTO DATA FLOW (WORKING)

### Symbol Loading Logic

**File**: `frontend/mobile/src/screens/DashboardScreen.tsx`

```typescript
// Lines 220-229: Crypto symbols are fetched
const cryptoSymbolsResult = await apiService.getSymbolsByAssetClass('CRYPTO');
const cryptoSymbols = cryptoSymbolsResult.status === 'fulfilled'
  ? cryptoSymbolsResult.value
  : [];

console.log(`[Dashboard] Loaded ${cryptoSymbols.length} crypto symbols:`,
  cryptoSymbols.map(s => s.symbol).join(', '));
```

**Expected Log Output**:
```
[Dashboard] Loaded 9 crypto symbols: BTCUSDT, ETHUSDT, BNBUSDT, SOLUSDT, XRPUSDT, AVAXUSDT, ENAUSDT, UNIUSDT, SUIUSDT
```

### API Service - Symbol Fetching

**File**: `frontend/mobile/src/services/multiAssetApi.ts`

```typescript
// Lines 519-563: getSymbolsByAssetClass
async getSymbolsByAssetClass(assetClass: string): Promise<EnhancedSymbolDto[]> {
  const operation = async () => fetch(
    `${API_BASE_URL}/symbol-preferences/defaults?assetClass=${encodeURIComponent(assetClass)}`,
    { headers: await this.getHeaders() }
  );

  const response = await this.withRetry<any>(operation, 'getSymbolsByAssetClass');
  const symbols = response.symbols || response;

  // Backend returns 'market' but frontend expects 'marketName'
  const transformedSymbols = symbols.map((symbol: any) => ({
    ...symbol,
    marketName: symbol.marketName || symbol.market, // ✅ Handles both field names
  }));

  return transformedSymbols;
}
```

**Key Points**:
- ✅ Fetches from `/symbol-preferences/defaults?assetClass=CRYPTO`
- ✅ Handles both `market` and `marketName` fields
- ✅ Returns 9 crypto symbols successfully

### WebSocket Subscription Logic

**File**: `frontend/mobile/src/context/PriceContext.tsx`

```typescript
// Lines 316-338: Subscribe to CRYPTO after connection
const cryptoSymbols = await multiAssetApiService.getSymbolsByAssetClass('CRYPTO');
console.log(`[PriceContext] Loaded ${cryptoSymbols.length} crypto symbols`);

const cryptoSymbolNames = cryptoSymbols.map(s => s.symbol);
if (cryptoSymbolNames.length > 0) {
  console.log('[PriceContext] Subscribing to CRYPTO symbols:', cryptoSymbolNames);
  await websocketService.subscribeToAssetClass('CRYPTO', cryptoSymbolNames);
  console.log('[PriceContext] Successfully subscribed to CRYPTO price updates');
}
```

**File**: `frontend/mobile/src/services/websocketService.ts`

```typescript
// Lines 709-723: Direct subscription method
async subscribeToAssetClass(assetClass: string, symbols: string[]): Promise<void> {
  if (this.connection && this.connectionState.isConnected) {
    console.log(`[WebSocket] Subscribing to ${assetClass} with symbols:`, symbols);
    await this.connection.invoke('SubscribeToPriceUpdates', assetClass, symbols);
    console.log(`[WebSocket] Successfully subscribed to ${assetClass} price updates`);
  }
}
```

**Expected Subscription Call**:
```javascript
connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', [
  'BTCUSDT', 'ETHUSDT', 'BNBUSDT', 'SOLUSDT', 'XRPUSDT',
  'AVAXUSDT', 'ENAUSDT', 'UNIUSDT', 'SUIUSDT'
])
```

### Price Update Handler

**File**: `frontend/mobile/src/context/PriceContext.tsx`

```typescript
// Lines 123-219: price_update event handler
websocketService.on('price_update', (data: any) => {
  // Handle both uppercase and lowercase field names from backend
  const rawSymbol = data.symbol || data.Symbol;
  const rawAssetClass = data.assetClass || data.AssetClass || 'CRYPTO';
  const rawPrice = data.price || data.Price;

  // Normalize price data
  const priceNormalized = normalizeMarketData({
    price: rawPrice,
    volume: rawVolume,
    change: rawChange,
    // ... other fields
  }, rawAssetClass);

  const normalizedData: UnifiedMarketDataDto = {
    symbolId: data.symbolId || data.id || rawSymbol,
    symbol: rawSymbol,
    assetClass: rawAssetClass as AssetClassType,
    price: priceNormalized.price,
    // ... other fields
  };

  setEnhancedPrices(prev => ({
    ...prev,
    [normalizedData.symbolId]: normalizedData,
    [normalizedData.symbol]: normalizedData  // ✅ Indexed by both ID and symbol
  }));
});
```

**Key Points**:
- ✅ Handles both `symbol` and `Symbol` field names (case-insensitive)
- ✅ Indexes prices by BOTH `symbolId` AND `symbol` for lookup flexibility
- ✅ Normalizes decimal precision based on asset class
- ✅ Updates state reactively

### Market Data Lookup in Accordion

**File**: `frontend/mobile/src/screens/DashboardScreen.tsx`

```typescript
// Lines 137-172: marketDataBySymbol memo - Creates lookup index
const marketDataBySymbol = useMemo(() => {
  const data: Record<string, UnifiedMarketDataDto> = {};

  // Index all price data by ticker and symbolId
  Object.entries(enhancedPrices).forEach(([key, marketData]) => {
    data[key] = marketData;
    if (marketData.symbol) {
      data[marketData.symbol] = marketData;  // ✅ Index by symbol
    }
    if (marketData.symbolId) {
      data[marketData.symbolId] = marketData;  // ✅ Index by UUID
    }
  });

  // Map symbol UUIDs to their price data by ticker
  const allSymbols = [...state.cryptoSymbols, ...state.bistSymbols, ...state.nasdaqSymbols];

  allSymbols.forEach(symbol => {
    const priceData = data[symbol.symbol];
    if (priceData && symbol.id) {
      data[symbol.id] = priceData;  // ✅ Map UUID → price data
    }
  });

  return data;
}, [enhancedPrices, state.cryptoSymbols, state.bistSymbols, state.nasdaqSymbols]);
```

**File**: `frontend/mobile/src/components/dashboard/AssetClassAccordion.tsx`

```typescript
// Lines 219-242: Symbol card rendering with comprehensive lookup
const renderSymbolCard = (symbol: EnhancedSymbolDto, index: number) => {
  // Try comprehensive lookup strategies
  const lookupKeys = [
    symbol.id,                    // UUID
    symbol.symbol,                // BTC or BTCUSDT
    `${symbol.symbol}USDT`,       // BTCUSDT
    symbol.baseCurrency,          // BTC
    // ... more variations
  ].filter(Boolean);

  let symbolMarketData = undefined;
  for (const key of lookupKeys) {
    if (marketData[key as string]) {
      symbolMarketData = marketData[key as string];
      break;  // ✅ Found match
    }
  }

  return <AssetCard
    symbol={symbol}
    marketData={symbolMarketData}  // ✅ Crypto data found
    // ...
  />;
};
```

**Why Crypto Works**:
1. ✅ Symbols fetched successfully (`BTCUSDT`, etc.)
2. ✅ WebSocket subscription successful
3. ✅ Price updates broadcast in real-time
4. ✅ `price_update` handler processes data
5. ✅ `enhancedPrices` state updated with both `BTCUSDT` and UUID keys
6. ✅ `marketDataBySymbol` lookup finds crypto prices by multiple key types
7. ✅ Accordion component receives `marketData` prop with price
8. ✅ UI displays price correctly

---

## 4. CURRENT TEST RESULTS (BASELINE FOR REGRESSION)

### API Test Results

```json
{
  "timestamp": "2025-10-09T14:34:00Z",
  "crypto_symbols_api": {
    "status": "success",
    "endpoint": "/api/symbols?exchange=BINANCE",
    "count": 9,
    "symbols": ["AVAX", "BNB", "BTC", "ENA", "ETH", "SOL", "SUI", "UNI", "XRP"],
    "sample_data": {
      "symbol": "BTCUSDT",
      "display_name": "Bitcoin",
      "market": "BINANCE",
      "marketName": "BINANCE",
      "baseCurrency": "BTC",
      "quoteCurrency": "USDT"
    }
  },
  "stock_symbols_api": {
    "status": "success",
    "endpoint": "/api/symbols?exchange=BIST",
    "count": 3,
    "symbols": ["GARAN", "SISE", "THYAO"],
    "sample_data": {
      "symbol": "GARAN",
      "display_name": "Garanti BBVA",
      "market": "BIST",
      "marketName": "BIST",
      "baseCurrency": "GARAN",
      "quoteCurrency": "TRY"
    }
  },
  "signalr_crypto": {
    "status": "connected",
    "broadcasting": true,
    "connection_url": "wss://stream.binance.com:9443/stream?streams=...",
    "update_frequency": "real-time",
    "sample_broadcast": {
      "symbol": "BTCUSDT",
      "price": 121420.64,
      "change": -0.929
    }
  },
  "mobile_crypto_accordion": {
    "status": "displaying",
    "prices_visible": true,
    "symbols_loaded": 9,
    "websocket_subscribed": true,
    "price_updates_received": true
  },
  "mobile_stock_accordion": {
    "status": "loading / no data",
    "prices_visible": false,
    "symbols_loaded": 3,
    "websocket_subscribed": true,
    "price_updates_received": false  // ❌ ROOT CAUSE
  }
}
```

---

## 5. ROOT CAUSE ANALYSIS: WHY STOCKS FAIL WHILE CRYPTO WORKS

### CRITICAL DIFFERENCE #1: Backend Data Source

**Crypto**:
- ✅ Real-time WebSocket from Binance (`wss://stream.binance.com`)
- ✅ Always broadcasting price updates
- ✅ Sub-second latency

**Stock**:
- ❓ Unknown data source for BIST stocks
- ❓ No evidence of price broadcasts in backend logs
- ❓ No equivalent to `BinanceWebSocketService` for BIST

### CRITICAL DIFFERENCE #2: Symbol Format Expectations

**Crypto Subscription**:
```javascript
// Mobile sends: ['BTCUSDT', 'ETHUSDT', 'BNBUSDT', ...]
websocketService.subscribeToAssetClass('CRYPTO', ['BTCUSDT', 'ETHUSDT', ...])

// Backend expects: btcusdt@ticker (lowercase with @ticker suffix)
// Binance WebSocket handles transformation internally
```

**Stock Subscription**:
```javascript
// Mobile sends: ['GARAN', 'SISE', 'THYAO']
websocketService.subscribeToAssetClass('STOCK', ['GARAN', 'SISE', 'THYAO'])

// Backend expects: ??? (No BIST WebSocket service found)
// ❌ NO HANDLER FOR STOCK SUBSCRIPTIONS
```

### CRITICAL DIFFERENCE #3: Price Broadcast Logic

**Backend Code Analysis Needed**:

```bash
# Search backend for STOCK price broadcasting
grep -r "STOCK\|BIST" backend/MyTrader.Api/ --include="*.cs" | grep -i "broadcast\|hub\|signalr"
```

**Expected to Find**:
- ❌ NO `BistWebSocketService.cs` equivalent to `BinanceWebSocketService.cs`
- ❌ NO scheduled polling service for BIST stock prices
- ❌ NO real-time data provider for BIST

**Hypothesis**: Backend may have:
1. A Yahoo Finance polling service (based on file names seen)
2. No real-time broadcast for stocks
3. REST API only (no SignalR hub events for STOCK updates)

### CRITICAL DIFFERENCE #4: SignalR Hub Method Handling

**File**: `backend/MyTrader.Api/Hubs/DashboardHub.cs` (needs verification)

**Expected Logic**:
```csharp
public async Task SubscribeToPriceUpdates(string assetClass, string[] symbols)
{
    if (assetClass == "CRYPTO")
    {
        // ✅ Subscribe to Binance WebSocket
        await BinanceWebSocketService.Subscribe(symbols);
    }
    else if (assetClass == "STOCK")
    {
        // ❌ MISSING IMPLEMENTATION
        // Should subscribe to BIST/Yahoo Finance polling
        // Or return cached REST data
    }
}
```

---

## 6. KEY QUESTIONS TO ANSWER (Before Fix Implementation)

### Backend Architecture Questions

1. **Does backend have a BIST WebSocket service?**
   - Location: `backend/MyTrader.Services/Market/BistWebSocketService.cs` ?
   - Status: Exists / Missing / Disabled?

2. **How are BIST stock prices updated?**
   - Option A: Yahoo Finance polling (5-minute intervals)
   - Option B: Real-time API (Alpha Vantage, IEX Cloud, etc.)
   - Option C: Mock/static data only

3. **Does DashboardHub broadcast stock prices?**
   - Method: `BroadcastStockPriceUpdate()` exists?
   - Trigger: Polling interval / Event-driven?
   - Frequency: Every 5 minutes / real-time / manual?

4. **What happens when mobile subscribes to 'STOCK'?**
   - Does `SubscribeToPriceUpdates('STOCK', ['GARAN', ...])` complete successfully?
   - Are there backend error logs indicating missing service?
   - Is subscription acknowledged but never sends updates?

### Mobile App Questions

1. **Are stock symbols correctly indexed in `enhancedPrices`?**
   - Check console logs: `[PriceContext] enhancedPrices state updated`
   - Verify: Are GARAN/SISE/THYAO keys present?

2. **Does `marketDataBySymbol` contain stock price data?**
   - Check logs: `[Dashboard] Stock lookup test`
   - Verify: `foundById: true` or `false`?

3. **Is the accordion receiving `marketData` prop?**
   - Check: AssetClassAccordion `marketData` prop in BIST section
   - Verify: Is it an empty object `{}` or does it have price data?

---

## 7. VALIDATION CHECKLIST (Post-Fix Regression Test)

After implementing the stock fix, verify that crypto functionality STILL WORKS:

### Backend Validation
- [ ] Crypto WebSocket still connects to Binance
- [ ] Backend logs show "Successfully connected to WebSocket wss://stream.binance.com..."
- [ ] Price updates still broadcast every second for BTC, ETH, etc.
- [ ] `/api/symbols?exchange=BINANCE` still returns 9 symbols

### Mobile App Validation
- [ ] `[PriceContext] Loaded 9 crypto symbols` log still appears
- [ ] `[PriceContext] Successfully subscribed to CRYPTO price updates` log still appears
- [ ] `[PriceContext] ✅ Stock price updated` logs appear for crypto symbols
- [ ] Crypto accordion expands and shows 9 cards
- [ ] Each crypto card displays live price (e.g., $121,420.64 for BTC)
- [ ] Price changes update in real-time (every 1-2 seconds)
- [ ] Green/red percentage changes display correctly

### Data Flow Validation
- [ ] `enhancedPrices` state contains keys: `BTCUSDT`, `ETHUSDT`, etc.
- [ ] `marketDataBySymbol` lookup returns data for crypto symbol IDs
- [ ] AssetCard receives non-null `marketData` prop for crypto
- [ ] No console errors or warnings in crypto data path

---

## 8. RECOMMENDED FIX APPROACH (Without Breaking Crypto)

### Option 1: Enable Backend BIST Polling Service
```csharp
// backend/MyTrader.Api/Program.cs
services.AddHostedService<YahooFinancePollingService>();

// backend/MyTrader.Services/Market/YahooFinancePollingService.cs
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await FetchBistPrices();
        await BroadcastToClients();
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
    }
}
```

### Option 2: Use Cached REST Data for Stocks
```typescript
// frontend/mobile/src/context/PriceContext.tsx
useEffect(() => {
  if (connectionStatus === 'connected') {
    // ✅ Subscribe to CRYPTO (real-time)
    await websocketService.subscribeToAssetClass('CRYPTO', cryptoSymbolNames);

    // ✅ Poll REST API for STOCK (5-minute cache)
    setInterval(async () => {
      const stockPrices = await apiService.getBatchMarketData(stockSymbolIds);
      setEnhancedPrices(prev => ({ ...prev, ...stockPrices }));
    }, 5 * 60 * 1000);
  }
}, [connectionStatus]);
```

### Option 3: Mock Stock Data for Development
```typescript
// Temporary fix for testing UI
const mockStockPrices = {
  'GARAN': { symbol: 'GARAN', price: 58.50, change: 1.2, assetClass: 'STOCK' },
  'SISE': { symbol: 'SISE', price: 42.30, change: -0.8, assetClass: 'STOCK' },
  'THYAO': { symbol: 'THYAO', price: 95.20, change: 2.3, assetClass: 'STOCK' },
};

setEnhancedPrices(prev => ({ ...prev, ...mockStockPrices }));
```

---

## 9. CRITICAL FILES - DO NOT MODIFY WITHOUT UNDERSTANDING

### Backend (Crypto-related - HIGH RISK)
- `backend/MyTrader.Services/Market/BinanceWebSocketService.cs` - Crypto price source
- `backend/MyTrader.Api/Hubs/DashboardHub.cs` - SignalR broadcast hub
- `backend/MyTrader.Api/Controllers/SymbolsController.cs` - Symbol API

### Frontend (Crypto data flow - HIGH RISK)
- `frontend/mobile/src/context/PriceContext.tsx` - Price state management
- `frontend/mobile/src/services/websocketService.ts` - SignalR client
- `frontend/mobile/src/services/multiAssetApi.ts` - Symbol fetching
- `frontend/mobile/src/screens/DashboardScreen.tsx` - Data aggregation
- `frontend/mobile/src/components/dashboard/AssetClassAccordion.tsx` - UI rendering

### Utility Files (Price normalization - MEDIUM RISK)
- `frontend/mobile/src/utils/priceFormatting.ts` - Decimal precision

---

## 10. SUCCESS CRITERIA FOR STOCK FIX

The fix is considered successful when:

1. ✅ Stock accordion displays 3 cards (GARAN, SISE, THYAO)
2. ✅ Each stock card shows current price (e.g., "58.50 TRY")
3. ✅ Green/red percentage changes display
4. ✅ No "loading" or "no data" state in stock accordion
5. ✅ **AND CRYPTO ACCORDION STILL WORKS EXACTLY AS BEFORE**

**Regression Test Required**:
- Run mobile app
- Expand crypto accordion → Should show 9 symbols with live prices
- Expand stock accordion → Should show 3 symbols with prices
- Wait 30 seconds → Crypto prices should update, stock prices may be static (depending on implementation)

---

## NEXT STEPS

1. Read backend `DashboardHub.cs` to understand `SubscribeToPriceUpdates` implementation
2. Search for BIST/Yahoo Finance polling services in backend
3. Verify if stock price data exists in backend (REST API check)
4. Implement appropriate fix based on backend capabilities
5. Validate crypto functionality remains intact
6. Document stock data flow in separate document

---

**Document Version**: 1.0
**Last Updated**: 2025-10-09
**Crypto Functionality**: ✅ WORKING - DO NOT REGRESS
**Stock Functionality**: ❌ BROKEN - NEEDS FIX
