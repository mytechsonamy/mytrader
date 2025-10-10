/**
 * Markets page - Extended market data and analysis
 */

import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent, Button, Badge } from '../components/ui';
import AuthenticatedLayout from '../components/layout/AuthenticatedLayout';
import { useMarketOverview, useTopMovers, useAssetClasses } from '../hooks/useMarketData';
import { formatCurrency, formatPercentage, cn } from '../utils';
import type { MarketData, TopMover, AssetClass } from '../types';

const Markets: React.FC = () => {
  const navigate = useNavigate();
  const [selectedAssetClass, setSelectedAssetClass] = useState<string>('all');
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');

  const { data: marketData, isLoading: isMarketLoading } = useMarketOverview();
  const { data: topMovers, isLoading: isTopMoversLoading } = useTopMovers();
  const { data: assetClasses } = useAssetClasses();

  const renderMarketStats = () => (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <Card>
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-text-tertiary">Market Cap</p>
              <p className="text-2xl font-bold text-text-primary">$45.2T</p>
            </div>
            <div className="text-positive-500">
              <span className="text-sm font-medium">+2.3%</span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-text-tertiary">24h Volume</p>
              <p className="text-2xl font-bold text-text-primary">$2.8T</p>
            </div>
            <div className="text-positive-500">
              <span className="text-sm font-medium">+12.4%</span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-text-tertiary">Active Symbols</p>
              <p className="text-2xl font-bold text-text-primary">8,247</p>
            </div>
            <div className="text-text-tertiary">
              <span className="text-sm font-medium">Live</span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-text-tertiary">Market Status</p>
              <p className="text-2xl font-bold text-positive-500">OPEN</p>
            </div>
            <div className="flex items-center">
              <div className="w-2 h-2 bg-positive-500 rounded-full mr-2"></div>
              <span className="text-sm text-text-tertiary">Live</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );

  const renderAssetClassFilters = () => (
    <div className="flex flex-wrap gap-2">
      <Button
        variant={selectedAssetClass === 'all' ? 'primary' : 'secondary'}
        size="sm"
        onClick={() => setSelectedAssetClass('all')}
      >
        All Markets
      </Button>
      {(assetClasses as AssetClass[])?.map((assetClass: AssetClass) => (
        <Button
          key={assetClass.id}
          variant={selectedAssetClass === assetClass.id ? 'primary' : 'secondary'}
          size="sm"
          onClick={() => setSelectedAssetClass(assetClass.id)}
        >
          {assetClass.displayName}
        </Button>
      ))}
    </div>
  );

  const renderMarketData = () => {
    if (isMarketLoading) {
      return (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {[...Array(12)].map((_, i) => (
            <Card key={i} className="animate-pulse">
              <CardContent className="p-4">
                <div className="h-4 bg-gray-200 rounded mb-2"></div>
                <div className="h-8 bg-gray-200 rounded mb-2"></div>
                <div className="h-4 bg-gray-200 rounded w-2/3"></div>
              </CardContent>
            </Card>
          ))}
        </div>
      );
    }

    if (!marketData) {
      return (
        <Card>
          <CardContent className="text-center py-8">
            <p className="text-text-tertiary">No market data available</p>
          </CardContent>
        </Card>
      );
    }

    const marketDataArray = Object.values(marketData as Record<string, MarketData>);

    if (viewMode === 'grid') {
      return (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {marketDataArray.map((item: MarketData) => (
            <Card
              key={item.symbolId}
              className="hover:shadow-lg transition-shadow cursor-pointer"
              onClick={() => navigate(`/strategies/test?symbol=${item.symbol}&name=${encodeURIComponent(item.name || item.symbol)}`)}
            >
              <CardContent className="p-4">
                <div className="flex items-center justify-between mb-3">
                  <div className="flex-1 min-w-0 mr-2">
                    <h3 className="font-semibold text-text-primary truncate">{item.symbol}</h3>
                    <p className="text-xs text-text-tertiary truncate">{item.name || 'Company Name'}</p>
                  </div>
                  <Badge
                    variant={item.change >= 0 ? 'success' : 'destructive'}
                    className="text-xs shrink-0"
                  >
                    {item.change >= 0 ? '+' : ''}{formatPercentage(item.changePercent)}
                  </Badge>
                </div>

                <div className="space-y-2">
                  <div className="text-xl lg:text-2xl font-bold text-text-primary font-mono tabular-nums truncate">
                    {formatCurrency(item.price)}
                  </div>
                  <div
                    className={cn(
                      'text-sm font-medium font-mono tabular-nums truncate',
                      item.change >= 0 ? 'text-positive-500' : 'text-negative-500'
                    )}
                  >
                    {item.change >= 0 ? '+' : ''}{formatCurrency(item.change)}
                  </div>

                  <div className="grid grid-cols-2 gap-2 pt-2 text-xs text-text-tertiary">
                    <div>
                      <span className="block">High</span>
                      <span className="font-medium text-text-primary font-mono tabular-nums text-xs truncate block">{formatCurrency(item.high || item.price * 1.05)}</span>
                    </div>
                    <div>
                      <span className="block">Low</span>
                      <span className="font-medium text-text-primary font-mono tabular-nums text-xs truncate block">{formatCurrency(item.low || item.price * 0.95)}</span>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      );
    }

    // List view
    return (
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full table-fixed">
              <thead>
                <tr className="border-b border-border-default">
                  <th className="text-left py-3 px-4 font-medium text-text-tertiary w-[180px]">Symbol</th>
                  <th className="text-right py-3 px-4 font-medium text-text-tertiary w-[120px]">Price</th>
                  <th className="text-right py-3 px-4 font-medium text-text-tertiary w-[100px]">Change</th>
                  <th className="text-right py-3 px-4 font-medium text-text-tertiary w-[90px]">Chg %</th>
                  <th className="text-right py-3 px-4 font-medium text-text-tertiary w-[120px]">High</th>
                  <th className="text-right py-3 px-4 font-medium text-text-tertiary w-[120px]">Low</th>
                  <th className="text-right py-3 px-4 font-medium text-text-tertiary w-[100px]">Volume</th>
                </tr>
              </thead>
              <tbody>
                {marketDataArray.map((item: MarketData) => (
                  <tr
                    key={item.symbolId}
                    className="border-b border-border-light hover:bg-background-secondary cursor-pointer"
                    onClick={() => navigate(`/strategies/test?symbol=${item.symbol}&name=${encodeURIComponent(item.name || item.symbol)}`)}
                  >
                    <td className="py-3 px-4">
                      <div>
                        <div className="font-semibold text-text-primary">{item.symbol}</div>
                        <div className="text-sm text-text-tertiary truncate max-w-32">{item.name || 'Company Name'}</div>
                      </div>
                    </td>
                    <td className="text-right py-3 px-4 font-semibold text-text-primary font-mono tabular-nums">
                      {formatCurrency(item.price)}
                    </td>
                    <td className={`text-right py-3 px-4 font-medium font-mono tabular-nums ${item.change >= 0 ? 'text-positive-500' : 'text-negative-500'}`}>
                      {item.change >= 0 ? '+' : ''}{formatCurrency(item.change)}
                    </td>
                    <td className={`text-right py-3 px-4 font-medium font-mono tabular-nums ${item.changePercent >= 0 ? 'text-positive-500' : 'text-negative-500'}`}>
                      {item.changePercent >= 0 ? '+' : ''}{formatPercentage(item.changePercent)}
                    </td>
                    <td className="text-right py-3 px-4 text-text-primary font-mono tabular-nums">
                      {formatCurrency(item.high || item.price * 1.05)}
                    </td>
                    <td className="text-right py-3 px-4 text-text-primary font-mono tabular-nums">
                      {formatCurrency(item.low || item.price * 0.95)}
                    </td>
                    <td className="text-right py-3 px-4 text-text-tertiary text-sm font-mono tabular-nums">
                      {((item.volume || 0) / 1000000).toFixed(1)}M
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    );
  };

  const renderTopMovers = () => {
    if (isTopMoversLoading) {
      return (
        <div className="space-y-3">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="flex items-center space-x-4 p-3 animate-pulse">
              <div className="h-10 w-10 bg-gray-200 rounded-full"></div>
              <div className="flex-1">
                <div className="h-4 bg-gray-200 rounded mb-1"></div>
                <div className="h-3 bg-gray-200 rounded w-2/3"></div>
              </div>
              <div className="h-6 w-16 bg-gray-200 rounded"></div>
            </div>
          ))}
        </div>
      );
    }

    if (!topMovers || topMovers.length === 0) {
      return (
        <div className="text-center py-8 text-text-tertiary">
          <p>No top movers data available</p>
        </div>
      );
    }

    return (
      <div className="space-y-3">
        {(topMovers as TopMover[]).slice(0, 10).map((mover: TopMover, index: number) => (
          <div
            key={mover.symbol}
            className="flex items-center justify-between p-3 rounded-lg hover:bg-background-secondary transition-colors cursor-pointer"
          >
            <div className="flex items-center space-x-3">
              <div className="flex items-center justify-center w-8 h-8 bg-brand-100 text-brand-600 rounded-full text-sm font-medium">
                {index + 1}
              </div>
              <div>
                <div className="font-semibold text-text-primary">{mover.symbol}</div>
                <div className="text-sm text-text-tertiary">{mover.name}</div>
              </div>
            </div>
            <div className="text-right">
              <div className="font-semibold">{formatCurrency(mover.price)}</div>
              <div
                className={cn(
                  'text-sm font-medium',
                  mover.changePercent >= 0 ? 'text-positive-500' : 'text-negative-500'
                )}
              >
                {mover.changePercent >= 0 ? '+' : ''}{formatPercentage(mover.changePercent)}
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  };

  return (
    <AuthenticatedLayout>
      <div className="space-y-6">
        {/* Page Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-text-primary">Markets</h1>
            <p className="text-text-tertiary mt-1">
              Real-time market data and analysis
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant={viewMode === 'grid' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setViewMode('grid')}
            >
              Grid
            </Button>
            <Button
              variant={viewMode === 'list' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setViewMode('list')}
            >
              List
            </Button>
          </div>
        </div>

        {/* Market Statistics */}
        {renderMarketStats()}

        {/* Filters */}
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-xl font-semibold text-text-primary mb-2">Asset Classes</h2>
            {renderAssetClassFilters()}
          </div>
        </div>

        {/* Main Content */}
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
          {/* Market Data - Main Content */}
          <div className="lg:col-span-3">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Live Market Data</CardTitle>
                  <div className="flex items-center text-sm text-text-tertiary">
                    <div className="w-2 h-2 bg-positive-500 rounded-full mr-2 animate-pulse"></div>
                    Updated every minute
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                {renderMarketData()}
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Top Movers */}
            <Card>
              <CardHeader>
                <CardTitle>Top Movers</CardTitle>
                <p className="text-text-tertiary">Biggest changes today</p>
              </CardHeader>
              <CardContent>
                {renderTopMovers()}
              </CardContent>
            </Card>

            {/* Market News */}
            <Card>
              <CardHeader>
                <CardTitle>Market News</CardTitle>
                <p className="text-text-tertiary">Latest market updates</p>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {[
                    { title: 'Fed Announces Interest Rate Decision', time: '2h ago', source: 'Reuters' },
                    { title: 'Tech Stocks Rally After Earnings', time: '4h ago', source: 'Bloomberg' },
                    { title: 'Oil Prices Surge on Supply Concerns', time: '6h ago', source: 'WSJ' },
                  ].map((news, index) => (
                    <div key={index} className="p-3 rounded-lg hover:bg-background-secondary cursor-pointer">
                      <h4 className="font-medium text-text-primary text-sm mb-1">{news.title}</h4>
                      <div className="flex items-center justify-between text-xs text-text-tertiary">
                        <span>{news.source}</span>
                        <span>{news.time}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </AuthenticatedLayout>
  );
};

export default Markets;