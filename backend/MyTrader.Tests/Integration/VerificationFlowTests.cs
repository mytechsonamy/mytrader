using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MyTrader.Tests.Integration;

public class VerificationFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public VerificationFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TradingDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<TradingDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RegisterUserFlow_ShouldSucceed()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Phone = "+1234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("doğrulama kodu", result.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task ResendVerification_ShouldSucceed_WhenUserExists()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "resend@example.com",
            Password = "Password123!",
            FirstName = "Resend",
            LastName = "User",
            Phone = "+1234567890"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var resendRequest = new VerificationCodeRequest
        {
            Email = "resend@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification", resendRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("doğrulama kodu", result.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task ResendVerification_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var resendRequest = new VerificationCodeRequest
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification", resendRequest);

        // Assert
        response.EnsureSuccessStatusCode(); // API doesn't return error status for security
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("bekleyen bir kayıt bulunamadı", result.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task ResendVerification_ShouldThrottle_WhenCalledTooQuickly()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "throttle@example.com",
            Password = "Password123!",
            FirstName = "Throttle",
            LastName = "User",
            Phone = "+1234567890"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var resendRequest = new VerificationCodeRequest
        {
            Email = "throttle@example.com"
        };

        // Act - First resend should succeed
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/resend-verification", resendRequest);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Second resend immediately should be throttled
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/resend-verification", resendRequest);

        // Assert
        secondResponse.EnsureSuccessStatusCode();
        var content = await secondResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("çok sık", result.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task CompleteVerificationFlow_ShouldCreateActiveUser()
    {
        // Arrange - Register user
        var registerRequest = new RegisterRequest
        {
            Email = "complete@example.com",
            Password = "Password123!",
            FirstName = "Complete",
            LastName = "User",
            Phone = "+1234567890"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Get verification code from database (in real scenario, would come from email)
        string verificationCode;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var verification = await context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == "complete@example.com");

            Assert.NotNull(verification);
            verificationCode = verification.VerificationCode;
        }

        var verifyRequest = new VerifyEmailRequest
        {
            Email = "complete@example.com",
            VerificationCode = verificationCode
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify user is created and temp data is cleaned up
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "complete@example.com");
            Assert.NotNull(user);
            Assert.True(user.IsActive);
            Assert.True(user.IsEmailVerified);

            var tempReg = await context.TempRegistrations.FirstOrDefaultAsync(t => t.Email == "complete@example.com");
            Assert.Null(tempReg);

            var verification = await context.EmailVerifications.FirstOrDefaultAsync(v => v.Email == "complete@example.com");
            Assert.Null(verification);
        }
    }

    [Fact]
    public async Task PasswordResetFlow_ShouldWork()
    {
        // Arrange - First create a verified user
        await CompleteVerificationFlow_ShouldCreateActiveUser();

        var resetRequest = new PasswordResetRequest
        {
            Email = "complete@example.com"
        };

        // Act - Request password reset
        var response = await _client.PostAsJsonAsync("/api/auth/request-password-reset", resetRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("doğrulama kodu", result.Message.ToLowerInvariant());
    }
}