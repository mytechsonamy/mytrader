import { vi } from 'vitest';
import { act } from '@testing-library/react';

/**
 * Test Helper Utilities
 * 
 * Collection of utility functions to simplify testing across the application
 */

/**
 * Wait for a specified amount of time in tests
 * @param ms Milliseconds to wait
 */
export const waitFor = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

/**
 * Flush all pending promises
 */
export const flushPromises = () => new Promise(resolve => setImmediate(resolve));

/**
 * Mock a successful API response
 * @param data Response data
 * @param delay Optional delay in milliseconds
 */
export const mockApiSuccess = <T>(data: T, delay = 0) => {
  return new Promise<T>((resolve) => {
    setTimeout(() => resolve(data), delay);
  });
};

/**
 * Mock an API error response
 * @param error Error message or object
 * @param delay Optional delay in milliseconds
 */
export const mockApiError = (error: string | Error, delay = 0) => {
  return new Promise((_, reject) => {
    setTimeout(() => {
      const errorObj = typeof error === 'string' ? new Error(error) : error;
      reject(errorObj);
    }, delay);
  });
};

/**
 * Create a mock function with spies for testing
 * @param implementation Optional implementation
 */
export const createMockFunction = <T extends (...args: any[]) => any>(
  implementation?: T
): T & ReturnType<typeof vi.fn> => {
  return vi.fn(implementation) as T & ReturnType<typeof vi.fn>;
};

/**
 * Mock WebSocket for testing real-time features
 */
export class MockWebSocket {
  static instances: MockWebSocket[] = [];
  
  url: string;
  readyState: number = WebSocket.CONNECTING;
  onopen?: (event: Event) => void;
  onmessage?: (event: MessageEvent) => void;
  onerror?: (event: Event) => void;
  onclose?: (event: CloseEvent) => void;

  constructor(url: string) {
    this.url = url;
    MockWebSocket.instances.push(this);
    
    // Simulate connection after a short delay
    setTimeout(() => {
      this.readyState = WebSocket.OPEN;
      this.onopen?.(new Event('open'));
    }, 10);
  }

  send(data: string | ArrayBuffer | Blob | ArrayBufferView) {
    if (this.readyState === WebSocket.OPEN) {
      // Simulate echo for testing
      setTimeout(() => {
        this.onmessage?.(new MessageEvent('message', { data }));
      }, 10);
    }
  }

  close(code?: number, reason?: string) {
    this.readyState = WebSocket.CLOSED;
    this.onclose?.(new CloseEvent('close', { code, reason }));
    
    const index = MockWebSocket.instances.indexOf(this);
    if (index > -1) {
      MockWebSocket.instances.splice(index, 1);
    }
  }

  static mockImplementation() {
    // @ts-ignore
    global.WebSocket = MockWebSocket;
    return MockWebSocket;
  }

  static resetMock() {
    MockWebSocket.instances.forEach(ws => ws.close());
    MockWebSocket.instances = [];
  }
}

/**
 * Mock SignalR connection for testing
 */
export const createMockSignalRConnection = () => {
  const mockConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    on: vi.fn(),
    off: vi.fn(),
    invoke: vi.fn(),
    send: vi.fn(),
    stream: vi.fn(),
    state: 'Connected',
    connectionId: 'mock-connection-id',
  };

  return mockConnection;
};

/**
 * Mock localStorage for testing
 */
export const createMockLocalStorage = () => {
  let store: Record<string, string> = {};

  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value;
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key];
    }),
    clear: vi.fn(() => {
      store = {};
    }),
    length: 0,
    key: vi.fn((index: number) => Object.keys(store)[index] || null),
  };
};

/**
 * Mock IntersectionObserver for testing
 */
export const mockIntersectionObserver = () => {
  const mockIntersectionObserver = vi.fn();
  mockIntersectionObserver.mockReturnValue({
    observe: vi.fn(),
    unobserve: vi.fn(),
    disconnect: vi.fn(),
  });
  
  Object.defineProperty(window, 'IntersectionObserver', {
    writable: true,
    configurable: true,
    value: mockIntersectionObserver,
  });

  Object.defineProperty(global, 'IntersectionObserver', {
    writable: true,
    configurable: true,
    value: mockIntersectionObserver,
  });
};

/**
 * Mock ResizeObserver for testing
 */
export const mockResizeObserver = () => {
  const mockResizeObserver = vi.fn();
  mockResizeObserver.mockReturnValue({
    observe: vi.fn(),
    unobserve: vi.fn(),
    disconnect: vi.fn(),
  });
  
  Object.defineProperty(window, 'ResizeObserver', {
    writable: true,
    configurable: true,
    value: mockResizeObserver,
  });
};

/**
 * Mock matchMedia for testing responsive components
 */
export const mockMatchMedia = (matches = false) => {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation(query => ({
      matches,
      media: query,
      onchange: null,
      addListener: vi.fn(), // deprecated
      removeListener: vi.fn(), // deprecated
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  });
};

/**
 * Mock fetch API for testing HTTP requests
 */
export const mockFetch = (response: any, options: { status?: number; ok?: boolean } = {}) => {
  const { status = 200, ok = true } = options;
  
  global.fetch = vi.fn().mockResolvedValue({
    ok,
    status,
    json: vi.fn().mockResolvedValue(response),
    text: vi.fn().mockResolvedValue(JSON.stringify(response)),
    headers: new Headers(),
    redirected: false,
    statusText: 'OK',
    type: 'basic',
    url: 'http://localhost:3000',
    clone: vi.fn(),
    body: null,
    bodyUsed: false,
    arrayBuffer: vi.fn(),
    blob: vi.fn(),
    formData: vi.fn(),
  });
  
  return fetch as ReturnType<typeof vi.fn>;
};

/**
 * Create mock market data for testing
 */
export const createMockMarketData = (overrides: Partial<any> = {}) => ({
  symbol: 'AAPL',
  price: 150.00,
  change24h: 2.50,
  changePercent24h: 1.69,
  volume: 1000000,
  high24h: 152.00,
  low24h: 148.00,
  timestamp: new Date().toISOString(),
  ...overrides,
});

/**
 * Create mock user data for testing
 */
export const createMockUser = (overrides: Partial<any> = {}) => ({
  id: 'mock-user-id',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  phone: '+1234567890',
  isActive: true,
  isEmailVerified: true,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides,
});

/**
 * Create mock portfolio data for testing
 */
export const createMockPortfolio = (overrides: Partial<any> = {}) => ({
  id: 'mock-portfolio-id',
  userId: 'mock-user-id',
  totalValue: 10000,
  cashBalance: 5000,
  positions: [],
  performance: {
    totalReturn: 500,
    totalReturnPercent: 5.0,
    dayReturn: 100,
    dayReturnPercent: 1.0,
  },
  ...overrides,
});

/**
 * Utility to simulate async actions in tests
 */
export const actAsync = async (fn: () => Promise<void>) => {
  await act(async () => {
    await fn();
  });
};

/**
 * Utility to wait for an element to appear in tests
 */
export const waitForElement = async (callback: () => any, timeout = 5000) => {
  const startTime = Date.now();
  
  while (Date.now() - startTime < timeout) {
    try {
      const result = callback();
      if (result) return result;
    } catch (error) {
      // Element not found yet, continue waiting
    }
    
    await waitFor(100);
  }
  
  throw new Error(`Element not found within ${timeout}ms`);
};

/**
 * Mock console methods for testing
 */
export const mockConsole = () => {
  const originalConsole = {
    log: console.log,
    warn: console.warn,
    error: console.error,
    info: console.info,
  };
  
  console.log = vi.fn();
  console.warn = vi.fn();
  console.error = vi.fn();
  console.info = vi.fn();
  
  return {
    restore: () => {
      console.log = originalConsole.log;
      console.warn = originalConsole.warn;
      console.error = originalConsole.error;
      console.info = originalConsole.info;
    },
    mocks: {
      log: console.log as ReturnType<typeof vi.fn>,
      warn: console.warn as ReturnType<typeof vi.fn>,
      error: console.error as ReturnType<typeof vi.fn>,
      info: console.info as ReturnType<typeof vi.fn>,
    },
  };
};

/**
 * Performance testing utility
 */
export const measurePerformance = async (fn: () => Promise<void> | void): Promise<number> => {
  const startTime = performance.now();
  await fn();
  const endTime = performance.now();
  return endTime - startTime;
};

/**
 * Memory leak testing utility
 */
export const detectMemoryLeaks = () => {
  if (typeof window !== 'undefined' && 'performance' in window && 'memory' in window.performance) {
    const memory = (window.performance as any).memory;
    return {
      used: memory.usedJSHeapSize,
      total: memory.totalJSHeapSize,
      limit: memory.jsHeapSizeLimit,
    };
  }
  return null;
};

export default {
  waitFor,
  flushPromises,
  mockApiSuccess,
  mockApiError,
  createMockFunction,
  MockWebSocket,
  createMockSignalRConnection,
  createMockLocalStorage,
  mockIntersectionObserver,
  mockResizeObserver,
  mockMatchMedia,
  mockFetch,
  createMockMarketData,
  createMockUser,
  createMockPortfolio,
  actAsync,
  waitForElement,
  mockConsole,
  measurePerformance,
  detectMemoryLeaks,
};