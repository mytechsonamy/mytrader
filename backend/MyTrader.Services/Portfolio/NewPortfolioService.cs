using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using MyTrader.Core.DTOs.Portfolio;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Services.Portfolio;

public class NewPortfolioService : IPortfolioService
{
    private readonly TradingDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NewPortfolioService> _logger;
    
    private const string PORTFOLIO_CACHE_PREFIX = "portfolio_";
    private const string POSITIONS_CACHE_PREFIX = "positions_";
    private const string PERFORMANCE_CACHE_PREFIX = "performance_";
    
    public NewPortfolioService(
        TradingDbContext context,
        IMemoryCache cache,
        ILogger<NewPortfolioService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PortfolioSummaryDto?> GetPortfolioAsync(Guid userId, Guid? portfolioId = null)
    {
        try
        {
            var portfolio = portfolioId.HasValue
                ? await _context.UserPortfolios
                    .Include(p => p.Positions)
                    .ThenInclude(pos => pos.Symbol)
                    .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId)
                : await _context.UserPortfolios
                    .Include(p => p.Positions)
                    .ThenInclude(pos => pos.Symbol)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.IsDefault);

            if (portfolio == null)
                return null;

            var positions = portfolio.Positions.Select(pos => new PortfolioPositionDto
            {
                Id = pos.Id,
                Symbol = pos.Symbol.Ticker,
                SymbolName = pos.Symbol.FullName ?? pos.Symbol.Ticker,
                Quantity = pos.Quantity,
                AveragePrice = pos.AveragePrice,
                CurrentPrice = pos.CurrentPrice,
                MarketValue = pos.MarketValue,
                UnrealizedPnL = pos.UnrealizedPnL,
                UnrealizedPnLPercent = pos.UnrealizedPnLPercent,
                CostBasis = pos.CostBasis,
                Weight = portfolio.CurrentValue > 0 ? (pos.MarketValue / portfolio.CurrentValue) * 100 : 0,
                LastTradedAt = pos.LastTradedAt
            }).ToList();

            return new PortfolioSummaryDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                BaseCurrency = portfolio.BaseCurrency,
                InitialCapital = portfolio.InitialCapital,
                CurrentValue = portfolio.CurrentValue,
                CashBalance = portfolio.CashBalance,
                TotalPnL = portfolio.TotalPnL,
                DailyPnL = portfolio.DailyPnL,
                TotalReturnPercent = portfolio.TotalReturnPercent,
                LastUpdated = portfolio.UpdatedAt,
                Positions = positions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio for user {UserId}, portfolioId {PortfolioId}", userId, portfolioId);
            return null;
        }
    }

    public async Task<List<PortfolioSummaryDto>> GetUserPortfoliosAsync(Guid userId)
    {
        try
        {
            var portfolios = await _context.UserPortfolios
                .Include(p => p.Positions)
                .ThenInclude(pos => pos.Symbol)
                .Where(p => p.UserId == userId && p.IsActive)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return portfolios.Select(portfolio => new PortfolioSummaryDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                BaseCurrency = portfolio.BaseCurrency,
                InitialCapital = portfolio.InitialCapital,
                CurrentValue = portfolio.CurrentValue,
                CashBalance = portfolio.CashBalance,
                TotalPnL = portfolio.TotalPnL,
                DailyPnL = portfolio.DailyPnL,
                TotalReturnPercent = portfolio.TotalReturnPercent,
                LastUpdated = portfolio.UpdatedAt,
                Positions = portfolio.Positions.Select(pos => new PortfolioPositionDto
                {
                    Id = pos.Id,
                    Symbol = pos.Symbol.Ticker,
                    SymbolName = pos.Symbol.FullName ?? pos.Symbol.Ticker,
                    Quantity = pos.Quantity,
                    AveragePrice = pos.AveragePrice,
                    CurrentPrice = pos.CurrentPrice,
                    MarketValue = pos.MarketValue,
                    UnrealizedPnL = pos.UnrealizedPnL,
                    UnrealizedPnLPercent = pos.UnrealizedPnLPercent,
                    CostBasis = pos.CostBasis,
                    Weight = portfolio.CurrentValue > 0 ? (pos.MarketValue / portfolio.CurrentValue) * 100 : 0,
                    LastTradedAt = pos.LastTradedAt
                }).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolios for user {UserId}", userId);
            return new List<PortfolioSummaryDto>();
        }
    }

    public async Task<PortfolioSummaryDto> CreatePortfolioAsync(Guid userId, CreatePortfolioDto createDto)
    {
        try
        {
            // Check if this will be the first portfolio (make it default)
            var hasExistingPortfolios = await _context.UserPortfolios
                .AnyAsync(p => p.UserId == userId && p.IsActive);

            var portfolio = new UserPortfolio
            {
                UserId = userId,
                Name = createDto.Name,
                Description = createDto.Description,
                BaseCurrency = createDto.BaseCurrency,
                InitialCapital = createDto.InitialCapital,
                CurrentValue = createDto.InitialCapital,
                CashBalance = createDto.InitialCapital,
                IsDefault = !hasExistingPortfolios, // First portfolio becomes default
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserPortfolios.Add(portfolio);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created portfolio {PortfolioId} for user {UserId}", portfolio.Id, userId);

            return new PortfolioSummaryDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                BaseCurrency = portfolio.BaseCurrency,
                InitialCapital = portfolio.InitialCapital,
                CurrentValue = portfolio.CurrentValue,
                CashBalance = portfolio.CashBalance,
                TotalPnL = portfolio.TotalPnL,
                DailyPnL = portfolio.DailyPnL,
                TotalReturnPercent = portfolio.TotalReturnPercent,
                LastUpdated = portfolio.UpdatedAt,
                Positions = new List<PortfolioPositionDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating portfolio for user {UserId}", userId);
            throw;
        }
    }

    // Stub implementations for now - will implement in next iteration
    public async Task<PortfolioSummaryDto?> UpdatePortfolioAsync(Guid userId, Guid portfolioId, UpdatePortfolioDto updateDto)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<bool> SetDefaultPortfolioAsync(Guid userId, Guid portfolioId)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<List<PortfolioPositionDto>> GetPositionsAsync(Guid userId, Guid? portfolioId = null)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<PortfolioPositionDto?> GetPositionAsync(Guid userId, Guid portfolioId, Guid symbolId)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto createDto)
    {
        try
        {
            // Get portfolio and validate ownership
            var portfolio = await _context.UserPortfolios
                .FirstOrDefaultAsync(p => p.Id == createDto.PortfolioId && p.UserId == userId);
            
            if (portfolio == null)
                throw new UnauthorizedAccessException("Portfolio not found or access denied");

            // Get symbol
            var symbol = await _context.Symbols
                .FirstOrDefaultAsync(s => s.Id == createDto.SymbolId);
            
            if (symbol == null)
                throw new ArgumentException("Symbol not found");

            var transaction = new Transaction
            {
                PortfolioId = createDto.PortfolioId,
                SymbolId = createDto.SymbolId,
                TransactionType = createDto.TransactionType,
                Side = createDto.Side,
                Quantity = createDto.Quantity,
                Price = createDto.Price,
                TotalAmount = createDto.Quantity * createDto.Price,
                Fee = createDto.Fee,
                Currency = createDto.Currency,
                Status = "Completed",
                ExecutedAt = createDto.ExecutedAt ?? DateTime.UtcNow,
                Notes = createDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Update portfolio position
            await UpdatePortfolioPositionAfterTransaction(portfolio.Id, createDto.SymbolId, transaction);

            _logger.LogInformation("Created transaction {TransactionId} for portfolio {PortfolioId}", 
                transaction.Id, portfolio.Id);

            return new TransactionDto
            {
                Id = transaction.Id,
                Symbol = symbol.Ticker,
                SymbolName = symbol.FullName ?? symbol.Ticker,
                TransactionType = transaction.TransactionType,
                Side = transaction.Side,
                Quantity = transaction.Quantity,
                Price = transaction.Price,
                TotalAmount = transaction.TotalAmount,
                Fee = transaction.Fee,
                Currency = transaction.Currency,
                Status = transaction.Status,
                ExecutedAt = transaction.ExecutedAt,
                Notes = transaction.Notes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction for user {UserId}", userId);
            throw;
        }
    }

    private async Task UpdatePortfolioPositionAfterTransaction(Guid portfolioId, Guid symbolId, Transaction transaction)
    {
        try
        {
            var position = await _context.PortfolioPositions
                .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId && p.SymbolId == symbolId);

            if (position == null)
            {
                // Create new position
                position = new PortfolioPosition
                {
                    PortfolioId = portfolioId,
                    SymbolId = symbolId,
                    Quantity = transaction.Side == "BUY" ? transaction.Quantity : -transaction.Quantity,
                    AveragePrice = transaction.Price,
                    CurrentPrice = transaction.Price,
                    CostBasis = transaction.TotalAmount + transaction.Fee,
                    MarketValue = transaction.Quantity * transaction.Price,
                    UnrealizedPnL = 0m,
                    UnrealizedPnLPercent = 0m,
                    RealizedPnL = transaction.Side == "SELL" ? transaction.TotalAmount - transaction.Fee : 0m,
                    LastTradedAt = transaction.ExecutedAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.PortfolioPositions.Add(position);
            }
            else
            {
                // Update existing position
                if (transaction.Side == "BUY")
                {
                    var newQuantity = position.Quantity + transaction.Quantity;
                    var newCostBasis = position.CostBasis + transaction.TotalAmount + transaction.Fee;
                    
                    position.AveragePrice = newCostBasis / newQuantity;
                    position.Quantity = newQuantity;
                    position.CostBasis = newCostBasis;
                }
                else if (transaction.Side == "SELL")
                {
                    position.Quantity -= transaction.Quantity;
                    position.RealizedPnL += transaction.TotalAmount - transaction.Fee - 
                        (transaction.Quantity * position.AveragePrice);
                    
                    if (position.Quantity <= 0)
                    {
                        _context.PortfolioPositions.Remove(position);
                        return;
                    }
                }

                position.MarketValue = position.Quantity * position.CurrentPrice;
                position.UnrealizedPnL = position.MarketValue - (position.Quantity * position.AveragePrice);
                position.UnrealizedPnLPercent = position.CostBasis > 0 ? 
                    (position.UnrealizedPnL / position.CostBasis) * 100 : 0m;
                position.LastTradedAt = transaction.ExecutedAt;
                position.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portfolio position for portfolio {PortfolioId}, symbol {SymbolId}", 
                portfolioId, symbolId);
            throw;
        }
    }

    public async Task<TransactionHistoryResponseDto> GetTransactionHistoryAsync(Guid userId, TransactionHistoryRequestDto request)
    {
        try
        {
            var query = _context.Transactions
                .Include(t => t.Portfolio)
                .Include(t => t.Symbol)
                .Where(t => t.Portfolio.UserId == userId);

            // Apply filters
            if (request.PortfolioId.HasValue)
                query = query.Where(t => t.PortfolioId == request.PortfolioId);

            if (request.SymbolId.HasValue)
                query = query.Where(t => t.SymbolId == request.SymbolId);

            if (!string.IsNullOrEmpty(request.TransactionType))
                query = query.Where(t => t.TransactionType == request.TransactionType);

            if (request.FromDate.HasValue)
                query = query.Where(t => t.ExecutedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(t => t.ExecutedAt <= request.ToDate);

            var totalCount = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.ExecutedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Symbol = t.Symbol.Ticker,
                    SymbolName = t.Symbol.FullName ?? t.Symbol.Ticker,
                    TransactionType = t.TransactionType,
                    Side = t.Side,
                    Quantity = t.Quantity,
                    Price = t.Price,
                    TotalAmount = t.TotalAmount,
                    Fee = t.Fee,
                    Currency = t.Currency,
                    Status = t.Status,
                    OrderId = t.OrderId,
                    Notes = t.Notes,
                    ExecutedAt = t.ExecutedAt
                })
                .ToListAsync();

            return new TransactionHistoryResponseDto
            {
                Transactions = transactions,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetRecentTransactionsAsync(Guid userId, int count = 10)
    {
        try
        {
            var transactions = await _context.Transactions
                .Include(t => t.Portfolio)
                .Include(t => t.Symbol)
                .Where(t => t.Portfolio.UserId == userId)
                .OrderByDescending(t => t.ExecutedAt)
                .Take(count)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Symbol = t.Symbol.Ticker,
                    SymbolName = t.Symbol.FullName ?? t.Symbol.Ticker,
                    TransactionType = t.TransactionType,
                    Side = t.Side,
                    Quantity = t.Quantity,
                    Price = t.Price,
                    TotalAmount = t.TotalAmount,
                    Fee = t.Fee,
                    Currency = t.Currency,
                    Status = t.Status,
                    OrderId = t.OrderId,
                    Notes = t.Notes,
                    ExecutedAt = t.ExecutedAt
                })
                .ToListAsync();

            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent transactions for user {UserId}", userId);
            return new List<TransactionDto>();
        }
    }

    public async Task<PortfolioPerformanceDto> GetPerformanceAsync(Guid userId, Guid? portfolioId = null)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<List<PortfolioAllocationDto>> GetAssetAllocationAsync(Guid userId, Guid? portfolioId = null)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task UpdatePortfolioValuesAsync(Guid portfolioId)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task UpdateAllPortfolioValuesAsync()
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<decimal> CalculatePortfolioValueAsync(Guid portfolioId)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<decimal> GetTotalPortfolioValueAsync(Guid userId)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<decimal> GetDailyPnLAsync(Guid userId, Guid? portfolioId = null)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    public async Task<Dictionary<string, decimal>> GetPortfolioMetricsAsync(Guid userId, Guid? portfolioId = null)
    {
        throw new NotImplementedException("Will implement in next iteration");
    }

    // Analytics Methods
    public Task<PortfolioAnalyticsDto> GetPortfolioAnalyticsAsync(Guid userId, AnalyticsRequestDto request)
    {
        throw new NotImplementedException("Analytics features will be implemented in next iteration");
    }

    public Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException("Performance metrics will be implemented in next iteration");
    }

    public Task<RiskMetricsDto> GetRiskMetricsAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException("Risk metrics will be implemented in next iteration");
    }

    public Task<List<AllocationDto>> GetAssetAllocationDetailAsync(Guid userId, Guid portfolioId)
    {
        throw new NotImplementedException("Asset allocation analysis will be implemented in next iteration");
    }

    public Task<List<AllocationDto>> GetSectorAllocationAsync(Guid userId, Guid portfolioId)
    {
        throw new NotImplementedException("Sector allocation analysis will be implemented in next iteration");
    }

    public Task<PortfolioComparisonDto> ComparePortfoliosAsync(Guid userId, List<Guid> portfolioIds, DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException("Portfolio comparison will be implemented in next iteration");
    }

    public Task<PortfolioOptimizationDto> GetPortfolioOptimizationAsync(Guid userId, Guid portfolioId, string optimizationType = "MaxSharpe")
    {
        throw new NotImplementedException("Portfolio optimization will be implemented in next iteration");
    }

    // Export & Reporting Methods
    public Task<ExportResponseDto> ExportPortfolioAsync(Guid userId, ExportRequestDto request)
    {
        throw new NotImplementedException("Portfolio export will be implemented in next iteration");
    }

    public Task<PortfolioReportDto> GeneratePortfolioReportAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException("Portfolio reporting will be implemented in next iteration");
    }

    public Task<byte[]> ExportTransactionHistoryAsync(Guid userId, Guid portfolioId, string format = "CSV", DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException("Transaction history export will be implemented in next iteration");
    }

    public Task<byte[]> ExportPerformanceReportAsync(Guid userId, Guid portfolioId, string format = "PDF", DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException("Performance report export will be implemented in next iteration");
    }

    public Task<CsvExportDto> GeneratePortfolioCsvAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException("CSV export will be implemented in next iteration");
    }

    public Task<PdfExportDto> GeneratePortfolioPdfAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException("PDF export will be implemented in next iteration");
    }
}