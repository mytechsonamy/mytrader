import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  Switch,
  Animated,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';

const ProfileScreen = ({ navigation }: any) => {
  const { user, logout } = useAuth();
  const { theme, themeMode, colors, setThemeMode, isDark } = useTheme();
  const [notificationsEnabled, setNotificationsEnabled] = React.useState(true);
  
  // Animation for theme transition
  const fadeAnim = React.useRef(new Animated.Value(1)).current;
  
  const handleThemeToggle = (value: boolean) => {
    // Smooth fade animation
    Animated.sequence([
      Animated.timing(fadeAnim, {
        toValue: 0.7,
        duration: 150,
        useNativeDriver: true,
      }),
      Animated.timing(fadeAnim, {
        toValue: 1,
        duration: 150,
        useNativeDriver: true,
      }),
    ]).start();
    
    // Toggle between light and dark (not system)
    setThemeMode(value ? 'dark' : 'light');
  };

  const handleLogout = () => {
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
  };

  const handleEditProfile = () => {
    Alert.alert('Profil Düzenle', 'Bu özellik yakında gelecek!');
  };

  const handleChangePassword = () => {
    Alert.alert('Şifre Değiştir', 'Bu özellik yakında gelecek!');
  };

  const handleSupport = () => {
    Alert.alert('Destek', 'Destek ekibimizle iletişime geçebilirsiniz:\nsupport@mytrader.com');
  };

  if (!user) {
    return (
      <Animated.View style={[styles.container, { backgroundColor: colors.background, opacity: fadeAnim }]}>
        <View style={styles.centerContent}>
          <Text style={[styles.title, { color: colors.text }]}>👤 Profil</Text>
          <Text style={[styles.guestText, { color: colors.textSecondary }]}>Profil bilgilerinizi görmek için giriş yapmanız gerekiyor.</Text>
          <TouchableOpacity
            style={[styles.loginButton, { backgroundColor: colors.primary }]}
            onPress={() => navigation.navigate('AuthStack')}
          >
            <Text style={styles.loginButtonText}>🔑 Giriş Yap</Text>
          </TouchableOpacity>
        </View>
      </Animated.View>
    );
  }

  return (
    <Animated.View style={[styles.container, { backgroundColor: colors.background, opacity: fadeAnim }]}>
      <ScrollView showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={[styles.title, { color: colors.text }]}>👤 Profil</Text>
        </View>

        {/* User Info */}
        <View style={[styles.userInfoCard, { backgroundColor: colors.card }]}>
          <View style={styles.avatarContainer}>
            <View style={[styles.avatar, { backgroundColor: colors.primary }]}>
              <Text style={styles.avatarText}>
                {user.first_name?.[0]?.toUpperCase()}{user.last_name?.[0]?.toUpperCase()}
              </Text>
            </View>
          </View>
          
          <View style={styles.userDetails}>
            <Text style={[styles.userName, { color: colors.text }]}>{user.first_name} {user.last_name}</Text>
            <Text style={[styles.userEmail, { color: colors.textSecondary }]}>{user.email}</Text>
            <Text style={[styles.userPhone, { color: colors.textSecondary }]}>{user.phone}</Text>
          </View>
          
          <TouchableOpacity style={[styles.editButton, { backgroundColor: colors.surface }]} onPress={handleEditProfile}>
            <Text style={[styles.editButtonText, { color: colors.primary }]}>✏️ Düzenle</Text>
          </TouchableOpacity>
        </View>

        {/* Account Settings */}
        <View style={styles.section}>
          <Text style={[styles.sectionTitle, { color: colors.text }]}>⚙️ Hesap Ayarları</Text>
          
          <TouchableOpacity style={[styles.settingItem, { backgroundColor: colors.card }]} onPress={handleChangePassword}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>🔐 Şifre Değiştir</Text>
            <Text style={[styles.settingArrow, { color: colors.textTertiary }]}>›</Text>
          </TouchableOpacity>
          
          <View style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>🔔 Bildirimler</Text>
            <Switch
              value={notificationsEnabled}
              onValueChange={setNotificationsEnabled}
              trackColor={{ false: '#767577', true: colors.primary }}
              thumbColor={notificationsEnabled ? '#fff' : '#f4f3f4'}
            />
          </View>
          
          <View style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>🌙 Karanlık Mod</Text>
            <Switch
              value={isDark}
              onValueChange={handleThemeToggle}
              trackColor={{ false: '#767577', true: colors.primary }}
              thumbColor={isDark ? '#fff' : '#f4f3f4'}
            />
          </View>
        </View>

        {/* App Settings */}
        <View style={styles.section}>
          <Text style={[styles.sectionTitle, { color: colors.text }]}>📱 Uygulama</Text>
          
          <TouchableOpacity style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>📊 Trading Tercihleri</Text>
            <Text style={[styles.settingArrow, { color: colors.textTertiary }]}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>🔒 Güvenlik Ayarları</Text>
            <Text style={[styles.settingArrow, { color: colors.textTertiary }]}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>💾 Veri ve Depolama</Text>
            <Text style={[styles.settingArrow, { color: colors.textTertiary }]}>›</Text>
          </TouchableOpacity>
        </View>

        {/* Support */}
        <View style={styles.section}>
          <Text style={[styles.sectionTitle, { color: colors.text }]}>🆘 Destek</Text>
          
          <TouchableOpacity style={[styles.settingItem, { backgroundColor: colors.card }]} onPress={handleSupport}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>📞 İletişim</Text>
            <Text style={[styles.settingArrow, { color: colors.textTertiary }]}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>❓ Sık Sorulan Sorular</Text>
            <Text style={[styles.settingArrow, { color: colors.textTertiary }]}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>📋 Kullanım Koşulları</Text>
            <Text style={[styles.settingArrow, { color: colors.textTertiary }]}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>🔒 Gizlilik Politikası</Text>
            <Text style={[styles.settingArrow, { color: colors.textTertiary }]}>›</Text>
          </TouchableOpacity>
        </View>

        {/* App Info */}
        <View style={styles.section}>
          <Text style={[styles.sectionTitle, { color: colors.text }]}>ℹ️ Uygulama Bilgisi</Text>
          
          <View style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>📱 Versiyon</Text>
            <Text style={[styles.settingValue, { color: colors.textSecondary }]}>1.0.0</Text>
          </View>
          
          <View style={[styles.settingItem, { backgroundColor: colors.card }]}>
            <Text style={[styles.settingLabel, { color: colors.text }]}>🔧 Build</Text>
            <Text style={[styles.settingValue, { color: colors.textSecondary }]}>Week 3</Text>
          </View>
        </View>

        {/* Logout */}
        <View style={styles.logoutContainer}>
          <TouchableOpacity style={[styles.logoutButton, { backgroundColor: colors.error }]} onPress={handleLogout}>
            <Text style={styles.logoutButtonText}>🚪 Çıkış Yap</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
    </Animated.View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
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
  },
  guestText: {
    fontSize: 16,
    textAlign: 'center',
    marginBottom: 20,
  },
  loginButton: {
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
    marginBottom: 5,
  },
  userEmail: {
    fontSize: 14,
    marginBottom: 2,
  },
  userPhone: {
    fontSize: 14,
  },
  editButton: {
    paddingHorizontal: 20,
    paddingVertical: 8,
    borderRadius: 20,
    alignSelf: 'center',
  },
  editButtonText: {
    fontSize: 14,
    fontWeight: '600',
  },
  section: {
    marginHorizontal: 20,
    marginBottom: 20,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 10,
  },
  settingItem: {
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
  settingLabel: {
    fontSize: 16,
  },
  settingValue: {
    fontSize: 14,
  },
  settingArrow: {
    fontSize: 18,
  },
  logoutContainer: {
    margin: 20,
    marginTop: 30,
  },
  logoutButton: {
    paddingVertical: 15,
    borderRadius: 25,
    alignItems: 'center',
  },
  logoutButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default ProfileScreen;