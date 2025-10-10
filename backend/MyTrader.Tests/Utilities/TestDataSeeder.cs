using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using System;
using System.Collections.Generic;

namespace MyTrader.Tests.Utilities;

public static class TestDataSeeder
{
    public static void SeedDatabase(TradingDbContext context)
    {
        // Clear existing data
        context.MarketData.RemoveRange(context.MarketData);
        context.Users.RemoveRange(context.Users);
        context.Symbols.RemoveRange(context.Symbols);
        context.SaveChanges();

        // Seed test users
        var testUsers = new List<User>
        {
            new User
            {
                Id = 1,
                Username = "testuser1",
                Email = "test1@example.com",
                PasswordHash = "hashedpassword1",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                IsActive = true,
                Points = 1000
            },
            new User
            {
                Id = 2,
                Username = "testuser2",
                Email = "test2@example.com",
                PasswordHash = "hashedpassword2",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                IsActive = true,
                Points = 1500
            }
        };

        context.Users.AddRange(testUsers);

        // Seed test symbols
        var testSymbols = new List<Symbol>
        {
            new Symbol
            {
                Id = 1,
                SymbolName = "AAPL",
                CompanyName = "Apple Inc.",
                AssetClass = "Stock",
                Exchange = "NASDAQ",
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Symbol
            {
                Id = 2,
                SymbolName = "BTC-USD",
                CompanyName = "Bitcoin",
                AssetClass = "Crypto",
                Exchange = "Binance",
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Symbol
            {
                Id = 3,
                SymbolName = "MSFT",
                CompanyName = "Microsoft Corporation",
                AssetClass = "Stock",
                Exchange = "NASDAQ",
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        context.Symbols.AddRange(testSymbols);

        // Seed test market data
        var testMarketData = new List<MarketData>
        {
            new MarketData
            {
                Id = 1,
                SymbolId = 1,
                Price = 150.25m,
                Volume = 1000000,
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Change = 2.50m,
                ChangePercent = 1.69m,
                DayHigh = 152.00m,
                DayLow = 148.00m,
                Open = 149.00m,
                PreviousClose = 147.75m
            },
            new MarketData
            {
                Id = 2,
                SymbolId = 2,
                Price = 45000.50m,
                Volume = 500000,
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Change = 1000.25m,
                ChangePercent = 2.27m,
                DayHigh = 46000.00m,
                DayLow = 44500.00m,
                Open = 44800.00m,
                PreviousClose = 44000.25m
            }
        };

        context.MarketData.AddRange(testMarketData);
        context.SaveChanges();
    }

    public static User CreateTestUser(string username = "testuser", string email = "test@example.com")
    {
        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Points = 1000
        };
    }

    public static Symbol CreateTestSymbol(string symbolName = "TEST", string assetClass = "Stock")
    {
        return new Symbol
        {
            SymbolName = symbolName,
            CompanyName = $"{symbolName} Corporation",
            AssetClass = assetClass,
            Exchange = "NYSE",
            Currency = "USD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static MarketData CreateTestMarketData(int symbolId, decimal price = 100.00m)
    {
        return new MarketData
        {
            SymbolId = symbolId,
            Price = price,
            Volume = 100000,
            Timestamp = DateTime.UtcNow,
            Change = 1.25m,
            ChangePercent = 1.27m,
            DayHigh = price + 5,
            DayLow = price - 5,
            Open = price - 1,
            PreviousClose = price - 1.25m
        };
    }
}