/**
 * User menu dropdown component for authenticated users
 * Features: User avatar, dropdown menu, profile actions
 */

import React, { useState, useRef, useEffect } from 'react';
import { useAuthStore } from '../../store/authStore';
import { cn } from '../../utils';
import type { BaseComponentProps, User } from '../../types';

export interface UserMenuProps extends BaseComponentProps {
  user: User;
  size?: 'sm' | 'md' | 'lg';
}

const UserMenu: React.FC<UserMenuProps> = ({
  user,
  size = 'md',
  className,
  ...props
}) => {
  const { logout } = useAuthStore();
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  const sizeStyles = {
    sm: {
      avatar: 'w-7 h-7 text-xs',
      dropdown: 'w-48',
    },
    md: {
      avatar: 'w-8 h-8 text-sm',
      dropdown: 'w-56',
    },
    lg: {
      avatar: 'w-10 h-10 text-base',
      dropdown: 'w-64',
    },
  };

  const styles = sizeStyles[size];

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        menuRef.current &&
        !menuRef.current.contains(event.target as Node) &&
        buttonRef.current &&
        !buttonRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [isOpen]);

  // Handle escape key
  useEffect(() => {
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && isOpen) {
        setIsOpen(false);
        buttonRef.current?.focus();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      return () => document.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen]);

  const toggleMenu = () => {
    setIsOpen(!isOpen);
  };

  const handleLogout = () => {
    logout();
    setIsOpen(false);
  };

  const displayName = user.firstName
    ? `${user.firstName} ${user.lastName || ''}`.trim()
    : user.username;

  const avatarInitial = user.firstName
    ? user.firstName[0].toUpperCase()
    : user.email[0].toUpperCase();

  return (
    <div className={cn('relative', className)} {...props}>
      {/* User Avatar Button */}
      <button
        ref={buttonRef}
        onClick={toggleMenu}
        className={cn(
          'flex items-center space-x-2 rounded-full border border-border-default bg-white hover:bg-background-secondary transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2',
          'px-2 py-1'
        )}
        aria-expanded={isOpen}
        aria-haspopup="menu"
        aria-label="User menu"
      >
        {/* Avatar */}
        <div
          className={cn(
            'flex items-center justify-center rounded-full bg-brand-500 text-white font-semibold',
            styles.avatar
          )}
        >
          {user.avatar ? (
            <img
              src={user.avatar}
              alt={displayName}
              className="w-full h-full rounded-full object-cover"
            />
          ) : (
            avatarInitial
          )}
        </div>

        {/* User Name and Dropdown Arrow */}
        <div className="hidden sm:flex items-center space-x-2">
          <span className="text-sm font-medium text-text-primary truncate max-w-24">
            {displayName}
          </span>
          <svg
            className={cn(
              'w-4 h-4 text-text-tertiary transition-transform duration-200',
              isOpen ? 'rotate-180' : 'rotate-0'
            )}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 9l-7 7-7-7"
            />
          </svg>
        </div>
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div
          ref={menuRef}
          className={cn(
            'absolute right-0 mt-2 bg-white rounded-lg shadow-lg border border-border-default py-2 z-50',
            styles.dropdown
          )}
          role="menu"
          aria-orientation="vertical"
        >
          {/* User Info */}
          <div className="px-4 py-3 border-b border-border-default">
            <p className="text-sm font-medium text-text-primary truncate">
              {displayName}
            </p>
            <p className="text-xs text-text-tertiary truncate">
              {user.email}
            </p>
          </div>

          {/* Menu Items */}
          <div className="py-1">
            <a
              href="/profile"
              className="block px-4 py-2 text-sm text-text-primary hover:bg-background-secondary transition-colors"
              role="menuitem"
            >
              <span className="flex items-center">
                <span className="mr-3">👤</span>
                Profile Settings
              </span>
            </a>
            <a
              href="/preferences"
              className="block px-4 py-2 text-sm text-text-primary hover:bg-background-secondary transition-colors"
              role="menuitem"
            >
              <span className="flex items-center">
                <span className="mr-3">⚙️</span>
                Preferences
              </span>
            </a>
            <a
              href="/portfolio"
              className="block px-4 py-2 text-sm text-text-primary hover:bg-background-secondary transition-colors"
              role="menuitem"
            >
              <span className="flex items-center">
                <span className="mr-3">💼</span>
                My Portfolio
              </span>
            </a>
            <a
              href="/strategies"
              className="block px-4 py-2 text-sm text-text-primary hover:bg-background-secondary transition-colors"
              role="menuitem"
            >
              <span className="flex items-center">
                <span className="mr-3">🎯</span>
                My Strategies
              </span>
            </a>
          </div>

          {/* Separator */}
          <div className="border-t border-border-default my-1"></div>

          {/* Logout */}
          <button
            onClick={handleLogout}
            className="block w-full text-left px-4 py-2 text-sm text-negative-600 hover:bg-negative-50 transition-colors"
            role="menuitem"
          >
            <span className="flex items-center">
              <span className="mr-3">🚪</span>
              Sign Out
            </span>
          </button>
        </div>
      )}
    </div>
  );
};

export default UserMenu;