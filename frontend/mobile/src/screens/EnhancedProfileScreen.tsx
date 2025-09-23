import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  Switch,
  Modal,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { useAuth } from '../context/AuthContext';
import { RootStackParamList } from '../types';
import AlarmsScreen from './AlarmsScreen';
import EducationSection from '../components/profile/EducationSection';

type ProfileNavigationProp = StackNavigationProp<RootStackParamList>;

interface MenuItem {
  id: string;
  title: string;
  icon: string;
  description?: string;
  type: 'navigation' | 'toggle' | 'action' | 'modal';
  route?: string;
  action?: () => void;
  value?: boolean;
  onToggle?: (value: boolean) => void;
  badge?: string;
  disabled?: boolean;
}

const EnhancedProfileScreen = () => {
  const navigation = useNavigation<ProfileNavigationProp>();
  const { user, logout } = useAuth();

  // Settings state
  const [notificationsEnabled, setNotificationsEnabled] = useState(true);
  const [priceAlertsEnabled, setPriceAlertsEnabled] = useState(true);
  const [newsNotificationsEnabled, setNewsNotificationsEnabled] = useState(true);
  const [darkMode, setDarkMode] = useState(false);
  const [biometricEnabled, setBiometricEnabled] = useState(false);

  // Modal states
  const [showAlarmsModal, setShowAlarmsModal] = useState(false);
  const [showEducationModal, setShowEducationModal] = useState(false);
  const [showAnalyticsModal, setShowAnalyticsModal] = useState(false);

  const handleLogout = useCallback(() => {
    Alert.alert(
      'Çıkış Yap',
      'Hesabınızdan çıkış yapmak istediğinizden emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Çıkış Yap',
          style: 'destructive',
          onPress: () => logout(),
        },
      ]
    );
  }, [logout]);

  const handleEditProfile = useCallback(() => {
    Alert.alert('Profil Düzenle', 'Bu özellik yakında gelecek!');
  }, []);

  const handleChangePassword = useCallback(() => {
    Alert.alert('Şifre Değiştir', 'Bu özellik yakında gelecek!');
  }, []);

  const handleSupport = useCallback(() => {
    Alert.alert('Destek', 'Destek ekibimizle iletişime geçebilirsiniz:\nsupport@mytrader.com');
  }, []);

  const handleOpenNews = useCallback(() => {
    // Navigate to dedicated news screen or show news section
    Alert.alert('Haberler', 'Haberler artık Ana Sayfa\'da entegre edilmiştir!');
  }, []);

  const handleEducation = useCallback(() => {
    setShowEducationModal(true);
  }, []);

  const handleAnalytics = useCallback(() => {
    setShowAnalyticsModal(true);
  }, []);

  // Menu sections configuration
  const accountMenuItems: MenuItem[] = [
    {
      id: 'edit-profile',
      title: 'Profil Düzenle',
      icon: '✏️',
      description: 'Kişisel bilgilerinizi güncelleyin',
      type: 'action',
      action: handleEditProfile,
    },
    {
      id: 'change-password',
      title: 'Şifre Değiştir',
      icon: '🔐',
      description: 'Hesap güvenliğinizi artırın',
      type: 'action',
      action: handleChangePassword,
    },
    {
      id: 'biometric',
      title: 'Biyometrik Giriş',
      icon: '👆',
      description: 'Parmak izi/yüz tanıma ile giriş',
      type: 'toggle',
      value: biometricEnabled,
      onToggle: setBiometricEnabled,
    },
  ];

  const alertsMenuItems: MenuItem[] = [
    {
      id: 'alarms',
      title: 'Fiyat Alarmları',
      icon: '🔔',
      description: 'Fiyat alarmlarınızı yönetin',
      type: 'modal',
      action: () => setShowAlarmsModal(true),
      badge: '3', // This would come from actual alarm count
    },
    {
      id: 'price-alerts',
      title: 'Fiyat Bildirimleri',
      icon: '📈',
      description: 'Fiyat değişimi bildirimleri',
      type: 'toggle',
      value: priceAlertsEnabled,
      onToggle: setPriceAlertsEnabled,
    },
    {
      id: 'news-notifications',
      title: 'Haber Bildirimleri',
      icon: '📰',
      description: 'Önemli piyasa haberleri',
      type: 'toggle',
      value: newsNotificationsEnabled,
      onToggle: setNewsNotificationsEnabled,
    },
    {
      id: 'general-notifications',
      title: 'Genel Bildirimler',
      icon: '🔔',
      description: 'Uygulama bildirimleri',
      type: 'toggle',
      value: notificationsEnabled,
      onToggle: setNotificationsEnabled,
    },
  ];

  const featuresMenuItems: MenuItem[] = [
    {
      id: 'news',
      title: 'Piyasa Haberleri',
      icon: '📰',
      description: 'Ana sayfada entegre edildi',
      type: 'action',
      action: handleOpenNews,
    },
    {
      id: 'education',
      title: 'Eğitim Merkezi',
      icon: '📚',
      description: 'Trading eğitimleri ve kaynaklar',
      type: 'modal',
      action: handleEducation,
    },
    {
      id: 'analytics',
      title: 'Portföy Analitiği',
      icon: '📊',
      description: 'Detaylı performans analizleri',
      type: 'modal',
      action: handleAnalytics,
    },
  ];

  const appMenuItems: MenuItem[] = [
    {
      id: 'dark-mode',
      title: 'Karanlık Mod',
      icon: '🌙',
      description: 'Gece modunu etkinleştirin',
      type: 'toggle',
      value: darkMode,
      onToggle: setDarkMode,
    },
    {
      id: 'trading-preferences',
      title: 'Trading Tercihleri',
      icon: '📊',
      description: 'İşlem ayarlarınızı özelleştirin',
      type: 'action',
      action: () => Alert.alert('Trading Tercihleri', 'Bu özellik yakında gelecek!'),
    },
    {
      id: 'security',
      title: 'Güvenlik Ayarları',
      icon: '🔒',
      description: 'Hesap güvenlik ayarları',
      type: 'action',
      action: () => Alert.alert('Güvenlik', 'Bu özellik yakında gelecek!'),
    },
    {
      id: 'data-storage',
      title: 'Veri ve Depolama',
      icon: '💾',
      description: 'Uygulama veri ayarları',
      type: 'action',
      action: () => Alert.alert('Veri ve Depolama', 'Bu özellik yakında gelecek!'),
    },
  ];

  const supportMenuItems: MenuItem[] = [
    {
      id: 'contact',
      title: 'İletişim',
      icon: '📞',
      description: 'Destek ekibiyle iletişime geçin',
      type: 'action',
      action: handleSupport,
    },
    {
      id: 'faq',
      title: 'Sık Sorulan Sorular',
      icon: '❓',
      description: 'Yaygın sorular ve cevaplar',
      type: 'action',
      action: () => Alert.alert('SSS', 'Bu özellik yakında gelecek!'),
    },
    {
      id: 'terms',
      title: 'Kullanım Koşulları',
      icon: '📋',
      description: 'Hizmet şartları ve koşullar',
      type: 'action',
      action: () => Alert.alert('Kullanım Koşulları', 'Bu özellik yakında gelecek!'),
    },
    {
      id: 'privacy',
      title: 'Gizlilik Politikası',
      icon: '🔒',
      description: 'Veri koruma ve gizlilik',
      type: 'action',
      action: () => Alert.alert('Gizlilik Politikası', 'Bu özellik yakında gelecek!'),
    },
  ];

  const renderMenuItem = (item: MenuItem) => {
    const handlePress = () => {
      if (item.type === 'action' || item.type === 'modal') {
        item.action?.();
      } else if (item.type === 'navigation' && item.route) {
        // Handle navigation if needed
      }
    };

    return (
      <TouchableOpacity
        key={item.id}
        style={[styles.menuItem, item.disabled && styles.disabledMenuItem]}
        onPress={handlePress}
        disabled={item.disabled || item.type === 'toggle'}
      >
        <View style={styles.menuItemLeft}>
          <Text style={styles.menuItemIcon}>{item.icon}</Text>
          <View style={styles.menuItemContent}>
            <View style={styles.menuItemTitleRow}>
              <Text style={[styles.menuItemTitle, item.disabled && styles.disabledText]}>
                {item.title}
              </Text>
              {item.badge && (
                <View style={styles.badge}>
                  <Text style={styles.badgeText}>{item.badge}</Text>
                </View>
              )}
            </View>
            {item.description && (
              <Text style={[styles.menuItemDescription, item.disabled && styles.disabledText]}>
                {item.description}
              </Text>
            )}
          </View>
        </View>

        <View style={styles.menuItemRight}>
          {item.type === 'toggle' ? (
            <Switch
              value={item.value}
              onValueChange={item.onToggle}
              trackColor={{ false: '#ccc', true: '#667eea' }}
              thumbColor={item.value ? 'white' : '#f4f3f4'}
              disabled={item.disabled}
            />
          ) : (
            <Text style={[styles.menuItemArrow, item.disabled && styles.disabledText]}>›</Text>
          )}
        </View>
      </TouchableOpacity>
    );
  };

  const renderMenuSection = (title: string, items: MenuItem[]) => (
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>{title}</Text>
      {items.map(renderMenuItem)}
    </View>
  );

  if (!user) {
    return (
      <View style={styles.container}>
        <View style={styles.centerContent}>
          <Text style={styles.title}>👤 Profil</Text>
          <Text style={styles.guestText}>Profil bilgilerinizi görmek için giriş yapmanız gerekiyor.</Text>
          <TouchableOpacity
            style={styles.loginButton}
            onPress={() => navigation.navigate('AuthStack')}
          >
            <Text style={styles.loginButtonText}>🔑 Giriş Yap</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <ScrollView showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.title}>👤 Profil</Text>
        </View>

        {/* User Info */}
        <View style={styles.userInfoCard}>
          <View style={styles.avatarContainer}>
            <View style={styles.avatar}>
              <Text style={styles.avatarText}>
                {user.first_name?.[0]?.toUpperCase()}{user.last_name?.[0]?.toUpperCase()}
              </Text>
            </View>
          </View>

          <View style={styles.userDetails}>
            <Text style={styles.userName}>{user.first_name} {user.last_name}</Text>
            <Text style={styles.userEmail}>{user.email}</Text>
            {user.phone && <Text style={styles.userPhone}>{user.phone}</Text>}
          </View>

          <TouchableOpacity style={styles.editButton} onPress={handleEditProfile}>
            <Text style={styles.editButtonText}>✏️ Düzenle</Text>
          </TouchableOpacity>
        </View>

        {/* Menu Sections */}
        {renderMenuSection('⚙️ Hesap Ayarları', accountMenuItems)}
        {renderMenuSection('🔔 Bildirimler & Alarmlar', alertsMenuItems)}
        {renderMenuSection('🚀 Özellikler', featuresMenuItems)}
        {renderMenuSection('📱 Uygulama Ayarları', appMenuItems)}
        {renderMenuSection('🆘 Destek', supportMenuItems)}

        {/* App Info */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>ℹ️ Uygulama Bilgisi</Text>

          <View style={styles.menuItem}>
            <View style={styles.menuItemLeft}>
              <Text style={styles.menuItemIcon}>📱</Text>
              <Text style={styles.menuItemTitle}>Versiyon</Text>
            </View>
            <Text style={styles.menuItemValue}>1.0.0</Text>
          </View>

          <View style={styles.menuItem}>
            <View style={styles.menuItemLeft}>
              <Text style={styles.menuItemIcon}>🔧</Text>
              <Text style={styles.menuItemTitle}>Build</Text>
            </View>
            <Text style={styles.menuItemValue}>Week 3 - Enhanced</Text>
          </View>
        </View>

        {/* Logout */}
        <View style={styles.logoutContainer}>
          <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
            <Text style={styles.logoutButtonText}>🚪 Çıkış Yap</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>

      {/* Alarms Modal */}
      <Modal
        visible={showAlarmsModal}
        animationType="slide"
        presentationStyle="pageSheet"
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>🔔 Fiyat Alarmları</Text>
            <TouchableOpacity
              onPress={() => setShowAlarmsModal(false)}
              style={styles.modalCloseButton}
            >
              <Text style={styles.modalCloseText}>✖️</Text>
            </TouchableOpacity>
          </View>
          <AlarmsScreen />
        </View>
      </Modal>

      {/* Education Modal */}
      <Modal
        visible={showEducationModal}
        animationType="slide"
        presentationStyle="pageSheet"
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>📚 Eğitim Merkezi</Text>
            <TouchableOpacity
              onPress={() => setShowEducationModal(false)}
              style={styles.modalCloseButton}
            >
              <Text style={styles.modalCloseText}>✖️</Text>
            </TouchableOpacity>
          </View>
          <EducationSection
            onItemPress={(item) => {
              if (item.isAvailable) {
                Alert.alert(
                  item.title,
                  `${item.description}\n\nSüre: ${item.duration}\nSeviye: ${item.difficulty}\n\nBu eğitim içeriği yakında aktif olacak!`
                );
              }
            }}
          />
        </View>
      </Modal>

      {/* Analytics Modal */}
      <Modal
        visible={showAnalyticsModal}
        animationType="slide"
        presentationStyle="pageSheet"
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>📊 Portföy Analitiği</Text>
            <TouchableOpacity
              onPress={() => setShowAnalyticsModal(false)}
              style={styles.modalCloseButton}
            >
              <Text style={styles.modalCloseText}>✖️</Text>
            </TouchableOpacity>
          </View>
          <View style={styles.comingSoonContainer}>
            <Text style={styles.comingSoonIcon}>📈</Text>
            <Text style={styles.comingSoonTitle}>Gelişmiş Analitik</Text>
            <Text style={styles.comingSoonText}>
              Portföy performans analitiği, risk metrikleri, benchmark
              karşılaştırmaları ve detaylı raporlama özellikleri
              yakında kullanıma sunulacak.
            </Text>
          </View>
        </View>
      </Modal>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  centerContent: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  header: {
    padding: 20,
    paddingTop: 60,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#333',
  },
  guestText: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
    marginBottom: 20,
  },
  loginButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 30,
    paddingVertical: 12,
    borderRadius: 25,
  },
  loginButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  userInfoCard: {
    backgroundColor: 'white',
    marginHorizontal: 20,
    marginBottom: 20,
    borderRadius: 15,
    padding: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  avatarContainer: {
    alignItems: 'center',
    marginBottom: 15,
  },
  avatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: '#667eea',
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarText: {
    fontSize: 24,
    fontWeight: 'bold',
    color: 'white',
  },
  userDetails: {
    alignItems: 'center',
    marginBottom: 15,
  },
  userName: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 5,
  },
  userEmail: {
    fontSize: 14,
    color: '#666',
    marginBottom: 2,
  },
  userPhone: {
    fontSize: 14,
    color: '#666',
  },
  editButton: {
    backgroundColor: '#f3f4f6',
    paddingHorizontal: 20,
    paddingVertical: 8,
    borderRadius: 20,
    alignSelf: 'center',
  },
  editButtonText: {
    fontSize: 14,
    color: '#667eea',
    fontWeight: '600',
  },
  section: {
    marginHorizontal: 20,
    marginBottom: 20,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 10,
  },
  menuItem: {
    backgroundColor: 'white',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 15,
    borderRadius: 10,
    marginBottom: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 1,
  },
  disabledMenuItem: {
    opacity: 0.6,
  },
  menuItemLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  menuItemIcon: {
    fontSize: 18,
    marginRight: 12,
    width: 24,
    textAlign: 'center',
  },
  menuItemContent: {
    flex: 1,
  },
  menuItemTitleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  menuItemTitle: {
    fontSize: 16,
    color: '#333',
    fontWeight: '600',
    flex: 1,
  },
  menuItemDescription: {
    fontSize: 12,
    color: '#666',
    marginTop: 2,
  },
  disabledText: {
    color: '#999',
  },
  badge: {
    backgroundColor: '#ef4444',
    borderRadius: 10,
    minWidth: 20,
    height: 20,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 6,
    marginLeft: 8,
  },
  badgeText: {
    color: 'white',
    fontSize: 11,
    fontWeight: 'bold',
  },
  menuItemRight: {
    marginLeft: 12,
  },
  menuItemValue: {
    fontSize: 14,
    color: '#666',
  },
  menuItemArrow: {
    fontSize: 18,
    color: '#ccc',
  },
  logoutContainer: {
    margin: 20,
    marginTop: 30,
    marginBottom: 40,
  },
  logoutButton: {
    backgroundColor: '#ef4444',
    paddingVertical: 15,
    borderRadius: 25,
    alignItems: 'center',
  },
  logoutButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  modalContainer: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    paddingTop: 60,
    backgroundColor: 'white',
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
  },
  modalCloseButton: {
    padding: 5,
  },
  modalCloseText: {
    fontSize: 18,
    color: '#666',
  },
  comingSoonContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 40,
  },
  comingSoonIcon: {
    fontSize: 64,
    marginBottom: 20,
  },
  comingSoonTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 16,
    textAlign: 'center',
  },
  comingSoonText: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
    lineHeight: 24,
  },
});

export default EnhancedProfileScreen;