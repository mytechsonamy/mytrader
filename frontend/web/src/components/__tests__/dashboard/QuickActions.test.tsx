import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import QuickActions from '../../dashboard/QuickActions';
import {
  renderWithProviders,
  createAuthenticatedState,
  createGuestState,
} from '../../../test-utils';
import { addNotification } from '../../../store/slices/uiSlice';
import { checkHealthAsync } from '../../../store/slices/authSlice';
import { fetchAllMarketData } from '../../../store/slices/marketSlice';

// Mock the async thunks
vi.mock('../../../store/slices/authSlice', async () => {
  const actual = await vi.importActual('../../../store/slices/authSlice');
  return {
    ...actual,
    checkHealthAsync: vi.fn(),
  };
});

vi.mock('../../../store/slices/marketSlice', async () => {
  const actual = await vi.importActual('../../../store/slices/marketSlice');
  return {
    ...actual,
    fetchAllMarketData: vi.fn(),
  };
});

const mockCheckHealthAsync = checkHealthAsync as any;
const mockFetchAllMarketData = fetchAllMarketData as any;

describe('QuickActions Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    // Mock successful thunk responses by default
    mockCheckHealthAsync.mockReturnValue({
      unwrap: vi.fn().mockResolvedValue({ status: 'healthy' })
    });
    
    mockFetchAllMarketData.mockReturnValue({
      unwrap: vi.fn().mockResolvedValue({ data: 'market data' })
    });
  });

  describe('Guest Mode Rendering', () => {
    it('renders quick actions for guest users', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText('Available Actions')).toBeInTheDocument();
      expect(screen.getByText('Refresh Market Data')).toBeInTheDocument();
      expect(screen.getByText('Health Check')).toBeInTheDocument();
      expect(screen.getByText('Start Trading')).toBeInTheDocument();
      expect(screen.getByText('View Analytics')).toBeInTheDocument();
      expect(screen.getByText('Portfolio Management')).toBeInTheDocument();
    });

    it('shows guest information message', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      expect(screen.getByText(/Sign in to unlock trading features/)).toBeInTheDocument();
    });

    it('displays lock indicators on restricted actions', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const lockIcons = screen.getAllByText('🔐');
      expect(lockIcons).toHaveLength(3); // Three auth-required actions
    });

    it('applies auth-required CSS class to restricted buttons', () => {
      const { container } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const authRequiredButtons = container.querySelectorAll('.action-btn.auth-required');
      expect(authRequiredButtons).toHaveLength(3);
    });
  });

  describe('Authenticated Mode Rendering', () => {
    it('renders quick actions for authenticated users', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText('Quick Actions')).toBeInTheDocument();
      expect(screen.getByText('Refresh Market Data')).toBeInTheDocument();
      expect(screen.getByText('Health Check')).toBeInTheDocument();
      expect(screen.getByText('View Portfolio')).toBeInTheDocument();
      expect(screen.getByText('Trading Strategies')).toBeInTheDocument();
    });

    it('does not show guest information for authenticated users', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.queryByText(/Sign in to unlock trading features/)).not.toBeInTheDocument();
    });

    it('does not show lock indicators for authenticated users', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.queryByText('🔐')).not.toBeInTheDocument();
    });

    it('shows secondary buttons instead of auth-required ones', () => {
      const { container } = renderWithProviders(<QuickActions />, {
        preloadedState: createAuthenticatedState(),
      });

      const secondaryButtons = container.querySelectorAll('.action-btn.secondary');
      expect(secondaryButtons).toHaveLength(2);
      
      const authRequiredButtons = container.querySelectorAll('.action-btn.auth-required');
      expect(authRequiredButtons).toHaveLength(0);
    });
  });

  describe('Market Data Refresh Action', () => {
    it('handles successful market data refresh', async () => {
      const user = userEvent.setup();
      
      const { store } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const refreshButton = screen.getByText('Refresh Market Data');
      await user.click(refreshButton);

      expect(mockFetchAllMarketData).toHaveBeenCalled();
      expect(store.dispatch).toHaveBeenCalledWith(
        addNotification({
          type: 'success',
          message: 'Market data refreshed successfully!'
        })
      );
    });

    it('handles failed market data refresh', async () => {
      const user = userEvent.setup();
      
      mockFetchAllMarketData.mockReturnValue({
        unwrap: vi.fn().mockRejectedValue(new Error('Network error'))
      });

      const { store } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const refreshButton = screen.getByText('Refresh Market Data');
      await user.click(refreshButton);

      await waitFor(() => {
        expect(store.dispatch).toHaveBeenCalledWith(
          addNotification({
            type: 'error',
            message: 'Failed to refresh market data'
          })
        );
      });
    });

    it('shows loading spinner during market data refresh', async () => {
      const user = userEvent.setup();
      
      // Make the promise never resolve to keep loading state
      mockFetchAllMarketData.mockReturnValue({
        unwrap: vi.fn().mockImplementation(() => new Promise(() => {}))
      });

      const { container } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const refreshButton = screen.getByText('Refresh Market Data');
      await user.click(refreshButton);

      expect(container.querySelector('.mini-spinner')).toBeInTheDocument();
      expect(refreshButton).toBeDisabled();
    });

    it('disables button when market data is already loading', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: {
          ...createGuestState(),
          market: {
            symbols: {},
            marketData: {},
            selectedSymbols: [],
            isLoading: true, // Market is loading
            error: null,
            lastUpdate: null,
          },
        },
      });

      const refreshButton = screen.getByText('Refresh Market Data');
      expect(refreshButton).toBeDisabled();
    });
  });

  describe('Health Check Action', () => {
    it('handles successful health check', async () => {
      const user = userEvent.setup();
      
      const { store } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const healthButton = screen.getByText('Health Check');
      await user.click(healthButton);

      expect(mockCheckHealthAsync).toHaveBeenCalled();
      expect(store.dispatch).toHaveBeenCalledWith(
        addNotification({
          type: 'success',
          message: 'API health check successful!'
        })
      );
    });

    it('handles failed health check', async () => {
      const user = userEvent.setup();
      
      mockCheckHealthAsync.mockReturnValue({
        unwrap: vi.fn().mockRejectedValue(new Error('API error'))
      });

      const { store } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const healthButton = screen.getByText('Health Check');
      await user.click(healthButton);

      await waitFor(() => {
        expect(store.dispatch).toHaveBeenCalledWith(
          addNotification({
            type: 'error',
            message: 'API health check failed'
          })
        );
      });
    });

    it('shows loading spinner during health check', async () => {
      const user = userEvent.setup();
      
      mockCheckHealthAsync.mockReturnValue({
        unwrap: vi.fn().mockImplementation(() => new Promise(() => {}))
      });

      const { container } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const healthButton = screen.getByText('Health Check');
      await user.click(healthButton);

      expect(container.querySelector('.mini-spinner')).toBeInTheDocument();
      expect(healthButton).toBeDisabled();
    });

    it('disables button when auth is loading', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: {
          ...createGuestState(),
          auth: {
            ...createGuestState().auth,
            isLoading: true, // Auth is loading
          },
        },
      });

      const healthButton = screen.getByText('Health Check');
      expect(healthButton).toBeDisabled();
    });
  });

  describe('Authentication Required Actions - Guest Mode', () => {
    it('opens auth prompt when Start Trading is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const startTradingButton = screen.getByText('Start Trading');
      await user.click(startTradingButton);

      expect(screen.getByText('Trading Features')).toBeInTheDocument();
      expect(screen.getByText('Sign in to access trading features, portfolio management, and advanced analytics.')).toBeInTheDocument();
    });

    it('opens auth prompt when View Analytics is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const analyticsButton = screen.getByText('View Analytics');
      await user.click(analyticsButton);

      expect(screen.getByText('Trading Features')).toBeInTheDocument();
    });

    it('opens auth prompt when Portfolio Management is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const portfolioButton = screen.getByText('Portfolio Management');
      await user.click(portfolioButton);

      expect(screen.getByText('Trading Features')).toBeInTheDocument();
    });

    it('closes auth prompt when close is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      // Open auth prompt
      const startTradingButton = screen.getByText('Start Trading');
      await user.click(startTradingButton);

      // Close auth prompt
      const continueAsGuestButton = screen.getByText('Continue as Guest');
      await user.click(continueAsGuestButton);

      expect(screen.queryByText('Trading Features')).not.toBeInTheDocument();
    });
  });

  describe('Authentication Required Actions - Authenticated Mode', () => {
    it('shows "coming soon" notification for View Portfolio', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(<QuickActions />, {
        preloadedState: createAuthenticatedState(),
      });

      const portfolioButton = screen.getByText('View Portfolio');
      await user.click(portfolioButton);

      expect(store.dispatch).toHaveBeenCalledWith(
        addNotification({
          type: 'info',
          message: 'Portfolio feature coming soon!'
        })
      );
    });

    it('shows "coming soon" notification for Trading Strategies', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(<QuickActions />, {
        preloadedState: createAuthenticatedState(),
      });

      const strategiesButton = screen.getByText('Trading Strategies');
      await user.click(strategiesButton);

      expect(store.dispatch).toHaveBeenCalledWith(
        addNotification({
          type: 'info',
          message: 'Trading strategies feature coming soon!'
        })
      );
    });

    it('does not show auth prompt for authenticated users', async () => {
      const user = userEvent.setup();

      renderWithProviders(<QuickActions />, {
        preloadedState: createAuthenticatedState(),
      });

      const portfolioButton = screen.getByText('View Portfolio');
      await user.click(portfolioButton);

      expect(screen.queryByText('Trading Features')).not.toBeInTheDocument();
    });
  });

  describe('CSS Classes and Styling', () => {
    it('applies custom className prop', () => {
      const { container } = renderWithProviders(
        <QuickActions className="custom-actions-class" />,
        { preloadedState: createGuestState() }
      );

      const quickActions = container.querySelector('.quick-actions');
      expect(quickActions).toHaveClass('custom-actions-class');
    });

    it('applies correct button classes', () => {
      const { container } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      expect(container.querySelector('.action-btn.primary')).toBeInTheDocument();
      expect(container.querySelector('.action-btn.utility')).toBeInTheDocument();
      expect(container.querySelectorAll('.action-btn.auth-required')).toHaveLength(3);
    });

    it('shows proper action grid structure', () => {
      const { container } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const actionsGrid = container.querySelector('.actions-grid');
      expect(actionsGrid).toBeInTheDocument();
      
      const actionButtons = actionsGrid!.querySelectorAll('.action-btn');
      expect(actionButtons).toHaveLength(5); // 2 always available + 3 auth-required
    });
  });

  describe('Loading State Management', () => {
    it('handles multiple simultaneous loading states', async () => {
      const user = userEvent.setup();
      
      // Make both promises never resolve
      mockFetchAllMarketData.mockReturnValue({
        unwrap: vi.fn().mockImplementation(() => new Promise(() => {}))
      });
      mockCheckHealthAsync.mockReturnValue({
        unwrap: vi.fn().mockImplementation(() => new Promise(() => {}))
      });

      const { container } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      // Start both actions
      const refreshButton = screen.getByText('Refresh Market Data');
      const healthButton = screen.getByText('Health Check');
      
      await user.click(refreshButton);
      await user.click(healthButton);

      // Both should show spinners and be disabled
      const spinners = container.querySelectorAll('.mini-spinner');
      expect(spinners).toHaveLength(2);
      
      expect(refreshButton).toBeDisabled();
      expect(healthButton).toBeDisabled();
    });

    it('clears loading state after completion', async () => {
      const user = userEvent.setup();
      
      const { container } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const refreshButton = screen.getByText('Refresh Market Data');
      await user.click(refreshButton);

      await waitFor(() => {
        expect(container.querySelector('.mini-spinner')).not.toBeInTheDocument();
        expect(refreshButton).not.toBeDisabled();
      });
    });
  });

  describe('Edge Cases and Error Handling', () => {
    it('handles rapid button clicking gracefully', async () => {
      const user = userEvent.setup();
      
      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const refreshButton = screen.getByText('Refresh Market Data');
      
      // Rapid clicks
      await user.click(refreshButton);
      await user.click(refreshButton);
      await user.click(refreshButton);

      // Should only call the action once (subsequent clicks on disabled button)
      expect(mockFetchAllMarketData).toHaveBeenCalledTimes(1);
    });

    it('handles authentication state changes during actions', async () => {
      const user = userEvent.setup();
      
      const { rerender } = renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      // Start as guest
      expect(screen.getByText('Available Actions')).toBeInTheDocument();
      expect(screen.getByText('Start Trading')).toBeInTheDocument();

      // Change to authenticated
      rerender(<QuickActions />);
      
      // Simulate state change by re-rendering with authenticated state
      const { rerender: rerenderAuth } = renderWithProviders(<QuickActions />, {
        preloadedState: createAuthenticatedState(),
      });

      expect(screen.getByText('Quick Actions')).toBeInTheDocument();
      expect(screen.queryByText('Start Trading')).not.toBeInTheDocument();
      expect(screen.getByText('View Portfolio')).toBeInTheDocument();
    });

    it('handles missing thunk responses gracefully', async () => {
      const user = userEvent.setup();
      
      mockFetchAllMarketData.mockReturnValue(undefined as any);

      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const refreshButton = screen.getByText('Refresh Market Data');
      
      // Should not crash when thunk returns undefined
      await expect(user.click(refreshButton)).resolves.not.toThrow();
    });
  });

  describe('Accessibility', () => {
    it('has proper button structure and semantics', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const buttons = screen.getAllByRole('button');
      expect(buttons.length).toBeGreaterThan(0);
      
      // Each button should have meaningful text
      buttons.forEach(button => {
        expect(button.textContent).toBeTruthy();
      });
    });

    it('supports keyboard navigation', async () => {
      const user = userEvent.setup();

      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const firstButton = screen.getByText('Refresh Market Data');
      
      // Focus and activate with keyboard
      firstButton.focus();
      expect(firstButton).toHaveFocus();
      
      await user.keyboard('{Enter}');
      expect(mockFetchAllMarketData).toHaveBeenCalled();
    });

    it('has proper heading structure', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      const heading = screen.getByRole('heading', { level: 3 });
      expect(heading).toHaveTextContent('Available Actions');
    });

    it('provides meaningful button text and icons', () => {
      renderWithProviders(<QuickActions />, {
        preloadedState: createGuestState(),
      });

      // Check that buttons have both icons and text
      expect(screen.getByText('📈')).toBeInTheDocument(); // Market data icon
      expect(screen.getByText('🔄')).toBeInTheDocument(); // Health check icon
      expect(screen.getByText('💼')).toBeInTheDocument(); // Trading icon
      expect(screen.getByText('📊')).toBeInTheDocument(); // Analytics icon
      expect(screen.getByText('🎯')).toBeInTheDocument(); // Portfolio icon
    });
  });
});
