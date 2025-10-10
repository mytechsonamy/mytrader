using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Controller for market status and trading hours information
/// </summary>
[ApiController]
[Route("api/market-status")]
[Produces("application/json")]
public class MarketStatusController : ControllerBase
{
    private readonly IMarketHoursService _marketHoursService;
    private readonly ILogger<MarketStatusController> _logger;

    public MarketStatusController(
        IMarketHoursService marketHoursService,
        ILogger<MarketStatusController> logger)
    {
        _marketHoursService = marketHoursService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current market status for a specific exchange
    /// </summary>
    /// <param name="exchange">The exchange to check (BIST, NASDAQ, NYSE, CRYPTO)</param>
    /// <returns>Market status information</returns>
    /// <response code="200">Returns the market status</response>
    /// <response code="400">Invalid exchange parameter</response>
    [HttpGet("{exchange}")]
    [ProducesResponseType(typeof(MarketHoursInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<MarketHoursInfo> GetMarketStatus([FromRoute] string exchange)
    {
        try
        {
            if (!Enum.TryParse<Exchange>(exchange, true, out var exchangeEnum))
            {
                return BadRequest(new
                {
                    error = "Invalid exchange",
                    message = $"Exchange '{exchange}' is not supported. Valid values: BIST, NASDAQ, NYSE, CRYPTO"
                });
            }

            if (exchangeEnum == Exchange.UNKNOWN)
            {
                return BadRequest(new
                {
                    error = "Invalid exchange",
                    message = "UNKNOWN is not a valid exchange to query"
                });
            }

            var status = _marketHoursService.GetMarketStatus(exchangeEnum);

            _logger.LogDebug("Market status requested for {Exchange}: {State}",
                exchange, status.State);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market status for {Exchange}", exchange);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to retrieve market status"
            });
        }
    }

    /// <summary>
    /// Gets the current market status for all supported exchanges
    /// </summary>
    /// <returns>Dictionary of exchange to market status</returns>
    /// <response code="200">Returns all market statuses</response>
    [HttpGet("all")]
    [ProducesResponseType(typeof(Dictionary<Exchange, MarketHoursInfo>), StatusCodes.Status200OK)]
    public ActionResult<Dictionary<Exchange, MarketHoursInfo>> GetAllMarketStatuses()
    {
        try
        {
            var statuses = _marketHoursService.GetAllMarketStatuses();

            _logger.LogDebug("All market statuses requested, returning {Count} exchanges",
                statuses.Count);

            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all market statuses");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to retrieve market statuses"
            });
        }
    }

    /// <summary>
    /// Checks if a specific exchange is currently open for trading
    /// </summary>
    /// <param name="exchange">The exchange to check</param>
    /// <returns>Boolean indicating if market is open</returns>
    /// <response code="200">Returns true if market is open, false otherwise</response>
    /// <response code="400">Invalid exchange parameter</response>
    [HttpGet("{exchange}/is-open")]
    [ProducesResponseType(typeof(MarketOpenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<MarketOpenResponse> IsMarketOpen([FromRoute] string exchange)
    {
        try
        {
            if (!Enum.TryParse<Exchange>(exchange, true, out var exchangeEnum))
            {
                return BadRequest(new
                {
                    error = "Invalid exchange",
                    message = $"Exchange '{exchange}' is not supported"
                });
            }

            if (exchangeEnum == Exchange.UNKNOWN)
            {
                return BadRequest(new
                {
                    error = "Invalid exchange",
                    message = "UNKNOWN is not a valid exchange to query"
                });
            }

            var isOpen = _marketHoursService.IsMarketOpen(exchangeEnum);
            var status = _marketHoursService.GetMarketStatus(exchangeEnum);

            return Ok(new MarketOpenResponse
            {
                Exchange = exchangeEnum,
                IsOpen = isOpen,
                State = status.State,
                CheckedAt = DateTime.UtcNow,
                NextOpenTime = status.NextOpenTime,
                NextCloseTime = status.NextCloseTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if market is open for {Exchange}", exchange);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to check market status"
            });
        }
    }

    /// <summary>
    /// Gets the next opening time for a specific exchange
    /// </summary>
    /// <param name="exchange">The exchange to check</param>
    /// <returns>Next opening time in UTC, or null for 24/7 markets</returns>
    /// <response code="200">Returns the next opening time</response>
    /// <response code="400">Invalid exchange parameter</response>
    [HttpGet("{exchange}/next-open")]
    [ProducesResponseType(typeof(NextTimeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<NextTimeResponse> GetNextOpenTime([FromRoute] string exchange)
    {
        try
        {
            if (!Enum.TryParse<Exchange>(exchange, true, out var exchangeEnum))
            {
                return BadRequest(new
                {
                    error = "Invalid exchange",
                    message = $"Exchange '{exchange}' is not supported"
                });
            }

            var nextOpenTime = _marketHoursService.GetNextOpenTime(exchangeEnum);

            return Ok(new NextTimeResponse
            {
                Exchange = exchangeEnum,
                Time = nextOpenTime,
                Type = "open",
                Is24x7 = nextOpenTime == null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next open time for {Exchange}", exchange);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to get next open time"
            });
        }
    }

    /// <summary>
    /// Gets the next closing time for a specific exchange
    /// </summary>
    /// <param name="exchange">The exchange to check</param>
    /// <returns>Next closing time in UTC, or null for 24/7 markets</returns>
    /// <response code="200">Returns the next closing time</response>
    /// <response code="400">Invalid exchange parameter</response>
    [HttpGet("{exchange}/next-close")]
    [ProducesResponseType(typeof(NextTimeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<NextTimeResponse> GetNextCloseTime([FromRoute] string exchange)
    {
        try
        {
            if (!Enum.TryParse<Exchange>(exchange, true, out var exchangeEnum))
            {
                return BadRequest(new
                {
                    error = "Invalid exchange",
                    message = $"Exchange '{exchange}' is not supported"
                });
            }

            var nextCloseTime = _marketHoursService.GetNextCloseTime(exchangeEnum);

            return Ok(new NextTimeResponse
            {
                Exchange = exchangeEnum,
                Time = nextCloseTime,
                Type = "close",
                Is24x7 = nextCloseTime == null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next close time for {Exchange}", exchange);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to get next close time"
            });
        }
    }

    /// <summary>
    /// Determines the exchange for a given symbol
    /// </summary>
    /// <param name="symbol">The trading symbol</param>
    /// <returns>The exchange for the symbol</returns>
    /// <response code="200">Returns the exchange</response>
    /// <response code="400">Invalid or empty symbol</response>
    [HttpGet("symbol/{symbol}/exchange")]
    [ProducesResponseType(typeof(SymbolExchangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SymbolExchangeResponse> GetExchangeForSymbol([FromRoute] string symbol)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new
                {
                    error = "Invalid symbol",
                    message = "Symbol cannot be empty"
                });
            }

            var exchange = _marketHoursService.GetExchangeForSymbol(symbol);

            return Ok(new SymbolExchangeResponse
            {
                Symbol = symbol,
                Exchange = exchange
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining exchange for symbol {Symbol}", symbol);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to determine exchange"
            });
        }
    }

    /// <summary>
    /// Checks if a specific date is a holiday for the given exchange
    /// </summary>
    /// <param name="exchange">The exchange to check</param>
    /// <param name="date">The date to check (format: YYYY-MM-DD)</param>
    /// <returns>Boolean indicating if the date is a holiday</returns>
    /// <response code="200">Returns true if holiday, false otherwise</response>
    /// <response code="400">Invalid exchange or date parameter</response>
    [HttpGet("{exchange}/is-holiday")]
    [ProducesResponseType(typeof(HolidayCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<HolidayCheckResponse> IsHoliday(
        [FromRoute] string exchange,
        [FromQuery] string date)
    {
        try
        {
            if (!Enum.TryParse<Exchange>(exchange, true, out var exchangeEnum))
            {
                return BadRequest(new
                {
                    error = "Invalid exchange",
                    message = $"Exchange '{exchange}' is not supported"
                });
            }

            if (!DateTime.TryParse(date, out var dateToCheck))
            {
                return BadRequest(new
                {
                    error = "Invalid date",
                    message = "Date must be in format YYYY-MM-DD"
                });
            }

            var isHoliday = _marketHoursService.IsHoliday(exchangeEnum, dateToCheck);

            return Ok(new HolidayCheckResponse
            {
                Exchange = exchangeEnum,
                Date = dateToCheck.Date,
                IsHoliday = isHoliday
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking holiday for {Exchange} on {Date}",
                exchange, date);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to check holiday status"
            });
        }
    }
}

/// <summary>
/// Response for market open status check
/// </summary>
public class MarketOpenResponse
{
    public Exchange Exchange { get; set; }
    public bool IsOpen { get; set; }
    public MyTrader.Core.Enums.MarketStatus State { get; set; }
    public DateTime CheckedAt { get; set; }
    public DateTime? NextOpenTime { get; set; }
    public DateTime? NextCloseTime { get; set; }
}

/// <summary>
/// Response for next open/close time queries
/// </summary>
public class NextTimeResponse
{
    public Exchange Exchange { get; set; }
    public DateTime? Time { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool Is24x7 { get; set; }
}

/// <summary>
/// Response for symbol-to-exchange lookup
/// </summary>
public class SymbolExchangeResponse
{
    public string Symbol { get; set; } = string.Empty;
    public Exchange Exchange { get; set; }
}

/// <summary>
/// Response for holiday check
/// </summary>
public class HolidayCheckResponse
{
    public Exchange Exchange { get; set; }
    public DateTime Date { get; set; }
    public bool IsHoliday { get; set; }
}