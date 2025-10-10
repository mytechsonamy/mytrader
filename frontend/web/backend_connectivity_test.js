#!/usr/bin/env node

/**
 * MyTrader React Router Backend Connectivity Test
 *
 * This script tests the backend API connectivity and authentication flow
 * to ensure proper integration with the React Router frontend.
 */

const https = require('https');
const http = require('http');
const { URL } = require('url');

// Configuration
const CONFIG = {
    FRONTEND_URL: 'http://localhost:3001',
    BACKEND_URL: 'http://localhost:5002',
    TEST_USER: {
        email: 'test@techsonamy.com',
        password: 'TestUser123!'
    },
    TIMEOUT: 30000 // 30 seconds
};

// Colors for console output
const colors = {
    reset: '\x1b[0m',
    bright: '\x1b[1m',
    red: '\x1b[31m',
    green: '\x1b[32m',
    yellow: '\x1b[33m',
    blue: '\x1b[34m',
    magenta: '\x1b[35m',
    cyan: '\x1b[36m'
};

class TestRunner {
    constructor() {
        this.results = {
            total: 0,
            passed: 0,
            failed: 0,
            startTime: Date.now()
        };
    }

    log(message, type = 'info') {
        const timestamp = new Date().toISOString().substr(11, 8);
        const colorMap = {
            'info': colors.blue,
            'success': colors.green,
            'error': colors.red,
            'warning': colors.yellow,
            'header': colors.magenta
        };

        const color = colorMap[type] || colors.reset;
        console.log(`${color}[${timestamp}] ${message}${colors.reset}`);
    }

    async makeRequest(url, options = {}) {
        return new Promise((resolve, reject) => {
            const urlObj = new URL(url);
            const protocol = urlObj.protocol === 'https:' ? https : http;

            const requestOptions = {
                hostname: urlObj.hostname,
                port: urlObj.port,
                path: urlObj.pathname + urlObj.search,
                method: options.method || 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'User-Agent': 'MyTrader-Test-Suite/1.0',
                    ...options.headers
                },
                timeout: CONFIG.TIMEOUT
            };

            const req = protocol.request(requestOptions, (res) => {
                let data = '';
                res.on('data', chunk => data += chunk);
                res.on('end', () => {
                    try {
                        const response = {
                            status: res.statusCode,
                            headers: res.headers,
                            data: data ? JSON.parse(data) : null
                        };
                        resolve(response);
                    } catch (e) {
                        resolve({
                            status: res.statusCode,
                            headers: res.headers,
                            data: data
                        });
                    }
                });
            });

            req.on('error', reject);
            req.on('timeout', () => {
                req.destroy();
                reject(new Error('Request timeout'));
            });

            if (options.body) {
                req.write(JSON.stringify(options.body));
            }

            req.end();
        });
    }

    async testEndpoint(name, url, expectedStatus = 200, options = {}) {
        this.results.total++;

        try {
            this.log(`Testing ${name}: ${url}`, 'info');
            const response = await this.makeRequest(url, options);

            if (response.status === expectedStatus) {
                this.results.passed++;
                this.log(`✅ ${name} - Status: ${response.status}`, 'success');
                return { success: true, response };
            } else {
                this.results.failed++;
                this.log(`❌ ${name} - Expected: ${expectedStatus}, Got: ${response.status}`, 'error');
                return { success: false, response };
            }
        } catch (error) {
            this.results.failed++;
            this.log(`❌ ${name} - Error: ${error.message}`, 'error');
            return { success: false, error };
        }
    }

    async testHealthEndpoints() {
        this.log('🏥 Testing Health Endpoints', 'header');

        const healthTests = [
            {
                name: 'Backend Health Check',
                url: `${CONFIG.BACKEND_URL}/api/health`,
                expected: 200
            },
            {
                name: 'Backend API Info',
                url: `${CONFIG.BACKEND_URL}/api/health/info`,
                expected: 200
            }
        ];

        for (const test of healthTests) {
            await this.testEndpoint(test.name, test.url, test.expected);
        }
    }

    async testAuthenticationEndpoints() {
        this.log('🔐 Testing Authentication Endpoints', 'header');

        // Test login endpoint
        const loginResult = await this.testEndpoint(
            'User Login',
            `${CONFIG.BACKEND_URL}/api/auth/login`,
            200,
            {
                method: 'POST',
                body: CONFIG.TEST_USER
            }
        );

        let authToken = null;
        if (loginResult.success && loginResult.response.data) {
            // Extract token from various possible response formats
            const data = loginResult.response.data.data || loginResult.response.data;
            authToken = data.token || data.accessToken || data.AccessToken;

            if (authToken) {
                this.log('✅ Authentication token obtained', 'success');
            } else {
                this.log('⚠️ No authentication token in response', 'warning');
            }
        }

        // Test token validation if we have a token
        if (authToken) {
            await this.testEndpoint(
                'Token Validation',
                `${CONFIG.BACKEND_URL}/api/auth/me`,
                200,
                {
                    headers: {
                        'Authorization': `Bearer ${authToken}`
                    }
                }
            );
        }

        // Test logout
        await this.testEndpoint(
            'User Logout',
            `${CONFIG.BACKEND_URL}/api/auth/logout`,
            200,
            {
                method: 'POST',
                headers: authToken ? { 'Authorization': `Bearer ${authToken}` } : {}
            }
        );

        return authToken;
    }

    async testMarketDataEndpoints(authToken = null) {
        this.log('📊 Testing Market Data Endpoints', 'header');

        const headers = authToken ? { 'Authorization': `Bearer ${authToken}` } : {};

        const marketDataTests = [
            {
                name: 'Market Overview',
                url: `${CONFIG.BACKEND_URL}/api/market-data/overview`,
                expected: 200
            },
            {
                name: 'Top Movers',
                url: `${CONFIG.BACKEND_URL}/api/market-data/top-movers`,
                expected: 200
            },
            {
                name: 'Asset Classes',
                url: `${CONFIG.BACKEND_URL}/api/asset-classes`,
                expected: 200
            },
            {
                name: 'Symbols List',
                url: `${CONFIG.BACKEND_URL}/api/symbols`,
                expected: 200
            }
        ];

        for (const test of marketDataTests) {
            await this.testEndpoint(test.name, test.url, test.expected, { headers });
        }
    }

    async testWebSocketConnectivity() {
        this.log('🔌 Testing WebSocket Connectivity', 'header');

        // Test SignalR hub endpoints
        const wsTests = [
            {
                name: 'Market Data Hub Info',
                url: `${CONFIG.BACKEND_URL}/marketDataHub`,
                expected: 200
            },
            {
                name: 'Dashboard Hub Info',
                url: `${CONFIG.BACKEND_URL}/dashboardHub`,
                expected: 200
            }
        ];

        for (const test of wsTests) {
            await this.testEndpoint(test.name, test.url, test.expected);
        }
    }

    async testCORSConfiguration() {
        this.log('🌐 Testing CORS Configuration', 'header');

        const corsTest = await this.testEndpoint(
            'CORS Preflight',
            `${CONFIG.BACKEND_URL}/api/health`,
            200,
            {
                method: 'OPTIONS',
                headers: {
                    'Origin': CONFIG.FRONTEND_URL,
                    'Access-Control-Request-Method': 'GET',
                    'Access-Control-Request-Headers': 'Content-Type,Authorization'
                }
            }
        );

        if (corsTest.success) {
            const headers = corsTest.response.headers;
            const allowOrigin = headers['access-control-allow-origin'];
            const allowMethods = headers['access-control-allow-methods'];

            if (allowOrigin === '*' || allowOrigin === CONFIG.FRONTEND_URL) {
                this.log('✅ CORS Origin configuration correct', 'success');
            } else {
                this.log(`⚠️ CORS Origin: ${allowOrigin}`, 'warning');
            }

            if (allowMethods && allowMethods.includes('GET')) {
                this.log('✅ CORS Methods configuration correct', 'success');
            } else {
                this.log(`⚠️ CORS Methods: ${allowMethods}`, 'warning');
            }
        }
    }

    async testFrontendConnectivity() {
        this.log('🌍 Testing Frontend Connectivity', 'header');

        const frontendTests = [
            {
                name: 'Frontend Home Page',
                url: CONFIG.FRONTEND_URL,
                expected: 200
            },
            {
                name: 'Frontend Static Assets',
                url: `${CONFIG.FRONTEND_URL}/static/`,
                expected: [200, 403, 404] // Any of these is acceptable
            }
        ];

        for (const test of frontendTests) {
            const expectedStatuses = Array.isArray(test.expected) ? test.expected : [test.expected];
            const result = await this.testEndpoint(test.name, test.url);

            if (result.success || expectedStatuses.includes(result.response?.status)) {
                if (!result.success) {
                    this.results.failed--;
                    this.results.passed++;
                    this.log(`✅ ${test.name} - Status: ${result.response.status} (acceptable)`, 'success');
                }
            }
        }
    }

    async testDatabaseConnectivity(authToken = null) {
        this.log('🗃️ Testing Database Connectivity', 'header');

        const headers = authToken ? { 'Authorization': `Bearer ${authToken}` } : {};

        const dbTests = [
            {
                name: 'Database Health',
                url: `${CONFIG.BACKEND_URL}/api/health/database`,
                expected: 200
            }
        ];

        for (const test of dbTests) {
            await this.testEndpoint(test.name, test.url, test.expected, { headers });
        }
    }

    displaySummary() {
        const duration = Math.round((Date.now() - this.results.startTime) / 1000);
        const successRate = ((this.results.passed / this.results.total) * 100).toFixed(1);

        this.log('', 'info');
        this.log('═'.repeat(60), 'header');
        this.log('📊 TEST SUMMARY', 'header');
        this.log('═'.repeat(60), 'header');
        this.log(`Total Tests: ${this.results.total}`, 'info');
        this.log(`Passed: ${this.results.passed}`, 'success');
        this.log(`Failed: ${this.results.failed}`, 'error');
        this.log(`Success Rate: ${successRate}%`, successRate === '100.0' ? 'success' : 'warning');
        this.log(`Duration: ${duration}s`, 'info');
        this.log('═'.repeat(60), 'header');

        if (this.results.failed === 0) {
            this.log('🎉 All tests passed! React Router integration is working correctly.', 'success');
        } else {
            this.log('⚠️ Some tests failed. Please review the issues above.', 'warning');
        }
    }

    async runAllTests() {
        this.log('🚀 Starting MyTrader React Router Backend Connectivity Tests', 'header');
        this.log(`Frontend: ${CONFIG.FRONTEND_URL}`, 'info');
        this.log(`Backend: ${CONFIG.BACKEND_URL}`, 'info');
        this.log('', 'info');

        try {
            // Test basic connectivity
            await this.testFrontendConnectivity();
            await this.testHealthEndpoints();
            await this.testCORSConfiguration();

            // Test authentication flow
            const authToken = await this.testAuthenticationEndpoints();

            // Test data endpoints
            await this.testMarketDataEndpoints(authToken);
            await this.testWebSocketConnectivity();
            await this.testDatabaseConnectivity(authToken);

        } catch (error) {
            this.log(`❌ Test suite error: ${error.message}`, 'error');
        }

        this.displaySummary();

        // Exit with appropriate code
        process.exit(this.results.failed === 0 ? 0 : 1);
    }
}

// Run tests if called directly
if (require.main === module) {
    const runner = new TestRunner();
    runner.runAllTests().catch(error => {
        console.error(`Fatal error: ${error.message}`);
        process.exit(1);
    });
}

module.exports = TestRunner;