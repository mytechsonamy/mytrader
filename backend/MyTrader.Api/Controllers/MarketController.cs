using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyTrader.Api.Hubs;
using MyTrader.Core.DTOs.Market;
using MyTrader.Services.Market;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/market")]
[Tags("Market Data")]
// Temporarily removing [Authorize] for debugging
// [Authorize]
public class MarketController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;
    private readonly IHubContext<TradingHub> _hubContext;

    public MarketController(IMarketDataService marketDataService, IHubContext<TradingHub> hubContext)
    {
        _marketDataService = marketDataService;
        _hubContext = hubContext;
    }

    [HttpPost("import-daily")]
    public async Task<ActionResult<ImportResponse>> ImportDailyPrices([FromBody] ImportRequest request)
    {
        try
        {
            var result = await _marketDataService.ImportDailyPricesAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { code = "INVALID_REQUEST", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "IMPORT_ERROR", message = "Failed to import prices" });
        }
    }

    [HttpGet("{symbol}")]
    public async Task<ActionResult<MarketDataResponse>> GetMarketData(
        string symbol, 
        [FromQuery] string timeframe = "1d",
        [FromQuery] DateTime? start = null,
        [FromQuery] DateTime? end = null)
    {
        try
        {
            var result = await _marketDataService.GetMarketDataAsync(symbol, timeframe, start, end);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "MARKET_DATA_ERROR", message = "Failed to retrieve market data" });
        }
    }

    [HttpPost("test-signalr")]
    public async Task<ActionResult> TestSignalR()
    {
        // Send test price updates for popular cryptocurrencies
        var testPrices = new[]
        {
            new { symbol = "BTCUSDT", price = 45123.45m, change = 2.3m },
            new { symbol = "ETHUSDT", price = 2845.67m, change = -1.8m },
            new { symbol = "XRPUSDT", price = 0.6234m, change = 4.2m },
            new { symbol = "BNBUSDT", price = 312.89m, change = 1.5m },
            new { symbol = "ADAUSDT", price = 0.4567m, change = -0.9m }
        };

        foreach (var price in testPrices)
        {
            await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", new 
            { 
                symbol = price.symbol, 
                price = price.price, 
                change = price.change, 
                timestamp = DateTime.UtcNow.ToString("O") 
            });
        }

        return Ok(new { message = "Test price updates sent", count = testPrices.Length });
    }
}