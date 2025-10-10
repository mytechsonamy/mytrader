/**
 * Market data store using Zustand
 */

import { create } from 'zustand';
import { devtools, subscribeWithSelector } from 'zustand/middleware';
import type { MarketData, Symbol, AssetClass, TopMover } from '../types';

interface MarketStore {
  // State
  marketData: Record<string, MarketData>;
  symbols: Symbol[];
  assetClasses: AssetClass[];
  topMovers: TopMover[];
  marketStatus: Record<string, string>;
  isLoading: boolean;
  error: string | null;
  lastUpdate: string | null;
  selectedAssetClass: string | null;
  watchlist: string[];

  // Actions
  setMarketData: (data: Record<string, MarketData>) => void;
  updateMarketData: (symbolId: string, data: MarketData) => void;
  batchUpdateMarketData: (updates: MarketData[]) => void;
  setSymbols: (symbols: Symbol[]) => void;
  setAssetClasses: (assetClasses: AssetClass[]) => void;
  setTopMovers: (topMovers: TopMover[]) => void;
  setMarketStatus: (market: string, status: string) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  setSelectedAssetClass: (assetClass: string | null) => void;
  addToWatchlist: (symbolId: string) => void;
  removeFromWatchlist: (symbolId: string) => void;
  toggleWatchlist: (symbolId: string) => void;
  clearMarketData: () => void;

  // Selectors
  getMarketDataBySymbol: (symbolId: string) => MarketData | null;
  getSymbolsByAssetClass: (assetClassId: string) => Symbol[];
  getMarketDataByAssetClass: (assetClassId: string) => MarketData[];
  getWatchlistData: () => MarketData[];
  isInWatchlist: (symbolId: string) => boolean;
  getMarketDataArray: () => MarketData[];
  getFilteredSymbols: (searchTerm: string, assetClass?: string) => Symbol[];
}

export const useMarketStore = create<MarketStore>()(
  devtools(
    subscribeWithSelector((set, get) => ({
      // Initial state
      marketData: {},
      symbols: [],
      assetClasses: [],
      topMovers: [],
      marketStatus: {},
      isLoading: false,
      error: null,
      lastUpdate: null,
      selectedAssetClass: null,
      watchlist: [],

      // Actions
      setMarketData: (data: Record<string, MarketData>) => {
        set({
          marketData: data,
          lastUpdate: new Date().toISOString(),
        });
      },

      updateMarketData: (symbolId: string, data: MarketData) => {
        set((state) => ({
          marketData: {
            ...state.marketData,
            [symbolId]: {
              ...state.marketData[symbolId],
              ...data,
              timestamp: new Date().toISOString(),
            },
          },
          lastUpdate: new Date().toISOString(),
        }));
      },

      batchUpdateMarketData: (updates: MarketData[]) => {
        set((state) => {
          const newMarketData = { ...state.marketData };
          updates.forEach((update) => {
            newMarketData[update.symbolId] = {
              ...newMarketData[update.symbolId],
              ...update,
              timestamp: new Date().toISOString(),
            };
          });

          return {
            marketData: newMarketData,
            lastUpdate: new Date().toISOString(),
          };
        });
      },

      setSymbols: (symbols: Symbol[]) => {
        set({ symbols });
      },

      setAssetClasses: (assetClasses: AssetClass[]) => {
        set({ assetClasses });
      },

      setTopMovers: (topMovers: TopMover[]) => {
        set({ topMovers });
      },

      setMarketStatus: (market: string, status: string) => {
        set((state) => ({
          marketStatus: {
            ...state.marketStatus,
            [market]: status,
          },
        }));
      },

      setLoading: (loading: boolean) => {
        set({ isLoading: loading });
      },

      setError: (error: string | null) => {
        set({ error });
      },

      setSelectedAssetClass: (assetClass: string | null) => {
        set({ selectedAssetClass: assetClass });
      },

      addToWatchlist: (symbolId: string) => {
        set((state) => ({
          watchlist: state.watchlist.includes(symbolId)
            ? state.watchlist
            : [...state.watchlist, symbolId],
        }));
      },

      removeFromWatchlist: (symbolId: string) => {
        set((state) => ({
          watchlist: state.watchlist.filter((id) => id !== symbolId),
        }));
      },

      toggleWatchlist: (symbolId: string) => {
        const { watchlist } = get();
        if (watchlist.includes(symbolId)) {
          get().removeFromWatchlist(symbolId);
        } else {
          get().addToWatchlist(symbolId);
        }
      },

      clearMarketData: () => {
        set({
          marketData: {},
          topMovers: [],
          error: null,
          lastUpdate: null,
        });
      },

      // Selectors
      getMarketDataBySymbol: (symbolId: string) => {
        return get().marketData[symbolId] || null;
      },

      getSymbolsByAssetClass: (assetClassId: string) => {
        return get().symbols.filter((symbol) => symbol.assetClass.id === assetClassId);
      },

      getMarketDataByAssetClass: (assetClassId: string) => {
        const symbols = get().getSymbolsByAssetClass(assetClassId);
        const marketData = get().marketData;

        return symbols
          .map((symbol) => marketData[symbol.id])
          .filter((data) => data !== undefined);
      },

      getWatchlistData: () => {
        const { watchlist, marketData } = get();
        return watchlist
          .map((symbolId) => marketData[symbolId])
          .filter((data) => data !== undefined);
      },

      isInWatchlist: (symbolId: string) => {
        return get().watchlist.includes(symbolId);
      },

      getMarketDataArray: () => {
        return Object.values(get().marketData);
      },

      getFilteredSymbols: (searchTerm: string, assetClass?: string) => {
        const { symbols } = get();
        let filteredSymbols = symbols;

        // Filter by asset class if provided
        if (assetClass) {
          filteredSymbols = symbols.filter(
            (symbol) => symbol.assetClass.id === assetClass
          );
        }

        // Filter by search term
        if (searchTerm) {
          const lowerSearchTerm = searchTerm.toLowerCase();
          filteredSymbols = filteredSymbols.filter(
            (symbol) =>
              symbol.symbol.toLowerCase().includes(lowerSearchTerm) ||
              symbol.name.toLowerCase().includes(lowerSearchTerm)
          );
        }

        return filteredSymbols;
      },
    })),
    { name: 'MarketStore' }
  )
);

// Selectors for easier access
export const useMarketData = () => useMarketStore((state) => state.marketData);
export const useSymbols = () => useMarketStore((state) => state.symbols);
export const useAssetClasses = () => useMarketStore((state) => state.assetClasses);
export const useTopMovers = () => useMarketStore((state) => state.topMovers);
export const useMarketStatus = () => useMarketStore((state) => state.marketStatus);
export const useMarketLoading = () => useMarketStore((state) => state.isLoading);
export const useMarketError = () => useMarketStore((state) => state.error);
export const useSelectedAssetClass = () => useMarketStore((state) => state.selectedAssetClass);
export const useWatchlist = () => useMarketStore((state) => state.watchlist);
export const useLastUpdate = () => useMarketStore((state) => state.lastUpdate);

// Market actions
export const useMarketActions = () => useMarketStore((state) => ({
  setMarketData: state.setMarketData,
  updateMarketData: state.updateMarketData,
  batchUpdateMarketData: state.batchUpdateMarketData,
  setSymbols: state.setSymbols,
  setAssetClasses: state.setAssetClasses,
  setTopMovers: state.setTopMovers,
  setMarketStatus: state.setMarketStatus,
  setLoading: state.setLoading,
  setError: state.setError,
  setSelectedAssetClass: state.setSelectedAssetClass,
  addToWatchlist: state.addToWatchlist,
  removeFromWatchlist: state.removeFromWatchlist,
  toggleWatchlist: state.toggleWatchlist,
  clearMarketData: state.clearMarketData,
}));

// Market selectors
export const useMarketSelectors = () => useMarketStore((state) => ({
  getMarketDataBySymbol: state.getMarketDataBySymbol,
  getSymbolsByAssetClass: state.getSymbolsByAssetClass,
  getMarketDataByAssetClass: state.getMarketDataByAssetClass,
  getWatchlistData: state.getWatchlistData,
  isInWatchlist: state.isInWatchlist,
  getMarketDataArray: state.getMarketDataArray,
  getFilteredSymbols: state.getFilteredSymbols,
}));

// Subscribe to market data changes for real-time updates
export const subscribeToMarketData = (
  symbolId: string,
  callback: (data: MarketData) => void
) => {
  return useMarketStore.subscribe(
    (state) => state.marketData[symbolId],
    (marketData) => {
      if (marketData) {
        callback(marketData);
      }
    }
  );
};

// Subscribe to watchlist changes
export const subscribeToWatchlist = (
  callback: (watchlist: string[]) => void
) => {
  return useMarketStore.subscribe(
    (state) => state.watchlist,
    callback
  );
};