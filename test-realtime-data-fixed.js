const signalR = require('@microsoft/signalr');

console.log('📊 Testing Real-time Data Streaming (Fixed) from myTrader Backend\n');

async function testRealTimeDataStreaming() {
    console.log('🔄 Connecting to MarketDataHub for real-time price updates...');

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5002/hubs/market-data")
        .withAutomaticReconnect()
        .build();

    let priceUpdateCount = 0;
    let lastUpdateTime = null;
    let receivedSymbols = new Set();
    let priceHistory = new Map();

    // Track all price updates
    connection.on("PriceUpdate", (data) => {
        priceUpdateCount++;
        lastUpdateTime = new Date();

        if (data.symbol) {
            receivedSymbols.add(data.symbol);

            // Track price history for analysis
            if (!priceHistory.has(data.symbol)) {
                priceHistory.set(data.symbol, []);
            }
            priceHistory.get(data.symbol).push({
                price: data.price,
                timestamp: lastUpdateTime,
                change: data.change || 0
            });

            console.log(`💰 [${lastUpdateTime.toLocaleTimeString()}] ${data.symbol}: $${data.price} (${data.change >= 0 ? '+' : ''}${data.change}%)`);
        }
    });

    connection.on("ConnectionStatus", (data) => {
        console.log('✅ Connection Status:', data.status);
    });

    connection.on("SubscriptionConfirmed", (data) => {
        console.log('✅ Subscription confirmed for:', data.symbols?.join(', ') || data.assetClass);
    });

    connection.on("SubscriptionError", (data) => {
        console.log('❌ Subscription error:', data.message);
    });

    // Connect and start monitoring
    try {
        await connection.start();
        console.log('✅ Connected successfully to MarketDataHub\n');

        // Test 1: Subscribe to single symbol
        console.log('🔄 Test 1: Subscribing to single symbol (BTCUSDT)...');
        await connection.invoke("SubscribeToPriceUpdates", "CRYPTO", "BTCUSDT");
        await new Promise(resolve => setTimeout(resolve, 5000));

        // Test 2: Subscribe to asset class (should work)
        console.log('🔄 Test 2: Subscribing to entire CRYPTO asset class...');
        await connection.invoke("SubscribeToAssetClass", "CRYPTO");
        await new Promise(resolve => setTimeout(resolve, 5000));

        // Test 3: Try legacy method
        console.log('🔄 Test 3: Using legacy SubscribeToCrypto method...');
        await connection.invoke("SubscribeToCrypto", "ETHUSDT");
        await new Promise(resolve => setTimeout(resolve, 5000));

        // Test 4: Multiple symbols as string array (might work differently)
        console.log('🔄 Test 4: Trying multiple symbols with different format...');
        try {
            await connection.invoke("SubscribeToPriceUpdates", "CRYPTO", ["BTCUSDT", "ETHUSDT"]);
        } catch (error) {
            console.log('❌ Array format failed:', error.message);
        }

        // Monitor for price updates
        console.log('⏱️  Monitoring for 15 seconds for any price updates...\n');

        let monitoringInterval = setInterval(() => {
            console.log(`📈 Updates: ${priceUpdateCount} | Symbols: ${receivedSymbols.size} | Last: ${lastUpdateTime ? lastUpdateTime.toLocaleTimeString() : 'None'}`);
        }, 3000);

        await new Promise(resolve => setTimeout(resolve, 15000));
        clearInterval(monitoringInterval);

        // Generate analysis report
        console.log('\n📊 Real-Time Data Analysis Report');
        console.log('=' .repeat(50));
        console.log(`Total price updates received: ${priceUpdateCount}`);
        console.log(`Unique symbols with data: ${receivedSymbols.size}`);
        console.log(`Symbols received: ${Array.from(receivedSymbols).join(', ')}`);

        if (priceUpdateCount > 0) {
            console.log(`✅ Real-time data streaming is WORKING`);
        } else {
            console.log(`❌ No real-time data received`);
            console.log(`🔍 Possible causes:`);
            console.log(`   • Binance WebSocket service not broadcasting to SignalR`);
            console.log(`   • Symbol names don't match backend expectations`);
            console.log(`   • Price broadcast service not running`);
        }

        await connection.stop();

    } catch (error) {
        console.log('❌ Real-time data test failed:', error.message);
    }
}

testRealTimeDataStreaming().catch(console.error);