import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Text, View, TouchableOpacity, StyleSheet, Alert } from 'react-native';
import ErrorBoundary from '../components/dashboard/ErrorBoundary';

import { useAuth } from '../context/AuthContext';
import { RootStackParamList, AuthStackParamList, MainTabsParamList } from '../types';

import LoginScreen from '../screens/LoginScreen';
import RegisterScreen from '../screens/RegisterScreen';
import ForgotPasswordStart from '../screens/ForgotPasswordStart';
import ForgotPasswordVerify from '../screens/ForgotPasswordVerify';
import ResetPasswordScreen from '../screens/ResetPasswordScreen';
import DashboardScreen from '../screens/DashboardScreen';
import PortfolioScreen from '../screens/PortfolioScreen';
import StrategiesScreen from '../screens/StrategiesScreen';
import StrategyTestScreen from '../screens/StrategyTestScreen';
import EnhancedLeaderboardScreen from '../screens/EnhancedLeaderboardScreen';
import EnhancedProfileScreen from '../screens/EnhancedProfileScreen';
import TestErrorBoundaryScreen from '../screens/TestErrorBoundary';

const RootStack = createStackNavigator<RootStackParamList>();
const AuthStack = createStackNavigator<AuthStackParamList>();
const MainTabs = createBottomTabNavigator<MainTabsParamList>();

const AuthNavigator = () => (
  <AuthStack.Navigator
    screenOptions={({ navigation }) => ({
      headerShown: true,
      headerStyle: { backgroundColor: 'white' },
      headerTitleStyle: { fontWeight: '700', color: '#333' },
      headerBackTitleVisible: false,
      headerBackTitle: '',
      headerRight: () => (
        <TouchableOpacity
          onPress={() => navigation.getParent()?.goBack()}
          style={{ paddingHorizontal: 16, paddingVertical: 8 }}
          accessibilityLabel="Kapat"
        >
          <Text style={{ fontSize: 18 }}>✖️</Text>
        </TouchableOpacity>
      ),
    })}
  >
    <AuthStack.Screen 
      name="Login" 
      component={LoginScreen}
      options={{ title: 'Giriş' }}
    />
    <AuthStack.Screen 
      name="Register" 
      component={RegisterScreen}
      options={{ title: 'Kayıt Ol' }}
    />
    <AuthStack.Screen
      name="ForgotPasswordStart"
      component={ForgotPasswordStart}
      options={{ title: 'Şifremi Unuttum' }}
    />
    <AuthStack.Screen
      name="ForgotPasswordVerify"
      component={ForgotPasswordVerify}
      options={{ title: 'Doğrulama' }}
    />
    <AuthStack.Screen
      name="ResetPassword"
      component={ResetPasswordScreen}
      options={{ title: 'Yeni Şifre' }}
    />
  </AuthStack.Navigator>
);

// Safe component wrapper for tab screens
const SafeScreen = ({
  Component,
  screenName
}: {
  Component: React.ComponentType<any>;
  screenName: string;
}) => (props: any) => (
  <ErrorBoundary
    isolate={true}
    onError={(error, errorInfo) => {
      console.error(`${screenName} Screen Error:`, { error, errorInfo });
      // Log to crash reporting in production
      if (!__DEV__) {
        // Add crash reporting here
      }
    }}
    fallback={(error, errorInfo, retry) => (
      <View style={profileStyles.screenErrorContainer}>
        <Text style={profileStyles.screenErrorIcon}>📱</Text>
        <Text style={profileStyles.screenErrorTitle}>{screenName} Hatası</Text>
        <Text style={profileStyles.screenErrorMessage}>
          {screenName} ekranı yüklenirken bir sorun oluştu.
        </Text>
        <TouchableOpacity
          style={profileStyles.screenErrorButton}
          onPress={() => {
            retry();
            // Also try to refresh the navigation state
            props.navigation?.reset({
              index: 0,
              routes: [{ name: props.route?.name || 'Dashboard' }],
            });
          }}
        >
          <Text style={profileStyles.screenErrorButtonText}>🔄 Yeniden Yükle</Text>
        </TouchableOpacity>
      </View>
    )}
  >
    <Component {...props} />
  </ErrorBoundary>
);

const MainTabsNavigator = () => (
  <ErrorBoundary
    onError={(error, errorInfo) => {
      console.error('Tab Navigation Error:', { error, errorInfo });
      Alert.alert(
        'Navigasyon Hatası',
        'Sekme geçişlerinde bir sorun oluştu. Uygulama yeniden başlatılıyor.',
        [{ text: 'Tamam' }]
      );
    }}
  >
    <MainTabs.Navigator
      screenOptions={{
        headerShown: false,
        tabBarStyle: {
          backgroundColor: '#667eea',
          borderTopWidth: 0,
          elevation: 8,
          shadowColor: '#000',
          shadowOffset: { width: 0, height: -2 },
          shadowOpacity: 0.1,
          shadowRadius: 4,
          paddingBottom: 5,
          height: 60,
        },
        tabBarActiveTintColor: 'white',
        tabBarInactiveTintColor: 'rgba(255,255,255,0.6)',
        tabBarLabelStyle: {
          fontSize: 12,
          fontWeight: '600',
          marginBottom: 3,
        },
      }}
    >
      <MainTabs.Screen
        name="Dashboard"
        component={SafeScreen({ Component: DashboardScreen, screenName: 'Ana Sayfa' })}
        options={{
          tabBarLabel: 'Ana Sayfa',
          tabBarIcon: ({ color }) => <Text style={{ fontSize: 20, color }}>🏠</Text>,
        }}
      />
      <MainTabs.Screen
        name="Portfolio"
        component={SafeScreen({ Component: PortfolioScreen, screenName: 'Portföy' })}
        options={{
          tabBarLabel: 'Portföy',
          tabBarIcon: ({ color }) => <Text style={{ fontSize: 20, color }}>💼</Text>,
        }}
      />
      <MainTabs.Screen
        name="Strategies"
        component={SafeScreen({ Component: StrategiesScreen, screenName: 'Stratejiler' })}
        options={{
          tabBarLabel: 'Stratejiler',
          tabBarIcon: ({ color }) => <Text style={{ fontSize: 20, color }}>⚡</Text>,
        }}
      />
      <MainTabs.Screen
        name="Gamification"
        component={SafeScreen({ Component: EnhancedLeaderboardScreen, screenName: 'Strategist' })}
        options={{
          tabBarLabel: 'Strategist',
          tabBarIcon: ({ color }) => <Text style={{ fontSize: 20, color }}>🏆</Text>,
        }}
      />
      <MainTabs.Screen
        name="Profile"
        component={SafeScreen({ Component: EnhancedProfileScreen, screenName: 'Profil' })}
        options={{
          tabBarLabel: 'Profil',
          tabBarIcon: ({ color }) => <Text style={{ fontSize: 20, color }}>👤</Text>,
        }}
      />

    </MainTabs.Navigator>
  </ErrorBoundary>
);

const AppNavigation = () => {
  const { isLoading } = useAuth();

  if (isLoading) {
    return (
      <View style={profileStyles.container}>
        <Text style={profileStyles.loading}>🚀 Yükleniyor...</Text>
      </View>
    );
  }

  return (
    <NavigationContainer>
      <RootStack.Navigator screenOptions={{ headerShown: false }}>
        <RootStack.Screen name="MainTabs" component={MainTabsNavigator} />
        <RootStack.Screen 
          name="AuthStack" 
          component={AuthNavigator} 
          options={{ presentation: 'modal' }}
        />
        <RootStack.Screen 
          name="StrategyTest" 
          component={StrategyTestScreen}
          options={{ presentation: 'modal' }}
        />
      </RootStack.Navigator>
    </NavigationContainer>
  );
};

const profileStyles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
    padding: 20,
    paddingTop: 60,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#333',
    textAlign: 'center',
    marginBottom: 30,
  },
  userInfo: {
    backgroundColor: 'white',
    borderRadius: 15,
    padding: 20,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  userName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 8,
  },
  userEmail: {
    fontSize: 16,
    color: '#666',
    marginBottom: 15,
  },
  userDetail: {
    fontSize: 14,
    color: '#888',
    marginBottom: 5,
  },
  logoutButton: {
    backgroundColor: '#ef4444',
    paddingHorizontal: 30,
    paddingVertical: 12,
    borderRadius: 25,
    marginTop: 20,
  },
  logoutText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  guestTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 8,
    textAlign: 'center',
  },
  guestText: {
    fontSize: 14,
    color: '#666',
    textAlign: 'center',
    marginBottom: 20,
  },
  loginButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 30,
    paddingVertical: 12,
    borderRadius: 25,
    marginTop: 10,
  },
  loginText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  comingSoon: {
    fontSize: 18,
    color: '#666',
    textAlign: 'center',
    marginTop: 50,
  },
  loading: {
    fontSize: 18,
    color: '#666',
    textAlign: 'center',
    flex: 1,
    textAlignVertical: 'center',
  },
  // Screen Error Styles
  screenErrorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f8fafc',
    padding: 30,
  },
  screenErrorIcon: {
    fontSize: 64,
    marginBottom: 20,
  },
  screenErrorTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 12,
    textAlign: 'center',
  },
  screenErrorMessage: {
    fontSize: 16,
    color: '#6b7280',
    textAlign: 'center',
    marginBottom: 24,
    lineHeight: 22,
  },
  screenErrorButton: {
    backgroundColor: '#667eea',
    borderRadius: 12,
    paddingHorizontal: 24,
    paddingVertical: 14,
  },
  screenErrorButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default AppNavigation;
