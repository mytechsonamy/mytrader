#!/bin/bash

echo "🔍 Checking database connection configurations..."

# Check for localhost in production configs
echo "Checking for localhost usage in production configs..."
if find backend -name "appsettings.Production.json" -exec grep -l "localhost\|127.0.0.1" {} \; | grep -q .; then
    echo "❌ Found localhost in production configuration!"
    find backend -name "appsettings.Production.json" -exec grep -n "localhost\|127.0.0.1" {} \;
    exit 1
fi

# Check for hardcoded ports
echo "Checking for hardcoded database ports..."
if grep -r "Port=5434" backend --include="*.json"; then
    echo "❌ Found incorrect hardcoded port 5434 (should be 5432)"
    exit 1
fi

# Check for missing environment variable usage
echo "Checking for proper environment variable usage..."
if grep -r "ConnectionStrings" backend --include="*.json" | grep -v "\${"; then
    echo "⚠️  Found connection strings that don't use environment variables"
    echo "Consider using \${DB_HOST}, \${DB_PORT}, etc."
fi

# Validate Docker environment consistency
echo "Checking Docker Compose configuration..."
if [ -f "docker-compose.yml" ]; then
    # Check if PostgreSQL service is defined
    if ! grep -q "postgres:" docker-compose.yml; then
        echo "⚠️  PostgreSQL service not found in docker-compose.yml"
    fi

    # Check for port consistency
    if grep -q "5434:5432" docker-compose.yml; then
        echo "⚠️  Docker exposes PostgreSQL on port 5434, ensure application config matches"
    fi
fi

echo "✅ Database connection check completed"