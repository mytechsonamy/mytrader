/**
 * K6 Performance Test - Scenario 4: Failover Performance
 *
 * Objective: Measure failover overhead from Alpaca to Yahoo
 * Steps:
 * 1. Run baseline (Alpaca active)
 * 2. Force failover to Yahoo
 * 3. Measure transition time
 * 4. Run 5 minutes on Yahoo
 * 5. Restore Alpaca
 * 6. Measure recovery time
 * 7. Compare latencies
 *
 * Success Criteria:
 * - Failover <5s
 * - Recovery <120s
 * - No data loss
 * - Latency acceptable on Yahoo
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend, Gauge } from 'k6/metrics';
import ws from 'k6/ws';

// Custom metrics
const failoverTime = new Trend('failover_time_ms');
const recoveryTime = new Trend('recovery_time_ms');
const dataGapDuration = new Trend('data_gap_duration_ms');
const alpacaLatency = new Trend('alpaca_latency_ms');
const yahooLatency = new Trend('yahoo_latency_ms');
const messageRate = new Counter('messages_received');
const errorRate = new Counter('errors');
const providerSwitches = new Counter('provider_switches');

// Test configuration
export const options = {
  scenarios: {
    failover_test: {
      executor: 'constant-vus',
      vus: 10,
      duration: '15m', // Enough time for full failover cycle
      gracefulStop: '30s',
    },
  },
  thresholds: {
    'failover_time_ms': ['p(95)<5000'], // Failover <5s
    'recovery_time_ms': ['p(95)<120000'], // Recovery <120s
    'data_gap_duration_ms': ['p(95)<1000'], // Data gap <1s
    'alpaca_latency_ms': ['p(95)<2000'],
    'yahoo_latency_ms': ['p(95)<5000'], // Yahoo allowed higher latency
  },
};

const SYMBOLS = [
  'BTCUSD', 'ETHUSD', 'SOLUSD', 'AVAXUSD',
  'AAPL', 'GOOGL', 'MSFT', 'TSLA', 'AMZN', 'NVDA'
];

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5002';
const WS_URL = __ENV.WS_URL || 'ws://localhost:5002/marketDataHub';
const ADMIN_TOKEN = __ENV.ADMIN_TOKEN || null;

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

  // Ensure Alpaca is enabled at start
  if (ADMIN_TOKEN) {
    http.post(`${BASE_URL}/api/admin/providers/alpaca/enable`, null, {
      headers: { 'Authorization': `Bearer ${ADMIN_TOKEN}` },
    });
  }

  return {
    authToken,
    startTime: Date.now(),
    testPhases: {
      baseline: { start: 0, end: 180000 }, // 0-3 min: Baseline on Alpaca
      failover: { start: 180000, end: 480000 }, // 3-8 min: Failover to Yahoo
      recovery: { start: 480000, end: 900000 }, // 8-15 min: Recovery to Alpaca
    }
  };
}

export default function (data) {
  const token = data.authToken || authToken;
  const elapsedTime = Date.now() - data.startTime;

  if (!token) {
    errorRate.add(1);
    return;
  }

  // Determine current test phase
  let currentPhase = 'baseline';
  let currentProvider = 'ALPACA';

  if (elapsedTime >= data.testPhases.failover.start && elapsedTime < data.testPhases.recovery.start) {
    currentPhase = 'failover';
    currentProvider = 'YAHOO';

    // Trigger failover (only on first iteration in this phase)
    if (__ITER === Math.floor(data.testPhases.failover.start / 1000)) {
      console.log('Triggering failover to Yahoo...');
      const failoverStart = Date.now();

      if (ADMIN_TOKEN) {
        const failoverRes = http.post(`${BASE_URL}/api/admin/providers/alpaca/disable`, null, {
          headers: { 'Authorization': `Bearer ${ADMIN_TOKEN}` },
        });

        const failoverDuration = Date.now() - failoverStart;
        failoverTime.add(failoverDuration);

        console.log(`Failover triggered in ${failoverDuration}ms`);

        if (failoverRes.status !== 200) {
          console.error('Failover trigger failed:', failoverRes.status);
        }
      }

      providerSwitches.add(1);
    }
  } else if (elapsedTime >= data.testPhases.recovery.start) {
    currentPhase = 'recovery';
    currentProvider = 'ALPACA';

    // Trigger recovery (only on first iteration in this phase)
    if (__ITER === Math.floor(data.testPhases.recovery.start / 1000)) {
      console.log('Triggering recovery to Alpaca...');
      const recoveryStart = Date.now();

      if (ADMIN_TOKEN) {
        const recoveryRes = http.post(`${BASE_URL}/api/admin/providers/alpaca/enable`, null, {
          headers: { 'Authorization': `Bearer ${ADMIN_TOKEN}` },
        });

        const recoveryDuration = Date.now() - recoveryStart;
        recoveryTime.add(recoveryDuration);

        console.log(`Recovery triggered in ${recoveryDuration}ms`);

        if (recoveryRes.status !== 200) {
          console.error('Recovery trigger failed:', recoveryRes.status);
        }
      }

      providerSwitches.add(1);
    }
  }

  const url = `${WS_URL}?access_token=${token}`;
  const params = {
    tags: {
      name: 'FailoverTest',
      phase: currentPhase,
      provider: currentProvider
    }
  };

  let lastMessageTimestamp = null;
  let gapDetected = false;

  const res = ws.connect(url, params, function (socket) {
    socket.on('open', () => {
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

          if (parsed.type === 1 && parsed.arguments && parsed.arguments[0]) {
            messageRate.add(1);

            const payload = parsed.arguments[0];
            const messageTimestamp = Date.now();

            // Detect data gaps (no messages received for >2 seconds)
            if (lastMessageTimestamp) {
              const gap = messageTimestamp - lastMessageTimestamp;
              if (gap > 2000 && !gapDetected) {
                dataGapDuration.add(gap);
                gapDetected = true;
                console.log(`Data gap detected: ${gap}ms during ${currentPhase} phase`);
              }
            }

            lastMessageTimestamp = messageTimestamp;
            gapDetected = false;

            // Track latency by provider
            if (payload.timestamp || payload.lastUpdated) {
              const serverTime = new Date(payload.timestamp || payload.lastUpdated);
              const latency = messageTimestamp - serverTime.getTime();

              if (latency > 0 && latency < 60000) {
                if (currentProvider === 'ALPACA') {
                  alpacaLatency.add(latency);
                } else {
                  yahooLatency.add(latency);
                }
              }
            }

            // Detect provider from message metadata if available
            if (payload.provider && payload.provider !== currentProvider) {
              console.log(`Provider mismatch: Expected ${currentProvider}, got ${payload.provider}`);
            }
          }
        });
      } catch (e) {
        errorRate.add(1);
      }
    });

    socket.on('error', (e) => {
      console.error(`WebSocket error during ${currentPhase}:`, e);
      errorRate.add(1);
    });

    socket.on('close', () => {
      console.log(`Connection closed during ${currentPhase} phase`);
    });

    // Hold connection for 60 seconds
    socket.setTimeout(() => {
      socket.close();
    }, 60000);
  });

  check(res, {
    'websocket connection successful': (r) => r && r.status === 101,
  });

  sleep(5);
}

export function teardown(data) {
  // Restore Alpaca at the end
  if (ADMIN_TOKEN) {
    http.post(`${BASE_URL}/api/admin/providers/alpaca/enable`, null, {
      headers: { 'Authorization': `Bearer ${ADMIN_TOKEN}` },
    });
  }

  console.log('Failover test completed');
}

export function handleSummary(data) {
  const alpacaP95 = data.metrics.alpaca_latency_ms?.values?.['p(95)'] || 0;
  const yahooP95 = data.metrics.yahoo_latency_ms?.values?.['p(95)'] || 0;
  const latencyDifference = yahooP95 - alpacaP95;
  const latencyIncreasePercent = alpacaP95 > 0 ? (latencyDifference / alpacaP95) * 100 : 0;

  const summary = {
    timestamp: new Date().toISOString(),
    scenario: 'Failover Performance',
    duration: '15 minutes',
    phases: ['baseline (Alpaca)', 'failover (Yahoo)', 'recovery (Alpaca)'],
    metrics: {
      failover_time_ms: {
        avg: data.metrics.failover_time_ms?.values?.avg || 0,
        p95: data.metrics.failover_time_ms?.values?.['p(95)'] || 0,
      },
      recovery_time_ms: {
        avg: data.metrics.recovery_time_ms?.values?.avg || 0,
        p95: data.metrics.recovery_time_ms?.values?.['p(95)'] || 0,
      },
      data_gap_duration_ms: {
        avg: data.metrics.data_gap_duration_ms?.values?.avg || 0,
        max: data.metrics.data_gap_duration_ms?.values?.max || 0,
      },
      alpaca_latency_ms: {
        p50: data.metrics.alpaca_latency_ms?.values?.['p(50)'] || 0,
        p95: alpacaP95,
        p99: data.metrics.alpaca_latency_ms?.values?.['p(99)'] || 0,
      },
      yahoo_latency_ms: {
        p50: data.metrics.yahoo_latency_ms?.values?.['p(50)'] || 0,
        p95: yahooP95,
        p99: data.metrics.yahoo_latency_ms?.values?.['p(99)'] || 0,
      },
      latency_comparison: {
        difference_ms: latencyDifference,
        increase_percent: latencyIncreasePercent,
      },
      provider_switches: data.metrics.provider_switches?.values?.count || 0,
      messages_received: data.metrics.messages_received?.values?.count || 0,
      errors: data.metrics.errors?.values?.count || 0,
    },
    success_criteria: {
      failover_under_5s: (data.metrics.failover_time_ms?.values?.['p(95)'] || Infinity) < 5000,
      recovery_under_120s: (data.metrics.recovery_time_ms?.values?.['p(95)'] || Infinity) < 120000,
      no_data_loss: (data.metrics.data_gap_duration_ms?.values?.max || 0) < 5000,
      yahoo_latency_acceptable: yahooP95 < 5000,
    },
    thresholds_met: Object.keys(data.metrics).every(key => {
      const metric = data.metrics[key];
      return !metric.thresholds || Object.values(metric.thresholds).every(t => t.ok);
    }),
  };

  return {
    'stdout': JSON.stringify(summary, null, 2),
    'performance-failover-results.json': JSON.stringify(summary, null, 2),
  };
}
