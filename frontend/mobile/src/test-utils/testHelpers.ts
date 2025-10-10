import { waitFor } from '@testing-library/react-native';

/**
 * Mobile Testing Helpers and Utilities
 * Comprehensive collection of helper functions for React Native testing
 */

// Async utilities
export const waitForAsync = async (fn: () => boolean, timeout = 5000) => {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    if (fn()) {
      return true;
    }
    await new Promise(resolve => setTimeout(resolve, 50));
  }
  throw new Error(`Timeout: condition not met within ${timeout}ms`);
};

export const flushPromises = () => new Promise(resolve => setImmediate(resolve));

// Mock data generators
export const generateMockSymbols = (count: number = 5, assetClass: string = 'CRYPTO') => {
  return Array.from({ length: count }, (_, index) => ({
    id: `${index + 1}`,
    symbol: `${assetClass === 'CRYPTO' ? 'BTC' : 'AAPL'}${index + 1}-USD`,
    displayName: `${assetClass === 'CRYPTO' ? 'Bitcoin' : 'Apple'} ${index + 1}`,
    assetClass,
    quoteCurrency: 'USD',
    baseCurrency: assetClass === 'CRYPTO' ? 'BTC' : 'USD',
    marketId: assetClass,
  }));
};

export const generateMockPriceData = (symbols: any[]) => {
  const priceData: Record<string, any> = {};
  symbols.forEach((symbol, index) => {
    priceData[symbol.id] = {
      symbol: symbol.symbol,
      price: 50000 + (index * 1000),
      change: (Math.random() - 0.5) * 5000,
      changePercent: (Math.random() - 0.5) * 10,
      volume: Math.floor(Math.random() * 1000000),
      marketCap: Math.floor(Math.random() * 1000000000),
      lastUpdate: new Date().toISOString(),
    };
  });
  return priceData;
};

export const generateMockPortfolio = (overrides: any = {}) => ({
  id: '1',
  name: 'Test Portfolio',
  totalValue: 25000,
  dailyPnL: 1250,
  totalPnLPercent: 5.2,
  baseCurrency: 'USD',
  positions: [
    {
      id: '1',
      symbol: 'BTC-USD',
      symbolName: 'Bitcoin',
      quantity: 0.5,
      averagePrice: 45000,
      marketValue: 25000,
      unrealizedPnL: 2500,
      unrealizedPnLPercent: 11.11,
    },
  ],
  ...overrides,
});

export const generateMockLeaderboard = (count: number = 5) => {
  return Array.from({ length: count }, (_, index) => ({
    userId: `user-${index + 1}`,
    username: `trader${index + 1}`,
    displayName: `Trader ${index + 1}`,
    rank: index + 1,
    totalReturn: 20 - (index * 2),
    portfolioValue: 50000 - (index * 5000),
    returnPercent: 20 - (index * 2),
    winRate: 80 - (index * 5),
    totalTrades: 50 - (index * 3),
    tier: index === 0 ? 'GOLD' : index === 1 ? 'SILVER' : 'BRONZE',
    badges: index < 2 ? ['winner', 'streak'] : [],
  }));
};

// Network simulation helpers
export class NetworkMocker {
  private static instance: NetworkMocker;
  private isOnline = true;
  private latency = 0;
  private errorRate = 0;
  private requestQueue: Array<() => void> = [];

  static getInstance(): NetworkMocker {
    if (!NetworkMocker.instance) {
      NetworkMocker.instance = new NetworkMocker();
    }
    return NetworkMocker.instance;
  }

  setOnline(online: boolean) {
    this.isOnline = online;
    if (online) {
      // Process queued requests
      const queue = [...this.requestQueue];
      this.requestQueue = [];
      queue.forEach(request => request());
    }
  }

  setLatency(ms: number) {
    this.latency = ms;
  }

  setErrorRate(rate: number) {
    this.errorRate = rate;
  }

  async simulateRequest<T>(response: T): Promise<T> {
    if (!this.isOnline) {
      throw new Error('Network unavailable');
    }

    if (this.latency > 0) {
      await new Promise(resolve => setTimeout(resolve, this.latency));
    }

    if (Math.random() < this.errorRate) {
      throw new Error('Network request failed');
    }

    return response;
  }

  queueRequest(request: () => void) {
    if (this.isOnline) {
      request();
    } else {
      this.requestQueue.push(request);
    }
  }

  reset() {
    this.isOnline = true;
    this.latency = 0;
    this.errorRate = 0;
    this.requestQueue = [];
  }
}

// Touch event simulation
export const createTouchEvent = (x: number = 0, y: number = 0) => ({
  nativeEvent: {
    touches: [{ pageX: x, pageY: y }],
    changedTouches: [{ pageX: x, pageY: y }],
  },
});

export const createPanGestureEvent = (translationX: number = 0, translationY: number = 0) => ({
  nativeEvent: {
    translationX,
    translationY,
    velocityX: 0,
    velocityY: 0,
    state: 'ACTIVE',
  },
});

// Animation testing helpers
export const mockAnimationValue = (initialValue: number = 0) => ({
  _value: initialValue,
  setValue: jest.fn(),
  addListener: jest.fn(),
  removeListener: jest.fn(),
  removeAllListeners: jest.fn(),
  stopAnimation: jest.fn(),
  resetAnimation: jest.fn(),
  interpolate: jest.fn(() => initialValue),
});

export const mockAnimatedTiming = () => ({
  start: jest.fn((callback?: () => void) => {
    if (callback) {
      setTimeout(callback, 0);
    }
  }),
  stop: jest.fn(),
  reset: jest.fn(),
});

// WebSocket testing helpers
export class MockWebSocket {
  private listeners: { [key: string]: Array<(data: any) => void> } = {};
  private isConnected = false;

  connect() {
    this.isConnected = true;
    this.emit('connect', {});
  }

  disconnect() {
    this.isConnected = false;
    this.emit('disconnect', {});
  }

  on(event: string, listener: (data: any) => void) {
    if (!this.listeners[event]) {
      this.listeners[event] = [];
    }
    this.listeners[event].push(listener);
  }

  off(event: string, listener: (data: any) => void) {
    if (this.listeners[event]) {
      this.listeners[event] = this.listeners[event].filter(l => l !== listener);
    }
  }

  emit(event: string, data: any) {
    if (this.listeners[event]) {
      this.listeners[event].forEach(listener => listener(data));
    }
  }

  simulateMessage(event: string, data: any) {
    this.emit(event, data);
  }

  isConnectionOpen() {
    return this.isConnected;
  }

  clearListeners() {
    this.listeners = {};
  }
}

// Performance testing
export const measureComponentRender = async (renderFn: () => void): Promise<number> => {
  const start = performance.now();
  renderFn();
  await flushPromises();
  return performance.now() - start;
};

export const measureAsyncOperation = async <T>(operation: () => Promise<T>): Promise<{ result: T; time: number }> => {
  const start = performance.now();
  const result = await operation();
  const time = performance.now() - start;
  return { result, time };
};

// Memory leak detection helpers
export const createMemoryLeakDetector = () => {
  const refs = new Set();

  return {
    track: (ref: any) => refs.add(ref),
    untrack: (ref: any) => refs.delete(ref),
    getActiveRefs: () => refs.size,
    clear: () => refs.clear(),
  };
};

// API response helpers
export const createMockApiResponse = <T>(
  data: T,
  options: {
    status?: number;
    delay?: number;
    shouldFail?: boolean;
  } = {}
) => {
  const { status = 200, delay = 0, shouldFail = false } = options;

  return new Promise<Response>((resolve, reject) => {
    setTimeout(() => {
      if (shouldFail) {
        reject(new Error('Mock API error'));
      } else {
        resolve({
          ok: status >= 200 && status < 300,
          status,
          json: () => Promise.resolve(data),
          text: () => Promise.resolve(JSON.stringify(data)),
        } as Response);
      }
    }, delay);
  });
};

// Storage testing helpers
export const createMockStorage = () => {
  const storage = new Map<string, string>();

  return {
    getItem: jest.fn((key: string) => Promise.resolve(storage.get(key) || null)),
    setItem: jest.fn((key: string, value: string) => {
      storage.set(key, value);
      return Promise.resolve();
    }),
    removeItem: jest.fn((key: string) => {
      storage.delete(key);
      return Promise.resolve();
    }),
    clear: jest.fn(() => {
      storage.clear();
      return Promise.resolve();
    }),
    getAllKeys: jest.fn(() => Promise.resolve(Array.from(storage.keys()))),
    size: () => storage.size,
  };
};

// Form testing helpers
export const fillForm = async (getByTestId: (id: string) => any, formData: { [key: string]: string }) => {
  for (const [fieldId, value] of Object.entries(formData)) {
    const field = getByTestId(fieldId);
    fireEvent.changeText(field, value);
  }
  await flushPromises();
};

// Navigation testing helpers
export const createMockNavigationProp = (overrides: any = {}) => ({
  navigate: jest.fn(),
  goBack: jest.fn(),
  reset: jest.fn(),
  setOptions: jest.fn(),
  dispatch: jest.fn(),
  canGoBack: jest.fn(() => true),
  getId: jest.fn(() => 'test-id'),
  getParent: jest.fn(),
  getState: jest.fn(() => ({
    routes: [{ name: 'Test', key: 'test-key' }],
    index: 0,
  })),
  addListener: jest.fn(),
  removeListener: jest.fn(),
  isFocused: jest.fn(() => true),
  ...overrides,
});

// Error testing helpers
export const expectToThrowAsync = async (asyncFn: () => Promise<any>, errorMessage?: string) => {
  let error: Error | null = null;
  try {
    await asyncFn();
  } catch (e) {
    error = e as Error;
  }

  expect(error).not.toBeNull();
  if (errorMessage) {
    expect(error?.message).toContain(errorMessage);
  }
};

// Accessibility testing helpers
export const checkAccessibility = (component: any) => {
  // Mock accessibility checks
  const accessibilityChecks = {
    hasAccessibilityLabel: !!component.props.accessibilityLabel,
    hasAccessibilityHint: !!component.props.accessibilityHint,
    hasAccessibilityRole: !!component.props.accessibilityRole,
    isAccessibilityElement: component.props.accessible !== false,
  };

  return accessibilityChecks;
};

// Device simulation helpers
export const simulateDeviceRotation = (orientation: 'portrait' | 'landscape') => {
  const dimensions = orientation === 'portrait'
    ? { width: 375, height: 812 }
    : { width: 812, height: 375 };

  jest.requireMock('react-native').Dimensions.get.mockReturnValue(dimensions);
};

export const simulateKeyboardShow = (height: number = 300) => {
  const mockKeyboard = jest.requireMock('react-native').Keyboard;
  const listeners = mockKeyboard.addListener.mock.calls
    .filter(([event]: [string]) => event === 'keyboardDidShow')
    .map(([, callback]: [string, Function]) => callback);

  listeners.forEach(listener => listener({ endCoordinates: { height } }));
};

export const simulateKeyboardHide = () => {
  const mockKeyboard = jest.requireMock('react-native').Keyboard;
  const listeners = mockKeyboard.addListener.mock.calls
    .filter(([event]: [string]) => event === 'keyboardDidHide')
    .map(([, callback]: [string, Function]) => callback);

  listeners.forEach(listener => listener({}));
};

// Test cleanup utilities
export const cleanupAllMocks = () => {
  jest.clearAllMocks();
  jest.clearAllTimers();
  jest.restoreAllMocks();
  NetworkMocker.getInstance().reset();
};

// Export commonly used testing library functions
export { fireEvent } from '@testing-library/react-native';