using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Trading;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/strategies")]
[Tags("Trading Strategies")]
[Authorize]
public class StrategiesController : ControllerBase
{
    private readonly ITradingStrategyService _tradingStrategyService;

    public StrategiesController(ITradingStrategyService tradingStrategyService)
    {
        _tradingStrategyService = tradingStrategyService;
    }

    [HttpGet("{symbol}/signals")]
    public async Task<ActionResult> GetSignals(string symbol, [FromQuery] int limit = 100)
    {
        try
        {
            var signals = await _tradingStrategyService.GetSignalsAsync(symbol, limit);
            return Ok(new
            {
                symbol,
                signals = signals.Select(s => new
                {
                    id = s.Id,
                    signal = s.SignalType,
                    price = s.Price,
                    rsi = s.Rsi,
                    macd = s.Macd,
                    bb_upper = s.BollingerBandUpper,
                    bb_lower = s.BollingerBandLower,
                    bb_position = s.BollingerPosition,
                    timestamp = s.Timestamp
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve signals" });
        }
    }

    [HttpPost("{symbol}/analyze")]
    public async Task<ActionResult> AnalyzeSymbol(string symbol, [FromBody] AnalyzeRequest request)
    {
        try
        {
            // In a real implementation, you would fetch market data from database
            // For now, return a mock response
            return Ok(new
            {
                symbol,
                signal = "NEUTRAL",
                timestamp = DateTime.UtcNow,
                message = "Analysis completed - this is a demo implementation"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to analyze symbol" });
        }
    }
}

public class AnalyzeRequest
{
    public int Period { get; set; } = 100;
    public StrategyParameters? Parameters { get; set; }
}