import * as signalR from '@microsoft/signalr';

export interface SignalRConnectionConfig {
  url: string;
  hubPath: string;
  onConnected?: () => void;
  onDisconnected?: (error?: Error) => void;
  onReconnecting?: () => void;
  onReconnected?: () => void;
  onError?: (error: Error) => void;
}

export interface DashboardSnapshot {
  portfolio?: any;
  recentSignals?: any[];
  activeStrategies?: any[];
  marketOverview?: any;
  timestamp: string;
}

export interface IndicatorUpdate {
  symbol: string;
  timeframe: string;
  price: number;
  timestamp: string;
  updatedIndicators: Record<string, any>;
}

export interface SignalNotification {
  symbol: string;
  type: string;
  confidence: number;
  price: number;
  source: string;
  generatedAt: string;
  reason: string;
}

export interface PortfolioUpdate {
  totalValue: number;
  dayChange: number;
  dayChangePercent: number;
  totalReturn: number;
  totalReturnPercent: number;
  activePositions: number;
  cashBalance: number;
  lastUpdated: string;
}

export class EnhancedSignalRService {
  private marketConnection: signalR.HubConnection | null = null;
  private dashboardConnection: signalR.HubConnection | null = null;
  private connectionCallbacks: Map<string, (() => void)[]> = new Map();
  private isConnecting: Map<string, boolean> = new Map();

  async connectToMarketDataHub(config: SignalRConnectionConfig): Promise<signalR.HubConnection> {
    return this.connectToHub('market', config, '/hubs/marketdata');
  }

  async connectToDashboardHub(config: SignalRConnectionConfig): Promise<signalR.HubConnection> {
    return this.connectToHub('dashboard', config, '/hubs/dashboard');
  }

  private async connectToHub(
    hubType: 'market' | 'dashboard',
    config: SignalRConnectionConfig,
    defaultPath: string
  ): Promise<signalR.HubConnection> {
    const connectionKey = hubType;
    const existingConnection = hubType === 'market' ? this.marketConnection : this.dashboardConnection;

    if (existingConnection && existingConnection.state === signalR.HubConnectionState.Connected) {
      return existingConnection;
    }

    if (this.isConnecting.get(connectionKey)) {
      // Wait for existing connection attempt
      return new Promise((resolve) => {
        const callbacks = this.connectionCallbacks.get(connectionKey) || [];
        callbacks.push(() => {
          const connection = hubType === 'market' ? this.marketConnection : this.dashboardConnection;
          if (connection) {
            resolve(connection);
          }
        });
        this.connectionCallbacks.set(connectionKey, callbacks);
      });
    }

    this.isConnecting.set(connectionKey, true);

    try {
      const hubPath = config.hubPath || defaultPath;
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${config.url}${hubPath}`, {
          withCredentials: false,
          headers: {
            'Access-Control-Allow-Origin': '*'
          }
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount < 5) {
              return 1000 * Math.pow(2, retryContext.previousRetryCount); // Exponential backoff
            }
            return 30000; // Max 30 seconds
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Connection event handlers
      connection.onreconnecting((error) => {
        console.log(`SignalR ${hubType}: Reconnecting...`, error);
        config.onReconnecting?.();
      });

      connection.onreconnected((connectionId) => {
        console.log(`SignalR ${hubType}: Reconnected successfully`, connectionId);
        config.onReconnected?.();
      });

      connection.onclose((error) => {
        console.log(`SignalR ${hubType}: Connection closed`, error);
        this.isConnecting.set(connectionKey, false);
        config.onDisconnected?.(error || undefined);
      });

      await connection.start();
      console.log(`SignalR: Connected to ${hubType} Hub`);

      // Store connection
      if (hubType === 'market') {
        this.marketConnection = connection;
      } else {
        this.dashboardConnection = connection;
      }

      this.isConnecting.set(connectionKey, false);
      config.onConnected?.();

      // Notify waiting callbacks
      const callbacks = this.connectionCallbacks.get(connectionKey) || [];
      callbacks.forEach(callback => callback());
      this.connectionCallbacks.set(connectionKey, []);

      return connection;
    } catch (error) {
      console.error(`SignalR ${hubType}: Connection failed:`, error);
      this.isConnecting.set(connectionKey, false);
      config.onError?.(error as Error);
      throw error;
    }
  }

  // === Market Data Hub Methods ===

  async subscribeToMarketDataPriceUpdates(assetClass: string, symbols: string[]): Promise<void> {
    if (!this.marketConnection || this.marketConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Market data hub not connected');
    }

    try {
      await this.marketConnection.invoke('SubscribeToPriceUpdates', assetClass, symbols);
      console.log(`Subscribed to ${assetClass} price updates for symbols:`, symbols);
    } catch (error) {
      console.error('Failed to subscribe to price updates:', error);
      throw error;
    }
  }

  async subscribeToAssetClass(assetClass: string): Promise<void> {
    if (!this.marketConnection || this.marketConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Market data hub not connected');
    }

    try {
      await this.marketConnection.invoke('SubscribeToAssetClass', assetClass);
      console.log(`Subscribed to all ${assetClass} symbols`);
    } catch (error) {
      console.error('Failed to subscribe to asset class:', error);
      throw error;
    }
  }

  async unsubscribeFromPriceUpdates(assetClass: string, symbols: string[]): Promise<void> {
    if (!this.marketConnection || this.marketConnection.state !== signalR.HubConnectionState.Connected) {
      return; // Connection already closed
    }

    try {
      await this.marketConnection.invoke('UnsubscribeFromPriceUpdates', assetClass, symbols);
      console.log(`Unsubscribed from ${assetClass} price updates for symbols:`, symbols);
    } catch (error) {
      console.error('Failed to unsubscribe from price updates:', error);
    }
  }

  async subscribeToMarketStatus(markets: string[]): Promise<void> {
    if (!this.marketConnection || this.marketConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Market data hub not connected');
    }

    try {
      await this.marketConnection.invoke('SubscribeToMarketStatus', markets);
      console.log('Subscribed to market status for:', markets);
    } catch (error) {
      console.error('Failed to subscribe to market status:', error);
      throw error;
    }
  }

  async getMarketStatus(): Promise<void> {
    if (!this.marketConnection || this.marketConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Market data hub not connected');
    }

    try {
      await this.marketConnection.invoke('GetMarketStatus');
      console.log('Requested current market status');
    } catch (error) {
      console.error('Failed to get market status:', error);
      throw error;
    }
  }

  async pingMarketDataHub(): Promise<void> {
    if (!this.marketConnection || this.marketConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Market data hub not connected');
    }

    try {
      await this.marketConnection.invoke('Ping');
      console.log('Market data hub pinged successfully');
    } catch (error) {
      console.error('Failed to ping market data hub:', error);
      throw error;
    }
  }

  // Market Data Event Listeners
  onPriceUpdate(callback: (data: any) => void): void {
    if (!this.marketConnection) return;
    this.marketConnection.on('PriceUpdate', callback);
  }

  onConnectionStatus(callback: (status: any) => void): void {
    if (!this.marketConnection) return;
    this.marketConnection.on('ConnectionStatus', callback);
  }

  onSubscriptionConfirmed(callback: (data: any) => void): void {
    if (!this.marketConnection) return;
    this.marketConnection.on('SubscriptionConfirmed', callback);
  }

  onSubscriptionError(callback: (error: any) => void): void {
    if (!this.marketConnection) return;
    this.marketConnection.on('SubscriptionError', callback);
  }

  onMarketStatusUpdate(callback: (status: any) => void): void {
    if (!this.marketConnection) return;
    this.marketConnection.on('CurrentMarketStatus', callback);
  }

  onHeartbeat(callback: (data: any) => void): void {
    if (!this.marketConnection) return;
    this.marketConnection.on('Heartbeat', callback);
  }

  onPong(callback: (data: any) => void): void {
    if (!this.marketConnection) return;
    this.marketConnection.on('Pong', callback);
  }

  // === Dashboard Hub Methods ===

  async subscribeToIndicators(symbols: string[], timeframes: string[]): Promise<void> {
    if (!this.dashboardConnection || this.dashboardConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Dashboard hub not connected');
    }

    try {
      await this.dashboardConnection.invoke('SubscribeToIndicators', symbols, timeframes);
      console.log('Subscribed to indicators for symbols:', symbols, 'timeframes:', timeframes);
    } catch (error) {
      console.error('Failed to subscribe to indicators:', error);
      throw error;
    }
  }

  async subscribeToDashboardSignals(symbols: string[], minConfidence: number = 50): Promise<void> {
    if (!this.dashboardConnection || this.dashboardConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Dashboard hub not connected');
    }

    try {
      await this.dashboardConnection.invoke('SubscribeToSignals', symbols, minConfidence);
      console.log('Subscribed to trading signals for symbols:', symbols, 'min confidence:', minConfidence);
    } catch (error) {
      console.error('Failed to subscribe to signals:', error);
      throw error;
    }
  }

  async subscribeToPortfolio(): Promise<void> {
    if (!this.dashboardConnection || this.dashboardConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Dashboard hub not connected');
    }

    try {
      await this.dashboardConnection.invoke('SubscribeToPortfolio');
      console.log('Subscribed to portfolio updates');
    } catch (error) {
      console.error('Failed to subscribe to portfolio:', error);
      throw error;
    }
  }

  async subscribeToStrategies(strategyIds: string[]): Promise<void> {
    if (!this.dashboardConnection || this.dashboardConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Dashboard hub not connected');
    }

    try {
      await this.dashboardConnection.invoke('SubscribeToStrategies', strategyIds);
      console.log('Subscribed to strategies:', strategyIds);
    } catch (error) {
      console.error('Failed to subscribe to strategies:', error);
      throw error;
    }
  }

  async requestDashboardUpdate(dataType: string, parameters?: Record<string, any>): Promise<void> {
    if (!this.dashboardConnection || this.dashboardConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Dashboard hub not connected');
    }

    try {
      await this.dashboardConnection.invoke('RequestUpdate', dataType, parameters);
      console.log('Requested dashboard update for:', dataType);
    } catch (error) {
      console.error('Failed to request dashboard update:', error);
      throw error;
    }
  }

  // Dashboard Event Listeners
  onDashboardSnapshot(callback: (snapshot: DashboardSnapshot) => void): void {
    if (!this.dashboardConnection) return;
    this.dashboardConnection.on('DashboardSnapshot', callback);
  }

  onIndicatorUpdate(callback: (update: IndicatorUpdate) => void): void {
    if (!this.dashboardConnection) return;
    this.dashboardConnection.on('IndicatorUpdate', callback);
  }

  onSignalAlert(callback: (signal: SignalNotification) => void): void {
    if (!this.dashboardConnection) return;
    this.dashboardConnection.on('SignalAlert', callback);
  }

  onPortfolioUpdate(callback: (portfolio: PortfolioUpdate) => void): void {
    if (!this.dashboardConnection) return;
    this.dashboardConnection.on('PortfolioUpdate', callback);
  }

  onDashboardError(callback: (error: string) => void): void {
    if (!this.dashboardConnection) return;
    this.dashboardConnection.on('Error', callback);
  }

  // === General Methods ===

  async testMarketDataConnection(): Promise<any> {
    if (!this.marketConnection || this.marketConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Market data hub not connected');
    }

    try {
      await this.marketConnection.invoke('TestConnection');
      console.log('Market data connection test successful');
      return { success: true, hub: 'market' };
    } catch (error: any) {
      console.error('Market data connection test failed:', error);
      return { success: false, hub: 'market', error: error.message || 'Unknown error' };
    }
  }

  async testDashboardConnection(): Promise<any> {
    if (!this.dashboardConnection || this.dashboardConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Dashboard hub not connected');
    }

    try {
      await this.dashboardConnection.invoke('TestConnection');
      console.log('Dashboard connection test successful');
      return { success: true, hub: 'dashboard' };
    } catch (error: any) {
      console.error('Dashboard connection test failed:', error);
      return { success: false, hub: 'dashboard', error: error.message || 'Unknown error' };
    }
  }

  async disconnect(): Promise<void> {
    const disconnectPromises: Promise<void>[] = [];

    if (this.marketConnection) {
      disconnectPromises.push(
        this.marketConnection.stop().catch(error => {
          console.error('Error disconnecting market hub:', error);
        })
      );
    }

    if (this.dashboardConnection) {
      disconnectPromises.push(
        this.dashboardConnection.stop().catch(error => {
          console.error('Error disconnecting dashboard hub:', error);
        })
      );
    }

    await Promise.all(disconnectPromises);

    this.marketConnection = null;
    this.dashboardConnection = null;
    this.isConnecting.clear();
    this.connectionCallbacks.clear();

    console.log('All SignalR connections disconnected');
  }

  getMarketConnectionState(): string {
    if (!this.marketConnection) {
      return 'Disconnected';
    }

    switch (this.marketConnection.state) {
      case signalR.HubConnectionState.Connected:
        return 'Connected';
      case signalR.HubConnectionState.Connecting:
        return 'Connecting';
      case signalR.HubConnectionState.Disconnected:
        return 'Disconnected';
      case signalR.HubConnectionState.Disconnecting:
        return 'Disconnecting';
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting';
      default:
        return 'Unknown';
    }
  }

  getDashboardConnectionState(): string {
    if (!this.dashboardConnection) {
      return 'Disconnected';
    }

    switch (this.dashboardConnection.state) {
      case signalR.HubConnectionState.Connected:
        return 'Connected';
      case signalR.HubConnectionState.Connecting:
        return 'Connecting';
      case signalR.HubConnectionState.Disconnected:
        return 'Disconnected';
      case signalR.HubConnectionState.Disconnecting:
        return 'Disconnecting';
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting';
      default:
        return 'Unknown';
    }
  }

  isMarketDataConnected(): boolean {
    return this.marketConnection?.state === signalR.HubConnectionState.Connected || false;
  }

  isDashboardConnected(): boolean {
    return this.dashboardConnection?.state === signalR.HubConnectionState.Connected || false;
  }

  isAnyConnected(): boolean {
    return this.isMarketDataConnected() || this.isDashboardConnected();
  }

  getAllConnectionStates(): { market: string; dashboard: string; anyConnected: boolean } {
    return {
      market: this.getMarketConnectionState(),
      dashboard: this.getDashboardConnectionState(),
      anyConnected: this.isAnyConnected()
    };
  }
}

// Create singleton instance
export const enhancedSignalRService = new EnhancedSignalRService();

// Keep the old service for backward compatibility
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private connectionCallbacks: (() => void)[] = [];
  private isConnecting = false;

  async connectToTradingHub(): Promise<signalR.HubConnection> {
    // Forward to enhanced service for market data hub
    const backendUrl = import.meta.env.VITE_BACKEND_URL || 'http://localhost:5002';
    await enhancedSignalRService.connectToMarketDataHub({
      url: backendUrl,
      hubPath: '/hubs/marketdata'
    });
    return enhancedSignalRService.marketConnection!;
  }

  isConnected(): boolean {
    return enhancedSignalRService.isMarketDataConnected();
  }

  getConnectionState(): string {
    return enhancedSignalRService.getMarketConnectionState();
  }

  async disconnect(): Promise<void> {
    return enhancedSignalRService.disconnect();
  }
}

// Create singleton instance for backward compatibility
export const signalRService = new SignalRService();