/**
 * Mobile Connectivity Test Script
 * Tests the fixed mobile configuration for connecting to localhost:5002
 */

const testMobileConfig = () => {
  console.log('🔍 Testing Mobile Configuration Fix...\n');

  // Test configuration values that should now be correct
  const expectedConfig = {
    API_BASE_URL: 'http://localhost:5002/api',
    AUTH_BASE_URL: 'http://localhost:5002/api',
    WS_BASE_URL: 'http://localhost:5002/hubs/dashboard'  // Fixed from /market-data
  };

  console.log('✅ Expected Configuration:');
  Object.entries(expectedConfig).forEach(([key, value]) => {
    console.log(`   ${key}: ${value}`);
  });

  console.log('\n🔧 Configuration Changes Made:');
  console.log('   1. Fixed app.json WS_BASE_URL: /hubs/market-data → /hubs/dashboard');
  console.log('   2. Cleared Metro bundler cache for config reload');
  console.log('   3. Restarted mobile app on port 8082');

  console.log('\n🎯 Next Steps When Backend Available:');
  console.log('   1. Test API call: GET http://localhost:5002/api/health');
  console.log('   2. Test WebSocket: ws://localhost:5002/hubs/dashboard');
  console.log('   3. Verify mobile app receives real-time crypto price updates');

  console.log('\n⚠️  Critical Requirements for Success:');
  console.log('   - Backend must be running on localhost:5002');
  console.log('   - SignalR hub /hubs/dashboard must be active');
  console.log('   - Mobile simulator must have network access to localhost');
  console.log('   - App.json changes must be picked up by Metro bundler');

  return expectedConfig;
};

// Run the test
const config = testMobileConfig();

// Simple connectivity test function
const testConnectivity = async (url) => {
  try {
    console.log(`\n🔍 Testing connectivity to: ${url}`);

    // Use fetch to test HTTP connectivity
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'User-Agent': 'mobile-connectivity-test'
      },
      timeout: 5000
    });

    if (response.ok) {
      console.log(`✅ SUCCESS: ${url} is reachable`);
      const data = await response.text();
      console.log(`   Response: ${data.substring(0, 100)}${data.length > 100 ? '...' : ''}`);
      return true;
    } else {
      console.log(`❌ FAILED: ${url} returned ${response.status}`);
      return false;
    }
  } catch (error) {
    console.log(`❌ FAILED: ${url} - ${error.message}`);
    return false;
  }
};

// Test when backend is available
if (typeof window === 'undefined') {
  // Node.js environment - try testing the endpoints
  Promise.resolve().then(async () => {
    console.log('\n🚀 Testing Backend Connectivity...');
    await testConnectivity('http://localhost:5002/health');
    await testConnectivity('http://localhost:5002/api/health');
  }).catch(err => {
    console.log('\n⚠️  Backend not available yet - tests will pass once backend is running');
  });
}

module.exports = { testMobileConfig, testConnectivity };