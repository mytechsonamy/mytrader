import React, { useState, useEffect } from 'react';
import { authService } from './services/authService';
import Login from './components/Login';
import Register from './components/Register';
import Dashboard from './components/Dashboard';
import './App.css';

type AuthView = 'login' | 'register';

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [authView, setAuthView] = useState<AuthView>('login');
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    // Check if user is already authenticated
    const checkAuth = () => {
      setIsAuthenticated(authService.isAuthenticated());
      setLoading(false);
    };

    checkAuth();
  }, []);

  const handleLoginSuccess = () => {
    setIsAuthenticated(true);
  };

  const handleLogout = () => {
    setIsAuthenticated(false);
  };

  const switchToRegister = () => {
    setAuthView('register');
  };

  const switchToLogin = () => {
    setAuthView('login');
  };

  if (loading) {
    return (
      <div className="loading-container">
        <div className="loading-spinner"></div>
        <p>Loading myTrader...</p>
      </div>
    );
  }

  if (isAuthenticated) {
    return <Dashboard onLogout={handleLogout} />;
  }

  return (
    <div className="App">
      {authView === 'login' ? (
        <Login 
          onLoginSuccess={handleLoginSuccess}
          onSwitchToRegister={switchToRegister}
        />
      ) : (
        <Register 
          onRegisterSuccess={handleLoginSuccess}
          onSwitchToLogin={switchToLogin}
        />
      )}
    </div>
  );
}

export default App;
