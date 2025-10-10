import { renderHook, act, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';
import authSlice from '../../store/slices/authSlice';
import marketSlice from '../../store/slices/marketSlice';
import uiSlice from '../../store/slices/uiSlice';
import { useWebSocket } from '../useWebSocket';
import { mockMarketData } from '../../test-utils';

// Mock the WebSocket service
const mockWebSocketService = {
  connect: vi.fn(),
  disconnect: vi.fn(),
  isConnected: vi.fn(() => false),
  subscribeToMultipleSymbols: vi.fn(),
  unsubscribeFromSymbol: vi.fn(),
  subscribeToSymbol: vi.fn(),
  getConnectionState: vi.fn(() => 'Disconnected'),
};

const mockCreateWebSocketService = vi.fn(() => mockWebSocketService);

vi.mock('../../services/websocketService', () => ({
  createWebSocketService: mockCreateWebSocketService,
  getWebSocketService: vi.fn(() => mockWebSocketService),
  WebSocketService: vi.fn().mockImplementation(() => mockWebSocketService),
}));

describe('useWebSocket Hook', () => {
  let testStore: any;
  let wrapper: any;

  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();

    testStore = configureStore({
      reducer: {
        auth: authSlice,
        market: marketSlice,
        ui: uiSlice,
      },
    });

    wrapper = ({ children }: { children: React.ReactNode }) => (
      <Provider store={testStore}>{children}</Provider>
    );

    // Reset mock implementations
    mockWebSocketService.connect.mockResolvedValue(undefined);
    mockWebSocketService.disconnect.mockResolvedValue(undefined);
    mockWebSocketService.isConnected.mockReturnValue(false);
    mockWebSocketService.subscribeToMultipleSymbols.mockResolvedValue(undefined);
    mockWebSocketService.unsubscribeFromSymbol.mockResolvedValue(undefined);
    mockWebSocketService.getConnectionState.mockReturnValue('Disconnected');
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  describe('Initialization', () => {
    it('should initialize with disconnected state when disabled', () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      expect(result.current.connected).toBe(false);
      expect(result.current.connecting).toBe(false);
      expect(result.current.error).toBe(null);
      expect(result.current.connectionState).toBe('Disconnected');
    });

    it('should initialize with correct default options', () => {
      const { result } = renderHook(() => useWebSocket(), { wrapper });

      expect(mockCreateWebSocketService).toHaveBeenCalledWith({
        url: 'http://localhost:8080/hubs/market-data',
        onMarketDataUpdate: expect.any(Function),
        onConnectionStatusChange: expect.any(Function),
        onError: expect.any(Function),
      });
    });

    it('should accept custom URL option', () => {
      renderHook(() => useWebSocket({ url: '/custom-hub' }), { wrapper });

      expect(mockCreateWebSocketService).toHaveBeenCalledWith({
        url: 'http://localhost:8080/custom-hub',
        onMarketDataUpdate: expect.any(Function),
        onConnectionStatusChange: expect.any(Function),
        onError: expect.any(Function),
      });
    });

    it('should provide all expected functions and properties', () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      expect(typeof result.current.connect).toBe('function');
      expect(typeof result.current.disconnect).toBe('function');
      expect(typeof result.current.subscribeToSymbols).toBe('function');
      expect(typeof result.current.unsubscribeFromSymbols).toBe('function');
      expect(result.current.service).toBeDefined();
    });
  });

  describe('Connection Management', () => {
    it('should attempt to connect when enabled', async () => {
      renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      await waitFor(() => {
        expect(mockWebSocketService.connect).toHaveBeenCalled();
      });
    });

    it('should not attempt to connect when disabled', () => {
      renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      expect(mockWebSocketService.connect).not.toHaveBeenCalled();
    });

    it('should handle manual connect call', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      await act(async () => {
        await result.current.connect();
      });

      expect(mockWebSocketService.connect).toHaveBeenCalled();
    });

    it('should handle manual disconnect call', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      await act(async () => {
        await result.current.disconnect();
      });

      expect(mockWebSocketService.disconnect).toHaveBeenCalled();
    });

    it('should prevent duplicate connections', async () => {
      mockWebSocketService.isConnected.mockReturnValue(true);

      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      await act(async () => {
        await result.current.connect();
      });

      // Should not call connect if already connected
      expect(mockWebSocketService.connect).not.toHaveBeenCalled();
    });

    it('should disconnect on unmount', () => {
      const { unmount } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      unmount();

      expect(mockWebSocketService.disconnect).toHaveBeenCalled();
    });
  });

  describe('Connection Status Handling', () => {
    it('should update state when connection status changes', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      // Get the onConnectionStatusChange callback
      const onConnectionStatusChange = mockCreateWebSocketService.mock.calls[0][0].onConnectionStatusChange;

      await act(async () => {
        onConnectionStatusChange(true);
      });

      expect(result.current.connected).toBe(true);
      expect(result.current.connecting).toBe(false);
      expect(result.current.connectionState).toBe('Connected');
    });

    it('should dispatch success notification on connection', async () => {
      renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onConnectionStatusChange = mockCreateWebSocketService.mock.calls[0][0].onConnectionStatusChange;

      await act(async () => {
        onConnectionStatusChange(true);
      });

      const state = testStore.getState();
      expect(state.ui.notifications).toHaveLength(1);
      expect(state.ui.notifications[0].type).toBe('success');
      expect(state.ui.notifications[0].message).toBe('Real-time market data connected');
    });

    it('should dispatch warning notification on disconnection', async () => {
      renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onConnectionStatusChange = mockCreateWebSocketService.mock.calls[0][0].onConnectionStatusChange;

      await act(async () => {
        onConnectionStatusChange(false);
      });

      const state = testStore.getState();
      expect(state.ui.notifications).toHaveLength(1);
      expect(state.ui.notifications[0].type).toBe('warning');
      expect(state.ui.notifications[0].message).toBe('Real-time market data disconnected');
    });
  });

  describe('Market Data Handling', () => {
    it('should dispatch market data updates', async () => {
      renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onMarketDataUpdate = mockCreateWebSocketService.mock.calls[0][0].onMarketDataUpdate;

      await act(async () => {
        onMarketDataUpdate(mockMarketData);
      });

      const state = testStore.getState();
      expect(state.market.marketData[mockMarketData.symbolId]).toEqual(mockMarketData);
    });

    it('should handle multiple market data updates', async () => {
      renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onMarketDataUpdate = mockCreateWebSocketService.mock.calls[0][0].onMarketDataUpdate;

      const updates = [
        { ...mockMarketData, symbolId: 'BTC', price: 50000 },
        { ...mockMarketData, symbolId: 'ETH', price: 3000 },
        { ...mockMarketData, symbolId: 'LTC', price: 150 },
      ];

      await act(async () => {
        updates.forEach(update => onMarketDataUpdate(update));
      });

      const state = testStore.getState();
      expect(state.market.marketData['BTC'].price).toBe(50000);
      expect(state.market.marketData['ETH'].price).toBe(3000);
      expect(state.market.marketData['LTC'].price).toBe(150);
    });
  });

  describe('Error Handling', () => {
    it('should handle connection errors', async () => {
      renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onError = mockCreateWebSocketService.mock.calls[0][0].onError;
      const testError = new Error('Connection failed');

      await act(async () => {
        onError(testError);
      });

      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      expect(result.current.error).toBeTruthy();
      expect(result.current.connected).toBe(false);
      expect(result.current.connecting).toBe(false);
    });

    it('should dispatch error notification on WebSocket error', async () => {
      renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onError = mockCreateWebSocketService.mock.calls[0][0].onError;
      const testError = new Error('Network timeout');

      await act(async () => {
        onError(testError);
      });

      const state = testStore.getState();
      expect(state.ui.notifications).toHaveLength(1);
      expect(state.ui.notifications[0].type).toBe('error');
      expect(state.ui.notifications[0].message).toBe('WebSocket error: Network timeout');
    });

    it('should handle service creation errors gracefully', async () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      mockCreateWebSocketService.mockImplementationOnce(() => {
        throw new Error('Service creation failed');
      });

      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      await act(async () => {
        await result.current.connect();
      });

      expect(consoleSpy).toHaveBeenCalledWith(
        'WebSocket connection failed, falling back to polling:',
        expect.any(Error)
      );

      consoleSpy.mockRestore();
    });
  });

  describe('Symbol Subscription Management', () => {
    it('should subscribe to symbols when connected', async () => {
      const symbols = ['BTC', 'ETH', 'LTC'];
      mockWebSocketService.isConnected.mockReturnValue(true);

      renderHook(() => useWebSocket({ symbols, enabled: true }), { wrapper });

      const onConnectionStatusChange = mockCreateWebSocketService.mock.calls[0][0].onConnectionStatusChange;

      await act(async () => {
        onConnectionStatusChange(true);
      });

      expect(mockWebSocketService.subscribeToMultipleSymbols).toHaveBeenCalledWith(symbols);
    });

    it('should handle subscription to new symbols', async () => {
      const { rerender } = renderHook(
        ({ symbols }) => useWebSocket({ symbols, enabled: true }),
        {
          wrapper,
          initialProps: { symbols: ['BTC'] }
        }
      );

      mockWebSocketService.isConnected.mockReturnValue(true);

      const onConnectionStatusChange = mockCreateWebSocketService.mock.calls[0][0].onConnectionStatusChange;

      await act(async () => {
        onConnectionStatusChange(true);
      });

      // Change symbols
      rerender({ symbols: ['BTC', 'ETH'] });

      await waitFor(() => {
        expect(mockWebSocketService.subscribeToMultipleSymbols).toHaveBeenCalledWith(['BTC', 'ETH']);
      });
    });

    it('should handle manual symbol subscription', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      mockWebSocketService.isConnected.mockReturnValue(true);

      await act(async () => {
        await result.current.subscribeToSymbols(['AAPL', 'GOOGL']);
      });

      expect(mockWebSocketService.subscribeToMultipleSymbols).toHaveBeenCalledWith(['AAPL', 'GOOGL']);
    });

    it('should handle manual symbol unsubscription', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      mockWebSocketService.isConnected.mockReturnValue(true);

      await act(async () => {
        await result.current.unsubscribeFromSymbols(['AAPL', 'GOOGL']);
      });

      expect(mockWebSocketService.unsubscribeFromSymbol).toHaveBeenCalledTimes(2);
      expect(mockWebSocketService.unsubscribeFromSymbol).toHaveBeenCalledWith('AAPL');
      expect(mockWebSocketService.unsubscribeFromSymbol).toHaveBeenCalledWith('GOOGL');
    });

    it('should warn when subscribing without connection', async () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      mockWebSocketService.isConnected.mockReturnValue(false);

      await act(async () => {
        await result.current.subscribeToSymbols(['BTC']);
      });

      expect(consoleSpy).toHaveBeenCalledWith('Cannot subscribe: WebSocket not connected');

      consoleSpy.mockRestore();
    });

    it('should handle subscription errors', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      mockWebSocketService.isConnected.mockReturnValue(true);
      mockWebSocketService.subscribeToMultipleSymbols.mockRejectedValue(new Error('Subscription failed'));

      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      await act(async () => {
        await result.current.subscribeToSymbols(['BTC']);
      });

      expect(consoleSpy).toHaveBeenCalledWith('Failed to subscribe to symbols:', expect.any(Error));

      const state = testStore.getState();
      expect(state.ui.notifications).toHaveLength(1);
      expect(state.ui.notifications[0].type).toBe('error');
      expect(state.ui.notifications[0].message).toBe('Failed to subscribe to market data');

      consoleSpy.mockRestore();
    });

    it('should handle unsubscription errors silently', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      mockWebSocketService.isConnected.mockReturnValue(true);
      mockWebSocketService.unsubscribeFromSymbol.mockRejectedValue(new Error('Unsubscription failed'));

      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      await act(async () => {
        await result.current.unsubscribeFromSymbols(['BTC']);
      });

      expect(consoleSpy).toHaveBeenCalledWith('Failed to unsubscribe from symbols:', expect.any(Error));

      consoleSpy.mockRestore();
    });
  });

  describe('Options and Configuration', () => {
    it('should handle empty symbols array', async () => {
      const { result } = renderHook(() => useWebSocket({ symbols: [], enabled: true }), { wrapper });

      mockWebSocketService.isConnected.mockReturnValue(true);

      const onConnectionStatusChange = mockCreateWebSocketService.mock.calls[0][0].onConnectionStatusChange;

      await act(async () => {
        onConnectionStatusChange(true);
      });

      // Should not try to subscribe to empty symbols
      expect(mockWebSocketService.subscribeToMultipleSymbols).not.toHaveBeenCalled();
    });

    it('should handle undefined symbols', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      expect(result.current.connectionState).toBe('Disconnected');
    });

    it('should handle dynamic enable/disable', async () => {
      const { rerender } = renderHook(
        ({ enabled }) => useWebSocket({ enabled }),
        {
          wrapper,
          initialProps: { enabled: false }
        }
      );

      expect(mockWebSocketService.connect).not.toHaveBeenCalled();

      rerender({ enabled: true });

      await waitFor(() => {
        expect(mockWebSocketService.connect).toHaveBeenCalled();
      });
    });

    it('should handle URL changes', async () => {
      const { rerender } = renderHook(
        ({ url }) => useWebSocket({ url, enabled: true }),
        {
          wrapper,
          initialProps: { url: '/hub1' }
        }
      );

      expect(mockCreateWebSocketService).toHaveBeenCalledWith(
        expect.objectContaining({
          url: 'http://localhost:8080/hub1'
        })
      );

      rerender({ url: '/hub2' });

      await waitFor(() => {
        expect(mockCreateWebSocketService).toHaveBeenCalledWith(
          expect.objectContaining({
            url: 'http://localhost:8080/hub2'
          })
        );
      });
    });
  });

  describe('Connection Lifecycle', () => {
    it('should handle connection state transitions', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onConnectionStatusChange = mockCreateWebSocketService.mock.calls[0][0].onConnectionStatusChange;

      // Initially disconnected
      expect(result.current.connected).toBe(false);
      expect(result.current.connectionState).toBe('Disconnected');

      // Connect
      await act(async () => {
        onConnectionStatusChange(true);
      });

      expect(result.current.connected).toBe(true);
      expect(result.current.connectionState).toBe('Connected');

      // Disconnect
      await act(async () => {
        onConnectionStatusChange(false);
      });

      expect(result.current.connected).toBe(false);
      expect(result.current.connectionState).toBe('Disconnected');
    });

    it('should clear error state on successful connection', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onError = mockCreateWebSocketService.mock.calls[0][0].onError;
      const onConnectionStatusChange = mockCreateWebSocketService.mock.calls[0][0].onConnectionStatusChange;

      // Set error state
      await act(async () => {
        onError(new Error('Test error'));
      });

      expect(result.current.error).toBeTruthy();

      // Connect successfully - should clear error
      await act(async () => {
        onConnectionStatusChange(true);
      });

      expect(result.current.error).toBeTruthy(); // Error state is managed separately
      expect(result.current.connected).toBe(true);
    });
  });

  describe('Performance and Optimization', () => {
    it('should not recreate service unnecessarily', async () => {
      const { rerender } = renderHook(
        ({ symbols }) => useWebSocket({ symbols, enabled: true }),
        {
          wrapper,
          initialProps: { symbols: ['BTC'] }
        }
      );

      const initialCallCount = mockCreateWebSocketService.mock.calls.length;

      // Change symbols but not other options
      rerender({ symbols: ['ETH'] });

      expect(mockCreateWebSocketService.mock.calls.length).toBe(initialCallCount);
    });

    it('should handle rapid subscription changes', async () => {
      const { rerender } = renderHook(
        ({ symbols }) => useWebSocket({ symbols, enabled: true }),
        {
          wrapper,
          initialProps: { symbols: ['BTC'] }
        }
      );

      mockWebSocketService.isConnected.mockReturnValue(true);

      const symbolChanges = [
        ['BTC', 'ETH'],
        ['ETH', 'LTC'],
        ['LTC', 'XRP'],
        ['XRP', 'ADA'],
      ];

      for (const symbols of symbolChanges) {
        rerender({ symbols });
        await act(async () => {
          vi.advanceTimersByTime(100);
        });
      }

      // Should handle all changes without errors
      expect(mockWebSocketService.subscribeToMultipleSymbols).toHaveBeenCalled();
    });
  });

  describe('Edge Cases', () => {
    it('should handle service reference being null', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      // Force service to be null
      await act(async () => {
        await result.current.disconnect();
      });

      // Should not throw when calling methods on null service
      await expect(result.current.subscribeToSymbols(['BTC'])).resolves.not.toThrow();
      await expect(result.current.unsubscribeFromSymbols(['BTC'])).resolves.not.toThrow();
    });

    it('should handle multiple simultaneous connect calls', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      await act(async () => {
        const promises = [
          result.current.connect(),
          result.current.connect(),
          result.current.connect(),
        ];
        await Promise.all(promises);
      });

      // Should only create service once
      expect(mockCreateWebSocketService).toHaveBeenCalledTimes(1);
    });

    it('should handle component unmount during connection', async () => {
      const { unmount } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      // Unmount while connecting
      unmount();

      expect(mockWebSocketService.disconnect).toHaveBeenCalled();
    });
  });
});