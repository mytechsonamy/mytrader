#!/bin/bash

echo "========================================="
echo "BACKEND API EXCHANGE FILTERING TEST"
echo "========================================="
echo ""

# Test 1: All symbols (no filter)
echo "Test 1: All Symbols (No Filter)"
echo "================================"
ALL_SYMBOLS=$(curl -s "http://localhost:8080/api/symbols" | jq '.symbols | length')
echo "Result: $ALL_SYMBOLS symbols"
echo "Expected: 19 symbols"
if [ "$ALL_SYMBOLS" -eq 19 ]; then
    echo "✅ PASS"
else
    echo "❌ FAIL"
fi
echo ""

# Test 2: BIST symbols
echo "Test 2: BIST Exchange Filter"
echo "============================="
BIST_RESPONSE=$(curl -s "http://localhost:8080/api/symbols?exchange=BIST")
BIST_COUNT=$(echo "$BIST_RESPONSE" | jq '.symbols | length')
echo "Result: $BIST_COUNT symbols"
echo "Expected: 3 symbols (THYAO, GARAN, SISE)"
if [ "$BIST_COUNT" -eq 3 ]; then
    echo "✅ PASS"
else
    echo "❌ FAIL - Got $BIST_COUNT instead of 3"
    echo "Symbols returned:"
    echo "$BIST_RESPONSE" | jq '.symbols | keys'
fi
echo ""

# Test 3: NASDAQ symbols
echo "Test 3: NASDAQ Exchange Filter"
echo "=============================="
NASDAQ_COUNT=$(curl -s "http://localhost:8080/api/symbols?exchange=NASDAQ" | jq '.symbols | length')
echo "Result: $NASDAQ_COUNT symbols"
echo "Expected: 5 symbols (AAPL, MSFT, GOOGL, NVDA, TSLA)"
if [ "$NASDAQ_COUNT" -eq 5 ]; then
    echo "✅ PASS"
else
    echo "❌ FAIL - Got $NASDAQ_COUNT instead of 5"
fi
echo ""

# Test 4: NYSE symbols
echo "Test 4: NYSE Exchange Filter"
echo "============================"
NYSE_COUNT=$(curl -s "http://localhost:8080/api/symbols?exchange=NYSE" | jq '.symbols | length')
echo "Result: $NYSE_COUNT symbols"
echo "Expected: 2 symbols (JPM, BA)"
if [ "$NYSE_COUNT" -eq 2 ]; then
    echo "✅ PASS"
else
    echo "❌ FAIL - Got $NYSE_COUNT instead of 2"
fi
echo ""

# Test 5: BINANCE symbols
echo "Test 5: BINANCE Exchange Filter"
echo "==============================="
BINANCE_COUNT=$(curl -s "http://localhost:8080/api/symbols?exchange=BINANCE" | jq '.symbols | length')
echo "Result: $BINANCE_COUNT symbols"
echo "Expected: 9 crypto symbols"
if [ "$BINANCE_COUNT" -eq 9 ]; then
    echo "✅ PASS"
else
    echo "❌ FAIL - Got $BINANCE_COUNT instead of 9"
fi
echo ""

# Test 6: Field verification for BIST
echo "Test 6: Field Verification (BIST)"
echo "================================="
BIST_FIELDS=$(curl -s "http://localhost:8080/api/symbols?exchange=BIST" | jq '.symbols | to_entries | first | .value')
echo "First symbol fields:"
echo "$BIST_FIELDS"
HAS_VENUE=$(echo "$BIST_FIELDS" | jq 'has("venue")')
HAS_MARKET=$(echo "$BIST_FIELDS" | jq 'has("market")')
HAS_MARKETNAME=$(echo "$BIST_FIELDS" | jq 'has("marketName")')
echo ""
echo "Has 'venue' field: $HAS_VENUE"
echo "Has 'market' field: $HAS_MARKET"
echo "Has 'marketName' field: $HAS_MARKETNAME"
if [ "$HAS_VENUE" = "true" ] && [ "$HAS_MARKET" = "true" ] && [ "$HAS_MARKETNAME" = "true" ]; then
    echo "✅ PASS - All required fields present"
else
    echo "❌ FAIL - Missing required fields"
fi
echo ""

echo "========================================="
echo "TEST SUMMARY"
echo "========================================="
