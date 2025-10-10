/**
 * Store hooks - Helper hooks for accessing Zustand stores
 */

import { useAuthStore } from './authStore';
import { useMarketStore } from './marketStore';
import { useUIStore } from './uiStore';

// Re-export store hooks for convenient access
export { useAuthStore, useMarketStore, useUIStore };

// Export specific selectors for convenience
export const useUser = () => useAuthStore((state) => state.user);
export const useIsAuthenticated = () => useAuthStore((state) => state.isAuthenticated);
export const useMarketData = () => useMarketStore((state) => state.marketData);
export const useSidebarOpen = () => useUIStore((state) => state.sidebar.isOpen);