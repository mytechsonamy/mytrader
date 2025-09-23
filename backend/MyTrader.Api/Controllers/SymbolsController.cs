using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Market;
using MyTrader.Core.Interfaces;
using MyTrader.Core.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Enhanced Symbols API controller with multi-asset support
/// Supports crypto, stocks, forex, and other asset classes
/// </summary>
[ApiController]
[Route("api/v1/symbols")]
public class SymbolsController : ControllerBase
{
    private readonly ISymbolService _symbolService;
    private readonly IEnhancedSymbolService _enhancedSymbolService;
    private readonly ILogger<SymbolsController> _logger;

    public SymbolsController(
        ISymbolService symbolService,
        IEnhancedSymbolService enhancedSymbolService,
        ILogger<SymbolsController> logger)
    {
        _symbolService = symbolService;
        _enhancedSymbolService = enhancedSymbolService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> GetSymbols()
    {
        // Return symbols in the format expected by frontend
        var trackedSymbols = await _symbolService.GetTrackedAsync();
        var symbols = trackedSymbols.ToDictionary(
            s => s.Ticker.Replace("USDT", ""), // Use clean symbol as key (BTC, ETH, etc.)
            s => new
            {
                symbol = s.Ticker,
                display_name = s.Display,
                precision = 2, // Default precision
                strategy_type = "quality_over_quantity" // Default strategy
            }
        );
        
        return Ok(new { symbols, interval = "1m" });
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public ActionResult GetTest()
    {
        return Ok(new { message = "Test endpoint working", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Public endpoint for market data overview
    /// </summary>
    [HttpGet("market-overview")]
    [AllowAnonymous]
    public async Task<ActionResult> GetMarketOverview()
    {
        try
        {
            var allSymbols = await _symbolService.GetActiveSymbolsAsync();
            var trackedSymbols = await _symbolService.GetTrackedAsync();
            
            // Group by venue
            var venueBreakdown = allSymbols
                .GroupBy(s => s.Venue)
                .Select(g => new
                {
                    Venue = g.Key,
                    Count = g.Count(),
                    TrackedCount = g.Count(s => s.IsTracked),
                    Symbols = g.Select(s => s.Ticker).Take(3).ToArray()
                })
                .ToArray();

            var overview = new
            {
                TotalSymbols = allSymbols.Count,
                TrackedSymbols = trackedSymbols.Count,
                VenueBreakdown = venueBreakdown,
                MarketSentiment = "Neutral", // Mock data
                LastUpdated = DateTime.UtcNow
            };

            return Ok(new { success = true, data = overview });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Failed to get market overview" });
        }
    }

    [HttpGet("tracked")]
    [Authorize]
    public async Task<ActionResult> GetTracked()
    {
        var list = await _symbolService.GetTrackedAsync();
        return Ok(list);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult> Create([FromBody] CreateSymbolRequest req)
    {
        var created = await _symbolService.GetOrCreateAsync(req.Ticker, req.Venue ?? "BINANCE", req.BaseCcy, req.QuoteCcy);
        return CreatedAtAction(nameof(GetTracked), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> Patch(Guid id, [FromBody] PatchSymbolRequest req)
    {
        var success = await _symbolService.SetTrackedAsync(id, req.IsTracked);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var symbol = await _symbolService.GetByIdAsync(id);
        return symbol != null ? Ok(symbol) : NotFound();
    }

    // ============================================
    // NEW MULTI-ASSET ENDPOINTS
    // ============================================

    /// <summary>
    /// Get symbols with pagination and filtering
    /// </summary>
    [HttpGet("enhanced")]
    [AllowAnonymous] // Allow public access for symbol discovery
    [ProducesResponseType(typeof(PaginatedResponse<SymbolSummaryDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<PaginatedResponse<SymbolSummaryDto>>> GetEnhancedSymbols(
        [FromQuery] BaseListRequest request)
    {
        try
        {
            _logger.LogInformation("Getting enhanced symbols with filters: {Search}", request.Search);

            var symbols = await _enhancedSymbolService.GetSymbolsAsync(request);

            return Ok(symbols);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced symbols");
            return StatusCode(500, PaginatedResponse<SymbolSummaryDto>.ErrorResult(
                "Failed to retrieve symbols"));
        }
    }

    /// <summary>
    /// Get symbol by ID with full enhanced details
    /// </summary>
    [HttpGet("enhanced/{id:guid}")]
    [AllowAnonymous] // Allow public access for symbol details
    [ProducesResponseType(typeof(ApiResponse<EnhancedSymbolDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<EnhancedSymbolDto>>> GetEnhancedSymbolById(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting enhanced symbol by ID: {SymbolId}", id);

            var symbol = await _enhancedSymbolService.GetByIdAsync(id);

            if (symbol == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Symbol with ID {id} not found", 404));
            }

            return Ok(ApiResponse<EnhancedSymbolDto>.SuccessResult(
                symbol,
                "Symbol retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced symbol by ID: {SymbolId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve symbol", 500));
        }
    }

    /// <summary>
    /// Search symbols across all asset classes
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous] // Allow public access for symbol search
    [ProducesResponseType(typeof(ApiResponse<List<SymbolSearchResultDto>>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<SymbolSearchResultDto>>>> SearchSymbols(
        [FromQuery] SymbolSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Searching symbols: {Query}, AssetClass: {AssetClass}",
                request.Query, request.AssetClass);

            var searchResults = await _enhancedSymbolService.SearchAsync(request);

            return Ok(ApiResponse<List<SymbolSearchResultDto>>.SuccessResult(
                searchResults,
                $"Found {searchResults.Count} symbols matching '{request.Query}'"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching symbols: {Query}", request.Query);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to search symbols", 500));
        }
    }

    /// <summary>
    /// Get symbols by asset class (by GUID)
    /// </summary>
    [HttpGet("by-asset-class/{assetClassId:guid}")]
    [AllowAnonymous] // Allow public access for asset class symbols
    [ProducesResponseType(typeof(ApiResponse<List<SymbolSummaryDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<SymbolSummaryDto>>>> GetSymbolsByAssetClass(
        Guid assetClassId,
        [FromQuery] int? limit = null)
    {
        try
        {
            _logger.LogInformation("Getting symbols by asset class: {AssetClassId}, limit: {Limit}",
                assetClassId, limit);

            var symbols = await _enhancedSymbolService.GetByAssetClassAsync(assetClassId, limit);

            return Ok(ApiResponse<List<SymbolSummaryDto>>.SuccessResult(
                symbols,
                $"Retrieved {symbols.Count} symbols for asset class"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols by asset class: {AssetClassId}", assetClassId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve symbols by asset class", 500));
        }
    }

    /// <summary>
    /// Get symbols by asset class name (CRYPTO, STOCK, etc.)
    /// Frontend-compatible endpoint
    /// </summary>
    [HttpGet("by-asset-class/{assetClassName}")]
    [AllowAnonymous] // Allow public access for asset class symbols
    [ProducesResponseType(typeof(object[]), 200)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public ActionResult GetSymbolsByAssetClassName(
        string assetClassName,
        [FromQuery] int? limit = null)
    {
        try
        {
            _logger.LogInformation("Getting symbols by asset class name: {AssetClassName}, limit: {Limit}",
                assetClassName, limit);

            // Return mock data for each asset class matching frontend expectations
            object[] symbols = Array.Empty<object>();

            switch (assetClassName.ToUpper())
            {
                case "CRYPTO":
                    symbols = GenerateMockCryptoSymbols(limit ?? 50);
                    break;
                case "STOCK":
                    symbols = GenerateMockStockSymbols(limit ?? 50);
                    break;
                default:
                    return NotFound(new { message = $"Asset class '{assetClassName}' not found" });
            }

            return Ok(symbols);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols by asset class name: {AssetClassName}", assetClassName);
            return StatusCode(500, new { message = "Failed to retrieve symbols by asset class" });
        }
    }

    /// <summary>
    /// Get symbols by market
    /// </summary>
    [HttpGet("by-market/{marketId:guid}")]
    [AllowAnonymous] // Allow public access for market symbols
    [ProducesResponseType(typeof(ApiResponse<List<SymbolSummaryDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<SymbolSummaryDto>>>> GetSymbolsByMarket(
        Guid marketId,
        [FromQuery] int? limit = null)
    {
        try
        {
            _logger.LogInformation("Getting symbols by market: {MarketId}, limit: {Limit}",
                marketId, limit);

            var symbols = await _enhancedSymbolService.GetByMarketAsync(marketId, limit);

            return Ok(ApiResponse<List<SymbolSummaryDto>>.SuccessResult(
                symbols,
                $"Retrieved {symbols.Count} symbols for market"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols by market: {MarketId}", marketId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve symbols by market", 500));
        }
    }

    /// <summary>
    /// Get enhanced tracked symbols
    /// </summary>
    [HttpGet("enhanced/tracked")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<SymbolSummaryDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<SymbolSummaryDto>>>> GetEnhancedTrackedSymbols()
    {
        try
        {
            _logger.LogInformation("Getting enhanced tracked symbols");

            var symbols = await _enhancedSymbolService.GetTrackedAsync();

            return Ok(ApiResponse<List<SymbolSummaryDto>>.SuccessResult(
                symbols,
                $"Retrieved {symbols.Count} tracked symbols"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced tracked symbols");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve tracked symbols", 500));
        }
    }

    /// <summary>
    /// Get popular symbols
    /// </summary>
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

            var symbols = await _enhancedSymbolService.GetPopularAsync(limit);

            return Ok(ApiResponse<List<SymbolSummaryDto>>.SuccessResult(
                symbols,
                $"Retrieved {symbols.Count} popular symbols"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular symbols");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to retrieve popular symbols", 500));
        }
    }

    /// <summary>
    /// Create a new symbol
    /// </summary>
    [HttpPost("enhanced")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EnhancedSymbolDto>), 201)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<EnhancedSymbolDto>>> CreateEnhancedSymbol(
        [FromBody, Required] Core.DTOs.CreateSymbolRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new symbol: {Ticker}", request.Ticker);

            // Check if ticker is unique
            var isUnique = await _enhancedSymbolService.IsTickerUniqueAsync(
                request.Ticker, request.MarketId);
            if (!isUnique)
            {
                return Conflict(ApiResponse<object>.ErrorResult(
                    $"Symbol with ticker '{request.Ticker}' already exists in this market", 409));
            }

            var symbol = await _enhancedSymbolService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetEnhancedSymbolById),
                new { id = symbol.Id },
                ApiResponse<EnhancedSymbolDto>.SuccessResult(
                    symbol,
                    "Symbol created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation creating symbol: {Message}", ex.Message);
            return Conflict(ApiResponse<object>.ErrorResult(ex.Message, 409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating symbol: {Ticker}", request.Ticker);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to create symbol", 500));
        }
    }

    /// <summary>
    /// Update symbol tracking status
    /// </summary>
    [HttpPatch("enhanced/{id:guid}/tracking")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateSymbolTracking(
        Guid id,
        [FromQuery, Required] bool isTracked)
    {
        try
        {
            _logger.LogInformation("Updating symbol tracking: {SymbolId} -> {IsTracked}", id, isTracked);

            var updated = await _enhancedSymbolService.UpdateTrackingStatusAsync(id, isTracked);

            if (!updated)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"Symbol with ID {id} not found", 404));
            }

            return Ok(ApiResponse<object>.SuccessResult(
                null,
                $"Symbol tracking {(isTracked ? "enabled" : "disabled")} successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating symbol tracking: {SymbolId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to update symbol tracking", 500));
        }
    }

    /// <summary>
    /// Bulk update symbol tracking status
    /// </summary>
    [HttpPatch("enhanced/bulk-tracking")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> BulkUpdateSymbolTracking(
        [FromBody, Required] BulkUpdateSymbolTrackingRequest request)
    {
        try
        {
            _logger.LogInformation("Bulk updating symbol tracking for {Count} symbols -> {IsTracked}",
                request.SymbolIds.Count, request.IsTracked);

            var updatedCount = await _enhancedSymbolService.BulkUpdateTrackingStatusAsync(request);

            return Ok(ApiResponse<object>.SuccessResult(
                new { UpdatedCount = updatedCount },
                $"Updated tracking status for {updatedCount}/{request.SymbolIds.Count} symbols"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating symbol tracking");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to bulk update symbol tracking", 500));
        }
    }

    // ============================================
    // MOCK DATA GENERATORS (Temporary for frontend compatibility)
    // ============================================

    private object[] GenerateMockCryptoSymbols(int limit)
    {
        var cryptoSymbols = new[]
        {
            new { Symbol = "BTC", Name = "Bitcoin", Price = 43250.00m },
            new { Symbol = "ETH", Name = "Ethereum", Price = 2680.50m },
            new { Symbol = "ADA", Name = "Cardano", Price = 0.48m },
            new { Symbol = "SOL", Name = "Solana", Price = 98.75m },
            new { Symbol = "AVAX", Name = "Avalanche", Price = 36.20m },
            new { Symbol = "MATIC", Name = "Polygon", Price = 0.85m },
            new { Symbol = "DOT", Name = "Polkadot", Price = 7.35m },
            new { Symbol = "LINK", Name = "Chainlink", Price = 14.65m },
            new { Symbol = "UNI", Name = "Uniswap", Price = 6.45m },
            new { Symbol = "LTC", Name = "Litecoin", Price = 72.80m }
        };

        return cryptoSymbols.Take(limit).Select((crypto, index) => (object)new
        {
            id = Guid.NewGuid().ToString(),
            symbol = crypto.Symbol,
            displayName = crypto.Name,
            assetClassId = Guid.NewGuid().ToString(),
            assetClassName = "CRYPTO",
            marketId = Guid.NewGuid().ToString(),
            marketName = "Crypto Market",
            baseCurrency = crypto.Symbol,
            quoteCurrency = "USDT",
            minTradeAmount = 0.001,
            maxTradeAmount = 1000000,
            priceDecimalPlaces = crypto.Symbol == "BTC" ? 2 : 4,
            quantityDecimalPlaces = 8,
            isActive = true,
            isTracked = index < 5, // Track first 5
            tickSize = crypto.Symbol == "BTC" ? 0.01 : 0.0001,
            lotSize = 0.001,
            description = $"{crypto.Name} cryptocurrency",
            sector = "Digital Assets",
            industry = "Cryptocurrency"
        }).ToArray();
    }

    private object[] GenerateMockStockSymbols(int limit)
    {
        var stockSymbols = new[]
        {
            // BIST Stocks
            new { Symbol = "TUPRS", Name = "Tüpraş", Exchange = "BIST", Currency = "TRY", Price = 115.50m },
            new { Symbol = "THYAO", Name = "Türk Hava Yolları", Exchange = "BIST", Currency = "TRY", Price = 89.25m },
            new { Symbol = "AKBNK", Name = "Akbank", Exchange = "BIST", Currency = "TRY", Price = 45.80m },
            new { Symbol = "GARAN", Name = "Garanti BBVA", Exchange = "BIST", Currency = "TRY", Price = 78.90m },
            new { Symbol = "ISCTR", Name = "İş Bankası", Exchange = "BIST", Currency = "TRY", Price = 12.45m },

            // NASDAQ Stocks
            new { Symbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ", Currency = "USD", Price = 195.75m },
            new { Symbol = "MSFT", Name = "Microsoft Corporation", Exchange = "NASDAQ", Currency = "USD", Price = 420.50m },
            new { Symbol = "GOOGL", Name = "Alphabet Inc.", Exchange = "NASDAQ", Currency = "USD", Price = 152.30m },
            new { Symbol = "AMZN", Name = "Amazon.com Inc.", Exchange = "NASDAQ", Currency = "USD", Price = 155.85m },
            new { Symbol = "TSLA", Name = "Tesla Inc.", Exchange = "NASDAQ", Currency = "USD", Price = 248.90m },
            new { Symbol = "META", Name = "Meta Platforms Inc.", Exchange = "NASDAQ", Currency = "USD", Price = 485.20m },
            new { Symbol = "NFLX", Name = "Netflix Inc.", Exchange = "NASDAQ", Currency = "USD", Price = 485.75m }
        };

        return stockSymbols.Take(limit).Select((stock, index) => (object)new
        {
            id = Guid.NewGuid().ToString(),
            symbol = stock.Symbol,
            displayName = stock.Name,
            assetClassId = Guid.NewGuid().ToString(),
            assetClassName = "STOCK",
            marketId = Guid.NewGuid().ToString(),
            marketName = $"{stock.Exchange} Market",
            baseCurrency = stock.Symbol,
            quoteCurrency = stock.Currency,
            minTradeAmount = 1.0,
            maxTradeAmount = 1000000.0,
            priceDecimalPlaces = 2,
            quantityDecimalPlaces = 0,
            isActive = true,
            isTracked = index < 7, // Track first 7
            tickSize = stock.Exchange == "BIST" ? 0.01 : 0.01,
            lotSize = stock.Exchange == "BIST" ? 1.0 : 1.0,
            description = $"{stock.Name} stock traded on {stock.Exchange}",
            sector = stock.Exchange == "BIST" ? "Turkey Stocks" : "US Technology",
            industry = stock.Exchange == "BIST" ?
                (stock.Symbol.Contains("BANK") || stock.Symbol.Contains("GARAN") || stock.Symbol.Contains("ISCTR") || stock.Symbol.Contains("AKBNK") ? "Banking" : "Industrial") :
                "Technology"
        }).ToArray();
    }
}

public record CreateSymbolRequest(string Ticker, string? Venue, string? BaseCcy, string? QuoteCcy);
public record PatchSymbolRequest(bool IsTracked);