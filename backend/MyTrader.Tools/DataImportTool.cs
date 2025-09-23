using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Data;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Infrastructure.Data;
using MyTrader.Infrastructure.Services;

namespace MyTrader.Tools;

/// <summary>
/// Command-line tool for importing Stock_Scrapper data into myTrader
/// Supports various import scenarios with progress reporting and validation
/// </summary>
public class DataImportTool
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("MyTrader Data Import Tool - Import Stock_Scrapper data");

        // Import single file command
        var importFileCommand = new Command("import-file", "Import data from a single CSV file");
        var filePathOption = new Option<string>("--file", "Path to CSV file") { IsRequired = true };
        var dataSourceOption = new Option<string>("--source", "Data source (BIST, CRYPTO, NASDAQ, NYSE)") { IsRequired = true };
        importFileCommand.AddOption(filePathOption);
        importFileCommand.AddOption(dataSourceOption);
        importFileCommand.SetHandler(ImportFileAsync, filePathOption, dataSourceOption);

        // Import directory command
        var importDirCommand = new Command("import-directory", "Import data from a directory of CSV files");
        var dirPathOption = new Option<string>("--directory", "Path to directory containing CSV files") { IsRequired = true };
        var dirSourceOption = new Option<string>("--source", "Data source (BIST, CRYPTO, NASDAQ, NYSE)") { IsRequired = true };
        importDirCommand.AddOption(dirPathOption);
        importDirCommand.AddOption(dirSourceOption);
        importDirCommand.SetHandler(ImportDirectoryAsync, dirPathOption, dirSourceOption);

        // Import all markets command
        var importAllCommand = new Command("import-all", "Import all markets from Stock_Scrapper DATA directory");
        var stockScrapperPathOption = new Option<string>("--stock-scrapper-path", "Path to Stock_Scrapper DATA directory") { IsRequired = true };
        importAllCommand.AddOption(stockScrapperPathOption);
        importAllCommand.SetHandler(ImportAllMarketsAsync, stockScrapperPathOption);

        // Validate file command
        var validateCommand = new Command("validate", "Validate a CSV file format");
        var validateFileOption = new Option<string>("--file", "Path to CSV file to validate") { IsRequired = true };
        validateCommand.AddOption(validateFileOption);
        validateCommand.SetHandler(ValidateFileAsync, validateFileOption);

        // Clean duplicates command
        var cleanCommand = new Command("clean-duplicates", "Clean duplicate records for a symbol");
        var symbolOption = new Option<string>("--symbol", "Symbol ticker") { IsRequired = true };
        var startDateOption = new Option<string>("--start-date", "Start date (YYYY-MM-DD)") { IsRequired = true };
        var endDateOption = new Option<string>("--end-date", "End date (YYYY-MM-DD)") { IsRequired = true };
        var dryRunOption = new Option<bool>("--dry-run", "Only show what would be deleted without actually deleting") { IsRequired = false };
        dryRunOption.SetDefaultValue(true);
        cleanCommand.AddOption(symbolOption);
        cleanCommand.AddOption(startDateOption);
        cleanCommand.AddOption(endDateOption);
        cleanCommand.AddOption(dryRunOption);
        cleanCommand.SetHandler(CleanDuplicatesAsync, symbolOption, startDateOption, endDateOption, dryRunOption);

        // Statistics command
        var statsCommand = new Command("statistics", "Get import statistics");
        var statsStartOption = new Option<string>("--start-date", "Start date (YYYY-MM-DD)") { IsRequired = true };
        var statsEndOption = new Option<string>("--end-date", "End date (YYYY-MM-DD)") { IsRequired = true };
        statsCommand.AddOption(statsStartOption);
        statsCommand.AddOption(statsEndOption);
        statsCommand.SetHandler(GetStatisticsAsync, statsStartOption, statsEndOption);

        rootCommand.AddCommand(importFileCommand);
        rootCommand.AddCommand(importDirCommand);
        rootCommand.AddCommand(importAllCommand);
        rootCommand.AddCommand(validateCommand);
        rootCommand.AddCommand(cleanCommand);
        rootCommand.AddCommand(statsCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> ImportFileAsync(string filePath, string dataSource)
    {
        using var host = CreateHost();
        var importService = host.Services.GetRequiredService<IDataImportService>();
        var logger = host.Services.GetRequiredService<ILogger<DataImportTool>>();

        logger.LogInformation("Starting import of file: {FilePath} with data source: {DataSource}", filePath, dataSource);

        var progress = new Progress<DataImportProgressDto>(p =>
        {
            Console.WriteLine($"Progress: {p.Operation} - {p.RecordsProcessed}/{p.TotalRecords} records ({p.FileProgressPercentage:F1}%) - {p.ProcessingRate:F1} records/sec");
        });

        try
        {
            var result = await importService.ImportFromCsvAsync(filePath, dataSource, progress);

            Console.WriteLine($"\nImport completed!");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Records processed: {result.RecordsProcessed}");
            Console.WriteLine($"Records imported: {result.RecordsImported}");
            Console.WriteLine($"Records skipped: {result.RecordsSkipped}");
            Console.WriteLine($"Processing time: {result.ProcessingTime}");

            if (result.Errors.Any())
            {
                Console.WriteLine($"\nErrors ({result.Errors.Count}):");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            if (result.Warnings.Any())
            {
                Console.WriteLine($"\nWarnings ({result.Warnings.Count}):");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"  - {warning}");
                }
            }

            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during file import");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ImportDirectoryAsync(string directoryPath, string dataSource)
    {
        using var host = CreateHost();
        var importService = host.Services.GetRequiredService<IDataImportService>();
        var logger = host.Services.GetRequiredService<ILogger<DataImportTool>>();

        logger.LogInformation("Starting import of directory: {DirectoryPath} with data source: {DataSource}", directoryPath, dataSource);

        var progress = new Progress<DataImportProgressDto>(p =>
        {
            Console.WriteLine($"Progress: {p.Operation} - {p.FilesProcessed}/{p.TotalFiles} files - {p.ProcessingRate:F1} records/sec");
        });

        try
        {
            var result = await importService.ImportFromDirectoryAsync(directoryPath, dataSource, progress);

            Console.WriteLine($"\nDirectory import completed!");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Files processed: {result.FilesProcessed}");
            Console.WriteLine($"Records imported: {result.RecordsImported}");
            Console.WriteLine($"Records skipped: {result.RecordsSkipped}");
            Console.WriteLine($"New symbols created: {result.NewSymbolsCreated}");
            Console.WriteLine($"Processing time: {result.ProcessingTime}");

            if (result.SymbolStats.Any())
            {
                Console.WriteLine($"\nSymbol Statistics:");
                foreach (var kvp in result.SymbolStats.OrderByDescending(x => x.Value.RecordsImported))
                {
                    var stats = kvp.Value;
                    Console.WriteLine($"  {stats.SymbolTicker}: {stats.RecordsImported} records, {stats.StartDate} to {stats.EndDate}");
                }
            }

            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during directory import");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ImportAllMarketsAsync(string stockScrapperPath)
    {
        using var host = CreateHost();
        var importService = host.Services.GetRequiredService<IDataImportService>();
        var logger = host.Services.GetRequiredService<ILogger<DataImportTool>>();

        logger.LogInformation("Starting import of all markets from: {StockScrapperPath}", stockScrapperPath);

        var progress = new Progress<DataImportProgressDto>(p =>
        {
            Console.WriteLine($"Progress: {p.Operation} - {p.FilesProcessed}/{p.TotalFiles} files - {p.ProcessingRate:F1} records/sec");
        });

        try
        {
            var results = await importService.ImportAllMarketsAsync(stockScrapperPath, progress);

            Console.WriteLine($"\nAll markets import completed!");

            var totalImported = results.Values.Sum(r => r.RecordsImported);
            var totalErrors = results.Values.Sum(r => r.Errors.Count);
            var allSuccessful = results.Values.All(r => r.Success);

            Console.WriteLine($"Overall success: {allSuccessful}");
            Console.WriteLine($"Total records imported: {totalImported}");
            Console.WriteLine($"Total errors: {totalErrors}");

            Console.WriteLine($"\nResults by market:");
            foreach (var kvp in results)
            {
                var market = kvp.Key;
                var result = kvp.Value;
                Console.WriteLine($"  {market}: {(result.Success ? "Success" : "Failed")} - {result.RecordsImported} records imported");
                if (!result.Success)
                {
                    Console.WriteLine($"    Error: {result.Message}");
                }
            }

            return allSuccessful ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during all markets import");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ValidateFileAsync(string filePath)
    {
        using var host = CreateHost();
        var importService = host.Services.GetRequiredService<IDataImportService>();
        var logger = host.Services.GetRequiredService<ILogger<DataImportTool>>();

        logger.LogInformation("Validating file: {FilePath}", filePath);

        try
        {
            var result = await importService.ValidateCsvFileAsync(filePath);

            Console.WriteLine($"File validation results:");
            Console.WriteLine($"Valid: {result.IsValid}");
            Console.WriteLine($"Data format: {result.DataFormat}");
            Console.WriteLine($"Symbol ticker: {result.SymbolTicker}");
            Console.WriteLine($"Data rows: {result.DataRowCount}");
            Console.WriteLine($"Date range: {result.StartDate} to {result.EndDate}");

            if (result.Headers.Any())
            {
                Console.WriteLine($"Headers found: {string.Join(", ", result.Headers)}");
            }

            if (result.MissingColumns.Any())
            {
                Console.WriteLine($"Missing columns: {string.Join(", ", result.MissingColumns)}");
            }

            if (result.ExtraColumns.Any())
            {
                Console.WriteLine($"Extra columns: {string.Join(", ", result.ExtraColumns)}");
            }

            if (result.Errors.Any())
            {
                Console.WriteLine($"Errors:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            if (result.Warnings.Any())
            {
                Console.WriteLine($"Warnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"  - {warning}");
                }
            }

            return result.IsValid ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during file validation");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> CleanDuplicatesAsync(string symbol, string startDate, string endDate, bool dryRun)
    {
        using var host = CreateHost();
        var importService = host.Services.GetRequiredService<IDataImportService>();
        var logger = host.Services.GetRequiredService<ILogger<DataImportTool>>();

        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
        {
            Console.WriteLine("Invalid date format. Use YYYY-MM-DD format.");
            return 1;
        }

        logger.LogInformation("Cleaning duplicates for {Symbol} from {StartDate} to {EndDate} (DryRun: {DryRun})",
            symbol, start, end, dryRun);

        try
        {
            var result = await importService.CleanDuplicateRecordsAsync(symbol, start, end, dryRun);

            Console.WriteLine($"Duplicate cleanup results:");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Duplicates found: {result.DuplicatesFound}");
            Console.WriteLine($"Records that would be deleted: {result.RecordsDeleted}");
            Console.WriteLine($"Records retained: {result.RecordsRetained}");
            Console.WriteLine($"Processing time: {result.ProcessingTime}");

            if (result.DuplicateGroups.Any())
            {
                Console.WriteLine($"\nDuplicate groups:");
                foreach (var group in result.DuplicateGroups)
                {
                    Console.WriteLine($"  {group.TradeDate}: {group.DuplicateCount} duplicates, retained {group.RetainedSource}");
                }
            }

            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during duplicate cleanup");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> GetStatisticsAsync(string startDate, string endDate)
    {
        using var host = CreateHost();
        var importService = host.Services.GetRequiredService<IDataImportService>();
        var logger = host.Services.GetRequiredService<ILogger<DataImportTool>>();

        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
        {
            Console.WriteLine("Invalid date format. Use YYYY-MM-DD format.");
            return 1;
        }

        logger.LogInformation("Getting import statistics from {StartDate} to {EndDate}", start, end);

        try
        {
            var stats = await importService.GetImportStatisticsAsync(start, end);

            Console.WriteLine($"Import statistics ({start} to {end}):");
            Console.WriteLine($"Total records imported: {stats.TotalRecordsImported:N0}");
            Console.WriteLine($"Import operations: {stats.ImportOperations}");
            Console.WriteLine($"Average records per operation: {stats.AvgRecordsPerOperation:F1}");

            if (stats.RecordsBySource.Any())
            {
                Console.WriteLine($"\nRecords by source:");
                foreach (var kvp in stats.RecordsBySource.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value:N0}");
                }
            }

            if (stats.RecordsBySymbol.Any())
            {
                Console.WriteLine($"\nTop symbols by record count:");
                foreach (var kvp in stats.RecordsBySymbol.Take(10))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value:N0}");
                }
            }

            Console.WriteLine($"\nData quality:");
            Console.WriteLine($"Average quality score: {stats.QualityStats.AvgQualityScore:F1}");
            Console.WriteLine($"Completeness percentage: {stats.QualityStats.CompletenessPercentage:F1}%");

            Console.WriteLine($"\nPerformance:");
            Console.WriteLine($"Average processing rate: {stats.PerformanceStats.AvgProcessingRate:F1} records/sec");
            Console.WriteLine($"Total processing time: {stats.PerformanceStats.TotalProcessingTime}");

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting statistics");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static IHost CreateHost()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Add database context
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? "Host=localhost;Port=5434;Database=mytrader;Username=postgres;Password=password";

                services.AddDbContext<TradingDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                });

                services.AddScoped<ITradingDbContext>(provider =>
                    provider.GetRequiredService<TradingDbContext>());

                // Add data import service
                services.AddScoped<IDataImportService, InfrastructureDataImportService>();
            })
            .Build();
    }
}