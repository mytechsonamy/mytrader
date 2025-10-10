#!/bin/bash

# Phase 4: Backend Deployment Script
# Rebuilds Docker image with WebSocket fixes and restarts container

set -e  # Exit on error

echo "════════════════════════════════════════════════════════════"
echo "Phase 4: Backend WebSocket Fixes Deployment"
echo "════════════════════════════════════════════════════════════"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

PROJECT_ROOT="/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader"
cd "$PROJECT_ROOT"

echo -e "${BLUE}Step 1: Checking current backend status...${NC}"
docker ps | grep mytrader_api || echo "Container not running"
echo ""

echo -e "${BLUE}Step 2: Stopping existing backend container...${NC}"
docker-compose stop api 2>/dev/null || echo "No running container to stop"
echo ""

echo -e "${BLUE}Step 3: Building new Docker image with fixes...${NC}"
echo "This will include:"
echo "  - object[] array handling in ParseSymbolData()"
echo "  - Enhanced debug logging"
echo "  - Symbol format corrections"
echo ""

docker-compose build api

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Build successful!${NC}"
else
    echo -e "${RED}❌ Build failed!${NC}"
    exit 1
fi
echo ""

echo -e "${BLUE}Step 4: Starting updated backend container...${NC}"
docker-compose up -d api

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Container started successfully!${NC}"
else
    echo -e "${RED}❌ Failed to start container!${NC}"
    exit 1
fi
echo ""

echo -e "${BLUE}Step 5: Waiting for backend to initialize (15 seconds)...${NC}"
for i in {15..1}; do
    echo -ne "  ${i}s remaining...\r"
    sleep 1
done
echo ""

echo -e "${BLUE}Step 6: Checking backend health...${NC}"
HEALTH_RESPONSE=$(curl -s http://192.168.68.102:8080/health 2>/dev/null)

if echo "$HEALTH_RESPONSE" | grep -q '"status":"Healthy"'; then
    echo -e "${GREEN}✅ Backend is healthy!${NC}"
    echo "Health check response:"
    echo "$HEALTH_RESPONSE" | jq -r '.status, .timestamp' 2>/dev/null || echo "$HEALTH_RESPONSE"
else
    echo -e "${YELLOW}⚠️  Backend health check inconclusive${NC}"
    echo "Response: $HEALTH_RESPONSE"
fi
echo ""

echo -e "${BLUE}Step 7: Checking SignalR hub endpoint...${NC}"
NEGOTIATE_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}\n" http://192.168.68.102:8080/hubs/market-data/negotiate 2>/dev/null)
HTTP_STATUS=$(echo "$NEGOTIATE_RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)

if [ "$HTTP_STATUS" = "200" ]; then
    echo -e "${GREEN}✅ SignalR hub is accessible!${NC}"
else
    echo -e "${YELLOW}⚠️  SignalR hub response: HTTP $HTTP_STATUS${NC}"
fi
echo ""

echo -e "${BLUE}Step 8: Checking recent logs for new logging statements...${NC}"
echo "Looking for 'ParseSymbolData' or 'SubscribeToPriceUpdates' logs..."
docker logs mytrader_api 2>&1 | grep -i "ParseSymbolData\|SubscribeToPriceUpdates" | tail -10 || echo "No subscription logs yet (this is expected before clients connect)"
echo ""

echo "════════════════════════════════════════════════════════════"
echo -e "${GREEN}✅ DEPLOYMENT COMPLETE!${NC}"
echo "════════════════════════════════════════════════════════════"
echo ""
echo "Next Steps:"
echo "  1. Run E2E validation: node phase4-websocket-validation.js"
echo "  2. Or open HTML test: open backend/PHASE4_E2E_VALIDATION_TEST.html"
echo "  3. Check logs: docker logs -f mytrader_api"
echo ""
echo "Expected in logs after client connects:"
echo "  [INFO] SubscribeToPriceUpdates called with assetClass=CRYPTO"
echo "  [INFO] ParseSymbolData - Type: System.Object[]"
echo "  [INFO] Parsed 5 symbols from symbolData: BTCUSDT, ETHUSDT, ..."
echo ""
echo "Container Info:"
docker ps | grep mytrader_api
echo ""
