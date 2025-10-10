/**
 * Brand logo component with consistent styling
 * Includes the myTrader logo, Pro badge, and Techsonamy attribution
 */

import React from 'react';
import { cn } from '../../utils';
import type { BaseComponentProps } from '../../types';

export interface BrandLogoProps extends BaseComponentProps {
  size?: 'sm' | 'md' | 'lg';
  showBadge?: boolean;
  showPoweredBy?: boolean;
  variant?: 'default' | 'light' | 'dark';
  href?: string;
}

const BrandLogo: React.FC<BrandLogoProps> = ({
  className,
  size = 'md',
  showBadge = true,
  showPoweredBy = true,
  variant = 'default',
  href = '/',
  ...props
}) => {
  const sizeStyles = {
    sm: {
      icon: 'w-8 h-8 text-sm',
      text: 'text-lg',
      badge: 'text-xs px-1.5 py-0.5',
      poweredBy: 'text-xs',
    },
    md: {
      icon: 'w-10 h-10 text-base',
      text: 'text-xl',
      badge: 'text-xs px-2 py-1',
      poweredBy: 'text-sm',
    },
    lg: {
      icon: 'w-12 h-12 text-lg',
      text: 'text-2xl',
      badge: 'text-sm px-2.5 py-1',
      poweredBy: 'text-base',
    },
  };

  const variantStyles = {
    default: {
      icon: 'bg-brand-500 text-white',
      text: 'text-text-primary',
      badge: 'bg-brand-600 text-white',
      poweredBy: 'text-text-tertiary',
      techsonamy: 'text-brand-500 font-semibold',
    },
    light: {
      icon: 'bg-white text-brand-500',
      text: 'text-white',
      badge: 'bg-white/20 text-white',
      poweredBy: 'text-white/80',
      techsonamy: 'text-white font-semibold',
    },
    dark: {
      icon: 'bg-gray-800 text-white',
      text: 'text-gray-800',
      badge: 'bg-gray-700 text-white',
      poweredBy: 'text-gray-600',
      techsonamy: 'text-gray-800 font-semibold',
    },
  };

  const styles = sizeStyles[size];
  const colors = variantStyles[variant];

  const logoContent = (
    <div
      className={cn(
        'flex items-center space-x-3',
        className
      )}
      {...props}
    >
      {/* Logo Icon */}
      <div
        className={cn(
          'rounded-lg flex items-center justify-center font-bold',
          styles.icon,
          colors.icon
        )}
      >
        T
      </div>

      {/* Logo Text and Badge */}
      <div className="flex flex-col">
        <div className="flex items-center space-x-2">
          <h1
            className={cn(
              'font-bold leading-none',
              styles.text,
              colors.text
            )}
          >
            myTrader
          </h1>
          {showBadge && (
            <span
              className={cn(
                'rounded-full font-medium leading-none',
                styles.badge,
                colors.badge
              )}
            >
              Pro
            </span>
          )}
        </div>

        {/* Powered by Techsonamy */}
        {showPoweredBy && (
          <div
            className={cn(
              'flex items-center space-x-1 leading-none mt-0.5',
              styles.poweredBy
            )}
          >
            <span className={colors.poweredBy}>Powered by</span>
            <span className={colors.techsonamy}>Techsonamy</span>
          </div>
        )}
      </div>
    </div>
  );

  // If href is provided, wrap in a link
  if (href) {
    return (
      <a
        href={href}
        className="flex items-center transition-opacity hover:opacity-80 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2 rounded-lg"
        aria-label="myTrader homepage"
      >
        {logoContent}
      </a>
    );
  }

  return logoContent;
};

export default BrandLogo;