# myTrader – Tek Parça Ajan Planı (Migrations + Patch + Backoffice + Web)

Bu paket, yapay zeka ajanının adım adım uygulayabileceği **tek parça** bir plan ve gerekli dosyaları içerir:
- **Veri normalizasyonu** (hardcoded asset temizliği, Symbols tabanlı kullanım)
- **Migration**: `NormalizeSymbolsAndAlerts`
- **Service/Endpoint**: Symbols, Users (me), Admin uçları
- **Jobs**: Backfill, Scheduled Backtest (recursive tetikleyici)
- **Mobile patch**: `PriceContext.tsx`, `UserMenu.tsx`
- **Web Backoffice (React/Vite)** basit iskelet
- **Postman** koleksiyonu (Users/Symbols/Admin)

## 1) Ön Hazırlık
- .NET 9 SDK, Node 18+, Postgres bağlantınız hazır olsun.
- Backend projesine referans isimleri kendi çözümünüzdeki namespace’lerle eşleştirilecek.

## 2) Migration Uygulama
- `backend/Migrations/20250910_NormalizeSymbolsAndAlerts.cs` dosyasını **Migrations** projenize kopyalayın.
- `dotnet ef database update` çalıştırın.
- Bu migration:
  - `symbols` tablosuna `display` (görsel ad) ve `is_tracked` sütunlarını ekler.
  - `strategies` tablosundan `symbol` string sütununu kaldırır ve `symbol_id` zorunlu hale getirir (varsa eşleştirme yapar).
  - `user_alerts` tablosunu ekler.
  - `market_data` için `candles` tablosu üzerinden bir **VIEW** oluşturur.

## 3) Servis & Controller Ekle
Aşağıdaki dosyaları projeye kopyalayın ve DI’ya ekleyin:
- `Application/Interfaces/ISymbolService.cs`
- `Application/Services/SymbolService.cs`
- `Application/Interfaces/IAlertService.cs`
- `Application/Services/AlertService.cs` (basit örnek)
- `Api/Controllers/SymbolsController.cs`
- `Api/Controllers/UsersController.cs`
- `Api/Controllers/AdminController.cs`
- DI: `backend/Setup/CoreRegistration.cs` → `builder.Services.AddMyTraderAgentPack();`

> Not: Mevcut `DashboardHub`, `PricesController`, `MarketDataService`, `BinanceWebSocketService` dosyalarında **hardcoded sembolleri kaldırın** ve `ISymbolService.GetTrackedAsync()` ile sembol listesini alın.

## 4) WebSocket/SignalR Abonelik
- WebSocket servisiniz başlangıçta: `var tracked = await symbolService.GetTrackedAsync("BINANCE");`
- Stream URL’i `tracked.Select(t => t.Ticker)` üzerinden kurulsun.
- Hub event payload’ları: `{ symbolId, ticker, display, price, change, ts }`.

## 5) Data Provider Abstraction
- `Application/Interfaces/IHistoricalDataProvider.cs` ve `IRealtimeTickerProvider.cs` arayüzlerini kullanın.
- Binance REST/WS ve Yahoo fallback implementasyonlarını kendi ana projenizde ekleyin.
- `BackfillJob` dosyası UPSERT ile `candles` yazımını gösterir.

## 6) Recursive Backtest (Jobs)
- Hangfire/Quartz ile:
  - **OnSymbolTrackedJob**: Yeni tracked sembolde backfill + backtest kuyruğu.
  - **ScheduledBacktestJob**: Günlük/haftalık tüm aktif stratejiler için backtest kuyruğu.
- `BacktestJob` (örnek dahil edilmedi) mevcut backtest servisinizi çağırmalı.

## 7) Gamification (simülasyon varsayımı)
- Sinyal → “bir sonraki barın açılışı” fiyatından işlem **varsayımı** ile `TradeHistory` kaydı.
- Günlük `StrategyStats` üretip Leaderboard oluşturun (endpointler AdminController altına eklenebilir).

## 8) Alarm Sistemi
- `user_alerts` tablosu ile alarm tanımlama.
- `IAlertService` ile CRUD ve aktif/pasif.
- Evaluator background service (fiyat/sinyal geldiğinde tetikleyin), throttle.

## 9) Mobil Patch’ler
- `frontend/mobile/patches/PriceContext.tsx`: mock veriyi kaldırır; tracked semboller + snapshot + SignalR ile doldurur.
- `frontend/mobile/patches/UserMenu.tsx`: kullanıcı menüsü (profil görüntüleme/düzenleme).
- API:
  - `GET /api/users/me` ve `PATCH /api/users/me`
  - `GET /api/symbols/tracked`
  - `GET /api/prices/live` (halihazırda mevcut olmalı)

## 10) Web Backoffice (React/Vite)
- `frontend/web` klasörünü `npm i && npm run dev` ile başlatın.
- Sayfalar:
  - **Kullanıcı**: `/api/users/me`
  - **Semboller**: `/api/symbols/tracked` listesi
  - **Stratejiler**: Listeleme/backtest tetikleme (TODO)
  - **İndikatörler**: Yeni indikatör kaydı (TODO: `/api/admin/indicators/register`)
- `VITE_API` ile backend URL’ini `.env` üzerinden verin.

## 11) Postman
- `backend/Collections/myTrader_agent_pack.postman_collection.json` dosyasını içe aktarın.

## 12) Kabul Kriterleri (Checklist)
- [ ] Hardcoded semboller ve mock fiyatlar **tamamen kaldırıldı**.
- [ ] Tüm sembol akışı **Symbols** tablosu üzerinden (`IsTracked`) çalışıyor.
- [ ] `Strategy` ve ilgili domainlerde **yalnızca `SymbolId`** kullanılıyor.
- [ ] `candles` UPSERT ile yazılıyor; `market_data` view çalışıyor.
- [ ] `GET /api/symbols/tracked` ve `GET /api/users/me` üretimde.
- [ ] Mobil uygulama snapshot + SignalR ile gerçek zamanlı güncelleniyor.
- [ ] Yeni sembol tracked → backfill → backtest tetikleniyor (recursive).
- [ ] Alarm ve gamification temel iskeletleri hazır.
- [ ] Web backoffice ana ekranı sembolleri ve kullanıcıyı gösterebiliyor.

## 13) Sonraki Aşamalar
- IndicatorRegistry ve IIndicatorService ile yeni indikatör ekleme UI’nin tamamlanması.
- StrategyStats/Leaderboard endpoint’leri ve UI.
- AlertEvaluator background service & bildirim kanalları (email/push/telegram) entegrasyonu.
