# Market Status Indicator - Implementation Roadmap
**Document Version:** 1.0
**Date:** 2025-10-09
**Estimated Duration:** 6-8 weeks
**Team Size:** 3-4 developers (1 backend, 2 frontend, 1 QA)

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Phase 1: Backend Enhancement](#phase-1-backend-enhancement)
4. [Phase 2: Mobile UI Implementation](#phase-2-mobile-ui-implementation)
5. [Phase 3: Web UI Implementation](#phase-3-web-ui-implementation)
6. [Phase 4: Integration & Testing](#phase-4-integration--testing)
7. [Phase 5: Deployment & Monitoring](#phase-5-deployment--monitoring)
8. [Risk Mitigation Plan](#risk-mitigation-plan)
9. [Success Criteria](#success-criteria)

---

## Overview

### Objective
Implement comprehensive market status indicators across mobile and web platforms to eliminate user confusion about market open/closed states and data freshness.

### Current State Analysis
**Existing Infrastructure (Already Built):**
- ✅ `MarketHoursService.cs` - Complete market status calculation logic
- ✅ `MarketStatusService.cs` - Status management with change events
- ✅ `TradingSession` model - Trading hours definition
- ✅ Holiday calendars for BIST, NASDAQ, NYSE (2025-2026)
- ✅ Timezone handling (TRT UTC+3, EST UTC-5/-4)
- ✅ `MarketStatusIndicator.tsx` component (mobile, needs integration)
- ✅ `MarketStatusContext.tsx` (mobile, needs activation)

**Gaps to Address:**
- 🔄 API endpoints not exposing market status to frontend
- 🔄 SignalR events not broadcasting status changes
- 🔄 Frontend components not integrated into dashboard
- 🆕 Data staleness detection missing
- 🆕 Real-time/delayed data badges missing
- 🆕 Web UI components not built

### Architecture Decision Records

**ADR-001: Backend Calculates Market Status**
- **Decision:** Backend calculates market status every 1 minute and caches result
- **Rationale:** Centralized calculation ensures consistency, reduces client-side complexity
- **Alternative Considered:** Client-side calculation (rejected: inconsistent across devices)

**ADR-002: SignalR for Real-time Status Updates**
- **Decision:** Use SignalR to push market status changes to connected clients
- **Rationale:** Immediate notification when markets open/close, reduces polling
- **Alternative Considered:** Long polling (rejected: more resource intensive)

**ADR-003: UTC-Only in API, Timezone Conversion Client-Side**
- **Decision:** API always returns times in UTC (ISO 8601), clients convert to local
- **Rationale:** Simplifies backend, leverages browser/OS timezone capabilities
- **Alternative Considered:** Backend converts to user timezone (rejected: needs user locale storage)

---

## Prerequisites

### Technical Requirements

**Backend:**
- .NET 8.0 SDK
- PostgreSQL 15+ database
- Redis (for distributed caching - optional but recommended)
- NodaTime library (already included)

**Frontend (Mobile):**
- Node.js 18+
- React Native 0.73+
- Expo SDK 50+
- TypeScript 5.3+

**Frontend (Web):**
- Node.js 18+
- React 18+
- TypeScript 5.3+
- Vite (build tool)
- Radix UI (tooltip library)

**Infrastructure:**
- Azure App Service or equivalent (production deployment)
- Azure SignalR Service (for scaling SignalR connections)
- CDN for static assets

### Team Skills Required

**Backend Developer:**
- C# / .NET expertise
- EF Core knowledge
- SignalR experience
- Timezone/datetime handling expertise

**Frontend Developers (x2):**
- React / React Native proficiency
- TypeScript expertise
- Real-time data handling (WebSocket/SignalR)
- Responsive design

**QA Engineer:**
- API testing (Postman/REST Client)
- E2E testing (Playwright/Detox)
- Timezone-aware test case design
- Accessibility testing (WCAG 2.1)

### Environment Setup

```bash
# Backend setup
cd backend/MyTrader.Api
dotnet restore
dotnet build

# Run tests
cd ../MyTrader.Tests
dotnet test

# Mobile setup
cd frontend/mobile
npm install
expo start

# Web setup
cd frontend/web
npm install
npm run dev
```

---

## Phase 1: Backend Enhancement

**Duration:** 2 weeks
**Owner:** Backend Developer

### Week 1: API Endpoints & DTOs

#### Task 1.1: Enhance UnifiedMarketDataDto
**File:** `backend/MyTrader.Core/DTOs/UnifiedMarketDataDto.cs`
**Changes:**
```csharp
public class UnifiedMarketDataDto
{
    // ... existing fields ...

    // NEW: Market Status Integration
    public string MarketStatus { get; set; } = string.Empty;
    public bool IsMarketOpen { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public DateTime DataTimestamp { get; set; }
    public DateTime ReceivedTimestamp { get; set; }

    // NEW: Data Quality
    public string DataSource { get; set; } = string.Empty;
    public bool IsRealtime { get; set; }
    public int DataDelayMinutes { get; set; }
    public int DataStalenessSeconds { get; set; }

    // NEW: Next Market Event
    public MarketEventDto? NextMarketEvent { get; set; }
}

public class MarketEventDto
{
    public string EventType { get; set; } = string.Empty; // "OPEN" | "CLOSE"
    public DateTime EventTime { get; set; }
    public int TimeUntilEventSeconds { get; set; }
}
```

**Acceptance Criteria:**
- [ ] DTO includes all required market status fields
- [ ] Unit tests pass for DTO serialization/deserialization
- [ ] No breaking changes to existing API contracts

**Estimated Time:** 4 hours

---

#### Task 1.2: Create Market Status API Endpoints
**File:** `backend/MyTrader.Api/Controllers/MarketsController.cs`
**New Endpoints:**

```csharp
[ApiController]
[Route("api/markets")]
public class MarketsController : ControllerBase
{
    [HttpGet("status/all")]
    [ProducesResponseType(typeof(MarketStatusCollectionDto), 200)]
    public async Task<IActionResult> GetAllMarketStatuses(
        CancellationToken cancellationToken)
    {
        // Implementation
    }

    [HttpGet("status/{marketCode}")]
    [ProducesResponseType(typeof(MarketStatusDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMarketStatus(
        string marketCode,
        CancellationToken cancellationToken)
    {
        // Implementation
    }

    [HttpGet("timing")]
    [ProducesResponseType(typeof(Dictionary<string, MarketTimingDto>), 200)]
    public async Task<IActionResult> GetAllMarketTimings(
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

**Acceptance Criteria:**
- [ ] All endpoints return correct status codes
- [ ] Response DTOs match API contract specification
- [ ] Endpoints are documented with XML comments
- [ ] Swagger/OpenAPI documentation generated
- [ ] Integration tests written and passing

**Estimated Time:** 8 hours

---

#### Task 1.3: Enhance MultiAssetDataService
**File:** `backend/MyTrader.Infrastructure/Services/MultiAssetDataService.cs`
**Changes:**
```csharp
public async Task<UnifiedMarketDataDto> GetMarketDataAsync(
    Guid symbolId,
    CancellationToken cancellationToken)
{
    var symbol = await _context.Symbols.FindAsync(symbolId);
    var marketStatus = await _marketHoursService.GetMarketStatus(
        GetExchangeFromMarketCode(symbol.MarketCode));

    var data = await FetchDataFromProvider(symbol);

    // Enrich with market status
    data.MarketStatus = marketStatus.State.ToString();
    data.IsMarketOpen = marketStatus.State == Enums.MarketStatus.OPEN;
    data.DataStalenessSeconds = CalculateStaleness(data.ReceivedTimestamp);

    // Calculate next market event
    data.NextMarketEvent = CalculateNextEvent(marketStatus);

    return data;
}
```

**Acceptance Criteria:**
- [ ] Market status populated for all symbols
- [ ] Data staleness calculated correctly
- [ ] Next market event accurate
- [ ] Performance impact <10ms per request

**Estimated Time:** 6 hours

---

### Week 2: SignalR Events & Background Services

#### Task 1.4: Create MarketStatusBroadcastService
**File:** `backend/MyTrader.Api/Services/MarketStatusBroadcastService.cs`
**New Service:**

```csharp
public class MarketStatusBroadcastService : BackgroundService
{
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly IMarketHoursService _marketHoursService;
    private readonly ILogger<MarketStatusBroadcastService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAndBroadcastMarketStatusChanges(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckAndBroadcastMarketStatusChanges(
        CancellationToken cancellationToken)
    {
        var allStatuses = _marketHoursService.GetAllMarketStatuses();

        foreach (var (exchange, status) in allStatuses)
        {
            var previousStatus = _statusCache.GetOrDefault(exchange);

            if (previousStatus != null && previousStatus.State != status.State)
            {
                await BroadcastStatusChange(exchange, previousStatus, status);
            }

            _statusCache[exchange] = status;
        }
    }
}
```

**Acceptance Criteria:**
- [ ] Service runs as background worker
- [ ] Checks market status every 1 minute
- [ ] Broadcasts changes to all connected clients via SignalR
- [ ] Handles SignalR connection failures gracefully
- [ ] Logs status changes

**Estimated Time:** 8 hours

---

#### Task 1.5: Add SignalR Hub Events
**File:** `backend/MyTrader.Api/Hubs/MarketDataHub.cs`
**New Methods:**

```csharp
public class MarketDataHub : Hub
{
    // Existing methods...

    public async Task SubscribeToMarketStatus(List<string> marketCodes)
    {
        foreach (var code in marketCodes)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"market_{code}");
        }
    }

    public async Task UnsubscribeFromMarketStatus(List<string> marketCodes)
    {
        foreach (var code in marketCodes)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"market_{code}");
        }
    }

    // Called by MarketStatusBroadcastService
    public async Task BroadcastMarketStatusChange(MarketStatusChangeDto change)
    {
        await Clients.Group($"market_{change.MarketCode}")
            .SendAsync("OnMarketStatusChanged", change);
    }
}
```

**Acceptance Criteria:**
- [ ] Subscription methods work correctly
- [ ] Status change events broadcast to subscribed clients only
- [ ] Hub handles concurrent connections (load test: 1000 clients)
- [ ] Connection recovery works after network interruption

**Estimated Time:** 6 hours

---

#### Task 1.6: Data Staleness Detection
**File:** `backend/MyTrader.Core/Services/DataStalenessService.cs`
**New Service:**

```csharp
public class DataStalenessService : IDataStalenessService
{
    public StalenessResult CheckStaleness(
        DateTime lastUpdateTime,
        MarketStatus marketStatus,
        DataProviderType provider)
    {
        if (marketStatus == MarketStatus.CLOSED)
        {
            return new StalenessResult { IsStale = false };
        }

        var staleness = (DateTime.UtcNow - lastUpdateTime).TotalSeconds;
        var threshold = GetThreshold(marketStatus, provider);

        return new StalenessResult
        {
            IsStale = staleness > threshold,
            StalenessSeconds = (int)staleness,
            ExpectedFrequencySeconds = threshold,
            Severity = staleness > threshold * 2 ? "ERROR" : "WARNING"
        };
    }

    private int GetThreshold(MarketStatus status, DataProviderType provider)
    {
        return (status, provider) switch
        {
            (MarketStatus.OPEN, DataProviderType.Crypto) => 30,
            (MarketStatus.OPEN, DataProviderType.Realtime) => 30,
            (MarketStatus.OPEN, DataProviderType.Delayed) => 1200, // 20 min
            (MarketStatus.PRE_MARKET, _) => 300, // 5 min
            (MarketStatus.AFTER_HOURS, _) => 300, // 5 min
            _ => int.MaxValue
        };
    }
}
```

**Acceptance Criteria:**
- [ ] Staleness calculated correctly for all market types
- [ ] No false positives when market closed
- [ ] Severity levels assigned correctly
- [ ] Performance <1ms per check

**Estimated Time:** 4 hours

---

#### Task 1.7: Testing & Documentation

**Unit Tests:**
```bash
# Test files to create:
- MarketStatusControllerTests.cs
- MarketStatusBroadcastServiceTests.cs
- DataStalenessServiceTests.cs
- MarketStatusDtoTests.cs
```

**Integration Tests:**
```bash
# Test scenarios:
- API endpoint returns correct status at market open
- API endpoint returns correct status at market close
- SignalR event fires when market status changes
- Staleness detection works during market hours
```

**API Documentation:**
- Update Swagger/OpenAPI spec
- Add example requests/responses
- Document rate limits
- Add troubleshooting guide

**Acceptance Criteria:**
- [ ] Code coverage >80%
- [ ] All tests pass
- [ ] API documented in Swagger
- [ ] README updated with new endpoints

**Estimated Time:** 8 hours

---

### Phase 1 Deliverables

✅ **Week 1:**
- Enhanced DTOs with market status fields
- New API endpoints: `/markets/status/all`, `/markets/status/{code}`
- Enriched `UnifiedMarketDataDto` with status

✅ **Week 2:**
- `MarketStatusBroadcastService` background worker
- SignalR hub events for status changes
- Data staleness detection service
- Comprehensive test suite
- Updated API documentation

**Phase 1 Exit Criteria:**
- [ ] All unit tests passing
- [ ] Integration tests passing
- [ ] API endpoints working in dev environment
- [ ] SignalR events broadcasting correctly
- [ ] Code reviewed and approved
- [ ] Documentation complete

---

## Phase 2: Mobile UI Implementation

**Duration:** 2 weeks
**Owner:** Frontend Developer (Mobile)

### Week 3: Component Integration

#### Task 2.1: Activate MarketStatusContext
**File:** `frontend/mobile/src/context/MarketStatusContext.tsx`
**Changes:**
```typescript
// Context already exists but not integrated
// Changes needed:

1. Connect to API endpoint `/markets/status/all`
2. Subscribe to SignalR `OnMarketStatusChanged` event
3. Update state when status changes
4. Provide status to child components

export const MarketStatusProvider: React.FC<Props> = ({ children }) => {
  const [marketStatuses, setMarketStatuses] = useState<Record<string, MarketStatusDto>>({});
  const { connection } = useWebSocket();

  useEffect(() => {
    // Fetch initial status
    fetchMarketStatuses();

    // Subscribe to SignalR events
    connection?.on('OnMarketStatusChanged', handleStatusChange);

    // Refresh every 5 minutes
    const interval = setInterval(fetchMarketStatuses, 5 * 60 * 1000);

    return () => {
      clearInterval(interval);
      connection?.off('OnMarketStatusChanged');
    };
  }, [connection]);

  // ... implementation
};
```

**Acceptance Criteria:**
- [ ] Context provides market status to all child components
- [ ] SignalR events update status in real-time
- [ ] Periodic refresh works (every 5 min)
- [ ] Error handling for API failures

**Estimated Time:** 6 hours

---

#### Task 2.2: Integrate MarketStatusIndicator into Accordions
**File:** `frontend/mobile/src/components/dashboard/AssetClassAccordion.tsx`
**Changes:**
```typescript
import { MarketStatusBadge } from './MarketStatusIndicator';
import { useMarketStatus } from '../../context/MarketStatusContext';

export const AssetClassAccordion: React.FC<Props> = ({ assetClass, symbols }) => {
  const { getStatusForMarket } = useMarketStatus();

  // Determine market from asset class
  const marketCode = getMarketCodeForAssetClass(assetClass.code);
  const marketStatus = getStatusForMarket(marketCode);

  return (
    <View>
      <TouchableOpacity onPress={toggleAccordion}>
        <View style={styles.header}>
          <Text style={styles.title}>
            {assetClass.icon} {assetClass.displayName} ({symbols.length})
          </Text>

          {/* NEW: Market Status Badge */}
          <MarketStatusBadge
            marketCode={marketCode}
            status={marketStatus.status}
            nextOpenTime={marketStatus.nextOpen}
            nextCloseTime={marketStatus.nextClose}
            compact
            onPress={() => showMarketStatusModal(marketStatus)}
          />
        </View>
      </TouchableOpacity>

      {/* Accordion content... */}
    </View>
  );
};
```

**Acceptance Criteria:**
- [ ] Badge appears on accordion header
- [ ] Badge color matches market status
- [ ] Tap opens market status modal
- [ ] Status updates when market opens/closes

**Estimated Time:** 4 hours

---

#### Task 2.3: Add Timestamp to Symbol Cards
**File:** `frontend/mobile/src/components/dashboard/SymbolCard.tsx`
**Changes:**
```typescript
export const SymbolCard: React.FC<Props> = ({ symbol, marketData }) => {
  const timestampText = formatTimestamp(marketData.lastUpdateTime);
  const isMarketClosed = marketData.marketStatus === 'CLOSED';

  return (
    <View style={styles.card}>
      <Text style={styles.symbol}>{symbol.symbol}</Text>
      <Text style={styles.price}>${marketData.price.toFixed(2)}</Text>

      {/* NEW: Last Update Timestamp */}
      <Text style={styles.timestamp}>
        {isMarketClosed
          ? `Piyasa Kapalı - Son: ${timestampText}`
          : `Son Güncelleme: ${timestampText}`}
      </Text>

      <Text style={styles.change}>
        {marketData.changePercent > 0 ? '+' : ''}
        {marketData.changePercent.toFixed(2)}%
      </Text>
    </View>
  );
};

// Utility function
const formatTimestamp = (timestamp: string): string => {
  const now = Date.now();
  const then = new Date(timestamp).getTime();
  const diffMinutes = Math.floor((now - then) / 60000);

  if (diffMinutes < 1) return 'Az önce';
  if (diffMinutes < 60) return `${diffMinutes} dakika önce`;

  const date = new Date(timestamp);
  return date.toLocaleTimeString('tr-TR', {
    hour: '2-digit',
    minute: '2-digit'
  });
};
```

**Acceptance Criteria:**
- [ ] Timestamp displays below price
- [ ] Relative time format for recent updates (<1h)
- [ ] Absolute time format for older updates
- [ ] Market closed message when applicable

**Estimated Time:** 3 hours

---

#### Task 2.4: Create Market Status Modal
**File:** `frontend/mobile/src/components/dashboard/MarketStatusModal.tsx`
**New Component:**
```typescript
export const MarketStatusModal: React.FC<Props> = ({ visible, marketStatus, onClose }) => {
  return (
    <Modal visible={visible} animationType="slide" transparent>
      <View style={styles.overlay}>
        <View style={styles.modal}>
          <Text style={styles.title}>
            {marketStatus.marketName} - Piyasa Durumu
          </Text>

          <View style={styles.statusRow}>
            <StatusDot status={marketStatus.status} />
            <Text style={styles.statusText}>
              {getStatusText(marketStatus.status)}
            </Text>
          </View>

          {marketStatus.nextOpenTime && (
            <InfoRow
              label="Sıradaki Açılış"
              value={formatDateTime(marketStatus.nextOpenTime)}
            />
          )}

          {marketStatus.nextCloseTime && (
            <InfoRow
              label="Sıradaki Kapanış"
              value={formatDateTime(marketStatus.nextCloseTime)}
            />
          )}

          <InfoRow label="Zaman Dilimi" value={marketStatus.timezone} />
          <InfoRow label="Yerel Saat" value={formatCurrentTime(marketStatus)} />

          {marketStatus.closureReason && (
            <View style={styles.closureReason}>
              <Text style={styles.label}>Kapanış Nedeni:</Text>
              <Text style={styles.value}>{marketStatus.closureReason}</Text>
            </View>
          )}

          <TouchableOpacity style={styles.closeButton} onPress={onClose}>
            <Text style={styles.closeButtonText}>Tamam</Text>
          </TouchableOpacity>
        </View>
      </View>
    </Modal>
  );
};
```

**Acceptance Criteria:**
- [ ] Modal appears from bottom with slide animation
- [ ] Shows all market status details
- [ ] Displays times in user's local timezone with market time reference
- [ ] Close button and tap-outside-to-close work

**Estimated Time:** 5 hours

---

### Week 4: Staleness Warnings & Polish

#### Task 2.5: Implement Data Staleness Warning
**File:** `frontend/mobile/src/components/dashboard/StalenessWarning.tsx`
**New Component:**
```typescript
export const StalenessWarning: React.FC<Props> = ({
  lastUpdateTime,
  marketStatus,
  dataSource
}) => {
  const staleness = useStalenessCheck(lastUpdateTime, marketStatus, dataSource);

  if (!staleness.isStale) return null;

  return (
    <TouchableOpacity
      style={[
        styles.warning,
        staleness.severity === 'ERROR' && styles.warningError
      ]}
      onPress={() => showStalenessDetails(staleness)}
    >
      <Icon
        name={staleness.severity === 'ERROR' ? 'alert-circle' : 'alert-triangle'}
        size={16}
        color={staleness.severity === 'ERROR' ? '#ef4444' : '#f59e0b'}
      />
      <Text style={styles.warningText}>
        {staleness.severity === 'ERROR'
          ? 'Bağlantı koptu'
          : 'Veri güncel olmayabilir'}
      </Text>
    </TouchableOpacity>
  );
};

// Hook for staleness calculation
const useStalenessCheck = (lastUpdate, marketStatus, dataSource) => {
  const [staleness, setStaleness] = useState<StalenessResult>(null);

  useEffect(() => {
    const check = () => {
      const result = calculateStaleness(lastUpdate, marketStatus, dataSource);
      setStaleness(result);
    };

    check();
    const interval = setInterval(check, 10000); // Check every 10s

    return () => clearInterval(interval);
  }, [lastUpdate, marketStatus, dataSource]);

  return staleness;
};
```

**Acceptance Criteria:**
- [ ] Warning appears when data is stale during market hours
- [ ] No warning when market is closed
- [ ] Error state for critical staleness (>60s crypto, >30min stocks)
- [ ] Tap shows detailed staleness info

**Estimated Time:** 6 hours

---

#### Task 2.6: Add Data Source Badge
**File:** `frontend/mobile/src/components/dashboard/DataSourceBadge.tsx`
**New Component:**
```typescript
export const DataSourceBadge: React.FC<Props> = ({
  dataSource,
  isRealtime,
  dataDelayMinutes
}) => {
  if (isRealtime) {
    return (
      <View style={styles.badgeRealtime}>
        <PulsingDot color="#10b981" size={6} />
        <Text style={styles.text}>Gerçek zamanlı</Text>
      </View>
    );
  }

  if (dataDelayMinutes > 0) {
    return (
      <TouchableOpacity
        style={styles.badgeDelayed}
        onPress={() => showDataSourceInfo(dataSource, dataDelayMinutes)}
      >
        <Icon name="info" size={12} color="#6b7280" />
        <Text style={styles.text}>{dataDelayMinutes} dk gecikme</Text>
      </TouchableOpacity>
    );
  }

  return null;
};
```

**Acceptance Criteria:**
- [ ] Badge shows "Gerçek zamanlı" with pulsing dot for realtime data
- [ ] Badge shows "X dk gecikme" for delayed data
- [ ] Tap shows data source information modal
- [ ] Badge hidden for crypto (always realtime)

**Estimated Time:** 3 hours

---

#### Task 2.7: Testing & Polish

**Unit Tests:**
```typescript
// Test files to create:
- MarketStatusContext.test.tsx
- MarketStatusIndicator.test.tsx
- StalenessWarning.test.tsx
- DataSourceBadge.test.tsx
- formatTimestamp.test.ts
```

**Integration Tests (Detox):**
```typescript
describe('Market Status Integration', () => {
  it('should display market status on dashboard', async () => {
    await element(by.id('dashboard-screen')).tap();
    await expect(element(by.id('market-status-bist'))).toBeVisible();
  });

  it('should open market status modal on tap', async () => {
    await element(by.id('market-status-badge-nasdaq')).tap();
    await expect(element(by.id('market-status-modal'))).toBeVisible();
  });

  it('should show staleness warning during market hours', async () => {
    // Mock stale data
    await mockStaleMarketData();
    await expect(element(by.id('staleness-warning'))).toBeVisible();
  });
});
```

**Manual Testing Checklist:**
- [ ] Market status badge visible on all accordions
- [ ] Status updates when market opens (test at 10:00 TRT)
- [ ] Timestamp updates every second
- [ ] Modal shows correct information
- [ ] Staleness warning appears when appropriate
- [ ] Data source badge shows correct info
- [ ] Turkish translations correct
- [ ] Animations smooth (60fps)

**Acceptance Criteria:**
- [ ] Test coverage >75%
- [ ] All Detox tests passing
- [ ] Manual testing checklist complete
- [ ] No console warnings/errors
- [ ] Performance metrics met (render <100ms)

**Estimated Time:** 8 hours

---

### Phase 2 Deliverables

✅ **Week 3:**
- MarketStatusContext activated and integrated
- Market status badges on accordion headers
- Timestamp display on symbol cards
- Market status detail modal

✅ **Week 4:**
- Data staleness warnings
- Data source badges (realtime/delayed)
- Comprehensive test suite
- UI polish and animations

**Phase 2 Exit Criteria:**
- [ ] All UI components working on iOS and Android
- [ ] Real-time status updates via SignalR
- [ ] Staleness warnings accurate
- [ ] Tests passing (unit + integration)
- [ ] UX reviewed and approved
- [ ] Accessibility verified (screen reader support)

---

## Phase 3: Web UI Implementation

**Duration:** 2 weeks
**Owner:** Frontend Developer (Web)

### Week 5: Core Components

#### Task 3.1: Create MarketStatusBadge Component
**File:** `frontend/web/src/components/shared/MarketStatusBadge.tsx`
**New Component:**
```typescript
import { Tooltip } from '@radix-ui/react-tooltip';

export const MarketStatusBadge: React.FC<Props> = ({
  marketCode,
  status,
  nextOpenTime,
  nextCloseTime,
  variant = 'default'
}) => {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <div className={cn('market-status-badge', `variant-${variant}`)}>
          <StatusDot status={status} />
          <span className="market-name">{marketCode}</span>
          <span className="status-text">{getStatusText(status)}</span>
        </div>
      </TooltipTrigger>

      <TooltipContent>
        <MarketStatusTooltipContent
          marketCode={marketCode}
          status={status}
          nextOpenTime={nextOpenTime}
          nextCloseTime={nextCloseTime}
        />
      </TooltipContent>
    </Tooltip>
  );
};
```

**Acceptance Criteria:**
- [ ] Badge renders with correct styling
- [ ] Hover shows tooltip with details
- [ ] Tooltip positioned correctly (no overflow)
- [ ] Keyboard accessible (Tab to focus, Enter to open)

**Estimated Time:** 5 hours

---

#### Task 3.2: Create MarketOverviewBar Component
**File:** `frontend/web/src/components/dashboard/MarketOverviewBar.tsx`
**New Component:**
```typescript
export const MarketOverviewBar: React.FC<Props> = () => {
  const { marketStatuses, lastUpdate } = useMarketStatus();

  return (
    <div className="market-overview-bar">
      <h3 className="title">Piyasa Durumları</h3>
      <span className="last-update">Son Güncelleme: {lastUpdate}</span>

      <div className="markets-grid">
        {Object.entries(marketStatuses).map(([code, status]) => (
          <MarketCard key={code} marketCode={code} status={status} />
        ))}
      </div>
    </div>
  );
};

const MarketCard: React.FC<Props> = ({ marketCode, status }) => (
  <div className="market-card">
    <StatusDot status={status.status} size="large" />
    <div className="content">
      <h4 className="market-name">{marketCode}</h4>
      <p className="status">{getStatusText(status.status)}</p>
      {status.nextOpenTime && (
        <p className="next-event">Açılış: {formatTime(status.nextOpenTime)}</p>
      )}
      {status.nextCloseTime && (
        <CountdownTimer targetTime={status.nextCloseTime} />
      )}
    </div>
  </div>
);
```

**Acceptance Criteria:**
- [ ] Overview bar shows all 4 markets (BIST, NASDAQ, NYSE, CRYPTO)
- [ ] Status colors correct
- [ ] Countdown timer updates every second
- [ ] Responsive layout (horizontal scroll on mobile)

**Estimated Time:** 6 hours

---

#### Task 3.3: Add Market Status to Symbol Table
**File:** `frontend/web/src/components/dashboard/SymbolTable.tsx`
**Changes:**
```typescript
export const SymbolTable: React.FC<Props> = ({ symbols, marketData }) => {
  return (
    <table className="symbol-table">
      <thead>
        <tr>
          <th>Symbol</th>
          <th>Name</th>
          <th>Price</th>
          <th>Change</th>
          <th>Volume</th>
          <th>Last Update</th>
          <th>Market Status</th> {/* NEW COLUMN */}
        </tr>
      </thead>
      <tbody>
        {symbols.map(symbol => {
          const data = marketData[symbol.id];
          return (
            <tr key={symbol.id}>
              <td>{symbol.symbol}</td>
              <td>{symbol.displayName}</td>
              <td>${data.price.toFixed(2)}</td>
              <td className={data.change > 0 ? 'positive' : 'negative'}>
                {data.changePercent.toFixed(2)}%
              </td>
              <td>{formatVolume(data.volume)}</td>
              <td>
                {formatTimestamp(data.lastUpdateTime)}
                {/* Show staleness warning if applicable */}
                {isStaleness(data) && <StalenessIcon />}
              </td>
              <td>
                <MarketStatusBadge
                  marketCode={symbol.marketCode}
                  status={data.marketStatus}
                  variant="compact"
                />
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
};
```

**Acceptance Criteria:**
- [ ] New "Market Status" column added
- [ ] Status badge shows correct state for each symbol
- [ ] Table remains performant with 50+ rows
- [ ] Sorting works on all columns including new column

**Estimated Time:** 4 hours

---

#### Task 3.4: Implement Market Status Tooltip
**File:** `frontend/web/src/components/shared/MarketStatusTooltip.tsx`
**New Component:**
```typescript
export const MarketStatusTooltipContent: React.FC<Props> = ({
  marketCode,
  status,
  nextOpenTime,
  nextCloseTime
}) => {
  const marketStatus = useMarketStatusDetails(marketCode);

  return (
    <div className="market-status-tooltip">
      <h4 className="title">{marketStatus.name} - Market Status</h4>

      <div className="status-row">
        <StatusDot status={status} />
        <span className="status">{getStatusText(status)}</span>
      </div>

      <div className="info-grid">
        {marketStatus.regularHours && (
          <InfoRow
            label="Trading Hours"
            value={`${marketStatus.regularHours.open} - ${marketStatus.regularHours.close}`}
          />
        )}

        {nextOpenTime && (
          <InfoRow
            label="Next Open"
            value={formatDateTime(nextOpenTime)}
          />
        )}

        {nextCloseTime && (
          <InfoRow
            label="Next Close"
            value={formatDateTime(nextCloseTime)}
          />
        )}

        <InfoRow
          label="Current Time"
          value={formatCurrentTime(marketStatus.timezone)}
        />

        {marketStatus.closureReason && (
          <InfoRow
            label="Closure Reason"
            value={marketStatus.closureReason}
          />
        )}
      </div>
    </div>
  );
};
```

**Acceptance Criteria:**
- [ ] Tooltip shows detailed market information
- [ ] Times formatted in both market timezone and user's local timezone
- [ ] Tooltip dismisses on outside click or Escape key
- [ ] Tooltip doesn't overflow viewport (auto-reposition)

**Estimated Time:** 5 hours

---

### Week 6: Staleness Detection & Polish

#### Task 3.5: Implement Staleness Warning Component
**File:** `frontend/web/src/components/shared/StalenessWarning.tsx`
**New Component:**
```typescript
export const StalenessWarning: React.FC<Props> = ({
  lastUpdateTime,
  marketStatus,
  dataSource,
  inline = true
}) => {
  const staleness = useStalenessCheck(lastUpdateTime, marketStatus, dataSource);

  if (!staleness.isStale) return null;

  if (inline) {
    return (
      <Tooltip>
        <TooltipTrigger>
          <AlertTriangle
            className={cn(
              'staleness-icon',
              staleness.severity === 'ERROR' && 'error'
            )}
            size={16}
          />
        </TooltipTrigger>
        <TooltipContent>
          <StalenessTooltipContent staleness={staleness} />
        </TooltipContent>
      </Tooltip>
    );
  }

  return (
    <Alert variant={staleness.severity === 'ERROR' ? 'destructive' : 'warning'}>
      <AlertTriangle className="icon" />
      <AlertTitle>Data Connection Issue</AlertTitle>
      <AlertDescription>
        Last update: {formatRelativeTime(lastUpdateTime)}
        <br />
        Expected frequency: {staleness.expectedFrequencyMinutes} minutes
      </AlertDescription>
    </Alert>
  );
};
```

**Acceptance Criteria:**
- [ ] Warning icon appears next to timestamp when data stale
- [ ] Tooltip shows detailed staleness info
- [ ] Banner alert option for critical staleness
- [ ] No warnings when market closed

**Estimated Time:** 4 hours

---

#### Task 3.6: Add Data Source Badge (Web)
**File:** `frontend/web/src/components/shared/DataSourceBadge.tsx`
**New Component:**
```typescript
export const DataSourceBadge: React.FC<Props> = ({
  dataSource,
  isRealtime,
  dataDelayMinutes
}) => {
  if (isRealtime) {
    return (
      <Badge variant="success" className="data-source-badge">
        <span className="dot pulsing" />
        Realtime
      </Badge>
    );
  }

  if (dataDelayMinutes > 0) {
    return (
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge variant="secondary" className="data-source-badge">
            <Info size={12} />
            {dataDelayMinutes}min delay
          </Badge>
        </TooltipTrigger>
        <TooltipContent>
          <DataSourceTooltipContent
            dataSource={dataSource}
            delay={dataDelayMinutes}
          />
        </TooltipContent>
      </Tooltip>
    );
  }

  return null;
};
```

**Acceptance Criteria:**
- [ ] Badge shows realtime status with pulsing dot
- [ ] Badge shows delay time for delayed data
- [ ] Tooltip provides data source details
- [ ] Badge integrates into symbol table

**Estimated Time:** 3 hours

---

#### Task 3.7: Responsive Design & Accessibility

**Responsive Breakpoints:**
```scss
// Mobile: < 768px
.market-overview-bar {
  overflow-x: auto;
  .markets-grid {
    display: flex;
    flex-direction: row;
    gap: 0.5rem;
  }
}

// Tablet: 768px - 1024px
@media (min-width: 768px) {
  .markets-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
  }
}

// Desktop: > 1024px
@media (min-width: 1024px) {
  .markets-grid {
    grid-template-columns: repeat(4, 1fr);
  }
}
```

**Accessibility Enhancements:**
- Add ARIA labels to all status indicators
- Ensure keyboard navigation works for all interactive elements
- Add focus states with visible outline
- Ensure color contrast meets WCAG AA (4.5:1)
- Test with screen reader (VoiceOver/NVDA)

**Acceptance Criteria:**
- [ ] Layout adapts to all screen sizes
- [ ] Touch targets ≥44px on mobile
- [ ] Keyboard navigation works end-to-end
- [ ] Screen reader announces all status changes
- [ ] WCAG 2.1 AA compliant

**Estimated Time:** 6 hours

---

#### Task 3.8: Testing & Documentation

**Unit Tests (Vitest):**
```typescript
describe('MarketStatusBadge', () => {
  it('renders with correct status', () => {
    render(<MarketStatusBadge status="OPEN" marketCode="NASDAQ" />);
    expect(screen.getByText('NASDAQ')).toBeInTheDocument();
    expect(screen.getByText('Open')).toBeInTheDocument();
  });

  it('shows tooltip on hover', async () => {
    render(<MarketStatusBadge status="CLOSED" marketCode="BIST" />);
    await userEvent.hover(screen.getByRole('button'));
    expect(await screen.findByRole('tooltip')).toBeVisible();
  });
});
```

**E2E Tests (Playwright):**
```typescript
test('market status indicator integration', async ({ page }) => {
  await page.goto('/dashboard');

  // Check market overview bar visible
  await expect(page.locator('.market-overview-bar')).toBeVisible();

  // Check BIST market status
  const bistStatus = page.locator('[data-market="BIST"]');
  await expect(bistStatus).toContainText('BIST');

  // Hover to show tooltip
  await bistStatus.hover();
  await expect(page.locator('.market-status-tooltip')).toBeVisible();

  // Check status in symbol table
  const tableStatus = page.locator('table tbody tr:first-child .market-status-badge');
  await expect(tableStatus).toBeVisible();
});
```

**Cross-browser Testing:**
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari (iOS)
- [ ] Chrome Mobile (Android)

**Acceptance Criteria:**
- [ ] Test coverage >75%
- [ ] All Playwright tests passing
- [ ] Cross-browser testing complete
- [ ] No console errors in any browser
- [ ] Performance metrics met (Lighthouse score >90)

**Estimated Time:** 8 hours

---

### Phase 3 Deliverables

✅ **Week 5:**
- MarketStatusBadge component
- MarketOverviewBar component
- Symbol table market status column
- Market status tooltip

✅ **Week 6:**
- Staleness warning component
- Data source badge
- Responsive design
- Accessibility enhancements
- Comprehensive test suite

**Phase 3 Exit Criteria:**
- [ ] All web UI components working
- [ ] Responsive design works on all devices
- [ ] Tests passing (unit + E2E)
- [ ] Accessibility audit passed
- [ ] UX reviewed and approved
- [ ] Documentation complete

---

## Phase 4: Integration & Testing

**Duration:** 1 week
**Owner:** QA Engineer + All Developers

### Week 7: Integration Testing & Bug Fixes

#### Task 4.1: API Contract Validation
**Responsibility:** QA Engineer

**Test Scenarios:**
1. Verify `/markets/status/all` returns correct schema
2. Verify status values match enum specification
3. Verify times are in UTC ISO 8601 format
4. Verify SignalR events match specification
5. Load test: 1000 concurrent connections

**Tools:**
- Postman/REST Client for API testing
- Artillery/k6 for load testing
- SignalR Test Client for WebSocket testing

**Acceptance Criteria:**
- [ ] All API endpoints return 200 OK
- [ ] Response schemas match specification
- [ ] SignalR events fire within 1 second of status change
- [ ] System handles 1000 concurrent users without degradation

**Estimated Time:** 8 hours

---

#### Task 4.2: End-to-End Flow Testing
**Responsibility:** QA Engineer

**Test Flows:**
1. **Market Opens Flow:**
   - Set system time to 1 minute before market open
   - Verify status shows CLOSED → PRE_MARKET → OPEN
   - Verify SignalR events fire
   - Verify UI updates immediately
   - Verify notifications sent

2. **Market Closes Flow:**
   - Set system time to 1 minute before market close
   - Verify status shows OPEN → AFTER_HOURS → CLOSED
   - Verify price updates stop
   - Verify "Market Closed" message appears

3. **Data Staleness Flow:**
   - Disconnect WebSocket during market hours
   - Verify staleness warning appears after threshold
   - Reconnect WebSocket
   - Verify warning disappears

4. **Cross-Timezone Flow:**
   - Set user timezone to Istanbul (UTC+3)
   - View NASDAQ status (EST UTC-5)
   - Verify times displayed correctly in both timezones

**Acceptance Criteria:**
- [ ] All flows complete successfully
- [ ] No race conditions or timing issues
- [ ] UI updates smooth without flicker
- [ ] Error handling works gracefully

**Estimated Time:** 12 hours

---

#### Task 4.3: Timezone Edge Case Testing
**Responsibility:** Backend Developer

**Test Cases:**
1. **DST Spring Forward (2nd Sunday March):**
   - Test market status at 01:59 EST (before spring forward)
   - Test market status at 03:00 EDT (after spring forward)
   - Verify 02:00-03:00 gap handled correctly

2. **DST Fall Back (1st Sunday November):**
   - Test market status during repeated hour (01:00-02:00)
   - Verify correct status during ambiguous time

3. **Holiday Overlap:**
   - Test status on US holiday when BIST open
   - Verify correct status for each market independently

4. **Weekend Handling:**
   - Test status on Saturday at various times
   - Test status on Sunday at various times
   - Verify "Weekend" closure reason

**Acceptance Criteria:**
- [ ] All DST transitions handled correctly
- [ ] No incorrect status during edge cases
- [ ] Holiday calendar accurate
- [ ] Weekend detection works globally

**Estimated Time:** 6 hours

---

#### Task 4.4: Performance Testing
**Responsibility:** Backend + Frontend Developers

**Backend Performance:**
- Market status calculation: <50ms per market
- API endpoint response: <200ms p95
- SignalR broadcast: <500ms to all clients
- Database query: <10ms for market status lookup

**Frontend Performance:**
- Initial render: <100ms
- Status indicator update: <50ms
- Tooltip open: <100ms
- WebSocket message processing: <10ms

**Tools:**
- Chrome DevTools Performance Profiler
- Lighthouse
- React DevTools Profiler
- Backend APM (Application Insights)

**Acceptance Criteria:**
- [ ] All performance targets met
- [ ] No memory leaks (24h soak test)
- [ ] Lighthouse score >90 on web
- [ ] Mobile frame rate ≥60fps

**Estimated Time:** 8 hours

---

#### Task 4.5: Bug Bash & Fixes
**Responsibility:** All Team Members

**Bug Bash Session:**
- 4-hour session with entire team
- Each member tests different scenarios
- Focus on edge cases and error states
- Document all issues in bug tracker

**Common Bug Categories:**
- UI alignment/styling issues
- Incorrect status calculations
- Translation errors
- Accessibility violations
- Performance regressions

**Acceptance Criteria:**
- [ ] All critical bugs fixed
- [ ] All high-priority bugs fixed
- [ ] Medium/low bugs triaged for future sprints
- [ ] Regression tests added for fixed bugs

**Estimated Time:** 12 hours (4h bash + 8h fixes)

---

### Phase 4 Deliverables

✅ **Week 7:**
- API contract validation complete
- E2E flow testing complete
- Timezone edge case testing complete
- Performance testing complete
- All critical/high bugs fixed

**Phase 4 Exit Criteria:**
- [ ] All tests passing (unit, integration, E2E)
- [ ] No critical or high-priority bugs
- [ ] Performance targets met
- [ ] Accessibility audit passed
- [ ] Code freeze for deployment

---

## Phase 5: Deployment & Monitoring

**Duration:** 1 week
**Owner:** DevOps + All Team Members

### Week 8: Production Deployment

#### Task 5.1: Pre-Deployment Checklist

**Environment Preparation:**
- [ ] Production database backup
- [ ] Environment variables configured
- [ ] SSL certificates valid
- [ ] CDN cache invalidation rules set
- [ ] Monitoring dashboards configured
- [ ] Alert rules defined

**Code Preparation:**
- [ ] All code merged to main branch
- [ ] Version tag created (e.g., v2.0.0-market-status)
- [ ] Release notes written
- [ ] Rollback plan documented
- [ ] Feature flags configured (gradual rollout)

**Estimated Time:** 4 hours

---

#### Task 5.2: Backend Deployment

**Deployment Steps:**
1. Deploy to staging environment
2. Run smoke tests
3. Deploy to production with zero-downtime strategy
4. Verify API endpoints responding
5. Verify SignalR hub accepting connections
6. Monitor for errors (30 minutes)

**Rollback Triggers:**
- API response time >1 second p95
- Error rate >1%
- SignalR connection failures >5%
- Database connection pool exhaustion

**Acceptance Criteria:**
- [ ] Backend deployed successfully
- [ ] No increase in error rate
- [ ] All endpoints responding <200ms
- [ ] SignalR connections stable

**Estimated Time:** 2 hours

---

#### Task 5.3: Frontend Deployment

**Mobile Deployment:**
1. Build production bundles (iOS + Android)
2. Submit to App Store / Play Store
3. Staged rollout: 10% → 50% → 100%
4. Monitor crash reports (Sentry)

**Web Deployment:**
1. Build production bundle
2. Deploy to CDN
3. Invalidate cache
4. Monitor Core Web Vitals

**Acceptance Criteria:**
- [ ] Mobile app approved and deployed
- [ ] Web app deployed to CDN
- [ ] Cache invalidated correctly
- [ ] No increase in crash rate

**Estimated Time:** 3 hours (excluding app store review time)

---

#### Task 5.4: Monitoring & Alerting

**Metrics to Monitor:**
```
Backend:
- /markets/status/all response time
- Market status calculation duration
- SignalR connection count
- SignalR message delivery latency
- Cache hit rate
- Error rate

Frontend:
- Render performance (FCP, LCP, TTI)
- JavaScript errors
- API request failures
- WebSocket disconnections
```

**Alert Rules:**
```
Critical:
- API error rate >2% for 5 minutes
- SignalR connection failures >10% for 5 minutes
- Market status calculation failures >1%

Warning:
- API response time >500ms p95 for 10 minutes
- SignalR latency >2 seconds for 10 minutes
- Frontend error rate >0.5% for 5 minutes
```

**Acceptance Criteria:**
- [ ] All metrics being collected
- [ ] Dashboards showing real-time data
- [ ] Alert rules tested and working
- [ ] On-call rotation defined

**Estimated Time:** 4 hours

---

#### Task 5.5: User Communication

**Communication Channels:**
1. In-app notification banner
2. Email to active users
3. Social media announcement
4. Blog post / changelog

**Message Content:**
```
Subject: New Feature: Market Status Indicators

We're excited to announce a new feature that makes it easier to know when markets are open!

What's New:
✓ See at a glance if markets are open or closed
✓ View next market open/close times
✓ Know if your data is real-time or delayed
✓ Get notified when markets open

This update helps you trade smarter by providing real-time market status information.

Learn more: [link to documentation]
```

**Acceptance Criteria:**
- [ ] Notifications sent to all users
- [ ] Documentation published
- [ ] Support team briefed
- [ ] FAQ updated

**Estimated Time:** 3 hours

---

#### Task 5.6: Post-Deployment Monitoring

**24-Hour Watch Period:**
- Monitor error rates continuously
- Check user feedback channels (support tickets, app reviews)
- Verify SignalR events firing correctly
- Validate market status accuracy during market open/close transitions

**Weekly Review:**
- Analyze performance metrics
- Review user feedback
- Identify optimization opportunities
- Plan follow-up improvements

**Success Metrics:**
- Error rate <0.1%
- API response time <200ms p95
- User satisfaction score >4.0/5.0
- Support tickets about "prices not updating" reduced by >50%

**Acceptance Criteria:**
- [ ] No critical issues in 24 hours
- [ ] Performance within targets
- [ ] User feedback positive
- [ ] Support tickets reduced

**Estimated Time:** 8 hours (spread over week)

---

### Phase 5 Deliverables

✅ **Week 8:**
- Backend deployed to production
- Frontend deployed (mobile + web)
- Monitoring dashboards active
- User communication sent
- 24-hour watch complete

**Phase 5 Exit Criteria:**
- [ ] Feature live in production
- [ ] All systems stable
- [ ] Monitoring active
- [ ] User feedback collected
- [ ] Post-mortem scheduled

---

## Risk Mitigation Plan

### Risk 1: Market Status Calculation Errors

**Probability:** Medium
**Impact:** High
**Mitigation:**
- Extensive unit tests for all timezone edge cases
- Manual testing at actual market open/close times
- Fallback to "UNKNOWN" status if calculation fails
- Monitoring alert if calculation takes >50ms

**Contingency:**
- If errors >1%, rollback feature flag
- Display generic "Check exchange website" message
- Fix and redeploy within 4 hours

---

### Risk 2: SignalR Connection Stability

**Probability:** Medium
**Impact:** Medium
**Mitigation:**
- Implement auto-reconnect with exponential backoff
- Fallback to HTTP polling if WebSocket fails
- Load test with 1000+ concurrent connections
- Use Azure SignalR Service for production (scales automatically)

**Contingency:**
- If connection failures >10%, disable real-time updates
- Use periodic polling (every 1 minute) as fallback
- Investigate and fix, redeploy within 24 hours

---

### Risk 3: Performance Degradation

**Probability:** Low
**Impact:** High
**Mitigation:**
- Cache market status (1-minute TTL)
- Use Redis for distributed caching if needed
- Optimize database queries (indexed lookups)
- Load test before deployment

**Contingency:**
- If API response time >1s p95, enable aggressive caching (5-minute TTL)
- Scale out backend instances
- Disable real-time updates temporarily if needed

---

### Risk 4: Timezone/DST Bugs

**Probability:** Medium
**Impact:** Medium
**Mitigation:**
- Use NodaTime (battle-tested library)
- Test at actual DST transition times
- Use UTC internally, convert only for display
- Extensive manual testing around DST dates

**Contingency:**
- If incorrect status detected, show "UNKNOWN" instead of wrong status
- Manual override capability for support team
- Fix and deploy hotfix within 4 hours

---

### Risk 5: Mobile App Store Rejection

**Probability:** Low
**Impact:** High
**Mitigation:**
- Follow all app store guidelines
- Test thoroughly on iOS and Android
- Provide clear descriptions in submission
- No use of private APIs

**Contingency:**
- If rejected, address issues and resubmit within 24 hours
- Deploy web version first while app review pending
- Communicate delay to users

---

## Success Criteria

### Quantitative Metrics

| Metric | Baseline | Target | Measurement Period |
|--------|----------|--------|-------------------|
| Support tickets "prices not updating" | 20/month | <5/month | 30 days post-launch |
| User session duration | 5 min avg | +15% (5.75 min) | 30 days post-launch |
| Trading activity during market hours | Baseline | +20% | 30 days post-launch |
| API error rate | 0.05% | <0.1% | Continuous |
| API response time p95 | 150ms | <200ms | Continuous |
| User retention (7-day) | 60% | +10% (66%) | 30 days post-launch |

### Qualitative Metrics

- **User Feedback:** Positive sentiment >80% in app reviews mentioning "market status"
- **Support Team Feedback:** Reduction in confusion-related tickets
- **Product Team Satisfaction:** Feature meets all acceptance criteria
- **Stakeholder Approval:** Positive sign-off from Product Owner and Tech Lead

### Exit Criteria for Project Completion

✅ **All phases complete:**
- [ ] Backend API deployed and stable
- [ ] Mobile UI deployed to app stores
- [ ] Web UI deployed to production
- [ ] All tests passing (>80% coverage)
- [ ] Performance targets met
- [ ] Accessibility audit passed
- [ ] User communication sent
- [ ] 30-day monitoring complete
- [ ] Success metrics achieved
- [ ] Retrospective conducted
- [ ] Documentation handed off to support team

---

## Appendix A: Team Roles & Responsibilities

| Role | Name | Responsibilities | Time Allocation |
|------|------|-----------------|----------------|
| **Product Owner** | [TBD] | Prioritization, acceptance, stakeholder communication | 10% |
| **Tech Lead** | [TBD] | Architecture decisions, code reviews, unblocking | 25% |
| **Backend Developer** | [TBD] | API development, SignalR, data services | 100% (Weeks 1-2) |
| **Frontend Developer (Mobile)** | [TBD] | React Native components, mobile integration | 100% (Weeks 3-4) |
| **Frontend Developer (Web)** | [TBD] | React components, web integration | 100% (Weeks 5-6) |
| **QA Engineer** | [TBD] | Testing strategy, E2E tests, bug bash | 100% (Week 7) |
| **DevOps Engineer** | [TBD] | Deployment, monitoring, infrastructure | 50% (Week 8) |
| **UX Designer** | [TBD] | UI review, accessibility audit, user feedback | 25% (Weeks 3-6) |

---

## Appendix B: Development Environment Setup

### Backend Setup
```bash
cd backend/MyTrader.Api
cp appsettings.Development.json.example appsettings.Development.json
# Edit appsettings.Development.json with your local config

dotnet restore
dotnet ef database update
dotnet run
```

### Mobile Setup
```bash
cd frontend/mobile
cp .env.example .env
# Edit .env with your local API endpoint

npm install
npx expo start

# For iOS
npx expo run:ios

# For Android
npx expo run:android
```

### Web Setup
```bash
cd frontend/web
cp .env.example .env
# Edit .env with your local API endpoint

npm install
npm run dev
```

---

## Appendix C: Useful Commands

### Testing
```bash
# Backend unit tests
cd backend/MyTrader.Tests
dotnet test

# Backend integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Mobile unit tests
cd frontend/mobile
npm test

# Mobile E2E tests (Detox)
npm run test:e2e:ios
npm run test:e2e:android

# Web unit tests
cd frontend/web
npm test

# Web E2E tests (Playwright)
npm run test:e2e
```

### Deployment
```bash
# Build mobile app
cd frontend/mobile
eas build --platform ios --profile production
eas build --platform android --profile production

# Build web app
cd frontend/web
npm run build

# Deploy backend (Azure)
cd backend/MyTrader.Api
az webapp deployment source config-zip \
  --resource-group mytrader-rg \
  --name mytrader-api \
  --src ./publish.zip
```

---

**END OF ROADMAP**
