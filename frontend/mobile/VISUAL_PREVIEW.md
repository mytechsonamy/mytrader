# Market Status Indicators - Visual Preview

## Accordion Header (Compact Mode)
```
┌────────────────────────────────────────────────┐
│  🏢  BIST 100                          ● ▼     │
│     2↗ 1↘ Ort: +1.23%                          │
└────────────────────────────────────────────────┘
```

## Asset Card - Compact Mode (NEW DESIGN)
```
┌────────────────────────────────────────────────┐
│ 🏢 AKBNK      Akbank               ₺45.32      │
│               T. A.Ş.             +2.15%      │
├────────────────────────────────────────────────┤
│ ● Açık                    5 dakika önce        │
└────────────────────────────────────────────────┘

┌────────────────────────────────────────────────┐
│ 🇺🇸 AAPL      Apple Inc.          $189.50      │
│                                    -0.85%      │
├────────────────────────────────────────────────┤
│ ● Kapalı                   Kapalı - Dün 18:00  │
└────────────────────────────────────────────────┘

┌────────────────────────────────────────────────┐
│ 🚀 BTC        Bitcoin             $67,234.50    │
│                                    +3.42%      │
├────────────────────────────────────────────────┤
│ ● Açık                           Şimdi         │
└────────────────────────────────────────────────┘
```

## Color Legend
- 🟢 (●) Green  - Market OPEN
- 🔴 (●) Red    - Market CLOSED  
- 🟡 (●) Yellow - PRE_MARKET / POST_MARKET
- ⚪ (●) Gray   - HOLIDAY

## Timestamp Examples
- **Şimdi** - Less than 1 minute ago
- **5 dakika önce** - 5 minutes ago
- **14:32** - Earlier today
- **Dün 18:00** - Yesterday at 18:00
- **2 gün önce** - 2 days ago

## Context Messages
- **OPEN:** "Son güncelleme: 14:32"
- **CLOSED:** "Piyasa Kapalı - Son: Dün 18:00"
- **PRE_MARKET:** "Ön Piyasa - 09:15"
- **POST_MARKET:** "Kapanış Sonrası - 18:30"
- **HOLIDAY:** "Tatil - 2 gün önce"

## Implementation Highlights
✅ Colored status dots in all cards
✅ Turkish localized status text
✅ Relative time formatting
✅ Context-aware messages
✅ Responsive layout
✅ Type-safe implementation
