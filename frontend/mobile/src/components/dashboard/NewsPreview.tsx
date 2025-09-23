import React, { memo } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  Image,
} from 'react-native';
import { NewsItem } from '../../types';

interface NewsPreviewProps {
  news: NewsItem[];
  isLoading?: boolean;
  maxItems?: number;
  showImages?: boolean;
  compact?: boolean;
  onPress?: () => void;
  onNewsItemPress?: (newsItem: NewsItem) => void;
}

interface NewsItemCardProps {
  item: NewsItem;
  compact?: boolean;
  showImage?: boolean;
  onPress?: (newsItem: NewsItem) => void;
}

const NewsItemCard: React.FC<NewsItemCardProps> = memo(({
  item,
  compact = false,
  showImage = true,
  onPress,
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
    switch (category.toLowerCase()) {
      case 'crypto':
      case 'cryptocurrency': return '🚀';
      case 'stock':
      case 'stocks': return '📊';
      case 'forex': return '💱';
      case 'market': return '📈';
      case 'economy': return '🌍';
      case 'politics': return '🏛️';
      case 'technology': return '💻';
      default: return '📰';
    }
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
    return `${diffInDays}g önce`;
  };

  const handlePress = () => {
    onPress?.(item);
  };

  if (compact) {
    return (
      <TouchableOpacity
        style={styles.compactNewsItem}
        onPress={handlePress}
        activeOpacity={0.8}
      >
        <View style={styles.compactHeader}>
          <Text style={styles.compactCategory}>
            {getCategoryIcon(item.category)}
          </Text>
          <Text style={styles.compactTime}>{formatTimeAgo(item.publishedAt)}</Text>
          <Text style={styles.compactSentiment}>
            {getSentimentIcon(item.sentiment)}
          </Text>
        </View>

        <Text style={styles.compactTitle} numberOfLines={2}>
          {item.title}
        </Text>

        {item.summary && (
          <Text style={styles.compactSummary} numberOfLines={1}>
            {item.summary}
          </Text>
        )}

        <View style={styles.compactFooter}>
          <Text style={styles.compactSource}>{item.source}</Text>
          {item.importance !== 'LOW' && (
            <View style={[
              styles.compactImportanceBadge,
              { backgroundColor: getImportanceColor(item.importance) }
            ]}>
              <Text style={styles.compactImportanceText}>
                {getImportanceText(item.importance)}
              </Text>
            </View>
          )}
        </View>
      </TouchableOpacity>
    );
  }

  return (
    <TouchableOpacity
      style={styles.newsItem}
      onPress={handlePress}
      activeOpacity={0.8}
    >
      <View style={styles.newsHeader}>
        <View style={styles.categoryRow}>
          <Text style={styles.categoryIcon}>{getCategoryIcon(item.category)}</Text>
          <Text style={styles.categoryText}>{item.category}</Text>
          <Text style={styles.timeText}>{formatTimeAgo(item.publishedAt)}</Text>
        </View>

        <View style={styles.badgeRow}>
          {item.sentiment && (
            <View style={[
              styles.sentimentBadge,
              { backgroundColor: getSentimentColor(item.sentiment) }
            ]}>
              <Text style={styles.sentimentText}>
                {getSentimentIcon(item.sentiment)} {item.sentiment.toLowerCase()}
              </Text>
            </View>
          )}

          {item.importance !== 'LOW' && (
            <View style={[
              styles.importanceBadge,
              { backgroundColor: getImportanceColor(item.importance) }
            ]}>
              <Text style={styles.importanceText}>
                {getImportanceText(item.importance)}
              </Text>
            </View>
          )}
        </View>
      </View>

      <View style={styles.newsContent}>
        {showImage && item.imageUrl && (
          <Image
            source={{ uri: item.imageUrl }}
            style={styles.newsImage}
            resizeMode="cover"
          />
        )}

        <View style={[styles.newsText, showImage && item.imageUrl && styles.newsTextWithImage]}>
          <Text style={styles.newsTitle} numberOfLines={2}>
            {item.title}
          </Text>

          {item.summary && (
            <Text style={styles.newsSummary} numberOfLines={3}>
              {item.summary}
            </Text>
          )}

          <View style={styles.newsFooter}>
            <Text style={styles.newsSource}>{item.source}</Text>
            {item.author && (
              <Text style={styles.newsAuthor}>• {item.author}</Text>
            )}
          </View>

          {item.relatedSymbols.length > 0 && (
            <View style={styles.relatedSymbols}>
              {item.relatedSymbols.slice(0, 3).map((symbol, index) => (
                <Text key={index} style={styles.relatedSymbol}>
                  {symbol}
                </Text>
              ))}
              {item.relatedSymbols.length > 3 && (
                <Text style={styles.moreSymbols}>
                  +{item.relatedSymbols.length - 3}
                </Text>
              )}
            </View>
          )}
        </View>
      </View>
    </TouchableOpacity>
  );
});

const NewsPreview: React.FC<NewsPreviewProps> = ({
  news,
  isLoading = false,
  maxItems = 4,
  showImages = true,
  compact = false,
  onPress,
  onNewsItemPress,
}) => {
  const displayedNews = news.slice(0, maxItems);

  const categorizeNews = () => {
    const categories = {
      crypto: news.filter(item =>
        item.category.toLowerCase().includes('crypto') ||
        item.tags.some(tag => tag.toLowerCase().includes('crypto'))
      ).length,
      stocks: news.filter(item =>
        item.category.toLowerCase().includes('stock') ||
        item.tags.some(tag => tag.toLowerCase().includes('stock'))
      ).length,
      market: news.filter(item =>
        item.category.toLowerCase().includes('market') ||
        item.tags.some(tag => tag.toLowerCase().includes('market'))
      ).length,
    };

    const total = categories.crypto + categories.stocks + categories.market;
    return { ...categories, total };
  };

  const newsStats = categorizeNews();

  if (isLoading) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>📰 Piyasa Haberleri</Text>
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="small" color="#667eea" />
          <Text style={styles.loadingText}>Haberler yükleniyor...</Text>
        </View>
      </View>
    );
  }

  if (news.length === 0) {
    return (
      <View style={styles.container}>
        <TouchableOpacity style={styles.header} onPress={onPress}>
          <Text style={styles.title}>📰 Piyasa Haberleri</Text>
          <Text style={styles.expandIcon}>→</Text>
        </TouchableOpacity>
        <View style={styles.emptyState}>
          <Text style={styles.emptyIcon}>📰</Text>
          <Text style={styles.emptyText}>Henüz haber bulunamadı</Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <TouchableOpacity style={styles.header} onPress={onPress}>
        <View style={styles.titleSection}>
          <Text style={styles.title}>📰 Piyasa Haberleri</Text>
          <Text style={styles.subtitle}>
            {newsStats.total} haber • {newsStats.crypto}🚀 {newsStats.stocks}📊 {newsStats.market}📈
          </Text>
        </View>
        <Text style={styles.expandIcon}>→</Text>
      </TouchableOpacity>

      <ScrollView
        style={styles.content}
        showsVerticalScrollIndicator={false}
        nestedScrollEnabled={true}
      >
        {displayedNews.map((item, index) => (
          <NewsItemCard
            key={`${item.id}-${index}`}
            item={item}
            compact={compact}
            showImage={showImages && !compact}
            onPress={onNewsItemPress}
          />
        ))}
      </ScrollView>

      <TouchableOpacity
        style={styles.footer}
        onPress={onPress}
      >
        <Text style={styles.viewAllText}>
          {news.length > maxItems
            ? `${news.length - maxItems} haber daha göster`
            : 'Tüm haberleri gör'
          }
        </Text>
        <Text style={styles.footerIcon}>📖</Text>
      </TouchableOpacity>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: 'rgba(255,255,255,0.95)',
    borderRadius: 15,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
    overflow: 'hidden',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    backgroundColor: 'rgba(255,255,255,0.95)',
  },
  titleSection: {
    flex: 1,
  },
  title: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 12,
    color: '#6b7280',
  },
  expandIcon: {
    fontSize: 16,
    color: '#6b7280',
    fontWeight: '600',
  },
  content: {
    maxHeight: 320,
    backgroundColor: '#f8fafc',
  },
  loadingContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 32,
    gap: 8,
  },
  loadingText: {
    fontSize: 14,
    color: '#6b7280',
  },
  emptyState: {
    alignItems: 'center',
    paddingVertical: 32,
    paddingHorizontal: 16,
  },
  emptyIcon: {
    fontSize: 48,
    marginBottom: 12,
  },
  emptyText: {
    fontSize: 14,
    color: '#6b7280',
    textAlign: 'center',
  },
  newsItem: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0,0,0,0.05)',
  },
  newsHeader: {
    marginBottom: 8,
  },
  categoryRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  categoryIcon: {
    fontSize: 14,
    marginRight: 6,
  },
  categoryText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#374151',
    textTransform: 'capitalize',
    flex: 1,
  },
  timeText: {
    fontSize: 11,
    color: '#9ca3af',
  },
  badgeRow: {
    flexDirection: 'row',
    gap: 6,
  },
  sentimentBadge: {
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 8,
  },
  sentimentText: {
    fontSize: 10,
    fontWeight: '600',
    color: 'white',
  },
  importanceBadge: {
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 8,
  },
  importanceText: {
    fontSize: 10,
    fontWeight: '600',
    color: 'white',
  },
  newsContent: {
    flexDirection: 'row',
  },
  newsImage: {
    width: 60,
    height: 60,
    borderRadius: 8,
    marginRight: 12,
  },
  newsText: {
    flex: 1,
  },
  newsTextWithImage: {
    flex: 1,
  },
  newsTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1f2937',
    marginBottom: 6,
    lineHeight: 18,
  },
  newsSummary: {
    fontSize: 12,
    color: '#6b7280',
    lineHeight: 16,
    marginBottom: 8,
  },
  newsFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 6,
  },
  newsSource: {
    fontSize: 11,
    fontWeight: '600',
    color: '#374151',
  },
  newsAuthor: {
    fontSize: 11,
    color: '#9ca3af',
    marginLeft: 4,
  },
  relatedSymbols: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 4,
  },
  relatedSymbol: {
    fontSize: 10,
    fontWeight: '600',
    color: '#667eea',
    backgroundColor: 'rgba(102, 126, 234, 0.1)',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6,
  },
  moreSymbols: {
    fontSize: 10,
    color: '#9ca3af',
    backgroundColor: '#f3f4f6',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6,
  },
  compactNewsItem: {
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0,0,0,0.05)',
  },
  compactHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 6,
    gap: 8,
  },
  compactCategory: {
    fontSize: 12,
  },
  compactTime: {
    fontSize: 10,
    color: '#9ca3af',
    flex: 1,
  },
  compactSentiment: {
    fontSize: 12,
  },
  compactTitle: {
    fontSize: 13,
    fontWeight: '600',
    color: '#1f2937',
    marginBottom: 4,
    lineHeight: 16,
  },
  compactSummary: {
    fontSize: 11,
    color: '#6b7280',
    marginBottom: 6,
  },
  compactFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  compactSource: {
    fontSize: 10,
    fontWeight: '600',
    color: '#374151',
  },
  compactImportanceBadge: {
    paddingHorizontal: 4,
    paddingVertical: 1,
    borderRadius: 6,
  },
  compactImportanceText: {
    fontSize: 9,
    fontWeight: '600',
    color: 'white',
  },
  footer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 12,
    backgroundColor: 'rgba(255,255,255,0.95)',
    borderTopWidth: 1,
    borderTopColor: 'rgba(0,0,0,0.05)',
    gap: 8,
  },
  viewAllText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#667eea',
  },
  footerIcon: {
    fontSize: 14,
  },
});

export default memo(NewsPreview);