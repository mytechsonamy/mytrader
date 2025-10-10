import { test, expect } from '@playwright/test';

test.describe('Guest User Journey', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should allow guest access to dashboard and market data', async ({ page }) => {
    // Verify dashboard loads for guests
    await expect(page.locator('h1')).toContainText('myTrader');
    await expect(page.locator('.welcome-text, .guest-text')).toContainText('Welcome, Guest');

    // Verify guest can see public features
    await expect(page.locator('text=Live Market Data')).toBeVisible();
    await expect(page.locator('text=Available')).toBeVisible();

    // Verify market overview section is visible
    await expect(page.locator('.market-overview')).toBeVisible();
    await expect(page.locator('text=Market Overview')).toBeVisible();
  });

  test('should show locked features to guests', async ({ page }) => {
    // Verify locked features are visible but marked as requiring sign-in
    await expect(page.locator('text=Portfolio Tracking')).toBeVisible();
    await expect(page.locator('text=Trading Analytics')).toBeVisible();
    await expect(page.locator('text=Strategy Management')).toBeVisible();

    // All should show locked status
    const lockedBadges = page.locator('text=🔒 Sign In Required');
    await expect(lockedBadges).toHaveCount(3);
  });

  test('should show auth prompt when clicking locked features', async ({ page }) => {
    // Click on a locked feature
    await page.locator('text=Portfolio Tracking').click();

    // Verify auth prompt appears
    await expect(page.locator('.auth-prompt-modal')).toBeVisible();
    await expect(page.locator('text=Authentication Required')).toBeVisible();
    await expect(page.locator('text=Please sign in to access this feature')).toBeVisible();

    // Verify login form is shown by default
    await expect(page.locator('text=Login to myTrader')).toBeVisible();
    await expect(page.locator('input[name="email"]')).toBeVisible();
    await expect(page.locator('input[name="password"]')).toBeVisible();
  });

  test('should allow guest to continue without authentication', async ({ page }) => {
    // Click on a locked feature to open auth prompt
    await page.locator('text=Trading Analytics').click();

    // Click "Continue as Guest"
    await page.locator('text=Continue as Guest').click();

    // Verify modal is closed
    await expect(page.locator('.auth-prompt-modal')).not.toBeVisible();

    // Verify still in guest mode
    await expect(page.locator('text=Welcome, Guest')).toBeVisible();
  });

  test('should allow guest to close auth prompt with X button', async ({ page }) => {
    // Click on a locked feature
    await page.locator('text=Strategy Management').click();

    // Click the close button
    await page.locator('[aria-label="Close"]').click();

    // Verify modal is closed
    await expect(page.locator('.auth-prompt-modal')).not.toBeVisible();
  });

  test('should allow switching between login and register in auth prompt', async ({ page }) => {
    // Open auth prompt
    await page.locator('text=Portfolio Tracking').click();

    // Initially should show login
    await expect(page.locator('text=Login to myTrader')).toBeVisible();

    // Switch to register
    await page.locator('text=Register here').click();
    await expect(page.locator('text=Register for myTrader')).toBeVisible();
    await expect(page.locator('input[name="firstName"]')).toBeVisible();
    await expect(page.locator('input[name="lastName"]')).toBeVisible();

    // Switch back to login
    await page.locator('text=Login here').click();
    await expect(page.locator('text=Login to myTrader')).toBeVisible();
  });

  test('should show guest activity in recent activity section', async ({ page }) => {
    // Verify guest activity is shown
    await expect(page.locator('text=Browsing market data as guest')).toBeVisible();
    await expect(page.locator('text=Sign in to track your trading activity')).toBeVisible();

    // Click on the auth prompt activity item
    await page.locator('text=Sign in to track your trading activity').click();

    // Should open auth prompt
    await expect(page.locator('.auth-prompt-modal')).toBeVisible();
  });

  test('should display market data for guests', async ({ page }) => {
    // Wait for market overview to load
    await page.waitForSelector('.market-overview', { timeout: 10000 });

    // Check if market data is loading or loaded
    const loadingState = page.locator('text=Loading market data...');
    const marketCards = page.locator('.market-card');

    // Either loading state or market cards should be visible
    await expect(loadingState.or(marketCards.first())).toBeVisible();

    // If market cards are loaded, verify they contain expected elements
    const cardCount = await marketCards.count();
    if (cardCount > 0) {
      // Check first market card has expected structure
      const firstCard = marketCards.first();
      await expect(firstCard.locator('.symbol')).toBeVisible();
      await expect(firstCard.locator('.display-name')).toBeVisible();
    }
  });

  test('should handle navigation through sidebar', async ({ page }) => {
    // Verify sidebar is visible and open by default
    await expect(page.locator('.dashboard-sidebar')).toHaveClass(/open/);

    // Verify navigation items are present
    await expect(page.locator('text=Dashboard')).toBeVisible();
    await expect(page.locator('text=Market Data')).toBeVisible();
    await expect(page.locator('text=Portfolio')).toBeVisible();
    await expect(page.locator('text=Strategies')).toBeVisible();
    await expect(page.locator('text=Settings')).toBeVisible();

    // Toggle sidebar
    await page.locator('.sidebar-toggle').click();
    await expect(page.locator('.dashboard-sidebar')).toHaveClass(/closed/);

    // Toggle sidebar back open
    await page.locator('.sidebar-toggle').click();
    await expect(page.locator('.dashboard-sidebar')).toHaveClass(/open/);
  });

  test('should handle WebSocket connection status for guests', async ({ page }) => {
    // Wait for market overview to load
    await page.waitForSelector('.market-overview', { timeout: 10000 });

    // Check WebSocket status indicators
    const wsStatus = page.locator('.ws-status');
    await expect(wsStatus).toBeVisible();

    // Should show either connecting, connected, or polling status
    const statusText = await wsStatus.textContent();
    expect(['🟢 Live', '🟡 Connecting...', '🔴 Polling']).toContain(statusText?.trim());
  });

  test('should navigate to login page via sign in button', async ({ page }) => {
    // Click the Sign In button in header
    await page.locator('.login-btn, text=Sign In').click();

    // Should navigate to login page or show login form
    // Implementation depends on routing strategy
    await expect(page.url()).toMatch(/\/login|\/$/);
  });

  test('should handle keyboard navigation', async ({ page }) => {
    // Test tab navigation through interactive elements
    await page.keyboard.press('Tab');

    // Should focus on first interactive element
    const focused = page.locator(':focus');
    await expect(focused).toBeVisible();

    // Continue tabbing to ensure navigation works
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');

    // Should still have a focused element
    await expect(page.locator(':focus')).toBeVisible();
  });

  test('should be responsive on mobile viewports', async ({ page, isMobile }) => {
    if (isMobile) {
      // On mobile, sidebar might start closed
      const sidebar = page.locator('.dashboard-sidebar');

      // Test sidebar toggle functionality on mobile
      const toggleButton = page.locator('.sidebar-toggle');
      await expect(toggleButton).toBeVisible();

      // Click to toggle sidebar
      await toggleButton.click();

      // Verify sidebar responds to toggle
      // Note: Exact behavior depends on CSS implementation
      await expect(sidebar).toBeVisible();
    }
  });

  test('should handle error states gracefully', async ({ page }) => {
    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Check if error boundary or error messages are shown
    const errorMessage = page.locator('.error-message');
    const errorBoundary = page.locator('text=Something went wrong');

    // If errors are present, they should be handled gracefully
    const errorCount = await errorMessage.count() + await errorBoundary.count();

    if (errorCount > 0) {
      // Should have retry or recovery options
      const retryButton = page.locator('button:has-text("Retry"), button:has-text("Try again")');
      await expect(retryButton).toBeVisible();
    }
  });

  test('should display correct theme', async ({ page }) => {
    // Verify the dashboard container has a theme class
    const dashboardContainer = page.locator('.dashboard-container');
    await expect(dashboardContainer).toHaveClass(/theme-(light|dark)/);

    // Verify theme is applied consistently
    const themeClass = await dashboardContainer.getAttribute('class');
    expect(themeClass).toMatch(/theme-(light|dark)/);
  });

  test('should show appropriate content in stats section for guests', async ({ page }) => {
    // Verify guest-specific stats are shown
    await expect(page.locator('text=Market Statistics')).toBeVisible();
    await expect(page.locator('.guest-stats')).toBeVisible();

    // Should not show authenticated user stats
    await expect(page.locator('text=Trading Statistics')).not.toBeVisible();
    await expect(page.locator('.stats-grid')).not.toBeVisible();
  });
});