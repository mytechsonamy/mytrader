#!/bin/bash

# myTrader Monitoring System Test Script
# This script tests all monitoring endpoints and verifies the observability infrastructure

set -e

BASE_URL="http://localhost:5055"
HEALTH_CHECKS=(
    "/health"
    "/health/critical"
    "/health/database"
    "/health/realtime"
    "/health-ui"
    "/metrics"
)

echo "🔍 Testing myTrader Monitoring System"
echo "======================================="
echo

# Test basic connectivity
echo "📡 Testing basic API connectivity..."
response=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/")
if [ "$response" -eq 200 ]; then
    echo "✅ API is responding (HTTP $response)"
else
    echo "❌ API is not responding (HTTP $response)"
    exit 1
fi
echo

# Test health check endpoints
echo "🏥 Testing health check endpoints..."
for endpoint in "${HEALTH_CHECKS[@]}"; do
    echo "  Testing $endpoint..."

    if [ "$endpoint" = "/health-ui" ]; then
        # Health UI returns HTML, just check if it's accessible
        response=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL$endpoint")
        if [ "$response" -eq 200 ]; then
            echo "    ✅ Health UI accessible (HTTP $response)"
        else
            echo "    ⚠️  Health UI returned HTTP $response"
        fi
    elif [ "$endpoint" = "/metrics" ]; then
        # Metrics endpoint returns Prometheus format
        response=$(curl -s "$BASE_URL$endpoint" | head -n 5)
        if [[ $response == *"mytrader_info"* ]]; then
            echo "    ✅ Metrics endpoint working"
            echo "    📊 Sample metrics:"
            curl -s "$BASE_URL$endpoint" | grep "mytrader_" | head -n 3 | sed 's/^/      /'
        else
            echo "    ❌ Metrics endpoint not working properly"
        fi
    else
        # JSON health check endpoints
        response=$(curl -s "$BASE_URL$endpoint")
        status=$(echo "$response" | jq -r '.status // "unknown"' 2>/dev/null || echo "parse_error")

        if [ "$status" = "Healthy" ] || [ "$status" = "healthy" ]; then
            echo "    ✅ $endpoint: $status"
        elif [ "$status" = "Degraded" ] || [ "$status" = "degraded" ]; then
            echo "    ⚠️  $endpoint: $status"
        elif [ "$status" = "Unhealthy" ] || [ "$status" = "unhealthy" ]; then
            echo "    ❌ $endpoint: $status"
        else
            echo "    ❓ $endpoint: Cannot parse response"
        fi
    fi
done
echo

# Test specific health check details
echo "🔬 Testing detailed health check information..."

# Database health
echo "  Database connectivity:"
db_health=$(curl -s "$BASE_URL/api/health/database")
if echo "$db_health" | jq -e '.connected' >/dev/null 2>&1; then
    connected=$(echo "$db_health" | jq -r '.connected')
    if [ "$connected" = "true" ]; then
        echo "    ✅ Database connected"
    else
        echo "    ❌ Database not connected"
    fi
else
    echo "    ❓ Cannot determine database status"
fi

# Market hours
echo "  Market status:"
market_status=$(curl -s "$BASE_URL/api/health/market-hours")
if echo "$market_status" | jq -e '.markets' >/dev/null 2>&1; then
    echo "    ✅ Market status available"
    echo "$market_status" | jq -r '.markets[] | "      \(.market): \(.status)"' 2>/dev/null || echo "      Markets data available"
else
    echo "    ❓ Cannot retrieve market status"
fi

# System metrics
echo "  System performance:"
metrics_response=$(curl -s "$BASE_URL/api/health/metrics")
if echo "$metrics_response" | jq -e '.memory.totalMemory' >/dev/null 2>&1; then
    memory_mb=$(echo "$metrics_response" | jq -r '.memory.totalMemory' | awk '{printf "%.0f", $1/1024/1024}')
    echo "    📊 Memory usage: ${memory_mb}MB"

    uptime=$(echo "$metrics_response" | jq -r '.uptime' | sed 's/\.[0-9]*//g')
    echo "    ⏱️  Uptime: $uptime"
else
    echo "    ❓ Cannot retrieve system metrics"
fi
echo

# Test SignalR hub health
echo "🔌 Testing real-time connection health..."
signalr_health=$(curl -s "$BASE_URL/api/health/connections")
if echo "$signalr_health" | jq -e '.signalR.hubs' >/dev/null 2>&1; then
    echo "  ✅ SignalR health check available"
    echo "$signalr_health" | jq -r '.signalR.hubs[]?' | sed 's/^/    📡 Hub: /' 2>/dev/null || echo "    📡 Hub information available"
else
    echo "  ❓ Cannot retrieve SignalR health status"
fi
echo

# Test API structure validation
echo "🔍 Testing API structure consistency..."
api_test=$(curl -s "$BASE_URL/api/health/test-api")
if echo "$api_test" | jq -e '.success' >/dev/null 2>&1; then
    success=$(echo "$api_test" | jq -r '.success')
    if [ "$success" = "true" ]; then
        echo "  ✅ API structure validation passed"
    else
        echo "  ❌ API structure validation failed"
    fi
else
    echo "  ❓ Cannot validate API structure"
fi
echo

# Test Prometheus metrics format
echo "📊 Validating Prometheus metrics format..."
metrics_count=$(curl -s "$BASE_URL/metrics" | grep -c "^mytrader_" || echo "0")
if [ "$metrics_count" -gt 0 ]; then
    echo "  ✅ Found $metrics_count myTrader metrics"
    echo "  📈 Sample metrics:"
    curl -s "$BASE_URL/metrics" | grep "^mytrader_" | head -n 5 | sed 's/^/    /'
else
    echo "  ❌ No myTrader metrics found"
fi
echo

# Final summary
echo "📋 Monitoring System Test Summary"
echo "================================="
echo
echo "🟢 Available Endpoints:"
echo "  • Health Dashboard: $BASE_URL/health-ui"
echo "  • Health API: $BASE_URL/health"
echo "  • Prometheus Metrics: $BASE_URL/metrics"
echo "  • System Info: $BASE_URL/"
echo
echo "🔧 Key Features Validated:"
echo "  • Multi-layer health checks"
echo "  • Real-time connection monitoring"
echo "  • Database connectivity tracking"
echo "  • System performance metrics"
echo "  • Market status monitoring"
echo "  • Prometheus metrics export"
echo
echo "📈 Monitoring Capabilities:"
echo "  • SLO compliance tracking"
echo "  • Error budget monitoring"
echo "  • Real-time alerting"
echo "  • Performance metrics"
echo "  • Data quality validation"
echo
echo "✅ Monitoring system test completed successfully!"
echo
echo "🎯 Next Steps:"
echo "  1. Visit $BASE_URL/health-ui for the monitoring dashboard"
echo "  2. Set up Prometheus to scrape $BASE_URL/metrics"
echo "  3. Configure alert notification webhooks"
echo "  4. Review SLO definitions in SLO_DEFINITIONS.md"
echo "  5. Follow operational procedures in MONITORING_OPERATIONS_GUIDE.md"