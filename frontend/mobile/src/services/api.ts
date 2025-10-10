import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform } from 'react-native';
import { API_BASE_URL, AUTH_BASE_URL, WS_BASE_URL } from '../config';
import {
  User, UserSession, StrategyConfig, BacktestResult,
  Portfolio, Position, Transaction, PortfolioAnalytics, ExportRequest, ExportResponse,
  AssetClassDto, MarketDto, EnhancedSymbolDto, UnifiedMarketDataDto, MarketStatusDto,
  MarketOverviewDto, TopMoversDto, PagedResponse, ApiResponse, RequestConfig,
  LeaderboardEntry, UserRanking, CompetitionStats, NewsItem
} from '../types';
import { SymbolCache } from './symbolCache';

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
    const cleanPath = path.startsWith('/') ? path : `/${path}`;

    // Check if base already has /v1 suffix (this is the bug we're fixing)
    const hasV1Suffix = base.endsWith('/v1');
    const hasApiSuffix = base.endsWith('/api') || hasV1Suffix;

    // Get the root URL without any API suffix
    const rootUrl = hasV1Suffix
      ? base.slice(0, -3) // Remove /v1
      : hasApiSuffix
        ? base.slice(0, -4) // Remove /api
        : base;

    // Build standardized base URLs
    const apiBase = `${rootUrl}/api`;
    const v1Base = `${rootUrl}/api/v1`;

    // Ensure path has proper /v1 prefix when needed
    const withV1Path = cleanPath.startsWith('/v1/') ? cleanPath : `/v1${cleanPath}`;
    const withoutV1Path = cleanPath.startsWith('/v1/') ? cleanPath.substring(3) : cleanPath;

    const candidates = [
      // Primary: /api/v1/path (most likely to work with current backend)
      `${v1Base}${withoutV1Path}`,
      // Fallback: /api/path (without version)
      `${apiBase}${withoutV1Path}`,
      // Legacy: direct path on root
      `${rootUrl}${withV1Path}`,
      `${rootUrl}${withoutV1Path}`,
      // Original base + path (as-is)
      `${base}${withoutV1Path}`,
    ];

    // De-duplicate while preserving order
    return Array.from(new Set(candidates));
  }

  private async postJsonWithFallback<T>(path: string, body: any, baseUrl: string = API_BASE_URL): Promise<Response> {
    const urls = this.buildCandidates(baseUrl, path);
    console.log(`POST ${path} - URL candidates:`, urls);

    let lastResponse: Response | null = null;
    for (const url of urls) {
      console.log(`POST attempting: ${url}`);
      try {
        const headers = await this.getHeaders();
        const res = await fetch(url, {
          method: 'POST',
          headers: headers,
          body: JSON.stringify(body),
        });

        console.log(`POST ${url} -> ${res.status} ${res.statusText}`);

        if (res.status === 404) {
          console.warn(`404 on ${url}, trying next candidate (if any)`);
          lastResponse = res;
          continue;
        }

        console.log(`POST success on: ${url}`);
        return res;
      } catch (error) {
        console.error(`POST failed on ${url}:`, error);
        if (urls.indexOf(url) === urls.length - 1) {
          // This was the last URL, throw the error
          throw error;
        }
        // Try next URL
        continue;
      }
    }

    // If all 404, return last 404 so caller surfaces error properly
    if (lastResponse) {
      console.error(`All POST attempts failed with 404. Last response from: ${urls[urls.length - 1]}`);
      return lastResponse;
    }

    // Should not reach here normally
    throw new Error('No response from any candidate URL');
  }

  async initialize() {
    this.sessionToken = await AsyncStorage.getItem(STORAGE_KEYS.SESSION_TOKEN);
  }

  private async getHeaders(): Promise<HeadersInit> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      'X-Client-Type': 'mobile',
      'User-Agent': `mytrader-mobile/${Platform.OS}`,
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

    // CRITICAL FIX: Handle ApiResponse<T> wrapper format from backend
    const jsonData = await response.json();

    // Check if this is an ApiResponse<T> wrapper (has data property)
    if (jsonData && typeof jsonData === 'object' && 'data' in jsonData) {
      // Backend returned ApiResponse<T> format - unwrap the data
      console.log('Unwrapping ApiResponse<T> format, data:', jsonData.data);
      return jsonData.data as T;
    }

    // Backend returned direct T object - return as-is for backward compatibility
    console.log('Using direct response format, data:', jsonData);
    return jsonData as T;
  }

  // Authentication APIs
  async login(email: string, password: string): Promise<UserSession> {
    console.log('Login attempt for:', email);
    console.log('Using API_BASE_URL:', API_BASE_URL);

    try {
      const loginPayload = { email: email, password: password };
      console.log('Login payload:', JSON.stringify(loginPayload, null, 2));

      const response = await this.postJsonWithFallback<UserSession>('/auth/login', loginPayload, API_BASE_URL);
      console.log('Login response status:', response.status, response.statusText);

      const session = await this.handleResponse<UserSession>(response);

      // Store session data
      this.sessionToken = session.accessToken;
      await AsyncStorage.setItem(STORAGE_KEYS.SESSION_TOKEN, session.accessToken);
      await AsyncStorage.setItem(STORAGE_KEYS.USER_DATA, JSON.stringify(session.user));

      console.log('Login successful for user:', session.user?.email || session.user?.first_name);
      return session;
    } catch (error: any) {
      console.error('Login error details:', {
        message: error?.message || 'Unknown error',
        status: error?.status || 'No status',
        code: error?.code || 'No code',
        stack: error?.stack?.substring(0, 200) || 'No stack'
      });

      // Provide more specific error messages
      if (error?.status === 404) {
        throw new Error('Sunucu endpoint\'ine erişilemiyor. Lütfen uygulama yapılandırmasını kontrol edin.');
      } else if (error?.status === 401) {
        throw new Error('E-posta veya şifre hatalı. Lütfen bilgilerinizi kontrol edin.');
      } else if (error?.status === 500) {
        throw new Error('Sunucu hatası. Lütfen daha sonra tekrar deneyin.');
      } else if (error?.message?.includes('Network')) {
        throw new Error('Ağ bağlantısı sorunu. İnternet bağlantınızı kontrol edin.');
      }

      throw error instanceof Error ? error : new Error('Giriş sırasında beklenmeyen bir hata oluştu.');
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
        firstName: userData.first_name,
        lastName: userData.last_name,
        phone: userData.phone || '',
        confirmPassword: userData.password
      }, API_BASE_URL);
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

  private async getCurrentUserId(): Promise<string | null> {
    try {
      const user = await this.getCurrentUser();
      return user?.id || null;
    } catch (error) {
      console.error('Error getting current user ID:', error);
      return null;
    }
  }

  // Strategy APIs
  async createStrategy(strategy: StrategyConfig, symbol: string): Promise<{ success: boolean; message: string; strategy_id?: string }> {
    console.log('🔍 createStrategy: Starting strategy creation');
    const requestBody = {
      name: strategy.name,
      description: strategy.description || '',
      templateId: 'custom', // Default template for user-created strategies
      symbol: symbol,
      parameters: strategy.parameters
    };

    console.log('📋 createStrategy: Request body:', requestBody);
    const url = `${API_BASE_URL}/v1/strategies/create`;
    console.log('🌐 createStrategy: Making request to:', url);

    try {
      const headers = await this.getHeaders();
      console.log('📋 createStrategy: Headers:', headers);

      const response = await fetch(url, {
        method: 'POST',
        headers: headers,
        body: JSON.stringify(requestBody),
      });

      console.log('📡 createStrategy: Response status:', response.status, response.statusText);

      const result = await this.handleResponse(response);
      console.log('✅ createStrategy: API response:', result);
      return result;
    } catch (error) {
      console.error('❌ createStrategy: API error:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Strateji kaydedilemedi'
      };
    }
  }

  async testStrategy(strategyId: string, symbol: string): Promise<BacktestResult> {
    const response = await fetch(`${API_BASE_URL}/v1/strategies/${strategyId}/test`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify({ symbol }),
    });

    return await this.handleResponse(response);
  }

  async getUserStrategies(): Promise<{ success: boolean; data?: any[]; message?: string }> {
    console.log('🔍 getUserStrategies: Starting API call');
    console.log('🔑 getUserStrategies: sessionToken exists?', !!this.sessionToken);
    const headers = await this.getHeaders();
    console.log('📋 getUserStrategies: headers', headers);

    const url = `${API_BASE_URL}/v1/strategies/my-strategies`;
    console.log('🌐 getUserStrategies: Making request to:', url);

    try {
      const response = await fetch(url, {
        headers,
      });

      console.log('📡 getUserStrategies: Response status:', response.status, response.statusText);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      // Backend returns direct array, not wrapped in success object
      const data = await response.json();
      console.log('✅ getUserStrategies: Raw API response', data);

      // Check if response is array (from backend) or wrapped object
      if (Array.isArray(data)) {
        console.log('📊 getUserStrategies: Backend returned array with', data.length, 'strategies');
        return { success: true, data: data };
      } else if (data && typeof data === 'object' && 'success' in data) {
        console.log('📊 getUserStrategies: Backend returned wrapped response');
        return data;
      } else {
        console.log('❌ getUserStrategies: Unexpected response format');
        return { success: false, message: 'Unexpected response format' };
      }
    } catch (e: any) {
      console.error('❌ getUserStrategies: API error', e);
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

  // Legacy WebSocket connection (maintained for backward compatibility)
  createWebSocketConnection(): WebSocket {
    // Get appropriate WebSocket URL based on platform
    const base = WS_BASE_URL;
    const url = this.sessionToken ? `${base}?token=${encodeURIComponent(this.sessionToken)}` : base;
    console.log('Connecting to WebSocket:', url);
    return new WebSocket(url);
  }

  // Legacy Gamification APIs (maintained for backward compatibility)
  async getUserAchievements(): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/gamification/achievements`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getLegacyLeaderboard(): Promise<any[]> {
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

  // Portfolio Management APIs
  async getPortfolios(): Promise<Portfolio[]> {
    const response = await fetch(`${API_BASE_URL}/portfolio`, {
      headers: await this.getHeaders(),
    });
    const portfolioData = await this.handleResponse<any>(response);
    // Backend returns single portfolio object, convert to array and map fields
    if (portfolioData) {
      const portfolio: Portfolio = {
        id: portfolioData.id,
        name: portfolioData.name || 'Portfolio',
        description: portfolioData.description,
        baseCurrency: portfolioData.baseCurrency || 'USD',
        totalValue: portfolioData.currentValue || 0,
        totalPnL: portfolioData.totalPnL || 0,
        totalPnLPercent: portfolioData.totalReturnPercent || 0,
        dailyPnL: portfolioData.dailyPnL || 0,
        positions: portfolioData.positions || [],
        initialCapital: portfolioData.initialCapital,
        cashBalance: portfolioData.cashBalance,
        lastUpdated: portfolioData.lastUpdated,
      };
      return [portfolio];
    }
    return [];
  }

  async getPortfolio(portfolioId: string): Promise<Portfolio> {
    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async createPortfolio(portfolio: { name: string; description?: string; baseCurrency: string }): Promise<Portfolio> {
    const response = await fetch(`${API_BASE_URL}/portfolio`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify(portfolio),
    });
    return await this.handleResponse(response);
  }

  async updatePortfolio(portfolioId: string, updates: Partial<Portfolio>): Promise<Portfolio> {
    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}`, {
      method: 'PUT',
      headers: await this.getHeaders(),
      body: JSON.stringify(updates),
    });
    return await this.handleResponse(response);
  }

  async deletePortfolio(portfolioId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}`, {
      method: 'DELETE',
      headers: await this.getHeaders(),
    });
    await this.handleResponse(response);
  }

  async getPortfolioPositions(portfolioId: string): Promise<Position[]> {
    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}/positions`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getPortfolioTransactions(portfolioId: string, limit?: number, offset?: number): Promise<Transaction[]> {
    const params = new URLSearchParams();
    if (limit) params.append('limit', limit.toString());
    if (offset) params.append('offset', offset.toString());
    
    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}/transactions?${params}`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async createTransaction(transaction: {
    portfolioId: string;
    symbol: string;
    type: 'BUY' | 'SELL';
    quantity: number;
    price: number;
    fee?: number;
    notes?: string;
  }): Promise<Transaction> {
    const response = await fetch(`${API_BASE_URL}/portfolio/transactions`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify(transaction),
    });
    return await this.handleResponse(response);
  }

  async getPortfolioAnalytics(portfolioId: string): Promise<PortfolioAnalytics> {
    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}/analytics`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getPortfolioPerformance(portfolioId: string): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}/performance`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async getPortfolioRisk(portfolioId: string): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}/risk`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  async exportPortfolio(request: ExportRequest): Promise<ExportResponse> {
    const response = await fetch(`${API_BASE_URL}/portfolio/export`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify(request),
    });
    return await this.handleResponse(response);
  }

  async getPortfolioReport(portfolioId: string, dateFrom?: string, dateTo?: string): Promise<any> {
    const params = new URLSearchParams();
    if (dateFrom) params.append('dateFrom', dateFrom);
    if (dateTo) params.append('dateTo', dateTo);

    const response = await fetch(`${API_BASE_URL}/portfolio/${portfolioId}/report?${params}`, {
      headers: await this.getHeaders(),
    });
    return await this.handleResponse(response);
  }

  // ====== ENHANCED MULTI-ASSET API METHODS ======

  // Asset Classes
  async getAssetClasses(config?: RequestConfig): Promise<AssetClassDto[]> {
    const response = await fetch(`${API_BASE_URL}/v1/asset-classes`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getActiveAssetClasses(config?: RequestConfig): Promise<AssetClassDto[]> {
    const response = await fetch(`${API_BASE_URL}/v1/asset-classes/active`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getAssetClass(id: string, config?: RequestConfig): Promise<AssetClassDto> {
    const response = await fetch(`${API_BASE_URL}/v1/asset-classes/${id}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  // Markets
  async getMarkets(config?: RequestConfig): Promise<MarketDto[]> {
    const response = await fetch(`${API_BASE_URL}/v1/markets`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getActiveMarkets(config?: RequestConfig): Promise<MarketDto[]> {
    const response = await fetch(`${API_BASE_URL}/v1/markets/active`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getMarketsByAssetClass(assetClassId: string, config?: RequestConfig): Promise<MarketDto[]> {
    const response = await fetch(`${API_BASE_URL}/v1/markets/by-asset-class/${assetClassId}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getMarketStatus(marketId: string, config?: RequestConfig): Promise<MarketStatusDto> {
    const response = await fetch(`${API_BASE_URL}/v1/markets/${marketId}/status`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getAllMarketStatuses(config?: RequestConfig): Promise<MarketStatusDto[]> {
    try {
      const response = await fetch(`${API_BASE_URL}/v1/markets/status`, {
        headers: await this.getHeaders(),
        signal: config?.abortSignal,
      });
      const result = await this.handleResponse(response);

      // If the API returns an empty array or wrapped response, return fallback data
      if (!result || (Array.isArray(result) && result.length === 0) || (result && typeof result === 'object' && 'data' in result && Array.isArray(result.data) && result.data.length === 0)) {
        return this.getFallbackMarketStatuses();
      }

      // Handle both direct array and wrapped response
      const data = Array.isArray(result) ? result : (result && typeof result === 'object' && 'data' in result ? result.data : []);
      return Array.isArray(data) ? data : [];
    } catch (error: any) {
      console.warn('Failed to fetch market statuses:', error);

      // Return fallback mock market status data
      return this.getFallbackMarketStatuses();
    }
  }

  private getFallbackMarketStatuses(): MarketStatusDto[] {
    const now = new Date();
    const currentHour = now.getHours();

    return [
      {
        marketId: 'crypto-market',
        marketName: 'Crypto Market',
        status: 'OPEN', // Crypto markets are always open
        nextOpen: undefined,
        nextClose: undefined,
        timeZone: 'UTC',
        currentTime: now.toISOString(),
        tradingDay: now.toISOString().split('T')[0],
        isHoliday: false
      },
      {
        marketId: 'bist-market',
        marketName: 'BIST',
        status: (currentHour >= 9 && currentHour < 18) ? 'OPEN' : 'CLOSED', // BIST trading hours approx
        nextOpen: (currentHour >= 9 && currentHour < 18) ? undefined : new Date(now.getFullYear(), now.getMonth(), now.getDate() + (currentHour >= 18 ? 1 : 0), 9, 0).toISOString(),
        nextClose: (currentHour >= 9 && currentHour < 18) ? new Date(now.getFullYear(), now.getMonth(), now.getDate(), 18, 0).toISOString() : undefined,
        timeZone: 'Europe/Istanbul',
        currentTime: now.toISOString(),
        tradingDay: now.toISOString().split('T')[0],
        isHoliday: false
      },
      {
        marketId: 'nasdaq-market',
        marketName: 'NASDAQ',
        status: 'CLOSED', // US markets are typically closed for Turkey time
        nextOpen: new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 16, 30).toISOString(), // Approx next open in Turkey time
        nextClose: undefined,
        timeZone: 'America/New_York',
        currentTime: now.toISOString(),
        tradingDay: now.toISOString().split('T')[0],
        isHoliday: false
      },
      {
        marketId: 'nyse-market',
        marketName: 'NYSE',
        status: 'CLOSED',
        nextOpen: new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 16, 30).toISOString(),
        nextClose: undefined,
        timeZone: 'America/New_York',
        currentTime: now.toISOString(),
        tradingDay: now.toISOString().split('T')[0],
        isHoliday: false
      }
    ];
  }

  // Enhanced Symbols
  async getEnhancedSymbols(
    page: number = 1,
    pageSize: number = 50,
    assetClassId?: string,
    marketId?: string,
    config?: RequestConfig
  ): Promise<PagedResponse<EnhancedSymbolDto>> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (assetClassId) params.append('assetClassId', assetClassId);
    if (marketId) params.append('marketId', marketId);

    const response = await fetch(`${API_BASE_URL}/v1/symbols?${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  // ====== SYMBOL PREFERENCE APIs ======

  /**
   * Get default symbols for an asset class (no authentication required)
   * @param assetClass The asset class (e.g., 'CRYPTO', 'STOCK')
   * @returns Default symbols for the asset class
   */
  async getDefaultSymbols(assetClass: string): Promise<EnhancedSymbolDto[]> {
    try {
      console.log(`[API] Fetching default symbols for ${assetClass}`);
      const response = await fetch(
        `${API_BASE_URL}/symbol-preferences/defaults?assetClass=${encodeURIComponent(assetClass)}`,
        {
          headers: await this.getHeaders(),
        }
      );

      // Handle wrapper response format with symbols property
      const data = await this.handleResponse<any>(response);
      const symbols = data.symbols || data; // Extract symbols array from wrapper
      console.log(`[API] Received ${symbols.length} default symbols for ${assetClass}`);
      return symbols;
    } catch (error) {
      console.error(`[API] Failed to fetch default symbols for ${assetClass}:`, error);
      throw error;
    }
  }

  /**
   * Get user-specific symbol preferences for an asset class
   * @param userId The user ID
   * @param assetClass The asset class (e.g., 'CRYPTO', 'STOCK')
   * @returns User's preferred symbols for the asset class
   */
  async getUserSymbolPreferences(userId: string, assetClass: string): Promise<EnhancedSymbolDto[]> {
    try {
      console.log(`[API] Fetching user symbol preferences for ${userId}, ${assetClass}`);
      const response = await fetch(
        `${API_BASE_URL}/symbol-preferences/user/${userId}?assetClass=${encodeURIComponent(assetClass)}`,
        {
          headers: await this.getHeaders(),
        }
      );

      // Handle wrapper response format with symbols property
      const data = await this.handleResponse<any>(response);
      const symbols = data.symbols || data; // Extract symbols array from wrapper
      console.log(`[API] Received ${symbols.length} user symbols for ${assetClass}`);
      return symbols;
    } catch (error) {
      console.error(`[API] Failed to fetch user symbol preferences:`, error);
      throw error;
    }
  }

  /**
   * Update user's symbol preferences
   * @param userId The user ID
   * @param symbolIds Array of symbol IDs to set as user preferences
   */
  async updateUserSymbolPreferences(userId: string, symbolIds: string[]): Promise<void> {
    try {
      console.log(`[API] Updating user symbol preferences for ${userId}:`, symbolIds.length, 'symbols');
      const response = await fetch(
        `${API_BASE_URL}/symbol-preferences/user/${userId}`,
        {
          method: 'PUT',
          headers: await this.getHeaders(),
          body: JSON.stringify({ symbolIds }),
        }
      );

      await this.handleResponse(response);
      console.log(`[API] Successfully updated user symbol preferences`);
    } catch (error) {
      console.error(`[API] Failed to update user symbol preferences:`, error);
      throw error;
    }
  }

  /**
   * Get symbols by asset class with intelligent caching and user preference support
   * @param assetClassId The asset class (e.g., 'CRYPTO', 'STOCK')
   * @param config Optional request configuration
   * @returns Array of symbols for the asset class
   */
  async getSymbolsByAssetClass(assetClassId: string, config?: RequestConfig): Promise<EnhancedSymbolDto[]> {
    try {
      // Step 1: Try to get from cache first
      const userId = await this.getCurrentUserId();
      const cachedSymbols = await SymbolCache.get(assetClassId, userId || undefined);

      if (cachedSymbols && cachedSymbols.length > 0) {
        console.log(`[API] Using cached symbols for ${assetClassId}: ${cachedSymbols.length} symbols`);
        return cachedSymbols;
      }

      // Step 2: Fetch from API with retry logic
      const symbols = await this.fetchSymbolsWithRetry(assetClassId, userId, config);

      // Step 3: Cache the result
      if (symbols && symbols.length > 0) {
        await SymbolCache.set(assetClassId, symbols, userId || undefined);
      }

      return symbols;
    } catch (error) {
      console.error(`[API] Failed to get symbols for ${assetClassId}:`, error);

      // Step 4: Try to use stale cache as last resort
      const userId = await this.getCurrentUserId();
      const staleCache = await SymbolCache.get(assetClassId, userId || undefined);
      if (staleCache && staleCache.length > 0) {
        console.warn(`[API] Using stale cache for ${assetClassId} due to error`);
        return staleCache;
      }

      // Step 5: Use minimal fallback only for emergencies
      console.warn(`[API] Using minimal fallback for ${assetClassId}`);
      return this.getMinimalFallback(assetClassId);
    }
  }

  /**
   * Fetch symbols from API with retry logic and user preference support
   */
  private async fetchSymbolsWithRetry(
    assetClassId: string,
    userId: string | null,
    config?: RequestConfig
  ): Promise<EnhancedSymbolDto[]> {
    const maxRetries = 3;
    const retryDelays = [1000, 2000, 4000]; // Exponential backoff

    for (let attempt = 0; attempt < maxRetries; attempt++) {
      try {
        // Use user-specific preferences if logged in, otherwise use defaults
        let symbols: EnhancedSymbolDto[];

        if (userId) {
          try {
            symbols = await this.getUserSymbolPreferences(userId, assetClassId);
          } catch (error) {
            console.warn(`[API] Failed to fetch user preferences, falling back to defaults:`, error);
            symbols = await this.getDefaultSymbols(assetClassId);
          }
        } else {
          symbols = await this.getDefaultSymbols(assetClassId);
        }

        // Validate response
        if (!symbols || !Array.isArray(symbols)) {
          throw new Error('Invalid response format from symbol API');
        }

        return symbols;
      } catch (error: any) {
        console.warn(`[API] Fetch attempt ${attempt + 1}/${maxRetries} failed for ${assetClassId}:`, error.message);

        // Handle specific HTTP error codes
        if (error.status === 409) {
          console.warn(`HTTP 409 conflict for ${assetClassId} symbols`);
          if (attempt < maxRetries - 1) {
            await this.delay(retryDelays[attempt]);
            continue;
          }
        }

        if (error.status === 429) {
          console.warn(`Rate limited for ${assetClassId} symbols`);
          if (attempt < maxRetries - 1) {
            await this.delay(retryDelays[attempt] * 2); // Double delay for rate limiting
            continue;
          }
        }

        // On network errors, retry with exponential backoff
        if (attempt < maxRetries - 1 && this.isRetryableError(error)) {
          await this.delay(retryDelays[attempt]);
          continue;
        }

        // On final attempt, throw to trigger fallback logic
        throw error;
      }
    }

    throw new Error(`Failed to fetch symbols after ${maxRetries} attempts`);
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  private isRetryableError(error: any): boolean {
    // Retry on network errors, timeouts, and server errors (5xx)
    return (
      error.name === 'TypeError' || // Network error
      error.name === 'AbortError' || // Timeout
      (error.status && error.status >= 500) // Server error
    );
  }

  private getFilteredFallbackData(query: string, assetClass?: string): EnhancedSymbolDto[] {
    const allFallbackData = [
      ...this.getFallbackSymbolsData('CRYPTO'),
      ...this.getFallbackSymbolsData('STOCK')
    ];

    let filteredData = allFallbackData;

    // Filter by asset class if specified
    if (assetClass) {
      filteredData = allFallbackData.filter(symbol =>
        symbol.assetClassName && symbol.assetClassName.toUpperCase() === assetClass.toUpperCase()
      );
    }

    // Filter by search query
    if (query && query.length > 0) {
      const lowerQuery = query.toLowerCase();
      filteredData = filteredData.filter(symbol =>
        symbol.symbol.toLowerCase().includes(lowerQuery) ||
        symbol.displayName.toLowerCase().includes(lowerQuery)
      );
    }

    return filteredData.slice(0, 20); // Limit results
  }

  /**
   * Minimal fallback symbols for emergency offline mode
   * Only BTC and ETH to ensure basic functionality
   */
  private getMinimalFallback(assetClassId: string): EnhancedSymbolDto[] {
    console.warn(`[API] Using MINIMAL emergency fallback for ${assetClassId} - network connectivity issue`);

    if (assetClassId.toUpperCase() === 'CRYPTO') {
      // Only BTC and ETH for emergency fallback
      return [
        {
          id: 'btc-fallback',
          symbol: 'BTC',
          displayName: 'Bitcoin',
          assetClassId: 'crypto-1',
          assetClassName: 'CRYPTO',
          marketId: 'crypto-market',
          marketName: 'Crypto Market',
          baseCurrency: 'BTC',
          quoteCurrency: 'USDT',
          isActive: true,
          isTracked: true,
          minTradeAmount: 0.001,
          maxTradeAmount: 1000000,
          priceDecimalPlaces: 2,
          quantityDecimalPlaces: 8,
          tickSize: 0.01,
          lotSize: 0.001,
          description: 'Bitcoin cryptocurrency',
          sector: 'Digital Assets',
          industry: 'Cryptocurrency',
          createdAt: new Date().toISOString()
        },
        {
          id: 'eth-fallback',
          symbol: 'ETH',
          displayName: 'Ethereum',
          assetClassId: 'crypto-1',
          assetClassName: 'CRYPTO',
          marketId: 'crypto-market',
          marketName: 'Crypto Market',
          baseCurrency: 'ETH',
          quoteCurrency: 'USDT',
          isActive: true,
          isTracked: true,
          minTradeAmount: 0.01,
          maxTradeAmount: 1000000,
          priceDecimalPlaces: 2,
          quantityDecimalPlaces: 8,
          tickSize: 0.01,
          lotSize: 0.01,
          description: 'Ethereum cryptocurrency',
          sector: 'Digital Assets',
          industry: 'Cryptocurrency',
          createdAt: new Date().toISOString()
        }
      ];
    }

    // No fallback for other asset classes in minimal mode
    return [];
  }

  async searchSymbols(
    query: string,
    assetClass?: string,
    limit: number = 20,
    config?: RequestConfig
  ): Promise<EnhancedSymbolDto[]> {
    try {
      const params = new URLSearchParams({
        q: query,
        limit: limit.toString(),
      });
      if (assetClass) params.append('assetClass', assetClass);

      const response = await fetch(`${API_BASE_URL}/v1/symbols/search?${params}`, {
        headers: await this.getHeaders(),
        signal: config?.abortSignal,
      });

      if (response.status === 409) {
        console.warn(`HTTP 409 conflict during symbol search for query: ${query}`);
        // Return filtered fallback data based on query
        return this.getFilteredFallbackData(query, assetClass);
      }

      return await this.handleResponse(response);
    } catch (error: any) {
      console.warn(`Symbol search failed for query '${query}':`, error);
      return this.getFilteredFallbackData(query, assetClass);
    }
  }

  async getSymbolDetails(symbolId: string, config?: RequestConfig): Promise<EnhancedSymbolDto> {
    const response = await fetch(`${API_BASE_URL}/v1/symbols/${symbolId}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  // Unified Market Data
  async getRealtimeData(symbolId: string, config?: RequestConfig): Promise<UnifiedMarketDataDto> {
    const response = await fetch(`${API_BASE_URL}/v1/market-data/${symbolId}/realtime`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getBatchMarketData(symbolIds: string[], config?: RequestConfig): Promise<UnifiedMarketDataDto[]> {
    const response = await fetch(`${API_BASE_URL}/v1/market-data/batch`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify({ symbolIds }),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getMarketOverview(config?: RequestConfig): Promise<MarketOverviewDto> {
    try {
      const response = await fetch(`${API_BASE_URL}/v1/market-data/overview`, {
        headers: await this.getHeaders(),
        signal: config?.abortSignal,
      });

      // CRITICAL FIX: Use enhanced handleResponse for proper ApiResponse<T> unwrapping
      const result = await this.handleResponse<MarketOverviewDto>(response);
      console.log('getMarketOverview result after unwrapping:', result);

      return result;
    } catch (error: any) {
      console.warn('Failed to fetch market overview:', error);

      // Return fallback market overview data
      return {
        totalMarketValue: 0,
        totalDailyChange: 0,
        totalDailyChangePercent: 0,
        activeMarkets: 3,
        totalSymbols: 0,
        cryptoMarketCap: 0,
        stockMarketValue: 0,
        topGainer: { symbol: '', change: 0 },
        topLoser: { symbol: '', change: 0 },
        mostActive: { symbol: '', volume: 0 },
        timestamp: new Date().toISOString()
      };
    }
  }

  async getTopMovers(assetClass?: string, config?: RequestConfig): Promise<TopMoversDto> {
    const params = assetClass ? `?assetClass=${encodeURIComponent(assetClass)}` : '';
    const response = await fetch(`${API_BASE_URL}/v1/market-data/top-movers${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  // New volume leaders endpoint integration
  async getTopByVolume(perClass: number = 8, config?: RequestConfig): Promise<any> {
    try {
      const response = await fetch(`${API_BASE_URL}/v1/market-data/top-by-volume?perClass=${perClass}`, {
        headers: await this.getHeaders(),
        signal: config?.abortSignal,
      });

      // CRITICAL FIX: Properly handle ApiResponse<T> unwrapping for volume data
      const result = await this.handleResponse(response);
      console.log('getTopByVolume result after unwrapping:', result);

      // Validate that we have the expected data structure
      if (result && typeof result === 'object') {
        // Check if it's already in the expected format (CRYPTO, STOCK keys)
        const hasExpectedKeys = Object.keys(result).some(key => ['CRYPTO', 'STOCK'].includes(key));
        if (hasExpectedKeys) {
          return result;
        }
      }

      // If result doesn't have expected format, return fallback
      console.warn('getTopByVolume: Unexpected data format, using fallback');
      return this.getFallbackVolumeLeadersData(perClass);
    } catch (error: any) {
      console.warn('Failed to fetch top volume leaders:', error);

      // Return fallback mock volume data
      return this.getFallbackVolumeLeadersData(perClass);
    }
  }

  private getFallbackVolumeLeadersData(perClass: number): any {
    const mockData = {
      CRYPTO: [
        { symbol: 'BTC', displayName: 'Bitcoin', volume: 25_000_000_000, price: 43_500, change: 2.5 },
        { symbol: 'ETH', displayName: 'Ethereum', volume: 15_000_000_000, price: 2_650, change: 3.2 },
        { symbol: 'BNB', displayName: 'Binance Coin', volume: 8_000_000_000, price: 320, change: -1.1 },
        { symbol: 'ADA', displayName: 'Cardano', volume: 5_000_000_000, price: 0.45, change: 1.8 },
        { symbol: 'SOL', displayName: 'Solana', volume: 4_500_000_000, price: 85, change: 4.3 },
        { symbol: 'XRP', displayName: 'Ripple', volume: 3_800_000_000, price: 0.62, change: -0.5 },
        { symbol: 'DOGE', displayName: 'Dogecoin', volume: 2_200_000_000, price: 0.08, change: 0.9 },
        { symbol: 'AVAX', displayName: 'Avalanche', volume: 1_800_000_000, price: 28, change: 2.1 },
      ],
      STOCK: [
        { symbol: 'TUPRS', displayName: 'Tüpraş', volume: 2_500_000, price: 58.5, change: 1.2 },
        { symbol: 'AKBNK', displayName: 'Akbank', volume: 180_000_000, price: 12.8, change: -0.8 },
        { symbol: 'THYAO', displayName: 'THY', volume: 85_000_000, price: 95.2, change: 2.3 },
        { symbol: 'BIMAS', displayName: 'BİM', volume: 45_000_000, price: 185, change: 0.7 },
        { symbol: 'TCELL', displayName: 'Turkcell', volume: 38_000_000, price: 14.5, change: 1.9 },
        { symbol: 'SAHOL', displayName: 'Sabancı Holding', volume: 32_000_000, price: 25.8, change: -1.3 },
        { symbol: 'EREGL', displayName: 'Erdemir', volume: 28_000_000, price: 42.3, change: 3.1 },
        { symbol: 'KCHOL', displayName: 'Koç Holding', volume: 25_000_000, price: 78.5, change: 0.4 },
      ]
    };

    const result: any = {};
    for (const [assetClass, symbols] of Object.entries(mockData)) {
      result[assetClass] = symbols.slice(0, perClass);
    }

    return result;
  }

  async getHistoricalData(
    symbolId: string,
    timeRange: '1D' | '7D' | '1M' | '3M' | '6M' | '1Y',
    interval: '1m' | '5m' | '15m' | '1h' | '4h' | '1d' = '1h',
    config?: RequestConfig
  ): Promise<Array<{
    timestamp: string;
    open: number;
    high: number;
    low: number;
    close: number;
    volume: number;
  }>> {
    const params = new URLSearchParams({
      timeRange,
      interval,
    });

    const response = await fetch(`${API_BASE_URL}/v1/market-data/${symbolId}/historical?${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  // Leaderboard APIs
  async getLeaderboard(
    period: 'weekly' | 'monthly' | 'all' = 'weekly',
    limit: number = 50,
    config?: RequestConfig
  ): Promise<LeaderboardEntry[]> {
    const params = new URLSearchParams({
      period,
      limit: limit.toString(),
    });

    const response = await fetch(`${API_BASE_URL}/v1/competition/leaderboard?${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getUserRanking(
    period: 'weekly' | 'monthly' | 'all' = 'weekly',
    config?: RequestConfig
  ): Promise<UserRanking> {
    const response = await fetch(`${API_BASE_URL}/v1/competition/my-ranking?period=${period}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getCompetitionStats(config?: RequestConfig): Promise<CompetitionStats> {
    const response = await fetch(`${API_BASE_URL}/v1/competition/stats`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async joinCompetition(config?: RequestConfig): Promise<{ success: boolean; message: string }> {
    const response = await fetch(`${API_BASE_URL}/v1/competition/join`, {
      method: 'POST',
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  // News Feed APIs
  async getMarketNews(
    assetClass?: string,
    limit: number = 20,
    offset: number = 0,
    config?: RequestConfig
  ): Promise<NewsItem[]> {
    try {
      const params = new URLSearchParams({
        limit: limit.toString(),
        offset: offset.toString(),
      });
      if (assetClass) params.append('assetClass', assetClass);

      const response = await fetch(`${API_BASE_URL}/v1/news/market?${params}`, {
        headers: await this.getHeaders(),
        signal: config?.abortSignal,
      });
      return await this.handleResponse(response);
    } catch (error: any) {
      console.warn('Failed to fetch news:', error);

      // Return fallback mock news data
      return this.getFallbackNewsData(limit, assetClass);
    }
  }

  private getFallbackNewsData(limit: number = 20, assetClass?: string): NewsItem[] {
    const mockNews: NewsItem[] = [
      {
        id: '1',
        title: 'Piyasalarda Günün Öne Çıkan Gelişmeleri',
        summary: 'Küresel piyasalarda bugün yaşanan önemli gelişmeler ve yatırımcılar için dikkat çekici noktalar.',
        content: 'Detaylı piyasa analizi ve uzman yorumları...',
        author: 'Piyasa Analisti',
        source: 'myTrader Haber',
        category: 'Market',
        publishedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
        imageUrl: 'https://via.placeholder.com/300x200/667eea/ffffff?text=Market+News',
        url: 'https://example.com/news/1',
        tags: ['piyasa', 'analiz', 'günlük'],
        relatedSymbols: [],
        importance: 'MEDIUM',
        language: 'tr'
      },
      {
        id: '2',
        title: 'Bitcoin 45.000 Dolar Seviyesini Test Ediyor',
        summary: 'Bitcoin, son günlerde yaşanan yükseliş ile kritik direnç seviyesine yaklaştı.',
        content: 'Bitcoin teknik analizi ve fiyat hedefleri...',
        author: 'Kripto Analisti',
        source: 'CryptoNews',
        category: 'Cryptocurrency',
        publishedAt: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(), // 1 hour ago
        imageUrl: 'https://via.placeholder.com/300x200/f59e0b/ffffff?text=Bitcoin',
        url: 'https://example.com/crypto-news/1',
        tags: ['bitcoin', 'btc', 'fiyat'],
        relatedSymbols: ['BTC'],
        importance: 'HIGH',
        language: 'tr'
      },
      {
        id: '3',
        title: 'Teknoloji Hisselerinde Yükseliş Trendi',
        summary: 'Teknoloji sektöründe yaşanan pozitif gelişmeler yatırımcı ilgisini artırıyor.',
        content: 'Teknoloji hisselerindeki yükseliş detayları...',
        author: 'Sektör Analisti',
        source: 'myTrader Haber',
        category: 'Technology',
        publishedAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(), // 6 hours ago
        imageUrl: 'https://via.placeholder.com/300x200/8b5cf6/ffffff?text=Tech+Stocks',
        url: 'https://example.com/news/3',
        tags: ['teknoloji', 'hisse', 'yükseliş'],
        relatedSymbols: ['AAPL', 'MSFT'],
        importance: 'MEDIUM',
        language: 'tr'
      },
      {
        id: '4',
        title: 'BIST 100 Endeksi Güçlü Performans Sergiliyor',
        summary: 'Borsa İstanbul\'da yaşanan yükseliş trendi devam ediyor.',
        content: 'BIST 100 teknik analizi...',
        author: 'BIST Analisti',
        source: 'Borsa Haber',
        category: 'Stock Market',
        publishedAt: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(), // 4 hours ago
        imageUrl: 'https://via.placeholder.com/300x200/ef4444/ffffff?text=BIST',
        url: 'https://example.com/bist-news/1',
        tags: ['bist', 'borsa', 'endeks'],
        relatedSymbols: ['XU100'],
        importance: 'HIGH',
        language: 'tr'
      },
      {
        id: '5',
        title: 'Merkez Bankası Faiz Kararı Açıklandı',
        summary: 'Merkez Bankası\'nın bugün açıkladığı faiz kararı piyasalarda nasıl karşılandı?',
        content: 'Faiz kararının detayları ve piyasa yansımaları...',
        author: 'Ekonomi Editörü',
        source: 'myTrader Haber',
        category: 'Economics',
        publishedAt: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(), // 8 hours ago
        imageUrl: 'https://via.placeholder.com/300x200/10b981/ffffff?text=Central+Bank',
        url: 'https://example.com/news/2',
        tags: ['merkez-bankası', 'faiz', 'ekonomi'],
        relatedSymbols: [],
        importance: 'HIGH',
        language: 'tr'
      }
    ];

    // Filter by asset class if specified (using category as proxy)
    let filteredNews = mockNews;
    if (assetClass) {
      const categoryMap: Record<string, string[]> = {
        'CRYPTO': ['Cryptocurrency'],
        'STOCK': ['Technology', 'Stock Market'],
        'GENERAL': ['Market', 'Economics']
      };
      const categories = categoryMap[assetClass.toUpperCase()] || [];
      filteredNews = mockNews.filter(news =>
        categories.includes(news.category)
      );
    }

    return filteredNews.slice(0, limit);
  }

  async getCryptoNews(limit: number = 20, config?: RequestConfig): Promise<NewsItem[]> {
    const response = await fetch(`${API_BASE_URL}/v1/news/crypto?limit=${limit}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getStockNews(
    market: 'BIST' | 'NASDAQ' = 'BIST',
    limit: number = 20,
    config?: RequestConfig
  ): Promise<NewsItem[]> {
    const response = await fetch(`${API_BASE_URL}/v1/news/stocks?market=${market}&limit=${limit}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async searchNews(query: string, limit: number = 20, config?: RequestConfig): Promise<NewsItem[]> {
    const params = new URLSearchParams({
      q: query,
      limit: limit.toString(),
    });

    const response = await fetch(`${API_BASE_URL}/v1/news/search?${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getNewsById(newsId: string, config?: RequestConfig): Promise<NewsItem> {
    const response = await fetch(`${API_BASE_URL}/v1/news/${newsId}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  // Watchlist Management
  async getUserWatchlists(config?: RequestConfig): Promise<Array<{
    id: string;
    name: string;
    symbols: string[];
    createdAt: string;
    updatedAt: string;
  }>> {
    const response = await fetch(`${API_BASE_URL}/v1/watchlists`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async createWatchlist(
    name: string,
    symbols: string[] = [],
    config?: RequestConfig
  ): Promise<{ id: string; success: boolean; message: string }> {
    const response = await fetch(`${API_BASE_URL}/v1/watchlists`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify({ name, symbols }),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async updateWatchlist(
    watchlistId: string,
    updates: { name?: string; symbols?: string[] },
    config?: RequestConfig
  ): Promise<{ success: boolean; message: string }> {
    const response = await fetch(`${API_BASE_URL}/v1/watchlists/${watchlistId}`, {
      method: 'PUT',
      headers: await this.getHeaders(),
      body: JSON.stringify(updates),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async deleteWatchlist(watchlistId: string, config?: RequestConfig): Promise<{ success: boolean; message: string }> {
    const response = await fetch(`${API_BASE_URL}/v1/watchlists/${watchlistId}`, {
      method: 'DELETE',
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  // Enhanced WebSocket Connection with Token
  createEnhancedWebSocketConnection(subscriptions?: string[]): WebSocket {
    const base = WS_BASE_URL;
    const params = new URLSearchParams();

    if (this.sessionToken) {
      params.append('token', this.sessionToken);
    }

    if (subscriptions && subscriptions.length > 0) {
      params.append('subscriptions', subscriptions.join(','));
    }

    const url = `${base}?${params.toString()}`;
    console.log('Creating enhanced WebSocket connection:', url);
    return new WebSocket(url);
  }
}

export const apiService = new ApiService();
