# Alpaca Integration - Mobile Testing Guide

## Quick Start Testing

### Prerequisites
1. Ensure backend is running with Alpaca integration
2. Ensure mobile app is running (`npm start` or `expo start`)
3. Have both iOS and Android test devices/simulators available

---

## Test Scenarios

### Scenario 1: Alpaca Real-Time Data (Stock Market Open)

**Expected Behavior**:
- Green dot indicator appears next to stock prices
- Full card view shows "Live" label
- Compact card view shows just green dot
- Data updates in real-time

**Test Steps**:
1. Open Dashboard screen
2. Expand "Stocks" accordion
3. Observe stock prices (e.g., AAPL, TSLA)
4. Verify green indicator appears
5. Tap on a stock to see full card view
6. Verify "Live" label is shown

**Screenshot Points**:
- [ ] Compact view with green dot
- [ ] Full view with green dot and "Live" label

---

### Scenario 2: Yahoo Fallback Data (Stock Market Closed)

**Expected Behavior**:
- Yellow/amber dot indicator appears
- Full card view shows "Delayed" label
- No real-time updates

**Test Steps**:
1. Open Dashboard screen when market is closed
2. Expand "Stocks" accordion
3. Observe stock prices
4. Verify yellow indicator appears
5. Tap on a stock to see full card view
6. Verify "Delayed" label is shown

**Screenshot Points**:
- [ ] Compact view with yellow dot
- [ ] Full view with yellow dot and "Delayed" label

---

### Scenario 3: Backward Compatibility (No Source Field)

**Expected Behavior**:
- No indicator shown
- Prices display normally
- No errors or crashes

**Test Steps**:
1. Open Dashboard screen
2. Observe crypto prices (may not have source field)
3. Verify no indicator appears
4. Verify prices still display correctly
5. Tap on crypto to see full card view
6. Verify card displays normally

**Success Criteria**:
- [ ] No indicators for crypto
- [ ] No console errors
- [ ] No crashes

---

### Scenario 4: Mixed Data Sources

**Expected Behavior**:
- Different indicators for different symbols
- Stocks show colored indicators
- Crypto shows no indicators (or appropriate indicator if backend sends)

**Test Steps**:
1. Open Dashboard screen
2. View multiple asset classes
3. Verify each symbol has correct indicator
4. Compare stock vs crypto indicators

**Verification**:
- [ ] AAPL has indicator
- [ ] BTC may not have indicator (depends on backend)
- [ ] All prices display correctly

---

## Visual Validation Tests

### Compact Card Layout
```
┌─────────────────────────────┐
│ 🇺🇸 AAPL               $150.25● │ ← Green dot here
│    Apple Inc.           +1.2% │
└─────────────────────────────┘
```

**Checklist**:
- [ ] Dot aligns with price
- [ ] Dot is 6px diameter
- [ ] Dot has subtle shadow
- [ ] Price text not pushed out of view
- [ ] Change percentage on new line

### Full Card Layout
```
┌─────────────────────────────────┐
│ 🇺🇸 AAPL                    AÇIK │
│    Apple Inc.                    │
│                                  │
│ $150.25 ● Live              ← Indicator here
│ +1.2%                           │
│                                  │
│ RSI: 65.4  MACD: 0.235          │
│ BB Üst: $152  BB Alt: $148      │
│                                  │
│ [ 📈 Strateji Test ]  [ ⭐ ]    │
│ Son güncelleme: 14:30:25        │
└─────────────────────────────────┘
```

**Checklist**:
- [ ] Dot appears next to price
- [ ] "Live" or "Delayed" label visible
- [ ] Dot is 8px diameter
- [ ] Text is legible
- [ ] Layout not disrupted

---

## Platform-Specific Tests

### iOS Testing

**Device**: iPhone Simulator (or real device)

1. **Text Rendering**:
   - [ ] Label text is clear and sharp
   - [ ] Dot is perfectly circular
   - [ ] Colors match design (green: #10b981, yellow: #f59e0b)

2. **Layout**:
   - [ ] No text clipping
   - [ ] Proper spacing
   - [ ] Consistent alignment

3. **Performance**:
   - [ ] Smooth scrolling through list
   - [ ] No lag when indicators appear
   - [ ] No memory issues

### Android Testing

**Device**: Android Emulator (or real device)

1. **Text Rendering**:
   - [ ] Label text is clear
   - [ ] Dot renders correctly (check for oval vs circle)
   - [ ] Colors consistent with iOS

2. **Layout**:
   - [ ] No text clipping
   - [ ] Proper spacing
   - [ ] Consistent alignment across devices

3. **Performance**:
   - [ ] Smooth scrolling
   - [ ] No lag
   - [ ] No memory issues

---

## Regression Testing

### Critical Paths to Verify

1. **Login Flow**:
   - [ ] Login still works
   - [ ] Session persists
   - [ ] No errors on login

2. **Dashboard Loading**:
   - [ ] Dashboard loads correctly
   - [ ] All accordions expand/collapse
   - [ ] Prices display correctly

3. **Real-Time Updates**:
   - [ ] WebSocket connects
   - [ ] Prices update in real-time
   - [ ] No connection drops

4. **Navigation**:
   - [ ] All tabs accessible
   - [ ] Navigation smooth
   - [ ] No crashes when navigating

5. **Strategy Test**:
   - [ ] Can tap on stock card
   - [ ] Navigation to Strategy Test works
   - [ ] Parameters passed correctly

---

## Debug Testing

### Console Logs to Monitor

When running in dev mode, watch for these logs:

```
[PriceContext] RAW price_update: { symbol, price, source, isRealtime, ... }
[PriceContext] Normalized price_update: { symbolId, source, isRealtime, ... }
```

**Expected Logs**:
- Source field should be present for stocks
- isRealtime should be boolean
- qualityScore optional

**Error Logs to Watch For**:
- ❌ "Failed to process price_update"
- ❌ TypeScript errors
- ❌ Render errors

---

## Performance Testing

### Metrics to Track

1. **Component Render Time**:
   - DataSourceIndicator should render in < 1ms
   - AssetCard render time should not increase significantly

2. **Memory Usage**:
   - No memory leaks when scrolling
   - Stable memory usage over time

3. **Network**:
   - No additional API calls
   - WebSocket traffic unchanged

### Tools
- React DevTools (Performance tab)
- Expo Dev Tools (Performance monitor)
- Native performance monitors

---

## Accessibility Testing

### Screen Reader Support

**iOS VoiceOver**:
1. Enable VoiceOver
2. Navigate to AssetCard
3. Verify indicator announces correctly
   - Expected: "Real-time data from Alpaca" or "Delayed data from Yahoo Finance"

**Android TalkBack**:
1. Enable TalkBack
2. Navigate to AssetCard
3. Verify indicator announces correctly
   - Same expectations as iOS

---

## Edge Cases to Test

### 1. Poor Network Conditions
- [ ] Indicators update correctly when connection drops
- [ ] Graceful handling of missing data
- [ ] No crashes

### 2. Rapid Data Updates
- [ ] Indicators don't flicker
- [ ] Performance remains smooth
- [ ] No memory issues

### 3. Multiple Symbols
- [ ] Each symbol has correct indicator
- [ ] No indicator confusion between symbols
- [ ] Batch updates handled correctly

### 4. Quality Score Edge Cases
- [ ] Quality score = 0 → warning shows
- [ ] Quality score = 69 → warning shows
- [ ] Quality score = 70 → no warning
- [ ] Quality score = 100 → no warning
- [ ] Quality score undefined → no warning

---

## Automated Testing (Future)

### Unit Tests to Add
```typescript
describe('DataSourceIndicator', () => {
  it('renders green dot for Alpaca real-time', () => {});
  it('renders yellow dot for Yahoo fallback', () => {});
  it('renders nothing when source undefined', () => {});
  it('shows label when showLabel=true', () => {});
  it('hides label when showLabel=false', () => {});
});
```

### Integration Tests to Add
```typescript
describe('AssetCard with DataSourceIndicator', () => {
  it('displays indicator when marketData has source', () => {});
  it('hides indicator when marketData missing source', () => {});
  it('updates indicator when data source changes', () => {});
});
```

---

## Test Completion Checklist

### Before Marking Complete

- [ ] All scenarios tested on iOS
- [ ] All scenarios tested on Android
- [ ] Visual validation passed
- [ ] Performance acceptable
- [ ] No regressions found
- [ ] Accessibility verified
- [ ] Edge cases handled
- [ ] Screenshots captured
- [ ] Documentation updated

---

## Bug Reporting Template

If issues found, use this template:

```
**Issue**: [Brief description]
**Severity**: Critical / High / Medium / Low
**Platform**: iOS / Android / Both
**Steps to Reproduce**:
1.
2.
3.

**Expected Behavior**:

**Actual Behavior**:

**Screenshots**:

**Console Logs**:

**Device Info**:
- Model:
- OS Version:
- App Version:
```

---

## Sign-Off

**Tester Name**: _______________
**Date**: _______________
**iOS Version Tested**: _______________
**Android Version Tested**: _______________

**Overall Status**: Pass / Fail / Pass with Issues

**Notes**:
