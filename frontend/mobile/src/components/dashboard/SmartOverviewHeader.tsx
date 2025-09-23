import React, { useMemo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useAuth } from '../../context/AuthContext';
import { usePrices } from '../../context/PriceContext';
import { Portfolio, MarketStatusDto, UserRanking } from '../../types';

interface SmartOverviewHeaderProps {
  portfolio?: Portfolio | null;
  marketStatuses?: MarketStatusDto[] | null;
  userRanking?: UserRanking | null;
  isLoading?: boolean;
  onProfilePress?: () => void;
  onLoginPress?: () => void;
}

interface MarketSentiment {
  overall: 'Bullish' | 'Bearish' | 'Neutral';
  score: number; // -1 to 1
}

const SmartOverviewHeader: React.FC<SmartOverviewHeaderProps> = ({
  portfolio,
  marketStatuses = [],
  userRanking,
  isLoading = false,
  onProfilePress,
  onLoginPress,
}) => {
  const { user } = useAuth();
  const { getAssetClassSummary, connectionStatus, enhancedPrices } = usePrices();

  // Calculate portfolio metrics
  const portfolioMetrics = useMemo((): {
    totalValue: number;
    dailyPnL: number;
    dailyPnLPercent: number;
    topPerformer: {
      symbol: string;
      symbolName: string;
      returnPercent: number;
    } | null;
  } => {
    if (!portfolio) {
      return {
        totalValue: 0,
        dailyPnL: 0,
        dailyPnLPercent: 0,
        topPerformer: null,
      };
    }

    // Find top performing asset from positions
    let topPerformer: {
      symbol: string;
      symbolName: string;
      returnPercent: number;
    } | null = null;
    let bestReturn = -Infinity;

    portfolio.positions?.forEach((position) => {
      if (position.unrealizedPnLPercent > bestReturn) {
        bestReturn = position.unrealizedPnLPercent;
        topPerformer = {
          symbol: position.symbol,
          symbolName: position.symbolName || position.symbol,
          returnPercent: position.unrealizedPnLPercent,
        };
      }
    });

    return {
      totalValue: portfolio.totalValue || 0,
      dailyPnL: portfolio.dailyPnL || 0,
      dailyPnLPercent: portfolio.totalPnLPercent || 0,
      topPerformer,
    };
  }, [portfolio]);

  // Calculate market sentiment
  const marketSentiment = useMemo((): MarketSentiment => {
    const summary = getAssetClassSummary();
    const assetClasses = Object.values(summary);

    if (assetClasses.length === 0) {
      return { overall: 'Neutral', score: 0 };
    }

    // Calculate weighted average change across all asset classes
    const totalSymbols = assetClasses.reduce((sum, ac) => sum + ac.totalSymbols, 0);
    const weightedAverage = assetClasses.reduce((sum, ac) => {
      const weight = ac.totalSymbols / totalSymbols;
      return sum + (ac.averageChange * weight);
    }, 0);

    let overall: 'Bullish' | 'Bearish' | 'Neutral';
    if (weightedAverage > 1) {
      overall = 'Bullish';
    } else if (weightedAverage < -1) {
      overall = 'Bearish';
    } else {
      overall = 'Neutral';
    }

    return {
      overall,
      score: Math.max(-1, Math.min(1, weightedAverage / 5)), // Normalize to -1 to 1
    };
  }, [getAssetClassSummary]);

  // Get market status summary
  const marketStatusSummary = useMemo(() => {
    // Ensure marketStatuses is always an array and handle null/undefined cases
    const validMarketStatuses = Array.isArray(marketStatuses) ? marketStatuses : [];

    const statusCounts = validMarketStatuses.reduce((acc, market) => {
      if (market && market.status) {
        acc[market.status] = (acc[market.status] || 0) + 1;
      }
      return acc;
    }, {} as Record<string, number>);

    return {
      openMarkets: statusCounts['OPEN'] || 0,
      closedMarkets: statusCounts['CLOSED'] || 0,
      preMarkets: statusCounts['PRE_MARKET'] || 0,
      afterMarkets: statusCounts['AFTER_MARKET'] || 0,
      total: validMarketStatuses.length,
    };
  }, [marketStatuses]);

  const formatCurrency = (amount: number): string => {
    if (amount === 0) return '$0.00';

    if (Math.abs(amount) >= 1000000) {
      return `$${(amount / 1000000).toFixed(2)}M`;
    } else if (Math.abs(amount) >= 1000) {
      return `$${(amount / 1000).toFixed(1)}K`;
    }

    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount);
  };

  const formatPercentage = (percent: number): string => {
    const sign = percent >= 0 ? '+' : '';
    return `${sign}${percent.toFixed(2)}%`;
  };

  const getConnectionStatusColor = (): string => {
    switch (connectionStatus) {
      case 'connected': return '#10b981';
      case 'connecting': return '#f59e0b';
      case 'error': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getSentimentColor = (sentiment: string): string => {
    switch (sentiment) {
      case 'Bullish': return '#10b981';
      case 'Bearish': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getPnLColor = (amount: number): string => {
    if (amount > 0) return '#10b981';
    if (amount < 0) return '#ef4444';
    return '#6b7280';
  };

  if (isLoading) {
    return (
      <LinearGradient colors={['#667eea', '#764ba2']} style={styles.container}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="white" />
          <Text style={styles.loadingText}>Yükleniyor...</Text>
        </View>
      </LinearGradient>
    );
  }

  return (
    <LinearGradient colors={['#667eea', '#764ba2']} style={styles.container}>
      {/* Header Row */}
      <View style={styles.headerRow}>
        <View style={styles.titleSection}>
          <Text style={styles.title}>🚀 myTrader</Text>
          <View style={styles.statusRow}>
            <Text style={styles.subtitle}>Canlı Piyasa Analizi</Text>
            <View style={[
              styles.connectionDot,
              { backgroundColor: getConnectionStatusColor() }
            ]} />
          </View>
        </View>

        <TouchableOpacity
          style={styles.userButton}
          onPress={user ? onProfilePress : onLoginPress}
        >
          <Text style={styles.userButtonText}>
            {user ? `👤 ${user.first_name}` : '👤 Giriş'}
          </Text>
        </TouchableOpacity>
      </View>

      {/* Smart Overview Content */}
      <View style={styles.overviewContent}>
        {/* Portfolio Summary */}
        {user && (
          <View style={styles.portfolioRow}>
            <View style={styles.portfolioSummary}>
              <Text style={styles.portfolioLabel}>Portföy Değeri</Text>
              <Text style={styles.portfolioValue}>
                {formatCurrency(portfolioMetrics.totalValue)}
              </Text>
              <Text style={[
                styles.portfolioPnL,
                { color: getPnLColor(portfolioMetrics.dailyPnL) }
              ]}>
                {formatCurrency(portfolioMetrics.dailyPnL)} ({formatPercentage(portfolioMetrics.dailyPnLPercent)})
              </Text>
            </View>

            {portfolioMetrics.topPerformer && (
              <View style={styles.topPerformer}>
                <Text style={styles.topPerformerLabel}>En İyi Varlık</Text>
                <Text style={styles.topPerformerSymbol}>
                  {portfolioMetrics.topPerformer.symbolName}
                </Text>
                <Text style={[
                  styles.topPerformerReturn,
                  { color: getPnLColor(portfolioMetrics.topPerformer.returnPercent) }
                ]}>
                  {formatPercentage(portfolioMetrics.topPerformer.returnPercent)}
                </Text>
              </View>
            )}
          </View>
        )}

        {/* Market Status & Sentiment Row */}
        <View style={styles.marketRow}>
          <View style={styles.marketStatus}>
            <Text style={styles.marketStatusLabel}>Piyasa Durumu</Text>
            <View style={styles.marketStatusGrid}>
              <View style={styles.marketStatusItem}>
                <Text style={styles.marketStatusCount}>{marketStatusSummary.openMarkets}</Text>
                <Text style={styles.marketStatusText}>Açık</Text>
              </View>
              <View style={styles.marketStatusItem}>
                <Text style={styles.marketStatusCount}>{marketStatusSummary.closedMarkets}</Text>
                <Text style={styles.marketStatusText}>Kapalı</Text>
              </View>
            </View>
          </View>

          <View style={styles.sentiment}>
            <Text style={styles.sentimentLabel}>Genel Sentiment</Text>
            <View style={styles.sentimentContent}>
              <Text style={[
                styles.sentimentValue,
                { color: getSentimentColor(marketSentiment.overall) }
              ]}>
                {marketSentiment.overall === 'Bullish' ? 'Yükselişli' :
                 marketSentiment.overall === 'Bearish' ? 'Düşüşlü' : 'Durgun'}
              </Text>
              <View style={styles.sentimentBar}>
                <View style={[
                  styles.sentimentFill,
                  {
                    width: `${Math.abs(marketSentiment.score) * 100}%`,
                    backgroundColor: getSentimentColor(marketSentiment.overall),
                  }
                ]} />
              </View>
            </View>
          </View>
        </View>

        {/* User Ranking (if available) */}
        {user && userRanking && (
          <View style={styles.rankingRow}>
            <Text style={styles.rankingLabel}>Sıralamanız</Text>
            <View style={styles.rankingContent}>
              <Text style={styles.rankingPosition}>#{userRanking.rank}</Text>
              <Text style={styles.rankingTotal}>/ {userRanking.totalParticipants}</Text>
              <Text style={styles.rankingPercentile}>
                %{userRanking.percentile.toFixed(1)} dilim
              </Text>
            </View>
          </View>
        )}
      </View>
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: {
    paddingHorizontal: 20,
    paddingTop: 60,
    paddingBottom: 20,
  },
  loadingContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 40,
  },
  loadingText: {
    color: 'white',
    fontSize: 16,
    marginTop: 10,
  },
  headerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 20,
  },
  titleSection: {
    flex: 1,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: 'white',
  },
  statusRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 4,
  },
  subtitle: {
    fontSize: 14,
    color: 'rgba(255,255,255,0.8)',
    marginRight: 8,
  },
  connectionDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  userButton: {
    backgroundColor: 'rgba(255,255,255,0.2)',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 15,
  },
  userButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  overviewContent: {
    gap: 16,
  },
  portfolioRow: {
    flexDirection: 'row',
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderRadius: 12,
    padding: 16,
    gap: 16,
  },
  portfolioSummary: {
    flex: 2,
  },
  portfolioLabel: {
    fontSize: 12,
    color: 'rgba(255,255,255,0.7)',
    marginBottom: 4,
  },
  portfolioValue: {
    fontSize: 24,
    fontWeight: 'bold',
    color: 'white',
    marginBottom: 4,
  },
  portfolioPnL: {
    fontSize: 14,
    fontWeight: '600',
  },
  topPerformer: {
    flex: 1,
    alignItems: 'flex-end',
  },
  topPerformerLabel: {
    fontSize: 12,
    color: 'rgba(255,255,255,0.7)',
    marginBottom: 4,
  },
  topPerformerSymbol: {
    fontSize: 16,
    fontWeight: '600',
    color: 'white',
    marginBottom: 2,
  },
  topPerformerReturn: {
    fontSize: 14,
    fontWeight: '600',
  },
  marketRow: {
    flexDirection: 'row',
    gap: 12,
  },
  marketStatus: {
    flex: 1,
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderRadius: 12,
    padding: 12,
  },
  marketStatusLabel: {
    fontSize: 12,
    color: 'rgba(255,255,255,0.7)',
    marginBottom: 8,
  },
  marketStatusGrid: {
    flexDirection: 'row',
    justifyContent: 'space-around',
  },
  marketStatusItem: {
    alignItems: 'center',
  },
  marketStatusCount: {
    fontSize: 18,
    fontWeight: 'bold',
    color: 'white',
  },
  marketStatusText: {
    fontSize: 12,
    color: 'rgba(255,255,255,0.8)',
    marginTop: 2,
  },
  sentiment: {
    flex: 1,
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderRadius: 12,
    padding: 12,
  },
  sentimentLabel: {
    fontSize: 12,
    color: 'rgba(255,255,255,0.7)',
    marginBottom: 8,
  },
  sentimentContent: {
    gap: 6,
  },
  sentimentValue: {
    fontSize: 16,
    fontWeight: '600',
  },
  sentimentBar: {
    height: 4,
    backgroundColor: 'rgba(255,255,255,0.2)',
    borderRadius: 2,
    overflow: 'hidden',
  },
  sentimentFill: {
    height: '100%',
  },
  rankingRow: {
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderRadius: 12,
    padding: 12,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  rankingLabel: {
    fontSize: 14,
    color: 'rgba(255,255,255,0.7)',
  },
  rankingContent: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  rankingPosition: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#fbbf24',
  },
  rankingTotal: {
    fontSize: 14,
    color: 'rgba(255,255,255,0.8)',
  },
  rankingPercentile: {
    fontSize: 12,
    color: 'rgba(255,255,255,0.6)',
    marginLeft: 8,
  },
});

export default SmartOverviewHeader;