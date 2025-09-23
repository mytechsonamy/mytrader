import React, { createContext, useContext, useReducer, useEffect, useCallback, ReactNode } from 'react';
import { multiAssetApiService } from '../services/multiAssetApi';
import { websocketService } from '../services/websocketService';
import { MarketStatusDto, MarketDto, LoadingState } from '../types';

// Market hours and status calculation utilities
interface MarketHours {
  open: string;
  close: string;
  timeZone: string;
  tradingDays: string[];
  holidays: string[];
}

interface MarketStatusWithTiming extends MarketStatusDto {
  minutesUntilOpen?: number;
  minutesUntilClose?: number;
  isExtendedHours?: boolean;
  nextTradingSession?: string;
  sessionProgress?: number; // 0-100 percentage through trading day
}

interface MarketStatusState {
  statuses: Record<string, MarketStatusWithTiming>;
  loading: LoadingState;
  error: string | null;
  lastUpdated: string | null;
  subscriptionId: string | null;
  refreshInterval: NodeJS.Timeout | null;
}

type MarketStatusAction =
  | { type: 'LOAD_START' }
  | { type: 'LOAD_SUCCESS'; payload: MarketStatusDto[] }
  | { type: 'LOAD_ERROR'; payload: string }
  | { type: 'UPDATE_STATUS'; payload: MarketStatusDto }
  | { type: 'SET_SUBSCRIPTION'; payload: string }
  | { type: 'CLEAR_SUBSCRIPTION' }
  | { type: 'UPDATE_TIMING'; payload: Record<string, MarketStatusWithTiming> }
  | { type: 'SET_REFRESH_INTERVAL'; payload: NodeJS.Timeout }
  | { type: 'CLEAR_REFRESH_INTERVAL' };

const initialState: MarketStatusState = {
  statuses: {},
  loading: 'idle',
  error: null,
  lastUpdated: null,
  subscriptionId: null,
  refreshInterval: null,
};

function marketStatusReducer(state: MarketStatusState, action: MarketStatusAction): MarketStatusState {
  switch (action.type) {
    case 'LOAD_START':
      return {
        ...state,
        loading: 'loading',
        error: null,
      };

    case 'LOAD_SUCCESS':
      const statusMap: Record<string, MarketStatusWithTiming> = {};
      action.payload.forEach(status => {
        statusMap[status.marketId] = enhanceStatusWithTiming(status);
      });

      return {
        ...state,
        statuses: statusMap,
        loading: 'success',
        error: null,
        lastUpdated: new Date().toISOString(),
      };

    case 'LOAD_ERROR':
      return {
        ...state,
        loading: 'error',
        error: action.payload,
      };

    case 'UPDATE_STATUS':
      return {
        ...state,
        statuses: {
          ...state.statuses,
          [action.payload.marketId]: enhanceStatusWithTiming(action.payload),
        },
        lastUpdated: new Date().toISOString(),
      };

    case 'SET_SUBSCRIPTION':
      return {
        ...state,
        subscriptionId: action.payload,
      };

    case 'CLEAR_SUBSCRIPTION':
      return {
        ...state,
        subscriptionId: null,
      };

    case 'UPDATE_TIMING':
      return {
        ...state,
        statuses: action.payload,
        lastUpdated: new Date().toISOString(),
      };

    case 'SET_REFRESH_INTERVAL':
      return {
        ...state,
        refreshInterval: action.payload,
      };

    case 'CLEAR_REFRESH_INTERVAL':
      if (state.refreshInterval) {
        clearInterval(state.refreshInterval);
      }
      return {
        ...state,
        refreshInterval: null,
      };

    default:
      return state;
  }
}

// Enhanced timing calculation
function enhanceStatusWithTiming(status: MarketStatusDto): MarketStatusWithTiming {
  const now = new Date();
  const marketTime = new Date(status.currentTime);

  let enhanced: MarketStatusWithTiming = { ...status };

  try {
    // Calculate minutes until next state change
    if (status.nextOpen) {
      const nextOpen = new Date(status.nextOpen);
      enhanced.minutesUntilOpen = Math.max(0, Math.floor((nextOpen.getTime() - now.getTime()) / (1000 * 60)));
    }

    if (status.nextClose) {
      const nextClose = new Date(status.nextClose);
      enhanced.minutesUntilClose = Math.max(0, Math.floor((nextClose.getTime() - now.getTime()) / (1000 * 60)));
    }

    // Determine if we're in extended hours
    enhanced.isExtendedHours = status.status === 'PRE_MARKET' || status.status === 'AFTER_MARKET';

    // Calculate session progress for open markets
    if (status.status === 'OPEN' && status.nextClose) {
      const sessionStart = new Date(marketTime);
      sessionStart.setHours(9, 30, 0, 0); // Assume 9:30 AM start (will be configurable)
      const sessionEnd = new Date(status.nextClose);

      const totalMinutes = (sessionEnd.getTime() - sessionStart.getTime()) / (1000 * 60);
      const elapsedMinutes = (now.getTime() - sessionStart.getTime()) / (1000 * 60);

      enhanced.sessionProgress = Math.min(100, Math.max(0, (elapsedMinutes / totalMinutes) * 100));
    }

    // Set next trading session
    if (status.status === 'CLOSED') {
      enhanced.nextTradingSession = status.nextOpen || 'Unknown';
    } else if (status.status === 'OPEN') {
      enhanced.nextTradingSession = status.nextClose || 'Unknown';
    }

  } catch (error) {
    console.warn('Error enhancing market status with timing:', error);
  }

  return enhanced;
}

// Context interface
interface MarketStatusContextType {
  state: MarketStatusState;

  // Core methods
  loadMarketStatuses: (marketIds?: string[]) => Promise<void>;
  subscribeToMarketStatuses: (marketIds?: string[]) => Promise<void>;
  unsubscribeFromMarketStatuses: () => Promise<void>;

  // Status queries
  getMarketStatus: (marketId: string) => MarketStatusWithTiming | null;
  isMarketOpen: (marketId: string) => boolean;
  isMarketClosed: (marketId: string) => boolean;
  isExtendedHours: (marketId: string) => boolean;
  getOpenMarkets: () => MarketStatusWithTiming[];
  getClosedMarkets: () => MarketStatusWithTiming[];

  // Timing utilities
  getMinutesUntilOpen: (marketId: string) => number | null;
  getMinutesUntilClose: (marketId: string) => number | null;
  getSessionProgress: (marketId: string) => number | null;
  getNextTradingSession: (marketId: string) => string | null;

  // Market groups
  getStatusByAssetClass: (assetClassId: string, markets: MarketDto[]) => MarketStatusWithTiming[];
  getTradingStatusSummary: () => {
    totalMarkets: number;
    openMarkets: number;
    closedMarkets: number;
    preMarketMarkets: number;
    afterMarketMarkets: number;
  };

  // Utilities
  refresh: () => Promise<void>;
  clearError: () => void;
}

const MarketStatusContext = createContext<MarketStatusContextType | null>(null);

// Provider component
interface MarketStatusProviderProps {
  children: ReactNode;
  refreshIntervalMs?: number;
  autoSubscribe?: boolean;
  marketIds?: string[];
}

export const MarketStatusProvider: React.FC<MarketStatusProviderProps> = ({
  children,
  refreshIntervalMs = 30000, // 30 seconds
  autoSubscribe = true,
  marketIds
}) => {
  const [state, dispatch] = useReducer(marketStatusReducer, initialState);

  // Load market statuses
  const loadMarketStatuses = useCallback(async (targetMarketIds?: string[]) => {
    try {
      dispatch({ type: 'LOAD_START' });
      const statuses = await multiAssetApiService.getMarketStatuses(targetMarketIds);
      dispatch({ type: 'LOAD_SUCCESS', payload: statuses });
    } catch (error) {
      dispatch({ type: 'LOAD_ERROR', payload: (error as Error).message });
    }
  }, []);

  // Subscribe to real-time updates
  const subscribeToMarketStatuses = useCallback(async (targetMarketIds?: string[]) => {
    try {
      if (state.subscriptionId) {
        await websocketService.unsubscribe(state.subscriptionId);
      }

      const subscriptionId = await websocketService.subscribeToMarketStatus(targetMarketIds);
      dispatch({ type: 'SET_SUBSCRIPTION', payload: subscriptionId });
    } catch (error) {
      console.error('Failed to subscribe to market statuses:', error);
    }
  }, [state.subscriptionId]);

  // Unsubscribe from updates
  const unsubscribeFromMarketStatuses = useCallback(async () => {
    if (state.subscriptionId) {
      try {
        await websocketService.unsubscribe(state.subscriptionId);
        dispatch({ type: 'CLEAR_SUBSCRIPTION' });
      } catch (error) {
        console.error('Failed to unsubscribe from market statuses:', error);
      }
    }
  }, [state.subscriptionId]);

  // Status queries
  const getMarketStatus = useCallback((marketId: string): MarketStatusWithTiming | null => {
    return state.statuses[marketId] || null;
  }, [state.statuses]);

  const isMarketOpen = useCallback((marketId: string): boolean => {
    const status = state.statuses[marketId];
    return status?.status === 'OPEN';
  }, [state.statuses]);

  const isMarketClosed = useCallback((marketId: string): boolean => {
    const status = state.statuses[marketId];
    return status?.status === 'CLOSED';
  }, [state.statuses]);

  const isExtendedHours = useCallback((marketId: string): boolean => {
    const status = state.statuses[marketId];
    return status?.isExtendedHours || false;
  }, [state.statuses]);

  const getOpenMarkets = useCallback((): MarketStatusWithTiming[] => {
    return Object.values(state.statuses).filter(status => status.status === 'OPEN');
  }, [state.statuses]);

  const getClosedMarkets = useCallback((): MarketStatusWithTiming[] => {
    return Object.values(state.statuses).filter(status => status.status === 'CLOSED');
  }, [state.statuses]);

  // Timing utilities
  const getMinutesUntilOpen = useCallback((marketId: string): number | null => {
    const status = state.statuses[marketId];
    return status?.minutesUntilOpen ?? null;
  }, [state.statuses]);

  const getMinutesUntilClose = useCallback((marketId: string): number | null => {
    const status = state.statuses[marketId];
    return status?.minutesUntilClose ?? null;
  }, [state.statuses]);

  const getSessionProgress = useCallback((marketId: string): number | null => {
    const status = state.statuses[marketId];
    return status?.sessionProgress ?? null;
  }, [state.statuses]);

  const getNextTradingSession = useCallback((marketId: string): string | null => {
    const status = state.statuses[marketId];
    return status?.nextTradingSession ?? null;
  }, [state.statuses]);

  // Market groups
  const getStatusByAssetClass = useCallback((
    assetClassId: string,
    markets: MarketDto[]
  ): MarketStatusWithTiming[] => {
    const assetClassMarkets = markets.filter(market => market.assetClassId === assetClassId);
    return assetClassMarkets
      .map(market => state.statuses[market.id])
      .filter(Boolean);
  }, [state.statuses]);

  const getTradingStatusSummary = useCallback(() => {
    const statuses = Object.values(state.statuses);
    return {
      totalMarkets: statuses.length,
      openMarkets: statuses.filter(s => s.status === 'OPEN').length,
      closedMarkets: statuses.filter(s => s.status === 'CLOSED').length,
      preMarketMarkets: statuses.filter(s => s.status === 'PRE_MARKET').length,
      afterMarketMarkets: statuses.filter(s => s.status === 'AFTER_MARKET').length,
    };
  }, [state.statuses]);

  // Utilities
  const refresh = useCallback(async () => {
    await loadMarketStatuses(marketIds);
  }, [loadMarketStatuses, marketIds]);

  const clearError = useCallback(() => {
    dispatch({ type: 'LOAD_ERROR', payload: '' });
  }, []);

  // Update timing calculations periodically
  const updateTimingCalculations = useCallback(() => {
    const updatedStatuses: Record<string, MarketStatusWithTiming> = {};

    Object.entries(state.statuses).forEach(([marketId, status]) => {
      updatedStatuses[marketId] = enhanceStatusWithTiming(status);
    });

    dispatch({ type: 'UPDATE_TIMING', payload: updatedStatuses });
  }, [state.statuses]);

  // Set up WebSocket event listeners
  useEffect(() => {
    const handleMarketStatusUpdate = (status: MarketStatusDto) => {
      dispatch({ type: 'UPDATE_STATUS', payload: status });
    };

    websocketService.on('market_status_update', handleMarketStatusUpdate);

    return () => {
      websocketService.off('market_status_update', handleMarketStatusUpdate);
    };
  }, []);

  // Set up periodic refresh
  useEffect(() => {
    if (refreshIntervalMs > 0) {
      const interval = setInterval(() => {
        updateTimingCalculations();
      }, refreshIntervalMs);

      dispatch({ type: 'SET_REFRESH_INTERVAL', payload: interval });

      return () => {
        clearInterval(interval);
        dispatch({ type: 'CLEAR_REFRESH_INTERVAL' });
      };
    }
  }, [refreshIntervalMs, updateTimingCalculations]);

  // Auto-initialize
  useEffect(() => {
    const initialize = async () => {
      await loadMarketStatuses(marketIds);

      if (autoSubscribe) {
        await subscribeToMarketStatuses(marketIds);
      }
    };

    initialize();

    // Cleanup on unmount
    return () => {
      if (state.subscriptionId) {
        websocketService.unsubscribe(state.subscriptionId);
      }
      if (state.refreshInterval) {
        clearInterval(state.refreshInterval);
      }
    };
  }, [autoSubscribe, marketIds]);

  const contextValue: MarketStatusContextType = {
    state,
    loadMarketStatuses,
    subscribeToMarketStatuses,
    unsubscribeFromMarketStatuses,
    getMarketStatus,
    isMarketOpen,
    isMarketClosed,
    isExtendedHours,
    getOpenMarkets,
    getClosedMarkets,
    getMinutesUntilOpen,
    getMinutesUntilClose,
    getSessionProgress,
    getNextTradingSession,
    getStatusByAssetClass,
    getTradingStatusSummary,
    refresh,
    clearError,
  };

  return (
    <MarketStatusContext.Provider value={contextValue}>
      {children}
    </MarketStatusContext.Provider>
  );
};

// Hook to use the context
export const useMarketStatus = (): MarketStatusContextType => {
  const context = useContext(MarketStatusContext);
  if (!context) {
    throw new Error('useMarketStatus must be used within a MarketStatusProvider');
  }
  return context;
};

// Specialized hooks
export const useMarketTiming = (marketId: string) => {
  const {
    getMinutesUntilOpen,
    getMinutesUntilClose,
    getSessionProgress,
    getNextTradingSession,
    isMarketOpen,
    isMarketClosed,
    isExtendedHours
  } = useMarketStatus();

  return {
    minutesUntilOpen: getMinutesUntilOpen(marketId),
    minutesUntilClose: getMinutesUntilClose(marketId),
    sessionProgress: getSessionProgress(marketId),
    nextTradingSession: getNextTradingSession(marketId),
    isOpen: isMarketOpen(marketId),
    isClosed: isMarketClosed(marketId),
    isExtendedHours: isExtendedHours(marketId),
  };
};

export const useTradingSummary = () => {
  const { getTradingStatusSummary } = useMarketStatus();
  return getTradingStatusSummary();
};

export const useOpenMarkets = () => {
  const { getOpenMarkets } = useMarketStatus();
  return getOpenMarkets();
};

export const useMarketsByStatus = (status: 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET') => {
  const { state } = useMarketStatus();
  return Object.values(state.statuses).filter(marketStatus => marketStatus.status === status);
};