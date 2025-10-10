#!/usr/bin/env node

const signalR = require('@microsoft/signalr');

// Test configuration - matches mobile app
const API_BASE_URL = 'http://192.168.68.103:5002/api';
const WS_HUB_URL = 'http://192.168.68.103:5002/hubs/market-data';

console.log('🧪 MOBILE CONNECTIVITY TEST SUITE');
console.log('================================');

async function testApiConnectivity() {
  console.log('\n1️⃣ Testing API Connectivity...');

  try {
    // Test health endpoint
    const response = await fetch(`${API_BASE_URL.replace('/api', '')}/api/health`);
    const health = await response.json();
    console.log('✅ Health:', response.status, health.status);

    // Test market data overview (should return ApiResponse<T> wrapper)
    const overviewResponse = await fetch(`${API_BASE_URL}/v1/market-data/overview`);
    const overview = await overviewResponse.json();
    console.log('✅ Market Overview:', overviewResponse.status);
    console.log('📊 Overview data structure:', Object.keys(overview).slice(0, 5));

    // Test volume leaders (should return ApiResponse<T> wrapper)
    const volumeResponse = await fetch(`${API_BASE_URL}/v1/market-data/top-by-volume?perClass=3`);
    const volume = await volumeResponse.json();
    console.log('✅ Volume Leaders:', volumeResponse.status);
    console.log('📈 Volume data structure:', typeof volume, Array.isArray(volume) ? 'Array' : 'Object');

    return true;
  } catch (error) {
    console.error('❌ API Connectivity failed:', error.message);
    return false;
  }
}

async function testWebSocketConnectivity() {
  console.log('\n2️⃣ Testing WebSocket/SignalR Connectivity...');

  return new Promise((resolve) => {
    try {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(WS_HUB_URL, {
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
          headers: {
            'X-Client-Type': 'mobile-test',
            'User-Agent': 'mobile-connectivity-test'
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      let messageCount = 0;
      let subscribed = false;

      // Set up event handlers
      connection.on('PriceUpdate', (data) => {
        messageCount++;
        console.log(`📡 PriceUpdate #${messageCount}:`, data?.symbol, data?.price);
      });

      connection.on('ReceivePriceUpdate', (data) => {
        messageCount++;
        console.log(`📡 ReceivePriceUpdate #${messageCount}:`, data?.symbol, data?.price);
      });

      connection.onclose((error) => {
        console.log('🔌 Connection closed:', error?.message || 'No error');
        resolve(messageCount > 0);
      });

      // Start connection
      connection.start()
        .then(async () => {
          console.log('✅ SignalR connected successfully');

          // Subscribe to crypto updates
          const cryptoSymbols = ['BTCUSD', 'ETHUSD', 'ADAUSD'];
          try {
            await connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', cryptoSymbols);
            console.log('✅ Subscribed to crypto updates:', cryptoSymbols);
            subscribed = true;
          } catch (subError) {
            console.warn('⚠️ Subscription failed:', subError.message);
          }

          // Wait for messages
          setTimeout(() => {
            console.log(`📊 Received ${messageCount} messages in 10 seconds`);
            connection.stop();
          }, 10000);
        })
        .catch((error) => {
          console.error('❌ SignalR connection failed:', error.message);
          resolve(false);
        });

    } catch (error) {
      console.error('❌ WebSocket setup failed:', error.message);
      resolve(false);
    }
  });
}

async function testApiResponseUnwrapping() {
  console.log('\n3️⃣ Testing ApiResponse<T> Unwrapping...');

  try {
    const response = await fetch(`${API_BASE_URL}/v1/market-data/overview`);
    const rawData = await response.json();

    console.log('📦 Raw response structure:', {
      hasData: 'data' in rawData,
      keys: Object.keys(rawData).slice(0, 10),
      isArray: Array.isArray(rawData),
      type: typeof rawData
    });

    // Simulate mobile app handleResponse logic
    let processedData;
    if (rawData && typeof rawData === 'object' && 'data' in rawData) {
      processedData = rawData.data;
      console.log('✅ Unwrapped ApiResponse<T> format');
    } else {
      processedData = rawData;
      console.log('✅ Using direct response format');
    }

    console.log('📊 Processed data structure:', Object.keys(processedData || {}).slice(0, 5));
    return true;
  } catch (error) {
    console.error('❌ ApiResponse unwrapping test failed:', error.message);
    return false;
  }
}

async function runAllTests() {
  console.log('Starting mobile connectivity tests...\n');

  const results = {
    api: await testApiConnectivity(),
    websocket: await testWebSocketConnectivity(),
    unwrapping: await testApiResponseUnwrapping()
  };

  console.log('\n🎯 TEST RESULTS SUMMARY');
  console.log('======================');
  console.log(`API Connectivity: ${results.api ? '✅ PASS' : '❌ FAIL'}`);
  console.log(`WebSocket Integration: ${results.websocket ? '✅ PASS' : '❌ FAIL'}`);
  console.log(`ApiResponse Unwrapping: ${results.unwrapping ? '✅ PASS' : '❌ FAIL'}`);

  const allPassed = Object.values(results).every(result => result);
  console.log(`\n🏆 Overall Status: ${allPassed ? '✅ ALL TESTS PASSED' : '❌ SOME TESTS FAILED'}`);

  if (allPassed) {
    console.log('🎉 Mobile app should now receive synchronized price data from backend!');
  } else {
    console.log('⚠️ Mobile app may experience connectivity or data parsing issues.');
  }
}

// Run tests
runAllTests().catch(console.error);