# MyTrader Web Frontend API Requirements Analysis

## Executive Summary

This document provides a comprehensive analysis of API requirements for MyTrader's web frontend redesign, based on analysis of 33+ backend controllers and mobile app integration patterns. The analysis identifies 85+ available endpoints across authentication, market data, trading, portfolio management, and real-time features.

## 1. API Architecture Overview

### Backend Structure Analysis
- **Controllers**: 33+ controllers covering all business domains
- **Real-time Hubs**: SignalR hubs for market data, dashboard, and trading updates
- **Authentication**: JWT-based with refresh tokens and session management
- **Data Sources**: Multi-asset support (Crypto, BIST, NASDAQ, Forex)
- **API Versioning**: Dual routes (legacy + v1 versioned)

### Mobile Integration Patterns
- Intelligent endpoint fallback system with URL candidates
- Enhanced WebSocket service with auto-reconnection
- Comprehensive error handling and retry mechanisms
- Real-time price context with legacy compatibility

## 2. Public Endpoints (No Authentication Required)

### 2.1 Market Data Endpoints

#### Real-time Market Data
```http
GET /api/v1/market-data/realtime/{symbolId}
GET /api/v1/market-data/batch (POST with symbol IDs)
GET /api/v1/market-data/overview
GET /api/v1/market-data/top-movers?assetClass={class}&limit={limit}
GET /api/v1/market-data/top-by-volume?perClass={count}
GET /api/v1/market-data/popular?limit={limit}
```

#### Historical Data
```http
GET /api/v1/market-data/historical/{symbolId}?interval={interval}&startTime={start}&endTime={end}
GET /api/v1/market-data/statistics/{symbolId}
```

#### Asset-Specific Endpoints
```http
# Cryptocurrency
GET /api/market-data/crypto?symbols={symbols}
GET /api/market-data/alpaca/symbols?assetClass=CRYPTO

# Turkish Stocks (BIST)
GET /api/market-data/bist?symbols={symbols}&limit={limit}
GET /api/market-data/bist/{symbol}
GET /api/market-data/bist/overview
GET /api/market-data/bist/top-movers
GET /api/market-data/bist/sectors
GET /api/market-data/bist/search?q={query}&limit={limit}
GET /api/market-data/bist/status

# US Stocks (NASDAQ)
GET /api/market-data/nasdaq?symbols={symbols}
```

#### Health & Status
```http
GET /api/market-data/alpaca/health
GET /api/v1/markets/status
GET /api/health
```

### 2.2 Asset & Symbol Discovery
```http
GET /api/v1/asset-classes
GET /api/v1/asset-classes/active
GET /api/v1/markets
GET /api/v1/markets/active
GET /api/v1/symbols?page={page}&pageSize={size}&assetClassId={id}
GET /api/v1/symbols/search?q={query}&assetClass={class}&limit={limit}
GET /api/v1/symbols/by-asset-class/{assetClassId}
```

### 2.3 Competition & Leaderboard (Public Views)
```http
GET /api/v1/competition/leaderboard?period={period}&limit={limit}
GET /api/v1/competition/stats
GET /api/v1/competition/status
GET /api/v1/competition/competition-leaderboard
```

### 2.4 News & Information
```http
GET /api/v1/news/market?assetClass={class}&limit={limit}&offset={offset}
GET /api/v1/news/crypto?limit={limit}
GET /api/v1/news/stocks?market={market}&limit={limit}
GET /api/v1/news/search?q={query}&limit={limit}
```

### 2.5 Legacy Price Endpoints
```http
GET /api/prices/live
GET /api/prices/{symbol}
GET /api/MockMarket/symbols
```

## 3. Authenticated Endpoints (Login Required)

### 3.1 Authentication & User Management
```http
POST /api/auth/login
POST /api/auth/register
POST /api/auth/verify-email
POST /api/auth/refresh
GET /api/auth/me
PUT /api/auth/me
POST /api/auth/logout
GET /api/auth/sessions
POST /api/auth/logout-all
```

#### Password Management
```http
POST /api/auth/request-password-reset
POST /api/auth/verify-password-reset
POST /api/auth/reset-password
```

### 3.2 Dashboard & User Preferences
```http
GET /api/dashboard/preferences?includeMarketData={bool}
POST /api/dashboard/preferences
PUT /api/dashboard/preferences/{id}
DELETE /api/dashboard/preferences/{id}
POST /api/dashboard/preferences/bulk
GET /api/dashboard/available-assets?assetClass={class}&search={query}
```

### 3.3 Portfolio Management
```http
GET /api/portfolio
GET /api/portfolio/{portfolioId}
POST /api/portfolio
PUT /api/portfolio/{portfolioId}
DELETE /api/portfolio/{portfolioId}
GET /api/portfolio/{portfolioId}/positions
GET /api/portfolio/{portfolioId}/transactions
POST /api/portfolio/transactions
GET /api/portfolio/{portfolioId}/analytics
GET /api/portfolio/{portfolioId}/performance
GET /api/portfolio/{portfolioId}/risk
POST /api/portfolio/export
```

### 3.4 Trading & Strategies
```http
GET /api/v1/strategies/my-strategies
POST /api/v1/strategies/create
POST /api/v1/strategies/{strategyId}/test
POST /api/v1/strategies/{strategyId}/activate
```

### 3.5 Watchlists & Alerts
```http
GET /api/v1/watchlists
POST /api/v1/watchlists
PUT /api/v1/watchlists/{watchlistId}
DELETE /api/v1/watchlists/{watchlistId}
GET /api/alerts
POST /api/alerts
PUT /api/alerts/{id}
DELETE /api/alerts/{id}
```

### 3.6 Gamification & Achievements
```http
GET /api/v1/competition/achievements
GET /api/v1/competition/performance-history
POST /api/v1/competition/record-performance
GET /api/v1/competition/rank
GET /api/v1/competition/my-ranking
POST /api/v1/competition/join
```

### 3.7 Real-time Subscriptions
```http
POST /api/market-data/subscribe
POST /api/market-data/unsubscribe
GET /api/market-data/providers/health
GET /api/market-data/providers
```

### 3.8 Education & Learning
```http
GET /api/education/paths
GET /api/education/modules?category={category}
GET /api/education/modules/{id}
POST /api/education/modules/{id}/complete
GET /api/education/modules/{moduleId}/quiz
POST /api/education/quizzes/{quizId}/submit
```

## 4. Real-time Data Requirements

### 4.1 SignalR Hub Connections

#### Market Data Hub (`/hubs/market-data`)
**Events Sent to Clients:**
- `ReceivePriceUpdate` / `PriceUpdate`
- `ReceiveBatchPriceUpdate`
- `MarketDataUpdate`
- `ReceiveMarketStatusUpdate`

**Client Methods:**
- `SubscribeToPriceUpdates(assetClass, symbols[])`
- `Subscribe(payload)`
- `Unsubscribe(payload)`
- `Ping` (heartbeat)

#### Dashboard Hub (`/hubs/dashboard`)
**Events:**
- `ReceiveSignalUpdate`
- `ReceiveMarketData`
- `SubscriptionConfirmed`
- `SubscriptionError`
- `Heartbeat`

### 4.2 WebSocket Authentication
- Bearer token via `accessTokenFactory`
- Query parameter: `?token={jwt}`
- Header: `Authorization: Bearer {jwt}`

### 4.3 Supported Asset Classes
- **CRYPTO**: Bitcoin, Ethereum, Altcoins
- **STOCK**: BIST (Turkish), NASDAQ, NYSE
- **FOREX**: Major currency pairs
- **COMMODITY**: Gold, Oil, etc.

## 5. API Response Formats

### 5.1 Standard Response Wrapper
```typescript
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  timestamp: string;
  errors?: { [field: string]: string[] };
}
```

### 5.2 Market Data Response
```typescript
interface UnifiedMarketDataDto {
  symbolId: string;
  symbol: string;
  price: number;
  change: number;
  changePercent: number;
  timestamp: string;
  marketStatus: 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_HOURS';
  dataSource: 'REAL_TIME' | 'DELAYED' | 'SIMULATED';
  lastUpdated: string;
  volume?: number;
  high24h?: number;
  low24h?: number;
}
```

### 5.3 Error Response Format
```typescript
interface ErrorResponse {
  success: false;
  message: string;
  code?: string;
  status: number;
  timestamp: string;
  errors?: { [field: string]: string[] };
}
```

## 6. Performance Requirements

### 6.1 Response Time Targets
- **Market Data**: < 100ms (cached data)
- **Authentication**: < 200ms
- **Dashboard Data**: < 300ms
- **Historical Data**: < 500ms
- **Search**: < 250ms

### 6.2 Real-time Update Latency
- **Price Updates**: < 100ms
- **WebSocket Reconnection**: < 2 seconds
- **Heartbeat Interval**: 30 seconds

### 6.3 Caching Strategy
- **Market Data**: Redis cache, 1-second TTL
- **Symbol Metadata**: 5-minute cache
- **Static Data**: 1-hour cache
- **CDN**: Static assets and images

## 7. Security Considerations

### 7.1 Rate Limiting
- **Public endpoints**: 100 requests/minute
- **Authenticated**: 500 requests/minute
- **WebSocket connections**: 10 per user

### 7.2 Data Access Control
- **Public**: Market data, news, basic competition stats
- **Authenticated**: Personal portfolios, strategies, detailed analytics
- **Premium**: Advanced features, real-time data

### 7.3 CORS Configuration
```javascript
{
  origin: ["https://mytrader.com", "https://app.mytrader.com"],
  credentials: true,
  methods: ["GET", "POST", "PUT", "DELETE"],
  allowedHeaders: ["Authorization", "Content-Type", "X-Client-Type"]
}
```

## 8. Mobile-Web Feature Parity

### 8.1 Core Features (Available on Both)
- ✅ Market data viewing
- ✅ User authentication
- ✅ Portfolio tracking
- ✅ Competition leaderboard
- ✅ Real-time price updates
- ✅ Basic trading strategies

### 8.2 Web-Enhanced Features
- 📊 Advanced charting
- 🔍 Enhanced search & filtering
- 📱 Multi-column layouts
- ⌨️ Keyboard shortcuts
- 📤 Export capabilities
- 🔗 Social sharing

### 8.3 Mobile-Specific Features
- 📱 Push notifications
- 📍 Location-based features
- 📸 QR code scanning
- 🔐 Biometric authentication

## 9. Integration Recommendations

### 9.1 API Client Implementation
```typescript
class MyTraderApiClient {
  private baseURL: string;
  private token: string | null;

  // Implement retry logic with exponential backoff
  // Handle ApiResponse<T> unwrapping
  // Provide TypeScript interfaces for all endpoints
  // Support real-time subscriptions
}
```

### 9.2 State Management
- Use React Query/SWR for server state
- Implement optimistic updates
- Cache frequently accessed data
- Handle offline scenarios

### 9.3 Error Handling
- Implement global error boundary
- Provide user-friendly error messages
- Log errors for monitoring
- Graceful degradation for API failures

## 10. Next Steps

1. **Phase 1**: Implement core public endpoints for market data
2. **Phase 2**: Add authentication and user-specific features
3. **Phase 3**: Integrate real-time WebSocket connections
4. **Phase 4**: Add advanced features and optimizations

---

**Document Version**: 1.0
**Last Updated**: September 28, 2025
**Author**: MyTrader Business Analysis Team