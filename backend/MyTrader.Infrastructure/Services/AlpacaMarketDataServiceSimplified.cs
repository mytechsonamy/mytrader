using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using System.Collections.Concurrent;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Simplified Alpaca market data service with mock data for development
/// This can be enhanced with real Alpaca API integration once the SDK is properly configured
/// </summary>
public class AlpacaMarketDataServiceSimplified : IAlpacaMarketDataService, IDisposable
{
    private readonly AlpacaConfiguration _config;
    private readonly ILogger<AlpacaMarketDataServiceSimplified> _logger;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;

    // Rate limiting
    private readonly ConcurrentQueue<DateTime> _requestTimestamps = new();
    private readonly object _rateLimitLock = new();

    // Circuit breaker state
    private int _consecutiveFailures = 0;
    private DateTime? _lastFailureTime;
    private readonly object _circuitBreakerLock = new();

    private bool _disposed = false;

    public AlpacaMarketDataServiceSimplified(
        IOptions<AlpacaConfiguration> config,
        ILogger<AlpacaMarketDataServiceSimplified> logger,
        IMemoryCache cache,
        HttpClient httpClient)
    {
        _config = config.Value;
        _logger = logger;
        _cache = cache;
        _httpClient = httpClient;

        _logger.LogInformation("Alpaca Market Data Service initialized (Simplified version with mock data)");
    }

    public async Task<List<AlpacaCryptoDataDto>> GetCryptoMarketDataAsync(
        List<string>? symbols = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await CanMakeRequestAsync())
            {
                return await GetCachedCryptoDataAsync(symbols) ?? new List<AlpacaCryptoDataDto>();
            }

            symbols ??= _config.DefaultSymbols.Crypto;
            if (!symbols.Any())
            {
                _logger.LogWarning("No crypto symbols provided and no default symbols configured");
                return new List<AlpacaCryptoDataDto>();
            }

            var cacheKey = $"crypto_data_{string.Join(",", symbols.OrderBy(s => s))}";

            if (_config.EnableCaching && _cache.TryGetValue(cacheKey, out List<AlpacaCryptoDataDto>? cachedData))
            {
                _logger.LogDebug("Returning cached crypto data for {SymbolCount} symbols", symbols.Count);
                return cachedData ?? new List<AlpacaCryptoDataDto>();
            }

            // Generate mock data for development
            var result = GenerateMockCryptoData(symbols);

            if (_config.EnableCaching && result.Any())
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_config.CacheExpirySeconds),
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }

            await RecordSuccessfulRequestAsync();
            return result;
        }
        catch (Exception ex)
        {
            await RecordFailedRequestAsync(ex);
            _logger.LogError(ex, "Error fetching crypto market data");
            return GenerateMockCryptoData(symbols ?? _config.DefaultSymbols.Crypto);
        }
    }

    public async Task<List<AlpacaStockDataDto>> GetNasdaqMarketDataAsync(
        List<string>? symbols = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await CanMakeRequestAsync())
            {
                return await GetCachedStockDataAsync(symbols) ?? new List<AlpacaStockDataDto>();
            }

            symbols ??= _config.DefaultSymbols.Stocks;
            if (!symbols.Any())
            {
                _logger.LogWarning("No stock symbols provided and no default symbols configured");
                return new List<AlpacaStockDataDto>();
            }

            var cacheKey = $"stock_data_{string.Join(",", symbols.OrderBy(s => s))}";

            if (_config.EnableCaching && _cache.TryGetValue(cacheKey, out List<AlpacaStockDataDto>? cachedData))
            {
                _logger.LogDebug("Returning cached stock data for {SymbolCount} symbols", symbols.Count);
                return cachedData ?? new List<AlpacaStockDataDto>();
            }

            // Generate mock data for development
            var result = GenerateMockStockData(symbols);

            if (_config.EnableCaching && result.Any())
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_config.CacheExpirySeconds),
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }

            await RecordSuccessfulRequestAsync();
            return result;
        }
        catch (Exception ex)
        {
            await RecordFailedRequestAsync(ex);
            _logger.LogError(ex, "Error fetching NASDAQ market data");
            return GenerateMockStockData(symbols ?? _config.DefaultSymbols.Stocks);
        }
    }

    public async Task<List<MarketDataDto>> GetUnifiedMarketDataAsync(
        List<string>? symbols = null,
        string? assetClass = null,
        CancellationToken cancellationToken = default)
    {
        var result = new List<MarketDataDto>();

        try
        {
            if (assetClass == null || assetClass.ToUpper() == "CRYPTO")
            {
                var cryptoSymbols = symbols?.Where(s => _config.DefaultSymbols.Crypto.Contains(s.ToUpper())).ToList();
                var cryptoData = await GetCryptoMarketDataAsync(cryptoSymbols, cancellationToken);
                result.AddRange(cryptoData.Select(MapToUnifiedDto));
            }

            if (assetClass == null || assetClass.ToUpper() == "STOCK")
            {
                var stockSymbols = symbols?.Where(s => _config.DefaultSymbols.Stocks.Contains(s.ToUpper())).ToList();
                var stockData = await GetNasdaqMarketDataAsync(stockSymbols, cancellationToken);
                result.AddRange(stockData.Select(MapToUnifiedDto));
            }

            return result.OrderBy(x => x.Symbol).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unified market data");
            return result;
        }
    }

    public async Task<HistoricalMarketDataDto?> GetHistoricalDataAsync(
        string symbol,
        string interval,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate mock historical data
            var random = new Random();
            var candles = new List<CandlestickDataDto>();
            var basePrice = random.Next(50, 500);
            var currentPrice = (decimal)basePrice;

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;
            var intervalMinutes = GetIntervalMinutes(interval);

            for (var date = start; date <= end; date = date.AddMinutes(intervalMinutes))
            {
                var open = currentPrice;
                var change = (decimal)(random.NextDouble() * 10 - 5); // -5 to +5
                var close = Math.Max(1, open + change);
                var high = Math.Max(open, close) + (decimal)(random.NextDouble() * 5);
                var low = Math.Min(open, close) - (decimal)(random.NextDouble() * 5);

                candles.Add(new CandlestickDataDto
                {
                    OpenTime = date,
                    Open = open,
                    High = high,
                    Low = Math.Max(0.01m, low),
                    Close = close,
                    Volume = random.Next(1000, 10000),
                    CloseTime = date.AddMinutes(intervalMinutes),
                    TradeCount = random.Next(10, 100)
                });

                currentPrice = close;
            }

            return new HistoricalMarketDataDto
            {
                Ticker = symbol,
                Interval = interval,
                Candles = candles,
                StartTime = start,
                EndTime = end,
                CandleCount = candles.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical data for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<MarketOverviewDto> GetMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cryptoTask = GetCryptoMarketDataAsync(cancellationToken: cancellationToken);
            var stockTask = GetNasdaqMarketDataAsync(cancellationToken: cancellationToken);

            await Task.WhenAll(cryptoTask, stockTask);

            var cryptoData = await cryptoTask;
            var stockData = await stockTask;

            var combinedData = cryptoData.Select(MapToUnifiedDto).Concat(stockData.Select(MapToUnifiedDto)).ToList();
            var topMovers = new TopMoversDto
            {
                Gainers = GetTopMovers(combinedData, true),
                Losers = GetTopMovers(combinedData, false),
                Timestamp = DateTime.UtcNow
            };

            return new MarketOverviewDto
            {
                TotalSymbols = cryptoData.Count + stockData.Count,
                TrackedSymbols = cryptoData.Count + stockData.Count,
                ActiveMarkets = 2,
                OpenMarkets = 2,
                AssetClassSummary = new List<MyTrader.Core.Interfaces.AssetClassSummaryDto>
                {
                    new MyTrader.Core.Interfaces.AssetClassSummaryDto
                    {
                        Code = "CRYPTO",
                        Name = "Cryptocurrency",
                        SymbolCount = cryptoData.Count,
                        TrackedSymbolCount = cryptoData.Count,
                        TotalMarketCap = cryptoData.Sum(c => c.MarketCap),
                        AvgPriceChange24h = cryptoData.Any() ? cryptoData.Average(c => c.ChangePercent) : 0
                    },
                    new MyTrader.Core.Interfaces.AssetClassSummaryDto
                    {
                        Code = "STOCK",
                        Name = "Stocks",
                        SymbolCount = stockData.Count,
                        TrackedSymbolCount = stockData.Count,
                        TotalMarketCap = stockData.Sum(s => s.MarketCap),
                        AvgPriceChange24h = stockData.Any() ? stockData.Average(s => s.ChangePercent) : 0
                    }
                },
                TopMovers = topMovers,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market overview");
            return new MarketOverviewDto
            {
                TotalSymbols = 0,
                TrackedSymbols = 0,
                ActiveMarkets = 0,
                OpenMarkets = 0,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<AlpacaHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await TestConnectionAsync(cancellationToken);

            return new AlpacaHealthStatus
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "Healthy" : "Unhealthy",
                LastChecked = DateTime.UtcNow,
                RateLimit = GetRateLimitStatus(),
                CircuitBreaker = GetCircuitBreakerStatus(),
                Details = new Dictionary<string, string>
                {
                    ["Environment"] = _config.UsePaperTrading ? "Paper" : "Live",
                    ["CachingEnabled"] = _config.EnableCaching.ToString(),
                    ["RateLimitPerMinute"] = _config.RateLimitPerMinute.ToString(),
                    ["Implementation"] = "Simplified Mock Version"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health status");
            return new AlpacaHealthStatus
            {
                IsHealthy = false,
                Status = "Error",
                LastChecked = DateTime.UtcNow,
                Details = new Dictionary<string, string> { ["Error"] = ex.Message }
            };
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simulate connection test
            await Task.Delay(100, cancellationToken);
            return !string.IsNullOrEmpty(_config.PaperApiKey) || !string.IsNullOrEmpty(_config.LiveApiKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alpaca connection test failed");
            return false;
        }
    }

    public async Task<List<string>> GetAvailableSymbolsAsync(string? assetClass = null, CancellationToken cancellationToken = default)
    {
        var symbols = new List<string>();

        if (assetClass == null || assetClass.ToUpper() == "CRYPTO")
        {
            symbols.AddRange(_config.DefaultSymbols.Crypto);
        }

        if (assetClass == null || assetClass.ToUpper() == "STOCK")
        {
            symbols.AddRange(_config.DefaultSymbols.Stocks);
        }

        return symbols.Distinct().ToList();
    }

    public AlpacaRateLimitStatus GetRateLimitStatus()
    {
        lock (_rateLimitLock)
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            // Remove old timestamps
            while (_requestTimestamps.TryPeek(out var timestamp) && timestamp < oneMinuteAgo)
            {
                _requestTimestamps.TryDequeue(out _);
            }

            var requestsInLastMinute = _requestTimestamps.Count;
            var remaining = Math.Max(0, _config.RateLimitPerMinute - requestsInLastMinute);

            return new AlpacaRateLimitStatus
            {
                RequestsRemaining = remaining,
                ResetTime = now.AddMinutes(1),
                RequestsPerMinute = _config.RateLimitPerMinute,
                IsNearLimit = remaining < _config.RateLimitPerMinute * 0.1
            };
        }
    }

    public AlpacaCircuitBreakerStatus GetCircuitBreakerStatus()
    {
        lock (_circuitBreakerLock)
        {
            var isOpen = _consecutiveFailures >= _config.CircuitBreakerFailureThreshold;
            DateTime? nextRetryTime = null;

            if (isOpen && _lastFailureTime.HasValue)
            {
                nextRetryTime = _lastFailureTime.Value.AddSeconds(_config.CircuitBreakerRecoveryTimeSeconds);
                if (DateTime.UtcNow > nextRetryTime)
                {
                    isOpen = false;
                    _consecutiveFailures = 0;
                    _lastFailureTime = null;
                }
            }

            return new AlpacaCircuitBreakerStatus
            {
                IsOpen = isOpen,
                FailureCount = _consecutiveFailures,
                LastFailureTime = _lastFailureTime,
                NextRetryTime = nextRetryTime
            };
        }
    }

    #region Private Helper Methods

    private async Task<bool> CanMakeRequestAsync()
    {
        var rateLimitStatus = GetRateLimitStatus();
        var circuitBreakerStatus = GetCircuitBreakerStatus();

        if (circuitBreakerStatus.IsOpen)
        {
            _logger.LogWarning("Circuit breaker is open, cannot make request");
            return false;
        }

        if (rateLimitStatus.RequestsRemaining <= 0)
        {
            _logger.LogWarning("Rate limit exceeded, cannot make request");
            return false;
        }

        return true;
    }

    private async Task RecordSuccessfulRequestAsync()
    {
        lock (_rateLimitLock)
        {
            _requestTimestamps.Enqueue(DateTime.UtcNow);
        }

        lock (_circuitBreakerLock)
        {
            _consecutiveFailures = 0;
            _lastFailureTime = null;
        }
    }

    private async Task RecordFailedRequestAsync(Exception ex)
    {
        lock (_circuitBreakerLock)
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;
        }
    }

    private async Task<List<AlpacaCryptoDataDto>?> GetCachedCryptoDataAsync(List<string>? symbols)
    {
        if (!_config.EnableCaching) return null;

        symbols ??= _config.DefaultSymbols.Crypto;
        var cacheKey = $"crypto_data_{string.Join(",", symbols.OrderBy(s => s))}";

        return _cache.TryGetValue(cacheKey, out List<AlpacaCryptoDataDto>? cachedData) ? cachedData : null;
    }

    private async Task<List<AlpacaStockDataDto>?> GetCachedStockDataAsync(List<string>? symbols)
    {
        if (!_config.EnableCaching) return null;

        symbols ??= _config.DefaultSymbols.Stocks;
        var cacheKey = $"stock_data_{string.Join(",", symbols.OrderBy(s => s))}";

        return _cache.TryGetValue(cacheKey, out List<AlpacaStockDataDto>? cachedData) ? cachedData : null;
    }

    private List<AlpacaCryptoDataDto> GenerateMockCryptoData(List<string> symbols)
    {
        var random = new Random();
        return symbols.Select(symbol => new AlpacaCryptoDataDto
        {
            Symbol = symbol,
            Name = symbol.Replace("USD", "/USD"),
            Price = random.Next(1000, 50000),
            Change = random.Next(-1000, 1000),
            ChangePercent = (decimal)(random.NextDouble() * 10 - 5),
            Volume = random.Next(1000000, 10000000),
            High24h = random.Next(1000, 60000),
            Low24h = random.Next(500, 40000),
            MarketCap = random.Next(1000000000, 2000000000),
            LastUpdated = DateTime.UtcNow,
            AssetClass = "CRYPTO"
        }).ToList();
    }

    private List<AlpacaStockDataDto> GenerateMockStockData(List<string> symbols)
    {
        var random = new Random();
        return symbols.Select(symbol => new AlpacaStockDataDto
        {
            Symbol = symbol,
            Name = GetStockName(symbol),
            Price = random.Next(50, 500),
            Change = random.Next(-20, 20),
            ChangePercent = (decimal)(random.NextDouble() * 6 - 3),
            Volume = random.Next(1000000, 50000000),
            DayHigh = random.Next(60, 520),
            DayLow = random.Next(40, 480),
            OpenPrice = random.Next(45, 495),
            PreviousClose = random.Next(48, 502),
            MarketCap = (decimal)random.Next(500000000, 2147483647), // Use max int32 value
            LastUpdated = DateTime.UtcNow,
            AssetClass = "STOCK"
        }).ToList();
    }

    private string GetStockName(string symbol)
    {
        return symbol switch
        {
            "AAPL" => "Apple Inc.",
            "GOOGL" => "Alphabet Inc.",
            "MSFT" => "Microsoft Corporation",
            "TSLA" => "Tesla Inc.",
            "AMZN" => "Amazon.com Inc.",
            "NVDA" => "NVIDIA Corporation",
            "META" => "Meta Platforms Inc.",
            "NFLX" => "Netflix Inc.",
            "AMD" => "Advanced Micro Devices Inc.",
            "CRM" => "Salesforce Inc.",
            _ => symbol
        };
    }

    private MarketDataDto MapToUnifiedDto(AlpacaCryptoDataDto crypto)
    {
        return new MarketDataDto
        {
            Symbol = crypto.Symbol,
            Name = crypto.Name,
            Price = crypto.Price,
            Change = crypto.Change,
            ChangePercent = crypto.ChangePercent,
            Volume = crypto.Volume,
            High24h = crypto.High24h,
            Low24h = crypto.Low24h,
            LastUpdated = crypto.LastUpdated,
            AssetClass = crypto.AssetClass,
            MarketCap = crypto.MarketCap
        };
    }

    private MarketDataDto MapToUnifiedDto(AlpacaStockDataDto stock)
    {
        return new MarketDataDto
        {
            Symbol = stock.Symbol,
            Name = stock.Name,
            Price = stock.Price,
            Change = stock.Change,
            ChangePercent = stock.ChangePercent,
            Volume = stock.Volume,
            High24h = stock.DayHigh,
            Low24h = stock.DayLow,
            LastUpdated = stock.LastUpdated,
            AssetClass = stock.AssetClass,
            MarketCap = stock.MarketCap
        };
    }

    private List<SymbolSummaryDto> GetTopMovers(List<MarketDataDto> data, bool isGainers)
    {
        return data
            .Where(x => x.ChangePercent != 0)
            .OrderBy(x => isGainers ? -x.ChangePercent : x.ChangePercent)
            .Take(10)
            .Select(x => new SymbolSummaryDto
            {
                Id = Guid.NewGuid(),
                Ticker = x.Symbol,
                Display = x.Name
            })
            .ToList();
    }

    private int GetIntervalMinutes(string interval)
    {
        return interval.ToLower() switch
        {
            "1m" => 1,
            "5m" => 5,
            "15m" => 15,
            "1h" => 60,
            "1d" => 1440,
            _ => 1440
        };
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}