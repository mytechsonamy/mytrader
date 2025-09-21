import React, { useState, useEffect } from 'react';
import { authService } from '../services/authService';
import { signalRService } from '../services/signalRService';
import './Dashboard.css';

interface DashboardProps {
  onLogout: () => void;
}

const Dashboard: React.FC<DashboardProps> = ({ onLogout }) => {
  const [healthStatus, setHealthStatus] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [signalRStatus, setSignalRStatus] = useState<string>('Disconnected');
  const [signalRError, setSignalRError] = useState<string | null>(null);

  useEffect(() => {
    checkHealth();
    updateSignalRStatus();
  }, []);

  const updateSignalRStatus = () => {
    setSignalRStatus(signalRService.getConnectionState());
  };

  const checkHealth = async () => {
    try {
      setLoading(true);
      const health = await authService.checkHealth();
      setHealthStatus(health);
      setError(null);
    } catch (error: any) {
      setError('Failed to connect to backend API');
      console.error('Health check failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const testSignalRConnection = async () => {
    try {
      setSignalRError(null);
      setSignalRStatus('Connecting...');
      
      await signalRService.connectToTradingHub();
      updateSignalRStatus();
      
      const testResult = await signalRService.testConnection();
      console.log('SignalR test result:', testResult);
      
    } catch (error: any) {
      setSignalRError(error.message);
      updateSignalRStatus();
      console.error('SignalR test failed:', error);
    }
  };

  const handleLogout = () => {
    authService.logout();
    onLogout();
  };

  return (
    <div className="dashboard-container">
      <header className="dashboard-header">
        <div className="header-content">
          <h1>myTrader Dashboard</h1>
          <button onClick={handleLogout} className="logout-btn">
            Logout
          </button>
        </div>
      </header>

      <main className="dashboard-main">
        <div className="dashboard-content">
          <div className="welcome-section">
            <h2>Welcome to myTrader</h2>
            <p>Your trading platform is ready for action!</p>
          </div>

          <div className="status-section">
            <h3>System Status</h3>
            
            {loading && (
              <div className="status-loading">
                <div className="spinner"></div>
                <span>Checking backend connection...</span>
              </div>
            )}

            {error && (
              <div className="status-error">
                <div className="status-indicator error"></div>
                <div>
                  <strong>Backend API:</strong> {error}
                  <button onClick={checkHealth} className="retry-btn">
                    Retry
                  </button>
                </div>
              </div>
            )}

            {healthStatus && !error && (
              <div className="status-success">
                <div className="status-indicator success"></div>
                <div>
                  <strong>Backend API:</strong> Connected ✓
                  <div className="status-details">
                    <small>Status: {healthStatus.status}</small>
                    <small>Timestamp: {new Date(healthStatus.timestamp).toLocaleString()}</small>
                  </div>
                </div>
              </div>
            )}

            {/* SignalR Status */}
            <div className={`status-${signalRStatus === 'Connected' ? 'success' : signalRError ? 'error' : 'loading'}`}>
              <div className={`status-indicator ${signalRStatus === 'Connected' ? 'success' : 'error'}`}></div>
              <div>
                <strong>SignalR Connection:</strong> {signalRStatus}
                {signalRError && (
                  <div className="status-details">
                    <small style={{color: '#dc3545'}}>{signalRError}</small>
                  </div>
                )}
                <button onClick={testSignalRConnection} className="retry-btn" style={{marginLeft: '10px'}}>
                  Test Connection
                </button>
              </div>
            </div>
          </div>

          <div className="features-section">
            <h3>Available Features</h3>
            <div className="feature-grid">
              <div className="feature-card">
                <h4>🔐 Authentication</h4>
                <p>User registration and login system</p>
                <div className="status-badge success">✓ Ready</div>
              </div>
              
              <div className="feature-card">
                <h4>� SignalR Hub</h4>
                <p>Real-time WebSocket connections</p>
                <div className={`status-badge ${signalRStatus === 'Connected' ? 'success' : 'pending'}`}>
                  {signalRStatus === 'Connected' ? '✓ Connected' : '🔄 Ready to Test'}
                </div>
              </div>
              
              <div className="feature-card">
                <h4>� Market Data</h4>
                <p>Real-time cryptocurrency prices</p>
                <div className="status-badge pending">🔄 In Development</div>
              </div>
              
              <div className="feature-card">
                <h4>💼 Portfolio</h4>
                <p>Track your trading positions</p>
                <div className="status-badge pending">🔄 In Development</div>
              </div>
            </div>
          </div>

          <div className="quick-actions">
            <h3>Quick Actions</h3>
            <div className="action-buttons">
              <button className="action-btn" disabled>
                📈 View Market Data
              </button>
              <button className="action-btn" disabled>
                💰 Check Portfolio
              </button>
              <button 
                className="action-btn"
                onClick={testSignalRConnection}
              >
                🔗 Test SignalR
              </button>
              <button 
                className="action-btn"
                onClick={checkHealth}
              >
                🔄 Test API
              </button>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default Dashboard;