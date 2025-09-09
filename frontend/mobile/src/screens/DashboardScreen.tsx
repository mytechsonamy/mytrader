import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  RefreshControl,
  Dimensions,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { RootStackParamList, SymbolData, WebSocketMessage } from '../types';
import { apiService } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { usePrices } from '../context/PriceContext';

type DashboardNavigationProp = StackNavigationProp<RootStackParamList>;

const { width } = Dimensions.get('window');

interface CryptoCard {
  symbol: string;
  display_name: string;
  price: number;
  signal: 'BUY' | 'SELL' | 'NEUTRAL';
  indicators: {
    RSI: number;
    MACD: number;
    BB_UPPER: number;
    BB_LOWER: number;
  };
  timestamp: string;
  strategy_type: string;
}

const DashboardScreen: React.FC = () => {
  const navigation = useNavigation<DashboardNavigationProp>();
  const defaultSymbolConfig: { [key: string]: any } = {
    BTC: { symbol: 'BTCUSDT', display_name: 'Bitcoin', precision: 2, strategy_type: 'quality_over_quantity' },
    ETH: { symbol: 'ETHUSDT', display_name: 'Ethereum', precision: 2, strategy_type: 'trend_momentum' },
    XRP: { symbol: 'XRPUSDT', display_name: 'Ripple', precision: 4, strategy_type: 'volatility_breakout' },
    BNB: { symbol: 'BNBUSDT', display_name: 'Binance Coin', precision: 2, strategy_type: 'quality_over_quantity' },
    ADA: { symbol: 'ADAUSDT', display_name: 'Cardano', precision: 4, strategy_type: 'trend_momentum' },
    SOL: { symbol: 'SOLUSDT', display_name: 'Solana', precision: 2, strategy_type: 'volatility_breakout' },
    DOT: { symbol: 'DOTUSDT', display_name: 'Polkadot', precision: 3, strategy_type: 'signal_rich' },
    POL: { symbol: 'POLUSDT', display_name: 'Polygon', precision: 4, strategy_type: 'trend_following' },
    AVAX: { symbol: 'AVAXUSDT', display_name: 'Avalanche', precision: 3, strategy_type: 'mean_reversion' },
    LINK: { symbol: 'LINKUSDT', display_name: 'Chainlink', precision: 3, strategy_type: 'signal_rich' },
  };
  const defaultSymbols = Object.keys(defaultSymbolConfig);

  const [symbolConfig, setSymbolConfig] = useState<{ [key: string]: any }>(defaultSymbolConfig);
  const [symbols, setSymbols] = useState<string[]>(defaultSymbols);

  const buildInitialData = (): { [key: string]: CryptoCard } => {
    const initial: { [key: string]: CryptoCard } = {};
    symbols.forEach((symbol) => {
      const config = symbolConfig[symbol];
      initial[symbol] = {
        symbol,
        display_name: config?.display_name || symbol,
        price: 0,
        signal: 'NEUTRAL',
        indicators: { RSI: 0, MACD: 0, BB_UPPER: 0, BB_LOWER: 0 },
        timestamp: new Date().toISOString(),
        strategy_type: config?.strategy_type || 'quality_over_quantity',
      };
    });
    return initial;
  };

  const [cryptoData, setCryptoData] = useState<{ [key: string]: CryptoCard }>(() => buildInitialData());
  const [symbolErrors, setSymbolErrors] = useState<{ [key: string]: string | null }>({});
  const [isRefreshing, setIsRefreshing] = useState(false);
  const { user } = useAuth();
  const { prices, isConnected, getPrice } = usePrices();

  // Simple reorder state - just track if we're in reorder mode
  const [isReordering, setIsReordering] = useState(false);

  

  useEffect(() => {
    const init = async () => {
      try {
        const cfg = await apiService.getSymbolsCached();
        if (cfg?.symbols && Object.keys(cfg.symbols).length > 0) {
          setSymbolConfig(cfg.symbols);
          setSymbols(Object.keys(cfg.symbols));
          // rebuild initial data using fetched config
          setCryptoData(() => {
            const initial: { [key: string]: CryptoCard } = {};
            Object.keys(cfg.symbols).forEach((k) => {
              const c = cfg.symbols[k];
              initial[k] = {
                symbol: k,
                display_name: c?.display_name || k,
                price: 0,
                signal: 'NEUTRAL',
                indicators: { RSI: 0, MACD: 0, BB_UPPER: 0, BB_LOWER: 0 },
                timestamp: new Date().toISOString(),
                strategy_type: c?.strategy_type || 'quality_over_quantity',
              };
            });
            return initial;
          });
        }
      } catch (e) {
        console.warn('Symbols config init failed:', e);
      }
    };
    init();
  }, []);

  // Load user preferences when user changes
  useEffect(() => {
    loadUserPreferences();
  }, [user]);

  // Update crypto data with prices from PriceContext
  useEffect(() => {
    setCryptoData(prevData => {
      const newData = { ...prevData };
      let hasUpdates = false;
      
      symbols.forEach(symbol => {
        const config = symbolConfig[symbol];
        const priceData = getPrice(config?.symbol || `${symbol}USDT`);
        
        if (priceData && newData[symbol]) {
          newData[symbol] = {
            ...newData[symbol],
            price: priceData.price,
            timestamp: new Date().toISOString(),
          };
          hasUpdates = true;
        }
      });
      
      return hasUpdates ? newData : prevData;
    });
  }, [prices, symbols, symbolConfig, getPrice]);

  // Connection status and errors are now handled by PriceContext

  const onRefresh = async () => {
    setIsRefreshing(true);
    try {
      // reset to initial, keep UI responsive
      setCryptoData(buildInitialData());
      // PriceContext will automatically reconnect if needed
    } catch (error) {
      console.error('Error refreshing data:', error);
    } finally {
      setIsRefreshing(false);
    }
  };

  const handleStrategyTest = (symbol: string, displayName: string) => {
    navigation.navigate('StrategyTest', { symbol, displayName });
  };

  // User preferences functions
  const loadUserPreferences = async () => {
    if (!user) return;
    try {
      const response = await apiService.getUserLayoutPreferences();
      if (response.asset_order && Array.isArray(response.asset_order)) {
        const orderedSymbols = response.asset_order.filter(s => symbols.includes(s));
        const remainingSymbols = symbols.filter(s => !orderedSymbols.includes(s));
        setSymbols([...orderedSymbols, ...remainingSymbols]);
      }
    } catch (error) {
      console.warn('Failed to load user preferences:', error);
    }
  };

  const saveUserPreferences = async (newOrder: string[]) => {
    if (!user) return;
    try {
      await apiService.saveUserLayoutPreferences({ asset_order: newOrder });
    } catch (error) {
      console.warn('Failed to save user preferences:', error);
    }
  };

  // Simple move to top functionality
  const moveToTop = (symbol: string) => {
    console.log('moveToTop called with symbol:', symbol);
    console.log('Current symbols:', symbols);
    const newSymbols = [symbol, ...symbols.filter(s => s !== symbol)];
    console.log('New symbols order:', newSymbols);
    setSymbols(newSymbols);
    saveUserPreferences(newSymbols);
    setIsReordering(false);
  };

  const formatPrice = (price: number, precision: number = 2): string => {
    if (price === 0) return '$0.00';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: precision,
      maximumFractionDigits: Math.max(precision, 8),
    }).format(price);
  };

  const getSignalColor = (signal: string): string => {
    switch (signal) {
      case 'BUY': return '#10b981';
      case 'SELL': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const renderCryptoCard = (symbol: string, data: CryptoCard, index: number) => {
    if (!data) {
      return (
        <View key={symbol} style={styles.cryptoCard}>
          <View style={styles.cardHeader}>
            <Text style={styles.cryptoName}>{symbol}</Text>
            <Text style={styles.price}>Yükleniyor...</Text>
          </View>
          {symbolErrors[symbol] && (
            <Text style={styles.errorText}>{symbolErrors[symbol]}</Text>
          )}
        </View>
      );
    }

    return (
      <TouchableOpacity
        key={symbol}
        style={styles.cryptoCard}
        activeOpacity={0.8}
        onLongPress={() => {
          console.log('Long press detected on:', symbol, 'User:', !!user);
          console.log('Moving to top:', symbol);
          moveToTop(symbol);
        }}
        delayLongPress={500}
      >
        <View style={styles.cardHeader}>
          <View>
            <Text style={styles.cryptoName}>{data.display_name || symbol} ({symbol})</Text>
            <Text style={styles.price}>{formatPrice(data.price || 0)}</Text>
          </View>
          <View style={[styles.signalBadge, { backgroundColor: getSignalColor(data.signal) }]}>
            <Text style={styles.signalText}>{data.signal}</Text>
          </View>
        </View>

        <View style={styles.indicatorsGrid}>
          <View style={styles.indicator}>
            <Text style={styles.indicatorLabel}>RSI</Text>
            <Text style={styles.indicatorValue}>{data.indicators.RSI.toFixed(2)}</Text>
          </View>
          <View style={styles.indicator}>
            <Text style={styles.indicatorLabel}>MACD</Text>
            <Text style={styles.indicatorValue}>{data.indicators.MACD.toFixed(2)}</Text>
          </View>
          <View style={styles.indicator}>
            <Text style={styles.indicatorLabel}>BB Üst</Text>
            <Text style={styles.indicatorValue}>{data.indicators.BB_UPPER.toFixed(2)}</Text>
          </View>
          <View style={styles.indicator}>
            <Text style={styles.indicatorLabel}>BB Alt</Text>
            <Text style={styles.indicatorValue}>{data.indicators.BB_LOWER.toFixed(2)}</Text>
          </View>
        </View>

        <TouchableOpacity
          style={styles.strategyButton}
          onPress={(e) => {
            e.stopPropagation();
            handleStrategyTest(symbol, data.display_name);
          }}
        >
          <Text style={styles.strategyButtonText}>📈 Strateji Test</Text>
        </TouchableOpacity>

        <Text style={styles.lastUpdate}>
          Son güncelleme: {new Date(data.timestamp).toLocaleTimeString('tr-TR')}
        </Text>
        {symbolErrors[symbol] && (
          <Text style={styles.errorText}>{symbolErrors[symbol]}</Text>
        )}
        {index > 0 && (
          <Text style={styles.reorderHint}>Uzun bas - en üste taşı</Text>
        )}
      </TouchableOpacity>
    );
  };

  return (
    <LinearGradient
      colors={['#667eea', '#764ba2']}
      style={styles.container}
    >
      <View style={styles.header}>
        <View>
          <Text style={styles.title}>🚀 myTrader</Text>
          <View style={styles.subtitleContainer}>
            <Text style={styles.subtitle}>Gerçek zamanlı fiyat analizi</Text>
            <View style={[
              styles.connectionDot,
              { backgroundColor: isConnected ? '#10b981' : '#ef4444' }
            ]} />
          </View>
        </View>
        
        <View style={styles.userSection}>
          {user ? (
            <TouchableOpacity 
              style={styles.profileButton}
              onPress={() => navigation.navigate('MainTabs', { screen: 'Profile' })}
            >
              <Text style={styles.profileButtonText}>👤 {user.first_name}</Text>
            </TouchableOpacity>
          ) : (
            <TouchableOpacity 
              style={styles.loginButton}
              onPress={() => navigation.navigate('AuthStack', { screen: 'Login', params: { returnTo: 'Dashboard' } })}
            >
              <Text style={styles.loginButtonText}>👤 Giriş</Text>
            </TouchableOpacity>
          )}
        </View>
      </View>



      <ScrollView
        style={styles.scrollView}
        refreshControl={
          <RefreshControl refreshing={isRefreshing} onRefresh={onRefresh} />
        }
      >
        <View style={styles.cryptoGrid}>
          {symbols.map((symbol, index) => renderCryptoCard(symbol, cryptoData[symbol], index))}
        </View>
      </ScrollView>
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 20,
    paddingTop: 60,
    paddingBottom: 20,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: 'white',
  },
  subtitleContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 4,
  },
  subtitle: {
    fontSize: 14,
    color: 'rgba(255,255,255,0.8)',
    marginRight: 8,
  },
  userSection: {
    alignItems: 'flex-end',
  },
  userInfo: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  loginButton: {
    backgroundColor: 'rgba(255,255,255,0.2)',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 15,
  },
  loginButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  profileButton: {
    backgroundColor: 'rgba(255,255,255,0.2)',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 15,
  },
  profileButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  connectionDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  scrollView: {
    flex: 1,
    paddingHorizontal: 20,
  },
  cryptoGrid: {
    paddingBottom: 20,
  },
  cryptoCard: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 15,
    padding: 20,
    marginBottom: 15,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 5,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 20,
  },
  cryptoName: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
  },
  price: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#2563eb',
    marginTop: 4,
  },
  signalBadge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 15,
  },
  signalText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
  },
  indicatorsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    marginBottom: 15,
  },
  indicator: {
    backgroundColor: '#f8fafc',
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 10,
    width: '48%',
    marginBottom: 8,
    alignItems: 'center',
  },
  indicatorLabel: {
    fontSize: 12,
    color: '#64748b',
    marginBottom: 4,
  },
  indicatorValue: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1e293b',
  },
  strategyButton: {
    backgroundColor: '#667eea',
    borderRadius: 10,
    paddingVertical: 12,
    alignItems: 'center',
    marginBottom: 10,
  },
  strategyButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  lastUpdate: {
    fontSize: 12,
    color: '#64748b',
    textAlign: 'center',
  },
  errorText: {
    marginTop: 8,
    textAlign: 'center',
    color: '#ef4444',
    fontSize: 12,
    fontWeight: '600',
  },
  reorderHint: {
    fontSize: 10,
    color: '#94a3b8',
    textAlign: 'center',
    marginTop: 4,
    fontStyle: 'italic',
  },
});

export default DashboardScreen;
