import 'react-native-gesture-handler/jestSetup';
import '@testing-library/jest-native/extend-expect';
import { jest } from '@jest/globals';

// Mock React Navigation
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    navigate: jest.fn(),
    goBack: jest.fn(),
    reset: jest.fn(),
    setOptions: jest.fn(),
    addListener: jest.fn(() => jest.fn()),
  }),
  useRoute: () => ({
    params: {},
    key: 'test-route-key',
    name: 'test-route-name',
  }),
  useFocusEffect: jest.fn(),
  useIsFocused: () => true,
  NavigationContainer: ({ children }: { children: React.ReactNode }) => children,
}));

// Mock React Navigation Stack
jest.mock('@react-navigation/stack', () => ({
  createStackNavigator: () => ({
    Navigator: ({ children }: { children: React.ReactNode }) => children,
    Screen: ({ children }: { children: React.ReactNode }) => children,
  }),
  TransitionPresets: {
    SlideFromRightIOS: {},
    ModalSlideFromBottomIOS: {},
  },
}));

// Mock React Navigation Bottom Tabs
jest.mock('@react-navigation/bottom-tabs', () => ({
  createBottomTabNavigator: () => ({
    Navigator: ({ children }: { children: React.ReactNode }) => children,
    Screen: ({ children }: { children: React.ReactNode }) => children,
  }),
}));

// Mock AsyncStorage
jest.mock('@react-native-async-storage/async-storage', () => ({
  getItem: jest.fn(() => Promise.resolve(null)),
  setItem: jest.fn(() => Promise.resolve()),
  removeItem: jest.fn(() => Promise.resolve()),
  clear: jest.fn(() => Promise.resolve()),
  getAllKeys: jest.fn(() => Promise.resolve([])),
  multiGet: jest.fn(() => Promise.resolve([])),
  multiSet: jest.fn(() => Promise.resolve()),
  multiRemove: jest.fn(() => Promise.resolve()),
}));

// Mock Expo Status Bar
jest.mock('expo-status-bar', () => ({
  StatusBar: 'StatusBar',
}));

// Mock Expo Linear Gradient
jest.mock('expo-linear-gradient', () => ({
  LinearGradient: 'LinearGradient',
}));

// Mock React Native Vector Icons
jest.mock('react-native-vector-icons/Ionicons', () => 'Icon');
jest.mock('react-native-vector-icons/MaterialIcons', () => 'Icon');
jest.mock('react-native-vector-icons/Feather', () => 'Icon');

// Mock React Native Safe Area Context
jest.mock('react-native-safe-area-context', () => ({
  SafeAreaProvider: ({ children }: { children: React.ReactNode }) => children,
  SafeAreaView: ({ children }: { children: React.ReactNode }) => children,
  useSafeAreaInsets: () => ({ top: 0, bottom: 0, left: 0, right: 0 }),
  useSafeAreaFrame: () => ({ width: 375, height: 812, x: 0, y: 0 }),
}));

// Mock React Native Gesture Handler
jest.mock('react-native-gesture-handler', () => {
  const View = require('react-native/Libraries/Components/View/View');
  return {
    Swipeable: View,
    DrawerLayout: View,
    State: {
      BEGAN: 'BEGAN',
      FAILED: 'FAILED',
      CANCELLED: 'CANCELLED',
      ACTIVE: 'ACTIVE',
      END: 'END',
    },
    PanGestureHandler: View,
    TapGestureHandler: View,
    LongPressGestureHandler: View,
    ScrollView: require('react-native/Libraries/Components/ScrollView/ScrollView'),
    Slider: View,
    Switch: View,
    TextInput: require('react-native/Libraries/Components/TextInput/TextInput'),
    ToolbarAndroid: View,
    ViewPagerAndroid: View,
    DrawerLayoutAndroid: View,
    WebView: View,
    NativeViewGestureHandler: View,
    TapGestureHandler: View,
    FlingGestureHandler: View,
    ForceTouchGestureHandler: View,
    LongPressGestureHandler: View,
    PanGestureHandler: View,
    PinchGestureHandler: View,
    RotationGestureHandler: View,
    RawButton: View,
    BaseButton: View,
    RectButton: View,
    BorderlessButton: View,
    FlatList: require('react-native/Libraries/Lists/FlatList'),
    gestureHandlerRootHOC: (component: any) => component,
    Directions: {
      DOWN: 1,
      LEFT: 2,
      RIGHT: 4,
      UP: 8,
    },
  };
});

// Mock React Native Chart Kit
jest.mock('react-native-chart-kit', () => ({
  LineChart: 'LineChart',
  BarChart: 'BarChart',
  PieChart: 'PieChart',
  ProgressChart: 'ProgressChart',
  ContributionGraph: 'ContributionGraph',
  StackedBarChart: 'StackedBarChart',
}));

// Mock React Native SVG
jest.mock('react-native-svg', () => ({
  Svg: 'Svg',
  Circle: 'Circle',
  Ellipse: 'Ellipse',
  G: 'G',
  Text: 'Text',
  TSpan: 'TSpan',
  TextPath: 'TextPath',
  Path: 'Path',
  Polygon: 'Polygon',
  Polyline: 'Polyline',
  Line: 'Line',
  Rect: 'Rect',
  Use: 'Use',
  Image: 'Image',
  Symbol: 'Symbol',
  Defs: 'Defs',
  LinearGradient: 'LinearGradient',
  RadialGradient: 'RadialGradient',
  Stop: 'Stop',
  ClipPath: 'ClipPath',
  Pattern: 'Pattern',
  Mask: 'Mask',
}));

// Mock SignalR
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn(() => ({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    build: jest.fn(() => ({
      start: jest.fn().mockResolvedValue(undefined),
      stop: jest.fn().mockResolvedValue(undefined),
      on: jest.fn(),
      off: jest.fn(),
      invoke: jest.fn().mockResolvedValue(undefined),
      send: jest.fn().mockResolvedValue(undefined),
      state: 'Connected',
      connectionId: 'mock-connection-id',
    })),
  })),
  HubConnectionState: {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting',
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting',
  },
}));

// Mock React Native Reanimated (commented out - not installed)
// jest.mock('react-native-reanimated', () => {
//   const Reanimated = require('react-native-reanimated/mock');
//   Reanimated.default.call = () => {};
//   return Reanimated;
// });

// Mock Dimensions
const mockDimensions = {
  window: {
    width: 375,
    height: 812,
    scale: 2,
    fontScale: 1,
  },
  screen: {
    width: 375,
    height: 812,
    scale: 2,
    fontScale: 1,
  },
};

jest.mock('react-native/Libraries/Utilities/Dimensions', () => ({
  get: jest.fn((dimension) => mockDimensions[dimension as keyof typeof mockDimensions]),
  addEventListener: jest.fn(),
  removeEventListener: jest.fn(),
}));

// Mock Platform
jest.mock('react-native/Libraries/Utilities/Platform', () => ({
  OS: 'ios',
  select: (options: { ios?: any; android?: any; default?: any }) => options.ios || options.default,
  Version: '14.0',
}));

// Mock Alert
jest.mock('react-native/Libraries/Alert/Alert', () => ({
  alert: jest.fn(),
  prompt: jest.fn(),
}));

// Mock Keyboard
jest.mock('react-native/Libraries/Components/Keyboard/Keyboard', () => ({
  addListener: jest.fn(() => ({ remove: jest.fn() })),
  removeListener: jest.fn(),
  removeAllListeners: jest.fn(),
  dismiss: jest.fn(),
}));

// Mock BackHandler
jest.mock('react-native/Libraries/Utilities/BackHandler', () => ({
  addEventListener: jest.fn(() => ({ remove: jest.fn() })),
  removeEventListener: jest.fn(),
}));

// Mock NetInfo (commented out - not installed)
// jest.mock('@react-native-netinfo/netinfo', () => ({
//   fetch: jest.fn(() => Promise.resolve({ isConnected: true, type: 'wifi' })),
//   addEventListener: jest.fn(() => jest.fn()),
// }));

// Mock Haptics (commented out - not installed)
// jest.mock('expo-haptics', () => ({
//   impactAsync: jest.fn(),
//   notificationAsync: jest.fn(),
//   selectionAsync: jest.fn(),
// }));

// Mock Share
jest.mock('react-native/Libraries/Share/Share', () => ({
  share: jest.fn().mockResolvedValue({ action: 'sharedAction' }),
}));

// Mock Linking
jest.mock('react-native/Libraries/Linking/Linking', () => ({
  openURL: jest.fn().mockResolvedValue(undefined),
  canOpenURL: jest.fn().mockResolvedValue(true),
  addEventListener: jest.fn(),
  removeEventListener: jest.fn(),
  getInitialURL: jest.fn().mockResolvedValue(null),
}));

// Mock DeviceInfo (commented out - not installed)
// jest.mock('react-native-device-info', () => ({
//   getVersion: jest.fn(() => '1.0.0'),
//   getBuildNumber: jest.fn(() => '1'),
//   getSystemName: jest.fn(() => 'iOS'),
//   getSystemVersion: jest.fn(() => '14.0'),
//   getModel: jest.fn(() => 'iPhone'),
//   getDeviceId: jest.fn(() => 'iPhone13,2'),
//   getUniqueId: jest.fn(() => 'unique-device-id'),
//   isTablet: jest.fn(() => Promise.resolve(false)),
// }));

// Console setup for tests
const originalWarn = console.warn;
console.warn = (...args) => {
  const message = args[0];
  if (
    typeof message === 'string' &&
    (
      message.includes('ReactNativeFiberHostComponent') ||
      message.includes('VirtualizedLists should never be nested') ||
      message.includes('componentWillReceiveProps') ||
      message.includes('componentWillUpdate')
    )
  ) {
    return;
  }
  originalWarn.call(console, ...args);
};

// Global error handler for tests
const originalError = console.error;
console.error = (...args) => {
  const message = args[0];
  if (
    typeof message === 'string' &&
    (
      message.includes('Warning: ReactDOM.render is no longer supported') ||
      message.includes('Warning: findDOMNode is deprecated')
    )
  ) {
    return;
  }
  originalError.call(console, ...args);
};

// Setup global test environment
global.__TEST_ENV__ = true;
global.__DEV__ = false;

// Mock timers
jest.useFakeTimers();

// Increase timeout for async operations
jest.setTimeout(10000);

console.log('React Native test environment setup completed');

// Export commonly used mocks for test files
export const mockNavigation = {
  navigate: jest.fn(),
  goBack: jest.fn(),
  reset: jest.fn(),
  setOptions: jest.fn(),
  addListener: jest.fn(() => jest.fn()),
};

export const mockRoute = {
  params: {},
  key: 'test-route-key',
  name: 'test-route-name',
};

export const mockAsyncStorage = {
  getItem: jest.fn(() => Promise.resolve(null)),
  setItem: jest.fn(() => Promise.resolve()),
  removeItem: jest.fn(() => Promise.resolve()),
  clear: jest.fn(() => Promise.resolve()),
};