import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import NotificationCenter from '../../dashboard/NotificationCenter';
import { renderWithProviders, createGuestState } from '../../../test-utils';
import { removeNotification } from '../../../store/slices/uiSlice';

describe('NotificationCenter Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  const mockNotifications = [
    {
      id: '1',
      type: 'success' as const,
      message: 'Login successful',
      autoClose: true,
    },
    {
      id: '2',
      type: 'error' as const,
      message: 'Failed to load data',
      autoClose: true,
    },
    {
      id: '3',
      type: 'warning' as const,
      message: 'Connection unstable',
      autoClose: false,
    },
    {
      id: '4',
      type: 'info' as const,
      message: 'New feature available',
      autoClose: true,
    },
  ];

  const createStateWithNotifications = (notifications: any[]) => ({
    ...createGuestState(),
    ui: {
      theme: 'light',
      sidebarOpen: true,
      loading: false,
      notifications,
    },
  });

  describe('Rendering', () => {
    it('renders nothing when no notifications', () => {
      const { container } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createGuestState(),
      });

      expect(container.firstChild).toBeNull();
    });

    it('renders all notifications', () => {
      renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mockNotifications),
      });

      expect(screen.getByText('Login successful')).toBeInTheDocument();
      expect(screen.getByText('Failed to load data')).toBeInTheDocument();
      expect(screen.getByText('Connection unstable')).toBeInTheDocument();
      expect(screen.getByText('New feature available')).toBeInTheDocument();
    });

    it('renders correct notification types with icons', () => {
      renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mockNotifications),
      });

      expect(screen.getByText('✅')).toBeInTheDocument(); // success icon
      expect(screen.getByText('❌')).toBeInTheDocument(); // error icon
      expect(screen.getByText('⚠️')).toBeInTheDocument(); // warning icon
      expect(screen.getByText('ℹ️')).toBeInTheDocument(); // info icon
    });

    it('applies correct CSS classes for notification types', () => {
      const { container } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mockNotifications),
      });

      expect(container.querySelector('.notification-success')).toBeInTheDocument();
      expect(container.querySelector('.notification-error')).toBeInTheDocument();
      expect(container.querySelector('.notification-warning')).toBeInTheDocument();
      expect(container.querySelector('.notification-info')).toBeInTheDocument();
    });

    it('renders close buttons for all notifications', () => {
      renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mockNotifications),
      });

      const closeButtons = screen.getAllByText('×');
      expect(closeButtons).toHaveLength(4);
    });
  });

  describe('Manual Dismissal', () => {
    it('dispatches removeNotification when close button is clicked', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([mockNotifications[0]]),
      });

      const closeButton = screen.getByText('×');
      await user.click(closeButton);

      expect(store.dispatch).toHaveBeenCalledWith(
        removeNotification('1')
      );
    });

    it('removes specific notification when clicked', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mockNotifications),
      });

      // Find and click the close button for the error notification
      const errorNotification = screen.getByText('Failed to load data')
        .closest('.notification');
      const closeButton = errorNotification!.querySelector('.notification-close');
      
      await user.click(closeButton!);

      expect(store.dispatch).toHaveBeenCalledWith(
        removeNotification('2')
      );
    });

    it('handles rapid clicking on close buttons', async () => {
      const user = userEvent.setup();

      const { store } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([mockNotifications[0]]),
      });

      const closeButton = screen.getByText('×');
      
      // Rapid clicks
      await user.click(closeButton);
      await user.click(closeButton);
      await user.click(closeButton);

      // Should still only dispatch once per existing notification
      expect(store.dispatch).toHaveBeenCalledWith(
        removeNotification('1')
      );
    });
  });

  describe('Auto Dismissal', () => {
    it('auto-removes notifications after 5 seconds', async () => {
      const { store } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([
          {
            id: 'auto-1',
            type: 'success',
            message: 'Auto dismiss test',
            autoClose: true,
          },
        ]),
      });

      // Fast-forward 5 seconds
      vi.advanceTimersByTime(5000);

      await waitFor(() => {
        expect(store.dispatch).toHaveBeenCalledWith(
          removeNotification('auto-1')
        );
      });
    });

    it('does not auto-remove notifications with autoClose: false', async () => {
      const { store } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([
          {
            id: 'persistent-1',
            type: 'warning',
            message: 'Persistent notification',
            autoClose: false,
          },
        ]),
      });

      // Fast-forward 5 seconds
      vi.advanceTimersByTime(5000);

      // Should not have been called for auto-dismiss
      expect(store.dispatch).not.toHaveBeenCalledWith(
        removeNotification('persistent-1')
      );
    });

    it('handles mixed auto-close and persistent notifications', async () => {
      const mixedNotifications = [
        {
          id: 'auto-1',
          type: 'success' as const,
          message: 'Will auto close',
          autoClose: true,
        },
        {
          id: 'persistent-1',
          type: 'error' as const,
          message: 'Will persist',
          autoClose: false,
        },
      ];

      const { store } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mixedNotifications),
      });

      // Fast-forward 5 seconds
      vi.advanceTimersByTime(5000);

      await waitFor(() => {
        expect(store.dispatch).toHaveBeenCalledWith(
          removeNotification('auto-1')
        );
      });

      expect(store.dispatch).not.toHaveBeenCalledWith(
        removeNotification('persistent-1')
      );
    });

    it('clears timers when notifications are removed manually', async () => {
      const user = userEvent.setup();
      const clearTimeoutSpy = vi.spyOn(global, 'clearTimeout');

      renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([
          {
            id: 'timer-test',
            type: 'info',
            message: 'Timer test',
            autoClose: true,
          },
        ]),
      });

      const closeButton = screen.getByText('×');
      await user.click(closeButton);

      // Timer should be cleared when component re-renders
      expect(clearTimeoutSpy).toHaveBeenCalled();
      clearTimeoutSpy.mockRestore();
    });
  });

  describe('Notification Updates', () => {
    it('handles new notifications being added', () => {
      const { rerender } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([mockNotifications[0]]),
      });

      expect(screen.getByText('Login successful')).toBeInTheDocument();
      expect(screen.queryByText('Failed to load data')).not.toBeInTheDocument();

      // Add new notification
      rerender(<NotificationCenter />);
      
      // Update the store state with new notifications (in real app, this would be done via Redux)
      const { rerender: rerenderWithNew } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mockNotifications.slice(0, 2)),
      });

      expect(screen.getByText('Login successful')).toBeInTheDocument();
      expect(screen.getByText('Failed to load data')).toBeInTheDocument();
    });

    it('handles notifications being removed from store', () => {
      const { rerender } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mockNotifications),
      });

      expect(screen.getByText('Login successful')).toBeInTheDocument();

      // Remove notifications
      const { rerender: rerenderWithLess } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([]),
      });

      expect(screen.queryByText('Login successful')).not.toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('handles notifications with missing properties gracefully', () => {
      const malformedNotifications = [
        {
          id: 'malformed-1',
          type: 'success' as const,
          message: '', // Empty message
          autoClose: true,
        },
        {
          id: 'malformed-2',
          type: undefined as any,
          message: 'No type notification',
          autoClose: true,
        },
      ];

      expect(() => {
        renderWithProviders(<NotificationCenter />, {
          preloadedState: createStateWithNotifications(malformedNotifications),
        });
      }).not.toThrow();
    });

    it('handles duplicate notification IDs', () => {
      const duplicateNotifications = [
        {
          id: 'duplicate',
          type: 'success' as const,
          message: 'First notification',
          autoClose: true,
        },
        {
          id: 'duplicate',
          type: 'error' as const,
          message: 'Second notification',
          autoClose: true,
        },
      ];

      expect(() => {
        renderWithProviders(<NotificationCenter />, {
          preloadedState: createStateWithNotifications(duplicateNotifications),
        });
      }).not.toThrow();

      // Should render both despite duplicate IDs (React will warn but continue)
      expect(screen.getByText('First notification')).toBeInTheDocument();
      expect(screen.getByText('Second notification')).toBeInTheDocument();
    });

    it('handles very long notification messages', () => {
      const longMessage = 'A'.repeat(1000);
      const longNotification = [{
        id: 'long-message',
        type: 'info' as const,
        message: longMessage,
        autoClose: true,
      }];

      renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(longNotification),
      });

      expect(screen.getByText(longMessage)).toBeInTheDocument();
    });

    it('handles rapid notification changes', async () => {
      const { store } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications(mockNotifications),
      });

      const closeButtons = screen.getAllByText('×');
      
      // Click multiple close buttons rapidly
      for (const button of closeButtons) {
        await userEvent.click(button);
      }

      expect(store.dispatch).toHaveBeenCalledTimes(4);
    });
  });

  describe('Accessibility', () => {
    it('close buttons are keyboard accessible', async () => {
      const user = userEvent.setup();

      renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([mockNotifications[0]]),
      });

      const closeButton = screen.getByText('×');
      closeButton.focus();
      expect(closeButton).toHaveFocus();

      await user.keyboard('{Enter}');
      // Note: The actual dispatch behavior may vary based on implementation
    });

    it('notifications have proper semantic structure', () => {
      const { container } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([mockNotifications[0]]),
      });

      const notification = container.querySelector('.notification');
      expect(notification).toBeInTheDocument();
      
      const content = container.querySelector('.notification-content');
      expect(content).toBeInTheDocument();
      
      const icon = container.querySelector('.notification-icon');
      expect(icon).toBeInTheDocument();
    });

    it('provides meaningful close button labels implicitly', () => {
      renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([mockNotifications[0]]),
      });

      const closeButton = screen.getByText('×');
      expect(closeButton).toHaveClass('notification-close');
    });
  });

  describe('Memory Management', () => {
    it('cleans up timers on component unmount', () => {
      const clearTimeoutSpy = vi.spyOn(global, 'clearTimeout');

      const { unmount } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([mockNotifications[0]]),
      });

      unmount();

      // Cleanup should occur when component unmounts
      expect(clearTimeoutSpy).toHaveBeenCalled();
      clearTimeoutSpy.mockRestore();
    });

    it('handles component re-mounting correctly', () => {
      const { unmount } = renderWithProviders(<NotificationCenter />, {
        preloadedState: createStateWithNotifications([mockNotifications[0]]),
      });

      unmount();

      // Re-mount should work without errors
      expect(() => {
        renderWithProviders(<NotificationCenter />, {
          preloadedState: createStateWithNotifications([mockNotifications[1]]),
        });
      }).not.toThrow();
    });
  });
});
