import React, { useState, useEffect } from 'react';
import { useWebSocket } from '../../hooks/useWebSocket';
import { useDashboard } from '../../hooks/useDashboard';
import { useHealthStatus } from '../../hooks/useHealthStatus';
import { useAppSelector } from '../../store/hooks';
import { enhancedSignalRService } from '../../services/signalRService';

interface ConnectionStatusProps {
  className?: string;
  showDetails?: boolean;
  showTestButtons?: boolean;
}

const ConnectionStatus: React.FC<ConnectionStatusProps> = ({
  className = '',
  showDetails = true,
  showTestButtons = false
}) => {
  const { isAuthenticated } = useAppSelector(state => state.auth);
  const [expanded, setExpanded] = useState(false);
  const [testResults, setTestResults] = useState<Record<string, any>>({});

  // Get connection states from various hooks
  const {
    connected: websocketConnected,
    connecting: websocketConnecting,
    error: websocketError,
    connectionState: websocketState,
    testConnection: testWebSocketConnection
  } = useWebSocket({
    enabled: true,
    useEnhancedService: true
  });

  const {
    connected: dashboardConnected,
    connecting: dashboardConnecting,
    error: dashboardError,
    connectionState: dashboardState,
    testConnection: testDashboardConnection
  } = useDashboard({
    enabled: isAuthenticated
  });

  const {
    isHealthy: apiHealthy,
    isChecking: apiChecking,
    overallStatus,
    alpacaHealthy,
    bistHealthy,
    apiHealth
  } = useHealthStatus({
    enabled: true
  });

  // Calculate overall connection status
  const getOverallStatus = () => {
    if (websocketConnecting || dashboardConnecting || apiChecking) {
      return 'connecting';
    }

    if (websocketConnected && dashboardConnected && apiHealthy) {
      return 'fully-connected';
    }

    if (websocketConnected || dashboardConnected || apiHealthy) {
      return 'partially-connected';
    }

    return 'disconnected';
  };

  const overallConnectionStatus = getOverallStatus();

  // Get status color and icon
  const getStatusDisplay = (status: string) => {
    switch (status) {
      case 'fully-connected':
        return { color: 'success', icon: '🟢', text: 'All Systems Online' };
      case 'partially-connected':
        return { color: 'warning', icon: '🟡', text: 'Partial Connection' };
      case 'connecting':
        return { color: 'info', icon: '🔄', text: 'Connecting...' };
      case 'disconnected':
      default:
        return { color: 'error', icon: '🔴', text: 'Offline' };
    }
  };

  const statusDisplay = getStatusDisplay(overallConnectionStatus);

  // Test individual connections
  const runConnectionTests = async () => {
    const results: Record<string, any> = {};

    try {
      if (testWebSocketConnection) {
        results.websocket = await testWebSocketConnection();
      }
    } catch (error) {
      results.websocket = { success: false, error: (error as Error).message };
    }

    try {
      if (testDashboardConnection && isAuthenticated) {
        results.dashboard = await testDashboardConnection();
      }
    } catch (error) {
      results.dashboard = { success: false, error: (error as Error).message };
    }

    try {
      results.signalr = {
        market: enhancedSignalRService.getMarketConnectionState(),
        dashboard: enhancedSignalRService.getDashboardConnectionState()
      };
    } catch (error) {
      results.signalr = { error: (error as Error).message };
    }

    setTestResults(results);
  };

  // Auto-refresh test results when connections change
  useEffect(() => {
    if (showTestButtons && (websocketConnected || dashboardConnected)) {
      const refreshTimer = setTimeout(runConnectionTests, 1000);
      return () => clearTimeout(refreshTimer);
    }
  }, [websocketConnected, dashboardConnected, showTestButtons]);

  return (
    <div className={`connection-status-widget ${className}`}>
      <div
        className={`status-header ${expanded ? 'expanded' : ''}`}
        onClick={() => setExpanded(!expanded)}
      >
        <div className="status-main">
          <span className="status-icon">{statusDisplay.icon}</span>
          <span className="status-text">{statusDisplay.text}</span>
        </div>
        {showDetails && (
          <div className="status-actions">
            <span className="expand-indicator">
              {expanded ? '▲' : '▼'}
            </span>
          </div>
        )}
      </div>

      {expanded && showDetails && (
        <div className="status-details">
          <div className="connection-grid">
            {/* Market Data WebSocket */}
            <div className="connection-item">
              <div className="connection-header">
                <span className="connection-name">Market Data</span>
                <span className={`connection-status ${websocketConnected ? 'connected' : websocketConnecting ? 'connecting' : 'disconnected'}`}>
                  {websocketConnected ? '🟢' : websocketConnecting ? '🟡' : '🔴'}
                  {websocketState}
                </span>
              </div>
              {websocketError && (
                <div className="connection-error">
                  ⚠️ {websocketError.message}
                </div>
              )}
            </div>

            {/* Dashboard Hub */}
            {isAuthenticated && (
              <div className="connection-item">
                <div className="connection-header">
                  <span className="connection-name">Dashboard Hub</span>
                  <span className={`connection-status ${dashboardConnected ? 'connected' : dashboardConnecting ? 'connecting' : 'disconnected'}`}>
                    {dashboardConnected ? '🟢' : dashboardConnecting ? '🟡' : '🔴'}
                    {dashboardState}
                  </span>
                </div>
                {dashboardError && (
                  <div className="connection-error">
                    ⚠️ {dashboardError.message}
                  </div>
                )}
              </div>
            )}

            {/* API Health */}
            <div className="connection-item">
              <div className="connection-header">
                <span className="connection-name">API Services</span>
                <span className={`connection-status ${apiHealthy ? 'connected' : apiChecking ? 'connecting' : 'disconnected'}`}>
                  {apiHealthy ? '🟢' : apiChecking ? '🟡' : '🔴'}
                  {overallStatus}
                </span>
              </div>
              <div className="api-breakdown">
                <div className="api-service">
                  <span>Alpaca:</span>
                  <span className={alpacaHealthy ? 'healthy' : 'unhealthy'}>
                    {alpacaHealthy ? '✅' : '❌'}
                  </span>
                </div>
                <div className="api-service">
                  <span>BIST:</span>
                  <span className={bistHealthy ? 'healthy' : 'unhealthy'}>
                    {bistHealthy ? '✅' : '❌'}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Test Buttons */}
          {showTestButtons && (
            <div className="test-actions">
              <button
                onClick={runConnectionTests}
                className="test-button"
                disabled={!websocketConnected && !dashboardConnected}
              >
                🔧 Test Connections
              </button>
            </div>
          )}

          {/* Test Results */}
          {Object.keys(testResults).length > 0 && (
            <div className="test-results">
              <h4>Connection Test Results</h4>
              {Object.entries(testResults).map(([service, result]) => (
                <div key={service} className="test-result">
                  <strong>{service}:</strong>
                  <span className={result?.success ? 'success' : 'error'}>
                    {result?.success ? '✅ Passed' : '❌ Failed'}
                    {result?.error && ` - ${result.error}`}
                  </span>
                </div>
              ))}
            </div>
          )}

          {/* Performance Metrics */}
          {apiHealth && (
            <div className="performance-metrics">
              <h4>Performance</h4>
              {apiHealth.alpaca?.lastUpdate && (
                <div className="metric">
                  <span>Last Alpaca Update:</span>
                  <span>{new Date(apiHealth.alpaca.lastUpdate).toLocaleTimeString()}</span>
                </div>
              )}
              {apiHealth.bist?.lastUpdate && (
                <div className="metric">
                  <span>Last BIST Update:</span>
                  <span>{new Date(apiHealth.bist.lastUpdate).toLocaleTimeString()}</span>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      <style jsx>{`
        .connection-status-widget {
          background: var(--card-background);
          border: 1px solid var(--border-color);
          border-radius: 8px;
          overflow: hidden;
        }

        .status-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 12px 16px;
          cursor: ${showDetails ? 'pointer' : 'default'};
          background: var(--surface-primary);
          border-bottom: ${expanded ? '1px solid var(--border-color)' : 'none'};
        }

        .status-header:hover {
          background: ${showDetails ? 'var(--surface-secondary)' : 'var(--surface-primary)'};
        }

        .status-main {
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .status-icon {
          font-size: 16px;
        }

        .status-text {
          font-weight: 600;
          color: var(--text-primary);
        }

        .status-actions {
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .expand-indicator {
          font-size: 12px;
          color: var(--text-tertiary);
          transition: transform 0.2s ease;
        }

        .status-header.expanded .expand-indicator {
          transform: rotate(180deg);
        }

        .status-details {
          padding: 16px;
        }

        .connection-grid {
          display: grid;
          gap: 12px;
          margin-bottom: 16px;
        }

        .connection-item {
          padding: 12px;
          background: var(--surface-secondary);
          border-radius: 6px;
          border: 1px solid var(--border-color);
        }

        .connection-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 4px;
        }

        .connection-name {
          font-weight: 600;
          color: var(--text-primary);
        }

        .connection-status {
          font-size: 14px;
          display: flex;
          align-items: center;
          gap: 4px;
        }

        .connection-status.connected {
          color: var(--success-color);
        }

        .connection-status.connecting {
          color: var(--warning-color);
        }

        .connection-status.disconnected {
          color: var(--error-color);
        }

        .connection-error {
          font-size: 12px;
          color: var(--error-color);
          margin-top: 4px;
        }

        .api-breakdown {
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 8px;
          margin-top: 8px;
        }

        .api-service {
          display: flex;
          justify-content: space-between;
          align-items: center;
          font-size: 12px;
          padding: 4px 8px;
          background: var(--surface-primary);
          border-radius: 4px;
        }

        .api-service .healthy {
          color: var(--success-color);
        }

        .api-service .unhealthy {
          color: var(--error-color);
        }

        .test-actions {
          margin-bottom: 16px;
        }

        .test-button {
          background: var(--primary-color);
          color: white;
          border: none;
          padding: 8px 16px;
          border-radius: 6px;
          cursor: pointer;
          font-size: 14px;
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .test-button:hover:not(:disabled) {
          background: var(--primary-hover);
        }

        .test-button:disabled {
          background: var(--text-tertiary);
          cursor: not-allowed;
        }

        .test-results {
          background: var(--surface-primary);
          padding: 12px;
          border-radius: 6px;
          margin-bottom: 16px;
        }

        .test-results h4 {
          margin: 0 0 8px 0;
          font-size: 14px;
          color: var(--text-primary);
        }

        .test-result {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 4px;
          font-size: 12px;
        }

        .test-result .success {
          color: var(--success-color);
        }

        .test-result .error {
          color: var(--error-color);
        }

        .performance-metrics {
          background: var(--surface-primary);
          padding: 12px;
          border-radius: 6px;
        }

        .performance-metrics h4 {
          margin: 0 0 8px 0;
          font-size: 14px;
          color: var(--text-primary);
        }

        .metric {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 4px;
          font-size: 12px;
          color: var(--text-secondary);
        }
      `}</style>
    </div>
  );
};

export default ConnectionStatus;