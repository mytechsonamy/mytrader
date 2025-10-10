import React, { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import authSlice, { AuthState } from '../store/slices/authSlice';
import uiSlice from '../store/slices/uiSlice';
import marketSlice, { MarketState } from '../store/slices/marketSlice';

// This type interface extends the default options for render from RTL, as well
// as allows the user to specify other things such as initialState, store.
interface ExtendedRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  preloadedState?: {
    auth?: Partial<AuthState>;
    market?: Partial<MarketState>;
    ui?: any;
  };
  store?: any;
  route?: string;
}

export function renderWithProviders(
  ui: ReactElement,
  {
    preloadedState = {},
    // Automatically create a store instance if no store was passed in
    store = configureStore({
      reducer: {
        auth: authSlice,
        ui: uiSlice,
        market: marketSlice,
      },
      preloadedState,
      middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware({
          serializableCheck: {
            ignoredActions: ['persist/PERSIST', 'persist/REHYDRATE'],
          },
        }),
    }),
    route = '/',
    ...renderOptions
  }: ExtendedRenderOptions = {}
) {
  // Set initial route
  window.history.pushState({}, 'Test page', route);

  function Wrapper({ children }: { children?: React.ReactNode }) {
    return (
      <Provider store={store}>
        <BrowserRouter>
          {children}
        </BrowserRouter>
      </Provider>
    );
  }

  // Return an object with the store and all of RTL's query functions
  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) };
}

// Mock user data for testing
export const mockUser = {
  id: 'test-user-1',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  phone: '+1234567890',
  isActive: true,
  isEmailVerified: true,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  plan: 'basic',
};

export const mockSessionResponse = {
  accessToken: 'mock-access-token',
  refreshToken: 'mock-refresh-token',
  user: mockUser,
  accessTokenExpiresAt: '2024-12-31T23:59:59Z',
  refreshTokenExpiresAt: '2025-01-31T23:59:59Z',
  tokenType: 'Bearer',
  jwtId: 'test-jwt-id',
  sessionId: 'test-session-id',
};

export const mockSymbolsResponse = {
  symbols: {
    'BTCUSDT': {
      symbol: 'BTCUSDT',
      display_name: 'Bitcoin / Tether',
      precision: 2,
      strategy_type: 'crypto',
    },
    'ETHUSDT': {
      symbol: 'ETHUSDT',
      display_name: 'Ethereum / Tether',
      precision: 2,
      strategy_type: 'crypto',
    },
  },
  interval: '1m',
};

export const mockMarketData = {
  symbolId: 'BTCUSDT',
  ticker: 'BTCUSDT',
  price: 50000,
  volume: 1000000,
  change24h: 2500,
  changePercent24h: 5.26,
  high24h: 51000,
  low24h: 48000,
  timestamp: '2024-01-01T12:00:00Z',
};

// Helper function to create authenticated state
export const createAuthenticatedState = () => ({
  auth: {
    user: mockUser,
    token: 'mock-token',
    refreshToken: 'mock-refresh-token',
    sessionId: 'mock-session-id',
    isAuthenticated: true,
    isGuest: false,
    isLoading: false,
    error: null,
  },
});

// Helper function to create guest state
export const createGuestState = () => ({
  auth: {
    user: null,
    token: null,
    refreshToken: null,
    sessionId: null,
    isAuthenticated: false,
    isGuest: true,
    isLoading: false,
    error: null,
  },
});

// Helper function to create market state with data
export const createMarketStateWithData = () => ({
  market: {
    symbols: mockSymbolsResponse,
    marketData: {
      'BTCUSDT': mockMarketData,
    },
    selectedSymbols: ['BTCUSDT'],
    isLoading: false,
    error: null,
    lastUpdate: '2024-01-01T12:00:00Z',
  },
});

// Re-export everything from React Testing Library
export * from '@testing-library/react';
export { default as userEvent } from '@testing-library/user-event';