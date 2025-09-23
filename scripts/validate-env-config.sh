#!/bin/bash

echo "🔍 Validating environment configuration..."

# Check for required environment variables in documentation
echo "Checking environment variable documentation..."
REQUIRED_VARS=("API_BASE_URL" "WS_BASE_URL" "DB_HOST" "DB_PORT" "DB_NAME" "DB_USER" "DB_PASSWORD")

for var in "${REQUIRED_VARS[@]}"; do
    if ! grep -q "$var" README.md 2>/dev/null && ! grep -q "$var" .env.example 2>/dev/null; then
        echo "⚠️  Environment variable $var not documented"
    fi
done

# Check for port consistency across files
echo "Checking port consistency..."

# Extract ports from various config files
API_PORT_PACKAGE=$(grep -o '"dev":.*localhost:[0-9]*' frontend/mobile/package.json 2>/dev/null | grep -o '[0-9]*' || echo "not_found")
API_PORT_CONFIG=$(grep -o 'API_BASE_URL.*:[0-9]*' frontend/mobile/src/config/constants.ts 2>/dev/null | grep -o '[0-9]*' || echo "not_found")

if [ "$API_PORT_PACKAGE" != "not_found" ] && [ "$API_PORT_CONFIG" != "not_found" ] && [ "$API_PORT_PACKAGE" != "$API_PORT_CONFIG" ]; then
    echo "❌ Port mismatch found between package.json ($API_PORT_PACKAGE) and config ($API_PORT_CONFIG)"
    exit 1
fi

# Check for SignalR URL consistency
echo "Checking SignalR URL configuration..."
if grep -r "localhost:.*hubs" frontend/mobile/src --include="*.ts" --include="*.tsx"; then
    echo "⚠️  Found hardcoded localhost in SignalR configuration"
fi

# Validate Docker Compose environment variables
echo "Checking Docker Compose environment variables..."
if [ -f "docker-compose.yml" ]; then
    # Check if environment variables are properly used
    if grep -q "environment:" docker-compose.yml; then
        echo "✅ Docker Compose uses environment variables"
    else
        echo "⚠️  Docker Compose doesn't define environment variables"
    fi
fi

# Check for .env.example existence
if [ ! -f ".env.example" ]; then
    echo "⚠️  .env.example file missing - should document all required environment variables"
fi

echo "✅ Environment configuration validation completed"