using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Configuration settings for Alpaca API integration (REST + WebSocket)
/// </summary>
public class AlpacaConfiguration
{
    public string PaperApiKey { get; set; } = string.Empty;
    public string PaperSecretKey { get; set; } = string.Empty;
    public string LiveApiKey { get; set; } = string.Empty;
    public string LiveSecretKey { get; set; } = string.Empty;
    public bool UsePaperTrading { get; set; } = true;
    public string BaseUrl { get; set; } = "https://paper-api.alpaca.markets";
    public string DataUrl { get; set; } = "https://data.alpaca.markets";
    public int RateLimitPerMinute { get; set; } = 200;
    public int CacheExpirySeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerRecoveryTimeSeconds { get; set; } = 60;
    public bool EnableCaching { get; set; } = true;
    public AlpacaDefaultSymbols DefaultSymbols { get; set; } = new();

    // WebSocket streaming configuration
    public AlpacaStreamingConfiguration Streaming { get; set; } = new();
    public AlpacaFallbackConfiguration Fallback { get; set; } = new();
}

/// <summary>
/// Alpaca WebSocket streaming configuration
/// </summary>
public class AlpacaStreamingConfiguration
{
    public bool Enabled { get; set; }
    public string WebSocketUrl { get; set; } = "wss://stream.data.alpaca.markets/v2/iex";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public int MaxSymbols { get; set; } = 30;
    public bool SubscribeToTrades { get; set; } = true;
    public bool SubscribeToQuotes { get; set; } = true;
    public bool SubscribeToBars { get; set; } = false;
    public int ReconnectBaseDelayMs { get; set; } = 1000;
    public int ReconnectMaxDelayMs { get; set; } = 60000;
    public int MessageTimeoutSeconds { get; set; } = 30;
    public int HealthCheckIntervalSeconds { get; set; } = 60;
    public int AuthTimeoutSeconds { get; set; } = 10;
    public bool EnableDetailedLogging { get; set; }
}

/// <summary>
/// Fallback configuration for Yahoo Finance
/// </summary>
public class AlpacaFallbackConfiguration
{
    public bool EnableYahooFallback { get; set; } = true;
    public int FallbackActivationDelaySeconds { get; set; } = 10;
    public int PrimaryRecoveryGracePeriodSeconds { get; set; } = 10;
    public int MaxConsecutiveFailures { get; set; } = 3;
    public bool NotifyUsersOnFallback { get; set; } = true;
    public bool NotifyUsersOnRecovery { get; set; } = true;
}

/// <summary>
/// Default symbols configuration for Alpaca
/// </summary>
public class AlpacaDefaultSymbols
{
    public List<string> Crypto { get; set; } = new();
    public List<string> Stocks { get; set; } = new();
}

/// <summary>
/// Alpaca market data response for crypto assets
/// </summary>
public class AlpacaCryptoDataDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal High24h { get; set; }
    public decimal Low24h { get; set; }
    public decimal MarketCap { get; set; }
    public DateTime LastUpdated { get; set; }
    public string AssetClass { get; set; } = "CRYPTO";
}

/// <summary>
/// Alpaca market data response for stock assets
/// </summary>
public class AlpacaStockDataDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal? MarketCap { get; set; }
    public decimal? PE { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime LastUpdated { get; set; }
    public string AssetClass { get; set; } = "STOCK";
}

/// <summary>
/// Unified frontend market data DTO matching the existing format
/// </summary>
public class MarketDataDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal High24h { get; set; }
    public decimal Low24h { get; set; }
    public DateTime LastUpdated { get; set; }
    public string AssetClass { get; set; } = string.Empty;
    public decimal? MarketCap { get; set; }
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// Request for crypto market data
/// </summary>
public class CryptoDataRequest
{
    public List<string> Symbols { get; set; } = new();
    public bool IncludeMarketCap { get; set; } = true;
    public bool IncludeVolumeData { get; set; } = true;
}

/// <summary>
/// Request for NASDAQ stock data
/// </summary>
public class NasdaqDataRequest
{
    public List<string> Symbols { get; set; } = new();
    public bool IncludeFundamentals { get; set; } = false;
    public bool IncludeExtendedHours { get; set; } = false;
}

/// <summary>
/// Alpaca API response wrapper
/// </summary>
public class AlpacaApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
    public string? DataSource { get; set; }
}

/// <summary>
/// Alpaca rate limit status
/// </summary>
public class AlpacaRateLimitStatus
{
    public int RequestsRemaining { get; set; }
    public DateTime ResetTime { get; set; }
    public int RequestsPerMinute { get; set; }
    public bool IsNearLimit { get; set; }
}

/// <summary>
/// Alpaca circuit breaker status
/// </summary>
public class AlpacaCircuitBreakerStatus
{
    public bool IsOpen { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public DateTime? NextRetryTime { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Alpaca health check response
/// </summary>
public class AlpacaHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
    public AlpacaRateLimitStatus RateLimit { get; set; } = new();
    public AlpacaCircuitBreakerStatus CircuitBreaker { get; set; } = new();
    public Dictionary<string, string> Details { get; set; } = new();
}