# 📊 MyTrader - Project Structure & Menu Architecture

## 🏗️ Overall Project Architecture

```
myTrader/
├── 🔧 backend/                    # .NET Core API Backend
│   ├── MyTrader.Api/              # Main API Layer
│   │   ├── Controllers/           # API Controllers
│   │   │   ├── AuthController.cs          # Authentication
│   │   │   ├── DashboardController.cs     # Main Dashboard
│   │   │   ├── MarketDataController.cs    # Market Data
│   │   │   ├── PricesController.cs        # Price Data
│   │   │   ├── SymbolsController.cs       # Symbol Management
│   │   │   ├── GamificationController.cs  # Leaderboards & Competitions
│   │   │   ├── CompetitionController.cs   # Competition Features
│   │   │   ├── PerformanceController.cs   # Analytics
│   │   │   └── HealthController.cs        # System Health
│   │   ├── Hubs/                  # SignalR Real-time Communication
│   │   │   └── MarketDataHub.cs           # Real-time Market Data
│   │   ├── Services/              # Business Logic
│   │   │   ├── MultiAssetDataBroadcastService.cs
│   │   │   ├── DatabaseSeederService.cs
│   │   │   └── MockServices/
│   │   └── Program.cs             # Application Entry Point
│   ├── MyTrader.Core/             # Domain Layer
│   │   ├── Models/                # Data Models
│   │   ├── DTOs/                  # Data Transfer Objects
│   │   ├── Interfaces/            # Service Contracts
│   │   └── Services/              # Core Business Logic
│   ├── MyTrader.Infrastructure/   # Data Access Layer
│   │   ├── Data/                  # Database Context & Configurations
│   │   ├── Services/              # External API Services
│   │   └── Extensions/            # Service Extensions
│   └── MyTrader.Services/         # Application Services
│       ├── Market/                # Market Data Services
│       ├── Portfolio/             # Portfolio Management
│       ├── Gamification/          # Competition & Rewards
│       └── Analytics/             # Performance Analytics
│
├── 🌐 frontend/                   # Frontend Applications
│   ├── web/                       # React Web Application
│   │   ├── src/
│   │   │   ├── components/        # Reusable UI Components
│   │   │   │   ├── dashboard/     # Dashboard Specific Components
│   │   │   │   │   ├── MarketOverview.tsx
│   │   │   │   │   ├── LeaderboardSection.tsx
│   │   │   │   │   └── SmartOverviewHeader.tsx
│   │   │   │   ├── AuthPrompt.tsx         # Authentication Modal
│   │   │   │   ├── ErrorBoundary.tsx      # Error Handling
│   │   │   │   ├── Login.tsx              # Login Component
│   │   │   │   ├── Register.tsx           # Registration Component
│   │   │   │   └── Footer.tsx             # Footer Component
│   │   │   ├── services/          # API & External Services
│   │   │   │   ├── api.ts                 # Main API Client
│   │   │   │   ├── authService.ts         # Authentication
│   │   │   │   ├── marketDataService.ts   # Market Data
│   │   │   │   └── websocketService.ts    # Real-time Connection
│   │   │   ├── hooks/             # React Custom Hooks
│   │   │   ├── store/             # State Management
│   │   │   ├── utils/             # Utility Functions
│   │   │   │   ├── dataValidation.ts      # Safe Data Handling
│   │   │   │   └── navigation.ts          # Safe Navigation
│   │   │   ├── App.tsx            # Main Application Component
│   │   │   └── index.tsx          # Application Entry Point
│   │   ├── e2e/                   # End-to-End Tests
│   │   ├── public/                # Static Assets
│   │   └── package.json           # Dependencies & Scripts
│   │
│   ├── mobile/                    # React Native Mobile App
│   │   ├── src/
│   │   │   ├── components/        # Mobile UI Components
│   │   │   │   ├── dashboard/     # Dashboard Components
│   │   │   │   │   ├── AssetClassAccordion.tsx
│   │   │   │   │   ├── CompactLeaderboard.tsx
│   │   │   │   │   └── SmartOverviewHeader.tsx
│   │   │   │   ├── leaderboard/   # Competition Components
│   │   │   │   │   ├── CompetitionEntry.tsx   # 🔧 Fixed Crash Issues
│   │   │   │   │   └── UserRankCard.tsx
│   │   │   │   └── ErrorNotification.tsx
│   │   │   ├── screens/           # Mobile Screens
│   │   │   │   ├── DashboardScreen.tsx        # Main Dashboard
│   │   │   │   ├── EnhancedLeaderboardScreen.tsx  # 🔧 Fixed Array Issues
│   │   │   │   └── PortfolioScreen.tsx
│   │   │   ├── context/           # React Context Providers
│   │   │   │   └── PriceContext.tsx           # Price Data Management
│   │   │   ├── services/          # Mobile API Services
│   │   │   │   ├── api.ts                     # Mobile API Client
│   │   │   │   └── websocketService.ts        # Mobile WebSocket
│   │   │   ├── utils/             # Mobile Utilities
│   │   │   │   └── errorHandling.ts           # 🔧 Mobile Error System
│   │   │   └── config.ts          # Configuration
│   │   ├── App.tsx                # Mobile App Entry Point
│   │   ├── index.ts               # Expo Entry Point
│   │   └── package.json           # Mobile Dependencies
│   │
│   └── backoffice/                # Admin Panel (Future)
│       └── src/                   # Admin Components
│
└── 📋 documentation/              # Project Documentation
    ├── api-contracts/             # API Specifications
    ├── monitoring/                # System Monitoring Docs
    ├── testing/                   # Testing Documentation
    └── deployment/                # Deployment Guides
```

## 🗂️ Mobile App Menu Structure & Navigation

### **Bottom Navigation Bar (Main Menu)**
```
┌─────────────────────────────────────────┐
│  📊 Dashboard  │  🏆 Competition  │  💼 Portfolio  │
│      (Home)    │  (Leaderboard)   │   (Holdings)    │
└─────────────────────────────────────────┘
```

**Navigation Details:**
- **📊 Dashboard** (`DashboardScreen.tsx`)
  - Market Overview with Asset Classes
  - Real-time Price Data
  - Quick Actions & Market Status

- **🏆 Competition** (`EnhancedLeaderboardScreen.tsx`)
  - User Rankings & Leaderboard
  - Competition Entries
  - Achievement System

- **💼 Portfolio** (`PortfolioScreen.tsx`)
  - User Holdings
  - Performance Analytics
  - Transaction History

### **Dashboard Screen Components**
```
DashboardScreen.tsx
├── SmartOverviewHeader          # Market Summary & User Info
├── AssetClassAccordion          # Expandable Asset Categories
│   ├── 📈 Stocks (BIST/NASDAQ)
│   ├── 🪙 Crypto Currencies
│   └── 🏛️ Other Assets
└── CompactLeaderboard          # Quick Competition View
```

### **Competition/Leaderboard Screen**
```
EnhancedLeaderboardScreen.tsx
├── UserRankCard                # Current User Ranking
├── CompetitionEntry[]          # Competition Listings
│   ├── Prize Information
│   ├── Participant Count
│   └── Time Remaining
└── Global Rankings             # Top Performers
```

## 🔌 Real-time Data Flow Architecture

### **WebSocket/SignalR Connection Flow**
```
📱 Mobile App                    🌐 Web App
        │                              │
        └─────────┬────────────────────┘
                  │
                  ▼
            🔗 WebSocket Service
                  │
                  ▼
            🏢 Backend SignalR Hub
            (MarketDataHub.cs)
                  │
                  ▼
            📊 Market Data Services
                  │
        ┌─────────┼─────────┐
        │         │         │
        ▼         ▼         ▼
    📈 Yahoo   🪙 Binance  🏛️ BIST
    Finance    WebSocket   API
```

### **API Endpoint Structure**
```
http://localhost:5002/api/
├── auth/                    # Authentication
│   ├── login               # User Login
│   ├── register           # User Registration
│   └── refresh            # Token Refresh
├── dashboard/              # Dashboard Data
│   ├── overview           # Market Overview
│   └── user-summary       # User Portfolio Summary
├── market-data/            # Market Information
│   ├── symbols            # Available Symbols
│   ├── prices             # Current Prices
│   └── history            # Historical Data
├── competition/            # Competition System
│   ├── leaderboard        # Rankings
│   ├── entries            # Competition Entries
│   └── user-rank          # User's Current Rank
└── health/                 # System Health
    ├── database           # DB Connection Status
    └── connections        # WebSocket Health
```

## 🚨 Critical Issue Resolution Status

### **✅ Fixed Issues (Phase 1-3)**
- **CompetitionEntry.tsx:155** - Undefined slice error fixed with safe array handling
- **EnhancedLeaderboardScreen.tsx:61** - Non-array data handling implemented
- **SignalR Connection Stability** - Enhanced error handling and reconnection
- **Frontend Error Boundaries** - Comprehensive crash prevention
- **Mobile Navigation** - Safe navigation with error boundaries

### **⚠️ Current Issues (In Progress)**
- **Backend Compilation Errors** - Program.cs monitoring services conflicts
- **Database Connection** - PostgreSQL connection configuration
- **API Endpoint Consistency** - Data structure validation

### **📊 System Health Dashboard**
- **Monitoring**: Health checks for database, API, WebSocket connections
- **Alerting**: Automated alerts for system failures
- **Performance**: Response time tracking and SLO monitoring
- **Testing**: Comprehensive automated test coverage

## 🎯 User Journey Flow

### **Guest User Experience**
```
🏠 Landing → 👀 Browse Market Data → 🔒 Hit Feature Lock → 📝 Register/Login
```

### **Authenticated User Experience**
```
🏠 Dashboard → 📊 Market Data → 🏆 Competitions → 💼 Portfolio → 🎖️ Achievements
```

### **Real-time Features**
- **Live Price Updates** via SignalR/WebSocket
- **Competition Ranking Changes** in real-time
- **Market Status Notifications** (open/closed)
- **Portfolio Value Updates** as prices change

---

**🏆 This structure supports a comprehensive trading competition platform with real-time market data, user competitions, and portfolio management across both web and mobile platforms.**