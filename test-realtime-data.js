const signalR = require('@microsoft/signalr');

console.log('📊 Testing Real-time Data Streaming from myTrader Backend\n');

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
        if (data.supportedAssetClasses) {
            console.log('📋 Supported Asset Classes:', data.supportedAssetClasses.join(', '));
        }
    });

    connection.on("SubscriptionConfirmed", (data) => {
        console.log('✅ Subscription confirmed for:', data.symbols?.join(', ') || data.assetClass);
    });

    connection.on("SubscriptionError", (data) => {
        console.log('❌ Subscription error:', data.message);
    });

    // Handle batch updates if they exist
    connection.on("ReceiveBatchPriceUpdate", (data) => {
        console.log(`📦 Received batch update with ${data.length} symbols`);
        data.forEach(item => {
            priceUpdateCount++;
            if (item.symbol) {
                receivedSymbols.add(item.symbol);
                console.log(`💰 [BATCH] ${item.symbol}: $${item.price}`);
            }
        });
    });

    // Connect and start monitoring
    try {
        await connection.start();
        console.log('✅ Connected successfully to MarketDataHub\n');

        // Subscribe to crypto symbols that should have live data
        console.log('🔄 Subscribing to live crypto symbols (BTCUSDT, ETHUSDT, ADAUSDT, SOLUSDT)...');
        await connection.invoke("SubscribeToPriceUpdates", "CRYPTO", ["BTCUSDT", "ETHUSDT", "ADAUSDT", "SOLUSDT"]);

        // Wait and analyze data for 30 seconds
        console.log('⏱️  Monitoring for 30 seconds...\n');

        let monitoringInterval = setInterval(() => {
            console.log(`📈 Updates received: ${priceUpdateCount} | Unique symbols: ${receivedSymbols.size} | Last update: ${lastUpdateTime ? lastUpdateTime.toLocaleTimeString() : 'None'}`);
        }, 5000);

        await new Promise(resolve => setTimeout(resolve, 30000));

        clearInterval(monitoringInterval);

        // Generate analysis report
        console.log('\n📊 Real-Time Data Analysis Report');
        console.log('=' .repeat(50));
        console.log(`Total price updates received: ${priceUpdateCount}`);
        console.log(`Unique symbols with data: ${receivedSymbols.size}`);
        console.log(`Symbols received: ${Array.from(receivedSymbols).join(', ')}`);

        if (priceUpdateCount > 0) {
            console.log(`✅ Real-time data streaming is WORKING`);
            console.log(`⏱️  Update frequency: ~${(priceUpdateCount / 30).toFixed(1)} updates/second`);

            // Analyze each symbol
            for (const [symbol, history] of priceHistory) {
                if (history.length > 1) {
                    const firstPrice = history[0].price;
                    const lastPrice = history[history.length - 1].price;
                    const priceChange = ((lastPrice - firstPrice) / firstPrice * 100).toFixed(4);
                    console.log(`📈 ${symbol}: ${history.length} updates, Price: $${firstPrice} → $${lastPrice} (${priceChange}%)`);
                }
            }
        } else {
            console.log(`❌ No real-time data received - potential issues:`);
            console.log(`   • Binance WebSocket service may not be running`);
            console.log(`   • Price broadcast service may be disabled`);
            console.log(`   • Subscription logic may have issues`);
        }

        await connection.stop();
        console.log('\n✅ Real-time data test completed');

    } catch (error) {
        console.log('❌ Real-time data test failed:', error.message);
    }
}

// Add graceful shutdown
process.on('SIGINT', () => {
    console.log('\n👋 Test interrupted by user');
    process.exit(0);
});

testRealTimeDataStreaming().catch(console.error);