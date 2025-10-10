import { faker } from '@faker-js/faker';
import { AuthState } from '../store/slices/authSlice';
import { MarketState } from '../store/slices/marketSlice';
import { MarketDataDto } from '../services/marketDataService';

/**
 * Test data factories for consistent test data generation
 */

// User-related factories
export const createMockUser = (overrides: Partial<any> = {}) => ({
  id: faker.string.uuid(),
  email: faker.internet.email(),
  firstName: faker.person.firstName(),
  lastName: faker.person.lastName(),
  phone: faker.phone.number(),
  isActive: true,
  isEmailVerified: true,
  createdAt: faker.date.past().toISOString(),
  updatedAt: faker.date.recent().toISOString(),
  lastLogin: faker.date.recent().toISOString(),
  plan: faker.helpers.arrayElement(['basic', 'pro', 'premium']),
  ...overrides,
});

export const createMockSessionResponse = (overrides: Partial<any> = {}) => ({
  accessToken: faker.string.alphanumeric(64),
  refreshToken: faker.string.alphanumeric(64),
  user: createMockUser(),
  accessTokenExpiresAt: faker.date.future().toISOString(),
  refreshTokenExpiresAt: faker.date.future({ years: 1 }).toISOString(),
  tokenType: 'Bearer',
  jwtId: faker.string.uuid(),
  sessionId: faker.string.uuid(),
  ...overrides,
});

// Market data factories
export const createMockMarketData = (overrides: Partial<MarketDataDto> = {}): MarketDataDto => ({
  symbol: faker.helpers.arrayElement(['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'DOTUSDT', 'LINKUSDT']),
  displayName: faker.company.name(),
  price: faker.number.float({ min: 0.01, max: 100000, fractionDigits: 2 }),
  priceChange: faker.number.float({ min: -1000, max: 1000, fractionDigits: 2 }),
  priceChangePercent: faker.number.float({ min: -50, max: 50, fractionDigits: 2 }),
  volume: faker.number.float({ min: 0, max: 10000000, fractionDigits: 0 }),
  high24h: faker.number.float({ min: 100, max: 101000, fractionDigits: 2 }),
  low24h: faker.number.float({ min: 50, max: 99000, fractionDigits: 2 }),
  marketCap: faker.number.float({ min: 1000000, max: 1000000000000, fractionDigits: 0 }),
  lastUpdate: faker.date.recent().toISOString(),
  source: faker.helpers.arrayElement(['alpaca', 'yahoo', 'binance']),
  isRealTime: faker.datatype.boolean(),
  ...overrides,
});

export const createMockSymbolsResponse = (symbolCount: number = 5) => {
  const symbols: Record<string, any> = {};
  
  for (let i = 0; i < symbolCount; i++) {
    const symbol = faker.helpers.arrayElement([
      'BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'DOTUSDT', 'LINKUSDT',
      'AAPL', 'GOOGL', 'MSFT', 'AMZN', 'TSLA'
    ]);
    
    symbols[symbol] = {
      symbol,
      display_name: `${symbol.slice(0, 3)} / ${symbol.slice(3) || 'USD'}`,
      precision: faker.number.int({ min: 0, max: 8 }),
      strategy_type: faker.helpers.arrayElement(['crypto', 'stock', 'forex']),
    };
  }
  
  return {
    symbols,
    interval: faker.helpers.arrayElement(['1m', '5m', '15m', '1h', '1d']),
  };
};

export const createMockLeaderboardUser = (overrides: Partial<any> = {}) => ({
  id: faker.string.uuid(),
  name: faker.person.fullName(),
  totalTrades: faker.number.int({ min: 0, max: 500 }),
  winRate: faker.number.float({ min: 0, max: 100, fractionDigits: 1 }),
  totalReturn: faker.number.float({ min: -50, max: 200, fractionDigits: 1 }),
  totalValue: faker.number.float({ min: 1000, max: 1000000, fractionDigits: 0 }),
  rank: faker.number.int({ min: 1, max: 100 }),
  badge: faker.helpers.arrayElement(['gold', 'silver', 'bronze', undefined]),
  ...overrides,
});

export const createMockNewsArticle = (overrides: Partial<any> = {}) => ({
  id: faker.string.uuid(),
  title: faker.lorem.sentence(),
  summary: faker.lorem.paragraph(),
  source: faker.helpers.arrayElement(['Reuters', 'Bloomberg', 'CoinDesk', 'Financial Times']),
  publishedAt: faker.date.recent({ days: 7 }).toISOString(),
  url: faker.internet.url(),
  category: faker.helpers.arrayElement(['market', 'crypto', 'stocks', 'economy']),
  tags: faker.helpers.arrayElements(['market', 'crypto', 'stocks', 'tech', 'finance'], { min: 1, max: 3 }),
  imageUrl: faker.image.url(),
  ...overrides,
});

// Notification factories
export const createMockNotification = (overrides: Partial<any> = {}) => ({
  id: faker.string.uuid(),
  type: faker.helpers.arrayElement(['success', 'error', 'warning', 'info']),
  message: faker.lorem.sentence(),
  autoClose: faker.datatype.boolean(),
  timestamp: faker.date.recent().toISOString(),
  ...overrides,
});

// State factories
export const createMockAuthState = (overrides: Partial<AuthState> = {}): AuthState => ({
  user: null,
  token: null,
  refreshToken: null,
  sessionId: null,
  isAuthenticated: false,
  isGuest: true,
  isLoading: false,
  error: null,
  ...overrides,
});

export const createAuthenticatedAuthState = (userOverrides: Partial<any> = {}): AuthState => ({
  user: createMockUser(userOverrides),
  token: faker.string.alphanumeric(64),
  refreshToken: faker.string.alphanumeric(64),
  sessionId: faker.string.uuid(),
  isAuthenticated: true,
  isGuest: false,
  isLoading: false,
  error: null,
});

export const createMockMarketState = (overrides: Partial<MarketState> = {}): MarketState => {
  const symbols = createMockSymbolsResponse();
  const marketData: Record<string, MarketDataDto> = {};
  
  Object.keys(symbols.symbols).forEach(symbol => {
    marketData[symbol] = createMockMarketData({ symbol });
  });
  
  return {
    symbols,
    marketData,
    selectedSymbols: Object.keys(symbols.symbols).slice(0, 3),
    isLoading: false,
    error: null,
    lastUpdate: faker.date.recent().toISOString(),
    ...overrides,
  };
};

export const createMockUIState = (overrides: Partial<any> = {}) => ({
  theme: faker.helpers.arrayElement(['light', 'dark']),
  sidebarOpen: faker.datatype.boolean(),
  loading: false,
  notifications: Array.from({ length: faker.number.int({ min: 0, max: 3 }) }, () => createMockNotification()),
  ...overrides,
});

// Complete app state factory
export const createMockAppState = (overrides: {
  auth?: Partial<AuthState>;
  market?: Partial<MarketState>;
  ui?: Partial<any>;
} = {}) => ({
  auth: createMockAuthState(overrides.auth),
  market: createMockMarketState(overrides.market),
  ui: createMockUIState(overrides.ui),
});

export const createAuthenticatedAppState = (overrides: {
  auth?: Partial<AuthState>;
  market?: Partial<MarketState>;
  ui?: Partial<any>;
  userOverrides?: Partial<any>;
} = {}) => ({
  auth: createAuthenticatedAuthState(overrides.userOverrides),
  market: createMockMarketState(overrides.market),
  ui: createMockUIState(overrides.ui),
});

// API response factories
export const createMockApiError = (overrides: Partial<any> = {}) => ({
  message: faker.lorem.sentence(),
  status: faker.helpers.arrayElement([400, 401, 403, 404, 500, 503]),
  code: faker.string.alphanumeric(10).toUpperCase(),
  details: faker.lorem.paragraph(),
  timestamp: faker.date.recent().toISOString(),
  ...overrides,
});

export const createMockApiResponse = <T>(data: T, overrides: Partial<any> = {}) => ({
  data,
  success: true,
  message: 'Operation successful',
  timestamp: faker.date.recent().toISOString(),
  ...overrides,
});

// WebSocket message factories
export const createMockWebSocketMessage = (overrides: Partial<any> = {}) => ({
  Symbol: faker.helpers.arrayElement(['BTCUSDT', 'ETHUSDT', 'ADAUSDT']),
  Price: faker.number.float({ min: 0.01, max: 100000, fractionDigits: 2 }),
  Change24h: faker.number.float({ min: -50, max: 50, fractionDigits: 2 }),
  Volume: faker.number.float({ min: 0, max: 10000000, fractionDigits: 0 }),
  Timestamp: faker.date.recent().toISOString(),
  Source: faker.helpers.arrayElement(['binance', 'alpaca']),
  ...overrides,
});

export const createMockLegacyCryptoMessage = (overrides: Partial<any> = {}) => ({
  symbol: faker.helpers.arrayElement(['BTCUSDT', 'ETHUSDT', 'ADAUSDT']),
  price: faker.number.float({ min: 0.01, max: 100000, fractionDigits: 2 }),
  changePercent: faker.number.float({ min: -50, max: 50, fractionDigits: 2 }),
  volume: faker.number.float({ min: 0, max: 10000000, fractionDigits: 0 }),
  timestamp: faker.date.recent().toISOString(),
  ...overrides,
});

// Portfolio and trading factories
export const createMockPortfolioItem = (overrides: Partial<any> = {}) => ({
  id: faker.string.uuid(),
  symbol: faker.helpers.arrayElement(['BTCUSDT', 'ETHUSDT', 'AAPL', 'GOOGL']),
  quantity: faker.number.float({ min: 0, max: 100, fractionDigits: 8 }),
  averagePrice: faker.number.float({ min: 0.01, max: 1000, fractionDigits: 2 }),
  currentPrice: faker.number.float({ min: 0.01, max: 1000, fractionDigits: 2 }),
  totalValue: faker.number.float({ min: 0, max: 100000, fractionDigits: 2 }),
  unrealizedPnL: faker.number.float({ min: -10000, max: 10000, fractionDigits: 2 }),
  unrealizedPnLPercent: faker.number.float({ min: -100, max: 200, fractionDigits: 2 }),
  lastUpdate: faker.date.recent().toISOString(),
  ...overrides,
});

export const createMockTrade = (overrides: Partial<any> = {}) => ({
  id: faker.string.uuid(),
  symbol: faker.helpers.arrayElement(['BTCUSDT', 'ETHUSDT', 'AAPL', 'GOOGL']),
  type: faker.helpers.arrayElement(['buy', 'sell']),
  quantity: faker.number.float({ min: 0.001, max: 100, fractionDigits: 8 }),
  price: faker.number.float({ min: 0.01, max: 1000, fractionDigits: 2 }),
  totalValue: faker.number.float({ min: 1, max: 100000, fractionDigits: 2 }),
  fee: faker.number.float({ min: 0, max: 100, fractionDigits: 2 }),
  status: faker.helpers.arrayElement(['pending', 'completed', 'cancelled', 'failed']),
  executedAt: faker.date.recent().toISOString(),
  createdAt: faker.date.recent().toISOString(),
  ...overrides,
});

// Utility functions for arrays
export const createMockArray = <T>(factory: () => T, count?: number): T[] => {
  const actualCount = count ?? faker.number.int({ min: 1, max: 10 });
  return Array.from({ length: actualCount }, factory);
};

// Reset faker seed for consistent test data
export const setSeed = (seed: number) => {
  faker.seed(seed);
};

// Generate test data with relationships
export const createRelatedTestData = () => {
  const user = createMockUser();
  const portfolio = createMockArray(() => createMockPortfolioItem());
  const trades = createMockArray(() => createMockTrade());
  const notifications = createMockArray(() => createMockNotification());
  
  return {
    user,
    portfolio,
    trades,
    notifications,
    session: createMockSessionResponse({ user }),
    marketData: createMockArray(() => createMockMarketData()),
    news: createMockArray(() => createMockNewsArticle()),
    leaderboard: createMockArray(() => createMockLeaderboardUser()),
  };
};
