import React, { createContext, useContext, useReducer, useCallback, useEffect } from 'react';
import { Portfolio, Position, Transaction, PortfolioAnalytics, LoadingState } from '../types';
import { apiService } from '../services/api';
import { useAuth } from './AuthContext';

interface PortfolioState {
  portfolios: Portfolio[];
  currentPortfolio: Portfolio | null;
  positions: Position[];
  transactions: Transaction[];
  analytics: PortfolioAnalytics | null;
  loadingState: LoadingState;
  error: string | null;
}

type PortfolioAction =
  | { type: 'SET_LOADING'; payload: LoadingState }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_PORTFOLIOS'; payload: Portfolio[] }
  | { type: 'SET_CURRENT_PORTFOLIO'; payload: Portfolio | null }
  | { type: 'SET_POSITIONS'; payload: Position[] }
  | { type: 'SET_TRANSACTIONS'; payload: Transaction[] }
  | { type: 'SET_ANALYTICS'; payload: PortfolioAnalytics | null }
  | { type: 'ADD_PORTFOLIO'; payload: Portfolio }
  | { type: 'UPDATE_PORTFOLIO'; payload: Portfolio }
  | { type: 'REMOVE_PORTFOLIO'; payload: string }
  | { type: 'ADD_TRANSACTION'; payload: Transaction }
  | { type: 'RESET' };

const initialState: PortfolioState = {
  portfolios: [],
  currentPortfolio: null,
  positions: [],
  transactions: [],
  analytics: null,
  loadingState: 'idle',
  error: null,
};

const portfolioReducer = (state: PortfolioState, action: PortfolioAction): PortfolioState => {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, loadingState: action.payload, error: null };
    case 'SET_ERROR':
      return { ...state, error: action.payload, loadingState: 'error' };
    case 'SET_PORTFOLIOS':
      return { ...state, portfolios: action.payload, loadingState: 'success' };
    case 'SET_CURRENT_PORTFOLIO':
      return { ...state, currentPortfolio: action.payload };
    case 'SET_POSITIONS':
      return { ...state, positions: action.payload };
    case 'SET_TRANSACTIONS':
      return { ...state, transactions: action.payload };
    case 'SET_ANALYTICS':
      return { ...state, analytics: action.payload };
    case 'ADD_PORTFOLIO':
      return { 
        ...state, 
        portfolios: [...state.portfolios, action.payload],
        loadingState: 'success' 
      };
    case 'UPDATE_PORTFOLIO':
      return {
        ...state,
        portfolios: state.portfolios.map(p => 
          p.id === action.payload.id ? action.payload : p
        ),
        currentPortfolio: state.currentPortfolio?.id === action.payload.id 
          ? action.payload 
          : state.currentPortfolio,
        loadingState: 'success'
      };
    case 'REMOVE_PORTFOLIO':
      return {
        ...state,
        portfolios: state.portfolios.filter(p => p.id !== action.payload),
        currentPortfolio: state.currentPortfolio?.id === action.payload 
          ? null 
          : state.currentPortfolio,
        loadingState: 'success'
      };
    case 'ADD_TRANSACTION':
      return {
        ...state,
        transactions: [action.payload, ...state.transactions],
      };
    case 'RESET':
      return initialState;
    default:
      return state;
  }
};

interface PortfolioContextType {
  state: PortfolioState;
  
  // Portfolio Operations
  loadPortfolios: () => Promise<void>;
  createPortfolio: (portfolio: { name: string; description?: string; baseCurrency: string }) => Promise<void>;
  updatePortfolio: (portfolioId: string, updates: Partial<Portfolio>) => Promise<void>;
  deletePortfolio: (portfolioId: string) => Promise<void>;
  selectPortfolio: (portfolioId: string) => Promise<void>;
  
  // Position & Transaction Operations
  loadPortfolioPositions: (portfolioId: string) => Promise<void>;
  loadPortfolioTransactions: (portfolioId: string, limit?: number, offset?: number) => Promise<void>;
  createTransaction: (transaction: {
    portfolioId: string;
    symbol: string;
    type: 'BUY' | 'SELL';
    quantity: number;
    price: number;
    fee?: number;
    notes?: string;
  }) => Promise<void>;
  
  // Analytics Operations
  loadPortfolioAnalytics: (portfolioId: string) => Promise<void>;
  
  // Export Operations
  exportPortfolio: (portfolioId: string, format: 'csv' | 'pdf' | 'json') => Promise<string>;
  
  // Utility
  clearError: () => void;
  reset: () => void;
}

const PortfolioContext = createContext<PortfolioContextType | undefined>(undefined);

export const PortfolioProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, dispatch] = useReducer(portfolioReducer, initialState);
  const { user } = useAuth();

  const loadPortfolios = useCallback(async () => {
    if (!user) return;
    
    dispatch({ type: 'SET_LOADING', payload: 'loading' });
    try {
      const portfolios = await apiService.getPortfolios();
      dispatch({ type: 'SET_PORTFOLIOS', payload: portfolios });
    } catch (error) {
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to load portfolios' });
    }
  }, [user]);

  const createPortfolio = useCallback(async (portfolio: { name: string; description?: string; baseCurrency: string }) => {
    dispatch({ type: 'SET_LOADING', payload: 'loading' });
    try {
      const newPortfolio = await apiService.createPortfolio(portfolio);
      dispatch({ type: 'ADD_PORTFOLIO', payload: newPortfolio });
    } catch (error) {
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to create portfolio' });
    }
  }, []);

  const updatePortfolio = useCallback(async (portfolioId: string, updates: Partial<Portfolio>) => {
    dispatch({ type: 'SET_LOADING', payload: 'loading' });
    try {
      const updatedPortfolio = await apiService.updatePortfolio(portfolioId, updates);
      dispatch({ type: 'UPDATE_PORTFOLIO', payload: updatedPortfolio });
    } catch (error) {
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to update portfolio' });
    }
  }, []);

  const deletePortfolio = useCallback(async (portfolioId: string) => {
    dispatch({ type: 'SET_LOADING', payload: 'loading' });
    try {
      await apiService.deletePortfolio(portfolioId);
      dispatch({ type: 'REMOVE_PORTFOLIO', payload: portfolioId });
    } catch (error) {
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to delete portfolio' });
    }
  }, []);

  const selectPortfolio = useCallback(async (portfolioId: string) => {
    dispatch({ type: 'SET_LOADING', payload: 'loading' });
    try {
      const portfolio = await apiService.getPortfolio(portfolioId);
      dispatch({ type: 'SET_CURRENT_PORTFOLIO', payload: portfolio });
      
      // Load related data
      await Promise.all([
        loadPortfolioPositions(portfolioId),
        loadPortfolioTransactions(portfolioId, 50),
        loadPortfolioAnalytics(portfolioId),
      ]);
    } catch (error) {
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to load portfolio' });
    }
  }, []);

  const loadPortfolioPositions = useCallback(async (portfolioId: string) => {
    try {
      const positions = await apiService.getPortfolioPositions(portfolioId);
      dispatch({ type: 'SET_POSITIONS', payload: positions });
    } catch (error) {
      console.error('Failed to load positions:', error);
    }
  }, []);

  const loadPortfolioTransactions = useCallback(async (portfolioId: string, limit?: number, offset?: number) => {
    try {
      const transactions = await apiService.getPortfolioTransactions(portfolioId, limit, offset);
      dispatch({ type: 'SET_TRANSACTIONS', payload: transactions });
    } catch (error) {
      console.error('Failed to load transactions:', error);
    }
  }, []);

  const createTransaction = useCallback(async (transaction: {
    portfolioId: string;
    symbol: string;
    type: 'BUY' | 'SELL';
    quantity: number;
    price: number;
    fee?: number;
    notes?: string;
  }) => {
    try {
      const newTransaction = await apiService.createTransaction(transaction);
      dispatch({ type: 'ADD_TRANSACTION', payload: newTransaction });
      
      // Refresh portfolio data
      await selectPortfolio(transaction.portfolioId);
    } catch (error) {
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to create transaction' });
    }
  }, [selectPortfolio]);

  const loadPortfolioAnalytics = useCallback(async (portfolioId: string) => {
    try {
      const analytics = await apiService.getPortfolioAnalytics(portfolioId);
      dispatch({ type: 'SET_ANALYTICS', payload: analytics });
    } catch (error) {
      console.error('Failed to load analytics:', error);
    }
  }, []);

  const exportPortfolio = useCallback(async (portfolioId: string, format: 'csv' | 'pdf' | 'json'): Promise<string> => {
    const request = {
      portfolioId,
      format,
      dateFrom: new Date(Date.now() - 365 * 24 * 60 * 60 * 1000).toISOString(), // 1 year ago
      dateTo: new Date().toISOString(),
      includeSummary: true,
      includeHoldings: true,
      includeTransactions: true,
      includePerformance: true,
      includeCharts: format === 'pdf',
    };
    
    const response = await apiService.exportPortfolio(request);
    return response.fileContent; // Base64 encoded file
  }, []);

  const clearError = useCallback(() => {
    dispatch({ type: 'SET_ERROR', payload: null });
  }, []);

  const reset = useCallback(() => {
    dispatch({ type: 'RESET' });
  }, []);

  // Load portfolios when user logs in
  useEffect(() => {
    if (user) {
      loadPortfolios();
    } else {
      reset();
    }
  }, [user, loadPortfolios, reset]);

  const contextValue: PortfolioContextType = {
    state,
    loadPortfolios,
    createPortfolio,
    updatePortfolio,
    deletePortfolio,
    selectPortfolio,
    loadPortfolioPositions,
    loadPortfolioTransactions,
    createTransaction,
    loadPortfolioAnalytics,
    exportPortfolio,
    clearError,
    reset,
  };

  return (
    <PortfolioContext.Provider value={contextValue}>
      {children}
    </PortfolioContext.Provider>
  );
};

export const usePortfolio = (): PortfolioContextType => {
  const context = useContext(PortfolioContext);
  if (!context) {
    throw new Error('usePortfolio must be used within a PortfolioProvider');
  }
  return context;
};