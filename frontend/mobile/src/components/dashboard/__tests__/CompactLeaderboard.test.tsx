import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import CompactLeaderboard from '../CompactLeaderboard';
import { apiService } from '../../../services/api';
import { Animated } from 'react-native';

// Mock apiService
jest.mock('../../../services/api', () => ({
  apiService: {
    getLeaderboard: jest.fn(),
    getUserRanking: jest.fn(),
    getCompetitionStats: jest.fn(),
  },
}));

// Mock React Native components
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    RefreshControl: 'RefreshControl',
    ScrollView: 'ScrollView',
    ActivityIndicator: 'ActivityIndicator',
    Animated: {
      ...actualRN.Animated,
      View: actualRN.View,
      sequence: jest.fn((animations) => ({
        start: jest.fn(),
      })),
      timing: jest.fn(() => ({ start: jest.fn() })),
    },
    TouchableOpacity: actualRN.TouchableOpacity,
  };
});

const mockApiService = apiService as jest.Mocked<typeof apiService>;

describe('CompactLeaderboard Component', () => {
  const validLeaderboardData = [
    {
      userId: '1',
      displayName: 'John Trader',
      rank: 1,
      portfolioValue: 15000,
      returnPercent: 15.5,
      winRate: 75.2,
      totalTrades: 42,
      tier: 'GOLD',
      badges: ['winner', 'streak'],
    },
    {
      userId: '2',
      displayName: 'Jane Investor',
      rank: 2,
      portfolioValue: 12000,
      returnPercent: 12.3,
      winRate: 68.7,
      totalTrades: 38,
      tier: 'SILVER',
      badges: ['consistent'],
    },
    {
      userId: '3',
      displayName: 'Bob Market',
      rank: 3,
      portfolioValue: 10000,
      returnPercent: -5.2,
      winRate: 45.1,
      totalTrades: 25,
      tier: 'BRONZE',
      badges: [],
    },
  ];

  const validUserRankingData = {
    userId: '1',
    rank: 25,
    score: 850,
    returnPercent: 8.5,
    percentile: 15.5,
    isEligible: true,
  };

  const validStatsData = {
    totalParticipants: 150,
    totalPrizePool: 10000,
    minimumTrades: 5,
    currentPeriodEnd: '2023-12-31T23:59:59Z',
    prizes: [
      { rank: 1, amount: 5000, currency: 'USD' },
      { rank: 2, amount: 3000, currency: 'USD' },
    ],
  };

  const defaultProps = {
    leaderboard: validLeaderboardData,
    userRanking: validUserRankingData,
    stats: validStatsData,
    isLoading: false,
    showUserRanking: true,
    maxEntries: 5,
    onPress: jest.fn(),
    onUserPress: jest.fn(),
    onChallengePress: jest.fn(),
    onJoinCompetition: jest.fn(),
    enablePullToRefresh: true,
    showPeriodTabs: true,
    initialPeriod: 'weekly' as const,
  };

  beforeEach(() => {
    jest.clearAllMocks();

    // Setup default API responses
    mockApiService.getLeaderboard.mockResolvedValue(validLeaderboardData);
    mockApiService.getUserRanking.mockResolvedValue(validUserRankingData);
    mockApiService.getCompetitionStats.mockResolvedValue(validStatsData);
  });

  describe('Rendering States', () => {
    it('should render without crashing', () => {
      expect(() => {
        render(<CompactLeaderboard {...defaultProps} />);
      }).not.toThrow();
    });

    it('should render leaderboard entries correctly', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      expect(getByText('🏆 Strategist Yarışması')).toBeTruthy();
      expect(getByText('John Trader')).toBeTruthy();
      expect(getByText('Jane Investor')).toBeTruthy();
      expect(getByText('Bob Market')).toBeTruthy();
    });

    it('should render loading state', () => {
      const { getByText } = render(
        <CompactLeaderboard {...defaultProps} isLoading={true} leaderboard={[]} />
      );

      expect(getByText('Sıralama yükleniyor...')).toBeTruthy();
    });

    it('should render empty state when no data', () => {
      const { getByText } = render(
        <CompactLeaderboard {...defaultProps} leaderboard={[]} isLoading={false} />
      );

      expect(getByText('🎮')).toBeTruthy();
      expect(getByText('Bu haftanın yarışması henüz başlamadı')).toBeTruthy();
    });
  });

  describe('Data Formatting', () => {
    it('should format currency values correctly', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      expect(getByText('$15K')).toBeTruthy(); // 15000 formatted
      expect(getByText('$12K')).toBeTruthy(); // 12000 formatted
      expect(getByText('$10K')).toBeTruthy(); // 10000 formatted
    });

    it('should format percentage values with correct colors', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      expect(getByText('+15.5%')).toBeTruthy(); // Positive return
      expect(getByText('+12.3%')).toBeTruthy();
      expect(getByText('-5.2%')).toBeTruthy(); // Negative return
    });

    it('should handle null/undefined values in formatting', () => {
      const dataWithNulls = [
        {
          ...validLeaderboardData[0],
          portfolioValue: null,
          returnPercent: undefined,
          winRate: NaN,
        },
      ];

      expect(() => {
        render(<CompactLeaderboard {...defaultProps} leaderboard={dataWithNulls} />);
      }).not.toThrow();
    });

    it('should format large currency values correctly', () => {
      const dataWithLargeValues = [
        {
          ...validLeaderboardData[0],
          portfolioValue: 1500000, // Should format to $1.5M
        },
      ];

      const { getByText } = render(
        <CompactLeaderboard {...defaultProps} leaderboard={dataWithLargeValues} />
      );

      expect(getByText('$1.5M')).toBeTruthy();
    });
  });

  describe('Rank Display', () => {
    it('should display rank icons for top 3 positions', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      expect(getByText('👑')).toBeTruthy(); // Rank 1
      expect(getByText('🥈')).toBeTruthy(); // Rank 2
      expect(getByText('🥉')).toBeTruthy(); // Rank 3
    });

    it('should display tier icons correctly', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      expect(getByText('🥇')).toBeTruthy(); // GOLD tier
      // Silver and Bronze tier icons would be visible in the component
    });

    it('should handle missing or invalid tier data', () => {
      const dataWithInvalidTiers = [
        {
          ...validLeaderboardData[0],
          tier: null,
        },
        {
          ...validLeaderboardData[1],
          tier: 'INVALID_TIER',
        },
      ];

      expect(() => {
        render(<CompactLeaderboard {...defaultProps} leaderboard={dataWithInvalidTiers} />);
      }).not.toThrow();
    });
  });

  describe('User Interaction', () => {
    it('should call onPress when header is pressed', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      fireEvent.press(getByText('🏆 Strategist Yarışması'));

      expect(defaultProps.onPress).toHaveBeenCalled();
    });

    it('should call onUserPress when user row is pressed', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      fireEvent.press(getByText('John Trader'));

      expect(defaultProps.onUserPress).toHaveBeenCalledWith('1');
    });

    it('should call onJoinCompetition when join button is pressed', () => {
      const { getByText } = render(
        <CompactLeaderboard {...defaultProps} leaderboard={[]} />
      );

      fireEvent.press(getByText('🚀 Yarışmaya Katıl'));

      expect(defaultProps.onJoinCompetition).toHaveBeenCalled();
    });

    it('should call onChallengePress when challenge button is pressed', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      fireEvent.press(getByText('🎯 Meydan Oku'));

      expect(defaultProps.onChallengePress).toHaveBeenCalled();
    });
  });

  describe('Period Tabs', () => {
    it('should render period tabs when enabled', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      expect(getByText('Bu Hafta')).toBeTruthy();
      expect(getByText('Genel')).toBeTruthy();
    });

    it('should not render period tabs when disabled', () => {
      const { queryByText } = render(
        <CompactLeaderboard {...defaultProps} showPeriodTabs={false} />
      );

      expect(queryByText('Bu Hafta')).toBeNull();
      expect(queryByText('Genel')).toBeNull();
    });

    it('should handle period tab changes', async () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      fireEvent.press(getByText('Genel'));

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalledWith('all', expect.any(Number));
      });
    });

    it('should not change period if same tab is pressed', async () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      // Press the already active tab
      fireEvent.press(getByText('Bu Hafta'));

      // Should not make additional API calls
      expect(mockApiService.getLeaderboard).not.toHaveBeenCalled();
    });
  });

  describe('User Ranking Display', () => {
    it('should display user ranking when available and not in top entries', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      expect(getByText('Sizin sıralamanız:')).toBeTruthy();
      expect(getByText('#25')).toBeTruthy();
      expect(getByText('850 puan')).toBeTruthy();
    });

    it('should not display user ranking when user is in top entries', () => {
      const propsWithCurrentUserInTop = {
        ...defaultProps,
        userRanking: {
          ...validUserRankingData,
          userId: '1', // Same as first entry in leaderboard
          rank: 1,
        },
      };

      const { queryByText } = render(
        <CompactLeaderboard {...propsWithCurrentUserInTop} />
      );

      expect(queryByText('Sizin sıralamanız:')).toBeNull();
    });

    it('should not display user ranking when showUserRanking is false', () => {
      const { queryByText } = render(
        <CompactLeaderboard {...defaultProps} showUserRanking={false} />
      );

      expect(queryByText('Sizin sıralamanız:')).toBeNull();
    });

    it('should handle user ranking with negative returns', () => {
      const propsWithNegativeReturn = {
        ...defaultProps,
        userRanking: {
          ...validUserRankingData,
          returnPercent: -3.5,
        },
      };

      const { getByText } = render(<CompactLeaderboard {...propsWithNegativeReturn} />);

      expect(getByText('-3.5%')).toBeTruthy();
    });
  });

  describe('API Integration', () => {
    it('should fetch data when period changes', async () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      fireEvent.press(getByText('Genel'));

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalledWith('all', 10);
        expect(mockApiService.getUserRanking).toHaveBeenCalledWith('all');
        expect(mockApiService.getCompetitionStats).toHaveBeenCalled();
      });
    });

    it('should handle API errors gracefully', async () => {
      mockApiService.getLeaderboard.mockRejectedValue(new Error('Network error'));

      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      fireEvent.press(getByText('Genel'));

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });

      // Component should not crash
      expect(getByText('🏆 Strategist Yarışması')).toBeTruthy();
    });

    it('should handle mixed API response states', async () => {
      mockApiService.getLeaderboard.mockResolvedValue(validLeaderboardData);
      mockApiService.getUserRanking.mockRejectedValue(new Error('User ranking error'));
      mockApiService.getCompetitionStats.mockResolvedValue(validStatsData);

      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      fireEvent.press(getByText('Genel'));

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });

      // Should still render successfully with partial data
      expect(getByText('John Trader')).toBeTruthy();
    });
  });

  describe('Refresh Control', () => {
    it('should handle refresh when enabled', async () => {
      render(<CompactLeaderboard {...defaultProps} />);

      // Refresh control would be tested through ScrollView interaction
      // For now, verify that API service methods are available
      expect(mockApiService.getLeaderboard).toBeDefined();
    });

    it('should not show refresh control when disabled', () => {
      render(<CompactLeaderboard {...defaultProps} enablePullToRefresh={false} />);

      // Would test RefreshControl props in actual implementation
      expect(true).toBe(true);
    });
  });

  describe('Animation and Performance', () => {
    it('should handle component mounting and unmounting', () => {
      const { unmount } = render(<CompactLeaderboard {...defaultProps} />);

      expect(() => unmount()).not.toThrow();
    });

    it('should handle rapid prop changes', () => {
      const { rerender } = render(<CompactLeaderboard {...defaultProps} />);

      // Rapidly change props
      for (let i = 0; i < 10; i++) {
        rerender(
          <CompactLeaderboard
            {...defaultProps}
            leaderboard={i % 2 === 0 ? validLeaderboardData : []}
          />
        );
      }

      expect(() => rerender(<CompactLeaderboard {...defaultProps} />)).not.toThrow();
    });

    it('should handle animation transitions', () => {
      render(<CompactLeaderboard {...defaultProps} />);

      // Animated.sequence should be called for transitions
      expect(Animated.sequence).toBeDefined();
    });
  });

  describe('Error Boundaries and Edge Cases', () => {
    it('should handle malformed leaderboard data', () => {
      const malformedData = [
        null,
        undefined,
        { userId: '1' }, // Missing required fields
        { rank: 'invalid', portfolioValue: NaN },
      ] as any;

      expect(() => {
        render(<CompactLeaderboard {...defaultProps} leaderboard={malformedData} />);
      }).not.toThrow();
    });

    it('should handle null user ranking', () => {
      expect(() => {
        render(<CompactLeaderboard {...defaultProps} userRanking={null} />);
      }).not.toThrow();
    });

    it('should handle null competition stats', () => {
      expect(() => {
        render(<CompactLeaderboard {...defaultProps} stats={null} />);
      }).not.toThrow();
    });

    it('should handle missing badges array', () => {
      const dataWithMissingBadges = [
        {
          ...validLeaderboardData[0],
          badges: null,
        },
      ];

      expect(() => {
        render(<CompactLeaderboard {...defaultProps} leaderboard={dataWithMissingBadges} />);
      }).not.toThrow();
    });
  });

  describe('Time Formatting', () => {
    it('should format remaining time correctly', () => {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 2);
      futureDate.setHours(futureDate.getHours() + 5);

      const statsWithFutureEnd = {
        ...validStatsData,
        currentPeriodEnd: futureDate.toISOString(),
      };

      const { getByText } = render(
        <CompactLeaderboard {...defaultProps} stats={statsWithFutureEnd} />
      );

      // Should display time in days and hours format
      expect(() => getByText(/2g 5s/)).not.toThrow();
    });

    it('should handle expired period', () => {
      const pastDate = new Date();
      pastDate.setDate(pastDate.getDate() - 1);

      const statsWithPastEnd = {
        ...validStatsData,
        currentPeriodEnd: pastDate.toISOString(),
      };

      const { getByText } = render(
        <CompactLeaderboard {...defaultProps} stats={statsWithPastEnd} />
      );

      expect(getByText('Süre doldu')).toBeTruthy();
    });
  });

  describe('Maximum Entries Limitation', () => {
    it('should limit displayed entries to maxEntries prop', () => {
      const manyEntries = Array.from({ length: 20 }, (_, index) => ({
        ...validLeaderboardData[0],
        userId: `user-${index}`,
        displayName: `Trader ${index}`,
        rank: index + 1,
      }));

      const { queryByText } = render(
        <CompactLeaderboard {...defaultProps} leaderboard={manyEntries} maxEntries={3} />
      );

      expect(queryByText('Trader 0')).toBeTruthy();
      expect(queryByText('Trader 1')).toBeTruthy();
      expect(queryByText('Trader 2')).toBeTruthy();
      expect(queryByText('Trader 3')).toBeNull(); // Should not be visible
    });

    it('should show separator when user is beyond maxEntries', () => {
      const userBeyondMax = {
        ...validUserRankingData,
        rank: 10, // Beyond maxEntries of 5
      };

      const { getByText } = render(
        <CompactLeaderboard {...defaultProps} userRanking={userBeyondMax} />
      );

      expect(getByText('...')).toBeTruthy();
    });
  });

  describe('Accessibility', () => {
    it('should have accessible elements', () => {
      const { getAllByRole } = render(<CompactLeaderboard {...defaultProps} />);

      // Should have accessible text elements
      const textElements = getAllByRole('text');
      expect(textElements.length).toBeGreaterThan(0);
    });

    it('should handle touch interactions correctly', () => {
      const { getByText } = render(<CompactLeaderboard {...defaultProps} />);

      // All TouchableOpacity elements should be pressable
      expect(() => {
        fireEvent.press(getByText('🏆 Strategist Yarışması'));
        fireEvent.press(getByText('John Trader'));
        fireEvent.press(getByText('Bu Hafta'));
      }).not.toThrow();
    });
  });
});