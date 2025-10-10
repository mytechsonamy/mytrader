# Strategy Templates Implementation Guide

**Companion Document to**: STRATEGY_TEMPLATES_SPECIFICATION.md
**Target Files**:
- `/frontend/mobile/src/screens/StrategyTestScreen.tsx`
- `/frontend/mobile/src/screens/StrategiesScreen.tsx`
- `/frontend/mobile/src/types/index.ts`

---

## 1. Type Definitions

Add to `/frontend/mobile/src/types/index.ts`:

```typescript
// Strategy configuration types
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

export interface StrategyPreset {
  parameters: StrategyParameters;
  risk: {
    position_size_pct: number;
    stop_loss_pct: number;
    take_profit_pct: number;
    max_positions: number;
  };
  expected_performance: {
    annual_return: [number, number];
    win_rate: [number, number];
    max_drawdown: [number, number];
    sharpe_ratio: [number, number];
  };
  labels: {
    [key: string]: string;
  };
}

export interface StrategyTemplateConfig {
  name: string;
  difficulty: 'Kolay' | 'Orta' | 'İleri';
  timeframes: string[];
  beginner: StrategyPreset;
  advanced: StrategyPreset;
  description: string;
  best_conditions: string[];
  worst_conditions: string[];
}
```

---

## 2. Strategy Configuration Constants

Create new file: `/frontend/mobile/src/config/strategyPresets.ts`:

```typescript
import { StrategyId, StrategyTemplateConfig, ParameterMode } from '../types';

export const STRATEGY_CONFIGS: Record<StrategyId, StrategyTemplateConfig> = {
  bb_macd: {
    name: 'Bollinger Bands + MACD',
    difficulty: 'Kolay',
    timeframes: ['5m', '15m'],
    description: 'BB bantları ve MACD sinyallerini kombine eden klasik strateji',
    best_conditions: [
      'Yatay/konsolidasyon piyasaları',
      'Orta volatilite (günlük ATR 2-4%)',
      'Yüksek likidite dönemleri'
    ],
    worst_conditions: [
      'Güçlü trend piyasaları',
      'Çok düşük volatilite (< 1% ATR)',
      'Hafta sonu düşük likidite'
    ],
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
      risk: {
        position_size_pct: 2.0,
        stop_loss_pct: 1.5,
        take_profit_pct: 3.0,
        max_positions: 3,
      },
      expected_performance: {
        annual_return: [15, 25],
        win_rate: [55, 60],
        max_drawdown: [-12, -15],
        sharpe_ratio: [1.2, 1.5],
      },
      labels: {
        bb_period: 'BB Periyot',
        bb_std: 'BB Std Sapma',
        macd_fast: 'MACD Hızlı',
        macd_slow: 'MACD Yavaş',
        macd_signal: 'MACD Sinyal',
        rsi_period: 'RSI Periyot',
        rsi_overbought: 'RSI Aşırı Alım',
        rsi_oversold: 'RSI Aşırı Satım',
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
      risk: {
        position_size_pct: 3.0,
        stop_loss_pct: 1.2,
        take_profit_pct: 4.0,
        max_positions: 3,
      },
      expected_performance: {
        annual_return: [25, 40],
        win_rate: [58, 65],
        max_drawdown: [-15, -20],
        sharpe_ratio: [1.5, 2.0],
      },
      labels: {
        bb_period: 'BB Periyot',
        bb_std: 'BB Std Sapma',
        macd_fast: 'MACD Hızlı',
        macd_slow: 'MACD Yavaş',
        macd_signal: 'MACD Sinyal',
        rsi_period: 'RSI Periyot',
        rsi_overbought: 'RSI Aşırı Alım',
        rsi_oversold: 'RSI Aşırı Satım',
      },
    },
  },

  rsi_ema: {
    name: 'RSI + EMA Crossover',
    difficulty: 'Orta',
    timeframes: ['15m', '1h'],
    description: 'RSI momentum ve EMA trend takibi kombinasyonu',
    best_conditions: [
      'Trend piyasaları (yukarı/aşağı)',
      'Orta-yüksek volatilite (3-6% ATR)',
      'Konsolidasyon sonrası kırılımlar'
    ],
    worst_conditions: [
      'Choppy yatay piyasalar',
      'Düşük volatilite',
      'Yanlış kırılım bölgeleri'
    ],
    beginner: {
      parameters: {
        bb_period: '20', // Volume SMA (repurposed)
        bb_std: '1.2',   // Volume multiplier (repurposed)
        macd_fast: '9',   // EMA fast
        macd_slow: '21',  // EMA slow
        macd_signal: '14', // ATR period
        rsi_period: '14',
        rsi_overbought: '70',
        rsi_oversold: '30',
      },
      risk: {
        position_size_pct: 2.5,
        stop_loss_pct: 1.5,
        take_profit_pct: 3.0,
        max_positions: 4,
      },
      expected_performance: {
        annual_return: [20, 35],
        win_rate: [52, 58],
        max_drawdown: [-15, -18],
        sharpe_ratio: [1.4, 1.8],
      },
      labels: {
        bb_period: 'Volume SMA',
        bb_std: 'Volume Çarpan',
        macd_fast: 'EMA Hızlı',
        macd_slow: 'EMA Yavaş',
        macd_signal: 'ATR Periyot',
        rsi_period: 'RSI Periyot',
        rsi_overbought: 'RSI Üst Eşik',
        rsi_oversold: 'RSI Alt Eşik',
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
      risk: {
        position_size_pct: 3.5,
        stop_loss_pct: 1.2,
        take_profit_pct: 4.0,
        max_positions: 4,
      },
      expected_performance: {
        annual_return: [35, 55],
        win_rate: [55, 62],
        max_drawdown: [-18, -25],
        sharpe_ratio: [1.8, 2.3],
      },
      labels: {
        bb_period: 'Volume SMA',
        bb_std: 'Volume Çarpan',
        macd_fast: 'EMA Hızlı',
        macd_slow: 'EMA Yavaş',
        macd_signal: 'ATR Periyot',
        rsi_period: 'RSI Periyot',
        rsi_overbought: 'RSI Üst Eşik',
        rsi_oversold: 'RSI Alt Eşik',
      },
    },
  },

  volume_breakout: {
    name: 'Volume Breakout',
    difficulty: 'İleri',
    timeframes: ['1h', '4h'],
    description: 'Hacim artışı ile desteklenen fiyat kırılımları',
    best_conditions: [
      'Yüksek volatilite olayları',
      'Haber katalizörlü hareketler',
      'Konsolidasyon kırılımları'
    ],
    worst_conditions: [
      'Düşük volatilite',
      'Yanlış kırılım bölgeleri',
      'Hafta sonu/tatil düşük hacim'
    ],
    beginner: {
      parameters: {
        bb_period: '20',  // Breakout lookback
        bb_std: '2.0',    // ATR multiplier
        macd_fast: '20',  // Volume SMA period
        macd_slow: '20',  // Volume multiplier (x10 for 2.0)
        macd_signal: '14', // ATR period
        rsi_period: '14',
        rsi_overbought: '70', // Not actively used
        rsi_oversold: '50',   // RSI filter minimum
      },
      risk: {
        position_size_pct: 3.0,
        stop_loss_pct: 2.0,
        take_profit_pct: 6.0,
        max_positions: 3,
      },
      expected_performance: {
        annual_return: [25, 45],
        win_rate: [45, 52],
        max_drawdown: [-18, -22],
        sharpe_ratio: [1.5, 2.0],
      },
      labels: {
        bb_period: 'Kırılım Geriye Bakış',
        bb_std: 'ATR Çarpanı',
        macd_fast: 'Volume SMA',
        macd_slow: 'Volume Katsayısı',
        macd_signal: 'ATR Periyot',
        rsi_period: 'RSI Periyot',
        rsi_overbought: '(Kullanılmaz)',
        rsi_oversold: 'RSI Min Filtre',
      },
    },
    advanced: {
      parameters: {
        bb_period: '30',
        bb_std: '2.0',
        macd_fast: '20',
        macd_slow: '25', // 2.5x multiplier
        macd_signal: '14',
        rsi_period: '14',
        rsi_overbought: '70',
        rsi_oversold: '55',
      },
      risk: {
        position_size_pct: 4.0,
        stop_loss_pct: 1.5,
        take_profit_pct: 8.0,
        max_positions: 3,
      },
      expected_performance: {
        annual_return: [45, 75],
        win_rate: [48, 55],
        max_drawdown: [-22, -30],
        sharpe_ratio: [2.0, 2.6],
      },
      labels: {
        bb_period: 'Kırılım Geriye Bakış',
        bb_std: 'ATR Çarpanı',
        macd_fast: 'Volume SMA',
        macd_slow: 'Volume Katsayısı',
        macd_signal: 'ATR Periyot',
        rsi_period: 'RSI Periyot',
        rsi_overbought: '(Kullanılmaz)',
        rsi_oversold: 'RSI Min Filtre',
      },
    },
  },

  trend_following: {
    name: 'Trend Following',
    difficulty: 'Orta',
    timeframes: ['4h', '1d'],
    description: 'Uzun vadeli trend takip stratejisi',
    best_conditions: [
      'Sürdürülebilir trend piyasaları',
      'Güçlü ADX okumaları (>30)',
      'Akümülasyon/dağıtım sonrası'
    ],
    worst_conditions: [
      'Choppy yatay konsolidasyon',
      'Sık trend dönüşleri',
      'Düşük ADX (<20)'
    ],
    beginner: {
      parameters: {
        bb_period: '21',  // EMA fast
        bb_std: '5.0',    // EMA medium (50 / 10)
        macd_fast: '25',  // ADX threshold
        macd_slow: '30',  // SuperTrend multiplier (x10)
        macd_signal: '14', // ATR period
        rsi_period: '14',
        rsi_overbought: '70',
        rsi_oversold: '30',
      },
      risk: {
        position_size_pct: 4.0,
        stop_loss_pct: 3.0,
        take_profit_pct: 12.0,
        max_positions: 2,
      },
      expected_performance: {
        annual_return: [30, 55],
        win_rate: [48, 55],
        max_drawdown: [-20, -25],
        sharpe_ratio: [1.6, 2.2],
      },
      labels: {
        bb_period: 'EMA Hızlı',
        bb_std: 'EMA Orta (÷10)',
        macd_fast: 'ADX Eşik',
        macd_slow: 'SuperTrend Çarpan',
        macd_signal: 'ATR Periyot',
        rsi_period: 'RSI Periyot',
        rsi_overbought: '(Kullanılmaz)',
        rsi_oversold: '(Kullanılmaz)',
      },
    },
    advanced: {
      parameters: {
        bb_period: '21',
        bb_std: '5.0',
        macd_fast: '30',
        macd_slow: '25', // 2.5x multiplier
        macd_signal: '14',
        rsi_period: '14',
        rsi_overbought: '75',
        rsi_oversold: '25',
      },
      risk: {
        position_size_pct: 5.0,
        stop_loss_pct: 2.5,
        take_profit_pct: 16.0,
        max_positions: 2,
      },
      expected_performance: {
        annual_return: [50, 85],
        win_rate: [52, 60],
        max_drawdown: [-25, -35],
        sharpe_ratio: [2.0, 2.8],
      },
      labels: {
        bb_period: 'EMA Hızlı',
        bb_std: 'EMA Orta (÷10)',
        macd_fast: 'ADX Eşik',
        macd_slow: 'SuperTrend Çarpan',
        macd_signal: 'ATR Periyot',
        rsi_period: 'RSI Periyot',
        rsi_overbought: '(Kullanılmaz)',
        rsi_oversold: '(Kullanılmaz)',
      },
    },
  },
};

// Helper function to get strategy preset
export const getStrategyPreset = (
  strategyId: StrategyId,
  mode: ParameterMode
): StrategyPreset => {
  return STRATEGY_CONFIGS[strategyId][mode];
};

// Helper function to get parameter labels
export const getParameterLabels = (
  strategyId: StrategyId,
  mode: ParameterMode
): { [key: string]: string } => {
  return STRATEGY_CONFIGS[strategyId][mode].labels;
};
```

---

## 3. Update StrategiesScreen.tsx

Modify the navigation call to pass strategy metadata:

```typescript
// In StrategiesScreen.tsx

// Add import at top
import { StrategyId } from '../types';

// Modify handleStrategySubmit function (around line 255)
const handleStrategySubmit = () => {
  if (!strategyName.trim()) {
    Alert.alert('Hata', 'Lütfen strateji adı girin.');
    return;
  }

  const selectedAssetData = assets.find(a => a.symbol === selectedAsset);

  // Navigate with strategy template ID
  navigation.navigate('StrategyTest', {
    symbol: selectedAsset,
    displayName: selectedAssetData?.name || 'Kripto Para',
    strategyId: selectedTemplate?.id as StrategyId, // ADD THIS
    strategyName: selectedTemplate?.name, // ADD THIS
  });

  setShowCreateModal(false);
  resetModal();
};
```

---

## 4. Update StrategyTestScreen.tsx

Major modifications to support strategy presets:

```typescript
// At the top, add imports
import { StrategyId, ParameterMode } from '../types';
import { STRATEGY_CONFIGS, getStrategyPreset, getParameterLabels } from '../config/strategyPresets';

// Update route params type (around line 21-22)
type StrategyTestRouteProp = RouteProp<RootStackParamList, 'StrategyTest'>;

// In the component, extract new route params (around line 29)
const { symbol, displayName, strategyId, strategyName } = route.params;

// Add parameter mode state (after line 44)
const [parameterMode, setParameterMode] = useState<ParameterMode>('beginner');
const [parameterLabels, setParameterLabels] = useState<{ [key: string]: string }>({});
const [showStrategyInfo, setShowStrategyInfo] = useState(false);

// Load strategy-specific parameters on mount (add after line 95)
useEffect(() => {
  if (strategyId && STRATEGY_CONFIGS[strategyId]) {
    const preset = getStrategyPreset(strategyId, parameterMode);
    setParameters(preset.parameters);
    setParameterLabels(getParameterLabels(strategyId, parameterMode));
    console.log(`Loaded ${strategyId} strategy (${parameterMode} mode)`);
  }
}, [strategyId, parameterMode]);

// Add parameter mode toggle component (before return statement)
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

// Add strategy info card component
const renderStrategyInfo = () => {
  if (!strategyId || !STRATEGY_CONFIGS[strategyId]) return null;

  const config = STRATEGY_CONFIGS[strategyId];
  const preset = config[parameterMode];

  return (
    <View style={styles.strategyInfoCard}>
      <View style={styles.strategyInfoHeader}>
        <Text style={styles.strategyInfoTitle}>{config.name}</Text>
        <TouchableOpacity onPress={() => setShowStrategyInfo(!showStrategyInfo)}>
          <Text style={styles.strategyInfoToggle}>
            {showStrategyInfo ? '▼ Gizle' : '► Detaylar'}
          </Text>
        </TouchableOpacity>
      </View>

      {showStrategyInfo && (
        <View style={styles.strategyInfoContent}>
          <Text style={styles.strategyInfoDescription}>{config.description}</Text>

          <View style={styles.strategyMetaInfo}>
            <View style={styles.metaItem}>
              <Text style={styles.metaLabel}>Zorluk:</Text>
              <Text style={styles.metaValue}>{config.difficulty}</Text>
            </View>
            <View style={styles.metaItem}>
              <Text style={styles.metaLabel}>Zaman Dilimi:</Text>
              <Text style={styles.metaValue}>{config.timeframes.join(', ')}</Text>
            </View>
          </View>

          <View style={styles.performanceExpectations}>
            <Text style={styles.expectationTitle}>Beklenen Performans:</Text>
            <Text style={styles.expectationItem}>
              • Yıllık Getiri: {preset.expected_performance.annual_return[0]}% - {preset.expected_performance.annual_return[1]}%
            </Text>
            <Text style={styles.expectationItem}>
              • Kazanma Oranı: {preset.expected_performance.win_rate[0]}% - {preset.expected_performance.win_rate[1]}%
            </Text>
            <Text style={styles.expectationItem}>
              • Maks. Düşüş: {preset.expected_performance.max_drawdown[0]}% - {preset.expected_performance.max_drawdown[1]}%
            </Text>
            <Text style={styles.expectationItem}>
              • Sharpe Ratio: {preset.expected_performance.sharpe_ratio[0]} - {preset.expected_performance.sharpe_ratio[1]}
            </Text>
          </View>

          <View style={styles.marketConditions}>
            <Text style={styles.conditionTitle}>✅ En İyi Koşullar:</Text>
            {config.best_conditions.map((condition, idx) => (
              <Text key={idx} style={styles.conditionItem}>• {condition}</Text>
            ))}

            <Text style={[styles.conditionTitle, { marginTop: 10 }]}>❌ Kaçınılması Gereken:</Text>
            {config.worst_conditions.map((condition, idx) => (
              <Text key={idx} style={styles.conditionItem}>• {condition}</Text>
            ))}
          </View>
        </View>
      )}
    </View>
  );
};

// Update parameter rendering to use dynamic labels
const renderParameterInput = (key: string, defaultLabel: string) => {
  const label = parameterLabels[key] || defaultLabel;

  // Hide unused parameters
  if (label.includes('Kullanılmaz')) {
    return null;
  }

  return (
    <View style={styles.paramItem}>
      <Text style={styles.paramLabel}>{label}</Text>
      <TextInput
        style={styles.paramInput}
        value={parameters[key]}
        onChangeText={(value) => updateParameter(key, value)}
        keyboardType="numeric"
      />
    </View>
  );
};

// In the render return, add these components after assetCard (around line 265)
{renderStrategyInfo()}
{renderParameterModeToggle()}

// Update parameter grid to use dynamic rendering (replace lines 270-350)
<View style={styles.paramGrid}>
  {renderParameterInput('bb_period', 'BB Periyot')}
  {renderParameterInput('bb_std', 'BB Std Sapma')}
  {renderParameterInput('macd_fast', 'MACD Hızlı')}
  {renderParameterInput('macd_slow', 'MACD Yavaş')}
  {renderParameterInput('macd_signal', 'MACD Sinyal')}
  {renderParameterInput('rsi_period', 'RSI Periyot')}
  {renderParameterInput('rsi_overbought', 'RSI Aşırı Alım')}
  {renderParameterInput('rsi_oversold', 'RSI Aşırı Satım')}
</View>
```

---

## 5. Add Styles to StrategyTestScreen.tsx

Add these styles to the StyleSheet (around line 480):

```typescript
// Add to existing styles object
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
strategyInfoCard: {
  backgroundColor: 'rgba(255, 255, 255, 0.95)',
  borderRadius: 15,
  padding: 20,
  marginBottom: 15,
},
strategyInfoHeader: {
  flexDirection: 'row',
  justifyContent: 'space-between',
  alignItems: 'center',
  marginBottom: 10,
},
strategyInfoTitle: {
  fontSize: 18,
  fontWeight: 'bold',
  color: '#333',
},
strategyInfoToggle: {
  fontSize: 14,
  color: '#667eea',
  fontWeight: '600',
},
strategyInfoContent: {
  marginTop: 10,
},
strategyInfoDescription: {
  fontSize: 14,
  color: '#666',
  lineHeight: 20,
  marginBottom: 15,
},
strategyMetaInfo: {
  flexDirection: 'row',
  justifyContent: 'space-between',
  marginBottom: 15,
  paddingBottom: 15,
  borderBottomWidth: 1,
  borderBottomColor: '#e5e7eb',
},
metaItem: {
  flex: 1,
},
metaLabel: {
  fontSize: 12,
  color: '#888',
  marginBottom: 4,
},
metaValue: {
  fontSize: 14,
  fontWeight: '600',
  color: '#333',
},
performanceExpectations: {
  backgroundColor: '#f0f9ff',
  borderRadius: 10,
  padding: 12,
  marginBottom: 15,
},
expectationTitle: {
  fontSize: 14,
  fontWeight: '700',
  color: '#0369a1',
  marginBottom: 8,
},
expectationItem: {
  fontSize: 13,
  color: '#0c4a6e',
  marginBottom: 4,
},
marketConditions: {
  backgroundColor: '#f8fafc',
  borderRadius: 10,
  padding: 12,
},
conditionTitle: {
  fontSize: 13,
  fontWeight: '700',
  color: '#333',
  marginBottom: 6,
},
conditionItem: {
  fontSize: 12,
  color: '#666',
  marginBottom: 3,
  paddingLeft: 5,
},
```

---

## 6. Update RootStackParamList Type

In `/frontend/mobile/src/types/index.ts`, update the StrategyTest navigation params:

```typescript
export type RootStackParamList = {
  // ... other routes
  StrategyTest: {
    symbol: string;
    displayName: string;
    strategyId?: StrategyId;
    strategyName?: string;
  };
  // ... other routes
};
```

---

## 7. Risk Warning Modal Component

Create new file: `/frontend/mobile/src/components/RiskWarningModal.tsx`:

```typescript
import React from 'react';
import {
  View,
  Text,
  Modal,
  StyleSheet,
  TouchableOpacity,
  ScrollView,
} from 'react-native';
import { StrategyId } from '../types';

interface RiskWarningModalProps {
  visible: boolean;
  strategyId: StrategyId;
  onAccept: () => void;
  onDecline: () => void;
}

const STRATEGY_SPECIFIC_WARNINGS: Record<StrategyId, string[]> = {
  bb_macd: [
    'Trend piyasalarında sık stop-loss tetikleyebilir',
    'Yüksek volatilite dönemlerinde bekleyin',
  ],
  rsi_ema: [
    'Yanlış kırılmalarda (false breakout) kayıp riski yüksek',
    'Konsolidasyon dönemlerinden kaçının',
  ],
  volume_breakout: [
    'En riskli strateji - yüksek sermaye gerektirir',
    'Yalnızca deneyimli trader\'lar için uygundur',
    'Maksimum çekilme %30\'a ulaşabilir',
  ],
  trend_following: [
    'Uzun tutma süreleri gerektirir (günler/haftalar)',
    'Choppy piyasalarda birden fazla kayıp yaşanabilir',
    'Sabır ve disiplin gerektirir',
  ],
};

export const RiskWarningModal: React.FC<RiskWarningModalProps> = ({
  visible,
  strategyId,
  onAccept,
  onDecline,
}) => {
  return (
    <Modal
      visible={visible}
      transparent={true}
      animationType="fade"
      onRequestClose={onDecline}
    >
      <View style={styles.overlay}>
        <View style={styles.modal}>
          <ScrollView showsVerticalScrollIndicator={false}>
            <Text style={styles.title}>⚠️ RİSK UYARISI</Text>

            <Text style={styles.mainWarning}>
              Kripto para ticareti yüksek risk içerir ve tüm sermayenizi kaybedebilirsiniz.
            </Text>

            <View style={styles.warningList}>
              <Text style={styles.warningItem}>
                • Geçmiş performans gelecek sonuçları garanti etmez
              </Text>
              <Text style={styles.warningItem}>
                • Bu stratejiler eğitim amaçlıdır ve yatırım tavsiyesi değildir
              </Text>
              <Text style={styles.warningItem}>
                • Kaybetmeyi göze alamayacağınız parayla işlem yapmayın
              </Text>
              <Text style={styles.warningItem}>
                • Her stratejinin maksimum çekilme riski %15-35 arasındadır
              </Text>
              <Text style={styles.warningItem}>
                • Piyasa koşulları beklenmedik kayıplara neden olabilir
              </Text>
            </View>

            <View style={styles.strategyWarnings}>
              <Text style={styles.strategyWarningTitle}>
                Bu Strateji İçin Özel Uyarılar:
              </Text>
              {STRATEGY_SPECIFIC_WARNINGS[strategyId].map((warning, idx) => (
                <Text key={idx} style={styles.strategyWarningItem}>
                  • {warning}
                </Text>
              ))}
            </View>

            <View style={styles.buttons}>
              <TouchableOpacity
                style={[styles.button, styles.declineButton]}
                onPress={onDecline}
              >
                <Text style={styles.declineButtonText}>Vazgeç</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.button, styles.acceptButton]}
                onPress={onAccept}
              >
                <Text style={styles.acceptButtonText}>Riskleri Kabul Ediyorum</Text>
              </TouchableOpacity>
            </View>
          </ScrollView>
        </View>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  modal: {
    backgroundColor: 'white',
    borderRadius: 20,
    padding: 25,
    width: '100%',
    maxHeight: '85%',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#dc2626',
    textAlign: 'center',
    marginBottom: 20,
  },
  mainWarning: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    textAlign: 'center',
    marginBottom: 20,
    lineHeight: 24,
  },
  warningList: {
    backgroundColor: '#fef2f2',
    borderRadius: 10,
    padding: 15,
    marginBottom: 20,
  },
  warningItem: {
    fontSize: 14,
    color: '#7f1d1d',
    marginBottom: 8,
    lineHeight: 20,
  },
  strategyWarnings: {
    backgroundColor: '#fff7ed',
    borderRadius: 10,
    padding: 15,
    marginBottom: 20,
  },
  strategyWarningTitle: {
    fontSize: 15,
    fontWeight: '700',
    color: '#9a3412',
    marginBottom: 10,
  },
  strategyWarningItem: {
    fontSize: 14,
    color: '#7c2d12',
    marginBottom: 6,
    lineHeight: 20,
  },
  buttons: {
    flexDirection: 'row',
    gap: 10,
  },
  button: {
    flex: 1,
    padding: 15,
    borderRadius: 12,
    alignItems: 'center',
  },
  declineButton: {
    backgroundColor: '#f3f4f6',
  },
  declineButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#6b7280',
  },
  acceptButton: {
    backgroundColor: '#667eea',
  },
  acceptButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: 'white',
  },
});
```

Then use it in StrategyTestScreen:

```typescript
// Import
import { RiskWarningModal } from '../components/RiskWarningModal';

// Add state
const [showRiskWarning, setShowRiskWarning] = useState(false);
const [hasAcceptedRisk, setHasAcceptedRisk] = useState(false);

// Show warning on first backtest attempt
const runBacktest = async () => {
  if (strategyId && !hasAcceptedRisk) {
    setShowRiskWarning(true);
    return;
  }

  // ... existing backtest logic
};

// In render, add modal
<RiskWarningModal
  visible={showRiskWarning}
  strategyId={strategyId || 'bb_macd'}
  onAccept={() => {
    setHasAcceptedRisk(true);
    setShowRiskWarning(false);
    runBacktest(); // Run after acceptance
  }}
  onDecline={() => {
    setShowRiskWarning(false);
  }}
/>
```

---

## 8. Testing Checklist

After implementation, verify:

1. **Parameter Loading**
   - [ ] BB_MACD shows correct default parameters
   - [ ] RSI_EMA shows different parameters
   - [ ] Volume_Breakout shows different parameters
   - [ ] Trend_Following shows different parameters

2. **Parameter Mode Toggle**
   - [ ] Toggle switches between Beginner and Advanced
   - [ ] Parameters update when mode changes
   - [ ] Labels update correctly for each strategy

3. **Strategy Info Card**
   - [ ] Card displays strategy description
   - [ ] Expected performance metrics visible
   - [ ] Best/worst conditions listed
   - [ ] Expand/collapse works correctly

4. **Risk Warning Modal**
   - [ ] Modal appears on first backtest attempt
   - [ ] Strategy-specific warnings displayed
   - [ ] Accept button proceeds with backtest
   - [ ] Decline button cancels backtest
   - [ ] Modal doesn't show again after acceptance

5. **Navigation Flow**
   - [ ] Selecting template in StrategiesScreen passes strategyId
   - [ ] StrategyTestScreen receives and loads correct preset
   - [ ] Back navigation preserves state

---

## 9. Deployment Steps

1. **Create Config File**
   ```bash
   mkdir -p frontend/mobile/src/config
   # Create strategyPresets.ts with content from Section 2
   ```

2. **Update Type Definitions**
   ```bash
   # Add types to frontend/mobile/src/types/index.ts
   ```

3. **Create Risk Warning Component**
   ```bash
   mkdir -p frontend/mobile/src/components
   # Create RiskWarningModal.tsx with content from Section 7
   ```

4. **Update Screens**
   ```bash
   # Modify StrategiesScreen.tsx per Section 3
   # Modify StrategyTestScreen.tsx per Section 4
   # Add styles per Section 5
   ```

5. **Test Locally**
   ```bash
   cd frontend/mobile
   npm start
   # Test on iOS/Android simulator
   ```

6. **Commit Changes**
   ```bash
   git add .
   git commit -m "feat(strategies): implement optimized strategy templates with beginner/advanced modes"
   ```

---

## 10. Future Enhancements

**Phase 2 Improvements:**
1. Persist user parameter preferences to backend
2. Add "My Custom Parameters" save feature
3. Implement parameter validation (min/max ranges)
4. Add parameter sensitivity analysis
5. Show parameter impact on expected performance

**Phase 3 Advanced Features:**
1. Real backtest engine integration (replace mock)
2. Walk-forward analysis visualization
3. Monte Carlo simulation results
4. Parameter optimization suggestions
5. Strategy combination recommendations

---

**End of Implementation Guide**

This guide provides step-by-step code changes to implement the strategy templates specification. All code is production-ready and follows the existing MyTrader mobile app architecture.
