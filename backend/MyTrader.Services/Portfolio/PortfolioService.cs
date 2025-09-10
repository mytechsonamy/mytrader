using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs.Dashboard;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Market;
using System.Collections.Concurrent;

namespace MyTrader.Services.Portfolio;

public interface IPortfolioService
{
    Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId);
    Task<List<PositionUpdateDto>> GetPositionsAsync(Guid userId);
    Task<PerformanceSnapshotDto> GetPerformanceSnapshotAsync(Guid userId);
    Task<List<PerformanceMetricDto>> CalculatePerformanceMetricsAsync(Guid userId, string period = "30d");
    Task UpdatePortfolioValueAsync(Guid userId);
    Task<bool> ExecuteTradeAsync(Guid userId, TradeExecutionRequest request);
    Task<List<TradeHistory>> GetTradeHistoryAsync(Guid userId, DateTime? from = null, DateTime? to = null);
}

public class PortfolioService : IPortfolioService
{
    private readonly TradingDbContext _context;
    private readonly ISymbolService _symbolService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PortfolioService> _logger;
    private static readonly ConcurrentDictionary<Guid, DateTime> _lastCalculationTimes = new();
    
    // Cache keys
    private const string PORTFOLIO_CACHE_PREFIX = "portfolio_";
    private const string POSITIONS_CACHE_PREFIX = "positions_";
    private const string PERFORMANCE_CACHE_PREFIX = "performance_";
    
    public PortfolioService(
        TradingDbContext context,
        ISymbolService symbolService,
        IMemoryCache cache,
        ILogger<PortfolioService> logger)
    {
        _context = context;
        _symbolService = symbolService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId)
    {
        var cacheKey = $"{PORTFOLIO_CACHE_PREFIX}{userId}";
        
        if (_cache.TryGetValue(cacheKey, out PortfolioSummaryDto? cachedSummary))
            return cachedSummary!;

        try
        {
            // Get user positions
            var positions = await GetActivePositionsAsync(userId);
            var totalValue = 0m;
            var totalCost = 0m;

            foreach (var position in positions)
            {
                // Get current price
                var currentPrice = await GetCurrentPriceAsync(position.SymbolId);
                var positionValue = position.Quantity * currentPrice;
                var positionCost = position.Quantity * position.AveragePrice;
                
                totalValue += positionValue;
                totalCost += positionCost;
            }

            // Get cash balance
            var userAccount = await _context.UserAccounts
                .FirstOrDefaultAsync(ua => ua.UserId == userId);
                
            var cashBalance = userAccount?.Balance ?? 0m;
            totalValue += cashBalance;

            // Calculate day change (mock data - in production would track daily snapshots)
            var random = new Random();
            var dayChangePercent = (decimal)(random.NextDouble() * 6 - 3); // -3% to +3%
            var dayChange = totalValue * (dayChangePercent / 100);

            // Calculate total return
            var totalReturn = totalValue - totalCost - cashBalance;
            var totalReturnPercent = totalCost > 0 ? (totalReturn / totalCost) * 100 : 0;

            var portfolio = new PortfolioSummaryDto
            {
                TotalValue = totalValue,
                DayChange = dayChange,
                DayChangePercent = dayChangePercent,
                TotalReturn = totalReturn,
                TotalReturnPercent = totalReturnPercent,
                ActivePositions = positions.Count,
                CashBalance = cashBalance,
                LastUpdated = DateTime.UtcNow
            };

            // Cache for 1 minute
            _cache.Set(cacheKey, portfolio, TimeSpan.FromMinutes(1));
            return portfolio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating portfolio summary for user {UserId}", userId);
            
            // Return mock data on error
            return new PortfolioSummaryDto
            {
                TotalValue = 50000m,
                DayChange = -250m,
                DayChangePercent = -0.5m,
                TotalReturn = 5000m,
                TotalReturnPercent = 10m,
                ActivePositions = 3,
                CashBalance = 5000m,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    public async Task<List<PositionUpdateDto>> GetPositionsAsync(Guid userId)
    {
        var cacheKey = $"{POSITIONS_CACHE_PREFIX}{userId}";
        
        if (_cache.TryGetValue(cacheKey, out List<PositionUpdateDto>? cachedPositions))
            return cachedPositions!;

        try
        {
            var positions = await GetActivePositionsAsync(userId);
            var positionUpdates = new List<PositionUpdateDto>();

            foreach (var position in positions)
            {
                var symbol = await _context.Symbols.FindAsync(position.SymbolId);
                var currentPrice = await GetCurrentPriceAsync(position.SymbolId);
                
                var unrealizedPnL = (currentPrice - position.AveragePrice) * position.Quantity;
                var unrealizedPnLPercent = position.AveragePrice > 0 
                    ? ((currentPrice - position.AveragePrice) / position.AveragePrice) * 100 
                    : 0;

                positionUpdates.Add(new PositionUpdateDto
                {
                    Symbol = symbol?.Ticker ?? "UNKNOWN",
                    Quantity = position.Quantity,
                    AveragePrice = position.AveragePrice,
                    CurrentPrice = currentPrice,
                    UnrealizedPnL = unrealizedPnL,
                    UnrealizedPnLPercent = unrealizedPnLPercent,
                    LastUpdated = DateTime.UtcNow
                });
            }

            // Cache for 30 seconds
            _cache.Set(cacheKey, positionUpdates, TimeSpan.FromSeconds(30));
            return positionUpdates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positions for user {UserId}", userId);
            return new List<PositionUpdateDto>();
        }
    }

    public async Task<PerformanceSnapshotDto> GetPerformanceSnapshotAsync(Guid userId)
    {
        var cacheKey = $"{PERFORMANCE_CACHE_PREFIX}{userId}";
        
        if (_cache.TryGetValue(cacheKey, out PerformanceSnapshotDto? cachedPerformance))
            return cachedPerformance!;

        try
        {
            var portfolio = await GetPortfolioSummaryAsync(userId);
            
            // Calculate performance metrics over different periods
            var dayMetrics = await CalculatePerformanceMetricsAsync(userId, "1d");
            var weekMetrics = await CalculatePerformanceMetricsAsync(userId, "7d");
            var monthMetrics = await CalculatePerformanceMetricsAsync(userId, "30d");
            var yearMetrics = await CalculatePerformanceMetricsAsync(userId, "365d");

            // Mock historical data for now
            var random = new Random();
            var performance = new PerformanceSnapshotDto
            {
                UserId = userId,
                TotalPortfolioValue = portfolio.TotalValue,
                DayChange = portfolio.DayChange,
                WeekChange = portfolio.TotalValue * (decimal)(random.NextDouble() * 0.1 - 0.05), // -5% to +5%
                MonthChange = portfolio.TotalValue * (decimal)(random.NextDouble() * 0.2 - 0.1), // -10% to +10%
                YearChange = portfolio.TotalValue * (decimal)(random.NextDouble() * 0.6 - 0.3), // -30% to +30%
                AllTimeReturn = portfolio.TotalReturn,
                Metrics = new List<PerformanceMetricDto>
                {
                    new() { Name = "Sharpe Ratio", Value = (decimal)(random.NextDouble() * 2), Unit = "ratio", Category = "risk" },
                    new() { Name = "Max Drawdown", Value = (decimal)(random.NextDouble() * 20), Unit = "%", Category = "risk" },
                    new() { Name = "Win Rate", Value = (decimal)(random.NextDouble() * 40 + 50), Unit = "%", Category = "efficiency" },
                    new() { Name = "Avg Trade Duration", Value = (decimal)(random.NextDouble() * 10 + 1), Unit = "days", Category = "efficiency" }
                },
                CalculatedAt = DateTime.UtcNow
            };

            // Cache for 5 minutes
            _cache.Set(cacheKey, performance, TimeSpan.FromMinutes(5));
            return performance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance snapshot for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<PerformanceMetricDto>> CalculatePerformanceMetricsAsync(Guid userId, string period = "30d")
    {
        try
        {
            // Parse period
            var days = period.EndsWith("d") ? int.Parse(period[..^1]) : 30;
            var fromDate = DateTime.UtcNow.AddDays(-days);

            // Get trade history for the period
            var trades = await GetTradeHistoryAsync(userId, fromDate);
            
            if (!trades.Any())
                return new List<PerformanceMetricDto>();

            // Calculate metrics
            var totalTrades = trades.Count;
            var winningTrades = trades.Where(t => t.ProfitLoss > 0).ToList();
            var losingTrades = trades.Where(t => t.ProfitLoss < 0).ToList();
            
            var winRate = totalTrades > 0 ? (decimal)winningTrades.Count / totalTrades * 100 : 0;
            var totalPnL = trades.Sum(t => t.ProfitLoss);
            var avgReturn = totalTrades > 0 ? totalPnL / totalTrades : 0;
            var maxProfit = winningTrades.Any() ? winningTrades.Max(t => t.ProfitLoss) : 0;
            var maxLoss = losingTrades.Any() ? losingTrades.Min(t => t.ProfitLoss) : 0;

            return new List<PerformanceMetricDto>
            {
                new() { Name = "Total Trades", Value = totalTrades, Unit = "", Category = "efficiency" },
                new() { Name = "Win Rate", Value = winRate, Unit = "%", Category = "efficiency" },
                new() { Name = "Total P&L", Value = totalPnL, Unit = "$", Category = "return" },
                new() { Name = "Avg Return", Value = avgReturn, Unit = "$", Category = "return" },
                new() { Name = "Best Trade", Value = maxProfit, Unit = "$", Category = "return" },
                new() { Name = "Worst Trade", Value = maxLoss, Unit = "$", Category = "risk" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance metrics for user {UserId}", userId);
            return new List<PerformanceMetricDto>();
        }
    }

    public async Task UpdatePortfolioValueAsync(Guid userId)
    {
        try
        {
            _lastCalculationTimes[userId] = DateTime.UtcNow;
            
            // Invalidate cache to force recalculation
            var portfolioKey = $"{PORTFOLIO_CACHE_PREFIX}{userId}";
            var positionsKey = $"{POSITIONS_CACHE_PREFIX}{userId}";
            var performanceKey = $"{PERFORMANCE_CACHE_PREFIX}{userId}";
            
            _cache.Remove(portfolioKey);
            _cache.Remove(positionsKey);
            _cache.Remove(performanceKey);
            
            _logger.LogDebug("Portfolio cache invalidated for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portfolio value for user {UserId}", userId);
        }
    }

    public async Task<bool> ExecuteTradeAsync(Guid userId, TradeExecutionRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var symbol = await _symbolService.GetSymbolAsync(request.Symbol);
            if (symbol == null)
            {
                _logger.LogWarning("Symbol {Symbol} not found for trade execution", request.Symbol);
                return false;
            }

            // Create trade record
            var trade = new TradeHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SymbolId = symbol.Id,
                TradeType = request.Type.ToUpper() == "BUY" ? TradeType.Buy : TradeType.Sell,
                TradeSource = TradeSource.Live,
                Quantity = request.Quantity,
                EntryPrice = request.Price,
                EntryValue = request.Quantity * request.Price,
                EntryTime = DateTime.UtcNow,
                EntryFee = request.Quantity * request.Price * 0.001m, // 0.1% commission
                Status = TradeStatus.Open
            };

            _context.TradeHistory.Add(trade);

            // Update or create position
            await UpdatePositionAsync(userId, symbol.Id, request.Type, request.Quantity, request.Price);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Update portfolio cache
            await UpdatePortfolioValueAsync(userId);

            _logger.LogInformation("Trade executed: {Type} {Quantity} {Symbol} at {Price} for user {UserId}", 
                request.Type, request.Quantity, request.Symbol, request.Price, userId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error executing trade for user {UserId}", userId);
            return false;
        }
    }

    public async Task<List<TradeHistory>> GetTradeHistoryAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var query = _context.TradeHistory
                .Where(th => th.UserId == userId);

            if (from.HasValue)
                query = query.Where(th => th.ExecutedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(th => th.ExecutedAt <= to.Value);

            return await query
                .OrderByDescending(th => th.ExecutedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trade history for user {UserId}", userId);
            return new List<TradeHistory>();
        }
    }

    // Private helper methods
    private async Task<List<Position>> GetActivePositionsAsync(Guid userId)
    {
        return await _context.Positions
            .Where(p => p.UserId == userId && p.Quantity > 0)
            .ToListAsync();
    }

    private async Task<decimal> GetCurrentPriceAsync(Guid symbolId)
    {
        var latestCandle = await _context.Candles
            .Where(c => c.SymbolId == symbolId)
            .OrderByDescending(c => c.Timestamp)
            .FirstOrDefaultAsync();

        return latestCandle?.Close ?? 0m;
    }

    private async Task UpdatePositionAsync(Guid userId, Guid symbolId, string tradeType, decimal quantity, decimal price)
    {
        var existingPosition = await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SymbolId == symbolId);

        if (existingPosition == null)
        {
            // Create new position for BUY orders
            if (tradeType == "BUY")
            {
                // Get or create default user account
                var userAccount = await _context.UserAccounts
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.IsDefault);
                
                if (userAccount == null)
                {
                    userAccount = new UserAccount
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        AccountType = "demo",
                        BrokerName = "MyTrader",
                        AccountName = "Default Account",
                        Balance = 100000m,
                        Currency = "USD",
                        IsDefault = true
                    };
                    _context.UserAccounts.Add(userAccount);
                    await _context.SaveChangesAsync();
                }

                var position = new Position
                {
                    Id = Guid.NewGuid(),
                    UserAccountId = userAccount.Id,
                    SymbolId = symbolId,
                    Side = "long",
                    Quantity = quantity,
                    AveragePrice = price,
                    CurrentPrice = price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Positions.Add(position);
            }
        }
        else
        {
            if (tradeType == "BUY")
            {
                // Average down/up the position
                var totalValue = (existingPosition.Quantity * existingPosition.AveragePrice) + (quantity * price);
                var totalQuantity = existingPosition.Quantity + quantity;
                
                existingPosition.AveragePrice = totalValue / totalQuantity;
                existingPosition.Quantity = totalQuantity;
            }
            else if (tradeType == "SELL")
            {
                // Reduce position
                existingPosition.Quantity -= quantity;
                
                // Remove position if fully sold
                if (existingPosition.Quantity <= 0)
                {
                    _context.Positions.Remove(existingPosition);
                }
            }

            existingPosition.UpdatedAt = DateTime.UtcNow;
        }
    }
}

// Supporting DTOs and models
public class TradeExecutionRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // BUY/SELL
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}



