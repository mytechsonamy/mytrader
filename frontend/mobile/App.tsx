import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { StyleSheet, View, Text, Alert } from 'react-native';

import { AuthProvider } from './src/context/AuthContext';
import { PriceProvider } from './src/context/PriceContext';
import { PortfolioProvider } from './src/context/PortfolioContext';
import { ErrorNotificationProvider } from './src/context/ErrorNotificationContext';
import { ThemeProvider } from './src/context/ThemeContext';
import AppNavigation from './src/navigation/AppNavigation';
import ErrorBoundary from './src/components/dashboard/ErrorBoundary';

export default function App() {
  console.log('App component rendering...');

  return (
    <ErrorBoundary
      onError={(error, errorInfo) => {
        console.error('App-level error caught:', { error, errorInfo });
        // Log to crash reporting service in production
        if (!__DEV__) {
          // Add crash reporting here (e.g., Crashlytics)
        }
      }}
      fallback={(error, errorInfo, retry) => (
        <View style={[styles.container, styles.errorContainer]}>
          <Text style={styles.errorIcon}>😔</Text>
          <Text style={styles.errorTitle}>Uygulama Hatası</Text>
          <Text style={styles.errorMessage}>
            Beklenmeyen bir hata oluştu. Uygulama yeniden başlatılacak.
          </Text>
          <Text
            style={styles.retryButton}
            onPress={() => {
              retry();
              // Force app reload if possible
              if (__DEV__) {
                console.log('Reloading app...');
              }
            }}
          >
            🔄 Yeniden Dene
          </Text>
          {__DEV__ && (
            <Text style={styles.debugText} numberOfLines={3}>
              {error.message}
            </Text>
          )}
        </View>
      )}
    >
      <GestureHandlerRootView style={styles.container}>
        <SafeAreaProvider>
          <ThemeProvider>
            <ErrorNotificationProvider>
              <AuthProvider>
                <PriceProvider>
                  <PortfolioProvider>
                    <AppNavigation />
                    <StatusBar style="light" />
                  </PortfolioProvider>
                </PriceProvider>
              </AuthProvider>
            </ErrorNotificationProvider>
          </ThemeProvider>
        </SafeAreaProvider>
      </GestureHandlerRootView>
    </ErrorBoundary>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  errorContainer: {
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f8fafc',
    paddingHorizontal: 20,
  },
  errorIcon: {
    fontSize: 64,
    marginBottom: 20,
  },
  errorTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 12,
    textAlign: 'center',
  },
  errorMessage: {
    fontSize: 16,
    color: '#6b7280',
    textAlign: 'center',
    marginBottom: 30,
    lineHeight: 22,
  },
  retryButton: {
    fontSize: 18,
    fontWeight: '600',
    color: '#667eea',
    backgroundColor: 'white',
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 12,
    textAlign: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  debugText: {
    fontSize: 12,
    color: '#ef4444',
    marginTop: 20,
    textAlign: 'center',
    fontFamily: 'monospace',
    backgroundColor: '#fef2f2',
    padding: 10,
    borderRadius: 8,
  },
});
