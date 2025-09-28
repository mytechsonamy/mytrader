using System.Collections.Concurrent;
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
    PriceUpdateData? GetLatestPrice(string symbol);
    IReadOnlyCollection<PriceUpdateData> GetAllLatestPrices();
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
    private readonly ConcurrentDictionary<string, PriceUpdateData> _latestPrices = new(StringComparer.OrdinalIgnoreCase);
    
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
        _logger.LogInformation("Starting Binance WebSocket service with enhanced monitoring");

        await LoadSymbolsFromDatabaseAsync();
        await StartAsync();

        var healthCheckCounter = 0;
        var lastHealthReport = DateTime.UtcNow;

        // Keep the service running with enhanced monitoring
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);
                healthCheckCounter++;

                // Check if WebSocket is still alive
                if (_webSocket?.State != WebSocketState.Open)
                {
                    _logger.LogWarning("WebSocket connection lost (State: {State}), attempting to reconnect...",
                        _webSocket?.State.ToString() ?? "null");
                    await ReconnectAsync();
                }

                // Periodic health reporting (every 2 minutes)
                if (DateTime.UtcNow - lastHealthReport > TimeSpan.FromMinutes(2))
                {
                    var connectionState = _webSocket?.State.ToString() ?? "Disconnected";
                    _logger.LogInformation("WebSocket Health Report: State={State}, Symbols={SymbolCount}, Uptime={Uptime}min",
                        connectionState, _symbols.Count, (DateTime.UtcNow - lastHealthReport).TotalMinutes);
                    lastHealthReport = DateTime.UtcNow;
                }

                // Enhanced connection validation every 30 seconds
                if (healthCheckCounter % 6 == 0 && _webSocket?.State == WebSocketState.Open)
                {
                    try
                    {
                        // Send a ping frame to test connection
                        var pingData = Encoding.UTF8.GetBytes("ping");
                        await _webSocket.SendAsync(new ArraySegment<byte>(pingData),
                            WebSocketMessageType.Text, true, stoppingToken);
                        _logger.LogDebug("Sent keepalive ping to Binance WebSocket");
                    }
                    catch (Exception pingEx)
                    {
                        _logger.LogWarning(pingEx, "Failed to send keepalive ping, connection may be stale");
                        // Force reconnection on ping failure
                        await ReconnectAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket service shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket monitoring loop");
                await Task.Delay(10000, stoppingToken); // Longer delay after errors
            }
        }

        _logger.LogInformation("Binance WebSocket service monitoring stopped");
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
        try
        {
            // Merge new symbols into the tracked list (case-insensitive, dedupe)
            var normalized = symbols
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpperInvariant())
                .ToList();

            var set = new HashSet<string>(_symbols.Select(s => s.ToUpperInvariant()));
            foreach (var s in normalized)
            {
                set.Add(s);
            }
            _symbols = set.ToList();

            _logger.LogInformation("Updated subscription symbol set: {Count} symbols", _symbols.Count);

            // Reconnect with the updated streams so Binance starts sending these tickers
            if (_webSocket?.State == WebSocketState.Open)
            {
                _logger.LogInformation("Reconnecting WebSocket to apply new subscriptions...");
                await ReconnectAsync();
            }
            else
            {
                // If not connected yet, ConnectAsync() will use _symbols when called
                _logger.LogDebug("WebSocket not connected yet; symbols will be used on connect");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating symbol subscriptions");
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
        const int maxRetries = 10; // Increased retry attempts for better reliability
        var retryCount = 0;

        while (retryCount < maxRetries && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // Clean up existing connection first
                if (_webSocket?.State == WebSocketState.Open)
                {
                    try
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
                    }
                    catch
                    {
                        // Ignore close errors during reconnect
                    }
                }

                _webSocket?.Dispose();

                await ConnectAsync();
                _ = Task.Run(ListenForMessagesAsync); // Restart listening
                _logger.LogInformation("Successfully reconnected to Binance WebSocket after {RetryCount} attempts", retryCount);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryCount), 60)); // Exponential backoff, max 60 seconds

                _logger.LogWarning(ex, "Reconnection attempt {RetryCount}/{MaxRetries} failed. Retrying in {Delay} seconds",
                    retryCount, maxRetries, delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Reconnection cancelled");
                    return;
                }
            }
        }

        _logger.LogError("Failed to reconnect after {MaxRetries} attempts. WebSocket service may be degraded.", maxRetries);

        // Schedule another reconnection attempt after a longer delay
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), _cancellationTokenSource.Token);
                _logger.LogInformation("Attempting recovery reconnection after extended delay");
                await ReconnectAsync();
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recovery reconnection failed");
            }
        });
    }

    private async Task ListenForMessagesAsync()
    {
        var buffer = new byte[8192]; // Increased buffer size for larger messages
        var messageBuffer = new StringBuilder();

        try
        {
            _logger.LogDebug("Started listening for WebSocket messages");

            while (_webSocket?.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
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

                            // Process message in background to avoid blocking the receive loop
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await ProcessMessageAsync(completeMessage);
                                }
                                catch (Exception msgEx)
                                {
                                    _logger.LogWarning(msgEx, "Error processing individual message, continuing");
                                }
                            });
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket connection closed by server. Close status: {CloseStatus}, Description: {Description}",
                            result.CloseStatus, result.CloseStatusDescription);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        _logger.LogDebug("Received binary message, ignoring");
                    }
                }
                catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    _logger.LogWarning("WebSocket connection closed prematurely, triggering reconnection");
                    break; // Exit loop to trigger reconnection
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogDebug("WebSocket was disposed during receive operation");
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
            _logger.LogError(ex, "Critical error while listening to WebSocket messages");
        }
        finally
        {
            _logger.LogDebug("WebSocket message listening stopped");
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

    private Task ProcessTickerDataAsync(JObject tickerData)
    {
        try
        {
            // Extract ticker data (Binance 24hr ticker format)
            var symbol = tickerData["s"]?.ToString(); // Symbol (e.g., "BTCUSDT")
            var price = decimal.Parse(tickerData["c"]?.ToString() ?? "0"); // Current price
            var priceChange = decimal.Parse(tickerData["P"]?.ToString() ?? "0"); // Price change percentage
            var volume = decimal.Parse(tickerData["v"]?.ToString() ?? "0"); // Volume
            
            if (string.IsNullOrEmpty(symbol)) return Task.CompletedTask;

            _logger.LogDebug("Received ticker data for {Symbol}: Price={Price}, Change={Change}%", 
                symbol, price, priceChange);

            // Fire event for price update
            var update = new PriceUpdateData
            {
                Symbol = symbol,
                Price = price,
                PriceChange = priceChange,
                Volume = volume,
                Timestamp = DateTime.UtcNow
            };

            _latestPrices.AddOrUpdate(symbol, update, (_, _) => update);

            PriceUpdated?.Invoke(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ticker data");
        }

        return Task.CompletedTask;
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
                                .Select(s => ConvertToBinanceFormat(s.Ticker))
                                .Where(s => !string.IsNullOrEmpty(s) && IsValidBinanceSymbol(s))
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

    public PriceUpdateData? GetLatestPrice(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        return _latestPrices.TryGetValue(symbol, out var update)
            ? update
            : null;
    }

    public IReadOnlyCollection<PriceUpdateData> GetAllLatestPrices()
    {
        return _latestPrices.Values.ToList();
    }

    /// <summary>
    /// Converts various ticker formats to Binance format
    /// </summary>
    /// <param name="ticker">Input ticker (e.g., "BTC-USD", "ETH_USDT", "CRYPTO_COMBINED")</param>
    /// <returns>Binance format ticker (e.g., "BTCUSDT") or empty string if invalid</returns>
    private string ConvertToBinanceFormat(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            return string.Empty;

        // Skip invalid symbols
        if (ticker.Contains("COMBINED") || ticker.Contains("_COMBINED"))
            return string.Empty;

        // Convert various formats to Binance format
        var normalized = ticker.ToUpperInvariant()
            .Replace("-USD", "USDT")
            .Replace("_USD", "USDT")
            .Replace("-USDT", "USDT")
            .Replace("_USDT", "USDT")
            .Replace("-", "")
            .Replace("_", "");

        // Ensure it ends with USDT for crypto pairs
        if (!normalized.EndsWith("USDT") && !normalized.EndsWith("BTC") && !normalized.EndsWith("ETH"))
        {
            // If it doesn't end with a known quote currency, assume USDT
            if (!normalized.EndsWith("USDT"))
                normalized += "USDT";
        }

        return normalized;
    }

    /// <summary>
    /// Validates if the symbol is a valid Binance trading pair
    /// </summary>
    /// <param name="symbol">Symbol to validate</param>
    /// <returns>True if valid Binance symbol format</returns>
    private bool IsValidBinanceSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        // Must be alphanumeric only
        if (!System.Text.RegularExpressions.Regex.IsMatch(symbol, @"^[A-Z0-9]+$"))
            return false;

        // Must end with a known quote currency
        return symbol.EndsWith("USDT") || symbol.EndsWith("BTC") || symbol.EndsWith("ETH") ||
               symbol.EndsWith("BNB") || symbol.EndsWith("BUSD");
    }

    public override void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _webSocket?.Dispose();
        _cancellationTokenSource.Dispose();
        base.Dispose();
    }
}
