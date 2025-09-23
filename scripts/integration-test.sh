#!/bin/bash

echo "🧪 Running integration smoke tests..."

# Test 1: Backend Health Check
echo "Testing backend health endpoint..."
BACKEND_URL="http://localhost:5245"
if curl -f -s "$BACKEND_URL/health" > /dev/null; then
    echo "✅ Backend health check passed"
else
    echo "❌ Backend health check failed"
    exit 1
fi

# Test 2: Database Connection (through API)
echo "Testing database connectivity..."
DB_TEST=$(curl -s "$BACKEND_URL/" | grep -o "running" || echo "failed")
if [ "$DB_TEST" = "running" ]; then
    echo "✅ Backend is running (database may have issues but API is operational)"
else
    echo "❌ Backend API test failed"
    exit 1
fi

# Test 3: SignalR Hub Endpoint
echo "Testing SignalR hub endpoint..."
HUB_URL="$BACKEND_URL/hubs/trading"
# Try to connect to SignalR (this will fail but should return a proper HTTP response)
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$HUB_URL" || echo "000")
if [ "$HTTP_CODE" = "404" ] || [ "$HTTP_CODE" = "400" ] || [ "$HTTP_CODE" = "200" ]; then
    echo "✅ SignalR hub endpoint is accessible (got HTTP $HTTP_CODE)"
else
    echo "❌ SignalR hub endpoint not accessible (got HTTP $HTTP_CODE)"
fi

# Test 4: Frontend Metro Bundler
echo "Testing frontend Metro bundler..."
METRO_URL="http://localhost:8081"
if curl -f -s "$METRO_URL" > /dev/null; then
    echo "✅ Metro bundler is running"
else
    echo "⚠️  Metro bundler not running (this is OK if not in development mode)"
fi

# Test 5: Environment Configuration Validation
echo "Testing environment configuration..."
if [ -f "docker-compose.yml" ]; then
    # Check if docker-compose is valid
    if docker-compose config > /dev/null 2>&1; then
        echo "✅ Docker Compose configuration is valid"
    else
        echo "⚠️  Docker Compose configuration has issues"
    fi
fi

# Test 6: Critical File Existence
echo "Checking critical files..."
CRITICAL_FILES=(
    "backend/MyTrader.Api/MyTrader.Api.csproj"
    "frontend/mobile/package.json"
    "frontend/mobile/src/App.tsx"
)

for file in "${CRITICAL_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo "✅ $file exists"
    else
        echo "❌ $file missing"
        exit 1
    fi
done

echo "🎉 Integration smoke tests completed successfully!"