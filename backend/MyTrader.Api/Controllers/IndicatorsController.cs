using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs.Indicators;
using MyTrader.Core.Models.Indicators;
using MyTrader.Services.Market;
using MyTrader.Services.Signals;
using MyTrader.Services.Trading;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IndicatorsController : ControllerBase
{
    private readonly IIndicatorService _indicatorService;
    private readonly ISignalGenerationEngine _signalEngine;
    private readonly ISymbolService _symbolService;
    private readonly ILogger<IndicatorsController> _logger;

    public IndicatorsController(
        IIndicatorService indicatorService,
        ISignalGenerationEngine signalEngine,
        ISymbolService symbolService,
        ILogger<IndicatorsController> logger)
    {
        _indicatorService = indicatorService;
        _signalEngine = signalEngine;
        _symbolService = symbolService;
        _logger = logger;
    }

    /// <summary>
    /// Get current indicators for a symbol
    /// </summary>
    [HttpGet("{symbol}/current")]
    public async Task<ActionResult<IndicatorResponse>> GetCurrentIndicators(
        string symbol,
        [FromQuery] string timeframe = "1h",
        [FromQuery] bool includeExtended = false)
    {
        try
        {
            var symbolEntity = await _symbolService.GetSymbolAsync(symbol);
            if (symbolEntity == null)
            {
                return NotFound($"Symbol {symbol} not found");
            }

            // Mock current indicators - in real implementation would fetch recent candles
            var mockIndicators = CreateMockIndicators(symbolEntity.Id, timeframe, includeExtended);
            
            return Ok(mockIndicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicators for {Symbol}", symbol);
            return StatusCode(500, "Error retrieving indicators");
        }
    }

    /// <summary>
    /// Get trading signals for a symbol
    /// </summary>
    [HttpGet("{symbol}/signals")]
    public async Task<ActionResult<SignalResponse>> GetTradingSignals(
        string symbol,
        [FromQuery] string timeframe = "1h",
        [FromQuery] int limit = 10)
    {
        try
        {
            var symbolEntity = await _symbolService.GetSymbolAsync(symbol);
            if (symbolEntity == null)
            {
                return NotFound($"Symbol {symbol} not found");
            }

            // Mock signals - in real implementation would use actual candle data
            var settings = new SignalGenerationSettings();
            var mockCandles = GenerateMockCandles(10);
            
            var signals = await _signalEngine.GenerateSignalsAsync(symbolEntity.Id, timeframe, mockCandles, settings);
            
            var response = new SignalResponse
            {
                Symbol = symbol,
                Timeframe = timeframe,
                Signals = signals.Take(limit).Select(s => new SignalInfo
                {
                    Type = s.SignalType.ToString(),
                    Source = s.Source.ToString(),
                    Confidence = s.Confidence,
                    Strength = s.Strength,
                    Price = s.Price,
                    Reason = s.Reason,
                    GeneratedAt = s.GeneratedAt,
                    ExpiresAt = s.ExpiresAt
                }).ToList(),
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signals for {Symbol}", symbol);
            return StatusCode(500, "Error retrieving signals");
        }
    }

    /// <summary>
    /// Get consensus signal for a symbol
    /// </summary>
    [HttpGet("{symbol}/consensus")]
    public async Task<ActionResult<ConsensusResponse>> GetConsensusSignal(
        string symbol,
        [FromQuery] string timeframe = "1h")
    {
        try
        {
            var symbolEntity = await _symbolService.GetSymbolAsync(symbol);
            if (symbolEntity == null)
            {
                return NotFound($"Symbol {symbol} not found");
            }

            var settings = new SignalGenerationSettings();
            var aggregationSettings = new SignalAggregationSettings();
            var mockCandles = GenerateMockCandles(10);
            
            var signals = await _signalEngine.GenerateSignalsAsync(symbolEntity.Id, timeframe, mockCandles, settings);
            var consensus = await _signalEngine.AggregateSignalsAsync(signals, aggregationSettings);
            
            var response = new ConsensusResponse
            {
                Symbol = symbol,
                Timeframe = timeframe,
                ConsensusType = consensus.ConsensusType.ToString(),
                Confidence = consensus.ConsensusConfidence,
                Strength = consensus.ConsensusStrength,
                TotalSignals = consensus.TotalSignals,
                BullishSignals = consensus.BullishSignals,
                BearishSignals = consensus.BearishSignals,
                Reason = consensus.ConsensusReason,
                GeneratedAt = consensus.GeneratedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consensus for {Symbol}", symbol);
            return StatusCode(500, "Error retrieving consensus signal");
        }
    }

    /// <summary>
    /// Get signal performance statistics
    /// </summary>
    [HttpGet("{symbol}/performance")]
    public async Task<ActionResult<PerformanceResponse>> GetSignalPerformance(
        string symbol,
        [FromQuery] string timeframe = "1h",
        [FromQuery] int days = 30)
    {
        try
        {
            var symbolEntity = await _symbolService.GetSymbolAsync(symbol);
            if (symbolEntity == null)
            {
                return NotFound($"Symbol {symbol} not found");
            }

            var fromDate = DateTime.UtcNow.AddDays(-days);
            var stats = await _signalEngine.GetSignalPerformanceAsync(symbolEntity.Id, timeframe, fromDate);
            
            var response = new PerformanceResponse
            {
                Symbol = symbol,
                Timeframe = timeframe,
                Period = $"{days} days",
                TotalSignals = stats.TotalSignals,
                ProfitableSignals = stats.ProfitableSignals,
                WinRate = stats.WinRate,
                AverageReturn = stats.AverageReturn,
                MaxReturn = stats.MaxReturn,
                MinReturn = stats.MinReturn,
                AverageHoldingTime = stats.AverageHoldingTime,
                CalculatedAt = stats.CalculatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance for {Symbol}", symbol);
            return StatusCode(500, "Error retrieving performance data");
        }
    }

    /// <summary>
    /// Get available symbols for indicator analysis
    /// </summary>
    [HttpGet("symbols")]
    public async Task<ActionResult<List<SymbolInfo>>> GetAvailableSymbols()
    {
        try
        {
            var symbols = await _symbolService.GetActiveSymbolsAsync();
            
            var response = symbols.Select(s => new SymbolInfo
            {
                Id = s.Id,
                Ticker = s.Ticker,
                FullName = s.FullName ?? s.Ticker,
                Venue = s.Venue,
                AssetClass = s.AssetClass
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available symbols");
            return StatusCode(500, "Error retrieving symbols");
        }
    }

    /// <summary>
    /// Get indicator calculation settings
    /// </summary>
    [HttpGet("settings")]
    public ActionResult<IndicatorSettingsResponse> GetIndicatorSettings()
    {
        var settings = new IndicatorSettings();
        
        var response = new IndicatorSettingsResponse
        {
            RSI = new RSISettingsDto
            {
                Period = settings.RSI.Period,
                OverboughtLevel = 70,
                OversoldLevel = 30
            },
            MACD = new MACDSettingsDto
            {
                FastPeriod = settings.MACD.FastPeriod,
                SlowPeriod = settings.MACD.SlowPeriod,
                SignalPeriod = settings.MACD.SignalPeriod
            },
            BollingerBands = new BollingerBandsSettingsDto
            {
                Period = settings.BollingerBands.Period,
                Multiplier = settings.BollingerBands.Multiplier
            },
            EMAPeriods = settings.EMAPeriods,
            SMAPeriods = settings.SMAPeriods,
            ATRPeriod = settings.ATRPeriod,
            SupportResistanceLookback = settings.SupportResistanceLookback
        };

        return Ok(response);
    }

    /// <summary>
    /// Update user's indicator preferences
    /// </summary>
    [HttpPost("preferences")]
    public async Task<ActionResult> UpdateIndicatorPreferences([FromBody] IndicatorPreferencesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // In a real implementation, save user preferences to database
            _logger.LogInformation("Updated indicator preferences for user {UserId}", userId);
            
            return Ok(new { message = "Preferences updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating indicator preferences");
            return StatusCode(500, "Error updating preferences");
        }
    }

    // Private helper methods
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }

    private IndicatorResponse CreateMockIndicators(Guid symbolId, string timeframe, bool includeExtended)
    {
        var random = new Random();
        
        var response = new IndicatorResponse
        {
            SymbolId = symbolId,
            Timeframe = timeframe,
            Price = (decimal)(45000 + random.NextDouble() * 1000),
            Timestamp = DateTime.UtcNow,
            
            // Basic indicators
            RSI = (decimal)(30 + random.NextDouble() * 40),
            MACD = new MACDDto
            {
                Value = (decimal)(random.NextDouble() * 200 - 100),
                Signal = (decimal)(random.NextDouble() * 200 - 100),
                Histogram = (decimal)(random.NextDouble() * 50 - 25)
            },
            BollingerBands = new BollingerBandsDto
            {
                Upper = (decimal)(46000 + random.NextDouble() * 500),
                Middle = (decimal)(45500 + random.NextDouble() * 200),
                Lower = (decimal)(45000 - random.NextDouble() * 500),
                Position = (decimal)random.NextDouble()
            },
            
            // Moving averages
            EMA9 = (decimal)(45200 + random.NextDouble() * 300),
            EMA21 = (decimal)(45100 + random.NextDouble() * 400),
            EMA50 = (decimal)(45000 + random.NextDouble() * 500),
            SMA20 = (decimal)(45150 + random.NextDouble() * 350),
            SMA50 = (decimal)(45050 + random.NextDouble() * 450),
            
            // Volume
            Volume = (decimal)(random.NextDouble() * 1000000),
            VWAP = (decimal)(45300 + random.NextDouble() * 200),
            
            // Volatility
            ATR = new ATRDto
            {
                Value = (decimal)(random.NextDouble() * 500 + 100),
                Percentage = (decimal)(random.NextDouble() * 2 + 1)
            }
        };
        
        if (includeExtended)
        {
            response.Stochastic = new StochasticDto
            {
                K = (decimal)(random.NextDouble() * 100),
                D = (decimal)(random.NextDouble() * 100)
            };
            
            response.Williams = (decimal)(random.NextDouble() * 100 - 100);
            response.CCI = (decimal)(random.NextDouble() * 400 - 200);
            response.MFI = (decimal)(random.NextDouble() * 100);
            
            response.ADX = new ADXDto
            {
                Value = (decimal)(random.NextDouble() * 50 + 10),
                PlusDI = (decimal)(random.NextDouble() * 40 + 10),
                MinusDI = (decimal)(random.NextDouble() * 40 + 10),
                TrendStrength = (decimal)(random.NextDouble() * 50 + 10)
            };
            
            response.SupportResistance = new SupportResistanceDto
            {
                CurrentSupport = (decimal)(44500 + random.NextDouble() * 300),
                CurrentResistance = (decimal)(45500 + random.NextDouble() * 300),
                SupportLevels = new List<decimal> 
                { 
                    (decimal)(44000 + random.NextDouble() * 200),
                    (decimal)(43500 + random.NextDouble() * 200)
                },
                ResistanceLevels = new List<decimal> 
                { 
                    (decimal)(46000 + random.NextDouble() * 200),
                    (decimal)(46500 + random.NextDouble() * 200)
                }
            };
        }

        return response;
    }

    private List<MyTrader.Core.Models.Candle> GenerateMockCandles(int count)
    {
        var candles = new List<MyTrader.Core.Models.Candle>();
        var random = new Random();
        var basePrice = 45000m;
        var currentTime = DateTime.UtcNow.AddHours(-count);

        for (int i = 0; i < count; i++)
        {
            var variation = (decimal)(random.NextDouble() * 1000 - 500);
            var open = basePrice + variation;
            var high = open + (decimal)(random.NextDouble() * 200);
            var low = open - (decimal)(random.NextDouble() * 200);
            var close = low + (decimal)(random.NextDouble() * (double)(high - low));

            candles.Add(new MyTrader.Core.Models.Candle
            {
                Id = Guid.NewGuid(),
                SymbolId = Guid.NewGuid(),
                Timeframe = "1h",
                Timestamp = currentTime.AddHours(i),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = (decimal)(random.NextDouble() * 1000000)
            });

            basePrice = close; // Use closing price as next base
        }

        return candles;
    }
}