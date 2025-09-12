# MyTrader Development Setup

This is a complete trading platform with ASP.NET Core backend and React Native (Expo) frontend.

## Prerequisites

- **Node.js** (v18 or higher)
- **.NET 9.0 SDK** 
- **PostgreSQL** (running on port 5434)
- **Expo CLI** (installed globally: `npm install -g @expo/cli`)

## Quick Start

### 1. Backend Setup

```bash
# Navigate to backend directory
cd backend/MyTrader.Api

# Install dependencies (restore packages)
dotnet restore

# Run the API server
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5002" dotnet run --no-launch-profile
```

The backend API will be available at: `http://localhost:5002`

### 2. Frontend Setup

```bash
# Navigate to frontend directory  
cd frontend/mobile

# Install dependencies
npm install

# Start Expo development server
npm start
# or
npx expo start
```

The Expo development server will start and provide options to:
- Press `w` to open in web browser
- Press `a` to open on Android device/emulator  
- Press `i` to open on iOS simulator
- Scan QR code with Expo Go app on physical device

## API Endpoints

The backend provides these key endpoints:

- **Health Check**: `GET /health`
- **Authentication**: `POST /api/auth/register`, `POST /api/auth/login` 
- **Market Data**: `GET /api/MockMarket/symbols`
- **WebSocket Hub**: `ws://localhost:5002/hubs/mock-trading`

## Development Features

### Backend Features ✅
- All compiler warnings cleaned up
- Mock authentication for testing
- Mock market data endpoints
- SignalR hub for real-time data
- PostgreSQL database integration
- JWT authentication system
- CORS configured for frontend

### Frontend Features ✅
- TypeScript with strict mode enabled
- Navigation between screens (Dashboard, Profile, etc.)
- Real-time price updates via WebSocket
- Authentication flow (Login, Register)
- New feature screens: Gamification, Alarms, Education
- Improved type safety with utility types
- Responsive design with React Native components

## Project Structure

```
myTrader/
├── backend/
│   └── MyTrader.Api/          # ASP.NET Core API
├── frontend/
│   └── mobile/                # React Native (Expo) app
├── README.dev.md              # This development guide
└── ...
```

## Configuration

### Backend Configuration
- API runs on port 5002
- Database: PostgreSQL on localhost:5434
- Environment: Development
- Mock controllers bypass authentication for testing

### Frontend Configuration  
- API Base URL: `http://localhost:5002/api`
- WebSocket URL: `http://localhost:5002/hubs/mock-trading`
- Expo SDK 53 with React Native 0.79.5

## Troubleshooting

### Common Issues

1. **Backend won't start**: Check if PostgreSQL is running on port 5434
2. **Frontend API calls fail**: Ensure backend is running on port 5002
3. **WebSocket connection errors**: Try restarting both backend and frontend
4. **TypeScript errors**: Run `npx tsc --noEmit` to check for issues

### Development Tips

- Backend API automatically restarts on code changes
- Frontend has hot reloading enabled via Expo
- Use Chrome DevTools for debugging WebSocket connections
- Check backend logs for API request details
- Use React Native Debugger for frontend debugging

## Next Steps

- Run backend: `cd backend/MyTrader.Api && dotnet run`
- Run frontend: `cd frontend/mobile && npm start`
- Open app in browser, simulator, or device
- Test authentication and market data features

The development environment is now fully configured with mock data for testing all features!