# Web Frontend Fixes - Quick Reference

## Files Modified

### 1. Dashboard.tsx
**Location**: `/frontend/web/src/pages/Dashboard.tsx`

**Changes**:
- Line 6: Added `useNavigate` import
- Line 16: Added `navigate` instance
- Lines 57-60: Added click handler to market cards

```tsx
// Added navigation import
import { Link, useNavigate } from 'react-router-dom';

// Added navigate instance
const navigate = useNavigate();

// Added onClick to cards
<Card
  onClick={() => navigate(`/strategies/test?symbol=${item.symbol}&name=${encodeURIComponent(item.name || item.symbol)}`)}
>
```

---

### 2. Markets.tsx
**Location**: `/frontend/web/src/pages/Markets.tsx`

**Changes**:
- Line 6: Added `useNavigate` import
- Line 14: Added `navigate` instance
- Line 97: Removed duplicate icon from asset class filters
- Lines 138-141: Added click handler to grid view cards
- Lines 207-210: Added click handler to list view rows

```tsx
// Removed duplicate icon
// Before: {assetClass.icon} {assetClass.displayName}
// After: {assetClass.displayName}

// Added onClick to cards
<Card
  onClick={() => navigate(`/strategies/test?symbol=${item.symbol}&name=${encodeURIComponent(item.name || item.symbol)}`)}
>

// Added onClick to table rows
<tr
  onClick={() => navigate(`/strategies/test?symbol=${item.symbol}&name=${encodeURIComponent(item.name || item.symbol)}`)}
>
```

---

### 3. StrategyTest.tsx (NEW)
**Location**: `/frontend/web/src/pages/StrategyTest.tsx`

**Purpose**: Test trading strategies on specific symbols

**Key Features**:
- Reads symbol and name from URL query parameters
- Displays current price prominently at top
- Shows 24h change, high, and low
- Strategy configuration interface
- Backtest results placeholder
- Quick actions sidebar

**URL Pattern**: `/strategies/test?symbol=BTC&name=Bitcoin`

---

### 4. App.tsx
**Location**: `/frontend/web/src/App.tsx`

**Changes**:
- Line 14: Added `StrategyTest` import
- Lines 133-142: Added `/strategies/test` route

```tsx
import StrategyTest from './pages/StrategyTest';

<Route
  path="/strategies/test"
  element={
    <ProtectedRoute>
      <ErrorBoundary fallback={<ErrorFallback error="Strategy Test Error" />}>
        <StrategyTest />
      </ErrorBoundary>
    </ProtectedRoute>
  }
/>
```

---

### 5. tsconfig.json
**Location**: `/frontend/web/tsconfig.json`

**Changes**:
- Line 70: Simplified include array to only `"src"`
- Line 83: Added `"src/test-utils.disabled"` to exclude array

```json
"include": [
  "src"
],
"exclude": [
  "node_modules",
  "dist",
  "coverage",
  "e2e",
  "playwright-report",
  "test-results",
  "temp_tests",
  "src/test-utils.disabled",
  "src/**/*.test.ts",
  "src/**/*.test.tsx",
  "src/**/*.spec.ts",
  "src/**/*.spec.tsx"
],
```

---

## Navigation Flow

```
Dashboard (/)
  └─ Click Asset Card
      └─ Navigate to: /strategies/test?symbol=BTC&name=Bitcoin

Markets (/markets)
  ├─ Grid View
  │   └─ Click Asset Card
  │       └─ Navigate to: /strategies/test?symbol=BTC&name=Bitcoin
  │
  └─ List View
      └─ Click Table Row
          └─ Navigate to: /strategies/test?symbol=BTC&name=Bitcoin

Strategy Test (/strategies/test)
  ├─ Displays current price
  ├─ Configure strategy
  ├─ Run backtest
  └─ Quick Actions
      ├─ My Strategies → /strategies
      ├─ Browse Markets → /markets
      └─ View Portfolio → /portfolio
```

---

## Key Components

### Price Display Card
```tsx
<Card className="bg-gradient-to-br from-brand-50 to-brand-100 border-brand-200">
  <CardContent className="p-6">
    <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
      {/* Current Price */}
      <div>
        <p className="text-3xl font-bold">$45,123.45</p>
      </div>
      {/* 24h Change */}
      {/* 24h High */}
      {/* 24h Low */}
    </div>
  </CardContent>
</Card>
```

### Asset Card Click Handler
```tsx
onClick={() => navigate(`/strategies/test?symbol=${symbol}&name=${encodeURIComponent(name)}`)}
```

### Reading Query Parameters
```tsx
const [searchParams] = useSearchParams();
const symbol = searchParams.get('symbol') || '';
const name = searchParams.get('name') || symbol;
```

---

## Testing Commands

```bash
# Start dev server
cd frontend/web
npm run dev

# Build for production
npm run build

# Run linter
npm run lint

# Run type check
npx tsc --noEmit
```

---

## Test URLs

```
# Dashboard
http://localhost:3000/

# Markets
http://localhost:3000/markets

# Strategy Test (with symbol)
http://localhost:3000/strategies/test?symbol=BTC&name=Bitcoin

# Strategy Test (without symbol - shows error)
http://localhost:3000/strategies/test

# All Strategies
http://localhost:3000/strategies
```

---

## Common Issues & Solutions

### Issue: "No Symbol Selected" error
**Cause**: Navigated directly to `/strategies/test` without query parameters
**Solution**: Always include `?symbol=...&name=...` in URL or navigate from asset card

### Issue: Price not displaying
**Cause**: Market data not loaded yet or symbol not found
**Solution**: Check that symbol exists in market data, wait for data to load

### Issue: Click not navigating
**Cause**: Event bubbling or missing onClick handler
**Solution**: Verify onClick is on the Card/tr element, not nested inside

---

## Color Coding Reference

```tsx
// Positive changes (gains)
text-positive-500  // Green

// Negative changes (losses)
text-negative-500  // Red

// Neutral/default
text-text-primary  // Default text color

// Background gradients
bg-gradient-to-br from-brand-50 to-brand-100  // Price card
```

---

## Responsive Breakpoints

```
Mobile: < 768px     → 1 column grid
Tablet: 768-1024px  → 2 columns
Desktop: > 1024px   → 4 columns
```

---

## Quick Tips

1. **Navigation**: Always use `navigate()` from `useNavigate()` hook
2. **URL Encoding**: Use `encodeURIComponent()` for names with special characters
3. **Query Params**: Use `useSearchParams()` hook to read query parameters
4. **Error Handling**: Wrap routes in ErrorBoundary and ProtectedRoute
5. **Formatting**: Use `formatCurrency()` and `formatPercentage()` utilities
6. **Memoization**: Use `useMemo()` for expensive computations
7. **Real-time Data**: Hook into existing WebSocket for live updates

---

## Related Documentation

- Full details: `WEB_FRONTEND_FIXES_SUMMARY.md`
- Mobile fixes: `MOBILE_CRITICAL_FIXES_SUMMARY.md`
- API contracts: `api-contracts/`
- Design system: `DESIGN_TOKENS_SPECIFICATION.md`

---

**Version**: 1.0
**Last Updated**: 2025-10-09
