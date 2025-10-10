# Project Structure & Organization

## Root Level Organization

```
myTrader/
├── backend/                    # .NET solution and projects
├── frontend/                   # All frontend applications
│   ├── web/                   # React web application
│   ├── mobile/                # React Native mobile app
│   └── backoffice/            # Admin dashboard (Vite + React)
├── api-contracts/             # OpenAPI specs and API documentation
├── scripts/                   # Build and deployment scripts
├── docs/                      # Project documentation
└── docker-compose.yml         # Full stack orchestration
```

## Backend Architecture (.NET Clean Architecture)

```
backend/
├── MyTrader.Api/              # Web API layer (controllers, hubs, middleware)
├── MyTrader.Core/             # Domain layer (models, DTOs, interfaces)
├── MyTrader.Services/         # Business logic layer
├── MyTrader.Infrastructure/   # Data access layer (EF Core, external services)
├── MyTrader.Tests/           # Test projects
└── MyTrader.Tools/           # Utility tools and scripts
```

### Backend Layer Responsibilities

- **Api**: Controllers, SignalR hubs, middleware, startup configuration
- **Core**: Domain models, DTOs, service interfaces, enums
- **Services**: Business logic, strategy implementations, authentication
- **Infrastructure**: Database context, repositories, external API clients
- **Tests**: Unit tests, integration tests, test utilities

## Frontend Web Structure (React + Vite)

```
frontend/web/src/
├── components/               # Reusable UI components
├── pages/                   # Route-level page components
├── layouts/                 # Layout wrapper components
├── hooks/                   # Custom React hooks
├── services/                # API clients and external services
├── store/                   # Zustand stores for state management
├── types/                   # TypeScript type definitions
├── utils/                   # Utility functions and helpers
└── styles/                  # Global styles and Tailwind config
```

### Frontend Web Conventions

- **Components**: Organized by feature/domain, use PascalCase
- **Hooks**: Prefix with `use`, handle specific logic concerns
- **Services**: API clients using axios + React Query
- **Store**: Zustand slices for different domains (auth, market, portfolio)
- **Types**: Shared TypeScript interfaces and types

## Frontend Mobile Structure (React Native + Expo)

```
frontend/mobile/src/
├── components/              # Reusable mobile components
├── screens/                 # Screen components for navigation
├── navigation/              # React Navigation setup
├── context/                 # React Context providers
├── services/                # API clients and SignalR connections
├── utils/                   # Mobile-specific utilities
└── types/                   # TypeScript definitions
```

### Mobile App Conventions

- **Screens**: Full-screen components for navigation stack
- **Context**: Global state management using React Context
- **Navigation**: Stack and tab navigators using React Navigation
- **Services**: Shared API logic with web frontend where possible

## Configuration Files

### Backend Configuration
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Dev overrides
- `appsettings.Production.json` - Production settings
- `.env` files for sensitive data

### Frontend Configuration
- `vite.config.ts` - Vite build configuration
- `tailwind.config.js` - Tailwind CSS setup
- `tsconfig.json` - TypeScript compiler options
- `.env` files for environment variables

## Testing Structure

### Backend Tests
```
MyTrader.Tests/
├── Unit/                    # Unit tests for services and logic
├── Integration/             # API integration tests
├── Controllers/             # Controller-specific tests
├── Services/                # Service layer tests
└── TestBase/                # Shared test utilities
```

### Frontend Tests
```
frontend/web/src/
├── **/*.test.tsx           # Component unit tests (co-located)
├── e2e/                    # Playwright end-to-end tests
└── test-utils/             # Testing utilities and mocks
```

## Naming Conventions

### Backend (.NET)
- **Classes**: PascalCase (`UserService`, `MarketController`)
- **Methods**: PascalCase (`GetUserById`, `CalculateRSI`)
- **Properties**: PascalCase (`UserId`, `CurrentPrice`)
- **Fields**: camelCase with underscore prefix (`_dbContext`)
- **Constants**: PascalCase (`DefaultTimeout`)

### Frontend (TypeScript/React)
- **Components**: PascalCase (`LoginForm`, `PriceChart`)
- **Files**: PascalCase for components, camelCase for utilities
- **Variables/Functions**: camelCase (`currentUser`, `fetchMarketData`)
- **Constants**: UPPER_SNAKE_CASE (`API_BASE_URL`)
- **Types/Interfaces**: PascalCase (`User`, `MarketData`)

## File Organization Patterns

### Feature-Based Organization
Group related files by business domain rather than technical type:
```
src/
├── auth/                   # Authentication feature
│   ├── components/
│   ├── hooks/
│   ├── services/
│   └── types/
├── market/                 # Market data feature
└── portfolio/              # Portfolio management
```

### API Contracts
- OpenAPI specifications in `api-contracts/`
- Shared between backend and frontend teams
- Version controlled and documented
- Used for client code generation

## Documentation Standards

- README files in each major directory
- API documentation via Swagger/OpenAPI
- Component documentation using JSDoc
- Architecture decision records (ADRs) in `docs/`