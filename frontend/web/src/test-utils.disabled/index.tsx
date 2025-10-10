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

// Export accessibility testing utilities
export * from './accessibility';

// Export test data factories
export * from './factories';

// Export additional test helpers
export { default as userEvent } from '@testing-library/user-event';

// Mock data for backward compatibility
export const mockMarketDataArray = [
  mockMarketData,
  {
    ...mockMarketData,
    symbol: 'ETHUSDT',
    price: 3000,
    priceChangePercent: -2.5,
  },
  {
    ...mockMarketData,
    symbol: 'ADAUSDT',
    price: 0.5,
    priceChangePercent: 1.8,
  },
];

// WebSocket test helpers
export const createMockWebSocketService = () => ({
  connect: vi.fn().mockResolvedValue(undefined),
  disconnect: vi.fn().mockResolvedValue(undefined),
  isConnected: vi.fn().mockReturnValue(false),
  subscribeToMultipleSymbols: vi.fn().mockResolvedValue(undefined),
  unsubscribeFromSymbol: vi.fn().mockResolvedValue(undefined),
  subscribeToSymbol: vi.fn().mockResolvedValue(undefined),
  getConnectionState: vi.fn().mockReturnValue('Disconnected'),
  destroy: vi.fn(),
});

// API response helpers
export const createMockApiSuccess = <T>(data: T) => ({
  data,
  success: true,
  status: 200,
  message: 'Success',
});

export const createMockApiError = (message: string, status: number = 400) => ({
  success: false,
  status,
  message,
  error: message,
});

// Test environment helpers
export const mockConsoleMethod = (method: keyof Console) => {
  const originalMethod = console[method];
  const mockedMethod = vi.fn();

  beforeEach(() => {
    (console[method] as any) = mockedMethod;
  });

  afterEach(() => {
    (console[method] as any) = originalMethod;
    mockedMethod.mockClear();
  });

  return mockedMethod;
};

// DOM testing helpers
export const waitForLoadingToFinish = () =>
  waitFor(() => {
    expect(screen.queryByText(/loading/i)).not.toBeInTheDocument();
  });

export const waitForErrorToAppear = (errorText?: string) =>
  waitFor(() => {
    if (errorText) {
      expect(screen.getByText(errorText)).toBeInTheDocument();
    } else {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    }
  });

// Form testing helpers
export const fillForm = async (fields: Record<string, string>) => {
  const user = userEvent.setup();

  for (const [fieldName, value] of Object.entries(fields)) {
    const field = screen.getByLabelText(new RegExp(fieldName, 'i'));
    await user.clear(field);
    await user.type(field, value);
  }
};

export const submitForm = async () => {
  const user = userEvent.setup();
  const submitButton = screen.getByRole('button', { name: /submit|login|register|save/i });
  await user.click(submitButton);
};

// Animation and timing helpers
export const skipAnimations = () => {
  const style = document.createElement('style');
  style.innerHTML = `
    *, *::before, *::after {
      animation-duration: 0s !important;
      animation-delay: 0s !important;
      transition-duration: 0s !important;
      transition-delay: 0s !important;
    }
  `;
  document.head.appendChild(style);

  return () => {
    document.head.removeChild(style);
  };
};

// Intersection Observer mock
export const mockIntersectionObserver = () => {
  const mockIntersectionObserver = vi.fn();
  mockIntersectionObserver.mockReturnValue({
    observe: () => null,
    unobserve: () => null,
    disconnect: () => null,
  });

  Object.defineProperty(window, 'IntersectionObserver', {
    writable: true,
    configurable: true,
    value: mockIntersectionObserver,
  });

  return mockIntersectionObserver;
};

// Resize Observer mock
export const mockResizeObserver = () => {
  const mockResizeObserver = vi.fn();
  mockResizeObserver.mockReturnValue({
    observe: () => null,
    unobserve: () => null,
    disconnect: () => null,
  });

  Object.defineProperty(window, 'ResizeObserver', {
    writable: true,
    configurable: true,
    value: mockResizeObserver,
  });

  return mockResizeObserver;
};

// Import vi for the mock functions above
import { vi, beforeEach, afterEach } from 'vitest';
import { waitFor, screen } from '@testing-library/react';

// Re-export everything from React Testing Library
export * from '@testing-library/react';