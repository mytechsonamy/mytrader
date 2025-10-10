#!/bin/bash

###############################################################################
# myTrader Performance Test Suite Runner
#
# This script executes all 5 performance test scenarios in sequence and
# generates a comprehensive performance report.
#
# Prerequisites:
# - k6 installed (https://k6.io/docs/getting-started/installation/)
# - Backend API running
# - PostgreSQL database accessible
# - Valid authentication credentials
#
# Usage:
#   ./run-all-tests.sh [BASE_URL] [AUTH_TOKEN]
#
# Example:
#   ./run-all-tests.sh http://localhost:5002 your-jwt-token
###############################################################################

set -e  # Exit on any error

# Configuration
BASE_URL="${1:-http://localhost:5002}"
AUTH_TOKEN="${2:-}"
RESULTS_DIR="./performance-results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
TEST_RUN_DIR="${RESULTS_DIR}/${TIMESTAMP}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Banner
print_banner() {
    echo ""
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║          myTrader Performance Test Suite                     ║"
    echo "║          Alpaca Streaming Integration Testing                ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."

    # Check if k6 is installed
    if ! command -v k6 &> /dev/null; then
        log_error "k6 is not installed. Please install from https://k6.io/docs/getting-started/installation/"
        exit 1
    fi

    # Check if backend is accessible
    if ! curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/health" | grep -q "200"; then
        log_error "Backend API is not accessible at ${BASE_URL}"
        exit 1
    fi

    log_success "All prerequisites met"
}

# Create results directory
setup_results_dir() {
    log_info "Creating results directory: ${TEST_RUN_DIR}"
    mkdir -p "${TEST_RUN_DIR}"
}

# Obtain authentication token if not provided
obtain_auth_token() {
    if [ -z "$AUTH_TOKEN" ]; then
        log_warning "No auth token provided. Attempting login..."

        # Attempt to login with test credentials
        local LOGIN_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/auth/login" \
            -H "Content-Type: application/json" \
            -d '{"username":"test@example.com","password":"TestPassword123!"}')

        AUTH_TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)

        if [ -z "$AUTH_TOKEN" ]; then
            log_error "Failed to obtain authentication token"
            log_info "Response: $LOGIN_RESPONSE"
            exit 1
        fi

        log_success "Authentication token obtained"
    fi
}

# Run a single test scenario
run_test() {
    local test_name="$1"
    local test_file="$2"
    local output_file="$3"

    log_info "Running ${test_name}..."
    echo ""

    local start_time=$(date +%s)

    # Run k6 test with environment variables
    if k6 run \
        --out json="${TEST_RUN_DIR}/${output_file}" \
        --env BASE_URL="${BASE_URL}" \
        --env AUTH_TOKEN="${AUTH_TOKEN}" \
        "${test_file}" > "${TEST_RUN_DIR}/${output_file}.log" 2>&1; then

        local end_time=$(date +%s)
        local duration=$((end_time - start_time))

        log_success "${test_name} completed in ${duration}s"
        return 0
    else
        local end_time=$(date +%s)
        local duration=$((end_time - start_time))

        log_error "${test_name} failed after ${duration}s"
        log_info "Check log: ${TEST_RUN_DIR}/${output_file}.log"
        return 1
    fi
}

# Generate summary report
generate_summary_report() {
    log_info "Generating summary report..."

    local report_file="${TEST_RUN_DIR}/PERFORMANCE_TEST_REPORT.md"

    cat > "$report_file" <<EOF
# myTrader Performance Test Report

**Test Run ID:** ${TIMESTAMP}
**Date:** $(date +"%Y-%m-%d %H:%M:%S")
**Base URL:** ${BASE_URL}

---

## Executive Summary

This report presents the results of comprehensive performance testing for the myTrader Alpaca streaming integration.

### Test Scenarios Executed

1. **Baseline Performance** - 10 VUs, 30 symbols, 10 minutes
2. **Burst Load** - 50 VUs, 30 symbols, 5 minutes (market open simulation)
3. **Sustained Load** - 50 VUs, 30 symbols, 1 hour (stability test)
4. **Failover Performance** - Alpaca → Yahoo → Alpaca transition
5. **Database Performance** - Concurrent real-time and batch operations

---

## Test Results

EOF

    # Extract and summarize results from each test
    for result_file in "${TEST_RUN_DIR}"/performance-*.json; do
        if [ -f "$result_file" ]; then
            local scenario_name=$(basename "$result_file" .json | sed 's/performance-//' | sed 's/-results//')

            echo "" >> "$report_file"
            echo "### Scenario: ${scenario_name^}" >> "$report_file"
            echo "" >> "$report_file"
            echo '```json' >> "$report_file"
            cat "$result_file" >> "$report_file"
            echo '```' >> "$report_file"
            echo "" >> "$report_file"
        fi
    done

    # Add SLO compliance section
    cat >> "$report_file" <<EOF

---

## SLO Compliance

| Metric | SLO Target | Baseline | Burst | Sustained | Status |
|--------|------------|----------|-------|-----------|--------|
| P95 Latency | <2s | TBD | TBD | TBD | ⏳ |
| P99 Latency | <5s | TBD | TBD | TBD | ⏳ |
| Message Rate | 150/sec | TBD | TBD | TBD | ⏳ |
| Memory Usage | <500MB | TBD | TBD | TBD | ⏳ |
| CPU Usage | <70% | TBD | TBD | TBD | ⏳ |
| DB Write Time | <100ms | TBD | TBD | TBD | ⏳ |

*Legend: ✅ Met | ❌ Not Met | ⏳ Pending Analysis*

---

## Observations

### Performance Strengths
- TBD: Analysis pending

### Performance Concerns
- TBD: Analysis pending

### Bottlenecks Identified
- TBD: Analysis pending

---

## Recommendations

### Immediate Actions
1. Review detailed test logs for any errors or warnings
2. Analyze resource utilization during peak load
3. Validate connection pool configuration

### Optimization Opportunities
1. TBD: Based on test results
2. TBD: Based on profiling data

### Infrastructure Recommendations
1. TBD: Based on capacity analysis

---

## Test Environment

- **Backend Version:** $(curl -s "${BASE_URL}/health" | grep -o '"version":"[^"]*' | cut -d'"' -f4 || echo "Unknown")
- **Database:** PostgreSQL
- **Test Tool:** k6 $(k6 version | head -n 1)
- **Test Duration:** $(date -d @$(($(date +%s) - $(date -d "$TIMESTAMP" +%s))) -u +%H:%M:%S 2>/dev/null || echo "N/A")

---

## Appendix

### Test Artifacts
- Raw k6 results: \`${TEST_RUN_DIR}\`
- Test logs: \`${TEST_RUN_DIR}/*.log\`
- JSON results: \`${TEST_RUN_DIR}/performance-*.json\`

### Next Steps
1. Review this report with the engineering team
2. Prioritize optimization work based on findings
3. Re-run tests after implementing improvements
4. Establish continuous performance monitoring

EOF

    log_success "Summary report generated: ${report_file}"
}

# Main execution
main() {
    print_banner

    # Setup
    check_prerequisites
    setup_results_dir
    obtain_auth_token

    # Track test results
    local tests_passed=0
    local tests_failed=0

    log_info "Starting performance test suite..."
    log_info "Results will be saved to: ${TEST_RUN_DIR}"
    echo ""

    # Run each test scenario
    log_info "═══════════════════════════════════════════════════════════"
    log_info "Test 1/5: Baseline Performance"
    log_info "═══════════════════════════════════════════════════════════"
    if run_test "Baseline Performance" "./k6-baseline-test.js" "baseline-results"; then
        ((tests_passed++))
    else
        ((tests_failed++))
    fi
    echo ""

    log_info "═══════════════════════════════════════════════════════════"
    log_info "Test 2/5: Burst Load"
    log_info "═══════════════════════════════════════════════════════════"
    if run_test "Burst Load" "./k6-burst-load-test.js" "burst-results"; then
        ((tests_passed++))
    else
        ((tests_failed++))
    fi
    echo ""

    log_info "═══════════════════════════════════════════════════════════"
    log_info "Test 3/5: Sustained Load"
    log_info "═══════════════════════════════════════════════════════════"
    if run_test "Sustained Load" "./k6-sustained-load-test.js" "sustained-results"; then
        ((tests_passed++))
    else
        ((tests_failed++))
    fi
    echo ""

    log_info "═══════════════════════════════════════════════════════════"
    log_info "Test 4/5: Failover Performance"
    log_info "═══════════════════════════════════════════════════════════"
    if run_test "Failover Performance" "./k6-failover-test.js" "failover-results"; then
        ((tests_passed++))
    else
        ((tests_failed++))
    fi
    echo ""

    log_info "═══════════════════════════════════════════════════════════"
    log_info "Test 5/5: Database Performance"
    log_info "═══════════════════════════════════════════════════════════"
    if run_test "Database Performance" "./k6-database-performance-test.js" "database-results"; then
        ((tests_passed++))
    else
        ((tests_failed++))
    fi
    echo ""

    # Generate summary report
    generate_summary_report

    # Final summary
    echo ""
    log_info "═══════════════════════════════════════════════════════════"
    log_info "Performance Test Suite Complete"
    log_info "═══════════════════════════════════════════════════════════"
    log_info "Tests Passed: ${tests_passed}/5"
    if [ "$tests_failed" -gt 0 ]; then
        log_warning "Tests Failed: ${tests_failed}/5"
    fi
    log_info "Results Directory: ${TEST_RUN_DIR}"
    log_info "Summary Report: ${TEST_RUN_DIR}/PERFORMANCE_TEST_REPORT.md"
    echo ""

    if [ "$tests_failed" -eq 0 ]; then
        log_success "All tests passed! 🎉"
        exit 0
    else
        log_warning "Some tests failed. Review logs for details."
        exit 1
    fi
}

# Execute main function
main
