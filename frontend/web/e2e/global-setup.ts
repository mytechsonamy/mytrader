import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting global E2E test setup...');

  const browser = await chromium.launch();
  const page = await browser.newPage();

  try {
    // Wait for the frontend server to be ready
    console.log('⏳ Waiting for frontend server...');
    await page.goto('http://localhost:3003', { waitUntil: 'networkidle' });

    // Check if the app loads successfully
    await page.waitForSelector('h1:has-text("myTrader")', { timeout: 30000 });
    console.log('✅ Frontend server is ready');

    // Check if backend is accessible (optional)
    try {
      const healthResponse = await page.request.get('http://localhost:8080/health');
      if (healthResponse.ok()) {
        console.log('✅ Backend server is ready');
      } else {
        console.log('⚠️ Backend server is not available, but tests can still run in guest mode');
      }
    } catch (error) {
      console.log('⚠️ Backend server is not available, but tests can still run in guest mode');
    }

  } catch (error) {
    console.error('❌ Failed to setup test environment:', error);
    throw error;
  } finally {
    await browser.close();
  }

  console.log('✅ Global E2E test setup completed');
}

export default globalSetup;