/**
 * Core type definitions for MyTrader application
 */

// User and Authentication Types
export interface User {
  id: string;
  email: string;
  username: string;
  firstName?: string;
  lastName?: string;
  avatar?: string;
  isEmailVerified: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isGuest: boolean;
  error: string | null;
  token: string | null;
  refreshToken: string | null;
}

export interface LoginCredentials {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterData {
  email: string;
  password: string;
  confirmPassword: string;
  username: string;
  firstName?: string;
  lastName?: string;
  acceptTerms: boolean;
}

// Market Data Types
export interface Symbol {
  id: string;
  symbol: string;
  name: string;  // Primary display name
  displayName?: string;  // Alternative display name from API
  assetClass?: AssetClass | string;  // Can be AssetClass object or string like "STOCK", "CRYPTO"
  assetClassId?: string;  // Asset class ID
  assetClassName?: string;  // Asset class name string
  market: string;  // Primary market field (preferred)
  marketId?: string;  // Market ID
  marketName?: string;  // Fallback market field from API
  venue?: string;  // Alternative market field
  fullName?: string;  // Full company/asset name
  baseCurrency?: string;  // For crypto pairs
  quoteCurrency?: string;  // For crypto pairs
  precision?: number;  // Price precision
  priceDecimalPlaces?: number;  // Alternative precision field
  quantityDecimalPlaces?: number;  // Quantity precision
  strategy_type?: string;  // Strategy type for this symbol
  isActive: boolean;
  isTracked?: boolean;  // Whether user is tracking this symbol
  tickSize?: number;  // Minimum price increment
  lotSize?: number;  // Minimum quantity increment
  minTradeAmount?: number;  // Minimum trade amount
  maxTradeAmount?: number;  // Maximum trade amount
  description?: string;  // Symbol description
  sector?: string;  // Market sector
  industry?: string;  // Industry classification
  metadata?: Record<string, any>;
}

export interface AssetClass {
  id: string;
  name: string;
  displayName: string;
  icon: string;
  isActive: boolean;
  sortOrder: number;
}

export interface MarketData {
  symbolId: string;
  symbol: string;
  name: string;
  price: number;
  previousClose: number;
  change: number;
  changePercent: number;
  volume: number;
  high: number;
  low: number;
  open: number;
  marketCap?: number;
  timestamp: string;
  marketStatus: MarketStatus;
}

export type MarketStatus = 'OPEN' | 'CLOSED' | 'PRE_MARKET' | 'AFTER_MARKET' | 'UNKNOWN';

export interface HistoricalData {
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface TopMover {
  symbol: string;
  name: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  assetClass: string;
}

// Portfolio Types
export interface Portfolio {
  id: string;
  userId: string;
  name: string;
  totalValue: number;
  totalCost: number;
  totalGain: number;
  totalGainPercent: number;
  cash: number;
  positions: Position[];
  performance: PortfolioPerformance[];
  createdAt: string;
  updatedAt: string;
}

export interface Position {
  id: string;
  portfolioId: string;
  symbolId: string;
  symbol: string;
  name: string;
  quantity: number;
  averagePrice: number;
  currentPrice: number;
  totalCost: number;
  currentValue: number;
  gain: number;
  gainPercent: number;
  assetClass: string;
  createdAt: string;
  updatedAt: string;
}

export interface PortfolioPerformance {
  date: string;
  totalValue: number;
  totalReturn: number;
  totalReturnPercent: number;
  cash: number;
}

// Trading Types
export interface Trade {
  id: string;
  portfolioId: string;
  symbolId: string;
  symbol: string;
  type: TradeType;
  side: TradeSide;
  quantity: number;
  price: number;
  totalAmount: number;
  fees: number;
  status: TradeStatus;
  executedAt?: string;
  createdAt: string;
}

export type TradeType = 'MARKET' | 'LIMIT' | 'STOP' | 'STOP_LIMIT';
export type TradeSide = 'BUY' | 'SELL';
export type TradeStatus = 'PENDING' | 'EXECUTED' | 'CANCELLED' | 'FAILED';

export interface TradeRequest {
  symbolId: string;
  type: TradeType;
  side: TradeSide;
  quantity: number;
  price?: number;
  stopPrice?: number;
}

// Competition and Leaderboard Types
export interface Competition {
  id: string;
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  status: CompetitionStatus;
  prize: string;
  participantCount: number;
  rules: string[];
}

export type CompetitionStatus = 'UPCOMING' | 'ACTIVE' | 'COMPLETED' | 'CANCELLED';

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  username: string;
  avatar?: string;
  totalReturn: number;
  totalReturnPercent: number;
  portfolioValue: number;
  tradeCount: number;
  winRate: number;
  tier: UserTier;
  isCurrentUser: boolean;
}

export type UserTier = 'BRONZE' | 'SILVER' | 'GOLD' | 'PLATINUM' | 'DIAMOND';

// News and Education Types
export interface NewsItem {
  id: string;
  title: string;
  summary: string;
  content?: string;
  imageUrl?: string;
  sourceUrl: string;
  sourceName: string;
  publishedAt: string;
  category: NewsCategory;
  symbols?: string[];
  assetClass?: string;
}

export type NewsCategory = 'MARKET' | 'CRYPTO' | 'STOCKS' | 'ECONOMIC' | 'COMPANY' | 'ANALYSIS';

// UI and State Types
export interface UIState {
  theme: Theme;
  sidebar: SidebarState;
  modals: ModalState;
  notifications: Notification[];
  loading: LoadingState;
}

export type Theme = 'light' | 'dark' | 'system';

export interface SidebarState {
  isOpen: boolean;
  isCollapsed: boolean;
  activeSection: string | null;
}

export interface ModalState {
  [key: string]: boolean;
}

export interface Notification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  duration?: number;
  action?: NotificationAction;
  createdAt: string;
}

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface NotificationAction {
  label: string;
  action: () => void;
}

export interface LoadingState {
  [key: string]: boolean;
}

// API Response Types
export interface ApiResponse<T = any> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
  meta?: PaginationMeta;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiError {
  message: string;
  code?: string;
  details?: Record<string, any>;
  timestamp: string;
}

// WebSocket Types
export interface WebSocketMessage {
  type: string;
  payload: any;
  timestamp: string;
}

export interface PriceUpdate {
  symbol: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  timestamp: string;
}

// Enhanced price data interface with data source information (Alpaca integration)
export interface StockPriceData {
  symbol: string;
  price: number;
  priceChange?: number;
  priceChangePercent?: number;
  volume: number;
  timestamp: string;
  source?: "ALPACA" | "YAHOO_FALLBACK" | "YAHOO_REALTIME"; // Optional for backward compatibility
  qualityScore?: number; // Optional: 100 for Alpaca, 80 for Yahoo
  isRealtime?: boolean; // Optional: true for Alpaca, false for Yahoo fallback
}

export interface MarketUpdate {
  symbols: PriceUpdate[];
  timestamp: string;
}

// Form Types
export interface FormField {
  name: string;
  type: 'text' | 'email' | 'password' | 'number' | 'select' | 'checkbox' | 'textarea';
  label: string;
  placeholder?: string;
  required?: boolean;
  validation?: ValidationRule[];
  options?: SelectOption[];
}

export interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
}

export interface ValidationRule {
  type: 'required' | 'email' | 'minLength' | 'maxLength' | 'pattern' | 'custom';
  value?: any;
  message: string;
}

export interface FormState {
  values: Record<string, any>;
  errors: Record<string, string>;
  touched: Record<string, boolean>;
  isValid: boolean;
  isSubmitting: boolean;
}

// Chart and Visualization Types
export interface ChartDataPoint {
  x: number | string | Date;
  y: number;
  label?: string;
}

export interface ChartConfig {
  type: 'line' | 'candlestick' | 'area' | 'bar';
  data: ChartDataPoint[];
  xAxis: ChartAxis;
  yAxis: ChartAxis;
  colors?: string[];
  height?: number;
  responsive?: boolean;
}

export interface ChartAxis {
  label: string;
  type: 'number' | 'date' | 'category';
  format?: string;
  min?: number;
  max?: number;
}

// Filter and Search Types
export interface FilterOption {
  key: string;
  label: string;
  type: 'select' | 'range' | 'checkbox' | 'date';
  options?: SelectOption[];
  min?: number;
  max?: number;
}

export interface SearchFilters {
  [key: string]: any;
}

export interface SortOption {
  field: string;
  direction: 'asc' | 'desc';
  label: string;
}

// Utility Types
export type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

export type Optional<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;

export type RequiredFields<T, K extends keyof T> = T & Required<Pick<T, K>>;

// Component Props Types
export interface BaseComponentProps {
  className?: string;
  id?: string;
  'data-testid'?: string;
}

export interface ChildrenProps {
  children: React.ReactNode;
}

export interface LoadingProps {
  isLoading?: boolean;
  loadingText?: string;
}

export interface ErrorProps {
  error?: Error | string | null;
  onRetry?: () => void;
}

// Export utility type guards
export const isMarketData = (data: any): data is MarketData => {
  return data && typeof data.price === 'number' && typeof data.symbol === 'string';
};

export const isUser = (data: any): data is User => {
  return data && typeof data.id === 'string' && typeof data.email === 'string';
};

export const isApiError = (error: any): error is ApiError => {
  return error && typeof error.message === 'string';
};