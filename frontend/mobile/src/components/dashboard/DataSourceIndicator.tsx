import React, { memo } from 'react';
import { View, Text, StyleSheet, ViewStyle, TextStyle } from 'react-native';
import { DataSourceType } from '../../types';

interface DataSourceIndicatorProps {
  source?: DataSourceType;
  isRealtime?: boolean;
  qualityScore?: number;
  size?: 'small' | 'medium';
  showLabel?: boolean;
  style?: ViewStyle;
}

/**
 * DataSourceIndicator Component
 *
 * Displays a visual indicator of the data source for market data.
 * Shows a colored dot and optional label to indicate data quality and source.
 *
 * Design Guidelines:
 * - Green dot: Real-time data from Alpaca
 * - Yellow dot: Delayed/fallback data from Yahoo Finance
 * - Small, unobtrusive design that doesn't disrupt UX
 * - Gracefully handles missing data (hidden when source undefined)
 *
 * @param source - Data source type (ALPACA, YAHOO_FALLBACK, YAHOO_REALTIME)
 * @param isRealtime - Whether the data is real-time
 * @param qualityScore - Quality score of the data (0-100)
 * @param size - Size variant (small or medium)
 * @param showLabel - Whether to show text label
 * @param style - Additional container styles
 */
const DataSourceIndicator: React.FC<DataSourceIndicatorProps> = ({
  source,
  isRealtime,
  qualityScore,
  size = 'small',
  showLabel = false,
  style,
}) => {
  // Hide indicator if source is undefined (backward compatibility)
  if (!source) {
    return null;
  }

  // Determine indicator color based on source and real-time status
  const getDotColor = (): string => {
    if (source === 'ALPACA' && isRealtime) {
      return '#10b981'; // Green for real-time Alpaca
    }
    if (source === 'YAHOO_REALTIME' && isRealtime) {
      return '#10b981'; // Green for real-time Yahoo
    }
    if (source === 'YAHOO_FALLBACK') {
      return '#f59e0b'; // Yellow/amber for delayed/fallback
    }
    // Default to yellow for any other case
    return '#f59e0b';
  };

  // Get label text based on source and real-time status
  const getLabelText = (): string => {
    if (isRealtime) {
      return 'Live';
    }
    if (source === 'YAHOO_FALLBACK') {
      return 'Delayed';
    }
    return 'Data';
  };

  // Get tooltip/description for accessibility
  const getDescription = (): string => {
    if (source === 'ALPACA' && isRealtime) {
      return 'Real-time data from Alpaca';
    }
    if (source === 'YAHOO_REALTIME' && isRealtime) {
      return 'Real-time data from Yahoo Finance';
    }
    if (source === 'YAHOO_FALLBACK') {
      return 'Delayed data from Yahoo Finance';
    }
    return 'Market data';
  };

  const dotColor = getDotColor();
  const labelText = getLabelText();
  const dotSize = size === 'small' ? 6 : 8;

  return (
    <View
      style={[styles.container, style]}
      accessible={true}
      accessibilityLabel={getDescription()}
      accessibilityRole="text"
    >
      <View
        style={[
          styles.dot,
          {
            width: dotSize,
            height: dotSize,
            borderRadius: dotSize / 2,
            backgroundColor: dotColor,
          },
        ]}
      />
      {showLabel && (
        <Text style={[
          styles.label,
          size === 'small' ? styles.labelSmall : styles.labelMedium
        ]}>
          {labelText}
        </Text>
      )}
      {qualityScore !== undefined && qualityScore < 70 && showLabel && (
        <Text style={styles.qualityWarning}>!</Text>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 4,
    paddingVertical: 2,
  },
  dot: {
    marginRight: 4,
    // Shadow for better visibility
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.2,
    shadowRadius: 1,
    elevation: 2,
  },
  label: {
    fontWeight: '500',
    color: '#64748b',
  },
  labelSmall: {
    fontSize: 9,
  },
  labelMedium: {
    fontSize: 11,
  },
  qualityWarning: {
    fontSize: 10,
    color: '#ef4444',
    fontWeight: 'bold',
    marginLeft: 2,
  },
});

export default memo(DataSourceIndicator);

// Export utility function for testing and reuse
export const getDataSourceColor = (source?: DataSourceType, isRealtime?: boolean): string => {
  if (!source) return '#6b7280'; // Gray for unknown
  if ((source === 'ALPACA' || source === 'YAHOO_REALTIME') && isRealtime) {
    return '#10b981'; // Green
  }
  return '#f59e0b'; // Yellow
};

export const getDataSourceLabel = (source?: DataSourceType, isRealtime?: boolean): string => {
  if (!source) return '';
  if (isRealtime) return 'Live';
  if (source === 'YAHOO_FALLBACK') return 'Delayed';
  return 'Data';
};
