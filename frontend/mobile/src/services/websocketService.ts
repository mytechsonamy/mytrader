import * as signalR from '@microsoft/signalr';
import { encode } from 'base-64';
import AsyncStorage from '@react-native-async-storage/async-storage';
import Constants from 'expo-constants';
import { WS_BASE_URL as CFG_WS_BASE_URL, API_BASE_URL as CFG_API_BASE_URL } from '../config';
import {
  EnhancedWebSocketMessage, SubscriptionRequest, WebSocketConnectionState,
  UnifiedMarketDataDto, MarketStatusDto, NewsItem, AssetClassType
} from '../types';

interface ConnectionOptions {
  autoReconnect?: boolean;
  maxReconnectAttempts?: number;
  reconnectIntervals?: number[];
  enableHeartbeat?: boolean;
  heartbeatInterval?: number;
  enableLogging?: boolean;
  logLevel?: signalR.LogLevel;
}

interface SubscriptionManager {
  subscriptions: Map<string, SubscriptionRequest>;
  activeSubscriptions: Set<string>;
  pendingSubscriptions: Set<string>;
}

type EventCallback = (data: any) => void;
type ConnectionStatusCallback = (status: 'connected' | 'disconnected' | 'reconnecting' | 'error', message?: string) => void;

class EnhancedWebSocketService {
  private connection: signalR.HubConnection | null = null;
  private connectionState: WebSocketConnectionState = {
    isConnected: false,
    reconnectAttempts: 0,
    subscriptions: [],
  };

  private subscriptionManager: SubscriptionManager = {
    subscriptions: new Map(),
    activeSubscriptions: new Set(),
    pendingSubscriptions: new Set(),
  };

  private eventCallbacks: Map<string, EventCallback[]> = new Map();
  private connectionCallbacks: ConnectionStatusCallback[] = [];
  private heartbeatTimer: NodeJS.Timeout | null = null;
  private reconnectTimer: NodeJS.Timeout | null = null;

  private readonly defaultOptions: ConnectionOptions = {
    autoReconnect: true,
    maxReconnectAttempts: 10,
    reconnectIntervals: [0, 2000, 5000, 10000, 30000],
    enableHeartbeat: true,
    heartbeatInterval: 30000, // 30 seconds
    enableLogging: true,
    logLevel: signalR.LogLevel.Information,
  };

  private options: ConnectionOptions;
  private sessionToken: string | null = null;
  private lastSubscriptionAttempt: { assetClass?: string; symbols?: string[] } | null = null;

  constructor(options: Partial<ConnectionOptions> = {}) {
    this.options = { ...this.defaultOptions, ...options };
  }

  // Initialize the service and establish connection
  async initialize(customHubUrl?: string): Promise<void> {
    try {
      this.sessionToken = await AsyncStorage.getItem('session_token');

      const hubUrl = this.buildHubUrl(customHubUrl);
      await this.createConnection(hubUrl);
      await this.connect();
    } catch (error) {
      console.warn('Failed to initialize WebSocket service:', error);
      // Don't throw error to prevent blocking app initialization
      // Connection will be retried automatically by SignalR
      const errorMessage = error instanceof Error ? error.message : String(error);

      // Provide more specific error handling for mobile
      if (errorMessage.includes('ERR_NAME_NOT_RESOLVED') || errorMessage.includes('ENOTFOUND')) {
        this.notifyConnectionStatus('error', 'Ağ bağlantısı bulunamadı. İnternet bağlantınızı kontrol edin.');
      } else if (errorMessage.includes('ERR_CONNECTION_REFUSED')) {
        this.notifyConnectionStatus('error', 'Sunucuya bağlanılamıyor. Lütfen daha sonra tekrar deneyin.');
      } else {
        this.notifyConnectionStatus('error', `Bağlantı hatası: ${errorMessage}`);
      }

      // Schedule a retry for mobile users
      this.scheduleRetry();
    }
  }

  private scheduleRetry(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
    }

    // Progressive retry delays for mobile
    const retryDelay = Math.min(5000 * Math.pow(2, this.connectionState.reconnectAttempts), 30000);

    this.reconnectTimer = setTimeout(async () => {
      console.log(`Attempting to reconnect (attempt ${this.connectionState.reconnectAttempts + 1})`);
      try {
        await this.initialize();
      } catch (error) {
        console.warn('Retry failed:', error);
      }
    }, retryDelay);
  }

  private buildHubUrl(customUrl?: string): string {
    if (customUrl) return customUrl;

    const API_BASE_URL = Constants.expoConfig?.extra?.API_BASE_URL || CFG_API_BASE_URL || 'http://192.168.68.102:8080/api';
    const configuredHubUrl = Constants.expoConfig?.extra?.WS_BASE_URL || CFG_WS_BASE_URL;
    const rawHubUrl = configuredHubUrl || API_BASE_URL.replace('/api', '/hubs/dashboard');

    // SignalR accepts http(s) or ws(s). Use http(s) for consistent negotiation
    return rawHubUrl.startsWith('wss://')
      ? rawHubUrl.replace('wss://', 'https://')
      : rawHubUrl.startsWith('ws://')
      ? rawHubUrl.replace('ws://', 'http://')
      : rawHubUrl;
  }

  private async createConnection(hubUrl: string): Promise<void> {
    if (this.connection) {
      await this.disconnect();
    }

    console.log('Creating SignalR connection to:', hubUrl);

    const connectionOptions: signalR.IHttpConnectionOptions = {
      skipNegotiation: false,
      transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
      withCredentials: false,
      headers: {
        'X-Client-Type': 'mobile',
        'User-Agent': 'mytrader-mobile/websocket',
        'X-Mobile-Platform': 'react-native',
        'Accept': 'application/json',
      },
    };

    // Add authentication if available
    if (this.sessionToken) {
      connectionOptions.accessTokenFactory = () => Promise.resolve(this.sessionToken || '');
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, connectionOptions)
      .withAutomaticReconnect(this.options.autoReconnect ? (this.options.reconnectIntervals || [0, 2000, 5000, 10000, 30000]) : [])
      .configureLogging(this.options.enableLogging ? this.options.logLevel! : signalR.LogLevel.None)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Connection lifecycle events
    this.connection.onclose((error) => {
      console.log('SignalR connection closed:', error?.message || 'No error');
      this.connectionState.isConnected = false;
      this.connectionState.connectionId = undefined;
      this.stopHeartbeat();
      this.notifyConnectionStatus('disconnected', error?.message);
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting:', error?.message || 'Unknown error');
      this.connectionState.isConnected = false;
      this.connectionState.reconnectAttempts++;
      this.stopHeartbeat();
      this.notifyConnectionStatus('reconnecting', error?.message);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected with ID:', connectionId);
      this.connectionState.isConnected = true;
      this.connectionState.connectionId = connectionId;
      this.connectionState.lastConnected = new Date().toISOString();
      this.connectionState.reconnectAttempts = 0;
      this.startHeartbeat();
      this.reestablishSubscriptions();
      this.notifyConnectionStatus('connected');
    });

    // Message handlers for different data types
    this.setupMessageHandlers();
  }

  private setupMessageHandlers(): void {
    if (!this.connection) return;

    // Price updates with error handling - dual listeners for backend compatibility
    this.connection.on('ReceivePriceUpdate', (data: any) => {
      try {
        // CRITICAL DEBUG: Log RAW data BEFORE any processing
        if (__DEV__) {
          console.log('[WebSocketService] ======= RAW ReceivePriceUpdate =======');
          console.log('[WebSocketService] RAW data type:', typeof data);
          console.log('[WebSocketService] RAW data keys:', Object.keys(data || {}));
          console.log('[WebSocketService] RAW data:', JSON.stringify(data, null, 2));
          console.log('[WebSocketService] Has previousClose?:', 'previousClose' in (data || {}));
          console.log('[WebSocketService] Has PreviousClose?:', 'PreviousClose' in (data || {}));
          console.log('[WebSocketService] previousClose value:', data?.previousClose);
          console.log('[WebSocketService] PreviousClose value:', data?.PreviousClose);
          console.log('[WebSocketService] ==========================================');
        }

        const parsedData = this.safeParseMessageData(data, 'price_update');
        if (parsedData) {
          // Log parsed data to compare with raw
          if (__DEV__) {
            console.log('[WebSocketService] PARSED data keys:', Object.keys(parsedData || {}));
            console.log('[WebSocketService] PARSED previousClose:', parsedData?.previousClose);
            console.log('[WebSocketService] PARSED PreviousClose:', parsedData?.PreviousClose);
          }
          this.emitEvent('price_update', parsedData);
          this.emitEvent('market_data', parsedData); // Legacy compatibility
        }
      } catch (error) {
        console.warn('Failed to process ReceivePriceUpdate:', error);
      }
    });

    // New backend event - PriceUpdate (without Receive prefix)
    this.connection.on('PriceUpdate', (data: any) => {
      try {
        const parsedData = this.safeParseMessageData(data, 'price_update');
        if (parsedData) {
          this.emitEvent('price_update', parsedData);
          this.emitEvent('market_data', parsedData); // Legacy compatibility
        }
      } catch (error) {
        console.warn('Failed to process PriceUpdate:', error);
      }
    });

    // Additional market data event listener
    this.connection.on('MarketDataUpdate', (data: any) => {
      try {
        const parsedData = this.safeParseMessageData(data, 'market_data_update');
        if (parsedData) {
          this.emitEvent('market_data_update', parsedData);
          this.emitEvent('market_data', parsedData); // Legacy compatibility
        }
      } catch (error) {
        console.warn('Failed to process MarketDataUpdate:', error);
      }
    });

    this.connection.on('ReceiveBatchPriceUpdate', (data: any) => {
      try {
        const parsedData = this.safeParseMessageData(data, 'batch_price_update');
        if (parsedData && Array.isArray(parsedData)) {
          this.emitEvent('batch_price_update', parsedData);
          parsedData.forEach((item: any) => {
            if (item) this.emitEvent('price_update', item);
          });
        }
      } catch (error) {
        console.warn('Failed to process batch price update:', error);
      }
    });

    // Market status updates with error handling
    this.connection.on('ReceiveMarketStatusUpdate', (data: any) => {
      try {
        const parsedData = this.safeParseMessageData(data, 'market_status_update');
        if (parsedData) {
          this.emitEvent('market_status_update', parsedData);
        }
      } catch (error) {
        console.warn('Failed to process market status update:', error);
      }
    });

    // News updates with error handling
    this.connection.on('ReceiveNewsUpdate', (data: any) => {
      try {
        const parsedData = this.safeParseMessageData(data, 'news_update');
        if (parsedData) {
          this.emitEvent('news_update', parsedData);
        }
      } catch (error) {
        console.warn('Failed to process news update:', error);
      }
    });

    // Subscription confirmations
    this.connection.on('SubscriptionConfirmed', (data: { subscriptionId: string; type: string }) => {
      this.subscriptionManager.activeSubscriptions.add(data.subscriptionId);
      this.subscriptionManager.pendingSubscriptions.delete(data.subscriptionId);
      this.emitEvent('subscription_confirmed', data);
    });

    this.connection.on('SubscriptionError', (data: { error: string; subscriptionType: string }) => {
      console.error('Subscription error:', data);
      this.emitEvent('subscription_error', data);
    });

    // Also handle lowercase 'subscriptionerror' for backward compatibility
    this.connection.on('subscriptionerror', (error: any) => {
      console.error('[SignalR] Subscription error (lowercase event):', {
        error,
        attemptedSymbols: this.lastSubscriptionAttempt?.symbols,
        assetClass: this.lastSubscriptionAttempt?.assetClass
      });
      this.emitEvent('subscription_error', { error: error?.message || error, subscriptionType: 'unknown' });
    });

    // Heartbeat
    this.connection.on('Heartbeat', (data: { timestamp: string }) => {
      if (__DEV__) {
        console.log('[SignalR] Heartbeat received:', data.timestamp);
      }
      this.emitEvent('heartbeat', data);
    });

    // Connection status updates
    this.connection.on('connectionstatus', (data: { status: string; message?: string }) => {
      console.log('[SignalR] Connection status update:', data);
      this.emitEvent('connectionstatus', data);
      
      // Update internal connection state based on status
      if (data.status === 'connected') {
        this.connectionState.isConnected = true;
      } else if (data.status === 'disconnected' || data.status === 'error') {
        this.connectionState.isConnected = false;
      }
    });

    // Legacy compatibility handlers with error handling
    this.connection.on('ReceiveSignalUpdate', (data: any) => {
      try {
        const parsedData = this.safeParseMessageData(data, 'signal');
        if (parsedData) {
          this.emitEvent('signal', parsedData);
        }
      } catch (error) {
        console.warn('Failed to process signal update:', error);
      }
    });

    this.connection.on('ReceiveMarketData', (data: any) => {
      try {
        const parsedData = this.safeParseMessageData(data, 'market');
        if (parsedData) {
          this.emitEvent('market', parsedData);
          this.emitEvent('market_data', parsedData); // Additional compatibility
        }
      } catch (error) {
        console.warn('Failed to process ReceiveMarketData:', error);
      }
    });

    // Simplified MarketData event (without Receive prefix)
    this.connection.on('MarketData', (data: any) => {
      try {
        const parsedData = this.safeParseMessageData(data, 'market');
        if (parsedData) {
          this.emitEvent('market', parsedData);
          this.emitEvent('market_data', parsedData); // Additional compatibility
        }
      } catch (error) {
        console.warn('Failed to process MarketData:', error);
      }
    });
  }

  // Connection management
  async connect(): Promise<void> {
    if (!this.connection) {
      console.warn('Connection not initialized. Call initialize() first.');
      return;
    }

    try {
      await this.connection.start();
      console.log('SignalR connection established');

      this.connectionState.isConnected = true;
      this.connectionState.connectionId = this.connection.connectionId || undefined;
      this.connectionState.lastConnected = new Date().toISOString();
      this.connectionState.reconnectAttempts = 0;

      // Clear any retry timers on successful connection
      if (this.reconnectTimer) {
        clearTimeout(this.reconnectTimer);
        this.reconnectTimer = null;
      }

      this.startHeartbeat();
      this.notifyConnectionStatus('connected');

      // Establish any pending subscriptions
      await this.reestablishSubscriptions();
    } catch (error) {
      console.warn('Failed to start SignalR connection:', error);
      this.connectionState.isConnected = false;
      this.connectionState.reconnectAttempts++;

      const errorMessage = error instanceof Error ? error.message : String(error);

      // Mobile-specific error messages
      let userFriendlyMessage = 'Bağlantı kurulurken hata oluştu.';

      if (errorMessage.includes('Failed to negotiate') || errorMessage.includes('WebSocket')) {
        userFriendlyMessage = 'WebSocket bağlantısı kurulamadı. Ağ ayarlarınızı kontrol edin.';
      } else if (errorMessage.includes('timeout') || errorMessage.includes('ETIMEDOUT')) {
        userFriendlyMessage = 'Bağlantı zaman aşımı. İnternet hızınızı kontrol edin.';
      } else if (errorMessage.includes('ECONNREFUSED')) {
        userFriendlyMessage = 'Sunucu erişilemez durumda.';
      }

      this.notifyConnectionStatus('error', userFriendlyMessage);

      // Schedule retry if under max attempts
      if (this.connectionState.reconnectAttempts < (this.options.maxReconnectAttempts || 10)) {
        this.scheduleRetry();
      }
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      this.stopHeartbeat();
      await this.connection.stop();
      this.connection = null;
    }

    this.connectionState.isConnected = false;
    this.connectionState.connectionId = undefined;
    this.subscriptionManager.activeSubscriptions.clear();
    this.subscriptionManager.pendingSubscriptions.clear();
  }

  // Subscription management
  async subscribe(request: SubscriptionRequest): Promise<string> {
    const subscriptionId = this.generateSubscriptionId(request);

    this.subscriptionManager.subscriptions.set(subscriptionId, request);
    this.connectionState.subscriptions.push(request);

    if (this.connectionState.isConnected && this.connection) {
      try {
        await this.sendSubscriptionRequest(subscriptionId, request);
        this.subscriptionManager.pendingSubscriptions.add(subscriptionId);
      } catch (error) {
        console.error('Failed to send subscription request:', error);
        throw error;
      }
    }

    return subscriptionId;
  }

  async unsubscribe(subscriptionId: string): Promise<void> {
    const request = this.subscriptionManager.subscriptions.get(subscriptionId);
    if (!request) return;

    if (this.connectionState.isConnected && this.connection) {
      try {
        await this.sendUnsubscriptionRequest(subscriptionId, request);
      } catch (error) {
        console.error('Failed to send unsubscription request:', error);
      }
    }

    this.subscriptionManager.subscriptions.delete(subscriptionId);
    this.subscriptionManager.activeSubscriptions.delete(subscriptionId);
    this.subscriptionManager.pendingSubscriptions.delete(subscriptionId);

    // Remove from connection state
    this.connectionState.subscriptions = this.connectionState.subscriptions.filter(
      sub => this.generateSubscriptionId(sub) !== subscriptionId
    );
  }

  // Convenient subscription methods
  async subscribeToPrices(symbols: string[], assetClass?: AssetClassType): Promise<string> {
    return this.subscribe({
      action: 'subscribe',
      subscriptionType: 'prices',
      symbols,
      filters: assetClass ? { assetClass } : undefined,
    });
  }

  // ENHANCED: Subscribe to live crypto data specifically (matches backend symbols)
  async subscribeToCryptoUpdates(): Promise<string> {
    const cryptoSymbols = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT', 'AVAXUSDT'];
    console.log('Subscribing to crypto price updates for symbols:', cryptoSymbols);

    // Track this subscription attempt for debugging
    this.lastSubscriptionAttempt = { assetClass: 'CRYPTO', symbols: cryptoSymbols };

    if (this.connection && this.connectionState.isConnected) {
      try {
        // Use the specific method that matches the backend SignalR hub
        console.log('Invoking SubscribeToPriceUpdates with:', { assetClass: 'CRYPTO', symbols: cryptoSymbols });
        await this.connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', cryptoSymbols);
        console.log('Successfully invoked SubscribeToPriceUpdates for CRYPTO');
        return 'crypto-subscription';
      } catch (error) {
        console.error('Failed to subscribe to crypto updates:', {
          error,
          attemptedSymbols: cryptoSymbols,
          assetClass: 'CRYPTO'
        });
        throw error;
      }
    } else {
      console.warn('Cannot subscribe to crypto updates - connection not established');
      throw new Error('WebSocket connection not established');
    }
  }

  async subscribeToMarketStatus(markets?: string[]): Promise<string> {
    return this.subscribe({
      action: 'subscribe',
      subscriptionType: 'market_status',
      markets,
    });
  }

  async subscribeToNews(filters?: {
    assetClasses?: string[];
    symbols?: string[];
    categories?: string[];
    importance?: 'LOW' | 'MEDIUM' | 'HIGH';
  }): Promise<string> {
    return this.subscribe({
      action: 'subscribe',
      subscriptionType: 'news',
      filters,
    });
  }

  private async sendSubscriptionRequest(subscriptionId: string, request: SubscriptionRequest): Promise<void> {
    if (!this.connection) throw new Error('Connection not available');

    const payload = {
      subscriptionId,
      ...request,
    };

    await this.connection.invoke('Subscribe', payload);
  }

  private async sendUnsubscriptionRequest(subscriptionId: string, request: SubscriptionRequest): Promise<void> {
    if (!this.connection) throw new Error('Connection not available');

    const payload = {
      subscriptionId,
      action: 'unsubscribe',
      subscriptionType: request.subscriptionType,
    };

    await this.connection.invoke('Unsubscribe', payload);
  }

  private async reestablishSubscriptions(): Promise<void> {
    console.log('Reestablishing subscriptions...');

    const subscriptions = Array.from(this.subscriptionManager.subscriptions.entries());

    for (const [subscriptionId, request] of subscriptions) {
      try {
        await this.sendSubscriptionRequest(subscriptionId, request);
        this.subscriptionManager.pendingSubscriptions.add(subscriptionId);
      } catch (error) {
        console.error(`Failed to reestablish subscription ${subscriptionId}:`, error);
      }
    }
  }

  private generateSubscriptionId(request: SubscriptionRequest): string {
    const components = [
      request.subscriptionType,
      request.symbols?.join(',') || '',
      request.assetClasses?.join(',') || '',
      request.markets?.join(',') || '',
      JSON.stringify(request.filters || {}),
    ];

    return encode(components.join('|')).replace(/[+/=]/g, '');
  }

  // Event handling
  on(event: string, callback: EventCallback): void {
    if (!this.eventCallbacks.has(event)) {
      this.eventCallbacks.set(event, []);
    }
    this.eventCallbacks.get(event)!.push(callback);
  }

  off(event: string, callback?: EventCallback): void {
    if (!this.eventCallbacks.has(event)) return;

    if (callback) {
      const callbacks = this.eventCallbacks.get(event)!;
      const index = callbacks.indexOf(callback);
      if (index > -1) {
        callbacks.splice(index, 1);
      }
    } else {
      this.eventCallbacks.delete(event);
    }
  }

  onConnectionStatus(callback: ConnectionStatusCallback): void {
    this.connectionCallbacks.push(callback);
  }

  offConnectionStatus(callback: ConnectionStatusCallback): void {
    const index = this.connectionCallbacks.indexOf(callback);
    if (index > -1) {
      this.connectionCallbacks.splice(index, 1);
    }
  }

  private safeParseMessageData(data: any, messageType: string): any {
    try {
      // If data is already an object, return it
      if (typeof data === 'object' && data !== null) {
        return data;
      }

      // If data is a string, try to parse it as JSON
      if (typeof data === 'string') {
        // Handle empty or malformed strings
        if (!data.trim()) {
          console.warn(`Empty message received for ${messageType}`);
          return null;
        }

        // Check for common malformed JSON patterns
        if (data.includes('\"') || data.includes('\\')) {
          // Try to fix escaped quotes
          const cleanedData = data.replace(/\\"/g, '"').replace(/\\\\/g, '\\');
          return JSON.parse(cleanedData);
        }

        return JSON.parse(data);
      }

      // For other types, return as-is
      return data;
    } catch (error) {
      console.error(`Failed to parse WebSocket message for ${messageType}:`, {
        error: error instanceof Error ? error.message : String(error),
        rawData: typeof data === 'string' ? data.substring(0, 200) : data
      });
      return null;
    }
  }

  private emitEvent(event: string, data: any): void {
    const callbacks = this.eventCallbacks.get(event);
    if (callbacks) {
      callbacks.forEach(callback => {
        try {
          callback(data);
        } catch (error) {
          console.error(`Error in event callback for ${event}:`, error);
        }
      });
    }
  }

  private notifyConnectionStatus(status: 'connected' | 'disconnected' | 'reconnecting' | 'error', message?: string): void {
    this.connectionCallbacks.forEach(callback => {
      try {
        callback(status, message);
      } catch (error) {
        console.error('Error in connection status callback:', error);
      }
    });
  }

  // Heartbeat management
  private startHeartbeat(): void {
    if (!this.options.enableHeartbeat) return;

    this.stopHeartbeat();
    this.heartbeatTimer = setInterval(() => {
      this.sendHeartbeat();
    }, this.options.heartbeatInterval!);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  private async sendHeartbeat(): Promise<void> {
    if (this.connection && this.connectionState.isConnected) {
      try {
        await this.connection.invoke('Ping');
      } catch (error) {
        console.warn('Heartbeat failed:', error);
      }
    }
  }

  // Utility methods
  getConnectionState(): WebSocketConnectionState {
    return { ...this.connectionState };
  }

  getActiveSubscriptions(): string[] {
    return Array.from(this.subscriptionManager.activeSubscriptions);
  }

  isConnected(): boolean {
    return this.connectionState.isConnected;
  }

  // Direct subscription method for specific asset class and symbols
  async subscribeToAssetClass(assetClass: string, symbols: string[]): Promise<void> {
    if (this.connection && this.connectionState.isConnected) {
      console.log(`[WebSocket] Subscribing to ${assetClass} with symbols:`, symbols);
      try {
        await this.connection.invoke('SubscribeToPriceUpdates', assetClass, symbols);
        console.log(`[WebSocket] Successfully subscribed to ${assetClass} price updates`);
      } catch (error) {
        console.error(`[WebSocket] Failed to subscribe to ${assetClass}:`, error);
        throw error;
      }
    } else {
      console.warn('[WebSocket] Cannot subscribe - connection not established');
      throw new Error('WebSocket connection not established');
    }
  }

  async updateToken(newToken: string): Promise<void> {
    this.sessionToken = newToken;
    await AsyncStorage.setItem('session_token', newToken);

    // Reconnect with new token
    if (this.connection) {
      await this.disconnect();
      await this.initialize();
    }
  }

  // Cleanup
  async destroy(): Promise<void> {
    this.stopHeartbeat();

    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    await this.disconnect();

    this.eventCallbacks.clear();
    this.connectionCallbacks.length = 0;
    this.subscriptionManager.subscriptions.clear();
    this.subscriptionManager.activeSubscriptions.clear();
    this.subscriptionManager.pendingSubscriptions.clear();
  }
}

// Singleton instance
export const websocketService = new EnhancedWebSocketService({
  autoReconnect: true,
  maxReconnectAttempts: 10,
  enableHeartbeat: true,
  heartbeatInterval: 30000,
  enableLogging: __DEV__,
  logLevel: __DEV__ ? signalR.LogLevel.Information : signalR.LogLevel.Warning,
});

// Export types for external use
export type {
  ConnectionOptions,
  EventCallback,
  ConnectionStatusCallback,
  EnhancedWebSocketMessage,
  SubscriptionRequest,
  WebSocketConnectionState,
};
