/**
 * Authenticated layout for logged-in users
 */

import React, { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Button } from '../components/ui';
import { useAuthStore } from '../store/authStore';
import { useUIStore } from '../store/uiStore';
import type { ChildrenProps } from '../types';

interface AuthLayoutProps extends ChildrenProps {
  showSidebar?: boolean;
}

const AuthLayout: React.FC<AuthLayoutProps> = ({
  children,
  showSidebar = true,
}) => {
  const location = useLocation();
  const navigate = useNavigate();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const { user, logout } = useAuthStore();
  const { sidebar, toggleSidebar, setSidebarOpen } = useUIStore();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const navigation = [
    {
      name: 'Dashboard',
      href: '/',
      icon: '🏠',
      current: location.pathname === '/',
    },
    {
      name: 'Portfolio',
      href: '/portfolio',
      icon: '💼',
      current: location.pathname === '/portfolio',
    },
    {
      name: 'Markets',
      href: '/markets',
      icon: '📈',
      current: location.pathname === '/markets',
    },
    {
      name: 'Strategies',
      href: '/strategies',
      icon: '⚡',
      current: location.pathname === '/strategies',
    },
    {
      name: 'Leaderboard',
      href: '/leaderboard',
      icon: '🏆',
      current: location.pathname === '/leaderboard',
    },
  ];

  const userNavigation = [
    { name: 'Profile', href: '/profile' },
    { name: 'Settings', href: '/settings' },
    { name: 'Notifications', href: '/notifications' },
  ];

  return (
    <div className="min-h-screen bg-background-primary">
      {/* Mobile sidebar overlay */}
      {isMobileMenuOpen && (
        <div
          className="fixed inset-0 flex z-40 lg:hidden"
          role="dialog"
          aria-modal="true"
        >
          <div
            className="fixed inset-0 bg-gray-600 bg-opacity-75"
            aria-hidden="true"
            onClick={() => setIsMobileMenuOpen(false)}
          />

          {/* Mobile sidebar */}
          <div className="relative flex-1 flex flex-col max-w-xs w-full bg-background-tertiary border-r border-border-subtle">
            <div className="absolute top-0 right-0 -mr-12 pt-2">
              <button
                type="button"
                className="ml-1 flex items-center justify-center h-10 w-10 rounded-full focus:outline-none focus:ring-2 focus:ring-inset focus:ring-white"
                onClick={() => setIsMobileMenuOpen(false)}
              >
                <span className="sr-only">Close sidebar</span>
                <svg
                  className="h-6 w-6 text-white"
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <SidebarContent navigation={navigation} />
          </div>

          <div className="flex-shrink-0 w-14" />
        </div>
      )}

      {/* Desktop sidebar */}
      {showSidebar && (
        <div
          className={`hidden lg:flex lg:flex-col lg:fixed lg:inset-y-0 transition-all duration-300 ${
            sidebar.isCollapsed ? 'lg:w-16' : 'lg:w-64'
          }`}
        >
          <SidebarContent navigation={navigation} isCollapsed={sidebar.isCollapsed} />
        </div>
      )}

      {/* Main content */}
      <div
        className={`${
          showSidebar
            ? sidebar.isCollapsed
              ? 'lg:pl-16'
              : 'lg:pl-64'
            : ''
        } transition-all duration-300`}
      >
        {/* Top navigation */}
        <header className="bg-background-tertiary border-b border-border-subtle sticky top-0 z-30">
          <div className="px-4 sm:px-6 lg:px-8">
            <div className="flex items-center justify-between h-16">
              {/* Left side */}
              <div className="flex items-center">
                {/* Mobile menu button */}
                <button
                  type="button"
                  className="lg:hidden -ml-0.5 -mt-0.5 h-12 w-12 inline-flex items-center justify-center rounded-md text-text-secondary hover:text-text-primary focus:outline-none focus:ring-2 focus:ring-brand-500"
                  onClick={() => setIsMobileMenuOpen(true)}
                >
                  <span className="sr-only">Open sidebar</span>
                  <svg
                    className="h-6 w-6"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                  </svg>
                </button>

                {/* Desktop sidebar toggle */}
                {showSidebar && (
                  <button
                    type="button"
                    className="hidden lg:flex items-center justify-center h-8 w-8 rounded-md text-text-secondary hover:text-text-primary focus:outline-none focus:ring-2 focus:ring-brand-500 ml-4"
                    onClick={toggleSidebar}
                  >
                    <svg
                      className="h-5 w-5"
                      xmlns="http://www.w3.org/2000/svg"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      {sidebar.isCollapsed ? (
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 5l7 7-7 7M5 5l7 7-7 7" />
                      ) : (
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 19l-7-7 7-7m8 14l-7-7 7-7" />
                      )}
                    </svg>
                  </button>
                )}

                {/* Logo for mobile */}
                <div className="lg:hidden ml-4">
                  <Link to="/" className="flex items-center space-x-2">
                    <div className="w-8 h-8 bg-gradient-brand rounded-md flex items-center justify-center">
                      <span className="text-white font-bold text-lg">MT</span>
                    </div>
                    <span className="font-bold text-lg text-text-primary">myTrader</span>
                  </Link>
                </div>
              </div>

              {/* Right side */}
              <div className="flex items-center space-x-4">
                {/* Notifications */}
                <button
                  type="button"
                  className="p-2 rounded-md text-text-secondary hover:text-text-primary focus:outline-none focus:ring-2 focus:ring-brand-500"
                >
                  <span className="sr-only">View notifications</span>
                  <svg
                    className="h-6 w-6"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-5 5v-5zM4 1h5l-5 5V1z" />
                  </svg>
                </button>

                {/* User menu */}
                <div className="relative">
                  <button
                    type="button"
                    className="flex items-center space-x-3 p-2 rounded-md text-text-secondary hover:text-text-primary focus:outline-none focus:ring-2 focus:ring-brand-500"
                  >
                    <div className="w-8 h-8 bg-brand-500 rounded-full flex items-center justify-center">
                      <span className="text-white text-sm font-medium">
                        {user?.firstName?.[0] || user?.username?.[0] || 'U'}
                      </span>
                    </div>
                    <span className="hidden md:block text-sm font-medium">
                      {user?.firstName || user?.username || 'User'}
                    </span>
                    <svg
                      className="h-4 w-4"
                      xmlns="http://www.w3.org/2000/svg"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>

                  {/* User dropdown menu would go here */}
                </div>
              </div>
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1">
          {children}
        </main>
      </div>
    </div>
  );
};

// Sidebar content component
const SidebarContent: React.FC<{
  navigation: Array<{ name: string; href: string; icon: string; current: boolean }>;
  isCollapsed?: boolean;
}> = ({ navigation, isCollapsed = false }) => {
  const { logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <div className="flex flex-col flex-1 bg-background-tertiary border-r border-border-subtle">
      {/* Logo */}
      <div className="flex items-center h-16 px-4 border-b border-border-subtle">
        <Link to="/" className="flex items-center space-x-2">
          <div className="w-8 h-8 bg-gradient-brand rounded-md flex items-center justify-center">
            <span className="text-white font-bold text-lg">MT</span>
          </div>
          {!isCollapsed && (
            <span className="font-bold text-lg text-text-primary">MyTrader</span>
          )}
        </Link>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-2 py-4 space-y-1">
        {navigation.map((item) => (
          <Link
            key={item.name}
            to={item.href}
            className={`group flex items-center px-2 py-2 text-sm font-medium rounded-md transition-colors ${
              item.current
                ? 'bg-brand-500 text-white'
                : 'text-text-secondary hover:text-text-primary hover:bg-background-secondary'
            }`}
            title={isCollapsed ? item.name : undefined}
          >
            <span className="text-lg mr-3">{item.icon}</span>
            {!isCollapsed && item.name}
          </Link>
        ))}
      </nav>

      {/* User section */}
      <div className="flex-shrink-0 border-t border-border-subtle p-4">
        <button
          onClick={handleLogout}
          className={`group flex items-center w-full px-2 py-2 text-sm font-medium rounded-md text-text-secondary hover:text-text-primary hover:bg-background-secondary transition-colors ${
            isCollapsed ? 'justify-center' : ''
          }`}
          title={isCollapsed ? 'Sign Out' : undefined}
        >
          <span className="text-lg mr-3">🚪</span>
          {!isCollapsed && 'Sign Out'}
        </button>
      </div>
    </div>
  );
};

export default AuthLayout;