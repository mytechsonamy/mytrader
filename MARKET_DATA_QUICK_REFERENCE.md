# Market Data Quick Reference Guide
## myTrader Platform

---

## QUICK STATUS CHECK

```bash
# One-line health check
docker exec mytrader_postgres psql -U postgres -d mytrader -c "SELECT COUNT(*) as records, MAX(\"Timestamp\") as latest FROM market_data;"

# Full monitoring report
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/backend
./monitor_market_data.sh
```

**Expected**: Records > 50, Latest timestamp < 15 minutes ago

---

## COMMON ISSUES & FIXES

### Issue 1: Dashboard Shows Empty Accordions

**Quick Fix**:
```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/backend
docker cp populate_market_data.sql mytrader_postgres:/tmp/
docker exec mytrader_postgres psql -U postgres -d mytrader -f /tmp/populate_market_data.sql
```

**Result**: Populates 76 records, dashboard should show data within 30 seconds

---

### Issue 2: Data is Stale (> 15 minutes old)

**Diagnosis**:
```bash
docker logs mytrader_api --tail 50 | grep -i "5-minute sync"
```

**Look for**: "Processed: 0" means sync is not fetching new data

**Temporary Fix**: Re-run population script (see Issue 1)

**Permanent Fix**: Implement long-term fixes in `MARKET_DATA_CRITICAL_FIX_REPORT.md`

---

### Issue 3: Specific Exchange Missing Data

**Check which exchanges have data**:
```bash
docker exec mytrader_postgres psql -U postgres -d mytrader -c "
SELECT
    CASE
        WHEN \"Symbol\" IN ('THYAO', 'GARAN', 'SISE') THEN 'BIST'
        WHEN \"Symbol\" IN ('AAPL', 'MSFT', 'GOOGL', 'NVDA', 'TSLA') THEN 'NASDAQ'
        WHEN \"Symbol\" IN ('JPM', 'BA') THEN 'NYSE'
        WHEN \"Symbol\" LIKE '%USDT' THEN 'BINANCE'
    END AS exchange,
    COUNT(*) as records
FROM market_data
GROUP BY exchange;"
```

**Fix**: Re-run population script (populates all exchanges)

---

## MANUAL DATA REFRESH

### Refresh All Data
```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/backend
docker cp populate_market_data.sql mytrader_postgres:/tmp/
docker exec mytrader_postgres psql -U postgres -d mytrader -f /tmp/populate_market_data.sql
```

### Refresh Specific Symbol
```sql
-- Example: Refresh BTCUSDT with current price
INSERT INTO market_data ("Id", "Symbol", "Timeframe", "Timestamp", "Open", "High", "Low", "Close", "Volume")
VALUES (gen_random_uuid(), 'BTCUSDT', '5MIN', NOW(), 123500, 123600, 123400, 123550, 850.5)
ON CONFLICT ("Symbol", "Timeframe", "Timestamp") DO NOTHING;
```

---

## MONITORING COMMANDS

### Check Total Records
```bash
docker exec mytrader_postgres psql -U postgres -d mytrader -c "SELECT COUNT(*) FROM market_data;"
```

### Check Data Age
```bash
docker exec mytrader_postgres psql -U postgres -d mytrader -c "
SELECT
    MAX(\"Timestamp\") as latest,
    ROUND(EXTRACT(EPOCH FROM (NOW() - MAX(\"Timestamp\"))) / 60, 1) as minutes_old
FROM market_data;"
```

### Check Per-Symbol Status
```bash
docker exec mytrader_postgres psql -U postgres -d mytrader -c "
SELECT
    s.ticker,
    s.venue,
    (SELECT COUNT(*) FROM market_data md WHERE md.\"Symbol\" = s.ticker) as record_count,
    (SELECT MAX(md.\"Timestamp\") FROM market_data md WHERE md.\"Symbol\" = s.ticker) as latest
FROM symbols s
WHERE s.is_active = true
ORDER BY s.venue, s.ticker;"
```

---

## SERVICE LOGS

### Watch Sync Activity
```bash
docker logs -f mytrader_api | grep -i "5-minute sync"
```

### Check Binance WebSocket
```bash
docker logs -f mytrader_api | grep -i "binance\|btcusdt\|ethusdt"
```

### Check Last 100 Lines for Errors
```bash
docker logs mytrader_api --tail 100 | grep -i "error\|exception\|fail"
```

---

## DATABASE ACCESS

### Direct Database Access
```bash
docker exec -it mytrader_postgres psql -U postgres -d mytrader
```

### Common Queries
```sql
-- Show all data
SELECT * FROM market_data ORDER BY "Timestamp" DESC LIMIT 20;

-- Show latest price per symbol
SELECT DISTINCT ON ("Symbol")
    "Symbol",
    "Close" as price,
    "Volume",
    "Timestamp"
FROM market_data
ORDER BY "Symbol", "Timestamp" DESC;

-- Delete old data (keep last 24 hours)
DELETE FROM market_data WHERE "Timestamp" < NOW() - INTERVAL '24 hours';

-- Clear all data (use with caution!)
TRUNCATE market_data;
```

---

## HEALTH CHECK THRESHOLDS

| Metric | Healthy | Degraded | Critical |
|--------|---------|----------|----------|
| Total Records | > 50 | 10-50 | < 10 |
| Data Age (minutes) | < 15 | 15-60 | > 60 |
| Symbols with Data | 19/19 | 10-18 | < 10 |
| Records per Symbol | > 2 | 1-2 | 0 |

---

## FILES LOCATION

| File | Path | Purpose |
|------|------|---------|
| Population Script | `backend/populate_market_data.sql` | Initial data load |
| Monitoring Script | `backend/monitor_market_data.sh` | Health check |
| Fix Report | `MARKET_DATA_CRITICAL_FIX_REPORT.md` | Technical details |
| Quick Reference | `MARKET_DATA_QUICK_REFERENCE.md` | This file |

---

## EMERGENCY CONTACTS

### If System is Down
1. Check Docker containers: `docker ps`
2. Restart API: `docker restart mytrader_api`
3. Check logs: `docker logs mytrader_api --tail 100`

### If Data is Empty
1. Run monitoring script: `./backend/monitor_market_data.sh`
2. Run population script: `docker exec ... populate_market_data.sql`
3. Verify: Check record count

### If Nothing Works
1. Backup current state: `docker exec mytrader_postgres pg_dump ...`
2. Review full fix report: `MARKET_DATA_CRITICAL_FIX_REPORT.md`
3. Check service registration in `backend/MyTrader.Api/Program.cs`
4. Verify database connection: `docker exec mytrader_postgres psql -U postgres -c "SELECT 1"`

---

## AUTOMATION RECOMMENDATIONS

### Cron Job: Auto-populate if empty (every hour)
```bash
# Add to crontab
0 * * * * cd /path/to/myTrader/backend && ./monitor_market_data.sh | grep -q "CRITICAL" && docker exec mytrader_postgres psql -U postgres -d mytrader -f /tmp/populate_market_data.sql
```

### Monitoring Alert: Data staleness
```bash
# Check data age and alert if > 30 minutes
#!/bin/bash
AGE=$(docker exec mytrader_postgres psql -U postgres -d mytrader -t -c "SELECT EXTRACT(EPOCH FROM (NOW() - MAX(\"Timestamp\"))) / 60 FROM market_data;")
if [ $(echo "$AGE > 30" | bc) -eq 1 ]; then
    echo "ALERT: Market data is $AGE minutes old!"
    # Send notification (email, Slack, etc.)
fi
```

---

## PERFORMANCE TIPS

### Query Optimization
```sql
-- Create index if not exists
CREATE INDEX IF NOT EXISTS idx_market_data_latest
ON market_data ("Symbol", "Timestamp" DESC);

-- Analyze table
ANALYZE market_data;

-- Check index usage
SELECT * FROM pg_stat_user_indexes WHERE tablename = 'market_data';
```

### Data Cleanup
```sql
-- Keep only last 7 days of 5MIN data
DELETE FROM market_data
WHERE "Timeframe" = '5MIN'
  AND "Timestamp" < NOW() - INTERVAL '7 days';

-- Vacuum to reclaim space
VACUUM ANALYZE market_data;
```

---

## TROUBLESHOOTING FLOWCHART

```
Dashboard Empty?
├─ Yes → Check database
│  ├─ market_data empty? → Run populate_market_data.sql
│  └─ market_data has data? → Check frontend
│     ├─ API responding? → Check logs
│     └─ CORS errors? → Check CORS config
└─ No → Data stale?
   ├─ Yes (> 15 min) → Check sync services
   │  └─ "Processed: 0" → Check market hours or run manual sync
   └─ No → System healthy ✅
```

---

**Last Updated**: October 9, 2025
**Version**: 1.0
**Maintained By**: Data Architecture Team
