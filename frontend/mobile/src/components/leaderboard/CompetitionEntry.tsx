import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Modal,
  ScrollView,
  ActivityIndicator,
  Alert,
  Animated,
} from 'react-native';
import { CompetitionStats, UserRanking } from '../../types';
import { apiService } from '../../services/api';

interface CompetitionEntryProps {
  visible: boolean;
  onClose: () => void;
  onSuccess: () => void;
  stats?: CompetitionStats | null;
  userRanking?: UserRanking | null;
}

interface EntryStep {
  id: string;
  title: string;
  description: string;
  icon: string;
  completed?: boolean;
  required: boolean;
}

const CompetitionEntry: React.FC<CompetitionEntryProps> = ({
  visible,
  onClose,
  onSuccess,
  stats,
  userRanking,
}) => {
  const [loading, setLoading] = useState(false);
  const [currentStep, setCurrentStep] = useState(0);
  const [fadeAnim] = useState(new Animated.Value(0));
  const [agreedToRules, setAgreedToRules] = useState(false);

  // Entry steps configuration
  const entrySteps: EntryStep[] = [
    {
      id: 'welcome',
      title: 'Strategist Yarışmasına Hoş Geldiniz!',
      description: 'Trading becerilerinizi test edin ve ödüller kazanın. En iyi strategistler arasında yerinizi alın.',
      icon: '🏆',
      required: true,
    },
    {
      id: 'requirements',
      title: 'Katılım Şartları',
      description: 'Yarışmaya katılabilmek için aşağıdaki şartları sağlamanız gerekmektedir.',
      icon: '📋',
      required: true,
    },
    {
      id: 'rules',
      title: 'Yarışma Kuralları',
      description: 'Adil ve eğlenceli bir yarışma için kurallarımızı okuyun ve kabul edin.',
      icon: '⚖️',
      required: true,
    },
    {
      id: 'strategy',
      title: 'Strateji Seçimi',
      description: 'Yarışmada kullanacağınız trading stratejisini seçin veya oluşturun.',
      icon: '🎯',
      required: false,
    },
    {
      id: 'confirmation',
      title: 'Katılımı Onayla',
      description: 'Tüm bilgileri kontrol edin ve yarışmaya katılımınızı onaylayın.',
      icon: '✅',
      required: true,
    },
  ];

  React.useEffect(() => {
    if (visible) {
      Animated.timing(fadeAnim, {
        toValue: 1,
        duration: 300,
        useNativeDriver: true,
      }).start();
    }
  }, [visible, fadeAnim]);

  const handleJoinCompetition = useCallback(async () => {
    if (!agreedToRules) {
      Alert.alert('Uyarı', 'Katılım için yarışma kurallarını kabul etmelisiniz.');
      return;
    }

    setLoading(true);
    try {
      const result = await apiService.joinCompetition();
      if (result.success) {
        Alert.alert(
          'Tebrikler! 🎉',
          'Strategist Yarışmasına başarıyla katıldınız! Artık sıralamada yerinizi alabilir ve ödüller için yarışabilirsiniz.',
          [
            {
              text: 'Harika!',
              onPress: () => {
                onSuccess();
                onClose();
              }
            }
          ]
        );
      } else {
        Alert.alert('Hata', result.message || 'Yarışmaya katılırken bir hata oluştu.');
      }
    } catch (error) {
      Alert.alert('Hata', 'Bağlantı hatası. Lütfen tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  }, [agreedToRules, onSuccess, onClose]);

  const renderWelcomeStep = () => (
    <View style={styles.stepContent}>
      <Text style={styles.stepIcon}>🏆</Text>
      <Text style={styles.stepTitle}>Strategist Yarışmasına Hoş Geldiniz!</Text>
      <Text style={styles.stepDescription}>
        En büyük trading yarışmasına katılın ve becerilerinizi diğer strategistlerle kıyaslayın.
      </Text>

      {stats && (
        <View style={styles.statsGrid}>
          <View style={styles.statCard}>
            <Text style={styles.statValue}>{stats.totalParticipants}</Text>
            <Text style={styles.statLabel}>Aktif Yarışmacı</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statValue}>{Math.round(stats.totalPrizePool / 1000)}K ₺</Text>
            <Text style={styles.statLabel}>Ödül Havuzu</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statValue}>{stats.minimumTrades}</Text>
            <Text style={styles.statLabel}>Min. İşlem</Text>
          </View>
        </View>
      )}

      <View style={styles.highlightBox}>
        <Text style={styles.highlightIcon}>💰</Text>
        <Text style={styles.highlightTitle}>Bu Haftanın Ödülleri</Text>
        {Array.isArray(stats?.prizes) ? stats.prizes.slice(0, 3).map((prize, index) => (
          <Text key={index} style={styles.prizeText}>
            #{prize.rank}: {prize.amount.toLocaleString('tr-TR')} {prize.currency}
          </Text>
        )) : null}
      </View>
    </View>
  );

  const renderRequirementsStep = () => (
    <View style={styles.stepContent}>
      <Text style={styles.stepIcon}>📋</Text>
      <Text style={styles.stepTitle}>Katılım Şartları</Text>

      <View style={styles.requirementsList}>
        {Array.isArray(stats?.eligibilityRequirements) ? stats.eligibilityRequirements.map((requirement, index) => (
          <View key={index} style={styles.requirementItem}>
            <Text style={styles.requirementIcon}>✅</Text>
            <Text style={styles.requirementText}>{requirement}</Text>
          </View>
        )) : null}
      </View>

      <View style={styles.warningBox}>
        <Text style={styles.warningIcon}>⚠️</Text>
        <Text style={styles.warningTitle}>Önemli Notlar</Text>
        <Text style={styles.warningText}>
          • Yarışma süresi boyunca en az {stats?.minimumTrades} işlem yapmalısınız{'\n'}
          • Minimum {stats?.minimumPortfolioValue.toLocaleString('tr-TR')} ₺ portföy değeri gereklidir{'\n'}
          • Sahtekarlık veya manipülasyon tespit edilirse diskalifiye edilirsiniz
        </Text>
      </View>
    </View>
  );

  const renderRulesStep = () => (
    <View style={styles.stepContent}>
      <Text style={styles.stepIcon}>⚖️</Text>
      <Text style={styles.stepTitle}>Yarışma Kuralları</Text>

      <ScrollView style={styles.rulesContainer} showsVerticalScrollIndicator={false}>
        <View style={styles.ruleSection}>
          <Text style={styles.ruleSectionTitle}>🎯 Genel Kurallar</Text>
          <Text style={styles.ruleText}>
            • Yarışma her hafta Pazartesi 00:00'da başlar, Pazar 23:59'da sona erer{'\n'}
            • Tüm işlemler gerçek zamanlı olarak değerlendirilir{'\n'}
            • Sadece platform üzerinden yapılan işlemler geçerlidir{'\n'}
            • Birden fazla hesap kullanımı yasaktır
          </Text>
        </View>

        <View style={styles.ruleSection}>
          <Text style={styles.ruleSectionTitle}>📊 Puanlama Sistemi</Text>
          <Text style={styles.ruleText}>
            • Getiri oranı (50%): Portföyünüzün haftalık performansı{'\n'}
            • Kazanç oranı (25%): Başarılı işlem yüzdesi{'\n'}
            • İşlem sayısı (15%): Minimum şartları aşan aktif trading{'\n'}
            • Risk yönetimi (10%): Maksimum çekilme oranı
          </Text>
        </View>

        <View style={styles.ruleSection}>
          <Text style={styles.ruleSectionTitle}>🏆 Ödüller</Text>
          <Text style={styles.ruleText}>
            • Haftalık sıralamaya göre ödüller dağıtılır{'\n'}
            • Ödüller yarışma bitiminden 48 saat içinde hesaplara yatırılır{'\n'}
            • Vergi kesintileri yasal düzenlemelere göre yapılır
          </Text>
        </View>

        <View style={styles.ruleSection}>
          <Text style={styles.ruleSectionTitle}>❌ Yasaklar</Text>
          <Text style={styles.ruleText}>
            • Otomatik trading botları kullanımı{'\n'}
            • Piyasa manipülasyonu girişimleri{'\n'}
            • Diğer yarışmacılarla işbirliği{'\n'}
            • Sahte hesap veya kimlik kullanımı
          </Text>
        </View>
      </ScrollView>

      <TouchableOpacity
        style={styles.checkboxContainer}
        onPress={() => setAgreedToRules(!agreedToRules)}
      >
        <View style={[styles.checkbox, agreedToRules && styles.checkedBox]}>
          {agreedToRules && <Text style={styles.checkmark}>✓</Text>}
        </View>
        <Text style={styles.checkboxText}>
          Yukarıdaki tüm kuralları okudum ve kabul ediyorum
        </Text>
      </TouchableOpacity>
    </View>
  );

  const renderStrategyStep = () => (
    <View style={styles.stepContent}>
      <Text style={styles.stepIcon}>🎯</Text>
      <Text style={styles.stepTitle}>Strateji Seçimi</Text>
      <Text style={styles.stepDescription}>
        Yarışmada kullanacağınız trading stratejisini seçebilir veya yeni bir strateji oluşturabilirsiniz.
      </Text>

      <View style={styles.strategyOptions}>
        <TouchableOpacity style={styles.strategyOption}>
          <Text style={styles.strategyIcon}>📈</Text>
          <Text style={styles.strategyTitle}>Mevcut Stratejimi Kullan</Text>
          <Text style={styles.strategyDescription}>
            Var olan stratejilerinizi yarışmada kullanın
          </Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.strategyOption}>
          <Text style={styles.strategyIcon}>⚡</Text>
          <Text style={styles.strategyTitle}>Hızlı Strateji Oluştur</Text>
          <Text style={styles.strategyDescription}>
            Şablon kullanarak hızlıca yeni strateji oluşturun
          </Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.strategyOption}>
          <Text style={styles.strategyIcon}>🔧</Text>
          <Text style={styles.strategyTitle}>Gelişmiş Strateji</Text>
          <Text style={styles.strategyDescription}>
            Detaylı parametrelerle özelleştirilmiş strateji
          </Text>
        </TouchableOpacity>
      </View>

      <View style={styles.infoBox}>
        <Text style={styles.infoIcon}>💡</Text>
        <Text style={styles.infoText}>
          Strateji seçimini daha sonra da değiştirebilirsiniz. Şimdilik atlayarak yarışmaya katılabilirsiniz.
        </Text>
      </View>
    </View>
  );

  const renderConfirmationStep = () => (
    <View style={styles.stepContent}>
      <Text style={styles.stepIcon}>✅</Text>
      <Text style={styles.stepTitle}>Katılımı Onayla</Text>
      <Text style={styles.stepDescription}>
        Tüm bilgileri kontrol ettiniz. Strategist Yarışmasına katılmaya hazır mısınız?
      </Text>

      <View style={styles.confirmationSummary}>
        <View style={styles.summaryRow}>
          <Text style={styles.summaryLabel}>Yarışma Türü:</Text>
          <Text style={styles.summaryValue}>Haftalık Strategist</Text>
        </View>
        <View style={styles.summaryRow}>
          <Text style={styles.summaryLabel}>Başlangıç Tarihi:</Text>
          <Text style={styles.summaryValue}>
            {stats?.currentPeriodStart ? new Date(stats.currentPeriodStart).toLocaleDateString('tr-TR') : '-'}
          </Text>
        </View>
        <View style={styles.summaryRow}>
          <Text style={styles.summaryLabel}>Bitiş Tarihi:</Text>
          <Text style={styles.summaryValue}>
            {stats?.currentPeriodEnd ? new Date(stats.currentPeriodEnd).toLocaleDateString('tr-TR') : '-'}
          </Text>
        </View>
        <View style={styles.summaryRow}>
          <Text style={styles.summaryLabel}>Kuralları Kabul:</Text>
          <Text style={[styles.summaryValue, { color: agreedToRules ? '#10b981' : '#ef4444' }]}>
            {agreedToRules ? '✅ Evet' : '❌ Hayır'}
          </Text>
        </View>
      </View>

      <View style={styles.finalWarning}>
        <Text style={styles.warningIcon}>🎯</Text>
        <Text style={styles.finalWarningText}>
          Katıldıktan sonra yarışma kurallarına uymakla yükümlüsünüz. Başarılar!
        </Text>
      </View>
    </View>
  );

  const renderStepContent = () => {
    switch (currentStep) {
      case 0: return renderWelcomeStep();
      case 1: return renderRequirementsStep();
      case 2: return renderRulesStep();
      case 3: return renderStrategyStep();
      case 4: return renderConfirmationStep();
      default: return renderWelcomeStep();
    }
  };

  const canGoNext = () => {
    if (currentStep === 2) return agreedToRules; // Rules step requires agreement
    return true;
  };

  const isLastStep = currentStep === entrySteps.length - 1;

  return (
    <Modal
      visible={visible}
      animationType="slide"
      presentationStyle="pageSheet"
      onRequestClose={onClose}
    >
      <Animated.View style={[styles.container, { opacity: fadeAnim }]}>
        <View style={styles.header}>
          <TouchableOpacity onPress={onClose} style={styles.closeButton}>
            <Text style={styles.closeButtonText}>✕</Text>
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Yarışmaya Katıl</Text>
          <View style={styles.stepIndicator}>
            <Text style={styles.stepText}>{currentStep + 1}/{entrySteps.length}</Text>
          </View>
        </View>

        <View style={styles.progressBar}>
          <View style={[
            styles.progressFill,
            { width: `${((currentStep + 1) / entrySteps.length) * 100}%` }
          ]} />
        </View>

        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          {renderStepContent()}
        </ScrollView>

        <View style={styles.footer}>
          <TouchableOpacity
            style={[styles.button, styles.secondaryButton]}
            onPress={currentStep > 0 ? () => setCurrentStep(currentStep - 1) : onClose}
          >
            <Text style={styles.secondaryButtonText}>
              {currentStep > 0 ? 'Geri' : 'İptal'}
            </Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[
              styles.button,
              styles.primaryButton,
              (!canGoNext() || loading) && styles.disabledButton
            ]}
            onPress={isLastStep ? handleJoinCompetition : () => setCurrentStep(currentStep + 1)}
            disabled={!canGoNext() || loading}
          >
            {loading ? (
              <ActivityIndicator size="small" color="white" />
            ) : (
              <Text style={styles.primaryButtonText}>
                {isLastStep ? '🚀 Yarışmaya Katıl' : 'İleri'}
              </Text>
            )}
          </TouchableOpacity>
        </View>
      </Animated.View>
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
  stepIndicator: {
    backgroundColor: '#667eea',
    paddingHorizontal: 12,
    paddingVertical: 4,
    borderRadius: 12,
  },
  stepText: {
    fontSize: 12,
    fontWeight: '600',
    color: 'white',
  },
  progressBar: {
    height: 4,
    backgroundColor: '#e5e7eb',
  },
  progressFill: {
    height: '100%',
    backgroundColor: '#667eea',
  },
  content: {
    flex: 1,
  },
  stepContent: {
    padding: 20,
  },
  stepIcon: {
    fontSize: 48,
    textAlign: 'center',
    marginBottom: 16,
  },
  stepTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: '#1f2937',
    textAlign: 'center',
    marginBottom: 12,
  },
  stepDescription: {
    fontSize: 16,
    color: '#6b7280',
    textAlign: 'center',
    lineHeight: 24,
    marginBottom: 24,
  },
  statsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 24,
  },
  statCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    flex: 1,
    marginHorizontal: 4,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  statValue: {
    fontSize: 20,
    fontWeight: '700',
    color: '#667eea',
    marginBottom: 4,
  },
  statLabel: {
    fontSize: 12,
    color: '#6b7280',
    textAlign: 'center',
  },
  highlightBox: {
    backgroundColor: '#667eea',
    borderRadius: 16,
    padding: 20,
    alignItems: 'center',
  },
  highlightIcon: {
    fontSize: 32,
    marginBottom: 8,
  },
  highlightTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: 'white',
    marginBottom: 12,
  },
  prizeText: {
    fontSize: 14,
    color: 'white',
    marginBottom: 4,
  },
  requirementsList: {
    marginBottom: 24,
  },
  requirementItem: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
    backgroundColor: 'white',
    padding: 16,
    borderRadius: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  requirementIcon: {
    fontSize: 16,
    marginRight: 12,
  },
  requirementText: {
    fontSize: 14,
    color: '#374151',
    flex: 1,
  },
  warningBox: {
    backgroundColor: '#fef3c7',
    borderRadius: 12,
    padding: 16,
    borderLeftWidth: 4,
    borderLeftColor: '#f59e0b',
  },
  warningIcon: {
    fontSize: 20,
    marginBottom: 8,
  },
  warningTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#d97706',
    marginBottom: 8,
  },
  warningText: {
    fontSize: 14,
    color: '#92400e',
    lineHeight: 20,
  },
  rulesContainer: {
    maxHeight: 300,
    marginBottom: 20,
  },
  ruleSection: {
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
  ruleSectionTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 8,
  },
  ruleText: {
    fontSize: 14,
    color: '#6b7280',
    lineHeight: 20,
  },
  checkboxContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 16,
  },
  checkbox: {
    width: 24,
    height: 24,
    borderWidth: 2,
    borderColor: '#d1d5db',
    borderRadius: 4,
    marginRight: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  checkedBox: {
    backgroundColor: '#667eea',
    borderColor: '#667eea',
  },
  checkmark: {
    color: 'white',
    fontSize: 14,
    fontWeight: '700',
  },
  checkboxText: {
    fontSize: 14,
    color: '#374151',
    flex: 1,
  },
  strategyOptions: {
    marginBottom: 24,
  },
  strategyOption: {
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
  strategyIcon: {
    fontSize: 24,
    marginBottom: 8,
  },
  strategyTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 4,
  },
  strategyDescription: {
    fontSize: 14,
    color: '#6b7280',
  },
  infoBox: {
    backgroundColor: '#e0f2fe',
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'flex-start',
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
  confirmationSummary: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    marginBottom: 24,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  summaryRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#f3f4f6',
  },
  summaryLabel: {
    fontSize: 14,
    color: '#6b7280',
  },
  summaryValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
  },
  finalWarning: {
    backgroundColor: '#f0f9ff',
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  finalWarningText: {
    fontSize: 14,
    color: '#0369a1',
    flex: 1,
    lineHeight: 20,
  },
  footer: {
    flexDirection: 'row',
    paddingHorizontal: 20,
    paddingVertical: 16,
    backgroundColor: 'white',
    borderTopWidth: 1,
    borderTopColor: '#e5e7eb',
    gap: 12,
  },
  button: {
    flex: 1,
    paddingVertical: 14,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  primaryButton: {
    backgroundColor: '#667eea',
  },
  secondaryButton: {
    backgroundColor: '#f3f4f6',
  },
  disabledButton: {
    backgroundColor: '#d1d5db',
  },
  primaryButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: 'white',
  },
  secondaryButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#6b7280',
  },
});

export default CompetitionEntry;