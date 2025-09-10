using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.Services;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BacktestController : ControllerBase
{
    private readonly IBacktestEngine _backtestEngine;
    private readonly IStrategyManagementService _strategyManagementService;
    private readonly IPerformanceTrackingService _performanceTrackingService;
    private readonly ILogger<BacktestController> _logger;

    public BacktestController(
        IBacktestEngine backtestEngine,
        IStrategyManagementService strategyManagementService,
        IPerformanceTrackingService performanceTrackingService,
        ILogger<BacktestController> logger)
    {
        _backtestEngine = backtestEngine;
        _strategyManagementService = strategyManagementService;
        _performanceTrackingService = performanceTrackingService;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunBacktest([FromBody] BacktestRunRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            var backtestRequest = new BacktestRequest
            {
                UserId = userId,
                StrategyId = request.StrategyId,
                SymbolId = request.SymbolId,
                ConfigurationId = Guid.NewGuid(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Timeframe = request.Timeframe,
                InitialBalance = request.InitialBalance,
                StrategyParameters = request.StrategyParameters ?? new StrategyParameters()
            };

            var result = await _backtestEngine.RunBacktestAsync(backtestRequest);
            
            return Ok(new BacktestRunResponse
            {
                BacktestId = result.Id,
                Status = result.Status,
                TotalReturn = result.TotalReturn,
                TotalReturnPercentage = result.TotalReturnPercentage,
                MaxDrawdown = result.MaxDrawdownPercentage,
                SharpeRatio = result.SharpeRatio,
                WinRate = result.WinRate,
                TotalTrades = result.TotalTrades,
                CompletedAt = result.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run backtest for user {UserId}", GetUserId());
            return BadRequest(new { message = "Failed to run backtest", error = ex.Message });
        }
    }

    [HttpPost("optimize")]
    public async Task<IActionResult> RunOptimization([FromBody] OptimizationRunRequest request)
    {
        try
        {
            var userId = GetUserId();

            var optimizationRequest = new OptimizationRequest
            {
                UserId = userId,
                StrategyId = request.StrategyId,
                SymbolId = request.SymbolId,
                ConfigurationId = Guid.NewGuid(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Timeframe = request.Timeframe,
                InitialBalance = request.InitialBalance,
                ParameterRanges = request.ParameterRanges
            };

            var results = await _backtestEngine.RunOptimizationAsync(optimizationRequest);

            var response = results.Take(20).Select(r => new OptimizationResult
            {
                ParameterSet = r.StrategyConfig,
                TotalReturn = r.TotalReturnPercentage,
                SharpeRatio = r.SharpeRatio,
                MaxDrawdown = r.MaxDrawdownPercentage,
                WinRate = r.WinRate,
                TotalTrades = r.TotalTrades
            }).ToList();

            return Ok(new { results = response, totalCount = results.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run optimization for user {UserId}", GetUserId());
            return BadRequest(new { message = "Failed to run optimization", error = ex.Message });
        }
    }

    [HttpGet("strategies/default")]
    public async Task<IActionResult> GetDefaultStrategies()
    {
        try
        {
            var strategies = await _strategyManagementService.GetDefaultStrategiesAsync();
            
            var response = strategies.Select(s => new DefaultStrategyResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                SymbolId = s.SymbolId ?? Guid.Empty,
                PerformanceScore = s.PerformanceScore ?? 0m,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default strategies");
            return BadRequest(new { message = "Failed to get default strategies", error = ex.Message });
        }
    }

    [HttpPost("strategies/user")]
    public async Task<IActionResult> CreateUserStrategy([FromBody] CreateUserStrategyRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            var userStrategy = await _strategyManagementService.CreateUserStrategyAsync(
                userId, request.StrategyId, request.CustomParameters);

            return Ok(new UserStrategyResponse
            {
                Id = userStrategy.Id,
                StrategyId = userStrategy.TemplateId ?? Guid.Empty,
                IsActive = userStrategy.IsActive,
                HasCustomParameters = userStrategy.Parameters.RootElement.EnumerateObject().Any(),
                CreatedAt = userStrategy.CreatedAt.DateTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user strategy for user {UserId}", GetUserId());
            return BadRequest(new { message = "Failed to create user strategy", error = ex.Message });
        }
    }

    [HttpGet("strategies/user")]
    public async Task<IActionResult> GetUserStrategies()
    {
        try
        {
            var userId = GetUserId();
            var userStrategies = await _strategyManagementService.GetUserStrategiesAsync(userId);

            var response = userStrategies.Select(us => new UserStrategyDetailResponse
            {
                Id = us.Id,
                Strategy = null, // Navigation property not available, will be handled separately
                IsActive = us.IsActive,
                HasCustomParameters = us.Parameters.RootElement.EnumerateObject().Any(),
                CreatedAt = us.CreatedAt.DateTime
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user strategies for user {UserId}", GetUserId());
            return BadRequest(new { message = "Failed to get user strategies", error = ex.Message });
        }
    }

    [HttpGet("performance/report")]
    public async Task<IActionResult> GetPerformanceReport()
    {
        try
        {
            var report = await _performanceTrackingService.GenerateDailyReportAsync();
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate performance report");
            return BadRequest(new { message = "Failed to generate performance report", error = ex.Message });
        }
    }

    [HttpGet("performance/top-strategies")]
    public async Task<IActionResult> GetTopPerformingStrategies([FromQuery] int count = 10)
    {
        try
        {
            var topStrategies = await _performanceTrackingService.GetTopPerformingStrategiesAsync(count);
            return Ok(topStrategies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top performing strategies");
            return BadRequest(new { message = "Failed to get top performing strategies", error = ex.Message });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }
        return userId;
    }
}

// Request/Response DTOs
public class BacktestRunRequest
{
    public Guid StrategyId { get; set; }
    public Guid SymbolId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Timeframe { get; set; } = "1h";
    public decimal InitialBalance { get; set; } = 10000m;
    public StrategyParameters? StrategyParameters { get; set; }
}

public class BacktestRunResponse
{
    public Guid BacktestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercentage { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class OptimizationRunRequest : BacktestRunRequest
{
    public Dictionary<string, ParameterRange> ParameterRanges { get; set; } = new();
}

public class OptimizationResult
{
    public string? ParameterSet { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
}

public class DefaultStrategyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SymbolId { get; set; }
    public decimal PerformanceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateUserStrategyRequest
{
    public Guid StrategyId { get; set; }
    public StrategyParameters? CustomParameters { get; set; }
}

public class UserStrategyResponse
{
    public Guid Id { get; set; }
    public Guid StrategyId { get; set; }
    public bool IsActive { get; set; }
    public bool HasCustomParameters { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserStrategyDetailResponse
{
    public Guid Id { get; set; }
    public StrategyInfo? Strategy { get; set; }
    public bool IsActive { get; set; }
    public bool HasCustomParameters { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StrategyInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SymbolName { get; set; } = string.Empty;
}