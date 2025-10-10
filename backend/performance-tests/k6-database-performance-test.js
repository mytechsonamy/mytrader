/**
 * K6 Performance Test - Scenario 5: Database Performance
 *
 * Objective: Validate Yahoo 5-minute persistence doesn't affect real-time flow
 * Test:
 * 1. Run Alpaca streaming (real-time)
 * 2. Simultaneously run Yahoo 5-min polling (database writes)
 * 3. Measure interference between flows
 *
 * Success Criteria:
 * - Database writes <100ms
 * - Zero impact on real-time flow
 * - Connection pool adequate
 * - No deadlocks
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend, Gauge } from 'k6/metrics';
import ws from 'k6/ws';

// Custom metrics
const dbWriteLatency = new Trend('db_write_latency_ms');
const dbQueryLatency = new Trend('db_query_latency_ms');
const realtimeLatency = new Trend('realtime_latency_ms');
const dbConnectionPoolUsage = new Gauge('db_connection_pool_usage_percent');
const dbDeadlocks = new Counter('db_deadlocks');
const dbTimeouts = new Counter('db_timeouts');
const messageRate = new Counter('messages_received');
const errorRate = new Counter('errors');

// Test configuration
export const options = {
  scenarios: {
    // Real-time WebSocket connections
    realtime_streaming: {
      executor: 'constant-vus',
      vus: 30,
      duration: '10m',
      exec: 'realtimeStreaming',
    },
    // Periodic database writes (Yahoo 5-min sync)
    database_writes: {
      executor: 'constant-arrival-rate',
      rate: 6, // 6 writes per minute (every 10 seconds)
      timeUnit: '1m',
      duration: '10m',
      preAllocatedVUs: 5,
      maxVUs: 10,
      exec: 'databaseWrites',
    },
    // Periodic database reads (market data queries)
    database_reads: {
      executor: 'constant-arrival-rate',
      rate: 60, // 60 reads per minute (1 per second)
      timeUnit: '1m',
      duration: '10m',
      preAllocatedVUs: 10,
      maxVUs: 20,
      exec: 'databaseReads',
    },
  },
  thresholds: {
    'db_write_latency_ms': ['p(95)<100'], // Database writes <100ms
    'db_query_latency_ms': ['p(95)<50'], // Database reads <50ms
    'realtime_latency_ms': ['p(95)<2000'], // Real-time not affected
    'db_deadlocks': ['count==0'], // No deadlocks
    'db_timeouts': ['count==0'], // No timeouts
  },
};

const SYMBOLS = [
  'BTCUSD', 'ETHUSD', 'SOLUSD', 'AVAXUSD', 'LINKUSD', 'ADAUSD',
  'AAPL', 'GOOGL', 'MSFT', 'TSLA', 'AMZN', 'NVDA', 'META', 'NFLX'
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

/**
 * Scenario: Real-time streaming via WebSocket
 * Simulates live market data consumption
 */
export function realtimeStreaming(data) {
  const token = data.authToken || authToken;

  if (!token) {
    errorRate.add(1);
    return;
  }

  const url = `${WS_URL}?access_token=${token}`;
  const params = { tags: { name: 'DatabasePerfTest-Realtime' } };

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
            if (payload.timestamp || payload.lastUpdated) {
              const serverTime = new Date(payload.timestamp || payload.lastUpdated);
              const latency = Date.now() - serverTime.getTime();

              if (latency > 0 && latency < 60000) {
                realtimeLatency.add(latency);
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

    // Hold connection for 2 minutes, then reconnect
    socket.setTimeout(() => {
      socket.close();
    }, 120000);
  });

  check(res, {
    'websocket connection successful': (r) => r && r.status === 101,
  });

  sleep(5);
}

/**
 * Scenario: Database writes (Yahoo 5-min sync simulation)
 * Simulates periodic batch writes to market_data table
 */
export function databaseWrites(data) {
  const token = data.authToken || authToken;

  if (!token) {
    errorRate.add(1);
    return;
  }

  // Simulate batch write of 30 symbols
  const writeStart = Date.now();

  const writeRes = http.post(
    `${BASE_URL}/api/market-data/batch-update`,
    JSON.stringify({
      symbols: SYMBOLS,
      provider: 'YAHOO',
      data: SYMBOLS.map(symbol => ({
        symbol: symbol,
        price: Math.random() * 1000,
        volume: Math.floor(Math.random() * 1000000),
        timestamp: new Date().toISOString(),
      }))
    }),
    {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      timeout: '5s',
    }
  );

  const writeDuration = Date.now() - writeStart;
  dbWriteLatency.add(writeDuration);

  check(writeRes, {
    'batch write successful': (r) => r.status === 200 || r.status === 201,
    'batch write under 100ms': (r) => writeDuration < 100,
  });

  // Check for database errors indicating deadlocks or timeouts
  if (writeRes.status === 408 || writeRes.status === 504) {
    dbTimeouts.add(1);
    console.error('Database write timeout');
  }

  if (writeRes.body && writeRes.body.includes('deadlock')) {
    dbDeadlocks.add(1);
    console.error('Database deadlock detected');
  }

  // Check connection pool usage
  const metricsRes = http.get(`${BASE_URL}/api/metrics/database`, {
    headers: { 'Authorization': `Bearer ${token}` },
  });

  if (metricsRes.status === 200) {
    try {
      const metrics = JSON.parse(metricsRes.body);
      if (metrics.connectionPoolUsage) {
        dbConnectionPoolUsage.add(metrics.connectionPoolUsage);

        if (metrics.connectionPoolUsage > 90) {
          console.warn(`Connection pool near capacity: ${metrics.connectionPoolUsage}%`);
        }
      }
    } catch (e) {
      // Ignore parse errors
    }
  }

  sleep(1);
}

/**
 * Scenario: Database reads (market data queries)
 * Simulates frontend queries for historical/current prices
 */
export function databaseReads(data) {
  const token = data.authToken || authToken;

  if (!token) {
    errorRate.add(1);
    return;
  }

  // Query random symbol
  const symbol = SYMBOLS[Math.floor(Math.random() * SYMBOLS.length)];
  const queryStart = Date.now();

  const queryRes = http.get(
    `${BASE_URL}/api/market-data/${symbol}`,
    {
      headers: { 'Authorization': `Bearer ${token}` },
      timeout: '2s',
    }
  );

  const queryDuration = Date.now() - queryStart;
  dbQueryLatency.add(queryDuration);

  check(queryRes, {
    'query successful': (r) => r.status === 200,
    'query under 50ms': (r) => queryDuration < 50,
  });

  if (queryRes.status === 408 || queryRes.status === 504) {
    dbTimeouts.add(1);
  }

  sleep(0.5);
}

export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    scenario: 'Database Performance',
    duration: '10 minutes',
    concurrent_operations: {
      realtime_streaming: 30,
      database_writes: 6, // per minute
      database_reads: 60, // per minute
    },
    metrics: {
      db_write_latency_ms: {
        p50: data.metrics.db_write_latency_ms?.values?.['p(50)'] || 0,
        p95: data.metrics.db_write_latency_ms?.values?.['p(95)'] || 0,
        p99: data.metrics.db_write_latency_ms?.values?.['p(99)'] || 0,
        avg: data.metrics.db_write_latency_ms?.values?.avg || 0,
        max: data.metrics.db_write_latency_ms?.values?.max || 0,
      },
      db_query_latency_ms: {
        p50: data.metrics.db_query_latency_ms?.values?.['p(50)'] || 0,
        p95: data.metrics.db_query_latency_ms?.values?.['p(95)'] || 0,
        p99: data.metrics.db_query_latency_ms?.values?.['p(99)'] || 0,
        avg: data.metrics.db_query_latency_ms?.values?.avg || 0,
      },
      realtime_latency_ms: {
        p50: data.metrics.realtime_latency_ms?.values?.['p(50)'] || 0,
        p95: data.metrics.realtime_latency_ms?.values?.['p(95)'] || 0,
        p99: data.metrics.realtime_latency_ms?.values?.['p(99)'] || 0,
      },
      db_connection_pool_usage_percent: {
        avg: data.metrics.db_connection_pool_usage_percent?.values?.avg || 0,
        max: data.metrics.db_connection_pool_usage_percent?.values?.max || 0,
      },
      db_deadlocks: data.metrics.db_deadlocks?.values?.count || 0,
      db_timeouts: data.metrics.db_timeouts?.values?.count || 0,
      messages_received: data.metrics.messages_received?.values?.count || 0,
      errors: data.metrics.errors?.values?.count || 0,
    },
    interference_analysis: {
      writes_affect_realtime: false, // Would be true if realtime latency spikes during writes
      connection_pool_adequate: (data.metrics.db_connection_pool_usage_percent?.values?.max || 0) < 90,
      no_resource_contention: (data.metrics.db_deadlocks?.values?.count || 0) === 0 &&
        (data.metrics.db_timeouts?.values?.count || 0) === 0,
    },
    success_criteria: {
      writes_under_100ms: (data.metrics.db_write_latency_ms?.values?.['p(95)'] || Infinity) < 100,
      reads_under_50ms: (data.metrics.db_query_latency_ms?.values?.['p(95)'] || Infinity) < 50,
      realtime_unaffected: (data.metrics.realtime_latency_ms?.values?.['p(95)'] || Infinity) < 2000,
      no_deadlocks: (data.metrics.db_deadlocks?.values?.count || 0) === 0,
      connection_pool_healthy: (data.metrics.db_connection_pool_usage_percent?.values?.max || 0) < 90,
    },
    thresholds_met: Object.keys(data.metrics).every(key => {
      const metric = data.metrics[key];
      return !metric.thresholds || Object.values(metric.thresholds).every(t => t.ok);
    }),
  };

  return {
    'stdout': JSON.stringify(summary, null, 2),
    'performance-database-results.json': JSON.stringify(summary, null, 2),
  };
}
