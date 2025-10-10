/**
 * Alerts page - Price alerts and notifications management
 */

import React, { useState } from 'react';
import { Card, CardHeader, CardTitle, CardContent, Button, Badge } from '../components/ui';
import AuthenticatedLayout from '../components/layout/AuthenticatedLayout';
import { formatCurrency, formatPercentage, cn } from '../utils';

interface Alert {
  id: string;
  symbol: string;
  name: string;
  type: 'price' | 'percentage' | 'volume';
  condition: 'above' | 'below';
  targetValue: number;
  currentValue: number;
  isActive: boolean;
  isTriggered: boolean;
  createdAt: string;
  triggeredAt?: string;
}

const Alerts: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'active' | 'triggered' | 'all'>('active');
  const [showCreateModal, setShowCreateModal] = useState(false);

  // Mock data - replace with actual API calls
  const mockAlerts: Alert[] = [
    {
      id: '1',
      symbol: 'AAPL',
      name: 'Apple Inc.',
      type: 'price',
      condition: 'above',
      targetValue: 180.00,
      currentValue: 175.50,
      isActive: true,
      isTriggered: false,
      createdAt: '2024-01-10T10:30:00Z'
    },
    {
      id: '2',
      symbol: 'TSLA',
      name: 'Tesla Inc.',
      type: 'percentage',
      condition: 'below',
      targetValue: -5.0,
      currentValue: -2.3,
      isActive: true,
      isTriggered: false,
      createdAt: '2024-01-09T14:15:00Z'
    },
    {
      id: '3',
      symbol: 'NVDA',
      name: 'NVIDIA Corp.',
      type: 'price',
      condition: 'above',
      targetValue: 900.00,
      currentValue: 890.75,
      isActive: false,
      isTriggered: true,
      createdAt: '2024-01-08T09:45:00Z',
      triggeredAt: '2024-01-12T11:22:00Z'
    },
    {
      id: '4',
      symbol: 'MSFT',
      name: 'Microsoft Corp.',
      type: 'volume',
      condition: 'above',
      targetValue: 50000000,
      currentValue: 35000000,
      isActive: true,
      isTriggered: false,
      createdAt: '2024-01-07T16:20:00Z'
    }
  ];

  const filteredAlerts = mockAlerts.filter(alert => {
    switch (activeTab) {
      case 'active':
        return alert.isActive && !alert.isTriggered;
      case 'triggered':
        return alert.isTriggered;
      case 'all':
      default:
        return true;
    }
  });

  const renderAlertIcon = (type: Alert['type']) => {
    switch (type) {
      case 'price':
        return '💰';
      case 'percentage':
        return '📊';
      case 'volume':
        return '📈';
      default:
        return '🔔';
    }
  };

  const renderAlertValue = (alert: Alert) => {
    switch (alert.type) {
      case 'price':
        return formatCurrency(alert.targetValue);
      case 'percentage':
        return formatPercentage(alert.targetValue);
      case 'volume':
        return `${(alert.targetValue / 1000000).toFixed(1)}M`;
      default:
        return alert.targetValue.toString();
    }
  };

  const renderCurrentValue = (alert: Alert) => {
    switch (alert.type) {
      case 'price':
        return formatCurrency(alert.currentValue);
      case 'percentage':
        return formatPercentage(alert.currentValue);
      case 'volume':
        return `${(alert.currentValue / 1000000).toFixed(1)}M`;
      default:
        return alert.currentValue.toString();
    }
  };

  const getAlertProgress = (alert: Alert) => {
    if (alert.isTriggered) return 100;

    const current = alert.currentValue;
    const target = alert.targetValue;

    if (alert.condition === 'above') {
      return Math.min((current / target) * 100, 100);
    } else {
      return Math.min((target / current) * 100, 100);
    }
  };

  const renderAlertCard = (alert: Alert) => (
    <Card key={alert.id} className={cn(
      'transition-all duration-200',
      alert.isTriggered ? 'border-positive-300 bg-positive-50' : '',
      !alert.isActive && !alert.isTriggered ? 'opacity-60' : ''
    )}>
      <CardContent className="p-4">
        <div className="flex items-start justify-between mb-3">
          <div className="flex items-center space-x-3">
            <div className="text-2xl">{renderAlertIcon(alert.type)}</div>
            <div>
              <h3 className="font-semibold text-text-primary">{alert.symbol}</h3>
              <p className="text-sm text-text-tertiary">{alert.name}</p>
            </div>
          </div>
          <div className="flex items-center space-x-2">
            {alert.isTriggered && (
              <Badge variant="success" className="text-xs">
                Triggered
              </Badge>
            )}
            {alert.isActive && !alert.isTriggered && (
              <Badge variant="default" className="text-xs">
                Active
              </Badge>
            )}
            {!alert.isActive && !alert.isTriggered && (
              <Badge variant="secondary" className="text-xs">
                Inactive
              </Badge>
            )}
          </div>
        </div>

        <div className="space-y-3">
          {/* Alert Condition */}
          <div className="flex items-center justify-between">
            <span className="text-sm text-text-tertiary">
              When {alert.type} goes {alert.condition}
            </span>
            <span className="font-semibold text-text-primary">
              {renderAlertValue(alert)}
            </span>
          </div>

          {/* Current Value */}
          <div className="flex items-center justify-between">
            <span className="text-sm text-text-tertiary">Current {alert.type}</span>
            <span className="font-medium text-text-primary">
              {renderCurrentValue(alert)}
            </span>
          </div>

          {/* Progress Bar */}
          {!alert.isTriggered && (
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div
                className={cn(
                  'h-2 rounded-full transition-all duration-300',
                  getAlertProgress(alert) >= 80 ? 'bg-orange-500' : 'bg-brand-500'
                )}
                style={{ width: `${Math.min(getAlertProgress(alert), 100)}%` }}
              ></div>
            </div>
          )}

          {/* Timestamps */}
          <div className="flex items-center justify-between text-xs text-text-tertiary">
            <span>Created {new Date(alert.createdAt).toLocaleDateString()}</span>
            {alert.triggeredAt && (
              <span>Triggered {new Date(alert.triggeredAt).toLocaleDateString()}</span>
            )}
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end space-x-2 pt-2">
            {alert.isActive && !alert.isTriggered && (
              <Button variant="secondary" size="sm">
                Pause
              </Button>
            )}
            {!alert.isActive && !alert.isTriggered && (
              <Button variant="primary" size="sm">
                Resume
              </Button>
            )}
            <Button variant="secondary" size="sm">
              Edit
            </Button>
            <Button variant="destructive" size="sm">
              Delete
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderCreateAlertForm = () => (
    <Card>
      <CardHeader>
        <CardTitle>Create New Alert</CardTitle>
        <p className="text-text-tertiary">Set up price, percentage, or volume alerts</p>
      </CardHeader>
      <CardContent>
        <form className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Symbol
              </label>
              <input
                type="text"
                placeholder="e.g., AAPL"
                className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Alert Type
              </label>
              <select className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                <option value="price">Price Alert</option>
                <option value="percentage">Percentage Change</option>
                <option value="volume">Volume Alert</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Condition
              </label>
              <select className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                <option value="above">Goes Above</option>
                <option value="below">Goes Below</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Target Value
              </label>
              <input
                type="number"
                step="0.01"
                placeholder="0.00"
                className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
          </div>

          <div className="flex items-center justify-end space-x-2 pt-4">
            <Button variant="secondary" onClick={() => setShowCreateModal(false)}>
              Cancel
            </Button>
            <Button variant="primary">
              Create Alert
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );

  return (
    <AuthenticatedLayout>
      <div className="space-y-6">
        {/* Page Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-text-primary">Alerts</h1>
            <p className="text-text-tertiary mt-1">
              Manage your price alerts and notifications
            </p>
          </div>
          <Button
            variant="primary"
            onClick={() => setShowCreateModal(!showCreateModal)}
          >
            + Create Alert
          </Button>
        </div>

        {/* Statistics */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="p-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-text-primary">{mockAlerts.filter(a => a.isActive).length}</div>
                <div className="text-sm text-text-tertiary">Active Alerts</div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-positive-500">{mockAlerts.filter(a => a.isTriggered).length}</div>
                <div className="text-sm text-text-tertiary">Triggered Today</div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-orange-500">
                  {mockAlerts.filter(a => a.isActive && getAlertProgress(a) >= 80).length}
                </div>
                <div className="text-sm text-text-tertiary">Close to Target</div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-text-primary">{mockAlerts.length}</div>
                <div className="text-sm text-text-tertiary">Total Alerts</div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Create Alert Form (Conditional) */}
        {showCreateModal && renderCreateAlertForm()}

        {/* Tabs */}
        <div className="flex items-center space-x-4 border-b border-border-default">
          {[
            { key: 'active', label: 'Active', count: mockAlerts.filter(a => a.isActive && !a.isTriggered).length },
            { key: 'triggered', label: 'Triggered', count: mockAlerts.filter(a => a.isTriggered).length },
            { key: 'all', label: 'All', count: mockAlerts.length }
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
              {tab.label} ({tab.count})
            </button>
          ))}
        </div>

        {/* Alerts Grid */}
        {filteredAlerts.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {filteredAlerts.map(renderAlertCard)}
          </div>
        ) : (
          <Card>
            <CardContent className="text-center py-12">
              <div className="text-6xl mb-4">🔔</div>
              <h3 className="text-xl font-semibold text-text-primary mb-2">
                No alerts found
              </h3>
              <p className="text-text-tertiary mb-6">
                {activeTab === 'active' && "You don't have any active alerts. Create one to get started."}
                {activeTab === 'triggered' && "No alerts have been triggered recently."}
                {activeTab === 'all' && "You haven't created any alerts yet. Start monitoring your favorite symbols."}
              </p>
              <Button
                variant="primary"
                onClick={() => setShowCreateModal(true)}
              >
                Create Your First Alert
              </Button>
            </CardContent>
          </Card>
        )}
      </div>
    </AuthenticatedLayout>
  );
};

export default Alerts;