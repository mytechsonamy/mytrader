/**
 * Public layout component for guest users
 * Features: Top navbar, responsive design, call-to-action areas
 */

import React from 'react';
import { Link } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { cn } from '../../utils';
import TopNavbar from './TopNavbar';
import Footer from '../Footer';
import type { BaseComponentProps, ChildrenProps } from '../../types';

export interface PublicLayoutProps extends BaseComponentProps, ChildrenProps {
  showCTA?: boolean;
  headerClassName?: string;
  mainClassName?: string;
  footerClassName?: string;
}

const PublicLayout: React.FC<PublicLayoutProps> = ({
  children,
  className,
  showCTA = true,
  headerClassName,
  mainClassName,
  footerClassName,
  ...props
}) => {
  const { isAuthenticated } = useAuthStore();

  return (
    <div
      className={cn(
        'min-h-screen bg-background-primary flex flex-col',
        className
      )}
      {...props}
    >
      {/* Top Navigation */}
      <TopNavbar
        showAuthActions={!isAuthenticated}
        className={headerClassName}
      />

      {/* Main Content */}
      <main
        className={cn(
          'flex-1 w-full',
          mainClassName
        )}
        role="main"
      >
        {children}
      </main>

      {/* Footer */}
      <Footer
        className={cn(
          'mt-auto',
          footerClassName
        )}
      />

      {/* Call-to-Action Section for Guests */}
      {showCTA && !isAuthenticated && (
        <section
          className="bg-gradient-brand text-white py-8"
          aria-labelledby="cta-heading"
        >
          <div className="container-wide text-center">
            <h2 id="cta-heading" className="text-2xl font-bold mb-4">
              Ready to Start Trading?
            </h2>
            <p className="text-lg opacity-90 mb-6 max-w-2xl mx-auto">
              Join thousands of traders using myTrader to make informed investment decisions.
              Get access to advanced analytics, portfolio tracking, and real-time alerts.
            </p>
            <div className="flex flex-col sm:flex-row justify-center gap-4">
              <Link
                to="/register"
                className="btn-primary bg-white text-brand-500 hover:bg-gray-100 focus:ring-2 focus:ring-white focus:ring-offset-2 focus:ring-offset-brand-500"
              >
                Create Free Account
              </Link>
              <Link
                to="/login"
                className="btn-secondary border-white text-white hover:bg-white/10 focus:ring-2 focus:ring-white focus:ring-offset-2 focus:ring-offset-brand-500"
              >
                Sign In
              </Link>
            </div>
          </div>
        </section>
      )}
    </div>
  );
};

export default PublicLayout;