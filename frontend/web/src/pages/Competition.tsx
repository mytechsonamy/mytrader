/**
 * Competition page - Trading competitions and leaderboards
 */

import React, { useState } from 'react';
import { Card, CardHeader, CardTitle, CardContent, Button, Badge } from '../components/ui';
import AuthenticatedLayout from '../components/layout/AuthenticatedLayout';
import { useAuthStore } from '../store/authStore';
import { formatCurrency, formatPercentage, cn } from '../utils';

interface Competition {
  id: string;
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  participants: number;
  maxParticipants: number;
  prize: number;
  status: 'upcoming' | 'active' | 'completed';
  entryFee: number;
  difficulty: 'beginner' | 'intermediate' | 'advanced';
}

interface LeaderboardEntry {
  rank: number;
  userId: string;
  username: string;
  avatar?: string;
  return: number;
  returnPercentage: number;
  trades: number;
  winRate: number;
  isCurrentUser?: boolean;
}

const Competition: React.FC = () => {
  const { user } = useAuthStore();
  const [activeTab, setActiveTab] = useState<'competitions' | 'leaderboard' | 'my-results'>('competitions');

  // Mock data - replace with actual API calls
  const mockCompetitions: Competition[] = [
    {
      id: '1',
      name: 'Weekly Stock Challenge',
      description: 'Test your skills in stock trading with $100K virtual portfolio',
      startDate: '2024-01-15T00:00:00Z',
      endDate: '2024-01-22T23:59:59Z',
      participants: 847,
      maxParticipants: 1000,
      prize: 5000,
      status: 'active',
      entryFee: 0,
      difficulty: 'beginner'
    },
    {
      id: '2',
      name: 'Crypto Masters Tournament',
      description: 'Advanced cryptocurrency trading competition',
      startDate: '2024-01-20T00:00:00Z',
      endDate: '2024-02-20T23:59:59Z',
      participants: 234,
      maxParticipants: 500,
      prize: 25000,
      status: 'upcoming',
      entryFee: 50,
      difficulty: 'advanced'
    },
    {
      id: '3',
      name: 'Options Strategy Challenge',
      description: 'Master complex options strategies in this intermediate competition',
      startDate: '2024-01-01T00:00:00Z',
      endDate: '2024-01-31T23:59:59Z',
      participants: 156,
      maxParticipants: 300,
      prize: 10000,
      status: 'completed',
      entryFee: 25,
      difficulty: 'intermediate'
    }
  ];

  const mockLeaderboard: LeaderboardEntry[] = [
    { rank: 1, userId: '1', username: 'TradeWizard', return: 25847.50, returnPercentage: 25.85, trades: 142, winRate: 78.5 },
    { rank: 2, userId: '2', username: 'StockNinja', return: 22156.75, returnPercentage: 22.16, trades: 98, winRate: 72.4 },
    { rank: 3, userId: '3', username: 'BullMarketBoss', return: 18934.20, returnPercentage: 18.93, trades: 156, winRate: 68.9 },
    { rank: 4, userId: '4', username: 'CryptoKing', return: 16723.45, returnPercentage: 16.72, trades: 89, winRate: 75.3 },
    { rank: 15, userId: user?.id || '15', username: user?.username || 'You', return: 8456.30, returnPercentage: 8.46, trades: 67, winRate: 61.2, isCurrentUser: true },
  ];

  const getDifficultyColor = (difficulty: Competition['difficulty']) => {
    switch (difficulty) {
      case 'beginner':
        return 'bg-green-100 text-green-700';
      case 'intermediate':
        return 'bg-yellow-100 text-yellow-700';
      case 'advanced':
        return 'bg-red-100 text-red-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  };

  const getStatusColor = (status: Competition['status']) => {
    switch (status) {
      case 'upcoming':
        return 'bg-blue-100 text-blue-700';
      case 'active':
        return 'bg-green-100 text-green-700';
      case 'completed':
        return 'bg-gray-100 text-gray-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  };

  const renderCompetitionCard = (competition: Competition) => (
    <Card key={competition.id} className="hover:shadow-lg transition-shadow">
      <CardContent className="p-6">
        <div className="flex items-start justify-between mb-4">
          <div className="flex-1">
            <h3 className="text-lg font-semibold text-text-primary mb-2">{competition.name}</h3>
            <p className="text-text-tertiary text-sm mb-3">{competition.description}</p>
          </div>
          <div className="flex flex-col items-end space-y-2">
            <Badge className={getStatusColor(competition.status)}>
              {competition.status.charAt(0).toUpperCase() + competition.status.slice(1)}
            </Badge>
            <Badge className={getDifficultyColor(competition.difficulty)}>
              {competition.difficulty.charAt(0).toUpperCase() + competition.difficulty.slice(1)}
            </Badge>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4 mb-4">
          <div>
            <p className="text-sm text-text-tertiary">Prize Pool</p>
            <p className="font-semibold text-text-primary">{formatCurrency(competition.prize)}</p>
          </div>
          <div>
            <p className="text-sm text-text-tertiary">Entry Fee</p>
            <p className="font-semibold text-text-primary">
              {competition.entryFee === 0 ? 'Free' : formatCurrency(competition.entryFee)}
            </p>
          </div>
          <div>
            <p className="text-sm text-text-tertiary">Participants</p>
            <p className="font-semibold text-text-primary">
              {competition.participants}/{competition.maxParticipants}
            </p>
          </div>
          <div>
            <p className="text-sm text-text-tertiary">Duration</p>
            <p className="font-semibold text-text-primary">
              {Math.ceil((new Date(competition.endDate).getTime() - new Date(competition.startDate).getTime()) / (1000 * 60 * 60 * 24))} days
            </p>
          </div>
        </div>

        <div className="mb-4">
          <div className="flex justify-between text-sm text-text-tertiary mb-1">
            <span>Registration Progress</span>
            <span>{Math.round((competition.participants / competition.maxParticipants) * 100)}%</span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2">
            <div
              className="bg-brand-500 h-2 rounded-full"
              style={{ width: `${(competition.participants / competition.maxParticipants) * 100}%` }}
            ></div>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <div className="text-sm text-text-tertiary">
            {competition.status === 'upcoming' && `Starts ${new Date(competition.startDate).toLocaleDateString()}`}
            {competition.status === 'active' && `Ends ${new Date(competition.endDate).toLocaleDateString()}`}
            {competition.status === 'completed' && `Ended ${new Date(competition.endDate).toLocaleDateString()}`}
          </div>
          <div className="flex space-x-2">
            {competition.status === 'upcoming' && (
              <Button variant="primary" size="sm">Join Competition</Button>
            )}
            {competition.status === 'active' && (
              <Button variant="secondary" size="sm">View Details</Button>
            )}
            {competition.status === 'completed' && (
              <Button variant="secondary" size="sm">View Results</Button>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderLeaderboardTable = () => (
    <Card>
      <CardHeader>
        <CardTitle>Weekly Stock Challenge - Leaderboard</CardTitle>
        <p className="text-text-tertiary">Current standings for the active competition</p>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-border-default">
                <th className="text-left py-3 px-4 font-medium text-text-tertiary">Rank</th>
                <th className="text-left py-3 px-4 font-medium text-text-tertiary">Trader</th>
                <th className="text-right py-3 px-4 font-medium text-text-tertiary">Return</th>
                <th className="text-right py-3 px-4 font-medium text-text-tertiary">Return %</th>
                <th className="text-right py-3 px-4 font-medium text-text-tertiary">Trades</th>
                <th className="text-right py-3 px-4 font-medium text-text-tertiary">Win Rate</th>
              </tr>
            </thead>
            <tbody>
              {mockLeaderboard.map((entry) => (
                <tr
                  key={entry.userId}
                  className={cn(
                    'border-b border-border-light hover:bg-background-secondary',
                    entry.isCurrentUser ? 'bg-brand-50 border-brand-200' : ''
                  )}
                >
                  <td className="py-3 px-4">
                    <div className="flex items-center">
                      {entry.rank <= 3 && (
                        <span className="mr-2">
                          {entry.rank === 1 && '🥇'}
                          {entry.rank === 2 && '🥈'}
                          {entry.rank === 3 && '🥉'}
                        </span>
                      )}
                      <span className="font-semibold text-text-primary">#{entry.rank}</span>
                    </div>
                  </td>
                  <td className="py-3 px-4">
                    <div className="flex items-center space-x-3">
                      <div className="w-8 h-8 bg-brand-100 text-brand-600 rounded-full flex items-center justify-center text-sm font-medium">
                        {entry.username.charAt(0).toUpperCase()}
                      </div>
                      <div>
                        <div className={cn(
                          'font-medium',
                          entry.isCurrentUser ? 'text-brand-600' : 'text-text-primary'
                        )}>
                          {entry.username}
                          {entry.isCurrentUser && (
                            <Badge variant="default" className="ml-2 text-xs">You</Badge>
                          )}
                        </div>
                      </div>
                    </div>
                  </td>
                  <td className={cn(
                    'text-right py-3 px-4 font-semibold',
                    entry.return >= 0 ? 'text-positive-500' : 'text-negative-500'
                  )}>
                    {entry.return >= 0 ? '+' : ''}{formatCurrency(entry.return)}
                  </td>
                  <td className={cn(
                    'text-right py-3 px-4 font-semibold',
                    entry.returnPercentage >= 0 ? 'text-positive-500' : 'text-negative-500'
                  )}>
                    {entry.returnPercentage >= 0 ? '+' : ''}{formatPercentage(entry.returnPercentage)}
                  </td>
                  <td className="text-right py-3 px-4 text-text-primary">{entry.trades}</td>
                  <td className="text-right py-3 px-4 text-text-primary">{formatPercentage(entry.winRate)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );

  const renderMyResults = () => (
    <div className="space-y-6">
      {/* Performance Summary */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-text-primary">15</div>
              <div className="text-sm text-text-tertiary">Current Rank</div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-positive-500">+8.46%</div>
              <div className="text-sm text-text-tertiary">Total Return</div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-text-primary">67</div>
              <div className="text-sm text-text-tertiary">Total Trades</div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-text-primary">61.2%</div>
              <div className="text-sm text-text-tertiary">Win Rate</div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Competition History */}
      <Card>
        <CardHeader>
          <CardTitle>Competition History</CardTitle>
          <p className="text-text-tertiary">Your past competition results</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {[
              { name: 'Options Strategy Challenge', rank: 23, participants: 156, return: 12.4, prize: 0 },
              { name: 'December Crypto Rally', rank: 8, participants: 234, return: 18.7, prize: 500 },
              { name: 'Black Friday Trading', rank: 45, participants: 567, return: -3.2, prize: 0 },
            ].map((result, index) => (
              <div key={index} className="flex items-center justify-between p-3 rounded-lg hover:bg-background-secondary">
                <div>
                  <div className="font-medium text-text-primary">{result.name}</div>
                  <div className="text-sm text-text-tertiary">
                    Rank #{result.rank} of {result.participants} participants
                  </div>
                </div>
                <div className="text-right">
                  <div className={cn(
                    'font-semibold',
                    result.return >= 0 ? 'text-positive-500' : 'text-negative-500'
                  )}>
                    {result.return >= 0 ? '+' : ''}{formatPercentage(result.return)}
                  </div>
                  <div className="text-sm text-text-tertiary">
                    {result.prize > 0 ? `Prize: ${formatCurrency(result.prize)}` : 'No prize'}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );

  return (
    <AuthenticatedLayout>
      <div className="space-y-6">
        {/* Page Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-text-primary">Competition</h1>
            <p className="text-text-tertiary mt-1">
              Join trading competitions and compete with other traders
            </p>
          </div>
          <Button variant="primary">
            Create Competition
          </Button>
        </div>

        {/* Tabs */}
        <div className="flex items-center space-x-4 border-b border-border-default">
          {[
            { key: 'competitions', label: 'Competitions' },
            { key: 'leaderboard', label: 'Leaderboard' },
            { key: 'my-results', label: 'My Results' }
          ].map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key as any)}
              className={cn(
                'pb-3 px-1 text-sm font-medium border-b-2 transition-colors',
                activeTab === tab.key
                  ? 'border-brand-500 text-brand-600'
                  : 'border-transparent text-text-tertiary hover:text-text-primary'
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Tab Content */}
        {activeTab === 'competitions' && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {mockCompetitions.map(renderCompetitionCard)}
          </div>
        )}

        {activeTab === 'leaderboard' && renderLeaderboardTable()}

        {activeTab === 'my-results' && renderMyResults()}
      </div>
    </AuthenticatedLayout>
  );
};

export default Competition;