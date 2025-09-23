const signalR = require('@microsoft/signalr');

console.log('📊 Simple MarketDataHub Subscription Test\n');

async function testSimpleSubscription() {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5002/hubs/market-data")
        .withAutomaticReconnect()
        .build();

    let dataReceived = false;

    // Listen for any message types
    connection.on("PriceUpdate", (data) => {
        dataReceived = true;
        console.log('💰 Price Update:', JSON.stringify(data, null, 2));
    });

    connection.on("ConnectionStatus", (data) => {
        console.log('✅ Connection Status:', data.message);
    });

    connection.on("SubscriptionConfirmed", (data) => {
        console.log('✅ Subscription confirmed:', JSON.stringify(data, null, 2));
    });

    connection.on("AssetClassSubscriptionConfirmed", (data) => {
        console.log('✅ Asset class subscription confirmed:', JSON.stringify(data, null, 2));
    });

    connection.on("SubscriptionError", (data) => {
        console.log('❌ Subscription error:', JSON.stringify(data, null, 2));
    });

    // Listen for market status
    connection.on("CurrentMarketStatus", (data) => {
        console.log('📊 Market Status:', JSON.stringify(data, null, 2));
    });

    try {
        await connection.start();
        console.log('✅ Connected to MarketDataHub\n');

        // Test: Subscribe to entire CRYPTO asset class (this should work)
        console.log('🔄 Subscribing to entire CRYPTO asset class...');
        await connection.invoke("SubscribeToAssetClass", "CRYPTO");

        // Wait and monitor
        console.log('⏱️  Waiting 20 seconds for any price updates...\n');
        await new Promise(resolve => setTimeout(resolve, 20000));

        if (dataReceived) {
            console.log('\n✅ SUCCESS: Real-time data is working!');
        } else {
            console.log('\n❌ No price data received');
            console.log('🔍 This suggests:');
            console.log('   • Binance WebSocket service may not be connected');
            console.log('   • Price broadcast service may not be working');
            console.log('   • No active price data streams');
        }

        await connection.stop();

    } catch (error) {
        console.log('❌ Test failed:', error.message);
    }
}

testSimpleSubscription().catch(console.error);