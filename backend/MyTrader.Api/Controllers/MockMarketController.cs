using Microsoft.AspNetCore.Mvc;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Market Data")]
public class MockMarketController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public MockMarketController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("symbols")]
    public ActionResult GetSymbols()
    {
        // Mock symbols data with current prices
        var symbols = new
        {
            symbols = new Dictionary<string, object>
            {
                ["BTC"] = new { 
                    symbol = "BTCUSDT", 
                    display_name = "Bitcoin",
                    price = 65430.50m, 
                    change = 2.45m,
                    signal = "BUY",
                    indicators = new { RSI = 45.2, MACD = 0.5, BB_UPPER = 66000, BB_LOWER = 64000 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["ETH"] = new { 
                    symbol = "ETHUSDT", 
                    display_name = "Ethereum",
                    price = 3542.80m, 
                    change = -1.25m,
                    signal = "SELL",
                    indicators = new { RSI = 62.1, MACD = -0.3, BB_UPPER = 3600, BB_LOWER = 3500 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["XRP"] = new { 
                    symbol = "XRPUSDT", 
                    display_name = "Ripple",
                    price = 0.5847m, 
                    change = 0.85m,
                    signal = "NEUTRAL",
                    indicators = new { RSI = 51.7, MACD = 0.1, BB_UPPER = 0.59, BB_LOWER = 0.57 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["BNB"] = new { 
                    symbol = "BNBUSDT", 
                    display_name = "Binance Coin",
                    price = 598.75m, 
                    change = 1.65m,
                    signal = "BUY",
                    indicators = new { RSI = 48.9, MACD = 0.7, BB_UPPER = 610, BB_LOWER = 585 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["ADA"] = new { 
                    symbol = "ADAUSDT", 
                    display_name = "Cardano",
                    price = 0.3421m, 
                    change = -0.95m,
                    signal = "SELL",
                    indicators = new { RSI = 55.3, MACD = -0.2, BB_UPPER = 0.35, BB_LOWER = 0.33 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["SOL"] = new { 
                    symbol = "SOLUSDT", 
                    display_name = "Solana",
                    price = 132.45m, 
                    change = 3.25m,
                    signal = "BUY",
                    indicators = new { RSI = 42.8, MACD = 0.9, BB_UPPER = 140, BB_LOWER = 125 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["DOT"] = new { 
                    symbol = "DOTUSDT", 
                    display_name = "Polkadot",
                    price = 4.12m, 
                    change = 0.45m,
                    signal = "NEUTRAL",
                    indicators = new { RSI = 49.2, MACD = 0.05, BB_UPPER = 4.2, BB_LOWER = 4.0 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["POL"] = new { 
                    symbol = "POLUSDT", 
                    display_name = "Polygon",
                    price = 0.3847m, 
                    change = -1.15m,
                    signal = "SELL",
                    indicators = new { RSI = 58.1, MACD = -0.15, BB_UPPER = 0.39, BB_LOWER = 0.37 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["AVAX"] = new { 
                    symbol = "AVAXUSDT", 
                    display_name = "Avalanche",
                    price = 24.68m, 
                    change = 2.75m,
                    signal = "BUY",
                    indicators = new { RSI = 44.6, MACD = 0.4, BB_UPPER = 25.5, BB_LOWER = 23.8 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                ["LINK"] = new { 
                    symbol = "LINKUSDT", 
                    display_name = "Chainlink",
                    price = 11.24m, 
                    change = 1.35m,
                    signal = "NEUTRAL",
                    indicators = new { RSI = 50.8, MACD = 0.2, BB_UPPER = 11.5, BB_LOWER = 10.9 },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            },
            interval = "1m"
        };

        return Ok(symbols);
    }

    [HttpGet("signals/{symbol}")]
    public ActionResult GetSignals(string symbol)
    {
        // Mock signals data for a specific symbol
        var signals = new
        {
            signals = new[]
            {
                new
                {
                    symbol = symbol.ToUpper(),
                    price = GetMockPrice(symbol),
                    change = GetMockChange(symbol),
                    signal = GetMockSignal(symbol),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    indicators = new { RSI = 45.2, MACD = 0.5, BB_UPPER = 66000, BB_LOWER = 64000 }
                }
            }
        };

        return Ok(signals);
    }

    private decimal GetMockPrice(string symbol)
    {
        return symbol.ToUpper() switch
        {
            "BTC" => 65430.50m,
            "ETH" => 3542.80m,
            "XRP" => 0.5847m,
            "BNB" => 598.75m,
            "ADA" => 0.3421m,
            "SOL" => 132.45m,
            "DOT" => 4.12m,
            "POL" => 0.3847m,
            "AVAX" => 24.68m,
            "LINK" => 11.24m,
            _ => 100.00m
        };
    }

    private decimal GetMockChange(string symbol)
    {
        return symbol.ToUpper() switch
        {
            "BTC" => 2.45m,
            "ETH" => -1.25m,
            "XRP" => 0.85m,
            "BNB" => 1.65m,
            "ADA" => -0.95m,
            "SOL" => 3.25m,
            "DOT" => 0.45m,
            "POL" => -1.15m,
            "AVAX" => 2.75m,
            "LINK" => 1.35m,
            _ => 0.00m
        };
    }

    private string GetMockSignal(string symbol)
    {
        return symbol.ToUpper() switch
        {
            "BTC" => "BUY",
            "ETH" => "SELL", 
            "XRP" => "NEUTRAL",
            "BNB" => "BUY",
            "ADA" => "SELL",
            "SOL" => "BUY",
            "DOT" => "NEUTRAL",
            "POL" => "SELL",
            "AVAX" => "BUY",
            "LINK" => "NEUTRAL",
            _ => "NEUTRAL"
        };
    }
}