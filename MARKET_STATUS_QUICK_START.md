# Market Status Indicator - Quick Start Guide
**For:** Developers joining the project
**Last Updated:** 2025-10-09

---

## TL;DR - What We're Building

Adding market status indicators (open/closed) across the platform so users know:
- ✅ Is the market currently open?
- ⏰ When does it open/close next?
- 📊 Is my data real-time or delayed?
- ⚠ Is my data stale?

**Backend:** ~60% already done! We have market hours service, just need API endpoints + SignalR.
**Frontend:** Components exist but not integrated. Need to wire up + add staleness warnings.

---

## Project Documents (Read This Order)

1. **START HERE:** `MARKET_STATUS_EXECUTIVE_SUMMARY.md` (15 min read)
   - Problem, solution, timeline, costs

2. **FOR PMs/BAs:** `MARKET_STATUS_INDICATOR_REQUIREMENTS.md` (60 min read)
   - Complete requirements, user stories, acceptance criteria

3. **FOR DESIGNERS:** `MARKET_STATUS_UI_MOCKUPS.md` (30 min read)
   - Visual mockups, component specs, responsive design

4. **FOR DEVELOPERS:** `MARKET_STATUS_IMPLEMENTATION_ROADMAP.md` (45 min read)
   - Task-by-task breakdown, code examples, testing strategy

5. **THIS DOCUMENT:** Quick reference while coding

---

## Architecture at a Glance

```
Frontend (Mobile/Web)
  ↓ API calls (/markets/status/all)
  ↓ SignalR WebSocket (market status changes)
Backend
  ↓ MarketStatusBroadcastService (checks every 1 min)
  ↓ MarketHoursService (calculates status)
Database
  ↓ markets, trading_sessions, holidays
```

**Data Flow:**
1. Backend calculates market status every 1 minute
2. If status changes (CLOSED → OPEN), broadcast via SignalR
3. Frontend receives event, updates UI
4. Frontend also polls `/markets/status/all` every 5 minutes as fallback

---

## Key Files (Where to Start)

### Backend (Already Exist - Enhance These)

**Core Logic (DONE):**
- `backend/MyTrader.Core/Services/MarketHoursService.cs` - Market status calculation
- `backend/MyTrader.Services/Market/MarketStatusService.cs` - Status management
- `backend/MyTrader.Core/Interfaces/IMarketHoursService.cs` - Interface
- `backend/MyTrader.Core/Models/TradingSession.cs` - Trading hours model
- `backend/MyTrader.Core/Models/Market.cs` - Market entity

**Need to Create:**
- `backend/MyTrader.Api/Controllers/MarketsController.cs` - NEW API endpoints
- `backend/MyTrader.Api/Services/MarketStatusBroadcastService.cs` - NEW SignalR broadcaster
- `backend/MyTrader.Core/Services/DataStalenessService.cs` - NEW staleness detection

**Need to Enhance:**
- `backend/MyTrader.Core/DTOs/UnifiedMarketDataDto.cs` - Add market status fields
- `backend/MyTrader.Api/Hubs/MarketDataHub.cs` - Add market status events

### Frontend Mobile (Components Exist - Need Integration)

**Already Built (Not Integrated):**
- `frontend/mobile/src/components/dashboard/MarketStatusIndicator.tsx` - Status badge component
- `frontend/mobile/src/context/MarketStatusContext.tsx` - Context provider

**Need to Enhance:**
- `frontend/mobile/src/components/dashboard/AssetClassAccordion.tsx` - Integrate badge
- `frontend/mobile/src/components/dashboard/SymbolCard.tsx` - Add timestamp
- `frontend/mobile/src/services/api.ts` - Add market status API calls
- `frontend/mobile/src/services/websocketService.ts` - Subscribe to status events

**Need to Create:**
- `frontend/mobile/src/components/dashboard/MarketStatusModal.tsx` - Detail modal
- `frontend/mobile/src/components/dashboard/StalenessWarning.tsx` - Warning UI
- `frontend/mobile/src/components/dashboard/DataSourceBadge.tsx` - Realtime/delayed badge

### Frontend Web (Need to Build)

**Need to Create (All New):**
- `frontend/web/src/components/shared/MarketStatusBadge.tsx` - Status badge
- `frontend/web/src/components/dashboard/MarketOverviewBar.tsx` - Top overview
- `frontend/web/src/components/shared/MarketStatusTooltip.tsx` - Hover tooltip
- `frontend/web/src/components/shared/StalenessWarning.tsx` - Warning icon
- `frontend/web/src/components/shared/DataSourceBadge.tsx` - Realtime badge
- `frontend/web/src/hooks/useMarketStatus.ts` - Custom hook
- `frontend/web/src/services/marketStatusService.ts` - API service

---

## API Endpoints to Create

### GET /api/markets/status/all
**Returns:** Status for all markets (BIST, NASDAQ, NYSE, CRYPTO)

**Response Example:**
```json
{
  "requestTimestamp": "2025-10-09T20:15:00Z",
  "markets": [
    {
      "marketId": "uuid",
      "marketCode": "NASDAQ",
      "marketName": "NASDAQ Stock Market",
      "status": "CLOSED",
      "isOpen": false,
      "currentTime": "2025-10-09T20:15:00-04:00",
      "nextOpenTime": "2025-10-10T13:30:00Z",
      "nextCloseTime": "2025-10-10T20:00:00Z",
      "timezone": "America/New_York",
      "timezoneOffset": -4,
      "isHoliday": false,
      "closureReason": "After trading hours",
      "regularHours": { "open": "09:30", "close": "16:00" }
    }
  ]
}
```

### GET /api/markets/status/{marketCode}
**Returns:** Status for specific market (BIST, NASDAQ, NYSE, BINANCE)

### SignalR Event: OnMarketStatusChanged
**Fired when:** Market status changes (e.g., CLOSED → OPEN at 9:30)

**Message Format:**
```typescript
{
  type: "market_status_update",
  data: {
    marketCode: "NASDAQ",
    previousStatus: "CLOSED",
    newStatus: "OPEN",
    timestamp: "2025-10-10T13:30:00Z",
    affectedSymbols: ["aapl-uuid", "tsla-uuid"],
    message: "NASDAQ market has opened"
  }
}
```

---

## Market Status States

```typescript
enum MarketStatus {
  OPEN = "OPEN",                 // Regular trading hours
  CLOSED = "CLOSED",             // Outside all sessions
  PRE_MARKET = "PRE_MARKET",     // Before open (US only)
  AFTER_HOURS = "AFTER_HOURS",   // After close (US only)
  HOLIDAY = "HOLIDAY",           // Market holiday
  HALTED = "HALTED",             // Trading suspended
  UNKNOWN = "UNKNOWN"            // Cannot determine
}
```

## Market Trading Hours Reference

| Market | Timezone | Regular Hours | Pre-Market | After-Hours |
|--------|----------|---------------|------------|-------------|
| **BIST** | Europe/Istanbul (UTC+3) | 10:00 - 18:00 | 09:40 - 10:00 | None |
| **NASDAQ** | America/New_York (EST/EDT) | 09:30 - 16:00 | 04:00 - 09:30 | 16:00 - 20:00 |
| **NYSE** | America/New_York (EST/EDT) | 09:30 - 16:00 | 04:00 - 09:30 | 16:00 - 20:00 |
| **CRYPTO** | UTC | 24/7 | N/A | N/A |

---

## Color Codes (Use These!)

```typescript
const STATUS_COLORS = {
  OPEN: '#10b981',           // Green 500
  PRE_MARKET: '#f59e0b',     // Amber 500
  AFTER_HOURS: '#f59e0b',    // Amber 500
  CLOSED: '#ef4444',         // Red 500
  HOLIDAY: '#ef4444',        // Red 500
  UNKNOWN: '#6b7280',        // Gray 500
};
```

**Accessibility:** Never use color alone! Always include icon + text + color.

---

## Staleness Thresholds (Business Rules)

```typescript
const STALENESS_THRESHOLDS = {
  CRYPTO_OPEN: 30,              // 30 seconds
  STOCK_REALTIME_OPEN: 30,      // 30 seconds
  STOCK_DELAYED_OPEN: 1200,     // 20 minutes
  PRE_MARKET: 300,              // 5 minutes
  AFTER_HOURS: 300,             // 5 minutes
  CLOSED: Infinity,             // No warning when closed
};
```

**Rule:** Only show staleness warning when market is OPEN/PRE_MARKET/AFTER_HOURS.

---

## Testing Strategy

### Unit Tests (Must Have)

**Backend:**
```csharp
[Fact]
public void GetMarketStatus_DuringBistOpenHours_ReturnsOpen()
{
    // Arrange
    var service = new MarketHoursService(cache, logger);
    MockCurrentTime("2025-10-09T14:00:00+03:00"); // 2pm TRT, Wed

    // Act
    var status = service.GetMarketStatus(Exchange.BIST);

    // Assert
    Assert.Equal(MarketStatus.OPEN, status.State);
}

[Fact]
public void GetMarketStatus_DuringBistWeekend_ReturnsClosed()
{
    MockCurrentTime("2025-10-11T14:00:00+03:00"); // Saturday
    var status = service.GetMarketStatus(Exchange.BIST);
    Assert.Equal(MarketStatus.CLOSED, status.State);
}
```

**Frontend:**
```typescript
describe('MarketStatusBadge', () => {
  it('renders green dot when market open', () => {
    render(<MarketStatusBadge status="OPEN" />);
    const dot = screen.getByRole('img', { name: /market status/i });
    expect(dot).toHaveStyle({ backgroundColor: '#10b981' });
  });

  it('shows "Açık" text in Turkish', () => {
    render(<MarketStatusBadge status="OPEN" />);
    expect(screen.getByText('Açık')).toBeInTheDocument();
  });
});
```

### Integration Tests (Must Have)

```csharp
[Fact]
public async Task GetAllMarketStatuses_ReturnsCorrectCount()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/markets/status/all");
    var data = await response.Content.ReadFromJsonAsync<MarketStatusCollectionDto>();

    // Assert
    Assert.Equal(4, data.Markets.Count); // BIST, NASDAQ, NYSE, CRYPTO
}
```

### E2E Tests (Nice to Have)

**Playwright (Web):**
```typescript
test('market status updates when status changes', async ({ page }) => {
  await page.goto('/dashboard');

  // Mock SignalR event
  await page.evaluate(() => {
    window.signalRConnection.trigger('OnMarketStatusChanged', {
      marketCode: 'NASDAQ',
      newStatus: 'OPEN'
    });
  });

  // Verify UI updated
  await expect(page.locator('[data-market="NASDAQ"]')).toContainText('Open');
});
```

**Critical Test Scenarios:**
1. ✅ Market status correct at market open time (09:30 EST, 10:00 TRT)
2. ✅ Market status correct at market close time (16:00 EST, 18:00 TRT)
3. ✅ Status updates in UI when SignalR event received
4. ✅ Staleness warning appears after threshold during market hours
5. ✅ No staleness warning when market closed
6. ✅ Timestamp updates every second
7. ✅ Tooltip shows correct information
8. ✅ Works on mobile Safari and Chrome

---

## Common Gotchas

### 1. Timezone Handling
**WRONG:**
```typescript
const openTime = new Date('2025-10-10T09:30:00'); // Ambiguous!
```

**RIGHT:**
```typescript
// Backend: Always UTC
const openTimeUtc = new Date('2025-10-10T13:30:00Z'); // 09:30 EST = 13:30 UTC

// Frontend: Convert to user timezone
const openTimeLocal = new Date(openTimeUtc).toLocaleString('tr-TR', {
  timeZone: 'Europe/Istanbul'
});
```

### 2. DST Transitions
**Problem:** US markets observe DST, Turkey doesn't
**Solution:** Always store in UTC, use NodaTime for calculations

**Test Edge Case:**
- DST spring forward: 2025-03-09 02:00 EST → 03:00 EDT (2am doesn't exist!)
- DST fall back: 2025-11-02 02:00 EDT → 01:00 EST (1am-2am repeats!)

### 3. SignalR Connection Management
**WRONG:**
```typescript
connection.on('OnMarketStatusChanged', updateStatus); // Memory leak!
```

**RIGHT:**
```typescript
useEffect(() => {
  connection.on('OnMarketStatusChanged', updateStatus);
  return () => {
    connection.off('OnMarketStatusChanged', updateStatus); // Cleanup!
  };
}, [connection]);
```

### 4. Staleness Detection During Market Close
**WRONG:**
```typescript
if (stalenessSeconds > threshold) {
  showWarning(); // Will warn even when market closed!
}
```

**RIGHT:**
```typescript
if (marketStatus === 'OPEN' && stalenessSeconds > threshold) {
  showWarning(); // Only warn during market hours
}
```

### 5. Holiday Calendar
**Problem:** Holidays need annual update
**Solution:** Set reminder for December 1st each year to update next year's holidays

---

## Quick Commands

### Backend
```bash
# Run unit tests
cd backend/MyTrader.Tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~MarketHoursService"

# Run API locally
cd backend/MyTrader.Api
dotnet run
```

### Mobile
```bash
# Run app
cd frontend/mobile
expo start

# Run tests
npm test

# Run on iOS simulator
npx expo run:ios

# Run on Android emulator
npx expo run:android
```

### Web
```bash
# Run dev server
cd frontend/web
npm run dev

# Run tests
npm test

# Build for production
npm run build
```

---

## Debugging Tips

### Backend: Check Market Status Calculation
```bash
# Set breakpoint in MarketHoursService.cs line 162
# Run test: GetMarketStatus_DuringBistOpenHours_ReturnsOpen
# Step through to see timezone conversion

# Or use logging:
_logger.LogDebug("Market time: {Time}, Status: {Status}", localTime, status.State);
```

### Frontend: Check SignalR Events
```typescript
// Add console logging
connection.on('OnMarketStatusChanged', (message) => {
  console.log('Market status changed:', message);
  updateStatus(message);
});

// Check connection state
console.log('SignalR state:', connection.state);
```

### Check API Response
```bash
curl http://localhost:5000/api/markets/status/all | jq
```

---

## Performance Checklist

- [ ] Backend caches market status (1-minute TTL)
- [ ] Frontend polls max once per 5 minutes
- [ ] SignalR only broadcasts on actual status changes
- [ ] Market status calculation < 50ms
- [ ] API response time < 200ms p95
- [ ] Frontend render time < 100ms

---

## Before Submitting PR

### Backend PR Checklist
- [ ] Unit tests added and passing
- [ ] Integration tests added and passing
- [ ] API endpoint documented in Swagger
- [ ] Logging added for debugging
- [ ] Error handling for edge cases
- [ ] Performance meets targets (<200ms)

### Frontend PR Checklist
- [ ] Unit tests added and passing
- [ ] Component renders correctly in Storybook (if applicable)
- [ ] Works on iOS and Android (mobile)
- [ ] Works on Chrome, Safari, Firefox (web)
- [ ] Accessibility tested (screen reader)
- [ ] Turkish translations verified
- [ ] No console warnings/errors

---

## Who to Ask

| Question | Ask |
|----------|-----|
| Requirements clarification | Product Owner |
| Technical architecture | Tech Lead |
| Backend implementation | Backend Developer |
| Mobile UI/UX | Frontend Developer (Mobile) + UX Designer |
| Web UI/UX | Frontend Developer (Web) + UX Designer |
| Testing strategy | QA Engineer |
| Deployment issues | DevOps Engineer |

---

## Useful Links

- **Swagger API Docs:** http://localhost:5000/swagger
- **Figma Design:** [Link TBD]
- **JIRA Board:** [Link TBD]
- **Team Slack:** #mytrader-market-status

---

## Need Help?

1. **Read the full requirements:** `MARKET_STATUS_INDICATOR_REQUIREMENTS.md`
2. **Check the roadmap:** `MARKET_STATUS_IMPLEMENTATION_ROADMAP.md`
3. **Ask in Slack:** #mytrader-market-status
4. **Schedule pairing session:** With tech lead or assigned developer

---

**Happy Coding!** 🚀

---

**Last Updated:** 2025-10-09
**Maintainer:** Business Analyst
**Feedback:** Submit via Slack or email
