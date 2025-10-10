using Microsoft.AspNetCore.Mvc;
using MyTrader.Infrastructure.Services;
using MyTrader.Core.Services;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Health monitoring endpoints for Alpaca streaming integration
/// </summary>
[ApiController]
[Route("api/health")]
public class AlpacaHealthController : ControllerBase
{
    private readonly IAlpacaStreamingService _alpacaService;
    private readonly IDataSourceRouter _dataSourceRouter;
    private readonly ILogger<AlpacaHealthController> _logger;

    public AlpacaHealthController(
        IAlpacaStreamingService alpacaService,
        IDataSourceRouter dataSourceRouter,
        ILogger<AlpacaHealthController> logger)
    {
        _alpacaService = alpacaService;
        _dataSourceRouter = dataSourceRouter;
        _logger = logger;
    }

    /// <summary>
    /// Get Alpaca connection health status
    /// </summary>
    [HttpGet("alpaca")]
    public async Task<IActionResult> GetAlpacaHealth()
    {
        try
        {
            var health = await _alpacaService.GetHealthStatusAsync();

            var status = health.IsConnected && health.IsAuthenticated ? "Healthy" : "Unhealthy";

            var response = new
            {
                status,
                alpacaStatus = new
                {
                    connected = health.IsConnected,
                    authenticated = health.IsAuthenticated,
                    subscribedSymbols = health.SubscribedSymbols,
                    lastMessageReceived = health.LastMessageReceived,
                    messagesPerMinute = health.MessagesPerMinute,
                    connectionUptime = health.ConnectionUptime?.ToString(@"hh\:mm\:ss"),
                    consecutiveFailures = health.ConsecutiveFailures,
                    lastError = health.LastError,
                    state = health.State.ToString()
                },
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Alpaca health status");
            return StatusCode(500, new { error = "Failed to retrieve Alpaca health status", message = ex.Message });
        }
    }

    /// <summary>
    /// Get data source router status (routing state, failover metrics)
    /// </summary>
    [HttpGet("datasource")]
    public IActionResult GetDataSourceHealth()
    {
        try
        {
            var status = _dataSourceRouter.GetStatus();

            var routerStatusText = status.CurrentState switch
            {
                RoutingState.PRIMARY_ACTIVE => "Healthy",
                RoutingState.FALLBACK_ACTIVE => "Degraded",
                RoutingState.BOTH_UNAVAILABLE => "Unhealthy",
                _ => "Unknown"
            };

            var response = new
            {
                status = routerStatusText,
                connectionState = status.CurrentState.ToString(),
                stateChangedAt = status.StateChangedAt,
                stateChangeReason = status.StateChangeReason,
                alpacaStatus = new
                {
                    name = status.AlpacaStatus.Name,
                    isHealthy = status.AlpacaStatus.IsHealthy,
                    lastMessageReceived = status.AlpacaStatus.LastMessageReceivedAt,
                    messagesReceived = status.AlpacaStatus.MessagesReceivedCount,
                    consecutiveFailures = status.AlpacaStatus.ConsecutiveFailures,
                    lastError = status.AlpacaStatus.LastError
                },
                yahooStatus = new
                {
                    name = status.YahooStatus.Name,
                    isHealthy = status.YahooStatus.IsHealthy,
                    lastMessageReceived = status.YahooStatus.LastMessageReceivedAt,
                    messagesReceived = status.YahooStatus.MessagesReceivedCount,
                    consecutiveFailures = status.YahooStatus.ConsecutiveFailures,
                    lastError = status.YahooStatus.LastError
                },
                fallbackCount = status.FallbackActivationCount,
                lastFallback = status.LastFallbackActivation,
                totalFallbackDuration = status.TotalFallbackDuration.ToString(@"hh\:mm\:ss"),
                uptimePercent = status.UptimePercent,
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data source router status");
            return StatusCode(500, new { error = "Failed to retrieve data source router status", message = ex.Message });
        }
    }

    /// <summary>
    /// Force manual failover to Yahoo Finance (admin only)
    /// </summary>
    [HttpPost("failover")]
    public async Task<IActionResult> ForceFailover()
    {
        try
        {
            await _dataSourceRouter.ForceFailoverAsync();

            _logger.LogWarning("Manual failover triggered via API");

            return Ok(new
            {
                message = "Failover to Yahoo Finance activated",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing failover");
            return StatusCode(500, new { error = "Failed to force failover", message = ex.Message });
        }
    }

    /// <summary>
    /// Force Alpaca reconnection (admin only)
    /// </summary>
    [HttpPost("alpaca/reconnect")]
    public async Task<IActionResult> ForceReconnect()
    {
        try
        {
            await _alpacaService.ForceReconnectAsync();

            _logger.LogWarning("Manual reconnection triggered via API");

            return Ok(new
            {
                message = "Alpaca reconnection initiated",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing reconnection");
            return StatusCode(500, new { error = "Failed to force reconnection", message = ex.Message });
        }
    }

    /// <summary>
    /// Combined health check for all stock data sources
    /// </summary>
    [HttpGet("stocks")]
    public async Task<IActionResult> GetStocksHealth()
    {
        try
        {
            var alpacaHealth = await _alpacaService.GetHealthStatusAsync();
            var routerStatus = _dataSourceRouter.GetStatus();

            var overallStatus = routerStatus.CurrentState switch
            {
                RoutingState.PRIMARY_ACTIVE => "Healthy",
                RoutingState.FALLBACK_ACTIVE => "Degraded",
                RoutingState.BOTH_UNAVAILABLE => "Unhealthy",
                _ => "Unknown"
            };

            var response = new
            {
                status = overallStatus,
                currentDataSource = routerStatus.CurrentState == RoutingState.PRIMARY_ACTIVE ? "Alpaca (Real-time)" : "Yahoo (Fallback)",
                alpaca = new
                {
                    connected = alpacaHealth.IsConnected,
                    authenticated = alpacaHealth.IsAuthenticated,
                    healthy = routerStatus.AlpacaStatus.IsHealthy,
                    lastMessage = alpacaHealth.LastMessageReceived,
                    messagesPerMinute = alpacaHealth.MessagesPerMinute
                },
                yahoo = new
                {
                    healthy = routerStatus.YahooStatus.IsHealthy,
                    lastMessage = routerStatus.YahooStatus.LastMessageReceivedAt,
                    messagesReceived = routerStatus.YahooStatus.MessagesReceivedCount
                },
                metrics = new
                {
                    uptimePercent = routerStatus.UptimePercent,
                    fallbackCount = routerStatus.FallbackActivationCount,
                    totalFallbackDuration = routerStatus.TotalFallbackDuration.ToString(@"hh\:mm\:ss")
                },
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stocks health status");
            return StatusCode(500, new { error = "Failed to retrieve stocks health status", message = ex.Message });
        }
    }
}
