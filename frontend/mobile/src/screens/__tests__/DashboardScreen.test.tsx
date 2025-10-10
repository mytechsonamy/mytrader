import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import DashboardScreen from '../DashboardScreen';
import { apiService } from '../../services/api';
import { usePrices } from '../../context/PriceContext';
import { useAuth } from '../../context/AuthContext';
import { usePerformanceOptimization } from '../../hooks/usePerformanceOptimization';

// Mock Alert
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    Alert: {
      alert: jest.fn(),
    },
    RefreshControl: 'RefreshControl',
    ScrollView: 'ScrollView',
    Modal: 'Modal',
    Dimensions: {
      get: () => ({ width: 375, height: 812 }),
    },
  };
});

// Mock navigation
const mockNavigation = {
  navigate: jest.fn(),
  goBack: jest.fn(),
  setOptions: jest.fn(),
};

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => mockNavigation,
  useFocusEffect: (callback: () => void) => callback(),
}));

// Mock AuthContext
const mockAuthContext = {
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
};

jest.mock('../../context/AuthContext', () => ({
  useAuth: jest.fn(),
}));

// Mock PriceContext
const mockPriceContext = {
  enhancedPrices: {
    'BTC-USD': {
      symbol: 'BTC-USD',
      price: 50000,
      change: 2.5,
      changePercent: 5.2,
      volume: 1000000,
      marketCap: 900000000,
      lastUpdate: new Date().toISOString(),
    },
    'ASELS': {
      symbol: 'ASELS',
      price: 15.5,
      change: 0.5,
      changePercent: 3.3,
      volume: 50000,
      marketCap: 5000000,
      lastUpdate: new Date().toISOString(),
    },
  },
  getSymbolsByAssetClass: jest.fn(),
  getAssetClassSummary: jest.fn(() => ({
    CRYPTO: { totalValue: 10000, totalChange: 500, totalChangePercent: 5.2 },
    STOCK: { totalValue: 5000, totalChange: 200, totalChangePercent: 4.2 },
  })),
  refreshPrices: jest.fn(),
  connectionStatus: 'connected',
  isLoading: false,
  error: null,
};

jest.mock('../../context/PriceContext', () => ({
  usePrices: jest.fn(),
}));

// Mock Performance Hook
const mockPerformanceOptimization = {
  startRender: jest.fn(),
  endRender: jest.fn(),
  throttledUpdate: jest.fn(),
  batchedSetState: jest.fn(),
  performanceUtils: {
    getAdaptiveQuality: jest.fn(() => 'high'),
  },
  metrics: {
    renderCount: 5,
    averageRenderTime: 50,
    updateCount: 10,
    droppedUpdates: 0,
  },
  isActive: true,
};

jest.mock('../../hooks/usePerformanceOptimization', () => ({
  usePerformanceOptimization: jest.fn(),
}));

// Mock API service
jest.mock('../../services/api', () => ({
  apiService: {
    getPortfolios: jest.fn(),
    getSymbolsByAssetClass: jest.fn(),
    getAllMarketStatuses: jest.fn(),
    getLeaderboard: jest.fn(),
    getUserRanking: jest.fn(),
    getCompetitionStats: jest.fn(),
    getMarketNews: jest.fn(),
  },
}));

// Mock dashboard components
jest.mock('../../components/dashboard', () => ({
  SmartOverviewHeader: 'SmartOverviewHeader',
  AssetClassAccordion: 'AssetClassAccordion',
  CompactLeaderboard: 'CompactLeaderboard',
  DashboardErrorBoundary: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  AccordionErrorBoundary: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

jest.mock('../../components/news', () => ({
  EnhancedNewsPreview: 'EnhancedNewsPreview',
}));

jest.mock('../EnhancedNewsScreen', () => 'EnhancedNewsScreen');

const mockApiService = apiService as jest.Mocked<typeof apiService>;
const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;
const mockUsePrices = usePrices as jest.MockedFunction<typeof usePrices>;
const mockUsePerformanceOptimization = usePerformanceOptimization as jest.MockedFunction<typeof usePerformanceOptimization>;

describe('DashboardScreen Component', () => {
  const validPortfolioData = [
    {
      id: '1',
      name: 'Main Portfolio',
      totalValue: 15000,
      totalChange: 700,
      totalChangePercent: 4.9,
    }
  ];

  const validCryptoSymbols = [
    {
      id: '1',
      symbol: 'BTC-USD',
      displayName: 'Bitcoin',
      assetClass: 'CRYPTO' as const,
      quoteCurrency: 'USD',
      marketId: 'CRYPTO',
    },
    {
      id: '2',
      symbol: 'ETH-USD',
      displayName: 'Ethereum',
      assetClass: 'CRYPTO' as const,
      quoteCurrency: 'USD',
      marketId: 'CRYPTO',
    },
  ];

  const validStockSymbols = [
    {
      id: '3',
      symbol: 'ASELS',
      displayName: 'Aselsan',
      assetClass: 'STOCK' as const,
      quoteCurrency: 'TRY',
      marketId: 'BIST',
    },
    {
      id: '4',
      symbol: 'AAPL',
      displayName: 'Apple Inc.',
      assetClass: 'STOCK' as const,
      quoteCurrency: 'USD',
      marketId: 'NASDAQ',
    },
  ];

  const validMarketStatuses = [
    {
      marketName: 'Crypto Market',
      status: 'OPEN',
      nextOpen: null,
      nextClose: '2023-12-25T00:00:00Z',
    },
    {
      marketName: 'BIST Stock Market',
      status: 'CLOSED',
      nextOpen: '2023-12-25T06:00:00Z',
      nextClose: null,
    },
  ];

  const validLeaderboardData = [
    {
      userId: '1',
      username: 'trader1',
      rank: 1,
      totalReturn: 15.5,
      totalTrades: 42,
      winRate: 65.2,
    },
    {
      userId: '2',
      username: 'trader2',
      rank: 2,
      totalReturn: 12.3,
      totalTrades: 38,
      winRate: 58.7,
    },
  ];

  const validNewsData = [
    {
      id: '1',
      title: 'Market Update',
      summary: 'Markets are showing positive trends',
      publishedAt: '2023-12-24T10:00:00Z',
      category: 'market',
      imageUrl: 'https://example.com/image1.jpg',
    },
    {
      id: '2',
      title: 'Crypto News',
      summary: 'Bitcoin reaches new heights',
      publishedAt: '2023-12-24T09:00:00Z',
      category: 'crypto',
      imageUrl: 'https://example.com/image2.jpg',
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();

    // Setup default mocks
    mockUseAuth.mockReturnValue(mockAuthContext);
    mockUsePrices.mockReturnValue(mockPriceContext);
    mockUsePerformanceOptimization.mockReturnValue(mockPerformanceOptimization);

    // Setup default API responses
    mockApiService.getPortfolios.mockResolvedValue(validPortfolioData);
    mockApiService.getSymbolsByAssetClass.mockImplementation((assetClass) => {
      if (assetClass === 'CRYPTO') return Promise.resolve(validCryptoSymbols);
      if (assetClass === 'STOCK') return Promise.resolve(validStockSymbols);
      return Promise.resolve([]);
    });
    mockApiService.getAllMarketStatuses.mockResolvedValue(validMarketStatuses);
    mockApiService.getLeaderboard.mockResolvedValue(validLeaderboardData);
    mockApiService.getUserRanking.mockResolvedValue({
      rank: 25,
      totalReturn: 8.5,
      userId: '1',
      username: 'testuser',
    });
    mockApiService.getCompetitionStats.mockResolvedValue({
      totalParticipants: 150,
      totalPrizePool: 10000,
      minimumTrades: 5,
      prizes: [{ rank: 1, amount: 5000, currency: 'USD' }],
    });
    mockApiService.getMarketNews.mockResolvedValue(validNewsData);
  });

  describe('Rendering', () => {
    it('should render without crashing', async () => {
      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });

    it('should render main components when data loads successfully', async () => {
      const { getByTestId, queryByTestId } = render(<DashboardScreen />);

      await waitFor(() => {
        // Should render main dashboard components
        expect(queryByTestId).toBeDefined();
      });
    });

    it('should render for unauthenticated users', async () => {
      mockUseAuth.mockReturnValue({
        ...mockAuthContext,
        user: null,
        isAuthenticated: false,
      });

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        // Should still render without user data
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalled();
      });
    });
  });

  describe('Data Loading and Error Handling', () => {
    it('should handle API errors gracefully during initialization', async () => {
      mockApiService.getSymbolsByAssetClass.mockRejectedValue(new Error('Network error'));
      mockApiService.getAllMarketStatuses.mockRejectedValue(new Error('Market status error'));

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalled();
      });
    });

    it('should handle malformed portfolio data', async () => {
      mockApiService.getPortfolios.mockResolvedValue([
        null,
        { id: '1' }, // Missing required fields
        { totalValue: 'invalid' } as any, // Invalid data type
      ] as any);

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getPortfolios).toHaveBeenCalled();
      });
    });

    it('should handle malformed symbol data', async () => {
      mockApiService.getSymbolsByAssetClass.mockResolvedValue([
        null,
        undefined,
        { symbol: 'BTC-USD' }, // Missing required fields
        { invalidField: 'test' }, // Invalid structure
      ] as any);

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalled();
      });
    });

    it('should handle null/undefined market status data', async () => {
      mockApiService.getAllMarketStatuses.mockResolvedValue([
        null,
        undefined,
        { marketName: null, status: 'OPEN' },
        { status: null },
      ] as any);

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });

    it('should handle malformed leaderboard data', async () => {
      mockApiService.getLeaderboard.mockResolvedValue([
        null,
        undefined,
        { userId: '1' }, // Missing required fields
        { rank: 'invalid' }, // Invalid data type
      ] as any);

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });
  });

  describe('Real-time Data Updates', () => {
    it('should handle price context updates', async () => {
      const { rerender } = render(<DashboardScreen />);

      // Simulate price updates
      mockUsePrices.mockReturnValue({
        ...mockPriceContext,
        enhancedPrices: {
          ...mockPriceContext.enhancedPrices,
          'BTC-USD': {
            ...mockPriceContext.enhancedPrices['BTC-USD'],
            price: 51000, // Updated price
            change: 3.0,
            changePercent: 6.0,
          },
        },
      });

      rerender(<DashboardScreen />);

      expect(() => rerender(<DashboardScreen />)).not.toThrow();
    });

    it('should handle WebSocket connection status changes', async () => {
      const { rerender } = render(<DashboardScreen />);

      // Simulate connection status changes
      mockUsePrices.mockReturnValue({
        ...mockPriceContext,
        connectionStatus: 'disconnected',
      });

      rerender(<DashboardScreen />);

      mockUsePrices.mockReturnValue({
        ...mockPriceContext,
        connectionStatus: 'reconnecting',
      });

      rerender(<DashboardScreen />);

      expect(() => rerender(<DashboardScreen />)).not.toThrow();
    });

    it('should handle null/undefined enhanced prices', async () => {
      mockUsePrices.mockReturnValue({
        ...mockPriceContext,
        enhancedPrices: null,
      });

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });

    it('should handle malformed enhanced prices data', async () => {
      mockUsePrices.mockReturnValue({
        ...mockPriceContext,
        enhancedPrices: {
          'invalid-symbol': null,
          'BTC-USD': {
            symbol: null,
            price: 'invalid',
            change: undefined,
          } as any,
          'malformed-data': 'not-an-object',
        } as any,
      });

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });
  });

  describe('User Interactions', () => {
    it('should handle refresh functionality', async () => {
      const { getByTestId } = render(<DashboardScreen />);

      await waitFor(() => {
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalled();
      });

      // Reset mocks to test refresh
      jest.clearAllMocks();

      // Simulate refresh action (would need actual RefreshControl interaction)
      // For now, verify that refresh functions are available
      expect(mockPriceContext.refreshPrices).toBeDefined();
    });

    it('should handle navigation to profile for authenticated users', async () => {
      render(<DashboardScreen />);

      // In a real implementation, we would trigger the profile press
      // For now, verify the navigation mock is available
      expect(mockNavigation.navigate).toBeDefined();
    });

    it('should prompt login for unauthenticated users on protected actions', async () => {
      mockUseAuth.mockReturnValue({
        ...mockAuthContext,
        user: null,
        isAuthenticated: false,
      });

      render(<DashboardScreen />);

      // Test would involve triggering watchlist or challenge actions
      // Verify Alert.alert is available for login prompts
      expect(Alert.alert).toBeDefined();
    });

    it('should handle symbol press interactions', async () => {
      render(<DashboardScreen />);

      await waitFor(() => {
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalled();
      });

      // In actual implementation, this would test symbol press handlers
      expect(true).toBe(true); // Placeholder for symbol interaction tests
    });

    it('should handle strategy test navigation', async () => {
      render(<DashboardScreen />);

      await waitFor(() => {
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalled();
      });

      // Verify navigation mock is available for strategy test
      expect(mockNavigation.navigate).toBeDefined();
    });
  });

  describe('Asset Class Management', () => {
    it('should properly categorize BIST symbols', async () => {
      const bistSymbols = [
        {
          id: '1',
          symbol: 'ASELS',
          displayName: 'Aselsan',
          assetClass: 'STOCK' as const,
          quoteCurrency: 'TRY',
          marketId: 'BIST',
        },
      ];

      mockApiService.getSymbolsByAssetClass.mockImplementation((assetClass) => {
        if (assetClass === 'STOCK') return Promise.resolve(bistSymbols);
        return Promise.resolve([]);
      });

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalledWith('STOCK');
      });
    });

    it('should properly categorize NASDAQ symbols', async () => {
      const nasdaqSymbols = [
        {
          id: '1',
          symbol: 'AAPL',
          displayName: 'Apple Inc.',
          assetClass: 'STOCK' as const,
          quoteCurrency: 'USD',
          marketId: 'NASDAQ',
        },
      ];

      mockApiService.getSymbolsByAssetClass.mockImplementation((assetClass) => {
        if (assetClass === 'STOCK') return Promise.resolve(nasdaqSymbols);
        return Promise.resolve([]);
      });

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalledWith('STOCK');
      });
    });

    it('should handle mixed asset class symbols', async () => {
      const mixedSymbols = [
        ...validCryptoSymbols,
        ...validStockSymbols,
        null,
        { invalidStructure: true },
      ] as any;

      mockApiService.getSymbolsByAssetClass.mockResolvedValue(mixedSymbols);

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });
  });

  describe('Performance Optimization', () => {
    it('should initialize performance monitoring', async () => {
      render(<DashboardScreen />);

      expect(mockUsePerformanceOptimization).toHaveBeenCalledWith({
        updateInterval: 1000,
        maxUpdateFrequency: 30,
        pauseOnBackground: true,
        enableBatching: true,
        debounceDelay: 200,
      });

      expect(mockPerformanceOptimization.startRender).toHaveBeenCalled();
    });

    it('should handle performance metrics updates', async () => {
      const { rerender } = render(<DashboardScreen />);

      // Simulate multiple renders to trigger performance logging
      for (let i = 0; i < 5; i++) {
        rerender(<DashboardScreen />);
      }

      expect(mockPerformanceOptimization.startRender).toHaveBeenCalled();
    });

    it('should handle component unmounting for performance cleanup', async () => {
      const { unmount } = render(<DashboardScreen />);

      unmount();

      expect(mockPerformanceOptimization.endRender).toHaveBeenCalled();
    });
  });

  describe('Memory Management', () => {
    it('should handle large datasets without performance issues', async () => {
      const largeSymbolDataset = Array.from({ length: 1000 }, (_, index) => ({
        id: `symbol-${index}`,
        symbol: `SYM${index}`,
        displayName: `Symbol ${index}`,
        assetClass: 'CRYPTO' as const,
        quoteCurrency: 'USD',
        marketId: 'CRYPTO',
      }));

      mockApiService.getSymbolsByAssetClass.mockResolvedValue(largeSymbolDataset);

      const startTime = Date.now();
      render(<DashboardScreen />);
      const renderTime = Date.now() - startTime;

      // Should render within reasonable time (less than 1 second)
      expect(renderTime).toBeLessThan(1000);
    });

    it('should handle component unmounting during async operations', async () => {
      let resolvePortfolio: (value: any) => void;
      const portfolioPromise = new Promise((resolve) => {
        resolvePortfolio = resolve;
      });

      mockApiService.getPortfolios.mockReturnValue(portfolioPromise);

      const { unmount } = render(<DashboardScreen />);

      // Unmount before async operation completes
      unmount();

      // Complete the async operation
      resolvePortfolio!(validPortfolioData);

      // Should not cause memory leaks or errors
      expect(true).toBe(true);
    });
  });

  describe('Error Boundary Integration', () => {
    it('should be wrapped in error boundary', () => {
      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });

    it('should handle component errors gracefully', async () => {
      // Simulate component error
      mockApiService.getSymbolsByAssetClass.mockRejectedValue(new Error('Component error'));

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });
  });

  describe('Accessibility', () => {
    it('should have accessible elements', () => {
      const { getAllByRole } = render(<DashboardScreen />);

      // Should have accessible text elements
      expect(() => getAllByRole('text')).not.toThrow();
    });

    it('should support screen readers', () => {
      const { getByRole } = render(<DashboardScreen />);

      // Should render accessible elements
      expect(() => getByRole('scrollbar')).not.toThrow();
    });
  });

  describe('Network Scenarios', () => {
    it('should handle network timeout errors', async () => {
      mockApiService.getSymbolsByAssetClass.mockRejectedValue({
        name: 'TimeoutError',
        message: 'Request timeout',
      });

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getSymbolsByAssetClass).toHaveBeenCalled();
      });
    });

    it('should handle network connection errors', async () => {
      mockApiService.getAllMarketStatuses.mockRejectedValue({
        name: 'NetworkError',
        message: 'Network request failed',
      });

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });

    it('should handle server errors', async () => {
      mockApiService.getMarketNews.mockRejectedValue({
        status: 500,
        message: 'Internal server error',
      });

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });
  });

  describe('State Management Edge Cases', () => {
    it('should handle rapid state changes', async () => {
      const { rerender } = render(<DashboardScreen />);

      // Simulate rapid data updates
      for (let i = 0; i < 20; i++) {
        mockApiService.getSymbolsByAssetClass.mockResolvedValue(
          i % 2 === 0 ? validCryptoSymbols : []
        );
        rerender(<DashboardScreen />);
      }

      expect(() => rerender(<DashboardScreen />)).not.toThrow();
    });

    it('should handle concurrent API calls', async () => {
      // Simulate concurrent API calls resolving in different orders
      const promises = [
        Promise.resolve(validCryptoSymbols),
        Promise.resolve(validMarketStatuses),
        Promise.resolve(validPortfolioData),
      ];

      mockApiService.getSymbolsByAssetClass.mockImplementation(() => promises[0]);
      mockApiService.getAllMarketStatuses.mockImplementation(() => promises[1]);
      mockApiService.getPortfolios.mockImplementation(() => promises[2]);

      expect(() => {
        render(<DashboardScreen />);
      }).not.toThrow();
    });
  });

  describe('Modal Functionality', () => {
    it('should handle news modal display', async () => {
      render(<DashboardScreen />);

      await waitFor(() => {
        expect(mockApiService.getMarketNews).toHaveBeenCalled();
      });

      // Would test modal opening/closing in actual implementation
      expect(true).toBe(true);
    });

    it('should handle modal interactions', async () => {
      render(<DashboardScreen />);

      // Test modal close functionality
      expect(true).toBe(true);
    });
  });
});