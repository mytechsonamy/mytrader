#!/usr/bin/env node

/**
 * MyTrader Mobile Connectivity Test Script
 * Tests API and WebSocket connectivity for React Native mobile app
 */

const fs = require('fs');
const https = require('https');
const http = require('http');

// Read app.json to get configuration
const appJsonPath = './app.json';
const appJson = JSON.parse(fs.readFileSync(appJsonPath, 'utf8'));
const config = appJson.expo.extra;

console.log('🔍 MyTrader Mobile Connectivity Test');
console.log('=====================================');
console.log(`API_BASE_URL: ${config.API_BASE_URL}`);
console.log(`AUTH_BASE_URL: ${config.AUTH_BASE_URL}`);
console.log(`WS_BASE_URL: ${config.WS_BASE_URL}`);
console.log('');

// Test endpoints
const testEndpoints = [
  { name: 'Health Check', url: config.API_BASE_URL.replace('/api', '/health') },
  { name: 'Auth Login (v1)', url: `${config.API_BASE_URL}/v1/auth/login` },
  { name: 'Auth Login (no v1)', url: `${config.API_BASE_URL}/auth/login` },
  { name: 'WebSocket Hub', url: config.WS_BASE_URL }
];

async function testEndpoint(endpoint) {
  return new Promise((resolve) => {
    const url = new URL(endpoint.url);
    const client = url.protocol === 'https:' ? https : http;

    const options = {
      hostname: url.hostname,
      port: url.port || (url.protocol === 'https:' ? 443 : 80),
      path: url.pathname,
      method: endpoint.name.includes('Login') ? 'POST' : 'GET',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': 'MyTrader-Mobile-Test/1.0'
      },
      timeout: 5000
    };

    const req = client.request(options, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        resolve({
          name: endpoint.name,
          url: endpoint.url,
          status: res.statusCode,
          success: res.statusCode < 500, // 2xx, 3xx, 4xx are OK (500+ are not)
          headers: res.headers,
          body: data.substring(0, 200)
        });
      });
    });

    req.on('timeout', () => {
      req.destroy();
      resolve({
        name: endpoint.name,
        url: endpoint.url,
        status: 'TIMEOUT',
        success: false,
        error: 'Request timed out after 5s'
      });
    });

    req.on('error', (err) => {
      resolve({
        name: endpoint.name,
        url: endpoint.url,
        status: 'ERROR',
        success: false,
        error: err.message
      });
    });

    // Send test login data for auth endpoints
    if (endpoint.name.includes('Login')) {
      req.write(JSON.stringify({ email: 'test@example.com', password: 'test' }));
    }

    req.end();
  });
}

async function runTests() {
  console.log('🧪 Running connectivity tests...\n');

  const results = [];
  for (const endpoint of testEndpoints) {
    console.log(`Testing ${endpoint.name}...`);
    const result = await testEndpoint(endpoint);
    results.push(result);

    const statusIcon = result.success ? '✅' : '❌';
    console.log(`${statusIcon} ${result.name}: ${result.status}`);
    if (result.error) {
      console.log(`   Error: ${result.error}`);
    }
    if (result.body && result.body.trim()) {
      console.log(`   Response: ${result.body.substring(0, 100)}...`);
    }
    console.log('');
  }

  console.log('📊 Test Summary:');
  console.log('=================');
  const successCount = results.filter(r => r.success).length;
  console.log(`Successful: ${successCount}/${results.length}`);

  if (successCount === results.length) {
    console.log('🎉 All connectivity tests passed!');
    console.log('✨ Mobile app should be able to connect to the backend.');
  } else {
    console.log('⚠️  Some connectivity issues detected.');
    console.log('🔧 Please check backend server status and network configuration.');
  }

  // URL Building Test
  console.log('\n🔗 URL Building Test:');
  console.log('=====================');
  const buildCandidates = (baseUrl, path) => {
    const base = baseUrl.replace(/\/$/, '');
    const cleanPath = path.startsWith('/') ? path : `/${path}`;

    const hasV1Suffix = base.endsWith('/v1');
    const hasApiSuffix = base.endsWith('/api') || hasV1Suffix;

    const rootUrl = hasV1Suffix
      ? base.slice(0, -3)
      : hasApiSuffix
        ? base.slice(0, -4)
        : base;

    const apiBase = `${rootUrl}/api`;
    const v1Base = `${rootUrl}/api/v1`;

    const withoutV1Path = cleanPath.startsWith('/v1/') ? cleanPath.substring(3) : cleanPath;

    const candidates = [
      `${v1Base}${withoutV1Path}`,
      `${apiBase}${withoutV1Path}`,
      `${rootUrl}${cleanPath}`,
      `${base}${withoutV1Path}`,
    ];

    return Array.from(new Set(candidates));
  };

  const testPath = '/auth/login';
  const candidates = buildCandidates(config.API_BASE_URL, testPath);
  console.log(`Base URL: ${config.API_BASE_URL}`);
  console.log(`Path: ${testPath}`);
  console.log('Generated candidates:');
  candidates.forEach((url, index) => {
    console.log(`  ${index + 1}. ${url}`);
  });

  console.log('\n🚀 Ready to test with iOS simulator or physical device!');
  console.log('Run: npx expo start --clear');
}

runTests().catch(console.error);