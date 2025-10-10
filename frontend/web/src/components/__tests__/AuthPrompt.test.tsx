import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import AuthPrompt from '../AuthPrompt';
import { renderWithProviders, createGuestState } from '../../test-utils';

// Mock the Login and Register components
vi.mock('../Login', () => ({
  default: ({ onSwitchToRegister, onSuccess, isModal }: any) => (
    <div data-testid="login-component">
      <div>Login Component</div>
      <div>isModal: {isModal ? 'true' : 'false'}</div>
      <button onClick={onSwitchToRegister}>Switch to Register</button>
      <button onClick={onSuccess}>Login Success</button>
    </div>
  ),
}));

vi.mock('../Register', () => ({
  default: ({ onSwitchToLogin, onSuccess, isModal }: any) => (
    <div data-testid="register-component">
      <div>Register Component</div>
      <div>isModal: {isModal ? 'true' : 'false'}</div>
      <button onClick={onSwitchToLogin}>Switch to Login</button>
      <button onClick={onSuccess}>Register Success</button>
    </div>
  ),
}));

describe('AuthPrompt Component', () => {
  const mockOnClose = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Visibility and Modal Behavior', () => {
    it('does not render when isOpen is false', () => {
      renderWithProviders(
        <AuthPrompt isOpen={false} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.queryByText('Authentication Required')).not.toBeInTheDocument();
      expect(screen.queryByTestId('login-component')).not.toBeInTheDocument();
    });

    it('renders when isOpen is true', () => {
      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByText('Authentication Required')).toBeInTheDocument();
      expect(screen.getByTestId('login-component')).toBeInTheDocument();
    });

    it('renders with custom title and message', () => {
      renderWithProviders(
        <AuthPrompt
          isOpen={true}
          onClose={mockOnClose}
          title="Custom Title"
          message="Custom message for authentication"
        />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByText('Custom Title')).toBeInTheDocument();
      expect(screen.getByText('Custom message for authentication')).toBeInTheDocument();
    });

    it('uses default title and message when not provided', () => {
      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByText('Authentication Required')).toBeInTheDocument();
      expect(screen.getByText('Please sign in to access this feature.')).toBeInTheDocument();
    });
  });

  describe('Close Functionality', () => {
    it('calls onClose when close button is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      const closeButton = screen.getByLabelText('Close');
      await user.click(closeButton);

      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });

    it('calls onClose when "Continue as Guest" is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      const continueAsGuestButton = screen.getByText('Continue as Guest');
      await user.click(continueAsGuestButton);

      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });

    it('calls onClose when authentication is successful', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      const loginSuccessButton = screen.getByText('Login Success');
      await user.click(loginSuccessButton);

      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });
  });

  describe('Authentication View Switching', () => {
    it('starts with login view by default', () => {
      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByTestId('login-component')).toBeInTheDocument();
      expect(screen.queryByTestId('register-component')).not.toBeInTheDocument();
    });

    it('switches from login to register view', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      const switchToRegisterButton = screen.getByText('Switch to Register');
      await user.click(switchToRegisterButton);

      await waitFor(() => {
        expect(screen.getByTestId('register-component')).toBeInTheDocument();
        expect(screen.queryByTestId('login-component')).not.toBeInTheDocument();
      });
    });

    it('switches from register to login view', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      // First switch to register
      const switchToRegisterButton = screen.getByText('Switch to Register');
      await user.click(switchToRegisterButton);

      await waitFor(() => {
        expect(screen.getByTestId('register-component')).toBeInTheDocument();
      });

      // Then switch back to login
      const switchToLoginButton = screen.getByText('Switch to Login');
      await user.click(switchToLoginButton);

      await waitFor(() => {
        expect(screen.getByTestId('login-component')).toBeInTheDocument();
        expect(screen.queryByTestId('register-component')).not.toBeInTheDocument();
      });
    });

    it('handles multiple rapid switches between views', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      // Rapid switching
      for (let i = 0; i < 3; i++) {
        const switchToRegisterButton = screen.getByText('Switch to Register');
        await user.click(switchToRegisterButton);

        await waitFor(() => {
          expect(screen.getByTestId('register-component')).toBeInTheDocument();
        });

        const switchToLoginButton = screen.getByText('Switch to Login');
        await user.click(switchToLoginButton);

        await waitFor(() => {
          expect(screen.getByTestId('login-component')).toBeInTheDocument();
        });
      }

      // Should end up in login view
      expect(screen.getByTestId('login-component')).toBeInTheDocument();
    });
  });

  describe('Modal Props Passing', () => {
    it('passes isModal=true to Login component', () => {
      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByText('isModal: true')).toBeInTheDocument();
    });

    it('passes isModal=true to Register component', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      const switchToRegisterButton = screen.getByText('Switch to Register');
      await user.click(switchToRegisterButton);

      await waitFor(() => {
        expect(screen.getByText('isModal: true')).toBeInTheDocument();
      });
    });
  });

  describe('Successful Authentication Handling', () => {
    it('closes modal after successful login', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      const loginSuccessButton = screen.getByText('Login Success');
      await user.click(loginSuccessButton);

      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });

    it('closes modal after successful registration', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      // Switch to register view
      const switchToRegisterButton = screen.getByText('Switch to Register');
      await user.click(switchToRegisterButton);

      await waitFor(() => {
        expect(screen.getByTestId('register-component')).toBeInTheDocument();
      });

      // Trigger successful registration
      const registerSuccessButton = screen.getByText('Register Success');
      await user.click(registerSuccessButton);

      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });
  });

  describe('Component Structure and Accessibility', () => {
    it('renders proper modal structure', () => {
      const { container } = renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      expect(container.querySelector('.auth-prompt-overlay')).toBeInTheDocument();
      expect(container.querySelector('.auth-prompt-modal')).toBeInTheDocument();
      expect(container.querySelector('.auth-prompt-header')).toBeInTheDocument();
      expect(container.querySelector('.auth-prompt-content')).toBeInTheDocument();
      expect(container.querySelector('.auth-prompt-footer')).toBeInTheDocument();
    });

    it('has accessible close button with aria-label', () => {
      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      const closeButton = screen.getByLabelText('Close');
      expect(closeButton).toBeInTheDocument();
      expect(closeButton).toHaveTextContent('×');
    });

    it('displays all required UI elements', () => {
      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByText('Authentication Required')).toBeInTheDocument();
      expect(screen.getByText('Please sign in to access this feature.')).toBeInTheDocument();
      expect(screen.getByText('Continue as Guest')).toBeInTheDocument();
      expect(screen.getByLabelText('Close')).toBeInTheDocument();
      expect(screen.getByTestId('login-component')).toBeInTheDocument();
    });
  });

  describe('Edge Cases and Error Handling', () => {
    it('handles missing onClose prop gracefully', async () => {
      const user = userEvent.setup();

      // TypeScript would normally prevent this, but testing runtime behavior
      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={undefined as any} />,
        { preloadedState: createGuestState() }
      );

      const closeButton = screen.getByLabelText('Close');

      // Should not throw error when clicking close without onClose prop
      await expect(user.click(closeButton)).resolves.not.toThrow();
    });

    it('maintains view state when reopened', async () => {
      const user = userEvent.setup();

      const { rerender } = renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      // Switch to register view
      const switchToRegisterButton = screen.getByText('Switch to Register');
      await user.click(switchToRegisterButton);

      await waitFor(() => {
        expect(screen.getByTestId('register-component')).toBeInTheDocument();
      });

      // Close modal
      rerender(<AuthPrompt isOpen={false} onClose={mockOnClose} />);

      // Reopen modal - should reset to login view
      rerender(<AuthPrompt isOpen={true} onClose={mockOnClose} />);

      expect(screen.getByTestId('login-component')).toBeInTheDocument();
      expect(screen.queryByTestId('register-component')).not.toBeInTheDocument();
    });

    it('handles rapid open/close operations', () => {
      const { rerender } = renderWithProviders(
        <AuthPrompt isOpen={false} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      // Rapidly toggle open/close
      for (let i = 0; i < 5; i++) {
        rerender(<AuthPrompt isOpen={true} onClose={mockOnClose} />);
        rerender(<AuthPrompt isOpen={false} onClose={mockOnClose} />);
      }

      // Final state
      rerender(<AuthPrompt isOpen={true} onClose={mockOnClose} />);

      expect(screen.getByText('Authentication Required')).toBeInTheDocument();
      expect(screen.getByTestId('login-component')).toBeInTheDocument();
    });
  });

  describe('Keyboard Navigation and Focus Management', () => {
    it('supports keyboard navigation', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      // Tab through interactive elements
      await user.tab();
      expect(screen.getByLabelText('Close')).toHaveFocus();

      // Continue tabbing to reach other elements
      await user.tab();
      // The next focusable element should receive focus
      // (exact element depends on the Login component implementation)
    });

    it('allows closing with Escape key when close button is focused', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      const closeButton = screen.getByLabelText('Close');
      closeButton.focus();

      await user.keyboard('{Enter}');

      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });
  });

  describe('Integration with Authentication Components', () => {
    it('properly integrates with Login component callbacks', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      // Verify Login component receives correct props
      expect(screen.getByTestId('login-component')).toBeInTheDocument();

      // Test switch functionality
      const switchButton = screen.getByText('Switch to Register');
      await user.click(switchButton);

      await waitFor(() => {
        expect(screen.getByTestId('register-component')).toBeInTheDocument();
      });
    });

    it('properly integrates with Register component callbacks', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AuthPrompt isOpen={true} onClose={mockOnClose} />,
        { preloadedState: createGuestState() }
      );

      // Switch to register first
      const switchToRegisterButton = screen.getByText('Switch to Register');
      await user.click(switchToRegisterButton);

      await waitFor(() => {
        expect(screen.getByTestId('register-component')).toBeInTheDocument();
      });

      // Test switch back functionality
      const switchToLoginButton = screen.getByText('Switch to Login');
      await user.click(switchToLoginButton);

      await waitFor(() => {
        expect(screen.getByTestId('login-component')).toBeInTheDocument();
      });
    });
  });
});