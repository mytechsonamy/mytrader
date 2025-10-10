import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { MarketDataDto } from './marketDataService';
import { normalizeMarketData } from '../utils/priceFormatting';

export interface WebSocketConnectionConfig {
  url: string;
  onMarketDataUpdate?: (data: MarketDataDto) => void;
  onConnectionStatusChange?: (connected: boolean) => void;
  onError?: (error: Error) => void;
}

export class WebSocketService {
  private connection: HubConnection | null = null;
  private config: WebSocketConnectionConfig;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10; // Increased attempts
  private reconnectDelay = 1000; // Start with 1 second
  private isDestroyed = false;
  private heartbeatInterval: NodeJS.Timeout | undefined;

  constructor(config: WebSocketConnectionConfig) {
    this.config = config;
  }

  async connect(): Promise<void> {
    if (this.isDestroyed) {
      console.log('WebSocket service is destroyed, not connecting');
      return;
    }

    if (this.connection && this.connection.state === 'Connected') {
      console.log('WebSocket already connected');
      return;
    }

    // Don't attempt to connect if we've exceeded max attempts
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.error('Max reconnection attempts reached, not attempting to connect');
      this.config.onError?.(new Error('Failed to establish connection after maximum retry attempts'));
      return;
    }

    try {
      // Clean up existing connection if any
      if (this.connection) {
        try {
          await this.connection.stop();
        } catch (stopError) {
          console.warn('Error stopping existing connection:', stopError);
        }
        this.connection = null;
      }

      this.connection = new HubConnectionBuilder()
        .withUrl(this.config.url)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
              return null; // Stop retrying
            }
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          }
        })
        .configureLogging(LogLevel.Information)
        .build();

      // Set up event handlers
      this.setupEventHandlers();

      // Add timeout for connection attempt
      const connectPromise = this.connection.start();
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Connection timeout after 10 seconds')), 10000);
      });

      await Promise.race([connectPromise, timeoutPromise]);
      console.log('WebSocket connected successfully');

      this.reconnectAttempts = 0;
      this.startHeartbeat();
      this.config.onConnectionStatusChange?.(true);

    } catch (error) {
      console.error('Failed to connect WebSocket:', error);

      // Clean up failed connection
      if (this.connection) {
        try {
          await this.connection.stop();
        } catch (cleanupError) {
          console.warn('Error cleaning up failed connection:', cleanupError);
        }
        this.connection = null;
      }

      // Create a more user-friendly error message
      const friendlyError = this.createFriendlyError(error as Error);
      this.config.onError?.(friendlyError);
      this.config.onConnectionStatusChange?.(false);

      // Only attempt reconnection for certain types of errors
      if (this.shouldAttemptReconnect(error as Error)) {
        this.scheduleReconnect();
      }
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Handle market data updates - support both new and legacy event names
    const handlePriceUpdate = (payload: any) => {
      try {
        // Validate payload exists and has required fields
        if (!payload || (typeof payload !== 'object')) {
          console.warn('Invalid payload received:', payload);
          return;
        }

        // Normalize incoming payload to MarketDataDto expected by UI
        // Supports both MultiAssetPriceUpdate ({ Symbol, Price, Change24h, Volume, Timestamp, Source })
        // and legacy crypto shape ({ symbol, price, change, volume, timestamp })
        const isMultiAsset = payload && typeof payload.Symbol === 'string';
        const symbol = isMultiAsset ? payload.Symbol : payload.symbol;

        // Validate required fields
        if (!symbol || typeof symbol !== 'string') {
          console.warn('Invalid symbol in payload:', payload);
          return;
        }

        // Normalize the payload using locale-independent price parsing
        // This fixes Turkish locale decimal parsing issues (. vs , separator)
        const rawData = {
          symbol,
          price: isMultiAsset ? payload.Price : payload.price,
          change: isMultiAsset ? payload.Change24h : (payload.changePercent ?? 0),
          volume: isMultiAsset ? payload.Volume : (payload.volume ?? 0),
          assetClass: payload.assetClass || 'CRYPTO'
        };

        console.log('[WebSocket] RAW payload for', symbol, ':', {
          rawPrice: rawData.price,
          rawPriceType: typeof rawData.price,
          fullPayload: payload
        });

        const normalizedData = normalizeMarketData(rawData);

        console.log('[WebSocket] NORMALIZED for', symbol, ':', {
          price: normalizedData.price,
          priceType: typeof normalizedData.price,
          changePercent: normalizedData.changePercent,
          volume: normalizedData.volume
        });

        // Validate normalized price
        if (!Number.isFinite(normalizedData.price) || normalizedData.price < 0) {
          console.warn('Invalid price after normalization:', payload);
          return;
        }

        const lastUpdate = (isMultiAsset ? payload.Timestamp : payload.timestamp) || new Date().toISOString();

        const normalized: MarketDataDto = {
          symbol,
          displayName: symbol,
          price: normalizedData.price,
          priceChange: 0, // not provided in update; UI mostly uses percent
          priceChangePercent: normalizedData.changePercent,
          volume: normalizedData.volume,
          lastUpdate: typeof lastUpdate === 'string' ? lastUpdate : new Date(lastUpdate).toISOString(),
          source: 'alpaca', // keep union happy; will treat as live
          isRealTime: true,
        };

        this.config.onMarketDataUpdate?.(normalized);
      } catch (error) {
        console.error('Error handling market data update:', error, 'Payload:', payload);
      }
    };

    // Listen for both new and legacy event names for backward compatibility
    this.connection.on('PriceUpdate', handlePriceUpdate);
    this.connection.on('ReceivePriceUpdate', handlePriceUpdate);

    // Handle connection state changes
    this.connection.onclose((error) => {
      console.log('WebSocket connection closed:', error);
      this.config.onConnectionStatusChange?.(false);

      if (error) {
        this.scheduleReconnect();
      }
    });

    this.connection.onreconnecting((error) => {
      console.log('WebSocket reconnecting:', error);
      this.config.onConnectionStatusChange?.(false);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('WebSocket reconnected:', connectionId);
      this.config.onConnectionStatusChange?.(true);
      this.reconnectAttempts = 0;
    });
  }

  private startHeartbeat(): void {
    this.stopHeartbeat();
    this.heartbeatInterval = setInterval(() => {
      if (this.connection && this.connection.state === 'Connected') {
        // Send a ping to keep connection alive
        this.connection.invoke('Ping').catch((error) => {
          console.warn('Heartbeat ping failed:', error);
        });
      }
    }, 30000); // Ping every 30 seconds
  }

  private stopHeartbeat(): void {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval);
      this.heartbeatInterval = undefined;
    }
  }

  private scheduleReconnect(): void {
    if (this.isDestroyed || this.reconnectAttempts >= this.maxReconnectAttempts) {
      if (!this.isDestroyed) {
        console.error('Max reconnection attempts reached');
        this.config.onError?.(new Error('Failed to reconnect after maximum attempts'));
      }
      return;
    }

    this.reconnectAttempts++;
    const delay = Math.min(this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1), 30000);

    console.log(`Scheduling WebSocket reconnection attempt ${this.reconnectAttempts} in ${delay}ms`);

    setTimeout(() => {
      if (!this.isDestroyed) {
        this.connect().catch((error) => {
          console.error('Reconnection attempt failed:', error);
        });
      }
    }, delay);
  }

  async subscribeToSymbol(symbolId: string): Promise<void> {
    if (!this.connection || this.connection.state !== 'Connected') {
      throw new Error('WebSocket not connected');
    }

    try {
      // Use the correct SignalR hub method names from MarketDataHub
      await this.connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', [symbolId]);
      console.log(`Subscribed to market data for symbol: ${symbolId}`);
    } catch (error) {
      console.error(`Failed to subscribe to symbol ${symbolId}:`, error);
      throw error;
    }
  }

  async unsubscribeFromSymbol(symbolId: string): Promise<void> {
    if (!this.connection || this.connection.state !== 'Connected') {
      return; // Connection already closed
    }

    try {
      // Use the correct SignalR hub method names from MarketDataHub
      await this.connection.invoke('UnsubscribeFromPriceUpdates', 'CRYPTO', [symbolId]);
      console.log(`Unsubscribed from market data for symbol: ${symbolId}`);
    } catch (error) {
      console.error(`Failed to unsubscribe from symbol ${symbolId}:`, error);
    }
  }

  async subscribeToMultipleSymbols(symbolIds: string[]): Promise<void> {
    if (!this.connection || this.connection.state !== 'Connected') {
      throw new Error('WebSocket not connected');
    }

    try {
      // Subscribe to all symbols at once using the correct method
      await this.connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', symbolIds);
      console.log(`Subscribed to market data for symbols: ${symbolIds.join(', ')}`);
    } catch (error) {
      console.error('Failed to subscribe to multiple symbols:', error);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    try {
      this.stopHeartbeat();
      await this.connection.stop();
      console.log('WebSocket disconnected');
      this.config.onConnectionStatusChange?.(false);
    } catch (error) {
      console.error('Error disconnecting WebSocket:', error);
    } finally {
      this.connection = null;
      this.reconnectAttempts = 0;
    }
  }

  destroy(): void {
    this.isDestroyed = true;
    this.stopHeartbeat();
    this.disconnect();
  }

  isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  getConnectionState(): string {
    return this.connection?.state || 'Disconnected';
  }

  private createFriendlyError(error: Error): Error {
    // Create more user-friendly error messages
    if (error.message.includes('Failed to negotiate')) {
      return new Error('Unable to connect to market data service. Please check your internet connection.');
    }
    if (error.message.includes('WebSocket failed to connect')) {
      return new Error('Real-time data connection unavailable. Using backup data polling.');
    }
    if (error.message.includes('ERR_NETWORK') || error.message.includes('ERR_NAME_NOT_RESOLVED')) {
      return new Error('Network error. Please check your connection and try again.');
    }
    if (error.message.includes('502') || error.message.includes('503') || error.message.includes('504')) {
      return new Error('Market data service temporarily unavailable. Will retry automatically.');
    }
    if (error.message.includes('timeout')) {
      return new Error('Connection timeout. Please check your network and try again.');
    }
    if (error.message.includes('refused')) {
      return new Error('Connection refused by server. Service may be temporarily down.');
    }

    // Return original error if we can't make it more friendly
    return error;
  }

  private shouldAttemptReconnect(error: Error): boolean {
    // Don't attempt reconnection for certain types of errors
    if (error.message.includes('401') || error.message.includes('403')) {
      return false; // Authentication errors
    }
    if (error.message.includes('Failed to negotiate')) {
      return false; // SignalR negotiation failed - likely server issue
    }

    return true; // Attempt reconnection for other errors
  }
}

// Create a singleton instance
let websocketServiceInstance: WebSocketService | null = null;

export const createWebSocketService = (config: WebSocketConnectionConfig): WebSocketService => {
  if (websocketServiceInstance) {
    websocketServiceInstance.destroy();
  }

  websocketServiceInstance = new WebSocketService(config);
  return websocketServiceInstance;
};

export const getWebSocketService = (): WebSocketService | null => {
  return websocketServiceInstance;
};
