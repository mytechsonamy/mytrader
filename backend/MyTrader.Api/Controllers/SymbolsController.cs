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
    private readonly ILogger<SymbolsController> _logger;

    public SymbolsController(
        ISymbolService symbolService,
        ILogger<SymbolsController> logger)
    {
        _symbolService = symbolService;
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
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpGet("enhanced")]
    [AllowAnonymous] // Allow public access for symbol discovery
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult GetEnhancedSymbols([FromQuery] object request)
    {
        _logger.LogInformation("Enhanced symbols endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbols service implementation pending" });
    }

    /// <summary>
    /// Get symbol by ID with full enhanced details
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpGet("enhanced/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult GetEnhancedSymbolById(Guid id)
    {
        _logger.LogInformation("Enhanced symbol by ID endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
    }

    /// <summary>
    /// Search symbols across all asset classes
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult SearchSymbols([FromQuery] object request)
    {
        _logger.LogInformation("Symbol search endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
    }

    /// <summary>
    /// Get symbols by asset class (by GUID)
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpGet("by-asset-class/{assetClassId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult GetSymbolsByAssetClass(Guid assetClassId, [FromQuery] int? limit = null)
    {
        _logger.LogInformation("Symbols by asset class GUID endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
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
    public async Task<ActionResult> GetSymbolsByAssetClassName(
        string assetClassName,
        [FromQuery] int? limit = null)
    {
        try
        {
            _logger.LogInformation("Getting symbols by asset class name: {AssetClassName}, limit: {Limit}",
                assetClassName, limit);

            // Try to get real symbols from database first
            try
            {
                var dbSymbols = await _symbolService.GetSymbolsByAssetClassAsync(assetClassName.ToUpper());
                if (dbSymbols != null && dbSymbols.Count > 0)
                {
                    var limitedSymbols = dbSymbols.Take(limit ?? 50);
                    var result = limitedSymbols.Select(s => new
                    {
                        id = s.Id.ToString(),
                        symbol = s.Ticker,
                        displayName = s.Display ?? s.Ticker,
                        assetClassId = Guid.NewGuid().ToString(),
                        assetClassName = assetClassName.ToUpper(),
                        marketId = Guid.NewGuid().ToString(),
                        marketName = $"{assetClassName} Market",
                        baseCurrency = s.BaseCurrency ?? "",
                        quoteCurrency = s.QuoteCurrency ?? "",
                        minTradeAmount = 0.001,
                        maxTradeAmount = 1000000,
                        priceDecimalPlaces = 2,
                        quantityDecimalPlaces = 8,
                        isActive = s.IsActive,
                        isTracked = s.IsTracked,
                        tickSize = 0.01,
                        lotSize = 0.001,
                        description = $"{s.Display ?? s.Ticker} {assetClassName.ToLower()}",
                        sector = "Financial",
                        industry = assetClassName
                    }).ToArray();

                    return Ok(result);
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "Failed to get symbols from database, falling back to mock data");
            }

            // Fall back to mock data if database lookup fails
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
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpGet("by-market/{marketId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult GetSymbolsByMarket(Guid marketId, [FromQuery] int? limit = null)
    {
        _logger.LogInformation("Symbols by market endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
    }

    /// <summary>
    /// Get enhanced tracked symbols
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpGet("enhanced/tracked")]
    [Authorize]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult GetEnhancedTrackedSymbols()
    {
        _logger.LogInformation("Enhanced tracked symbols endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
    }

    /// <summary>
    /// Get popular symbols
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpGet("popular")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult GetPopularSymbols([FromQuery] int limit = 50)
    {
        _logger.LogInformation("Popular symbols endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
    }

    /// <summary>
    /// Create a new symbol
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpPost("enhanced")]
    [Authorize]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult CreateEnhancedSymbol([FromBody] object request)
    {
        _logger.LogInformation("Create enhanced symbol endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
    }

    /// <summary>
    /// Update symbol tracking status
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpPatch("enhanced/{id:guid}/tracking")]
    [Authorize]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult UpdateSymbolTracking(Guid id, [FromQuery] bool isTracked)
    {
        _logger.LogInformation("Update symbol tracking endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
    }

    /// <summary>
    /// Bulk update symbol tracking status
    /// TEMPORARILY DISABLED - Enhanced symbol service implementation pending
    /// </summary>
    [HttpPatch("enhanced/bulk-tracking")]
    [Authorize]
    [ProducesResponseType(typeof(object), 501)]
    public ActionResult BulkUpdateSymbolTracking([FromBody] object request)
    {
        _logger.LogInformation("Bulk update symbol tracking endpoint called - returning not implemented");
        return StatusCode(501, new { message = "Enhanced symbol service implementation pending" });
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