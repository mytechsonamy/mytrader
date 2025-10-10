using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs;
using MyTrader.Core.Services;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Controller for managing symbol preferences and dynamic symbol loading.
/// Supports user-specific preferences and system-wide symbol configuration.
/// </summary>
[ApiController]
[Route("api/symbol-preferences")]
[Produces("application/json")]
public class SymbolPreferencesController : ControllerBase
{
    private readonly ISymbolManagementService _symbolManagementService;
    private readonly ILogger<SymbolPreferencesController> _logger;

    public SymbolPreferencesController(
        ISymbolManagementService symbolManagementService,
        ILogger<SymbolPreferencesController> logger)
    {
        _symbolManagementService = symbolManagementService ?? throw new ArgumentNullException(nameof(symbolManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get user-specific symbol preferences.
    /// Returns symbols the user has explicitly selected.
    /// </summary>
    /// <param name="userId">User ID (GUID)</param>
    /// <param name="assetClass">Optional asset class filter (e.g., "CRYPTO", "STOCK_BIST")</param>
    /// <returns>List of user's preferred symbols</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(SymbolListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SymbolListResponse>> GetUserSymbols(
        [FromRoute] string userId,
        [FromQuery] string? assetClass = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new SymbolListResponse
                {
                    Success = false,
                    Message = "User ID is required"
                });
            }

            _logger.LogInformation("Getting user symbols: UserId={UserId}, AssetClass={AssetClass}",
                userId, assetClass ?? "ALL");

            var symbols = await _symbolManagementService.GetUserSymbolsAsync(userId, assetClass);

            var symbolDtos = symbols.Select(s => new SymbolDto
            {
                Id = s.Id.ToString(),
                Symbol = s.Ticker,
                DisplayName = s.Display ?? s.FullName ?? s.Ticker,
                BaseCurrency = s.BaseCurrency,
                QuoteCurrency = s.QuoteCurrency,
                AssetClass = s.AssetClass,
                Market = s.Venue ?? s.Market?.Code,
                BroadcastPriority = s.DisplayOrder, // Using DisplayOrder as proxy
                IsDefault = s.IsPopular, // Using IsPopular as proxy for is_default_symbol
                IsActive = s.IsActive,
                IsPopular = s.IsPopular,
                CurrentPrice = s.CurrentPrice,
                PriceChange24h = s.PriceChange24h,
                Volume24h = s.Volume24h,
                DisplayOrder = s.DisplayOrder,
                Sector = s.Sector,
                Country = s.Country
            }).ToList();

            return Ok(new SymbolListResponse
            {
                Success = true,
                Symbols = symbolDtos,
                TotalCount = symbolDtos.Count,
                AssetClass = assetClass
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user symbols for UserId={UserId}", userId);
            return StatusCode(500, new SymbolListResponse
            {
                Success = false,
                Message = "An error occurred while retrieving user symbols"
            });
        }
    }

    /// <summary>
    /// Get default symbols for new/anonymous users.
    /// Returns system-wide default symbols (is_default_symbol=true).
    /// </summary>
    /// <param name="assetClass">Optional asset class filter</param>
    /// <returns>List of default symbols</returns>
    [HttpGet("defaults")]
    [ProducesResponseType(typeof(SymbolListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SymbolListResponse>> GetDefaultSymbols(
        [FromQuery] string? assetClass = null)
    {
        try
        {
            _logger.LogInformation("Getting default symbols: AssetClass={AssetClass}", assetClass ?? "ALL");

            var symbols = await _symbolManagementService.GetDefaultSymbolsAsync(assetClass);

            var symbolDtos = symbols.Select(s => new SymbolDto
            {
                Id = s.Id.ToString(),
                Symbol = s.Ticker,
                DisplayName = s.Display ?? s.FullName ?? s.Ticker,
                BaseCurrency = s.BaseCurrency,
                QuoteCurrency = s.QuoteCurrency,
                AssetClass = s.AssetClass,
                Market = s.Venue ?? s.Market?.Code,
                BroadcastPriority = s.DisplayOrder,
                IsDefault = true,
                IsActive = s.IsActive,
                IsPopular = s.IsPopular,
                CurrentPrice = s.CurrentPrice,
                PriceChange24h = s.PriceChange24h,
                Volume24h = s.Volume24h,
                DisplayOrder = s.DisplayOrder,
                Sector = s.Sector,
                Country = s.Country
            }).ToList();

            return Ok(new SymbolListResponse
            {
                Success = true,
                Symbols = symbolDtos,
                TotalCount = symbolDtos.Count,
                AssetClass = assetClass,
                Message = "Default symbols retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default symbols");
            return StatusCode(500, new SymbolListResponse
            {
                Success = false,
                Message = "An error occurred while retrieving default symbols"
            });
        }
    }

    /// <summary>
    /// Get all active symbols for a specific asset class.
    /// Useful for symbol selectors and dropdowns.
    /// </summary>
    /// <param name="assetClass">Asset class code (e.g., "CRYPTO")</param>
    /// <param name="includeInactive">Include inactive symbols</param>
    /// <returns>List of symbols</returns>
    [HttpGet("asset-class/{assetClass}")]
    [ProducesResponseType(typeof(SymbolListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SymbolListResponse>> GetSymbolsByAssetClass(
        [FromRoute] string assetClass,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(assetClass))
            {
                return BadRequest(new SymbolListResponse
                {
                    Success = false,
                    Message = "Asset class is required"
                });
            }

            _logger.LogInformation("Getting symbols by asset class: {AssetClass}, IncludeInactive={IncludeInactive}",
                assetClass, includeInactive);

            var symbols = await _symbolManagementService.GetSymbolsByAssetClassAsync(assetClass, includeInactive);

            var symbolDtos = symbols.Select(s => new SymbolDto
            {
                Id = s.Id.ToString(),
                Symbol = s.Ticker,
                DisplayName = s.Display ?? s.FullName ?? s.Ticker,
                BaseCurrency = s.BaseCurrency,
                QuoteCurrency = s.QuoteCurrency,
                AssetClass = s.AssetClass,
                Market = s.Venue ?? s.Market?.Code,
                BroadcastPriority = s.DisplayOrder,
                IsDefault = s.IsPopular,
                IsActive = s.IsActive,
                IsPopular = s.IsPopular,
                CurrentPrice = s.CurrentPrice,
                PriceChange24h = s.PriceChange24h,
                Volume24h = s.Volume24h,
                DisplayOrder = s.DisplayOrder,
                Sector = s.Sector,
                Country = s.Country
            }).ToList();

            return Ok(new SymbolListResponse
            {
                Success = true,
                Symbols = symbolDtos,
                TotalCount = symbolDtos.Count,
                AssetClass = assetClass
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols by asset class: {AssetClass}", assetClass);
            return StatusCode(500, new SymbolListResponse
            {
                Success = false,
                Message = "An error occurred while retrieving symbols"
            });
        }
    }

    /// <summary>
    /// Update user symbol preferences.
    /// Replaces all existing preferences with the provided list.
    /// </summary>
    /// <param name="userId">User ID (GUID)</param>
    /// <param name="request">List of symbol IDs to save</param>
    /// <returns>Success response</returns>
    [HttpPut("user/{userId}")]
    [ProducesResponseType(typeof(UpdateSymbolPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateSymbolPreferencesResponse>> UpdateUserPreferences(
        [FromRoute] string userId,
        [FromBody] UpdateSymbolPreferencesRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new UpdateSymbolPreferencesResponse
                {
                    Success = false,
                    Message = "User ID is required"
                });
            }

            if (request?.SymbolIds == null || !request.SymbolIds.Any())
            {
                return BadRequest(new UpdateSymbolPreferencesResponse
                {
                    Success = false,
                    Message = "At least one symbol ID is required"
                });
            }

            _logger.LogInformation("Updating symbol preferences: UserId={UserId}, Count={Count}",
                userId, request.SymbolIds.Count);

            await _symbolManagementService.UpdateSymbolPreferencesAsync(userId, request.SymbolIds);

            return Ok(new UpdateSymbolPreferencesResponse
            {
                Success = true,
                Message = "Symbol preferences updated successfully",
                UpdatedCount = request.SymbolIds.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating symbol preferences for UserId={UserId}", userId);
            return StatusCode(500, new UpdateSymbolPreferencesResponse
            {
                Success = false,
                Message = "An error occurred while updating symbol preferences"
            });
        }
    }

    /// <summary>
    /// Reload symbols from database and clear cache.
    /// Admin-only operation for hot-reload without service restart.
    /// </summary>
    /// <returns>Reload status</returns>
    [HttpPost("reload")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SymbolReloadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SymbolReloadResponse>> ReloadSymbols()
    {
        try
        {
            _logger.LogInformation("Symbol reload requested by admin");

            await _symbolManagementService.ReloadSymbolsAsync();

            // Get count of reloaded symbols
            var symbols = await _symbolManagementService.GetDefaultSymbolsAsync();

            return Ok(new SymbolReloadResponse
            {
                Success = true,
                Message = "Symbols reloaded successfully from database",
                Timestamp = DateTime.UtcNow,
                SymbolsReloaded = symbols.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading symbols");
            return StatusCode(500, new SymbolReloadResponse
            {
                Success = false,
                Message = "An error occurred while reloading symbols",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get symbols for WebSocket broadcasting.
    /// Returns active symbols ordered by broadcast priority.
    /// </summary>
    /// <param name="assetClass">Asset class code (e.g., "CRYPTO")</param>
    /// <param name="market">Market code (e.g., "BINANCE")</param>
    /// <returns>List of symbols for broadcasting</returns>
    [HttpGet("broadcast")]
    [ProducesResponseType(typeof(SymbolListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SymbolListResponse>> GetBroadcastSymbols(
        [FromQuery] string assetClass = "CRYPTO",
        [FromQuery] string market = "BINANCE")
    {
        try
        {
            _logger.LogInformation("Getting broadcast symbols: AssetClass={AssetClass}, Market={Market}",
                assetClass, market);

            var symbols = await _symbolManagementService.GetActiveSymbolsForBroadcastAsync(assetClass, market);

            var symbolDtos = symbols.Select(s => new SymbolDto
            {
                Id = s.Id.ToString(),
                Symbol = s.Ticker,
                DisplayName = s.Display ?? s.FullName ?? s.Ticker,
                BaseCurrency = s.BaseCurrency,
                QuoteCurrency = s.QuoteCurrency,
                AssetClass = s.AssetClass,
                Market = s.Venue ?? s.Market?.Code,
                BroadcastPriority = s.DisplayOrder,
                IsDefault = s.IsPopular,
                IsActive = s.IsActive,
                IsPopular = s.IsPopular,
                CurrentPrice = s.CurrentPrice,
                PriceChange24h = s.PriceChange24h,
                Volume24h = s.Volume24h,
                DisplayOrder = s.DisplayOrder
            }).ToList();

            return Ok(new SymbolListResponse
            {
                Success = true,
                Symbols = symbolDtos,
                TotalCount = symbolDtos.Count,
                AssetClass = assetClass,
                Market = market
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting broadcast symbols");
            return StatusCode(500, new SymbolListResponse
            {
                Success = false,
                Message = "An error occurred while retrieving broadcast symbols"
            });
        }
    }
}
