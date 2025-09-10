using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MyTrader.Infrastructure.Data;

public class TradingDbContextFactory : IDesignTimeDbContextFactory<TradingDbContext>
{
    public TradingDbContext CreateDbContext(string[] args)
    {
        // Prefer env var, then a sane local default
        var envConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        string? connectionString = envConn;

        connectionString ??= "Host=localhost;Port=5434;Database=mytrader;Username=postgres;Password=password";

        var optionsBuilder = new DbContextOptionsBuilder<TradingDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new TradingDbContext(optionsBuilder.Options);
    }
}
