using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Data;
using MyTrader.Core.Models;
using MyTrader.Core.Services;
using MyTrader.Core.Services.ETL;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Api.Services;

/// <summary>
/// Service to initialize database and populate essential data on startup
/// Ensures dashboard has data immediately after API start
/// </summary>
public class DatabaseInitializationService
{
    private readonly ITradingDbContext _dbContext;
    private readonly IMarketDataBootstrapService _bootstrapService;
    private readonly YahooFinanceApiService _yahooFinanceService;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        ITradingDbContext dbContext,
        IMarketDataBootstrapService bootstrapService,
        YahooFinanceApiService yahooFinanceService,
        ILogger<DatabaseInitializationService> logger)
    {
        _dbContext = dbContext;
        _bootstrapService = bootstrapService;
        _yahooFinanceService = yahooFinanceService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize database with all required reference data and sample market data
    /// </summary>
    public async Task InitializeAsync()
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting database initialization");

        try
        {
            // Step 1: Ensure database exists and migrations are applied
            await EnsureDatabaseExistsAsync();

            // Step 2: Bootstrap reference data
            await BootstrapReferenceDataAsync();

            // Step 3: Seed symbols if empty
            await SeedSymbolsAsync();

            // Step 4: Populate initial market data
            await PopulateInitialMarketDataAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Database initialization completed successfully in {Duration}", duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// Ensure database exists and apply pending migrations
    /// </summary>
    private async Task EnsureDatabaseExistsAsync()
    {
        try
        {
            _logger.LogInformation("Ensuring database exists and migrations are applied");

            if (_dbContext is TradingDbContext efContext)
            {
                await efContext.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    /// <summary>
    /// Bootstrap all reference data (asset classes, markets, etc.)
    /// </summary>
    private async Task BootstrapReferenceDataAsync()
    {
        try
        {
            _logger.LogInformation("Bootstrapping reference data");

            var result = await _bootstrapService.BootstrapAllReferenceDataAsync();

            if (result.Success)
            {
                _logger.LogInformation("Reference data bootstrap completed. Created: {Created}, Updated: {Updated}",
                    result.TotalItemsCreated, result.TotalItemsUpdated);
            }
            else
            {
                _logger.LogWarning("Reference data bootstrap had issues: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap reference data");
            // Don't throw - continue with symbol seeding
        }
    }

    /// <summary>
    /// Seed symbols if the symbols table is empty
    /// </summary>
    private async Task SeedSymbolsAsync()
    {
        try
        {
            var symbolCount = await _dbContext.Symbols.CountAsync();
            if (symbolCount == 0)
            {
                _logger.LogInformation("Symbols table is empty, seeding default symbols");

                var seederService = new DatabaseSeederService(_dbContext,
                    LoggerFactory.Create(builder => builder.AddConsole())
                        .CreateLogger<DatabaseSeederService>());

                await seederService.SeedAllDataAsync();
                _logger.LogInformation("Default symbols seeded successfully");
            }
            else
            {
                _logger.LogInformation("Symbols table contains {Count} symbols, skipping seed", symbolCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed symbols");
            // Don't throw - continue with market data population
        }
    }

    /// <summary>
    /// Populate initial market data for dashboard display
    /// Gets recent data for popular symbols to ensure dashboard shows data immediately
    /// </summary>
    private async Task PopulateInitialMarketDataAsync()
    {
        try
        {
            var marketDataCount = await _dbContext.MarketData.CountAsync();
            if (marketDataCount > 0)
            {
                _logger.LogInformation("Market data table contains {Count} records, checking freshness", marketDataCount);

                // Check if we have recent data (within last hour)
                var recentDataCount = await _dbContext.MarketData
                    .CountAsync(md => md.Timestamp >= DateTime.UtcNow.AddHours(-1));

                if (recentDataCount > 10)
                {
                    _logger.LogInformation("Found {Count} recent market data records, skipping initial population", recentDataCount);
                    return;
                }
            }

            _logger.LogInformation("Populating initial market data for popular symbols");

            // Get popular symbols for each asset class
            var popularSymbols = await GetPopularSymbolsForInitialDataAsync();
            var successCount = 0;
            var failCount = 0;

            foreach (var symbol in popularSymbols)
            {
                try
                {
                    await PopulateSymbolMarketDataAsync(symbol);
                    successCount++;

                    // Add small delay to respect rate limits
                    await Task.Delay(250);
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogWarning(ex, "Failed to populate market data for symbol: {Symbol}", symbol.Ticker);
                }
            }

            _logger.LogInformation("Initial market data population completed. Success: {Success}, Failed: {Failed}",
                successCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to populate initial market data");
            // Don't throw - this is not critical for application startup
        }
    }

    /// <summary>
    /// Get popular symbols for initial data population
    /// </summary>
    private async Task<List<Symbol>> GetPopularSymbolsForInitialDataAsync()
    {
        var symbols = new List<Symbol>();

        try
        {
            // Get top symbols from each asset class
            var popularStocks = await _dbContext.Symbols
                .Where(s => s.IsActive && s.AssetClass == "STOCK" && s.IsPopular)
                .Take(5)
                .ToListAsync();
            symbols.AddRange(popularStocks);

            var popularCrypto = await _dbContext.Symbols
                .Where(s => s.IsActive && s.AssetClass == "CRYPTO" && s.IsPopular)
                .Take(5)
                .ToListAsync();
            symbols.AddRange(popularCrypto);

            var popularBist = await _dbContext.Symbols
                .Where(s => s.IsActive && (s.AssetClass == "BIST" || s.AssetClass == "STOCK_BIST") && s.IsPopular)
                .Take(3)
                .ToListAsync();
            symbols.AddRange(popularBist);

            _logger.LogInformation("Selected {Count} popular symbols for initial data population", symbols.Count);
            return symbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get popular symbols");
            return new List<Symbol>();
        }
    }

    /// <summary>
    /// Populate market data for a specific symbol
    /// </summary>
    private async Task PopulateSymbolMarketDataAsync(Symbol symbol)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-1); // Get last day of data

            // Determine market based on asset class
            var market = symbol.AssetClass switch
            {
                "STOCK" => symbol.Venue ?? "NASDAQ",
                "CRYPTO" => "CRYPTO",
                "BIST" or "STOCK_BIST" => "BIST",
                _ => "NASDAQ"
            };

            // Get latest data from Yahoo Finance
            var result = await _yahooFinanceService.GetHistoricalDataAsync(
                symbol.Ticker, startDate, endDate, market);

            if (result.Success && result.Data?.Any() == true)
            {
                var latestData = result.Data.OrderByDescending(d => d.TradeDate).FirstOrDefault();
                if (latestData != null)
                {
                    // Create market data record
                    var marketData = new MarketData
                    {
                        Id = Guid.NewGuid(),
                        Symbol = symbol.Ticker,
                        Timeframe = "1d",
                        Timestamp = latestData.Timestamp ?? DateTime.UtcNow,
                        Open = latestData.OpenPrice ?? 0,
                        High = latestData.HighPrice ?? 0,
                        Low = latestData.LowPrice ?? 0,
                        Close = latestData.ClosePrice ?? 0,
                        Volume = latestData.Volume ?? 0,
                        AssetClass = symbol.AssetClass
                    };

                    _dbContext.MarketData.Add(marketData);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogDebug("Populated market data for {Symbol}: Close={Close}, Volume={Volume}",
                        symbol.Ticker, marketData.Close, marketData.Volume);
                }
            }
            else
            {
                _logger.LogWarning("No data received for symbol: {Symbol}, Error: {Error}",
                    symbol.Ticker, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating market data for symbol: {Symbol}", symbol.Ticker);
            throw;
        }
    }

    /// <summary>
    /// Get initialization status for health checks
    /// </summary>
    public async Task<DatabaseInitializationStatus> GetStatusAsync()
    {
        try
        {
            var status = new DatabaseInitializationStatus();

            // Check table counts
            status.SymbolCount = await _dbContext.Symbols.CountAsync();
            status.MarketDataCount = await _dbContext.MarketData.CountAsync();
            status.AssetClassCount = await _dbContext.AssetClasses.CountAsync();
            status.MarketCount = await _dbContext.Markets.CountAsync();

            // Check data freshness
            var latestMarketData = await _dbContext.MarketData
                .OrderByDescending(md => md.Timestamp)
                .FirstOrDefaultAsync();

            status.LatestMarketDataTime = latestMarketData?.Timestamp;
            status.IsMarketDataFresh = latestMarketData?.Timestamp >= DateTime.UtcNow.AddHours(-2);

            // Overall health
            status.IsHealthy = status.SymbolCount > 10 &&
                              status.MarketDataCount > 0 &&
                              status.AssetClassCount > 3 &&
                              status.MarketCount > 3;

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database initialization status");
            return new DatabaseInitializationStatus
            {
                IsHealthy = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Database initialization status for monitoring
/// </summary>
public class DatabaseInitializationStatus
{
    public bool IsHealthy { get; set; }
    public int SymbolCount { get; set; }
    public int MarketDataCount { get; set; }
    public int AssetClassCount { get; set; }
    public int MarketCount { get; set; }
    public DateTime? LatestMarketDataTime { get; set; }
    public bool IsMarketDataFresh { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}