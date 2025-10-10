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
[Route("api/v1/market-data")] // New API versioning route
public class MarketDataController : ControllerBase
{
    private readonly IMultiAssetDataService _multiAssetDataService;
    private readonly IDataProviderOrchestrator _dataProviderOrchestrator;
    private readonly MyTrader.Infrastructure.Services.IEnhancedDbConnectionManager _dbConnectionManager;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(
        IMultiAssetDataService multiAssetDataService,
        IDataProviderOrchestrator dataProviderOrchestrator,
        MyTrader.Infrastructure.Services.IEnhancedDbConnectionManager dbConnectionManager,
        ILogger<MarketDataController> logger)
    {
        _multiAssetDataService = multiAssetDataService;
        _dataProviderOrchestrator = dataProviderOrchestrator;
        _dbConnectionManager = dbConnectionManager;
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

            // Use Enhanced Database Connection Manager for database operations
            var marketData = await _dbConnectionManager.ExecuteWithRetryAsync(async () =>
            {
                return await _multiAssetDataService.GetMarketDataAsync(symbolId);
            });

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
    /// Get top symbols by volume per asset class for market leaders dashboard
    /// High-performance endpoint with <100ms response time requirement
    /// </summary>
    /// <param name="perClass">Number of symbols to return per asset class (default: 8)</param>
    /// <returns>Volume leaders grouped by asset class</returns>
    [HttpGet("top-by-volume")]
    [AllowAnonymous] // Allow public access for volume leaders
    [ProducesResponseType(typeof(ApiResponse<List<VolumeLeaderDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<VolumeLeaderDto>>>> GetTopByVolumePerAssetClass(
        [FromQuery] int perClass = 8)
    {
        try
        {
            _logger.LogInformation("Getting top {PerClass} symbols by volume per asset class", perClass);

            var volumeLeaders = await _multiAssetDataService.GetTopByVolumePerAssetClassAsync(perClass);

            return Ok(ApiResponse<List<VolumeLeaderDto>>.SuccessResult(
                volumeLeaders,
                $"Retrieved {volumeLeaders.Count} volume leaders across asset classes"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting volume leaders");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve volume leaders", 500));
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
    /// Get symbols for frontend (simplified format)
    /// </summary>
    /// <returns>List of symbols in frontend format</returns>
    [HttpGet("symbols")]
    [AllowAnonymous] // Allow public access for symbols
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult> GetSymbols()
    {
        try
        {
            _logger.LogInformation("Getting symbols for frontend");

            // Use popular symbols as the basis for frontend display
            var popularSymbols = await _multiAssetDataService.GetPopularSymbolsAsync(20);

            var symbols = popularSymbols.ToDictionary(
                s => s.Ticker.Replace("-USD", "").Replace("USDT", ""), // Clean symbol name
                s => new
                {
                    symbol = s.Ticker,
                    display_name = s.Display ?? s.Ticker,
                    precision = 2,
                    strategy_type = "quality_over_quantity"
                }
            );

            return Ok(new { symbols, interval = "1m" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols for frontend");
            // Return fallback mock data
            var mockSymbols = new Dictionary<string, object>
            {
                ["BTC"] = new { symbol = "BTC-USD", display_name = "Bitcoin", precision = 2, strategy_type = "quality_over_quantity" },
                ["ETH"] = new { symbol = "ETH-USD", display_name = "Ethereum", precision = 2, strategy_type = "quality_over_quantity" },
                ["SOL"] = new { symbol = "SOL-USD", display_name = "Solana", precision = 2, strategy_type = "quality_over_quantity" },
                ["AVAX"] = new { symbol = "AVAX-USD", display_name = "Avalanche", precision = 2, strategy_type = "quality_over_quantity" },
                ["LINK"] = new { symbol = "LINK-USD", display_name = "Chainlink", precision = 2, strategy_type = "quality_over_quantity" }
            };

            return Ok(new { symbols = mockSymbols, interval = "1m" });
        }
    }

    /// <summary>
    /// Get database connection health status
    /// </summary>
    /// <returns>Database connection health information</returns>
    [HttpGet("database/health")]
    [Authorize] // Require authentication for database health
    [ProducesResponseType(typeof(ApiResponse<MyTrader.Infrastructure.Services.ConnectionHealthStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MyTrader.Infrastructure.Services.ConnectionHealthStatus>>> GetDatabaseHealth()
    {
        try
        {
            _logger.LogInformation("Getting database connection health status");

            var healthStatus = _dbConnectionManager.GetHealthStatus();

            return Ok(ApiResponse<MyTrader.Infrastructure.Services.ConnectionHealthStatus>.SuccessResult(
                healthStatus,
                $"Database connection is {healthStatus.Status}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database health status");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve database health status", 500));
        }
    }

    /// <summary>
    /// Get database migration status
    /// </summary>
    /// <returns>Database migration status information</returns>
    [HttpGet("database/migrations")]
    [Authorize] // Require authentication for migration status
    [ProducesResponseType(typeof(ApiResponse<MyTrader.Infrastructure.Services.MigrationStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MyTrader.Infrastructure.Services.MigrationStatus>>> GetMigrationStatus()
    {
        try
        {
            _logger.LogInformation("Getting database migration status");

            var migrationManager = HttpContext.RequestServices.GetRequiredService<MyTrader.Infrastructure.Services.IDatabaseMigrationManager>();
            var migrationStatus = await migrationManager.GetMigrationStatusAsync();

            return Ok(ApiResponse<MyTrader.Infrastructure.Services.MigrationStatus>.SuccessResult(
                migrationStatus,
                $"Database has {migrationStatus.AppliedMigrationsCount} applied migrations and {migrationStatus.PendingMigrationsCount} pending"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database migration status");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve database migration status", 500));
        }
    }

    // NOTE: WebSocket health/metrics/reconnect endpoints temporarily disabled
    // Reason: IEnhancedBinanceWebSocketService interface not implemented
    // TODO: Implement IEnhancedBinanceWebSocketService or refactor to use IBinanceWebSocketService

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

    /// <summary>
    /// Get live cryptocurrency market data from Alpaca
    /// </summary>
    /// <param name="symbols">Optional comma-separated list of crypto symbols (e.g., BTCUSD,ETHUSD)</param>
    /// <returns>Live crypto market data</returns>
    [HttpGet("crypto")]
    [AllowAnonymous] // Allow public access for crypto data
    [ProducesResponseType(typeof(ApiResponse<List<MarketDataDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<MarketDataDto>>>> GetCryptoMarketData(
        [FromQuery] string? symbols = null)
    {
        try
        {
            _logger.LogInformation("Getting crypto market data for symbols: {Symbols}", symbols ?? "default");

            var symbolList = string.IsNullOrEmpty(symbols)
                ? null
                : symbols.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToList();

            var alpacaService = HttpContext.RequestServices.GetRequiredService<IAlpacaMarketDataService>();
            var cryptoData = await alpacaService.GetUnifiedMarketDataAsync(symbolList, "CRYPTO");

            return Ok(ApiResponse<List<MarketDataDto>>.SuccessResult(
                cryptoData,
                $"Retrieved {cryptoData.Count} crypto symbols"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting crypto market data");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve crypto market data", 500));
        }
    }

    /// <summary>
    /// Get live NASDAQ stock market data from Alpaca
    /// </summary>
    /// <param name="symbols">Optional comma-separated list of stock symbols (e.g., AAPL,GOOGL,MSFT)</param>
    /// <returns>Live NASDAQ stock market data</returns>
    [HttpGet("nasdaq")]
    [AllowAnonymous] // Allow public access for stock data
    [ProducesResponseType(typeof(ApiResponse<List<MarketDataDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<MarketDataDto>>>> GetNasdaqMarketData(
        [FromQuery] string? symbols = null)
    {
        try
        {
            _logger.LogInformation("Getting NASDAQ market data for symbols: {Symbols}", symbols ?? "default");

            var symbolList = string.IsNullOrEmpty(symbols)
                ? null
                : symbols.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToList();

            var alpacaService = HttpContext.RequestServices.GetRequiredService<IAlpacaMarketDataService>();
            var stockData = await alpacaService.GetUnifiedMarketDataAsync(symbolList, "STOCK");

            return Ok(ApiResponse<List<MarketDataDto>>.SuccessResult(
                stockData,
                $"Retrieved {stockData.Count} stock symbols"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting NASDAQ market data");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve NASDAQ market data", 500));
        }
    }

    /// <summary>
    /// Get Alpaca API health status and connection information
    /// </summary>
    /// <returns>Alpaca API health status</returns>
    [HttpGet("alpaca/health")]
    [AllowAnonymous] // Allow public access for health check to support web frontend status indicators
    [ProducesResponseType(typeof(ApiResponse<AlpacaHealthStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<AlpacaHealthStatus>>> GetAlpacaHealthStatus()
    {
        try
        {
            _logger.LogInformation("Getting Alpaca API health status");

            var alpacaService = HttpContext.RequestServices.GetRequiredService<IAlpacaMarketDataService>();
            var healthStatus = await alpacaService.GetHealthStatusAsync();

            return Ok(ApiResponse<AlpacaHealthStatus>.SuccessResult(
                healthStatus,
                $"Alpaca API is {healthStatus.Status}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Alpaca health status");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve Alpaca health status", 500));
        }
    }

    /// <summary>
    /// Test Alpaca API connectivity
    /// </summary>
    /// <returns>Connection test result</returns>
    [HttpGet("alpaca/test")]
    [Authorize] // Require authentication for connection test
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<bool>>> TestAlpacaConnection()
    {
        try
        {
            _logger.LogInformation("Testing Alpaca API connection");

            var alpacaService = HttpContext.RequestServices.GetRequiredService<IAlpacaMarketDataService>();
            var isConnected = await alpacaService.TestConnectionAsync();

            return Ok(ApiResponse<bool>.SuccessResult(
                isConnected,
                isConnected ? "Alpaca API connection successful" : "Alpaca API connection failed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Alpaca connection");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to test Alpaca connection", 500));
        }
    }

    /// <summary>
    /// Get available symbols from Alpaca
    /// </summary>
    /// <param name="assetClass">Optional asset class filter (CRYPTO, STOCK)</param>
    /// <returns>List of available symbols</returns>
    [HttpGet("alpaca/symbols")]
    [AllowAnonymous] // Allow public access for symbols
    [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetAlpacaSymbols(
        [FromQuery] string? assetClass = null)
    {
        try
        {
            _logger.LogInformation("Getting Alpaca symbols for asset class: {AssetClass}", assetClass ?? "all");

            var alpacaService = HttpContext.RequestServices.GetRequiredService<IAlpacaMarketDataService>();
            var symbols = await alpacaService.GetAvailableSymbolsAsync(assetClass);

            return Ok(ApiResponse<List<string>>.SuccessResult(
                symbols,
                $"Retrieved {symbols.Count} symbols"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Alpaca symbols");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve Alpaca symbols", 500));
        }
    }

    // === BIST MARKET DATA ENDPOINTS ===

    /// <summary>
    /// Get BIST (Borsa Istanbul) market data for Turkish stocks
    /// Optimized for sub-100ms response times with intelligent caching
    /// </summary>
    /// <param name="symbols">Optional comma-separated list of BIST symbols (e.g., THYAO,AKBNK,ISCTR)</param>
    /// <param name="limit">Maximum number of stocks to return (default: 50)</param>
    /// <returns>BIST market data in unified format</returns>
    [HttpGet("bist")]
    [AllowAnonymous] // Allow public access for BIST data
    [ProducesResponseType(typeof(ApiResponse<List<MarketDataDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<MarketDataDto>>>> GetBistMarketData(
        [FromQuery] string? symbols = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            _logger.LogInformation("Getting BIST market data for symbols: {Symbols}", symbols ?? "all");

            var symbolList = string.IsNullOrEmpty(symbols)
                ? null
                : symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToUpper())
                        .ToList();

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var bistData = await bistService.GetBistMarketDataAsync(symbolList, limit);

            return Ok(ApiResponse<List<MarketDataDto>>.SuccessResult(
                bistData,
                $"Retrieved {bistData.Count} BIST stocks"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BIST market data");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve BIST market data", 500));
        }
    }

    /// <summary>
    /// Get individual BIST stock data
    /// Ultra-fast endpoint with <10ms target response time
    /// </summary>
    /// <param name="symbol">BIST stock symbol (e.g., THYAO, AKBNK)</param>
    /// <returns>Individual stock market data</returns>
    [HttpGet("bist/{symbol}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<MarketDataDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<MarketDataDto>>> GetBistStockData(string symbol)
    {
        try
        {
            _logger.LogDebug("Getting BIST stock data for: {Symbol}", symbol);

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var stockData = await bistService.GetBistStockDataAsync(symbol.ToUpper());

            if (stockData == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"BIST stock {symbol} not found", 404));
            }

            return Ok(ApiResponse<MarketDataDto>.SuccessResult(
                stockData,
                $"Retrieved BIST stock data for {symbol}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BIST stock data for {Symbol}", symbol);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve BIST stock data", 500));
        }
    }

    /// <summary>
    /// Get BIST market overview for dashboard
    /// Includes market statistics, volume, and performance metrics
    /// </summary>
    /// <returns>BIST market overview</returns>
    [HttpGet("bist/overview")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BistMarketOverviewDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<BistMarketOverviewDto>>> GetBistMarketOverview()
    {
        try
        {
            _logger.LogInformation("Getting BIST market overview");

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var overview = await bistService.GetBistMarketOverviewAsync();

            return Ok(ApiResponse<BistMarketOverviewDto>.SuccessResult(
                overview,
                $"BIST overview: {overview.TotalStocks} stocks, {overview.GainersCount} gainers, {overview.LosersCount} losers"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BIST market overview");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve BIST market overview", 500));
        }
    }

    /// <summary>
    /// Get BIST top movers (gainers, losers, most active)
    /// High-performance endpoint for dashboard widgets
    /// </summary>
    /// <returns>BIST top movers data</returns>
    [HttpGet("bist/top-movers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BistTopMoversDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<BistTopMoversDto>>> GetBistTopMovers()
    {
        try
        {
            _logger.LogInformation("Getting BIST top movers");

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var topMovers = await bistService.GetBistTopMoversAsync();

            return Ok(ApiResponse<BistTopMoversDto>.SuccessResult(
                topMovers,
                $"Retrieved {topMovers.Gainers.Count} gainers, {topMovers.Losers.Count} losers, {topMovers.MostActive.Count} most active"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BIST top movers");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve BIST top movers", 500));
        }
    }

    /// <summary>
    /// Get BIST sector performance analysis
    /// Shows performance by industry sectors
    /// </summary>
    /// <returns>BIST sector performance data</returns>
    [HttpGet("bist/sectors")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<BistSectorPerformanceDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<BistSectorPerformanceDto>>>> GetBistSectorPerformance()
    {
        try
        {
            _logger.LogInformation("Getting BIST sector performance");

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var sectors = await bistService.GetBistSectorPerformanceAsync();

            return Ok(ApiResponse<List<BistSectorPerformanceDto>>.SuccessResult(
                sectors,
                $"Retrieved performance data for {sectors.Count} sectors"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BIST sector performance");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve BIST sector performance", 500));
        }
    }

    /// <summary>
    /// Search BIST stocks by symbol or company name
    /// Supports fuzzy matching and Turkish characters
    /// </summary>
    /// <param name="q">Search query (minimum 2 characters)</param>
    /// <param name="limit">Maximum results to return</param>
    /// <returns>BIST stock search results</returns>
    [HttpGet("bist/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<BistStockSearchResultDto>>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<BistStockSearchResultDto>>>> SearchBistStocks(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Invalid search query",
                    Errors = { ["query"] = new List<string> { "Search query must be at least 2 characters long" } }
                });
            }

            _logger.LogDebug("Searching BIST stocks for: {Query}", q);

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var searchResults = await bistService.SearchBistStocksAsync(q, limit);

            return Ok(ApiResponse<List<BistStockSearchResultDto>>.SuccessResult(
                searchResults,
                $"Found {searchResults.Count} BIST stocks matching '{q}'"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching BIST stocks for query: {Query}", q);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to search BIST stocks", 500));
        }
    }

    /// <summary>
    /// Get BIST historical data for a stock
    /// Supports multiple time periods and intervals
    /// </summary>
    /// <param name="symbol">BIST stock symbol</param>
    /// <param name="period">Time period (1d, 5d, 1m, 3m, 6m, 1y, 2y, 5y)</param>
    /// <param name="interval">Data interval (1d, 1w, 1m)</param>
    /// <returns>Historical price data</returns>
    [HttpGet("bist/{symbol}/history")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BistHistoricalDataDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<BistHistoricalDataDto>>> GetBistHistoricalData(
        string symbol,
        [FromQuery] string period = "1m",
        [FromQuery] string interval = "1d")
    {
        try
        {
            _logger.LogDebug("Getting BIST historical data for {Symbol} ({Period}/{Interval})",
                symbol, period, interval);

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var historicalData = await bistService.GetBistHistoricalDataAsync(symbol.ToUpper(), period, interval);

            if (historicalData == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Historical data not available for BIST stock {symbol}", 404));
            }

            return Ok(ApiResponse<BistHistoricalDataDto>.SuccessResult(
                historicalData,
                $"Retrieved {historicalData.CandleCount} candles for {symbol}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BIST historical data for {Symbol}", symbol);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve BIST historical data", 500));
        }
    }

    /// <summary>
    /// Get BIST market status (open/closed)
    /// Returns current market hours and trading status
    /// </summary>
    /// <returns>BIST market status</returns>
    [HttpGet("bist/status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BistMarketStatusDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<BistMarketStatusDto>>> GetBistMarketStatus()
    {
        try
        {
            _logger.LogDebug("Getting BIST market status");

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var marketStatus = await bistService.GetBistMarketStatusAsync();

            return Ok(ApiResponse<BistMarketStatusDto>.SuccessResult(
                marketStatus,
                $"BIST market is {marketStatus.Status}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BIST market status");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve BIST market status", 500));
        }
    }

    /// <summary>
    /// Get BIST service health and performance metrics
    /// </summary>
    /// <returns>BIST service health status</returns>
    [HttpGet("bist/health")]
    [Authorize] // Require authentication for health check
    [ProducesResponseType(typeof(ApiResponse<BistCacheHealthDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<BistCacheHealthDto>>> GetBistHealthStatus()
    {
        try
        {
            _logger.LogInformation("Getting BIST service health status");

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var healthStatus = await bistService.GetCacheHealthAsync();

            return Ok(ApiResponse<BistCacheHealthDto>.SuccessResult(
                healthStatus,
                $"BIST service is {(healthStatus.IsHealthy ? "healthy" : "unhealthy")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BIST health status");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve BIST health status", 500));
        }
    }

    /// <summary>
    /// Manually refresh BIST cache data
    /// Should be used sparingly as it can impact performance
    /// </summary>
    /// <returns>Cache refresh result</returns>
    [HttpPost("bist/refresh")]
    [Authorize] // Require authentication for cache refresh
    [ProducesResponseType(typeof(ApiResponse<BistCacheRefreshResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<BistCacheRefreshResultDto>>> RefreshBistData()
    {
        try
        {
            _logger.LogInformation("Manual BIST cache refresh requested");

            var bistService = HttpContext.RequestServices.GetRequiredService<IBistMarketDataService>();
            var refreshResult = await bistService.RefreshBistDataAsync();

            return Ok(ApiResponse<BistCacheRefreshResultDto>.SuccessResult(
                refreshResult,
                refreshResult.Success
                    ? $"BIST cache refreshed: {refreshResult.SymbolsUpdated} symbols updated in {refreshResult.RefreshDuration.TotalMilliseconds}ms"
                    : $"BIST cache refresh failed: {refreshResult.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing BIST cache");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to refresh BIST cache", 500));
        }
    }
}