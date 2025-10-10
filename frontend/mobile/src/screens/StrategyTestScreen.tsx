import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
  Modal,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { RouteProp, useRoute, useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { RootStackParamList, BacktestResult, StrategyConfig } from '../types';
import { apiService } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { usePrices } from '../context/PriceContext';

type StrategyTestRouteProp = RouteProp<RootStackParamList, 'StrategyTest'>;
type StrategyTestNavigationProp = StackNavigationProp<RootStackParamList, 'StrategyTest'>;

const StrategyTestScreen: React.FC = () => {
  const route = useRoute<StrategyTestRouteProp>();
  const navigation = useNavigation<StrategyTestNavigationProp>();
  const { user } = useAuth();
  const { getPriceBySymbol, connectionStatus } = usePrices();
  const { symbol, displayName, assetClass, templateId, strategyName: templateStrategyName, bestFor, defaultParameters } = route.params;
  
  // Determine asset class - default to CRYPTO if not provided for backward compatibility
  const symbolAssetClass = assetClass || 'CRYPTO';

  // Helper to check if connected
  const isConnected = connectionStatus === 'connected';

  // Strategy parameters - use defaultParameters if provided from template, otherwise fall back to generic defaults
  const [parameters, setParameters] = useState(() => {
    if (defaultParameters) {
      return defaultParameters;
    }
    // Fallback defaults
    return {
      bb_period: '20',
      bb_std: '2.0',
      macd_fast: '12',
      macd_slow: '26',
      macd_signal: '9',
      rsi_period: '14',
      rsi_overbought: '70',
      rsi_oversold: '30',
    };
  });
  
  // Save modal state
  const [showSaveModal, setShowSaveModal] = useState(false);
  const [strategyName, setStrategyName] = useState('');
  const [description, setDescription] = useState('');
  const [isStrategyNameInitialized, setIsStrategyNameInitialized] = useState(false);

  const [isLoading, setIsLoading] = useState(false);
  const [backtestResult, setBacktestResult] = useState<BacktestResult | null>(null);
  const [currentPrice, setCurrentPrice] = useState<number>(0);
  const [priceChange24h, setPriceChange24h] = useState<number>(0);

  // Strategy info state
  const [showStrategyInfo, setShowStrategyInfo] = useState(false);

  // Helper function to get entry conditions based on strategy template
  const getEntryConditions = (templateId: string): string => {
    const conditions: Record<string, string> = {
      'bb_macd': '• Fiyat alt BB bandına dokunur\n• MACD yukarı keser\n• RSI < 40',
      'rsi_ema': '• EMA(9) EMA(21)\'i yukarı keser\n• 40 < RSI < 70\n• Volume > 1.2x ortalama',
      'volume_breakout': '• Fiyat 20 günlük yüksek kırılımı\n• Volume > 2x ortalama\n• RSI > 50',
      'trend_following': '• EMA(21) > EMA(50) > EMA(200)\n• Fiyat > EMA(21)\n• ADX > 25',
    };
    return conditions[templateId] || 'Giriş koşulları yükleniyor...';
  };

  // Get real-time price data from dashboard WebSocket
  const updatePriceFromDashboard = () => {
    // Use the correct PriceContext API with the symbol's asset class
    const priceData = getPriceBySymbol(symbol, symbolAssetClass);

    if (priceData && priceData.price > 0) {
      // Use real-time data from dashboard
      setCurrentPrice(priceData.price);
      setPriceChange24h(priceData.changePercent || 0);
      console.log(`Updated ${symbol} price from dashboard: $${priceData.price}, change: ${priceData.changePercent}%`);
    } else if (!isConnected) {
      // Only use external API if dashboard connection is not available
      console.log(`Dashboard not connected for ${symbol}, trying external API...`);
      fetchExternalPrice();
    }
  };

  // Fallback to external API if dashboard is not available
  const fetchExternalPrice = async () => {
    try {
      const response = await fetch(`https://api.binance.com/api/v3/ticker/24hr?symbol=${symbol}`);
      if (response.ok) {
        const data = await response.json();
        setCurrentPrice(parseFloat(data.lastPrice));
        setPriceChange24h(parseFloat(data.priceChangePercent));
        console.log(`Updated ${symbol} price from external API: $${data.lastPrice}`);
      }
    } catch (error) {
      console.warn(`Failed to fetch external price for ${symbol}:`, error);
    }
  };

  // Set strategy name only once when component mounts
  useEffect(() => {
    if (!isStrategyNameInitialized) {
      setStrategyName(`${displayName} Strategy`);
      setIsStrategyNameInitialized(true);
    }
  }, [displayName, isStrategyNameInitialized]);

  // Handle price updates separately
  useEffect(() => {
    updatePriceFromDashboard();

    // Update price every 5 seconds from dashboard or fallback
    const priceInterval = setInterval(updatePriceFromDashboard, 5000);

    return () => clearInterval(priceInterval);
  }, [symbol, displayName, getPriceBySymbol, isConnected]);

  const updateParameter = (key: string, value: string) => {
    setParameters(prev => ({ ...prev, [key]: value }));
  };

  const runBacktest = async () => {
    setIsLoading(true);
    try {
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      const mockResult: BacktestResult = {
        total_return: Math.random() * 50 - 10,
        sharpe_ratio: Math.random() * 2 + 0.5,
        max_drawdown: -(Math.random() * 15 + 5),
        win_rate: Math.random() * 40 + 45,
        total_trades: Math.floor(Math.random() * 200) + 50,
        start_date: '2024-01-01',
        end_date: '2024-12-31',
        final_portfolio_value: 10000 + (Math.random() * 5000 - 1000),
        equity_curve: []
      };
      
      setBacktestResult(mockResult);
    } catch (error) {
      Alert.alert('Hata', 'Backtest çalıştırılırken hata oluştu');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSavePress = () => {
    if (!backtestResult) {
      Alert.alert('Uyarı', 'Önce stratejiyi test edin');
      return;
    }
    
    if (!user) {
      Alert.alert(
        'Giriş Gerekli', 
        'Strateji kaydetmek için giriş yapmalısınız.',
        [
          { text: 'İptal', style: 'cancel' },
          { text: 'Giriş Yap', onPress: () => navigation.navigate('AuthStack', { screen: 'Login', params: { returnTo: 'Dashboard' } }) }
        ]
      );
      return;
    }
    
    setShowSaveModal(true);
  };

  const handleSaveStrategy = async () => {
    if (!strategyName.trim()) {
      Alert.alert('Hata', 'Strateji adı gereklidir');
      return;
    }
    
    try {
      setIsLoading(true);
      const strategyConfig: StrategyConfig = {
        name: strategyName.trim(),
        description: description.trim() || `${displayName} için otomatik oluşturulan strateji`,
        parameters: parameters
      };
      
      // Save strategy via API
      console.log('🎯 StrategyTestScreen: Saving strategy:', strategyConfig);
      const result = await apiService.createStrategy(strategyConfig, symbol);
      console.log('📊 StrategyTestScreen: Create strategy result:', result);

      if (result.success) {
        console.log('✅ StrategyTestScreen: Strategy created successfully, showing success alert');
        Alert.alert('Başarılı', 'Strateji başarıyla kaydedildi', [
          { text: 'Geri Dön', onPress: () => {
            setShowSaveModal(false);
            navigation.goBack();
          }},
          { text: 'Stratejileri Gör', onPress: () => {
            setShowSaveModal(false);
            navigation.navigate('MainTabs', { screen: 'Strategies' });
          }}
        ]);
      } else {
        console.log('❌ StrategyTestScreen: Strategy creation failed:', result.message);
        Alert.alert('Hata', result.message || 'Strateji kaydedilemedi');
      }
    } catch (error) {
      console.error('Strategy save error:', error);
      Alert.alert(
        'Hata', 
        error instanceof Error ? error.message : 'Strateji kaydedilemedi. Lütfen tekrar deneyin.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  const getPerformanceGrade = (totalReturn: number): string => {
    if (totalReturn > 30) return 'A+';
    if (totalReturn > 20) return 'A';
    if (totalReturn > 10) return 'B+';
    if (totalReturn > 5) return 'B';
    if (totalReturn > 0) return 'C+';
    if (totalReturn > -5) return 'C';
    if (totalReturn > -10) return 'D+';
    return 'F';
  };

  const getGradeColor = (grade: string): string => {
    if (grade.startsWith('A')) return '#10b981';
    if (grade.startsWith('B')) return '#059669';
    if (grade.startsWith('C')) return '#f59e0b';
    if (grade.startsWith('D')) return '#ef4444';
    return '#dc2626';
  };

  return (
    <LinearGradient colors={['#667eea', '#764ba2']} style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity 
          style={styles.backButton} 
          onPress={() => navigation.goBack()}
        >
          <Text style={styles.backButtonText}>← Geri</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Strateji Test</Text>
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

      <ScrollView style={styles.scrollView} showsVerticalScrollIndicator={false}>
        {/* Asset Info */}
        <View style={styles.assetCard}>
          <Text style={styles.assetName}>{displayName} ({symbol.replace('USDT', '')})</Text>
          <Text style={styles.assetPrice}>
            ${currentPrice.toLocaleString()} 
            <Text style={[styles.priceChange, { color: priceChange24h >= 0 ? '#10b981' : '#ef4444' }]}>
              {' '}({priceChange24h >= 0 ? '+' : ''}{priceChange24h.toFixed(2)}%)
            </Text>
          </Text>
          <Text style={[styles.connectionStatus, { color: isConnected ? '#10b981' : '#ef4444' }]}>
            {isConnected ? '🟢 Dashboard Connected' : '🔴 Dashboard Offline'}
          </Text>
        </View>

        {/* Strategy Info Section */}
        {templateId && bestFor && (
          <View style={styles.strategyInfoCard}>
            <View style={styles.strategyInfoHeader}>
              <Text style={styles.strategyInfoTitle}>📋 Strateji Bilgisi</Text>
            </View>
            <View style={styles.strategyInfoContent}>
              <View style={styles.infoRow}>
                <Text style={styles.infoLabel}>Strateji:</Text>
                <Text style={styles.infoValue}>{templateStrategyName || 'Özel Strateji'}</Text>
              </View>
              <View style={styles.infoRow}>
                <Text style={styles.infoLabel}>En İyi:</Text>
                <Text style={styles.infoValue}>{bestFor}</Text>
              </View>
              <TouchableOpacity
                style={styles.expandButton}
                onPress={() => setShowStrategyInfo(!showStrategyInfo)}
              >
                <Text style={styles.expandButtonText}>
                  {showStrategyInfo ? '▼ Giriş Koşulları' : '▶ Giriş Koşulları'}
                </Text>
              </TouchableOpacity>
              {showStrategyInfo && (
                <View style={styles.entryConditions}>
                  <Text style={styles.conditionsTitle}>Alım Sinyali:</Text>
                  <Text style={styles.conditionsText}>
                    {getEntryConditions(templateId || '')}
                  </Text>
                </View>
              )}
            </View>
          </View>
        )}

        {/* Compact Parameters */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>📊 Parametreler</Text>
          
          <View style={styles.paramGrid}>
            <View style={styles.paramItem}>
              <Text style={styles.paramLabel}>BB Periyot</Text>
              <TextInput
                style={styles.paramInput}
                value={parameters.bb_period}
                onChangeText={(value) => updateParameter('bb_period', value)}
                keyboardType="numeric"
              />
            </View>
            
            <View style={styles.paramItem}>
              <Text style={styles.paramLabel}>BB Std Sapma</Text>
              <TextInput
                style={styles.paramInput}
                value={parameters.bb_std}
                onChangeText={(value) => updateParameter('bb_std', value)}
                keyboardType="numeric"
              />
            </View>
            
            <View style={styles.paramItem}>
              <Text style={styles.paramLabel}>MACD Hızlı</Text>
              <TextInput
                style={styles.paramInput}
                value={parameters.macd_fast}
                onChangeText={(value) => updateParameter('macd_fast', value)}
                keyboardType="numeric"
              />
            </View>
            
            <View style={styles.paramItem}>
              <Text style={styles.paramLabel}>MACD Yavaş</Text>
              <TextInput
                style={styles.paramInput}
                value={parameters.macd_slow}
                onChangeText={(value) => updateParameter('macd_slow', value)}
                keyboardType="numeric"
              />
            </View>
            
            <View style={styles.paramItem}>
              <Text style={styles.paramLabel}>MACD Sinyal</Text>
              <TextInput
                style={styles.paramInput}
                value={parameters.macd_signal}
                onChangeText={(value) => updateParameter('macd_signal', value)}
                keyboardType="numeric"
              />
            </View>
            
            <View style={styles.paramItem}>
              <Text style={styles.paramLabel}>RSI Periyot</Text>
              <TextInput
                style={styles.paramInput}
                value={parameters.rsi_period}
                onChangeText={(value) => updateParameter('rsi_period', value)}
                keyboardType="numeric"
              />
            </View>
            
            <View style={styles.paramItem}>
              <Text style={styles.paramLabel}>RSI Aşırı Alım</Text>
              <TextInput
                style={styles.paramInput}
                value={parameters.rsi_overbought}
                onChangeText={(value) => updateParameter('rsi_overbought', value)}
                keyboardType="numeric"
              />
            </View>
            
            <View style={styles.paramItem}>
              <Text style={styles.paramLabel}>RSI Aşırı Satım</Text>
              <TextInput
                style={styles.paramInput}
                value={parameters.rsi_oversold}
                onChangeText={(value) => updateParameter('rsi_oversold', value)}
                keyboardType="numeric"
              />
            </View>
          </View>
        </View>

        {/* Test Button */}
        <TouchableOpacity
          style={styles.testButton}
          onPress={runBacktest}
          disabled={isLoading}
        >
          {isLoading ? (
            <ActivityIndicator color="white" />
          ) : (
            <Text style={styles.testButtonText}>🧪 Test Et</Text>
          )}
        </TouchableOpacity>

        {/* Results */}
        {backtestResult && (
          <View style={styles.resultsSection}>
            <View style={styles.resultsHeader}>
              <Text style={styles.resultsTitle}>📈 Test Sonuçları</Text>
              <View style={[
                styles.gradeBadge,
                { backgroundColor: getGradeColor(getPerformanceGrade(backtestResult.total_return)) }
              ]}>
                <Text style={styles.gradeText}>{getPerformanceGrade(backtestResult.total_return)}</Text>
              </View>
            </View>
            
            <View style={styles.metricsGrid}>
              <View style={styles.metric}>
                <Text style={styles.metricLabel}>Toplam Getiri</Text>
                <Text style={[
                  styles.metricValue,
                  { color: backtestResult.total_return >= 0 ? '#10b981' : '#ef4444' }
                ]}>
                  {backtestResult.total_return >= 0 ? '+' : ''}{backtestResult.total_return.toFixed(2)}%
                </Text>
              </View>
              
              <View style={styles.metric}>
                <Text style={styles.metricLabel}>Sharpe Ratio</Text>
                <Text style={styles.metricValue}>{backtestResult.sharpe_ratio.toFixed(2)}</Text>
              </View>
              
              <View style={styles.metric}>
                <Text style={styles.metricLabel}>Max Düşüş</Text>
                <Text style={[styles.metricValue, { color: '#ef4444' }]}>
                  {backtestResult.max_drawdown.toFixed(2)}%
                </Text>
              </View>
              
              <View style={styles.metric}>
                <Text style={styles.metricLabel}>Kazanç Oranı</Text>
                <Text style={styles.metricValue}>{backtestResult.win_rate.toFixed(1)}%</Text>
              </View>
            </View>

            {/* Save Strategy Button */}
            <TouchableOpacity
              style={styles.saveButton}
              onPress={handleSavePress}
            >
              <Text style={styles.saveButtonText}>💾 Strateji Kaydet</Text>
            </TouchableOpacity>
          </View>
        )}
      </ScrollView>

      {/* Save Modal */}
      <Modal
        visible={showSaveModal}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setShowSaveModal(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Strateji Kaydet</Text>
              <TouchableOpacity
                onPress={() => setShowSaveModal(false)}
                style={styles.modalCloseButton}
              >
                <Text style={styles.modalCloseText}>✕</Text>
              </TouchableOpacity>
            </View>
            
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Strateji Adı *</Text>
              <TextInput
                style={styles.textInput}
                value={strategyName}
                onChangeText={setStrategyName}
                placeholder="Örn: BTC Momentum Strategy"
                placeholderTextColor="#999"
              />
            </View>
            
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Açıklama</Text>
              <TextInput
                style={[styles.textInput, styles.textArea]}
                value={description}
                onChangeText={setDescription}
                placeholder="Strateji hakkında kısa açıklama (opsiyonel)"
                placeholderTextColor="#999"
                multiline
                numberOfLines={3}
              />
            </View>
            
            <TouchableOpacity
              style={[styles.modalSaveButton, { opacity: isLoading ? 0.6 : 1 }]}
              onPress={handleSaveStrategy}
              disabled={isLoading}
            >
              {isLoading ? (
                <ActivityIndicator color="white" />
              ) : (
                <Text style={styles.modalSaveButtonText}>💾 Kaydet</Text>
              )}
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 20,
    paddingTop: 50,
    paddingBottom: 15,
  },
  backButton: {
    padding: 8,
  },
  backButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  title: {
    color: 'white',
    fontSize: 20,
    fontWeight: 'bold',
  },
  placeholder: {
    width: 60,
  },
  scrollView: {
    flex: 1,
    paddingHorizontal: 20,
  },
  assetCard: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 15,
    padding: 20,
    marginBottom: 20,
    alignItems: 'center',
  },
  assetName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 8,
  },
  assetPrice: {
    fontSize: 20,
    fontWeight: '600',
    color: '#666',
  },
  priceChange: {
    fontSize: 16,
  },
  connectionStatus: {
    fontSize: 12,
    fontWeight: '600',
    marginTop: 8,
    textAlign: 'center',
  },
  section: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 15,
    padding: 20,
    marginBottom: 20,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 15,
  },
  paramGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
  },
  paramItem: {
    width: '48%',
    marginBottom: 15,
  },
  paramLabel: {
    fontSize: 12,
    color: '#666',
    marginBottom: 5,
    fontWeight: '500',
  },
  paramInput: {
    backgroundColor: '#f8fafc',
    borderRadius: 8,
    padding: 10,
    borderWidth: 1,
    borderColor: '#e5e7eb',
    fontSize: 14,
    color: '#333',
  },
  testButton: {
    backgroundColor: '#667eea',
    borderRadius: 15,
    padding: 16,
    alignItems: 'center',
    marginBottom: 20,
  },
  testButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  resultsSection: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 15,
    padding: 20,
    marginBottom: 30,
  },
  resultsHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 15,
  },
  resultsTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  gradeBadge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
  },
  gradeText: {
    color: 'white',
    fontSize: 14,
    fontWeight: 'bold',
  },
  metricsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    marginBottom: 20,
  },
  metric: {
    width: '48%',
    alignItems: 'center',
    marginBottom: 10,
  },
  metricLabel: {
    fontSize: 12,
    color: '#666',
    marginBottom: 4,
  },
  metricValue: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  saveButton: {
    backgroundColor: '#10b981',
    borderRadius: 12,
    padding: 14,
    alignItems: 'center',
  },
  saveButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  // Modal Styles
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  modalContent: {
    backgroundColor: 'white',
    borderRadius: 15,
    padding: 20,
    width: '90%',
    maxHeight: '80%',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 20,
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
  },
  modalCloseButton: {
    padding: 8,
  },
  modalCloseText: {
    fontSize: 20,
    color: '#666',
  },
  inputGroup: {
    marginBottom: 20,
  },
  inputLabel: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
  },
  textInput: {
    backgroundColor: '#f8fafc',
    borderRadius: 10,
    padding: 12,
    fontSize: 16,
    color: '#333',
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  textArea: {
    height: 80,
    textAlignVertical: 'top',
  },
  modalSaveButton: {
    backgroundColor: '#10b981',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
  },
  modalSaveButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  userSection: {
    alignItems: 'flex-end',
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
  // Strategy Info Card Styles
  strategyInfoCard: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 15,
    padding: 20,
    marginBottom: 20,
  },
  strategyInfoHeader: {
    marginBottom: 12,
  },
  strategyInfoTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  strategyInfoContent: {
    gap: 10,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  infoLabel: {
    fontSize: 14,
    color: '#666',
    fontWeight: '500',
  },
  infoValue: {
    fontSize: 14,
    color: '#333',
    fontWeight: '600',
    flex: 1,
    textAlign: 'right',
  },
  expandButton: {
    backgroundColor: '#f8fafc',
    padding: 12,
    borderRadius: 8,
    marginTop: 8,
  },
  expandButtonText: {
    fontSize: 14,
    color: '#667eea',
    fontWeight: '600',
    textAlign: 'center',
  },
  entryConditions: {
    backgroundColor: '#f0f4ff',
    padding: 12,
    borderRadius: 8,
    marginTop: 8,
  },
  conditionsTitle: {
    fontSize: 13,
    fontWeight: '600',
    color: '#333',
    marginBottom: 6,
  },
  conditionsText: {
    fontSize: 12,
    color: '#555',
    lineHeight: 18,
  },
});

export default StrategyTestScreen;