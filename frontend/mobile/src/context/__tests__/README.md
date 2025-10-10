# PriceContext Auto-Subscribe Tests

## Overview

This directory contains unit tests for the PriceContext auto-subscribe functionality, which is a critical feature for real-time market data updates in the MyTrader mobile app.

## Test Files

### PriceContext.autosubscribe.simple.test.ts

Comprehensive unit tests for the auto-subscribe logic in PriceContext. These tests verify:

1. **Auto-subscribe on connection** (Requirements: 1.1, 1.4)
   - Automatic subscription to CRYPTO symbols when SignalR connection is established
   - Proper error handling when subscription fails
   - Prevention of subscription attempts when connection fails
   - Event handler registration before connection starts

2. **Re-subscription after reconnection** (Requirements: 1.1, 1.3)
   - Automatic re-subscription when connection is re-established
   - Error handling during re-subscription
   - Connection state maintenance during reconnection process

3. **Subscription cleanup on unmount** (Requirements: 1.3)
   - Proper cleanup of SignalR connection when component unmounts
   - Handling cleanup during active connection attempts
   - Event handler cleanup

4. **Edge cases and error handling**
   - Multiple rapid reconnections
   - Authentication token handling
   - Retry counter reset on successful connection
   - Connection close event handling

5. **Subscription state management**
   - Tracking of subscribed symbols
   - Maintenance of subscription list after reconnection

## Test Results

All 16 tests pass successfully:

```
PASS src/context/__tests__/PriceContext.autosubscribe.simple.test.ts
  PriceContext Auto-Subscribe Logic
    Auto-subscribe on connection
      ✓ should call SubscribeToPriceUpdates with CRYPTO symbols after connection
      ✓ should handle subscription errors gracefully
      ✓ should not subscribe if connection fails
      ✓ should register event handlers before starting connection
    Re-subscription after reconnection
      ✓ should re-subscribe when onreconnected callback is triggered
      ✓ should handle re-subscription errors after reconnection
      ✓ should maintain connection state during reconnection process
    Subscription cleanup on unmount
      ✓ should stop SignalR connection on cleanup
      ✓ should handle cleanup during connection attempt
      ✓ should clear event handlers on cleanup
    Edge cases and error handling
      ✓ should handle multiple rapid reconnections
      ✓ should handle connection with authentication token
      ✓ should reset retry counter on successful connection
      ✓ should handle connection close event
    Subscription state management
      ✓ should track subscribed symbols
      ✓ should maintain subscription list after reconnection

Test Suites: 1 passed, 1 total
Tests:       16 passed, 16 total
```

## Running the Tests

To run these tests:

```bash
# Run all tests in this file
npm test -- src/context/__tests__/PriceContext.autosubscribe.simple.test.ts --no-coverage

# Run with coverage
npm test -- src/context/__tests__/PriceContext.autosubscribe.simple.test.ts

# Run in watch mode
npm test -- src/context/__tests__/PriceContext.autosubscribe.simple.test.ts --watch
```

## Test Strategy

The tests use a simplified approach that focuses on testing the SignalR connection logic without requiring the full React component rendering. This approach:

- Uses Jest mocks for SignalR and AsyncStorage
- Tests the core subscription logic in isolation
- Avoids React Native rendering issues in the test environment
- Provides fast, reliable test execution

## Requirements Coverage

These tests satisfy the following requirements from the mobile-app-enhancement spec:

- **Requirement 1.1**: Auto-subscribe triggers on connection
- **Requirement 1.3**: Re-subscription after reconnection and subscription cleanup on unmount

## Future Enhancements

Potential areas for additional testing:

1. Integration tests with actual PriceContext component rendering
2. Tests for price update event handling
3. Tests for multiple market subscriptions (BIST, NASDAQ, NYSE)
4. Performance tests for high-frequency price updates
5. Memory leak detection tests for long-running connections

## Notes

- The original `PriceContext.autosubscribe.test.tsx` file was created but had React rendering issues due to version compatibility. The simplified `.simple.test.ts` version focuses on logic testing without rendering.
- Jest configuration was updated to fix module name mapping and remove optional dependencies.
- Test setup was updated to comment out mocks for packages not installed in the project.
