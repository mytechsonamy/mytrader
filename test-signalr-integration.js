const signalR = require('@microsoft/signalr');
const fetch = require('node-fetch');

console.log('🚀 Starting myTrader SignalR Integration Test...\n');

async function testMarketDataHub() {
    console.log('📊 Testing MarketDataHub (Anonymous Connection)...');

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5002/hubs/market-data")
        .withAutomaticReconnect()
        .build();

    // Set up event handlers
    connection.on("ConnectionStatus", (data) => {
        console.log('✅ Connection Status:', JSON.stringify(data, null, 2));
    });

    connection.on("SubscriptionConfirmed", (data) => {
        console.log('✅ Subscription Confirmed:', JSON.stringify(data, null, 2));
    });

    connection.on("AssetClassSubscriptionConfirmed", (data) => {
        console.log('✅ Asset Class Subscription:', JSON.stringify(data, null, 2));
    });

    connection.on("CurrentMarketStatus", (data) => {
        console.log('✅ Market Status:', JSON.stringify(data, null, 2));
    });

    connection.on("PriceUpdate", (data) => {
        console.log('💰 Price Update:', JSON.stringify(data, null, 2));
    });

    connection.on("SubscriptionError", (data) => {
        console.log('❌ Subscription Error:', JSON.stringify(data, null, 2));
    });

    try {
        // Connect to hub
        await connection.start();
        console.log('✅ Connected to MarketDataHub successfully');

        // Test 1: Subscribe to specific crypto symbols
        console.log('\n🔄 Test 1: Subscribing to BTCUSDT and ETHUSDT...');
        await connection.invoke("SubscribeToPriceUpdates", "CRYPTO", ["BTCUSDT", "ETHUSDT"]);

        // Wait for data
        await new Promise(resolve => setTimeout(resolve, 5000));

        // Test 2: Subscribe to asset class
        console.log('\n🔄 Test 2: Subscribing to all CRYPTO symbols...');
        await connection.invoke("SubscribeToAssetClass", "CRYPTO");

        // Wait for data
        await new Promise(resolve => setTimeout(resolve, 3000));

        // Test 3: Get market status
        console.log('\n🔄 Test 3: Getting market status...');
        await connection.invoke("GetMarketStatus");

        // Wait for response
        await new Promise(resolve => setTimeout(resolve, 2000));

        // Test 4: Test invalid asset class
        console.log('\n🔄 Test 4: Testing invalid asset class...');
        await connection.invoke("SubscribeToPriceUpdates", "INVALID", ["BTCUSDT"]);

        // Wait for error
        await new Promise(resolve => setTimeout(resolve, 2000));

        console.log('\n✅ MarketDataHub tests completed');
        await connection.stop();

    } catch (error) {
        console.log('❌ MarketDataHub test failed:', error.message);
    }
}

async function testTradingHub() {
    console.log('\n🔐 Testing TradingHub (Authentication Required)...');

    // First, try without authentication
    console.log('🔄 Test 1: Connecting without authentication...');
    let connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5002/hubs/trading")
        .withAutomaticReconnect()
        .build();

    try {
        await connection.start();
        console.log('⚠️  Connected to TradingHub without auth (unexpected)');
        await connection.stop();
    } catch (error) {
        console.log('✅ TradingHub correctly rejected unauthenticated connection:', error.message);
    }

    // TODO: Test with JWT token (requires login endpoint)
    console.log('🔄 Test 2: Testing with JWT token (requires manual token)...');
    console.log('💡 To test authenticated connection, provide JWT token manually');
}

async function testPortfolioHub() {
    console.log('\n💼 Testing PortfolioHub...');

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5002/hubs/portfolio")
        .withAutomaticReconnect()
        .build();

    try {
        await connection.start();
        console.log('✅ Connected to PortfolioHub');
        await connection.stop();
    } catch (error) {
        console.log('❌ PortfolioHub connection failed:', error.message);
    }
}

async function getJWTToken() {
    console.log('\n🔑 Testing JWT token generation...');

    try {
        const response = await fetch('http://localhost:5002/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                email: 'test@example.com',
                password: 'test123'
            })
        });

        if (response.ok) {
            const data = await response.json();
            console.log('✅ JWT Token obtained:', data.token ? 'Token received' : 'No token in response');
            return data.token;
        } else {
            const errorData = await response.text();
            console.log('❌ Login failed:', response.status, errorData);
            return null;
        }
    } catch (error) {
        console.log('❌ Login request failed:', error.message);
        return null;
    }
}

async function testAuthenticatedHubs() {
    console.log('\n🔐 Testing Authenticated Hubs...');

    const token = await getJWTToken();
    if (!token) {
        console.log('⚠️  Skipping authenticated hub tests (no token available)');
        return;
    }

    // Test TradingHub with authentication
    console.log('\n🔄 Testing TradingHub with JWT token...');
    const tradingConnection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5002/hubs/trading", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

    try {
        await tradingConnection.start();
        console.log('✅ TradingHub authenticated connection successful');

        // Test joining a group
        await tradingConnection.invoke("JoinGroup", "test-group");
        console.log('✅ Successfully joined test-group');

        await tradingConnection.stop();
    } catch (error) {
        console.log('❌ TradingHub authenticated connection failed:', error.message);
    }
}

// Run all tests
async function runAllTests() {
    console.log('🧪 myTrader SignalR Integration Test Suite');
    console.log('=' .repeat(50));

    await testMarketDataHub();
    await testTradingHub();
    await testPortfolioHub();
    await testAuthenticatedHubs();

    console.log('\n🏁 All SignalR integration tests completed!');
    console.log('=' .repeat(50));
}

// Add graceful shutdown
process.on('SIGINT', () => {
    console.log('\n👋 Test interrupted by user');
    process.exit(0);
});

runAllTests().catch(console.error);