using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Data;
using MyTrader.Core.Models;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetController : ControllerBase
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger<AssetController> _logger;

    public AssetController(ITradingDbContext dbContext, ILogger<AssetController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get detailed asset information including market data, symbol details, and market information
    /// </summary>
    [HttpGet("details")]
    public async Task<IActionResult> GetAssetDetails(
        [FromQuery] List<string>? symbols = null,
        [FromQuery] string? assetClass = null,
        [FromQuery] bool includeMarketData = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = from symbol in _dbContext.Symbols.AsQueryable()
                       join market in _dbContext.Markets on symbol.MarketId equals market.Id into marketGroup
                       from market in marketGroup.DefaultIfEmpty()
                       join assetClassEntity in _dbContext.AssetClasses on symbol.AssetClassId equals assetClassEntity.Id into assetClassGroup
                       from assetClassEntity in assetClassGroup.DefaultIfEmpty()
                       where symbol.IsActive
                       select new
                       {
                           // Symbol information
                           SymbolId = symbol.Id,
                           Ticker = symbol.Ticker,
                           FullName = symbol.FullName,
                           Display = symbol.Display,
                           Description = symbol.Description,
                           AssetClass = symbol.AssetClass,
                           Sector = symbol.Sector,
                           Industry = symbol.Industry,
                           Country = symbol.Country,
                           Currency = symbol.QuoteCurrency ?? symbol.BaseCurrency,

                           // Market information
                           MarketId = market != null ? market.Id : (Guid?)null,
                           MarketCode = market != null ? market.Code : null,
                           MarketName = market != null ? market.Name : null,
                           MarketTimezone = market != null ? market.Timezone : null,
                           MarketStatus = market != null ? market.Status : null,

                           // Asset class information
                           AssetClassId = assetClassEntity != null ? assetClassEntity.Id : (Guid?)null,
                           AssetClassName = assetClassEntity != null ? assetClassEntity.Name : null,
                           AssetClassCode = assetClassEntity != null ? assetClassEntity.Code : null,
                           PrimaryCurrency = assetClassEntity != null ? assetClassEntity.PrimaryCurrency : symbol.QuoteCurrency ?? symbol.BaseCurrency,

                           // Trading information
                           IsActive = symbol.IsActive,
                           IsTracked = symbol.IsTracked,
                           TickSize = symbol.TickSize,
                           PricePrecision = symbol.PricePrecision,
                           QuantityPrecision = symbol.QuantityPrecision,

                           // Market metrics
                           MarketCap = symbol.MarketCap,
                           Volume24h = symbol.Volume24h,

                           UpdatedAt = symbol.UpdatedAt
                       };

            // Apply filters
            if (symbols != null && symbols.Any())
            {
                query = query.Where(a => symbols.Contains(a.Ticker));
            }

            if (!string.IsNullOrEmpty(assetClass))
            {
                query = query.Where(a => a.AssetClass == assetClass);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var assets = await query
                .OrderBy(a => a.AssetClass)
                .ThenBy(a => a.Ticker)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get current market data if requested
            Dictionary<string, object>? marketDataDict = null;
            if (includeMarketData && assets.Any())
            {
                var tickers = assets.Select(a => a.Ticker).ToList();
                var marketData = await _dbContext.MarketData
                    .Where(md => tickers.Contains(md.Symbol))
                    .GroupBy(md => md.Symbol)
                    .Select(g => g.OrderByDescending(md => md.Timestamp).First())
                    .ToListAsync();

                marketDataDict = marketData.ToDictionary(
                    md => md.Symbol,
                    md => new
                    {
                        CurrentPrice = md.Close,
                        PriceChange = md.Close - md.Open,
                        PriceChangePercent = md.Open > 0 ? ((md.Close - md.Open) / md.Open) * 100 : 0,
                        Volume = md.Volume,
                        High24h = md.High,
                        Low24h = md.Low,
                        LastUpdated = md.Timestamp
                    } as object
                );
            }

            var result = new
            {
                Assets = assets.Select(asset => new
                {
                    // Symbol information
                    asset.SymbolId,
                    asset.Ticker,
                    asset.FullName,
                    asset.Display,
                    asset.Description,
                    asset.AssetClass,
                    asset.Sector,
                    asset.Industry,
                    asset.Country,
                    asset.Currency,

                    // Market information
                    asset.MarketId,
                    asset.MarketCode,
                    asset.MarketName,
                    asset.MarketTimezone,
                    asset.MarketStatus,

                    // Asset class information
                    asset.AssetClassId,
                    asset.AssetClassName,
                    asset.AssetClassCode,
                    asset.PrimaryCurrency,

                    // Trading information
                    asset.IsActive,
                    asset.IsTracked,
                    asset.TickSize,
                    asset.PricePrecision,
                    asset.QuantityPrecision,

                    // Market metrics
                    asset.MarketCap,
                    asset.Volume24h,

                    // Current market data (if available)
                    CurrentPrice = marketDataDict?.GetValueOrDefault(asset.Ticker) != null
                        ? ((dynamic)marketDataDict[asset.Ticker]).CurrentPrice
                        : (decimal?)null,
                    PriceChange = marketDataDict?.GetValueOrDefault(asset.Ticker) != null
                        ? ((dynamic)marketDataDict[asset.Ticker]).PriceChange
                        : (decimal?)null,
                    PriceChangePercent = marketDataDict?.GetValueOrDefault(asset.Ticker) != null
                        ? ((dynamic)marketDataDict[asset.Ticker]).PriceChangePercent
                        : (decimal?)null,
                    Volume = marketDataDict?.GetValueOrDefault(asset.Ticker) != null
                        ? ((dynamic)marketDataDict[asset.Ticker]).Volume
                        : (decimal?)null,
                    PriceUpdatedAt = marketDataDict?.GetValueOrDefault(asset.Ticker) != null
                        ? ((dynamic)marketDataDict[asset.Ticker]).LastUpdated
                        : (DateTime?)null,

                    asset.UpdatedAt
                }).ToList(),
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset details");
            return StatusCode(500, new { Message = "Internal server error retrieving asset details" });
        }
    }

    /// <summary>
    /// Get available asset classes
    /// </summary>
    [HttpGet("classes")]
    public async Task<IActionResult> GetAssetClasses()
    {
        try
        {
            var assetClasses = await _dbContext.AssetClasses
                .Where(ac => ac.IsActive)
                .Select(ac => new
                {
                    ac.Id,
                    ac.Code,
                    ac.Name,
                    ac.NameTurkish,
                    ac.Description,
                    ac.PrimaryCurrency,
                    ac.Supports24x7Trading,
                    ac.SupportsFractional,
                    ac.MinTradeAmount,
                    ac.DisplayOrder
                })
                .OrderBy(ac => ac.DisplayOrder)
                .ThenBy(ac => ac.Name)
                .ToListAsync();

            return Ok(assetClasses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset classes");
            return StatusCode(500, new { Message = "Internal server error retrieving asset classes" });
        }
    }

    /// <summary>
    /// Get available markets
    /// </summary>
    [HttpGet("markets")]
    public async Task<IActionResult> GetMarkets([FromQuery] string? assetClassCode = null)
    {
        try
        {
            var query = _dbContext.Markets
                .Include(m => m.AssetClass)
                .Where(m => m.IsActive);

            if (!string.IsNullOrEmpty(assetClassCode))
            {
                query = query.Where(m => m.AssetClass.Code == assetClassCode);
            }

            var markets = await query
                .Select(m => new
                {
                    m.Id,
                    m.Code,
                    m.Name,
                    m.NameTurkish,
                    m.Description,
                    m.CountryCode,
                    m.Timezone,
                    m.PrimaryCurrency,
                    m.Status,
                    m.IsActive,
                    m.HasRealtimeData,
                    m.DataDelayMinutes,
                    AssetClass = new
                    {
                        m.AssetClass.Id,
                        m.AssetClass.Code,
                        m.AssetClass.Name
                    },
                    m.DisplayOrder
                })
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync();

            return Ok(markets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving markets");
            return StatusCode(500, new { Message = "Internal server error retrieving markets" });
        }
    }

    /// <summary>
    /// Get symbols by market or asset class
    /// </summary>
    [HttpGet("symbols")]
    public async Task<IActionResult> GetSymbols(
        [FromQuery] string? marketCode = null,
        [FromQuery] string? assetClass = null,
        [FromQuery] bool activeOnly = true,
        [FromQuery] bool popularOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var query = _dbContext.Symbols.AsQueryable();

            // Apply filters
            if (activeOnly)
                query = query.Where(s => s.IsActive);

            if (popularOnly)
                query = query.Where(s => s.IsPopular);

            if (!string.IsNullOrEmpty(marketCode))
            {
                query = query.Where(s => s.Venue == marketCode);
            }

            if (!string.IsNullOrEmpty(assetClass))
            {
                query = query.Where(s => s.AssetClass == assetClass);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchTerm = search.ToUpper();
                query = query.Where(s =>
                    s.Ticker.ToUpper().Contains(searchTerm) ||
                    (s.FullName != null && s.FullName.ToUpper().Contains(searchTerm)) ||
                    (s.Display != null && s.Display.ToUpper().Contains(searchTerm))
                );
            }

            var totalCount = await query.CountAsync();

            var symbols = await query
                .Select(s => new
                {
                    s.Id,
                    s.Ticker,
                    s.FullName,
                    s.Display,
                    s.AssetClass,
                    s.Sector,
                    s.Industry,
                    s.Country,
                    Currency = s.QuoteCurrency ?? s.BaseCurrency,
                    s.Venue,
                    s.IsActive,
                    s.IsTracked,
                    s.IsPopular,
                    s.MarketCap,
                    s.Volume24h,
                    s.UpdatedAt
                })
                .OrderBy(s => s.AssetClass)
                .ThenByDescending(s => s.IsPopular)
                .ThenByDescending(s => s.Volume24h)
                .ThenBy(s => s.Ticker)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                Symbols = symbols,
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving symbols");
            return StatusCode(500, new { Message = "Internal server error retrieving symbols" });
        }
    }
}