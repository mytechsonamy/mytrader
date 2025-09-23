using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Models;

namespace MyTrader.Infrastructure.Data;

/// <summary>
/// Sample data seeder for multi-asset support
/// Seeds BIST and NASDAQ symbols with trading sessions and data providers
/// </summary>
public static class SampleDataSeeder
{
    public static async Task SeedMultiAssetDataAsync(TradingDbContext context)
    {
        // Skip seeding if data already exists
        if (await context.AssetClasses.AnyAsync())
            return;

        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Seed Asset Classes
            await SeedAssetClassesAsync(context);

            // Seed Markets
            await SeedMarketsAsync(context);

            // Seed Trading Sessions
            await SeedTradingSessionsAsync(context);

            // Seed Data Providers
            await SeedDataProvidersAsync(context);

            // Seed Sample Symbols
            await SeedSampleSymbolsAsync(context);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception($"Failed to seed multi-asset data: {ex.Message}", ex);
        }
    }

    private static async Task SeedAssetClassesAsync(TradingDbContext context)
    {
        var assetClasses = new List<AssetClass>
        {
            new AssetClass
            {
                Id = AssetClassIds.Crypto,
                Code = "CRYPTO",
                Name = "Cryptocurrency",
                NameTurkish = "Kripto Para",
                Description = "Digital cryptocurrencies and tokens",
                PrimaryCurrency = "USDT",
                DefaultPricePrecision = 8,
                DefaultQuantityPrecision = 8,
                Supports24x7Trading = true,
                SupportsFractional = true,
                RegulatoryClass = "unregulated",
                IsActive = true,
                DisplayOrder = 1
            },
            new AssetClass
            {
                Id = AssetClassIds.StockBist,
                Code = "STOCK_BIST",
                Name = "Turkish Stocks (BIST)",
                NameTurkish = "Türk Hisse Senetleri (BIST)",
                Description = "Turkish stock market - Borsa Istanbul",
                PrimaryCurrency = "TRY",
                DefaultPricePrecision = 4,
                DefaultQuantityPrecision = 0,
                Supports24x7Trading = false,
                SupportsFractional = false,
                MinTradeAmount = 100m,
                RegulatoryClass = "regulated",
                IsActive = true,
                DisplayOrder = 2
            },
            new AssetClass
            {
                Id = AssetClassIds.StockNasdaq,
                Code = "STOCK_NASDAQ",
                Name = "US NASDAQ Stocks",
                NameTurkish = "ABD NASDAQ Hisse Senetleri",
                Description = "US NASDAQ stock market",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 4,
                DefaultQuantityPrecision = 2,
                Supports24x7Trading = false,
                SupportsFractional = true,
                MinTradeAmount = 1m,
                RegulatoryClass = "sec_regulated",
                IsActive = true,
                DisplayOrder = 3
            },
            new AssetClass
            {
                Id = AssetClassIds.StockNyse,
                Code = "STOCK_NYSE",
                Name = "US NYSE Stocks",
                NameTurkish = "ABD NYSE Hisse Senetleri",
                Description = "US New York Stock Exchange",
                PrimaryCurrency = "USD",
                DefaultPricePrecision = 4,
                DefaultQuantityPrecision = 2,
                Supports24x7Trading = false,
                SupportsFractional = true,
                MinTradeAmount = 1m,
                RegulatoryClass = "sec_regulated",
                IsActive = true,
                DisplayOrder = 4
            }
        };

        await context.AssetClasses.AddRangeAsync(assetClasses);
        await context.SaveChangesAsync();
    }

    private static async Task SeedMarketsAsync(TradingDbContext context)
    {
        var markets = new List<Market>
        {
            new Market
            {
                Id = MarketIds.Binance,
                Code = "BINANCE",
                Name = "Binance",
                NameTurkish = "Binance",
                AssetClassId = AssetClassIds.Crypto,
                CountryCode = "GLOBAL",
                Timezone = "UTC",
                PrimaryCurrency = "USDT",
                ApiBaseUrl = "https://api.binance.com",
                WebSocketUrl = "wss://stream.binance.com:9443/ws",
                Status = "OPEN",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 1
            },
            new Market
            {
                Id = MarketIds.Bist,
                Code = "BIST",
                Name = "Borsa Istanbul",
                NameTurkish = "Borsa İstanbul",
                AssetClassId = AssetClassIds.StockBist,
                CountryCode = "TR",
                Timezone = "Europe/Istanbul",
                PrimaryCurrency = "TRY",
                DefaultCommissionRate = 0.002m, // 0.2%
                Status = "CLOSED",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 15,
                DisplayOrder = 2
            },
            new Market
            {
                Id = MarketIds.Nasdaq,
                Code = "NASDAQ",
                Name = "NASDAQ Global Select Market",
                NameTurkish = "NASDAQ Küresel Seçkin Pazar",
                AssetClassId = AssetClassIds.StockNasdaq,
                CountryCode = "US",
                Timezone = "America/New_York",
                PrimaryCurrency = "USD",
                DefaultCommissionRate = 0.001m, // 0.1%
                Status = "CLOSED",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 3
            },
            new Market
            {
                Id = MarketIds.Nyse,
                Code = "NYSE",
                Name = "New York Stock Exchange",
                NameTurkish = "New York Menkul Kıymetler Borsası",
                AssetClassId = AssetClassIds.StockNyse,
                CountryCode = "US",
                Timezone = "America/New_York",
                PrimaryCurrency = "USD",
                DefaultCommissionRate = 0.001m, // 0.1%
                Status = "CLOSED",
                IsActive = true,
                HasRealtimeData = true,
                DataDelayMinutes = 0,
                DisplayOrder = 4
            }
        };

        await context.Markets.AddRangeAsync(markets);
        await context.SaveChangesAsync();
    }

    private static async Task SeedTradingSessionsAsync(TradingDbContext context)
    {
        var tradingSessions = new List<TradingSession>
        {
            // BIST Trading Sessions
            new TradingSession
            {
                MarketId = MarketIds.Bist,
                SessionName = "Regular Session",
                SessionType = "REGULAR",
                StartTime = new TimeOnly(10, 0), // 10:00 Istanbul time
                EndTime = new TimeOnly(17, 30),   // 17:30 Istanbul time
                IsPrimary = true,
                IsTradingEnabled = true,
                VolumeMultiplier = 1.0m,
                IsActive = true,
                DisplayOrder = 1
            },
            new TradingSession
            {
                MarketId = MarketIds.Bist,
                SessionName = "Closing Session",
                SessionType = "EXTENDED",
                StartTime = new TimeOnly(17, 30),
                EndTime = new TimeOnly(18, 0),
                IsPrimary = false,
                IsTradingEnabled = true,
                VolumeMultiplier = 0.1m,
                IsActive = true,
                DisplayOrder = 2
            },

            // NASDAQ Trading Sessions
            new TradingSession
            {
                MarketId = MarketIds.Nasdaq,
                SessionName = "Pre-Market",
                SessionType = "PRE_MARKET",
                StartTime = new TimeOnly(4, 0),  // 04:00 ET
                EndTime = new TimeOnly(9, 30),   // 09:30 ET
                IsPrimary = false,
                IsTradingEnabled = true,
                VolumeMultiplier = 0.3m,
                IsActive = true,
                DisplayOrder = 1
            },
            new TradingSession
            {
                MarketId = MarketIds.Nasdaq,
                SessionName = "Regular Session",
                SessionType = "REGULAR",
                StartTime = new TimeOnly(9, 30),  // 09:30 ET
                EndTime = new TimeOnly(16, 0),    // 16:00 ET
                IsPrimary = true,
                IsTradingEnabled = true,
                VolumeMultiplier = 1.0m,
                IsActive = true,
                DisplayOrder = 2
            },
            new TradingSession
            {
                MarketId = MarketIds.Nasdaq,
                SessionName = "After Hours",
                SessionType = "AFTER_HOURS",
                StartTime = new TimeOnly(16, 0),  // 16:00 ET
                EndTime = new TimeOnly(20, 0),    // 20:00 ET
                IsPrimary = false,
                IsTradingEnabled = true,
                VolumeMultiplier = 0.2m,
                IsActive = true,
                DisplayOrder = 3
            },

            // NYSE Trading Sessions (similar to NASDAQ)
            new TradingSession
            {
                MarketId = MarketIds.Nyse,
                SessionName = "Pre-Market",
                SessionType = "PRE_MARKET",
                StartTime = new TimeOnly(4, 0),
                EndTime = new TimeOnly(9, 30),
                IsPrimary = false,
                IsTradingEnabled = true,
                VolumeMultiplier = 0.3m,
                IsActive = true,
                DisplayOrder = 1
            },
            new TradingSession
            {
                MarketId = MarketIds.Nyse,
                SessionName = "Regular Session",
                SessionType = "REGULAR",
                StartTime = new TimeOnly(9, 30),
                EndTime = new TimeOnly(16, 0),
                IsPrimary = true,
                IsTradingEnabled = true,
                VolumeMultiplier = 1.0m,
                IsActive = true,
                DisplayOrder = 2
            },
            new TradingSession
            {
                MarketId = MarketIds.Nyse,
                SessionName = "After Hours",
                SessionType = "AFTER_HOURS",
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(20, 0),
                IsPrimary = false,
                IsTradingEnabled = true,
                VolumeMultiplier = 0.2m,
                IsActive = true,
                DisplayOrder = 3
            }
        };

        await context.TradingSessions.AddRangeAsync(tradingSessions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDataProvidersAsync(TradingDbContext context)
    {
        var dataProviders = new List<DataProvider>
        {
            // Binance WebSocket for Crypto
            new DataProvider
            {
                Code = "BINANCE_WS",
                Name = "Binance WebSocket",
                Description = "Real-time cryptocurrency data from Binance",
                MarketId = MarketIds.Binance,
                ProviderType = "REALTIME",
                FeedType = "WEBSOCKET",
                EndpointUrl = "https://api.binance.com/api/v3",
                WebSocketUrl = "wss://stream.binance.com:9443/ws",
                AuthType = "NONE",
                TimeoutSeconds = 30,
                MaxRetries = 3,
                RetryDelayMs = 1000,
                DataDelayMinutes = 0,
                SupportedDataTypes = "[\"OHLCV\", \"TRADES\", \"TICKER\", \"ORDERBOOK\"]",
                ConnectionStatus = "CONNECTED",
                IsActive = true,
                IsPrimary = true,
                Priority = 1
            },

            // Yahoo Finance for BIST (delayed data)
            new DataProvider
            {
                Code = "YAHOO_BIST",
                Name = "Yahoo Finance BIST",
                Description = "BIST stock data from Yahoo Finance (delayed)",
                MarketId = MarketIds.Bist,
                ProviderType = "DELAYED",
                FeedType = "REST_API",
                EndpointUrl = "https://query1.finance.yahoo.com/v8/finance/chart",
                AuthType = "NONE",
                TimeoutSeconds = 10,
                MaxRetries = 3,
                RetryDelayMs = 2000,
                DataDelayMinutes = 15,
                SupportedDataTypes = "[\"OHLCV\", \"TICKER\"]",
                ConnectionStatus = "CONNECTED",
                IsActive = true,
                IsPrimary = true,
                Priority = 1
            },

            // Alpha Vantage for US Stocks
            new DataProvider
            {
                Code = "ALPHA_VANTAGE_US",
                Name = "Alpha Vantage US Stocks",
                Description = "US stock market data from Alpha Vantage",
                MarketId = MarketIds.Nasdaq,
                ProviderType = "DELAYED",
                FeedType = "REST_API",
                EndpointUrl = "https://www.alphavantage.co/query",
                AuthType = "API_KEY",
                TimeoutSeconds = 15,
                MaxRetries = 3,
                RetryDelayMs = 1500,
                DataDelayMinutes = 0,
                RateLimitPerMinute = 5, // Free tier limit
                SupportedDataTypes = "[\"OHLCV\", \"TICKER\", \"FUNDAMENTAL\"]",
                ConnectionStatus = "DISCONNECTED",
                IsActive = true,
                IsPrimary = true,
                Priority = 1,
                MonthlyLimit = 500
            }
        };

        await context.DataProviders.AddRangeAsync(dataProviders);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleSymbolsAsync(TradingDbContext context)
    {
        var symbols = new List<Symbol>
        {
            // Popular Crypto symbols
            new Symbol
            {
                Ticker = "BTCUSDT",
                Venue = "BINANCE",
                AssetClass = "CRYPTO",
                AssetClassId = AssetClassIds.Crypto,
                MarketId = MarketIds.Binance,
                BaseCurrency = "BTC",
                QuoteCurrency = "USDT",
                FullName = "Bitcoin",
                FullNameTurkish = "Bitcoin",
                Display = "BTC/USDT",
                Description = "The first and most well-known cryptocurrency",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                PricePrecision = 2,
                QuantityPrecision = 6,
                TickSize = 0.01m,
                StepSize = 0.000001m,
                DisplayOrder = 1
            },
            new Symbol
            {
                Ticker = "ETHUSDT",
                Venue = "BINANCE",
                AssetClass = "CRYPTO",
                AssetClassId = AssetClassIds.Crypto,
                MarketId = MarketIds.Binance,
                BaseCurrency = "ETH",
                QuoteCurrency = "USDT",
                FullName = "Ethereum",
                FullNameTurkish = "Ethereum",
                Display = "ETH/USDT",
                Description = "Second-largest cryptocurrency by market cap",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                PricePrecision = 2,
                QuantityPrecision = 5,
                TickSize = 0.01m,
                StepSize = 0.00001m,
                DisplayOrder = 2
            },

            // Popular BIST stocks
            new Symbol
            {
                Ticker = "THYAO",
                Venue = "BIST",
                AssetClass = "STOCK_BIST",
                AssetClassId = AssetClassIds.StockBist,
                MarketId = MarketIds.Bist,
                BaseCurrency = "TRY",
                QuoteCurrency = "TRY",
                FullName = "Türk Hava Yolları",
                FullNameTurkish = "Türk Hava Yolları A.O.",
                Display = "THYAO",
                Description = "Turkish Airlines",
                Sector = "Transportation",
                Industry = "Airlines",
                Country = "TR",
                ISIN = "TRAEREGL91E7",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                PricePrecision = 2,
                QuantityPrecision = 0,
                TickSize = 0.01m,
                StepSize = 1m,
                DisplayOrder = 1
            },
            new Symbol
            {
                Ticker = "AKBNK",
                Venue = "BIST",
                AssetClass = "STOCK_BIST",
                AssetClassId = AssetClassIds.StockBist,
                MarketId = MarketIds.Bist,
                BaseCurrency = "TRY",
                QuoteCurrency = "TRY",
                FullName = "Akbank",
                FullNameTurkish = "Akbank T.A.Ş.",
                Display = "AKBNK",
                Description = "Turkish commercial bank",
                Sector = "Financial Services",
                Industry = "Banking",
                Country = "TR",
                ISIN = "TRAAKBNK91N6",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                PricePrecision = 2,
                QuantityPrecision = 0,
                TickSize = 0.01m,
                StepSize = 1m,
                DisplayOrder = 2
            },
            new Symbol
            {
                Ticker = "BIMAS",
                Venue = "BIST",
                AssetClass = "STOCK_BIST",
                AssetClassId = AssetClassIds.StockBist,
                MarketId = MarketIds.Bist,
                BaseCurrency = "TRY",
                QuoteCurrency = "TRY",
                FullName = "BİM Birleşik Mağazalar",
                FullNameTurkish = "BİM Birleşik Mağazalar A.Ş.",
                Display = "BIMAS",
                Description = "Turkish retail chain",
                Sector = "Consumer Defensive",
                Industry = "Discount Stores",
                Country = "TR",
                ISIN = "TRABIMAS90E7",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                PricePrecision = 2,
                QuantityPrecision = 0,
                TickSize = 0.02m,
                StepSize = 1m,
                DisplayOrder = 3
            },

            // Popular NASDAQ stocks
            new Symbol
            {
                Ticker = "AAPL",
                Venue = "NASDAQ",
                AssetClass = "STOCK_NASDAQ",
                AssetClassId = AssetClassIds.StockNasdaq,
                MarketId = MarketIds.Nasdaq,
                BaseCurrency = "USD",
                QuoteCurrency = "USD",
                FullName = "Apple Inc.",
                FullNameTurkish = "Apple A.Ş.",
                Display = "AAPL",
                Description = "Technology company - Consumer electronics",
                Sector = "Technology",
                Industry = "Consumer Electronics",
                Country = "US",
                ISIN = "US0378331005",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                PricePrecision = 2,
                QuantityPrecision = 2,
                TickSize = 0.01m,
                StepSize = 0.01m,
                DisplayOrder = 1
            },
            new Symbol
            {
                Ticker = "GOOGL",
                Venue = "NASDAQ",
                AssetClass = "STOCK_NASDAQ",
                AssetClassId = AssetClassIds.StockNasdaq,
                MarketId = MarketIds.Nasdaq,
                BaseCurrency = "USD",
                QuoteCurrency = "USD",
                FullName = "Alphabet Inc.",
                FullNameTurkish = "Alphabet A.Ş.",
                Display = "GOOGL",
                Description = "Technology company - Internet services",
                Sector = "Communication Services",
                Industry = "Internet Content & Information",
                Country = "US",
                ISIN = "US02079K3059",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                PricePrecision = 2,
                QuantityPrecision = 2,
                TickSize = 0.01m,
                StepSize = 0.01m,
                DisplayOrder = 2
            },
            new Symbol
            {
                Ticker = "MSFT",
                Venue = "NASDAQ",
                AssetClass = "STOCK_NASDAQ",
                AssetClassId = AssetClassIds.StockNasdaq,
                MarketId = MarketIds.Nasdaq,
                BaseCurrency = "USD",
                QuoteCurrency = "USD",
                FullName = "Microsoft Corporation",
                FullNameTurkish = "Microsoft Şirketi",
                Display = "MSFT",
                Description = "Technology company - Software",
                Sector = "Technology",
                Industry = "Software - Infrastructure",
                Country = "US",
                ISIN = "US5949181045",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                PricePrecision = 2,
                QuantityPrecision = 2,
                TickSize = 0.01m,
                StepSize = 0.01m,
                DisplayOrder = 3
            }
        };

        await context.Symbols.AddRangeAsync(symbols);
        await context.SaveChangesAsync();
    }

    // Static IDs for reference consistency
    public static class AssetClassIds
    {
        public static readonly Guid Crypto = new("11111111-1111-1111-1111-111111111111");
        public static readonly Guid StockBist = new("22222222-2222-2222-2222-222222222222");
        public static readonly Guid StockNasdaq = new("33333333-3333-3333-3333-333333333333");
        public static readonly Guid StockNyse = new("44444444-4444-4444-4444-444444444444");
    }

    public static class MarketIds
    {
        public static readonly Guid Binance = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid Bist = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid Nasdaq = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid Nyse = new("dddddddd-dddd-dddd-dddd-dddddddddddd");
    }
}