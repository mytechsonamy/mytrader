/**
 * Dashboard page - Public dashboard accessible to all users
 */

import React, { useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent, PriceChangeBadge } from '../components/ui';
import { useMarketOverview, useTopMovers, useAssetClasses } from '../hooks/useMarketData';
import { useAuthStore } from '../store/authStore';
import { formatCurrency, formatPercentage, cn } from '../utils';
import type { MarketData, TopMover, AssetClass } from '../types';
import AuthenticatedLayout from '../components/layout/AuthenticatedLayout';
import PublicLayout from '../components/layout/PublicLayout';

const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const { isAuthenticated, isGuest, checkAuthStatus } = useAuthStore();
  const { data: marketData, isLoading: isMarketLoading } = useMarketOverview();
  const { data: topMovers, isLoading: isTopMoversLoading } = useTopMovers();
  const { data: assetClasses } = useAssetClasses();

  useEffect(() => {
    // Check auth status when component mounts
    checkAuthStatus();
  }, [checkAuthStatus]);

  const renderMarketOverview = () => {
    if (isMarketLoading) {
      return (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i} className="animate-pulse">
              <CardContent className="p-6">
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

    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {Object.values(marketData as Record<string, MarketData>).slice(0, 8).map((item: MarketData) => (
          <Card
            key={item.symbolId}
            className="hover:shadow-lg transition-shadow cursor-pointer"
            onClick={() => navigate(`/strategies/test?symbol=${item.symbol}&name=${encodeURIComponent(item.name || item.symbol)}`)}
          >
            <CardContent className="p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="font-semibold text-text-primary">{item.symbol}</span>
                <PriceChangeBadge
                  change={item.change}
                  changePercent={item.changePercent}
                />
              </div>
              <div className="text-2xl font-bold mb-1">
                {formatCurrency(item.price)}
              </div>
              <div
                className={cn(
                  'text-sm font-medium',
                  item.change >= 0 ? 'text-positive-500' : 'text-negative-500'
                )}
              >
                {item.change >= 0 ? '+' : ''}{formatCurrency(item.change)} (
                {formatPercentage(item.changePercent)})
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
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
            className="flex items-center justify-between p-3 rounded-lg hover:bg-background-secondary transition-colors"
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

  const dashboardContent = (
    <div className="space-y-6">
      {/* Welcome Section for Authenticated Users */}
      {isAuthenticated && (
        <div className="bg-gradient-brand text-white rounded-lg p-6">
          <div className="text-center">
            <h1 className="text-3xl font-bold mb-4">
              Welcome back to myTrader!
            </h1>
            <p className="text-lg opacity-90 mb-6">
              Your personalized trading dashboard with real-time market insights
            </p>
            <div className="flex flex-col sm:flex-row justify-center gap-4">
              <Link
                to="/portfolio"
                className="btn-primary bg-white text-brand-500 hover:bg-gray-100"
              >
                View Portfolio
              </Link>
              <Link
                to="/strategies"
                className="btn-secondary border-white text-white hover:bg-white/10"
              >
                Manage Strategies
              </Link>
            </div>
          </div>
        </div>
      )}

      {/* Hero Section for Public Users */}
      {!isAuthenticated && (
        <div className="bg-gradient-brand text-white rounded-lg p-8">
          <div className="text-center">
            <h1 className="text-4xl font-bold mb-4">
              Welcome to myTrader
            </h1>
            <p className="text-xl opacity-90 mb-6">
              Trade smarter with AI-powered insights and real-time market data
            </p>
            <div className="flex flex-col sm:flex-row justify-center gap-4">
              <Link
                to="/register"
                className="btn-primary bg-white text-brand-500 hover:bg-gray-100"
              >
                Start Trading
              </Link>
              <Link
                to="/login"
                className="btn-secondary border-white text-white hover:bg-white/10"
              >
                Sign In
              </Link>
            </div>
          </div>
        </div>
      )}

      {/* Market Overview Section */}
      <section className="space-y-6">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-text-primary">Market Overview</h2>
          <div className="flex items-center space-x-4">
            <span className="text-sm text-text-tertiary">
              Live prices • Updated every minute
            </span>
            <Link
              to="/markets"
              className="btn-secondary text-sm"
            >
              View All Markets
            </Link>
          </div>
        </div>
        {renderMarketOverview()}
      </section>

      {/* Top Movers Section */}
      <section className="space-y-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle>Top Movers</CardTitle>
                    <p className="text-text-tertiary">
                      Biggest price movements across all asset classes
                    </p>
                  </div>
                  <Link
                    to="/markets"
                    className="btn-secondary text-sm"
                  >
                    View All
                  </Link>
                </div>
              </CardHeader>
              <CardContent>
                {renderTopMovers()}
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Asset Classes */}
            <Card>
              <CardHeader>
                <CardTitle>Asset Classes</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {(assetClasses as AssetClass[])?.map((assetClass: AssetClass) => (
                    <Link
                      key={assetClass.id}
                      to={`/markets?asset=${assetClass.id}`}
                      className="flex items-center justify-between p-3 rounded-lg hover:bg-background-secondary transition-colors cursor-pointer"
                    >
                      <div className="flex items-center space-x-3">
                        <span className="text-lg">{assetClass.icon}</span>
                        <span className="font-medium">{assetClass.displayName}</span>
                      </div>
                      <span className="text-sm text-text-tertiary">→</span>
                    </Link>
                  ))}
                </div>
              </CardContent>
            </Card>

            {/* Quick Stats */}
            <Card>
              <CardHeader>
                <CardTitle>Market Stats</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="flex justify-between">
                    <span className="text-text-tertiary">Total Symbols</span>
                    <span className="font-semibold">
                      {marketData ? Object.keys(marketData as Record<string, MarketData>).length : 0}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-text-tertiary">Gainers</span>
                    <span className="font-semibold text-positive-500">
                      {marketData
                        ? Object.values(marketData as Record<string, MarketData>).filter((d: MarketData) => d.change > 0).length
                        : 0}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-text-tertiary">Losers</span>
                    <span className="font-semibold text-negative-500">
                      {marketData
                        ? Object.values(marketData as Record<string, MarketData>).filter((d: MarketData) => d.change < 0).length
                        : 0}
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Quick Actions for Authenticated Users */}
            {isAuthenticated && (
              <Card>
                <CardHeader>
                  <CardTitle>Quick Actions</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    <Link
                      to="/alerts"
                      className="block w-full btn-secondary text-center"
                    >
                      🔔 Create Alert
                    </Link>
                    <Link
                      to="/strategies"
                      className="block w-full btn-secondary text-center"
                    >
                      🎯 New Strategy
                    </Link>
                    <Link
                      to="/competition"
                      className="block w-full btn-secondary text-center"
                    >
                      🏆 Join Competition
                    </Link>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      </section>
    </div>
  );

  // Use different layouts based on authentication status
  if (isAuthenticated) {
    return (
      <AuthenticatedLayout>
        {dashboardContent}
      </AuthenticatedLayout>
    );
  }

  return (
    <PublicLayout showCTA={false}>
      {dashboardContent}
    </PublicLayout>
  );
};

export default Dashboard;