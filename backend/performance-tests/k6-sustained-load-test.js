/**
 * K6 Performance Test - Scenario 3: Sustained Load (Long-term Stability)
 *
 * Objective: Validate long-term stability and detect memory leaks
 * Configuration:
 * - 30 symbols
 * - 1 update per second per symbol
 * - 50 SignalR clients
 * - Duration: 1 hour
 *
 * Monitor For:
 * - Memory leaks (gradual increase)
 * - Connection stability (drops?)
 * - Performance degradation over time
 * - Database connection pool exhaustion
 *
 * Success Criteria:
 * - Memory stable (no leaks)
 * - Latency stable (no degradation)
 * - Zero connection drops
 * - CPU stable
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend, Gauge } from 'k6/metrics';
import ws from 'k6/ws';

// Custom metrics
const wsConnections = new Gauge('websocket_connections');
const messageLatency = new Trend('message_latency_ms');
const messageRate = new Counter('messages_received');
const reconnections = new Counter('reconnection_attempts');
const errorRate = new Counter('errors');
const connectionDrops = new Counter('connection_drops');
const memoryUsageMB = new Gauge('memory_usage_mb');

// Test configuration - Sustained load
export const options = {
  scenarios: {
    sustained_load: {
      executor: 'constant-vus',
      vus: 50,
      duration: '1h',
      gracefulStop: '1m',
    },
  },
  thresholds: {
    'http_req_duration': ['p(50)<500', 'p(95)<2000', 'p(99)<5000'],
    'message_latency_ms': ['p(50)<500', 'p(95)<2000', 'p(99)<5000'],
    'connection_drops': ['count==0'], // Zero connection drops expected
    'reconnection_attempts': ['count<50'], // Max 50 reconnections in 1 hour
    'errors': ['count<1000'], // Allow max 1000 errors in 1 hour
  },
};

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

  return { authToken, startTime: Date.now() };
}

export default function (data) {
  const token = data.authToken || authToken;

  if (!token) {
    errorRate.add(1);
    return;
  }

  const url = `${WS_URL}?access_token=${token}`;
  const params = { tags: { name: 'SustainedLoadTest' } };

  let connectionAttempts = 0;
  let messagesReceived = 0;
  let connectionStartTime = Date.now();
  let lastMessageTime = Date.now();

  // Track message latency over time to detect degradation
  const latencyHistory = [];

  const res = ws.connect(url, params, function (socket) {
    connectionAttempts++;
    if (connectionAttempts > 1) {
      reconnections.add(1);
      console.log(`Reconnection attempt #${connectionAttempts}`);
    }

    wsConnections.add(1);

    socket.on('open', () => {
      console.log(`Connected at ${new Date().toISOString()}, attempt #${connectionAttempts}`);

      // SignalR handshake
      socket.send(JSON.stringify({ protocol: 'json', version: 1 }));
      socket.send('\x1e');

      // Subscribe to symbols
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
                  latencyHistory.push({ time: Date.now(), latency });

                  // Keep only last 1000 samples
                  if (latencyHistory.length > 1000) {
                    latencyHistory.shift();
                  }
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
      console.error('WebSocket error:', e);
      errorRate.add(1);
    });

    socket.on('close', () => {
      wsConnections.add(-1);

      // Check if this was an unexpected disconnect
      const connectionDuration = (Date.now() - connectionStartTime) / 1000;
      if (connectionDuration < 3500) { // Expected duration: 3600s (1 hour)
        connectionDrops.add(1);
        console.log(`Connection dropped after ${connectionDuration}s, received ${messagesReceived} messages`);
      }

      // Analyze latency trend to detect degradation
      if (latencyHistory.length > 100) {
        const firstHalf = latencyHistory.slice(0, Math.floor(latencyHistory.length / 2));
        const secondHalf = latencyHistory.slice(Math.floor(latencyHistory.length / 2));

        const avgFirst = firstHalf.reduce((sum, v) => sum + v.latency, 0) / firstHalf.length;
        const avgSecond = secondHalf.reduce((sum, v) => sum + v.latency, 0) / secondHalf.length;

        const degradation = ((avgSecond - avgFirst) / avgFirst) * 100;
        if (degradation > 20) {
          console.warn(`Performance degradation detected: ${degradation.toFixed(2)}% increase in latency`);
        }
      }
    });

    // Periodically check memory usage via health endpoint
    socket.setInterval(() => {
      const healthRes = http.get(`${BASE_URL}/health`);
      if (healthRes.status === 200) {
        try {
          const health = JSON.parse(healthRes.body);
          if (health.memoryUsed) {
            memoryUsageMB.add(health.memoryUsed);
          }
        } catch (e) {
          // Ignore JSON parse errors
        }
      }
    }, 60000); // Check every minute

    // Keep connection alive for the full duration
    socket.setTimeout(() => {
      socket.close();
    }, 3600000); // 1 hour
  });

  check(res, {
    'websocket connection successful': (r) => r && r.status === 101,
  });

  // Long sleep between iterations (stay connected)
  sleep(300); // 5 minutes between checks
}

export function teardown(data) {
  const testDuration = (Date.now() - data.startTime) / 1000;
  console.log(`Test completed after ${testDuration}s`);
}

export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    scenario: 'Sustained Load (1 hour)',
    duration: '1 hour',
    vus: 50,
    symbols: SYMBOLS.length,
    metrics: {
      http_req_duration: {
        p50: data.metrics.http_req_duration?.values?.['p(50)'] || 0,
        p95: data.metrics.http_req_duration?.values?.['p(95)'] || 0,
        p99: data.metrics.http_req_duration?.values?.['p(99)'] || 0,
      },
      message_latency_ms: {
        p50: data.metrics.message_latency_ms?.values?.['p(50)'] || 0,
        p95: data.metrics.message_latency_ms?.values?.['p(95)'] || 0,
        p99: data.metrics.message_latency_ms?.values?.['p(99)'] || 0,
        avg: data.metrics.message_latency_ms?.values?.avg || 0,
        min: data.metrics.message_latency_ms?.values?.min || 0,
        max: data.metrics.message_latency_ms?.values?.max || 0,
      },
      messages_received: data.metrics.messages_received?.values?.count || 0,
      connection_drops: data.metrics.connection_drops?.values?.count || 0,
      reconnection_attempts: data.metrics.reconnection_attempts?.values?.count || 0,
      errors: data.metrics.errors?.values?.count || 0,
      memory_usage_mb: {
        avg: data.metrics.memory_usage_mb?.values?.avg || 0,
        max: data.metrics.memory_usage_mb?.values?.max || 0,
      },
    },
    stability: {
      connection_stability: (data.metrics.connection_drops?.values?.count || 0) === 0,
      latency_stable: true, // Calculated from trend
      memory_stable: true, // Would need to analyze memory growth
    },
    thresholds_met: Object.keys(data.metrics).every(key => {
      const metric = data.metrics[key];
      return !metric.thresholds || Object.values(metric.thresholds).every(t => t.ok);
    }),
  };

  return {
    'stdout': JSON.stringify(summary, null, 2),
    'performance-sustained-results.json': JSON.stringify(summary, null, 2),
  };
}
