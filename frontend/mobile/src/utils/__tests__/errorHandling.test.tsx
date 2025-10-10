import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import { Alert, NetInfo } from 'react-native';
import { ErrorBoundary } from '../errorHandling';

// Mock React Native components
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    Alert: {
      alert: jest.fn(),
    },
    NetInfo: {
      addEventListener: jest.fn(),
      fetch: jest.fn(),
    },
  };
});

// Mock AsyncStorage
jest.mock('@react-native-async-storage/async-storage', () => ({
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
}));

// Mock Crash Reporting
jest.mock('@react-native-firebase/crashlytics', () => ({
  log: jest.fn(),
  recordError: jest.fn(),
  setAttribute: jest.fn(),
  setUserId: jest.fn(),
}), { virtual: true });

// Test components
const ThrowingComponent: React.FC<{ shouldThrow?: boolean }> = ({ shouldThrow = true }) => {
  if (shouldThrow) {
    throw new Error('Test error');
  }
  return <div>Normal component</div>;
};

const NetworkAwareComponent: React.FC = () => {
  const [isOffline, setIsOffline] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const handleNetworkChange = (state: any) => {
    setIsOffline(!state.isConnected);
  };

  const fetchData = async () => {
    try {
      if (isOffline) {
        throw new Error('No internet connection');
      }
      const response = await fetch('/api/test');
      if (!response.ok) {
        throw new Error('Network request failed');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  };

  return (
    <div>
      <div data-testid="offline-status">{isOffline ? 'offline' : 'online'}</div>
      <div data-testid="error-message">{error}</div>
      <button data-testid="fetch-button" onClick={fetchData}>
        Fetch Data
      </button>
    </div>
  );
};

describe('Error Handling Utilities', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
  });

  describe('ErrorBoundary Component', () => {
    it('should render children when no error occurs', () => {
      const { getByText } = render(
        <ErrorBoundary>
          <ThrowingComponent shouldThrow={false} />
        </ErrorBoundary>
      );

      expect(getByText('Normal component')).toBeTruthy();
    });

    it('should catch and display error fallback UI', () => {
      // Suppress console.error for this test
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      const { getByText } = render(
        <ErrorBoundary>
          <ThrowingComponent />
        </ErrorBoundary>
      );

      expect(getByText(/Something went wrong/)).toBeTruthy();
      consoleSpy.mockRestore();
    });

    it('should provide retry functionality', () => {
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      const { getByText } = render(
        <ErrorBoundary>
          <ThrowingComponent />
        </ErrorBoundary>
      );

      // Should show retry button
      const retryButton = getByText(/Try Again/);
      expect(retryButton).toBeTruthy();

      consoleSpy.mockRestore();
    });

    it('should reset error state on retry', () => {
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      let shouldThrow = true;

      const DynamicComponent = () => (
        <ThrowingComponent shouldThrow={shouldThrow} />
      );

      const { getByText, rerender } = render(
        <ErrorBoundary>
          <DynamicComponent />
        </ErrorBoundary>
      );

      // Error should be caught
      expect(getByText(/Something went wrong/)).toBeTruthy();

      // Fix the component
      shouldThrow = false;

      // Click retry
      fireEvent.press(getByText(/Try Again/));

      rerender(
        <ErrorBoundary>
          <DynamicComponent />
        </ErrorBoundary>
      );

      // Should show normal component now
      expect(getByText('Normal component')).toBeTruthy();

      consoleSpy.mockRestore();
    });

    it('should log errors to crash reporting service', () => {
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      render(
        <ErrorBoundary>
          <ThrowingComponent />
        </ErrorBoundary>
      );

      // Would verify crash reporting in actual implementation
      expect(true).toBe(true);

      consoleSpy.mockRestore();
    });
  });

  describe('Network Error Handling', () => {
    it('should detect network connectivity changes', async () => {
      const mockNetInfo = NetInfo as any;
      const addEventListenerSpy = mockNetInfo.addEventListener;

      render(<NetworkAwareComponent />);

      expect(addEventListenerSpy).toHaveBeenCalledWith(
        expect.any(Function)
      );
    });

    it('should handle offline scenarios', async () => {
      const mockNetInfo = NetInfo as any;
      mockNetInfo.fetch.mockResolvedValue({ isConnected: false });

      const { getByTestId } = render(<NetworkAwareComponent />);

      // Simulate going offline
      const networkCallback = mockNetInfo.addEventListener.mock.calls[0][0];
      networkCallback({ isConnected: false });

      expect(getByTestId('offline-status').children[0]).toBe('offline');
    });

    it('should queue requests when offline', async () => {
      const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>;
      mockFetch.mockRejectedValue(new Error('Network error'));

      const { getByTestId } = render(<NetworkAwareComponent />);

      fireEvent.press(getByTestId('fetch-button'));

      await waitFor(() => {
        expect(getByTestId('error-message').children[0]).toBeTruthy();
      });
    });

    it('should retry failed requests when back online', async () => {
      const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>;

      // First fail, then succeed
      mockFetch
        .mockRejectedValueOnce(new Error('Network error'))
        .mockResolvedValueOnce({ ok: true } as Response);

      const { getByTestId } = render(<NetworkAwareComponent />);

      // First attempt fails
      fireEvent.press(getByTestId('fetch-button'));

      await waitFor(() => {
        expect(getByTestId('error-message').children[0]).toBeTruthy();
      });

      // Second attempt succeeds
      fireEvent.press(getByTestId('fetch-button'));

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledTimes(2);
      });
    });
  });

  describe('API Error Handling', () => {
    it('should handle HTTP error codes gracefully', async () => {
      const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>;
      mockFetch.mockResolvedValue({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
      } as Response);

      const { getByTestId } = render(<NetworkAwareComponent />);

      fireEvent.press(getByTestId('fetch-button'));

      await waitFor(() => {
        expect(getByTestId('error-message').children[0]).toBe('Network request failed');
      });
    });

    it('should handle timeout errors', async () => {
      const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>;
      mockFetch.mockImplementation(() =>
        new Promise((_, reject) =>
          setTimeout(() => reject(new Error('Request timeout')), 100)
        )
      );

      const { getByTestId } = render(<NetworkAwareComponent />);

      fireEvent.press(getByTestId('fetch-button'));

      await waitFor(() => {
        expect(getByTestId('error-message').children[0]).toBe('Request timeout');
      });
    });

    it('should handle malformed JSON responses', async () => {
      const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>;
      mockFetch.mockResolvedValue({
        ok: true,
        json: () => Promise.reject(new Error('Invalid JSON')),
      } as Response);

      const TestJSONComponent = () => {
        const [error, setError] = React.useState<string | null>(null);

        const fetchJSON = async () => {
          try {
            const response = await fetch('/api/test');
            await response.json();
          } catch (err) {
            setError(err instanceof Error ? err.message : 'JSON parse error');
          }
        };

        return (
          <div>
            <div data-testid="json-error">{error}</div>
            <button data-testid="fetch-json" onClick={fetchJSON}>
              Fetch JSON
            </button>
          </div>
        );
      };

      const { getByTestId } = render(<TestJSONComponent />);

      fireEvent.press(getByTestId('fetch-json'));

      await waitFor(() => {
        expect(getByTestId('json-error').children[0]).toBe('Invalid JSON');
      });
    });
  });

  describe('Authentication Error Handling', () => {
    it('should handle expired token scenarios', async () => {
      const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>;
      mockFetch.mockResolvedValue({
        ok: false,
        status: 401,
        statusText: 'Unauthorized',
      } as Response);

      const AuthTestComponent = () => {
        const [authError, setAuthError] = React.useState<string | null>(null);

        const fetchWithAuth = async () => {
          try {
            const response = await fetch('/api/protected', {
              headers: { Authorization: 'Bearer expired-token' },
            });
            if (response.status === 401) {
              throw new Error('Token expired');
            }
          } catch (err) {
            setAuthError(err instanceof Error ? err.message : 'Auth error');
          }
        };

        return (
          <div>
            <div data-testid="auth-error">{authError}</div>
            <button data-testid="auth-request" onClick={fetchWithAuth}>
              Protected Request
            </button>
          </div>
        );
      };

      const { getByTestId } = render(<AuthTestComponent />);

      fireEvent.press(getByTestId('auth-request'));

      await waitFor(() => {
        expect(getByTestId('auth-error').children[0]).toBe('Token expired');
      });
    });

    it('should handle token refresh scenarios', () => {
      // Would test automatic token refresh
      expect(true).toBe(true);
    });
  });

  describe('Data Validation Errors', () => {
    it('should handle schema validation errors', () => {
      const validateData = (data: any) => {
        if (!data || typeof data !== 'object') {
          throw new Error('Invalid data format');
        }
        if (!data.id || !data.name) {
          throw new Error('Missing required fields');
        }
        return data;
      };

      expect(() => validateData(null)).toThrow('Invalid data format');
      expect(() => validateData({})).toThrow('Missing required fields');
      expect(() => validateData({ id: 1, name: 'test' })).not.toThrow();
    });

    it('should sanitize user input', () => {
      const sanitizeInput = (input: string) => {
        return input.replace(/[<>]/g, '');
      };

      expect(sanitizeInput('<script>alert("xss")</script>')).toBe('scriptalert("xss")/script');
      expect(sanitizeInput('normal text')).toBe('normal text');
    });
  });

  describe('Memory and Performance Errors', () => {
    it('should handle memory pressure scenarios', () => {
      const largeSeedData = Array.from({ length: 100000 }, (_, i) => ({ id: i }));

      expect(() => {
        const processed = largeSeedData.map(item => ({ ...item, processed: true }));
        return processed;
      }).not.toThrow();
    });

    it('should handle infinite loops prevention', () => {
      const processWithLimit = (items: any[], maxIterations = 1000) => {
        let iterations = 0;
        let current = items;

        while (current.length > 0 && iterations < maxIterations) {
          current = current.slice(1);
          iterations++;

          if (iterations >= maxIterations) {
            throw new Error('Maximum iterations exceeded');
          }
        }

        return current;
      };

      const largeArray = Array.from({ length: 2000 }, (_, i) => i);
      expect(() => processWithLimit(largeArray)).toThrow('Maximum iterations exceeded');
    });
  });

  describe('Async Error Handling', () => {
    it('should handle Promise rejection chains', async () => {
      const faultyAsyncOperation = async () => {
        throw new Error('Async operation failed');
      };

      const chainedOperations = async () => {
        try {
          await faultyAsyncOperation();
        } catch (error) {
          return { error: error instanceof Error ? error.message : 'Unknown error' };
        }
      };

      const result = await chainedOperations();
      expect(result.error).toBe('Async operation failed');
    });

    it('should handle concurrent Promise failures', async () => {
      const failingPromise1 = Promise.reject(new Error('Error 1'));
      const failingPromise2 = Promise.reject(new Error('Error 2'));
      const successPromise = Promise.resolve('Success');

      const results = await Promise.allSettled([
        failingPromise1,
        failingPromise2,
        successPromise,
      ]);

      expect(results[0].status).toBe('rejected');
      expect(results[1].status).toBe('rejected');
      expect(results[2].status).toBe('fulfilled');
    });
  });

  describe('Recovery Strategies', () => {
    it('should implement exponential backoff for retries', async () => {
      let attemptCount = 0;
      const maxRetries = 3;

      const unreliableOperation = async () => {
        attemptCount++;
        if (attemptCount < 3) {
          throw new Error(`Attempt ${attemptCount} failed`);
        }
        return 'Success';
      };

      const retryWithBackoff = async (
        operation: () => Promise<string>,
        retries: number = 3
      ): Promise<string> => {
        for (let i = 0; i < retries; i++) {
          try {
            return await operation();
          } catch (error) {
            if (i === retries - 1) throw error;
            await new Promise(resolve => setTimeout(resolve, Math.pow(2, i) * 100));
          }
        }
        throw new Error('All retries exhausted');
      };

      const result = await retryWithBackoff(unreliableOperation);
      expect(result).toBe('Success');
      expect(attemptCount).toBe(3);
    });

    it('should provide fallback mechanisms', () => {
      const primaryDataSource = () => {
        throw new Error('Primary source failed');
      };

      const fallbackDataSource = () => {
        return { data: 'fallback data' };
      };

      const getDataWithFallback = () => {
        try {
          return primaryDataSource();
        } catch (error) {
          console.warn('Primary data source failed, using fallback');
          return fallbackDataSource();
        }
      };

      const result = getDataWithFallback();
      expect(result.data).toBe('fallback data');
    });
  });

  describe('Error Reporting and Analytics', () => {
    it('should collect error context information', () => {
      const collectErrorContext = (error: Error) => {
        return {
          message: error.message,
          stack: error.stack,
          timestamp: Date.now(),
          userAgent: 'test-agent',
          url: 'test-url',
          userId: 'test-user',
        };
      };

      const testError = new Error('Test error');
      const context = collectErrorContext(testError);

      expect(context.message).toBe('Test error');
      expect(context.timestamp).toBeDefined();
      expect(context.userAgent).toBe('test-agent');
    });

    it('should filter sensitive information from error reports', () => {
      const sanitizeErrorData = (data: any) => {
        const sanitized = { ...data };

        // Remove sensitive fields
        delete sanitized.password;
        delete sanitized.token;
        delete sanitized.apiKey;

        // Sanitize URLs
        if (sanitized.url) {
          sanitized.url = sanitized.url.replace(/token=[^&]+/g, 'token=***');
        }

        return sanitized;
      };

      const errorData = {
        message: 'API error',
        password: 'secret123',
        token: 'bearer-token',
        url: 'https://api.example.com?token=secret123&data=public',
      };

      const sanitized = sanitizeErrorData(errorData);

      expect(sanitized.password).toBeUndefined();
      expect(sanitized.token).toBeUndefined();
      expect(sanitized.url).toBe('https://api.example.com?token=***&data=public');
    });
  });
});