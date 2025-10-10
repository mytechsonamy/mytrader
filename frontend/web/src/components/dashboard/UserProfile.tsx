import React, { useState, useEffect } from 'react';
import { useAuthStore } from '../../store/authStore';
import { marketDataService, type ConnectionStatus } from '../../services/marketDataService';
import AuthPrompt from '../AuthPrompt';
import type { User } from '../../types';
import './UserProfile.css';

interface UserProfileProps {
  className?: string;
  user?: User;
  variant?: 'default' | 'sidebar' | 'card';
}

const UserProfile: React.FC<UserProfileProps> = ({
  className = '',
  user: propUser,
  variant = 'default'
}) => {
  const { user: storeUser, isAuthenticated, isGuest } = useAuthStore();
  const user = propUser || storeUser;
  const [showAuthPrompt, setShowAuthPrompt] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus | null>(null);
  const [portfolioValue] = useState(0); // Mock portfolio value for now
  const [activePositions] = useState(0); // Mock positions for now

  // Fetch connection status for health indicators
  useEffect(() => {
    const fetchConnectionStatus = async () => {
      try {
        const status = await marketDataService.getDataSourceHealth();
        setConnectionStatus(status);
      } catch (error) {
        console.error('Failed to fetch connection status:', error);
      }
    };

    fetchConnectionStatus();
    const interval = setInterval(fetchConnectionStatus, 30000); // Update every 30 seconds
    return () => clearInterval(interval);
  }, []);

  const handleLoginPrompt = () => {
    setShowAuthPrompt(true);
  };

  const handleCloseAuthPrompt = () => {
    setShowAuthPrompt(false);
  };

  const getMarketSessionStatus = () => {
    const now = new Date();
    const hour = now.getHours();
    const isWeekday = now.getDay() >= 1 && now.getDay() <= 5;

    if (isWeekday && hour >= 9 && hour < 16) {
      return { status: 'open', label: 'Market Open' };
    } else if (isWeekday && ((hour >= 4 && hour < 9) || (hour >= 16 && hour < 20))) {
      return { status: 'pre-post', label: 'Extended Hours' };
    } else {
      return { status: 'closed', label: 'Market Closed' };
    }
  };

  const getConnectionStatusDisplay = () => {
    if (!connectionStatus) return { status: 'connecting', label: 'Connecting...' };

    switch (connectionStatus.overall) {
      case 'connected':
        return { status: 'live', label: 'Live Data' };
      case 'partial':
        return { status: 'partial', label: 'Partial' };
      case 'disconnected':
        return { status: 'offline', label: 'Offline' };
      default:
        return { status: 'connecting', label: 'Connecting...' };
    }
  };

  const formatCurrency = (value: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  };

  // Guest mode - show professional sign-in prompt with Bloomberg Terminal aesthetic
  if (isGuest || !isAuthenticated) {
    const marketSession = getMarketSessionStatus();
    const connectionDisplay = getConnectionStatusDisplay();

    return (
      <>
        <div className={`professional-user-profile guest-mode ${className}`}>
          {/* Professional Guest Avatar */}
          <div className="professional-avatar guest-avatar">
            <div className="avatar-image guest">G</div>
            <div className="status-indicator guest"></div>
          </div>

          {/* Trading Status Bar */}
          <div className="trading-status-bar">
            <div className="session-status">
              <div className={`market-session-dot ${marketSession.status}`}></div>
              <span className="status-label">{marketSession.label}</span>
            </div>
            <div className="connection-status">
              <span className={`connection-indicator ${connectionDisplay.status}`}>
                {connectionDisplay.label}
              </span>
            </div>
          </div>

          {/* Guest Account Metrics */}
          <div className="account-metrics guest">
            <div className="metric-item">
              <div className="metric-value locked">--</div>
              <div className="metric-label">Portfolio</div>
            </div>
            <div className="metric-item">
              <div className="metric-value locked">--</div>
              <div className="metric-label">Positions</div>
            </div>
          </div>

          {/* Guest User Info */}
          <div className="user-details guest">
            <h3 className="user-name">Guest Trader</h3>
            <p className="user-status">Limited Access Mode</p>
            <button className="professional-signin-btn" onClick={handleLoginPrompt}>
              <span className="btn-icon">🔑</span>
              Sign In to Trade
            </button>
          </div>

          {/* Feature Access Indicators */}
          <div className="feature-access-grid">
            <div className="access-item available">
              <div className="access-icon">📊</div>
              <span className="access-label">Live Data</span>
              <div className="access-status available"></div>
            </div>
            <div className="access-item locked">
              <div className="access-icon">💼</div>
              <span className="access-label">Portfolio</span>
              <div className="access-status locked"></div>
            </div>
            <div className="access-item locked">
              <div className="access-icon">⚡</div>
              <span className="access-label">Trading</span>
              <div className="access-status locked"></div>
            </div>
          </div>
        </div>

        <AuthPrompt
          isOpen={showAuthPrompt}
          onClose={handleCloseAuthPrompt}
          title="Access Professional Trading Platform"
          message="Sign in to unlock portfolio tracking, advanced analytics, and live trading capabilities."
        />
      </>
    );
  }

  // Professional loading state
  if (!user) {
    const marketSession = getMarketSessionStatus();
    const connectionDisplay = getConnectionStatusDisplay();

    return (
      <div className={`professional-user-profile loading-mode ${className}`}>
        <div className="professional-avatar loading">
          <div className="avatar-image loading">
            <div className="loading-spinner-small"></div>
          </div>
          <div className="status-indicator loading"></div>
        </div>

        <div className="trading-status-bar">
          <div className="session-status">
            <div className={`market-session-dot ${marketSession.status}`}></div>
            <span className="status-label">{marketSession.label}</span>
          </div>
          <div className="connection-status">
            <span className={`connection-indicator ${connectionDisplay.status}`}>
              {connectionDisplay.label}
            </span>
          </div>
        </div>

        <div className="account-metrics loading">
          <div className="metric-item">
            <div className="metric-value loading">--</div>
            <div className="metric-label">Portfolio</div>
          </div>
          <div className="metric-item">
            <div className="metric-value loading">--</div>
            <div className="metric-label">Positions</div>
          </div>
        </div>

        <div className="user-details loading">
          <div className="loading-text">Loading profile...</div>
        </div>
      </div>
    );
  }

  // Professional authenticated user profile
  const marketSession = getMarketSessionStatus();
  const connectionDisplay = getConnectionStatusDisplay();

  return (
    <div className={`professional-user-profile authenticated ${className}`}>
      {/* Professional Avatar with Status */}
      <div className="professional-avatar">
        <div className="avatar-image">
          {user.firstName[0]}{user.lastName[0]}
        </div>
        <div className={`status-indicator ${user.isActive ? 'online' : 'offline'}`}></div>
      </div>

      {/* Trading Status Bar */}
      <div className="trading-status-bar">
        <div className="session-status">
          <div className={`market-session-dot ${marketSession.status}`}></div>
          <span className="status-label">{marketSession.label}</span>
        </div>
        <div className="connection-status">
          <span className={`connection-indicator ${connectionDisplay.status}`}>
            {connectionDisplay.label}
          </span>
        </div>
      </div>

      {/* Account Metrics Dashboard */}
      <div className="account-metrics">
        <div className="metric-item">
          <div className="metric-value">{formatCurrency(portfolioValue)}</div>
          <div className="metric-label">Portfolio</div>
        </div>
        <div className="metric-item">
          <div className="metric-value">{activePositions}</div>
          <div className="metric-label">Positions</div>
        </div>
      </div>

      {/* Professional User Details */}
      <div className="user-details">
        <h3 className="user-name">{user.firstName} {user.lastName}</h3>
        <div className="user-meta">
          <span className={`plan-badge plan-${user.plan}`}>
            {user.plan.toUpperCase()}
          </span>
          <span className={`verification-badge ${user.isEmailVerified ? 'verified' : 'unverified'}`}>
            {user.isEmailVerified ? '✓ Verified' : '⚠ Unverified'}
          </span>
        </div>
        <p className="user-email">{user.email}</p>
      </div>

      {/* Professional Stats Grid */}
      <div className="professional-stats">
        <div className="stat-row">
          <span className="stat-label">Account Status</span>
          <span className={`stat-value ${user.isActive ? 'active' : 'inactive'}`}>
            {user.isActive ? 'Active' : 'Inactive'}
          </span>
        </div>
        {user.lastLogin && (
          <div className="stat-row">
            <span className="stat-label">Last Session</span>
            <span className="stat-value">
              {new Date(user.lastLogin).toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
              })}
            </span>
          </div>
        )}
        <div className="stat-row">
          <span className="stat-label">Member Since</span>
          <span className="stat-value">
            {new Date(user.createdAt).toLocaleDateString('en-US', {
              month: 'short',
              year: 'numeric'
            })}
          </span>
        </div>
      </div>
    </div>
  );
};

export default UserProfile;