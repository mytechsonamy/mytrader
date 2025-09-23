import { UnifiedMarketDataDto, EnhancedSymbolDto, AssetClassType, TimeRange } from '../types';

/**
 * Utility functions for Market Data operations, formatting, and calculations
 */

// Price Formatting Utilities
export interface PriceFormatOptions {
  locale?: string;
  currency?: string;
  minimumFractionDigits?: number;
  maximumFractionDigits?: number;
  useGrouping?: boolean;
  notation?: 'standard' | 'scientific' | 'engineering' | 'compact';
  compactDisplay?: 'short' | 'long';
}

export const formatPrice = (
  price: number,
  options: PriceFormatOptions = {}
): string => {
  const {
    locale = 'tr-TR',
    currency = 'TRY',
    minimumFractionDigits = 2,
    maximumFractionDigits = 8,
    useGrouping = true,
    notation = 'standard',
    compactDisplay = 'short'
  } = options;

  // Handle special cases
  if (isNaN(price) || !isFinite(price)) return 'N/A';
  if (price === 0) return '0';

  try {
    // Determine appropriate decimal places based on price magnitude
    let fractionDigits = minimumFractionDigits;

    if (price < 0.01) {
      fractionDigits = Math.min(8, maximumFractionDigits);
    } else if (price < 1) {
      fractionDigits = Math.min(4, maximumFractionDigits);
    } else if (price > 1000) {
      fractionDigits = Math.min(2, maximumFractionDigits);
    }

    const formatter = new Intl.NumberFormat(locale, {
      style: currency ? 'currency' : 'decimal',
      currency: currency || undefined,
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
      useGrouping,
      notation,
      compactDisplay: notation === 'compact' ? compactDisplay : undefined
    });

    return formatter.format(price);
  } catch (error) {
    console.warn('Error formatting price:', error);
    return price.toFixed(minimumFractionDigits);
  }
};

export const formatPercentage = (
  value: number,
  options: {
    locale?: string;
    minimumFractionDigits?: number;
    maximumFractionDigits?: number;
    showSign?: boolean;
  } = {}
): string => {
  const {
    locale = 'tr-TR',
    minimumFractionDigits = 2,
    maximumFractionDigits = 2,
    showSign = true
  } = options;

  if (isNaN(value) || !isFinite(value)) return 'N/A';

  try {
    const formatter = new Intl.NumberFormat(locale, {
      style: 'percent',
      minimumFractionDigits,
      maximumFractionDigits,
      signDisplay: showSign ? 'exceptZero' : 'auto'
    });

    return formatter.format(value / 100);
  } catch (error) {
    console.warn('Error formatting percentage:', error);
    const sign = showSign && value > 0 ? '+' : '';
    return `${sign}${value.toFixed(minimumFractionDigits)}%`;
  }
};

export const formatVolume = (volume: number, locale: string = 'tr-TR'): string => {
  if (isNaN(volume) || !isFinite(volume)) return 'N/A';
  if (volume === 0) return '0';

  try {
    const formatter = new Intl.NumberFormat(locale, {
      notation: 'compact',
      compactDisplay: 'short',
      maximumFractionDigits: 1
    });

    return formatter.format(volume);
  } catch (error) {
    console.warn('Error formatting volume:', error);
    return volume.toLocaleString();
  }
};

export const formatMarketCap = (marketCap: number, locale: string = 'tr-TR', currency: string = 'USD'): string => {
  if (isNaN(marketCap) || !isFinite(marketCap)) return 'N/A';
  if (marketCap === 0) return '0';

  try {
    const formatter = new Intl.NumberFormat(locale, {
      style: 'currency',
      currency,
      notation: 'compact',
      compactDisplay: 'short',
      maximumFractionDigits: 2
    });

    return formatter.format(marketCap);
  } catch (error) {
    console.warn('Error formatting market cap:', error);
    return marketCap.toLocaleString();
  }
};

// Price Change Calculations
export interface PriceChangeData {
  currentPrice: number;
  previousPrice: number;
  change: number;
  changePercent: number;
  direction: 'up' | 'down' | 'neutral';
}

export const calculatePriceChange = (
  currentPrice: number,
  previousPrice: number
): PriceChangeData => {
  const change = currentPrice - previousPrice;
  const changePercent = previousPrice !== 0 ? (change / previousPrice) * 100 : 0;

  let direction: 'up' | 'down' | 'neutral' = 'neutral';
  if (change > 0) direction = 'up';
  else if (change < 0) direction = 'down';

  return {
    currentPrice,
    previousPrice,
    change,
    changePercent,
    direction
  };
};

export const getPriceChangeColor = (
  changePercent: number,
  theme: 'light' | 'dark' = 'light'
): string => {
  const colors = {
    light: {
      positive: '#22C55E', // Green
      negative: '#EF4444', // Red
      neutral: '#6B7280'   // Gray
    },
    dark: {
      positive: '#16A34A', // Dark Green
      negative: '#DC2626', // Dark Red
      neutral: '#9CA3AF'   // Light Gray
    }
  };

  if (changePercent > 0) return colors[theme].positive;
  if (changePercent < 0) return colors[theme].negative;
  return colors[theme].neutral;
};

// Market Data Validation
export const validateMarketData = (data: Partial<UnifiedMarketDataDto>): string[] => {
  const errors: string[] = [];

  if (!data.symbolId) errors.push('Symbol ID is required');
  if (!data.symbol) errors.push('Symbol is required');
  if (typeof data.price !== 'number' || data.price < 0) errors.push('Valid price is required');
  if (!data.timestamp) errors.push('Timestamp is required');

  // Validate optional fields if present
  if (data.volume !== undefined && (typeof data.volume !== 'number' || data.volume < 0)) {
    errors.push('Volume must be a non-negative number');
  }

  if (data.high !== undefined && data.low !== undefined && data.high < data.low) {
    errors.push('High price cannot be less than low price');
  }

  if (data.open !== undefined && data.price !== undefined) {
    const change = data.price - data.open;
    const calculatedChangePercent = data.open !== 0 ? (change / data.open) * 100 : 0;

    if (data.changePercent !== undefined && Math.abs(data.changePercent - calculatedChangePercent) > 0.01) {
      errors.push('Change percentage does not match calculated value');
    }
  }

  return errors;
};

// Market Data Aggregation
export interface MarketDataSummary {
  totalSymbols: number;
  gainers: number;
  losers: number;
  unchanged: number;
  averageChange: number;
  totalVolume: number;
  topGainer: { symbol: string; changePercent: number } | null;
  topLoser: { symbol: string; changePercent: number } | null;
  mostActive: { symbol: string; volume: number } | null;
}

export const aggregateMarketData = (
  marketDataList: UnifiedMarketDataDto[]
): MarketDataSummary => {
  if (marketDataList.length === 0) {
    return {
      totalSymbols: 0,
      gainers: 0,
      losers: 0,
      unchanged: 0,
      averageChange: 0,
      totalVolume: 0,
      topGainer: null,
      topLoser: null,
      mostActive: null
    };
  }

  let gainers = 0;
  let losers = 0;
  let unchanged = 0;
  let totalChange = 0;
  let totalVolume = 0;
  let topGainer = marketDataList[0];
  let topLoser = marketDataList[0];
  let mostActive = marketDataList[0];

  marketDataList.forEach(data => {
    // Count direction
    if (data.changePercent > 0) gainers++;
    else if (data.changePercent < 0) losers++;
    else unchanged++;

    // Sum changes and volume
    totalChange += data.changePercent;
    totalVolume += data.volume || 0;

    // Track extremes
    if (data.changePercent > topGainer.changePercent) {
      topGainer = data;
    }
    if (data.changePercent < topLoser.changePercent) {
      topLoser = data;
    }
    if ((data.volume || 0) > (mostActive.volume || 0)) {
      mostActive = data;
    }
  });

  return {
    totalSymbols: marketDataList.length,
    gainers,
    losers,
    unchanged,
    averageChange: totalChange / marketDataList.length,
    totalVolume,
    topGainer: { symbol: topGainer.symbol, changePercent: topGainer.changePercent },
    topLoser: { symbol: topLoser.symbol, changePercent: topLoser.changePercent },
    mostActive: { symbol: mostActive.symbol, volume: mostActive.volume || 0 }
  };
};

// Market Data Filtering and Sorting
export interface MarketDataFilters {
  assetClass?: AssetClassType;
  minPrice?: number;
  maxPrice?: number;
  minVolume?: number;
  maxVolume?: number;
  minChangePercent?: number;
  maxChangePercent?: number;
  symbols?: string[];
  excludeSymbols?: string[];
}

export const filterMarketData = (
  marketDataList: UnifiedMarketDataDto[],
  filters: MarketDataFilters,
  symbols?: EnhancedSymbolDto[]
): UnifiedMarketDataDto[] => {
  return marketDataList.filter(data => {
    // Symbol filters
    if (filters.symbols && !filters.symbols.includes(data.symbol)) return false;
    if (filters.excludeSymbols && filters.excludeSymbols.includes(data.symbol)) return false;

    // Asset class filter
    if (filters.assetClass && symbols) {
      const symbol = symbols.find(s => s.id === data.symbolId);
      if (!symbol || symbol.assetClassName !== filters.assetClass) return false;
    }

    // Price filters
    if (filters.minPrice !== undefined && data.price < filters.minPrice) return false;
    if (filters.maxPrice !== undefined && data.price > filters.maxPrice) return false;

    // Volume filters
    if (filters.minVolume !== undefined && (data.volume || 0) < filters.minVolume) return false;
    if (filters.maxVolume !== undefined && (data.volume || 0) > filters.maxVolume) return false;

    // Change filters
    if (filters.minChangePercent !== undefined && data.changePercent < filters.minChangePercent) return false;
    if (filters.maxChangePercent !== undefined && data.changePercent > filters.maxChangePercent) return false;

    return true;
  });
};

export type MarketDataSortField = 'symbol' | 'price' | 'changePercent' | 'volume' | 'marketCap';
export type SortDirection = 'asc' | 'desc';

export const sortMarketData = (
  marketDataList: UnifiedMarketDataDto[],
  sortBy: MarketDataSortField,
  direction: SortDirection = 'desc'
): UnifiedMarketDataDto[] => {
  const multiplier = direction === 'asc' ? 1 : -1;

  return [...marketDataList].sort((a, b) => {
    let aValue: number | string;
    let bValue: number | string;

    switch (sortBy) {
      case 'symbol':
        aValue = a.symbol;
        bValue = b.symbol;
        return multiplier * aValue.localeCompare(bValue);

      case 'price':
        aValue = a.price;
        bValue = b.price;
        break;

      case 'changePercent':
        aValue = a.changePercent;
        bValue = b.changePercent;
        break;

      case 'volume':
        aValue = a.volume || 0;
        bValue = b.volume || 0;
        break;

      case 'marketCap':
        aValue = a.marketCap || 0;
        bValue = b.marketCap || 0;
        break;

      default:
        return 0;
    }

    return multiplier * ((aValue as number) - (bValue as number));
  });
};

// Market Data Search
export const searchMarketData = (
  marketDataList: UnifiedMarketDataDto[],
  query: string,
  symbols?: EnhancedSymbolDto[]
): UnifiedMarketDataDto[] => {
  if (!query.trim()) return marketDataList;

  const normalizedQuery = query.toLowerCase().trim();

  return marketDataList.filter(data => {
    const symbol = symbols?.find(s => s.id === data.symbolId);

    const searchableText = [
      data.symbol,
      symbol?.displayName || '',
      symbol?.description || ''
    ].join(' ').toLowerCase();

    return searchableText.includes(normalizedQuery);
  });
};

// Technical Analysis Helpers
export interface TechnicalIndicators {
  rsi?: number;
  macd?: number;
  bollinger?: {
    upper: number;
    middle: number;
    lower: number;
  };
  movingAverages?: {
    sma20?: number;
    sma50?: number;
    sma200?: number;
    ema12?: number;
    ema26?: number;
  };
}

export const calculateSimpleMovingAverage = (prices: number[], period: number): number | null => {
  if (prices.length < period || period <= 0) return null;

  const relevantPrices = prices.slice(-period);
  const sum = relevantPrices.reduce((acc, price) => acc + price, 0);
  return sum / period;
};

export const calculatePercentageChange = (oldValue: number, newValue: number): number => {
  if (oldValue === 0) return 0;
  return ((newValue - oldValue) / oldValue) * 100;
};

export const calculateVolatility = (prices: number[], period: number = 20): number | null => {
  if (prices.length < period) return null;

  const relevantPrices = prices.slice(-period);
  const mean = relevantPrices.reduce((sum, price) => sum + price, 0) / period;

  const squaredDifferences = relevantPrices.map(price => Math.pow(price - mean, 2));
  const variance = squaredDifferences.reduce((sum, diff) => sum + diff, 0) / period;

  return Math.sqrt(variance);
};

// Currency Conversion Utilities
export interface CurrencyConversionRates {
  [currencyPair: string]: number; // e.g., "USD_TRY": 30.5
}

export const convertPrice = (
  price: number,
  fromCurrency: string,
  toCurrency: string,
  exchangeRates: CurrencyConversionRates
): number => {
  if (fromCurrency === toCurrency) return price;

  const conversionKey = `${fromCurrency}_${toCurrency}`;
  const reverseConversionKey = `${toCurrency}_${fromCurrency}`;

  if (exchangeRates[conversionKey]) {
    return price * exchangeRates[conversionKey];
  } else if (exchangeRates[reverseConversionKey]) {
    return price / exchangeRates[reverseConversionKey];
  }

  // Try conversion through USD
  const toUsdKey = `${fromCurrency}_USD`;
  const fromUsdKey = `USD_${toCurrency}`;

  if (exchangeRates[toUsdKey] && exchangeRates[fromUsdKey]) {
    const usdPrice = price * exchangeRates[toUsdKey];
    return usdPrice * exchangeRates[fromUsdKey];
  }

  console.warn(`No conversion rate found for ${fromCurrency} to ${toCurrency}`);
  return price;
};

// Time-based utilities
export const getTimeRangeInMilliseconds = (timeRange: TimeRange): number => {
  const now = Date.now();

  switch (timeRange) {
    case '1D': return 24 * 60 * 60 * 1000;
    case '7D': return 7 * 24 * 60 * 60 * 1000;
    case '1M': return 30 * 24 * 60 * 60 * 1000;
    case '3M': return 90 * 24 * 60 * 60 * 1000;
    case '6M': return 180 * 24 * 60 * 60 * 1000;
    case '1Y': return 365 * 24 * 60 * 60 * 1000;
    case 'ALL': return now; // Return current timestamp for "all time"
    default: return 24 * 60 * 60 * 1000;
  }
};

export const isMarketDataStale = (
  timestamp: string,
  maxAgeMinutes: number = 5
): boolean => {
  const dataTime = new Date(timestamp).getTime();
  const now = Date.now();
  const maxAgeMs = maxAgeMinutes * 60 * 1000;

  return (now - dataTime) > maxAgeMs;
};

// Export utility collections
export const PriceFormatters = {
  formatPrice,
  formatPercentage,
  formatVolume,
  formatMarketCap
};

export const PriceCalculators = {
  calculatePriceChange,
  calculatePercentageChange,
  calculateSimpleMovingAverage,
  calculateVolatility,
  getPriceChangeColor
};

export const MarketDataValidators = {
  validateMarketData,
  isMarketDataStale
};

export const MarketDataAnalyzers = {
  aggregateMarketData,
  filterMarketData,
  sortMarketData,
  searchMarketData
};

export const CurrencyConverters = {
  convertPrice
};

export const TimeUtilities = {
  getTimeRangeInMilliseconds,
  isMarketDataStale
};