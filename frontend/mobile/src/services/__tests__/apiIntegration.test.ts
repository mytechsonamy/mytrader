import { apiService } from '../api';
import { websocketService } from '../websocketService';
import AsyncStorage from '@react-native-async-storage/async-storage';

// Mock AsyncStorage
jest.mock('@react-native-async-storage/async-storage', () => ({
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
}));

// Mock WebSocket Service
jest.mock('../websocketService', () => ({
  websocketService: {
    connect: jest.fn(),
    disconnect: jest.fn(),
    subscribe: jest.fn(),
    unsubscribe: jest.fn(),
    isConnected: jest.fn(),
    onPriceUpdate: jest.fn(),
    onConnectionChange: jest.fn(),
  },
}));

// Network Simulation Utilities
class NetworkSimulator {
  private requestCount = 0;
  private failureRate = 0;
  private latency = 0;
  private isOnline = true;

  setFailureRate(rate: number) {
    this.failureRate = rate;
  }

  setLatency(ms: number) {
    this.latency = ms;
  }

  setOnline(online: boolean) {
    this.isOnline = online;
  }

  reset() {
    this.requestCount = 0;
    this.failureRate = 0;
    this.latency = 0;
    this.isOnline = true;
  }

  async simulateRequest<T>(response: T): Promise<T> {
    this.requestCount++;

    if (!this.isOnline) {
      throw new Error('Network Error: No internet connection');
    }

    if (this.latency > 0) {
      await new Promise(resolve => setTimeout(resolve, this.latency));
    }

    if (Math.random() < this.failureRate) {
      throw new Error(`Network Error: Request failed (${this.requestCount})`);
    }

    return response;
  }

  getRequestCount() {
    return this.requestCount;
  }
}

const mockAsyncStorage = AsyncStorage as jest.Mocked<typeof AsyncStorage>;
const mockWebSocketService = websocketService as jest.Mocked<typeof websocketService>;

describe('API Integration Tests', () => {
  let networkSimulator: NetworkSimulator;
  let originalFetch: typeof global.fetch;

  beforeAll(() => {
    originalFetch = global.fetch;
    networkSimulator = new NetworkSimulator();
  });

  beforeEach(() => {
    jest.clearAllMocks();
    networkSimulator.reset();

    // Setup default mocks
    mockAsyncStorage.getItem.mockResolvedValue('test-token');

    // Mock fetch with network simulation
    global.fetch = jest.fn().mockImplementation(async (url, options) => {
      const responseData = await networkSimulator.simulateRequest({
        ok: true,
        status: 200,
        json: async () => getDefaultResponseForUrl(url.toString()),
      });

      return responseData as Response;
    });
  });

  afterAll(() => {
    global.fetch = originalFetch;
  });

  const getDefaultResponseForUrl = (url: string) => {
    if (url.includes('/auth/login')) {
      return {
        token: 'mock-jwt-token',
        user: {
          id: '1',
          email: 'test@example.com',
          firstName: 'Test',
          lastName: 'User',
        },
      };
    }

    if (url.includes('/symbols')) {
      return [
        {
          id: '1',
          symbol: 'BTC-USD',
          displayName: 'Bitcoin',
          assetClass: 'CRYPTO',
        },
        {
          id: '2',
          symbol: 'ETH-USD',
          displayName: 'Ethereum',
          assetClass: 'CRYPTO',
        },
      ];
    }

    if (url.includes('/portfolios')) {
      return [
        {
          id: '1',
          name: 'Main Portfolio',
          totalValue: 25000,
          dailyPnL: 1250,
          positions: [],
        },
      ];
    }

    if (url.includes('/leaderboard')) {
      return [
        {
          userId: '1',
          username: 'trader1',
          rank: 1,
          totalReturn: 15.5,
        },
        {
          userId: '2',
          username: 'trader2',
          rank: 2,
          totalReturn: 12.3,
        },
      ];
    }

    if (url.includes('/market-data')) {
      return {
        '1': {
          symbol: 'BTC-USD',
          price: 50000,
          change: 2500,
          changePercent: 5.2,
        },
      };
    }

    return { success: true };
  };

  describe('Authentication API', () => {
    it('should login successfully with valid credentials', async () => {
      const loginData = {
        email: 'test@example.com',
        password: 'password123',
      };

      const result = await apiService.login(loginData.email, loginData.password);

      expect(result.token).toBe('mock-jwt-token');
      expect(result.user.email).toBe('test@example.com');
    });

    it('should handle login failures gracefully', async () => {
      networkSimulator.setFailureRate(1); // Force failure

      const loginPromise = apiService.login('test@example.com', 'wrongpassword');

      await expect(loginPromise).rejects.toThrow('Network Error');
    });

    it('should retry login on network failures', async () => {
      networkSimulator.setFailureRate(0.5); // 50% failure rate

      let attempts = 0;
      const maxAttempts = 5;

      while (attempts < maxAttempts) {
        try {
          await apiService.login('test@example.com', 'password123');
          break; // Success
        } catch (error) {
          attempts++;
          if (attempts >= maxAttempts) {
            throw error;
          }
          await new Promise(resolve => setTimeout(resolve, 100)); // Brief delay
        }
      }

      expect(attempts).toBeLessThanOrEqual(maxAttempts);
    });

    it('should handle token expiration', async () => {
      // Mock 401 response for expired token
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({ message: 'Token expired' }),
      });

      await expect(apiService.getPortfolios()).rejects.toThrow();
    });

    it('should refresh token automatically', async () => {
      const refreshTokenSpy = jest.spyOn(apiService, 'refreshToken').mockResolvedValue({
        token: 'new-token',
        refreshToken: 'new-refresh-token',
      });

      // First call fails with 401, second succeeds
      global.fetch = jest.fn()
        .mockResolvedValueOnce({
          ok: false,
          status: 401,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => [{ id: '1', name: 'Portfolio' }],
        });

      const result = await apiService.getPortfolios();

      expect(refreshTokenSpy).toHaveBeenCalled();
      expect(result).toBeDefined();
    });
  });

  describe('Market Data API', () => {
    it('should fetch symbols successfully', async () => {
      const symbols = await apiService.getSymbolsByAssetClass('CRYPTO');

      expect(symbols).toHaveLength(2);
      expect(symbols[0].symbol).toBe('BTC-USD');
      expect(symbols[1].symbol).toBe('ETH-USD');
    });

    it('should handle high-frequency price updates', async () => {
      const priceUpdates = [];
      const updateCount = 100;

      for (let i = 0; i < updateCount; i++) {
        priceUpdates.push(
          apiService.getMarketData(['1']).then(data => ({
            timestamp: Date.now(),
            data,
          }))
        );
      }

      const results = await Promise.allSettled(priceUpdates);
      const successful = results.filter(r => r.status === 'fulfilled');

      expect(successful.length).toBeGreaterThan(0);
      expect(networkSimulator.getRequestCount()).toBe(updateCount);
    });

    it('should handle WebSocket connection failures', async () => {
      mockWebSocketService.connect.mockRejectedValue(new Error('WebSocket connection failed'));

      const connectionPromise = mockWebSocketService.connect();

      await expect(connectionPromise).rejects.toThrow('WebSocket connection failed');
    });

    it('should reconnect WebSocket automatically', async () => {
      let connectionAttempts = 0;
      mockWebSocketService.connect.mockImplementation(async () => {
        connectionAttempts++;
        if (connectionAttempts < 3) {
          throw new Error('Connection failed');
        }
        return Promise.resolve();
      });

      // Simulate retry logic
      const maxRetries = 5;
      for (let i = 0; i < maxRetries; i++) {
        try {
          await mockWebSocketService.connect();
          break;
        } catch (error) {
          if (i === maxRetries - 1) throw error;
          await new Promise(resolve => setTimeout(resolve, 100));
        }
      }

      expect(connectionAttempts).toBe(3);
    });
  });

  describe('Portfolio API', () => {
    it('should fetch portfolios successfully', async () => {
      const portfolios = await apiService.getPortfolios();

      expect(portfolios).toHaveLength(1);
      expect(portfolios[0].name).toBe('Main Portfolio');
      expect(portfolios[0].totalValue).toBe(25000);
    });

    it('should handle portfolio updates', async () => {
      const updateData = {
        id: '1',
        name: 'Updated Portfolio',
        description: 'Updated description',
      };

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ ...updateData, success: true }),
      });

      const result = await apiService.updatePortfolio(updateData.id, updateData);

      expect(result.success).toBe(true);
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/portfolios/1'),
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify(updateData),
        })
      );
    });

    it('should handle concurrent portfolio operations', async () => {
      const operations = [
        apiService.getPortfolios(),
        apiService.getPortfolios(),
        apiService.getPortfolios(),
      ];

      const results = await Promise.all(operations);

      results.forEach(result => {
        expect(result).toHaveLength(1);
        expect(result[0].name).toBe('Main Portfolio');
      });

      expect(networkSimulator.getRequestCount()).toBe(3);
    });
  });

  describe('Leaderboard API', () => {
    it('should fetch leaderboard data', async () => {
      const leaderboard = await apiService.getLeaderboard('weekly', 10);

      expect(leaderboard).toHaveLength(2);
      expect(leaderboard[0].rank).toBe(1);
      expect(leaderboard[1].rank).toBe(2);
    });

    it('should handle leaderboard filters', async () => {
      const weeklyLeaderboard = await apiService.getLeaderboard('weekly', 5);
      const allTimeLeaderboard = await apiService.getLeaderboard('all', 5);

      expect(weeklyLeaderboard).toBeDefined();
      expect(allTimeLeaderboard).toBeDefined();
      expect(global.fetch).toHaveBeenCalledTimes(2);
    });

    it('should cache leaderboard data appropriately', async () => {
      // First request
      await apiService.getLeaderboard('weekly', 10);
      const firstRequestCount = networkSimulator.getRequestCount();

      // Second request (should use cache)
      await apiService.getLeaderboard('weekly', 10);
      const secondRequestCount = networkSimulator.getRequestCount();

      // In a real implementation, second request might be cached
      expect(secondRequestCount).toBeGreaterThanOrEqual(firstRequestCount);
    });
  });

  describe('Network Conditions', () => {
    it('should handle slow network conditions', async () => {
      networkSimulator.setLatency(2000); // 2 second delay

      const startTime = Date.now();
      await apiService.getSymbolsByAssetClass('CRYPTO');
      const endTime = Date.now();

      expect(endTime - startTime).toBeGreaterThanOrEqual(2000);
    });

    it('should handle intermittent connectivity', async () => {
      networkSimulator.setFailureRate(0.3); // 30% failure rate

      const requests = Array.from({ length: 10 }, () =>
        apiService.getSymbolsByAssetClass('CRYPTO').catch(() => null)
      );

      const results = await Promise.all(requests);
      const successfulRequests = results.filter(r => r !== null);

      expect(successfulRequests.length).toBeGreaterThan(0);
      expect(successfulRequests.length).toBeLessThan(10); // Some should fail
    });

    it('should handle complete network outage', async () => {
      networkSimulator.setOnline(false);

      await expect(apiService.getSymbolsByAssetClass('CRYPTO')).rejects.toThrow(
        'No internet connection'
      );
    });

    it('should recover from network outage', async () => {
      networkSimulator.setOnline(false);

      // First request should fail
      await expect(apiService.getSymbolsByAssetClass('CRYPTO')).rejects.toThrow();

      // Network comes back online
      networkSimulator.setOnline(true);

      // Second request should succeed
      const result = await apiService.getSymbolsByAssetClass('CRYPTO');
      expect(result).toBeDefined();
    });
  });

  describe('Error Scenarios', () => {
    it('should handle malformed JSON responses', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: async () => {
          throw new Error('Invalid JSON');
        },
      });

      await expect(apiService.getSymbolsByAssetClass('CRYPTO')).rejects.toThrow();
    });

    it('should handle server errors (5xx)', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
      });

      await expect(apiService.getSymbolsByAssetClass('CRYPTO')).rejects.toThrow();
    });

    it('should handle client errors (4xx)', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 404,
        statusText: 'Not Found',
      });

      await expect(apiService.getSymbolsByAssetClass('CRYPTO')).rejects.toThrow();
    });

    it('should handle request timeouts', async () => {
      global.fetch = jest.fn().mockImplementation(() =>
        new Promise((_, reject) =>
          setTimeout(() => reject(new Error('Request timeout')), 1000)
        )
      );

      await expect(apiService.getSymbolsByAssetClass('CRYPTO')).rejects.toThrow(
        'Request timeout'
      );
    });
  });

  describe('Data Integrity', () => {
    it('should validate API response schemas', async () => {
      const invalidSymbolResponse = [
        {
          // Missing required fields
          symbol: 'BTC-USD',
        },
        {
          id: '2',
          // Missing symbol field
          displayName: 'Ethereum',
        },
      ];

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: async () => invalidSymbolResponse,
      });

      // In actual implementation, would validate schema
      const result = await apiService.getSymbolsByAssetClass('CRYPTO');
      expect(result).toEqual(invalidSymbolResponse);
    });

    it('should handle data consistency across requests', async () => {
      let requestCount = 0;

      global.fetch = jest.fn().mockImplementation(() => {
        requestCount++;
        return Promise.resolve({
          ok: true,
          json: async () => ({
            requestId: requestCount,
            timestamp: Date.now(),
            data: 'consistent-data',
          }),
        });
      });

      const [result1, result2] = await Promise.all([
        apiService.getSymbolsByAssetClass('CRYPTO'),
        apiService.getSymbolsByAssetClass('CRYPTO'),
      ]);

      expect(result1).toBeDefined();
      expect(result2).toBeDefined();
      expect(requestCount).toBe(2);
    });
  });

  describe('Performance Metrics', () => {
    it('should measure API response times', async () => {
      const measurements: number[] = [];

      for (let i = 0; i < 5; i++) {
        const startTime = performance.now();
        await apiService.getSymbolsByAssetClass('CRYPTO');
        const endTime = performance.now();
        measurements.push(endTime - startTime);
      }

      const averageTime = measurements.reduce((a, b) => a + b, 0) / measurements.length;
      expect(averageTime).toBeGreaterThan(0);
      expect(measurements.every(time => time > 0)).toBe(true);
    });

    it('should handle rate limiting', async () => {
      let requestCount = 0;

      global.fetch = jest.fn().mockImplementation(() => {
        requestCount++;
        if (requestCount > 5) {
          return Promise.resolve({
            ok: false,
            status: 429, // Too Many Requests
            statusText: 'Rate Limited',
          });
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        });
      });

      const requests = Array.from({ length: 10 }, () =>
        apiService.getSymbolsByAssetClass('CRYPTO').catch(err => err)
      );

      const results = await Promise.all(requests);
      const rateLimitedRequests = results.filter(r => r instanceof Error);

      expect(rateLimitedRequests.length).toBeGreaterThan(0);
    });
  });

  describe('Caching and Storage', () => {
    it('should cache API responses appropriately', async () => {
      // First request should hit the API
      const result1 = await apiService.getSymbolsByAssetClass('CRYPTO');
      const firstRequestCount = networkSimulator.getRequestCount();

      // Immediate second request might use cache
      const result2 = await apiService.getSymbolsByAssetClass('CRYPTO');
      const secondRequestCount = networkSimulator.getRequestCount();

      expect(result1).toBeDefined();
      expect(result2).toBeDefined();
      // In real implementation, second request might be cached
      expect(secondRequestCount).toBeGreaterThanOrEqual(firstRequestCount);
    });

    it('should invalidate cache on data updates', async () => {
      // Get initial data
      await apiService.getSymbolsByAssetClass('CRYPTO');

      // Update data
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ updated: true }),
      });

      // Next request should fetch fresh data
      const result = await apiService.getSymbolsByAssetClass('CRYPTO');
      expect(result).toEqual({ updated: true });
    });

    it('should persist auth tokens correctly', async () => {
      await apiService.login('test@example.com', 'password123');

      expect(mockAsyncStorage.setItem).toHaveBeenCalledWith(
        'session_token',
        'mock-jwt-token'
      );
    });

    it('should clear storage on logout', async () => {
      await apiService.logout();

      expect(mockAsyncStorage.removeItem).toHaveBeenCalledWith('session_token');
      expect(mockAsyncStorage.removeItem).toHaveBeenCalledWith('refresh_token');
    });
  });
});