import { describe, it, expect, beforeEach, vi, beforeAll, afterAll } from 'vitest';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { authService } from '../services/authService';
import { marketDataService } from '../services/marketDataService';
import { mockSessionResponse, mockSymbolsResponse, mockMarketData } from './index';

// Create a test server with MSW
const testServer = setupServer();

// API Integration Tests
describe('API Integration Tests', () => {
  beforeAll(() => {
    testServer.listen({ onUnhandledRequest: 'error' });
  });

  afterAll(() => {
    testServer.close();
  });

  beforeEach(() => {
    testServer.resetHandlers();
    localStorage.clear();
  });

  describe('Authentication API Integration', () => {
    describe('Login Flow', () => {
      it('should successfully authenticate user with valid credentials', async () => {
        testServer.use(
          http.post('/api/auth/login', async ({ request }) => {
            const body = await request.json() as any;

            if (body.email === 'test@example.com' && body.password === 'password123') {
              return HttpResponse.json(mockSessionResponse);
            }

            return HttpResponse.json(
              { message: 'Invalid credentials' },
              { status: 401 }
            );
          })
        );

        const result = await authService.login({
          email: 'test@example.com',
          password: 'password123',
        });

        expect(result).toEqual(mockSessionResponse);
        expect(authService.isAuthenticated()).toBe(true);
        expect(authService.getToken()).toBe(mockSessionResponse.accessToken);
        expect(localStorage.getItem('authToken')).toBe(mockSessionResponse.accessToken);
      });

      it('should reject invalid credentials', async () => {
        testServer.use(
          http.post('/api/auth/login', () => {
            return HttpResponse.json(
              { message: 'Invalid email or password' },
              { status: 401 }
            );
          })
        );

        await expect(authService.login({
          email: 'wrong@example.com',
          password: 'wrongpassword',
        })).rejects.toThrow('Invalid email or password');

        expect(authService.isAuthenticated()).toBe(false);
      });

      it('should handle server errors during login', async () => {
        testServer.use(
          http.post('/api/auth/login', () => {
            return HttpResponse.json(
              { message: 'Internal server error' },
              { status: 500 }
            );
          })
        );

        await expect(authService.login({
          email: 'test@example.com',
          password: 'password123',
        })).rejects.toThrow('Internal server error');
      });

      it('should handle network errors during login', async () => {
        testServer.use(
          http.post('/api/auth/login', () => {
            return HttpResponse.error();
          })
        );

        await expect(authService.login({
          email: 'test@example.com',
          password: 'password123',
        })).rejects.toThrow('Login failed');
      });
    });

    describe('Registration Flow', () => {
      it('should successfully register new user', async () => {
        testServer.use(
          http.post('/api/auth/register', async ({ request }) => {
            const body = await request.json() as any;

            if (body.email !== 'existing@example.com') {
              return HttpResponse.json(mockSessionResponse);
            }

            return HttpResponse.json(
              { message: 'User already exists' },
              { status: 409 }
            );
          })
        );

        const userData = {
          email: 'newuser@example.com',
          password: 'password123',
          firstName: 'New',
          lastName: 'User',
          phone: '+1234567890',
        };

        const result = await authService.register(userData);

        expect(result).toEqual(mockSessionResponse);
        expect(authService.isAuthenticated()).toBe(true);
      });

      it('should reject registration with existing email', async () => {
        testServer.use(
          http.post('/api/auth/register', () => {
            return HttpResponse.json(
              { message: 'Email already registered' },
              { status: 409 }
            );
          })
        );

        await expect(authService.register({
          email: 'existing@example.com',
          password: 'password123',
          firstName: 'Existing',
          lastName: 'User',
          phone: '+1234567890',
        })).rejects.toThrow('Email already registered');
      });
    });

    describe('Health Check', () => {
      it('should successfully check service health', async () => {
        testServer.use(
          http.get('/health', () => {
            return HttpResponse.json({
              status: 'healthy',
              timestamp: '2024-01-01T12:00:00Z',
              version: '1.0.0',
            });
          })
        );

        const result = await authService.checkHealth();

        expect(result.status).toBe('healthy');
        expect(result.version).toBe('1.0.0');
      });

      it('should handle unhealthy service status', async () => {
        testServer.use(
          http.get('/health', () => {
            return HttpResponse.json(
              { status: 'unhealthy', reason: 'Database connection failed' },
              { status: 503 }
            );
          })
        );

        await expect(authService.checkHealth()).rejects.toThrow();
      });
    });
  });

  describe('Market Data API Integration', () => {
    describe('Symbols Endpoint', () => {
      it('should fetch symbols successfully', async () => {
        testServer.use(
          http.get('/api/v1/symbols', () => {
            return HttpResponse.json(mockSymbolsResponse);
          })
        );

        const result = await marketDataService.getSymbols();

        expect(result).toEqual(mockSymbolsResponse);
        expect(result.symbols).toHaveProperty('BTCUSDT');
        expect(result.symbols).toHaveProperty('ETHUSDT');
      });

      it('should handle symbols endpoint failure', async () => {
        testServer.use(
          http.get('/api/v1/symbols', () => {
            return HttpResponse.json(
              { message: 'Symbols service temporarily unavailable' },
              { status: 503 }
            );
          })
        );

        await expect(marketDataService.getSymbols()).rejects.toThrow('Symbols service temporarily unavailable');
      });

      it('should handle empty symbols response', async () => {
        testServer.use(
          http.get('/api/v1/symbols', () => {
            return HttpResponse.json({
              symbols: {},
              interval: '1m',
            });
          })
        );

        const result = await marketDataService.getSymbols();

        expect(result.symbols).toEqual({});
        expect(result.interval).toBe('1m');
      });
    });

    describe('Real-time Market Data Endpoint', () => {
      it('should fetch market data for valid symbol', async () => {
        testServer.use(
          http.get('/api/market-data/realtime/:symbolId', ({ params }) => {
            const { symbolId } = params;

            if (symbolId === 'BTCUSDT') {
              return HttpResponse.json({
                data: mockMarketData,
              });
            }

            return HttpResponse.json(
              { message: 'Symbol not found' },
              { status: 404 }
            );
          })
        );

        const result = await marketDataService.getMarketData('BTCUSDT');

        expect(result).toEqual(mockMarketData);
        expect(result.symbolId).toBe('BTCUSDT');
        expect(result.price).toBe(50000);
      });

      it('should handle invalid symbol requests', async () => {
        testServer.use(
          http.get('/api/market-data/realtime/:symbolId', () => {
            return HttpResponse.json(
              { message: 'Symbol not found' },
              { status: 404 }
            );
          })
        );

        await expect(marketDataService.getMarketData('INVALID_SYMBOL')).rejects.toThrow('Symbol not found');
      });

      it('should fetch multiple symbols concurrently', async () => {
        testServer.use(
          http.get('/api/market-data/realtime/:symbolId', ({ params }) => {
            const { symbolId } = params;
            return HttpResponse.json({
              data: {
                ...mockMarketData,
                symbolId: symbolId as string,
                ticker: symbolId as string,
                price: symbolId === 'BTCUSDT' ? 50000 : 3000,
              },
            });
          })
        );

        const symbolIds = ['BTCUSDT', 'ETHUSDT'];
        const result = await marketDataService.getMultipleMarketData(symbolIds);

        expect(result).toHaveLength(2);
        expect(result[0].symbolId).toBe('BTCUSDT');
        expect(result[0].price).toBe(50000);
        expect(result[1].symbolId).toBe('ETHUSDT');
        expect(result[1].price).toBe(3000);
      });

      it('should handle partial failures in multiple symbol requests', async () => {
        testServer.use(
          http.get('/api/market-data/realtime/BTCUSDT', () => {
            return HttpResponse.json({ data: mockMarketData });
          }),
          http.get('/api/market-data/realtime/INVALID', () => {
            return HttpResponse.json(
              { message: 'Symbol not found' },
              { status: 404 }
            );
          }),
          http.get('/api/market-data/realtime/ETHUSDT', () => {
            return HttpResponse.json({
              data: { ...mockMarketData, symbolId: 'ETHUSDT' },
            });
          })
        );

        const symbolIds = ['BTCUSDT', 'INVALID', 'ETHUSDT'];
        const result = await marketDataService.getMultipleMarketData(symbolIds);

        // Should return only successful results
        expect(result).toHaveLength(2);
        expect(result[0].symbolId).toBe('BTCUSDT');
        expect(result[1].symbolId).toBe('ETHUSDT');
      });
    });
  });

  describe('Protected Endpoint Integration', () => {
    beforeEach(async () => {
      // Set up authenticated state
      testServer.use(
        http.post('/api/auth/login', () => {
          return HttpResponse.json(mockSessionResponse);
        })
      );

      await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });
    });

    it('should access protected endpoints with valid token', async () => {
      testServer.use(
        http.get('/api/protected/portfolio', ({ request }) => {
          const authHeader = request.headers.get('Authorization');

          if (authHeader === `Bearer ${mockSessionResponse.accessToken}`) {
            return HttpResponse.json({
              totalValue: 10000,
              positions: [],
            });
          }

          return HttpResponse.json(
            { message: 'Unauthorized' },
            { status: 401 }
          );
        })
      );

      // Mock a protected API call
      const response = await fetch('/api/protected/portfolio', {
        headers: {
          'Authorization': `Bearer ${authService.getToken()}`,
        },
      });

      expect(response.ok).toBe(true);
      const data = await response.json();
      expect(data.totalValue).toBe(10000);
    });

    it('should reject protected endpoints without token', async () => {
      testServer.use(
        http.get('/api/protected/portfolio', ({ request }) => {
          const authHeader = request.headers.get('Authorization');

          if (!authHeader) {
            return HttpResponse.json(
              { message: 'Authentication required' },
              { status: 401 }
            );
          }

          return HttpResponse.json({ data: 'protected data' });
        })
      );

      // Logout to remove token
      authService.logout();

      const response = await fetch('/api/protected/portfolio');

      expect(response.status).toBe(401);
      const data = await response.json();
      expect(data.message).toBe('Authentication required');
    });

    it('should handle token expiry scenarios', async () => {
      testServer.use(
        http.get('/api/protected/portfolio', () => {
          return HttpResponse.json(
            { message: 'Token expired' },
            { status: 401 }
          );
        })
      );

      const response = await fetch('/api/protected/portfolio', {
        headers: {
          'Authorization': `Bearer expired-token`,
        },
      });

      expect(response.status).toBe(401);
      const data = await response.json();
      expect(data.message).toBe('Token expired');
    });
  });

  describe('Cross-Service Integration', () => {
    it('should handle complete user journey from registration to market data access', async () => {
      // Step 1: Health check
      testServer.use(
        http.get('/health', () => {
          return HttpResponse.json({ status: 'healthy' });
        })
      );

      const healthResult = await authService.checkHealth();
      expect(healthResult.status).toBe('healthy');

      // Step 2: Registration
      testServer.use(
        http.post('/api/auth/register', () => {
          return HttpResponse.json(mockSessionResponse);
        })
      );

      const registerResult = await authService.register({
        email: 'journey@example.com',
        password: 'password123',
        firstName: 'Test',
        lastName: 'User',
        phone: '+1234567890',
      });

      expect(registerResult).toEqual(mockSessionResponse);
      expect(authService.isAuthenticated()).toBe(true);

      // Step 3: Fetch symbols (public endpoint)
      testServer.use(
        http.get('/api/v1/symbols', () => {
          return HttpResponse.json(mockSymbolsResponse);
        })
      );

      const symbolsResult = await marketDataService.getSymbols();
      expect(symbolsResult).toEqual(mockSymbolsResponse);

      // Step 4: Fetch market data (public endpoint)
      testServer.use(
        http.get('/api/market-data/realtime/BTCUSDT', () => {
          return HttpResponse.json({ data: mockMarketData });
        })
      );

      const marketDataResult = await marketDataService.getMarketData('BTCUSDT');
      expect(marketDataResult).toEqual(mockMarketData);

      // Step 5: Access protected endpoint
      testServer.use(
        http.get('/api/protected/trading', ({ request }) => {
          const authHeader = request.headers.get('Authorization');

          if (authHeader === `Bearer ${mockSessionResponse.accessToken}`) {
            return HttpResponse.json({
              canTrade: true,
              tradingLimits: { daily: 10000 },
            });
          }

          return HttpResponse.json(
            { message: 'Unauthorized' },
            { status: 401 }
          );
        })
      );

      const tradingResponse = await fetch('/api/protected/trading', {
        headers: {
          'Authorization': `Bearer ${authService.getToken()}`,
        },
      });

      expect(tradingResponse.ok).toBe(true);
      const tradingData = await tradingResponse.json();
      expect(tradingData.canTrade).toBe(true);
    });

    it('should handle guest user journey (public access only)', async () => {
      // Guest user should access public endpoints without authentication

      // Step 1: Fetch symbols as guest
      testServer.use(
        http.get('/api/v1/symbols', () => {
          return HttpResponse.json(mockSymbolsResponse);
        })
      );

      const symbolsResult = await marketDataService.getSymbols();
      expect(symbolsResult).toEqual(mockSymbolsResponse);

      // Step 2: Fetch market data as guest
      testServer.use(
        http.get('/api/market-data/realtime/BTCUSDT', () => {
          return HttpResponse.json({ data: mockMarketData });
        })
      );

      const marketDataResult = await marketDataService.getMarketData('BTCUSDT');
      expect(marketDataResult).toEqual(mockMarketData);

      // Step 3: Attempt to access protected endpoint should fail
      testServer.use(
        http.get('/api/protected/trading', () => {
          return HttpResponse.json(
            { message: 'Authentication required' },
            { status: 401 }
          );
        })
      );

      const tradingResponse = await fetch('/api/protected/trading');
      expect(tradingResponse.status).toBe(401);
    });
  });

  describe('Error Recovery and Resilience', () => {
    it('should handle transient network failures with retry logic', async () => {
      let attemptCount = 0;

      testServer.use(
        http.get('/api/v1/symbols', () => {
          attemptCount++;

          if (attemptCount <= 2) {
            return HttpResponse.error();
          }

          return HttpResponse.json(mockSymbolsResponse);
        })
      );

      // This would require implementing retry logic in the service
      // For now, we test that it eventually fails
      await expect(marketDataService.getSymbols()).rejects.toThrow();
    });

    it('should handle API rate limiting gracefully', async () => {
      testServer.use(
        http.get('/api/v1/symbols', () => {
          return HttpResponse.json(
            {
              message: 'Rate limit exceeded',
              retryAfter: 60,
            },
            {
              status: 429,
              headers: {
                'Retry-After': '60',
              },
            }
          );
        })
      );

      await expect(marketDataService.getSymbols()).rejects.toThrow('Rate limit exceeded');
    });

    it('should handle server maintenance windows', async () => {
      testServer.use(
        http.get('/api/v1/symbols', () => {
          return HttpResponse.json(
            {
              message: 'Service temporarily unavailable for maintenance',
              estimatedDowntime: '30 minutes',
            },
            { status: 503 }
          );
        })
      );

      await expect(marketDataService.getSymbols()).rejects.toThrow('Service temporarily unavailable for maintenance');
    });

    it('should handle partial service degradation', async () => {
      // Symbols endpoint works, but market data is degraded
      testServer.use(
        http.get('/api/v1/symbols', () => {
          return HttpResponse.json(mockSymbolsResponse);
        }),
        http.get('/api/market-data/realtime/:symbolId', () => {
          return HttpResponse.json(
            { message: 'Market data service degraded' },
            { status: 503 }
          );
        })
      );

      // Symbols should work
      const symbolsResult = await marketDataService.getSymbols();
      expect(symbolsResult).toEqual(mockSymbolsResponse);

      // Market data should fail
      await expect(marketDataService.getMarketData('BTCUSDT')).rejects.toThrow('Market data service degraded');
    });
  });

  describe('Data Consistency and Validation', () => {
    it('should validate API response schemas', async () => {
      testServer.use(
        http.get('/api/v1/symbols', () => {
          return HttpResponse.json({
            // Missing required fields
            symbols: {},
            // interval field is missing
          });
        })
      );

      const result = await marketDataService.getSymbols();

      // Service should handle malformed data gracefully
      expect(result.symbols).toEqual({});
      expect(result.interval).toBeUndefined();
    });

    it('should handle data type mismatches', async () => {
      testServer.use(
        http.get('/api/market-data/realtime/BTCUSDT', () => {
          return HttpResponse.json({
            data: {
              ...mockMarketData,
              price: "50000", // String instead of number
              volume: null, // Null instead of number
            },
          });
        })
      );

      const result = await marketDataService.getMarketData('BTCUSDT');

      // Service should handle type mismatches gracefully
      expect(result.price).toBe("50000");
      expect(result.volume).toBe(null);
    });

    it('should handle missing nested data fields', async () => {
      testServer.use(
        http.get('/api/market-data/realtime/BTCUSDT', () => {
          return HttpResponse.json({
            // Missing 'data' wrapper
            symbolId: 'BTCUSDT',
            price: 50000,
          });
        })
      );

      const result = await marketDataService.getMarketData('BTCUSDT');

      // Should return undefined when data field is missing
      expect(result).toBeUndefined();
    });
  });
});