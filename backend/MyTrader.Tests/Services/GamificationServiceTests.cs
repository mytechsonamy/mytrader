using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Gamification;
using MyTrader.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Services;

public class GamificationServiceTests : TestBase
{
    private readonly TradingDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _mockLogger;
    private readonly GamificationService _service;

    public GamificationServiceTests()
    {
        _context = CreateInMemoryDbContext();
        _mockLogger = MockServiceHelper.CreateMockLogger<GamificationService>();
        _service = new GamificationService(_context, _mockLogger.Object);

        SeedTestData(_context);
    }

    [Fact]
    public async Task GetUserAchievementsAsync_WithExistingAchievements_ReturnsOrderedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievements = new List<UserAchievement>
        {
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "FIRST_TRADE",
                AchievementName = "First Trade",
                Description = "Made your first trade",
                Points = 100,
                Icon = "🎯",
                EarnedAt = DateTime.UtcNow.AddDays(-2)
            },
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "PROFITABLE_WEEK",
                AchievementName = "Profitable Week",
                Description = "Profitable for a whole week",
                Points = 500,
                Icon = "📈",
                EarnedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.UserAchievements.AddRange(achievements);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserAchievementsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        // Should be ordered by EarnedAt descending (most recent first)
        result[0].AchievementType.Should().Be("PROFITABLE_WEEK");
        result[1].AchievementType.Should().Be("FIRST_TRADE");

        // Verify all achievements belong to the correct user
        result.All(a => a.UserId == userId).Should().BeTrue();
    }

    [Fact]
    public async Task GetUserAchievementsAsync_WithNoAchievements_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserAchievementsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AwardAchievementAsync_NewAchievement_CreatesAndReturnsAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementType = "FIRST_PROFITABLE_TRADE";
        var name = "First Profit";
        var description = "Made your first profitable trade";
        var points = 250;
        var icon = "💰";

        // Act
        var result = await _service.AwardAchievementAsync(userId, achievementType, name, description, points, icon);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.AchievementType.Should().Be(achievementType);
        result.AchievementName.Should().Be(name);
        result.Description.Should().Be(description);
        result.Points.Should().Be(points);
        result.Icon.Should().Be(icon);
        result.EarnedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify it was saved to database
        var saved = await _context.UserAchievements
            .FirstOrDefaultAsync(a => a.UserId == userId && a.AchievementType == achievementType);

        saved.Should().NotBeNull();
        saved!.Id.Should().Be(result.Id);
    }

    [Fact]
    public async Task AwardAchievementAsync_ExistingAchievement_ReturnsExistingWithoutDuplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementType = "FIRST_TRADE";
        var existingAchievement = new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AchievementType = achievementType,
            AchievementName = "Original First Trade",
            Description = "Original description",
            Points = 100,
            Icon = "🎯",
            EarnedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.UserAchievements.Add(existingAchievement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AwardAchievementAsync(
            userId,
            achievementType,
            "New First Trade",
            "New description",
            200,
            "🚀");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingAchievement.Id);
        result.AchievementName.Should().Be("Original First Trade"); // Should retain original
        result.Points.Should().Be(100); // Should retain original

        // Verify no duplicate was created
        var achievementCount = await _context.UserAchievements
            .CountAsync(a => a.UserId == userId && a.AchievementType == achievementType);

        achievementCount.Should().Be(1);

        // Verify debug log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already has achievement")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AwardAchievementAsync_WithDefaultIcon_UsesDefaultTrophy()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementType = "TEST_ACHIEVEMENT";

        // Act - Not providing icon parameter
        var result = await _service.AwardAchievementAsync(
            userId,
            achievementType,
            "Test Achievement",
            "Test description",
            100);

        // Assert
        result.Should().NotBeNull();
        result.Icon.Should().Be("🏆"); // Default icon
    }

    [Theory]
    [InlineData("FIRST_TRADE", "First Trade", "Made your first trade", 100, "🎯")]
    [InlineData("PROFITABLE_DAY", "Daily Profit", "Profitable for a day", 250, "📈")]
    [InlineData("RISK_MANAGER", "Risk Manager", "Managed risk well", 300, "🛡️")]
    [InlineData("DIVERSIFIED", "Diversified", "Traded multiple assets", 400, "🌐")]
    public async Task AwardAchievementAsync_VariousAchievementTypes_WorksCorrectly(
        string type, string name, string description, int points, string icon)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.AwardAchievementAsync(userId, type, name, description, points, icon);

        // Assert
        result.Should().NotBeNull();
        result.AchievementType.Should().Be(type);
        result.AchievementName.Should().Be(name);
        result.Description.Should().Be(description);
        result.Points.Should().Be(points);
        result.Icon.Should().Be(icon);
    }

    [Fact]
    public async Task GetUserAchievementsAsync_WithMultipleUsers_ReturnsOnlyUserAchievements()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var achievements = new List<UserAchievement>
        {
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = user1Id,
                AchievementType = "FIRST_TRADE",
                AchievementName = "First Trade",
                Description = "Made first trade",
                Points = 100,
                Icon = "🎯",
                EarnedAt = DateTime.UtcNow
            },
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = user2Id,
                AchievementType = "PROFITABLE_DAY",
                AchievementName = "Profitable Day",
                Description = "Profitable for a day",
                Points = 250,
                Icon = "📈",
                EarnedAt = DateTime.UtcNow
            }
        };

        _context.UserAchievements.AddRange(achievements);
        await _context.SaveChangesAsync();

        // Act
        var user1Achievements = await _service.GetUserAchievementsAsync(user1Id);
        var user2Achievements = await _service.GetUserAchievementsAsync(user2Id);

        // Assert
        user1Achievements.Should().HaveCount(1);
        user1Achievements[0].AchievementType.Should().Be("FIRST_TRADE");
        user1Achievements.All(a => a.UserId == user1Id).Should().BeTrue();

        user2Achievements.Should().HaveCount(1);
        user2Achievements[0].AchievementType.Should().Be("PROFITABLE_DAY");
        user2Achievements.All(a => a.UserId == user2Id).Should().BeTrue();
    }

    [Fact]
    public async Task AwardAchievementAsync_DatabaseSaveError_ThrowsException()
    {
        // Arrange
        await _context.DisposeAsync(); // Dispose context to simulate database error

        var userId = Guid.NewGuid();
        var achievementType = "TEST_ACHIEVEMENT";

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await _service.AwardAchievementAsync(
                userId,
                achievementType,
                "Test",
                "Test description",
                100);
        });
    }

    [Fact]
    public async Task GetUserAchievementsAsync_DatabaseError_ThrowsException()
    {
        // Arrange
        await _context.DisposeAsync(); // Dispose context to simulate database error
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await _service.GetUserAchievementsAsync(userId);
        });
    }

    [Fact]
    public async Task AwardAchievementAsync_LargeDataSet_PerformsWell()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Create many existing achievements for other users to test performance
        var existingAchievements = new List<UserAchievement>();
        for (int i = 0; i < 1000; i++)
        {
            existingAchievements.Add(new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(), // Different user
                AchievementType = $"ACHIEVEMENT_{i}",
                AchievementName = $"Achievement {i}",
                Description = $"Achievement {i} description",
                Points = i * 10,
                Icon = "🏆",
                EarnedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        _context.UserAchievements.AddRange(existingAchievements);
        await _context.SaveChangesAsync();

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _service.AwardAchievementAsync(
            userId,
            "NEW_ACHIEVEMENT",
            "New Achievement",
            "New description",
            500);
        var endTime = DateTime.UtcNow;

        // Assert
        result.Should().NotBeNull();
        var executionTime = endTime - startTime;
        executionTime.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    protected override void SeedTestData(TradingDbContext context)
    {
        // No additional seeding needed for gamification tests
        // Each test will create its own data
    }

    public override void Dispose()
    {
        _context?.Dispose();
        base.Dispose();
    }
}

// Test interface if not available
public interface IGamificationService
{
    Task<List<UserAchievement>> GetUserAchievementsAsync(Guid userId);
    Task<UserAchievement> AwardAchievementAsync(Guid userId, string achievementType, string name, string description, int points, string icon = "🏆");
}

// Test model if not available
public class UserAchievement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AchievementType { get; set; } = string.Empty;
    public string AchievementName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Icon { get; set; } = "🏆";
    public DateTime EarnedAt { get; set; }
}