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
    private readonly ISymbolService _symbolService;

    public MarketController(IMarketDataService marketDataService, IHubContext<TradingHub> hubContext, ISymbolService symbolService)
    {
        _marketDataService = marketDataService;
        _hubContext = hubContext;
        _symbolService = symbolService;
    }

    [HttpPost("import-daily")]
    public async Task<ActionResult<MyTrader.Core.DTOs.Market.ImportResponse>> ImportDailyPrices([FromBody] ImportRequest request)
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
    public async Task<ActionResult<MyTrader.Core.DTOs.Market.MarketDataResponse>> GetMarketData(
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
        // Get tracked symbols and send test price updates
        var trackedSymbols = await _symbolService.GetTrackedAsync("BINANCE");
        var random = new Random();
        
        var testPrices = trackedSymbols.Take(5).Select(symbol => new {
            symbol = symbol.Ticker,
            price = Math.Round((decimal)(random.NextDouble() * 50000 + 1000), 2),
            change = Math.Round((decimal)(random.NextDouble() * 10 - 5), 2)
        }).ToArray();

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