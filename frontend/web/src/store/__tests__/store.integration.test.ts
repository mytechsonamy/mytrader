import { describe, it, expect, beforeEach, vi } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';
import { store } from '../index';
import { loginAsync, logout, setCredentials } from '../slices/authSlice';
import { fetchSymbols, fetchMarketData, updateMarketData } from '../slices/marketSlice';
import { addNotification, setTheme } from '../slices/uiSlice';
import { mockSessionResponse, mockSymbolsResponse, mockMarketData } from '../test-utils';

// Mock the services
vi.mock('../services/authService', () => ({
  authService: {
    login: vi.fn(),
    logout: vi.fn(),
    getStoredUser: vi.fn().mockReturnValue(null),
    getToken: vi.fn().mockReturnValue(null),
    getRefreshToken: vi.fn().mockReturnValue(null),
    isAuthenticated: vi.fn().mockReturnValue(false),
  },
}));

vi.mock('../services/marketDataService', () => ({
  marketDataService: {
    getSymbols: vi.fn(),
    getMultipleMarketData: vi.fn(),
  },
}));

import { authService } from '../services/authService';
import { marketDataService } from '../services/marketDataService';

const mockAuthService = authService as any;
const mockMarketDataService = marketDataService as any;

describe('Store Integration Tests', () => {
  let testStore: typeof store;

  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();

    // Create a fresh store for each test
    testStore = configureStore({
      reducer: {
        auth: (await import('../slices/authSlice')).default,
        ui: (await import('../slices/uiSlice')).default,
        market: (await import('../slices/marketSlice')).default,
      },
      middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware({
          serializableCheck: {
            ignoredActions: ['persist/PERSIST', 'persist/REHYDRATE'],
          },
        }),
    });
  });

  describe('Authentication Flow Integration', () => {
    it('should handle complete login flow with UI feedback', async () => {
      mockAuthService.login.mockResolvedValue(mockSessionResponse);

      // Start login process
      const loginPromise = testStore.dispatch(loginAsync({
        email: 'test@example.com',
        password: 'password123',
      }));

      // Check loading state
      let state = testStore.getState();
      expect(state.auth.isLoading).toBe(true);

      // Add notification about login attempt
      testStore.dispatch(addNotification({
        type: 'info',
        message: 'Logging in...',
      }));

      await loginPromise;

      // Check final state
      state = testStore.getState();
      expect(state.auth.isAuthenticated).toBe(true);
      expect(state.auth.isLoading).toBe(false);
      expect(state.auth.user).toEqual(mockSessionResponse.user);
      expect(state.ui.notifications).toHaveLength(1);
    });

    it('should handle logout flow with state cleanup', async () => {
      // First login
      testStore.dispatch(setCredentials(mockSessionResponse));

      // Add some market data
      testStore.dispatch(updateMarketData(mockMarketData));

      // Add notification
      testStore.dispatch(addNotification({
        type: 'success',
        message: 'Welcome back!',
      }));

      // Now logout
      testStore.dispatch(logout());

      const state = testStore.getState();

      // Auth state should be cleared
      expect(state.auth.isAuthenticated).toBe(false);
      expect(state.auth.isGuest).toBe(true);
      expect(state.auth.user).toBeNull();
      expect(state.auth.token).toBeNull();

      // Market data should remain (public access)
      expect(state.market.marketData).toEqual({ [mockMarketData.symbolId]: mockMarketData });

      // UI state should remain
      expect(state.ui.notifications).toHaveLength(1);
    });

    it('should handle login failure with error notification', async () => {
      const errorMessage = 'Invalid credentials';
      mockAuthService.login.mockRejectedValue(new Error(errorMessage));

      await testStore.dispatch(loginAsync({
        email: 'test@example.com',
        password: 'wrongpassword',
      }));

      // Add error notification
      testStore.dispatch(addNotification({
        type: 'error',
        message: errorMessage,
      }));

      const state = testStore.getState();
      expect(state.auth.isAuthenticated).toBe(false);
      expect(state.auth.error).toBe(errorMessage);
      expect(state.ui.notifications).toHaveLength(1);
      expect(state.ui.notifications[0].type).toBe('error');
    });
  });

  describe('Market Data Integration', () => {
    it('should handle public market data access for guest users', async () => {
      mockMarketDataService.getSymbols.mockResolvedValue(mockSymbolsResponse);
      mockMarketDataService.getMultipleMarketData.mockResolvedValue([mockMarketData]);

      // Fetch symbols as guest
      await testStore.dispatch(fetchSymbols());

      let state = testStore.getState();
      expect(state.auth.isGuest).toBe(true);
      expect(state.market.symbols).toEqual(mockSymbolsResponse);

      // Fetch market data as guest
      await testStore.dispatch(fetchMarketData(['BTCUSDT']));

      state = testStore.getState();
      expect(state.market.marketData['BTCUSDT']).toEqual(mockMarketData);
    });

    it('should handle market data updates for authenticated users', async () => {
      // Login first
      testStore.dispatch(setCredentials(mockSessionResponse));

      // Fetch initial data
      mockMarketDataService.getSymbols.mockResolvedValue(mockSymbolsResponse);
      await testStore.dispatch(fetchSymbols());

      // Simulate real-time updates
      const updatedData = { ...mockMarketData, price: 55000 };
      testStore.dispatch(updateMarketData(updatedData));

      const state = testStore.getState();
      expect(state.auth.isAuthenticated).toBe(true);
      expect(state.market.marketData['BTCUSDT'].price).toBe(55000);
      expect(state.market.lastUpdate).toBeTruthy();
    });

    it('should handle market data errors with notifications', async () => {
      const errorMessage = 'Market data service unavailable';
      mockMarketDataService.getSymbols.mockRejectedValue(new Error(errorMessage));

      await testStore.dispatch(fetchSymbols());

      testStore.dispatch(addNotification({
        type: 'error',
        message: errorMessage,
      }));

      const state = testStore.getState();
      expect(state.market.error).toBe(errorMessage);
      expect(state.ui.notifications).toHaveLength(1);
      expect(state.ui.notifications[0].type).toBe('error');
    });
  });

  describe('Cross-Slice State Management', () => {
    it('should handle theme changes affecting entire application', () => {
      // Login user
      testStore.dispatch(setCredentials(mockSessionResponse));

      // Add market data
      testStore.dispatch(updateMarketData(mockMarketData));

      // Change theme
      testStore.dispatch(setTheme('light'));

      const state = testStore.getState();
      expect(state.ui.theme).toBe('light');
      expect(state.auth.isAuthenticated).toBe(true); // Should not affect auth
      expect(state.market.marketData).toEqual({ [mockMarketData.symbolId]: mockMarketData }); // Should not affect market
    });

    it('should handle concurrent operations across slices', async () => {
      mockAuthService.login.mockResolvedValue(mockSessionResponse);
      mockMarketDataService.getSymbols.mockResolvedValue(mockSymbolsResponse);

      // Dispatch multiple operations concurrently
      const operations = [
        testStore.dispatch(loginAsync({ email: 'test@example.com', password: 'password123' })),
        testStore.dispatch(fetchSymbols()),
        testStore.dispatch(setTheme('light')),
      ];

      testStore.dispatch(addNotification({
        type: 'info',
        message: 'Loading application...',
      }));

      await Promise.all(operations);

      const state = testStore.getState();
      expect(state.auth.isAuthenticated).toBe(true);
      expect(state.market.symbols).toEqual(mockSymbolsResponse);
      expect(state.ui.theme).toBe('light');
      expect(state.ui.notifications).toHaveLength(1);
    });

    it('should maintain data consistency during rapid state changes', () => {
      // Simulate rapid user interactions
      const actions = [
        setCredentials(mockSessionResponse),
        updateMarketData(mockMarketData),
        addNotification({ type: 'success', message: 'Connected' }),
        setTheme('dark'),
        updateMarketData({ ...mockMarketData, price: 51000 }),
        addNotification({ type: 'info', message: 'Price updated' }),
        logout(),
        setTheme('light'),
      ];

      actions.forEach(action => {
        testStore.dispatch(action);
      });

      const state = testStore.getState();

      // Final state should be consistent
      expect(state.auth.isAuthenticated).toBe(false); // Logged out
      expect(state.auth.isGuest).toBe(true);
      expect(state.market.marketData['BTCUSDT'].price).toBe(51000); // Market data persists
      expect(state.ui.theme).toBe('light'); // Theme change applied
      expect(state.ui.notifications).toHaveLength(2); // Notifications preserved
    });
  });

  describe('Performance and Memory Management', () => {
    it('should handle large datasets efficiently', () => {
      const startTime = performance.now();

      // Simulate large number of market updates
      for (let i = 0; i < 1000; i++) {
        testStore.dispatch(updateMarketData({
          ...mockMarketData,
          symbolId: `SYMBOL${i}`,
          ticker: `SYMBOL${i}`,
          price: 1000 + i,
        }));
      }

      const endTime = performance.now();
      const state = testStore.getState();

      expect(Object.keys(state.market.marketData)).toHaveLength(1000);
      expect(endTime - startTime).toBeLessThan(1000); // Should complete within 1 second
    });

    it('should handle rapid notification management efficiently', () => {
      const startTime = performance.now();

      // Add many notifications
      for (let i = 0; i < 100; i++) {
        testStore.dispatch(addNotification({
          type: 'info',
          message: `Notification ${i}`,
        }));
      }

      const endTime = performance.now();
      const state = testStore.getState();

      expect(state.ui.notifications).toHaveLength(100);
      expect(endTime - startTime).toBeLessThan(500); // Should be very fast
    });
  });

  describe('Error Recovery and State Resilience', () => {
    it('should recover gracefully from partial failures', async () => {
      // Successful login
      mockAuthService.login.mockResolvedValue(mockSessionResponse);
      await testStore.dispatch(loginAsync({ email: 'test@example.com', password: 'password123' }));

      // Failed market data fetch
      mockMarketDataService.getSymbols.mockRejectedValue(new Error('Network error'));
      await testStore.dispatch(fetchSymbols());

      // Successful theme change
      testStore.dispatch(setTheme('light'));

      const state = testStore.getState();

      // Should have mixed state reflecting partial success
      expect(state.auth.isAuthenticated).toBe(true); // Login succeeded
      expect(state.market.error).toBe('Network error'); // Market fetch failed
      expect(state.market.symbols).toBeNull(); // No symbols loaded
      expect(state.ui.theme).toBe('light'); // Theme change succeeded
    });

    it('should maintain state integrity during errors', async () => {
      // Set up initial good state
      testStore.dispatch(setCredentials(mockSessionResponse));
      testStore.dispatch(updateMarketData(mockMarketData));

      // Cause an error
      mockMarketDataService.getSymbols.mockRejectedValue(new Error('Service unavailable'));
      await testStore.dispatch(fetchSymbols());

      const state = testStore.getState();

      // Good state should be preserved
      expect(state.auth.isAuthenticated).toBe(true);
      expect(state.market.marketData['BTCUSDT']).toEqual(mockMarketData);

      // Only the failing operation should show error
      expect(state.market.error).toBe('Service unavailable');
      expect(state.auth.error).toBeNull();
    });
  });
});