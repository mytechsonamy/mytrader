/**
 * Top navigation bar component
 * Responsive design: Desktop top navbar with hamburger menu for mobile
 */

import React, { useState } from 'react';
import { useAuthStore } from '../../store/authStore';
import { cn } from '../../utils';
import BrandLogo from './BrandLogo';
import MobileMenu from './MobileMenu';
import UserMenu from './UserMenu';
import type { BaseComponentProps } from '../../types';

export interface TopNavbarProps extends BaseComponentProps {
  showAuthActions?: boolean;
  variant?: 'default' | 'transparent' | 'solid';
  size?: 'sm' | 'md' | 'lg';
}

const TopNavbar: React.FC<TopNavbarProps> = ({
  className,
  showAuthActions = true,
  variant = 'default',
  size = 'md',
  ...props
}) => {
  const { isAuthenticated, user } = useAuthStore();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const toggleMobileMenu = () => {
    setIsMobileMenuOpen(!isMobileMenuOpen);
  };

  const closeMobileMenu = () => {
    setIsMobileMenuOpen(false);
  };

  const navbarStyles = {
    default: 'bg-white border-b border-border-default shadow-sm',
    transparent: 'bg-transparent',
    solid: 'bg-background-secondary border-b border-border-default',
  };

  const sizeStyles = {
    sm: 'h-14',
    md: 'h-16',
    lg: 'h-20',
  };

  return (
    <>
      <header
        className={cn(
          'sticky top-0 z-40 w-full',
          navbarStyles[variant],
          sizeStyles[size],
          className
        )}
        {...props}
      >
        <div className="container-wide h-full">
          <div className="flex items-center justify-between h-full">
            {/* Brand Section */}
            <div className="flex items-center space-x-4">
              <BrandLogo
                size={size === 'lg' ? 'lg' : 'md'}
                showBadge={true}
              />
            </div>

            {/* Desktop Navigation */}
            <nav className="hidden lg:flex items-center space-x-8" role="navigation">
              <a
                href="/"
                className="nav-link text-text-primary hover:text-brand-500 transition-colors"
              >
                Dashboard
              </a>
              <a
                href="/market"
                className="nav-link text-text-primary hover:text-brand-500 transition-colors"
              >
                Market Data
              </a>
              <a
                href="/news"
                className="nav-link text-text-primary hover:text-brand-500 transition-colors"
              >
                News
              </a>
              <a
                href="/leaderboard"
                className="nav-link text-text-primary hover:text-brand-500 transition-colors"
              >
                Leaderboard
              </a>
              {isAuthenticated && (
                <>
                  <a
                    href="/portfolio"
                    className="nav-link text-text-primary hover:text-brand-500 transition-colors"
                  >
                    Portfolio
                  </a>
                  <a
                    href="/strategies"
                    className="nav-link text-text-primary hover:text-brand-500 transition-colors"
                  >
                    Strategies
                  </a>
                </>
              )}
            </nav>

            {/* Right Actions */}
            <div className="flex items-center space-x-4">
              {/* Auth Actions for Desktop */}
              {showAuthActions && (
                <div className="hidden lg:flex items-center space-x-3">
                  {isAuthenticated && user ? (
                    <UserMenu user={user} />
                  ) : (
                    <>
                      <a
                        href="/login"
                        className="btn-ghost text-text-primary hover:text-brand-500"
                      >
                        Sign In
                      </a>
                      <a
                        href="/register"
                        className="btn-primary"
                      >
                        Get Started
                      </a>
                    </>
                  )}
                </div>
              )}

              {/* Mobile Menu Toggle */}
              <button
                onClick={toggleMobileMenu}
                className="lg:hidden p-2 rounded-md text-text-primary hover:text-brand-500 hover:bg-background-secondary transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500"
                aria-label="Toggle navigation menu"
                aria-expanded={isMobileMenuOpen}
              >
                <div className="w-6 h-6 flex flex-col justify-center items-center">
                  <span
                    className={cn(
                      'block h-0.5 w-6 bg-current transition-all duration-300 ease-out',
                      isMobileMenuOpen ? 'rotate-45 translate-y-1' : '-translate-y-0.5'
                    )}
                  />
                  <span
                    className={cn(
                      'block h-0.5 w-6 bg-current transition-all duration-300 ease-out',
                      isMobileMenuOpen ? 'opacity-0' : 'opacity-100'
                    )}
                  />
                  <span
                    className={cn(
                      'block h-0.5 w-6 bg-current transition-all duration-300 ease-out',
                      isMobileMenuOpen ? '-rotate-45 -translate-y-1' : 'translate-y-0.5'
                    )}
                  />
                </div>
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Mobile Menu */}
      <MobileMenu
        isOpen={isMobileMenuOpen}
        onClose={closeMobileMenu}
        showAuthActions={showAuthActions}
      />
    </>
  );
};

export default TopNavbar;