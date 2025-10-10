# Data-Driven Symbol Management Architecture

**Document Version:** 1.0
**Date:** 2025-01-08
**Author:** Data Architecture Manager
**Status:** Design Complete - Ready for Implementation

---

## Executive Summary

This document outlines the comprehensive data architecture for transitioning myTrader from hard-coded symbol lists to a fully data-driven symbol management system. The design leverages existing tables while adding critical enhancements to support user preferences, broadcast prioritization, and multi-market symbol management.

### Key Objectives

1. **Eliminate Hard-Coded Symbols**: All symbol lists moved to database with full configurability
2. **User Preference Support**: Individual users can customize their dashboard symbols
3. **System Defaults**: Anonymous/new users see predefined default symbols
4. **Broadcast Control**: Data-driven broadcast lists based on database configuration
5. **Market Relationships**: Proper symbol-to-market-to-data-provider linkage
6. **Performance Optimized**: Read-heavy workload optimization with strategic indexes

---

## Current State Analysis

### Existing Tables (Already Implemented)

#### 1. **Symbols Table**
**Status:** EXISTS - Needs Enhancement
**Location:** `MyTrader.Core.Models.Symbol.cs`

**Current Structure:**
```sql
CREATE TABLE symbols (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticker VARCHAR(50) NOT NULL,
    venue VARCHAR(50),
    asset_class VARCHAR(20) DEFAULT 'CRYPTO',
    asset_class_id UUID REFERENCES asset_classes(id),
    market_id UUID REFERENCES markets(id),
    base_currency VARCHAR(12),
    quote_currency VARCHAR(12),
    full_name VARCHAR(200),
    full_name_tr VARCHAR(200),
    display VARCHAR(100),
    description VARCHAR(500),
    sector VARCHAR(100),
    industry VARCHAR(100),
    country VARCHAR(10),
    isin VARCHAR(20),
    is_active BOOLEAN DEFAULT TRUE,
    is_tracked BOOLEAN DEFAULT FALSE,
    is_popular BOOLEAN DEFAULT FALSE,
    price_precision INT,
    quantity_precision INT,
    tick_size DECIMAL(38,18),
    step_size DECIMAL(38,18),
    min_order_value DECIMAL(18,8),
    max_order_value DECIMAL(18,8),
    volume_24h DECIMAL(38,18),
    market_cap DECIMAL(38,18),
    current_price DECIMAL(18,8),
    price_change_24h DECIMAL(10,4),
    price_updated_at TIMESTAMPTZ,
    metadata JSONB,
    trading_config JSONB,
    display_order INT DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);
```

**Columns to Add:**
- `broadcast_priority` INT DEFAULT 0 - Priority for WebSocket broadcast
- `last_broadcast_at` TIMESTAMPTZ - Last time symbol was broadcast
- `data_provider_id` UUID - Primary data provider for this symbol
- `is_default_symbol` BOOLEAN DEFAULT FALSE - System default for new users

#### 2. **Markets Table**
**Status:** EXISTS
**Location:** `MyTrader.Core.Models.Market.cs`

Fully implemented with proper relationships to AssetClass and DataProvider.

#### 3. **DataProviders Table**
**Status:** EXISTS
**Location:** `MyTrader.Core.Models.DataProvider.cs`

Fully implemented with connection status tracking and priority management.

#### 4. **AssetClasses Table**
**Status:** EXISTS
**Location:** `MyTrader.Core.Models.AssetClass.cs`

Fully implemented with support for CRYPTO, STOCK_BIST, STOCK_NASDAQ, etc.

#### 5. **UserDashboardPreferences Table**
**Status:** EXISTS - Perfect for Our Needs!
**Location:** `MyTrader.Core.Models.UserDashboardPreferences.cs`

**Current Structure:**
```sql
CREATE TABLE user_dashboard_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    symbol_id UUID NOT NULL REFERENCES symbols(id) ON DELETE CASCADE,
    display_order INT DEFAULT 0,
    is_visible BOOLEAN DEFAULT TRUE,
    is_pinned BOOLEAN DEFAULT FALSE,
    custom_alias VARCHAR(100),
    notes VARCHAR(500),
    widget_type VARCHAR(50) DEFAULT 'card',
    widget_config JSONB,
    category VARCHAR(50),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, symbol_id)
);
```

**Analysis:** This table is PERFECT for user-specific symbol preferences! No changes needed to structure.

---

## Architecture Gaps & Solutions

### Problem 1: No Default Symbol Configuration for Anonymous Users

**Current Issue:**
- Hard-coded symbol lists in broadcast services
- No database-driven defaults for new/anonymous users

**Solution:**
- Add `is_default_symbol` flag to Symbols table
- Mark BTC, ETH, XRP, SOL, AVAX, SUI, ENA, UNI, BNB as defaults
- Services query `WHERE is_default_symbol = TRUE` for anonymous users

### Problem 2: Old Symbols Still in Database but Not Broadcast

**Current Issue:**
- ADA, MATIC, DOT, LINK, LTC exist in DB but aren't broadcast
- No clear deactivation mechanism

**Solution:**
- Update `is_active = FALSE` for deprecated symbols
- Update `is_tracked = FALSE` to stop data collection
- Services filter `WHERE is_active = TRUE AND is_tracked = TRUE`

### Problem 3: Symbol-Market-DataProvider Relationship Incomplete

**Current Issue:**
- Symbols have `market_id` but no direct `data_provider_id`
- Difficult to determine which provider feeds which symbol

**Solution:**
- Add `data_provider_id` to Symbols table
- Allows explicit provider assignment per symbol
- Fallback to Market's primary DataProvider if NULL

### Problem 4: No Broadcast Priority Management

**Current Issue:**
- All symbols broadcast equally
- No mechanism to prioritize high-demand symbols

**Solution:**
- Add `broadcast_priority` INT to Symbols table
- Higher priority = more frequent updates
- Add `last_broadcast_at` for broadcast scheduling

---

## Enhanced Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                     DATA-DRIVEN SYMBOL ARCHITECTURE                   │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│   AssetClasses   │
│──────────────────│       ┌──────────────────┐
│ id (PK)          │──────<│     Markets      │
│ code (UNIQUE)    │       │──────────────────│       ┌──────────────────┐
│ name             │       │ id (PK)          │──────<│  DataProviders   │
│ name_tr          │       │ code (UNIQUE)    │       │──────────────────│
│ description      │       │ name             │       │ id (PK)          │
│ primary_currency │       │ asset_class_id   │       │ code (UNIQUE)    │
│ is_active        │       │ country_code     │       │ market_id (FK)   │
│ display_order    │       │ timezone         │       │ provider_type    │
└──────────────────┘       │ api_base_url     │       │ feed_type        │
                           │ websocket_url    │       │ endpoint_url     │
                           │ status           │       │ websocket_url    │
                           │ is_active        │       │ is_active        │
                           │ display_order    │       │ is_primary       │
                           └──────────────────┘       │ priority         │
                                    │                 └──────────────────┘
                                    │                          │
                                    ▼                          │
                           ┌──────────────────┐               │
                           │     Symbols      │◄──────────────┘
                           │──────────────────│
                           │ id (PK)          │
                           │ ticker           │
                           │ asset_class_id   │◄──────────┐
                           │ market_id (FK)   │           │
                           │ data_provider_id │◄──────────┤
                           │ base_currency    │           │
                           │ quote_currency   │           │
                           │ full_name        │           │
                           │ is_active        │           │
                           │ is_tracked       │           │
                           │ is_popular       │           │
                           │ is_default_symbol│ ← NEW     │
                           │ broadcast_priority│ ← NEW    │
                           │ last_broadcast_at│ ← NEW     │
                           │ current_price    │           │
                           │ display_order    │           │
                           └──────────────────┘           │
                                    │                     │
                                    │                     │
                   ┌────────────────┴────────────┐       │
                   │                             │       │
                   ▼                             ▼       │
    ┌───────────────────────────┐   ┌──────────────────┐│
    │ UserDashboardPreferences  │   │      Users       ││
    │───────────────────────────│   │──────────────────││
    │ id (PK)                   │   │ id (PK)          ││
    │ user_id (FK) ─────────────┼──>│ email (UNIQUE)   ││
    │ symbol_id (FK) ───────────┼───┘ first_name       ││
    │ display_order             │     │ last_name        ││
    │ is_visible                │     │ is_active        ││
    │ is_pinned                 │     │ plan             ││
    │ custom_alias              │     └──────────────────┘
    │ widget_type               │
    │ category                  │
    │ UNIQUE(user_id, symbol_id)│
    └───────────────────────────┘

LEGEND:
  ──────> : One-to-Many Relationship
  ──────< : Many-to-One Relationship
  (PK)    : Primary Key
  (FK)    : Foreign Key
  (UNIQUE): Unique Constraint
  ← NEW   : New column to be added
```

---

## Database Schema Enhancements

### Enhancement 1: Symbols Table Additions

```sql
-- Add new columns to symbols table
ALTER TABLE symbols
ADD COLUMN broadcast_priority INT DEFAULT 0,
ADD COLUMN last_broadcast_at TIMESTAMPTZ,
ADD COLUMN data_provider_id UUID REFERENCES data_providers(id),
ADD COLUMN is_default_symbol BOOLEAN DEFAULT FALSE;

-- Add comments for documentation
COMMENT ON COLUMN symbols.broadcast_priority IS 'Higher values = more frequent broadcasts (0-100 scale)';
COMMENT ON COLUMN symbols.last_broadcast_at IS 'Last time this symbol was broadcast via WebSocket';
COMMENT ON COLUMN symbols.data_provider_id IS 'Primary data provider for this symbol (NULL = use market default)';
COMMENT ON COLUMN symbols.is_default_symbol IS 'System default symbol shown to new/anonymous users';
```

### Enhancement 2: Strategic Indexes

```sql
-- Index for broadcast queries (most critical for performance)
CREATE INDEX idx_symbols_broadcast_active
ON symbols(is_active, is_tracked, broadcast_priority DESC, last_broadcast_at)
WHERE is_active = TRUE AND is_tracked = TRUE;

-- Index for default symbol queries (anonymous users)
CREATE INDEX idx_symbols_defaults
ON symbols(is_default_symbol, display_order)
WHERE is_default_symbol = TRUE AND is_active = TRUE;

-- Index for market-provider relationships
CREATE INDEX idx_symbols_market_provider
ON symbols(market_id, data_provider_id, is_active)
WHERE is_active = TRUE;

-- Index for user preference joins
CREATE INDEX idx_user_prefs_visible
ON user_dashboard_preferences(user_id, is_visible, display_order)
WHERE is_visible = TRUE;

-- Composite index for asset class filtering
CREATE INDEX idx_symbols_asset_class_active
ON symbols(asset_class_id, market_id, is_active, is_popular)
WHERE is_active = TRUE;
```

### Enhancement 3: Database Constraints

```sql
-- Ensure broadcast priority is within valid range
ALTER TABLE symbols
ADD CONSTRAINT chk_broadcast_priority
CHECK (broadcast_priority >= 0 AND broadcast_priority <= 100);

-- Ensure at least one active default symbol exists (enforced via trigger)
CREATE OR REPLACE FUNCTION ensure_default_symbols()
RETURNS TRIGGER AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM symbols
        WHERE is_default_symbol = TRUE
        AND is_active = TRUE
    ) THEN
        RAISE EXCEPTION 'At least one active default symbol must exist';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_ensure_default_symbols
AFTER UPDATE OR DELETE ON symbols
FOR EACH STATEMENT
EXECUTE FUNCTION ensure_default_symbols();
```

---

## Default Data Configuration

### Default Symbols (9 Primary Crypto Assets)

| Ticker    | Display Name       | Market  | Provider        | Priority | Default | Active | Tracked |
|-----------|-------------------|---------|-----------------|----------|---------|--------|---------|
| BTCUSDT   | Bitcoin           | BINANCE | BINANCE_WS      | 100      | TRUE    | TRUE   | TRUE    |
| ETHUSDT   | Ethereum          | BINANCE | BINANCE_WS      | 95       | TRUE    | TRUE   | TRUE    |
| XRPUSDT   | Ripple            | BINANCE | BINANCE_WS      | 90       | TRUE    | TRUE   | TRUE    |
| SOLUSDT   | Solana            | BINANCE | BINANCE_WS      | 85       | TRUE    | TRUE   | TRUE    |
| AVAXUSDT  | Avalanche         | BINANCE | BINANCE_WS      | 80       | TRUE    | TRUE   | TRUE    |
| SUIUSDT   | Sui               | BINANCE | BINANCE_WS      | 75       | TRUE    | TRUE   | TRUE    |
| ENAUSDT   | Ethena            | BINANCE | BINANCE_WS      | 70       | TRUE    | TRUE   | TRUE    |
| UNIUSDT   | Uniswap           | BINANCE | BINANCE_WS      | 65       | TRUE    | TRUE   | TRUE    |
| BNBUSDT   | Binance Coin      | BINANCE | BINANCE_WS      | 60       | TRUE    | TRUE   | TRUE    |

### Deprecated Symbols (To Be Deactivated)

| Ticker    | Display Name       | Action Required                                     |
|-----------|-------------------|-----------------------------------------------------|
| ADAUSDT   | Cardano           | SET is_active=FALSE, is_tracked=FALSE, is_default_symbol=FALSE |
| MATICUSDT | Polygon           | SET is_active=FALSE, is_tracked=FALSE, is_default_symbol=FALSE |
| DOTUSDT   | Polkadot          | SET is_active=FALSE, is_tracked=FALSE, is_default_symbol=FALSE |
| LINKUSDT  | Chainlink         | SET is_active=FALSE, is_tracked=FALSE, is_default_symbol=FALSE |
| LTCUSDT   | Litecoin          | SET is_active=FALSE, is_tracked=FALSE, is_default_symbol=FALSE |

---

## Migration Strategy

### Migration File: `20250108_DataDrivenSymbols.sql`

**Execution Order:**
1. Add new columns to symbols table (broadcast_priority, last_broadcast_at, data_provider_id, is_default_symbol)
2. Create new indexes for performance
3. Add constraints and triggers
4. Update existing symbols with default configuration
5. Mark deprecated symbols as inactive
6. Link symbols to data providers
7. Verify data integrity

**Estimated Execution Time:** < 5 seconds (assuming < 10,000 symbols)

**Downtime Required:** ZERO (non-blocking operations)

---

## Performance Optimization

### Read-Heavy Workload Optimization

#### Query 1: Get Default Symbols for Anonymous Users
```sql
-- BEFORE: Hard-coded in application
const symbols = ['BTCUSDT', 'ETHUSDT', ...];

-- AFTER: Database query with index support
SELECT id, ticker, display, current_price, price_change_24h
FROM symbols
WHERE is_default_symbol = TRUE
  AND is_active = TRUE
ORDER BY display_order;

-- Index Used: idx_symbols_defaults
-- Expected Rows: 9
-- Execution Time: < 1ms
```

#### Query 2: Get User-Specific Symbols
```sql
-- Get symbols for authenticated user with preferences
SELECT
    s.id,
    s.ticker,
    s.display,
    s.current_price,
    s.price_change_24h,
    udp.display_order,
    udp.is_pinned,
    udp.custom_alias,
    udp.widget_type
FROM symbols s
INNER JOIN user_dashboard_preferences udp ON s.id = udp.symbol_id
WHERE udp.user_id = $1
  AND udp.is_visible = TRUE
  AND s.is_active = TRUE
ORDER BY udp.is_pinned DESC, udp.display_order;

-- Indexes Used: idx_user_prefs_visible, symbols PK
-- Expected Rows: 5-50
-- Execution Time: < 2ms
```

#### Query 3: Get Broadcast List for WebSocket Service
```sql
-- Get symbols to broadcast (high priority first)
SELECT
    s.id,
    s.ticker,
    s.market_id,
    s.data_provider_id,
    s.broadcast_priority,
    dp.websocket_url,
    dp.connection_status
FROM symbols s
INNER JOIN markets m ON s.market_id = m.id
LEFT JOIN data_providers dp ON s.data_provider_id = dp.id OR (s.data_provider_id IS NULL AND dp.market_id = m.id AND dp.is_primary = TRUE)
WHERE s.is_active = TRUE
  AND s.is_tracked = TRUE
  AND m.is_active = TRUE
ORDER BY s.broadcast_priority DESC, s.last_broadcast_at ASC NULLS FIRST;

-- Index Used: idx_symbols_broadcast_active, idx_symbols_market_provider
-- Expected Rows: 9-100
-- Execution Time: < 3ms
```

### Index Effectiveness Analysis

| Index Name                      | Size Est. | Selectivity | Usage Pattern                    |
|--------------------------------|-----------|-------------|----------------------------------|
| idx_symbols_broadcast_active   | 50 KB     | High (90%)  | Every broadcast cycle (10/sec)   |
| idx_symbols_defaults           | 5 KB      | Very High   | Anonymous user requests          |
| idx_symbols_market_provider    | 30 KB     | Medium      | Data provider routing            |
| idx_user_prefs_visible         | 100 KB    | High        | User dashboard loads             |
| idx_symbols_asset_class_active | 40 KB     | Medium      | Asset class filtering            |

**Total Index Overhead:** ~225 KB (negligible)
**Query Performance Gain:** 10-100x faster than full table scans

---

## Data Integrity Rules

### Referential Integrity

1. **Symbol → Market**: ON DELETE SET NULL (preserve symbol history)
2. **Symbol → DataProvider**: ON DELETE SET NULL (fallback to market default)
3. **Symbol → AssetClass**: ON DELETE SET NULL (preserve symbol)
4. **UserDashboardPreferences → Symbol**: ON DELETE CASCADE (clean up preferences)
5. **UserDashboardPreferences → User**: ON DELETE CASCADE (clean up on user deletion)

### Business Logic Constraints

1. **Default Symbol Requirement**: At least 1 active default symbol must exist (enforced by trigger)
2. **Broadcast Priority Range**: 0-100 (enforced by CHECK constraint)
3. **Unique User Preferences**: (user_id, symbol_id) UNIQUE (enforced by UNIQUE constraint)
4. **Active Symbol Validation**: is_active=FALSE symbols cannot have is_default_symbol=TRUE

### Data Quality Rules

```sql
-- Data Quality Validation Query
SELECT
    'Missing Market Assignment' AS issue,
    COUNT(*) AS count
FROM symbols
WHERE is_active = TRUE
  AND market_id IS NULL

UNION ALL

SELECT
    'Default Symbol Inactive' AS issue,
    COUNT(*) AS count
FROM symbols
WHERE is_default_symbol = TRUE
  AND is_active = FALSE

UNION ALL

SELECT
    'Orphaned User Preferences' AS issue,
    COUNT(*) AS count
FROM user_dashboard_preferences udp
LEFT JOIN symbols s ON udp.symbol_id = s.id
WHERE s.id IS NULL;

-- Expected Result: All counts should be 0
```

---

## Rollback Procedures

### Rollback File: `20250108_DataDrivenSymbols_ROLLBACK.sql`

```sql
-- Step 1: Drop triggers
DROP TRIGGER IF EXISTS trg_ensure_default_symbols ON symbols;
DROP FUNCTION IF EXISTS ensure_default_symbols();

-- Step 2: Drop indexes
DROP INDEX IF EXISTS idx_symbols_broadcast_active;
DROP INDEX IF EXISTS idx_symbols_defaults;
DROP INDEX IF EXISTS idx_symbols_market_provider;
DROP INDEX IF EXISTS idx_user_prefs_visible;
DROP INDEX IF EXISTS idx_symbols_asset_class_active;

-- Step 3: Remove constraints
ALTER TABLE symbols DROP CONSTRAINT IF EXISTS chk_broadcast_priority;

-- Step 4: Remove columns
ALTER TABLE symbols
DROP COLUMN IF EXISTS broadcast_priority,
DROP COLUMN IF EXISTS last_broadcast_at,
DROP COLUMN IF EXISTS data_provider_id,
DROP COLUMN IF EXISTS is_default_symbol;

-- Rollback complete: Database restored to pre-migration state
```

**Rollback Safety:** 100% reversible with no data loss
**Rollback Time:** < 2 seconds

---

## Implementation Checklist

### Phase 1: Database Migration (Priority: CRITICAL)
- [ ] Review migration script `20250108_DataDrivenSymbols.sql`
- [ ] Test migration on development database
- [ ] Backup production database
- [ ] Execute migration on production
- [ ] Verify all indexes created successfully
- [ ] Run data quality validation queries
- [ ] Test rollback script on staging environment

### Phase 2: Service Layer Updates (Priority: HIGH)
- [ ] Update `BinanceWebSocketService` to query symbols from DB
- [ ] Update `MultiAssetDataBroadcastService` to use broadcast_priority
- [ ] Create `SymbolManagementService` for CRUD operations
- [ ] Update dashboard controllers to use UserDashboardPreferences
- [ ] Implement anonymous user default symbol logic
- [ ] Add symbol broadcast scheduling based on last_broadcast_at

### Phase 3: API Enhancements (Priority: MEDIUM)
- [ ] Create `/api/symbols/defaults` endpoint for anonymous users
- [ ] Create `/api/users/{userId}/symbols` for user preferences
- [ ] Create `/api/symbols/broadcast-list` for WebSocket services
- [ ] Add admin endpoints for symbol management
- [ ] Implement symbol search/filter API

### Phase 4: Frontend Integration (Priority: MEDIUM)
- [ ] Update mobile app to fetch symbols dynamically
- [ ] Update web frontend to fetch symbols dynamically
- [ ] Implement user preference management UI
- [ ] Add symbol search and filter UI
- [ ] Remove hard-coded symbol lists from frontend

### Phase 5: Monitoring & Optimization (Priority: LOW)
- [ ] Add logging for symbol queries
- [ ] Monitor index usage with pg_stat_user_indexes
- [ ] Track broadcast performance metrics
- [ ] Implement cache layer for default symbols (optional)
- [ ] Add alerts for orphaned preferences

---

## Success Criteria

### Functional Requirements
✓ All symbols managed in database
✓ User preferences fully supported
✓ Anonymous users see default symbols
✓ Broadcast lists driven by database
✓ Old symbols properly deactivated
✓ Market-provider relationships established

### Performance Requirements
✓ Default symbol query: < 1ms
✓ User preference query: < 2ms
✓ Broadcast list query: < 3ms
✓ Index overhead: < 500 KB
✓ Zero downtime deployment

### Data Integrity Requirements
✓ All foreign keys enforced
✓ No orphaned preferences
✓ At least 1 default symbol exists
✓ Broadcast priority within 0-100
✓ 100% rollback capability

---

## Monitoring & Maintenance

### Key Performance Indicators (KPIs)

1. **Query Performance**
   - Target: 95th percentile < 5ms
   - Alert: > 10ms for 5 consecutive minutes

2. **Data Integrity**
   - Target: 0 orphaned records
   - Check: Daily via scheduled job

3. **Symbol Coverage**
   - Target: 100% of broadcast symbols have data_provider_id
   - Check: Weekly manual review

4. **User Adoption**
   - Target: 50% of active users customize preferences within 30 days
   - Metric: Track UserDashboardPreferences growth

### Maintenance Tasks

**Daily:**
- Monitor query performance via application logs
- Check for failed broadcasts due to missing symbols

**Weekly:**
- Run data quality validation queries
- Review new symbol additions for completeness

**Monthly:**
- Analyze index usage and effectiveness
- Review broadcast priority distribution
- Clean up inactive user preferences (> 90 days inactive users)

**Quarterly:**
- Review and update default symbol list
- Evaluate addition of new asset classes/markets
- Performance tuning based on usage patterns

---

## Appendix A: SQL Scripts Location

All migration scripts are located in:
```
/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/backend/MyTrader.Infrastructure/Migrations/
```

**Primary Scripts:**
1. `20250108_DataDrivenSymbols.sql` - Main migration
2. `20250108_DataDrivenSymbols_ROLLBACK.sql` - Rollback procedure
3. `20250108_DefaultDataPopulation.sql` - Default data inserts
4. `20250108_DataQualityValidation.sql` - Validation queries

---

## Appendix B: Contact & Support

**Data Architecture Manager**
Role: Database Design, Migrations, Performance Optimization
Scope: Schema design, data modeling, integrity enforcement

**Backend Team**
Required Action: Update service layer to consume new database structure
Timeline: Within 1 week of migration

**Frontend Team**
Required Action: Remove hard-coded symbols, implement dynamic loading
Timeline: Within 2 weeks of migration

---

**End of Document**
