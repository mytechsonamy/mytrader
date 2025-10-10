using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Data;
using MyTrader.Core.Models;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ITradingDbContext dbContext, ILogger<DashboardController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }

    /// <summary>
    /// Get user's dashboard preferences with detailed asset information
    /// </summary>
    [HttpGet("preferences")]
    [Authorize] // Require authentication for user preferences
    public async Task<IActionResult> GetDashboardPreferences([FromQuery] bool includeMarketData = true)
    {
        try
        {
            var userId = GetCurrentUserId();

            var preferences = await (from pref in _dbContext.UserDashboardPreferences
                                   join symbol in _dbContext.Symbols on pref.SymbolId equals symbol.Id
                                   join market in _dbContext.Markets on symbol.MarketId equals market.Id into marketGroup
                                   from market in marketGroup.DefaultIfEmpty()
                                   join assetClass in _dbContext.AssetClasses on symbol.AssetClassId equals assetClass.Id into assetClassGroup
                                   from assetClass in assetClassGroup.DefaultIfEmpty()
                                   where pref.UserId == userId && pref.IsVisible
                                   orderby pref.IsPinned descending, pref.DisplayOrder, symbol.Ticker
                                   select new DashboardAssetDetailDto
                                   {
                                       // User preference data
                                       PreferenceId = pref.Id,
                                       DisplayOrder = pref.DisplayOrder,
                                       IsVisible = pref.IsVisible,
                                       IsPinned = pref.IsPinned,
                                       CustomAlias = pref.CustomAlias,
                                       Notes = pref.Notes,
                                       WidgetType = pref.WidgetType,
                                       WidgetConfig = pref.WidgetConfig,
                                       Category = pref.Category,

                                       // Symbol/Asset detailed information
                                       SymbolId = symbol.Id,
                                       Ticker = symbol.Ticker,
                                       FullName = symbol.FullName,
                                       Display = symbol.Display,
                                       Description = symbol.Description,
                                       AssetClass = symbol.AssetClass,
                                       Sector = symbol.Sector,
                                       Industry = symbol.Industry,
                                       Country = symbol.Country,

                                       // Market information
                                       MarketId = market != null ? market.Id : null,
                                       MarketCode = market != null ? market.Code : null,
                                       MarketName = market != null ? market.Name : null,
                                       MarketTimezone = market != null ? market.Timezone : null,
                                       MarketStatus = market != null ? market.Status : null,
                                       PrimaryCurrency = assetClass != null ? assetClass.PrimaryCurrency : symbol.QuoteCurrency ?? symbol.BaseCurrency,

                                       // Trading information
                                       IsActive = symbol.IsActive,
                                       IsTracked = symbol.IsTracked,
                                       TickSize = symbol.TickSize,
                                       PricePrecision = symbol.PricePrecision,
                                       QuantityPrecision = symbol.QuantityPrecision,

                                       // Market metrics
                                       MarketCap = symbol.MarketCap,
                                       Volume24h = symbol.Volume24h,

                                       UpdatedAt = pref.UpdatedAt
                                   }).ToListAsync();

            // Get current market data if requested
            if (includeMarketData && preferences.Any())
            {
                var tickers = preferences.Select(p => p.Ticker).ToList();
                var marketData = await _dbContext.MarketData
                    .Where(md => tickers.Contains(md.Symbol))
                    .GroupBy(md => md.Symbol)
                    .Select(g => g.OrderByDescending(md => md.Timestamp).First())
                    .ToListAsync();

                var marketDataDict = marketData.ToDictionary(md => md.Symbol);

                foreach (var preference in preferences)
                {
                    if (marketDataDict.TryGetValue(preference.Ticker, out var md))
                    {
                        preference.CurrentPrice = md.Close;
                        // ✅ FIXED: Calculate percent change using previous close (represented by Open in daily data)
                        preference.PriceChange24h = md.Open > 0 ? ((md.Close - md.Open) / md.Open) * 100 : 0;
                        preference.PriceUpdatedAt = md.Timestamp;
                    }
                }
            }

            return Ok(preferences);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard preferences for user");
            return StatusCode(500, new { Message = "Internal server error retrieving dashboard preferences" });
        }
    }

    /// <summary>
    /// Add asset to user's dashboard
    /// </summary>
    [HttpPost("preferences")]
    [Authorize] // Require authentication for adding preferences
    public async Task<IActionResult> AddDashboardPreference([FromBody] UserDashboardPreferenceDto request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Check if preference already exists
            var existingPreference = await _dbContext.UserDashboardPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.SymbolId == request.SymbolId);

            if (existingPreference != null)
            {
                return Conflict(new { Message = "Asset already exists in dashboard" });
            }

            // Check if symbol exists
            var symbol = await _dbContext.Symbols.FindAsync(request.SymbolId);
            if (symbol == null)
            {
                return BadRequest(new { Message = "Invalid symbol ID" });
            }

            // Get next display order if not specified
            var displayOrder = request.DisplayOrder;
            if (displayOrder == 0)
            {
                var maxOrder = await _dbContext.UserDashboardPreferences
                    .Where(p => p.UserId == userId)
                    .MaxAsync(p => (int?)p.DisplayOrder) ?? 0;
                displayOrder = maxOrder + 1;
            }

            var preference = new UserDashboardPreferences
            {
                UserId = userId,
                SymbolId = request.SymbolId,
                DisplayOrder = displayOrder,
                IsVisible = request.IsVisible,
                IsPinned = request.IsPinned,
                CustomAlias = request.CustomAlias,
                Notes = request.Notes,
                WidgetType = request.WidgetType,
                WidgetConfig = request.WidgetConfig,
                Category = request.Category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.UserDashboardPreferences.Add(preference);
            await _dbContext.SaveChangesAsync();

            var response = new UserDashboardPreferenceDto
            {
                Id = preference.Id,
                SymbolId = preference.SymbolId,
                DisplayOrder = preference.DisplayOrder,
                IsVisible = preference.IsVisible,
                IsPinned = preference.IsPinned,
                CustomAlias = preference.CustomAlias,
                Notes = preference.Notes,
                WidgetType = preference.WidgetType,
                WidgetConfig = preference.WidgetConfig,
                Category = preference.Category,
                SymbolTicker = symbol.Ticker,
                SymbolName = symbol.FullName,
                AssetClass = symbol.AssetClass
            };

            return CreatedAtAction(nameof(GetDashboardPreferences), new { id = preference.Id }, response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding dashboard preference");
            return StatusCode(500, new { Message = "Internal server error adding dashboard preference" });
        }
    }

    /// <summary>
    /// Update user's dashboard preference
    /// </summary>
    [HttpPut("preferences/{id}")]
    [Authorize] // Require authentication for updating preferences
    public async Task<IActionResult> UpdateDashboardPreference(Guid id, [FromBody] UserDashboardPreferenceDto request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var preference = await _dbContext.UserDashboardPreferences
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (preference == null)
            {
                return NotFound(new { Message = "Dashboard preference not found" });
            }

            // Update properties
            preference.DisplayOrder = request.DisplayOrder;
            preference.IsVisible = request.IsVisible;
            preference.IsPinned = request.IsPinned;
            preference.CustomAlias = request.CustomAlias;
            preference.Notes = request.Notes;
            preference.WidgetType = request.WidgetType;
            preference.WidgetConfig = request.WidgetConfig;
            preference.Category = request.Category;
            preference.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Dashboard preference updated successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard preference");
            return StatusCode(500, new { Message = "Internal server error updating dashboard preference" });
        }
    }

    /// <summary>
    /// Remove asset from user's dashboard
    /// </summary>
    [HttpDelete("preferences/{id}")]
    [Authorize] // Require authentication for removing preferences
    public async Task<IActionResult> RemoveDashboardPreference(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var preference = await _dbContext.UserDashboardPreferences
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (preference == null)
            {
                return NotFound(new { Message = "Dashboard preference not found" });
            }

            _dbContext.UserDashboardPreferences.Remove(preference);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Asset removed from dashboard successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing dashboard preference");
            return StatusCode(500, new { Message = "Internal server error removing dashboard preference" });
        }
    }

    /// <summary>
    /// Bulk update dashboard preferences
    /// </summary>
    [HttpPost("preferences/bulk")]
    [Authorize] // Require authentication for bulk updates
    public async Task<IActionResult> BulkUpdatePreferences([FromBody] BulkDashboardPreferenceUpdateDto request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var existingPreferences = await _dbContext.UserDashboardPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Reset display order if requested
                if (request.ResetOrder)
                {
                    for (int i = 0; i < request.Preferences.Count; i++)
                    {
                        request.Preferences[i].DisplayOrder = i + 1;
                    }
                }

                foreach (var dto in request.Preferences)
                {
                    if (dto.Id.HasValue)
                    {
                        // Update existing preference
                        var existing = existingPreferences.FirstOrDefault(p => p.Id == dto.Id.Value);
                        if (existing != null)
                        {
                            existing.DisplayOrder = dto.DisplayOrder;
                            existing.IsVisible = dto.IsVisible;
                            existing.IsPinned = dto.IsPinned;
                            existing.CustomAlias = dto.CustomAlias;
                            existing.Notes = dto.Notes;
                            existing.WidgetType = dto.WidgetType;
                            existing.WidgetConfig = dto.WidgetConfig;
                            existing.Category = dto.Category;
                            existing.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Add new preference
                        var newPreference = new UserDashboardPreferences
                        {
                            UserId = userId,
                            SymbolId = dto.SymbolId,
                            DisplayOrder = dto.DisplayOrder,
                            IsVisible = dto.IsVisible,
                            IsPinned = dto.IsPinned,
                            CustomAlias = dto.CustomAlias,
                            Notes = dto.Notes,
                            WidgetType = dto.WidgetType,
                            WidgetConfig = dto.WidgetConfig,
                            Category = dto.Category,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _dbContext.UserDashboardPreferences.Add(newPreference);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Dashboard preferences updated successfully" });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating dashboard preferences");
            return StatusCode(500, new { Message = "Internal server error updating dashboard preferences" });
        }
    }

    /// <summary>
    /// Get available assets that can be added to dashboard
    /// </summary>
    [HttpGet("available-assets")]
    [AllowAnonymous] // Allow public access for asset discovery
    public async Task<IActionResult> GetAvailableAssets(
        [FromQuery] string? assetClass = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            List<Guid> existingSymbolIds = new List<Guid>();

            // For authenticated users, exclude symbols already in their dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var userId = GetCurrentUserId();
                    existingSymbolIds = await _dbContext.UserDashboardPreferences
                        .Where(p => p.UserId == userId)
                        .Select(p => p.SymbolId)
                        .ToListAsync();
                }
                catch (UnauthorizedAccessException)
                {
                    // User not properly authenticated, treat as anonymous
                    existingSymbolIds = new List<Guid>();
                }
            }

            var query = _dbContext.Symbols
                .Where(s => s.IsActive && !existingSymbolIds.Contains(s.Id));

            if (!string.IsNullOrEmpty(assetClass))
            {
                query = query.Where(s => s.AssetClass == assetClass);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchTerm = search.ToUpper();
                query = query.Where(s =>
                    s.Ticker.ToUpper().Contains(searchTerm) ||
                    (s.FullName != null && s.FullName.ToUpper().Contains(searchTerm))
                );
            }

            var totalCount = await query.CountAsync();

            var assets = await query
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
                    s.IsPopular,
                    s.MarketCap,
                    s.Volume24h
                })
                .OrderByDescending(s => s.IsPopular)
                .ThenByDescending(s => s.Volume24h)
                .ThenBy(s => s.Ticker)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                Assets = assets,
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
            _logger.LogError(ex, "Error retrieving available assets");
            return StatusCode(500, new { Message = "Internal server error retrieving available assets" });
        }
    }

    /// <summary>
    /// Get public dashboard data for web frontend (guest users)
    /// </summary>
    [HttpGet("public-data")]
    [AllowAnonymous] // Allow public access for dashboard overview
    public async Task<IActionResult> GetPublicDashboardData()
    {
        try
        {
            _logger.LogInformation("Getting public dashboard data for web frontend");

            // Get popular symbols with market data
            var popularSymbols = await _dbContext.Symbols
                .Where(s => s.IsActive && s.IsPopular)
                .OrderByDescending(s => s.Volume24h)
                .Take(20)
                .Select(s => new
                {
                    id = s.Id,
                    ticker = s.Ticker,
                    display = s.Display ?? s.FullName ?? s.Ticker,
                    assetClass = s.AssetClass,
                    sector = s.Sector,
                    industry = s.Industry,
                    marketCap = s.MarketCap,
                    volume24h = s.Volume24h,
                    baseCurrency = s.BaseCurrency,
                    quoteCurrency = s.QuoteCurrency
                })
                .ToListAsync();

            // Get latest market data for popular symbols
            var symbolTickers = popularSymbols.Select(s => s.ticker).ToList();
            // ✅ FIXED: Need to get TWO data points - today and yesterday for proper percent change
            var latestMarketDataRaw = await _dbContext.MarketData
                .Where(md => symbolTickers.Contains(md.Symbol))
                .GroupBy(md => md.Symbol)
                .SelectMany(g => g.OrderByDescending(md => md.Timestamp).Take(2)) // Get last 2 data points
                .ToListAsync();

            var latestMarketData = latestMarketDataRaw
                .GroupBy(md => md.Symbol)
                .Select(g =>
                {
                    var ordered = g.OrderByDescending(md => md.Timestamp).ToList();
                    var current = ordered.First();
                    var previous = ordered.Skip(1).FirstOrDefault();
                    var previousClose = previous?.Close ?? current.Open; // Fallback to Open if no previous data

                    return new
                    {
                        symbol = current.Symbol,
                        price = current.Close,
                        change = current.Close - previousClose,
                        changePercent = previousClose > 0 ? ((current.Close - previousClose) / previousClose) * 100 : 0,
                        high = current.High,
                        low = current.Low,
                        volume = current.Volume,
                        timestamp = current.Timestamp
                    };
                })
                .ToList();

            // Combine symbol info with market data
            var dashboardAssets = popularSymbols.Select(symbol =>
            {
                var marketData = latestMarketData.FirstOrDefault(md => md.symbol == symbol.ticker);
                return new
                {
                    symbol.id,
                    symbol.ticker,
                    symbol.display,
                    symbol.assetClass,
                    symbol.sector,
                    symbol.industry,
                    symbol.marketCap,
                    symbol.volume24h,
                    price = marketData?.price ?? 0,
                    change = marketData?.change ?? 0,
                    changePercent = marketData?.changePercent ?? 0,
                    high24h = marketData?.high ?? 0,
                    low24h = marketData?.low ?? 0,
                    volume = marketData?.volume ?? 0,
                    lastUpdated = marketData?.timestamp ?? DateTime.UtcNow
                };
            }).ToList();

            // Market summary statistics
            var totalSymbols = await _dbContext.Symbols.CountAsync(s => s.IsActive);
            var trackedSymbols = await _dbContext.Symbols.CountAsync(s => s.IsTracked);
            var assetClassBreakdown = await _dbContext.Symbols
                .Where(s => s.IsActive)
                .GroupBy(s => s.AssetClass)
                .Select(g => new { AssetClass = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new
            {
                assets = dashboardAssets,
                summary = new
                {
                    totalSymbols,
                    trackedSymbols,
                    assetClassBreakdown,
                    lastUpdated = DateTime.UtcNow
                },
                metadata = new
                {
                    source = "public_dashboard",
                    isGuestMode = true,
                    dataPoints = dashboardAssets.Count
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public dashboard data");

            // Return fallback data on error
            var fallbackData = new
            {
                assets = new[]
                {
                    new { id = Guid.NewGuid(), ticker = "BTC-USD", display = "Bitcoin", assetClass = "CRYPTO", price = 43250.00m, change = 1250.50m, changePercent = 2.98m },
                    new { id = Guid.NewGuid(), ticker = "ETH-USD", display = "Ethereum", assetClass = "CRYPTO", price = 2680.50m, change = -45.20m, changePercent = -1.66m },
                    new { id = Guid.NewGuid(), ticker = "AAPL", display = "Apple Inc.", assetClass = "STOCK", price = 195.75m, change = 2.50m, changePercent = 1.29m },
                    new { id = Guid.NewGuid(), ticker = "MSFT", display = "Microsoft", assetClass = "STOCK", price = 420.50m, change = -1.20m, changePercent = -0.28m }
                },
                summary = new { totalSymbols = 4, trackedSymbols = 4, lastUpdated = DateTime.UtcNow },
                metadata = new { source = "fallback_data", isGuestMode = true, dataPoints = 4 }
            };

            return Ok(fallbackData);
        }
    }
}