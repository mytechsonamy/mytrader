using System;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// Service for determining market hours, trading schedules, and timezone-aware status for different exchanges
/// Complements IMarketStatusService (database-driven) with real-time calculation logic
/// </summary>
public interface IMarketHoursService
{
    /// <summary>
    /// Gets the current market status for a specific exchange
    /// </summary>
    /// <param name="exchange">The exchange to check status for</param>
    /// <returns>Detailed market status information</returns>
    MarketHoursInfo GetMarketStatus(Exchange exchange);

    /// <summary>
    /// Gets the next opening time for a specific exchange
    /// </summary>
    /// <param name="exchange">The exchange to check</param>
    /// <returns>Next opening time in UTC, null if always open (crypto)</returns>
    DateTime? GetNextOpenTime(Exchange exchange);

    /// <summary>
    /// Gets the next closing time for a specific exchange
    /// </summary>
    /// <param name="exchange">The exchange to check</param>
    /// <returns>Next closing time in UTC, null if always open (crypto)</returns>
    DateTime? GetNextCloseTime(Exchange exchange);

    /// <summary>
    /// Checks if the market is currently open for trading
    /// </summary>
    /// <param name="exchange">The exchange to check</param>
    /// <returns>True if market is open, false otherwise</returns>
    bool IsMarketOpen(Exchange exchange);

    /// <summary>
    /// Gets the market status for all supported exchanges
    /// </summary>
    /// <returns>Dictionary of exchange to market status information</returns>
    Dictionary<Exchange, MarketHoursInfo> GetAllMarketStatuses();

    /// <summary>
    /// Determines the exchange from a given symbol
    /// </summary>
    /// <param name="symbol">The trading symbol</param>
    /// <returns>The exchange for the symbol</returns>
    Exchange GetExchangeForSymbol(string symbol);

    /// <summary>
    /// Checks if a specific date is a holiday for the given exchange
    /// </summary>
    /// <param name="exchange">The exchange to check</param>
    /// <param name="date">The date to check</param>
    /// <returns>True if holiday, false otherwise</returns>
    bool IsHoliday(Exchange exchange, DateTime date);
}

/// <summary>
/// Represents the current status of a market with timezone-aware information
/// </summary>
public class MarketHoursInfo
{
    /// <summary>
    /// The exchange this status is for
    /// </summary>
    public Exchange Exchange { get; set; }

    /// <summary>
    /// Current state of the market
    /// </summary>
    public Enums.MarketStatus State { get; set; }

    /// <summary>
    /// When this status was last checked
    /// </summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// Next time the market opens (null for 24/7 markets)
    /// </summary>
    public DateTime? NextOpenTime { get; set; }

    /// <summary>
    /// Next time the market closes (null for 24/7 markets)
    /// </summary>
    public DateTime? NextCloseTime { get; set; }

    /// <summary>
    /// Current time in the exchange's timezone
    /// </summary>
    public DateTime LocalTime { get; set; }

    /// <summary>
    /// The timezone of the exchange
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// If closed, the reason (weekend, holiday, after-hours, etc.)
    /// </summary>
    public string? ClosureReason { get; set; }

    /// <summary>
    /// Trading hours in local time (e.g., "09:30-16:00 EST")
    /// </summary>
    public string TradingHours { get; set; } = string.Empty;

    /// <summary>
    /// Pre-market hours if applicable
    /// </summary>
    public string? PreMarketHours { get; set; }

    /// <summary>
    /// Post-market hours if applicable
    /// </summary>
    public string? PostMarketHours { get; set; }
}

/// <summary>
/// Supported trading exchanges
/// </summary>
public enum Exchange
{
    /// <summary>
    /// Borsa Istanbul (Turkey)
    /// </summary>
    BIST,

    /// <summary>
    /// NASDAQ Stock Exchange (US)
    /// </summary>
    NASDAQ,

    /// <summary>
    /// New York Stock Exchange (US)
    /// </summary>
    NYSE,

    /// <summary>
    /// Cryptocurrency exchanges (24/7)
    /// </summary>
    CRYPTO,

    /// <summary>
    /// Unknown or unspecified exchange
    /// </summary>
    UNKNOWN
}