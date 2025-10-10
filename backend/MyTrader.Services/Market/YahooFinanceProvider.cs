using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs;
using MyTrader.Core.Enums;
using MyTrader.Core.Interfaces;

namespace MyTrader.Services.Market;

/// <summary>
/// Yahoo Finance data provider for stock market data (BIST, NASDAQ, NYSE)
/// Provides 15-minute delayed stock prices
/// </summary>
public class YahooFinanceProvider : IMarketDataProvider
{
    private readonly ILogger<YahooFinanceProvider> _logger;
    private readonly HttpClient _httpClient;
    private const string YahooFinanceApiBaseUrl = "https://query1.finance.yahoo.com/v8/finance/chart/";
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    public string ProviderName => "Yahoo Finance";
    public string SupportedMarket { get; }
    public TimeSpan UpdateInterval => TimeSpan.FromMinutes(1);

    public YahooFinanceProvider(
        ILogger<YahooFinanceProvider> logger,
        IHttpClientFactory httpClientFactory,
        string supportedMarket)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("YahooFinance");
        SupportedMarket = supportedMarket;
        
        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MyTrader/1.0");
    }

    public async Task<List<UnifiedMarketDataDto>> GetPricesAsync(List<string> symbols, CancellationToken cancellationToken = default)
    {
        var results = new List<UnifiedMarketDataDto>();

        if (symbols == null || !symbols.Any())
        {
            _logger.LogWarning("No symbols provided to GetPricesAsync");
            return results;
        }

        _logger.LogInformation("Fetching prices for {Count} symbols from Yahoo Finance", symbols.Count);

        // Process symbols in parallel with rate limiting
        var semaphore = new SemaphoreSlim(5); // Max 5 concurrent requests
        var tasks = symbols.Select(async symbol =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await GetSinglePriceWithRetryAsync(symbol, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var fetchedData = await Task.WhenAll(tasks);
        results.AddRange(fetchedData.Where(d => d != null)!);

        _logger.LogInformation("Successfully fetched {Count}/{Total} prices from Yahoo Finance", 
            results.Count, symbols.Count);

        return results;
    }

    private async Task<UnifiedMarketDataDto?> GetSinglePriceWithRetryAsync(string symbol, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await GetSinglePriceAsync(symbol, cancellationToken);
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} failed for symbol {Symbol}. Retrying...", 
                    attempt, MaxRetries, symbol);
                await Task.Delay(RetryDelayMs * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch price for symbol {Symbol} after {MaxRetries} attempts", 
                    symbol, MaxRetries);
                return null;
            }
        }

        return null;
    }

    private async Task<UnifiedMarketDataDto?> GetSinglePriceAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            // Format symbol for Yahoo Finance (e.g., AAPL, GARAN.IS for BIST)
            var yahooSymbol = FormatSymbolForYahoo(symbol);
            var url = $"{YahooFinanceApiBaseUrl}{yahooSymbol}?interval=1d&range=1d";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo Finance API returned {StatusCode} for symbol {Symbol}", 
                    response.StatusCode, symbol);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<YahooFinanceResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Chart?.Result == null || !data.Chart.Result.Any())
            {
                _logger.LogWarning("No data returned from Yahoo Finance for symbol {Symbol}", symbol);
                return null;
            }

            var result = data.Chart.Result[0];
            var quote = result.Indicators?.Quote?.FirstOrDefault();
            var meta = result.Meta;

            if (quote == null || meta == null)
            {
                _logger.LogWarning("Invalid data structure from Yahoo Finance for symbol {Symbol}", symbol);
                return null;
            }

            // Get the latest values
            var latestIndex = quote.Close?.Count - 1 ?? -1;
            if (latestIndex < 0)
            {
                _logger.LogWarning("No price data available for symbol {Symbol}", symbol);
                return null;
            }

            var currentPrice = quote.Close?[latestIndex];
            var previousClose = meta.ChartPreviousClose ?? meta.PreviousClose;
            var openPrice = quote.Open?[latestIndex];
            var highPrice = quote.High?[latestIndex];
            var lowPrice = quote.Low?[latestIndex];
            var volume = quote.Volume?[latestIndex];

            if (!currentPrice.HasValue)
            {
                _logger.LogWarning("No current price available for symbol {Symbol}", symbol);
                return null;
            }

            // Calculate price change using standard financial formula
            decimal? priceChange = null;
            decimal? priceChangePercent = null;
            if (previousClose.HasValue && previousClose.Value > 0)
            {
                priceChange = currentPrice.Value - previousClose.Value;
                // ✅ FIXED: Use previousClose as denominator (standard financial formula)
                // Formula: ((current - previousClose) / previousClose) * 100
                priceChangePercent = (priceChange.Value / previousClose.Value) * 100;
            }

            var marketData = new UnifiedMarketDataDto
            {
                Ticker = symbol,
                AssetClassCode = "STOCK",
                MarketCode = SupportedMarket,
                Price = currentPrice,
                PreviousClose = previousClose,
                OpenPrice = openPrice,
                HighPrice = highPrice,
                LowPrice = lowPrice,
                Volume = volume,
                PriceChange = priceChange,
                PriceChangePercent = priceChangePercent,
                DataTimestamp = DateTimeOffset.FromUnixTimeSeconds(result.Meta.RegularMarketTime ?? 0).UtcDateTime,
                ReceivedTimestamp = DateTime.UtcNow,
                DataProvider = ProviderName,
                IsRealTime = false,
                DataDelayMinutes = 15,
                MarketStatus = meta.MarketState ?? "UNKNOWN",
                IsMarketOpen = meta.MarketState?.ToUpper() == "REGULAR",
                Currency = meta.Currency ?? "USD",
                PricePrecision = 2,
                QuantityPrecision = 0
            };

            return marketData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for symbol {Symbol}", symbol);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Yahoo Finance response for symbol {Symbol}", symbol);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching price for symbol {Symbol}", symbol);
            throw;
        }
    }

    private string FormatSymbolForYahoo(string symbol)
    {
        // BIST symbols need .IS suffix (e.g., GARAN.IS)
        if (SupportedMarket == "BIST" && !symbol.EndsWith(".IS"))
        {
            return $"{symbol}.IS";
        }

        // NASDAQ and NYSE symbols are used as-is
        return symbol;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Test with a known symbol
            var testSymbol = SupportedMarket == "BIST" ? "GARAN.IS" : "AAPL";
            var url = $"{YahooFinanceApiBaseUrl}{testSymbol}?interval=1d&range=1d";

            var response = await _httpClient.GetAsync(url);
            var isAvailable = response.IsSuccessStatusCode;

            _logger.LogInformation("Yahoo Finance availability check: {Status}", 
                isAvailable ? "Available" : "Unavailable");

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yahoo Finance availability check failed");
            return false;
        }
    }

    public async Task<MarketStatusDto> GetMarketStatusAsync(string market)
    {
        try
        {
            // Use a representative symbol to check market status
            var testSymbol = market switch
            {
                "BIST" => "GARAN.IS",
                "NASDAQ" => "AAPL",
                "NYSE" => "IBM",
                _ => "AAPL"
            };

            var url = $"{YahooFinanceApiBaseUrl}{testSymbol}?interval=1d&range=1d";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return CreateUnknownMarketStatus(market);
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<YahooFinanceResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var meta = data?.Chart?.Result?.FirstOrDefault()?.Meta;
            if (meta == null)
            {
                return CreateUnknownMarketStatus(market);
            }

            var isOpen = meta.MarketState?.ToUpper() == "REGULAR";
            var status = meta.MarketState ?? "UNKNOWN";

            return new MarketStatusDto
            {
                Code = market,
                Name = market,
                Status = status,
                IsOpen = isOpen,
                StatusUpdatedAt = DateTime.UtcNow,
                IsActive = true,
                Timezone = meta.ExchangeTimezoneName ?? "UTC",
                LastUpdate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get market status for {Market}", market);
            return CreateUnknownMarketStatus(market);
        }
    }

    private MarketStatusDto CreateUnknownMarketStatus(string market)
    {
        return new MarketStatusDto
        {
            Code = market,
            Name = market,
            Status = "UNKNOWN",
            IsOpen = false,
            StatusUpdatedAt = DateTime.UtcNow,
            IsActive = true,
            Timezone = "UTC",
            LastUpdate = DateTime.UtcNow
        };
    }
}

#region Yahoo Finance API Response Models

internal class YahooFinanceResponse
{
    public YahooChart? Chart { get; set; }
}

internal class YahooChart
{
    public List<YahooResult>? Result { get; set; }
    public YahooError? Error { get; set; }
}

internal class YahooResult
{
    public YahooMeta? Meta { get; set; }
    public List<long>? Timestamp { get; set; }
    public YahooIndicators? Indicators { get; set; }
}

internal class YahooMeta
{
    public string? Currency { get; set; }
    public string? Symbol { get; set; }
    public string? ExchangeName { get; set; }
    public string? InstrumentType { get; set; }
    public long? FirstTradeDate { get; set; }
    public long? RegularMarketTime { get; set; }
    public int? Gmtoffset { get; set; }
    public string? Timezone { get; set; }
    public string? ExchangeTimezoneName { get; set; }
    public decimal? RegularMarketPrice { get; set; }
    public decimal? ChartPreviousClose { get; set; }
    public decimal? PreviousClose { get; set; }
    public int? Scale { get; set; }
    public int? PriceHint { get; set; }
    public string? DataGranularity { get; set; }
    public string? Range { get; set; }
    public string? MarketState { get; set; }
}

internal class YahooIndicators
{
    public List<YahooQuote>? Quote { get; set; }
}

internal class YahooQuote
{
    public List<decimal?>? Open { get; set; }
    public List<decimal?>? High { get; set; }
    public List<decimal?>? Low { get; set; }
    public List<decimal?>? Close { get; set; }
    public List<long?>? Volume { get; set; }
}

internal class YahooError
{
    public string? Code { get; set; }
    public string? Description { get; set; }
}

#endregion
