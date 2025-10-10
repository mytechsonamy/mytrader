#!/usr/bin/env node

const { HubConnectionBuilder, LogLevel } = require('@microsoft/signalr');

// Configuration from mobile app
const LAN_IP = '192.168.68.103';
const API_PORT = '5002';
const WS_BASE_URL = `http://${LAN_IP}:${API_PORT}/hubs/market-data`;

console.log('🚀 MyTrader Mobile Connection Integration Test');
console.log('=' .repeat(60));
console.log(`Testing mobile-like connection to: ${WS_BASE_URL}`);
console.log(`Backend API accessible at: http://${LAN_IP}:${API_PORT}/health`);
console.log('=' .repeat(60));

let connection = null;
let messageCount = 0;
let priceUpdateCount = 0;
let connectionStartTime = null;
let statsInterval = null;

function log(message, level = 'INFO') {
    const timestamp = new Date().toISOString();
    console.log(`[${timestamp}] [${level}] ${message}`);
}

function startStatsLogger() {
    statsInterval = setInterval(() => {
        if (connectionStartTime) {
            const uptime = Math.floor((Date.now() - connectionStartTime) / 1000);
            log(`📊 Stats: ${messageCount} total messages, ${priceUpdateCount} price updates, ${uptime}s uptime`);
        }
    }, 10000); // Every 10 seconds
}

async function testMobileConnection() {
    try {
        log('🔄 Creating SignalR connection (mobile simulation)...');

        // Create connection with mobile-like configuration
        connection = new HubConnectionBuilder()
            .withUrl(WS_BASE_URL, {
                skipNegotiation: false,
                transport: 1 | 2, // WebSockets | ServerSentEvents (like mobile)
                withCredentials: false,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(LogLevel.Information)
            .build();

        // Set up event handlers for both event names (testing dual compatibility)
        connection.on("PriceUpdate", (data) => {
            messageCount++;
            priceUpdateCount++;

            const symbol = data.Symbol || data.symbol || 'Unknown';
            const price = data.Price || data.price || 0;
            const change = data.Change24h || data.changePercent || 0;

            log(`💰 PriceUpdate: ${symbol} = $${parseFloat(price).toFixed(2)} (${change >= 0 ? '+' : ''}${parseFloat(change).toFixed(2)}%)`);
        });

        connection.on("ReceivePriceUpdate", (data) => {
            messageCount++;
            priceUpdateCount++;

            const symbol = data.Symbol || data.symbol || 'Unknown';
            const price = data.Price || data.price || 0;
            const change = data.Change24h || data.changePercent || 0;

            log(`💰 ReceivePriceUpdate: ${symbol} = $${parseFloat(price).toFixed(2)} (${change >= 0 ? '+' : ''}${parseFloat(change).toFixed(2)}%)`);
        });

        // Connection lifecycle events
        connection.onclose((error) => {
            log(`❌ Connection closed: ${error ? error.toString() : 'No error'}`, 'ERROR');
            if (statsInterval) clearInterval(statsInterval);
        });

        connection.onreconnecting((error) => {
            log(`🔄 Reconnecting: ${error ? error.toString() : 'Unknown error'}`, 'WARN');
        });

        connection.onreconnected((connectionId) => {
            log(`✅ Reconnected with ID: ${connectionId}`, 'SUCCESS');
        });

        // Start the connection
        log('🔌 Starting connection...');
        await connection.start();

        connectionStartTime = Date.now();
        log('✅ Connected successfully!', 'SUCCESS');
        log(`🔗 Connection ID: ${connection.connectionId}`);

        startStatsLogger();

        // Test subscription (like mobile app would do)
        try {
            log('📡 Testing subscription to crypto symbols...');
            await connection.invoke("SubscribeToPriceUpdates", "CRYPTO", ["BTCUSDT", "ETHUSDT"]);
            log('✅ Subscription successful!', 'SUCCESS');
        } catch (error) {
            log(`❌ Subscription failed: ${error.toString()}`, 'ERROR');
        }

        // Keep connection alive for testing
        log('🎯 Connection established. Monitoring real-time data...');
        log('   Press Ctrl+C to stop the test');

    } catch (error) {
        log(`❌ Connection failed: ${error.toString()}`, 'ERROR');
        process.exit(1);
    }
}

// Test API accessibility first
async function testApiAccess() {
    try {
        const fetch = (await import('node-fetch')).default;
        const healthUrl = `http://${LAN_IP}:${API_PORT}/health`;

        log(`🏥 Testing API health at: ${healthUrl}`);
        const response = await fetch(healthUrl);
        const data = await response.json();

        log(`✅ API accessible: ${data.message}`, 'SUCCESS');

        // Test versioned endpoint
        const volumeUrl = `http://${LAN_IP}:${API_PORT}/api/v1/market-data/top-by-volume?perClass=8`;
        log(`📊 Testing versioned endpoint: ${volumeUrl}`);
        const volumeResponse = await fetch(volumeUrl);
        const volumeData = await volumeResponse.json();

        log(`✅ Versioned API working: Retrieved ${volumeData.data?.length || 0} volume leaders`, 'SUCCESS');

    } catch (error) {
        log(`❌ API test failed: ${error.message}`, 'ERROR');
        process.exit(1);
    }
}

// Graceful shutdown
process.on('SIGINT', async () => {
    log('\n🛑 Shutting down gracefully...');

    if (statsInterval) {
        clearInterval(statsInterval);
    }

    if (connection) {
        try {
            await connection.stop();
            log('✅ Connection closed successfully');
        } catch (error) {
            log(`❌ Error closing connection: ${error.toString()}`, 'ERROR');
        }
    }

    log(`📊 Final Stats: ${messageCount} messages received, ${priceUpdateCount} price updates`);
    process.exit(0);
});

// Run the test
async function runIntegrationTest() {
    try {
        await testApiAccess();
        await testMobileConnection();
    } catch (error) {
        log(`💥 Integration test failed: ${error.message}`, 'ERROR');
        process.exit(1);
    }
}

runIntegrationTest();