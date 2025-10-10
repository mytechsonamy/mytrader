using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTrader.Infrastructure.Data;
using MyTrader.Core.Services;
using MyTrader.Services.Gamification;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/strategies")]
[Tags("Trading Strategies")]
// [Authorize] // Temporarily disabled for testing
public class StrategiesController : ControllerBase
{
    private readonly ITradingStrategyService _tradingStrategyService;
    private readonly TradingDbContext _context;
    private readonly IGamificationService _gamificationService;
    private readonly ILogger<StrategiesController> _logger;

    public StrategiesController(ITradingStrategyService tradingStrategyService, TradingDbContext context, IGamificationService gamificationService, ILogger<StrategiesController> logger)
    {
        _tradingStrategyService = tradingStrategyService;
        _context = context;
        _gamificationService = gamificationService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        // For testing without auth, return a fixed test user ID
        var userIdClaim = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Guid.Parse("86afecc9-507f-454b-9a17-59d5ffaf1917"); // Try the second test user ID
        }
        return Guid.Parse(userIdClaim);
    }    // [HttpGet("{symbol}/signals")]
    // public async Task<ActionResult> GetSignals(string symbol, [FromQuery] int limit = 100)
    // {
    //     try
    //     {
    //         var signals = await _tradingStrategyService.GetSignalsAsync(symbol, limit);
    //         return Ok(new
    //         {
    //             symbol,
    //             signals = signals.Select(s => new
    //             {
    //                 id = s.Id,
    //                 signal = s.SignalType,
    //                 price = s.Price,
    //                 rsi = s.Rsi,
    //                 macd = s.Macd,
    //                 bb_upper = s.BollingerBandUpper,
    //                 bb_lower = s.BollingerBandLower,
    //                 bb_position = s.BollingerPosition,
    //                 timestamp = s.Timestamp
    //             })
    //         });
    //     }
    //     catch (Exception ex)
    //     {
    //         return StatusCode(500, new { error = "Failed to retrieve signals" });
    //     }
    // }

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
    // [Authorize] // Temporarily remove for testing
    public async Task<ActionResult> CreateStrategy([FromBody] CreateStrategyRequest request)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("🎯 CreateStrategy: Creating strategy for user: {UserId}", userId);
            _logger.LogInformation("📋 CreateStrategy: Request data - Name: {Name}, Symbol: {Symbol}", request.Name, request.Symbol);
            
            // Create new UserStrategy entity
            var userStrategy = new MyTrader.Core.Models.UserStrategy
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TemplateId = !string.IsNullOrEmpty(request.TemplateId) && Guid.TryParse(request.TemplateId, out var templateId) ? templateId : null,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Parameters = System.Text.Json.JsonDocument.Parse(
                    System.Text.Json.JsonSerializer.Serialize(request.Parameters ?? new Dictionary<string, object>())
                ),
                TargetSymbols = request.Symbol,
                Timeframe = "1h", // Set explicit timeframe
                IsActive = false,
                IsCustom = true,
                IsFavorite = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("UserStrategy object created: {Name}", userStrategy.Name);
            _logger.LogInformation("UserStrategy values - Id: {Id}, UserId: {UserId}", userStrategy.Id, userStrategy.UserId);
            _logger.LogInformation("Parameters: {Parameters}", userStrategy.Parameters?.RootElement);

            // Test with raw SQL to bypass Entity Framework
            var strategyId = Guid.NewGuid();
            var parametersJson = System.Text.Json.JsonSerializer.Serialize(request.Parameters ?? new Dictionary<string, object>());
            
            _logger.LogInformation("About to execute raw SQL with userId: {UserId}", userId);
            
            var sql = @"
                INSERT INTO user_strategies (
                    id, user_id, name, description, timeframe, 
                    is_active, is_custom, is_favorite, initial_capital, 
                    max_position_size_percent, parameters, created_at, updated_at
                ) VALUES (
                    @id, @userId, @name, @description, @timeframe,
                    @isActive, @isCustom, @isFavorite, @initialCapital,
                    @maxPositionSizePercent, @parameters::jsonb, @createdAt, @updatedAt
                )";
            
            await _context.Database.ExecuteSqlRawAsync(sql,
                new Npgsql.NpgsqlParameter("@id", strategyId),
                new Npgsql.NpgsqlParameter("@userId", userId),
                new Npgsql.NpgsqlParameter("@name", request.Name),
                new Npgsql.NpgsqlParameter("@description", request.Description ?? ""),
                new Npgsql.NpgsqlParameter("@timeframe", "1h"),
                new Npgsql.NpgsqlParameter("@isActive", false),
                new Npgsql.NpgsqlParameter("@isCustom", true),
                new Npgsql.NpgsqlParameter("@isFavorite", false),
                new Npgsql.NpgsqlParameter("@initialCapital", 10000),
                new Npgsql.NpgsqlParameter("@maxPositionSizePercent", 5),
                new Npgsql.NpgsqlParameter("@parameters", parametersJson),
                new Npgsql.NpgsqlParameter("@createdAt", DateTimeOffset.UtcNow),
                new Npgsql.NpgsqlParameter("@updatedAt", DateTimeOffset.UtcNow)
            );
            
            return Ok(new 
            { 
                success = true, 
                message = "Strategy created successfully", 
                strategy_id = strategyId.ToString() 
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception details: {ex}");
            return StatusCode(500, new { success = false, message = $"Failed to create strategy: {ex.Message}", details = ex.InnerException?.Message });
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
    // [Authorize] // Temporarily disabled
    public async Task<ActionResult> GetUserStrategies()
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("🔍 GetUserStrategies: Loading strategies for user: {UserId}", userId);

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
    public string? Description { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class TestStrategyRequest
{
    public string Symbol { get; set; } = string.Empty;
}