# Market Data Percentage Calculation - Quick Reference

## Overview
All market data percentage calculations use the standard financial formula with proper zero-division handling.

## Standard Formula

```
PercentChange = ((CurrentPrice - PreviousClose) / PreviousClose) × 100
```

## Implementation Examples

### 1. Stock Data (Yahoo Finance)
**File**: `YahooFinanceProvider.cs`
```csharp
decimal? priceChangePercent = null;
if (previousClose.HasValue && previousClose.Value > 0)
{
    priceChange = currentPrice.Value - previousClose.Value;
    priceChangePercent = (priceChange.Value / previousClose.Value) * 100;
}
```

### 2. Crypto Data (Binance - Reverse Calculation)
**File**: `MultiAssetDataBroadcastService.cs`

Binance provides percentage, so we calculate PreviousClose:
```csharp
// Reverse formula to get PreviousClose from price and percentage
decimal? previousClose = null;
if (priceUpdate.PriceChange != 0 && priceUpdate.Price > 0)
{
    previousClose = priceUpdate.Price / (1 + (priceUpdate.PriceChange / 100));
}
else if (priceUpdate.Price > 0)
{
    previousClose = priceUpdate.Price; // No change = same price
}
```

## Data Sources

### Yahoo Finance API
- Provides: `CurrentPrice`, `PreviousClose`
- Calculate: `PriceChange` and `PriceChangePercent`
- Used for: BIST, NASDAQ, NYSE stocks

### Binance API
- Provides: `CurrentPrice`, `PriceChangePercent` (field "P")
- Calculate: `PreviousClose` (reverse formula)
- Used for: Crypto (BTC, ETH, etc.)

## Test Cases

### Forward Calculation (Stock)
```
Given:
  CurrentPrice = $105
  PreviousClose = $100

Calculate:
  PriceChange = 105 - 100 = $5
  PercentChange = (5 / 100) × 100 = +5.00%
```

### Reverse Calculation (Crypto)
```
Given:
  CurrentPrice = $50,000
  PercentChange = +2.50%

Calculate:
  PreviousClose = 50000 / (1 + 0.025)
                = 50000 / 1.025
                = $48,780.49
```

### Zero Change
```
Given:
  CurrentPrice = $100
  PreviousClose = $100

Calculate:
  PriceChange = 0
  PercentChange = 0.00%
```

### Negative Change
```
Given:
  CurrentPrice = $95
  PreviousClose = $100

Calculate:
  PriceChange = 95 - 100 = -$5
  PercentChange = (-5 / 100) × 100 = -5.00%
```

## Zero-Division Protection

Always check before division:
```csharp
if (previousClose.HasValue && previousClose.Value > 0)
{
    // Safe to calculate percentage
}
else
{
    // Set to null or 0
    priceChangePercent = null; // or 0
}
```

## DTOs with PreviousClose Field

All these DTOs include `PreviousClose`:
- ✅ `UnifiedMarketDataDto`
- ✅ `StockPriceData`
- ✅ `MultiAssetPriceUpdate`

## SignalR Broadcast Fields

Every SignalR message includes:
- `Price` - Current price
- `PreviousClose` - Previous day's closing price
- `Change24h` - Percentage change (already calculated)
- `Metadata.priceChange` - Absolute price change amount
- `Metadata.priceChangePercent` - Redundant percentage for clarity

## Common Mistakes to Avoid

❌ **Wrong**: Using price difference as percentage
```csharp
Change24h = currentPrice - previousClose; // This is AMOUNT, not %
```

✅ **Correct**: Using calculated percentage
```csharp
Change24h = ((currentPrice - previousClose) / previousClose) * 100;
```

❌ **Wrong**: Not handling zero division
```csharp
priceChangePercent = (priceChange / previousClose) * 100; // CRASH if previousClose = 0
```

✅ **Correct**: Check before dividing
```csharp
if (previousClose > 0)
{
    priceChangePercent = (priceChange / previousClose) * 100;
}
```

## Verification Queries

### Check PreviousClose Population
```sql
-- Should return all symbols with non-null PreviousClose
SELECT symbol, price, previous_close,
       price_change_percent
FROM market_data
WHERE previous_close IS NOT NULL
ORDER BY timestamp DESC
LIMIT 100;
```

### Verify Percentage Calculation
```sql
-- Manual verification of percentage
SELECT symbol, price, previous_close,
       price_change_percent AS stored_percent,
       ((price - previous_close) / previous_close * 100) AS calculated_percent,
       ABS(price_change_percent - ((price - previous_close) / previous_close * 100)) AS difference
FROM market_data
WHERE previous_close > 0
  AND price_change_percent IS NOT NULL
ORDER BY timestamp DESC
LIMIT 50;
```

## Files Reference

### Services
- `YahooFinanceProvider.cs` - Stock percentage calculation
- `YahooFinancePollingService.cs` - Uses YahooFinanceProvider
- `BinanceWebSocketService.cs` - Receives percentage from API
- `MultiAssetDataBroadcastService.cs` - PreviousClose reverse calculation for crypto

### DTOs
- `UnifiedMarketDataDto.cs` - Has PreviousClose property
- `StockPriceData.cs` - Has PreviousClose property
- `MultiAssetPriceUpdate.cs` - Has PreviousClose property

### Hubs
- `MarketDataHub.cs` - Broadcasts market data
- `DashboardHub.cs` - Broadcasts dashboard updates

## Monitoring Commands

```bash
# Check logs for percentage calculations
tail -f logs/mytrader.log | grep -i "percent"

# Check for division by zero warnings
tail -f logs/mytrader.log | grep -i "division\|zero"

# Monitor Binance price updates
tail -f logs/mytrader.log | grep -i "binance.*price"
```

## Frontend Display

Mobile should display:
```typescript
{
  symbol: "BTC-USD",
  price: 50000.00,
  previousClose: 48780.49,
  change24h: 2.50,        // This is percentage
  changeAmount: 1219.51   // Calculated: price - previousClose
}
```

## Last Updated
2025-10-10

## Related Documents
- `MARKET_DATA_PERCENTAGE_FIX_SUMMARY.md` - Full implementation details
- `PERCENT_CHANGE_FIX_COMPLETED_SUMMARY.md` - Previous fix documentation
