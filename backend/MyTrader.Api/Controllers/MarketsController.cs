using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Api.Controllers;

/// <summary>
/// API controller for markets/exchanges management
/// Supports Binance, BIST, NASDAQ, NYSE and other markets
/// </summary>
[ApiController]
[Route("api/v1/markets")]
[Authorize]
public class MarketsController : ControllerBase
{
    private readonly IMarketService _marketService;
    private readonly ILogger<MarketsController> _logger;

    public MarketsController(
        IMarketService marketService,
        ILogger<MarketsController> logger)
    {
        _marketService = marketService;
        _logger = logger;
    }

    /// <summary>
    /// Get all markets
    /// </summary>
    /// <returns>List of all markets</returns>
    [HttpGet]
    [AllowAnonymous] // Allow public access for market discovery
    [ProducesResponseType(typeof(ApiResponse<List<MarketSummaryDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<MarketSummaryDto>>>> GetAllMarkets()
    {
        try
        {
            _logger.LogInformation("Getting all markets");

            var markets = await _marketService.GetAllAsync();

            return Ok(ApiResponse<List<MarketSummaryDto>>.SuccessResult(
                markets,
                $"Retrieved {markets.Count} markets"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all markets");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve markets", 500));
        }
    }

    /// <summary>
    /// Get active markets only
    /// </summary>
    /// <returns>List of active markets</returns>
    [HttpGet("active")]
    [AllowAnonymous] // Allow public access for market discovery
    [ProducesResponseType(typeof(ApiResponse<List<MarketSummaryDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<MarketSummaryDto>>>> GetActiveMarkets()
    {
        try
        {
            _logger.LogInformation("Getting active markets");

            var markets = await _marketService.GetActiveAsync();

            return Ok(ApiResponse<List<MarketSummaryDto>>.SuccessResult(
                markets,
                $"Retrieved {markets.Count} active markets"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active markets");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve active markets", 500));
        }
    }

    /// <summary>
    /// Get markets by asset class
    /// </summary>
    /// <param name="assetClassId">Asset class ID</param>
    /// <returns>List of markets for the asset class</returns>
    [HttpGet("by-asset-class/{assetClassId:guid}")]
    [AllowAnonymous] // Allow public access for market discovery
    [ProducesResponseType(typeof(ApiResponse<List<MarketSummaryDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<MarketSummaryDto>>>> GetMarketsByAssetClass(
        Guid assetClassId)
    {
        try
        {
            _logger.LogInformation("Getting markets by asset class: {AssetClassId}", assetClassId);

            var markets = await _marketService.GetByAssetClassAsync(assetClassId);

            return Ok(ApiResponse<List<MarketSummaryDto>>.SuccessResult(
                markets,
                $"Retrieved {markets.Count} markets for asset class"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting markets by asset class: {AssetClassId}", assetClassId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve markets by asset class", 500));
        }
    }

    /// <summary>
    /// Get market by ID with full details
    /// </summary>
    /// <param name="id">Market ID</param>
    /// <returns>Market details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MarketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MarketDto>>> GetMarketById(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting market by ID: {MarketId}", id);

            var market = await _marketService.GetByIdAsync(id);

            if (market == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Market with ID {id} not found", 404));
            }

            return Ok(ApiResponse<MarketDto>.SuccessResult(
                market,
                "Market retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market by ID: {MarketId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve market", 500));
        }
    }

    /// <summary>
    /// Get market by code
    /// </summary>
    /// <param name="code">Market code (e.g., BINANCE, BIST, NASDAQ)</param>
    /// <returns>Market details</returns>
    [HttpGet("by-code/{code}")]
    [AllowAnonymous] // Allow public access for market discovery
    [ProducesResponseType(typeof(ApiResponse<MarketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MarketDto>>> GetMarketByCode(
        [Required] string code)
    {
        try
        {
            _logger.LogInformation("Getting market by code: {Code}", code);

            var market = await _marketService.GetByCodeAsync(code);

            if (market == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Market with code '{code}' not found", 404));
            }

            return Ok(ApiResponse<MarketDto>.SuccessResult(
                market,
                "Market retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market by code: {Code}", code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve market", 500));
        }
    }

    /// <summary>
    /// Get market status
    /// </summary>
    /// <param name="id">Market ID</param>
    /// <returns>Market status information</returns>
    [HttpGet("{id:guid}/status")]
    [AllowAnonymous] // Allow public access for market status
    [ProducesResponseType(typeof(ApiResponse<MarketStatusDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MarketStatusDto>>> GetMarketStatus(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting market status: {MarketId}", id);

            var status = await _marketService.GetMarketStatusAsync(id);

            if (status == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Market with ID {id} not found", 404));
            }

            return Ok(ApiResponse<MarketStatusDto>.SuccessResult(
                status,
                "Market status retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market status: {MarketId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve market status", 500));
        }
    }

    /// <summary>
    /// Get all market statuses
    /// </summary>
    /// <returns>Status information for all active markets</returns>
    [HttpGet("status")]
    [AllowAnonymous] // Allow public access for market status
    [ProducesResponseType(typeof(ApiResponse<List<MarketStatusDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<MarketStatusDto>>>> GetAllMarketStatuses()
    {
        try
        {
            _logger.LogInformation("Getting all market statuses");

            var statuses = await _marketService.GetAllMarketStatusesAsync();

            return Ok(ApiResponse<List<MarketStatusDto>>.SuccessResult(
                statuses,
                $"Retrieved status for {statuses.Count} markets"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all market statuses");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve market statuses", 500));
        }
    }

    /// <summary>
    /// Create a new market
    /// </summary>
    /// <param name="request">Market creation request</param>
    /// <returns>Created market</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MarketDto>), 201)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MarketDto>>> CreateMarket(
        [FromBody, Required] CreateMarketRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new market: {Code}", request.Code);

            // Check if code is unique
            var isUnique = await _marketService.IsCodeUniqueAsync(request.Code);
            if (!isUnique)
            {
                return Conflict(ApiResponse<object>.ErrorResult(
                    $"Market with code '{request.Code}' already exists", 409));
            }

            var market = await _marketService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetMarketById),
                new { id = market.Id },
                ApiResponse<MarketDto>.SuccessResult(
                    market,
                    "Market created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation creating market: {Message}", ex.Message);
            return Conflict(ApiResponse<object>.ErrorResult(ex.Message, 409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating market: {Code}", request.Code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to create market", 500));
        }
    }

    /// <summary>
    /// Update market status
    /// </summary>
    /// <param name="id">Market ID</param>
    /// <param name="status">New status (OPEN, CLOSED, PRE_MARKET, etc.)</param>
    /// <param name="statusMessage">Optional status message</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMarketStatus(
        Guid id,
        [FromQuery, Required] string status,
        [FromQuery] string? statusMessage = null)
    {
        try
        {
            _logger.LogInformation("Updating market status: {MarketId} -> {Status}", id, status);

            var updated = await _marketService.UpdateMarketStatusAsync(id, status, statusMessage);

            if (!updated)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Market with ID {id} not found", 404));
            }

            return Ok(ApiResponse<object>.SuccessResult(
                null,
                $"Market status updated to {status}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating market status: {MarketId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to update market status", 500));
        }
    }
}