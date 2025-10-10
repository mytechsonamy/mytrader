using System.Text.Json.Serialization;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Base message from Alpaca WebSocket
/// </summary>
public class AlpacaBaseMessage
{
    /// <summary>
    /// Message type: 't' (trade), 'q' (quote), 'b' (bar), 'success', 'error', 'subscription'
    /// </summary>
    [JsonPropertyName("T")]
    public string T { get; set; } = string.Empty;
}

/// <summary>
/// Alpaca authentication success message
/// </summary>
public class AlpacaAuthSuccessMessage : AlpacaBaseMessage
{
    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Alpaca subscription confirmation message
/// </summary>
public class AlpacaSubscriptionMessage : AlpacaBaseMessage
{
    [JsonPropertyName("trades")]
    public List<string> Trades { get; set; } = new();

    [JsonPropertyName("quotes")]
    public List<string> Quotes { get; set; } = new();

    [JsonPropertyName("bars")]
    public List<string> Bars { get; set; } = new();
}

/// <summary>
/// Alpaca error message
/// </summary>
public class AlpacaErrorMessage : AlpacaBaseMessage
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Alpaca trade message (Type: 't')
/// Real-time individual trade execution
/// </summary>
public class AlpacaTradeMessage : AlpacaBaseMessage
{
    /// <summary>
    /// Symbol (e.g., "AAPL")
    /// </summary>
    [JsonPropertyName("S")]
    public string S { get; set; } = string.Empty;

    /// <summary>
    /// Trade ID
    /// </summary>
    [JsonPropertyName("i")]
    public long I { get; set; }

    /// <summary>
    /// Exchange code (e.g., "V" for Nasdaq)
    /// </summary>
    [JsonPropertyName("x")]
    public string X { get; set; } = string.Empty;

    /// <summary>
    /// Trade price
    /// </summary>
    [JsonPropertyName("p")]
    public decimal P { get; set; }

    /// <summary>
    /// Trade size (number of shares)
    /// </summary>
    [JsonPropertyName("s")]
    public int S_Size { get; set; }

    /// <summary>
    /// Timestamp (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("t")]
    public string T_Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Trade conditions
    /// </summary>
    [JsonPropertyName("c")]
    public List<string> C { get; set; } = new();

    /// <summary>
    /// Tape (e.g., "C")
    /// </summary>
    [JsonPropertyName("z")]
    public string Z { get; set; } = string.Empty;

    /// <summary>
    /// Parse timestamp to DateTime
    /// </summary>
    public DateTime GetTimestamp()
    {
        if (DateTime.TryParse(T_Timestamp, out var dt))
            return dt.ToUniversalTime();
        return DateTime.UtcNow;
    }
}

/// <summary>
/// Alpaca quote message (Type: 'q')
/// Best bid/ask prices
/// </summary>
public class AlpacaQuoteMessage : AlpacaBaseMessage
{
    /// <summary>
    /// Symbol (e.g., "AAPL")
    /// </summary>
    [JsonPropertyName("S")]
    public string S { get; set; } = string.Empty;

    /// <summary>
    /// Bid exchange
    /// </summary>
    [JsonPropertyName("bx")]
    public string BX { get; set; } = string.Empty;

    /// <summary>
    /// Bid price
    /// </summary>
    [JsonPropertyName("bp")]
    public decimal BP { get; set; }

    /// <summary>
    /// Bid size
    /// </summary>
    [JsonPropertyName("bs")]
    public int BS { get; set; }

    /// <summary>
    /// Ask exchange
    /// </summary>
    [JsonPropertyName("ax")]
    public string AX { get; set; } = string.Empty;

    /// <summary>
    /// Ask price
    /// </summary>
    [JsonPropertyName("ap")]
    public decimal AP { get; set; }

    /// <summary>
    /// Ask size
    /// </summary>
    [JsonPropertyName("as")]
    public int AS { get; set; }

    /// <summary>
    /// Timestamp (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("t")]
    public string T_Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Conditions
    /// </summary>
    [JsonPropertyName("c")]
    public List<string> C { get; set; } = new();

    /// <summary>
    /// Tape
    /// </summary>
    [JsonPropertyName("z")]
    public string Z { get; set; } = string.Empty;

    /// <summary>
    /// Parse timestamp to DateTime
    /// </summary>
    public DateTime GetTimestamp()
    {
        if (DateTime.TryParse(T_Timestamp, out var dt))
            return dt.ToUniversalTime();
        return DateTime.UtcNow;
    }
}

/// <summary>
/// Alpaca bar message (Type: 'b')
/// 1-minute aggregated OHLCV data
/// </summary>
public class AlpacaBarMessage : AlpacaBaseMessage
{
    /// <summary>
    /// Symbol (e.g., "AAPL")
    /// </summary>
    [JsonPropertyName("S")]
    public string S { get; set; } = string.Empty;

    /// <summary>
    /// Open price
    /// </summary>
    [JsonPropertyName("o")]
    public decimal O { get; set; }

    /// <summary>
    /// High price
    /// </summary>
    [JsonPropertyName("h")]
    public decimal H { get; set; }

    /// <summary>
    /// Low price
    /// </summary>
    [JsonPropertyName("l")]
    public decimal L { get; set; }

    /// <summary>
    /// Close price
    /// </summary>
    [JsonPropertyName("c")]
    public decimal C { get; set; }

    /// <summary>
    /// Volume
    /// </summary>
    [JsonPropertyName("v")]
    public decimal V { get; set; }

    /// <summary>
    /// Timestamp (start time of bar)
    /// </summary>
    [JsonPropertyName("t")]
    public string T_Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Number of trades
    /// </summary>
    [JsonPropertyName("n")]
    public int N { get; set; }

    /// <summary>
    /// Volume-weighted average price
    /// </summary>
    [JsonPropertyName("vw")]
    public decimal VW { get; set; }

    /// <summary>
    /// Parse timestamp to DateTime
    /// </summary>
    public DateTime GetTimestamp()
    {
        if (DateTime.TryParse(T_Timestamp, out var dt))
            return dt.ToUniversalTime();
        return DateTime.UtcNow;
    }
}
