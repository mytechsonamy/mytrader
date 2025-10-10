using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Core.Interfaces;
using MyTrader.Core.DTOs;
using MyTrader.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTrader.Tests.Utilities;

public static class MockServiceHelper
{
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static Mock<IMultiAssetDataService> CreateMockMultiAssetDataService()
    {
        var mock = new Mock<IMultiAssetDataService>();

        // Setup common methods
        mock.Setup(x => x.GetLatestMarketDataAsync(It.IsAny<string>()))
            .ReturnsAsync(new UnifiedMarketDataDto
            {
                Symbol = "AAPL",
                Price = 150.25m,
                Change = 2.50m,
                ChangePercent = 1.69m,
                Volume = 1000000,
                Timestamp = DateTime.UtcNow,
                AssetClass = "Stock"
            });

        mock.Setup(x => x.GetMarketDataBatchAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<UnifiedMarketDataDto>
            {
                new UnifiedMarketDataDto
                {
                    Symbol = "AAPL",
                    Price = 150.25m,
                    Change = 2.50m,
                    ChangePercent = 1.69m,
                    Volume = 1000000,
                    Timestamp = DateTime.UtcNow,
                    AssetClass = "Stock"
                }
            });

        return mock;
    }

    public static Mock<IYahooFinanceApiService> CreateMockYahooFinanceService()
    {
        var mock = new Mock<IYahooFinanceApiService>();

        mock.Setup(x => x.GetQuoteAsync(It.IsAny<string>()))
            .ReturnsAsync(new YahooFinanceQuote
            {
                Symbol = "AAPL",
                RegularMarketPrice = 150.25m,
                RegularMarketChange = 2.50m,
                RegularMarketChangePercent = 1.69m,
                RegularMarketVolume = 1000000,
                RegularMarketTime = DateTime.UtcNow
            });

        return mock;
    }

    public static Mock<IGamificationService> CreateMockGamificationService()
    {
        var mock = new Mock<IGamificationService>();

        mock.Setup(x => x.GetUserPointsAsync(It.IsAny<int>()))
            .ReturnsAsync(1000);

        mock.Setup(x => x.AddPointsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.GetLeaderboardAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<LeaderboardEntry>
            {
                new LeaderboardEntry
                {
                    UserId = 1,
                    Username = "testuser1",
                    Points = 1500,
                    Rank = 1
                }
            });

        return mock;
    }

    public static Mock<ITradingDbContext> CreateMockDbContext()
    {
        var mock = new Mock<ITradingDbContext>();

        // Setup DbSet mocks as needed
        // This would typically be done with more specific setup in individual tests

        return mock;
    }
}

// Additional helper classes for test data
public class YahooFinanceQuote
{
    public string Symbol { get; set; } = string.Empty;
    public decimal RegularMarketPrice { get; set; }
    public decimal RegularMarketChange { get; set; }
    public decimal RegularMarketChangePercent { get; set; }
    public long RegularMarketVolume { get; set; }
    public DateTime RegularMarketTime { get; set; }
}

public class LeaderboardEntry
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Rank { get; set; }
}

public interface IGamificationService
{
    Task<int> GetUserPointsAsync(int userId);
    Task AddPointsAsync(int userId, int points, string reason);
    Task<List<LeaderboardEntry>> GetLeaderboardAsync(int page = 1, int pageSize = 10);
}

public interface IYahooFinanceApiService
{
    Task<YahooFinanceQuote> GetQuoteAsync(string symbol);
}