# WebSocket Price Update Fix - Mobile Frontend

## Problem Summary

WebSocket price updates were being received from the backend but NOT updating the mobile UI. Backend was successfully broadcasting real-time prices, SignalR connection was established, and events were arriving at the mobile app, but React components were not re-rendering with the new prices.

## Root Causes Identified

### 1. Multiple Duplicate Event Handlers
**Problem:** The PriceContext.tsx file registered the SAME event handlers multiple times:
- Lines 271-293: First `PriceUpdate` and `MarketDataUpdate` handlers
- Lines 337-351: Duplicate `ReceivePriceUpdate` handler
- Lines 402-521: Additional duplicate `ReceivePriceUpdate` and `PriceUpdate` handlers

**Impact:** When duplicate handlers are registered, React state updates can conflict, causing unpredictable behavior and preventing re-renders.

### 2. Early Return Optimization Preventing Updates
**Problem:** The duplicate handlers (lines 425-427, 442-444, 482-485, 499-501) had this logic:
```typescript
setPrices(prev => {
  const currentPrice = prev[symbol];
  if (currentPrice && Math.abs(currentPrice.price - data.price) < 0.01) {
    return prev; // CRITICAL BUG: Returns old state reference
  }
  return { ...prev, [symbol]: {...} };
});
```

**Impact:** When the price difference was less than $0.01, the function returned the SAME state object reference (`prev`). React uses reference equality to detect state changes, so returning `prev` means React thinks nothing changed and SKIPS re-rendering the UI.

### 3. Inconsistent State Updates
**Problem:** Some handlers updated both `prices` and `enhancedPrices`, while others only updated one state object.

**Impact:** Components reading from `enhancedPrices` wouldn't update if the handler only updated `prices`, and vice versa.

## Changes Made

### File: `/frontend/mobile/src/context/PriceContext.tsx`

#### 1. Fixed PriceUpdate Handler (Lines 271-318)
**Before:**
```typescript
hubConnection.on('PriceUpdate', (data: any) => {
  console.log('[PriceUpdate] Received:', data);
  if (data && data.symbol && data.price !== undefined) {
    const symbol = data.symbol.replace(/USDT$/i, '');
    setPrices(prev => {
      const updated = { ...prev, [symbol]: {...} };
      return updated;
    });
  }
});
```

**After:**
```typescript
hubConnection.on('PriceUpdate', (data: any) => {
  try {
    console.log('[PriceUpdate] Received:', data);

    if (!data || !data.symbol || data.price === undefined) {
      console.warn('[PriceUpdate] Invalid data');
      return;
    }

    const symbol = data.symbol.replace(/USDT$/i, '');
    console.log('[PriceUpdate] Processing:', { symbol, price: data.price });

    // CRITICAL: Always create NEW state object to trigger re-render
    setPrices(prev => {
      const newState = {
        ...prev,
        [symbol]: {
          price: data.price,
          change: data.change24h || data.change || data.priceChange || 0,
          timestamp: data.timestamp || new Date().toISOString()
        }
      };
      console.log('[PriceUpdate] State updated for', symbol, ':', newState[symbol]);
      return newState;
    });

    // Also update enhanced prices
    const symbolId = data.symbol.toLowerCase();
    setEnhancedPrices(prev => ({
      ...prev,
      [symbolId]: {
        symbolId,
        symbol: data.symbol,
        price: data.price,
        change: data.change24h || data.change || data.priceChange || 0,
        changePercent: data.changePercent || 0,
        timestamp: data.timestamp || new Date().toISOString(),
        marketStatus: 'OPEN',
        dataSource: 'REAL_TIME',
        lastUpdated: new Date().toISOString(),
      }
    }));
  } catch (error) {
    console.error('[PriceUpdate] Error processing update:', error);
  }
});
```

**Key Changes:**
- Always creates a NEW state object (never returns `prev`)
- Updates BOTH `prices` and `enhancedPrices` consistently
- Enhanced error handling with try/catch
- Better logging to track state changes
- Handles multiple field name variations (change24h, change, priceChange)

#### 2. Fixed MarketDataUpdate Handler (Lines 320-365)
- Applied same fixes as PriceUpdate
- Always creates new state objects
- Updates both legacy and enhanced prices
- Better error handling and logging

#### 3. Fixed Legacy ReceivePriceUpdate Handler (Lines 392-433)
- Applied same consistency fixes
- Updates both state objects
- Enhanced error handling

#### 4. Removed Duplicate Event Handlers (Lines 400-521 removed)
**Before:** Had duplicate handlers that conflicted with the primary handlers.

**After:** Replaced with:
```typescript
// Note: All event handlers are now registered BEFORE connection start above
// No need to register duplicate handlers here
```

**Impact:** Eliminates handler conflicts and ensures clean state updates.

#### 5. Fixed ReceiveMarketData Batch Handler (Lines 495-547)
**Before:**
```typescript
if (!previousPrice || priceChange > 0.01) {
  hasSignificantChanges = true;
  updates[cleanSymbol] = {...};
}
if (hasSignificantChanges) {
  setPrices(prev => ({ ...prev, ...updates }));
}
```

**After:**
```typescript
Object.keys(data.symbols).forEach(symbol => {
  const symbolData = data.symbols[symbol];
  if (symbolData.price !== undefined) {
    updates[cleanSymbol] = {...};
    enhancedUpdates[symbolId] = {...};
  }
});

// CRITICAL: Always update state to trigger re-render
if (Object.keys(updates).length > 0) {
  setPrices(prev => ({ ...prev, ...updates }));
}
if (Object.keys(enhancedUpdates).length > 0) {
  setEnhancedPrices(prev => ({ ...prev, ...enhancedUpdates }));
}
```

**Impact:** No longer skips updates based on price change threshold. Always triggers re-render when new data arrives.

#### 6. Added State Change Monitoring (Lines 706-720)
```typescript
useEffect(() => {
  const legacyCount = Object.keys(prices).length;
  const enhancedCount = Object.keys(enhancedPrices).length;

  if (legacyCount > 0 || enhancedCount > 0) {
    console.log('[PriceContext] State updated:', {
      legacyPrices: legacyCount,
      enhancedPrices: enhancedCount,
      connectionStatus,
      sampleLegacy: Object.entries(prices).slice(0, 1).map(([k, v]) => `${k}: $${v.price}`),
      sampleEnhanced: Object.entries(enhancedPrices).slice(0, 1).map(([k, v]) => `${k}: $${v.price}`),
    });
  }
}, [prices, enhancedPrices, connectionStatus]);
```

**Purpose:** Debug logging to verify state updates are propagating correctly.

### File: `/frontend/mobile/src/screens/DashboardScreen.tsx`

#### Added Component Re-render Monitoring (Lines 149-158)
```typescript
// DEBUG: Log when enhancedPrices changes
useEffect(() => {
  const priceCount = Object.keys(enhancedPrices).length;
  if (priceCount > 0) {
    const samplePrices = Object.entries(enhancedPrices).slice(0, 2).map(([id, data]) =>
      `${id}: $${data.price}`
    );
    console.log(`[Dashboard] Enhanced prices updated: ${priceCount} symbols`, samplePrices);
  }
}, [enhancedPrices]);
```

**Purpose:** Verify that component receives and processes price state updates.

## Expected Behavior After Fix

### Successful WebSocket Price Update Flow:

1. **Backend Broadcasts:**
   ```
   Backend -> SignalR Hub -> "PriceUpdate" event
   Data: { symbol: "BTCUSDT", price: 122173.12, change24h: -2.5 }
   ```

2. **Mobile Receives Event:**
   ```
   LOG [SignalR] Event received: PriceUpdate [{"symbol": "BTCUSDT", "price": 122173.12, ...}]
   LOG [PriceUpdate] Received: {"symbol": "BTCUSDT", "price": 122173.12, ...}
   LOG [PriceUpdate] Processing: { symbol: "BTC", price: 122173.12, change: -2.5 }
   ```

3. **State Updates:**
   ```
   LOG [PriceUpdate] State updated for BTC : { price: 122173.12, change: -2.5, timestamp: "..." }
   LOG [PriceContext] State updated: {
     legacyPrices: 5,
     enhancedPrices: 5,
     connectionStatus: "connected",
     sampleLegacy: ["BTC: $122173.12"],
     sampleEnhanced: ["btcusdt: $122173.12"]
   }
   ```

4. **Component Re-renders:**
   ```
   LOG [Dashboard] Enhanced prices updated: 5 symbols ["btcusdt: $122173.12", "ethusdt: $4511.8"]
   ```

5. **UI Updates:**
   ```
   Asset card displays: BTC $122,173.12 -2.5%
   ```

## Testing Checklist

### Before Fix:
- [x] WebSocket events arriving ✅
- [x] Initial HTTP fetch displays prices ✅
- [ ] Real-time WebSocket updates NOT showing in UI ❌

### After Fix (Expected):
- [x] WebSocket events arriving ✅
- [x] Initial HTTP fetch displays prices ✅
- [x] Real-time WebSocket updates trigger UI refresh ✅
- [x] State updates logged in console ✅
- [x] Component re-renders logged in console ✅
- [x] Price changes visible in UI immediately ✅

## Key Learnings

### React State Update Principles:
1. **Reference Equality:** React uses `Object.is()` to compare state. Returning the same object reference skips re-renders.
2. **Always Create New Objects:** Use spread operator `{ ...prev, [key]: newValue }` to ensure new reference.
3. **Avoid Early Returns:** Don't return `prev` unless you genuinely want to skip the update.
4. **State Consistency:** If multiple state variables represent the same data, update ALL of them together.

### WebSocket Event Handling:
1. **Register Handlers ONCE:** Multiple registrations cause conflicts.
2. **Register BEFORE Connection:** Set up handlers before calling `.start()`.
3. **Consistent Field Naming:** Handle variations (change24h, change, priceChange) gracefully.
4. **Error Boundaries:** Wrap handlers in try/catch to prevent one error from breaking all updates.

### Debugging React State:
1. **Log Inside setState:** Add console.log INSIDE the setState callback to verify it runs.
2. **Monitor Dependencies:** Use useEffect with state dependencies to track changes.
3. **Check Reference Equality:** Verify new objects are created, not mutated.
4. **Component-Level Logging:** Add useEffect in consumer components to verify they receive updates.

## Files Modified

1. `/frontend/mobile/src/context/PriceContext.tsx` - Fixed state management and event handlers
2. `/frontend/mobile/src/screens/DashboardScreen.tsx` - Added debug logging

## Regression Prevention

**BEFORE MARKING COMPLETE, VERIFY:**
1. Start the mobile application
2. Verify WebSocket connection establishes
3. Check console logs show price updates arriving
4. Confirm UI displays real-time price changes
5. Verify no errors in console
6. Test for at least 30 seconds to see multiple updates

**If any test fails:** Rollback changes and investigate further before deploying.

## Performance Considerations

The fix removed the "skip update if price difference < $0.01" optimization. This was necessary because it prevented re-renders.

**Future Optimization Options:**
1. Implement debouncing in components (not in state updates)
2. Use React.memo with custom comparison function
3. Implement virtual scrolling for large lists
4. Use useMemo/useCallback to prevent unnecessary child re-renders

**DO NOT** re-introduce early return optimizations in setState callbacks.

## Deployment Notes

This fix is CRITICAL for production. Without it, users see stale prices and think the app is broken.

**Rollout Plan:**
1. Test thoroughly in development
2. Deploy to staging environment
3. Verify real-time updates work with production backend
4. Monitor logs for 24 hours before full rollout
5. Keep rollback plan ready

**Monitoring:**
- Watch for console errors in production logs
- Track WebSocket connection success rate
- Monitor user engagement metrics (should increase with real-time updates)

## Related Issues

- Backend broadcasts to groups: `CRYPTO_BTCUSDT`, `CRYPTO_ETHUSDT`
- Event names: `PriceUpdate`, `MarketDataUpdate`, `ReceivePriceUpdate`
- Field name variations: `change24h`, `change`, `priceChange`
- Symbol format: Backend sends `BTCUSDT`, state stores as `BTC`

All these variations are now handled consistently in the fixed code.
