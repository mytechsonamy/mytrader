/**
 * Strategies page - Trading strategies and backtesting
 */

import React, { useState } from 'react';
import { Card, CardHeader, CardTitle, CardContent, Button, Badge } from '../components/ui';
import AuthenticatedLayout from '../components/layout/AuthenticatedLayout';
import { formatCurrency, formatPercentage, cn } from '../utils';

interface Strategy {
  id: string;
  name: string;
  description: string;
  type: 'momentum' | 'mean-reversion' | 'trend-following' | 'arbitrage' | 'custom';
  status: 'active' | 'paused' | 'draft';
  performance: {
    totalReturn: number;
    returnPercentage: number;
    sharpeRatio: number;
    maxDrawdown: number;
    winRate: number;
    trades: number;
  };
  risk: 'low' | 'medium' | 'high';
  createdAt: string;
  lastModified: string;
}

const Strategies: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'my-strategies' | 'marketplace' | 'backtest'>('my-strategies');
  const [showCreateModal, setShowCreateModal] = useState(false);

  // Mock data - replace with actual API calls
  const mockStrategies: Strategy[] = [
    {
      id: '1',
      name: 'Tech Momentum Strategy',
      description: 'Focuses on momentum plays in technology stocks with high volume',
      type: 'momentum',
      status: 'active',
      performance: {
        totalReturn: 15847.30,
        returnPercentage: 18.2,
        sharpeRatio: 1.45,
        maxDrawdown: -8.5,
        winRate: 72.4,
        trades: 156
      },
      risk: 'medium',
      createdAt: '2024-01-01T00:00:00Z',
      lastModified: '2024-01-15T10:30:00Z'
    },
    {
      id: '2',
      name: 'Mean Reversion Crypto',
      description: 'Mean reversion strategy for major cryptocurrencies',
      type: 'mean-reversion',
      status: 'paused',
      performance: {
        totalReturn: 8234.56,
        returnPercentage: 12.7,
        sharpeRatio: 0.98,
        maxDrawdown: -15.2,
        winRate: 64.8,
        trades: 89
      },
      risk: 'high',
      createdAt: '2023-12-15T00:00:00Z',
      lastModified: '2024-01-10T14:20:00Z'
    },
    {
      id: '3',
      name: 'Conservative Dividend Growth',
      description: 'Long-term dividend growth strategy with low volatility',
      type: 'trend-following',
      status: 'active',
      performance: {
        totalReturn: 4562.78,
        returnPercentage: 9.8,
        sharpeRatio: 1.82,
        maxDrawdown: -3.2,
        winRate: 78.5,
        trades: 45
      },
      risk: 'low',
      createdAt: '2023-11-01T00:00:00Z',
      lastModified: '2024-01-12T09:15:00Z'
    }
  ];

  const marketplaceStrategies = [
    {
      id: 'mp1',
      name: 'RSI Swing Trading',
      author: 'QuantWizard',
      rating: 4.7,
      subscribers: 1247,
      performance: 24.5,
      price: 49.99,
      description: 'Advanced RSI-based swing trading strategy for stocks'
    },
    {
      id: 'mp2',
      name: 'Breakout Scanner',
      author: 'TradePro',
      rating: 4.3,
      subscribers: 856,
      performance: 18.2,
      price: 29.99,
      description: 'Automated breakout detection for day trading'
    },
    {
      id: 'mp3',
      name: 'Pairs Trading Algorithm',
      author: 'AlgoTrader',
      rating: 4.9,
      subscribers: 2134,
      performance: 16.8,
      price: 99.99,
      description: 'Statistical arbitrage using pairs trading'
    }
  ];

  const getStrategyTypeColor = (type: Strategy['type']) => {
    switch (type) {
      case 'momentum':
        return 'bg-blue-100 text-blue-700';
      case 'mean-reversion':
        return 'bg-purple-100 text-purple-700';
      case 'trend-following':
        return 'bg-green-100 text-green-700';
      case 'arbitrage':
        return 'bg-orange-100 text-orange-700';
      case 'custom':
        return 'bg-gray-100 text-gray-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  };

  const getStatusColor = (status: Strategy['status']) => {
    switch (status) {
      case 'active':
        return 'bg-green-100 text-green-700';
      case 'paused':
        return 'bg-yellow-100 text-yellow-700';
      case 'draft':
        return 'bg-gray-100 text-gray-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  };

  const getRiskColor = (risk: Strategy['risk']) => {
    switch (risk) {
      case 'low':
        return 'text-green-600';
      case 'medium':
        return 'text-yellow-600';
      case 'high':
        return 'text-red-600';
      default:
        return 'text-gray-600';
    }
  };

  const renderStrategyCard = (strategy: Strategy) => (
    <Card key={strategy.id} className="hover:shadow-lg transition-shadow">
      <CardContent className="p-6">
        <div className="flex items-start justify-between mb-4">
          <div className="flex-1">
            <h3 className="text-lg font-semibold text-text-primary mb-2">{strategy.name}</h3>
            <p className="text-text-tertiary text-sm mb-3">{strategy.description}</p>
          </div>
          <div className="flex flex-col items-end space-y-2">
            <Badge className={getStatusColor(strategy.status)}>
              {strategy.status.charAt(0).toUpperCase() + strategy.status.slice(1)}
            </Badge>
            <Badge className={getStrategyTypeColor(strategy.type)}>
              {strategy.type.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase())}
            </Badge>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4 mb-4">
          <div>
            <p className="text-sm text-text-tertiary">Total Return</p>
            <p className={cn(
              'font-semibold',
              strategy.performance.returnPercentage >= 0 ? 'text-positive-500' : 'text-negative-500'
            )}>
              {strategy.performance.returnPercentage >= 0 ? '+' : ''}{formatPercentage(strategy.performance.returnPercentage)}
            </p>
          </div>
          <div>
            <p className="text-sm text-text-tertiary">Sharpe Ratio</p>
            <p className="font-semibold text-text-primary">{strategy.performance.sharpeRatio.toFixed(2)}</p>
          </div>
          <div>
            <p className="text-sm text-text-tertiary">Win Rate</p>
            <p className="font-semibold text-text-primary">{formatPercentage(strategy.performance.winRate)}</p>
          </div>
          <div>
            <p className="text-sm text-text-tertiary">Max Drawdown</p>
            <p className="font-semibold text-negative-500">{formatPercentage(strategy.performance.maxDrawdown)}</p>
          </div>
        </div>

        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center space-x-2">
            <span className="text-sm text-text-tertiary">Risk Level:</span>
            <span className={cn('text-sm font-medium', getRiskColor(strategy.risk))}>
              {strategy.risk.charAt(0).toUpperCase() + strategy.risk.slice(1)}
            </span>
          </div>
          <div className="text-sm text-text-tertiary">
            {strategy.performance.trades} trades
          </div>
        </div>

        <div className="flex items-center justify-between">
          <div className="text-sm text-text-tertiary">
            Modified {new Date(strategy.lastModified).toLocaleDateString()}
          </div>
          <div className="flex space-x-2">
            {strategy.status === 'active' && (
              <Button variant="secondary" size="sm">Pause</Button>
            )}
            {strategy.status === 'paused' && (
              <Button variant="primary" size="sm">Resume</Button>
            )}
            <Button variant="secondary" size="sm">Edit</Button>
            <Button variant="secondary" size="sm">Backtest</Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderMarketplaceCard = (strategy: any) => (
    <Card key={strategy.id} className="hover:shadow-lg transition-shadow">
      <CardContent className="p-6">
        <div className="flex items-start justify-between mb-4">
          <div className="flex-1">
            <h3 className="text-lg font-semibold text-text-primary mb-1">{strategy.name}</h3>
            <p className="text-sm text-text-tertiary mb-2">by {strategy.author}</p>
            <p className="text-text-tertiary text-sm mb-3">{strategy.description}</p>
          </div>
          <div className="text-right">
            <div className="text-lg font-bold text-text-primary">{formatCurrency(strategy.price)}</div>
            <div className="text-sm text-text-tertiary">one-time</div>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4 mb-4">
          <div>
            <p className="text-sm text-text-tertiary">Performance</p>
            <p className="font-semibold text-positive-500">+{formatPercentage(strategy.performance)}</p>
          </div>
          <div>
            <p className="text-sm text-text-tertiary">Rating</p>
            <div className="flex items-center">
              <span className="font-semibold text-text-primary mr-1">{strategy.rating}</span>
              <span className="text-yellow-500">★</span>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <div className="text-sm text-text-tertiary">
            {strategy.subscribers} subscribers
          </div>
          <div className="flex space-x-2">
            <Button variant="secondary" size="sm">Preview</Button>
            <Button variant="primary" size="sm">Purchase</Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderBacktestInterface = () => (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Strategy Backtesting</CardTitle>
          <p className="text-text-tertiary">Test your strategies against historical data</p>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Strategy
              </label>
              <select className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                <option>Select Strategy...</option>
                {mockStrategies.map(strategy => (
                  <option key={strategy.id} value={strategy.id}>{strategy.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Start Date
              </label>
              <input
                type="date"
                className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                defaultValue="2023-01-01"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                End Date
              </label>
              <input
                type="date"
                className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                defaultValue="2024-01-15"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Initial Capital
              </label>
              <input
                type="number"
                className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                defaultValue="100000"
                placeholder="100000"
              />
            </div>
          </div>
          <Button variant="primary">Run Backtest</Button>
        </CardContent>
      </Card>

      {/* Backtest Results */}
      <Card>
        <CardHeader>
          <CardTitle>Backtest Results</CardTitle>
          <p className="text-text-tertiary">Performance metrics for selected period</p>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
            <div className="text-center">
              <div className="text-2xl font-bold text-positive-500">+24.8%</div>
              <div className="text-sm text-text-tertiary">Total Return</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-text-primary">1.64</div>
              <div className="text-sm text-text-tertiary">Sharpe Ratio</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-negative-500">-12.3%</div>
              <div className="text-sm text-text-tertiary">Max Drawdown</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-text-primary">68.4%</div>
              <div className="text-sm text-text-tertiary">Win Rate</div>
            </div>
          </div>

          <div className="bg-gray-100 rounded-lg p-6 text-center">
            <div className="text-text-tertiary mb-2">Performance Chart</div>
            <div className="h-64 bg-white rounded border-2 border-dashed border-gray-300 flex items-center justify-center">
              <p className="text-text-tertiary">Chart visualization would appear here</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );

  return (
    <AuthenticatedLayout>
      <div className="space-y-6">
        {/* Page Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-text-primary">Strategies</h1>
            <p className="text-text-tertiary mt-1">
              Create, manage, and backtest your trading strategies
            </p>
          </div>
          <Button
            variant="primary"
            onClick={() => setShowCreateModal(true)}
          >
            + Create Strategy
          </Button>
        </div>

        {/* Tabs */}
        <div className="flex items-center space-x-4 border-b border-border-default">
          {[
            { key: 'my-strategies', label: 'My Strategies' },
            { key: 'marketplace', label: 'Marketplace' },
            { key: 'backtest', label: 'Backtesting' }
          ].map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key as any)}
              className={cn(
                'pb-3 px-1 text-sm font-medium border-b-2 transition-colors',
                activeTab === tab.key
                  ? 'border-brand-500 text-brand-600'
                  : 'border-transparent text-text-tertiary hover:text-text-primary'
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Tab Content */}
        {activeTab === 'my-strategies' && (
          <div className="space-y-6">
            {/* Strategy Performance Overview */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <Card>
                <CardContent className="p-4">
                  <div className="text-center">
                    <div className="text-2xl font-bold text-text-primary">{mockStrategies.length}</div>
                    <div className="text-sm text-text-tertiary">Active Strategies</div>
                  </div>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="p-4">
                  <div className="text-center">
                    <div className="text-2xl font-bold text-positive-500">+16.2%</div>
                    <div className="text-sm text-text-tertiary">Avg Return</div>
                  </div>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="p-4">
                  <div className="text-center">
                    <div className="text-2xl font-bold text-text-primary">1.42</div>
                    <div className="text-sm text-text-tertiary">Avg Sharpe</div>
                  </div>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="p-4">
                  <div className="text-center">
                    <div className="text-2xl font-bold text-text-primary">71.9%</div>
                    <div className="text-sm text-text-tertiary">Avg Win Rate</div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Strategy Cards */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {mockStrategies.map(renderStrategyCard)}
            </div>
          </div>
        )}

        {activeTab === 'marketplace' && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <p className="text-text-tertiary">
                Discover and purchase strategies created by professional traders
              </p>
              <div className="flex items-center space-x-2">
                <select className="px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                  <option>Sort by Performance</option>
                  <option>Sort by Rating</option>
                  <option>Sort by Price</option>
                  <option>Sort by Popularity</option>
                </select>
              </div>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
              {marketplaceStrategies.map(renderMarketplaceCard)}
            </div>
          </div>
        )}

        {activeTab === 'backtest' && renderBacktestInterface()}
      </div>
    </AuthenticatedLayout>
  );
};

export default Strategies;