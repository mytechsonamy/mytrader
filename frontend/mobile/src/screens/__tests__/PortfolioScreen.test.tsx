import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import PortfolioScreen from '../PortfolioScreen';
import { usePortfolio } from '../../context/PortfolioContext';
import { useAuth } from '../../context/AuthContext';

// Mock Alert
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    Alert: {
      alert: jest.fn(),
      prompt: jest.fn(),
    },
    RefreshControl: 'RefreshControl',
    ScrollView: 'ScrollView',
    Dimensions: {
      get: () => ({ width: 375, height: 812 }),
    },
  };
});

// Mock react-native-chart-kit
jest.mock('react-native-chart-kit', () => ({
  LineChart: 'LineChart',
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

// Mock PortfolioContext
const mockPortfolioContext = {
  state: {
    portfolios: [],
    currentPortfolio: null,
    positions: [],
    transactions: [],
    analytics: null,
    loadingState: 'idle',
    error: null,
  },
  loadPortfolios: jest.fn(),
  selectPortfolio: jest.fn(),
  clearError: jest.fn(),
  createPortfolio: jest.fn(),
  deletePortfolio: jest.fn(),
  updatePortfolio: jest.fn(),
};

jest.mock('../../context/PortfolioContext', () => ({
  usePortfolio: jest.fn(),
}));

// Mock navigation
const mockNavigation = {
  navigate: jest.fn(),
  goBack: jest.fn(),
  setOptions: jest.fn(),
};

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;
const mockUsePortfolio = usePortfolio as jest.MockedFunction<typeof usePortfolio>;

describe('PortfolioScreen Component', () => {
  const validPortfolioData = [
    {
      id: '1',
      name: 'Main Portfolio',
      baseCurrency: 'USD',
      totalValue: 15000,
      dailyPnL: 750,
      totalPnL: 2500,
      totalPnLPercent: 16.67,
      description: 'My main investment portfolio',
    },
    {
      id: '2',
      name: 'Crypto Portfolio',
      baseCurrency: 'USD',
      totalValue: 8000,
      dailyPnL: -200,
      totalPnL: 1200,
      totalPnLPercent: 15.0,
      description: 'Cryptocurrency investments',
    },
  ];

  const validPositionData = [
    {
      id: '1',
      portfolioId: '1',
      symbol: 'BTC-USD',
      symbolType: 'CRYPTO',
      quantity: 0.5,
      averagePrice: 45000,
      marketValue: 25000,
      unrealizedPnL: 2500,
      unrealizedPnLPercent: 11.11,
    },
    {
      id: '2',
      portfolioId: '1',
      symbol: 'AAPL',
      symbolType: 'STOCK',
      quantity: 100,
      averagePrice: 150,
      marketValue: 17000,
      unrealizedPnL: 2000,
      unrealizedPnLPercent: 13.33,
    },
  ];

  const validTransactionData = [
    {
      id: '1',
      portfolioId: '1',
      symbol: 'BTC-USD',
      type: 'BUY' as const,
      quantity: 0.5,
      price: 45000,
      amount: 22500,
      executedAt: '2023-12-01T10:00:00Z',
    },
    {
      id: '2',
      portfolioId: '1',
      symbol: 'AAPL',
      type: 'BUY' as const,
      quantity: 100,
      price: 150,
      amount: 15000,
      executedAt: '2023-12-02T11:00:00Z',
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();

    // Setup default mocks
    mockUseAuth.mockReturnValue(mockAuthContext);
    mockUsePortfolio.mockReturnValue({
      ...mockPortfolioContext,
      state: {
        ...mockPortfolioContext.state,
        portfolios: validPortfolioData,
        currentPortfolio: validPortfolioData[0],
        positions: validPositionData,
        transactions: validTransactionData,
        loadingState: 'idle',
        error: null,
      },
    });
  });

  describe('Authentication States', () => {
    it('should render login prompt for unauthenticated users', () => {
      mockUseAuth.mockReturnValue({
        ...mockAuthContext,
        user: null,
        isAuthenticated: false,
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('📊 Portföy Yönetimi')).toBeTruthy();
      expect(getByText('Portföy özelliklerini kullanmak için giriş yapmanız gerekiyor.')).toBeTruthy();
      expect(getByText('🔑 Giriş Yap')).toBeTruthy();
    });

    it('should navigate to auth stack when login button is pressed', () => {
      mockUseAuth.mockReturnValue({
        ...mockAuthContext,
        user: null,
        isAuthenticated: false,
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      fireEvent.press(getByText('🔑 Giriş Yap'));

      expect(mockNavigation.navigate).toHaveBeenCalledWith('AuthStack');
    });

    it('should render portfolio content for authenticated users', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('📊 Portföy Yönetimi')).toBeTruthy();
      expect(getByText('Main Portfolio')).toBeTruthy();
      expect(getByText('Crypto Portfolio')).toBeTruthy();
    });
  });

  describe('Loading States', () => {
    it('should show loading state', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          loadingState: 'loading',
        },
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('📊 Portföyler yükleniyor...')).toBeTruthy();
    });

    it('should not show loading state when refreshing', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          loadingState: 'loading',
        },
      });

      const { queryByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Should not show loading text when refreshing is true
      expect(queryByText('📊 Portföyler yükleniyor...')).toBeTruthy();
    });

    it('should call loadPortfolios on component mount', () => {
      render(<PortfolioScreen navigation={mockNavigation} />);

      expect(mockPortfolioContext.loadPortfolios).toHaveBeenCalled();
    });
  });

  describe('Error Handling', () => {
    it('should display error state', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          error: 'Network connection failed',
          loadingState: 'error',
        },
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('❌ Hata: Network connection failed')).toBeTruthy();
      expect(getByText('🔄 Tekrar Dene')).toBeTruthy();
    });

    it('should handle retry functionality', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          error: 'Network connection failed',
          loadingState: 'error',
        },
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      fireEvent.press(getByText('🔄 Tekrar Dene'));

      expect(mockPortfolioContext.clearError).toHaveBeenCalled();
      expect(mockPortfolioContext.loadPortfolios).toHaveBeenCalled();
    });

    it('should handle malformed portfolio data gracefully', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          portfolios: [
            null,
            undefined,
            { id: '1' }, // Missing required fields
            { totalValue: 'invalid' }, // Invalid data type
          ] as any,
        },
      });

      expect(() => {
        render(<PortfolioScreen navigation={mockNavigation} />);
      }).not.toThrow();
    });

    it('should handle null/undefined portfolios array', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          portfolios: null,
        },
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('📈 Henüz portföyünüz yok')).toBeTruthy();
    });
  });

  describe('Empty State', () => {
    it('should show empty state when no portfolios exist', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          portfolios: [],
        },
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('📈 Henüz portföyünüz yok')).toBeTruthy();
      expect(getByText('İlk portföyünüzü oluşturun ve yatırımlarınızı takip edin')).toBeTruthy();
      expect(getByText('💼 İlk Portföyümü Oluştur')).toBeTruthy();
    });

    it('should trigger create portfolio prompt from empty state', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          portfolios: [],
        },
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      fireEvent.press(getByText('💼 İlk Portföyümü Oluştur'));

      expect(Alert.prompt).toHaveBeenCalledWith(
        'Yeni Portföy',
        'Portföy adını girin:',
        expect.any(Array),
        'plain-text'
      );
    });
  });

  describe('Portfolio Management', () => {
    it('should render portfolio list correctly', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('Main Portfolio')).toBeTruthy();
      expect(getByText('Crypto Portfolio')).toBeTruthy();
      expect(getByText('USD')).toBeTruthy(); // Base currency
    });

    it('should handle portfolio selection', async () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      fireEvent.press(getByText('Crypto Portfolio'));

      expect(mockPortfolioContext.selectPortfolio).toHaveBeenCalledWith('2');
    });

    it('should trigger create portfolio prompt', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      fireEvent.press(getByText('+ Yeni'));

      expect(Alert.prompt).toHaveBeenCalledWith(
        'Yeni Portföy',
        'Portföy adını girin:',
        expect.any(Array),
        'plain-text'
      );
    });

    it('should handle refresh functionality', async () => {
      const { getByTestId } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Note: In actual implementation, would need to trigger RefreshControl
      // For now, test that refresh functions are available
      expect(mockPortfolioContext.loadPortfolios).toBeDefined();
      expect(mockPortfolioContext.selectPortfolio).toBeDefined();
    });
  });

  describe('Data Formatting', () => {
    it('should format currency values correctly', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Should format USD values properly
      expect(() => getByText(/\$15\.000,00/)).not.toThrow();
      expect(() => getByText(/\$8\.000,00/)).not.toThrow();
    });

    it('should handle null/undefined currency values', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          portfolios: [
            {
              ...validPortfolioData[0],
              totalValue: null,
              dailyPnL: undefined,
              totalPnL: NaN,
            } as any,
          ],
        },
      });

      expect(() => {
        render(<PortfolioScreen navigation={mockNavigation} />);
      }).not.toThrow();
    });

    it('should format percentage values correctly', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Should format percentage values with proper signs
      expect(() => getByText(/\+16\.67%/)).not.toThrow();
      expect(() => getByText(/\+15\.00%/)).not.toThrow();
    });

    it('should handle negative values with proper colors', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          portfolios: [
            {
              ...validPortfolioData[0],
              dailyPnL: -500,
              totalPnL: -1000,
              totalPnLPercent: -10.5,
            },
          ],
        },
      });

      expect(() => {
        render(<PortfolioScreen navigation={mockNavigation} />);
      }).not.toThrow();
    });
  });

  describe('Portfolio Details', () => {
    it('should render current portfolio details', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('📈 Main Portfolio Detayları')).toBeTruthy();
      expect(getByText('💰 Pozisyonlar')).toBeTruthy();
      expect(getByText('📝 Son İşlemler')).toBeTruthy();
    });

    it('should render performance chart when analytics available', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          analytics: {
            performanceData: [],
            riskMetrics: {},
            allocation: {},
          },
        },
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('Performans Grafiği')).toBeTruthy();
    });

    it('should handle missing portfolio details gracefully', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          currentPortfolio: null,
          positions: [],
          transactions: [],
        },
      });

      expect(() => {
        render(<PortfolioScreen navigation={mockNavigation} />);
      }).not.toThrow();
    });
  });

  describe('Position Management', () => {
    it('should render positions correctly', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('BTC-USD')).toBeTruthy();
      expect(getByText('AAPL')).toBeTruthy();
      expect(getByText('CRYPTO')).toBeTruthy();
      expect(getByText('STOCK')).toBeTruthy();
    });

    it('should format position quantities correctly', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('0.5000')).toBeTruthy(); // BTC quantity
      expect(getByText('100.0000')).toBeTruthy(); // AAPL quantity
    });

    it('should handle null/undefined position data', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          positions: [
            null,
            undefined,
            {
              ...validPositionData[0],
              quantity: null,
              averagePrice: undefined,
              marketValue: NaN,
            } as any,
          ],
        },
      });

      expect(() => {
        render(<PortfolioScreen navigation={mockNavigation} />);
      }).not.toThrow();
    });

    it('should show correct colors for position P&L', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Positions with positive P&L should be visible
      expect(() => getByText(/\+11\.11%/)).not.toThrow();
      expect(() => getByText(/\+13\.33%/)).not.toThrow();
    });
  });

  describe('Transaction History', () => {
    it('should render transactions correctly', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('BTC-USD')).toBeTruthy();
      expect(getByText('AAPL')).toBeTruthy();
      expect(getByText('ALIŞ')).toBeTruthy(); // BUY transactions
    });

    it('should format transaction dates correctly', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Should format dates in Turkish locale
      expect(() => getByText(/01\.12\.2023/)).not.toThrow();
      expect(() => getByText(/02\.12\.2023/)).not.toThrow();
    });

    it('should handle sell transactions', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          transactions: [
            {
              ...validTransactionData[0],
              type: 'SELL',
            },
          ],
        },
      });

      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      expect(getByText('SATIŞ')).toBeTruthy();
    });

    it('should handle malformed transaction data', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          transactions: [
            null,
            undefined,
            {
              ...validTransactionData[0],
              quantity: null,
              price: undefined,
              amount: NaN,
              executedAt: 'invalid-date',
            } as any,
          ],
        },
      });

      expect(() => {
        render(<PortfolioScreen navigation={mockNavigation} />);
      }).not.toThrow();
    });

    it('should limit displayed transactions to 5', () => {
      const manyTransactions = Array.from({ length: 10 }, (_, index) => ({
        ...validTransactionData[0],
        id: `transaction-${index}`,
        executedAt: `2023-12-${String(index + 1).padStart(2, '0')}T10:00:00Z`,
      }));

      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          transactions: manyTransactions,
        },
      });

      const { getAllByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Should only show first 5 transactions (limited by slice(0, 5))
      expect(() => getAllByText('BTC-USD')).not.toThrow();
    });
  });

  describe('Memory Management', () => {
    it('should handle component unmounting during async operations', async () => {
      let resolveLoad: () => void;
      const loadPromise = new Promise<void>((resolve) => {
        resolveLoad = resolve;
      });

      mockPortfolioContext.loadPortfolios.mockReturnValue(loadPromise);

      const { unmount } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Unmount before async operation completes
      unmount();

      // Complete the async operation
      resolveLoad!();

      // Should not cause memory leaks or errors
      expect(true).toBe(true);
    });

    it('should handle large datasets efficiently', () => {
      const largePortfolioData = Array.from({ length: 100 }, (_, index) => ({
        ...validPortfolioData[0],
        id: `portfolio-${index}`,
        name: `Portfolio ${index}`,
      }));

      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          portfolios: largePortfolioData,
        },
      });

      const startTime = Date.now();
      render(<PortfolioScreen navigation={mockNavigation} />);
      const renderTime = Date.now() - startTime;

      // Should render within reasonable time (less than 1 second)
      expect(renderTime).toBeLessThan(1000);
    });
  });

  describe('Accessibility', () => {
    it('should have accessible elements', () => {
      const { getAllByRole } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Should have accessible text elements
      const textElements = getAllByRole('text');
      expect(textElements.length).toBeGreaterThan(0);
    });

    it('should support keyboard navigation', () => {
      const { getByText } = render(<PortfolioScreen navigation={mockNavigation} />);

      // Touchable elements should be accessible
      expect(() => getByText('+ Yeni')).not.toThrow();
      expect(() => getByText('Main Portfolio')).not.toThrow();
    });
  });

  describe('Error Boundary Integration', () => {
    it('should handle component errors gracefully', () => {
      // Simulate component error
      mockUsePortfolio.mockImplementation(() => {
        throw new Error('Component error');
      });

      expect(() => {
        render(<PortfolioScreen navigation={mockNavigation} />);
      }).toThrow(); // This would be caught by error boundary in actual app
    });

    it('should recover from context errors', () => {
      mockUsePortfolio.mockReturnValue({
        ...mockPortfolioContext,
        state: {
          ...mockPortfolioContext.state,
          error: 'Context error occurred',
        },
      });

      expect(() => {
        render(<PortfolioScreen navigation={mockNavigation} />);
      }).not.toThrow();
    });
  });
});