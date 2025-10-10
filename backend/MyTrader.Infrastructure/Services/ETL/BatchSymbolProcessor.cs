using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Data;
using MyTrader.Core.Models;
using MyTrader.Core.Services.ETL;
using System.Diagnostics;

namespace MyTrader.Infrastructure.Services.ETL;

/// <summary>
/// Handles batch processing of symbols with transaction isolation and error recovery
/// </summary>
internal class BatchSymbolProcessor
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly SymbolSyncOptions _options;

    public BatchSymbolProcessor(
        ITradingDbContext dbContext,
        ILogger logger,
        SymbolSyncOptions options)
    {
        _dbContext = dbContext;
        _logger = logger;
        _options = options;
    }

    public async Task<List<BatchProcessResult>> ProcessSymbolBatchesAsync(
        List<MissingSymbolInfo> missingSymbols,
        CancellationToken cancellationToken)
    {
        var results = new List<BatchProcessResult>();
        var batches = CreateBatches(missingSymbols, _options.BatchSize);

        _logger.LogInformation("Processing {TotalSymbols} symbols in {BatchCount} batches",
            missingSymbols.Count, batches.Count);

        var semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
        var tasks = batches.Select(async (batch, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await ProcessBatchAsync(batch, index + 1, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var batchResults = await Task.WhenAll(tasks);
        results.AddRange(batchResults);

        return results;
    }

    private async Task<BatchProcessResult> ProcessBatchAsync(
        List<MissingSymbolInfo> batchSymbols,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new BatchProcessResult
        {
            BatchNumber = batchNumber,
            TotalSymbolsInBatch = batchSymbols.Count
        };

        _logger.LogDebug("Processing batch {BatchNumber} with {Count} symbols",
            batchNumber, batchSymbols.Count);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var missingSymbol in batchSymbols)
            {
                try
                {
                    var symbolResult = await ProcessSingleSymbolAsync(missingSymbol, cancellationToken);

                    switch (symbolResult.Action)
                    {
                        case SymbolProcessAction.Added:
                            result.SymbolsAdded++;
                            break;
                        case SymbolProcessAction.Updated:
                            result.SymbolsUpdated++;
                            break;
                        case SymbolProcessAction.Skipped:
                            result.SymbolsSkipped++;
                            if (symbolResult.Warning != null)
                                result.Warnings.Add(symbolResult.Warning);
                            break;
                    }

                    result.RecordsProcessed += missingSymbol.RecordCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing symbol {Symbol} in batch {BatchNumber}",
                        missingSymbol.Symbol, batchNumber);

                    result.Errors.Add(new SymbolSyncError
                    {
                        Symbol = missingSymbol.Symbol,
                        ErrorType = ex.GetType().Name,
                        ErrorMessage = ex.Message,
                        IsRetryable = IsRetryableError(ex)
                    });
                }
            }

            // Save all changes in this batch as a single transaction
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            result.Success = true;
            result.Duration = stopwatch.Elapsed;

            _logger.LogDebug("Completed batch {BatchNumber}: Added {Added}, Updated {Updated}, Errors {Errors}",
                batchNumber, result.SymbolsAdded, result.SymbolsUpdated, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error processing batch {BatchNumber}, rolling back transaction", batchNumber);

            await transaction.RollbackAsync(cancellationToken);

            result.Success = false;
            result.FatalError = ex.Message;
            result.Duration = stopwatch.Elapsed;

            return result;
        }
    }

    private async Task<SymbolProcessResult> ProcessSingleSymbolAsync(
        MissingSymbolInfo missingSymbol,
        CancellationToken cancellationToken)
    {
        // Check if symbol was added by another batch (race condition protection)
        var existingSymbol = await _dbContext.Symbols
            .FirstOrDefaultAsync(s => s.Ticker == missingSymbol.Symbol, cancellationToken);

        if (existingSymbol != null)
        {
            if (_options.SkipExistingSymbols)
            {
                return new SymbolProcessResult
                {
                    Action = SymbolProcessAction.Skipped,
                    Warning = $"Symbol {missingSymbol.Symbol} was already added by another process"
                };
            }
            else
            {
                // Update existing symbol if needed
                var wasUpdated = UpdateSymbolMetadata(existingSymbol, missingSymbol);
                return new SymbolProcessResult
                {
                    Action = wasUpdated ? SymbolProcessAction.Updated : SymbolProcessAction.Skipped
                };
            }
        }

        // Create new symbol
        var newSymbol = CreateSymbolFromMissingInfo(missingSymbol);

        // Enrich with metadata if enabled
        if (_options.AutoEnrichMetadata)
        {
            await EnrichSymbolMetadataAsync(newSymbol, cancellationToken);
        }

        _dbContext.Symbols.Add(newSymbol);

        return new SymbolProcessResult { Action = SymbolProcessAction.Added };
    }

    private Symbol CreateSymbolFromMissingInfo(MissingSymbolInfo missingInfo)
    {
        var symbol = new Symbol
        {
            Id = Guid.NewGuid(),
            Ticker = missingInfo.Symbol,
            AssetClass = missingInfo.AssetClass,
            IsActive = true,
            IsTracked = false, // Don't auto-track newly discovered symbols
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Infer venue and currencies from ticker and asset class
        InferSymbolProperties(symbol, missingInfo);

        return symbol;
    }

    private void InferSymbolProperties(Symbol symbol, MissingSymbolInfo missingInfo)
    {
        var ticker = symbol.Ticker.ToUpper();

        // Infer properties based on asset class and ticker patterns
        switch (missingInfo.AssetClass?.ToUpper())
        {
            case "CRYPTO":
                if (ticker.EndsWith("USDT"))
                {
                    symbol.BaseCurrency = ticker[..^4]; // Remove USDT suffix
                    symbol.QuoteCurrency = "USDT";
                    symbol.Venue = "BINANCE";
                }
                else if (ticker.EndsWith("-USD"))
                {
                    symbol.BaseCurrency = ticker[..^4]; // Remove -USD suffix
                    symbol.QuoteCurrency = "USD";
                    symbol.Venue = "YAHOO_FINANCE";
                }
                else
                {
                    symbol.BaseCurrency = ticker;
                    symbol.QuoteCurrency = "USD";
                    symbol.Venue = "CRYPTO_EXCHANGE";
                }
                break;

            case "STOCK":
            case "STOCK_BIST":
                if (IsBistSymbol(ticker))
                {
                    symbol.Venue = "BIST";
                    symbol.QuoteCurrency = "TRY";
                    symbol.Country = "TR";
                }
                else
                {
                    symbol.Venue = "NASDAQ"; // Default assumption
                    symbol.QuoteCurrency = "USD";
                    symbol.Country = "US";
                }
                break;

            case "FOREX":
                var currencyPair = ParseForexPair(ticker);
                symbol.BaseCurrency = currencyPair.Base;
                symbol.QuoteCurrency = currencyPair.Quote;
                symbol.Venue = "FOREX_MARKET";
                break;

            default:
                symbol.Venue = "UNKNOWN";
                symbol.QuoteCurrency = "USD";
                break;
        }

        // Set display name
        symbol.Display = GenerateDisplayName(symbol);
    }

    private bool UpdateSymbolMetadata(Symbol existingSymbol, MissingSymbolInfo missingInfo)
    {
        var wasUpdated = false;

        // Update timestamps if this symbol has more recent data
        if (missingInfo.LastSeen > existingSymbol.UpdatedAt)
        {
            existingSymbol.UpdatedAt = DateTime.UtcNow;
            wasUpdated = true;
        }

        // Activate symbol if it was inactive but has recent data
        if (!existingSymbol.IsActive &&
            missingInfo.LastSeen > DateTime.UtcNow.AddDays(-30))
        {
            existingSymbol.IsActive = true;
            existingSymbol.UpdatedAt = DateTime.UtcNow;
            wasUpdated = true;
        }

        return wasUpdated;
    }

    private async Task EnrichSymbolMetadataAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        // TODO: Implement metadata enrichment from external sources
        // This would call APIs like CoinMarketCap, Alpha Vantage, etc.
        await Task.Delay(10, cancellationToken); // Placeholder

        // Example enrichment logic:
        if (symbol.AssetClass == "CRYPTO" && string.IsNullOrEmpty(symbol.FullName))
        {
            symbol.FullName = GetCryptoFullName(symbol.Ticker);
        }
    }

    private string GetCryptoFullName(string ticker)
    {
        // Simple mapping - in production this would come from external API
        return ticker?.ToUpper() switch
        {
            "BTC" or "BTCUSDT" => "Bitcoin",
            "ETH" or "ETHUSDT" => "Ethereum",
            "ADA" or "ADAUSDT" => "Cardano",
            "SOL" or "SOLUSDT" => "Solana",
            "AVAX" or "AVAXUSDT" => "Avalanche",
            _ => ticker
        };
    }

    private static bool IsBistSymbol(string ticker)
    {
        // BIST symbols typically follow certain patterns
        return ticker.Length <= 6 &&
               ticker.All(c => char.IsLetter(c) || char.IsDigit(c)) &&
               !ticker.EndsWith("USD") &&
               !ticker.Contains("-");
    }

    private static (string Base, string Quote) ParseForexPair(string ticker)
    {
        if (ticker.Length == 6)
        {
            return (ticker[..3], ticker[3..]);
        }
        else if (ticker.Contains("/"))
        {
            var parts = ticker.Split('/');
            return (parts[0], parts.Length > 1 ? parts[1] : "USD");
        }

        return (ticker, "USD");
    }

    private static string GenerateDisplayName(Symbol symbol)
    {
        if (!string.IsNullOrEmpty(symbol.FullName))
            return symbol.FullName;

        return symbol.AssetClass switch
        {
            "CRYPTO" => $"{symbol.BaseCurrency}/{symbol.QuoteCurrency}",
            "STOCK" => symbol.Ticker,
            "FOREX" => $"{symbol.BaseCurrency}/{symbol.QuoteCurrency}",
            _ => symbol.Ticker
        };
    }

    private static List<List<T>> CreateBatches<T>(List<T> items, int batchSize)
    {
        var batches = new List<List<T>>();
        for (int i = 0; i < items.Count; i += batchSize)
        {
            batches.Add(items.Skip(i).Take(batchSize).ToList());
        }
        return batches;
    }

    private static bool IsRetryableError(Exception ex)
    {
        return ex is TimeoutException ||
               ex is TaskCanceledException ||
               ex is DbUpdateException ||
               (ex.InnerException != null && IsRetryableError(ex.InnerException));
    }
}

/// <summary>
/// Result of processing a batch of symbols
/// </summary>
internal class BatchProcessResult
{
    public int BatchNumber { get; set; }
    public int TotalSymbolsInBatch { get; set; }
    public bool Success { get; set; }
    public string? FatalError { get; set; }
    public TimeSpan Duration { get; set; }

    // Statistics
    public int SymbolsAdded { get; set; }
    public int SymbolsUpdated { get; set; }
    public int SymbolsSkipped { get; set; }
    public int RecordsProcessed { get; set; }

    // Error tracking
    public List<SymbolSyncError> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of processing a single symbol
/// </summary>
internal class SymbolProcessResult
{
    public SymbolProcessAction Action { get; set; }
    public string? Warning { get; set; }
}

/// <summary>
/// Action taken when processing a symbol
/// </summary>
internal enum SymbolProcessAction
{
    Added,
    Updated,
    Skipped
}