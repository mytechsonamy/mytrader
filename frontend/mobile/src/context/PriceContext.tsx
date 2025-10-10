import React, { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';
import Constants from 'expo-constants';
import { API_BASE_URL as CFG_API_BASE_URL, WS_BASE_URL as CFG_WS_BASE_URL } from '../config';
import * as signalR from '@microsoft/signalr';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { websocketService } from '../services/websocketService';
import { apiService } from '../services/api';
import { multiAssetApiService } from '../services/multiAssetApi';
import { UnifiedMarketDataDto, EnhancedSymbolDto, AssetClassType } from '../types';
import { normalizeMarketData } from '../utils/priceFormatting';

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

interface PriceContextType {


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

const PriceContext = createContext<PriceContextType>({


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

        // Initialize API service first
        await multiAssetApiService.initialize();

        // Set up WebSocket event listeners BEFORE connecting
        websocketService.onConnectionStatus((status) => {
          console.log('[PriceContext] Connection status changed:', status);
          setConnectionStatus(status);
        });

                websocketService.on('price_update', (data: any) => {
                  try {
                    // Extract previous close with case-insensitive handling
                    const rawPreviousClose = data.previousClose ?? data.PreviousClose ?? data.prevClose ?? data.PrevClose;

                    // Log raw data for debugging - show all field names to diagnose case sensitivity
                    if (__DEV__) {
                      console.log('[PriceContext] ======= RECEIVED FROM WEBSOCKET SERVICE =======');
                      console.log('[PriceContext] RAW price_update - All fields:', Object.keys(data));
                      console.log('[PriceContext] RAW price_update FULL OBJECT:', JSON.stringify(data, null, 2));
                      console.log('[PriceContext] Extracted values:', {
                        symbol: data.symbol || data.Symbol,
                        assetClass: data.assetClass || data.AssetClass,
                        price: data.price || data.Price,
                        previousClose: rawPreviousClose,
                        'data.previousClose': data.previousClose,
                        'data.PreviousClose': data.PreviousClose,
                        volume: data.volume || data.Volume,
                        change: data.change || data.Change24h
                      });

                      // CRITICAL: Log if previousClose is missing for stocks
                      if ((data.assetClass === 'STOCK' || data.AssetClass === 'STOCK') && rawPreviousClose === undefined) {
                        console.error('[PriceContext] ❌ STOCK WITHOUT previousClose:', data.symbol || data.Symbol);
                        console.error('[PriceContext] Available fields:', Object.keys(data));
                        console.error('[PriceContext] Field check:', {
                          hasPreviousClose: 'previousClose' in data,
                          hasPreviousCloseUpper: 'PreviousClose' in data,
                          hasPrevClose: 'prevClose' in data,
                          hasPrevCloseUpper: 'PrevClose' in data
                        });
                      } else if ((data.assetClass === 'STOCK' || data.AssetClass === 'STOCK')) {
                        console.log('[PriceContext] ✅ STOCK WITH previousClose:', data.symbol || data.Symbol, '=', rawPreviousClose);
                      }
                      console.log('[PriceContext] ================================================');
                    }

                    // Handle both uppercase and lowercase field names from backend
                    const rawSymbol = data.symbol || data.Symbol;
                    const rawAssetClass = data.assetClass || data.AssetClass || 'CRYPTO';
                    const rawPrice = data.price || data.Price;
                    const rawVolume = data.volume || data.Volume;
                    const rawChange = data.change || data.change24h || data.Change24h;

                    // Normalize price data using the utility function
                    const priceNormalized = normalizeMarketData({
                      price: rawPrice,
                      bid: data.bid || data.Bid,
                      ask: data.ask || data.Ask,
                      open: data.open || data.Open,
                      high: data.high || data.High,
                      low: data.low || data.Low,
                      close: data.close || data.Close,
                      previousClose: rawPreviousClose,
                      volume: rawVolume,
                      change: rawChange,
                      changePercent: data.changePercent || data.ChangePercent
                    }, rawAssetClass);

                    // Parse and normalize the incoming data
                    const normalizedData: UnifiedMarketDataDto = {
                      symbolId: data.symbolId || data.id || rawSymbol,
                      symbol: rawSymbol,
                      displayName: data.displayName || data.name || rawSymbol,
                      assetClass: rawAssetClass as AssetClassType,
                      market: data.market || data.Market || data.source || data.Source || 'UNKNOWN',
                      price: priceNormalized.price,
                      previousClose: priceNormalized.previousClose,
                      bid: priceNormalized.bid,
                      ask: priceNormalized.ask,
                      open: priceNormalized.open,
                      high: priceNormalized.high,
                      low: priceNormalized.low,
                      close: priceNormalized.close,
                      volume: priceNormalized.volume,
                      quoteVolume: data.quoteVolume ? Number(data.quoteVolume) : undefined,
                      change: priceNormalized.change,
                      changePercent: priceNormalized.changePercent,
                      timestamp: data.timestamp || data.Timestamp || new Date().toISOString(),
                      marketStatus: data.marketStatus || data.MarketStatus || 'OPEN',
                      highLow52Week: data.highLow52Week,
                      averageVolume: data.averageVolume ? Number(data.averageVolume) : undefined,
                      marketCap: data.marketCap ? Number(data.marketCap) : undefined,
                      peRatio: data.peRatio ? Number(data.peRatio) : undefined,
                      dividendYield: data.dividendYield ? Number(data.dividendYield) : undefined,
                      beta: data.beta ? Number(data.beta) : undefined,
                      earningsDate: data.earningsDate,
                      // Alpaca integration fields (optional, backward compatible)
                      source: data.source as 'ALPACA' | 'YAHOO_FALLBACK' | 'YAHOO_REALTIME' | undefined,
                      qualityScore: data.qualityScore ? Number(data.qualityScore) : undefined,
                      isRealtime: data.isRealtime !== undefined ? Boolean(data.isRealtime) : undefined,
                    };

                    if (__DEV__) {
                      console.log('[PriceContext] Normalized price_update:', {
                        symbolId: normalizedData.symbolId,
                        symbol: normalizedData.symbol,
                        assetClass: normalizedData.assetClass,
                        price: normalizedData.price,
                        previousClose: normalizedData.previousClose,
                        change: normalizedData.change,
                        changePercent: normalizedData.changePercent,
                        volume: normalizedData.volume
                      });

                      // Note: previousClose may be null when markets are closed or Yahoo Finance API is rate-limited
                      // UI components handle this gracefully by hiding the "Önceki Kapanış" field
                    }

                    setEnhancedPrices(prev => {
                      const updated = {
                        ...prev,
                        [normalizedData.symbolId]: normalizedData
                      };

                      // Also index by symbol for lookup flexibility
                      if (normalizedData.symbol) {
                        updated[normalizedData.symbol] = normalizedData;
                      }

                      if (__DEV__ && normalizedData.assetClass === 'STOCK') {
                        console.log(`[PriceContext] ✅ Stock price updated: ${normalizedData.symbol} = $${normalizedData.price}`);
                      }

                      return updated;
                    });
                  } catch (error) {
                    console.error('[PriceContext] Failed to process price_update:', error, data);
                  }
                });

        websocketService.on('batch_price_update', (dataArray: any[]) => {
          try {
            const enhancedUpdates: EnhancedPriceData = {};

            dataArray.forEach(data => {
              // Extract previous close with case-insensitive handling
              const rawPreviousClose = data.previousClose ?? data.PreviousClose ?? data.prevClose ?? data.PrevClose;

              // Handle both uppercase and lowercase field names from backend
              const rawSymbol = data.symbol || data.Symbol;
              const rawAssetClass = data.assetClass || data.AssetClass || 'CRYPTO';
              const rawPrice = data.price || data.Price;
              const rawVolume = data.volume || data.Volume;

              // Normalize price data using the utility function
              const priceNormalized = normalizeMarketData({
                price: rawPrice,
                bid: data.bid || data.Bid,
                ask: data.ask || data.Ask,
                open: data.open || data.Open,
                high: data.high || data.High,
                low: data.low || data.Low,
                close: data.close || data.Close,
                previousClose: rawPreviousClose,
                volume: rawVolume,
                change: data.change || data.change24h || data.Change24h,
                changePercent: data.changePercent || data.ChangePercent
              }, rawAssetClass);

              const normalizedData: UnifiedMarketDataDto = {
                symbolId: data.symbolId || data.id || rawSymbol,
                symbol: rawSymbol,
                displayName: data.displayName || data.name || rawSymbol,
                assetClass: rawAssetClass as AssetClassType,
                market: data.market || data.Market || data.source || data.Source || 'UNKNOWN',
                price: priceNormalized.price,
                previousClose: priceNormalized.previousClose,
                bid: priceNormalized.bid,
                ask: priceNormalized.ask,
                open: priceNormalized.open,
                high: priceNormalized.high,
                low: priceNormalized.low,
                close: priceNormalized.close,
                volume: priceNormalized.volume,
                quoteVolume: data.quoteVolume ? Number(data.quoteVolume) : undefined,
                change: priceNormalized.change,
                changePercent: priceNormalized.changePercent,
                timestamp: data.timestamp || data.Timestamp || new Date().toISOString(),
                marketStatus: data.marketStatus || data.MarketStatus || 'OPEN',
                highLow52Week: data.highLow52Week,
                averageVolume: data.averageVolume ? Number(data.averageVolume) : undefined,
                marketCap: data.marketCap ? Number(data.marketCap) : undefined,
                peRatio: data.peRatio ? Number(data.peRatio) : undefined,
                dividendYield: data.dividendYield ? Number(data.dividendYield) : undefined,
                beta: data.beta ? Number(data.beta) : undefined,
                earningsDate: data.earningsDate,
                // Alpaca integration fields (optional, backward compatible)
                source: data.source as 'ALPACA' | 'YAHOO_FALLBACK' | 'YAHOO_REALTIME' | undefined,
                qualityScore: data.qualityScore ? Number(data.qualityScore) : undefined,
                isRealtime: data.isRealtime !== undefined ? Boolean(data.isRealtime) : undefined,
              };

              enhancedUpdates[normalizedData.symbolId] = normalizedData;

              // Also index by symbol for lookup flexibility
              if (normalizedData.symbol) {
                enhancedUpdates[normalizedData.symbol] = normalizedData;
              }
            });

            console.log('[PriceContext] Processed batch_price_update:', Object.keys(enhancedUpdates).length, 'items');
            if (__DEV__) {
              const stockUpdates = Object.values(enhancedUpdates).filter(d => d.assetClass === 'STOCK');
              if (stockUpdates.length > 0) {
                console.log(`[PriceContext] ✅ Batch included ${stockUpdates.length} stock updates`);
              }
            }
            setEnhancedPrices(prev => ({ ...prev, ...enhancedUpdates }));
          } catch (error) {
            console.error('[PriceContext] Failed to process batch_price_update:', error);
          }
        });

        // NOW initialize WebSocket connection (after event listeners are set up)
        console.log('[PriceContext] Initializing WebSocket connection...');
        await websocketService.initialize();
        console.log('[PriceContext] Enhanced price service initialized');
      } catch (error) {
        console.error('[PriceContext] Failed to initialize enhanced price service:', error);
        console.error('[PriceContext] Error details:', error);
        setConnectionStatus('error');

        // Don't throw - allow app to continue without real-time updates
      }
    };

    initializeEnhancedService();
  }, []);

  useEffect(() => {
    const fetchAndSubscribe = async () => {
      try {
        console.log('[PriceContext] Fetching symbols and subscribing...');

        // Fetch CRYPTO symbols
        const cryptoSymbols = await apiService.getSymbolsByAssetClass('CRYPTO');
        console.log(`[PriceContext] Loaded ${cryptoSymbols.length} crypto symbols:`, cryptoSymbols.map(s => s.symbol).join(', '));

        // Fetch STOCK symbols
        const stockSymbols = await apiService.getSymbolsByAssetClass('STOCK');
        console.log(`[PriceContext] Loaded ${stockSymbols.length} stock symbols:`, stockSymbols.map(s => s.symbol).join(', '));

        // Combine all symbols into trackedSymbols
        const allSymbols = [...cryptoSymbols, ...stockSymbols];
        setTrackedSymbols(allSymbols);

        // Subscribe to CRYPTO price updates
        const cryptoSymbolNames = cryptoSymbols.map(s => s.symbol);
        if (cryptoSymbolNames.length > 0) {
          try {
            console.log('[PriceContext] Subscribing to CRYPTO symbols:', cryptoSymbolNames);
            await websocketService.subscribeToAssetClass('CRYPTO', cryptoSymbolNames);
            console.log('[PriceContext] Successfully subscribed to CRYPTO price updates');
          } catch (subError) {
            console.error('[PriceContext] Failed to subscribe to CRYPTO:', subError);
          }
        }

        // Subscribe to STOCK price updates
        const stockSymbolNames = stockSymbols.map(s => s.symbol);
        if (stockSymbolNames.length > 0) {
          try {
            console.log('[PriceContext] Subscribing to STOCK symbols:', stockSymbolNames);
            await websocketService.subscribeToAssetClass('STOCK', stockSymbolNames);
            console.log('[PriceContext] Successfully subscribed to STOCK price updates');
          } catch (subError) {
            console.error('[PriceContext] Failed to subscribe to STOCK:', subError);
          }
        }
      } catch (error) {
        console.error('Failed to fetch and subscribe to symbols:', error);
      }
    };

    if (connectionStatus === 'connected') {
      fetchAndSubscribe();
    }
  }, [connectionStatus]);

  // Debug: Log enhancedPrices state changes
  useEffect(() => {
    const symbolIds = Object.keys(enhancedPrices);
    console.log(`[PriceContext] enhancedPrices state updated: ${symbolIds.length} items`);
    if (symbolIds.length > 0) {
      console.log('[PriceContext] Sample prices:', symbolIds.slice(0, 3).map(id => ({
        id,
        symbol: enhancedPrices[id]?.symbol,
        price: enhancedPrices[id]?.price
      })));
    }
  }, [enhancedPrices]);



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



  return (
    <PriceContext.Provider value={{


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
    </PriceContext.Provider>
  );
};

// Enhanced hooks for specific use cases
export const usePricesOnly = () => {
  const { enhancedPrices, getEnhancedPrice, connectionStatus } = usePrices();
  return { enhancedPrices, getEnhancedPrice, connectionStatus };
};

export const useSymbolPrice = (symbolId: string) => {
  const { getEnhancedPrice } = usePrices();
  return getEnhancedPrice(symbolId);
};

export const useAssetClassPrices = (assetClass: AssetClassType) => {
  const { getSymbolsByAssetClass, enhancedPrices } = usePrices();
  const symbols = getSymbolsByAssetClass(assetClass);
  const prices = symbols.map(symbol => enhancedPrices[symbol.id]).filter(Boolean);
  return { symbols, prices };
};

export const usePriceSubscription = () => {
  const { subscribeToSymbols, unsubscribeFromSymbols, subscriptions } = usePrices();
  return { subscribeToSymbols, unsubscribeFromSymbols, subscriptions };
};

export const useTrackedSymbols = () => {
  const {
    trackedSymbols,
    addTrackedSymbol,
    removeTrackedSymbol,
    isSymbolTracked,
    getSymbolsByAssetClass
  } = usePrices();
  return {
    trackedSymbols,
    addTrackedSymbol,
    removeTrackedSymbol,
    isSymbolTracked,
    getSymbolsByAssetClass
  };
};

export const useMarketSummary = () => {
  const { getAssetClassSummary } = usePrices();
  return getAssetClassSummary();
};