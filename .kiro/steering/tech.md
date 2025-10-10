# Technology Stack & Build System

## Backend (.NET 9)

**Framework**: ASP.NET Core Web API with SignalR
**Database**: PostgreSQL with Entity Framework Core
**Authentication**: JWT Bearer tokens
**Real-time**: SignalR WebSocket hubs
**Logging**: Serilog with Grafana Loki integration
**Health Checks**: Built-in ASP.NET Core health checks

### Key Dependencies
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.EntityFrameworkCore (PostgreSQL provider)
- Microsoft.AspNetCore.SignalR
- Serilog.AspNetCore
- Swashbuckle.AspNetCore (OpenAPI/Swagger)

### Common Commands
```bash
# Build solution
cd backend && dotnet build

# Run API locally
cd backend/MyTrader.Api && dotnet run

# Run tests
cd backend && dotnet test

# Database migrations
cd backend/MyTrader.Api && dotnet ef migrations add <MigrationName>
cd backend/MyTrader.Api && dotnet ef database update

# Docker deployment
docker-compose up -d
```

## Frontend Web (React + Vite)

**Framework**: React 19 with TypeScript
**Build Tool**: Vite
**Styling**: Tailwind CSS
**State Management**: Zustand + React Query
**Routing**: React Router v6
**Testing**: Vitest + Playwright
**Real-time**: SignalR client (@microsoft/signalr)

### Key Dependencies
- React 19 + React DOM
- @tanstack/react-query (server state)
- zustand (client state)
- react-router-dom
- @microsoft/signalr
- tailwindcss
- framer-motion (animations)

### Common Commands
```bash
# Install dependencies
cd frontend/web && npm install

# Development server
cd frontend/web && npm run dev

# Build for production
cd frontend/web && npm run build

# Run tests
cd frontend/web && npm test
cd frontend/web && npm run test:e2e

# Linting & formatting
cd frontend/web && npm run lint
cd frontend/web && npm run format
```

## Frontend Mobile (React Native + Expo)

**Framework**: React Native with Expo SDK 54
**Navigation**: React Navigation v7
**State Management**: Context API + AsyncStorage
**Charts**: react-native-chart-kit
**Testing**: Jest + Detox

### Key Dependencies
- expo ~54.0.0
- react-native 0.81.4
- @react-navigation/native
- @microsoft/signalr
- react-native-chart-kit

### Common Commands
```bash
# Install dependencies
cd frontend/mobile && npm install

# Start Expo dev server
cd frontend/mobile && npm start

# Run on iOS simulator
cd frontend/mobile && npm run ios

# Run on Android emulator
cd frontend/mobile && npm run android

# Run tests
cd frontend/mobile && npm test
```

## Database

**Primary**: PostgreSQL 15
**Connection**: Entity Framework Core with Npgsql provider
**Migrations**: EF Core migrations
**Health Checks**: AspNetCore.HealthChecks.Npgsql

## Development Environment

**Containerization**: Docker + Docker Compose
**API Documentation**: Swagger/OpenAPI
**Proxy Setup**: Vite dev server proxies to backend
**CORS**: Configured for local development

## Build & Deployment

**Backend**: Docker multi-stage builds
**Frontend Web**: Vite static build
**Mobile**: Expo managed workflow
**Database**: PostgreSQL container with init scripts
**Orchestration**: Docker Compose for full stack