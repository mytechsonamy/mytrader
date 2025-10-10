/**
 * Price Formatting Utilities for myTrader
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
 * Normalizes a price value from backend (Binance returns decimal strings, no conversion needed)
 *
 * @param value - The raw price value from WebSocket
 * @param options - Additional context for normalization (currently unused but kept for API compatibility)
 * @returns Normalized decimal price
 */
export function normalizePrice(value: any, options: PriceNormalizationOptions = {}): number {
  if (value === null || value === undefined) return 0;

  const numValue = Number(value);

  if (isNaN(numValue)) return 0;

  // Backend now correctly parses Binance prices using InvariantCulture
  // No normalization needed - prices are already in correct decimal format
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
    locale = 'tr-TR',
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

  const price = normalizePrice(data.price || data.lastPrice || data.close, normalizationOptions);

  // ✅ FIX: Preserve undefined/null previousClose instead of normalizing to 0
  const previousClose = (data.previousClose !== undefined && data.previousClose !== null)
    ? normalizePrice(data.previousClose, normalizationOptions)
    : undefined;

  // CRITICAL DEBUG: Log if previousClose is being lost during normalization
  if (__DEV__ && (assetClass === 'STOCK' || data.assetClass === 'STOCK')) {
    console.log('[priceFormatting.normalizeMarketData] INPUT previousClose:', data.previousClose);
    console.log('[priceFormatting.normalizeMarketData] OUTPUT previousClose:', previousClose);
    console.log('[priceFormatting.normalizeMarketData] Was undefined/null?:', data.previousClose === undefined || data.previousClose === null);
  }
  
  // Calculate changePercent correctly
  // Priority: 1) Backend-provided changePercent, 2) Calculate from price and previousClose
  let changePercent = 0;
  let change = 0;
  
  // Backend sends 'change' field which might be the change amount or percentage
  // Check if we have explicit changePercent or priceChangePercent fields
  if (data.changePercent !== undefined && data.changePercent !== null) {
    changePercent = Number(data.changePercent);
  } else if (data.priceChangePercent !== undefined && data.priceChangePercent !== null) {
    changePercent = Number(data.priceChangePercent);
  } else if (data.change !== undefined && data.change !== null) {
    // If 'change' field exists, it's the percentage from backend (Change24h)
    changePercent = Number(data.change);
  } else if (previousClose && previousClose > 0 && price) {
    // Calculate percentage if we have previousClose
    change = price - previousClose;
    changePercent = (change / previousClose) * 100;
  }
  
  // Calculate change amount if not provided
  if (previousClose && previousClose > 0 && price) {
    change = price - previousClose;
  }

  return {
    ...data,
    // Price fields - Binance sends correct decimal values, no conversion needed
    price,
    bid: (data.bid !== undefined && data.bid !== null) ? normalizePrice(data.bid, normalizationOptions) : undefined,
    ask: (data.ask !== undefined && data.ask !== null) ? normalizePrice(data.ask, normalizationOptions) : undefined,
    open: (data.open !== undefined && data.open !== null) ? normalizePrice(data.open, normalizationOptions) : undefined,
    high: (data.high !== undefined && data.high !== null) ? normalizePrice(data.high, normalizationOptions) : undefined,
    low: (data.low !== undefined && data.low !== null) ? normalizePrice(data.low, normalizationOptions) : undefined,
    close: (data.close !== undefined && data.close !== null) ? normalizePrice(data.close, normalizationOptions) : undefined,
    previousClose,
    // Volume fields
    volume: Number(data.volume) || 0,
    volume24h: Number(data.volume24h) || 0,
    // Percentage change and change amount
    change,
    changePercent,
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