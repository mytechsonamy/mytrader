# MyTrader SignalR Hub Contracts

## Overview

MyTrader uses SignalR for real-time WebSocket communication, providing live market data updates, trading notifications, and dashboard information. All hubs support both authenticated and anonymous access patterns.

## Connection URLs

| Hub | URL | Access | Description |
|-----|-----|--------|-------------|
| MarketDataHub | `/hubs/market-data` | Anonymous | Real-time market data for all asset classes |
| TradingHub | `/hubs/trading` | Authenticated | Trading operations and order updates |
| PortfolioHub | `/hubs/portfolio` | Authenticated | Portfolio updates and performance metrics |
| MockTradingHub | `/hubs/mock-trading` | Anonymous | Simulated trading for demo purposes |

## Authentication

- **Anonymous Hubs**: No authentication required, accessible to all clients
- **Authenticated Hubs**: Require JWT token via:
  - Query string parameter: `?access_token=<jwt_token>`
  - Authorization header: `Authorization: Bearer <jwt_token>`

## MarketDataHub (/hubs/market-data)

**Access**: Anonymous
**Purpose**: Real-time market data distribution for crypto, stocks, and other asset classes

### Client → Server Methods

#### SubscribeToPriceUpdates
Subscribe to price updates for specific symbols within an asset class.

```typescript
connection.invoke("SubscribeToPriceUpdates", assetClass: string, symbols: string | string[] | List<string>)
```

**Parameters:**
- `assetClass`: Asset class code (`CRYPTO`, `STOCK_BIST`, `STOCK_NASDAQ`)
- `symbols`: Symbol(s) to subscribe to (flexible input types)

**Example:**
```javascript
// Subscribe to crypto symbols
await connection.invoke("SubscribeToPriceUpdates", "CRYPTO", ["BTC", "ETH", "ADA"]);

// Subscribe to single stock
await connection.invoke("SubscribeToPriceUpdates", "STOCK_NASDAQ", "AAPL");
```

#### SubscribeToAssetClass
Subscribe to all symbols within an asset class.

```typescript
connection.invoke("SubscribeToAssetClass", assetClass: string)
```

**Parameters:**
- `assetClass`: Asset class code

**Example:**
```javascript
await connection.invoke("SubscribeToAssetClass", "CRYPTO");
```

#### UnsubscribeFromPriceUpdates
Unsubscribe from specific symbols.

```typescript
connection.invoke("UnsubscribeFromPriceUpdates", assetClass: string, symbols: string | string[])
```

#### SubscribeToMarketStatus
Subscribe to market status updates for specific markets.

```typescript
connection.invoke("SubscribeToMarketStatus", markets: string[])
```

**Parameters:**
- `markets`: Array of market names (`["NASDAQ", "BIST", "BINANCE"]`)

#### GetMarketStatus
Get current market status for all tracked markets.

```typescript
connection.invoke("GetMarketStatus")
```

#### Legacy Methods (Backward Compatibility)
```typescript
connection.invoke("SubscribeToCrypto", symbols: string | string[])
connection.invoke("UnsubscribeFromCrypto", symbols: string[])
```

### Server → Client Events

#### ConnectionStatus
Sent when client connects successfully.

```typescript
interface ConnectionStatusEvent {
  status: "connected" | "disconnected";
  message: string;
  timestamp: string; // ISO 8601
  supportedAssetClasses: string[];
}
```

#### SubscriptionConfirmed
Sent after successful subscription to symbols.

```typescript
interface SubscriptionConfirmedEvent {
  assetClass: string;
  symbols: string[];
  timestamp: string; // ISO 8601
}
```

#### AssetClassSubscriptionConfirmed
Sent after successful asset class subscription.

```typescript
interface AssetClassSubscriptionConfirmedEvent {
  assetClass: string;
  timestamp: string; // ISO 8601
}
```

#### UnsubscriptionConfirmed
Sent after successful unsubscription.

```typescript
interface UnsubscriptionConfirmedEvent {
  assetClass: string;
  symbols: string[];
  timestamp: string; // ISO 8601
}
```

#### MarketStatusSubscriptionConfirmed
Sent after successful market status subscription.

```typescript
interface MarketStatusSubscriptionConfirmedEvent {
  markets: string[];
  timestamp: string; // ISO 8601
}
```

#### SubscriptionError
Sent when subscription fails.

```typescript
interface SubscriptionErrorEvent {
  error: "InvalidAssetClass" | "NoSymbols" | "InternalError";
  message: string;
  supportedClasses?: string[]; // Only for InvalidAssetClass
}
```

#### PriceUpdate
Real-time price updates for subscribed symbols.

```typescript
interface PriceUpdateEvent {
  assetClass: string;
  symbol: string;
  price: number;
  priceChange: number;
  priceChangePercent: number;
  volume: number;
  high24h: number;
  low24h: number;
  timestamp: string; // ISO 8601
  dataProvider: string;
}
```

#### MarketDataUpdate
Comprehensive market data updates.

```typescript
interface MarketDataUpdateEvent {
  symbolId: string; // UUID
  ticker: string;
  assetClassCode: string;
  price: number;
  previousClose: number;
  openPrice: number;
  highPrice: number;
  lowPrice: number;
  volume: number;
  volumeQuote: number;
  priceChange: number;
  priceChangePercent: number;
  bidPrice: number;
  askPrice: number;
  spread: number;
  lastTradeTime: string; // ISO 8601
  dataTimestamp: string; // ISO 8601
  isRealTime: boolean;
  marketStatus: "OPEN" | "CLOSED" | "PRE_MARKET" | "AFTER_HOURS";
}
```

#### CurrentMarketStatus
Current status for all markets.

```typescript
interface CurrentMarketStatusEvent {
  markets: MarketStatus[];
  timestamp: string; // ISO 8601
}

interface MarketStatus {
  market: string;
  status: "OPEN" | "CLOSED" | "PRE_MARKET" | "AFTER_HOURS" | "HOLIDAY";
  nextOpen: string | null; // ISO 8601
  nextClose: string | null; // ISO 8601
  timezone: string;
}
```

## TradingHub (/hubs/trading)

**Access**: Authenticated
**Purpose**: Real-time trading operations and order management

### Client → Server Methods

#### PlaceOrder
Place a new trading order.

```typescript
connection.invoke("PlaceOrder", order: PlaceOrderRequest)
```

```typescript
interface PlaceOrderRequest {
  symbolId: string; // UUID
  side: "BUY" | "SELL";
  type: "MARKET" | "LIMIT" | "STOP_LOSS" | "TAKE_PROFIT";
  quantity: number;
  price?: number; // Required for LIMIT orders
  stopPrice?: number; // Required for STOP orders
  timeInForce: "GTC" | "IOC" | "FOK" | "DAY";
}
```

#### CancelOrder
Cancel an existing order.

```typescript
connection.invoke("CancelOrder", orderId: string)
```

#### GetOpenOrders
Get all open orders for the user.

```typescript
connection.invoke("GetOpenOrders")
```

#### GetOrderHistory
Get order history with pagination.

```typescript
connection.invoke("GetOrderHistory", request: OrderHistoryRequest)
```

```typescript
interface OrderHistoryRequest {
  symbolId?: string; // UUID - optional filter
  startDate?: string; // ISO 8601
  endDate?: string; // ISO 8601
  limit?: number; // Default 100, max 500
  offset?: number; // Default 0
}
```

### Server → Client Events

#### OrderUpdate
Real-time order status updates.

```typescript
interface OrderUpdateEvent {
  orderId: string; // UUID
  symbolId: string; // UUID
  symbol: string;
  side: "BUY" | "SELL";
  type: string;
  status: "NEW" | "PARTIALLY_FILLED" | "FILLED" | "CANCELED" | "REJECTED";
  quantity: number;
  filledQuantity: number;
  price: number;
  averagePrice: number;
  createdAt: string; // ISO 8601
  updatedAt: string; // ISO 8601
}
```

#### TradeUpdate
Real-time trade execution updates.

```typescript
interface TradeUpdateEvent {
  tradeId: string; // UUID
  orderId: string; // UUID
  symbolId: string; // UUID
  symbol: string;
  side: "BUY" | "SELL";
  quantity: number;
  price: number;
  commission: number;
  commissionAsset: string;
  timestamp: string; // ISO 8601
}
```

#### OrderError
Order operation errors.

```typescript
interface OrderErrorEvent {
  orderId?: string; // UUID - if applicable
  error: string;
  message: string;
  details?: any;
}
```

## PortfolioHub (/hubs/portfolio)

**Access**: Authenticated
**Purpose**: Real-time portfolio updates and performance metrics

### Client → Server Methods

#### SubscribeToPortfolioUpdates
Subscribe to portfolio value and holdings updates.

```typescript
connection.invoke("SubscribeToPortfolioUpdates")
```

#### GetPortfolioSummary
Get current portfolio summary.

```typescript
connection.invoke("GetPortfolioSummary")
```

#### GetPositions
Get current positions.

```typescript
connection.invoke("GetPositions")
```

### Server → Client Events

#### PortfolioUpdate
Real-time portfolio value updates.

```typescript
interface PortfolioUpdateEvent {
  totalValue: number;
  totalPnL: number;
  totalPnLPercent: number;
  availableBalance: number;
  investedAmount: number;
  lastUpdated: string; // ISO 8601
  positions: PositionUpdate[];
}

interface PositionUpdate {
  symbolId: string; // UUID
  symbol: string;
  quantity: number;
  averagePrice: number;
  currentPrice: number;
  marketValue: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
  lastUpdated: string; // ISO 8601
}
```

#### PerformanceUpdate
Portfolio performance metrics updates.

```typescript
interface PerformanceUpdateEvent {
  daily: PerformanceMetrics;
  weekly: PerformanceMetrics;
  monthly: PerformanceMetrics;
  yearly: PerformanceMetrics;
  allTime: PerformanceMetrics;
}

interface PerformanceMetrics {
  return: number;
  returnPercent: number;
  maxDrawdown: number;
  sharpeRatio: number;
  winRate: number;
  profitFactor: number;
}
```

## MockTradingHub (/hubs/mock-trading)

**Access**: Anonymous
**Purpose**: Simulated trading for demo and testing purposes

### Features
- Same interface as TradingHub but with simulated executions
- No real money involved
- Useful for testing strategies and learning
- No authentication required

### Methods and Events
Identical to TradingHub but with `Mock` prefix for clarity:
- `MockPlaceOrder`
- `MockCancelOrder`
- `MockOrderUpdate`
- etc.

## Connection Management

### Connection Lifecycle

```typescript
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

// Create connection
const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:8080/hubs/market-data', {
    // For authenticated hubs
    accessTokenFactory: () => getJwtToken()
  })
  .withAutomaticReconnect([0, 2000, 10000, 30000])
  .configureLogging(LogLevel.Information)
  .build();

// Set up event handlers
connection.on('PriceUpdate', (data) => {
  console.log('Price update:', data);
});

connection.on('ConnectionStatus', (status) => {
  console.log('Connection status:', status);
});

// Start connection
try {
  await connection.start();
  console.log('Connected to hub');

  // Subscribe to updates
  await connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', ['BTC', 'ETH']);
} catch (error) {
  console.error('Connection failed:', error);
}

// Graceful shutdown
window.addEventListener('beforeunload', () => {
  connection.stop();
});
```

### Error Handling

```typescript
connection.onclose((error) => {
  if (error) {
    console.error('Connection closed with error:', error);
  } else {
    console.log('Connection closed gracefully');
  }
});

connection.onreconnecting((error) => {
  console.log('Attempting to reconnect:', error);
});

connection.onreconnected((connectionId) => {
  console.log('Reconnected with ID:', connectionId);
  // Re-subscribe to events
  resubscribeToEvents();
});
```

## Rate Limiting

| Hub | Rate Limit | Description |
|-----|------------|-------------|
| MarketDataHub | 100 subscriptions/minute | Prevents subscription spam |
| TradingHub | 60 orders/minute | Trading rate limit |
| PortfolioHub | No limit | Portfolio data access |
| MockTradingHub | 120 orders/minute | Higher limit for testing |

## Best Practices

1. **Connection Management**
   - Always implement automatic reconnection
   - Handle connection state changes gracefully
   - Resubscribe to events after reconnection

2. **Event Handling**
   - Use strongly typed interfaces for events
   - Implement proper error handling for all events
   - Debounce high-frequency updates if needed

3. **Subscription Management**
   - Unsubscribe from unused symbols to reduce bandwidth
   - Batch subscription requests when possible
   - Use asset class subscriptions for broad market monitoring

4. **Security**
   - Never expose JWT tokens in client-side logs
   - Implement token refresh logic for long-lived connections
   - Validate all received data on the client side

5. **Performance**
   - Use connection pooling for multiple hub connections
   - Implement client-side caching for frequently accessed data
   - Monitor connection health and performance metrics