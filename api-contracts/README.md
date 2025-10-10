# MyTrader API Contracts - Unified Specification v1.1

**Contract Authority**: API Contract Governor
**Effective Date**: 2024-09-24
**Implementation Priority**: Based on RICE Scoring Analysis

This directory contains the comprehensive API contract definitions for the MyTrader trading platform, addressing critical connectivity issues, event naming mismatches, and API versioning inconsistencies identified through Business Analyst findings and Product Owner prioritization.

## 🚨 Critical Issues Addressed

| Issue | RICE Score | Phase | Status |
|-------|------------|-------|---------|
| **WebSocket Port Fix** | 576 | Immediate | 🔴 Critical |
| **Event Naming Standardization** | 432 | Phase 1 | 🟡 High Priority |
| **API Versioning Consistency** | 288 | Phase 2 | 🟢 Medium Priority |
| **Volume Leaders Endpoint** | 216 | Phase 2 | 🟢 New Feature |

## 📁 Contract Artifacts

### Core Specifications
- **`unified-openapi.yaml`** - 🆕 Unified OpenAPI 3.1.0 specification with standardized endpoints
- **`websocket-contract.yaml`** - 🆕 Complete WebSocket hub contract with event standardization
- **`openapi.yaml`** - Legacy OpenAPI specification (to be deprecated)

### Migration & Governance
- **`migration-strategy.md`** - 🆕 Comprehensive migration strategy with backward compatibility matrix
- **`validation-rules.yaml`** - 🆕 Contract validation rules and testing strategies

### Testing & Integration
- **`MyTrader-Unified-API.postman_collection.json`** - 🆕 Complete test collection with performance tests
- **`MyTrader-API.postman_collection.json`** - Legacy Postman collection (to be deprecated)

### Documentation
- **`signalr-hubs.md`** - WebSocket hub documentation (legacy)
- **`compatibility-assessment.md`** - API compatibility analysis
- **`security-analysis.md`** - Security analysis of the API

## 🚀 Quick Start

### 1. Port Configuration Fix (IMMEDIATE)
```bash
# OLD - INCORRECT PORT
http://localhost:8080/api/market-data/overview

# NEW - CORRECTED PORT (use immediately)
http://localhost:5002/api/v1/market-data/overview
```

### 2. WebSocket Connection Fix (IMMEDIATE)
```javascript
// OLD - INCORRECT
const connection = new HubConnectionBuilder()
    .withUrl('ws://localhost:8080/hubs/trading')
    .build();

// NEW - CORRECTED
const connection = new HubConnectionBuilder()
    .withUrl('ws://localhost:5002/hubs/market-data')
    .build();
```

### 3. Event Name Standardization (Phase 1)
```javascript
// OLD - Legacy event names (remove after 90 days)
connection.on('ReceivePriceUpdate', handlePriceUpdate);
connection.on('ReceiveMarketData', handleMarketData);

// NEW - Standardized event names (implement immediately)
connection.on('PriceUpdate', handlePriceUpdate);
connection.on('BatchPriceUpdate', handleMarketData);
```

## 📊 New Endpoint: Volume Leaders

**Priority**: RICE Score 216 - Phase 2 Implementation

```bash
# Get top 8 highest volume assets per asset class
GET /api/v1/market-data/top-by-volume?perClass=8&timeframe=24h

# Response time requirement: <100ms (95th percentile)
```

## 🔄 Migration Timeline

### Phase 1: Critical Fixes (Week 1)
- ✅ Port configuration standardization (5002)
- ✅ WebSocket event naming standardization
- ✅ Backward compatibility layer implementation

### Phase 2: Feature Completion (Weeks 2-4)
- 🔄 API versioning unification (/api/v1/*)
- 🔄 Volume leaders endpoint implementation
- 🔄 Frontend client migration

### Phase 3: Legacy Cleanup (Weeks 5-16)
- ⏳ Legacy endpoint deprecation (90-day notice)
- ⏳ Client migration completion
- ⏳ Backward compatibility removal

## 🏗️ Implementation Guidelines

### Backend Teams

1. **Update Controller Routes**
   ```csharp
   // NEW - Unified versioning pattern
   [Route("api/v1/market-data")]
   public class MarketDataController : ControllerBase

   // Add to Program.cs
   app.MapHub<MarketDataHub>("/hubs/market-data");
   ```

2. **WebSocket Event Names**
   ```csharp
   // NEW - Standardized naming
   await Clients.All.SendAsync("PriceUpdate", marketData);

   // TEMPORARY - Dual emission for backward compatibility
   await Clients.All.SendAsync("ReceivePriceUpdate", marketData);
   ```

### Frontend Teams

1. **Configuration Updates**
   ```typescript
   // Web & Mobile - Update immediately
   const API_BASE_URL = 'http://localhost:5002/api/v1';
   const WS_BASE_URL = 'ws://localhost:5002/hubs/market-data';
   ```

2. **Event Handler Migration**
   ```typescript
   // Update WebSocket event handlers
   connection.on('PriceUpdate', (data) => {
     // Handle standardized event
   });
   ```

## 🧪 Testing & Validation

### Import Test Collections
1. **Unified API Testing**:
   - Import `MyTrader-Unified-API.postman_collection.json`
   - Run performance tests (Volume Leaders <100ms requirement)
   - Validate contract compliance

2. **WebSocket Testing**:
   ```bash
   # Test WebSocket connectivity
   wscat -c ws://localhost:5002/hubs/market-data

   # Validate event names (no "Receive" prefix)
   ```

### Automated Validation
```bash
# Run contract validation
spectral lint unified-openapi.yaml

# Check breaking changes
oasdiff diff old-spec.yaml unified-openapi.yaml --breaking-only

# Performance validation
newman run MyTrader-Unified-API.postman_collection.json
```

## 🔐 Security & Authentication

### API Base URLs
- **Development**: `http://localhost:5002/api/v1` (CORRECTED PORT)
- **Staging**: `https://staging-api.mytrader.com/api/v1`
- **Production**: `https://api.mytrader.com/api/v1`

### WebSocket URLs
- **Development**: `ws://localhost:5002/hubs/market-data` (CORRECTED PORT)
- **Staging**: `wss://staging-api.mytrader.com/hubs/market-data`
- **Production**: `wss://api.mytrader.com/hubs/market-data`

### Authentication
- **Public Endpoints**: Market data, symbols, health checks
- **Protected Endpoints**: User profiles, trading operations, subscriptions
- **JWT Format**: Bearer token in Authorization header
- **Token Expiry**: 24 hours maximum

## 📈 Performance Requirements

| Endpoint | Target Response Time | Priority |
|----------|---------------------|----------|
| `/health` | <10ms (95th percentile) | Critical |
| `/market-data/top-by-volume` | <100ms (95th percentile) | **RICE Requirement** |
| `/market-data/realtime/*` | <50ms (95th percentile) | High |
| `/market-data/batch` | <200ms (95th percentile) | Medium |

## 🚦 Breaking Change Policy

### Forbidden Changes
- ❌ Removing existing endpoints
- ❌ Changing HTTP methods
- ❌ Removing required parameters
- ❌ Changing response data types
- ❌ Removing WebSocket events without deprecation

### Allowed Changes
- ✅ Adding optional parameters
- ✅ Adding new response fields
- ✅ Adding new endpoints
- ✅ Adding new WebSocket events
- ✅ Improving error messages

## 📞 Support & Escalation

### Contract Governance
- **API Contract Governor**: Primary authority for all contract decisions
- **Merge Blocking**: Breaking changes automatically blocked
- **Review Cycle**: Weekly contract reviews
- **Violation Response**: Immediate rollback for breaking changes

### Team Contacts
- **Backend Team**: API implementation and WebSocket hubs
- **Frontend Web Team**: React application migration
- **Frontend Mobile Team**: React Native application migration
- **DevOps Team**: Infrastructure and port configuration

## 📋 Migration Checklist

### Phase 1 (Week 1) - CRITICAL
- [ ] Update all environment configurations to use port 5002
- [ ] Deploy dual emission WebSocket events
- [ ] Test cross-environment connectivity
- [ ] Validate mobile app WebSocket connection

### Phase 2 (Weeks 2-4)
- [ ] Implement volume leaders endpoint with <100ms target
- [ ] Migrate all controllers to `/api/v1/` pattern
- [ ] Update frontend clients to use new event names
- [ ] Add deprecation warnings to legacy endpoints

### Phase 3 (Weeks 5-16)
- [ ] Monitor legacy endpoint usage (target: 0%)
- [ ] Complete client migration validation
- [ ] Remove backward compatibility layers
- [ ] Archive migration artifacts

## 🏆 Success Criteria

### Phase 1 Success
- 100% of connections use port 5002
- 0% increase in WebSocket connection errors
- Mobile app receives both old and new event names
- Web app connects without 8080 dependency

### Phase 2 Success
- 90% of API calls use `/api/v1/` endpoints
- Volume leaders endpoint <100ms 95th percentile response
- Frontend clients use standardized event names
- Legacy endpoints show deprecation warnings

### Final Success
- 0% usage of legacy endpoints
- 100% client migration completion
- Performance improvements measured and documented
- Clean codebase without migration artifacts

## 🏗️ Architecture Overview

### Hybrid Access Model

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Public Web    │    │   Authenticated  │    │   Trading       │
│   Dashboard     │    │   User Portal    │    │   Operations    │
├─────────────────┤    ├──────────────────┤    ├─────────────────┤
│ • Market Data   │    │ • User Profile   │    │ • Place Orders  │
│ • Price Updates │    │ • Portfolio View │    │ • Cancel Orders │
│ • Symbol Search │    │ • Trade History  │    │ • Real-time P&L │
│ • Top Movers    │    │ • Settings       │    │ • Risk Mgmt     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
      Anonymous              JWT Bearer              JWT Bearer
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                    MyTrader API Gateway                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ Public Endpoints│  │ Protected REST  │  │ SignalR Hubs    │  │
│  │ • Market Data   │  │ • User Data     │  │ • Trading       │  │
│  │ • Symbols       │  │ • Portfolio     │  │ • Portfolio     │  │
│  │ • Real-time     │  │ • Trading       │  │ • Market Data   │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Access Patterns

#### 🌍 Public Access (No Authentication)
- **Market Data**: Real-time prices, historical data, market overview
- **Symbol Discovery**: Search and browse available instruments
- **WebSocket**: Live market data feeds via SignalR
- **Rate Limits**: 1000 requests/hour per IP

#### 🔒 Authenticated Access (JWT Required)
- **User Management**: Profile, settings, session management
- **Portfolio**: Holdings, performance, transactions
- **Trading**: Order placement, cancellation, trade history
- **Rate Limits**: 5000 requests/hour per user

## 📊 API Endpoints Summary

### Public Endpoints (No Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | API health check |
| GET | `/info` | API information and discovery |
| GET | `/symbols` | Get tracked symbols |
| GET | `/symbols/by-asset-class/{class}` | Symbols by asset class |
| GET | `/symbols/search` | Search symbols |
| GET | `/symbols/popular` | Popular symbols |
| GET | `/market-data/overview` | Market overview |
| GET | `/market-data/realtime/{symbolId}` | Real-time market data |
| POST | `/market-data/batch` | Batch market data |
| GET | `/market-data/historical/{symbolId}` | Historical data |
| GET | `/market-data/top-movers` | Top gainers/losers |
| GET | `/market-data/top-by-volume` | **NEW** - Volume leaders |
| GET | `/market-data/crypto` | Cryptocurrency data |
| GET | `/market-data/bist` | BIST stock data |
| GET | `/market-data/nasdaq` | NASDAQ stock data |

### Authenticated Endpoints (JWT Required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/register` | User registration |
| POST | `/auth/login` | User authentication |
| POST | `/auth/logout` | User logout |
| GET/PUT | `/auth/me` | User profile |
| GET | `/auth/sessions` | Active sessions |
| POST | `/symbols` | Create symbol |
| PATCH | `/symbols/{id}` | Update symbol tracking |
| POST | `/market-data/subscribe` | Subscribe to updates |

### SignalR Hubs

| Hub | URL | Access | Purpose |
|-----|-----|--------|---------|
| MarketDataHub | `/hubs/market-data` | Public | **UNIFIED** - All real-time data |
| TradingHub | `/hubs/trading` | **DEPRECATED** | Redirects to market-data |
| PortfolioHub | `/hubs/portfolio` | Auth | Portfolio updates |

## 📈 Monitoring and Analytics

### Key Metrics to Track
- **Port migration progress**: Connections using 5002 vs 8080
- **Event naming adoption**: New event names vs legacy
- **API versioning migration**: /api/v1/* vs /api/* usage
- **Volume leaders performance**: <100ms response time compliance
- **WebSocket connection health**: Connection success/failure rates

### Critical Alerts
```yaml
Critical (Immediate Response):
  - WebSocket port 8080 usage > 0% (should be 0 after Phase 1)
  - Volume leaders endpoint > 100ms response time
  - API response time > 5 seconds
  - Authentication failure rate > 10%

High Priority (1 hour response):
  - Legacy event name usage > 5%
  - /api/* (non-versioned) endpoint usage > 10%
  - WebSocket connection failures > 20%

Medium Priority (4 hour response):
  - Deprecation warning header missing on legacy endpoints
  - Client migration progress stalled
  - Performance regression detected
```

## 🛣️ Implementation Roadmap

### ✅ Completed (Current)
- Unified OpenAPI specification design
- WebSocket contract standardization
- Migration strategy documentation
- Validation rules and testing framework
- Comprehensive Postman collection

### 🚧 Phase 1: Critical Fixes (Week 1)
- [ ] **Port standardization to 5002**
- [ ] **WebSocket event dual emission**
- [ ] **Hub consolidation to /hubs/market-data**
- [ ] Backend deployment with backward compatibility
- [ ] Frontend configuration updates

### 🔄 Phase 2: Feature Implementation (Weeks 2-4)
- [ ] **Volume leaders endpoint** (<100ms performance requirement)
- [ ] **API versioning migration** to /api/v1/* pattern
- [ ] **Frontend event name migration**
- [ ] Deprecation warnings on legacy endpoints
- [ ] Performance monitoring setup

### ⏳ Phase 3: Legacy Cleanup (Weeks 5-16)
- [ ] **90-day deprecation notices** for legacy endpoints
- [ ] **Client migration completion** tracking
- [ ] **Legacy code removal** after deprecation period
- [ ] **Performance optimization** and monitoring
- [ ] **Documentation finalization**

## 🤝 Contributing

### API Contract Changes
1. **Update unified OpenAPI specification first**
2. **Run breaking change detection** with validation rules
3. **Implement backward-compatible changes only**
4. **Add comprehensive tests** to Postman collection
5. **Update migration documentation**
6. **Validate security implications**

### Development Workflow
```bash
# 1. Validate contract changes
spectral lint unified-openapi.yaml
oasdiff diff old-spec.yaml unified-openapi.yaml --breaking-only

# 2. Test implementation
newman run MyTrader-Unified-API.postman_collection.json

# 3. Update documentation
# Update migration-strategy.md, validation-rules.yaml

# 4. Deploy with backward compatibility
# Ensure dual emission for WebSocket events
# Maintain legacy endpoint redirects
```

---

**Document Status**: ✅ Complete - Unified Contract Specification
**Next Review**: 2024-10-24
**Contract Version**: 1.1.0
**Implementation Priority**: RICE-scored, phased approach

*This unified documentation package provides complete coverage of the MyTrader API contracts addressing all critical issues identified through Business Analyst findings and Product Owner prioritization. All documents are maintained in sync with the actual implementation requirements and validated through comprehensive testing strategies.*