import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import UserProfile from '../../dashboard/UserProfile';
import {
  renderWithProviders,
  createAuthenticatedState,
  createGuestState,
  mockUser,
} from '../../../test-utils';
import { marketDataService } from '../../../services/marketDataService';

// Mock the market data service
vi.mock('../../../services/marketDataService', () => ({
  marketDataService: {
    getDataSourceHealth: vi.fn(),
  },
}));

const mockMarketDataService = marketDataService as any;

describe('UserProfile Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Mock successful connection status by default
    mockMarketDataService.getDataSourceHealth.mockResolvedValue({
      overall: 'connected',
      binance: { status: 'connected' },
      yahoo: { status: 'connected' },
    });
    
    // Mock Date for consistent market session testing
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2024-01-01T14:00:00Z')); // Monday 2 PM
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('Guest Mode', () => {
    it('renders guest profile with correct elements', async () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Guest Trader')).toBeInTheDocument();
      expect(screen.getByText('Limited Access Mode')).toBeInTheDocument();
      expect(screen.getByText('Sign In to Trade')).toBeInTheDocument();
      expect(screen.getByText('Live Data')).toBeInTheDocument();
      expect(screen.getByText('Portfolio')).toBeInTheDocument();
      expect(screen.getByText('Trading')).toBeInTheDocument();
    });

    it('shows locked metrics for guest users', () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      const lockedMetrics = screen.getAllByText('--');
      expect(lockedMetrics).toHaveLength(2); // Portfolio and Positions
    });

    it('displays market session status correctly', () => {
      // Test market open hours (Monday 2 PM)
      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Market Open')).toBeInTheDocument();
    });

    it('opens auth prompt when sign in button is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      const signInButton = screen.getByText('Sign In to Trade');
      await user.click(signInButton);

      expect(screen.getByText('Access Professional Trading Platform')).toBeInTheDocument();
      expect(screen.getByText('Sign in to unlock portfolio tracking, advanced analytics, and live trading capabilities.')).toBeInTheDocument();
    });

    it('closes auth prompt when close is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      // Open auth prompt
      const signInButton = screen.getByText('Sign In to Trade');
      await user.click(signInButton);

      // Close auth prompt
      const continueAsGuestButton = screen.getByText('Continue as Guest');
      await user.click(continueAsGuestButton);

      expect(screen.queryByText('Access Professional Trading Platform')).not.toBeInTheDocument();
    });

    it('shows correct feature access indicators', () => {
      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      const availableItems = container.querySelectorAll('.access-item.available');
      const lockedItems = container.querySelectorAll('.access-item.locked');

      expect(availableItems).toHaveLength(1); // Live Data
      expect(lockedItems).toHaveLength(2); // Portfolio and Trading
    });
  });

  describe('Loading State', () => {
    it('renders loading state when user is null but authenticated', () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: {
          auth: {
            user: null,
            token: 'mock-token',
            isAuthenticated: true,
            isGuest: false,
            isLoading: false,
            error: null,
          },
        },
      });

      expect(screen.getByText('Loading profile...')).toBeInTheDocument();
      expect(screen.getByText('Market Open')).toBeInTheDocument(); // Market status still shown
    });

    it('shows loading spinner in avatar', () => {
      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: {
          auth: {
            user: null,
            token: 'mock-token',
            isAuthenticated: true,
            isGuest: false,
            isLoading: false,
            error: null,
          },
        },
      });

      const loadingSpinner = container.querySelector('.loading-spinner-small');
      expect(loadingSpinner).toBeInTheDocument();
    });
  });

  describe('Authenticated User Mode', () => {
    it('renders authenticated user profile correctly', () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText(`${mockUser.firstName} ${mockUser.lastName}`)).toBeInTheDocument();
      expect(screen.getByText(mockUser.email)).toBeInTheDocument();
      expect(screen.getByText('BASIC')).toBeInTheDocument(); // Plan badge
      expect(screen.getByText('✓ Verified')).toBeInTheDocument(); // Email verified
    });

    it('displays user avatar with initials', () => {
      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      const avatarImage = container.querySelector('.avatar-image');
      expect(avatarImage).toHaveTextContent(`${mockUser.firstName[0]}${mockUser.lastName[0]}`);
    });

    it('shows portfolio value and positions', () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText('$0.00')).toBeInTheDocument(); // Portfolio value
      expect(screen.getByText('0')).toBeInTheDocument(); // Active positions
    });

    it('displays account status as active', () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText('Active')).toBeInTheDocument();
    });

    it('displays member since date correctly', () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText('Jan 2024')).toBeInTheDocument();
    });

    it('shows online status indicator', () => {
      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      const statusIndicator = container.querySelector('.status-indicator.online');
      expect(statusIndicator).toBeInTheDocument();
    });

    it('handles inactive user correctly', () => {
      const inactiveUserState = createAuthenticatedState();
      inactiveUserState.auth.user!.isActive = false;

      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: inactiveUserState,
      });

      expect(screen.getByText('Inactive')).toBeInTheDocument();
      const statusIndicator = container.querySelector('.status-indicator.offline');
      expect(statusIndicator).toBeInTheDocument();
    });

    it('handles unverified email correctly', () => {
      const unverifiedUserState = createAuthenticatedState();
      unverifiedUserState.auth.user!.isEmailVerified = false;

      renderWithProviders(<UserProfile />, {
        preloadedState: unverifiedUserState,
      });

      expect(screen.getByText('⚠ Unverified')).toBeInTheDocument();
    });
  });

  describe('Market Session Status', () => {
    it('shows market closed during weekend', () => {
      vi.setSystemTime(new Date('2024-01-06T14:00:00Z')); // Saturday 2 PM

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Market Closed')).toBeInTheDocument();
    });

    it('shows extended hours before market open', () => {
      vi.setSystemTime(new Date('2024-01-01T05:00:00Z')); // Monday 5 AM

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Extended Hours')).toBeInTheDocument();
    });

    it('shows extended hours after market close', () => {
      vi.setSystemTime(new Date('2024-01-01T18:00:00Z')); // Monday 6 PM

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Extended Hours')).toBeInTheDocument();
    });

    it('shows market closed at night', () => {
      vi.setSystemTime(new Date('2024-01-01T22:00:00Z')); // Monday 10 PM

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Market Closed')).toBeInTheDocument();
    });
  });

  describe('Connection Status', () => {
    it('displays live data when connected', async () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Live Data')).toBeInTheDocument();
      });
    });

    it('displays partial connection status', async () => {
      mockMarketDataService.getDataSourceHealth.mockResolvedValue({
        overall: 'partial',
        binance: { status: 'connected' },
        yahoo: { status: 'disconnected' },
      });

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Partial')).toBeInTheDocument();
      });
    });

    it('displays offline when disconnected', async () => {
      mockMarketDataService.getDataSourceHealth.mockResolvedValue({
        overall: 'disconnected',
        binance: { status: 'disconnected' },
        yahoo: { status: 'disconnected' },
      });

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Offline')).toBeInTheDocument();
      });
    });

    it('shows connecting status when service fails', async () => {
      mockMarketDataService.getDataSourceHealth.mockRejectedValue(
        new Error('Connection failed')
      );

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(screen.getByText('Connecting...')).toBeInTheDocument();
      });
    });

    it('updates connection status periodically', async () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      // Initial call
      await waitFor(() => {
        expect(mockMarketDataService.getDataSourceHealth).toHaveBeenCalledTimes(1);
      });

      // Fast-forward 30 seconds
      vi.advanceTimersByTime(30000);

      await waitFor(() => {
        expect(mockMarketDataService.getDataSourceHealth).toHaveBeenCalledTimes(2);
      });
    });
  });

  describe('Currency Formatting', () => {
    it('formats portfolio value correctly', () => {
      renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      // Mock portfolio value is 0, should format as $0.00
      expect(screen.getByText('$0.00')).toBeInTheDocument();
    });
  });

  describe('CSS Classes and Styling', () => {
    it('applies custom className prop', () => {
      const { container } = renderWithProviders(
        <UserProfile className="custom-class" />,
        { preloadedState: createGuestState() }
      );

      const profileElement = container.querySelector('.professional-user-profile');
      expect(profileElement).toHaveClass('custom-class');
    });

    it('applies guest mode classes correctly', () => {
      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      expect(container.querySelector('.guest-mode')).toBeInTheDocument();
    });

    it('applies authenticated mode classes correctly', () => {
      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(container.querySelector('.authenticated')).toBeInTheDocument();
    });

    it('applies loading mode classes correctly', () => {
      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: {
          auth: {
            user: null,
            token: 'mock-token',
            isAuthenticated: true,
            isGuest: false,
            isLoading: false,
            error: null,
          },
        },
      });

      expect(container.querySelector('.loading-mode')).toBeInTheDocument();
    });
  });

  describe('Error Handling and Edge Cases', () => {
    it('handles missing user properties gracefully', () => {
      const incompleteUserState = {
        auth: {
          user: {
            ...mockUser,
            firstName: '',
            lastName: '',
          },
          token: 'mock-token',
          isAuthenticated: true,
          isGuest: false,
          isLoading: false,
          error: null,
        },
      };

      expect(() => {
        renderWithProviders(<UserProfile />, {
          preloadedState: incompleteUserState,
        });
      }).not.toThrow();
    });

    it('handles connection status fetch errors', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockMarketDataService.getDataSourceHealth.mockRejectedValue(
        new Error('Network error')
      );

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      await waitFor(() => {
        expect(consoleSpy).toHaveBeenCalledWith(
          'Failed to fetch connection status:',
          expect.any(Error)
        );
      });

      consoleSpy.mockRestore();
    });

    it('cleans up interval on component unmount', () => {
      const clearIntervalSpy = vi.spyOn(global, 'clearInterval');
      
      const { unmount } = renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      unmount();

      expect(clearIntervalSpy).toHaveBeenCalled();
      clearIntervalSpy.mockRestore();
    });
  });

  describe('Accessibility', () => {
    it('has proper button accessibility', async () => {
      const user = userEvent.setup();

      renderWithProviders(<UserProfile />, {
        preloadedState: createGuestState(),
      });

      const signInButton = screen.getByText('Sign In to Trade');
      expect(signInButton).toBeInTheDocument();

      // Button should be keyboard accessible
      signInButton.focus();
      expect(signInButton).toHaveFocus();

      await user.keyboard('{Enter}');
      expect(screen.getByText('Access Professional Trading Platform')).toBeInTheDocument();
    });

    it('provides proper semantic structure', () => {
      const { container } = renderWithProviders(<UserProfile />, {
        preloadedState: createAuthenticatedState(),
      });

      // Check for proper heading structure
      const userNameHeading = screen.getByRole('heading', { level: 3 });
      expect(userNameHeading).toHaveTextContent(`${mockUser.firstName} ${mockUser.lastName}`);
    });
  });
});
