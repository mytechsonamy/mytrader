import { FullConfig } from '@playwright/test';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting global E2E test teardown...');

  // Cleanup any global resources if needed
  // For example, close database connections, stop test servers, etc.

  console.log('✅ Global E2E test teardown completed');
}

export default globalTeardown;