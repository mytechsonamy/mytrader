/**
 * K6 Performance Test - Scenario 1: Baseline Performance
 *
 * Objective: Establish baseline metrics for Alpaca streaming integration
 * Configuration:
 * - 30 symbols subscribed (AAPL, MSFT, GOOGL, etc.)
 * - 1 update per second per symbol (average market rate)
 * - 10 SignalR clients connected
 * - Duration: 10 minutes
 *
 * Success Criteria:
 * - P50 latency <500ms
 * - P95 latency <2s
 * - P99 latency <5s
 * - Memory stable (<500MB increase)
 * - CPU <50%
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend, Gauge } from 'k6/metrics';
import ws from 'k6/ws';

// Custom metrics
const wsConnections = new Gauge('websocket_connections');
const messageLatency = new Trend('message_latency_ms');
const messageRate = new Counter('messages_received');
const subscriptionLatency = new Trend('subscription_latency_ms');
const errorRate = new Counter('errors');

// Test configuration
export const options = {
  scenarios: {
    baseline_load: {
      executor: 'constant-vus',
      vus: 10,
      duration: '10m',
      gracefulStop: '30s',
    },
  },
  thresholds: {
    'http_req_duration': ['p(50)<500', 'p(95)<2000', 'p(99)<5000'],
    'websocket_connections': ['value>0'],
    'message_latency_ms': ['p(50)<500', 'p(95)<2000', 'p(99)<5000'],
    'errors': ['count<100'], // Allow max 100 errors in 10 minutes
  },
};

// Test symbols (30 symbols)
const SYMBOLS = [
  // Crypto (8 symbols)
  'BTCUSD', 'ETHUSD', 'SOLUSD', 'AVAXUSD', 'LINKUSD', 'ADAUSD', 'DOTUSD', 'MATICUSD',
  // Stocks (22 symbols)
  'AAPL', 'GOOGL', 'MSFT', 'TSLA', 'AMZN', 'NVDA', 'META', 'NFLX', 'AMD', 'CRM',
  'JPM', 'BAC', 'WMT', 'V', 'MA', 'DIS', 'NKE', 'HD', 'PG', 'KO', 'MCD', 'VZ'
];

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5002';
const WS_URL = __ENV.WS_URL || 'ws://localhost:5002/marketDataHub';

// Authentication token (you'll need to obtain this from login)
let authToken = __ENV.AUTH_TOKEN || null;

export function setup() {
  // Perform login and get auth token
  if (!authToken) {
    const loginRes = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify({
      username: 'test@example.com',
      password: 'TestPassword123!'
    }), {
      headers: { 'Content-Type': 'application/json' },
    });

    if (loginRes.status === 200) {
      const body = JSON.parse(loginRes.body);
      authToken = body.token;
    } else {
      console.error('Login failed:', loginRes.status, loginRes.body);
    }
  }

  // Health check
  const healthRes = http.get(`${BASE_URL}/health`);
  check(healthRes, {
    'health check returns 200': (r) => r.status === 200,
  });

  return { authToken };
}

export default function (data) {
  const token = data.authToken || authToken;

  if (!token) {
    console.error('No auth token available');
    errorRate.add(1);
    return;
  }

  // Connect to SignalR WebSocket
  const url = `${WS_URL}?access_token=${token}`;

  const params = {
    tags: { name: 'MarketDataHub' },
  };

  let messagesReceived = 0;
  let connectionTime = Date.now();

  const res = ws.connect(url, params, function (socket) {
    wsConnections.add(1);

    socket.on('open', () => {
      const openLatency = Date.now() - connectionTime;
      subscriptionLatency.add(openLatency);
      console.log(`WebSocket connected in ${openLatency}ms`);

      // SignalR handshake
      socket.send(JSON.stringify({
        protocol: 'json',
        version: 1
      }));
      socket.send('\x1e');

      // Subscribe to all symbols
      const subscribeStart = Date.now();

      // Subscribe to crypto symbols
      socket.send(JSON.stringify({
        type: 1,
        target: 'SubscribeToPriceUpdates',
        arguments: ['CRYPTO', SYMBOLS.filter(s => s.endsWith('USD'))]
      }));
      socket.send('\x1e');

      // Subscribe to stock symbols
      socket.send(JSON.stringify({
        type: 1,
        target: 'SubscribeToPriceUpdates',
        arguments: ['STOCK', SYMBOLS.filter(s => !s.endsWith('USD'))]
      }));
      socket.send('\x1e');

      const subscribeLatency = Date.now() - subscribeStart;
      subscriptionLatency.add(subscribeLatency);
    });

    socket.on('message', (data) => {
      try {
        const messages = data.split('\x1e').filter(m => m.trim());

        messages.forEach(msg => {
          if (!msg) return;

          const parsed = JSON.parse(msg);

          // Track message reception
          if (parsed.type === 1) { // Message type
            messagesReceived++;
            messageRate.add(1);

            // Calculate latency if timestamp is available
            if (parsed.arguments && parsed.arguments[0]) {
              const payload = parsed.arguments[0];
              if (payload.timestamp || payload.lastUpdated) {
                const serverTime = new Date(payload.timestamp || payload.lastUpdated);
                const latency = Date.now() - serverTime.getTime();

                if (latency > 0 && latency < 60000) { // Sanity check: latency < 60s
                  messageLatency.add(latency);
                }
              }
            }
          }
        });
      } catch (e) {
        console.error('Error parsing message:', e);
        errorRate.add(1);
      }
    });

    socket.on('error', (e) => {
      console.error('WebSocket error:', e);
      errorRate.add(1);
    });

    socket.on('close', () => {
      wsConnections.add(-1);
      console.log(`WebSocket closed. Received ${messagesReceived} messages`);
    });

    // Keep connection open for random duration (simulate real user behavior)
    const connectionDuration = Math.floor(Math.random() * 300) + 60; // 60-360 seconds
    socket.setTimeout(() => {
      socket.close();
    }, connectionDuration * 1000);
  });

  check(res, {
    'websocket connection successful': (r) => r && r.status === 101,
  });

  // Wait before reconnecting (simulate user behavior)
  sleep(Math.random() * 5 + 1); // 1-6 seconds
}

export function teardown(data) {
  console.log('Test completed. Cleaning up...');
}

export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    scenario: 'Baseline Performance',
    duration: '10 minutes',
    vus: 10,
    symbols: SYMBOLS.length,
    metrics: {
      http_req_duration: {
        p50: data.metrics.http_req_duration.values['p(50)'],
        p95: data.metrics.http_req_duration.values['p(95)'],
        p99: data.metrics.http_req_duration.values['p(99)'],
        avg: data.metrics.http_req_duration.values.avg,
        max: data.metrics.http_req_duration.values.max,
      },
      message_latency_ms: {
        p50: data.metrics.message_latency_ms?.values?.['p(50)'] || 0,
        p95: data.metrics.message_latency_ms?.values?.['p(95)'] || 0,
        p99: data.metrics.message_latency_ms?.values?.['p(99)'] || 0,
        avg: data.metrics.message_latency_ms?.values?.avg || 0,
      },
      subscription_latency_ms: {
        avg: data.metrics.subscription_latency_ms?.values?.avg || 0,
        max: data.metrics.subscription_latency_ms?.values?.max || 0,
      },
      messages_received: data.metrics.messages_received?.values?.count || 0,
      errors: data.metrics.errors?.values?.count || 0,
      iterations: data.metrics.iterations?.values?.count || 0,
    },
    checks: {
      passed: data.metrics.checks?.values?.passes || 0,
      failed: data.metrics.checks?.values?.fails || 0,
      rate: data.metrics.checks?.values?.rate || 0,
    },
    thresholds_met: Object.keys(data.metrics).every(key => {
      const metric = data.metrics[key];
      return !metric.thresholds || Object.values(metric.thresholds).every(t => t.ok);
    }),
  };

  return {
    'stdout': JSON.stringify(summary, null, 2),
    'performance-baseline-results.json': JSON.stringify(summary, null, 2),
  };
}
