# Percent Change Calculation Fix Report

**Date**: 10 Ekim 2025
**Issue**: %Değişim hesaplama hatası - Tutarsal değişim yüzdesel olarak yanlış gösteriliyor
**Status**: 🔧 FIX IN PROGRESS

---

## 🎯 Kullanıcı Raporu

> "hisse senetleri için, tamamında %sel değişim hatalı, tutarsal değişim yüzdesel değişim olarak geliyor"

**Sorun**: Frontend'de gösterilen yüzde değişim değerleri yanlış - bazen sıfır, bazen farklı değerler.

---

## 🔍 Root Cause Analysis

### Doğru Hesaplama (Beklenen)

```
PriceChange = CurrentPrice - PreviousClose
PriceChangePercent = (PriceChange / PreviousClose) × 100

Örnek:
- Previous Close: ₺100.00
- Current Price: ₺102.00
- ✅ PriceChange = ₺2.00
- ✅ PriceChangePercent = +2.00%
```

### Yanlış Hesaplama (Mevcut Durum)

Backend'de bazı servisler **intraday change** (Close - Open) kullanıyor:

```
❌ PriceChange = Close - Open (Gün içi değişim)
❌ PriceChangePercent = ((Close - Open) / Open) × 100

Örnek:
- Previous Close: ₺100.00
- Open: ₺101.00 (Pre-market hareketi)
- Current Close: ₺102.00
- ❌ PriceChange = ₺1.00 (Yanlış! Gün içi değişim)
- ❌ PriceChangePercent = +0.99% (Yanlış!)
- ✅ Doğru olması gereken: +2.00%
```

---

## 📊 Etkilenen Servisler

### 1. ✅ YahooFinanceProvider.cs (DOĞRU)

**Dosya**: `backend/MyTrader.Services/Market/YahooFinanceProvider.cs`
**Satırlar**: 164-167

```csharp
// ✅ DOĞRU: Previous close kullanıyor
if (previousClose.HasValue && previousClose.Value > 0)
{
    priceChange = currentPrice.Value - previousClose.Value;
    priceChangePercent = (priceChange.Value / previousClose.Value) * 100;
}
```

**Durum**: ✅ **NO CHANGE NEEDED** - Bu servis doğru çalışıyor.

---

### 2. ❌ YahooFinanceApiService.cs (YANLIŞ)

**Dosya**: `backend/MyTrader.Core/Services/YahooFinanceApiService.cs`
**Satırlar**: 324-329 (ParseHistoricalDataResponse) ve 377-381 (ParseIntradayDataResponse)

```csharp
// ❌ YANLIŞLIKLA OPEN'I PREVIOUSCLOSE OLARAK KULLANIYOR
if (record.ClosePrice.HasValue && record.OpenPrice.HasValue)
{
    record.PreviousClose = record.OpenPrice; // ❌ Approximation (yanlış!)
    record.PriceChange = record.ClosePrice.Value - record.OpenPrice.Value; // ❌ Intraday
    record.PriceChangePercent = record.OpenPrice.Value != 0 ?
        (record.PriceChange / record.OpenPrice.Value * 100) : 0;
}
```

**Problem**:
- `PreviousClose` olarak `OpenPrice` kullanılıyor (yanlış approximation)
- `PriceChange` gün içi değişim olarak hesaplanıyor (Close - Open)

**Çözüm**: Yahoo Finance API'den gelen `ChartPreviousClose` veya `PreviousClose` field'ını kullanmalı.

---

### 3. ❌ MultiAssetDataService.cs (YANLIŞ)

**Dosya**: `backend/MyTrader.Infrastructure/Services/MultiAssetDataService.cs`
**Satırlar**: 424-426 (GetTopByVolumePerAssetClassAsync)

```csharp
// ❌ VOLUME LEADERS İÇİN YANLIŞ HESAPLAMA
select new VolumeLeaderDto
{
    ...
    Price = x.md.Close,
    PriceChange = x.md.Close - x.md.Open, // ❌ Intraday change
    PriceChangePercent = x.md.Open != 0 ?
        ((x.md.Close - x.md.Open) / x.md.Open) * 100 : 0,
    ...
}
```

**Problem**: Volume leaders için gün içi değişim gösteriliyor.

**Çözüm**: `MarketData` tablosunda `PreviousClose` field'ı varsa kullanmalı, yoksa bir önceki günün `Close`'unu query ile almalı.

---

### 4. ❌ YahooFinancePollingService.cs (YANLIŞ)

**Dosya**: `backend/MyTrader.Services/Market/YahooFinancePollingService.cs`
**Satırlar**: 156-167

```csharp
// ❌ ÖNCEKİ POLL'U PREVIOUSCLOSE OLARAK KULLANIYOR
decimal priceChange = 0;
decimal priceChangePercent = 0;

if (_latestPrices.TryGetValue(symbol.Ticker, out var previousPrice))
{
    priceChange = price - previousPrice.Price; // ❌ Previous poll, not previous close
    if (previousPrice.Price != 0)
    {
        priceChangePercent = (priceChange / previousPrice.Price) * 100;
    }
}
```

**Problem**:
- Bir önceki polling değerini `PreviousClose` olarak kullanıyor
- Bu her dakikada değiştiği için hatalı yüzde gösteriyor

**Çözüm**: Yahoo Finance API'den `PreviousClose` değerini almalı ve bunu kullanmalı.

---

### 5. ❌ DataImportService.cs (YANLIŞ)

**Dosya**: `backend/MyTrader.Core/Services/DataImportService.cs`
**Satırlar**: 889-890, 942-943

```csharp
// ❌ INTRADAY CHANGE KULLANILIYOR
record.PriceChange = record.ClosePrice - record.OpenPrice;
record.PriceChangePercent = record.OpenPrice > 0 ?
    (record.PriceChange / record.OpenPrice * 100) : 0;
```

**Problem**: Historical data import sırasında intraday change hesaplanıyor.

**Çözüm**: Bir önceki record'un `ClosePrice`'ını kullanmalı.

---

### 6. ❌ AssetController.cs (YANLIŞ)

**Dosya**: `backend/MyTrader.Api/Controllers/AssetController.cs`
**Satırlar**: 121-122

```csharp
// ❌ INTRADAY CHANGE
PriceChange = md.Close - md.Open,
PriceChangePercent = md.Open > 0 ? ((md.Close - md.Open) / md.Open) * 100 : 0,
```

**Problem**: Asset controller API endpoint'i yanlış hesaplama yapıyor.

---

## 🔧 Fix Strategy

### Strategy 1: Use PreviousClose from Data Source (Preferred)

Yahoo Finance API zaten `PreviousClose` değeri sağlıyor:

```json
{
  "chart": {
    "result": [{
      "meta": {
        "chartPreviousClose": 100.50,
        "previousClose": 100.50,
        "regularMarketPrice": 102.00
      }
    }]
  }
}
```

**Avantaj**: Doğrudan veri kaynağından geliyor, hesaplama hatası yok.

### Strategy 2: Database Query for Previous Close

Eğer veri kaynağı `PreviousClose` sağlamıyorsa:

```csharp
// Get previous trading day's close
var previousClose = await _context.MarketData
    .Where(md => md.Symbol == symbol && md.Timestamp < currentTimestamp)
    .OrderByDescending(md => md.Timestamp)
    .Select(md => md.Close)
    .FirstOrDefaultAsync();
```

---

## 📝 Implementation Plan

### Phase 1: Critical Fixes (High Priority)

1. **YahooFinanceApiService.cs**
   - Lines 324-329: Fix `ParseHistoricalDataResponse` to NOT set `PreviousClose = OpenPrice`
   - Lines 377-381: Fix `ParseIntradayDataResponse` to use actual previous close
   - **Impact**: Historical data imports will be correct

2. **YahooFinancePollingService.cs**
   - Lines 156-167: Use Yahoo API's `PreviousClose` instead of previous poll
   - **Impact**: Real-time stock updates will show correct percent change

3. **MultiAssetDataService.cs**
   - Lines 424-426: Fix volume leaders calculation
   - **Impact**: Dashboard volume leaders will show correct changes

### Phase 2: API Controllers (Medium Priority)

4. **AssetController.cs**
   - Lines 121-122: Fix asset endpoint calculation
   - **Impact**: Asset API responses will be correct

### Phase 3: Data Import (Low Priority - Historical Data Only)

5. **DataImportService.cs**
   - Lines 889-890, 942-943: Fix batch import calculations
   - **Impact**: Future data imports will calculate correctly

---

## ✅ Expected Results After Fix

### Before Fix:
```
GARAN (BIST)
- Previous Close: ₺130.00
- Open: ₺130.50
- Current: ₺131.00
- ❌ Shows: +0.38% (calculated from open: 131-130.5)
```

### After Fix:
```
GARAN (BIST)
- Previous Close: ₺130.00
- Open: ₺130.50
- Current: ₺131.00
- ✅ Shows: +0.77% (calculated from prev close: 131-130)
```

---

## 🧪 Testing Plan

### Test Cases:

1. **BIST Stock (GARAN)**:
   - Previous Close: ₺130.00
   - Current Price: ₺131.17
   - Expected %Change: +0.90%

2. **NASDAQ Stock (AAPL)**:
   - Previous Close: $254.04
   - Current Price: $254.00
   - Expected %Change: -0.02%

3. **Pre-market Movement Test**:
   - Previous Close: $100.00
   - Pre-market Open: $102.00
   - Regular Market Price: $103.00
   - Expected %Change: +3.00% (not +0.98%)

### Validation:
```sql
-- Check market data calculations
SELECT
    symbol,
    timestamp,
    open,
    close,
    close - open as intraday_change, -- ❌ Wrong
    -- Need to add PreviousClose column to verify correct calculation
FROM market_data
WHERE symbol IN ('GARAN', 'AAPL', 'MSFT')
ORDER BY timestamp DESC
LIMIT 10;
```

---

## 💡 Additional Recommendations

### 1. Add PreviousClose Column to MarketData Table

```sql
ALTER TABLE market_data
ADD COLUMN previous_close DECIMAL(20, 8);

CREATE INDEX idx_market_data_previous_close
ON market_data(symbol, timestamp, previous_close);
```

### 2. Create Calculated View for Easy Validation

```sql
CREATE OR REPLACE VIEW vw_market_data_with_changes AS
SELECT
    id,
    symbol,
    timestamp,
    open,
    high,
    low,
    close,
    previous_close,
    close - previous_close AS price_change,
    CASE
        WHEN previous_close > 0 THEN
            ((close - previous_close) / previous_close) * 100
        ELSE 0
    END AS price_change_percent,
    close - open AS intraday_change,
    CASE
        WHEN open > 0 THEN
            ((close - open) / open) * 100
        ELSE 0
    END AS intraday_change_percent
FROM market_data;
```

### 3. Add Data Quality Check

```csharp
// Validation: Ensure PreviousClose is reasonable
if (priceChange.HasValue && previousClose.HasValue && currentPrice.HasValue)
{
    var percentChange = Math.Abs(priceChange.Value / previousClose.Value * 100);
    if (percentChange > 50) // Suspicious if >50% change
    {
        _logger.LogWarning(
            "Suspicious price change for {Symbol}: {Change}% (Prev: {Prev}, Current: {Current})",
            symbol, percentChange, previousClose, currentPrice);
    }
}
```

---

## 📋 File Changes Summary

| File | Lines | Type | Priority |
|------|-------|------|----------|
| YahooFinanceApiService.cs | 324-329, 377-381 | Fix | HIGH |
| YahooFinancePollingService.cs | 156-167 | Fix | HIGH |
| MultiAssetDataService.cs | 424-426 | Fix | HIGH |
| AssetController.cs | 121-122 | Fix | MEDIUM |
| DataImportService.cs | 889-890, 942-943 | Fix | LOW |
| YahooFinanceProvider.cs | 164-167 | ✅ No Change | - |

---

## 🎯 Next Steps

1. ✅ Create this analysis report
2. 🔧 Implement fixes in HIGH priority files
3. 🧪 Test with real market data
4. 📊 Verify frontend displays correct percentages
5. 📝 Update DTO documentation to clarify calculation method
6. 🗄️ Consider adding `previous_close` column to `market_data` table

---

**Prepared By**: Claude Code
**Date**: 10 Ekim 2025
**Version**: 1.0
**Status**: Ready for Implementation
