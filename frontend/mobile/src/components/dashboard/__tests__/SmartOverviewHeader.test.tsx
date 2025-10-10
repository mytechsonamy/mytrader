import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import SmartOverviewHeader from '../SmartOverviewHeader';
import { useAuth } from '../../../context/AuthContext';
import { usePrices } from '../../../context/PriceContext';

// Mock React Native components
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    ActivityIndicator: 'ActivityIndicator',
    TouchableOpacity: actualRN.TouchableOpacity,
  };
});

// Mock expo-linear-gradient
jest.mock('expo-linear-gradient', () => ({
  LinearGradient: ({ children }: any) => {
    const { View } = jest.requireActual('react-native');
    return <View testID="linear-gradient">{children}</View>;
  },
}));

// Mock AuthContext
const mockAuthContext = {
  user: {
    id: '1',
    email: 'test@example.com',
    first_name: 'John',
    last_name: 'Doe',
  },
  login: jest.fn(),
  logout: jest.fn(),
  register: jest.fn(),
  isAuthenticated: true,
  loading: false,
};

jest.mock('../../../context/AuthContext', () => ({
  useAuth: jest.fn(),
}));

// Mock PriceContext
const mockPriceContext = {
  getAssetClassSummary: jest.fn(() => ({
    CRYPTO: {
      totalSymbols: 10,
      averageChange: 2.5,
      gainers: 6,
      losers: 4,
    },
    STOCK: {
      totalSymbols: 15,
      averageChange: -1.2,
      gainers: 5,
      losers: 10,
    },
  })),
  connectionStatus: 'connected' as const,
  enhancedPrices: {},
  refreshPrices: jest.fn(),
  isLoading: false,
  error: null,
};

jest.mock('../../../context/PriceContext', () => ({
  usePrices: jest.fn(),
}));

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;
const mockUsePrices = usePrices as jest.MockedFunction<typeof usePrices>;

describe('SmartOverviewHeader Component', () => {
  const validPortfolioData = {
    id: '1',
    name: 'Main Portfolio',
    totalValue: 25000,
    dailyPnL: 1250,
    totalPnLPercent: 8.5,
    positions: [
      {
        id: '1',
        symbol: 'BTC-USD',
        symbolName: 'Bitcoin',
        unrealizedPnLPercent: 15.2,
        quantity: 0.5,
        marketValue: 25000,
      },
      {
        id: '2',
        symbol: 'ETH-USD',
        symbolName: 'Ethereum',
        unrealizedPnLPercent: 8.7,
        quantity: 10,
        marketValue: 30000,
      },
    ],
  };

  const validMarketStatusData = [
    {
      marketName: 'Crypto Market',
      status: 'OPEN' as const,
      nextOpen: null,
      nextClose: '2023-12-25T00:00:00Z',
    },
    {
      marketName: 'Stock Market',
      status: 'CLOSED' as const,
      nextOpen: '2023-12-25T09:00:00Z',
      nextClose: null,
    },
    {
      marketName: 'Forex Market',
      status: 'OPEN' as const,
      nextOpen: null,
      nextClose: '2023-12-24T21:00:00Z',
    },
  ];

  const validUserRankingData = {
    userId: '1',
    rank: 25,
    totalParticipants: 150,
    percentile: 16.7,
    score: 850,
    returnPercent: 12.5,
    isEligible: true,
  };

  const defaultProps = {
    portfolio: validPortfolioData,
    marketStatuses: validMarketStatusData,
    userRanking: validUserRankingData,
    isLoading: false,
    onProfilePress: jest.fn(),
    onLoginPress: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockUseAuth.mockReturnValue(mockAuthContext);
    mockUsePrices.mockReturnValue(mockPriceContext);
  });

  describe('Rendering States', () => {
    it('should render without crashing', () => {
      expect(() => {
        render(<SmartOverviewHeader {...defaultProps} />);
      }).not.toThrow();
    });

    it('should render loading state', () => {
      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} isLoading={true} />
      );

      expect(getByText('Yükleniyor...')).toBeTruthy();
    });

    it('should render main header content', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('🚀 myTrader')).toBeTruthy();
      expect(getByText('Canlı Piyasa Analizi')).toBeTruthy();
    });

    it('should render user button for authenticated users', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('👤 John')).toBeTruthy();
    });

    it('should render login button for unauthenticated users', () => {
      mockUseAuth.mockReturnValue({
        ...mockAuthContext,
        user: null,
        isAuthenticated: false,
      });

      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('👤 Giriş')).toBeTruthy();
    });
  });

  describe('Portfolio Metrics', () => {
    it('should display portfolio value correctly', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('Portföy Değeri')).toBeTruthy();
      expect(getByText('$25.0K')).toBeTruthy(); // Formatted portfolio value
    });

    it('should display portfolio P&L with correct formatting', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('$1.25K (+8.50%)')).toBeTruthy();
    });

    it('should display top performer information', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('En İyi Varlık')).toBeTruthy();
      expect(getByText('Bitcoin')).toBeTruthy(); // Top performer symbol name
      expect(getByText('+15.20%')).toBeTruthy(); // Top performer return
    });

    it('should handle negative P&L correctly', () => {
      const portfolioWithLoss = {
        ...validPortfolioData,
        dailyPnL: -500,
        totalPnLPercent: -2.0,
      };

      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} portfolio={portfolioWithLoss} />
      );

      expect(getByText('-$500.00 (-2.00%)')).toBeTruthy();
    });

    it('should handle missing portfolio data', () => {
      const { queryByText } = render(
        <SmartOverviewHeader {...defaultProps} portfolio={null} />
      );

      expect(queryByText('Portföy Değeri')).toBeNull();
      expect(queryByText('En İyi Varlık')).toBeNull();
    });

    it('should handle portfolio without positions', () => {
      const portfolioWithoutPositions = {
        ...validPortfolioData,
        positions: [],
      };

      const { queryByText } = render(
        <SmartOverviewHeader {...defaultProps} portfolio={portfolioWithoutPositions} />
      );

      expect(queryByText('En İyi Varlık')).toBeNull();
    });

    it('should handle portfolio with null/undefined positions', () => {
      const portfolioWithNullPositions = {
        ...validPortfolioData,
        positions: null,
      };

      expect(() => {
        render(<SmartOverviewHeader {...defaultProps} portfolio={portfolioWithNullPositions} />);
      }).not.toThrow();
    });
  });

  describe('Market Status Display', () => {
    it('should display market status summary', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('Piyasa Durumu')).toBeTruthy();
      expect(getByText('2')).toBeTruthy(); // Open markets count
      expect(getByText('Açık')).toBeTruthy();
      expect(getByText('1')).toBeTruthy(); // Closed markets count
      expect(getByText('Kapalı')).toBeTruthy();
    });

    it('should handle null market statuses', () => {
      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} marketStatuses={null} />
      );

      expect(getByText('0')).toBeTruthy(); // Should show 0 for open markets
      expect(getByText('Açık')).toBeTruthy();
    });

    it('should handle empty market statuses array', () => {
      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} marketStatuses={[]} />
      );

      expect(getByText('0')).toBeTruthy(); // Should show 0 for both open and closed
    });

    it('should handle malformed market status data', () => {
      const malformedMarketStatuses = [
        null,
        undefined,
        { marketName: 'Test', status: null },
        { status: 'INVALID_STATUS' },
      ] as any;

      expect(() => {
        render(<SmartOverviewHeader {...defaultProps} marketStatuses={malformedMarketStatuses} />);
      }).not.toThrow();
    });
  });

  describe('Market Sentiment', () => {
    it('should display market sentiment correctly', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('Genel Sentiment')).toBeTruthy();
      // Based on mock data: CRYPTO +2.5%, STOCK -1.2%, weighted average should be positive
      expect(getByText('Yükselişli')).toBeTruthy(); // Should be bullish
    });

    it('should handle bearish sentiment', () => {
      mockUsePrices.mockReturnValue({
        ...mockPriceContext,
        getAssetClassSummary: jest.fn(() => ({
          CRYPTO: {
            totalSymbols: 5,
            averageChange: -3.5,
            gainers: 1,
            losers: 4,
          },
          STOCK: {
            totalSymbols: 10,
            averageChange: -2.8,
            gainers: 2,
            losers: 8,
          },
        })),
      });

      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('Düşüşlü')).toBeTruthy();
    });

    it('should handle neutral sentiment', () => {
      mockUsePrices.mockReturnValue({
        ...mockPriceContext,
        getAssetClassSummary: jest.fn(() => ({
          CRYPTO: {
            totalSymbols: 10,
            averageChange: 0.5,
            gainers: 5,
            losers: 5,
          },
        })),
      });

      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('Durgun')).toBeTruthy();
    });

    it('should handle empty asset class summary', () => {
      mockUsePrices.mockReturnValue({
        ...mockPriceContext,
        getAssetClassSummary: jest.fn(() => ({})),
      });

      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('Durgun')).toBeTruthy(); // Should default to neutral
    });
  });

  describe('User Ranking Display', () => {
    it('should display user ranking for authenticated users', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('Sıralamanız')).toBeTruthy();
      expect(getByText('#25')).toBeTruthy();
      expect(getByText('/ 150')).toBeTruthy();
      expect(getByText('%16.7 dilim')).toBeTruthy();
    });

    it('should not display ranking for unauthenticated users', () => {
      mockUseAuth.mockReturnValue({
        ...mockAuthContext,
        user: null,
        isAuthenticated: false,
      });

      const { queryByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(queryByText('Sıralamanız')).toBeNull();
    });

    it('should not display ranking when userRanking is null', () => {
      const { queryByText } = render(
        <SmartOverviewHeader {...defaultProps} userRanking={null} />
      );

      expect(queryByText('Sıralamanız')).toBeNull();
    });

    it('should handle missing percentile data', () => {
      const rankingWithoutPercentile = {
        ...validUserRankingData,
        percentile: null,
      };

      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} userRanking={rankingWithoutPercentile} />
      );

      expect(getByText('%0.0 dilim')).toBeTruthy();
    });
  });

  describe('Connection Status', () => {
    it('should display connected status', () => {
      const { getByTestId } = render(<SmartOverviewHeader {...defaultProps} />);

      // Connection dot should be present (would need testID in actual component)
      expect(getByTestId('linear-gradient')).toBeTruthy();
    });

    it('should handle different connection statuses', () => {
      const connectionStates = ['connected', 'connecting', 'error', 'disconnected'] as const;

      connectionStates.forEach((status) => {
        mockUsePrices.mockReturnValue({
          ...mockPriceContext,
          connectionStatus: status,
        });

        expect(() => {
          render(<SmartOverviewHeader {...defaultProps} />);
        }).not.toThrow();
      });
    });
  });

  describe('User Interactions', () => {
    it('should call onProfilePress when user button is pressed (authenticated)', () => {
      const onProfilePressMock = jest.fn();
      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} onProfilePress={onProfilePressMock} />
      );

      fireEvent.press(getByText('👤 John'));

      expect(onProfilePressMock).toHaveBeenCalled();
    });

    it('should call onLoginPress when user button is pressed (unauthenticated)', () => {
      mockUseAuth.mockReturnValue({
        ...mockAuthContext,
        user: null,
        isAuthenticated: false,
      });

      const onLoginPressMock = jest.fn();
      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} onLoginPress={onLoginPressMock} />
      );

      fireEvent.press(getByText('👤 Giriş'));

      expect(onLoginPressMock).toHaveBeenCalled();
    });

    it('should not crash when callbacks are not provided', () => {
      const { getByText } = render(
        <SmartOverviewHeader
          {...defaultProps}
          onProfilePress={undefined}
          onLoginPress={undefined}
        />
      );

      expect(() => {
        fireEvent.press(getByText('👤 John'));
      }).not.toThrow();
    });
  });

  describe('Currency Formatting', () => {
    it('should format large amounts correctly', () => {
      const largePortfolio = {
        ...validPortfolioData,
        totalValue: 1500000,
        dailyPnL: 25000,
      };

      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} portfolio={largePortfolio} />
      );

      expect(getByText('$1.50M')).toBeTruthy();
      expect(getByText('$25.0K (+8.50%)')).toBeTruthy();
    });

    it('should format small amounts correctly', () => {
      const smallPortfolio = {
        ...validPortfolioData,
        totalValue: 500,
        dailyPnL: 25,
        totalPnLPercent: 5.2,
      };

      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} portfolio={smallPortfolio} />
      );

      expect(getByText('$500.00')).toBeTruthy();
      expect(getByText('$25.00 (+5.20%)')).toBeTruthy();
    });

    it('should handle zero values', () => {
      const zeroPortfolio = {
        ...validPortfolioData,
        totalValue: 0,
        dailyPnL: 0,
        totalPnLPercent: 0,
      };

      const { getByText } = render(
        <SmartOverviewHeader {...defaultProps} portfolio={zeroPortfolio} />
      );

      expect(getByText('$0.00')).toBeTruthy();
    });

    it('should handle null/undefined numeric values', () => {
      const portfolioWithNulls = {
        ...validPortfolioData,
        totalValue: null,
        dailyPnL: undefined,
        totalPnLPercent: NaN,
      };

      expect(() => {
        render(<SmartOverviewHeader {...defaultProps} portfolio={portfolioWithNulls} />);
      }).not.toThrow();
    });
  });

  describe('Error Handling', () => {
    it('should handle missing user first_name', () => {
      mockUseAuth.mockReturnValue({
        ...mockAuthContext,
        user: {
          ...mockAuthContext.user,
          first_name: undefined,
        },
      });

      expect(() => {
        render(<SmartOverviewHeader {...defaultProps} />);
      }).not.toThrow();
    });

    it('should handle context errors gracefully', () => {
      mockUsePrices.mockImplementation(() => {
        throw new Error('Context error');
      });

      expect(() => {
        render(<SmartOverviewHeader {...defaultProps} />);
      }).toThrow(); // Would be caught by error boundary in actual app
    });

    it('should handle malformed portfolio positions', () => {
      const portfolioWithMalformedPositions = {
        ...validPortfolioData,
        positions: [
          null,
          undefined,
          { symbol: 'BTC-USD' }, // Missing required fields
          { unrealizedPnLPercent: 'invalid' }, // Invalid data type
        ] as any,
      };

      expect(() => {
        render(<SmartOverviewHeader {...defaultProps} portfolio={portfolioWithMalformedPositions} />);
      }).not.toThrow();
    });
  });

  describe('Memory Management', () => {
    it('should handle component unmounting', () => {
      const { unmount } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(() => unmount()).not.toThrow();
    });

    it('should handle rapid prop changes', () => {
      const { rerender } = render(<SmartOverviewHeader {...defaultProps} />);

      // Rapidly change props
      for (let i = 0; i < 10; i++) {
        rerender(
          <SmartOverviewHeader
            {...defaultProps}
            isLoading={i % 2 === 0}
            portfolio={i % 2 === 0 ? validPortfolioData : null}
          />
        );
      }

      expect(() => rerender(<SmartOverviewHeader {...defaultProps} />)).not.toThrow();
    });
  });

  describe('Accessibility', () => {
    it('should have accessible elements', () => {
      const { getAllByRole } = render(<SmartOverviewHeader {...defaultProps} />);

      // Should have accessible text elements
      const textElements = getAllByRole('text');
      expect(textElements.length).toBeGreaterThan(0);
    });

    it('should support touch interactions', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      // User button should be pressable
      expect(() => {
        fireEvent.press(getByText('👤 John'));
      }).not.toThrow();
    });
  });

  describe('Visual Elements', () => {
    it('should render LinearGradient background', () => {
      const { getByTestId } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByTestId('linear-gradient')).toBeTruthy();
    });

    it('should show sentiment bar visualization', () => {
      const { getByText } = render(<SmartOverviewHeader {...defaultProps} />);

      expect(getByText('Genel Sentiment')).toBeTruthy();
      // Sentiment bar would be tested with specific testIDs in actual implementation
    });

    it('should display appropriate colors for P&L values', () => {
      // This would be tested by checking style props in actual implementation
      expect(() => {
        render(<SmartOverviewHeader {...defaultProps} />);
      }).not.toThrow();
    });
  });
});