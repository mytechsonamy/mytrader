import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import NewsSection from '../../dashboard/NewsSection';
import { renderWithProviders, createGuestState } from '../../../test-utils';
import { marketDataService } from '../../../services/marketDataService';

// Mock the market data service
vi.mock('../../../services/marketDataService', () => ({
  marketDataService: {
    getNews: vi.fn(),
  },
}));

const mockMarketDataService = marketDataService as any;

describe('NewsSection Component', () => {
  const mockNewsArticles = [
    {
      id: '1',
      title: 'Test Market News Article',
      summary: 'This is a test summary for the market news article with important information.',
      source: 'Reuters',
      publishedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
      url: 'https://example.com/news/1',
      category: 'market',
      tags: ['market', 'stocks']
    },
    {
      id: '2',
      title: 'Cryptocurrency Market Update',
      summary: 'Latest developments in the cryptocurrency market showing positive trends.',
      source: 'CoinDesk',
      publishedAt: new Date(Date.now() - 30 * 60 * 1000).toISOString(), // 30 minutes ago
      url: 'https://example.com/news/2',
      category: 'crypto',
      tags: ['crypto', 'bitcoin']
    },
    {
      id: '3',
      title: 'Federal Reserve Policy Decision',
      summary: 'Central bank announces new policy measures affecting interest rates.',
      source: 'Bloomberg',
      publishedAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(), // 6 hours ago
      url: 'https://example.com/news/3',
      category: 'market',
      tags: ['policy', 'fed']
    }
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
    
    // Mock successful news fetch by default
    mockMarketDataService.getNews.mockResolvedValue(mockNewsArticles);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('Loading State', () => {
    it('shows loading state initially', () => {
      // Make the mock pending
      mockMarketDataService.getNews.mockImplementation(
        () => new Promise(() => {}) // Never resolves
      );

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Loading latest market news...')).toBeInTheDocument();
      expect(screen.getByText('Market News')).toBeInTheDocument();
    });

    it('shows loading spinner during fetch', () => {
      mockMarketDataService.getNews.mockImplementation(
        () => new Promise(() => {})
      );

      const { container } = renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      expect(container.querySelector('.spinner')).toBeInTheDocument();
    });
  });

  describe('Successful Data Loading', () => {
    it('renders news articles successfully', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Test Market News Article')).toBeInTheDocument();
        expect(screen.getByText('Cryptocurrency Market Update')).toBeInTheDocument();
        expect(screen.getByText('Federal Reserve Policy Decision')).toBeInTheDocument();
      });
    });

    it('displays correct news sources', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Reuters')).toBeInTheDocument();
        expect(screen.getByText('CoinDesk')).toBeInTheDocument();
        expect(screen.getByText('Bloomberg')).toBeInTheDocument();
      });
    });

    it('shows article summaries', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText(/This is a test summary/)).toBeInTheDocument();
        expect(screen.getByText(/Latest developments in the cryptocurrency/)).toBeInTheDocument();
        expect(screen.getByText(/Central bank announces/)).toBeInTheDocument();
      });
    });

    it('displays article tags', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('market')).toBeInTheDocument();
        expect(screen.getByText('stocks')).toBeInTheDocument();
        expect(screen.getByText('crypto')).toBeInTheDocument();
        expect(screen.getByText('bitcoin')).toBeInTheDocument();
        expect(screen.getByText('policy')).toBeInTheDocument();
        expect(screen.getByText('fed')).toBeInTheDocument();
      });
    });

    it('shows source logos (first letter)', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('R')).toBeInTheDocument(); // Reuters
        expect(screen.getByText('C')).toBeInTheDocument(); // CoinDesk
        expect(screen.getByText('B')).toBeInTheDocument(); // Bloomberg
      });
    });
  });

  describe('Time Formatting', () => {
    it('formats time correctly for recent articles', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('30m ago')).toBeInTheDocument(); // 30 minutes
        expect(screen.getByText('2h ago')).toBeInTheDocument(); // 2 hours
        expect(screen.getByText('6h ago')).toBeInTheDocument(); // 6 hours
      });
    });

    it('handles different time ranges correctly', async () => {
      const timeVariedArticles = [
        {
          ...mockNewsArticles[0],
          publishedAt: new Date(Date.now() - 5 * 60 * 1000).toISOString(), // 5 minutes
        },
        {
          ...mockNewsArticles[1],
          publishedAt: new Date(Date.now() - 25 * 60 * 60 * 1000).toISOString(), // 25 hours
        },
        {
          ...mockNewsArticles[2],
          publishedAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(), // 3 days
        }
      ];

      mockMarketDataService.getNews.mockResolvedValue(timeVariedArticles);

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('5m ago')).toBeInTheDocument();
        expect(screen.getByText('1d ago')).toBeInTheDocument(); // 25 hours -> 1 day
        expect(screen.getByText('3d ago')).toBeInTheDocument();
      });
    });
  });

  describe('Error Handling', () => {
    it('shows error state when fetch fails', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockMarketDataService.getNews.mockRejectedValue(new Error('Network error'));

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Unable to Load News')).toBeInTheDocument();
        expect(screen.getByText('Network error')).toBeInTheDocument();
        expect(screen.getByText('Retry')).toBeInTheDocument();
      });

      consoleSpy.mockRestore();
    });

    it('falls back to mock data when fetch fails', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockMarketDataService.getNews.mockRejectedValue(new Error('API error'));

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      // Should still show the component with fallback data, but also show error indicator
      await waitFor(() => {
        expect(screen.getByText('Market Analysis: Tech Stocks Show Strong Performance')).toBeInTheDocument();
      });

      consoleSpy.mockRestore();
    });

    it('shows error indicator when there is partial failure', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockMarketDataService.getNews.mockRejectedValue(new Error('Partial failure'));

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        const errorIndicator = screen.getByTitle('Error: Partial failure');
        expect(errorIndicator).toBeInTheDocument();
        expect(errorIndicator).toHaveTextContent('⚠️');
      });

      consoleSpy.mockRestore();
    });

    it('handles retry button click', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      
      // First call fails
      mockMarketDataService.getNews
        .mockRejectedValueOnce(new Error('Network error'))
        .mockResolvedValue([]); // Empty but successful

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Retry')).toBeInTheDocument();
      });

      // Mock window.location.reload
      const mockReload = vi.fn();
      Object.defineProperty(window, 'location', {
        value: { reload: mockReload },
        writable: true,
      });

      const retryButton = screen.getByText('Retry');
      await userEvent.click(retryButton);

      expect(mockReload).toHaveBeenCalled();

      consoleSpy.mockRestore();
    });
  });

  describe('Data Service Integration', () => {
    it('calls marketDataService.getNews with correct parameters', () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      expect(mockMarketDataService.getNews).toHaveBeenCalledWith('market', 10);
    });

    it('handles non-array response gracefully', async () => {
      mockMarketDataService.getNews.mockResolvedValue({ invalid: 'response' });

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        // Should not crash and should show empty state or fallback
        expect(screen.getByText('Market News')).toBeInTheDocument();
      });
    });

    it('handles null response gracefully', async () => {
      mockMarketDataService.getNews.mockResolvedValue(null);

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Market News')).toBeInTheDocument();
      });
    });
  });

  describe('Auto-refresh Functionality', () => {
    it('sets up auto-refresh interval', () => {
      const setIntervalSpy = vi.spyOn(global, 'setInterval');

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      expect(setIntervalSpy).toHaveBeenCalledWith(
        expect.any(Function),
        5 * 60 * 1000 // 5 minutes
      );

      setIntervalSpy.mockRestore();
    });

    it('refreshes news every 5 minutes', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      expect(mockMarketDataService.getNews).toHaveBeenCalledTimes(1);

      // Fast-forward 5 minutes
      vi.advanceTimersByTime(5 * 60 * 1000);

      await waitFor(() => {
        expect(mockMarketDataService.getNews).toHaveBeenCalledTimes(2);
      });
    });

    it('cleans up interval on unmount', () => {
      const clearIntervalSpy = vi.spyOn(global, 'clearInterval');

      const { unmount } = renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      unmount();

      expect(clearIntervalSpy).toHaveBeenCalled();
      clearIntervalSpy.mockRestore();
    });
  });

  describe('CSS Classes and Styling', () => {
    it('applies custom className prop', () => {
      const { container } = renderWithProviders(
        <NewsSection className="custom-news-class" />,
        { preloadedState: createGuestState() }
      );

      const sectionCard = container.querySelector('.section-card');
      expect(sectionCard).toHaveClass('custom-news-class');
    });

    it('applies correct tag classes', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        const marketTag = screen.getByText('market');
        expect(marketTag).toHaveClass('news-tag', 'market');

        const cryptoTag = screen.getByText('crypto');
        expect(cryptoTag).toHaveClass('news-tag', 'crypto');
      });
    });

    it('renders proper semantic HTML structure', async () => {
      const { container } = renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        const articles = container.querySelectorAll('article.news-article');
        expect(articles).toHaveLength(3);

        const headings = container.querySelectorAll('.news-title');
        expect(headings).toHaveLength(3);
      });
    });
  });

  describe('Edge Cases and Malformed Data', () => {
    it('handles articles with missing properties', async () => {
      const malformedArticles = [
        {
          id: '1',
          title: 'Article with missing data',
          summary: '',
          source: '',
          publishedAt: new Date().toISOString(),
          url: '',
          category: 'market',
          tags: []
        },
        {
          id: '2',
          // Missing title
          summary: 'Article without title',
          source: 'Test Source',
          publishedAt: new Date().toISOString(),
          url: 'https://example.com',
          category: 'market',
          tags: ['test']
        }
      ];

      mockMarketDataService.getNews.mockResolvedValue(malformedArticles);

      expect(() => {
        renderWithProviders(<NewsSection />, {
          preloadedState: createGuestState(),
        });
      }).not.toThrow();

      await waitFor(() => {
        expect(screen.getByText('Article with missing data')).toBeInTheDocument();
      });
    });

    it('handles articles with invalid dates', async () => {
      const invalidDateArticle = [{
        ...mockNewsArticles[0],
        publishedAt: 'invalid-date'
      }];

      mockMarketDataService.getNews.mockResolvedValue(invalidDateArticle);

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        // Component should still render without crashing
        expect(screen.getByText('Test Market News Article')).toBeInTheDocument();
      });
    });

    it('handles articles with non-array tags', async () => {
      const invalidTagsArticle = [{
        ...mockNewsArticles[0],
        tags: 'not-an-array' as any
      }];

      mockMarketDataService.getNews.mockResolvedValue(invalidTagsArticle);

      expect(() => {
        renderWithProviders(<NewsSection />, {
          preloadedState: createGuestState(),
        });
      }).not.toThrow();
    });

    it('handles very long article titles and summaries', async () => {
      const longContentArticle = [{
        ...mockNewsArticles[0],
        title: 'A'.repeat(500),
        summary: 'B'.repeat(2000)
      }];

      mockMarketDataService.getNews.mockResolvedValue(longContentArticle);

      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('A'.repeat(500))).toBeInTheDocument();
        expect(screen.getByText('B'.repeat(2000))).toBeInTheDocument();
      });
    });
  });

  describe('Accessibility', () => {
    it('provides proper semantic structure for screen readers', async () => {
      const { container } = renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        const articles = container.querySelectorAll('article[class="news-article"]');
        expect(articles).toHaveLength(3);

        const headings = container.querySelectorAll('h3.news-title');
        expect(headings).toHaveLength(3);

        const sectionHeading = screen.getByRole('heading', { level: 2 });
        expect(sectionHeading).toHaveTextContent('Market News');
      });
    });

    it('has proper heading hierarchy', async () => {
      renderWithProviders(<NewsSection />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        // Main section heading (h2)
        expect(screen.getByRole('heading', { level: 2, name: /Market News/ })).toBeInTheDocument();
        
        // Article titles (h3)
        const articleHeadings = screen.getAllByRole('heading', { level: 3 });
        expect(articleHeadings).toHaveLength(3);
      });
    });
  });
});
