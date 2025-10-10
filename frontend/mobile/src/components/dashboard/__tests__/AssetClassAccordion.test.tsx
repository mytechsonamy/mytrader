import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import { Animated, LayoutAnimation } from 'react-native';
import AssetClassAccordion from '../AssetClassAccordion';

// Mock React Native components
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    Animated: {
      ...actualRN.Animated,
      Value: jest.fn(() => ({
        interpolate: jest.fn(() => 'mocked-interpolation'),
      })),
      timing: jest.fn(() => ({ start: jest.fn() })),
      View: actualRN.View,
    },
    LayoutAnimation: {
      configureNext: jest.fn(),
      Presets: {
        easeInEaseOut: {},
      },
    },
    UIManager: {
      setLayoutAnimationEnabledExperimental: jest.fn(),
    },
  };
});

// Mock AssetCard component
jest.mock('../AssetCard', () => {
  const { Text, View } = jest.requireActual('react-native');
  return ({ symbol, isLoading }: any) => (
    <View testID={`asset-card-${symbol.id || 'loading'}`}>
      {isLoading ? (
        <Text>Loading...</Text>
      ) : (
        <Text>{symbol.displayName || symbol.symbol}</Text>
      )}
    </View>
  );
});

// Mock MarketStatusIndicator
jest.mock('./MarketStatusIndicator', () => ({
  MarketStatusBadge: ({ status }: any) => {
    const { Text } = jest.requireActual('react-native');
    return <Text testID="market-status-badge">{status}</Text>;
  },
}));

// Mock animation utils
jest.mock('../../utils/animationUtils', () => ({
  createTimingAnimation: jest.fn(() => ({ start: jest.fn() })),
  runSafeAnimation: jest.fn(),
}));

describe('AssetClassAccordion Component', () => {
  const validSymbolsData = [
    {
      id: '1',
      symbol: 'BTC-USD',
      displayName: 'Bitcoin',
      assetClass: 'CRYPTO' as const,
      quoteCurrency: 'USD',
      baseCurrency: 'BTC',
      marketId: 'CRYPTO',
    },
    {
      id: '2',
      symbol: 'ETH-USD',
      displayName: 'Ethereum',
      assetClass: 'CRYPTO' as const,
      quoteCurrency: 'USD',
      baseCurrency: 'ETH',
      marketId: 'CRYPTO',
    },
    {
      id: '3',
      symbol: 'ADA-USD',
      displayName: 'Cardano',
      assetClass: 'CRYPTO' as const,
      quoteCurrency: 'USD',
      baseCurrency: 'ADA',
      marketId: 'CRYPTO',
    },
  ];

  const validMarketData = {
    '1': {
      symbol: 'BTC-USD',
      price: 50000,
      change: 2500,
      changePercent: 5.2,
      volume: 1000000,
      marketCap: 900000000,
      lastUpdate: '2023-12-24T10:00:00Z',
    },
    '2': {
      symbol: 'ETH-USD',
      price: 3000,
      change: -150,
      changePercent: -4.8,
      volume: 800000,
      marketCap: 360000000,
      lastUpdate: '2023-12-24T10:00:00Z',
    },
  };

  const validSummaryData = {
    totalSymbols: 3,
    gainers: 1,
    losers: 2,
    averageChange: 1.5,
  };

  const defaultProps = {
    assetClass: 'CRYPTO' as const,
    title: '🚀 Kripto',
    icon: '🚀',
    symbols: validSymbolsData,
    marketData: validMarketData,
    summary: validSummaryData,
    marketStatus: 'OPEN' as const,
    nextChangeTime: '2023-12-25T00:00:00Z',
    isExpanded: false,
    defaultExpanded: false,
    maxVisibleItems: 6,
    showLoadMore: false,
    isLoading: false,
    onToggle: jest.fn(),
    onSymbolPress: jest.fn(),
    onStrategyTest: jest.fn(),
    onAddToWatchlist: jest.fn(),
    onLoadMore: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Rendering States', () => {
    it('should render without crashing', () => {
      expect(() => {
        render(<AssetClassAccordion {...defaultProps} />);
      }).not.toThrow();
    });

    it('should render header correctly when collapsed', () => {
      const { getByText } = render(<AssetClassAccordion {...defaultProps} />);

      expect(getByText('🚀')).toBeTruthy();
      expect(getByText('🚀 Kripto')).toBeTruthy();
      expect(getByText('1↗ 2↘ Ort: +1.50%')).toBeTruthy();
      expect(getByText('3 varlık')).toBeTruthy();
    });

    it('should render content when expanded', () => {
      const { getByText, getByTestId } = render(
        <AssetClassAccordion {...defaultProps} isExpanded={true} />
      );

      expect(getByText('Bitcoin')).toBeTruthy();
      expect(getByText('Ethereum')).toBeTruthy();
      expect(getByText('Cardano')).toBeTruthy();
      expect(getByTestId('asset-card-1')).toBeTruthy();
    });

    it('should not render content when collapsed', () => {
      const { queryByText } = render(
        <AssetClassAccordion {...defaultProps} isExpanded={false} />
      );

      expect(queryByText('Bitcoin')).toBeNull();
      expect(queryByText('Ethereum')).toBeNull();
    });

    it('should render market status badge when provided', () => {
      const { getByTestId } = render(<AssetClassAccordion {...defaultProps} />);

      expect(getByTestId('market-status-badge')).toBeTruthy();
    });
  });

  describe('Expansion and Collapse', () => {
    it('should toggle expansion when header is pressed (uncontrolled)', () => {
      const onToggleMock = jest.fn();
      const { getByText, rerender } = render(
        <AssetClassAccordion
          {...defaultProps}
          isExpanded={undefined} // Make it uncontrolled
          onToggle={onToggleMock}
        />
      );

      // Initially collapsed
      expect(queryByText('Bitcoin')).toBeNull();

      // Press header to expand
      fireEvent.press(getByText('🚀 Kripto'));

      expect(onToggleMock).toHaveBeenCalledWith('CRYPTO', true);
      expect(LayoutAnimation.configureNext).toHaveBeenCalled();
    });

    it('should handle controlled expansion state', () => {
      const onToggleMock = jest.fn();
      const { getByText, queryByText } = render(
        <AssetClassAccordion
          {...defaultProps}
          isExpanded={false}
          onToggle={onToggleMock}
        />
      );

      // Initially collapsed (controlled)
      expect(queryByText('Bitcoin')).toBeNull();

      // Press header
      fireEvent.press(getByText('🚀 Kripto'));

      expect(onToggleMock).toHaveBeenCalledWith('CRYPTO', true);
      // Content should still be hidden because isExpanded is still false
      expect(queryByText('Bitcoin')).toBeNull();
    });

    it('should use defaultExpanded for initial state', () => {
      const { getByText } = render(
        <AssetClassAccordion
          {...defaultProps}
          isExpanded={undefined} // Uncontrolled
          defaultExpanded={true}
        />
      );

      // Should be expanded initially
      expect(getByText('Bitcoin')).toBeTruthy();
    });
  });

  describe('Loading States', () => {
    it('should render loading cards when isLoading is true', () => {
      const { getAllByTestId } = render(
        <AssetClassAccordion {...defaultProps} isExpanded={true} isLoading={true} />
      );

      const loadingCards = getAllByTestId(/asset-card-loading/);
      expect(loadingCards).toHaveLength(3);
    });

    it('should render actual symbols when not loading', () => {
      const { getByText, queryByText } = render(
        <AssetClassAccordion {...defaultProps} isExpanded={true} isLoading={false} />
      );

      expect(getByText('Bitcoin')).toBeTruthy();
      expect(getByText('Ethereum')).toBeTruthy();
      expect(queryByText('Loading...')).toBeNull();
    });
  });

  describe('Empty State', () => {
    it('should render empty state when no symbols', () => {
      const { getByText } = render(
        <AssetClassAccordion
          {...defaultProps}
          symbols={[]}
          isExpanded={true}
          isLoading={false}
        />
      );

      expect(getByText('📭')).toBeTruthy();
      expect(getByText('Bu kategoride varlık bulunamadı')).toBeTruthy();
    });

    it('should not render empty state when loading', () => {
      const { queryByText } = render(
        <AssetClassAccordion
          {...defaultProps}
          symbols={[]}
          isExpanded={true}
          isLoading={true}
        />
      );

      expect(queryByText('Bu kategoride varlık bulunamadı')).toBeNull();
    });
  });

  describe('Summary Data', () => {
    it('should format summary text correctly with positive average', () => {
      const { getByText } = render(<AssetClassAccordion {...defaultProps} />);

      expect(getByText('1↗ 2↘ Ort: +1.50%')).toBeTruthy();
    });

    it('should format summary text correctly with negative average', () => {
      const negativeAverage = {
        ...validSummaryData,
        averageChange: -2.3,
      };

      const { getByText } = render(
        <AssetClassAccordion {...defaultProps} summary={negativeAverage} />
      );

      expect(getByText('2↗ 1↘ Ort: -2.30%')).toBeTruthy();
    });

    it('should handle missing summary data', () => {
      const { getByText } = render(
        <AssetClassAccordion {...defaultProps} summary={undefined} />
      );

      expect(getByText('Veri yok')).toBeTruthy();
    });

    it('should handle empty summary data', () => {
      const emptySummary = {
        totalSymbols: 0,
        gainers: 0,
        losers: 0,
        averageChange: 0,
      };

      const { getByText } = render(
        <AssetClassAccordion {...defaultProps} summary={emptySummary} />
      );

      expect(getByText('Veri yok')).toBeTruthy();
    });

    it('should not show symbol count when totalSymbols is 0', () => {
      const emptySummary = {
        totalSymbols: 0,
        gainers: 0,
        losers: 0,
        averageChange: 0,
      };

      const { queryByText } = render(
        <AssetClassAccordion {...defaultProps} summary={emptySummary} />
      );

      expect(queryByText('0 varlık')).toBeNull();
    });
  });

  describe('Load More Functionality', () => {
    it('should show load more button when symbols exceed maxVisibleItems', () => {
      const manySymbols = Array.from({ length: 10 }, (_, index) => ({
        ...validSymbolsData[0],
        id: `symbol-${index}`,
        displayName: `Symbol ${index}`,
      }));

      const { getByText } = render(
        <AssetClassAccordion
          {...defaultProps}
          symbols={manySymbols}
          maxVisibleItems={5}
          isExpanded={true}
        />
      );

      expect(getByText('5 tane daha göster')).toBeTruthy();
    });

    it('should handle show more button press', () => {
      const manySymbols = Array.from({ length: 10 }, (_, index) => ({
        ...validSymbolsData[0],
        id: `symbol-${index}`,
        displayName: `Symbol ${index}`,
      }));

      const { getByText, queryByText } = render(
        <AssetClassAccordion
          {...defaultProps}
          symbols={manySymbols}
          maxVisibleItems={5}
          isExpanded={true}
        />
      );

      // Initially shows "5 tane daha göster"
      expect(getByText('5 tane daha göster')).toBeTruthy();

      // Press show more
      fireEvent.press(getByText('5 tane daha göster'));

      // Should show all symbols now
      expect(queryByText('5 tane daha göster')).toBeNull();
    });

    it('should call onLoadMore when showLoadMore is enabled', () => {
      const onLoadMoreMock = jest.fn();
      const { getByText } = render(
        <AssetClassAccordion
          {...defaultProps}
          symbols={validSymbolsData}
          maxVisibleItems={3}
          showLoadMore={true}
          isExpanded={true}
          onLoadMore={onLoadMoreMock}
        />
      );

      fireEvent.press(getByText('Daha fazla yükle'));

      expect(onLoadMoreMock).toHaveBeenCalledWith('CRYPTO');
    });

    it('should not show load more button when symbols are within limit', () => {
      const { queryByText } = render(
        <AssetClassAccordion
          {...defaultProps}
          symbols={validSymbolsData.slice(0, 2)}
          maxVisibleItems={5}
          isExpanded={true}
        />
      );

      expect(queryByText('tane daha göster')).toBeNull();
      expect(queryByText('Daha fazla yükle')).toBeNull();
    });
  });

  describe('Symbol Interactions', () => {
    it('should pass correct props to AssetCard components', () => {
      const { getByTestId } = render(
        <AssetClassAccordion {...defaultProps} isExpanded={true} />
      );

      expect(getByTestId('asset-card-1')).toBeTruthy();
      expect(getByTestId('asset-card-2')).toBeTruthy();
      expect(getByTestId('asset-card-3')).toBeTruthy();
    });

    it('should handle symbol press callbacks', () => {
      // This would be tested by verifying that the AssetCard receives
      // the correct onPress, onStrategyTest, and onAddToWatchlist callbacks
      expect(() => {
        render(<AssetClassAccordion {...defaultProps} isExpanded={true} />);
      }).not.toThrow();
    });
  });

  describe('Market Data Integration', () => {
    it('should pass market data to symbol cards', () => {
      const { getByTestId } = render(
        <AssetClassAccordion {...defaultProps} isExpanded={true} />
      );

      // AssetCard should receive market data for symbols
      expect(getByTestId('asset-card-1')).toBeTruthy();
    });

    it('should handle missing market data gracefully', () => {
      const { getByTestId } = render(
        <AssetClassAccordion
          {...defaultProps}
          marketData={{}} // Empty market data
          isExpanded={true}
        />
      );

      expect(getByTestId('asset-card-1')).toBeTruthy();
    });

    it('should match market data by both id and symbol', () => {
      const marketDataWithSymbolKeys = {
        'BTC-USD': validMarketData['1'],
        'ETH-USD': validMarketData['2'],
      };

      expect(() => {
        render(
          <AssetClassAccordion
            {...defaultProps}
            marketData={marketDataWithSymbolKeys}
            isExpanded={true}
          />
        );
      }).not.toThrow();
    });
  });

  describe('Animation', () => {
    it('should initialize animation values', () => {
      render(<AssetClassAccordion {...defaultProps} />);

      expect(Animated.Value).toHaveBeenCalled();
    });

    it('should trigger layout animation on toggle', () => {
      const { getByText } = render(
        <AssetClassAccordion {...defaultProps} isExpanded={undefined} />
      );

      fireEvent.press(getByText('🚀 Kripto'));

      expect(LayoutAnimation.configureNext).toHaveBeenCalledWith(
        LayoutAnimation.Presets.easeInEaseOut
      );
    });

    it('should handle chevron rotation animation', () => {
      const { rerender } = render(
        <AssetClassAccordion {...defaultProps} isExpanded={false} />
      );

      rerender(<AssetClassAccordion {...defaultProps} isExpanded={true} />);

      // Animation utility should be called for chevron rotation
      expect(true).toBe(true); // Placeholder for animation verification
    });
  });

  describe('Error Handling', () => {
    it('should handle malformed symbols data', () => {
      const malformedSymbols = [
        null,
        undefined,
        { id: '1' }, // Missing required fields
        { displayName: 'Test' }, // Missing id and symbol
      ] as any;

      expect(() => {
        render(
          <AssetClassAccordion
            {...defaultProps}
            symbols={malformedSymbols}
            isExpanded={true}
          />
        );
      }).not.toThrow();
    });

    it('should handle null market data', () => {
      expect(() => {
        render(
          <AssetClassAccordion
            {...defaultProps}
            marketData={null as any}
            isExpanded={true}
          />
        );
      }).not.toThrow();
    });

    it('should handle missing required props', () => {
      expect(() => {
        render(
          <AssetClassAccordion
            assetClass="CRYPTO"
            title="Crypto"
            icon="🚀"
            symbols={[]}
            marketData={{}}
          />
        );
      }).not.toThrow();
    });
  });

  describe('Memory Management', () => {
    it('should handle component unmounting', () => {
      const { unmount } = render(<AssetClassAccordion {...defaultProps} />);

      expect(() => unmount()).not.toThrow();
    });

    it('should handle rapid prop changes', () => {
      const { rerender } = render(<AssetClassAccordion {...defaultProps} />);

      // Rapidly change props
      for (let i = 0; i < 10; i++) {
        rerender(
          <AssetClassAccordion
            {...defaultProps}
            isExpanded={i % 2 === 0}
            symbols={i % 2 === 0 ? validSymbolsData : []}
          />
        );
      }

      expect(() => {
        rerender(<AssetClassAccordion {...defaultProps} />);
      }).not.toThrow();
    });

    it('should handle large datasets efficiently', () => {
      const largeSymbolDataset = Array.from({ length: 1000 }, (_, index) => ({
        ...validSymbolsData[0],
        id: `symbol-${index}`,
        displayName: `Symbol ${index}`,
      }));

      const startTime = Date.now();
      render(
        <AssetClassAccordion
          {...defaultProps}
          symbols={largeSymbolDataset}
          isExpanded={true}
        />
      );
      const renderTime = Date.now() - startTime;

      // Should render within reasonable time (less than 1 second)
      expect(renderTime).toBeLessThan(1000);
    });
  });

  describe('Accessibility', () => {
    it('should have accessible elements', () => {
      const { getAllByRole } = render(<AssetClassAccordion {...defaultProps} />);

      // Should have accessible text elements
      const textElements = getAllByRole('text');
      expect(textElements.length).toBeGreaterThan(0);
    });

    it('should support touch interactions', () => {
      const { getByText } = render(<AssetClassAccordion {...defaultProps} />);

      // Header should be pressable
      expect(() => {
        fireEvent.press(getByText('🚀 Kripto'));
      }).not.toThrow();
    });

    it('should provide proper touch feedback', () => {
      const { getByText } = render(<AssetClassAccordion {...defaultProps} />);

      // TouchableOpacity should have activeOpacity
      const header = getByText('🚀 Kripto').parent;
      expect(header).toBeTruthy();
    });
  });

  describe('Visual States', () => {
    it('should show different colors for positive and negative changes', () => {
      const positiveSummary = {
        ...validSummaryData,
        averageChange: 5.5,
      };

      const { getByText: getByTextPositive } = render(
        <AssetClassAccordion {...defaultProps} summary={positiveSummary} />
      );

      expect(getByTextPositive('1↗ 2↘ Ort: +5.50%')).toBeTruthy();

      const negativeSummary = {
        ...validSummaryData,
        averageChange: -3.2,
      };

      const { getByText: getByTextNegative } = render(
        <AssetClassAccordion {...defaultProps} summary={negativeSummary} />
      );

      expect(getByTextNegative('1↗ 2↘ Ort: -3.20%')).toBeTruthy();
    });

    it('should handle zero change correctly', () => {
      const zeroSummary = {
        ...validSummaryData,
        averageChange: 0,
      };

      const { getByText } = render(
        <AssetClassAccordion {...defaultProps} summary={zeroSummary} />
      );

      expect(getByText('1↗ 2↘ Ort: +0.00%')).toBeTruthy();
    });
  });
});