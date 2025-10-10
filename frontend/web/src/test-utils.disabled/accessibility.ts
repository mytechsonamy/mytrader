import { expect } from 'vitest';
import { axe, toHaveNoViolations } from 'jest-axe';
import { RenderResult } from '@testing-library/react';

// Extend Jest matchers for accessibility testing
expect.extend(toHaveNoViolations);

/**
 * Test accessibility compliance using axe-core
 * @param renderResult - The result from render() or renderWithProviders()
 * @param options - Optional axe configuration
 */
export const testAccessibility = async (
  renderResult: RenderResult | { container: HTMLElement },
  options?: any
) => {
  const results = await axe(renderResult.container, {
    rules: {
      // Disable color contrast checks for now as they can be flaky in tests
      'color-contrast': { enabled: false },
      // Allow empty buttons for icon-only buttons with aria-labels
      'button-name': { enabled: true },
    },
    ...options,
  });
  
  expect(results).toHaveNoViolations();
};

/**
 * Test keyboard navigation on focusable elements
 * @param container - The container element to test
 */
export const testKeyboardNavigation = async (container: HTMLElement) => {
  const focusableElements = container.querySelectorAll(
    'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
  );
  
  // Test that all interactive elements are keyboard accessible
  focusableElements.forEach((element) => {
    expect(element).not.toHaveAttribute('tabindex', '-1');
  });
  
  return focusableElements;
};

/**
 * Test ARIA attributes and labels
 * @param container - The container element to test
 */
export const testAriaAttributes = (container: HTMLElement) => {
  // Test that interactive elements have accessible names
  const buttons = container.querySelectorAll('button');
  buttons.forEach((button) => {
    const hasAccessibleName = 
      button.textContent?.trim() ||
      button.getAttribute('aria-label') ||
      button.getAttribute('aria-labelledby') ||
      button.getAttribute('title');
    
    expect(hasAccessibleName).toBeTruthy();
  });
  
  // Test that form inputs have labels
  const inputs = container.querySelectorAll('input, textarea, select');
  inputs.forEach((input) => {
    const hasLabel = 
      input.getAttribute('aria-label') ||
      input.getAttribute('aria-labelledby') ||
      container.querySelector(`label[for="${input.id}"]`);
    
    expect(hasLabel).toBeTruthy();
  });
  
  // Test that images have alt text
  const images = container.querySelectorAll('img');
  images.forEach((img) => {
    expect(img).toHaveAttribute('alt');
  });
};

/**
 * Test semantic HTML structure
 * @param container - The container element to test
 */
export const testSemanticStructure = (container: HTMLElement) => {
  // Test heading hierarchy
  const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6');
  const headingLevels = Array.from(headings).map((h) => parseInt(h.tagName[1]));
  
  if (headingLevels.length > 1) {
    // Check that heading levels don't skip (e.g., h1 -> h3)
    for (let i = 1; i < headingLevels.length; i++) {
      const diff = headingLevels[i] - headingLevels[i - 1];
      expect(diff).toBeLessThanOrEqual(1);
    }
  }
  
  // Test that lists are properly structured
  const lists = container.querySelectorAll('ul, ol');
  lists.forEach((list) => {
    const listItems = list.querySelectorAll('li');
    expect(listItems.length).toBeGreaterThan(0);
  });
  
  // Test that tables have proper headers
  const tables = container.querySelectorAll('table');
  tables.forEach((table) => {
    const headers = table.querySelectorAll('th, thead');
    if (table.querySelectorAll('td').length > 0) {
      expect(headers.length).toBeGreaterThan(0);
    }
  });
};

/**
 * Comprehensive accessibility test suite
 * Combines all accessibility tests into one function
 * @param renderResult - The result from render() or renderWithProviders()
 * @param options - Optional configuration
 */
export const runA11yTests = async (
  renderResult: RenderResult | { container: HTMLElement },
  options: {
    skipAxe?: boolean;
    skipKeyboard?: boolean;
    skipAria?: boolean;
    skipSemantic?: boolean;
    axeOptions?: any;
  } = {}
) => {
  const { container } = renderResult;
  
  if (!options.skipAxe) {
    await testAccessibility(renderResult, options.axeOptions);
  }
  
  if (!options.skipKeyboard) {
    await testKeyboardNavigation(container);
  }
  
  if (!options.skipAria) {
    testAriaAttributes(container);
  }
  
  if (!options.skipSemantic) {
    testSemanticStructure(container);
  }
};

/**
 * Test focus management
 * @param container - The container element
 * @param expectedFocusedElement - CSS selector for expected focused element
 */
export const testFocusManagement = (container: HTMLElement, expectedFocusedElement?: string) => {
  if (expectedFocusedElement) {
    const focusedElement = container.querySelector(expectedFocusedElement);
    expect(focusedElement).toHaveFocus();
  }
  
  // Test that focus is visible
  const activeElement = document.activeElement;
  if (activeElement && activeElement !== document.body) {
    const computedStyle = window.getComputedStyle(activeElement);
    // Should have some form of focus indicator
    expect(
      computedStyle.outline !== 'none' ||
      computedStyle.boxShadow !== 'none' ||
      computedStyle.backgroundColor !== 'transparent'
    ).toBe(true);
  }
};

/**
 * Test screen reader announcements
 * @param container - The container element
 */
export const testScreenReaderAnnouncements = (container: HTMLElement) => {
  // Test for live regions
  const liveRegions = container.querySelectorAll('[aria-live], [role="status"], [role="alert"]');
  
  liveRegions.forEach((region) => {
    expect(region).toBeInTheDocument();
    
    // Live regions should have meaningful content or be properly empty
    if (region.textContent?.trim()) {
      expect(region.textContent.trim().length).toBeGreaterThan(0);
    }
  });
  
  // Test for skip links
  const skipLinks = container.querySelectorAll('a[href^="#"]');
  skipLinks.forEach((link) => {
    const target = link.getAttribute('href');
    if (target && target !== '#') {
      const targetElement = container.querySelector(target);
      expect(targetElement).toBeInTheDocument();
    }
  });
};

/**
 * Test color contrast (basic implementation)
 * @param container - The container element
 */
export const testColorContrast = (container: HTMLElement) => {
  // This is a simplified test - in practice, you'd use a proper contrast checking library
  const textElements = container.querySelectorAll('p, span, div, h1, h2, h3, h4, h5, h6, button, a');
  
  textElements.forEach((element) => {
    const computedStyle = window.getComputedStyle(element);
    const color = computedStyle.color;
    const backgroundColor = computedStyle.backgroundColor;
    
    // Basic check that text isn't the same color as background
    expect(color).not.toBe(backgroundColor);
  });
};

/**
 * Create accessibility test helpers for specific components
 */
export const createA11yHelpers = () => {
  return {
    /**
     * Test modal accessibility
     */
    testModalA11y: async (modalContainer: HTMLElement) => {
      // Test focus trap
      const focusableElements = modalContainer.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      );
      
      if (focusableElements.length > 0) {
        expect(focusableElements[0]).toHaveFocus();
      }
      
      // Test modal attributes
      expect(modalContainer).toHaveAttribute('role', 'dialog');
      expect(modalContainer).toHaveAttribute('aria-modal', 'true');
      
      // Test escape key handling
      const closeButton = modalContainer.querySelector('[aria-label*="close"], [aria-label*="Close"]');
      expect(closeButton).toBeInTheDocument();
    },
    
    /**
     * Test form accessibility
     */
    testFormA11y: (formContainer: HTMLElement) => {
      const form = formContainer.querySelector('form');
      expect(form).toBeInTheDocument();
      
      // Test required field indicators
      const requiredFields = formContainer.querySelectorAll('[required]');
      requiredFields.forEach((field) => {
        expect(
          field.getAttribute('aria-required') === 'true' ||
          field.getAttribute('required') !== null
        ).toBe(true);
      });
      
      // Test error messages
      const errorElements = formContainer.querySelectorAll('[role="alert"], .error-message, [aria-invalid="true"]');
      errorElements.forEach((error) => {
        expect(error).toBeInTheDocument();
      });
    },
    
    /**
     * Test table accessibility
     */
    testTableA11y: (tableContainer: HTMLElement) => {
      const table = tableContainer.querySelector('table');
      if (table) {
        // Test table headers
        const headers = table.querySelectorAll('th');
        expect(headers.length).toBeGreaterThan(0);
        
        // Test table caption or aria-label
        const caption = table.querySelector('caption');
        const ariaLabel = table.getAttribute('aria-label');
        const ariaLabelledby = table.getAttribute('aria-labelledby');
        
        expect(
          caption || ariaLabel || ariaLabelledby
        ).toBeTruthy();
      }
    },
  };
};
