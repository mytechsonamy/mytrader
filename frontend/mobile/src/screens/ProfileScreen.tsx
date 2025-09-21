import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  Switch,
} from 'react-native';
import { useAuth } from '../context/AuthContext';

const ProfileScreen = ({ navigation }: any) => {
  const { user, logout } = useAuth();
  const [notificationsEnabled, setNotificationsEnabled] = React.useState(true);
  const [darkMode, setDarkMode] = React.useState(false);

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
            <Text style={styles.userPhone}>{user.phone}</Text>
          </View>
          
          <TouchableOpacity style={styles.editButton} onPress={handleEditProfile}>
            <Text style={styles.editButtonText}>✏️ Düzenle</Text>
          </TouchableOpacity>
        </View>

        {/* Account Settings */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>⚙️ Hesap Ayarları</Text>
          
          <TouchableOpacity style={styles.settingItem} onPress={handleChangePassword}>
            <Text style={styles.settingLabel}>🔐 Şifre Değiştir</Text>
            <Text style={styles.settingArrow}>›</Text>
          </TouchableOpacity>
          
          <View style={styles.settingItem}>
            <Text style={styles.settingLabel}>🔔 Bildirimler</Text>
            <Switch
              value={notificationsEnabled}
              onValueChange={setNotificationsEnabled}
              trackColor={{ false: '#767577', true: '#667eea' }}
              thumbColor={notificationsEnabled ? '#fff' : '#f4f3f4'}
            />
          </View>
          
          <View style={styles.settingItem}>
            <Text style={styles.settingLabel}>🌙 Karanlık Mod</Text>
            <Switch
              value={darkMode}
              onValueChange={setDarkMode}
              trackColor={{ false: '#767577', true: '#667eea' }}
              thumbColor={darkMode ? '#fff' : '#f4f3f4'}
            />
          </View>
        </View>

        {/* App Settings */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>📱 Uygulama</Text>
          
          <TouchableOpacity style={styles.settingItem}>
            <Text style={styles.settingLabel}>📊 Trading Tercihleri</Text>
            <Text style={styles.settingArrow}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={styles.settingItem}>
            <Text style={styles.settingLabel}>🔒 Güvenlik Ayarları</Text>
            <Text style={styles.settingArrow}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={styles.settingItem}>
            <Text style={styles.settingLabel}>💾 Veri ve Depolama</Text>
            <Text style={styles.settingArrow}>›</Text>
          </TouchableOpacity>
        </View>

        {/* Support */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>🆘 Destek</Text>
          
          <TouchableOpacity style={styles.settingItem} onPress={handleSupport}>
            <Text style={styles.settingLabel}>📞 İletişim</Text>
            <Text style={styles.settingArrow}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={styles.settingItem}>
            <Text style={styles.settingLabel}>❓ Sık Sorulan Sorular</Text>
            <Text style={styles.settingArrow}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={styles.settingItem}>
            <Text style={styles.settingLabel}>📋 Kullanım Koşulları</Text>
            <Text style={styles.settingArrow}>›</Text>
          </TouchableOpacity>
          
          <TouchableOpacity style={styles.settingItem}>
            <Text style={styles.settingLabel}>🔒 Gizlilik Politikası</Text>
            <Text style={styles.settingArrow}>›</Text>
          </TouchableOpacity>
        </View>

        {/* App Info */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>ℹ️ Uygulama Bilgisi</Text>
          
          <View style={styles.settingItem}>
            <Text style={styles.settingLabel}>📱 Versiyon</Text>
            <Text style={styles.settingValue}>1.0.0</Text>
          </View>
          
          <View style={styles.settingItem}>
            <Text style={styles.settingLabel}>🔧 Build</Text>
            <Text style={styles.settingValue}>Week 3</Text>
          </View>
        </View>

        {/* Logout */}
        <View style={styles.logoutContainer}>
          <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
            <Text style={styles.logoutButtonText}>🚪 Çıkış Yap</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
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
  settingItem: {
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
  settingLabel: {
    fontSize: 16,
    color: '#333',
  },
  settingValue: {
    fontSize: 14,
    color: '#666',
  },
  settingArrow: {
    fontSize: 18,
    color: '#ccc',
  },
  logoutContainer: {
    margin: 20,
    marginTop: 30,
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
});

export default ProfileScreen;