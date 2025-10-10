using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Data;
using MyTrader.Core.Models;
using MyTrader.Core.Services.ETL;
using System.Diagnostics;

namespace MyTrader.Infrastructure.Services.ETL;

/// <summary>
/// Production-ready service for bootstrapping market reference data
/// </summary>
public class MarketDataBootstrapService : IMarketDataBootstrapService
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger<MarketDataBootstrapService> _logger;

    public MarketDataBootstrapService(
        ITradingDbContext dbContext,
        ILogger<MarketDataBootstrapService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BootstrapResult> InitializeAssetClassesAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new BootstrapResult { ExecutedAt = DateTime.UtcNow };

        _logger.LogInformation("Initializing asset classes (overwrite: {Overwrite})", overwriteExisting);

        try
        {
            var standardAssetClasses = GetStandardAssetClasses();
            result.TotalItems = standardAssetClasses.Count;

            foreach (var assetClass in standardAssetClasses)
            {
                var existing = await _dbContext.AssetClasses
                    .FirstOrDefaultAsync(ac => ac.Code == assetClass.Code, cancellationToken);

                if (existing == null)
                {
                    _dbContext.AssetClasses.Add(assetClass);
                    result.ItemsCreated++;
                    result.CreatedItems.Add($"{assetClass.Code} - {assetClass.Name}");
                    _logger.LogDebug("Created asset class: {Code}", assetClass.Code);
                }
                else if (overwriteExisting)
                {
                    UpdateAssetClass(existing, assetClass);
                    result.ItemsUpdated++;
                    result.UpdatedItems.Add($"{assetClass.Code} - {assetClass.Name}");
                    _logger.LogDebug("Updated asset class: {Code}", assetClass.Code);
                }
                else
                {
                    result.ItemsSkipped++;
                    _logger.LogDebug("Skipped existing asset class: {Code}", assetClass.Code);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Asset classes initialization completed. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                result.ItemsCreated, result.ItemsUpdated, result.ItemsSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing asset classes");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<BootstrapResult> InitializeMarketsAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new BootstrapResult { ExecutedAt = DateTime.UtcNow };

        _logger.LogInformation("Initializing markets (overwrite: {Overwrite})", overwriteExisting);

        try
        {
            // Ensure asset classes exist first
            var assetClasses = await _dbContext.AssetClasses.ToListAsync(cancellationToken);
            if (!assetClasses.Any())
            {
                result.Success = false;
                result.ErrorMessage = "Asset classes must be initialized before markets";
                result.Warnings.Add("Run InitializeAssetClassesAsync first");
                return result;
            }

            var assetClassLookup = assetClasses.ToDictionary(ac => ac.Code, ac => ac.Id);
            var standardMarkets = GetStandardMarkets(assetClassLookup);
            result.TotalItems = standardMarkets.Count;

            foreach (var market in standardMarkets)
            {
                var existing = await _dbContext.Markets
                    .FirstOrDefaultAsync(m => m.Code == market.Code, cancellationToken);

                if (existing == null)
                {
                    _dbContext.Markets.Add(market);
                    result.ItemsCreated++;
                    result.CreatedItems.Add($"{market.Code} - {market.Name}");
                    _logger.LogDebug("Created market: {Code}", market.Code);
                }
                else if (overwriteExisting)
                {
                    UpdateMarket(existing, market);
                    result.ItemsUpdated++;
                    result.UpdatedItems.Add($"{market.Code} - {market.Name}");
                    _logger.LogDebug("Updated market: {Code}", market.Code);
                }
                else
                {
                    result.ItemsSkipped++;
                    _logger.LogDebug("Skipped existing market: {Code}", market.Code);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Markets initialization completed. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                result.ItemsCreated, result.ItemsUpdated, result.ItemsSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing markets");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<BootstrapResult> InitializeTradingSessionsAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new BootstrapResult { ExecutedAt = DateTime.UtcNow };

        _logger.LogInformation("Initializing trading sessions (overwrite: {Overwrite})", overwriteExisting);

        try
        {
            var markets = await _dbContext.Markets.ToListAsync(cancellationToken);
            if (!markets.Any())
            {
                result.Success = false;
                result.ErrorMessage = "Markets must be initialized before trading sessions";
                result.Warnings.Add("Run InitializeMarketsAsync first");
                return result;
            }

            var marketLookup = markets.ToDictionary(m => m.Code, m => m.Id);
            var standardSessions = GetStandardTradingSessions(marketLookup);
            result.TotalItems = standardSessions.Count;

            foreach (var session in standardSessions)
            {
                var existing = await _dbContext.TradingSessions
                    .FirstOrDefaultAsync(ts => ts.MarketId == session.MarketId &&
                                               ts.SessionName == session.SessionName &&
                                               ts.DayOfWeek == session.DayOfWeek, cancellationToken);

                if (existing == null)
                {
                    _dbContext.TradingSessions.Add(session);
                    result.ItemsCreated++;
                    result.CreatedItems.Add($"{session.SessionName} for market ID {session.MarketId}");
                    _logger.LogDebug("Created trading session: {SessionName} for market {MarketId}",
                        session.SessionName, session.MarketId);
                }
                else if (overwriteExisting)
                {
                    UpdateTradingSession(existing, session);
                    result.ItemsUpdated++;
                    result.UpdatedItems.Add($"{session.SessionName} for market ID {session.MarketId}");
                    _logger.LogDebug("Updated trading session: {SessionName} for market {MarketId}",
                        session.SessionName, session.MarketId);
                }
                else
                {
                    result.ItemsSkipped++;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Trading sessions initialization completed. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                result.ItemsCreated, result.ItemsUpdated, result.ItemsSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing trading sessions");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<BootstrapResult> InitializeDataProvidersAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new BootstrapResult { ExecutedAt = DateTime.UtcNow };

        _logger.LogInformation("Initializing data providers (overwrite: {Overwrite})", overwriteExisting);

        try
        {
            var markets = await _dbContext.Markets.ToListAsync(cancellationToken);
            if (!markets.Any())
            {
                result.Success = false;
                result.ErrorMessage = "Markets must be initialized before data providers";
                result.Warnings.Add("Run InitializeMarketsAsync first");
                return result;
            }

            var marketLookup = markets.ToDictionary(m => m.Code, m => m.Id);
            var standardProviders = GetStandardDataProviders(marketLookup);
            result.TotalItems = standardProviders.Count;

            foreach (var provider in standardProviders)
            {
                var existing = await _dbContext.DataProviders
                    .FirstOrDefaultAsync(dp => dp.Code == provider.Code, cancellationToken);

                if (existing == null)
                {
                    _dbContext.DataProviders.Add(provider);
                    result.ItemsCreated++;
                    result.CreatedItems.Add($"{provider.Code} - {provider.Name}");
                    _logger.LogDebug("Created data provider: {Code}", provider.Code);
                }
                else if (overwriteExisting)
                {
                    UpdateDataProvider(existing, provider);
                    result.ItemsUpdated++;
                    result.UpdatedItems.Add($"{provider.Code} - {provider.Name}");
                    _logger.LogDebug("Updated data provider: {Code}", provider.Code);
                }
                else
                {
                    result.ItemsSkipped++;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Data providers initialization completed. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                result.ItemsCreated, result.ItemsUpdated, result.ItemsSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing data providers");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<CompleteBootstrapResult> BootstrapAllReferenceDataAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CompleteBootstrapResult { ExecutedAt = DateTime.UtcNow };

        _logger.LogInformation("Starting complete reference data bootstrap");

        try
        {
            // Bootstrap in dependency order
            result.AssetClassesResult = await InitializeAssetClassesAsync(overwriteExisting, cancellationToken);
            if (!result.AssetClassesResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = $"Asset classes initialization failed: {result.AssetClassesResult.ErrorMessage}";
                return result;
            }

            result.MarketsResult = await InitializeMarketsAsync(overwriteExisting, cancellationToken);
            if (!result.MarketsResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = $"Markets initialization failed: {result.MarketsResult.ErrorMessage}";
                return result;
            }

            result.TradingSessionsResult = await InitializeTradingSessionsAsync(overwriteExisting, cancellationToken);
            if (!result.TradingSessionsResult.Success)
            {
                result.AllWarnings.Add($"Trading sessions initialization had issues: {result.TradingSessionsResult.ErrorMessage}");
            }

            result.DataProvidersResult = await InitializeDataProvidersAsync(overwriteExisting, cancellationToken);
            if (!result.DataProvidersResult.Success)
            {
                result.AllWarnings.Add($"Data providers initialization had issues: {result.DataProvidersResult.ErrorMessage}");
            }

            // Collect all warnings and errors
            result.AllWarnings.AddRange(result.AssetClassesResult.Warnings);
            result.AllWarnings.AddRange(result.MarketsResult.Warnings);
            result.AllWarnings.AddRange(result.TradingSessionsResult.Warnings);
            result.AllWarnings.AddRange(result.DataProvidersResult.Warnings);

            result.AllErrors.AddRange(result.AssetClassesResult.Errors);
            result.AllErrors.AddRange(result.MarketsResult.Errors);
            result.AllErrors.AddRange(result.TradingSessionsResult.Errors);
            result.AllErrors.AddRange(result.DataProvidersResult.Errors);

            result.Success = result.AssetClassesResult.Success && result.MarketsResult.Success;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Complete bootstrap finished. Total created: {Created}, updated: {Updated}, warnings: {Warnings}",
                result.TotalItemsCreated, result.TotalItemsUpdated, result.AllWarnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during complete bootstrap");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<ReferenceDataValidationResult> ValidateReferenceDataAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new ReferenceDataValidationResult();

        try
        {
            // Get counts
            result.AssetClassCount = await _dbContext.AssetClasses.CountAsync(cancellationToken);
            result.MarketCount = await _dbContext.Markets.CountAsync(cancellationToken);
            result.TradingSessionCount = await _dbContext.TradingSessions.CountAsync(cancellationToken);
            result.DataProviderCount = await _dbContext.DataProviders.CountAsync(cancellationToken);

            // Check for orphaned records
            result.OrphanedMarkets = await _dbContext.Markets
                .CountAsync(m => !_dbContext.AssetClasses.Any(ac => ac.Id == m.AssetClassId), cancellationToken);

            result.OrphanedSessions = await _dbContext.TradingSessions
                .CountAsync(ts => !_dbContext.Markets.Any(m => m.Id == ts.MarketId), cancellationToken);

            result.OrphanedProviders = await _dbContext.DataProviders
                .CountAsync(dp => !_dbContext.Markets.Any(m => m.Id == dp.MarketId), cancellationToken);

            // Validate critical records exist
            var criticalAssetClasses = new[] { "CRYPTO", "STOCK", "FOREX" };
            var missingAssetClasses = new List<string>();

            foreach (var code in criticalAssetClasses)
            {
                var exists = await _dbContext.AssetClasses
                    .AnyAsync(ac => ac.Code == code, cancellationToken);
                if (!exists)
                {
                    missingAssetClasses.Add(code);
                }
            }

            if (missingAssetClasses.Any())
            {
                result.Issues.Add(new ValidationIssue
                {
                    IssueType = "MISSING_CRITICAL_ASSET_CLASSES",
                    Description = $"Missing critical asset classes: {string.Join(", ", missingAssetClasses)}"
                });
            }

            var criticalMarkets = new[] { "BINANCE", "BIST", "NASDAQ", "NYSE" };
            var missingMarkets = new List<string>();

            foreach (var code in criticalMarkets)
            {
                var exists = await _dbContext.Markets
                    .AnyAsync(m => m.Code == code, cancellationToken);
                if (!exists)
                {
                    missingMarkets.Add(code);
                }
            }

            if (missingMarkets.Any())
            {
                result.Issues.Add(new ValidationIssue
                {
                    IssueType = "MISSING_CRITICAL_MARKETS",
                    Description = $"Missing critical markets: {string.Join(", ", missingMarkets)}"
                });
            }

            // Add issues for orphaned records
            if (result.OrphanedMarkets > 0)
            {
                result.Issues.Add(new ValidationIssue
                {
                    IssueType = "ORPHANED_MARKETS",
                    Description = $"{result.OrphanedMarkets} markets have invalid asset class references"
                });
            }

            if (result.OrphanedSessions > 0)
            {
                result.Issues.Add(new ValidationIssue
                {
                    IssueType = "ORPHANED_TRADING_SESSIONS",
                    Description = $"{result.OrphanedSessions} trading sessions have invalid market references"
                });
            }

            if (result.OrphanedProviders > 0)
            {
                result.Issues.Add(new ValidationIssue
                {
                    IssueType = "ORPHANED_DATA_PROVIDERS",
                    Description = $"{result.OrphanedProviders} data providers have invalid market references"
                });
            }

            result.IsValid = !result.Issues.Any();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reference data");
            result.IsValid = false;
            result.Issues.Add(new ValidationIssue
            {
                IssueType = "VALIDATION_ERROR",
                Description = $"Validation failed: {ex.Message}"
            });
            return result;
        }
    }

    public async Task<BootstrapStatus> GetBootstrapStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var status = new BootstrapStatus();

        try
        {
            // Check if components are initialized
            status.AssetClassCount = await _dbContext.AssetClasses.CountAsync(cancellationToken);
            status.MarketCount = await _dbContext.Markets.CountAsync(cancellationToken);
            status.TradingSessionCount = await _dbContext.TradingSessions.CountAsync(cancellationToken);
            status.DataProviderCount = await _dbContext.DataProviders.CountAsync(cancellationToken);

            status.AssetClassesInitialized = status.AssetClassCount >= 3; // Minimum expected
            status.MarketsInitialized = status.MarketCount >= 4; // Minimum expected
            status.TradingSessionsInitialized = status.TradingSessionCount > 0;
            status.DataProvidersInitialized = status.DataProviderCount > 0;

            status.IsFullyBootstrapped = status.AssetClassesInitialized && status.MarketsInitialized &&
                                        status.TradingSessionsInitialized && status.DataProvidersInitialized;

            // Check for missing components
            if (!status.AssetClassesInitialized)
                status.MissingComponents.Add("Asset Classes");
            if (!status.MarketsInitialized)
                status.MissingComponents.Add("Markets");
            if (!status.TradingSessionsInitialized)
                status.MissingComponents.Add("Trading Sessions");
            if (!status.DataProvidersInitialized)
                status.MissingComponents.Add("Data Providers");

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bootstrap status");
            status.HealthWarnings.Add($"Error retrieving status: {ex.Message}");
            return status;
        }
    }

    #region Standard Data Definitions

    private List<AssetClass> GetStandardAssetClasses()
    {
        return new List<AssetClass>
        {
            new AssetClass
            {
                Code = "CRYPTO",
                Name = "Cryptocurrency",
                NameTurkish = "Kripto Para",
                Description = "Digital or virtual currencies secured by cryptography",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 6,
                DefaultQuantityPrecision = 8,
                Supports24x7Trading = true,
                SupportsFractional = true,
                MinTradeAmount = 0.001m,
                RegulatoryClass = "unregulated",
                DisplayOrder = 1
            },
            new AssetClass
            {
                Code = "STOCK",
                Name = "Stocks",
                NameTurkish = "Hisse Senetleri",
                Description = "Equity securities representing ownership in corporations",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 2,
                DefaultQuantityPrecision = 0,
                Supports24x7Trading = false,
                SupportsFractional = true,
                MinTradeAmount = 1.0m,
                RegulatoryClass = "regulated",
                DisplayOrder = 2
            },
            new AssetClass
            {
                Code = "STOCK_BIST",
                Name = "Turkish Stocks",
                NameTurkish = "Türk Hisse Senetleri",
                Description = "Turkish equity securities traded on BIST",
                PrimaryCurrency = "TRY",
                DefaultPricePrecision = 2,
                DefaultQuantityPrecision = 0,
                Supports24x7Trading = false,
                SupportsFractional = false,
                MinTradeAmount = 1.0m,
                RegulatoryClass = "regulated",
                DisplayOrder = 3
            },
            new AssetClass
            {
                Code = "FOREX",
                Name = "Foreign Exchange",
                NameTurkish = "Döviz",
                Description = "Currency pairs for foreign exchange trading",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 5,
                DefaultQuantityPrecision = 5,
                Supports24x7Trading = true,
                SupportsFractional = true,
                MinTradeAmount = 1000.0m,
                RegulatoryClass = "regulated",
                DisplayOrder = 4
            },
            new AssetClass
            {
                Code = "COMMODITY",
                Name = "Commodities",
                NameTurkish = "Emtia",
                Description = "Physical goods and raw materials",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 2,
                DefaultQuantityPrecision = 3,
                Supports24x7Trading = false,
                SupportsFractional = true,
                MinTradeAmount = 100.0m,
                RegulatoryClass = "regulated",
                DisplayOrder = 5
            },
            new AssetClass
            {
                Code = "ETF",
                Name = "Exchange Traded Funds",
                NameTurkish = "Borsa Yatırım Fonları",
                Description = "Investment funds traded on stock exchanges",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 2,
                DefaultQuantityPrecision = 0,
                Supports24x7Trading = false,
                SupportsFractional = true,
                MinTradeAmount = 1.0m,
                RegulatoryClass = "regulated",
                DisplayOrder = 6
            }
        };
    }

    private List<Market> GetStandardMarkets(Dictionary<string, Guid> assetClassLookup)
    {
        return new List<Market>
        {
            new Market
            {
                Code = "BINANCE",
                Name = "Binance",
                NameTurkish = "Binance",
                Description = "World's largest cryptocurrency exchange by trading volume",
                AssetClassId = assetClassLookup["CRYPTO"],
                CountryCode = "MT",
                Timezone = "UTC",
                PrimaryCurrency = "USDT",
                ApiBaseUrl = "https://api.binance.com",
                WebSocketUrl = "wss://stream.binance.com:9443",
                DefaultCommissionRate = 0.001m,
                Status = "OPEN",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 1
            },
            new Market
            {
                Code = "BIST",
                Name = "Borsa Istanbul",
                NameTurkish = "Borsa İstanbul",
                Description = "Turkish national stock exchange",
                AssetClassId = assetClassLookup["STOCK_BIST"],
                CountryCode = "TR",
                Timezone = "Europe/Istanbul",
                PrimaryCurrency = "TRY",
                ApiBaseUrl = "https://www.borsaistanbul.com",
                DefaultCommissionRate = 0.002m,
                Status = "CLOSED",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 15,
                DisplayOrder = 2
            },
            new Market
            {
                Code = "NASDAQ",
                Name = "NASDAQ Global Select Market",
                NameTurkish = "NASDAQ Küresel Seçilmiş Pazar",
                Description = "American stock exchange for technology companies",
                AssetClassId = assetClassLookup["STOCK"],
                CountryCode = "US",
                Timezone = "America/New_York",
                PrimaryCurrency = "USD",
                ApiBaseUrl = "https://api.nasdaq.com",
                DefaultCommissionRate = 0.005m,
                Status = "CLOSED",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 15,
                DisplayOrder = 3
            },
            new Market
            {
                Code = "NYSE",
                Name = "New York Stock Exchange",
                NameTurkish = "New York Menkul Kıymetler Borsası",
                Description = "Largest stock exchange in the world by market capitalization",
                AssetClassId = assetClassLookup["STOCK"],
                CountryCode = "US",
                Timezone = "America/New_York",
                PrimaryCurrency = "USD",
                DefaultCommissionRate = 0.005m,
                Status = "CLOSED",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 15,
                DisplayOrder = 4
            },
            new Market
            {
                Code = "FOREX_MARKET",
                Name = "Foreign Exchange Market",
                NameTurkish = "Döviz Piyasası",
                Description = "Global decentralized market for currency trading",
                AssetClassId = assetClassLookup["FOREX"],
                CountryCode = "GLOBAL",
                Timezone = "UTC",
                PrimaryCurrency = "USD",
                Status = "OPEN",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 5
            }
        };
    }

    private List<TradingSession> GetStandardTradingSessions(Dictionary<string, Guid> marketLookup)
    {
        var sessions = new List<TradingSession>();

        // BIST Sessions (Monday to Friday)
        if (marketLookup.TryGetValue("BIST", out var bistMarketId))
        {
            for (int dayOfWeek = 1; dayOfWeek <= 5; dayOfWeek++) // Monday to Friday
            {
                sessions.Add(new TradingSession
                {
                    MarketId = bistMarketId,
                    SessionName = "Regular Trading",
                    SessionType = "REGULAR",
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeOnly(9, 30),
                    EndTime = new TimeOnly(18, 10),
                    IsPrimary = true,
                    IsTradingEnabled = true,
                    IsActive = true,
                    DisplayOrder = 1
                });

                sessions.Add(new TradingSession
                {
                    MarketId = bistMarketId,
                    SessionName = "Pre-Market",
                    SessionType = "PRE_MARKET",
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeOnly(9, 00),
                    EndTime = new TimeOnly(9, 30),
                    IsPrimary = false,
                    IsTradingEnabled = true,
                    IsActive = true,
                    DisplayOrder = 2
                });
            }
        }

        // NASDAQ Sessions (Monday to Friday)
        if (marketLookup.TryGetValue("NASDAQ", out var nasdaqMarketId))
        {
            for (int dayOfWeek = 1; dayOfWeek <= 5; dayOfWeek++)
            {
                sessions.Add(new TradingSession
                {
                    MarketId = nasdaqMarketId,
                    SessionName = "Regular Trading",
                    SessionType = "REGULAR",
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeOnly(9, 30),
                    EndTime = new TimeOnly(16, 0),
                    IsPrimary = true,
                    IsTradingEnabled = true,
                    IsActive = true,
                    DisplayOrder = 1
                });

                sessions.Add(new TradingSession
                {
                    MarketId = nasdaqMarketId,
                    SessionName = "Pre-Market",
                    SessionType = "PRE_MARKET",
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeOnly(4, 0),
                    EndTime = new TimeOnly(9, 30),
                    IsPrimary = false,
                    IsTradingEnabled = true,
                    IsActive = true,
                    DisplayOrder = 2
                });

                sessions.Add(new TradingSession
                {
                    MarketId = nasdaqMarketId,
                    SessionName = "After Hours",
                    SessionType = "AFTER_HOURS",
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeOnly(16, 0),
                    EndTime = new TimeOnly(20, 0),
                    IsPrimary = false,
                    IsTradingEnabled = true,
                    IsActive = true,
                    DisplayOrder = 3
                });
            }
        }

        // Similar sessions for NYSE would be added here
        // Crypto markets (24/7) would have continuous sessions

        return sessions;
    }

    private List<DataProvider> GetStandardDataProviders(Dictionary<string, Guid> marketLookup)
    {
        var providers = new List<DataProvider>();

        // Yahoo Finance - Multi-market provider
        foreach (var market in marketLookup)
        {
            if (market.Key != "BINANCE") // Yahoo Finance doesn't provide Binance data well
            {
                providers.Add(new DataProvider
                {
                    Code = $"YAHOO_FINANCE_{market.Key}",
                    Name = $"Yahoo Finance for {market.Key}",
                    Description = $"Yahoo Finance data provider for {market.Key} market",
                    MarketId = market.Value,
                    ProviderType = "REST_API",
                    FeedType = "DELAYED",
                    EndpointUrl = "https://query1.finance.yahoo.com",
                    AuthType = "NONE",
                    RateLimitPerMinute = 100,
                    TimeoutSeconds = 30,
                    MaxRetries = 3,
                    DataDelayMinutes = 15,
                    ConnectionStatus = "DISCONNECTED",
                    IsActive = true,
                    IsPrimary = market.Key == "NASDAQ" || market.Key == "NYSE",
                    Priority = market.Key == "BIST" ? 5 : 2 // Lower priority for BIST
                });
            }
        }

        // Binance API for crypto
        if (marketLookup.TryGetValue("BINANCE", out var binanceMarketId))
        {
            providers.Add(new DataProvider
            {
                Code = "BINANCE_API",
                Name = "Binance API",
                Description = "Official Binance REST and WebSocket API",
                MarketId = binanceMarketId,
                ProviderType = "REST_API",
                FeedType = "REALTIME",
                EndpointUrl = "https://api.binance.com",
                WebSocketUrl = "wss://stream.binance.com:9443",
                AuthType = "API_KEY",
                RateLimitPerMinute = 1200,
                TimeoutSeconds = 10,
                MaxRetries = 3,
                DataDelayMinutes = 0,
                ConnectionStatus = "DISCONNECTED",
                IsActive = true,
                IsPrimary = true,
                Priority = 1
            });
        }

        return providers;
    }

    #endregion

    #region Update Methods

    private void UpdateAssetClass(AssetClass existing, AssetClass updated)
    {
        existing.Name = updated.Name;
        existing.NameTurkish = updated.NameTurkish;
        existing.Description = updated.Description;
        existing.PrimaryCurrency = updated.PrimaryCurrency;
        existing.DefaultPricePrecision = updated.DefaultPricePrecision;
        existing.DefaultQuantityPrecision = updated.DefaultQuantityPrecision;
        existing.Supports24x7Trading = updated.Supports24x7Trading;
        existing.SupportsFractional = updated.SupportsFractional;
        existing.MinTradeAmount = updated.MinTradeAmount;
        existing.RegulatoryClass = updated.RegulatoryClass;
        existing.DisplayOrder = updated.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private void UpdateMarket(Market existing, Market updated)
    {
        existing.Name = updated.Name;
        existing.NameTurkish = updated.NameTurkish;
        existing.Description = updated.Description;
        existing.CountryCode = updated.CountryCode;
        existing.Timezone = updated.Timezone;
        existing.PrimaryCurrency = updated.PrimaryCurrency;
        existing.ApiBaseUrl = updated.ApiBaseUrl;
        existing.WebSocketUrl = updated.WebSocketUrl;
        existing.DefaultCommissionRate = updated.DefaultCommissionRate;
        existing.HasRealtimeData = updated.HasRealtimeData;
        existing.DataDelayMinutes = updated.DataDelayMinutes;
        existing.DisplayOrder = updated.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private void UpdateTradingSession(TradingSession existing, TradingSession updated)
    {
        existing.SessionType = updated.SessionType;
        existing.StartTime = updated.StartTime;
        existing.EndTime = updated.EndTime;
        existing.IsPrimary = updated.IsPrimary;
        existing.IsTradingEnabled = updated.IsTradingEnabled;
        existing.VolumeMultiplier = updated.VolumeMultiplier;
        existing.DisplayOrder = updated.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private void UpdateDataProvider(DataProvider existing, DataProvider updated)
    {
        existing.Name = updated.Name;
        existing.Description = updated.Description;
        existing.ProviderType = updated.ProviderType;
        existing.FeedType = updated.FeedType;
        existing.EndpointUrl = updated.EndpointUrl;
        existing.WebSocketUrl = updated.WebSocketUrl;
        existing.AuthType = updated.AuthType;
        existing.RateLimitPerMinute = updated.RateLimitPerMinute;
        existing.TimeoutSeconds = updated.TimeoutSeconds;
        existing.MaxRetries = updated.MaxRetries;
        existing.DataDelayMinutes = updated.DataDelayMinutes;
        existing.Priority = updated.Priority;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}