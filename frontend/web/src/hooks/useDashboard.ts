import { useEffect, useRef, useState } from 'react';
import { useAppDispatch } from '../store/hooks';
import { addNotification } from '../store/slices/uiSlice';
import {
  enhancedSignalRService,
  DashboardSnapshot,
  IndicatorUpdate,
  SignalNotification,
  PortfolioUpdate
} from '../services/signalRService';

export interface UseDashboardOptions {
  enabled?: boolean;
  symbols?: string[];
  timeframes?: string[];
  autoSubscribeToPortfolio?: boolean;
  minSignalConfidence?: number;
  onHealthStatusChange?: (connected: boolean) => void;
}

export interface DashboardState {
  connected: boolean;
  connecting: boolean;
  error: Error | null;
  connectionState: string;
  dashboardData: DashboardSnapshot | null;
  indicators: Record<string, IndicatorUpdate>;
  signals: SignalNotification[];
  portfolio: PortfolioUpdate | null;
  lastUpdate: string | null;
}

export const useDashboard = (options: UseDashboardOptions = {}) => {
  const dispatch = useAppDispatch();
  const connectionRef = useRef<any>(null);
  const [state, setState] = useState<DashboardState>({
    connected: false,
    connecting: false,
    error: null,
    connectionState: 'Disconnected',
    dashboardData: null,
    indicators: {},
    signals: [],
    portfolio: null,
    lastUpdate: null
  });

  const {
    enabled = true,
    symbols = [],
    timeframes = ['1h', '4h', '1d'],
    autoSubscribeToPortfolio = true,
    minSignalConfidence = 50,
    onHealthStatusChange
  } = options;

  // Handle dashboard snapshot updates
  const handleDashboardSnapshot = (snapshot: DashboardSnapshot) => {
    setState(prev => ({
      ...prev,
      dashboardData: snapshot,
      portfolio: snapshot.portfolio || prev.portfolio,
      lastUpdate: snapshot.timestamp
    }));

    dispatch(addNotification({
      type: 'info',
      message: 'Dashboard data updated'
    }));
  };

  // Handle indicator updates
  const handleIndicatorUpdate = (update: IndicatorUpdate) => {
    const key = `${update.symbol}_${update.timeframe}`;
    setState(prev => ({
      ...prev,
      indicators: {
        ...prev.indicators,
        [key]: update
      },
      lastUpdate: update.timestamp
    }));
  };

  // Handle signal alerts
  const handleSignalAlert = (signal: SignalNotification) => {
    setState(prev => ({
      ...prev,
      signals: [signal, ...prev.signals.slice(0, 9)], // Keep last 10 signals
      lastUpdate: signal.generatedAt
    }));

    dispatch(addNotification({
      type: 'success',
      message: `Trading signal: ${signal.type} ${signal.symbol} (${signal.confidence}% confidence)`
    }));
  };

  // Handle portfolio updates
  const handlePortfolioUpdate = (portfolio: PortfolioUpdate) => {
    setState(prev => ({
      ...prev,
      portfolio,
      lastUpdate: portfolio.lastUpdated
    }));
  };

  // Handle connection status changes
  const handleConnectionStatusChange = (connected: boolean) => {
    setState(prev => ({
      ...prev,
      connected,
      connecting: false,
      connectionState: connected ? 'Connected' : 'Disconnected'
    }));

    onHealthStatusChange?.(connected);

    if (connected) {
      dispatch(addNotification({
        type: 'success',
        message: 'Dashboard real-time data connected'
      }));

      // Set up subscriptions after connection
      setupSubscriptions();
    } else {
      dispatch(addNotification({
        type: 'warning',
        message: 'Dashboard real-time data disconnected'
      }));
    }
  };

  // Handle dashboard errors
  const handleDashboardError = (error: string) => {
    setState(prev => ({
      ...prev,
      error: new Error(error)
    }));

    dispatch(addNotification({
      type: 'error',
      message: `Dashboard error: ${error}`
    }));
  };

  // Set up all subscriptions
  const setupSubscriptions = async () => {
    try {
      // Subscribe to portfolio updates if enabled
      if (autoSubscribeToPortfolio) {
        await enhancedSignalRService.subscribeToPortfolio();
      }

      // Subscribe to indicators if symbols are provided
      if (symbols.length > 0 && timeframes.length > 0) {
        await enhancedSignalRService.subscribeToIndicators(symbols, timeframes);
      }

      // Subscribe to trading signals if symbols are provided
      if (symbols.length > 0) {
        await enhancedSignalRService.subscribeToDashboardSignals(symbols, minSignalConfidence);
      }

      // Request initial dashboard snapshot
      await enhancedSignalRService.requestDashboardUpdate('dashboard');

    } catch (error) {
      console.error('Failed to set up dashboard subscriptions:', error);
      dispatch(addNotification({
        type: 'error',
        message: 'Failed to set up dashboard subscriptions'
      }));
    }
  };

  // Connect to dashboard hub
  const connect = async () => {
    if (!enabled || enhancedSignalRService.isDashboardConnected()) {
      return;
    }

    setState(prev => ({ ...prev, connecting: true, error: null }));

    try {
      // Use environment variable for backend URL with fallback
      const backendUrl = import.meta.env.VITE_BACKEND_URL || 'http://localhost:5002';

      await enhancedSignalRService.connectToDashboardHub({
        url: backendUrl,
        hubPath: '/hubs/dashboard',
        onConnected: () => handleConnectionStatusChange(true),
        onDisconnected: (error) => {
          handleConnectionStatusChange(false);
          if (error) {
            setState(prev => ({ ...prev, error }));
          }
        },
        onReconnecting: () => {
          setState(prev => ({ ...prev, connecting: true, connectionState: 'Reconnecting' }));
        },
        onReconnected: () => handleConnectionStatusChange(true),
        onError: (error) => {
          setState(prev => ({ ...prev, error, connecting: false }));
          dispatch(addNotification({
            type: 'error',
            message: `Dashboard connection error: ${error.message}`
          }));
        }
      });

      // Set up event listeners
      enhancedSignalRService.onDashboardSnapshot(handleDashboardSnapshot);
      enhancedSignalRService.onIndicatorUpdate(handleIndicatorUpdate);
      enhancedSignalRService.onSignalAlert(handleSignalAlert);
      enhancedSignalRService.onPortfolioUpdate(handlePortfolioUpdate);
      enhancedSignalRService.onDashboardError(handleDashboardError);

    } catch (error) {
      console.warn('Dashboard connection failed, continuing without real-time updates:', error);
      setState(prev => ({
        ...prev,
        error: error as Error,
        connecting: false,
        connected: false
      }));

      dispatch(addNotification({
        type: 'info',
        message: 'Dashboard running in static mode (real-time updates unavailable)'
      }));
    }
  };

  // Disconnect from dashboard hub
  const disconnect = async () => {
    if (enhancedSignalRService.isDashboardConnected()) {
      await enhancedSignalRService.disconnect();
    }
    setState(prev => ({
      ...prev,
      connected: false,
      connecting: false,
      connectionState: 'Disconnected'
    }));
  };

  // Subscribe to additional symbols
  const subscribeToSymbols = async (symbolIds: string[], newTimeframes?: string[]) => {
    if (!enhancedSignalRService.isDashboardConnected()) {
      console.warn('Cannot subscribe: Dashboard not connected');
      return;
    }

    try {
      const framesToUse = newTimeframes || timeframes;
      await enhancedSignalRService.subscribeToIndicators(symbolIds, framesToUse);
      await enhancedSignalRService.subscribeToDashboardSignals(symbolIds, minSignalConfidence);
    } catch (error) {
      console.error('Failed to subscribe to symbols:', error);
      dispatch(addNotification({
        type: 'error',
        message: 'Failed to subscribe to dashboard data'
      }));
    }
  };

  // Request specific data updates
  const requestUpdate = async (dataType: 'portfolio' | 'indicators' | 'signals' | 'dashboard', parameters?: Record<string, any>) => {
    if (!enhancedSignalRService.isDashboardConnected()) {
      console.warn('Cannot request update: Dashboard not connected');
      return;
    }

    try {
      await enhancedSignalRService.requestDashboardUpdate(dataType, parameters);
    } catch (error) {
      console.error('Failed to request dashboard update:', error);
      dispatch(addNotification({
        type: 'error',
        message: 'Failed to request dashboard update'
      }));
    }
  };

  // Test dashboard connection
  const testConnection = async () => {
    try {
      const result = await enhancedSignalRService.testDashboardConnection();
      dispatch(addNotification({
        type: result.success ? 'success' : 'error',
        message: result.success ? 'Dashboard connection test successful' : `Dashboard connection test failed: ${result.error}`
      }));
      return result;
    } catch (error) {
      dispatch(addNotification({
        type: 'error',
        message: 'Dashboard connection test failed'
      }));
      return { success: false, error: (error as Error).message };
    }
  };

  // Effect to manage connection lifecycle
  useEffect(() => {
    if (enabled) {
      connect();
    }

    return () => {
      // Don't disconnect here as other components might be using the same connection
      // The service manages connection lifecycle
    };
  }, [enabled]);

  // Effect to manage symbol subscriptions
  useEffect(() => {
    if (state.connected && symbols.length > 0) {
      subscribeToSymbols(symbols);
    }
  }, [symbols, timeframes, state.connected, minSignalConfidence]);

  return {
    ...state,
    connect,
    disconnect,
    subscribeToSymbols,
    requestUpdate,
    testConnection,

    // Derived state
    hasData: Boolean(state.dashboardData || state.portfolio || Object.keys(state.indicators).length > 0),
    indicatorCount: Object.keys(state.indicators).length,
    signalCount: state.signals.length,

    // Service access for advanced usage
    service: enhancedSignalRService
  };
};

// Selector hooks for specific dashboard data
export const useDashboardSnapshot = () => {
  const { dashboardData } = useDashboard();
  return dashboardData;
};

export const useDashboardPortfolio = () => {
  const { portfolio } = useDashboard();
  return portfolio;
};

export const useDashboardSignals = () => {
  const { signals } = useDashboard();
  return signals;
};

export const useDashboardIndicators = () => {
  const { indicators } = useDashboard();
  return indicators;
};

export const useDashboardConnection = () => {
  const { connected, connecting, connectionState, error } = useDashboard();
  return { connected, connecting, connectionState, error };
};