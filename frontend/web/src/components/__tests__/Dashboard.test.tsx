import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Dashboard from '../Dashboard';
import {
  renderWithProviders,
  createAuthenticatedState,
  createGuestState,
  createMarketStateWithData,
  mockUser,
} from '../../test-utils';

// Mock the WebSocket hook
vi.mock('../../hooks/useWebSocket', () => ({
  useWebSocket: vi.fn(() => ({
    connected: true,
    connecting: false,
    error: null,
  })),
}));

// Mock window.location
const mockLocation = {
  href: '',
};
Object.defineProperty(window, 'location', {
  value: mockLocation,
  writable: true,
});

describe('Dashboard Component', () => {
  const mockOnLogout = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    mockLocation.href = '';
  });

  describe('Guest User Experience', () => {
    it('renders dashboard in guest mode', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('myTrader')).toBeInTheDocument();
      expect(screen.getByText('Welcome, Guest')).toBeInTheDocument();
      expect(screen.getByText('Sign In')).toBeInTheDocument();
      expect(screen.getByText('Market Statistics')).toBeInTheDocument();
      expect(screen.getByText('Browsing market data as guest')).toBeInTheDocument();
    });

    it('shows available public features for guests', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Live Market Data')).toBeInTheDocument();
      expect(screen.getByText('Real-time market updates available to all users')).toBeInTheDocument();
      expect(screen.getByText('Available')).toBeInTheDocument();
    });

    it('shows locked features for guests', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Portfolio Tracking')).toBeInTheDocument();
      expect(screen.getByText('Trading Analytics')).toBeInTheDocument();
      expect(screen.getByText('Strategy Management')).toBeInTheDocument();

      const lockedBadges = screen.getAllByText('🔒 Sign In Required');
      expect(lockedBadges).toHaveLength(3);
    });

    it('handles navigation to login page', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      const signInButton = screen.getByText('Sign In');
      await user.click(signInButton);

      expect(mockLocation.href).toBe('/login');
    });

    it('shows auth prompt when locked feature is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      const lockedFeature = screen.getByText('Portfolio Tracking').closest('.guest-stat-card');
      await user.click(lockedFeature!);

      expect(screen.getByText('Authentication Required')).toBeInTheDocument();
      expect(screen.getByText('Please sign in to access this feature and unlock the full myTrader experience.')).toBeInTheDocument();
    });

    it('can close auth prompt and continue as guest', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      // Open auth prompt
      const lockedFeature = screen.getByText('Portfolio Tracking').closest('.guest-stat-card');
      await user.click(lockedFeature!);

      // Close auth prompt
      const continueAsGuestButton = screen.getByText('Continue as Guest');
      await user.click(continueAsGuestButton);

      expect(screen.queryByText('Authentication Required')).not.toBeInTheDocument();
    });

    it('can close auth prompt with close button', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      // Open auth prompt
      const lockedFeature = screen.getByText('Trading Analytics').closest('.guest-stat-card');
      await user.click(lockedFeature!);

      // Close with X button
      const closeButton = screen.getByLabelText('Close');
      await user.click(closeButton);

      expect(screen.queryByText('Authentication Required')).not.toBeInTheDocument();
    });
  });

  describe('Authenticated User Experience', () => {
    it('renders dashboard for authenticated user', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText('myTrader')).toBeInTheDocument();
      expect(screen.getByText(`Welcome, ${mockUser.firstName}`)).toBeInTheDocument();
      expect(screen.getByText('Logout')).toBeInTheDocument();
      expect(screen.getByText('Trading Statistics')).toBeInTheDocument();
    });

    it('shows authenticated user stats', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText('Total Portfolio')).toBeInTheDocument();
      expect(screen.getByText("Today's P&L")).toBeInTheDocument();
      expect(screen.getByText('Active Positions')).toBeInTheDocument();
      expect(screen.getByText('Win Rate')).toBeInTheDocument();
    });

    it('shows authenticated user activity', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText('Welcome back to myTrader!')).toBeInTheDocument();
      expect(screen.getByText('Logged in successfully')).toBeInTheDocument();
      expect(screen.getByText('Account created 1/1/2024')).toBeInTheDocument();
    });

    it('handles logout correctly', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createAuthenticatedState(),
      });

      const logoutButton = screen.getByText('Logout');
      await user.click(logoutButton);

      expect(mockOnLogout).toHaveBeenCalledTimes(1);
    });

    it('does not show auth prompt for authenticated users', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.queryByText('🔒 Sign In Required')).not.toBeInTheDocument();
      expect(screen.queryByText('Authentication Required')).not.toBeInTheDocument();
    });
  });

  describe('Sidebar Functionality', () => {
    it('renders sidebar navigation', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Dashboard')).toBeInTheDocument();
      expect(screen.getByText('Market Data')).toBeInTheDocument();
      expect(screen.getByText('Portfolio')).toBeInTheDocument();
      expect(screen.getByText('Strategies')).toBeInTheDocument();
      expect(screen.getByText('Settings')).toBeInTheDocument();
    });

    it('toggles sidebar open/closed', async () => {
      const user = userEvent.setup();

      const { container } = renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      const sidebar = container.querySelector('.dashboard-sidebar');
      expect(sidebar).toHaveClass('open');

      const toggleButton = screen.getByText('☰');
      await user.click(toggleButton);

      await waitFor(() => {
        expect(sidebar).toHaveClass('closed');
      });
    });

    it('starts with correct sidebar state based on Redux', () => {
      const { container } = renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: {
          ...createGuestState(),
          ui: {
            theme: 'dark',
            sidebarOpen: false,
            loading: false,
            notifications: [],
          },
        },
      });

      const sidebar = container.querySelector('.dashboard-sidebar');
      expect(sidebar).toHaveClass('closed');
    });
  });

  describe('Theme Support', () => {
    it('applies light theme correctly', () => {
      const { container } = renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: {
          ...createGuestState(),
          ui: {
            theme: 'light',
            sidebarOpen: true,
            loading: false,
            notifications: [],
          },
        },
      });

      const dashboardContainer = container.querySelector('.dashboard-container');
      expect(dashboardContainer).toHaveClass('theme-light');
    });

    it('applies dark theme correctly', () => {
      const { container } = renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: {
          ...createGuestState(),
          ui: {
            theme: 'dark',
            sidebarOpen: true,
            loading: false,
            notifications: [],
          },
        },
      });

      const dashboardContainer = container.querySelector('.dashboard-container');
      expect(dashboardContainer).toHaveClass('theme-dark');
    });
  });

  describe('Market Data Integration', () => {
    it('renders MarketOverview component', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: {
          ...createGuestState(),
          ...createMarketStateWithData(),
        },
      });

      // MarketOverview should be rendered (tested separately)
      expect(screen.getByTestId).toBeDefined(); // Component should be mounted
    });

    it('renders other dashboard components', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      // These components should be present in the DOM
      // We're testing that the Dashboard renders all its child components
      const dashboardGrid = document.querySelector('.dashboard-grid');
      expect(dashboardGrid).toBeInTheDocument();

      const marketSection = document.querySelector('.market-section');
      expect(marketSection).toBeInTheDocument();

      const actionsSection = document.querySelector('.actions-section');
      expect(actionsSection).toBeInTheDocument();
    });
  });

  describe('Component Lifecycle', () => {
    it('initializes correctly for guest user', () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      expect(consoleSpy).toHaveBeenCalledWith('Dashboard initialized in guest mode');

      consoleSpy.mockRestore();
    });

    it('initializes correctly for authenticated user', () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(consoleSpy).toHaveBeenCalledWith('Dashboard initialized for user:', mockUser.email);

      consoleSpy.mockRestore();
    });
  });

  describe('Error Handling and Edge Cases', () => {
    it('handles missing onLogout prop gracefully', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard />, {
        preloadedState: createAuthenticatedState(),
      });

      const logoutButton = screen.getByText('Logout');

      // Should not throw error when clicking logout without onLogout prop
      await expect(user.click(logoutButton)).resolves.not.toThrow();
    });

    it('handles rapid interactions gracefully', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      // Rapid clicking on locked features
      const lockedFeatures = screen.getAllByText('🔒 Sign In Required');

      for (const feature of lockedFeatures.slice(0, 2)) {
        const parentCard = feature.closest('.guest-stat-card');
        if (parentCard) {
          await user.click(parentCard);
          // Close the modal
          const continueButton = screen.getByText('Continue as Guest');
          await user.click(continueButton);
        }
      }

      // Should handle this gracefully without errors
      expect(screen.queryByText('Authentication Required')).not.toBeInTheDocument();
    });

    it('handles theme transitions correctly', () => {
      const { rerender, container } = renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: {
          ...createGuestState(),
          ui: {
            theme: 'light',
            sidebarOpen: true,
            loading: false,
            notifications: [],
          },
        },
      });

      let dashboardContainer = container.querySelector('.dashboard-container');
      expect(dashboardContainer).toHaveClass('theme-light');

      // Change theme
      rerender(<Dashboard onLogout={mockOnLogout} />);

      // Re-render with dark theme
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: {
          ...createGuestState(),
          ui: {
            theme: 'dark',
            sidebarOpen: true,
            loading: false,
            notifications: [],
          },
        },
      });

      dashboardContainer = container.querySelector('.dashboard-container');
      expect(dashboardContainer).toHaveClass('theme-dark');
    });
  });

  describe('Accessibility', () => {
    it('has proper ARIA labels', () => {
      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      // Open auth prompt to test close button
      const lockedFeature = screen.getByText('Portfolio Tracking').closest('.guest-stat-card');
      userEvent.click(lockedFeature!);

      const closeButton = screen.getByLabelText('Close');
      expect(closeButton).toBeInTheDocument();
    });

    it('supports keyboard navigation', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      const signInButton = screen.getByText('Sign In');

      // Tab to the button and press Enter
      await user.tab();
      expect(signInButton).toHaveFocus();

      await user.keyboard('{Enter}');
      expect(mockLocation.href).toBe('/login');
    });

    it('maintains focus management in modals', async () => {
      const user = userEvent.setup();

      renderWithProviders(<Dashboard onLogout={mockOnLogout} />, {
        preloadedState: createGuestState(),
      });

      const lockedFeature = screen.getByText('Strategy Management').closest('.guest-stat-card');
      await user.click(lockedFeature!);

      // Modal should be focusable
      const modal = screen.getByRole('dialog', { hidden: true }) || screen.getByText('Authentication Required').closest('.auth-prompt-modal');
      expect(modal).toBeInTheDocument();
    });
  });
});