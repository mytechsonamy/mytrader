// frontend/web/src/components/dashboard/__tests__/LeaderboardSection.test.tsx

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import { act } from 'react-dom/test-utils';
import LeaderboardSection from '../LeaderboardSection';
import * as marketDataService from '../../../services/marketDataService';

// Mock services
vi.mock('../../../services/marketDataService');

describe('LeaderboardSection', () => {
  const mockLeaderboardData = {
    topPerformers: [
      {
        id: 1,
        username: 'trader1',
        returnRate: 25.5,
        totalValue: 125500,
        rank: 1,
        avatar: null,
        badges: ['top-performer']
      },
      {
        id: 2,
        username: 'trader2',
        returnRate: 20.3,
        totalValue: 120300,
        rank: 2,
        avatar: null,
        badges: []
      },
      {
        id: 3,
        username: 'trader3',
        returnRate: 15.2,
        totalValue: 115200,
        rank: 3,
        avatar: null,
        badges: []
      }
    ],
    currentUser: {
      rank: 45,
      returnRate: 5.2,
      totalValue: 105200
    },
    totalParticipants: 1250,
    lastUpdated: new Date().toISOString()
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  describe('Data Loading', () => {
    it('should display loading state initially', () => {
      vi.mocked(marketDataService.getLeaderboard).mockImplementation(
        () => new Promise(() => {}) // Never resolves to keep loading
      );

      render(<LeaderboardSection />);
      
      expect(screen.getByTestId('leaderboard-skeleton')).toBeInTheDocument();
    });

    it('should load and display leaderboard data', async () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(mockLeaderboardData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByText('trader1')).toBeInTheDocument();
        expect(screen.getByText('trader2')).toBeInTheDocument();
        expect(screen.getByText('trader3')).toBeInTheDocument();
      });

      expect(screen.getByText('25.5%')).toBeInTheDocument();
      expect(screen.getByText('₺125,500')).toBeInTheDocument();
    });

    it('should handle empty leaderboard gracefully', async () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue({
        topPerformers: [],
        currentUser: null,
        totalParticipants: 0,
        lastUpdated: new Date().toISOString()
      });

      render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByText('No participants yet')).toBeInTheDocument();
      });
    });
  });

  describe('Error Handling', () => {
    it('should display error message when API call fails', async () => {
      const errorMessage = 'Failed to fetch leaderboard';
      vi.mocked(marketDataService.getLeaderboard).mockRejectedValue(
        new Error(errorMessage)
      );

      render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByTestId('error-message')).toBeInTheDocument();
        expect(screen.getByText(/Unable to load leaderboard/i)).toBeInTheDocument();
      });
    });

    it('should allow retry after error', async () => {
      vi.mocked(marketDataService.getLeaderboard)
        .mockRejectedValueOnce(new Error('Network error'))
        .mockResolvedValueOnce(mockLeaderboardData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByTestId('retry-button')).toBeInTheDocument();
      });

      const retryButton = screen.getByTestId('retry-button');
      act(() => {
        retryButton.click();
      });

      await waitFor(() => {
        expect(screen.getByText('trader1')).toBeInTheDocument();
      });
    });
  });

  describe('Data Validation', () => {
    it('should handle missing user data safely', async () => {
      const dataWithMissingFields = {
        topPerformers: [
          {
            id: 1,
            username: null, // Missing username
            returnRate: undefined, // Missing return rate
            totalValue: 100000,
            rank: 1
          }
        ],
        currentUser: null,
        totalParticipants: 100
      };

      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(dataWithMissingFields);

      render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByText('Anonymous')).toBeInTheDocument();
        expect(screen.getByText('0.0%')).toBeInTheDocument();
      });
    });

    it('should handle non-array topPerformers data', async () => {
      const invalidData = {
        topPerformers: 'not-an-array', // Invalid data type
        currentUser: null,
        totalParticipants: 0
      };

      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(invalidData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByText('No participants yet')).toBeInTheDocument();
      });
      
      // Should not crash
      expect(screen.queryByTestId('error-boundary')).not.toBeInTheDocument();
    });
  });

  describe('User Interaction', () => {
    it('should highlight current user rank if available', async () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(mockLeaderboardData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        const userRankElement = screen.getByTestId('current-user-rank');
        expect(userRankElement).toBeInTheDocument();
        expect(userRankElement).toHaveTextContent('#45');
        expect(userRankElement).toHaveClass('highlight');
      });
    });

    it('should show view all button when more participants exist', async () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(mockLeaderboardData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        const viewAllButton = screen.getByRole('button', { name: /view all 1,250 participants/i });
        expect(viewAllButton).toBeInTheDocument();
      });
    });
  });

  describe('Real-time Updates', () => {
    it('should update when receiving WebSocket data', async () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(mockLeaderboardData);

      const { rerender } = render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByText('trader1')).toBeInTheDocument();
      });

      // Simulate WebSocket update
      const updatedData = {
        ...mockLeaderboardData,
        topPerformers: [
          {
            ...mockLeaderboardData.topPerformers[0],
            returnRate: 30.5 // Updated return rate
          },
          ...mockLeaderboardData.topPerformers.slice(1)
        ]
      };

      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(updatedData);

      act(() => {
        // Trigger re-render with new data
        rerender(<LeaderboardSection key="updated" />);
      });

      await waitFor(() => {
        expect(screen.getByText('30.5%')).toBeInTheDocument();
      });
    });
  });

  describe('@smoke', () => {
    it('should render without crashing', () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(mockLeaderboardData);
      
      const { container } = render(<LeaderboardSection />);
      expect(container.firstChild).toBeInTheDocument();
    });

    it('should have proper accessibility attributes', async () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(mockLeaderboardData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        const leaderboardSection = screen.getByRole('region', { name: /leaderboard/i });
        expect(leaderboardSection).toBeInTheDocument();
        expect(leaderboardSection).toHaveAttribute('aria-label', 'Competition Leaderboard');
      });
    });
  });

  describe('Performance', () => {
    it('should not re-fetch data on every render', async () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(mockLeaderboardData);

      const { rerender } = render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByText('trader1')).toBeInTheDocument();
      });

      expect(marketDataService.getLeaderboard).toHaveBeenCalledTimes(1);

      // Re-render component
      rerender(<LeaderboardSection />);

      // Should not call API again
      expect(marketDataService.getLeaderboard).toHaveBeenCalledTimes(1);
    });

    it('should debounce rapid refresh requests', async () => {
      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(mockLeaderboardData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByText('trader1')).toBeInTheDocument();
      });

      const refreshButton = screen.getByTestId('refresh-button');

      // Click refresh multiple times rapidly
      act(() => {
        refreshButton.click();
        refreshButton.click();
        refreshButton.click();
      });

      // Should only make one additional API call despite multiple clicks
      await waitFor(() => {
        expect(marketDataService.getLeaderboard).toHaveBeenCalledTimes(2);
      });
    });
  });

  describe('Edge Cases', () => {
    it('should handle very large numbers correctly', async () => {
      const largeNumberData = {
        topPerformers: [
          {
            id: 1,
            username: 'whale',
            returnRate: 9999.99,
            totalValue: 999999999,
            rank: 1
          }
        ],
        currentUser: null,
        totalParticipants: 1000000
      };

      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(largeNumberData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        expect(screen.getByText('9,999.99%')).toBeInTheDocument();
        expect(screen.getByText('₺999,999,999')).toBeInTheDocument();
      });
    });

    it('should handle negative returns correctly', async () => {
      const negativeData = {
        topPerformers: [
          {
            id: 1,
            username: 'loser',
            returnRate: -15.5,
            totalValue: 84500,
            rank: 1
          }
        ],
        currentUser: null,
        totalParticipants: 1
      };

      vi.mocked(marketDataService.getLeaderboard).mockResolvedValue(negativeData);

      render(<LeaderboardSection />);

      await waitFor(() => {
        const returnElement = screen.getByText('-15.5%');
        expect(returnElement).toBeInTheDocument();
        expect(returnElement).toHaveClass('text-red-500'); // Should show in red
      });
    });
  });
});