using Microsoft.Extensions.Logging;
using MyTrader.Core.Data;
using MyTrader.Core.Services;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of DataImportService
/// This inherits from the Core service implementation and can add infrastructure-specific features
/// </summary>
public class InfrastructureDataImportService : DataImportService
{
    public InfrastructureDataImportService(
        ITradingDbContext dbContext,
        ILogger<DataImportService> logger)
        : base(dbContext, logger)
    {
    }

    // Add any infrastructure-specific overrides or extensions here
    // For example: Azure Blob Storage integration, file system monitoring, etc.
}