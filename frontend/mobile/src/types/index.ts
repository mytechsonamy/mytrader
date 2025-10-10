export interface User {
  id: string;
  email: string;
  first_name: string;
  last_name: string;
  phone?: string;
  telegram_id?: string;
  is_active: boolean;
  is_email_verified: boolean;
  last_login?: string;
  created_at: string;
}

export interface UserSession {
  accessToken: string;
  refreshToken: string;
  user: User;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  tokenType: string;
  jwtId: string;
  sessionId: string;
}

export interface SymbolData {
  symbol: string;
  display_name: string;
  price: number;
  change?: number;
  signal: SignalType;
  indicators: {
    rsi: number;
    macd: number;
    bB_UPPER: number;
    bB_LOWER: number;
  };
  timestamp: string;
  strategy_type?: string;
}

export interface StrategyConfig {
  name: string;
  description?: string;
  parameters: {
    [key: string]: number | string | boolean;
  };
}

export interface BacktestResult {
  total_return: number;
  sharpe_ratio: number;
  max_drawdown: number;
  win_rate: number;
  total_trades: number;
  start_date: string;
  end_date: string;
  final_portfolio_value: number;
  equity_curve: Array<{
    date: string;
    value: number;
  }>;
}

// Legacy API Response Types (maintained for backward compatibility)
export type LegacyApiResponse<T> = {
  success: boolean;
  data?: T;
  message?: string;
  error?: string;
};

// Legacy WebSocket message types (maintained for backward compatibility)
export type WebSocketMessage =
  | { type: 'symbol_update'; symbol: string; data: SymbolData }
  | { type: 'initial'; data: Record<string, unknown> }
  | { type: 'market'; data: Record<string, SymbolData> }
  | { type: 'price_update'; data: SymbolData }
  | { type: 'signal'; data: { symbol: string; signal: SignalType; timestamp: string } }
  | { type: 'connection_status' | 'error'; message?: string };

// Portfolio Management Types
export interface Portfolio {
  id: string;
  name: string;
  description?: string;
  baseCurrency: string;
  totalValue: number; // Maps to API's currentValue
  totalPnL: number;
  totalPnLPercent: number; // Maps to API's totalReturnPercent
  dailyPnL: number;
  positions: Position[];
  createdAt?: string; // Optional since API doesn't provide
  updatedAt?: string; // Optional since API doesn't provide
  
  // Additional fields from API
  initialCapital?: number;
  cashBalance?: number;
  lastUpdated?: string;
}

export interface Position {
  id: string;
  portfolioId?: string; // Optional since API doesn't provide
  symbol: string;
  symbolType?: AssetClassType; // Enhanced with AssetClassType
  symbolName?: string; // Added from API
  quantity: number;
  averagePrice: number;
  currentPrice: number;
  marketValue: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
  realizedPnL?: number; // Optional
  totalPnL?: number; // Optional
  totalPnLPercent?: number; // Optional
  costBasis?: number; // Added from API
  weight?: number; // Added from API
  allocation?: number; // Optional
  openDate?: string; // Optional
  lastUpdated?: string; // Optional
  lastTradedAt?: string; // Added from API
  // Enhanced multi-asset fields
  assetClassId?: string;
  marketId?: string;
  sector?: string;
  industry?: string;
}

export interface Transaction {
  id: string;
  portfolioId: string;
  symbol: string;
  type: 'BUY' | 'SELL';
  quantity: number;
  price: number;
  amount: number;
  fee: number;
  executedAt: string;
  orderId?: string;
  notes?: string;
}

export interface PortfolioAnalytics {
  portfolioId: string;
  performance: {
    totalReturn: number;
    annualizedReturn: number;
    volatility: number;
    sharpeRatio: number;
    sortinoRatio: number;
    maxDrawdown: number;
    maxDrawdownPercent: number;
    bestDay: number;
    worstDay: number;
    winRate: number;
    profitFactor: number;
  };
  risk: {
    valueAtRisk: number;
    beta: number;
    alpha: number;
    currentDrawdown: number;
  };
  allocation: {
    assetAllocation: Array<{ asset: string; percentage: number; value: number }>;
    sectorAllocation: Array<{ sector: string; percentage: number; value: number }>;
  };
}

export interface ExportRequest {
  portfolioId: string;
  format: 'csv' | 'pdf' | 'json';
  dateFrom: string;
  dateTo: string;
  includeSummary?: boolean;
  includeHoldings?: boolean;
  includeTransactions?: boolean;
  includePerformance?: boolean;
  includeCharts?: boolean;
}

export interface ExportResponse {
  fileName: string;
  contentType: string;
  fileContent: string; // Base64 encoded
  fileSizeBytes: number;
  generatedAt: string;
  downloadUrl: string;
}

// Multi-Asset Types
export interface AssetClassDto {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  isActive: boolean;
  priority: number;
  createdAt: string;
  updatedAt?: string;
}

export interface MarketDto {
  id: string;
  name: string;
  displayName: string;
  assetClassId: string;
  assetClassName?: string;
  timeZone: string;
  openTime: string;
  closeTime: string;
  isActive: boolean;
  tradingDays: string[]; // ['MON', 'TUE', 'WED', 'THU', 'FRI']
  holidays?: string[]; // Array of holiday dates
  currency: string;
  country?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface EnhancedSymbolDto {
  id: string;
  symbol: string;
  displayName: string;
  assetClassId: string;
  assetClassName?: string;
  marketId: string;
  marketName?: string;
  baseCurrency: string;
  quoteCurrency: string;
  isActive: boolean;
  isTracked: boolean;
  minTradeAmount?: number;
  maxTradeAmount?: number;
  priceDecimalPlaces: number;
  quantityDecimalPlaces: number;
  tickSize?: number;
  lotSize?: number;
  description?: string;
  sector?: string;
  industry?: string;
  marketCap?: number;
  tags?: string[];
  createdAt: string;
  updatedAt?: string;
}

export interface UnifiedMarketDataDto {
  symbolId: string;
  symbol: string;
  price: number;
  previousClose?: number;
  change: number;
  changePercent: number;
  volume?: number;
  volumeAverage?: number;
  high?: number;
  low?: number;
  open?: number;
  bid?: number;
  ask?: number;
  bidSize?: number;
  askSize?: number;
  marketCap?: number;
  pe?: number;
  timestamp: string;
  marketStatus: 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET' | 'POST_MARKET' | 'HOLIDAY';
  dataSource: string;
  lastUpdated: string;
  // Alpaca integration fields (optional, backward compatible)
  source?: 'ALPACA' | 'YAHOO_FALLBACK' | 'YAHOO_REALTIME';
  qualityScore?: number;
  isRealtime?: boolean;
  // Market status enhancement fields
  exchange?: 'BIST' | 'NASDAQ' | 'NYSE' | 'CRYPTO' | string;
  lastUpdateTime?: string; // Alias for timestamp for clarity
  nextOpenTime?: string;
  nextCloseTime?: string;
}

export interface MarketStatusDto {
  marketId: string;
  marketName: string;
  status: 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET' | 'POST_MARKET' | 'HOLIDAY';
  nextOpen?: string;
  nextClose?: string;
  timeZone: string;
  currentTime: string;
  tradingDay: string;
  isHoliday: boolean;
  holidayName?: string;
}

export interface MarketOverviewDto {
  totalMarkets: number;
  openMarkets: number;
  closedMarkets: number;
  marketsByAssetClass: Array<{
    assetClass: string;
    totalSymbols: number;
    activeSymbols: number;
    averageChange: number;
  }>;
  topGainers: UnifiedMarketDataDto[];
  topLosers: UnifiedMarketDataDto[];
  mostActive: UnifiedMarketDataDto[];
  lastUpdated: string;
}

export interface TopMoversDto {
  gainers: UnifiedMarketDataDto[];
  losers: UnifiedMarketDataDto[];
  mostActive: UnifiedMarketDataDto[];
  assetClass?: string;
  generatedAt: string;
}

// Leaderboard Types
export interface LeaderboardEntry {
  userId: string;
  userName: string;
  displayName: string;
  rank: number;
  score: number;
  returnPercent: number;
  totalTrades: number;
  winRate: number;
  portfolioValue: number;
  period: 'weekly' | 'monthly' | 'all';
  avatar?: string;
  country?: string;
  tier: 'BRONZE' | 'SILVER' | 'GOLD' | 'PLATINUM' | 'DIAMOND';
  badges: string[];
  joinedAt: string;
}

export interface UserRanking {
  userId: string;
  rank: number;
  score: number;
  returnPercent: number;
  totalParticipants: number;
  percentile: number;
  period: 'weekly' | 'monthly' | 'all';
  isEligible: boolean;
  disqualificationReason?: string;
}

export interface CompetitionStats {
  totalParticipants: number;
  activeParticipants: number;
  totalPrizePool: number;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  leaderboardUpdateFrequency: number; // minutes
  minimumTrades: number;
  minimumPortfolioValue: number;
  eligibilityRequirements: string[];
  prizes: Array<{
    rank: number;
    amount: number;
    currency: string;
    description: string;
  }>;
}

// News Types
export interface NewsItem {
  id: string;
  title: string;
  summary: string;
  content?: string;
  url: string;
  source: string;
  author?: string;
  publishedAt: string;
  category: string;
  tags: string[];
  relatedSymbols: string[];
  sentiment?: 'POSITIVE' | 'NEGATIVE' | 'NEUTRAL';
  importance: 'LOW' | 'MEDIUM' | 'HIGH';
  imageUrl?: string;
  language: string;
}

// Pagination and API Response Types
export interface PagedResponse<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, any>;
  timestamp: string;
}

// Enhanced API Response Types
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  error?: ApiError;
  metadata?: {
    requestId: string;
    timestamp: string;
    source: string;
    cached?: boolean;
    ttl?: number;
  };
}

// WebSocket Subscription Types
export interface SubscriptionRequest {
  action: 'subscribe' | 'unsubscribe';
  subscriptionType: 'prices' | 'market_status' | 'news' | 'signals';
  symbols?: string[];
  assetClasses?: string[];
  markets?: string[];
  filters?: Record<string, any>;
}

export interface WebSocketConnectionState {
  isConnected: boolean;
  connectionId?: string;
  lastConnected?: string;
  reconnectAttempts: number;
  subscriptions: SubscriptionRequest[];
}

// Enhanced WebSocket Message Types
export type EnhancedWebSocketMessage =
  | { type: 'price_update'; data: UnifiedMarketDataDto }
  | { type: 'batch_price_update'; data: UnifiedMarketDataDto[] }
  | { type: 'market_status_update'; data: MarketStatusDto }
  | { type: 'news_update'; data: NewsItem }
  | { type: 'subscription_confirmed'; data: { subscriptionId: string; type: string } }
  | { type: 'subscription_error'; data: { error: string; subscriptionType: string } }
  | { type: 'connection_status'; data: { status: string; message?: string } }
  | { type: 'heartbeat'; data: { timestamp: string } }
  // Legacy compatibility
  | { type: 'symbol_update'; symbol: string; data: SymbolData }
  | { type: 'initial'; data: Record<string, unknown> }
  | { type: 'market'; data: Record<string, SymbolData> }
  | { type: 'signal'; data: { symbol: string; signal: SignalType; timestamp: string } }
  | { type: 'error'; message?: string };

// Cache Configuration
export interface CacheConfig {
  ttl: number; // milliseconds
  maxSize: number;
  strategy: 'LRU' | 'FIFO' | 'TTL';
}

export interface CacheEntry<T> {
  data: T;
  timestamp: number;
  ttl: number;
  accessCount: number;
  lastAccessed: number;
}

// Request Configuration
export interface RequestConfig {
  timeout?: number;
  retries?: number;
  retryDelay?: number;
  cacheKey?: string;
  cacheTtl?: number;
  priority?: 'LOW' | 'NORMAL' | 'HIGH';
  abortSignal?: AbortSignal;
}

// Error Handling Types
export interface ErrorContext {
  operation: string;
  endpoint: string;
  requestId?: string;
  userId?: string;
  timestamp: string;
  userAgent?: string;
  networkStatus?: 'online' | 'offline';
}

export interface ErrorRecoveryStrategy {
  maxRetries: number;
  backoffMultiplier: number;
  jitterRange: [number, number];
  retryableStatusCodes: number[];
  fallbackEnabled: boolean;
}

// Helper Types
export type SignalType = 'BUY' | 'SELL' | 'NEUTRAL';
export type LoadingState = 'idle' | 'loading' | 'success' | 'error';
export type AssetClassType = 'CRYPTO' | 'STOCK' | 'FOREX' | 'COMMODITY' | 'INDEX';
export type MarketStatus = 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET' | 'POST_MARKET' | 'HOLIDAY';
export type TimeRange = '1D' | '7D' | '1M' | '3M' | '6M' | '1Y' | 'ALL';
export type SortDirection = 'ASC' | 'DESC';
export type PriceDataSource = 'REAL_TIME' | 'DELAYED' | 'HISTORICAL' | 'SIMULATED';
export type DataSourceType = 'ALPACA' | 'YAHOO_FALLBACK' | 'YAHOO_REALTIME';

// Common Component Props
export interface BaseScreenProps {
  loading?: boolean;
  error?: string | null;
}

export type RootStackParamList = {
  MainTabs: { screen?: string } | undefined;
  AuthStack: { screen?: string; params?: any } | undefined;
  StrategyTest: {
    symbol: string;
    displayName: string;
    assetClass?: AssetClassType;
    templateId?: string;
    strategyName?: string;
    bestFor?: string;
    defaultParameters?: {
      bb_period: string;
      bb_std: string;
      macd_fast: string;
      macd_slow: string;
      macd_signal: string;
      rsi_period: string;
      rsi_overbought: string;
      rsi_oversold: string;
    };
  };
  News?: undefined;
};

export type MainTabParamList = {
  Dashboard: undefined;
  Portfolio: undefined;
  Strategies: undefined;
  Gamification: undefined;
  Profile: undefined;
  Test?: undefined; // Temporary test screen for error boundaries - REMOVE AFTER TESTING
};

// Backward-compatible alias
export type MainTabsParamList = MainTabParamList;

export type AuthStackParamList = {
  Login: { fromPasswordReset?: boolean; returnTo?: string } | undefined;
  Register: undefined;
  ForgotPasswordStart: undefined;
  ForgotPasswordVerify: { email: string };
  ResetPassword: { email: string };
};
