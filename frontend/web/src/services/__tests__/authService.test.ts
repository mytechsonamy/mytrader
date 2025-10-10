import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import axios from 'axios';
import { authService, LoginRequest, RegisterRequest } from '../authService';
import { mockUser, mockSessionResponse } from '../../test-utils';

// Mock axios
vi.mock('axios', () => ({
  default: {
    post: vi.fn(),
    get: vi.fn(),
    defaults: {
      headers: {
        common: {},
      },
    },
  },
}));

const mockAxios = axios as any;

describe('AuthService', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    // Clear localStorage
    localStorage.clear();

    // Reset axios defaults
    mockAxios.defaults.headers.common = {};

    // Reset service state
    (authService as any).token = null;
    (authService as any).refreshToken = null;
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('Constructor and Initialization', () => {
    it('should initialize with no stored tokens', () => {
      expect(authService.getToken()).toBeNull();
      expect(authService.getRefreshToken()).toBeNull();
      expect(authService.isAuthenticated()).toBe(false);
      expect(authService.getStoredUser()).toBeNull();
    });

    it('should initialize with stored tokens when available', () => {
      const testToken = 'stored-token';
      const testRefreshToken = 'stored-refresh-token';

      localStorage.setItem('authToken', testToken);
      localStorage.setItem('refreshToken', testRefreshToken);
      localStorage.setItem('user', JSON.stringify(mockUser));

      // Create new service instance to test initialization
      const { authService: newAuthService } = require('../authService');

      expect(newAuthService.getToken()).toBe(testToken);
      expect(newAuthService.getRefreshToken()).toBe(testRefreshToken);
      expect(newAuthService.isAuthenticated()).toBe(true);
      expect(newAuthService.getStoredUser()).toEqual(mockUser);
    });

    it('should set authorization header when token exists', () => {
      const testToken = 'test-token';
      localStorage.setItem('authToken', testToken);

      // Reinitialize service
      const { authService: newAuthService } = require('../authService');

      expect(mockAxios.defaults.headers.common['Authorization']).toBe(`Bearer ${testToken}`);
    });
  });

  describe('Login', () => {
    const validCredentials: LoginRequest = {
      email: 'test@example.com',
      password: 'password123',
    };

    it('should successfully login with valid credentials', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });

      const result = await authService.login(validCredentials);

      expect(mockAxios.post).toHaveBeenCalledWith('/api/auth/login', validCredentials);
      expect(result).toEqual(mockSessionResponse);
      expect(consoleSpy).toHaveBeenCalledWith('Attempting login with:', validCredentials.email);

      // Verify tokens are stored
      expect(localStorage.getItem('authToken')).toBe(mockSessionResponse.accessToken);
      expect(localStorage.getItem('refreshToken')).toBe(mockSessionResponse.refreshToken);
      expect(localStorage.getItem('sessionId')).toBe(mockSessionResponse.sessionId);
      expect(localStorage.getItem('user')).toBe(JSON.stringify(mockSessionResponse.user));

      // Verify authorization header is set
      expect(mockAxios.defaults.headers.common['Authorization']).toBe(`Bearer ${mockSessionResponse.accessToken}`);

      consoleSpy.mockRestore();
    });

    it('should handle login failure with server error message', async () => {
      const errorMessage = 'Invalid credentials';
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.post.mockRejectedValue({
        response: {
          data: { message: errorMessage },
        },
      });

      await expect(authService.login(validCredentials)).rejects.toThrow(errorMessage);
      expect(consoleSpy).toHaveBeenCalledWith('Login error:', { message: errorMessage });

      consoleSpy.mockRestore();
    });

    it('should handle login failure with generic error', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.post.mockRejectedValue(new Error('Network error'));

      await expect(authService.login(validCredentials)).rejects.toThrow('Login failed');
      expect(consoleSpy).toHaveBeenCalledWith('Login error:', 'Network error');

      consoleSpy.mockRestore();
    });

    it('should handle login failure without error response', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.post.mockRejectedValue({
        message: 'Request failed',
      });

      await expect(authService.login(validCredentials)).rejects.toThrow('Login failed');

      consoleSpy.mockRestore();
    });

    it('should update service state after successful login', async () => {
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });

      await authService.login(validCredentials);

      expect(authService.getToken()).toBe(mockSessionResponse.accessToken);
      expect(authService.getRefreshToken()).toBe(mockSessionResponse.refreshToken);
      expect(authService.isAuthenticated()).toBe(true);
    });
  });

  describe('Register', () => {
    const validUserData: RegisterRequest = {
      email: 'newuser@example.com',
      password: 'password123',
      firstName: 'New',
      lastName: 'User',
      phone: '+1234567890',
    };

    it('should successfully register with valid data', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });

      const result = await authService.register(validUserData);

      expect(mockAxios.post).toHaveBeenCalledWith('/api/auth/register', validUserData);
      expect(result).toEqual(mockSessionResponse);
      expect(consoleSpy).toHaveBeenCalledWith('Attempting registration with:', validUserData.email);

      // Verify tokens are stored
      expect(localStorage.getItem('authToken')).toBe(mockSessionResponse.accessToken);
      expect(localStorage.getItem('refreshToken')).toBe(mockSessionResponse.refreshToken);

      consoleSpy.mockRestore();
    });

    it('should handle registration failure with server error', async () => {
      const errorMessage = 'Email already exists';
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.post.mockRejectedValue({
        response: {
          data: { message: errorMessage },
        },
      });

      await expect(authService.register(validUserData)).rejects.toThrow(errorMessage);
      expect(consoleSpy).toHaveBeenCalledWith('Registration error:', { message: errorMessage });

      consoleSpy.mockRestore();
    });

    it('should handle registration failure with generic error', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      mockAxios.post.mockRejectedValue(new Error('Network timeout'));

      await expect(authService.register(validUserData)).rejects.toThrow('Registration failed');

      consoleSpy.mockRestore();
    });

    it('should update service state after successful registration', async () => {
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });

      await authService.register(validUserData);

      expect(authService.getToken()).toBe(mockSessionResponse.accessToken);
      expect(authService.isAuthenticated()).toBe(true);
    });
  });

  describe('Logout', () => {
    beforeEach(async () => {
      // Set up authenticated state
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });
      await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });
    });

    it('should clear all authentication data on logout', () => {
      authService.logout();

      expect(authService.getToken()).toBeNull();
      expect(authService.getRefreshToken()).toBeNull();
      expect(authService.isAuthenticated()).toBe(false);
      expect(authService.getStoredUser()).toBeNull();

      // Verify localStorage is cleared
      expect(localStorage.getItem('authToken')).toBeNull();
      expect(localStorage.getItem('refreshToken')).toBeNull();
      expect(localStorage.getItem('sessionId')).toBeNull();
      expect(localStorage.getItem('user')).toBeNull();

      // Verify authorization header is removed
      expect(mockAxios.defaults.headers.common['Authorization']).toBeUndefined();
    });

    it('should handle logout when not authenticated', () => {
      authService.logout();

      // Call logout again
      expect(() => authService.logout()).not.toThrow();
    });
  });

  describe('Health Check', () => {
    it('should successfully perform health check', async () => {
      const healthResponse = {
        status: 'healthy',
        timestamp: '2024-01-01T12:00:00Z',
        version: '1.0.0',
      };

      mockAxios.get.mockResolvedValue({ data: healthResponse });

      const result = await authService.checkHealth();

      expect(mockAxios.get).toHaveBeenCalledWith('/health');
      expect(result).toEqual(healthResponse);
    });

    it('should handle health check failure', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      const testError = new Error('Health check failed');

      mockAxios.get.mockRejectedValue(testError);

      await expect(authService.checkHealth()).rejects.toThrow(testError);
      expect(consoleSpy).toHaveBeenCalledWith('Health check failed:', 'Health check failed');

      consoleSpy.mockRestore();
    });
  });

  describe('Token Management', () => {
    it('should return current token', () => {
      expect(authService.getToken()).toBeNull();

      // Set token directly
      (authService as any).token = 'test-token';
      expect(authService.getToken()).toBe('test-token');
    });

    it('should return current refresh token', () => {
      expect(authService.getRefreshToken()).toBeNull();

      // Set refresh token directly
      (authService as any).refreshToken = 'test-refresh-token';
      expect(authService.getRefreshToken()).toBe('test-refresh-token');
    });

    it('should correctly identify authentication status', () => {
      expect(authService.isAuthenticated()).toBe(false);

      // Set token
      (authService as any).token = 'test-token';
      expect(authService.isAuthenticated()).toBe(true);

      // Remove token
      (authService as any).token = null;
      expect(authService.isAuthenticated()).toBe(false);
    });
  });

  describe('User Data Management', () => {
    it('should return null when no user is stored', () => {
      expect(authService.getStoredUser()).toBeNull();
    });

    it('should return stored user data when available', () => {
      localStorage.setItem('user', JSON.stringify(mockUser));

      expect(authService.getStoredUser()).toEqual(mockUser);
    });

    it('should handle corrupted user data gracefully', () => {
      localStorage.setItem('user', 'invalid-json');

      expect(authService.getStoredUser()).toBeNull();
    });

    it('should handle empty user data', () => {
      localStorage.setItem('user', '');

      expect(authService.getStoredUser()).toBeNull();
    });
  });

  describe('Authorization Header Management', () => {
    it('should set authorization header with token', () => {
      const token = 'test-auth-token';
      (authService as any).setAuthHeader(token);

      expect(mockAxios.defaults.headers.common['Authorization']).toBe(`Bearer ${token}`);
    });

    it('should remove authorization header', () => {
      // First set a header
      mockAxios.defaults.headers.common['Authorization'] = 'Bearer test-token';

      (authService as any).removeAuthHeader();

      expect(mockAxios.defaults.headers.common['Authorization']).toBeUndefined();
    });
  });

  describe('Token Storage and Retrieval', () => {
    it('should store all session data correctly', async () => {
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });

      await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });

      expect(localStorage.getItem('authToken')).toBe(mockSessionResponse.accessToken);
      expect(localStorage.getItem('refreshToken')).toBe(mockSessionResponse.refreshToken);
      expect(localStorage.getItem('sessionId')).toBe(mockSessionResponse.sessionId);

      const storedUser = JSON.parse(localStorage.getItem('user')!);
      expect(storedUser).toEqual(mockSessionResponse.user);
    });

    it('should handle storage errors gracefully', () => {
      // Mock localStorage to throw error
      const originalSetItem = localStorage.setItem;
      localStorage.setItem = vi.fn(() => {
        throw new Error('Storage quota exceeded');
      });

      expect(() => {
        (authService as any).storeTokens(mockSessionResponse);
      }).not.toThrow();

      // Restore original localStorage
      localStorage.setItem = originalSetItem;
    });
  });

  describe('Edge Cases and Error Handling', () => {
    it('should handle axios request without response data', async () => {
      mockAxios.post.mockRejectedValue({
        message: 'Request failed',
        code: 'NETWORK_ERROR',
      });

      await expect(authService.login({
        email: 'test@example.com',
        password: 'password123',
      })).rejects.toThrow('Login failed');
    });

    it('should handle null response from server', async () => {
      mockAxios.post.mockResolvedValue({ data: null });

      const result = await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });

      expect(result).toBeNull();
    });

    it('should handle malformed session response', async () => {
      const malformedResponse = {
        // Missing required fields
        accessToken: 'token',
        // user field is missing
      };

      mockAxios.post.mockResolvedValue({ data: malformedResponse });

      const result = await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });

      expect(result).toEqual(malformedResponse);
      // Service should handle this gracefully even with malformed data
    });

    it('should handle concurrent login attempts', async () => {
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });

      const promises = [
        authService.login({ email: 'test1@example.com', password: 'password1' }),
        authService.login({ email: 'test2@example.com', password: 'password2' }),
        authService.login({ email: 'test3@example.com', password: 'password3' }),
      ];

      const results = await Promise.allSettled(promises);

      // All should succeed
      results.forEach(result => {
        expect(result.status).toBe('fulfilled');
      });
    });

    it('should handle rapid logout after login', async () => {
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });

      await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });

      // Immediate logout
      authService.logout();

      expect(authService.isAuthenticated()).toBe(false);
      expect(authService.getToken()).toBeNull();
    });

    it('should handle browser storage being disabled', () => {
      // Mock localStorage to throw on access
      const originalGetItem = localStorage.getItem;
      localStorage.getItem = vi.fn(() => {
        throw new Error('localStorage is not available');
      });

      expect(() => authService.getStoredUser()).not.toThrow();
      expect(authService.getStoredUser()).toBeNull();

      // Restore original localStorage
      localStorage.getItem = originalGetItem;
    });
  });

  describe('Real-world Scenarios', () => {
    it('should handle session expiry scenario', async () => {
      // Login successfully
      mockAxios.post.mockResolvedValue({ data: mockSessionResponse });
      await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });

      expect(authService.isAuthenticated()).toBe(true);

      // Simulate session expiry by clearing tokens
      authService.logout();

      expect(authService.isAuthenticated()).toBe(false);
    });

    it('should handle network connectivity issues', async () => {
      const networkError = new Error('Network request failed');
      networkError.name = 'NetworkError';

      mockAxios.post.mockRejectedValue(networkError);

      await expect(authService.login({
        email: 'test@example.com',
        password: 'password123',
      })).rejects.toThrow('Login failed');
    });

    it('should handle server maintenance mode', async () => {
      mockAxios.post.mockRejectedValue({
        response: {
          status: 503,
          data: { message: 'Service temporarily unavailable' },
        },
      });

      await expect(authService.login({
        email: 'test@example.com',
        password: 'password123',
      })).rejects.toThrow('Service temporarily unavailable');
    });

    it('should handle rate limiting', async () => {
      mockAxios.post.mockRejectedValue({
        response: {
          status: 429,
          data: { message: 'Too many requests' },
        },
      });

      await expect(authService.login({
        email: 'test@example.com',
        password: 'password123',
      })).rejects.toThrow('Too many requests');
    });
  });
});