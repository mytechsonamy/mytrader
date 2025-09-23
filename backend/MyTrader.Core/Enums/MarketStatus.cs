namespace MyTrader.Core.Enums;

/// <summary>
/// Defines the current status of a market
/// </summary>
public enum MarketStatus
{
    /// <summary>
    /// Market is currently open for trading
    /// </summary>
    OPEN = 1,

    /// <summary>
    /// Market is currently closed
    /// </summary>
    CLOSED = 2,

    /// <summary>
    /// Pre-market trading session (US markets)
    /// </summary>
    PRE_MARKET = 3,

    /// <summary>
    /// After-hours trading session (US markets)
    /// </summary>
    AFTER_HOURS = 4,

    /// <summary>
    /// Market is temporarily halted
    /// </summary>
    HALTED = 5,

    /// <summary>
    /// Market status is unknown or unavailable
    /// </summary>
    UNKNOWN = 6
}