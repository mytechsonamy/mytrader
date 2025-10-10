# Platform Stabilization Design Document

## Overview

Bu tasarım dokümanı MyTrader platformunun stabilite sorunlarını çözmek için gerekli mimari değişiklikleri ve implementasyon stratejilerini tanımlar. Ana odak noktaları database bağlantı stabilitesi, Binance WebSocket güvenilirliği, market data organizasyonu ve SignalR hub koordinasyonudur.

## Architecture

### High-Level Architecture Changes

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend Layer                           │
├─────────────────────────────────────────────────────────────┤
│  Market Data Display (Organized by Exchange)               │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┐  │
│  │   NASDAQ    │    NYSE     │    BIST     │   Binance   │  │
│  │ Accordion   │ Accordion   │ Accordion   │ Accordion   │  │
│  └─────────────┴─────────────┴─────────────┴─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                 SignalR Hub Layer                           │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │ MarketDataHub   │  │  TradingHub     │  │DashboardHub │  │
│  │ (Price Updates) │  │ (Signals)       │  │(Dashboard)  │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              Business Logic Layer                           │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐ │
│  │         Enhanced Connection Manager                     │ │
│  │  ┌─────────────────┐  ┌─────────────────────────────┐  │ │
│  │  │ DB Connection   │  │  WebSocket Connection       │  │ │
│  │  │ Pool Manager    │  │  Manager (with Circuit      │  │ │
│  │  │                 │  │  Breaker Pattern)           │  │ │
│  │  └─────────────────┘  └─────────────────────────────┘  │ │
│  └─────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │         Market Data Router & Classifier                │ │
│  │  ┌─────────────────┐  ┌─────────────────────────────┐  │ │
│  │  │ Asset Class     │  │  Market Status              │  │ │
│  │  │ Detector        │  │  Monitor                    │  │ │
│  │  └─────────────────┘  └─────────────────────────────┘  │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                Data Access Layer                            │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────────────────────┐  │
│  │ PostgreSQL      │  │  External APIs                  │  │
│  │ (with           │  │  ┌─────────────┬─────────────┐  │  │
│  │ Connection      │  │  │   Binance   │   Yahoo     │  │  │
│  │ Resilience)     │  │  │   WebSocket │   Finance   │  │  │
│  └─────────────────┘  │  └─────────────┴─────────────┘  │  │
│                       └─────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. Enhanced Database Connection Manager

```csharp
public interface IEnhancedDbConnectionManager
{
    Task<bool> EnsureConnectionAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3);
    Task ApplyPendingMigrationsAsync();
    bool IsHealthy { get; }
    event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
}

public class EnhancedDbConnectionManager : IEnhancedDbConnectionManager
{
    private readonly TradingDbContext _context;
    private readonly ILogger<EnhancedDbConnectionManager> _logger;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly CircuitBreakerPolicy _circuitBreaker;
    
    // Implementation with exponential backoff and circuit breaker
}
```

### 2. Resilient WebSocket Connection Manager

```csharp
public interface IResilientWebSocketManager
{
    Task<bool> ConnectAsync(Uri uri, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<bool> SendAsync(string message);
    bool IsConnected { get; }
    event EventHandler<WebSocketMessageEventArgs> MessageReceived;
    event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
}

public class ResilientWebSocketManager : IResilientWebSocketManager
{
    private readonly ILogger<ResilientWebSocketManager> _logger;
    private readonly CircuitBreakerPolicy _circuitBreaker;
    private readonly RetryPolicy _retryPolicy;
    private ClientWebSocket _webSocket;
    private Timer _heartbeatTimer;
    
    // Implementation with circuit breaker, exponential backoff, and heartbeat
}
```

### 3. Market Data Router and Classifier

```csharp
public interface IMarketDataRouter
{
    Task RouteMarketDataAsync(UnifiedMarketDataDto marketData);
    AssetClassCode ClassifySymbol(string symbol);
    string DetermineMarket(string symbol, AssetClassCode assetClass);
}

public class MarketDataRouter : IMarketDataRouter
{
    private readonly IHubContext<MarketDataHub> _marketDataHub;
    private readonly ILogger<MarketDataRouter> _logger;
    private readonly Dictionary<string, AssetClassCode> _symbolClassificationCache;
    
    // Routes data to correct SignalR groups based on market classification
}
```

### 4. Enhanced Binance WebSocket Service

```csharp
public interface IEnhancedBinanceWebSocketService : IBinanceWebSocketService
{
    Task<ConnectionHealthStatus> GetHealthStatusAsync();
    Task ForceReconnectAsync();
    IReadOnlyDictionary<string, DateTime> GetLastUpdateTimes();
    event EventHandler<ConnectionHealthChangedEventArgs> HealthStatusChanged;
}

public class EnhancedBinanceWebSocketService : IEnhancedBinanceWebSocketService
{
    private readonly IResilientWebSocketManager _webSocketManager;
    private readonly IMarketDataRouter _marketDataRouter;
    private readonly ILogger<EnhancedBinanceWebSocketService> _logger;
    private readonly Timer _healthCheckTimer;
    private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes;
    
    // Enhanced implementation with health monitoring and automatic recovery
}
```

## Data Models

### Connection Health Status

```csharp
public class ConnectionHealthStatus
{
    public bool IsHealthy { get; set; }
    public DateTime LastSuccessfulConnection { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public int ConsecutiveFailures { get; set; }
    public TimeSpan Uptime { get; set; }
    public string Status { get; set; } // "Connected", "Reconnecting", "Failed", "CircuitOpen"
    public Dictionary<string, object> Metrics { get; set; }
}
```

### Market Data Classification

```csharp
public class MarketDataClassification
{
    public string Symbol { get; set; }
    public AssetClassCode AssetClass { get; set; }
    public string Market { get; set; } // "BINANCE", "NASDAQ", "NYSE", "BIST"
    public string SignalRGroup { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastClassified { get; set; }
}
```

### Enhanced Market Data DTO

```csharp
public class EnhancedUnifiedMarketDataDto : UnifiedMarketDataDto
{
    public string Market { get; set; }
    public AssetClassCode AssetClass { get; set; }
    public MarketStatus MarketStatus { get; set; }
    public DateTime LastUpdate { get; set; }
    public string DataSource { get; set; }
    public bool IsStale { get; set; }
    public TimeSpan DataAge => DateTime.UtcNow - LastUpdate;
}
```

## Error Handling

### Circuit Breaker Pattern Implementation

```csharp
public class CircuitBreakerConfiguration
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public int SuccessThreshold { get; set; } = 3;
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(2);
}

public enum CircuitBreakerState
{
    Closed,    // Normal operation
    Open,      // Failing fast
    HalfOpen   // Testing if service recovered
}
```

### Retry Policy Configuration

```csharp
public class RetryPolicyConfiguration
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public List<Type> RetriableExceptions { get; set; }
}
```

### Comprehensive Error Logging

```csharp
public class ErrorContext
{
    public string Operation { get; set; }
    public string Component { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; }
}

public interface IEnhancedLogger
{
    void LogDatabaseError(Exception exception, string operation, ErrorContext context);
    void LogWebSocketError(Exception exception, string connectionId, ErrorContext context);
    void LogPerformanceMetrics(string operation, TimeSpan duration, Dictionary<string, object> metrics);
    void LogRecoveryEvent(string component, string recoveryAction, TimeSpan downtime);
}
```

## Testing Strategy

### Unit Tests

1. **Database Connection Manager Tests**
   - Connection retry logic
   - Circuit breaker behavior
   - Migration application
   - Error handling scenarios

2. **WebSocket Manager Tests**
   - Connection establishment
   - Reconnection logic
   - Message processing
   - Heartbeat functionality

3. **Market Data Router Tests**
   - Symbol classification
   - Data routing logic
   - SignalR group management
   - Error handling

### Integration Tests

1. **End-to-End Data Flow Tests**
   - Binance WebSocket → Database → SignalR → Frontend
   - Market data classification and routing
   - Error recovery scenarios

2. **Performance Tests**
   - Memory usage under load
   - Connection stability over time
   - Message throughput testing

3. **Resilience Tests**
   - Database connection failures
   - WebSocket disconnections
   - Network interruptions
   - High load scenarios

### Load Testing

1. **WebSocket Connection Load**
   - 1000+ concurrent connections
   - High-frequency message processing
   - Memory leak detection

2. **Database Performance**
   - Concurrent query execution
   - Connection pool exhaustion
   - Long-running operations

## Implementation Phases

### Phase 1: Database Stability (Week 1)
- Implement Enhanced Database Connection Manager
- Add circuit breaker pattern for database operations
- Implement automatic migration application
- Add comprehensive database error logging

### Phase 2: WebSocket Resilience (Week 1-2)
- Implement Resilient WebSocket Manager
- Enhance Binance WebSocket Service with health monitoring
- Add automatic reconnection with exponential backoff
- Implement heartbeat mechanism

### Phase 3: Market Data Organization (Week 2)
- Implement Market Data Router and Classifier
- Update SignalR hubs for proper data routing
- Modify frontend to display data in organized accordions
- Add market status monitoring

### Phase 4: Performance Optimization (Week 3)
- Implement memory management improvements
- Add performance monitoring and metrics
- Optimize database queries and connection pooling
- Add caching layers where appropriate

### Phase 5: Testing and Validation (Week 3-4)
- Comprehensive testing of all components
- Load testing and performance validation
- Error scenario testing
- Documentation and deployment preparation

## Monitoring and Observability

### Health Check Endpoints

```csharp
public class PlatformHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var checks = new Dictionary<string, object>
        {
            ["database"] = await CheckDatabaseHealth(),
            ["binance_websocket"] = await CheckBinanceWebSocketHealth(),
            ["signalr_hubs"] = await CheckSignalRHubsHealth(),
            ["memory_usage"] = GetMemoryUsage(),
            ["uptime"] = GetUptime()
        };
        
        // Return overall health status
    }
}
```

### Performance Metrics

- Database connection pool utilization
- WebSocket connection uptime percentage
- Message processing latency
- Memory usage trends
- Error rates by component

### Alerting Thresholds

- Database connection failures > 3 in 5 minutes
- WebSocket uptime < 95% over 1 hour
- Memory usage > 1GB sustained for 10 minutes
- Message processing latency > 500ms average over 5 minutes