import { describe, it, expect, beforeEach, vi } from 'vitest';
import axios from 'axios';
import { marketDataService } from '../marketDataService';
import { mockSymbolsResponse, mockMarketData } from '../../test-utils';

// Mock axios
vi.mock('axios', () => ({
  default: {
    get: vi.fn(),
  },
}));

const mockAxios = axios as any;

describe('MarketDataService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getSymbols', () => {
    it('should fetch symbols successfully', async () => {
      mockAxios.get.mockResolvedValue({ data: mockSymbolsResponse });

      const result = await marketDataService.getSymbols();

      expect(mockAxios.get).toHaveBeenCalledWith('/api/v1/symbols');
      expect(result).toEqual(mockSymbolsResponse);
    });

    it('should handle symbols fetch failure with server error message', async () => {
      const errorMessage = 'Symbols service unavailable';
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        response: {
          data: { message: errorMessage },
        },
      });

      await expect(marketDataService.getSymbols()).rejects.toThrow(errorMessage);
      expect(consoleSpy).toHaveBeenCalledWith('Failed to fetch symbols:', expect.any(Object));

      consoleSpy.mockRestore();
    });

    it('should handle symbols fetch failure with generic error', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue(new Error('Network timeout'));

      await expect(marketDataService.getSymbols()).rejects.toThrow('Failed to fetch symbols');

      consoleSpy.mockRestore();
    });

    it('should handle symbols fetch failure without error response', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        message: 'Request failed',
      });

      await expect(marketDataService.getSymbols()).rejects.toThrow('Failed to fetch symbols');

      consoleSpy.mockRestore();
    });

    it('should handle empty symbols response', async () => {
      const emptyResponse = { symbols: {}, interval: '1m' };
      mockAxios.get.mockResolvedValue({ data: emptyResponse });

      const result = await marketDataService.getSymbols();

      expect(result).toEqual(emptyResponse);
    });

    it('should handle malformed symbols response', async () => {
      const malformedResponse = { invalid: 'data' };
      mockAxios.get.mockResolvedValue({ data: malformedResponse });

      const result = await marketDataService.getSymbols();

      expect(result).toEqual(malformedResponse);
    });
  });

  describe('getMarketData', () => {
    const testSymbolId = 'BTCUSDT';

    it('should fetch market data for a single symbol successfully', async () => {
      mockAxios.get.mockResolvedValue({ data: { data: mockMarketData } });

      const result = await marketDataService.getMarketData(testSymbolId);

      expect(mockAxios.get).toHaveBeenCalledWith(`/api/market-data/realtime/${testSymbolId}`);
      expect(result).toEqual(mockMarketData);
    });

    it('should handle market data fetch failure with server error', async () => {
      const errorMessage = 'Symbol not found';
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        response: {
          data: { message: errorMessage },
        },
      });

      await expect(marketDataService.getMarketData(testSymbolId)).rejects.toThrow(errorMessage);

      consoleSpy.mockRestore();
    });

    it('should handle market data fetch failure with generic error', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue(new Error('Connection refused'));

      await expect(marketDataService.getMarketData(testSymbolId)).rejects.toThrow('Failed to fetch market data');

      consoleSpy.mockRestore();
    });

    it('should handle invalid symbol ID', async () => {
      const invalidSymbolId = 'INVALID_SYMBOL';
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        response: {
          status: 404,
          data: { message: 'Symbol not found' },
        },
      });

      await expect(marketDataService.getMarketData(invalidSymbolId)).rejects.toThrow('Symbol not found');

      consoleSpy.mockRestore();
    });

    it('should handle missing data field in response', async () => {
      mockAxios.get.mockResolvedValue({ data: { status: 'success' } });

      const result = await marketDataService.getMarketData(testSymbolId);

      expect(result).toBeUndefined();
    });

    it('should handle null response data', async () => {
      mockAxios.get.mockResolvedValue({ data: null });

      const result = await marketDataService.getMarketData(testSymbolId);

      expect(result).toBeUndefined();
    });
  });

  describe('getMultipleMarketData', () => {
    const testSymbolIds = ['BTCUSDT', 'ETHUSDT', 'LTCUSDT'];

    it('should fetch market data for multiple symbols successfully', async () => {
      const marketDataResponses = testSymbolIds.map((symbolId, index) => ({
        data: {
          data: {
            ...mockMarketData,
            symbolId,
            ticker: symbolId,
            price: 50000 + index * 1000,
          }
        }
      }));

      mockAxios.get
        .mockResolvedValueOnce(marketDataResponses[0])
        .mockResolvedValueOnce(marketDataResponses[1])
        .mockResolvedValueOnce(marketDataResponses[2]);

      const result = await marketDataService.getMultipleMarketData(testSymbolIds);

      expect(result).toHaveLength(3);
      expect(result[0].symbolId).toBe('BTCUSDT');
      expect(result[1].symbolId).toBe('ETHUSDT');
      expect(result[2].symbolId).toBe('LTCUSDT');
      expect(mockAxios.get).toHaveBeenCalledTimes(3);
    });

    it('should handle partial success in multiple symbol fetch', async () => {
      const successResponse = { data: { data: mockMarketData } };
      const failureError = new Error('Symbol not found');

      mockAxios.get
        .mockResolvedValueOnce(successResponse)
        .mockRejectedValueOnce(failureError)
        .mockResolvedValueOnce(successResponse);

      const result = await marketDataService.getMultipleMarketData(testSymbolIds);

      // Should return only successful results
      expect(result).toHaveLength(2);
      expect(result[0]).toEqual(mockMarketData);
      expect(result[1]).toEqual(mockMarketData);
    });

    it('should handle complete failure in multiple symbol fetch', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue(new Error('Service unavailable'));

      await expect(marketDataService.getMultipleMarketData(testSymbolIds)).rejects.toThrow('Failed to fetch market data');

      consoleSpy.mockRestore();
    });

    it('should handle empty symbol array', async () => {
      const result = await marketDataService.getMultipleMarketData([]);

      expect(result).toEqual([]);
      expect(mockAxios.get).not.toHaveBeenCalled();
    });

    it('should handle single symbol in array', async () => {
      const singleSymbol = ['BTCUSDT'];
      mockAxios.get.mockResolvedValue({ data: { data: mockMarketData } });

      const result = await marketDataService.getMultipleMarketData(singleSymbol);

      expect(result).toHaveLength(1);
      expect(result[0]).toEqual(mockMarketData);
      expect(mockAxios.get).toHaveBeenCalledTimes(1);
    });

    it('should handle large number of symbols', async () => {
      const manySymbols = Array.from({ length: 50 }, (_, i) => `SYMBOL${i}`);

      mockAxios.get.mockResolvedValue({ data: { data: mockMarketData } });

      const result = await marketDataService.getMultipleMarketData(manySymbols);

      expect(result).toHaveLength(50);
      expect(mockAxios.get).toHaveBeenCalledTimes(50);
    });

    it('should handle mixed success and failure scenarios', async () => {
      const symbolIds = ['VALID1', 'INVALID', 'VALID2'];

      mockAxios.get
        .mockResolvedValueOnce({ data: { data: { ...mockMarketData, symbolId: 'VALID1' } } })
        .mockRejectedValueOnce(new Error('Invalid symbol'))
        .mockResolvedValueOnce({ data: { data: { ...mockMarketData, symbolId: 'VALID2' } } });

      const result = await marketDataService.getMultipleMarketData(symbolIds);

      expect(result).toHaveLength(2);
      expect(result[0].symbolId).toBe('VALID1');
      expect(result[1].symbolId).toBe('VALID2');
    });
  });

  describe('Error Handling and Edge Cases', () => {
    it('should handle network timeouts', async () => {
      const timeoutError = new Error('Request timeout');
      timeoutError.name = 'TimeoutError';

      mockAxios.get.mockRejectedValue(timeoutError);

      await expect(marketDataService.getSymbols()).rejects.toThrow('Failed to fetch symbols');
    });

    it('should handle rate limiting errors', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        response: {
          status: 429,
          data: { message: 'Rate limit exceeded' },
        },
      });

      await expect(marketDataService.getSymbols()).rejects.toThrow('Rate limit exceeded');

      consoleSpy.mockRestore();
    });

    it('should handle server errors (5xx)', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        response: {
          status: 503,
          data: { message: 'Service temporarily unavailable' },
        },
      });

      await expect(marketDataService.getSymbols()).rejects.toThrow('Service temporarily unavailable');

      consoleSpy.mockRestore();
    });

    it('should handle authentication errors', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        response: {
          status: 401,
          data: { message: 'Unauthorized' },
        },
      });

      await expect(marketDataService.getSymbols()).rejects.toThrow('Unauthorized');

      consoleSpy.mockRestore();
    });

    it('should handle malformed JSON responses', async () => {
      // Mock axios to return invalid JSON
      mockAxios.get.mockResolvedValue({ data: 'invalid json' });

      const result = await marketDataService.getSymbols();

      expect(result).toBe('invalid json');
    });

    it('should handle extremely large responses', async () => {
      const largeSymbolsResponse = {
        symbols: Object.fromEntries(
          Array.from({ length: 10000 }, (_, i) => [
            `SYMBOL${i}`,
            {
              symbol: `SYMBOL${i}`,
              display_name: `Symbol ${i}`,
              precision: 2,
              strategy_type: 'crypto',
            }
          ])
        ),
        interval: '1m',
      };

      mockAxios.get.mockResolvedValue({ data: largeSymbolsResponse });

      const result = await marketDataService.getSymbols();

      expect(result).toEqual(largeSymbolsResponse);
      expect(Object.keys(result.symbols)).toHaveLength(10000);
    });

    it('should handle concurrent requests', async () => {
      mockAxios.get.mockResolvedValue({ data: mockSymbolsResponse });

      const promises = [
        marketDataService.getSymbols(),
        marketDataService.getSymbols(),
        marketDataService.getSymbols(),
      ];

      const results = await Promise.allSettled(promises);

      results.forEach(result => {
        expect(result.status).toBe('fulfilled');
        if (result.status === 'fulfilled') {
          expect(result.value).toEqual(mockSymbolsResponse);
        }
      });
    });

    it('should handle null/undefined symbol IDs gracefully', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      // Test with null symbol ID
      await expect(marketDataService.getMarketData(null as any)).rejects.toThrow();

      // Test with undefined symbol ID
      await expect(marketDataService.getMarketData(undefined as any)).rejects.toThrow();

      consoleSpy.mockRestore();
    });

    it('should handle empty string symbol ID', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        response: {
          status: 400,
          data: { message: 'Invalid symbol ID' },
        },
      });

      await expect(marketDataService.getMarketData('')).rejects.toThrow('Invalid symbol ID');

      consoleSpy.mockRestore();
    });

    it('should handle special characters in symbol IDs', async () => {
      const specialSymbolId = 'BTC/USD-PERP';
      const encodedSymbolId = encodeURIComponent(specialSymbolId);

      mockAxios.get.mockResolvedValue({ data: { data: mockMarketData } });

      await marketDataService.getMarketData(specialSymbolId);

      expect(mockAxios.get).toHaveBeenCalledWith(`/api/market-data/realtime/${specialSymbolId}`);
    });
  });

  describe('Performance and Load Testing', () => {
    it('should handle rapid consecutive requests', async () => {
      mockAxios.get.mockResolvedValue({ data: { data: mockMarketData } });

      const rapidRequests = Array.from({ length: 100 }, () =>
        marketDataService.getMarketData('BTCUSDT')
      );

      const results = await Promise.allSettled(rapidRequests);

      results.forEach(result => {
        expect(result.status).toBe('fulfilled');
      });
    });

    it('should handle batch requests efficiently', async () => {
      const batchSize = 20;
      const symbolIds = Array.from({ length: batchSize }, (_, i) => `SYMBOL${i}`);

      mockAxios.get.mockResolvedValue({ data: { data: mockMarketData } });

      const startTime = performance.now();
      const result = await marketDataService.getMultipleMarketData(symbolIds);
      const endTime = performance.now();

      expect(result).toHaveLength(batchSize);
      // Performance check - should complete within reasonable time
      expect(endTime - startTime).toBeLessThan(5000); // Less than 5 seconds
    });

    it('should handle memory pressure with large datasets', async () => {
      const largeMarketData = {
        ...mockMarketData,
        // Add large data payload
        historicalData: Array.from({ length: 10000 }, (_, i) => ({
          timestamp: new Date(Date.now() - i * 60000).toISOString(),
          price: 50000 + Math.random() * 1000,
          volume: Math.random() * 1000000,
        })),
      };

      mockAxios.get.mockResolvedValue({ data: { data: largeMarketData } });

      const result = await marketDataService.getMarketData('BTCUSDT');

      expect(result).toEqual(largeMarketData);
    });
  });

  describe('Real-world Integration Scenarios', () => {
    it('should handle API versioning changes', async () => {
      // Simulate API returning new field
      const extendedSymbolsResponse = {
        ...mockSymbolsResponse,
        version: '2.0',
        metadata: {
          lastUpdated: '2024-01-01T12:00:00Z',
          totalSymbols: 2,
        },
      };

      mockAxios.get.mockResolvedValue({ data: extendedSymbolsResponse });

      const result = await marketDataService.getSymbols();

      expect(result).toEqual(extendedSymbolsResponse);
    });

    it('should handle deprecated fields gracefully', async () => {
      // Simulate API response with missing fields
      const minimalSymbolsResponse = {
        symbols: {
          'BTCUSDT': {
            symbol: 'BTCUSDT',
            display_name: 'Bitcoin / Tether',
            // Missing precision and strategy_type fields
          },
        },
        interval: '1m',
      };

      mockAxios.get.mockResolvedValue({ data: minimalSymbolsResponse });

      const result = await marketDataService.getSymbols();

      expect(result).toEqual(minimalSymbolsResponse);
    });

    it('should handle server maintenance windows', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.get.mockRejectedValue({
        response: {
          status: 503,
          data: {
            message: 'Scheduled maintenance in progress',
            retryAfter: '300',
          },
        },
      });

      await expect(marketDataService.getSymbols()).rejects.toThrow('Scheduled maintenance in progress');

      consoleSpy.mockRestore();
    });

    it('should handle cache-related headers', async () => {
      mockAxios.get.mockResolvedValue({
        data: mockSymbolsResponse,
        headers: {
          'cache-control': 'max-age=300',
          'etag': '"abc123"',
          'last-modified': 'Mon, 01 Jan 2024 12:00:00 GMT',
        },
      });

      const result = await marketDataService.getSymbols();

      expect(result).toEqual(mockSymbolsResponse);
    });

    it('should handle CORS preflight scenarios', async () => {
      // Simulate CORS error
      const corsError = new Error('CORS policy violation');
      corsError.name = 'CORSError';

      mockAxios.get.mockRejectedValue(corsError);

      await expect(marketDataService.getSymbols()).rejects.toThrow('Failed to fetch symbols');
    });
  });
});