import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Register from '../Register';
import { renderWithProviders, createGuestState } from '../../test-utils';
import { registerAsync } from '../../store/slices/authSlice';

// Mock the async thunk
vi.mock('../../store/slices/authSlice', async () => {
  const actual = await vi.importActual('../../store/slices/authSlice');
  return {
    ...actual,
    registerAsync: vi.fn(),
  };
});

const mockRegisterAsync = registerAsync as any;

describe('Register Component', () => {
  const mockOnSwitchToLogin = vi.fn();
  const mockOnSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    mockRegisterAsync.mockReturnValue({ type: 'auth/register/pending' });
  });

  describe('Rendering and UI Elements', () => {
    it('renders registration form correctly', () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByText('Register for myTrader')).toBeInTheDocument();
      expect(screen.getByLabelText('First Name')).toBeInTheDocument();
      expect(screen.getByLabelText('Last Name')).toBeInTheDocument();
      expect(screen.getByLabelText('Email')).toBeInTheDocument();
      expect(screen.getByLabelText('Phone')).toBeInTheDocument();
      expect(screen.getByLabelText('Password')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Register' })).toBeInTheDocument();
      expect(screen.getByText('Already have an account?')).toBeInTheDocument();
      expect(screen.getByText('Login here')).toBeInTheDocument();
    });

    it('renders with correct CSS classes for modal mode', () => {
      const { container } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} isModal={true} />,
        { preloadedState: createGuestState() }
      );

      expect(container.querySelector('.auth-modal-content')).toBeInTheDocument();
      expect(container.querySelector('.auth-modal-card')).toBeInTheDocument();
    });

    it('renders with correct CSS classes for non-modal mode', () => {
      const { container } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} isModal={false} />,
        { preloadedState: createGuestState() }
      );

      expect(container.querySelector('.auth-container')).toBeInTheDocument();
      expect(container.querySelector('.auth-card')).toBeInTheDocument();
    });

    it('has proper form accessibility attributes', () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const firstNameInput = screen.getByLabelText('First Name');
      const lastNameInput = screen.getByLabelText('Last Name');
      const emailInput = screen.getByLabelText('Email');
      const phoneInput = screen.getByLabelText('Phone');
      const passwordInput = screen.getByLabelText('Password');

      expect(firstNameInput).toHaveAttribute('type', 'text');
      expect(firstNameInput).toHaveAttribute('required');
      expect(lastNameInput).toHaveAttribute('type', 'text');
      expect(lastNameInput).toHaveAttribute('required');
      expect(emailInput).toHaveAttribute('type', 'email');
      expect(emailInput).toHaveAttribute('required');
      expect(phoneInput).toHaveAttribute('type', 'tel');
      expect(phoneInput).toHaveAttribute('required');
      expect(passwordInput).toHaveAttribute('type', 'password');
      expect(passwordInput).toHaveAttribute('required');
    });

    it('has proper placeholder text', () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByPlaceholderText('First name')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Last name')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Enter your email')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('+905551234567')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Enter your password (min 6 chars)')).toBeInTheDocument();
    });
  });

  describe('Form Input Handling', () => {
    it('updates all form fields correctly', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const firstNameInput = screen.getByLabelText('First Name');
      const lastNameInput = screen.getByLabelText('Last Name');
      const emailInput = screen.getByLabelText('Email');
      const phoneInput = screen.getByLabelText('Phone');
      const passwordInput = screen.getByLabelText('Password');

      await user.type(firstNameInput, 'John');
      await user.type(lastNameInput, 'Doe');
      await user.type(emailInput, 'john.doe@example.com');
      await user.type(phoneInput, '+1234567890');
      await user.type(passwordInput, 'password123');

      expect(firstNameInput).toHaveValue('John');
      expect(lastNameInput).toHaveValue('Doe');
      expect(emailInput).toHaveValue('john.doe@example.com');
      expect(phoneInput).toHaveValue('+1234567890');
      expect(passwordInput).toHaveValue('password123');
    });

    it('clears local error when user starts typing', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const submitButton = screen.getByRole('button', { name: 'Register' });

      // Submit empty form to trigger error
      await user.click(submitButton);
      expect(screen.getByText('All fields are required')).toBeInTheDocument();

      // Start typing to clear error
      const firstNameInput = screen.getByLabelText('First Name');
      await user.type(firstNameInput, 'J');

      expect(screen.queryByText('All fields are required')).not.toBeInTheDocument();
    });

    it('handles special characters in inputs', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const firstNameInput = screen.getByLabelText('First Name');
      const emailInput = screen.getByLabelText('Email');
      const passwordInput = screen.getByLabelText('Password');

      await user.type(firstNameInput, "O'Connor");
      await user.type(emailInput, 'test+special@example-domain.com');
      await user.type(passwordInput, 'Pass@word#123!');

      expect(firstNameInput).toHaveValue("O'Connor");
      expect(emailInput).toHaveValue('test+special@example-domain.com');
      expect(passwordInput).toHaveValue('Pass@word#123!');
    });
  });

  describe('Form Validation', () => {
    it('shows error for empty fields', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      expect(screen.getByText('All fields are required')).toBeInTheDocument();
    });

    it('shows error for short password', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      // Fill all fields except password with short value
      await user.type(screen.getByLabelText('First Name'), 'John');
      await user.type(screen.getByLabelText('Last Name'), 'Doe');
      await user.type(screen.getByLabelText('Email'), 'john@example.com');
      await user.type(screen.getByLabelText('Phone'), '+1234567890');
      await user.type(screen.getByLabelText('Password'), '123'); // Too short

      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      expect(screen.getByText('Password must be at least 6 characters long')).toBeInTheDocument();
    });

    it('validates missing individual fields', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      // Fill all fields except first name
      await user.type(screen.getByLabelText('Last Name'), 'Doe');
      await user.type(screen.getByLabelText('Email'), 'john@example.com');
      await user.type(screen.getByLabelText('Phone'), '+1234567890');
      await user.type(screen.getByLabelText('Password'), 'password123');

      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      expect(screen.getByText('All fields are required')).toBeInTheDocument();
    });

    it('accepts valid form data', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      // Fill all fields with valid data
      await user.type(screen.getByLabelText('First Name'), 'John');
      await user.type(screen.getByLabelText('Last Name'), 'Doe');
      await user.type(screen.getByLabelText('Email'), 'john@example.com');
      await user.type(screen.getByLabelText('Phone'), '+1234567890');
      await user.type(screen.getByLabelText('Password'), 'password123');

      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/register')
        })
      );
    });

    it('validates minimum password length exactly', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      // Fill all fields with exactly 6 character password
      await user.type(screen.getByLabelText('First Name'), 'John');
      await user.type(screen.getByLabelText('Last Name'), 'Doe');
      await user.type(screen.getByLabelText('Email'), 'john@example.com');
      await user.type(screen.getByLabelText('Phone'), '+1234567890');
      await user.type(screen.getByLabelText('Password'), '123456'); // Exactly 6 chars

      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      // Should not show validation error and should submit
      expect(screen.queryByText('Password must be at least 6 characters long')).not.toBeInTheDocument();
      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/register')
        })
      );
    });
  });

  describe('Form Submission', () => {
    const validFormData = {
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      phone: '+1234567890',
      password: 'password123',
    };

    const fillValidForm = async (user: any) => {
      await user.type(screen.getByLabelText('First Name'), validFormData.firstName);
      await user.type(screen.getByLabelText('Last Name'), validFormData.lastName);
      await user.type(screen.getByLabelText('Email'), validFormData.email);
      await user.type(screen.getByLabelText('Phone'), validFormData.phone);
      await user.type(screen.getByLabelText('Password'), validFormData.password);
    };

    it('submits form with correct data structure', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      await fillValidForm(user);

      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/register')
        })
      );
    });

    it('handles Enter key submission', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      await fillValidForm(user);
      await user.keyboard('{Enter}');

      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/register')
        })
      );
    });

    it('prevents submission when validation fails', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      // Fill form with invalid password
      await user.type(screen.getByLabelText('First Name'), 'John');
      await user.type(screen.getByLabelText('Last Name'), 'Doe');
      await user.type(screen.getByLabelText('Email'), 'john@example.com');
      await user.type(screen.getByLabelText('Phone'), '+1234567890');
      await user.type(screen.getByLabelText('Password'), '123'); // Too short

      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      expect(store.dispatch).not.toHaveBeenCalledWith(
        expect.objectContaining({
          type: expect.stringContaining('auth/register')
        })
      );
    });
  });

  describe('Loading States', () => {
    it('shows loading state when registration is in progress', () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        {
          preloadedState: {
            auth: {
              ...createGuestState().auth,
              isLoading: true,
            },
          },
        }
      );

      const submitButton = screen.getByRole('button', { name: 'Creating Account...' });
      expect(submitButton).toBeDisabled();
    });

    it('disables form during loading', () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        {
          preloadedState: {
            auth: {
              ...createGuestState().auth,
              isLoading: true,
            },
          },
        }
      );

      const submitButton = screen.getByRole('button', { name: 'Creating Account...' });
      expect(submitButton).toBeDisabled();
    });

    it('enables form when not loading', () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const submitButton = screen.getByRole('button', { name: 'Register' });
      expect(submitButton).not.toBeDisabled();
    });
  });

  describe('Error Handling', () => {
    it('displays server error message when registration fails', () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        {
          preloadedState: {
            auth: {
              ...createGuestState().auth,
              error: 'Email already exists',
            },
          },
        }
      );

      expect(screen.getByText('Email already exists')).toBeInTheDocument();
    });

    it('displays local validation error message', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      expect(screen.getByText('All fields are required')).toBeInTheDocument();
    });

    it('prioritizes server error over local error', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        {
          preloadedState: {
            auth: {
              ...createGuestState().auth,
              error: 'Server error',
            },
          },
        }
      );

      // Try to trigger local error
      const submitButton = screen.getByRole('button', { name: 'Register' });
      await user.click(submitButton);

      // Should show server error, not local validation error
      expect(screen.getByText('Server error')).toBeInTheDocument();
      expect(screen.queryByText('All fields are required')).not.toBeInTheDocument();
    });

    it('clears error when component mounts', () => {
      const { store } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      expect(store.dispatch).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'auth/clearError'
        })
      );
    });
  });

  describe('Navigation and Callbacks', () => {
    it('calls onSwitchToLogin when login link is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const loginLink = screen.getByText('Login here');
      await user.click(loginLink);

      expect(mockOnSwitchToLogin).toHaveBeenCalledTimes(1);
    });

    it('calls onSuccess when authentication succeeds', async () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} onSuccess={mockOnSuccess} />,
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
        <Register onSwitchToLogin={mockOnSwitchToLogin} onSuccess={mockOnSuccess} />,
        { preloadedState: createGuestState() }
      );

      expect(mockOnSuccess).not.toHaveBeenCalled();
    });

    it('handles missing onSuccess callback gracefully', () => {
      expect(() => {
        renderWithProviders(
          <Register onSwitchToLogin={mockOnSwitchToLogin} />,
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
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      await user.tab();
      expect(screen.getByLabelText('First Name')).toHaveFocus();

      await user.tab();
      expect(screen.getByLabelText('Last Name')).toHaveFocus();

      await user.tab();
      expect(screen.getByLabelText('Email')).toHaveFocus();

      await user.tab();
      expect(screen.getByLabelText('Phone')).toHaveFocus();

      await user.tab();
      expect(screen.getByLabelText('Password')).toHaveFocus();

      await user.tab();
      expect(screen.getByRole('button', { name: 'Register' })).toHaveFocus();
    });

    it('has proper ARIA labels and form associations', () => {
      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      expect(screen.getByLabelText('First Name')).toHaveAttribute('id', 'firstName');
      expect(screen.getByLabelText('Last Name')).toHaveAttribute('id', 'lastName');
      expect(screen.getByLabelText('Email')).toHaveAttribute('id', 'email');
      expect(screen.getByLabelText('Phone')).toHaveAttribute('id', 'phone');
      expect(screen.getByLabelText('Password')).toHaveAttribute('id', 'password');
    });

    it('has proper form structure with form-row grouping', () => {
      const { container } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const formRow = container.querySelector('.form-row');
      expect(formRow).toBeInTheDocument();

      const formGroups = container.querySelectorAll('.form-group');
      expect(formGroups.length).toBeGreaterThanOrEqual(5); // At least 5 form groups
    });
  });

  describe('Edge Cases and Error Recovery', () => {
    it('handles extremely long input values', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const longName = 'a'.repeat(100);
      const longEmail = 'test' + 'x'.repeat(100) + '@example.com';

      await user.type(screen.getByLabelText('First Name'), longName);
      await user.type(screen.getByLabelText('Email'), longEmail);

      expect(screen.getByLabelText('First Name')).toHaveValue(longName);
      expect(screen.getByLabelText('Email')).toHaveValue(longEmail);
    });

    it('handles rapid error state changes', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const submitButton = screen.getByRole('button', { name: 'Register' });

      // Trigger error
      await user.click(submitButton);
      expect(screen.getByText('All fields are required')).toBeInTheDocument();

      // Clear error by typing
      await user.type(screen.getByLabelText('First Name'), 'J');
      expect(screen.queryByText('All fields are required')).not.toBeInTheDocument();

      // Trigger error again
      await user.clear(screen.getByLabelText('First Name'));
      await user.click(submitButton);
      expect(screen.getByText('All fields are required')).toBeInTheDocument();
    });

    it('maintains form state during component updates', async () => {
      const user = userEvent.setup();

      const { rerender } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      await user.type(screen.getByLabelText('First Name'), 'Persistent');
      await user.type(screen.getByLabelText('Email'), 'persist@example.com');

      // Re-render with different props
      rerender(<Register onSwitchToLogin={mockOnSwitchToLogin} isModal={true} />);

      expect(screen.getByLabelText('First Name')).toHaveValue('Persistent');
      expect(screen.getByLabelText('Email')).toHaveValue('persist@example.com');
    });

    it('handles international phone numbers', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      const internationalNumbers = [
        '+447700900123', // UK
        '+33123456789', // France
        '+49301234567', // Germany
        '+8613800138000', // China
      ];

      for (const number of internationalNumbers) {
        const phoneInput = screen.getByLabelText('Phone');
        await user.clear(phoneInput);
        await user.type(phoneInput, number);
        expect(phoneInput).toHaveValue(number);
      }
    });

    it('validates edge case passwords', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(
        <Register onSwitchToLogin={mockOnSwitchToLogin} />,
        { preloadedState: createGuestState() }
      );

      // Test various password edge cases
      const testCases = [
        { password: 'abcdef', shouldPass: true },  // Exactly 6 chars
        { password: 'abcde', shouldPass: false },  // 5 chars
        { password: '123456', shouldPass: true },  // Numbers only
        { password: '!@#$%^', shouldPass: true },  // Special chars only
        { password: 'aB3!@#', shouldPass: true },  // Mixed chars
      ];

      for (const testCase of testCases) {
        // Fill form
        await user.clear(screen.getByLabelText('First Name'));
        await user.type(screen.getByLabelText('First Name'), 'John');
        await user.clear(screen.getByLabelText('Last Name'));
        await user.type(screen.getByLabelText('Last Name'), 'Doe');
        await user.clear(screen.getByLabelText('Email'));
        await user.type(screen.getByLabelText('Email'), 'john@example.com');
        await user.clear(screen.getByLabelText('Phone'));
        await user.type(screen.getByLabelText('Phone'), '+1234567890');
        await user.clear(screen.getByLabelText('Password'));
        await user.type(screen.getByLabelText('Password'), testCase.password);

        const submitButton = screen.getByRole('button', { name: 'Register' });
        await user.click(submitButton);

        if (testCase.shouldPass) {
          expect(screen.queryByText('Password must be at least 6 characters long')).not.toBeInTheDocument();
        } else {
          expect(screen.getByText('Password must be at least 6 characters long')).toBeInTheDocument();
        }
      }
    });
  });
});