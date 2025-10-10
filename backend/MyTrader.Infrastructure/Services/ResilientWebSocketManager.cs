using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace MyTrader.Infrastructure.Services;

public interface IResilientWebSocketManager
{
    Task<bool> ConnectAsync(Uri uri, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<bool> SendAsync(string message);
    bool IsConnected { get; }
    WebSocketState State { get; }
    event EventHandler<WebSocketMessageEventArgs> MessageReceived;
    event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
    ConnectionHealthStatus GetHealthStatus();
    Task ForceReconnectAsync();
}

public class WebSocketMessageEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public WebSocketMessageType MessageType { get; set; }
}

public class ConnectionStatusEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public WebSocketState State { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ResilientWebSocketManager : IResilientWebSocketManager, IDisposable
{
    private readonly ILogger<ResilientWebSocketManager> _logger;
    private readonly IDatabaseRetryPolicyService _retryPolicyService;
    
    private ClientWebSocket? _webSocket;
    private Uri? _uri;
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly Timer _heartbeatTimer;
    private readonly Timer _healthCheckTimer;
    private readonly object _timeLock = new();
    
    // Circuit breaker state
    private volatile CircuitBreakerState _circuitState = CircuitBreakerState.Closed;
    private DateTime _circuitOpenedAt = DateTime.MinValue;
    private readonly TimeSpan _circuitOpenTimeout = TimeSpan.FromMinutes(1);
    private readonly int _failureThreshold = 5;
    private readonly int _successThreshold = 3;
    private volatile int _consecutiveFailures = 0;
    private volatile int _successCount = 0;
    
    // Health monitoring
    private DateTime _lastSuccessfulConnection = DateTime.MinValue;
    private DateTime _lastHeartbeat = DateTime.MinValue;
    private DateTime _lastMessageReceived = DateTime.MinValue;
    private readonly DateTime _startTime = DateTime.UtcNow;
    
    // Performance metrics
    private readonly ConcurrentQueue<TimeSpan> _recentConnectionTimes = new();
    private readonly ConcurrentQueue<DateTime> _messageTimestamps = new();
    private readonly ConcurrentDictionary<string, long> _operationCounts = new();

    public event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open && _circuitState != CircuitBreakerState.Open;
    public WebSocketState State => _webSocket?.State ?? WebSocketState.None;

    public ResilientWebSocketManager(
        ILogger<ResilientWebSocketManager> logger,
        IDatabaseRetryPolicyService retryPolicyService)
    {
        _logger = logger;
        _retryPolicyService = retryPolicyService;
        
        // Start heartbeat timer (every 30 seconds)
        _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        // Start health check timer (every 60 seconds)
        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        
        _logger.LogInformation("Resilient WebSocket Manager initialized");
    }

    public async Task<bool> ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (_circuitState == CircuitBreakerState.Open)
        {
            DateTime circuitOpenedAt;
            lock (_timeLock)
            {
                circuitOpenedAt = _circuitOpenedAt;
            }
            
            if (DateTime.UtcNow - circuitOpenedAt < _circuitOpenTimeout)
            {
                _logger.LogWarning("Circuit breaker is open, rejecting connection attempt to {Uri}", uri);
                return false;
            }
            
            // Transition to half-open to test connection
            _circuitState = CircuitBreakerState.HalfOpen;
            _successCount = 0;
            _logger.LogInformation("Circuit breaker transitioning to half-open state for {Uri}", uri);
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            _uri = uri;
            var startTime = DateTime.UtcNow;
            
            // Clean up existing connection
            await DisconnectInternalAsync();
            
            // Create new WebSocket
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            
            _logger.LogInformation("Attempting to connect to WebSocket: {Uri}", uri);
            
            // Connect with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout
            
            await _webSocket.ConnectAsync(uri, timeoutCts.Token);
            
            var connectionTime = DateTime.UtcNow - startTime;
            RecordConnectionTime(connectionTime);
            
            lock (_timeLock)
            {
                _lastSuccessfulConnection = DateTime.UtcNow;
            }
            _consecutiveFailures = 0;
            RecordSuccess();
            
            _logger.LogInformation("Successfully connected to WebSocket {Uri} in {Duration}ms", 
                uri, connectionTime.TotalMilliseconds);
            
            // Start listening for messages
            _ = Task.Run(ListenForMessagesAsync);
            
            OnConnectionStatusChanged(true, _webSocket.State, "Connected", null);
            return true;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            RecordFailure();
            
            _logger.LogError(ex, "Failed to connect to WebSocket {Uri} (attempt {ConsecutiveFailures})", 
                uri, _consecutiveFailures);
            
            OnConnectionStatusChanged(false, WebSocketState.Closed, "Failed", ex.Message);
            return false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await DisconnectInternalAsync();
            OnConnectionStatusChanged(false, WebSocketState.Closed, "Disconnected", null);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<bool> SendAsync(string message)
    {
        if (!IsConnected || _webSocket == null)
        {
            _logger.LogWarning("Cannot send message - WebSocket is not connected");
            return false;
        }

        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), 
                WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            
            IncrementOperationCount("MessagesSent");
            _logger.LogDebug("Sent WebSocket message: {MessageLength} bytes", buffer.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WebSocket message");
            
            // Trigger reconnection on send failure
            _ = Task.Run(async () => await HandleConnectionFailureAsync(ex));
            return false;
        }
    }

    public ConnectionHealthStatus GetHealthStatus()
    {
        DateTime lastSuccessfulConnection, lastHeartbeat, lastMessageReceived;
        lock (_timeLock)
        {
            lastSuccessfulConnection = _lastSuccessfulConnection;
            lastHeartbeat = _lastHeartbeat;
            lastMessageReceived = _lastMessageReceived;
        }

        return new ConnectionHealthStatus
        {
            IsHealthy = IsConnected,
            LastSuccessfulConnection = lastSuccessfulConnection,
            LastHeartbeat = lastHeartbeat,
            ConsecutiveFailures = _consecutiveFailures,
            Uptime = DateTime.UtcNow - _startTime,
            Status = _circuitState switch
            {
                CircuitBreakerState.Open => "CircuitOpen",
                CircuitBreakerState.HalfOpen => "Reconnecting",
                _ => IsConnected ? "Connected" : "Disconnected"
            },
            Metrics = new Dictionary<string, object>
            {
                ["CircuitState"] = _circuitState.ToString(),
                ["WebSocketState"] = State.ToString(),
                ["AverageConnectionTime"] = GetAverageConnectionTime(),
                ["MessagesPerMinute"] = GetMessagesPerMinute(),
                ["LastMessageReceived"] = lastMessageReceived,
                ["OperationCounts"] = _operationCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["Uri"] = _uri?.ToString() ?? "Not set"
            }
        };
    }

    public async Task ForceReconnectAsync()
    {
        _logger.LogInformation("Force reconnection requested");
        
        if (_uri != null)
        {
            await DisconnectAsync();
            await Task.Delay(1000); // Brief delay before reconnection
            await ConnectAsync(_uri);
        }
    }

    private async Task DisconnectInternalAsync()
    {
        try
        {
            _cancellationTokenSource.Cancel();
            
            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                    "Disconnecting", CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during WebSocket disconnection");
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
        }
    }

    private async Task ListenForMessagesAsync()
    {
        var buffer = new byte[8192];
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

                            lock (_timeLock)
                            {
                                _lastMessageReceived = DateTime.UtcNow;
                            }
                            RecordMessageReceived();

                            // Process message in background
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    MessageReceived?.Invoke(this, new WebSocketMessageEventArgs
                                    {
                                        Message = completeMessage,
                                        MessageType = result.MessageType,
                                        Timestamp = DateTime.UtcNow
                                    });
                                }
                                catch (Exception msgEx)
                                {
                                    _logger.LogWarning(msgEx, "Error processing received message");
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
                    _logger.LogWarning("WebSocket connection closed prematurely");
                    break;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("WebSocket listening cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving WebSocket message");
                    await HandleConnectionFailureAsync(ex);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in WebSocket message listener");
            await HandleConnectionFailureAsync(ex);
        }
        finally
        {
            _logger.LogDebug("WebSocket message listening stopped");
        }
    }

    private async Task HandleConnectionFailureAsync(Exception exception)
    {
        _consecutiveFailures++;
        RecordFailure();
        
        _logger.LogWarning(exception, "WebSocket connection failure detected (consecutive failures: {ConsecutiveFailures})", 
            _consecutiveFailures);
        
        OnConnectionStatusChanged(false, _webSocket?.State ?? WebSocketState.Closed, "Failed", exception.Message);
        
        // Attempt automatic reconnection if we have a URI
        if (_uri != null && _circuitState != CircuitBreakerState.Open)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(CalculateReconnectionDelay());
                await ConnectAsync(_uri);
            });
        }
    }

    private TimeSpan CalculateReconnectionDelay()
    {
        // Exponential backoff with jitter
        var baseDelay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, _consecutiveFailures - 1), 60));
        var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 1000));
        return baseDelay.Add(jitter);
    }

    private void SendHeartbeat(object? state)
    {
        if (IsConnected)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendAsync("ping");
                    lock (_timeLock)
                    {
                        _lastHeartbeat = DateTime.UtcNow;
                    }
                    _logger.LogDebug("Sent heartbeat ping");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send heartbeat");
                }
            });
        }
    }

    private void PerformHealthCheck(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var healthStatus = GetHealthStatus();
                
                // Check if connection is stale (no messages received in 5 minutes)
                DateTime lastMessageReceived;
                lock (_timeLock)
                {
                    lastMessageReceived = _lastMessageReceived;
                }
                
                if (IsConnected && DateTime.UtcNow - lastMessageReceived > TimeSpan.FromMinutes(5))
                {
                    _logger.LogWarning("WebSocket connection appears stale - no messages received for 5 minutes");
                    await ForceReconnectAsync();
                }
                
                _logger.LogDebug("WebSocket health check: {Status}, Uptime: {Uptime}", 
                    healthStatus.Status, healthStatus.Uptime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during WebSocket health check");
            }
        });
    }

    private void RecordSuccess()
    {
        if (_circuitState == CircuitBreakerState.HalfOpen)
        {
            _successCount++;
            if (_successCount >= _successThreshold)
            {
                _circuitState = CircuitBreakerState.Closed;
                _logger.LogInformation("WebSocket circuit breaker closed - connection restored");
            }
        }
    }

    private void RecordFailure()
    {
        if (_circuitState == CircuitBreakerState.Closed && _consecutiveFailures >= _failureThreshold)
        {
            _circuitState = CircuitBreakerState.Open;
            lock (_timeLock)
            {
                _circuitOpenedAt = DateTime.UtcNow;
            }
            _logger.LogWarning("WebSocket circuit breaker opened due to {FailureCount} consecutive failures", _consecutiveFailures);
        }
    }

    private void RecordConnectionTime(TimeSpan duration)
    {
        _recentConnectionTimes.Enqueue(duration);
        
        // Keep only last 50 connection times
        while (_recentConnectionTimes.Count > 50)
        {
            _recentConnectionTimes.TryDequeue(out _);
        }
    }

    private void RecordMessageReceived()
    {
        _messageTimestamps.Enqueue(DateTime.UtcNow);
        IncrementOperationCount("MessagesReceived");
        
        // Keep only last 1000 message timestamps
        while (_messageTimestamps.Count > 1000)
        {
            _messageTimestamps.TryDequeue(out _);
        }
    }

    private void IncrementOperationCount(string operationType)
    {
        _operationCounts.AddOrUpdate(operationType, 1, (key, value) => value + 1);
    }

    private double GetAverageConnectionTime()
    {
        var times = _recentConnectionTimes.ToArray();
        return times.Length > 0 ? times.Average(t => t.TotalMilliseconds) : 0;
    }

    private int GetMessagesPerMinute()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-1);
        return _messageTimestamps.Count(t => t > cutoff);
    }

    private void OnConnectionStatusChanged(bool isConnected, WebSocketState state, string status, string? errorMessage)
    {
        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
        {
            IsConnected = isConnected,
            State = state,
            Status = status,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        });
    }

    public void Dispose()
    {
        _heartbeatTimer?.Dispose();
        _healthCheckTimer?.Dispose();
        _cancellationTokenSource?.Cancel();
        _webSocket?.Dispose();
        _connectionSemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}