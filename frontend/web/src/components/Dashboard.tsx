import { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import { logout } from '../store/slices/authSlice';
import { setSidebarOpen } from '../store/slices/uiSlice';
import MarketOverview from './dashboard/MarketOverview';
import UserProfile from './dashboard/UserProfile';
import NotificationCenter from './dashboard/NotificationCenter';
import NewsSection from './dashboard/NewsSection';
import LeaderboardSection from './dashboard/LeaderboardSection';
import AuthPrompt from './AuthPrompt';
import Footer from './Footer';
import './Dashboard.css';

interface DashboardProps {
  onLogout?: () => void;
}

const Dashboard: React.FC<DashboardProps> = ({ onLogout }) => {
  const dispatch = useAppDispatch();
  const { user, isAuthenticated, isGuest } = useAppSelector((state) => state.auth);
  const { sidebarOpen, theme } = useAppSelector((state) => state.ui);
  const [showAuthPrompt, setShowAuthPrompt] = useState(false);

  useEffect(() => {
    // Initialize dashboard data
    if (user) {
      console.log('Dashboard initialized for user:', user.email);
    } else if (isGuest) {
      console.log('Dashboard initialized in guest mode');
    }
  }, [user, isGuest]);

  const handleLogout = () => {
    dispatch(logout());
    if (onLogout) {
      onLogout();
    }
  };

  const handleAuthRequired = () => {
    setShowAuthPrompt(true);
  };

  const handleCloseAuthPrompt = () => {
    setShowAuthPrompt(false);
  };

  const navigateToLogin = () => {
    window.location.href = '/login';
  };

  const toggleSidebar = () => {
    dispatch(setSidebarOpen(!sidebarOpen));
  };

  return (
    <div className={`dashboard-container theme-${theme}`}>
      <NotificationCenter />

      <header className="dashboard-header">
        <div className="header-content">
          <div className="brand-section">
            <button
              className="sidebar-toggle"
              onClick={toggleSidebar}
              aria-label="Toggle navigation menu"
            >
              ☰
            </button>
            <div className="brand-logo">
              <div className="brand-icon">T</div>
              <h1 className="brand-name">myTrader</h1>
              <span className="brand-badge">Pro</span>
            </div>
            <div className="powered-by-badge">
              <span className="powered-text">Powered by</span>
              <span className="techsonamy-brand">Techsonamy</span>
            </div>
          </div>

          <div className="header-actions">
            {isAuthenticated && user ? (
              <div className="user-info">
                <span className="welcome-text">
                  Welcome back, {user.firstName}
                </span>
                <div className="user-avatar">
                  {user.firstName?.[0]?.toUpperCase() || 'U'}
                </div>
                <button onClick={handleLogout} className="logout-btn">
                  Sign Out
                </button>
              </div>
            ) : (
              <div className="user-info">
                <span className="guest-text">
                  Welcome to myTrader
                </span>
                <button onClick={navigateToLogin} className="login-btn">
                  Sign In
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      <div className="dashboard-layout">
        <aside className={`dashboard-sidebar ${sidebarOpen ? 'open' : 'closed'}`}>
          <div className="sidebar-header">
            <UserProfile className="sidebar-profile" />
          </div>

          <nav className="sidebar-nav">
            <div className="nav-section">
              <h4 className="nav-section-title">Trading</h4>
              <ul className="nav-list">
                <li className="nav-item">
                  <a href="#" className="nav-link active">
                    <span className="nav-icon">📊</span>
                    <span className="nav-label">Dashboard</span>
                  </a>
                </li>
                <li className="nav-item">
                  <a href="#" className="nav-link">
                    <span className="nav-icon">📈</span>
                    <span className="nav-label">Market Data</span>
                    <span className="nav-badge">Live</span>
                  </a>
                </li>
                <li className="nav-item">
                  <a
                    href="#"
                    className={`nav-link ${!isAuthenticated ? 'disabled' : ''}`}
                    onClick={!isAuthenticated ? handleAuthRequired : undefined}
                  >
                    <span className="nav-icon">💼</span>
                    <span className="nav-label">Portfolio</span>
                    {!isAuthenticated && <span className="nav-lock">🔒</span>}
                  </a>
                </li>
                <li className="nav-item">
                  <a
                    href="#"
                    className={`nav-link ${!isAuthenticated ? 'disabled' : ''}`}
                    onClick={!isAuthenticated ? handleAuthRequired : undefined}
                  >
                    <span className="nav-icon">🎯</span>
                    <span className="nav-label">Strategies</span>
                    {!isAuthenticated && <span className="nav-lock">🔒</span>}
                  </a>
                </li>
              </ul>
            </div>

            <div className="nav-section">
              <h4 className="nav-section-title">Analysis</h4>
              <ul className="nav-list">
                <li className="nav-item">
                  <a href="#" className="nav-link">
                    <span className="nav-icon">📰</span>
                    <span className="nav-label">News</span>
                  </a>
                </li>
                <li className="nav-item">
                  <a href="#" className="nav-link">
                    <span className="nav-icon">🏆</span>
                    <span className="nav-label">Leaderboard</span>
                  </a>
                </li>
                <li className="nav-item">
                  <a
                    href="#"
                    className={`nav-link ${!isAuthenticated ? 'disabled' : ''}`}
                    onClick={!isAuthenticated ? handleAuthRequired : undefined}
                  >
                    <span className="nav-icon">📊</span>
                    <span className="nav-label">Analytics</span>
                    {!isAuthenticated && <span className="nav-lock">🔒</span>}
                  </a>
                </li>
              </ul>
            </div>

            <div className="nav-section">
              <h4 className="nav-section-title">Settings</h4>
              <ul className="nav-list">
                <li className="nav-item">
                  <a
                    href="#"
                    className={`nav-link ${!isAuthenticated ? 'disabled' : ''}`}
                    onClick={!isAuthenticated ? handleAuthRequired : undefined}
                  >
                    <span className="nav-icon">👤</span>
                    <span className="nav-label">Profile</span>
                    {!isAuthenticated && <span className="nav-lock">🔒</span>}
                  </a>
                </li>
                <li className="nav-item">
                  <a href="#" className="nav-link">
                    <span className="nav-icon">⚙️</span>
                    <span className="nav-label">Preferences</span>
                  </a>
                </li>
              </ul>
            </div>
          </nav>

          {!isAuthenticated && (
            <div className="sidebar-guest">
              <div className="guest-welcome">
                <h3>Join myTrader</h3>
                <p>Unlock advanced features and start your trading journey</p>
              </div>
              <div className="guest-actions">
                <button
                  onClick={navigateToLogin}
                  className="guest-action-btn primary"
                >
                  Sign In
                </button>
                <a href="/register" className="guest-action-btn secondary">
                  Create Account
                </a>
              </div>
              <div className="feature-preview">
                <h4>Premium Features</h4>
                <p>Portfolio tracking, advanced analytics, and personalized insights</p>
                <div className="feature-status">
                  <span className="status-indicator locked"></span>
                  <span className="status-text locked">Sign in to unlock</span>
                </div>
              </div>
            </div>
          )}
        </aside>

        <main className="dashboard-main">
          <div className="dashboard-content">
            {/* Top Section - Market Data Accordion */}
            <MarketOverview className="market-accordion-section" />

            {/* Bottom Section - News & Leaderboard */}
            <div className="bottom-section">
              <NewsSection />
              <LeaderboardSection />
            </div>
          </div>
        </main>
      </div>

      <Footer />

      <AuthPrompt
        isOpen={showAuthPrompt}
        onClose={handleCloseAuthPrompt}
        title="Authentication Required"
        message="Please sign in to access this feature and unlock the full myTrader experience."
      />
    </div>
  );
};

export default Dashboard;