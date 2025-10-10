#!/usr/bin/env node

/**
 * Test script to verify WebSocket symbol format fixes
 * Run this to ensure correct subscription to CRYPTO market data
 */

const { HubConnectionBuilder, HttpTransportType, LogLevel } = require('@microsoft/signalr');

// Configuration
const API_BASE_URL = 'http://192.168.68.103:5002';
const HUB_URL = `${API_BASE_URL}/hubs/dashboard`;

// Correct Binance symbol format (with USDT)
const CRYPTO_SYMBOLS = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT', 'AVAXUSDT'];

console.log('=== WebSocket Symbol Format Test ===');
console.log('Testing with correct symbol format:', CRYPTO_SYMBOLS);
console.log('Hub URL:', HUB_URL);
console.log('');

async function testWebSocketConnection() {
  try {
    // Create SignalR connection
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        skipNegotiation: false,
        transport: HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    // Set up event handlers
    connection.on('PriceUpdate', (data) => {
      console.log('✅ [PriceUpdate] Received:', {
        symbol: data.Symbol || data.symbol,
        price: data.Price || data.price,
        timestamp: new Date().toISOString()
      });
    });

    connection.on('MarketDataUpdate', (data) => {
      console.log('✅ [MarketDataUpdate] Received:', {
        symbol: data.Symbol || data.symbol,
        price: data.Price || data.price,
        timestamp: new Date().toISOString()
      });
    });

    connection.on('subscriptionerror', (error) => {
      console.error('❌ [subscriptionerror] Error:', error);
    });

    connection.on('SubscriptionError', (error) => {
      console.error('❌ [SubscriptionError] Error:', error);
    });

    // Connection lifecycle events
    connection.onclose((error) => {
      console.log('Connection closed:', error?.message || 'No error');
    });

    connection.onreconnecting(() => {
      console.log('Reconnecting...');
    });

    connection.onreconnected(() => {
      console.log('Reconnected');
    });

    // Start connection
    console.log('Connecting to SignalR hub...');
    await connection.start();
    console.log('✅ Connected successfully');

    // Subscribe to CRYPTO updates with correct symbol format
    console.log('');
    console.log('Subscribing to CRYPTO market with symbols:', CRYPTO_SYMBOLS);
    console.log('Invoking: SubscribeToPriceUpdates("CRYPTO", [...symbols])');

    try {
      await connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', CRYPTO_SYMBOLS);
      console.log('✅ Successfully subscribed to CRYPTO price updates');
      console.log('');
      console.log('Waiting for price updates...');
      console.log('(Press Ctrl+C to stop)');
    } catch (subscribeError) {
      console.error('❌ Failed to subscribe:', subscribeError);

      // Detailed error analysis
      if (subscribeError.message?.includes('NoSymbols')) {
        console.error('');
        console.error('ERROR ANALYSIS:');
        console.error('- Backend received no symbols or wrong format');
        console.error('- Check that symbols are sent as array: ["BTCUSDT", ...]');
        console.error('- Verify backend can parse the array correctly');
      }
    }

    // Keep the connection alive
    setInterval(() => {
      // Heartbeat
    }, 30000);

  } catch (error) {
    console.error('❌ Connection failed:', error);
    process.exit(1);
  }
}

// Run the test
testWebSocketConnection().catch(console.error);

// Handle graceful shutdown
process.on('SIGINT', () => {
  console.log('\n\nShutting down...');
  process.exit(0);
});