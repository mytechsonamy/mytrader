import React, { memo, useMemo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  Dimensions,
  ScrollView,
} from 'react-native';
import Svg, { Path, Circle, Defs, Pattern, Rect, Line } from 'react-native-svg';
import { UserRanking, LeaderboardEntry } from '../../types';

interface PerformanceDataPoint {
  date: string;
  value: number;
  rank?: number;
  trades?: number;
}

interface PerformanceChartProps {
  userRanking?: UserRanking | null;
  userHistory?: PerformanceDataPoint[];
  leaderboard?: LeaderboardEntry[];
  period: 'weekly' | 'monthly' | 'all';
  showComparison?: boolean;
  compact?: boolean;
}

const { width: SCREEN_WIDTH } = Dimensions.get('window');
const CHART_WIDTH = SCREEN_WIDTH - 40;
const CHART_HEIGHT = 200;

const PerformanceChart: React.FC<PerformanceChartProps> = ({
  userRanking,
  userHistory = [],
  leaderboard = [],
  period,
  showComparison = true,
  compact = false,
}) => {

  // Mock historical data - In real app, this would come from API
  const mockHistoryData: PerformanceDataPoint[] = useMemo(() => {
    const days = period === 'weekly' ? 7 : period === 'monthly' ? 30 : 90;
    const data: PerformanceDataPoint[] = [];
    let currentValue = 10000;
    let currentRank = userRanking?.rank || 50;

    for (let i = days; i >= 0; i--) {
      const date = new Date();
      date.setDate(date.getDate() - i);

      // Simulate some performance variation
      const change = (Math.random() - 0.5) * 200;
      currentValue += change;

      // Simulate rank changes
      const rankChange = Math.floor((Math.random() - 0.5) * 10);
      currentRank = Math.max(1, Math.min(100, currentRank + rankChange));

      data.push({
        date: date.toISOString().split('T')[0],
        value: currentValue,
        rank: currentRank,
        trades: Math.floor(Math.random() * 10) + 1,
      });
    }
    return data;
  }, [period, userRanking?.rank]);

  const historyData = userHistory.length > 0 ? userHistory : mockHistoryData;

  // Calculate chart dimensions and scales
  const chartStats = useMemo(() => {
    if (historyData.length === 0) return null;

    const values = historyData.map(d => d.value);
    const ranks = historyData.map(d => d.rank || 0);

    const minValue = Math.min(...values);
    const maxValue = Math.max(...values);
    const minRank = Math.min(...ranks);
    const maxRank = Math.max(...ranks);

    const valueRange = maxValue - minValue;
    const rankRange = maxRank - minRank;

    return {
      minValue,
      maxValue,
      valueRange,
      minRank,
      maxRank,
      rankRange,
      currentValue: values[values.length - 1],
      startValue: values[0],
      totalReturn: ((values[values.length - 1] - values[0]) / values[0]) * 100,
    };
  }, [historyData]);

  const formatCurrency = (value: number): string => {
    if (value >= 1000000) return `${(value / 1000000).toFixed(1)}M ₺`;
    if (value >= 1000) return `${(value / 1000).toFixed(1)}K ₺`;
    return `${value.toFixed(0)} ₺`;
  };

  const formatPercentage = (percent: number): string => {
    const sign = percent >= 0 ? '+' : '';
    return `${sign}${percent.toFixed(1)}%`;
  };

  const getReturnColor = (percent: number): string => {
    if (percent > 0) return '#10b981';
    if (percent < 0) return '#ef4444';
    return '#6b7280';
  };

  // Generate SVG path for performance line
  const generatePath = (data: PerformanceDataPoint[], useRank = false): string => {
    if (data.length === 0 || !chartStats) return '';

    const { minValue, valueRange, minRank, rankRange } = chartStats;
    const stepX = CHART_WIDTH / (data.length - 1);

    let path = '';

    data.forEach((point, index) => {
      const x = index * stepX;
      let y: number;

      if (useRank && point.rank) {
        // Invert rank scale (lower rank = higher on chart)
        y = CHART_HEIGHT - ((point.rank - minRank) / rankRange) * CHART_HEIGHT;
      } else {
        y = CHART_HEIGHT - ((point.value - minValue) / valueRange) * CHART_HEIGHT;
      }

      if (index === 0) {
        path = `M ${x} ${y}`;
      } else {
        path += ` L ${x} ${y}`;
      }
    });

    return path;
  };

  const renderMiniChart = () => {
    if (!chartStats || historyData.length === 0) return null;

    const performancePath = generatePath(historyData);
    const rankPath = generatePath(historyData, true);

    return (
      <View style={styles.miniChartContainer}>
        <View style={styles.chartHeader}>
          <Text style={styles.chartTitle}>📈 Performans Grafiği</Text>
          <Text style={[
            styles.totalReturn,
            { color: getReturnColor(chartStats.totalReturn) }
          ]}>
            {formatPercentage(chartStats.totalReturn)}
          </Text>
        </View>

        <View style={styles.chartArea}>
          <Svg width={CHART_WIDTH} height={CHART_HEIGHT} style={styles.svgChart}>
            {/* Grid lines */}
            <Defs>
              <Pattern id="grid" width="40" height="40" patternUnits="userSpaceOnUse">
                <Line x1="0" y1="0" x2="0" y2="40" stroke="#f3f4f6" strokeWidth="1"/>
                <Line x1="0" y1="0" x2="40" y2="0" stroke="#f3f4f6" strokeWidth="1"/>
              </Pattern>
            </Defs>
            <Rect width="100%" height="100%" fill="url(#grid)" />

            {/* Performance line */}
            <Path
              d={performancePath}
              fill="none"
              stroke="#667eea"
              strokeWidth="3"
              strokeLinecap="round"
              strokeLinejoin="round"
            />

            {/* Rank line (if showing comparison) */}
            {showComparison && (
              <Path
                d={rankPath}
                fill="none"
                stroke="#f59e0b"
                strokeWidth="2"
                strokeDasharray="5,5"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            )}

            {/* Data points */}
            {historyData.map((point, index) => {
              const x = (index / (historyData.length - 1)) * CHART_WIDTH;
              const y = CHART_HEIGHT - ((point.value - chartStats.minValue) / chartStats.valueRange) * CHART_HEIGHT;

              return (
                <Circle
                  key={index}
                  cx={x}
                  cy={y}
                  r="4"
                  fill="#667eea"
                  stroke="white"
                  strokeWidth="2"
                />
              );
            })}
          </Svg>

          {/* Chart labels */}
          <View style={styles.chartLabels}>
            <View style={styles.leftLabels}>
              <Text style={styles.labelText}>{formatCurrency(chartStats.maxValue)}</Text>
              <Text style={styles.labelText}>{formatCurrency(chartStats.minValue)}</Text>
            </View>
            <View style={styles.bottomLabels}>
              <Text style={styles.labelText}>
                {period === 'weekly' ? '7g önce' : period === 'monthly' ? '30g önce' : '90g önce'}
              </Text>
              <Text style={styles.labelText}>Bugün</Text>
            </View>
          </View>
        </View>

        <View style={styles.chartLegend}>
          <View style={styles.legendItem}>
            <View style={[styles.legendColor, { backgroundColor: '#667eea' }]} />
            <Text style={styles.legendText}>Portföy Değeri</Text>
          </View>
          {showComparison && (
            <View style={styles.legendItem}>
              <View style={[styles.legendColor, { backgroundColor: '#f59e0b', opacity: 0.7 }]} />
              <Text style={styles.legendText}>Sıralama</Text>
            </View>
          )}
        </View>
      </View>
    );
  };

  const renderDetailedChart = () => {
    if (!chartStats) return null;

    return (
      <ScrollView style={styles.detailedContainer} showsVerticalScrollIndicator={false}>
        {/* Summary Stats */}
        <View style={styles.statsGrid}>
          <View style={styles.statCard}>
            <Text style={styles.statValue}>{formatCurrency(chartStats.currentValue)}</Text>
            <Text style={styles.statLabel}>Mevcut Değer</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={[
              styles.statValue,
              { color: getReturnColor(chartStats.totalReturn) }
            ]}>
              {formatPercentage(chartStats.totalReturn)}
            </Text>
            <Text style={styles.statLabel}>Toplam Getiri</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statValue}>#{userRanking?.rank || '-'}</Text>
            <Text style={styles.statLabel}>Mevcut Sıra</Text>
          </View>
        </View>

        {/* Main Chart */}
        {renderMiniChart()}

        {/* Detailed Analytics */}
        <View style={styles.analyticsSection}>
          <Text style={styles.sectionTitle}>📊 Detaylı Analiz</Text>

          <View style={styles.analyticCard}>
            <Text style={styles.analyticTitle}>Volatilite Analizi</Text>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>Günlük Volatilite:</Text>
              <Text style={styles.analyticValue}>±2.4%</Text>
            </View>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>Maksimum Çekilme:</Text>
              <Text style={[styles.analyticValue, { color: '#ef4444' }]}>-5.2%</Text>
            </View>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>Sharpe Oranı:</Text>
              <Text style={styles.analyticValue}>1.84</Text>
            </View>
          </View>

          <View style={styles.analyticCard}>
            <Text style={styles.analyticTitle}>Trading İstatistikleri</Text>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>Toplam İşlem:</Text>
              <Text style={styles.analyticValue}>{historyData.reduce((sum, d) => sum + (d.trades || 0), 0)}</Text>
            </View>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>Günlük Ortalama:</Text>
              <Text style={styles.analyticValue}>
                {(historyData.reduce((sum, d) => sum + (d.trades || 0), 0) / historyData.length).toFixed(1)}
              </Text>
            </View>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>En Aktif Gün:</Text>
              <Text style={styles.analyticValue}>
                {Math.max(...historyData.map(d => d.trades || 0))} işlem
              </Text>
            </View>
          </View>

          <View style={styles.analyticCard}>
            <Text style={styles.analyticTitle}>Sıralama Geçmişi</Text>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>En İyi Sıra:</Text>
              <Text style={[styles.analyticValue, { color: '#10b981' }]}>
                #{Math.min(...historyData.map(d => d.rank || 100))}
              </Text>
            </View>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>Ortalama Sıra:</Text>
              <Text style={styles.analyticValue}>
                #{Math.round(historyData.reduce((sum, d) => sum + (d.rank || 0), 0) / historyData.length)}
              </Text>
            </View>
            <View style={styles.analyticRow}>
              <Text style={styles.analyticLabel}>Sıra Değişimi:</Text>
              <Text style={styles.analyticValue}>
                {(() => {
                  if (historyData.length <= 1) return '-';
                  const first = historyData[0];
                  const last = historyData[historyData.length - 1];
                  if (!first || !last || first.rank === undefined || last.rank === undefined) return '-';
                  const diff = first.rank - last.rank;
                  return `${diff > 0 ? '+' : ''}${diff}`;
                })()}
              </Text>
            </View>
          </View>
        </View>

        {/* Comparison with Top Performers */}
        {showComparison && leaderboard.length > 0 && (
          <View style={styles.comparisonSection}>
            <Text style={styles.sectionTitle}>🏆 Liderlerde Karşılaştırma</Text>
            {leaderboard.slice(0, 3).map((leader, index) => (
              <View key={leader.userId} style={styles.comparisonItem}>
                <View style={styles.comparisonRank}>
                  <Text style={styles.comparisonRankText}>
                    {index === 0 ? '🥇' : index === 1 ? '🥈' : '🥉'}
                  </Text>
                </View>
                <View style={styles.comparisonInfo}>
                  <Text style={styles.comparisonName}>{leader.displayName}</Text>
                  <Text style={styles.comparisonValue}>
                    {formatCurrency(leader.portfolioValue)}
                  </Text>
                </View>
                <View style={styles.comparisonReturn}>
                  <Text style={[
                    styles.comparisonReturnText,
                    { color: getReturnColor(leader.returnPercent) }
                  ]}>
                    {formatPercentage(leader.returnPercent)}
                  </Text>
                </View>
              </View>
            ))}
          </View>
        )}
      </ScrollView>
    );
  };

  if (compact) {
    return renderMiniChart();
  }

  return renderDetailedChart();
};

const styles = StyleSheet.create({
  miniChartContainer: {
    backgroundColor: 'white',
    borderRadius: 16,
    padding: 16,
    marginBottom: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  chartHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
  },
  chartTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1f2937',
  },
  totalReturn: {
    fontSize: 16,
    fontWeight: '700',
  },
  chartArea: {
    position: 'relative',
    marginBottom: 12,
  },
  svgChart: {
    backgroundColor: 'transparent',
  },
  chartLabels: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    pointerEvents: 'none',
  },
  leftLabels: {
    position: 'absolute',
    left: -40,
    top: 0,
    height: CHART_HEIGHT,
    justifyContent: 'space-between',
  },
  bottomLabels: {
    position: 'absolute',
    bottom: -20,
    left: 0,
    right: 0,
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  labelText: {
    fontSize: 10,
    color: '#9ca3af',
  },
  chartLegend: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 20,
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  legendColor: {
    width: 12,
    height: 3,
    borderRadius: 2,
    marginRight: 6,
  },
  legendText: {
    fontSize: 12,
    color: '#6b7280',
  },
  detailedContainer: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  statsGrid: {
    flexDirection: 'row',
    paddingHorizontal: 20,
    paddingVertical: 16,
    gap: 12,
  },
  statCard: {
    flex: 1,
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  statValue: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 4,
  },
  statLabel: {
    fontSize: 12,
    color: '#6b7280',
    textAlign: 'center',
  },
  analyticsSection: {
    paddingHorizontal: 20,
    paddingBottom: 20,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 16,
  },
  analyticCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  analyticTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1f2937',
    marginBottom: 12,
  },
  analyticRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  analyticLabel: {
    fontSize: 14,
    color: '#6b7280',
  },
  analyticValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
  },
  comparisonSection: {
    paddingHorizontal: 20,
    paddingBottom: 20,
  },
  comparisonItem: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    marginBottom: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  comparisonRank: {
    width: 40,
    alignItems: 'center',
    marginRight: 12,
  },
  comparisonRankText: {
    fontSize: 20,
  },
  comparisonInfo: {
    flex: 1,
  },
  comparisonName: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
    marginBottom: 2,
  },
  comparisonValue: {
    fontSize: 12,
    color: '#6b7280',
  },
  comparisonReturn: {
    alignItems: 'flex-end',
  },
  comparisonReturnText: {
    fontSize: 14,
    fontWeight: '600',
  },
});

export default memo(PerformanceChart);