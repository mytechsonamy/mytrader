/**
 * Authentication store using Zustand
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { devtools } from 'zustand/middleware';
import type { User, AuthState, LoginCredentials, RegisterData } from '../types';
import { getStorageItem, setStorageItem, removeStorageItem } from '../utils';

interface AuthStore extends AuthState {
  // Actions
  login: (credentials: LoginCredentials) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => void;
  setGuestMode: () => void;
  clearError: () => void;
  refreshAuthToken: () => Promise<void>;
  updateUser: (user: Partial<User>) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string) => void;

  // Session management
  checkAuthStatus: () => void;
  isTokenValid: () => boolean;
  getAuthHeaders: () => Record<string, string>;
}

const TOKEN_KEY = 'mytrader_token';
const REFRESH_TOKEN_KEY = 'mytrader_refresh_token';
const USER_KEY = 'mytrader_user';

// Token validation helper
const isTokenExpired = (token: string): boolean => {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const currentTime = Date.now() / 1000;
    return payload.exp < currentTime;
  } catch {
    return true;
  }
};

export const useAuthStore = create<AuthStore>()(
  devtools(
    persist(
      (set, get) => ({
        // Initial state
        user: null,
        isAuthenticated: false,
        isLoading: false,
        isGuest: false,
        error: null,
        token: null,
        refreshToken: null,

        // Actions
        login: async (credentials: LoginCredentials) => {
          set({ isLoading: true, error: null });

          try {
            const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5002';
            const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify(credentials),
            });

            if (!response.ok) {
              const errorData = await response.json();
              throw new Error(errorData.message || 'Login failed');
            }

            const data = await response.json();
            // Check if response has nested data structure or direct structure
            const responseData = data.data || data;
            // Map backend response fields to frontend expected fields
            const user = responseData.user || responseData.User;
            const token = responseData.token || responseData.accessToken || responseData.AccessToken;
            const refreshToken = responseData.refreshToken || responseData.RefreshToken;

            // Store tokens
            setStorageItem(TOKEN_KEY, token);
            setStorageItem(REFRESH_TOKEN_KEY, refreshToken);
            setStorageItem(USER_KEY, user);

            set({
              user,
              token,
              refreshToken,
              isAuthenticated: true,
              isGuest: false,
              isLoading: false,
              error: null,
            });
          } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Login failed';
            set({
              isLoading: false,
              error: errorMessage,
              isAuthenticated: false,
              user: null,
              token: null,
              refreshToken: null,
            });
            throw error;
          }
        },

        register: async (data: RegisterData) => {
          set({ isLoading: true, error: null });

          try {
            const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5002';
            const response = await fetch(`${apiBaseUrl}/api/auth/register`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify(data),
            });

            if (!response.ok) {
              const errorData = await response.json();
              throw new Error(errorData.message || 'Registration failed');
            }

            const responseData = await response.json();
            // Check if response has nested data structure or direct structure
            const apiData = responseData.data || responseData;
            // Map backend response fields to frontend expected fields
            const user = apiData.user || apiData.User;
            const token = apiData.token || apiData.accessToken || apiData.AccessToken;
            const refreshToken = apiData.refreshToken || apiData.RefreshToken;

            // Store tokens
            setStorageItem(TOKEN_KEY, token);
            setStorageItem(REFRESH_TOKEN_KEY, refreshToken);
            setStorageItem(USER_KEY, user);

            set({
              user,
              token,
              refreshToken,
              isAuthenticated: true,
              isGuest: false,
              isLoading: false,
              error: null,
            });
          } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Registration failed';
            set({
              isLoading: false,
              error: errorMessage,
              isAuthenticated: false,
              user: null,
              token: null,
              refreshToken: null,
            });
            throw error;
          }
        },

        logout: () => {
          // Clear tokens from storage
          removeStorageItem(TOKEN_KEY);
          removeStorageItem(REFRESH_TOKEN_KEY);
          removeStorageItem(USER_KEY);

          set({
            user: null,
            isAuthenticated: false,
            isGuest: false,
            token: null,
            refreshToken: null,
            error: null,
            isLoading: false,
          });

          // Optional: Call logout endpoint to invalidate token on server
          const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5002';
          fetch(`${apiBaseUrl}/api/auth/logout`, {
            method: 'POST',
            headers: get().getAuthHeaders(),
          }).catch(() => {
            // Ignore errors on logout
          });
        },

        setGuestMode: () => {
          set({
            isGuest: true,
            isAuthenticated: false,
            user: null,
            token: null,
            refreshToken: null,
            error: null,
          });
        },

        clearError: () => {
          set({ error: null });
        },

        refreshAuthToken: async () => {
          const state = get();
          const { refreshToken } = state;
          if (!refreshToken) {
            throw new Error('No refresh token available');
          }

          set({ isLoading: true });

          try {
            const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5002';
            const response = await fetch(`${apiBaseUrl}/api/auth/refresh`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify({ refreshToken }),
            });

            if (!response.ok) {
              throw new Error('Token refresh failed');
            }

            const data = await response.json();
            // Check if response has nested data structure or direct structure
            const apiData = data.data || data;
            // Map backend response fields to frontend expected fields
            const newToken = apiData.token || apiData.accessToken || apiData.AccessToken;
            const newRefreshToken = apiData.refreshToken || apiData.RefreshToken;

            // Update tokens in storage
            setStorageItem(TOKEN_KEY, newToken);
            setStorageItem(REFRESH_TOKEN_KEY, newRefreshToken);

            set({
              token: newToken,
              refreshToken: newRefreshToken,
              isLoading: false,
            });
          } catch (error) {
            // Refresh failed, logout user
            get().logout();
            throw error;
          }
        },

        updateUser: (userUpdate: Partial<User>) => {
          const currentUser = get().user;
          if (currentUser) {
            const updatedUser = { ...currentUser, ...userUpdate };
            setStorageItem(USER_KEY, updatedUser);
            set({ user: updatedUser });
          }
        },

        setLoading: (loading: boolean) => {
          set({ isLoading: loading });
        },

        setError: (error: string) => {
          set({ error });
        },

        // Session management
        checkAuthStatus: () => {
          const token = getStorageItem(TOKEN_KEY, null);
          const refreshTokenValue = getStorageItem(REFRESH_TOKEN_KEY, null);
          const user = getStorageItem(USER_KEY, null);

          if (token && !isTokenExpired(token) && user) {
            set({
              user,
              token,
              refreshToken: refreshTokenValue,
              isAuthenticated: true,
              isGuest: false,
            });
          } else if (refreshTokenValue && !isTokenExpired(refreshTokenValue)) {
            // Try to refresh token
            get().refreshToken().catch(() => {
              // If refresh fails, set guest mode
              get().setGuestMode();
            });
          } else {
            // No valid tokens, set guest mode
            get().setGuestMode();
          }
        },

        isTokenValid: () => {
          const { token } = get();
          return token ? !isTokenExpired(token) : false;
        },

        getAuthHeaders: () => {
          const { token } = get();
          return token
            ? {
                Authorization: `Bearer ${token}`,
                'Content-Type': 'application/json',
              }
            : {
                'Content-Type': 'application/json',
              };
        },
      }),
      {
        name: 'auth-storage',
        storage: createJSONStorage(() => localStorage),
        partialize: (state) => ({
          user: state.user,
          token: state.token,
          refreshToken: state.refreshToken,
          isAuthenticated: state.isAuthenticated,
          isGuest: state.isGuest,
        }),
      }
    ),
    { name: 'AuthStore' }
  )
);

// Selectors for easier access
export const useUser = () => useAuthStore((state) => state.user);
export const useIsAuthenticated = () => useAuthStore((state) => state.isAuthenticated);
export const useIsGuest = () => useAuthStore((state) => state.isGuest);
export const useAuthLoading = () => useAuthStore((state) => state.isLoading);
export const useAuthError = () => useAuthStore((state) => state.error);

// Auth actions
export const useAuthActions = () => useAuthStore((state) => ({
  login: state.login,
  register: state.register,
  logout: state.logout,
  setGuestMode: state.setGuestMode,
  clearError: state.clearError,
  refreshToken: state.refreshToken,
  updateUser: state.updateUser,
  checkAuthStatus: state.checkAuthStatus,
  getAuthHeaders: state.getAuthHeaders,
}));