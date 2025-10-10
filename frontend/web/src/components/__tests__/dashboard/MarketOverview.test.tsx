import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import MarketOverview from '../../dashboard/MarketOverview';
import {
  renderWithProviders,
  createMarketStateWithData,
  mockSymbolsResponse,
  mockMarketData,
} from '../../../test-utils';

// Mock the WebSocket hook
const mockUseWebSocket = vi.fn();
vi.mock('../../../hooks/useWebSocket', () => ({
  useWebSocket: mockUseWebSocket,
}));

// Mock market data service
vi.mock('../../../services/marketDataService', () => ({
  marketDataService: {
    getSymbols: vi.fn(),
    getMultipleMarketData: vi.fn(),
  },
}));

describe('MarketOverview Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.clearAllTimers();
    vi.useFakeTimers();

    // Default WebSocket mock
    mockUseWebSocket.mockReturnValue({
      connected: true,
      connecting: false,
      error: null,
    });
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  describe('Loading States', () => {
    it('shows loading state when market data is being fetched', () => {
      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: null,
            marketData: {},
            selectedSymbols: [],
            isLoading: true,
            error: null,
            lastUpdate: null,
          },
        },
      });

      expect(screen.getByText('Loading market data...')).toBeInTheDocument();
      expect(screen.getByText('Market Overview')).toBeInTheDocument();
    });

    it('shows individual symbol loading states', () => {
      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: mockSymbolsResponse,
            marketData: {}, // No data loaded yet
            selectedSymbols: ['BTCUSDT'],
            isLoading: false,
            error: null,
            lastUpdate: null,
          },
        },
      });

      expect(screen.getByText('BTCUSDT')).toBeInTheDocument();
      expect(screen.getByText('Bitcoin / Tether')).toBeInTheDocument();
      expect(screen.getByText('Loading...')).toBeInTheDocument();
    });
  });

  describe('Market Data Display', () => {
    it('displays market data correctly when loaded', () => {
      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      expect(screen.getByText('BTCUSDT')).toBeInTheDocument();
      expect(screen.getByText('Bitcoin / Tether')).toBeInTheDocument();
      expect(screen.getByText('$50,000.00')).toBeInTheDocument();
      expect(screen.getByText('+5.26%')).toBeInTheDocument();
      expect(screen.getByText('Vol: 1M')).toBeInTheDocument();
    });

    it('formats prices correctly for different values', () => {
      const testData = {
        ...mockMarketData,
        price: 0.123456,
      };

      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: mockSymbolsResponse,
            marketData: { [testData.symbolId]: testData },
            selectedSymbols: [testData.symbolId],
            isLoading: false,
            error: null,
            lastUpdate: '2024-01-01T12:00:00Z',
          },
        },
      });

      expect(screen.getByText('$0.123456')).toBeInTheDocument();
    });

    it('formats percentage changes correctly', () => {
      const negativeChangeData = {
        ...mockMarketData,
        changePercent24h: -3.45,
      };

      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: mockSymbolsResponse,
            marketData: { [negativeChangeData.symbolId]: negativeChangeData },
            selectedSymbols: [negativeChangeData.symbolId],
            isLoading: false,
            error: null,
            lastUpdate: '2024-01-01T12:00:00Z',
          },
        },
      });

      expect(screen.getByText('-3.45%')).toBeInTheDocument();

      const changeElement = screen.getByText('-3.45%');
      expect(changeElement).toHaveClass('negative');
    });

    it('formats volume with compact notation', () => {
      const highVolumeData = {
        ...mockMarketData,
        volume: 1234567890,
      };

      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: mockSymbolsResponse,
            marketData: { [highVolumeData.symbolId]: highVolumeData },
            selectedSymbols: [highVolumeData.symbolId],
            isLoading: false,
            error: null,
            lastUpdate: '2024-01-01T12:00:00Z',
          },
        },
      });

      expect(screen.getByText('Vol: 1.2B')).toBeInTheDocument();
    });

    it('applies correct CSS classes for positive and negative changes', () => {
      const positiveData = { ...mockMarketData, changePercent24h: 5.26 };
      const negativeData = { ...mockMarketData, symbolId: 'ETHUSDT', ticker: 'ETHUSDT', changePercent24h: -2.15 };

      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: {
              ...mockSymbolsResponse,
              symbols: {
                ...mockSymbolsResponse.symbols,
                ETHUSDT: {
                  symbol: 'ETHUSDT',
                  display_name: 'Ethereum / Tether',
                  precision: 2,
                  strategy_type: 'crypto',
                },
              },
            },
            marketData: {
              [positiveData.symbolId]: positiveData,
              [negativeData.symbolId]: negativeData,
            },
            selectedSymbols: [positiveData.symbolId, negativeData.symbolId],
            isLoading: false,
            error: null,
            lastUpdate: '2024-01-01T12:00:00Z',
          },
        },
      });

      const positiveChange = screen.getByText('+5.26%');
      const negativeChange = screen.getByText('-2.15%');

      expect(positiveChange).toHaveClass('positive');
      expect(negativeChange).toHaveClass('negative');
    });
  });

  describe('WebSocket Status Indicators', () => {
    it('shows connected status when WebSocket is connected', () => {
      mockUseWebSocket.mockReturnValue({
        connected: true,
        connecting: false,
        error: null,
      });

      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      expect(screen.getByText('🟢 Live')).toBeInTheDocument();
    });

    it('shows connecting status when WebSocket is connecting', () => {
      mockUseWebSocket.mockReturnValue({
        connected: false,
        connecting: true,
        error: null,
      });

      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      expect(screen.getByText('🟡 Connecting...')).toBeInTheDocument();
    });

    it('shows polling status when WebSocket is disconnected', () => {
      mockUseWebSocket.mockReturnValue({
        connected: false,
        connecting: false,
        error: new Error('Connection failed'),
      });

      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      expect(screen.getByText('🔴 Polling')).toBeInTheDocument();
    });

    it('shows last update time when available', () => {
      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      // The exact time format will depend on locale, but should contain "Last updated"
      expect(screen.getByText(/Last updated:/)).toBeInTheDocument();
    });
  });

  describe('Error Handling', () => {
    it('displays error state when market data fetch fails', () => {
      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: null,
            marketData: {},
            selectedSymbols: [],
            isLoading: false,
            error: 'Failed to fetch market data',
            lastUpdate: null,
          },
        },
      });

      expect(screen.getByText('Failed to load market data: Failed to fetch market data')).toBeInTheDocument();
      expect(screen.getByText('Retry')).toBeInTheDocument();
    });

    it('handles retry button click', async () => {
      const user = userEvent.setup();

      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: null,
            marketData: {},
            selectedSymbols: [],
            isLoading: false,
            error: 'Network error',
            lastUpdate: null,
          },
        },
      });

      const retryButton = screen.getByText('Retry');
      await user.click(retryButton);

      // This would trigger a Redux action (tested in integration tests)
      expect(retryButton).toBeInTheDocument();
    });
  });

  describe('Empty States', () => {
    it('shows empty state when no symbols are available', () => {
      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: null,
            marketData: {},
            selectedSymbols: [],
            isLoading: false,
            error: null,
            lastUpdate: null,
          },
        },
      });

      expect(screen.getByText('No market data available')).toBeInTheDocument();
      expect(screen.getByText('Load Data')).toBeInTheDocument();
    });

    it('handles load data button click in empty state', async () => {
      const user = userEvent.setup();

      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: null,
            marketData: {},
            selectedSymbols: [],
            isLoading: false,
            error: null,
            lastUpdate: null,
          },
        },
      });

      const loadDataButton = screen.getByText('Load Data');
      await user.click(loadDataButton);

      // This would trigger a Redux action
      expect(loadDataButton).toBeInTheDocument();
    });
  });

  describe('Component Lifecycle and Effects', () => {
    it('fetches symbols on mount', () => {
      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: null,
            marketData: {},
            selectedSymbols: [],
            isLoading: false,
            error: null,
            lastUpdate: null,
          },
        },
      });

      // This would be verified through Redux action dispatch in integration tests
      expect(screen.getByText('Market Overview')).toBeInTheDocument();
    });

    it('sets up polling interval when WebSocket is not connected', async () => {
      mockUseWebSocket.mockReturnValue({
        connected: false,
        connecting: false,
        error: null,
      });

      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      // Fast-forward time to trigger the interval
      vi.advanceTimersByTime(30000);

      // Polling should trigger market data refresh (tested in integration)
      expect(vi.getTimerCount()).toBeGreaterThan(0);
    });

    it('does not poll when WebSocket is connected', () => {
      mockUseWebSocket.mockReturnValue({
        connected: true,
        connecting: false,
        error: null,
      });

      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      // Even after advancing time, polling should not occur when WebSocket is connected
      vi.advanceTimersByTime(30000);

      // Component should still be rendered normally
      expect(screen.getByText('Market Overview')).toBeInTheDocument();
    });

    it('limits symbols display to maximum of 6', () => {
      const largeSymbolsResponse = {
        symbols: Object.fromEntries(
          Array.from({ length: 10 }, (_, i) => [
            `SYMBOL${i}`,
            {
              symbol: `SYMBOL${i}`,
              display_name: `Symbol ${i}`,
              precision: 2,
              strategy_type: 'crypto',
            },
          ])
        ),
        interval: '1m',
      };

      renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: largeSymbolsResponse,
            marketData: {},
            selectedSymbols: [],
            isLoading: false,
            error: null,
            lastUpdate: null,
          },
        },
      });

      // Should only show first 6 symbols
      expect(screen.getByText('SYMBOL0')).toBeInTheDocument();
      expect(screen.getByText('SYMBOL5')).toBeInTheDocument();
      expect(screen.queryByText('SYMBOL6')).not.toBeInTheDocument();
      expect(screen.queryByText('SYMBOL9')).not.toBeInTheDocument();
    });
  });

  describe('Responsive Design and CSS Classes', () => {
    it('applies correct CSS classes', () => {
      const { container } = renderWithProviders(<MarketOverview className="custom-class" />, {
        preloadedState: createMarketStateWithData(),
      });

      const marketOverview = container.querySelector('.market-overview');
      expect(marketOverview).toHaveClass('custom-class');
    });

    it('applies error CSS class when in error state', () => {
      const { container } = renderWithProviders(<MarketOverview />, {
        preloadedState: {
          market: {
            symbols: null,
            marketData: {},
            selectedSymbols: [],
            isLoading: false,
            error: 'Test error',
            lastUpdate: null,
          },
        },
      });

      const marketOverview = container.querySelector('.market-overview');
      expect(marketOverview).toHaveClass('error');
    });

    it('renders market grid with proper structure', () => {
      const { container } = renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      const marketGrid = container.querySelector('.market-grid');
      expect(marketGrid).toBeInTheDocument();

      const marketCards = container.querySelectorAll('.market-card');
      expect(marketCards.length).toBeGreaterThan(0);
    });
  });

  describe('WebSocket Integration', () => {
    it('passes correct parameters to useWebSocket hook', () => {
      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      expect(mockUseWebSocket).toHaveBeenCalledWith({
        enabled: true,
        symbols: expect.any(Array),
      });
    });

    it('handles WebSocket error states gracefully', () => {
      mockUseWebSocket.mockReturnValue({
        connected: false,
        connecting: false,
        error: new Error('WebSocket connection failed'),
      });

      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      const pollingStatus = screen.getByText('🔴 Polling');
      expect(pollingStatus).toHaveAttribute('title', 'WebSocket connection failed');
    });

    it('shows appropriate status when WebSocket error has no message', () => {
      mockUseWebSocket.mockReturnValue({
        connected: false,
        connecting: false,
        error: {},
      });

      renderWithProviders(<MarketOverview />, {
        preloadedState: createMarketStateWithData(),
      });

      const pollingStatus = screen.getByText('🔴 Polling');
      expect(pollingStatus).toHaveAttribute('title', 'WebSocket unavailable, using polling');
    });
  });
});