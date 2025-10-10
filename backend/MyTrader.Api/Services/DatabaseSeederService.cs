using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Data;
using MyTrader.Core.Models;

namespace MyTrader.Api.Services;

public class DatabaseSeederService
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger<DatabaseSeederService> _logger;

    public DatabaseSeederService(ITradingDbContext dbContext, ILogger<DatabaseSeederService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAllDataAsync()
    {
        try
        {
            await SeedAssetClassesAsync();
            await SeedMarketsAsync();
            await SeedSymbolsAsync();
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private async Task SeedAssetClassesAsync()
    {
        if (await _dbContext.AssetClasses.AnyAsync())
        {
            _logger.LogInformation("Asset classes already exist, skipping seed");
            return;
        }

        var assetClasses = new List<AssetClass>
        {
            new AssetClass
            {
                Code = "STOCK",
                Name = "Stocks",
                NameTurkish = "Hisse Senetleri",
                Description = "Publicly traded company shares",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 2,
                DefaultQuantityPrecision = 0,
                Supports24x7Trading = false,
                SupportsFractional = true,
                MinTradeAmount = 1.00m,
                IsActive = true,
                DisplayOrder = 1
            },
            new AssetClass
            {
                Code = "CRYPTO",
                Name = "Cryptocurrencies",
                NameTurkish = "Kripto Paralar",
                Description = "Digital cryptocurrencies",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 8,
                DefaultQuantityPrecision = 8,
                Supports24x7Trading = true,
                SupportsFractional = true,
                MinTradeAmount = 0.00000001m,
                IsActive = true,
                DisplayOrder = 2
            },
            new AssetClass
            {
                Code = "FOREX",
                Name = "Foreign Exchange",
                NameTurkish = "Döviz",
                Description = "Currency pairs",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 5,
                DefaultQuantityPrecision = 2,
                Supports24x7Trading = true,
                SupportsFractional = true,
                MinTradeAmount = 1000.00m,
                IsActive = true,
                DisplayOrder = 3
            },
            new AssetClass
            {
                Code = "BIST",
                Name = "BIST Stocks",
                NameTurkish = "BIST Hisseleri",
                Description = "Borsa Istanbul stocks",
                PrimaryCurrency = "TRY",
                DefaultPricePrecision = 2,
                DefaultQuantityPrecision = 0,
                Supports24x7Trading = false,
                SupportsFractional = false,
                MinTradeAmount = 100.00m,
                IsActive = true,
                DisplayOrder = 4
            }
        };

        _dbContext.AssetClasses.AddRange(assetClasses);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation($"Seeded {assetClasses.Count} asset classes");
    }

    private async Task SeedMarketsAsync()
    {
        if (await _dbContext.Markets.AnyAsync())
        {
            _logger.LogInformation("Markets already exist, skipping seed");
            return;
        }

        var stockAssetClass = await _dbContext.AssetClasses.FirstAsync(ac => ac.Code == "STOCK");
        var cryptoAssetClass = await _dbContext.AssetClasses.FirstAsync(ac => ac.Code == "CRYPTO");
        var bistAssetClass = await _dbContext.AssetClasses.FirstAsync(ac => ac.Code == "BIST");
        var forexAssetClass = await _dbContext.AssetClasses.FirstAsync(ac => ac.Code == "FOREX");

        var markets = new List<Market>
        {
            new Market
            {
                Code = "NYSE",
                Name = "New York Stock Exchange",
                NameTurkish = "New York Menkul Kıymetler Borsası",
                Description = "The largest stock exchange in the world by market capitalization",
                CountryCode = "US",
                Timezone = "America/New_York",
                PrimaryCurrency = "USD",
                Status = "OPEN",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 1,
                AssetClassId = stockAssetClass.Id
            },
            new Market
            {
                Code = "NASDAQ",
                Name = "NASDAQ",
                NameTurkish = "NASDAQ",
                Description = "Technology-focused stock exchange",
                CountryCode = "US",
                Timezone = "America/New_York",
                PrimaryCurrency = "USD",
                Status = "OPEN",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 2,
                AssetClassId = stockAssetClass.Id
            },
            new Market
            {
                Code = "CRYPTO",
                Name = "Cryptocurrency Market",
                NameTurkish = "Kripto Para Piyasası",
                Description = "24/7 cryptocurrency trading market",
                CountryCode = "GLOBAL",
                Timezone = "UTC",
                PrimaryCurrency = "USD",
                Status = "OPEN",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 3,
                AssetClassId = cryptoAssetClass.Id
            },
            new Market
            {
                Code = "BIST",
                Name = "Borsa Istanbul",
                NameTurkish = "Borsa İstanbul",
                Description = "Turkish stock exchange",
                CountryCode = "TR",
                Timezone = "Europe/Istanbul",
                PrimaryCurrency = "TRY",
                Status = "OPEN",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 4,
                AssetClassId = bistAssetClass.Id
            },
            new Market
            {
                Code = "FOREX",
                Name = "Foreign Exchange Market",
                NameTurkish = "Döviz Piyasası",
                Description = "Global currency exchange market",
                CountryCode = "GLOBAL",
                Timezone = "UTC",
                PrimaryCurrency = "USD",
                Status = "OPEN",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 5,
                AssetClassId = forexAssetClass.Id
            }
        };

        _dbContext.Markets.AddRange(markets);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation($"Seeded {markets.Count} markets");
    }

    private async Task SeedSymbolsAsync()
    {
        if (await _dbContext.Symbols.AnyAsync())
        {
            _logger.LogInformation("Symbols already exist, skipping seed");
            return;
        }

        // Get market and asset class references
        var nyseMarket = await _dbContext.Markets.FirstAsync(m => m.Code == "NYSE");
        var nasdaqMarket = await _dbContext.Markets.FirstAsync(m => m.Code == "NASDAQ");
        var cryptoMarket = await _dbContext.Markets.FirstAsync(m => m.Code == "CRYPTO");
        var bistMarket = await _dbContext.Markets.FirstAsync(m => m.Code == "BIST");

        var stockAssetClass = await _dbContext.AssetClasses.FirstAsync(ac => ac.Code == "STOCK");
        var cryptoAssetClass = await _dbContext.AssetClasses.FirstAsync(ac => ac.Code == "CRYPTO");
        var bistAssetClass = await _dbContext.AssetClasses.FirstAsync(ac => ac.Code == "BIST");

        var symbols = new List<Symbol>();

        // Add NYSE stocks
        var nyseStocks = new[]
        {
            new { Ticker = "AAPL", Name = "Apple Inc.", Sector = "Technology", Industry = "Consumer Electronics" },
            new { Ticker = "MSFT", Name = "Microsoft Corporation", Sector = "Technology", Industry = "Software" },
            new { Ticker = "GOOGL", Name = "Alphabet Inc.", Sector = "Technology", Industry = "Internet Services" },
            new { Ticker = "AMZN", Name = "Amazon.com Inc.", Sector = "Consumer Discretionary", Industry = "E-commerce" },
            new { Ticker = "TSLA", Name = "Tesla Inc.", Sector = "Consumer Discretionary", Industry = "Electric Vehicles" },
            new { Ticker = "META", Name = "Meta Platforms Inc.", Sector = "Technology", Industry = "Social Media" },
            new { Ticker = "NVDA", Name = "NVIDIA Corporation", Sector = "Technology", Industry = "Semiconductors" },
            new { Ticker = "JPM", Name = "JPMorgan Chase & Co.", Sector = "Financial Services", Industry = "Banking" },
            new { Ticker = "JNJ", Name = "Johnson & Johnson", Sector = "Healthcare", Industry = "Pharmaceuticals" },
            new { Ticker = "PG", Name = "Procter & Gamble Co.", Sector = "Consumer Staples", Industry = "Household Products" }
        };

        foreach (var stock in nyseStocks)
        {
            symbols.Add(new Symbol
            {
                Ticker = stock.Ticker,
                Venue = "NYSE",
                AssetClass = "STOCK",
                AssetClassId = stockAssetClass.Id,
                MarketId = nyseMarket.Id,
                BaseCurrency = "USD",
                QuoteCurrency = "USD",
                FullName = stock.Name,
                Display = $"{stock.Ticker} - {stock.Name}",
                Description = $"{stock.Name} stock traded on NYSE",
                Sector = stock.Sector,
                Industry = stock.Industry,
                Country = "US",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                TickSize = 0.01m,
                PricePrecision = 2,
                QuantityPrecision = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Add NASDAQ stocks
        var nasdaqStocks = new[]
        {
            new { Ticker = "NFLX", Name = "Netflix Inc.", Sector = "Communication Services", Industry = "Streaming" },
            new { Ticker = "AMD", Name = "Advanced Micro Devices Inc.", Sector = "Technology", Industry = "Semiconductors" },
            new { Ticker = "PYPL", Name = "PayPal Holdings Inc.", Sector = "Financial Services", Industry = "Digital Payments" },
            new { Ticker = "INTC", Name = "Intel Corporation", Sector = "Technology", Industry = "Semiconductors" },
            new { Ticker = "CSCO", Name = "Cisco Systems Inc.", Sector = "Technology", Industry = "Networking Equipment" },
            new { Ticker = "ORCL", Name = "Oracle Corporation", Sector = "Technology", Industry = "Enterprise Software" },
            new { Ticker = "CRM", Name = "Salesforce Inc.", Sector = "Technology", Industry = "Cloud Software" },
            new { Ticker = "ZM", Name = "Zoom Video Communications", Sector = "Technology", Industry = "Video Communications" },
            new { Ticker = "DOCU", Name = "DocuSign Inc.", Sector = "Technology", Industry = "Digital Document Solutions" },
            new { Ticker = "UBER", Name = "Uber Technologies Inc.", Sector = "Technology", Industry = "Ride Sharing" }
        };

        foreach (var stock in nasdaqStocks)
        {
            symbols.Add(new Symbol
            {
                Ticker = stock.Ticker,
                Venue = "NASDAQ",
                AssetClass = "STOCK",
                AssetClassId = stockAssetClass.Id,
                MarketId = nasdaqMarket.Id,
                BaseCurrency = "USD",
                QuoteCurrency = "USD",
                FullName = stock.Name,
                Display = $"{stock.Ticker} - {stock.Name}",
                Description = $"{stock.Name} stock traded on NASDAQ",
                Sector = stock.Sector,
                Industry = stock.Industry,
                Country = "US",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                TickSize = 0.01m,
                PricePrecision = 2,
                QuantityPrecision = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Add cryptocurrencies
        var cryptos = new[]
        {
            new { Ticker = "BTCUSD", Name = "Bitcoin", Base = "BTC" },
            new { Ticker = "ETHUSD", Name = "Ethereum", Base = "ETH" },
            new { Ticker = "SOLUSD", Name = "Solana", Base = "SOL" },
            new { Ticker = "AVAXUSD", Name = "Avalanche", Base = "AVAX" },
            new { Ticker = "LINKUSD", Name = "Chainlink", Base = "LINK" },
            new { Ticker = "ADAUSD", Name = "Cardano", Base = "ADA" },
            new { Ticker = "DOTUSD", Name = "Polkadot", Base = "DOT" },
            new { Ticker = "MATICUSD", Name = "Polygon", Base = "MATIC" },
            new { Ticker = "UNIUSD", Name = "Uniswap", Base = "UNI" },
            new { Ticker = "ATOMUSD", Name = "Cosmos", Base = "ATOM" }
        };

        foreach (var crypto in cryptos)
        {
            symbols.Add(new Symbol
            {
                Ticker = crypto.Ticker,
                Venue = "CRYPTO",
                AssetClass = "CRYPTO",
                AssetClassId = cryptoAssetClass.Id,
                MarketId = cryptoMarket.Id,
                BaseCurrency = crypto.Base,
                QuoteCurrency = "USD",
                FullName = crypto.Name,
                Display = $"{crypto.Base}/USD - {crypto.Name}",
                Description = $"{crypto.Name} cryptocurrency",
                Sector = "Cryptocurrency",
                Industry = "Digital Assets",
                Country = "GLOBAL",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                TickSize = 0.00000001m,
                PricePrecision = 8,
                QuantityPrecision = 8,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Add some BIST stocks
        var bistStocks = new[]
        {
            new { Ticker = "AKBNK", Name = "Akbank T.A.Ş.", Sector = "Financial Services", Industry = "Banking" },
            new { Ticker = "ASELS", Name = "Aselsan Elektronik San. ve Tic. A.Ş.", Sector = "Technology", Industry = "Defense Electronics" },
            new { Ticker = "BIMAS", Name = "BİM Birleşik Mağazalar A.Ş.", Sector = "Consumer Staples", Industry = "Retail" },
            new { Ticker = "EREGL", Name = "Ereğli Demir ve Çelik Fabrikaları T.A.Ş.", Sector = "Materials", Industry = "Steel" },
            new { Ticker = "GARAN", Name = "Türkiye Garanti Bankası A.Ş.", Sector = "Financial Services", Industry = "Banking" },
            new { Ticker = "HALKB", Name = "Türkiye Halk Bankası A.Ş.", Sector = "Financial Services", Industry = "Banking" },
            new { Ticker = "ISCTR", Name = "Türkiye İş Bankası A.Ş.", Sector = "Financial Services", Industry = "Banking" },
            new { Ticker = "KCHOL", Name = "Koç Holding A.Ş.", Sector = "Industrials", Industry = "Conglomerates" },
            new { Ticker = "PETKM", Name = "Petkim Petrokimya Holding A.Ş.", Sector = "Materials", Industry = "Chemicals" },
            new { Ticker = "SAHOL", Name = "Hacı Ömer Sabancı Holding A.Ş.", Sector = "Industrials", Industry = "Conglomerates" }
        };

        foreach (var stock in bistStocks)
        {
            symbols.Add(new Symbol
            {
                Ticker = stock.Ticker,
                Venue = "BIST",
                AssetClass = "BIST",
                AssetClassId = bistAssetClass.Id,
                MarketId = bistMarket.Id,
                BaseCurrency = "TRY",
                QuoteCurrency = "TRY",
                FullName = stock.Name,
                Display = $"{stock.Ticker} - {stock.Name}",
                Description = $"{stock.Name} stock traded on BIST",
                Sector = stock.Sector,
                Industry = stock.Industry,
                Country = "TR",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                TickSize = 0.01m,
                PricePrecision = 2,
                QuantityPrecision = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _dbContext.Symbols.AddRange(symbols);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation($"Seeded {symbols.Count} symbols");
    }

    public async Task<object> GetDatabaseCountsAsync()
    {
        var counts = new
        {
            AssetClasses = await _dbContext.AssetClasses.CountAsync(),
            Markets = await _dbContext.Markets.CountAsync(),
            Symbols = await _dbContext.Symbols.CountAsync(),
            MarketData = await _dbContext.MarketData.CountAsync(),
            Users = await _dbContext.Users.CountAsync(),
            UserDashboardPreferences = await _dbContext.UserDashboardPreferences.CountAsync()
        };

        return counts;
    }
}