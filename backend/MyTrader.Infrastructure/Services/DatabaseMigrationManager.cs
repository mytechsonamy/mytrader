using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyTrader.Infrastructure.Data;
using System.Reflection;

namespace MyTrader.Infrastructure.Services;

public interface IDatabaseMigrationManager
{
    Task<MigrationResult> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default);
    Task<MigrationStatus> GetMigrationStatusAsync();
    Task<bool> RollbackToMigrationAsync(string migrationName);
    Task<List<string>> GetPendingMigrationsAsync();
    Task<List<string>> GetAppliedMigrationsAsync();
    Task<bool> ValidateDatabaseSchemaAsync();
}

public class MigrationResult
{
    public bool Success { get; set; }
    public List<string> AppliedMigrations { get; set; } = new();
    public List<string> FailedMigrations { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}

public class MigrationStatus
{
    public bool DatabaseExists { get; set; }
    public bool HasPendingMigrations { get; set; }
    public int PendingMigrationsCount { get; set; }
    public int AppliedMigrationsCount { get; set; }
    public List<string> PendingMigrations { get; set; } = new();
    public List<string> AppliedMigrations { get; set; } = new();
    public string DatabaseVersion { get; set; } = string.Empty;
    public bool IsSchemaValid { get; set; }
    public DateTime LastMigrationDate { get; set; }
}

public class DatabaseMigrationManager : IDatabaseMigrationManager
{
    private readonly TradingDbContext _context;
    private readonly ILogger<DatabaseMigrationManager> _logger;
    private readonly IDatabaseRetryPolicyService _retryPolicyService;
    
    public DatabaseMigrationManager(
        TradingDbContext context,
        ILogger<DatabaseMigrationManager> logger,
        IDatabaseRetryPolicyService retryPolicyService)
    {
        _context = context;
        _logger = logger;
        _retryPolicyService = retryPolicyService;
    }

    public async Task<MigrationResult> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting automatic migration process");

            // Skip migrations for in-memory database
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                await _context.Database.EnsureCreatedAsync(cancellationToken);
                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("In-memory database ensured created successfully");
                return result;
            }

            // Get pending migrations
            var pendingMigrations = await GetPendingMigrationsAsync();
            
            if (!pendingMigrations.Any())
            {
                _logger.LogInformation("No pending migrations found");
                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            _logger.LogInformation("Found {Count} pending migrations: {Migrations}", 
                pendingMigrations.Count, string.Join(", ", pendingMigrations));

            // Apply migrations with retry logic
            await _retryPolicyService.ExecuteAsync(async () =>
            {
                await _context.Database.MigrateAsync(cancellationToken);
            }, "ApplyMigrations");

            // Verify migrations were applied
            var remainingPending = await GetPendingMigrationsAsync();
            if (remainingPending.Any())
            {
                throw new InvalidOperationException($"Migrations were not applied successfully. Remaining: {string.Join(", ", remainingPending)}");
            }

            result.Success = true;
            result.AppliedMigrations = pendingMigrations;
            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Successfully applied {Count} migrations in {Duration}ms", 
                pendingMigrations.Count, result.Duration.TotalMilliseconds);

            // Validate schema after migration
            var isSchemaValid = await ValidateDatabaseSchemaAsync();
            if (!isSchemaValid)
            {
                _logger.LogWarning("Database schema validation failed after migration");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.ErrorMessage = ex.Message;
            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex, "Migration process failed after {Duration}ms", result.Duration.TotalMilliseconds);
        }

        return result;
    }

    public async Task<MigrationStatus> GetMigrationStatusAsync()
    {
        var status = new MigrationStatus();

        try
        {
            // Check if database exists
            status.DatabaseExists = await _context.Database.CanConnectAsync();

            if (!status.DatabaseExists)
            {
                _logger.LogWarning("Database does not exist or is not accessible");
                return status;
            }

            // Skip detailed status for in-memory database
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                status.DatabaseExists = true;
                status.IsSchemaValid = true;
                status.DatabaseVersion = "InMemory";
                return status;
            }

            // Get applied migrations
            status.AppliedMigrations = (await GetAppliedMigrationsAsync()).ToList();
            status.AppliedMigrationsCount = status.AppliedMigrations.Count;

            // Get pending migrations
            status.PendingMigrations = (await GetPendingMigrationsAsync()).ToList();
            status.PendingMigrationsCount = status.PendingMigrations.Count;
            status.HasPendingMigrations = status.PendingMigrationsCount > 0;

            // Get database version (last applied migration)
            status.DatabaseVersion = status.AppliedMigrations.LastOrDefault() ?? "No migrations applied";

            // Get last migration date
            if (status.AppliedMigrations.Any())
            {
                status.LastMigrationDate = await GetLastMigrationDateAsync();
            }

            // Validate schema
            status.IsSchemaValid = await ValidateDatabaseSchemaAsync();

            _logger.LogDebug("Migration status: Applied={AppliedCount}, Pending={PendingCount}, Valid={IsValid}",
                status.AppliedMigrationsCount, status.PendingMigrationsCount, status.IsSchemaValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting migration status");
            status.DatabaseExists = false;
        }

        return status;
    }

    public async Task<bool> RollbackToMigrationAsync(string migrationName)
    {
        try
        {
            _logger.LogInformation("Rolling back to migration: {MigrationName}", migrationName);

            // Skip rollback for in-memory database
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                _logger.LogWarning("Rollback not supported for in-memory database");
                return false;
            }

            // Verify migration exists
            var appliedMigrations = await GetAppliedMigrationsAsync();
            if (!appliedMigrations.Contains(migrationName))
            {
                _logger.LogError("Migration {MigrationName} not found in applied migrations", migrationName);
                return false;
            }

            // Perform rollback with retry logic
            await _retryPolicyService.ExecuteAsync(async () =>
            {
                await _context.Database.MigrateAsync(migrationName);
            }, "RollbackMigration");

            _logger.LogInformation("Successfully rolled back to migration: {MigrationName}", migrationName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back to migration: {MigrationName}", migrationName);
            return false;
        }
    }

    public async Task<List<string>> GetPendingMigrationsAsync()
    {
        try
        {
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                return new List<string>();
            }

            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            return pendingMigrations.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending migrations");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetAppliedMigrationsAsync()
    {
        try
        {
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                return new List<string> { "InMemoryDatabase" };
            }

            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
            return appliedMigrations.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applied migrations");
            return new List<string>();
        }
    }

    public async Task<bool> ValidateDatabaseSchemaAsync()
    {
        try
        {
            // Skip validation for in-memory database
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                return true;
            }

            // Perform basic schema validation by testing key operations
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            
            // Test that we can access key tables
            var userCount = await _context.Users.CountAsync();
            var symbolCount = await _context.Symbols.CountAsync();
            
            _logger.LogDebug("Schema validation passed. Users: {UserCount}, Symbols: {SymbolCount}", 
                userCount, symbolCount);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database schema validation failed");
            return false;
        }
    }

    private async Task<DateTime> GetLastMigrationDateAsync()
    {
        try
        {
            // Try to get migration history from __EFMigrationsHistory table
            var sql = "SELECT MAX(\"AppliedOn\") FROM \"__EFMigrationsHistory\"";
            
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            
            if (command.Connection?.State != System.Data.ConnectionState.Open)
            {
                await command.Connection!.OpenAsync();
            }
            
            var result = await command.ExecuteScalarAsync();
            
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToDateTime(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve last migration date");
        }
        
        return DateTime.MinValue;
    }
}

public static class DatabaseMigrationExtensions
{
    public static IServiceCollection AddDatabaseMigrationManager(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseMigrationManager, DatabaseMigrationManager>();
        return services;
    }
    
    public static async Task<IServiceProvider> ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var migrationManager = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationManager>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseMigrationManager>>();
        
        try
        {
            var result = await migrationManager.ApplyPendingMigrationsAsync();
            
            if (result.Success)
            {
                logger.LogInformation("Database migrations applied successfully during startup");
            }
            else
            {
                logger.LogError("Database migration failed during startup: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying migrations during startup");
        }
        
        return serviceProvider;
    }
}