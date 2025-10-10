// Simple WebSocket connection test for SignalR
// Run this in browser console after loading SignalR

const testWebSocketConnection = async () => {
  try {
    console.log('🔍 Testing WebSocket connection to SignalR Hub...');

    // Use the SignalR library from CDN
    if (typeof signalR === 'undefined') {
      console.error('❌ SignalR library not loaded. Please include: https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js');
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5002/hubs/market-data")
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up event handlers
    connection.on("PriceUpdate", (data) => {
      console.log("📈 Received price update:", data);
    });

    connection.onclose((error) => {
      console.log("🔌 Connection closed:", error);
    });

    connection.onreconnecting((error) => {
      console.log("🔄 Reconnecting:", error);
    });

    connection.onreconnected((connectionId) => {
      console.log("✅ Reconnected with ID:", connectionId);
    });

    // Connect
    console.log('🚀 Attempting to connect...');
    await connection.start();
    console.log('✅ Connected successfully!');

    // Subscribe to crypto symbols
    const cryptoSymbols = ['BTCUSD', 'ETHUSD', 'ADAUSD'];
    console.log('📡 Subscribing to symbols:', cryptoSymbols);

    try {
      await connection.invoke("SubscribeToPriceUpdates", "CRYPTO", cryptoSymbols);
      console.log('✅ Subscription successful!');
    } catch (subscribeError) {
      console.error('❌ Subscription failed:', subscribeError);
    }

    // Keep connection alive for testing
    setTimeout(async () => {
      console.log('🔌 Closing connection after test...');
      await connection.stop();
      console.log('✅ Test completed');
    }, 30000); // 30 seconds

    return connection;
  } catch (error) {
    console.error('❌ WebSocket connection test failed:', error);
  }
};

// Auto-run if in browser
if (typeof window !== 'undefined') {
  console.log('🧪 WebSocket Connection Test Ready');
  console.log('📋 Run testWebSocketConnection() in console to start test');
  window.testWebSocketConnection = testWebSocketConnection;
}

// Node.js export
if (typeof module !== 'undefined') {
  module.exports = { testWebSocketConnection };
}