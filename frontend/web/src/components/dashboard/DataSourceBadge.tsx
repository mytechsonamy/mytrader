import React from 'react';
import './DataSourceBadge.css';

interface DataSourceBadgeProps {
  source?: string | undefined;
  isRealtime?: boolean | undefined;
  qualityScore?: number | undefined;
  timestamp?: string | undefined;
  className?: string | undefined;
}

/**
 * DataSourceBadge Component
 *
 * Displays a subtle badge indicating the data source for market prices.
 * - Shows "Live" (green) for Alpaca real-time data
 * - Shows "Delayed" (yellow) for Yahoo Finance fallback data
 * - Hides badge if source is undefined (backward compatible)
 *
 * @param source - Data source identifier: "ALPACA", "YAHOO_FALLBACK", or "YAHOO_REALTIME"
 * @param isRealtime - Whether the data is real-time
 * @param qualityScore - Data quality score (100 for Alpaca, 80 for Yahoo)
 * @param timestamp - Last update timestamp
 */
const DataSourceBadge: React.FC<DataSourceBadgeProps> = ({
  source,
  isRealtime,
  qualityScore,
  timestamp,
  className = '',
}) => {
  // Don't render if no source provided (backward compatible)
  if (!source) {
    return null;
  }

  // Determine badge type and display text
  const isLive = source === 'ALPACA' || isRealtime === true;
  const badgeType = isLive ? 'realtime' : 'delayed';
  const displayText = isLive ? 'Live' : 'Delayed';

  // Format source name for tooltip
  const getSourceName = (): string => {
    switch (source) {
      case 'ALPACA':
        return 'Alpaca (Real-time)';
      case 'YAHOO_FALLBACK':
        return 'Yahoo Finance (Delayed)';
      case 'YAHOO_REALTIME':
        return 'Yahoo Finance (Real-time)';
      default:
        return source;
    }
  };

  // Format timestamp for tooltip
  const formatTimestamp = (): string => {
    if (!timestamp) return 'N/A';
    try {
      const date = new Date(timestamp);
      return date.toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
      });
    } catch {
      return 'N/A';
    }
  };

  // Tooltip content
  const tooltipContent = `
    Source: ${getSourceName()}
    Quality: ${qualityScore || 'N/A'}%
    Last update: ${formatTimestamp()}
  `.trim();

  return (
    <span
      className={`data-source-badge ${badgeType} ${className}`}
      title={tooltipContent}
      aria-label={`Data source: ${displayText}`}
      data-testid="data-source-badge"
    >
      {displayText}
    </span>
  );
};

export default DataSourceBadge;
