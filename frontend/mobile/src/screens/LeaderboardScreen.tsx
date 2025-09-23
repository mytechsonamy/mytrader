import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  RefreshControl,
  FlatList,
  TextInput,
  Modal,
  Animated,
  VirtualizedList,
  Dimensions,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { apiService } from '../services/api';
import {
  LeaderboardEntry,
  UserRanking,
  CompetitionStats,
  AssetClassType
} from '../types';

interface FilterOptions {
  period: 'weekly' | 'monthly' | 'all';
  assetClass?: AssetClassType;
  minTrades?: number;
  sortBy: 'rank' | 'return' | 'winRate' | 'totalTrades';
}

interface PerformanceTier {
  name: string;
  color: string;
  icon: string;
  minScore: number;
  benefits: string[];
}

const { width: SCREEN_WIDTH } = Dimensions.get('window');

const LeaderboardScreen: React.FC = () => {
  const { user } = useAuth();
  const scrollY = useRef(new Animated.Value(0)).current;

  // State management
  const [activeTab, setActiveTab] = useState<'leaderboard' | 'achievements' | 'analytics'>('leaderboard');
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [userRanking, setUserRanking] = useState<UserRanking | null>(null);
  const [stats, setStats] = useState<CompetitionStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [showFilters, setShowFilters] = useState(false);
  const [showRules, setShowRules] = useState(false);
  const [showUserProfile, setShowUserProfile] = useState<LeaderboardEntry | null>(null);

  // Filter state
  const [filters, setFilters] = useState<FilterOptions>({
    period: 'weekly',
    sortBy: 'rank',
  });

  // Performance tiers
  const performanceTiers: PerformanceTier[] = [
    {
      name: 'Elmas',
      color: '#8b5cf6',
      icon: '💎',
      minScore: 10000,
      benefits: ['VIP destek', 'Özel analiz raporları', 'Erken erişim']
    },
    {
      name: 'Platin',
      color: '#06b6d4',
      icon: '🏆',
      minScore: 5000,
      benefits: ['Gelişmiş analizler', 'Grup danışmanlığı', 'Özel etkinlikler']
    },
    {
      name: 'Altın',
      color: '#eab308',
      icon: '🥇',
      minScore: 2500,
      benefits: ['Haftalık rapor', 'Strateji önerileri', 'Topluluk erişimi']
    },
    {
      name: 'Gümüş',
      color: '#64748b',
      icon: '🥈',
      minScore: 1000,
      benefits: ['Temel analizler', 'E-posta bildirimleri']
    },
    {
      name: 'Bronz',
      color: '#dc2626',
      icon: '🥉',
      minScore: 0,
      benefits: ['Temel özellikler']
    },
  ];

  // Fetch data functions
  const fetchLeaderboardData = useCallback(async (showLoading = true) => {
    if (showLoading) setLoading(true);

    try {
      const [leaderboardData, userRankingData, statsData] = await Promise.allSettled([
        apiService.getLeaderboard(filters.period, 100),
        user ? apiService.getUserRanking(filters.period) : Promise.resolve(null),
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
      console.error('Failed to fetch leaderboard data:', error);
      Alert.alert('Hata', 'Veriler yüklenirken bir hata oluştu.');
    } finally {
      setLoading(false);
    }
  }, [filters.period, user]);

  const handleRefresh = useCallback(async () => {
    setRefreshing(true);
    await fetchLeaderboardData(false);
    setRefreshing(false);
  }, [fetchLeaderboardData]);

  // Filter and search logic
  const filteredLeaderboard = useMemo(() => {
    let filtered = [...leaderboard];

    // Search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(entry =>
        entry.displayName.toLowerCase().includes(query) ||
        entry.userName.toLowerCase().includes(query)
      );
    }

    // Asset class filter
    if (filters.assetClass) {
      // This would need to be implemented in the API
      // For now, we'll just return the filtered list
    }

    // Minimum trades filter
    if (filters.minTrades !== undefined) {
      filtered = filtered.filter(entry => entry.totalTrades >= filters.minTrades!);
    }

    // Sort
    filtered.sort((a, b) => {
      switch (filters.sortBy) {
        case 'return':
          return b.returnPercent - a.returnPercent;
        case 'winRate':
          return b.winRate - a.winRate;
        case 'totalTrades':
          return b.totalTrades - a.totalTrades;
        default:
          return a.rank - b.rank;
      }
    });

    return filtered;
  }, [leaderboard, searchQuery, filters]);

  // Helper functions
  const getTierForScore = (score: number): PerformanceTier => {
    return performanceTiers.find(tier => score >= tier.minScore) || performanceTiers[performanceTiers.length - 1];
  };

  const formatCurrency = (amount: number): string => {
    if (amount >= 1000000) return `${(amount / 1000000).toFixed(1)}M ₺`;
    if (amount >= 1000) return `${(amount / 1000).toFixed(1)}K ₺`;
    return `${amount.toFixed(0)} ₺`;
  };

  const formatPercentage = (percent: number): string => {
    const sign = percent >= 0 ? '+' : '';
    return `${sign}${percent.toFixed(1)}%`;
  };

  const getReturnColor = (returnPercent: number): string => {
    if (returnPercent > 0) return '#10b981';
    if (returnPercent < 0) return '#ef4444';
    return '#6b7280';
  };

  const formatTimeRemaining = (): string => {
    if (!stats?.currentPeriodEnd) return '';

    const endDate = new Date(stats.currentPeriodEnd);
    const now = new Date();
    const diff = endDate.getTime() - now.getTime();

    if (diff <= 0) return 'Süre doldu';

    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));

    if (days > 0) return `${days} gün ${hours} saat`;
    return `${hours} saat`;
  };

  const handleJoinCompetition = useCallback(async () => {
    if (!user) {
      Alert.alert(
        'Giriş Gerekli',
        'Yarışmaya katılmak için giriş yapmanız gerekiyor.',
        [{ text: 'Tamam' }]
      );
      return;
    }

    try {
      setLoading(true);
      const result = await apiService.joinCompetition();
      if (result.success) {
        Alert.alert('Başarılı!', 'Yarışmaya başarıyla katıldınız!');
        await fetchLeaderboardData(false);
      } else {
        Alert.alert('Hata', result.message || 'Yarışmaya katılırken bir hata oluştu.');
      }
    } catch (error) {
      Alert.alert('Hata', 'Yarışmaya katılırken bir hata oluştu.');
    } finally {
      setLoading(false);
    }
  }, [user, fetchLeaderboardData]);

  // Component render functions
  const renderHeader = () => (
    <Animated.View style={[
      styles.header,
      {
        transform: [{
          translateY: scrollY.interpolate({
            inputRange: [0, 100],
            outputRange: [0, -20],
            extrapolate: 'clamp',
          }),
        }],
      },
    ]}>
      <View style={styles.headerTop}>
        <Text style={styles.title}>🏆 Strategist Yarışması</Text>
        <TouchableOpacity
          style={styles.rulesButton}
          onPress={() => setShowRules(true)}
        >
          <Text style={styles.rulesButtonText}>📋 Kurallar</Text>
        </TouchableOpacity>
      </View>

      {stats && (
        <View style={styles.statsContainer}>
          <View style={styles.statItem}>
            <Text style={styles.statValue}>{stats.totalParticipants}</Text>
            <Text style={styles.statLabel}>Katılımcı</Text>
          </View>
          <View style={styles.statItem}>
            <Text style={styles.statValue}>{formatCurrency(stats.totalPrizePool)}</Text>
            <Text style={styles.statLabel}>Ödül Havuzu</Text>
          </View>
          <View style={styles.statItem}>
            <Text style={styles.statValue}>{formatTimeRemaining()}</Text>
            <Text style={styles.statLabel}>Kalan Süre</Text>
          </View>
        </View>
      )}

      <View style={styles.tabContainer}>
        {['leaderboard', 'achievements', 'analytics'].map((tab) => (
          <TouchableOpacity
            key={tab}
            style={[styles.tab, activeTab === tab && styles.activeTab]}
            onPress={() => setActiveTab(tab as any)}
          >
            <Text style={[styles.tabText, activeTab === tab && styles.activeTabText]}>
              {tab === 'leaderboard' ? '🏆 Sıralama' :
               tab === 'achievements' ? '🏅 Başarılar' : '📊 Analiz'}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
    </Animated.View>
  );

  const renderPeriodFilter = () => (
    <View style={styles.periodFilter}>
      {(['weekly', 'monthly', 'all'] as const).map((period) => (
        <TouchableOpacity
          key={period}
          style={[
            styles.periodButton,
            filters.period === period && styles.activePeriodButton,
          ]}
          onPress={() => setFilters(prev => ({ ...prev, period }))}
        >
          <Text style={[
            styles.periodButtonText,
            filters.period === period && styles.activePeriodButtonText,
          ]}>
            {period === 'weekly' ? 'Haftalık' :
             period === 'monthly' ? 'Aylık' : 'Genel'}
          </Text>
        </TouchableOpacity>
      ))}
    </View>
  );

  const renderSearchAndFilters = () => (
    <View style={styles.searchContainer}>
      <View style={styles.searchInputContainer}>
        <Text style={styles.searchIcon}>🔍</Text>
        <TextInput
          style={styles.searchInput}
          placeholder="Kullanıcı ara..."
          value={searchQuery}
          onChangeText={setSearchQuery}
          placeholderTextColor="#9ca3af"
        />
      </View>
      <TouchableOpacity
        style={styles.filterButton}
        onPress={() => setShowFilters(true)}
      >
        <Text style={styles.filterIcon}>⚙️</Text>
      </TouchableOpacity>
    </View>
  );

  const renderUserRankingCard = () => {
    if (!userRanking || !user) return null;

    const userTier = getTierForScore(userRanking.score);

    return (
      <View style={styles.userRankingCard}>
        <View style={styles.userRankingHeader}>
          <View style={styles.userRankInfo}>
            <Text style={styles.userRankNumber}>#{userRanking.rank}</Text>
            <View style={styles.userTierContainer}>
              <Text style={styles.tierIcon}>{userTier.icon}</Text>
              <Text style={[styles.tierName, { color: userTier.color }]}>
                {userTier.name}
              </Text>
            </View>
          </View>
          <View style={styles.userScoreContainer}>
            <Text style={styles.userScore}>{userRanking.score}</Text>
            <Text style={styles.userScoreLabel}>puan</Text>
          </View>
        </View>

        <View style={styles.userStatsGrid}>
          <View style={styles.userStatItem}>
            <Text style={[styles.userStatValue, { color: getReturnColor(userRanking.returnPercent) }]}>
              {formatPercentage(userRanking.returnPercent)}
            </Text>
            <Text style={styles.userStatLabel}>Getiri</Text>
          </View>
          <View style={styles.userStatItem}>
            <Text style={styles.userStatValue}>%{userRanking.percentile.toFixed(1)}</Text>
            <Text style={styles.userStatLabel}>Dilim</Text>
          </View>
          <View style={styles.userStatItem}>
            <Text style={[
              styles.userStatValue,
              { color: userRanking.isEligible ? '#10b981' : '#ef4444' }
            ]}>
              {userRanking.isEligible ? '✅' : '❌'}
            </Text>
            <Text style={styles.userStatLabel}>Uygunluk</Text>
          </View>
        </View>

        {!userRanking.isEligible && userRanking.disqualificationReason && (
          <View style={styles.disqualificationNotice}>
            <Text style={styles.disqualificationText}>
              ⚠️ {userRanking.disqualificationReason}
            </Text>
          </View>
        )}
      </View>
    );
  };

  const renderLeaderboardEntry = ({ item, index }: { item: LeaderboardEntry; index: number }) => {
    const tier = getTierForScore(item.score);
    const isCurrentUser = item.userId === user?.id;

    return (
      <TouchableOpacity
        style={[
          styles.leaderboardEntry,
          isCurrentUser && styles.currentUserEntry,
          index < 3 && styles.topThreeEntry,
        ]}
        onPress={() => setShowUserProfile(item)}
        activeOpacity={0.8}
      >
        <View style={styles.rankContainer}>
          <Text style={[
            styles.rankText,
            index < 3 && styles.topRankText,
          ]}>
            {index === 0 ? '👑' :
             index === 1 ? '🥈' :
             index === 2 ? '🥉' : `#${item.rank}`}
          </Text>
        </View>

        <View style={styles.userInfoSection}>
          <View style={styles.userNameRow}>
            <Text style={[
              styles.displayName,
              isCurrentUser && styles.currentUserName,
            ]} numberOfLines={1}>
              {item.displayName}
            </Text>
            <View style={styles.tierBadge}>
              <Text style={styles.tierIcon}>{tier.icon}</Text>
            </View>
          </View>

          <View style={styles.userStatsRow}>
            <Text style={styles.portfolioValue}>
              {formatCurrency(item.portfolioValue)}
            </Text>
            <Text style={[
              styles.returnValue,
              { color: getReturnColor(item.returnPercent) }
            ]}>
              {formatPercentage(item.returnPercent)}
            </Text>
          </View>

          <View style={styles.tradingStatsRow}>
            <Text style={styles.winRate}>{item.winRate.toFixed(0)}% kazanç</Text>
            <Text style={styles.totalTrades}>{item.totalTrades} işlem</Text>
          </View>
        </View>

        <View style={styles.badgeSection}>
          <Text style={styles.scoreText}>{item.score}</Text>
          {item.badges.length > 0 && (
            <View style={styles.badgeIndicator}>
              <Text style={styles.badgeCount}>{item.badges.length}</Text>
            </View>
          )}
        </View>
      </TouchableOpacity>
    );
  };

  const renderEmptyState = () => (
    <View style={styles.emptyState}>
      <Text style={styles.emptyIcon}>🎯</Text>
      <Text style={styles.emptyTitle}>Henüz yarışmacı yok</Text>
      <Text style={styles.emptyText}>
        İlk katılan ol ve liderlik koltuğunu kap!
      </Text>
      <TouchableOpacity
        style={styles.joinButton}
        onPress={handleJoinCompetition}
      >
        <Text style={styles.joinButtonText}>🚀 Yarışmaya Katıl</Text>
      </TouchableOpacity>
    </View>
  );

  // Effects
  useEffect(() => {
    fetchLeaderboardData();
  }, [fetchLeaderboardData]);

  useEffect(() => {
    fetchLeaderboardData();
  }, [filters.period]);

  if (!user) {
    return (
      <View style={styles.container}>
        <View style={styles.loginPrompt}>
          <Text style={styles.loginTitle}>🏆 Strategist Yarışması</Text>
          <Text style={styles.loginText}>
            Sıralamaları görüntülemek ve yarışmaya katılmak için giriş yapın
          </Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {renderHeader()}

      {activeTab === 'leaderboard' && (
        <View style={styles.leaderboardContainer}>
          {renderPeriodFilter()}
          {renderUserRankingCard()}
          {renderSearchAndFilters()}

          <Animated.FlatList
            data={filteredLeaderboard}
            renderItem={renderLeaderboardEntry}
            keyExtractor={(item) => item.userId}
            contentContainerStyle={styles.listContainer}
            showsVerticalScrollIndicator={false}
            refreshControl={
              <RefreshControl
                refreshing={refreshing}
                onRefresh={handleRefresh}
                tintColor="#667eea"
                colors={['#667eea']}
              />
            }
            ListEmptyComponent={!loading ? renderEmptyState : null}
            onScroll={Animated.event(
              [{ nativeEvent: { contentOffset: { y: scrollY } } }],
              { useNativeDriver: true }
            )}
            scrollEventThrottle={16}
            removeClippedSubviews={true}
            maxToRenderPerBatch={10}
            updateCellsBatchingPeriod={50}
            initialNumToRender={15}
          />
        </View>
      )}

      {loading && (
        <View style={styles.loadingOverlay}>
          <ActivityIndicator size="large" color="#667eea" />
          <Text style={styles.loadingText}>Yükleniyor...</Text>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  header: {
    backgroundColor: 'white',
    paddingTop: 60,
    paddingBottom: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  headerTop: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 20,
    marginBottom: 16,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
    color: '#1f2937',
  },
  rulesButton: {
    backgroundColor: '#f3f4f6',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
  },
  rulesButtonText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#374151',
  },
  statsContainer: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingHorizontal: 20,
    marginBottom: 16,
  },
  statItem: {
    alignItems: 'center',
  },
  statValue: {
    fontSize: 18,
    fontWeight: '700',
    color: '#667eea',
    marginBottom: 4,
  },
  statLabel: {
    fontSize: 12,
    color: '#6b7280',
  },
  tabContainer: {
    flexDirection: 'row',
    paddingHorizontal: 20,
    gap: 8,
  },
  tab: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    alignItems: 'center',
    backgroundColor: '#f3f4f6',
  },
  activeTab: {
    backgroundColor: '#667eea',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#6b7280',
  },
  activeTabText: {
    color: 'white',
  },
  leaderboardContainer: {
    flex: 1,
  },
  periodFilter: {
    flexDirection: 'row',
    paddingHorizontal: 20,
    paddingVertical: 16,
    gap: 8,
  },
  periodButton: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 20,
    alignItems: 'center',
    backgroundColor: '#f3f4f6',
  },
  activePeriodButton: {
    backgroundColor: '#667eea',
  },
  periodButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#6b7280',
  },
  activePeriodButtonText: {
    color: 'white',
  },
  userRankingCard: {
    backgroundColor: 'white',
    marginHorizontal: 20,
    marginBottom: 16,
    borderRadius: 16,
    padding: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  userRankingHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
  },
  userRankInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  userRankNumber: {
    fontSize: 32,
    fontWeight: '700',
    color: '#667eea',
  },
  userTierContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  tierIcon: {
    fontSize: 16,
  },
  tierName: {
    fontSize: 14,
    fontWeight: '600',
  },
  userScoreContainer: {
    alignItems: 'flex-end',
  },
  userScore: {
    fontSize: 24,
    fontWeight: '700',
    color: '#1f2937',
  },
  userScoreLabel: {
    fontSize: 12,
    color: '#6b7280',
  },
  userStatsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  userStatItem: {
    alignItems: 'center',
  },
  userStatValue: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 4,
  },
  userStatLabel: {
    fontSize: 12,
    color: '#6b7280',
  },
  disqualificationNotice: {
    backgroundColor: '#fef3c7',
    padding: 12,
    borderRadius: 8,
    marginTop: 12,
  },
  disqualificationText: {
    fontSize: 12,
    color: '#d97706',
    textAlign: 'center',
  },
  searchContainer: {
    flexDirection: 'row',
    paddingHorizontal: 20,
    marginBottom: 16,
    gap: 12,
  },
  searchInputContainer: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: 'white',
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  searchIcon: {
    fontSize: 16,
    marginRight: 8,
  },
  searchInput: {
    flex: 1,
    fontSize: 14,
    color: '#1f2937',
  },
  filterButton: {
    backgroundColor: 'white',
    width: 44,
    height: 44,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  filterIcon: {
    fontSize: 16,
  },
  listContainer: {
    paddingHorizontal: 20,
    paddingBottom: 20,
  },
  leaderboardEntry: {
    backgroundColor: 'white',
    borderRadius: 16,
    padding: 16,
    marginBottom: 12,
    flexDirection: 'row',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  currentUserEntry: {
    borderWidth: 2,
    borderColor: '#667eea',
    backgroundColor: '#f0f9ff',
  },
  topThreeEntry: {
    borderLeftWidth: 4,
    borderLeftColor: '#f59e0b',
  },
  rankContainer: {
    width: 40,
    alignItems: 'center',
    marginRight: 16,
  },
  rankText: {
    fontSize: 16,
    fontWeight: '700',
    color: '#6b7280',
  },
  topRankText: {
    fontSize: 20,
  },
  userInfoSection: {
    flex: 1,
  },
  userNameRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 4,
  },
  displayName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1f2937',
    flex: 1,
  },
  currentUserName: {
    color: '#667eea',
  },
  tierBadge: {
    marginLeft: 8,
  },
  userStatsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    marginBottom: 4,
  },
  portfolioValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#374151',
  },
  returnValue: {
    fontSize: 14,
    fontWeight: '600',
  },
  tradingStatsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  winRate: {
    fontSize: 12,
    color: '#10b981',
    fontWeight: '500',
  },
  totalTrades: {
    fontSize: 12,
    color: '#6b7280',
  },
  badgeSection: {
    alignItems: 'flex-end',
    marginLeft: 12,
  },
  scoreText: {
    fontSize: 14,
    fontWeight: '700',
    color: '#667eea',
    marginBottom: 4,
  },
  badgeIndicator: {
    backgroundColor: '#f59e0b',
    borderRadius: 10,
    paddingHorizontal: 6,
    paddingVertical: 2,
    minWidth: 20,
    alignItems: 'center',
  },
  badgeCount: {
    fontSize: 10,
    fontWeight: '600',
    color: 'white',
  },
  emptyState: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 60,
    paddingHorizontal: 40,
  },
  emptyIcon: {
    fontSize: 64,
    marginBottom: 16,
  },
  emptyTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 8,
    textAlign: 'center',
  },
  emptyText: {
    fontSize: 14,
    color: '#6b7280',
    textAlign: 'center',
    marginBottom: 24,
    lineHeight: 20,
  },
  joinButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 24,
  },
  joinButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: 'white',
  },
  loadingOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0,0,0,0.3)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingText: {
    marginTop: 12,
    fontSize: 16,
    color: 'white',
    fontWeight: '600',
  },
  loginPrompt: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 40,
  },
  loginTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 12,
    textAlign: 'center',
  },
  loginText: {
    fontSize: 16,
    color: '#6b7280',
    textAlign: 'center',
    lineHeight: 24,
  },
});

export default LeaderboardScreen;