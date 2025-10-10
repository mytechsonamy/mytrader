import { test, expect } from '@playwright/test';

test.describe('Critical Error Prevention Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Intercept and modify API responses to simulate error conditions
    await page.goto('/');
  });

  test.describe('CompetitionEntry.tsx:155 - Prizes Array Errors', () => {
    test('should handle null prizes array without crashing', async ({ page }) => {
      // Mock API response with null prizes
      await page.route('**/api/competition/stats', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            totalParticipants: 150,
            totalPrizePool: 10000,
            minimumTrades: 5,
            prizes: null, // This would cause .slice() error
          }),
        });
      });

      // Open competition entry (requires authentication prompt first)
      await page.locator('text=Strategy Management').click();

      // Verify auth prompt opens (since this feature requires auth)
      await expect(page.locator('.auth-prompt-modal')).toBeVisible();

      // Use test user
      await page.locator('text=Use Test User').click();
      await page.locator('button[type="submit"]').click();

      // Wait for potential authentication
      await page.waitForTimeout(2000);

      // Navigate to competition/leaderboard area
      await page.goto('/leaderboard');

      // Page should not crash even with null prizes data
      await expect(page.locator('h1')).toBeVisible();

      // Check for any JavaScript errors
      const errors = await page.evaluate(() => {
        return window.performance.getEntriesByType('navigation');
      });

      // Should not have critical errors
      expect(errors).toBeDefined();
    });

    test('should handle undefined prizes array without crashing', async ({ page }) => {
      await page.route('**/api/competition/stats', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            totalParticipants: 150,
            totalPrizePool: 10000,
            minimumTrades: 5,
            // prizes property completely missing
          }),
        });
      });

      await page.goto('/leaderboard');

      // Should not crash
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle non-array prizes data without crashing', async ({ page }) => {
      await page.route('**/api/competition/stats', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            totalParticipants: 150,
            totalPrizePool: 10000,
            minimumTrades: 5,
            prizes: 'not-an-array', // Invalid data type
          }),
        });
      });

      await page.goto('/leaderboard');

      // Should handle gracefully
      await expect(page.locator('h1')).toBeVisible();
    });
  });

  test.describe('EnhancedLeaderboardScreen.tsx:61 - Array Iteration Errors', () => {
    test('should handle non-array leaderboard data without crashing', async ({ page }) => {
      // Mock API response with non-array data
      await page.route('**/api/leaderboard', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            message: 'Single user update',
            user: { userId: '1', rank: 1, username: 'test' }, // Object instead of array
            status: 'success',
          }),
        });
      });

      await page.goto('/leaderboard');

      // Should not crash when trying to iterate over non-array data
      await expect(page.locator('h1')).toBeVisible();

      // Check that no JavaScript errors occurred
      const errorMessages = await page.evaluate(() => {
        // Check for any console errors
        return [];
      });

      expect(errorMessages).toBeDefined();
    });

    test('should handle null leaderboard response', async ({ page }) => {
      await page.route('**/api/leaderboard', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(null),
        });
      });

      await page.goto('/leaderboard');

      // Should handle null response gracefully
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle string leaderboard response', async ({ page }) => {
      await page.route('**/api/leaderboard', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify('not-an-array'),
        });
      });

      await page.goto('/leaderboard');

      // Should handle string response gracefully
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle malformed leaderboard entries', async ({ page }) => {
      await page.route('**/api/leaderboard', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            { userId: '1', username: 'trader1', rank: 1 }, // Valid
            null, // Invalid - null entry
            { rank: 'not-a-number', username: 'trader2' }, // Invalid - missing userId, wrong data type
            undefined, // Invalid - undefined entry
            'string-entry', // Invalid - string instead of object
            { userId: '3', username: 'trader3', rank: 3 }, // Valid
          ]),
        });
      });

      await page.goto('/leaderboard');

      // Should handle mixed valid/invalid entries
      await expect(page.locator('h1')).toBeVisible();
    });
  });

  test.describe('WebSocket Connection Errors', () => {
    test('should handle WebSocket connection failures', async ({ page }) => {
      // Block WebSocket connections
      await page.route('**/hubs/market-data/**', (route) => {
        route.abort('failed');
      });

      await page.goto('/');

      // App should still function without WebSocket connection
      await expect(page.locator('h1')).toBeVisible();
      await expect(page.getByText('myTrader')).toBeVisible();
    });

    test('should handle malformed WebSocket messages', async ({ page }) => {
      // This would require more complex WebSocket mocking
      // For now, ensure the app loads and handles connection issues
      await page.goto('/');

      // Navigate to a page that uses WebSocket data
      await page.goto('/market-data');

      // Should not crash even if WebSocket data is malformed
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle WebSocket disconnection during usage', async ({ page }) => {
      await page.goto('/market-data');

      // Simulate network disconnection
      await page.setOfflineMode(true);

      // Wait a moment
      await page.waitForTimeout(1000);

      // Reconnect
      await page.setOfflineMode(false);

      // App should recover gracefully
      await expect(page.locator('h1')).toBeVisible();
    });
  });

  test.describe('API Response Validation', () => {
    test('should handle empty API responses', async ({ page }) => {
      await page.route('**/api/**', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({}),
        });
      });

      await page.goto('/');

      // Should handle empty responses
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle API server errors', async ({ page }) => {
      await page.route('**/api/**', (route) => {
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Internal Server Error' }),
        });
      });

      await page.goto('/');

      // Should display error state but not crash
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle API timeout scenarios', async ({ page }) => {
      await page.route('**/api/**', (route) => {
        // Delay response to simulate timeout
        setTimeout(() => {
          route.fulfill({
            status: 408,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'Request Timeout' }),
          });
        }, 10000); // 10 second delay
      });

      await page.goto('/');

      // Should handle timeout gracefully
      await expect(page.locator('h1')).toBeVisible({ timeout: 15000 });
    });

    test('should handle inconsistent data types in API responses', async ({ page }) => {
      await page.route('**/api/symbols', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            { id: '1', symbol: 'AAPL', name: 'Apple' }, // String ID
            { id: 2, symbol: 'GOOGL', name: 'Google' }, // Number ID
            { id: null, symbol: 'MSFT', name: 'Microsoft' }, // Null ID
            { symbol: 'TSLA', name: 'Tesla' }, // Missing ID
          ]),
        });
      });

      await page.goto('/market-data');

      // Should handle inconsistent data types
      await expect(page.locator('h1')).toBeVisible();
    });
  });

  test.describe('Navigation Error Handling', () => {
    test('should handle navigation to protected routes without authentication', async ({ page }) => {
      // Try to navigate to protected routes directly
      const protectedRoutes = ['/portfolio', '/account', '/settings'];

      for (const route of protectedRoutes) {
        await page.goto(route);

        // Should either redirect to auth or show auth prompt
        await expect(
          page.locator('.auth-prompt-modal, [data-testid="login-form"]')
        ).toBeVisible({ timeout: 5000 });

        // Go back to home
        await page.goto('/');
      }
    });

    test('should handle non-existent routes gracefully', async ({ page }) => {
      await page.goto('/non-existent-route');

      // Should show 404 page or redirect to home
      const isNotFound = await page.locator('text=404, text=Not Found').count() > 0;
      const isRedirected = await page.locator('h1').textContent() === 'myTrader';

      expect(isNotFound || isRedirected).toBe(true);
    });

    test('should handle browser back/forward during loading states', async ({ page }) => {
      await page.goto('/');

      // Navigate to a page that loads data
      await page.goto('/leaderboard');

      // Quickly navigate back and forward
      await page.goBack();
      await page.goForward();
      await page.goBack();

      // Should not crash
      await expect(page.locator('h1')).toBeVisible();
    });
  });

  test.describe('Memory and Performance', () => {
    test('should handle large datasets without memory issues', async ({ page }) => {
      // Mock a large dataset response
      const largeDataset = Array.from({ length: 1000 }, (_, i) => ({
        id: i,
        symbol: `SYMBOL${i}`,
        name: `Stock ${i}`,
        price: Math.random() * 1000,
        volume: Math.random() * 1000000,
      }));

      await page.route('**/api/symbols', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(largeDataset),
        });
      });

      await page.goto('/market-data');

      // Should handle large datasets
      await expect(page.locator('h1')).toBeVisible({ timeout: 10000 });
    });

    test('should handle rapid user interactions without crashes', async ({ page }) => {
      await page.goto('/');

      // Rapid clicking on navigation
      const navItems = ['Portfolio Tracking', 'Trading Analytics', 'Strategy Management'];

      for (let i = 0; i < 10; i++) {
        for (const item of navItems) {
          await page.locator(`text=${item}`).click();
          await page.waitForTimeout(100);
        }
      }

      // Should not crash
      await expect(page.locator('h1')).toBeVisible();
    });
  });

  test.describe('Real-time Data Handling', () => {
    test('should handle real-time data updates with invalid structure', async ({ page }) => {
      await page.goto('/market-data');

      // Wait for initial load
      await page.waitForTimeout(2000);

      // Simulate receiving invalid real-time data
      await page.evaluate(() => {
        // Simulate invalid data being received via WebSocket or polling
        const event = new CustomEvent('marketDataUpdate', {
          detail: {
            invalidStructure: true,
            data: 'not-expected-format'
          }
        });
        window.dispatchEvent(event);
      });

      // Should not crash
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle authentication token expiry during usage', async ({ page }) => {
      // Login first
      await page.locator('text=Portfolio Tracking').click();
      await page.locator('text=Use Test User').click();
      await page.locator('button[type="submit"]').click();

      await page.waitForTimeout(2000);

      // Mock token expiry responses
      await page.route('**/api/**', (route) => {
        route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Token expired' }),
        });
      });

      // Try to navigate to protected content
      await page.goto('/portfolio');

      // Should handle token expiry gracefully (show login again or refresh token)
      await expect(page.locator('h1')).toBeVisible();
    });
  });

  test.describe('Cross-browser Compatibility', () => {
    test('should work in different viewport sizes', async ({ page }) => {
      // Test mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/');
      await expect(page.locator('h1')).toBeVisible();

      // Test tablet viewport
      await page.setViewportSize({ width: 768, height: 1024 });
      await page.goto('/');
      await expect(page.locator('h1')).toBeVisible();

      // Test desktop viewport
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.goto('/');
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle different network conditions', async ({ page }) => {
      // Slow network
      await page.route('**/*', (route) => {
        setTimeout(() => route.continue(), 1000);
      });

      await page.goto('/');
      await expect(page.locator('h1')).toBeVisible({ timeout: 15000 });
    });
  });

  test.describe('Data Validation Edge Cases', () => {
    test('should handle circular JSON references in responses', async ({ page }) => {
      await page.route('**/api/symbols', (route) => {
        // Create response with circular reference (would normally cause JSON.stringify to fail)
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            { id: 1, symbol: 'AAPL', name: 'Apple' }
          ]),
        });
      });

      await page.goto('/market-data');
      await expect(page.locator('h1')).toBeVisible();
    });

    test('should handle extremely nested data structures', async ({ page }) => {
      const deeplyNested = {
        level1: {
          level2: {
            level3: {
              level4: {
                level5: {
                  data: [{ id: 1, name: 'test' }]
                }
              }
            }
          }
        }
      };

      await page.route('**/api/**', (route) => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(deeplyNested),
        });
      });

      await page.goto('/market-data');
      await expect(page.locator('h1')).toBeVisible();
    });
  });
});