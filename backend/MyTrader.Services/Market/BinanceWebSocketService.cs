using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyTrader.Services.Market;

public interface IBinanceWebSocketService
{
    Task StartAsync();
    Task StopAsync();
    Task SubscribeToSymbolsAsync(List<string> symbols);
    event Action<PriceUpdateData>? PriceUpdated;
}

public class PriceUpdateData
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal PriceChange { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
}

public class BinanceWebSocketService : BackgroundService, IBinanceWebSocketService
{
    private readonly ILogger<BinanceWebSocketService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource _cancellationTokenSource = new();
    private List<string> _symbols = new();
    
    // Binance WebSocket API endpoints
    private const string BinanceWsUrl = "wss://stream.binance.com:9443/stream";

    public event Action<PriceUpdateData>? PriceUpdated;

    public BinanceWebSocketService(ILogger<BinanceWebSocketService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LoadSymbolsFromDatabaseAsync();
        await StartAsync();
        
        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);
                
                // Check if WebSocket is still alive
                if (_webSocket?.State != WebSocketState.Open)
                {
                    _logger.LogWarning("WebSocket connection lost, attempting to reconnect...");
                    await ReconnectAsync();
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

    public async Task StartAsync()
    {
        try
        {
            await ConnectAsync();
            await SubscribeToSymbolsAsync(_symbols);
            _ = Task.Run(ListenForMessagesAsync); // Start listening in background
            
            _logger.LogInformation("Binance WebSocket service started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Binance WebSocket service");
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            _cancellationTokenSource.Cancel();
            
            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service stopping", CancellationToken.None);
            }
            
            _webSocket?.Dispose();
            _logger.LogInformation("Binance WebSocket service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Binance WebSocket service");
        }
    }

    public async Task SubscribeToSymbolsAsync(List<string> symbols)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            _logger.LogWarning("Cannot subscribe: WebSocket is not connected");
            return;
        }

        try
        {
            // Build stream names for multiple symbols (e.g., btcusdt@ticker/ethusdt@ticker)
            var streams = symbols.Select(s => $"{s.ToLower()}@ticker").ToList();
            var streamUrl = $"{BinanceWsUrl}?streams={string.Join("/", streams)}";

            _logger.LogInformation("Subscribing to symbols: {Symbols}", string.Join(", ", symbols));
            _logger.LogInformation("WebSocket URL: {Url}", streamUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to symbols");
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            
            // Build stream names for ticker data (24hr ticker statistics)
            var streams = _symbols.Select(s => $"{s.ToLower()}@ticker").ToList();
            var uri = new Uri($"{BinanceWsUrl}?streams={string.Join("/", streams)}");
            
            _logger.LogInformation("Connecting to Binance WebSocket: {Uri}", uri);
            
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
            
            _logger.LogInformation("Successfully connected to Binance WebSocket");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Binance WebSocket");
            throw;
        }
    }

    private async Task ReconnectAsync()
    {
        const int maxRetries = 5;
        var retryCount = 0;
        
        while (retryCount < maxRetries && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync();
                _ = Task.Run(ListenForMessagesAsync); // Restart listening
                _logger.LogInformation("Successfully reconnected to Binance WebSocket");
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff
                
                _logger.LogWarning(ex, "Reconnection attempt {RetryCount}/{MaxRetries} failed. Retrying in {Delay} seconds", 
                    retryCount, maxRetries, delay.TotalSeconds);
                
                await Task.Delay(delay, _cancellationTokenSource.Token);
            }
        }
        
        _logger.LogError("Failed to reconnect after {MaxRetries} attempts", maxRetries);
    }

    private async Task ListenForMessagesAsync()
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
                        
                        await ProcessMessageAsync(completeMessage);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket connection closed by server");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebSocket listening cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while listening to WebSocket messages");
        }
    }

    private async Task ProcessMessageAsync(string message)
    {
        try
        {
            var json = JObject.Parse(message);
            
            // Check if it's a stream message
            if (json["stream"] != null && json["data"] != null)
            {
                var streamName = json["stream"]?.ToString();
                var data = json["data"] as JObject;
                
                if (streamName != null && data != null && streamName.Contains("@ticker"))
                {
                    await ProcessTickerDataAsync(data);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse WebSocket message: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket message");
        }
    }

    private async Task ProcessTickerDataAsync(JObject tickerData)
    {
        try
        {
            // Extract ticker data (Binance 24hr ticker format)
            var symbol = tickerData["s"]?.ToString(); // Symbol (e.g., "BTCUSDT")
            var price = decimal.Parse(tickerData["c"]?.ToString() ?? "0"); // Current price
            var priceChange = decimal.Parse(tickerData["P"]?.ToString() ?? "0"); // Price change percentage
            var volume = decimal.Parse(tickerData["v"]?.ToString() ?? "0"); // Volume
            
            if (string.IsNullOrEmpty(symbol)) return;

            _logger.LogDebug("Received ticker data for {Symbol}: Price={Price}, Change={Change}%", 
                symbol, price, priceChange);

            // Fire event for price update
            PriceUpdated?.Invoke(new PriceUpdateData
            {
                Symbol = symbol,
                Price = price,
                PriceChange = priceChange,
                Volume = volume,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ticker data");
        }
    }

    private async Task LoadSymbolsFromDatabaseAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var symbolService = scope.ServiceProvider.GetService<ISymbolService>();
            
            if (symbolService != null)
            {
                var symbols = await symbolService.GetTrackedAsync("BINANCE");
                _symbols = symbols.Where(s => s.IsTracked)
                                .Select(s => s.Ticker)
                                .ToList();
                
                _logger.LogInformation("Loaded {Count} symbols from database: {Symbols}", 
                    _symbols.Count, string.Join(", ", _symbols));
            }
            else
            {
                // Fallback to default symbols if service not available
                _symbols = new List<string> 
                { 
                    "BTCUSDT", "ETHUSDT", "XRPUSDT", "BNBUSDT", 
                    "ADAUSDT", "SOLUSDT", "DOTUSDT", "AVAXUSDT", "LINKUSDT" 
                };
                
                _logger.LogWarning("SymbolService not available, using fallback symbols: {Symbols}", 
                    string.Join(", ", _symbols));
            }
            
            if (!_symbols.Any())
            {
                // Ultimate fallback if no symbols found
                _symbols = new List<string> { "BTCUSDT", "ETHUSDT" };
                _logger.LogWarning("No symbols found in database, using minimal fallback: {Symbols}", 
                    string.Join(", ", _symbols));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading symbols from database, using fallback");
            _symbols = new List<string> { "BTCUSDT", "ETHUSDT" };
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopAsync();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _webSocket?.Dispose();
        _cancellationTokenSource.Dispose();
        base.Dispose();
    }
}