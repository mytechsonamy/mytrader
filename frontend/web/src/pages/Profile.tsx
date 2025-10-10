/**
 * Profile page - User profile management and settings
 */

import React, { useState } from 'react';
import { Card, CardHeader, CardTitle, CardContent, Button, Badge } from '../components/ui';
import AuthenticatedLayout from '../components/layout/AuthenticatedLayout';
import { useAuthStore } from '../store/authStore';
import { formatCurrency, formatPercentage, cn } from '../utils';

const Profile: React.FC = () => {
  const { user, updateUser } = useAuthStore();
  const [activeTab, setActiveTab] = useState<'profile' | 'settings' | 'security' | 'preferences'>('profile');
  const [isEditing, setIsEditing] = useState(false);

  const [formData, setFormData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    phone: user?.phone || '',
    bio: user?.bio || '',
    location: user?.location || '',
    website: user?.website || '',
    timezone: user?.timezone || 'America/New_York'
  });

  const handleSave = () => {
    updateUser(formData);
    setIsEditing(false);
  };

  const renderProfileTab = () => (
    <div className="space-y-6">
      {/* Profile Header */}
      <Card>
        <CardContent className="p-6">
          <div className="flex items-start space-x-6">
            <div className="flex-shrink-0">
              <div className="w-24 h-24 bg-brand-100 text-brand-600 rounded-full flex items-center justify-center text-3xl font-bold">
                {user?.firstName?.charAt(0) || 'U'}
              </div>
            </div>
            <div className="flex-1">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="text-2xl font-bold text-text-primary">
                    {user?.firstName} {user?.lastName}
                  </h2>
                  <p className="text-text-tertiary">@{user?.username}</p>
                </div>
                <Button
                  variant={isEditing ? "primary" : "secondary"}
                  onClick={() => isEditing ? handleSave() : setIsEditing(true)}
                >
                  {isEditing ? 'Save Changes' : 'Edit Profile'}
                </Button>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-text-tertiary mb-1">
                    Email
                  </label>
                  {isEditing ? (
                    <input
                      type="email"
                      value={formData.email}
                      onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                      className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                    />
                  ) : (
                    <p className="text-text-primary">{user?.email}</p>
                  )}
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-tertiary mb-1">
                    Phone
                  </label>
                  {isEditing ? (
                    <input
                      type="tel"
                      value={formData.phone}
                      onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                      className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                    />
                  ) : (
                    <p className="text-text-primary">{user?.phone || 'Not provided'}</p>
                  )}
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-tertiary mb-1">
                    Location
                  </label>
                  {isEditing ? (
                    <input
                      type="text"
                      value={formData.location}
                      onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                      className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                    />
                  ) : (
                    <p className="text-text-primary">{user?.location || 'Not provided'}</p>
                  )}
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-tertiary mb-1">
                    Website
                  </label>
                  {isEditing ? (
                    <input
                      type="url"
                      value={formData.website}
                      onChange={(e) => setFormData({ ...formData, website: e.target.value })}
                      className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                    />
                  ) : (
                    <p className="text-text-primary">{user?.website || 'Not provided'}</p>
                  )}
                </div>
              </div>

              <div className="mt-4">
                <label className="block text-sm font-medium text-text-tertiary mb-1">
                  Bio
                </label>
                {isEditing ? (
                  <textarea
                    value={formData.bio}
                    onChange={(e) => setFormData({ ...formData, bio: e.target.value })}
                    rows={3}
                    className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                    placeholder="Tell us about yourself..."
                  />
                ) : (
                  <p className="text-text-primary">{user?.bio || 'No bio provided'}</p>
                )}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Trading Statistics */}
      <Card>
        <CardHeader>
          <CardTitle>Trading Statistics</CardTitle>
          <p className="text-text-tertiary">Your trading performance overview</p>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <div className="text-center">
              <div className="text-2xl font-bold text-positive-500">+24.8%</div>
              <div className="text-sm text-text-tertiary">Total Return</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-text-primary">342</div>
              <div className="text-sm text-text-tertiary">Total Trades</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-text-primary">68.4%</div>
              <div className="text-sm text-text-tertiary">Win Rate</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-text-primary">1.64</div>
              <div className="text-sm text-text-tertiary">Sharpe Ratio</div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
          <p className="text-text-tertiary">Your latest trading activities</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[
              { type: 'trade', action: 'BUY', symbol: 'AAPL', amount: 100, price: 175.50, date: '2024-01-15', time: '10:30 AM' },
              { type: 'alert', message: 'Price alert triggered for TSLA', date: '2024-01-14', time: '2:15 PM' },
              { type: 'strategy', action: 'PAUSED', name: 'Tech Momentum Strategy', date: '2024-01-13', time: '4:20 PM' },
              { type: 'competition', action: 'JOINED', name: 'Weekly Stock Challenge', date: '2024-01-12', time: '9:00 AM' },
            ].map((activity, index) => (
              <div key={index} className="flex items-center justify-between p-3 rounded-lg hover:bg-background-secondary">
                <div className="flex items-center space-x-3">
                  <div className={cn(
                    'w-8 h-8 rounded-full flex items-center justify-center text-xs font-medium',
                    activity.type === 'trade' ? 'bg-blue-100 text-blue-600' :
                    activity.type === 'alert' ? 'bg-orange-100 text-orange-600' :
                    activity.type === 'strategy' ? 'bg-purple-100 text-purple-600' :
                    'bg-green-100 text-green-600'
                  )}>
                    {activity.type === 'trade' ? '📈' :
                     activity.type === 'alert' ? '🔔' :
                     activity.type === 'strategy' ? '🎯' : '🏆'}
                  </div>
                  <div>
                    <div className="font-medium text-text-primary">
                      {activity.type === 'trade' && `${activity.action} ${activity.amount} ${activity.symbol} @ ${formatCurrency(activity.price!)}`}
                      {activity.type === 'alert' && activity.message}
                      {activity.type === 'strategy' && `${activity.action} strategy: ${activity.name}`}
                      {activity.type === 'competition' && `${activity.action} competition: ${activity.name}`}
                    </div>
                    <div className="text-sm text-text-tertiary">{activity.date} at {activity.time}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );

  const renderSettingsTab = () => (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Account Settings</CardTitle>
          <p className="text-text-tertiary">Manage your account preferences</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Timezone
              </label>
              <select
                value={formData.timezone}
                onChange={(e) => setFormData({ ...formData, timezone: e.target.value })}
                className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
              >
                <option value="America/New_York">Eastern Time (ET)</option>
                <option value="America/Chicago">Central Time (CT)</option>
                <option value="America/Denver">Mountain Time (MT)</option>
                <option value="America/Los_Angeles">Pacific Time (PT)</option>
                <option value="Europe/London">London (GMT)</option>
                <option value="Europe/Paris">Paris (CET)</option>
                <option value="Asia/Tokyo">Tokyo (JST)</option>
              </select>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-text-primary">Email Notifications</h4>
                <p className="text-sm text-text-tertiary">Receive email notifications for important updates</p>
              </div>
              <label className="flex items-center">
                <input type="checkbox" className="form-checkbox h-4 w-4 text-brand-600" defaultChecked />
              </label>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-text-primary">Push Notifications</h4>
                <p className="text-sm text-text-tertiary">Get push notifications on your device</p>
              </div>
              <label className="flex items-center">
                <input type="checkbox" className="form-checkbox h-4 w-4 text-brand-600" defaultChecked />
              </label>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-text-primary">SMS Alerts</h4>
                <p className="text-sm text-text-tertiary">Receive SMS alerts for critical events</p>
              </div>
              <label className="flex items-center">
                <input type="checkbox" className="form-checkbox h-4 w-4 text-brand-600" />
              </label>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Privacy Settings</CardTitle>
          <p className="text-text-tertiary">Control your privacy and data sharing</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-text-primary">Public Profile</h4>
                <p className="text-sm text-text-tertiary">Make your profile visible to other users</p>
              </div>
              <label className="flex items-center">
                <input type="checkbox" className="form-checkbox h-4 w-4 text-brand-600" defaultChecked />
              </label>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-text-primary">Show Trading Stats</h4>
                <p className="text-sm text-text-tertiary">Display your trading performance publicly</p>
              </div>
              <label className="flex items-center">
                <input type="checkbox" className="form-checkbox h-4 w-4 text-brand-600" />
              </label>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-text-primary">Activity Status</h4>
                <p className="text-sm text-text-tertiary">Show when you're online</p>
              </div>
              <label className="flex items-center">
                <input type="checkbox" className="form-checkbox h-4 w-4 text-brand-600" defaultChecked />
              </label>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );

  const renderSecurityTab = () => (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Password & Security</CardTitle>
          <p className="text-text-tertiary">Keep your account secure</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <h4 className="font-medium text-text-primary mb-2">Change Password</h4>
              <div className="space-y-3">
                <input
                  type="password"
                  placeholder="Current password"
                  className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
                <input
                  type="password"
                  placeholder="New password"
                  className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
                <input
                  type="password"
                  placeholder="Confirm new password"
                  className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
                <Button variant="primary">Update Password</Button>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Two-Factor Authentication</CardTitle>
          <p className="text-text-tertiary">Add an extra layer of security to your account</p>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <h4 className="font-medium text-text-primary">2FA Status</h4>
              <p className="text-sm text-text-tertiary">Two-factor authentication is currently disabled</p>
            </div>
            <Button variant="primary">Enable 2FA</Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Active Sessions</CardTitle>
          <p className="text-text-tertiary">Manage your active login sessions</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {[
              { device: 'MacBook Pro', location: 'New York, NY', lastActive: '2 minutes ago', current: true },
              { device: 'iPhone 14', location: 'New York, NY', lastActive: '1 hour ago', current: false },
              { device: 'Chrome Browser', location: 'Boston, MA', lastActive: '2 days ago', current: false },
            ].map((session, index) => (
              <div key={index} className="flex items-center justify-between p-3 rounded-lg border border-border-default">
                <div>
                  <div className="font-medium text-text-primary">{session.device}</div>
                  <div className="text-sm text-text-tertiary">{session.location} • {session.lastActive}</div>
                </div>
                <div className="flex items-center space-x-2">
                  {session.current && (
                    <Badge variant="success" className="text-xs">Current</Badge>
                  )}
                  {!session.current && (
                    <Button variant="destructive" size="sm">Revoke</Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );

  const renderPreferencesTab = () => (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Trading Preferences</CardTitle>
          <p className="text-text-tertiary">Customize your trading experience</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Default Order Type
              </label>
              <select className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                <option>Market Order</option>
                <option>Limit Order</option>
                <option>Stop Order</option>
                <option>Stop-Limit Order</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Default Position Size
              </label>
              <select className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                <option>1% of portfolio</option>
                <option>2% of portfolio</option>
                <option>5% of portfolio</option>
                <option>10% of portfolio</option>
                <option>Custom amount</option>
              </select>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-text-primary">Confirm Orders</h4>
                <p className="text-sm text-text-tertiary">Require confirmation before placing orders</p>
              </div>
              <label className="flex items-center">
                <input type="checkbox" className="form-checkbox h-4 w-4 text-brand-600" defaultChecked />
              </label>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-text-primary">Sound Alerts</h4>
                <p className="text-sm text-text-tertiary">Play sounds for alerts and notifications</p>
              </div>
              <label className="flex items-center">
                <input type="checkbox" className="form-checkbox h-4 w-4 text-brand-600" defaultChecked />
              </label>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Display Preferences</CardTitle>
          <p className="text-text-tertiary">Customize how information is displayed</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Theme
              </label>
              <select className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                <option>Light</option>
                <option>Dark</option>
                <option>Auto (System)</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Currency Display
              </label>
              <select className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                <option>USD ($)</option>
                <option>EUR (€)</option>
                <option>GBP (£)</option>
                <option>JPY (¥)</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">
                Number Format
              </label>
              <select className="w-full px-3 py-2 border border-border-default rounded-md focus:outline-none focus:ring-2 focus:ring-brand-500">
                <option>1,234.56</option>
                <option>1.234,56</option>
                <option>1 234.56</option>
              </select>
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
            <h1 className="text-3xl font-bold text-text-primary">Profile</h1>
            <p className="text-text-tertiary mt-1">
              Manage your account settings and preferences
            </p>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex items-center space-x-4 border-b border-border-default">
          {[
            { key: 'profile', label: 'Profile' },
            { key: 'settings', label: 'Settings' },
            { key: 'security', label: 'Security' },
            { key: 'preferences', label: 'Preferences' }
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
        {activeTab === 'profile' && renderProfileTab()}
        {activeTab === 'settings' && renderSettingsTab()}
        {activeTab === 'security' && renderSecurityTab()}
        {activeTab === 'preferences' && renderPreferencesTab()}
      </div>
    </AuthenticatedLayout>
  );
};

export default Profile;