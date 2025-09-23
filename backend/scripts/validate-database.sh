#!/bin/bash

# MyTrader Database Validation Script
# Validates database connection, schema, and critical tables for user registration

echo "🔍 MyTrader Database Validation"
echo "================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Database connection parameters
DB_HOST="localhost"
DB_PORT="5434"
DB_NAME="mytrader"
DB_USER="postgres"
CONTAINER_NAME="mytrader_postgres"

# Function to execute SQL in container
execute_sql() {
    docker exec $CONTAINER_NAME psql -U $DB_USER -d $DB_NAME -c "$1" 2>/dev/null
}

# Function to check container status
check_container() {
    echo -e "${BLUE}Checking PostgreSQL container status...${NC}"
    if docker ps | grep -q $CONTAINER_NAME; then
        echo -e "${GREEN}✅ PostgreSQL container is running${NC}"
        return 0
    else
        echo -e "${RED}❌ PostgreSQL container is not running${NC}"
        echo "Run: docker-compose up -d postgres"
        return 1
    fi
}

# Function to validate core tables
validate_tables() {
    echo -e "${BLUE}Validating core database tables...${NC}"

    tables=("users" "temp_registrations" "email_verifications" "user_sessions")

    for table in "${tables[@]}"; do
        count=$(execute_sql "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '$table';" | grep -o '[0-9]*' | tail -1)
        if [ "$count" = "1" ]; then
            echo -e "${GREEN}✅ Table '$table' exists${NC}"
        else
            echo -e "${RED}❌ Table '$table' missing${NC}"
            return 1
        fi
    done

    return 0
}

# Function to validate user table schema
validate_user_schema() {
    echo -e "${BLUE}Validating users table schema...${NC}"

    required_columns=("Id" "Email" "PasswordHash" "FirstName" "LastName" "IsActive" "IsEmailVerified")

    for column in "${required_columns[@]}"; do
        count=$(execute_sql "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'users' AND column_name = '$column';" | grep -o '[0-9]*' | tail -1)
        if [ "$count" = "1" ]; then
            echo -e "${GREEN}✅ Column '$column' exists in users table${NC}"
        else
            echo -e "${RED}❌ Column '$column' missing in users table${NC}"
            return 1
        fi
    done

    return 0
}

# Function to test database operations
test_database_operations() {
    echo -e "${BLUE}Testing database operations...${NC}"

    # Test write operation
    echo -e "${YELLOW}Testing INSERT operation...${NC}"
    execute_sql "INSERT INTO temp_registrations (Id, Email, PasswordHash, FirstName, LastName, CreatedAt) VALUES (gen_random_uuid(), 'test@validation.com', 'hashed_password', 'Test', 'User', NOW()) ON CONFLICT DO NOTHING;" > /dev/null

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ INSERT operation successful${NC}"
    else
        echo -e "${RED}❌ INSERT operation failed${NC}"
        return 1
    fi

    # Test read operation
    echo -e "${YELLOW}Testing SELECT operation...${NC}"
    count=$(execute_sql "SELECT COUNT(*) FROM temp_registrations WHERE Email = 'test@validation.com';" | grep -o '[0-9]*' | tail -1)

    if [ "$count" -ge "1" ]; then
        echo -e "${GREEN}✅ SELECT operation successful${NC}"
    else
        echo -e "${RED}❌ SELECT operation failed${NC}"
        return 1
    fi

    # Clean up test data
    execute_sql "DELETE FROM temp_registrations WHERE Email = 'test@validation.com';" > /dev/null

    return 0
}

# Function to show database stats
show_database_stats() {
    echo -e "${BLUE}Database Statistics...${NC}"

    user_count=$(execute_sql "SELECT COUNT(*) FROM users;" | grep -o '[0-9]*' | tail -1)
    temp_reg_count=$(execute_sql "SELECT COUNT(*) FROM temp_registrations;" | grep -o '[0-9]*' | tail -1)
    email_verif_count=$(execute_sql "SELECT COUNT(*) FROM email_verifications;" | grep -o '[0-9]*' | tail -1)

    echo -e "📊 Users: ${YELLOW}$user_count${NC}"
    echo -e "📊 Temp Registrations: ${YELLOW}$temp_reg_count${NC}"
    echo -e "📊 Email Verifications: ${YELLOW}$email_verif_count${NC}"
}

# Function to validate indexes
validate_indexes() {
    echo -e "${BLUE}Validating critical indexes...${NC}"

    # Check for email index on users table
    email_index_count=$(execute_sql "SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'users' AND indexname LIKE '%email%';" | grep -o '[0-9]*' | tail -1)

    if [ "$email_index_count" -ge "1" ]; then
        echo -e "${GREEN}✅ Email index exists on users table${NC}"
    else
        echo -e "${YELLOW}⚠️  Email index missing on users table (performance impact)${NC}"
    fi
}

# Main execution
main() {
    echo -e "${BLUE}Starting database validation...${NC}"
    echo ""

    # Check container
    if ! check_container; then
        exit 1
    fi

    echo ""

    # Validate tables
    if ! validate_tables; then
        echo -e "${RED}❌ Database table validation failed${NC}"
        exit 1
    fi

    echo ""

    # Validate schema
    if ! validate_user_schema; then
        echo -e "${RED}❌ User table schema validation failed${NC}"
        exit 1
    fi

    echo ""

    # Test operations
    if ! test_database_operations; then
        echo -e "${RED}❌ Database operation tests failed${NC}"
        exit 1
    fi

    echo ""

    # Validate indexes
    validate_indexes

    echo ""

    # Show stats
    show_database_stats

    echo ""
    echo -e "${GREEN}🎉 Database validation completed successfully!${NC}"
    echo -e "${GREEN}✅ All required tables exist${NC}"
    echo -e "${GREEN}✅ User registration schema is valid${NC}"
    echo -e "${GREEN}✅ Database operations working correctly${NC}"
}

# Run main function
main