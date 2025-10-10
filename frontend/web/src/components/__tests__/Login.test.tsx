import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Login from '../Login';
import { renderWithProviders, createGuestState } from '../../test-utils';
import { loginAsync } from '../../store/slices/authSlice';

// Mock the async thunk
vi.mock('../../store/slices/authSlice', async () => {
  const actual = await vi.importActual('../../store/slices/authSlice');
  return {
    ...actual,
    loginAsync: vi.fn(),
  };
});

const mockLoginAsync = loginAsync as any;

describe('Login Component', () => {
  const mockOnSwitchToRegister = vi.fn();
  const mockOnSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    mockLoginAsync.mockReturnValue({ type: 'auth/login/pending' });
  });

  describe('Rendering and UI Elements', () => {
    it('renders login form correctly', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByText('Login to myTrader')).toBeInTheDocument();
      expect(screen.getByLabelText('Email')).toBeInTheDocument();
      expect(screen.getByLabelText('Password')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Login' })).toBeInTheDocument();
      expect(screen.getByText('Use Test User')).toBeInTheDocument();
      expect(screen.getByText("Don't have an account?")).toBeInTheDocument();
      expect(screen.getByText('Register here')).toBeInTheDocument();
    });

    it('renders with correct CSS classes for modal mode', () => {
      const { container } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} isModal={true} />,
        { preloadedState: createGuestState() }
      );

      expect(container.querySelector('.auth-modal-content')).toBeInTheDocument();
      expect(container.querySelector('.auth-modal-card')).toBeInTheDocument();
    });

    it('renders with correct CSS classes for non-modal mode', () => {
      const { container } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} isModal={false} />,
        { preloadedState: createGuestState() }
      );

      expect(container.querySelector('.auth-container')).toBeInTheDocument();
      expect(container.querySelector('.auth-card')).toBeInTheDocument();
    });

    it('has proper form accessibility attributes', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');

      expect(emailInput).toHaveAttribute('type', 'email');
      expect(emailInput).toHaveAttribute('required');
      expect(emailInput).toHaveAttribute('placeholder', 'Enter your email');

      expect(passwordInput).toHaveAttribute('type', 'password');
      expect(passwordInput).toHaveAttribute('required');
      expect(passwordInput).toHaveAttribute('placeholder', 'Enter your password');
    });
  });

  describe('Form Input Handling', () => {
    it('updates email input value', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      await user.type(emailInput, 'test@example.com');

      expect(emailInput).toHaveValue('test@example.com');
    });

    it('updates password input value', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const passwordInput = screen.getByLabelText('Password');
      await user.type(passwordInput, 'password123');

      expect(passwordInput).toHaveValue('password123');
    });

    it('handles rapid input changes', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');

      // Rapid typing simulation
      await user.type(emailInput, 'a');
      await user.type(emailInput, 'b');
      await user.type(emailInput, 'c');

      expect(emailInput).toHaveValue('abc');
    });

    it('handles special characters in input', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');

      await user.type(emailInput, 'test+special@example.com');
      await user.type(passwordInput, 'pass@word#123!');

      expect(emailInput).toHaveValue('test+special@example.com');
      expect(passwordInput).toHaveValue('pass@word#123!');
    });
  });

  describe('Form Submission', () => {
    it('submits form with correct data', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');
      const submitButton = screen.getByRole('button', { name: 'Login' });

      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'password123');
      await user.click(submitButton);

      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/login')
        })
      );
    });

    it('does not submit form with empty email', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const passwordInput = screen.getByLabelText('Password');
      const submitButton = screen.getByRole('button', { name: 'Login' });

      await user.type(passwordInput, 'password123');
      await user.click(submitButton);

      // Should not dispatch login action with empty email
      expect(store.dispatch).not.toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/login')
        })
      );
    });

    it('does not submit form with empty password', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      const submitButton = screen.getByRole('button', { name: 'Login' });

      await user.type(emailInput, 'test@example.com');
      await user.click(submitButton);

      expect(store.dispatch).not.toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/login')
        })
      );
    });

    it('handles Enter key submission', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');

      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'password123');
      await user.keyboard('{Enter}');

      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/login')
        })
      );
    });
  });

  describe('Loading States', () => {
    it('shows loading state when login is in progress', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        {
          preloadedState: {
            auth: {
              ...createGuestState().auth,
              isLoading: true,
            },
          },
        }
      );

      const submitButton = screen.getByRole('button', { name: 'Logging in...' });
      expect(submitButton).toBeDisabled();
    });

    it('disables form during loading', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        {
          preloadedState: {
            auth: {
              ...createGuestState().auth,
              isLoading: true,
            },
          },
        }
      );

      const submitButton = screen.getByRole('button', { name: 'Logging in...' });
      expect(submitButton).toBeDisabled();
    });

    it('enables form when not loading', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const submitButton = screen.getByRole('button', { name: 'Login' });
      expect(submitButton).not.toBeDisabled();
    });
  });

  describe('Error Handling', () => {
    it('displays error message when login fails', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        {
          preloadedState: {
            auth: {
              ...createGuestState().auth,
              error: 'Invalid credentials',
            },
          },
        }
      );

      expect(screen.getByText('Invalid credentials')).toBeInTheDocument();
      expect(screen.getByText('Invalid credentials')).toHaveClass('error-message');
    });

    it('does not display error message when no error', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.queryByText(/error/i)).not.toBeInTheDocument();
    });

    it('clears error when component mounts', () => {
      const { store } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'auth/clearError'
        })
      );
    });
  });

  describe('Test User Functionality', () => {
    it('populates form with test user credentials', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const testUserButton = screen.getByText('Use Test User');
      await user.click(testUserButton);

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');

      expect(emailInput).toHaveValue('qatest@mytrader.com');
      expect(passwordInput).toHaveValue('QATest123!');
    });

    it('overwrites existing form data with test user', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');

      // Enter some data first
      await user.type(emailInput, 'existing@example.com');
      await user.type(passwordInput, 'existingpass');

      // Click test user button
      const testUserButton = screen.getByText('Use Test User');
      await user.click(testUserButton);

      expect(emailInput).toHaveValue('qatest@mytrader.com');
      expect(passwordInput).toHaveValue('QATest123!');
    });
  });

  describe('Navigation and Callbacks', () => {
    it('calls onSwitchToRegister when register link is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const registerLink = screen.getByText('Register here');
      await user.click(registerLink);

      expect(mockOnSwitchToRegister).toHaveBeenCalledTimes(1);
    });

    it('calls onSuccess when authentication succeeds', async () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} onSuccess={mockOnSuccess} />,
        {
          preloadedState: {
            auth: {
              ...createGuestState().auth,
              isAuthenticated: true,
            },
          },
        }
      );

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledTimes(1);
      });
    });

    it('does not call onSuccess when not authenticated', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} onSuccess={mockOnSuccess} />,
        { preloadedState: createGuestState() }
      );

      expect(mockOnSuccess).not.toHaveBeenCalled();
    });

    it('handles missing onSuccess callback gracefully', () => {
      expect(() => {
        renderWithProviders(
          <Login onSwitchToRegister={mockOnSwitchToRegister} />,
          {
            preloadedState: {
              auth: {
                ...createGuestState().auth,
                isAuthenticated: true,
              },
            },
          }
        );
      }).not.toThrow();
    });
  });

  describe('Accessibility and Keyboard Navigation', () => {
    it('supports tab navigation through form elements', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      // Tab through form elements
      await user.tab();
      expect(screen.getByLabelText('Email')).toHaveFocus();

      await user.tab();
      expect(screen.getByLabelText('Password')).toHaveFocus();

      await user.tab();
      expect(screen.getByRole('button', { name: 'Login' })).toHaveFocus();
    });

    it('has proper ARIA labels and form associations', () => {
      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');

      expect(emailInput).toHaveAttribute('id', 'email');
      expect(passwordInput).toHaveAttribute('id', 'password');

      const emailLabel = screen.getByText('Email');
      const passwordLabel = screen.getByText('Password');

      expect(emailLabel).toHaveAttribute('for', 'email');
      expect(passwordLabel).toHaveAttribute('for', 'password');
    });

    it('supports keyboard interaction with buttons', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const testUserButton = screen.getByText('Use Test User');
      testUserButton.focus();

      await user.keyboard('{Enter}');

      const emailInput = screen.getByLabelText('Email');
      expect(emailInput).toHaveValue('qatest@mytrader.com');
    });
  });

  describe('Edge Cases and Error Recovery', () => {
    it('handles extremely long input values', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const longEmail = 'a'.repeat(100) + '@example.com';
      const longPassword = 'password' + 'x'.repeat(100);

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');

      await user.type(emailInput, longEmail);
      await user.type(passwordInput, longPassword);

      expect(emailInput).toHaveValue(longEmail);
      expect(passwordInput).toHaveValue(longPassword);
    });

    it('handles rapid form submissions', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');
      const submitButton = screen.getByRole('button', { name: 'Login' });

      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'password123');

      // Rapid clicks
      await user.click(submitButton);
      await user.click(submitButton);
      await user.click(submitButton);

      // Should only process valid submissions
      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/login')
        })
      );
    });

    it('maintains form state during component updates', async () => {
      const user = userEvent.setup();

      const { rerender } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      await user.type(emailInput, 'persistent@example.com');

      // Re-render with different props
      rerender(<Login onSwitchToRegister={mockOnSwitchToRegister} isModal={true} />);

      expect(screen.getByLabelText('Email')).toHaveValue('persistent@example.com');
    });

    it('handles authentication state changes during form interaction', async () => {
      const user = userEvent.setup();

      const { store, rerender } = renderWithProviders(
        <Login onSwitchToRegister={mockOnSwitchToRegister} onSuccess={mockOnSuccess} />,
        { preloadedState: createGuestState() }
      );

      const emailInput = screen.getByLabelText('Email');
      await user.type(emailInput, 'test@example.com');

      // Simulate authentication success during typing
      rerender(<Login onSwitchToRegister={mockOnSwitchToRegister} onSuccess={mockOnSuccess} />);

      // Update store to authenticated state
      store.dispatch({ type: 'test/setAuthenticated' });

      // Component should handle this gracefully
      expect(screen.getByLabelText('Email')).toHaveValue('test@example.com');
    });
  });
});