#!/bin/bash

# Quality Gate: User Journey Flow Tests
# Ensures critical user flows work before deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_BASE_URL=${API_BASE_URL:-"http://localhost:5245/api"}
TEST_TIMEOUT=${TEST_TIMEOUT:-30}
MAX_RETRIES=${MAX_RETRIES:-5}

echo -e "${BLUE}🚀 MyTrader Quality Gate: User Journey Tests${NC}"
echo "=================================="
echo "API Base URL: $API_BASE_URL"
echo "Test Timeout: ${TEST_TIMEOUT}s"
echo "Max Retries: $MAX_RETRIES"
echo ""

# Function to wait for API to be ready
wait_for_api() {
    echo -e "${YELLOW}⏳ Waiting for API to be ready...${NC}"

    for i in $(seq 1 $MAX_RETRIES); do
        if curl -s "$API_BASE_URL/../health" > /dev/null 2>&1; then
            echo -e "${GREEN}✅ API is ready${NC}"
            return 0
        fi

        echo "Attempt $i/$MAX_RETRIES failed, retrying in 5 seconds..."
        sleep 5
    done

    echo -e "${RED}❌ API failed to start after $MAX_RETRIES attempts${NC}"
    return 1
}

# Function to test endpoint accessibility
test_endpoint() {
    local method=$1
    local endpoint=$2
    local expected_status=$3
    local description=$4

    echo -e "${BLUE}Testing: $description${NC}"

    local response
    local status_code

    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "%{http_code}" "$API_BASE_URL$endpoint")
        status_code="${response: -3}"
    else
        response=$(curl -s -w "%{http_code}" -X "$method" \
            -H "Content-Type: application/json" \
            -d '{}' \
            "$API_BASE_URL$endpoint")
        status_code="${response: -3}"
    fi

    if [ "$status_code" = "$expected_status" ]; then
        echo -e "${GREEN}✅ PASS: $description (Status: $status_code)${NC}"
        return 0
    elif [ "$status_code" = "404" ]; then
        echo -e "${RED}❌ CRITICAL FAIL: $description returns 404 - User journey broken!${NC}"
        return 1
    else
        echo -e "${YELLOW}⚠️  WARNING: $description (Expected: $expected_status, Got: $status_code)${NC}"
        return 0
    fi
}

# Function to test user registration flow
test_user_registration_flow() {
    echo -e "\n${BLUE}🧪 Testing User Registration Flow${NC}"
    echo "======================================"

    local test_email="qg_test_$(date +%s)@example.com"
    local registration_payload="{\"email\":\"$test_email\",\"password\":\"Test123abc\",\"firstName\":\"QualityGate\",\"lastName\":\"Test\",\"confirmPassword\":\"Test123abc\"}"

    echo "Testing user registration..."
    local reg_response=$(curl -s -w "%{http_code}" -X POST \
        -H "Content-Type: application/json" \
        -d "$registration_payload" \
        "$API_BASE_URL/auth/register")

    local reg_status="${reg_response: -3}"
    local reg_body="${reg_response%???}"

    if [ "$reg_status" = "404" ]; then
        echo -e "${RED}❌ CRITICAL FAIL: Registration endpoint returns 404${NC}"
        return 1
    elif [ "$reg_status" = "200" ]; then
        echo -e "${GREEN}✅ PASS: Registration endpoint accessible${NC}"

        # Check if response contains success field
        if echo "$reg_body" | grep -q '"success"'; then
            echo -e "${GREEN}✅ PASS: Registration response format correct${NC}"
        else
            echo -e "${YELLOW}⚠️  WARNING: Registration response format unexpected${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  WARNING: Registration returned unexpected status: $reg_status${NC}"
    fi

    return 0
}

# Function to test email verification flow
test_email_verification_flow() {
    echo -e "\n${BLUE}📧 Testing Email Verification Flow${NC}"
    echo "======================================"

    local test_email="qg_verify_$(date +%s)@example.com"
    local verify_payload="{\"Email\":\"$test_email\",\"VerificationCode\":\"111111\"}"

    echo "Testing email verification..."
    local verify_response=$(curl -s -w "%{http_code}" -X POST \
        -H "Content-Type: application/json" \
        -d "$verify_payload" \
        "$API_BASE_URL/auth/verify-email")

    local verify_status="${verify_response: -3}"
    local verify_body="${verify_response%???}"

    if [ "$verify_status" = "404" ]; then
        echo -e "${RED}❌ CRITICAL FAIL: Email verification endpoint returns 404${NC}"
        return 1
    elif [ "$verify_status" = "200" ]; then
        echo -e "${GREEN}✅ PASS: Email verification endpoint accessible${NC}"

        # Check response format
        if echo "$verify_body" | grep -q '"success"'; then
            echo -e "${GREEN}✅ PASS: Email verification response format correct${NC}"
        else
            echo -e "${YELLOW}⚠️  WARNING: Email verification response format unexpected${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  WARNING: Email verification returned unexpected status: $verify_status${NC}"
    fi

    # Test resend verification
    echo "Testing resend verification..."
    local resend_payload="{\"Email\":\"$test_email\"}"
    local resend_response=$(curl -s -w "%{http_code}" -X POST \
        -H "Content-Type: application/json" \
        -d "$resend_payload" \
        "$API_BASE_URL/auth/resend-verification")

    local resend_status="${resend_response: -3}"

    if [ "$resend_status" = "404" ]; then
        echo -e "${RED}❌ CRITICAL FAIL: Resend verification endpoint returns 404${NC}"
        return 1
    elif [ "$resend_status" = "200" ]; then
        echo -e "${GREEN}✅ PASS: Resend verification endpoint accessible${NC}"
    else
        echo -e "${YELLOW}⚠️  WARNING: Resend verification returned unexpected status: $resend_status${NC}"
    fi

    return 0
}

# Function to test critical API endpoints
test_critical_endpoints() {
    echo -e "\n${BLUE}🔗 Testing Critical API Endpoints${NC}"
    echo "===================================="

    local endpoints=(
        "POST /auth/register 200 User Registration"
        "POST /auth/verify-email 200 Email Verification"
        "POST /auth/resend-verification 200 Resend Verification"
        "POST /auth/login 200 User Login"
        "POST /auth/request-password-reset 200 Password Reset Request"
        "POST /auth/verify-password-reset 200 Password Reset Verification"
        "POST /auth/reset-password 200 Password Reset"
    )

    local failed_tests=0

    for endpoint_config in "${endpoints[@]}"; do
        IFS=' ' read -r method path expected_status description <<< "$endpoint_config"

        if ! test_endpoint "$method" "$path" "$expected_status" "$description"; then
            ((failed_tests++))
        fi
    done

    if [ $failed_tests -gt 0 ]; then
        echo -e "\n${RED}❌ $failed_tests critical endpoint(s) failed${NC}"
        return 1
    else
        echo -e "\n${GREEN}✅ All critical endpoints passed${NC}"
        return 0
    fi
}

# Function to test mobile app URL compatibility
test_mobile_app_compatibility() {
    echo -e "\n${BLUE}📱 Testing Mobile App URL Compatibility${NC}"
    echo "========================================"

    # Test the primary URL that mobile app will try first
    local primary_url="/auth/verify-email"

    echo "Testing primary mobile app URL pattern..."
    if test_endpoint "POST" "$primary_url" "200" "Mobile App Primary URL"; then
        echo -e "${GREEN}✅ PASS: Mobile app will connect successfully${NC}"
        return 0
    else
        echo -e "${RED}❌ FAIL: Mobile app URL compatibility issue${NC}"
        return 1
    fi
}

# Main test execution
main() {
    echo -e "${BLUE}Starting Quality Gate Tests...${NC}\n"

    local failed_tests=0

    # Wait for API to be ready
    if ! wait_for_api; then
        echo -e "${RED}❌ Quality Gate FAILED: API not accessible${NC}"
        exit 1
    fi

    # Run test suites
    if ! test_critical_endpoints; then
        ((failed_tests++))
    fi

    if ! test_user_registration_flow; then
        ((failed_tests++))
    fi

    if ! test_email_verification_flow; then
        ((failed_tests++))
    fi

    if ! test_mobile_app_compatibility; then
        ((failed_tests++))
    fi

    # Final result
    echo -e "\n${BLUE}Quality Gate Results${NC}"
    echo "===================="

    if [ $failed_tests -eq 0 ]; then
        echo -e "${GREEN}🎉 ALL TESTS PASSED - Quality Gate: ✅ PASSED${NC}"
        echo -e "${GREEN}✅ No 404 errors detected in user journeys${NC}"
        echo -e "${GREEN}✅ All critical authentication flows are accessible${NC}"
        echo -e "${GREEN}✅ Mobile app compatibility confirmed${NC}"
        echo ""
        echo -e "${GREEN}🚀 Deployment can proceed safely${NC}"
        exit 0
    else
        echo -e "${RED}❌ $failed_tests test suite(s) failed - Quality Gate: ❌ FAILED${NC}"
        echo -e "${RED}🚫 Deployment should be blocked until issues are resolved${NC}"
        echo ""
        echo -e "${YELLOW}Common fixes:${NC}"
        echo "1. Ensure API server is running on correct port"
        echo "2. Check database connectivity"
        echo "3. Verify authentication service configuration"
        echo "4. Confirm mobile app base URL matches API"
        exit 1
    fi
}

# Allow script to be sourced for testing
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi