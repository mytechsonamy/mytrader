using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Unified market data API controller for all asset classes
/// Provides real-time and historical data across crypto, stocks, forex, etc.
/// </summary>
[ApiController]
[Route("api/market-data")]
public class MarketDataController : ControllerBase
{
    private readonly IMultiAssetDataService _multiAssetDataService;
    private readonly IDataProviderOrchestrator _dataProviderOrchestrator;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(
        IMultiAssetDataService multiAssetDataService,
        IDataProviderOrchestrator dataProviderOrchestrator,
        ILogger<MarketDataController> logger)
    {
        _multiAssetDataService = multiAssetDataService;
        _dataProviderOrchestrator = dataProviderOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Get real-time market data for a symbol
    /// </summary>
    /// <param name="symbolId">Symbol ID</param>
    /// <returns>Unified market data</returns>
    [HttpGet("realtime/{symbolId:guid}")]
    [AllowAnonymous] // Allow public access for market data
    [ProducesResponseType(typeof(ApiResponse<UnifiedMarketDataDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<UnifiedMarketDataDto>>> GetRealtimeMarketData(
        Guid symbolId)
    {
        try
        {
            _logger.LogInformation("Getting real-time market data for symbol: {SymbolId}", symbolId);

            var marketData = await _multiAssetDataService.GetMarketDataAsync(symbolId);

            if (marketData == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Market data not available for symbol {symbolId}", 404));
            }

            return Ok(ApiResponse<UnifiedMarketDataDto>.SuccessResult(
                marketData,
                "Real-time market data retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real-time market data for symbol: {SymbolId}", symbolId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve real-time market data", 500));
        }
    }

    /// <summary>
    /// Get batch market data for multiple symbols
    /// </summary>
    /// <param name="request">Market data request with symbol IDs</param>
    /// <returns>Batch market data response</returns>
    [HttpPost("batch")]
    [AllowAnonymous] // Allow public access for market data
    [ProducesResponseType(typeof(ApiResponse<BatchMarketDataDto>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<BatchMarketDataDto>>> GetBatchMarketData(
        [FromBody, Required] MarketDataRequest request)
    {
        try
        {
            _logger.LogInformation("Getting batch market data for {Count} symbols", request.SymbolIds.Count);

            var batchData = await _multiAssetDataService.GetBatchMarketDataAsync(request.SymbolIds);

            return Ok(ApiResponse<BatchMarketDataDto>.SuccessResult(
                batchData,
                $"Batch market data retrieved for {batchData.SuccessfulSymbols}/{batchData.TotalSymbols} symbols"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch market data");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve batch market data", 500));
        }
    }

    /// <summary>
    /// Get historical candlestick data
    /// </summary>
    /// <param name="symbolId">Symbol ID</param>
    /// <param name="request">Historical data request parameters</param>
    /// <returns>Historical market data</returns>
    [HttpGet("historical/{symbolId:guid}")]
    [AllowAnonymous] // Allow public access for historical data
    [ProducesResponseType(typeof(ApiResponse<HistoricalMarketDataDto>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<HistoricalMarketDataDto>>> GetHistoricalData(
        Guid symbolId,
        [FromQuery] HistoricalDataRequest request)
    {
        try
        {
            _logger.LogInformation("Getting historical data for symbol: {SymbolId}, interval: {Interval}",
                symbolId, request.Interval);

            // Override symbolId from route
            request.SymbolId = symbolId;

            var historicalData = await _multiAssetDataService.GetHistoricalDataAsync(
                symbolId,
                request.Interval,
                request.StartTime,
                request.EndTime,
                request.Limit);

            if (historicalData == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Historical data not available for symbol {symbolId}", 404));
            }

            return Ok(ApiResponse<HistoricalMarketDataDto>.SuccessResult(
                historicalData,
                $"Historical data retrieved with {historicalData.CandleCount} candles"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical data for symbol: {SymbolId}", symbolId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve historical data", 500));
        }
    }

    /// <summary>
    /// Get market statistics for a symbol
    /// </summary>
    /// <param name="symbolId">Symbol ID</param>
    /// <returns>Market statistics</returns>
    [HttpGet("statistics/{symbolId:guid}")]
    [AllowAnonymous] // Allow public access for market statistics
    [ProducesResponseType(typeof(ApiResponse<MarketStatisticsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MarketStatisticsDto>>> GetMarketStatistics(
        Guid symbolId)
    {
        try
        {
            _logger.LogInformation("Getting market statistics for symbol: {SymbolId}", symbolId);

            var statistics = await _multiAssetDataService.GetMarketStatisticsAsync(symbolId);

            if (statistics == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Market statistics not available for symbol {symbolId}", 404));
            }

            return Ok(ApiResponse<MarketStatisticsDto>.SuccessResult(
                statistics,
                "Market statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market statistics for symbol: {SymbolId}", symbolId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve market statistics", 500));
        }
    }

    /// <summary>
    /// Get market overview for dashboard
    /// </summary>
    /// <returns>Market overview data</returns>
    [HttpGet("overview")]
    [AllowAnonymous] // Allow public access for market overview
    [ProducesResponseType(typeof(ApiResponse<MarketOverviewDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MarketOverviewDto>>> GetMarketOverview()
    {
        try
        {
            _logger.LogInformation("Getting market overview");

            var overview = await _multiAssetDataService.GetMarketOverviewAsync();

            return Ok(ApiResponse<MarketOverviewDto>.SuccessResult(
                overview,
                "Market overview retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market overview");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve market overview", 500));
        }
    }

    /// <summary>
    /// Get top movers (gainers/losers)
    /// </summary>
    /// <param name="assetClass">Optional asset class filter</param>
    /// <param name="limit">Number of results per category</param>
    /// <returns>Top movers data</returns>
    [HttpGet("top-movers")]
    [AllowAnonymous] // Allow public access for top movers
    [ProducesResponseType(typeof(ApiResponse<TopMoversDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<TopMoversDto>>> GetTopMovers(
        [FromQuery] string? assetClass = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            _logger.LogInformation("Getting top movers for asset class: {AssetClass}, limit: {Limit}",
                assetClass ?? "all", limit);

            var topMovers = await _multiAssetDataService.GetTopMoversAsync(assetClass, limit);

            return Ok(ApiResponse<TopMoversDto>.SuccessResult(
                topMovers,
                $"Top movers retrieved with {topMovers.Gainers.Count} gainers and {topMovers.Losers.Count} losers"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top movers");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve top movers", 500));
        }
    }

    /// <summary>
    /// Get popular symbols across all asset classes
    /// </summary>
    /// <param name="limit">Number of results to return</param>
    /// <returns>Popular symbols</returns>
    [HttpGet("popular")]
    [AllowAnonymous] // Allow public access for popular symbols
    [ProducesResponseType(typeof(ApiResponse<List<SymbolSummaryDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<SymbolSummaryDto>>>> GetPopularSymbols(
        [FromQuery] int limit = 50)
    {
        try
        {
            _logger.LogInformation("Getting popular symbols, limit: {Limit}", limit);

            var popularSymbols = await _multiAssetDataService.GetPopularSymbolsAsync(limit);

            return Ok(ApiResponse<List<SymbolSummaryDto>>.SuccessResult(
                popularSymbols,
                $"Retrieved {popularSymbols.Count} popular symbols"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular symbols");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve popular symbols", 500));
        }
    }

    /// <summary>
    /// Subscribe to real-time updates for symbols
    /// </summary>
    /// <param name="symbolIds">List of symbol IDs to subscribe to</param>
    /// <returns>Subscription status</returns>
    [HttpPost("subscribe")]
    [Authorize] // Require authentication for subscriptions
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<bool>>> SubscribeToRealtimeUpdates(
        [FromBody, Required] List<Guid> symbolIds)
    {
        try
        {
            _logger.LogInformation("Subscribing to real-time updates for {Count} symbols", symbolIds.Count);

            var success = await _multiAssetDataService.SubscribeToRealtimeUpdatesAsync(symbolIds);

            return Ok(ApiResponse<bool>.SuccessResult(
                success,
                success ? "Successfully subscribed to real-time updates" : "Failed to subscribe to real-time updates"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to real-time updates");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to subscribe to real-time updates", 500));
        }
    }

    /// <summary>
    /// Unsubscribe from real-time updates
    /// </summary>
    /// <param name="symbolIds">List of symbol IDs to unsubscribe from</param>
    /// <returns>Unsubscription status</returns>
    [HttpPost("unsubscribe")]
    [Authorize] // Require authentication for subscriptions
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<bool>>> UnsubscribeFromRealtimeUpdates(
        [FromBody, Required] List<Guid> symbolIds)
    {
        try
        {
            _logger.LogInformation("Unsubscribing from real-time updates for {Count} symbols", symbolIds.Count);

            var success = await _multiAssetDataService.UnsubscribeFromRealtimeUpdatesAsync(symbolIds);

            return Ok(ApiResponse<bool>.SuccessResult(
                success,
                success ? "Successfully unsubscribed from real-time updates" : "Failed to unsubscribe from real-time updates"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from real-time updates");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to unsubscribe from real-time updates", 500));
        }
    }

    /// <summary>
    /// Get data provider health status
    /// </summary>
    /// <returns>Health status of all data providers</returns>
    [HttpGet("providers/health")]
    [Authorize] // Require authentication for provider health
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, ComponentHealth>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, ComponentHealth>>>> GetProvidersHealth()
    {
        try
        {
            _logger.LogInformation("Getting data providers health status");

            var health = await _dataProviderOrchestrator.GetAllProvidersHealthAsync();

            return Ok(ApiResponse<Dictionary<string, ComponentHealth>>.SuccessResult(
                health,
                $"Retrieved health status for {health.Count} data providers"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data providers health");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve data providers health", 500));
        }
    }

    /// <summary>
    /// Get available data providers
    /// </summary>
    /// <returns>List of available data providers</returns>
    [HttpGet("providers")]
    [Authorize] // Require authentication for provider info
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> GetAvailableProviders()
    {
        try
        {
            _logger.LogInformation("Getting available data providers");

            var providers = _dataProviderOrchestrator.GetProviders();

            var providerInfo = providers.Select(p => new
            {
                ProviderId = p.ProviderId,
                ProviderName = p.ProviderName,
                SupportedAssetClasses = p.SupportedAssetClasses,
                SupportedMarkets = p.SupportedMarkets,
                IsConnected = p.IsConnected
            }).ToList();

            return Ok(ApiResponse<object>.SuccessResult(
                providerInfo,
                $"Retrieved {providerInfo.Count} data providers"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available data providers");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve data providers", 500));
        }
    }
}