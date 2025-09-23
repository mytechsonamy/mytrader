import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  View,
  ScrollView,
  StyleSheet,
  RefreshControl,
  Alert,
  Modal,
  Text,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import {
  RootStackParamList,
  EnhancedSymbolDto,
  UnifiedMarketDataDto,
  LeaderboardEntry,
  UserRanking,
  CompetitionStats,
  NewsItem,
  Portfolio,
  MarketStatusDto,
  AssetClassType,
} from '../types';
import { apiService } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { usePrices } from '../context/PriceContext';
import {
  SmartOverviewHeader,
  AssetClassAccordion,
  CompactLeaderboard,
  DashboardErrorBoundary,
  AccordionErrorBoundary,
} from '../components/dashboard';
import { EnhancedNewsPreview } from '../components/news';
import EnhancedNewsScreen from './EnhancedNewsScreen';
import { usePerformanceOptimization } from '../hooks/usePerformanceOptimization';

type DashboardNavigationProp = StackNavigationProp<RootStackParamList>;

interface DashboardState {
  portfolio: Portfolio | null;
  cryptoSymbols: EnhancedSymbolDto[];
  bistSymbols: EnhancedSymbolDto[];
  nasdaqSymbols: EnhancedSymbolDto[];
  marketStatuses: MarketStatusDto[];
  leaderboard: LeaderboardEntry[];
  userRanking: UserRanking | null;
  competitionStats: CompetitionStats | null;
  news: NewsItem[];
  expandedSections: Record<AssetClassType, boolean>;
  isLoading: boolean;
  error: string | null;
}

interface AssetClassConfig {
  type: AssetClassType;
  title: string;
  icon: string;
  priority: number;
}

const DashboardScreen: React.FC = () => {
  const navigation = useNavigation<DashboardNavigationProp>();
  const { user } = useAuth();
  const {
    enhancedPrices,
    getSymbolsByAssetClass,
    getAssetClassSummary,
    refreshPrices,
    connectionStatus,
  } = usePrices();

  // Performance optimization
  const {
    startRender,
    endRender,
    throttledUpdate,
    batchedSetState,
    performanceUtils,
    metrics,
    isActive,
  } = usePerformanceOptimization({
    updateInterval: 1000,
    maxUpdateFrequency: 30, // 30 FPS for dashboard updates
    pauseOnBackground: true,
    enableBatching: true,
    debounceDelay: 200,
  });

  // Asset class configurations
  const assetClassConfigs: AssetClassConfig[] = [
    { type: 'CRYPTO', title: '🚀 Kripto', icon: '🚀', priority: 1 },
    { type: 'STOCK', title: '🏢 BIST Hisseleri', icon: '🏢', priority: 2 },
    { type: 'STOCK', title: '🇺🇸 NASDAQ Hisseleri', icon: '🇺🇸', priority: 3 },
  ];

  const [state, setState] = useState<DashboardState>({
    portfolio: null,
    cryptoSymbols: [],
    bistSymbols: [],
    nasdaqSymbols: [],
    marketStatuses: [],
    leaderboard: [],
    userRanking: null,
    competitionStats: null,
    news: [],
    expandedSections: {
      CRYPTO: true,
      STOCK: false,
      FOREX: false,
      COMMODITY: false,
      INDEX: false,
    },
    isLoading: true,
    error: null,
  });

  const [isRefreshing, setIsRefreshing] = useState(false);
  const [showNewsModal, setShowNewsModal] = useState(false);

  // Helper function to update state with performance optimization
  const updateState = useCallback((updates: Partial<DashboardState>) => {
    setState(prev => ({ ...prev, ...updates }));
  }, []);

  // Asset class summary data
  const assetClassSummary = useMemo(() => {
    return getAssetClassSummary();
  }, [getAssetClassSummary]);

  // Market data organized by symbol ID
  const marketDataBySymbol = useMemo(() => {
    const data: Record<string, UnifiedMarketDataDto> = {};
    Object.entries(enhancedPrices).forEach(([symbolId, marketData]) => {
      data[symbolId] = marketData;
      data[marketData.symbol] = marketData; // Also index by symbol for compatibility
    });
    return data;
  }, [enhancedPrices]);

  // Initialize dashboard data
  const initializeDashboard = useCallback(async () => {
    try {
      updateState({ isLoading: true, error: null });

      // Fetch portfolio data if user is logged in
      let portfolio = null;
      if (user) {
        try {
          const portfolios = await apiService.getPortfolios();
          portfolio = portfolios[0] || null;
        } catch (error) {
          console.warn('Failed to fetch portfolio:', error);
        }
      }

      // Fetch symbols by asset class
      const [cryptoSymbols, marketStatuses] = await Promise.allSettled([
        apiService.getSymbolsByAssetClass('CRYPTO'),
        apiService.getAllMarketStatuses(),
      ]);

      // Fetch BIST symbols (Turkish stocks)
      let bistSymbols: EnhancedSymbolDto[] = [];
      try {
        const allStocks = await apiService.getSymbolsByAssetClass('STOCK');
        bistSymbols = allStocks.filter(symbol =>
          symbol.marketId?.includes('BIST') ||
          symbol.quoteCurrency === 'TRY'
        );
      } catch (error) {
        console.warn('Failed to fetch BIST symbols:', error);
      }

      // Fetch NASDAQ symbols (US stocks)
      let nasdaqSymbols: EnhancedSymbolDto[] = [];
      try {
        const allStocks = await apiService.getSymbolsByAssetClass('STOCK');
        nasdaqSymbols = allStocks.filter(symbol =>
          symbol.marketId?.includes('NASDAQ') ||
          symbol.quoteCurrency === 'USD'
        );
      } catch (error) {
        console.warn('Failed to fetch NASDAQ symbols:', error);
      }

      // Fetch gamification data if user is logged in
      let leaderboard: LeaderboardEntry[] = [];
      let userRanking: UserRanking | null = null;
      let competitionStats: CompetitionStats | null = null;

      if (user) {
        try {
          const [leaderboardResult, userRankingResult, statsResult] = await Promise.allSettled([
            apiService.getLeaderboard('weekly', 10),
            apiService.getUserRanking('weekly'),
            apiService.getCompetitionStats(),
          ]);

          if (leaderboardResult.status === 'fulfilled') {
            leaderboard = leaderboardResult.value;
          }
          if (userRankingResult.status === 'fulfilled') {
            userRanking = userRankingResult.value;
          }
          if (statsResult.status === 'fulfilled') {
            competitionStats = statsResult.value;
          }
        } catch (error) {
          console.warn('Failed to fetch gamification data:', error);
        }
      }

      // Fetch news
      let news: NewsItem[] = [];
      try {
        news = await apiService.getMarketNews(undefined, 6);
      } catch (error) {
        console.warn('Failed to fetch news:', error);
      }

      // Update state with all fetched data
      updateState({
        portfolio,
        cryptoSymbols: cryptoSymbols.status === 'fulfilled' ? cryptoSymbols.value : [],
        bistSymbols,
        nasdaqSymbols,
        marketStatuses: marketStatuses.status === 'fulfilled' ? marketStatuses.value : [],
        leaderboard,
        userRanking,
        competitionStats,
        news,
        isLoading: false,
      });

    } catch (error) {
      console.error('Failed to initialize dashboard:', error);
      updateState({
        error: 'Dashboard yüklenirken bir hata oluştu.',
        isLoading: false,
      });
    }
  }, [user, updateState]);

  useEffect(() => {
    initializeDashboard();
  }, [initializeDashboard]);

  // Refresh function
  const onRefresh = useCallback(async () => {
    setIsRefreshing(true);
    try {
      await Promise.all([
        refreshPrices(),
        initializeDashboard(),
      ]);
    } catch (error) {
      console.error('Error refreshing dashboard:', error);
    } finally {
      setIsRefreshing(false);
    }
  }, [refreshPrices, initializeDashboard]);

  // Handle section toggle
  const handleSectionToggle = useCallback((assetClass: AssetClassType, expanded: boolean) => {
    updateState({
      expandedSections: {
        ...state.expandedSections,
        [assetClass]: expanded,
      },
    });
  }, [state.expandedSections, updateState]);

  // Navigation handlers
  const handleSymbolPress = useCallback((symbol: EnhancedSymbolDto) => {
    // Navigate to symbol details or trading screen
    console.log('Symbol pressed:', symbol.symbol);
  }, []);

  const handleStrategyTest = useCallback((symbol: EnhancedSymbolDto) => {
    navigation.navigate('StrategyTest', {
      symbol: symbol.symbol,
      displayName: symbol.displayName,
    });
  }, [navigation]);

  const handleAddToWatchlist = useCallback((symbol: EnhancedSymbolDto) => {
    if (!user) {
      Alert.alert(
        'Giriş Gerekli',
        'İzleme listesine eklemek için giriş yapmanız gerekiyor.',
        [
          { text: 'İptal', style: 'cancel' },
          { text: 'Giriş Yap', onPress: () => navigation.navigate('AuthStack', { screen: 'Login' }) },
        ]
      );
      return;
    }

    // Add to watchlist logic here
    console.log('Add to watchlist:', symbol.symbol);
  }, [user, navigation]);

  const handleProfilePress = useCallback(() => {
    navigation.navigate('MainTabs', { screen: 'Profile' });
  }, [navigation]);

  const handleLoginPress = useCallback(() => {
    navigation.navigate('AuthStack', { screen: 'Login', params: { returnTo: 'Dashboard' } });
  }, [navigation]);

  const handleLeaderboardPress = useCallback(() => {
    navigation.navigate('MainTabs', { screen: 'Gamification' });
  }, [navigation]);

  const handleNewsPress = useCallback(() => {
    setShowNewsModal(true);
  }, []);

  const handleNewsItemPress = useCallback((newsItem: NewsItem) => {
    // Navigate to news detail or open URL
    console.log('News item pressed:', newsItem.title);
  }, []);

  const handleChallengePress = useCallback(() => {
    if (!user) {
      Alert.alert(
        'Giriş Gerekli',
        'Yarışmaya katılmak için giriş yapmanız gerekiyor.',
        [
          { text: 'İptal', style: 'cancel' },
          { text: 'Giriş Yap', onPress: () => navigation.navigate('AuthStack', { screen: 'Login' }) },
        ]
      );
      return;
    }

    navigation.navigate('MainTabs', { screen: 'Gamification' });
  }, [user, navigation]);

  // Get market status for specific asset class
  const getMarketStatusForAssetClass = useCallback((assetClass: AssetClassType): MarketStatusDto | undefined => {
    if (!state.marketStatuses || !Array.isArray(state.marketStatuses)) {
      return undefined;
    }
    return state.marketStatuses.find(status => {
      switch (assetClass) {
        case 'CRYPTO':
          return status.marketName.toLowerCase().includes('crypto');
        case 'STOCK':
          return status.marketName.toLowerCase().includes('stock') ||
                 status.marketName.toLowerCase().includes('bist') ||
                 status.marketName.toLowerCase().includes('nasdaq');
        default:
          return false;
      }
    });
  }, [state.marketStatuses]);

  // Get symbols for specific section
  const getSymbolsForSection = useCallback((sectionType: string): EnhancedSymbolDto[] => {
    switch (sectionType) {
      case 'crypto':
        return state.cryptoSymbols;
      case 'bist':
        return state.bistSymbols;
      case 'nasdaq':
        return state.nasdaqSymbols;
      default:
        return [];
    }
  }, [state.cryptoSymbols, state.bistSymbols, state.nasdaqSymbols]);

  // Render accordion sections
  const renderAccordionSections = () => {
    const sections = [
      {
        type: 'crypto',
        assetClass: 'CRYPTO' as AssetClassType,
        title: '🚀 Kripto',
        icon: '🚀',
        symbols: state.cryptoSymbols,
      },
      {
        type: 'bist',
        assetClass: 'STOCK' as AssetClassType,
        title: '🏢 BIST Hisseleri',
        icon: '🏢',
        symbols: state.bistSymbols,
      },
      {
        type: 'nasdaq',
        assetClass: 'STOCK' as AssetClassType,
        title: '🇺🇸 NASDAQ Hisseleri',
        icon: '🇺🇸',
        symbols: state.nasdaqSymbols,
      },
    ];

    return sections.map((section) => {
      const summary = assetClassSummary[section.assetClass];
      const marketStatus = getMarketStatusForAssetClass(section.assetClass);

      return (
        <AccordionErrorBoundary
          key={section.type}
          sectionName={section.title}
        >
          <AssetClassAccordion
            assetClass={section.assetClass}
            title={section.title}
            icon={section.icon}
            symbols={section.symbols}
            marketData={marketDataBySymbol}
            summary={summary}
            marketStatus={marketStatus?.status}
            nextChangeTime={marketStatus?.nextOpen || marketStatus?.nextClose}
            isExpanded={state.expandedSections[section.assetClass]}
            maxVisibleItems={6}
            isLoading={state.isLoading}
            onToggle={handleSectionToggle}
            onSymbolPress={handleSymbolPress}
            onStrategyTest={handleStrategyTest}
            onAddToWatchlist={handleAddToWatchlist}
          />
        </AccordionErrorBoundary>
      );
    });
  };

  // Performance monitoring - simplified to avoid infinite renders
  useEffect(() => {
    startRender();
    return () => {
      endRender();
    };
  }, []); // Empty dependency array - only run on mount/unmount

  // Log performance metrics in development
  useEffect(() => {
    if (__DEV__ && metrics.renderCount > 0 && metrics.renderCount % 50 === 0) {
      console.log('📊 Dashboard Performance Metrics:', {
        renders: metrics.renderCount,
        avgRenderTime: `${metrics.averageRenderTime.toFixed(2)}ms`,
        updates: metrics.updateCount,
        droppedUpdates: metrics.droppedUpdates,
        quality: performanceUtils.getAdaptiveQuality(),
        isActive,
      });
    }
  }, [metrics, performanceUtils, isActive]);

  return (
    <DashboardErrorBoundary>
      <View style={styles.container}>
        <AccordionErrorBoundary sectionName="Genel Bakış">
          <SmartOverviewHeader
            portfolio={state.portfolio}
            marketStatuses={state.marketStatuses || []}
            userRanking={state.userRanking}
            isLoading={state.isLoading}
            onProfilePress={handleProfilePress}
            onLoginPress={handleLoginPress}
          />
        </AccordionErrorBoundary>

        <ScrollView
          style={styles.scrollView}
          showsVerticalScrollIndicator={false}
          removeClippedSubviews={true}
          refreshControl={
            <RefreshControl
              refreshing={isRefreshing}
              onRefresh={onRefresh}
              tintColor="#667eea"
              colors={['#667eea']}
            />
          }
        >
          <View style={styles.content}>
            {/* Asset Class Accordions */}
            {renderAccordionSections()}

            {/* Strategist Competition Section */}
            <AccordionErrorBoundary sectionName="Strategist Yarışması">
              <CompactLeaderboard
                leaderboard={state.leaderboard}
                userRanking={state.userRanking}
                stats={state.competitionStats}
                isLoading={state.isLoading}
                showUserRanking={!!user}
                maxEntries={5}
                onPress={handleLeaderboardPress}
                onChallengePress={handleChallengePress}
                onJoinCompetition={handleChallengePress}
                enablePullToRefresh={true}
                showPeriodTabs={true}
                initialPeriod="weekly"
              />
            </AccordionErrorBoundary>

            {/* Enhanced News Section */}
            <AccordionErrorBoundary sectionName="Piyasa Haberleri">
              <EnhancedNewsPreview
                news={state.news}
                isLoading={state.isLoading}
                maxItems={6}
                showImages={true}
                compact={false}
                showSearch={false}
                showCategories={true}
                showFilters={true}
                onPress={handleNewsPress}
                onNewsItemPress={handleNewsItemPress}
                title="📰 Piyasa Haberleri"
                variant="dashboard"
                enableRealTime={true}
              />
            </AccordionErrorBoundary>

            {/* Performance Debug Info (Development Only) */}
            {__DEV__ && (
              <View style={styles.debugInfo}>
                <Text style={styles.debugText}>
                  Renders: {metrics.renderCount} | Avg: {metrics.averageRenderTime.toFixed(1)}ms | Quality: {performanceUtils.getAdaptiveQuality()}
                </Text>
              </View>
            )}
          </View>
        </ScrollView>
      </View>

      {/* News Modal */}
      <Modal
        visible={showNewsModal}
        animationType="slide"
        presentationStyle="pageSheet"
      >
        <EnhancedNewsScreen
          isModal={true}
          onClose={() => setShowNewsModal(false)}
        />
      </Modal>
    </DashboardErrorBoundary>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  scrollView: {
    flex: 1,
  },
  content: {
    padding: 16,
    paddingBottom: 32,
  },
  debugInfo: {
    backgroundColor: 'rgba(0,0,0,0.05)',
    padding: 8,
    borderRadius: 4,
    marginTop: 16,
  },
  debugText: {
    fontSize: 10,
    color: '#666',
    fontFamily: 'monospace',
    textAlign: 'center',
  },
});

export default DashboardScreen;
