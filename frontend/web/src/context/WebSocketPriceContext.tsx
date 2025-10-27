/**
 * WebSocket Price Context for Web Frontend
 * Provides real-time cryptocurrency price updates via SignalR WebSocket
 */

import React, { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react';
import { WebSocketService, createWebSocketService } from '../services/websocketService';
import { MarketDataDto } from '../services/marketDataService';
import { normalizeMarketData } from '../utils/priceFormatting';

interface PriceData {
  symbol: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  high?: number;
  low?: number;
  open?: number;
  lastUpdate: string;
}

interface WebSocketPriceContextType {
  prices: Record<string, PriceData>;
  isConnected: boolean;
  connectionStatus: string;
  error: string | null;
  subscribeToSymbol: (symbol: string) => Promise<void>;
  unsubscribeFromSymbol: (symbol: string) => void;
  reconnect: () => void;
}

const WebSocketPriceContext = createContext<WebSocketPriceContextType | undefined>(undefined);

export const WebSocketPriceProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [prices, setPrices] = useState<Record<string, PriceData>>({});
  const [isConnected, setIsConnected] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState('Disconnected');
  const [error, setError] = useState<string | null>(null);
  const wsServiceRef = useRef<WebSocketService | null>(null);
  const subscribedSymbolsRef = useRef<Set<string>>(new Set());

  // Initialize WebSocket connection
  useEffect(() => {
    console.log('[WebSocketPriceContext] Initializing WebSocket connection');

    // Get backend URL from environment variable
    const backendUrl = import.meta.env.VITE_BACKEND_URL || 'http://localhost:8080';
    const wsUrl = `${backendUrl}/hubs/market-data`;
    console.log('[WebSocketPriceContext] WebSocket URL:', wsUrl);

    const wsService = createWebSocketService({
      url: wsUrl,
      onMarketDataUpdate: (data: MarketDataDto) => {
        console.log('[WebSocketPriceContext] Received market data update:', data.symbol, data.price);

        // Normalize the data to handle Turkish locale decimal parsing
        const normalized = normalizeMarketData({
          symbol: data.symbol,
          price: data.price,
          change: data.priceChange,
          changePercent: data.priceChangePercent,
          volume: data.volume,
          assetClass: 'CRYPTO'
        });

        const priceData: PriceData = {
          symbol: data.symbol,
          price: normalized.price,
          change: normalized.changePercent, // Use percent as change for now
          changePercent: normalized.changePercent,
          volume: normalized.volume,
          high: data.high || undefined,
          low: data.low || undefined,
          open: data.open || undefined,
          lastUpdate: data.lastUpdate || new Date().toISOString(),
        };

        setPrices(prev => ({
          ...prev,
          [data.symbol]: priceData,
        }));
      },
      onConnectionStatusChange: (connected: boolean) => {
        console.log('[WebSocketPriceContext] Connection status changed:', connected);
        setIsConnected(connected);
        setConnectionStatus(connected ? 'Connected' : 'Disconnected');

        // Re-subscribe to all symbols when reconnected
        if (connected && subscribedSymbolsRef.current.size > 0) {
          const symbols = Array.from(subscribedSymbolsRef.current);
          console.log('[WebSocketPriceContext] Re-subscribing to symbols after reconnect:', symbols);
          wsService.subscribeToMultipleSymbols(symbols).catch(err => {
            console.error('[WebSocketPriceContext] Failed to re-subscribe:', err);
          });
        }
      },
      onError: (err: Error) => {
        console.error('[WebSocketPriceContext] WebSocket error:', err.message);
        setError(err.message);
        setConnectionStatus('Error: ' + err.message);
      },
    });

    wsServiceRef.current = wsService;

    // Connect to WebSocket
    wsService.connect().catch(err => {
      console.error('[WebSocketPriceContext] Failed to connect:', err);
      setError(err.message);
    });

    // Cleanup on unmount
    return () => {
      console.log('[WebSocketPriceContext] Cleaning up WebSocket connection');
      wsService.destroy();
      wsServiceRef.current = null;
      subscribedSymbolsRef.current.clear();
    };
  }, []);

  const subscribeToSymbol = useCallback(async (symbol: string) => {
    const wsService = wsServiceRef.current;
    if (!wsService) {
      console.warn('[WebSocketPriceContext] WebSocket service not initialized');
      return;
    }

    if (subscribedSymbolsRef.current.has(symbol)) {
      console.log('[WebSocketPriceContext] Already subscribed to', symbol);
      return;
    }

    try {
      console.log('[WebSocketPriceContext] Subscribing to', symbol);
      await wsService.subscribeToSymbol(symbol);
      subscribedSymbolsRef.current.add(symbol);
    } catch (err: any) {
      console.error('[WebSocketPriceContext] Failed to subscribe to', symbol, err);
      setError(`Failed to subscribe to ${symbol}: ${err.message}`);
    }
  }, []);

  const unsubscribeFromSymbol = useCallback((symbol: string) => {
    const wsService = wsServiceRef.current;
    if (!wsService || !subscribedSymbolsRef.current.has(symbol)) {
      return;
    }

    console.log('[WebSocketPriceContext] Unsubscribing from', symbol);
    wsService.unsubscribeFromSymbol(symbol);
    subscribedSymbolsRef.current.delete(symbol);
  }, []);

  const reconnect = useCallback(() => {
    const wsService = wsServiceRef.current;
    if (!wsService) return;

    console.log('[WebSocketPriceContext] Manual reconnect requested');
    setError(null);
    wsService.disconnect().then(() => {
      wsService.connect().catch(err => {
        console.error('[WebSocketPriceContext] Reconnect failed:', err);
        setError(err.message);
      });
    });
  }, []);

  const contextValue: WebSocketPriceContextType = {
    prices,
    isConnected,
    connectionStatus,
    error,
    subscribeToSymbol,
    unsubscribeFromSymbol,
    reconnect,
  };

  return (
    <WebSocketPriceContext.Provider value={contextValue}>
      {children}
    </WebSocketPriceContext.Provider>
  );
};

export const useWebSocketPrices = () => {
  const context = useContext(WebSocketPriceContext);
  if (!context) {
    throw new Error('useWebSocketPrices must be used within a WebSocketPriceProvider');
  }
  return context;
};

export default WebSocketPriceContext;
