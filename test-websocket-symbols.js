#!/usr/bin/env node

const signalR = require("@microsoft/signalr");

const API_BASE = "http://192.168.68.102:5002";

console.log("=".repeat(80));
console.log("WEBSOCKET SYMBOL BROADCAST TEST");
console.log("=".repeat(80));
console.log(`Connecting to: ${API_BASE}/hubs/market-data`);

const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE}/hubs/market-data`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
    })
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

const receivedSymbols = new Set();
const priceUpdates = {};

// Track price updates
connection.on("ReceivePriceUpdate", (data) => {
    const symbol = data.symbol || data.Symbol;
    receivedSymbols.add(symbol);
    priceUpdates[symbol] = (priceUpdates[symbol] || 0) + 1;
    console.log(`[PRICE UPDATE] ${symbol}: $${data.price || data.Price} (Update #${priceUpdates[symbol]})`);
});

async function runTest() {
    try {
        await connection.start();
        console.log("\n✅ Connected to SignalR hub");

        // Subscribe to price updates for CRYPTO asset class with all symbols
        console.log("\n📡 Subscribing to CRYPTO price updates...");
        await connection.invoke("SubscribeToAssetClass", "CRYPTO");

        // Wait 15 seconds to collect updates
        console.log("⏳ Collecting updates for 15 seconds...\n");
        await new Promise(resolve => setTimeout(resolve, 15000));

        // Report results
        console.log("\n" + "=".repeat(80));
        console.log("TEST RESULTS");
        console.log("=".repeat(80));
        console.log(`Total unique symbols received: ${receivedSymbols.size}`);
        console.log(`Expected symbols: 9 (BTC, ETH, XRP, SOL, AVAX, SUI, ENA, UNI, BNB)`);

        console.log("\nSymbols broadcast:");
        const sortedSymbols = Array.from(receivedSymbols).sort();
        sortedSymbols.forEach(symbol => {
            console.log(`  - ${symbol} (${priceUpdates[symbol]} updates)`);
        });

        // Validate against expected symbols
        const expectedSymbols = [
            'BTCUSDT', 'ETHUSDT', 'XRPUSDT', 'SOLUSDT',
            'AVAXUSDT', 'SUIUSDT', 'ENAUSDT', 'UNIUSDT', 'BNBUSDT'
        ];

        const missing = expectedSymbols.filter(s => !receivedSymbols.has(s));
        const unexpected = sortedSymbols.filter(s => !expectedSymbols.includes(s));

        console.log("\nValidation:");
        if (missing.length === 0 && unexpected.length === 0) {
            console.log("✅ SUCCESS: All expected symbols broadcast, no unexpected symbols");
        } else {
            if (missing.length > 0) {
                console.log(`❌ MISSING: ${missing.join(', ')}`);
            }
            if (unexpected.length > 0) {
                console.log(`⚠️  UNEXPECTED: ${unexpected.join(', ')}`);
            }
        }

        await connection.stop();
        process.exit(missing.length === 0 && unexpected.length === 0 ? 0 : 1);

    } catch (error) {
        console.error("❌ Test failed:", error.message);
        process.exit(1);
    }
}

runTest();
