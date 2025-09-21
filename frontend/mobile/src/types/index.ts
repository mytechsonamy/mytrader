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

// API Response Types
export type ApiResponse<T> = {
  success: boolean;
  data?: T;
  message?: string;
  error?: string;
};

// WebSocket message types
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
  totalValue: number;
  totalPnL: number;
  totalPnLPercent: number;
  dailyPnL: number;
  positions: Position[];
  createdAt: string;
  updatedAt: string;
}

export interface Position {
  id: string;
  portfolioId: string;
  symbol: string;
  symbolType: 'CRYPTO' | 'STOCK' | 'FOREX' | 'COMMODITY';
  quantity: number;
  averagePrice: number;
  currentPrice: number;
  marketValue: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
  realizedPnL: number;
  totalPnL: number;
  totalPnLPercent: number;
  allocation: number;
  openDate: string;
  lastUpdated: string;
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

// Helper Types
export type SignalType = 'BUY' | 'SELL' | 'NEUTRAL';
export type LoadingState = 'idle' | 'loading' | 'success' | 'error';

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
  };
  News?: undefined;
};

export type MainTabParamList = {
  Dashboard: undefined;
  Portfolio: undefined;
  News: undefined;
  Strategies: undefined;
  Gamification: undefined;
  Alarms: undefined;
  Education: undefined;
  Profile: undefined;
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
