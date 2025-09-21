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
using Serilog;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", corsBuilder =>
    {
        corsBuilder.WithOrigins(
                       "http://localhost:3000", "https://localhost:3000",
                       "http://localhost:8081", "https://localhost:8081",
                       "http://localhost:8084", "https://localhost:8084",
                       "http://localhost:3333", "https://localhost:3333"
                   )
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
    });
});

// Add database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5434;Database=mytrader;Username=postgres;Password=password";

builder.Services.AddDbContext<TradingDbContext>(options =>
{
    options.UseNpgsql(connectionString);
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
            
            // Allow anonymous access to dashboard hub
            if (path.StartsWithSegments("/hubs/dashboard"))
            {
                return Task.CompletedTask;
            }
            
            // Require authentication for other hubs
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add SignalR
builder.Services.AddSignalR();

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

// Register background services: WebSocket + Daily backtest automation  
builder.Services.AddSingleton<IBinanceWebSocketService, BinanceWebSocketService>();
builder.Services.AddHostedService<BinanceWebSocketService>();
// MarketDataBroadcastService to connect Binance WebSocket to SignalR
builder.Services.AddHostedService<MyTrader.Api.Services.MarketDataBroadcastService>();
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
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<TradingHub>("/hubs/trading");
// app.MapHub<DashboardHub>("/hubs/dashboard"); // Temporarily disabled - reference issue
app.MapHub<MockTradingHub>("/hubs/mock-trading");
app.MapHub<MarketDataHub>("/hubs/market-data");
app.MapHub<PortfolioHub>("/hubs/portfolio");

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

// Apply pending EF Core migrations at startup
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    await context.Database.MigrateAsync();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Database migrations applied successfully");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
    logger.LogError(ex, "Failed to apply database migrations on startup");
}

Console.WriteLine("🚀 MyTrader API starting...");
Console.WriteLine($"🌍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"📊 Database: Enabled ({builder.Configuration.GetConnectionString("DefaultConnection")})");
Console.WriteLine($"💾 Redis: {(string.IsNullOrEmpty(builder.Configuration.GetConnectionString("Redis")) ? "Not configured" : "Configured")}");
Console.WriteLine("🎯 Available endpoints:");
Console.WriteLine("   GET  /           - API info");
Console.WriteLine("   GET  /health     - Health check");
Console.WriteLine("   POST /api/auth/* - Authentication");
Console.WriteLine("   WS   /hubs/trading - SignalR trading hub");
Console.WriteLine("   WS   /hubs/dashboard - SignalR dashboard hub (anonymous)");

app.Run();
