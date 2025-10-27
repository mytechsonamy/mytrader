/**
 * Strategy Test page - Test trading strategies on specific symbols
 */

import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent, Button, Badge } from '../components/ui';
import AuthenticatedLayout from '../components/layout/AuthenticatedLayout';
import { useMarketOverview } from '../hooks/useMarketData';
import { useWebSocketPrices } from '../context/WebSocketPriceContext';
import { formatCurrency, formatPercentage, cn } from '../utils';
import type { MarketData } from '../types';

const StrategyTest: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const symbol = searchParams.get('symbol') || '';
  const name = searchParams.get('name') || symbol;

  const { data: marketData } = useMarketOverview();
  const { prices, subscribeToSymbol, unsubscribeFromSymbol, isConnected } = useWebSocketPrices();
  const [selectedStrategy, setSelectedStrategy] = useState<string>('momentum');
  const [timeframe, setTimeframe] = useState<string>('1d');
  const [amount, setAmount] = useState<string>('10000');

  // Subscribe to symbol when page loads
  useEffect(() => {
    if (symbol && isConnected) {
      console.log('[StrategyTest] Subscribing to symbol:', symbol);
      subscribeToSymbol(symbol).catch(err => {
        console.error('[StrategyTest] Failed to subscribe to symbol:', err);
      });

      // Cleanup: unsubscribe when component unmounts or symbol changes
      return () => {
        console.log('[StrategyTest] Unsubscribing from symbol:', symbol);
        unsubscribeFromSymbol(symbol).catch(err => {
          console.error('[StrategyTest] Failed to unsubscribe from symbol:', err);
        });
      };
    }
  }, [symbol, isConnected, subscribeToSymbol, unsubscribeFromSymbol]);

  // Get current market data for the symbol - prioritize WebSocket data
  const currentMarketData = React.useMemo(() => {
    if (!symbol) return null;

    // First, try to get real-time WebSocket data
    const wsData = prices[symbol];
    if (wsData) {
      return {
        symbol: symbol,
        price: wsData.price,
        change: wsData.change,
        changePercent: wsData.changePercent,
        volume: wsData.volume,
        high: wsData.high || wsData.price * 1.05, // Use real high from WebSocket or approximate
        low: wsData.low || wsData.price * 0.95,   // Use real low from WebSocket or approximate
        open: wsData.open || wsData.price,        // Use real open from WebSocket or current price
      } as MarketData;
    }

    // Fall back to initial market data if WebSocket hasn't updated yet
    if (marketData) {
      const dataArray = Object.values(marketData as Record<string, MarketData>);
      return dataArray.find((item: MarketData) => item.symbol === symbol) || null;
    }

    return null;
  }, [symbol, prices, marketData]);

  if (!symbol) {
    return (
      <AuthenticatedLayout>
        <div className="text-center py-12">
          <h2 className="text-2xl font-bold text-text-primary mb-4">No Symbol Selected</h2>
          <p className="text-text-tertiary mb-6">Please select a symbol to test strategies.</p>
          <Button onClick={() => navigate('/markets')}>
            Browse Markets
          </Button>
        </div>
      </AuthenticatedLayout>
    );
  }

  return (
    <AuthenticatedLayout>
      <div className="space-y-6">
        {/* Page Header with Symbol Info */}
        <div className="flex items-center justify-between">
          <div>
            <div className="flex items-center space-x-3 mb-2">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => navigate(-1)}
              >
                ← Back
              </Button>
              <h1 className="text-3xl font-bold text-text-primary">{symbol}</h1>
              {currentMarketData && (
                <Badge
                  variant={currentMarketData.change >= 0 ? 'success' : 'destructive'}
                >
                  {currentMarketData.change >= 0 ? '+' : ''}{formatPercentage(currentMarketData.changePercent)}
                </Badge>
              )}
            </div>
            <p className="text-text-tertiary">{name}</p>
          </div>
        </div>

        {/* Current Price Display */}
        {currentMarketData && (
          <Card className="bg-gradient-to-br from-brand-50 to-brand-100 border-brand-200">
            <CardContent className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-sm font-medium text-text-tertiary">Live Market Data</h3>
                <div className="flex items-center space-x-2">
                  <div className={cn(
                    'h-2 w-2 rounded-full',
                    isConnected ? 'bg-positive-500 animate-pulse' : 'bg-negative-500'
                  )}></div>
                  <span className="text-xs text-text-tertiary">
                    {isConnected ? 'Live' : 'Disconnected'}
                  </span>
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
                <div>
                  <p className="text-sm font-medium text-text-tertiary mb-1">Current Price</p>
                  <p className="text-3xl font-bold text-text-primary">
                    {formatCurrency(currentMarketData.price)}
                  </p>
                </div>
                <div>
                  <p className="text-sm font-medium text-text-tertiary mb-1">24h Change</p>
                  <p className={cn(
                    'text-2xl font-bold',
                    currentMarketData.change >= 0 ? 'text-positive-500' : 'text-negative-500'
                  )}>
                    {currentMarketData.change >= 0 ? '+' : ''}{formatCurrency(currentMarketData.change)}
                  </p>
                </div>
                <div>
                  <p className="text-sm font-medium text-text-tertiary mb-1">24h High</p>
                  <p className="text-2xl font-bold text-text-primary">
                    {formatCurrency(currentMarketData.high || currentMarketData.price * 1.05)}
                  </p>
                </div>
                <div>
                  <p className="text-sm font-medium text-text-tertiary mb-1">24h Low</p>
                  <p className="text-2xl font-bold text-text-primary">
                    {formatCurrency(currentMarketData.low || currentMarketData.price * 0.95)}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Strategy Configuration */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Strategy Selection */}
          <div className="lg:col-span-2">
            <Card>
              <CardHeader>
                <CardTitle>Strategy Configuration</CardTitle>
                <p className="text-text-tertiary">Configure and test your trading strategy</p>
              </CardHeader>
              <CardContent>
                <div className="space-y-6">
                  {/* Strategy Type */}
                  <div>
                    <label className="block text-sm font-medium text-text-primary mb-2">
                      Strategy Type
                    </label>
                    <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                      {[
                        { id: 'momentum', name: 'Momentum', description: 'Buy when price is rising' },
                        { id: 'mean-reversion', name: 'Mean Reversion', description: 'Buy when price dips' },
                        { id: 'trend-following', name: 'Trend Following', description: 'Follow market trends' },
                        { id: 'breakout', name: 'Breakout', description: 'Buy on price breakouts' },
                        { id: 'scalping', name: 'Scalping', description: 'Quick small profits' },
                        { id: 'custom', name: 'Custom', description: 'Create your own' },
                      ].map((strategy) => (
                        <button
                          key={strategy.id}
                          onClick={() => setSelectedStrategy(strategy.id)}
                          className={cn(
                            'p-4 rounded-lg border-2 text-left transition-all',
                            selectedStrategy === strategy.id
                              ? 'border-brand-500 bg-brand-50'
                              : 'border-border-default hover:border-brand-200'
                          )}
                        >
                          <div className="font-medium text-text-primary mb-1">{strategy.name}</div>
                          <div className="text-xs text-text-tertiary">{strategy.description}</div>
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Timeframe */}
                  <div>
                    <label className="block text-sm font-medium text-text-primary mb-2">
                      Timeframe
                    </label>
                    <div className="flex flex-wrap gap-2">
                      {['5m', '15m', '1h', '4h', '1d', '1w'].map((tf) => (
                        <button
                          key={tf}
                          onClick={() => setTimeframe(tf)}
                          className={cn(
                            'px-4 py-2 rounded-lg font-medium transition-colors',
                            timeframe === tf
                              ? 'bg-brand-500 text-white'
                              : 'bg-background-secondary text-text-primary hover:bg-background-tertiary'
                          )}
                        >
                          {tf}
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Investment Amount */}
                  <div>
                    <label className="block text-sm font-medium text-text-primary mb-2">
                      Investment Amount
                    </label>
                    <div className="relative">
                      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary">$</span>
                      <input
                        type="number"
                        value={amount}
                        onChange={(e) => setAmount(e.target.value)}
                        className="w-full pl-8 pr-4 py-3 border border-border-default rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500"
                        placeholder="10000"
                      />
                    </div>
                  </div>

                  {/* Action Buttons */}
                  <div className="flex space-x-3">
                    <Button variant="primary" className="flex-1">
                      Run Backtest
                    </Button>
                    <Button variant="secondary">
                      Save Configuration
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Backtest Results */}
            <Card className="mt-6">
              <CardHeader>
                <CardTitle>Backtest Results</CardTitle>
                <p className="text-text-tertiary">Performance metrics for the selected strategy</p>
              </CardHeader>
              <CardContent>
                <div className="text-center py-12">
                  <div className="text-6xl mb-4">📊</div>
                  <p className="text-text-tertiary mb-4">
                    Configure your strategy and run a backtest to see results
                  </p>
                  <Button variant="primary">
                    Run Your First Backtest
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Strategy Info */}
            <Card>
              <CardHeader>
                <CardTitle>Strategy Info</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <p className="text-sm text-text-tertiary mb-1">Selected Strategy</p>
                    <p className="font-semibold text-text-primary capitalize">
                      {selectedStrategy.replace('-', ' ')}
                    </p>
                  </div>
                  <div>
                    <p className="text-sm text-text-tertiary mb-1">Symbol</p>
                    <p className="font-semibold text-text-primary">{symbol}</p>
                  </div>
                  <div>
                    <p className="text-sm text-text-tertiary mb-1">Timeframe</p>
                    <p className="font-semibold text-text-primary">{timeframe}</p>
                  </div>
                  <div>
                    <p className="text-sm text-text-tertiary mb-1">Investment</p>
                    <p className="font-semibold text-text-primary">{formatCurrency(parseFloat(amount))}</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Quick Actions */}
            <Card>
              <CardHeader>
                <CardTitle>Quick Actions</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <Button variant="secondary" className="w-full" onClick={() => navigate('/strategies')}>
                    My Strategies
                  </Button>
                  <Button variant="secondary" className="w-full" onClick={() => navigate('/markets')}>
                    Browse Markets
                  </Button>
                  <Button variant="secondary" className="w-full" onClick={() => navigate('/portfolio')}>
                    View Portfolio
                  </Button>
                </div>
              </CardContent>
            </Card>

            {/* Help */}
            <Card>
              <CardHeader>
                <CardTitle>Need Help?</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-text-tertiary mb-4">
                  Learn how to create and test effective trading strategies with our comprehensive guides.
                </p>
                <Button variant="secondary" className="w-full">
                  View Tutorials
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </AuthenticatedLayout>
  );
};

export default StrategyTest;
