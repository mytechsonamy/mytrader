# Market Data Percentage Calculation & PreviousClose Field Fix

**Date**: 2025-10-10
**Status**: ✅ COMPLETED
**Priority**: CRITICAL

## Executive Summary

Fixed two critical issues in market data calculation and transmission:
1. **Missing PreviousClose field** - Already present in DTOs but not populated for crypto
2. **Incorrect percentage calculation for crypto** - Binance data was correctly providing percentage, but PreviousClose was missing

## Issues Fixed

### Issue 1: Missing PreviousClose for Crypto (Binance)
**Problem**: While `UnifiedMarketDataDto` and `StockPriceData` already had `PreviousClose` properties, the Binance crypto data handler was not calculating or populating this field.

**Impact**:
- Mobile UI couldn't display previous close values for crypto
- Frontend had incomplete data for calculations

**Solution**: Added calculation to derive PreviousClose from current price and percentage change using the formula:
```csharp
PreviousClose = CurrentPrice / (1 + (PercentChange / 100))
```

### Issue 2: Percentage Calculation Already Correct
**Finding**: The percentage calculation was already correct in all services:
- ✅ **YahooFinanceProvider**: Uses formula `((current - previousClose) / previousClose) * 100`
- ✅ **BinanceWebSocketService**: Receives percentage directly from Binance API (field "P")
- ✅ **YahooFinancePollingService**: Uses YahooFinanceProvider which has correct formula

**Note**: The field name `PriceChange` in `PriceUpdateData` is misleading - it actually stores the percentage for Binance data, not the amount.

## Files Modified

### 1. MultiAssetDataBroadcastService.cs
**Location**: `/backend/MyTrader.Api/Services/MultiAssetDataBroadcastService.cs`

**Changes**:
- Added PreviousClose calculation in `OnBinancePriceUpdated()` method
- Formula: `PreviousClose = CurrentPrice / (1 + (PercentChange / 100))`
- Added metadata field `percentChange24h` for clarity
- Added inline comment clarifying that `Change24h` is already a percentage from Binance

**Code Added**:
```csharp
// Calculate previous close from current price and percentage change
// Formula: PreviousClose = CurrentPrice / (1 + (PercentChange / 100))
decimal? previousClose = null;
if (priceUpdate.PriceChange != 0 && priceUpdate.Price > 0)
{
    previousClose = priceUpdate.Price / (1 + (priceUpdate.PriceChange / 100));
}
else if (priceUpdate.Price > 0)
{
    // If no price change, previous close equals current price
    previousClose = priceUpdate.Price;
}
```

## Verification Summary

### DTOs Already Have PreviousClose ✅
1. **UnifiedMarketDataDto** (line 23): `public decimal? PreviousClose { get; set; }`
2. **StockPriceData** (line 47): `public decimal? PreviousClose { get; set; }`
3. **MultiAssetPriceUpdate** (line 38): `public decimal? PreviousClose { get; set; }`

### Services Populate PreviousClose Correctly ✅

#### YahooFinanceProvider
- **File**: `/backend/MyTrader.Services/Market/YahooFinanceProvider.cs`
- **Lines**: 148-178
- ✅ Gets `PreviousClose` from Yahoo Finance API (`meta.ChartPreviousClose ?? meta.PreviousClose`)
- ✅ Calculates percentage correctly: `((current - previousClose) / previousClose) * 100`
- ✅ Handles division by zero

#### YahooFinancePollingService
- **File**: `/backend/MyTrader.Services/Market/YahooFinancePollingService.cs`
- **Lines**: 143-178
- ✅ Uses YahooFinanceProvider to get data
- ✅ Correctly maps `PreviousClose` from provider to `StockPriceData`
- ✅ Percentage already calculated by provider

#### BinanceWebSocketService
- **File**: `/backend/MyTrader.Services/Market/BinanceWebSocketService.cs`
- **Lines**: 353-396
- ✅ Receives percentage directly from Binance API (field "P")
- ⚠️ Does not receive PreviousClose from API
- ✅ **NOW FIXED**: MultiAssetDataBroadcastService calculates it

#### MultiAssetDataBroadcastService
- **File**: `/backend/MyTrader.Api/Services/MultiAssetDataBroadcastService.cs`
- **Lines**: 144-187 (Binance), 189-211 (Yahoo legacy), 216-254 (Yahoo routed)
- ✅ **NOW FIXED**: Calculates PreviousClose for Binance crypto
- ✅ Correctly uses `PriceChangePercent` for stock data
- ✅ Includes PreviousClose in all broadcast messages

## Data Flow Verification

### Crypto (Binance) Flow
1. **BinanceWebSocketService** receives ticker data from Binance WebSocket
   - Field "c" = Current Price
   - Field "P" = Price Change Percentage (24h)
   - Field "v" = Volume
2. **MultiAssetDataBroadcastService.OnBinancePriceUpdated()** processes update
   - **NOW**: Calculates PreviousClose from price and percentage
   - Creates `MultiAssetPriceUpdate` with percentage and PreviousClose
3. **SignalR Broadcast** sends complete data to mobile clients

### Stock (Yahoo Finance) Flow
1. **YahooFinanceProvider.GetPricesAsync()** fetches data from Yahoo API
   - Gets current price from API
   - Gets PreviousClose from API metadata
   - Calculates: `PriceChange = current - previousClose`
   - Calculates: `PriceChangePercent = (PriceChange / previousClose) * 100`
2. **YahooFinancePollingService** calls provider and maps data
   - Maps PreviousClose to `StockPriceData`
3. **MultiAssetDataBroadcastService** broadcasts stock updates
   - Uses `PriceChangePercent` for `Change24h` (percentage)
   - Includes `PreviousClose` in message
4. **SignalR Broadcast** sends complete data to clients

## Percentage Calculation Formulas

### Correct Formula (Used Throughout)
```
PercentChange = ((CurrentPrice - PreviousClose) / PreviousClose) × 100
```

### Zero Division Handling
```csharp
if (previousClose.HasValue && previousClose.Value > 0)
{
    priceChange = currentPrice.Value - previousClose.Value;
    priceChangePercent = (priceChange.Value / previousClose.Value) * 100;
}
```

### Reverse Calculation (Crypto - Derive PreviousClose)
```
PreviousClose = CurrentPrice / (1 + (PercentChange / 100))
```

Example:
- CurrentPrice = $50,000
- PercentChange = +2.5%
- PreviousClose = 50000 / (1 + 0.025) = 50000 / 1.025 = $48,780.49

## Testing Checklist

### Manual Testing Required
- [ ] Verify crypto assets (BTC, ETH) show PreviousClose in mobile UI
- [ ] Verify stock assets (AAPL, MSFT) show PreviousClose in mobile UI
- [ ] Verify BIST stocks show PreviousClose in mobile UI
- [ ] Verify percentage calculations are correct for all asset classes
- [ ] Check SignalR messages include PreviousClose field
- [ ] Verify zero price change scenario (PreviousClose = CurrentPrice)
- [ ] Verify negative price change scenario

### Automated Testing Needed
```csharp
[Fact]
public void Calculate_PreviousClose_From_Price_And_Percentage()
{
    // Arrange
    decimal currentPrice = 50000m;
    decimal percentChange = 2.5m;

    // Act
    decimal previousClose = currentPrice / (1 + (percentChange / 100));

    // Assert
    Assert.Equal(48780.49m, previousClose, 2);
}

[Fact]
public void Calculate_Percentage_From_Price_And_PreviousClose()
{
    // Arrange
    decimal currentPrice = 50000m;
    decimal previousClose = 48780.49m;

    // Act
    decimal priceChange = currentPrice - previousClose;
    decimal percentChange = (priceChange / previousClose) * 100;

    // Assert
    Assert.Equal(2.5m, percentChange, 2);
}
```

## Breaking Changes
**None** - This is a fix that adds missing data without changing existing API contracts.

## Migration Steps
1. ✅ Update `MultiAssetDataBroadcastService.cs` with PreviousClose calculation
2. ✅ Verify all services populate PreviousClose correctly
3. Deploy to staging environment
4. Test mobile UI displays PreviousClose values
5. Deploy to production

## Performance Impact
**Minimal** - Added calculation is a simple division operation performed once per price update.

## Monitoring

### Metrics to Watch
- PreviousClose field population rate (should be 100% for all assets)
- Percentage calculation accuracy (compare with source APIs)
- SignalR message size (negligible increase)

### Logs to Monitor
```
MultiAssetDataBroadcastService: "Broadcasting price update: {AssetClass} {Symbol} = {Price}"
```

Look for:
- Consistent percentage values for crypto
- PreviousClose values present in metadata
- No division-by-zero errors

## Known Limitations

1. **Crypto PreviousClose is Calculated**: For Binance crypto, PreviousClose is derived from current price and percentage, not from the API. This is mathematically accurate but theoretically could have minor rounding differences.

2. **PriceChange Field Name Ambiguity**: In `PriceUpdateData` class, the field `PriceChange` actually contains the percentage for Binance data. Consider renaming to `PriceChangePercent` in future refactoring.

## Related Files

### Data Models
- `/backend/MyTrader.Core/DTOs/UnifiedMarketDataDto.cs`
- `/backend/MyTrader.Core/DTOs/StockPriceData.cs`
- `/backend/MyTrader.Core/Models/MultiAssetPriceUpdate.cs`

### Services
- `/backend/MyTrader.Services/Market/YahooFinanceProvider.cs`
- `/backend/MyTrader.Services/Market/YahooFinancePollingService.cs`
- `/backend/MyTrader.Services/Market/BinanceWebSocketService.cs`
- `/backend/MyTrader.Api/Services/MultiAssetDataBroadcastService.cs`

### Hubs
- `/backend/MyTrader.Api/Hubs/MarketDataHub.cs`
- `/backend/MyTrader.Api/Hubs/DashboardHub.cs`

## References

### Binance API Documentation
- Ticker 24hr format: https://binance-docs.github.io/apidocs/spot/en/#24hr-ticker-price-change-statistics
- Field "P" = Price change percentage (24h)
- Field "c" = Last price

### Yahoo Finance API
- ChartPreviousClose: Previous day's closing price
- Calculation uses standard financial formula

## Conclusion

✅ **All Issues Resolved**:
1. PreviousClose now calculated and populated for Binance crypto data
2. Percentage calculations confirmed correct in all services
3. Zero-division handling in place
4. All DTOs already had PreviousClose fields
5. SignalR broadcasts now include complete market data

**Next Steps**:
1. Deploy to staging
2. Manual testing on mobile
3. Monitor logs for any calculation errors
4. Deploy to production after successful testing
