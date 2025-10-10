import { useEffect, useState } from 'react';
import { marketDataService } from '../../services/marketDataService';

interface NewsArticle {
  id: string;
  title: string;
  summary: string;
  source: string;
  publishedAt: string;
  url: string;
  category: string;
  tags: string[];
  imageUrl?: string;
}

interface NewsSectionProps {
  className?: string;
}

const NewsSection: React.FC<NewsSectionProps> = ({ className = '' }) => {
  const [news, setNews] = useState<NewsArticle[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchNews = async () => {
      try {
        setLoading(true);
        setError(null);
        const newsData = await marketDataService.getNews('market', 10);
        setNews(Array.isArray(newsData) ? newsData : []);
      } catch (err: any) {
        console.error('Failed to fetch news:', err);
        setError(err.message || 'Failed to load news');
        // Fallback to mock data on error
        setNews(getMockNews());
      } finally {
        setLoading(false);
      }
    };

    fetchNews();

    // Set up auto-refresh every 5 minutes
    const interval = setInterval(fetchNews, 5 * 60 * 1000);

    return () => clearInterval(interval);
  }, []);

  const getMockNews = (): NewsArticle[] => [
    {
      id: '1',
      title: 'Market Analysis: Tech Stocks Show Strong Performance',
      summary: 'Technology sector continues to outperform expectations with major companies reporting robust quarterly earnings...',
      source: 'Financial Times',
      publishedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
      url: '#',
      category: 'market',
      tags: ['stocks', 'market']
    },
    {
      id: '2',
      title: 'Bitcoin Reaches New Monthly High',
      summary: 'Cryptocurrency markets show renewed strength as institutional adoption continues to grow...',
      source: 'CoinDesk',
      publishedAt: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(), // 4 hours ago
      url: '#',
      category: 'crypto',
      tags: ['crypto', 'market']
    },
    {
      id: '3',
      title: 'Federal Reserve Policy Update',
      summary: 'Central bank maintains current interest rates while signaling potential changes in upcoming quarters...',
      source: 'Reuters',
      publishedAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(), // 6 hours ago
      url: '#',
      category: 'market',
      tags: ['market']
    }
  ];

  const formatTimeAgo = (timestamp: string): string => {
    const now = new Date();
    const publishedTime = new Date(timestamp);
    const diffInMinutes = Math.floor((now.getTime() - publishedTime.getTime()) / (1000 * 60));

    if (diffInMinutes < 60) {
      return `${diffInMinutes}m ago`;
    } else if (diffInMinutes < 1440) {
      const hours = Math.floor(diffInMinutes / 60);
      return `${hours}h ago`;
    } else {
      const days = Math.floor(diffInMinutes / 1440);
      return `${days}d ago`;
    }
  };

  const getSourceLogo = (source: string): string => {
    return source.charAt(0).toUpperCase();
  };

  if (loading) {
    return (
      <div className={`section-card ${className}`}>
        <div className="section-header">
          <h2 className="section-title">
            <div className="section-icon">📰</div>
            <span className="section-title-text">Market News</span>
          </h2>
        </div>
        <div className="section-content">
          <div className="loading-state">
            <div className="spinner"></div>
            <p>Loading latest market news...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error && news.length === 0) {
    return (
      <div className={`section-card ${className}`}>
        <div className="section-header">
          <h2 className="section-title">
            <div className="section-icon">📰</div>
            <span className="section-title-text">Market News</span>
          </h2>
        </div>
        <div className="section-content">
          <div className="error-state">
            <h3>Unable to Load News</h3>
            <p>{error}</p>
            <button
              onClick={() => window.location.reload()}
              className="retry-button"
            >
              Retry
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`section-card ${className}`}>
      <div className="section-header">
        <h2 className="section-title">
          <div className="section-icon">📰</div>
          <span className="section-title-text">Market News</span>
        </h2>
        {error && (
          <div className="status-indicator error" title={`Error: ${error}`}>
            ⚠️
          </div>
        )}
      </div>
      <div className="section-content">
        <div className="news-feed">
          {Array.isArray(news) && news.map((article) => (
            <article key={article.id} className="news-article">
              <div className="news-header">
                <div className="news-source">
                  <div className="source-logo">
                    {getSourceLogo(article.source)}
                  </div>
                  <span className="source-name">{article.source}</span>
                </div>
                <span className="news-time">{formatTimeAgo(article.publishedAt)}</span>
              </div>
              <h3 className="news-title">{article.title}</h3>
              <p className="news-summary">{article.summary}</p>
              <div className="news-tags">
                {Array.isArray(article.tags) && article.tags.map((tag) => (
                  <span key={tag} className={`news-tag ${tag}`}>
                    {tag}
                  </span>
                ))}
              </div>
            </article>
          ))}
        </div>
      </div>
    </div>
  );
};

export default NewsSection;