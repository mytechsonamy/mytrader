import * as signalR from '@microsoft/signalr';

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private connectionCallbacks: (() => void)[] = [];
  private isConnecting = false;

  async connectToTradingHub(): Promise<signalR.HubConnection> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    if (this.isConnecting) {
      // Wait for existing connection attempt
      return new Promise((resolve) => {
        this.connectionCallbacks.push(() => {
          if (this.connection) {
            resolve(this.connection);
          }
        });
      });
    }

    this.isConnecting = true;

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:8080/hubs/trading', {
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
      this.connection.onreconnecting(() => {
        console.log('SignalR: Reconnecting...');
      });

      this.connection.onreconnected(() => {
        console.log('SignalR: Reconnected successfully');
      });

      this.connection.onclose((error) => {
        console.log('SignalR: Connection closed', error);
        this.isConnecting = false;
      });

      await this.connection.start();
      console.log('SignalR: Connected to Trading Hub');
      
      this.isConnecting = false;
      
      // Notify waiting callbacks
      this.connectionCallbacks.forEach(callback => callback());
      this.connectionCallbacks = [];

      return this.connection;
    } catch (error) {
      console.error('SignalR: Connection failed:', error);
      this.isConnecting = false;
      throw error;
    }
  }

  async connectToMockTradingHub(): Promise<signalR.HubConnection> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:8080/hubs/mock-trading', {
          withCredentials: false
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      this.connection.onreconnecting(() => {
        console.log('SignalR Mock: Reconnecting...');
      });

      this.connection.onreconnected(() => {
        console.log('SignalR Mock: Reconnected successfully');
      });

      await this.connection.start();
      console.log('SignalR: Connected to Mock Trading Hub');

      return this.connection;
    } catch (error) {
      console.error('SignalR Mock: Connection failed:', error);
      throw error;
    }
  }

  // Subscribe to price updates
  subscribeToPrice(symbol: string, callback: (symbol: string, price: number, change: number) => void) {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }

    this.connection.on('PriceUpdate', callback);
    
    // Join symbol group if available
    this.connection.invoke('JoinSymbolGroup', symbol).catch(err => {
      console.warn('Failed to join symbol group:', err);
    });
  }

  // Subscribe to trading signals
  subscribeToSignals(callback: (signal: any) => void) {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }

    this.connection.on('TradingSignal', callback);
  }

  // Subscribe to market status
  subscribeToMarketStatus(callback: (status: any) => void) {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }

    this.connection.on('MarketStatus', callback);
  }

  // Send trading order (mock)
  async sendTradingOrder(order: any) {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }

    try {
      await this.connection.invoke('PlaceOrder', order);
      console.log('Trading order sent:', order);
    } catch (error) {
      console.error('Failed to send trading order:', error);
      throw error;
    }
  }

  // Request current prices
  async requestPrices(symbols: string[]) {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }

    try {
      await this.connection.invoke('RequestPrices', symbols);
    } catch (error) {
      console.error('Failed to request prices:', error);
    }
  }

  // Test connection and get server info
  async testConnection(): Promise<any> {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }

    try {
      const serverInfo = await this.connection.invoke('GetServerInfo');
      console.log('Server info:', serverInfo);
      return serverInfo;
    } catch (error: any) {
      console.error('Failed to get server info:', error);
      return { error: error.message || 'Unknown error' };
    }
  }

  // Disconnect
  async disconnect() {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('SignalR: Disconnected');
      } catch (error) {
        console.error('SignalR: Error during disconnect:', error);
      } finally {
        this.connection = null;
        this.isConnecting = false;
      }
    }
  }

  // Get connection state
  getConnectionState(): string {
    if (!this.connection) {
      return 'Disconnected';
    }
    
    switch (this.connection.state) {
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

  // Check if connected
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected || false;
  }
}

// Create singleton instance
export const signalRService = new SignalRService();