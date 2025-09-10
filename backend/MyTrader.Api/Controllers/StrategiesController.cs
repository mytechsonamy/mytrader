using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Trading;
using MyTrader.Services.Gamification;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/strategies")]
[Tags("Trading Strategies")]
[Authorize]
public class StrategiesController : ControllerBase
{
    private readonly ITradingStrategyService _tradingStrategyService;
    private readonly TradingDbContext _context;
    private readonly IGamificationService _gamificationService;

    public StrategiesController(ITradingStrategyService tradingStrategyService, TradingDbContext context, IGamificationService gamificationService)
    {
        _tradingStrategyService = tradingStrategyService;
        _context = context;
        _gamificationService = gamificationService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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

    [HttpPost("create")]
    public async Task<ActionResult> CreateStrategy([FromBody] CreateStrategyRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            // For now, return a mock success response
            // In production, this would create a UserStrategy record in the database
            return Ok(new 
            { 
                success = true, 
                message = "Strategy created successfully", 
                strategy_id = Guid.NewGuid().ToString() 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to create strategy" });
        }
    }

    [HttpPost("{strategyId}/test")]
    public async Task<ActionResult> TestStrategy(string strategyId, [FromBody] TestStrategyRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            // Mock backtest result
            var random = new Random();
            var totalReturn = Math.Round((decimal)(random.NextDouble() * 40 - 10), 2); // -10% to +30%
            var winRate = Math.Round((decimal)(random.NextDouble() * 40 + 50), 2); // 50% to 90%
            var maxDrawdown = Math.Round((decimal)(random.NextDouble() * 15 + 5), 2); // 5% to 20%
            var sharpeRatio = Math.Round((decimal)(random.NextDouble() * 2 + 0.5), 2);
            var totalTrades = random.Next(50, 200);
            var profitableTrades = (int)(totalTrades * (winRate / 100));
            var startDate = DateTimeOffset.UtcNow.AddDays(-90);
            var endDate = DateTimeOffset.UtcNow;

            // Record performance and award achievements
            if (Guid.TryParse(strategyId, out var strategyGuid))
            {
                await _gamificationService.RecordStrategyPerformanceAsync(
                    strategyGuid, userId, request.Symbol, totalReturn, winRate,
                    maxDrawdown, sharpeRatio, totalTrades, profitableTrades,
                    startDate, endDate
                );
            }

            var backtestResult = new
            {
                strategy_id = strategyId,
                symbol = request.Symbol,
                total_return = totalReturn,
                win_rate = winRate,
                max_drawdown = maxDrawdown,
                total_trades = totalTrades,
                profitable_trades = profitableTrades,
                sharpe_ratio = sharpeRatio,
                start_date = startDate,
                end_date = endDate,
                performance_chart = new object[0], // Empty for now
                trades = new object[0] // Empty for now
            };

            return Ok(backtestResult);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to test strategy" });
        }
    }

    [HttpGet("my-strategies")]
    public async Task<ActionResult> GetUserStrategies()
    {
        try
        {
            var userId = GetUserId();
            
            var strategies = await _context.UserStrategies
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    description = s.Description,
                    is_active = s.IsActive,
                    created_at = s.CreatedAt,
                    updated_at = s.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = strategies });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to load strategies" });
        }
    }

    [HttpPost("{strategyId}/activate")]
    public async Task<ActionResult> ActivateStrategy(string strategyId)
    {
        try
        {
            var userId = GetUserId();
            
            if (Guid.TryParse(strategyId, out var strategyGuid))
            {
                var strategy = await _context.UserStrategies
                    .FirstOrDefaultAsync(s => s.Id == strategyGuid && s.UserId == userId);

                if (strategy != null)
                {
                    strategy.IsActive = true;
                    strategy.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { success = true, message = "Strategy activated successfully" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Strategy not found" });
                }
            }

            return BadRequest(new { success = false, message = "Invalid strategy ID" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to activate strategy" });
        }
    }
}

public class AnalyzeRequest
{
    public int Period { get; set; } = 100;
    public StrategyParameters? Parameters { get; set; }
}

public class CreateStrategyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class TestStrategyRequest
{
    public string Symbol { get; set; } = string.Empty;
}