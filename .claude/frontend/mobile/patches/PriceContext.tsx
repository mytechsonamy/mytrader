// PriceContext.tsx (drop-in replacement idea — adjust import paths accordingly)
import React, { createContext, useContext, useEffect, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

type PriceMap = Record<string, { price: number; change: number; ts: number }>;
type SymbolRow = { id: string; ticker: string; display: string };

const PriceContext = createContext<{ prices: PriceMap; isConnected: boolean; symbols: SymbolRow[] }>({ prices: {}, isConnected: false, symbols: [] });
export const usePrices = () => useContext(PriceContext);

export const PriceProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [prices, setPrices] = useState<PriceMap>({});
  const [symbols, setSymbols] = useState<SymbolRow[]>([]);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    (async () => {
      const baseUrl = process.env.EXPO_PUBLIC_API ?? 'http://localhost:5000';
      const token = await getAccessToken();

      // 1) tracked symbols
      const symRes = await fetch(`${baseUrl}/api/symbols/tracked`, { headers: { Authorization: `Bearer ${token}` }});
      const sym = await symRes.json();
      setSymbols(sym);

      // 2) snapshot
      const snapRes = await fetch(`${baseUrl}/api/prices/live`, { headers: { Authorization: `Bearer ${token}` }});
      const snap = await snapRes.json();
      setPrices(snap);

      // 3) SignalR
      const conn = new HubConnectionBuilder()
        .withUrl(`${baseUrl}/hub/trading`, {
          accessTokenFactory: async () => token
        })
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect()
        .build();

      conn.on('PriceUpdated', (p: { ticker: string; price: number; change: number; ts: number }) => {
        setPrices(prev => ({ ...prev, [p.ticker]: { price: p.price, change: p.change, ts: p.ts } }));
      });

      await conn.start();
      setIsConnected(true);

      return () => { conn.stop(); };
    })();
  }, []);

  return <PriceContext.Provider value={{ prices, isConnected, symbols }}>{children}</PriceContext.Provider>
}

// TODO: replace with your real auth
async function getAccessToken(): Promise<string> { return ''; }
