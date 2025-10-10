using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.DTOs;
using MyTrader.Core.Models;
using MyTrader.Core.Services;
using MyTrader.Core.Enums;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Interface for Alpaca WebSocket streaming service
/// </summary>
public interface IAlpacaStreamingService
{
    Task StartAsync();
    Task StopAsync();
    Task SubscribeToSymbolsAsync(List<string> symbols);
    event Action<StockPriceData>? StockPriceUpdated;
    Task<AlpacaConnectionHealth> GetHealthStatusAsync();
    Task ForceReconnectAsync();
}

/// <summary>
/// Alpaca connection health status
/// </summary>
public class AlpacaConnectionHealth
{
    public bool IsConnected { get; set; }
    public bool IsAuthenticated { get; set; }
    public int SubscribedSymbols { get; set; }
    public DateTime? LastMessageReceived { get; set; }
    public int MessagesPerMinute { get; set; }
    public TimeSpan? ConnectionUptime { get; set; }
    public int ConsecutiveFailures { get; set; }
    public string? LastError { get; set; }
    public WebSocketState State { get; set; }
}

/// <summary>
/// Alpaca WebSocket streaming service for real-time stock market data
/// </summary>
public class AlpacaStreamingService : BackgroundService, IAlpacaStreamingService
{
    private readonly ILogger<AlpacaStreamingService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AlpacaConfiguration> _config;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource _cancellationTokenSource = new();
    private List<string> _symbols = new();
    private readonly ConcurrentDictionary<string, decimal> _previousCloseCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes = new();

    // Connection state
    private bool _isAuthenticated;
    private DateTime? _connectionStartTime;
    private DateTime? _lastMessageReceived;
    private int _messagesReceivedCount;
    private int _consecutiveFailures;
    private string? _lastError;

    public event Action<StockPriceData>? StockPriceUpdated;

    public AlpacaStreamingService(
        ILogger<AlpacaStreamingService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<AlpacaConfiguration> config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Value.Streaming.Enabled)
        {
            _logger.LogInformation("Alpaca streaming is disabled in configuration");
            return;
        }

        _logger.LogInformation("Starting Alpaca WebSocket service");

        await LoadSymbolsFromDatabaseAsync();
        await StartAsync();

        // Keep the service running with health monitoring
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);

                // Check connection health
                if (_webSocket?.State != WebSocketState.Open)
                {
                    _logger.LogWarning("WebSocket connection lost (State: {State}), attempting to reconnect...",
                        _webSocket?.State.ToString() ?? "null");
                    await ReconnectAsync();
                }

                // Check for stale data
                if (_lastMessageReceived.HasValue &&
                    DateTime.UtcNow - _lastMessageReceived.Value > TimeSpan.FromSeconds(_config.Value.Streaming.MessageTimeoutSeconds))
                {
                    _logger.LogWarning("No messages received for {Seconds} seconds, connection may be stale",
                        _config.Value.Streaming.MessageTimeoutSeconds);
                    await ReconnectAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Alpaca WebSocket service shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket monitoring loop");
                await Task.Delay(10000, stoppingToken);
            }
        }

        _logger.LogInformation("Alpaca WebSocket service monitoring stopped");
    }

    public async Task StartAsync()
    {
        try
        {
            await ConnectAsync();
            await AuthenticateAsync();
            await SubscribeToSymbolsAsync(_symbols);

            _logger.LogInformation("Alpaca WebSocket service started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Alpaca WebSocket service");
            _consecutiveFailures++;
            _lastError = ex.Message;
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            _cancellationTokenSource.Cancel();

            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service stopping", CancellationToken.None);
            }

            _webSocket?.Dispose();
            _logger.LogInformation("Alpaca WebSocket service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Alpaca WebSocket service");
        }
    }

    public async Task SubscribeToSymbolsAsync(List<string> symbols)
    {
        try
        {
            var normalized = symbols
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpperInvariant())
                .Distinct()
                .Take(_config.Value.Streaming.MaxSymbols)
                .ToList();

            _symbols = normalized;
            _logger.LogInformation("Updated subscription symbol set: {Count} symbols", _symbols.Count);

            if (_isAuthenticated && _webSocket?.State == WebSocketState.Open)
            {
                await SendSubscriptionMessageAsync();
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

            var uri = new Uri(_config.Value.Streaming.WebSocketUrl);
            _logger.LogInformation("Connecting to Alpaca WebSocket: {Uri}", uri);

            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

            if (_webSocket.State == WebSocketState.Open)
            {
                _logger.LogInformation("Successfully connected to Alpaca WebSocket");
                _connectionStartTime = DateTime.UtcNow;
                _consecutiveFailures = 0;

                // Start receiving messages
                _ = Task.Run(() => ReceiveMessagesAsync());
            }
            else
            {
                throw new InvalidOperationException("Failed to establish WebSocket connection");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Alpaca WebSocket");
            _consecutiveFailures++;
            _lastError = ex.Message;
            throw;
        }
    }

    private async Task AuthenticateAsync()
    {
        try
        {
            var authMessage = new
            {
                action = "auth",
                key = _config.Value.Streaming.ApiKey,
                secret = _config.Value.Streaming.ApiSecret
            };

            var json = JsonSerializer.Serialize(authMessage);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _webSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);

            _logger.LogInformation("Sent authentication message to Alpaca");

            // Wait for auth confirmation (will be processed in ReceiveMessagesAsync)
            await Task.Delay(TimeSpan.FromSeconds(_config.Value.Streaming.AuthTimeoutSeconds));

            if (!_isAuthenticated)
            {
                throw new InvalidOperationException("Authentication timeout - no confirmation received");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed");
            _consecutiveFailures++;
            _lastError = ex.Message;
            throw;
        }
    }

    private async Task SendSubscriptionMessageAsync()
    {
        try
        {
            var subscriptionMessage = new Dictionary<string, object>
            {
                ["action"] = "subscribe"
            };

            if (_config.Value.Streaming.SubscribeToTrades)
            {
                subscriptionMessage["trades"] = _symbols;
            }

            if (_config.Value.Streaming.SubscribeToQuotes)
            {
                subscriptionMessage["quotes"] = _symbols;
            }

            if (_config.Value.Streaming.SubscribeToBars)
            {
                subscriptionMessage["bars"] = _symbols;
            }

            var json = JsonSerializer.Serialize(subscriptionMessage);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _webSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);

            _logger.LogInformation("Sent subscription request for {Count} symbols", _symbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription message");
            throw;
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[1024 * 16]; // 16KB buffer
        var messageBuilder = new StringBuilder();

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket close message received: {Status} {Description}",
                        result.CloseStatus, result.CloseStatusDescription);
                    break;
                }

                var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(chunk);

                if (result.EndOfMessage)
                {
                    var message = messageBuilder.ToString();
                    messageBuilder.Clear();

                    _lastMessageReceived = DateTime.UtcNow;
                    _messagesReceivedCount++;

                    await ProcessMessageAsync(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message receiving canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving messages");
            _consecutiveFailures++;
            _lastError = ex.Message;
        }
    }

    private async Task ProcessMessageAsync(string message)
    {
        try
        {
            if (_config.Value.Streaming.EnableDetailedLogging)
            {
                _logger.LogDebug("Received message: {Message}", message.Length > 500 ? message.Substring(0, 500) + "..." : message);
            }

            // Alpaca sends messages as JSON arrays
            var messages = JsonSerializer.Deserialize<List<JsonElement>>(message);

            if (messages == null) return;

            foreach (var msgElement in messages)
            {
                var msgType = msgElement.GetProperty("T").GetString();

                switch (msgType)
                {
                    case "success":
                        var successMsg = JsonSerializer.Deserialize<AlpacaAuthSuccessMessage>(msgElement.GetRawText());
                        _logger.LogInformation("Alpaca authentication successful: {Message}", successMsg?.Message);
                        _isAuthenticated = true;
                        break;

                    case "error":
                        var errorMsg = JsonSerializer.Deserialize<AlpacaErrorMessage>(msgElement.GetRawText());
                        _logger.LogError("Alpaca error: Code={Code}, Message={Message}", errorMsg?.Code, errorMsg?.Message);
                        _lastError = errorMsg?.Message;
                        break;

                    case "subscription":
                        var subMsg = JsonSerializer.Deserialize<AlpacaSubscriptionMessage>(msgElement.GetRawText());
                        _logger.LogInformation("Subscription confirmed - Trades: {Trades}, Quotes: {Quotes}, Bars: {Bars}",
                            subMsg?.Trades.Count, subMsg?.Quotes.Count, subMsg?.Bars.Count);
                        break;

                    case "t":
                        var trade = JsonSerializer.Deserialize<AlpacaTradeMessage>(msgElement.GetRawText());
                        if (trade != null) ProcessTradeMessage(trade);
                        break;

                    case "q":
                        var quote = JsonSerializer.Deserialize<AlpacaQuoteMessage>(msgElement.GetRawText());
                        if (quote != null) ProcessQuoteMessage(quote);
                        break;

                    case "b":
                        var bar = JsonSerializer.Deserialize<AlpacaBarMessage>(msgElement.GetRawText());
                        if (bar != null) ProcessBarMessage(bar);
                        break;

                    default:
                        _logger.LogDebug("Unknown message type: {Type}", msgType);
                        break;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse WebSocket message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket message");
        }
    }

    private void ProcessTradeMessage(AlpacaTradeMessage trade)
    {
        try
        {
            var previousClose = _previousCloseCache.GetValueOrDefault(trade.S, trade.P);
            var priceChange = trade.P - previousClose;

            // ✅ FIXED: Use previousClose as denominator (standard financial formula)
            var priceChangePercent = previousClose > 0 ? (priceChange / previousClose) * 100 : 0;

            var stockData = new StockPriceData
            {
                Symbol = trade.S,
                AssetClass = AssetClassCode.STOCK,
                Market = MapExchangeToMarket(trade.X),
                Price = trade.P,
                PreviousClose = previousClose,
                PriceChange = priceChange,
                PriceChangePercent = priceChangePercent,
                Volume = trade.S_Size,
                Timestamp = trade.GetTimestamp(),
                Source = "ALPACA",
                QualityScore = 100
            };

            _lastUpdateTimes[trade.S] = stockData.Timestamp;

            StockPriceUpdated?.Invoke(stockData);

            if (_config.Value.Streaming.EnableDetailedLogging)
            {
                _logger.LogDebug("Processed trade: {Symbol} @ ${Price}", trade.S, trade.P);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing trade message for {Symbol}", trade.S);
        }
    }

    private void ProcessQuoteMessage(AlpacaQuoteMessage quote)
    {
        try
        {
            var midPrice = (quote.BP + quote.AP) / 2;
            var previousClose = _previousCloseCache.GetValueOrDefault(quote.S, midPrice);
            var priceChange = midPrice - previousClose;

            // ✅ FIXED: Use previousClose as denominator (standard financial formula)
            var priceChangePercent = previousClose > 0 ? (priceChange / previousClose) * 100 : 0;

            var stockData = new StockPriceData
            {
                Symbol = quote.S,
                AssetClass = AssetClassCode.STOCK,
                Market = "NASDAQ", // Inferred from context
                Price = midPrice,
                PreviousClose = previousClose,
                PriceChange = priceChange,
                PriceChangePercent = priceChangePercent,
                BidPrice = quote.BP,
                AskPrice = quote.AP,
                Volume = 0, // Quotes don't have volume
                Timestamp = quote.GetTimestamp(),
                Source = "ALPACA",
                QualityScore = 100
            };

            _lastUpdateTimes[quote.S] = stockData.Timestamp;

            StockPriceUpdated?.Invoke(stockData);

            if (_config.Value.Streaming.EnableDetailedLogging)
            {
                _logger.LogDebug("Processed quote: {Symbol} Bid=${Bid} Ask=${Ask}", quote.S, quote.BP, quote.AP);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing quote message for {Symbol}", quote.S);
        }
    }

    private void ProcessBarMessage(AlpacaBarMessage bar)
    {
        try
        {
            var previousClose = _previousCloseCache.GetValueOrDefault(bar.S, bar.O);
            var priceChange = bar.C - previousClose;
            // ✅ FIXED: Use previousClose as denominator (standard financial formula)
            var priceChangePercent = previousClose > 0 ? (priceChange / previousClose) * 100 : 0;

            var stockData = new StockPriceData
            {
                Symbol = bar.S,
                AssetClass = AssetClassCode.STOCK,
                Market = "NASDAQ",
                Price = bar.C,
                PreviousClose = previousClose,
                PriceChange = priceChange,
                PriceChangePercent = priceChangePercent,
                OpenPrice = bar.O,
                HighPrice = bar.H,
                LowPrice = bar.L,
                Volume = bar.V,
                TradeCount = bar.N,
                Timestamp = bar.GetTimestamp(),
                Source = "ALPACA",
                QualityScore = 100
            };

            _lastUpdateTimes[bar.S] = stockData.Timestamp;

            StockPriceUpdated?.Invoke(stockData);

            if (_config.Value.Streaming.EnableDetailedLogging)
            {
                _logger.LogDebug("Processed bar: {Symbol} O=${Open} H=${High} L=${Low} C=${Close} V={Volume}",
                    bar.S, bar.O, bar.H, bar.L, bar.C, bar.V);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bar message for {Symbol}", bar.S);
        }
    }

    private string MapExchangeToMarket(string exchangeCode)
    {
        return exchangeCode switch
        {
            "V" => "NASDAQ",
            "Q" => "NASDAQ",
            "P" => "NYSE",
            "N" => "NYSE",
            "Z" => "BATS",
            "J" => "EDGA",
            "K" => "EDGX",
            _ => "NYSE" // Default
        };
    }

    private async Task ReconnectAsync()
    {
        _logger.LogInformation("Initiating Alpaca WebSocket reconnection");

        try
        {
            await StopAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(_config.Value.Streaming.ReconnectBaseDelayMs));
            await StartAsync();

            _logger.LogInformation("Successfully reconnected to Alpaca WebSocket");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect");
            _consecutiveFailures++;
            _lastError = ex.Message;

            // Exponential backoff
            var delay = Math.Min(
                _config.Value.Streaming.ReconnectBaseDelayMs * Math.Pow(2, _consecutiveFailures),
                _config.Value.Streaming.ReconnectMaxDelayMs);

            await Task.Delay(TimeSpan.FromMilliseconds(delay));
        }
    }

    private async Task LoadSymbolsFromDatabaseAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var symbolManagementService = scope.ServiceProvider.GetService<ISymbolManagementService>();

            if (symbolManagementService != null)
            {
                var symbolEntities = await symbolManagementService.GetActiveSymbolsForBroadcastAsync("STOCK", "NASDAQ,NYSE");

                _symbols = symbolEntities
                    .Select(s => s.Ticker.ToUpperInvariant())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .Take(_config.Value.Streaming.MaxSymbols)
                    .ToList();

                _logger.LogInformation("Loaded {Count} stock symbols from database: {Symbols}",
                    _symbols.Count, string.Join(", ", _symbols.Take(10)));
            }
            else
            {
                // Fallback to default symbols
                _symbols = new List<string> { "AAPL", "GOOGL", "MSFT", "TSLA", "AMZN" };
                _logger.LogWarning("SymbolManagementService not available, using fallback symbols: {Symbols}",
                    string.Join(", ", _symbols));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading symbols from database, using fallback");
            _symbols = new List<string> { "AAPL", "GOOGL", "MSFT" };
        }
    }

    public async Task<AlpacaConnectionHealth> GetHealthStatusAsync()
    {
        return new AlpacaConnectionHealth
        {
            IsConnected = _webSocket?.State == WebSocketState.Open,
            IsAuthenticated = _isAuthenticated,
            SubscribedSymbols = _symbols.Count,
            LastMessageReceived = _lastMessageReceived,
            MessagesPerMinute = CalculateMessagesPerMinute(),
            ConnectionUptime = _connectionStartTime.HasValue ? DateTime.UtcNow - _connectionStartTime.Value : null,
            ConsecutiveFailures = _consecutiveFailures,
            LastError = _lastError,
            State = _webSocket?.State ?? WebSocketState.None
        };
    }

    private int CalculateMessagesPerMinute()
    {
        if (!_connectionStartTime.HasValue) return 0;

        var elapsed = DateTime.UtcNow - _connectionStartTime.Value;
        if (elapsed.TotalMinutes == 0) return 0;

        return (int)(_messagesReceivedCount / elapsed.TotalMinutes);
    }

    public async Task ForceReconnectAsync()
    {
        _logger.LogInformation("Force reconnection requested");
        await ReconnectAsync();
    }

    public override void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _webSocket?.Dispose();
        _cancellationTokenSource.Dispose();
        base.Dispose();
    }
}
