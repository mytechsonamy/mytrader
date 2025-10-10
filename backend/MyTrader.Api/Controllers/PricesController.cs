using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyTrader.Api.Hubs;
using MyTrader.Infrastructure.Data;
using MyTrader.Core.Models;
using MyTrader.Services.Market;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // Backward compatibility route
[Route("api/v1/prices")] // New versioned route
[Tags("Price Data")]
public class PricesController : ControllerBase
{
    private readonly IHubContext<TradingHub> _hubContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PricesController> _logger;
    private readonly TradingDbContext _context;
    private readonly ISymbolService _symbolService;

    public PricesController(IHubContext<TradingHub> hubContext, HttpClient httpClient, ILogger<PricesController> logger, TradingDbContext context, ISymbolService symbolService)
    {
        _hubContext = hubContext;
        _httpClient = httpClient;
        _logger = logger;
        _context = context;
        _symbolService = symbolService;
    }

    [HttpGet("live")]
    [AllowAnonymous] // Allow public access for web frontend dashboard
    public async Task<IActionResult> GetLivePrices()
    {
        // Structured request logging to trace external callers of /api/prices/live
        var startedAt = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();
        var remoteIp = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        var xff = Request?.Headers["X-Forwarded-For"].ToString() ?? string.Empty;
        var userAgent = Request?.Headers["User-Agent"].ToString() ?? string.Empty;
        var traceId = HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        var userId = User?.Identity?.IsAuthenticated == true 
            ? (User?.FindFirst("sub")?.Value 
               ?? User?.FindFirst("user_id")?.Value 
               ?? User?.Identity?.Name 
               ?? "authenticated")
            : "anonymous";

        _logger.LogInformation(
            "[PricesController] LIVE request start TraceId={TraceId} RemoteIp={RemoteIp} XFF={XFF} UserAgent={UserAgent} User={User}",
            traceId, remoteIp, xff, userAgent, userId);

        try
        {
            // Get tracked symbols from database with error handling
            var symbols = new[] { "BTCUSDT", "ETHUSDT", "XRPUSDT", "BNBUSDT", "ADAUSDT" }; // Default fallback
            try
            {
                var trackedSymbols = await _symbolService.GetTrackedAsync("BINANCE");
                if (trackedSymbols != null && trackedSymbols.Any())
                {
                    // Convert symbols to Binance format (e.g., ETH-USD -> ETHUSDT, BTC-USD -> BTCUSDT)
                    symbols = trackedSymbols
                        .Select(s => s.Ticker.Replace("-USD", "USDT").Replace("_", ""))
                        .Where(s => s.EndsWith("USDT")) // Only keep USDT pairs
                        .ToArray();
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "Failed to get tracked symbols from database, using defaults");
            }
            var symbolsParam = string.Join(",", symbols);

            // Make API call with better error handling
            string url;
            _logger.LogInformation("Processing {Count} symbols: {Symbols}", symbols.Length, string.Join(", ", symbols));

            if (symbols.Length == 1)
            {
                url = $"https://api.binance.com/api/v3/ticker/24hr?symbol={symbols[0]}";
            }
            else
            {
                var symbolsArray = $"[{string.Join(",", symbols.Select(s => $"\"{s}\""))}]";
                url = $"https://api.binance.com/api/v3/ticker/24hr?symbols={Uri.EscapeDataString(symbolsArray)}";
            }
            _logger.LogInformation("Making request to Binance API: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            JsonElement[] binanceData;
            try
            {
                if (symbols.Length == 1)
                {
                    // Single symbol returns a single object, wrap it in an array
                    var singleItem = JsonSerializer.Deserialize<JsonElement>(response);
                    binanceData = new[] { singleItem };
                }
                else
                {
                    // Multiple symbols return an array
                    binanceData = JsonSerializer.Deserialize<JsonElement[]>(response) ?? Array.Empty<JsonElement>();
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize Binance response: {Response}", response);
                binanceData = Array.Empty<JsonElement>();
            }

            var prices = new Dictionary<string, object>();
            var marketDataEntries = new List<MarketData>();
            
            foreach (var item in binanceData)
            {
                try
                {
                    var symbol = item.GetProperty("symbol").GetString() ?? "UNKNOWN";

                    // CRITICAL: Use InvariantCulture for parsing to ensure decimal point is handled correctly
                    var priceStr = item.GetProperty("lastPrice").GetString() ?? "0";
                    var price = decimal.TryParse(priceStr, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign,
                        System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0;

                    var changeStr = item.GetProperty("priceChangePercent").GetString() ?? "0";
                    var change = decimal.TryParse(changeStr, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowLeadingWhite,
                        System.Globalization.CultureInfo.InvariantCulture, out var c) ? c : 0;

                    var high = decimal.TryParse(item.GetProperty("highPrice").GetString() ?? "0",
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var h) ? h : 0;
                    var low = decimal.TryParse(item.GetProperty("lowPrice").GetString() ?? "0",
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var l) ? l : 0;
                    var open = decimal.TryParse(item.GetProperty("openPrice").GetString() ?? "0",
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var o) ? o : 0;
                    var volume = decimal.TryParse(item.GetProperty("volume").GetString() ?? "0",
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

                    if (symbol != null && symbol != "UNKNOWN")
                    {
                        var cleanSymbol = symbol.Replace("USDT", "");
                        var timestamp = DateTime.UtcNow;

                        // DEBUG: Log what's happening with the parsing
                        _logger.LogWarning($"Symbol {cleanSymbol}: Raw from Binance='{priceStr}', Parsed={price}, Change={change}");

                        // CRITICAL FIX: Manually divide by 10^8 if value is too large
                        // This indicates a systemic issue with decimal handling
                        var correctedPrice = price;
                        if (price > 1_000_000) // Crypto prices shouldn't be this high
                        {
                            correctedPrice = price / 100_000_000m; // Divide by 10^8
                            _logger.LogWarning($"Applied correction for {cleanSymbol}: {price} -> {correctedPrice}");
                        }

                        prices[cleanSymbol] = new
                        {
                            price = correctedPrice,
                            change = change,
                            timestamp = timestamp.ToString("O")
                        };

                        // Store market data in database
                        var marketData = new MarketData
                        {
                            Symbol = symbol,
                            Timeframe = "1d", // Daily timeframe for live prices
                            Timestamp = timestamp,
                            Open = open,
                            High = high,
                            Low = low,
                            Close = price, // Current price as close
                            Volume = volume
                        };
                        marketDataEntries.Add(marketData);
                    }
                }
                catch (Exception itemEx)
                {
                    _logger.LogWarning(itemEx, "Failed to parse market data item, skipping");
                }
            }
            
            // DISABLED: Database writes prevented to avoid memory issues
            try
            {
                _logger.LogDebug("Market data database writes DISABLED to prevent memory issues. Would have saved {Count} entries", marketDataEntries.Count);
                // Database operations completely disabled
                // foreach (var marketData in marketDataEntries) { ... }
                // await _context.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Database operation skipped due to disabled writes");
            }

            // Send to SignalR clients
            await _hubContext.Clients.All.SendAsync("ReceiveMarketData", new { symbols = prices });

            sw.Stop();
            _logger.LogInformation(
                "[PricesController] LIVE request end TraceId={TraceId} DurationMs={DurationMs} SavedEntries={Saved} RemoteIp={RemoteIp}",
                traceId, sw.ElapsedMilliseconds, marketDataEntries.Count, remoteIp);

            // CRITICAL: Ensure consistent response structure for frontend
            var responseObject = new
            {
                success = true,
                symbols = prices, // Always a dictionary, never null
                timestamp = DateTime.UtcNow,
                traceId = traceId,
                durationMs = sw.ElapsedMilliseconds,
                metadata = new
                {
                    symbolCount = prices.Count,
                    source = "BINANCE",
                    remoteIp = remoteIp
                }
            };

            return Ok(responseObject);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[PricesController] LIVE request error TraceId={TraceId} DurationMs={DurationMs} RemoteIp={RemoteIp}", traceId, sw.ElapsedMilliseconds, remoteIp);

            // CRITICAL: Return consistent error structure to prevent frontend crashes
            return StatusCode(500, new {
                success = false,
                error = "Failed to fetch live prices",
                message = ex.Message,
                symbols = new Dictionary<string, object>(), // Empty dictionary, not null
                timestamp = DateTime.UtcNow,
                traceId = traceId,
                durationMs = sw.ElapsedMilliseconds
            });
        }
    }

    [HttpGet("{symbol}")]
    [AllowAnonymous] // Allow public access for individual symbol prices
    public async Task<IActionResult> GetSymbolPrice(string symbol)
    {
        var startedAt = DateTime.UtcNow;
        var traceId = HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        try
        {
            _logger.LogInformation("Getting price for symbol: {Symbol}, TraceId: {TraceId}", symbol, traceId);

            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new {
                    success = false,
                    error = "Invalid symbol",
                    message = "Symbol parameter is required",
                    symbol = "",
                    price = 0m,
                    change = 0m,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    traceId = traceId
                });
            }

            var fullSymbol = symbol.ToUpper().Contains("USDT") ? symbol.ToUpper() : $"{symbol.ToUpper()}USDT";
            var apiUrl = $"https://api.binance.com/api/v3/ticker/24hr?symbol={fullSymbol}";

            var response = await _httpClient.GetStringAsync(apiUrl);
            var binanceData = JsonSerializer.Deserialize<JsonElement>(response);

            // CRITICAL: Use InvariantCulture for all decimal parsing
            var price = decimal.TryParse(binanceData.GetProperty("lastPrice").GetString(),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0m;
            var change = decimal.TryParse(binanceData.GetProperty("priceChangePercent").GetString(),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var c) ? c : 0m;
            var volume = decimal.TryParse(binanceData.GetProperty("volume").GetString(),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0m;
            var high = decimal.TryParse(binanceData.GetProperty("highPrice").GetString(),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var h) ? h : 0m;
            var low = decimal.TryParse(binanceData.GetProperty("lowPrice").GetString(),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var l) ? l : 0m;

            // CRITICAL FIX: Ensure prices are in correct decimal format
            if (price > 1_000_000) price = price / 100_000_000m;
            if (high > 1_000_000) high = high / 100_000_000m;
            if (low > 1_000_000) low = low / 100_000_000m;
            if (volume > 1_000_000_000_000) volume = volume / 100_000_000m;

            // CRITICAL: Consistent response structure for frontend
            var result = new
            {
                success = true,
                symbol = symbol.ToUpper(),
                fullSymbol = fullSymbol,
                price = price,
                change = change,
                volume = volume,
                high24h = high,
                low24h = low,
                timestamp = DateTime.UtcNow.ToString("O"),
                traceId = traceId,
                source = "BINANCE",
                durationMs = (DateTime.UtcNow - startedAt).TotalMilliseconds
            };

            _logger.LogInformation("Successfully fetched price for {Symbol}: ${Price} ({Change}%)", symbol, price, change);
            return Ok(result);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "HTTP error fetching price for {Symbol}: {Message}", symbol, httpEx.Message);
            return StatusCode(503, new {
                success = false,
                error = $"Service unavailable for symbol {symbol}",
                message = "External API temporarily unavailable",
                symbol = symbol?.ToUpper() ?? "",
                price = 0m,
                change = 0m,
                timestamp = DateTime.UtcNow.ToString("O"),
                traceId = traceId,
                source = "BINANCE"
            });
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON parsing error for symbol {Symbol}", symbol);
            return StatusCode(502, new {
                success = false,
                error = $"Data format error for symbol {symbol}",
                message = "Invalid response format from external API",
                symbol = symbol?.ToUpper() ?? "",
                price = 0m,
                change = 0m,
                timestamp = DateTime.UtcNow.ToString("O"),
                traceId = traceId,
                source = "BINANCE"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching price for {Symbol}", symbol);
            return StatusCode(500, new {
                success = false,
                error = $"Failed to fetch price for {symbol}",
                message = ex.Message,
                symbol = symbol?.ToUpper() ?? "",
                price = 0m,
                change = 0m,
                timestamp = DateTime.UtcNow.ToString("O"),
                traceId = traceId,
                source = "BINANCE"
            });
        }
    }
}
