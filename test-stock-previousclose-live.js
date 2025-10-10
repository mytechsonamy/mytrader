// Test script to verify stock previousClose field in SignalR broadcasts
const signalR = require('@microsoft/signalr');

const hubUrl = 'http://localhost:8080/hubs/market-data';

console.log('🔌 Connecting to SignalR hub:', hubUrl);

const connection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl, {
    skipNegotiation: true,
    transport: signalR.HttpTransportType.WebSockets
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Listen for price updates
connection.on('ReceivePriceUpdate', (data) => {
  console.log('\n📊 ReceivePriceUpdate event received');
  console.log('Raw data type:', typeof data);
  console.log('Raw data:', JSON.stringify(data, null, 2));

  // Try to parse if string
  let parsed = data;
  if (typeof data === 'string') {
    try {
      parsed = JSON.parse(data);
      console.log('Parsed data:', JSON.stringify(parsed, null, 2));
    } catch (e) {
      console.log('Failed to parse:', e.message);
    }
  }

  // Check for stock with previousClose
  if (parsed.assetClass === 'STOCK' || parsed.assetClass === 'Stock') {
    console.log('\n🎯 STOCK DATA RECEIVED:');
    console.log('Symbol:', parsed.symbol);
    console.log('Price:', parsed.price);
    console.log('Change:', parsed.change);
    console.log('Change24h:', parsed.change24h);
    console.log('previousClose:', parsed.previousClose);
    console.log('PreviousClose:', parsed.PreviousClose);
    console.log('Volume:', parsed.volume);
    console.log('Timestamp:', parsed.timestamp);
    console.log('AssetClass:', parsed.assetClass);

    // Check all keys
    console.log('\n🔑 All available keys:', Object.keys(parsed));

    if (!parsed.previousClose && !parsed.PreviousClose) {
      console.log('\n❌ ERROR: previousClose field is MISSING!');
    } else {
      console.log('\n✅ SUCCESS: previousClose field is PRESENT!');
    }
  }
});

// Listen for legacy updates
connection.on('price_update', (data) => {
  console.log('\n📊 price_update event received');
  console.log('Raw data:', JSON.stringify(data, null, 2));

  if (data.assetClass === 'STOCK' || data.assetClass === 'Stock') {
    console.log('\n🎯 STOCK DATA (legacy):');
    console.log('Symbol:', data.symbol);
    console.log('previousClose:', data.previousClose);
    console.log('PreviousClose:', data.PreviousClose);
    console.log('All keys:', Object.keys(data));
  }
});

// Start connection
connection.start()
  .then(() => {
    console.log('✅ Connected to SignalR hub');
    console.log('📡 Listening for stock updates with previousClose field...\n');
  })
  .catch(err => {
    console.error('❌ Connection error:', err);
    process.exit(1);
  });

// Handle disconnection
connection.onclose((error) => {
  if (error) {
    console.error('❌ Connection closed with error:', error);
  } else {
    console.log('Connection closed');
  }
});

// Keep process alive
process.on('SIGINT', () => {
  console.log('\n\nClosing connection...');
  connection.stop();
  process.exit(0);
});
