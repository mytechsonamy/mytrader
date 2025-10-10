using Microsoft.EntityFrameworkCore;
using MyTrader.Infrastructure.Data;
using MyTrader.Core.Models;

namespace MyTrader.Tests.TestBase;

/// <summary>
/// Test-specific database context with in-memory database configuration
/// </summary>
public class TestDbContext : TradingDbContext
{
    public TestDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Seed test data
        SeedTestData(modelBuilder);
    }

    private void SeedTestData(ModelBuilder modelBuilder)
    {
        // Seed test users
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "testuser1@example.com",
                FirstName = "Test",
                LastName = "User1",
                Phone = "+1234567890",
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "testuser2@example.com",
                FirstName = "Test",
                LastName = "User2",
                Phone = "+1234567891",
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        // Seed test symbols
        modelBuilder.Entity<Symbol>().HasData(
            new Symbol
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                SymbolName = "AAPL",
                Name = "Apple Inc.",
                AssetClass = "Stock",
                Exchange = "NASDAQ",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Symbol
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                SymbolName = "BTCUSDT",
                Name = "Bitcoin/USDT",
                AssetClass = "Crypto",
                Exchange = "Binance",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        // Seed test market data
        modelBuilder.Entity<MarketData>().HasData(
            new MarketData
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                SymbolId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Symbol = "AAPL",
                Price = 150.00m,
                Volume = 1000000,
                Change24h = 2.50m,
                ChangePercent24h = 1.69m,
                High24h = 152.00m,
                Low24h = 148.00m,
                Timestamp = DateTime.UtcNow,
                AssetClass = "Stock"
            });
    }
}