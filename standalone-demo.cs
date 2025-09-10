using Microsoft.AspNetCore.SignalR;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", corsBuilder =>
    {
        corsBuilder.WithOrigins("*")
                   .AllowAnyMethod()
                   .AllowAnyHeader();
    });
});

// Add SignalR
builder.Services.AddSignalR();

// Add our demo services
builder.Services.AddSingleton<DemoBinanceService>();
builder.Services.AddHostedService<DemoBinanceService>();

var app = builder.Build();

app.UseCors("DefaultPolicy");
app.MapHub<DemoTradingHub>("/hubs/trading");

app.MapGet("/", () => Results.Ok(new {
    name = "MyTrader Demo API",
    status = "running - Real Binance Data",
    timestamp = DateTime.UtcNow,
    endpoints = new {
        signalr = "/hubs/trading"
    }
}));

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    message = "Demo API with real Binance data" 
}));

Console.WriteLine("🚀 MyTrader Demo API starting...");
Console.WriteLine("📊 Real-time Binance data enabled");
Console.WriteLine("🎯 SignalR Hub: /hubs/trading");
Console.WriteLine("🌐 Test page: test-signalr.html");

app.Run("http://localhost:5245");

// Demo SignalR Hub
public class DemoTradingHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

// Demo Binance Service
public class DemoBinanceService : BackgroundService
{
    private readonly IHubContext<DemoTradingHub> _hubContext;
    private readonly ILogger<DemoBinanceService> _logger;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource _cancellationTokenSource = new();
    
    private const string BinanceWsUrl = "wss://stream.binance.com:9443/stream";
    private readonly List<string> _symbols = new()
    {
        "BTCUSDT", "ETHUSDT", "XRPUSDT", "BNBUSDT", 
        "ADAUSDT", "SOLUSDT", "DOTUSDT", "MATICUSDT", 
        "AVAXUSDT", "LINKUSDT"
    };

    // Price history for technical indicators
    private readonly Dictionary<string, Queue<decimal>> _priceHistory = new();

    public DemoBinanceService(IHubContext<DemoTradingHub> hubContext, ILogger<DemoBinanceService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        
        // Initialize price history
        foreach (var symbol in _symbols)
        {
            _priceHistory[symbol] = new Queue<decimal>();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartWebSocket();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);
                
                if (_webSocket?.State != WebSocketState.Open)
                {
                    _logger.LogWarning("WebSocket connection lost, reconnecting...");
                    await StartWebSocket();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket monitoring");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task StartWebSocket()
    {
        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            
            var streams = _symbols.Select(s => $"{s.ToLower()}@ticker").ToList();
            var uri = new Uri($"{BinanceWsUrl}?streams={string.Join("/", streams)}");
            
            _logger.LogInformation("Connecting to Binance: {Uri}", uri);
            
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
            
            _logger.LogInformation("Connected to Binance WebSocket");
            
            _ = Task.Run(ListenForMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Binance");
        }
    }

    private async Task ListenForMessages()
    {
        var buffer = new byte[4096];
        var messageBuffer = new StringBuilder();
        
        try
        {
            while (_webSocket?.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuffer.Append(message);
                    
                    if (result.EndOfMessage)
                    {
                        var completeMessage = messageBuffer.ToString();
                        messageBuffer.Clear();
                        
                        await ProcessMessage(completeMessage);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listening to WebSocket");
        }
    }

    private async Task ProcessMessage(string message)
    {
        try
        {
            var json = JObject.Parse(message);
            
            if (json["stream"] != null && json["data"] != null)
            {
                var streamName = json["stream"]?.ToString();
                var data = json["data"] as JObject;
                
                if (streamName != null && data != null && streamName.Contains("@ticker"))
                {
                    await ProcessTickerData(data);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse message");
        }
    }

    private async Task ProcessTickerData(JObject tickerData)
    {
        try
        {
            var symbol = tickerData["s"]?.ToString();
            var price = decimal.Parse(tickerData["c"]?.ToString() ?? "0");
            var priceChange = decimal.Parse(tickerData["P"]?.ToString() ?? "0");
            var volume = decimal.Parse(tickerData["v"]?.ToString() ?? "0");
            
            if (string.IsNullOrEmpty(symbol)) return;

            // Add to price history
            if (!_priceHistory.ContainsKey(symbol))
                _priceHistory[symbol] = new Queue<decimal>();
                
            _priceHistory[symbol].Enqueue(price);
            if (_priceHistory[symbol].Count > 50) // Keep last 50 prices
                _priceHistory[symbol].Dequeue();

            // Calculate simple indicators
            var indicators = CalculateIndicators(symbol, price);

            // Broadcast price update
            await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", new
            {
                symbol = symbol,
                price = price,
                change = priceChange,
                volume = volume,
                timestamp = DateTime.UtcNow.ToString("O")
            });

            // Broadcast indicators
            await _hubContext.Clients.All.SendAsync("ReceiveSignalUpdate", new
            {
                symbol = symbol,
                signal = indicators.Signal,
                trend = indicators.Trend,
                indicators = new
                {
                    rsi = indicators.RSI,
                    sma = indicators.SMA,
                    price_above_sma = price > indicators.SMA
                },
                timestamp = DateTime.UtcNow.ToString("O")
            });

            _logger.LogDebug("Processed {Symbol}: ${Price} ({Change}%) RSI={RSI} Signal={Signal}", 
                symbol, price, priceChange, indicators.RSI, indicators.Signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ticker data");
        }
    }

    private (decimal? RSI, decimal? SMA, string Signal, string Trend) CalculateIndicators(string symbol, decimal price)
    {
        var prices = _priceHistory[symbol].ToArray();
        
        if (prices.Length < 14) 
            return (null, null, "HOLD", "UNKNOWN");

        // Simple RSI calculation
        var gains = new List<decimal>();
        var losses = new List<decimal>();
        
        for (int i = 1; i < prices.Length; i++)
        {
            var change = prices[i] - prices[i - 1];
            if (change > 0)
            {
                gains.Add(change);
                losses.Add(0);
            }
            else
            {
                gains.Add(0);
                losses.Add(Math.Abs(change));
            }
        }

        var avgGain = gains.TakeLast(14).Average();
        var avgLoss = losses.TakeLast(14).Average();
        
        var rsi = avgLoss == 0 ? 100m : 100 - (100 / (1 + (avgGain / avgLoss)));
        
        // Simple Moving Average
        var sma = prices.TakeLast(Math.Min(20, prices.Length)).Average();
        
        // Generate signals
        string signal;
        if (rsi < 30) signal = "BUY";
        else if (rsi > 70) signal = "SELL";
        else signal = "HOLD";
        
        // Determine trend
        string trend;
        if (price > sma) trend = "UPTREND";
        else if (price < sma * 0.98m) trend = "DOWNTREND";
        else trend = "SIDEWAYS";

        return (Math.Round(rsi, 2), Math.Round(sma, 2), signal, trend);
    }

    public override void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _webSocket?.Dispose();
        _cancellationTokenSource.Dispose();
        base.Dispose();
    }
}