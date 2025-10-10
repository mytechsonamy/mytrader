using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.Configuration;
using MyTrader.Core.Interfaces;

namespace MyTrader.Services.Market;

/// <summary>
/// Factory for creating market data providers based on configuration
/// Supports dynamic provider selection and fallback logic
/// </summary>
public class MarketDataProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly MarketDataProvidersConfiguration _configuration;
    private readonly Dictionary<string, IMarketDataProvider> _providerCache;
    private readonly object _cacheLock = new();

    public MarketDataProviderFactory(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IOptions<MarketDataProvidersConfiguration> configuration)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _configuration = configuration.Value;
        _providerCache = new Dictionary<string, IMarketDataProvider>();
    }

    /// <summary>
    /// Get provider for a specific market
    /// </summary>
    /// <param name="market">Market code (e.g., BIST, NASDAQ, NYSE)</param>
    /// <returns>Market data provider instance</returns>
    public IMarketDataProvider GetProvider(string market)
    {
        lock (_cacheLock)
        {
            // Check cache first
            if (_providerCache.TryGetValue(market, out var cachedProvider))
            {
                return cachedProvider;
            }

            // Get configuration for this market
            if (!_configuration.Providers.TryGetValue(market, out var config))
            {
                throw new InvalidOperationException($"No provider configuration found for market: {market}");
            }

            if (!config.Enabled)
            {
                throw new InvalidOperationException($"Provider for market {market} is disabled");
            }

            // Create provider instance
            var provider = CreateProviderInstance(config.Provider, market);

            // Cache the provider
            _providerCache[market] = provider;

            return provider;
        }
    }

    /// <summary>
    /// Get provider with fallback support
    /// </summary>
    /// <param name="market">Market code</param>
    /// <returns>Primary provider or fallback if primary fails</returns>
    public async Task<IMarketDataProvider> GetProviderWithFallbackAsync(string market)
    {
        var primaryProvider = GetProvider(market);

        // Check if primary provider is available
        if (await primaryProvider.IsAvailableAsync())
        {
            return primaryProvider;
        }

        // Try fallback provider if configured
        if (_configuration.Providers.TryGetValue(market, out var config) && 
            !string.IsNullOrEmpty(config.FallbackProvider))
        {
            var logger = _loggerFactory.CreateLogger<MarketDataProviderFactory>();
            logger.LogWarning(
                "Primary provider {Primary} for {Market} is unavailable, using fallback {Fallback}",
                config.Provider, market, config.FallbackProvider);

            var fallbackProvider = CreateProviderInstance(config.FallbackProvider, market);
            
            if (await fallbackProvider.IsAvailableAsync())
            {
                return fallbackProvider;
            }

            logger.LogError(
                "Both primary and fallback providers are unavailable for {Market}",
                market);
        }

        // Return primary provider even if unavailable (will handle errors in provider)
        return primaryProvider;
    }

    /// <summary>
    /// Get all enabled providers
    /// </summary>
    /// <returns>Dictionary of market to provider mappings</returns>
    public Dictionary<string, IMarketDataProvider> GetAllProviders()
    {
        var providers = new Dictionary<string, IMarketDataProvider>();

        foreach (var (market, config) in _configuration.Providers)
        {
            if (config.Enabled)
            {
                try
                {
                    providers[market] = GetProvider(market);
                }
                catch (Exception ex)
                {
                    var logger = _loggerFactory.CreateLogger<MarketDataProviderFactory>();
                    logger.LogError(ex, "Failed to create provider for market {Market}", market);
                }
            }
        }

        return providers;
    }

    /// <summary>
    /// Check if a market has a configured provider
    /// </summary>
    /// <param name="market">Market code</param>
    /// <returns>True if provider is configured and enabled</returns>
    public bool HasProvider(string market)
    {
        return _configuration.Providers.TryGetValue(market, out var config) && config.Enabled;
    }

    /// <summary>
    /// Get provider configuration for a market
    /// </summary>
    /// <param name="market">Market code</param>
    /// <returns>Provider configuration or null if not found</returns>
    public MarketProviderConfig? GetProviderConfig(string market)
    {
        return _configuration.Providers.TryGetValue(market, out var config) ? config : null;
    }

    private IMarketDataProvider CreateProviderInstance(string providerName, string market)
    {
        return providerName.ToLower() switch
        {
            "yahoofinance" => new YahooFinanceProvider(
                _loggerFactory.CreateLogger<YahooFinanceProvider>(),
                _httpClientFactory,
                market),

            "binance" => throw new NotImplementedException("Binance provider should be created separately"),

            "alpaca" => throw new NotImplementedException("Alpaca provider not yet implemented"),

            _ => throw new InvalidOperationException($"Unknown provider: {providerName}")
        };
    }

    /// <summary>
    /// Clear provider cache (useful for testing or configuration changes)
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _providerCache.Clear();
        }
    }
}
