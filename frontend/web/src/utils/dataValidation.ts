// Data validation utilities to prevent crashes from malformed data

/**
 * Safely access array with validation
 */
export const safeArray = <T>(data: unknown): T[] => {
  if (Array.isArray(data)) {
    return data;
  }
  console.warn('Expected array but received:', typeof data, data);
  return [];
};

/**
 * Safely slice an array with proper null checking
 */
export const safeSlice = <T>(
  data: unknown,
  start?: number,
  end?: number
): T[] => {
  const array = safeArray<T>(data);
  try {
    return array.slice(start, end);
  } catch (error) {
    console.error('Error slicing array:', error);
    return [];
  }
};

/**
 * Safely access object properties with default values
 */
export const safeGet = <T>(
  obj: unknown,
  path: string,
  defaultValue: T
): T => {
  if (!obj || typeof obj !== 'object') {
    return defaultValue;
  }

  try {
    const keys = path.split('.');
    let current: any = obj;

    for (const key of keys) {
      if (current?.[key] !== undefined) {
        current = current[key];
      } else {
        return defaultValue;
      }
    }

    return current ?? defaultValue;
  } catch (error) {
    console.warn('Error accessing object path:', path, error);
    return defaultValue;
  }
};

/**
 * Safely format numbers with fallback
 */
export const safeNumber = (
  value: unknown,
  fallback: number = 0
): number => {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }

  if (typeof value === 'string') {
    const parsed = parseFloat(value);
    if (Number.isFinite(parsed)) {
      return parsed;
    }
  }

  return fallback;
};

/**
 * Safely format strings with fallback
 */
export const safeString = (
  value: unknown,
  fallback: string = ''
): string => {
  if (typeof value === 'string') {
    return value;
  }

  if (value != null && typeof value !== 'object') {
    return String(value);
  }

  return fallback;
};

/**
 * Validate market data object structure
 */
export const validateMarketData = (data: unknown): boolean => {
  if (!data || typeof data !== 'object') {
    return false;
  }

  const item = data as any;
  return (
    typeof item.symbol === 'string' &&
    typeof item.price === 'number' &&
    Number.isFinite(item.price)
  );
};

/**
 * Validate user/leaderboard entry structure
 */
export const validateLeaderboardEntry = (data: unknown): boolean => {
  if (!data || typeof data !== 'object') {
    return false;
  }

  const entry = data as any;
  return (
    (typeof entry.id === 'string' || typeof entry.id === 'number') &&
    typeof entry.name === 'string' &&
    typeof entry.rank === 'number'
  );
};

/**
 * Safe date formatting
 */
export const safeDate = (
  dateValue: unknown,
  fallback: string = '--'
): string => {
  if (!dateValue) {
    return fallback;
  }

  try {
    const date = new Date(dateValue as string | number);
    if (isNaN(date.getTime())) {
      return fallback;
    }
    return date.toLocaleTimeString();
  } catch (error) {
    console.warn('Error formatting date:', dateValue, error);
    return fallback;
  }
};

/**
 * Safe currency formatting
 */
export const safeCurrency = (
  value: unknown,
  currency: string = 'USD',
  fallback: string = '--'
): string => {
  const numValue = safeNumber(value);
  if (numValue === 0 && value !== 0 && value !== '0') {
    return fallback;
  }

  try {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 6,
    }).format(numValue);
  } catch (error) {
    console.warn('Error formatting currency:', value, error);
    return fallback;
  }
};

/**
 * Safe percentage formatting
 */
export const safePercent = (
  value: unknown,
  fallback: string = '0.0%'
): string => {
  const numValue = safeNumber(value);
  if (numValue === 0 && value !== 0 && value !== '0') {
    return fallback;
  }

  try {
    return `${numValue >= 0 ? '+' : ''}${numValue.toFixed(2)}%`;
  } catch (error) {
    console.warn('Error formatting percentage:', value, error);
    return fallback;
  }
};

/**
 * Safely map over array with error handling
 */
export const safeMap = <T, R>(
  data: unknown,
  mapFn: (item: T, index: number) => R,
  fallback: R[] = []
): R[] => {
  const array = safeArray<T>(data);

  try {
    return array.map((item, index) => {
      try {
        return mapFn(item, index);
      } catch (error) {
        console.error('Error in map function for item:', item, error);
        throw error; // Re-throw to be caught by outer try-catch
      }
    });
  } catch (error) {
    console.error('Error mapping array:', error);
    return fallback;
  }
};

/**
 * Safely filter array with validation
 */
export const safeFilter = <T>(
  data: unknown,
  filterFn: (item: T) => boolean,
  fallback: T[] = []
): T[] => {
  const array = safeArray<T>(data);

  try {
    return array.filter((item) => {
      try {
        return filterFn(item);
      } catch (error) {
        console.warn('Error in filter function for item:', item, error);
        return false; // Exclude items that cause errors
      }
    });
  } catch (error) {
    console.error('Error filtering array:', error);
    return fallback;
  }
};