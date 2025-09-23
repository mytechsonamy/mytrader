using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Service implementation for asset class management
/// </summary>
public class AssetClassService : IAssetClassService
{
    private readonly TradingDbContext _context;
    private readonly ILogger<AssetClassService> _logger;

    public AssetClassService(TradingDbContext context, ILogger<AssetClassService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AssetClassDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var assetClasses = await _context.AssetClasses
                .Include(ac => ac.Markets)
                .Include(ac => ac.Symbols)
                .OrderBy(ac => ac.DisplayOrder)
                .ThenBy(ac => ac.Name)
                .ToListAsync(cancellationToken);

            return assetClasses.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all asset classes");
            throw;
        }
    }

    public async Task<List<AssetClassDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var assetClasses = await _context.AssetClasses
                .Include(ac => ac.Markets)
                .Include(ac => ac.Symbols)
                .Where(ac => ac.IsActive)
                .OrderBy(ac => ac.DisplayOrder)
                .ThenBy(ac => ac.Name)
                .ToListAsync(cancellationToken);

            return assetClasses.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active asset classes");
            throw;
        }
    }

    public async Task<AssetClassDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var assetClass = await _context.AssetClasses
                .Include(ac => ac.Markets)
                .Include(ac => ac.Symbols)
                .FirstOrDefaultAsync(ac => ac.Id == id, cancellationToken);

            return assetClass != null ? MapToDto(assetClass) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset class by ID: {AssetClassId}", id);
            throw;
        }
    }

    public async Task<AssetClassDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var assetClass = await _context.AssetClasses
                .Include(ac => ac.Markets)
                .Include(ac => ac.Symbols)
                .FirstOrDefaultAsync(ac => ac.Code.ToUpper() == code.ToUpper(), cancellationToken);

            return assetClass != null ? MapToDto(assetClass) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset class by code: {Code}", code);
            throw;
        }
    }

    public async Task<AssetClassDto> CreateAsync(CreateAssetClassRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if code already exists
            var existingAssetClass = await _context.AssetClasses
                .FirstOrDefaultAsync(ac => ac.Code.ToUpper() == request.Code.ToUpper(), cancellationToken);

            if (existingAssetClass != null)
            {
                throw new InvalidOperationException($"Asset class with code '{request.Code}' already exists");
            }

            var assetClass = new AssetClass
            {
                Code = request.Code.ToUpper(),
                Name = request.Name,
                NameTurkish = request.NameTurkish,
                Description = request.Description,
                PrimaryCurrency = request.PrimaryCurrency,
                DefaultPricePrecision = request.DefaultPricePrecision,
                DefaultQuantityPrecision = request.DefaultQuantityPrecision,
                Supports24x7Trading = request.Supports24x7Trading,
                SupportsFractional = request.SupportsFractional,
                MinTradeAmount = request.MinTradeAmount,
                RegulatoryClass = request.RegulatoryClass,
                DisplayOrder = request.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AssetClasses.Add(assetClass);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new asset class: {Code} - {Name}", assetClass.Code, assetClass.Name);

            return MapToDto(assetClass);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset class: {Code}", request.Code);
            throw;
        }
    }

    public async Task<AssetClassDto?> UpdateAsync(Guid id, UpdateAssetClassRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var assetClass = await _context.AssetClasses
                .Include(ac => ac.Markets)
                .Include(ac => ac.Symbols)
                .FirstOrDefaultAsync(ac => ac.Id == id, cancellationToken);

            if (assetClass == null)
            {
                return null;
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Name))
                assetClass.Name = request.Name;

            if (request.NameTurkish != null)
                assetClass.NameTurkish = request.NameTurkish;

            if (request.Description != null)
                assetClass.Description = request.Description;

            if (!string.IsNullOrEmpty(request.PrimaryCurrency))
                assetClass.PrimaryCurrency = request.PrimaryCurrency;

            if (request.DefaultPricePrecision.HasValue)
                assetClass.DefaultPricePrecision = request.DefaultPricePrecision.Value;

            if (request.DefaultQuantityPrecision.HasValue)
                assetClass.DefaultQuantityPrecision = request.DefaultQuantityPrecision.Value;

            if (request.Supports24x7Trading.HasValue)
                assetClass.Supports24x7Trading = request.Supports24x7Trading.Value;

            if (request.SupportsFractional.HasValue)
                assetClass.SupportsFractional = request.SupportsFractional.Value;

            if (request.MinTradeAmount.HasValue)
                assetClass.MinTradeAmount = request.MinTradeAmount.Value;

            if (request.RegulatoryClass != null)
                assetClass.RegulatoryClass = request.RegulatoryClass;

            if (request.IsActive.HasValue)
                assetClass.IsActive = request.IsActive.Value;

            if (request.DisplayOrder.HasValue)
                assetClass.DisplayOrder = request.DisplayOrder.Value;

            assetClass.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated asset class: {Id} - {Code}", assetClass.Id, assetClass.Code);

            return MapToDto(assetClass);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset class: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var assetClass = await _context.AssetClasses
                .Include(ac => ac.Markets)
                .Include(ac => ac.Symbols)
                .FirstOrDefaultAsync(ac => ac.Id == id, cancellationToken);

            if (assetClass == null)
            {
                return false;
            }

            // Check if asset class has dependent records
            if (assetClass.Markets.Any() || assetClass.Symbols.Any())
            {
                throw new InvalidOperationException("Cannot delete asset class that has associated markets or symbols");
            }

            _context.AssetClasses.Remove(assetClass);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted asset class: {Id} - {Code}", assetClass.Id, assetClass.Code);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset class: {Id}", id);
            throw;
        }
    }

    public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.AssetClasses.Where(ac => ac.Code.ToUpper() == code.ToUpper());

            if (excludeId.HasValue)
            {
                query = query.Where(ac => ac.Id != excludeId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking asset class code uniqueness: {Code}", code);
            throw;
        }
    }

    private static AssetClassDto MapToDto(AssetClass assetClass)
    {
        return new AssetClassDto
        {
            Id = assetClass.Id,
            Code = assetClass.Code,
            Name = assetClass.Name,
            NameTurkish = assetClass.NameTurkish,
            Description = assetClass.Description,
            PrimaryCurrency = assetClass.PrimaryCurrency,
            DefaultPricePrecision = assetClass.DefaultPricePrecision,
            DefaultQuantityPrecision = assetClass.DefaultQuantityPrecision,
            Supports24x7Trading = assetClass.Supports24x7Trading,
            SupportsFractional = assetClass.SupportsFractional,
            MinTradeAmount = assetClass.MinTradeAmount,
            RegulatoryClass = assetClass.RegulatoryClass,
            IsActive = assetClass.IsActive,
            DisplayOrder = assetClass.DisplayOrder,
            MarketsCount = assetClass.Markets?.Count ?? 0,
            SymbolsCount = assetClass.Symbols?.Count ?? 0
        };
    }
}