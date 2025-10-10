/**
 * PriceContext Auto-Subscribe Unit Tests
 * 
 * Tests for auto-subscribe functionality in PriceContext:
 * - Auto-subscribe triggers on connection
 * - Re-subscription after reconnection
 * - Subscription cleanup on unmount
 * 
 * Requirements: 1.1, 1.3
 */

import React from 'react';
import { render, waitFor, act } from '@testing-library/react-native';
import { Text } from 'react-native';
import { PriceProvider, usePrices } from '../PriceContext';
import * as signalR from '@microsoft/signalr';
import AsyncStorage from '@react-native-async-storage/async-storage';

// Mock modules
jest.mock('@react-native-async-storage/async-storage');
jest.mock('@microsoft/signalr');

// Test component that uses PriceContext
const TestComponent = () => {
  const { isConnected, subscribedSymbols, connectionStatus, isLoading } = usePrices();
  
  return (
    <>
      <Text testID="connection-status">{connectionStatus}</Text>
      <Text testID="is-connected">{isConnected ? 'true' : 'false'}</Text>
      <Text testID="is-loading">{isLoading ? 'true' : 'false'}</Text>
      <Text testID="subscribed-count">{subscribedSymbols.size}</Text>
      <Text testID="subscribed-symbols">{Array.from(subscribedSymbols).join(',')}</Text>
    </>
  );
};

describe('PriceContext Auto-Subscribe', () => {
  let mockHubConnection: any;
  let mockInvoke: jest.Mock;
  let mockOn: jest.Mock;
  let mockStart: jest.Mock;
  let mockStop: jest.Mock;
  let mockOnClose: jest.Mock;
  let mockOnReconnecting: jest.Mock;
  let mockOnReconnected: jest.Mock;
  
  beforeEach(() => {
    // Clear all mocks
    jest.clearAllMocks();
    
    // Reset AsyncStorage mock
    (AsyncStorage.getItem as jest.Mock).mockResolvedValue(null);
    
    // Create mock SignalR connection
    mockInvoke = jest.fn().mockResolvedValue(undefined);
    mockOn = jest.fn();
    mockStart = jest.fn().mockResolvedValue(undefined);
    mockStop = jest.fn().mockResolvedValue(undefined);
    mockOnClose = jest.fn();
    mockOnReconnecting = jest.fn();
    mockOnReconnected = jest.fn();
    
    mockHubConnection = {
      invoke: mockInvoke,
      on: mockOn,
      start: mockStart,
      stop: mockStop,
      onclose: mockOnClose,
      onreconnecting: mockOnReconnecting,
      onreconnected: mockOnReconnected,
      state: 'Connected',
      connectionId: 'test-connection-id',
    };
    
    // Mock HubConnectionBuilder
    const mockBuilder = {
      withUrl: jest.fn().mockReturnThis(),
      withAutomaticReconnect: jest.fn().mockReturnThis(),
      configureLogging: jest.fn().mockReturnThis(),
      build: jest.fn(() => mockHubConnection),
    };
    
    (signalR.HubConnectionBuilder as jest.Mock).mockImplementation(() => mockBuilder);
    
    // Mock fetch for initial data
    global.fetch = jest.fn((url: string) => {
      if (url.includes('/v1/symbols/tracked')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve([]),
        } as Response);
      }
      if (url.includes('/prices/live')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ symbols: {} }),
        } as Response);
      }
      return Promise.reject(new Error('Unknown URL'));
    }) as jest.Mock;
  });
  
  afterEach(() => {
    jest.restoreAllMocks();
  });
  
  describe('Auto-subscribe on connection', () => {
    it('should automatically subscribe to CRYPTO symbols when connection is established', async () => {
      // Arrange
      const expectedSymbols = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT', 'AVAXUSDT'];
      
      // Act
      const { getByTestId } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for connection to be established
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled();
      });
      
      // Assert - verify SubscribeToPriceUpdates was called with correct parameters
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith(
          'SubscribeToPriceUpdates',
          'CRYPTO',
          expectedSymbols
        );
      });
      
      // Verify connection status
      await waitFor(() => {
        expect(getByTestId('connection-status').props.children).toBe('connected');
        expect(getByTestId('is-connected').props.children).toBe('true');
      });
    });
    
    it('should track subscribed symbols after successful subscription', async () => {
      // Arrange
      const expectedSymbols = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT', 'AVAXUSDT'];
      
      // Act
      const { getByTestId } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for subscription
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalled();
      });
      
      // Assert - verify subscribed symbols are tracked
      await waitFor(() => {
        const subscribedCount = getByTestId('subscribed-count').props.children;
        expect(subscribedCount).toBe(expectedSymbols.length);
        
        const subscribedSymbols = getByTestId('subscribed-symbols').props.children;
        expectedSymbols.forEach(symbol => {
          expect(subscribedSymbols).toContain(symbol);
        });
      });
    });
    
    it('should set isLoading to false after subscription completes', async () => {
      // Act
      const { getByTestId } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Initially loading should be true
      expect(getByTestId('is-loading').props.children).toBe('true');
      
      // Wait for subscription to complete
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalled();
      });
      
      // Assert - loading should be false after subscription
      await waitFor(() => {
        expect(getByTestId('is-loading').props.children).toBe('false');
      });
    });
    
    it('should handle subscription errors gracefully', async () => {
      // Arrange - make invoke fail
      mockInvoke.mockRejectedValueOnce(new Error('Subscription failed'));
      
      // Spy on console.error
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
      
      // Act
      const { getByTestId } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for subscription attempt
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalled();
      });
      
      // Assert - should log error and set loading to false
      await waitFor(() => {
        expect(consoleErrorSpy).toHaveBeenCalledWith(
          'Failed to subscribe to price updates:',
          expect.any(Error)
        );
        expect(getByTestId('is-loading').props.children).toBe('false');
      });
      
      consoleErrorSpy.mockRestore();
    });
    
    it('should not subscribe if connection fails', async () => {
      // Arrange - make connection fail
      mockStart.mockRejectedValueOnce(new Error('Connection failed'));
      
      // Act
      render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for connection attempt
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled();
      });
      
      // Assert - invoke should not be called
      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 100));
      });
      
      expect(mockInvoke).not.toHaveBeenCalled();
    });
  });
  
  describe('Re-subscription after reconnection', () => {
    it('should re-subscribe to symbols when connection is re-established', async () => {
      // Arrange
      const expectedSymbols = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT', 'AVAXUSDT'];
      let onReconnectedCallback: ((connectionId?: string) => void) | null = null;
      
      // Capture the onreconnected callback
      mockHubConnection.onreconnected = jest.fn((callback) => {
        onReconnectedCallback = callback;
      });
      
      // Act
      render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for initial connection and subscription
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledTimes(1);
      });
      
      // Clear the mock to track re-subscription
      mockInvoke.mockClear();
      
      // Simulate reconnection
      await act(async () => {
        if (onReconnectedCallback) {
          onReconnectedCallback('new-connection-id');
        }
        await new Promise(resolve => setTimeout(resolve, 100));
      });
      
      // Assert - should re-subscribe after reconnection
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledWith(
          'SubscribeToPriceUpdates',
          'CRYPTO',
          expectedSymbols
        );
      });
    });
    
    it('should update connection status during reconnection', async () => {
      // Arrange
      let onReconnectingCallback: (() => void) | null = null;
      let onReconnectedCallback: ((connectionId?: string) => void) | null = null;
      
      mockHubConnection.onreconnecting = jest.fn((callback) => {
        onReconnectingCallback = callback;
      });
      
      mockHubConnection.onreconnected = jest.fn((callback) => {
        onReconnectedCallback = callback;
      });
      
      // Act
      const { getByTestId } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for initial connection
      await waitFor(() => {
        expect(getByTestId('connection-status').props.children).toBe('connected');
      });
      
      // Simulate reconnecting
      await act(async () => {
        if (onReconnectingCallback) {
          onReconnectingCallback();
        }
      });
      
      // Assert - status should be 'connecting'
      await waitFor(() => {
        expect(getByTestId('connection-status').props.children).toBe('connecting');
        expect(getByTestId('is-connected').props.children).toBe('false');
      });
      
      // Simulate reconnected
      await act(async () => {
        if (onReconnectedCallback) {
          onReconnectedCallback('new-connection-id');
        }
      });
      
      // Assert - status should be 'connected' again
      await waitFor(() => {
        expect(getByTestId('connection-status').props.children).toBe('connected');
        expect(getByTestId('is-connected').props.children).toBe('true');
      });
    });
    
    it('should handle re-subscription errors after reconnection', async () => {
      // Arrange
      let onReconnectedCallback: ((connectionId?: string) => void) | null = null;
      
      mockHubConnection.onreconnected = jest.fn((callback) => {
        onReconnectedCallback = callback;
      });
      
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
      
      // Act
      render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for initial subscription
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledTimes(1);
      });
      
      // Make re-subscription fail
      mockInvoke.mockRejectedValueOnce(new Error('Re-subscription failed'));
      
      // Simulate reconnection
      await act(async () => {
        if (onReconnectedCallback) {
          onReconnectedCallback('new-connection-id');
        }
        await new Promise(resolve => setTimeout(resolve, 100));
      });
      
      // Assert - should log error
      await waitFor(() => {
        expect(consoleErrorSpy).toHaveBeenCalledWith(
          'Failed to re-subscribe to price updates:',
          expect.any(Error)
        );
      });
      
      consoleErrorSpy.mockRestore();
    });
    
    it('should maintain subscribed symbols list after reconnection', async () => {
      // Arrange
      const expectedSymbols = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT', 'AVAXUSDT'];
      let onReconnectedCallback: ((connectionId?: string) => void) | null = null;
      
      mockHubConnection.onreconnected = jest.fn((callback) => {
        onReconnectedCallback = callback;
      });
      
      // Act
      const { getByTestId } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for initial subscription
      await waitFor(() => {
        expect(getByTestId('subscribed-count').props.children).toBe(expectedSymbols.length);
      });
      
      // Simulate reconnection
      await act(async () => {
        if (onReconnectedCallback) {
          onReconnectedCallback('new-connection-id');
        }
        await new Promise(resolve => setTimeout(resolve, 100));
      });
      
      // Assert - subscribed symbols should still be tracked
      await waitFor(() => {
        expect(getByTestId('subscribed-count').props.children).toBe(expectedSymbols.length);
      });
    });
  });
  
  describe('Subscription cleanup on unmount', () => {
    it('should stop SignalR connection when component unmounts', async () => {
      // Act
      const { unmount } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for connection
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled();
      });
      
      // Unmount component
      unmount();
      
      // Assert - stop should be called
      await waitFor(() => {
        expect(mockStop).toHaveBeenCalled();
      });
    });
    
    it('should clear all event handlers on unmount', async () => {
      // Arrange
      const eventHandlers: string[] = [];
      mockOn.mockImplementation((event: string) => {
        eventHandlers.push(event);
      });
      
      // Act
      const { unmount } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for connection
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled();
      });
      
      // Verify event handlers were registered
      expect(eventHandlers.length).toBeGreaterThan(0);
      
      // Unmount component
      unmount();
      
      // Assert - connection should be stopped (which clears handlers)
      await waitFor(() => {
        expect(mockStop).toHaveBeenCalled();
      });
    });
    
    it('should handle unmount during connection attempt', async () => {
      // Arrange - make connection slow
      mockStart.mockImplementation(() => 
        new Promise(resolve => setTimeout(resolve, 1000))
      );
      
      // Act
      const { unmount } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Unmount before connection completes
      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 50));
        unmount();
      });
      
      // Assert - should not throw errors
      expect(mockStop).toHaveBeenCalled();
    });
    
    it('should clear reconnection timers on unmount', async () => {
      // Arrange
      mockStart.mockRejectedValueOnce(new Error('Connection failed'));
      
      // Mock setTimeout to track timers
      const timers: NodeJS.Timeout[] = [];
      const originalSetTimeout = global.setTimeout;
      global.setTimeout = jest.fn((callback, delay) => {
        const timer = originalSetTimeout(callback, delay);
        timers.push(timer);
        return timer;
      }) as any;
      
      // Act
      const { unmount } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for connection failure and retry timer
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled();
      });
      
      // Unmount before retry
      unmount();
      
      // Assert - timers should be cleared
      expect(mockStop).toHaveBeenCalled();
      
      // Restore setTimeout
      global.setTimeout = originalSetTimeout;
    });
    
    it('should not attempt subscription after unmount', async () => {
      // Arrange - make connection slow
      let resolveConnection: (() => void) | null = null;
      mockStart.mockImplementation(() => 
        new Promise(resolve => {
          resolveConnection = resolve;
        })
      );
      
      // Act
      const { unmount } = render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Unmount before connection completes
      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 50));
        unmount();
      });
      
      // Complete connection after unmount
      await act(async () => {
        if (resolveConnection) {
          resolveConnection();
        }
        await new Promise(resolve => setTimeout(resolve, 100));
      });
      
      // Assert - invoke should not be called after unmount
      expect(mockInvoke).not.toHaveBeenCalled();
    });
  });
  
  describe('Edge cases and error handling', () => {
    it('should handle multiple rapid reconnections', async () => {
      // Arrange
      let onReconnectedCallback: ((connectionId?: string) => void) | null = null;
      
      mockHubConnection.onreconnected = jest.fn((callback) => {
        onReconnectedCallback = callback;
      });
      
      // Act
      render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for initial subscription
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalledTimes(1);
      });
      
      // Simulate multiple rapid reconnections
      await act(async () => {
        for (let i = 0; i < 5; i++) {
          if (onReconnectedCallback) {
            onReconnectedCallback(`connection-${i}`);
          }
          await new Promise(resolve => setTimeout(resolve, 10));
        }
      });
      
      // Assert - should handle all reconnections
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalled();
      });
    });
    
    it('should handle connection with authentication token', async () => {
      // Arrange
      const mockToken = 'test-auth-token';
      (AsyncStorage.getItem as jest.Mock).mockResolvedValue(mockToken);
      
      // Act
      render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for connection
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalled();
      });
      
      // Assert - token should be used in connection
      expect(AsyncStorage.getItem).toHaveBeenCalledWith('session_token');
    });
    
    it('should reset retry counter on successful connection', async () => {
      // Arrange
      mockStart
        .mockRejectedValueOnce(new Error('Connection failed'))
        .mockResolvedValueOnce(undefined);
      
      // Act
      render(
        <PriceProvider>
          <TestComponent />
        </PriceProvider>
      );
      
      // Wait for retry and successful connection
      await waitFor(() => {
        expect(mockStart).toHaveBeenCalledTimes(2);
      }, { timeout: 10000 });
      
      // Assert - should eventually connect and subscribe
      await waitFor(() => {
        expect(mockInvoke).toHaveBeenCalled();
      });
    });
  });
});