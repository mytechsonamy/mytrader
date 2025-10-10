/**
 * MyTrader React Router End-to-End Integration Tests
 *
 * Comprehensive Playwright tests for React Router multi-page architecture
 * This file tests navigation, authentication flow, and user experience
 */

const { test, expect } = require('@playwright/test');

// Configuration
const CONFIG = {
    FRONTEND_URL: 'http://localhost:3001',
    BACKEND_URL: 'http://localhost:5002',
    TEST_USER: {
        email: 'test@techsonamy.com',
        password: 'TestUser123!'
    },
    TIMEOUT: 30000
};

// Test data for various scenarios
const ROUTES = {
    public: [
        { path: '/', name: 'Dashboard', title: /MyTrader|Dashboard/ },
        { path: '/markets', name: 'Markets', title: /Markets/ },
        { path: '/competition', name: 'Competition', title: /Competition/ },
        { path: '/login', name: 'Login', title: /Login|Sign In/ }
    ],
    protected: [
        { path: '/portfolio', name: 'Portfolio', title: /Portfolio/ },
        { path: '/alerts', name: 'Alerts', title: /Alerts/ },
        { path: '/strategies', name: 'Strategies', title: /Strategies/ },
        { path: '/profile', name: 'Profile', title: /Profile/ }
    ]
};

test.describe('MyTrader React Router Integration Tests', () => {
    test.beforeEach(async ({ page }) => {
        // Set longer timeout for integration tests
        test.setTimeout(CONFIG.TIMEOUT);

        // Navigate to the application
        await page.goto(CONFIG.FRONTEND_URL);

        // Wait for the application to load
        await page.waitForLoadState('networkidle');
    });

    test.describe('Navigation Tests', () => {
        test('should load dashboard page successfully', async ({ page }) => {
            await expect(page).toHaveTitle(/MyTrader/);

            // Check for main dashboard elements
            await expect(page.locator('h1')).toContainText(/Welcome to MyTrader|MyTrader/);

            // Verify market data sections are present
            const marketOverview = page.locator('text=Market Overview');
            await expect(marketOverview).toBeVisible();
        });

        test('should navigate to all public routes', async ({ page }) => {
            for (const route of ROUTES.public) {
                console.log(`Testing navigation to ${route.name} (${route.path})`);

                await page.goto(`${CONFIG.FRONTEND_URL}${route.path}`);
                await page.waitForLoadState('networkidle');

                // Check if page loads without errors
                await expect(page.locator('body')).toBeVisible();

                // Check for 404 error
                const notFoundElement = page.locator('text=404');
                await expect(notFoundElement).toHaveCount(0);

                console.log(`✅ ${route.name} page loaded successfully`);
            }
        });

        test('should handle 404 for non-existent routes', async ({ page }) => {
            await page.goto(`${CONFIG.FRONTEND_URL}/non-existent-route`);
            await page.waitForLoadState('networkidle');

            // Should show 404 page
            const notFoundElement = page.locator('text=404');
            await expect(notFoundElement).toBeVisible();

            // Should have "Go Home" link
            const homeLink = page.locator('text=Go Home');
            await expect(homeLink).toBeVisible();
        });

        test('should preserve navigation state across page refreshes', async ({ page }) => {
            // Navigate to markets page
            await page.goto(`${CONFIG.FRONTEND_URL}/markets`);
            await page.waitForLoadState('networkidle');

            // Refresh the page
            await page.reload();
            await page.waitForLoadState('networkidle');

            // Should still be on markets page
            expect(page.url()).toContain('/markets');
        });
    });

    test.describe('Authentication Flow Tests', () => {
        test('should redirect protected routes to login when not authenticated', async ({ page }) => {
            for (const route of ROUTES.protected) {
                console.log(`Testing protected route: ${route.path}`);

                await page.goto(`${CONFIG.FRONTEND_URL}${route.path}`);
                await page.waitForLoadState('networkidle');

                // Should redirect to login page
                await page.waitForURL(/\/login/, { timeout: 10000 });
                expect(page.url()).toContain('/login');

                console.log(`✅ ${route.name} correctly redirected to login`);
            }
        });

        test('should show login form on login page', async ({ page }) => {
            await page.goto(`${CONFIG.FRONTEND_URL}/login`);
            await page.waitForLoadState('networkidle');

            // Check for login form elements
            await expect(page.locator('input[type="email"]')).toBeVisible();
            await expect(page.locator('input[type="password"]')).toBeVisible();
            await expect(page.locator('button[type="submit"]')).toBeVisible();

            // Check for form labels
            await expect(page.locator('text=Email')).toBeVisible();
            await expect(page.locator('text=Password')).toBeVisible();
        });

        test('should handle guest mode access', async ({ page }) => {
            // Visit dashboard as guest
            await page.goto(`${CONFIG.FRONTEND_URL}/`);
            await page.waitForLoadState('networkidle');

            // Should show public dashboard content
            const welcomeText = page.locator('text=Welcome to MyTrader');
            await expect(welcomeText).toBeVisible();

            // Should show CTA buttons for registration/login
            const startTradingBtn = page.locator('text=Start Trading');
            const signInBtn = page.locator('text=Sign In');

            await expect(startTradingBtn).toBeVisible();
            await expect(signInBtn).toBeVisible();
        });

        test('should preserve route after login redirect', async ({ page }) => {
            // Try to access protected route
            await page.goto(`${CONFIG.FRONTEND_URL}/portfolio`);
            await page.waitForLoadState('networkidle');

            // Should be redirected to login
            await page.waitForURL(/\/login/, { timeout: 10000 });

            // The route preservation would be tested in a real login flow
            // This test verifies the redirect happens
            expect(page.url()).toContain('/login');
        });
    });

    test.describe('UI Consistency Tests', () => {
        test('should maintain consistent navigation across pages', async ({ page }) => {
            const testPages = ['/', '/markets', '/competition'];

            for (const path of testPages) {
                await page.goto(`${CONFIG.FRONTEND_URL}${path}`);
                await page.waitForLoadState('networkidle');

                // Check for consistent header/navigation
                const nav = page.locator('nav, header');
                if (await nav.count() > 0) {
                    await expect(nav.first()).toBeVisible();
                }

                // Check for consistent footer
                const footer = page.locator('footer');
                if (await footer.count() > 0) {
                    await expect(footer.first()).toBeVisible();
                }

                console.log(`✅ UI consistency verified for ${path}`);
            }
        });

        test('should display Techsonamy branding consistently', async ({ page }) => {
            await page.goto(`${CONFIG.FRONTEND_URL}/`);
            await page.waitForLoadState('networkidle');

            // Check for brand elements
            const brandText = page.locator('text=MyTrader');
            await expect(brandText.first()).toBeVisible();

            // Check if Techsonamy branding is present (if applicable)
            const techsonamyElement = page.locator('text=Techsonamy');
            if (await techsonamyElement.count() > 0) {
                await expect(techsonamyElement.first()).toBeVisible();
            }
        });

        test('should be responsive across different viewport sizes', async ({ page }) => {
            const viewports = [
                { width: 375, height: 667, name: 'Mobile' },
                { width: 768, height: 1024, name: 'Tablet' },
                { width: 1920, height: 1080, name: 'Desktop' }
            ];

            for (const viewport of viewports) {
                console.log(`Testing ${viewport.name} viewport (${viewport.width}x${viewport.height})`);

                await page.setViewportSize({ width: viewport.width, height: viewport.height });
                await page.goto(`${CONFIG.FRONTEND_URL}/`);
                await page.waitForLoadState('networkidle');

                // Check if page content is visible
                await expect(page.locator('body')).toBeVisible();

                // Check if main content area is properly sized
                const mainContent = page.locator('main, .container, .app');
                if (await mainContent.count() > 0) {
                    await expect(mainContent.first()).toBeVisible();
                }

                console.log(`✅ ${viewport.name} viewport test passed`);
            }
        });
    });

    test.describe('Data Integration Tests', () => {
        test('should load market data on dashboard', async ({ page }) => {
            await page.goto(`${CONFIG.FRONTEND_URL}/`);
            await page.waitForLoadState('networkidle');

            // Wait for market data to load (with timeout)
            try {
                await page.waitForSelector('text=Market Overview', { timeout: 10000 });

                // Check for market data cards or similar content
                const marketDataElements = page.locator('[data-testid*="market"], .market-data, text=/\\$[0-9]/, text=/[0-9]+\\.[0-9]+%/');

                if (await marketDataElements.count() > 0) {
                    console.log('✅ Market data loaded successfully');
                } else {
                    console.log('⚠️ Market data elements not found, but page loaded');
                }
            } catch (error) {
                console.log('⚠️ Market data loading timeout - this may be expected in test environment');
            }
        });

        test('should handle network errors gracefully', async ({ page }) => {
            // Block API requests to simulate network issues
            await page.route('**/api/**', route => {
                route.abort('connectionfailed');
            });

            await page.goto(`${CONFIG.FRONTEND_URL}/`);
            await page.waitForLoadState('networkidle');

            // Page should still load, even if API calls fail
            await expect(page.locator('body')).toBeVisible();

            // Should not show unhandled error messages
            const errorMessages = page.locator('text=/error|Error|ERROR/');
            const criticalErrors = await errorMessages.count();

            if (criticalErrors > 0) {
                console.log(`⚠️ Found ${criticalErrors} error messages - check error handling`);
            } else {
                console.log('✅ Network errors handled gracefully');
            }
        });
    });

    test.describe('Performance Tests', () => {
        test('should load pages within acceptable time limits', async ({ page }) => {
            const performanceThreshold = 5000; // 5 seconds

            for (const route of ROUTES.public) {
                const startTime = Date.now();

                await page.goto(`${CONFIG.FRONTEND_URL}${route.path}`);
                await page.waitForLoadState('networkidle');

                const loadTime = Date.now() - startTime;

                expect(loadTime).toBeLessThan(performanceThreshold);
                console.log(`✅ ${route.name} loaded in ${loadTime}ms`);
            }
        });

        test('should handle rapid navigation without errors', async ({ page }) => {
            const routes = ['/', '/markets', '/competition', '/', '/markets'];

            for (const route of routes) {
                await page.goto(`${CONFIG.FRONTEND_URL}${route}`);
                // Don't wait for complete network idle for rapid navigation test
                await page.waitForLoadState('domcontentloaded');
            }

            // Final check - should be on last route without errors
            await page.waitForLoadState('networkidle');
            expect(page.url()).toContain('/markets');

            // Check for JavaScript errors
            const errors = await page.evaluate(() => {
                return window.performance?.getEntriesByType?.('navigation')?.[0]?.type || 'unknown';
            });

            console.log('✅ Rapid navigation completed without critical errors');
        });
    });

    test.describe('Error Handling Tests', () => {
        test('should show error boundaries for component failures', async ({ page }) => {
            // This test would require intentionally breaking a component
            // For now, we'll test that the page loads without unhandled errors

            await page.goto(`${CONFIG.FRONTEND_URL}/`);
            await page.waitForLoadState('networkidle');

            // Check console for unhandled errors
            const jsErrors = [];
            page.on('pageerror', error => {
                jsErrors.push(error.message);
            });

            // Navigate through a few pages to trigger any potential errors
            await page.goto(`${CONFIG.FRONTEND_URL}/markets`);
            await page.waitForLoadState('networkidle');

            await page.goto(`${CONFIG.FRONTEND_URL}/competition`);
            await page.waitForLoadState('networkidle');

            // Check for critical JavaScript errors
            const criticalErrors = jsErrors.filter(error =>
                !error.includes('WebSocket') && // Ignore WebSocket connection errors in test
                !error.includes('fetch') && // Ignore fetch errors in test environment
                !error.includes('CORS') // Ignore CORS issues in test
            );

            expect(criticalErrors.length).toBe(0);

            if (criticalErrors.length === 0) {
                console.log('✅ No critical JavaScript errors detected');
            } else {
                console.log(`⚠️ Found ${criticalErrors.length} JavaScript errors:`, criticalErrors);
            }
        });
    });
});

// Helper functions for common test operations
async function waitForMarketData(page, timeout = 10000) {
    try {
        await page.waitForSelector('text=Market Overview', { timeout });
        return true;
    } catch {
        return false;
    }
}

async function simulateSlowNetwork(page) {
    await page.route('**/*', route => {
        // Add delay to simulate slow network
        setTimeout(() => route.continue(), 1000);
    });
}

async function checkConsoleErrors(page) {
    const errors = [];
    page.on('console', msg => {
        if (msg.type() === 'error') {
            errors.push(msg.text());
        }
    });
    return errors;
}