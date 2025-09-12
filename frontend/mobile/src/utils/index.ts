/**
 * Utility functions for the mobile app
 */

/**
 * Safely parse a number from a string or return a default value
 */
export const safeParseNumber = (value: unknown, defaultValue = 0): number => {
  if (typeof value === 'number' && !isNaN(value)) return value;
  if (typeof value === 'string') {
    const parsed = parseFloat(value);
    return isNaN(parsed) ? defaultValue : parsed;
  }
  return defaultValue;
};

/**
 * Format a number as currency
 */
export const formatCurrency = (
  value: number, 
  currency = 'USD',
  minimumFractionDigits = 2,
  maximumFractionDigits = 6
): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits,
    maximumFractionDigits,
  }).format(value);
};

/**
 * Format a percentage value
 */
export const formatPercentage = (
  value: number,
  decimalPlaces = 2
): string => {
  return `${(value > 0 ? '+' : '')}${value.toFixed(decimalPlaces)}%`;
};

/**
 * Debounce a function
 */
export const debounce = <T extends (...args: unknown[]) => unknown>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let timeout: NodeJS.Timeout | null = null;
  
  return (...args: Parameters<T>) => {
    if (timeout) clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
};

/**
 * Sleep for a given number of milliseconds
 */
export const sleep = (ms: number): Promise<void> => {
  return new Promise(resolve => setTimeout(resolve, ms));
};

/**
 * Check if an object is empty
 */
export const isEmpty = (obj: Record<string, unknown> | unknown[]): boolean => {
  if (Array.isArray(obj)) return obj.length === 0;
  return Object.keys(obj).length === 0;
};