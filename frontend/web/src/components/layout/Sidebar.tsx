/**
 * Sidebar navigation component for authenticated users
 * Features: Collapsible design, navigation sections, user profile
 */

import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { useUIStore } from '../../store/uiStore';
import { cn } from '../../utils';
import UserProfile from '../dashboard/UserProfile';
import type { BaseComponentProps } from '../../types';

export interface SidebarProps extends BaseComponentProps {
  isCollapsed?: boolean;
  variant?: 'default' | 'compact';
}

interface NavigationSection {
  title: string;
  items: NavigationItem[];
}

interface NavigationItem {
  icon: string;
  label: string;
  href: string;
  badge?: string;
  requiresAuth?: boolean;
}

const Sidebar: React.FC<SidebarProps> = ({
  isCollapsed = false,
  variant = 'default',
  className,
  ...props
}) => {
  const { isAuthenticated, user } = useAuthStore();
  const { sidebar, setSidebarOpen } = useUIStore();
  const location = useLocation();

  const handleLinkClick = () => {
    // Auto-close sidebar on mobile after navigation
    if (window.innerWidth < 1024) {
      setSidebarOpen(false);
    }
  };

  const navigationSections: NavigationSection[] = [
    {
      title: 'Trading',
      items: [
        {
          icon: '📊',
          label: 'Dashboard',
          href: '/',
        },
        {
          icon: '📈',
          label: 'Markets',
          href: '/markets',
          badge: 'Live',
        },
        {
          icon: '💼',
          label: 'Portfolio',
          href: '/portfolio',
          requiresAuth: true,
        },
        {
          icon: '🎯',
          label: 'Strategies',
          href: '/strategies',
          requiresAuth: true,
        },
      ],
    },
    {
      title: 'Analysis',
      items: [
        {
          icon: '🔔',
          label: 'Alerts',
          href: '/alerts',
          requiresAuth: true,
        },
        {
          icon: '🏆',
          label: 'Competition',
          href: '/competition',
        },
        {
          icon: '📊',
          label: 'Analytics',
          href: '/analytics',
          requiresAuth: true,
        },
      ],
    },
    {
      title: 'Account',
      items: [
        {
          icon: '👤',
          label: 'Profile',
          href: '/profile',
          requiresAuth: true,
        },
        {
          icon: '⚙️',
          label: 'Settings',
          href: '/settings',
          requiresAuth: true,
        },
      ],
    },
  ];

  const renderNavigationItem = (item: NavigationItem) => {
    const isDisabled = item.requiresAuth && !isAuthenticated;
    const isActive = location.pathname === item.href;

    const content = (
      <>
        <span
          className={cn(
            'flex-shrink-0 text-lg',
            isCollapsed ? '' : 'mr-3'
          )}
        >
          {item.icon}
        </span>

        {!isCollapsed && (
          <>
            <span className="flex-1">{item.label}</span>
            {item.badge && (
              <span className="ml-2 px-2 py-0.5 text-xs bg-positive-100 text-positive-700 rounded-full">
                {item.badge}
              </span>
            )}
            {isDisabled && (
              <span className="ml-2 text-text-tertiary">🔒</span>
            )}
          </>
        )}
      </>
    );

    const linkClasses = cn(
      'nav-link group flex items-center px-3 py-2 text-sm font-medium rounded-md transition-colors',
      'hover:bg-background-secondary focus:outline-none focus:ring-2 focus:ring-brand-500',
      isActive
        ? 'bg-brand-50 text-brand-600 border-r-2 border-brand-600'
        : 'text-text-primary hover:text-brand-500',
      isDisabled && 'opacity-50 cursor-not-allowed pointer-events-none',
      isCollapsed ? 'justify-center px-2' : 'justify-start'
    );

    return (
      <li key={item.href} className="nav-item">
        {isDisabled ? (
          <div
            className={linkClasses}
            aria-label={isCollapsed ? item.label : undefined}
            title={isCollapsed ? item.label : undefined}
          >
            {content}
          </div>
        ) : (
          <Link
            to={item.href}
            onClick={handleLinkClick}
            className={linkClasses}
            aria-label={isCollapsed ? item.label : undefined}
            title={isCollapsed ? item.label : undefined}
          >
            {content}
          </Link>
        )}
      </li>
    );
  };

  const renderNavigationSection = (section: NavigationSection) => (
    <div key={section.title} className="nav-section">
      {!isCollapsed && (
        <h4 className="px-3 mb-2 text-xs font-semibold text-text-tertiary uppercase tracking-wider">
          {section.title}
        </h4>
      )}
      <ul className="space-y-1">
        {section.items.map(renderNavigationItem)}
      </ul>
    </div>
  );

  return (
    <>
      {/* Mobile Overlay */}
      {sidebar.isOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 lg:hidden"
          onClick={() => setSidebarOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed top-16 left-0 z-40 h-[calc(100vh-4rem)] bg-white border-r border-border-default',
          'transform transition-transform duration-300 ease-in-out lg:translate-x-0',
          'overflow-y-auto',
          sidebar.isOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0',
          isCollapsed ? 'w-16' : 'w-64',
          className
        )}
        aria-label="Sidebar navigation"
        {...props}
      >
        {/* User Profile Section */}
        {!isCollapsed && isAuthenticated && user && (
          <div className="p-4 border-b border-border-default">
            <UserProfile
              user={user}
              variant="sidebar"
              className="sidebar-profile"
            />
          </div>
        )}

        {/* Navigation */}
        <nav className="flex-1 px-4 py-4 space-y-6" role="navigation">
          {navigationSections.map(renderNavigationSection)}
        </nav>

        {/* Guest Call-to-Action */}
        {!isCollapsed && !isAuthenticated && (
          <div className="p-4 border-t border-border-default">
            <div className="bg-gradient-brand rounded-lg p-4 text-white">
              <h3 className="text-sm font-semibold mb-2">Join myTrader</h3>
              <p className="text-xs opacity-90 mb-3">
                Unlock advanced features and start your trading journey
              </p>
              <div className="space-y-2">
                <Link
                  to="/login"
                  className="block w-full text-center py-2 px-3 bg-white text-brand-500 text-xs font-medium rounded hover:bg-gray-100 transition-colors"
                >
                  Sign In
                </Link>
                <Link
                  to="/register"
                  className="block w-full text-center py-2 px-3 border border-white/20 text-white text-xs font-medium rounded hover:bg-white/10 transition-colors"
                >
                  Create Account
                </Link>
              </div>
            </div>

            <div className="mt-4 p-3 bg-background-secondary rounded-lg">
              <h4 className="text-xs font-semibold text-text-primary mb-1">
                Premium Features
              </h4>
              <p className="text-xs text-text-tertiary mb-2">
                Portfolio tracking, advanced analytics, and personalized insights
              </p>
              <div className="flex items-center text-xs text-text-tertiary">
                <span className="w-2 h-2 bg-negative-400 rounded-full mr-2"></span>
                Sign in to unlock
              </div>
            </div>
          </div>
        )}

        {/* Collapse Toggle for Desktop */}
        <div className="hidden lg:block p-4 border-t border-border-default">
          <button
            onClick={() => setSidebarOpen(!sidebar.isOpen)}
            className="w-full flex items-center justify-center py-2 px-3 text-text-tertiary hover:text-text-primary hover:bg-background-secondary rounded-md transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500"
            aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            <svg
              className={cn(
                'w-4 h-4 transition-transform duration-200',
                isCollapsed ? 'rotate-180' : 'rotate-0'
              )}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M11 19l-7-7 7-7m8 14l-7-7 7-7"
              />
            </svg>
            {!isCollapsed && (
              <span className="ml-2 text-xs">Collapse</span>
            )}
          </button>
        </div>
      </aside>
    </>
  );
};

export default Sidebar;