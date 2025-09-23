using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Service implementation for market management
/// </summary>
public class MarketService : IMarketService
{
    private readonly TradingDbContext _context;
    private readonly ILogger<MarketService> _logger;

    public MarketService(TradingDbContext context, ILogger<MarketService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MarketSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var markets = await _context.Markets
                .Include(m => m.AssetClass)
                .Include(m => m.Symbols)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync(cancellationToken);

            return markets.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all markets");
            throw;
        }
    }

    public async Task<List<MarketSummaryDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var markets = await _context.Markets
                .Include(m => m.AssetClass)
                .Include(m => m.Symbols)
                .Where(m => m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync(cancellationToken);

            return markets.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active markets");
            throw;
        }
    }

    public async Task<List<MarketSummaryDto>> GetByAssetClassAsync(Guid assetClassId, CancellationToken cancellationToken = default)
    {
        try
        {
            var markets = await _context.Markets
                .Include(m => m.AssetClass)
                .Include(m => m.Symbols)
                .Where(m => m.AssetClassId == assetClassId)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync(cancellationToken);

            return markets.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting markets by asset class: {AssetClassId}", assetClassId);
            throw;
        }
    }

    public async Task<MarketDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var market = await _context.Markets
                .Include(m => m.AssetClass)
                .Include(m => m.TradingSessions)
                .Include(m => m.DataProviders)
                .Include(m => m.Symbols)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            return market != null ? MapToDto(market) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market by ID: {MarketId}", id);
            throw;
        }
    }

    public async Task<MarketDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var market = await _context.Markets
                .Include(m => m.AssetClass)
                .Include(m => m.TradingSessions)
                .Include(m => m.DataProviders)
                .Include(m => m.Symbols)
                .FirstOrDefaultAsync(m => m.Code.ToUpper() == code.ToUpper(), cancellationToken);

            return market != null ? MapToDto(market) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market by code: {Code}", code);
            throw;
        }
    }

    public async Task<MarketStatusDto?> GetMarketStatusAsync(Guid marketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var market = await _context.Markets
                .FirstOrDefaultAsync(m => m.Id == marketId, cancellationToken);

            if (market == null)
                return null;

            return new MarketStatusDto
            {
                MarketId = market.Id,
                Code = market.Code,
                Name = market.Name,
                Status = market.Status,
                StatusUpdatedAt = market.StatusUpdatedAt ?? DateTime.UtcNow,
                IsActive = market.IsActive,
                // TODO: Calculate next session times based on trading sessions
                CurrentSessionType = DetermineCurrentSessionType(market)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market status: {MarketId}", marketId);
            throw;
        }
    }

    public async Task<List<MarketStatusDto>> GetAllMarketStatusesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var markets = await _context.Markets
                .Where(m => m.IsActive)
                .ToListAsync(cancellationToken);

            return markets.Select(market => new MarketStatusDto
            {
                MarketId = market.Id,
                Code = market.Code,
                Name = market.Name,
                Status = market.Status,
                StatusUpdatedAt = market.StatusUpdatedAt ?? DateTime.UtcNow,
                IsActive = market.IsActive,
                CurrentSessionType = DetermineCurrentSessionType(market)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all market statuses");
            throw;
        }
    }

    public async Task<MarketDto> CreateAsync(CreateMarketRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if code already exists
            var existingMarket = await _context.Markets
                .FirstOrDefaultAsync(m => m.Code.ToUpper() == request.Code.ToUpper(), cancellationToken);

            if (existingMarket != null)
            {
                throw new InvalidOperationException($"Market with code '{request.Code}' already exists");
            }

            // Verify asset class exists
            var assetClass = await _context.AssetClasses
                .FirstOrDefaultAsync(ac => ac.Id == request.AssetClassId, cancellationToken);

            if (assetClass == null)
            {
                throw new InvalidOperationException($"Asset class with ID '{request.AssetClassId}' does not exist");
            }

            var market = new Market
            {
                Code = request.Code.ToUpper(),
                Name = request.Name,
                NameTurkish = request.NameTurkish,
                Description = request.Description,
                AssetClassId = request.AssetClassId,
                CountryCode = request.CountryCode.ToUpper(),
                Timezone = request.Timezone,
                PrimaryCurrency = request.PrimaryCurrency,
                MarketMaker = request.MarketMaker,
                ApiBaseUrl = request.ApiBaseUrl,
                WebSocketUrl = request.WebSocketUrl,
                DefaultCommissionRate = request.DefaultCommissionRate,
                MinCommission = request.MinCommission,
                HasRealtimeData = request.HasRealtimeData,
                DataDelayMinutes = request.DataDelayMinutes,
                DisplayOrder = request.DisplayOrder,
                Status = "UNKNOWN",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Markets.Add(market);
            await _context.SaveChangesAsync(cancellationToken);

            // Reload with includes
            market = await _context.Markets
                .Include(m => m.AssetClass)
                .Include(m => m.TradingSessions)
                .Include(m => m.DataProviders)
                .Include(m => m.Symbols)
                .FirstAsync(m => m.Id == market.Id, cancellationToken);

            _logger.LogInformation("Created new market: {Code} - {Name}", market.Code, market.Name);

            return MapToDto(market);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating market: {Code}", request.Code);
            throw;
        }
    }

    public async Task<MarketDto?> UpdateAsync(Guid id, UpdateMarketRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var market = await _context.Markets
                .Include(m => m.AssetClass)
                .Include(m => m.TradingSessions)
                .Include(m => m.DataProviders)
                .Include(m => m.Symbols)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (market == null)
            {
                return null;
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Name))
                market.Name = request.Name;

            if (request.NameTurkish != null)
                market.NameTurkish = request.NameTurkish;

            if (request.Description != null)
                market.Description = request.Description;

            if (!string.IsNullOrEmpty(request.CountryCode))
                market.CountryCode = request.CountryCode.ToUpper();

            if (!string.IsNullOrEmpty(request.Timezone))
                market.Timezone = request.Timezone;

            if (!string.IsNullOrEmpty(request.PrimaryCurrency))
                market.PrimaryCurrency = request.PrimaryCurrency;

            if (request.MarketMaker != null)
                market.MarketMaker = request.MarketMaker;

            if (request.ApiBaseUrl != null)
                market.ApiBaseUrl = request.ApiBaseUrl;

            if (request.WebSocketUrl != null)
                market.WebSocketUrl = request.WebSocketUrl;

            if (request.DefaultCommissionRate.HasValue)
                market.DefaultCommissionRate = request.DefaultCommissionRate.Value;

            if (request.MinCommission.HasValue)
                market.MinCommission = request.MinCommission.Value;

            if (request.IsActive.HasValue)
                market.IsActive = request.IsActive.Value;

            if (request.HasRealtimeData.HasValue)
                market.HasRealtimeData = request.HasRealtimeData.Value;

            if (request.DataDelayMinutes.HasValue)
                market.DataDelayMinutes = request.DataDelayMinutes.Value;

            if (request.DisplayOrder.HasValue)
                market.DisplayOrder = request.DisplayOrder.Value;

            market.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated market: {Id} - {Code}", market.Id, market.Code);

            return MapToDto(market);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating market: {Id}", id);
            throw;
        }
    }

    public async Task<bool> UpdateMarketStatusAsync(Guid marketId, string status, string? statusMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var market = await _context.Markets
                .FirstOrDefaultAsync(m => m.Id == marketId, cancellationToken);

            if (market == null)
                return false;

            market.Status = status;
            market.StatusUpdatedAt = DateTime.UtcNow;
            market.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated market status: {MarketCode} -> {Status}", market.Code, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating market status: {MarketId}", marketId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var market = await _context.Markets
                .Include(m => m.Symbols)
                .Include(m => m.DataProviders)
                .Include(m => m.TradingSessions)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (market == null)
            {
                return false;
            }

            // Check if market has dependent records
            if (market.Symbols.Any())
            {
                throw new InvalidOperationException("Cannot delete market that has associated symbols");
            }

            _context.Markets.Remove(market);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted market: {Id} - {Code}", market.Id, market.Code);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting market: {Id}", id);
            throw;
        }
    }

    public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Markets.Where(m => m.Code.ToUpper() == code.ToUpper());

            if (excludeId.HasValue)
            {
                query = query.Where(m => m.Id != excludeId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking market code uniqueness: {Code}", code);
            throw;
        }
    }

    private static MarketSummaryDto MapToSummaryDto(Market market)
    {
        return new MarketSummaryDto
        {
            Id = market.Id,
            Code = market.Code,
            Name = market.Name,
            NameTurkish = market.NameTurkish,
            AssetClassCode = market.AssetClass?.Code ?? string.Empty,
            CountryCode = market.CountryCode,
            Status = market.Status,
            IsActive = market.IsActive,
            SymbolsCount = market.Symbols?.Count ?? 0
        };
    }

    private static MarketDto MapToDto(Market market)
    {
        return new MarketDto
        {
            Id = market.Id,
            Code = market.Code,
            Name = market.Name,
            NameTurkish = market.NameTurkish,
            Description = market.Description,
            AssetClass = market.AssetClass != null ? new AssetClassDto
            {
                Id = market.AssetClass.Id,
                Code = market.AssetClass.Code,
                Name = market.AssetClass.Name,
                NameTurkish = market.AssetClass.NameTurkish,
                Description = market.AssetClass.Description,
                PrimaryCurrency = market.AssetClass.PrimaryCurrency,
                DefaultPricePrecision = market.AssetClass.DefaultPricePrecision,
                DefaultQuantityPrecision = market.AssetClass.DefaultQuantityPrecision,
                Supports24x7Trading = market.AssetClass.Supports24x7Trading,
                SupportsFractional = market.AssetClass.SupportsFractional,
                MinTradeAmount = market.AssetClass.MinTradeAmount,
                RegulatoryClass = market.AssetClass.RegulatoryClass,
                IsActive = market.AssetClass.IsActive,
                DisplayOrder = market.AssetClass.DisplayOrder,
                MarketsCount = 0,
                SymbolsCount = 0
            } : new AssetClassDto(),
            CountryCode = market.CountryCode,
            Timezone = market.Timezone,
            PrimaryCurrency = market.PrimaryCurrency,
            MarketMaker = market.MarketMaker,
            DefaultCommissionRate = market.DefaultCommissionRate,
            Status = market.Status,
            StatusUpdatedAt = market.StatusUpdatedAt,
            IsActive = market.IsActive,
            HasRealtimeData = market.HasRealtimeData,
            DataDelayMinutes = market.DataDelayMinutes,
            DisplayOrder = market.DisplayOrder,
            SymbolsCount = market.Symbols?.Count ?? 0,
            DataProvidersCount = market.DataProviders?.Count ?? 0,
            TradingSessions = market.TradingSessions?.Select(ts => new TradingSessionDto
            {
                Id = ts.Id,
                SessionType = ts.SessionType,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                DaysOfWeek = ts.DayOfWeek?.ToString(),
                IsActive = ts.IsActive
            }).ToList() ?? new List<TradingSessionDto>()
        };
    }

    private static string? DetermineCurrentSessionType(Market market)
    {
        // TODO: Implement logic to determine current session type based on trading sessions
        // This would require parsing trading sessions and current time in market timezone
        return market.Status switch
        {
            "OPEN" => "REGULAR",
            "PRE_MARKET" => "PRE_MARKET",
            "AFTER_HOURS" => "AFTER_HOURS",
            _ => null
        };
    }
}