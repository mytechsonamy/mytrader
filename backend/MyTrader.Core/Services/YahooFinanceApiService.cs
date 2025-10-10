using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.DTOs;
using MyTrader.Core.Models;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTrader.Core.Services;

/// <summary>
/// Yahoo Finance API service with rate limiting and error handling
/// Supports BIST, Crypto, NASDAQ, NYSE symbols
/// </summary>
public class YahooFinanceApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooFinanceApiService> _logger;
    private readonly YahooFinanceConfiguration _config;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly object _rateLimitLock = new();

    public YahooFinanceApiService(
        HttpClient httpClient,
        ILogger<YahooFinanceApiService> logger,
        IOptions<YahooFinanceConfiguration> configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = configuration.Value;
        _rateLimitSemaphore = new SemaphoreSlim(_config.MaxConcurrentRequests, _config.MaxConcurrentRequests);

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgent);
    }

    /// <summary>
    /// Get historical market data for a symbol from Yahoo Finance
    /// </summary>
    public async Task<YahooFinanceResult<List<HistoricalMarketData>>> GetHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string market = "NASDAQ",
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var yahooSymbol = ConvertToYahooSymbol(symbol, market);
            var url = BuildHistoricalDataUrl(yahooSymbol, startDate, endDate);

            _logger.LogDebug("Fetching data for {Symbol} from {StartDate} to {EndDate}",
                yahooSymbol, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Yahoo Finance API error for {Symbol}: {StatusCode} - {Error}",
                    yahooSymbol, response.StatusCode, errorContent);

                return new YahooFinanceResult<List<HistoricalMarketData>>
                {
                    Success = false,
                    ErrorMessage = $"API error {response.StatusCode}: {errorContent}",
                    Symbol = symbol
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var historicalData = ParseHistoricalDataResponse(content, symbol, market);

            _logger.LogDebug("Successfully fetched {Count} records for {Symbol}",
                historicalData.Count, yahooSymbol);

            return new YahooFinanceResult<List<HistoricalMarketData>>
            {
                Success = true,
                Data = historicalData,
                Symbol = symbol,
                RecordsCount = historicalData.Count
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning("Request timeout for {Symbol}: {Message}", symbol, ex.Message);
            return new YahooFinanceResult<List<HistoricalMarketData>>
            {
                Success = false,
                ErrorMessage = "Request timeout",
                Symbol = symbol,
                IsRetryable = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data for {Symbol}", symbol);
            return new YahooFinanceResult<List<HistoricalMarketData>>
            {
                Success = false,
                ErrorMessage = ex.Message,
                Symbol = symbol,
                IsRetryable = IsRetryableError(ex)
            };
        }
    }

    /// <summary>
    /// Get intraday market data for a symbol from Yahoo Finance with specified interval
    /// </summary>
    public async Task<YahooFinanceResult<List<HistoricalMarketData>>> GetIntradayDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string interval = "5m",
        string market = "NASDAQ",
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var yahooSymbol = ConvertToYahooSymbol(symbol, market);
            var url = BuildIntradayDataUrl(yahooSymbol, startDate, endDate, interval);

            _logger.LogDebug("Fetching {Interval} data for {Symbol} from {StartDate} to {EndDate}",
                interval, yahooSymbol, startDate.ToString("yyyy-MM-dd HH:mm"), endDate.ToString("yyyy-MM-dd HH:mm"));

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Yahoo Finance API error for {Symbol} ({Interval}): {StatusCode} - {Error}",
                    yahooSymbol, interval, response.StatusCode, errorContent);

                return new YahooFinanceResult<List<HistoricalMarketData>>
                {
                    Success = false,
                    ErrorMessage = $"API error {response.StatusCode}: {errorContent}",
                    Symbol = symbol
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var intradayData = ParseIntradayDataResponse(content, symbol, market, interval);

            _logger.LogDebug("Successfully fetched {Count} {Interval} records for {Symbol}",
                intradayData.Count, interval, yahooSymbol);

            return new YahooFinanceResult<List<HistoricalMarketData>>
            {
                Success = true,
                Data = intradayData,
                Symbol = symbol,
                RecordsCount = intradayData.Count
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning("Request timeout for {Symbol} ({Interval}): {Message}", symbol, interval, ex.Message);
            return new YahooFinanceResult<List<HistoricalMarketData>>
            {
                Success = false,
                ErrorMessage = "Request timeout",
                Symbol = symbol,
                IsRetryable = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Interval} data for {Symbol}", interval, symbol);
            return new YahooFinanceResult<List<HistoricalMarketData>>
            {
                Success = false,
                ErrorMessage = ex.Message,
                Symbol = symbol,
                IsRetryable = IsRetryableError(ex)
            };
        }
    }

    /// <summary>
    /// Get latest quote for real-time validation
    /// </summary>
    public async Task<YahooFinanceResult<decimal?>> GetLatestPriceAsync(
        string symbol,
        string market = "NASDAQ",
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var yahooSymbol = ConvertToYahooSymbol(symbol, market);
            var url = BuildQuoteUrl(yahooSymbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new YahooFinanceResult<decimal?>
                {
                    Success = false,
                    ErrorMessage = $"API error {response.StatusCode}",
                    Symbol = symbol
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var price = ParseQuoteResponse(content);

            return new YahooFinanceResult<decimal?>
            {
                Success = true,
                Data = price,
                Symbol = symbol
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quote for {Symbol}", symbol);
            return new YahooFinanceResult<decimal?>
            {
                Success = false,
                ErrorMessage = ex.Message,
                Symbol = symbol
            };
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimitSemaphore.WaitAsync(cancellationToken);

        lock (_rateLimitLock)
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minDelay = TimeSpan.FromMilliseconds(_config.MinRequestIntervalMs);

            if (timeSinceLastRequest < minDelay)
            {
                var delayNeeded = minDelay - timeSinceLastRequest;
                Task.Delay(delayNeeded, cancellationToken).Wait(cancellationToken);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
    }

    private string ConvertToYahooSymbol(string symbol, string market)
    {
        return market.ToUpper() switch
        {
            "BIST" => $"{symbol}.IS",  // BIST symbols need .IS suffix
            "CRYPTO" => symbol.EndsWith("-USD") ? symbol : $"{symbol}-USD", // Crypto pairs - check if already has -USD
            "NASDAQ" or "NYSE" => symbol, // US symbols as-is
            _ => symbol
        };
    }

    private string BuildHistoricalDataUrl(string yahooSymbol, DateTime startDate, DateTime endDate)
    {
        var start = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
        var end = ((DateTimeOffset)endDate).ToUnixTimeSeconds();

        return $"https://query1.finance.yahoo.com/v7/finance/download/{yahooSymbol}?" +
               $"period1={start}&period2={end}&interval=1d&events=history&includeAdjustedClose=true";
    }

    private string BuildIntradayDataUrl(string yahooSymbol, DateTime startDate, DateTime endDate, string interval)
    {
        var start = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
        var end = ((DateTimeOffset)endDate).ToUnixTimeSeconds();

        return $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooSymbol}?" +
               $"period1={start}&period2={end}&interval={interval}&includePrePost=false";
    }

    private string BuildQuoteUrl(string yahooSymbol)
    {
        return $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooSymbol}";
    }

    private List<HistoricalMarketData> ParseHistoricalDataResponse(string csvContent, string originalSymbol, string market)
    {
        var records = new List<HistoricalMarketData>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2) return records; // No data or just header

        // Skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var values = lines[i].Split(',');
                if (values.Length < 7) continue; // Need at least Date,Open,High,Low,Close,Adj Close,Volume

                var record = new HistoricalMarketData
                {
                    SymbolTicker = originalSymbol,
                    DataSource = "YAHOO",
                    MarketCode = market,
                    TradeDate = DateOnly.Parse(values[0]),
                    Timeframe = "DAILY",
                    OpenPrice = ParseDecimal(values[1]),
                    HighPrice = ParseDecimal(values[2]),
                    LowPrice = ParseDecimal(values[3]),
                    ClosePrice = ParseDecimal(values[4]),
                    AdjustedClosePrice = ParseDecimal(values[5]),
                    Volume = ParseDecimal(values[6]),
                    Currency = GetCurrencyForMarket(market),
                    SourcePriority = GetSourcePriority(market),
                    DataCollectedAt = DateTime.UtcNow
                };

                // ✅ FIX: Calculate derived fields using previous record's close as previous close
                if (record.ClosePrice.HasValue)
                {
                    // Use previous record's close as this record's previous close
                    if (records.Count > 0 && records[^1].ClosePrice.HasValue)
                    {
                        record.PreviousClose = records[^1].ClosePrice.Value;
                        record.PriceChange = record.ClosePrice.Value - record.PreviousClose.Value;
                        record.PriceChangePercent = record.PreviousClose.Value != 0 ?
                            (record.PriceChange / record.PreviousClose.Value * 100) : 0;
                    }
                    else
                    {
                        // First record: no previous close available
                        record.PreviousClose = null;
                        record.PriceChange = 0;
                        record.PriceChangePercent = 0;
                    }
                }

                // Set data quality score
                record.DataQualityScore = CalculateDataQualityScore(record);

                records.Add(record);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to parse line {LineNumber}: {Line}. Error: {Error}",
                    i, lines[i], ex.Message);
            }
        }

        return records;
    }

    private List<HistoricalMarketData> ParseIntradayDataResponse(string jsonContent, string originalSymbol, string market, string interval)
    {
        var records = new List<HistoricalMarketData>();

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var chart = doc.RootElement.GetProperty("chart");

            if (!chart.GetProperty("result").EnumerateArray().Any())
            {
                _logger.LogWarning("No chart result data found for {Symbol}", originalSymbol);
                return records;
            }

            var result = chart.GetProperty("result")[0];
            var timestamps = result.GetProperty("timestamp").EnumerateArray().Select(t => t.GetInt64()).ToArray();
            var indicators = result.GetProperty("indicators");
            var quote = indicators.GetProperty("quote")[0];

            var opens = quote.TryGetProperty("open", out var openProp) ? openProp.EnumerateArray().Select(ParseJsonDecimal).ToArray() : new decimal?[timestamps.Length];
            var highs = quote.TryGetProperty("high", out var highProp) ? highProp.EnumerateArray().Select(ParseJsonDecimal).ToArray() : new decimal?[timestamps.Length];
            var lows = quote.TryGetProperty("low", out var lowProp) ? lowProp.EnumerateArray().Select(ParseJsonDecimal).ToArray() : new decimal?[timestamps.Length];
            var closes = quote.TryGetProperty("close", out var closeProp) ? closeProp.EnumerateArray().Select(ParseJsonDecimal).ToArray() : new decimal?[timestamps.Length];
            var volumes = quote.TryGetProperty("volume", out var volumeProp) ? volumeProp.EnumerateArray().Select(ParseJsonDecimal).ToArray() : new decimal?[timestamps.Length];

            for (int i = 0; i < timestamps.Length; i++)
            {
                var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).DateTime;

                var record = new HistoricalMarketData
                {
                    SymbolTicker = originalSymbol,
                    DataSource = "YAHOO",
                    MarketCode = market,
                    TradeDate = DateOnly.FromDateTime(timestamp),
                    Timestamp = timestamp,
                    Timeframe = GetTimeframeFromInterval(interval),
                    OpenPrice = opens[i],
                    HighPrice = highs[i],
                    LowPrice = lows[i],
                    ClosePrice = closes[i],
                    Volume = volumes[i],
                    Currency = GetCurrencyForMarket(market),
                    SourcePriority = GetSourcePriority(market),
                    DataCollectedAt = DateTime.UtcNow
                };

                // ✅ FIX: Calculate derived fields using previous record's close as previous close
                if (record.ClosePrice.HasValue)
                {
                    // Use previous record's close as this record's previous close
                    if (records.Count > 0 && records[^1].ClosePrice.HasValue)
                    {
                        record.PreviousClose = records[^1].ClosePrice.Value;
                        record.PriceChange = record.ClosePrice.Value - record.PreviousClose.Value;
                        record.PriceChangePercent = record.PreviousClose.Value != 0 ?
                            (record.PriceChange / record.PreviousClose.Value * 100) : 0;
                    }
                    else
                    {
                        // First record: no previous close available
                        record.PreviousClose = null;
                        record.PriceChange = 0;
                        record.PriceChangePercent = 0;
                    }
                }

                // Set data quality score
                record.DataQualityScore = CalculateDataQualityScore(record);

                records.Add(record);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse intraday JSON response for {Symbol}", originalSymbol);
        }

        return records;
    }

    private static decimal? ParseJsonDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (element.ValueKind == JsonValueKind.Number)
            return (decimal)element.GetDouble();

        return null;
    }

    private static string GetTimeframeFromInterval(string interval)
    {
        return interval switch
        {
            "1m" => "1MIN",
            "2m" => "2MIN",
            "5m" => "5MIN",
            "15m" => "15MIN",
            "30m" => "30MIN",
            "60m" or "1h" => "1HOUR",
            "1d" => "DAILY",
            "1wk" => "WEEKLY",
            "1mo" => "MONTHLY",
            _ => interval.ToUpper()
        };
    }

    private decimal? ParseQuoteResponse(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var chart = doc.RootElement.GetProperty("chart");
            var result = chart.GetProperty("result")[0];
            var meta = result.GetProperty("meta");

            return (decimal)meta.GetProperty("regularMarketPrice").GetDouble();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse quote response: {Error}", ex.Message);
            return null;
        }
    }

    private static decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        return decimal.TryParse(value, out var result) ? result : null;
    }

    private static string GetCurrencyForMarket(string market)
    {
        return market.ToUpper() switch
        {
            "BIST" => "TRY",
            "CRYPTO" => "USD",
            "NASDAQ" or "NYSE" => "USD",
            _ => "USD"
        };
    }

    private static int GetSourcePriority(string market)
    {
        return market.ToUpper() switch
        {
            "BIST" => 5,     // Yahoo might not be best for BIST
            "CRYPTO" => 3,   // Good for crypto
            "NASDAQ" or "NYSE" => 2, // Excellent for US stocks
            _ => 10
        };
    }

    private static int CalculateDataQualityScore(HistoricalMarketData record)
    {
        var score = 0;

        // Basic OHLCV completeness (40 points)
        if (record.OpenPrice.HasValue) score += 8;
        if (record.HighPrice.HasValue) score += 8;
        if (record.LowPrice.HasValue) score += 8;
        if (record.ClosePrice.HasValue) score += 8;
        if (record.Volume.HasValue) score += 8;

        // Adjusted close (20 points)
        if (record.AdjustedClosePrice.HasValue) score += 20;

        // Data consistency checks (40 points)
        if (record.HighPrice >= record.LowPrice) score += 10;
        if (record.OpenPrice <= record.HighPrice && record.OpenPrice >= record.LowPrice) score += 10;
        if (record.ClosePrice <= record.HighPrice && record.ClosePrice >= record.LowPrice) score += 10;
        if (record.Volume >= 0) score += 10;

        return Math.Min(score, 100);
    }

    private static bool IsRetryableError(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is SocketException;
    }

    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
    }
}

/// <summary>
/// Configuration for Yahoo Finance API service
/// </summary>
public class YahooFinanceConfiguration
{
    public int MaxConcurrentRequests { get; set; } = 5;
    public int MinRequestIntervalMs { get; set; } = 200; // 5 requests per second max
    public int RequestTimeoutSeconds { get; set; } = 30;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

/// <summary>
/// Result wrapper for Yahoo Finance API operations
/// </summary>
public class YahooFinanceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int RecordsCount { get; set; }
    public bool IsRetryable { get; set; }
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
}