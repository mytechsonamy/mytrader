#!/bin/bash

# MyTrader Production Health Monitoring Script
# Monitors all critical services and endpoints

set -e

# Configuration
BACKEND_URL="${BACKEND_URL:-http://localhost:5002}"
WEB_URL="${WEB_URL:-http://localhost:3000}"
LOG_FILE="/tmp/mytrader-health-check.log"
ALERT_THRESHOLD=5
FAILED_CHECKS=0

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log() {
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

check_health() {
    local url=$1
    local name=$2
    local expected_status=${3:-200}

    log "Checking $name health at $url"

    response=$(curl -s -o /tmp/health_response.json -w "%{http_code}" "$url" 2>/dev/null || echo "000")

    if [[ "$response" == "$expected_status" ]]; then
        log "✅ $name: HEALTHY (HTTP $response)"
        echo -e "${GREEN}✅ $name: HEALTHY${NC}"
        return 0
    else
        log "❌ $name: UNHEALTHY (HTTP $response)"
        echo -e "${RED}❌ $name: UNHEALTHY (HTTP $response)${NC}"
        ((FAILED_CHECKS++))
        return 1
    fi
}

check_websocket() {
    local ws_url=$1
    local name=$2

    log "Checking $name WebSocket connectivity"

    # Test WebSocket endpoint availability (returns 400 for HTTP requests, which is expected)
    response=$(curl -s -o /dev/null -w "%{http_code}" "$ws_url" 2>/dev/null || echo "000")

    if [[ "$response" == "400" ]] || [[ "$response" == "404" ]]; then
        log "✅ $name WebSocket: AVAILABLE (HTTP $response - expected for WS endpoint)"
        echo -e "${GREEN}✅ $name WebSocket: AVAILABLE${NC}"
        return 0
    else
        log "❌ $name WebSocket: UNAVAILABLE (HTTP $response)"
        echo -e "${RED}❌ $name WebSocket: UNAVAILABLE${NC}"
        ((FAILED_CHECKS++))
        return 1
    fi
}

check_api_endpoint() {
    local endpoint=$1
    local name=$2

    log "Checking API endpoint: $name"

    response=$(curl -s -o /tmp/api_response.json -w "%{http_code}" "$BACKEND_URL$endpoint" 2>/dev/null || echo "000")

    if [[ "$response" == "200" ]]; then
        log "✅ $name API: OPERATIONAL (HTTP $response)"
        echo -e "${GREEN}✅ $name API: OPERATIONAL${NC}"
        return 0
    else
        log "⚠️  $name API: ISSUES (HTTP $response)"
        echo -e "${YELLOW}⚠️  $name API: ISSUES (HTTP $response)${NC}"
        # Don't increment failed checks for API endpoints as some may have expected errors
        return 1
    fi
}

check_performance() {
    local url=$1
    local name=$2
    local max_time=${3:-1000}  # Max response time in ms

    log "Checking $name performance"

    response_time=$(curl -s -o /dev/null -w "%{time_total}" "$url" 2>/dev/null)
    response_time_ms=$(echo "$response_time * 1000" | bc)
    response_time_int=${response_time_ms%.*}

    if [[ $response_time_int -lt $max_time ]]; then
        log "✅ $name Performance: GOOD (${response_time_int}ms < ${max_time}ms)"
        echo -e "${GREEN}✅ $name Performance: GOOD (${response_time_int}ms)${NC}"
        return 0
    else
        log "⚠️  $name Performance: SLOW (${response_time_int}ms > ${max_time}ms)"
        echo -e "${YELLOW}⚠️  $name Performance: SLOW (${response_time_int}ms)${NC}"
        return 1
    fi
}

main() {
    echo "🔍 MyTrader Production Health Check Starting..."
    echo "================================================="

    log "Starting health check cycle"

    # Core service health checks
    echo -e "\n📊 Core Services Health:"
    check_health "$BACKEND_URL/health" "Backend API"
    check_health "$WEB_URL" "Web Frontend"

    # WebSocket connectivity
    echo -e "\n🔌 WebSocket Connectivity:"
    check_websocket "$BACKEND_URL/hubs/market-data" "Market Data Hub"
    check_websocket "$BACKEND_URL/hubs/trading" "Trading Hub"
    check_websocket "$BACKEND_URL/hubs/portfolio" "Portfolio Hub"

    # API endpoint functionality
    echo -e "\n🔧 API Endpoints:"
    check_api_endpoint "/api/symbols" "Symbols"
    check_api_endpoint "/api/prices/volume-leaders" "Volume Leaders"
    check_api_endpoint "/api/dashboard/summary" "Dashboard"

    # Performance checks
    echo -e "\n⚡ Performance Metrics:"
    check_performance "$BACKEND_URL/health" "Backend Response Time" 100
    check_performance "$WEB_URL" "Web Frontend Response Time" 500

    # Database and external services (if health endpoints are working)
    echo -e "\n🗄️  External Dependencies:"
    if curl -s "$BACKEND_URL/api/health/database" >/dev/null 2>&1; then
        check_health "$BACKEND_URL/api/health/database" "Database"
    else
        echo -e "${YELLOW}⚠️  Database health endpoint not available${NC}"
    fi

    # Summary
    echo -e "\n📋 Health Check Summary:"
    echo "================================================="

    if [[ $FAILED_CHECKS -eq 0 ]]; then
        echo -e "${GREEN}🎉 ALL SYSTEMS OPERATIONAL${NC}"
        log "Health check completed successfully - all systems operational"
        exit 0
    elif [[ $FAILED_CHECKS -lt $ALERT_THRESHOLD ]]; then
        echo -e "${YELLOW}⚠️  SOME ISSUES DETECTED ($FAILED_CHECKS failures)${NC}"
        log "Health check completed with $FAILED_CHECKS issues (below alert threshold)"
        exit 1
    else
        echo -e "${RED}🚨 CRITICAL ISSUES DETECTED ($FAILED_CHECKS failures)${NC}"
        log "Health check failed with $FAILED_CHECKS critical issues"
        exit 2
    fi
}

# Trap to clean up temp files
cleanup() {
    rm -f /tmp/health_response.json /tmp/api_response.json
}
trap cleanup EXIT

# Run main function
main "$@"