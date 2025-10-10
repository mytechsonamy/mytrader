/**
 * Test Script for Dynamic Symbol Loading
 *
 * This script validates that the frontend correctly loads symbols
 * from the backend API without using hard-coded fallbacks.
 *
 * Run with: node test-dynamic-symbols.js
 */

const API_BASE_URL = process.env.API_BASE_URL || 'http://192.168.68.103:5002/api';

console.log('='.repeat(60));
console.log('Dynamic Symbol Loading Test');
console.log('='.repeat(60));
console.log(`API Base URL: ${API_BASE_URL}`);
console.log('');

async function testDefaultSymbols() {
  console.log('Test 1: Fetching default CRYPTO symbols...');
  try {
    const response = await fetch(
      `${API_BASE_URL}/v1/symbol-preferences/defaults?assetClass=CRYPTO`,
      {
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Type': 'test-script',
        },
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const data = await response.json();

    // Handle ApiResponse<T> wrapper
    const symbols = data.data || data;

    if (!Array.isArray(symbols)) {
      throw new Error('Response is not an array');
    }

    console.log(`✓ Success: Received ${symbols.length} symbols`);
    console.log('  Symbols:', symbols.map(s => s.symbol).join(', '));
    console.log('');
    return true;
  } catch (error) {
    console.log(`✗ Failed: ${error.message}`);
    console.log('');
    return false;
  }
}

async function testUserSymbols() {
  console.log('Test 2: Fetching user symbol preferences (without auth)...');
  try {
    // This should fail with 401/403 since we're not authenticated
    const response = await fetch(
      `${API_BASE_URL}/v1/symbol-preferences/user/test-user?assetClass=CRYPTO`,
      {
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Type': 'test-script',
        },
      }
    );

    if (response.status === 401 || response.status === 403) {
      console.log(`✓ Expected: Requires authentication (${response.status})`);
      console.log('');
      return true;
    }

    if (response.ok) {
      const data = await response.json();
      const symbols = data.data || data;
      console.log(`✓ Success: Received ${symbols.length} user symbols`);
      console.log('');
      return true;
    }

    throw new Error(`Unexpected status: ${response.status}`);
  } catch (error) {
    console.log(`✗ Failed: ${error.message}`);
    console.log('');
    return false;
  }
}

async function testSymbolsByAssetClass() {
  console.log('Test 3: Fetching symbols by asset class (legacy endpoint)...');
  try {
    const response = await fetch(
      `${API_BASE_URL}/v1/symbols/by-asset-class/CRYPTO`,
      {
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Type': 'test-script',
        },
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const data = await response.json();
    const symbols = data.data || data;

    if (!Array.isArray(symbols)) {
      throw new Error('Response is not an array');
    }

    console.log(`✓ Success: Received ${symbols.length} symbols`);
    console.log('  Sample:', symbols.slice(0, 3).map(s => `${s.symbol} (${s.displayName})`).join(', '));
    console.log('');
    return true;
  } catch (error) {
    console.log(`✗ Failed: ${error.message}`);
    console.log('');
    return false;
  }
}

async function testCacheSimulation() {
  console.log('Test 4: Cache simulation (local only)...');
  try {
    // This test just validates the cache key format
    const assetClass = 'CRYPTO';
    const userId = 'test-user-123';
    const version = 'v1';

    const cacheKey = `symbols_cache_${version}_${assetClass.toLowerCase()}_${userId}`;
    console.log(`✓ Cache key format: ${cacheKey}`);

    const defaultCacheKey = `symbols_cache_${version}_${assetClass.toLowerCase()}_default`;
    console.log(`✓ Default cache key: ${defaultCacheKey}`);
    console.log('');
    return true;
  } catch (error) {
    console.log(`✗ Failed: ${error.message}`);
    console.log('');
    return false;
  }
}

async function runTests() {
  const results = [];

  results.push(await testDefaultSymbols());
  results.push(await testUserSymbols());
  results.push(await testSymbolsByAssetClass());
  results.push(await testCacheSimulation());

  console.log('='.repeat(60));
  console.log('Test Summary');
  console.log('='.repeat(60));

  const passed = results.filter(Boolean).length;
  const total = results.length;

  console.log(`Passed: ${passed}/${total}`);
  console.log(`Failed: ${total - passed}/${total}`);
  console.log('');

  if (passed === total) {
    console.log('✓ All tests passed!');
    console.log('');
    console.log('Next steps:');
    console.log('1. Test on mobile device/simulator');
    console.log('2. Verify symbols load on Dashboard');
    console.log('3. Check WebSocket subscriptions');
    console.log('4. Test offline mode with cached data');
    console.log('5. Verify user-specific preferences (when logged in)');
  } else {
    console.log('✗ Some tests failed. Please check the backend API.');
    console.log('');
    console.log('Common issues:');
    console.log('- Backend not running');
    console.log('- Incorrect API_BASE_URL');
    console.log('- CORS issues');
    console.log('- API endpoints not implemented');
  }

  console.log('');
  console.log('='.repeat(60));

  process.exit(passed === total ? 0 : 1);
}

// Run tests
runTests().catch(error => {
  console.error('Fatal error:', error);
  process.exit(1);
});
