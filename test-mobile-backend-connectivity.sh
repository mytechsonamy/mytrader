#!/bin/bash

# Mobile Backend Connectivity Test
# Tests all critical endpoints mobile app needs to function

BASE_URL="http://192.168.68.102:8080"
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=========================================="
echo "  Mobile Backend Connectivity Test"
echo "=========================================="
echo ""
echo "Backend URL: $BASE_URL"
echo "Testing all critical mobile app endpoints..."
echo ""

# Test 1: Health Check
echo "1. Health Check Endpoint"
echo "   GET $BASE_URL/api/health"
HEALTH=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/health")
HTTP_CODE=$(echo "$HEALTH" | tail -n 1)
BODY=$(echo "$HEALTH" | head -n -1)

if [ "$HTTP_CODE" = "200" ]; then
    IS_HEALTHY=$(echo "$BODY" | grep -o '"isHealthy":true' | head -1)
    if [ -n "$IS_HEALTHY" ]; then
        echo -e "   ${GREEN}✓ PASS${NC} - Backend is healthy (HTTP $HTTP_CODE)"
    else
        echo -e "   ${RED}✗ FAIL${NC} - Backend returned 200 but not healthy"
    fi
else
    echo -e "   ${RED}✗ FAIL${NC} - HTTP $HTTP_CODE"
fi
echo ""

# Test 2: Authentication Endpoint
echo "2. Authentication Endpoint"
echo "   POST $BASE_URL/api/auth/login"
AUTH_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com","password":"test"}')
AUTH_CODE=$(echo "$AUTH_RESPONSE" | tail -n 1)

if [ "$AUTH_CODE" = "400" ] || [ "$AUTH_CODE" = "401" ]; then
    echo -e "   ${GREEN}✓ PASS${NC} - Endpoint reachable (HTTP $AUTH_CODE - auth error expected)"
elif [ "$AUTH_CODE" = "200" ]; then
    echo -e "   ${GREEN}✓ PASS${NC} - Endpoint reachable (HTTP $AUTH_CODE)"
else
    echo -e "   ${RED}✗ FAIL${NC} - HTTP $AUTH_CODE"
fi
echo ""

# Test 3: Symbols API - CRYPTO
echo "3. Symbols API - CRYPTO"
echo "   GET $BASE_URL/api/symbols?assetClass=CRYPTO"
CRYPTO_RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/symbols?assetClass=CRYPTO")
CRYPTO_CODE=$(echo "$CRYPTO_RESPONSE" | tail -n 1)
CRYPTO_BODY=$(echo "$CRYPTO_RESPONSE" | head -n -1)

if [ "$CRYPTO_CODE" = "200" ]; then
    CRYPTO_COUNT=$(echo "$CRYPTO_BODY" | grep -o '"symbol":"[^"]*USDT"' | wc -l | tr -d ' ')
    if [ "$CRYPTO_COUNT" -gt 0 ]; then
        echo -e "   ${GREEN}✓ PASS${NC} - Found $CRYPTO_COUNT crypto symbols (HTTP $CRYPTO_CODE)"
    else
        echo -e "   ${YELLOW}⚠ WARN${NC} - No crypto symbols returned"
    fi
else
    echo -e "   ${RED}✗ FAIL${NC} - HTTP $CRYPTO_CODE"
fi
echo ""

# Test 4: Symbols API - STOCK
echo "4. Symbols API - STOCK"
echo "   GET $BASE_URL/api/symbols?assetClass=STOCK"
STOCK_RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/symbols?assetClass=STOCK")
STOCK_CODE=$(echo "$STOCK_RESPONSE" | tail -n 1)
STOCK_BODY=$(echo "$STOCK_RESPONSE" | head -n -1)

if [ "$STOCK_CODE" = "200" ]; then
    # Count symbols that have NASDAQ, NYSE, or BIST as venue
    STOCK_COUNT=$(echo "$STOCK_BODY" | grep -o '"venue":"[^"]*"' | wc -l | tr -d ' ')
    if [ "$STOCK_COUNT" -gt 0 ]; then
        echo -e "   ${GREEN}✓ PASS${NC} - Found $STOCK_COUNT stock symbols (HTTP $STOCK_CODE)"
    else
        echo -e "   ${YELLOW}⚠ WARN${NC} - No stock symbols returned"
    fi
else
    echo -e "   ${RED}✗ FAIL${NC} - HTTP $STOCK_CODE"
fi
echo ""

# Test 5: CORS Configuration
echo "5. CORS Configuration"
echo "   OPTIONS $BASE_URL/api/symbols"
CORS_RESPONSE=$(curl -s -I -X OPTIONS "$BASE_URL/api/symbols" \
    -H "Origin: http://localhost" \
    -H "Access-Control-Request-Method: GET" \
    -H "Access-Control-Request-Headers: Content-Type")
CORS_ALLOW=$(echo "$CORS_RESPONSE" | grep -i "access-control-allow-origin")
CORS_METHODS=$(echo "$CORS_RESPONSE" | grep -i "access-control-allow-methods")

if [ -n "$CORS_ALLOW" ] && [ -n "$CORS_METHODS" ]; then
    echo -e "   ${GREEN}✓ PASS${NC} - CORS headers present"
else
    echo -e "   ${RED}✗ FAIL${NC} - CORS headers missing"
fi
echo ""

# Test 6: Network Connectivity
echo "6. Network Connectivity"
echo "   Testing TCP connection to 192.168.68.102:8080"
if nc -z -w 2 192.168.68.102 8080 2>/dev/null; then
    echo -e "   ${GREEN}✓ PASS${NC} - Port 8080 is reachable"
else
    echo -e "   ${RED}✗ FAIL${NC} - Cannot connect to port 8080"
fi
echo ""

# Test 7: SignalR Hub Negotiation
echo "7. SignalR Hub Negotiation"
echo "   POST $BASE_URL/hubs/market-data/negotiate"
HUB_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/hubs/market-data/negotiate" \
    -H "Content-Type: text/plain;charset=UTF-8")
HUB_CODE=$(echo "$HUB_RESPONSE" | tail -n 1)

if [ "$HUB_CODE" = "200" ]; then
    echo -e "   ${GREEN}✓ PASS${NC} - SignalR hub reachable (HTTP $HUB_CODE)"
elif [ "$HUB_CODE" = "400" ]; then
    echo -e "   ${GREEN}✓ PASS${NC} - Hub reachable (HTTP $HUB_CODE - negotiation format error expected)"
else
    echo -e "   ${RED}✗ FAIL${NC} - HTTP $HUB_CODE"
fi
echo ""

# Test 8: Dashboard Hub (Anonymous Access)
echo "8. Dashboard Hub (Anonymous)"
echo "   POST $BASE_URL/hubs/dashboard/negotiate"
DASH_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/hubs/dashboard/negotiate" \
    -H "Content-Type: text/plain;charset=UTF-8")
DASH_CODE=$(echo "$DASH_RESPONSE" | tail -n 1)

if [ "$DASH_CODE" = "200" ] || [ "$DASH_CODE" = "400" ]; then
    echo -e "   ${GREEN}✓ PASS${NC} - Dashboard hub reachable (HTTP $DASH_CODE)"
else
    echo -e "   ${RED}✗ FAIL${NC} - HTTP $DASH_CODE"
fi
echo ""

# Summary
echo "=========================================="
echo "  Test Summary"
echo "=========================================="
echo ""
echo "Configuration:"
echo "  Backend IP:   192.168.68.102"
echo "  Backend Port: 8080"
echo "  Mobile Config: frontend/mobile/app.json"
echo ""
echo "Next Steps:"
echo "  1. Restart mobile app (npm start -- --clear)"
echo "  2. Test login functionality"
echo "  3. Verify real-time price updates"
echo "  4. Check WebSocket connection in app logs"
echo ""
