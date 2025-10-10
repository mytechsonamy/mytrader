import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { useColorScheme } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';

const THEME_STORAGE_KEY = '@mytrader_theme_preference';

export interface ThemeColors {
  // Background colors
  background: string;
  surface: string;
  surfaceVariant: string;
  
  // Primary colors
  primary: string;
  primaryVariant: string;
  
  // Text colors
  text: string;
  textSecondary: string;
  textTertiary: string;
  
  // Border and divider colors
  border: string;
  divider: string;
  
  // Status colors
  success: string;
  error: string;
  warning: string;
  info: string;
  
  // Chart colors
  chartPositive: string;
  chartNegative: string;
  
  // Card and component colors
  card: string;
  cardHover: string;
  
  // Input colors
  inputBackground: string;
  inputBorder: string;
  inputPlaceholder: string;
  
  // Shadow colors
  shadow: string;
}

export type ThemeMode = 'light' | 'dark' | 'system';

export interface ThemeContextType {
  theme: 'light' | 'dark';
  themeMode: ThemeMode;
  colors: ThemeColors;
  toggleTheme: () => void;
  setThemeMode: (mode: ThemeMode) => void;
  isDark: boolean;
}

const lightTheme: ThemeColors = {
  // Background colors
  background: '#FFFFFF',
  surface: '#F8FAFC',
  surfaceVariant: '#F1F5F9',
  
  // Primary colors
  primary: '#3B82F6',
  primaryVariant: '#2563EB',
  
  // Text colors
  text: '#1F2937',
  textSecondary: '#6B7280',
  textTertiary: '#9CA3AF',
  
  // Border and divider colors
  border: '#E5E7EB',
  divider: '#F3F4F6',
  
  // Status colors
  success: '#10B981',
  error: '#EF4444',
  warning: '#F59E0B',
  info: '#3B82F6',
  
  // Chart colors
  chartPositive: '#10B981',
  chartNegative: '#EF4444',
  
  // Card and component colors
  card: '#FFFFFF',
  cardHover: '#F9FAFB',
  
  // Input colors
  inputBackground: '#FFFFFF',
  inputBorder: '#D1D5DB',
  inputPlaceholder: '#9CA3AF',
  
  // Shadow colors
  shadow: 'rgba(0, 0, 0, 0.1)',
};

const darkTheme: ThemeColors = {
  // Background colors
  background: '#111827',
  surface: '#1F2937',
  surfaceVariant: '#374151',
  
  // Primary colors
  primary: '#60A5FA',
  primaryVariant: '#3B82F6',
  
  // Text colors
  text: '#F9FAFB',
  textSecondary: '#D1D5DB',
  textTertiary: '#9CA3AF',
  
  // Border and divider colors
  border: '#374151',
  divider: '#2D3748',
  
  // Status colors
  success: '#34D399',
  error: '#F87171',
  warning: '#FBBF24',
  info: '#60A5FA',
  
  // Chart colors
  chartPositive: '#34D399',
  chartNegative: '#F87171',
  
  // Card and component colors
  card: '#1F2937',
  cardHover: '#374151',
  
  // Input colors
  inputBackground: '#1F2937',
  inputBorder: '#4B5563',
  inputPlaceholder: '#6B7280',
  
  // Shadow colors
  shadow: 'rgba(0, 0, 0, 0.3)',
};

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within a ThemeProvider');
  }
  return context;
};

interface ThemeProviderProps {
  children: ReactNode;
}

export const ThemeProvider: React.FC<ThemeProviderProps> = ({ children }) => {
  const systemColorScheme = useColorScheme();
  const [themeMode, setThemeModeState] = useState<ThemeMode>('system');
  const [isLoading, setIsLoading] = useState(true);

  // Determine the actual theme based on mode and system preference
  const getEffectiveTheme = (): 'light' | 'dark' => {
    if (themeMode === 'system') {
      return systemColorScheme === 'dark' ? 'dark' : 'light';
    }
    return themeMode;
  };

  const theme = getEffectiveTheme();
  const colors = theme === 'dark' ? darkTheme : lightTheme;

  // Load saved theme preference on mount
  useEffect(() => {
    loadThemePreference();
  }, []);

  // Listen to system theme changes when in system mode
  useEffect(() => {
    if (themeMode === 'system') {
      // Theme will automatically update when systemColorScheme changes
    }
  }, [systemColorScheme, themeMode]);

  const loadThemePreference = async () => {
    try {
      const savedTheme = await AsyncStorage.getItem(THEME_STORAGE_KEY);
      if (savedTheme && (savedTheme === 'light' || savedTheme === 'dark' || savedTheme === 'system')) {
        setThemeModeState(savedTheme as ThemeMode);
      }
    } catch (error) {
      console.error('Error loading theme preference:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const saveThemePreference = async (mode: ThemeMode) => {
    try {
      await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
    } catch (error) {
      console.error('Error saving theme preference:', error);
    }
  };

  const setThemeMode = (mode: ThemeMode) => {
    setThemeModeState(mode);
    saveThemePreference(mode);
  };

  const toggleTheme = () => {
    const currentTheme = getEffectiveTheme();
    const newMode = currentTheme === 'light' ? 'dark' : 'light';
    setThemeMode(newMode);
  };

  const value: ThemeContextType = {
    theme,
    themeMode,
    colors,
    toggleTheme,
    setThemeMode,
    isDark: theme === 'dark',
  };

  // Don't render children until theme is loaded to prevent flash
  if (isLoading) {
    return null;
  }

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
};
