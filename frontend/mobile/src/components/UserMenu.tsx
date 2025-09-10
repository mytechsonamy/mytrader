import React, { useState, useEffect } from 'react';
import { View, Text, Button, StyleSheet, Alert } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { API_BASE_URL } from '../config';

interface UserProfile {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  preferences?: {
    baseCurrency?: string;
    theme?: string;
  };
}

export default function UserMenu() {
  const [userProfile, setUserProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchUserProfile();
  }, []);

  const fetchUserProfile = async () => {
    try {
      const token = await AsyncStorage.getItem('session_token');
      if (!token) {
        setLoading(false);
        return;
      }

      const response = await fetch(`${API_BASE_URL}/users/me`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const profile = await response.json();
        setUserProfile(profile);
      } else {
        console.warn('Failed to fetch user profile');
      }
    } catch (error) {
      console.error('Error fetching user profile:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = async () => {
    Alert.alert(
      'Çıkış Yap',
      'Hesabınızdan çıkmak istediğinizden emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Çıkış Yap',
          style: 'destructive',
          onPress: async () => {
            await AsyncStorage.removeItem('session_token');
            // Navigate to login screen - this should be handled by the app's navigation
            console.log('User logged out');
          },
        },
      ]
    );
  };

  if (loading) {
    return (
      <View style={styles.container}>
        <Text>Yükleniyor...</Text>
      </View>
    );
  }

  if (!userProfile) {
    return (
      <View style={styles.container}>
        <Text>Kullanıcı bilgileri yüklenemedi</Text>
        <Button title="Tekrar Dene" onPress={fetchUserProfile} />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Kullanıcı Profili</Text>
      
      <View style={styles.profileInfo}>
        <Text style={styles.label}>Email:</Text>
        <Text style={styles.value}>{userProfile.email}</Text>
        
        {userProfile.firstName && (
          <>
            <Text style={styles.label}>Ad:</Text>
            <Text style={styles.value}>{userProfile.firstName} {userProfile.lastName || ''}</Text>
          </>
        )}
        
        {userProfile.phone && (
          <>
            <Text style={styles.label}>Telefon:</Text>
            <Text style={styles.value}>{userProfile.phone}</Text>
          </>
        )}
      </View>

      <View style={styles.buttons}>
        <Button title="Profili Düzenle" onPress={() => {
          // TODO: Navigate to profile edit screen
          Alert.alert('Bilgi', 'Profil düzenleme sayfası yakında eklenecek');
        }} />
        
        <Button title="Tercihler" onPress={() => {
          // TODO: Navigate to preferences screen
          Alert.alert('Bilgi', 'Tercihler sayfası yakında eklenecek');
        }} />
        
        <Button 
          title="Çıkış Yap" 
          onPress={handleLogout}
          color="#ff4444"
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    padding: 16,
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
    marginBottom: 16,
  },
  profileInfo: {
    marginBottom: 24,
  },
  label: {
    fontSize: 14,
    color: '#666',
    marginTop: 8,
  },
  value: {
    fontSize: 16,
    fontWeight: '500',
    marginBottom: 4,
  },
  buttons: {
    gap: 12,
  },
});
