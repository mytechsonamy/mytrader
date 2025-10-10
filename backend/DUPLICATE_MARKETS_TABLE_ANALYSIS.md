# Duplicate Markets Table Analysis and Remediation Plan

**Date:** 2025-10-10
**Database:** PostgreSQL (mytrader)
**Issue:** Two separate tables exist: `markets` (lowercase) and `Markets` (capitalized)
**Status:** CRITICAL - Requires immediate remediation

---

## Executive Summary

Your PostgreSQL database currently contains TWO separate `markets` tables with different schemas and purposes:

1. **`markets`** (lowercase) - Entity Framework managed, production table
2. **`Markets`** (capitalized) - Recently created migration table for market hours/holidays

**Answer to your question:** No, managing tables with only one letter difference in capitalization is NOT correct and violates PostgreSQL best practices. This is a naming collision that creates technical debt, confusion, and maintenance risks.

---

## Detailed Analysis

### Table 1: `markets` (lowercase) - PRIMARY TABLE

**Source:** Entity Framework Code First model mapping
**Model:** `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/backend/MyTrader.Core/Models/Market.cs`
**Schema:** 23 columns
**Purpose:** Core market/exchange entity for trading venues
**Data:** 3 rows (BIST, NASDAQ, NYSE)

**Key Columns:**
- `Id`, `code`, `name`, `name_tr`, `description`
- `AssetClassId` (FK to asset_classes)
- `country_code`, `timezone`, `primary_currency`
- `api_base_url`, `websocket_url`
- `default_commission_rate`, `min_commission`
- `market_config` (jsonb)
- `status`, `is_active`, `has_realtime_data`
- `data_delay_minutes`, `display_order`
- `created_at`, `updated_at`

**Foreign Key Relationships (REFERENCED BY):**
- `data_providers.MarketId` -> `markets.Id`
- `symbols.market_id` -> `markets.Id`
- `trading_sessions.MarketId` -> `markets.Id`

**Table Annotation in Model:**
```csharp
[Table("markets")]
public class Market { ... }
```

**DbContext Configuration:**
```csharp
modelBuilder.Entity<Market>(entity =>
{
    entity.ToTable("markets");
    // ... extensive configuration
});
```

---

### Table 2: `Markets` (capitalized) - DUPLICATE TABLE

**Source:** Manual SQL migration
**Migration:** `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/backend/MyTrader.Infrastructure/Migrations/20251010_CreateMarketsTable.sql`
**Schema:** 25 columns
**Purpose:** Market trading hours, holidays, and status management
**Data:** 4 rows (BIST, NASDAQ, NYSE, CRYPTO)

**Key Columns:**
- `Id`, `MarketCode`, `MarketName`, `Timezone`, `Country`, `Currency`
- `RegularMarketOpen`, `RegularMarketClose` (TIME)
- `PreMarketOpen`, `PreMarketClose` (TIME)
- `PostMarketOpen`, `PostMarketClose` (TIME)
- `TradingDays` (INTEGER ARRAY)
- `CurrentStatus`, `NextOpenTime`, `NextCloseTime`
- `IsHolidayToday`, `HolidayName`
- `EnableDataFetching`, `DataFetchInterval`, `DataFetchIntervalClosed`
- `CreatedAt`, `UpdatedAt`

**Related Tables:**
- `MarketHolidays` (FK: `MarketId` -> `Markets.Id`)
- `vw_MarketStatus` (view based on `Markets`)

**Stored Function:**
- `update_market_status()` - Updates market status and next open/close times

---

## PostgreSQL Case Sensitivity and Naming Conventions

### PostgreSQL Behavior

PostgreSQL treats **unquoted** identifiers as case-insensitive (folded to lowercase):
```sql
CREATE TABLE Markets (...);  -- Creates 'markets' (lowercase)
CREATE TABLE "Markets" (...); -- Creates 'Markets' (capitalized, quoted)
```

Your migration used **quoted identifiers** (`"Markets"`), creating a case-sensitive table name separate from the existing `markets` table.

### Industry Best Practices

**PostgreSQL Standard:** Use lowercase with underscores (snake_case)
- Good: `markets`, `market_hours`, `market_holidays`
- Bad: `Markets`, `MarketHours`, `marketHolidays`

**Entity Framework Default:** PascalCase mapped to lowercase
- Model: `Market` class
- Table: `markets` (automatic lowercase conversion)
- Columns: `PrimaryCurrency` -> `primary_currency`

### Why This Matters

1. **Query Confusion:** Developers must remember which table requires quotes
   ```sql
   SELECT * FROM markets;      -- Works (lowercase)
   SELECT * FROM "Markets";    -- Works (capitalized)
   SELECT * FROM Markets;      -- ERROR or wrong table
   ```

2. **ORM Conflicts:** Entity Framework expects `markets`, not `"Markets"`

3. **Maintenance Burden:** Two sources of truth for market data

4. **Data Synchronization:** No automated sync between tables

5. **Migration Risks:** Future schema changes could affect wrong table

---

## Root Cause Analysis

### Why Two Tables Exist

1. **Original Table (`markets`):** Created by Entity Framework migrations as part of core multi-asset data architecture
   - Stores market metadata, configuration, and relationships
   - Integrated with `symbols`, `data_providers`, `asset_classes`

2. **New Table (`Markets`):** Created manually on 2025-10-10 for market hours optimization
   - Intended to track trading sessions, holidays, and market status
   - Designed to optimize data fetching based on market open/closed status
   - Includes specialized fields for pre-market, post-market, and holiday management

### Why This Happened

- **Lack of Schema Discovery:** Migration author didn't check existing `markets` table
- **Case-Sensitivity Trap:** Using quoted identifiers created separate table
- **Purpose Overlap:** Both tables store market information but with different focuses

---

## Impact Assessment

### Current System State

**Active Table:** `markets` (lowercase)
- Referenced by 4 foreign keys (symbols, data_providers, trading_sessions)
- Used by Entity Framework models
- Production data in use

**Orphaned Table:** `Markets` (capitalized)
- Only referenced by `MarketHolidays` table
- Not used by Entity Framework
- Contains useful trading hours/holiday data not in `markets`

### Data Overlap

Both tables have:
- Market codes (BIST, NASDAQ, NYSE)
- Market names
- Timezone information
- Status tracking

`Markets` has unique data:
- Detailed trading hours (regular, pre-market, post-market)
- Holiday calendar (`MarketHolidays` table)
- Data fetching intervals
- Market status update logic (`update_market_status()` function)

---

## Recommendation: CONSOLIDATE INTO SINGLE TABLE

### Chosen Strategy: Merge `Markets` Features into `markets`

**Rationale:**
1. `markets` is the production table with established foreign key relationships
2. Entity Framework expects lowercase table names
3. Adding columns is safer than migrating foreign keys
4. Preserves existing data architecture

**Migration Approach:** Add missing columns to `markets`, migrate data, drop `Markets`

---

## Remediation Plan

### Phase 1: Schema Extension (Non-Breaking)

Add new columns to existing `markets` table:

```sql
-- Add trading hours columns
ALTER TABLE markets
ADD COLUMN regular_market_open TIME,
ADD COLUMN regular_market_close TIME,
ADD COLUMN pre_market_open TIME,
ADD COLUMN pre_market_close TIME,
ADD COLUMN post_market_open TIME,
ADD COLUMN post_market_close TIME;

-- Add trading days array
ALTER TABLE markets
ADD COLUMN trading_days INTEGER[] DEFAULT '{1,2,3,4,5}';

-- Add status tracking columns
ALTER TABLE markets
ADD COLUMN current_status VARCHAR(20),
ADD COLUMN next_open_time TIMESTAMPTZ,
ADD COLUMN next_close_time TIMESTAMPTZ,
ADD COLUMN status_last_updated TIMESTAMPTZ;

-- Add holiday tracking
ALTER TABLE markets
ADD COLUMN is_holiday_today BOOLEAN DEFAULT FALSE,
ADD COLUMN holiday_name VARCHAR(200);

-- Add data fetching configuration
ALTER TABLE markets
ADD COLUMN enable_data_fetching BOOLEAN DEFAULT TRUE,
ADD COLUMN data_fetch_interval INTEGER DEFAULT 5,
ADD COLUMN data_fetch_interval_closed INTEGER DEFAULT 300;

-- Add indexes for new columns
CREATE INDEX idx_markets_current_status ON markets(current_status);
CREATE INDEX idx_markets_next_open_time ON markets(next_open_time);
```

### Phase 2: Rename MarketHolidays Foreign Key

Recreate `MarketHolidays` table to reference `markets`:

```sql
-- Create new market_holidays table (lowercase, consistent naming)
CREATE TABLE market_holidays (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    market_id UUID NOT NULL REFERENCES markets(id) ON DELETE CASCADE,
    holiday_date DATE NOT NULL,
    holiday_name VARCHAR(200) NOT NULL,
    is_recurring BOOLEAN DEFAULT FALSE,
    recurring_month INTEGER,
    recurring_day INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT market_holidays_unique UNIQUE (market_id, holiday_date)
);

-- Migrate data from MarketHolidays to market_holidays
INSERT INTO market_holidays (market_id, holiday_date, holiday_name, is_recurring, recurring_month, recurring_day, created_at)
SELECT
    m.id,
    mh."HolidayDate",
    mh."HolidayName",
    mh."IsRecurring",
    mh."RecurringMonth",
    mh."RecurringDay",
    mh."CreatedAt"
FROM "MarketHolidays" mh
INNER JOIN "Markets" MM ON mh."MarketId" = MM."Id"
INNER JOIN markets m ON UPPER(m.code) = UPPER(MM."MarketCode");

-- Create indexes
CREATE INDEX idx_market_holidays_market_id_date ON market_holidays(market_id, holiday_date);
```

### Phase 3: Data Migration

Migrate trading hours and status data from `Markets` to `markets`:

```sql
-- Update BIST
UPDATE markets
SET
    regular_market_open = '10:00:00'::TIME,
    regular_market_close = '18:00:00'::TIME,
    trading_days = '{1,2,3,4,5}',
    enable_data_fetching = TRUE,
    data_fetch_interval = 5,
    data_fetch_interval_closed = 300,
    updated_at = NOW()
WHERE code = 'BIST';

-- Update NASDAQ
UPDATE markets
SET
    regular_market_open = '09:30:00'::TIME,
    regular_market_close = '16:00:00'::TIME,
    pre_market_open = '04:00:00'::TIME,
    pre_market_close = '09:30:00'::TIME,
    post_market_open = '16:00:00'::TIME,
    post_market_close = '20:00:00'::TIME,
    trading_days = '{1,2,3,4,5}',
    enable_data_fetching = TRUE,
    data_fetch_interval = 5,
    data_fetch_interval_closed = 300,
    updated_at = NOW()
WHERE code = 'NASDAQ';

-- Update NYSE
UPDATE markets
SET
    regular_market_open = '09:30:00'::TIME,
    regular_market_close = '16:00:00'::TIME,
    pre_market_open = '04:00:00'::TIME,
    pre_market_close = '09:30:00'::TIME,
    post_market_open = '16:00:00'::TIME,
    post_market_close = '20:00:00'::TIME,
    trading_days = '{1,2,3,4,5}',
    enable_data_fetching = TRUE,
    data_fetch_interval = 5,
    data_fetch_interval_closed = 300,
    updated_at = NOW()
WHERE code = 'NYSE';

-- Insert CRYPTO market (if not exists)
INSERT INTO markets (code, name, asset_class_id, country_code, timezone, primary_currency, trading_days, enable_data_fetching, data_fetch_interval, data_fetch_interval_closed, is_active, created_at, updated_at, status)
SELECT
    'CRYPTO',
    'Cryptocurrency Markets',
    (SELECT id FROM asset_classes WHERE code = 'CRYPTO' LIMIT 1),
    'GLOBAL',
    'UTC',
    'USD',
    '{1,2,3,4,5,6,7}',
    TRUE,
    5,
    5,
    TRUE,
    NOW(),
    NOW(),
    'OPEN'
WHERE NOT EXISTS (SELECT 1 FROM markets WHERE code = 'CRYPTO');
```

### Phase 4: Migrate update_market_status() Function

Recreate function to work with lowercase `markets` table:

```sql
-- Drop old function referencing "Markets"
DROP FUNCTION IF EXISTS update_market_status();

-- Create new function for 'markets' table
CREATE OR REPLACE FUNCTION update_market_status()
RETURNS TABLE (
    market_code VARCHAR(50),
    old_status VARCHAR(20),
    new_status VARCHAR(20),
    next_open TIMESTAMPTZ,
    next_close TIMESTAMPTZ
) AS $$
DECLARE
    v_market RECORD;
    v_current_time TIMESTAMPTZ;
    v_local_time TIME;
    v_local_date DATE;
    v_day_of_week INTEGER;
    v_new_status VARCHAR(20);
    v_next_open TIMESTAMPTZ;
    v_next_close TIMESTAMPTZ;
    v_is_holiday BOOLEAN;
    v_holiday_name VARCHAR(200);
BEGIN
    FOR v_market IN SELECT * FROM markets WHERE is_active = TRUE
    LOOP
        v_current_time := NOW();
        v_local_time := (v_current_time AT TIME ZONE v_market.timezone)::TIME;
        v_local_date := (v_current_time AT TIME ZONE v_market.timezone)::DATE;
        v_day_of_week := EXTRACT(ISODOW FROM v_local_date);

        -- Check if today is a holiday
        SELECT TRUE, mh.holiday_name
        INTO v_is_holiday, v_holiday_name
        FROM market_holidays mh
        WHERE mh.market_id = v_market.id
          AND mh.holiday_date = v_local_date
        LIMIT 1;

        v_is_holiday := COALESCE(v_is_holiday, FALSE);

        -- Determine market status
        IF v_market.code = 'CRYPTO' THEN
            v_new_status := 'OPEN';
            v_next_open := NULL;
            v_next_close := NULL;

        ELSIF v_is_holiday THEN
            v_new_status := 'HOLIDAY';
            v_next_open := NULL;
            v_next_close := NULL;

        ELSIF NOT (v_day_of_week = ANY(v_market.trading_days)) THEN
            v_new_status := 'CLOSED';
            v_next_open := NULL;
            v_next_close := NULL;

        ELSE
            IF v_market.regular_market_open IS NOT NULL AND v_market.regular_market_close IS NOT NULL THEN
                IF v_local_time >= v_market.regular_market_open AND v_local_time < v_market.regular_market_close THEN
                    v_new_status := 'OPEN';
                    v_next_close := (v_local_date || ' ' || v_market.regular_market_close)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_open := NULL;

                ELSIF v_market.pre_market_open IS NOT NULL AND
                      v_local_time >= v_market.pre_market_open AND v_local_time < v_market.regular_market_open THEN
                    v_new_status := 'PRE_MARKET';
                    v_next_open := (v_local_date || ' ' || v_market.regular_market_open)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_close := NULL;

                ELSIF v_market.post_market_open IS NOT NULL AND
                      v_local_time >= v_market.post_market_open AND v_local_time < v_market.post_market_close THEN
                    v_new_status := 'POST_MARKET';
                    v_next_close := (v_local_date || ' ' || v_market.post_market_close)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_open := NULL;

                ELSE
                    v_new_status := 'CLOSED';
                    v_next_open := (v_local_date + INTERVAL '1 day' || ' ' || v_market.regular_market_open)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_close := NULL;
                END IF;
            ELSE
                v_new_status := 'CLOSED';
                v_next_open := NULL;
                v_next_close := NULL;
            END IF;
        END IF;

        -- Update market status
        UPDATE markets
        SET
            current_status = v_new_status,
            next_open_time = v_next_open,
            next_close_time = v_next_close,
            is_holiday_today = v_is_holiday,
            holiday_name = v_holiday_name,
            status_last_updated = v_current_time,
            updated_at = v_current_time
        WHERE id = v_market.id;

        -- Return results
        RETURN QUERY SELECT
            v_market.code,
            v_market.status::VARCHAR(20),
            v_new_status::VARCHAR(20),
            v_next_open,
            v_next_close;
    END LOOP;
END;
$$ LANGUAGE plpgsql;
```

### Phase 5: Recreate View

```sql
-- Drop old view
DROP VIEW IF EXISTS "vw_MarketStatus";

-- Create new view for lowercase markets
CREATE OR REPLACE VIEW vw_market_status AS
SELECT
    m.code AS market_code,
    m.name AS market_name,
    m.timezone,
    m.current_status,
    m.next_open_time,
    m.next_close_time,
    m.is_holiday_today,
    m.holiday_name,
    m.enable_data_fetching,
    CASE
        WHEN m.current_status = 'OPEN' THEN m.data_fetch_interval
        ELSE m.data_fetch_interval_closed
    END AS recommended_fetch_interval,
    m.status_last_updated,
    EXTRACT(EPOCH FROM (NOW() - m.status_last_updated)) AS seconds_since_update
FROM markets m
WHERE m.is_active = TRUE;

COMMENT ON VIEW vw_market_status IS 'Current market status with recommended data fetch intervals';
```

### Phase 6: Drop Duplicate Tables

```sql
-- Drop old capitalized tables (after verification)
DROP VIEW IF EXISTS "vw_MarketStatus";
DROP TABLE IF EXISTS "MarketHolidays" CASCADE;
DROP TABLE IF EXISTS "Markets" CASCADE;
```

### Phase 7: Update Entity Framework Model

Update `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/backend/MyTrader.Core/Models/Market.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("markets")]
public class Market
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Existing columns...
    [Required]
    [MaxLength(20)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    // ... existing properties ...

    // NEW: Trading hours
    [Column("regular_market_open")]
    public TimeOnly? RegularMarketOpen { get; set; }

    [Column("regular_market_close")]
    public TimeOnly? RegularMarketClose { get; set; }

    [Column("pre_market_open")]
    public TimeOnly? PreMarketOpen { get; set; }

    [Column("pre_market_close")]
    public TimeOnly? PreMarketClose { get; set; }

    [Column("post_market_open")]
    public TimeOnly? PostMarketOpen { get; set; }

    [Column("post_market_close")]
    public TimeOnly? PostMarketClose { get; set; }

    // NEW: Trading days
    [Column("trading_days")]
    public int[] TradingDays { get; set; } = new[] { 1, 2, 3, 4, 5 };

    // NEW: Status tracking
    [Column("current_status")]
    [MaxLength(20)]
    public string? CurrentStatus { get; set; }

    [Column("next_open_time")]
    public DateTime? NextOpenTime { get; set; }

    [Column("next_close_time")]
    public DateTime? NextCloseTime { get; set; }

    [Column("status_last_updated")]
    public DateTime? StatusLastUpdated { get; set; }

    // NEW: Holiday tracking
    [Column("is_holiday_today")]
    public bool IsHolidayToday { get; set; }

    [Column("holiday_name")]
    [MaxLength(200)]
    public string? HolidayName { get; set; }

    // NEW: Data fetching configuration
    [Column("enable_data_fetching")]
    public bool EnableDataFetching { get; set; } = true;

    [Column("data_fetch_interval")]
    public int DataFetchInterval { get; set; } = 5;

    [Column("data_fetch_interval_closed")]
    public int DataFetchIntervalClosed { get; set; } = 300;

    // Navigation properties
    [ForeignKey("AssetClassId")]
    public AssetClass AssetClass { get; set; } = null!;

    public ICollection<TradingSession> TradingSessions { get; set; } = new List<TradingSession>();
    public ICollection<Symbol> Symbols { get; set; } = new List<Symbol>();
    public ICollection<DataProvider> DataProviders { get; set; } = new List<DataProvider>();
    public ICollection<MarketHoliday> MarketHolidays { get; set; } = new List<MarketHoliday>();
}
```

Create new `MarketHoliday` model:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("market_holidays")]
public class MarketHoliday
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid MarketId { get; set; }

    [Required]
    [Column("holiday_date")]
    public DateOnly HolidayDate { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("holiday_name")]
    public string HolidayName { get; set; } = string.Empty;

    [Column("is_recurring")]
    public bool IsRecurring { get; set; }

    [Column("recurring_month")]
    public int? RecurringMonth { get; set; }

    [Column("recurring_day")]
    public int? RecurringDay { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("MarketId")]
    public Market Market { get; set; } = null!;
}
```

Update DbContext configuration in `TradingDbContext.cs`:

```csharp
// Market configuration (update existing)
modelBuilder.Entity<Market>(entity =>
{
    entity.ToTable("markets");
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.Code).IsUnique();
    entity.HasIndex(e => new { e.IsActive, e.DisplayOrder });
    entity.HasIndex(e => e.AssetClassId);
    entity.HasIndex(e => e.CurrentStatus);
    entity.HasIndex(e => e.NextOpenTime);

    // ... existing property configurations ...

    // NEW: Trading hours array configuration
    entity.Property(e => e.TradingDays)
        .HasColumnName("trading_days")
        .HasColumnType("integer[]");

    entity.HasOne(e => e.AssetClass)
          .WithMany(a => a.Markets)
          .HasForeignKey(e => e.AssetClassId)
          .OnDelete(DeleteBehavior.Restrict);
});

// MarketHoliday configuration (NEW)
modelBuilder.Entity<MarketHoliday>(entity =>
{
    entity.ToTable("market_holidays");
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => new { e.MarketId, e.HolidayDate }).IsUnique();

    entity.Property(e => e.HolidayDate)
        .HasColumnName("holiday_date")
        .HasColumnType("date");

    entity.HasOne(e => e.Market)
          .WithMany(m => m.MarketHolidays)
          .HasForeignKey(e => e.MarketId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

---

## Migration Script

Complete migration script combining all phases:

**File:** `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/backend/MyTrader.Infrastructure/Migrations/20251010_ConsolidateMarketsTables.sql`

```sql
-- =====================================================================
-- Migration: Consolidate Duplicate Markets Tables
-- Version: 1.0
-- Date: 2025-10-10
-- Description: Merge "Markets" table functionality into existing 'markets' table
-- =====================================================================

BEGIN;

-- =====================================================================
-- PHASE 1: Extend markets table schema
-- =====================================================================

ALTER TABLE markets
ADD COLUMN IF NOT EXISTS regular_market_open TIME,
ADD COLUMN IF NOT EXISTS regular_market_close TIME,
ADD COLUMN IF NOT EXISTS pre_market_open TIME,
ADD COLUMN IF NOT EXISTS pre_market_close TIME,
ADD COLUMN IF NOT EXISTS post_market_open TIME,
ADD COLUMN IF NOT EXISTS post_market_close TIME,
ADD COLUMN IF NOT EXISTS trading_days INTEGER[] DEFAULT '{1,2,3,4,5}',
ADD COLUMN IF NOT EXISTS current_status VARCHAR(20),
ADD COLUMN IF NOT EXISTS next_open_time TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS next_close_time TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS status_last_updated TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS is_holiday_today BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS holiday_name VARCHAR(200),
ADD COLUMN IF NOT EXISTS enable_data_fetching BOOLEAN DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS data_fetch_interval INTEGER DEFAULT 5,
ADD COLUMN IF NOT EXISTS data_fetch_interval_closed INTEGER DEFAULT 300;

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_markets_current_status ON markets(current_status);
CREATE INDEX IF NOT EXISTS idx_markets_next_open_time ON markets(next_open_time);

RAISE NOTICE 'Phase 1 Complete: Schema extended';

-- =====================================================================
-- PHASE 2: Create market_holidays table (lowercase)
-- =====================================================================

CREATE TABLE IF NOT EXISTS market_holidays (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    market_id UUID NOT NULL REFERENCES markets(id) ON DELETE CASCADE,
    holiday_date DATE NOT NULL,
    holiday_name VARCHAR(200) NOT NULL,
    is_recurring BOOLEAN DEFAULT FALSE,
    recurring_month INTEGER,
    recurring_day INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT market_holidays_unique UNIQUE (market_id, holiday_date)
);

CREATE INDEX IF NOT EXISTS idx_market_holidays_market_id_date
ON market_holidays(market_id, holiday_date);

RAISE NOTICE 'Phase 2 Complete: market_holidays table created';

-- =====================================================================
-- PHASE 3: Migrate data from "Markets" to markets
-- =====================================================================

-- Update BIST
UPDATE markets m
SET
    regular_market_open = MM."RegularMarketOpen",
    regular_market_close = MM."RegularMarketClose",
    pre_market_open = MM."PreMarketOpen",
    pre_market_close = MM."PreMarketClose",
    post_market_open = MM."PostMarketOpen",
    post_market_close = MM."PostMarketClose",
    trading_days = MM."TradingDays",
    current_status = MM."CurrentStatus",
    next_open_time = MM."NextOpenTime",
    next_close_time = MM."NextCloseTime",
    status_last_updated = MM."StatusLastUpdated",
    is_holiday_today = MM."IsHolidayToday",
    holiday_name = MM."HolidayName",
    enable_data_fetching = MM."EnableDataFetching",
    data_fetch_interval = MM."DataFetchInterval",
    data_fetch_interval_closed = MM."DataFetchIntervalClosed",
    updated_at = NOW()
FROM "Markets" MM
WHERE UPPER(m.code) = UPPER(MM."MarketCode")
  AND MM."MarketCode" IN ('BIST', 'NASDAQ', 'NYSE');

-- Insert CRYPTO market if not exists
INSERT INTO markets (
    code, name, asset_class_id, country_code, timezone, primary_currency,
    regular_market_open, regular_market_close, trading_days,
    current_status, enable_data_fetching, data_fetch_interval,
    data_fetch_interval_closed, is_active, status, created_at, updated_at
)
SELECT
    'CRYPTO',
    'Cryptocurrency Markets',
    (SELECT id FROM asset_classes WHERE code = 'CRYPTO' LIMIT 1),
    'GLOBAL',
    'UTC',
    'USD',
    NULL,
    NULL,
    '{1,2,3,4,5,6,7}',
    'OPEN',
    TRUE,
    5,
    5,
    TRUE,
    'OPEN',
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM markets WHERE code = 'CRYPTO');

RAISE NOTICE 'Phase 3 Complete: Data migrated from Markets to markets';

-- =====================================================================
-- PHASE 4: Migrate holiday data
-- =====================================================================

INSERT INTO market_holidays (market_id, holiday_date, holiday_name, is_recurring, recurring_month, recurring_day, created_at)
SELECT
    m.id,
    mh."HolidayDate",
    mh."HolidayName",
    mh."IsRecurring",
    mh."RecurringMonth",
    mh."RecurringDay",
    mh."CreatedAt"
FROM "MarketHolidays" mh
INNER JOIN "Markets" MM ON mh."MarketId" = MM."Id"
INNER JOIN markets m ON UPPER(m.code) = UPPER(MM."MarketCode")
ON CONFLICT (market_id, holiday_date) DO NOTHING;

RAISE NOTICE 'Phase 4 Complete: Holiday data migrated';

-- =====================================================================
-- PHASE 5: Drop old function and create new one
-- =====================================================================

DROP FUNCTION IF EXISTS update_market_status();

CREATE OR REPLACE FUNCTION update_market_status()
RETURNS TABLE (
    market_code VARCHAR(50),
    old_status VARCHAR(20),
    new_status VARCHAR(20),
    next_open TIMESTAMPTZ,
    next_close TIMESTAMPTZ
) AS $$
DECLARE
    v_market RECORD;
    v_current_time TIMESTAMPTZ;
    v_local_time TIME;
    v_local_date DATE;
    v_day_of_week INTEGER;
    v_new_status VARCHAR(20);
    v_next_open TIMESTAMPTZ;
    v_next_close TIMESTAMPTZ;
    v_is_holiday BOOLEAN;
    v_holiday_name VARCHAR(200);
BEGIN
    FOR v_market IN SELECT * FROM markets WHERE is_active = TRUE
    LOOP
        v_current_time := NOW();
        v_local_time := (v_current_time AT TIME ZONE v_market.timezone)::TIME;
        v_local_date := (v_current_time AT TIME ZONE v_market.timezone)::DATE;
        v_day_of_week := EXTRACT(ISODOW FROM v_local_date);

        SELECT TRUE, mh.holiday_name
        INTO v_is_holiday, v_holiday_name
        FROM market_holidays mh
        WHERE mh.market_id = v_market.id
          AND mh.holiday_date = v_local_date
        LIMIT 1;

        v_is_holiday := COALESCE(v_is_holiday, FALSE);

        IF v_market.code = 'CRYPTO' THEN
            v_new_status := 'OPEN';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSIF v_is_holiday THEN
            v_new_status := 'HOLIDAY';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSIF NOT (v_day_of_week = ANY(v_market.trading_days)) THEN
            v_new_status := 'CLOSED';
            v_next_open := NULL;
            v_next_close := NULL;
        ELSE
            IF v_market.regular_market_open IS NOT NULL AND v_market.regular_market_close IS NOT NULL THEN
                IF v_local_time >= v_market.regular_market_open AND v_local_time < v_market.regular_market_close THEN
                    v_new_status := 'OPEN';
                    v_next_close := (v_local_date || ' ' || v_market.regular_market_close)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_open := NULL;
                ELSIF v_market.pre_market_open IS NOT NULL AND
                      v_local_time >= v_market.pre_market_open AND v_local_time < v_market.regular_market_open THEN
                    v_new_status := 'PRE_MARKET';
                    v_next_open := (v_local_date || ' ' || v_market.regular_market_open)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_close := NULL;
                ELSIF v_market.post_market_open IS NOT NULL AND
                      v_local_time >= v_market.post_market_open AND v_local_time < v_market.post_market_close THEN
                    v_new_status := 'POST_MARKET';
                    v_next_close := (v_local_date || ' ' || v_market.post_market_close)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_open := NULL;
                ELSE
                    v_new_status := 'CLOSED';
                    v_next_open := (v_local_date + INTERVAL '1 day' || ' ' || v_market.regular_market_open)::TIMESTAMP AT TIME ZONE v_market.timezone;
                    v_next_close := NULL;
                END IF;
            ELSE
                v_new_status := 'CLOSED';
                v_next_open := NULL;
                v_next_close := NULL;
            END IF;
        END IF;

        UPDATE markets
        SET
            current_status = v_new_status,
            next_open_time = v_next_open,
            next_close_time = v_next_close,
            is_holiday_today = v_is_holiday,
            holiday_name = v_holiday_name,
            status_last_updated = v_current_time,
            updated_at = v_current_time
        WHERE id = v_market.id;

        RETURN QUERY SELECT
            v_market.code,
            v_market.status::VARCHAR(20),
            v_new_status::VARCHAR(20),
            v_next_open,
            v_next_close;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE 'Phase 5 Complete: update_market_status() function recreated';

-- =====================================================================
-- PHASE 6: Recreate view
-- =====================================================================

DROP VIEW IF EXISTS "vw_MarketStatus";

CREATE OR REPLACE VIEW vw_market_status AS
SELECT
    m.code AS market_code,
    m.name AS market_name,
    m.timezone,
    m.current_status,
    m.next_open_time,
    m.next_close_time,
    m.is_holiday_today,
    m.holiday_name,
    m.enable_data_fetching,
    CASE
        WHEN m.current_status = 'OPEN' THEN m.data_fetch_interval
        ELSE m.data_fetch_interval_closed
    END AS recommended_fetch_interval,
    m.status_last_updated,
    EXTRACT(EPOCH FROM (NOW() - m.status_last_updated)) AS seconds_since_update
FROM markets m
WHERE m.is_active = TRUE;

COMMENT ON VIEW vw_market_status IS 'Current market status with recommended data fetch intervals';

RAISE NOTICE 'Phase 6 Complete: View recreated';

-- =====================================================================
-- PHASE 7: Update initial market status
-- =====================================================================

SELECT * FROM update_market_status();

RAISE NOTICE 'Phase 7 Complete: Market status updated';

-- =====================================================================
-- PHASE 8: Drop duplicate tables
-- =====================================================================

DROP TABLE IF EXISTS "MarketHolidays" CASCADE;
DROP TABLE IF EXISTS "Markets" CASCADE;

RAISE NOTICE 'Phase 8 Complete: Duplicate tables dropped';

-- =====================================================================
-- VERIFICATION
-- =====================================================================

DO $$
DECLARE
    markets_count INTEGER;
    holidays_count INTEGER;
    crypto_exists BOOLEAN;
BEGIN
    SELECT COUNT(*) INTO markets_count FROM markets WHERE is_active = TRUE;
    SELECT COUNT(*) INTO holidays_count FROM market_holidays;
    SELECT EXISTS(SELECT 1 FROM markets WHERE code = 'CRYPTO') INTO crypto_exists;

    RAISE NOTICE '=== Migration Verification ===';
    RAISE NOTICE 'Active markets: %', markets_count;
    RAISE NOTICE 'Total holidays: %', holidays_count;
    RAISE NOTICE 'CRYPTO market exists: %', crypto_exists;
    RAISE NOTICE '=============================';

    IF markets_count < 3 THEN
        RAISE EXCEPTION 'Migration failed: Expected at least 3 active markets, found %', markets_count;
    END IF;
END $$;

COMMIT;

-- =====================================================================
-- MIGRATION COMPLETE
-- =====================================================================

SELECT
    'Migration 20251010_ConsolidateMarketsTables completed successfully' AS status,
    COUNT(*) AS total_markets,
    (SELECT COUNT(*) FROM market_holidays) AS total_holidays
FROM markets;
```

---

## Rollback Plan

**File:** `/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/backend/MyTrader.Infrastructure/Migrations/20251010_ConsolidateMarketsTables_ROLLBACK.sql`

```sql
-- =====================================================================
-- ROLLBACK: Consolidate Duplicate Markets Tables
-- =====================================================================

BEGIN;

-- Recreate "Markets" table from backup (if needed)
-- This assumes you have backed up the data before running the migration

-- Drop new structures
DROP VIEW IF EXISTS vw_market_status;
DROP FUNCTION IF EXISTS update_market_status();
DROP TABLE IF EXISTS market_holidays CASCADE;

-- Remove added columns from markets
ALTER TABLE markets
DROP COLUMN IF EXISTS regular_market_open,
DROP COLUMN IF EXISTS regular_market_close,
DROP COLUMN IF EXISTS pre_market_open,
DROP COLUMN IF EXISTS pre_market_close,
DROP COLUMN IF EXISTS post_market_open,
DROP COLUMN IF EXISTS post_market_close,
DROP COLUMN IF EXISTS trading_days,
DROP COLUMN IF EXISTS current_status,
DROP COLUMN IF EXISTS next_open_time,
DROP COLUMN IF EXISTS next_close_time,
DROP COLUMN IF EXISTS status_last_updated,
DROP COLUMN IF EXISTS is_holiday_today,
DROP COLUMN IF EXISTS holiday_name,
DROP COLUMN IF EXISTS enable_data_fetching,
DROP COLUMN IF EXISTS data_fetch_interval,
DROP COLUMN IF EXISTS data_fetch_interval_closed;

-- Drop indexes
DROP INDEX IF EXISTS idx_markets_current_status;
DROP INDEX IF EXISTS idx_markets_next_open_time;

-- Restore original 20251010_CreateMarketsTable.sql migration
\i /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/backend/MyTrader.Infrastructure/Migrations/20251010_CreateMarketsTable.sql

COMMIT;

RAISE NOTICE 'Rollback complete: Restored to dual-table configuration';
```

---

## Naming Convention Guidelines (Going Forward)

### PostgreSQL Table Naming Standard

1. **Use lowercase with underscores (snake_case)**
   - Tables: `markets`, `market_holidays`, `user_sessions`
   - Columns: `market_code`, `trading_days`, `is_active`

2. **Never use quoted identifiers** unless absolutely necessary
   - Avoid: `CREATE TABLE "Markets"`
   - Prefer: `CREATE TABLE markets`

3. **Plural table names** for entity collections
   - `markets` (not `market`)
   - `symbols` (not `symbol`)
   - `users` (not `user`)

### Entity Framework Mapping

1. **Model class names:** PascalCase (singular)
   ```csharp
   public class Market { }
   ```

2. **Explicit table name mapping:** Use `[Table]` attribute
   ```csharp
   [Table("markets")]
   public class Market { }
   ```

3. **Column name mapping:** Use `[Column]` attribute
   ```csharp
   [Column("market_code")]
   public string Code { get; set; }
   ```

### Migration Checklist

Before creating any new table:

1. Check if table already exists:
   ```sql
   SELECT * FROM information_schema.tables
   WHERE table_name ILIKE '%your_table%';
   ```

2. Check Entity Framework models in `/MyTrader.Core/Models/`

3. Review DbContext configuration in `TradingDbContext.cs`

4. Search existing migrations for similar tables

5. Use lowercase, unquoted identifiers

6. Document purpose and relationships in migration header

---

## Timeline and Execution

### Pre-Migration

1. **Backup database:**
   ```bash
   pg_dump -h localhost -p 5434 -U postgres -d mytrader > mytrader_backup_20251010.sql
   ```

2. **Review migration script** with team

3. **Test in development environment**

### Execution Window

- **Downtime required:** Approximately 5-10 minutes
- **Best time:** Low-traffic period
- **Dependencies:** Stop API services during migration

### Post-Migration Verification

1. **Verify table count:**
   ```sql
   SELECT COUNT(*) FROM markets; -- Should be 4 (BIST, NASDAQ, NYSE, CRYPTO)
   ```

2. **Verify holidays migrated:**
   ```sql
   SELECT COUNT(*) FROM market_holidays; -- Should match old MarketHolidays count
   ```

3. **Test market status function:**
   ```sql
   SELECT * FROM update_market_status();
   ```

4. **Test view:**
   ```sql
   SELECT * FROM vw_market_status;
   ```

5. **Verify foreign keys:**
   ```sql
   SELECT * FROM symbols WHERE market_id IS NOT NULL LIMIT 5;
   ```

6. **Run Entity Framework migrations:**
   ```bash
   cd backend/MyTrader.Api
   dotnet ef database update
   ```

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Data loss during migration | HIGH | Full database backup before execution |
| Foreign key constraint violations | MEDIUM | Migration script handles FK updates automatically |
| Application downtime | MEDIUM | Execute during low-traffic window |
| Entity Framework model mismatch | MEDIUM | Update models before deploying API |
| Query performance degradation | LOW | Indexes created for new columns |
| Rollback complexity | MEDIUM | Rollback script provided |

---

## Success Criteria

1. Single `markets` table contains all market metadata and trading hours
2. All foreign key relationships point to `markets` (lowercase)
3. `market_holidays` table successfully migrated with all data
4. `update_market_status()` function works with lowercase table
5. `vw_market_status` view returns expected data
6. No orphaned data or broken relationships
7. Entity Framework models match database schema
8. Application connects and queries successfully

---

## Conclusion

**Direct Answer:** Managing tables with only case-sensitivity differences (markets vs Markets) is NOT correct and violates PostgreSQL best practices. This creates:

- Query confusion requiring quoted identifiers
- ORM mapping conflicts
- Maintenance complexity
- Data synchronization issues

**Recommendation:** Consolidate immediately using the provided migration script to merge `Markets` functionality into the existing `markets` table.

**Next Steps:**

1. Review this analysis with your team
2. Schedule maintenance window
3. Backup database
4. Execute consolidation migration
5. Update Entity Framework models
6. Deploy updated application
7. Verify all functionality

This remediation aligns with PostgreSQL standards, Entity Framework conventions, and database normalization principles while preserving all valuable functionality from both tables.
