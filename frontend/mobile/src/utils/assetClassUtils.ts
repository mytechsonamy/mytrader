import { AssetClassDto, MarketDto, EnhancedSymbolDto, AssetClassType } from '../types';

/**
 * Utility functions for Asset Class operations
 */

// Asset Class Validators
export const isValidAssetClass = (assetClass: string): assetClass is AssetClassType => {
  return ['CRYPTO', 'STOCK', 'FOREX', 'COMMODITY', 'INDEX'].includes(assetClass as AssetClassType);
};

export const validateAssetClassData = (data: Partial<AssetClassDto>): string[] => {
  const errors: string[] = [];

  if (!data.name || data.name.trim().length === 0) {
    errors.push('Asset class name is required');
  }

  if (!data.displayName || data.displayName.trim().length === 0) {
    errors.push('Asset class display name is required');
  }

  if (data.priority !== undefined && (data.priority < 0 || data.priority > 100)) {
    errors.push('Priority must be between 0 and 100');
  }

  return errors;
};

// Asset Class Filtering and Sorting
export const filterActiveAssetClasses = (assetClasses: AssetClassDto[]): AssetClassDto[] => {
  return assetClasses.filter(ac => ac.isActive);
};

export const sortAssetClassesByPriority = (assetClasses: AssetClassDto[]): AssetClassDto[] => {
  return [...assetClasses].sort((a, b) => (b.priority || 0) - (a.priority || 0));
};

export const sortAssetClassesAlphabetically = (assetClasses: AssetClassDto[]): AssetClassDto[] => {
  return [...assetClasses].sort((a, b) => a.displayName.localeCompare(b.displayName));
};

// Asset Class Grouping
export const groupMarketsByAssetClass = (
  markets: MarketDto[],
  assetClasses: AssetClassDto[]
): Record<string, { assetClass: AssetClassDto; markets: MarketDto[] }> => {
  const grouped: Record<string, { assetClass: AssetClassDto; markets: MarketDto[] }> = {};

  assetClasses.forEach(assetClass => {
    grouped[assetClass.id] = {
      assetClass,
      markets: markets.filter(market => market.assetClassId === assetClass.id)
    };
  });

  return grouped;
};

export const groupSymbolsByAssetClass = (
  symbols: EnhancedSymbolDto[],
  assetClasses: AssetClassDto[]
): Record<string, { assetClass: AssetClassDto; symbols: EnhancedSymbolDto[] }> => {
  const grouped: Record<string, { assetClass: AssetClassDto; symbols: EnhancedSymbolDto[] }> = {};

  assetClasses.forEach(assetClass => {
    grouped[assetClass.id] = {
      assetClass,
      symbols: symbols.filter(symbol => symbol.assetClassId === assetClass.id)
    };
  });

  return grouped;
};

// Asset Class Statistics
export interface AssetClassStats {
  totalSymbols: number;
  activeSymbols: number;
  trackedSymbols: number;
  marketsCount: number;
  activeMarketsCount: number;
  symbolsPerMarket: number;
  coverage: number; // percentage of tracked vs total symbols
}

export const calculateAssetClassStats = (
  assetClass: AssetClassDto,
  symbols: EnhancedSymbolDto[],
  markets: MarketDto[]
): AssetClassStats => {
  const assetSymbols = symbols.filter(s => s.assetClassId === assetClass.id);
  const assetMarkets = markets.filter(m => m.assetClassId === assetClass.id);

  const activeSymbols = assetSymbols.filter(s => s.isActive);
  const trackedSymbols = assetSymbols.filter(s => s.isTracked);
  const activeMarkets = assetMarkets.filter(m => m.isActive);

  return {
    totalSymbols: assetSymbols.length,
    activeSymbols: activeSymbols.length,
    trackedSymbols: trackedSymbols.length,
    marketsCount: assetMarkets.length,
    activeMarketsCount: activeMarkets.length,
    symbolsPerMarket: assetMarkets.length > 0 ? assetSymbols.length / assetMarkets.length : 0,
    coverage: assetSymbols.length > 0 ? (trackedSymbols.length / assetSymbols.length) * 100 : 0
  };
};

// Asset Class Display Helpers
export const getAssetClassDisplayInfo = (assetClassType: AssetClassType) => {
  const displayInfo = {
    CRYPTO: {
      icon: '₿',
      color: '#F7931A',
      description: 'Cryptocurrencies and digital assets',
      examples: ['Bitcoin', 'Ethereum', 'Altcoins']
    },
    STOCK: {
      icon: '📈',
      color: '#2E8B57',
      description: 'Equity securities and shares',
      examples: ['BIST Stocks', 'NASDAQ Stocks', 'NYSE Stocks']
    },
    FOREX: {
      icon: '💱',
      color: '#4169E1',
      description: 'Foreign exchange currency pairs',
      examples: ['USD/TRY', 'EUR/USD', 'GBP/JPY']
    },
    COMMODITY: {
      icon: '🥇',
      color: '#DAA520',
      description: 'Physical goods and raw materials',
      examples: ['Gold', 'Oil', 'Agricultural Products']
    },
    INDEX: {
      icon: '📊',
      color: '#800080',
      description: 'Market indices and benchmarks',
      examples: ['S&P 500', 'BIST 100', 'NASDAQ Composite']
    }
  };

  return displayInfo[assetClassType] || {
    icon: '❓',
    color: '#808080',
    description: 'Unknown asset class',
    examples: []
  };
};

export const formatAssetClassName = (assetClass: AssetClassDto | AssetClassType): string => {
  if (typeof assetClass === 'string') {
    return assetClass.charAt(0).toUpperCase() + assetClass.slice(1).toLowerCase();
  }
  return assetClass.displayName || assetClass.name;
};

// Asset Class Search and Filter Utilities
export const searchAssetClasses = (
  assetClasses: AssetClassDto[],
  query: string,
  options: {
    includeInactive?: boolean;
    fuzzyMatch?: boolean;
  } = {}
): AssetClassDto[] => {
  const { includeInactive = false, fuzzyMatch = true } = options;

  let filtered = includeInactive ? assetClasses : filterActiveAssetClasses(assetClasses);

  if (!query.trim()) {
    return filtered;
  }

  const normalizedQuery = query.toLowerCase().trim();

  return filtered.filter(ac => {
    const searchableText = [
      ac.name,
      ac.displayName,
      ac.description || ''
    ].join(' ').toLowerCase();

    if (fuzzyMatch) {
      // Simple fuzzy matching - check if all query characters appear in order
      let queryIndex = 0;
      for (let i = 0; i < searchableText.length && queryIndex < normalizedQuery.length; i++) {
        if (searchableText[i] === normalizedQuery[queryIndex]) {
          queryIndex++;
        }
      }
      return queryIndex === normalizedQuery.length;
    } else {
      return searchableText.includes(normalizedQuery);
    }
  });
};

// Asset Class Configuration Helpers
export const getDefaultAssetClassConfig = (): Partial<AssetClassDto> => ({
  isActive: true,
  priority: 50,
  createdAt: new Date().toISOString()
});

export const mergeAssetClassConfig = (
  base: AssetClassDto,
  updates: Partial<AssetClassDto>
): AssetClassDto => ({
  ...base,
  ...updates,
  updatedAt: new Date().toISOString()
});

// Asset Class Comparison
export const compareAssetClasses = (a: AssetClassDto, b: AssetClassDto): {
  differences: Array<{
    field: keyof AssetClassDto;
    aValue: any;
    bValue: any;
  }>;
  isEqual: boolean;
} => {
  const differences: Array<{
    field: keyof AssetClassDto;
    aValue: any;
    bValue: any;
  }> = [];

  const fieldsToCompare: (keyof AssetClassDto)[] = [
    'name', 'displayName', 'description', 'isActive', 'priority'
  ];

  fieldsToCompare.forEach(field => {
    if (a[field] !== b[field]) {
      differences.push({
        field,
        aValue: a[field],
        bValue: b[field]
      });
    }
  });

  return {
    differences,
    isEqual: differences.length === 0
  };
};

// Asset Class Performance Analytics
export interface AssetClassPerformance {
  assetClassId: string;
  assetClassName: string;
  symbolCount: number;
  averageReturn: number;
  volatility: number;
  sharpeRatio: number;
  topPerformers: Array<{
    symbolId: string;
    symbol: string;
    return: number;
  }>;
  worstPerformers: Array<{
    symbolId: string;
    symbol: string;
    return: number;
  }>;
}

export const calculateAssetClassPerformance = (
  assetClass: AssetClassDto,
  symbols: EnhancedSymbolDto[],
  marketData: Record<string, { changePercent: number; volatility?: number }>
): AssetClassPerformance => {
  const assetSymbols = symbols.filter(s => s.assetClassId === assetClass.id);
  const returns = assetSymbols
    .map(s => marketData[s.id]?.changePercent || 0)
    .filter(r => !isNaN(r));

  const averageReturn = returns.length > 0
    ? returns.reduce((sum, r) => sum + r, 0) / returns.length
    : 0;

  const variance = returns.length > 0
    ? returns.reduce((sum, r) => sum + Math.pow(r - averageReturn, 2), 0) / returns.length
    : 0;

  const volatility = Math.sqrt(variance);
  const sharpeRatio = volatility > 0 ? averageReturn / volatility : 0;

  // Sort symbols by performance
  const performanceData = assetSymbols
    .map(s => ({
      symbolId: s.id,
      symbol: s.symbol,
      return: marketData[s.id]?.changePercent || 0
    }))
    .sort((a, b) => b.return - a.return);

  return {
    assetClassId: assetClass.id,
    assetClassName: assetClass.displayName,
    symbolCount: assetSymbols.length,
    averageReturn,
    volatility,
    sharpeRatio,
    topPerformers: performanceData.slice(0, 5),
    worstPerformers: performanceData.slice(-5).reverse()
  };
};

// Export utility collections
export const AssetClassValidators = {
  isValidAssetClass,
  validateAssetClassData
};

export const AssetClassFilters = {
  filterActiveAssetClasses,
  sortAssetClassesByPriority,
  sortAssetClassesAlphabetically,
  searchAssetClasses
};

export const AssetClassGroupers = {
  groupMarketsByAssetClass,
  groupSymbolsByAssetClass
};

export const AssetClassAnalytics = {
  calculateAssetClassStats,
  calculateAssetClassPerformance
};

export const AssetClassHelpers = {
  getAssetClassDisplayInfo,
  formatAssetClassName,
  getDefaultAssetClassConfig,
  mergeAssetClassConfig,
  compareAssetClasses
};