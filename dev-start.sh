#!/bin/bash

# MyTrader Development Startup Script
# This script starts both the backend API and frontend Expo dev server

set -e

echo "🚀 Starting MyTrader Development Environment"
echo "============================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if backend directory exists
if [ ! -d "backend/MyTrader.Api" ]; then
    echo -e "${RED}❌ Backend directory not found. Please run this script from the project root.${NC}"
    exit 1
fi

# Check if frontend directory exists
if [ ! -d "frontend/mobile" ]; then
    echo -e "${RED}❌ Frontend directory not found. Please run this script from the project root.${NC}"
    exit 1
fi

# Function to start backend
start_backend() {
    echo -e "${BLUE}📡 Starting Backend API Server...${NC}"
    cd backend/MyTrader.Api
    
    # Check if .NET is installed
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}❌ .NET SDK is not installed. Please install .NET 9.0 SDK.${NC}"
        exit 1
    fi
    
    # Start the backend API server
    echo -e "${GREEN}✅ Backend starting on http://localhost:5002${NC}"
    ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5002" dotnet run --no-launch-profile &
    BACKEND_PID=$!
    
    cd ../..
    echo -e "${GREEN}✅ Backend API started with PID: $BACKEND_PID${NC}"
}

# Function to start frontend
start_frontend() {
    echo -e "${BLUE}📱 Starting Frontend Expo Server...${NC}"
    cd frontend/mobile
    
    # Check if Node.js is installed
    if ! command -v node &> /dev/null; then
        echo -e "${RED}❌ Node.js is not installed. Please install Node.js v18 or higher.${NC}"
        exit 1
    fi
    
    # Check if packages are installed
    if [ ! -d "node_modules" ]; then
        echo -e "${YELLOW}📦 Installing frontend dependencies...${NC}"
        npm install
    fi
    
    # Start Expo development server
    echo -e "${GREEN}✅ Frontend starting with Expo...${NC}"
    npx expo start &
    FRONTEND_PID=$!
    
    cd ../..
    echo -e "${GREEN}✅ Frontend Expo server started with PID: $FRONTEND_PID${NC}"
}

# Function to cleanup processes on exit
cleanup() {
    echo -e "${YELLOW}🛑 Shutting down development servers...${NC}"
    if [ ! -z "$BACKEND_PID" ]; then
        kill $BACKEND_PID 2>/dev/null || true
        echo -e "${GREEN}✅ Backend server stopped${NC}"
    fi
    if [ ! -z "$FRONTEND_PID" ]; then
        kill $FRONTEND_PID 2>/dev/null || true
        echo -e "${GREEN}✅ Frontend server stopped${NC}"
    fi
    exit 0
}

# Set trap to cleanup on script exit
trap cleanup SIGINT SIGTERM

# Start services
start_backend
sleep 3  # Give backend time to start
start_frontend

echo ""
echo -e "${GREEN}🎉 MyTrader Development Environment is Ready!${NC}"
echo "============================================="
echo -e "${BLUE}Backend API:${NC}     http://localhost:5002"
echo -e "${BLUE}Frontend:${NC}       Follow Expo instructions above"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop both servers${NC}"
echo ""

# Keep script running and wait for processes
wait