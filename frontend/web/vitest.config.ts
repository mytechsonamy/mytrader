/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    // Test Environment Configuration
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test-utils/setup.ts'],
    
    // Coverage Configuration
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html', 'lcov'],
      reportsDirectory: './coverage',
      include: [
        'src/**/*.{js,jsx,ts,tsx}'
      ],
      exclude: [
        'src/**/*.d.ts',
        'src/**/*.test.{js,jsx,ts,tsx}',
        'src/**/*.spec.{js,jsx,ts,tsx}',
        'src/**/index.{js,jsx,ts,tsx}',
        'src/test-utils/**',
        'src/vite-env.d.ts',
        'src/main.tsx'
      ],
      thresholds: {
        global: {
          branches: 75,
          functions: 80,
          lines: 80,
          statements: 80
        },
        // Critical components should have higher coverage
        'src/components/dashboard/**': {
          branches: 85,
          functions: 90,
          lines: 85,
          statements: 85
        },
        'src/services/**': {
          branches: 90,
          functions: 95,
          lines: 90,
          statements: 90
        },
        'src/store/**': {
          branches: 85,
          functions: 90,
          lines: 85,
          statements: 85
        }
      }
    },
    
    // Test Execution Configuration
    testTimeout: 10000,
    hookTimeout: 10000,
    maxConcurrency: 5,
    
    // File Patterns
    include: [
      'src/**/*.{test,spec}.{js,jsx,ts,tsx}'
    ],
    exclude: [
      'node_modules/**',
      'dist/**',
      'build/**',
      'coverage/**',
      'e2e/**'
    ],
    
    // Test Categories via Tags
    // Usage: npm run test:smoke, npm run test:critical
    testNamePattern: process.env.TEST_NAME_PATTERN,
    
    // Reporters
    reporters: [
      'default',
      'json',
      'html'
    ],
    outputFile: {
      json: './test-results/results.json',
      html: './test-results/results.html'
    },
    
    // Mock Configuration
    deps: {
      inline: [
        '@testing-library/jest-dom',
        '@testing-library/react',
        '@testing-library/user-event'
      ]
    },
    
    // Watch Configuration
    watchExclude: [
      '**/node_modules/**',
      '**/dist/**',
      '**/build/**',
      '**/coverage/**'
    ]
  },
  
  // Path Resolution for Tests
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@components': path.resolve(__dirname, './src/components'),
      '@services': path.resolve(__dirname, './src/services'),
      '@store': path.resolve(__dirname, './src/store'),
      '@test-utils': path.resolve(__dirname, './src/test-utils')
    }
  }
});