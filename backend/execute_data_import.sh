#!/bin/bash

# myTrader Stock_Scrapper Data Import Execution Script
# Orchestrates systematic data loading with progress monitoring

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ORCHESTRATOR_SCRIPT="$SCRIPT_DIR/stock_data_orchestrator.js"
API_BASE_URL="http://localhost:5245"
LOG_FILE="$SCRIPT_DIR/data_import_$(date +%Y%m%d_%H%M%S).log"

echo -e "${BLUE}🚀 MYTRADER DATA IMPORT EXECUTION${NC}"
echo -e "${BLUE}=================================${NC}"
echo "Timestamp: $(date)"
echo "Script Directory: $SCRIPT_DIR"
echo "Log File: $LOG_FILE"
echo "API Endpoint: $API_BASE_URL"
echo ""

# Function to log messages
log_message() {
    echo -e "$1" | tee -a "$LOG_FILE"
}

# Function to check prerequisites
check_prerequisites() {
    log_message "${YELLOW}🔍 Checking prerequisites...${NC}"

    # Check Node.js
    if ! command -v node &> /dev/null; then
        log_message "${RED}❌ Node.js not found. Please install Node.js.${NC}"
        exit 1
    fi
    log_message "${GREEN}✅ Node.js found: $(node --version)${NC}"

    # Check if orchestrator script exists
    if [ ! -f "$ORCHESTRATOR_SCRIPT" ]; then
        log_message "${RED}❌ Orchestrator script not found: $ORCHESTRATOR_SCRIPT${NC}"
        exit 1
    fi
    log_message "${GREEN}✅ Orchestrator script found${NC}"

    # Check API server accessibility
    if ! curl -s "$API_BASE_URL/api/test" &> /dev/null; then
        log_message "${RED}❌ API server not accessible at $API_BASE_URL${NC}"
        log_message "${YELLOW}   Please ensure the .NET API server is running:${NC}"
        log_message "${YELLOW}   cd MyTrader.Api && dotnet run --urls=http://localhost:5245${NC}"
        exit 1
    fi
    log_message "${GREEN}✅ API server accessible${NC}"

    # Check if axios is available (will install if needed)
    if ! node -e "require('axios')" &> /dev/null; then
        log_message "${YELLOW}⚠️  Installing axios dependency...${NC}"
        npm install axios --save-dev || {
            log_message "${RED}❌ Failed to install axios. Please install manually: npm install axios${NC}"
            exit 1
        }
        log_message "${GREEN}✅ axios installed${NC}"
    else
        log_message "${GREEN}✅ axios dependency available${NC}"
    fi
}

# Function to get current data statistics
get_current_stats() {
    log_message "${BLUE}📊 Current database statistics:${NC}"

    local stats_response
    stats_response=$(curl -s "$API_BASE_URL/api/DataImport/statistics?startDate=2020-01-01&endDate=2025-01-01" || echo '{"success":false}')

    if echo "$stats_response" | grep -q '"success":true'; then
        local total_records
        total_records=$(echo "$stats_response" | grep -o '"totalRecordsImported":[0-9]*' | cut -d':' -f2)
        log_message "${GREEN}   Current records in database: ${total_records:-0}${NC}"
    else
        log_message "${YELLOW}   Could not retrieve current statistics${NC}"
    fi
}

# Function to execute the data import
execute_import() {
    log_message "${GREEN}🚀 Starting systematic data import...${NC}"
    log_message "${BLUE}   This will process 223 CSV files across 4 markets${NC}"
    log_message "${BLUE}   Estimated total time: ~45 minutes${NC}"
    log_message ""

    # Execute the Node.js orchestrator
    cd "$SCRIPT_DIR"
    if node "$ORCHESTRATOR_SCRIPT" 2>&1 | tee -a "$LOG_FILE"; then
        log_message ""
        log_message "${GREEN}🎉 DATA IMPORT COMPLETED SUCCESSFULLY!${NC}"
        return 0
    else
        local exit_code=$?
        log_message ""
        log_message "${RED}❌ DATA IMPORT FAILED (exit code: $exit_code)${NC}"
        return $exit_code
    fi
}

# Function to generate final report
generate_final_report() {
    log_message "${BLUE}📊 Generating final report...${NC}"

    local final_stats
    final_stats=$(curl -s "$API_BASE_URL/api/DataImport/statistics?startDate=2020-01-01&endDate=2025-01-01" || echo '{"success":false}')

    if echo "$final_stats" | grep -q '"success":true'; then
        local total_records
        total_records=$(echo "$final_stats" | grep -o '"totalRecordsImported":[0-9]*' | cut -d':' -f2)

        log_message "${GREEN}📈 FINAL STATISTICS:${NC}"
        log_message "${GREEN}   Total records imported: ${total_records:-0}${NC}"
        log_message "${GREEN}   Log file: $LOG_FILE${NC}"

        # Extract market breakdown if available
        if echo "$final_stats" | grep -q '"recordsBySource"'; then
            log_message "${GREEN}   Market breakdown available in API statistics${NC}"
        fi
    else
        log_message "${YELLOW}⚠️  Could not retrieve final statistics${NC}"
    fi

    log_message ""
    log_message "${GREEN}✨ Data import orchestration complete!${NC}"
    log_message "${GREEN}   myTrader is now ready with historical market data${NC}"
}

# Main execution flow
main() {
    # Start timer
    start_time=$(date +%s)

    # Execute all phases
    check_prerequisites
    get_current_stats
    echo ""

    # Confirmation prompt
    read -p "$(echo -e "${YELLOW}Proceed with data import? This will process 223 files (~45 minutes) [y/N]: ${NC}")" -n 1 -r
    echo ""

    if [[ $REPLY =~ ^[Yy]$ ]]; then
        execute_import
        import_result=$?

        # Calculate elapsed time
        end_time=$(date +%s)
        elapsed=$((end_time - start_time))
        elapsed_minutes=$((elapsed / 60))
        elapsed_seconds=$((elapsed % 60))

        log_message ""
        log_message "${BLUE}⏱️  Total execution time: ${elapsed_minutes}m ${elapsed_seconds}s${NC}"

        generate_final_report

        exit $import_result
    else
        log_message "${YELLOW}Import cancelled by user${NC}"
        exit 0
    fi
}

# Execute main function
main "$@"