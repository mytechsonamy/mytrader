/**
 * Portfolio page - User's portfolio overview and management
 */

import React from 'react';
import { useAuthStore } from '../store/authStore';
import { Card, CardHeader, CardTitle, CardContent } from '../components/ui';
import AuthenticatedLayout from '../components/layout/AuthenticatedLayout';
import { formatCurrency, formatPercentage } from '../utils';

const Portfolio: React.FC = () => {
  const { user } = useAuthStore();

  return (
    <AuthenticatedLayout>
      <div className="space-y-6">
        {/* Page Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-text-primary">Portfolio</h1>
            <p className="text-text-tertiary mt-1">
              Track and manage your investments
            </p>
          </div>
          <div className="flex gap-2">
            <button className="btn-secondary">
              Add Position
            </button>
            <button className="btn-primary">
              Execute Trade
            </button>
          </div>
        </div>

        {/* Portfolio Overview */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-text-tertiary">Total Value</p>
                  <p className="text-2xl font-bold text-text-primary">
                    {formatCurrency(125487.50)}
                  </p>
                </div>
                <div className="text-right">
                  <span className="text-positive-500 text-sm font-medium">
                    +{formatPercentage(8.34)}
                  </span>
                  <p className="text-xs text-text-tertiary">Today</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-text-tertiary">Total Gain/Loss</p>
                  <p className="text-2xl font-bold text-positive-500">
                    +{formatCurrency(12847.50)}
                  </p>
                </div>
                <div className="text-right">
                  <span className="text-positive-500 text-sm font-medium">
                    +{formatPercentage(11.42)}
                  </span>
                  <p className="text-xs text-text-tertiary">All time</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-text-tertiary">Day Change</p>
                  <p className="text-2xl font-bold text-positive-500">
                    +{formatCurrency(2847.30)}
                  </p>
                </div>
                <div className="text-right">
                  <span className="text-positive-500 text-sm font-medium">
                    +{formatPercentage(2.32)}
                  </span>
                  <p className="text-xs text-text-tertiary">Since open</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-text-tertiary">Buying Power</p>
                  <p className="text-2xl font-bold text-text-primary">
                    {formatCurrency(25000.00)}
                  </p>
                </div>
                <div className="text-right">
                  <span className="text-text-tertiary text-sm font-medium">
                    Available
                  </span>
                  <p className="text-xs text-text-tertiary">Cash</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Holdings Table */}
        <Card>
          <CardHeader>
            <CardTitle>Holdings</CardTitle>
            <p className="text-text-tertiary">Your current positions</p>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-border-default">
                    <th className="text-left py-3 px-4 font-medium text-text-tertiary">Symbol</th>
                    <th className="text-right py-3 px-4 font-medium text-text-tertiary">Shares</th>
                    <th className="text-right py-3 px-4 font-medium text-text-tertiary">Avg Cost</th>
                    <th className="text-right py-3 px-4 font-medium text-text-tertiary">Current Price</th>
                    <th className="text-right py-3 px-4 font-medium text-text-tertiary">Market Value</th>
                    <th className="text-right py-3 px-4 font-medium text-text-tertiary">Gain/Loss</th>
                    <th className="text-right py-3 px-4 font-medium text-text-tertiary">%</th>
                  </tr>
                </thead>
                <tbody>
                  {/* Sample holdings data */}
                  {[
                    { symbol: 'AAPL', shares: 100, avgCost: 150.25, currentPrice: 175.50, gainLoss: 2525, gainLossPercent: 16.8 },
                    { symbol: 'TSLA', shares: 50, avgCost: 220.80, currentPrice: 195.40, gainLoss: -1270, gainLossPercent: -11.5 },
                    { symbol: 'MSFT', shares: 75, avgCost: 280.90, currentPrice: 335.20, gainLoss: 4072.5, gainLossPercent: 19.3 },
                    { symbol: 'NVDA', shares: 25, avgCost: 420.50, currentPrice: 890.75, gainLoss: 11756.25, gainLossPercent: 111.9 },
                  ].map((holding) => (
                    <tr key={holding.symbol} className="border-b border-border-light hover:bg-background-secondary">
                      <td className="py-3 px-4">
                        <div className="font-semibold text-text-primary">{holding.symbol}</div>
                      </td>
                      <td className="text-right py-3 px-4 text-text-primary">{holding.shares}</td>
                      <td className="text-right py-3 px-4 text-text-primary">{formatCurrency(holding.avgCost)}</td>
                      <td className="text-right py-3 px-4 text-text-primary">{formatCurrency(holding.currentPrice)}</td>
                      <td className="text-right py-3 px-4 text-text-primary">{formatCurrency(holding.shares * holding.currentPrice)}</td>
                      <td className={`text-right py-3 px-4 font-medium ${holding.gainLoss >= 0 ? 'text-positive-500' : 'text-negative-500'}`}>
                        {holding.gainLoss >= 0 ? '+' : ''}{formatCurrency(holding.gainLoss)}
                      </td>
                      <td className={`text-right py-3 px-4 font-medium ${holding.gainLossPercent >= 0 ? 'text-positive-500' : 'text-negative-500'}`}>
                        {holding.gainLossPercent >= 0 ? '+' : ''}{formatPercentage(holding.gainLossPercent)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>

        {/* Asset Allocation */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <Card>
            <CardHeader>
              <CardTitle>Asset Allocation</CardTitle>
              <p className="text-text-tertiary">Portfolio breakdown by asset class</p>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {[
                  { name: 'Stocks', percentage: 75, value: 94115.63, color: 'bg-blue-500' },
                  { name: 'ETFs', percentage: 15, value: 18823.13, color: 'bg-green-500' },
                  { name: 'Cash', percentage: 8, value: 10039.00, color: 'bg-gray-500' },
                  { name: 'Crypto', percentage: 2, value: 2509.75, color: 'bg-orange-500' },
                ].map((allocation) => (
                  <div key={allocation.name} className="flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      <div className={`w-3 h-3 rounded-full ${allocation.color}`}></div>
                      <span className="font-medium text-text-primary">{allocation.name}</span>
                    </div>
                    <div className="text-right">
                      <div className="font-semibold text-text-primary">{formatCurrency(allocation.value)}</div>
                      <div className="text-sm text-text-tertiary">{allocation.percentage}%</div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Recent Activity</CardTitle>
              <p className="text-text-tertiary">Latest portfolio transactions</p>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {[
                  { type: 'BUY', symbol: 'AAPL', shares: 25, price: 175.50, date: '2024-01-15', time: '10:30 AM' },
                  { type: 'SELL', symbol: 'TSLA', shares: 10, price: 195.40, date: '2024-01-14', time: '2:15 PM' },
                  { type: 'BUY', symbol: 'NVDA', shares: 5, price: 890.75, date: '2024-01-12', time: '11:45 AM' },
                ].map((transaction, index) => (
                  <div key={index} className="flex items-center justify-between p-3 rounded-lg hover:bg-background-secondary">
                    <div className="flex items-center space-x-3">
                      <div className={`px-2 py-1 rounded text-xs font-medium ${
                        transaction.type === 'BUY' ? 'bg-positive-100 text-positive-700' : 'bg-negative-100 text-negative-700'
                      }`}>
                        {transaction.type}
                      </div>
                      <div>
                        <div className="font-semibold text-text-primary">{transaction.symbol}</div>
                        <div className="text-sm text-text-tertiary">{transaction.shares} shares</div>
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="font-semibold text-text-primary">{formatCurrency(transaction.price)}</div>
                      <div className="text-xs text-text-tertiary">{transaction.date} {transaction.time}</div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </AuthenticatedLayout>
  );
};

export default Portfolio;