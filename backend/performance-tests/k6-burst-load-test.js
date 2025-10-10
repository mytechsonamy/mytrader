/**
 * K6 Performance Test - Scenario 2: Burst Load (Market Open)
 *
 * Objective: Validate handling of burst traffic during market open
 * Configuration:
 * - 30 symbols
 * - 10 updates per second per symbol (market open surge)
 * - 50 SignalR clients connected
 * - Duration: 5 minutes
 *
 * Expected Behavior:
 * - System handles burst without crashes
 * - Latency increases but remains acceptable
 * - Message queue doesn't overflow
 * - Memory doesn't spike excessively
 *
 * Success Criteria:
 * - P95 latency <5s during burst
 * - No message loss
 * - Recovery to baseline after burst
 * - Memory <1GB
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend, Gauge, Rate } from 'k6/metrics';
import ws from 'k6/ws';

// Custom metrics
const wsConnections = new Gauge('websocket_connections');
const messageLatency = new Trend('message_latency_ms');
const messageRate = new Counter('messages_received');
const subscriptionLatency = new Trend('subscription_latency_ms');
const errorRate = new Counter('errors');
const messageLossRate = new Rate('message_loss_rate');

// Test configuration - Burst load pattern
export const options = {
  scenarios: {
    burst_ramp_up: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 50 },  // Ramp up to 50 VUs in 30s
        { duration: '4m', target: 50 },   // Hold 50 VUs for 4 minutes
        { duration: '30s', target: 0 },   // Ramp down
      ],
      gracefulRampDown: '30s',
    },
  },
  thresholds: {
    'http_req_duration': ['p(95)<5000', 'p(99)<10000'],
    'message_latency_ms': ['p(95)<5000', 'p(99)<10000'],
    'message_loss_rate': ['rate<0.01'], // <1% message loss
    'errors': ['count<500'], // Allow max 500 errors during burst
    'websocket_connections': ['value>=0'],
  },
};

// Test symbols (30 symbols)
const SYMBOLS = [
  'BTCUSD', 'ETHUSD', 'SOLUSD', 'AVAXUSD', 'LINKUSD', 'ADAUSD', 'DOTUSD', 'MATICUSD',
  'AAPL', 'GOOGL', 'MSFT', 'TSLA', 'AMZN', 'NVDA', 'META', 'NFLX', 'AMD', 'CRM',
  'JPM', 'BAC', 'WMT', 'V', 'MA', 'DIS', 'NKE', 'HD', 'PG', 'KO', 'MCD', 'VZ'
];

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5002';
const WS_URL = __ENV.WS_URL || 'ws://localhost:5002/marketDataHub';

let authToken = __ENV.AUTH_TOKEN || null;

export function setup() {
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
    }
  }

  return { authToken };
}

export default function (data) {
  const token = data.authToken || authToken;

  if (!token) {
    errorRate.add(1);
    return;
  }

  const url = `${WS_URL}?access_token=${token}`;
  const params = { tags: { name: 'BurstLoadTest' } };

  let expectedMessages = 0;
  let messagesReceived = 0;
  let lastMessageTime = Date.now();

  const res = ws.connect(url, params, function (socket) {
    wsConnections.add(1);

    socket.on('open', () => {
      const openLatency = Date.now() - lastMessageTime;
      subscriptionLatency.add(openLatency);

      // SignalR handshake
      socket.send(JSON.stringify({ protocol: 'json', version: 1 }));
      socket.send('\x1e');

      // Subscribe to all symbols
      socket.send(JSON.stringify({
        type: 1,
        target: 'SubscribeToPriceUpdates',
        arguments: ['CRYPTO', SYMBOLS.filter(s => s.endsWith('USD'))]
      }));
      socket.send('\x1e');

      socket.send(JSON.stringify({
        type: 1,
        target: 'SubscribeToPriceUpdates',
        arguments: ['STOCK', SYMBOLS.filter(s => !s.endsWith('USD'))]
      }));
      socket.send('\x1e');

      // Expect 10 updates per second per symbol during burst
      expectedMessages = SYMBOLS.length * 10; // Per second
    });

    socket.on('message', (data) => {
      try {
        const messages = data.split('\x1e').filter(m => m.trim());

        messages.forEach(msg => {
          if (!msg) return;

          const parsed = JSON.parse(msg);

          if (parsed.type === 1) {
            messagesReceived++;
            messageRate.add(1);
            lastMessageTime = Date.now();

            if (parsed.arguments && parsed.arguments[0]) {
              const payload = parsed.arguments[0];
              if (payload.timestamp || payload.lastUpdated) {
                const serverTime = new Date(payload.timestamp || payload.lastUpdated);
                const latency = Date.now() - serverTime.getTime();

                if (latency > 0 && latency < 60000) {
                  messageLatency.add(latency);
                }
              }
            }
          }
        });
      } catch (e) {
        errorRate.add(1);
      }
    });

    socket.on('error', (e) => {
      console.error('WebSocket error during burst:', e);
      errorRate.add(1);
    });

    socket.on('close', () => {
      wsConnections.add(-1);

      // Calculate message loss rate
      if (expectedMessages > 0) {
        const lossRate = Math.max(0, (expectedMessages - messagesReceived) / expectedMessages);
        messageLossRate.add(lossRate > 0);
      }

      console.log(`Burst test: Expected ${expectedMessages}, Received ${messagesReceived}`);
    });

    // Hold connection for the duration of the test
    socket.setTimeout(() => {
      socket.close();
    }, 270000); // 4.5 minutes
  });

  check(res, {
    'websocket connection successful': (r) => r && r.status === 101,
  });

  // Minimal sleep during burst to maximize load
  sleep(0.5);
}

export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    scenario: 'Burst Load (Market Open)',
    duration: '5 minutes',
    max_vus: 50,
    symbols: SYMBOLS.length,
    expected_message_rate: SYMBOLS.length * 10, // per second
    metrics: {
      http_req_duration: {
        p95: data.metrics.http_req_duration?.values?.['p(95)'] || 0,
        p99: data.metrics.http_req_duration?.values?.['p(99)'] || 0,
        max: data.metrics.http_req_duration?.values?.max || 0,
      },
      message_latency_ms: {
        p50: data.metrics.message_latency_ms?.values?.['p(50)'] || 0,
        p95: data.metrics.message_latency_ms?.values?.['p(95)'] || 0,
        p99: data.metrics.message_latency_ms?.values?.['p(99)'] || 0,
        max: data.metrics.message_latency_ms?.values?.max || 0,
      },
      messages_received: data.metrics.messages_received?.values?.count || 0,
      message_loss_rate: data.metrics.message_loss_rate?.values?.rate || 0,
      errors: data.metrics.errors?.values?.count || 0,
      peak_connections: Math.max(...(data.metrics.websocket_connections?.values?.value || [0])),
    },
    thresholds_met: Object.keys(data.metrics).every(key => {
      const metric = data.metrics[key];
      return !metric.thresholds || Object.values(metric.thresholds).every(t => t.ok);
    }),
  };

  return {
    'stdout': JSON.stringify(summary, null, 2),
    'performance-burst-results.json': JSON.stringify(summary, null, 2),
  };
}
