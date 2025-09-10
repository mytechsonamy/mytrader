# MyTrader - Full Stack Trading Platform

This project is a complete .NET-based crypto trading platform, providing a modern, scalable trading system with authentication, real-time data streaming, and technical analysis capabilities.

## Project Structure

```
myTrader/
├── backend/                    # .NET Backend API
│   ├── MyTrader.Api/          # ASP.NET Core Web API
│   ├── MyTrader.Core/         # Domain models, DTOs, interfaces
│   ├── MyTrader.Services/     # Business logic and services
│   ├── MyTrader.Infrastructure/ # Data access, Entity Framework
│   ├── docker-compose.yml     # Backend Docker setup
│   └── Dockerfile            # Backend container
├── frontend/                   # Frontend Applications
│   ├── web/                   # Web Frontend (Future React/Vue app)
│   └── mobile/                # React Native/Expo Mobile App (✅ Active)
├── docker-compose.yml         # Full stack Docker setup
└── DOCKER_README.md          # Docker setup guide
```

## Architecture

The solution uses a clean architecture approach with the following structure:

- **Backend (.NET 9)** - RESTful API with SignalR for real-time updates
- **Web Frontend** - Modern web application for trading interface (Coming Soon)
- **Mobile App** - React Native/Expo cross-platform mobile application (✅ Active)
- **PostgreSQL** - Database for persistent data storage

## Features

### ✅ Completed Features

1. **Authentication System**
   - JWT-based authentication
   - User registration with email verification
   - Password reset functionality
   - Secure session management

2. **Market Data Management**
   - Market data import endpoints
   - Historical price data storage
   - Multi-symbol support
   - PostgreSQL integration with Entity Framework Core

3. **Trading Strategy Engine**
   - Bollinger Bands indicator
   - RSI (Relative Strength Index) calculation
   - MACD indicator
   - Signal generation based on multiple indicators
   - Strategy backtesting capabilities

4. **Real-time Communication**
   - SignalR WebSocket hub for live data streaming
   - Real-time signal broadcasting
   - User-specific data channels

5. **API Endpoints**
   - `/auth/*` - Complete authentication system
   - `/api/market/*` - Market data operations
   - `/api/signals` - Signal history and current data
   - `/api/strategies/*` - Strategy management

## Technology Stack

- **.NET 9.0** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM with PostgreSQL
- **SignalR** - Real-time web communication
- **JWT Authentication** - Secure token-based auth
- **PostgreSQL** - Primary database

## API Endpoints

### Authentication
- `POST /auth/register` - User registration
- `POST /auth/verify-email` - Email verification
- `POST /auth/login` - User login
- `POST /auth/logout` - User logout
- `GET /auth/me` - Get current user info

### Market Data
- `POST /api/market/import-daily` - Import daily price data
- `GET /api/market/{symbol}` - Get historical data

### Signals & Trading
- `GET /api/signals` - Get signal history
- `GET /api/market-data` - Get current market data
- `GET /api/strategies/{symbol}/signals` - Get signals for symbol

### Real-time
- `WebSocket /hubs/trading` - SignalR connection for real-time updates

## Configuration

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=mytrader;Username=postgres;Password=password"
  },
  "Jwt": {
    "Secret": "your-256-bit-secret-key"
  },
  "AuthTestMode": false
}
```

## Database Setup

1. Ensure PostgreSQL is running
2. Create database: `createdb mytrader`
3. The application will automatically create tables on startup

## Running the Application

```bash
# Restore dependencies
dotnet restore

# Run the API
cd MyTrader.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- OpenAPI/Swagger: `https://localhost:5001/swagger` (in development)

## Trading Strategy Logic

The system implements a multi-indicator strategy:

1. **Bollinger Bands** - Identifies overbought/oversold conditions
2. **RSI** - Confirms momentum (< 30 = oversold, > 70 = overbought)  
3. **MACD** - Trend direction and momentum

**Signal Generation:**
- **BUY**: RSI < 30, price at lower Bollinger Band, MACD bullish
- **SELL**: RSI > 70, price at upper Bollinger Band, MACD bearish
- **NEUTRAL**: Mixed or insufficient signals

## Real-time Features

- WebSocket connections via SignalR
- Live price updates
- Real-time signal notifications
- User-specific data streams

## Security Features

- JWT token authentication
- Password hashing with salt
- Rate limiting for sensitive operations
- Email verification for new accounts
- CORS configuration

## Development Notes

The solution includes some dependency version conflicts that are common in .NET projects but don't prevent functionality. These can be resolved by:

1. Updating all Entity Framework packages to the same version
2. Using explicit package references
3. Adding binding redirects if needed

The core functionality is complete and working, providing a solid foundation for a production trading system.

## Next Steps for Production

1. Implement real market data providers (Binance, Alpha Vantage, etc.)
2. Add proper email service integration
3. Implement advanced order management
4. Add comprehensive logging and monitoring
5. Deploy with Docker containers
6. Set up CI/CD pipelines

## Project Structure

```
MyTrader/
├── MyTrader.Api/              # Web API and SignalR
│   ├── Controllers/           # API controllers
│   ├── Hubs/                 # SignalR hubs
│   └── Program.cs            # Application startup
├── MyTrader.Core/            # Domain layer
│   ├── Models/               # Data models
│   └── DTOs/                 # Data transfer objects
├── MyTrader.Services/        # Business logic
│   ├── Authentication/       # Auth services
│   ├── Market/              # Market data services
│   ├── Trading/             # Strategy services
│   └── Signals/             # Signal services
└── MyTrader.Infrastructure/  # Data access
    └── Data/                # EF Core context
```

## Mobile App Setup

The mobile app is a React Native/Expo application located in `frontend/mobile/`.

### Prerequisites
- Node.js (v18 or later)
- npm or yarn
- Expo CLI
- iOS Simulator or Android Emulator (or physical device with Expo Go app)

### Quick Start

1. **Start the backend API:**
   ```bash
   docker-compose up -d
   ```

2. **Navigate to mobile app:**
   ```bash
   cd frontend/mobile
   ```

3. **Install dependencies (if needed):**
   ```bash
   npm install
   ```

4. **Start Expo development server:**
   ```bash
   npm start
   ```

5. **Run on device/simulator:**
   - Scan QR code with Expo Go app (iOS/Android)
   - Press `i` for iOS Simulator
   - Press `a` for Android Emulator
   - Press `w` for web browser

### Configuration

The mobile app is pre-configured to connect to the .NET backend:
- **API Base URL:** `http://localhost:8080/api`
- **SignalR Hub:** `ws://localhost:8080/signalrhub`

### Features
- User authentication (login/register)
- Real-time trading signals
- Market data visualization
- Strategy testing interface
- Dashboard with portfolio overview

### Development Notes
- The app uses TypeScript for type safety
- Real-time updates via SignalR WebSocket connection
- Responsive design for both iOS and Android
- Navigation handled by React Navigation v6

This .NET implementation provides all the core functionality of the original Python trading bot with modern architecture, better performance, and enhanced scalability.
