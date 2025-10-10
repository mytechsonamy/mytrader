import AsyncStorage from '@react-native-async-storage/async-storage';
import { EnhancedSymbolDto } from '../types';

/**
 * Symbol Cache Service
 *
 * Provides caching functionality for symbol data to improve performance
 * and enable offline mode. Uses AsyncStorage for persistence.
 */
export class SymbolCache {
  private static readonly CACHE_KEY_PREFIX = 'symbols_cache_';
  private static readonly CACHE_DURATION = 5 * 60 * 1000; // 5 minutes
  private static readonly VERSION = 'v1';

  /**
   * Get cached symbols for a specific asset class and user
   * @param assetClass The asset class to retrieve (e.g., 'CRYPTO', 'STOCK')
   * @param userId Optional user ID for user-specific preferences
   * @returns Cached symbols or null if cache is expired/missing
   */
  static async get(assetClass: string, userId?: string): Promise<EnhancedSymbolDto[] | null> {
    try {
      const key = this.getCacheKey(assetClass, userId);
      const cached = await AsyncStorage.getItem(key);

      if (!cached) {
        return null;
      }

      const { data, timestamp, version } = JSON.parse(cached);

      // Check if cache is expired
      if (Date.now() - timestamp > this.CACHE_DURATION) {
        console.log(`[SymbolCache] Cache expired for ${assetClass}`);
        await AsyncStorage.removeItem(key);
        return null;
      }

      // Check if cache version matches
      if (version !== this.VERSION) {
        console.log(`[SymbolCache] Cache version mismatch for ${assetClass}`);
        await AsyncStorage.removeItem(key);
        return null;
      }

      console.log(`[SymbolCache] Cache hit for ${assetClass}: ${data.length} symbols`);
      return data as EnhancedSymbolDto[];
    } catch (error) {
      console.error('[SymbolCache] Error reading cache:', error);
      return null;
    }
  }

  /**
   * Set cached symbols for a specific asset class and user
   * @param assetClass The asset class to cache (e.g., 'CRYPTO', 'STOCK')
   * @param data The symbol data to cache
   * @param userId Optional user ID for user-specific preferences
   */
  static async set(assetClass: string, data: EnhancedSymbolDto[], userId?: string): Promise<void> {
    try {
      const key = this.getCacheKey(assetClass, userId);
      const cacheData = {
        data,
        timestamp: Date.now(),
        version: this.VERSION,
      };

      await AsyncStorage.setItem(key, JSON.stringify(cacheData));
      console.log(`[SymbolCache] Cached ${data.length} symbols for ${assetClass}`);
    } catch (error) {
      console.error('[SymbolCache] Error writing cache:', error);
    }
  }

  /**
   * Clear all symbol caches
   */
  static async clear(): Promise<void> {
    try {
      const keys = await AsyncStorage.getAllKeys();
      const symbolKeys = keys.filter(k => k.startsWith(this.CACHE_KEY_PREFIX));

      if (symbolKeys.length > 0) {
        await AsyncStorage.multiRemove(symbolKeys);
        console.log(`[SymbolCache] Cleared ${symbolKeys.length} cache entries`);
      }
    } catch (error) {
      console.error('[SymbolCache] Error clearing cache:', error);
    }
  }

  /**
   * Clear cache for a specific asset class
   * @param assetClass The asset class to clear
   */
  static async clearAssetClass(assetClass: string): Promise<void> {
    try {
      const keys = await AsyncStorage.getAllKeys();
      const assetClassKeys = keys.filter(k =>
        k.startsWith(this.CACHE_KEY_PREFIX) && k.includes(assetClass.toLowerCase())
      );

      if (assetClassKeys.length > 0) {
        await AsyncStorage.multiRemove(assetClassKeys);
        console.log(`[SymbolCache] Cleared ${assetClassKeys.length} cache entries for ${assetClass}`);
      }
    } catch (error) {
      console.error('[SymbolCache] Error clearing asset class cache:', error);
    }
  }

  /**
   * Get the cache key for a specific asset class and user
   * @param assetClass The asset class
   * @param userId Optional user ID
   * @returns The cache key
   */
  private static getCacheKey(assetClass: string, userId?: string): string {
    const userPart = userId || 'default';
    return `${this.CACHE_KEY_PREFIX}${this.VERSION}_${assetClass.toLowerCase()}_${userPart}`;
  }

  /**
   * Get cache statistics for debugging
   * @returns Object with cache statistics
   */
  static async getStats(): Promise<{
    totalCaches: number;
    cacheKeys: string[];
    totalSize: number;
  }> {
    try {
      const keys = await AsyncStorage.getAllKeys();
      const symbolKeys = keys.filter(k => k.startsWith(this.CACHE_KEY_PREFIX));

      let totalSize = 0;
      for (const key of symbolKeys) {
        const value = await AsyncStorage.getItem(key);
        if (value) {
          totalSize += value.length;
        }
      }

      return {
        totalCaches: symbolKeys.length,
        cacheKeys: symbolKeys,
        totalSize,
      };
    } catch (error) {
      console.error('[SymbolCache] Error getting stats:', error);
      return {
        totalCaches: 0,
        cacheKeys: [],
        totalSize: 0,
      };
    }
  }
}
