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
      const response = await this.postJsonWithFallback<UserSession>('/auth/login', { email: email, password: password }, API_BASE_URL);

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

  // Strategy APIs
  async createStrategy(strategy: StrategyConfig, symbol: string): Promise<{ success: boolean; message: string; strategy_id?: string }> {
    const requestBody = {
      name: strategy.name,
      description: strategy.description || '',
      templateId: 'custom', // Default template for user-created strategies
      symbol: symbol,
      parameters: strategy.parameters
    };

    const response = await fetch(`${API_BASE_URL}/v1/strategies/create`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify(requestBody),
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

  async getUserStrategies(): Promise<{ success: boolean; data?: any[]; message?: string }> {
    console.log('getUserStrategies: sessionToken exists?', !!this.sessionToken);
    const headers = await this.getHeaders();
    console.log('getUserStrategies: headers', headers);
    
    const response = await fetch(`${API_BASE_URL}/v1/strategies/my-strategies`, {
      headers,
    });

    try {
      const result = await this.handleResponse<{ success: boolean; data: any[] }>(response);
      console.log('getUserStrategies: API response', result);
      return { success: result.success, data: result.data };
    } catch (e: any) {
      console.error('getUserStrategies: API error', e);
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

  async getSymbolsByAssetClass(assetClassId: string, config?: RequestConfig): Promise<EnhancedSymbolDto[]> {
    const maxRetries = 3;
    const retryDelays = [1000, 2000, 4000]; // Exponential backoff

    for (let attempt = 0; attempt < maxRetries; attempt++) {
      try {
        const response = await fetch(`${API_BASE_URL}/v1/symbols/by-asset-class/${assetClassId}`, {
          headers: await this.getHeaders(),
          signal: config?.abortSignal,
        });

        // Handle specific HTTP error codes
        if (response.status === 409) {
          console.warn(`HTTP 409 conflict for ${assetClassId} symbols (attempt ${attempt + 1}/${maxRetries})`);
          if (attempt < maxRetries - 1) {
            await this.delay(retryDelays[attempt]);
            continue;
          }
          // On final attempt, return fallback data for 409 errors
          console.warn(`Returning fallback data for ${assetClassId} after ${maxRetries} attempts`);
          return this.getFallbackSymbolsData(assetClassId);
        }

        if (response.status === 429) {
          // Rate limited - wait longer
          console.warn(`Rate limited for ${assetClassId} symbols (attempt ${attempt + 1}/${maxRetries})`);
          if (attempt < maxRetries - 1) {
            await this.delay(retryDelays[attempt] * 2); // Double delay for rate limiting
            continue;
          }
        }

        return await this.handleResponse(response);
      } catch (error: any) {
        console.warn(`Failed to fetch ${assetClassId} symbols (attempt ${attempt + 1}/${maxRetries}):`, error);

        // On network errors, retry with exponential backoff
        if (attempt < maxRetries - 1 && this.isRetryableError(error)) {
          await this.delay(retryDelays[attempt]);
          continue;
        }

        // On final attempt or non-retryable error, return fallback data
        console.warn(`Returning fallback data for ${assetClassId} due to error:`, error.message);
        return this.getFallbackSymbolsData(assetClassId);
      }
    }

    // Fallback if somehow we get here
    return this.getFallbackSymbolsData(assetClassId);
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
        symbol.assetClassName.toUpperCase() === assetClass.toUpperCase()
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

  private getFallbackSymbolsData(assetClassId: string): EnhancedSymbolDto[] {
    if (assetClassId.toUpperCase() === 'CRYPTO') {
      return [
        {
          id: '1',
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
          id: '2',
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
        },
        {
          id: '3',
          symbol: 'ADA',
          displayName: 'Cardano',
          assetClassId: 'crypto-1',
          assetClassName: 'CRYPTO',
          marketId: 'crypto-market',
          marketName: 'Crypto Market',
          baseCurrency: 'ADA',
          quoteCurrency: 'USDT',
          isActive: true,
          isTracked: true,
          minTradeAmount: 1,
          maxTradeAmount: 1000000,
          priceDecimalPlaces: 4,
          quantityDecimalPlaces: 0,
          tickSize: 0.0001,
          lotSize: 1,
          description: 'Cardano cryptocurrency',
          sector: 'Digital Assets',
          industry: 'Cryptocurrency',
          createdAt: new Date().toISOString()
        }
      ];
    } else if (assetClassId.toUpperCase() === 'STOCK') {
      return [
        {
          id: '4',
          symbol: 'TUPRS',
          displayName: 'Tüpraş',
          assetClassId: 'stock-1',
          assetClassName: 'STOCK',
          marketId: 'bist-market',
          marketName: 'BIST Market',
          baseCurrency: 'TUPRS',
          quoteCurrency: 'TRY',
          isActive: true,
          isTracked: true,
          minTradeAmount: 1,
          maxTradeAmount: 1000000,
          priceDecimalPlaces: 2,
          quantityDecimalPlaces: 0,
          tickSize: 0.01,
          lotSize: 1,
          description: 'Tüpraş stock traded on BIST',
          sector: 'Energy',
          industry: 'Oil & Gas',
          createdAt: new Date().toISOString()
        },
        {
          id: '5',
          symbol: 'AAPL',
          displayName: 'Apple Inc.',
          assetClassId: 'stock-1',
          assetClassName: 'STOCK',
          marketId: 'nasdaq-market',
          marketName: 'NASDAQ Market',
          baseCurrency: 'AAPL',
          quoteCurrency: 'USD',
          isActive: true,
          isTracked: true,
          minTradeAmount: 1,
          maxTradeAmount: 1000000,
          priceDecimalPlaces: 2,
          quantityDecimalPlaces: 0,
          tickSize: 0.01,
          lotSize: 1,
          description: 'Apple Inc. stock traded on NASDAQ',
          sector: 'Technology',
          industry: 'Consumer Electronics',
          createdAt: new Date().toISOString()
        },
        {
          id: '6',
          symbol: 'MSFT',
          displayName: 'Microsoft Corporation',
          assetClassId: 'stock-1',
          assetClassName: 'STOCK',
          marketId: 'nasdaq-market',
          marketName: 'NASDAQ Market',
          baseCurrency: 'MSFT',
          quoteCurrency: 'USD',
          isActive: true,
          isTracked: true,
          minTradeAmount: 1,
          maxTradeAmount: 1000000,
          priceDecimalPlaces: 2,
          quantityDecimalPlaces: 0,
          tickSize: 0.01,
          lotSize: 1,
          description: 'Microsoft Corporation stock traded on NASDAQ',
          sector: 'Technology',
          industry: 'Software',
          createdAt: new Date().toISOString()
        }
      ];
    }

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
    const response = await fetch(`${API_BASE_URL}/v1/market-data/overview`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
  }

  async getTopMovers(assetClass?: string, config?: RequestConfig): Promise<TopMoversDto> {
    const params = assetClass ? `?assetClass=${encodeURIComponent(assetClass)}` : '';
    const response = await fetch(`${API_BASE_URL}/v1/market-data/top-movers${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });
    return await this.handleResponse(response);
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
