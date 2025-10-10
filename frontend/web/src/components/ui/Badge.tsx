/**
 * Badge component for status indicators and labels
 */

import React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../utils';
import type { BaseComponentProps, ChildrenProps } from '../../types';

// Badge variants
const badgeVariants = cva(
  // Base styles
  'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2 max-w-fit whitespace-nowrap overflow-hidden text-ellipsis',
  {
    variants: {
      variant: {
        default: 'bg-background-secondary text-text-primary border border-border-default',
        primary: 'bg-brand-500 text-white',
        secondary: 'bg-purple-500 text-white',
        success: 'bg-positive-50 text-positive-800 border border-positive-200',
        error: 'bg-negative-50 text-negative-800 border border-negative-200',
        warning: 'bg-warning-50 text-warning-800 border border-warning-200',
        info: 'bg-blue-50 text-blue-800 border border-blue-200',
        outline: 'text-text-primary border border-border-default bg-transparent',
        ghost: 'text-text-primary bg-transparent',
        // Market-specific variants
        bullish: 'bg-positive-50 text-positive-800 border border-positive-200',
        bearish: 'bg-negative-50 text-negative-800 border border-negative-200',
        neutral: 'bg-gray-50 text-gray-800 border border-gray-200',
        // Market status variants
        open: 'bg-positive-50 text-positive-800 border border-positive-200',
        closed: 'bg-negative-50 text-negative-800 border border-negative-200',
        preMarket: 'bg-warning-50 text-warning-800 border border-warning-200',
        afterMarket: 'bg-warning-50 text-warning-800 border border-warning-200',
      },
      size: {
        sm: 'px-2 py-0.5 text-xs',
        md: 'px-2.5 py-0.5 text-xs',
        lg: 'px-3 py-1 text-sm',
      },
      rounded: {
        sm: 'rounded-sm',
        md: 'rounded-md',
        lg: 'rounded-lg',
        full: 'rounded-full',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'md',
      rounded: 'full',
    },
  }
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants>,
    BaseComponentProps,
    ChildrenProps {
  icon?: React.ReactNode;
  removable?: boolean;
  onRemove?: () => void;
}

const Badge = React.forwardRef<HTMLDivElement, BadgeProps>(
  (
    {
      className,
      variant,
      size,
      rounded,
      icon,
      removable = false,
      onRemove,
      children,
      ...props
    },
    ref
  ) => {
    return (
      <div
        ref={ref}
        className={cn(badgeVariants({ variant, size, rounded }), className)}
        {...props}
      >
        {icon && <span className="mr-1">{icon}</span>}
        {children}
        {removable && onRemove && (
          <button
            type="button"
            className="ml-1.5 inline-flex items-center justify-center w-3 h-3 rounded-full hover:bg-black/10 focus:outline-none focus:ring-1 focus:ring-offset-1 focus:ring-current"
            onClick={onRemove}
            aria-label="Remove"
          >
            <svg
              className="w-2 h-2"
              fill="currentColor"
              viewBox="0 0 20 20"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path
                fillRule="evenodd"
                d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                clipRule="evenodd"
              />
            </svg>
          </button>
        )}
      </div>
    );
  }
);

Badge.displayName = 'Badge';

// Market-specific badge helpers
export const MarketStatusBadge: React.FC<{
  status: string;
  className?: string;
}> = ({ status, className }) => {
  const getStatusVariant = (status: string) => {
    switch (status.toUpperCase()) {
      case 'OPEN':
        return 'open';
      case 'CLOSED':
        return 'closed';
      case 'PRE_MARKET':
        return 'preMarket';
      case 'AFTER_MARKET':
        return 'afterMarket';
      default:
        return 'neutral';
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status.toUpperCase()) {
      case 'OPEN':
        return 'Open';
      case 'CLOSED':
        return 'Closed';
      case 'PRE_MARKET':
        return 'Pre-Market';
      case 'AFTER_MARKET':
        return 'After-Market';
      default:
        return status;
    }
  };

  return (
    <Badge
      variant={getStatusVariant(status)}
      className={className}
    >
      {getStatusLabel(status)}
    </Badge>
  );
};

// Price change badge
export const PriceChangeBadge: React.FC<{
  change: number;
  changePercent: number;
  className?: string;
}> = ({ change, changePercent, className }) => {
  const variant = change > 0 ? 'bullish' : change < 0 ? 'bearish' : 'neutral';
  const sign = change >= 0 ? '+' : '';
  const icon = change > 0 ? '↗' : change < 0 ? '↘' : '→';

  return (
    <Badge
      variant={variant}
      icon={<span className="text-xs">{icon}</span>}
      className={className}
    >
      {sign}{changePercent.toFixed(2)}%
    </Badge>
  );
};

// Asset class badge
export const AssetClassBadge: React.FC<{
  assetClass: string;
  className?: string;
}> = ({ assetClass, className }) => {
  const getAssetClassIcon = (assetClass: string) => {
    switch (assetClass.toUpperCase()) {
      case 'CRYPTO':
        return '₿';
      case 'STOCK':
        return '📈';
      case 'ETF':
        return '📊';
      case 'FOREX':
        return '💱';
      default:
        return '💼';
    }
  };

  return (
    <Badge
      variant="outline"
      icon={getAssetClassIcon(assetClass)}
      className={className}
    >
      {assetClass}
    </Badge>
  );
};

export { Badge, badgeVariants };