using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using MyTrader.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Infrastructure;

public class TradingDbContextTests : TestBase
{
    [Fact]
    public async Task CreateContext_ShouldInitializeAllDbSets()
    {
        // Arrange & Act
        using var context = CreateInMemoryDbContext();

        // Assert
        context.Users.Should().NotBeNull();
        context.UserSessions.Should().NotBeNull();
        context.EmailVerifications.Should().NotBeNull();
        context.TempRegistrations.Should().NotBeNull();
        context.MarketData.Should().NotBeNull();
        context.Strategies.Should().NotBeNull();
        context.Signals.Should().NotBeNull();
        context.IndicatorConfigs.Should().NotBeNull();
        context.IndicatorValues.Should().NotBeNull();
        context.BacktestResults.Should().NotBeNull();
        context.TradeHistory.Should().NotBeNull();
        context.PasswordResets.Should().NotBeNull();
        context.Symbols.Should().NotBeNull();
        context.UserStrategies.Should().NotBeNull();
        context.UserNotificationPreferences.Should().NotBeNull();
        context.Candles.Should().NotBeNull();
        context.BacktestQueue.Should().NotBeNull();
        context.UserAchievements.Should().NotBeNull();
        context.StrategyPerformances.Should().NotBeNull();
        context.PriceAlerts.Should().NotBeNull();
        context.NotificationHistory.Should().NotBeNull();
        context.UserPortfolios.Should().NotBeNull();
        context.PortfolioPositions.Should().NotBeNull();
        context.Transactions.Should().NotBeNull();
        context.AssetClasses.Should().NotBeNull();
        context.Markets.Should().NotBeNull();
        context.TradingSessions.Should().NotBeNull();
        context.DataProviders.Should().NotBeNull();
        context.UserDashboardPreferences.Should().NotBeNull();
        context.HistoricalMarketData.Should().NotBeNull();
        context.MarketDataSummaries.Should().NotBeNull();
    }

    [Fact]
    public async Task AddUser_ShouldSaveToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = TestDataSeeder.CreateTestUser("testuser", "test@example.com");

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be("testuser");
        savedUser.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task AddSymbol_ShouldSaveToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var symbol = TestDataSeeder.CreateTestSymbol("AAPL", "Stock");

        // Act
        context.Symbols.Add(symbol);
        await context.SaveChangesAsync();

        // Assert
        var savedSymbol = await context.Symbols.FirstOrDefaultAsync(s => s.SymbolName == "AAPL");
        savedSymbol.Should().NotBeNull();
        savedSymbol!.SymbolName.Should().Be("AAPL");
        savedSymbol.AssetClass.Should().Be("Stock");
        savedSymbol.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddMarketData_WithSymbolId_ShouldSaveToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // First create and save a symbol
        var symbol = TestDataSeeder.CreateTestSymbol("AAPL", "Stock");
        context.Symbols.Add(symbol);
        await context.SaveChangesAsync();

        var marketData = TestDataSeeder.CreateTestMarketData(symbol.Id, 150.25m);

        // Act
        context.MarketData.Add(marketData);
        await context.SaveChangesAsync();

        // Assert
        var savedMarketData = await context.MarketData.FirstOrDefaultAsync(m => m.SymbolId == symbol.Id);
        savedMarketData.Should().NotBeNull();
        savedMarketData!.Price.Should().Be(150.25m);
        savedMarketData.Volume.Should().Be(100000);
    }

    [Fact]
    public async Task QueryUsers_WithFiltering_ShouldReturnFilteredResults()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var users = new List<User>
        {
            new User { Username = "user1", Email = "user1@test.com", IsActive = true, Points = 100, CreatedAt = DateTime.UtcNow },
            new User { Username = "user2", Email = "user2@test.com", IsActive = false, Points = 200, CreatedAt = DateTime.UtcNow },
            new User { Username = "user3", Email = "user3@test.com", IsActive = true, Points = 300, CreatedAt = DateTime.UtcNow }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // Act
        var activeUsers = await context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync();

        // Assert
        activeUsers.Should().HaveCount(2);
        activeUsers[0].Username.Should().Be("user1");
        activeUsers[1].Username.Should().Be("user3");
    }

    [Fact]
    public async Task UpdateUser_ShouldModifyExistingRecord()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = TestDataSeeder.CreateTestUser("originaluser", "original@test.com");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        user.Username = "updateduser";
        user.Points = 2000;
        await context.SaveChangesAsync();

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "original@test.com");
        updatedUser.Should().NotBeNull();
        updatedUser!.Username.Should().Be("updateduser");
        updatedUser.Points.Should().Be(2000);
    }

    [Fact]
    public async Task DeleteUser_ShouldRemoveFromDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = TestDataSeeder.CreateTestUser("deleteuser", "delete@test.com");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        context.Users.Remove(user);
        await context.SaveChangesAsync();

        // Assert
        var deletedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "delete@test.com");
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task AddMarketData_BulkInsert_ShouldHandleLargeDataset()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // Create symbols first
        var symbols = new List<Symbol>();
        for (int i = 1; i <= 10; i++)
        {
            symbols.Add(TestDataSeeder.CreateTestSymbol($"SYM{i}", "Stock"));
        }
        context.Symbols.AddRange(symbols);
        await context.SaveChangesAsync();

        // Create market data
        var marketDataList = new List<MarketData>();
        foreach (var symbol in symbols)
        {
            for (int i = 0; i < 100; i++) // 100 data points per symbol
            {
                marketDataList.Add(new MarketData
                {
                    SymbolId = symbol.Id,
                    Price = 100m + i,
                    Volume = 100000 + (i * 1000),
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Change = i % 2 == 0 ? 1.25m : -0.75m,
                    ChangePercent = (i % 2 == 0 ? 1.25m : -0.75m) / 100m,
                    DayHigh = 105m + i,
                    DayLow = 95m + i,
                    Open = 99m + i,
                    PreviousClose = 98m + i
                });
            }
        }

        // Act
        var startTime = DateTime.UtcNow;
        context.MarketData.AddRange(marketDataList);
        await context.SaveChangesAsync();
        var endTime = DateTime.UtcNow;

        // Assert
        var totalCount = await context.MarketData.CountAsync();
        totalCount.Should().Be(1000); // 10 symbols * 100 data points

        var executionTime = endTime - startTime;
        executionTime.Should().BeLessThan(TimeSpan.FromSeconds(5)); // Performance assertion
    }

    [Fact]
    public async Task QueryMarketData_WithComplexFiltering_ShouldReturnCorrectResults()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        TestDataSeeder.SeedDatabase(context);

        // Act
        var recentHighVolumeData = await context.MarketData
            .Where(m => m.Volume > 500000 && m.Timestamp > DateTime.UtcNow.AddHours(-24))
            .OrderByDescending(m => m.Timestamp)
            .Take(10)
            .ToListAsync();

        // Assert
        recentHighVolumeData.Should().NotBeEmpty();
        recentHighVolumeData.All(m => m.Volume > 500000).Should().BeTrue();

        // Verify ordering
        for (int i = 0; i < recentHighVolumeData.Count - 1; i++)
        {
            recentHighVolumeData[i].Timestamp.Should().BeOnOrAfter(recentHighVolumeData[i + 1].Timestamp);
        }
    }

    [Fact]
    public async Task AddUserAchievement_ShouldSaveCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = TestDataSeeder.CreateTestUser("achievementuser", "achievement@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var achievement = new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(user.Id.ToString()),
            AchievementType = "FIRST_TRADE",
            AchievementName = "First Trade",
            Description = "Made your first trade",
            Points = 100,
            Icon = "🎯",
            EarnedAt = DateTime.UtcNow
        };

        // Act
        context.UserAchievements.Add(achievement);
        await context.SaveChangesAsync();

        // Assert
        var savedAchievement = await context.UserAchievements
            .FirstOrDefaultAsync(a => a.AchievementType == "FIRST_TRADE");

        savedAchievement.Should().NotBeNull();
        savedAchievement!.Points.Should().Be(100);
        savedAchievement.Icon.Should().Be("🎯");
    }

    [Fact]
    public async Task DatabaseTransaction_RollbackOnError_ShouldNotSavePartialData()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // Act & Assert
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Add valid user
            var user1 = TestDataSeeder.CreateTestUser("user1", "user1@test.com");
            context.Users.Add(user1);
            await context.SaveChangesAsync();

            // Add invalid user (simulate constraint violation)
            var user2 = TestDataSeeder.CreateTestUser("user2", "user1@test.com"); // Duplicate email
            context.Users.Add(user2);

            // This should fail due to unique constraint on email
            await Assert.ThrowsAsync<Exception>(async () => await context.SaveChangesAsync());

            await transaction.RollbackAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
        }

        // Verify no users were saved due to rollback
        var userCount = await context.Users.CountAsync();
        userCount.Should().Be(0);
    }

    [Fact]
    public async Task ContextDisposal_ShouldCleanupResources()
    {
        // Arrange
        TradingDbContext context;

        // Act
        using (context = CreateInMemoryDbContext())
        {
            var user = TestDataSeeder.CreateTestUser("disposetest", "dispose@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        // Assert - Context should be disposed
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await context.Users.CountAsync();
        });
    }

    [Theory]
    [InlineData("Stock", "NASDAQ")]
    [InlineData("Crypto", "Binance")]
    [InlineData("Forex", "FOREX")]
    public async Task AddSymbol_DifferentAssetClasses_ShouldSaveCorrectly(string assetClass, string exchange)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var symbol = new Symbol
        {
            SymbolName = $"TEST_{assetClass.ToUpper()}",
            CompanyName = $"Test {assetClass} Company",
            AssetClass = assetClass,
            Exchange = exchange,
            Currency = "USD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        context.Symbols.Add(symbol);
        await context.SaveChangesAsync();

        // Assert
        var savedSymbol = await context.Symbols
            .FirstOrDefaultAsync(s => s.SymbolName == $"TEST_{assetClass.ToUpper()}");

        savedSymbol.Should().NotBeNull();
        savedSymbol!.AssetClass.Should().Be(assetClass);
        savedSymbol.Exchange.Should().Be(exchange);
    }
}