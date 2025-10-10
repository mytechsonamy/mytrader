import 'react-native-gesture-handler/jestSetup';
import mockAsyncStorage from '@react-native-async-storage/async-storage/jest/async-storage-mock';

// Mock AsyncStorage
jest.mock('@react-native-async-storage/async-storage', () => mockAsyncStorage);

// Mock React Native components
jest.mock('react-native/Libraries/Animated/NativeAnimatedHelper');

// Mock React Navigation
jest.mock('@react-navigation/native', () => {
  const actualNav = jest.requireActual('@react-navigation/native');
  return {
    ...actualNav,
    useNavigation: () => ({
      navigate: jest.fn(),
      goBack: jest.fn(),
      reset: jest.fn(),
      setOptions: jest.fn(),
      dispatch: jest.fn(),
      canGoBack: jest.fn(() => true),
      getId: jest.fn(() => 'test-id'),
      getParent: jest.fn(),
      getState: jest.fn(),
      addListener: jest.fn(),
      removeListener: jest.fn(),
      isFocused: jest.fn(() => true),
    }),
    useFocusEffect: jest.fn((callback) => callback()),
    useRoute: () => ({
      key: 'test',
      name: 'test',
      params: {},
    }),
    NavigationContainer: ({ children }) => children,
  };
});

// Mock SignalR
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn(() => ({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    configureLogging: jest.fn().mockReturnThis(),
    build: jest.fn(() => ({
      start: jest.fn(),
      stop: jest.fn(),
      invoke: jest.fn(),
      on: jest.fn(),
      off: jest.fn(),
      onclose: jest.fn(),
      onreconnecting: jest.fn(),
      onreconnected: jest.fn(),
      state: 'Disconnected',
    })),
  })),
  LogLevel: {
    Information: 'Information',
    Error: 'Error',
    Debug: 'Debug',
    Warning: 'Warning',
  },
}));

// Mock Expo modules
jest.mock('expo-constants', () => ({
  expoConfig: {
    extra: {
      API_BASE_URL: 'https://test-api.example.com/api',
      WS_BASE_URL: 'wss://test-ws.example.com/hubs/trading',
    },
  },
}));

jest.mock('expo-linear-gradient', () => ({
  LinearGradient: ({ children }) => children,
}));

jest.mock('expo-status-bar', () => ({
  StatusBar: 'StatusBar',
}));

// Mock React Native components
jest.mock('react-native', () => {
  const actualRN = jest.requireActual('react-native');
  return {
    ...actualRN,
    Alert: {
      alert: jest.fn(),
      prompt: jest.fn(),
    },
    Dimensions: {
      get: jest.fn(() => ({ width: 375, height: 812 })),
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    },
    Platform: {
      ...actualRN.Platform,
      OS: 'ios',
      select: jest.fn(({ ios }) => ios),
    },
    Linking: {
      openURL: jest.fn(),
      canOpenURL: jest.fn(() => Promise.resolve(true)),
      getInitialURL: jest.fn(() => Promise.resolve(null)),
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    },
    Share: {
      share: jest.fn(),
    },
    PermissionsAndroid: {
      request: jest.fn(() => Promise.resolve('granted')),
      check: jest.fn(() => Promise.resolve(true)),
      PERMISSIONS: {
        CAMERA: 'android.permission.CAMERA',
        WRITE_EXTERNAL_STORAGE: 'android.permission.WRITE_EXTERNAL_STORAGE',
      },
      RESULTS: {
        GRANTED: 'granted',
        DENIED: 'denied',
      },
    },
    BackHandler: {
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
      exitApp: jest.fn(),
    },
    Keyboard: {
      addListener: jest.fn(),
      removeListener: jest.fn(),
      removeAllListeners: jest.fn(),
      dismiss: jest.fn(),
    },
    Vibration: {
      vibrate: jest.fn(),
      cancel: jest.fn(),
    },
    NetInfo: {
      fetch: jest.fn(() => Promise.resolve({ isConnected: true })),
      addEventListener: jest.fn(),
      useNetInfo: jest.fn(() => ({ isConnected: true })),
    },
  };
});

// Mock react-native-vector-icons
jest.mock('react-native-vector-icons/MaterialIcons', () => 'Icon');
jest.mock('react-native-vector-icons/Ionicons', () => 'Icon');
jest.mock('react-native-vector-icons/FontAwesome', () => 'Icon');
jest.mock('react-native-vector-icons/Feather', () => 'Icon');

// Mock react-native-chart-kit
jest.mock('react-native-chart-kit', () => ({
  LineChart: 'LineChart',
  PieChart: 'PieChart',
  BarChart: 'BarChart',
  ContributionGraph: 'ContributionGraph',
}));

// Mock react-native-svg
jest.mock('react-native-svg', () => ({
  Svg: 'Svg',
  Circle: 'Circle',
  Path: 'Path',
  G: 'G',
  Text: 'Text',
  Defs: 'Defs',
  LinearGradient: 'LinearGradient',
  Stop: 'Stop',
}));

// Mock gesture handler
jest.mock('react-native-gesture-handler', () => {
  const actualGH = jest.requireActual('react-native-gesture-handler');
  return {
    ...actualGH,
    TouchableOpacity: actualGH.TouchableOpacity || 'TouchableOpacity',
    TouchableHighlight: actualGH.TouchableHighlight || 'TouchableHighlight',
    TouchableWithoutFeedback: actualGH.TouchableWithoutFeedback || 'TouchableWithoutFeedback',
    GestureHandlerRootView: ({ children }) => children,
    Swipeable: ({ children }) => children,
    DrawerLayout: ({ children }) => children,
  };
});

// Mock safe area context
jest.mock('react-native-safe-area-context', () => ({
  SafeAreaProvider: ({ children }) => children,
  SafeAreaView: ({ children }) => children,
  useSafeAreaInsets: () => ({ top: 0, bottom: 0, left: 0, right: 0 }),
}));

// Mock screens
jest.mock('react-native-screens', () => ({
  enableScreens: jest.fn(),
  Screen: ({ children }) => children,
  ScreenContainer: ({ children }) => children,
}));

// Mock reanimated
jest.mock('react-native-reanimated', () => {
  const actualReanimated = jest.requireActual('react-native-reanimated/mock');
  return {
    ...actualReanimated,
    default: {
      ...actualReanimated.default,
      call: jest.fn(),
      Value: jest.fn(() => ({
        setValue: jest.fn(),
        addListener: jest.fn(),
        removeListener: jest.fn(),
        removeAllListeners: jest.fn(),
        stopAnimation: jest.fn(),
        resetAnimation: jest.fn(),
        interpolate: jest.fn(),
      })),
    },
  };
});

// Mock device info
jest.mock('react-native-device-info', () => ({
  getBrand: jest.fn(() => Promise.resolve('Apple')),
  getBuildNumber: jest.fn(() => Promise.resolve('1.0')),
  getBundleId: jest.fn(() => Promise.resolve('com.mytrader.app')),
  getDeviceId: jest.fn(() => Promise.resolve('test-device')),
  getSystemVersion: jest.fn(() => Promise.resolve('15.0')),
  isEmulator: jest.fn(() => Promise.resolve(false)),
  hasNotch: jest.fn(() => Promise.resolve(false)),
}), { virtual: true });

// Mock crash reporting
jest.mock('@react-native-firebase/crashlytics', () => ({
  log: jest.fn(),
  recordError: jest.fn(),
  setAttribute: jest.fn(),
  setUserId: jest.fn(),
  crash: jest.fn(),
}), { virtual: true });

// Mock analytics
jest.mock('@react-native-firebase/analytics', () => ({
  logEvent: jest.fn(),
  setUserId: jest.fn(),
  setUserProperties: jest.fn(),
  logScreenView: jest.fn(),
}), { virtual: true });

// Global test utilities
global.setTimeout = jest.fn((callback, delay) => {
  callback();
  return 1;
});

global.clearTimeout = jest.fn();

global.setInterval = jest.fn((callback, delay) => {
  callback();
  return 1;
});

global.clearInterval = jest.fn();

// Mock performance API
global.performance = {
  now: jest.fn(() => Date.now()),
  mark: jest.fn(),
  measure: jest.fn(),
};

// Mock IntersectionObserver for web compatibility
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
};

// Mock window.matchMedia for web compatibility
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: jest.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: jest.fn(), // Deprecated
    removeListener: jest.fn(), // Deprecated
    addEventListener: jest.fn(),
    removeEventListener: jest.fn(),
    dispatchEvent: jest.fn(),
  })),
});

// Mock requestAnimationFrame
global.requestAnimationFrame = jest.fn(callback => {
  setTimeout(callback, 16);
  return 1;
});

global.cancelAnimationFrame = jest.fn();

// Mock fetch globally
global.fetch = jest.fn(() =>
  Promise.resolve({
    ok: true,
    status: 200,
    json: () => Promise.resolve({}),
    text: () => Promise.resolve(''),
  })
);

// Mock console methods for cleaner test output
const originalError = console.error;
const originalWarn = console.warn;

console.error = (...args) => {
  // Suppress specific React warnings in tests
  if (
    typeof args[0] === 'string' &&
    (args[0].includes('Warning: React.createElement') ||
     args[0].includes('Warning: validateDOMNesting') ||
     args[0].includes('Warning: componentWillMount') ||
     args[0].includes('Warning: componentWillReceiveProps'))
  ) {
    return;
  }
  originalError.apply(console, args);
};

console.warn = (...args) => {
  // Suppress specific warnings in tests
  if (
    typeof args[0] === 'string' &&
    (args[0].includes('Animated:') ||
     args[0].includes('VirtualizedLists should never be nested'))
  ) {
    return;
  }
  originalWarn.apply(console, args);
};

// Set test environment flag
process.env.NODE_ENV = 'test';
process.env.JEST_WORKER_ID = '1';

// Clean up after each test
afterEach(() => {
  jest.clearAllMocks();
  jest.clearAllTimers();
});

// Global error handler for unhandled promise rejections
process.on('unhandledRejection', (reason, promise) => {
  console.error('Unhandled Rejection at:', promise, 'reason:', reason);
});

// Increase test timeout for async operations
jest.setTimeout(10000);