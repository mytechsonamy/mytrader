import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { WebSocketService, WebSocketConnectionConfig } from '../websocketService';
import { MarketDataDto } from '../marketDataService';

// Mock SignalR
const mockConnection = {
  start: vi.fn(),
  stop: vi.fn(),
  on: vi.fn(),
  onclose: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
  invoke: vi.fn(),
  state: 'Disconnected',
};

const mockHubConnectionBuilder = {
  withUrl: vi.fn().mockReturnThis(),
  withAutomaticReconnect: vi.fn().mockReturnThis(),
  configureLogging: vi.fn().mockReturnThis(),
  build: vi.fn(() => mockConnection),
};

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn(() => mockHubConnectionBuilder),
  LogLevel: {
    Information: 'Information',
  },
}));

describe('WebSocketService Integration Tests', () => {
  let service: WebSocketService;
  let config: WebSocketConnectionConfig;
  let onMarketDataUpdate: vi.Mock;
  let onConnectionStatusChange: vi.Mock;
  let onError: vi.Mock;

  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();

    onMarketDataUpdate = vi.fn();
    onConnectionStatusChange = vi.fn();
    onError = vi.fn();

    config = {
      url: 'http://localhost:8080/hubs/market-data',
      onMarketDataUpdate,
      onConnectionStatusChange,
      onError,
    };

    // Reset connection state
    mockConnection.state = 'Disconnected';
    mockConnection.start.mockResolvedValue(undefined);
    mockConnection.stop.mockResolvedValue(undefined);
    mockConnection.invoke.mockResolvedValue(undefined);

    service = new WebSocketService(config);
  });

  afterEach(async () => {
    try {
      await service.disconnect();
      service.destroy();
    } catch (error) {
      // Ignore cleanup errors
    }
    vi.useRealTimers();
  });

  describe('Connection Flow', () => {
    it('should successfully establish connection', async () => {
      mockConnection.state = 'Connected';

      await service.connect();

      expect(mockHubConnectionBuilder.withUrl).toHaveBeenCalledWith(config.url);
      expect(mockHubConnectionBuilder.withAutomaticReconnect).toHaveBeenCalled();
      expect(mockConnection.start).toHaveBeenCalled();
      expect(onConnectionStatusChange).toHaveBeenCalledWith(true);
      expect(service.isConnected()).toBe(true);
    });

    it('should handle connection timeout', async () => {
      mockConnection.start.mockImplementation(() => new Promise(() => {})); // Never resolves

      const connectPromise = service.connect();
      vi.advanceTimersByTime(10000); // 10 seconds timeout

      await expect(connectPromise).rejects.toThrow(/Connection timeout/);
      expect(onError).toHaveBeenCalled();
      expect(onConnectionStatusChange).toHaveBeenCalledWith(false);
    });

    it('should handle connection failure with retry', async () => {
      const connectionError = new Error('Connection failed');
      mockConnection.start.mockRejectedValue(connectionError);

      await service.connect();

      expect(onError).toHaveBeenCalledWith(expect.any(Error));
      expect(onConnectionStatusChange).toHaveBeenCalledWith(false);

      // Should schedule reconnection
      vi.advanceTimersByTime(1000);
      expect(mockConnection.start).toHaveBeenCalledTimes(2);
    });

    it('should not reconnect after max attempts', async () => {
      const connectionError = new Error('Connection failed');
      mockConnection.start.mockRejectedValue(connectionError);

      // Simulate multiple failed attempts
      for (let i = 0; i < 15; i++) {
        await service.connect();
        vi.advanceTimersByTime(30000); // Max delay
      }

      // Should stop trying after max attempts
      const callCount = mockConnection.start.mock.calls.length;
      vi.advanceTimersByTime(60000);
      expect(mockConnection.start).toHaveBeenCalledTimes(callCount);
    });

    it('should clean up on disconnect', async () => {
      mockConnection.state = 'Connected';
      await service.connect();

      await service.disconnect();

      expect(mockConnection.stop).toHaveBeenCalled();
      expect(onConnectionStatusChange).toHaveBeenCalledWith(false);
      expect(service.isConnected()).toBe(false);
    });
  });

  describe('Market Data Processing', () => {
    beforeEach(async () => {
      mockConnection.state = 'Connected';
      await service.connect();
    });

    it('should process multi-asset price updates', () => {
      const payload = {
        Symbol: 'BTCUSDT',
        Price: 50000,
        Change24h: 5.25,
        Volume: 1000000,
        Timestamp: '2024-01-01T12:00:00Z',
        Source: 'binance'
      };

      // Get the price update handler
      const onPriceUpdate = mockConnection.on.mock.calls.find(
        call => call[0] === 'PriceUpdate'
      )[1];

      onPriceUpdate(payload);

      expect(onMarketDataUpdate).toHaveBeenCalledWith({
        symbol: 'BTCUSDT',
        displayName: 'BTCUSDT',
        price: 50000,
        priceChange: 0,
        priceChangePercent: 5.25,
        volume: 1000000,
        high24h: undefined,
        low24h: undefined,
        marketCap: undefined,
        lastUpdate: '2024-01-01T12:00:00Z',
        source: 'alpaca',
        isRealTime: true,
      });
    });

    it('should process legacy crypto updates', () => {
      const payload = {
        symbol: 'ETHUSDT',
        price: 3000,
        changePercent: -2.5,
        volume: 500000,
        timestamp: '2024-01-01T12:00:00Z'
      };

      const onPriceUpdate = mockConnection.on.mock.calls.find(
        call => call[0] === 'PriceUpdate'
      )[1];

      onPriceUpdate(payload);

      expect(onMarketDataUpdate).toHaveBeenCalledWith({
        symbol: 'ETHUSDT',
        displayName: 'ETHUSDT',
        price: 3000,
        priceChange: 0,
        priceChangePercent: -2.5,
        volume: 500000,
        high24h: undefined,
        low24h: undefined,
        marketCap: undefined,
        lastUpdate: '2024-01-01T12:00:00Z',
        source: 'alpaca',
        isRealTime: true,
      });
    });

    it('should handle invalid market data gracefully', () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      
      const invalidPayloads = [
        null,
        undefined,
        'not an object',
        { price: 'invalid' },
        { Symbol: '', Price: 100 },
        { symbol: 'BTC', price: -100 },
      ];

      const onPriceUpdate = mockConnection.on.mock.calls.find(
        call => call[0] === 'PriceUpdate'
      )[1];

      invalidPayloads.forEach(payload => {
        onPriceUpdate(payload);
      });

      expect(onMarketDataUpdate).not.toHaveBeenCalled();
      expect(consoleSpy).toHaveBeenCalled();
      
      consoleSpy.mockRestore();
    });

    it('should support both event names for backward compatibility', () => {
      expect(mockConnection.on).toHaveBeenCalledWith('PriceUpdate', expect.any(Function));
      expect(mockConnection.on).toHaveBeenCalledWith('ReceivePriceUpdate', expect.any(Function));
    });
  });

  describe('Symbol Subscription Management', () => {
    beforeEach(async () => {
      mockConnection.state = 'Connected';
      await service.connect();
    });

    it('should subscribe to single symbol', async () => {
      await service.subscribeToSymbol('BTCUSDT');

      expect(mockConnection.invoke).toHaveBeenCalledWith(
        'SubscribeToPriceUpdates',
        'CRYPTO',
        ['BTCUSDT']
      );
    });

    it('should subscribe to multiple symbols', async () => {
      const symbols = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT'];
      
      await service.subscribeToMultipleSymbols(symbols);

      expect(mockConnection.invoke).toHaveBeenCalledWith(
        'SubscribeToPriceUpdates',
        'CRYPTO',
        symbols
      );
    });

    it('should unsubscribe from symbol', async () => {
      await service.unsubscribeFromSymbol('BTCUSDT');

      expect(mockConnection.invoke).toHaveBeenCalledWith(
        'UnsubscribeFromPriceUpdates',
        'CRYPTO',
        ['BTCUSDT']
      );
    });

    it('should handle subscription errors', async () => {
      const subscriptionError = new Error('Subscription failed');
      mockConnection.invoke.mockRejectedValue(subscriptionError);

      await expect(service.subscribeToSymbol('INVALID')).rejects.toThrow('Subscription failed');
    });

    it('should prevent subscription when not connected', async () => {
      mockConnection.state = 'Disconnected';

      await expect(service.subscribeToSymbol('BTCUSDT')).rejects.toThrow('WebSocket not connected');
    });

    it('should handle unsubscription when not connected', async () => {
      mockConnection.state = 'Disconnected';

      // Should not throw error
      await expect(service.unsubscribeFromSymbol('BTCUSDT')).resolves.not.toThrow();
      expect(mockConnection.invoke).not.toHaveBeenCalled();
    });
  });

  describe('Connection State Management', () => {
    beforeEach(async () => {
      mockConnection.state = 'Connected';
      await service.connect();
    });

    it('should handle connection close event', () => {
      const onCloseHandler = mockConnection.onclose.mock.calls[0][0];
      
      onCloseHandler(new Error('Connection lost'));
      
      expect(onConnectionStatusChange).toHaveBeenCalledWith(false);
    });

    it('should handle reconnecting event', () => {
      const onReconnectingHandler = mockConnection.onreconnecting.mock.calls[0][0];
      
      onReconnectingHandler(new Error('Reconnecting'));
      
      expect(onConnectionStatusChange).toHaveBeenCalledWith(false);
    });

    it('should handle reconnected event', () => {
      const onReconnectedHandler = mockConnection.onreconnected.mock.calls[0][0];
      
      onReconnectedHandler('connection-id-123');
      
      expect(onConnectionStatusChange).toHaveBeenCalledWith(true);
    });

    it('should report correct connection state', () => {
      expect(service.getConnectionState()).toBe('Disconnected');
      
      mockConnection.state = 'Connected';
      expect(service.getConnectionState()).toBe('Connected');
      
      mockConnection.state = 'Connecting';
      expect(service.getConnectionState()).toBe('Connecting');
    });
  });

  describe('Heartbeat and Keep-Alive', () => {
    beforeEach(async () => {
      mockConnection.state = 'Connected';
      await service.connect();
    });

    it('should send periodic heartbeat pings', () => {
      expect(mockConnection.invoke).not.toHaveBeenCalledWith('Ping');
      
      // Advance time to trigger heartbeat
      vi.advanceTimersByTime(30000);
      
      expect(mockConnection.invoke).toHaveBeenCalledWith('Ping');
    });

    it('should handle heartbeat failures gracefully', () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      mockConnection.invoke.mockRejectedValue(new Error('Ping failed'));
      
      vi.advanceTimersByTime(30000);
      
      expect(consoleSpy).toHaveBeenCalledWith('Heartbeat ping failed:', expect.any(Error));
      consoleSpy.mockRestore();
    });

    it('should stop heartbeat on disconnect', async () => {
      vi.advanceTimersByTime(30000);
      expect(mockConnection.invoke).toHaveBeenCalledWith('Ping');
      
      await service.disconnect();
      
      // Clear previous calls
      mockConnection.invoke.mockClear();
      
      // Advance time - no more heartbeats should occur
      vi.advanceTimersByTime(60000);
      expect(mockConnection.invoke).not.toHaveBeenCalledWith('Ping');
    });
  });

  describe('Error Handling and Recovery', () => {
    it('should create friendly error messages', async () => {
      const testCases = [
        { error: 'Failed to negotiate', expected: /Unable to connect to market data service/ },
        { error: 'WebSocket failed to connect', expected: /Real-time data connection unavailable/ },
        { error: 'ERR_NETWORK', expected: /Network error/ },
        { error: '502 Bad Gateway', expected: /temporarily unavailable/ },
        { error: 'timeout', expected: /Connection timeout/ },
        { error: 'refused', expected: /Connection refused/ },
      ];
      
      for (const testCase of testCases) {
        mockConnection.start.mockRejectedValue(new Error(testCase.error));
        
        await service.connect();
        
        expect(onError).toHaveBeenCalledWith(expect.objectContaining({
          message: expect.stringMatching(testCase.expected)
        }));
        
        onError.mockClear();
      }
    });

    it('should not attempt reconnection for authentication errors', async () => {
      const authError = new Error('401 Unauthorized');
      mockConnection.start.mockRejectedValue(authError);
      
      await service.connect();
      
      // Should not schedule reconnection
      vi.advanceTimersByTime(5000);
      expect(mockConnection.start).toHaveBeenCalledTimes(1);
    });

    it('should handle service destruction during operations', async () => {
      mockConnection.state = 'Connected';
      await service.connect();
      
      service.destroy();
      
      // Should not attempt further operations
      await service.connect();
      expect(mockConnection.start).toHaveBeenCalledTimes(1);
    });

    it('should clean up existing connection before creating new one', async () => {
      mockConnection.state = 'Connected';
      await service.connect();
      
      const firstStopCall = mockConnection.stop.mock.calls.length;
      
      // Attempt to connect again
      await service.connect();
      
      expect(mockConnection.stop).toHaveBeenCalledTimes(firstStopCall + 1);
    });
  });

  describe('Concurrent Operations', () => {
    it('should handle simultaneous connection attempts', async () => {
      mockConnection.state = 'Connected';
      
      const promises = [
        service.connect(),
        service.connect(),
        service.connect(),
      ];
      
      await Promise.all(promises);
      
      // Should only attempt connection once when already connected
      expect(mockConnection.start).toHaveBeenCalledTimes(1);
    });

    it('should handle subscription while connecting', async () => {
      let resolveConnection: () => void;
      const connectionPromise = new Promise<void>(resolve => {
        resolveConnection = resolve;
      });
      
      mockConnection.start.mockImplementation(() => {
        mockConnection.state = 'Connecting';
        return connectionPromise;
      });
      
      const connectPromise = service.connect();
      
      // Try to subscribe while connecting
      await expect(service.subscribeToSymbol('BTCUSDT')).rejects.toThrow('WebSocket not connected');
      
      // Complete connection
      mockConnection.state = 'Connected';
      resolveConnection!();
      await connectPromise;
      
      // Now subscription should work
      await expect(service.subscribeToSymbol('BTCUSDT')).resolves.not.toThrow();
    });

    it('should handle rapid subscribe/unsubscribe operations', async () => {
      mockConnection.state = 'Connected';
      await service.connect();
      
      const operations = [];
      for (let i = 0; i < 10; i++) {
        operations.push(service.subscribeToSymbol(`SYMBOL${i}`));
        operations.push(service.unsubscribeFromSymbol(`SYMBOL${i - 1}`));
      }
      
      await Promise.allSettled(operations);
      
      // Should handle all operations without throwing
      expect(mockConnection.invoke).toHaveBeenCalled();
    });
  });

  describe('Memory Management', () => {
    it('should clean up timers on destruction', async () => {
      mockConnection.state = 'Connected';
      await service.connect();
      
      const clearIntervalSpy = vi.spyOn(global, 'clearInterval');
      
      service.destroy();
      
      expect(clearIntervalSpy).toHaveBeenCalled();
      clearIntervalSpy.mockRestore();
    });

    it('should not leak event handlers', async () => {
      mockConnection.state = 'Connected';
      await service.connect();
      
      const initialHandlerCount = mockConnection.on.mock.calls.length;
      
      await service.disconnect();
      await service.connect();
      
      // Should not accumulate event handlers
      const finalHandlerCount = mockConnection.on.mock.calls.length;
      expect(finalHandlerCount).toBe(initialHandlerCount * 2); // New connection = new handlers
    });

    it('should handle multiple destroy calls safely', () => {
      expect(() => {
        service.destroy();
        service.destroy();
        service.destroy();
      }).not.toThrow();
    });
  });
});
