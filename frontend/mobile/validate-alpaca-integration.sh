#!/bin/bash

# Alpaca Integration Validation Script
# This script validates the mobile implementation

set -e

echo "================================================"
echo "Alpaca Integration - Mobile Validation Script"
echo "================================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track results
PASS=0
FAIL=0

# Function to check file exists
check_file() {
    local file=$1
    local description=$2

    if [ -f "$file" ]; then
        echo -e "${GREEN}✓${NC} $description"
        ((PASS++))
        return 0
    else
        echo -e "${RED}✗${NC} $description - File not found: $file"
        ((FAIL++))
        return 1
    fi
}

# Function to check content in file
check_content() {
    local file=$1
    local pattern=$2
    local description=$3

    if [ ! -f "$file" ]; then
        echo -e "${RED}✗${NC} $description - File not found: $file"
        ((FAIL++))
        return 1
    fi

    if grep -q "$pattern" "$file"; then
        echo -e "${GREEN}✓${NC} $description"
        ((PASS++))
        return 0
    else
        echo -e "${RED}✗${NC} $description - Pattern not found: $pattern"
        ((FAIL++))
        return 1
    fi
}

echo "1. Checking File Structure..."
echo "------------------------------"

check_file "src/types/index.ts" "Types file exists"
check_file "src/components/dashboard/DataSourceIndicator.tsx" "DataSourceIndicator component exists"
check_file "src/context/PriceContext.tsx" "PriceContext exists"
check_file "src/components/dashboard/AssetCard.tsx" "AssetCard exists"
check_file "src/components/dashboard/index.ts" "Dashboard index exists"

echo ""
echo "2. Checking Type Definitions..."
echo "--------------------------------"

check_content "src/types/index.ts" "source\?" "Type: source field is optional"
check_content "src/types/index.ts" "qualityScore\?" "Type: qualityScore field is optional"
check_content "src/types/index.ts" "isRealtime\?" "Type: isRealtime field is optional"
check_content "src/types/index.ts" "ALPACA" "Type: ALPACA source type defined"
check_content "src/types/index.ts" "YAHOO_FALLBACK" "Type: YAHOO_FALLBACK source type defined"
check_content "src/types/index.ts" "YAHOO_REALTIME" "Type: YAHOO_REALTIME source type defined"

echo ""
echo "3. Checking DataSourceIndicator Component..."
echo "----------------------------------------------"

check_content "src/components/dashboard/DataSourceIndicator.tsx" "DataSourceIndicatorProps" "Component: Props interface defined"
check_content "src/components/dashboard/DataSourceIndicator.tsx" "getDotColor" "Component: getDotColor function exists"
check_content "src/components/dashboard/DataSourceIndicator.tsx" "getLabelText" "Component: getLabelText function exists"
check_content "src/components/dashboard/DataSourceIndicator.tsx" "#10b981" "Component: Green color defined"
check_content "src/components/dashboard/DataSourceIndicator.tsx" "#f59e0b" "Component: Yellow color defined"
check_content "src/components/dashboard/DataSourceIndicator.tsx" "memo" "Component: Uses React.memo"

echo ""
echo "4. Checking PriceContext Integration..."
echo "----------------------------------------"

check_content "src/context/PriceContext.tsx" "source:" "PriceContext: Maps source field"
check_content "src/context/PriceContext.tsx" "qualityScore:" "PriceContext: Maps qualityScore field"
check_content "src/context/PriceContext.tsx" "isRealtime:" "PriceContext: Maps isRealtime field"

echo ""
echo "5. Checking AssetCard Integration..."
echo "-------------------------------------"

check_content "src/components/dashboard/AssetCard.tsx" "import DataSourceIndicator" "AssetCard: Imports DataSourceIndicator"
check_content "src/components/dashboard/AssetCard.tsx" "DataSourceIndicator" "AssetCard: Uses DataSourceIndicator component"
check_content "src/components/dashboard/AssetCard.tsx" "compactPriceRow" "AssetCard: Has compact price row style"
check_content "src/components/dashboard/AssetCard.tsx" "priceRow" "AssetCard: Has full price row style"

echo ""
echo "6. Checking Exports..."
echo "-----------------------"

check_content "src/components/dashboard/index.ts" "DataSourceIndicator" "Exports: DataSourceIndicator exported"

echo ""
echo "7. Checking Documentation..."
echo "-----------------------------"

check_file "ALPACA_INTEGRATION_MOBILE_IMPLEMENTATION.md" "Implementation documentation exists"
check_file "ALPACA_TESTING_GUIDE.md" "Testing guide exists"
check_file "../../ALPACA_MOBILE_INTEGRATION_SUMMARY.md" "Summary document exists"
check_file "../../ALPACA_MOBILE_VISUAL_GUIDE.md" "Visual guide exists"

echo ""
echo "================================================"
echo "Validation Results"
echo "================================================"
echo -e "Passed: ${GREEN}$PASS${NC}"
echo -e "Failed: ${RED}$FAIL${NC}"
echo ""

if [ $FAIL -eq 0 ]; then
    echo -e "${GREEN}✓ All validation checks passed!${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Test on iOS simulator/device"
    echo "2. Test on Android emulator/device"
    echo "3. Verify data source indicators appear"
    echo "4. Check console logs for data flow"
    echo "5. Review ALPACA_TESTING_GUIDE.md for detailed tests"
    exit 0
else
    echo -e "${RED}✗ Some validation checks failed!${NC}"
    echo ""
    echo "Please review the failures above and fix them."
    echo "Then run this script again."
    exit 1
fi
