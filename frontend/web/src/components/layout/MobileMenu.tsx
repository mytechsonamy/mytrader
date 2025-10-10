/**
 * Mobile menu component with slide-out navigation
 * Features: Full-screen overlay, navigation links, auth actions
 */

import React, { useEffect } from 'react';
import { useAuthStore } from '../../store/authStore';
import { cn } from '../../utils';
import type { BaseComponentProps } from '../../types';

export interface MobileMenuProps extends BaseComponentProps {
  isOpen: boolean;
  onClose: () => void;
  showAuthActions?: boolean;
}

const MobileMenu: React.FC<MobileMenuProps> = ({
  isOpen,
  onClose,
  showAuthActions = true,
  className,
  ...props
}) => {
  const { isAuthenticated, user, logout } = useAuthStore();

  // Handle escape key and prevent body scroll when open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';

      const handleEscape = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
          onClose();
        }
      };

      document.addEventListener('keydown', handleEscape);
      return () => {
        document.removeEventListener('keydown', handleEscape);
        document.body.style.overflow = 'unset';
      };
    } else {
      document.body.style.overflow = 'unset';
    }
  }, [isOpen, onClose]);

  const handleLinkClick = () => {
    onClose();
  };

  const handleLogout = () => {
    logout();
    onClose();
  };

  if (!isOpen) return null;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/50 z-50 lg:hidden"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Mobile Menu Panel */}
      <div
        className={cn(
          'fixed top-0 right-0 h-full w-80 max-w-sm bg-white shadow-2xl z-50 lg:hidden',
          'transform transition-transform duration-300 ease-in-out',
          isOpen ? 'translate-x-0' : 'translate-x-full',
          className
        )}
        role="dialog"
        aria-modal="true"
        aria-labelledby="mobile-menu-title"
        {...props}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border-default">
          <h2 id="mobile-menu-title" className="text-lg font-semibold text-text-primary">
            Navigation
          </h2>
          <button
            onClick={onClose}
            className="p-2 rounded-md text-text-tertiary hover:text-text-primary hover:bg-background-secondary transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500"
            aria-label="Close menu"
          >
            <svg
              className="w-5 h-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        {/* User Section */}
        {isAuthenticated && user && (
          <div className="p-4 bg-background-secondary border-b border-border-default">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 bg-brand-500 text-white rounded-full flex items-center justify-center font-semibold">
                {user.firstName?.[0]?.toUpperCase() || user.email[0].toUpperCase()}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-text-primary truncate">
                  {user.firstName ? `${user.firstName} ${user.lastName || ''}`.trim() : user.username}
                </p>
                <p className="text-xs text-text-tertiary truncate">
                  {user.email}
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Navigation Links */}
        <nav className="flex-1 overflow-y-auto py-4" role="navigation">
          <div className="space-y-1 px-4">
            {/* Main Navigation */}
            <div className="space-y-1">
              <a
                href="/"
                onClick={handleLinkClick}
                className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
              >
                <span className="mr-3">📊</span>
                Dashboard
              </a>
              <a
                href="/market"
                onClick={handleLinkClick}
                className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
              >
                <span className="mr-3">📈</span>
                Market Data
                <span className="ml-auto text-xs bg-positive-100 text-positive-700 px-2 py-0.5 rounded-full">Live</span>
              </a>
              <a
                href="/news"
                onClick={handleLinkClick}
                className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
              >
                <span className="mr-3">📰</span>
                News
              </a>
              <a
                href="/leaderboard"
                onClick={handleLinkClick}
                className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
              >
                <span className="mr-3">🏆</span>
                Leaderboard
              </a>
            </div>

            {/* Authenticated User Links */}
            {isAuthenticated && (
              <div className="pt-4 mt-4 border-t border-border-default space-y-1">
                <p className="px-3 text-xs font-semibold text-text-tertiary uppercase tracking-wider">
                  Trading
                </p>
                <a
                  href="/portfolio"
                  onClick={handleLinkClick}
                  className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
                >
                  <span className="mr-3">💼</span>
                  Portfolio
                </a>
                <a
                  href="/strategies"
                  onClick={handleLinkClick}
                  className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
                >
                  <span className="mr-3">🎯</span>
                  Strategies
                </a>
                <a
                  href="/analytics"
                  onClick={handleLinkClick}
                  className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
                >
                  <span className="mr-3">📊</span>
                  Analytics
                </a>
              </div>
            )}

            {/* Settings */}
            <div className="pt-4 mt-4 border-t border-border-default space-y-1">
              <p className="px-3 text-xs font-semibold text-text-tertiary uppercase tracking-wider">
                Settings
              </p>
              {isAuthenticated && (
                <a
                  href="/profile"
                  onClick={handleLinkClick}
                  className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
                >
                  <span className="mr-3">👤</span>
                  Profile
                </a>
              )}
              <a
                href="/preferences"
                onClick={handleLinkClick}
                className="mobile-nav-link flex items-center px-3 py-2 text-base font-medium text-text-primary hover:text-brand-500 hover:bg-background-secondary rounded-md transition-colors"
              >
                <span className="mr-3">⚙️</span>
                Preferences
              </a>
            </div>
          </div>
        </nav>

        {/* Auth Actions */}
        {showAuthActions && (
          <div className="p-4 border-t border-border-default">
            {isAuthenticated ? (
              <button
                onClick={handleLogout}
                className="w-full btn-secondary text-center"
              >
                Sign Out
              </button>
            ) : (
              <div className="space-y-3">
                <a
                  href="/login"
                  onClick={handleLinkClick}
                  className="w-full btn-ghost text-center block"
                >
                  Sign In
                </a>
                <a
                  href="/register"
                  onClick={handleLinkClick}
                  className="w-full btn-primary text-center block"
                >
                  Get Started
                </a>
              </div>
            )}
          </div>
        )}
      </div>
    </>
  );
};

export default MobileMenu;