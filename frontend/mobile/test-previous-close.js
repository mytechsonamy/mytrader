#!/usr/bin/env node

/**
 * Test script to validate previousClose field is being received and displayed
 * Run this to simulate what the mobile app should be receiving
 */

const WebSocket = require('ws');

const WS_URL = 'ws://localhost:5002/hubs/trading';
const TEST_SYMBOLS = ['AAPL', 'MSFT', 'GARAN', 'THYAO'];

console.log('🔍 TESTING PREVIOUS CLOSE FIELD TRANSMISSION\n');
console.log('Connecting to:', WS_URL);

const ws = new WebSocket(WS_URL);

ws.on('open', () => {
  console.log('✅ Connected to WebSocket\n');

  // Subscribe to test symbols
  const subscription = {
    type: 'subscribe',
    assetClass: 'STOCK',
    symbols: TEST_SYMBOLS
  };

  console.log('📤 Subscribing to:', TEST_SYMBOLS.join(', '));
  ws.send(JSON.stringify(subscription));
});

ws.on('message', (data) => {
  try {
    const message = JSON.parse(data.toString());

    // Check for price_update messages
    if (message.type === 'price_update' || message.price !== undefined) {
      console.log('\n📦 RECEIVED PRICE UPDATE:');
      console.log('Symbol:', message.symbol || message.Symbol);
      console.log('Price:', message.price || message.Price);

      // Check all possible previousClose field names
      const previousCloseFields = {
        'previousClose': message.previousClose,
        'PreviousClose': message.PreviousClose,
        'prevClose': message.prevClose,
        'PrevClose': message.PrevClose,
        'previous_close': message.previous_close
      };

      console.log('\n🔍 CHECKING PREVIOUS CLOSE FIELDS:');
      let foundField = null;
      Object.entries(previousCloseFields).forEach(([field, value]) => {
        if (value !== undefined) {
          console.log(`  ✅ ${field}: ${value}`);
          foundField = field;
        } else {
          console.log(`  ❌ ${field}: undefined`);
        }
      });

      if (!foundField) {
        console.error('\n⚠️ WARNING: NO PREVIOUS CLOSE FIELD FOUND!');
        console.log('Available fields:', Object.keys(message));
      }

      // Check percentage calculation
      if (message.changePercent !== undefined) {
        console.log('\n📊 PERCENTAGE CALCULATION:');
        console.log('Change Percent:', message.changePercent);

        const previousClose = previousCloseFields[foundField];
        if (previousClose && message.price) {
          const calculatedPercent = ((message.price - previousClose) / previousClose) * 100;
          console.log('Expected Percent:', calculatedPercent.toFixed(2));
          console.log('Match:', Math.abs(calculatedPercent - message.changePercent) < 0.01 ? '✅' : '❌');
        }
      }

      console.log('\n' + '='.repeat(60));
    }
  } catch (error) {
    console.error('Error parsing message:', error);
  }
});

ws.on('error', (error) => {
  console.error('❌ WebSocket error:', error.message);
});

ws.on('close', () => {
  console.log('\n🔴 WebSocket connection closed');
});

// Run for 30 seconds then exit
setTimeout(() => {
  console.log('\n✅ Test complete. Closing connection...');
  ws.close();
  process.exit(0);
}, 30000);

console.log('\n⏳ Waiting for price updates (30 seconds)...\n');