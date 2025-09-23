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
  Animated,
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
import { useLeaderboardWebSocket } from '../hooks/useLeaderboardWebSocket';
import {
  CompetitionEntry,
  RulesModal,
  PerformanceChart,
  PerformanceTiers,
  UserRankCard,
  SocialFeatures,
} from '../components/leaderboard';

interface FilterOptions {
  period: 'weekly' | 'monthly' | 'all';
  assetClass?: AssetClassType;
  minTrades?: number;
  sortBy: 'rank' | 'return' | 'winRate' | 'totalTrades';
}

const { width: SCREEN_WIDTH } = Dimensions.get('window');

const EnhancedLeaderboardScreen: React.FC = () => {
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
  const [showCompetitionEntry, setShowCompetitionEntry] = useState(false);
  const [followedUsers, setFollowedUsers] = useState<Set<string>>(new Set());
  const [connectionStatus, setConnectionStatus] = useState<'connected' | 'disconnected'>('disconnected');

  // Filter state
  const [filters, setFilters] = useState<FilterOptions>({
    period: 'weekly',
    sortBy: 'rank',
  });

  // Real-time WebSocket integration
  const {
    isConnected,
    lastUpdate,
    connectionStatus: wsStatus,
    forceRefresh,
  } = useLeaderboardWebSocket({
    enabled: true,
    period: filters.period,
    onRankingChange: (newRanking) => {
      setLeaderboard(newRanking);
    },
    onUserRankingChange: (newUserRanking) => {
      setUserRanking(newUserRanking);
    },
    onStatsChange: (newStats) => {
      setStats(newStats);
    },
  });

  useEffect(() => {
    setConnectionStatus(isConnected ? 'connected' : 'disconnected');
  }, [isConnected]);

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
    try {
      if (isConnected) {
        await forceRefresh();
      } else {
        await fetchLeaderboardData(false);
      }
    } finally {
      setRefreshing(false);
    }
  }, [fetchLeaderboardData, forceRefresh, isConnected]);

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

  const handleJoinCompetition = useCallback(() => {
    if (!user) {
      Alert.alert(
        'Giriş Gerekli',
        'Yarışmaya katılmak için giriş yapmanız gerekiyor.',
        [{ text: 'Tamam' }]
      );
      return;
    }
    setShowCompetitionEntry(true);
  }, [user]);

  const handleCompetitionSuccess = useCallback(async () => {
    await fetchLeaderboardData(false);
  }, [fetchLeaderboardData]);

  const handleFollowToggle = useCallback(async (userId: string, following: boolean) => {
    // In a real app, this would call an API
    setFollowedUsers(prev => {
      const newSet = new Set(prev);
      if (following) {
        newSet.add(userId);
      } else {
        newSet.delete(userId);
      }
      return newSet;
    });
  }, []);

  const handleCopyStrategy = useCallback(async (userId: string, strategySettings: any) => {
    // In a real app, this would call an API to set up strategy copying
    console.log('Copy strategy:', userId, strategySettings);
  }, []);

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
          <View style={styles.statItem}>
            <View style={[styles.connectionIndicator, { backgroundColor: connectionStatus === 'connected' ? '#10b981' : '#ef4444' }]} />
            <Text style={styles.statLabel}>{connectionStatus === 'connected' ? 'Canlı' : 'Çevrimdışı'}</Text>
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
               tab === 'achievements' ? '🏅 Seviyeler' : '📊 Analiz'}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {lastUpdate && (
        <View style={styles.lastUpdateContainer}>
          <Text style={styles.lastUpdateText}>
            Son güncelleme: {lastUpdate.toLocaleTimeString('tr-TR')}
          </Text>
        </View>
      )}
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

    return (
      <UserRankCard
        userRanking={userRanking}
        stats={stats}
        showProgress={true}
        animated={true}
      />
    );
  };

  const renderLeaderboardEntry = ({ item, index }: { item: LeaderboardEntry; index: number }) => {
    const isCurrentUser = item.userId === user?.id;

    return (
      <TouchableOpacity
        style={[
          styles.leaderboardEntry,
          isCurrentUser && styles.currentUserEntry,
          index < 3 && styles.topThreeEntry,
        ]}
        onPress={() => {
          setShowUserProfile(item);
        }}
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
              <Text style={styles.tierIcon}>
                {item.tier === 'DIAMOND' ? '💎' :
                 item.tier === 'PLATINUM' ? '🏆' :
                 item.tier === 'GOLD' ? '🥇' :
                 item.tier === 'SILVER' ? '🥈' : '🥉'}
              </Text>
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

      {activeTab === 'achievements' && (
        <View style={styles.achievementsContainer}>
          <PerformanceTiers
            currentScore={userRanking?.score || 0}
            tiers={[
              {
                id: 'bronze',
                name: 'Bronze',
                color: '#CD7F32',
                icon: '🥉',
                minScore: 0,
                maxScore: 100,
                benefits: ['Basic analytics'],
                requirements: ['Complete registration'],
                description: 'Entry level trader'
              },
              {
                id: 'silver',
                name: 'Silver',
                color: '#C0C0C0',
                icon: '🥈',
                minScore: 100,
                maxScore: 500,
                benefits: ['Advanced analytics', 'Custom alerts'],
                requirements: ['Score 100+ points'],
                description: 'Intermediate trader'
              },
              {
                id: 'gold',
                name: 'Gold',
                color: '#FFD700',
                icon: '🥇',
                minScore: 500,
                benefits: ['Premium features', 'Priority support'],
                requirements: ['Score 500+ points'],
                description: 'Expert trader'
              }
            ]}
            showProgress={true}
            compact={false}
          />
        </View>
      )}

      {activeTab === 'analytics' && (
        <View style={styles.analyticsContainer}>
          <PerformanceChart
            userRanking={userRanking}
            leaderboard={leaderboard}
            period={filters.period}
            showComparison={true}
            compact={false}
          />
        </View>
      )}

      {loading && (
        <View style={styles.loadingOverlay}>
          <ActivityIndicator size="large" color="#667eea" />
          <Text style={styles.loadingText}>Yükleniyor...</Text>
        </View>
      )}

      {/* Competition Entry Modal */}
      <CompetitionEntry
        visible={showCompetitionEntry}
        onClose={() => setShowCompetitionEntry(false)}
        onSuccess={handleCompetitionSuccess}
        stats={stats}
        userRanking={userRanking}
      />

      {/* Rules Modal */}
      <RulesModal
        visible={showRules}
        onClose={() => setShowRules(false)}
        stats={stats}
      />

      {/* User Profile Modal */}
      {showUserProfile && (
        <SocialFeatures
          visible={true}
          onClose={() => setShowUserProfile(null)}
          trader={showUserProfile}
          isFollowing={followedUsers.has(showUserProfile.userId)}
          onFollowToggle={handleFollowToggle}
          onCopyStrategy={handleCopyStrategy}
        />
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
  connectionIndicator: {
    width: 8,
    height: 8,
    borderRadius: 4,
    marginBottom: 4,
  },
  lastUpdateContainer: {
    alignItems: 'center',
    paddingVertical: 8,
    backgroundColor: 'rgba(255,255,255,0.9)',
  },
  lastUpdateText: {
    fontSize: 11,
    color: '#9ca3af',
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
  achievementsContainer: {
    flex: 1,
  },
  analyticsContainer: {
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
  tierIcon: {
    fontSize: 16,
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

export default EnhancedLeaderboardScreen;