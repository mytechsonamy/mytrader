import React, { createContext, useContext, useReducer, useEffect, useCallback, ReactNode } from 'react';
import { multiAssetApiService } from '../services/multiAssetApi';
import { websocketService } from '../services/websocketService';
import {
  AssetClassDto, MarketDto, EnhancedSymbolDto, UnifiedMarketDataDto,
  PagedResponse, AssetClassType, LoadingState
} from '../types';

// State interfaces
interface AssetClassState {
  data: AssetClassDto[];
  loading: LoadingState;
  error: string | null;
  lastUpdated: string | null;
}

interface MarketsState {
  data: MarketDto[];
  loading: LoadingState;
  error: string | null;
  lastUpdated: string | null;
  byAssetClass: Record<string, MarketDto[]>;
}

interface SymbolsState {
  data: EnhancedSymbolDto[];
  loading: LoadingState;
  error: string | null;
  lastUpdated: string | null;
  byAssetClass: Record<string, EnhancedSymbolDto[]>;
  byMarket: Record<string, EnhancedSymbolDto[]>;
  searchResults: EnhancedSymbolDto[];
  searchLoading: boolean;
  totalPages: number;
  currentPage: number;
}

interface MarketDataState {
  data: Record<string, UnifiedMarketDataDto>;
  loading: LoadingState;
  error: string | null;
  lastUpdated: string | null;
  subscriptions: Set<string>;
}

interface MultiAssetState {
  assetClasses: AssetClassState;
  markets: MarketsState;
  symbols: SymbolsState;
  marketData: MarketDataState;
  isInitialized: boolean;
  connectionStatus: 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error';
}

// Action types
type MultiAssetAction =
  // Asset Classes
  | { type: 'LOAD_ASSET_CLASSES_START' }
  | { type: 'LOAD_ASSET_CLASSES_SUCCESS'; payload: AssetClassDto[] }
  | { type: 'LOAD_ASSET_CLASSES_ERROR'; payload: string }

  // Markets
  | { type: 'LOAD_MARKETS_START' }
  | { type: 'LOAD_MARKETS_SUCCESS'; payload: MarketDto[] }
  | { type: 'LOAD_MARKETS_ERROR'; payload: string }
  | { type: 'SET_MARKETS_BY_ASSET_CLASS'; payload: { assetClassId: string; markets: MarketDto[] } }

  // Symbols
  | { type: 'LOAD_SYMBOLS_START' }
  | { type: 'LOAD_SYMBOLS_SUCCESS'; payload: { symbols: EnhancedSymbolDto[]; totalPages: number; currentPage: number } }
  | { type: 'LOAD_SYMBOLS_ERROR'; payload: string }
  | { type: 'SET_SYMBOLS_BY_ASSET_CLASS'; payload: { assetClassId: string; symbols: EnhancedSymbolDto[] } }
  | { type: 'SET_SYMBOLS_BY_MARKET'; payload: { marketId: string; symbols: EnhancedSymbolDto[] } }
  | { type: 'SEARCH_SYMBOLS_START' }
  | { type: 'SEARCH_SYMBOLS_SUCCESS'; payload: EnhancedSymbolDto[] }
  | { type: 'SEARCH_SYMBOLS_ERROR' }
  | { type: 'CLEAR_SEARCH_RESULTS' }

  // Market Data
  | { type: 'LOAD_MARKET_DATA_START' }
  | { type: 'LOAD_MARKET_DATA_SUCCESS'; payload: UnifiedMarketDataDto[] }
  | { type: 'LOAD_MARKET_DATA_ERROR'; payload: string }
  | { type: 'UPDATE_MARKET_DATA'; payload: UnifiedMarketDataDto }
  | { type: 'BATCH_UPDATE_MARKET_DATA'; payload: UnifiedMarketDataDto[] }
  | { type: 'ADD_SUBSCRIPTION'; payload: string }
  | { type: 'REMOVE_SUBSCRIPTION'; payload: string }

  // General
  | { type: 'SET_INITIALIZED'; payload: boolean }
  | { type: 'SET_CONNECTION_STATUS'; payload: 'disconnected' | 'connecting' | 'connected' | 'error' }
  | { type: 'CLEAR_ALL_ERRORS' }
  | { type: 'REFRESH_ALL_DATA' };

// Initial state
const initialState: MultiAssetState = {
  assetClasses: {
    data: [],
    loading: 'idle',
    error: null,
    lastUpdated: null,
  },
  markets: {
    data: [],
    loading: 'idle',
    error: null,
    lastUpdated: null,
    byAssetClass: {},
  },
  symbols: {
    data: [],
    loading: 'idle',
    error: null,
    lastUpdated: null,
    byAssetClass: {},
    byMarket: {},
    searchResults: [],
    searchLoading: false,
    totalPages: 0,
    currentPage: 1,
  },
  marketData: {
    data: {},
    loading: 'idle',
    error: null,
    lastUpdated: null,
    subscriptions: new Set(),
  },
  isInitialized: false,
  connectionStatus: 'disconnected',
};

// Reducer
function multiAssetReducer(state: MultiAssetState, action: MultiAssetAction): MultiAssetState {
  switch (action.type) {
    // Asset Classes
    case 'LOAD_ASSET_CLASSES_START':
      return {
        ...state,
        assetClasses: {
          ...state.assetClasses,
          loading: 'loading',
          error: null,
        },
      };

    case 'LOAD_ASSET_CLASSES_SUCCESS':
      return {
        ...state,
        assetClasses: {
          ...state.assetClasses,
          data: action.payload,
          loading: 'success',
          error: null,
          lastUpdated: new Date().toISOString(),
        },
      };

    case 'LOAD_ASSET_CLASSES_ERROR':
      return {
        ...state,
        assetClasses: {
          ...state.assetClasses,
          loading: 'error',
          error: action.payload,
        },
      };

    // Markets
    case 'LOAD_MARKETS_START':
      return {
        ...state,
        markets: {
          ...state.markets,
          loading: 'loading',
          error: null,
        },
      };

    case 'LOAD_MARKETS_SUCCESS':
      const marketsByAssetClass: Record<string, MarketDto[]> = {};
      action.payload.forEach(market => {
        if (!marketsByAssetClass[market.assetClassId]) {
          marketsByAssetClass[market.assetClassId] = [];
        }
        marketsByAssetClass[market.assetClassId].push(market);
      });

      return {
        ...state,
        markets: {
          ...state.markets,
          data: action.payload,
          loading: 'success',
          error: null,
          lastUpdated: new Date().toISOString(),
          byAssetClass: marketsByAssetClass,
        },
      };

    case 'LOAD_MARKETS_ERROR':
      return {
        ...state,
        markets: {
          ...state.markets,
          loading: 'error',
          error: action.payload,
        },
      };

    case 'SET_MARKETS_BY_ASSET_CLASS':
      return {
        ...state,
        markets: {
          ...state.markets,
          byAssetClass: {
            ...state.markets.byAssetClass,
            [action.payload.assetClassId]: action.payload.markets,
          },
        },
      };

    // Symbols
    case 'LOAD_SYMBOLS_START':
      return {
        ...state,
        symbols: {
          ...state.symbols,
          loading: 'loading',
          error: null,
        },
      };

    case 'LOAD_SYMBOLS_SUCCESS':
      const symbolsByAssetClass: Record<string, EnhancedSymbolDto[]> = {};
      const symbolsByMarket: Record<string, EnhancedSymbolDto[]> = {};

      action.payload.symbols.forEach(symbol => {
        // Group by asset class
        if (!symbolsByAssetClass[symbol.assetClassId]) {
          symbolsByAssetClass[symbol.assetClassId] = [];
        }
        symbolsByAssetClass[symbol.assetClassId].push(symbol);

        // Group by market
        if (!symbolsByMarket[symbol.marketId]) {
          symbolsByMarket[symbol.marketId] = [];
        }
        symbolsByMarket[symbol.marketId].push(symbol);
      });

      return {
        ...state,
        symbols: {
          ...state.symbols,
          data: action.payload.symbols,
          loading: 'success',
          error: null,
          lastUpdated: new Date().toISOString(),
          byAssetClass: symbolsByAssetClass,
          byMarket: symbolsByMarket,
          totalPages: action.payload.totalPages,
          currentPage: action.payload.currentPage,
        },
      };

    case 'LOAD_SYMBOLS_ERROR':
      return {
        ...state,
        symbols: {
          ...state.symbols,
          loading: 'error',
          error: action.payload,
        },
      };

    case 'SET_SYMBOLS_BY_ASSET_CLASS':
      return {
        ...state,
        symbols: {
          ...state.symbols,
          byAssetClass: {
            ...state.symbols.byAssetClass,
            [action.payload.assetClassId]: action.payload.symbols,
          },
        },
      };

    case 'SET_SYMBOLS_BY_MARKET':
      return {
        ...state,
        symbols: {
          ...state.symbols,
          byMarket: {
            ...state.symbols.byMarket,
            [action.payload.marketId]: action.payload.symbols,
          },
        },
      };

    case 'SEARCH_SYMBOLS_START':
      return {
        ...state,
        symbols: {
          ...state.symbols,
          searchLoading: true,
        },
      };

    case 'SEARCH_SYMBOLS_SUCCESS':
      return {
        ...state,
        symbols: {
          ...state.symbols,
          searchResults: action.payload,
          searchLoading: false,
        },
      };

    case 'SEARCH_SYMBOLS_ERROR':
      return {
        ...state,
        symbols: {
          ...state.symbols,
          searchLoading: false,
        },
      };

    case 'CLEAR_SEARCH_RESULTS':
      return {
        ...state,
        symbols: {
          ...state.symbols,
          searchResults: [],
          searchLoading: false,
        },
      };

    // Market Data
    case 'LOAD_MARKET_DATA_START':
      return {
        ...state,
        marketData: {
          ...state.marketData,
          loading: 'loading',
          error: null,
        },
      };

    case 'LOAD_MARKET_DATA_SUCCESS':
      const marketDataMap: Record<string, UnifiedMarketDataDto> = {};
      action.payload.forEach(data => {
        marketDataMap[data.symbolId] = data;
      });

      return {
        ...state,
        marketData: {
          ...state.marketData,
          data: { ...state.marketData.data, ...marketDataMap },
          loading: 'success',
          error: null,
          lastUpdated: new Date().toISOString(),
        },
      };

    case 'LOAD_MARKET_DATA_ERROR':
      return {
        ...state,
        marketData: {
          ...state.marketData,
          loading: 'error',
          error: action.payload,
        },
      };

    case 'UPDATE_MARKET_DATA':
      return {
        ...state,
        marketData: {
          ...state.marketData,
          data: {
            ...state.marketData.data,
            [action.payload.symbolId]: action.payload,
          },
          lastUpdated: new Date().toISOString(),
        },
      };

    case 'BATCH_UPDATE_MARKET_DATA':
      const batchUpdates: Record<string, UnifiedMarketDataDto> = {};
      action.payload.forEach(data => {
        batchUpdates[data.symbolId] = data;
      });

      return {
        ...state,
        marketData: {
          ...state.marketData,
          data: { ...state.marketData.data, ...batchUpdates },
          lastUpdated: new Date().toISOString(),
        },
      };

    case 'ADD_SUBSCRIPTION':
      const newSubscriptions = new Set(state.marketData.subscriptions);
      newSubscriptions.add(action.payload);
      return {
        ...state,
        marketData: {
          ...state.marketData,
          subscriptions: newSubscriptions,
        },
      };

    case 'REMOVE_SUBSCRIPTION':
      const updatedSubscriptions = new Set(state.marketData.subscriptions);
      updatedSubscriptions.delete(action.payload);
      return {
        ...state,
        marketData: {
          ...state.marketData,
          subscriptions: updatedSubscriptions,
        },
      };

    // General
    case 'SET_INITIALIZED':
      return {
        ...state,
        isInitialized: action.payload,
      };

    case 'SET_CONNECTION_STATUS':
      return {
        ...state,
        connectionStatus: action.payload,
      };

    case 'CLEAR_ALL_ERRORS':
      return {
        ...state,
        assetClasses: { ...state.assetClasses, error: null },
        markets: { ...state.markets, error: null },
        symbols: { ...state.symbols, error: null },
        marketData: { ...state.marketData, error: null },
      };

    case 'REFRESH_ALL_DATA':
      return {
        ...state,
        assetClasses: { ...state.assetClasses, loading: 'loading', error: null },
        markets: { ...state.markets, loading: 'loading', error: null },
        symbols: { ...state.symbols, loading: 'loading', error: null },
        marketData: { ...state.marketData, loading: 'loading', error: null },
      };

    default:
      return state;
  }
}

// Context interface
interface MultiAssetContextType {
  state: MultiAssetState;

  // Asset Classes
  loadAssetClasses: () => Promise<void>;

  // Markets
  loadMarkets: (filters?: { assetClassId?: string; isActive?: boolean }) => Promise<void>;
  getMarketsByAssetClass: (assetClassId: string) => MarketDto[];

  // Symbols
  loadSymbols: (page?: number, pageSize?: number, filters?: { assetClassId?: string; marketId?: string }) => Promise<void>;
  searchSymbols: (query: string, filters?: { assetClass?: AssetClassType }) => Promise<void>;
  clearSearchResults: () => void;
  getSymbolsByAssetClass: (assetClassId: string) => EnhancedSymbolDto[];
  getSymbolsByMarket: (marketId: string) => EnhancedSymbolDto[];

  // Market Data
  loadMarketData: (symbolIds: string[]) => Promise<void>;
  subscribeToSymbols: (symbolIds: string[], assetClass?: AssetClassType) => Promise<void>;
  unsubscribeFromSymbols: (symbolIds: string[]) => Promise<void>;
  getMarketData: (symbolId: string) => UnifiedMarketDataDto | null;

  // Utilities
  refreshAllData: () => Promise<void>;
  clearAllErrors: () => void;
  initialize: () => Promise<void>;
  isReady: () => boolean;
}

// Create context
const MultiAssetContext = createContext<MultiAssetContextType | null>(null);

// Provider component
interface MultiAssetProviderProps {
  children: ReactNode;
  autoInitialize?: boolean;
}

export const MultiAssetProvider: React.FC<MultiAssetProviderProps> = ({
  children,
  autoInitialize = true
}) => {
  const [state, dispatch] = useReducer(multiAssetReducer, initialState);

  // Initialize services
  const initialize = useCallback(async () => {
    try {
      dispatch({ type: 'SET_CONNECTION_STATUS', payload: 'connecting' });

      // Initialize API service
      await multiAssetApiService.initialize();

      // Initialize WebSocket service
      await websocketService.initialize();

      // Set up WebSocket event listeners
      websocketService.onConnectionStatus((status) => {
        dispatch({ type: 'SET_CONNECTION_STATUS', payload: status });
      });

      websocketService.on('price_update', (data: UnifiedMarketDataDto) => {
        dispatch({ type: 'UPDATE_MARKET_DATA', payload: data });
      });

      websocketService.on('batch_price_update', (data: UnifiedMarketDataDto[]) => {
        dispatch({ type: 'BATCH_UPDATE_MARKET_DATA', payload: data });
      });

      // Load initial data
      await loadAssetClasses();
      await loadMarkets();

      // Prefetch essential data
      await multiAssetApiService.prefetchEssentialData();

      dispatch({ type: 'SET_INITIALIZED', payload: true });
      console.log('MultiAssetContext initialized successfully');

    } catch (error) {
      console.error('Failed to initialize MultiAssetContext:', error);
      dispatch({ type: 'SET_CONNECTION_STATUS', payload: 'error' });
    }
  }, []);

  // Asset Classes
  const loadAssetClasses = useCallback(async () => {
    try {
      dispatch({ type: 'LOAD_ASSET_CLASSES_START' });
      const assetClasses = await multiAssetApiService.getActiveAssetClasses();
      dispatch({ type: 'LOAD_ASSET_CLASSES_SUCCESS', payload: assetClasses });
    } catch (error) {
      dispatch({ type: 'LOAD_ASSET_CLASSES_ERROR', payload: (error as Error).message });
    }
  }, []);

  // Markets
  const loadMarkets = useCallback(async (filters?: { assetClassId?: string; isActive?: boolean }) => {
    try {
      dispatch({ type: 'LOAD_MARKETS_START' });
      const markets = await multiAssetApiService.getMarkets(filters);
      dispatch({ type: 'LOAD_MARKETS_SUCCESS', payload: markets });
    } catch (error) {
      dispatch({ type: 'LOAD_MARKETS_ERROR', payload: (error as Error).message });
    }
  }, []);

  const getMarketsByAssetClass = useCallback((assetClassId: string): MarketDto[] => {
    return state.markets.byAssetClass[assetClassId] || [];
  }, [state.markets.byAssetClass]);

  // Symbols
  const loadSymbols = useCallback(async (
    page = 1,
    pageSize = 50,
    filters?: { assetClassId?: string; marketId?: string }
  ) => {
    try {
      dispatch({ type: 'LOAD_SYMBOLS_START' });
      const response = await multiAssetApiService.getEnhancedSymbols(
        page,
        pageSize,
        filters?.assetClassId,
        filters?.marketId
      );
      dispatch({
        type: 'LOAD_SYMBOLS_SUCCESS',
        payload: {
          symbols: response.data,
          totalPages: response.totalPages,
          currentPage: response.pageNumber
        }
      });
    } catch (error) {
      dispatch({ type: 'LOAD_SYMBOLS_ERROR', payload: (error as Error).message });
    }
  }, []);

  const searchSymbols = useCallback(async (
    query: string,
    filters?: { assetClass?: AssetClassType }
  ) => {
    try {
      dispatch({ type: 'SEARCH_SYMBOLS_START' });
      const results = await multiAssetApiService.searchSymbols(query, {
        assetClass: filters?.assetClass,
      });
      dispatch({ type: 'SEARCH_SYMBOLS_SUCCESS', payload: results.data });
    } catch (error) {
      dispatch({ type: 'SEARCH_SYMBOLS_ERROR' });
    }
  }, []);

  const clearSearchResults = useCallback(() => {
    dispatch({ type: 'CLEAR_SEARCH_RESULTS' });
  }, []);

  const getSymbolsByAssetClass = useCallback((assetClassId: string): EnhancedSymbolDto[] => {
    return state.symbols.byAssetClass[assetClassId] || [];
  }, [state.symbols.byAssetClass]);

  const getSymbolsByMarket = useCallback((marketId: string): EnhancedSymbolDto[] => {
    return state.symbols.byMarket[marketId] || [];
  }, [state.symbols.byMarket]);

  // Market Data
  const loadMarketData = useCallback(async (symbolIds: string[]) => {
    try {
      dispatch({ type: 'LOAD_MARKET_DATA_START' });
      const marketData = await multiAssetApiService.getBatchMarketData(symbolIds);
      dispatch({ type: 'LOAD_MARKET_DATA_SUCCESS', payload: marketData });
    } catch (error) {
      dispatch({ type: 'LOAD_MARKET_DATA_ERROR', payload: (error as Error).message });
    }
  }, []);

  const subscribeToSymbols = useCallback(async (symbolIds: string[], assetClass?: AssetClassType) => {
    try {
      const subscriptionId = await websocketService.subscribeToPrices(symbolIds, assetClass);
      dispatch({ type: 'ADD_SUBSCRIPTION', payload: subscriptionId });
    } catch (error) {
      console.error('Failed to subscribe to symbols:', error);
    }
  }, []);

  const unsubscribeFromSymbols = useCallback(async (symbolIds: string[]) => {
    // Find and remove matching subscriptions
    for (const subscriptionId of state.marketData.subscriptions) {
      try {
        await websocketService.unsubscribe(subscriptionId);
        dispatch({ type: 'REMOVE_SUBSCRIPTION', payload: subscriptionId });
      } catch (error) {
        console.error('Failed to unsubscribe from symbols:', error);
      }
    }
  }, [state.marketData.subscriptions]);

  const getMarketData = useCallback((symbolId: string): UnifiedMarketDataDto | null => {
    return state.marketData.data[symbolId] || null;
  }, [state.marketData.data]);

  // Utilities
  const refreshAllData = useCallback(async () => {
    dispatch({ type: 'REFRESH_ALL_DATA' });
    await Promise.all([
      loadAssetClasses(),
      loadMarkets(),
      loadSymbols(),
    ]);
  }, [loadAssetClasses, loadMarkets, loadSymbols]);

  const clearAllErrors = useCallback(() => {
    dispatch({ type: 'CLEAR_ALL_ERRORS' });
  }, []);

  const isReady = useCallback(() => {
    return state.isInitialized &&
           state.connectionStatus === 'connected' &&
           state.assetClasses.loading === 'success';
  }, [state.isInitialized, state.connectionStatus, state.assetClasses.loading]);

  // Auto-initialize on mount
  useEffect(() => {
    if (autoInitialize && !state.isInitialized) {
      initialize();
    }
  }, [autoInitialize, state.isInitialized, initialize]);

  const contextValue: MultiAssetContextType = {
    state,
    loadAssetClasses,
    loadMarkets,
    getMarketsByAssetClass,
    loadSymbols,
    searchSymbols,
    clearSearchResults,
    getSymbolsByAssetClass,
    getSymbolsByMarket,
    loadMarketData,
    subscribeToSymbols,
    unsubscribeFromSymbols,
    getMarketData,
    refreshAllData,
    clearAllErrors,
    initialize,
    isReady,
  };

  return (
    <MultiAssetContext.Provider value={contextValue}>
      {children}
    </MultiAssetContext.Provider>
  );
};

// Hook to use the context
export const useMultiAsset = (): MultiAssetContextType => {
  const context = useContext(MultiAssetContext);
  if (!context) {
    throw new Error('useMultiAsset must be used within a MultiAssetProvider');
  }
  return context;
};

// Selector hooks for specific data
export const useAssetClasses = () => {
  const { state } = useMultiAsset();
  return state.assetClasses;
};

export const useMarkets = () => {
  const { state } = useMultiAsset();
  return state.markets;
};

export const useSymbols = () => {
  const { state } = useMultiAsset();
  return state.symbols;
};

export const useMarketData = () => {
  const { state } = useMultiAsset();
  return state.marketData;
};

export const useConnectionStatus = () => {
  const { state } = useMultiAsset();
  return state.connectionStatus;
};