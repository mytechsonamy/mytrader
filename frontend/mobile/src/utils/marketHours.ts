/**
 * Market Hours Utility
 * Calculates if markets are open/closed based on current time
 */

export type MarketStatusType = 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'POST_MARKET' | 'HOLIDAY';

export interface MarketInfo {
  status: MarketStatusType;
  nextOpenTime?: Date;
  nextCloseTime?: Date;
  isWeekend: boolean;
  isHoliday: boolean;
}

/**
 * Get current time in a specific timezone
 *
 * Note: We use Intl.DateTimeFormat to get individual date/time parts
 * instead of parsing localeString to avoid "date value out of bounds" errors
 */
function getTimeInTimezone(timezone: string): Date {
  const now = new Date();

  try {
    // Use Intl.DateTimeFormat to get date parts in the target timezone
    const formatter = new Intl.DateTimeFormat('en-US', {
      timeZone: timezone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    });

    const parts = formatter.formatToParts(now);
    const dateParts: { [key: string]: string } = {};

    parts.forEach(part => {
      if (part.type !== 'literal') {
        dateParts[part.type] = part.value;
      }
    });

    // Create date using ISO format (YYYY-MM-DDTHH:mm:ss)
    const isoString = `${dateParts.year}-${dateParts.month}-${dateParts.day}T${dateParts.hour}:${dateParts.minute}:${dateParts.second}`;
    return new Date(isoString);
  } catch (error) {
    console.error('[marketHours] Error getting time in timezone:', error);
    // Fallback to local time
    return now;
  }
}

/**
 * Check if date is a weekend
 */
function isWeekend(date: Date): boolean {
  const day = date.getDay();
  return day === 0 || day === 6; // Sunday or Saturday
}

/**
 * Get BIST market status (Borsa Istanbul)
 * Trading hours: Mon-Fri 10:00-18:00 Turkey Time (UTC+3)
 */
export function getBISTStatus(): MarketInfo {
  const turkeyTime = getTimeInTimezone('Europe/Istanbul');
  const hours = turkeyTime.getHours();
  const minutes = turkeyTime.getMinutes();
  const currentMinutes = hours * 60 + minutes;

  const openMinutes = 10 * 60; // 10:00
  const closeMinutes = 18 * 60; // 18:00

  const weekend = isWeekend(turkeyTime);

  if (weekend) {
    return {
      status: 'CLOSED',
      isWeekend: true,
      isHoliday: false,
      nextOpenTime: getNextMonday(turkeyTime, 10, 0),
    };
  }

  if (currentMinutes >= openMinutes && currentMinutes < closeMinutes) {
    return {
      status: 'OPEN',
      isWeekend: false,
      isHoliday: false,
      nextCloseTime: setTime(turkeyTime, 18, 0),
    };
  }

  return {
    status: 'CLOSED',
    isWeekend: false,
    isHoliday: false,
    nextOpenTime: currentMinutes >= closeMinutes
      ? setTime(addDays(turkeyTime, 1), 10, 0)
      : setTime(turkeyTime, 10, 0),
  };
}

/**
 * Get NASDAQ/NYSE market status
 * Trading hours: Mon-Fri 09:30-16:00 EST/EDT (UTC-5/UTC-4)
 * Pre-market: 04:00-09:30
 * Post-market: 16:00-20:00
 */
export function getUSMarketStatus(): MarketInfo {
  const nyTime = getTimeInTimezone('America/New_York');
  const hours = nyTime.getHours();
  const minutes = nyTime.getMinutes();
  const currentMinutes = hours * 60 + minutes;

  const preMarketStart = 4 * 60; // 04:00
  const marketOpen = 9 * 60 + 30; // 09:30
  const marketClose = 16 * 60; // 16:00
  const postMarketEnd = 20 * 60; // 20:00

  const weekend = isWeekend(nyTime);

  if (weekend) {
    return {
      status: 'CLOSED',
      isWeekend: true,
      isHoliday: false,
      nextOpenTime: getNextMonday(nyTime, 9, 30),
    };
  }

  // Pre-market
  if (currentMinutes >= preMarketStart && currentMinutes < marketOpen) {
    return {
      status: 'PRE_MARKET',
      isWeekend: false,
      isHoliday: false,
      nextOpenTime: setTime(nyTime, 9, 30),
    };
  }

  // Market open
  if (currentMinutes >= marketOpen && currentMinutes < marketClose) {
    return {
      status: 'OPEN',
      isWeekend: false,
      isHoliday: false,
      nextCloseTime: setTime(nyTime, 16, 0),
    };
  }

  // Post-market
  if (currentMinutes >= marketClose && currentMinutes < postMarketEnd) {
    return {
      status: 'POST_MARKET',
      isWeekend: false,
      isHoliday: false,
      nextCloseTime: setTime(nyTime, 20, 0),
    };
  }

  // Closed
  return {
    status: 'CLOSED',
    isWeekend: false,
    isHoliday: false,
    nextOpenTime: currentMinutes >= postMarketEnd
      ? setTime(addDays(nyTime, 1), 9, 30)
      : setTime(nyTime, 9, 30),
  };
}

/**
 * Get crypto market status (always open)
 */
export function getCryptoMarketStatus(): MarketInfo {
  return {
    status: 'OPEN',
    isWeekend: false,
    isHoliday: false,
  };
}

/**
 * Get market status by exchange
 */
export function getMarketStatus(exchange: 'BIST' | 'NASDAQ' | 'NYSE' | 'CRYPTO'): MarketInfo {
  switch (exchange) {
    case 'BIST':
      return getBISTStatus();
    case 'NASDAQ':
    case 'NYSE':
      return getUSMarketStatus();
    case 'CRYPTO':
      return getCryptoMarketStatus();
    default:
      return getCryptoMarketStatus();
  }
}

/**
 * Helper: Set time on a date
 */
function setTime(date: Date, hours: number, minutes: number): Date {
  const newDate = new Date(date);
  newDate.setHours(hours, minutes, 0, 0);
  return newDate;
}

/**
 * Helper: Add days to a date
 */
function addDays(date: Date, days: number): Date {
  const newDate = new Date(date);
  newDate.setDate(newDate.getDate() + days);
  return newDate;
}

/**
 * Helper: Get next Monday
 */
function getNextMonday(date: Date, hours: number, minutes: number): Date {
  const daysUntilMonday = (8 - date.getDay()) % 7 || 7;
  const nextMonday = addDays(date, daysUntilMonday);
  return setTime(nextMonday, hours, minutes);
}

/**
 * Format next open/close time
 */
export function formatNextChangeTime(time?: Date): string | undefined {
  if (!time) return undefined;

  const now = new Date();
  const diffMs = time.getTime() - now.getTime();
  const diffHours = diffMs / (1000 * 60 * 60);

  if (diffHours < 24) {
    return `Bugün ${time.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}`;
  }

  if (diffHours < 48) {
    return `Yarın ${time.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}`;
  }

  return time.toLocaleDateString('tr-TR', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}
