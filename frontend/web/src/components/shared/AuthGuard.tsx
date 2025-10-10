/**
 * Authentication guard for protected routes
 */

import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import type { ChildrenProps } from '../../types';

interface AuthGuardProps extends ChildrenProps {
  fallback?: React.ReactNode;
  redirectTo?: string;
  requireAuth?: boolean;
}

export const AuthGuard: React.FC<AuthGuardProps> = ({
  children,
  fallback = null,
  redirectTo = '/login',
  requireAuth = true,
}) => {
  const location = useLocation();
  const { isAuthenticated, isLoading, isGuest } = useAuthStore();

  // Show loading state while checking authentication
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="loading-spinner w-8 h-8 mx-auto mb-4" />
          <p className="text-text-tertiary">Loading...</p>
        </div>
      </div>
    );
  }

  // If authentication is required but user is not authenticated
  if (requireAuth && !isAuthenticated && !isGuest) {
    return (
      <Navigate
        to={redirectTo}
        state={{ from: location.pathname }}
        replace
      />
    );
  }

  // If authentication is not required but user is authenticated
  // (e.g., login page when already logged in)
  if (!requireAuth && isAuthenticated) {
    const from = location.state?.from || '/';
    return <Navigate to={from} replace />;
  }

  // Show fallback if provided and conditions are not met
  if (fallback && requireAuth && !isAuthenticated) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
};

// Guest guard - for routes that should only be accessible to non-authenticated users
export const GuestGuard: React.FC<ChildrenProps> = ({ children }) => {
  return (
    <AuthGuard requireAuth={false}>
      {children}
    </AuthGuard>
  );
};

// Protected route wrapper
export const ProtectedRoute: React.FC<ChildrenProps> = ({ children }) => {
  return (
    <AuthGuard requireAuth={true}>
      {children}
    </AuthGuard>
  );
};