# DataSourceBadge Component - Usage Guide

## Overview

The DataSourceBadge is a lightweight React component that displays the data source for market prices. It provides transparency about whether data is coming from Alpaca (real-time) or Yahoo Finance (delayed).

---

## Quick Start

### Import
```typescript
import DataSourceBadge from './components/dashboard/DataSourceBadge';
import type { StockPriceData } from './types';
```

### Basic Usage
```tsx
<DataSourceBadge
  source="ALPACA"
  isRealtime={true}
  qualityScore={100}
  timestamp="2025-10-09T20:45:23Z"
/>
```

---

## Props Interface

```typescript
interface DataSourceBadgeProps {
  source?: string | undefined;          // "ALPACA" | "YAHOO_FALLBACK" | "YAHOO_REALTIME"
  isRealtime?: boolean | undefined;     // true for real-time, false for delayed
  qualityScore?: number | undefined;    // 100 for Alpaca, 80 for Yahoo
  timestamp?: string | undefined;       // ISO timestamp of last update
  className?: string | undefined;       // Optional CSS class
}
```

---

## Usage Examples

### Example 1: Alpaca Real-time Data
```tsx
const priceData: StockPriceData = {
  symbol: "BTCUSD",
  price: 42531.20,
  volume: 1234567,
  timestamp: "2025-10-09T20:45:23Z",
  source: "ALPACA",
  qualityScore: 100,
  isRealtime: true
};

<div className="price-container">
  <span className="price">${priceData.price.toFixed(2)}</span>
  <DataSourceBadge
    source={priceData.source}
    isRealtime={priceData.isRealtime}
    qualityScore={priceData.qualityScore}
    timestamp={priceData.timestamp}
  />
</div>

// Displays: $42,531.20 [Live]
```

### Example 2: Yahoo Finance Delayed Data
```tsx
const priceData: StockPriceData = {
  symbol: "AAPL",
  price: 178.45,
  volume: 9876543,
  timestamp: "2025-10-09T20:44:00Z",
  source: "YAHOO_FALLBACK",
  qualityScore: 80,
  isRealtime: false
};

<DataSourceBadge
  source={priceData.source}
  isRealtime={priceData.isRealtime}
  qualityScore={priceData.qualityScore}
  timestamp={priceData.timestamp}
/>

// Displays: [Delayed]
```

### Example 3: Backward Compatible (No Source)
```tsx
const legacyPriceData = {
  symbol: "ETHUSD",
  price: 2250.30,
  volume: 543210,
  timestamp: "2025-10-09T20:45:23Z"
  // No source field
};

<DataSourceBadge
  source={legacyPriceData.source}
  isRealtime={legacyPriceData.isRealtime}
/>

// Displays: Nothing (badge hidden)
```

---

## Behavior

### Display Logic
- **source = "ALPACA"** → Shows "Live" badge (green)
- **source = "YAHOO_FALLBACK"** → Shows "Delayed" badge (yellow)
- **source = "YAHOO_REALTIME"** → Shows "Live" badge (green)
- **isRealtime = true** → Shows "Live" badge (green)
- **isRealtime = false** → Shows "Delayed" badge (yellow)
- **source = undefined** → Badge hidden (returns null)

### Tooltip Information
Hover over the badge to see:
```
Source: Alpaca (Real-time)
Quality: 100%
Last update: 8:45:23 PM
```

---

## Styling

### Default Styles
The component comes with pre-defined styles in `DataSourceBadge.css`:

**Real-time Badge (Green):**
- Background: #10b981
- Pulse animation (2s)
- Box shadow

**Delayed Badge (Yellow):**
- Background: #f59e0b
- No animation
- Box shadow

### Custom Styling
Add custom classes via the `className` prop:

```tsx
<DataSourceBadge
  source="ALPACA"
  className="my-custom-badge"
/>
```

```css
.my-custom-badge {
  margin-left: 10px;
  font-size: 0.75rem;
}
```

---

## Integration with Market Data

### With StockPriceData Interface
```typescript
import type { StockPriceData } from './types';

interface PriceCardProps {
  data: StockPriceData;
}

const PriceCard: React.FC<PriceCardProps> = ({ data }) => {
  return (
    <div className="price-card">
      <h3>{data.symbol}</h3>
      <div className="price-row">
        <span className="price">${data.price.toFixed(2)}</span>
        <DataSourceBadge
          source={data.source}
          isRealtime={data.isRealtime}
          qualityScore={data.qualityScore}
          timestamp={data.timestamp}
        />
      </div>
    </div>
  );
};
```

### With SignalR WebSocket Updates
```typescript
useEffect(() => {
  signalRService.onPriceUpdate((priceData: StockPriceData) => {
    setPriceData(priceData);
    // Badge will automatically update based on priceData.source
  });
}, []);

<DataSourceBadge
  source={priceData.source}
  isRealtime={priceData.isRealtime}
  qualityScore={priceData.qualityScore}
  timestamp={priceData.timestamp}
/>
```

---

## Accessibility

### ARIA Support
The badge includes proper ARIA labels:

```html
<span
  class="data-source-badge realtime"
  aria-label="Data source: Live"
  title="Source: Alpaca (Real-time)..."
>
  Live
</span>
```

### Keyboard Navigation
- Badge is focusable with tab key
- Focus styles clearly indicate selection
- Tooltip accessible via keyboard

### Screen Readers
Screen readers will announce:
- Badge text ("Live" or "Delayed")
- aria-label with context
- Tooltip information on focus

---

## Responsive Design

### Mobile (< 768px)
- Smaller font size (0.6rem)
- Reduced padding (1px 5px)
- Smaller margin (4px)

### Desktop
- Default font size (0.65rem)
- Standard padding (2px 6px)
- Standard margin (6px)

---

## Performance Considerations

### Optimization Tips
1. **Memoization:** Wrap in React.memo if parent re-renders frequently
```tsx
const MemoizedBadge = React.memo(DataSourceBadge);
```

2. **Conditional Rendering:** Only render when data is available
```tsx
{priceData.source && (
  <DataSourceBadge source={priceData.source} />
)}
```

3. **Animation:** Disable animations if performance is critical
```css
@media (prefers-reduced-motion: reduce) {
  .data-source-badge.realtime {
    animation: none;
  }
}
```

---

## Testing

### Unit Test Example (Jest + React Testing Library)
```typescript
import { render, screen } from '@testing-library/react';
import DataSourceBadge from './DataSourceBadge';

describe('DataSourceBadge', () => {
  it('displays Live badge for Alpaca source', () => {
    render(
      <DataSourceBadge
        source="ALPACA"
        isRealtime={true}
        qualityScore={100}
      />
    );

    const badge = screen.getByText('Live');
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveClass('realtime');
  });

  it('displays Delayed badge for Yahoo fallback', () => {
    render(
      <DataSourceBadge
        source="YAHOO_FALLBACK"
        isRealtime={false}
        qualityScore={80}
      />
    );

    const badge = screen.getByText('Delayed');
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveClass('delayed');
  });

  it('returns null when source is undefined', () => {
    const { container } = render(<DataSourceBadge />);
    expect(container.firstChild).toBeNull();
  });
});
```

---

## Troubleshooting

### Issue: Badge not displaying
**Solution:** Check if `source` prop is defined and not undefined

### Issue: Wrong color showing
**Solution:** Verify `source` value matches expected enum values

### Issue: Tooltip not showing
**Solution:** Check CSS z-index and title attribute is present

### Issue: Animation not working
**Solution:** Check for `prefers-reduced-motion` setting or browser compatibility

---

## Best Practices

1. **Always pass timestamp:** Helps users understand data freshness
2. **Use TypeScript:** Leverage StockPriceData interface for type safety
3. **Consistent placement:** Place badge next to price for clarity
4. **Test fallback:** Ensure component works when source is undefined
5. **Respect user preferences:** Honor reduced motion settings
6. **Monitor performance:** Check if badge impacts render times

---

## Browser Support

| Browser | Minimum Version | Notes |
|---------|----------------|-------|
| Chrome | 90+ | Full support |
| Firefox | 88+ | Full support |
| Safari | 14+ | Full support |
| Edge | 90+ | Full support |
| Mobile Safari | 14+ | Full support |
| Chrome Mobile | 90+ | Full support |

---

## Related Documentation

- [StockPriceData Interface](../src/types/index.ts)
- [MarketOverview Component](../src/components/dashboard/MarketOverview.tsx)
- [Alpaca Integration Guide](../../backend/ALPACA_INTEGRATION.md)
- [Design System](./DESIGN_SYSTEM.md)

---

## Support

For questions or issues:
1. Check this guide
2. Review test report: `test-alpaca-ui-enhancements.html`
3. Check component source: `src/components/dashboard/DataSourceBadge.tsx`
4. Consult implementation summary: `ALPACA_UI_ENHANCEMENTS_COMPLETE.md`

---

**Last Updated:** October 9, 2025
**Component Version:** 1.0.0
**Maintainer:** React Frontend Team
