namespace MyTrader.Core.Interfaces;

/// <summary>
/// Market data router for categorizing and routing market data to appropriate channels
/// </summary>
public interface IMarketDataRouter
{
    /// <summary>
    /// Determine the market/exchange for a given symbol
    /// </summary>
    /// <param name="symbol">Symbol ticker (e.g., AAPL, BTCUSDT, THYAO)</param>
    /// <returns>Market identifier (NASDAQ, NYSE, BIST, BINANCE, etc.)</returns>
    string DetermineMarket(string symbol);

    /// <summary>
    /// Classify the asset class for a given symbol
    /// </summary>
    /// <param name="symbol">Symbol ticker</param>
    /// <returns>Asset class (STOCK, CRYPTO, FOREX, COMMODITY, etc.)</returns>
    string ClassifyAssetClass(string symbol);

    /// <summary>
    /// Get the SignalR group name for a symbol's market
    /// </summary>
    /// <param name="symbol">Symbol ticker</param>
    /// <returns>SignalR group name (e.g., "Market_NASDAQ", "Market_BINANCE")</returns>
    string GetMarketGroupName(string symbol);

    /// <summary>
    /// Get the SignalR group name for a symbol's asset class
    /// </summary>
    /// <param name="symbol">Symbol ticker</param>
    /// <returns>SignalR group name (e.g., "AssetClass_CRYPTO", "AssetClass_STOCK")</returns>
    string GetAssetClassGroupName(string symbol);

    /// <summary>
    /// Check if a market is currently open for trading
    /// </summary>
    /// <param name="market">Market identifier</param>
    /// <returns>True if market is open, false otherwise</returns>
    bool IsMarketOpen(string market);

    /// <summary>
    /// Get market status information
    /// </summary>
    /// <param name="market">Market identifier</param>
    /// <returns>Market status details</returns>
    MarketStatus GetMarketStatus(string market);

    /// <summary>
    /// Get all active markets
    /// </summary>
    /// <returns>List of active market identifiers</returns>
    List<string> GetActiveMarkets();

    /// <summary>
    /// Route market data to appropriate SignalR groups
    /// </summary>
    /// <param name="symbol">Symbol ticker</param>
    /// <returns>List of SignalR group names to broadcast to</returns>
    List<string> GetRoutingGroups(string symbol);
}

/// <summary>
/// Market status information
/// </summary>
public class MarketStatus
{
    public string Market { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public DateTime? NextOpen { get; set; }
    public DateTime? NextClose { get; set; }
    public string Status { get; set; } = "UNKNOWN"; // OPEN, CLOSED, PRE_MARKET, AFTER_HOURS
    public string TimeZone { get; set; } = "UTC";
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}
