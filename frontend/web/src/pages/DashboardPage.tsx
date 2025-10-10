/**
 * Main Dashboard Page Component
 * Uses responsive layouts based on authentication status
 */

import React from 'react';
import { useAuthStore } from '../store/authStore';
import { PublicLayout, AuthenticatedLayout } from '../components/layout';
import Dashboard from './Dashboard';

const DashboardPage: React.FC = () => {
  const { isAuthenticated } = useAuthStore();

  if (isAuthenticated) {
    return (
      <AuthenticatedLayout>
        <Dashboard />
      </AuthenticatedLayout>
    );
  }

  return (
    <PublicLayout>
      <Dashboard />
    </PublicLayout>
  );
};

export default DashboardPage;