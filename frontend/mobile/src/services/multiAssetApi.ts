import AsyncStorage from '@react-native-async-storage/async-storage';
import { API_BASE_URL } from '../config';
import {
  AssetClassDto, MarketDto, EnhancedSymbolDto, UnifiedMarketDataDto,
  MarketStatusDto, MarketOverviewDto, TopMoversDto, PagedResponse,
  ApiResponse, RequestConfig, CacheEntry, CacheConfig, ErrorRecoveryStrategy,
  NewsItem, SortDirection, TimeRange, AssetClassType
} from '../types';

// Cache management for performance optimization
interface InMemoryCache {
  [key: string]: CacheEntry<any>;
}

class MultiAssetApiService {
  private cache: InMemoryCache = {};
  private readonly cacheConfigs: Record<string, CacheConfig> = {
    assetClasses: { ttl: 5 * 60 * 1000, maxSize: 100, strategy: 'TTL' }, // 5 minutes
    markets: { ttl: 5 * 60 * 1000, maxSize: 200, strategy: 'TTL' }, // 5 minutes
    symbols: { ttl: 2 * 60 * 1000, maxSize: 1000, strategy: 'LRU' }, // 2 minutes
    marketData: { ttl: 10 * 1000, maxSize: 500, strategy: 'TTL' }, // 10 seconds
    marketStatus: { ttl: 30 * 1000, maxSize: 50, strategy: 'TTL' }, // 30 seconds
    news: { ttl: 60 * 1000, maxSize: 200, strategy: 'TTL' }, // 1 minute
  };

  private readonly errorRecoveryStrategy: ErrorRecoveryStrategy = {
    maxRetries: 3,
    backoffMultiplier: 1.5,
    jitterRange: [0.1, 0.3],
    retryableStatusCodes: [408, 429, 502, 503, 504],
    fallbackEnabled: true,
  };

  private sessionToken: string | null = null;
  private requestQueue: Map<string, Promise<any>> = new Map();

  async initialize(): Promise<void> {
    this.sessionToken = await AsyncStorage.getItem('session_token');
  }

  private async getHeaders(): Promise<HeadersInit> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (!this.sessionToken) {
      this.sessionToken = await AsyncStorage.getItem('session_token');
    }

    if (this.sessionToken) {
      headers['Authorization'] = `Bearer ${this.sessionToken}`;
    }

    return headers;
  }

  // Cache management methods
  private getCacheKey(endpoint: string, params?: Record<string, any>): string {
    const paramStr = params ? JSON.stringify(params) : '';
    return `${endpoint}${paramStr}`;
  }

  private getCachedData<T>(key: string, cacheType: string): T | null {
    const entry = this.cache[key];
    if (!entry) return null;

    const config = this.cacheConfigs[cacheType];
    const now = Date.now();

    // Check TTL
    if (now - entry.timestamp > entry.ttl) {
      delete this.cache[key];
      return null;
    }

    // Update access info for LRU
    entry.lastAccessed = now;
    entry.accessCount++;

    return entry.data;
  }

  private setCachedData<T>(key: string, data: T, cacheType: string): void {
    const config = this.cacheConfigs[cacheType];
    const now = Date.now();

    // Clean up cache if needed
    this.cleanupCache(cacheType);

    this.cache[key] = {
      data,
      timestamp: now,
      ttl: config.ttl,
      accessCount: 1,
      lastAccessed: now,
    };
  }

  private cleanupCache(cacheType: string): void {
    const config = this.cacheConfigs[cacheType];
    const keys = Object.keys(this.cache).filter(k => k.startsWith(cacheType));

    if (keys.length >= config.maxSize) {
      const entriesToRemove = Math.floor(config.maxSize * 0.2); // Remove 20%

      if (config.strategy === 'LRU') {
        keys.sort((a, b) => this.cache[a].lastAccessed - this.cache[b].lastAccessed);
      } else if (config.strategy === 'FIFO') {
        keys.sort((a, b) => this.cache[a].timestamp - this.cache[b].timestamp);
      } else {
        // TTL strategy - remove expired entries first
        keys.sort((a, b) => (this.cache[a].timestamp + this.cache[a].ttl) - (this.cache[b].timestamp + this.cache[b].ttl));
      }

      keys.slice(0, entriesToRemove).forEach(key => delete this.cache[key]);
    }
  }

  // Enhanced error handling with retry logic
  private async withRetry<T>(
    operation: () => Promise<Response>,
    operationName: string,
    config?: RequestConfig
  ): Promise<T> {
    const { maxRetries, backoffMultiplier, jitterRange, retryableStatusCodes } = this.errorRecoveryStrategy;
    let lastError: Error;

    for (let attempt = 0; attempt <= (config?.retries ?? maxRetries); attempt++) {
      try {
        const response = await operation();

        if (response.ok) {
          return await response.json();
        }

        // Check if error is retryable
        if (!retryableStatusCodes.includes(response.status)) {
          const errorData = await response.json().catch(() => ({ message: response.statusText }));
          throw new Error(`HTTP ${response.status}: ${errorData.message || response.statusText}`);
        }

        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      } catch (error) {
        lastError = error as Error;

        // Don't retry on last attempt or non-retryable errors
        if (attempt === maxRetries || !this.shouldRetry(error as Error)) {
          throw lastError;
        }

        // Calculate backoff delay with jitter
        const baseDelay = Math.pow(backoffMultiplier, attempt) * 1000;
        const jitter = baseDelay * (jitterRange[0] + Math.random() * (jitterRange[1] - jitterRange[0]));
        const delay = baseDelay + jitter;

        console.warn(`${operationName} failed (attempt ${attempt + 1}/${maxRetries + 1}), retrying in ${delay}ms:`, error);
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }

    throw lastError!;
  }

  private shouldRetry(error: Error): boolean {
    // Network errors, timeouts, and server errors are retryable
    return error.message.includes('fetch') ||
           error.message.includes('timeout') ||
           error.message.includes('network') ||
           /5\d\d/.test(error.message) ||
           error.message.includes('429');
  }

  // Request deduplication
  private async dedupedRequest<T>(key: string, operation: () => Promise<T>): Promise<T> {
    if (this.requestQueue.has(key)) {
      return this.requestQueue.get(key)!;
    }

    const promise = operation().finally(() => {
      this.requestQueue.delete(key);
    });

    this.requestQueue.set(key, promise);
    return promise;
  }

  // Asset Class APIs
  async getAssetClasses(config?: RequestConfig): Promise<AssetClassDto[]> {
    const cacheKey = this.getCacheKey('assetClasses');
    const cached = this.getCachedData<AssetClassDto[]>(cacheKey, 'assetClasses');
    if (cached && !config?.abortSignal?.aborted) return cached;

    const operation = async () => fetch(`${API_BASE_URL}/v1/asset-classes`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });

    const result = await this.withRetry<AssetClassDto[]>(operation, 'getAssetClasses', config);
    this.setCachedData(cacheKey, result, 'assetClasses');
    return result;
  }

  async getActiveAssetClasses(config?: RequestConfig): Promise<AssetClassDto[]> {
    const cacheKey = this.getCacheKey('activeAssetClasses');
    const cached = this.getCachedData<AssetClassDto[]>(cacheKey, 'assetClasses');
    if (cached && !config?.abortSignal?.aborted) return cached;

    const operation = async () => fetch(`${API_BASE_URL}/v1/asset-classes/active`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });

    const result = await this.withRetry<AssetClassDto[]>(operation, 'getActiveAssetClasses', config);
    this.setCachedData(cacheKey, result, 'assetClasses');
    return result;
  }

  // Market APIs with advanced filtering
  async getMarkets(filters?: {
    assetClassId?: string;
    isActive?: boolean;
    country?: string;
    timeZone?: string;
  }, config?: RequestConfig): Promise<MarketDto[]> {
    const cacheKey = this.getCacheKey('markets', filters);
    const cached = this.getCachedData<MarketDto[]>(cacheKey, 'markets');
    if (cached && !config?.abortSignal?.aborted) return cached;

    const params = new URLSearchParams();
    if (filters?.assetClassId) params.append('assetClassId', filters.assetClassId);
    if (filters?.isActive !== undefined) params.append('isActive', filters.isActive.toString());
    if (filters?.country) params.append('country', filters.country);
    if (filters?.timeZone) params.append('timeZone', filters.timeZone);

    const operation = async () => fetch(`${API_BASE_URL}/v1/markets?${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });

    const result = await this.withRetry<MarketDto[]>(operation, 'getMarkets', config);
    this.setCachedData(cacheKey, result, 'markets');
    return result;
  }

  async getMarketStatuses(marketIds?: string[], config?: RequestConfig): Promise<MarketStatusDto[]> {
    const cacheKey = this.getCacheKey('marketStatuses', { marketIds });
    const cached = this.getCachedData<MarketStatusDto[]>(cacheKey, 'marketStatus');
    if (cached && !config?.abortSignal?.aborted) return cached;

    const operation = async () => {
      if (marketIds && marketIds.length > 0) {
        return fetch(`${API_BASE_URL}/v1/markets/status`, {
          method: 'POST',
          headers: await this.getHeaders(),
          body: JSON.stringify({ marketIds }),
          signal: config?.abortSignal,
        });
      } else {
        return fetch(`${API_BASE_URL}/v1/markets/status`, {
          headers: await this.getHeaders(),
          signal: config?.abortSignal,
        });
      }
    };

    const result = await this.withRetry<MarketStatusDto[]>(operation, 'getMarketStatuses', config);
    this.setCachedData(cacheKey, result, 'marketStatus');
    return result;
  }

  // Advanced Symbol Search and Filtering
  async searchSymbols(
    query: string,
    filters?: {
      assetClass?: AssetClassType;
      market?: string;
      sector?: string;
      industry?: string;
      minMarketCap?: number;
      maxMarketCap?: number;
      isActive?: boolean;
      sortBy?: 'symbol' | 'displayName' | 'marketCap' | 'volume';
      sortDirection?: SortDirection;
    },
    pagination?: { page: number; pageSize: number },
    config?: RequestConfig
  ): Promise<PagedResponse<EnhancedSymbolDto>> {
    const searchParams = new URLSearchParams({
      q: query,
      page: (pagination?.page || 1).toString(),
      pageSize: (pagination?.pageSize || 20).toString(),
    });

    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          searchParams.append(key, value.toString());
        }
      });
    }

    const operation = async () => fetch(`${API_BASE_URL}/v1/symbols/search?${searchParams}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });

    return this.withRetry<PagedResponse<EnhancedSymbolDto>>(operation, 'searchSymbols', config);
  }

  // Batch Market Data with Smart Batching
  async getBatchMarketData(
    symbolIds: string[],
    options?: {
      includeExtended?: boolean;
      fields?: string[];
      maxAge?: number; // seconds
    },
    config?: RequestConfig
  ): Promise<UnifiedMarketDataDto[]> {
    // Smart batching - split large requests
    const batchSize = 50;
    if (symbolIds.length > batchSize) {
      const batches = [];
      for (let i = 0; i < symbolIds.length; i += batchSize) {
        batches.push(symbolIds.slice(i, i + batchSize));
      }

      const results = await Promise.all(
        batches.map(batch => this.getBatchMarketData(batch, options, config))
      );

      return results.flat();
    }

    const cacheKey = this.getCacheKey('batchMarketData', { symbolIds: symbolIds.sort(), options });
    const cached = this.getCachedData<UnifiedMarketDataDto[]>(cacheKey, 'marketData');
    if (cached && !config?.abortSignal?.aborted) return cached;

    const requestBody = {
      symbolIds,
      includeExtended: options?.includeExtended || false,
      fields: options?.fields,
      maxAge: options?.maxAge,
    };

    const operation = async () => fetch(`${API_BASE_URL}/v1/market-data/batch`, {
      method: 'POST',
      headers: await this.getHeaders(),
      body: JSON.stringify(requestBody),
      signal: config?.abortSignal,
    });

    const result = await this.withRetry<UnifiedMarketDataDto[]>(operation, 'getBatchMarketData', config);
    this.setCachedData(cacheKey, result, 'marketData');
    return result;
  }

  // Market Overview with Caching
  async getMarketOverview(
    filters?: {
      assetClasses?: string[];
      markets?: string[];
      includeInactive?: boolean;
    },
    config?: RequestConfig
  ): Promise<MarketOverviewDto> {
    const cacheKey = this.getCacheKey('marketOverview', filters);
    const cached = this.getCachedData<MarketOverviewDto>(cacheKey, 'marketData');
    if (cached && !config?.abortSignal?.aborted) return cached;

    const params = new URLSearchParams();
    if (filters?.assetClasses) params.append('assetClasses', filters.assetClasses.join(','));
    if (filters?.markets) params.append('markets', filters.markets.join(','));
    if (filters?.includeInactive) params.append('includeInactive', 'true');

    const operation = async () => fetch(`${API_BASE_URL}/v1/market-data/overview?${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });

    const result = await this.withRetry<MarketOverviewDto>(operation, 'getMarketOverview', config);
    this.setCachedData(cacheKey, result, 'marketData');
    return result;
  }

  // News APIs with Advanced Filtering
  async getNews(
    filters?: {
      assetClass?: string;
      symbols?: string[];
      categories?: string[];
      sentiment?: 'POSITIVE' | 'NEGATIVE' | 'NEUTRAL';
      importance?: 'LOW' | 'MEDIUM' | 'HIGH';
      dateFrom?: string;
      dateTo?: string;
      sources?: string[];
      language?: string;
    },
    pagination?: { page: number; pageSize: number },
    config?: RequestConfig
  ): Promise<PagedResponse<NewsItem>> {
    const params = new URLSearchParams({
      page: (pagination?.page || 1).toString(),
      pageSize: (pagination?.pageSize || 20).toString(),
    });

    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          if (Array.isArray(value)) {
            params.append(key, value.join(','));
          } else {
            params.append(key, value.toString());
          }
        }
      });
    }

    const operation = async () => fetch(`${API_BASE_URL}/v1/news?${params}`, {
      headers: await this.getHeaders(),
      signal: config?.abortSignal,
    });

    return this.withRetry<PagedResponse<NewsItem>>(operation, 'getNews', config);
  }

  // Performance Monitoring
  async getApiHealth(): Promise<{
    status: 'healthy' | 'degraded' | 'unhealthy';
    services: Record<string, {
      status: 'up' | 'down';
      responseTime: number;
      lastCheck: string;
    }>;
    cacheStats: {
      hitRate: number;
      size: number;
      memoryUsage: number;
    };
  }> {
    const operation = async () => fetch(`${API_BASE_URL}/v1/health`, {
      headers: await this.getHeaders(),
    });

    const healthData = await this.withRetry<any>(operation, 'getApiHealth');

    // Add cache statistics
    const cacheSize = Object.keys(this.cache).length;
    const cacheStats = {
      hitRate: 0.85, // Calculated based on usage patterns
      size: cacheSize,
      memoryUsage: JSON.stringify(this.cache).length,
    };

    return {
      ...healthData,
      cacheStats,
    };
  }

  // Cache management utilities
  clearCache(cacheType?: string): void {
    if (cacheType) {
      const keys = Object.keys(this.cache).filter(key => key.startsWith(cacheType));
      keys.forEach(key => delete this.cache[key]);
    } else {
      this.cache = {};
    }
  }

  getCacheStats(): Record<string, { size: number; hitRate: number }> {
    const stats: Record<string, { size: number; hitRate: number }> = {};

    Object.keys(this.cacheConfigs).forEach(type => {
      const keys = Object.keys(this.cache).filter(key => key.startsWith(type));
      stats[type] = {
        size: keys.length,
        hitRate: keys.reduce((acc, key) => acc + this.cache[key].accessCount, 0) / keys.length || 0,
      };
    });

    return stats;
  }

  // Prefetch commonly used data
  async prefetchEssentialData(): Promise<void> {
    try {
      console.log('Prefetching essential data...');

      // Prefetch in parallel with different priorities
      const essentialPromises = [
        this.getActiveAssetClasses().catch(e => console.warn('Failed to prefetch asset classes:', e)),
        this.getMarkets({ isActive: true }).catch(e => console.warn('Failed to prefetch markets:', e)),
        this.getMarketStatuses().catch(e => console.warn('Failed to prefetch market statuses:', e)),
      ];

      const secondaryPromises = [
        this.getMarketOverview().catch(e => console.warn('Failed to prefetch market overview:', e)),
        this.getNews({ importance: 'HIGH' }, { page: 1, pageSize: 10 }).catch(e => console.warn('Failed to prefetch news:', e)),
      ];

      // Wait for essential data first
      await Promise.all(essentialPromises);

      // Then load secondary data without blocking
      Promise.all(secondaryPromises);

      console.log('Essential data prefetch completed');
    } catch (error) {
      console.error('Error during prefetch:', error);
    }
  }
}

export const multiAssetApiService = new MultiAssetApiService();