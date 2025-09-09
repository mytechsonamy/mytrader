using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Authentication;
using MyTrader.Services.Market;
using MyTrader.Services.Signals;
using MyTrader.Services.Trading;
using MyTrader.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", builder =>
    {
        builder.WithOrigins(
                   "http://localhost:3000", "https://localhost:3000", 
                   "http://localhost:8081", "https://localhost:8081",
                   "http://localhost:8084", "https://localhost:8084",
                   "http://192.168.68.103:8081", "https://192.168.68.103:8081",
                   "http://192.168.68.103:8084", "https://192.168.68.103:8084",
                   "http://192.168.68.103:8080", "https://192.168.68.103:8080",
                   "file://", // Local HTML files
                   "null" // Some browsers send null for local files
               )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=mytrader;Username=postgres;Password=password";

builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-secret-key-change-in-production-make-it-longer-than-256-bits";
var key = Encoding.ASCII.GetBytes(jwtSecret);

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
        ClockSkew = TimeSpan.Zero
    };
    
    // Enable JWT tokens for SignalR
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
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

// Add application services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<ISignalService, SignalService>();
builder.Services.AddScoped<IIndicatorService, IndicatorService>();
builder.Services.AddScoped<ITradingStrategyService, TradingStrategyService>();

// Add Binance WebSocket service as singleton for real-time data
builder.Services.AddSingleton<IBinanceWebSocketService, BinanceWebSocketService>();
builder.Services.AddHostedService<BinanceWebSocketService>(provider => 
    (BinanceWebSocketService)provider.GetService<IBinanceWebSocketService>()!);

// Add HTTP client
builder.Services.AddHttpClient();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<TradingHub>("/hub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not ensure database creation. This may be expected in development.");
    }
}

// Set up WebSocket event handler for SignalR hub
using (var scope = app.Services.CreateScope())
{
    var webSocketService = scope.ServiceProvider.GetRequiredService<IBinanceWebSocketService>();
    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TradingHub>>();
    
    // Subscribe to price updates and broadcast to SignalR clients
    webSocketService.PriceUpdated += async (priceData) =>
    {
        try
        {
            await hubContext.Clients.All.SendAsync("ReceivePriceUpdate", new
            {
                symbol = priceData.Symbol,
                price = priceData.Price,
                change = priceData.PriceChange,
                volume = priceData.Volume,
                timestamp = priceData.Timestamp.ToString("O")
            });

            await hubContext.Clients.All.SendAsync("ReceiveMarketData", new
            {
                symbols = new Dictionary<string, object>
                {
                    [priceData.Symbol] = new
                    {
                        price = priceData.Price,
                        change = priceData.PriceChange,
                        volume = priceData.Volume,
                        timestamp = priceData.Timestamp.ToString("O")
                    }
                }
            });
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error broadcasting price update for {Symbol}", priceData.Symbol);
        }
    };
}

app.Run();
