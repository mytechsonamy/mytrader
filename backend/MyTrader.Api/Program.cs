using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MyTrader.Infrastructure.Data;
using MyTrader.Infrastructure.Extensions;
using MyTrader.Services.Market;
using MyTrader.Core.Interfaces;
using MyTrader.Api.Hubs;
using MyTrader.Api.Services;
using MyTrader.Api.Setup;
using MyTrader.API.Hubs;
using MyTrader.Api.Middleware;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
// Removed Hangfire dependencies - replaced with native .NET background services

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for WebSocket support
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = false;
    options.DisableStringReuse = false;
});

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.GrafanaLoki("http://localhost:3100", new[]
    {
        new LokiLabel { Key = "app", Value = "mytrader-api" },
        new LokiLabel { Key = "environment", Value = builder.Environment.EnvironmentName }
    })
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers(options =>
{
    // Configure model validation
    options.ModelValidatorProviders.Clear();
})
.ConfigureApiBehaviorOptions(options =>
{
    // Customize validation error responses
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
            );

        var response = new MyTrader.Core.DTOs.ValidationErrorResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors
        };

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
    };
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MyTrader API",
        Version = "v1",
        Description = "Trading platform API"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", corsBuilder =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow all localhost origins in development for React Native and web clients
            corsBuilder.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrEmpty(origin)) return true; // Allow null origin for mobile apps
                    var uri = new Uri(origin);
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host.EndsWith(".local");
                })
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(5))
                // Explicitly allow SignalR headers
                .WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Requested-With");
        }
        else
        {
            // Production - restrict to specific origins
            corsBuilder.WithOrigins(
                           "http://localhost:3000", "https://localhost:3000",
                           "http://localhost:8081", "https://localhost:8081",
                           "http://localhost:8084", "https://localhost:8084",
                           "http://localhost:3333", "https://localhost:3333"
                       )
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials()
                       .SetPreflightMaxAge(TimeSpan.FromMinutes(5))
                       .WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Requested-With");
        }
    });
});

// Add database
var useInMemoryDatabase = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5434;Database=mytrader;Username=postgres;Password=password";

builder.Services.AddDbContext<TradingDbContext>(options =>
{
    if (useInMemoryDatabase)
    {
        options.UseInMemoryDatabase("MyTraderTestDb");
    }
    else
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Disable any naming conventions that might convert to snake_case
        });
    }
    // Suppress pending model changes warning during development
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Add authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "your_super_secret_jwt_key_for_development_only_at_least_256_bits_long_abcdef123456";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "sub" // Use 'sub' claim for user identification
    };
    
    // Configure JWT for SignalR
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // Allow anonymous access to dashboard and market-data hubs
            if (path.StartsWithSegments("/hubs/dashboard") || path.StartsWithSegments("/hubs/market-data"))
            {
                return Task.CompletedTask;
            }

            // For SignalR hubs, check for token in query string or Authorization header
            if (path.StartsWithSegments("/hubs"))
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                else
                {
                    // Check Authorization header
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer ") == true)
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add SignalR with enhanced configuration for WebSocket support
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.StreamBufferCapacity = 10;
}).AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Add MyTrader core services
// builder.Services.AddMyTraderCore();

// Register core services (DB-backed)
builder.Services.AddScoped<MyTrader.Services.Authentication.IAuthenticationService, MyTrader.Services.Authentication.AuthenticationService>();
builder.Services.AddScoped<MyTrader.Services.Authentication.IEmailService, MyTrader.Services.Authentication.EmailService>();
// builder.Services.AddScoped<ISymbolService, SymbolService>();
// builder.Services.AddScoped<IMarketDataService, MarketDataService>();

// Register backtest and strategy services
builder.Services.AddScoped<MyTrader.Core.Services.IBacktestEngine, MyTrader.Core.Services.BacktestEngine>();
builder.Services.AddScoped<MyTrader.Core.Services.IIndicatorCalculator, MyTrader.Core.Services.IndicatorCalculator>();
builder.Services.AddScoped<MyTrader.Core.Services.IMarketDataService, MyTrader.Core.Services.MarketDataService>();
builder.Services.AddScoped<MyTrader.Core.Services.IStrategyManagementService, MyTrader.Core.Services.StrategyManagementService>();
builder.Services.AddScoped<MyTrader.Core.Services.IPerformanceTrackingService, MyTrader.Core.Services.PerformanceTrackingService>();

// Register additional services for controllers
// Authentication services are now enabled and registered above
builder.Services.AddScoped<MyTrader.Services.Market.ISymbolService, MyTrader.Services.Market.SymbolService>();
builder.Services.AddScoped<MyTrader.Core.Services.IIndicatorService, MyTrader.Core.Services.IndicatorService>();
builder.Services.AddScoped<MyTrader.Core.Services.ISignalGenerationEngine, MyTrader.Core.Services.SignalGenerationEngine>();
builder.Services.AddScoped<MyTrader.Core.Services.ITradingStrategyService, MyTrader.Core.Services.TradingStrategyService>();

// Register multi-asset services
builder.Services.AddScoped<MyTrader.Core.Interfaces.IAssetClassService, MyTrader.Infrastructure.Services.AssetClassService>();
builder.Services.AddScoped<MyTrader.Core.Interfaces.IMarketService, MyTrader.Infrastructure.Services.MarketService>();

// Register singleton for data provider orchestrator to maintain provider state across requests
builder.Services.AddSingleton<MyTrader.Core.Interfaces.IDataProviderOrchestrator, MyTrader.Core.Services.DataProviderOrchestrator>();

// Note: Additional service implementations (IEnhancedSymbolService, IMultiAssetDataService, IMarketStatusService)
// need to be implemented and registered when ready

// Register portfolio service
//builder.Services.AddScoped<IPortfolioService, MyTrader.Services.Portfolio.NewPortfolioService>();
builder.Services.AddScoped<IPortfolioService, MyTrader.API.Services.InMemoryPortfolioService>();

// Register gamification service
builder.Services.AddScoped<MyTrader.Services.Gamification.IGamificationService, MyTrader.Services.Gamification.GamificationService>();

// Register notification service - temporarily disabled due to missing models
// builder.Services.AddScoped<MyTrader.Services.Notifications.INotificationService, MyTrader.Services.Notifications.NotificationService>();

// Register analytics service - temporarily disabled due to missing models
// builder.Services.AddScoped<MyTrader.Services.Analytics.IAnalyticsService, MyTrader.Services.Analytics.AnalyticsService>();

// Register education service - temporarily disabled due to missing models
// builder.Services.AddScoped<MyTrader.Services.Education.IEducationService, MyTrader.Services.Education.EducationService>();

// Register DbContext interface
builder.Services.AddScoped<MyTrader.Core.Data.ITradingDbContext>(provider =>
    provider.GetRequiredService<TradingDbContext>());

// Register data import service for Stock_Scrapper data
builder.Services.AddScoped<MyTrader.Core.Interfaces.IDataImportService, MyTrader.Infrastructure.Services.InfrastructureDataImportService>();

// Add native .NET batch processing services (replaces Hangfire)
builder.Services.AddSingleton<MyTrader.Infrastructure.Services.BatchProcessing.MemoryJobStore>();
builder.Services.AddSingleton<MyTrader.Core.Services.BatchProcessing.IJobStore>(provider =>
    provider.GetRequiredService<MyTrader.Infrastructure.Services.BatchProcessing.MemoryJobStore>());
builder.Services.AddSingleton<MyTrader.Core.Services.BatchProcessing.IRecurringJobStore>(provider =>
    provider.GetRequiredService<MyTrader.Infrastructure.Services.BatchProcessing.MemoryJobStore>());

// Register native batch processing services
builder.Services.AddSingleton<MyTrader.Infrastructure.Services.BatchProcessing.NativeBatchJobOrchestrator>();
builder.Services.AddSingleton<MyTrader.Core.Services.BatchProcessing.IBatchJobOrchestrator>(provider =>
    provider.GetRequiredService<MyTrader.Infrastructure.Services.BatchProcessing.NativeBatchJobOrchestrator>());
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<MyTrader.Infrastructure.Services.BatchProcessing.NativeBatchJobOrchestrator>());

// Register background services: WebSocket + Daily backtest automation
builder.Services.AddSingleton<IBinanceWebSocketService, BinanceWebSocketService>();
builder.Services.AddHostedService<BinanceWebSocketService>();

// Register enhanced multi-asset data broadcast service (replaces old MarketDataBroadcastService)
builder.Services.AddHostedService<MyTrader.Api.Services.MultiAssetDataBroadcastService>();

// Register market status monitoring service for market hours tracking
builder.Services.AddHostedService<MyTrader.Infrastructure.Services.MarketStatusMonitoringService>();

// Portfolio Monitoring Service for real-time portfolio updates
builder.Services.AddHostedService<MyTrader.API.Services.PortfolioMonitoringService>();
// PriceToDbWriter COMPLETELY DISABLED due to excessive DB writes causing memory issues
// builder.Services.AddHostedService<PriceToDbWriter>();
// DailyBacktestService ENABLED for controlled backtesting and strategy optimization
builder.Services.AddHostedService<MyTrader.Core.Services.DailyBacktestService>();
// BacktestQueueProcessor for recursive backtesting system
builder.Services.AddHostedService<MyTrader.Core.Services.BacktestQueueProcessor>();

// Add HTTP client
builder.Services.AddHttpClient();
builder.Services.AddScoped<BacktestServiceSimple>();

// Add memory cache
builder.Services.AddMemoryCache();

// Add logging
builder.Services.AddLogging();

// Add Agent Pack Services (after dependencies are registered)
builder.Services.AddMyTraderAgentPack();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyTrader API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

// Add custom validation middleware
app.UseValidationMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<TradingHub>("/hubs/trading");
// app.MapHub<MyTrader.Api.Hubs.DashboardHub>("/hubs/dashboard"); // Temporarily disabled - dependency issues
app.MapHub<MockTradingHub>("/hubs/mock-trading");
app.MapHub<MarketDataHub>("/hubs/market-data");
app.MapHub<PortfolioHub>("/hubs/portfolio");

// Native batch processing dashboard removed - use API endpoints at /api/batch-processing/*
// Health check and monitoring available through REST API instead of Hangfire dashboard

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    message = "MyTrader API is running" 
}));

// Simple API info endpoint
app.MapGet("/", () => Results.Ok(new {
    name = "MyTrader API",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow,
    endpoints = new {
        health = "/health",
        auth = "/api/auth",
        swagger = "/swagger",
        hubs = "/hubs/trading"
    }
}));

// Test logging endpoint
app.MapGet("/test-logging", (ILogger<Program> logger) => 
{
    logger.LogInformation("Test log message from API - Information level");
    logger.LogWarning("Test log message from API - Warning level");
    logger.LogError("Test log message from API - Error level");
    
    return Results.Ok(new { 
        message = "Test logs sent to Loki",
        timestamp = DateTime.UtcNow 
    });
});

// Apply pending EF Core migrations at startup (skip for in-memory database)
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

    // Only migrate if not using in-memory database
    if (!context.Database.IsInMemory())
    {
        await context.Database.MigrateAsync();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database migrations applied successfully");
    }
    else
    {
        // For in-memory database, ensure the database is created
        await context.Database.EnsureCreatedAsync();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("In-memory database created successfully");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
    logger.LogError(ex, "Failed to initialize database on startup");
}

Console.WriteLine("🚀 MyTrader API starting...");
Console.WriteLine($"🌍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"📊 Database: Enabled ({builder.Configuration.GetConnectionString("DefaultConnection")})");
Console.WriteLine($"💾 Redis: {(string.IsNullOrEmpty(builder.Configuration.GetConnectionString("Redis")) ? "Not configured" : "Configured")}");
Console.WriteLine("🎯 Available endpoints:");
Console.WriteLine("   GET  /           - API info");
Console.WriteLine("   GET  /health     - Health check");
Console.WriteLine("   POST /api/auth/* - Authentication");
Console.WriteLine("   POST /api/batch-processing/* - Batch processing jobs");
Console.WriteLine("   GET  /api/batch-processing/health - Batch processing health check");
Console.WriteLine("   WS   /hubs/trading - SignalR trading hub");
Console.WriteLine("   WS   /hubs/dashboard - SignalR dashboard hub (anonymous)");

app.Run();

// Make Program class accessible for integration testing
public partial class Program { }
