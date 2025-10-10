import { useEffect, useState, useCallback } from 'react';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import { updateConnectionStatus } from '../store/slices/marketSlice';
import { marketDataService, ConnectionStatus } from '../services/marketDataService';
import { addNotification } from '../store/slices/uiSlice';

export interface HealthStatusState {
  isHealthy: boolean;
  isChecking: boolean;
  lastCheck: string | null;
  error: string | null;
  apiHealth: ConnectionStatus | null;
  websocketConnected: boolean;
}

export interface UseHealthStatusOptions {
  enabled?: boolean;
  checkInterval?: number; // in milliseconds
  onHealthChange?: (isHealthy: boolean) => void;
}

export const useHealthStatus = (options: UseHealthStatusOptions = {}) => {
  const {
    enabled = true,
    checkInterval = 30000, // Check every 30 seconds
    onHealthChange
  } = options;

  const dispatch = useAppDispatch();
  const { connectionStatus: reduxConnectionStatus } = useAppSelector((state) => state.market);
  const [state, setState] = useState<HealthStatusState>({
    isHealthy: false,
    isChecking: false,
    lastCheck: null,
    error: null,
    apiHealth: null,
    websocketConnected: false
  });

  // Check API health endpoint
  const checkApiHealth = useCallback(async (): Promise<ConnectionStatus | null> => {
    try {
      setState(prev => ({ ...prev, isChecking: true, error: null }));

      const healthStatus = await marketDataService.getDataSourceHealth();

      // Update Redux store with the health status
      dispatch(updateConnectionStatus(healthStatus));

      return healthStatus;
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Health check failed';
      console.error('API health check failed:', errorMessage);

      setState(prev => ({ ...prev, error: errorMessage }));

      // Still update Redux with offline status
      const offlineStatus: ConnectionStatus = {
        alpaca: {
          crypto: false,
          nasdaq: false,
          nyse: false,
          error: errorMessage,
          lastUpdate: new Date().toISOString()
        },
        bist: {
          connected: false,
          error: errorMessage,
          lastUpdate: new Date().toISOString()
        },
        overall: 'disconnected'
      };
      dispatch(updateConnectionStatus(offlineStatus));

      return null;
    } finally {
      setState(prev => ({ ...prev, isChecking: false, lastCheck: new Date().toISOString() }));
    }
  }, [dispatch]);

  // Perform comprehensive health check
  const performHealthCheck = useCallback(async () => {
    if (!enabled) return;

    const apiHealth = await checkApiHealth();

    // Get WebSocket status (this would come from useWebSocket hook)
    // For now, we'll rely on Redux state or check WebSocket service directly
    const websocketConnected = false; // This will be updated by WebSocket hook

    // Determine overall health
    const isApiHealthy = apiHealth?.overall === 'connected' || apiHealth?.overall === 'partial';
    const isHealthy = isApiHealthy; // Can add websocketConnected as requirement if needed

    setState(prev => ({
      ...prev,
      isHealthy,
      apiHealth,
      websocketConnected
    }));

    // Notify about health changes
    if (onHealthChange) {
      onHealthChange(isHealthy);
    }

    // Show notifications for significant health changes
    if (apiHealth) {
      const wasConnected = reduxConnectionStatus?.overall === 'connected' || reduxConnectionStatus?.overall === 'partial';
      const isConnected = apiHealth.overall === 'connected' || apiHealth.overall === 'partial';

      if (!wasConnected && isConnected) {
        dispatch(addNotification({
          type: 'success',
          message: 'Market data connection restored'
        }));
      } else if (wasConnected && !isConnected && reduxConnectionStatus) {
        dispatch(addNotification({
          type: 'warning',
          message: 'Market data connection lost - using cached data'
        }));
      }
    }
  }, [enabled, checkApiHealth, onHealthChange, reduxConnectionStatus, dispatch]);

  // Manual health check function
  const checkHealth = useCallback(() => {
    return performHealthCheck();
  }, [performHealthCheck]);

  // Setup periodic health checks
  useEffect(() => {
    if (!enabled) return;

    // Initial health check
    performHealthCheck();

    // Setup interval for periodic checks
    const interval = setInterval(performHealthCheck, checkInterval);

    return () => {
      clearInterval(interval);
    };
  }, [enabled, checkInterval, performHealthCheck]);

  // Update WebSocket connection status when it changes (this would be called by WebSocket hook)
  const updateWebSocketStatus = useCallback((connected: boolean) => {
    setState(prev => {
      const newState = { ...prev, websocketConnected: connected };
      const isHealthy = (prev.apiHealth?.overall === 'connected' || prev.apiHealth?.overall === 'partial');
      newState.isHealthy = isHealthy; // Can add && connected if WebSocket is required
      return newState;
    });
  }, []);

  return {
    ...state,
    checkHealth,
    updateWebSocketStatus,
    // Computed values
    overallStatus: state.apiHealth?.overall || 'disconnected',
    alpacaHealthy: state.apiHealth?.alpaca?.crypto || state.apiHealth?.alpaca?.nasdaq || state.apiHealth?.alpaca?.nyse || false,
    bistHealthy: state.apiHealth?.bist?.connected || false,
    // Helper functions
    isConnected: () => state.isHealthy,
    getHealthSummary: () => ({
      api: state.apiHealth?.overall || 'disconnected',
      websocket: state.websocketConnected ? 'connected' : 'disconnected',
      overall: state.isHealthy ? 'healthy' : 'unhealthy'
    })
  };
};