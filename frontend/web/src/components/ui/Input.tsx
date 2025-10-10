/**
 * Input component with form integration
 */

import React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../utils';
import type { BaseComponentProps } from '../../types';

// Input variants
const inputVariants = cva(
  // Base styles
  'flex w-full rounded-md border px-3 py-2 text-sm ring-offset-background-primary file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-text-quaternary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'border-border-default bg-background-tertiary focus-visible:border-brand-500',
        error: 'border-negative-500 bg-background-tertiary focus-visible:border-negative-500 focus-visible:ring-negative-500',
        success: 'border-positive-500 bg-background-tertiary focus-visible:border-positive-500 focus-visible:ring-positive-500',
        ghost: 'border-transparent bg-transparent focus-visible:border-brand-500',
      },
      size: {
        sm: 'h-8 px-2 text-xs',
        md: 'h-10 px-3 text-sm',
        lg: 'h-12 px-4 text-base',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'md',
    },
  }
);

export interface InputProps
  extends React.InputHTMLAttributes<HTMLInputElement>,
    VariantProps<typeof inputVariants>,
    BaseComponentProps {
  label?: string;
  error?: string;
  helperText?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  loading?: boolean;
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  (
    {
      className,
      variant,
      size,
      type = 'text',
      label,
      error,
      helperText,
      leftIcon,
      rightIcon,
      loading = false,
      disabled,
      id,
      ...props
    },
    ref
  ) => {
    const inputId = id || `input-${Math.random().toString(36).substr(2, 9)}`;
    const isDisabled = disabled || loading;
    const inputVariant = error ? 'error' : variant;

    return (
      <div className="w-full">
        {label && (
          <label
            htmlFor={inputId}
            className="block text-sm font-medium text-text-secondary mb-2"
          >
            {label}
          </label>
        )}

        <div className="relative">
          {leftIcon && (
            <div className="absolute left-3 top-1/2 transform -translate-y-1/2 text-text-tertiary">
              {leftIcon}
            </div>
          )}

          <input
            ref={ref}
            id={inputId}
            type={type}
            className={cn(
              inputVariants({ variant: inputVariant, size }),
              leftIcon && 'pl-10',
              (rightIcon || loading) && 'pr-10',
              className
            )}
            disabled={isDisabled}
            {...props}
          />

          {(rightIcon || loading) && (
            <div className="absolute right-3 top-1/2 transform -translate-y-1/2 text-text-tertiary">
              {loading ? (
                <div className="loading-spinner w-4 h-4" />
              ) : (
                rightIcon
              )}
            </div>
          )}
        </div>

        {(error || helperText) && (
          <p
            className={cn(
              'mt-2 text-xs',
              error ? 'text-negative-500' : 'text-text-tertiary'
            )}
          >
            {error || helperText}
          </p>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';

// Textarea variant
export interface TextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement>,
    BaseComponentProps {
  label?: string;
  error?: string;
  helperText?: string;
  resize?: 'none' | 'vertical' | 'horizontal' | 'both';
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  (
    {
      className,
      label,
      error,
      helperText,
      resize = 'vertical',
      disabled,
      id,
      ...props
    },
    ref
  ) => {
    const textareaId = id || `textarea-${Math.random().toString(36).substr(2, 9)}`;

    return (
      <div className="w-full">
        {label && (
          <label
            htmlFor={textareaId}
            className="block text-sm font-medium text-text-secondary mb-2"
          >
            {label}
          </label>
        )}

        <textarea
          ref={ref}
          id={textareaId}
          className={cn(
            inputVariants({ variant: error ? 'error' : 'default', size: 'md' }),
            'min-h-[80px]',
            {
              'resize-none': resize === 'none',
              'resize-y': resize === 'vertical',
              'resize-x': resize === 'horizontal',
              'resize': resize === 'both',
            },
            className
          )}
          disabled={disabled}
          {...props}
        />

        {(error || helperText) && (
          <p
            className={cn(
              'mt-2 text-xs',
              error ? 'text-negative-500' : 'text-text-tertiary'
            )}
          >
            {error || helperText}
          </p>
        )}
      </div>
    );
  }
);

Textarea.displayName = 'Textarea';

export { Input, Textarea, inputVariants };