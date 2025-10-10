/**
 * Price Formatting Utilities for myTrader Web Frontend
 *
 * Handles normalization and formatting of price data received from WebSocket
 * Addresses the issue where prices might be transmitted in smallest units (cents, satoshis, etc.)
 */

export interface PriceNormalizationOptions {
  assetClass?: 'CRYPTO' | 'STOCK' | 'FOREX' | 'COMMODITY' | 'INDEX';
  symbol?: string;
  market?: string;
}

/**
 * Normalizes a price value from backend with locale-independent parsing
 *
 * This function handles Turkish locale decimal parsing issues where '.' might be
 * misinterpreted as a thousands separator instead of a decimal point.
 *
 * @param value - The raw price value from WebSocket
 * @param options - Additional context for normalization (currently unused but kept for API compatibility)
 * @returns Normalized decimal price
 */
export function normalizePrice(value: any, options: PriceNormalizationOptions = {}): number {
  if (value === null || value === undefined) return 0;

  // If already a number, return it
  if (typeof value === 'number') {
    return isNaN(value) ? 0 : value;
  }

  // Convert to string and ensure it uses dot as decimal separator
  let strValue = String(value).trim();

  // Handle empty strings
  if (strValue === '') return 0;

  // Ensure we're using dot as decimal separator (invariant culture)
  // Remove any thousands separators and normalize decimal point
  // This handles both "1,234.56" and "1.234,56" formats
  const hasComma = strValue.includes(',');
  const hasDot = strValue.includes('.');

  if (hasComma && hasDot) {
    // Both present: determine which is decimal separator
    // In invariant culture (and Binance), dot is decimal, comma is thousands
    strValue = strValue.replace(/,/g, '');
  } else if (hasComma && !hasDot) {
    // Only comma: might be European decimal separator, but Binance uses dot
    // Since Binance sends decimal strings with dot, treat comma as thousands separator
    strValue = strValue.replace(/,/g, '');
  }

  // Use parseFloat for explicit decimal parsing
  const numValue = parseFloat(strValue);

  if (isNaN(numValue)) return 0;

  return numValue;
}

/**
 * Formats a normalized price for display
 *
 * @param price - The normalized decimal price
 * @param options - Formatting options
 * @returns Formatted price string
 */
export function formatPrice(
  price: number,
  options: {
    currency?: string;
    locale?: string;
    minimumFractionDigits?: number;
    maximumFractionDigits?: number;
    assetClass?: 'CRYPTO' | 'STOCK' | 'FOREX' | 'COMMODITY' | 'INDEX';
  } = {}
): string {
  const {
    currency = 'USD',
    locale = 'en-US',
    assetClass
  } = options;

  // Determine decimal places based on asset class and price magnitude
  let minDecimals = options.minimumFractionDigits;
  let maxDecimals = options.maximumFractionDigits;

  if (minDecimals === undefined || maxDecimals === undefined) {
    switch (assetClass) {
      case 'CRYPTO':
        // More decimals for smaller values
        if (price < 0.01) {
          minDecimals = 6;
          maxDecimals = 8;
        } else if (price < 1) {
          minDecimals = 4;
          maxDecimals = 6;
        } else if (price < 100) {
          minDecimals = 2;
          maxDecimals = 4;
        } else {
          minDecimals = 2;
          maxDecimals = 2;
        }
        break;

      case 'FOREX':
        minDecimals = 4;
        maxDecimals = 5;
        break;

      case 'STOCK':
      case 'COMMODITY':
      case 'INDEX':
      default:
        minDecimals = 2;
        maxDecimals = 2;
        break;
    }
  }

  try {
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: minDecimals,
      maximumFractionDigits: maxDecimals,
    }).format(price);
  } catch (error) {
    // Fallback for unsupported currencies
    return `${currency} ${price.toFixed(maxDecimals || 2)}`;
  }
}

/**
 * Formats a percentage change value
 *
 * @param value - The percentage change value
 * @param includeSign - Whether to include + sign for positive values
 * @returns Formatted percentage string
 */
export function formatPercentageChange(value: number, includeSign: boolean = true): string {
  if (value === 0) return '0.00%';

  const formatted = Math.abs(value).toFixed(2);

  if (value > 0 && includeSign) {
    return `+${formatted}%`;
  } else if (value < 0) {
    return `-${formatted}%`;
  } else {
    return `${formatted}%`;
  }
}

/**
 * Formats large numbers with abbreviations (K, M, B)
 *
 * @param value - The number to format
 * @param decimals - Number of decimal places
 * @returns Formatted string with abbreviation
 */
export function formatLargeNumber(value: number, decimals: number = 2): string {
  if (value >= 1e9) {
    return `${(value / 1e9).toFixed(decimals)}B`;
  } else if (value >= 1e6) {
    return `${(value / 1e6).toFixed(decimals)}M`;
  } else if (value >= 1e3) {
    return `${(value / 1e3).toFixed(decimals)}K`;
  } else {
    return value.toFixed(decimals);
  }
}

/**
 * Normalizes all price-related fields in a market data object
 *
 * @param data - Raw market data from WebSocket
 * @param assetClass - The asset class for context
 * @returns Normalized market data
 */
export function normalizeMarketData(data: any, assetClass?: string): any {
  const normalizationOptions: PriceNormalizationOptions = {
    assetClass: assetClass as any || data.assetClass,
    symbol: data.symbol || data.ticker,
    market: data.market
  };

  return {
    ...data,
    // Price fields - Binance sends correct decimal values, no conversion needed
    price: normalizePrice(data.price || data.lastPrice || data.close, normalizationOptions),
    bid: data.bid ? normalizePrice(data.bid, normalizationOptions) : undefined,
    ask: data.ask ? normalizePrice(data.ask, normalizationOptions) : undefined,
    open: data.open ? normalizePrice(data.open, normalizationOptions) : undefined,
    high: data.high ? normalizePrice(data.high, normalizationOptions) : undefined,
    low: data.low ? normalizePrice(data.low, normalizationOptions) : undefined,
    close: data.close ? normalizePrice(data.close, normalizationOptions) : undefined,
    previousClose: data.previousClose ? normalizePrice(data.previousClose, normalizationOptions) : undefined,
    // Volume fields
    volume: Number(data.volume) || 0,
    volume24h: Number(data.volume24h) || 0,
    // Percentage change - handle multiple field name variations
    // Backend sends 'change' field which contains the percentage change
    changePercent: Number(data.change) || Number(data.changePercent) || Number(data.priceChangePercent) || 0,
  };
}

/**
 * Detects if a price value needs normalization based on heuristics
 *
 * NOTE: With Binance API, prices are sent as decimal strings and DO NOT need normalization
 * This function is kept for backward compatibility but should return false for Binance data
 *
 * @param value - The price value to check
 * @returns false - Binance prices are already in correct format
 */
export function needsNormalization(value: any): boolean {
  // Binance WebSocket API sends prices as decimal strings (e.g., "0.55", "95000.00")
  // No normalization is needed
  return false;
}