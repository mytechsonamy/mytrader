using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs.Signals;
using MyTrader.Services.Signals;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api")]
[Tags("Signals")]
public class SignalsController : ControllerBase
{
    private readonly ISignalService _signalService;

    public SignalsController(ISignalService signalService)
    {
        _signalService = signalService;
    }

    [HttpGet("signals")]
    public async Task<ActionResult<SignalsListResponse>> GetSignals([FromQuery] int limit = 50, [FromQuery] int cursor = 0)
    {
        try
        {
            var result = await _signalService.GetSignalsAsync(limit, cursor);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                error = new 
                { 
                    code = "SIGNALS_FETCH_FAILED",
                    message = "Failed to fetch signal history"
                }
            });
        }
    }

    [HttpGet("market-data")]
    public async Task<ActionResult<Services.Signals.MarketDataResponse>> GetMarketData()
    {
        try
        {
            var result = await _signalService.GetCurrentMarketDataAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                error = new 
                { 
                    code = "MARKET_DATA_FAILED",
                    message = "Failed to fetch market data"
                }
            });
        }
    }
}