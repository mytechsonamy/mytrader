import React, { useEffect } from 'react';
import { useDashboard } from '../../hooks/useDashboard';
import { useAppSelector } from '../../store/hooks';

interface RealTimeStatsProps {
  className?: string;
}

const RealTimeStats: React.FC<RealTimeStatsProps> = ({ className = '' }) => {
  const { isAuthenticated } = useAppSelector(state => state.auth);

  const {
    connected,
    connecting,
    error,
    portfolio,
    signals,
    indicators,
    lastUpdate,
    hasData,
    signalCount,
    indicatorCount,
    requestUpdate,
    testConnection
  } = useDashboard({
    enabled: isAuthenticated, // Only enable for authenticated users
    symbols: ['BTCUSD', 'ETHUSD', 'ADAUSD'], // Default crypto symbols
    timeframes: ['1h', '4h', '1d'],
    autoSubscribeToPortfolio: true,
    minSignalConfidence: 70 // Only high-confidence signals
  });

  // Test connection on mount for debugging
  useEffect(() => {
    if (connected && testConnection) {
      const testTimer = setTimeout(() => {
        testConnection().then(result => {
          console.log('Dashboard connection test result:', result);
        });
      }, 2000);

      return () => clearTimeout(testTimer);
    }
  }, [connected, testConnection]);

  if (!isAuthenticated) {
    return (
      <div className={`real-time-stats guest-mode ${className}`}>
        <div className="stats-header">
          <h3 className="stats-title">
            <span className="stats-icon">📊</span>
            Real-Time Analytics
          </h3>
          <div className="connection-status">
            <div className="status-dot disabled"></div>
            <span>Sign in required</span>
          </div>
        </div>
        <div className="stats-content">
          <div className="auth-prompt">
            <p>Sign in to access real-time portfolio analytics and trading signals</p>
            <div className="preview-features">
              <div className="feature-item">
                <span className="feature-icon">💼</span>
                <span>Live Portfolio Tracking</span>
              </div>
              <div className="feature-item">
                <span className="feature-icon">📈</span>
                <span>Real-Time Indicators</span>
              </div>
              <div className="feature-item">
                <span className="feature-icon">🎯</span>
                <span>Trading Signals</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`real-time-stats ${className}`}>
      <div className="stats-header">
        <h3 className="stats-title">
          <span className="stats-icon">📊</span>
          Real-Time Analytics
        </h3>
        <div className="connection-status">
          {connected && (
            <>
              <div className="status-dot connected"></div>
              <span>Live</span>
            </>
          )}
          {connecting && (
            <>
              <div className="status-dot connecting"></div>
              <span>Connecting...</span>
            </>
          )}
          {!connected && !connecting && (
            <>
              <div className="status-dot disconnected"></div>
              <span>Offline</span>
            </>
          )}
        </div>
      </div>

      <div className="stats-content">
        {error && (
          <div className="error-banner">
            <span className="error-icon">⚠️</span>
            <span>Real-time updates unavailable: {error.message}</span>
          </div>
        )}

        {/* Portfolio Summary */}
        {portfolio && (
          <div className="stat-section portfolio-section">
            <h4 className="section-title">
              <span className="section-icon">💼</span>
              Portfolio Overview
            </h4>
            <div className="portfolio-metrics">
              <div className="metric-row">
                <div className="metric-item">
                  <span className="metric-label">Total Value</span>
                  <span className="metric-value">
                    {new Intl.NumberFormat('en-US', {
                      style: 'currency',
                      currency: 'USD'
                    }).format(portfolio.totalValue)}
                  </span>
                </div>
                <div className="metric-item">
                  <span className="metric-label">Day Change</span>
                  <span className={`metric-value ${portfolio.dayChangePercent >= 0 ? 'positive' : 'negative'}`}>
                    {portfolio.dayChangePercent >= 0 ? '+' : ''}
                    {portfolio.dayChangePercent.toFixed(2)}%
                  </span>
                </div>
              </div>
              <div className="metric-row">
                <div className="metric-item">
                  <span className="metric-label">Active Positions</span>
                  <span className="metric-value">{portfolio.activePositions}</span>
                </div>
                <div className="metric-item">
                  <span className="metric-label">Cash Balance</span>
                  <span className="metric-value">
                    {new Intl.NumberFormat('en-US', {
                      style: 'currency',
                      currency: 'USD'
                    }).format(portfolio.cashBalance)}
                  </span>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Recent Signals */}
        {signals.length > 0 && (
          <div className="stat-section signals-section">
            <h4 className="section-title">
              <span className="section-icon">🎯</span>
              Recent Signals ({signalCount})
            </h4>
            <div className="signals-list">
              {signals.slice(0, 3).map((signal, index) => (
                <div key={index} className={`signal-item ${signal.type.toLowerCase()}`}>
                  <div className="signal-header">
                    <span className="signal-symbol">{signal.symbol}</span>
                    <span className={`signal-type ${signal.type.toLowerCase()}`}>
                      {signal.type}
                    </span>
                    <span className="signal-confidence">{signal.confidence.toFixed(0)}%</span>
                  </div>
                  <div className="signal-details">
                    <span className="signal-price">
                      {new Intl.NumberFormat('en-US', {
                        style: 'currency',
                        currency: 'USD'
                      }).format(signal.price)}
                    </span>
                    <span className="signal-time">
                      {new Date(signal.generatedAt).toLocaleTimeString()}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Indicators Summary */}
        {indicatorCount > 0 && (
          <div className="stat-section indicators-section">
            <h4 className="section-title">
              <span className="section-icon">📈</span>
              Live Indicators ({indicatorCount})
            </h4>
            <div className="indicators-summary">
              {Object.entries(indicators).slice(0, 3).map(([key, indicator]) => (
                <div key={key} className="indicator-item">
                  <div className="indicator-header">
                    <span className="indicator-symbol">{indicator.symbol}</span>
                    <span className="indicator-timeframe">{indicator.timeframe}</span>
                  </div>
                  <div className="indicator-price">
                    {new Intl.NumberFormat('en-US', {
                      style: 'currency',
                      currency: 'USD'
                    }).format(indicator.price)}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* No Data State */}
        {!hasData && connected && (
          <div className="no-data-state">
            <div className="no-data-icon">📊</div>
            <p>Waiting for real-time data...</p>
            <button
              onClick={() => requestUpdate('dashboard')}
              className="refresh-button"
            >
              Request Update
            </button>
          </div>
        )}

        {/* Last Update */}
        {lastUpdate && (
          <div className="stats-footer">
            <span className="last-update">
              Last updated: {new Date(lastUpdate).toLocaleTimeString()}
            </span>
          </div>
        )}
      </div>

      <style jsx>{`
        .real-time-stats {
          background: var(--card-background);
          border-radius: 12px;
          padding: 20px;
          border: 1px solid var(--border-color);
          margin-bottom: 20px;
        }

        .stats-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 16px;
        }

        .stats-title {
          display: flex;
          align-items: center;
          gap: 8px;
          margin: 0;
          font-size: 18px;
          font-weight: 600;
          color: var(--text-primary);
        }

        .stats-icon {
          font-size: 20px;
        }

        .connection-status {
          display: flex;
          align-items: center;
          gap: 8px;
          font-size: 14px;
          color: var(--text-secondary);
        }

        .status-dot {
          width: 8px;
          height: 8px;
          border-radius: 50%;
          background: var(--status-disconnected);
        }

        .status-dot.connected {
          background: var(--status-connected);
          animation: pulse 2s infinite;
        }

        .status-dot.connecting {
          background: var(--status-connecting);
          animation: spin 1s linear infinite;
        }

        .status-dot.disabled {
          background: var(--text-tertiary);
        }

        .error-banner {
          display: flex;
          align-items: center;
          gap: 8px;
          padding: 12px;
          background: var(--error-background);
          border: 1px solid var(--error-border);
          border-radius: 8px;
          margin-bottom: 16px;
          font-size: 14px;
          color: var(--error-text);
        }

        .stat-section {
          margin-bottom: 20px;
        }

        .section-title {
          display: flex;
          align-items: center;
          gap: 8px;
          margin: 0 0 12px 0;
          font-size: 16px;
          font-weight: 600;
          color: var(--text-primary);
        }

        .section-icon {
          font-size: 16px;
        }

        .portfolio-metrics .metric-row {
          display: flex;
          gap: 20px;
          margin-bottom: 8px;
        }

        .metric-item {
          flex: 1;
        }

        .metric-label {
          display: block;
          font-size: 12px;
          color: var(--text-tertiary);
          margin-bottom: 4px;
        }

        .metric-value {
          display: block;
          font-size: 16px;
          font-weight: 600;
          color: var(--text-primary);
        }

        .metric-value.positive {
          color: var(--success-color);
        }

        .metric-value.negative {
          color: var(--error-color);
        }

        .signal-item {
          padding: 12px;
          border-radius: 8px;
          margin-bottom: 8px;
          border-left: 4px solid var(--border-color);
        }

        .signal-item.buy {
          border-left-color: var(--success-color);
          background: var(--success-background);
        }

        .signal-item.sell {
          border-left-color: var(--error-color);
          background: var(--error-background);
        }

        .signal-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 4px;
        }

        .signal-symbol {
          font-weight: 600;
          color: var(--text-primary);
        }

        .signal-type {
          font-size: 12px;
          padding: 2px 6px;
          border-radius: 4px;
          text-transform: uppercase;
          font-weight: 600;
        }

        .signal-type.buy {
          background: var(--success-color);
          color: white;
        }

        .signal-type.sell {
          background: var(--error-color);
          color: white;
        }

        .signal-confidence {
          font-size: 14px;
          color: var(--text-secondary);
        }

        .signal-details {
          display: flex;
          justify-content: space-between;
          align-items: center;
          font-size: 14px;
          color: var(--text-secondary);
        }

        .indicator-item {
          padding: 8px 12px;
          background: var(--surface-secondary);
          border-radius: 6px;
          margin-bottom: 8px;
        }

        .indicator-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 4px;
        }

        .indicator-symbol {
          font-weight: 600;
          color: var(--text-primary);
        }

        .indicator-timeframe {
          font-size: 12px;
          color: var(--text-tertiary);
        }

        .indicator-price {
          font-size: 16px;
          font-weight: 600;
          color: var(--text-primary);
        }

        .no-data-state {
          text-align: center;
          padding: 40px 20px;
          color: var(--text-secondary);
        }

        .no-data-icon {
          font-size: 48px;
          margin-bottom: 16px;
        }

        .refresh-button {
          margin-top: 16px;
          padding: 8px 16px;
          background: var(--primary-color);
          color: white;
          border: none;
          border-radius: 6px;
          cursor: pointer;
          font-size: 14px;
        }

        .refresh-button:hover {
          background: var(--primary-hover);
        }

        .stats-footer {
          text-align: center;
          padding-top: 16px;
          border-top: 1px solid var(--border-color);
          font-size: 12px;
          color: var(--text-tertiary);
        }

        .auth-prompt {
          text-align: center;
          padding: 20px;
        }

        .preview-features {
          margin-top: 20px;
        }

        .feature-item {
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 8px;
          margin-bottom: 8px;
          color: var(--text-secondary);
        }

        .feature-icon {
          font-size: 16px;
        }

        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }

        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
};

export default RealTimeStats;