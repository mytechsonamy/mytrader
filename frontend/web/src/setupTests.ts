// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom';
import { vi } from 'vitest';

// Mock environment variables for tests
Object.defineProperty(import.meta, 'env', {
  value: {
    VITE_BACKEND_URL: 'http://localhost:5002',
    VITE_WEBSOCKET_HUB_PATH: '/hubs/market-data',
    VITE_API_VERSION: 'v1',
    VITE_DEBUG_WEBSOCKET: 'false',
    VITE_ENABLE_MOCK_DATA: 'false',
    MODE: 'test',
    DEV: false,
    PROD: false,
    SSR: false,
    BASE_URL: '/',
  },
  writable: true,
});

// Mock WebSocket for tests
global.WebSocket = class MockWebSocket {
  constructor(public url: string) {}
  close() {}
  send() {}
  addEventListener() {}
  removeEventListener() {}
} as any;

// Mock ResizeObserver
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // deprecated
    removeListener: vi.fn(), // deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});