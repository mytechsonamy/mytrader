import { http, HttpResponse } from 'msw';
import { mockSessionResponse, mockSymbolsResponse, mockMarketData, mockUser } from '../index';

export const handlers = [
  // Auth endpoints
  http.post('/api/auth/login', async ({ request }) => {
    const body = await request.json() as { email: string; password: string };

    if (body.email === 'test@example.com' && body.password === 'password123') {
      return HttpResponse.json(mockSessionResponse);
    }

    return HttpResponse.json(
      { message: 'Invalid credentials' },
      { status: 401 }
    );
  }),

  http.post('/api/auth/register', async ({ request }) => {
    const body = await request.json() as {
      email: string;
      password: string;
      firstName: string;
      lastName: string;
      phone: string;
    };

    if (body.email === 'existing@example.com') {
      return HttpResponse.json(
        { message: 'User already exists' },
        { status: 409 }
      );
    }

    return HttpResponse.json(mockSessionResponse);
  }),

  // Market data endpoints
  http.get('/api/v1/symbols', () => {
    return HttpResponse.json(mockSymbolsResponse);
  }),

  http.get('/api/market-data/realtime/:symbolId', ({ params }) => {
    const { symbolId } = params;

    if (symbolId === 'INVALID') {
      return HttpResponse.json(
        { message: 'Symbol not found' },
        { status: 404 }
      );
    }

    return HttpResponse.json({
      data: {
        ...mockMarketData,
        symbolId: symbolId as string,
        ticker: symbolId as string,
      }
    });
  }),

  // Health check
  http.get('/health', () => {
    return HttpResponse.json({
      status: 'healthy',
      timestamp: new Date().toISOString(),
      version: '1.0.0'
    });
  }),

  // Protected endpoints (require authentication)
  http.get('/api/protected/*', ({ request }) => {
    const authHeader = request.headers.get('Authorization');

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Authentication required' },
        { status: 401 }
      );
    }

    return HttpResponse.json({ message: 'Protected data' });
  }),

  // Error simulation endpoints
  http.get('/api/error/500', () => {
    return HttpResponse.json(
      { message: 'Internal server error' },
      { status: 500 }
    );
  }),

  http.get('/api/error/network', () => {
    return HttpResponse.error();
  }),
];

// Handlers for specific test scenarios
export const authErrorHandlers = [
  http.post('/api/auth/login', () => {
    return HttpResponse.json(
      { message: 'Authentication service unavailable' },
      { status: 503 }
    );
  }),
];

export const marketDataErrorHandlers = [
  http.get('/api/v1/symbols', () => {
    return HttpResponse.json(
      { message: 'Symbols service unavailable' },
      { status: 503 }
    );
  }),

  http.get('/api/market-data/realtime/:symbolId', () => {
    return HttpResponse.json(
      { message: 'Market data service unavailable' },
      { status: 503 }
    );
  }),
];

export const networkErrorHandlers = [
  http.get('/api/*', () => {
    return HttpResponse.error();
  }),

  http.post('/api/*', () => {
    return HttpResponse.error();
  }),
];