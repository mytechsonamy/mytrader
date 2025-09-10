# TraderMobile - MyTrader Mobile App

A React Native/Expo mobile application for the MyTrader crypto trading platform.

## Features

- **User Authentication** - JWT-based login and registration
- **Real-time Trading Signals** - Live updates via SignalR WebSocket
- **Market Data Visualization** - Interactive charts and indicators
- **Strategy Testing** - Backtest trading strategies
- **Dashboard** - Portfolio overview and performance metrics
- **Multi-Symbol Support** - Track multiple crypto pairs

## Technology Stack

- **React Native** - Cross-platform mobile framework
- **Expo** - Development platform and tools
- **TypeScript** - Type-safe JavaScript
- **React Navigation** - Navigation library
- **React Native Chart Kit** - Data visualization
- **AsyncStorage** - Local data persistence

## Quick Start

### Prerequisites
- Node.js (v18 or later)
- Expo CLI: `npm install -g @expo/cli`
- iOS Simulator or Android Emulator

### Installation

1. **Navigate to the mobile app directory:**
   ```bash
   cd frontend/mobile
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start the development server:**
   ```bash
   npm start
   ```

4. **Run on your preferred platform:**
   - iOS: Press `i` or run `npm run ios`
   - Android: Press `a` or run `npm run android`
   - Web: Press `w` or run `npm run web`

## Configuration

The app connects to the .NET backend API running on `localhost:8080`. The configuration can be found in:

- `src/config.ts` - Base URLs and configuration
- `app.json` - Expo configuration with environment variables

### API Endpoints
- **Base API**: `http://localhost:8080/api`
- **SignalR Hub**: `ws://localhost:8080/hubs/trading`

## Project Structure

```
src/
├── components/           # Reusable UI components
├── config.ts            # App configuration
├── context/             # React context providers
│   └── AuthContext.tsx  # Authentication state management
├── navigation/          # Navigation configuration
│   └── AppNavigation.tsx # Main navigation setup
├── screens/             # Screen components
│   ├── DashboardScreen.tsx
│   ├── LoginScreen.tsx
│   ├── RegisterScreen.tsx
│   └── StrategyTestScreen.tsx
├── services/            # API services
│   └── api.ts           # HTTP client and API calls
├── types/               # TypeScript type definitions
└── utils/               # Utility functions
```

## Available Scripts

- `npm start` - Start Expo development server
- `npm run android` - Run on Android
- `npm run ios` - Run on iOS
- `npm run web` - Run in web browser
- `gen:types` - Generate TypeScript types (placeholder)

## Backend Integration

This mobile app is designed to work with the MyTrader .NET backend. Make sure the backend is running before starting the mobile app:

```bash
# From the project root
docker-compose up -d
```

The app will automatically connect to the backend API and establish WebSocket connections for real-time updates.

## Development Notes

- Uses Expo SDK 53 with React Native 0.79
- Navigation powered by React Navigation v7
- Real-time updates via SignalR WebSocket
- TypeScript for enhanced development experience
- Responsive design for various screen sizes

## Troubleshooting

1. **Metro bundler issues**: Clear cache with `npx expo start --clear`
2. **Dependency conflicts**: Run `npx expo install --check` to fix version mismatches
3. **Backend connection**: Ensure Docker containers are running and accessible on port 8080
4. **iOS Simulator**: Make sure Xcode and iOS Simulator are installed
5. **Android Emulator**: Ensure Android Studio and AVD are set up
6. **iOS build fails in ReactCodegen with spaces in path**: If your project path contains spaces (e.g., `Personal Documents`), Xcode build scripts may fail. Either move the project to a path without spaces or re-run `pod install` after our included patch to `node_modules/react-native/scripts/react_native_pods_utils/script_phases.rb` which quotes env vars.

## Contributing

1. Follow the existing code style and TypeScript conventions
2. Test on both iOS and Android platforms
3. Update documentation for any new features
4. Ensure real-time features work correctly with the backend
