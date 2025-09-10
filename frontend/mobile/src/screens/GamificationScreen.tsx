import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  RefreshControl,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { API_BASE_URL } from '../config';

interface Achievement {
  id: string;
  name: string;
  description: string;
  category: string;
  points: number;
  isUnlocked: boolean;
  unlockedAt?: string;
  icon: string;
}

interface LeaderboardEntry {
  rank: number;
  userId: string;
  username: string;
  totalPoints: number;
  level: number;
  totalReturn: number;
}

interface UserStats {
  userId: string;
  totalPoints: number;
  level: number;
  nextLevelPoints: number;
  achievements: Achievement[];
}

const GamificationScreen = () => {
  const { user, getAuthHeaders } = useAuth();
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [activeTab, setActiveTab] = useState<'achievements' | 'leaderboard'>('achievements');
  const [userStats, setUserStats] = useState<UserStats | null>(null);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);

  const achievementIcons: Record<string, string> = {
    'Trading': '📈',
    'Strategy': '🎯',
    'Performance': '🏆',
    'Learning': '📚',
    'Social': '👥',
    'Milestone': '🎖️',
  };

  const fetchUserStats = async () => {
    if (!user) return;

    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/gamification/achievements/${user.id}`, {
        method: 'GET',
        headers,
      });

      if (response.ok) {
        const data = await response.json();
        setUserStats(data);
      } else {
        // Mock data for development
        setUserStats({
          userId: user.id,
          totalPoints: 1250,
          level: 5,
          nextLevelPoints: 1500,
          achievements: [
            {
              id: '1',
              name: 'İlk Adım',
              description: 'İlk stratejini oluştur',
              category: 'Trading',
              points: 100,
              isUnlocked: true,
              unlockedAt: '2024-01-15',
              icon: '🎯',
            },
            {
              id: '2', 
              name: 'Kar Avcısı',
              description: '%10 kar elde et',
              category: 'Performance',
              points: 250,
              isUnlocked: true,
              unlockedAt: '2024-01-20',
              icon: '💰',
            },
            {
              id: '3',
              name: 'Strateji Uzmanı',
              description: '5 farklı strateji oluştur',
              category: 'Strategy',
              points: 300,
              isUnlocked: false,
              icon: '🧠',
            },
          ],
        });
      }
    } catch (error) {
      console.error('Error fetching user stats:', error);
      Alert.alert('Hata', 'Başarı verileriniz yüklenemedi');
    }
  };

  const fetchLeaderboard = async () => {
    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/gamification/leaderboard?limit=10`, {
        method: 'GET',
        headers,
      });

      if (response.ok) {
        const data = await response.json();
        setLeaderboard(data.leaderboard || []);
      } else {
        // Mock data for development
        setLeaderboard([
          {
            rank: 1,
            userId: 'user1',
            username: 'TradingMaster',
            totalPoints: 5240,
            level: 12,
            totalReturn: 0.25,
          },
          {
            rank: 2,
            userId: 'user2',
            username: 'CryptoKing',
            totalPoints: 4180,
            level: 10,
            totalReturn: 0.18,
          },
          {
            rank: 3,
            userId: user?.id || 'user3',
            username: user?.first_name + ' ' + user?.last_name || 'Sen',
            totalPoints: 1250,
            level: 5,
            totalReturn: 0.05,
          },
        ]);
      }
    } catch (error) {
      console.error('Error fetching leaderboard:', error);
    }
  };

  const loadData = async () => {
    setLoading(true);
    await Promise.all([fetchUserStats(), fetchLeaderboard()]);
    setLoading(false);
  };

  const onRefresh = async () => {
    setRefreshing(true);
    await loadData();
    setRefreshing(false);
  };

  useEffect(() => {
    loadData();
  }, []);

  const calculateProgress = () => {
    if (!userStats) return 0;
    const currentLevelPoints = (userStats.level - 1) * 300;
    const nextLevelPoints = userStats.level * 300;
    const progress = (userStats.totalPoints - currentLevelPoints) / (nextLevelPoints - currentLevelPoints);
    return Math.max(0, Math.min(1, progress));
  };

  const renderUserLevel = () => {
    if (!userStats) return null;

    const progress = calculateProgress();

    return (
      <View style={styles.levelCard}>
        <View style={styles.levelHeader}>
          <Text style={styles.levelTitle}>Seviye {userStats.level}</Text>
          <Text style={styles.points}>{userStats.totalPoints} Puan</Text>
        </View>
        
        <View style={styles.progressContainer}>
          <View style={styles.progressBar}>
            <View style={[styles.progressFill, { width: `${progress * 100}%` }]} />
          </View>
          <Text style={styles.progressText}>
            {Math.round(progress * 100)}% - Sonraki seviyeye {userStats.nextLevelPoints - userStats.totalPoints} puan
          </Text>
        </View>
      </View>
    );
  };

  const renderAchievements = () => {
    if (!userStats) return null;

    const unlockedAchievements = userStats.achievements.filter(a => a.isUnlocked);
    const lockedAchievements = userStats.achievements.filter(a => !a.isUnlocked);

    return (
      <View style={styles.achievementsContainer}>
        {unlockedAchievements.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>🏆 Kazanılan Başarılar</Text>
            {unlockedAchievements.map((achievement) => (
              <View key={achievement.id} style={[styles.achievementCard, styles.unlockedCard]}>
                <Text style={styles.achievementIcon}>
                  {achievementIcons[achievement.category] || '🏅'}
                </Text>
                <View style={styles.achievementContent}>
                  <Text style={styles.achievementName}>{achievement.name}</Text>
                  <Text style={styles.achievementDescription}>{achievement.description}</Text>
                  <Text style={styles.achievementPoints}>+{achievement.points} puan</Text>
                </View>
                <Text style={styles.checkmark}>✅</Text>
              </View>
            ))}
          </>
        )}

        {lockedAchievements.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>🔒 Kilidi Açılacak Başarılar</Text>
            {lockedAchievements.map((achievement) => (
              <View key={achievement.id} style={[styles.achievementCard, styles.lockedCard]}>
                <Text style={[styles.achievementIcon, styles.lockedIcon]}>
                  {achievementIcons[achievement.category] || '🏅'}
                </Text>
                <View style={styles.achievementContent}>
                  <Text style={[styles.achievementName, styles.lockedText]}>{achievement.name}</Text>
                  <Text style={[styles.achievementDescription, styles.lockedText]}>
                    {achievement.description}
                  </Text>
                  <Text style={[styles.achievementPoints, styles.lockedText]}>
                    +{achievement.points} puan
                  </Text>
                </View>
                <Text style={styles.lockIcon}>🔒</Text>
              </View>
            ))}
          </>
        )}
      </View>
    );
  };

  const renderLeaderboard = () => {
    return (
      <View style={styles.leaderboardContainer}>
        <Text style={styles.sectionTitle}>🏆 Lider Tablosu</Text>
        {leaderboard.map((entry, index) => (
          <View
            key={entry.userId}
            style={[
              styles.leaderboardCard,
              entry.userId === user?.id && styles.currentUserCard,
              index < 3 && styles.topThreeCard,
            ]}
          >
            <View style={styles.rankContainer}>
              <Text style={[styles.rank, index < 3 && styles.topRank]}>
                {entry.rank === 1 ? '🥇' : entry.rank === 2 ? '🥈' : entry.rank === 3 ? '🥉' : `#${entry.rank}`}
              </Text>
            </View>
            
            <View style={styles.userInfo}>
              <Text style={[styles.username, entry.userId === user?.id && styles.currentUserText]}>
                {entry.username}
              </Text>
              <Text style={styles.userStats}>
                Level {entry.level} • {entry.totalPoints} puan
              </Text>
              <Text style={styles.returnRate}>
                Getiri: {(entry.totalReturn * 100).toFixed(1)}%
              </Text>
            </View>

            {entry.userId === user?.id && (
              <Text style={styles.youBadge}>SEN</Text>
            )}
          </View>
        ))}
      </View>
    );
  };

  if (!user) {
    return (
      <View style={styles.container}>
        <View style={styles.loginPrompt}>
          <Text style={styles.loginTitle}>🏆 Gamifikasyon</Text>
          <Text style={styles.loginText}>Başarılarınızı takip etmek için giriş yapın</Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>🏆 Gamifikasyon</Text>
        <View style={styles.tabContainer}>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'achievements' && styles.activeTab]}
            onPress={() => setActiveTab('achievements')}
          >
            <Text style={[styles.tabText, activeTab === 'achievements' && styles.activeTabText]}>
              Başarılar
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'leaderboard' && styles.activeTab]}
            onPress={() => setActiveTab('leaderboard')}
          >
            <Text style={[styles.tabText, activeTab === 'leaderboard' && styles.activeTabText]}>
              Sıralama
            </Text>
          </TouchableOpacity>
        </View>
      </View>

      <ScrollView
        style={styles.content}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
      >
        {renderUserLevel()}
        
        {loading ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color="#667eea" />
            <Text style={styles.loadingText}>Veriler yükleniyor...</Text>
          </View>
        ) : (
          <>
            {activeTab === 'achievements' && renderAchievements()}
            {activeTab === 'leaderboard' && renderLeaderboard()}
          </>
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
  header: {
    backgroundColor: 'white',
    padding: 20,
    paddingTop: 60,
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#333',
    textAlign: 'center',
    marginBottom: 20,
  },
  tabContainer: {
    flexDirection: 'row',
    backgroundColor: '#f1f5f9',
    borderRadius: 8,
    padding: 4,
  },
  tab: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 6,
    alignItems: 'center',
  },
  activeTab: {
    backgroundColor: '#667eea',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#64748b',
  },
  activeTabText: {
    color: 'white',
  },
  content: {
    flex: 1,
    padding: 20,
  },
  levelCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 20,
    marginBottom: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  levelHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 15,
  },
  levelTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
  },
  points: {
    fontSize: 18,
    fontWeight: '600',
    color: '#667eea',
  },
  progressContainer: {
    marginTop: 10,
  },
  progressBar: {
    height: 8,
    backgroundColor: '#e2e8f0',
    borderRadius: 4,
    marginBottom: 8,
  },
  progressFill: {
    height: '100%',
    backgroundColor: '#667eea',
    borderRadius: 4,
  },
  progressText: {
    fontSize: 12,
    color: '#64748b',
    textAlign: 'center',
  },
  achievementsContainer: {
    marginBottom: 20,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 15,
  },
  achievementCard: {
    flexDirection: 'row',
    alignItems: 'center',
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
  unlockedCard: {
    borderLeftWidth: 4,
    borderLeftColor: '#10b981',
  },
  lockedCard: {
    opacity: 0.6,
    borderLeftWidth: 4,
    borderLeftColor: '#94a3b8',
  },
  achievementIcon: {
    fontSize: 24,
    marginRight: 15,
  },
  lockedIcon: {
    opacity: 0.5,
  },
  achievementContent: {
    flex: 1,
  },
  achievementName: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 4,
  },
  achievementDescription: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 4,
  },
  achievementPoints: {
    fontSize: 12,
    fontWeight: '600',
    color: '#667eea',
  },
  lockedText: {
    opacity: 0.6,
  },
  checkmark: {
    fontSize: 20,
    color: '#10b981',
  },
  lockIcon: {
    fontSize: 20,
    color: '#94a3b8',
  },
  leaderboardContainer: {
    marginBottom: 20,
  },
  leaderboardCard: {
    flexDirection: 'row',
    alignItems: 'center',
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
  topThreeCard: {
    borderLeftWidth: 4,
    borderLeftColor: '#f59e0b',
  },
  currentUserCard: {
    backgroundColor: '#f0f9ff',
    borderWidth: 2,
    borderColor: '#667eea',
  },
  rankContainer: {
    marginRight: 15,
    alignItems: 'center',
    minWidth: 40,
  },
  rank: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#64748b',
  },
  topRank: {
    fontSize: 20,
  },
  userInfo: {
    flex: 1,
  },
  username: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 2,
  },
  currentUserText: {
    color: '#667eea',
  },
  userStats: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 2,
  },
  returnRate: {
    fontSize: 12,
    color: '#10b981',
    fontWeight: '600',
  },
  youBadge: {
    backgroundColor: '#667eea',
    color: 'white',
    fontSize: 10,
    fontWeight: 'bold',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  loadingContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 40,
  },
  loadingText: {
    marginTop: 10,
    fontSize: 14,
    color: '#64748b',
  },
  loginPrompt: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 40,
  },
  loginTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 10,
  },
  loginText: {
    fontSize: 16,
    color: '#64748b',
    textAlign: 'center',
  },
});

export default GamificationScreen;