using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.Services;

/// <summary>
/// Mock implementation of IMarketStatusService for development
/// Provides sample data to prevent HTTP 409 errors
/// </summary>
public class MockMarketStatusService : IMarketStatusService
{
    private readonly ILogger<MockMarketStatusService> _logger;

    public MockMarketStatusService(ILogger<MockMarketStatusService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<MarketStatusChangedEventArgs>? OnMarketStatusChanged;

    public Task<List<MarketStatusDto>> GetAllMarketStatusesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetAllMarketStatusesAsync called");

        var statuses = new List<MarketStatusDto>
        {
            new MarketStatusDto
            {
                Status = "OPEN",
                StatusMessage = "24/7 Trading"
            },
            new MarketStatusDto
            {
                Status = "CLOSED",
                StatusMessage = "Market Closed"
            },
            new MarketStatusDto
            {
                Status = "CLOSED",
                StatusMessage = "Market Closed"
            }
        };

        return Task.FromResult(statuses);
    }

    public Task<MarketStatusDto?> GetMarketStatusAsync(string marketCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetMarketStatusAsync called for market: {MarketCode}", marketCode);

        var status = new MarketStatusDto
        {
            Status = marketCode == "CRYPTO" ? "OPEN" : "CLOSED",
            StatusMessage = marketCode == "CRYPTO" ? "24/7 Trading" : "Market Closed"
        };

        return Task.FromResult<MarketStatusDto?>(status);
    }

    public Task<bool> UpdateMarketStatusAsync(string marketCode, string status, string? statusMessage = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock UpdateMarketStatusAsync called for market: {MarketCode}, status: {Status}", marketCode, status);
        return Task.FromResult(true);
    }

    public Task<bool> IsMarketOpenAsync(string marketCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock IsMarketOpenAsync called for market: {MarketCode}", marketCode);
        return Task.FromResult(marketCode == "CRYPTO");
    }

    public Task<MarketTimingDto?> GetMarketTimingAsync(string marketCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetMarketTimingAsync called for market: {MarketCode}", marketCode);

        var timing = new MarketTimingDto
        {
            MarketCode = marketCode,
            Status = marketCode == "CRYPTO" ? "OPEN" : "CLOSED",
            NextOpen = marketCode == "CRYPTO" ? null : DateTime.UtcNow.AddHours(12),
            NextClose = marketCode == "CRYPTO" ? null : DateTime.UtcNow.AddHours(8),
            Timezone = marketCode switch
            {
                "NASDAQ" => "EST",
                "BIST" => "TRT",
                _ => "UTC"
            }
        };

        return Task.FromResult<MarketTimingDto?>(timing);
    }

    public Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock StartMonitoringAsync called");
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock StopMonitoringAsync called");
        return Task.CompletedTask;
    }
}