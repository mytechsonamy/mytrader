# WebSocket Price Update Test Plan

## Pre-Test Checklist
- [ ] Backend is running and accessible
- [ ] Mobile app is built and running on device/simulator
- [ ] Console/logs are visible

## Test Steps

### 1. Verify Initial Connection
**Expected Logs:**
```
LOG Connecting to SignalR hub: http://192.168.68.103:5002/hubs/dashboard
LOG Connected to SignalR hub
LOG Auto-subscribing to CRYPTO symbols: ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT', 'AVAXUSDT']
LOG Successfully subscribed to CRYPTO price updates
```

**Pass Criteria:** Connection establishes without errors

---

### 2. Verify Initial Price Fetch
**Expected Logs:**
```
LOG Fetching real price data from: http://192.168.68.103:5002/api
LOG Received price data: {...}
LOG Setting formatted prices: {BTC: {price: 122039.54, ...}, ETH: {price: 4511.81, ...}}
LOG [PriceContext] State updated: {legacyPrices: 5, enhancedPrices: 5, ...}
```

**Visual Verification:**
- Dashboard screen shows crypto cards
- Each card displays a price (e.g., "BTC $122,039.54")
- Prices are NOT zero or "N/A"

**Pass Criteria:** Initial prices display correctly in UI

---

### 3. Verify WebSocket Events Arrive
**Wait 10-15 seconds for price updates**

**Expected Logs (should appear repeatedly):**
```
LOG [SignalR] Event received: PriceUpdate [{"symbol": "BTCUSDT", "price": 122173.12, ...}]
LOG [PriceUpdate] Received: {"symbol": "BTCUSDT", "price": 122173.12, ...}
LOG [PriceUpdate] Processing: { symbol: "BTC", price: 122173.12, change: -2.5 }
```

**Pass Criteria:** WebSocket events logged every few seconds

---

### 4. CRITICAL TEST: Verify State Updates Trigger
**Expected Logs (MUST appear for every PriceUpdate event):**
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

**CRITICAL:** If these logs don't appear, state updates are NOT happening → FAIL

**Pass Criteria:** State update logs appear for each incoming price event

---

### 5. CRITICAL TEST: Verify Component Re-renders
**Expected Logs (should appear when prices change):**
```
LOG [Dashboard] Enhanced prices updated: 5 symbols ["btcusdt: $122173.12", "ethusdt: $4511.8"]
```

**CRITICAL:** If this log doesn't appear, components are NOT re-rendering → FAIL

**Pass Criteria:** Dashboard re-render logs appear when prices change

---

### 6. CRITICAL TEST: Verify UI Updates in Real-Time
**Visual Verification:**
1. Watch the BTC price card on screen
2. Compare the displayed price to the console logs
3. Price should change every few seconds
4. The change percentage should update (red/green color)

**Example:**
```
Time 0s:  BTC $122,039.54  -2.34%  (red)
Time 5s:  BTC $122,173.12  -2.25%  (red)
Time 10s: BTC $122,456.78  -2.01%  (red)
Time 15s: BTC $122,789.34  +0.12%  (green) <- Color changes!
```

**CRITICAL:** If UI doesn't update, there's a rendering issue → FAIL

**Pass Criteria:** UI prices update in real-time matching console logs

---

### 7. Test Symbol Key Consistency
**Check logs for symbol formatting:**
```
LOG [PriceUpdate] Processing: { symbol: "BTC", ... }     <- Should be "BTC" not "BTCUSDT"
LOG [PriceContext] sampleLegacy: ["BTC: $122173.12"]     <- Uses "BTC"
LOG [PriceContext] sampleEnhanced: ["btcusdt: $122173.12"] <- Uses "btcusdt"
LOG [Dashboard] Enhanced prices updated: ... ["btcusdt: $122173.12"] <- Uses "btcusdt"
```

**Pass Criteria:** Symbol keys are consistent and prices are found

---

### 8. Test Error Handling
**Manually trigger error (if possible):**
- Disconnect backend
- Restart backend
- Send malformed data

**Expected Logs:**
```
LOG SignalR reconnecting...
LOG [PriceUpdate] Invalid data: {...}
ERROR [PriceUpdate] Error processing update: ...
```

**Pass Criteria:** Errors logged but app doesn't crash

---

## Success Criteria Summary

### MUST PASS (Critical):
- [x] Initial connection successful
- [x] Initial prices load and display
- [x] WebSocket events arrive continuously
- [x] **State updates trigger for each event** ← MOST CRITICAL
- [x] **Components re-render when state changes** ← MOST CRITICAL
- [x] **UI displays real-time price changes** ← MOST CRITICAL

### SHOULD PASS (Important):
- [x] Symbol keys consistent between events and state
- [x] No console errors during normal operation
- [x] Reconnection works after network interruption

### NICE TO HAVE:
- [x] Price change animations smooth
- [x] Color changes (red/green) work correctly
- [x] Performance remains good with continuous updates

---

## Failure Scenarios & Fixes

### Scenario 1: Events arrive but state doesn't update
**Symptoms:**
```
✓ LOG [PriceUpdate] Received: {...}
✓ LOG [PriceUpdate] Processing: {...}
✗ No "State updated" log
```

**Diagnosis:** setState not executing
**Fix:** Check if setPrices callback is running (add console.log inside)

---

### Scenario 2: State updates but components don't re-render
**Symptoms:**
```
✓ LOG [PriceUpdate] State updated for BTC
✓ LOG [PriceContext] State updated: {...}
✗ No "[Dashboard] Enhanced prices updated" log
```

**Diagnosis:** Component not subscribed to state changes
**Fix:** Verify usePrices() hook returns correct values and component has useEffect dependency

---

### Scenario 3: Components re-render but UI doesn't change
**Symptoms:**
```
✓ LOG [Dashboard] Enhanced prices updated: {...}
✗ UI still shows old prices
```

**Diagnosis:** Component rendering stale data
**Fix:** Check component is reading from correct state property (prices vs enhancedPrices)

---

### Scenario 4: Same object reference returned
**Symptoms:**
```
LOG [PriceUpdate] State updated for BTC : {...}
(but price in UI doesn't change)
```

**Diagnosis:** setState returning previous state reference
**Fix:** Ensure setState always returns NEW object: `{ ...prev, [key]: value }`

---

## Rollback Plan

If tests fail:
1. Stop mobile app
2. Git revert changes to PriceContext.tsx
3. Rebuild app
4. Document failure scenario
5. Investigate root cause before re-attempting fix

---

## Performance Monitoring

During 30-second test period, monitor:
- **Update frequency:** Should be 1-5 updates per second
- **Memory usage:** Should remain stable (not increasing)
- **CPU usage:** Should be <10% average
- **Frame rate:** Should stay 60fps (no jank)

If performance degrades:
- Consider adding debouncing (in components, not state)
- Use React.memo for child components
- Implement virtual scrolling for large lists

---

## Post-Test Verification

After confirming all tests pass:
1. Test on physical device (not just simulator)
2. Test with poor network (throttled connection)
3. Test with app in background then foreground
4. Test with multiple rapid price changes
5. Test with backend restart scenario

Only deploy to production after ALL tests pass consistently.
