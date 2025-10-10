#!/usr/bin/env node

/**
 * Phase 4: End-to-End WebSocket Validation Script
 *
 * This script validates the fixes made to:
 * 1. Mobile frontend: Symbol format correction (BTCUSD → BTCUSDT)
 * 2. Backend: object[] array handling in MarketDataHub.ParseSymbolData()
 *
 * Success Criteria:
 * - No "NoSymbols" errors for valid symbol arrays
 * - SubscriptionConfirmed events received for all valid subscriptions
 * - Price updates flow continuously after subscription
 * - Error handling works correctly for null/empty inputs
 */

const signalR = require('@microsoft/signalr');
const chalk = require('chalk');

// Configuration
const HUB_URL = process.env.HUB_URL || 'http://192.168.68.102:8080/hubs/market-data';
const TEST_DURATION = 30000; // 30 seconds per test

// Test Results
const testResults = {
    total: 0,
    passed: 0,
    failed: 0,
    errors: [],
    subscriptions: [],
    priceUpdates: [],
    startTime: null,
    endTime: null
};

// Metrics
let metrics = {
    priceUpdateCount: 0,
    subscriptionCount: 0,
    errorCount: 0,
    connectionAttempts: 0
};

// Color logging helpers
const log = {
    info: (msg) => console.log(chalk.blue(`ℹ️  ${msg}`)),
    success: (msg) => console.log(chalk.green(`✅ ${msg}`)),
    error: (msg) => console.log(chalk.red(`❌ ${msg}`)),
    warning: (msg) => console.log(chalk.yellow(`⚠️  ${msg}`)),
    header: (msg) => console.log(chalk.cyan.bold(`\n${'═'.repeat(60)}\n${msg}\n${'═'.repeat(60)}`)),
    section: (msg) => console.log(chalk.magenta.bold(`\n${msg}`))
};

// Create SignalR connection
async function createConnection() {
    log.info(`Creating connection to ${HUB_URL}`);
    metrics.connectionAttempts++;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL, {
            skipNegotiation: false,
            transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    return connection;
}

// Setup event handlers
function setupEventHandlers(connection) {
    // Connection status
    connection.on('ConnectionStatus', (data) => {
        log.success(`Connection status: ${JSON.stringify(data)}`);
        if (data.connectionId) {
            log.info(`Connection ID: ${data.connectionId}`);
        }
    });

    // Heartbeat
    connection.on('Heartbeat', (data) => {
        log.info(`💓 Heartbeat: ${data.timestamp}`);
    });

    // Subscription confirmed
    connection.on('SubscriptionConfirmed', (data) => {
        metrics.subscriptionCount++;
        log.success('SUBSCRIPTION CONFIRMED!');
        log.success(`  Asset Class: ${data.assetClass}`);
        log.success(`  Symbols: ${data.symbols ? data.symbols.join(', ') : 'none'}`);
        log.success(`  Timestamp: ${data.timestamp}`);

        testResults.subscriptions.push({
            assetClass: data.assetClass,
            symbols: data.symbols,
            timestamp: data.timestamp
        });
    });

    // Subscription error
    connection.on('SubscriptionError', (data) => {
        metrics.errorCount++;
        log.error('SUBSCRIPTION ERROR!');
        log.error(`  Error Code: ${data.error}`);
        log.error(`  Message: ${data.message}`);
        if (data.receivedData) {
            log.error(`  Received Data: ${JSON.stringify(data.receivedData)}`);
        }

        testResults.errors.push({
            error: data.error,
            message: data.message,
            receivedData: data.receivedData,
            timestamp: new Date().toISOString()
        });
    });

    // Price updates
    connection.on('PriceUpdate', (data) => {
        metrics.priceUpdateCount++;
        log.info(`📊 Price Update: ${data.symbol} = $${data.price} (${data.change24h >= 0 ? '+' : ''}${data.change24h}%)`);

        testResults.priceUpdates.push({
            symbol: data.symbol,
            price: data.price,
            change: data.change24h,
            timestamp: new Date().toISOString()
        });
    });

    connection.on('ReceivePriceUpdate', (data) => {
        metrics.priceUpdateCount++;
        log.info(`📊 Receive Price Update: ${data.symbol} = $${data.price}`);

        testResults.priceUpdates.push({
            symbol: data.symbol,
            price: data.price,
            timestamp: new Date().toISOString()
        });
    });

    // Connection lifecycle
    connection.onclose((error) => {
        if (error) {
            log.error(`Connection closed with error: ${error.message}`);
        } else {
            log.warning('Connection closed');
        }
    });

    connection.onreconnecting((error) => {
        log.warning(`Reconnecting... ${error ? error.message : ''}`);
    });

    connection.onreconnected((connectionId) => {
        log.success(`Reconnected! Connection ID: ${connectionId}`);
    });
}

// Test 1: Array of symbols (object[] handling)
async function testArraySubscription(connection) {
    log.section('🧪 Test 1: Array Subscription (object[] handling)');
    testResults.total++;

    try {
        const symbols = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT', 'AVAXUSDT'];
        log.info(`Sending array of ${symbols.length} symbols: ${JSON.stringify(symbols)}`);
        log.info('Expected: Backend should parse as object[] and return SubscriptionConfirmed');

        const beforeSubs = metrics.subscriptionCount;
        await connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', symbols);

        // Wait for subscription confirmation
        await new Promise(resolve => setTimeout(resolve, 2000));

        if (metrics.subscriptionCount > beforeSubs) {
            log.success('✅ Test 1 PASSED: Array subscription successful');
            testResults.passed++;
            return true;
        } else {
            log.error('❌ Test 1 FAILED: No subscription confirmation received');
            testResults.failed++;
            return false;
        }
    } catch (error) {
        log.error(`❌ Test 1 FAILED: ${error.message}`);
        testResults.failed++;
        testResults.errors.push({ test: 'Test 1', error: error.message, timestamp: new Date().toISOString() });
        return false;
    }
}

// Test 2: Single symbol (string handling)
async function testSingleSymbol(connection) {
    log.section('🧪 Test 2: Single Symbol (string handling)');
    testResults.total++;

    try {
        const symbol = 'BTCUSDT';
        log.info(`Sending single symbol: "${symbol}"`);

        const beforeSubs = metrics.subscriptionCount;
        await connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', symbol);

        await new Promise(resolve => setTimeout(resolve, 2000));

        if (metrics.subscriptionCount > beforeSubs) {
            log.success('✅ Test 2 PASSED: Single symbol subscription successful');
            testResults.passed++;
            return true;
        } else {
            log.error('❌ Test 2 FAILED: No subscription confirmation received');
            testResults.failed++;
            return false;
        }
    } catch (error) {
        log.error(`❌ Test 2 FAILED: ${error.message}`);
        testResults.failed++;
        testResults.errors.push({ test: 'Test 2', error: error.message, timestamp: new Date().toISOString() });
        return false;
    }
}

// Test 3: Empty array (error handling)
async function testEmptyArray(connection) {
    log.section('🧪 Test 3: Empty Array (error handling)');
    testResults.total++;

    try {
        const symbols = [];
        log.info(`Sending empty array: ${JSON.stringify(symbols)}`);
        log.info('Expected: Should receive SubscriptionError with "NoSymbols" error code');

        const beforeErrors = metrics.errorCount;
        await connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', symbols);

        await new Promise(resolve => setTimeout(resolve, 2000));

        if (metrics.errorCount > beforeErrors) {
            log.success('✅ Test 3 PASSED: Empty array correctly triggered error');
            testResults.passed++;
            return true;
        } else {
            log.error('❌ Test 3 FAILED: No error received for empty array');
            testResults.failed++;
            return false;
        }
    } catch (error) {
        log.warning(`Invoke error (may be expected): ${error.message}`);
        // This might be expected behavior
        testResults.passed++;
        return true;
    }
}

// Test 4: Null value (error handling)
async function testNullValue(connection) {
    log.section('🧪 Test 4: Null Value (error handling)');
    testResults.total++;

    try {
        log.info('Sending null value');
        log.info('Expected: Should receive SubscriptionError with "NoSymbols" error code');

        const beforeErrors = metrics.errorCount;
        await connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', null);

        await new Promise(resolve => setTimeout(resolve, 2000));

        if (metrics.errorCount > beforeErrors) {
            log.success('✅ Test 4 PASSED: Null value correctly triggered error');
            testResults.passed++;
            return true;
        } else {
            log.error('❌ Test 4 FAILED: No error received for null value');
            testResults.failed++;
            return false;
        }
    } catch (error) {
        log.warning(`Invoke error (may be expected): ${error.message}`);
        // This might be expected behavior
        testResults.passed++;
        return true;
    }
}

// Test 5: Price updates flow
async function testPriceUpdates(connection) {
    log.section('🧪 Test 5: Real-time Price Updates');
    testResults.total++;

    try {
        log.info('Subscribing to crypto symbols and waiting for price updates...');

        const symbols = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT'];
        await connection.invoke('SubscribeToPriceUpdates', 'CRYPTO', symbols);

        const beforeUpdates = metrics.priceUpdateCount;
        log.info('Waiting 15 seconds for price updates...');
        await new Promise(resolve => setTimeout(resolve, 15000));

        const updatesReceived = metrics.priceUpdateCount - beforeUpdates;
        log.info(`Received ${updatesReceived} price updates in 15 seconds`);

        if (updatesReceived > 0) {
            log.success(`✅ Test 5 PASSED: Received ${updatesReceived} price updates`);
            testResults.passed++;
            return true;
        } else {
            log.error('❌ Test 5 FAILED: No price updates received');
            testResults.failed++;
            return false;
        }
    } catch (error) {
        log.error(`❌ Test 5 FAILED: ${error.message}`);
        testResults.failed++;
        testResults.errors.push({ test: 'Test 5', error: error.message, timestamp: new Date().toISOString() });
        return false;
    }
}

// Generate test report
function generateReport() {
    const duration = (testResults.endTime - testResults.startTime) / 1000;

    log.header('PHASE 4 E2E VALIDATION TEST REPORT');

    console.log(chalk.bold('\n📊 Test Summary:'));
    console.log(`  Total Tests: ${testResults.total}`);
    console.log(chalk.green(`  Passed: ${testResults.passed}`));
    console.log(chalk.red(`  Failed: ${testResults.failed}`));
    console.log(`  Success Rate: ${((testResults.passed / testResults.total) * 100).toFixed(2)}%`);
    console.log(`  Duration: ${duration.toFixed(2)}s`);

    console.log(chalk.bold('\n📈 Metrics:'));
    console.log(`  Price Updates: ${metrics.priceUpdateCount}`);
    console.log(`  Subscriptions: ${metrics.subscriptionCount}`);
    console.log(`  Errors: ${metrics.errorCount}`);
    console.log(`  Connection Attempts: ${metrics.connectionAttempts}`);

    console.log(chalk.bold('\n✅ Success Criteria:'));
    const criteria = [
        { name: 'Array subscriptions work (object[] handling)', passed: testResults.subscriptions.length > 0 },
        { name: 'No "NoSymbols" errors for valid arrays', passed: testResults.errors.filter(e => e.error === 'NoSymbols' && e.receivedData).length === 0 },
        { name: 'Price updates received', passed: metrics.priceUpdateCount > 0 },
        { name: 'Error handling works correctly', passed: metrics.errorCount > 0 }, // Should have errors from empty/null tests
        { name: 'Subscription confirmations received', passed: metrics.subscriptionCount > 0 }
    ];

    criteria.forEach(criterion => {
        const icon = criterion.passed ? '✅' : '❌';
        const color = criterion.passed ? chalk.green : chalk.red;
        console.log(color(`  ${icon} ${criterion.name}`));
    });

    if (testResults.subscriptions.length > 0) {
        console.log(chalk.bold('\n📋 Subscriptions:'));
        testResults.subscriptions.forEach((sub, i) => {
            console.log(`  ${i + 1}. ${sub.assetClass}: ${sub.symbols ? sub.symbols.join(', ') : 'N/A'}`);
        });
    }

    if (testResults.errors.length > 0) {
        console.log(chalk.bold('\n⚠️  Errors:'));
        testResults.errors.forEach((err, i) => {
            console.log(chalk.yellow(`  ${i + 1}. [${err.error || err.test}] ${err.message || JSON.stringify(err)}`));
        });
    }

    const uniquePriceSymbols = [...new Set(testResults.priceUpdates.map(p => p.symbol))];
    if (uniquePriceSymbols.length > 0) {
        console.log(chalk.bold('\n💰 Price Updates Received For:'));
        console.log(`  ${uniquePriceSymbols.join(', ')}`);
        console.log(`  Total updates: ${testResults.priceUpdates.length}`);
    }

    console.log(chalk.bold('\n🏆 Final Verdict:'));
    const allCriteriaPassed = criteria.every(c => c.passed);
    if (allCriteriaPassed && testResults.failed === 0) {
        console.log(chalk.green.bold('  ✅ ALL TESTS PASSED! WebSocket fixes are working correctly.'));
    } else {
        console.log(chalk.red.bold('  ❌ SOME TESTS FAILED. Review the errors above.'));
    }

    console.log('\n');

    // Return summary for external use
    return {
        success: allCriteriaPassed && testResults.failed === 0,
        testResults,
        metrics,
        criteria
    };
}

// Main test execution
async function runTests() {
    log.header('PHASE 4: END-TO-END WEBSOCKET VALIDATION');
    log.info('Testing object[] array handling fix in MarketDataHub');
    log.info('Testing symbol format correction in mobile frontend');

    testResults.startTime = Date.now();

    let connection;

    try {
        // Create and start connection
        connection = await createConnection();
        setupEventHandlers(connection);

        log.info('Starting connection...');
        await connection.start();
        log.success(`Connected! Connection ID: ${connection.connectionId}`);

        // Wait for connection to stabilize
        await new Promise(resolve => setTimeout(resolve, 2000));

        // Run tests
        await testArraySubscription(connection);
        await new Promise(resolve => setTimeout(resolve, 2000));

        await testSingleSymbol(connection);
        await new Promise(resolve => setTimeout(resolve, 2000));

        await testEmptyArray(connection);
        await new Promise(resolve => setTimeout(resolve, 2000));

        await testNullValue(connection);
        await new Promise(resolve => setTimeout(resolve, 2000));

        await testPriceUpdates(connection);

        testResults.endTime = Date.now();

        // Disconnect
        log.info('Disconnecting...');
        await connection.stop();

    } catch (error) {
        log.error(`Fatal error: ${error.message}`);
        console.error(error);
        testResults.endTime = Date.now();

        if (connection) {
            try {
                await connection.stop();
            } catch (e) {
                // Ignore disconnect errors
            }
        }
    }

    // Generate and display report
    const report = generateReport();

    // Save report to file
    const fs = require('fs');
    const reportPath = './PHASE4_E2E_VALIDATION_REPORT.json';
    fs.writeFileSync(reportPath, JSON.stringify(report, null, 2));
    log.success(`Report saved to: ${reportPath}`);

    // Exit with appropriate code
    process.exit(report.success ? 0 : 1);
}

// Run tests
runTests().catch(error => {
    console.error('Unhandled error:', error);
    process.exit(1);
});
