import { Component, ErrorInfo, ReactNode } from 'react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  resetKeys?: Array<string | number>;
  resetOnPropsChange?: boolean;
}

interface State {
  hasError: boolean;
  error?: Error;
  errorId: string;
}

class ErrorBoundary extends Component<Props, State> {
  private resetTimeoutId: NodeJS.Timeout | null = null;

  private generateErrorId = (): string => {
    return Math.random().toString(36).substr(2, 9);
  };

  public state: State = {
    hasError: false,
    errorId: this.generateErrorId(),
  };

  public static getDerivedStateFromError(error: Error): Partial<State> {
    // Update state so the next render will show the fallback UI.
    return {
      hasError: true,
      error,
      errorId: Math.random().toString(36).substr(2, 9)
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);

    // Call the custom error handler if provided
    this.props.onError?.(error, errorInfo);

    // You could also log the error to an error reporting service here
    if (process.env.NODE_ENV === 'production') {
      // Log to error reporting service in production
      console.error('Production error:', { error, errorInfo, errorId: this.state.errorId });
    }
  }

  public componentDidUpdate(prevProps: Props) {
    const { resetKeys, resetOnPropsChange } = this.props;
    const { hasError } = this.state;

    // Reset the error boundary when resetKeys change
    if (hasError && resetKeys && prevProps.resetKeys !== resetKeys) {
      if (resetKeys.some((resetKey, idx) => prevProps.resetKeys?.[idx] !== resetKey)) {
        this.handleReset();
      }
    }

    // Reset the error boundary when any props change
    if (hasError && resetOnPropsChange && prevProps !== this.props) {
      this.handleReset();
    }
  }

  public componentWillUnmount() {
    if (this.resetTimeoutId) {
      clearTimeout(this.resetTimeoutId);
    }
  }

  private handleReset = () => {
    this.setState({
      hasError: false,
      error: undefined,
      errorId: this.generateErrorId()
    });
  };

  private handleAutoReset = () => {
    console.log('Auto-resetting error boundary after delay...');
    this.resetTimeoutId = setTimeout(() => {
      this.handleReset();
    }, 5000);
  };

  public render() {
    if (this.state.hasError) {
      // Custom fallback UI
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div className="error-boundary" style={{
          padding: '20px',
          margin: '20px',
          border: '1px solid #ffcdd2',
          borderRadius: '8px',
          backgroundColor: '#ffebee',
          textAlign: 'center'
        }}>
          <div className="error-boundary-content">
            <div style={{ fontSize: '48px', marginBottom: '16px' }}>⚠️</div>
            <h2 style={{ color: '#c62828', marginBottom: '16px' }}>Something went wrong</h2>
            <p style={{ color: '#666', marginBottom: '20px' }}>
              We've encountered an unexpected error. This has been reported automatically.
            </p>
            <p style={{ fontSize: '12px', color: '#999', marginBottom: '20px' }}>
              Error ID: {this.state.errorId}
            </p>

            {process.env.NODE_ENV === 'development' && (
              <details style={{
                marginBottom: '20px',
                textAlign: 'left',
                backgroundColor: '#fff',
                padding: '10px',
                border: '1px solid #ddd',
                borderRadius: '4px'
              }}>
                <summary style={{ cursor: 'pointer', fontWeight: 'bold', color: '#333' }}>
                  Error details (Development)
                </summary>
                <pre style={{
                  whiteSpace: 'pre-wrap',
                  fontSize: '12px',
                  color: '#d32f2f',
                  marginTop: '10px'
                }}>
                  {this.state.error?.message}
                </pre>
                <pre style={{
                  whiteSpace: 'pre-wrap',
                  fontSize: '10px',
                  color: '#666',
                  marginTop: '10px',
                  maxHeight: '200px',
                  overflow: 'auto'
                }}>
                  {this.state.error?.stack}
                </pre>
              </details>
            )}

            <div className="error-boundary-actions" style={{ display: 'flex', gap: '10px', justifyContent: 'center' }}>
              <button
                onClick={this.handleReset}
                style={{
                  padding: '10px 20px',
                  backgroundColor: '#1976d2',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer'
                }}
              >
                Try again
              </button>
              <button
                onClick={() => window.location.reload()}
                style={{
                  padding: '10px 20px',
                  backgroundColor: '#f57c00',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer'
                }}
              >
                Reload page
              </button>
            </div>

            <p style={{ fontSize: '12px', color: '#666', marginTop: '16px' }}>
              This component will automatically retry in a few seconds...
            </p>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;