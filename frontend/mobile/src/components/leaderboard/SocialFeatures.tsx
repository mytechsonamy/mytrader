import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Modal,
  ScrollView,
  Alert,
  Switch,
  TextInput,
} from 'react-native';
import { LeaderboardEntry } from '../../types';

interface SocialFeaturesProps {
  visible: boolean;
  onClose: () => void;
  trader: LeaderboardEntry;
  isFollowing?: boolean;
  onFollowToggle?: (userId: string, following: boolean) => void;
  onCopyStrategy?: (userId: string, strategySettings: any) => void;
}

interface StrategySettings {
  copyAmount: number;
  maxRisk: number;
  autoExecute: boolean;
  notifications: boolean;
  stopLoss: number;
  takeProfit: number;
}

const SocialFeatures: React.FC<SocialFeaturesProps> = ({
  visible,
  onClose,
  trader,
  isFollowing = false,
  onFollowToggle,
  onCopyStrategy,
}) => {
  const [activeTab, setActiveTab] = useState<'profile' | 'strategies' | 'copy'>('profile');
  const [loading, setLoading] = useState(false);
  const [strategySettings, setStrategySettings] = useState<StrategySettings>({
    copyAmount: 1000,
    maxRisk: 5,
    autoExecute: false,
    notifications: true,
    stopLoss: 5,
    takeProfit: 10,
  });

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

  const getTierColor = (tier: string): string => {
    switch (tier) {
      case 'DIAMOND': return '#8b5cf6';
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

  const handleFollowToggle = useCallback(async () => {
    if (!onFollowToggle) return;

    setLoading(true);
    try {
      await onFollowToggle(trader.userId, !isFollowing);
      Alert.alert(
        'Başarılı!',
        isFollowing
          ? `${trader.displayName} adlı kullanıcıyı takip etmeyi bıraktınız.`
          : `${trader.displayName} adlı kullanıcıyı takip etmeye başladınız.`
      );
    } catch (error) {
      Alert.alert('Hata', 'İşlem gerçekleştirilemedi. Lütfen tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  }, [trader, isFollowing, onFollowToggle]);

  const handleCopyStrategy = useCallback(async () => {
    if (!onCopyStrategy) return;

    // Validation
    if (strategySettings.copyAmount < 100) {
      Alert.alert('Uyarı', 'Minimum kopyalama tutarı 100 ₺ olmalıdır.');
      return;
    }

    if (strategySettings.maxRisk > 20) {
      Alert.alert('Uyarı', 'Maksimum risk %20\'yi geçemez.');
      return;
    }

    Alert.alert(
      'Strateji Kopyalama Onayı',
      `${trader.displayName} adlı kullanıcının stratejisini ${formatCurrency(strategySettings.copyAmount)} tutarında kopyalamak istediğinizi onaylıyor musunuz?\n\nRisk: %${strategySettings.maxRisk}\nOtomatik: ${strategySettings.autoExecute ? 'Evet' : 'Hayır'}`,
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Onayla',
          style: 'default',
          onPress: async () => {
            setLoading(true);
            try {
              await onCopyStrategy(trader.userId, strategySettings);
              Alert.alert('Başarılı!', 'Strateji kopyalama ayarları kaydedildi.');
              onClose();
            } catch (error) {
              Alert.alert('Hata', 'Strateji kopyalanamadı. Lütfen tekrar deneyin.');
            } finally {
              setLoading(false);
            }
          },
        },
      ]
    );
  }, [trader, strategySettings, onCopyStrategy, onClose]);

  const renderProfileTab = () => (
    <ScrollView style={styles.tabContent} showsVerticalScrollIndicator={false}>
      {/* Trader Header */}
      <View style={styles.traderHeader}>
        <View style={styles.traderInfo}>
          <Text style={styles.traderName}>{trader.displayName}</Text>
          <View style={styles.traderTier}>
            <Text style={[styles.tierIcon, { color: getTierColor(trader.tier) }]}>
              {getTierIcon(trader.tier)}
            </Text>
            <Text style={[styles.tierText, { color: getTierColor(trader.tier) }]}>
              {trader.tier}
            </Text>
          </View>
        </View>
        <View style={styles.traderRank}>
          <Text style={styles.rankNumber}>#{trader.rank}</Text>
          <Text style={styles.rankLabel}>Sıralama</Text>
        </View>
      </View>

      {/* Performance Stats */}
      <View style={styles.statsSection}>
        <Text style={styles.sectionTitle}>📊 Performans İstatistikleri</Text>
        <View style={styles.statsGrid}>
          <View style={styles.statCard}>
            <Text style={styles.statValue}>{formatCurrency(trader.portfolioValue)}</Text>
            <Text style={styles.statLabel}>Portföy Değeri</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={[styles.statValue, { color: getReturnColor(trader.returnPercent) }]}>
              {formatPercentage(trader.returnPercent)}
            </Text>
            <Text style={styles.statLabel}>Getiri</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statValue}>{trader.winRate.toFixed(0)}%</Text>
            <Text style={styles.statLabel}>Kazanç Oranı</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statValue}>{trader.totalTrades}</Text>
            <Text style={styles.statLabel}>Toplam İşlem</Text>
          </View>
        </View>
      </View>

      {/* Achievements */}
      {trader.badges.length > 0 && (
        <View style={styles.achievementsSection}>
          <Text style={styles.sectionTitle}>🏅 Başarılar</Text>
          <View style={styles.badgesContainer}>
            {trader.badges.slice(0, 6).map((badge, index) => (
              <View key={index} style={styles.badgeItem}>
                <Text style={styles.badgeIcon}>🏆</Text>
                <Text style={styles.badgeText}>{badge}</Text>
              </View>
            ))}
            {trader.badges.length > 6 && (
              <View style={styles.badgeItem}>
                <Text style={styles.badgeIcon}>➕</Text>
                <Text style={styles.badgeText}>+{trader.badges.length - 6}</Text>
              </View>
            )}
          </View>
        </View>
      )}

      {/* Activity */}
      <View style={styles.activitySection}>
        <Text style={styles.sectionTitle}>📈 Son Aktivite</Text>
        <View style={styles.activityCard}>
          <Text style={styles.activityText}>
            Son 7 günde {Math.floor(trader.totalTrades / 4)} işlem gerçekleştirdi
          </Text>
          <Text style={styles.activityText}>
            Ortalama işlem büyüklüğü: {formatCurrency(trader.portfolioValue / trader.totalTrades)}
          </Text>
          <Text style={styles.activityText}>
            En aktif olduğu saat: 10:00-16:00
          </Text>
        </View>
      </View>

      {/* Risk Profile */}
      <View style={styles.riskSection}>
        <Text style={styles.sectionTitle}>⚠️ Risk Profili</Text>
        <View style={styles.riskCard}>
          <View style={styles.riskItem}>
            <Text style={styles.riskLabel}>Risk Seviyesi:</Text>
            <Text style={[styles.riskValue, { color: '#f59e0b' }]}>Orta</Text>
          </View>
          <View style={styles.riskItem}>
            <Text style={styles.riskLabel}>Maksimum Çekilme:</Text>
            <Text style={[styles.riskValue, { color: '#ef4444' }]}>-3.2%</Text>
          </View>
          <View style={styles.riskItem}>
            <Text style={styles.riskLabel}>Volatilite:</Text>
            <Text style={styles.riskValue}>±2.8%</Text>
          </View>
        </View>
      </View>
    </ScrollView>
  );

  const renderStrategiesTab = () => (
    <ScrollView style={styles.tabContent} showsVerticalScrollIndicator={false}>
      <Text style={styles.sectionTitle}>🎯 Aktif Stratejiler</Text>

      {/* Mock strategies */}
      {[
        { name: 'Trend Takip', success: 78, trades: 45, return: 12.5 },
        { name: 'Scalping Pro', success: 65, trades: 120, return: 8.3 },
        { name: 'Swing Trading', success: 82, trades: 28, return: 15.7 },
      ].map((strategy, index) => (
        <View key={index} style={styles.strategyCard}>
          <View style={styles.strategyHeader}>
            <Text style={styles.strategyName}>{strategy.name}</Text>
            <Text style={[styles.strategyReturn, { color: getReturnColor(strategy.return) }]}>
              {formatPercentage(strategy.return)}
            </Text>
          </View>
          <View style={styles.strategyStats}>
            <Text style={styles.strategyStatText}>
              Başarı: %{strategy.success}
            </Text>
            <Text style={styles.strategyStatText}>
              İşlemler: {strategy.trades}
            </Text>
          </View>
        </View>
      ))}

      <View style={styles.infoBox}>
        <Text style={styles.infoIcon}>💡</Text>
        <Text style={styles.infoText}>
          Bu kullanıcının stratejilerini kopyalayarak benzer sonuçlar elde edebilirsiniz.
          Kopyalama sekmesinden ayarlarınızı yapabilirsiniz.
        </Text>
      </View>
    </ScrollView>
  );

  const renderCopyTab = () => (
    <ScrollView style={styles.tabContent} showsVerticalScrollIndicator={false}>
      <Text style={styles.sectionTitle}>📋 Kopyalama Ayarları</Text>

      {/* Copy Amount */}
      <View style={styles.settingCard}>
        <Text style={styles.settingLabel}>Kopyalama Tutarı</Text>
        <TextInput
          style={styles.settingInput}
          value={strategySettings.copyAmount.toString()}
          onChangeText={(text) => setStrategySettings(prev => ({
            ...prev,
            copyAmount: parseInt(text) || 0
          }))}
          keyboardType="numeric"
          placeholder="1000"
        />
        <Text style={styles.settingHint}>Minimum: 100 ₺</Text>
      </View>

      {/* Max Risk */}
      <View style={styles.settingCard}>
        <Text style={styles.settingLabel}>Maksimum Risk (%)</Text>
        <TextInput
          style={styles.settingInput}
          value={strategySettings.maxRisk.toString()}
          onChangeText={(text) => setStrategySettings(prev => ({
            ...prev,
            maxRisk: parseFloat(text) || 0
          }))}
          keyboardType="numeric"
          placeholder="5"
        />
        <Text style={styles.settingHint}>Önerilen: %1-10 arası</Text>
      </View>

      {/* Stop Loss */}
      <View style={styles.settingCard}>
        <Text style={styles.settingLabel}>Stop Loss (%)</Text>
        <TextInput
          style={styles.settingInput}
          value={strategySettings.stopLoss.toString()}
          onChangeText={(text) => setStrategySettings(prev => ({
            ...prev,
            stopLoss: parseFloat(text) || 0
          }))}
          keyboardType="numeric"
          placeholder="5"
        />
      </View>

      {/* Take Profit */}
      <View style={styles.settingCard}>
        <Text style={styles.settingLabel}>Take Profit (%)</Text>
        <TextInput
          style={styles.settingInput}
          value={strategySettings.takeProfit.toString()}
          onChangeText={(text) => setStrategySettings(prev => ({
            ...prev,
            takeProfit: parseFloat(text) || 0
          }))}
          keyboardType="numeric"
          placeholder="10"
        />
      </View>

      {/* Auto Execute */}
      <View style={styles.settingCard}>
        <View style={styles.switchSetting}>
          <View>
            <Text style={styles.settingLabel}>Otomatik İşlem</Text>
            <Text style={styles.settingHint}>İşlemler otomatik olarak gerçekleştirilsin</Text>
          </View>
          <Switch
            value={strategySettings.autoExecute}
            onValueChange={(value) => setStrategySettings(prev => ({
              ...prev,
              autoExecute: value
            }))}
            trackColor={{ false: '#f3f4f6', true: '#667eea' }}
            thumbColor={strategySettings.autoExecute ? '#ffffff' : '#f9fafb'}
          />
        </View>
      </View>

      {/* Notifications */}
      <View style={styles.settingCard}>
        <View style={styles.switchSetting}>
          <View>
            <Text style={styles.settingLabel}>Bildirimler</Text>
            <Text style={styles.settingHint}>İşlem bildirimleri alın</Text>
          </View>
          <Switch
            value={strategySettings.notifications}
            onValueChange={(value) => setStrategySettings(prev => ({
              ...prev,
              notifications: value
            }))}
            trackColor={{ false: '#f3f4f6', true: '#667eea' }}
            thumbColor={strategySettings.notifications ? '#ffffff' : '#f9fafb'}
          />
        </View>
      </View>

      {/* Warning */}
      <View style={styles.warningBox}>
        <Text style={styles.warningIcon}>⚠️</Text>
        <Text style={styles.warningText}>
          Strateji kopyalama yatırım tavsiyesi değildir. Geçmiş performans gelecekteki sonuçları garanti etmez.
          Kendi risk toleransınızı göz önünde bulundurarak işlem yapın.
        </Text>
      </View>

      {/* Copy Button */}
      <TouchableOpacity
        style={[styles.copyButton, loading && styles.disabledButton]}
        onPress={handleCopyStrategy}
        disabled={loading}
      >
        <Text style={styles.copyButtonText}>
          {loading ? 'İşleniyor...' : '📋 Stratejiyi Kopyala'}
        </Text>
      </TouchableOpacity>
    </ScrollView>
  );

  const renderTabContent = () => {
    switch (activeTab) {
      case 'profile': return renderProfileTab();
      case 'strategies': return renderStrategiesTab();
      case 'copy': return renderCopyTab();
      default: return renderProfileTab();
    }
  };

  return (
    <Modal
      visible={visible}
      animationType="slide"
      presentationStyle="pageSheet"
      onRequestClose={onClose}
    >
      <View style={styles.container}>
        {/* Header */}
        <View style={styles.header}>
          <TouchableOpacity onPress={onClose} style={styles.closeButton}>
            <Text style={styles.closeButtonText}>✕</Text>
          </TouchableOpacity>
          <Text style={styles.headerTitle}>{trader.displayName}</Text>
          <TouchableOpacity
            style={[styles.followButton, isFollowing && styles.followingButton]}
            onPress={handleFollowToggle}
            disabled={loading}
          >
            <Text style={[styles.followButtonText, isFollowing && styles.followingButtonText]}>
              {isFollowing ? '✓ Takip Ediliyor' : '+ Takip Et'}
            </Text>
          </TouchableOpacity>
        </View>

        {/* Tabs */}
        <View style={styles.tabContainer}>
          {[
            { id: 'profile', label: '👤 Profil' },
            { id: 'strategies', label: '🎯 Stratejiler' },
            { id: 'copy', label: '📋 Kopyala' },
          ].map((tab) => (
            <TouchableOpacity
              key={tab.id}
              style={[styles.tab, activeTab === tab.id && styles.activeTab]}
              onPress={() => setActiveTab(tab.id as any)}
            >
              <Text style={[styles.tabText, activeTab === tab.id && styles.activeTabText]}>
                {tab.label}
              </Text>
            </TouchableOpacity>
          ))}
        </View>

        {/* Tab Content */}
        {renderTabContent()}
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 20,
    paddingTop: 60,
    paddingBottom: 16,
    backgroundColor: 'white',
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
  },
  closeButton: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: '#f3f4f6',
    alignItems: 'center',
    justifyContent: 'center',
  },
  closeButtonText: {
    fontSize: 16,
    color: '#6b7280',
    fontWeight: '600',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
  },
  followButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
  },
  followingButton: {
    backgroundColor: '#10b981',
  },
  followButtonText: {
    fontSize: 12,
    fontWeight: '600',
    color: 'white',
  },
  followingButtonText: {
    color: 'white',
  },
  tabContainer: {
    flexDirection: 'row',
    backgroundColor: 'white',
    paddingHorizontal: 20,
    paddingBottom: 4,
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
  },
  tab: {
    flex: 1,
    paddingVertical: 12,
    alignItems: 'center',
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  activeTab: {
    borderBottomColor: '#667eea',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#6b7280',
  },
  activeTabText: {
    color: '#667eea',
  },
  tabContent: {
    flex: 1,
    padding: 20,
  },
  traderHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: 'white',
    borderRadius: 16,
    padding: 20,
    marginBottom: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  traderInfo: {
    flex: 1,
  },
  traderName: {
    fontSize: 24,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 8,
  },
  traderTier: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  tierIcon: {
    fontSize: 16,
  },
  tierText: {
    fontSize: 14,
    fontWeight: '600',
  },
  traderRank: {
    alignItems: 'center',
  },
  rankNumber: {
    fontSize: 32,
    fontWeight: '700',
    color: '#667eea',
  },
  rankLabel: {
    fontSize: 12,
    color: '#6b7280',
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 16,
  },
  statsSection: {
    marginBottom: 20,
  },
  statsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
  },
  statCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    width: '47%',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
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
  achievementsSection: {
    marginBottom: 20,
  },
  badgesContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  badgeItem: {
    backgroundColor: 'white',
    borderRadius: 8,
    padding: 8,
    alignItems: 'center',
    minWidth: 60,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  badgeIcon: {
    fontSize: 16,
    marginBottom: 4,
  },
  badgeText: {
    fontSize: 10,
    color: '#6b7280',
    textAlign: 'center',
  },
  activitySection: {
    marginBottom: 20,
  },
  activityCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  activityText: {
    fontSize: 14,
    color: '#6b7280',
    marginBottom: 8,
  },
  riskSection: {
    marginBottom: 20,
  },
  riskCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  riskItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  riskLabel: {
    fontSize: 14,
    color: '#6b7280',
  },
  riskValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
  },
  strategyCard: {
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
  strategyHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  strategyName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1f2937',
  },
  strategyReturn: {
    fontSize: 16,
    fontWeight: '700',
  },
  strategyStats: {
    flexDirection: 'row',
    gap: 16,
  },
  strategyStatText: {
    fontSize: 14,
    color: '#6b7280',
  },
  infoBox: {
    backgroundColor: '#e0f2fe',
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginTop: 16,
  },
  infoIcon: {
    fontSize: 20,
    marginRight: 12,
  },
  infoText: {
    fontSize: 14,
    color: '#0891b2',
    flex: 1,
    lineHeight: 20,
  },
  settingCard: {
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
  settingLabel: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1f2937',
    marginBottom: 8,
  },
  settingInput: {
    borderWidth: 1,
    borderColor: '#d1d5db',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    color: '#1f2937',
    backgroundColor: '#f9fafb',
  },
  settingHint: {
    fontSize: 12,
    color: '#6b7280',
    marginTop: 4,
  },
  switchSetting: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  warningBox: {
    backgroundColor: '#fef3c7',
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: 20,
  },
  warningIcon: {
    fontSize: 20,
    marginRight: 12,
  },
  warningText: {
    fontSize: 14,
    color: '#d97706',
    flex: 1,
    lineHeight: 20,
  },
  copyButton: {
    backgroundColor: '#667eea',
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
    marginBottom: 20,
  },
  disabledButton: {
    backgroundColor: '#d1d5db',
  },
  copyButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: 'white',
  },
});

export default SocialFeatures;