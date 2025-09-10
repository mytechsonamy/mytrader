import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import Constants from 'expo-constants';
import { API_BASE_URL as CFG_API_BASE_URL, WS_BASE_URL as CFG_WS_BASE_URL } from '../config';
import * as signalR from '@microsoft/signalr';
import AsyncStorage from '@react-native-async-storage/async-storage';

interface PriceData {
  [symbol: string]: {
    price: number;
    change: number;
    timestamp: string;
  };
}

type SymbolRow = { id: string; ticker: string; display: string; venue: string; baseCcy: string; quoteCcy: string; isTracked: boolean };

interface PriceContextType {
  prices: PriceData;
  isConnected: boolean;
  symbols: SymbolRow[];
  getPrice: (symbol: string) => { price: number; change: number } | null;
}

const PriceContext = createContext<PriceContextType>({
  prices: {},
  isConnected: false,
  symbols: [],
  getPrice: () => null,
});

export const usePrices = () => useContext(PriceContext);

interface PriceProviderProps {
  children: ReactNode;
}

export const PriceProvider: React.FC<PriceProviderProps> = ({ children }) => {
  const [prices, setPrices] = useState<PriceData>({});
  const [symbols, setSymbols] = useState<SymbolRow[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  const API_BASE_URL = Constants.expoConfig?.extra?.API_BASE_URL || CFG_API_BASE_URL || 'http://localhost:8080/api';
  // Prefer explicit WS_BASE_URL from config; fall back to API->hubs/trading
  const configuredHubUrl = (Constants.expoConfig?.extra?.WS_BASE_URL as string | undefined) || CFG_WS_BASE_URL;
  const rawHubUrl = configuredHubUrl || API_BASE_URL.replace('/api', '/hubs/trading');
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
        console.warn('Error fetching initial data:', error);
        // Fallback to mock data if needed
        const fallbackPrices: PriceData = {
          BTCUSDT: { price: 43250.67, change: 2.34, timestamp: new Date().toISOString() },
          ETHUSDT: { price: 2634.89, change: -1.23, timestamp: new Date().toISOString() },
        };
        setPrices(fallbackPrices);
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

        // Set up event handlers
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

        // Start connection
        await hubConnection.start();
        console.log('Connected to SignalR hub');
        setIsConnected(true);
        setConnection(hubConnection);

        // Set up SignalR event handlers for receiving messages from backend
        hubConnection.on('ReceivePriceUpdate', (data: any) => {
          try {
            console.log('Received SignalR price update:', data);
            
            // Handle the actual message format from .NET backend
            if (data.symbol && data.price) {
              const symbol = data.symbol.replace('USDT', ''); // Clean symbol (BTC, ETH, etc.)
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
            // Handle signal updates here if needed
          } catch (error) {
            console.warn('Error processing SignalR signal update:', error);
          }
        });

        hubConnection.on('ReceiveMarketData', (data: any) => {
          try {
            console.log('Received SignalR market data:', data);
            // Handle batch market data updates
            if (data.symbols) {
              const updates: PriceData = {};
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
        
        // Retry connection after 5 seconds
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

  const getPrice = (symbol: string): { price: number; change: number } | null => {
    // Remove USDT suffix if present to match our price data keys
    const cleanSymbol = symbol.replace('USDT', '');
    const priceData = prices[cleanSymbol];
    
    console.log(`Looking for price data for symbol: "${symbol}" -> cleaned: "${cleanSymbol}"`);
    console.log('Available price data:', Object.keys(prices));
    console.log(`Price data for ${cleanSymbol}:`, priceData);
    
    if (priceData) {
      return {
        price: priceData.price,
        change: priceData.change,
      };
    }
    
    return null;
  };

  return (
    <PriceContext.Provider value={{ prices, isConnected, symbols, getPrice }}>
      {children}
    </PriceContext.Provider>
  );
};
