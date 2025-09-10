# MyTrader API Documentation

## Overview

MyTrader API is a comprehensive trading platform backend built with ASP.NET Core 9.0 and PostgreSQL. It provides endpoints for authentication, market data, trading strategies, gamification, and real-time communication via SignalR.

## Base URL
- **Development**: `http://localhost:5002`
- **Production**: *To be configured*

## Authentication

The API uses JWT Bearer token authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

## Core Endpoints

### 1. Health & Status

#### GET /
Returns basic API information and available endpoints.

**Response:**
```json
{
  "name": "MyTrader API",
  "version": "1.0.0", 
  "status": "running",
  "timestamp": "2025-09-10T18:38:29.829625Z",
  "endpoints": {
    "health": "/health",
    "auth": "/api/auth",
    "swagger": "/swagger",
    "hubs": "/hubs/trading"
  }
}
```

#### GET /health
Health check endpoint for monitoring.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-09-10T18:38:33.921983Z", 
  "message": "MyTrader API is running"
}
```

### 2. Authentication Endpoints

#### POST /api/auth/register
Register a new user account.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

#### POST /api/auth/login
Authenticate user and receive JWT token.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

### 3. Market Data Endpoints

#### GET /api/market/candles/{symbol}
Get historical candle data for a trading symbol.

**Parameters:**
- `symbol` (path): Trading pair symbol (e.g., BTCUSDT, ETHUSDT)
- `interval` (query): Time interval (1m, 5m, 1h, 1d) - default: 1h
- `limit` (query): Number of candles (max 1000) - default: 100

**Example Request:**
```
GET /api/market/candles/BTCUSDT?interval=1h&limit=100
```

**Response:**
```json
{
  "symbol": "BTCUSDT",
  "interval": "1h",
  "candles": [
    {
      "timestamp": "2025-09-10T17:00:00Z",
      "open": 64500.00,
      "high": 64800.00,
      "low": 64200.00,
      "close": 64650.00,
      "volume": 125.45
    }
  ]
}
```

#### GET /api/market/price/{symbol}
Get current price for a symbol.

**Response:**
```json
{
  "symbol": "BTCUSDT",
  "price": 64650.00,
  "change24h": 2.5,
  "changePercent24h": 0.039,
  "timestamp": "2025-09-10T18:00:00Z"
}
```

### 4. Gamification Endpoints

#### GET /api/gamification/achievements/{userId}
Get user achievements and progress.

**Response:**
```json
{
  "userId": "uuid",
  "achievements": [
    {
      "id": "uuid",
      "name": "First Trade",
      "description": "Complete your first trade",
      "category": "Trading",
      "points": 100,
      "isUnlocked": true,
      "unlockedAt": "2025-09-10T15:30:00Z"
    }
  ],
  "totalPoints": 850,
  "level": 3,
  "nextLevelPoints": 1000
}
```

#### GET /api/gamification/leaderboard
Get global leaderboard rankings.

**Parameters:**
- `limit` (query): Number of entries (max 100) - default: 10
- `timeframe` (query): weekly, monthly, all-time - default: all-time

**Response:**
```json
{
  "timeframe": "all-time",
  "leaderboard": [
    {
      "rank": 1,
      "userId": "uuid",
      "username": "TradingPro",
      "totalPoints": 5240,
      "level": 12,
      "totalReturn": 0.25
    }
  ]
}
```

### 5. Indicator & Signal Endpoints

#### GET /api/indicators/rsi/{symbol}
Calculate RSI (Relative Strength Index) for a symbol.

**Parameters:**
- `symbol` (path): Trading pair symbol
- `period` (query): RSI period (default: 14)

**Response:**
```json
{
  "symbol": "BTCUSDT",
  "indicator": "RSI",
  "period": 14,
  "value": 65.4,
  "signal": "Neutral",
  "timestamp": "2025-09-10T18:00:00Z"
}
```

#### GET /api/indicators/macd/{symbol}
Calculate MACD indicator for a symbol.

**Parameters:**
- `fastPeriod` (query): Fast EMA period (default: 12)
- `slowPeriod` (query): Slow EMA period (default: 26)
- `signalPeriod` (query): Signal line period (default: 9)

**Response:**
```json
{
  "symbol": "BTCUSDT",
  "indicator": "MACD",
  "macdLine": 125.6,
  "signalLine": 98.3,
  "histogram": 27.3,
  "signal": "Buy",
  "timestamp": "2025-09-10T18:00:00Z"
}
```

### 6. Strategy & Backtest Endpoints

#### POST /api/backtests/run
Execute a strategy backtest.

**Request Body:**
```json
{
  "strategyId": "uuid",
  "symbolId": "uuid", 
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "initialBalance": 10000.00,
  "timeframe": "1h",
  "parameters": {
    "rsiPeriod": 14,
    "rsiOverbought": 70,
    "rsiOversold": 30
  }
}
```

**Response:**
```json
{
  "backtestId": "uuid",
  "status": "Running",
  "message": "Backtest queued for execution"
}
```

#### GET /api/backtests/{backtestId}/results
Get backtest results.

**Response:**
```json
{
  "id": "uuid",
  "status": "Completed",
  "totalReturn": 2500.00,
  "totalReturnPercentage": 25.0,
  "maxDrawdown": 0.08,
  "winRate": 65.5,
  "totalTrades": 145,
  "winningTrades": 95,
  "sharpeRatio": 1.85,
  "trades": [...],
  "equityCurve": [...]
}
```

### 7. Price Alert Endpoints

#### POST /api/prices/alerts
Create a price alert.

**Request Body:**
```json
{
  "symbol": "BTCUSDT",
  "alertType": "PRICE_ABOVE",
  "targetPrice": 70000.00,
  "message": "BTC reached $70k!"
}
```

#### GET /api/prices/alerts
Get user's active price alerts.

**Response:**
```json
{
  "alerts": [
    {
      "id": "uuid",
      "symbol": "BTCUSDT",
      "alertType": "PRICE_ABOVE",
      "targetPrice": 70000.00,
      "isActive": true,
      "createdAt": "2025-09-10T18:00:00Z"
    }
  ]
}
```

## Real-time Communication (SignalR)

### WebSocket Hub: /hubs/trading

Connect to the trading hub for real-time updates:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5002/hubs/trading", {
        accessTokenFactory: () => yourJWTToken
    })
    .build();

// Subscribe to price updates
connection.invoke("SubscribeToPrices", ["BTCUSDT", "ETHUSDT"]);

// Listen for price updates
connection.on("PriceUpdate", (data) => {
    console.log(`${data.symbol}: $${data.price}`);
});
```

### Available Hub Methods:

#### Client to Server:
- `SubscribeToPrices(symbols[])` - Subscribe to price updates
- `UnsubscribeFromPrices(symbols[])` - Unsubscribe from price updates
- `JoinGroup(groupName)` - Join a trading group
- `LeaveGroup(groupName)` - Leave a trading group

#### Server to Client:
- `PriceUpdate(priceData)` - Real-time price updates
- `SignalAlert(signalData)` - Trading signal notifications
- `OrderUpdate(orderData)` - Order status updates

## Error Handling

All endpoints return consistent error responses:

```json
{
  "error": {
    "code": "INVALID_SYMBOL",
    "message": "The provided symbol is not supported",
    "details": "Symbol 'INVALID' is not found in supported trading pairs"
  },
  "timestamp": "2025-09-10T18:00:00Z"
}
```

### Common Error Codes:
- `UNAUTHORIZED` (401) - Invalid or missing authentication token
- `FORBIDDEN` (403) - Insufficient permissions
- `NOT_FOUND` (404) - Resource not found
- `VALIDATION_ERROR` (400) - Invalid request data
- `RATE_LIMIT_EXCEEDED` (429) - Too many requests
- `INTERNAL_ERROR` (500) - Server error

## Rate Limiting

- **General endpoints**: 100 requests per minute
- **Market data endpoints**: 200 requests per minute  
- **Authentication endpoints**: 10 requests per minute

Rate limit headers are included in responses:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1641904800
```

## Pagination

Endpoints returning lists support pagination:

```
GET /api/endpoint?page=1&limit=50&sortBy=createdAt&sortOrder=desc
```

**Response includes pagination metadata:**
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "limit": 50,
    "total": 1250,
    "pages": 25,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

## Development & Testing

### Running the API
```bash
cd backend/MyTrader.Api
dotnet run --urls="http://localhost:5002"
```

### Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection="Host=localhost;Port=5434;Database=mytrader;Username=postgres;Password=password"
Jwt__SecretKey="your-secret-key-here"
```

### Testing Endpoints
```bash
# Health check
curl http://localhost:5002/health

# API info
curl http://localhost:5002/

# Market data (example)
curl http://localhost:5002/api/market/candles/BTCUSDT?interval=1h&limit=10
```

## Production Deployment

For production deployment, ensure:

1. **Database Connection**: Update connection string for production database
2. **JWT Security**: Use a strong, randomly generated secret key
3. **HTTPS**: Enable SSL/TLS certificates
4. **CORS**: Configure allowed origins for frontend
5. **Rate Limiting**: Implement appropriate rate limits
6. **Monitoring**: Set up logging and health checks
7. **Environment Variables**: Use secure environment variable management

### Docker Deployment
```bash
# Build the Docker image
docker build -t mytrader-api .

# Run with environment variables
docker run -p 5002:5002 \
  -e ConnectionStrings__DefaultConnection="production-connection-string" \
  -e Jwt__SecretKey="production-jwt-secret" \
  mytrader-api
```

---

*Last updated: September 10, 2025*