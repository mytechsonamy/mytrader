import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Modal,
  ScrollView,
  Animated,
} from 'react-native';
import { CompetitionStats } from '../../types';

interface RulesModalProps {
  visible: boolean;
  onClose: () => void;
  stats?: CompetitionStats | null;
}

interface RuleSection {
  id: string;
  title: string;
  icon: string;
  content: string[];
  expanded?: boolean;
}

const RulesModal: React.FC<RulesModalProps> = ({
  visible,
  onClose,
  stats,
}) => {
  const [fadeAnim] = useState(new Animated.Value(0));
  const [expandedSections, setExpandedSections] = useState<Record<string, boolean>>({
    general: true,
  });

  React.useEffect(() => {
    if (visible) {
      Animated.timing(fadeAnim, {
        toValue: 1,
        duration: 300,
        useNativeDriver: true,
      }).start();
    }
  }, [visible, fadeAnim]);

  const ruleSections: RuleSection[] = [
    {
      id: 'general',
      title: 'Genel Kurallar',
      icon: '📋',
      content: [
        'Yarışma her hafta Pazartesi 00:00\'da başlar ve Pazar 23:59\'da sona erer.',
        'Tüm işlemler gerçek zamanlı olarak değerlendirilir ve anlık puanlama yapılır.',
        'Sadece MyTrader platformu üzerinden yapılan işlemler yarışmada geçerlidir.',
        'Katılımcılar yarışma süresince platform kurallarına uymakla yükümlüdür.',
        'Birden fazla hesap kullanımı kesinlikle yasaktır ve tespit edildiğinde diskalifiye edilir.',
        'Yarışma Türkiye saati (UTC+3) referans alınarak değerlendirilir.',
      ],
    },
    {
      id: 'scoring',
      title: 'Puanlama Sistemi',
      icon: '📊',
      content: [
        'Getiri Oranı (%50): Portföyünüzün haftalık performans yüzdesi ana faktördür.',
        'Kazanç Oranı (%25): Başarılı işlemlerinizin toplam işlemlerinize oranı.',
        'İşlem Sayısı (%15): Minimum şartları aşan aktif trading aktivitesi.',
        'Risk Yönetimi (%10): Maksimum çekilme oranı ve volatilite kontrolü.',
        'Bonus Puanlar: Sürekli katılım, başarı rozetleri ve özel etkinlikler.',
        'Minimum 5 işlem yapılması zorunludur, aksi takdirde hafta geçersiz sayılır.',
      ],
    },
    {
      id: 'eligibility',
      title: 'Katılım Şartları',
      icon: '✅',
      content: [
        `Minimum ${stats?.minimumPortfolioValue?.toLocaleString('tr-TR') || '10.000'} ₺ portföy değeri gereklidir.`,
        `Hafta boyunca en az ${stats?.minimumTrades || 5} işlem yapılmalıdır.`,
        'Kimlik doğrulaması tamamlanmış, aktif hesap sahibi olunmalıdır.',
        'Platform kullanım şartlarını kabul etmiş ve ihlal geçmişi bulunmamalıdır.',
        '18 yaşından büyük, Türkiye Cumhuriyeti vatandaşı veya yerleşik olunmalıdır.',
        'Vergi numarası ve gerekli yasal belgelerin eksiksiz olması zorunludur.',
      ],
    },
    {
      id: 'prizes',
      title: 'Ödüller ve Dağıtım',
      icon: '🏆',
      content: [
        `Toplam haftalık ödül havuzu: ${stats?.totalPrizePool?.toLocaleString('tr-TR') || '50.000'} ₺`,
        '1. sıra: Haftalık ödül havuzunun %40\'ı (20.000 ₺)',
        '2. sıra: Haftalık ödül havuzunun %25\'i (12.500 ₺)',
        '3. sıra: Haftalık ödül havuzunun %15\'i (7.500 ₺)',
        '4-10. sıralar: Kalan ödül havuzunun eşit dağıtımı (yaklaşık 1.430 ₺)',
        'Ödüller yarışma bitiminden en geç 48 saat içinde hesaplara yatırılır.',
        'Vergi kesintileri mevcut yasal düzenlemelere göre yapılır (%20 stopaj).',
        'Ödül almak için minimum 3 ay aktif kullanıcı olunması gerekir.',
      ],
    },
    {
      id: 'trading',
      title: 'Trading Kuralları',
      icon: '💼',
      content: [
        'Tüm varlık sınıfları (kripto, hisse, forex, emtia) yarışmada geçerlidir.',
        'Kaldıraçlı işlemler maksimum 1:10 oranında kullanılabilir.',
        'Minimum işlem tutarı 100 ₺, maksimum işlem tutarı portföy değerinin %50\'sidir.',
        'Gün içi trading, swing trading ve uzun vadeli yatırım tüm stratejiler geçerlidir.',
        'Stop-loss ve take-profit emirleri kullanımı önerilir ve puanlama avantajı sağlar.',
        'Market emirleri, limit emirleri ve koşullu emirler tüm yarışmada kullanılabilir.',
      ],
    },
    {
      id: 'prohibited',
      title: 'Yasaklanan Davranışlar',
      icon: '❌',
      content: [
        'Otomatik trading botları ve algoritmik sistemler kullanımı yasaktır.',
        'Piyasa manipülasyonu, fiyat manipülasyonu ve koordineli işlemler yasaktır.',
        'Diğer yarışmacılarla işbirliği yaparak avantaj sağlamaya çalışmak yasaktır.',
        'Sahte hesap, kimlik hırsızlığı veya başkası adına işlem yapılması yasaktır.',
        'Platform güvenlik açıklarını kullanma girişimleri yasaktır.',
        'Spam, reklam veya uygunsuz içerik paylaşımı yasaktır.',
        'Hakaret, tehdit veya diğer katılımcıları rahatsız edici davranışlar yasaktır.',
      ],
    },
    {
      id: 'technical',
      title: 'Teknik Kurallar',
      icon: '⚙️',
      content: [
        'Platform bakım saatleri (03:00-04:00 arası) yarışma süresinden düşülür.',
        'Teknik arızalar durumunda yarışma askıya alınabilir ve telafi edilir.',
        'Fiyat verisi sağlayıcıları: Binance, BIST, NASDAQ, Reuters.',
        'Spread ve komisyonlar yarışma puanlamasına dahil edilir.',
        'Yarışma verileri 15 dakika aralıklarla yedeklenir ve kaydedilir.',
        'Hesap güvenliği katılımcıların sorumluluğundadır (2FA zorunlu).',
      ],
    },
    {
      id: 'disputes',
      title: 'Uyuşmazlık ve İtirazlar',
      icon: '⚖️',
      content: [
        'Yarışma sonuçlarına itirazlar 24 saat içinde yapılmalıdır.',
        'İtirazlar destek@mytrader.com adresine detaylı açıklama ile gönderilmelidir.',
        'İnceleme süreci maksimum 5 iş günüdür ve kararlar kesindir.',
        'Hileli davranış şüphesi durumunda hesap geçici olarak askıya alınabilir.',
        'Diskalifiye edilen katılımcılar 30 gün boyunca yeni yarışmalara katılamazlar.',
        'Platform yönetimi gerektiğinde kural değişikliği yapabilir (7 gün önceden duyuru).',
      ],
    },
  ];

  const toggleSection = (sectionId: string) => {
    setExpandedSections(prev => ({
      ...prev,
      [sectionId]: !prev[sectionId],
    }));
  };

  const renderSection = (section: RuleSection) => {
    const isExpanded = expandedSections[section.id];

    return (
      <View key={section.id} style={styles.sectionContainer}>
        <TouchableOpacity
          style={styles.sectionHeader}
          onPress={() => toggleSection(section.id)}
          activeOpacity={0.7}
        >
          <View style={styles.sectionTitleContainer}>
            <Text style={styles.sectionIcon}>{section.icon}</Text>
            <Text style={styles.sectionTitle}>{section.title}</Text>
          </View>
          <Text style={[
            styles.expandIcon,
            { transform: [{ rotate: isExpanded ? '180deg' : '0deg' }] }
          ]}>
            ▼
          </Text>
        </TouchableOpacity>

        {isExpanded && (
          <View style={styles.sectionContent}>
            {section.content.map((rule, index) => (
              <View key={index} style={styles.ruleItem}>
                <Text style={styles.ruleNumber}>{index + 1}.</Text>
                <Text style={styles.ruleText}>{rule}</Text>
              </View>
            ))}
          </View>
        )}
      </View>
    );
  };

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
          <Text style={styles.headerTitle}>Yarışma Kuralları</Text>
          <View style={styles.headerPlaceholder} />
        </View>

        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          <View style={styles.introSection}>
            <Text style={styles.introIcon}>📜</Text>
            <Text style={styles.introTitle}>Strategist Yarışması Kuralları</Text>
            <Text style={styles.introText}>
              Adil, güvenli ve eğlenceli bir yarışma deneyimi için tüm kuralları dikkatle okuyun.
              Bu kurallar tüm katılımcılar için bağlayıcıdır.
            </Text>
          </View>

          {stats && (
            <View style={styles.quickStatsSection}>
              <Text style={styles.quickStatsTitle}>⚡ Hızlı Bilgiler</Text>
              <View style={styles.quickStatsGrid}>
                <View style={styles.quickStatItem}>
                  <Text style={styles.quickStatValue}>{stats.totalParticipants}</Text>
                  <Text style={styles.quickStatLabel}>Katılımcı</Text>
                </View>
                <View style={styles.quickStatItem}>
                  <Text style={styles.quickStatValue}>{stats.minimumTrades}</Text>
                  <Text style={styles.quickStatLabel}>Min. İşlem</Text>
                </View>
                <View style={styles.quickStatItem}>
                  <Text style={styles.quickStatValue}>{Math.round(stats.totalPrizePool / 1000)}K ₺</Text>
                  <Text style={styles.quickStatLabel}>Ödül Havuzu</Text>
                </View>
              </View>
            </View>
          )}

          <View style={styles.rulesSection}>
            {ruleSections.map(renderSection)}
          </View>

          <View style={styles.contactSection}>
            <Text style={styles.contactIcon}>📞</Text>
            <Text style={styles.contactTitle}>İletişim ve Destek</Text>
            <Text style={styles.contactText}>
              Sorularınız için: destek@mytrader.com{'\n'}
              Teknik destek: +90 212 XXX XX XX{'\n'}
              Çalışma saatleri: Hafta içi 09:00-18:00
            </Text>
          </View>

          <View style={styles.lastUpdateSection}>
            <Text style={styles.lastUpdateText}>
              Son güncellenme: {new Date().toLocaleDateString('tr-TR')}{'\n'}
              Versiyon: 2.1
            </Text>
          </View>
        </ScrollView>

        <View style={styles.footer}>
          <TouchableOpacity style={styles.closeFooterButton} onPress={onClose}>
            <Text style={styles.closeFooterButtonText}>Anladım</Text>
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
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
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
  headerPlaceholder: {
    width: 32,
  },
  content: {
    flex: 1,
  },
  introSection: {
    backgroundColor: 'white',
    margin: 20,
    borderRadius: 16,
    padding: 24,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  introIcon: {
    fontSize: 48,
    marginBottom: 16,
  },
  introTitle: {
    fontSize: 22,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 12,
    textAlign: 'center',
  },
  introText: {
    fontSize: 16,
    color: '#6b7280',
    textAlign: 'center',
    lineHeight: 24,
  },
  quickStatsSection: {
    backgroundColor: 'white',
    marginHorizontal: 20,
    marginBottom: 20,
    borderRadius: 16,
    padding: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  quickStatsTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 16,
    textAlign: 'center',
  },
  quickStatsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  quickStatItem: {
    alignItems: 'center',
    flex: 1,
  },
  quickStatValue: {
    fontSize: 20,
    fontWeight: '700',
    color: '#667eea',
    marginBottom: 4,
  },
  quickStatLabel: {
    fontSize: 12,
    color: '#6b7280',
    textAlign: 'center',
  },
  rulesSection: {
    paddingHorizontal: 20,
  },
  sectionContainer: {
    backgroundColor: 'white',
    borderRadius: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: 20,
  },
  sectionTitleContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  sectionIcon: {
    fontSize: 20,
    marginRight: 12,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1f2937',
  },
  expandIcon: {
    fontSize: 12,
    color: '#6b7280',
    fontWeight: '600',
  },
  sectionContent: {
    paddingHorizontal: 20,
    paddingBottom: 20,
  },
  ruleItem: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: 12,
  },
  ruleNumber: {
    fontSize: 14,
    fontWeight: '600',
    color: '#667eea',
    marginRight: 8,
    minWidth: 20,
  },
  ruleText: {
    fontSize: 14,
    color: '#374151',
    lineHeight: 20,
    flex: 1,
  },
  contactSection: {
    backgroundColor: '#f0f9ff',
    margin: 20,
    borderRadius: 16,
    padding: 20,
    alignItems: 'center',
  },
  contactIcon: {
    fontSize: 32,
    marginBottom: 12,
  },
  contactTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#0369a1',
    marginBottom: 12,
  },
  contactText: {
    fontSize: 14,
    color: '#0891b2',
    textAlign: 'center',
    lineHeight: 20,
  },
  lastUpdateSection: {
    alignItems: 'center',
    paddingHorizontal: 20,
    paddingBottom: 20,
  },
  lastUpdateText: {
    fontSize: 12,
    color: '#9ca3af',
    textAlign: 'center',
    lineHeight: 16,
  },
  footer: {
    backgroundColor: 'white',
    paddingHorizontal: 20,
    paddingVertical: 16,
    borderTopWidth: 1,
    borderTopColor: '#e5e7eb',
  },
  closeFooterButton: {
    backgroundColor: '#667eea',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
  },
  closeFooterButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: 'white',
  },
});

export default RulesModal;