/**
 * Time Formatting Utilities
 *
 * Provides Turkish-localized time formatting functions for:
 * - Relative time displays (5 dakika önce)
 * - Next market open/close times (Yarın 10:00)
 * - Timestamp formatting
 */

/**
 * Format a timestamp as relative time in Turkish
 *
 * @param timestamp ISO 8601 timestamp string
 * @returns Formatted relative time string
 *
 * Examples:
 * - Less than 1 minute: "Şimdi"
 * - 5 minutes ago: "5 dakika önce"
 * - 2 hours ago (same day): "14:32"
 * - Yesterday: "Dün 18:00"
 * - 2 days ago: "2 gün önce"
 */
export function formatRelativeTime(timestamp: string): string {
  try {
    const now = new Date();
    const updateTime = new Date(timestamp);

    // Check if timestamp is valid
    if (isNaN(updateTime.getTime())) {
      return '--';
    }

    const diffMs = now.getTime() - updateTime.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);

    // Just now (less than 1 minute)
    if (diffMinutes < 1) {
      return 'Şimdi';
    }

    // Minutes ago (1-59 minutes)
    if (diffMinutes < 60) {
      return `${diffMinutes} dakika önce`;
    }

    const diffHours = Math.floor(diffMinutes / 60);

    // Today - show time only (within last 24 hours)
    if (diffHours < 24 && updateTime.toDateString() === now.toDateString()) {
      return updateTime.toLocaleTimeString('tr-TR', {
        hour: '2-digit',
        minute: '2-digit'
      });
    }

    const diffDays = Math.floor(diffHours / 24);

    // Yesterday
    const yesterday = new Date(now);
    yesterday.setDate(yesterday.getDate() - 1);
    if (updateTime.toDateString() === yesterday.toDateString()) {
      return `Dün ${updateTime.toLocaleTimeString('tr-TR', {
        hour: '2-digit',
        minute: '2-digit'
      })}`;
    }

    // Days ago (2-6 days)
    if (diffDays < 7) {
      return `${diffDays} gün önce`;
    }

    // Weeks ago
    const diffWeeks = Math.floor(diffDays / 7);
    if (diffWeeks < 4) {
      return `${diffWeeks} hafta önce`;
    }

    // Months ago
    const diffMonths = Math.floor(diffDays / 30);
    return `${diffMonths} ay önce`;

  } catch (error) {
    console.warn('[timeFormatting] Error formatting relative time:', error);
    return '--';
  }
}

/**
 * Format next market open/close time in Turkish
 *
 * @param nextTime ISO 8601 timestamp string
 * @returns Formatted next event time string
 *
 * Examples:
 * - Today: "Bugün 09:30"
 * - Tomorrow: "Yarın 10:00"
 * - Future date: "15 Oca 09:30"
 */
export function formatNextOpenTime(nextTime: string): string {
  try {
    const targetTime = new Date(nextTime);
    const now = new Date();

    // Check if timestamp is valid
    if (isNaN(targetTime.getTime())) {
      return '--';
    }

    // Check if it's today
    const isToday = targetTime.toDateString() === now.toDateString();
    if (isToday) {
      return `Bugün ${targetTime.toLocaleTimeString('tr-TR', {
        hour: '2-digit',
        minute: '2-digit'
      })}`;
    }

    // Check if it's tomorrow
    const tomorrow = new Date(now);
    tomorrow.setDate(tomorrow.getDate() + 1);
    const isTomorrow = targetTime.toDateString() === tomorrow.toDateString();
    if (isTomorrow) {
      return `Yarın ${targetTime.toLocaleTimeString('tr-TR', {
        hour: '2-digit',
        minute: '2-digit'
      })}`;
    }

    // Future date - show abbreviated date and time
    return targetTime.toLocaleDateString('tr-TR', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });

  } catch (error) {
    console.warn('[timeFormatting] Error formatting next open time:', error);
    return '--';
  }
}

/**
 * Format timestamp for display with market status context
 *
 * @param timestamp ISO 8601 timestamp string
 * @param marketStatus Market status (OPEN, CLOSED, etc.)
 * @returns Formatted string with context
 *
 * Examples:
 * - OPEN: "Son güncelleme: 14:32"
 * - CLOSED: "Piyasa Kapalı - Son: 18:00"
 */
export function formatLastUpdateWithStatus(
  timestamp: string,
  marketStatus?: 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET' | 'POST_MARKET' | 'HOLIDAY'
): string {
  try {
    const relativeTime = formatRelativeTime(timestamp);

    if (!marketStatus || marketStatus === 'OPEN') {
      return `Son güncelleme: ${relativeTime}`;
    }

    if (marketStatus === 'CLOSED' || marketStatus === 'HOLIDAY') {
      return `Piyasa Kapalı - Son: ${relativeTime}`;
    }

    if (marketStatus === 'PRE_MARKET') {
      return `Ön Piyasa - ${relativeTime}`;
    }

    if (marketStatus === 'AFTER_MARKET' || marketStatus === 'POST_MARKET') {
      return `Kapanış Sonrası - ${relativeTime}`;
    }

    return `Son güncelleme: ${relativeTime}`;

  } catch (error) {
    console.warn('[timeFormatting] Error formatting last update with status:', error);
    return '--';
  }
}

/**
 * Format a simple timestamp to HH:MM format
 *
 * @param timestamp ISO 8601 timestamp string
 * @returns Formatted time string (HH:MM)
 */
export function formatSimpleTime(timestamp: string): string {
  try {
    const date = new Date(timestamp);

    if (isNaN(date.getTime())) {
      return '--';
    }

    return date.toLocaleTimeString('tr-TR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  } catch (error) {
    console.warn('[timeFormatting] Error formatting simple time:', error);
    return '--';
  }
}

/**
 * Check if a timestamp is recent (within last N minutes)
 *
 * @param timestamp ISO 8601 timestamp string
 * @param thresholdMinutes Number of minutes to consider "recent"
 * @returns True if timestamp is within threshold
 */
export function isRecentUpdate(timestamp: string, thresholdMinutes: number = 5): boolean {
  try {
    const now = new Date();
    const updateTime = new Date(timestamp);

    if (isNaN(updateTime.getTime())) {
      return false;
    }

    const diffMs = now.getTime() - updateTime.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);

    return diffMinutes <= thresholdMinutes;
  } catch (error) {
    return false;
  }
}

/**
 * Get time until next event in human-readable format
 *
 * @param nextTime ISO 8601 timestamp string
 * @returns Formatted countdown string
 *
 * Examples:
 * - "5 dakika sonra"
 * - "2 saat sonra"
 * - "Yarın"
 */
export function getTimeUntil(nextTime: string): string {
  try {
    const targetTime = new Date(nextTime);
    const now = new Date();

    if (isNaN(targetTime.getTime())) {
      return '--';
    }

    const diffMs = targetTime.getTime() - now.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);

    if (diffMinutes <= 0) {
      return 'Şimdi';
    }

    if (diffMinutes < 60) {
      return `${diffMinutes} dakika sonra`;
    }

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) {
      return `${diffHours} saat sonra`;
    }

    const diffDays = Math.floor(diffHours / 24);
    if (diffDays === 1) {
      return 'Yarın';
    }

    return `${diffDays} gün sonra`;

  } catch (error) {
    console.warn('[timeFormatting] Error calculating time until:', error);
    return '--';
  }
}
