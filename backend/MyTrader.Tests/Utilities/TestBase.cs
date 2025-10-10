using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyTrader.Api;
using MyTrader.Infrastructure.Data;
using System;
using System.Linq;

namespace MyTrader.Tests.Utilities;

public abstract class TestBase : IDisposable
{
    protected TestBase()
    {
        // Initialize any common test setup here
    }

    protected virtual TradingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TradingDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    protected virtual void SeedTestData(TradingDbContext context)
    {
        // Override in derived classes to seed specific test data
    }

    public virtual void Dispose()
    {
        // Cleanup resources
        GC.SuppressFinalize(this);
    }
}

public class IntegrationTestBase : TestBase
{
    protected WebApplicationFactory<Program> Factory { get; private set; }
    protected HttpClient Client { get; private set; }

    public IntegrationTestBase()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real database registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<TradingDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add in-memory database for testing
                    services.AddDbContext<TradingDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });

                    // Build the service provider
                    var sp = services.BuildServiceProvider();

                    // Create a scope to obtain a reference to the database context
                    using var scope = sp.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<TradingDbContext>();

                    // Ensure the database is created
                    db.Database.EnsureCreated();

                    try
                    {
                        // Seed the database with test data
                        TestDataSeeder.SeedDatabase(db);
                    }
                    catch (Exception ex)
                    {
                        // Log errors or handle them as appropriate for tests
                        throw new InvalidOperationException("An error occurred seeding the test database.", ex);
                    }
                });
            });

        Client = Factory.CreateClient();
    }

    public override void Dispose()
    {
        Client?.Dispose();
        Factory?.Dispose();
        base.Dispose();
    }
}