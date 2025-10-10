import React, { memo } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
} from 'react-native';
import { UnifiedMarketDataDto, EnhancedSymbolDto, AssetClassType, SignalType } from '../../types';
import DataSourceIndicator from './DataSourceIndicator';
import { formatLastUpdateWithStatus, formatRelativeTime } from '../../utils/timeFormatting';

interface AssetCardProps {
  symbol: EnhancedSymbolDto;
  marketData?: UnifiedMarketDataDto | null;
  signal?: SignalType;
  isLoading?: boolean;
  compact?: boolean;
  showIndicators?: boolean;
  onPress?: (symbol: EnhancedSymbolDto) => void;
  onStrategyTest?: (symbol: EnhancedSymbolDto) => void;
  onAddToWatchlist?: (symbol: EnhancedSymbolDto) => void;
}

interface AssetCardSkeletonProps {
  compact?: boolean;
}

// Technical indicators (mock data for now)
interface TechnicalIndicators {
  rsi: number;
  macd: number;
  bbUpper: number;
  bbLower: number;
}

const AssetCardSkeleton: React.FC<AssetCardSkeletonProps> = ({ compact = false }) => (
  <View style={[styles.card, compact && styles.compactCard]}>
    <View style={styles.skeletonHeader}>
      <View style={styles.skeletonTitle} />
      <View style={styles.skeletonBadge} />
    </View>
    <View style={styles.skeletonPrice} />
    {!compact && (
      <>
        <View style={styles.skeletonIndicators}>
          <View style={styles.skeletonIndicator} />
          <View style={styles.skeletonIndicator} />
        </View>
        <View style={styles.skeletonButton} />
      </>
    )}
  </View>
);

const AssetCard: React.FC<AssetCardProps> = ({
  symbol,
  marketData,
  signal,
  isLoading = false,
  compact = false,
  showIndicators = true,
  onPress,
  onStrategyTest,
  onAddToWatchlist,
}) => {
  // Mock technical indicators - in real app, these would come from API
  const mockIndicators: TechnicalIndicators = {
    rsi: Math.random() * 100,
    macd: (Math.random() - 0.5) * 2,
    bbUpper: (marketData?.price || 0) * 1.02,
    bbLower: (marketData?.price || 0) * 0.98,
  };

  const formatPrice = (price: number, useFixedDecimals: boolean = true): string => {
    if (price === 0 || price === null || price === undefined) return '--';

    // Always use 2 decimal places for display
    const decimalPlaces = useFixedDecimals ? 2 : (symbol.priceDecimalPlaces || 2);

    // Determine currency based on asset class
    const currency = symbol.assetClassId === 'CRYPTO' ? 'USD' :
                    symbol.quoteCurrency === 'TRY' ? 'TRY' : 'USD';

    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: decimalPlaces,
      maximumFractionDigits: decimalPlaces,
    }).format(price);
  };

  const formatChange = (change: number, changePercent: number): string => {
    const sign = change >= 0 ? '+' : '';
    return `${sign}${change.toFixed(2)} (${sign}${changePercent.toFixed(2)}%)`;
  };

  const getAssetClassIcon = (assetClassId: string): string => {
    switch (assetClassId) {
      case 'CRYPTO': return '🚀';
      case 'STOCK': return symbol.marketId?.includes('BIST') ? '🏢' : '🇺🇸';
      case 'FOREX': return '💱';
      case 'COMMODITY': return '🥇';
      case 'INDEX': return '📊';
      default: return '📈';
    }
  };

  const getSignalColor = (signal?: SignalType): string => {
    switch (signal) {
      case 'BUY': return '#10b981';
      case 'SELL': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getSignalText = (signal?: SignalType): string => {
    switch (signal) {
      case 'BUY': return 'AL';
      case 'SELL': return 'SAT';
      default: return 'NÖTR';
    }
  };

  const getChangeColor = (change: number): string => {
    if (change > 0) return '#10b981';
    if (change < 0) return '#ef4444';
    return '#6b7280';
  };

  const getMarketStatusColor = (status?: string): string => {
    switch (status) {
      case 'OPEN': return '#10b981';
      case 'PRE_MARKET':
      case 'POST_MARKET':
      case 'AFTER_MARKET': return '#f59e0b';
      case 'CLOSED': return '#ef4444';
      case 'HOLIDAY': return '#9ca3af';
      default: return '#6b7280';
    }
  };

  const getMarketStatusText = (status?: string): string => {
    switch (status) {
      case 'OPEN': return 'AÇIK';
      case 'PRE_MARKET': return 'ÖN';
      case 'POST_MARKET':
      case 'AFTER_MARKET': return 'KAPALI';
      case 'CLOSED': return 'KAPALI';
      case 'HOLIDAY': return 'TATİL';
      default: return '';
    }
  };

  if (isLoading) {
    return <AssetCardSkeleton compact={compact} />;
  }

  const handlePress = () => {
    onPress?.(symbol);
  };

  const handleStrategyTest = (e: any) => {
    e?.stopPropagation?.();
    onStrategyTest?.(symbol);
  };

  const handleAddToWatchlist = (e: any) => {
    e?.stopPropagation?.();
    onAddToWatchlist?.(symbol);
  };

  // Debug logging for stocks to diagnose previousClose issue
  if (__DEV__ && symbol.assetClassId === 'STOCK' && marketData) {
    console.log(`[AssetCard] Rendering ${symbol.symbol}:`, {
      hasMarketData: !!marketData,
      price: marketData.price,
      previousClose: marketData.previousClose,
      previousCloseType: typeof marketData.previousClose,
      previousCloseUndefined: marketData.previousClose === undefined,
      previousCloseNull: marketData.previousClose === null,
      willShowPreviousClose: marketData.previousClose !== undefined && marketData.previousClose !== null
    });
  }

  if (compact) {
    return (
      <TouchableOpacity
        style={styles.compactCard}
        onPress={handlePress}
        activeOpacity={0.8}
      >
        <View style={styles.compactHeader}>
          <View style={styles.compactLeft}>
            <Text style={styles.compactIcon}>{getAssetClassIcon(symbol.assetClassId)}</Text>
            <View>
              <Text style={styles.compactSymbol}>{symbol.symbol}</Text>
              <Text style={styles.compactName} numberOfLines={1}>
                {symbol.displayName}
              </Text>
            </View>
          </View>

          <View style={styles.compactRight}>
            {marketData ? (
              <>
                <View style={styles.compactPriceRow}>
                  <Text style={styles.compactPrice}>
                    {formatPrice(marketData.price, true)}
                  </Text>
                  <DataSourceIndicator
                    source={marketData.source}
                    isRealtime={marketData.isRealtime}
                    qualityScore={marketData.qualityScore}
                    size="small"
                    showLabel={false}
                  />
                </View>
                <Text style={[
                  styles.compactChange,
                  { color: getChangeColor(marketData.changePercent || 0) }
                ]}>
                  {`${marketData.changePercent >= 0 ? '+' : ''}${marketData.changePercent.toFixed(2)}%`}
                </Text>
                {/* Previous Close in compact view */}
                {marketData.previousClose !== undefined && marketData.previousClose !== null && (
                  <Text style={styles.compactPreviousClose}>
                    Önc: {formatPrice(marketData.previousClose, true)}
                  </Text>
                )}
              </>
            ) : (
              <>
                <Text style={styles.compactPrice}>--</Text>
                <Text style={styles.compactChange}>--</Text>
              </>
            )}
          </View>
        </View>

        {/* Market Status and Last Update Time Row */}
        <View style={styles.compactFooter}>
          {marketData?.marketStatus && (
            <View style={styles.compactStatusRow}>
              <View style={[
                styles.compactStatusDot,
                { backgroundColor: getMarketStatusColor(marketData.marketStatus) }
              ]} />
              <Text style={styles.compactStatusLabel}>
                {getMarketStatusText(marketData.marketStatus)}
              </Text>
            </View>
          )}

          {marketData?.timestamp && (
            <Text style={styles.compactLastUpdate}>
              {marketData.marketStatus === 'CLOSED'
                ? `Kapalı - ${formatRelativeTime(marketData.timestamp)}`
                : formatRelativeTime(marketData.timestamp)
              }
            </Text>
          )}
        </View>
      </TouchableOpacity>
    );
  }

  return (
    <TouchableOpacity
      style={styles.card}
      onPress={handlePress}
      activeOpacity={0.8}
    >
      <View style={styles.header}>
        <View style={styles.titleRow}>
          <Text style={styles.icon}>{getAssetClassIcon(symbol.assetClassId)}</Text>
          <View style={styles.titleContent}>
            <Text style={styles.symbolText}>{symbol.symbol}</Text>
            <Text style={styles.nameText} numberOfLines={1}>
              {symbol.displayName}
            </Text>
          </View>
        </View>

        <View style={styles.badges}>
          {signal && (
            <View style={[
              styles.signalBadge,
              { backgroundColor: getSignalColor(signal) }
            ]}>
              <Text style={styles.signalText}>{getSignalText(signal)}</Text>
            </View>
          )}

          {marketData?.marketStatus && (
            <View style={[
              styles.statusBadge,
              { backgroundColor: getMarketStatusColor(marketData.marketStatus) }
            ]}>
              <Text style={styles.statusText}>
                {getMarketStatusText(marketData.marketStatus)}
              </Text>
            </View>
          )}
        </View>
      </View>

      <View style={styles.priceSection}>
        {marketData ? (
          <>
            <View style={styles.priceRow}>
              <Text style={styles.price}>
                {formatPrice(marketData.price, true)}
              </Text>
              <DataSourceIndicator
                source={marketData.source}
                isRealtime={marketData.isRealtime}
                qualityScore={marketData.qualityScore}
                size="medium"
                showLabel={true}
              />
            </View>
            <Text style={[
              styles.change,
              { color: getChangeColor(marketData.changePercent || 0) }
            ]}>
              {`${marketData.changePercent >= 0 ? '+' : ''}${marketData.changePercent.toFixed(2)}%`}
            </Text>

            {/* Previous Close Information */}
            {marketData.previousClose !== undefined && marketData.previousClose !== null && (
              <View style={styles.previousCloseContainer}>
                <Text style={styles.previousCloseLabel}>Önceki Kapanış:</Text>
                <Text style={styles.previousCloseValue}>
                  {formatPrice(marketData.previousClose, true)}
                </Text>
              </View>
            )}
          </>
        ) : (
          <>
            <Text style={styles.price}>--</Text>
            <Text style={styles.change}>Veri bekleniyor...</Text>
          </>
        )}
      </View>

      {showIndicators && (
        <View style={styles.indicatorsGrid}>
          <View style={styles.indicator}>
            <Text style={styles.indicatorLabel}>RSI</Text>
            <Text style={styles.indicatorValue}>{mockIndicators.rsi.toFixed(1)}</Text>
          </View>
          <View style={styles.indicator}>
            <Text style={styles.indicatorLabel}>MACD</Text>
            <Text style={styles.indicatorValue}>{mockIndicators.macd.toFixed(3)}</Text>
          </View>
          <View style={styles.indicator}>
            <Text style={styles.indicatorLabel}>BB Üst</Text>
            <Text style={styles.indicatorValue}>
              {formatPrice(mockIndicators.bbUpper, true)}
            </Text>
          </View>
          <View style={styles.indicator}>
            <Text style={styles.indicatorLabel}>BB Alt</Text>
            <Text style={styles.indicatorValue}>
              {formatPrice(mockIndicators.bbLower, true)}
            </Text>
          </View>
        </View>
      )}

      <View style={styles.actions}>
        {onStrategyTest && (
          <TouchableOpacity
            style={styles.strategyButton}
            onPress={handleStrategyTest}
          >
            <Text style={styles.strategyButtonText}>📈 Strateji Test</Text>
          </TouchableOpacity>
        )}

        {onAddToWatchlist && (
          <TouchableOpacity
            style={styles.watchlistButton}
            onPress={handleAddToWatchlist}
          >
            <Text style={styles.watchlistButtonText}>⭐</Text>
          </TouchableOpacity>
        )}
      </View>

      {marketData?.timestamp && (
        <Text style={styles.lastUpdate}>
          {formatLastUpdateWithStatus(marketData.timestamp, marketData.marketStatus)}
        </Text>
      )}
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  card: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 15,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 5,
  },
  compactCard: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 12,
    padding: 12,
    marginBottom: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
    gap: 8,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 12,
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  icon: {
    fontSize: 20,
    marginRight: 8,
  },
  titleContent: {
    flex: 1,
  },
  symbolText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
  },
  nameText: {
    fontSize: 12,
    color: '#666',
    marginTop: 2,
  },
  badges: {
    gap: 4,
  },
  signalBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  signalText: {
    color: 'white',
    fontSize: 10,
    fontWeight: '600',
  },
  statusBadge: {
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 8,
  },
  statusText: {
    color: 'white',
    fontSize: 9,
    fontWeight: '600',
  },
  priceSection: {
    marginBottom: 12,
  },
  priceRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  price: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#2563eb',
    marginRight: 8,
  },
  change: {
    fontSize: 14,
    fontWeight: '600',
  },
  previousCloseContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 6,
    paddingTop: 6,
    borderTopWidth: 1,
    borderTopColor: '#f0f0f0',
  },
  previousCloseLabel: {
    fontSize: 11,
    color: '#64748b',
    marginRight: 6,
  },
  previousCloseValue: {
    fontSize: 12,
    fontWeight: '600',
    color: '#475569',
  },
  indicatorsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    marginBottom: 12,
    gap: 8,
  },
  indicator: {
    backgroundColor: '#f8fafc',
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 8,
    width: '48%',
    alignItems: 'center',
  },
  indicatorLabel: {
    fontSize: 10,
    color: '#64748b',
    marginBottom: 2,
  },
  indicatorValue: {
    fontSize: 12,
    fontWeight: '600',
    color: '#1e293b',
  },
  actions: {
    flexDirection: 'row',
    gap: 8,
    marginBottom: 8,
  },
  strategyButton: {
    flex: 1,
    backgroundColor: '#667eea',
    borderRadius: 8,
    paddingVertical: 10,
    alignItems: 'center',
  },
  strategyButtonText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
  },
  watchlistButton: {
    backgroundColor: '#f59e0b',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    alignItems: 'center',
  },
  watchlistButtonText: {
    fontSize: 16,
  },
  lastUpdate: {
    fontSize: 10,
    color: '#64748b',
    textAlign: 'center',
  },
  compactHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  compactLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  compactIcon: {
    fontSize: 16,
    marginRight: 6,
  },
  compactSymbol: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
  },
  compactName: {
    fontSize: 10,
    color: '#666',
  },
  compactRight: {
    alignItems: 'flex-end',
  },
  compactPriceRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  compactPrice: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#2563eb',
    marginRight: 4,
  },
  compactChange: {
    fontSize: 11,
    fontWeight: '600',
  },
  compactPreviousClose: {
    fontSize: 9,
    color: '#64748b',
    marginTop: 2,
  },
  compactStatusBadge: {
    alignSelf: 'flex-start',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6,
  },
  compactStatusText: {
    color: 'white',
    fontSize: 8,
    fontWeight: '600',
  },
  compactFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: 4,
    borderTopWidth: 1,
    borderTopColor: '#f3f4f6',
  },
  compactStatusRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  compactStatusDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
  },
  compactStatusLabel: {
    fontSize: 10,
    fontWeight: '600',
    color: '#6b7280',
  },
  compactLastUpdate: {
    fontSize: 9,
    color: '#9ca3af',
    fontStyle: 'italic',
  },
  // Skeleton styles
  skeletonHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  skeletonTitle: {
    height: 20,
    width: 120,
    backgroundColor: '#e5e7eb',
    borderRadius: 4,
  },
  skeletonBadge: {
    height: 16,
    width: 40,
    backgroundColor: '#e5e7eb',
    borderRadius: 8,
  },
  skeletonPrice: {
    height: 24,
    width: 80,
    backgroundColor: '#e5e7eb',
    borderRadius: 4,
    marginBottom: 12,
  },
  skeletonIndicators: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  skeletonIndicator: {
    height: 40,
    width: '48%',
    backgroundColor: '#e5e7eb',
    borderRadius: 8,
  },
  skeletonButton: {
    height: 36,
    backgroundColor: '#e5e7eb',
    borderRadius: 8,
  },
});

export default memo(AssetCard);