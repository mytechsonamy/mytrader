# Market Status Indicator - UI Mockups & Specifications
**Document Version:** 1.0
**Date:** 2025-10-09
**Related Document:** MARKET_STATUS_INDICATOR_REQUIREMENTS.md

---

## Mobile UI Mockups (React Native)

### 1. Dashboard - Accordion Headers with Market Status

```
┌─────────────────────────────────────────────────────────────┐
│ myTrader Dashboard                                    [≡]   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ Piyasa Durumları                                            │
│ [●] BIST Açık   [●] NASDAQ Kapalı   [●] CRYPTO Açık       │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ ▼ 🏢 Hisse Senetleri (5)                [●] Piyasa: Kapalı │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐  │
│   │ AAPL • Apple Inc.                                   │  │
│   │ $150.25                                             │  │
│   │ Son Güncelleme: 16:00                              │  │
│   │ +2.5% • +$3.75                                      │  │
│   └─────────────────────────────────────────────────────┘  │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐  │
│   │ TSLA • Tesla Inc.                                   │  │
│   │ $245.80                                             │  │
│   │ Piyasa Kapalı - Son: 16:00                         │  │
│   │ -1.2% • -$2.95                                      │  │
│   └─────────────────────────────────────────────────────┘  │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ ▼ 🚀 Kripto Paralar (3)                   [●] Piyasa: Açık │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐  │
│   │ BTCUSDT • Bitcoin                                   │  │
│   │ $42,350.50                                          │  │
│   │ Az önce                                             │  │
│   │ +3.2% • +$1,315.00                                  │  │
│   └─────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘

Legend:
[●] Green dot = Market OPEN
[●] Red dot = Market CLOSED
[●] Yellow dot = PRE_MARKET / AFTER_HOURS
```

---

### 2. Market Status Tooltip (Tap-to-Show)

**Trigger:** Tap on [●] Piyasa: Kapalı indicator

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                    ┌───────────────────────────────────┐    │
│                    │ NYSE - Piyasa Durumu              │    │
│                    │                                   │    │
│                    │ Durum: [●] Kapalı                 │    │
│                    │                                   │    │
│                    │ Sıradaki Açılış:                  │    │
│                    │ Yarın 09:30 EST                   │    │
│                    │ (15:30 Türkiye Saati)             │    │
│                    │                                   │    │
│                    │ İşlem Saatleri:                   │    │
│                    │ 09:30 - 16:00 EST                 │    │
│                    │                                   │    │
│                    │ Yerel Saat: 20:15 EST             │    │
│                    │                                   │    │
│                    │ Kapanış Nedeni:                   │    │
│                    │ Günlük işlem saatleri dışında    │    │
│                    │                                   │    │
│                    │           [Tamam]                 │    │
│                    └───────────────────────────────────┘    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

### 3. Pre-Market Status Indicator

```
┌─────────────────────────────────────────────────────────────┐
│ ▼ 🏢 Hisse Senetleri (5)        [●] Piyasa: Açılış Öncesi  │
│                                  ↑ Pulsing yellow dot       │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐  │
│   │ AAPL • Apple Inc.                                   │  │
│   │ $150.25                                             │  │
│   │ Açılış öncesi • Son: 08:45                          │  │
│   │ +0.8% • +$1.20                                      │  │
│   │ ⏰ 45 dakika içinde açılış                          │  │
│   └─────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

### 4. Data Staleness Warning

```
┌─────────────────────────────────────────────────────────────┐
│ ▼ 🏢 Hisse Senetleri (5)                [●] Piyasa: Açık   │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐  │
│   │ AAPL • Apple Inc.                                   │  │
│   │ $150.25                                             │  │
│   │ ⚠ Son Güncelleme: 25 dakika önce                   │  │
│   │ +2.5% • +$3.75                                      │  │
│   │                                                     │  │
│   │ [!] Veri güncel olmayabilir                        │  │
│   │     Tap for details                                 │  │
│   └─────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘

Tap on warning shows:
┌───────────────────────────────────────┐
│ Veri Bağlantı Sorunu                  │
│                                       │
│ Son güncelleme: 25 dakika önce        │
│ Beklenen sıklık: 15 dakika            │
│                                       │
│ Gösterilen fiyat güncel olmayabilir.  │
│ Yeniden bağlanılıyor...               │
│                                       │
│           [Tamam]                     │
└───────────────────────────────────────┘
```

---

### 5. Holiday Status

```
┌─────────────────────────────────────────────────────────────┐
│ ▼ 🏢 Hisse Senetleri (5)              [●] Piyasa: Tatil    │
│                                        🎉 Thanksgiving       │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐  │
│   │ AAPL • Apple Inc.                                   │  │
│   │ $150.25                                             │  │
│   │ Piyasa Kapalı - Thanksgiving Day                    │  │
│   │ Sıradaki Açılış: Cuma 09:30 EST                    │  │
│   │ +2.5% • +$3.75 (Son işlem: Çarşamba)               │  │
│   └─────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

### 6. Crypto Always-Open Status

```
┌─────────────────────────────────────────────────────────────┐
│ ▼ 🚀 Kripto Paralar (3)             [●] Piyasa: Açık 24/7  │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐  │
│   │ BTCUSDT • Bitcoin                                   │  │
│   │ $42,350.50                                          │  │
│   │ Az önce                                             │  │
│   │ +3.2% • +$1,315.00                                  │  │
│   └─────────────────────────────────────────────────────┘  │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐  │
│   │ ETHUSDT • Ethereum                                  │  │
│   │ $2,245.80                                           │  │
│   │ 2 saniye önce                                       │  │
│   │ +1.8% • +$39.50                                     │  │
│   └─────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘

Note: No "next open/close" times shown for crypto
```

---

## Web UI Mockups (React)

### 7. Dashboard - Market Overview Bar (Top)

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ myTrader                                                     Profile ▼    [≡]       │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│ Piyasa Durumları                                           Son Güncelleme: 15:30   │
│                                                                                     │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────┐│
│  │ [●] BIST         │  │ [●] NASDAQ       │  │ [●] NYSE         │  │ [●] CRYPTO  ││
│  │ Açık             │  │ Kapalı           │  │ Kapalı           │  │ Açık (24/7) ││
│  │ Kapanış: 18:00   │  │ Açılış: 09:30    │  │ Açılış: 09:30    │  │ Her Zaman   ││
│  │ +45m kaldı       │  │ Yarın 09:30 EST  │  │ Yarın 09:30 EST  │  │ Açık        ││
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  └─────────────┘│
│                                                                                     │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│ ▼ 🏢 Hisse Senetleri (5 symbol)                           [●] NASDAQ: Kapalı      │
│                                                                                     │
│ ┌─────────┬──────────────────┬──────────┬──────────┬────────────┬─────────────────┐│
│ │ Symbol  │ Name             │ Price    │ Change   │ Volume     │ Last Update     ││
│ ├─────────┼──────────────────┼──────────┼──────────┼────────────┼─────────────────┤│
│ │ AAPL    │ Apple Inc.       │ $150.25  │ +2.5%    │ 54.2M      │ 16:00 • Kapalı  ││
│ │ TSLA    │ Tesla Inc.       │ $245.80  │ -1.2%    │ 102.8M     │ 16:00 • Kapalı  ││
│ │ MSFT    │ Microsoft Corp.  │ $380.50  │ +1.8%    │ 28.5M      │ 16:00 • Kapalı  ││
│ └─────────┴──────────────────┴──────────┴──────────┴────────────┴─────────────────┘│
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘

Hover on [●] NASDAQ: Kapalı shows tooltip (see mockup #10)
```

---

### 8. Market Status Card (Hover State)

```
┌──────────────────────────────────────────────────────┐
│  ┌──────────────────┐        ← On hover, card elevates
│  │ [●] NASDAQ       │           and shows border
│  │ Kapalı           │
│  │ Açılış: 09:30    │        ┌──────────────────────────┐
│  │ Yarın 09:30 EST  │───────►│ NASDAQ Market Status     │
│  └──────────────────┘        │                          │
│                              │ Status: CLOSED            │
│                              │ Next Open: Tomorrow 09:30 │
│                              │ Trading Hours: 09:30-16:00│
│                              │ Current Time: 20:15 EST   │
│                              │                          │
│                              │ Closure Reason:          │
│                              │ After trading hours      │
│                              └──────────────────────────┘
```

---

### 9. Symbol Table with Market Status Column

```
┌───────────────────────────────────────────────────────────────────────────────────────────────┐
│ ▼ 🏢 Hisse Senetleri (5 symbol)                                   [●] NASDAQ: Kapalı         │
│                                                                                               │
│ ┌────────┬──────────────────┬──────────┬──────────┬────────────┬──────────────┬────────────┐│
│ │ Symbol │ Name             │ Price    │ Change   │ Volume     │ Last Update  │ Market     ││
│ ├────────┼──────────────────┼──────────┼──────────┼────────────┼──────────────┼────────────┤│
│ │ AAPL   │ Apple Inc.       │ $150.25  │ +2.5%    │ 54.2M      │ 16:00        │ [●] Kapalı ││
│ │        │ NASDAQ           │          │ +$3.75   │            │ 4h 15m önce  │            ││
│ ├────────┼──────────────────┼──────────┼──────────┼────────────┼──────────────┼────────────┤│
│ │ THYAO  │ Türk Hava Yolları│ ₺245.80  │ +3.2%    │ 8.5M       │ 17:45        │ [●] Açık   ││
│ │        │ BIST             │          │ +₺7.65   │            │ 15 dk önce   │            ││
│ ├────────┼──────────────────┼──────────┼──────────┼────────────┼──────────────┼────────────┤│
│ │ BTCUSDT│ Bitcoin          │ $42,350  │ +1.8%    │ 1.2B       │ Az önce      │ [●] Açık   ││
│ │        │ Binance          │          │ +$748    │            │ 2s önce      │   (24/7)   ││
│ └────────┴──────────────────┴──────────┴──────────┴────────────┴──────────────┴────────────┘│
│                                                                                               │
└───────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

### 10. Market Status Tooltip (Hover) - Detailed

```
┌──────────────────────────────────────────────────────────────┐
│ ▼ 🏢 Hisse Senetleri    [●] NASDAQ: Kapalı                   │
│                              ↑                                │
│                              │ Hover trigger                  │
│                         ┌────┴──────────────────────────────┐ │
│                         │ NASDAQ - Market Status            │ │
│                         │                                   │ │
│                         │ Status: [●] CLOSED                │ │
│                         │                                   │ │
│                         │ Trading Hours (EST):              │ │
│                         │ • Regular: 09:30 - 16:00          │ │
│                         │ • Pre-Market: 04:00 - 09:30       │ │
│                         │ • After-Hours: 16:00 - 20:00      │ │
│                         │                                   │ │
│                         │ Current Time: 20:15 EST           │ │
│                         │ Next Open: Tomorrow 09:30 EST     │ │
│                         │            (15:30 Türkiye Saati)  │ │
│                         │                                   │ │
│                         │ Closure Reason:                   │ │
│                         │ After trading hours               │ │
│                         │                                   │ │
│                         │ Trading Day: 2025-10-09           │ │
│                         │ Timezone: America/New_York        │ │
│                         └───────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

---

### 11. Data Delay Badge

```
┌─────────────────────────────────────────────────────────────┐
│ AAPL • Apple Inc.                           [i] 15 dk gecikme│
│ $150.25                                     ↑                │
│ +2.5% • +$3.75                              │                │
│                                             │                │
│                           Hover: ┌──────────┴──────────────┐ │
│                                  │ Veri Kaynağı Bilgisi    │ │
│                                  │                         │ │
│                                  │ Kaynak: Yahoo Finance   │ │
│                                  │ Gecikme: 15 dakika      │ │
│                                  │ Son Güncelleme: 15:30   │ │
│                                  │                         │ │
│                                  │ Gerçek zamanlı veri için│ │
│                                  │ premium hesaba geçin.   │ │
│                                  └─────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

### 12. Real-time Data Badge

```
┌─────────────────────────────────────────────────────────────┐
│ AAPL • Apple Inc.                       [●] Gerçek zamanlı  │
│ $150.25                                 ↑ Pulsing green dot  │
│ Az önce güncellendi                                         │
│ +2.5% • +$3.75                                              │
│                                                             │
└─────────────────────────────────────────────────────────────┘

Hover on badge:
┌──────────────────────────────────────┐
│ Gerçek Zamanlı Veri                  │
│                                      │
│ Kaynak: Alpaca Markets (IEX)         │
│ Gecikme: <5 saniye                   │
│ Son Güncelleme: 2 saniye önce        │
│                                      │
│ Fiyatlar anlık olarak güncellenir.   │
└──────────────────────────────────────┘
```

---

## Component Specifications

### 13. MarketStatusBadge Component (Mobile)

**Props:**
```typescript
interface MarketStatusBadgeProps {
  marketCode: string;           // "BIST" | "NASDAQ" | "NYSE" | "BINANCE"
  status: MarketStatus;         // Enum
  nextOpenTime?: string;        // ISO 8601 UTC
  nextCloseTime?: string;       // ISO 8601 UTC
  compact?: boolean;            // Default: false
  showTime?: boolean;           // Default: true
  onPress?: () => void;         // Tap handler
}
```

**States:**
- Default: `[●] Piyasa: Açık`
- Hover/Pressed: Slight scale up (1.05x) + opacity change
- Disabled: Grayed out

**Variants:**
- `compact`: `[●]` (dot only, no text)
- `full`: `[●] Piyasa: Açık`
- `detailed`: `[●] Piyasa: Açık • Kapanış: 18:00`

**Animations:**
- Dot pulse animation for PRE_MARKET / AFTER_HOURS status
- Fade-in when status changes
- Rotate icon on status change

---

### 14. MarketStatusTooltip Component (Web)

**Props:**
```typescript
interface MarketStatusTooltipProps {
  marketStatus: MarketStatusDto;
  trigger: "hover" | "click";   // Default: hover
  placement: "top" | "bottom" | "left" | "right";
  children: React.ReactNode;    // Trigger element
}
```

**Content Sections:**
1. **Header:** Market name + status badge
2. **Trading Hours:** Regular, pre-market, after-hours
3. **Current Time:** Local market time
4. **Next Event:** Next open/close with countdown
5. **Closure Reason:** If closed
6. **Trading Day:** Current trading day date
7. **Timezone:** IANA timezone ID

**Styling:**
- Background: White (`#ffffff`)
- Border: 1px solid gray-200 (`#e5e7eb`)
- Shadow: `0 4px 6px rgba(0, 0, 0, 0.1)`
- Border radius: 8px
- Padding: 16px
- Max width: 320px

---

### 15. DataStalenessWarning Component

**Props:**
```typescript
interface DataStalenessWarningProps {
  lastUpdateTime: string;       // ISO 8601 UTC
  expectedFrequencyMinutes: number;
  severity: "warning" | "error";
  marketStatus: MarketStatus;
  onDismiss?: () => void;
}
```

**Display Logic:**
```typescript
const shouldShowWarning = (
  marketStatus: MarketStatus,
  stalenessMinutes: number,
  expectedFrequency: number
): boolean => {
  // Never show warning when market closed
  if (marketStatus === "CLOSED") return false;

  // Crypto: warn after 1 minute
  if (marketStatus === "OPEN" && isCrypto) {
    return stalenessMinutes > 1;
  }

  // Stock realtime: warn after expected frequency + 5 min
  if (marketStatus === "OPEN" && isRealtime) {
    return stalenessMinutes > expectedFrequency + 5;
  }

  // Stock delayed: warn after expected frequency + 10 min
  return stalenessMinutes > expectedFrequency + 10;
};
```

**Variants:**
- `warning`: Yellow triangle icon, dismissible
- `error`: Red error icon, persistent

---

### 16. MarketOverviewBar Component (Web)

**Props:**
```typescript
interface MarketOverviewBarProps {
  markets: MarketStatusDto[];
  onMarketClick?: (marketCode: string) => void;
  showCountdown?: boolean;      // Default: true
  autoRefresh?: boolean;        // Default: true
  refreshIntervalSeconds?: number; // Default: 300 (5 min)
}
```

**Layout:**
- Horizontal scrollable on mobile
- 4-column grid on desktop (BIST, NASDAQ, NYSE, CRYPTO)
- Sticky position at top of dashboard
- Collapse button for mobile (minimize to icon strip)

**Content per Market:**
- Market name
- Status badge with colored dot
- Next event (open/close) with time
- Countdown timer (optional)

---

## Responsive Design Breakpoints

### Mobile (< 768px)
- Single column layout
- Compact market status badges
- Bottom sheet tooltips (modal)
- Horizontal scroll for market overview
- Font sizes: 11px status, 10px timestamp

### Tablet (768px - 1024px)
- 2-column symbol grid
- Full market status badges
- Popover tooltips
- Fixed market overview bar
- Font sizes: 12px status, 11px timestamp

### Desktop (> 1024px)
- Multi-column symbol grid
- Full market status badges with details
- Hover tooltips
- Expanded market overview bar
- Font sizes: 12px status, 11px timestamp

---

## Animation Specifications

### 1. Status Dot Pulse (PRE_MARKET / AFTER_HOURS)

```css
@keyframes pulse {
  0% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.6;
    transform: scale(1.1);
  }
  100% {
    opacity: 1;
    transform: scale(1);
  }
}

.status-dot.pre-market,
.status-dot.after-hours {
  animation: pulse 2s ease-in-out infinite;
}
```

### 2. Status Change Transition

```css
.market-status-badge {
  transition: all 0.3s ease-in-out;
}

.market-status-badge.changing {
  animation: statusChange 0.5s ease-in-out;
}

@keyframes statusChange {
  0% { opacity: 1; }
  50% { opacity: 0.3; transform: scale(0.95); }
  100% { opacity: 1; transform: scale(1); }
}
```

### 3. Warning Icon Attention

```css
@keyframes attention {
  0%, 100% { transform: rotate(0deg); }
  25% { transform: rotate(-10deg); }
  75% { transform: rotate(10deg); }
}

.staleness-warning-icon {
  animation: attention 2s ease-in-out infinite;
}
```

### 4. Countdown Timer

```typescript
const CountdownTimer: React.FC<{ targetTime: string }> = ({ targetTime }) => {
  const [timeLeft, setTimeLeft] = useState<string>('');

  useEffect(() => {
    const interval = setInterval(() => {
      const now = Date.now();
      const target = new Date(targetTime).getTime();
      const diff = target - now;

      if (diff <= 0) {
        setTimeLeft('Az önce');
        clearInterval(interval);
        return;
      }

      const hours = Math.floor(diff / (1000 * 60 * 60));
      const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

      setTimeLeft(`${hours}s ${minutes}dk içinde`);
    }, 1000);

    return () => clearInterval(interval);
  }, [targetTime]);

  return <Text>{timeLeft}</Text>;
};
```

---

## Accessibility Specifications

### Screen Reader Support

**Market Status Badge:**
```html
<button
  aria-label="NASDAQ market status: Closed. Next open: Tomorrow 9:30 AM EST"
  role="button"
  tabIndex={0}
>
  <span aria-hidden="true">[●]</span>
  <span>Piyasa: Kapalı</span>
</button>
```

**Status Dot:**
```html
<span
  className="status-dot"
  role="img"
  aria-label="Market status: Open"
  style={{ backgroundColor: '#10b981' }}
/>
```

**Tooltip:**
```html
<div
  role="tooltip"
  aria-live="polite"
  aria-describedby="market-status-details"
>
  <!-- Tooltip content -->
</div>
```

### Keyboard Navigation

- Tab: Focus next market status badge
- Shift+Tab: Focus previous badge
- Enter/Space: Open tooltip/modal
- Escape: Close tooltip/modal
- Arrow keys: Navigate within tooltip

### Focus States

```css
.market-status-badge:focus {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
}

.market-status-badge:focus-visible {
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.3);
}
```

---

## Dark Mode Support (Future Enhancement)

### Color Palette Adjustments

| Element | Light Mode | Dark Mode |
|---------|-----------|-----------|
| Background | `#ffffff` | `#1f2937` |
| Text Primary | `#1f2937` | `#f9fafb` |
| Text Secondary | `#6b7280` | `#9ca3af` |
| Status Open | `#10b981` | `#34d399` |
| Status Closed | `#ef4444` | `#f87171` |
| Status Warning | `#f59e0b` | `#fbbf24` |
| Border | `#e5e7eb` | `#374151` |
| Tooltip BG | `#ffffff` | `#374151` |

### Implementation

```typescript
const getStatusColor = (status: MarketStatus, isDarkMode: boolean) => {
  const colors = {
    OPEN: isDarkMode ? '#34d399' : '#10b981',
    CLOSED: isDarkMode ? '#f87171' : '#ef4444',
    PRE_MARKET: isDarkMode ? '#fbbf24' : '#f59e0b',
    AFTER_HOURS: isDarkMode ? '#fbbf24' : '#f59e0b',
  };
  return colors[status] || (isDarkMode ? '#9ca3af' : '#6b7280');
};
```

---

## Testing Checklist

### Visual Regression Testing

- [ ] Market status badge renders correctly in all states
- [ ] Status dot color matches specification
- [ ] Tooltip positioning correct in all placements
- [ ] Responsive layout adapts to screen sizes
- [ ] Animations smooth and performant (60fps)

### Interaction Testing

- [ ] Tap/click opens tooltip
- [ ] Hover triggers tooltip on web
- [ ] Dismiss behavior works (tap outside, Escape key)
- [ ] Status updates reflected immediately
- [ ] Countdown timer counts down accurately

### Accessibility Testing

- [ ] Screen reader announces status correctly
- [ ] Keyboard navigation works
- [ ] Focus states visible
- [ ] Color contrast meets WCAG AA (4.5:1)
- [ ] Touch targets at least 44x44px

### Cross-browser Testing

- [ ] Chrome (desktop + mobile)
- [ ] Safari (desktop + iOS)
- [ ] Firefox (desktop)
- [ ] Edge (desktop)
- [ ] Samsung Internet (mobile)

---

**END OF MOCKUPS DOCUMENT**
