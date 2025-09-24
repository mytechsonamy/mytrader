import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { StyleSheet, View, Text } from 'react-native';

import { AuthProvider } from './src/context/AuthContext';
import { PriceProvider } from './src/context/PriceContext';
import { PortfolioProvider } from './src/context/PortfolioContext';
import { ErrorNotificationProvider } from './src/context/ErrorNotificationContext';
import AppNavigation from './src/navigation/AppNavigation';

export default function App() {
  console.log('App component rendering...');
  
  return (
    <GestureHandlerRootView style={styles.container}>
      <SafeAreaProvider>
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
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});
