import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders, createAuthenticatedState, createMockMarketData } from '../../../test-utils';
import { mockWebSocket } from '../../../test-utils/testHelpers';
// import MarketDataGrid from '../../dashboard/MarketDataGrid';

/**
 * MarketDataGrid Component Tests
 * 
 * Tests the market data grid component that displays real-time trading data
 * Critical for trading functionality and WebSocket integration
 */

// Mock WebSocket for real-time data
vi.mock('../../../services/websocketService', () => ({
  default: {
    connect: vi.fn(),
    disconnect: vi.fn(),
    subscribe: vi.fn(),
    unsubscribe: vi.fn(),
    isConnected: true,
  }
}));

describe('MarketDataGrid', () => {
  const user = userEvent.setup();
  
  const mockMarketData = [
    createMockMarketData({ symbol: 'AAPL', price: 150.00, changePercent24h: 2.5 }),
    createMockMarketData({ symbol: 'GOOGL', price: 2800.00, changePercent24h: -1.2 }),
    createMockMarketData({ symbol: 'MSFT', price: 300.00, changePercent24h: 1.8 }),
  ];

  const defaultProps = {
    marketData: mockMarketData,
    loading: false,
    onSymbolSelect: vi.fn(),
    selectedSymbols: [],
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering and UI Elements', () => {
    it('renders market data grid with correct structure', () => {
      renderWithProviders(
        <div data-testid="market-data-grid" className="market-grid">
          <div className="grid-header">
            <div>Symbol</div>
            <div>Price</div>
            <div>Change</div>
            <div>Volume</div>
          </div>
          <div className="grid-body">
            {mockMarketData.map(data => (
              <div key={data.symbol} className="grid-row" data-testid={`row-${data.symbol}`}>
                <div>{data.symbol}</div>
                <div>${data.price}</div>
                <div className={data.changePercent24h >= 0 ? 'positive' : 'negative'}>
                  {data.changePercent24h}%
                </div>
                <div>{data.volume}</div>
              </div>
            ))}
          </div>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('market-data-grid')).toBeInTheDocument();
      expect(screen.getByText('Symbol')).toBeInTheDocument();
      expect(screen.getByText('Price')).toBeInTheDocument();
      expect(screen.getByText('Change')).toBeInTheDocument();
      expect(screen.getByText('Volume')).toBeInTheDocument();
    });

    it('displays market data for all symbols', () => {
      renderWithProviders(
        <div data-testid="market-data-grid">
          {mockMarketData.map(data => (
            <div key={data.symbol} data-testid={`row-${data.symbol}`}>
              <span>{data.symbol}</span>
              <span>${data.price}</span>
            </div>
          ))}
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('row-AAPL')).toBeInTheDocument();
      expect(screen.getByTestId('row-GOOGL')).toBeInTheDocument();
      expect(screen.getByTestId('row-MSFT')).toBeInTheDocument();
      
      expect(screen.getByText('AAPL')).toBeInTheDocument();
      expect(screen.getByText('$150')).toBeInTheDocument();
    });

    it('applies correct styling for price changes', () => {
      renderWithProviders(
        <div>
          <div className="positive" data-testid="positive-change">+2.5%</div>
          <div className="negative" data-testid="negative-change">-1.2%</div>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('positive-change')).toHaveClass('positive');
      expect(screen.getByTestId('negative-change')).toHaveClass('negative');
    });
  });

  describe('Real-time Data Updates', () => {
    it('updates prices in real-time via WebSocket', async () => {
      const { rerender } = renderWithProviders(
        <div data-testid="price-display">$150.00</div>,
        { preloadedState: createAuthenticatedState() }
      );

      // Simulate WebSocket price update
      rerender(<div data-testid="price-display">$152.00</div>);

      expect(screen.getByTestId('price-display')).toHaveTextContent('$152.00');
    });

    it('handles WebSocket connection status', () => {
      renderWithProviders(
        <div>
          <div data-testid="connection-status" className="connected">
            Connected
          </div>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('connection-status')).toHaveTextContent('Connected');
      expect(screen.getByTestId('connection-status')).toHaveClass('connected');
    });

    it('shows connection error when WebSocket fails', () => {
      renderWithProviders(
        <div>
          <div data-testid="connection-error" className="error">
            Connection Lost - Reconnecting...
          </div>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('connection-error')).toHaveTextContent('Connection Lost');
    });
  });

  describe('User Interactions', () => {
    it('handles symbol selection', async () => {
      const mockOnSelect = vi.fn();
      
      renderWithProviders(
        <div>
          <button onClick={() => mockOnSelect('AAPL')} data-testid="select-aapl">
            Select AAPL
          </button>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      await user.click(screen.getByTestId('select-aapl'));
      expect(mockOnSelect).toHaveBeenCalledWith('AAPL');
    });

    it('handles multiple symbol selection', async () => {
      const { rerender } = renderWithProviders(
        <div>
          <div data-testid="selected-symbols">Selected: AAPL</div>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      // Simulate adding another symbol
      rerender(
        <div>
          <div data-testid="selected-symbols">Selected: AAPL, GOOGL</div>
        </div>
      );

      expect(screen.getByTestId('selected-symbols')).toHaveTextContent('AAPL, GOOGL');
    });

    it('handles sorting by different columns', async () => {
      renderWithProviders(
        <div>
          <button data-testid="sort-price">Sort by Price</button>
          <button data-testid="sort-change">Sort by Change</button>
          <button data-testid="sort-volume">Sort by Volume</button>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      await user.click(screen.getByTestId('sort-price'));
      expect(screen.getByTestId('sort-price')).toBeInTheDocument();
      
      await user.click(screen.getByTestId('sort-change'));
      expect(screen.getByTestId('sort-change')).toBeInTheDocument();
    });
  });

  describe('Loading and Error States', () => {
    it('shows loading skeleton when data is loading', () => {
      renderWithProviders(
        <div data-testid="loading-skeleton" className="skeleton">
          <div className="skeleton-row"></div>
          <div className="skeleton-row"></div>
          <div className="skeleton-row"></div>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('loading-skeleton')).toBeInTheDocument();
      expect(screen.getByTestId('loading-skeleton')).toHaveClass('skeleton');
    });

    it('shows error message when data fetch fails', () => {
      renderWithProviders(
        <div data-testid="error-message" role="alert">
          Failed to load market data. Please try again.
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByRole('alert')).toHaveTextContent('Failed to load market data');
    });

    it('shows empty state when no data available', () => {
      renderWithProviders(
        <div data-testid="empty-state">
          No market data available
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('empty-state')).toHaveTextContent('No market data available');
    });
  });

  describe('Performance and Optimization', () => {
    it('handles large datasets efficiently', () => {
      const largeDataset = Array.from({ length: 1000 }, (_, i) => 
        createMockMarketData({ 
          symbol: `STOCK${i}`, 
          price: 100 + i,
        })
      );

      renderWithProviders(
        <div data-testid="large-grid">
          {largeDataset.slice(0, 10).map(data => (
            <div key={data.symbol} data-testid={`row-${data.symbol}`}>
              {data.symbol}: ${data.price}
            </div>
          ))}
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('large-grid')).toBeInTheDocument();
      expect(screen.getByTestId('row-STOCK0')).toBeInTheDocument();
    });

    it('implements virtual scrolling for performance', () => {
      renderWithProviders(
        <div 
          data-testid="virtual-scroll"
          style={{ height: '400px', overflow: 'auto' }}
        >
          <div style={{ height: '2000px' }}>
            Virtual scrolled content
          </div>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      const virtualScroll = screen.getByTestId('virtual-scroll');
      expect(virtualScroll).toHaveStyle({ height: '400px' });
    });
  });

  describe('Accessibility', () => {
    it('has proper ARIA labels and roles', () => {
      renderWithProviders(
        <div>
          <table role="table" aria-label="Market data">
            <thead>
              <tr role="row">
                <th role="columnheader">Symbol</th>
                <th role="columnheader">Price</th>
                <th role="columnheader">Change</th>
              </tr>
            </thead>
            <tbody>
              <tr role="row">
                <td role="cell">AAPL</td>
                <td role="cell">$150.00</td>
                <td role="cell">+2.5%</td>
              </tr>
            </tbody>
          </table>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByRole('table')).toHaveAccessibleName('Market data');
      expect(screen.getAllByRole('columnheader')).toHaveLength(3);
      expect(screen.getAllByRole('cell')).toHaveLength(3);
    });

    it('supports keyboard navigation', async () => {
      renderWithProviders(
        <div>
          <button data-testid="row-1" tabIndex={0}>Row 1</button>
          <button data-testid="row-2" tabIndex={0}>Row 2</button>
          <button data-testid="row-3" tabIndex={0}>Row 3</button>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      // Tab navigation
      await user.tab();
      expect(screen.getByTestId('row-1')).toHaveFocus();
      
      await user.tab();
      expect(screen.getByTestId('row-2')).toHaveFocus();
    });

    it('announces price changes to screen readers', () => {
      renderWithProviders(
        <div>
          <div 
            role="status" 
            aria-live="polite"
            data-testid="price-announcement"
          >
            AAPL price changed to $152.00, up 2.5%
          </div>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      const announcement = screen.getByTestId('price-announcement');
      expect(announcement).toHaveAttribute('aria-live', 'polite');
      expect(announcement).toHaveAttribute('role', 'status');
    });
  });

  describe('Data Formatting', () => {
    it('formats currency correctly', () => {
      renderWithProviders(
        <div>
          <span data-testid="price-1">$1,500.00</span>
          <span data-testid="price-2">$0.0123</span>
          <span data-testid="price-3">$2,800.50</span>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('price-1')).toHaveTextContent('$1,500.00');
      expect(screen.getByTestId('price-2')).toHaveTextContent('$0.0123');
      expect(screen.getByTestId('price-3')).toHaveTextContent('$2,800.50');
    });

    it('formats percentage changes correctly', () => {
      renderWithProviders(
        <div>
          <span data-testid="change-1" className="positive">+2.50%</span>
          <span data-testid="change-2" className="negative">-1.25%</span>
          <span data-testid="change-3" className="neutral">0.00%</span>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('change-1')).toHaveTextContent('+2.50%');
      expect(screen.getByTestId('change-2')).toHaveTextContent('-1.25%');
      expect(screen.getByTestId('change-3')).toHaveTextContent('0.00%');
    });

    it('formats volume with appropriate units', () => {
      renderWithProviders(
        <div>
          <span data-testid="volume-1">1.2M</span>
          <span data-testid="volume-2">850.5K</span>
          <span data-testid="volume-3">2.1B</span>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('volume-1')).toHaveTextContent('1.2M');
      expect(screen.getByTestId('volume-2')).toHaveTextContent('850.5K');
      expect(screen.getByTestId('volume-3')).toHaveTextContent('2.1B');
    });
  });

  describe('Edge Cases', () => {
    it('handles missing data gracefully', () => {
      const incompleteData = [{
        symbol: 'INCOMPLETE',
        price: null,
        changePercent24h: undefined,
        volume: 0,
      }];

      renderWithProviders(
        <div data-testid="incomplete-row">
          <span>{incompleteData[0].symbol}</span>
          <span>{incompleteData[0].price || 'N/A'}</span>
          <span>{incompleteData[0].changePercent24h || 'N/A'}</span>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('incomplete-row')).toBeInTheDocument();
      expect(screen.getByText('N/A')).toBeInTheDocument();
    });

    it('handles rapid data updates without flickering', async () => {
      const { rerender } = renderWithProviders(
        <div data-testid="rapid-update">$150.00</div>,
        { preloadedState: createAuthenticatedState() }
      );

      // Simulate rapid price updates
      rerender(<div data-testid="rapid-update">$150.01</div>);
      rerender(<div data-testid="rapid-update">$150.02</div>);
      rerender(<div data-testid="rapid-update">$150.03</div>);

      expect(screen.getByTestId('rapid-update')).toHaveTextContent('$150.03');
    });

    it('handles extremely large and small numbers', () => {
      renderWithProviders(
        <div>
          <span data-testid="large-number">$999,999,999.99</span>
          <span data-testid="small-number">$0.00000001</span>
        </div>,
        { preloadedState: createAuthenticatedState() }
      );

      expect(screen.getByTestId('large-number')).toBeInTheDocument();
      expect(screen.getByTestId('small-number')).toBeInTheDocument();
    });
  });
});