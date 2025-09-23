import * as signalR from '@microsoft/signalr';
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
      console.error('Failed to initialize WebSocket service:', error);
      throw error;
    }
  }

  private buildHubUrl(customUrl?: string): string {
    if (customUrl) return customUrl;

    const API_BASE_URL = Constants.expoConfig?.extra?.API_BASE_URL || CFG_API_BASE_URL || 'http://localhost:8080/api';
    const configuredHubUrl = Constants.expoConfig?.extra?.WS_BASE_URL || CFG_WS_BASE_URL;
    const rawHubUrl = configuredHubUrl || API_BASE_URL.replace('/api', '/hubs/trading');

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
    };

    // Add authentication if available
    if (this.sessionToken) {
      connectionOptions.accessTokenFactory = () => Promise.resolve(this.sessionToken || '');
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, connectionOptions)
      .withAutomaticReconnect(this.options.autoReconnect ? (this.options.reconnectIntervals || [0, 2000, 5000, 10000, 30000]) : undefined)
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

    // Price updates
    this.connection.on('ReceivePriceUpdate', (data: UnifiedMarketDataDto) => {
      this.emitEvent('price_update', data);
      this.emitEvent('market_data', data); // Legacy compatibility
    });

    this.connection.on('ReceiveBatchPriceUpdate', (data: UnifiedMarketDataDto[]) => {
      this.emitEvent('batch_price_update', data);
      data.forEach(item => this.emitEvent('price_update', item));
    });

    // Market status updates
    this.connection.on('ReceiveMarketStatusUpdate', (data: MarketStatusDto) => {
      this.emitEvent('market_status_update', data);
    });

    // News updates
    this.connection.on('ReceiveNewsUpdate', (data: NewsItem) => {
      this.emitEvent('news_update', data);
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

    // Heartbeat
    this.connection.on('Heartbeat', (data: { timestamp: string }) => {
      this.emitEvent('heartbeat', data);
    });

    // Legacy compatibility handlers
    this.connection.on('ReceiveSignalUpdate', (data: any) => {
      this.emitEvent('signal', data);
    });

    this.connection.on('ReceiveMarketData', (data: any) => {
      this.emitEvent('market', data);
    });
  }

  // Connection management
  async connect(): Promise<void> {
    if (!this.connection) {
      throw new Error('Connection not initialized. Call initialize() first.');
    }

    try {
      await this.connection.start();
      console.log('SignalR connection established');

      this.connectionState.isConnected = true;
      this.connectionState.connectionId = this.connection.connectionId || undefined;
      this.connectionState.lastConnected = new Date().toISOString();
      this.connectionState.reconnectAttempts = 0;

      this.startHeartbeat();
      this.notifyConnectionStatus('connected');

      // Establish any pending subscriptions
      await this.reestablishSubscriptions();
    } catch (error) {
      console.error('Failed to start SignalR connection:', error);
      this.notifyConnectionStatus('error', (error as Error).message);
      throw error;
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

    return btoa(components.join('|')).replace(/[+/=]/g, '');
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