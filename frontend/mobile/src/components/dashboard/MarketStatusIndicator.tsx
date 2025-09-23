import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { MarketStatusDto, AssetClassType } from '../../types';

interface MarketStatusIndicatorProps {
  marketStatus: MarketStatusDto;
  compact?: boolean;
  showTime?: boolean;
}

interface MarketStatusBadgeProps {
  assetClass: AssetClassType;
  status: 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET';
  nextChangeTime?: string;
  compact?: boolean;
}

export const MarketStatusIndicator: React.FC<MarketStatusIndicatorProps> = ({
  marketStatus,
  compact = false,
  showTime = true,
}) => {
  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'OPEN': return '#10b981';
      case 'PRE_MARKET':
      case 'AFTER_MARKET': return '#f59e0b';
      case 'CLOSED': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getStatusText = (status: string): string => {
    switch (status) {
      case 'OPEN': return 'Açık';
      case 'PRE_MARKET': return 'Açılış Öncesi';
      case 'AFTER_MARKET': return 'Kapanış Sonrası';
      case 'CLOSED': return 'Kapalı';
      default: return 'Bilinmiyor';
    }
  };

  const formatTime = (timeString: string): string => {
    try {
      const date = new Date(timeString);
      return date.toLocaleTimeString('tr-TR', {
        hour: '2-digit',
        minute: '2-digit',
        timeZone: marketStatus.timeZone,
      });
    } catch {
      return '';
    }
  };

  const getNextEventText = (): string => {
    if (marketStatus.status === 'OPEN' && marketStatus.nextClose) {
      return `Kapanış: ${formatTime(marketStatus.nextClose)}`;
    } else if (marketStatus.status === 'CLOSED' && marketStatus.nextOpen) {
      return `Açılış: ${formatTime(marketStatus.nextOpen)}`;
    }
    return '';
  };

  if (compact) {
    return (
      <View style={styles.compactContainer}>
        <View style={[
          styles.compactDot,
          { backgroundColor: getStatusColor(marketStatus.status) }
        ]} />
        <Text style={styles.compactText}>
          {marketStatus.marketName}: {getStatusText(marketStatus.status)}
        </Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.marketName}>{marketStatus.marketName}</Text>
        {marketStatus.isHoliday && marketStatus.holidayName && (
          <Text style={styles.holidayBadge}>🎉 {marketStatus.holidayName}</Text>
        )}
      </View>

      <View style={styles.statusRow}>
        <View style={[
          styles.statusBadge,
          { backgroundColor: getStatusColor(marketStatus.status) }
        ]}>
          <Text style={styles.statusText}>{getStatusText(marketStatus.status)}</Text>
        </View>

        {showTime && (
          <Text style={styles.currentTime}>
            {formatTime(marketStatus.currentTime)}
          </Text>
        )}
      </View>

      {showTime && getNextEventText() && (
        <Text style={styles.nextEvent}>{getNextEventText()}</Text>
      )}
    </View>
  );
};

export const MarketStatusBadge: React.FC<MarketStatusBadgeProps> = ({
  assetClass,
  status,
  nextChangeTime,
  compact = false,
}) => {
  const getAssetClassIcon = (assetClass: AssetClassType): string => {
    switch (assetClass) {
      case 'CRYPTO': return '🚀';
      case 'STOCK': return '🏢';
      case 'FOREX': return '💱';
      case 'COMMODITY': return '🥇';
      case 'INDEX': return '📊';
      default: return '📈';
    }
  };

  const getAssetClassDisplayName = (assetClass: AssetClassType): string => {
    switch (assetClass) {
      case 'CRYPTO': return 'Kripto';
      case 'STOCK': return 'Hisse';
      case 'FOREX': return 'Forex';
      case 'COMMODITY': return 'Emtia';
      case 'INDEX': return 'Endeks';
      default: return assetClass;
    }
  };

  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'OPEN': return '#10b981';
      case 'PRE_MARKET':
      case 'AFTER_MARKET': return '#f59e0b';
      case 'CLOSED': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getStatusText = (status: string): string => {
    switch (status) {
      case 'OPEN': return compact ? 'Açık' : 'Açık';
      case 'PRE_MARKET': return compact ? 'Ön' : 'Açılış Öncesi';
      case 'AFTER_MARKET': return compact ? 'Son' : 'Kapanış Sonrası';
      case 'CLOSED': return compact ? 'Kapalı' : 'Kapalı';
      default: return '?';
    }
  };

  if (compact) {
    return (
      <View style={styles.badgeCompact}>
        <Text style={styles.badgeIcon}>{getAssetClassIcon(assetClass)}</Text>
        <View style={[
          styles.badgeStatusDot,
          { backgroundColor: getStatusColor(status) }
        ]} />
      </View>
    );
  }

  return (
    <View style={styles.badge}>
      <View style={styles.badgeHeader}>
        <Text style={styles.badgeIcon}>{getAssetClassIcon(assetClass)}</Text>
        <Text style={styles.badgeTitle}>{getAssetClassDisplayName(assetClass)}</Text>
      </View>

      <View style={[
        styles.badgeStatus,
        { backgroundColor: getStatusColor(status) }
      ]}>
        <Text style={styles.badgeStatusText}>{getStatusText(status)}</Text>
      </View>

      {nextChangeTime && (
        <Text style={styles.badgeNextTime}>
          {new Date(nextChangeTime).toLocaleTimeString('tr-TR', {
            hour: '2-digit',
            minute: '2-digit',
          })}
        </Text>
      )}
    </View>
  );
};

// Aggregate component for multiple markets
interface MarketStatusGridProps {
  marketStatuses: MarketStatusDto[];
  layout: 'horizontal' | 'grid';
  compact?: boolean;
}

export const MarketStatusGrid: React.FC<MarketStatusGridProps> = ({
  marketStatuses,
  layout = 'horizontal',
  compact = false,
}) => {
  const containerStyle = layout === 'grid' ? styles.gridContainer : styles.horizontalContainer;

  return (
    <View style={containerStyle}>
      {marketStatuses.map((market) => (
        <MarketStatusIndicator
          key={market.marketId}
          marketStatus={market}
          compact={compact}
          showTime={!compact}
        />
      ))}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: 'rgba(255,255,255,0.95)',
    borderRadius: 12,
    padding: 12,
    marginBottom: 8,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  marketName: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
  },
  holidayBadge: {
    fontSize: 10,
    color: '#dc2626',
    backgroundColor: '#fef2f2',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 8,
  },
  statusRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  statusBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 8,
  },
  statusText: {
    fontSize: 12,
    fontWeight: '600',
    color: 'white',
  },
  currentTime: {
    fontSize: 12,
    color: '#6b7280',
    fontWeight: '500',
  },
  nextEvent: {
    fontSize: 11,
    color: '#9ca3af',
    fontStyle: 'italic',
  },
  compactContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 4,
  },
  compactDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    marginRight: 6,
  },
  compactText: {
    fontSize: 12,
    color: '#374151',
  },
  badge: {
    backgroundColor: 'white',
    borderRadius: 10,
    padding: 10,
    alignItems: 'center',
    minWidth: 80,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 2,
  },
  badgeHeader: {
    alignItems: 'center',
    marginBottom: 6,
  },
  badgeIcon: {
    fontSize: 20,
    marginBottom: 2,
  },
  badgeTitle: {
    fontSize: 10,
    fontWeight: '600',
    color: '#374151',
    textAlign: 'center',
  },
  badgeStatus: {
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 8,
    marginBottom: 4,
  },
  badgeStatusText: {
    fontSize: 10,
    fontWeight: '600',
    color: 'white',
  },
  badgeNextTime: {
    fontSize: 9,
    color: '#6b7280',
  },
  badgeCompact: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: 'white',
    borderRadius: 8,
    padding: 6,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 1,
  },
  badgeStatusDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
    marginLeft: 4,
  },
  horizontalContainer: {
    flexDirection: 'row',
    gap: 8,
    paddingHorizontal: 4,
  },
  gridContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    justifyContent: 'space-between',
  },
});

export default MarketStatusIndicator;