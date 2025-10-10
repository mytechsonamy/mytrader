using Microsoft.Extensions.Logging;
using MyTrader.Core.Interfaces;

namespace MyTrader.Services.Market;

/// <summary>
/// Market data router implementation for categorizing and routing market data
/// </summary>
public class MarketDataRouter : IMarketDataRouter
{
    private readonly ILogger<MarketDataRouter> _logger;

    // Market classification patterns
    private static readonly Dictionary<string, List<string>> MarketPatterns = new()
    {
        { "BINANCE", new List<string> { "USDT", "BUSD", "BTC", "ETH", "BNB" } },
        { "BIST", new List<string> { ".IS", "THYAO", "GARAN", "AKBNK", "EREGL", "SAHOL" } },
        { "NASDAQ", new List<string> { "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", "META", "NVDA" } },
        { "NYSE", new List<string> { "JPM", "BAC", "WMT", "V", "MA", "DIS", "NKE" } }
    };

    // Asset class patterns
    private static readonly Dictionary<string, List<string>> AssetClassPatterns = new()
    {
        { "CRYPTO", new List<string> { "BTC", "ETH", "USDT", "BUSD", "BNB", "XRP", "ADA", "SOL", "DOGE" } },
        { "STOCK", new List<string> { ".IS", "AAPL", "MSFT", "GOOGL", "THYAO", "GARAN" } },
        { "FOREX", new List<string> { "USD", "EUR", "GBP", "JPY", "TRY" } },
        { "COMMODITY", new List<string> { "GOLD", "SILVER", "OIL", "GAS" } }
    };

    // Market trading hours (UTC)
    private static readonly Dictionary<string, (TimeSpan Open, TimeSpan Close, string TimeZone)> MarketHours = new()
    {
        { "BINANCE", (TimeSpan.Zero, TimeSpan.FromHours(24), "UTC") }, // 24/7
        { "BIST", (TimeSpan.FromHours(7), TimeSpan.FromHours(15, 30, 0), "Europe/Istanbul") }, // 10:00-18:30 Istanbul time
        { "NASDAQ", (TimeSpan.FromHours(14, 30, 0), TimeSpan.FromHours(21), "America/New_York") }, // 9:30-16:00 ET
        { "NYSE", (TimeSpan.FromHours(14, 30, 0), TimeSpan.FromHours(21), "America/New_York") } // 9:30-16:00 ET
    };

    public MarketDataRouter(ILogger<MarketDataRouter> logger)
    {
        _logger = logger;
    }

    public string DetermineMarket(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            _logger.LogWarning("Empty symbol provided to DetermineMarket");
            return "UNKNOWN";
        }

        symbol = symbol.ToUpperInvariant();

        // Check crypto patterns (Binance)
        if (symbol.EndsWith("USDT") || symbol.EndsWith("BUSD") || 
            symbol.EndsWith("BTC") || symbol.EndsWith("ETH"))
        {
            return "BINANCE";
        }

        // Check BIST patterns
        if (symbol.EndsWith(".IS") || IsBistSymbol(symbol))
        {
            return "BIST";
        }

        // Check known NASDAQ symbols
        if (IsNasdaqSymbol(symbol))
        {
            return "NASDAQ";
        }

        // Check known NYSE symbols
        if (IsNyseSymbol(symbol))
        {
            return "NYSE";
        }

        // Default to UNKNOWN
        _logger.LogDebug($"Could not determine market for symbol: {symbol}");
        return "UNKNOWN";
    }

    public string ClassifyAssetClass(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            _logger.LogWarning("Empty symbol provided to ClassifyAssetClass");
            return "UNKNOWN";
        }

        symbol = symbol.ToUpperInvariant();

        // Check crypto patterns
        if (symbol.EndsWith("USDT") || symbol.EndsWith("BUSD") || 
            symbol.EndsWith("BTC") || symbol.EndsWith("ETH") ||
            symbol.Contains("BTC") || symbol.Contains("ETH"))
        {
            return "CRYPTO";
        }

        // Check forex patterns
        if (IsForexPair(symbol))
        {
            return "FOREX";
        }

        // Check commodity patterns
        if (symbol.Contains("GOLD") || symbol.Contains("SILVER") || 
            symbol.Contains("OIL") || symbol.Contains("GAS"))
        {
            return "COMMODITY";
        }

        // Default to STOCK for everything else
        return "STOCK";
    }

    public string GetMarketGroupName(string symbol)
    {
        var market = DetermineMarket(symbol);
        return $"Market_{market}";
    }

    public string GetAssetClassGroupName(string symbol)
    {
        var assetClass = ClassifyAssetClass(symbol);
        return $"AssetClass_{assetClass}";
    }

    public bool IsMarketOpen(string market)
    {
        var status = GetMarketStatus(market);
        return status.IsOpen;
    }

    public MarketStatus GetMarketStatus(string market)
    {
        if (!MarketHours.ContainsKey(market))
        {
            return new MarketStatus
            {
                Market = market,
                IsOpen = false,
                Status = "UNKNOWN",
                LastUpdate = DateTime.UtcNow
            };
        }

        var (openTime, closeTime, timeZone) = MarketHours[market];
        var now = DateTime.UtcNow;
        var currentTime = now.TimeOfDay;

        // Binance is 24/7
        if (market == "BINANCE")
        {
            return new MarketStatus
            {
                Market = market,
                IsOpen = true,
                Status = "OPEN",
                TimeZone = timeZone,
                LastUpdate = now
            };
        }

        // Check if current time is within market hours
        bool isOpen = currentTime >= openTime && currentTime <= closeTime;

        // Check if it's a weekend (markets closed)
        if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
        {
            isOpen = false;
        }

        var status = new MarketStatus
        {
            Market = market,
            IsOpen = isOpen,
            Status = isOpen ? "OPEN" : "CLOSED",
            TimeZone = timeZone,
            LastUpdate = now
        };

        // Calculate next open/close times
        if (isOpen)
        {
            status.NextClose = now.Date.Add(closeTime);
        }
        else
        {
            // Calculate next open (next business day)
            var nextOpen = now.Date.Add(openTime);
            if (currentTime > closeTime || now.DayOfWeek == DayOfWeek.Friday)
            {
                // Move to next business day
                nextOpen = nextOpen.AddDays(1);
                while (nextOpen.DayOfWeek == DayOfWeek.Saturday || nextOpen.DayOfWeek == DayOfWeek.Sunday)
                {
                    nextOpen = nextOpen.AddDays(1);
                }
            }
            status.NextOpen = nextOpen;
        }

        return status;
    }

    public List<string> GetActiveMarkets()
    {
        return MarketHours.Keys.ToList();
    }

    public List<string> GetRoutingGroups(string symbol)
    {
        var groups = new List<string>
        {
            // Symbol-specific group
            $"Symbol_{symbol}",
            
            // Market-specific group
            GetMarketGroupName(symbol),
            
            // Asset class group
            GetAssetClassGroupName(symbol),
            
            // Global market data group
            "MarketData_All"
        };

        return groups;
    }

    // Helper methods

    private bool IsBistSymbol(string symbol)
    {
        // Common BIST symbols
        var bistSymbols = new HashSet<string>
        {
            "THYAO", "GARAN", "AKBNK", "EREGL", "SAHOL", "KCHOL", "TUPRS", 
            "PETKM", "SISE", "ASELS", "TCELL", "ISCTR", "VAKBN", "KOZAL"
        };

        return bistSymbols.Contains(symbol);
    }

    private bool IsNasdaqSymbol(string symbol)
    {
        var nasdaqSymbols = new HashSet<string>
        {
            "AAPL", "MSFT", "GOOGL", "GOOG", "AMZN", "TSLA", "META", "NVDA",
            "NFLX", "ADBE", "INTC", "CSCO", "CMCSA", "PEP", "AVGO", "COST"
        };

        return nasdaqSymbols.Contains(symbol);
    }

    private bool IsNyseSymbol(string symbol)
    {
        var nyseSymbols = new HashSet<string>
        {
            "JPM", "BAC", "WMT", "V", "MA", "DIS", "NKE", "HD", "PG", "KO",
            "MCD", "VZ", "T", "CVX", "XOM", "BA", "GE", "IBM", "CAT"
        };

        return nyseSymbols.Contains(symbol);
    }

    private bool IsForexPair(string symbol)
    {
        var forexCurrencies = new HashSet<string> { "USD", "EUR", "GBP", "JPY", "TRY", "CHF", "AUD", "CAD" };
        
        // Check if symbol contains two currency codes
        foreach (var curr1 in forexCurrencies)
        {
            foreach (var curr2 in forexCurrencies)
            {
                if (curr1 != curr2 && symbol.Contains(curr1) && symbol.Contains(curr2))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
