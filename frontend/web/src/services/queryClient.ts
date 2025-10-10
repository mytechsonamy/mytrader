/**
 * React Query configuration and client setup
 */

import { QueryClient, DefaultOptions } from '@tanstack/react-query';
import { useNotificationHelpers } from '../store/uiStore';

// Default query options
const defaultQueryOptions: DefaultOptions = {
  queries: {
    // Cache time (how long to keep unused data in cache)
    gcTime: 1000 * 60 * 15, // 15 minutes

    // Stale time (how long data is considered fresh)
    staleTime: 1000 * 60 * 5, // 5 minutes

    // Retry configuration
    retry: (failureCount, error: any) => {
      // Don't retry on 4xx errors (client errors)
      if (error?.status >= 400 && error?.status < 500) {
        return false;
      }
      // Retry up to 3 times for other errors
      return failureCount < 3;
    },

    // Retry delay (exponential backoff)
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),

    // Refetch configuration
    refetchOnWindowFocus: true,
    refetchOnReconnect: true,
    refetchOnMount: true,

    // Network mode
    networkMode: 'online',
  },
  mutations: {
    // Retry configuration for mutations
    retry: 1,
    retryDelay: 1000,

    // Network mode
    networkMode: 'online',
  },
};

// Create query client
export const queryClient = new QueryClient({
  defaultOptions: defaultQueryOptions,
});

// Query keys factory for consistent cache key management
export const queryKeys = {
  // Authentication
  auth: {
    user: () => ['auth', 'user'] as const,
    profile: () => ['auth', 'profile'] as const,
  },

  // Market data
  market: {
    all: () => ['market'] as const,
    overview: () => ['market', 'overview'] as const,
    realtime: (symbolId: string) => ['market', 'realtime', symbolId] as const,
    batch: (symbolIds: string[]) => ['market', 'batch', symbolIds.sort()] as const,
    topMovers: (params?: Record<string, any>) => ['market', 'top-movers', params] as const,
    topByVolume: (params?: Record<string, any>) => ['market', 'top-by-volume', params] as const,
    popular: (params?: Record<string, any>) => ['market', 'popular', params] as const,
    historical: (symbolId: string, params?: Record<string, any>) =>
      ['market', 'historical', symbolId, params] as const,
    statistics: (symbolId: string) => ['market', 'statistics', symbolId] as const,
    status: () => ['market', 'status'] as const,
  },

  // Symbols and assets
  symbols: {
    all: () => ['symbols'] as const,
    list: (params?: Record<string, any>) => ['symbols', 'list', params] as const,
    search: (query: string, params?: Record<string, any>) =>
      ['symbols', 'search', query, params] as const,
    byAssetClass: (assetClassId: string) => ['symbols', 'by-asset-class', assetClassId] as const,
  },

  assetClasses: {
    all: () => ['asset-classes'] as const,
    active: () => ['asset-classes', 'active'] as const,
  },

  markets: {
    all: () => ['markets'] as const,
    active: () => ['markets', 'active'] as const,
  },

  // Portfolio
  portfolio: {
    all: () => ['portfolio'] as const,
    list: () => ['portfolio', 'list'] as const,
    detail: (id: string) => ['portfolio', 'detail', id] as const,
    positions: (id: string) => ['portfolio', 'positions', id] as const,
    performance: (id: string, params?: Record<string, any>) =>
      ['portfolio', 'performance', id, params] as const,
  },

  // Trading
  trading: {
    all: () => ['trading'] as const,
    history: (params?: Record<string, any>) => ['trading', 'history', params] as const,
    pending: () => ['trading', 'pending'] as const,
  },

  // Competition
  competition: {
    all: () => ['competition'] as const,
    leaderboard: (params?: Record<string, any>) => ['competition', 'leaderboard', params] as const,
    stats: () => ['competition', 'stats'] as const,
    status: () => ['competition', 'status'] as const,
  },

  // News
  news: {
    all: () => ['news'] as const,
    market: (params?: Record<string, any>) => ['news', 'market', params] as const,
    crypto: (params?: Record<string, any>) => ['news', 'crypto', params] as const,
    stocks: (params?: Record<string, any>) => ['news', 'stocks', params] as const,
    search: (query: string, params?: Record<string, any>) =>
      ['news', 'search', query, params] as const,
  },

  // User
  user: {
    all: () => ['user'] as const,
    profile: () => ['user', 'profile'] as const,
    preferences: () => ['user', 'preferences'] as const,
    watchlist: () => ['user', 'watchlist'] as const,
    notifications: () => ['user', 'notifications'] as const,
  },
};

// Cache invalidation helpers
export const invalidateQueries = {
  auth: () => queryClient.invalidateQueries({ queryKey: queryKeys.auth.user() }),

  market: {
    all: () => queryClient.invalidateQueries({ queryKey: queryKeys.market.all() }),
    overview: () => queryClient.invalidateQueries({ queryKey: queryKeys.market.overview() }),
    realtime: (symbolId?: string) =>
      symbolId
        ? queryClient.invalidateQueries({ queryKey: queryKeys.market.realtime(symbolId) })
        : queryClient.invalidateQueries({ queryKey: ['market', 'realtime'] }),
    topMovers: () => queryClient.invalidateQueries({ queryKey: ['market', 'top-movers'] }),
  },

  portfolio: {
    all: () => queryClient.invalidateQueries({ queryKey: queryKeys.portfolio.all() }),
    list: () => queryClient.invalidateQueries({ queryKey: queryKeys.portfolio.list() }),
    detail: (id: string) => queryClient.invalidateQueries({ queryKey: queryKeys.portfolio.detail(id) }),
  },

  user: {
    all: () => queryClient.invalidateQueries({ queryKey: queryKeys.user.all() }),
    watchlist: () => queryClient.invalidateQueries({ queryKey: queryKeys.user.watchlist() }),
  },
};

// Prefetch helpers for better UX
export const prefetchQueries = {
  market: {
    overview: () =>
      queryClient.prefetchQuery({
        queryKey: queryKeys.market.overview(),
        queryFn: () => import('../hooks/useMarketData').then(m => m.fetchMarketOverview()),
        staleTime: 1000 * 30, // 30 seconds
      }),

    topMovers: () =>
      queryClient.prefetchQuery({
        queryKey: queryKeys.market.topMovers(),
        queryFn: () => import('../hooks/useMarketData').then(m => m.fetchTopMovers()),
        staleTime: 1000 * 60, // 1 minute
      }),
  },

  portfolio: {
    list: () =>
      queryClient.prefetchQuery({
        queryKey: queryKeys.portfolio.list(),
        queryFn: () => import('../hooks/usePortfolio').then(m => m.fetchPortfolios()),
        staleTime: 1000 * 60 * 2, // 2 minutes
      }),
  },
};

// Error handling for queries
export const handleQueryError = (error: any, context: string) => {
  console.error(`[${context}] Query error:`, error);

  // You can integrate with notification system here
  if (typeof window !== 'undefined') {
    const { showError } = useNotificationHelpers();

    if (error?.status === 401) {
      showError('Authentication Error', 'Please log in to continue');
    } else if (error?.status >= 500) {
      showError('Server Error', 'Something went wrong on our end. Please try again later.');
    } else if (error?.status === 404) {
      showError('Not Found', 'The requested resource was not found');
    } else {
      showError('Error', error?.message || 'An unexpected error occurred');
    }
  }
};

// Query client event listeners for global error handling
queryClient.getQueryCache().subscribe((event) => {
  if (event.type === 'queryAdded') {
    // Query was added to cache
    console.log('[QueryCache] Query added:', event.query.queryKey);
  }
});

queryClient.getMutationCache().subscribe((event) => {
  if (event.type === 'mutationAdded') {
    // Mutation was added to cache
    console.log('[MutationCache] Mutation added:', event.mutation.options.mutationKey);
  }
});

// Development tools integration
if (import.meta.env.DEV) {
  // Add query client to window for debugging
  (window as any).queryClient = queryClient;

  // Log cache statistics
  setInterval(() => {
    const cache = queryClient.getQueryCache();
    console.log('[QueryCache] Stats:', {
      totalQueries: cache.getAll().length,
      activeQueries: cache.getAll().filter(q => q.observers.length > 0).length,
      staleQueries: cache.getAll().filter(q => q.isStale()).length,
    });
  }, 30000); // Log every 30 seconds
}