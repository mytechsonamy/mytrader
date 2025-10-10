import '@testing-library/jest-dom';
import { beforeAll, afterAll, afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import { 
  mockIntersectionObserver,
  mockResizeObserver,
  mockMatchMedia,
  mockConsole,
  MockWebSocket
} from './testHelpers';

// Global test setup for Vitest

// Cleanup after each test
afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  localStorage.clear();
  sessionStorage.clear();
});

// Setup global mocks
beforeAll(() => {
  // Mock DOM APIs
  mockIntersectionObserver();
  mockResizeObserver();
  mockMatchMedia();
  
  // Mock WebSocket
  MockWebSocket.mockImplementation();
  
  // Mock console for cleaner test output (optional)
  if (process.env.NODE_ENV === 'test') {
    const consoleMock = mockConsole();
    // You can restore console in specific tests if needed
    // consoleMock.restore();
  }
  
  // Mock window.location
  Object.defineProperty(window, 'location', {
    value: {
      href: 'http://localhost:3000',
      origin: 'http://localhost:3000',
      pathname: '/',
      search: '',
      hash: '',
      assign: vi.fn(),
      replace: vi.fn(),
      reload: vi.fn(),
    },
    writable: true,
  });
  
  // Mock localStorage
  const localStorageMock = {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
    length: 0,
    key: vi.fn(),
  };
  Object.defineProperty(window, 'localStorage', {
    value: localStorageMock,
    writable: true,
  });
  
  // Mock sessionStorage
  const sessionStorageMock = {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
    length: 0,
    key: vi.fn(),
  };
  Object.defineProperty(window, 'sessionStorage', {
    value: sessionStorageMock,
    writable: true,
  });
  
  // Mock crypto.randomUUID
  Object.defineProperty(window, 'crypto', {
    value: {
      randomUUID: () => 'mock-uuid-' + Math.random().toString(36).substr(2, 9),
      getRandomValues: (arr: any) => {
        for (let i = 0; i < arr.length; i++) {
          arr[i] = Math.floor(Math.random() * 256);
        }
        return arr;
      },
    },
    writable: true,
  });
  
  // Mock requestAnimationFrame
  global.requestAnimationFrame = vi.fn(cb => setTimeout(cb, 0));
  global.cancelAnimationFrame = vi.fn(id => clearTimeout(id));
  
  // Mock performance.now
  Object.defineProperty(window, 'performance', {
    value: {
      now: vi.fn(() => Date.now()),
      mark: vi.fn(),
      measure: vi.fn(),
      getEntriesByName: vi.fn(() => []),
      getEntriesByType: vi.fn(() => []),
    },
    writable: true,
  });
  
  // Mock URL.createObjectURL
  global.URL.createObjectURL = vi.fn(() => 'mock-object-url');
  global.URL.revokeObjectURL = vi.fn();
  
  // Setup global error handling for tests
  const originalError = console.error;
  console.error = (...args: any[]) => {
    // Suppress known React testing warnings
    const message = args[0];
    if (typeof message === 'string') {
      // Suppress React Router warnings in tests
      if (message.includes('useNavigate')) return;
      if (message.includes('useLocation')) return;
      if (message.includes('Router.Provider')) return;
    }
    originalError.call(console, ...args);
  };
});

// Cleanup after all tests
afterAll(() => {
  MockWebSocket.resetMock();
  vi.restoreAllMocks();
});

// Global test configuration
declare global {
  var __TEST_ENV__: boolean;
}

global.__TEST_ENV__ = true;

// Export test utilities for easy access
export * from './testHelpers';
export * from './index';

// Test environment validation
if (typeof window === 'undefined') {
  throw new Error('Tests must run in a DOM environment. Make sure jsdom is configured.');
}

console.log('Test environment setup completed');

// Performance monitoring for slow tests
const TEST_TIMEOUT_WARNING = 5000; // 5 seconds
const originalSetTimeout = global.setTimeout;

global.setTimeout = ((callback: Function, delay: number, ...args: any[]) => {
  if (delay > TEST_TIMEOUT_WARNING) {
    console.warn(`Long timeout detected: ${delay}ms. Consider reducing for faster tests.`);
  }
  return originalSetTimeout.call(global, callback, delay, ...args);
}) as any;

// Memory leak detection (basic)
let initialMemory: any = null;
beforeAll(() => {
  if (typeof window !== 'undefined' && 'performance' in window && 'memory' in (window.performance as any)) {
    initialMemory = (window.performance as any).memory.usedJSHeapSize;
  }
});

afterAll(() => {
  if (initialMemory !== null && typeof window !== 'undefined' && 'performance' in window && 'memory' in (window.performance as any)) {
    const finalMemory = (window.performance as any).memory.usedJSHeapSize;
    const memoryIncrease = finalMemory - initialMemory;
    
    if (memoryIncrease > 10 * 1024 * 1024) { // 10MB threshold
      console.warn(`Potential memory leak detected. Memory increased by ${(memoryIncrease / 1024 / 1024).toFixed(2)}MB`);
    }
  }
});