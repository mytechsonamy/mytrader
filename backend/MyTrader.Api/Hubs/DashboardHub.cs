using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyTrader.Core.DTOs.Dashboard;
using MyTrader.Core.DTOs.Indicators;
using MyTrader.Infrastructure.Data;
using MyTrader.Core.Services;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;

namespace MyTrader.Api.Hubs;

[AllowAnonymous]
public class DashboardHub : Hub
{
    private readonly TradingDbContext _context;
    private readonly MyTrader.Services.Market.ISymbolService _symbolService;
    private readonly IIndicatorService _indicatorService;
    private readonly ISignalGenerationEngine _signalEngine;
    private readonly ILogger<DashboardHub> _logger;
    
    // Track active subscriptions
    private static readonly ConcurrentDictionary<string, UserSubscriptions> _userSubscriptions = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes = new();

    public DashboardHub(
        TradingDbContext context,
    MyTrader.Services.Market.ISymbolService symbolService, 
        IIndicatorService indicatorService,
        ISignalGenerationEngine signalEngine,
        ILogger<DashboardHub> logger)
    {
        _context = context;
        _symbolService = symbolService;
        _indicatorService = indicatorService;
        _signalEngine = signalEngine;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;
        
        _logger.LogInformation("Dashboard connection established: {ConnectionId} for user {UserId}", connectionId, userId);
        
        // Add to user group
        await Groups.AddToGroupAsync(connectionId, $"user_{userId}");

        // Add to asset class groups for price updates
        await Groups.AddToGroupAsync(connectionId, "AssetClass_CRYPTO");
        await Groups.AddToGroupAsync(connectionId, "AssetClass_STOCK");
        await Groups.AddToGroupAsync(connectionId, "AssetClass_GENERAL");

        // Initialize user subscriptions
        _userSubscriptions.TryAdd(connectionId, new UserSubscriptions { UserId = userId });
        
        // Send initial dashboard data
        await SendInitialDashboardData();
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();
        
        _logger.LogInformation("Dashboard connection closed: {ConnectionId} for user {UserId}", connectionId, userId);
        
        // Clean up subscriptions
        _userSubscriptions.TryRemove(connectionId, out _);
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to real-time indicator updates for specific symbols
    /// </summary>
    public async Task SubscribeToIndicators(List<string> symbols, List<string> timeframes)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();
        
        try
        {
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.IndicatorSubscriptions.Clear();
                
                foreach (var symbol in symbols)
                {
                    foreach (var timeframe in timeframes)
                    {
                        var key = $"{symbol}_{timeframe}";
                        subscriptions.IndicatorSubscriptions.Add(key);
                        
                        // Add to SignalR group for this symbol/timeframe
                        await Groups.AddToGroupAsync(connectionId, $"indicators_{key}");
                        
                        _logger.LogDebug("User {UserId} subscribed to indicators: {Symbol} {Timeframe}", userId, symbol, timeframe);
                    }
                }
                
                await Clients.Caller.SendAsync("SubscriptionConfirmed", new
                {
                    Type = "indicators",
                    Symbols = symbols,
                    Timeframes = timeframes,
                    Count = symbols.Count * timeframes.Count
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to indicators for user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to subscribe to indicators");
        }
    }

    /// <summary>
    /// Subscribe to real-time trading signals
    /// </summary>
    public async Task SubscribeToSignals(List<string> symbols, decimal minConfidence = 50m)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();
        
        try
        {
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.SignalSubscriptions.Clear();
                subscriptions.MinSignalConfidence = minConfidence;
                
                foreach (var symbol in symbols)
                {
                    subscriptions.SignalSubscriptions.Add(symbol);
                    await Groups.AddToGroupAsync(connectionId, $"signals_{symbol}");
                    
                    _logger.LogDebug("User {UserId} subscribed to signals: {Symbol} (min confidence: {MinConfidence})", 
                        userId, symbol, minConfidence);
                }
                
                await Clients.Caller.SendAsync("SubscriptionConfirmed", new
                {
                    Type = "signals",
                    Symbols = symbols,
                    MinConfidence = minConfidence,
                    Count = symbols.Count
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to signals for user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to subscribe to signals");
        }
    }

    /// <summary>
    /// Subscribe to portfolio updates
    /// </summary>
    public async Task SubscribeToPortfolio()
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();
        
        try
        {
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.PortfolioSubscription = true;
                await Groups.AddToGroupAsync(connectionId, $"portfolio_{userId}");
                
                // Send initial portfolio data
                var portfolioData = await GetPortfolioData(userId);
                await Clients.Caller.SendAsync("PortfolioUpdate", portfolioData);
                
                _logger.LogDebug("User {UserId} subscribed to portfolio updates", userId);
                
                await Clients.Caller.SendAsync("SubscriptionConfirmed", new
                {
                    Type = "portfolio",
                    Message = "Portfolio subscription active"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to portfolio for user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to subscribe to portfolio");
        }
    }

    /// <summary>
    /// Subscribe to strategy performance updates
    /// </summary>
    public async Task SubscribeToStrategies(List<Guid> strategyIds)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();
        
        try
        {
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.StrategySubscriptions.Clear();
                subscriptions.StrategySubscriptions.AddRange(strategyIds);
                
                foreach (var strategyId in strategyIds)
                {
                    await Groups.AddToGroupAsync(connectionId, $"strategy_{strategyId}");
                }
                
                _logger.LogDebug("User {UserId} subscribed to {Count} strategies", userId, strategyIds.Count);
                
                await Clients.Caller.SendAsync("SubscriptionConfirmed", new
                {
                    Type = "strategies",
                    StrategyIds = strategyIds,
                    Count = strategyIds.Count
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to strategies for user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to subscribe to strategies");
        }
    }

    /// <summary>
    /// Request immediate update for specific data type
    /// </summary>
    public async Task RequestUpdate(string dataType, Dictionary<string, object>? parameters = null)
    {
        var userId = GetUserId();
        
        try
        {
            switch (dataType.ToLower())
            {
                case "indicators":
                    if (parameters != null && parameters.TryGetValue("symbol", out var symbol) && 
                        parameters.TryGetValue("timeframe", out var timeframe))
                    {
                        await SendIndicatorUpdate(symbol.ToString()!, timeframe.ToString()!);
                    }
                    break;
                    
                case "portfolio":
                    var portfolioData = await GetPortfolioData(userId);
                    await Clients.Caller.SendAsync("PortfolioUpdate", portfolioData);
                    break;
                    
                case "signals":
                    await SendRecentSignals(userId);
                    break;
                    
                case "dashboard":
                    await SendInitialDashboardData();
                    break;
                    
                default:
                    await Clients.Caller.SendAsync("Error", $"Unknown data type: {dataType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update request for {DataType} from user {UserId}", dataType, userId);
            await Clients.Caller.SendAsync("Error", "Failed to process update request");
        }
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    public async Task UpdateNotificationPreferences(NotificationPreferences preferences)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();
        
        try
        {
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.NotificationPreferences = preferences;
                
                _logger.LogInformation("Updated notification preferences for user {UserId}", userId);
                
                await Clients.Caller.SendAsync("NotificationPreferencesUpdated", preferences);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to update notification preferences");
        }
    }

    // Static methods for sending updates from external services
    public static async Task SendIndicatorUpdateToAll(IHubContext<DashboardHub> hubContext, 
        string symbol, string timeframe, IndicatorUpdateDto update)
    {
        var groupName = $"indicators_{symbol}_{timeframe}";
        var lastUpdateKey = $"{symbol}_{timeframe}";
        
        // Throttle updates to prevent spam (max 1 per second)
        if (_lastUpdateTimes.TryGetValue(lastUpdateKey, out var lastUpdate) && 
            DateTime.UtcNow.Subtract(lastUpdate).TotalSeconds < 1)
        {
            return;
        }
        
        _lastUpdateTimes[lastUpdateKey] = DateTime.UtcNow;
        
        await hubContext.Clients.Group(groupName).SendAsync("IndicatorUpdate", update);
    }

    public static async Task SendSignalToUsers(IHubContext<DashboardHub> hubContext, 
        string symbol, SignalNotification signal)
    {
        var groupName = $"signals_{symbol}";
        
        // Filter by user preferences
        foreach (var subscription in _userSubscriptions.Values)
        {
            if (subscription.SignalSubscriptions.Contains(symbol) && 
                signal.Confidence >= subscription.MinSignalConfidence)
            {
                // Send to specific user if they meet criteria
                await hubContext.Clients.Group($"user_{subscription.UserId}")
                    .SendAsync("SignalAlert", signal);
            }
        }
    }

    public static async Task SendPortfolioUpdate(IHubContext<DashboardHub> hubContext, 
        Guid userId, PortfolioUpdateDto update)
    {
        var groupName = $"portfolio_{userId}";
        await hubContext.Clients.Group(groupName).SendAsync("PortfolioUpdate", update);
    }

    // Private helper methods
    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }

    private async Task SendInitialDashboardData()
    {
        var userId = GetUserId();
        
        try
        {
            var dashboardData = new DashboardSnapshot
            {
                Portfolio = await GetPortfolioData(userId),
                RecentSignals = await GetRecentSignals(userId),
                ActiveStrategies = await GetActiveStrategies(userId),
                MarketOverview = await GetMarketOverview(),
                Timestamp = DateTime.UtcNow
            };
            
            await Clients.Caller.SendAsync("DashboardSnapshot", dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial dashboard data for user {UserId}", userId);
        }
    }

    private async Task<PortfolioSummaryDto> GetPortfolioData(Guid userId)
    {
        // Mock portfolio data - in production would calculate from actual positions
        var random = new Random();
        return new PortfolioSummaryDto
        {
            TotalValue = 50000m + (decimal)(random.NextDouble() * 10000),
            DayChange = (decimal)(random.NextDouble() * 2000 - 1000),
            DayChangePercent = (decimal)(random.NextDouble() * 4 - 2),
            TotalReturn = (decimal)(random.NextDouble() * 10000 - 5000),
            TotalReturnPercent = (decimal)(random.NextDouble() * 20 - 10),
            ActivePositions = random.Next(3, 8),
            CashBalance = 5000m + (decimal)(random.NextDouble() * 5000),
            LastUpdated = DateTime.UtcNow
        };
    }

    private async Task<List<SignalAlertDto>> GetRecentSignals(Guid userId)
    {
        // Mock recent signals - in production would query actual signals
        var signals = new List<SignalAlertDto>();
        var trackedSymbols = await _symbolService.GetTrackedAsync("BINANCE");
        var symbols = trackedSymbols.Select(s => s.Ticker).Take(3).ToArray();
        
        if (!symbols.Any())
        {
            symbols = new[] { "BTCUSDT", "ETHUSDT", "ADAUSDT" };
        }
        var random = new Random();
        
        for (int i = 0; i < 5; i++)
        {
            signals.Add(new SignalAlertDto
            {
                Symbol = symbols[random.Next(symbols.Length)],
                Type = random.Next(2) == 0 ? "BUY" : "SELL",
                Confidence = (decimal)(random.NextDouble() * 40 + 60), // 60-100%
                Price = 45000m + (decimal)(random.NextDouble() * 10000),
                Source = "RSI",
                GeneratedAt = DateTime.UtcNow.AddMinutes(-random.Next(60)),
                Reason = "Technical indicator signal"
            });
        }
        
        return signals.OrderByDescending(s => s.GeneratedAt).ToList();
    }

    private async Task<List<StrategyStatusDto>> GetActiveStrategies(Guid userId)
    {
        try
        {
            var strategies = await _context.UserStrategies
                .Where(us => us.UserId == userId && us.IsActive)
                .Take(10)
                .ToListAsync();
                
            return strategies.Select(s => new StrategyStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                Status = "Active",
                Performance = (decimal)(new Random().NextDouble() * 20 - 10), // Mock performance
                LastSignal = DateTime.UtcNow.AddHours(-new Random().Next(24))
            }).ToList();
        }
        catch
        {
            return new List<StrategyStatusDto>();
        }
    }

    private async Task<MarketOverviewDto> GetMarketOverview()
    {
        // Mock market overview - in production would fetch from market data service
        var random = new Random();
        return new MarketOverviewDto
        {
            MarketSentiment = random.Next(3) switch
            {
                0 => "Bullish",
                1 => "Bearish", 
                _ => "Neutral"
            },
            VolumeIndex = (decimal)(random.NextDouble() * 200 + 50), // 50-250
            VolatilityIndex = (decimal)(random.NextDouble() * 50 + 10), // 10-60
            TopMovers = await GetTopMoversAsync(),
            LastUpdated = DateTime.UtcNow
        };
    }

    private async Task<List<MarketMoverDto>> GetTopMoversAsync()
    {
        var trackedSymbols = await _symbolService.GetTrackedAsync("BINANCE");
        var random = new Random();
        
        return trackedSymbols.Take(3).Select(symbol => new MarketMoverDto
        {
            Symbol = symbol.Ticker,
            Change = (decimal)(random.NextDouble() * 10 - 5)
        }).ToList();
    }

    private async Task SendIndicatorUpdate(string symbol, string timeframe)
    {
        try
        {
            // Mock indicator update - in production would calculate real indicators
            var update = new IndicatorUpdateDto
            {
                Symbol = symbol,
                Timeframe = timeframe,
                Price = 45000m + (decimal)(new Random().NextDouble() * 1000),
                Timestamp = DateTime.UtcNow,
                UpdatedIndicators = new Dictionary<string, object>
                {
                    ["RSI"] = new Random().NextDouble() * 100,
                    ["MACD"] = new { Value = new Random().NextDouble() * 200 - 100, Signal = new Random().NextDouble() * 200 - 100 }
                }
            };
            
            await Clients.Caller.SendAsync("IndicatorUpdate", update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending indicator update for {Symbol} {Timeframe}", symbol, timeframe);
        }
    }

    private async Task SendRecentSignals(Guid userId)
    {
        var signals = await GetRecentSignals(userId);
        await Clients.Caller.SendAsync("SignalsUpdate", signals);
    }
}

// Supporting classes
public class UserSubscriptions
{
    public Guid UserId { get; set; }
    public List<string> IndicatorSubscriptions { get; set; } = new();
    public List<string> SignalSubscriptions { get; set; } = new();
    public List<Guid> StrategySubscriptions { get; set; } = new();
    public bool PortfolioSubscription { get; set; }
    public decimal MinSignalConfidence { get; set; } = 50m;
    public NotificationPreferences NotificationPreferences { get; set; } = new();
}