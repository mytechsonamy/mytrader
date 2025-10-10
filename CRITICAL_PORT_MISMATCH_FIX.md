# CRITICAL PRODUCTION FAILURE FIX: Port Mismatch

**Date**: 2025-10-09
**Priority**: P0 - CRITICAL
**Status**: RESOLVED

## Root Cause

Mobile app was configured to connect to backend on **port 5002**, but backend is actually running on **port 8080**.

This caused complete system failure:
- Login failures (network error, not authentication error)
- Price data unavailable (all showing "--")
- WebSocket connection failures
- All API calls timing out

## Evidence

### Backend Configuration (docker-compose.yml)
```yaml
mytrader_api:
  ports:
    - "8080:8080"  # Backend exposed on port 8080
  environment:
    - ASPNETCORE_URLS=http://+:8080
```

### Mobile App Configuration (BEFORE FIX)
```json
// frontend/mobile/app.json
"extra": {
  "API_BASE_URL": "http://192.168.68.102:5002/api",     // WRONG PORT
  "AUTH_BASE_URL": "http://192.168.68.102:5002/api",    // WRONG PORT
  "WS_BASE_URL": "http://192.168.68.102:5002/hubs/market-data"  // WRONG PORT
}
```

### Verification Tests
```bash
# Port 5002 - NOT REACHABLE
curl http://192.168.68.102:5002/api/health
# Result: Connection refused

# Port 8080 - WORKING
curl http://192.168.68.102:8080/api/health
# Result: {"isHealthy":true,"status":"Healthy",...}
```

## Fix Applied

Updated `frontend/mobile/app.json` to use correct port:

```json
"extra": {
  "API_BASE_URL": "http://192.168.68.102:8080/api",
  "AUTH_BASE_URL": "http://192.168.68.102:8080/api",
  "WS_BASE_URL": "http://192.168.68.102:8080/hubs/market-data"
}
```

## Verification

### Backend Health Check
```bash
curl -s http://192.168.68.102:8080/api/health | jq .
```
**Result**: Status 200, isHealthy: true

### Authentication Endpoint
```bash
curl -s -X POST "http://192.168.68.102:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"test"}'
```
**Result**: Returns auth error (expected - endpoint is reachable)

### CORS Configuration
```bash
curl -s -H "Origin: http://localhost" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS -i "http://192.168.68.102:8080/api/symbols"
```
**Result**: Status 204, CORS headers present

### Network Connectivity
```bash
nc -zv 192.168.68.102 8080
```
**Result**: Connection succeeded

### Symbols API
```bash
curl -s "http://192.168.68.102:8080/api/symbols?assetClass=CRYPTO" | jq '.symbols | keys | length'
```
**Result**: Returns 19 symbols (BTC, ETH, etc.)

## Backend Status

### Docker Containers
```
NAMES               STATUS                PORTS
mytrader_api        Up 17 minutes         0.0.0.0:8080->8080/tcp
mytrader_postgres   Up 2 days (healthy)   0.0.0.0:5434->5432/tcp
```

### SignalR Hubs Registered
- /hubs/trading
- /hubs/dashboard
- /hubs/mock-trading
- /hubs/market-data  (mobile app connects here)
- /hubs/portfolio

### CORS Policy (Development Mode)
- Allows: localhost, 127.0.0.1, 192.168.*, 10.*, 172.*, *.local
- Allows: expo.io, *.expo.dev
- Allows: All methods, headers, credentials
- Mobile app origin (null/empty) explicitly allowed

## User Impact

**Before Fix**:
- Login: "Email veya şifre hatalı" (network failure masked as auth error)
- Dashboard: All prices showing "--" (no data)
- Market Data: "Veri yok" for all asset classes
- WebSocket: Complete connection failure

**After Fix**:
- Login: Should work correctly
- Dashboard: Real-time prices should display
- Market Data: All asset classes should load
- WebSocket: Real-time updates should work

## Action Required

User must restart mobile app to pick up new configuration:

### iOS Simulator
```bash
# Stop Expo
# In terminal running Expo: Ctrl+C

# Clear cache and restart
cd frontend/mobile
npm start -- --clear
```

### Physical Device
1. Force quit myTrader app
2. Reopen app
3. If issues persist, reinstall from Expo

## Testing Script

```bash
#!/bin/bash
# Test mobile app connectivity to backend

BASE_URL="http://192.168.68.102:8080"

echo "=== Mobile Backend Connectivity Test ==="
echo ""

echo "1. Health Check"
curl -s "$BASE_URL/api/health" | jq '.isHealthy, .status'
echo ""

echo "2. Symbols API (CRYPTO)"
curl -s "$BASE_URL/api/symbols?assetClass=CRYPTO" | jq '.symbols | keys | length'
echo ""

echo "3. Symbols API (STOCK)"
curl -s "$BASE_URL/api/symbols?assetClass=STOCK" | jq '.symbols | keys | length'
echo ""

echo "4. CORS Test"
curl -s -I -H "Origin: http://localhost" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS "$BASE_URL/api/symbols" | grep -i "access-control"
echo ""

echo "5. Port Connectivity"
nc -zv 192.168.68.102 8080 2>&1
echo ""

echo "=== Test Complete ==="
```

## Lessons Learned

1. **Configuration Drift**: Mobile app config became stale when backend port changed
2. **Error Masking**: Network errors appearing as authentication errors
3. **Need for Config Validation**: Should add startup check that pings backend health
4. **Documentation**: Port configuration should be documented in README

## Recommended Improvements

### 1. Add Backend Health Check on App Startup
```typescript
// frontend/mobile/src/config.ts
export const validateBackendConnection = async () => {
  try {
    const response = await fetch(`${API_BASE_URL}/health`);
    if (!response.ok) {
      console.error('Backend health check failed:', response.status);
      return false;
    }
    return true;
  } catch (error) {
    console.error('Cannot reach backend:', error);
    return false;
  }
};
```

### 2. Add Configuration Documentation
Create `frontend/mobile/CONFIG.md` with:
- Backend URL configuration
- Port mappings
- Network requirements
- Troubleshooting guide

### 3. Add Startup Validation
Show user-friendly error if backend is unreachable on app startup.

## Files Modified

- `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/frontend/mobile/app.json`
  - Changed port from 5002 to 8080 in all URL configurations

## Deployment Impact

This fix requires:
- No backend changes (backend already running on correct port)
- Mobile app restart to pick up new config
- No database changes
- No infrastructure changes

## Sign-off

Root Cause: Port mismatch (5002 vs 8080)
Fix: Updated mobile app.json with correct port
Status: Ready for user testing
Verification: All backend endpoints reachable on port 8080
