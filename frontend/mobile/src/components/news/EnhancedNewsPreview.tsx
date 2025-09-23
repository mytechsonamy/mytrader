import React, { memo, useState, useMemo } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
} from 'react-native';
import { NewsItem } from '../../types';
import NewsCard from './NewsCard';
import NewsCategoryFilter, { NewsCategory } from './NewsCategoryFilter';
import NewsSearch from './NewsSearch';

interface EnhancedNewsPreviewProps {
  news: NewsItem[];
  isLoading?: boolean;
  maxItems?: number;
  showImages?: boolean;
  compact?: boolean;
  showSearch?: boolean;
  showCategories?: boolean;
  showFilters?: boolean;
  onPress?: () => void;
  onNewsItemPress?: (newsItem: NewsItem) => void;
  onCategorySelect?: (category: string) => void;
  onSearch?: (query: string) => void;
  title?: string;
  subtitle?: string;
  variant?: 'dashboard' | 'standalone';
  enableRealTime?: boolean;
}

const EnhancedNewsPreview: React.FC<EnhancedNewsPreviewProps> = memo(({
  news,
  isLoading = false,
  maxItems = 4,
  showImages = true,
  compact = false,
  showSearch = false,
  showCategories = true,
  showFilters = true,
  onPress,
  onNewsItemPress,
  onCategorySelect,
  onSearch,
  title = '📰 Piyasa Haberleri',
  subtitle,
  variant = 'dashboard',
  enableRealTime = false,
}) => {
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [sortBy, setSortBy] = useState<'time' | 'importance' | 'relevance'>('time');

  // Define news categories
  const newsCategories: NewsCategory[] = useMemo(() => {
    const categoryMap = new Map<string, { count: number; color: string; emoji: string; label: string }>();

    // Initialize default categories
    const defaultCategories = [
      { key: 'all', label: 'Tümü', emoji: '📰', color: '#667eea', count: news.length },
      { key: 'crypto', label: 'Kripto', emoji: '🚀', color: '#f7931a', count: 0 },
      { key: 'stock', label: 'Hisse', emoji: '📊', color: '#10b981', count: 0 },
      { key: 'forex', label: 'Forex', emoji: '💱', color: '#3b82f6', count: 0 },
      { key: 'market', label: 'Piyasa', emoji: '📈', color: '#8b5cf6', count: 0 },
      { key: 'economy', label: 'Ekonomi', emoji: '🌍', color: '#06b6d4', count: 0 },
      { key: 'regulation', label: 'Düzenleme', emoji: '⚖️', color: '#f59e0b', count: 0 },
    ];

    defaultCategories.forEach(cat => {
      categoryMap.set(cat.key, {
        count: cat.count,
        color: cat.color,
        emoji: cat.emoji,
        label: cat.label,
      });
    });

    // Count news items by category
    news.forEach(item => {
      const category = item.category.toLowerCase();

      if (category.includes('crypto') || category.includes('bitcoin') || category.includes('ethereum')) {
        const cat = categoryMap.get('crypto');
        if (cat) cat.count++;
      } else if (category.includes('stock') || category.includes('bist') || category.includes('nasdaq')) {
        const cat = categoryMap.get('stock');
        if (cat) cat.count++;
      } else if (category.includes('forex') || category.includes('currency')) {
        const cat = categoryMap.get('forex');
        if (cat) cat.count++;
      } else if (category.includes('market') || category.includes('trading')) {
        const cat = categoryMap.get('market');
        if (cat) cat.count++;
      } else if (category.includes('economy') || category.includes('economic')) {
        const cat = categoryMap.get('economy');
        if (cat) cat.count++;
      } else if (category.includes('regulation') || category.includes('legal')) {
        const cat = categoryMap.get('regulation');
        if (cat) cat.count++;
      }
    });

    return Array.from(categoryMap.entries())
      .filter(([key, data]) => key === 'all' || data.count > 0)
      .map(([key, data]) => ({
        key,
        label: data.label,
        emoji: data.emoji,
        color: data.color,
        count: data.count,
      }));
  }, [news]);

  // Filter and sort news
  const filteredNews = useMemo(() => {
    let filtered = [...news];

    // Apply category filter
    if (selectedCategory !== 'all') {
      filtered = filtered.filter(item => {
        const category = item.category.toLowerCase();
        switch (selectedCategory) {
          case 'crypto':
            return category.includes('crypto') || category.includes('bitcoin') || category.includes('ethereum');
          case 'stock':
            return category.includes('stock') || category.includes('bist') || category.includes('nasdaq');
          case 'forex':
            return category.includes('forex') || category.includes('currency');
          case 'market':
            return category.includes('market') || category.includes('trading');
          case 'economy':
            return category.includes('economy') || category.includes('economic');
          case 'regulation':
            return category.includes('regulation') || category.includes('legal');
          default:
            return true;
        }
      });
    }

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(item =>
        item.title.toLowerCase().includes(query) ||
        item.summary.toLowerCase().includes(query) ||
        item.source.toLowerCase().includes(query) ||
        (item.tags || []).some(tag => tag.toLowerCase().includes(query)) ||
        (item.relatedSymbols || []).some(symbol => symbol.toLowerCase().includes(query))
      );
    }

    // Apply sorting
    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'importance':
          const importanceOrder = { HIGH: 3, MEDIUM: 2, LOW: 1 };
          const aImportance = importanceOrder[a.importance as keyof typeof importanceOrder] || 1;
          const bImportance = importanceOrder[b.importance as keyof typeof importanceOrder] || 1;
          return bImportance - aImportance;
        case 'relevance':
          // Simple relevance based on related symbols and tags count
          const aRelevance = (a.relatedSymbols || []).length + (a.tags || []).length;
          const bRelevance = (b.relatedSymbols || []).length + (b.tags || []).length;
          return bRelevance - aRelevance;
        case 'time':
        default:
          return new Date(b.publishedAt).getTime() - new Date(a.publishedAt).getTime();
      }
    });

    return filtered.slice(0, maxItems);
  }, [news, selectedCategory, searchQuery, sortBy, maxItems]);

  const handleCategorySelect = (categoryKey: string) => {
    setSelectedCategory(categoryKey);
    onCategorySelect?.(categoryKey);
  };

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    onSearch?.(query);
  };

  const handleClearSearch = () => {
    setSearchQuery('');
    onSearch?.('');
  };

  const generateSubtitle = () => {
    if (subtitle) return subtitle;

    const totalNews = news.length;
    const filteredCount = filteredNews.length;
    const categoryLabel = newsCategories.find(cat => cat.key === selectedCategory)?.label || 'Tümü';

    if (searchQuery.trim()) {
      return `"${searchQuery}" için ${filteredCount} sonuç`;
    }

    if (selectedCategory === 'all') {
      return `${totalNews} haber • Son güncellemeler`;
    }

    return `${filteredCount} ${categoryLabel.toLowerCase()} haberi`;
  };

  if (isLoading) {
    return (
      <View style={[styles.container, variant === 'standalone' && styles.standaloneContainer]}>
        <View style={styles.header}>
          <Text style={styles.title}>{title}</Text>
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
      <View style={[styles.container, variant === 'standalone' && styles.standaloneContainer]}>
        <TouchableOpacity style={styles.header} onPress={onPress}>
          <Text style={styles.title}>{title}</Text>
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
    <View style={[styles.container, variant === 'standalone' && styles.standaloneContainer]}>
      <TouchableOpacity style={styles.header} onPress={onPress} disabled={variant === 'standalone'}>
        <View style={styles.titleSection}>
          <Text style={styles.title}>{title}</Text>
          <Text style={styles.subtitle}>{generateSubtitle()}</Text>
        </View>
        {variant === 'dashboard' && (
          <Text style={styles.expandIcon}>→</Text>
        )}
      </TouchableOpacity>

      {/* Search */}
      {showSearch && (
        <View style={styles.searchContainer}>
          <NewsSearch
            value={searchQuery}
            onSearch={handleSearch}
            onClear={handleClearSearch}
            placeholder="Haberlerde ara..."
            variant="compact"
          />
        </View>
      )}

      {/* Categories */}
      {showCategories && newsCategories.length > 1 && (
        <View style={styles.categoriesContainer}>
          <NewsCategoryFilter
            categories={newsCategories}
            selectedCategory={selectedCategory}
            onCategorySelect={handleCategorySelect}
            showCounts={true}
            horizontal={true}
            compact={compact}
          />
        </View>
      )}

      {/* Filters */}
      {showFilters && !compact && (
        <View style={styles.filtersContainer}>
          <ScrollView
            horizontal
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={styles.filtersContent}
          >
            <TouchableOpacity
              style={[styles.filterButton, sortBy === 'time' && styles.activeFilterButton]}
              onPress={() => setSortBy('time')}
            >
              <Text style={[styles.filterText, sortBy === 'time' && styles.activeFilterText]}>
                🕒 En Yeni
              </Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.filterButton, sortBy === 'importance' && styles.activeFilterButton]}
              onPress={() => setSortBy('importance')}
            >
              <Text style={[styles.filterText, sortBy === 'importance' && styles.activeFilterText]}>
                ⚡ Önemli
              </Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.filterButton, sortBy === 'relevance' && styles.activeFilterButton]}
              onPress={() => setSortBy('relevance')}
            >
              <Text style={[styles.filterText, sortBy === 'relevance' && styles.activeFilterText]}>
                🎯 İlgili
              </Text>
            </TouchableOpacity>
          </ScrollView>
        </View>
      )}

      {/* News List */}
      <ScrollView
        style={styles.content}
        showsVerticalScrollIndicator={false}
        nestedScrollEnabled={true}
      >
        {filteredNews.map((item, index) => (
          <NewsCard
            key={`${item.id}-${index}`}
            item={item}
            variant={compact ? 'compact' : 'full'}
            showImage={showImages && !compact}
            onPress={onNewsItemPress}
            maxTitleLines={compact ? 2 : 3}
            maxSummaryLines={compact ? 1 : 2}
          />
        ))}

        {filteredNews.length === 0 && searchQuery.trim() && (
          <View style={styles.noResultsState}>
            <Text style={styles.noResultsIcon}>🔍</Text>
            <Text style={styles.noResultsText}>
              "{searchQuery}" için sonuç bulunamadı
            </Text>
            <TouchableOpacity onPress={handleClearSearch} style={styles.clearSearchButton}>
              <Text style={styles.clearSearchText}>Aramayı temizle</Text>
            </TouchableOpacity>
          </View>
        )}
      </ScrollView>

      {/* Footer */}
      {variant === 'dashboard' && news.length > maxItems && (
        <TouchableOpacity style={styles.footer} onPress={onPress}>
          <Text style={styles.viewAllText}>
            {news.length - filteredNews.length > 0
              ? `${news.length - filteredNews.length} haber daha göster`
              : 'Tüm haberleri gör'
            }
          </Text>
          <Text style={styles.footerIcon}>📖</Text>
        </TouchableOpacity>
      )}

      {/* Real-time indicator */}
      {enableRealTime && (
        <View style={styles.realTimeIndicator}>
          <View style={styles.liveDot} />
          <Text style={styles.liveText}>Canlı</Text>
        </View>
      )}
    </View>
  );
});

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
    overflow: 'visible',
    position: 'relative',
  },
  standaloneContainer: {
    flex: 1,
    marginBottom: 0,
    borderRadius: 0,
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
  searchContainer: {
    paddingHorizontal: 16,
    paddingBottom: 12,
  },
  categoriesContainer: {
    paddingBottom: 8,
  },
  filtersContainer: {
    paddingBottom: 8,
  },
  filtersContent: {
    paddingHorizontal: 16,
    gap: 8,
  },
  filterButton: {
    backgroundColor: '#f8fafc',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#e2e8f0',
  },
  activeFilterButton: {
    backgroundColor: '#667eea',
    borderColor: '#667eea',
  },
  filterText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#64748b',
  },
  activeFilterText: {
    color: 'white',
  },
  content: {
    maxHeight: 400,
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
  noResultsState: {
    alignItems: 'center',
    paddingVertical: 32,
    paddingHorizontal: 16,
  },
  noResultsIcon: {
    fontSize: 48,
    marginBottom: 12,
  },
  noResultsText: {
    fontSize: 14,
    color: '#6b7280',
    textAlign: 'center',
    marginBottom: 16,
  },
  clearSearchButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
  },
  clearSearchText: {
    fontSize: 12,
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
  realTimeIndicator: {
    position: 'absolute',
    top: 16,
    right: 48,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#ef4444',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
    gap: 4,
  },
  liveDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
    backgroundColor: 'white',
  },
  liveText: {
    fontSize: 10,
    fontWeight: '600',
    color: 'white',
  },
});

export default EnhancedNewsPreview;