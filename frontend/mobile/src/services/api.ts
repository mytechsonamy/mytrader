import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform } from 'react-native';
import { API_BASE_URL, AUTH_BASE_URL, WS_BASE_URL } from '../config';
import { User, UserSession, StrategyConfig, BacktestResult } from '../types';

// API base URL is configured via app.json extra; falls back to localhost

const STORAGE_KEYS = {
  SESSION_TOKEN: 'session_token',
  USER_DATA: 'user_data',
  SYMBOLS_CONFIG: 'symbols_config_v1',
};

class ApiService {
  private sessionToken: string | null = null;

  private buildCandidates(baseUrl: string, path: string): string[] {
    const base = baseUrl.replace(/\/$/, '');
    const hasApiSuffix = base.endsWith('/api');
    const trimmed = hasApiSuffix ? base.slice(0, -4) : base; // remove /api if present
    const ensuredApiBase = hasApiSuffix ? base : `${trimmed}/api`; // add /api if missing
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    const withV1 = cleanPath.startsWith('/v1/') ? cleanPath : `/v1${cleanPath}`;

    const candidates = [
      // As-is
      `${base}${cleanPath}`,
      `${base}${withV1}`,
      // Without /api (if present)
      `${trimmed}${cleanPath}`,
      `${trimmed}${withV1}`,
      // With /api (if missing)
      `${ensuredApiBase}${cleanPath}`,
      `${ensuredApiBase}${withV1}`,
    ];
    // De-duplicate while preserving order
    return Array.from(new Set(candidates));
  }

  private async postJsonWithFallback<T>(path: string, body: any, baseUrl: string = API_BASE_URL): Promise<Response> {
    const urls = this.buildCandidates(baseUrl, path);
    let lastResponse: Response | null = null;
    for (const url of urls) {
      console.log(`POST try: ${url}`);
      const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (res.status === 404) {
        console.warn(`404 on ${url}, trying next candidate (if any)`);
        lastResponse = res;
        continue;
      }
      return res;
    }
    // If all 404, return last 404 so caller surfaces error properly
    if (lastResponse) return lastResponse;
    // Should not reach here normally
    throw new Error('No response from any candidate URL');
  }

  async initialize() {
    this.sessionToken = await AsyncStorage.getItem(STORAGE_KEYS.SESSION_TOKEN);
  }

  private async getHeaders(): Promise<HeadersInit> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (this.sessionToken) {
      headers['Authorization'] = `Bearer ${this.sessionToken}`;
    }

    return headers;
  }

  public async getAuthHeaders(): Promise<HeadersInit> {
    return this.getHeaders();
  }

  private async handleResponse<T>(response: Response): Promise<T> {
    // Clone the response so we can read it multiple times if needed
    const responseClone = response.clone();
    
    if (!response.ok) {
      let message: string = `HTTP Error: ${response.status}`;
      let code: string | undefined;
      try {
        const errorData = await responseClone.json();
        const detail = errorData?.detail || errorData;
        if (typeof detail === 'object') {
          message = detail.message || message;
          code = detail.code;
        } else if (typeof detail === 'string') {
          message = detail;
        }
      } catch {
        try {
          const txt = await responseClone.text();
          if (txt) message = txt;
        } catch {
          // If both json and text fail, use the default message
        }
      }
      const err: any = new Error(message);
      err.code = code;
      err.status = response.status;
      throw err;
    }
    return await response.json();
  }

  // Authentication APIs
  async login(email: string, password: string): Promise<UserSession> {
    console.log('Login attempt:', email);
    
    try {
      const response = await this.postJsonWithFallback<UserSession>('/auth/login', { Email: email, Password: password }, AUTH_BASE_URL);

      const session = await this.handleResponse<UserSession>(response);
      
      // Store session data
      this.sessionToken = session.accessToken;
      await AsyncStorage.setItem(STORAGE_KEYS.SESSION_TOKEN, session.accessToken);
      await AsyncStorage.setItem(STORAGE_KEYS.USER_DATA, JSON.stringify(session.user));
      
      console.log('Login successful');
      return session;
    } catch (error) {
      console.error('Login error:', error);
      throw error instanceof Error ? error : new Error('Giriş sırasında bir hata oluştu.');
    }
  }

  async register(userData: {
    email: string;
    password: string;
    first_name: string;
    last_name: string;
    phone?: string;
    telegram_id?: string;
  }): Promise<{ success: boolean; message: string }> {
    console.log('Register attempt:', userData.email);
    
    try {
      const response = await this.postJsonWithFallback('/auth/register', {
        email: userData.email,
        password: userData.password,
        FirstName: userData.first_name,
        LastName: userData.last_name,
        Phone: userData.phone || '',
      }, AUTH_BASE_URL);
      const result = await this.handleResponse<{ success: boolean; message: string }>(response);
      return result;
    } catch (error) {
      console.error('Registration error:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Kayıt sırasında bir hata oluştu.'
      };
    }
  }

  async verifyEmail(email: string, verificationCode: string): Promise<{ success: boolean; message: string }> {
    try {
      const response = await this.postJsonWithFallback('/auth/verify-email', { Email: email, VerificationCode: verificationCode }, AUTH_BASE_URL);

      const result = await this.handleResponse<{ success: boolean; message: string }>(response);
      return result;
    } catch (error) {
      console.error('Verify email error:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Doğrulama sırasında bir hata oluştu.'
      };
    }
  }

  async resendVerificationCode(email: string): Promise<{ success: boolean; message: string }> {
    try {
      const response = await this.postJsonWithFallback('/auth/resend-verification', { Email: email }, AUTH_BASE_URL);

      const result = await this.handleResponse<{ success: boolean; message: string }>(response);
      return result;
    } catch (error) {
      console.error('Resend verification error:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Kod gönderilirken bir hata oluştu.'
      };
    }
  }

  async logout(): Promise<void> {
    if (this.sessionToken) {
      try {
        await this.postJsonWithFallback('/auth/logout', {}, AUTH_BASE_URL);
      } catch (error) {
        console.warn('Logout API call failed:', error);
      }
    }

    // Clear local storage
    this.sessionToken = null;
    await AsyncStorage.multiRemove([STORAGE_KEYS.SESSION_TOKEN, STORAGE_KEYS.USER_DATA]);
  }

  // Profile update
  async updateProfile(partial: {
    first_name?: string;
    last_name?: string;
    phone?: string;
    telegram_id?: string;
  }): Promise<{ success: boolean; message: string }> {
    const payload = {
      Name: partial.first_name && partial.last_name ? `${partial.first_name} ${partial.last_name}` : undefined,
      Phone: partial.phone,
      Country: undefined // Add if needed later
    };
    const response = await this.postJsonWithFallback('/auth/me', payload, AUTH_BASE_URL);
    return await this.handleResponse(response);
  }

  // Password reset flow
  async requestPasswordReset(email: string): Promise<{ success: boolean; message: string }> {
    const response = await this.postJsonWithFallback('/auth/request-password-reset', { Email: email }, AUTH_BASE_URL);
    return await this.handleResponse(response);
  }

  async verifyPasswordReset(email: string, code: string): Promise<{ success: boolean; message: string }> {
    const response = await this.postJsonWithFallback('/auth/verify-password-reset', { Email: email, VerificationCode: code }, AUTH_BASE_URL);
    return await this.handleResponse<{ success: boolean; message: string }>(response);
  }

  async resetPassword(email: string, newPassword: string): Promise<{ success: boolean; message: string }> {
    const response = await this.postJsonWithFallback('/auth/reset-password', { Email: email, NewPassword: newPassword }, AUTH_BASE_URL);
    return await this.handleResponse<{ success: boolean; message: string }>(response);
  }

  async getCurrentUser(): Promise<User | null> {
    const userData = await AsyncStorage.getItem(STORAGE_KEYS.USER_DATA);
    if (!userData || !this.sessionToken) {
      return null;
    }

    try {
      // Verify session is still valid
      const response = await fetch(`${AUTH_BASE_URL.replace(/\/$/, '')}/auth/me`, {
        headers: await this.getHeaders(),
      });

      if (response.ok) {
        return JSON.parse(userData);
      } else {
        // Session expired, clear storage
        await this.logout();
        return null;
      }
    } catch (error) {
      console.error('Error verifying session:', error);
      return null;
    }
  }

  // Strategy APIs
  async createStrategy(strategy: StrategyConfig): Promise<{ success: boolean; message: string; strategy_id?: string }> {
    const response = await fetch(`${API_BASE_URL}/v1/strategies/create`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify(strategy),
    });

    return await this.handleResponse(response);
  }

  async testStrategy(strategyId: string, symbol: string): Promise<BacktestResult> {
    const response = await fetch(`${API_BASE_URL}/v1/strategies/${strategyId}/test`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify({ symbol }),
    });

    return await this.handleResponse(response);
  }

  async getUserStrategies(): Promise<{ success: boolean; data?: StrategyConfig[]; message?: string }> {
    const response = await fetch(`${API_BASE_URL}/v1/strategies/my-strategies`, {
      headers: await this.getHeaders(),
    });

    try {
      const data = await this.handleResponse<StrategyConfig[]>(response);
      return { success: true, data };
    } catch (e: any) {
      return { success: false, message: e?.message || 'Failed to load strategies' };
    }
  }

  async activateStrategy(strategyId: string): Promise<{ success: boolean; message: string }> {
    const response = await fetch(`${API_BASE_URL}/v1/strategies/${strategyId}/activate`, {
      method: 'POST',
      headers: await this.getHeaders(),
    });

    return await this.handleResponse(response);
  }

  // Get real-time market data from dashboard
  async getMarketData(): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/symbols`);
    return await this.handleResponse(response);
  }

  // Get current price for a specific symbol from dashboard
  async getCurrentPrice(symbol: string): Promise<{ price: number; change: number } | null> {
    try {
      // First try to get from dashboard WebSocket data via REST endpoint
      const marketResponse = await fetch(`${API_BASE_URL}/signals/${symbol.replace('USDT', '')}`);
      if (marketResponse.ok) {
        const data = await marketResponse.json();
        // If we have recent signal data with price info
        if (data.signals && data.signals.length > 0) {
          const latestSignal = data.signals[0];
          return {
            price: latestSignal.price || 0,
            change: latestSignal.change || 0
          };
        }
      }
      
      // Fallback: try to connect to WebSocket for live data
      // This is a simplified approach - in production you'd maintain a persistent connection
      return null;
    } catch (error) {
      console.warn('Failed to get current price from dashboard:', error);
      return null;
    }
  }

  // Dashboard APIs
  async getSymbolData(): Promise<{ [symbol: string]: any }> {
    const response = await fetch(`${API_BASE_URL}/dashboard/symbols`, {
      headers: await this.getHeaders(),
    });

    return await this.handleResponse(response);
  }

  async getSymbolsConfig(): Promise<{ symbols: Record<string, any>; interval: string }> {
    const response = await fetch(`${API_BASE_URL}/MockMarket/symbols`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getSymbolsCached(maxAgeMs: number = 5 * 60 * 1000): Promise<{ symbols: Record<string, any>; interval: string } | null> {
    try {
      const cached = await AsyncStorage.getItem(STORAGE_KEYS.SYMBOLS_CONFIG);
      if (cached) {
        const parsed = JSON.parse(cached);
        const ts = parsed?.__ts as number | undefined;
        if (ts && Date.now() - ts < maxAgeMs && parsed?.symbols) {
          return { symbols: parsed.symbols, interval: parsed.interval };
        }
      }
    } catch {}

    try {
      const fresh = await this.getSymbolsConfig();
      await AsyncStorage.setItem(
        STORAGE_KEYS.SYMBOLS_CONFIG,
        JSON.stringify({ ...fresh, __ts: Date.now() })
      );
      return fresh;
    } catch (e) {
      console.warn('Failed to fetch symbols config:', e);
      return null;
    }
  }

  // User Layout Preferences APIs
  async getUserLayoutPreferences(): Promise<{ asset_order?: string[] }> {
    const response = await fetch(`${API_BASE_URL}/v1/user/layout-preferences`, {
      headers: await this.getHeaders(),
    });

    return await this.handleResponse(response);
  }

  async saveUserLayoutPreferences(preferences: { asset_order: string[] }): Promise<{ success: boolean }> {
    const response = await fetch(`${API_BASE_URL}/v1/user/layout-preferences`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify(preferences),
    });

    return await this.handleResponse(response);
  }

  // WebSocket connection
  createWebSocketConnection(): WebSocket {
    // Get appropriate WebSocket URL based on platform
    const base = WS_BASE_URL;
    const url = this.sessionToken ? `${base}?token=${encodeURIComponent(this.sessionToken)}` : base;
    console.log('Connecting to WebSocket:', url);
    return new WebSocket(url);
  }

  // Gamification APIs
  async getUserAchievements(): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/gamification/achievements`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getLeaderboard(): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/gamification/leaderboard`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getUserLevel(): Promise<{ level: number; points: number; nextLevelPoints: number }> {
    const response = await fetch(`${API_BASE_URL}/gamification/level`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  // Alerts APIs
  async getPriceAlerts(): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/alerts`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async createPriceAlert(alert: any): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/alerts`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify(alert),
    });
    return await this.handleResponse(response);
  }

  async updatePriceAlert(id: string, alert: any): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/alerts/${id}`, {
      method: 'PUT',
      headers: await this.getHeaders(),
      body: JSON.stringify(alert),
    });
    return await this.handleResponse(response);
  }

  async deletePriceAlert(id: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/alerts/${id}`, {
      method: 'DELETE',
      headers: await this.getHeaders(),
    });
    await this.handleResponse(response);
  }

  async getNotificationHistory(): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/notifications/history`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async markNotificationAsRead(id: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/notifications/${id}/read`, {
      method: 'POST',
      headers: await this.getHeaders(),
    });
    await this.handleResponse(response);
  }

  // Education APIs
  async getLearningPaths(): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/education/paths`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getLearningModules(category?: string): Promise<any[]> {
    const endpoint = category ? 
      `${API_BASE_URL}/education/modules?category=${encodeURIComponent(category)}` : 
      `${API_BASE_URL}/education/modules`;
    const response = await fetch(endpoint, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getModule(id: string): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/education/modules/${id}`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async markModuleComplete(id: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/education/modules/${id}/complete`, {
      method: 'POST',
      headers: await this.getHeaders(),
    });
    await this.handleResponse(response);
  }

  async getQuiz(moduleId: string): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/education/modules/${moduleId}/quiz`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async submitQuiz(quizId: string, answers: any[]): Promise<{ score: number; passed: boolean }> {
    const response = await fetch(`${API_BASE_URL}/education/quizzes/${quizId}/submit`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify({ answers }),
    });
    return await this.handleResponse(response);
  }
}

export const apiService = new ApiService();
