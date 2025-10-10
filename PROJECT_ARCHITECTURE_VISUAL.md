# 📊 MyTrader - Visual Project Architecture & Flow Diagrams

## 🏛️ System Architecture Overview

```mermaid
graph TB
    subgraph "Frontend Layer"
        A[📱 React Native Mobile]
        B[🌐 React Web App]
        C[⚙️ Admin Panel]
    end

    subgraph "API Gateway"
        D[🔗 ASP.NET Core API]
        E[📡 SignalR Hubs]
    end

    subgraph "Business Logic"
        F[🏢 Core Services]
        G[📊 Market Data Services]
        H[🏆 Competition Services]
        I[💼 Portfolio Services]
    end

    subgraph "Data Layer"
        J[(🗄️ PostgreSQL)]
        K[📈 Yahoo Finance API]
        L[🪙 Binance WebSocket]
        M[🏛️ BIST API]
    end

    A --> D
    B --> D
    C --> D
    D --> E
    D --> F
    F --> G
    F --> H
    F --> I
    G --> K
    G --> L
    G --> M
    F --> J
    E -.-> A
    E -.-> B
    E -.-> C
```

## 📱 Mobile App Screen Flow

```mermaid
graph LR
    subgraph "Authentication Flow"
        A1[🚪 Splash Screen] --> A2[🔑 Login/Register]
        A2 --> A3[✅ Auth Success]
    end

    subgraph "Main Navigation"
        A3 --> B1[📊 Dashboard]
        A3 --> B2[🏆 Leaderboard]
        A3 --> B3[💼 Portfolio]
    end

    subgraph "Dashboard Components"
        B1 --> C1[📈 Market Overview]
        B1 --> C2[🔢 Asset Classes]
        B1 --> C3[⚡ Real-time Prices]
    end

    subgraph "Competition Features"
        B2 --> D1[🏅 User Ranking]
        B2 --> D2[🎯 Competition Entry]
        B2 --> D3[🏆 Prize Pool]
    end

    subgraph "Portfolio Management"
        B3 --> E1[💹 Holdings]
        B3 --> E2[📊 Performance]
        B3 --> E3[📋 Transactions]
    end
```

## 🔄 Real-time Data Flow

```mermaid
sequenceDiagram
    participant M as 📱 Mobile App
    participant W as 🌐 Web App
    participant S as 📡 SignalR Hub
    participant A as 🔗 API Controller
    participant D as 📊 Data Service
    participant Y as 📈 Yahoo Finance
    participant B as 🪙 Binance

    Note over M,B: Real-time Price Update Flow

    B->>D: WebSocket Price Update
    Y->>D: REST API Price Update
    D->>A: Process & Validate Data
    A->>S: Broadcast to Hub
    S->>M: Push to Mobile
    S->>W: Push to Web

    Note over M,W: User sees live prices
```

## 🗂️ File Structure Tree

```
myTrader/
│
├── 🔧 backend/
│   ├── MyTrader.Api/
│   │   ├── 🎯 Controllers/
│   │   │   ├── AuthController.cs ────────── 🔑 Authentication
│   │   │   ├── DashboardController.cs ───── 📊 Dashboard Data
│   │   │   ├── MarketDataController.cs ──── 📈 Market Information
│   │   │   ├── CompetitionController.cs ─── 🏆 Competitions
│   │   │   └── HealthController.cs ─────── ❤️ System Health
│   │   ├── 📡 Hubs/
│   │   │   └── MarketDataHub.cs ─────────── ⚡ Real-time Communication
│   │   ├── 🛠️ Services/
│   │   │   ├── MultiAssetDataBroadcastService.cs
│   │   │   ├── DatabaseSeederService.cs
│   │   │   └── MockServices/
│   │   └── Program.cs ──────────────────── 🚀 Application Entry
│   │
│   ├── MyTrader.Core/
│   │   ├── 📋 Models/ ──────────────────── 🗃️ Data Models
│   │   ├── 📦 DTOs/ ────────────────────── 📤 Data Transfer Objects
│   │   ├── 🔌 Interfaces/ ─────────────── 📜 Service Contracts
│   │   └── ⚙️ Services/ ────────────────── 🏢 Business Logic
│   │
│   ├── MyTrader.Infrastructure/
│   │   ├── 🗄️ Data/ ────────────────────── 💾 Database Context
│   │   ├── 🔗 Services/ ────────────────── 🌐 External APIs
│   │   └── 🧩 Extensions/ ──────────────── 🔧 Service Extensions
│   │
│   └── MyTrader.Services/
│       ├── 📊 Market/ ──────────────────── 📈 Market Data Services
│       ├── 💼 Portfolio/ ───────────────── 💹 Portfolio Management
│       ├── 🏆 Gamification/ ────────────── 🎮 Competitions & Rewards
│       └── 📈 Analytics/ ───────────────── 📊 Performance Analytics
│
├── 🌐 frontend/
│   ├── web/ ────────────────────────────── 🖥️ React Web Application
│   │   ├── src/
│   │   │   ├── 🧩 components/
│   │   │   │   ├── dashboard/
│   │   │   │   │   ├── MarketOverview.tsx ──── 📈 Market Summary
│   │   │   │   │   ├── LeaderboardSection.tsx ─ 🏆 Rankings Display
│   │   │   │   │   └── SmartOverviewHeader.tsx ─ 📊 Header Info
│   │   │   │   ├── AuthPrompt.tsx ────────── 🔑 Login Modal
│   │   │   │   ├── ErrorBoundary.tsx ───── 🛡️ Crash Prevention
│   │   │   │   ├── Login.tsx ──────────── 👤 Login Form
│   │   │   │   └── Register.tsx ────────── 📝 Registration
│   │   │   ├── 🔗 services/
│   │   │   │   ├── api.ts ──────────────── 🌐 API Client
│   │   │   │   ├── authService.ts ────── 🔐 Authentication
│   │   │   │   ├── marketDataService.ts ─ 📊 Market Data
│   │   │   │   └── websocketService.ts ── ⚡ Real-time Connection
│   │   │   ├── 🎣 hooks/ ────────────────── ⚛️ React Custom Hooks
│   │   │   ├── 🏪 store/ ────────────────── 📦 State Management
│   │   │   ├── 🔧 utils/
│   │   │   │   ├── dataValidation.ts ──── ✅ Safe Data Handling
│   │   │   │   └── navigation.ts ──────── 🧭 Safe Navigation
│   │   │   ├── App.tsx ──────────────────── 🏠 Main Component
│   │   │   └── index.tsx ───────────────── 🚀 Entry Point
│   │   └── 🧪 e2e/ ──────────────────────── 🔬 End-to-End Tests
│   │
│   ├── mobile/ ─────────────────────────── 📱 React Native App
│   │   ├── src/
│   │   │   ├── 🧩 components/
│   │   │   │   ├── dashboard/
│   │   │   │   │   ├── AssetClassAccordion.tsx ── 📂 Asset Categories
│   │   │   │   │   ├── CompactLeaderboard.tsx ─── 🏆 Quick Rankings
│   │   │   │   │   └── SmartOverviewHeader.tsx ── 📱 Mobile Header
│   │   │   │   ├── leaderboard/
│   │   │   │   │   ├── CompetitionEntry.tsx ──── 🎯 Competition Cards
│   │   │   │   │   └── UserRankCard.tsx ──────── 👤 User Ranking
│   │   │   │   └── ErrorNotification.tsx ──── 🚨 Error Display
│   │   │   ├── 📺 screens/
│   │   │   │   ├── DashboardScreen.tsx ──────── 🏠 Main Dashboard
│   │   │   │   ├── EnhancedLeaderboardScreen.tsx ─ 🏆 Competition Screen
│   │   │   │   └── PortfolioScreen.tsx ─────── 💼 Portfolio View
│   │   │   ├── 🌐 context/
│   │   │   │   └── PriceContext.tsx ───────── 💰 Price Management
│   │   │   ├── 🔗 services/
│   │   │   │   ├── api.ts ──────────────────── 📱 Mobile API
│   │   │   │   └── websocketService.ts ────── ⚡ Mobile WebSocket
│   │   │   ├── 🔧 utils/
│   │   │   │   └── errorHandling.ts ───────── 🛡️ Error System
│   │   │   └── config.ts ─────────────────── ⚙️ Configuration
│   │   ├── App.tsx ──────────────────────── 📱 Mobile Entry
│   │   └── index.ts ─────────────────────── 📲 Expo Entry
│   │
│   └── backoffice/ ─────────────────────── ⚙️ Admin Panel
│
└── 📚 documentation/
    ├── api-contracts/ ─────────────────── 📜 API Specs
    ├── monitoring/ ───────────────────── 📈 Monitoring Docs
    ├── testing/ ──────────────────────── 🧪 Test Documentation
    └── deployment/ ───────────────────── 🚀 Deployment Guides
```

## 📱 Mobile Navigation Structure

```mermaid
graph TB
    subgraph "Bottom Tab Navigation"
        A[📊 Dashboard<br/>DashboardScreen.tsx]
        B[🏆 Competition<br/>EnhancedLeaderboardScreen.tsx]
        C[💼 Portfolio<br/>PortfolioScreen.tsx]
    end

    subgraph "Dashboard Components"
        A --> A1[📈 SmartOverviewHeader]
        A --> A2[📂 AssetClassAccordion]
        A --> A3[🏆 CompactLeaderboard]

        A2 --> A2a[📈 Stocks<br/>BIST/NASDAQ]
        A2 --> A2b[🪙 Crypto<br/>BTC/ETH/etc]
        A2 --> A2c[🏛️ Other Assets]
    end

    subgraph "Competition Components"
        B --> B1[👤 UserRankCard]
        B --> B2[🎯 CompetitionEntry[]]
        B --> B3[🏅 Global Rankings]

        B2 --> B2a[💰 Prize Pool Info]
        B2 --> B2b[👥 Participant Count]
        B2 --> B2c[⏰ Time Remaining]
    end

    subgraph "Portfolio Components"
        C --> C1[💹 Current Holdings]
        C --> C2[📊 Performance Charts]
        C --> C3[📋 Transaction History]
    end
```

## 🔄 API Endpoint Structure

```mermaid
graph LR
    subgraph "API Routes - localhost:5002/api/"
        A[🔑 /auth]
        B[📊 /dashboard]
        C[📈 /market-data]
        D[🏆 /competition]
        E[❤️ /health]
    end

    subgraph "Auth Endpoints"
        A --> A1[POST /login]
        A --> A2[POST /register]
        A --> A3[POST /refresh]
    end

    subgraph "Dashboard Endpoints"
        B --> B1[GET /overview]
        B --> B2[GET /user-summary]
    end

    subgraph "Market Data Endpoints"
        C --> C1[GET /symbols]
        C --> C2[GET /prices]
        C --> C3[GET /history]
    end

    subgraph "Competition Endpoints"
        D --> D1[GET /leaderboard]
        D --> D2[GET /entries]
        D --> D3[GET /user-rank]
    end

    subgraph "Health Endpoints"
        E --> E1[GET /database]
        E --> E2[GET /connections]
    end
```

## 🚨 Critical Issues Resolution Map

```mermaid
flowchart TD
    subgraph "Phase 1: Analysis & Testing ✅"
        P1A[📋 Business Analysis] --> P1B[🧪 Test Framework]
    end

    subgraph "Phase 2: Backend Fixes ✅"
        P2A[🗄️ Database Architecture] --> P2B[🔧 API Stability]
    end

    subgraph "Phase 3: Frontend Fixes ✅"
        P3A[🌐 Web App Fixes] --> P3B[📱 Mobile App Fixes]
        P3A1[CompetitionEntry.tsx:155<br/>❌→✅ Fixed undefined slice]
        P3B1[EnhancedLeaderboardScreen.tsx:61<br/>❌→✅ Fixed non-array data]
    end

    subgraph "Phase 4: Monitoring ✅"
        P4A[📈 Health Checks] --> P4B[🚨 Alerting System]
    end

    subgraph "Phase 5: QA & Validation ✅"
        P5A[🧪 Manual Testing] --> P5B[🔄 Integration Tests]
    end

    subgraph "Current Issues ⚠️"
        CI1[⚠️ Backend Compilation<br/>Program.cs monitoring conflicts]
        CI2[⚠️ Database Connection<br/>PostgreSQL configuration]
        CI3[⚠️ API Consistency<br/>Data structure validation]
    end

    P1A --> P1B --> P2A --> P2B --> P3A --> P3B --> P4A --> P4B --> P5A --> P5B
    P3A --> P3A1
    P3B --> P3B1
    P5B --> CI1
    P5B --> CI2
    P5B --> CI3
```

## 🎯 User Journey Visual Flow

```mermaid
journey
    title MyTrader User Journey

    section Guest User
        Visit App: 3: Guest
        Browse Market Data: 4: Guest
        Hit Feature Lock: 2: Guest
        See Auth Prompt: 3: Guest

    section Registration
        Fill Registration Form: 3: User
        Email Verification: 4: User
        Account Created: 5: User

    section Authenticated Experience
        Login Success: 5: User
        View Dashboard: 5: User
        Check Real-time Prices: 5: User
        Enter Competition: 4: User
        View Portfolio: 4: User
        Check Rankings: 4: User
        Receive Notifications: 3: User

    section Advanced Features
        Analyze Performance: 4: User
        Compare with Others: 4: User
        Win Competition: 5: User
        Earn Achievements: 5: User
```

---

## 🏆 Summary

Bu görsel mimari MyTrader platformunun:
- **📱 Multi-platform** (Web + Mobile) yapısını
- **⚡ Real-time** veri akışını
- **🏆 Competition** sistemini
- **📊 Market data** entegrasyonunu
- **🔧 Fixed issues** ve mevcut durumu

göstermektedir. Platform, comprehensive trading yarışma deneyimi sunmak üzere tasarlanmıştır.