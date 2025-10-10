/**
 * UI state store using Zustand
 */

import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';
import type { UIState, Theme, Notification, NotificationType } from '../types';
import { generateId } from '../utils';

interface UIStore extends UIState {
  // Actions
  setTheme: (theme: Theme) => void;
  toggleSidebar: () => void;
  setSidebarOpen: (isOpen: boolean) => void;
  setSidebarCollapsed: (isCollapsed: boolean) => void;
  setActiveSection: (section: string | null) => void;
  openModal: (modalId: string) => void;
  closeModal: (modalId: string) => void;
  toggleModal: (modalId: string) => void;
  addNotification: (notification: Omit<Notification, 'id' | 'createdAt'>) => void;
  removeNotification: (id: string) => void;
  clearNotifications: () => void;
  setLoading: (key: string, loading: boolean) => void;
  clearLoading: () => void;

  // Getters
  isModalOpen: (modalId: string) => boolean;
  getNotificationsByType: (type: NotificationType) => Notification[];
  isLoading: (key: string) => boolean;
  hasNotifications: () => boolean;
}

const DEFAULT_NOTIFICATION_DURATION = 5000; // 5 seconds

export const useUIStore = create<UIStore>()(
  devtools(
    persist(
      (set, get) => ({
        // Initial state
        theme: 'system',
        sidebar: {
          isOpen: false,
          isCollapsed: false,
          activeSection: null,
        },
        modals: {},
        notifications: [],
        loading: {},

        // Actions
        setTheme: (theme: Theme) => {
          set({ theme });

          // Apply theme to document
          const root = document.documentElement;

          if (theme === 'dark') {
            root.classList.add('dark');
          } else if (theme === 'light') {
            root.classList.remove('dark');
          } else {
            // System theme
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            if (prefersDark) {
              root.classList.add('dark');
            } else {
              root.classList.remove('dark');
            }
          }
        },

        toggleSidebar: () => {
          set((state) => ({
            sidebar: {
              ...state.sidebar,
              isOpen: !state.sidebar.isOpen,
            },
          }));
        },

        setSidebarOpen: (isOpen: boolean) => {
          set((state) => ({
            sidebar: {
              ...state.sidebar,
              isOpen,
            },
          }));
        },

        setSidebarCollapsed: (isCollapsed: boolean) => {
          set((state) => ({
            sidebar: {
              ...state.sidebar,
              isCollapsed,
            },
          }));
        },

        setActiveSection: (section: string | null) => {
          set((state) => ({
            sidebar: {
              ...state.sidebar,
              activeSection: section,
            },
          }));
        },

        openModal: (modalId: string) => {
          set((state) => ({
            modals: {
              ...state.modals,
              [modalId]: true,
            },
          }));
        },

        closeModal: (modalId: string) => {
          set((state) => ({
            modals: {
              ...state.modals,
              [modalId]: false,
            },
          }));
        },

        toggleModal: (modalId: string) => {
          const isOpen = get().isModalOpen(modalId);
          if (isOpen) {
            get().closeModal(modalId);
          } else {
            get().openModal(modalId);
          }
        },

        addNotification: (notificationData) => {
          const notification: Notification = {
            id: generateId('notification'),
            createdAt: new Date().toISOString(),
            duration: DEFAULT_NOTIFICATION_DURATION,
            ...notificationData,
          };

          set((state) => ({
            notifications: [notification, ...state.notifications],
          }));

          // Auto-remove notification after duration
          if (notification.duration && notification.duration > 0) {
            setTimeout(() => {
              get().removeNotification(notification.id);
            }, notification.duration);
          }
        },

        removeNotification: (id: string) => {
          set((state) => ({
            notifications: state.notifications.filter((n) => n.id !== id),
          }));
        },

        clearNotifications: () => {
          set({ notifications: [] });
        },

        setLoading: (key: string, loading: boolean) => {
          set((state) => ({
            loading: {
              ...state.loading,
              [key]: loading,
            },
          }));
        },

        clearLoading: () => {
          set({ loading: {} });
        },

        // Getters
        isModalOpen: (modalId: string) => {
          return get().modals[modalId] === true;
        },

        getNotificationsByType: (type: NotificationType) => {
          return get().notifications.filter((n) => n.type === type);
        },

        isLoading: (key: string) => {
          return get().loading[key] === true;
        },

        hasNotifications: () => {
          return get().notifications.length > 0;
        },
      }),
      {
        name: 'ui-storage',
        storage: createJSONStorage(() => localStorage),
        partialize: (state) => ({
          theme: state.theme,
          sidebar: {
            isCollapsed: state.sidebar.isCollapsed,
            activeSection: state.sidebar.activeSection,
          },
        }),
      }
    ),
    { name: 'UIStore' }
  )
);

// Initialize theme on store creation
const initializeTheme = () => {
  const { theme, setTheme } = useUIStore.getState();
  setTheme(theme);

  // Listen for system theme changes
  if (theme === 'system') {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const handleChange = () => {
      if (useUIStore.getState().theme === 'system') {
        useUIStore.getState().setTheme('system');
      }
    };

    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }
};

// Initialize theme when the store is created
if (typeof window !== 'undefined') {
  initializeTheme();
}

// Selectors for easier access
export const useTheme = () => useUIStore((state) => state.theme);
export const useSidebar = () => useUIStore((state) => state.sidebar);
export const useModals = () => useUIStore((state) => state.modals);
export const useNotifications = () => useUIStore((state) => state.notifications);
export const useLoadingStates = () => useUIStore((state) => state.loading);

// UI actions
export const useUIActions = () => useUIStore((state) => ({
  setTheme: state.setTheme,
  toggleSidebar: state.toggleSidebar,
  setSidebarOpen: state.setSidebarOpen,
  setSidebarCollapsed: state.setSidebarCollapsed,
  setActiveSection: state.setActiveSection,
  openModal: state.openModal,
  closeModal: state.closeModal,
  toggleModal: state.toggleModal,
  addNotification: state.addNotification,
  removeNotification: state.removeNotification,
  clearNotifications: state.clearNotifications,
  setLoading: state.setLoading,
  clearLoading: state.clearLoading,
}));

// UI getters
export const useUIGetters = () => useUIStore((state) => ({
  isModalOpen: state.isModalOpen,
  getNotificationsByType: state.getNotificationsByType,
  isLoading: state.isLoading,
  hasNotifications: state.hasNotifications,
}));

// Notification helpers
export const useNotificationHelpers = () => {
  const { addNotification } = useUIActions();

  return {
    showSuccess: (title: string, message: string, action?: Notification['action']) =>
      addNotification({ type: 'success', title, message, action }),

    showError: (title: string, message: string, action?: Notification['action']) =>
      addNotification({ type: 'error', title, message, action }),

    showWarning: (title: string, message: string, action?: Notification['action']) =>
      addNotification({ type: 'warning', title, message, action }),

    showInfo: (title: string, message: string, action?: Notification['action']) =>
      addNotification({ type: 'info', title, message, action }),
  };
};

// Loading helpers
export const useLoadingHelpers = () => {
  const { setLoading } = useUIActions();

  return {
    startLoading: (key: string) => setLoading(key, true),
    stopLoading: (key: string) => setLoading(key, false),
    withLoading: async <T>(key: string, asyncOperation: () => Promise<T>): Promise<T> => {
      setLoading(key, true);
      try {
        const result = await asyncOperation();
        setLoading(key, false);
        return result;
      } catch (error) {
        setLoading(key, false);
        throw error;
      }
    },
  };
};

// Modal helpers
export const useModalHelpers = () => {
  const { openModal, closeModal, toggleModal, isModalOpen } = useUIStore((state) => ({
    openModal: state.openModal,
    closeModal: state.closeModal,
    toggleModal: state.toggleModal,
    isModalOpen: state.isModalOpen,
  }));

  return {
    openModal,
    closeModal,
    toggleModal,
    isModalOpen,

    // Modal state hook for specific modal
    useModal: (modalId: string) => {
      const isOpen = useUIStore((state) => state.isModalOpen(modalId));
      return {
        isOpen,
        open: () => openModal(modalId),
        close: () => closeModal(modalId),
        toggle: () => toggleModal(modalId),
      };
    },
  };
};