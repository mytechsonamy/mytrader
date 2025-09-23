import React, { memo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ScrollView,
  Animated,
} from 'react-native';

export interface PerformanceTier {
  id: string;
  name: string;
  color: string;
  icon: string;
  minScore: number;
  maxScore?: number;
  benefits: string[];
  requirements: string[];
  description: string;
  isUnlocked?: boolean;
  progress?: number; // 0-1 for progress towards this tier
}

interface PerformanceTiersProps {
  currentScore: number;
  currentTier?: PerformanceTier;
  tiers: PerformanceTier[];
  showProgress?: boolean;
  compact?: boolean;
  onTierPress?: (tier: PerformanceTier) => void;
}

// Default tier system for Turkish market
export const DEFAULT_PERFORMANCE_TIERS: PerformanceTier[] = [
  {
    id: 'diamond',
    name: 'Elmas',
    color: '#8b5cf6',
    icon: '💎',
    minScore: 10000,
    benefits: [
      'VIP müşteri desteği',
      'Özel analiz raporları',
      'Erken erişim özellikleri',
      'Bireysel strateji danışmanlığı',
      'Aylık performans toplantısı'
    ],
    requirements: [
      '10.000+ puan',
      'En az 50 başarılı işlem',
      '%75+ kazanç oranı',
      'Sürekli aktif katılım'
    ],
    description: 'En seçkin trading seviyesi. Sadece en başarılı strategistler bu seviyeye ulaşabilir.',
  },
  {
    id: 'platinum',
    name: 'Platin',
    color: '#06b6d4',
    icon: '🏆',
    minScore: 5000,
    maxScore: 9999,
    benefits: [
      'Gelişmiş analizler',
      'Grup danışmanlığı',
      'Özel etkinliklere davet',
      'Haftalık piyasa bülteni',
      'Premium strateji şablonları'
    ],
    requirements: [
      '5.000-9.999 puan',
      'En az 30 başarılı işlem',
      '%65+ kazanç oranı',
      'Düzenli katılım'
    ],
    description: 'İleri seviye trading becerileri. Deneyimli strategistler için özel avantajlar.',
  },
  {
    id: 'gold',
    name: 'Altın',
    color: '#eab308',
    icon: '🥇',
    minScore: 2500,
    maxScore: 4999,
    benefits: [
      'Haftalık analiz raporu',
      'Gelişmiş strateji önerileri',
      'Topluluk erişimi',
      'Eğitim webinarları',
      'Risk yönetimi araçları'
    ],
    requirements: [
      '2.500-4.999 puan',
      'En az 20 başarılı işlem',
      '%55+ kazanç oranı',
      'Haftalık aktif katılım'
    ],
    description: 'Gelişmekte olan trading becerilerinizi destekleyecek kapsamlı kaynaklar.',
  },
  {
    id: 'silver',
    name: 'Gümüş',
    color: '#64748b',
    icon: '🥈',
    minScore: 1000,
    maxScore: 2499,
    benefits: [
      'Temel analizler',
      'E-posta bildirimleri',
      'Giriş seviyesi eğitimler',
      'Topluluk forumu erişimi',
      'Temel risk araçları'
    ],
    requirements: [
      '1.000-2.499 puan',
      'En az 10 başarılı işlem',
      '%45+ kazanç oranı',
      'Düzenli katılım'
    ],
    description: 'Trading yolculuğunuza başlarken size yardımcı olacak temel araçlar.',
  },
  {
    id: 'bronze',
    name: 'Bronz',
    color: '#dc2626',
    icon: '🥉',
    minScore: 0,
    maxScore: 999,
    benefits: [
      'Temel platform özellikleri',
      'Günlük fiyat uyarıları',
      'Basit analiz araçları',
      'Topluluk okuma erişimi'
    ],
    requirements: [
      '0-999 puan',
      'Platform kaydı',
      'İlk işlem tamamlama'
    ],
    description: 'Trading serüveninizin başlangıcı. Temel özelliklerle tanışın.',
  },
];

const PerformanceTiers: React.FC<PerformanceTiersProps> = ({
  currentScore,
  currentTier,
  tiers = DEFAULT_PERFORMANCE_TIERS,
  showProgress = true,
  compact = false,
  onTierPress,
}) => {

  // Calculate current tier if not provided
  const getCurrentTier = (): PerformanceTier => {
    if (currentTier) return currentTier;
    return tiers.find(tier =>
      currentScore >= tier.minScore &&
      (!tier.maxScore || currentScore <= tier.maxScore)
    ) || tiers[tiers.length - 1];
  };

  const activeUserTier = getCurrentTier();

  // Calculate progress to next tier
  const getProgressToNextTier = (): { progress: number; nextTier: PerformanceTier | null } => {
    const currentTierIndex = tiers.findIndex(tier => tier.id === activeUserTier.id);
    const nextTier = currentTierIndex > 0 ? tiers[currentTierIndex - 1] : null;

    if (!nextTier) {
      return { progress: 1, nextTier: null }; // Max tier reached
    }

    const currentMin = activeUserTier.minScore;
    const nextMin = nextTier.minScore;
    const progress = Math.min(1, Math.max(0, (currentScore - currentMin) / (nextMin - currentMin)));

    return { progress, nextTier };
  };

  const { progress, nextTier } = getProgressToNextTier();

  const formatScore = (score: number): string => {
    if (score >= 1000000) return `${(score / 1000000).toFixed(1)}M`;
    if (score >= 1000) return `${(score / 1000).toFixed(1)}K`;
    return score.toString();
  };

  const renderCompactView = () => (
    <View style={styles.compactContainer}>
      <View style={styles.currentTierCard}>
        <View style={styles.tierIconContainer}>
          <Text style={[styles.tierIcon, { color: activeUserTier.color }]}>
            {activeUserTier.icon}
          </Text>
        </View>

        <View style={styles.tierInfo}>
          <Text style={[styles.tierName, { color: activeUserTier.color }]}>
            {activeUserTier.name}
          </Text>
          <Text style={styles.currentScore}>
            {formatScore(currentScore)} puan
          </Text>
        </View>

        {showProgress && nextTier && (
          <View style={styles.progressContainer}>
            <View style={styles.progressBar}>
              <Animated.View
                style={[
                  styles.progressFill,
                  {
                    width: `${progress * 100}%`,
                    backgroundColor: activeUserTier.color
                  }
                ]}
              />
            </View>
            <Text style={styles.progressText}>
              {nextTier.name} seviyesine {formatScore(nextTier.minScore - currentScore)} puan kaldı
            </Text>
          </View>
        )}
      </View>
    </View>
  );

  const renderFullView = () => (
    <ScrollView style={styles.fullContainer} showsVerticalScrollIndicator={false}>
      <Text style={styles.sectionTitle}>🏆 Performans Seviyeleri</Text>

      {/* Current Tier Highlight */}
      <View style={[styles.currentTierHighlight, { borderColor: activeUserTier.color }]}>
        <View style={styles.currentTierHeader}>
          <Text style={[styles.currentTierIcon, { color: activeUserTier.color }]}>
            {activeUserTier.icon}
          </Text>
          <View style={styles.currentTierDetails}>
            <Text style={[styles.currentTierName, { color: activeUserTier.color }]}>
              Mevcut Seviyeniz: {activeUserTier.name}
            </Text>
            <Text style={styles.currentTierScore}>
              {formatScore(currentScore)} / {nextTier ? formatScore(nextTier.minScore) : '∞'} puan
            </Text>
          </View>
        </View>

        {showProgress && nextTier && (
          <View style={styles.progressSection}>
            <View style={styles.progressBar}>
              <Animated.View
                style={[
                  styles.progressFill,
                  {
                    width: `${progress * 100}%`,
                    backgroundColor: activeUserTier.color
                  }
                ]}
              />
            </View>
            <View style={styles.progressLabels}>
              <Text style={styles.progressLabel}>{Math.round(progress * 100)}% tamamlandı</Text>
              <Text style={styles.progressLabel}>
                {formatScore(nextTier.minScore - currentScore)} puan kaldı
              </Text>
            </View>
          </View>
        )}

        <Text style={styles.tierDescription}>{activeUserTier.description}</Text>
      </View>

      {/* All Tiers */}
      <Text style={styles.subsectionTitle}>Tüm Seviyeler</Text>
      {tiers.map((tier, index) => {
        const isCurrentTier = tier.id === activeUserTier.id;
        const isUnlocked = currentScore >= tier.minScore;

        return (
          <TouchableOpacity
            key={tier.id}
            style={[
              styles.tierCard,
              isCurrentTier && styles.activeTierCard,
              !isUnlocked && styles.lockedTierCard,
            ]}
            onPress={() => onTierPress?.(tier)}
            activeOpacity={0.8}
          >
            <View style={styles.tierCardHeader}>
              <View style={styles.tierIconBadge}>
                <Text style={[
                  styles.tierCardIcon,
                  { color: isUnlocked ? tier.color : '#9ca3af' }
                ]}>
                  {tier.icon}
                </Text>
                {isCurrentTier && (
                  <View style={[styles.currentBadge, { backgroundColor: tier.color }]}>
                    <Text style={styles.currentBadgeText}>Mevcut</Text>
                  </View>
                )}
              </View>

              <View style={styles.tierCardInfo}>
                <Text style={[
                  styles.tierCardName,
                  { color: isUnlocked ? tier.color : '#9ca3af' }
                ]}>
                  {tier.name}
                </Text>
                <Text style={[
                  styles.tierCardScore,
                  { color: isUnlocked ? '#374151' : '#9ca3af' }
                ]}>
                  {formatScore(tier.minScore)}
                  {tier.maxScore ? ` - ${formatScore(tier.maxScore)}` : '+'} puan
                </Text>
              </View>

              {!isUnlocked && (
                <View style={styles.lockIcon}>
                  <Text style={styles.lockText}>🔒</Text>
                </View>
              )}
            </View>

            <Text style={[
              styles.tierCardDescription,
              { color: isUnlocked ? '#6b7280' : '#9ca3af' }
            ]}>
              {tier.description}
            </Text>

            {/* Benefits Preview */}
            <View style={styles.benefitsPreview}>
              <Text style={[
                styles.benefitsTitle,
                { color: isUnlocked ? '#374151' : '#9ca3af' }
              ]}>
                Avantajlar:
              </Text>
              {tier.benefits.slice(0, 2).map((benefit, idx) => (
                <Text key={idx} style={[
                  styles.benefitText,
                  { color: isUnlocked ? '#6b7280' : '#9ca3af' }
                ]}>
                  • {benefit}
                </Text>
              ))}
              {tier.benefits.length > 2 && (
                <Text style={[
                  styles.moreBenefits,
                  { color: isUnlocked ? tier.color : '#9ca3af' }
                ]}>
                  +{tier.benefits.length - 2} avantaj daha
                </Text>
              )}
            </View>
          </TouchableOpacity>
        );
      })}
    </ScrollView>
  );

  return compact ? renderCompactView() : renderFullView();
};

const styles = StyleSheet.create({
  compactContainer: {
    padding: 16,
  },
  currentTierCard: {
    backgroundColor: 'white',
    borderRadius: 16,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  tierIconContainer: {
    alignItems: 'center',
    marginBottom: 12,
  },
  tierIcon: {
    fontSize: 32,
  },
  tierInfo: {
    alignItems: 'center',
    marginBottom: 16,
  },
  tierName: {
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 4,
  },
  currentScore: {
    fontSize: 14,
    color: '#6b7280',
  },
  progressContainer: {
    marginTop: 8,
  },
  progressBar: {
    height: 8,
    backgroundColor: '#e5e7eb',
    borderRadius: 4,
    marginBottom: 8,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    borderRadius: 4,
  },
  progressText: {
    fontSize: 12,
    color: '#6b7280',
    textAlign: 'center',
  },
  fullContainer: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  sectionTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 16,
    paddingHorizontal: 20,
  },
  subsectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#374151',
    marginBottom: 12,
    marginTop: 24,
    paddingHorizontal: 20,
  },
  currentTierHighlight: {
    backgroundColor: 'white',
    marginHorizontal: 20,
    borderRadius: 16,
    padding: 20,
    marginBottom: 16,
    borderWidth: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.15,
    shadowRadius: 8,
    elevation: 5,
  },
  currentTierHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  currentTierIcon: {
    fontSize: 32,
    marginRight: 16,
  },
  currentTierDetails: {
    flex: 1,
  },
  currentTierName: {
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 4,
  },
  currentTierScore: {
    fontSize: 14,
    color: '#6b7280',
  },
  progressSection: {
    marginBottom: 16,
  },
  progressLabels: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 4,
  },
  progressLabel: {
    fontSize: 12,
    color: '#6b7280',
  },
  tierDescription: {
    fontSize: 14,
    color: '#6b7280',
    lineHeight: 20,
  },
  tierCard: {
    backgroundColor: 'white',
    marginHorizontal: 20,
    marginBottom: 12,
    borderRadius: 16,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  activeTierCard: {
    borderWidth: 2,
    borderColor: '#667eea',
    backgroundColor: '#f0f9ff',
  },
  lockedTierCard: {
    opacity: 0.6,
  },
  tierCardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
  },
  tierIconBadge: {
    position: 'relative',
    marginRight: 16,
  },
  tierCardIcon: {
    fontSize: 24,
  },
  currentBadge: {
    position: 'absolute',
    top: -8,
    right: -8,
    borderRadius: 8,
    paddingHorizontal: 6,
    paddingVertical: 2,
  },
  currentBadgeText: {
    fontSize: 8,
    fontWeight: '600',
    color: 'white',
  },
  tierCardInfo: {
    flex: 1,
  },
  tierCardName: {
    fontSize: 16,
    fontWeight: '700',
    marginBottom: 2,
  },
  tierCardScore: {
    fontSize: 14,
    fontWeight: '500',
  },
  lockIcon: {
    marginLeft: 12,
  },
  lockText: {
    fontSize: 16,
  },
  tierCardDescription: {
    fontSize: 13,
    lineHeight: 18,
    marginBottom: 12,
  },
  benefitsPreview: {
    borderTopWidth: 1,
    borderTopColor: '#e5e7eb',
    paddingTop: 12,
  },
  benefitsTitle: {
    fontSize: 12,
    fontWeight: '600',
    marginBottom: 6,
  },
  benefitText: {
    fontSize: 12,
    lineHeight: 16,
    marginBottom: 2,
  },
  moreBenefits: {
    fontSize: 12,
    fontWeight: '500',
    marginTop: 4,
  },
});

export default memo(PerformanceTiers);