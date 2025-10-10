import React, { useState, useRef, useCallback, memo } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Animated,
  LayoutAnimation,
  Platform,
  UIManager,
} from 'react-native';
import { AssetClassType, EnhancedSymbolDto, UnifiedMarketDataDto } from '../../types';
import AssetCard from './AssetCard';
import { MarketStatusBadge } from './MarketStatusIndicator';
import { createTimingAnimation, runSafeAnimation } from '../../utils/animationUtils';
import { useTheme } from '../../context/ThemeContext';

// Enable LayoutAnimation for Android
if (Platform.OS === 'android' && UIManager.setLayoutAnimationEnabledExperimental) {
  UIManager.setLayoutAnimationEnabledExperimental(true);
}

interface AssetClassSummary {
  totalSymbols: number;
  gainers: number;
  losers: number;
  averageChange: number;
}

interface AssetClassAccordionProps {
  assetClass: AssetClassType;
  title: string;
  icon: string;
  symbols: EnhancedSymbolDto[];
  marketData: Record<string, UnifiedMarketDataDto>;
  summary?: AssetClassSummary;
  marketStatus?: 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET' | 'POST_MARKET' | 'HOLIDAY';
  nextChangeTime?: string;
  isExpanded?: boolean;
  defaultExpanded?: boolean;
  maxVisibleItems?: number;
  showLoadMore?: boolean;
  isLoading?: boolean;
  onToggle?: (assetClass: AssetClassType, expanded: boolean) => void;
  onSymbolPress?: (symbol: EnhancedSymbolDto) => void;
  onStrategyTest?: (symbol: EnhancedSymbolDto) => void;
  onAddToWatchlist?: (symbol: EnhancedSymbolDto) => void;
  onLoadMore?: (assetClass: AssetClassType) => void;
}

interface AccordionHeaderProps {
  assetClass: AssetClassType;
  title: string;
  icon: string;
  summary?: AssetClassSummary;
  marketStatus?: 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET' | 'POST_MARKET' | 'HOLIDAY';
  nextChangeTime?: string;
  lastUpdateTime?: string;
  isExpanded: boolean;
  isLoading?: boolean;
  symbolsCount?: number;
  onPress: () => void;
}

const AccordionHeader: React.FC<AccordionHeaderProps> = memo(({
  assetClass,
  title,
  icon,
  summary,
  marketStatus,
  nextChangeTime,
  lastUpdateTime,
  isExpanded,
  isLoading,
  symbolsCount,
  onPress,
}) => {
  const { colors } = useTheme();
  const rotateAnim = useRef(new Animated.Value(isExpanded ? 1 : 0)).current;

  React.useEffect(() => {
    const animation = createTimingAnimation(rotateAnim, {
      toValue: isExpanded ? 1 : 0,
      duration: 200,
      useNativeDriver: true,
    });
    runSafeAnimation(animation);
  }, [isExpanded, rotateAnim]);

  const getSummaryColor = (change: number): string => {
    if (change > 0) return '#10b981';
    if (change < 0) return '#ef4444';
    return '#6b7280';
  };

  const formatSummaryText = (isLoading?: boolean): string => {
    if (isLoading) return 'Yükleniyor...';
    if (!summary || (summary.totalSymbols === 0 && (!symbolsCount || symbolsCount === 0))) return 'Veri yok';

    // If summary is invalid but we have symbols, show symbol count
    if (summary.totalSymbols === 0 && symbolsCount && symbolsCount > 0) {
      return `${symbolsCount} varlık`;
    }

    const { gainers, losers, averageChange } = summary;
    const sign = averageChange >= 0 ? '+' : '';
    return `${gainers}↗ ${losers}↘ Ort: ${sign}${averageChange.toFixed(2)}%`;
  };

  return (
    <TouchableOpacity
      style={[styles.header, { backgroundColor: colors.surface }]}
      onPress={onPress}
      activeOpacity={0.8}
    >
      <View style={styles.headerLeft}>
        <Text style={styles.headerIcon}>{icon}</Text>
        <View style={styles.headerContent}>
          <Text style={[styles.headerTitle, { color: colors.text }]}>{title}</Text>
          <View style={styles.headerSummary}>
            <Text style={[
              styles.summaryText,
              { color: isLoading ? colors.textSecondary : getSummaryColor(summary?.averageChange || 0) }
            ]}>
              {formatSummaryText(isLoading)}
            </Text>
            {summary && summary.totalSymbols > 0 && (
              <Text style={[styles.symbolCount, { color: colors.textTertiary }]}>
                {summary.totalSymbols} varlık
              </Text>
            )}
          </View>
        </View>
      </View>

      <View style={styles.headerRight}>
        {marketStatus && (
          <MarketStatusBadge
            assetClass={assetClass}
            status={marketStatus}
            nextChangeTime={nextChangeTime}
            lastUpdateTime={lastUpdateTime}
            compact={true}
            size="small"
          />
        )}

        <Animated.View
          style={[
            styles.chevron,
            {
              transform: [{
                rotate: rotateAnim.interpolate({
                  inputRange: [0, 1],
                  outputRange: ['0deg', '180deg'],
                }),
              }],
            },
          ]}
        >
          <Text style={[styles.chevronText, { color: colors.textSecondary }]}>▼</Text>
        </Animated.View>
      </View>
    </TouchableOpacity>
  );
});

const AssetClassAccordion: React.FC<AssetClassAccordionProps> = ({
  assetClass,
  title,
  icon,
  symbols,
  marketData,
  summary,
  marketStatus,
  nextChangeTime,
  isExpanded: controlledExpanded,
  defaultExpanded = false,
  maxVisibleItems = 6,
  showLoadMore = false,
  isLoading = false,
  onToggle,
  onSymbolPress,
  onStrategyTest,
  onAddToWatchlist,
  onLoadMore,
}) => {
  const { colors } = useTheme();
  const [internalExpanded, setInternalExpanded] = useState(defaultExpanded);
  const [showAll, setShowAll] = useState(false);

  const isExpanded = controlledExpanded !== undefined ? controlledExpanded : internalExpanded;

  // Calculate most recent update time from market data
  const lastUpdateTime = React.useMemo(() => {
    const timestamps = Object.values(marketData)
      .map((data: UnifiedMarketDataDto) => data?.timestamp || data?.lastUpdated)
      .filter((ts): ts is string => Boolean(ts));

    if (timestamps.length === 0) return undefined;

    // Return the most recent timestamp
    return timestamps.reduce((latest, current) => {
      return new Date(current) > new Date(latest) ? current : latest;
    });
  }, [marketData]);

  const handleToggle = useCallback(() => {
    LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);

    if (controlledExpanded === undefined) {
      setInternalExpanded(!internalExpanded);
    }

    onToggle?.(assetClass, !isExpanded);
  }, [assetClass, controlledExpanded, internalExpanded, isExpanded, onToggle]);

  const handleShowMore = useCallback(() => {
    if (symbols.length > maxVisibleItems && !showAll) {
      setShowAll(true);
    } else if (onLoadMore) {
      onLoadMore(assetClass);
    }
  }, [symbols.length, maxVisibleItems, showAll, onLoadMore, assetClass]);

  const visibleSymbols = React.useMemo(() => {
    if (!isExpanded) return [];

    if (showAll) return symbols;

    return symbols.slice(0, maxVisibleItems);
  }, [symbols, isExpanded, showAll, maxVisibleItems]);

  const needsLoadMore = symbols.length > maxVisibleItems && !showAll;
  const hasMore = showLoadMore && symbols.length >= maxVisibleItems;

  const renderSymbolCard = useCallback((symbol: EnhancedSymbolDto, index: number) => {
    // Try comprehensive lookup strategies to find market data
    // Priority order: exact ID -> symbol variations -> with USDT suffix
    const lookupKeys = [
      symbol.id,                                          // UUID
      symbol.symbol,                                      // BTC
      symbol.symbol?.toLowerCase(),                       // btc
      symbol.symbol?.toUpperCase(),                       // BTC
      `${symbol.symbol}USDT`,                            // BTCUSDT
      `${symbol.symbol?.toLowerCase()}usdt`,             // btcusdt
      `${symbol.symbol?.toUpperCase()}USDT`,             // BTCUSDT
      symbol.baseCurrency,                               // BTC
      symbol.baseCurrency?.toLowerCase(),                // btc
      `${symbol.baseCurrency}${symbol.quoteCurrency}`,   // BTCUSDT
      `${symbol.baseCurrency?.toLowerCase()}${symbol.quoteCurrency?.toLowerCase()}`, // btcusdt
    ].filter(Boolean);

    let symbolMarketData = undefined;
    for (const key of lookupKeys) {
      if (marketData[key as string]) {
        symbolMarketData = marketData[key as string];
        break;
      }
    }

    // Debug: Log when market data is missing (only occasionally to reduce noise)
    if (!symbolMarketData && __DEV__) {
      console.warn(`[AssetClassAccordion] No market data found for symbol:`, {
        id: symbol.id,
        symbol: symbol.symbol,
        triedKeys: lookupKeys.slice(0, 5),
        availableKeys: Object.keys(marketData).slice(0, 5),
      });
    }

    return (
      <AssetCard
        key={`${symbol.id}-${index}`}
        symbol={symbol}
        marketData={symbolMarketData}
        compact={true}
        showIndicators={false}
        isLoading={isLoading}
        onPress={onSymbolPress}
        onStrategyTest={onStrategyTest}
        onAddToWatchlist={onAddToWatchlist}
      />
    );
  }, [marketData, isLoading, onSymbolPress, onStrategyTest, onAddToWatchlist]);

  const renderLoadingCards = () => {
    return Array.from({ length: 3 }, (_, index) => (
      <AssetCard
        key={`loading-${index}`}
        symbol={{} as EnhancedSymbolDto}
        compact={true}
        showIndicators={false}
        isLoading={true}
      />
    ));
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.card }]}>
      <AccordionHeader
        assetClass={assetClass}
        title={title}
        icon={icon}
        summary={summary}
        marketStatus={marketStatus}
        nextChangeTime={nextChangeTime}
        lastUpdateTime={lastUpdateTime}
        isExpanded={isExpanded}
        isLoading={isLoading}
        symbolsCount={symbols.length}
        onPress={handleToggle}
      />

      {isExpanded && (
        <View style={styles.content}>
          {isLoading ? (
            renderLoadingCards()
          ) : symbols.length === 0 ? (
            <View style={styles.emptyState}>
              <Text style={styles.emptyIcon}>📭</Text>
              <Text style={[styles.emptyText, { color: colors.textSecondary }]}>Bu kategoride varlık bulunamadı</Text>
            </View>
          ) : (
            <>
              {visibleSymbols.map(renderSymbolCard)}

              {(needsLoadMore || hasMore) && (
                <TouchableOpacity
                  style={[styles.loadMoreButton, { backgroundColor: colors.surface }]}
                  onPress={handleShowMore}
                  activeOpacity={0.8}
                >
                  <Text style={[styles.loadMoreText, { color: colors.primary }]}>
                    {needsLoadMore
                      ? `${symbols.length - maxVisibleItems} tane daha göster`
                      : 'Daha fazla yükle'
                    }
                  </Text>
                  <Text style={[styles.loadMoreIcon, { color: colors.primary }]}>↓</Text>
                </TouchableOpacity>
              )}
            </>
          )}
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: 'rgba(255,255,255,0.95)',
    borderRadius: 15,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
    overflow: 'hidden',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    backgroundColor: 'rgba(255,255,255,0.95)',
  },
  headerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  headerIcon: {
    fontSize: 24,
    marginRight: 12,
  },
  headerContent: {
    flex: 1,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 4,
  },
  headerSummary: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  summaryText: {
    fontSize: 12,
    fontWeight: '600',
  },
  symbolCount: {
    fontSize: 11,
    color: '#6b7280',
    backgroundColor: '#f3f4f6',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 8,
  },
  headerRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  chevron: {
    padding: 4,
  },
  chevronText: {
    fontSize: 12,
    color: '#6b7280',
    fontWeight: '600',
  },
  content: {
    paddingHorizontal: 16,
    paddingBottom: 16,
    backgroundColor: '#f8fafc',
  },
  emptyState: {
    alignItems: 'center',
    paddingVertical: 32,
    paddingHorizontal: 16,
  },
  emptyIcon: {
    fontSize: 48,
    marginBottom: 12,
  },
  emptyText: {
    fontSize: 14,
    color: '#6b7280',
    textAlign: 'center',
  },
  loadMoreButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(102, 126, 234, 0.1)',
    borderRadius: 12,
    paddingVertical: 12,
    paddingHorizontal: 16,
    marginTop: 8,
    gap: 8,
  },
  loadMoreText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#667eea',
  },
  loadMoreIcon: {
    fontSize: 12,
    color: '#667eea',
  },
});

export default memo(AssetClassAccordion);