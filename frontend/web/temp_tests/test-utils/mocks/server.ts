import { setupServer } from 'msw/node';
import { handlers } from './handlers';

// This configures a request mocking server for testing
export const server = setupServer(...handlers);

// Enable request mocking for all tests
beforeAll(() => {
  server.listen({
    onUnhandledRequest: 'warn',
  });
});

// Reset handlers after each test
afterEach(() => {
  server.resetHandlers();
});

// Disable request mocking after all tests are done
afterAll(() => {
  server.close();
});