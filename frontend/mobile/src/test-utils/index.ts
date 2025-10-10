import React, { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react-native';
import { NavigationContainer } from '@react-navigation/native';
import { PriceProvider } from '../context/PriceContext';
import { AuthProvider } from '../context/AuthContext';
import { PortfolioProvider } from '../context/PortfolioContext';

// Mock providers for testing
interface MockAuthContextValue {
  user: any;
  login: jest.MockedFunction<any>;
  logout: jest.MockedFunction<any>;
  register: jest.MockedFunction<any>;
  isAuthenticated: boolean;
  loading: boolean;
}

interface MockPriceContextValue {
  prices: Record<string, any>;
  enhancedPrices: Record<string, any>;
  isConnected: boolean;
  connectionStatus: 'connected' | 'disconnected' | 'connecting' | 'error';
  symbols: any[];
  trackedSymbols: any[];
  getPrice: jest.MockedFunction<any>;
  getEnhancedPrice: jest.MockedFunction<any>;
  subscribeToSymbols: jest.MockedFunction<any>;
  refreshPrices: jest.MockedFunction<any>;
  getAssetClassSummary: jest.MockedFunction<any>;
  getSymbolsByAssetClass: jest.MockedFunction<any>;
}

interface MockPortfolioContextValue {
  state: {
    portfolios: any[];
    currentPortfolio: any;
    positions: any[];
    transactions: any[];
    loadingState: 'idle' | 'loading' | 'error';
    error: string | null;
  };
  loadPortfolios: jest.MockedFunction<any>;
  selectPortfolio: jest.MockedFunction<any>;
  createPortfolio: jest.MockedFunction<any>;
  clearError: jest.MockedFunction<any>;
}

// Default mock values
export const createMockAuthContext = (overrides: Partial<MockAuthContextValue> = {}): MockAuthContextValue => ({
  user: {
    id: '1',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
  },
  login: jest.fn(),
  logout: jest.fn(),
  register: jest.fn(),
  isAuthenticated: true,
  loading: false,
  ...overrides,
});

export const createMockPriceContext = (overrides: Partial<MockPriceContextValue> = {}): MockPriceContextValue => ({
  prices: {
    'BTC-USD': { price: 50000, change: 2500, timestamp: '2023-12-24T10:00:00Z' },
    'ETH-USD': { price: 3000, change: -150, timestamp: '2023-12-24T10:00:00Z' },
  },
  enhancedPrices: {
    '1': {
      symbol: 'BTC-USD',
      price: 50000,
      change: 2500,
      changePercent: 5.2,
      volume: 1000000,
      marketCap: 900000000,
      lastUpdate: '2023-12-24T10:00:00Z',
    },
  },
  isConnected: true,
  connectionStatus: 'connected',
  symbols: [
    { id: '1', ticker: 'BTC-USD', display: 'Bitcoin', venue: 'CRYPTO' },
    { id: '2', ticker: 'ETH-USD', display: 'Ethereum', venue: 'CRYPTO' },
  ],
  trackedSymbols: [
    {
      id: '1',
      symbol: 'BTC-USD',
      displayName: 'Bitcoin',
      assetClass: 'CRYPTO',
      quoteCurrency: 'USD',
    },
  ],
  getPrice: jest.fn((symbol: string) => {
    const mockPrices = overrides.prices || {
      'BTC-USD': { price: 50000, change: 2500 },
      'ETH-USD': { price: 3000, change: -150 },
    };
    return mockPrices[symbol] || null;
  }),
  getEnhancedPrice: jest.fn((symbolId: string) => {
    const mockEnhancedPrices = overrides.enhancedPrices || {
      '1': { symbol: 'BTC-USD', price: 50000, change: 2500 },
    };
    return mockEnhancedPrices[symbolId] || null;
  }),
  subscribeToSymbols: jest.fn(),
  refreshPrices: jest.fn(),
  getAssetClassSummary: jest.fn(() => ({
    CRYPTO: {
      totalSymbols: 5,
      gainers: 3,
      losers: 2,
      averageChange: 2.5,
    },
    STOCK: {
      totalSymbols: 10,
      gainers: 6,
      losers: 4,
      averageChange: 1.2,
    },
  })),
  getSymbolsByAssetClass: jest.fn(() => []),
  ...overrides,
});

export const createMockPortfolioContext = (overrides: Partial<MockPortfolioContextValue> = {}): MockPortfolioContextValue => ({
  state: {
    portfolios: [
      {
        id: '1',
        name: 'Main Portfolio',
        totalValue: 25000,
        dailyPnL: 1250,
        totalPnLPercent: 8.5,
      },
    ],
    currentPortfolio: {
      id: '1',
      name: 'Main Portfolio',
      totalValue: 25000,
      dailyPnL: 1250,
      positions: [],
    },
    positions: [
      {
        id: '1',
        symbol: 'BTC-USD',
        quantity: 0.5,
        averagePrice: 45000,
        marketValue: 25000,
        unrealizedPnL: 2500,
        unrealizedPnLPercent: 11.11,
      },
    ],
    transactions: [
      {
        id: '1',
        symbol: 'BTC-USD',
        type: 'BUY' as const,
        quantity: 0.5,
        price: 45000,
        amount: 22500,
        executedAt: '2023-12-01T10:00:00Z',
      },
    ],
    loadingState: 'idle' as const,
    error: null,
  },
  loadPortfolios: jest.fn(),
  selectPortfolio: jest.fn(),
  createPortfolio: jest.fn(),
  clearError: jest.fn(),
  ...overrides,
});

// Mock React Navigation
export const createMockNavigation = () => ({
  navigate: jest.fn(),
  goBack: jest.fn(),
  reset: jest.fn(),
  setOptions: jest.fn(),
  dispatch: jest.fn(),
  canGoBack: jest.fn(() => true),
  getId: jest.fn(() => 'test-id'),
  getParent: jest.fn(),
  getState: jest.fn(),
  addListener: jest.fn(),
  removeListener: jest.fn(),
  isFocused: jest.fn(() => true),
});

// Custom render with providers
interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  authContext?: Partial<MockAuthContextValue>;
  priceContext?: Partial<MockPriceContextValue>;
  portfolioContext?: Partial<MockPortfolioContextValue>;
  includeNavigation?: boolean;
}

const AllTheProviders: React.FC<{
  children: React.ReactNode;
  authContext?: MockAuthContextValue;
  priceContext?: MockPriceContextValue;
  portfolioContext?: MockPortfolioContextValue;
  includeNavigation?: boolean;
}> = ({
  children,
  authContext,
  priceContext,
  portfolioContext,
  includeNavigation = false,
}) => {
  // Mock context providers
  const MockAuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const value = React.useMemo(() => authContext || createMockAuthContext(), []);
    return React.createElement(
      React.createContext(value).Provider,
      { value },
      children
    );
  };

  const MockPriceProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const value = React.useMemo(() => priceContext || createMockPriceContext(), []);
    return React.createElement(
      React.createContext(value).Provider,
      { value },
      children
    );
  };

  const MockPortfolioProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const value = React.useMemo(() => portfolioContext || createMockPortfolioContext(), []);
    return React.createElement(
      React.createContext(value).Provider,
      { value },
      children
    );
  };

  const content = (
    <MockAuthProvider>
      <MockPriceProvider>
        <MockPortfolioProvider>
          {children}
        </MockPortfolioProvider>
      </MockPriceProvider>
    </MockAuthProvider>
  );

  if (includeNavigation) {
    return (
      <NavigationContainer>
        {content}
      </NavigationContainer>
    );
  }

  return <>{content}</>;
};

export const customRender = (
  ui: ReactElement,
  options: CustomRenderOptions = {}
) => {
  const {
    authContext,
    priceContext,
    portfolioContext,
    includeNavigation,
    ...renderOptions
  } = options;

  const mockAuthContext = authContext ? createMockAuthContext(authContext) : createMockAuthContext();
  const mockPriceContext = priceContext ? createMockPriceContext(priceContext) : createMockPriceContext();
  const mockPortfolioContext = portfolioContext ? createMockPortfolioContext(portfolioContext) : createMockPortfolioContext();

  return render(ui, {
    wrapper: ({ children }) => (
      <AllTheProviders
        authContext={mockAuthContext}
        priceContext={mockPriceContext}
        portfolioContext={mockPortfolioContext}
        includeNavigation={includeNavigation}
      >
        {children}
      </AllTheProviders>
    ),
    ...renderOptions,
  });
};

// Utility functions for common test scenarios
export const renderWithAuth = (
  ui: ReactElement,
  authContextOverrides: Partial<MockAuthContextValue> = {}
) => {
  return customRender(ui, {
    authContext: authContextOverrides,
  });
};

export const renderWithPrice = (
  ui: ReactElement,
  priceContextOverrides: Partial<MockPriceContextValue> = {}
) => {
  return customRender(ui, {
    priceContext: priceContextOverrides,
  });
};

export const renderWithPortfolio = (
  ui: ReactElement,
  portfolioContextOverrides: Partial<MockPortfolioContextValue> = {}
) => {
  return customRender(ui, {
    portfolioContext: portfolioContextOverrides,
  });
};

export const renderWithNavigation = (ui: ReactElement) => {
  return customRender(ui, {
    includeNavigation: true,
  });
};

// Test data generators
export const createTestSymbol = (overrides: any = {}) => ({
  id: '1',
  symbol: 'BTC-USD',
  displayName: 'Bitcoin',
  assetClass: 'CRYPTO' as const,
  quoteCurrency: 'USD',
  baseCurrency: 'BTC',
  marketId: 'CRYPTO',
  ...overrides,
});

export const createTestMarketData = (overrides: any = {}) => ({
  symbol: 'BTC-USD',
  price: 50000,
  change: 2500,
  changePercent: 5.2,
  volume: 1000000,
  marketCap: 900000000,
  lastUpdate: '2023-12-24T10:00:00Z',
  ...overrides,
});

export const createTestPortfolio = (overrides: any = {}) => ({
  id: '1',
  name: 'Test Portfolio',
  totalValue: 25000,
  dailyPnL: 1250,
  totalPnLPercent: 8.5,
  baseCurrency: 'USD',
  positions: [],
  ...overrides,
});

export const createTestLeaderboardEntry = (overrides: any = {}) => ({
  userId: '1',
  username: 'trader1',
  displayName: 'Trader One',
  rank: 1,
  totalReturn: 15.5,
  portfolioValue: 50000,
  returnPercent: 15.5,
  winRate: 75.2,
  totalTrades: 42,
  tier: 'GOLD',
  badges: [],
  ...overrides,
});

// Mock API response generators
export const createMockApiResponse = <T>(data: T, options: { delay?: number; shouldFail?: boolean } = {}) => {
  const { delay = 0, shouldFail = false } = options;

  return new Promise<T>((resolve, reject) => {
    setTimeout(() => {
      if (shouldFail) {
        reject(new Error('Mock API error'));
      } else {
        resolve(data);
      }
    }, delay);
  });
};

// Test assertion helpers
export const expectToHaveBeenCalledWithPartialMatch = (
  mockFn: jest.MockedFunction<any>,
  expectedPartialArgs: any[]
) => {
  const calls = mockFn.mock.calls;
  const matchingCall = calls.find(call => {
    return expectedPartialArgs.every((expectedArg, index) => {
      if (typeof expectedArg === 'object' && expectedArg !== null) {
        return expect.objectContaining(expectedArg);
      }
      return call[index] === expectedArg;
    });
  });

  expect(matchingCall).toBeDefined();
};

// Performance testing utilities
export const measureRenderTime = async (renderFn: () => void) => {
  const startTime = performance.now();
  renderFn();
  await new Promise(resolve => setTimeout(resolve, 0)); // Wait for render
  const endTime = performance.now();
  return endTime - startTime;
};

// Network simulation utilities
export const mockNetworkDelay = (delay: number = 100) => {
  return new Promise(resolve => setTimeout(resolve, delay));
};

export const mockNetworkError = (errorMessage: string = 'Network error') => {
  throw new Error(errorMessage);
};

// Cleanup utilities
export const cleanupMocks = () => {
  jest.clearAllMocks();
  jest.clearAllTimers();
};

// Re-export everything from testing-library
export * from '@testing-library/react-native';

// Override render method
export { customRender as render };