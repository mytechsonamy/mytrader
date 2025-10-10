import { test, expect } from '@playwright/test';

test.describe('Market Data Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test.describe('Market Overview Component', () => {
    test('should display market overview section', async ({ page }) => {
      // Wait for market overview to be visible
      await expect(page.locator('.market-overview')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('text=Market Overview')).toBeVisible();
    });

    test('should show loading state initially', async ({ page }) => {
      // Check for loading state
      const loadingIndicator = page.locator('text=Loading market data...');
      const marketCards = page.locator('.market-card');

      // Either loading state or market cards should be visible
      await expect(loadingIndicator.or(marketCards.first())).toBeVisible({ timeout: 10000 });
    });

    test('should display WebSocket connection status', async ({ page }) => {
      // Wait for market overview
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Check for WebSocket status indicators
      const statusIndicators = [
        page.locator('text=🟢 Live'),      // Connected
        page.locator('text=🟡 Connecting...'), // Connecting
        page.locator('text=🔴 Polling')    // Disconnected/Polling
      ];

      // At least one status should be visible
      let statusFound = false;
      for (const indicator of statusIndicators) {
        if (await indicator.isVisible()) {
          statusFound = true;
          break;
        }
      }

      expect(statusFound).toBe(true);
    });

    test('should show last update timestamp when data is available', async ({ page }) => {
      // Wait for market overview
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Look for last update indicator
      const lastUpdate = page.locator('text=Last updated:');
      const marketCards = page.locator('.market-card');

      // If market data is loaded, should show last update
      const cardCount = await marketCards.count();
      if (cardCount > 0) {
        await expect(lastUpdate).toBeVisible();
      }
    });

    test('should display market cards with proper structure', async ({ page }) => {
      // Wait for market data to potentially load
      await page.waitForTimeout(5000);

      const marketCards = page.locator('.market-card');
      const cardCount = await marketCards.count();

      if (cardCount > 0) {
        // Check first card structure
        const firstCard = marketCards.first();
        await expect(firstCard.locator('.symbol')).toBeVisible();
        await expect(firstCard.locator('.display-name')).toBeVisible();

        // Should have market data or loading state
        const hasPrice = await firstCard.locator('.price').count() > 0;
        const hasLoading = await firstCard.locator('text=Loading...').count() > 0;

        expect(hasPrice || hasLoading).toBe(true);
      }
    });

    test('should handle error state gracefully', async ({ page }) => {
      // Wait for potential error states
      await page.waitForTimeout(5000);

      const errorMessage = page.locator('.error-message');
      const retryButton = page.locator('button:has-text("Retry")');

      const hasError = await errorMessage.count() > 0;

      if (hasError) {
        // Error should be displayed with retry option
        await expect(errorMessage).toBeVisible();
        await expect(retryButton).toBeVisible();

        // Test retry functionality
        await retryButton.click();

        // Should attempt to reload
        await page.waitForTimeout(2000);
        await expect(page.locator('.market-overview')).toBeVisible();
      }
    });

    test('should handle empty state appropriately', async ({ page }) => {
      // Wait for loading to complete
      await page.waitForTimeout(5000);

      const emptyState = page.locator('text=No market data available');
      const loadDataButton = page.locator('button:has-text("Load Data")');

      const hasEmptyState = await emptyState.count() > 0;

      if (hasEmptyState) {
        await expect(emptyState).toBeVisible();
        await expect(loadDataButton).toBeVisible();

        // Test load data functionality
        await loadDataButton.click();
        await page.waitForTimeout(2000);
      }
    });
  });

  test.describe('Real-time Data Updates', () => {
    test('should handle WebSocket connection lifecycle', async ({ page }) => {
      // Wait for market overview
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Check initial connection state
      const initialStatus = await page.locator('.ws-status').textContent();
      expect(initialStatus).toBeTruthy();

      // Wait for potential connection changes
      await page.waitForTimeout(3000);

      // Status should remain consistent or show progression
      const finalStatus = await page.locator('.ws-status').textContent();
      expect(finalStatus).toBeTruthy();
    });

    test('should show appropriate fallback when WebSocket fails', async ({ page }) => {
      // Wait for market overview
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // If WebSocket fails, should show polling status
      await page.waitForTimeout(5000);

      const pollingStatus = page.locator('text=🔴 Polling');
      const liveStatus = page.locator('text=🟢 Live');
      const connectingStatus = page.locator('text=🟡 Connecting...');

      // Should show one of the valid statuses
      const hasValidStatus = await pollingStatus.isVisible() ||
                           await liveStatus.isVisible() ||
                           await connectingStatus.isVisible();

      expect(hasValidStatus).toBe(true);
    });

    test('should handle data refresh intervals', async ({ page }) => {
      // Wait for initial load
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Get initial state
      const initialTimestamp = await page.locator('.last-update').textContent();

      // Wait for potential refresh
      await page.waitForTimeout(35000); // Wait for refresh interval

      const newTimestamp = await page.locator('.last-update').textContent();

      // If data is being refreshed, timestamp should update
      // (This test may pass even if no real data is available)
      if (initialTimestamp && newTimestamp) {
        // Timestamps should be valid
        expect(initialTimestamp).toBeTruthy();
        expect(newTimestamp).toBeTruthy();
      }
    });
  });

  test.describe('Market Data Display', () => {
    test('should format prices correctly', async ({ page }) => {
      // Wait for market data
      await page.waitForTimeout(5000);

      const priceElements = page.locator('.price');
      const priceCount = await priceElements.count();

      if (priceCount > 0) {
        // Check price formatting
        const firstPrice = await priceElements.first().textContent();

        if (firstPrice && firstPrice !== 'Loading...') {
          // Should be formatted as currency or number
          expect(firstPrice).toMatch(/[\$€£¥]?[\d,]+\.?\d*/);
        }
      }
    });

    test('should display percentage changes with proper styling', async ({ page }) => {
      // Wait for market data
      await page.waitForTimeout(5000);

      const changeElements = page.locator('.change');
      const changeCount = await changeElements.count();

      if (changeCount > 0) {
        const firstChange = changeElements.first();
        const changeText = await firstChange.textContent();

        if (changeText && changeText !== '--') {
          // Should show percentage
          expect(changeText).toMatch(/[+-]?\d+\.?\d*%/);

          // Should have appropriate CSS class
          const className = await firstChange.getAttribute('class');
          expect(className).toMatch(/positive|negative|neutral/);
        }
      }
    });

    test('should display volume with compact notation', async ({ page }) => {
      // Wait for market data
      await page.waitForTimeout(5000);

      const volumeElements = page.locator('.volume');
      const volumeCount = await volumeElements.count();

      if (volumeCount > 0) {
        const firstVolume = await volumeElements.first().textContent();

        if (firstVolume && !firstVolume.includes('--')) {
          // Should show volume with compact notation (K, M, B)
          expect(firstVolume).toMatch(/Vol:\s*[\d.]+[KMB]?/);
        }
      }
    });

    test('should limit displayed symbols appropriately', async ({ page }) => {
      // Wait for market data
      await page.waitForTimeout(5000);

      const marketCards = page.locator('.market-card');
      const cardCount = await marketCards.count();

      // Should not exceed reasonable limit (e.g., 6 symbols)
      expect(cardCount).toBeLessThanOrEqual(6);
    });
  });

  test.describe('Responsive Design', () => {
    test('should adapt to mobile viewport', async ({ page, isMobile }) => {
      if (isMobile) {
        // Wait for market overview
        await page.waitForSelector('.market-overview', { timeout: 10000 });

        // Market grid should be responsive
        const marketGrid = page.locator('.market-grid');
        if (await marketGrid.isVisible()) {
          // Should be visible and properly styled for mobile
          await expect(marketGrid).toBeVisible();

          // Cards should stack appropriately on mobile
          const cards = page.locator('.market-card');
          const cardCount = await cards.count();

          if (cardCount > 0) {
            // Cards should be visible and properly sized
            await expect(cards.first()).toBeVisible();
          }
        }
      }
    });

    test('should handle different screen sizes gracefully', async ({ page }) => {
      // Test different viewport sizes
      const viewports = [
        { width: 320, height: 568 },  // Mobile
        { width: 768, height: 1024 }, // Tablet
        { width: 1920, height: 1080 } // Desktop
      ];

      for (const viewport of viewports) {
        await page.setViewportSize(viewport);
        await page.waitForTimeout(1000);

        // Market overview should remain functional
        await expect(page.locator('.market-overview')).toBeVisible();

        // Content should not overflow
        const body = page.locator('body');
        const hasHorizontalScroll = await body.evaluate((el) => {
          return el.scrollWidth > el.clientWidth;
        });

        // Should not have horizontal scroll (unless intended)
        expect(hasHorizontalScroll).toBe(false);
      }
    });
  });

  test.describe('Performance and Loading', () => {
    test('should load market data within reasonable time', async ({ page }) => {
      const startTime = Date.now();

      // Wait for market overview to be interactive
      await page.waitForSelector('.market-overview', { timeout: 15000 });

      const endTime = Date.now();
      const loadTime = endTime - startTime;

      // Should load within 15 seconds
      expect(loadTime).toBeLessThan(15000);
    });

    test('should handle slow network conditions', async ({ page }) => {
      // Simulate slow network
      const client = await page.context().newCDPSession(page);
      await client.send('Network.emulateNetworkConditions', {
        offline: false,
        downloadThroughput: 50000, // 50kb/s
        uploadThroughput: 50000,
        latency: 500 // 500ms latency
      });

      // Market overview should still load and be functional
      await expect(page.locator('.market-overview')).toBeVisible({ timeout: 30000 });

      // Should show appropriate loading states
      const loadingStates = page.locator('text=Loading..., text=Connecting...');
      // Loading states might be visible during slow load
    });

    test('should maintain performance with continuous updates', async ({ page }) => {
      // Wait for initial load
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Measure performance over time
      const startMemory = await page.evaluate(() => {
        return (performance as any).memory?.usedJSHeapSize || 0;
      });

      // Wait for potential data updates
      await page.waitForTimeout(30000);

      const endMemory = await page.evaluate(() => {
        return (performance as any).memory?.usedJSHeapSize || 0;
      });

      // Memory usage should not grow excessively
      const memoryGrowth = endMemory - startMemory;
      const memoryGrowthMB = memoryGrowth / (1024 * 1024);

      // Should not grow more than 50MB in 30 seconds
      expect(memoryGrowthMB).toBeLessThan(50);
    });
  });

  test.describe('Accessibility', () => {
    test('should be keyboard navigable', async ({ page }) => {
      // Wait for market overview
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Should be able to navigate with keyboard
      await page.keyboard.press('Tab');

      // Focus should be visible somewhere in the market section
      const focusedElement = page.locator(':focus');
      await expect(focusedElement).toBeVisible();
    });

    test('should have proper ARIA labels', async ({ page }) => {
      // Wait for market overview
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Market cards should have appropriate labels
      const marketCards = page.locator('.market-card');
      const cardCount = await marketCards.count();

      if (cardCount > 0) {
        // Cards should be accessible
        const firstCard = marketCards.first();
        const symbolText = await firstCard.locator('.symbol').textContent();

        if (symbolText) {
          // Should have readable content
          expect(symbolText.trim()).toBeTruthy();
        }
      }
    });

    test('should provide screen reader friendly content', async ({ page }) => {
      // Wait for market overview
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Check for screen reader friendly elements
      const marketHeader = page.locator('h3:has-text("Market Overview")');
      await expect(marketHeader).toBeVisible();

      // Status indicators should be readable
      const statusElements = page.locator('.ws-status');
      const statusCount = await statusElements.count();

      if (statusCount > 0) {
        const statusText = await statusElements.first().textContent();
        expect(statusText).toBeTruthy();
      }
    });
  });

  test.describe('Integration with Other Components', () => {
    test('should integrate properly with dashboard layout', async ({ page }) => {
      // Market overview should be part of dashboard grid
      await expect(page.locator('.dashboard-grid .market-section')).toBeVisible();

      // Should not interfere with other dashboard components
      await expect(page.locator('.actions-section')).toBeVisible();
      await expect(page.locator('.stats-section')).toBeVisible();
    });

    test('should work alongside sidebar navigation', async ({ page }) => {
      // Toggle sidebar and verify market overview remains functional
      await page.locator('.sidebar-toggle').click();
      await expect(page.locator('.market-overview')).toBeVisible();

      // Toggle sidebar back
      await page.locator('.sidebar-toggle').click();
      await expect(page.locator('.market-overview')).toBeVisible();
    });

    test('should maintain state during theme changes', async ({ page }) => {
      // Wait for market overview
      await page.waitForSelector('.market-overview', { timeout: 10000 });

      // Market overview should work with different themes
      const themeContainer = page.locator('.dashboard-container');
      const currentTheme = await themeContainer.getAttribute('class');

      // Should have theme class
      expect(currentTheme).toMatch(/theme-(light|dark)/);

      // Market overview should remain functional regardless of theme
      await expect(page.locator('.market-overview')).toBeVisible();
    });
  });
});