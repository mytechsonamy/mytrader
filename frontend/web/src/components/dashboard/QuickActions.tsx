import { useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { addNotification } from '../../store/slices/uiSlice';
import { checkHealthAsync } from '../../store/slices/authSlice';
import { fetchAllMarketData } from '../../store/slices/marketSlice';
import AuthPrompt from '../AuthPrompt';
import './QuickActions.css';

interface QuickActionsProps {
  className?: string;
}

const QuickActions: React.FC<QuickActionsProps> = ({ className = '' }) => {
  const dispatch = useAppDispatch();
  const { isLoading: authLoading, isAuthenticated, isGuest } = useAppSelector((state) => state.auth);
  const { isLoading: marketLoading } = useAppSelector((state) => state.market);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [showAuthPrompt, setShowAuthPrompt] = useState(false);

  const handleHealthCheck = async () => {
    setActionLoading('health');
    try {
      await dispatch(checkHealthAsync()).unwrap();
      dispatch(addNotification({
        type: 'success',
        message: 'API health check successful!'
      }));
    } catch (error) {
      dispatch(addNotification({
        type: 'error',
        message: 'API health check failed'
      }));
    } finally {
      setActionLoading(null);
    }
  };

  const handleRefreshMarketData = async () => {
    setActionLoading('market');
    try {
      await dispatch(fetchAllMarketData()).unwrap();
      dispatch(addNotification({
        type: 'success',
        message: 'Market data refreshed successfully!'
      }));
    } catch (error) {
      dispatch(addNotification({
        type: 'error',
        message: 'Failed to refresh market data'
      }));
    } finally {
      setActionLoading(null);
    }
  };

  const handleAuthRequired = (featureName: string) => {
    if (isAuthenticated) {
      dispatch(addNotification({
        type: 'info',
        message: `${featureName} feature coming soon!`
      }));
    } else {
      setShowAuthPrompt(true);
    }
  };

  const handleCloseAuthPrompt = () => {
    setShowAuthPrompt(false);
  };

  const handleViewPortfolio = () => {
    handleAuthRequired('Portfolio');
  };

  const handleViewStrategies = () => {
    handleAuthRequired('Trading strategies');
  };

  const handleStartTrading = () => {
    handleAuthRequired('Trading');
  };

  const handleViewAnalytics = () => {
    handleAuthRequired('Analytics');
  };

  return (
    <>
      <div className={`quick-actions ${className}`}>
        <h3>{isAuthenticated ? 'Quick Actions' : 'Available Actions'}</h3>
        <div className="actions-grid">
          {/* Always available actions */}
          <button
            className="action-btn primary"
            onClick={handleRefreshMarketData}
            disabled={marketLoading || actionLoading === 'market'}
          >
            <span className="icon">📈</span>
            <span className="text">Refresh Market Data</span>
            {actionLoading === 'market' && <div className="mini-spinner"></div>}
          </button>

          <button
            className="action-btn utility"
            onClick={handleHealthCheck}
            disabled={authLoading || actionLoading === 'health'}
          >
            <span className="icon">🔄</span>
            <span className="text">Health Check</span>
            {actionLoading === 'health' && <div className="mini-spinner"></div>}
          </button>

          {/* Authentication-aware actions */}
          {isAuthenticated ? (
            <>
              <button
                className="action-btn secondary"
                onClick={handleViewPortfolio}
              >
                <span className="icon">💼</span>
                <span className="text">View Portfolio</span>
              </button>

              <button
                className="action-btn secondary"
                onClick={handleViewStrategies}
              >
                <span className="icon">🎯</span>
                <span className="text">Trading Strategies</span>
              </button>
            </>
          ) : (
            <>
              <button
                className="action-btn auth-required"
                onClick={handleStartTrading}
              >
                <span className="icon">💼</span>
                <span className="text">Start Trading</span>
                <span className="auth-indicator">🔐</span>
              </button>

              <button
                className="action-btn auth-required"
                onClick={handleViewAnalytics}
              >
                <span className="icon">📊</span>
                <span className="text">View Analytics</span>
                <span className="auth-indicator">🔐</span>
              </button>

              <button
                className="action-btn auth-required"
                onClick={handleViewPortfolio}
              >
                <span className="icon">🎯</span>
                <span className="text">Portfolio Management</span>
                <span className="auth-indicator">🔐</span>
              </button>
            </>
          )}
        </div>

        {/* Guest mode information */}
        {isGuest && (
          <div className="guest-info">
            <p className="guest-message">
              <span className="info-icon">ℹ️</span>
              Sign in to unlock trading features and portfolio management.
            </p>
          </div>
        )}
      </div>

      <AuthPrompt
        isOpen={showAuthPrompt}
        onClose={handleCloseAuthPrompt}
        title="Trading Features"
        message="Sign in to access trading features, portfolio management, and advanced analytics."
      />
    </>
  );
};

export default QuickActions;