/**
 * Portfolio hooks using React Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiService, endpoints } from '../services/api';
import { queryKeys, handleQueryError } from '../services/queryClient';
import type { Portfolio, Position, Trade } from '../types';

// Portfolio list hook
export const usePortfolios = () => {
  return useQuery({
    queryKey: queryKeys.portfolio.list(),
    queryFn: async () => {
      const response = await apiService.get<Portfolio[]>(endpoints.portfolio.list);
      return response.data;
    },
    staleTime: 1000 * 60 * 2, // 2 minutes
    onError: (error) => handleQueryError(error, 'Portfolio list'),
  });
};

// Single portfolio hook
export const usePortfolio = (portfolioId: string, enabled = true) => {
  return useQuery({
    queryKey: queryKeys.portfolio.detail(portfolioId),
    queryFn: async () => {
      const response = await apiService.get<Portfolio>(endpoints.portfolio.detail(portfolioId));
      return response.data;
    },
    enabled: enabled && !!portfolioId,
    staleTime: 1000 * 60, // 1 minute
    onError: (error) => handleQueryError(error, `Portfolio ${portfolioId}`),
  });
};

// Portfolio positions hook
export const usePortfolioPositions = (portfolioId: string, enabled = true) => {
  return useQuery({
    queryKey: queryKeys.portfolio.positions(portfolioId),
    queryFn: async () => {
      const response = await apiService.get<Position[]>(endpoints.portfolio.positions(portfolioId));
      return response.data;
    },
    enabled: enabled && !!portfolioId,
    staleTime: 1000 * 30, // 30 seconds
    onError: (error) => handleQueryError(error, `Portfolio positions ${portfolioId}`),
  });
};

// Portfolio performance hook
export const usePortfolioPerformance = (
  portfolioId: string,
  params?: { period?: string; interval?: string },
  enabled = true
) => {
  return useQuery({
    queryKey: queryKeys.portfolio.performance(portfolioId, params),
    queryFn: async () => {
      const response = await apiService.get(endpoints.portfolio.performance(portfolioId), params);
      return response.data;
    },
    enabled: enabled && !!portfolioId,
    staleTime: 1000 * 60 * 5, // 5 minutes
    onError: (error) => handleQueryError(error, `Portfolio performance ${portfolioId}`),
  });
};

// Portfolio trades hook
export const usePortfolioTrades = (
  portfolioId: string,
  params?: { limit?: number; offset?: number },
  enabled = true
) => {
  return useQuery({
    queryKey: queryKeys.portfolio.trades(portfolioId, params),
    queryFn: async () => {
      const response = await apiService.get<Trade[]>(endpoints.portfolio.trades(portfolioId), params);
      return response.data;
    },
    enabled: enabled && !!portfolioId,
    staleTime: 1000 * 60 * 2, // 2 minutes
    onError: (error) => handleQueryError(error, `Portfolio trades ${portfolioId}`),
  });
};

// Portfolio mutations
export const usePortfolioMutations = () => {
  const queryClient = useQueryClient();

  const createPortfolio = useMutation({
    mutationFn: async (data: Partial<Portfolio>) => {
      const response = await apiService.post<Portfolio>(endpoints.portfolio.create, data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.portfolio.list() });
    },
    onError: (error) => handleQueryError(error, 'Create portfolio'),
  });

  const updatePortfolio = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: Partial<Portfolio> }) => {
      const response = await apiService.put<Portfolio>(endpoints.portfolio.update(id), data);
      return response.data;
    },
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.portfolio.detail(id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.portfolio.list() });
    },
    onError: (error) => handleQueryError(error, 'Update portfolio'),
  });

  const deletePortfolio = useMutation({
    mutationFn: async (id: string) => {
      const response = await apiService.delete(endpoints.portfolio.delete(id));
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.portfolio.list() });
    },
    onError: (error) => handleQueryError(error, 'Delete portfolio'),
  });

  return {
    createPortfolio,
    updatePortfolio,
    deletePortfolio,
  };
};

// Export fetch functions for prefetching
export const fetchPortfolios = async () => {
  const response = await apiService.get<Portfolio[]>(endpoints.portfolio.list);
  return response.data;
};

export const fetchPortfolio = async (portfolioId: string) => {
  const response = await apiService.get<Portfolio>(endpoints.portfolio.detail(portfolioId));
  return response.data;
};