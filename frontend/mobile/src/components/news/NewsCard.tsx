import React, { memo } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Image,
  Share,
  Alert,
  Linking,
} from 'react-native';
import { NewsItem } from '../../types';

interface NewsCardProps {
  item: NewsItem;
  variant?: 'full' | 'compact' | 'minimal';
  showImage?: boolean;
  showSource?: boolean;
  showTime?: boolean;
  showSentiment?: boolean;
  showImportance?: boolean;
  showTags?: boolean;
  showRelatedSymbols?: boolean;
  maxTitleLines?: number;
  maxSummaryLines?: number;
  onPress?: (newsItem: NewsItem) => void;
  onSymbolPress?: (symbol: string) => void;
  onSourcePress?: (source: string) => void;
  onShare?: (newsItem: NewsItem) => void;
  onBookmark?: (newsItem: NewsItem) => void;
  isBookmarked?: boolean;
}

const NewsCard: React.FC<NewsCardProps> = memo(({
  item,
  variant = 'full',
  showImage = true,
  showSource = true,
  showTime = true,
  showSentiment = true,
  showImportance = true,
  showTags = false,
  showRelatedSymbols = true,
  maxTitleLines = 3,
  maxSummaryLines = 3,
  onPress,
  onSymbolPress,
  onSourcePress,
  onShare,
  onBookmark,
  isBookmarked = false,
}) => {
  const getSentimentColor = (sentiment?: string): string => {
    switch (sentiment) {
      case 'POSITIVE': return '#10b981';
      case 'NEGATIVE': return '#ef4444';
      case 'NEUTRAL': return '#6b7280';
      default: return '#6b7280';
    }
  };

  const getSentimentIcon = (sentiment?: string): string => {
    switch (sentiment) {
      case 'POSITIVE': return '📈';
      case 'NEGATIVE': return '📉';
      case 'NEUTRAL': return '⚪';
      default: return '📰';
    }
  };

  const getImportanceColor = (importance: string): string => {
    switch (importance) {
      case 'HIGH': return '#ef4444';
      case 'MEDIUM': return '#f59e0b';
      case 'LOW': return '#6b7280';
      default: return '#6b7280';
    }
  };

  const getImportanceText = (importance: string): string => {
    switch (importance) {
      case 'HIGH': return 'Yüksek';
      case 'MEDIUM': return 'Orta';
      case 'LOW': return 'Düşük';
      default: return '';
    }
  };

  const getCategoryIcon = (category: string): string => {
    const lowerCategory = category.toLowerCase();
    if (lowerCategory.includes('crypto') || lowerCategory.includes('bitcoin') || lowerCategory.includes('ethereum')) return '🚀';
    if (lowerCategory.includes('stock') || lowerCategory.includes('bist') || lowerCategory.includes('nasdaq')) return '📊';
    if (lowerCategory.includes('forex') || lowerCategory.includes('currency')) return '💱';
    if (lowerCategory.includes('market') || lowerCategory.includes('trading')) return '📈';
    if (lowerCategory.includes('economy') || lowerCategory.includes('economic')) return '🌍';
    if (lowerCategory.includes('politics') || lowerCategory.includes('political')) return '🏛️';
    if (lowerCategory.includes('technology') || lowerCategory.includes('tech')) return '💻';
    if (lowerCategory.includes('regulation') || lowerCategory.includes('legal')) return '⚖️';
    return '📰';
  };

  const formatTimeAgo = (publishedAt: string): string => {
    const now = new Date();
    const published = new Date(publishedAt);
    const diffInMinutes = Math.floor((now.getTime() - published.getTime()) / (1000 * 60));

    if (diffInMinutes < 1) return 'Az önce';
    if (diffInMinutes < 60) return `${diffInMinutes}dk önce`;

    const diffInHours = Math.floor(diffInMinutes / 60);
    if (diffInHours < 24) return `${diffInHours}s önce`;

    const diffInDays = Math.floor(diffInHours / 24);
    if (diffInDays < 7) return `${diffInDays}g önce`;

    return new Date(publishedAt).toLocaleDateString('tr-TR', {
      day: 'numeric',
      month: 'short',
    });
  };

  const handlePress = () => {
    onPress?.(item);
  };

  const handleShare = async () => {
    try {
      await Share.share({
        message: `${item.title}\n\n${item.summary}\n\n${item.url}`,
        url: item.url,
        title: item.title,
      });
      onShare?.(item);
    } catch (error) {
      console.error('Error sharing news:', error);
    }
  };

  const handleOpenLink = async () => {
    try {
      await Linking.openURL(item.url);
    } catch (error) {
      console.error('Error opening link:', error);
      Alert.alert('Hata', 'Link açılamadı');
    }
  };

  const handleSymbolPress = (symbol: string) => {
    onSymbolPress?.(symbol);
  };

  const handleSourcePress = () => {
    onSourcePress?.(item.source);
  };

  const handleBookmark = () => {
    onBookmark?.(item);
  };

  if (variant === 'minimal') {
    return (
      <TouchableOpacity
        style={styles.minimalCard}
        onPress={handlePress}
        activeOpacity={0.8}
      >
        <View style={styles.minimalHeader}>
          <Text style={styles.minimalCategory}>
            {getCategoryIcon(item.category)}
          </Text>
          {showTime && (
            <Text style={styles.minimalTime}>{formatTimeAgo(item.publishedAt)}</Text>
          )}
          {showSentiment && item.sentiment && (
            <Text style={styles.minimalSentiment}>
              {getSentimentIcon(item.sentiment)}
            </Text>
          )}
        </View>

        <Text style={styles.minimalTitle} numberOfLines={2}>
          {item.title}
        </Text>

        {showSource && (
          <Text style={styles.minimalSource}>{item.source}</Text>
        )}
      </TouchableOpacity>
    );
  }

  if (variant === 'compact') {
    return (
      <TouchableOpacity
        style={styles.compactCard}
        onPress={handlePress}
        activeOpacity={0.8}
      >
        <View style={styles.compactHeader}>
          <View style={styles.compactCategoryRow}>
            <Text style={styles.compactCategoryIcon}>
              {getCategoryIcon(item.category)}
            </Text>
            <Text style={styles.compactCategoryText}>{item.category}</Text>
          </View>
          {showTime && (
            <Text style={styles.compactTime}>{formatTimeAgo(item.publishedAt)}</Text>
          )}
        </View>

        <View style={styles.compactContent}>
          {showImage && item.imageUrl && (
            <Image
              source={{ uri: item.imageUrl }}
              style={styles.compactImage}
              resizeMode="cover"
            />
          )}

          <View style={styles.compactText}>
            <Text style={styles.compactTitle} numberOfLines={maxTitleLines}>
              {item.title}
            </Text>

            {item.summary && (
              <Text style={styles.compactSummary} numberOfLines={maxSummaryLines}>
                {item.summary}
              </Text>
            )}

            <View style={styles.compactFooter}>
              {showSource && (
                <TouchableOpacity onPress={handleSourcePress}>
                  <Text style={styles.compactSource}>{item.source}</Text>
                </TouchableOpacity>
              )}

              <View style={styles.compactBadges}>
                {showSentiment && item.sentiment && (
                  <View style={[
                    styles.compactSentimentBadge,
                    { backgroundColor: getSentimentColor(item.sentiment) }
                  ]}>
                    <Text style={styles.compactBadgeText}>
                      {getSentimentIcon(item.sentiment)}
                    </Text>
                  </View>
                )}

                {showImportance && item.importance !== 'LOW' && (
                  <View style={[
                    styles.compactImportanceBadge,
                    { backgroundColor: getImportanceColor(item.importance) }
                  ]}>
                    <Text style={styles.compactBadgeText}>
                      {getImportanceText(item.importance)}
                    </Text>
                  </View>
                )}
              </View>
            </View>
          </View>
        </View>
      </TouchableOpacity>
    );
  }

  // Full variant
  return (
    <TouchableOpacity
      style={styles.fullCard}
      onPress={handlePress}
      activeOpacity={0.8}
    >
      <View style={styles.fullHeader}>
        <View style={styles.categorySection}>
          <Text style={styles.categoryIcon}>{getCategoryIcon(item.category)}</Text>
          <Text style={styles.categoryText}>{item.category}</Text>
          {showTime && (
            <Text style={styles.timeText}>{formatTimeAgo(item.publishedAt)}</Text>
          )}
        </View>

        <View style={styles.actionButtons}>
          <TouchableOpacity onPress={handleBookmark} style={styles.actionButton}>
            <Text style={styles.actionIcon}>{isBookmarked ? '🔖' : '📌'}</Text>
          </TouchableOpacity>
          <TouchableOpacity onPress={handleShare} style={styles.actionButton}>
            <Text style={styles.actionIcon}>📤</Text>
          </TouchableOpacity>
        </View>
      </View>

      <View style={styles.badgeRow}>
        {showSentiment && item.sentiment && (
          <View style={[
            styles.sentimentBadge,
            { backgroundColor: getSentimentColor(item.sentiment) }
          ]}>
            <Text style={styles.badgeText}>
              {getSentimentIcon(item.sentiment)} {item.sentiment.toLowerCase()}
            </Text>
          </View>
        )}

        {showImportance && item.importance !== 'LOW' && (
          <View style={[
            styles.importanceBadge,
            { backgroundColor: getImportanceColor(item.importance) }
          ]}>
            <Text style={styles.badgeText}>
              {getImportanceText(item.importance)}
            </Text>
          </View>
        )}
      </View>

      <View style={styles.contentSection}>
        {showImage && item.imageUrl && (
          <Image
            source={{ uri: item.imageUrl }}
            style={styles.newsImage}
            resizeMode="cover"
          />
        )}

        <View style={styles.textSection}>
          <Text style={styles.newsTitle} numberOfLines={maxTitleLines}>
            {item.title}
          </Text>

          {item.summary && (
            <Text style={styles.newsSummary} numberOfLines={maxSummaryLines}>
              {item.summary}
            </Text>
          )}
        </View>
      </View>

      <View style={styles.footerSection}>
        <View style={styles.sourceSection}>
          {showSource && (
            <TouchableOpacity onPress={handleSourcePress}>
              <Text style={styles.sourceText}>📰 {item.source}</Text>
            </TouchableOpacity>
          )}
          {item.author && (
            <Text style={styles.authorText}>• {item.author}</Text>
          )}
        </View>

        <TouchableOpacity onPress={handleOpenLink} style={styles.readMoreButton}>
          <Text style={styles.readMoreText}>Devamını oku →</Text>
        </TouchableOpacity>
      </View>

      {showRelatedSymbols && item.relatedSymbols && item.relatedSymbols.length > 0 && (
        <View style={styles.relatedSymbolsSection}>
          <Text style={styles.relatedSymbolsLabel}>İlgili Semboller:</Text>
          <View style={styles.relatedSymbols}>
            {(item.relatedSymbols || []).slice(0, 4).map((symbol, index) => (
              <TouchableOpacity
                key={index}
                onPress={() => handleSymbolPress(symbol)}
                style={styles.relatedSymbol}
              >
                <Text style={styles.relatedSymbolText}>{symbol}</Text>
              </TouchableOpacity>
            ))}
            {(item.relatedSymbols || []).length > 4 && (
              <View style={styles.moreSymbols}>
                <Text style={styles.moreSymbolsText}>
                  +{(item.relatedSymbols || []).length - 4}
                </Text>
              </View>
            )}
          </View>
        </View>
      )}

      {showTags && item.tags && item.tags.length > 0 && (
        <View style={styles.tagsSection}>
          <View style={styles.tags}>
            {(item.tags || []).slice(0, 3).map((tag, index) => (
              <View key={index} style={styles.tag}>
                <Text style={styles.tagText}>#{tag}</Text>
              </View>
            ))}
          </View>
        </View>
      )}
    </TouchableOpacity>
  );
});

const styles = StyleSheet.create({
  // Minimal variant styles
  minimalCard: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0,0,0,0.05)',
  },
  minimalHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
    gap: 8,
  },
  minimalCategory: {
    fontSize: 12,
  },
  minimalTime: {
    fontSize: 10,
    color: '#9ca3af',
    flex: 1,
  },
  minimalSentiment: {
    fontSize: 12,
  },
  minimalTitle: {
    fontSize: 12,
    fontWeight: '600',
    color: '#1f2937',
    marginBottom: 4,
    lineHeight: 16,
  },
  minimalSource: {
    fontSize: 10,
    fontWeight: '600',
    color: '#6b7280',
  },

  // Compact variant styles
  compactCard: {
    backgroundColor: 'white',
    marginHorizontal: 16,
    marginVertical: 6,
    borderRadius: 12,
    padding: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  compactHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  compactCategoryRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  compactCategoryIcon: {
    fontSize: 14,
  },
  compactCategoryText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#374151',
    textTransform: 'capitalize',
  },
  compactTime: {
    fontSize: 11,
    color: '#9ca3af',
  },
  compactContent: {
    flexDirection: 'row',
    gap: 12,
  },
  compactImage: {
    width: 60,
    height: 60,
    borderRadius: 8,
  },
  compactText: {
    flex: 1,
  },
  compactTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
    marginBottom: 4,
    lineHeight: 18,
  },
  compactSummary: {
    fontSize: 12,
    color: '#6b7280',
    lineHeight: 16,
    marginBottom: 8,
  },
  compactFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  compactSource: {
    fontSize: 11,
    fontWeight: '600',
    color: '#667eea',
  },
  compactBadges: {
    flexDirection: 'row',
    gap: 4,
  },
  compactSentimentBadge: {
    paddingHorizontal: 4,
    paddingVertical: 2,
    borderRadius: 6,
  },
  compactImportanceBadge: {
    paddingHorizontal: 4,
    paddingVertical: 2,
    borderRadius: 6,
  },
  compactBadgeText: {
    fontSize: 9,
    fontWeight: '600',
    color: 'white',
  },

  // Full variant styles
  fullCard: {
    backgroundColor: 'white',
    marginHorizontal: 16,
    marginVertical: 8,
    borderRadius: 15,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 5,
  },
  fullHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  categorySection: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  categoryIcon: {
    fontSize: 16,
    marginRight: 8,
  },
  categoryText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#374151',
    textTransform: 'capitalize',
    marginRight: 8,
  },
  timeText: {
    fontSize: 11,
    color: '#9ca3af',
    marginLeft: 'auto',
  },
  actionButtons: {
    flexDirection: 'row',
    gap: 8,
  },
  actionButton: {
    padding: 4,
  },
  actionIcon: {
    fontSize: 16,
  },
  badgeRow: {
    flexDirection: 'row',
    gap: 8,
    marginBottom: 12,
  },
  sentimentBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  importanceBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  badgeText: {
    fontSize: 10,
    fontWeight: '600',
    color: 'white',
  },
  contentSection: {
    flexDirection: 'row',
    marginBottom: 12,
  },
  newsImage: {
    width: 80,
    height: 80,
    borderRadius: 12,
    marginRight: 12,
  },
  textSection: {
    flex: 1,
  },
  newsTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 8,
    lineHeight: 22,
  },
  newsSummary: {
    fontSize: 14,
    color: '#6b7280',
    lineHeight: 20,
  },
  footerSection: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: '#f3f4f6',
  },
  sourceSection: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  sourceText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#667eea',
  },
  authorText: {
    fontSize: 11,
    color: '#9ca3af',
    marginLeft: 4,
  },
  readMoreButton: {
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
  readMoreText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#667eea',
  },
  relatedSymbolsSection: {
    marginTop: 8,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: '#f3f4f6',
  },
  relatedSymbolsLabel: {
    fontSize: 11,
    fontWeight: '600',
    color: '#6b7280',
    marginBottom: 6,
  },
  relatedSymbols: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
  },
  relatedSymbol: {
    backgroundColor: 'rgba(102, 126, 234, 0.1)',
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 8,
  },
  relatedSymbolText: {
    fontSize: 11,
    fontWeight: '600',
    color: '#667eea',
  },
  moreSymbols: {
    backgroundColor: '#f3f4f6',
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 8,
  },
  moreSymbolsText: {
    fontSize: 11,
    fontWeight: '600',
    color: '#9ca3af',
  },
  tagsSection: {
    marginTop: 8,
  },
  tags: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
  },
  tag: {
    backgroundColor: '#f8fafc',
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#e2e8f0',
  },
  tagText: {
    fontSize: 10,
    fontWeight: '600',
    color: '#64748b',
  },
});

export default NewsCard;