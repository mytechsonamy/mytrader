module.exports = {
  preset: 'react-native',
  
  // Test Environment
  testEnvironment: 'node',
  
  // Setup Files
  setupFilesAfterEnv: [
    '@testing-library/jest-native/extend-expect',
    '<rootDir>/src/test-utils/setup.ts'
  ],
  
  // Module Name Mapper (correct property name)
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    '^@components/(.*)$': '<rootDir>/src/components/$1',
    '^@screens/(.*)$': '<rootDir>/src/screens/$1',
    '^@services/(.*)$': '<rootDir>/src/services/$1',
    '^@utils/(.*)$': '<rootDir>/src/utils/$1',
    '^@test-utils/(.*)$': '<rootDir>/src/test-utils/$1'
  },
  
  // Transform Configuration
  transform: {
    '^.+\\.(js|jsx|ts|tsx)$': 'babel-jest'
  },
  
  // Transform Ignore Patterns
  transformIgnorePatterns: [
    'node_modules/(?!(react-native|@react-native|@react-navigation|expo|@expo|react-native-vector-icons|react-native-svg|react-native-chart-kit|@react-native-async-storage|react-native-gesture-handler|react-native-screens|react-native-safe-area-context|@microsoft/signalr)/)',
  ],
  
  // Module File Extensions
  moduleFileExtensions: [
    'ts',
    'tsx',
    'js',
    'jsx',
    'json',
    'node'
  ],
  
  // Test Match Patterns
  testMatch: [
    '<rootDir>/src/**/__tests__/**/*.{js,jsx,ts,tsx}',
    '<rootDir>/src/**/*.(test|spec).{js,jsx,ts,tsx}'
  ],
  
  // Test Path Ignore Patterns
  testPathIgnorePatterns: [
    '<rootDir>/node_modules/',
    '<rootDir>/ios/',
    '<rootDir>/android/',
    '<rootDir>/.expo/'
  ],
  
  // Coverage Configuration
  collectCoverage: true,
  collectCoverageFrom: [
    'src/**/*.{js,jsx,ts,tsx}',
    '!src/**/*.d.ts',
    '!src/**/index.{js,jsx,ts,tsx}',
    '!src/**/__tests__/**',
    '!src/**/*.test.{js,jsx,ts,tsx}',
    '!src/**/*.spec.{js,jsx,ts,tsx}',
    '!src/test-utils/**'
  ],
  
  coverageDirectory: 'coverage',
  
  coverageReporters: [
    'text',
    'lcov',
    'html',
    'json'
  ],
  
  // Coverage Thresholds
  coverageThreshold: {
    global: {
      branches: 70,
      functions: 75,
      lines: 75,
      statements: 75
    },
    // Critical screens should have higher coverage
    './src/screens/Dashboard*.tsx': {
      branches: 80,
      functions: 85,
      lines: 80,
      statements: 80
    },
    './src/screens/EnhancedLeaderboard*.tsx': {
      branches: 80,
      functions: 85,
      lines: 80,
      statements: 80
    },
    './src/services/**': {
      branches: 85,
      functions: 90,
      lines: 85,
      statements: 85
    },
    './src/components/**': {
      branches: 75,
      functions: 80,
      lines: 75,
      statements: 75
    }
  },
  
  // Test Environment Variables
  globals: {
    __TEST_ENV__: true,
    __DEV__: false
  },
  
  // Clear Mocks
  clearMocks: true,
  restoreMocks: true,
  
  // Test Timeout
  testTimeout: 10000,
  
  // Reporters (simplified - removed optional reporters)
  reporters: ['default'],
  
  // Verbose Output
  verbose: true,
  
  // Error Handling
  errorOnDeprecated: true,
  
  // Mock Configuration
  automock: false,
  
  // Cache
  cache: true,
  cacheDirectory: '<rootDir>/node_modules/.cache/jest',
  
  // Watch Plugins (commented out - optional dependencies)
  // watchPlugins: [
  //   'jest-watch-typeahead/filename',
  //   'jest-watch-typeahead/testname'
  // ],
  
  // Max Workers (for CI environments)
  maxWorkers: process.env.CI ? 1 : '50%',
  
  // Snapshot Serializers (commented out - enzyme not used)
  // snapshotSerializers: [
  //   'enzyme-to-json/serializer'
  // ],
  
  // Module Directories
  moduleDirectories: [
    'node_modules',
    '<rootDir>/src'
  ],
  
};