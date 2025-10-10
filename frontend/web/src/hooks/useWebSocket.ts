import { useEffect, useRef, useState } from 'react';
import { useAppDispatch } from '../store/hooks';
import { mergeCryptoData } from '../store/slices/marketSlice';
import { addNotification } from '../store/slices/uiSlice';
import { createWebSocketService, WebSocketService } from '../services/websocketService';
import { enhancedSignalRService } from '../services/signalRService';
import { MarketDataDto } from '../services/marketDataService';

export interface UseWebSocketOptions {
  url?: string;
  enabled?: boolean;
  symbols?: string[];
  onHealthStatusChange?: (connected: boolean) => void;
  useEnhancedService?: boolean; // New option to use enhanced SignalR service
  assetClass?: string; // Asset class for enhanced service
}

export interface WebSocketState {
  connected: boolean;
  connecting: boolean;
  error: Error | null;
  connectionState: string;
}

export const useWebSocket = (options: UseWebSocketOptions = {}) => {
  const dispatch = useAppDispatch();
  const serviceRef = useRef<WebSocketService | null>(null);
  const [state, setState] = useState<WebSocketState>({
    connected: false,
    connecting: false,
    error: null,
    connectionState: 'Disconnected'
  });

  const {
    url = import.meta.env.VITE_WEBSOCKET_HUB_PATH || '/hubs/dashboard',
    enabled = true,
    symbols = [],
    onHealthStatusChange,
    useEnhancedService = false,
    assetClass = 'CRYPTO'
  } = options;

  // Handle market data updates (simplified for now)
  const handleMarketDataUpdate = (data: MarketDataDto) => {
    // Merge real-time tick into crypto slice by symbol
    dispatch(mergeCryptoData(data));
  };

  // Handle connection status changes
  const handleConnectionStatusChange = (connected: boolean) => {
    setState(prev => ({
      ...prev,
      connected,
      connecting: false,
      connectionState: connected ? 'Connected' : 'Disconnected'
    }));

    // Notify health status hook about WebSocket status change
    onHealthStatusChange?.(connected);

    if (connected) {
      dispatch(addNotification({
        type: 'success',
        message: 'Real-time market data connected'
      }));

      // If explicit symbols provided, subscribe to them
      const subscribe = async () => {
        try {
          if (symbols.length > 0 && serviceRef.current) {
            await serviceRef.current.subscribeToMultipleSymbols(symbols);
            return;
          }
          // Subscribe to the specific crypto symbols that are being actively streamed
          // These match the symbols shown in the UI (filtered in marketDataService)
          const cryptoSymbols = ['BTCUSD', 'ETHUSD', 'ADAUSD', 'SOLUSD', 'AVAXUSD'];
          console.log('Subscribing to actively streamed crypto symbols:', cryptoSymbols);
          if (serviceRef.current) {
            await serviceRef.current.subscribeToMultipleSymbols(cryptoSymbols);
          }
        } catch (error) {
          console.error('Failed to subscribe to crypto symbols:', error);
          dispatch(addNotification({
            type: 'error',
            message: 'Failed to subscribe to crypto symbols'
          }));
        }
      };
      subscribe();
    } else {
      dispatch(addNotification({
        type: 'warning',
        message: 'Real-time market data disconnected'
      }));
    }
  };

  // Handle WebSocket errors
  const handleError = (error: Error) => {
    setState(prev => ({
      ...prev,
      error,
      connecting: false,
      connected: false,
      connectionState: 'Disconnected'
    }));

    console.error('WebSocket error:', error);
    dispatch(addNotification({
      type: 'error',
      message: `WebSocket error: ${error.message}`
    }));
  };

  // Connect to WebSocket with improved error handling
  const connect = async () => {
    if (!enabled) {
      return;
    }

    // Check if using enhanced service or legacy service
    if (useEnhancedService) {
      // Use enhanced SignalR service
      if (enhancedSignalRService.isMarketDataConnected()) {
        return;
      }

      setState(prev => ({ ...prev, connecting: true, error: null }));

      try {
        const backendUrl = import.meta.env.VITE_BACKEND_URL || 'http://localhost:5002';

        await enhancedSignalRService.connectToMarketDataHub({
          url: backendUrl,
          hubPath: '/hubs/marketdata',
          onConnected: () => handleConnectionStatusChange(true),
          onDisconnected: (error) => {
            handleConnectionStatusChange(false);
            if (error) {
              handleError(error);
            }
          },
          onReconnecting: () => {
            setState(prev => ({ ...prev, connecting: true, connectionState: 'Reconnecting' }));
          },
          onReconnected: () => handleConnectionStatusChange(true),
          onError: handleError
        });

        // Set up event listeners for enhanced service
        enhancedSignalRService.onPriceUpdate((data: any) => {
          // Transform SignalR data to MarketDataDto format
          const transformedData: MarketDataDto = {
            symbol: data.Symbol || data.symbol,
            displayName: data.Symbol || data.symbol,
            price: Number(data.Price || data.price),
            priceChange: Number(data.PriceChange || 0),
            priceChangePercent: Number(data.Change24h || data.changePercent || 0),
            volume: Number(data.Volume || data.volume || 0),
            high24h: Number(data.High24h),
            low24h: Number(data.Low24h),
            marketCap: Number(data.MarketCap),
            lastUpdate: data.Timestamp || data.timestamp || new Date().toISOString(),
            source: 'signalr',
            isRealTime: true
          };
          handleMarketDataUpdate(transformedData);
        });

        enhancedSignalRService.onConnectionStatus((status: any) => {
          console.log('SignalR connection status:', status);
        });

        enhancedSignalRService.onSubscriptionConfirmed((data: any) => {
          console.log('SignalR subscription confirmed:', data);
        });

        enhancedSignalRService.onSubscriptionError((error: any) => {
          console.error('SignalR subscription error:', error);
          dispatch(addNotification({
            type: 'error',
            message: `Subscription error: ${error.message || 'Unknown error'}`
          }));
        });

      } catch (error) {
        console.warn('Enhanced SignalR connection failed, falling back to polling:', error);
        handleError(error as Error);
        dispatch(addNotification({
          type: 'info',
          message: 'Using polling for market data (SignalR unavailable)'
        }));
      }
    } else {
      // Use legacy WebSocket service
      if (serviceRef.current?.isConnected()) {
        return;
      }

      setState(prev => ({ ...prev, connecting: true, error: null }));

      try {
        if (!serviceRef.current) {
          const backendUrl = import.meta.env.VITE_BACKEND_URL || 'http://localhost:5002';
          serviceRef.current = createWebSocketService({
            url: `${backendUrl}${url}`,
            onMarketDataUpdate: handleMarketDataUpdate,
            onConnectionStatusChange: handleConnectionStatusChange,
            onError: handleError
          });
        }

        await serviceRef.current.connect();
      } catch (error) {
        console.warn('WebSocket connection failed, falling back to polling:', error);
        handleError(error as Error);
        dispatch(addNotification({
          type: 'info',
          message: 'Using polling for market data (WebSocket unavailable)'
        }));
      }
    }
  };

  // Disconnect from WebSocket
  const disconnect = async () => {
    if (serviceRef.current) {
      await serviceRef.current.disconnect();
      serviceRef.current = null;
    }
    setState(prev => ({
      ...prev,
      connected: false,
      connecting: false,
      connectionState: 'Disconnected'
    }));
  };

  // Subscribe to additional symbols
  const subscribeToSymbols = async (symbolIds: string[]) => {
    if (useEnhancedService) {
      if (!enhancedSignalRService.isMarketDataConnected()) {
        console.warn('Cannot subscribe: Enhanced SignalR not connected');
        return;
      }

      try {
        await enhancedSignalRService.subscribeToMarketDataPriceUpdates(assetClass, symbolIds);
      } catch (error) {
        console.error('Failed to subscribe to symbols via SignalR:', error);
        dispatch(addNotification({
          type: 'error',
          message: 'Failed to subscribe to market data'
        }));
      }
    } else {
      if (!serviceRef.current?.isConnected()) {
        console.warn('Cannot subscribe: WebSocket not connected');
        return;
      }

      try {
        await serviceRef.current.subscribeToMultipleSymbols(symbolIds);
      } catch (error) {
        console.error('Failed to subscribe to symbols:', error);
        dispatch(addNotification({
          type: 'error',
          message: 'Failed to subscribe to market data'
        }));
      }
    }
  };

  // Unsubscribe from symbols
  const unsubscribeFromSymbols = async (symbolIds: string[]) => {
    if (useEnhancedService) {
      if (!enhancedSignalRService.isMarketDataConnected()) {
        return;
      }

      try {
        await enhancedSignalRService.unsubscribeFromPriceUpdates(assetClass, symbolIds);
      } catch (error) {
        console.error('Failed to unsubscribe from symbols via SignalR:', error);
      }
    } else {
      if (!serviceRef.current?.isConnected()) {
        return;
      }

      try {
        await Promise.all(symbolIds.map(id => serviceRef.current!.unsubscribeFromSymbol(id)));
      } catch (error) {
        console.error('Failed to unsubscribe from symbols:', error);
      }
    }
  };

  // Effect to manage connection lifecycle
  useEffect(() => {
    if (enabled) {
      connect();
    }

    return () => {
      if (serviceRef.current) {
        serviceRef.current.disconnect();
      }
    };
  }, [enabled, url]);

  // Effect to manage symbol subscriptions
  useEffect(() => {
    if (state.connected && symbols.length > 0) {
      subscribeToSymbols(symbols);
    }

    // Cleanup: unsubscribe from previous symbols when symbols change
    return () => {
      if (state.connected && symbols.length > 0) {
        unsubscribeFromSymbols(symbols);
      }
    };
  }, [symbols, state.connected]);

  return {
    ...state,
    connect,
    disconnect,
    subscribeToSymbols,
    unsubscribeFromSymbols,
    service: useEnhancedService ? enhancedSignalRService : serviceRef.current,
    // Enhanced service methods when available
    ...(useEnhancedService && {
      enhancedService: enhancedSignalRService,
      subscribeToAssetClass: (assetClassCode: string) =>
        enhancedSignalRService.subscribeToAssetClass(assetClassCode),
      subscribeToMarketStatus: (markets: string[]) =>
        enhancedSignalRService.subscribeToMarketStatus(markets),
      getMarketStatus: () => enhancedSignalRService.getMarketStatus(),
      testConnection: () => enhancedSignalRService.testMarketDataConnection()
    })
  };
};
