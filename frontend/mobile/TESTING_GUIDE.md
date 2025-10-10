# Testing Guide: Dynamic Symbol Loading

## Quick Start

### 1. Prerequisites

Before testing, ensure:
- [ ] Backend API is running on the configured URL
- [ ] Symbol preference endpoints are implemented
- [ ] Database has default symbols configured
- [ ] Mobile app can reach the backend (same network or proper port forwarding)

### 2. Environment Configuration

Check your `config.ts` or `app.json` for the correct API URL:

```typescript
// src/config.ts
export const API_BASE_URL = 'http://192.168.68.103:5002/api';
```

Or in `app.json`:
```json
{
  "expo": {
    "extra": {
      "API_BASE_URL": "http://192.168.68.103:5002/api"
    }
  }
}
```

### 3. Run Backend Validation Test

```bash
cd frontend/mobile
node test-dynamic-symbols.js
```

**Expected Output**:
```
============================================================
Dynamic Symbol Loading Test
============================================================
API Base URL: http://192.168.68.103:5002/api

Test 1: Fetching default CRYPTO symbols...
✓ Success: Received 9 symbols
  Symbols: BTC, ETH, XRP, SOL, AVAX, SUI, ENA, UNI, BNB

Test 2: Fetching user symbol preferences (without auth)...
✓ Expected: Requires authentication (401)

Test 3: Fetching symbols by asset class (legacy endpoint)...
✓ Success: Received 9 symbols
  Sample: BTC (Bitcoin), ETH (Ethereum), XRP (Ripple)

Test 4: Cache simulation (local only)...
✓ Cache key format: symbols_cache_v1_crypto_test-user-123
✓ Default cache key: symbols_cache_v1_crypto_default

============================================================
Test Summary
============================================================
Passed: 4/4
Failed: 0/4

✓ All tests passed!
```

## Manual Testing Scenarios

### Scenario 1: First App Launch (No Cache)

**Steps**:
1. Clear app data: Settings → Apps → myTrader → Clear Data
2. Launch app
3. Open Dashboard

**Expected Behavior**:
- App fetches symbols from backend API
- Default symbols load (BTC, ETH, XRP, SOL, AVAX, SUI, ENA, UNI, BNB)
- Symbols cached in AsyncStorage
- WebSocket subscribes to all loaded symbols
- Price updates appear in real-time

**Debug Logs to Check**:
```
[API] Fetching default symbols for CRYPTO
[API] Received 9 default symbols for CRYPTO
[SymbolCache] Cached 9 symbols for CRYPTO
[PriceContext] Loading crypto symbols from API...
[PriceContext] Auto-subscribing to 9 CRYPTO symbols: BTCUSDT, ETHUSDT, ...
[Dashboard] Loaded 9 crypto symbols: BTC, ETH, XRP, SOL, AVAX, SUI, ENA, UNI, BNB
```

### Scenario 2: Second App Launch (With Cache)

**Steps**:
1. Close app (force quit)
2. Relaunch app within 5 minutes
3. Open Dashboard

**Expected Behavior**:
- App loads symbols from cache instantly
- No API call for symbols (uses cached data)
- WebSocket subscribes to cached symbols
- Price updates work normally

**Debug Logs to Check**:
```
[SymbolCache] Cache hit for CRYPTO: 9 symbols
[API] Using cached symbols for CRYPTO: 9 symbols
[PriceContext] Auto-subscribing to 9 CRYPTO symbols: BTCUSDT, ETHUSDT, ...
[Dashboard] Loaded 9 crypto symbols: BTC, ETH, XRP, SOL, AVAX, SUI, ENA, UNI, BNB
```

### Scenario 3: Offline Mode

**Steps**:
1. Launch app with network
2. Let symbols load and cache
3. Enable Airplane Mode
4. Force quit and relaunch app
5. Open Dashboard

**Expected Behavior**:
- App loads from cache
- Symbols display correctly
- Price updates don't work (offline)
- No crashes or errors
- User sees last known prices

**Debug Logs to Check**:
```
[SymbolCache] Cache hit for CRYPTO: 9 symbols
[API] Using cached symbols for CRYPTO: 9 symbols
[PriceContext] Failed to subscribe to price updates: [network error]
[Dashboard] Loaded 9 crypto symbols: BTC, ETH, XRP, SOL, AVAX, SUI, ENA, UNI, BNB
```

### Scenario 4: Stale Cache Fallback

**Steps**:
1. Launch app and let cache expire (wait 6+ minutes)
2. Disconnect from network
3. Force quit and relaunch app

**Expected Behavior**:
- App tries to fetch from API (fails - offline)
- Falls back to stale cache
- Symbols load from expired cache
- Warning logged about using stale cache

**Debug Logs to Check**:
```
[SymbolCache] Cache expired for CRYPTO
[API] Failed to fetch default symbols for CRYPTO: [network error]
[API] Using stale cache for CRYPTO due to error
[Dashboard] Loaded 9 crypto symbols: BTC, ETH, XRP, SOL, AVAX, SUI, ENA, UNI, BNB
```

### Scenario 5: Emergency Minimal Fallback

**Steps**:
1. Clear app data completely
2. Ensure backend is unreachable
3. Launch app

**Expected Behavior**:
- App tries to fetch from API (fails)
- No cache available
- Falls back to minimal emergency list (BTC + ETH only)
- Warning logged about minimal fallback
- App remains functional with limited symbols

**Debug Logs to Check**:
```
[API] Failed to fetch default symbols for CRYPTO: [network error]
[API] Using MINIMAL emergency fallback for CRYPTO - network connectivity issue
[Dashboard] Loaded 2 crypto symbols: BTC, ETH
```

### Scenario 6: User-Specific Preferences (Logged In)

**Steps**:
1. Login to the app
2. Backend should have custom preferences for this user
3. Open Dashboard

**Expected Behavior**:
- App fetches user-specific preferences
- Different symbol list than default (if customized)
- Symbols cached with user ID
- WebSocket subscribes to user's symbols

**Debug Logs to Check**:
```
[API] Fetching user symbol preferences for user-123, CRYPTO
[API] Received 5 user symbols for CRYPTO
[SymbolCache] Cached 5 symbols for CRYPTO
[Dashboard] Loaded 5 crypto symbols: BTC, ETH, SOL, AVAX, BNB
```

### Scenario 7: Pull to Refresh

**Steps**:
1. Launch app with cached data
2. Pull down on Dashboard to refresh
3. Wait for refresh to complete

**Expected Behavior**:
- Cache clears
- Fresh symbols load from API
- WebSocket resubscribes
- UI updates with any new symbols

**Debug Logs to Check**:
```
[PriceContext] Refreshing prices and symbols...
[SymbolCache] Cleared 1 cache entries
[API] Fetching default symbols for CRYPTO
[API] Received 9 default symbols for CRYPTO
[PriceContext] Resubscribing to 9 symbols
[PriceContext] Refresh complete
```

## Debugging Tools

### 1. Check AsyncStorage Cache

Add this to your code temporarily:

```typescript
import { SymbolCache } from './services/symbolCache';

// Check what's cached
const stats = await SymbolCache.getStats();
console.log('Cache Stats:', {
  totalCaches: stats.totalCaches,
  cacheKeys: stats.cacheKeys,
  totalSizeKB: (stats.totalSize / 1024).toFixed(2)
});

// Inspect specific cache
const cryptoCache = await SymbolCache.get('CRYPTO');
console.log('CRYPTO cache:', cryptoCache);
```

### 2. Clear Cache for Testing

```typescript
import { SymbolCache } from './services/symbolCache';

// Clear all caches
await SymbolCache.clear();
console.log('All caches cleared');

// Clear specific asset class
await SymbolCache.clearAssetClass('CRYPTO');
console.log('CRYPTO cache cleared');
```

### 3. Test API Endpoints Manually

```bash
# Test default symbols
curl http://192.168.68.103:5002/api/v1/symbol-preferences/defaults?assetClass=CRYPTO

# Test user preferences (requires token)
curl -H "Authorization: Bearer YOUR_TOKEN" \
  http://192.168.68.103:5002/api/v1/symbol-preferences/user/USER_ID?assetClass=CRYPTO

# Test legacy endpoint
curl http://192.168.68.103:5002/api/v1/symbols/by-asset-class/CRYPTO
```

### 4. Monitor Network Requests

Use React Native Debugger or Flipper:
- Network tab shows all API calls
- Check request/response for symbol endpoints
- Verify caching behavior (no duplicate calls)

## Common Issues & Solutions

### Issue 1: Symbols Not Loading

**Symptoms**: Dashboard shows "No symbols available" or only BTC/ETH

**Possible Causes**:
- Backend not running
- Wrong API_BASE_URL
- Network connectivity issues
- Backend endpoints not implemented

**Solution**:
1. Check backend is running: `curl http://YOUR_API_URL/health`
2. Verify API_BASE_URL in config
3. Check device/simulator can reach backend
4. Run `node test-dynamic-symbols.js` to validate endpoints

### Issue 2: Cache Not Working

**Symptoms**: App always calls API, even on second launch

**Possible Causes**:
- AsyncStorage permission issues
- Cache TTL too short
- Cache keys changing

**Solution**:
1. Check AsyncStorage permissions
2. Verify cache TTL (default 5 minutes)
3. Check logs for cache hits/misses
4. Inspect cache with `SymbolCache.getStats()`

### Issue 3: WebSocket Not Subscribing

**Symptoms**: No price updates, even though symbols loaded

**Possible Causes**:
- Symbol format mismatch (BTC vs BTCUSDT)
- WebSocket connection failed
- Backend not broadcasting for these symbols

**Solution**:
1. Check logs for subscription messages
2. Verify symbol format in subscription (should have USDT suffix)
3. Test backend WebSocket manually
4. Check backend is broadcasting for loaded symbols

### Issue 4: User Preferences Not Loading

**Symptoms**: Logged-in user sees default symbols instead of custom preferences

**Possible Causes**:
- Authentication token not sent
- User ID incorrect
- Backend endpoint returns defaults for missing preferences

**Solution**:
1. Verify auth token is present in request
2. Check user ID matches backend
3. Verify user has custom preferences in database
4. Check API logs for request details

### Issue 5: Offline Mode Shows Minimal Fallback

**Symptoms**: Only BTC and ETH shown in offline mode

**Possible Causes**:
- Cache was cleared
- App never connected before
- Cache expired and no stale data available

**Solution**:
1. Ensure app connected at least once before going offline
2. Check cache wasn't cleared
3. Verify cache TTL allows for reasonable offline period
4. Use stale cache fallback for longer offline periods

## Performance Benchmarks

Target metrics for optimal performance:

- **Cache Hit Rate**: >90% on repeat launches
- **Symbol Load Time**: <200ms (cache), <1s (API)
- **App Start Impact**: <100ms additional time
- **Cache Size**: ~50KB for 50 symbols
- **API Call Reduction**: ~90% fewer symbol API calls

## Success Indicators

✓ Dashboard loads symbols dynamically (no hard-coded list visible in UI)
✓ Logged-in users see their custom preferences
✓ Non-logged-in users see default symbols
✓ Offline mode works with cached data
✓ Pull-to-refresh updates symbol list
✓ WebSocket subscribes to correct symbols
✓ No crashes when backend is unavailable
✓ Cache improves performance on repeat launches

## Next Steps After Testing

If all tests pass:

1. **Test on Real Devices**: iOS and Android physical devices
2. **Stress Test**: Large symbol lists (100+ symbols)
3. **Network Conditions**: Slow 3G, packet loss, timeouts
4. **User Preference UI**: Build screen to manage preferences
5. **Analytics**: Track cache hit rates, API call frequency
6. **Error Reporting**: Monitor production errors related to symbols

## Support

For issues or questions:
- Check logs: All operations log with `[API]`, `[SymbolCache]`, or `[PriceContext]` prefixes
- Review implementation: See `DYNAMIC_SYMBOLS_IMPLEMENTATION.md`
- Backend validation: Run `node test-dynamic-symbols.js`
- Create detailed bug report with logs and steps to reproduce
