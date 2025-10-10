# Quick Start Implementation Guide

**For Developers: Get Strategy Templates Working in 30 Minutes**

---

## TL;DR

Add 4 optimized strategy templates to MyTrader mobile app so each template shows different parameter values instead of the same defaults.

**Time Estimate**: 2-3 hours for core functionality
**Complexity**: Medium
**Files to Touch**: 5 files (2 new, 3 modified)

---

## Step 1: Add Type Definitions (5 minutes)

**File**: `/frontend/mobile/src/types/index.ts`

Add to existing file:

```typescript
// Add these types to existing exports
export type StrategyId = 'bb_macd' | 'rsi_ema' | 'volume_breakout' | 'trend_following';
export type ParameterMode = 'beginner' | 'advanced';

export interface StrategyParameters {
  bb_period: string;
  bb_std: string;
  macd_fast: string;
  macd_slow: string;
  macd_signal: string;
  rsi_period: string;
  rsi_overbought: string;
  rsi_oversold: string;
}

// Update existing RootStackParamList
export type RootStackParamList = {
  // ... existing routes ...
  StrategyTest: {
    symbol: string;
    displayName: string;
    strategyId?: StrategyId;  // ADD THIS
    strategyName?: string;     // ADD THIS
  };
  // ... existing routes ...
};
```

**Commit**: `git add . && git commit -m "feat: add strategy template types"`

---

## Step 2: Create Strategy Config (15 minutes)

**File**: `/frontend/mobile/src/config/strategyPresets.ts` (NEW FILE)

Create this file and paste the minimal config:

```typescript
import { StrategyId, ParameterMode, StrategyParameters } from '../types';

interface StrategyPreset {
  parameters: StrategyParameters;
}

interface StrategyConfig {
  beginner: StrategyPreset;
  advanced: StrategyPreset;
}

export const STRATEGY_CONFIGS: Record<StrategyId, StrategyConfig> = {
  bb_macd: {
    beginner: {
      parameters: {
        bb_period: '20',
        bb_std: '2.0',
        macd_fast: '12',
        macd_slow: '26',
        macd_signal: '9',
        rsi_period: '14',
        rsi_overbought: '70',
        rsi_oversold: '30',
      },
    },
    advanced: {
      parameters: {
        bb_period: '20',
        bb_std: '2.5',
        macd_fast: '8',
        macd_slow: '21',
        macd_signal: '5',
        rsi_period: '14',
        rsi_overbought: '75',
        rsi_oversold: '25',
      },
    },
  },

  rsi_ema: {
    beginner: {
      parameters: {
        bb_period: '20',  // Volume SMA
        bb_std: '1.2',    // Volume multiplier
        macd_fast: '9',   // EMA fast
        macd_slow: '21',  // EMA slow
        macd_signal: '14', // ATR period
        rsi_period: '14',
        rsi_overbought: '70',
        rsi_oversold: '30',
      },
    },
    advanced: {
      parameters: {
        bb_period: '20',
        bb_std: '1.5',
        macd_fast: '8',
        macd_slow: '21',
        macd_signal: '14',
        rsi_period: '13',
        rsi_overbought: '75',
        rsi_oversold: '25',
      },
    },
  },

  volume_breakout: {
    beginner: {
      parameters: {
        bb_period: '20',  // Breakout lookback
        bb_std: '2.0',    // ATR multiplier
        macd_fast: '20',  // Volume SMA
        macd_slow: '20',  // Volume multiplier (x10)
        macd_signal: '14', // ATR period
        rsi_period: '14',
        rsi_overbought: '70',
        rsi_oversold: '50', // RSI min filter
      },
    },
    advanced: {
      parameters: {
        bb_period: '30',
        bb_std: '2.0',
        macd_fast: '20',
        macd_slow: '25', // 2.5x
        macd_signal: '14',
        rsi_period: '14',
        rsi_overbought: '70',
        rsi_oversold: '55',
      },
    },
  },

  trend_following: {
    beginner: {
      parameters: {
        bb_period: '21',  // EMA fast
        bb_std: '5.0',    // EMA medium (50/10)
        macd_fast: '25',  // ADX threshold
        macd_slow: '30',  // SuperTrend mult (x10)
        macd_signal: '14', // ATR period
        rsi_period: '14',
        rsi_overbought: '70',
        rsi_oversold: '30',
      },
    },
    advanced: {
      parameters: {
        bb_period: '21',
        bb_std: '5.0',
        macd_fast: '30',
        macd_slow: '25',
        macd_signal: '14',
        rsi_period: '14',
        rsi_overbought: '75',
        rsi_oversold: '25',
      },
    },
  },
};

export const getStrategyPreset = (
  strategyId: StrategyId,
  mode: ParameterMode
): StrategyParameters => {
  return STRATEGY_CONFIGS[strategyId][mode].parameters;
};
```

**Commit**: `git add . && git commit -m "feat: add strategy preset configurations"`

---

## Step 3: Update StrategiesScreen (5 minutes)

**File**: `/frontend/mobile/src/screens/StrategiesScreen.tsx`

Find the `handleStrategySubmit` function (around line 255) and modify:

```typescript
// Add import at top
import { StrategyId } from '../types';

// Modify the function
const handleStrategySubmit = () => {
  if (!strategyName.trim()) {
    Alert.alert('Hata', 'Lütfen strateji adı girin.');
    return;
  }

  const selectedAssetData = assets.find(a => a.symbol === selectedAsset);

  // ADD THESE TWO LINES to pass strategy info
  navigation.navigate('StrategyTest', {
    symbol: selectedAsset,
    displayName: selectedAssetData?.name || 'Kripto Para',
    strategyId: selectedTemplate?.id as StrategyId, // ← ADD THIS
    strategyName: selectedTemplate?.name,            // ← ADD THIS
  });

  setShowCreateModal(false);
  resetModal();
};
```

**Commit**: `git add . && git commit -m "feat: pass strategy template ID to test screen"`

---

## Step 4: Update StrategyTestScreen - Part 1 (10 minutes)

**File**: `/frontend/mobile/src/screens/StrategyTestScreen.tsx`

Add imports at top:

```typescript
// Add these imports
import { StrategyId, ParameterMode } from '../types';
import { getStrategyPreset } from '../config/strategyPresets';
```

Modify component state (around line 29-44):

```typescript
// Extract strategyId from route params
const { symbol, displayName, strategyId, strategyName } = route.params;

// Add new state variable for parameter mode
const [parameterMode, setParameterMode] = useState<ParameterMode>('beginner');

// Existing parameters state stays the same
const [parameters, setParameters] = useState({
  bb_period: '20',
  bb_std: '2.0',
  macd_fast: '12',
  macd_slow: '26',
  macd_signal: '9',
  rsi_period: '14',
  rsi_overbought: '70',
  rsi_oversold: '30',
});
```

---

## Step 5: Update StrategyTestScreen - Part 2 (10 minutes)

Add effect to load strategy parameters (after line 95):

```typescript
// Add this useEffect to load strategy-specific parameters
useEffect(() => {
  if (strategyId) {
    const preset = getStrategyPreset(strategyId, parameterMode);
    setParameters(preset);
    console.log(`Loaded ${strategyId} strategy (${parameterMode} mode)`);
  }
}, [strategyId, parameterMode]);
```

Add toggle component before the return statement:

```typescript
const renderParameterModeToggle = () => {
  if (!strategyId) return null;

  return (
    <View style={styles.modeToggleContainer}>
      <Text style={styles.modeToggleLabel}>Parametre Seviyesi:</Text>
      <View style={styles.modeToggle}>
        <TouchableOpacity
          style={[
            styles.modeButton,
            parameterMode === 'beginner' && styles.modeButtonActive
          ]}
          onPress={() => setParameterMode('beginner')}
        >
          <Text style={[
            styles.modeButtonText,
            parameterMode === 'beginner' && styles.modeButtonTextActive
          ]}>
            Başlangıç
          </Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[
            styles.modeButton,
            parameterMode === 'advanced' && styles.modeButtonActive
          ]}
          onPress={() => setParameterMode('advanced')}
        >
          <Text style={[
            styles.modeButtonText,
            parameterMode === 'advanced' && styles.modeButtonTextActive
          ]}>
            İleri Seviye
          </Text>
        </TouchableOpacity>
      </View>
    </View>
  );
};
```

---

## Step 6: Update StrategyTestScreen - Part 3 (10 minutes)

In the render method, add the toggle after the asset card (around line 264):

```typescript
{/* Asset Info */}
<View style={styles.assetCard}>
  {/* ... existing asset card content ... */}
</View>

{/* ADD THIS LINE */}
{renderParameterModeToggle()}

{/* Compact Parameters */}
<View style={styles.section}>
  {/* ... existing parameters section ... */}
</View>
```

Add styles at the bottom of the StyleSheet (around line 736):

```typescript
const styles = StyleSheet.create({
  // ... existing styles ...

  // ADD THESE STYLES
  modeToggleContainer: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 15,
    padding: 15,
    marginBottom: 15,
  },
  modeToggleLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
    marginBottom: 10,
  },
  modeToggle: {
    flexDirection: 'row',
    backgroundColor: '#f8fafc',
    borderRadius: 10,
    padding: 4,
  },
  modeButton: {
    flex: 1,
    paddingVertical: 10,
    paddingHorizontal: 15,
    borderRadius: 8,
    alignItems: 'center',
  },
  modeButtonActive: {
    backgroundColor: '#667eea',
  },
  modeButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#666',
  },
  modeButtonTextActive: {
    color: 'white',
  },
});
```

**Commit**: `git add . && git commit -m "feat: implement strategy parameter loading and mode toggle"`

---

## Step 7: Test Locally (10 minutes)

```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader/frontend/mobile

# Start Metro bundler
npm start

# In another terminal, run iOS simulator
npx react-native run-ios

# OR Android
npx react-native run-android
```

**Test Checklist**:
1. [ ] Open app
2. [ ] Navigate to Strategies screen
3. [ ] Tap "Bollinger Bands + MACD" → "Bu Şablonu Kullan"
4. [ ] Select Bitcoin
5. [ ] Tap "Stratejiyi Test Et"
6. [ ] Verify parameters show: BB Period=20, MACD Fast=12
7. [ ] Toggle to "İleri Seviye"
8. [ ] Verify parameters change: BB Period=20, MACD Fast=8
9. [ ] Go back, select "RSI + EMA"
10. [ ] Verify different parameters appear
11. [ ] Repeat for other strategies

---

## Step 8: Verify Each Strategy (5 minutes)

Run this quick test script in the app:

```typescript
// Quick console test (add temporarily to StrategyTestScreen)
useEffect(() => {
  console.log('=== STRATEGY PARAMETER TEST ===');
  ['bb_macd', 'rsi_ema', 'volume_breakout', 'trend_following'].forEach(id => {
    const beginnerParams = getStrategyPreset(id as StrategyId, 'beginner');
    const advancedParams = getStrategyPreset(id as StrategyId, 'advanced');
    console.log(`\n${id}:`);
    console.log('  Beginner MACD Fast:', beginnerParams.macd_fast);
    console.log('  Advanced MACD Fast:', advancedParams.macd_fast);
  });
}, []);
```

Expected output:
```
bb_macd:
  Beginner MACD Fast: 12
  Advanced MACD Fast: 8

rsi_ema:
  Beginner MACD Fast: 9
  Advanced MACD Fast: 8

volume_breakout:
  Beginner MACD Fast: 20
  Advanced MACD Fast: 20

trend_following:
  Beginner MACD Fast: 25
  Advanced MACD Fast: 30
```

---

## Troubleshooting

### Issue: "Cannot find module '../config/strategyPresets'"

**Solution**: Make sure you created the file at correct path:
```bash
ls -la frontend/mobile/src/config/strategyPresets.ts
```

If missing:
```bash
mkdir -p frontend/mobile/src/config
# Create the file from Step 2
```

---

### Issue: Parameters not updating when switching strategies

**Solution**: Check that `strategyId` is being passed correctly:

```typescript
// Add this debug log in StrategyTestScreen
console.log('Route params:', route.params);
// Should show: { symbol: 'BTCUSDT', displayName: 'Bitcoin', strategyId: 'bb_macd', ... }
```

If `strategyId` is undefined, check `StrategiesScreen.tsx` navigation call.

---

### Issue: Toggle not appearing

**Solution**: Check that `strategyId` exists:

```typescript
// In renderParameterModeToggle
if (!strategyId) {
  console.log('No strategyId - toggle hidden');
  return null;
}
```

If strategyId is missing, the toggle won't show (expected behavior for manual strategy creation).

---

### Issue: TypeScript errors

**Solution**: Rebuild:
```bash
cd frontend/mobile
rm -rf node_modules
npm install
npm start -- --reset-cache
```

---

## Validation Checklist

After implementation, verify:

- [ ] **BB + MACD Beginner**: Shows BB Period=20, MACD Fast=12
- [ ] **BB + MACD Advanced**: Shows BB Period=20, MACD Fast=8
- [ ] **RSI + EMA Beginner**: Shows different parameters (MACD Fast=9)
- [ ] **RSI + EMA Advanced**: Shows MACD Fast=8
- [ ] **Volume Breakout**: Shows unique parameters
- [ ] **Trend Following**: Shows unique parameters
- [ ] Toggle switches between beginner/advanced smoothly
- [ ] Manual strategy creation still works (no strategyId)
- [ ] No console errors
- [ ] Parameters are editable by user

---

## Commit and Push

```bash
# Final commit
git add .
git commit -m "feat(strategies): implement optimized strategy templates

- Add strategy preset configurations for all 4 templates
- Implement beginner/advanced parameter modes
- Add parameter mode toggle UI component
- Pass strategy metadata from StrategiesScreen to StrategyTestScreen

Each strategy now loads optimized parameters specific to its
trading approach and timeframe. Users can toggle between
beginner (conservative) and advanced (aggressive) configurations."

# Push to feature branch
git push origin feature/strategy-templates
```

---

## Next Steps (Optional Enhancements)

After core functionality works, add these in order of priority:

1. **Risk Warning Modal** (1 hour)
   - See STRATEGY_IMPLEMENTATION_GUIDE.md Section 7
   - Shows warnings before first backtest

2. **Strategy Info Card** (2 hours)
   - Expandable card showing strategy details
   - Expected performance metrics
   - Best/worst market conditions

3. **Parameter Labels** (1 hour)
   - Dynamic labels per strategy
   - E.g., "EMA Fast" for RSI strategy instead of "MACD Fast"

4. **Real Backtest Integration** (TBD)
   - Replace mock backtest with real engine
   - Connect to backend API

---

## Production Deployment Checklist

Before deploying to production:

- [ ] All tests pass
- [ ] Code review completed
- [ ] QA testing on iOS
- [ ] QA testing on Android
- [ ] Performance testing (no lag)
- [ ] Accessibility testing
- [ ] Analytics events added
- [ ] Error tracking configured
- [ ] Documentation updated
- [ ] Changelog entry added
- [ ] Beta testing (10-20 users)
- [ ] Rollout plan prepared (gradual 10% → 50% → 100%)

---

## Success Metrics to Track

After deployment, monitor:

1. **Adoption**: % of users selecting strategy templates vs manual
2. **Engagement**: Average time on strategy screen
3. **Conversion**: % of users who backtest after viewing template
4. **Mode Usage**: Beginner vs Advanced selection ratio
5. **Performance**: Strategy win rates by template
6. **Support**: Reduction in "how to configure" tickets

Target within 30 days:
- 70%+ template selection rate
- 2+ min average engagement
- 60%+ backtest conversion
- <5 support tickets/week

---

## Resources

**Full Documentation**:
- `/STRATEGY_TEMPLATES_SPECIFICATION.md` - Complete quant specs
- `/STRATEGY_IMPLEMENTATION_GUIDE.md` - Detailed implementation
- `/STRATEGY_TEMPLATES_SUMMARY.md` - Executive summary
- `/STRATEGY_VISUAL_COMPARISON.md` - Visual decision guides
- `/strategy_presets.json` - Machine-readable config

**Support**:
- GitHub Issues: Tag with `strategy-templates`
- Slack: #mobile-dev channel
- Email: dev-support@mytrader.com

---

**Time Investment Summary**:
- Core Implementation: 2-3 hours
- Testing: 30-45 minutes
- Documentation: 15 minutes
- **Total**: ~3-4 hours for working feature

**Complexity**: Medium (mostly config-driven, minimal logic)

**Risk**: Low (isolated changes, backward compatible)

---

You're done! The app now shows different optimized parameters for each strategy template. Users can toggle between beginner and advanced modes to suit their risk tolerance.
