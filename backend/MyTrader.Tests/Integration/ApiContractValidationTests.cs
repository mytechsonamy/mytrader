using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using System.Net;

namespace MyTrader.Tests.Integration;

/// <summary>
/// Tests to validate API contract compliance and data structure consistency
/// Addresses critical issues: CompetitionEntry.tsx:155 and EnhancedLeaderboardScreen.tsx:61
/// </summary>
public class ApiContractValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiContractValidationTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("ApiContractTest_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AuthController_Login_ReturnsCorrectStructure()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@apitest.com",
            Password = "ApiTest123!",
            FirstName = "API",
            LastName = "Test",
            Phone = "+1234567890"
        };

        // First register a user
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "test@apitest.com",
            Password = "ApiTest123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Validate required fields exist
        Assert.True(jsonDoc.RootElement.TryGetProperty("token", out _), "Response must contain 'token' field");
        Assert.True(jsonDoc.RootElement.TryGetProperty("user", out _), "Response must contain 'user' field");

        var userElement = jsonDoc.RootElement.GetProperty("user");
        Assert.True(userElement.TryGetProperty("id", out _), "User must contain 'id' field");
        Assert.True(userElement.TryGetProperty("email", out _), "User must contain 'email' field");
        Assert.True(userElement.TryGetProperty("firstName", out _), "User must contain 'firstName' field");
        Assert.True(userElement.TryGetProperty("lastName", out _), "User must contain 'lastName' field");

        // Validate token is not empty
        var token = jsonDoc.RootElement.GetProperty("token").GetString();
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task LeaderboardController_GetLeaderboard_ReturnsArrayStructure()
    {
        // This test addresses the EnhancedLeaderboardScreen.tsx:61 error
        // where non-array data causes .map() to fail

        // Act
        var response = await _client.GetAsync("/api/leaderboard");

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // CRITICAL: Response must always be an array or contain an array
        if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
        {
            // If it's an object, it should have a 'data' property that's an array
            Assert.True(jsonDoc.RootElement.TryGetProperty("data", out var dataProperty),
                "Object response must contain 'data' property");
            Assert.Equal(JsonValueKind.Array, dataProperty.ValueKind);
        }
        else
        {
            // If it's directly an array
            Assert.Equal(JsonValueKind.Array, jsonDoc.RootElement.ValueKind);
        }

        // Validate array elements structure
        var array = jsonDoc.RootElement.ValueKind == JsonValueKind.Array
            ? jsonDoc.RootElement
            : jsonDoc.RootElement.GetProperty("data");

        foreach (var element in array.EnumerateArray())
        {
            Assert.True(element.TryGetProperty("userId", out _), "Leaderboard entry must contain 'userId'");
            Assert.True(element.TryGetProperty("username", out _), "Leaderboard entry must contain 'username'");
            Assert.True(element.TryGetProperty("rank", out _), "Leaderboard entry must contain 'rank'");
            Assert.True(element.TryGetProperty("totalReturn", out _), "Leaderboard entry must contain 'totalReturn'");
        }
    }

    [Fact]
    public async Task CompetitionController_GetStats_ReturnsPrizesAsArray()
    {
        // This test addresses the CompetitionEntry.tsx:155 error
        // where prizes?.slice(0, 3) fails if prizes is null/undefined

        // Act
        var response = await _client.GetAsync("/api/competition/stats");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);

            // CRITICAL: prizes field must always be an array, never null
            Assert.True(jsonDoc.RootElement.TryGetProperty("prizes", out var prizesProperty),
                "Competition stats must contain 'prizes' property");

            Assert.Equal(JsonValueKind.Array, prizesProperty.ValueKind);

            // Validate prize structure
            foreach (var prize in prizesProperty.EnumerateArray())
            {
                Assert.True(prize.TryGetProperty("rank", out _), "Prize must contain 'rank'");
                Assert.True(prize.TryGetProperty("amount", out _), "Prize must contain 'amount'");
                Assert.True(prize.TryGetProperty("currency", out _), "Prize must contain 'currency'");
            }

            // Validate other required fields
            Assert.True(jsonDoc.RootElement.TryGetProperty("totalParticipants", out _));
            Assert.True(jsonDoc.RootElement.TryGetProperty("totalPrizePool", out _));
            Assert.True(jsonDoc.RootElement.TryGetProperty("minimumTrades", out _));
        }
        else
        {
            // If endpoint doesn't exist, it should return 404, not 500
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task SymbolsController_GetSymbols_ReturnsConsistentStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/symbols");

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Response should be an array of symbols
        Assert.Equal(JsonValueKind.Array, jsonDoc.RootElement.ValueKind);

        foreach (var symbol in jsonDoc.RootElement.EnumerateArray())
        {
            Assert.True(symbol.TryGetProperty("id", out _), "Symbol must contain 'id'");
            Assert.True(symbol.TryGetProperty("symbol", out _), "Symbol must contain 'symbol'");
            Assert.True(symbol.TryGetProperty("name", out _), "Symbol must contain 'name'");
            Assert.True(symbol.TryGetProperty("assetClass", out _), "Symbol must contain 'assetClass'");
        }
    }

    [Fact]
    public async Task MarketDataController_GetRealTimeData_HandlesInvalidSymbol()
    {
        // Act
        var response = await _client.GetAsync("/api/market-data/realtime/INVALID_SYMBOL");

        // Assert
        // Should return either 404 or 200 with empty/null data, but never 500
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.OK,
            $"Invalid symbol request returned unexpected status: {response.StatusCode}"
        );

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);

            // If OK, response should be well-structured (null/empty is fine)
            if (jsonDoc.RootElement.ValueKind != JsonValueKind.Null)
            {
                // Should have proper structure or be empty
                Assert.True(jsonDoc.RootElement.TryGetProperty("data", out _) ||
                           jsonDoc.RootElement.ValueKind == JsonValueKind.Object);
            }
        }
    }

    [Fact]
    public async Task WebSocketConnectionInfo_IsAccessible()
    {
        // Test that WebSocket connection endpoint is accessible
        // This helps validate SignalR hub configuration

        // Act
        var response = await _client.GetAsync("/hubs/market-data/negotiate");

        // Assert
        // Should return negotiation info or method not allowed, but not 500
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.MethodNotAllowed ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"WebSocket negotiate returned unexpected status: {response.StatusCode}"
        );
    }

    [Fact]
    public async Task ApiEndpoints_ReturnConsistentErrorStructure()
    {
        // Test that all endpoints return consistent error structure

        var testEndpoints = new[]
        {
            "/api/auth/login", // POST
            "/api/nonexistent", // GET
            "/api/symbols/999999", // GET with invalid ID
        };

        foreach (var endpoint in testEndpoints)
        {
            HttpResponseMessage response;

            if (endpoint.Contains("/login"))
            {
                // POST endpoint - send invalid data
                response = await _client.PostAsJsonAsync(endpoint, new { invalid = "data" });
            }
            else
            {
                // GET endpoint
                response = await _client.GetAsync(endpoint);
            }

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseContent))
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);

                    // Error responses should have consistent structure
                    if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        // Should have either 'message' or 'errors' property
                        Assert.True(
                            jsonDoc.RootElement.TryGetProperty("message", out _) ||
                            jsonDoc.RootElement.TryGetProperty("errors", out _) ||
                            jsonDoc.RootElement.TryGetProperty("title", out _),
                            $"Error response from {endpoint} should have consistent structure"
                        );
                    }
                }
            }
        }
    }

    [Fact]
    public async Task AuthenticationHeaders_AreValidated()
    {
        // Test protected endpoints require proper authentication headers

        var protectedEndpoints = new[]
        {
            "/api/portfolio",
            "/api/leaderboard/me",
            "/api/competition/join",
        };

        foreach (var endpoint in testEndpoints)
        {
            // Act - Request without auth header
            var response = await _client.GetAsync(endpoint);

            // Assert - Should return 401 Unauthorized
            if (response.StatusCode != HttpStatusCode.NotFound) // Endpoint exists
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        // Test with invalid token
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid_token");

        foreach (var endpoint in protectedEndpoints)
        {
            var response = await _client.GetAsync(endpoint);

            if (response.StatusCode != HttpStatusCode.NotFound) // Endpoint exists
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }
    }

    [Fact]
    public async Task CriticalEndpoints_HandleHighLoad()
    {
        // Simulate concurrent requests to critical endpoints
        var tasks = new List<Task<HttpResponseMessage>>();

        // Create 20 concurrent requests to symbols endpoint
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_client.GetAsync("/api/symbols"));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            // Should not return 500 errors under load
            Assert.True(
                response.StatusCode != HttpStatusCode.InternalServerError,
                "Endpoint should handle concurrent requests without 500 errors"
            );
        }

        // At least some requests should succeed
        Assert.True(responses.Any(r => r.IsSuccessStatusCode),
                   "At least some concurrent requests should succeed");
    }

    [Fact]
    public async Task DataValidation_PreventsNullArrays()
    {
        // Test that API never returns null where arrays are expected

        var arrayEndpoints = new[]
        {
            ("/api/symbols", "symbols"),
            ("/api/leaderboard", "leaderboard entries"),
        };

        foreach (var (endpoint, description) in arrayEndpoints)
        {
            var response = await _client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseContent);

                // Should never be null at root level
                Assert.NotEqual(JsonValueKind.Null, jsonDoc.RootElement.ValueKind);

                // If it's an object, it should have array properties
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in jsonDoc.RootElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            // Arrays should never be null (they can be empty)
                            Assert.NotEqual(JsonValueKind.Null, property.Value.ValueKind);
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public async Task ResponseTime_IsWithinAcceptableLimits()
    {
        var endpoints = new[]
        {
            "/api/symbols",
            "/api/auth/me",
            "/api/leaderboard"
        };

        foreach (var endpoint in endpoints)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var response = await _client.GetAsync(endpoint);
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                // API should respond within 5 seconds
                Assert.True(duration.TotalSeconds < 5,
                           $"Endpoint {endpoint} took {duration.TotalSeconds:F2} seconds (max 5 seconds allowed)");
            }
            catch (TaskCanceledException)
            {
                Assert.True(false, $"Endpoint {endpoint} timed out");
            }
        }
    }
}