# Alpaca Streaming Implementation - Validation Checklist

**Date**: 2025-01-09
**Implementation Status**: ✅ Core Complete | ⏳ Testing Pending

---

## 1. Build Validation

### Compilation
- [x] ✅ Project compiles without errors
- [x] ✅ Zero build warnings related to new code
- [x] ✅ All dependencies resolved correctly

**Command**:
```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/backend/MyTrader.Api
dotnet build --no-restore
```

**Expected Output**: `Build succeeded.`

---

## 2. Service Registration Validation

### Dependency Injection
- [ ] Alpaca services registered when feature flag enabled
- [ ] DataSourceRouter registered as singleton
- [ ] No DI registration conflicts
- [ ] MultiAssetDataBroadcastService compatible with optional IDataSourceRouter

**Validation Steps**:
1. Start application with feature flags disabled:
   ```json
   "FeatureFlags": { "EnableAlpacaStreaming": false }
   ```
2. Check logs for: "Alpaca Streaming disabled"
3. Verify no AlpacaStreamingService initialization errors

---

## 3. Zero Regression Validation (CRITICAL)

### Existing Services Must Continue Working

#### 3.1 Authentication Endpoints
- [ ] POST /api/auth/login - Returns JWT token
- [ ] POST /api/auth/register - Creates new user
- [ ] GET /api/auth/me - Returns authenticated user

**Test Commands**:
```bash
# Test login
curl -X POST http://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}'

# Expected: 200 OK with JWT token
```

#### 3.2 Database Connectivity
- [ ] PostgreSQL connection established
- [ ] Health check passes: GET /health
- [ ] Database queries execute normally

**Test Commands**:
```bash
# Health check
curl http://localhost:5002/health

# Expected: 200 OK with "status": "Healthy"
```

#### 3.3 Binance WebSocket (Crypto)
- [ ] Binance WebSocket connects
- [ ] Crypto price updates broadcast via SignalR
- [ ] No interference with stock data flow

**Validation**:
- Check logs for: "Successfully connected to Binance WebSocket"
- Open browser console, connect to SignalR hub
- Verify crypto price updates (BTCUSDT, ETHUSDT, etc.)

#### 3.4 Yahoo Finance Polling (Stocks)
- [ ] Yahoo polling service starts
- [ ] Stock prices update every 1 minute
- [ ] Data persists to market_data table

**Validation**:
- Check logs for: "Yahoo Finance Polling Service starting"
- Wait 1 minute, verify stock price updates in logs
- Query market_data table for recent entries

#### 3.5 SignalR Hubs
- [ ] DashboardHub operational
- [ ] MarketDataHub operational
- [ ] WebSocket connections established
- [ ] Price updates broadcast correctly

**Test**:
- Open browser console
- Connect to http://localhost:5002/hubs/dashboard
- Subscribe to symbol groups
- Verify price update events received

---

## 4. Feature Flag Controlled Activation

### Scenario A: Feature Flags Disabled (Default)
**Configuration**:
```json
"FeatureFlags": { "EnableAlpacaStreaming": false },
"Alpaca": { "Streaming": { "Enabled": false } }
```

**Expected Behavior**:
- [ ] No Alpaca services registered
- [ ] DataSourceRouter not created
- [ ] Yahoo polling works directly (legacy mode)
- [ ] MultiAssetDataBroadcastService uses Yahoo events
- [ ] Logs show: "Alpaca Streaming disabled"

---

### Scenario B: Feature Flags Enabled (Alpaca Active)
**Configuration**:
```json
"FeatureFlags": { "EnableAlpacaStreaming": true },
"Alpaca": {
  "Streaming": {
    "Enabled": true,
    "ApiKey": "YOUR_API_KEY",
    "ApiSecret": "YOUR_API_SECRET"
  }
}
```

**Expected Behavior**:
- [ ] Alpaca services registered
- [ ] AlpacaStreamingService attempts WebSocket connection
- [ ] DataSourceRouter created and operational
- [ ] Logs show: "Alpaca Streaming services registered"
- [ ] Logs show: "Connected to DataSourceRouter for Alpaca/Yahoo routing"

**Note**: Will fail authentication if API keys invalid (expected during development).

---

## 5. Health Endpoints Validation

### When Alpaca Disabled
- [ ] GET /api/health/alpaca - 500 error (service not registered)
- [ ] GET /api/health/datasource - 500 error (service not registered)
- [ ] GET /api/health/stocks - 500 error (service not registered)

### When Alpaca Enabled
- [ ] GET /api/health/alpaca - Returns connection status
- [ ] GET /api/health/datasource - Returns router state
- [ ] GET /api/health/stocks - Returns combined health

**Test Commands** (when enabled):
```bash
# Alpaca health
curl http://localhost:5002/api/health/alpaca

# Router health
curl http://localhost:5002/api/health/datasource

# Combined stocks health
curl http://localhost:5002/api/health/stocks
```

---

## 6. Data Flow Validation

### Scenario: Alpaca Active (Primary)
1. [ ] AlpacaStreamingService connects to WebSocket
2. [ ] Receives trade/quote/bar messages
3. [ ] Parses messages → StockPriceData
4. [ ] DataSourceRouter receives Alpaca data
5. [ ] Router validates data (price >0, volume >=0, etc.)
6. [ ] Router emits PriceDataRouted event
7. [ ] MultiAssetDataBroadcastService receives routed data
8. [ ] Broadcasts via SignalR with `source: "ALPACA"`
9. [ ] Frontend displays data with real-time indicator

### Scenario: Yahoo Fallback
1. [ ] Alpaca connection fails/unhealthy
2. [ ] DataSourceRouter transitions to FALLBACK_ACTIVE
3. [ ] Yahoo polling continues normally
4. [ ] Yahoo data flows through DataSourceRouter
5. [ ] Router emits PriceDataRouted event with `source: "YAHOO_FALLBACK"`
6. [ ] Frontend receives data with fallback indicator
7. [ ] Logs show: "Data source router state transition: PRIMARY_ACTIVE → FALLBACK_ACTIVE"

### Scenario: Alpaca Recovery
1. [ ] Alpaca reconnects successfully
2. [ ] Router applies 10s grace period
3. [ ] Router transitions back to PRIMARY_ACTIVE
4. [ ] Alpaca data resumes
5. [ ] Logs show: "Data source router state transition: FALLBACK_ACTIVE → PRIMARY_ACTIVE"

---

## 7. Error Handling Validation

### Invalid Data Rejection
- [ ] Price = 0 → Rejected (logged warning)
- [ ] Volume < 0 → Rejected (logged warning)
- [ ] Timestamp in future (>5 min) → Rejected
- [ ] Price movement >20% → Circuit breaker triggered, rejected

### Connection Failures
- [ ] Alpaca auth failure → Logged error, Yahoo fallback activated
- [ ] WebSocket timeout → Reconnect with exponential backoff
- [ ] Both sources down → BOTH_UNAVAILABLE state, last cached data served

---

## 8. Performance Validation

### Latency
- [ ] P95 latency for routed stock data <100ms
- [ ] SignalR broadcast latency <50ms
- [ ] Database write latency <200ms

### Throughput
- [ ] Handles >100 messages/second from Alpaca
- [ ] No message loss during high-volume periods
- [ ] Broadcast throttling works (max 20 updates/second/symbol)

### Memory
- [ ] No memory leaks after 1 hour operation
- [ ] Memory usage stable under load
- [ ] Connection pool sizes appropriate

---

## 9. Logging Validation

### Info Logs (Expected)
```
[INFO] Alpaca Streaming disabled (Feature Flag: False, Config Enabled: False)
[INFO] MultiAssetDataBroadcastService started successfully - listening to Binance (crypto) and stock data sources
[INFO] Yahoo Finance Polling Service starting - polling every 1 minute(s)
```

### Warning Logs (Expected in Development)
```
[WARN] SymbolManagementService not available, using fallback symbols
[WARN] AlpacaHealthController endpoints unavailable (services not registered)
```

### Error Logs (NOT Expected)
```
[ERROR] Failed to start MultiAssetDataBroadcastService
[ERROR] Error in WebSocket monitoring loop
[ERROR] Error broadcasting price update
```

---

## 10. Rollback Validation

### Emergency Rollback
If any critical issues arise:

1. **Disable Feature Flags**:
   ```json
   "FeatureFlags": { "EnableAlpacaStreaming": false }
   ```

2. **Restart Application**:
   ```bash
   dotnet run
   ```

3. **Verify Rollback**:
   - [ ] Application starts normally
   - [ ] Yahoo polling active
   - [ ] Binance WebSocket active
   - [ ] Auth endpoints working
   - [ ] No Alpaca-related errors in logs

---

## Validation Summary

### Critical Path (MUST PASS)
1. ✅ Build succeeds
2. [ ] Application starts without errors
3. [ ] Authentication works
4. [ ] Database connectivity maintained
5. [ ] Binance crypto data flows
6. [ ] Yahoo stock data flows
7. [ ] SignalR broadcasts operational
8. [ ] Feature flag disable/enable toggles services correctly

### Enhanced Features (WITH ALPACA ENABLED)
1. [ ] Alpaca WebSocket connects
2. [ ] DataSourceRouter state machine works
3. [ ] Failover to Yahoo activates
4. [ ] Recovery to Alpaca works
5. [ ] Health endpoints return accurate data
6. [ ] Data validation rules enforced

---

## Test Execution Commands

### Quick Validation Script
```bash
#!/bin/bash

echo "=== Alpaca Integration Validation ==="

# 1. Build check
echo "1. Building project..."
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/backend/MyTrader.Api
dotnet build --no-restore
if [ $? -ne 0 ]; then
  echo "❌ Build failed"
  exit 1
fi
echo "✅ Build succeeded"

# 2. Start application (background)
echo "2. Starting application..."
dotnet run &
APP_PID=$!
sleep 10

# 3. Health check
echo "3. Testing health endpoint..."
curl -f http://localhost:5002/health > /dev/null 2>&1
if [ $? -eq 0 ]; then
  echo "✅ Health check passed"
else
  echo "❌ Health check failed"
fi

# 4. Auth login test
echo "4. Testing authentication..."
curl -f -X POST http://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}' > /dev/null 2>&1
if [ $? -eq 0 ]; then
  echo "✅ Authentication working"
else
  echo "⚠️  Authentication test failed (may be expected if user doesn't exist)"
fi

# 5. Stop application
echo "5. Stopping application..."
kill $APP_PID
wait $APP_PID 2>/dev/null

echo "=== Validation Complete ==="
```

---

## Next Steps

### After Validation Passes
1. ✅ Mark implementation as complete
2. ✅ Handoff to QA team for manual testing
3. ✅ Create unit tests for each service (>80% coverage)
4. ✅ Create integration tests for end-to-end flow
5. ✅ Performance benchmarking
6. ✅ Security review
7. ✅ Deploy to staging environment

### Known Issues / Tech Debt
- [ ] Unit tests not yet written (Phase 3C)
- [ ] Integration tests not yet written (Phase 3C)
- [ ] Performance benchmarks not run
- [ ] Security review pending
- [ ] API keys need to be configured for Alpaca activation

---

**Validation Status**: ⏳ Pending
**Approver**: [QA Lead Name]
**Date**: [To be filled]
