import React, { memo, useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Animated,
  Easing,
} from 'react-native';
import { UserRanking, CompetitionStats } from '../../types';
import { DEFAULT_PERFORMANCE_TIERS, PerformanceTier } from './PerformanceTiers';

interface RankChange {
  direction: 'up' | 'down' | 'same';
  amount: number;
  isNew: boolean;
}

interface UserRankCardProps {
  userRanking: UserRanking;
  stats?: CompetitionStats | null;
  previousRank?: number;
  showProgress?: boolean;
  compact?: boolean;
  animated?: boolean;
  onPress?: () => void;
}

const UserRankCard: React.FC<UserRankCardProps> = ({
  userRanking,
  stats,
  previousRank,
  showProgress = true,
  compact = false,
  animated = true,
  onPress,
}) => {
  const [expandAnim] = useState(new Animated.Value(compact ? 0 : 1));
  const [bounceAnim] = useState(new Animated.Value(1));

  // Calculate rank change
  const getRankChange = (): RankChange => {
    if (!previousRank) {
      return { direction: 'same', amount: 0, isNew: true };
    }

    const change = previousRank - userRanking.rank;

    if (change > 0) {
      return { direction: 'up', amount: change, isNew: false };
    } else if (change < 0) {
      return { direction: 'down', amount: Math.abs(change), isNew: false };
    } else {
      return { direction: 'same', amount: 0, isNew: false };
    }
  };

  const rankChange = getRankChange();

  // Get user's current tier
  const getCurrentTier = (): PerformanceTier => {
    return DEFAULT_PERFORMANCE_TIERS.find(tier =>
      userRanking.score >= tier.minScore &&
      (!tier.maxScore || userRanking.score <= tier.maxScore)
    ) || DEFAULT_PERFORMANCE_TIERS[DEFAULT_PERFORMANCE_TIERS.length - 1];
  };

  const currentTier = getCurrentTier();

  // Calculate progress to next tier
  const getNextTierProgress = (): { nextTier: PerformanceTier | null; progress: number; pointsNeeded: number } => {
    const currentTierIndex = DEFAULT_PERFORMANCE_TIERS.findIndex(tier => tier.id === currentTier.id);
    const nextTier = currentTierIndex > 0 ? DEFAULT_PERFORMANCE_TIERS[currentTierIndex - 1] : null;

    if (!nextTier) {
      return { nextTier: null, progress: 1, pointsNeeded: 0 };
    }

    const currentMin = currentTier.minScore;
    const nextMin = nextTier.minScore;
    const progress = Math.min(1, Math.max(0, (userRanking.score - currentMin) / (nextMin - currentMin)));
    const pointsNeeded = Math.max(0, nextMin - userRanking.score);

    return { nextTier, progress, pointsNeeded };
  };

  const { nextTier, progress, pointsNeeded } = getNextTierProgress();

  // Format numbers
  const formatScore = (score: number | undefined | null): string => {
    if (score === undefined || score === null || isNaN(score)) return '0';
    if (score >= 1000000) return `${(score / 1000000).toFixed(1)}M`;
    if (score >= 1000) return `${(score / 1000).toFixed(1)}K`;
    return score.toString();
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

  const getRankChangeIcon = (): string => {
    switch (rankChange.direction) {
      case 'up': return '📈';
      case 'down': return '📉';
      case 'same': return '➡️';
    }
  };

  const getRankChangeColor = (): string => {
    switch (rankChange.direction) {
      case 'up': return '#10b981';
      case 'down': return '#ef4444';
      case 'same': return '#6b7280';
    }
  };

  const getRankChangeText = (): string => {
    if (rankChange.isNew) return 'Yeni katılım';
    if (rankChange.direction === 'same') return 'Değişiklik yok';
    return `${rankChange.amount} sıra ${rankChange.direction === 'up' ? 'yükseldi' : 'düştü'}`;
  };

  // Calculate percentile details
  const getPercentileDetails = () => {
    const totalParticipants = stats?.totalParticipants || userRanking.totalParticipants;
    const percentile = userRanking.percentile;

    let description = '';
    if (percentile <= 1) description = 'Efsane seviye! En elit %1\'de';
    else if (percentile <= 5) description = 'Mükemmel! En iyi %5\'te';
    else if (percentile <= 10) description = 'Harika! En iyi %10\'da';
    else if (percentile <= 25) description = 'Çok iyi! İlk çeyrek';
    else if (percentile <= 50) description = 'İyi! Ortalamanın üstünde';
    else if (percentile <= 75) description = 'Gelişiyor! Ortalama seviye';
    else description = 'Başlangıç seviyesi';

    return {
      percentile,
      description,
      totalParticipants,
      betterThan: Math.round((100 - percentile) * totalParticipants / 100),
    };
  };

  const percentileDetails = getPercentileDetails();

  // Animation effects
  const animateRankChange = useCallback(() => {
    if (!animated) return;

    Animated.sequence([
      Animated.timing(bounceAnim, {
        toValue: 1.1,
        duration: 150,
        easing: Easing.out(Easing.quad),
        useNativeDriver: true,
      }),
      Animated.timing(bounceAnim, {
        toValue: 1,
        duration: 150,
        easing: Easing.out(Easing.quad),
        useNativeDriver: true,
      }),
    ]).start();
  }, [animated, bounceAnim]);

  // Trigger animation when rank changes
  React.useEffect(() => {
    if (rankChange.direction !== 'same' && !rankChange.isNew) {
      animateRankChange();
    }
  }, [userRanking.rank, animateRankChange, rankChange]);

  const renderCompactView = () => (
    <TouchableOpacity
      style={[styles.compactCard, { borderColor: currentTier.color }]}
      onPress={onPress}
      activeOpacity={0.8}
    >
      <View style={styles.compactHeader}>
        <View style={styles.rankSection}>
          <Animated.View style={{ transform: [{ scale: bounceAnim }] }}>
            <Text style={styles.compactRank}>#{userRanking.rank}</Text>
          </Animated.View>
          <View style={styles.rankChangeIndicator}>
            <Text style={styles.rankChangeIcon}>{getRankChangeIcon()}</Text>
            <Text style={[styles.rankChangeAmount, { color: getRankChangeColor() }]}>
              {rankChange.amount > 0 ? rankChange.amount : ''}
            </Text>
          </View>
        </View>

        <View style={styles.compactInfo}>
          <View style={styles.tierContainer}>
            <Text style={[styles.tierIcon, { color: currentTier.color }]}>
              {currentTier.icon}
            </Text>
            <Text style={[styles.tierName, { color: currentTier.color }]}>
              {currentTier.name}
            </Text>
          </View>
          <Text style={styles.compactScore}>{formatScore(userRanking.score)} puan</Text>
        </View>

        <View style={styles.compactStats}>
          <Text style={[styles.compactReturn, { color: getReturnColor(userRanking.returnPercent) }]}>
            {formatPercentage(userRanking.returnPercent)}
          </Text>
          <Text style={styles.compactPercentile}>
            En iyi %{userRanking.percentile ? userRanking.percentile.toFixed(1) : '0.0'}
          </Text>
        </View>
      </View>
    </TouchableOpacity>
  );

  const renderFullView = () => (
    <TouchableOpacity
      style={[styles.fullCard, { borderColor: currentTier.color }]}
      onPress={onPress}
      activeOpacity={0.8}
    >
      {/* Header with rank and tier */}
      <View style={styles.cardHeader}>
        <View style={styles.rankDisplay}>
          <Animated.View style={{ transform: [{ scale: bounceAnim }] }}>
            <Text style={styles.fullRank}>#{userRanking.rank}</Text>
          </Animated.View>
          <View style={styles.rankChangeSection}>
            <Text style={styles.rankChangeIcon}>{getRankChangeIcon()}</Text>
            <Text style={[styles.rankChangeText, { color: getRankChangeColor() }]}>
              {getRankChangeText()}
            </Text>
          </View>
        </View>

        <View style={styles.tierSection}>
          <View style={[styles.tierBadge, { backgroundColor: currentTier.color }]}>
            <Text style={styles.tierBadgeIcon}>{currentTier.icon}</Text>
            <Text style={styles.tierBadgeText}>{currentTier.name}</Text>
          </View>
          <Text style={styles.tierDescription}>{currentTier.description}</Text>
        </View>
      </View>

      {/* Stats Grid */}
      <View style={styles.statsGrid}>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>{formatScore(userRanking.score)}</Text>
          <Text style={styles.statLabel}>Puan</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={[styles.statValue, { color: getReturnColor(userRanking.returnPercent) }]}>
            {formatPercentage(userRanking.returnPercent)}
          </Text>
          <Text style={styles.statLabel}>Getiri</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>%{userRanking.percentile ? userRanking.percentile.toFixed(1) : '0.0'}</Text>
          <Text style={styles.statLabel}>Dilim</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={[
            styles.statValue,
            { color: userRanking.isEligible ? '#10b981' : '#ef4444' }
          ]}>
            {userRanking.isEligible ? '✅' : '❌'}
          </Text>
          <Text style={styles.statLabel}>Uygun</Text>
        </View>
      </View>

      {/* Percentile Breakdown */}
      <View style={styles.percentileSection}>
        <Text style={styles.percentileTitle}>📊 Sıralama Detayları</Text>
        <Text style={styles.percentileDescription}>{percentileDetails.description}</Text>
        <View style={styles.percentileStats}>
          <Text style={styles.percentileText}>
            {percentileDetails.betterThan} kişiden daha iyi performans
          </Text>
          <Text style={styles.percentileText}>
            {percentileDetails.totalParticipants} toplam yarışmacı
          </Text>
        </View>
      </View>

      {/* Tier Progress */}
      {showProgress && nextTier && (
        <View style={styles.progressSection}>
          <View style={styles.progressHeader}>
            <Text style={styles.progressTitle}>
              🎯 {nextTier.name} Seviyesine İlerleme
            </Text>
            <Text style={styles.progressPercentage}>
              %{Math.round(progress * 100)}
            </Text>
          </View>

          <View style={styles.progressBarContainer}>
            <View style={styles.progressBar}>
              <Animated.View
                style={[
                  styles.progressFill,
                  {
                    width: `${progress * 100}%`,
                    backgroundColor: currentTier.color,
                  }
                ]}
              />
            </View>
          </View>

          <View style={styles.progressFooter}>
            <Text style={styles.progressText}>
              {formatScore(pointsNeeded)} puan daha gerekli
            </Text>
            <Text style={styles.nextTierIcon}>{nextTier.icon}</Text>
          </View>
        </View>
      )}

      {/* Eligibility Status */}
      {!userRanking.isEligible && userRanking.disqualificationReason && (
        <View style={styles.warningSection}>
          <Text style={styles.warningIcon}>⚠️</Text>
          <View style={styles.warningContent}>
            <Text style={styles.warningTitle}>Uygunluk Durumu</Text>
            <Text style={styles.warningText}>{userRanking.disqualificationReason}</Text>
          </View>
        </View>
      )}
    </TouchableOpacity>
  );

  return compact ? renderCompactView() : renderFullView();
};

const styles = StyleSheet.create({
  compactCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderLeftWidth: 4,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  compactHeader: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  rankSection: {
    alignItems: 'center',
    marginRight: 16,
  },
  compactRank: {
    fontSize: 24,
    fontWeight: '700',
    color: '#667eea',
    marginBottom: 4,
  },
  rankChangeIndicator: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 2,
  },
  rankChangeIcon: {
    fontSize: 12,
  },
  rankChangeAmount: {
    fontSize: 10,
    fontWeight: '600',
  },
  compactInfo: {
    flex: 1,
  },
  tierContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
    gap: 6,
  },
  tierIcon: {
    fontSize: 16,
  },
  tierName: {
    fontSize: 14,
    fontWeight: '600',
  },
  compactScore: {
    fontSize: 14,
    color: '#6b7280',
  },
  compactStats: {
    alignItems: 'flex-end',
  },
  compactReturn: {
    fontSize: 16,
    fontWeight: '700',
    marginBottom: 2,
  },
  compactPercentile: {
    fontSize: 12,
    color: '#6b7280',
  },
  fullCard: {
    backgroundColor: 'white',
    borderRadius: 16,
    padding: 20,
    marginBottom: 16,
    borderLeftWidth: 6,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.15,
    shadowRadius: 8,
    elevation: 5,
  },
  cardHeader: {
    marginBottom: 20,
  },
  rankDisplay: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 16,
  },
  fullRank: {
    fontSize: 36,
    fontWeight: '700',
    color: '#667eea',
  },
  rankChangeSection: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  rankChangeText: {
    fontSize: 14,
    fontWeight: '600',
  },
  tierSection: {
    alignItems: 'center',
  },
  tierBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
    marginBottom: 8,
    gap: 6,
  },
  tierBadgeIcon: {
    fontSize: 16,
    color: 'white',
  },
  tierBadgeText: {
    fontSize: 16,
    fontWeight: '700',
    color: 'white',
  },
  tierDescription: {
    fontSize: 14,
    color: '#6b7280',
    textAlign: 'center',
    lineHeight: 20,
  },
  statsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 20,
  },
  statItem: {
    alignItems: 'center',
    flex: 1,
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
  },
  percentileSection: {
    backgroundColor: '#f8fafc',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
  },
  percentileTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 8,
  },
  percentileDescription: {
    fontSize: 14,
    color: '#667eea',
    fontWeight: '600',
    marginBottom: 8,
  },
  percentileStats: {
    gap: 4,
  },
  percentileText: {
    fontSize: 13,
    color: '#6b7280',
  },
  progressSection: {
    backgroundColor: '#f8fafc',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
  },
  progressHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  progressTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
  },
  progressPercentage: {
    fontSize: 14,
    fontWeight: '700',
    color: '#667eea',
  },
  progressBarContainer: {
    marginBottom: 8,
  },
  progressBar: {
    height: 8,
    backgroundColor: '#e5e7eb',
    borderRadius: 4,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    borderRadius: 4,
  },
  progressFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  progressText: {
    fontSize: 12,
    color: '#6b7280',
  },
  nextTierIcon: {
    fontSize: 16,
  },
  warningSection: {
    flexDirection: 'row',
    backgroundColor: '#fef3c7',
    borderRadius: 12,
    padding: 16,
    borderLeftWidth: 4,
    borderLeftColor: '#f59e0b',
  },
  warningIcon: {
    fontSize: 20,
    marginRight: 12,
  },
  warningContent: {
    flex: 1,
  },
  warningTitle: {
    fontSize: 14,
    fontWeight: '700',
    color: '#d97706',
    marginBottom: 4,
  },
  warningText: {
    fontSize: 13,
    color: '#92400e',
    lineHeight: 18,
  },
});

export default memo(UserRankCard);