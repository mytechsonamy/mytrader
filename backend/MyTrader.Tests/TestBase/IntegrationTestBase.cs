using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyTrader.Infrastructure.Data;
using System.Net.Http.Headers;
using Xunit;

namespace MyTrader.Tests.TestBase;

/// <summary>
/// Base class for integration tests providing common setup and utilities
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly TradingDbContext DbContext;
    private bool _disposed;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TradingDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<TradingDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    options.EnableSensitiveDataLogging();
                });

                // Disable logging for tests
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            });

            builder.UseEnvironment("Testing");
        });

        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        // Ensure database is created and seeded
        DbContext.Database.EnsureCreated();
        SeedTestData();
    }

    /// <summary>
    /// Seed initial test data for integration tests
    /// </summary>
    protected virtual async Task SeedTestData()
    {
        // Override in derived classes to add specific test data
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Authenticate the client with a test user token
    /// </summary>
    /// <param name="userId">User ID to authenticate as</param>
    /// <returns>Authentication token</returns>
    protected async Task<string> AuthenticateAsync(Guid? userId = null)
    {
        var testUserId = userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        // Create a test JWT token (simplified for testing)
        var token = $"test-token-{testUserId}";
        Client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        return token;
    }

    /// <summary>
    /// Clear authentication headers
    /// </summary>
    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Get a fresh database context for data verification
    /// </summary>
    /// <returns>New database context instance</returns>
    protected TradingDbContext GetDbContext()
    {
        return Scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    }

    /// <summary>
    /// Reset the database to a clean state
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
        await SeedTestData();
    }

    public virtual void Dispose()
    {
        if (_disposed) return;

        DbContext?.Database?.EnsureDeleted();
        DbContext?.Dispose();
        Scope?.Dispose();
        Client?.Dispose();
        
        _disposed = true;
    }
}