using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Api.Controllers;

/// <summary>
/// API controller for asset class management
/// Supports CRYPTO, STOCK_BIST, STOCK_NASDAQ, and other asset classes
/// </summary>
[ApiController]
[Route("api/asset-classes")]
[Authorize]
public class AssetClassController : ControllerBase
{
    private readonly IAssetClassService _assetClassService;
    private readonly ILogger<AssetClassController> _logger;

    public AssetClassController(
        IAssetClassService assetClassService,
        ILogger<AssetClassController> logger)
    {
        _assetClassService = assetClassService;
        _logger = logger;
    }

    /// <summary>
    /// Get all asset classes
    /// </summary>
    /// <returns>List of all asset classes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AssetClassDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<AssetClassDto>>>> GetAllAssetClasses()
    {
        try
        {
            _logger.LogInformation("Getting all asset classes");

            var assetClasses = await _assetClassService.GetAllAsync();

            return Ok(ApiResponse<List<AssetClassDto>>.SuccessResult(
                assetClasses,
                $"Retrieved {assetClasses.Count} asset classes"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all asset classes");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve asset classes", 500));
        }
    }

    /// <summary>
    /// Get active asset classes only
    /// </summary>
    /// <returns>List of active asset classes</returns>
    [HttpGet("active")]
    [AllowAnonymous] // Allow public access for asset class discovery
    [ProducesResponseType(typeof(ApiResponse<List<AssetClassDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<AssetClassDto>>>> GetActiveAssetClasses()
    {
        try
        {
            _logger.LogInformation("Getting active asset classes");

            var assetClasses = await _assetClassService.GetActiveAsync();

            return Ok(ApiResponse<List<AssetClassDto>>.SuccessResult(
                assetClasses,
                $"Retrieved {assetClasses.Count} active asset classes"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active asset classes");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve active asset classes", 500));
        }
    }

    /// <summary>
    /// Get asset class by ID
    /// </summary>
    /// <param name="id">Asset class ID</param>
    /// <returns>Asset class details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AssetClassDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<AssetClassDto>>> GetAssetClassById(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting asset class by ID: {AssetClassId}", id);

            var assetClass = await _assetClassService.GetByIdAsync(id);

            if (assetClass == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Asset class with ID {id} not found", 404));
            }

            return Ok(ApiResponse<AssetClassDto>.SuccessResult(
                assetClass,
                "Asset class retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset class by ID: {AssetClassId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve asset class", 500));
        }
    }

    /// <summary>
    /// Get asset class by code
    /// </summary>
    /// <param name="code">Asset class code (e.g., CRYPTO, STOCK_BIST)</param>
    /// <returns>Asset class details</returns>
    [HttpGet("by-code/{code}")]
    [AllowAnonymous] // Allow public access for asset class discovery
    [ProducesResponseType(typeof(ApiResponse<AssetClassDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<AssetClassDto>>> GetAssetClassByCode(
        [Required] string code)
    {
        try
        {
            _logger.LogInformation("Getting asset class by code: {Code}", code);

            var assetClass = await _assetClassService.GetByCodeAsync(code);

            if (assetClass == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Asset class with code '{code}' not found", 404));
            }

            return Ok(ApiResponse<AssetClassDto>.SuccessResult(
                assetClass,
                "Asset class retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset class by code: {Code}", code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve asset class", 500));
        }
    }

    /// <summary>
    /// Create a new asset class
    /// </summary>
    /// <param name="request">Asset class creation request</param>
    /// <returns>Created asset class</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AssetClassDto>), 201)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<AssetClassDto>>> CreateAssetClass(
        [FromBody, Required] CreateAssetClassRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new asset class: {Code}", request.Code);

            // Check if code is unique
            var isUnique = await _assetClassService.IsCodeUniqueAsync(request.Code);
            if (!isUnique)
            {
                return Conflict(ApiResponse<object>.ErrorResult(
                    $"Asset class with code '{request.Code}' already exists", 409));
            }

            var assetClass = await _assetClassService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetAssetClassById),
                new { id = assetClass.Id },
                ApiResponse<AssetClassDto>.SuccessResult(
                    assetClass,
                    "Asset class created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation creating asset class: {Message}", ex.Message);
            return Conflict(ApiResponse<object>.ErrorResult(ex.Message, 409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset class: {Code}", request.Code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to create asset class", 500));
        }
    }

    /// <summary>
    /// Update an existing asset class
    /// </summary>
    /// <param name="id">Asset class ID</param>
    /// <param name="request">Asset class update request</param>
    /// <returns>Updated asset class</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AssetClassDto>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<AssetClassDto>>> UpdateAssetClass(
        Guid id,
        [FromBody, Required] UpdateAssetClassRequest request)
    {
        try
        {
            _logger.LogInformation("Updating asset class: {AssetClassId}", id);

            var assetClass = await _assetClassService.UpdateAsync(id, request);

            if (assetClass == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Asset class with ID {id} not found", 404));
            }

            return Ok(ApiResponse<AssetClassDto>.SuccessResult(
                assetClass,
                "Asset class updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset class: {AssetClassId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to update asset class", 500));
        }
    }

    /// <summary>
    /// Delete an asset class
    /// </summary>
    /// <param name="id">Asset class ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAssetClass(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting asset class: {AssetClassId}", id);

            var deleted = await _assetClassService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Asset class with ID {id} not found", 404));
            }

            return Ok(ApiResponse<object>.SuccessResult(
                null,
                "Asset class deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot delete asset class: {Message}", ex.Message);
            return Conflict(ApiResponse<object>.ErrorResult(ex.Message, 409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset class: {AssetClassId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to delete asset class", 500));
        }
    }

    /// <summary>
    /// Check if asset class code is unique
    /// </summary>
    /// <param name="code">Asset class code to check</param>
    /// <param name="excludeId">Optional ID to exclude from uniqueness check</param>
    /// <returns>Uniqueness status</returns>
    [HttpGet("check-unique/{code}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<bool>>> CheckCodeUniqueness(
        [Required] string code,
        [FromQuery] Guid? excludeId = null)
    {
        try
        {
            _logger.LogInformation("Checking asset class code uniqueness: {Code}", code);

            var isUnique = await _assetClassService.IsCodeUniqueAsync(code, excludeId);

            return Ok(ApiResponse<bool>.SuccessResult(
                isUnique,
                $"Asset class code '{code}' is {(isUnique ? "unique" : "already taken")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking asset class code uniqueness: {Code}", code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to check code uniqueness", 500));
        }
    }

    /// <summary>
    /// Get asset class statistics
    /// </summary>
    /// <returns>Asset class statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> GetAssetClassStatistics()
    {
        try
        {
            _logger.LogInformation("Getting asset class statistics");

            var allAssetClasses = await _assetClassService.GetAllAsync();
            var activeAssetClasses = allAssetClasses.Where(ac => ac.IsActive).ToList();

            var statistics = new
            {
                TotalAssetClasses = allAssetClasses.Count,
                ActiveAssetClasses = activeAssetClasses.Count,
                InactiveAssetClasses = allAssetClasses.Count - activeAssetClasses.Count,
                TotalMarkets = allAssetClasses.Sum(ac => ac.MarketsCount),
                TotalSymbols = allAssetClasses.Sum(ac => ac.SymbolsCount),
                AssetClassBreakdown = activeAssetClasses.GroupBy(ac => ac.Code)
                    .Select(g => new
                    {
                        Code = g.Key,
                        Count = g.Count(),
                        MarketsCount = g.Sum(ac => ac.MarketsCount),
                        SymbolsCount = g.Sum(ac => ac.SymbolsCount)
                    }).ToList(),
                LastUpdated = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.SuccessResult(
                statistics,
                "Asset class statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset class statistics");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve asset class statistics", 500));
        }
    }
}