import React, { memo, useState, useCallback, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  Animated,
  RefreshControl,
} from 'react-native';
import { LeaderboardEntry, UserRanking, CompetitionStats } from '../../types';
import { apiService } from '../../services/api';

interface CompactLeaderboardProps {
  leaderboard: LeaderboardEntry[];
  userRanking?: UserRanking | null;
  stats?: CompetitionStats | null;
  isLoading?: boolean;
  showUserRanking?: boolean;
  maxEntries?: number;
  onPress?: () => void;
  onUserPress?: (userId: string) => void;
  onChallengePress?: () => void;
  onJoinCompetition?: () => void;
  enablePullToRefresh?: boolean;
  showPeriodTabs?: boolean;
  initialPeriod?: 'weekly' | 'all';
}

interface LeaderboardRowProps {
  entry: LeaderboardEntry;
  isCurrentUser?: boolean;
  onPress?: (userId: string) => void;
}

const LeaderboardRow: React.FC<LeaderboardRowProps> = memo(({
  entry,
  isCurrentUser = false,
  onPress,
}) => {
  const getTierColor = (tier: string): string => {
    switch (tier) {
      case 'DIAMOND': return '#a855f7';
      case 'PLATINUM': return '#06b6d4';
      case 'GOLD': return '#eab308';
      case 'SILVER': return '#64748b';
      case 'BRONZE': return '#dc2626';
      default: return '#6b7280';
    }
  };

  const getTierIcon = (tier: string): string => {
    switch (tier) {
      case 'DIAMOND': return '💎';
      case 'PLATINUM': return '🏆';
      case 'GOLD': return '🥇';
      case 'SILVER': return '🥈';
      case 'BRONZE': return '🥉';
      default: return '🏅';
    }
  };

  const getRankIcon = (rank: number): string => {
    switch (rank) {
      case 1: return '👑';
      case 2: return '🥈';
      case 3: return '🥉';
      default: return `#${rank}`;
    }
  };

  const formatCurrency = (amount: number | undefined | null): string => {
    if (amount === undefined || amount === null || isNaN(amount)) return '$0';
    if (amount >= 1000000) {
      return `$${(amount / 1000000).toFixed(1)}M`;
    } else if (amount >= 1000) {
      return `$${(amount / 1000).toFixed(1)}K`;
    }
    return `$${amount.toFixed(0)}`;
  };

  const formatPercentage = (percent: number | undefined | null): string => {
    if (percent === undefined || percent === null || isNaN(percent)) return '0.0%';
    const sign = percent >= 0 ? '+' : '';
    return `${sign}${percent.toFixed(1)}%`;
  };

  const getReturnColor = (returnPercent: number): string => {
    if (returnPercent > 0) return '#10b981';
    if (returnPercent < 0) return '#ef4444';
    return '#6b7280';
  };

  return (
    <TouchableOpacity
      style={[
        styles.leaderboardRow,
        isCurrentUser && styles.currentUserRow,
      ]}
      onPress={() => onPress?.(entry.userId)}
      activeOpacity={0.8}
    >
      <View style={styles.rankSection}>
        <Text style={[
          styles.rankText,
          entry.rank <= 3 && styles.topRankText,
        ]}>
          {typeof getRankIcon(entry.rank) === 'string' && getRankIcon(entry.rank).startsWith('#')
            ? getRankIcon(entry.rank)
            : getRankIcon(entry.rank)
          }
        </Text>
      </View>

      <View style={styles.userSection}>
        <View style={styles.userInfo}>
          <Text style={[
            styles.userName,
            isCurrentUser && styles.currentUserName,
          ]} numberOfLines={1}>
            {entry.displayName}
          </Text>
          <View style={styles.userBadges}>
            <Text style={styles.tierIcon}>{getTierIcon(entry.tier)}</Text>
            {entry.badges.length > 0 && (
              <View style={styles.badgeCount}>
                <Text style={styles.badgeCountText}>{entry.badges.length}</Text>
              </View>
            )}
          </View>
        </View>

        <View style={styles.statsSection}>
          <Text style={styles.portfolioValue}>
            {formatCurrency(entry.portfolioValue)}
          </Text>
          <Text style={[
            styles.returnPercent,
            { color: getReturnColor(entry.returnPercent) }
          ]}>
            {formatPercentage(entry.returnPercent)}
          </Text>
        </View>
      </View>

      <View style={styles.metricsSection}>
        <Text style={styles.winRate}>{entry.winRate ? entry.winRate.toFixed(0) : '0'}%</Text>
        <Text style={styles.trades}>{entry.totalTrades} işlem</Text>
      </View>
    </TouchableOpacity>
  );
});

const CompactLeaderboard: React.FC<CompactLeaderboardProps> = ({
  leaderboard: initialLeaderboard,
  userRanking: initialUserRanking,
  stats: initialStats,
  isLoading = false,
  showUserRanking = true,
  maxEntries = 5,
  onPress,
  onUserPress,
  onChallengePress,
  onJoinCompetition,
  enablePullToRefresh = true,
  showPeriodTabs = true,
  initialPeriod = 'weekly',
}) => {
  // Local state for tab switching and data management
  const [activePeriod, setActivePeriod] = useState<'weekly' | 'all'>(initialPeriod);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>(initialLeaderboard);
  const [userRanking, setUserRanking] = useState<UserRanking | null>(initialUserRanking ?? null);
  const [stats, setStats] = useState<CompetitionStats | null>(initialStats ?? null);
  const [localLoading, setLocalLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [fadeAnim] = useState(new Animated.Value(1));
  const [scrollToUserEnabled, setScrollToUserEnabled] = useState(false);

  // Animation for smooth transitions
  const animateTransition = useCallback(() => {
    Animated.sequence([
      Animated.timing(fadeAnim, {
        toValue: 0.5,
        duration: 150,
        useNativeDriver: true,
      }),
      Animated.timing(fadeAnim, {
        toValue: 1,
        duration: 150,
        useNativeDriver: true,
      }),
    ]).start();
  }, [fadeAnim]);

  // Fetch data for specific period
  const fetchLeaderboardData = useCallback(async (period: 'weekly' | 'all', showAnimation = true) => {
    if (showAnimation) {
      setLocalLoading(true);
      animateTransition();
    }

    try {
      const [leaderboardData, userRankingData, statsData] = await Promise.allSettled([
        apiService.getLeaderboard(period, maxEntries + 5), // Get a few extra for better UX
        apiService.getUserRanking(period),
        apiService.getCompetitionStats(),
      ]);

      if (leaderboardData.status === 'fulfilled') {
        setLeaderboard(leaderboardData.value);
      }
      if (userRankingData.status === 'fulfilled') {
        setUserRanking(userRankingData.value);
      }
      if (statsData.status === 'fulfilled') {
        setStats(statsData.value);
      }
    } catch (error) {
      console.warn('Failed to fetch leaderboard data:', error);
    } finally {
      setLocalLoading(false);
    }
  }, [maxEntries, animateTransition]);

  // Handle period change
  const handlePeriodChange = useCallback(async (period: 'weekly' | 'all') => {
    if (period === activePeriod) return;

    setActivePeriod(period);
    await fetchLeaderboardData(period);
  }, [activePeriod, fetchLeaderboardData]);

  // Handle refresh
  const handleRefresh = useCallback(async () => {
    setRefreshing(true);
    await fetchLeaderboardData(activePeriod, false);
    setRefreshing(false);
  }, [activePeriod, fetchLeaderboardData]);

  // Update when props change
  useEffect(() => {
    setLeaderboard(initialLeaderboard);
    setUserRanking(initialUserRanking ?? null);
    setStats(initialStats ?? null);
  }, [initialLeaderboard, initialUserRanking, initialStats]);

  // Enhanced user ranking with rank change indication
  const getUserRankChange = useCallback((): 'up' | 'down' | 'same' | null => {
    if (!userRanking?.rank) return null;
    // This would ideally come from the API with previous rank data
    // For now, we'll simulate it based on performance
    if (userRanking.returnPercent > 5) return 'up';
    if (userRanking.returnPercent < -2) return 'down';
    return 'same';
  }, [userRanking]);

  const getRankChangeIcon = (change: 'up' | 'down' | 'same' | null): string => {
    switch (change) {
      case 'up': return '📈';
      case 'down': return '📉';
      case 'same': return '➡️';
      default: return '';
    }
  };
  const displayedEntries = leaderboard.slice(0, maxEntries);
  const currentUserId = userRanking?.userId;
  const rankChange = getUserRankChange();
  const isCurrentlyLoading = isLoading || localLoading;

  const formatTimeRemaining = (): string => {
    if (!stats?.currentPeriodEnd) return '';

    const endDate = new Date(stats.currentPeriodEnd);
    const now = new Date();
    const diff = endDate.getTime() - now.getTime();

    if (diff <= 0) return 'Süre doldu';

    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));

    if (days > 0) {
      return `${days}g ${hours}s`;
    } else {
      return `${hours}s`;
    }
  };

  // Render period tabs
  const renderPeriodTabs = () => {
    if (!showPeriodTabs) return null;

    return (
      <View style={styles.periodTabs}>
        <TouchableOpacity
          style={[
            styles.periodTab,
            activePeriod === 'weekly' && styles.activePeriodTab,
          ]}
          onPress={() => handlePeriodChange('weekly')}
          disabled={localLoading}
        >
          <Text style={[
            styles.periodTabText,
            activePeriod === 'weekly' && styles.activePeriodTabText,
          ]}>
            Bu Hafta
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[
            styles.periodTab,
            activePeriod === 'all' && styles.activePeriodTab,
          ]}
          onPress={() => handlePeriodChange('all')}
          disabled={localLoading}
        >
          <Text style={[
            styles.periodTabText,
            activePeriod === 'all' && styles.activePeriodTabText,
          ]}>
            Genel
          </Text>
        </TouchableOpacity>
      </View>
    );
  };

  if (isCurrentlyLoading && leaderboard.length === 0) {
    return (
      <View style={styles.container}>
        <TouchableOpacity style={styles.header} onPress={onPress}>
          <View style={styles.titleSection}>
            <Text style={styles.title}>🏆 Strategist Yarışması</Text>
            <Text style={styles.subtitle}>Sıralama yükleniyor...</Text>
          </View>
          <Text style={styles.expandIcon}>→</Text>
        </TouchableOpacity>
        {renderPeriodTabs()}
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="small" color="#667eea" />
          <Text style={styles.loadingText}>Sıralama yükleniyor...</Text>
        </View>
      </View>
    );
  }

  if (leaderboard.length === 0 && !isCurrentlyLoading) {
    return (
      <View style={styles.container}>
        <TouchableOpacity style={styles.header} onPress={onPress}>
          <View style={styles.titleSection}>
            <Text style={styles.title}>🏆 Strategist Yarışması</Text>
            <Text style={styles.subtitle}>Yarışma başlıyor</Text>
          </View>
          <Text style={styles.expandIcon}>→</Text>
        </TouchableOpacity>
        {renderPeriodTabs()}
        <View style={styles.emptyState}>
          <Text style={styles.emptyIcon}>🎮</Text>
          <Text style={styles.emptyText}>
            {activePeriod === 'weekly' ? 'Bu haftanın yarışması henüz başlamadı' : 'Henüz katılımcı yok'}
          </Text>
          <Text style={styles.emptySubtext}>
            İlk katılan ol ve liderlik koltuğunu kap!
          </Text>
          {(onChallengePress || onJoinCompetition) && (
            <TouchableOpacity
              style={styles.joinButton}
              onPress={onJoinCompetition || onChallengePress}
            >
              <Text style={styles.joinButtonText}>🚀 Yarışmaya Katıl</Text>
            </TouchableOpacity>
          )}
        </View>
      </View>
    );
  }

  return (
    <Animated.View style={[styles.container, { opacity: fadeAnim }]}>
      <TouchableOpacity style={styles.header} onPress={onPress}>
        <View style={styles.titleSection}>
          <Text style={styles.title}>🏆 Strategist Yarışması</Text>
          {stats && (
            <Text style={styles.subtitle}>
              {stats.totalParticipants} katılımcı • {formatTimeRemaining()}
              {activePeriod === 'weekly' ? ' • Haftalık' : ' • Genel'}
            </Text>
          )}
        </View>
        <View style={styles.headerRight}>
          {localLoading && <ActivityIndicator size="small" color="#667eea" style={styles.headerSpinner} />}
          <Text style={styles.expandIcon}>→</Text>
        </View>
      </TouchableOpacity>

      {renderPeriodTabs()}

      <ScrollView
        style={styles.content}
        showsVerticalScrollIndicator={false}
        nestedScrollEnabled={true}
        refreshControl={
          enablePullToRefresh ? (
            <RefreshControl
              refreshing={refreshing}
              onRefresh={handleRefresh}
              tintColor="#667eea"
              colors={['#667eea']}
              title="Sıralama güncelleniyor..."
            />
          ) : undefined
        }
      >
        {displayedEntries.map((entry) => (
          <LeaderboardRow
            key={entry.userId}
            entry={entry}
            isCurrentUser={entry.userId === currentUserId}
            onPress={onUserPress}
          />
        ))}

        {showUserRanking && userRanking && !displayedEntries.some(e => e.userId === currentUserId) && (
          <>
            {userRanking.rank > maxEntries && (
              <View style={styles.separator}>
                <View style={styles.separatorLine} />
                <Text style={styles.separatorText}>...</Text>
                <View style={styles.separatorLine} />
              </View>
            )}

            <View style={styles.userRankingContainer}>
              <View style={styles.userRankingHeader}>
                <Text style={styles.userRankingLabel}>Sizin sıralamanız:</Text>
                {rankChange && (
                  <View style={styles.rankChangeContainer}>
                    <Text style={styles.rankChangeIcon}>{getRankChangeIcon(rankChange)}</Text>
                    <Text style={[
                      styles.rankChangeText,
                      {
                        color: rankChange === 'up' ? '#10b981' :
                               rankChange === 'down' ? '#ef4444' : '#6b7280'
                      }
                    ]}>
                      {rankChange === 'up' ? 'Yükseliş' :
                       rankChange === 'down' ? 'Düşüş' : 'Sabit'}
                    </Text>
                  </View>
                )}
              </View>
              <View style={styles.userRankingRow}>
                <Text style={styles.userRank}>#{userRanking.rank}</Text>
                <Text style={styles.userScore}>{userRanking.score} puan</Text>
                <Text style={[
                  styles.userReturn,
                  { color: userRanking.returnPercent >= 0 ? '#10b981' : '#ef4444' }
                ]}>
                  {userRanking.returnPercent >= 0 ? '+' : ''}{userRanking.returnPercent ? userRanking.returnPercent.toFixed(1) : '0.0'}%
                </Text>
              </View>
              <View style={styles.userStatsRow}>
                <Text style={styles.userPercentile}>
                  En iyi %{userRanking.percentile ? userRanking.percentile.toFixed(1) : '0.0'} dilimde
                </Text>
                <Text style={styles.userEligibility}>
                  {userRanking.isEligible ? '✅ Uygun' : '❌ Uygun değil'}
                </Text>
              </View>
            </View>
          </>
        )}
      </ScrollView>

      <View style={styles.footer}>
        {(onChallengePress || onJoinCompetition) && (
          <TouchableOpacity
            style={styles.challengeButton}
            onPress={onJoinCompetition || onChallengePress}
            disabled={localLoading}
          >
            <Text style={styles.challengeButtonText}>
              {userRanking ? '🎯 Meydan Oku' : '🚀 Katıl'}
            </Text>
          </TouchableOpacity>
        )}

        <TouchableOpacity
          style={styles.viewAllButton}
          onPress={onPress}
          disabled={localLoading}
        >
          <Text style={styles.viewAllButtonText}>Detaylı Sıralama</Text>
        </TouchableOpacity>
      </View>
    </Animated.View>
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
  titleSection: {
    flex: 1,
  },
  title: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 12,
    color: '#6b7280',
  },
  headerRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  headerSpinner: {
    marginRight: 4,
  },
  expandIcon: {
    fontSize: 16,
    color: '#6b7280',
    fontWeight: '600',
  },
  periodTabs: {
    flexDirection: 'row',
    backgroundColor: '#f8fafc',
    marginHorizontal: 16,
    borderRadius: 8,
    padding: 2,
    marginBottom: 4,
  },
  periodTab: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 6,
    alignItems: 'center',
  },
  activePeriodTab: {
    backgroundColor: '#667eea',
  },
  periodTabText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#6b7280',
  },
  activePeriodTabText: {
    color: 'white',
  },
  content: {
    maxHeight: 240,
    backgroundColor: '#f8fafc',
  },
  loadingContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 32,
    gap: 8,
  },
  loadingText: {
    fontSize: 14,
    color: '#6b7280',
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
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 12,
    color: '#9ca3af',
    textAlign: 'center',
    marginBottom: 16,
  },
  joinButton: {
    backgroundColor: '#667eea',
    borderRadius: 12,
    paddingHorizontal: 20,
    paddingVertical: 10,
  },
  joinButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  leaderboardRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0,0,0,0.05)',
  },
  currentUserRow: {
    backgroundColor: 'rgba(102, 126, 234, 0.1)',
  },
  rankSection: {
    width: 32,
    alignItems: 'center',
  },
  rankText: {
    fontSize: 14,
    fontWeight: '700',
    color: '#374151',
  },
  topRankText: {
    fontSize: 16,
    color: '#f59e0b',
  },
  userSection: {
    flex: 1,
    marginLeft: 12,
  },
  userInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  userName: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
    flex: 1,
  },
  currentUserName: {
    color: '#667eea',
  },
  userBadges: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  tierIcon: {
    fontSize: 12,
  },
  badgeCount: {
    backgroundColor: '#f59e0b',
    borderRadius: 8,
    paddingHorizontal: 4,
    paddingVertical: 1,
    minWidth: 16,
    alignItems: 'center',
  },
  badgeCountText: {
    fontSize: 10,
    fontWeight: '600',
    color: 'white',
  },
  statsSection: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  portfolioValue: {
    fontSize: 12,
    fontWeight: '600',
    color: '#374151',
  },
  returnPercent: {
    fontSize: 12,
    fontWeight: '600',
  },
  metricsSection: {
    alignItems: 'flex-end',
    marginLeft: 8,
  },
  winRate: {
    fontSize: 12,
    fontWeight: '600',
    color: '#10b981',
  },
  trades: {
    fontSize: 10,
    color: '#6b7280',
  },
  separator: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  separatorLine: {
    flex: 1,
    height: 1,
    backgroundColor: '#e5e7eb',
  },
  separatorText: {
    fontSize: 12,
    color: '#9ca3af',
    paddingHorizontal: 8,
  },
  userRankingContainer: {
    backgroundColor: 'rgba(102, 126, 234, 0.05)',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: 'rgba(102, 126, 234, 0.2)',
  },
  userRankingHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  rankChangeContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  rankChangeIcon: {
    fontSize: 12,
  },
  rankChangeText: {
    fontSize: 10,
    fontWeight: '600',
  },
  userRankingLabel: {
    fontSize: 11,
    color: '#6b7280',
    marginBottom: 4,
  },
  userRankingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    marginBottom: 4,
  },
  userRank: {
    fontSize: 16,
    fontWeight: '700',
    color: '#667eea',
  },
  userScore: {
    fontSize: 14,
    fontWeight: '600',
    color: '#374151',
  },
  userReturn: {
    fontSize: 14,
    fontWeight: '600',
  },
  userStatsRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  userPercentile: {
    fontSize: 11,
    color: '#6b7280',
  },
  userEligibility: {
    fontSize: 10,
    fontWeight: '600',
    color: '#6b7280',
  },
  footer: {
    flexDirection: 'row',
    padding: 12,
    gap: 8,
    backgroundColor: 'rgba(255,255,255,0.95)',
    borderTopWidth: 1,
    borderTopColor: 'rgba(0,0,0,0.05)',
  },
  challengeButton: {
    backgroundColor: '#667eea',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    flex: 1,
  },
  challengeButtonText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
    textAlign: 'center',
  },
  viewAllButton: {
    backgroundColor: '#f3f4f6',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    flex: 1,
  },
  viewAllButtonText: {
    color: '#374151',
    fontSize: 12,
    fontWeight: '600',
    textAlign: 'center',
  },
});

export default memo(CompactLeaderboard);