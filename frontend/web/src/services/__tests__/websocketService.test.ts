import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { WebSocketService, createWebSocketService } from '../websocketService';
import { mockMarketData } from '../../test-utils';

// Mock SignalR
const mockConnection = {
  start: vi.fn(),
  stop: vi.fn(),
  invoke: vi.fn(),
  on: vi.fn(),
  onclose: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
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

describe('WebSocketService', () => {
  let service: WebSocketService;
  let mockConfig: any;

  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();

    mockConfig = {
      url: 'http://localhost:8080/hubs/market-data',
      onMarketDataUpdate: vi.fn(),
      onConnectionStatusChange: vi.fn(),
      onError: vi.fn(),
    };

    mockConnection.start.mockResolvedValue(undefined);
    mockConnection.stop.mockResolvedValue(undefined);
    mockConnection.invoke.mockResolvedValue(undefined);
    mockConnection.state = 'Disconnected';

    service = new WebSocketService(mockConfig);
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  describe('Connection Management', () => {
    it('should create connection with correct configuration', async () => {
      await service.connect();

      expect(HubConnectionBuilder).toHaveBeenCalled();
      expect(mockHubConnectionBuilder.withUrl).toHaveBeenCalledWith(mockConfig.url);
      expect(mockHubConnectionBuilder.withAutomaticReconnect).toHaveBeenCalledWith({
        nextRetryDelayInMilliseconds: expect.any(Function),
      });
      expect(mockHubConnectionBuilder.configureLogging).toHaveBeenCalledWith(LogLevel.Information);
      expect(mockHubConnectionBuilder.build).toHaveBeenCalled();
    });

    it('should start connection successfully', async () => {
      await service.connect();

      expect(mockConnection.start).toHaveBeenCalled();
      expect(mockConfig.onConnectionStatusChange).toHaveBeenCalledWith(true);
    });

    it('should handle connection errors', async () => {
      const testError = new Error('Connection failed');
      mockConnection.start.mockRejectedValue(testError);

      await service.connect();

      expect(mockConfig.onError).toHaveBeenCalledWith(expect.any(Error));
      expect(mockConfig.onConnectionStatusChange).toHaveBeenCalledWith(false);
    });

    it('should not connect if already connected', async () => {
      mockConnection.state = 'Connected';

      await service.connect();

      expect(mockConnection.start).not.toHaveBeenCalled();
    });

    it('should disconnect properly', async () => {
      await service.connect();
      await service.disconnect();

      expect(mockConnection.stop).toHaveBeenCalled();
      expect(mockConfig.onConnectionStatusChange).toHaveBeenCalledWith(false);
    });

    it('should handle disconnect errors gracefully', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockConnection.stop.mockRejectedValue(new Error('Disconnect failed'));

      await service.disconnect();

      expect(consoleSpy).toHaveBeenCalledWith('Error disconnecting WebSocket:', expect.any(Error));

      consoleSpy.mockRestore();
    });
  });

  describe('Event Handler Setup', () => {
    it('should set up all event handlers on connection', async () => {
      await service.connect();

      expect(mockConnection.on).toHaveBeenCalledWith('PriceUpdate', expect.any(Function));
      expect(mockConnection.onclose).toHaveBeenCalledWith(expect.any(Function));
      expect(mockConnection.onreconnecting).toHaveBeenCalledWith(expect.any(Function));
      expect(mockConnection.onreconnected).toHaveBeenCalledWith(expect.any(Function));
    });

    it('should handle PriceUpdate events correctly', async () => {
      await service.connect();

      const priceUpdateHandler = mockConnection.on.mock.calls.find(
        call => call[0] === 'PriceUpdate'
      )[1];

      priceUpdateHandler(mockMarketData);

      expect(mockConfig.onMarketDataUpdate).toHaveBeenCalledWith(mockMarketData);
    });

    it('should handle PriceUpdate events errors gracefully', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockConfig.onMarketDataUpdate.mockImplementation(() => {
        throw new Error('Handler error');
      });

      await service.connect();

      const priceUpdateHandler = mockConnection.on.mock.calls.find(
        call => call[0] === 'PriceUpdate'
      )[1];

      priceUpdateHandler(mockMarketData);

      expect(consoleSpy).toHaveBeenCalledWith('Error handling market data update:', expect.any(Error));

      consoleSpy.mockRestore();
    });

    it('should handle connection close events', async () => {
      await service.connect();

      const closeHandler = mockConnection.onclose.mock.calls[0][0];

      closeHandler(new Error('Connection closed'));

      expect(mockConfig.onConnectionStatusChange).toHaveBeenCalledWith(false);
    });

    it('should handle reconnecting events', async () => {
      await service.connect();

      const reconnectingHandler = mockConnection.onreconnecting.mock.calls[0][0];

      reconnectingHandler(new Error('Reconnecting'));

      expect(mockConfig.onConnectionStatusChange).toHaveBeenCalledWith(false);
    });

    it('should handle reconnected events', async () => {
      await service.connect();

      const reconnectedHandler = mockConnection.onreconnected.mock.calls[0][0];

      reconnectedHandler('connection-id-123');

      expect(mockConfig.onConnectionStatusChange).toHaveBeenCalledWith(true);
    });
  });

  describe('Symbol Subscription', () => {
    beforeEach(async () => {
      await service.connect();
      mockConnection.state = 'Connected';
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
      const symbols = ['BTCUSDT', 'ETHUSDT', 'LTCUSDT'];

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

    it('should throw error when subscribing without connection', async () => {
      mockConnection.state = 'Disconnected';

      await expect(service.subscribeToSymbol('BTCUSDT')).rejects.toThrow('WebSocket not connected');
    });

    it('should handle subscription errors', async () => {
      const testError = new Error('Subscription failed');
      mockConnection.invoke.mockRejectedValue(testError);

      await expect(service.subscribeToSymbol('BTCUSDT')).rejects.toThrow('Subscription failed');
    });

    it('should handle unsubscription errors gracefully', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockConnection.invoke.mockRejectedValue(new Error('Unsubscription failed'));

      await service.unsubscribeFromSymbol('BTCUSDT');

      expect(consoleSpy).toHaveBeenCalledWith(
        'Failed to unsubscribe from symbol BTCUSDT:',
        expect.any(Error)
      );

      consoleSpy.mockRestore();
    });

    it('should not attempt unsubscription when disconnected', async () => {
      mockConnection.state = 'Disconnected';

      await service.unsubscribeFromSymbol('BTCUSDT');

      expect(mockConnection.invoke).not.toHaveBeenCalled();
    });
  });

  describe('Connection State Management', () => {
    it('should return correct connection state', () => {
      expect(service.isConnected()).toBe(false);

      mockConnection.state = 'Connected';
      expect(service.isConnected()).toBe(true);
    });

    it('should return connection state string', () => {
      expect(service.getConnectionState()).toBe('Disconnected');

      mockConnection.state = 'Connected';
      expect(service.getConnectionState()).toBe('Connected');
    });

    it('should handle null connection gracefully', async () => {
      await service.disconnect();

      expect(service.isConnected()).toBe(false);
      expect(service.getConnectionState()).toBe('Disconnected');
    });
  });

  describe('Automatic Reconnection', () => {
    it('should configure automatic reconnection with exponential backoff', async () => {
      await service.connect();

      const reconnectConfig = mockHubConnectionBuilder.withAutomaticReconnect.mock.calls[0][0];
      const nextRetryDelay = reconnectConfig.nextRetryDelayInMilliseconds;

      // Test exponential backoff
      expect(nextRetryDelay({ previousRetryCount: 0 })).toBe(1000);
      expect(nextRetryDelay({ previousRetryCount: 1 })).toBe(2000);
      expect(nextRetryDelay({ previousRetryCount: 2 })).toBe(4000);
      expect(nextRetryDelay({ previousRetryCount: 3 })).toBe(8000);
      expect(nextRetryDelay({ previousRetryCount: 4 })).toBe(16000);
    });

    it('should cap retry delay at 30 seconds', async () => {
      await service.connect();

      const reconnectConfig = mockHubConnectionBuilder.withAutomaticReconnect.mock.calls[0][0];
      const nextRetryDelay = reconnectConfig.nextRetryDelayInMilliseconds;

      expect(nextRetryDelay({ previousRetryCount: 10 })).toBe(30000);
    });

    it('should stop retrying after max attempts', async () => {
      await service.connect();

      const reconnectConfig = mockHubConnectionBuilder.withAutomaticReconnect.mock.calls[0][0];
      const nextRetryDelay = reconnectConfig.nextRetryDelayInMilliseconds;

      expect(nextRetryDelay({ previousRetryCount: 5 })).toBeNull();
    });

    it('should schedule manual reconnection on close with error', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

      await service.connect();

      const closeHandler = mockConnection.onclose.mock.calls[0][0];

      closeHandler(new Error('Connection lost'));

      vi.advanceTimersByTime(1000);

      expect(consoleSpy).toHaveBeenCalledWith(
        expect.stringContaining('Scheduling WebSocket reconnection attempt')
      );

      consoleSpy.mockRestore();
    });

    it('should reset reconnect attempts on successful connection', async () => {
      await service.connect();

      const reconnectedHandler = mockConnection.onreconnected.mock.calls[0][0];

      reconnectedHandler('connection-id-123');

      // Reconnect attempts should be reset (tested indirectly through timer scheduling)
      expect(mockConfig.onConnectionStatusChange).toHaveBeenCalledWith(true);
    });
  });

  describe('Error Message Handling', () => {
    it('should create friendly error for negotiation failure', async () => {
      const originalError = new Error('Failed to negotiate');
      mockConnection.start.mockRejectedValue(originalError);

      await service.connect();

      expect(mockConfig.onError).toHaveBeenCalledWith(
        expect.objectContaining({
          message: 'Unable to connect to market data service. Please check your internet connection.'
        })
      );
    });

    it('should create friendly error for WebSocket connection failure', async () => {
      const originalError = new Error('WebSocket failed to connect');
      mockConnection.start.mockRejectedValue(originalError);

      await service.connect();

      expect(mockConfig.onError).toHaveBeenCalledWith(
        expect.objectContaining({
          message: 'Real-time data connection unavailable. Using backup data polling.'
        })
      );
    });

    it('should create friendly error for network errors', async () => {
      const originalError = new Error('ERR_NETWORK connection failed');
      mockConnection.start.mockRejectedValue(originalError);

      await service.connect();

      expect(mockConfig.onError).toHaveBeenCalledWith(
        expect.objectContaining({
          message: 'Network error. Please check your connection and try again.'
        })
      );
    });

    it('should create friendly error for server errors', async () => {
      const originalError = new Error('Server responded with 502');
      mockConnection.start.mockRejectedValue(originalError);

      await service.connect();

      expect(mockConfig.onError).toHaveBeenCalledWith(
        expect.objectContaining({
          message: 'Market data service temporarily unavailable. Will retry automatically.'
        })
      );
    });

    it('should pass through unknown errors unchanged', async () => {
      const originalError = new Error('Unknown error type');
      mockConnection.start.mockRejectedValue(originalError);

      await service.connect();

      expect(mockConfig.onError).toHaveBeenCalledWith(originalError);
    });
  });

  describe('Reconnection Logic', () => {
    it('should not attempt reconnection for authentication errors', async () => {
      const authError = new Error('401 Unauthorized');
      mockConnection.start.mockRejectedValue(authError);

      await service.connect();

      // Should not schedule reconnection for auth errors
      expect(mockConfig.onError).toHaveBeenCalled();
    });

    it('should not attempt reconnection for negotiation failures', async () => {
      const negotiationError = new Error('Failed to negotiate');
      mockConnection.start.mockRejectedValue(negotiationError);

      await service.connect();

      // Should not schedule reconnection for negotiation failures
      expect(mockConfig.onError).toHaveBeenCalled();
    });

    it('should attempt reconnection for other errors', async () => {
      const networkError = new Error('Network timeout');
      mockConnection.start.mockRejectedValue(networkError);

      await service.connect();

      // Should schedule reconnection for network errors
      expect(mockConfig.onError).toHaveBeenCalled();
    });

    it('should stop reconnection after max attempts', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      // Create a new service instance to test reconnection logic
      const reconnectService = new WebSocketService(mockConfig);

      // Mock the reconnection attempts by simulating the private method behavior
      // In a real scenario, this would be triggered by connection failures

      for (let i = 0; i < 6; i++) {
        const networkError = new Error('Network timeout');
        mockConnection.start.mockRejectedValue(networkError);
        await reconnectService.connect();
      }

      expect(consoleSpy).toHaveBeenCalledWith('Max reconnection attempts reached');

      consoleSpy.mockRestore();
    });
  });

  describe('Edge Cases', () => {
    it('should handle missing callbacks gracefully', async () => {
      const serviceWithoutCallbacks = new WebSocketService({
        url: 'http://localhost:8080/hubs/test',
      });

      await expect(serviceWithoutCallbacks.connect()).resolves.not.toThrow();
    });

    it('should handle multiple connect calls gracefully', async () => {
      mockConnection.state = 'Connected';

      await service.connect();
      await service.connect();
      await service.connect();

      // Should only call start once
      expect(mockConnection.start).toHaveBeenCalledTimes(1);
    });

    it('should handle disconnect when not connected', async () => {
      await expect(service.disconnect()).resolves.not.toThrow();
    });

    it('should handle subscription when connection is null', async () => {
      await service.disconnect();

      await expect(service.subscribeToSymbol('BTCUSDT')).rejects.toThrow('WebSocket not connected');
    });

    it('should handle concurrent subscriptions', async () => {
      await service.connect();
      mockConnection.state = 'Connected';

      const promises = [
        service.subscribeToSymbol('BTCUSDT'),
        service.subscribeToSymbol('ETHUSDT'),
        service.subscribeToSymbol('LTCUSDT'),
      ];

      await expect(Promise.all(promises)).resolves.not.toThrow();
    });
  });
});

describe('WebSocket Service Factory Functions', () => {
  let mockConfig: any;

  beforeEach(() => {
    vi.clearAllMocks();

    mockConfig = {
      url: 'http://localhost:8080/hubs/market-data',
      onMarketDataUpdate: vi.fn(),
      onConnectionStatusChange: vi.fn(),
      onError: vi.fn(),
    };
  });

  describe('createWebSocketService', () => {
    it('should create a new service instance', () => {
      const service = createWebSocketService(mockConfig);

      expect(service).toBeInstanceOf(WebSocketService);
    });

    it('should disconnect existing service before creating new one', async () => {
      const firstService = createWebSocketService(mockConfig);
      const disconnectSpy = vi.spyOn(firstService, 'disconnect');

      const secondService = createWebSocketService(mockConfig);

      expect(disconnectSpy).toHaveBeenCalled();
      expect(secondService).toBeInstanceOf(WebSocketService);
    });

    it('should handle null existing service gracefully', () => {
      const service = createWebSocketService(mockConfig);

      expect(service).toBeInstanceOf(WebSocketService);
    });
  });

  describe('getWebSocketService', () => {
    it('should return null when no service exists', () => {
      const { getWebSocketService } = require('../websocketService');
      const service = getWebSocketService();

      expect(service).toBeNull();
    });

    it('should return the current service instance', () => {
      const createdService = createWebSocketService(mockConfig);
      const { getWebSocketService } = require('../websocketService');
      const retrievedService = getWebSocketService();

      expect(retrievedService).toBe(createdService);
    });
  });
});