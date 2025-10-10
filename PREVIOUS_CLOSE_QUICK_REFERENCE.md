# Previous Close - Quick Reference Guide

## For Developers

### Backend Implementation

**DTO Field:**
```csharp
// backend/MyTrader.Core/DTOs/UnifiedMarketDataDto.cs
public decimal? PreviousClose { get; set; }
```

**Percentage Formula:**
```csharp
PriceChange = CurrentPrice - PreviousClose
PriceChangePercent = (PriceChange / PreviousClose) × 100
```

**Data Source:**
```csharp
// Yahoo Finance API provides actual Previous Close
PreviousClose = marketData.PreviousClose
```

### Mobile UI Implementation

**Display Logic:**
```tsx
{marketData.previousClose !== undefined &&
 marketData.previousClose !== null && (
  <Text>Önceki Kapanış: {formatPrice(marketData.previousClose)}</Text>
)}
```

**Currency Formatting:**
```tsx
const currency = symbol.quoteCurrency === 'TRY' ? 'TRY' : 'USD';
// BIST → ₺  |  NASDAQ/NYSE → $
```

## For Testers

### Test Endpoints

```bash
# Market overview
curl http://localhost:5002/api/market-data/overview

# BIST stocks
curl http://localhost:5002/api/market-data/bist

# NASDAQ stocks
curl http://localhost:5002/api/market-data/nasdaq
```

### Expected Response
```json
{
  "ticker": "AAPL",
  "price": 150.00,
  "previousClose": 146.58,
  "priceChange": 3.42,
  "priceChangePercent": 2.33
}
```

### Test Tools

**Interactive Test Suite:**
```bash
open e2e_previous_close_validation_test.html
```

**Automated Tests:**
```bash
node validate_previous_close_implementation.js
```

## For QA

### Manual Test Checklist

- [ ] Open mobile app
- [ ] Navigate to Dashboard
- [ ] Verify "Önceki Kapanış" displays for BIST stocks
- [ ] Verify "Previous Close" displays for US stocks
- [ ] Check currency formatting (₺ vs $)
- [ ] Verify percentage matches calculation
- [ ] Test with null/zero Previous Close
- [ ] Check compact card view
- [ ] Verify real-time updates via WebSocket

### Edge Cases to Test

1. **Null Previous Close** → Component should not render
2. **Zero Previous Close** → Should show 0%, not crash
3. **Large Numbers** → Should format correctly ($15,000.00)
4. **Small Numbers** → Should format correctly ($0.05)
5. **Negative Change** → Should show negative percent (-2.33%)

## For Product Managers

### Feature Status

✅ **COMPLETE** - Ready for production

### Markets Supported

- ✅ BIST (Borsa İstanbul) - Turkish stocks
- ✅ NASDAQ - US technology stocks
- ✅ NYSE - US financial stocks

### User-Facing Changes

**Full Card View:**
- Shows "Önceki Kapanış: [price]" below current price
- Formatted with proper currency symbol

**Compact Card View:**
- Shows "Önc: [price]" in abbreviated form
- Maintains readability on small screens

### Localization

- Turkish: "Önceki Kapanış" (Previous Close)
- Turkish (Compact): "Önc" (Prev)
- Currency: ₺ for TRY, $ for USD

## For DevOps

### Monitoring

**Key Metrics:**
- PreviousClose population rate: Should be >95%
- Percentage calculation accuracy: Should be 100%
- SignalR broadcast latency: Should be <100ms

**Health Checks:**
```bash
# Backend health
curl http://localhost:5002/api/health

# Market data availability
curl http://localhost:5002/api/market-data/overview | jq '.[] | select(.previousClose != null) | .ticker'
```

### Troubleshooting

**Issue:** Previous Close not displaying
**Solution:** Check Yahoo Finance API connection, verify symbol is active

**Issue:** Incorrect percentage
**Solution:** Verify formula: `(Change / PreviousClose) × 100`

**Issue:** Wrong currency symbol
**Solution:** Check `quoteCurrency` field in symbol configuration

## API Reference

### Get Market Data

**Endpoint:** `GET /api/market-data/overview`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "symbolId": "guid",
      "ticker": "AAPL",
      "price": 150.00,
      "previousClose": 146.58,
      "priceChange": 3.42,
      "priceChangePercent": 2.33,
      "marketStatus": "OPEN",
      "currency": "USD"
    }
  ]
}
```

### SignalR Events

**Event:** `PriceUpdate`

**Payload:**
```json
{
  "Symbol": "AAPL",
  "Price": 150.00,
  "PreviousClose": 146.58,
  "Change24h": 2.33,
  "Timestamp": "2025-10-10T15:30:00Z"
}
```

## Files Modified

### Backend
- `backend/MyTrader.Core/DTOs/UnifiedMarketDataDto.cs`
- `backend/MyTrader.Api/Services/MultiAssetDataBroadcastService.cs`
- `backend/MyTrader.Services/Market/YahooFinancePollingService.cs`

### Mobile
- `frontend/mobile/src/components/dashboard/AssetCard.tsx`
- `frontend/mobile/src/types/index.ts`

## Documentation

- **Full Report:** `PREVIOUS_CLOSE_E2E_VALIDATION_REPORT.md`
- **Summary:** `PREVIOUS_CLOSE_TEST_SUMMARY.md`
- **This Guide:** `PREVIOUS_CLOSE_QUICK_REFERENCE.md`

## Support

**Questions?** Review the full validation report for detailed information.

**Issues?** Run the test suite to identify specific problems:
```bash
node validate_previous_close_implementation.js
```

---

✅ **Status:** Production Ready | **Last Updated:** October 10, 2025
