/**
 * Utility functions for MyTrader application
 */

import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

// API configuration
export function getApiBaseUrl(): string {
  return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5002';
}

// Error handling
export function getErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }
  if (typeof error === 'string') {
    return error;
  }
  return 'An unknown error occurred';
}

// Tailwind CSS class utility
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

// Format currency values
export function formatCurrency(
  value: number,
  currency: string = 'USD',
  options: Intl.NumberFormatOptions = {}
): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 8, // Support low-price assets with more decimals
    ...options,
  }).format(value);
}

// Format percentage values
export function formatPercentage(
  value: number,
  options: Intl.NumberFormatOptions = {}
): string {
  return new Intl.NumberFormat('en-US', {
    style: 'percent',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    ...options,
  }).format(value / 100);
}

// Format large numbers with abbreviations
export function formatNumber(
  value: number,
  options: { compact?: boolean; precision?: number } = {}
): string {
  const { compact = false, precision = 2 } = options;

  if (!compact) {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 0,
      maximumFractionDigits: precision,
    }).format(value);
  }

  return new Intl.NumberFormat('en-US', {
    notation: 'compact',
    compactDisplay: 'short',
    minimumFractionDigits: 0,
    maximumFractionDigits: precision,
  }).format(value);
}

// Date formatting
export function formatDate(
  date: Date | string,
  options: Intl.DateTimeFormatOptions = {}
): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;

  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    ...options,
  }).format(dateObj);
}

export function formatDateTime(
  date: Date | string,
  options: Intl.DateTimeFormatOptions = {}
): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;

  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    ...options,
  }).format(dateObj);
}

export function formatRelativeTime(
  date: Date | string,
  baseDate: Date = new Date()
): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  const diffInSeconds = Math.floor((baseDate.getTime() - dateObj.getTime()) / 1000);

  if (diffInSeconds < 60) return 'just now';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
  if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)}d ago`;

  return formatDate(dateObj);
}

// String utilities
export function capitalize(str: string): string {
  return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
}

export function truncate(str: string, length: number = 50): string {
  if (str.length <= length) return str;
  return str.slice(0, length - 3) + '...';
}

export function kebabCase(str: string): string {
  return str
    .replace(/([a-z])([A-Z])/g, '$1-$2')
    .replace(/[\s_]+/g, '-')
    .toLowerCase();
}

export function camelCase(str: string): string {
  return str
    .replace(/(?:^\w|[A-Z]|\b\w)/g, (word, index) =>
      index === 0 ? word.toLowerCase() : word.toUpperCase()
    )
    .replace(/\s+/g, '');
}

// Array utilities
export function groupBy<T, K extends keyof any>(
  array: T[],
  key: (item: T) => K
): Record<K, T[]> {
  return array.reduce((groups, item) => {
    const group = key(item);
    groups[group] = groups[group] || [];
    groups[group].push(item);
    return groups;
  }, {} as Record<K, T[]>);
}

export function sortBy<T>(
  array: T[],
  key: keyof T | ((item: T) => any),
  direction: 'asc' | 'desc' = 'asc'
): T[] {
  const keyFn = typeof key === 'function' ? key : (item: T) => item[key];

  return [...array].sort((a, b) => {
    const aVal = keyFn(a);
    const bVal = keyFn(b);

    if (aVal < bVal) return direction === 'asc' ? -1 : 1;
    if (aVal > bVal) return direction === 'asc' ? 1 : -1;
    return 0;
  });
}

export function unique<T>(array: T[]): T[] {
  return [...new Set(array)];
}

export function chunk<T>(array: T[], size: number): T[][] {
  return Array.from({ length: Math.ceil(array.length / size) }, (_, index) =>
    array.slice(index * size, index * size + size)
  );
}

// Object utilities
export function pick<T, K extends keyof T>(obj: T, keys: K[]): Pick<T, K> {
  return keys.reduce((result, key) => {
    if (key in obj) {
      result[key] = obj[key];
    }
    return result;
  }, {} as Pick<T, K>);
}

export function omit<T, K extends keyof T>(obj: T, keys: K[]): Omit<T, K> {
  const result = { ...obj };
  keys.forEach((key) => delete result[key]);
  return result;
}

export function isEqual(a: any, b: any): boolean {
  if (a === b) return true;
  if (a == null || b == null) return false;
  if (Array.isArray(a) && Array.isArray(b)) {
    if (a.length !== b.length) return false;
    return a.every((item, index) => isEqual(item, b[index]));
  }
  if (typeof a === 'object' && typeof b === 'object') {
    const keysA = Object.keys(a);
    const keysB = Object.keys(b);
    if (keysA.length !== keysB.length) return false;
    return keysA.every((key) => isEqual(a[key], b[key]));
  }
  return false;
}

// Validation utilities
export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

export function isValidUrl(url: string): boolean {
  try {
    new URL(url);
    return true;
  } catch {
    return false;
  }
}

export function isValidPassword(password: string): boolean {
  // At least 8 characters, 1 uppercase, 1 lowercase, 1 number
  const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{8,}$/;
  return passwordRegex.test(password);
}

// Local storage utilities
export function getStorageItem<T>(key: string, defaultValue: T): T {
  try {
    const item = localStorage.getItem(key);
    return item ? JSON.parse(item) : defaultValue;
  } catch {
    return defaultValue;
  }
}

export function setStorageItem<T>(key: string, value: T): void {
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch (error) {
    console.warn('Failed to save to localStorage:', error);
  }
}

export function removeStorageItem(key: string): void {
  try {
    localStorage.removeItem(key);
  } catch (error) {
    console.warn('Failed to remove from localStorage:', error);
  }
}

// Debounce and throttle
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout;

  return (...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
}

export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number
): (...args: Parameters<T>) => void {
  let inThrottle: boolean;

  return (...args: Parameters<T>) => {
    if (!inThrottle) {
      func(...args);
      inThrottle = true;
      setTimeout(() => (inThrottle = false), limit);
    }
  };
}

// Environment utilities
export function isDevelopment(): boolean {
  return import.meta.env.DEV;
}

export function isProduction(): boolean {
  return import.meta.env.PROD;
}

export function getWebSocketUrl(): string {
  return import.meta.env.VITE_WS_BASE_URL || 'ws://localhost:5002';
}

// Error handling utilities
export function createErrorHandler(context: string) {
  return (error: unknown) => {
    const message = getErrorMessage(error);
    console.error(`[${context}] Error:`, message, error);
    return message;
  };
}

// Performance utilities
export function measurePerformance<T>(
  fn: () => T,
  label: string = 'Operation'
): T {
  const start = performance.now();
  const result = fn();
  const end = performance.now();

  console.log(`${label} took ${(end - start).toFixed(2)}ms`);
  return result;
}

// Random utilities
export function generateId(length: number = 8): string {
  return Math.random()
    .toString(36)
    .substring(2, length + 2);
}

export function randomBetween(min: number, max: number): number {
  return Math.random() * (max - min) + min;
}

export function randomChoice<T>(array: T[]): T {
  return array[Math.floor(Math.random() * array.length)];
}

// Trading utilities
export function calculatePercentageChange(current: number, previous: number): number {
  if (previous === 0) return 0;
  return ((current - previous) / previous) * 100;
}

export function formatPriceChange(
  current: number,
  previous: number,
  options: { showSign?: boolean; precision?: number } = {}
): string {
  const { showSign = true, precision = 2 } = options;
  const change = calculatePercentageChange(current, previous);
  const formatted = change.toFixed(precision);

  if (showSign && change > 0) {
    return `+${formatted}%`;
  }
  return `${formatted}%`;
}

export function getChangeColor(value: number): string {
  if (value > 0) return 'text-green-600';
  if (value < 0) return 'text-red-600';
  return 'text-gray-600';
}

export function formatPrice(
  price: number,
  options: { precision?: number; showCurrency?: boolean } = {}
): string {
  const { precision = 2, showCurrency = true } = options;

  if (showCurrency) {
    return formatCurrency(price, 'USD', {
      minimumFractionDigits: precision,
      maximumFractionDigits: precision
    });
  }

  return price.toFixed(precision);
}

// Export enhanced price formatting utilities
export {
  normalizePrice,
  formatPrice as formatPriceEnhanced,
  formatPercentageChange,
  formatLargeNumber,
  normalizeMarketData,
  needsNormalization
} from './priceFormatting';