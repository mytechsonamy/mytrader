import React, { createContext, useContext, ReactNode } from 'react';
import { useErrorNotification } from '../components/ErrorNotification';

interface ErrorNotificationContextType {
  showError: (message: string, type?: 'error' | 'warning' | 'info') => void;
  hideError: () => void;
}

const ErrorNotificationContext = createContext<ErrorNotificationContextType | undefined>(undefined);

export const useErrorNotifications = () => {
  const context = useContext(ErrorNotificationContext);
  if (!context) {
    throw new Error('useErrorNotifications must be used within an ErrorNotificationProvider');
  }
  return context;
};

interface ErrorNotificationProviderProps {
  children: ReactNode;
}

export const ErrorNotificationProvider: React.FC<ErrorNotificationProviderProps> = ({ children }) => {
  const { showError, hideError, NotificationComponent } = useErrorNotification();

  const value: ErrorNotificationContextType = {
    showError,
    hideError,
  };

  return (
    <ErrorNotificationContext.Provider value={value}>
      {children}
      <NotificationComponent />
    </ErrorNotificationContext.Provider>
  );
};

export default ErrorNotificationContext;