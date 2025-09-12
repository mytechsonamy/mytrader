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
[Route("api/[controller]")]
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
            // Get tracked symbols from database instead of hardcoded list
            var trackedSymbols = await _symbolService.GetTrackedAsync("BINANCE");
            var symbols = trackedSymbols.Select(s => s.Ticker).ToArray();
            
            if (!symbols.Any())
            {
                // Fallback to default symbols if none are tracked
                symbols = new[] { "BTCUSDT", "ETHUSDT", "XRPUSDT", "BNBUSDT", "ADAUSDT" };
            }
            var symbolsParam = string.Join(",", symbols);
            
            var response = await _httpClient.GetStringAsync($"https://api.binance.com/api/v3/ticker/24hr?symbols=[{string.Join(",", symbols.Select(s => $"\"{s}\""))}]");
            var binanceData = JsonSerializer.Deserialize<JsonElement[]>(response) ?? Array.Empty<JsonElement>();

            var prices = new Dictionary<string, object>();
            var marketDataEntries = new List<MarketData>();
            
            foreach (var item in binanceData)
            {
                var symbol = item.GetProperty("symbol").GetString() ?? "UNKNOWN";
                var price = decimal.Parse(item.GetProperty("lastPrice").GetString() ?? "0");
                var change = decimal.Parse(item.GetProperty("priceChangePercent").GetString() ?? "0");
                var high = decimal.Parse(item.GetProperty("highPrice").GetString() ?? "0");
                var low = decimal.Parse(item.GetProperty("lowPrice").GetString() ?? "0");
                var open = decimal.Parse(item.GetProperty("openPrice").GetString() ?? "0");
                var volume = decimal.Parse(item.GetProperty("volume").GetString() ?? "0");
                
                if (symbol != null)
                {
                    var cleanSymbol = symbol.Replace("USDT", "");
                    var timestamp = DateTime.UtcNow;
                    
                    prices[cleanSymbol] = new
                    {
                        price = price,
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

            return Ok(new { symbols = prices, timestamp = DateTime.UtcNow, traceId, durationMs = sw.ElapsedMilliseconds });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[PricesController] LIVE request error TraceId={TraceId} DurationMs={DurationMs} RemoteIp={RemoteIp}", traceId, sw.ElapsedMilliseconds, remoteIp);
            return StatusCode(500, new { error = "Failed to fetch live prices", message = ex.Message });
        }
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetSymbolPrice(string symbol)
    {
        try
        {
            var fullSymbol = symbol.ToUpper().Contains("USDT") ? symbol.ToUpper() : $"{symbol.ToUpper()}USDT";
            var response = await _httpClient.GetStringAsync($"https://api.binance.com/api/v3/ticker/24hr?symbol={fullSymbol}");
            var binanceData = JsonSerializer.Deserialize<JsonElement>(response);

            var price = decimal.Parse(binanceData.GetProperty("lastPrice").GetString() ?? "0");
            var change = decimal.Parse(binanceData.GetProperty("priceChangePercent").GetString() ?? "0");

            var result = new
            {
                symbol = symbol.ToUpper(),
                price = price,
                change = change,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch price for {Symbol}", symbol);
            return StatusCode(500, new { error = $"Failed to fetch price for {symbol}", message = ex.Message });
        }
    }
}
