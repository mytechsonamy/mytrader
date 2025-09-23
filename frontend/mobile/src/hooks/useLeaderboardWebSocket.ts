import { useState, useEffect, useCallback, useRef } from 'react';
import { LeaderboardEntry, UserRanking, CompetitionStats } from '../types';
import { apiService } from '../services/api';

interface LeaderboardUpdate {
  type: 'ranking_update' | 'user_ranking_update' | 'stats_update' | 'new_participant';
  data: any;
  timestamp: string;
}

interface UseLeaderboardWebSocketProps {
  enabled?: boolean;
  period?: 'weekly' | 'monthly' | 'all';
  updateInterval?: number; // Fallback polling interval in ms
  onRankingChange?: (newRanking: LeaderboardEntry[]) => void;
  onUserRankingChange?: (newUserRanking: UserRanking) => void;
  onStatsChange?: (newStats: CompetitionStats) => void;
}

interface UseLeaderboardWebSocketReturn {
  isConnected: boolean;
  lastUpdate: Date | null;
  connectionStatus: 'connecting' | 'connected' | 'disconnected' | 'error';
  reconnectAttempts: number;
  startConnection: () => void;
  stopConnection: () => void;
  forceRefresh: () => Promise<void>;
}

export const useLeaderboardWebSocket = ({
  enabled = true,
  period = 'weekly',
  updateInterval = 30000, // 30 seconds fallback
  onRankingChange,
  onUserRankingChange,
  onStatsChange,
}: UseLeaderboardWebSocketProps): UseLeaderboardWebSocketReturn => {
  const [isConnected, setIsConnected] = useState(false);
  const [lastUpdate, setLastUpdate] = useState<Date | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'disconnected' | 'error'>('disconnected');
  const [reconnectAttempts, setReconnectAttempts] = useState(0);

  const wsRef = useRef<WebSocket | null>(null);
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const pollingIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const maxReconnectAttempts = 5;
  const reconnectDelay = Math.min(1000 * Math.pow(2, reconnectAttempts), 30000); // Exponential backoff

  // Fallback polling mechanism
  const startPolling = useCallback(async () => {
    if (pollingIntervalRef.current) {
      clearInterval(pollingIntervalRef.current);
    }

    const poll = async () => {
      try {
        const [leaderboardResult, userRankingResult, statsResult] = await Promise.allSettled([
          apiService.getLeaderboard(period, 50),
          apiService.getUserRanking(period),
          apiService.getCompetitionStats(),
        ]);

        if (leaderboardResult.status === 'fulfilled' && onRankingChange) {
          onRankingChange(leaderboardResult.value);
        }

        if (userRankingResult.status === 'fulfilled' && onUserRankingChange) {
          onUserRankingChange(userRankingResult.value);
        }

        if (statsResult.status === 'fulfilled' && onStatsChange) {
          onStatsChange(statsResult.value);
        }

        setLastUpdate(new Date());
      } catch (error) {
        console.warn('Polling update failed:', error);
      }
    };

    // Initial poll
    await poll();

    // Set up interval
    pollingIntervalRef.current = setInterval(poll, updateInterval);
  }, [period, updateInterval, onRankingChange, onUserRankingChange, onStatsChange]);

  const stopPolling = useCallback(() => {
    if (pollingIntervalRef.current) {
      clearInterval(pollingIntervalRef.current);
      pollingIntervalRef.current = null;
    }
  }, []);

  // WebSocket connection management
  const connectWebSocket = useCallback(async () => {
    if (!enabled || wsRef.current?.readyState === WebSocket.CONNECTING) {
      return;
    }

    try {
      setConnectionStatus('connecting');

      // Create WebSocket connection with authentication
      const subscriptions = [`leaderboard_${period}`, 'competition_stats'];
      wsRef.current = apiService.createEnhancedWebSocketConnection(subscriptions);

      wsRef.current.onopen = () => {
        console.log('✅ Leaderboard WebSocket connected');
        setIsConnected(true);
        setConnectionStatus('connected');
        setReconnectAttempts(0);
        setLastUpdate(new Date());

        // Stop polling when WebSocket is connected
        stopPolling();

        // Send subscription message
        if (wsRef.current?.readyState === WebSocket.OPEN) {
          wsRef.current.send(JSON.stringify({
            type: 'subscribe',
            subscriptions: [
              {
                action: 'subscribe',
                subscriptionType: 'leaderboard',
                period: period,
                limit: 50,
              },
              {
                action: 'subscribe',
                subscriptionType: 'competition_stats',
              },
            ],
          }));
        }
      };

      wsRef.current.onmessage = (event) => {
        try {
          const message = JSON.parse(event.data);
          setLastUpdate(new Date());

          switch (message.type) {
            case 'leaderboard_update':
              if (onRankingChange && message.data) {
                onRankingChange(message.data);
              }
              break;

            case 'user_ranking_update':
              if (onUserRankingChange && message.data) {
                onUserRankingChange(message.data);
              }
              break;

            case 'competition_stats_update':
              if (onStatsChange && message.data) {
                onStatsChange(message.data);
              }
              break;

            case 'rank_change_notification':
              // Handle rank change notifications
              console.log('🔄 Rank change:', message.data);
              break;

            case 'new_participant':
              // Handle new participant notifications
              console.log('👋 New participant:', message.data);
              break;

            case 'subscription_confirmed':
              console.log('✅ Subscription confirmed:', message.data);
              break;

            case 'heartbeat':
              // Handle heartbeat to keep connection alive
              break;

            default:
              console.log('📨 Unknown message type:', message.type);
          }
        } catch (error) {
          console.error('Failed to parse WebSocket message:', error);
        }
      };

      wsRef.current.onerror = (error) => {
        console.error('❌ Leaderboard WebSocket error:', error);
        setConnectionStatus('error');
      };

      wsRef.current.onclose = (event) => {
        console.log('🔌 Leaderboard WebSocket closed:', event.code, event.reason);
        setIsConnected(false);
        setConnectionStatus('disconnected');

        // Start fallback polling
        startPolling();

        // Attempt to reconnect if not manually closed and within retry limit
        if (event.code !== 1000 && reconnectAttempts < maxReconnectAttempts) {
          setReconnectAttempts(prev => prev + 1);

          reconnectTimeoutRef.current = setTimeout(() => {
            console.log(`🔄 Reconnecting... Attempt ${reconnectAttempts + 1}/${maxReconnectAttempts}`);
            connectWebSocket();
          }, reconnectDelay);
        }
      };

    } catch (error) {
      console.error('Failed to create WebSocket connection:', error);
      setConnectionStatus('error');

      // Fallback to polling
      startPolling();
    }
  }, [enabled, period, reconnectAttempts, reconnectDelay, onRankingChange, onUserRankingChange, onStatsChange, startPolling, stopPolling]);

  const disconnectWebSocket = useCallback(() => {
    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current);
      reconnectTimeoutRef.current = null;
    }

    if (wsRef.current) {
      wsRef.current.close(1000, 'Manual disconnect');
      wsRef.current = null;
    }

    stopPolling();
    setIsConnected(false);
    setConnectionStatus('disconnected');
    setReconnectAttempts(0);
  }, [stopPolling]);

  const startConnection = useCallback(() => {
    if (enabled) {
      connectWebSocket();
    } else {
      // If WebSocket is disabled, use polling
      startPolling();
    }
  }, [enabled, connectWebSocket, startPolling]);

  const stopConnection = useCallback(() => {
    disconnectWebSocket();
  }, [disconnectWebSocket]);

  const forceRefresh = useCallback(async () => {
    try {
      const [leaderboardResult, userRankingResult, statsResult] = await Promise.allSettled([
        apiService.getLeaderboard(period, 50),
        apiService.getUserRanking(period),
        apiService.getCompetitionStats(),
      ]);

      if (leaderboardResult.status === 'fulfilled' && onRankingChange) {
        onRankingChange(leaderboardResult.value);
      }

      if (userRankingResult.status === 'fulfilled' && onUserRankingChange) {
        onUserRankingChange(userRankingResult.value);
      }

      if (statsResult.status === 'fulfilled' && onStatsChange) {
        onStatsChange(statsResult.value);
      }

      setLastUpdate(new Date());
    } catch (error) {
      console.error('Force refresh failed:', error);
      throw error;
    }
  }, [period, onRankingChange, onUserRankingChange, onStatsChange]);

  // Start connection when enabled
  useEffect(() => {
    if (enabled) {
      startConnection();
    }

    return () => {
      stopConnection();
    };
  }, [enabled, period]); // Restart when period changes

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      stopConnection();
    };
  }, [stopConnection]);

  return {
    isConnected,
    lastUpdate,
    connectionStatus,
    reconnectAttempts,
    startConnection,
    stopConnection,
    forceRefresh,
  };
};