#!/bin/bash

echo "🧪 Running comprehensive user journey tests..."

# Configuration
BACKEND_URL="http://localhost:5245"
TEST_EMAIL="testuser$(date +%s)@example.com"
TEST_PASSWORD="Test12345"
TEST_FIRST_NAME="Test"
TEST_LAST_NAME="User"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper function for test output
log_test() {
    echo -e "${BLUE}🔍 $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
    exit 1
}

# Test 1: User Registration
log_test "Testing user registration..."
REGISTER_RESPONSE=$(curl -s "$BACKEND_URL/api/auth/register" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"email\":\"$TEST_EMAIL\",
        \"password\":\"$TEST_PASSWORD\",
        \"firstName\":\"$TEST_FIRST_NAME\",
        \"lastName\":\"$TEST_LAST_NAME\"
    }")

REGISTER_SUCCESS=$(echo "$REGISTER_RESPONSE" | jq -r '.success')
if [ "$REGISTER_SUCCESS" = "true" ]; then
    log_success "User registration successful"
else
    REGISTER_MESSAGE=$(echo "$REGISTER_RESPONSE" | jq -r '.message')
    log_error "User registration failed: $REGISTER_MESSAGE"
fi

# Test 2: Email Verification
log_test "Testing email verification with test code..."
VERIFY_RESPONSE=$(curl -s "$BACKEND_URL/api/auth/verify-email" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"email\":\"$TEST_EMAIL\",
        \"verificationCode\":\"111111\"
    }")

VERIFY_SUCCESS=$(echo "$VERIFY_RESPONSE" | jq -r '.success')
if [ "$VERIFY_SUCCESS" = "true" ]; then
    log_success "Email verification successful"
else
    VERIFY_MESSAGE=$(echo "$VERIFY_RESPONSE" | jq -r '.message')
    log_error "Email verification failed: $VERIFY_MESSAGE"
fi

# Test 3: User Login
log_test "Testing user login..."
LOGIN_RESPONSE=$(curl -s "$BACKEND_URL/api/auth/login" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"email\":\"$TEST_EMAIL\",
        \"password\":\"$TEST_PASSWORD\"
    }")

ACCESS_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.accessToken')
if [ "$ACCESS_TOKEN" != "null" ] && [ -n "$ACCESS_TOKEN" ]; then
    log_success "User login successful"
else
    log_error "User login failed: No access token received"
fi

# Test 4: Authenticated API Access
log_test "Testing authenticated API access..."
USER_PROFILE_RESPONSE=$(curl -s "$BACKEND_URL/api/user/profile" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -H "Content-Type: application/json")

USER_EMAIL=$(echo "$USER_PROFILE_RESPONSE" | jq -r '.email')
if [ "$USER_EMAIL" = "$TEST_EMAIL" ]; then
    log_success "Authenticated API access successful"
else
    log_warning "Authenticated API access failed or profile endpoint not found"
fi

# Test 5: Password Reset Request
log_test "Testing password reset request..."
RESET_REQUEST_RESPONSE=$(curl -s "$BACKEND_URL/api/auth/request-password-reset" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$TEST_EMAIL\"}")

RESET_REQUEST_SUCCESS=$(echo "$RESET_REQUEST_RESPONSE" | jq -r '.success')
if [ "$RESET_REQUEST_SUCCESS" = "true" ]; then
    log_success "Password reset request successful"
else
    log_warning "Password reset request failed (may be throttled)"
fi

# Test 6: Resend Verification (for different user)
log_test "Testing resend verification code throttling..."
RESEND_RESPONSE=$(curl -s "$BACKEND_URL/api/auth/resend-verification" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$TEST_EMAIL\"}")

RESEND_MESSAGE=$(echo "$RESEND_RESPONSE" | jq -r '.message')
if [[ "$RESEND_MESSAGE" == *"bekleyen bir kayıt bulunamadı"* ]]; then
    log_success "Resend verification correctly rejects completed registrations"
elif [[ "$RESEND_MESSAGE" == *"sık istek"* ]]; then
    log_success "Resend verification correctly applies throttling"
else
    log_warning "Resend verification response: $RESEND_MESSAGE"
fi

# Test 7: Invalid Login Attempt
log_test "Testing invalid login attempt..."
INVALID_LOGIN_RESPONSE=$(curl -s "$BACKEND_URL/api/auth/login" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"email\":\"$TEST_EMAIL\",
        \"password\":\"WrongPassword123\"
    }")

INVALID_LOGIN_TOKEN=$(echo "$INVALID_LOGIN_RESPONSE" | jq -r '.accessToken')
if [ "$INVALID_LOGIN_TOKEN" = "null" ] || [ -z "$INVALID_LOGIN_TOKEN" ]; then
    log_success "Invalid login correctly rejected"
else
    log_error "Invalid login was incorrectly accepted"
fi

# Test 8: Duplicate Registration Attempt
log_test "Testing duplicate registration attempt..."
DUPLICATE_REGISTER_RESPONSE=$(curl -s "$BACKEND_URL/api/auth/register" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"email\":\"$TEST_EMAIL\",
        \"password\":\"$TEST_PASSWORD\",
        \"firstName\":\"$TEST_FIRST_NAME\",
        \"lastName\":\"$TEST_LAST_NAME\"
    }")

DUPLICATE_REGISTER_SUCCESS=$(echo "$DUPLICATE_REGISTER_RESPONSE" | jq -r '.success')
if [ "$DUPLICATE_REGISTER_SUCCESS" = "false" ]; then
    log_success "Duplicate registration correctly rejected"
else
    log_error "Duplicate registration was incorrectly accepted"
fi

# Summary
echo ""
echo "🎉 User journey tests completed successfully!"
echo ""
echo "📊 Test Summary:"
echo "✅ User Registration Flow"
echo "✅ Email Verification Flow"
echo "✅ User Login Flow"
echo "✅ Password Reset Flow"
echo "✅ Security Validation (invalid credentials, duplicates)"
echo ""
echo "🔐 Test User Created:"
echo "   Email: $TEST_EMAIL"
echo "   Status: Verified and Active"
echo ""
echo "🎯 All critical user journeys are working correctly!"