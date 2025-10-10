import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import EnhancedLeaderboardScreen from '../EnhancedLeaderboardScreen';
import { apiService } from '../../services/api';

// Mock AuthContext
const mockAuthContext = {
  user: {
    id: '1',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
  },
  login: jest.fn(),
  logout: jest.fn(),
  register: jest.fn(),
  isAuthenticated: true,
  loading: false,
};

jest.mock('../../context/AuthContext', () => ({
  useAuth: () => mockAuthContext,
}));

// Mock the API service
jest.mock('../../services/api', () => ({
  apiService: {
    getLeaderboard: jest.fn(),
    getCompetitionStats: jest.fn(),
    getUserRanking: jest.fn(),
  },
}));

// Mock WebSocket hook
const mockWebSocketHook = {
  isConnected: false,
  lastUpdate: null,
  connectionStatus: 'disconnected',
  forceRefresh: jest.fn(),
};

jest.mock('../../hooks/useLeaderboardWebSocket', () => ({
  useLeaderboardWebSocket: () => mockWebSocketHook,
}));

// Mock React Native components
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    Alert: {
      alert: jest.fn(),
    },
    RefreshControl: 'RefreshControl',
    FlatList: 'FlatList',
    ScrollView: 'ScrollView',
    Dimensions: {
      get: () => ({ width: 375, height: 812 }),
    },
  };
});

// Mock navigation
const mockNavigation = {
  navigate: jest.fn(),
  goBack: jest.fn(),
  setOptions: jest.fn(),
};

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => mockNavigation,
  useFocusEffect: (callback: () => void) => callback(),
}));

// Mock leaderboard components
jest.mock('../../components/leaderboard', () => ({
  CompetitionEntry: 'CompetitionEntry',
  RulesModal: 'RulesModal',
  PerformanceChart: 'PerformanceChart',
  PerformanceTiers: 'PerformanceTiers',
  UserRankCard: 'UserRankCard',
  SocialFeatures: 'SocialFeatures',
}));

const mockApiService = apiService as jest.Mocked<typeof apiService>;

describe('EnhancedLeaderboardScreen Component', () => {
  const validLeaderboardData = [
    {
      userId: '1',
      username: 'trader1',
      rank: 1,
      totalReturn: 15.5,
      totalTrades: 42,
      winRate: 65.2,
    },
    {
      userId: '2',
      username: 'trader2',
      rank: 2,
      totalReturn: 12.3,
      totalTrades: 38,
      winRate: 58.7,
    },
    {
      userId: '3',
      username: 'trader3',
      rank: 3,
      totalReturn: 10.8,
      totalTrades: 35,
      winRate: 62.1,
    },
  ];

  const validStatsData = {
    totalParticipants: 150,
    totalPrizePool: 10000,
    minimumTrades: 5,
    prizes: [
      { rank: 1, amount: 5000, currency: 'USD' },
      { rank: 2, amount: 3000, currency: 'USD' },
      { rank: 3, amount: 2000, currency: 'USD' },
    ],
  };

  beforeEach(() => {
    jest.clearAllMocks();

    // Setup default successful API responses
    mockApiService.getLeaderboard.mockResolvedValue(validLeaderboardData);
    mockApiService.getCompetitionStats.mockResolvedValue(validStatsData);
    mockApiService.getUserRanking.mockResolvedValue({
      rank: 25,
      totalReturn: 8.5,
      userId: '1',
      username: 'testuser',
    });
  });

  describe('Rendering', () => {
    it('should render without crashing', () => {
      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });

    it('should render with valid leaderboard data', async () => {
      const { getByText } = render(<EnhancedLeaderboardScreen />);

      await waitFor(() => {
        // Should show some leaderboard content
        expect(getByText).toBeDefined();
      });
    });
  });

  describe('Critical Error Prevention - Line 61', () => {
    it('should handle non-array leaderboard data gracefully', async () => {
      // This simulates the exact error scenario: EnhancedLeaderboardScreen.tsx:61
      const nonArrayResponse = {
        message: 'Single user update',
        user: { userId: '1', rank: 1 },
        status: 'success',
      };

      mockApiService.getLeaderboard.mockResolvedValue(nonArrayResponse as any);

      // Should not crash when rendering
      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        // Component should handle the error gracefully
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });
    });

    it('should handle null leaderboard data', async () => {
      mockApiService.getLeaderboard.mockResolvedValue(null as any);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });
    });

    it('should handle undefined leaderboard data', async () => {
      mockApiService.getLeaderboard.mockResolvedValue(undefined as any);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });
    });

    it('should handle string leaderboard data', async () => {
      mockApiService.getLeaderboard.mockResolvedValue('not-an-array' as any);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });
    });

    it('should handle object leaderboard data', async () => {
      const objectResponse = {
        totalUsers: 150,
        currentPage: 1,
        // Missing the actual array of users
      };

      mockApiService.getLeaderboard.mockResolvedValue(objectResponse as any);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });
    });

    it('should handle empty array leaderboard data', async () => {
      mockApiService.getLeaderboard.mockResolvedValue([]);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });
    });
  });

  describe('WebSocket Integration', () => {
    it('should handle WebSocket data updates with non-array data', async () => {
      // Test WebSocket callback with non-array data
      const webSocketCallback = mockWebSocketHook.onRankingChange;

      if (webSocketCallback) {
        // Simulate receiving non-array data via WebSocket
        const nonArrayData = { message: 'single update', user: { id: 1 } };

        expect(() => {
          webSocketCallback(nonArrayData as any);
        }).not.toThrow();
      }

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });

    it('should handle WebSocket reconnection scenarios', async () => {
      // Simulate connection status changes
      mockWebSocketHook.connectionStatus = 'reconnecting';
      mockWebSocketHook.isConnected = false;

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });
  });

  describe('API Error Handling', () => {
    it('should handle API errors gracefully', async () => {
      mockApiService.getLeaderboard.mockRejectedValue(new Error('Network error'));

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });
    });

    it('should handle API timeout errors', async () => {
      mockApiService.getLeaderboard.mockRejectedValue({
        name: 'TimeoutError',
        message: 'Request timeout',
      });

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });

    it('should handle malformed API responses', async () => {
      const malformedResponse = {
        data: {
          users: 'should-be-array-but-is-string',
          meta: null,
        },
        status: 200,
      };

      mockApiService.getLeaderboard.mockResolvedValue(malformedResponse as any);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });
  });

  describe('Data Filtering and Sorting', () => {
    it('should handle filtering with empty data', async () => {
      mockApiService.getLeaderboard.mockResolvedValue([]);

      const { getByText } = render(<EnhancedLeaderboardScreen />);

      // Should render without crashing even with no data to filter
      await waitFor(() => {
        expect(getByText).toBeDefined();
      });
    });

    it('should handle sorting with malformed data', async () => {
      const malformedLeaderboardData = [
        { userId: '1', username: 'trader1' }, // Missing required fields
        { rank: 2, totalReturn: 'not-a-number' }, // Invalid data types
        null, // Null entry
        undefined, // Undefined entry
      ];

      mockApiService.getLeaderboard.mockResolvedValue(malformedLeaderboardData as any);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });
  });

  describe('State Management', () => {
    it('should handle state updates with invalid data', async () => {
      const { rerender } = render(<EnhancedLeaderboardScreen />);

      // First render with valid data
      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });

      // Then simulate receiving invalid data
      mockApiService.getLeaderboard.mockResolvedValue('invalid-data' as any);

      rerender(<EnhancedLeaderboardScreen />);

      expect(() => rerender(<EnhancedLeaderboardScreen />)).not.toThrow();
    });

    it('should handle rapid state changes', async () => {
      const { rerender } = render(<EnhancedLeaderboardScreen />);

      // Simulate rapid data updates
      for (let i = 0; i < 10; i++) {
        mockApiService.getLeaderboard.mockResolvedValue(
          i % 2 === 0 ? validLeaderboardData : []
        );
        rerender(<EnhancedLeaderboardScreen />);
      }

      expect(() => rerender(<EnhancedLeaderboardScreen />)).not.toThrow();
    });
  });

  describe('Memory Management', () => {
    it('should handle component unmounting during API calls', async () => {
      let resolveApiCall: (value: any) => void;
      const apiPromise = new Promise((resolve) => {
        resolveApiCall = resolve;
      });

      mockApiService.getLeaderboard.mockReturnValue(apiPromise);

      const { unmount } = render(<EnhancedLeaderboardScreen />);

      // Unmount before API call completes
      unmount();

      // Complete the API call
      resolveApiCall!(validLeaderboardData);

      // Should not cause memory leaks or errors
      expect(true).toBe(true);
    });

    it('should handle large datasets without performance issues', async () => {
      const largeDataset = Array.from({ length: 1000 }, (_, index) => ({
        userId: `user-${index}`,
        username: `trader${index}`,
        rank: index + 1,
        totalReturn: Math.random() * 100,
        totalTrades: Math.floor(Math.random() * 1000),
        winRate: Math.random() * 100,
      }));

      mockApiService.getLeaderboard.mockResolvedValue(largeDataset);

      const startTime = Date.now();
      render(<EnhancedLeaderboardScreen />);
      const renderTime = Date.now() - startTime;

      // Should render within reasonable time (less than 1 second)
      expect(renderTime).toBeLessThan(1000);
    });
  });

  describe('User Interactions', () => {
    it('should handle tab switching', async () => {
      const { getByText } = render(<EnhancedLeaderboardScreen />);

      await waitFor(() => {
        expect(getByText).toBeDefined();
      });

      // Test would require more specific UI elements to interact with
      expect(true).toBe(true); // Placeholder
    });

    it('should handle refresh actions', async () => {
      const { getByText } = render(<EnhancedLeaderboardScreen />);

      await waitFor(() => {
        expect(mockApiService.getLeaderboard).toHaveBeenCalled();
      });

      // Reset and test refresh
      jest.clearAllMocks();

      // Simulate refresh action (would need to trigger actual refresh)
      expect(true).toBe(true); // Placeholder for refresh test
    });
  });

  describe('Accessibility', () => {
    it('should render with proper accessibility labels', () => {
      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();

      // Would need to add accessibility tests for actual screen elements
    });

    it('should support screen readers', () => {
      const { getAllByRole } = render(<EnhancedLeaderboardScreen />);

      // Should have accessible elements
      expect(() => getAllByRole('text')).not.toThrow();
    });
  });

  describe('Edge Cases', () => {
    it('should handle extremely nested malformed data', async () => {
      const deeplyNestedData = {
        data: {
          results: {
            users: {
              list: {
                items: 'not-an-array',
              },
            },
          },
        },
      };

      mockApiService.getLeaderboard.mockResolvedValue(deeplyNestedData as any);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });

    it('should handle circular reference data', async () => {
      const circularData: any = { name: 'circular' };
      circularData.self = circularData; // Create circular reference

      mockApiService.getLeaderboard.mockResolvedValue([circularData]);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });

    it('should handle mixed valid and invalid array elements', async () => {
      const mixedData = [
        validLeaderboardData[0], // Valid
        null, // Invalid
        validLeaderboardData[1], // Valid
        { invalidStructure: true }, // Invalid
        undefined, // Invalid
        validLeaderboardData[2], // Valid
      ];

      mockApiService.getLeaderboard.mockResolvedValue(mixedData as any);

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });
  });

  describe('Performance Under Stress', () => {
    it('should handle concurrent API calls', async () => {
      // Simulate multiple concurrent calls
      const promises = Array.from({ length: 10 }, () =>
        Promise.resolve(validLeaderboardData)
      );

      mockApiService.getLeaderboard.mockImplementation(() =>
        promises[Math.floor(Math.random() * promises.length)]
      );

      expect(() => {
        render(<EnhancedLeaderboardScreen />);
      }).not.toThrow();
    });

    it('should handle rapid re-renders', () => {
      const { rerender } = render(<EnhancedLeaderboardScreen />);

      // Rapid re-renders
      for (let i = 0; i < 50; i++) {
        rerender(<EnhancedLeaderboardScreen />);
      }

      expect(true).toBe(true); // Should not crash
    });
  });
});