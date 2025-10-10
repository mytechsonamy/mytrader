# Entity Relationship Diagram & Implementation Guide

**Document Version:** 1.0
**Date:** 2025-01-08
**Author:** Data Architecture Manager
**Status:** Complete Design Specification

---

## Complete Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    MYTRADER DATA-DRIVEN SYMBOL ARCHITECTURE                   │
│                         Complete Entity Relationship Model                     │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────┐
│       ASSET_CLASSES          │
│──────────────────────────────│
│ PK id                  UUID  │
│ UK code                VARCHAR(20)  ← 'CRYPTO', 'STOCK_BIST', etc.
│    name                VARCHAR(100) │
│    name_tr             VARCHAR(100) │
│    description         VARCHAR(500) │
│    primary_currency    VARCHAR(12)  │
│    default_price_precision  INT    │
│    default_quantity_precision INT  │
│    supports_24_7_trading BOOLEAN   │
│    supports_fractional BOOLEAN     │
│    min_trade_amount    DECIMAL(18,8)│
│    regulatory_class    VARCHAR(50) │
│    is_active           BOOLEAN     │
│    display_order       INT         │
│    created_at          TIMESTAMPTZ │
│    updated_at          TIMESTAMPTZ │
└──────────────────────────────┘
              │
              │ 1:N
              ▼
┌──────────────────────────────┐
│          MARKETS             │
│──────────────────────────────│
│ PK id                  UUID  │
│ UK code                VARCHAR(20)  ← 'BINANCE', 'BIST', 'NASDAQ'
│ FK asset_class_id      UUID  │──────┐
│    name                VARCHAR(200) │
│    name_tr             VARCHAR(200) │
│    description         VARCHAR(1000)│
│    country_code        VARCHAR(10)  │
│    timezone            VARCHAR(50)  │
│    primary_currency    VARCHAR(12)  │
│    market_maker        VARCHAR(50)  │
│    api_base_url        VARCHAR(200) │
│    websocket_url       VARCHAR(200) │ ← WebSocket endpoint
│    default_commission_rate DECIMAL(10,6)│
│    min_commission      DECIMAL(18,8)│
│    market_config       JSONB        │
│    status              VARCHAR(20)  │ ← 'OPEN', 'CLOSED', etc.
│    status_updated_at   TIMESTAMPTZ  │
│    is_active           BOOLEAN      │
│    has_realtime_data   BOOLEAN      │
│    data_delay_minutes  INT          │
│    display_order       INT          │
│    created_at          TIMESTAMPTZ  │
│    updated_at          TIMESTAMPTZ  │
└──────────────────────────────┘
         │                │
         │ 1:N            │ 1:N
         ▼                ▼
┌──────────────────┐  ┌──────────────────────────────┐
│ TRADING_SESSIONS │  │      DATA_PROVIDERS          │
│──────────────────│  │──────────────────────────────│
│ PK id      UUID  │  │ PK id                  UUID  │
│ FK market_id UUID│  │ UK code                VARCHAR(50) ← 'BINANCE_WS'
│ session_name     │  │ FK market_id           UUID  │──────┐
│ session_type     │  │    name                VARCHAR(200) │
│ day_of_week      │  │    description         VARCHAR(1000)│
│ start_time       │  │    provider_type       VARCHAR(20)  │ ← 'REALTIME'
│ end_time         │  │    feed_type           VARCHAR(20)  │ ← 'WEBSOCKET'
│ is_primary       │  │    endpoint_url        VARCHAR(500) │
│ ...              │  │    websocket_url       VARCHAR(500) │ ← WS endpoint
└──────────────────┘  │    backup_endpoint_url VARCHAR(500) │
                      │    auth_type           VARCHAR(20)  │
                      │    api_key             VARCHAR(500) │ ← encrypted
                      │    api_secret          VARCHAR(500) │ ← encrypted
                      │    auth_config         JSONB        │
                      │    rate_limit_per_minute INT        │
                      │    timeout_seconds     INT          │
                      │    max_retries         INT          │
                      │    retry_delay_ms      INT          │
                      │    data_delay_minutes  INT          │
                      │    supported_data_types JSONB       │
                      │    provider_config     JSONB        │
                      │    connection_status   VARCHAR(20)  │ ← 'CONNECTED'
                      │    last_connected_at   TIMESTAMPTZ  │
                      │    last_error          VARCHAR(1000)│
                      │    error_count_hourly  INT          │
                      │    is_active           BOOLEAN      │
                      │    is_primary          BOOLEAN      │
                      │    priority            INT          │
                      │    cost_per_1k_calls   DECIMAL(10,6)│
                      │    monthly_limit       INT          │
                      │    monthly_usage       INT          │
                      │    created_at          TIMESTAMPTZ  │
                      │    updated_at          TIMESTAMPTZ  │
                      └──────────────────────────────┘
                                    │
                                    │ 1:N
                                    ▼
          ┌─────────────────────────────────────────────────┐
          │                   SYMBOLS                       │
          │─────────────────────────────────────────────────│
          │ PK id                      UUID                 │
          │ UK (ticker, venue)         VARCHAR(50)          │
          │    ticker                  VARCHAR(50)  ← 'BTCUSDT'
          │    venue                   VARCHAR(50)  ← 'BINANCE' (legacy)
          │    asset_class             VARCHAR(20)  ← 'CRYPTO' (legacy)
          │ FK asset_class_id          UUID         │──────┐
          │ FK market_id               UUID         │──────┤
          │ FK data_provider_id        UUID [NEW]   │◄─────┘
          │    base_currency           VARCHAR(12)  │
          │    quote_currency          VARCHAR(12)  │
          │    full_name               VARCHAR(200) │
          │    full_name_tr            VARCHAR(200) │
          │    display                 VARCHAR(100) │
          │    description             VARCHAR(500) │
          │    sector                  VARCHAR(100) │
          │    industry                VARCHAR(100) │
          │    country                 VARCHAR(10)  │
          │    isin                    VARCHAR(20)  │
          │    is_active               BOOLEAN      │
          │    is_tracked              BOOLEAN      │
          │    is_popular              BOOLEAN      │
          │ ★  is_default_symbol       BOOLEAN [NEW]│ ← System default
          │ ★  broadcast_priority      INT [NEW]    │ ← 0-100 priority
          │ ★  last_broadcast_at       TIMESTAMPTZ [NEW]│
          │    price_precision         INT          │
          │    quantity_precision      INT          │
          │    tick_size               DECIMAL(38,18)│
          │    step_size               DECIMAL(38,18)│
          │    min_order_value         DECIMAL(18,8)│
          │    max_order_value         DECIMAL(18,8)│
          │    volume_24h              DECIMAL(38,18)│
          │    market_cap              DECIMAL(38,18)│
          │    current_price           DECIMAL(18,8)│
          │    price_change_24h        DECIMAL(10,4)│
          │    price_updated_at        TIMESTAMPTZ  │
          │    metadata                JSONB        │
          │    trading_config          JSONB        │
          │    display_order           INT          │
          │    created_at              TIMESTAMPTZ  │
          │    updated_at              TIMESTAMPTZ  │
          └─────────────────────────────────────────────────┘
                        │                          │
                        │                          │
          ┌─────────────┴─────┐         ┌──────────┴─────────┐
          │ 1:N               │         │ 1:N                │
          ▼                   ▼         ▼                    ▼
┌───────────────────┐  ┌──────────────────────────┐  ┌────────────────┐
│ BACKTEST_RESULTS  │  │ USER_DASHBOARD_PREFS     │  │ MARKET_DATA    │
│───────────────────│  │──────────────────────────│  │────────────────│
│ PK id       UUID  │  │ PK id              UUID  │  │ PK id    UUID  │
│ FK symbol_id UUID │  │ FK user_id         UUID  │  │ symbol VARCHAR │
│ FK user_id  UUID  │  │ FK symbol_id       UUID  │  │ timeframe      │
│ ...               │  │ UK (user_id, symbol_id)  │  │ timestamp      │
└───────────────────┘  │    display_order    INT  │  │ open           │
                       │    is_visible      BOOLEAN│  │ high           │
┌───────────────────┐  │    is_pinned       BOOLEAN│  │ low            │
│ TRADE_HISTORY     │  │    custom_alias VARCHAR  │  │ close          │
│───────────────────│  │    notes        VARCHAR  │  │ volume         │
│ PK id       UUID  │  │    widget_type  VARCHAR  │  │ ...            │
│ FK symbol_id UUID │  │    widget_config JSONB   │  └────────────────┘
│ FK user_id  UUID  │  │    category     VARCHAR  │
│ ...               │  │    created_at   TIMESTAMPTZ│
└───────────────────┘  │    updated_at   TIMESTAMPTZ│
                       └──────────────────────────┘
                                  │
                                  │ N:1
                                  ▼
                       ┌──────────────────────────┐
                       │         USERS            │
                       │──────────────────────────│
                       │ PK id              UUID  │
                       │ UK email       VARCHAR   │
                       │    password_hash VARCHAR │
                       │    first_name   VARCHAR  │
                       │    last_name    VARCHAR  │
                       │    phone        VARCHAR  │
                       │    telegram_id  VARCHAR  │
                       │    is_active    BOOLEAN  │
                       │    is_email_verified BOOLEAN│
                       │    last_login   TIMESTAMPTZ│
                       │    preferences  JSONB    │
                       │    plan         VARCHAR  │
                       │    created_at   TIMESTAMPTZ│
                       │    updated_at   TIMESTAMPTZ│
                       └──────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                         LEGEND                                    │
├──────────────────────────────────────────────────────────────────┤
│ PK = Primary Key                                                 │
│ FK = Foreign Key                                                 │
│ UK = Unique Constraint                                           │
│ ★  = New column added by migration                               │
│ [NEW] = New in this migration                                    │
│ 1:N = One-to-Many relationship                                   │
│ N:1 = Many-to-One relationship                                   │
│ ◄─ = Foreign key reference direction                             │
│ JSONB = PostgreSQL JSON Binary type                              │
│ TIMESTAMPTZ = Timestamp with timezone                            │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                    KEY RELATIONSHIPS                              │
├──────────────────────────────────────────────────────────────────┤
│ 1. AssetClasses (1) ──< (N) Markets                              │
│    - Each asset class can have multiple markets                  │
│                                                                  │
│ 2. Markets (1) ──< (N) DataProviders                             │
│    - Each market can have multiple data providers               │
│    - One provider marked as is_primary                           │
│                                                                  │
│ 3. Symbols (N) ──> (1) Market                                    │
│    - Each symbol belongs to one market                           │
│                                                                  │
│ 4. Symbols (N) ──> (1) DataProvider [NEW]                        │
│    - Each symbol can have explicit provider assignment          │
│    - NULL = use market's primary provider                        │
│                                                                  │
│ 5. Users (1) ──< (N) UserDashboardPreferences                    │
│    - Each user can customize multiple symbols                   │
│                                                                  │
│ 6. Symbols (1) ──< (N) UserDashboardPreferences                  │
│    - Each symbol can be tracked by multiple users               │
│                                                                  │
│ 7. Default Symbols (is_default_symbol = TRUE)                    │
│    - System-wide defaults for new/anonymous users               │
│    - Minimum 9 symbols enforced by trigger                      │
└──────────────────────────────────────────────────────────────────┘
```

---

## Data Flow Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        DATA FLOW DIAGRAM                             │
│              Symbol Management & Price Broadcasting                  │
└─────────────────────────────────────────────────────────────────────┘

                          ┌──────────────────┐
                          │   BINANCE API    │
                          │  (External WS)   │
                          └────────┬─────────┘
                                   │
                                   │ Real-time prices
                                   ▼
                    ┌──────────────────────────────┐
                    │  BinanceWebSocketService     │
                    │  - Subscribes to symbols     │
                    │  - Receives price updates    │
                    └──────────────┬───────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │  Query broadcast list from DB│
                    │  WHERE is_active=TRUE AND    │
                    │  is_tracked=TRUE             │
                    │  ORDER BY broadcast_priority │
                    └──────────────┬───────────────┘
                                   │
                                   ▼
              ┌────────────────────────────────────────┐
              │         SYMBOLS TABLE                  │
              │  - id, ticker, broadcast_priority      │
              │  - data_provider_id, market_id         │
              │  - is_default_symbol, is_active        │
              └────────────────┬───────────────────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        │ Anonymous Users      │ Authenticated Users  │
        ▼                      ▼                      ▼
┌───────────────────┐  ┌──────────────────────────────────┐
│ Query:            │  │ Query:                           │
│ SELECT * FROM     │  │ SELECT s.*, udp.*                │
│ symbols WHERE     │  │ FROM symbols s                   │
│ is_default_symbol │  │ JOIN user_dashboard_preferences  │
│ = TRUE            │  │ WHERE udp.user_id = $1           │
└─────────┬─────────┘  └──────────────┬───────────────────┘
          │                           │
          │ 9 default symbols         │ User-specific symbols
          ▼                           ▼
    ┌──────────────────────────────────────────┐
    │   MultiAssetDataBroadcastService         │
    │   - Aggregates price updates             │
    │   - Broadcasts via SignalR/WebSocket     │
    │   - Respects broadcast_priority          │
    └──────────────────┬───────────────────────┘
                       │
        ┌──────────────┴──────────────┐
        │                             │
        ▼                             ▼
┌──────────────┐              ┌──────────────┐
│ Mobile App   │              │  Web App     │
│ Dashboard    │              │  Dashboard   │
└──────────────┘              └──────────────┘
```

---

## Implementation Priority Matrix

```
┌──────────────────────────────────────────────────────────────────────┐
│                   IMPLEMENTATION PHASES                               │
│                Priority-Based Rollout Plan                            │
└──────────────────────────────────────────────────────────────────────┘

PHASE 1: DATABASE MIGRATION (Week 1)
┌───────────────────────────────────────────────────────────────┐
│ Priority: CRITICAL | Risk: LOW | Effort: 2 days                │
├───────────────────────────────────────────────────────────────┤
│ ✓ Execute 20250108_DataDrivenSymbols.sql                      │
│ ✓ Execute 20250108_DefaultDataPopulation.sql                  │
│ ✓ Run 20250108_DataQualityValidation.sql                      │
│ ✓ Verify all indexes created                                  │
│ ✓ Test rollback script on staging                             │
│ ✓ Backup production database                                  │
│ ✓ Deploy to production (zero downtime)                        │
└───────────────────────────────────────────────────────────────┘

PHASE 2: BACKEND SERVICE UPDATES (Week 1-2)
┌───────────────────────────────────────────────────────────────┐
│ Priority: HIGH | Risk: MEDIUM | Effort: 3-4 days              │
├───────────────────────────────────────────────────────────────┤
│ □ Update BinanceWebSocketService.cs                           │
│   - Remove hard-coded symbol array                            │
│   - Query symbols from database                               │
│   - Use broadcast_priority for scheduling                     │
│                                                                │
│ □ Update MultiAssetDataBroadcastService.cs                    │
│   - Query broadcast list dynamically                          │
│   - Update last_broadcast_at after each broadcast             │
│                                                                │
│ □ Create SymbolManagementService.cs                           │
│   - CRUD operations for symbols                               │
│   - Validation logic                                          │
│   - Broadcast list generation                                 │
│                                                                │
│ □ Update DashboardController.cs                               │
│   - GetDefaultSymbols() for anonymous users                   │
│   - GetUserSymbols(userId) for authenticated users            │
│                                                                │
│ □ Update existing controllers to remove hard-coded lists      │
└───────────────────────────────────────────────────────────────┘

PHASE 3: API ENDPOINTS (Week 2)
┌───────────────────────────────────────────────────────────────┐
│ Priority: HIGH | Risk: LOW | Effort: 2 days                   │
├───────────────────────────────────────────────────────────────┤
│ □ GET  /api/symbols/defaults                                  │
│   - Returns 9 default symbols for anonymous users             │
│                                                                │
│ □ GET  /api/users/{userId}/symbols                            │
│   - Returns user's customized symbol preferences              │
│                                                                │
│ □ POST /api/users/{userId}/symbols                            │
│   - Add symbol to user's dashboard                            │
│                                                                │
│ □ PUT  /api/users/{userId}/symbols/{symbolId}                 │
│   - Update user preference (order, pinned, etc.)              │
│                                                                │
│ □ DELETE /api/users/{userId}/symbols/{symbolId}               │
│   - Remove symbol from user's dashboard                       │
│                                                                │
│ □ GET  /api/symbols/search?q={query}                          │
│   - Search symbols for adding to dashboard                    │
│                                                                │
│ □ GET  /api/admin/symbols                                     │
│   - Admin symbol management                                   │
└───────────────────────────────────────────────────────────────┘

PHASE 4: FRONTEND INTEGRATION (Week 2-3)
┌───────────────────────────────────────────────────────────────┐
│ Priority: MEDIUM | Risk: MEDIUM | Effort: 4-5 days            │
├───────────────────────────────────────────────────────────────┤
│ MOBILE APP:                                                    │
│ □ Remove hard-coded SYMBOLS array from config.ts              │
│ □ Implement API calls to /api/symbols/defaults                │
│ □ Add user preference management UI                           │
│ □ Implement symbol search and add functionality               │
│                                                                │
│ WEB APP:                                                       │
│ □ Remove hard-coded symbol lists                              │
│ □ Implement dynamic symbol loading                            │
│ □ Add preference management UI                                │
│ □ Implement drag-and-drop reordering                          │
└───────────────────────────────────────────────────────────────┘

PHASE 5: TESTING & VALIDATION (Week 3)
┌───────────────────────────────────────────────────────────────┐
│ Priority: CRITICAL | Risk: LOW | Effort: 3 days               │
├───────────────────────────────────────────────────────────────┤
│ □ Unit tests for SymbolManagementService                      │
│ □ Integration tests for API endpoints                         │
│ □ E2E tests for frontend symbol loading                       │
│ □ Load testing (1000+ concurrent users)                       │
│ □ Performance validation (< 5ms query times)                  │
│ □ User acceptance testing                                     │
└───────────────────────────────────────────────────────────────┘

PHASE 6: MONITORING & OPTIMIZATION (Ongoing)
┌───────────────────────────────────────────────────────────────┐
│ Priority: MEDIUM | Risk: LOW | Effort: Ongoing                │
├───────────────────────────────────────────────────────────────┤
│ □ Setup query performance monitoring                          │
│ □ Configure slow query alerts (> 10ms)                        │
│ □ Monitor index hit ratio                                     │
│ □ Track user preference adoption                              │
│ □ Review and update default symbols monthly                   │
└───────────────────────────────────────────────────────────────┘
```

---

## Service Layer Implementation Examples

### Example 1: SymbolManagementService.cs

```csharp
using MyTrader.Core.Models;
using MyTrader.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace MyTrader.Services.Symbols;

public interface ISymbolManagementService
{
    Task<List<Symbol>> GetDefaultSymbolsAsync();
    Task<List<Symbol>> GetUserSymbolsAsync(Guid userId);
    Task<List<Symbol>> GetBroadcastListAsync();
    Task<Symbol?> GetSymbolByIdAsync(Guid id);
    Task<List<Symbol>> SearchSymbolsAsync(string query, Guid? assetClassId = null);
}

public class SymbolManagementService : ISymbolManagementService
{
    private readonly ITradingDbContext _context;
    private readonly ILogger<SymbolManagementService> _logger;

    public SymbolManagementService(
        ITradingDbContext context,
        ILogger<SymbolManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get default symbols for anonymous/new users
    /// Uses idx_symbols_defaults index
    /// Expected execution time: < 1ms
    /// </summary>
    public async Task<List<Symbol>> GetDefaultSymbolsAsync()
    {
        return await _context.Symbols
            .Where(s => s.is_default_symbol == true && s.is_active == true)
            .OrderBy(s => s.display_order)
            .Select(s => new Symbol
            {
                Id = s.Id,
                Ticker = s.Ticker,
                Display = s.Display,
                BaseCurrency = s.BaseCurrency,
                QuoteCurrency = s.QuoteCurrency,
                CurrentPrice = s.CurrentPrice,
                PriceChange24h = s.PriceChange24h,
                PriceUpdatedAt = s.PriceUpdatedAt,
                DisplayOrder = s.DisplayOrder
            })
            .ToListAsync();
    }

    /// <summary>
    /// Get user-specific symbols with preferences
    /// Uses idx_user_prefs_visible index
    /// Expected execution time: < 2ms
    /// </summary>
    public async Task<List<Symbol>> GetUserSymbolsAsync(Guid userId)
    {
        var userSymbols = await _context.Symbols
            .Include(s => s.Market)
            .Join(
                _context.UserDashboardPreferences,
                s => s.Id,
                udp => udp.SymbolId,
                (s, udp) => new { Symbol = s, Preference = udp }
            )
            .Where(x =>
                x.Preference.UserId == userId &&
                x.Preference.IsVisible == true &&
                x.Symbol.IsActive == true)
            .OrderByDescending(x => x.Preference.IsPinned)
            .ThenBy(x => x.Preference.DisplayOrder)
            .Select(x => x.Symbol)
            .ToListAsync();

        // Fallback to defaults if user has no preferences
        if (userSymbols.Count == 0)
        {
            _logger.LogInformation("User {UserId} has no symbol preferences, returning defaults", userId);
            return await GetDefaultSymbolsAsync();
        }

        return userSymbols;
    }

    /// <summary>
    /// Get broadcast list for WebSocket service
    /// Uses idx_symbols_broadcast_active index
    /// Expected execution time: < 3ms
    /// </summary>
    public async Task<List<Symbol>> GetBroadcastListAsync()
    {
        return await _context.Symbols
            .Include(s => s.Market)
            .Include(s => s.DataProvider)
            .Where(s =>
                s.IsActive == true &&
                s.IsTracked == true &&
                s.Market != null &&
                s.Market.IsActive == true)
            .OrderByDescending(s => s.BroadcastPriority)
            .ThenBy(s => s.LastBroadcastAt ?? DateTime.MinValue)
            .Take(100)
            .ToListAsync();
    }

    public async Task<Symbol?> GetSymbolByIdAsync(Guid id)
    {
        return await _context.Symbols
            .Include(s => s.Market)
            .Include(s => s.AssetClassEntity)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Symbol>> SearchSymbolsAsync(
        string query,
        Guid? assetClassId = null)
    {
        var searchQuery = _context.Symbols
            .Where(s => s.IsActive == true);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchTerm = $"%{query}%";
            searchQuery = searchQuery.Where(s =>
                EF.Functions.ILike(s.Ticker, searchTerm) ||
                EF.Functions.ILike(s.Display, searchTerm) ||
                EF.Functions.ILike(s.FullName, searchTerm));
        }

        if (assetClassId.HasValue)
        {
            searchQuery = searchQuery.Where(s => s.AssetClassId == assetClassId);
        }

        return await searchQuery
            .OrderByDescending(s => s.IsPopular)
            .ThenByDescending(s => s.Volume24h)
            .Take(50)
            .ToListAsync();
    }
}
```

### Example 2: Updated BinanceWebSocketService.cs

```csharp
// BEFORE: Hard-coded symbols
private readonly string[] SYMBOLS = new[]
{
    "BTCUSDT", "ETHUSDT", "XRPUSDT", "SOLUSDT",
    "AVAXUSDT", "SUIUSDT", "ENAUSDT", "UNIUSDT", "BNBUSDT"
};

// AFTER: Database-driven symbols
private readonly ISymbolManagementService _symbolService;

public async Task ConnectAsync()
{
    // Get broadcast list from database
    var symbols = await _symbolService.GetBroadcastListAsync();

    var streams = symbols
        .Select(s => $"{s.Ticker.ToLower()}@ticker")
        .ToList();

    var streamString = string.Join("/", streams);
    var wsUrl = $"wss://stream.binance.com:9443/stream?streams={streamString}";

    await _webSocket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
    _logger.LogInformation("Connected to Binance WebSocket with {Count} symbols", symbols.Count);
}
```

---

## Quick Reference: Migration Execution

### Step-by-Step Deployment

```bash
# 1. Backup production database
pg_dump -h localhost -U mytrader_user -d mytrader_db > backup_$(date +%Y%m%d_%H%M%S).sql

# 2. Test on staging first
psql -h staging-db -U mytrader_user -d mytrader_db -f Migrations/20250108_DataDrivenSymbols.sql

# 3. Run validation
psql -h staging-db -U mytrader_user -d mytrader_db -f Migrations/20250108_DataQualityValidation.sql

# 4. Verify default symbols
psql -h staging-db -U mytrader_user -d mytrader_db -c "SELECT * FROM v_symbol_data_quality;"

# 5. If all checks pass, deploy to production
psql -h prod-db -U mytrader_user -d mytrader_db -f Migrations/20250108_DataDrivenSymbols.sql

# 6. Run validation on production
psql -h prod-db -U mytrader_user -d mytrader_db -f Migrations/20250108_DataQualityValidation.sql

# 7. If issues occur, rollback immediately
psql -h prod-db -U mytrader_user -d mytrader_db -f Migrations/20250108_DataDrivenSymbols_ROLLBACK.sql
```

---

**End of Implementation Guide**
