import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  RefreshControl,
  Alert,
  Linking,
  StatusBar,
  Platform,
  TouchableOpacity,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { RootStackParamList, NewsItem } from '../types';
import { useAuth } from '../context/AuthContext';
import { apiService } from '../services/api';
import { EnhancedNewsPreview } from '../components/news';

type NewsNavigationProp = StackNavigationProp<RootStackParamList>;

interface EnhancedNewsScreenProps {
  isModal?: boolean;
  onClose?: () => void;
}

const EnhancedNewsScreen: React.FC<EnhancedNewsScreenProps> = ({
  isModal = false,
  onClose,
}) => {
  const navigation = useNavigation<NewsNavigationProp>();
  const { user } = useAuth();

  const [news, setNews] = useState<NewsItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // News search and filter state
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('all');

  // Recent searches for the search component
  const [recentSearches, setRecentSearches] = useState<string[]>([
    'Bitcoin',
    'Ethereum',
    'BIST',
    'Fed kararı',
    'Altcoin',
  ]);

  // Search suggestions
  const searchSuggestions = [
    'Bitcoin fiyat analizi',
    'Ethereum 2.0 güncellemesi',
    'BIST 100 endeksi',
    'Fed faiz kararı',
    'Altcoin sezonu',
    'DeFi protokolleri',
    'NFT pazar analizi',
    'Kripto düzenlemeler',
    'Merkez bankası dijital para',
    'Blockchain teknolojisi',
  ];

  const loadNews = useCallback(async () => {
    try {
      setError(null);
      const newsData = await apiService.getMarketNews(undefined, 50); // Load more news for the full screen
      setNews(newsData);
    } catch (err) {
      console.error('Error loading news:', err);
      setError('Haberler yüklenirken bir hata oluştu.');

      // Fallback to mock data for development
      const mockNews: NewsItem[] = [
        {
          id: '1',
          title: 'Bitcoin 110.000 Dolları Aştı: Yeni Rekor Seviyeye Ulaştı',
          summary: 'Bitcoin, kurumsal yatırımcıların artan ilgisi ve ETF girişleri ile birlikte 110.000 dolar seviyesini aşarak yeni bir rekor kırdı.',
          content: 'Bitcoin fiyatı bugün 110.000 dolar seviyesini aşarak tarihi bir rekor kırdı...',
          url: 'https://example.com/bitcoin-110k',
          source: 'CoinDesk',
          author: 'Sarah Johnson',
          publishedAt: new Date(Date.now() - 30 * 60 * 1000).toISOString(), // 30 minutes ago
          category: 'cryptocurrency',
          tags: ['bitcoin', 'price', 'record', 'institutional'],
          relatedSymbols: ['BTCUSDT', 'BTCEUR'],
          sentiment: 'POSITIVE',
          importance: 'HIGH',
          imageUrl: 'https://example.com/bitcoin-chart.jpg',
          language: 'tr',
        },
        {
          id: '2',
          title: 'BIST 100 Endeksi Güçlü Yükselişle Haftayı Tamamladı',
          summary: 'Borsa İstanbul\'da BIST 100 endeksi, bankacılık ve teknoloji hisselerinin öncülüğünde %2.5 yükselişle haftayı tamamladı.',
          content: 'BIST 100 endeksi bu hafta güçlü bir performans sergileyerek...',
          url: 'https://example.com/bist-100-rise',
          source: 'Bloomberg HT',
          author: 'Mehmet Kaya',
          publishedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
          category: 'stock',
          tags: ['bist', 'borsa', 'turkey', 'banking', 'technology'],
          relatedSymbols: ['XU100', 'GARAN', 'AKBNK', 'TOASO'],
          sentiment: 'POSITIVE',
          importance: 'MEDIUM',
          language: 'tr',
        },
        {
          id: '3',
          title: 'Ethereum Staking Ödülleri Rekor Seviyeye Ulaştı',
          summary: 'Ethereum 2.0 staking ödülleri, ağ aktivitesindeki artış ve protokol güncellemeleri sonrasında rekor seviyelere çıktı.',
          content: 'Ethereum staking ödülleri son güncellemeler ile...',
          url: 'https://example.com/ethereum-staking',
          source: 'CryptoNews',
          publishedAt: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(), // 4 hours ago
          category: 'cryptocurrency',
          tags: ['ethereum', 'staking', 'defi', 'protocol'],
          relatedSymbols: ['ETHUSDT', 'ETHEUR'],
          sentiment: 'POSITIVE',
          importance: 'HIGH',
          language: 'tr',
        },
        {
          id: '4',
          title: 'Fed Faiz Kararı Piyasaları Nasıl Etkileyecek?',
          summary: 'Federal Reserve\'in bu akşam açıklayacağı faiz kararı, küresel piyasalar ve kripto para birimleri için kritik önem taşıyor.',
          content: 'Federal Reserve\'in faiz kararı beklentileri...',
          url: 'https://example.com/fed-decision',
          source: 'Reuters',
          publishedAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(), // 6 hours ago
          category: 'economy',
          tags: ['fed', 'interest-rates', 'monetary-policy', 'global-markets'],
          relatedSymbols: ['DXY', 'USDTRY'],
          sentiment: 'NEUTRAL',
          importance: 'HIGH',
          language: 'tr',
        },
        {
          id: '5',
          title: 'DeFi Sektöründe TVL 300 Milyar Doları Aştı',
          summary: 'Merkezi olmayan finans (DeFi) protokollerinde toplam kilitli değer (TVL) 300 milyar dolar sınırını aşarak yeni bir kilometre taşına ulaştı.',
          content: 'DeFi sektöründe toplam kilitli değer...',
          url: 'https://example.com/defi-tvl-record',
          source: 'DeFi Pulse',
          publishedAt: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(), // 8 hours ago
          category: 'cryptocurrency',
          tags: ['defi', 'tvl', 'protocols', 'yield-farming'],
          relatedSymbols: ['UNI', 'AAVE', 'COMP', 'SUSHI'],
          sentiment: 'POSITIVE',
          importance: 'MEDIUM',
          language: 'tr',
        },
      ];
      setNews(mockNews);
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    loadNews();
  }, [loadNews]);

  const handleRefresh = useCallback(async () => {
    setIsRefreshing(true);
    await loadNews();
  }, [loadNews]);

  const handleNewsItemPress = useCallback(async (newsItem: NewsItem) => {
    try {
      await Linking.openURL(newsItem.url);
    } catch (error) {
      console.error('Error opening news URL:', error);
      Alert.alert('Hata', 'Haber açılamadı');
    }
  }, []);

  const handleCategorySelect = useCallback((category: string) => {
    setSelectedCategory(category);
  }, []);

  const handleSearch = useCallback((query: string) => {
    setSearchQuery(query);

    // Add to recent searches if not empty and not already in the list
    if (query.trim() && !recentSearches.includes(query.trim())) {
      setRecentSearches(prev => [query.trim(), ...prev.slice(0, 4)]);
    }
  }, [recentSearches]);

  const handleRecentSearchPress = useCallback((query: string) => {
    setSearchQuery(query);
  }, []);

  const handleSuggestionPress = useCallback((suggestion: string) => {
    setSearchQuery(suggestion);
  }, []);

  return (
    <View style={styles.container}>
      <StatusBar
        barStyle={Platform.OS === 'ios' ? 'light-content' : 'default'}
        backgroundColor="#667eea"
      />

      <LinearGradient
        colors={['#667eea', '#764ba2']}
        style={styles.header}
      >
        <View style={styles.headerContent}>
          <View style={styles.titleSection}>
            <Text style={styles.title}>📰 Piyasa Haberleri</Text>
            <Text style={styles.subtitle}>
              {news.length} haber • Anlık güncellemeler
            </Text>
          </View>

          <View style={styles.userSection}>
            {isModal && (
              <TouchableOpacity
                onPress={onClose}
                style={styles.closeButton}
                hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
              >
                <Text style={styles.closeButtonText}>✖️</Text>
              </TouchableOpacity>
            )}

            {user && (
              <Text style={styles.userText}>👤 {user.first_name}</Text>
            )}
          </View>
        </View>
      </LinearGradient>

      <View style={styles.content}>
        <EnhancedNewsPreview
          news={news}
          isLoading={isLoading}
          maxItems={news.length} // Show all news in full screen
          showImages={true}
          compact={false}
          showSearch={true}
          showCategories={true}
          showFilters={true}
          onNewsItemPress={handleNewsItemPress}
          onCategorySelect={handleCategorySelect}
          onSearch={handleSearch}
          title="" // Hide title since we have header
          variant="standalone"
          enableRealTime={true}
        />
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  header: {
    paddingTop: Platform.OS === 'ios' ? 50 : 25,
    paddingBottom: 20,
    paddingHorizontal: 20,
  },
  headerContent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  titleSection: {
    flex: 1,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: 'white',
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 14,
    color: 'rgba(255,255,255,0.8)',
  },
  userSection: {
    alignItems: 'flex-end',
    gap: 12,
  },
  closeButton: {
    padding: 8,
  },
  closeButtonText: {
    fontSize: 16,
    color: 'rgba(255,255,255,0.9)',
  },
  userText: {
    fontSize: 14,
    color: 'rgba(255,255,255,0.9)',
    fontWeight: '600',
  },
  content: {
    flex: 1,
  },
});

export default EnhancedNewsScreen;