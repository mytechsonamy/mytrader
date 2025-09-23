import React, { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';
import Constants from 'expo-constants';
import { API_BASE_URL as CFG_API_BASE_URL, WS_BASE_URL as CFG_WS_BASE_URL } from '../config';
import * as signalR from '@microsoft/signalr';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { websocketService } from '../services/websocketService';
import { multiAssetApiService } from '../services/multiAssetApi';
import { UnifiedMarketDataDto, EnhancedSymbolDto, AssetClassType } from '../types';

// Legacy price data interface (maintained for backward compatibility)
interface LegacyPriceData {
  [symbol: string]: {
    price: number;
    change: number;
    timestamp: string;
  };
}

// Enhanced price data interface
interface EnhancedPriceData {
  [symbolId: string]: UnifiedMarketDataDto;
}

// Legacy symbol row (maintained for backward compatibility)
type LegacySymbolRow = { id: string; ticker: string; display: string; venue: string; baseCcy: string; quoteCcy: string; isTracked: boolean };

// Enhanced symbol data
interface SymbolSubscription {
  symbolId: string;
  assetClass: AssetClassType;
  subscriptionId?: string;
}

interface EnhancedPriceContextType {
  // Legacy properties (maintained for backward compatibility)
  prices: LegacyPriceData;
  isConnected: boolean;
  symbols: LegacySymbolRow[];
  getPrice: (symbol: string) => { price: number; change: number } | null;

  // Enhanced multi-asset properties
  enhancedPrices: EnhancedPriceData;
  trackedSymbols: EnhancedSymbolDto[];
  subscriptions: SymbolSubscription[];
  connectionStatus: 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error';

  // Enhanced methods
  getEnhancedPrice: (symbolId: string) => UnifiedMarketDataDto | null;
  getPriceBySymbol: (symbol: string, assetClass?: AssetClassType) => UnifiedMarketDataDto | null;
  subscribeToSymbols: (symbolIds: string[], assetClass?: AssetClassType) => Promise<void>;
  unsubscribeFromSymbols: (symbolIds: string[]) => Promise<void>;
  addTrackedSymbol: (symbol: EnhancedSymbolDto) => Promise<void>;
  removeTrackedSymbol: (symbolId: string) => Promise<void>;
  getSymbolsByAssetClass: (assetClass: AssetClassType) => EnhancedSymbolDto[];
  refreshPrices: () => Promise<void>;
  isSymbolTracked: (symbolId: string) => boolean;

  // Utility methods
  getAssetClassSummary: () => Record<AssetClassType, {
    totalSymbols: number;
    gainers: number;
    losers: number;
    averageChange: number;
  }>;
}

const EnhancedPriceContext = createContext<EnhancedPriceContextType>({
  // Legacy defaults
  prices: {},
  isConnected: false,
  symbols: [],
  getPrice: () => null,

  // Enhanced defaults
  enhancedPrices: {},
  trackedSymbols: [],
  subscriptions: [],
  connectionStatus: 'disconnected',
  getEnhancedPrice: () => null,
  getPriceBySymbol: () => null,
  subscribeToSymbols: async () => {},
  unsubscribeFromSymbols: async () => {},
  addTrackedSymbol: async () => {},
  removeTrackedSymbol: async () => {},
  getSymbolsByAssetClass: () => [],
  refreshPrices: async () => {},
  isSymbolTracked: () => false,
  getAssetClassSummary: () => ({} as any),
});

export const useEnhancedPrices = () => useContext(EnhancedPriceContext);

interface EnhancedPriceProviderProps {
  children: ReactNode;
}

export const EnhancedPriceProvider: React.FC<EnhancedPriceProviderProps> = ({ children }) => {
  // Legacy state (maintained for backward compatibility)
  const [prices, setPrices] = useState<LegacyPriceData>({});
  const [symbols, setSymbols] = useState<LegacySymbolRow[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  // Enhanced multi-asset state
  const [enhancedPrices, setEnhancedPrices] = useState<EnhancedPriceData>({});
  const [trackedSymbols, setTrackedSymbols] = useState<EnhancedSymbolDto[]>([]);
  const [subscriptions, setSubscriptions] = useState<SymbolSubscription[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error'>('disconnected');

  const API_BASE_URL = Constants.expoConfig?.extra?.API_BASE_URL || CFG_API_BASE_URL || 'http://localhost:8080/api';
  const configuredHubUrl = (Constants.expoConfig?.extra?.WS_BASE_URL as string | undefined) || CFG_WS_BASE_URL;
  const rawHubUrl = configuredHubUrl || API_BASE_URL.replace('/api', '/hubs/trading');
  const SIGNALR_HUB_URL = rawHubUrl.startsWith('wss://')
    ? rawHubUrl.replace('wss://', 'https://')
    : rawHubUrl.startsWith('ws://')
    ? rawHubUrl.replace('ws://', 'http://')
    : rawHubUrl;

  // Initialize enhanced WebSocket service
  useEffect(() => {
    const initializeEnhancedService = async () => {
      try {
        setConnectionStatus('connecting');

        // Initialize services
        await multiAssetApiService.initialize();
        await websocketService.initialize();

        // Set up enhanced WebSocket event listeners
        websocketService.onConnectionStatus((status) => {
          setConnectionStatus(status);
          setIsConnected(status === 'connected');
        });

        websocketService.on('price_update', (data: UnifiedMarketDataDto) => {
          setEnhancedPrices(prev => ({
            ...prev,
            [data.symbolId]: data
          }));

          // Update legacy prices for backward compatibility
          const legacyFormat = {
            price: data.price,
            change: data.changePercent,
            timestamp: data.timestamp
          };
          setPrices(prev => ({
            ...prev,
            [data.symbol]: legacyFormat,
            [data.symbol.replace('USDT', '')]: legacyFormat // Support both formats
          }));
        });

        websocketService.on('batch_price_update', (dataArray: UnifiedMarketDataDto[]) => {
          const enhancedUpdates: EnhancedPriceData = {};
          const legacyUpdates: LegacyPriceData = {};

          dataArray.forEach(data => {
            enhancedUpdates[data.symbolId] = data;

            const legacyFormat = {
              price: data.price,
              change: data.changePercent,
              timestamp: data.timestamp
            };
            legacyUpdates[data.symbol] = legacyFormat;
            legacyUpdates[data.symbol.replace('USDT', '')] = legacyFormat;
          });

          setEnhancedPrices(prev => ({ ...prev, ...enhancedUpdates }));
          setPrices(prev => ({ ...prev, ...legacyUpdates }));
        });

        console.log('Enhanced price service initialized');
      } catch (error) {
        console.error('Failed to initialize enhanced price service:', error);
        setConnectionStatus('error');
      }
    };

    initializeEnhancedService();
  }, []);

  // Legacy data initialization (maintained for backward compatibility)
  useEffect(() => {
    const initializeLegacyData = async () => {
      try {
        const token = await AsyncStorage.getItem('session_token');
        const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};

        // 1) Fetch tracked symbols
        console.log('Fetching tracked symbols from:', API_BASE_URL);
        const symbolsResponse = await fetch(`${API_BASE_URL}/symbols/tracked`, { headers });
        if (symbolsResponse.ok) {
          const symbolsData = await symbolsResponse.json();
          console.log('Received symbols data:', symbolsData);
          setSymbols(symbolsData);
        }

        // 2) Fetch live prices snapshot
        console.log('Fetching real price data from:', API_BASE_URL);
        const pricesResponse = await fetch(`${API_BASE_URL}/prices/live`, { headers });
        if (pricesResponse.ok) {
          const pricesData = await pricesResponse.json();
          console.log('Received price data:', pricesData);
          if (pricesData.symbols) {
            setPrices(pricesData.symbols);
          }
        }
      } catch (error) {
        console.warn('Error fetching legacy data:', error);
        // Fallback to mock data if needed
        const fallbackPrices: LegacyPriceData = {
          BTCUSDT: { price: 43250.67, change: 2.34, timestamp: new Date().toISOString() },
          ETHUSDT: { price: 2634.89, change: -1.23, timestamp: new Date().toISOString() },
        };
        setPrices(fallbackPrices);
      }
    };

    initializeLegacyData();
  }, [API_BASE_URL]);

  // Legacy SignalR connection for backward compatibility
  useEffect(() => {
    let hubConnection: signalR.HubConnection | null = null;
    let reconnectTimer: NodeJS.Timeout | null = null;

    const connect = async () => {
      try {
        const token = await AsyncStorage.getItem('session_token');

        console.log('Connecting to SignalR hub:', SIGNALR_HUB_URL);

        const options: signalR.IHttpConnectionOptions = {
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets,
        };
        if (token) {
          options.accessTokenFactory = () => token;
        }

        hubConnection = new signalR.HubConnectionBuilder()
          .withUrl(SIGNALR_HUB_URL, options)
          .withAutomaticReconnect([0, 2000, 10000, 30000])
          .configureLogging(signalR.LogLevel.Information)
          .build();

        hubConnection.onclose(() => {
          console.log('SignalR connection closed');
          setIsConnected(false);
        });

        hubConnection.onreconnecting(() => {
          console.log('SignalR reconnecting...');
          setIsConnected(false);
        });

        hubConnection.onreconnected(() => {
          console.log('SignalR reconnected');
          setIsConnected(true);
        });

        await hubConnection.start();
        console.log('Connected to SignalR hub');
        setIsConnected(true);
        setConnection(hubConnection);

        // Legacy event handlers
        hubConnection.on('ReceivePriceUpdate', (data: any) => {
          try {
            console.log('Received SignalR price update:', data);

            if (data.symbol && data.price) {
              const symbol = data.symbol.replace('USDT', '');
              console.log(`Updating price for ${symbol}: $${data.price}`);
              setPrices(prev => ({
                ...prev,
                [symbol]: {
                  price: data.price,
                  change: data.change || 0,
                  timestamp: data.timestamp || new Date().toISOString(),
                }
              }));
            }
          } catch (error) {
            console.warn('Error processing SignalR price update:', error);
          }
        });

        hubConnection.on('ReceiveSignalUpdate', (data: any) => {
          try {
            console.log('Received SignalR signal update:', data);
          } catch (error) {
            console.warn('Error processing SignalR signal update:', error);
          }
        });

        hubConnection.on('ReceiveMarketData', (data: any) => {
          try {
            console.log('Received SignalR market data:', data);
            if (data.symbols) {
              const updates: LegacyPriceData = {};
              Object.keys(data.symbols).forEach(symbol => {
                const symbolData = data.symbols[symbol];
                if (symbolData.price) {
                  const cleanSymbol = symbol.replace('USDT', '');
                  updates[cleanSymbol] = {
                    price: symbolData.price,
                    change: symbolData.change || 0,
                    timestamp: symbolData.timestamp || new Date().toISOString(),
                  };
                }
              });
              setPrices(prev => ({ ...prev, ...updates }));
            }
          } catch (error) {
            console.warn('Error processing SignalR market data:', error);
          }
        });
      } catch (error) {
        console.error('Failed to create SignalR connection:', error);
        setIsConnected(false);

        if (!reconnectTimer) {
          reconnectTimer = setTimeout(() => {
            reconnectTimer = null;
            connect();
          }, 5000);
        }
      }
    };

    connect();

    return () => {
      if (reconnectTimer) {
        clearTimeout(reconnectTimer);
      }
      if (hubConnection) {
        hubConnection.stop();
      }
    };
  }, [SIGNALR_HUB_URL]);

  // Enhanced methods
  const getEnhancedPrice = useCallback((symbolId: string): UnifiedMarketDataDto | null => {
    return enhancedPrices[symbolId] || null;
  }, [enhancedPrices]);

  const getPriceBySymbol = useCallback((symbol: string, assetClass?: AssetClassType): UnifiedMarketDataDto | null => {
    // Find symbol in tracked symbols
    const trackedSymbol = trackedSymbols.find(s =>
      s.symbol === symbol && (!assetClass || s.assetClassName === assetClass)
    );

    if (trackedSymbol) {
      return enhancedPrices[trackedSymbol.id] || null;
    }

    return null;
  }, [enhancedPrices, trackedSymbols]);

  const subscribeToSymbols = useCallback(async (symbolIds: string[], assetClass?: AssetClassType) => {
    try {
      const subscriptionId = await websocketService.subscribeToPrices(symbolIds, assetClass);

      const newSubscription: SymbolSubscription = {
        symbolId: symbolIds.join(','),
        assetClass: assetClass || 'CRYPTO',
        subscriptionId
      };

      setSubscriptions(prev => [...prev, newSubscription]);

      // Load initial data for subscribed symbols
      const marketData = await multiAssetApiService.getBatchMarketData(symbolIds);
      const enhancedUpdates: EnhancedPriceData = {};

      marketData.forEach(data => {
        enhancedUpdates[data.symbolId] = data;
      });

      setEnhancedPrices(prev => ({ ...prev, ...enhancedUpdates }));
    } catch (error) {
      console.error('Failed to subscribe to symbols:', error);
    }
  }, []);

  const unsubscribeFromSymbols = useCallback(async (symbolIds: string[]) => {
    try {
      const targetSymbolKey = symbolIds.join(',');
      const subscription = subscriptions.find(s => s.symbolId === targetSymbolKey);

      if (subscription?.subscriptionId) {
        await websocketService.unsubscribe(subscription.subscriptionId);
        setSubscriptions(prev => prev.filter(s => s.subscriptionId !== subscription.subscriptionId));
      }
    } catch (error) {
      console.error('Failed to unsubscribe from symbols:', error);
    }
  }, [subscriptions]);

  const addTrackedSymbol = useCallback(async (symbol: EnhancedSymbolDto) => {
    if (!trackedSymbols.find(s => s.id === symbol.id)) {
      setTrackedSymbols(prev => [...prev, symbol]);
      await subscribeToSymbols([symbol.id], symbol.assetClassName as AssetClassType);
    }
  }, [trackedSymbols, subscribeToSymbols]);

  const removeTrackedSymbol = useCallback(async (symbolId: string) => {
    const symbol = trackedSymbols.find(s => s.id === symbolId);
    if (symbol) {
      setTrackedSymbols(prev => prev.filter(s => s.id !== symbolId));
      await unsubscribeFromSymbols([symbolId]);

      // Remove from enhanced prices
      setEnhancedPrices(prev => {
        const updated = { ...prev };
        delete updated[symbolId];
        return updated;
      });
    }
  }, [trackedSymbols, unsubscribeFromSymbols]);

  const getSymbolsByAssetClass = useCallback((assetClass: AssetClassType): EnhancedSymbolDto[] => {
    return trackedSymbols.filter(symbol => symbol.assetClassName === assetClass);
  }, [trackedSymbols]);

  const refreshPrices = useCallback(async () => {
    try {
      const symbolIds = trackedSymbols.map(s => s.id);
      if (symbolIds.length > 0) {
        const marketData = await multiAssetApiService.getBatchMarketData(symbolIds);
        const enhancedUpdates: EnhancedPriceData = {};

        marketData.forEach(data => {
          enhancedUpdates[data.symbolId] = data;
        });

        setEnhancedPrices(prev => ({ ...prev, ...enhancedUpdates }));
      }
    } catch (error) {
      console.error('Failed to refresh prices:', error);
    }
  }, [trackedSymbols]);

  const isSymbolTracked = useCallback((symbolId: string): boolean => {
    return trackedSymbols.some(s => s.id === symbolId);
  }, [trackedSymbols]);

  const getAssetClassSummary = useCallback(() => {
    const summary: Record<AssetClassType, {
      totalSymbols: number;
      gainers: number;
      losers: number;
      averageChange: number;
    }> = {} as any;

    const assetClasses: AssetClassType[] = ['CRYPTO', 'STOCK', 'FOREX', 'COMMODITY', 'INDEX'];

    assetClasses.forEach(assetClass => {
      const symbols = getSymbolsByAssetClass(assetClass);
      const prices = symbols.map(s => enhancedPrices[s.id]).filter(Boolean);

      const gainers = prices.filter(p => p.changePercent > 0).length;
      const losers = prices.filter(p => p.changePercent < 0).length;
      const averageChange = prices.length > 0
        ? prices.reduce((sum, p) => sum + p.changePercent, 0) / prices.length
        : 0;

      summary[assetClass] = {
        totalSymbols: symbols.length,
        gainers,
        losers,
        averageChange
      };
    });

    return summary;
  }, [getSymbolsByAssetClass, enhancedPrices]);

  // Legacy method (maintained for backward compatibility)
  const getPrice = (symbol: string): { price: number; change: number } | null => {
    // Try enhanced prices first
    const enhancedData = getPriceBySymbol(symbol);
    if (enhancedData) {
      return {
        price: enhancedData.price,
        change: enhancedData.changePercent,
      };
    }

    // Fallback to legacy prices
    const cleanSymbol = symbol.replace('USDT', '');
    const priceData = prices[cleanSymbol] || prices[symbol];

    if (priceData) {
      return {
        price: priceData.price,
        change: priceData.change,
      };
    }

    return null;
  };

  return (
    <EnhancedPriceContext.Provider value={{
      // Legacy properties
      prices,
      isConnected,
      symbols,
      getPrice,

      // Enhanced properties
      enhancedPrices,
      trackedSymbols,
      subscriptions,
      connectionStatus,
      getEnhancedPrice,
      getPriceBySymbol,
      subscribeToSymbols,
      unsubscribeFromSymbols,
      addTrackedSymbol,
      removeTrackedSymbol,
      getSymbolsByAssetClass,
      refreshPrices,
      isSymbolTracked,
      getAssetClassSummary,
    }}>
      {children}
    </EnhancedPriceContext.Provider>
  );
};

// Enhanced hooks for specific use cases
export const useEnhancedPricesOnly = () => {
  const { enhancedPrices, getEnhancedPrice, connectionStatus } = useEnhancedPrices();
  return { enhancedPrices, getEnhancedPrice, connectionStatus };
};

export const useSymbolPrice = (symbolId: string) => {
  const { getEnhancedPrice } = useEnhancedPrices();
  return getEnhancedPrice(symbolId);
};

export const useAssetClassPrices = (assetClass: AssetClassType) => {
  const { getSymbolsByAssetClass, enhancedPrices } = useEnhancedPrices();
  const symbols = getSymbolsByAssetClass(assetClass);
  const prices = symbols.map(symbol => enhancedPrices[symbol.id]).filter(Boolean);
  return { symbols, prices };
};

export const usePriceSubscription = () => {
  const { subscribeToSymbols, unsubscribeFromSymbols, subscriptions } = useEnhancedPrices();
  return { subscribeToSymbols, unsubscribeFromSymbols, subscriptions };
};

export const useTrackedSymbols = () => {
  const {
    trackedSymbols,
    addTrackedSymbol,
    removeTrackedSymbol,
    isSymbolTracked,
    getSymbolsByAssetClass
  } = useEnhancedPrices();
  return {
    trackedSymbols,
    addTrackedSymbol,
    removeTrackedSymbol,
    isSymbolTracked,
    getSymbolsByAssetClass
  };
};

export const useMarketSummary = () => {
  const { getAssetClassSummary } = useEnhancedPrices();
  return getAssetClassSummary();
};