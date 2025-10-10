#!/bin/bash

# =================================================================
# Market Data Monitoring Script
# Purpose: Monitor market_data table health and freshness
# Usage: ./monitor_market_data.sh
# =================================================================

set -e

POSTGRES_CONTAINER="mytrader_postgres"
DB_NAME="mytrader"
DB_USER="postgres"

echo "======================================================================"
echo "MYTRADER MARKET DATA HEALTH CHECK"
echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S %Z')"
echo "======================================================================"
echo ""

# Check if container is running
if ! docker ps | grep -q $POSTGRES_CONTAINER; then
    echo "❌ ERROR: PostgreSQL container '$POSTGRES_CONTAINER' is not running"
    exit 1
fi

echo "✅ PostgreSQL container is running"
echo ""

# Check total records
echo "📊 TOTAL RECORDS:"
echo "----------------------------------------------------------------------"
docker exec $POSTGRES_CONTAINER psql -U $DB_USER -d $DB_NAME -c "
SELECT
    COUNT(*) as total_records,
    COUNT(DISTINCT \"Symbol\") as unique_symbols,
    MIN(\"Timestamp\") as oldest_data,
    MAX(\"Timestamp\") as newest_data
FROM market_data;
"
echo ""

# Check by exchange
echo "📈 RECORDS BY EXCHANGE:"
echo "----------------------------------------------------------------------"
docker exec $POSTGRES_CONTAINER psql -U $DB_USER -d $DB_NAME -c "
SELECT
    CASE
        WHEN \"Symbol\" IN ('THYAO', 'GARAN', 'SISE') THEN 'BIST'
        WHEN \"Symbol\" IN ('AAPL', 'MSFT', 'GOOGL', 'NVDA', 'TSLA') THEN 'NASDAQ'
        WHEN \"Symbol\" IN ('JPM', 'BA') THEN 'NYSE'
        WHEN \"Symbol\" LIKE '%USDT' THEN 'BINANCE'
        ELSE 'OTHER'
    END AS exchange,
    COUNT(*) as record_count,
    MAX(\"Timestamp\") as most_recent_data,
    ROUND(EXTRACT(EPOCH FROM (NOW() - MAX(\"Timestamp\"))) / 60, 1) as minutes_stale
FROM market_data
GROUP BY exchange
ORDER BY exchange;
"
echo ""

# Check data freshness (CRITICAL)
echo "⏰ DATA FRESHNESS CHECK:"
echo "----------------------------------------------------------------------"
STALE_THRESHOLD=15  # minutes

STALE_DATA=$(docker exec $POSTGRES_CONTAINER psql -U $DB_USER -d $DB_NAME -t -c "
SELECT COUNT(DISTINCT \"Symbol\")
FROM (
    SELECT \"Symbol\", MAX(\"Timestamp\") as latest
    FROM market_data
    GROUP BY \"Symbol\"
) sub
WHERE EXTRACT(EPOCH FROM (NOW() - latest)) / 60 > $STALE_THRESHOLD;
")

STALE_DATA=$(echo $STALE_DATA | xargs)  # trim whitespace

if [ "$STALE_DATA" -eq "0" ]; then
    echo "✅ All symbols have fresh data (< $STALE_THRESHOLD minutes old)"
else
    echo "⚠️  WARNING: $STALE_DATA symbols have stale data (> $STALE_THRESHOLD minutes old)"
    echo ""
    echo "Stale Symbols:"
    docker exec $POSTGRES_CONTAINER psql -U $DB_USER -d $DB_NAME -c "
    SELECT
        \"Symbol\",
        MAX(\"Timestamp\") as latest_data,
        ROUND(EXTRACT(EPOCH FROM (NOW() - MAX(\"Timestamp\"))) / 60, 1) as minutes_stale
    FROM market_data
    GROUP BY \"Symbol\"
    HAVING EXTRACT(EPOCH FROM (NOW() - MAX(\"Timestamp\"))) / 60 > $STALE_THRESHOLD
    ORDER BY minutes_stale DESC;
    "
fi
echo ""

# Show sample of latest prices
echo "💰 LATEST PRICES (Top 10 Most Recent):"
echo "----------------------------------------------------------------------"
docker exec $POSTGRES_CONTAINER psql -U $DB_USER -d $DB_NAME -c "
SELECT
    \"Symbol\",
    \"Close\" as price,
    ROUND(\"Volume\"::numeric, 2) as volume,
    \"Timestamp\"
FROM market_data
ORDER BY \"Timestamp\" DESC
LIMIT 10;
"
echo ""

# Check symbols without any data
echo "🔍 SYMBOLS WITHOUT DATA:"
echo "----------------------------------------------------------------------"
NO_DATA=$(docker exec $POSTGRES_CONTAINER psql -U $DB_USER -d $DB_NAME -t -c "
SELECT COUNT(*)
FROM symbols s
WHERE s.is_active = true
  AND NOT EXISTS (
      SELECT 1 FROM market_data md WHERE md.\"Symbol\" = s.ticker
  );
")

NO_DATA=$(echo $NO_DATA | xargs)  # trim whitespace

if [ "$NO_DATA" -eq "0" ]; then
    echo "✅ All active symbols have market data"
else
    echo "⚠️  WARNING: $NO_DATA active symbols have NO market data"
    echo ""
    docker exec $POSTGRES_CONTAINER psql -U $DB_USER -d $DB_NAME -c "
    SELECT ticker, display, venue
    FROM symbols s
    WHERE s.is_active = true
      AND NOT EXISTS (
          SELECT 1 FROM market_data md WHERE md.\"Symbol\" = s.ticker
      )
    ORDER BY venue, ticker;
    "
fi
echo ""

# Overall health status
echo "======================================================================"
echo "OVERALL HEALTH STATUS:"
echo "----------------------------------------------------------------------"

TOTAL_RECORDS=$(docker exec $POSTGRES_CONTAINER psql -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM market_data;")
TOTAL_RECORDS=$(echo $TOTAL_RECORDS | xargs)

if [ "$TOTAL_RECORDS" -eq "0" ]; then
    echo "🔴 CRITICAL: market_data table is EMPTY"
    echo "   Action Required: Run populate_market_data.sql"
    exit 1
elif [ "$TOTAL_RECORDS" -lt "50" ]; then
    echo "⚠️  WARNING: Low data count ($TOTAL_RECORDS records)"
    echo "   Recommendation: Verify sync services are running"
elif [ "$STALE_DATA" -gt "0" ] || [ "$NO_DATA" -gt "0" ]; then
    echo "⚠️  DEGRADED: Some symbols have stale or missing data"
    echo "   Recommendation: Check sync service logs"
else
    echo "✅ HEALTHY: All systems nominal"
    echo "   - Total Records: $TOTAL_RECORDS"
    echo "   - All symbols have fresh data"
    echo "   - No gaps detected"
fi

echo "======================================================================"
echo ""

# Check if sync services are running (optional)
echo "🔧 SYNC SERVICE STATUS:"
echo "----------------------------------------------------------------------"
echo "Checking for recent sync activity in logs..."

RECENT_SYNC=$(docker logs mytrader_api 2>&1 | grep -i "5-minute sync completed" | tail -3)

if [ -z "$RECENT_SYNC" ]; then
    echo "⚠️  No recent sync activity found in logs"
else
    echo "Recent sync activity:"
    echo "$RECENT_SYNC" | while read -r line; do
        echo "  $line"
    done
fi

echo ""
echo "======================================================================"
echo "Monitoring script completed at $(date '+%Y-%m-%d %H:%M:%S %Z')"
echo "======================================================================"
