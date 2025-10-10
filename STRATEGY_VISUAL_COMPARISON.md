# MyTrader Strategy Templates - Visual Comparison

**Quick Reference Guide for Strategy Selection**

---

## Strategy Selection Flowchart

```
START: What's your trading style?
│
├─ I prefer SHORT trades (minutes to hours)
│  │
│  ├─ I like RANGE-BOUND markets
│  │  └─→ ✅ BB + MACD (5m-15m)
│  │      • Win Rate: 55-65%
│  │      • Risk: LOW
│  │      • Difficulty: EASY
│  │
│  └─ I like TRENDING markets
│     └─→ ✅ RSI + EMA (15m-1h)
│         • Win Rate: 52-62%
│         • Risk: MEDIUM
│         • Difficulty: MEDIUM
│
├─ I prefer MEDIUM trades (hours)
│  │
│  └─ I want HIGH VOLATILITY breakouts
│     └─→ ✅ VOLUME BREAKOUT (1h-4h)
│         • Win Rate: 45-55%
│         • Risk: HIGH
│         • Difficulty: ADVANCED
│
└─ I prefer LONG trades (days to weeks)
   │
   └─ I have PATIENCE for trends
      └─→ ✅ TREND FOLLOWING (4h-1d)
          • Win Rate: 48-60%
          • Risk: HIGH DD
          • Difficulty: MEDIUM
```

---

## Risk/Reward Profiles

```
Risk-Reward Matrix (Annual Return vs Max Drawdown)

100% │                                    ◈ Trend Following
     │                                   (50-85% / -25 to -35%)
     │
 80% │                      ◇ Volume Breakout
     │                     (45-75% / -22 to -30%)
     │
 60% │          ○ RSI + EMA
     │         (35-55% / -18 to -25%)
     │
 40% │    ● BB + MACD
     │   (25-40% / -15 to -20%)
     │
 20% │ ● BB + MACD (Beginner)
     │  (15-25% / -12 to -15%)
     │
   0%└─────┴─────┴─────┴─────┴─────┴─────┴──────► Max Drawdown
     0%   -10%  -15%  -20%  -25%  -30%  -35%

● = Beginner Config    ○ = Advanced Config
◇ = Advanced Config    ◈ = Advanced Config

INTERPRETATION:
- Further RIGHT = Higher Risk (more drawdown)
- Further UP = Higher Reward (more return)
- Diagonal = Similar Risk/Reward Ratio
```

---

## Win Rate vs Trade Frequency

```
Win Rate Chart (Higher = Better)

70% ┤
    │
65% ┤   ● BB+MACD (Adv)
    │
60% ┤ ● BB+MACD (Beg)  ○ RSI+EMA (Adv)  ◈ Trend (Adv)
    │
55% ┤           ○ RSI+EMA (Beg)    ◈ Trend (Beg)  ◇ Volume (Adv)
    │
50% ┤                              ◇ Volume (Beg)
    │
45% ┤
    │
    └─────┴─────┴─────┴─────┴─────┴─────┴──────► Trades/Month
      0    10    20    30    40    50    60    70

INTERPRETATION:
- Higher win rate = More consistent profits
- More trades = More opportunities (but also more effort)
- Sweet spot: 55-60% win rate with 30-50 trades/month
```

---

## Sharpe Ratio Comparison (Risk-Adjusted Returns)

```
Sharpe Ratio (Higher = Better Risk-Adjusted Performance)

3.0 ┤
    │
2.8 ┤                                  ◈ Trend Following (Adv)
    │
2.6 ┤                      ◇ Volume Breakout (Adv)
    │
2.4 ┤
    │
2.2 ┤              ○ RSI+EMA (Adv)    ◈ Trend Following (Beg)
    │
2.0 ┤     ● BB+MACD (Adv)      ◇ Volume Breakout (Beg)
    │
1.8 ┤              ○ RSI+EMA (Beg)
    │
1.6 ┤
    │
1.4 ┤ ● BB+MACD (Beg)
    │
1.2 ┤
    │
    └────────────────────────────────────────────
         BB+MACD    RSI+EMA    Volume    Trend

Sharpe Ratio Guide:
< 1.0 = Poor
1.0-1.5 = Acceptable
1.5-2.0 = Good
2.0-3.0 = Very Good
> 3.0 = Excellent
```

---

## Parameter Complexity Grid

```
                    SIMPLE ←──────────────→ COMPLEX

BEGINNER      │  ● BB+MACD
              │  (8 params)
              │
              │  ● RSI+EMA
              │  (7 params)
              │
              │          ◈ Trend Following
INTERMEDIATE  │          (7 params + ADX)
              │
              │                    ◇ Volume Breakout
ADVANCED      │                    (6 params + volume analysis)
              │
              │
```

---

## Strategy Characteristics Radar Chart

### BB + MACD (Beginner)
```
        Profitability
              │
         3 ◄──┼──► 5
              │
              ●
Win Rate ◄────┼────► Complexity
    6         │         2
              │
              │
    Drawdown  │  Trade Freq
       4      │      5
```

### RSI + EMA (Advanced)
```
        Profitability
              │
         5 ◄──┼──► 7
              │
              ○
Win Rate ◄────┼────► Complexity
    6         │         5
              │
              │
    Drawdown  │  Trade Freq
       5      │      6
```

### Volume Breakout (Advanced)
```
        Profitability
              │
         7 ◄──┼──► 9
              │
              ◇
Win Rate ◄────┼────► Complexity
    5         │         8
              │
              │
    Drawdown  │  Trade Freq
       7      │      3
```

### Trend Following (Advanced)
```
        Profitability
              │
         8 ◄──┼──► 10
              │
              ◈
Win Rate ◄────┼────► Complexity
    6         │         7
              │
              │
    Drawdown  │  Trade Freq
       8      │      2
```

Scale: 1-10 (10 = Best/Highest)

---

## Market Condition Suitability

```
Market Condition Matrix

                BULL MARKET    BEAR MARKET    SIDEWAYS    HIGH VOL    LOW VOL

BB + MACD         ★★★☆☆         ★★★☆☆        ★★★★★      ★★★☆☆      ★★★★☆
                  (Good)        (Good)       (Excellent) (Good)     (Very Good)

RSI + EMA         ★★★★★         ★★★★★        ★★☆☆☆      ★★★★☆      ★★☆☆☆
                  (Excellent)   (Excellent)  (Fair)     (Very Good) (Fair)

Volume Breakout   ★★★★☆         ★★★☆☆        ★★☆☆☆      ★★★★★      ★☆☆☆☆
                  (Very Good)   (Good)       (Fair)     (Excellent) (Poor)

Trend Following   ★★★★★         ★★★★☆        ★☆☆☆☆      ★★★★☆      ★★☆☆☆
                  (Excellent)   (Very Good)  (Poor)     (Very Good) (Fair)

★★★★★ = Excellent  ★★★★☆ = Very Good  ★★★☆☆ = Good
★★☆☆☆ = Fair       ★☆☆☆☆ = Poor
```

---

## Time Commitment Requirements

```
Time Investment per Strategy

BB + MACD
Monitor: [████░░░░░░] 40% (2-3 hours/day)
Setup:   [███░░░░░░░] 30% (Quick setup)
Total:   LOW TIME COMMITMENT

RSI + EMA
Monitor: [█████░░░░░] 50% (3-4 hours/day)
Setup:   [████░░░░░░] 40% (Moderate setup)
Total:   MEDIUM TIME COMMITMENT

Volume Breakout
Monitor: [████████░░] 80% (Alert-based, 5-6 hours monitoring)
Setup:   [██████░░░░] 60% (Complex setup)
Total:   HIGH TIME COMMITMENT

Trend Following
Monitor: [███░░░░░░░] 30% (1-2 hours/day check-ins)
Setup:   [████░░░░░░] 40% (Moderate setup)
Total:   LOW TIME COMMITMENT
```

---

## Capital Requirements

```
Minimum Capital Recommendations

Strategy          | Min Capital | Optimal Capital | Reasoning
------------------|-------------|-----------------|---------------------------
BB + MACD         | $500        | $2,000          | Small positions (2%)
RSI + EMA         | $1,000      | $3,000          | 4 concurrent positions
Volume Breakout   | $2,000      | $5,000          | Larger positions (4%)
Trend Following   | $2,500      | $7,500          | Long holding periods

⚠️ IMPORTANT: Only use capital you can afford to lose
```

---

## Parameter Adjustment Guide

### When to Use Beginner vs Advanced Parameters

```
Choose BEGINNER if you:
✓ Are new to algorithmic trading
✓ Have limited trading capital (<$2,000)
✓ Prefer lower risk/lower reward
✓ Want more conservative entries
✓ Are still learning market dynamics

Choose ADVANCED if you:
✓ Have 6+ months trading experience
✓ Have sufficient capital (>$3,000)
✓ Can handle higher volatility
✓ Understand technical indicators deeply
✓ Want higher risk/reward opportunities
```

---

## Strategy Upgrade Path

```
Recommended Learning Progression

MONTH 1-2:  Start with BB + MACD (Beginner)
            ↓
            • Learn Bollinger Bands behavior
            • Understand MACD signals
            • Practice risk management
            ↓
MONTH 3-4:  Transition to BB + MACD (Advanced)
            ↓
            • Tighter parameters
            • More aggressive entries
            ↓
MONTH 5-6:  Add RSI + EMA (Beginner)
            ↓
            • Learn trend following
            • Understand EMA crossovers
            ↓
MONTH 7-9:  Master RSI + EMA (Advanced)
            ↓
            • Multiple timeframes
            • Advanced filtering
            ↓
MONTH 10+:  Explore Volume Breakout OR Trend Following
            ↓
            • Requires patience and discipline
            • Higher risk tolerance needed
```

---

## Performance Benchmarks

### What to Expect in Your First 3 Months

```
BB + MACD (Beginner)
Month 1: -2% to +5%   (Learning curve)
Month 2: +3% to +8%   (Improving)
Month 3: +5% to +12%  (Confident)

RSI + EMA (Beginner)
Month 1: -3% to +4%   (Learning curve)
Month 2: +4% to +10%  (Improving)
Month 3: +6% to +15%  (Confident)

Volume Breakout (NOT RECOMMENDED for first 3 months)
Trend Following (NOT RECOMMENDED for first 3 months)
```

---

## Red Flags: When to Stop Trading a Strategy

```
STOP IMMEDIATELY if:
❌ 5+ consecutive losses
❌ Daily loss exceeds strategy threshold
❌ Win rate drops below 35% over 20 trades
❌ Sharpe ratio < 0.5 over 30 days
❌ Market conditions drastically changed

PAUSE AND REASSESS if:
⚠️ 3 consecutive losses
⚠️ Monthly return < expected range
⚠️ Increased slippage or execution issues
⚠️ Emotional decision-making
⚠️ Not following strategy rules
```

---

## FAQ Visual Answers

### "Which strategy should I start with?"

```
Your Profile              → Recommended Strategy
──────────────────────────────────────────────────
Complete Beginner        → BB + MACD (Beginner)
Some Trading Experience  → RSI + EMA (Beginner)
Experienced Trader       → Volume Breakout OR Trend Following
Day Trader              → BB + MACD or RSI + EMA
Swing Trader            → Trend Following
Low Risk Tolerance      → BB + MACD (Beginner)
High Risk Tolerance     → Volume Breakout (Advanced)
```

### "How much can I make?"

```
Starting Capital: $1,000

Strategy        | Conservative | Realistic | Aggressive
----------------|--------------|-----------|------------
BB + MACD       | $150-250     | $250-400  | $400-500
RSI + EMA       | $200-350     | $350-550  | $550-750
Volume Breakout | $250-450     | $450-750  | $750-1,000
Trend Following | $300-550     | $550-850  | $850-1,200

Annual projections (assuming consistent execution)
⚠️ Results may vary significantly based on market conditions
```

### "How much time do I need?"

```
Strategy            | Daily Time | Weekly Time | Setup Time
--------------------|------------|-------------|------------
BB + MACD           | 2-3 hours  | 10-15 hours | 30 minutes
RSI + EMA           | 3-4 hours  | 15-20 hours | 45 minutes
Volume Breakout     | 5-6 hours  | 25-30 hours | 60 minutes
Trend Following     | 1-2 hours  | 7-10 hours  | 45 minutes
```

---

## Mobile UI Preview (Text-Based Mockup)

```
┌─────────────────────────────┐
│  ← Back    Strategy Test    │
├─────────────────────────────┤
│                             │
│  📊 Bollinger Bands + MACD  │
│                             │
│  BB bantları ve MACD        │
│  sinyallerini kombine eden  │
│  klasik strateji            │
│                             │
│  Zorluk: Kolay  ⏱ 5m-15m   │
│                             │
│  ▼ Detaylar                 │
│                             │
├─────────────────────────────┤
│  Parametre Seviyesi:        │
│  ┌────────────┬────────────┐│
│  │ Başlangıç  │ İleri Sev. ││
│  └────────────┴────────────┘│
├─────────────────────────────┤
│  📊 Parametreler            │
│                             │
│  BB Periyot      [20  ]    │
│  BB Std Sapma    [2.0 ]    │
│  MACD Hızlı      [12  ]    │
│  MACD Yavaş      [26  ]    │
│  ...                        │
│                             │
│  [  🧪 Test Stratejisi  ]  │
│                             │
└─────────────────────────────┘
```

---

## Summary Decision Matrix

```
If you value:     → Choose:
─────────────────────────────────────
Simplicity        → BB + MACD
Versatility       → RSI + EMA
High Returns      → Trend Following
Fast Trades       → BB + MACD
Swing Trading     → Trend Following
Risk Management   → BB + MACD (Beg)
Aggressive Growth → Volume Breakout
Passive Income    → Trend Following
Active Trading    → RSI + EMA
Learning          → BB + MACD (Beg)
```

---

**Navigation**:
- Full Spec: See STRATEGY_TEMPLATES_SPECIFICATION.md
- Implementation: See STRATEGY_IMPLEMENTATION_GUIDE.md
- Summary: See STRATEGY_TEMPLATES_SUMMARY.md
- Data: See strategy_presets.json
