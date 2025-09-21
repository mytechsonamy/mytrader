import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  RefreshControl,
  Alert,
  Dimensions,
} from 'react-native';
import { LineChart } from 'react-native-chart-kit';
import { usePortfolio } from '../context/PortfolioContext';
import { useAuth } from '../context/AuthContext';
import { Portfolio } from '../types';

const { width: screenWidth } = Dimensions.get('window');

const PortfolioScreen = ({ navigation }: any) => {
  const { user } = useAuth();
  const { state, loadPortfolios, selectPortfolio, clearError } = usePortfolio();
  const [selectedPortfolioId, setSelectedPortfolioId] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    if (user) {
      loadPortfolios();
    }
  }, [user, loadPortfolios]);

  const onRefresh = async () => {
    setRefreshing(true);
    await loadPortfolios();
    if (selectedPortfolioId) {
      await selectPortfolio(selectedPortfolioId);
    }
    setRefreshing(false);
  };

  const handlePortfolioSelect = async (portfolio: Portfolio) => {
    setSelectedPortfolioId(portfolio.id);
    await selectPortfolio(portfolio.id);
  };

  const handleCreatePortfolio = () => {
    Alert.prompt(
      'Yeni Portföy',
      'Portföy adını girin:',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Oluştur',
          onPress: async (name) => {
            if (name?.trim()) {
              // Navigate to create portfolio screen or handle inline
              console.log('Creating portfolio:', name);
            }
          },
        },
      ],
      'plain-text'
    );
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
    }).format(value);
  };

  const formatPercentage = (value: number) => {
    const sign = value >= 0 ? '+' : '';
    return `${sign}${value.toFixed(2)}%`;
  };

  const getColorForValue = (value: number) => {
    return value >= 0 ? '#10b981' : '#ef4444';
  };

  if (!user) {
    return (
      <View style={styles.container}>
        <View style={styles.centerContent}>
          <Text style={styles.title}>📊 Portföy Yönetimi</Text>
          <Text style={styles.guestText}>Portföy özelliklerini kullanmak için giriş yapmanız gerekiyor.</Text>
          <TouchableOpacity
            style={styles.loginButton}
            onPress={() => navigation.navigate('AuthStack')}
          >
            <Text style={styles.loginButtonText}>🔑 Giriş Yap</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  if (state.loadingState === 'loading' && !refreshing) {
    return (
      <View style={styles.container}>
        <View style={styles.centerContent}>
          <Text style={styles.loadingText}>📊 Portföyler yükleniyor...</Text>
        </View>
      </View>
    );
  }

  if (state.error) {
    return (
      <View style={styles.container}>
        <View style={styles.centerContent}>
          <Text style={styles.errorText}>❌ Hata: {state.error}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={() => {
            clearError();
            loadPortfolios();
          }}>
            <Text style={styles.retryButtonText}>🔄 Tekrar Dene</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <ScrollView
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        showsVerticalScrollIndicator={false}
      >
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.title}>📊 Portföy Yönetimi</Text>
          <TouchableOpacity style={styles.createButton} onPress={handleCreatePortfolio}>
            <Text style={styles.createButtonText}>+ Yeni</Text>
          </TouchableOpacity>
        </View>

        {/* Portfolio List */}
        {state.portfolios.length === 0 ? (
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>📈 Henüz portföyünüz yok</Text>
            <Text style={styles.emptySubtext}>İlk portföyünüzü oluşturun ve yatırımlarınızı takip edin</Text>
            <TouchableOpacity style={styles.createFirstButton} onPress={handleCreatePortfolio}>
              <Text style={styles.createFirstButtonText}>💼 İlk Portföyümü Oluştur</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <View style={styles.portfoliosList}>
            {state.portfolios.map((portfolio) => (
              <TouchableOpacity
                key={portfolio.id}
                style={[
                  styles.portfolioCard,
                  selectedPortfolioId === portfolio.id && styles.selectedPortfolioCard,
                ]}
                onPress={() => handlePortfolioSelect(portfolio)}
              >
                <View style={styles.portfolioHeader}>
                  <Text style={styles.portfolioName}>{portfolio.name}</Text>
                  <Text style={styles.portfolioCurrency}>{portfolio.baseCurrency}</Text>
                </View>
                
                <View style={styles.portfolioMetrics}>
                  <View style={styles.metricRow}>
                    <Text style={styles.metricLabel}>Toplam Değer:</Text>
                    <Text style={styles.metricValue}>{formatCurrency(portfolio.totalValue)}</Text>
                  </View>
                  
                  <View style={styles.metricRow}>
                    <Text style={styles.metricLabel}>Günlük K/Z:</Text>
                    <Text style={[styles.metricValue, { color: getColorForValue(portfolio.dailyPnL) }]}>
                      {formatCurrency(portfolio.dailyPnL)} ({formatPercentage(portfolio.dailyPnL / portfolio.totalValue * 100)})
                    </Text>
                  </View>
                  
                  <View style={styles.metricRow}>
                    <Text style={styles.metricLabel}>Toplam K/Z:</Text>
                    <Text style={[styles.metricValue, { color: getColorForValue(portfolio.totalPnL) }]}>
                      {formatCurrency(portfolio.totalPnL)} ({formatPercentage(portfolio.totalPnLPercent)})
                    </Text>
                  </View>
                </View>

                {portfolio.description && (
                  <Text style={styles.portfolioDescription}>{portfolio.description}</Text>
                )}
              </TouchableOpacity>
            ))}
          </View>
        )}

        {/* Current Portfolio Details */}
        {state.currentPortfolio && (
          <View style={styles.portfolioDetails}>
            <Text style={styles.sectionTitle}>📈 {state.currentPortfolio.name} Detayları</Text>
            
            {/* Performance Chart */}
            {state.analytics && (
              <View style={styles.chartContainer}>
                <Text style={styles.chartTitle}>Performans Grafiği</Text>
                <LineChart
                  data={{
                    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                    datasets: [
                      {
                        data: [
                          state.currentPortfolio.totalValue * 0.8,
                          state.currentPortfolio.totalValue * 0.85,
                          state.currentPortfolio.totalValue * 0.9,
                          state.currentPortfolio.totalValue * 0.95,
                          state.currentPortfolio.totalValue * 0.98,
                          state.currentPortfolio.totalValue,
                        ],
                      },
                    ],
                  }}
                  width={screenWidth - 40}
                  height={220}
                  yAxisLabel="$"
                  chartConfig={{
                    backgroundColor: '#667eea',
                    backgroundGradientFrom: '#667eea',
                    backgroundGradientTo: '#764ba2',
                    decimalPlaces: 0,
                    color: (opacity = 1) => `rgba(255, 255, 255, ${opacity})`,
                    labelColor: (opacity = 1) => `rgba(255, 255, 255, ${opacity})`,
                    style: {
                      borderRadius: 16,
                    },
                    propsForDots: {
                      r: '6',
                      strokeWidth: '2',
                      stroke: '#ffa726',
                    },
                  }}
                  bezier
                  style={styles.chart}
                />
              </View>
            )}

            {/* Positions */}
            {state.positions.length > 0 && (
              <View style={styles.positionsContainer}>
                <Text style={styles.sectionTitle}>💰 Pozisyonlar</Text>
                {state.positions.map((position) => (
                  <View key={position.id} style={styles.positionCard}>
                    <View style={styles.positionHeader}>
                      <Text style={styles.positionSymbol}>{position.symbol}</Text>
                      <Text style={styles.positionType}>{position.symbolType}</Text>
                    </View>
                    
                    <View style={styles.positionMetrics}>
                      <View style={styles.positionRow}>
                        <Text style={styles.positionLabel}>Miktar:</Text>
                        <Text style={styles.positionValue}>{position.quantity.toFixed(4)}</Text>
                      </View>
                      
                      <View style={styles.positionRow}>
                        <Text style={styles.positionLabel}>Ortalama Fiyat:</Text>
                        <Text style={styles.positionValue}>{formatCurrency(position.averagePrice)}</Text>
                      </View>
                      
                      <View style={styles.positionRow}>
                        <Text style={styles.positionLabel}>Güncel Değer:</Text>
                        <Text style={styles.positionValue}>{formatCurrency(position.marketValue)}</Text>
                      </View>
                      
                      <View style={styles.positionRow}>
                        <Text style={styles.positionLabel}>K/Z:</Text>
                        <Text style={[styles.positionValue, { color: getColorForValue(position.unrealizedPnL) }]}>
                          {formatCurrency(position.unrealizedPnL)} ({formatPercentage(position.unrealizedPnLPercent)})
                        </Text>
                      </View>
                    </View>
                  </View>
                ))}
              </View>
            )}

            {/* Recent Transactions */}
            {state.transactions.length > 0 && (
              <View style={styles.transactionsContainer}>
                <Text style={styles.sectionTitle}>📝 Son İşlemler</Text>
                {state.transactions.slice(0, 5).map((transaction) => (
                  <View key={transaction.id} style={styles.transactionCard}>
                    <View style={styles.transactionHeader}>
                      <Text style={styles.transactionSymbol}>{transaction.symbol}</Text>
                      <Text
                        style={[
                          styles.transactionType,
                          { color: transaction.type === 'BUY' ? '#10b981' : '#ef4444' },
                        ]}
                      >
                        {transaction.type === 'BUY' ? 'ALIŞ' : 'SATIŞ'}
                      </Text>
                    </View>
                    
                    <View style={styles.transactionDetails}>
                      <Text style={styles.transactionDetail}>
                        {transaction.quantity.toFixed(4)} @ {formatCurrency(transaction.price)}
                      </Text>
                      <Text style={styles.transactionAmount}>
                        {formatCurrency(transaction.amount)}
                      </Text>
                    </View>
                    
                    <Text style={styles.transactionDate}>
                      {new Date(transaction.executedAt).toLocaleDateString('tr-TR')}
                    </Text>
                  </View>
                ))}
              </View>
            )}
          </View>
        )}
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  centerContent: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    paddingTop: 60,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#333',
  },
  createButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
  },
  createButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  loadingText: {
    fontSize: 18,
    color: '#666',
    textAlign: 'center',
  },
  errorText: {
    fontSize: 16,
    color: '#ef4444',
    textAlign: 'center',
    marginBottom: 20,
  },
  retryButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 20,
  },
  retryButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  guestText: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
    marginBottom: 20,
  },
  loginButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 30,
    paddingVertical: 12,
    borderRadius: 25,
  },
  loginButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  emptyContainer: {
    padding: 40,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 20,
    color: '#333',
    fontWeight: '600',
    textAlign: 'center',
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: '#666',
    textAlign: 'center',
    marginBottom: 30,
  },
  createFirstButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 30,
    paddingVertical: 15,
    borderRadius: 25,
  },
  createFirstButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  portfoliosList: {
    padding: 20,
    paddingTop: 0,
  },
  portfolioCard: {
    backgroundColor: 'white',
    borderRadius: 15,
    padding: 20,
    marginBottom: 15,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  selectedPortfolioCard: {
    borderWidth: 2,
    borderColor: '#667eea',
  },
  portfolioHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 15,
  },
  portfolioName: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  portfolioCurrency: {
    fontSize: 14,
    color: '#666',
    backgroundColor: '#f3f4f6',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 8,
  },
  portfolioMetrics: {
    gap: 8,
  },
  metricRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  metricLabel: {
    fontSize: 14,
    color: '#666',
  },
  metricValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
  },
  portfolioDescription: {
    fontSize: 12,
    color: '#888',
    marginTop: 10,
    fontStyle: 'italic',
  },
  portfolioDetails: {
    padding: 20,
  },
  sectionTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 15,
  },
  chartContainer: {
    backgroundColor: 'white',
    borderRadius: 15,
    padding: 15,
    marginBottom: 20,
    alignItems: 'center',
  },
  chartTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 10,
  },
  chart: {
    borderRadius: 16,
  },
  positionsContainer: {
    marginBottom: 20,
  },
  positionCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 15,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  positionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 10,
  },
  positionSymbol: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
  },
  positionType: {
    fontSize: 12,
    color: '#666',
    backgroundColor: '#f3f4f6',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6,
  },
  positionMetrics: {
    gap: 5,
  },
  positionRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  positionLabel: {
    fontSize: 13,
    color: '#666',
  },
  positionValue: {
    fontSize: 13,
    fontWeight: '600',
    color: '#333',
  },
  transactionsContainer: {
    marginBottom: 20,
  },
  transactionCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 15,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  transactionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  transactionSymbol: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#333',
  },
  transactionType: {
    fontSize: 12,
    fontWeight: '600',
  },
  transactionDetails: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 5,
  },
  transactionDetail: {
    fontSize: 13,
    color: '#666',
  },
  transactionAmount: {
    fontSize: 13,
    fontWeight: '600',
    color: '#333',
  },
  transactionDate: {
    fontSize: 11,
    color: '#888',
  },
});

export default PortfolioScreen;