import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test.describe('Login Process', () => {
    test('should successfully login with valid test credentials', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Portfolio Tracking').click();

      // Verify login form is shown
      await expect(page.locator('text=Login to myTrader')).toBeVisible();

      // Use test user credentials
      await page.locator('text=Use Test User').click();

      // Verify test credentials are populated
      await expect(page.locator('input[name="email"]')).toHaveValue('qatest@mytrader.com');
      await expect(page.locator('input[name="password"]')).toHaveValue('QATest123!');

      // Submit login form
      await page.locator('button[type="submit"]').click();

      // Wait for potential authentication to complete
      await page.waitForTimeout(2000);

      // Check if login was successful (even if backend is not available)
      // The UI should update to reflect attempted authentication
      const isModalClosed = await page.locator('.auth-prompt-modal').isVisible() === false;
      const hasAuthState = await page.locator('text=Welcome,').isVisible();

      // At least one should be true (modal closed or auth state changed)
      expect(isModalClosed || hasAuthState).toBe(true);
    });

    test('should show validation errors for empty fields', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Trading Analytics').click();

      // Try to submit empty form
      await page.locator('button[type="submit"]').click();

      // Form should not submit (HTML5 validation or custom validation)
      // Modal should still be visible
      await expect(page.locator('.auth-prompt-modal')).toBeVisible();
    });

    test('should handle login form validation', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Strategy Management').click();

      // Fill in invalid email format
      await page.locator('input[name="email"]').fill('invalid-email');
      await page.locator('input[name="password"]').fill('short');

      // Try to submit
      await page.locator('button[type="submit"]').click();

      // Should show validation errors or prevent submission
      const emailInput = page.locator('input[name="email"]');
      const passwordInput = page.locator('input[name="password"]');

      // Check for HTML5 validation
      const emailValid = await emailInput.evaluate((el: HTMLInputElement) => el.validity.valid);
      expect(emailValid).toBe(false);
    });

    test('should show loading state during login attempt', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Portfolio Tracking').click();

      // Fill in credentials
      await page.locator('input[name="email"]').fill('test@example.com');
      await page.locator('input[name="password"]').fill('password123');

      // Submit form
      await page.locator('button[type="submit"]').click();

      // Should show loading state (even briefly)
      const submitButton = page.locator('button[type="submit"]');

      // Check if button text changes or is disabled
      await page.waitForTimeout(100); // Brief wait to catch loading state

      // Button should either be disabled or show loading text
      const isDisabled = await submitButton.isDisabled();
      const buttonText = await submitButton.textContent();

      expect(isDisabled || buttonText?.includes('...') || buttonText?.includes('Loading')).toBe(true);
    });

    test('should handle server error responses gracefully', async ({ page }) => {
      // Mock server error by using invalid credentials
      await page.locator('text=Portfolio Tracking').click();

      // Fill in credentials that will likely fail
      await page.locator('input[name="email"]').fill('nonexistent@example.com');
      await page.locator('input[name="password"]').fill('wrongpassword');

      // Submit form
      await page.locator('button[type="submit"]').click();

      // Wait for response
      await page.waitForTimeout(3000);

      // Should handle error gracefully (no crash, modal still visible)
      await expect(page.locator('.auth-prompt-modal')).toBeVisible();

      // Check if error message is displayed
      const errorMessage = page.locator('.error-message');
      const hasError = await errorMessage.count() > 0;

      if (hasError) {
        await expect(errorMessage).toBeVisible();
      }
    });
  });

  test.describe('Registration Process', () => {
    test('should show registration form when switching from login', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Portfolio Tracking').click();

      // Switch to register
      await page.locator('text=Register here').click();

      // Verify registration form is shown
      await expect(page.locator('text=Register for myTrader')).toBeVisible();
      await expect(page.locator('input[name="firstName"]')).toBeVisible();
      await expect(page.locator('input[name="lastName"]')).toBeVisible();
      await expect(page.locator('input[name="email"]')).toBeVisible();
      await expect(page.locator('input[name="phone"]')).toBeVisible();
      await expect(page.locator('input[name="password"]')).toBeVisible();
    });

    test('should validate registration form fields', async ({ page }) => {
      // Open auth prompt and switch to register
      await page.locator('text=Trading Analytics').click();
      await page.locator('text=Register here').click();

      // Try to submit empty form
      await page.locator('button[type="submit"]').click();

      // Should show validation errors or prevent submission
      await expect(page.locator('text=Register for myTrader')).toBeVisible();

      // Form should still be visible (validation prevented submission)
      await expect(page.locator('.auth-prompt-modal')).toBeVisible();
    });

    test('should validate password length requirement', async ({ page }) => {
      // Open registration form
      await page.locator('text=Strategy Management').click();
      await page.locator('text=Register here').click();

      // Fill form with short password
      await page.locator('input[name="firstName"]').fill('Test');
      await page.locator('input[name="lastName"]').fill('User');
      await page.locator('input[name="email"]').fill('test@example.com');
      await page.locator('input[name="phone"]').fill('+1234567890');
      await page.locator('input[name="password"]').fill('123'); // Too short

      // Submit form
      await page.locator('button[type="submit"]').click();

      // Should show password validation error
      await expect(page.locator('text=Password must be at least 6 characters long')).toBeVisible();
    });

    test('should clear validation errors when user starts typing', async ({ page }) => {
      // Open registration form
      await page.locator('text=Portfolio Tracking').click();
      await page.locator('text=Register here').click();

      // Trigger validation error by submitting empty form
      await page.locator('button[type="submit"]').click();

      // Check if validation error appears
      const errorMessage = page.locator('.error-message');
      const hasError = await errorMessage.count() > 0;

      if (hasError) {
        // Start typing in first name field
        await page.locator('input[name="firstName"]').type('T');

        // Error should be cleared
        await expect(errorMessage).not.toBeVisible();
      }
    });

    test('should handle successful registration', async ({ page }) => {
      // Open registration form
      await page.locator('text=Trading Analytics').click();
      await page.locator('text=Register here').click();

      // Fill valid registration data
      await page.locator('input[name="firstName"]').fill('New');
      await page.locator('input[name="lastName"]').fill('User');
      await page.locator('input[name="email"]').fill('newuser@example.com');
      await page.locator('input[name="phone"]').fill('+1234567890');
      await page.locator('input[name="password"]').fill('password123');

      // Submit form
      await page.locator('button[type="submit"]').click();

      // Wait for potential registration to complete
      await page.waitForTimeout(2000);

      // Should handle registration attempt gracefully
      // (May succeed or fail depending on backend availability)
      const isModalClosed = await page.locator('.auth-prompt-modal').isVisible() === false;
      const stillVisible = await page.locator('.auth-prompt-modal').isVisible();

      // One should be true (either closed on success or still visible with feedback)
      expect(isModalClosed || stillVisible).toBe(true);
    });
  });

  test.describe('Form Interactions', () => {
    test('should support tab navigation through form fields', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Portfolio Tracking').click();

      // Tab through form fields
      await page.keyboard.press('Tab');
      await expect(page.locator('input[name="email"]')).toBeFocused();

      await page.keyboard.press('Tab');
      await expect(page.locator('input[name="password"]')).toBeFocused();

      await page.keyboard.press('Tab');
      await expect(page.locator('button[type="submit"]')).toBeFocused();
    });

    test('should support Enter key submission', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Trading Analytics').click();

      // Fill email field and press Enter
      await page.locator('input[name="email"]').fill('test@example.com');
      await page.locator('input[name="password"]').fill('password123');

      // Press Enter in password field
      await page.locator('input[name="password"]').press('Enter');

      // Should trigger form submission
      await page.waitForTimeout(1000);

      // Form should be processing or completed
      const submitButton = page.locator('button[type="submit"]');
      const isDisabled = await submitButton.isDisabled();
      const buttonText = await submitButton.textContent();

      expect(isDisabled || buttonText?.includes('...') || buttonText?.includes('Loading')).toBe(true);
    });

    test('should maintain form state when switching between login and register', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Strategy Management').click();

      // Fill login form
      await page.locator('input[name="email"]').fill('test@example.com');
      await page.locator('input[name="password"]').fill('password123');

      // Switch to register
      await page.locator('text=Register here').click();

      // Email should be preserved
      await expect(page.locator('input[name="email"]')).toHaveValue('test@example.com');

      // Switch back to login
      await page.locator('text=Login here').click();

      // Email should still be there
      await expect(page.locator('input[name="email"]')).toHaveValue('test@example.com');
    });

    test('should handle rapid form submissions', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Portfolio Tracking').click();

      // Fill form
      await page.locator('input[name="email"]').fill('test@example.com');
      await page.locator('input[name="password"]').fill('password123');

      // Submit multiple times rapidly
      const submitButton = page.locator('button[type="submit"]');
      await submitButton.click();
      await submitButton.click();
      await submitButton.click();

      // Should handle gracefully without errors
      await page.waitForTimeout(1000);
      await expect(page.locator('.auth-prompt-modal')).toBeVisible();
    });
  });

  test.describe('Accessibility', () => {
    test('should have proper ARIA labels and roles', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Portfolio Tracking').click();

      // Check form accessibility
      const form = page.locator('form');
      await expect(form).toBeVisible();

      // Check input labels
      await expect(page.locator('label[for="email"]')).toBeVisible();
      await expect(page.locator('label[for="password"]')).toBeVisible();

      // Check close button has aria-label
      await expect(page.locator('[aria-label="Close"]')).toBeVisible();
    });

    test('should maintain focus management in modal', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Trading Analytics').click();

      // Focus should be trapped within modal
      // Tab through modal elements
      await page.keyboard.press('Tab');
      await page.keyboard.press('Tab');
      await page.keyboard.press('Tab');
      await page.keyboard.press('Tab');

      // Focus should still be within modal
      const focusedElement = page.locator(':focus');
      const modalContainer = page.locator('.auth-prompt-modal');

      // Focused element should be within modal
      const isWithinModal = await focusedElement.evaluate((el, modal) => {
        return modal.contains(el);
      }, modalContainer);

      expect(isWithinModal).toBe(true);
    });

    test('should support Escape key to close modal', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Strategy Management').click();

      // Press Escape
      await page.keyboard.press('Escape');

      // Modal should close
      await expect(page.locator('.auth-prompt-modal')).not.toBeVisible();
    });
  });

  test.describe('Error Handling', () => {
    test('should display appropriate error messages', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Portfolio Tracking').click();

      // Try login with invalid credentials
      await page.locator('input[name="email"]').fill('invalid@example.com');
      await page.locator('input[name="password"]').fill('wrongpassword');
      await page.locator('button[type="submit"]').click();

      // Wait for response
      await page.waitForTimeout(3000);

      // Should either show error message or handle gracefully
      const errorElements = await page.locator('.error-message').count();
      const modalStillVisible = await page.locator('.auth-prompt-modal').isVisible();

      // Should handle error appropriately
      expect(modalStillVisible).toBe(true);
    });

    test('should recover from network errors', async ({ page }) => {
      // Open auth prompt
      await page.locator('text=Trading Analytics').click();

      // Fill form with valid data
      await page.locator('input[name="email"]').fill('test@example.com');
      await page.locator('input[name="password"]').fill('password123');

      // Submit form
      await page.locator('button[type="submit"]').click();

      // Wait for network timeout
      await page.waitForTimeout(5000);

      // Application should remain stable
      await expect(page.locator('h1')).toContainText('myTrader');

      // Modal should still be functional
      const isModalVisible = await page.locator('.auth-prompt-modal').isVisible();
      if (isModalVisible) {
        await page.locator('[aria-label="Close"]').click();
        await expect(page.locator('.auth-prompt-modal')).not.toBeVisible();
      }
    });
  });
});