# MyTrader Frontend Test Structure

This document outlines the comprehensive test structure for the MyTrader React web frontend application.

## Test Organization

The test suite follows a hierarchical structure that mirrors the source code organization:

```
src/
├── __tests__/                    # Root-level test files
├── components/
│   ├── __tests__/                # Component tests
│   │   ├── Dashboard.test.tsx    # Main dashboard component
│   │   ├── Login.test.tsx        # Authentication components
│   │   ├── Register.test.tsx
│   │   ├── AuthPrompt.test.tsx
│   │   ├── ErrorBoundary.test.tsx
│   │   └── dashboard/            # Dashboard subcomponents
│   │       ├── UserProfile.test.tsx
│   │       ├── NotificationCenter.test.tsx
│   │       ├── QuickActions.test.tsx
│   │       ├── NewsSection.test.tsx
│   │       ├── LeaderboardSection.test.tsx
│   │       └── MarketOverview.test.tsx
├── hooks/
│   └── __tests__/                # Custom hooks tests
│       └── useWebSocket.test.tsx
├── services/
│   └── __tests__/                # Service layer tests
│       ├── authService.test.ts
│       ├── marketDataService.test.ts
│       ├── websocketService.test.ts
│       └── websocketService.integration.test.ts
├── store/
│   ├── __tests__/                # Store integration tests
│   │   └── store.integration.test.ts
│   └── slices/
│       └── __tests__/            # Redux slice tests
│           ├── authSlice.test.ts
│           ├── marketSlice.test.ts
│           └── uiSlice.test.ts
├── test-utils/                   # Testing utilities and helpers
│   ├── index.tsx                 # Main test utilities
│   ├── accessibility.ts         # Accessibility testing helpers
│   ├── factories.ts              # Test data factories
│   ├── apiIntegration.test.ts    # API integration test helpers
│   └── performance.test.ts       # Performance testing utilities
└── App.test.tsx                  # Main app component test
```

## Test Categories

### 1. Unit Tests
- **Components**: Individual React component functionality
- **Hooks**: Custom React hooks behavior
- **Services**: Business logic and API interaction
- **Slices**: Redux state management logic

### 2. Integration Tests
- **Store Integration**: Complete Redux store behavior
- **WebSocket Integration**: Real-time data connectivity
- **API Integration**: End-to-end service communication

### 3. Accessibility Tests
- **WCAG 2.1 AA Compliance**: Using jest-axe for automated testing
- **Keyboard Navigation**: Tab order and focus management
- **Screen Reader Support**: ARIA attributes and semantic HTML
- **Color Contrast**: Visual accessibility standards

### 4. Performance Tests
- **Component Rendering**: Render time optimization
- **Bundle Size**: Code splitting effectiveness
- **Memory Leaks**: Cleanup verification

## Testing Standards

### Test File Naming
- Unit tests: `ComponentName.test.tsx` or `serviceName.test.ts`
- Integration tests: `feature.integration.test.ts`
- Accessibility tests: `ComponentName.a11y.test.tsx`
- Performance tests: `ComponentName.perf.test.tsx`

### Test Structure
Each test file follows this structure:

```typescript
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders, createGuestState } from '../../test-utils';
import ComponentName from '../ComponentName';

describe('ComponentName', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    // Basic rendering tests
  });

  describe('User Interactions', () => {
    // Event handling tests
  });

  describe('State Management', () => {
    // Redux integration tests
  });

  describe('Error Handling', () => {
    // Error boundary and edge cases
  });

  describe('Accessibility', () => {
    // A11y compliance tests
  });
});
```

### Test Coverage Requirements
- **Minimum Coverage**: 80% overall
- **Critical Components**: 95% coverage (Auth, Dashboard, Market Data)
- **Services**: 90% coverage
- **Redux Slices**: 95% coverage

### Mock Strategy
- **External APIs**: Always mocked
- **WebSocket Connections**: Mocked with realistic data
- **Local Storage**: Mocked and cleaned up
- **Timers**: Fake timers for consistent testing

## Test Utilities

### Core Utilities (`test-utils/index.tsx`)
- `renderWithProviders`: Render components with Redux and Router
- `createAuthenticatedState`: Mock authenticated user state
- `createGuestState`: Mock guest user state
- `createMarketStateWithData`: Mock market data state

### Accessibility Utilities (`test-utils/accessibility.ts`)
- `testAccessibility`: Automated a11y testing with axe
- `testKeyboardNavigation`: Focus management verification
- `testAriaAttributes`: ARIA compliance checking
- `runA11yTests`: Comprehensive accessibility test suite

### Data Factories (`test-utils/factories.ts`)
- `createMockUser`: Generate test user data
- `createMockMarketData`: Generate market data
- `createMockNotification`: Generate UI notifications
- `createRelatedTestData`: Generate related test datasets

## Test Execution

### Running Tests
```bash
# Run all tests
npm run test

# Run with coverage
npm run test:coverage

# Run specific test file
npm run test ComponentName.test.tsx

# Run integration tests only
npm run test:integration

# Run accessibility tests
npm run test:a11y
```

### Continuous Integration
- All tests must pass before merge
- Coverage reports generated automatically
- Accessibility violations fail the build
- Performance regression detection

## Best Practices

### Test Writing
1. **Test Behavior, Not Implementation**: Focus on what users experience
2. **Arrange-Act-Assert**: Clear test structure
3. **Descriptive Test Names**: Self-documenting test purpose
4. **Isolated Tests**: No test dependencies
5. **Realistic Mocks**: Mirror production behavior

### Component Testing
1. **Render States**: Loading, error, success, empty states
2. **User Interactions**: Clicks, form submissions, keyboard navigation
3. **Props Variations**: Different prop combinations
4. **Conditional Rendering**: Based on state or props
5. **Integration Points**: Redux, routing, external services

### Service Testing
1. **Happy Path**: Successful operations
2. **Error Scenarios**: Network failures, invalid data
3. **Edge Cases**: Boundary values, null inputs
4. **Retry Logic**: Exponential backoff, max attempts
5. **Cleanup**: Resource disposal, memory management

## Maintenance

### Regular Tasks
- Update test data factories with new fields
- Review and update accessibility standards
- Monitor test execution performance
- Update mock data to match API changes
- Refactor tests for better maintainability

### Code Reviews
- Verify test coverage for new features
- Check accessibility compliance
- Ensure proper cleanup in tests
- Validate mock accuracy
- Review test readability and documentation

## Tools and Libraries

### Testing Framework
- **Vitest**: Fast unit test runner
- **React Testing Library**: Component testing utilities
- **Jest DOM**: Custom Jest matchers
- **User Event**: Realistic user interaction simulation

### Mocking
- **Vitest Mocks**: Built-in mocking capabilities
- **MSW (Mock Service Worker)**: API request mocking
- **Faker.js**: Realistic test data generation

### Accessibility
- **jest-axe**: Automated accessibility testing
- **Testing Library**: Built-in accessibility queries
- **axe-core**: Accessibility engine

### Performance
- **@testing-library/react-profiler**: React performance testing
- **bundlesize**: Bundle size monitoring

This comprehensive test structure ensures reliable, maintainable, and accessible code that meets production quality standards.
