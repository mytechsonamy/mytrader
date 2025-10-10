/**
 * Market data hooks using WebSocket for real-time updates
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';
import { apiService, endpoints } from '../services/api';
import { queryKeys, handleQueryError } from '../services/queryClient';
import { useMarketStore } from '../store/marketStore';
import { normalizeMarketData } from '../utils/priceFormatting';
import { useWebSocketPrices } from '../context/WebSocketPriceContext';
import type { MarketData, TopMover, Symbol, AssetClass } from '../types';

// Default symbols to subscribe to
const DEFAULT_SYMBOLS = ['BTCUSDT', 'ETHUSDT', 'XRPUSDT', 'BNBUSDT', 'SOLUSDT', 'UNIUSDT', 'AVAXUSDT', 'SUIUSDT', 'ENAUSDT'];

// Market overview hook - now using WebSocket for real-time updates
export const useMarketOverview = () => {
  const setMarketData = useMarketStore((state) => state.setMarketData);
  const { prices, subscribeToSymbol, isConnected } = useWebSocketPrices();

  // Subscribe to default symbols when component mounts and WebSocket is connected
  useEffect(() => {
    if (isConnected) {
      console.log('[useMarketOverview] WebSocket connected, subscribing to symbols:', DEFAULT_SYMBOLS);
      DEFAULT_SYMBOLS.forEach(symbol => {
        subscribeToSymbol(symbol).catch(err => {
          console.error(`[useMarketOverview] Failed to subscribe to ${symbol}:`, err);
        });
      });
    }
  }, [isConnected, subscribeToSymbol]);

  // Update Zustand store whenever prices change from WebSocket
  useEffect(() => {
    if (Object.keys(prices).length > 0) {
      const transformedData: Record<string, MarketData> = {};

      Object.entries(prices).forEach(([symbol, data]) => {
        // Remove 'USDT' suffix for cleaner display (e.g., BTCUSDT -> BTC)
        const cleanSymbol = symbol.replace('USDT', '');

        transformedData[cleanSymbol] = {
          symbolId: cleanSymbol,
          symbol: cleanSymbol,
          name: getSymbolName(cleanSymbol),
          price: data.price,
          change: data.change,
          changePercent: data.changePercent,
          volume: data.volume,
          high: data.price, // WebSocket doesn't provide high/low for now
          low: data.price,
          open: data.price,
          timestamp: data.lastUpdate,
          marketStatus: 'OPEN' as const,
        };
      });

      setMarketData(transformedData);
    }
  }, [prices, setMarketData]);

  // Return a query-like structure for backward compatibility
  return {
    data: prices,
    isLoading: !isConnected,
    isError: false,
    error: null,
  };
};

// Real-time market data for a specific symbol
export const useRealTimeMarketData = (symbolId: string, enabled = true) => {
  const updateMarketData = useMarketStore((state) => state.updateMarketData);

  return useQuery({
    queryKey: queryKeys.market.realtime(symbolId),
    queryFn: async () => {
      const response = await apiService.get<MarketData>(endpoints.market.realtime(symbolId));

      // Update Zustand store
      if (response.data) {
        updateMarketData(symbolId, response.data);
      }

      return response.data;
    },
    enabled: enabled && !!symbolId,
    staleTime: 1000 * 10, // 10 seconds
    refetchInterval: 1000 * 15, // Refetch every 15 seconds
    onError: (error) => handleQueryError(error, `Real-time data for ${symbolId}`),
  });
};

// Batch market data for multiple symbols
export const useBatchMarketData = (symbolIds: string[], enabled = true) => {
  const batchUpdateMarketData = useMarketStore((state) => state.batchUpdateMarketData);

  return useQuery({
    queryKey: queryKeys.market.batch(symbolIds),
    queryFn: async () => {
      const response = await apiService.post<MarketData[]>(endpoints.market.batch, {
        symbolIds,
      });

      // Update Zustand store
      if (response.data) {
        batchUpdateMarketData(response.data);
      }

      return response.data;
    },
    enabled: enabled && symbolIds.length > 0,
    staleTime: 1000 * 30, // 30 seconds
    refetchInterval: 1000 * 60, // Refetch every minute
    onError: (error) => handleQueryError(error, 'Batch market data'),
  });
};

// Top movers by asset class
export const useTopMovers = (params?: { assetClass?: string; limit?: number }) => {
  const setTopMovers = useMarketStore((state) => state.setTopMovers);

  return useQuery({
    queryKey: queryKeys.market.topMovers(params),
    queryFn: async () => {
      try {
        // First try to get live prices and use them to create top movers
        const pricesResponse = await apiService.get<any>(endpoints.market.overview);

        if (pricesResponse.symbols) {
          // Create top movers from live price data
          const topMovers: TopMover[] = Object.entries(pricesResponse.symbols)
            .map(([symbol, data]: [string, any]) => {
              // Normalize price data to handle incorrect scaling
              const normalized = normalizeMarketData(data, data.assetClass || 'CRYPTO');

              return {
                symbol: symbol,
                name: getSymbolName(symbol),
                price: normalized.price || 0,
                change: normalized.change || ((normalized.changePercent || 0) / 100) * (normalized.price || 0),
                changePercent: normalized.changePercent || 0,
                volume: normalized.volume || 1000000,
                assetClass: 'CRYPTO',
              };
            })
            .sort((a, b) => Math.abs(b.changePercent) - Math.abs(a.changePercent)) // Sort by biggest movers
            .slice(0, params?.limit || 10);

          setTopMovers(topMovers);
          return topMovers;
        }

        return [];
      } catch (error) {
        console.error('Top movers error:', error);
        return [];
      }
    },
    staleTime: 1000 * 60, // 1 minute
    refetchInterval: 1000 * 60 * 2, // Refetch every 2 minutes
    onError: (error) => handleQueryError(error, 'Top movers'),
  });
};

// Helper function to get full name for symbols
const getSymbolName = (symbol: string): string => {
  const names: Record<string, string> = {
    'BTC': 'Bitcoin',
    'ETH': 'Ethereum',
    'ADA': 'Cardano',
    'SOL': 'Solana',
    'DOT': 'Polkadot',
    'LINK': 'Chainlink',
    'AVAX': 'Avalanche',
    'MATIC': 'Polygon',
    'UNI': 'Uniswap',
    'ATOM': 'Cosmos',
  };
  return names[symbol] || symbol;
};

// Popular symbols
export const usePopularSymbols = (limit = 20) => {
  return useQuery({
    queryKey: queryKeys.market.popular({ limit }),
    queryFn: async () => {
      const response = await apiService.get<Symbol[]>(endpoints.market.popular, { limit });
      return response.data;
    },
    staleTime: 1000 * 60 * 5, // 5 minutes
    onError: (error) => handleQueryError(error, 'Popular symbols'),
  });
};

// Historical data for a symbol
export const useHistoricalData = (
  symbolId: string,
  params?: {
    interval?: string;
    startTime?: string;
    endTime?: string;
  },
  enabled = true
) => {
  return useQuery({
    queryKey: queryKeys.market.historical(symbolId, params),
    queryFn: async () => {
      const response = await apiService.get<any[]>(endpoints.market.historical(symbolId), params);
      return response.data;
    },
    enabled: enabled && !!symbolId,
    staleTime: 1000 * 60 * 5, // 5 minutes
    onError: (error) => handleQueryError(error, `Historical data for ${symbolId}`),
  });
};

// Symbol search
export const useSymbolSearch = (query: string, params?: { assetClass?: string; limit?: number }) => {
  return useQuery({
    queryKey: queryKeys.symbols.search(query, params),
    queryFn: async () => {
      const response = await apiService.get<Symbol[]>(endpoints.symbols.search, {
        q: query,
        ...params,
      });
      return response.data;
    },
    enabled: query.length >= 2, // Only search when query has at least 2 characters
    staleTime: 1000 * 60 * 2, // 2 minutes
    onError: (error) => handleQueryError(error, 'Symbol search'),
  });
};

// Asset classes
export const useAssetClasses = () => {
  const setAssetClasses = useMarketStore((state) => state.setAssetClasses);

  return useQuery({
    queryKey: queryKeys.assetClasses.active(),
    queryFn: async () => {
      try {
        const response = await apiService.get<any[]>(endpoints.assetClasses.active);

        if (response.data) {
          // Transform backend response to match frontend types
          const transformedData = response.data.map((item: any) => ({
            id: item.id,
            name: item.name,
            displayName: item.nameTurkish || item.name,
            icon: getAssetClassIcon(item.code),
            isActive: item.isActive,
            sortOrder: item.displayOrder || 0,
          }));

          setAssetClasses(transformedData);
          return transformedData;
        }

        return [];
      } catch (error) {
        console.error('Asset classes error:', error);
        // Return default asset classes if API is not available
        return [
          { id: 'crypto', name: 'Cryptocurrency', displayName: 'Kripto Para', icon: '₿', isActive: true, sortOrder: 1 },
          { id: 'stock', name: 'Stocks', displayName: 'Hisse Senetleri', icon: '📈', isActive: true, sortOrder: 2 },
        ];
      }
    },
    staleTime: 1000 * 60 * 10, // 10 minutes (asset classes don't change often)
    onError: (error) => handleQueryError(error, 'Asset classes'),
  });
};

// Helper function to get icon for asset class
const getAssetClassIcon = (code: string): string => {
  const icons: Record<string, string> = {
    'CRYPTO': '₿',
    'STOCK': '📈',
    'STOCK_BIST': '🇹🇷',
    'FOREX': '💱',
    'COMMODITY': '🥇',
    'ETF': '📊',
  };
  return icons[code] || '💼';
};

// Symbols by asset class
export const useSymbolsByAssetClass = (assetClassId: string, enabled = true) => {
  const setSymbols = useMarketStore((state) => state.setSymbols);

  return useQuery({
    queryKey: queryKeys.symbols.byAssetClass(assetClassId),
    queryFn: async () => {
      const response = await apiService.get<Symbol[]>(endpoints.symbols.byAssetClass(assetClassId));

      // Update Zustand store if this is the first load
      if (response.data) {
        setSymbols(response.data);
      }

      return response.data;
    },
    enabled: enabled && !!assetClassId,
    staleTime: 1000 * 60 * 5, // 5 minutes
    onError: (error) => handleQueryError(error, `Symbols for asset class ${assetClassId}`),
  });
};

// Market status
export const useMarketStatus = () => {
  const setMarketStatus = useMarketStore((state) => state.setMarketStatus);

  return useQuery({
    queryKey: queryKeys.market.status(),
    queryFn: async () => {
      const response = await apiService.get<Record<string, string>>(endpoints.market.status);

      // Update Zustand store
      if (response.data) {
        Object.entries(response.data).forEach(([market, status]) => {
          setMarketStatus(market, status);
        });
      }

      return response.data;
    },
    staleTime: 1000 * 60, // 1 minute
    refetchInterval: 1000 * 60 * 2, // Refetch every 2 minutes
    onError: (error) => handleQueryError(error, 'Market status'),
  });
};

// Watchlist management mutations
export const useWatchlistMutations = () => {
  const queryClient = useQueryClient();
  const { addToWatchlist, removeFromWatchlist } = useMarketStore();

  const addToWatchlistMutation = useMutation({
    mutationFn: async (symbolId: string) => {
      const response = await apiService.post('/api/v1/user/watchlist', { symbolId });
      return response.data;
    },
    onSuccess: (_, symbolId) => {
      addToWatchlist(symbolId);
      queryClient.invalidateQueries({ queryKey: queryKeys.user.watchlist() });
    },
    onError: (error) => handleQueryError(error, 'Add to watchlist'),
  });

  const removeFromWatchlistMutation = useMutation({
    mutationFn: async (symbolId: string) => {
      const response = await apiService.delete(`/api/v1/user/watchlist/${symbolId}`);
      return response.data;
    },
    onSuccess: (_, symbolId) => {
      removeFromWatchlist(symbolId);
      queryClient.invalidateQueries({ queryKey: queryKeys.user.watchlist() });
    },
    onError: (error) => handleQueryError(error, 'Remove from watchlist'),
  });

  return {
    addToWatchlist: addToWatchlistMutation,
    removeFromWatchlist: removeFromWatchlistMutation,
  };
};

// Export fetch functions for prefetching
export const fetchMarketOverview = async () => {
  const response = await apiService.get<Record<string, MarketData>>(endpoints.market.overview);
  return response.data;
};

export const fetchTopMovers = async (params?: { assetClass?: string; limit?: number }) => {
  const response = await apiService.get<TopMover[]>(endpoints.market.topMovers, params);
  return response.data;
};