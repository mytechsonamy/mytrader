using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using System.Collections.Concurrent;

namespace MyTrader.Core.Services;

/// <summary>
/// Routing states for data source management
/// </summary>
public enum RoutingState
{
    STARTUP,
    PRIMARY_ACTIVE,
    FALLBACK_ACTIVE,
    BOTH_UNAVAILABLE
}

/// <summary>
/// Interface for data source router
/// </summary>
public interface IDataSourceRouter
{
    RoutingState CurrentState { get; }
    void OnAlpacaPriceUpdate(StockPriceData data);
    void OnYahooPriceUpdate(StockPriceData data);
    void NotifyAlpacaHealthStatus(bool isHealthy);
    void NotifyYahooHealthStatus(bool isHealthy);
    Task ForceFailoverAsync();
    DataSourceRouterStatus GetStatus();
    event Action<StockPriceData>? PriceDataRouted;
    event Action<RoutingState, string>? StateChanged;
}

/// <summary>
/// Data source router status
/// </summary>
public class DataSourceRouterStatus
{
    public RoutingState CurrentState { get; set; }
    public DateTime StateChangedAt { get; set; }
    public string StateChangeReason { get; set; } = string.Empty;
    public DataProviderStatus AlpacaStatus { get; set; } = new();
    public DataProviderStatus YahooStatus { get; set; } = new();
    public int FallbackActivationCount { get; set; }
    public DateTime? LastFallbackActivation { get; set; }
    public TimeSpan TotalFallbackDuration { get; set; }
    public double UptimePercent { get; set; }
}

/// <summary>
/// Data provider status
/// </summary>
public class DataProviderStatus
{
    public string Name { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public DateTime? LastMessageReceivedAt { get; set; }
    public int ConsecutiveFailures { get; set; }
    public string? LastError { get; set; }
    public int MessagesReceivedCount { get; set; }
}

/// <summary>
/// Routes stock price data between Alpaca (primary) and Yahoo Finance (fallback)
/// Implements automatic failover and recovery with grace periods
/// </summary>
public class DataSourceRouter : IDataSourceRouter
{
    private readonly ILogger<DataSourceRouter> _logger;
    private readonly object _stateLock = new();
    private readonly TimeSpan _fallbackActivationDelay = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _primaryRecoveryGracePeriod = TimeSpan.FromSeconds(10);
    private readonly int _maxConsecutiveFailures = 3;

    // State management
    private RoutingState _currentState = RoutingState.STARTUP;
    private DateTime _stateChangedAt = DateTime.UtcNow;
    private string _stateChangeReason = "Initial startup";
    private DateTime? _fallbackActivationTime;

    // Provider status
    private readonly DataProviderStatus _alpacaStatus = new() { Name = "Alpaca" };
    private readonly DataProviderStatus _yahooStatus = new() { Name = "Yahoo" };

    // Metrics
    private int _fallbackActivationCount;
    private TimeSpan _totalFallbackDuration;
    private DateTime _serviceStartTime = DateTime.UtcNow;

    // Validation
    private readonly ConcurrentDictionary<string, decimal> _lastPriceBySymbol = new();

    public RoutingState CurrentState => _currentState;

    public event Action<StockPriceData>? PriceDataRouted;
    public event Action<RoutingState, string>? StateChanged;

    public DataSourceRouter(ILogger<DataSourceRouter> logger)
    {
        _logger = logger;
    }

    public void OnAlpacaPriceUpdate(StockPriceData data)
    {
        try
        {
            _alpacaStatus.LastMessageReceivedAt = DateTime.UtcNow;
            _alpacaStatus.MessagesReceivedCount++;
            _alpacaStatus.ConsecutiveFailures = 0;
            _alpacaStatus.IsHealthy = true;

            // Validate data
            if (!ValidateStockPriceData(data))
            {
                _logger.LogWarning("Invalid Alpaca price data for {Symbol}, rejecting", data.Symbol);
                return;
            }

            // Check if we should recover from fallback
            if (_currentState == RoutingState.FALLBACK_ACTIVE)
            {
                CheckPrimaryRecovery();
            }

            // Route data based on current state
            if (_currentState == RoutingState.PRIMARY_ACTIVE || _currentState == RoutingState.STARTUP)
            {
                // Transition from STARTUP to PRIMARY_ACTIVE on first message
                if (_currentState == RoutingState.STARTUP)
                {
                    TransitionToState(RoutingState.PRIMARY_ACTIVE, "Alpaca connected and receiving data");
                }

                RouteData(data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Alpaca price update");
            _alpacaStatus.ConsecutiveFailures++;
            _alpacaStatus.LastError = ex.Message;
        }
    }

    public void OnYahooPriceUpdate(StockPriceData data)
    {
        try
        {
            _yahooStatus.LastMessageReceivedAt = DateTime.UtcNow;
            _yahooStatus.MessagesReceivedCount++;
            _yahooStatus.ConsecutiveFailures = 0;
            _yahooStatus.IsHealthy = true;

            // Validate data
            if (!ValidateStockPriceData(data))
            {
                _logger.LogWarning("Invalid Yahoo price data for {Symbol}, rejecting", data.Symbol);
                return;
            }

            // Route data based on current state
            if (_currentState == RoutingState.FALLBACK_ACTIVE)
            {
                RouteData(data);
            }
            else if (_currentState == RoutingState.STARTUP && !_alpacaStatus.IsHealthy)
            {
                // Use Yahoo if Alpaca hasn't connected yet
                TransitionToState(RoutingState.FALLBACK_ACTIVE, "Yahoo active while waiting for Alpaca");
                RouteData(data);
            }
            // In PRIMARY_ACTIVE state, Yahoo events are ignored (but continue for DB persistence)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Yahoo price update");
            _yahooStatus.ConsecutiveFailures++;
            _yahooStatus.LastError = ex.Message;
        }
    }

    public void NotifyAlpacaHealthStatus(bool isHealthy)
    {
        _alpacaStatus.IsHealthy = isHealthy;

        if (!isHealthy)
        {
            _alpacaStatus.ConsecutiveFailures++;
            _logger.LogWarning("Alpaca health check failed (consecutive failures: {Count})",
                _alpacaStatus.ConsecutiveFailures);

            // Activate fallback if threshold exceeded
            if (_alpacaStatus.ConsecutiveFailures >= _maxConsecutiveFailures &&
                _currentState == RoutingState.PRIMARY_ACTIVE)
            {
                ActivateFallback("Alpaca unhealthy after multiple failures");
            }
        }
        else
        {
            _alpacaStatus.ConsecutiveFailures = 0;
            CheckPrimaryRecovery();
        }
    }

    public void NotifyYahooHealthStatus(bool isHealthy)
    {
        _yahooStatus.IsHealthy = isHealthy;

        if (!isHealthy)
        {
            _yahooStatus.ConsecutiveFailures++;
            _logger.LogWarning("Yahoo health check failed (consecutive failures: {Count})",
                _yahooStatus.ConsecutiveFailures);

            // Check if both sources are down
            if (!_alpacaStatus.IsHealthy && _yahooStatus.ConsecutiveFailures >= _maxConsecutiveFailures)
            {
                TransitionToState(RoutingState.BOTH_UNAVAILABLE, "Both Alpaca and Yahoo are unavailable");
            }
        }
        else
        {
            _yahooStatus.ConsecutiveFailures = 0;

            // Recover from BOTH_UNAVAILABLE if Yahoo is healthy
            if (_currentState == RoutingState.BOTH_UNAVAILABLE)
            {
                if (_alpacaStatus.IsHealthy)
                {
                    TransitionToState(RoutingState.PRIMARY_ACTIVE, "Alpaca recovered from both sources down");
                }
                else
                {
                    TransitionToState(RoutingState.FALLBACK_ACTIVE, "Yahoo recovered from both sources down");
                }
            }
        }
    }

    public async Task ForceFailoverAsync()
    {
        _logger.LogWarning("Manual failover requested");
        ActivateFallback("Manual failover triggered");
    }

    public DataSourceRouterStatus GetStatus()
    {
        var now = DateTime.UtcNow;
        var uptime = now - _serviceStartTime;
        var fallbackTime = _currentState == RoutingState.FALLBACK_ACTIVE && _fallbackActivationTime.HasValue
            ? now - _fallbackActivationTime.Value + _totalFallbackDuration
            : _totalFallbackDuration;

        var uptimePercent = uptime.TotalSeconds > 0
            ? ((uptime.TotalSeconds - fallbackTime.TotalSeconds) / uptime.TotalSeconds) * 100
            : 100;

        return new DataSourceRouterStatus
        {
            CurrentState = _currentState,
            StateChangedAt = _stateChangedAt,
            StateChangeReason = _stateChangeReason,
            AlpacaStatus = _alpacaStatus,
            YahooStatus = _yahooStatus,
            FallbackActivationCount = _fallbackActivationCount,
            LastFallbackActivation = _fallbackActivationTime,
            TotalFallbackDuration = fallbackTime,
            UptimePercent = Math.Round(uptimePercent, 2)
        };
    }

    private void RouteData(StockPriceData data)
    {
        try
        {
            // Cross-source validation
            if (_lastPriceBySymbol.TryGetValue(data.Symbol, out var lastPrice) && lastPrice > 0)
            {
                var priceDelta = Math.Abs((data.Price - lastPrice) / lastPrice * 100);
                if (priceDelta > 5)
                {
                    _logger.LogWarning("Large price movement detected for {Symbol}: {Delta}% (was ${LastPrice}, now ${NewPrice} from {Source})",
                        data.Symbol, Math.Round(priceDelta, 2), lastPrice, data.Price, data.Source);
                }
            }

            _lastPriceBySymbol[data.Symbol] = data.Price;

            // Emit routed event
            PriceDataRouted?.Invoke(data);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Routed {Symbol} price ${Price} from {Source} (state: {State})",
                    data.Symbol, data.Price, data.Source, _currentState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing price data for {Symbol}", data.Symbol);
        }
    }

    private bool ValidateStockPriceData(StockPriceData data)
    {
        // Validation Rule 1: Price must be positive
        if (data.Price <= 0)
        {
            _logger.LogWarning("Validation failed: Invalid price {Price} for {Symbol}", data.Price, data.Symbol);
            return false;
        }

        // Validation Rule 2: Volume must be non-negative
        if (data.Volume < 0)
        {
            _logger.LogWarning("Validation failed: Invalid volume {Volume} for {Symbol}", data.Volume, data.Symbol);
            return false;
        }

        // Validation Rule 3: Timestamp must not be in future
        if (data.Timestamp > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogWarning("Validation failed: Future timestamp {Timestamp} for {Symbol}",
                data.Timestamp, data.Symbol);
            return false;
        }

        // Validation Rule 4: Circuit breaker - reject >20% price movement
        if (data.PreviousClose.HasValue && data.PreviousClose > 0)
        {
            var priceChangePercent = Math.Abs((data.Price - data.PreviousClose.Value) / data.PreviousClose.Value * 100);

            if (priceChangePercent > 20)
            {
                _logger.LogError("Circuit breaker triggered: {Symbol} price movement {ChangePercent}% exceeds 20% threshold",
                    data.Symbol, Math.Round(priceChangePercent, 2));
                return false;
            }
        }

        return true;
    }

    private void ActivateFallback(string reason)
    {
        lock (_stateLock)
        {
            if (_currentState != RoutingState.FALLBACK_ACTIVE)
            {
                TransitionToState(RoutingState.FALLBACK_ACTIVE, reason);
                _fallbackActivationTime = DateTime.UtcNow;
                _fallbackActivationCount++;
            }
        }
    }

    private void CheckPrimaryRecovery()
    {
        lock (_stateLock)
        {
            if (_currentState != RoutingState.FALLBACK_ACTIVE) return;

            // Check if Alpaca is healthy and has received messages
            if (_alpacaStatus.IsHealthy &&
                _alpacaStatus.LastMessageReceivedAt.HasValue &&
                _alpacaStatus.ConsecutiveFailures == 0)
            {
                // Apply grace period
                var timeSinceLastMessage = DateTime.UtcNow - _alpacaStatus.LastMessageReceivedAt.Value;
                if (timeSinceLastMessage <= _primaryRecoveryGracePeriod)
                {
                    // Calculate fallback duration
                    if (_fallbackActivationTime.HasValue)
                    {
                        _totalFallbackDuration += DateTime.UtcNow - _fallbackActivationTime.Value;
                        _fallbackActivationTime = null;
                    }

                    TransitionToState(RoutingState.PRIMARY_ACTIVE, "Alpaca recovered and healthy");
                }
            }
        }
    }

    private void TransitionToState(RoutingState newState, string reason)
    {
        var oldState = _currentState;

        if (oldState == newState) return;

        _currentState = newState;
        _stateChangedAt = DateTime.UtcNow;
        _stateChangeReason = reason;

        _logger.LogInformation("Data source router state transition: {OldState} → {NewState} (reason: {Reason})",
            oldState, newState, reason);

        StateChanged?.Invoke(newState, reason);
    }
}
