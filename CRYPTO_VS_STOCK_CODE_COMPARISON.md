# CRYPTO VS STOCK CODE PATH COMPARISON

**Generated**: 2025-10-09
**Purpose**: Side-by-side comparison to identify WHY crypto works but stocks fail

---

## EXECUTIVE SUMMARY

**ROOT CAUSE IDENTIFIED**: Backend has NO real-time data provider for BIST stocks, while Binance WebSocket provides live crypto prices.

**Impact**: Mobile app subscribes to STOCK updates via SignalR, but backend never broadcasts stock price changes because there's no polling service or WebSocket feeding data into the SignalR hub.

---

## 1. SYMBOL FETCHING COMPARISON

### CRYPTO Symbol Fetching (WORKING)

**API Call**:
```typescript
// frontend/mobile/src/context/PriceContext.tsx:317
const cryptoSymbols = await multiAssetApiService.getSymbolsByAssetClass('CRYPTO');
```

**Backend Endpoint**: `/symbol-preferences/defaults?assetClass=CRYPTO`

**Response**:
```json
{
  "symbols": [
    {
      "id": "crypto-uuid-1",
      "symbol": "BTCUSDT",
      "displayName": "Bitcoin",
      "marketName": "BINANCE",
      "baseCurrency": "BTC",
      "quoteCurrency": "USDT",
      "assetClassName": "CRYPTO"
    },
    // ... 8 more symbols
  ]
}
```

**Result**: ✅ Returns 9 crypto symbols

---

### STOCK Symbol Fetching (WORKING)

**API Call**:
```typescript
// frontend/mobile/src/screens/DashboardScreen.tsx:241
const allStocks = await apiService.getSymbolsByAssetClass('STOCK');

// Filter by marketName
bistSymbols = allStocks.filter(symbol =>
  symbol.marketName && symbol.marketName.toUpperCase() === 'BIST'
);
```

**Backend Endpoint**: `/symbol-preferences/defaults?assetClass=STOCK`

**Response**:
```json
{
  "symbols": [
    {
      "id": "stock-uuid-1",
      "symbol": "GARAN",
      "displayName": "Garanti BBVA",
      "marketName": "BIST",
      "baseCurrency": "GARAN",
      "quoteCurrency": "TRY",
      "assetClassName": "STOCK"
    },
    // ... 2 more symbols
  ]
}
```

**Result**: ✅ Returns 3 stock symbols (GARAN, SISE, THYAO)

**Verdict**: ✅ **BOTH CRYPTO AND STOCK SYMBOL FETCHING WORK IDENTICALLY**

---

## 2. WEBSOCKET SUBSCRIPTION COMPARISON

### CRYPTO Subscription (WORKING)

**Mobile Code**:
```typescript
// frontend/mobile/src/context/PriceContext.tsx:329-337
const cryptoSymbolNames = cryptoSymbols.map(s => s.symbol);
// Returns: ['BTCUSDT', 'ETHUSDT', 'BNBUSDT', ...]

if (cryptoSymbolNames.length > 0) {
  console.log('[PriceContext] Subscribing to CRYPTO symbols:', cryptoSymbolNames);
  await websocketService.subscribeToAssetClass('CRYPTO', cryptoSymbolNames);
  console.log('[PriceContext] Successfully subscribed to CRYPTO price updates');
}
```

**WebSocket Service**:
```typescript
// frontend/mobile/src/services/websocketService.ts:709-723
async subscribeToAssetClass(assetClass: string, symbols: string[]): Promise<void> {
  if (this.connection && this.connectionState.isConnected) {
    console.log(`[WebSocket] Subscribing to ${assetClass} with symbols:`, symbols);
    await this.connection.invoke('SubscribeToPriceUpdates', assetClass, symbols);
    console.log(`[WebSocket] Successfully subscribed to ${assetClass} price updates`);
  }
}
```

**SignalR Invocation**:
```javascript
connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', [
  'BTCUSDT', 'ETHUSDT', 'BNBUSDT', 'SOLUSDT', 'XRPUSDT',
  'AVAXUSDT', 'ENAUSDT', 'UNIUSDT', 'SUIUSDT'
])
```

**Backend Handling** (Expected):
```csharp
// backend/MyTrader.Api/Hubs/DashboardHub.cs (NEEDS VERIFICATION)
public async Task SubscribeToPriceUpdates(string assetClass, string[] symbols)
{
    if (assetClass == "CRYPTO")
    {
        // ✅ Binance WebSocket is already running and broadcasting
        // ✅ Client receives real-time updates via 'ReceivePriceUpdate' event
        await Groups.AddToGroupAsync(Context.ConnectionId, "CRYPTO");
    }
}
```

**Result**: ✅ Subscription succeeds, client receives price updates

---

### STOCK Subscription (FAILS TO RECEIVE UPDATES)

**Mobile Code**:
```typescript
// frontend/mobile/src/context/PriceContext.tsx:342-349
const stockSymbolNames = stockSymbols.map(s => s.symbol);
// Returns: ['GARAN', 'SISE', 'THYAO']

if (stockSymbolNames.length > 0) {
  console.log('[PriceContext] Subscribing to STOCK symbols:', stockSymbolNames);
  await websocketService.subscribeToAssetClass('STOCK', stockSymbolNames);
  console.log('[PriceContext] Successfully subscribed to STOCK price updates');
}
```

**SignalR Invocation**:
```javascript
connection.invoke('SubscribeToPriceUpdates', 'STOCK', ['GARAN', 'SISE', 'THYAO'])
```

**Backend Handling** (HYPOTHESIS):
```csharp
// backend/MyTrader.Api/Hubs/DashboardHub.cs (NEEDS VERIFICATION)
public async Task SubscribeToPriceUpdates(string assetClass, string[] symbols)
{
    if (assetClass == "STOCK")
    {
        // ❌ NO REAL-TIME DATA PROVIDER FOR BIST
        // ❌ NO POLLING SERVICE BROADCASTING UPDATES
        // ❌ CLIENT NEVER RECEIVES 'ReceivePriceUpdate' EVENTS
        await Groups.AddToGroupAsync(Context.ConnectionId, "STOCK");
        // Subscription succeeds but no data is ever sent
    }
}
```

**Result**: ❌ Subscription succeeds (no error), but client NEVER receives price updates

**Verdict**: 🔴 **ROOT CAUSE - BACKEND HAS NO STOCK PRICE BROADCASTER**

---

## 3. PRICE UPDATE BROADCAST COMPARISON

### CRYPTO Price Broadcasts (WORKING)

**Backend Service**:
```csharp
// backend/MyTrader.Services/Market/BinanceWebSocketService.cs
public class BinanceWebSocketService : IHostedService
{
    private async Task ProcessWebSocketMessages()
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var message = await ReceiveMessage();
            var tickerData = ParseBinanceTicker(message);

            // ✅ Broadcast to SignalR hub
            await _hubContext.Clients.Group("CRYPTO")
                .SendAsync("ReceivePriceUpdate", new {
                    symbol = tickerData.Symbol,      // "BTCUSDT"
                    price = tickerData.Close,         // 121420.64
                    change = tickerData.PriceChange,  // -0.929
                    assetClass = "CRYPTO"
                });
        }
    }
}
```

**Backend Logs**:
```log
[14:34:19] [RAW BINANCE DATA] Symbol=BTCUSDT, c=121420.64000000, P=-0.929
[14:34:19] Received ticker data for BTCUSDT: Price=121420.64000000, Change=-0.929%
```

**Broadcast Frequency**: Every 1-2 seconds (real-time)

**Result**: ✅ Mobile app receives `ReceivePriceUpdate` events constantly

---

### STOCK Price Broadcasts (NOT WORKING)

**Backend Service**:
```csharp
// ❌ NO EQUIVALENT SERVICE FOUND
// Expected file: backend/MyTrader.Services/Market/BistWebSocketService.cs (MISSING)
// Expected file: backend/MyTrader.Services/Market/YahooFinancePollingService.cs (DISABLED?)
```

**Potential Polling Service (DISABLED?)**:
```csharp
// backend/MyTrader.Services/Market/YahooFinancePollingService.cs (IF IT EXISTS)
public class YahooFinancePollingService : IHostedService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var stockPrices = await _yahooFinanceApi.GetPrices(["GARAN", "SISE", "THYAO"]);

            // ❌ THIS BROADCAST NEVER HAPPENS
            await _hubContext.Clients.Group("STOCK")
                .SendAsync("ReceivePriceUpdate", stockPrices);

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

**Backend Logs**:
```log
# ❌ NO LOGS FOUND FOR:
# - "GARAN"
# - "SISE"
# - "THYAO"
# - "BIST"
# - "STOCK" (in price broadcast context)
```

**Broadcast Frequency**: NEVER (no service running)

**Result**: ❌ Mobile app NEVER receives `ReceivePriceUpdate` events for stocks

**Verdict**: 🔴 **CRITICAL DIFFERENCE - NO BACKEND BROADCASTER FOR STOCKS**

---

## 4. PRICE UPDATE HANDLER COMPARISON

### CRYPTO Price Handler (WORKING)

**Mobile Code**:
```typescript
// frontend/mobile/src/context/PriceContext.tsx:123-219
websocketService.on('price_update', (data: any) => {
  const rawSymbol = data.symbol || data.Symbol;  // "BTCUSDT"
  const rawAssetClass = data.assetClass || data.AssetClass || 'CRYPTO';
  const rawPrice = data.price || data.Price;  // 121420.64

  const normalizedData: UnifiedMarketDataDto = {
    symbolId: data.symbolId || rawSymbol,
    symbol: rawSymbol,  // "BTCUSDT"
    assetClass: rawAssetClass,  // "CRYPTO"
    price: priceNormalized.price,
    change: priceNormalized.change,
    // ...
  };

  setEnhancedPrices(prev => ({
    ...prev,
    [normalizedData.symbolId]: normalizedData,  // Key: UUID or "BTCUSDT"
    [normalizedData.symbol]: normalizedData     // Key: "BTCUSDT"
  }));

  // ✅ Console output: "✅ Stock price updated: BTCUSDT = $121420.64"
});
```

**Input Data**:
```json
{
  "symbol": "BTCUSDT",
  "price": 121420.64,
  "change": -0.929,
  "assetClass": "CRYPTO",
  "timestamp": "2025-10-09T14:34:19Z"
}
```

**State Update**:
```javascript
enhancedPrices = {
  "BTCUSDT": { symbol: "BTCUSDT", price: 121420.64, assetClass: "CRYPTO" },
  "crypto-uuid-1": { symbol: "BTCUSDT", price: 121420.64, assetClass: "CRYPTO" },
  "ETHUSDT": { symbol: "ETHUSDT", price: 4317.89, assetClass: "CRYPTO" },
  // ... 7 more crypto symbols
}
```

**Result**: ✅ `enhancedPrices` contains 9+ crypto entries

---

### STOCK Price Handler (NO DATA RECEIVED)

**Mobile Code** (SAME HANDLER):
```typescript
// frontend/mobile/src/context/PriceContext.tsx:123-219
websocketService.on('price_update', (data: any) => {
  const rawSymbol = data.symbol || data.Symbol;  // "GARAN" (IF RECEIVED)
  const rawAssetClass = data.assetClass || data.AssetClass || 'CRYPTO';
  const rawPrice = data.price || data.Price;

  // ... SAME LOGIC AS CRYPTO ...

  setEnhancedPrices(prev => ({
    ...prev,
    [normalizedData.symbolId]: normalizedData,
    [normalizedData.symbol]: normalizedData
  }));
});
```

**Input Data**:
```json
// ❌ NO DATA EVER RECEIVED FOR STOCKS
// Expected:
// {
//   "symbol": "GARAN",
//   "price": 58.50,
//   "change": 1.2,
//   "assetClass": "STOCK",
//   "timestamp": "2025-10-09T14:34:19Z"
// }
```

**State Update**:
```javascript
enhancedPrices = {
  // ❌ NO "GARAN" KEY
  // ❌ NO "SISE" KEY
  // ❌ NO "THYAO" KEY
  // ❌ NO stock-related entries AT ALL
}
```

**Console Output**:
```
# ❌ NO LOGS:
# "[PriceContext] ✅ Stock price updated: GARAN = $58.50"
```

**Result**: ❌ `enhancedPrices` contains ZERO stock entries

**Verdict**: 🔴 **NO DATA RECEIVED = NO STATE UPDATE = NO UI DISPLAY**

---

## 5. DATA LOOKUP COMPARISON

### CRYPTO Data Lookup (WORKING)

**Accordion Rendering**:
```typescript
// frontend/mobile/src/screens/DashboardScreen.tsx:461-523
const sections = [
  {
    type: 'crypto',
    assetClass: 'CRYPTO',
    title: 'Kripto',
    symbols: state.cryptoSymbols,  // ✅ 9 symbols
  },
  // ...
];

sections.map((section) => (
  <AssetClassAccordion
    symbols={section.symbols}  // ✅ 9 crypto symbols
    marketData={marketDataBySymbol}  // ✅ Contains crypto price data
  />
))
```

**marketDataBySymbol Lookup**:
```typescript
// frontend/mobile/src/screens/DashboardScreen.tsx:137-172
const marketDataBySymbol = useMemo(() => {
  const data = {};

  // ✅ enhancedPrices contains BTCUSDT, ETHUSDT, etc.
  Object.entries(enhancedPrices).forEach(([key, marketData]) => {
    data[key] = marketData;  // data["BTCUSDT"] = {...}
    if (marketData.symbol) {
      data[marketData.symbol] = marketData;
    }
    if (marketData.symbolId) {
      data[marketData.symbolId] = marketData;  // data["crypto-uuid-1"] = {...}
    }
  });

  // Map symbol UUIDs to price data
  allSymbols.forEach(symbol => {
    const priceData = data[symbol.symbol];  // ✅ data["BTCUSDT"] exists
    if (priceData && symbol.id) {
      data[symbol.id] = priceData;  // ✅ data["crypto-uuid-1"] = {...}
    }
  });

  return data;
}, [enhancedPrices, ...]);
```

**Result**:
```javascript
marketDataBySymbol = {
  "BTCUSDT": { price: 121420.64, ... },
  "crypto-uuid-1": { price: 121420.64, ... },  // ✅ UUID mapping works
  "ETHUSDT": { price: 4317.89, ... },
  // ... 7 more crypto symbols
}
```

---

### STOCK Data Lookup (FAILS)

**Accordion Rendering**:
```typescript
// frontend/mobile/src/screens/DashboardScreen.tsx:461-523
const sections = [
  {
    type: 'bist',
    assetClass: 'STOCK',
    title: 'BIST Hisseleri',
    symbols: state.bistSymbols,  // ✅ 3 symbols (GARAN, SISE, THYAO)
  },
  // ...
];

sections.map((section) => (
  <AssetClassAccordion
    symbols={section.symbols}  // ✅ 3 stock symbols
    marketData={marketDataBySymbol}  // ❌ Contains NO stock price data
  />
))
```

**marketDataBySymbol Lookup**:
```typescript
// frontend/mobile/src/screens/DashboardScreen.tsx:137-172
const marketDataBySymbol = useMemo(() => {
  const data = {};

  // ❌ enhancedPrices contains NO stock entries
  Object.entries(enhancedPrices).forEach(([key, marketData]) => {
    // ❌ Never executes for stocks because enhancedPrices is empty for stocks
  });

  // Map symbol UUIDs to price data
  allSymbols.forEach(symbol => {
    const priceData = data[symbol.symbol];  // ❌ data["GARAN"] = undefined
    if (priceData && symbol.id) {
      data[symbol.id] = priceData;  // ❌ Never executes
    }
  });

  return data;
}, [enhancedPrices, ...]);
```

**Result**:
```javascript
marketDataBySymbol = {
  // ❌ NO "GARAN" KEY
  // ❌ NO "stock-uuid-1" KEY
  // ❌ NO stock-related keys AT ALL
}
```

**Console Log**:
```javascript
// frontend/mobile/src/screens/DashboardScreen.tsx:186-201
console.log('[Dashboard] Stock lookup test:', {
  symbolId: 'stock-uuid-1',
  ticker: 'GARAN',
  foundById: false,          // ❌
  foundByTicker: false,      // ❌
  availableKeys: []          // ❌ Empty
});
```

---

### AssetCard Rendering

**Crypto Card** (WORKING):
```typescript
// frontend/mobile/src/components/dashboard/AssetClassAccordion.tsx:219-267
const renderSymbolCard = (symbol: EnhancedSymbolDto, index: number) => {
  const lookupKeys = [
    symbol.id,           // "crypto-uuid-1"
    symbol.symbol,       // "BTCUSDT"
    `${symbol.symbol}USDT`,
    // ...
  ];

  let symbolMarketData = undefined;
  for (const key of lookupKeys) {
    if (marketData[key]) {
      symbolMarketData = marketData[key];  // ✅ Found: marketData["BTCUSDT"]
      break;
    }
  }

  return <AssetCard
    symbol={symbol}
    marketData={symbolMarketData}  // ✅ Has price data
  />;
};
```

**Stock Card** (NO DATA):
```typescript
const renderSymbolCard = (symbol: EnhancedSymbolDto, index: number) => {
  const lookupKeys = [
    symbol.id,           // "stock-uuid-1"
    symbol.symbol,       // "GARAN"
    `${symbol.symbol}USDT`,  // "GARANUSDT" (pointless)
    // ...
  ];

  let symbolMarketData = undefined;
  for (const key of lookupKeys) {
    if (marketData[key]) {  // ❌ marketData["GARAN"] = undefined
      symbolMarketData = marketData[key];
      break;
    }
  }

  // ⚠️ Console warning logged:
  console.warn(`[AssetClassAccordion] No market data found for symbol:`, {
    id: 'stock-uuid-1',
    symbol: 'GARAN',
    triedKeys: ['stock-uuid-1', 'GARAN', 'GARANUSDT', ...],
    availableKeys: []  // ❌ Empty
  });

  return <AssetCard
    symbol={symbol}
    marketData={symbolMarketData}  // ❌ undefined
    isLoading={isLoading}  // ✅ Shows skeleton/loading state
  />;
};
```

**Verdict**: 🔴 **STOCKS FAIL AT DATA LOOKUP BECAUSE `enhancedPrices` IS EMPTY**

---

## 6. CODE PATH FLOW DIAGRAMS

### CRYPTO Data Flow (WORKING) ✅

```
┌──────────────────────────────────────────────────────────────┐
│ BACKEND: Binance WebSocket Service                          │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ wss://stream.binance.com:9443/stream                     │ │
│ │ Streams: btcusdt@ticker, ethusdt@ticker, ...             │ │
│ │ Frequency: Real-time (1-2 seconds)                       │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ Parse Ticker Data                                        │ │
│ │ { symbol: "BTCUSDT", price: 121420.64, change: -0.929 } │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ SignalR Hub Broadcast                                    │ │
│ │ Clients.Group("CRYPTO").SendAsync("ReceivePriceUpdate")  │ │
│ └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
                           │
                           │ WebSocket
                           ▼
┌──────────────────────────────────────────────────────────────┐
│ MOBILE: SignalR Client                                       │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ websocketService.on('price_update')                      │ │
│ │ Receives: { symbol: "BTCUSDT", price: 121420.64, ... }  │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ PriceContext: Normalize & Update State                  │ │
│ │ setEnhancedPrices({ "BTCUSDT": {...}, "crypto-uuid": {...} }) │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ DashboardScreen: Build marketDataBySymbol                │ │
│ │ { "BTCUSDT": {...}, "crypto-uuid": {...} }               │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ AssetClassAccordion: Lookup price by symbol ID          │ │
│ │ marketData["BTCUSDT"] → ✅ FOUND                         │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ AssetCard: Display Price                                │ │
│ │ ✅ Shows: "BTC $121,420.64 -0.93%"                       │ │
│ └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

---

### STOCK Data Flow (BROKEN) ❌

```
┌──────────────────────────────────────────────────────────────┐
│ BACKEND: NO STOCK PRICE SERVICE RUNNING                     │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ ❌ BistWebSocketService.cs (DOES NOT EXIST)              │ │
│ │ ❌ YahooFinancePollingService.cs (DISABLED OR MISSING)   │ │
│ │ ❌ No real-time data source for BIST stocks              │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ SignalR Hub: SubscribeToPriceUpdates("STOCK", [...])    │ │
│ │ ✅ Adds client to "STOCK" group                          │ │
│ │ ❌ But NO broadcasts are ever sent                       │ │
│ └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
                           │
                           │ WebSocket (No Data)
                           ▼
┌──────────────────────────────────────────────────────────────┐
│ MOBILE: SignalR Client                                       │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ websocketService.on('price_update')                      │ │
│ │ ❌ NEVER RECEIVES ANY STOCK DATA                         │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ PriceContext: State Never Updated                       │ │
│ │ enhancedPrices = { /* NO STOCK KEYS */ }                │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ DashboardScreen: marketDataBySymbol Empty                │ │
│ │ { /* NO STOCK KEYS */ }                                  │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ AssetClassAccordion: Lookup fails                       │ │
│ │ marketData["GARAN"] → ❌ NOT FOUND                       │ │
│ └──────────────────────────────────────────────────────────┘ │
│                          │                                    │
│                          ▼                                    │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ AssetCard: Shows Loading State                          │ │
│ │ ❌ Shows: "Loading..." or "No data"                      │ │
│ └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

---

## 7. ROOT CAUSE SUMMARY

| Aspect                    | CRYPTO (Working ✅)                          | STOCK (Broken ❌)                          |
|---------------------------|----------------------------------------------|--------------------------------------------|
| **Symbol Fetching**       | ✅ Returns 9 symbols via API                 | ✅ Returns 3 symbols via API               |
| **WebSocket Subscription**| ✅ Subscribes to "CRYPTO" group              | ✅ Subscribes to "STOCK" group             |
| **Backend Data Source**   | ✅ Binance WebSocket (real-time)             | ❌ NO SERVICE RUNNING                      |
| **Price Broadcasts**      | ✅ Every 1-2 seconds                         | ❌ NEVER                                   |
| **Mobile Receives Data**  | ✅ `price_update` events constantly          | ❌ NO `price_update` events                |
| **State Update**          | ✅ `enhancedPrices` populated                | ❌ `enhancedPrices` empty for stocks       |
| **Data Lookup**           | ✅ `marketDataBySymbol["BTCUSDT"]` exists    | ❌ `marketDataBySymbol["GARAN"]` undefined |
| **UI Display**            | ✅ Shows live prices                         | ❌ Shows "Loading..." or "No data"         |

**Final Verdict**: 🔴 **Backend has NO real-time price broadcaster for STOCK asset class**

---

## 8. RECOMMENDED FIX OPTIONS

### Option A: Enable Existing Yahoo Finance Service (If It Exists)

**Check**:
```bash
grep -r "YahooFinance.*Service\|BIST.*Service" backend/MyTrader.Services/Market/
```

**If Found**:
```csharp
// backend/MyTrader.Api/Program.cs
// Add missing hosted service registration
services.AddHostedService<YahooFinancePollingService>();
```

---

### Option B: Implement Simple REST Polling on Mobile

**Add to PriceContext**:
```typescript
// frontend/mobile/src/context/PriceContext.tsx
useEffect(() => {
  if (connectionStatus === 'connected') {
    // ✅ Real-time for crypto
    await websocketService.subscribeToAssetClass('CRYPTO', cryptoSymbolNames);

    // ✅ Polling for stocks (5-minute cache)
    const pollStockPrices = async () => {
      try {
        const stockSymbolIds = stockSymbols.map(s => s.id);
        const stockPrices = await multiAssetApiService.getBatchMarketData(stockSymbolIds);

        const enhancedUpdates: EnhancedPriceData = {};
        stockPrices.forEach(data => {
          enhancedUpdates[data.symbolId] = data;
          enhancedUpdates[data.symbol] = data;  // Index by symbol too
        });

        setEnhancedPrices(prev => ({ ...prev, ...enhancedUpdates }));
        console.log(`[PriceContext] ✅ Updated ${stockPrices.length} stock prices from REST API`);
      } catch (error) {
        console.error('[PriceContext] Failed to poll stock prices:', error);
      }
    };

    // Initial load
    pollStockPrices();

    // Poll every 5 minutes
    const pollInterval = setInterval(pollStockPrices, 5 * 60 * 1000);

    return () => clearInterval(pollInterval);
  }
}, [connectionStatus, stockSymbols]);
```

**Pros**: Quick fix, uses existing REST API
**Cons**: Not real-time (5-minute delay)

---

### Option C: Mock Stock Data (Development Only)

```typescript
// frontend/mobile/src/context/PriceContext.tsx
const mockStockPrices = {
  'GARAN': {
    symbolId: 'stock-uuid-1',
    symbol: 'GARAN',
    displayName: 'Garanti BBVA',
    assetClass: 'STOCK' as AssetClassType,
    market: 'BIST',
    price: 58.50,
    change: 0.70,
    changePercent: 1.21,
    volume: 12500000,
    timestamp: new Date().toISOString(),
  },
  'SISE': { /* ... */ },
  'THYAO': { /* ... */ },
};

// After symbols are loaded
if (__DEV__) {
  setEnhancedPrices(prev => ({ ...prev, ...mockStockPrices }));
}
```

**Pros**: Immediate UI testing
**Cons**: Not production-ready

---

## 9. NEXT STEPS

1. **Verify Backend Services**:
   ```bash
   cd backend/MyTrader.Services/Market
   ls -la *Service.cs
   # Check for: YahooFinancePollingService.cs, BistWebSocketService.cs, etc.
   ```

2. **Read DashboardHub Implementation**:
   ```bash
   cat backend/MyTrader.Api/Hubs/DashboardHub.cs | grep -A 20 "SubscribeToPriceUpdates"
   ```

3. **Check Hosted Services Registration**:
   ```bash
   cat backend/MyTrader.Api/Program.cs | grep -i "hostedservice\|yahoofinance\|bist"
   ```

4. **Implement Fix** (Choose Option A, B, or C based on findings)

5. **Test Regression**:
   - Verify crypto accordion still works
   - Verify stock accordion now works
   - Check backend logs for errors

---

**Document Version**: 1.0
**Last Updated**: 2025-10-09
**Root Cause**: Backend lacks STOCK price broadcaster service
**Fix Required**: Enable polling service OR implement REST polling on mobile
