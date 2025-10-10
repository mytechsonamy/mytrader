# Previous Close Testing Guide

## Quick Start Testing

### 1. Start the Mobile App
```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/frontend/mobile
npm start
```

### 2. Open on Device/Simulator
- Press `i` for iOS Simulator
- Press `a` for Android Emulator
- Scan QR code with Expo Go app on physical device

### 3. Navigate to Dashboard
- App should open directly on Dashboard screen
- Look for stock symbols with asset cards

## Visual Verification Checklist

### ✅ Full Card View Testing

#### Test 1: BIST Stock (Turkish Market)
**Symbol**: THYAO (Türk Hava Yolları)
- [ ] Previous Close displays with "Önceki Kapanış:" label
- [ ] Value shows in Turkish Lira (₺)
- [ ] Top border separator visible above Previous Close
- [ ] Font size is readable but smaller than current price
- [ ] Value is right-aligned with proper spacing

**Expected Display**:
```
┌────────────────────────────────────────┐
│ 🏢 THYAO                    ₺244.50    │
│    Türk Hava Yolları       +1.85% ↑    │
│ ────────────────────────────────────── │
│ Önceki Kapanış: ₺240.06                │
│ ...                                     │
└────────────────────────────────────────┘
```

#### Test 2: NASDAQ Stock (US Market)
**Symbol**: AAPL (Apple Inc.)
- [ ] Previous Close displays with "Önceki Kapanış:" label
- [ ] Value shows in US Dollars ($)
- [ ] Percentage change calculation appears correct
- [ ] No layout breaking or overflow

**Expected Display**:
```
┌────────────────────────────────────────┐
│ 🇺🇸 AAPL                    $150.25    │
│    Apple Inc.              +2.50% ↑    │
│ ────────────────────────────────────── │
│ Önceki Kapanış: $146.58                │
│ ...                                     │
└────────────────────────────────────────┘
```

#### Test 3: NYSE Stock (US Market)
**Symbol**: JPM (JPMorgan Chase)
- [ ] Previous Close displays with "Önceki Kapanış:" label
- [ ] Value shows in US Dollars ($)
- [ ] Market status indicator works correctly
- [ ] Data source badge displays properly

**Expected Display**:
```
┌────────────────────────────────────────┐
│ 🇺🇸 JPM                     $192.75    │
│    JPMorgan Chase          +3.21% ↑    │
│ ────────────────────────────────────── │
│ Önceki Kapanış: $186.75                │
│ ...                                     │
└────────────────────────────────────────┘
```

### ✅ Compact Card View Testing

#### Test 4: Accordion Collapsed View
**When**: Stocks accordion is collapsed but showing preview
- [ ] Compact cards display abbreviated "Önc:" label
- [ ] Value is right-aligned
- [ ] Text is smaller (9px) and gray
- [ ] Doesn't crowd other information

**Expected Display**:
```
┌────────────────────────────────────────┐
│ 🇺🇸 AAPL                    $150.25    │
│    Apple Inc.              +2.50%      │
│                        Önc: $146.58    │
│ ────────────────────────────────────── │
│ • AÇIK              2 dakika önce      │
└────────────────────────────────────────┘
```

### ✅ Edge Cases Testing

#### Test 5: Missing Previous Close
**Scenario**: Asset without previous close data
- [ ] Previous Close section does NOT display
- [ ] No blank space or placeholder
- [ ] Card layout remains clean
- [ ] No console errors

**Expected Behavior**:
```
┌────────────────────────────────────────┐
│ 🇺🇸 NEWIPO                  $25.00     │
│    New IPO Company         +0.00%      │
│                                         │
│ ← No Previous Close section            │
│                                         │
│ RSI: 45.2    MACD: 0.123              │
└────────────────────────────────────────┘
```

#### Test 6: Zero Previous Close
**Scenario**: Asset with previousClose = 0
- [ ] Previous Close section does NOT display
- [ ] Behaves same as undefined/null

#### Test 7: Very Large Numbers
**Scenario**: High-value asset (e.g., BTC)
**Symbol**: BTCUSDT
- [ ] Previous Close: $95,234.50
- [ ] Formatting uses proper thousand separators
- [ ] Currency symbol displays correctly
- [ ] No text overflow

#### Test 8: Very Small Numbers
**Scenario**: Low-value stock
- [ ] Previous Close: $0.15
- [ ] Maintains 2 decimal places
- [ ] Leading zero displays correctly

## Functional Testing

### Test 9: Real-Time Updates
**Steps**:
1. Note current Previous Close value
2. Wait for market data update (WebSocket)
3. Verify Previous Close remains stable (should not change intraday)
4. Verify current price updates while Previous Close stays same

**Expected**: Previous Close should only change at market open with new session data

### Test 10: Market Status Integration
**Test Different States**:
- [ ] **OPEN**: Previous Close from yesterday
- [ ] **CLOSED**: Previous Close from yesterday
- [ ] **PRE_MARKET**: Previous Close from yesterday
- [ ] **AFTER_MARKET**: Previous Close from today's close

### Test 11: Multi-Market Display
**Steps**:
1. Expand BIST accordion
2. Verify BIST stocks show TRY values
3. Expand NASDAQ accordion
4. Verify NASDAQ stocks show USD values
5. Expand NYSE accordion
6. Verify NYSE stocks show USD values

**Check**: Currency symbols and formatting are correct for each market

## Data Accuracy Testing

### Test 12: Percentage Calculation Verification
**Formula**: `changePercent = (currentPrice - previousClose) / previousClose × 100`

**Example**:
- Current Price: $150.25
- Previous Close: $146.58
- Expected Change %: +2.50%

**Steps**:
1. Note displayed values
2. Calculate manually: (150.25 - 146.58) / 146.58 × 100 = 2.50%
3. Verify displayed percentage matches

**Test with**:
- [ ] Positive change
- [ ] Negative change
- [ ] Zero change

### Test 13: Price Formatting Consistency
**Check**:
- [ ] Previous Close uses same formatting as Current Price
- [ ] Decimal places match (2 for stocks, variable for crypto)
- [ ] Currency symbols match
- [ ] Locale settings respected (tr-TR)

## Performance Testing

### Test 14: Scroll Performance
**Steps**:
1. Open Dashboard with multiple assets
2. Scroll through all accordions quickly
3. Monitor for lag or frame drops
4. Check React DevTools profiler

**Expected**: Smooth 60 FPS scrolling with no jank

### Test 15: Memory Usage
**Steps**:
1. Open Dashboard
2. Note baseline memory usage
3. Expand all accordions
4. Wait for all data to load
5. Check memory usage

**Expected**: Minimal increase (<5 MB)

### Test 16: WebSocket Data Flow
**Open Developer Console**:
```bash
# Look for these log messages
[PriceContext] RAW price_update - All fields: [...]
[PriceContext] RAW price_update: { previousClose: 146.58, ... }
[PriceContext] Normalized price_update: { previousClose: 146.58, ... }
```

**Verify**:
- [ ] `previousClose` field present in WebSocket data
- [ ] Value is correctly normalized
- [ ] Both PascalCase and camelCase handled

## Regression Testing

### Test 17: Existing Features Still Work
- [ ] Current price displays correctly
- [ ] Change percentage displays correctly
- [ ] Market status badge works
- [ ] Data source indicator works
- [ ] Technical indicators display
- [ ] Strategy Test button works
- [ ] Watchlist star works
- [ ] Last update time shows

### Test 18: Different Screen Sizes
**Test on**:
- [ ] iPhone SE (small screen)
- [ ] iPhone 14 Pro (medium screen)
- [ ] iPhone 14 Pro Max (large screen)
- [ ] iPad (tablet size)

**Verify**: Layout adapts correctly without overflow

### Test 19: Orientation Changes
**Steps**:
1. View in portrait mode
2. Rotate to landscape
3. Rotate back to portrait

**Expected**: Previous Close displays correctly in both orientations

## Accessibility Testing

### Test 20: Screen Reader Support
**iOS VoiceOver**:
1. Enable VoiceOver (Settings > Accessibility > VoiceOver)
2. Navigate to asset card
3. Swipe to Previous Close section

**Expected Announcement**:
"Önceki kapanış fiyatı: Yüz kırk altı Dolar elli sekiz sent"

**Android TalkBack**:
1. Enable TalkBack (Settings > Accessibility > TalkBack)
2. Navigate to asset card
3. Tap Previous Close section

**Expected Announcement**: Similar to VoiceOver

### Test 21: Font Scaling
**iOS Dynamic Type**:
1. Settings > Accessibility > Display & Text Size > Larger Text
2. Increase font size to maximum
3. Check Previous Close still fits

**Android Font Size**:
1. Settings > Display > Font size
2. Increase to largest
3. Check layout doesn't break

## Error Handling Testing

### Test 22: Network Issues
**Steps**:
1. Enable Airplane Mode
2. Open app (should show cached data)
3. Verify Previous Close shows from cache
4. Disable Airplane Mode
5. Verify data updates

### Test 23: Backend Data Issues
**Scenarios to test**:
- [ ] Backend sends null previousClose
- [ ] Backend sends undefined previousClose
- [ ] Backend sends 0 previousClose
- [ ] Backend sends negative previousClose (invalid)
- [ ] Backend sends NaN previousClose

**Expected**: All cases handled gracefully without crashes

### Test 24: Type Safety
**Open TypeScript compiler**:
```bash
npx tsc --noEmit
```

**Expected**: No type errors related to previousClose

## Console Error Checking

### Test 25: Clean Console
**Open React Native Debugger**:
```bash
# Should see NO errors like:
❌ TypeError: Cannot read property 'previousClose' of undefined
❌ Warning: React does not recognize the `previousClose` prop
❌ Invariant Violation: Text strings must be rendered within a <Text> component

# Should see logs like:
✅ [PriceContext] Normalized price_update: { previousClose: 146.58 }
✅ [PriceContext] ✅ Stock price updated: AAPL = $150.25
```

## Documentation Review

### Test 26: Code Comments
- [ ] Complex logic has comments
- [ ] Turkish labels are explained
- [ ] Null checks are documented
- [ ] Type definitions are clear

### Test 27: Summary Documentation
- [ ] `PREVIOUS_CLOSE_IMPLEMENTATION_SUMMARY.md` is accurate
- [ ] `PREVIOUS_CLOSE_VISUAL_GUIDE.md` matches implementation
- [ ] Screenshots/diagrams are helpful

## Sign-Off Checklist

### Functionality
- [ ] Previous Close displays for BIST stocks
- [ ] Previous Close displays for NASDAQ stocks
- [ ] Previous Close displays for NYSE stocks
- [ ] Null/undefined values handled gracefully
- [ ] Price formatting is consistent
- [ ] Works in full card view
- [ ] Works in compact card view

### Design
- [ ] Turkish label "Önceki Kapanış" displays correctly
- [ ] Compact label "Önc:" displays correctly
- [ ] Styling matches design system
- [ ] Spacing and alignment are correct
- [ ] Colors are appropriate
- [ ] Font sizes are readable

### Performance
- [ ] No performance degradation
- [ ] Smooth scrolling maintained
- [ ] Memory usage acceptable
- [ ] WebSocket updates efficient

### Quality
- [ ] No TypeScript errors
- [ ] No console warnings
- [ ] No runtime errors
- [ ] Code is clean and maintainable
- [ ] Tests pass
- [ ] Documentation complete

### Accessibility
- [ ] Screen reader compatible
- [ ] Font scaling works
- [ ] Color contrast sufficient
- [ ] Touch targets adequate

## Bug Reporting Template

If you find issues, report using this template:

```markdown
### Bug Report: Previous Close Display Issue

**Environment**:
- Device: [iPhone 14 Pro / Samsung Galaxy S23 / etc.]
- OS Version: [iOS 17.0 / Android 13 / etc.]
- App Version: [1.0.0]
- Network: [WiFi / 4G / etc.]

**Steps to Reproduce**:
1. [Open app]
2. [Navigate to Dashboard]
3. [Expand Stock accordion]
4. [...]

**Expected Behavior**:
[Previous Close should display as $146.58]

**Actual Behavior**:
[Previous Close shows as undefined]

**Screenshots**:
[Attach screenshot]

**Console Logs**:
```
[PriceContext] Error: ...
```

**Additional Context**:
[Any other relevant information]
```

## Final Verification

Before marking complete, verify:

1. ✅ All files modified correctly
2. ✅ No breaking changes to existing features
3. ✅ TypeScript compilation succeeds
4. ✅ App builds without errors
5. ✅ Manual testing completed
6. ✅ Documentation written
7. ✅ Code reviewed
8. ✅ Ready for production

## Deployment Checklist

### Pre-Deployment
- [ ] All tests passing
- [ ] No console errors in production build
- [ ] Performance benchmarks met
- [ ] Code review approved
- [ ] Documentation updated

### Post-Deployment
- [ ] Monitor error rates
- [ ] Check analytics for usage
- [ ] Collect user feedback
- [ ] Monitor performance metrics

## Support Resources

### Documentation
- Implementation Summary: `PREVIOUS_CLOSE_IMPLEMENTATION_SUMMARY.md`
- Visual Guide: `PREVIOUS_CLOSE_VISUAL_GUIDE.md`
- Testing Guide: This file

### Code References
- AssetCard: `src/components/dashboard/AssetCard.tsx`
- PriceContext: `src/context/PriceContext.tsx`
- Types: `src/types/index.ts`
- Price Formatting: `src/utils/priceFormatting.ts`

### Contact
For issues or questions:
- Check existing documentation first
- Review console logs for errors
- Consult TypeScript definitions
- Ask team for clarification

---

**Testing completed by**: _________________
**Date**: _________________
**Result**: ☐ PASS  ☐ FAIL  ☐ NEEDS REVISION
**Notes**: _________________
