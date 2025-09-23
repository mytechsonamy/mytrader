#!/bin/bash

echo "🔍 Checking for infinite render patterns..."

# Check for useEffect without dependency array that might cause re-renders
echo "Checking useEffect dependency arrays..."
if grep -r -n "useEffect.*{" frontend/mobile/src --include="*.tsx" --include="*.ts" | grep -v "], \["; then
    echo "❌ Found useEffect without proper dependency array:"
    grep -r -n "useEffect.*{" frontend/mobile/src --include="*.tsx" --include="*.ts" | grep -v "], \["
    echo "This can cause infinite re-renders!"
    exit 1
fi

# Check for setState in useEffect without proper dependencies
echo "Checking for setState in useEffect..."
if grep -A 10 -B 2 "useEffect" frontend/mobile/src/**/*.tsx | grep -E "(setState|set[A-Z])" | grep -v "prev =>"; then
    echo "⚠️  Found setState in useEffect - verify dependencies are correct"
fi

# Check for object/array dependencies in useEffect
echo "Checking for object dependencies in useEffect..."
if grep -r -n "useEffect.*\[.*\.\|useEffect.*\[.*{" frontend/mobile/src --include="*.tsx"; then
    echo "⚠️  Found object/array dependencies in useEffect - these may cause re-renders"
fi

# Check for functions created inside render without useCallback
echo "Checking for inline functions in JSX..."
if grep -r -n "onClick={() =>" frontend/mobile/src --include="*.tsx"; then
    echo "⚠️  Found inline functions in JSX - consider using useCallback"
fi

echo "✅ Infinite render check completed"