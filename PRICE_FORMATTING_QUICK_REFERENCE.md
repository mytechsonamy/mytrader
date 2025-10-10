# Price Formatting - Quick Reference Guide

## For Developers

### When to Use Price Normalization

**Always normalize prices when:**
- Receiving data from WebSocket/SignalR
- Fetching data from API endpoints
- Displaying price values to users
- Processing historical price data

**Never normalize:**
- Volume fields
- Percentage values (changePercent, priceChangePercent)
- Already normalized data (check source)

### Usage Examples

#### Mobile Frontend (React Native)

```typescript
import { normalizeMarketData, formatPrice } from '../utils/priceFormatting';

// In WebSocket handler
websocketService.on('price_update', (data: any) => {
  const normalized = normalizeMarketData(data, data.assetClass || 'CRYPTO');

  // Use normalized.price, normalized.change, etc.
  console.log('Price:', normalized.price);
});

// In component for display
const displayPrice = formatPrice(marketData.price, {
  assetClass: 'CRYPTO',
  currency: 'USD'
});
```

#### Web Frontend (React)

```typescript
import { normalizeMarketData, formatPrice } from '../utils/priceFormatting';

// In React Query hook
const { data } = useQuery({
  queryFn: async () => {
    const response = await fetch('/api/market-data');
    const data = await response.json();

    // Normalize before returning
    return normalizeMarketData(data, 'CRYPTO');
  }
});

// In component
<div className="price">
  {formatPrice(data.price, { assetClass: 'CRYPTO' })}
</div>
```

### API Reference

#### `normalizePrice(value, options)`

Normalize a single price value.

```typescript
normalizePrice(2833000000, { assetClass: 'CRYPTO' })
// Returns: 28.33

normalizePrice(2833, { assetClass: 'STOCK' })
// Returns: 28.33

normalizePrice(28.33, { assetClass: 'CRYPTO' })
// Returns: 28.33 (no change needed)
```

#### `normalizeMarketData(data, assetClass)`

Normalize all price fields in a market data object.

```typescript
const raw = {
  price: 2833000000,
  high: 2900000000,
  low: 2700000000,
  volume: 1000000
};

const normalized = normalizeMarketData(raw, 'CRYPTO');
// {
//   price: 28.33,
//   high: 29.00,
//   low: 27.00,
//   volume: 1000000  // unchanged
// }
```

#### `formatPrice(price, options)`

Format a normalized price for display.

```typescript
formatPrice(28.33, {
  assetClass: 'CRYPTO',
  currency: 'USD',
  locale: 'en-US'
})
// Returns: "$28.33"

formatPrice(0.00123456, {
  assetClass: 'CRYPTO',
  currency: 'USD'
})
// Returns: "$0.001235" (more decimals for small values)
```

### Asset Class Specific Rules

#### CRYPTO
- Values > 1 billion → ÷ 100,000,000 (satoshis)
- Values > 100,000 → ÷ 100 (cents)
- Display: 2-8 decimals based on magnitude

#### STOCK
- Values > 10,000 → ÷ 100 (cents)
- Display: 2 decimals

#### FOREX
- Values > 100,000 → ÷ 10,000 (pips)
- Display: 4-5 decimals

### Debugging

Enable debug logging in development:

```typescript
// Mobile
if (__DEV__) {
  console.log('RAW:', rawData);
  console.log('NORMALIZED:', normalizedData);
}

// Web
if (import.meta.env.DEV) {
  console.log('RAW:', rawData);
  console.log('NORMALIZED:', normalizedData);
}
```

### Testing

Test price normalization with edge cases:

```typescript
// Test cases
const testCases = [
  { input: 0.0001, expected: 0.0001, desc: 'Small decimal' },
  { input: 28.33, expected: 28.33, desc: 'Normal decimal' },
  { input: 2833, expected: 28.33, desc: 'Cents' },
  { input: 2833000000, expected: 28.33, desc: 'Satoshis' },
  { input: 100000000000000, expected: 1000000, desc: 'Very large' }
];

testCases.forEach(({ input, expected, desc }) => {
  const result = normalizePrice(input, { assetClass: 'CRYPTO' });
  console.assert(
    Math.abs(result - expected) < 0.01,
    `Failed: ${desc} - Expected ${expected}, got ${result}`
  );
});
```

### Common Pitfalls

#### ❌ DON'T: Normalize volume
```typescript
// WRONG
const normalized = normalizeMarketData(data);
console.log(normalized.volume); // This is already correct!
```

#### ✅ DO: Only normalize price fields
```typescript
// CORRECT
const normalized = normalizeMarketData(data);
console.log(normalized.price);   // Normalized
console.log(data.volume);         // Original
```

#### ❌ DON'T: Double normalize
```typescript
// WRONG - normalizing twice
const normalized1 = normalizeMarketData(data);
const normalized2 = normalizeMarketData(normalized1); // Don't do this!
```

#### ✅ DO: Normalize once at data ingress
```typescript
// CORRECT - normalize when data enters the app
const normalized = normalizeMarketData(data);
// Use normalized data throughout the app
```

### Performance Considerations

- Normalization is fast (< 1ms per object)
- Can safely normalize in real-time WebSocket handlers
- Consider memoization for frequently accessed values

```typescript
// React example with useMemo
const normalizedPrice = useMemo(
  () => normalizePrice(rawPrice, { assetClass }),
  [rawPrice, assetClass]
);
```

### File Locations

- **Mobile**: `frontend/mobile/src/utils/priceFormatting.ts`
- **Web**: `frontend/web/src/utils/priceFormatting.ts`
- **Documentation**: `PRICE_FORMATTING_FIX_SUMMARY.md`
- **Test Script**: `test-price-format.html`

### Support

For questions or issues:
1. Check the comprehensive documentation: `PRICE_FORMATTING_FIX_SUMMARY.md`
2. Review the test script: `test-price-format.html`
3. Examine the utility source code
4. Contact the development team

---

Last updated: 2025-10-08