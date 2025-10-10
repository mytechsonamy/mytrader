/**
 * API service with Axios and React Query integration
 */

import axios, { AxiosInstance, AxiosResponse, AxiosError } from 'axios';
import { useAuthStore } from '../store/authStore';
import { getApiBaseUrl, getErrorMessage } from '../utils';
import type { ApiResponse, ApiError } from '../types';

// Create axios instance
const createApiInstance = (): AxiosInstance => {
  const api = axios.create({
    baseURL: getApiBaseUrl(),
    timeout: 30000,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // Request interceptor to add auth token
  api.interceptors.request.use(
    (config) => {
      const { token, getAuthHeaders } = useAuthStore.getState();

      if (token) {
        const authHeaders = getAuthHeaders();
        Object.assign(config.headers, authHeaders);
      }

      // Add request timestamp for debugging
      if (import.meta.env.DEV) {
        config.metadata = { requestStartTime: Date.now() };
      }

      return config;
    },
    (error) => {
      return Promise.reject(error);
    }
  );

  // Response interceptor for error handling and token refresh
  api.interceptors.response.use(
    (response: AxiosResponse) => {
      // Log request duration in development
      if (import.meta.env.DEV && response.config.metadata?.requestStartTime) {
        const duration = Date.now() - response.config.metadata.requestStartTime;
        console.log(`[API] ${response.config.method?.toUpperCase()} ${response.config.url} - ${duration}ms`);
      }

      return response;
    },
    async (error: AxiosError) => {
      const originalRequest = error.config;

      // Handle 401 Unauthorized - try to refresh token
      if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
        originalRequest._retry = true;

        try {
          const { refreshToken, isTokenValid } = useAuthStore.getState();

          if (refreshToken && !isTokenValid()) {
            await useAuthStore.getState().refreshToken();

            // Retry the original request with new token
            const newHeaders = useAuthStore.getState().getAuthHeaders();
            originalRequest.headers = { ...originalRequest.headers, ...newHeaders };

            return api(originalRequest);
          }
        } catch (refreshError) {
          // Refresh failed, redirect to login
          useAuthStore.getState().logout();
        }
      }

      // Transform error response
      const apiError: ApiError = {
        message: getErrorMessage(error),
        code: error.code,
        details: error.response?.data,
        timestamp: new Date().toISOString(),
      };

      return Promise.reject(apiError);
    }
  );

  return api;
};

// Export API instance
export const api = createApiInstance();

// API response transformer
const transformResponse = <T>(response: AxiosResponse): ApiResponse<T> => {
  return response.data;
};

// Generic API methods
export const apiService = {
  // GET request
  get: async <T>(url: string, params?: Record<string, any>): Promise<ApiResponse<T>> => {
    const response = await api.get(url, { params });
    return transformResponse<T>(response);
  },

  // POST request
  post: async <T>(url: string, data?: any): Promise<ApiResponse<T>> => {
    const response = await api.post(url, data);
    return transformResponse<T>(response);
  },

  // PUT request
  put: async <T>(url: string, data?: any): Promise<ApiResponse<T>> => {
    const response = await api.put(url, data);
    return transformResponse<T>(response);
  },

  // PATCH request
  patch: async <T>(url: string, data?: any): Promise<ApiResponse<T>> => {
    const response = await api.patch(url, data);
    return transformResponse<T>(response);
  },

  // DELETE request
  delete: async <T>(url: string): Promise<ApiResponse<T>> => {
    const response = await api.delete(url);
    return transformResponse<T>(response);
  },
};

// API endpoints configuration
export const endpoints = {
  // Authentication endpoints
  auth: {
    login: '/api/auth/login',
    register: '/api/auth/register',
    logout: '/api/auth/logout',
    refresh: '/api/auth/refresh',
    profile: '/api/auth/profile',
    changePassword: '/api/auth/change-password',
    resetPassword: '/api/auth/reset-password',
    verifyEmail: '/api/auth/verify-email',
  },

  // Market data endpoints
  market: {
    realtime: (symbolId: string) => `/api/prices/live/${symbolId}`,
    batch: '/api/prices/live',
    overview: '/api/prices/live',
    topMovers: '/api/prices/live',
    topByVolume: '/api/prices/live',
    popular: '/api/prices/live',
    historical: (symbolId: string) => `/api/prices/historical/${symbolId}`,
    statistics: (symbolId: string) => `/api/prices/statistics/${symbolId}`,

    // Asset-specific endpoints
    crypto: '/api/prices/live',
    bist: '/api/prices/live',
    nasdaq: '/api/prices/live',

    // Market status
    status: '/api/health',
    health: '/api/health',
  },

  // Symbols and asset classes
  symbols: {
    list: '/api/v1/symbols',
    search: '/api/v1/symbols/search',
    byAssetClass: (assetClassId: string) => `/api/v1/symbols/by-asset-class/${assetClassId}`,
  },

  assetClasses: {
    list: '/api/asset-classes',
    active: '/api/asset-classes/active',
  },

  markets: {
    list: '/api/v1/markets',
    active: '/api/v1/markets/active',
  },

  // Portfolio endpoints
  portfolio: {
    list: '/api/v1/portfolios',
    create: '/api/v1/portfolios',
    get: (id: string) => `/api/v1/portfolios/${id}`,
    update: (id: string) => `/api/v1/portfolios/${id}`,
    delete: (id: string) => `/api/v1/portfolios/${id}`,
    positions: (id: string) => `/api/v1/portfolios/${id}/positions`,
    performance: (id: string) => `/api/v1/portfolios/${id}/performance`,
  },

  // Trading endpoints
  trading: {
    execute: '/api/v1/trades',
    history: '/api/v1/trades/history',
    pending: '/api/v1/trades/pending',
    cancel: (tradeId: string) => `/api/v1/trades/${tradeId}/cancel`,
  },

  // Competition endpoints
  competition: {
    leaderboard: '/api/v1/competition/leaderboard',
    stats: '/api/v1/competition/stats',
    status: '/api/v1/competition/status',
    join: '/api/v1/competition/join',
    leave: '/api/v1/competition/leave',
  },

  // News endpoints
  news: {
    market: '/api/v1/news/market',
    crypto: '/api/v1/news/crypto',
    stocks: '/api/v1/news/stocks',
    search: '/api/v1/news/search',
  },

  // User endpoints
  user: {
    profile: '/api/v1/user/profile',
    preferences: '/api/v1/user/preferences',
    watchlist: '/api/v1/user/watchlist',
    notifications: '/api/v1/user/notifications',
  },

  // Health and status
  health: '/api/health',
};

// Error types for better error handling
export class ApiError extends Error {
  constructor(
    message: string,
    public status?: number,
    public code?: string,
    public details?: any
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export class NetworkError extends Error {
  constructor(message: string = 'Network connection failed') {
    super(message);
    this.name = 'NetworkError';
  }
}

export class TimeoutError extends Error {
  constructor(message: string = 'Request timed out') {
    super(message);
    this.name = 'TimeoutError';
  }
}

// Export default API instance for direct use
export default api;