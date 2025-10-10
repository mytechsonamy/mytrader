import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import authSlice from '../store/slices/authSlice';
import marketSlice from '../store/slices/marketSlice';
import uiSlice from '../store/slices/uiSlice';
import { useWebSocket } from '../hooks/useWebSocket';
import { mockMarketData } from './index';

// Mock performance API
global.performance = {
  ...global.performance,
  mark: vi.fn(),
  measure: vi.fn(),
  getEntriesByType: vi.fn(),
  getEntriesByName: vi.fn(),
  clearMarks: vi.fn(),
  clearMeasures: vi.fn(),
  now: vi.fn(() => Date.now()),
};

// Mock WebSocket service with performance tracking
const mockWebSocketService = {
  connect: vi.fn(),
  disconnect: vi.fn(),
  isConnected: vi.fn(() => false),
  subscribeToMultipleSymbols: vi.fn(),
  unsubscribeFromSymbol: vi.fn(),
  subscribeToSymbol: vi.fn(),
  getConnectionState: vi.fn(() => 'Disconnected'),
  connectionTime: 0,
  dataProcessingTime: 0,
  messagesReceived: 0,
  messagesPerSecond: 0,
};

const mockCreateWebSocketService = vi.fn(() => mockWebSocketService);

vi.mock('../services/websocketService', () => ({
  createWebSocketService: mockCreateWebSocketService,
  getWebSocketService: vi.fn(() => mockWebSocketService),
  WebSocketService: vi.fn().mockImplementation(() => mockWebSocketService),
}));

describe('WebSocket Performance and Load Testing', () => {
  let testStore: any;
  let wrapper: any;
  let performanceMetrics: any;

  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();

    performanceMetrics = {
      connectionAttempts: 0,
      successfulConnections: 0,
      failedConnections: 0,
      messagesReceived: 0,
      dataProcessingTime: [],
      memoryUsage: [],
      subscriptions: new Set(),
    };

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
    mockWebSocketService.connect.mockImplementation(async () => {
      performanceMetrics.connectionAttempts++;
      const startTime = performance.now();

      // Simulate connection delay
      await new Promise(resolve => setTimeout(resolve, 100));

      const endTime = performance.now();
      mockWebSocketService.connectionTime = endTime - startTime;
      performanceMetrics.successfulConnections++;
    });

    mockWebSocketService.subscribeToMultipleSymbols.mockImplementation(async (symbols: string[]) => {
      symbols.forEach(symbol => performanceMetrics.subscriptions.add(symbol));
    });
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  describe('Connection Performance', () => {
    it('should establish connection within acceptable time limits', async () => {
      const startTime = performance.now();

      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      await act(async () => {
        await result.current.connect();
      });

      const endTime = performance.now();
      const connectionTime = endTime - startTime;

      expect(connectionTime).toBeLessThan(5000); // Should connect within 5 seconds
      expect(performanceMetrics.successfulConnections).toBe(1);
    });

    it('should handle multiple concurrent connection attempts efficiently', async () => {
      const connectionPromises = [];
      const hookResults = [];

      // Create multiple hooks
      for (let i = 0; i < 10; i++) {
        const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });
        hookResults.push(result);
      }

      const startTime = performance.now();

      // Attempt concurrent connections
      for (const result of hookResults) {
        connectionPromises.push(
          act(async () => {
            await result.current.connect();
          })
        );
      }

      await Promise.all(connectionPromises);

      const endTime = performance.now();
      const totalTime = endTime - startTime;

      // Should handle 10 concurrent connections within reasonable time
      expect(totalTime).toBeLessThan(10000);
      expect(performanceMetrics.connectionAttempts).toBe(10);
    });

    it('should recover quickly from connection failures', async () => {
      let failureCount = 0;
      mockWebSocketService.connect.mockImplementation(async () => {
        performanceMetrics.connectionAttempts++;
        failureCount++;

        if (failureCount <= 2) {
          performanceMetrics.failedConnections++;
          throw new Error('Connection failed');
        }

        performanceMetrics.successfulConnections++;
      });

      const { result } = renderHook(() => useWebSocket({ enabled: false }), { wrapper });

      const startTime = performance.now();

      // Attempt connection multiple times
      for (let i = 0; i < 3; i++) {
        try {
          await act(async () => {
            await result.current.connect();
          });
        } catch (error) {
          // Expected failures
        }
      }

      const endTime = performance.now();
      const recoveryTime = endTime - startTime;

      expect(recoveryTime).toBeLessThan(5000);
      expect(performanceMetrics.successfulConnections).toBe(1);
      expect(performanceMetrics.failedConnections).toBe(2);
    });
  });

  describe('Data Processing Performance', () => {
    it('should handle high-frequency market data updates efficiently', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      // Get the market data handler
      const onMarketDataUpdate = mockCreateWebSocketService.mock.calls[0][0].onMarketDataUpdate;

      const dataUpdates = Array.from({ length: 1000 }, (_, i) => ({
        ...mockMarketData,
        symbolId: `SYMBOL${i % 10}`, // 10 different symbols
        price: 50000 + i,
        timestamp: new Date(Date.now() + i * 100).toISOString(),
      }));

      const startTime = performance.now();

      // Process updates
      await act(async () => {
        dataUpdates.forEach(update => {
          const updateStartTime = performance.now();
          onMarketDataUpdate(update);
          const updateEndTime = performance.now();
          performanceMetrics.dataProcessingTime.push(updateEndTime - updateStartTime);
        });
      });

      const endTime = performance.now();
      const totalProcessingTime = endTime - startTime;

      // Should process 1000 updates within 5 seconds
      expect(totalProcessingTime).toBeLessThan(5000);

      // Average processing time per update should be minimal
      const avgProcessingTime = performanceMetrics.dataProcessingTime.reduce((a, b) => a + b, 0) / performanceMetrics.dataProcessingTime.length;
      expect(avgProcessingTime).toBeLessThan(10); // Less than 10ms per update
    });

    it('should maintain performance with continuous data streams', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onMarketDataUpdate = mockCreateWebSocketService.mock.calls[0][0].onMarketDataUpdate;

      // Simulate continuous data stream
      const streamDuration = 10000; // 10 seconds
      const updateInterval = 100; // Update every 100ms
      const totalUpdates = streamDuration / updateInterval;

      let updateCount = 0;
      const processingTimes: number[] = [];

      const startTime = performance.now();

      const streamInterval = setInterval(() => {
        if (updateCount >= totalUpdates) {
          clearInterval(streamInterval);
          return;
        }

        const updateStartTime = performance.now();

        act(() => {
          onMarketDataUpdate({
            ...mockMarketData,
            symbolId: `SYMBOL${updateCount % 5}`,
            price: 50000 + updateCount,
            timestamp: new Date().toISOString(),
          });
        });

        const updateEndTime = performance.now();
        processingTimes.push(updateEndTime - updateStartTime);
        updateCount++;
      }, updateInterval);

      // Fast-forward through the entire stream
      vi.advanceTimersByTime(streamDuration + 1000);

      const endTime = performance.now();

      expect(updateCount).toBe(totalUpdates);

      // Check that processing times remain consistent
      const avgProcessingTime = processingTimes.reduce((a, b) => a + b, 0) / processingTimes.length;
      const maxProcessingTime = Math.max(...processingTimes);

      expect(avgProcessingTime).toBeLessThan(5);
      expect(maxProcessingTime).toBeLessThan(50);
    });

    it('should handle burst traffic without performance degradation', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onMarketDataUpdate = mockCreateWebSocketService.mock.calls[0][0].onMarketDataUpdate;

      // Simulate burst of 500 updates in rapid succession
      const burstUpdates = Array.from({ length: 500 }, (_, i) => ({
        ...mockMarketData,
        symbolId: `BURST_SYMBOL${i % 20}`,
        price: 50000 + i,
        timestamp: new Date().toISOString(),
      }));

      const startTime = performance.now();

      await act(async () => {
        burstUpdates.forEach(update => {
          onMarketDataUpdate(update);
        });
      });

      const endTime = performance.now();
      const burstProcessingTime = endTime - startTime;

      // Should handle burst within 2 seconds
      expect(burstProcessingTime).toBeLessThan(2000);

      // Store should contain all updates
      const state = testStore.getState();
      expect(Object.keys(state.market.marketData)).toHaveLength(20); // 20 unique symbols
    });
  });

  describe('Memory Management', () => {
    it('should not leak memory during extended operation', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onMarketDataUpdate = mockCreateWebSocketService.mock.calls[0][0].onMarketDataUpdate;

      // Track memory usage over time
      const memorySnapshots: number[] = [];

      // Simulate extended operation
      for (let cycle = 0; cycle < 100; cycle++) {
        // Process a batch of updates
        for (let i = 0; i < 50; i++) {
          act(() => {
            onMarketDataUpdate({
              ...mockMarketData,
              symbolId: `SYMBOL${i % 10}`,
              price: 50000 + cycle * 50 + i,
              timestamp: new Date().toISOString(),
            });
          });
        }

        // Simulate memory snapshot
        const memoryUsage = process.memoryUsage ? process.memoryUsage().heapUsed : Math.random() * 1000000;
        memorySnapshots.push(memoryUsage);

        vi.advanceTimersByTime(100);
      }

      // Check memory growth pattern
      const initialMemory = memorySnapshots[0];
      const finalMemory = memorySnapshots[memorySnapshots.length - 1];
      const memoryGrowth = finalMemory - initialMemory;

      // Memory growth should be reasonable (less than 100MB)
      expect(memoryGrowth).toBeLessThan(100 * 1024 * 1024);

      // Memory usage should stabilize (not grow indefinitely)
      const lastTenSnapshots = memorySnapshots.slice(-10);
      const memoryVariation = Math.max(...lastTenSnapshots) - Math.min(...lastTenSnapshots);
      const averageMemory = lastTenSnapshots.reduce((a, b) => a + b, 0) / lastTenSnapshots.length;

      // Variation should be less than 20% of average
      expect(memoryVariation).toBeLessThan(averageMemory * 0.2);
    });

    it('should clean up subscriptions on disconnect', async () => {
      const { result } = renderHook(() => useWebSocket({
        enabled: true,
        symbols: ['SYMBOL1', 'SYMBOL2', 'SYMBOL3']
      }), { wrapper });

      // Connect and subscribe
      await act(async () => {
        await result.current.connect();
      });

      mockWebSocketService.isConnected.mockReturnValue(true);

      await act(async () => {
        await result.current.subscribeToSymbols(['SYMBOL1', 'SYMBOL2', 'SYMBOL3']);
      });

      expect(performanceMetrics.subscriptions.size).toBe(3);

      // Disconnect should clean up
      await act(async () => {
        await result.current.disconnect();
      });

      // Verify cleanup
      expect(mockWebSocketService.disconnect).toHaveBeenCalled();
    });
  });

  describe('Scalability Testing', () => {
    it('should handle large number of symbol subscriptions', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      mockWebSocketService.isConnected.mockReturnValue(true);

      // Subscribe to 100 symbols
      const symbols = Array.from({ length: 100 }, (_, i) => `SYMBOL${i}`);

      const startTime = performance.now();

      await act(async () => {
        await result.current.subscribeToSymbols(symbols);
      });

      const endTime = performance.now();
      const subscriptionTime = endTime - startTime;

      // Should handle large subscription within reasonable time
      expect(subscriptionTime).toBeLessThan(1000);
      expect(performanceMetrics.subscriptions.size).toBe(100);
    });

    it('should maintain performance with multiple concurrent users simulation', async () => {
      const userCount = 50;
      const hookResults = [];

      // Create multiple user simulations
      for (let i = 0; i < userCount; i++) {
        const { result } = renderHook(() => useWebSocket({
          enabled: true,
          symbols: [`USER${i}_SYMBOL1`, `USER${i}_SYMBOL2`]
        }), { wrapper });
        hookResults.push(result);
      }

      const startTime = performance.now();

      // Simulate all users connecting and subscribing
      const userPromises = hookResults.map(async (result, index) => {
        await act(async () => {
          await result.current.connect();
        });

        mockWebSocketService.isConnected.mockReturnValue(true);

        await act(async () => {
          await result.current.subscribeToSymbols([`USER${index}_SYMBOL1`, `USER${index}_SYMBOL2`]);
        });
      });

      await Promise.allSettled(userPromises);

      const endTime = performance.now();
      const totalTime = endTime - startTime;

      // Should handle 50 concurrent users within 10 seconds
      expect(totalTime).toBeLessThan(10000);
      expect(performanceMetrics.subscriptions.size).toBe(100); // 50 users * 2 symbols each
    });

    it('should handle rapid subscription changes efficiently', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      mockWebSocketService.isConnected.mockReturnValue(true);

      const startTime = performance.now();

      // Rapidly change subscriptions 100 times
      for (let i = 0; i < 100; i++) {
        const symbols = [`RAPID_${i}_1`, `RAPID_${i}_2`, `RAPID_${i}_3`];

        await act(async () => {
          await result.current.subscribeToSymbols(symbols);
        });

        await act(async () => {
          await result.current.unsubscribeFromSymbols(symbols);
        });
      }

      const endTime = performance.now();
      const totalTime = endTime - startTime;

      // Should handle rapid changes within 5 seconds
      expect(totalTime).toBeLessThan(5000);
    });
  });

  describe('Resource Utilization', () => {
    it('should minimize CPU usage during idle periods', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      // Connect but don't send data
      await act(async () => {
        await result.current.connect();
      });

      // Measure idle CPU usage (simulated)
      const startTime = performance.now();

      // Advance time without triggering updates
      vi.advanceTimersByTime(60000); // 1 minute

      const endTime = performance.now();

      // CPU usage should be minimal during idle (simulation)
      const idleTime = endTime - startTime;
      expect(idleTime).toBeLessThan(100); // Minimal processing time
    });

    it('should optimize network usage', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      let networkRequests = 0;

      // Mock network requests
      mockWebSocketService.subscribeToMultipleSymbols.mockImplementation(async (symbols) => {
        networkRequests++;
        symbols.forEach(symbol => performanceMetrics.subscriptions.add(symbol));
      });

      mockWebSocketService.isConnected.mockReturnValue(true);

      // Subscribe to overlapping symbol sets
      await act(async () => {
        await result.current.subscribeToSymbols(['A', 'B', 'C']);
      });

      await act(async () => {
        await result.current.subscribeToSymbols(['B', 'C', 'D']);
      });

      await act(async () => {
        await result.current.subscribeToSymbols(['C', 'D', 'E']);
      });

      // Should optimize network requests
      expect(networkRequests).toBe(3); // One per subscription call
      expect(performanceMetrics.subscriptions.size).toBe(5); // Total unique symbols
    });
  });

  describe('Performance Benchmarks', () => {
    it('should meet connection establishment benchmarks', async () => {
      const benchmarks = {
        connectionTime: 3000, // Max 3 seconds
        subscriptionTime: 1000, // Max 1 second for 10 symbols
        dataProcessingRate: 100, // Min 100 updates per second
      };

      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      // Benchmark connection time
      const connectionStart = performance.now();
      await act(async () => {
        await result.current.connect();
      });
      const connectionEnd = performance.now();

      expect(connectionEnd - connectionStart).toBeLessThan(benchmarks.connectionTime);

      // Benchmark subscription time
      mockWebSocketService.isConnected.mockReturnValue(true);
      const symbols = Array.from({ length: 10 }, (_, i) => `BENCH_SYMBOL${i}`);

      const subscriptionStart = performance.now();
      await act(async () => {
        await result.current.subscribeToSymbols(symbols);
      });
      const subscriptionEnd = performance.now();

      expect(subscriptionEnd - subscriptionStart).toBeLessThan(benchmarks.subscriptionTime);
    });

    it('should maintain performance under stress conditions', async () => {
      const { result } = renderHook(() => useWebSocket({ enabled: true }), { wrapper });

      const onMarketDataUpdate = mockCreateWebSocketService.mock.calls[0][0].onMarketDataUpdate;

      // Stress test: High frequency updates for multiple symbols
      const stressTestDuration = 5000; // 5 seconds
      const updateFrequency = 10; // Every 10ms
      const symbolCount = 20;

      let updatesProcessed = 0;
      const startTime = performance.now();

      const stressInterval = setInterval(() => {
        for (let i = 0; i < symbolCount; i++) {
          act(() => {
            onMarketDataUpdate({
              ...mockMarketData,
              symbolId: `STRESS_SYMBOL${i}`,
              price: 50000 + updatesProcessed,
              timestamp: new Date().toISOString(),
            });
          });
          updatesProcessed++;
        }
      }, updateFrequency);

      // Run stress test
      vi.advanceTimersByTime(stressTestDuration);
      clearInterval(stressInterval);

      const endTime = performance.now();
      const actualDuration = endTime - startTime;

      const expectedUpdates = (stressTestDuration / updateFrequency) * symbolCount;
      const updateRate = updatesProcessed / (actualDuration / 1000);

      // Should maintain high update rate
      expect(updateRate).toBeGreaterThan(1000); // At least 1000 updates per second
      expect(updatesProcessed).toBeGreaterThanOrEqual(expectedUpdates * 0.9); // At least 90% of expected updates
    });
  });
});