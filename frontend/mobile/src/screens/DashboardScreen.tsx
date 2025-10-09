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
import { useTheme } from '../context/ThemeContext';
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
  nyseSymbols: EnhancedSymbolDto[];
  marketStatuses: MarketStatusDto[];
  leaderboard: LeaderboardEntry[];
  userRanking: UserRanking | null;
  competitionStats: CompetitionStats | null;
  news: NewsItem[];
  expandedSections: Record<string, boolean>; // Changed from AssetClassType to string for unique section keys
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
  const { colors } = useTheme();
  const {
    enhancedPrices,
    getSymbolsByAssetClass,
    getAssetClassSummary,
    refreshPrices,
    connectionStatus,
    isLoading: priceDataLoading,
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
    { type: 'CRYPTO', title: 'Kripto', icon: '🚀', priority: 1 },
    { type: 'STOCK', title: 'BIST Hisseleri', icon: '🏢', priority: 2 },
    { type: 'STOCK', title: 'NASDAQ Hisseleri', icon: '🇺🇸', priority: 3 },
  ];

  const [state, setState] = useState<DashboardState>({
    portfolio: null,
    cryptoSymbols: [],
    bistSymbols: [],
    nasdaqSymbols: [],
    nyseSymbols: [],
    marketStatuses: [],
    leaderboard: [],
    userRanking: null,
    competitionStats: null,
    news: [],
    expandedSections: {
      crypto: true,      // Each section has unique key now
      bist: false,
      nasdaq: false,
      nyse: false,
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

  // Market data organized by symbol ID with UUID mapping
  const marketDataBySymbol = useMemo(() => {
    const data: Record<string, UnifiedMarketDataDto> = {};

    // Index all price data by ticker and symbolId
    if (enhancedPrices && typeof enhancedPrices === 'object') {
      Object.entries(enhancedPrices).forEach(([key, marketData]) => {
        if (marketData && key) {
          data[key] = marketData;
          if (marketData.symbol) {
            data[marketData.symbol] = marketData;
          }
          if (marketData.symbolId) {
            data[marketData.symbolId] = marketData;
          }
        }
      });
    }

    // Map symbol UUIDs to their price data by ticker
    // This fixes the lookup issue where AssetClassAccordion looks up by UUID
    const allSymbols = [
      ...state.cryptoSymbols,
      ...state.bistSymbols,
      ...state.nasdaqSymbols,
      ...state.nyseSymbols
    ];

    allSymbols.forEach(symbol => {
      const priceData = data[symbol.symbol];
      if (priceData && symbol.id) {
        data[symbol.id] = priceData; // Map UUID → price data
      }
    });

    return data;
  }, [enhancedPrices, state.cryptoSymbols, state.bistSymbols, state.nasdaqSymbols, state.nyseSymbols]);

  // DEBUG: Log when enhancedPrices changes
  useEffect(() => {
    const priceCount = Object.keys(enhancedPrices).length;
    if (priceCount > 0) {
      const samplePrices = Object.entries(enhancedPrices).slice(0, 2).map(([id, data]) =>
        `${id}: $${data.price}`
      );
      console.log(`[Dashboard] Enhanced prices updated: ${priceCount} symbols`, samplePrices);
    }
  }, [enhancedPrices]);

  // DEBUG: Log stock lookup test
  useEffect(() => {
    if (Object.keys(marketDataBySymbol).length > 0) {
      const sampleStock = state.bistSymbols[0] || state.nasdaqSymbols[0];
      if (sampleStock) {
        console.log('[Dashboard] Stock lookup test:', {
          symbolId: sampleStock.id,
          ticker: sampleStock.symbol,
          foundById: !!marketDataBySymbol[sampleStock.id],
          foundByTicker: !!marketDataBySymbol[sampleStock.symbol],
          availableKeys: Object.keys(marketDataBySymbol).filter(k =>
            marketDataBySymbol[k]?.assetClass === 'STOCK'
          ).slice(0, 5)
        });
      }
    }
  }, [marketDataBySymbol, state.bistSymbols, state.nasdaqSymbols]);

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

      // Fetch symbols by asset class - backend now returns correct symbols dynamically
      const [cryptoSymbolsResult, marketStatuses] = await Promise.allSettled([
        apiService.getSymbolsByAssetClass('CRYPTO'),
        apiService.getAllMarketStatuses(),
      ]);

      // Trust the API response - no filtering needed
      // Backend manages which symbols to return via symbol preferences
      const cryptoSymbols = cryptoSymbolsResult.status === 'fulfilled'
        ? cryptoSymbolsResult.value
        : [];

      if (cryptoSymbols.length > 0) {
        console.log(`[Dashboard] Loaded ${cryptoSymbols.length} crypto symbols:`, cryptoSymbols.map(s => s.symbol).join(', '));
      }

      // Fetch all stock symbols once and filter by marketName
      let bistSymbols: EnhancedSymbolDto[] = [];
      let nasdaqSymbols: EnhancedSymbolDto[] = [];
      let nyseSymbols: EnhancedSymbolDto[] = [];

      try {
        const allStocks = await apiService.getSymbolsByAssetClass('STOCK');
        console.log(`[Dashboard] Loaded ${allStocks.length} total stock symbols`);

        // Filter by market field (check both marketName and market for compatibility)
        bistSymbols = (allStocks || []).filter(symbol => {
          const marketValue = (symbol?.marketName || symbol?.market || '').toUpperCase();
          return marketValue === 'BIST';
        });

        nasdaqSymbols = (allStocks || []).filter(symbol => {
          const marketValue = (symbol?.marketName || symbol?.market || '').toUpperCase();
          return marketValue === 'NASDAQ';
        });

        nyseSymbols = (allStocks || []).filter(symbol => {
          const marketValue = (symbol?.marketName || symbol?.market || '').toUpperCase();
          return marketValue === 'NYSE';
        });

        console.log(`[Dashboard] Filtered stocks - BIST: ${bistSymbols.length}, NASDAQ: ${nasdaqSymbols.length}, NYSE: ${nyseSymbols.length}`);
      } catch (error) {
        console.warn('Failed to fetch stock symbols:', error);
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
        cryptoSymbols,
        bistSymbols,
        nasdaqSymbols,
        nyseSymbols,
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

  // Handle section toggle - now uses section type (string) instead of AssetClassType
  const handleSectionToggle = useCallback((sectionType: string, expanded: boolean) => {
    updateState({
      expandedSections: {
        ...state.expandedSections,
        [sectionType]: expanded,
      },
    });
  }, [state.expandedSections, updateState]);

  // Navigation handlers
  const handleSymbolPress = useCallback((symbol: EnhancedSymbolDto) => {
    navigation.navigate('StrategyTest', {
      symbol: symbol.symbol,
      displayName: symbol.displayName,
    });
  }, [navigation]);

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
      // Add null/undefined checks for marketName
      if (!status || !status.marketName || typeof status.marketName !== 'string') {
        return false;
      }

      const marketNameLower = status.marketName.toLowerCase();
      switch (assetClass) {
        case 'CRYPTO':
          return marketNameLower.includes('crypto');
        case 'STOCK':
          return marketNameLower.includes('stock') ||
                 marketNameLower.includes('bist') ||
                 marketNameLower.includes('nasdaq');
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
      case 'nyse':
        return state.nyseSymbols;
      default:
        return [];
    }
  }, [state.cryptoSymbols, state.bistSymbols, state.nasdaqSymbols, state.nyseSymbols]);

  // Render accordion sections
  const renderAccordionSections = () => {
    const sections = [
      {
        type: 'crypto',
        assetClass: 'CRYPTO' as AssetClassType,
        title: 'Kripto',
        icon: '🚀',
        symbols: state.cryptoSymbols,
      },
      {
        type: 'bist',
        assetClass: 'STOCK' as AssetClassType,
        title: 'BIST Hisseleri',
        icon: '🏢',
        symbols: state.bistSymbols,
      },
      {
        type: 'nasdaq',
        assetClass: 'STOCK' as AssetClassType,
        title: 'NASDAQ Hisseleri',
        icon: '🇺🇸',
        symbols: state.nasdaqSymbols,
      },
      {
        type: 'nyse',
        assetClass: 'STOCK' as AssetClassType,
        title: 'NYSE Hisseleri',
        icon: '🗽',
        symbols: state.nyseSymbols,
      },
    ];

    return sections.map((section) => {
      console.log('marketDataBySymbol', marketDataBySymbol);
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
            isExpanded={state.expandedSections[section.type]}
            maxVisibleItems={6}
            isLoading={state.isLoading || priceDataLoading}
            onToggle={(_, expanded) => handleSectionToggle(section.type, expanded)}
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
      <View style={[styles.container, { backgroundColor: colors.background }]}>
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
              tintColor={colors.primary}
              colors={[colors.primary]}
            />
          }
        >
          <View style={styles.content}>
            {/* Asset Class Accordions */}
            {renderAccordionSections()}

            {/* Strategist Competition Section */}
            <AccordionErrorBoundary sectionName="Strategist Yarışması">
              <CompactLeaderboard
                leaderboard={Array.isArray(state.leaderboard) ? state.leaderboard : []}
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
                news={Array.isArray(state.news) ? state.news : []}
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
              <View style={[styles.debugInfo, { backgroundColor: colors.surface }]}>
                <Text style={[styles.debugText, { color: colors.textSecondary }]}>
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
  },
  scrollView: {
    flex: 1,
  },
  content: {
    padding: 16,
    paddingBottom: 32,
    flex: 1,
  },
  debugInfo: {
    padding: 8,
    borderRadius: 4,
    marginTop: 16,
  },
  debugText: {
    fontSize: 10,
    fontFamily: 'monospace',
    textAlign: 'center',
  },
});

export default DashboardScreen;
