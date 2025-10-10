import React, { Component, ErrorInfo, ReactNode } from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: (error: Error, errorInfo: ErrorInfo | null, retry: () => void) => ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  isolate?: boolean;
}

// Styles defined first to ensure they're available for all components
const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f8fafc',
    padding: 20,
  },
  isolatedContainer: {
    flex: 0,
    minHeight: 150,
    backgroundColor: 'transparent',
  },
  errorCard: {
    backgroundColor: 'white',
    borderRadius: 16,
    padding: 24,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 5,
    maxWidth: 300,
    alignItems: 'center',
  },
  errorIcon: {
    fontSize: 48,
    marginBottom: 16,
  },
  errorTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 8,
    textAlign: 'center',
  },
  errorMessage: {
    fontSize: 14,
    color: '#6b7280',
    textAlign: 'center',
    marginBottom: 20,
    lineHeight: 20,
  },
  debugInfo: {
    backgroundColor: '#f3f4f6',
    borderRadius: 8,
    padding: 12,
    marginBottom: 20,
    width: '100%',
  },
  debugTitle: {
    fontSize: 12,
    fontWeight: '600',
    color: '#374151',
    marginBottom: 8,
  },
  debugText: {
    fontSize: 10,
    color: '#6b7280',
    fontFamily: 'monospace',
  },
  retryButton: {
    backgroundColor: '#667eea',
    borderRadius: 8,
    paddingHorizontal: 20,
    paddingVertical: 12,
  },
  retryButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  // Dashboard-specific styles
  dashboardErrorContainer: {
    backgroundColor: 'rgba(255,255,255,0.95)',
    borderRadius: 16,
    padding: 32,
    margin: 20,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 5,
  },
  dashboardErrorIcon: {
    fontSize: 64,
    marginBottom: 16,
  },
  dashboardErrorTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 12,
  },
  dashboardErrorMessage: {
    fontSize: 16,
    color: '#6b7280',
    textAlign: 'center',
    marginBottom: 24,
    lineHeight: 22,
  },
  dashboardRetryButton: {
    backgroundColor: '#667eea',
    borderRadius: 12,
    paddingHorizontal: 24,
    paddingVertical: 14,
  },
  dashboardRetryButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  // Section-specific styles
  sectionErrorContainer: {
    backgroundColor: '#fef2f2',
    borderRadius: 12,
    padding: 20,
    margin: 8,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#fecaca',
  },
  sectionErrorIcon: {
    fontSize: 32,
    marginBottom: 8,
  },
  sectionErrorText: {
    fontSize: 14,
    color: '#dc2626',
    textAlign: 'center',
    marginBottom: 12,
  },
  sectionRetryButton: {
    backgroundColor: '#dc2626',
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  sectionRetryText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
  },
});

class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return {
      hasError: true,
      error,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);

    this.setState({
      error,
      errorInfo,
    });

    // Call the onError callback if provided
    this.props.onError?.(error, errorInfo);

    // Log to crash reporting service in production
    if (__DEV__) {
      console.group('🚨 Error Boundary');
      console.error('Error:', error);
      console.error('Error Info:', errorInfo);
      console.error('Component Stack:', errorInfo.componentStack);
      console.groupEnd();
    }
  }

  retry = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  render() {
    if (this.state.hasError && this.state.error) {
      // If a custom fallback is provided, use it
      if (this.props.fallback) {
        return this.props.fallback(this.state.error, this.state.errorInfo, this.retry);
      }

      // Default error UI
      return (
        <View style={[
          styles.container,
          this.props.isolate && styles.isolatedContainer
        ]}>
          <View style={styles.errorCard}>
            <Text style={styles.errorIcon}>⚠️</Text>
            <Text style={styles.errorTitle}>Bir şeyler ters gitti</Text>
            <Text style={styles.errorMessage}>
              {this.state.error.message || 'Beklenmeyen bir hata oluştu'}
            </Text>

            {__DEV__ && (
              <View style={styles.debugInfo}>
                <Text style={styles.debugTitle}>Geliştirici Bilgisi:</Text>
                <Text style={styles.debugText} numberOfLines={5}>
                  {this.state.error.stack}
                </Text>
              </View>
            )}

            <TouchableOpacity style={styles.retryButton} onPress={this.retry}>
              <Text style={styles.retryButtonText}>🔄 Tekrar Dene</Text>
            </TouchableOpacity>
          </View>
        </View>
      );
    }

    return this.props.children;
  }
}

// Higher-order component for easier usage
export function withErrorBoundary<P extends object>(
  WrappedComponent: React.ComponentType<P>,
  errorBoundaryConfig?: Omit<ErrorBoundaryProps, 'children'>
) {
  const WithErrorBoundaryComponent = (props: P) => (
    <ErrorBoundary {...errorBoundaryConfig}>
      <WrappedComponent {...props} />
    </ErrorBoundary>
  );

  WithErrorBoundaryComponent.displayName =
    `withErrorBoundary(${WrappedComponent.displayName || WrappedComponent.name})`;

  return WithErrorBoundaryComponent;
}

// Specialized error boundaries for different sections
export const DashboardErrorBoundary: React.FC<{ children: ReactNode }> = ({ children }) => {
  // Create a local reference to styles to ensure it's captured in the closure
  const errorStyles = styles;

  return (
    <ErrorBoundary
      onError={(error, errorInfo) => {
        // Log dashboard-specific errors
        console.error('Dashboard Error:', { error, errorInfo });
      }}
      fallback={(error, errorInfo, retry) => (
        <View style={errorStyles.dashboardErrorContainer}>
          <Text style={errorStyles.dashboardErrorIcon}>📊</Text>
          <Text style={errorStyles.dashboardErrorTitle}>Dashboard Hatası</Text>
          <Text style={errorStyles.dashboardErrorMessage}>
            Dashboard yüklenirken bir sorun oluştu. Lütfen tekrar deneyin.
          </Text>
          <TouchableOpacity style={errorStyles.dashboardRetryButton} onPress={retry}>
            <Text style={errorStyles.dashboardRetryButtonText}>Yenile</Text>
          </TouchableOpacity>
        </View>
      )}
    >
      {children}
    </ErrorBoundary>
  );
};

export const AccordionErrorBoundary: React.FC<{
  children: ReactNode;
  sectionName: string;
}> = ({ children, sectionName }) => {
  // Create a local reference to styles to ensure it's captured in the closure
  const errorStyles = styles;

  return (
    <ErrorBoundary
      isolate={true}
      fallback={(error, errorInfo, retry) => (
        <View style={errorStyles.sectionErrorContainer}>
          <Text style={errorStyles.sectionErrorIcon}>⚠️</Text>
          <Text style={errorStyles.sectionErrorText}>
            {sectionName} bölümü yüklenemedi
          </Text>
          <TouchableOpacity style={errorStyles.sectionRetryButton} onPress={retry}>
            <Text style={errorStyles.sectionRetryText}>Tekrar Dene</Text>
          </TouchableOpacity>
        </View>
      )}
      onError={(error) => {
        console.error(`Section Error (${sectionName}):`, error);
      }}
    >
      {children}
    </ErrorBoundary>
  );
};

export default ErrorBoundary;