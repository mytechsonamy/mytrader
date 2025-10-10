import React, { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

// Import pages
import Dashboard from './pages/Dashboard';
import Login from './pages/Login';
import Portfolio from './pages/Portfolio';
import Markets from './pages/Markets';
import Alerts from './pages/Alerts';
import Competition from './pages/Competition';
import Strategies from './pages/Strategies';
import StrategyTest from './pages/StrategyTest';
import Profile from './pages/Profile';
// import Register from './pages/Register';

// Import components
import ErrorBoundary from './components/ErrorBoundary';
import { ProtectedRoute, GuestGuard } from './components/shared/AuthGuard';

// Import stores and services
import { useAuthStore } from './store/authStore';
import { queryClient } from './services/queryClient';
import { WebSocketPriceProvider } from './context/WebSocketPriceContext';

// Import styles
import './styles/globals.css';

function App() {
  const { checkAuthStatus, isLoading } = useAuthStore();

  useEffect(() => {
    // Check authentication status on app startup
    checkAuthStatus();
  }, [checkAuthStatus]);

  // Global loading state
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background-primary">
        <div className="text-center">
          <div className="loading-spinner w-8 h-8 mx-auto mb-4" />
          <p className="text-text-tertiary">Loading myTrader...</p>
        </div>
      </div>
    );
  }

  return (
    <QueryClientProvider client={queryClient}>
      <WebSocketPriceProvider>
        <ErrorBoundary>
          <Router>
            <div className="App">
              <Routes>
              {/* Public Routes */}
              <Route
                path="/"
                element={
                  <ErrorBoundary fallback={<ErrorFallback error="Dashboard Error" />}>
                    <Dashboard />
                  </ErrorBoundary>
                }
              />

              {/* Markets - Public Access */}
              <Route
                path="/markets"
                element={
                  <ErrorBoundary fallback={<ErrorFallback error="Markets Error" />}>
                    <Markets />
                  </ErrorBoundary>
                }
              />

              {/* Competition - Public Access */}
              <Route
                path="/competition"
                element={
                  <ErrorBoundary fallback={<ErrorFallback error="Competition Error" />}>
                    <Competition />
                  </ErrorBoundary>
                }
              />

              {/* Authentication Routes (only for non-authenticated users) */}
              <Route
                path="/login"
                element={
                  <GuestGuard>
                    <ErrorBoundary fallback={<ErrorFallback error="Login Error" />}>
                      <Login />
                    </ErrorBoundary>
                  </GuestGuard>
                }
              />

              {/* Protected Routes (require authentication) */}
              <Route
                path="/portfolio"
                element={
                  <ProtectedRoute>
                    <ErrorBoundary fallback={<ErrorFallback error="Portfolio Error" />}>
                      <Portfolio />
                    </ErrorBoundary>
                  </ProtectedRoute>
                }
              />

              <Route
                path="/alerts"
                element={
                  <ProtectedRoute>
                    <ErrorBoundary fallback={<ErrorFallback error="Alerts Error" />}>
                      <Alerts />
                    </ErrorBoundary>
                  </ProtectedRoute>
                }
              />

              <Route
                path="/strategies"
                element={
                  <ProtectedRoute>
                    <ErrorBoundary fallback={<ErrorFallback error="Strategies Error" />}>
                      <Strategies />
                    </ErrorBoundary>
                  </ProtectedRoute>
                }
              />

              <Route
                path="/strategies/test"
                element={
                  <ProtectedRoute>
                    <ErrorBoundary fallback={<ErrorFallback error="Strategy Test Error" />}>
                      <StrategyTest />
                    </ErrorBoundary>
                  </ProtectedRoute>
                }
              />

              <Route
                path="/profile"
                element={
                  <ProtectedRoute>
                    <ErrorBoundary fallback={<ErrorFallback error="Profile Error" />}>
                      <Profile />
                    </ErrorBoundary>
                  </ProtectedRoute>
                }
              />

              {/* TODO: Add Register route when component is created */}
              {/*
              <Route
                path="/register"
                element={
                  <GuestGuard>
                    <ErrorBoundary fallback={<ErrorFallback error="Registration Error" />}>
                      <Register />
                    </ErrorBoundary>
                  </GuestGuard>
                }
              />
              */}

              {/* Catch-all route for 404 */}
              <Route
                path="*"
                element={
                  <div className="min-h-screen flex items-center justify-center bg-background-primary">
                    <div className="text-center">
                      <h1 className="text-4xl font-bold text-text-primary mb-4">404</h1>
                      <p className="text-text-tertiary mb-6">Page not found</p>
                      <a
                        href="/"
                        className="btn-primary"
                      >
                        Go Home
                      </a>
                    </div>
                  </div>
                }
              />
            </Routes>
          </div>
        </Router>
      </ErrorBoundary>

      {/* React Query DevTools (only in development) */}
      {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
    </WebSocketPriceProvider>
  </QueryClientProvider>
  );
}

// Error fallback component
const ErrorFallback: React.FC<{ error: string }> = ({ error }) => (
  <div className="min-h-screen flex items-center justify-center bg-background-primary">
    <div className="text-center max-w-md mx-auto p-6">
      <h2 className="text-2xl font-bold text-text-primary mb-4">{error}</h2>
      <p className="text-text-tertiary mb-6">
        There was an error loading this page. Please refresh the page or try again later.
      </p>
      <button
        onClick={() => window.location.reload()}
        className="btn-primary"
      >
        Refresh Page
      </button>
    </div>
  </div>
);

export default App;
