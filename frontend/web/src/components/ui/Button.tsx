/**
 * Button component with design system variants
 */

import React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../utils';
import type { BaseComponentProps, ChildrenProps } from '../../types';

// Button variants using CVA (Class Variance Authority)
const buttonVariants = cva(
  // Base styles
  'inline-flex items-center justify-center rounded-md text-sm font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none',
  {
    variants: {
      variant: {
        primary: 'btn-primary',
        secondary: 'btn-secondary',
        outline: 'border border-border-default bg-transparent hover:bg-background-secondary',
        ghost: 'hover:bg-background-secondary hover:text-text-primary',
        link: 'underline-offset-4 hover:underline text-brand-primary',
        destructive: 'bg-negative-500 text-white hover:bg-negative-600',
      },
      size: {
        sm: 'h-8 px-3 text-xs',
        md: 'h-10 px-4 py-2',
        lg: 'h-12 px-6 py-3',
        icon: 'h-10 w-10',
      },
      fullWidth: {
        true: 'w-full',
        false: '',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
      fullWidth: false,
    },
  }
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants>,
    BaseComponentProps {
  loading?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  children: React.ReactNode;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant,
      size,
      fullWidth,
      loading = false,
      leftIcon,
      rightIcon,
      disabled,
      children,
      ...props
    },
    ref
  ) => {
    const isDisabled = disabled || loading;

    return (
      <button
        ref={ref}
        className={cn(buttonVariants({ variant, size, fullWidth }), className)}
        disabled={isDisabled}
        {...props}
      >
        {loading && (
          <div className="mr-2">
            <div className="loading-spinner w-4 h-4" />
          </div>
        )}
        {!loading && leftIcon && <div className="mr-2">{leftIcon}</div>}
        {children}
        {!loading && rightIcon && <div className="ml-2">{rightIcon}</div>}
      </button>
    );
  }
);

Button.displayName = 'Button';

export { Button, buttonVariants };