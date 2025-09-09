import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import Constants from 'expo-constants';
import * as signalR from '@microsoft/signalr';
import AsyncStorage from '@react-native-async-storage/async-storage';

interface PriceData {
  [symbol: string]: {
    price: number;
    change: number;
    timestamp: string;
  };
}

interface PriceContextType {
  prices: PriceData;
  isConnected: boolean;
  getPrice: (symbol: string) => { price: number; change: number } | null;
}

const PriceContext = createContext<PriceContextType>({
  prices: {},
  isConnected: false,
  getPrice: () => null,
});

export const usePrices = () => useContext(PriceContext);

interface PriceProviderProps {
  children: ReactNode;
}

export const PriceProvider: React.FC<PriceProviderProps> = ({ children }) => {
  const [prices, setPrices] = useState<PriceData>({});
  const [isConnected, setIsConnected] = useState(false);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  const API_BASE_URL = Constants.expoConfig?.extra?.API_BASE_URL || 'http://localhost:8080/api';
  const SIGNALR_HUB_URL = API_BASE_URL.replace('/api', '/hub');

  useEffect(() => {
    let hubConnection: signalR.HubConnection | null = null;
    let reconnectTimer: NodeJS.Timeout | null = null;

    const connect = async () => {
      try {
        // Get auth token (temporarily disabled for debugging)
        // const token = await AsyncStorage.getItem('token');
        
        console.log('Connecting to SignalR hub:', SIGNALR_HUB_URL);
        
        // Create SignalR connection (without auth for now)
        hubConnection = new signalR.HubConnectionBuilder()
          .withUrl(SIGNALR_HUB_URL, {
            // accessTokenFactory: () => token || '',
            skipNegotiation: false,
            transport: signalR.HttpTransportType.WebSockets,
          })
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
    <PriceContext.Provider value={{ prices, isConnected, getPrice }}>
      {children}
    </PriceContext.Provider>
  );
};