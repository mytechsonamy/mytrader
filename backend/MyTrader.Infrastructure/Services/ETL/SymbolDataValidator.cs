using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Data;
using MyTrader.Core.Models;
using MyTrader.Core.Services.ETL;

namespace MyTrader.Infrastructure.Services.ETL;

/// <summary>
/// Validates symbol data quality and fixes common issues
/// </summary>
internal class SymbolDataValidator
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger _logger;

    public SymbolDataValidator(ITradingDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ValidationIssue>> ValidateSymbolAsync(
        Symbol symbol,
        CancellationToken cancellationToken)
    {
        var issues = new List<ValidationIssue>();

        // Check for required fields
        if (string.IsNullOrWhiteSpace(symbol.Ticker))
        {
            issues.Add(CreateIssue(symbol, "MISSING_TICKER", "Symbol ticker is empty or null"));
        }

        if (string.IsNullOrWhiteSpace(symbol.AssetClass))
        {
            issues.Add(CreateIssue(symbol, "MISSING_ASSET_CLASS", "Symbol asset class is not specified"));
        }

        // Validate ticker format
        if (!string.IsNullOrEmpty(symbol.Ticker))
        {
            if (symbol.Ticker != symbol.Ticker.Trim())
            {
                issues.Add(CreateIssue(symbol, "TICKER_WHITESPACE", "Ticker contains leading/trailing whitespace"));
            }

            if (symbol.Ticker.Length > 50)
            {
                issues.Add(CreateIssue(symbol, "TICKER_TOO_LONG", "Ticker exceeds maximum length"));
            }

            if (symbol.Ticker.Contains(' '))
            {
                issues.Add(CreateIssue(symbol, "TICKER_CONTAINS_SPACES", "Ticker contains spaces"));
            }
        }

        // Check for duplicate tickers
        if (!string.IsNullOrEmpty(symbol.Ticker))
        {
            var duplicateCount = await _dbContext.Symbols
                .CountAsync(s => s.Ticker == symbol.Ticker && s.Id != symbol.Id, cancellationToken);

            if (duplicateCount > 0)
            {
                issues.Add(CreateIssue(symbol, "DUPLICATE_TICKER",
                    $"Ticker '{symbol.Ticker}' is used by {duplicateCount} other symbols"));
            }
        }

        // Validate venue consistency
        if (!string.IsNullOrEmpty(symbol.Venue) && !string.IsNullOrEmpty(symbol.AssetClass))
        {
            var isConsistent = ValidateVenueAssetClassConsistency(symbol.Venue, symbol.AssetClass);
            if (!isConsistent)
            {
                issues.Add(CreateIssue(symbol, "VENUE_ASSET_CLASS_MISMATCH",
                    $"Venue '{symbol.Venue}' is not consistent with asset class '{symbol.AssetClass}'"));
            }
        }

        // Check if symbol has market data
        var hasMarketData = await _dbContext.MarketData
            .AnyAsync(md => md.Symbol == symbol.Ticker, cancellationToken);

        if (!hasMarketData && symbol.IsActive)
        {
            issues.Add(CreateIssue(symbol, "NO_MARKET_DATA",
                "Active symbol has no corresponding market data"));
        }

        // Check currency consistency for crypto
        if (symbol.AssetClass == "CRYPTO")
        {
            var cryptoIssues = ValidateCryptoSymbol(symbol);
            issues.AddRange(cryptoIssues);
        }

        // Check precision values
        if (symbol.PricePrecision.HasValue && (symbol.PricePrecision < 0 || symbol.PricePrecision > 18))
        {
            issues.Add(CreateIssue(symbol, "INVALID_PRICE_PRECISION",
                "Price precision must be between 0 and 18"));
        }

        if (symbol.QuantityPrecision.HasValue && (symbol.QuantityPrecision < 0 || symbol.QuantityPrecision > 18))
        {
            issues.Add(CreateIssue(symbol, "INVALID_QUANTITY_PRECISION",
                "Quantity precision must be between 0 and 18"));
        }

        return issues;
    }

    public async Task<int> FixSymbolIssuesAsync(
        Symbol symbol,
        List<ValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var fixedCount = 0;

        foreach (var issue in issues)
        {
            try
            {
                var wasFixed = await FixIndividualIssueAsync(symbol, issue, cancellationToken);
                if (wasFixed)
                {
                    issue.WasFixed = true;
                    issue.FixAction = GetFixActionDescription(issue.IssueType);
                    fixedCount++;

                    _logger.LogDebug("Fixed issue {IssueType} for symbol {Symbol}: {Description}",
                        issue.IssueType, symbol.Ticker, issue.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fix issue {IssueType} for symbol {Symbol}",
                    issue.IssueType, symbol.Ticker);
            }
        }

        if (fixedCount > 0)
        {
            symbol.UpdatedAt = DateTime.UtcNow;
        }

        return fixedCount;
    }

    private async Task<bool> FixIndividualIssueAsync(
        Symbol symbol,
        ValidationIssue issue,
        CancellationToken cancellationToken)
    {
        switch (issue.IssueType)
        {
            case "TICKER_WHITESPACE":
                if (!string.IsNullOrEmpty(symbol.Ticker))
                {
                    symbol.Ticker = symbol.Ticker.Trim();
                    return true;
                }
                break;

            case "TICKER_CONTAINS_SPACES":
                if (!string.IsNullOrEmpty(symbol.Ticker))
                {
                    // Only fix if it's a simple case like "BTC USD" -> "BTCUSD"
                    if (symbol.Ticker.Split(' ').Length == 2)
                    {
                        symbol.Ticker = symbol.Ticker.Replace(" ", "");
                        return true;
                    }
                }
                break;

            case "MISSING_ASSET_CLASS":
                // Try to infer asset class from ticker patterns
                var inferredAssetClass = InferAssetClassFromTicker(symbol.Ticker);
                if (!string.IsNullOrEmpty(inferredAssetClass))
                {
                    symbol.AssetClass = inferredAssetClass;
                    return true;
                }
                break;

            case "NO_MARKET_DATA":
                // If symbol has no market data for extended period, deactivate it
                var lastMarketData = await _dbContext.MarketData
                    .Where(md => md.Symbol == symbol.Ticker)
                    .MaxAsync(md => (DateTime?)md.Timestamp, cancellationToken);

                if (lastMarketData == null || lastMarketData < DateTime.UtcNow.AddMonths(-6))
                {
                    symbol.IsActive = false;
                    return true;
                }
                break;

            case "CRYPTO_CURRENCY_MISMATCH":
                // Fix common crypto currency mismatches
                if (symbol.AssetClass == "CRYPTO")
                {
                    return FixCryptoCurrencies(symbol);
                }
                break;

            case "INVALID_PRICE_PRECISION":
                // Set reasonable default based on asset class
                symbol.PricePrecision = symbol.AssetClass switch
                {
                    "CRYPTO" => symbol.Ticker?.Contains("BTC") == true ? 2 : 6,
                    "STOCK" => 2,
                    "FOREX" => 4,
                    _ => 2
                };
                return true;

            case "INVALID_QUANTITY_PRECISION":
                // Set reasonable default based on asset class
                symbol.QuantityPrecision = symbol.AssetClass switch
                {
                    "CRYPTO" => 8,
                    "STOCK" => 0,
                    "FOREX" => 5,
                    _ => 8
                };
                return true;
        }

        return false;
    }

    private ValidationIssue CreateIssue(Symbol symbol, string issueType, string description)
    {
        return new ValidationIssue
        {
            SymbolId = symbol.Id,
            Symbol = symbol.Ticker,
            IssueType = issueType,
            Description = description,
            WasFixed = false
        };
    }

    private List<ValidationIssue> ValidateCryptoSymbol(Symbol symbol)
    {
        var issues = new List<ValidationIssue>();
        var ticker = symbol.Ticker?.ToUpper() ?? "";

        // Check common crypto ticker patterns
        if (ticker.EndsWith("USDT"))
        {
            var expectedBase = ticker[..^4];
            if (symbol.BaseCurrency != expectedBase)
            {
                issues.Add(CreateIssue(symbol, "CRYPTO_BASE_CURRENCY_MISMATCH",
                    $"Base currency should be '{expectedBase}' for ticker '{ticker}'"));
            }
            if (symbol.QuoteCurrency != "USDT")
            {
                issues.Add(CreateIssue(symbol, "CRYPTO_QUOTE_CURRENCY_MISMATCH",
                    $"Quote currency should be 'USDT' for ticker '{ticker}'"));
            }
        }
        else if (ticker.EndsWith("-USD"))
        {
            var expectedBase = ticker[..^4];
            if (symbol.BaseCurrency != expectedBase)
            {
                issues.Add(CreateIssue(symbol, "CRYPTO_BASE_CURRENCY_MISMATCH",
                    $"Base currency should be '{expectedBase}' for ticker '{ticker}'"));
            }
            if (symbol.QuoteCurrency != "USD")
            {
                issues.Add(CreateIssue(symbol, "CRYPTO_QUOTE_CURRENCY_MISMATCH",
                    $"Quote currency should be 'USD' for ticker '{ticker}'"));
            }
        }

        return issues;
    }

    private bool ValidateVenueAssetClassConsistency(string venue, string assetClass)
    {
        return venue.ToUpper() switch
        {
            "BINANCE" => assetClass == "CRYPTO",
            "BIST" => assetClass.StartsWith("STOCK"),
            "NASDAQ" or "NYSE" => assetClass.StartsWith("STOCK"),
            "FOREX_MARKET" => assetClass == "FOREX",
            _ => true // Unknown venues are considered valid
        };
    }

    private string? InferAssetClassFromTicker(string ticker)
    {
        if (string.IsNullOrEmpty(ticker))
            return null;

        ticker = ticker.ToUpper();

        // Crypto patterns
        if (ticker.EndsWith("USDT") || ticker.EndsWith("-USD") ||
            ticker.EndsWith("BTC") || ticker.EndsWith("ETH"))
        {
            return "CRYPTO";
        }

        // Forex patterns (6 characters, all letters)
        if (ticker.Length == 6 && ticker.All(char.IsLetter))
        {
            return "FOREX";
        }

        // Turkish stocks (typically 5-6 characters, all letters, no USD suffix)
        if (ticker.Length <= 6 && ticker.All(char.IsLetter) &&
            !ticker.EndsWith("USD") && !ticker.Contains("-"))
        {
            return "STOCK_BIST";
        }

        // US stocks (common patterns)
        if ((ticker.Length <= 5 && ticker.All(char.IsLetter)) ||
            ticker.All(c => char.IsLetter(c) || c == '.'))
        {
            return "STOCK";
        }

        return null;
    }

    private bool FixCryptoCurrencies(Symbol symbol)
    {
        var ticker = symbol.Ticker?.ToUpper() ?? "";
        var wasFixed = false;

        if (ticker.EndsWith("USDT"))
        {
            var expectedBase = ticker[..^4];
            if (symbol.BaseCurrency != expectedBase)
            {
                symbol.BaseCurrency = expectedBase;
                wasFixed = true;
            }
            if (symbol.QuoteCurrency != "USDT")
            {
                symbol.QuoteCurrency = "USDT";
                wasFixed = true;
            }
        }
        else if (ticker.EndsWith("-USD"))
        {
            var expectedBase = ticker[..^4];
            if (symbol.BaseCurrency != expectedBase)
            {
                symbol.BaseCurrency = expectedBase;
                wasFixed = true;
            }
            if (symbol.QuoteCurrency != "USD")
            {
                symbol.QuoteCurrency = "USD";
                wasFixed = true;
            }
        }

        return wasFixed;
    }

    private string GetFixActionDescription(string issueType)
    {
        return issueType switch
        {
            "TICKER_WHITESPACE" => "Trimmed whitespace from ticker",
            "TICKER_CONTAINS_SPACES" => "Removed spaces from ticker",
            "MISSING_ASSET_CLASS" => "Inferred asset class from ticker pattern",
            "NO_MARKET_DATA" => "Deactivated symbol due to lack of market data",
            "CRYPTO_CURRENCY_MISMATCH" => "Fixed crypto currency fields based on ticker",
            "INVALID_PRICE_PRECISION" => "Set default price precision based on asset class",
            "INVALID_QUANTITY_PRECISION" => "Set default quantity precision based on asset class",
            _ => "Applied automatic fix"
        };
    }
}