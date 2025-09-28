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

// Combined price data for backward compatibility
type CombinedPriceData = LegacyPriceData & EnhancedPriceData;

interface PriceContextType {
  // Legacy properties (maintained for backward compatibility)
  prices: LegacyPriceData;
  isConnected: boolean;
  symbols: LegacySymbolRow[];
  getPrice: (symbol: string) => { price: number; change: number } | null;

  // Enhanced multi-asset properties
  enhancedPrices: EnhancedPriceData;
  trackedSymbols: EnhancedSymbolDto[];
  subscriptions: SymbolSubscription[];
  connectionStatus: 'disconnected' | 'connecting' | 'connected' | 'error';

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

const PriceContext = createContext<PriceContextType>({
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

export const usePrices = () => useContext(PriceContext);

interface PriceProviderProps {
  children: ReactNode;
}

export const PriceProvider: React.FC<PriceProviderProps> = ({ children }) => {
  // Legacy state (maintained for backward compatibility)
  const [prices, setPrices] = useState<LegacyPriceData>({});
  const [symbols, setSymbols] = useState<LegacySymbolRow[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  // Enhanced multi-asset state
  const [enhancedPrices, setEnhancedPrices] = useState<EnhancedPriceData>({});
  const [trackedSymbols, setTrackedSymbols] = useState<EnhancedSymbolDto[]>([]);
  const [subscriptions, setSubscriptions] = useState<SymbolSubscription[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<'disconnected' | 'connecting' | 'connected' | 'error'>('disconnected');

  const API_BASE_URL = Constants.expoConfig?.extra?.API_BASE_URL || CFG_API_BASE_URL || 'http://192.168.68.103:5002/api';
  // CRITICAL FIX: Use correct hub URL that matches backend (hubs/market-data)
  const configuredHubUrl = (Constants.expoConfig?.extra?.WS_BASE_URL as string | undefined) || CFG_WS_BASE_URL;
  const rawHubUrl = configuredHubUrl || API_BASE_URL.replace('/api', '/hubs/dashboard');
  // SignalR accepts http(s) or ws(s). Use http(s) so negotiation works consistently.
  const SIGNALR_HUB_URL = rawHubUrl.startsWith('wss://')
    ? rawHubUrl.replace('wss://', 'https://')
    : rawHubUrl.startsWith('ws://')
    ? rawHubUrl.replace('ws://', 'http://')
    : rawHubUrl;

  // Fetch tracked symbols and initial price data
  useEffect(() => {
    const initializeData = async () => {
      try {
        const token = await AsyncStorage.getItem('session_token');
        const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};
        
        // 1) Fetch tracked symbols
        console.log('Fetching tracked symbols from:', API_BASE_URL);
        const symbolsResponse = await fetch(`${API_BASE_URL}/v1/symbols/tracked`, { headers });
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
        console.warn('Error fetching initial data:', error);
        // Fallback to mock data if needed
        const fallbackPrices: LegacyPriceData = {
          BTCUSDT: { price: 43250.67, change: 2.34, timestamp: new Date().toISOString() },
          ETHUSDT: { price: 2634.89, change: -1.23, timestamp: new Date().toISOString() },
        };
        setPrices(fallbackPrices);

        // Also set some mock enhanced prices for testing
        const mockEnhancedPrices: EnhancedPriceData = {
          'btc-usdt': {
            symbolId: 'btc-usdt',
            symbol: 'BTCUSDT',
            price: 43250.67,
            change: 1012.34,
            changePercent: 2.34,
            timestamp: new Date().toISOString(),
            marketStatus: 'OPEN',
            dataSource: 'SIMULATED',
            lastUpdated: new Date().toISOString(),
          },
          'eth-usdt': {
            symbolId: 'eth-usdt',
            symbol: 'ETHUSDT',
            price: 2634.89,
            change: -32.45,
            changePercent: -1.23,
            timestamp: new Date().toISOString(),
            marketStatus: 'OPEN',
            dataSource: 'SIMULATED',
            lastUpdated: new Date().toISOString(),
          },
        };
        setEnhancedPrices(mockEnhancedPrices);

        // Set mock tracked symbols
        const mockTrackedSymbols: EnhancedSymbolDto[] = [
          {
            id: 'btc-usdt',
            symbol: 'BTCUSDT',
            displayName: 'Bitcoin',
            assetClassId: 'CRYPTO',
            marketId: 'crypto-market',
            baseCurrency: 'BTC',
            quoteCurrency: 'USDT',
            isActive: true,
            isTracked: true,
            priceDecimalPlaces: 2,
            quantityDecimalPlaces: 8,
            createdAt: new Date().toISOString(),
          },
          {
            id: 'eth-usdt',
            symbol: 'ETHUSDT',
            displayName: 'Ethereum',
            assetClassId: 'CRYPTO',
            marketId: 'crypto-market',
            baseCurrency: 'ETH',
            quoteCurrency: 'USDT',
            isActive: true,
            isTracked: true,
            priceDecimalPlaces: 2,
            quantityDecimalPlaces: 8,
            createdAt: new Date().toISOString(),
          },
        ];
        setTrackedSymbols(mockTrackedSymbols);
      }
    };

    initializeData();
  }, [API_BASE_URL]);

  useEffect(() => {
    let hubConnection: signalR.HubConnection | null = null;
    let reconnectTimer: NodeJS.Timeout | null = null;

    const connect = async () => {
      try {
        // Get auth token if available
        const token = await AsyncStorage.getItem('session_token');
        
        console.log('Connecting to SignalR hub:', SIGNALR_HUB_URL);
        
        // Create SignalR connection (attach token when present)
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

        // Debug: Log ALL incoming events - BEFORE connection starts
        const originalOn = hubConnection.on.bind(hubConnection);
        hubConnection.on = (methodName: string, callback: (...args: any[]) => void) => {
          return originalOn(methodName, (...args: any[]) => {
            console.log(`[SignalR] Event received: ${methodName}`, args);
            return callback(...args);
          });
        };

        // Set up ALL event handlers BEFORE starting connection
        hubConnection.on('PriceUpdate', (data: any) => {
          console.log('Received price update:', data);
          if (data && data.Symbol && data.Price) {
            const symbol = data.Symbol.replace('USDT', ''); // Convert BTCUSDT to BTC
            setPrices(prev => ({
              ...prev,
              [symbol]: {
                price: data.Price,
                change: data.PriceChange || 0,
                timestamp: data.Timestamp || new Date().toISOString()
              }
            }));
          }
        });

        hubConnection.on('MarketDataUpdate', (data: any) => {
          console.log('Received market data update:', data);
          if (data && data.Symbol && data.Price) {
            const symbol = data.Symbol.replace('USDT', '');
            setPrices(prev => ({
              ...prev,
              [symbol]: {
                price: data.Price,
                change: data.PriceChange || 0,
                timestamp: data.Timestamp || new Date().toISOString()
              }
            }));
          }
        });

        // Handle dashboard snapshot
        hubConnection.on('DashboardSnapshot', (data: any) => {
          console.log('Received dashboard snapshot:', data);
        });

        // Legacy events for backward compatibility
        hubConnection.on('ReceivePriceUpdate', (data: any) => {
          console.log('Received legacy price update:', data);
          if (data && data.symbol && data.price) {
            const symbol = data.symbol.replace('USDT', '');
            setPrices(prev => ({
              ...prev,
              [symbol]: {
                price: data.price,
                change: data.priceChange || 0,
                timestamp: data.timestamp || new Date().toISOString()
              }
            }));
          }
        });

        // Set up event handlers
        hubConnection.onclose((error) => {
          console.log('SignalR connection closed:', error?.message || 'No error');
          setIsConnected(false);
          setConnectionStatus('disconnected');
        });

        hubConnection.onreconnecting(() => {
          console.log('SignalR reconnecting...');
          setIsConnected(false);
          setConnectionStatus('connecting');
        });

        hubConnection.onreconnected(() => {
          console.log('SignalR reconnected');
          setIsConnected(true);
          setConnectionStatus('connected');

          // Reset retry counter on successful reconnection
          (window as any).__signalRRetryCount = 0;
        });

        // Start connection
        await hubConnection.start();
        console.log('Connected to SignalR hub');
        setIsConnected(true);
        setConnectionStatus('connected');
        setConnection(hubConnection);

        // Reset retry counter on successful connection
        (window as any).__signalRRetryCount = 0;


        // Set up price update event handlers
        hubConnection.on('PriceUpdate', (data: any) => {
          console.log('Received price update:', data);
          if (data && data.Symbol && data.Price) {
            const symbol = data.Symbol.replace('USDT', ''); // Convert BTCUSDT to BTC
            setPrices(prev => ({
              ...prev,
              [symbol]: {
                price: data.Price,
                change: data.PriceChange || 0,
                timestamp: data.Timestamp || new Date().toISOString()
              }
            }));
          }
        });

        hubConnection.on('MarketDataUpdate', (data: any) => {
          console.log('Received market data update:', data);
          if (data && data.Symbol && data.Price) {
            const symbol = data.Symbol.replace('USDT', '');
            setPrices(prev => ({
              ...prev,
              [symbol]: {
                price: data.Price,
                change: data.PriceChange || 0,
                timestamp: data.Timestamp || new Date().toISOString()
              }
            }));
          }
        });

        // Handle dashboard snapshot
        hubConnection.on('DashboardSnapshot', (data: any) => {
          console.log('Received dashboard snapshot:', data);
        });

        // No manual subscription needed - users are automatically added to AssetClass groups on connection

        // Set up SignalR event handlers for receiving messages from backend
        // Handle both ReceivePriceUpdate and PriceUpdate events for backend compatibility
        hubConnection.on('ReceivePriceUpdate', (data: any) => {
          try {
            // Only log in development and throttle the logging
            if (__DEV__ && Math.random() < 0.1) { // Log only 10% of updates in dev
              console.log('Received SignalR price update:', data);
            }

            // Handle the actual message format from .NET backend
            if (data.symbol && data.price) {
              const symbol = data.symbol.replace('USDT', ''); // Clean symbol (BTC, ETH, etc.)

              // Only log significant price changes (>1%) to reduce noise
              const previousPrice = prices[symbol]?.price;
              const priceChangePercent = previousPrice ? Math.abs((data.price - previousPrice) / previousPrice * 100) : 0;

              if (__DEV__ && priceChangePercent > 1) {
                console.log(`Significant price change for ${symbol}: $${data.price} (${priceChangePercent.toFixed(2)}%)`);
              }

              // Update legacy prices with memoized state update
              setPrices(prev => {
                const currentPrice = prev[symbol];
                if (currentPrice && Math.abs(currentPrice.price - data.price) < 0.01) {
                  return prev; // Skip update if price difference is negligible
                }
                return {
                  ...prev,
                  [symbol]: {
                    price: data.price,
                    change: data.change || 0,
                    timestamp: data.timestamp || new Date().toISOString(),
                  }
                };
              });

              // Update enhanced prices with memoized state update
              const symbolId = data.symbol.toLowerCase();
              setEnhancedPrices(prev => {
                const currentPrice = prev[symbolId];
                if (currentPrice && Math.abs(currentPrice.price - data.price) < 0.01) {
                  return prev; // Skip update if price difference is negligible
                }
                return {
                  ...prev,
                  [symbolId]: {
                    symbolId,
                    symbol: data.symbol,
                    price: data.price,
                    change: data.change || 0,
                    changePercent: data.changePercent || 0,
                    timestamp: data.timestamp || new Date().toISOString(),
                    marketStatus: 'OPEN',
                    dataSource: 'REAL_TIME',
                    lastUpdated: new Date().toISOString(),
                  }
                };
              });
            }
          } catch (error) {
            console.warn('Error processing SignalR price update:', error);
          }
        });

        // CRITICAL FIX: Also listen for PriceUpdate (without Receive prefix) for backend compatibility
        hubConnection.on('PriceUpdate', (data: any) => {
          try {
            // Only log in development and throttle the logging
            if (__DEV__ && Math.random() < 0.1) { // Log only 10% of updates in dev
              console.log('Received SignalR PriceUpdate:', data);
            }

            // Handle the actual message format from .NET backend
            if (data.symbol && data.price) {
              const symbol = data.symbol.replace('USD', ''); // Clean symbol (BTC, ETH, etc.)

              // Update legacy prices with memoized state update
              setPrices(prev => {
                const currentPrice = prev[symbol];
                if (currentPrice && Math.abs(currentPrice.price - data.price) < 0.01) {
                  return prev; // Skip update if price difference is negligible
                }
                return {
                  ...prev,
                  [symbol]: {
                    price: data.price,
                    change: data.change || 0,
                    timestamp: data.timestamp || new Date().toISOString(),
                  }
                };
              });

              // Update enhanced prices with memoized state update
              const symbolId = data.symbol.toLowerCase();
              setEnhancedPrices(prev => {
                const currentPrice = prev[symbolId];
                if (currentPrice && Math.abs(currentPrice.price - data.price) < 0.01) {
                  return prev; // Skip update if price difference is negligible
                }
                return {
                  ...prev,
                  [symbolId]: {
                    symbolId,
                    symbol: data.symbol,
                    price: data.price,
                    change: data.change || 0,
                    changePercent: data.changePercent || 0,
                    timestamp: data.timestamp || new Date().toISOString(),
                    marketStatus: 'OPEN',
                    dataSource: 'REAL_TIME',
                    lastUpdated: new Date().toISOString(),
                  }
                };
              });
            }
          } catch (error) {
            console.warn('Error processing SignalR PriceUpdate:', error);
          }
        });

        hubConnection.on('ReceiveSignalUpdate', (data: any) => {
          try {
            // Only log in development and rarely to reduce noise
            if (__DEV__ && Math.random() < 0.05) { // Log only 5% of signal updates
              console.log('Received SignalR signal update:', data);
            }
            // Handle signal updates here if needed
          } catch (error) {
            console.warn('Error processing SignalR signal update:', error);
          }
        });

        hubConnection.on('ReceiveMarketData', (data: any) => {
          try {
            // Only log batch updates occasionally to reduce noise
            if (__DEV__ && Math.random() < 0.02) { // Log only 2% of batch updates
              console.log('Received SignalR market data batch:', Object.keys(data.symbols || {}).length, 'symbols');
            }

            // Handle batch market data updates with optimization
            if (data.symbols) {
              const updates: LegacyPriceData = {};
              const enhancedUpdates: EnhancedPriceData = {};
              let hasSignificantChanges = false;

              Object.keys(data.symbols).forEach(symbol => {
                const symbolData = data.symbols[symbol];
                if (symbolData.price) {
                  const cleanSymbol = symbol.replace('USDT', '');

                  // Check if this is a significant price change
                  const previousPrice = prices[cleanSymbol]?.price;
                  const priceChange = previousPrice ? Math.abs((symbolData.price - previousPrice) / previousPrice * 100) : 0;

                  if (!previousPrice || priceChange > 0.01) { // Only update if price changed by more than 0.01%
                    hasSignificantChanges = true;

                    // Legacy format
                    updates[cleanSymbol] = {
                      price: symbolData.price,
                      change: symbolData.change || 0,
                      timestamp: symbolData.timestamp || new Date().toISOString(),
                    };

                    // Enhanced format
                    const symbolId = symbol.toLowerCase();
                    enhancedUpdates[symbolId] = {
                      symbolId,
                      symbol,
                      price: symbolData.price,
                      change: symbolData.change || 0,
                      changePercent: symbolData.changePercent || 0,
                      timestamp: symbolData.timestamp || new Date().toISOString(),
                      marketStatus: 'OPEN',
                      dataSource: 'REAL_TIME',
                      lastUpdated: new Date().toISOString(),
                    };
                  }
                }
              });

              // Only update state if there are significant changes
              if (hasSignificantChanges) {
                if (Object.keys(updates).length > 0) {
                  setPrices(prev => ({ ...prev, ...updates }));
                }
                if (Object.keys(enhancedUpdates).length > 0) {
                  setEnhancedPrices(prev => ({ ...prev, ...enhancedUpdates }));
                }
              }
            }
          } catch (error) {
            console.warn('Error processing SignalR market data:', error);
          }
        });
      } catch (error) {
        console.warn('Failed to create SignalR connection:', error);
        setIsConnected(false);
        setConnectionStatus('error');

        // Implement exponential backoff for retries to prevent spam
        const retryDelay = Math.min(5000 * Math.pow(2, (window as any).__signalRRetryCount || 0), 60000);
        (window as any).__signalRRetryCount = ((window as any).__signalRRetryCount || 0) + 1;

        if (!reconnectTimer && (window as any).__signalRRetryCount < 10) {
          console.log(`Will retry SignalR connection in ${retryDelay/1000}s (attempt ${(window as any).__signalRRetryCount})`);
          reconnectTimer = setTimeout(() => {
            reconnectTimer = null;
            connect();
          }, retryDelay);
        } else if ((window as any).__signalRRetryCount >= 10) {
          console.warn('Maximum SignalR retry attempts reached. Connection will not be retried automatically.');
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

  const getPrice = useCallback((symbol: string): { price: number; change: number } | null => {
    // Remove USDT suffix if present to match our price data keys
    const cleanSymbol = symbol.replace('USDT', '');
    const priceData = prices[cleanSymbol];

    // Only log in development and occasionally to reduce noise
    if (__DEV__ && !priceData && Math.random() < 0.1) {
      console.log(`Price not found for symbol: "${symbol}" -> cleaned: "${cleanSymbol}"`);
      console.log('Available symbols:', Object.keys(prices).slice(0, 5), '...');
    }

    if (priceData) {
      return {
        price: priceData.price,
        change: priceData.change,
      };
    }

    return null;
  }, [prices]);

  // Enhanced methods implementation
  const getEnhancedPrice = useCallback((symbolId: string): UnifiedMarketDataDto | null => {
    return enhancedPrices[symbolId] || null;
  }, [enhancedPrices]);

  const getPriceBySymbol = useCallback((symbol: string, assetClass?: AssetClassType): UnifiedMarketDataDto | null => {
    // First try direct symbol lookup
    const direct = Object.values(enhancedPrices).find(price => price.symbol === symbol);
    if (direct) return direct;

    // If asset class is provided, filter by it
    if (assetClass) {
      const filtered = trackedSymbols
        .filter(s => s.assetClassId === assetClass && s.symbol === symbol)
        .map(s => enhancedPrices[s.id])
        .find(Boolean);
      if (filtered) return filtered;
    }

    return null;
  }, [enhancedPrices, trackedSymbols]);

  const subscribeToSymbols = useCallback(async (symbolIds: string[], assetClass?: AssetClassType): Promise<void> => {
    // Implementation for subscribing to symbols
    if (__DEV__) {
      console.log('Subscribing to symbols:', symbolIds.length, 'symbols', assetClass ? `in ${assetClass}` : '');
    }
    // This would connect to enhanced WebSocket service
  }, []);

  const unsubscribeFromSymbols = useCallback(async (symbolIds: string[]): Promise<void> => {
    if (__DEV__) {
      console.log('Unsubscribing from', symbolIds.length, 'symbols');
    }
    // Implementation for unsubscribing
  }, []);

  const addTrackedSymbol = useCallback(async (symbol: EnhancedSymbolDto): Promise<void> => {
    setTrackedSymbols(prev => {
      const exists = prev.find(s => s.id === symbol.id);
      if (exists) return prev;
      return [...prev, symbol];
    });
  }, []);

  const removeTrackedSymbol = useCallback(async (symbolId: string): Promise<void> => {
    setTrackedSymbols(prev => prev.filter(s => s.id !== symbolId));
  }, []);

  const getSymbolsByAssetClass = useCallback((assetClass: AssetClassType): EnhancedSymbolDto[] => {
    return trackedSymbols.filter(symbol => symbol.assetClassId === assetClass);
  }, [trackedSymbols]);

  const refreshPrices = useCallback(async (): Promise<void> => {
    if (__DEV__) {
      console.log('Refreshing prices...');
    }
    // Implementation for refreshing prices
  }, []);

  const isSymbolTracked = useCallback((symbolId: string): boolean => {
    return trackedSymbols.some(symbol => symbol.id === symbolId);
  }, [trackedSymbols]);

  const getAssetClassSummary = useCallback(() => {
    const summary: Record<AssetClassType, {
      totalSymbols: number;
      gainers: number;
      losers: number;
      averageChange: number;
    }> = {
      CRYPTO: { totalSymbols: 0, gainers: 0, losers: 0, averageChange: 0 },
      STOCK: { totalSymbols: 0, gainers: 0, losers: 0, averageChange: 0 },
      FOREX: { totalSymbols: 0, gainers: 0, losers: 0, averageChange: 0 },
      COMMODITY: { totalSymbols: 0, gainers: 0, losers: 0, averageChange: 0 },
      INDEX: { totalSymbols: 0, gainers: 0, losers: 0, averageChange: 0 },
    };

    // Group tracked symbols by asset class
    const symbolsByClass = trackedSymbols.reduce((acc, symbol) => {
      const assetClass = symbol.assetClassId as AssetClassType;
      if (!acc[assetClass]) acc[assetClass] = [];
      acc[assetClass].push(symbol);
      return acc;
    }, {} as Record<AssetClassType, EnhancedSymbolDto[]>);

    // Calculate summary for each asset class
    Object.entries(symbolsByClass).forEach(([assetClass, symbols]) => {
      const ac = assetClass as AssetClassType;
      const prices = symbols.map(s => enhancedPrices[s.id]).filter(Boolean);

      summary[ac].totalSymbols = symbols.length;
      summary[ac].gainers = prices.filter(p => p.changePercent > 0).length;
      summary[ac].losers = prices.filter(p => p.changePercent < 0).length;

      if (prices.length > 0) {
        const totalChange = prices.reduce((sum, p) => sum + p.changePercent, 0);
        summary[ac].averageChange = totalChange / prices.length;
      }
    });

    return summary;
  }, [trackedSymbols, enhancedPrices]);

  return (
    <PriceContext.Provider value={{
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

      // Enhanced methods
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
    </PriceContext.Provider>
  );
};
