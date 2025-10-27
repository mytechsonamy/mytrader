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
// using MyTrader.Infrastructure.Monitoring;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Polly;
using Polly.Extensions.Http;
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

// Add services to the container with JSON configuration optimized for web frontend
builder.Services.AddControllers(options =>
{
    // Configure model validation
    options.ModelValidatorProviders.Clear();
})
.AddJsonOptions(options =>
{
    // Configure JSON serialization for web frontend compatibility
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.AllowTrailingCommas = true;
    options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
})
.ConfigureApiBehaviorOptions(options =>
{
    // Customize validation error responses for consistent web frontend consumption
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
            // Allow all localhost origins and local network IPs in development for React Native and web clients
            corsBuilder.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrEmpty(origin) || origin == "null") return true; // Allow null origin for mobile apps
                    try
                    {
                        var uri = new Uri(origin);
                        return uri.Host == "localhost" ||
                               uri.Host == "127.0.0.1" ||
                               uri.Host.StartsWith("192.168.") || // Local network range
                               uri.Host.StartsWith("10.") ||      // Private network range
                               uri.Host.StartsWith("172.") ||     // Private network range
                               uri.Host.EndsWith(".local") ||     // mDNS local domains
                               uri.Host == "expo.io" ||           // Expo dev server
                               uri.Host.EndsWith(".expo.dev");    // Expo cloud builds
                    }
                    catch
                    {
                        return false; // Invalid URI format
                    }
                })
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(5))
                // Explicitly allow SignalR headers for mobile and web clients
                .WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Requested-With", "x-signalr-user-agent", "X-SignalR-User-Agent");
        }
        else
        {
            // Production - restrict to specific origins including production domain
            corsBuilder.WithOrigins(
                           // Production domain
                           "https://mytrader.tech", "https://www.mytrader.tech",
                           "http://mytrader.tech", "http://www.mytrader.tech",  // Allow HTTP for redirects
                           // Web frontend origins (for local testing)
                           "http://localhost:3000", "https://localhost:3000",   // React dev server
                           "http://localhost:3002", "https://localhost:3002",   // Vite dev server (actual)
                           "http://localhost:5173", "https://localhost:5173",   // Vite dev server
                           "http://localhost:4173", "https://localhost:4173",   // Vite preview
                           "http://localhost:8080", "https://localhost:8080",   // Alternative web ports
                           // Mobile app origins
                           "http://localhost:8081", "https://localhost:8081",   // React Native Metro
                           "http://localhost:8084", "https://localhost:8084",   // Expo development
                           "http://localhost:3333", "https://localhost:3333",   // Alternative mobile port
                           "http://localhost:19006", "https://localhost:19006"  // Expo web
                       )
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials()
                       .SetPreflightMaxAge(TimeSpan.FromMinutes(5))
                       .WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Requested-With", "x-signalr-user-agent", "X-SignalR-User-Agent");
        }
    });
});

// Add database
var useInMemoryDatabase = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=mytrader;Username=postgres;Password=password";

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

// Add SignalR with enhanced configuration for WebSocket support and web browsers
builder.Services.AddSignalR(options =>
{
    // Optimize for web browser connections
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.StreamBufferCapacity = 10;
    options.MaximumParallelInvocationsPerClient = 1;
    options.StatefulReconnectBufferSize = 1000;
}).AddJsonProtocol(options =>
{
    // Configure JSON serialization for web frontend compatibility
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.WriteIndented = false;
    options.PayloadSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
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

// Register Symbol Management Service for SymbolPreferencesController
builder.Services.AddScoped<MyTrader.Core.Services.ISymbolManagementService, MyTrader.Infrastructure.Services.SymbolManagementService>();
builder.Services.AddScoped<MyTrader.Core.Services.ISymbolCacheService, MyTrader.Infrastructure.Services.SymbolCacheService>();

// Register multi-asset services
builder.Services.AddScoped<MyTrader.Core.Interfaces.IAssetClassService, MyTrader.Infrastructure.Services.AssetClassService>();
builder.Services.AddScoped<MyTrader.Core.Interfaces.IMarketService, MyTrader.Infrastructure.Services.MarketService>();

// Register singleton for data provider orchestrator to maintain provider state across requests
builder.Services.AddSingleton<MyTrader.Core.Interfaces.IDataProviderOrchestrator, MyTrader.Core.Services.DataProviderOrchestrator>();

// Register multi-asset data service with real database implementation
builder.Services.AddScoped<MyTrader.Core.Interfaces.IMultiAssetDataService, MyTrader.Infrastructure.Services.MultiAssetDataService>();
builder.Services.AddScoped<MyTrader.Core.Interfaces.IEnhancedSymbolService, MyTrader.Api.Services.MockEnhancedSymbolService>();
builder.Services.AddScoped<MyTrader.Core.Interfaces.IMarketStatusService, MyTrader.Api.Services.MockMarketStatusService>();

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

// Register database seeder service
builder.Services.AddScoped<MyTrader.Api.Services.DatabaseSeederService>();
// Register database initialization service
builder.Services.AddScoped<MyTrader.Api.Services.DatabaseInitializationService>();

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
// Register BinanceWebSocketService as both singleton and hosted service properly
builder.Services.AddSingleton<BinanceWebSocketService>();
builder.Services.AddSingleton<IBinanceWebSocketService>(provider => provider.GetRequiredService<BinanceWebSocketService>());
builder.Services.AddHostedService(provider => provider.GetRequiredService<BinanceWebSocketService>());

// Register MarketHoursService as singleton (required by MultiAssetDataBroadcastService)
builder.Services.AddSingleton<MyTrader.Core.Interfaces.IMarketHoursService, MyTrader.Core.Services.MarketHoursService>();

// Register YahooFinancePollingService as singleton (required by MultiAssetDataBroadcastService)
builder.Services.AddSingleton<YahooFinancePollingService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<YahooFinancePollingService>());

// Register StockDataPollingService for BIST, NASDAQ, NYSE stock data polling (10-second interval)
builder.Services.AddHostedService<MyTrader.Api.Services.StockDataPollingService>();

// Register AlpacaStreamingService for NASDAQ/NYSE real-time data (only if enabled in config)
builder.Services.AddSingleton<MyTrader.Infrastructure.Services.AlpacaStreamingService>();
builder.Services.AddSingleton<MyTrader.Infrastructure.Services.IAlpacaStreamingService>(provider =>
    provider.GetRequiredService<MyTrader.Infrastructure.Services.AlpacaStreamingService>());
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<MyTrader.Infrastructure.Services.AlpacaStreamingService>());

// Register DataSourceRouter for intelligent Alpaca (primary) / Yahoo (fallback) routing
builder.Services.AddSingleton<MyTrader.Core.Services.IDataSourceRouter, MyTrader.Core.Services.DataSourceRouter>();

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

// Add basic health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString,
        healthQuery: "SELECT 1;",
        name: "postgresql_database",
        tags: new[] { "database", "critical" })
    .AddCheck("memory_usage", () =>
    {
        var totalMemory = GC.GetTotalMemory(false);
        var maxMemory = 1024L * 1024 * 1024; // 1GB threshold

        if (totalMemory > maxMemory)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                $"Memory usage is high: {totalMemory / (1024 * 1024)} MB");
        }

        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
            $"Memory usage is normal: {totalMemory / (1024 * 1024)} MB");
    }, tags: new[] { "system", "performance" });

// TODO: Re-enable monitoring services when infrastructure is ready
// builder.Services.AddSingleton<SignalRHealthCheck>();
// builder.Services.AddSingleton<WebSocketHealthCheck>();
// builder.Services.AddSingleton<MarketDataHealthCheck>();
// builder.Services.AddSingleton<ApiEndpointHealthCheck>();
// builder.Services.AddSingleton<PrometheusMetricsExporter>();

// TODO: Re-enable alerting and monitoring services when infrastructure is ready
// builder.Services.Configure<AlertingConfiguration>(options => { ... });
// builder.Services.AddSingleton<AlertingService>();
// builder.Services.AddHostedService<HealthMonitoringService>();

// Add Health Checks UI for monitoring dashboard
builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(30); // Check every 30 seconds
    setup.MaximumHistoryEntriesPerEndpoint(50);
    setup.AddHealthCheckEndpoint("MyTrader API", "/health");
    setup.AddHealthCheckEndpoint("MyTrader Detailed", "/health/ready");
}).AddInMemoryStorage();

// Add Agent Pack Services (after dependencies are registered)
builder.Services.AddMyTraderAgentPack();

// Add Yahoo Finance Intraday Sync Services for 5-minute data collection
// Configure intraday service settings
builder.Services.Configure<MyTrader.Core.Services.YahooFinanceIntradayConfiguration>(options =>
{
    options.BatchSize = 5;
    options.BatchDelayMs = 500;
    options.InterMarketDelayMs = 1000;
    options.MaxRetryAttempts = 2;
    options.RetryDelayMs = 1000;
    options.OverwriteExistingData = false;
    options.LookbackMinutes = 15;
    options.MaxSymbolsPerMarket = 100; // Reduced for development
    options.MinSuccessRatePercent = 70.0m;
    options.EnableBistSync = true;
    options.EnableUSMarketsSync = true;
    options.EnableCryptoSync = true;
});

// Configure scheduled service settings
builder.Services.Configure<MyTrader.Infrastructure.Services.YahooFinanceIntradayScheduleConfiguration>(options =>
{
    options.MaxExecutionDuration = TimeSpan.FromMinutes(4);
    options.AlertAfterConsecutiveFailures = 3;
    options.LogDetailedResults = true; // Enable for debugging
    options.EnableHealthChecks = true;
    options.EnableDuringPremarket = false;
    options.EnableDuringAfterHours = false;
    options.EnablePerformanceLogging = true;
    options.PerformanceLogThreshold = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<MyTrader.Core.Services.YahooFinanceApiService>();
builder.Services.AddScoped<MyTrader.Core.Services.YahooFinanceIntradayDataService>();
builder.Services.AddHostedService<MyTrader.Infrastructure.Services.YahooFinanceIntradayScheduledService>();

// Configure Alpaca API settings
builder.Services.Configure<MyTrader.Core.DTOs.AlpacaConfiguration>(
    builder.Configuration.GetSection("Alpaca"));

// Add Alpaca market data services (simplified version for development)
builder.Services.AddScoped<MyTrader.Core.Interfaces.IAlpacaMarketDataService, MyTrader.Infrastructure.Services.AlpacaMarketDataServiceSimplified>();

// Add HTTP client for Alpaca (simplified)
builder.Services.AddHttpClient<MyTrader.Infrastructure.Services.AlpacaMarketDataServiceSimplified>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MyTrader/1.0");
});

// Add Alpaca background data refresh service (simplified version)
// Temporarily disabled to avoid conflicts with the simplified service
// builder.Services.AddHostedService<MyTrader.Infrastructure.Services.AlpacaDataRefreshService>();

// Add BIST market data services
builder.Services.AddBistServices(builder.Configuration);

// Add ETL Services
builder.Services.AddScoped<MyTrader.Core.Services.ETL.IDataIntegrityETLService, MyTrader.Infrastructure.Services.ETL.DataIntegrityETLService>();
builder.Services.AddScoped<MyTrader.Core.Services.ETL.ISymbolSynchronizationService, MyTrader.Infrastructure.Services.ETL.SymbolSynchronizationService>();
builder.Services.AddScoped<MyTrader.Core.Services.ETL.IAssetEnrichmentService, MyTrader.Infrastructure.Services.ETL.AssetEnrichmentService>();
builder.Services.AddScoped<MyTrader.Core.Services.ETL.IMarketDataBootstrapService, MyTrader.Infrastructure.Services.ETL.MarketDataBootstrapService>();

// AssetClass Correction ETL Services temporarily disabled for authentication fix
// builder.Services.AddScoped<MyTrader.Core.Services.ETL.IMarketDataAssetClassCorrectionService, MyTrader.Infrastructure.Services.ETL.MarketDataAssetClassCorrectionService>();
// builder.Services.AddSingleton<MyTrader.Infrastructure.Monitoring.AssetClassCorrectionMonitor>();

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

// In development, avoid forcing HTTPS because local certs and mobile simulators
// can cause negotiation/login to fail.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("DefaultPolicy");

// Add mobile response unwrapping middleware (before authentication)
app.UseMobileResponseUnwrapping();

// Add custom validation middleware
app.UseValidationMiddleware();

// Add API metrics collection middleware
// app.UseApiMetrics(); // TODO: Implement metrics middleware

// Add Prometheus metrics endpoint
// app.UsePrometheusMetrics(); // TODO: Implement Prometheus metrics

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<TradingHub>("/hubs/trading");
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapHub<MockTradingHub>("/hubs/mock-trading");
app.MapHub<MarketDataHub>("/hubs/market-data");
app.MapHub<PortfolioHub>("/hubs/portfolio");

// Native batch processing dashboard removed - use API endpoints at /api/batch-processing/*
// Health check and monitoring available through REST API instead of Hangfire dashboard

// Map health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                description = e.Value.Description,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message,
                tags = e.Value.Tags
            }).ToArray()
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

// Health check UI (monitoring dashboard)
app.MapHealthChecksUI(setup =>
{
    setup.UIPath = "/health-ui";
    setup.ApiPath = "/health-ui-api";
});

// Health check endpoints by category
app.MapHealthChecks("/health/critical", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("critical")
});

app.MapHealthChecks("/health/database", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database")
});

app.MapHealthChecks("/health/realtime", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("realtime")
});

// Simple API info endpoint
app.MapGet("/", () => Results.Ok(new {
    name = "MyTrader API",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow,
    endpoints = new {
        health = "/health",
        healthDashboard = "/health-ui",
        healthCritical = "/health/critical",
        healthDatabase = "/health/database",
        healthRealtime = "/health/realtime",
        metrics = "/metrics",
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
    if (!Microsoft.EntityFrameworkCore.InMemoryDatabaseFacadeExtensions.IsInMemory(context.Database))
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
