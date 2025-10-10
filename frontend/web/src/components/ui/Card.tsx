/**
 * Card component with mobile-inspired design
 */

import React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../utils';
import type { BaseComponentProps, ChildrenProps } from '../../types';

// Card variants
const cardVariants = cva(
  // Base styles
  'card',
  {
    variants: {
      variant: {
        default: 'bg-background-card border border-border-subtle',
        elevated: 'bg-background-card border border-border-subtle shadow-card-hover',
        outline: 'bg-transparent border border-border-default',
        ghost: 'bg-transparent border-0',
        glass: 'glass border border-white/20',
      },
      size: {
        sm: 'p-3',
        md: 'p-4',
        lg: 'p-6',
        xl: 'p-8',
      },
      interactive: {
        true: 'card-interactive cursor-pointer',
        false: '',
      },
      padding: {
        none: 'p-0',
        sm: 'p-3',
        md: 'p-4',
        lg: 'p-6',
        xl: 'p-8',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'md',
      interactive: false,
    },
  }
);

export interface CardProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof cardVariants>,
    BaseComponentProps,
    ChildrenProps {}

const Card = React.forwardRef<HTMLDivElement, CardProps>(
  ({ className, variant, size, interactive, padding, children, ...props }, ref) => {
    // Use padding variant if specified, otherwise fall back to size
    const paddingClass = padding ? cardVariants({ variant, padding, interactive }) : cardVariants({ variant, size, interactive });

    return (
      <div
        ref={ref}
        className={cn(paddingClass, className)}
        {...props}
      >
        {children}
      </div>
    );
  }
);

Card.displayName = 'Card';

// Card Header component
const CardHeader = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement> & BaseComponentProps
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn('flex flex-col space-y-1.5 pb-4', className)}
    {...props}
  />
));

CardHeader.displayName = 'CardHeader';

// Card Title component
const CardTitle = React.forwardRef<
  HTMLHeadingElement,
  React.HTMLAttributes<HTMLHeadingElement> & BaseComponentProps
>(({ className, ...props }, ref) => (
  <h3
    ref={ref}
    className={cn('text-lg font-semibold leading-none tracking-tight text-text-primary', className)}
    {...props}
  />
));

CardTitle.displayName = 'CardTitle';

// Card Description component
const CardDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement> & BaseComponentProps
>(({ className, ...props }, ref) => (
  <p
    ref={ref}
    className={cn('text-sm text-text-tertiary', className)}
    {...props}
  />
));

CardDescription.displayName = 'CardDescription';

// Card Content component
const CardContent = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement> & BaseComponentProps
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn('', className)}
    {...props}
  />
));

CardContent.displayName = 'CardContent';

// Card Footer component
const CardFooter = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement> & BaseComponentProps
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn('flex items-center pt-4', className)}
    {...props}
  />
));

CardFooter.displayName = 'CardFooter';

export {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
  cardVariants,
};