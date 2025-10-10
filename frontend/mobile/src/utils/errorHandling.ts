import { Alert } from 'react-native';

export interface ErrorContext {
  screen?: string;
  action?: string;
  timestamp?: Date;
  userId?: string;
  additional?: Record<string, any>;
}

export class AppError extends Error {
  public code?: string;
  public status?: number;
  public context?: ErrorContext;
  public userFriendly: boolean;

  constructor(
    message: string,
    code?: string,
    status?: number,
    context?: ErrorContext,
    userFriendly: boolean = false
  ) {
    super(message);
    this.name = 'AppError';
    this.code = code;
    this.status = status;
    this.context = context;
    this.userFriendly = userFriendly;
  }
}

export class NetworkError extends AppError {
  constructor(message: string = 'Ağ bağlantısı sorunu', context?: ErrorContext) {
    super(message, 'NETWORK_ERROR', 0, context, true);
    this.name = 'NetworkError';
  }
}

export class ValidationError extends AppError {
  constructor(message: string, context?: ErrorContext) {
    super(message, 'VALIDATION_ERROR', 400, context, true);
    this.name = 'ValidationError';
  }
}

export class AuthenticationError extends AppError {
  constructor(message: string = 'Kimlik doğrulama hatası', context?: ErrorContext) {
    super(message, 'AUTH_ERROR', 401, context, true);
    this.name = 'AuthenticationError';
  }
}

export class DataError extends AppError {
  constructor(message: string = 'Veri işleme hatası', context?: ErrorContext) {
    super(message, 'DATA_ERROR', 500, context, true);
    this.name = 'DataError';
  }
}

// Error handling utilities
export class ErrorHandler {
  private static crashReportingEnabled = false;

  static enableCrashReporting() {
    this.crashReportingEnabled = true;
  }

  static handleError(error: Error, context?: ErrorContext, showAlert: boolean = true): void {
    const timestamp = new Date();
    const errorInfo = {
      message: error.message,
      name: error.name,
      stack: error.stack,
      context: { ...context, timestamp },
    };

    // Log to console in development
    if (__DEV__) {
      console.error('🚨 Application Error:', errorInfo);
    }

    // Log to crash reporting service in production
    if (this.crashReportingEnabled && !__DEV__) {
      this.logToCrashlytics(error, context);
    }

    // Show user-friendly alert if requested
    if (showAlert) {
      this.showUserFriendlyAlert(error);
    }
  }

  private static logToCrashlytics(error: Error, context?: ErrorContext): void {
    // TODO: Integrate with Firebase Crashlytics or similar service
    console.log('Would log to Crashlytics:', { error, context });
  }

  private static showUserFriendlyAlert(error: Error): void {
    let title = 'Hata';
    let message = 'Beklenmeyen bir hata oluştu.';

    if (error instanceof AppError && error.userFriendly) {
      message = error.message;
    } else if (error instanceof NetworkError) {
      title = 'Bağlantı Hatası';
      message = error.message || 'İnternet bağlantınızı kontrol edin.';
    } else if (error instanceof AuthenticationError) {
      title = 'Giriş Hatası';
      message = error.message || 'Lütfen tekrar giriş yapmayı deneyin.';
    } else if (error instanceof ValidationError) {
      title = 'Geçersiz Veri';
      message = error.message;
    } else if (error instanceof DataError) {
      title = 'Veri Hatası';
      message = 'Veriler işlenirken bir sorun oluştu.';
    }

    Alert.alert(title, message, [{ text: 'Tamam', style: 'default' }]);
  }

  static handleNetworkError(error: any, context?: ErrorContext): NetworkError {
    let message = 'Ağ bağlantısı sorunu';

    if (error?.message) {
      const errorMessage = error.message.toLowerCase();

      if (errorMessage.includes('network') || errorMessage.includes('fetch')) {
        message = 'İnternet bağlantınızı kontrol edin';
      } else if (errorMessage.includes('timeout')) {
        message = 'Bağlantı zaman aşımı. Lütfen tekrar deneyin';
      } else if (errorMessage.includes('err_name_not_resolved')) {
        message = 'Sunucu adı çözümlenemedi. DNS ayarlarınızı kontrol edin';
      } else if (errorMessage.includes('err_connection_refused')) {
        message = 'Sunucuya bağlanılamıyor. Lütfen daha sonra deneyin';
      }
    }

    const networkError = new NetworkError(message, context);
    this.handleError(networkError, context, true);
    return networkError;
  }

  static handleAPIError(response: Response, context?: ErrorContext): AppError {
    let message = `HTTP ${response.status}: ${response.statusText}`;
    let code = `HTTP_${response.status}`;

    switch (response.status) {
      case 400:
        message = 'Geçersiz istek';
        break;
      case 401:
        message = 'Kimlik doğrulama gerekli';
        return new AuthenticationError(message, context);
      case 403:
        message = 'Bu işlem için yetkiniz yok';
        break;
      case 404:
        message = 'İstenen kaynak bulunamadı';
        break;
      case 409:
        message = 'Çakışan istek. Lütfen tekrar deneyin';
        break;
      case 429:
        message = 'Çok fazla istek. Lütfen biraz bekleyin';
        break;
      case 500:
      case 502:
      case 503:
      case 504:
        message = 'Sunucu hatası. Lütfen daha sonra deneyin';
        break;
    }

    const error = new AppError(message, code, response.status, context, true);
    this.handleError(error, context, true);
    return error;
  }

  static handleWebSocketError(error: Event, context?: ErrorContext): NetworkError {
    const message = 'WebSocket bağlantısı kesildi. Canlı veriler gecikmeli olabilir';
    const networkError = new NetworkError(message, {
      ...context,
      action: 'websocket_error',
    });

    this.handleError(networkError, context, false); // Don't show alert for WebSocket errors
    return networkError;
  }

  static handleDataProcessingError(error: any, context?: ErrorContext): DataError {
    let message = 'Veri işlenirken hata oluştu';

    if (error?.message) {
      if (error.message.includes('JSON')) {
        message = 'Geçersiz veri formatı';
      } else if (error.message.includes('undefined') || error.message.includes('null')) {
        message = 'Eksik veri';
      } else if (error.message.includes('array') || error.message.includes('slice')) {
        message = 'Liste verileri işlenirken hata';
      }
    }

    const dataError = new DataError(message, context);
    this.handleError(dataError, context, false); // Log but don't show alert
    return dataError;
  }

  static wrapAsyncOperation<T>(
    operation: () => Promise<T>,
    context?: ErrorContext
  ): Promise<T> {
    return operation().catch((error) => {
      if (error instanceof AppError) {
        throw error;
      }

      // Determine error type and handle accordingly
      if (error?.name === 'TypeError' && error?.message?.includes('fetch')) {
        throw this.handleNetworkError(error, context);
      } else if (error?.name === 'SyntaxError' && error?.message?.includes('JSON')) {
        throw this.handleDataProcessingError(error, context);
      } else {
        throw new AppError(
          error?.message || 'Beklenmeyen hata',
          'UNKNOWN_ERROR',
          undefined,
          context,
          false
        );
      }
    });
  }

  static safeExecute<T>(
    operation: () => T,
    fallback: T,
    context?: ErrorContext
  ): T {
    try {
      return operation();
    } catch (error) {
      this.handleError(error as Error, context, false);
      return fallback;
    }
  }

  static createRetryWrapper<T>(
    operation: () => Promise<T>,
    maxRetries: number = 3,
    retryDelay: number = 1000,
    context?: ErrorContext
  ): Promise<T> {
    return new Promise((resolve, reject) => {
      let attempt = 0;

      const executeAttempt = async () => {
        attempt++;

        try {
          const result = await operation();
          resolve(result);
        } catch (error) {
          if (attempt >= maxRetries) {
            this.handleError(error as Error, {
              ...context,
              additional: { attempts: attempt }
            });
            reject(error);
            return;
          }

          // Exponential backoff
          const delay = retryDelay * Math.pow(2, attempt - 1);
          setTimeout(executeAttempt, delay);
        }
      };

      executeAttempt();
    });
  }
}

// Utility functions for common error handling patterns
export const withErrorHandling = <T extends any[], R>(
  fn: (...args: T) => R,
  context?: ErrorContext
) => {
  return (...args: T): R => {
    try {
      return fn(...args);
    } catch (error) {
      ErrorHandler.handleError(error as Error, context);
      throw error;
    }
  };
};

export const withAsyncErrorHandling = <T extends any[], R>(
  fn: (...args: T) => Promise<R>,
  context?: ErrorContext
) => {
  return async (...args: T): Promise<R> => {
    return ErrorHandler.wrapAsyncOperation(() => fn(...args), context);
  };
};

// Global error handler for unhandled promise rejections
if (typeof global !== 'undefined') {
  global.addEventListener?.('unhandledrejection', (event) => {
    ErrorHandler.handleError(
      new Error(`Unhandled Promise Rejection: ${event.reason}`),
      { action: 'unhandled_promise_rejection' },
      false
    );
  });
}