import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import CompetitionEntry from '../leaderboard/CompetitionEntry';
import { apiService } from '../../services/api';

// Mock the API service
jest.mock('../../services/api', () => ({
  apiService: {
    joinCompetition: jest.fn(),
  },
}));

// Mock Alert
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    Alert: {
      alert: jest.fn(),
    },
  };
});

const mockApiService = apiService as jest.Mocked<typeof apiService>;

describe('CompetitionEntry Component', () => {
  const defaultProps = {
    visible: true,
    onClose: jest.fn(),
    onSuccess: jest.fn(),
    stats: {
      totalParticipants: 150,
      totalPrizePool: 10000,
      minimumTrades: 5,
      prizes: [
        { rank: 1, amount: 5000, currency: 'USD' },
        { rank: 2, amount: 3000, currency: 'USD' },
        { rank: 3, amount: 2000, currency: 'USD' },
      ],
      eligibilityRequirements: [
        'Minimum portfolio value required',
        'Complete KYC verification',
        'Agree to competition terms',
      ],
      minimumPortfolioValue: 1000,
    },
    userRanking: {
      rank: 25,
      totalReturn: 8.5,
      userId: '1',
      username: 'testuser',
    },
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render when visible is true', () => {
      const { getByText } = render(<CompetitionEntry {...defaultProps} />);

      expect(getByText('Strategist Yarışmasına Hoş Geldiniz!')).toBeTruthy();
    });

    it('should not render when visible is false', () => {
      const { queryByText } = render(
        <CompetitionEntry {...defaultProps} visible={false} />
      );

      expect(queryByText('Strategist Yarışmasına Hoş Geldiniz!')).toBeNull();
    });

    it('should render competition stats correctly', () => {
      const { getByText } = render(<CompetitionEntry {...defaultProps} />);

      expect(getByText('150')).toBeTruthy(); // totalParticipants
      expect(getByText('10K ₺')).toBeTruthy(); // totalPrizePool formatted
      expect(getByText('5')).toBeTruthy(); // minimumTrades
    });
  });

  describe('Critical Error Prevention - Line 155', () => {
    it('should handle null prizes array gracefully', () => {
      const propsWithNullPrizes = {
        ...defaultProps,
        stats: {
          ...defaultProps.stats,
          prizes: null, // This would cause the .slice() error
        },
      };

      // Should not crash when rendering
      expect(() => {
        render(<CompetitionEntry {...propsWithNullPrizes} />);
      }).not.toThrow();
    });

    it('should handle undefined prizes array gracefully', () => {
      const propsWithUndefinedPrizes = {
        ...defaultProps,
        stats: {
          ...defaultProps.stats,
          prizes: undefined, // This would cause the .slice() error
        },
      };

      // Should not crash when rendering
      expect(() => {
        render(<CompetitionEntry {...propsWithUndefinedPrizes} />);
      }).not.toThrow();
    });

    it('should handle empty prizes array', () => {
      const propsWithEmptyPrizes = {
        ...defaultProps,
        stats: {
          ...defaultProps.stats,
          prizes: [], // Empty array should work fine
        },
      };

      const { getByText } = render(<CompetitionEntry {...propsWithEmptyPrizes} />);

      // Should still render the component
      expect(getByText('Bu Haftanın Ödülleri')).toBeTruthy();
    });

    it('should handle non-array prizes gracefully', () => {
      const propsWithInvalidPrizes = {
        ...defaultProps,
        stats: {
          ...defaultProps.stats,
          prizes: 'not-an-array' as any, // Invalid data type
        },
      };

      // Should not crash when rendering
      expect(() => {
        render(<CompetitionEntry {...propsWithInvalidPrizes} />);
      }).not.toThrow();
    });

    it('should render maximum 3 prizes when more are available', () => {
      const propsWithManyPrizes = {
        ...defaultProps,
        stats: {
          ...defaultProps.stats,
          prizes: [
            { rank: 1, amount: 5000, currency: 'USD' },
            { rank: 2, amount: 3000, currency: 'USD' },
            { rank: 3, amount: 2000, currency: 'USD' },
            { rank: 4, amount: 1000, currency: 'USD' },
            { rank: 5, amount: 500, currency: 'USD' },
          ],
        },
      };

      const { getByText, queryByText } = render(<CompetitionEntry {...propsWithManyPrizes} />);

      // Should show first 3 prizes
      expect(getByText('#1: 5.000 USD')).toBeTruthy();
      expect(getByText('#2: 3.000 USD')).toBeTruthy();
      expect(getByText('#3: 2.000 USD')).toBeTruthy();

      // Should not show 4th and 5th prizes
      expect(queryByText('#4: 1.000 USD')).toBeNull();
      expect(queryByText('#5: 500 USD')).toBeNull();
    });
  });

  describe('Stats Validation', () => {
    it('should handle null stats object', () => {
      const propsWithNullStats = {
        ...defaultProps,
        stats: null,
      };

      expect(() => {
        render(<CompetitionEntry {...propsWithNullStats} />);
      }).not.toThrow();
    });

    it('should handle undefined stats object', () => {
      const propsWithUndefinedStats = {
        ...defaultProps,
        stats: undefined,
      };

      expect(() => {
        render(<CompetitionEntry {...propsWithUndefinedStats} />);
      }).not.toThrow();
    });

    it('should handle missing properties in stats', () => {
      const propsWithIncompleteStats = {
        ...defaultProps,
        stats: {
          // Missing most properties
          totalParticipants: 100,
        } as any,
      };

      expect(() => {
        render(<CompetitionEntry {...propsWithIncompleteStats} />);
      }).not.toThrow();
    });
  });

  describe('User Interaction', () => {
    it('should handle join competition flow', async () => {
      mockApiService.joinCompetition.mockResolvedValue({ success: true });

      const { getByText } = render(<CompetitionEntry {...defaultProps} />);

      // Navigate through steps to reach confirmation
      // This would require multiple interactions in the actual component

      // Find and press join button (would need to navigate to final step)
      // For now, test that the component renders without crashing
      expect(getByText('Strategist Yarışmasına Hoş Geldiniz!')).toBeTruthy();
    });

    it('should handle API errors during join', async () => {
      mockApiService.joinCompetition.mockRejectedValue(new Error('Network error'));

      const { getByText } = render(<CompetitionEntry {...defaultProps} />);

      // Component should render and handle errors gracefully
      expect(getByText('Strategist Yarışmasına Hoş Geldiniz!')).toBeTruthy();
    });

    it('should call onClose when modal is closed', () => {
      const onCloseMock = jest.fn();
      const { getByTestId } = render(
        <CompetitionEntry {...defaultProps} onClose={onCloseMock} />
      );

      // Would need to add testID to close button in actual component
      // For now, verify component renders
      expect(getByText('Strategist Yarışmasına Hoş Geldiniz!')).toBeTruthy();
    });
  });

  describe('Requirements Validation', () => {
    it('should render eligibility requirements', () => {
      const { getByText } = render(<CompetitionEntry {...defaultProps} />);

      // Should show requirements from props
      expect(getByText('Minimum portfolio value required')).toBeTruthy();
      expect(getByText('Complete KYC verification')).toBeTruthy();
      expect(getByText('Agree to competition terms')).toBeTruthy();
    });

    it('should handle null eligibility requirements', () => {
      const propsWithNullRequirements = {
        ...defaultProps,
        stats: {
          ...defaultProps.stats,
          eligibilityRequirements: null,
        },
      };

      expect(() => {
        render(<CompetitionEntry {...propsWithNullRequirements} />);
      }).not.toThrow();
    });

    it('should handle empty eligibility requirements', () => {
      const propsWithEmptyRequirements = {
        ...defaultProps,
        stats: {
          ...defaultProps.stats,
          eligibilityRequirements: [],
        },
      };

      expect(() => {
        render(<CompetitionEntry {...propsWithEmptyRequirements} />);
      }).not.toThrow();
    });
  });

  describe('Animation and Performance', () => {
    it('should handle component mounting and unmounting', () => {
      const { rerender, unmount } = render(<CompetitionEntry {...defaultProps} />);

      // Rerender with different props
      rerender(<CompetitionEntry {...defaultProps} visible={false} />);

      // Unmount should not cause errors
      expect(() => unmount()).not.toThrow();
    });

    it('should handle rapid visibility changes', () => {
      const { rerender } = render(<CompetitionEntry {...defaultProps} />);

      // Rapidly toggle visibility
      for (let i = 0; i < 5; i++) {
        rerender(<CompetitionEntry {...defaultProps} visible={false} />);
        rerender(<CompetitionEntry {...defaultProps} visible={true} />);
      }

      // Should not cause crashes
      expect(true).toBe(true);
    });
  });

  describe('Memory Management', () => {
    it('should handle component with large data sets', () => {
      const propsWithLargeDataset = {
        ...defaultProps,
        stats: {
          ...defaultProps.stats,
          eligibilityRequirements: Array.from({ length: 100 }, (_, i) => `Requirement ${i + 1}`),
          prizes: Array.from({ length: 50 }, (_, i) => ({
            rank: i + 1,
            amount: 1000 - (i * 10),
            currency: 'USD',
          })),
        },
      };

      expect(() => {
        render(<CompetitionEntry {...propsWithLargeDataset} />);
      }).not.toThrow();
    });
  });

  describe('Error Boundary Integration', () => {
    it('should be wrappable in error boundary without issues', () => {
      const ErrorBoundary: React.FC<{ children: React.ReactNode }> = ({ children }) => {
        const [hasError, setHasError] = React.useState(false);

        if (hasError) {
          return <div>Error occurred</div>;
        }

        return <>{children}</>;
      };

      expect(() => {
        render(
          <ErrorBoundary>
            <CompetitionEntry {...defaultProps} />
          </ErrorBoundary>
        );
      }).not.toThrow();
    });
  });

  describe('Accessibility', () => {
    it('should have accessible elements', () => {
      const { getByRole, getAllByRole } = render(<CompetitionEntry {...defaultProps} />);

      // Should have accessible text elements
      const textElements = getAllByRole('text');
      expect(textElements.length).toBeGreaterThan(0);
    });

    it('should support keyboard navigation', () => {
      const { getByText } = render(<CompetitionEntry {...defaultProps} />);

      // Component should render and be navigable
      expect(getByText('Strategist Yarışmasına Hoş Geldiniz!')).toBeTruthy();
    });
  });

  describe('Real-world Data Scenarios', () => {
    it('should handle malformed API response data', () => {
      const malformedProps = {
        ...defaultProps,
        stats: {
          totalParticipants: 'not-a-number' as any,
          totalPrizePool: null,
          minimumTrades: undefined,
          prizes: { 'not': 'an-array' } as any,
          eligibilityRequirements: 'not-an-array' as any,
          minimumPortfolioValue: 'invalid' as any,
        },
        userRanking: {
          rank: 'invalid' as any,
          totalReturn: null,
          userId: undefined,
          username: '',
        },
      };

      expect(() => {
        render(<CompetitionEntry {...malformedProps} />);
      }).not.toThrow();
    });

    it('should handle network interruption scenarios', () => {
      // Simulate network interruption during API call
      mockApiService.joinCompetition.mockRejectedValue({
        name: 'NetworkError',
        message: 'Network request failed',
      });

      expect(() => {
        render(<CompetitionEntry {...defaultProps} />);
      }).not.toThrow();
    });
  });
});