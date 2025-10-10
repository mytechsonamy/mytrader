import { useEffect, useState } from 'react';
import { marketDataService } from '../../services/marketDataService';
import ErrorBoundary from '../ErrorBoundary';
import { safeArray, safeString, safeNumber, safeCurrency, safePercent } from '../../utils/dataValidation';

interface LeaderboardUser {
  id: string;
  name: string;
  totalTrades: number;
  winRate: number;
  totalReturn: number;
  totalValue: number;
  rank: number;
  badge?: string;
}

interface LeaderboardSectionProps {
  className?: string;
}

const LeaderboardSection: React.FC<LeaderboardSectionProps> = ({ className = '' }) => {
  const [leaderboard, setLeaderboard] = useState<LeaderboardUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [period, setPeriod] = useState<'weekly' | 'monthly' | 'alltime'>('weekly');

  useEffect(() => {
    const fetchLeaderboard = async () => {
      try {
        setLoading(true);
        setError(null);
        const leaderboardData = await marketDataService.getLeaderboard(period);
        setLeaderboard(safeArray<LeaderboardUser>(leaderboardData));
      } catch (err: any) {
        console.error('Failed to fetch leaderboard:', err);
        setError(err.message || 'Failed to load leaderboard');
        // Fallback to mock data on error
        setLeaderboard(getMockLeaderboard());
      } finally {
        setLoading(false);
      }
    };

    fetchLeaderboard();
  }, [period]);

  const getMockLeaderboard = (): LeaderboardUser[] => [
    {
      id: '1',
      name: 'Sarah Chen',
      totalTrades: 127,
      winRate: 89,
      totalReturn: 24.7,
      totalValue: 47230,
      rank: 1,
      badge: 'gold'
    },
    {
      id: '2',
      name: 'Michael Rodriguez',
      totalTrades: 94,
      winRate: 82,
      totalReturn: 19.2,
      totalValue: 35840,
      rank: 2,
      badge: 'silver'
    },
    {
      id: '3',
      name: 'David Kim',
      totalTrades: 156,
      winRate: 76,
      totalReturn: 15.8,
      totalValue: 28920,
      rank: 3,
      badge: 'bronze'
    },
    {
      id: '4',
      name: 'Emma Thompson',
      totalTrades: 73,
      winRate: 78,
      totalReturn: 12.4,
      totalValue: 22150,
      rank: 4
    },
    {
      id: '5',
      name: 'Alex Johnson',
      totalTrades: 89,
      winRate: 71,
      totalReturn: 9.6,
      totalValue: 18730,
      rank: 5
    }
  ];

  const formatCurrency = (value: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(value);
  };

  const formatPercent = (value: number): string => {
    return `${value >= 0 ? '+' : ''}${value.toFixed(1)}%`;
  };

  const getRankBadgeClass = (rank: number): string => {
    if (rank === 1) return 'rank-badge first';
    if (rank === 2) return 'rank-badge second';
    if (rank === 3) return 'rank-badge third';
    return 'rank-badge other';
  };

  if (loading) {
    return (
      <div className={`section-card ${className}`}>
        <div className="section-header">
          <h2 className="section-title">
            <div className="section-icon">🏆</div>
            <span className="section-title-text">Leaderboard</span>
          </h2>
        </div>
        <div className="section-content">
          <div className="loading-state">
            <div className="spinner"></div>
            <p>Loading leaderboard...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error && leaderboard.length === 0) {
    return (
      <div className={`section-card ${className}`}>
        <div className="section-header">
          <h2 className="section-title">
            <div className="section-icon">🏆</div>
            <span className="section-title-text">Leaderboard</span>
          </h2>
        </div>
        <div className="section-content">
          <div className="error-state">
            <h3>Unable to Load Leaderboard</h3>
            <p>{error}</p>
            <button
              onClick={() => window.location.reload()}
              className="retry-button"
            >
              Retry
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`section-card ${className}`}>
      <div className="section-header">
        <h2 className="section-title">
          <div className="section-icon">🏆</div>
          <span className="section-title-text">Leaderboard</span>
        </h2>
        {error && (
          <div className="status-indicator error" title={`Error: ${error}`}>
            ⚠️
          </div>
        )}
      </div>
      <div className="section-content">
        <div className="leaderboard">
          <div className="leaderboard-filters">
            <button
              className={`filter-tab ${period === 'weekly' ? 'active' : ''}`}
              onClick={() => setPeriod('weekly')}
            >
              This Week
            </button>
            <button
              className={`filter-tab ${period === 'monthly' ? 'active' : ''}`}
              onClick={() => setPeriod('monthly')}
            >
              This Month
            </button>
            <button
              className={`filter-tab ${period === 'alltime' ? 'active' : ''}`}
              onClick={() => setPeriod('alltime')}
            >
              All Time
            </button>
          </div>

          <div className="leaderboard-list">
            {safeArray<LeaderboardUser>(leaderboard).map((user) => {
              const userName = safeString(user.name, 'Unknown User');
              const userRank = safeNumber(user.rank, 0);
              const totalReturn = safeNumber(user.totalReturn, 0);

              return (
                <ErrorBoundary
                  key={user.id || `user-${userRank}`}
                  fallback={
                    <div className="leaderboard-item error-item">
                      <p>Error loading user data</p>
                    </div>
                  }
                >
                  <div className="leaderboard-item">
                    <div className={getRankBadgeClass(userRank)}>
                      {userRank}
                    </div>
                    <div className="user-info">
                      <h4 className="user-name">{userName}</h4>
                      <p className="user-stats">
                        {safeNumber(user.totalTrades, 0)} trades • {safeNumber(user.winRate, 0)}% win rate
                      </p>
                    </div>
                    <div className="performance-metrics">
                      <p className={`total-return ${totalReturn >= 0 ? 'positive' : 'negative'}`}>
                        {safePercent(totalReturn)}
                      </p>
                      <p className="win-rate">{safeCurrency(user.totalValue)}</p>
                    </div>
                  </div>
                </ErrorBoundary>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
};

export default LeaderboardSection;