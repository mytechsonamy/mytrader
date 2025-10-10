/**
 * Authenticated layout component for logged-in users
 * Features: Top navbar, collapsible sidebar, responsive design
 */

import React from 'react';
import { useUIStore } from '../../store/uiStore';
import { cn } from '../../utils';
import TopNavbar from './TopNavbar';
import Sidebar from './Sidebar';
import Footer from '../Footer';
import type { BaseComponentProps, ChildrenProps } from '../../types';

export interface AuthenticatedLayoutProps extends BaseComponentProps, ChildrenProps {
  showSidebar?: boolean;
  sidebarCollapsed?: boolean;
  headerClassName?: string;
  sidebarClassName?: string;
  mainClassName?: string;
  footerClassName?: string;
}

const AuthenticatedLayout: React.FC<AuthenticatedLayoutProps> = ({
  children,
  className,
  showSidebar = true,
  sidebarCollapsed: propSidebarCollapsed,
  headerClassName,
  sidebarClassName,
  mainClassName,
  footerClassName,
  ...props
}) => {
  const { sidebar } = useUIStore();
  const sidebarCollapsed = propSidebarCollapsed ?? !sidebar.isOpen;

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
        showAuthActions={true}
        className={headerClassName}
      />

      {/* Main Layout */}
      <div className="flex flex-1 overflow-hidden">
        {/* Sidebar */}
        {showSidebar && (
          <Sidebar
            isCollapsed={sidebarCollapsed}
            className={sidebarClassName}
          />
        )}

        {/* Main Content Area */}
        <main
          className={cn(
            'flex-1 overflow-auto',
            'transition-all duration-300 ease-in-out',
            showSidebar && !sidebarCollapsed ? 'lg:ml-64' : '',
            showSidebar && sidebarCollapsed ? 'lg:ml-16' : '',
            mainClassName
          )}
          role="main"
        >
          <div className="container-wide py-6">
            {children}
          </div>
        </main>
      </div>

      {/* Footer */}
      <Footer
        className={cn(
          'mt-auto',
          showSidebar && !sidebarCollapsed ? 'lg:ml-64' : '',
          showSidebar && sidebarCollapsed ? 'lg:ml-16' : '',
          footerClassName
        )}
      />
    </div>
  );
};

export default AuthenticatedLayout;