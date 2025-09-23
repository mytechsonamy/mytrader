using MyTrader.Core.DTOs;
using MyTrader.Core.Models;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// Generic interface for all data providers (Binance, BIST, NASDAQ, etc.)
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// Provider unique identifier
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Provider display name
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Supported asset classes
    /// </summary>
    IEnumerable<string> SupportedAssetClasses { get; }

    /// <summary>
    /// Supported markets
    /// </summary>
    IEnumerable<string> SupportedMarkets { get; }

    /// <summary>
    /// Whether the provider is connected and operational
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Initialize the data provider
    /// </summary>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect and cleanup resources
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time market data for a single symbol
    /// </summary>
    Task<UnifiedMarketDataDto?> GetMarketDataAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time market data for multiple symbols
    /// </summary>
    Task<List<UnifiedMarketDataDto>> GetBatchMarketDataAsync(IEnumerable<string> tickers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical candlestick data
    /// </summary>
    Task<HistoricalMarketDataDto?> GetHistoricalDataAsync(
        string ticker,
        string interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market statistics and extended information
    /// </summary>
    Task<MarketStatisticsDto?> GetMarketStatisticsAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available symbols from this provider
    /// </summary>
    Task<List<Symbol>> GetAvailableSymbolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a symbol is supported by this provider
    /// </summary>
    bool IsSymbolSupported(string ticker, string? assetClass = null, string? market = null);

    /// <summary>
    /// Get provider-specific configuration
    /// </summary>
    Task<Dictionary<string, object>> GetProviderInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Health check for the provider
    /// </summary>
    Task<ComponentHealth> GetHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for real-time data streaming providers
/// </summary>
public interface IRealtimeDataProvider : IDataProvider
{
    /// <summary>
    /// Event for real-time price updates
    /// </summary>
    event EventHandler<MarketDataUpdateDto> OnPriceUpdate;

    /// <summary>
    /// Event for connection status changes
    /// </summary>
    event EventHandler<ConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

    /// <summary>
    /// Subscribe to real-time updates for symbols
    /// </summary>
    Task<bool> SubscribeAsync(IEnumerable<string> tickers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribe from real-time updates for symbols
    /// </summary>
    Task<bool> UnsubscribeAsync(IEnumerable<string> tickers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get currently subscribed symbols
    /// </summary>
    IEnumerable<string> GetSubscribedSymbols();
}

/// <summary>
/// Interface for cryptocurrency data providers (Binance, Coinbase, etc.)
/// </summary>
public interface ICryptoDataProvider : IRealtimeDataProvider
{
    /// <summary>
    /// Get trading pairs for a base currency
    /// </summary>
    Task<List<string>> GetTradingPairsAsync(string baseCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order book data
    /// </summary>
    Task<OrderBookDto?> GetOrderBookAsync(string ticker, int depth = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent trades
    /// </summary>
    Task<List<TradeDto>> GetRecentTradesAsync(string ticker, int limit = 50, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for stock market data providers (BIST, NASDAQ, NYSE, etc.)
/// </summary>
public interface IStockDataProvider : IDataProvider
{
    /// <summary>
    /// Get fundamental data for stocks
    /// </summary>
    Task<FundamentalDataDto?> GetFundamentalDataAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dividend information
    /// </summary>
    Task<List<DividendDto>> GetDividendsAsync(string ticker, DateTime? fromDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get earnings data
    /// </summary>
    Task<List<EarningsDto>> GetEarningsAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market hours information
    /// </summary>
    Task<MarketHoursDto> GetMarketHoursAsync(string marketCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Connection status changed event arguments
/// </summary>
public class ConnectionStatusChangedEventArgs : EventArgs
{
    public string ProviderId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Order book data DTO
/// </summary>
public class OrderBookDto
{
    public string Ticker { get; set; } = string.Empty;
    public List<OrderBookLevel> Bids { get; set; } = new();
    public List<OrderBookLevel> Asks { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Order book level
/// </summary>
public class OrderBookLevel
{
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}

/// <summary>
/// Trade data DTO
/// </summary>
public class TradeDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public DateTime Timestamp { get; set; }
    public string Side { get; set; } = string.Empty; // BUY or SELL
    public bool IsBuyerMaker { get; set; }
}

/// <summary>
/// Fundamental data for stocks
/// </summary>
public class FundamentalDataDto
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Sector { get; set; }
    public string? Country { get; set; }
    public decimal? MarketCap { get; set; }
    public decimal? PERatio { get; set; }
    public decimal? PEGRatio { get; set; }
    public decimal? PriceToBook { get; set; }
    public decimal? PriceToSales { get; set; }
    public decimal? DebtToEquity { get; set; }
    public decimal? ROE { get; set; }
    public decimal? ROA { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? Beta { get; set; }
    public long? SharesOutstanding { get; set; }
    public decimal? BookValue { get; set; }
    public decimal? Revenue { get; set; }
    public decimal? NetIncome { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Dividend information
/// </summary>
public class DividendDto
{
    public string Ticker { get; set; } = string.Empty;
    public DateTime ExDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Regular, Special, etc.
}

/// <summary>
/// Earnings information
/// </summary>
public class EarningsDto
{
    public string Ticker { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public string Quarter { get; set; } = string.Empty;
    public decimal? EPS { get; set; }
    public decimal? EPSEstimate { get; set; }
    public decimal? Revenue { get; set; }
    public decimal? RevenueEstimate { get; set; }
    public bool IsPremarket { get; set; }
}

/// <summary>
/// Market hours information
/// </summary>
public class MarketHoursDto
{
    public string MarketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? MarketOpen { get; set; }
    public DateTime? MarketClose { get; set; }
    public DateTime? PreMarketOpen { get; set; }
    public DateTime? PreMarketClose { get; set; }
    public DateTime? AfterHoursOpen { get; set; }
    public DateTime? AfterHoursClose { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public List<DateTime> Holidays { get; set; } = new();
}