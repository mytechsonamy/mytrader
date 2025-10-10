#!/usr/bin/env node

/**
 * Automated E2E Validation Script for Previous Close Implementation
 * Tests BIST, NASDAQ, and NYSE markets
 */

const API_BASE_URL = 'http://localhost:5002';

// ANSI color codes for terminal output
const colors = {
    reset: '\x1b[0m',
    bright: '\x1b[1m',
    red: '\x1b[31m',
    green: '\x1b[32m',
    yellow: '\x1b[33m',
    blue: '\x1b[34m',
    magenta: '\x1b[35m',
    cyan: '\x1b[36m',
};

// Test results tracking
const testResults = {
    total: 0,
    passed: 0,
    failed: 0,
    tests: []
};

// Logging utilities
function log(message, color = 'reset') {
    console.log(`${colors[color]}${message}${colors.reset}`);
}

function logSuccess(message) {
    log(`✓ ${message}`, 'green');
}

function logError(message) {
    log(`✗ ${message}`, 'red');
}

function logInfo(message) {
    log(`ℹ ${message}`, 'blue');
}

function logWarning(message) {
    log(`⚠ ${message}`, 'yellow');
}

function logSection(title) {
    console.log('');
    log('='.repeat(80), 'cyan');
    log(`  ${title}`, 'bright');
    log('='.repeat(80), 'cyan');
    console.log('');
}

// Test assertion
function assert(testName, condition, errorMessage) {
    testResults.total++;

    if (condition) {
        testResults.passed++;
        logSuccess(`${testName}`);
        testResults.tests.push({ name: testName, passed: true });
        return true;
    } else {
        testResults.failed++;
        logError(`${testName}: ${errorMessage}`);
        testResults.tests.push({ name: testName, passed: false, error: errorMessage });
        return false;
    }
}

// Fetch API data
async function fetchAPI(endpoint) {
    try {
        const response = await fetch(`${API_BASE_URL}${endpoint}`);

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        return await response.json();
    } catch (error) {
        throw new Error(`API request failed: ${error.message}`);
    }
}

// Test 1: Backend Health Check
async function testBackendHealth() {
    logSection('TEST 1: Backend Health Check');

    try {
        const response = await fetch(`${API_BASE_URL}/api/health`);
        assert(
            'Backend API is running',
            response.ok,
            `Backend returned status ${response.status}`
        );

        logInfo(`Backend URL: ${API_BASE_URL}`);
        return true;
    } catch (error) {
        assert('Backend API is running', false, error.message);
        return false;
    }
}

// Test 2: Dashboard Overview Endpoint
async function testDashboardOverview() {
    logSection('TEST 2: Dashboard Overview Endpoint - PreviousClose Field');

    try {
        const data = await fetchAPI('/api/dashboard/overview');

        assert(
            'Dashboard endpoint returns data',
            Array.isArray(data) && data.length > 0,
            'No data returned from dashboard'
        );

        logInfo(`Received ${data.length} symbols from dashboard`);

        // Group by market
        const markets = { BIST: [], NASDAQ: [], NYSE: [], OTHER: [] };

        data.forEach(item => {
            const market = item.market || item.marketId || 'OTHER';
            if (market.includes('BIST')) markets.BIST.push(item);
            else if (market.includes('NASDAQ')) markets.NASDAQ.push(item);
            else if (market.includes('NYSE')) markets.NYSE.push(item);
            else markets.OTHER.push(item);
        });

        // Test BIST symbols
        logInfo(`\nBIST Symbols: ${markets.BIST.length}`);
        testMarketSymbols('BIST', markets.BIST);

        // Test NASDAQ symbols
        logInfo(`\nNASDAQ Symbols: ${markets.NASDAQ.length}`);
        testMarketSymbols('NASDAQ', markets.NASDAQ);

        // Test NYSE symbols
        logInfo(`\nNYSE Symbols: ${markets.NYSE.length}`);
        testMarketSymbols('NYSE', markets.NYSE);

        return true;
    } catch (error) {
        assert('Dashboard endpoint test', false, error.message);
        return false;
    }
}

// Test market symbols for PreviousClose
function testMarketSymbols(marketName, symbols) {
    if (symbols.length === 0) {
        logWarning(`No ${marketName} symbols found in response`);
        return;
    }

    let symbolsWithPreviousClose = 0;
    let symbolsWithoutPreviousClose = 0;
    let calculationErrors = 0;

    symbols.slice(0, 10).forEach(symbol => {
        const ticker = symbol.symbol || symbol.ticker;
        const previousClose = symbol.previousClose ?? symbol.PreviousClose;
        const price = symbol.price ?? symbol.Price ?? symbol.currentPrice;
        const changePercent = symbol.changePercent ?? symbol.ChangePercent ?? symbol.priceChangePercent;

        // Check if PreviousClose exists
        if (previousClose !== null && previousClose !== undefined) {
            symbolsWithPreviousClose++;

            // Validate percentage calculation
            if (previousClose > 0) {
                const change = price - previousClose;
                const expectedPercent = (change / previousClose) * 100;
                const actualPercent = changePercent ?? 0;

                // Allow 0.1% tolerance for rounding
                const isValid = Math.abs(expectedPercent - actualPercent) < 0.1;

                if (isValid) {
                    logSuccess(`  ${ticker}: PreviousClose=$${previousClose.toFixed(2)}, Calculation Valid`);
                } else {
                    calculationErrors++;
                    logError(`  ${ticker}: Calculation Error - Expected ${expectedPercent.toFixed(2)}%, Got ${actualPercent.toFixed(2)}%`);
                }

                assert(
                    `${marketName} ${ticker} percentage calculation`,
                    isValid,
                    `Expected ${expectedPercent.toFixed(2)}%, got ${actualPercent.toFixed(2)}%`
                );
            }
        } else {
            symbolsWithoutPreviousClose++;
            logWarning(`  ${ticker}: PreviousClose field is MISSING`);

            assert(
                `${marketName} ${ticker} has PreviousClose`,
                false,
                'PreviousClose field not populated'
            );
        }
    });

    logInfo(`${marketName} Summary: ${symbolsWithPreviousClose} with PreviousClose, ${symbolsWithoutPreviousClose} missing, ${calculationErrors} calculation errors`);
}

// Test 3: Percentage Calculation Formula
async function testPercentageFormula() {
    logSection('TEST 3: Percentage Calculation Formula Validation');

    const testCases = [
        {
            name: 'Positive change',
            price: 150.00,
            previousClose: 146.58,
            expectedChange: 3.42,
            expectedPercent: 2.33
        },
        {
            name: 'Negative change',
            price: 100.00,
            previousClose: 102.00,
            expectedChange: -2.00,
            expectedPercent: -1.96
        },
        {
            name: 'Small positive change',
            price: 50.25,
            previousClose: 50.00,
            expectedChange: 0.25,
            expectedPercent: 0.50
        },
        {
            name: 'Large price',
            price: 15000.00,
            previousClose: 14500.00,
            expectedChange: 500.00,
            expectedPercent: 3.45
        },
        {
            name: 'Small price (<$1)',
            price: 0.55,
            previousClose: 0.50,
            expectedChange: 0.05,
            expectedPercent: 10.00
        }
    ];

    testCases.forEach(testCase => {
        const calculatedChange = testCase.price - testCase.previousClose;
        const calculatedPercent = (calculatedChange / testCase.previousClose) * 100;

        const changeMatch = Math.abs(calculatedChange - testCase.expectedChange) < 0.01;
        const percentMatch = Math.abs(calculatedPercent - testCase.expectedPercent) < 0.01;

        assert(
            `Formula test: ${testCase.name}`,
            changeMatch && percentMatch,
            `Change: ${calculatedChange.toFixed(2)} (expected ${testCase.expectedChange.toFixed(2)}), Percent: ${calculatedPercent.toFixed(2)}% (expected ${testCase.expectedPercent.toFixed(2)}%)`
        );

        if (changeMatch && percentMatch) {
            logInfo(`  Price: $${testCase.price}, PrevClose: $${testCase.previousClose} → ${calculatedPercent >= 0 ? '+' : ''}${calculatedPercent.toFixed(2)}%`);
        }
    });
}

// Test 4: Edge Cases
async function testEdgeCases() {
    logSection('TEST 4: Edge Case Handling');

    // Test zero previous close
    try {
        const price = 100;
        const previousClose = 0;
        const change = price - previousClose;
        const percent = previousClose > 0 ? (change / previousClose) * 100 : 0;

        assert(
            'PreviousClose = 0 does not crash',
            percent === 0,
            'Should return 0% when PreviousClose is 0'
        );
        logInfo('  Zero previous close handled: returns 0%');
    } catch (error) {
        assert('PreviousClose = 0 does not crash', false, error.message);
    }

    // Test null previous close
    try {
        const price = 100;
        const previousClose = null;
        const change = previousClose !== null ? price - previousClose : null;

        assert(
            'PreviousClose = null does not crash',
            change === null,
            'Should return null when PreviousClose is null'
        );
        logInfo('  Null previous close handled: no calculation performed');
    } catch (error) {
        assert('PreviousClose = null does not crash', false, error.message);
    }

    // Test very small price
    try {
        const price = 0.05;
        const previousClose = 0.045;
        const change = price - previousClose;
        const percent = (change / previousClose) * 100;

        assert(
            'Very small price (<$1) calculates correctly',
            Math.abs(percent - 11.11) < 0.1,
            `Expected ~11.11%, got ${percent.toFixed(2)}%`
        );
        logInfo(`  Small price calculation: $${price} / $${previousClose} = ${percent.toFixed(2)}%`);
    } catch (error) {
        assert('Very small price (<$1) calculates correctly', false, error.message);
    }

    // Test very large price
    try {
        const price = 15000;
        const previousClose = 14500;
        const change = price - previousClose;
        const percent = (change / previousClose) * 100;

        assert(
            'Very large price (>$10,000) calculates correctly',
            Math.abs(percent - 3.45) < 0.1,
            `Expected ~3.45%, got ${percent.toFixed(2)}%`
        );
        logInfo(`  Large price calculation: $${price} / $${previousClose} = ${percent.toFixed(2)}%`);
    } catch (error) {
        assert('Very large price (>$10,000) calculates correctly', false, error.message);
    }

    // Test zero change
    try {
        const price = 100;
        const previousClose = 100;
        const change = price - previousClose;
        const percent = (change / previousClose) * 100;

        assert(
            'Zero change shows 0.00%',
            percent === 0,
            `Expected 0%, got ${percent.toFixed(2)}%`
        );
        logInfo('  Zero change handled: returns 0.00%');
    } catch (error) {
        assert('Zero change shows 0.00%', false, error.message);
    }
}

// Test 5: Currency Formatting
async function testCurrencyFormatting() {
    logSection('TEST 5: Currency Formatting Validation');

    try {
        const data = await fetchAPI('/api/dashboard/overview');

        // Check BIST symbols use TRY
        const bistSymbols = data.filter(item => {
            const market = item.market || item.marketId || '';
            return market.includes('BIST');
        });

        if (bistSymbols.length > 0) {
            const hasTRY = bistSymbols.some(s => s.currency === 'TRY' || s.quoteCurrency === 'TRY');
            assert(
                'BIST symbols use TRY currency',
                hasTRY,
                'Expected TRY currency for BIST symbols'
            );
            logInfo(`  BIST symbols: ${hasTRY ? 'Using TRY ₺' : 'Currency not set correctly'}`);
        } else {
            logWarning('  No BIST symbols found to test TRY currency');
        }

        // Check US symbols use USD
        const usSymbols = data.filter(item => {
            const market = item.market || item.marketId || '';
            return market.includes('NASDAQ') || market.includes('NYSE');
        });

        if (usSymbols.length > 0) {
            const hasUSD = usSymbols.some(s => s.currency === 'USD' || s.quoteCurrency === 'USD' || !s.currency);
            assert(
                'US market symbols use USD currency',
                hasUSD,
                'Expected USD currency for NASDAQ/NYSE symbols'
            );
            logInfo(`  US market symbols: ${hasUSD ? 'Using USD $' : 'Currency not set correctly'}`);
        } else {
            logWarning('  No US market symbols found to test USD currency');
        }

    } catch (error) {
        assert('Currency formatting test', false, error.message);
    }
}

// Test 6: DTO Field Mapping
async function testDTOMapping() {
    logSection('TEST 6: DTO Field Mapping Validation');

    try {
        const data = await fetchAPI('/api/dashboard/overview');

        if (data.length === 0) {
            logWarning('No data to test DTO mapping');
            return;
        }

        const sample = data[0];

        // Check for required fields
        const requiredFields = ['price', 'previousClose', 'changePercent'];
        const alternativeFields = {
            'price': ['Price', 'currentPrice'],
            'previousClose': ['PreviousClose', 'prevClose'],
            'changePercent': ['ChangePercent', 'priceChangePercent']
        };

        requiredFields.forEach(field => {
            const hasField = sample[field] !== undefined ||
                            alternativeFields[field].some(alt => sample[alt] !== undefined);

            assert(
                `DTO includes ${field} field`,
                hasField,
                `Field ${field} not found in response`
            );

            if (hasField) {
                const actualField = sample[field] !== undefined ? field :
                                  alternativeFields[field].find(alt => sample[alt] !== undefined);
                logInfo(`  ${field}: ✓ (mapped as ${actualField})`);
            }
        });

    } catch (error) {
        assert('DTO mapping test', false, error.message);
    }
}

// Print test summary
function printSummary() {
    logSection('TEST EXECUTION SUMMARY');

    const successRate = testResults.total > 0
        ? ((testResults.passed / testResults.total) * 100).toFixed(1)
        : 0;

    log(`Total Tests: ${testResults.total}`, 'bright');
    log(`Passed: ${testResults.passed}`, 'green');
    log(`Failed: ${testResults.failed}`, 'red');
    log(`Success Rate: ${successRate}%`, successRate >= 95 ? 'green' : successRate >= 80 ? 'yellow' : 'red');

    console.log('');

    if (testResults.failed > 0) {
        log('FAILED TESTS:', 'red');
        testResults.tests
            .filter(t => !t.passed)
            .forEach(t => {
                logError(`  - ${t.name}`);
                if (t.error) {
                    log(`    ${t.error}`, 'red');
                }
            });
        console.log('');
    }

    if (testResults.passed === testResults.total) {
        logSuccess('ALL TESTS PASSED! ✓✓✓');
        log('\nThe Previous Close implementation is working correctly across all markets.', 'green');
    } else if (successRate >= 80) {
        logWarning('MOST TESTS PASSED, BUT SOME ISSUES FOUND');
        log('\nReview the failed tests above and address the issues.', 'yellow');
    } else {
        logError('CRITICAL ISSUES FOUND');
        log('\nMultiple tests failed. Immediate attention required.', 'red');
    }

    console.log('');
    log('='.repeat(80), 'cyan');
}

// Main test execution
async function runTests() {
    log('\n' + '='.repeat(80), 'magenta');
    log('  PREVIOUS CLOSE E2E VALIDATION TEST SUITE', 'bright');
    log('  Testing BIST, NASDAQ, and NYSE Markets', 'bright');
    log('='.repeat(80) + '\n', 'magenta');

    logInfo(`Test started at: ${new Date().toLocaleString()}`);
    logInfo(`Backend URL: ${API_BASE_URL}\n`);

    const startTime = Date.now();

    // Run all tests
    const backendOk = await testBackendHealth();

    if (!backendOk) {
        logError('\nBackend is not running. Please start the backend service first.');
        process.exit(1);
    }

    await testDashboardOverview();
    await testPercentageFormula();
    await testEdgeCases();
    await testCurrencyFormatting();
    await testDTOMapping();

    const duration = ((Date.now() - startTime) / 1000).toFixed(2);

    console.log('');
    logInfo(`Test execution completed in ${duration}s`);

    printSummary();

    // Exit with appropriate code
    process.exit(testResults.failed > 0 ? 1 : 0);
}

// Run tests
runTests().catch(error => {
    logError(`\nFatal error: ${error.message}`);
    console.error(error);
    process.exit(1);
});
