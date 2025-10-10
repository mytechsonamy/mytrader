/**
 * Public layout for non-authenticated users
 */

import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Button } from '../components/ui';
import { useAuthStore } from '../store/authStore';
import type { ChildrenProps } from '../types';

interface PublicLayoutProps extends ChildrenProps {
  showNavigation?: boolean;
  showFooter?: boolean;
}

const PublicLayout: React.FC<PublicLayoutProps> = ({
  children,
  showNavigation = true,
  showFooter = true,
}) => {
  const location = useLocation();
  const { isAuthenticated, isGuest } = useAuthStore();

  const navigation = [
    { name: 'Dashboard', href: '/', current: location.pathname === '/' },
    { name: 'Markets', href: '/markets', current: location.pathname === '/markets' },
    { name: 'Leaderboard', href: '/leaderboard', current: location.pathname === '/leaderboard' },
  ];

  return (
    <div className="min-h-screen bg-background-primary flex flex-col">
      {/* Navigation Header */}
      {showNavigation && (
        <header className="bg-background-tertiary border-b border-border-subtle sticky top-0 z-40">
          <div className="container-wide">
            <div className="flex items-center justify-between h-16">
              {/* Logo */}
              <div className="flex items-center">
                <Link to="/" className="flex items-center space-x-2">
                  <div className="w-8 h-8 bg-gradient-brand rounded-md flex items-center justify-center">
                    <span className="text-white font-bold text-lg">MT</span>
                  </div>
                  <span className="font-bold text-xl text-text-primary">myTrader</span>
                </Link>
              </div>

              {/* Navigation Links - Desktop */}
              <nav className="hidden md:flex space-x-8">
                {navigation.map((item) => (
                  <Link
                    key={item.name}
                    to={item.href}
                    className={`px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                      item.current
                        ? 'bg-brand-500 text-white'
                        : 'text-text-secondary hover:text-text-primary hover:bg-background-secondary'
                    }`}
                  >
                    {item.name}
                  </Link>
                ))}
              </nav>

              {/* Auth Buttons */}
              <div className="flex items-center space-x-4">
                {!isAuthenticated && !isGuest && (
                  <>
                    <Link to="/login">
                      <Button variant="ghost" size="sm">
                        Sign In
                      </Button>
                    </Link>
                    <Link to="/register">
                      <Button variant="primary" size="sm">
                        Get Started
                      </Button>
                    </Link>
                  </>
                )}

                {isAuthenticated && (
                  <div className="flex items-center space-x-4">
                    <Link to="/portfolio">
                      <Button variant="ghost" size="sm">
                        Portfolio
                      </Button>
                    </Link>
                    <div className="w-8 h-8 bg-brand-500 rounded-full flex items-center justify-center">
                      <span className="text-white text-sm font-medium">U</span>
                    </div>
                  </div>
                )}

                {isGuest && (
                  <div className="flex items-center space-x-2">
                    <span className="text-sm text-text-tertiary">Guest Mode</span>
                    <Link to="/login">
                      <Button variant="primary" size="sm">
                        Sign In
                      </Button>
                    </Link>
                  </div>
                )}

                {/* Mobile menu button */}
                <button
                  type="button"
                  className="md:hidden inline-flex items-center justify-center p-2 rounded-md text-text-secondary hover:text-text-primary hover:bg-background-secondary focus:outline-none focus:ring-2 focus:ring-brand-500"
                  aria-expanded="false"
                >
                  <span className="sr-only">Open main menu</span>
                  <svg
                    className="block h-6 w-6"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                  </svg>
                </button>
              </div>
            </div>
          </div>

          {/* Mobile Navigation Menu */}
          <div className="md:hidden border-t border-border-subtle">
            <div className="px-2 pt-2 pb-3 space-y-1">
              {navigation.map((item) => (
                <Link
                  key={item.name}
                  to={item.href}
                  className={`block px-3 py-2 rounded-md text-base font-medium transition-colors ${
                    item.current
                      ? 'bg-brand-500 text-white'
                      : 'text-text-secondary hover:text-text-primary hover:bg-background-secondary'
                  }`}
                >
                  {item.name}
                </Link>
              ))}
            </div>
          </div>
        </header>
      )}

      {/* Main Content */}
      <main className="flex-1">
        {children}
      </main>

      {/* Footer */}
      {showFooter && (
        <footer className="bg-background-secondary border-t border-border-subtle">
          <div className="container-wide py-8">
            <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
              {/* Company Info */}
              <div className="col-span-1 md:col-span-2">
                <div className="flex items-center space-x-2 mb-4">
                  <div className="w-8 h-8 bg-gradient-brand rounded-md flex items-center justify-center">
                    <span className="text-white font-bold text-lg">MT</span>
                  </div>
                  <span className="font-bold text-xl text-text-primary">myTrader</span>
                </div>
                <p className="text-text-tertiary text-sm mb-4">
                  Trade smarter with AI-powered insights and real-time market data.
                  Join thousands of traders making informed investment decisions.
                </p>
                <div className="flex space-x-4">
                  <a
                    href="#"
                    className="text-text-tertiary hover:text-text-primary transition-colors"
                    aria-label="Twitter"
                  >
                    <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M8.29 20.251c7.547 0 11.675-6.253 11.675-11.675 0-.178 0-.355-.012-.53A8.348 8.348 0 0022 5.92a8.19 8.19 0 01-2.357.646 4.118 4.118 0 001.804-2.27 8.224 8.224 0 01-2.605.996 4.107 4.107 0 00-6.993 3.743 11.65 11.65 0 01-8.457-4.287 4.106 4.106 0 001.27 5.477A4.072 4.072 0 012.8 9.713v.052a4.105 4.105 0 003.292 4.022 4.095 4.095 0 01-1.853.07 4.108 4.108 0 003.834 2.85A8.233 8.233 0 012 18.407a11.616 11.616 0 006.29 1.84" />
                    </svg>
                  </a>
                  <a
                    href="#"
                    className="text-text-tertiary hover:text-text-primary transition-colors"
                    aria-label="LinkedIn"
                  >
                    <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z" />
                    </svg>
                  </a>
                </div>
              </div>

              {/* Quick Links */}
              <div>
                <h3 className="text-sm font-semibold text-text-primary uppercase tracking-wider mb-4">
                  Platform
                </h3>
                <ul className="space-y-2">
                  <li>
                    <Link to="/markets" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                      Markets
                    </Link>
                  </li>
                  <li>
                    <Link to="/leaderboard" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                      Leaderboard
                    </Link>
                  </li>
                  <li>
                    <a href="#" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                      API Docs
                    </a>
                  </li>
                </ul>
              </div>

              {/* Support */}
              <div>
                <h3 className="text-sm font-semibold text-text-primary uppercase tracking-wider mb-4">
                  Support
                </h3>
                <ul className="space-y-2">
                  <li>
                    <a href="#" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                      Help Center
                    </a>
                  </li>
                  <li>
                    <a href="#" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                      Contact Us
                    </a>
                  </li>
                  <li>
                    <a href="#" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                      Privacy Policy
                    </a>
                  </li>
                  <li>
                    <a href="#" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                      Terms of Service
                    </a>
                  </li>
                </ul>
              </div>
            </div>

            {/* Bottom Bar */}
            <div className="mt-8 pt-8 border-t border-border-subtle">
              <div className="flex flex-col md:flex-row justify-between items-center">
                <p className="text-sm text-text-tertiary">
                  © 2024 myTrader. All rights reserved.
                </p>
                <div className="mt-4 md:mt-0 flex space-x-6">
                  <a href="#" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                    Privacy
                  </a>
                  <a href="#" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                    Terms
                  </a>
                  <a href="#" className="text-sm text-text-tertiary hover:text-text-primary transition-colors">
                    Cookies
                  </a>
                </div>
              </div>
            </div>
          </div>
        </footer>
      )}
    </div>
  );
};

export default PublicLayout;