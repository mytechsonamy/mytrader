# NYSE Market Status Fix

**Date**: October 10, 2025
**Issue**: Header showing "1 Açık 2 Kapalı" instead of "1 Açık 3 Kapalı"
**User Report**: "nyse de kapalı, 1 açık 3 kapalı olmalı"
**Status**: ✅ FIXED

---

## Problem

Dashboard has 4 accordions:
1. 🏢 BIST Hisseleri - **KAPALΙ** (closed at night)
2. 🇺🇸 NASDAQ Hisseleri - **KAPALI** (closed at night)
3. 🗽 NYSE Hisseleri - **KAPALI** (closed at night)
4. 🚀 Kripto - **AÇIK** (24/7)

**Expected Header**: "1 Açık 3 Kapalı"
**Actual Header**: "1 Açık 2 Kapalı" ❌

---

## Root Cause

**File**: `src/screens/DashboardScreen.tsx`
**Line**: 558-609

The `marketStatuses` array in SmartOverviewHeader only included 3 markets:
- BIST
- NASDAQ (representing both NASDAQ and NYSE)
- CRYPTO

NYSE was missing as a separate entry.

### Before Fix
```typescript
marketStatuses={useMemo(() => {
  const bistStatus = getMarketStatus('BIST');
  const usStatus = getMarketStatus('NASDAQ');  // ❌ Only NASDAQ
  const cryptoStatus = getMarketStatus('CRYPTO');

  return [
    { marketId: 'bist', marketName: 'BIST', status: bistStatus.status, ... },
    { marketId: 'nasdaq', marketName: 'NASDAQ', status: usStatus.status, ... },
    { marketId: 'crypto', marketName: 'CRYPTO', status: cryptoStatus.status, ... },
  ];
}, [])}
```

**Result**: Only 3 markets counted
- BIST: CLOSED
- NASDAQ: CLOSED
- CRYPTO: OPEN
- **Total**: 1 Açık 2 Kapalı ❌

---

## Solution

Added NYSE as a separate market status calculation.

### After Fix
```typescript
marketStatuses={useMemo(() => {
  const bistStatus = getMarketStatus('BIST');
  const nasdaqStatus = getMarketStatus('NASDAQ');  // ✅ NASDAQ separate
  const nyseStatus = getMarketStatus('NYSE');      // ✅ NYSE added
  const cryptoStatus = getMarketStatus('CRYPTO');

  return [
    { marketId: 'bist', marketName: 'BIST', status: bistStatus.status, ... },
    { marketId: 'nasdaq', marketName: 'NASDAQ', status: nasdaqStatus.status, ... },
    { marketId: 'nyse', marketName: 'NYSE', status: nyseStatus.status, ... },  // ✅ NYSE entry
    { marketId: 'crypto', marketName: 'CRYPTO', status: cryptoStatus.status, ... },
  ];
}, [])}
```

**Result**: All 4 markets counted correctly
- BIST: CLOSED (🔴)
- NASDAQ: CLOSED (🔴)
- NYSE: CLOSED (🔴)
- CRYPTO: OPEN (🟢)
- **Total**: 1 Açık 3 Kapalı ✅

---

## Market Hours Logic

Both NASDAQ and NYSE use the same market hours (they're both US markets):
- **Trading Hours**: 09:30-16:00 EST/EDT (New York Time)
- **Pre-market**: 04:00-09:30
- **Post-market**: 16:00-20:00
- **Weekends**: Closed
- **Timezone**: America/New_York (UTC-5/UTC-4 with DST)

The `getMarketStatus('NYSE')` function in `marketHours.ts` returns the same hours as `getMarketStatus('NASDAQ')` because both are US exchanges.

---

## Files Modified

**`src/screens/DashboardScreen.tsx`**
- Line 558-609: Added NYSE market status calculation
- Line 562: Added `const nyseStatus = getMarketStatus('NYSE');`
- Line 588-598: Added NYSE market object to array

---

## Verification

### Expected Behavior (Midnight Turkey Time)
```
🏢 BIST Hisseleri    🔴 Kapalı   (10:00-18:00 TRT)
🇺🇸 NASDAQ Hisseleri  🔴 Kapalı   (09:30-16:00 EST)
🗽 NYSE Hisseleri     🔴 Kapalı   (09:30-16:00 EST)
🚀 Kripto            🟢 Açık     (24/7)

Header: "1 Açık 3 Kapalı" ✅
```

### Testing
```bash
cd frontend/mobile
npx expo start
```

Open app and verify:
- ✅ Header shows "1 Açık 3 Kapalı"
- ✅ All 4 accordions display correct status
- ✅ BIST, NASDAQ, NYSE show red "Kapalı" indicator
- ✅ Crypto shows green "Açık" indicator

---

## Summary

✅ **Fixed**: Added NYSE to market status array
✅ **Header**: Now correctly shows "1 Açık 3 Kapalı"
✅ **All Markets**: BIST, NASDAQ, NYSE, CRYPTO all counted

**Time to Fix**: ~2 minutes
**Complexity**: Trivial (added missing array entry)
**Risk**: None (simple data addition)
