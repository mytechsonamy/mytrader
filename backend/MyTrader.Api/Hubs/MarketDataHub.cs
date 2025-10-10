using Microsoft.AspNetCore.SignalR;
using MyTrader.Core.Enums;
using MyTrader.Core.Models;
using MyTrader.Services.Market;
// using MyTrader.Infrastructure.Monitoring;

namespace MyTrader.Api.Hubs;

/// <summary>
/// Enhanced SignalR Hub for multi-asset real-time market data distribution
/// Supports crypto, stocks (BIST/NASDAQ), and future asset classes
/// </summary>
public class MarketDataHub : BaseHub
{
    private readonly IBinanceWebSocketService? _binanceService;
    private readonly MyTrader.Core.Interfaces.IMarketDataRouter? _marketDataRouter;
    private readonly MyTrader.Core.Interfaces.IMarketStatusService? _marketStatusService;
    // private readonly PrometheusMetricsExporter? _metricsExporter;
    
    protected override string HubName => "MarketData";

    public MarketDataHub(
        ILogger<MarketDataHub> logger, 
        IBinanceWebSocketService? binanceService = null,
        MyTrader.Core.Interfaces.IMarketDataRouter? marketDataRouter = null,
        MyTrader.Core.Interfaces.IMarketStatusService? marketStatusService = null,
        MyTrader.Core.Interfaces.IHubCoordinationService? hubCoordination = null)
        : base(logger, hubCoordination)
    {
        _binanceService = binanceService;
        _marketDataRouter = marketDataRouter;
        _marketStatusService = marketStatusService;
        // _metricsExporter = metricsExporter;
    }

    public override async Task OnConnectedAsync()
    {
        Logger.LogInformation("Client connected to MarketDataHub: {ConnectionId}", Context.ConnectionId);

        // Register connection with coordination service
        if (HubCoordination != null)
        {
            await HubCoordination.RegisterConnectionAsync(HubName, Context.ConnectionId);
        }

        // Record connection metrics
        // _metricsExporter?.RecordSignalRConnection("MarketData", true);

        try
        {
            await Clients.Caller.SendAsync("ConnectionStatus", new
            {
                status = "connected",
                message = "Connected to multi-asset real-time market data",
                timestamp = DateTime.UtcNow,
                connectionId = Context.ConnectionId,
                supportedAssetClasses = Enum.GetNames<AssetClassCode>()
            });

            // Send immediate heartbeat to verify connection is working
            await Clients.Caller.SendAsync("Heartbeat", new
            {
                timestamp = DateTime.UtcNow,
                connectionId = Context.ConnectionId,
                status = "alive"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending connection status to client {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Logger.LogInformation("Client disconnected from MarketDataHub: {ConnectionId}, Exception: {Exception}",
            Context.ConnectionId, exception?.Message);

        // Unregister connection with coordination service (automatic cleanup)
        if (HubCoordination != null)
        {
            await HubCoordination.UnregisterConnectionAsync(HubName, Context.ConnectionId);
        }

        // Record disconnection metrics
        // _metricsExporter?.RecordSignalRConnection("MarketData", false);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to price updates for specific symbols within an asset class
    /// </summary>
    /// <param name="assetClass">The asset class (CRYPTO, STOCK_BIST, STOCK_NASDAQ)</param>
    /// <param name="symbolData">Single symbol string, array of symbols, or List of symbols</param>
    public async Task SubscribeToPriceUpdates(string assetClass, object symbolData)
    {
        try
        {
            // DEBUG: Log the incoming symbol data type and value
            Logger.LogWarning("SubscribeToPriceUpdates called with assetClass={AssetClass}, symbolData type={SymbolDataType}, value={SymbolDataValue}",
                assetClass, symbolData?.GetType().FullName ?? "null", symbolData);

            if (!Enum.TryParse<AssetClassCode>(assetClass, true, out var parsedAssetClass))
            {
                Logger.LogWarning("Invalid asset class '{AssetClass}' from client {ConnectionId}",
                    assetClass, Context.ConnectionId);
                await Clients.Caller.SendAsync("SubscriptionError", new
                {
                    error = "InvalidAssetClass",
                    message = $"Asset class '{assetClass}' is not supported",
                    supportedClasses = Enum.GetNames<AssetClassCode>()
                });
                return;
            }

            var symbols = ParseSymbolData(symbolData);
            Logger.LogWarning("Parsed {SymbolCount} symbols from symbolData: {Symbols}", symbols.Count, string.Join(", ", symbols));
            
            if (!symbols.Any())
            {
                Logger.LogWarning(
                    "No valid symbols after parsing. AssetClass: {AssetClass}, SymbolDataType: {Type}, RawData: {Data}",
                    assetClass,
                    symbolData?.GetType().FullName ?? "null",
                    System.Text.Json.JsonSerializer.Serialize(symbolData)
                );

                await Clients.Caller.SendAsync("SubscriptionError", new
                {
                    error = "NoSymbols",
                    message = $"No valid symbols provided for subscription. Received type: {symbolData?.GetType().Name ?? "null"}",
                    receivedData = symbolData
                });
                return;
            }

            Logger.LogInformation("Client {ConnectionId} subscribing to {AssetClass} symbols: {Symbols}",
                Context.ConnectionId, parsedAssetClass, string.Join(", ", symbols));

            // Add client to asset class group with automatic tracking
            var assetClassGroup = $"AssetClass_{parsedAssetClass}";
            await AddToTrackedGroupAsync(assetClassGroup);

            // Add client to individual symbol groups within the asset class
            foreach (var symbol in symbols)
            {
                var groupName = $"{parsedAssetClass}_{symbol}";
                await AddToTrackedGroupAsync(groupName);
            }

            await Clients.Caller.SendAsync("SubscriptionConfirmed", new
            {
                assetClass = parsedAssetClass.ToString(),
                symbols = symbols,
                timestamp = DateTime.UtcNow
            });

            // For crypto symbols, ensure they are subscribed via Binance service
            if (parsedAssetClass == AssetClassCode.CRYPTO && _binanceService != null)
            {
                await _binanceService.SubscribeToSymbolsAsync(symbols);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing subscription request from client {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("SubscriptionError", new
            {
                error = "InternalError",
                message = "An error occurred while processing your subscription"
            });
        }
    }

    /// <summary>
    /// Subscribe to all available symbols for a specific asset class
    /// </summary>
    /// <param name="assetClass">The asset class to subscribe to</param>
    public async Task SubscribeToAssetClass(string assetClass)
    {
        try
        {
            if (!Enum.TryParse<AssetClassCode>(assetClass, true, out var parsedAssetClass))
            {
                Logger.LogWarning("Invalid asset class '{AssetClass}' from client {ConnectionId}",
                    assetClass, Context.ConnectionId);
                return;
            }

            Logger.LogInformation("Client {ConnectionId} subscribing to all {AssetClass} symbols",
                Context.ConnectionId, parsedAssetClass);

            // Add client to asset class group for bulk updates
            await Groups.AddToGroupAsync(Context.ConnectionId, $"AssetClass_{parsedAssetClass}");

            await Clients.Caller.SendAsync("AssetClassSubscriptionConfirmed", new
            {
                assetClass = parsedAssetClass.ToString(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing asset class subscription from client {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Unsubscribe from specific symbols within an asset class
    /// </summary>
    /// <param name="assetClass">The asset class</param>
    /// <param name="symbolData">Symbols to unsubscribe from</param>
    public async Task UnsubscribeFromPriceUpdates(string assetClass, object symbolData)
    {
        try
        {
            if (!Enum.TryParse<AssetClassCode>(assetClass, true, out var parsedAssetClass))
            {
                Logger.LogWarning("Invalid asset class '{AssetClass}' from client {ConnectionId}",
                    assetClass, Context.ConnectionId);
                return;
            }

            var symbols = ParseSymbolData(symbolData);

            Logger.LogInformation("Client {ConnectionId} unsubscribing from {AssetClass} symbols: {Symbols}",
                Context.ConnectionId, parsedAssetClass, string.Join(", ", symbols));

            // Remove client from individual symbol groups
            foreach (var symbol in symbols)
            {
                var groupName = $"{parsedAssetClass}_{symbol}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new
            {
                assetClass = parsedAssetClass.ToString(),
                symbols = symbols,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing unsubscription request from client {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Subscribe to market status updates for specific markets
    /// </summary>
    /// <param name="markets">Array of market names (e.g., ["NASDAQ", "BIST", "BINANCE"])</param>
    public async Task SubscribeToMarketStatus(string[] markets)
    {
        try
        {
            Logger.LogInformation("Client {ConnectionId} subscribing to market status for: {Markets}",
                Context.ConnectionId, string.Join(", ", markets));

            foreach (var market in markets)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"MarketStatus_{market}");
            }

            await Clients.Caller.SendAsync("MarketStatusSubscriptionConfirmed", new
            {
                markets = markets,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing market status subscription from client {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Get current market status for all tracked markets
    /// </summary>
    public async Task GetMarketStatus()
    {
        try
        {
            if (_marketStatusService == null)
            {
                Logger.LogWarning("MarketStatusService not available for client {ConnectionId}", Context.ConnectionId);
                
                // Fallback to basic crypto status
                await Clients.Caller.SendAsync("CurrentMarketStatus", new
                {
                    markets = new[]
                    {
                        new
                        {
                            market = "BINANCE",
                            status = "OPEN",
                            isOpen = true,
                            nextOpen = (DateTime?)null,
                            nextClose = (DateTime?)null,
                            timezone = "UTC"
                        }
                    },
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            var allStatuses = await _marketStatusService.GetAllMarketStatusesAsync();
            var marketStatuses = allStatuses.Select(status => new
            {
                market = status.Code,
                status = status.Status,
                isOpen = status.IsOpen,
                nextOpen = status.NextOpen,
                nextClose = status.NextClose,
                timezone = status.Timezone
            }).ToList();

            await Clients.Caller.SendAsync("CurrentMarketStatus", new
            {
                markets = marketStatuses,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving market status for client {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility with existing clients
    /// </summary>
    /// <param name="symbolData">Symbols to subscribe to (assumes CRYPTO asset class)</param>
    public async Task SubscribeToCrypto(object symbolData)
    {
        Logger.LogInformation("Client {ConnectionId} using legacy subscription method, defaulting to CRYPTO",
            Context.ConnectionId);

        await SubscribeToPriceUpdates("CRYPTO", symbolData);
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    /// <param name="symbols">Symbols to unsubscribe from (assumes CRYPTO asset class)</param>
    public async Task UnsubscribeFromCrypto(List<string> symbols)
    {
        Logger.LogInformation("Client {ConnectionId} using legacy unsubscription method, defaulting to CRYPTO",
            Context.ConnectionId);

        await UnsubscribeFromPriceUpdates("CRYPTO", symbols);
    }

    /// <summary>
    /// Force connection test - useful for debugging connection issues
    /// </summary>
    public async Task TestConnection()
    {
        try
        {
            Logger.LogInformation("Connection test requested by client {ConnectionId}", Context.ConnectionId);

            await Clients.Caller.SendAsync("ConnectionTest", new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                connectionId = Context.ConnectionId,
                message = "Connection is working properly",
                serverTime = DateTime.UtcNow.ToString("O")
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Connection test failed for client {ConnectionId}", Context.ConnectionId);
            // Try to send error response
            try
            {
                await Clients.Caller.SendAsync("ConnectionTest", new
                {
                    success = false,
                    timestamp = DateTime.UtcNow,
                    connectionId = Context.ConnectionId,
                    message = "Connection test failed",
                    error = ex.Message
                });
            }
            catch
            {
                // If even error response fails, just log it
                Logger.LogError("Failed to send connection test error response to {ConnectionId}", Context.ConnectionId);
            }
        }
    }

    /// <summary>
    /// Subscribe to specific market updates (e.g., NASDAQ, BIST, BINANCE)
    /// </summary>
    /// <param name="market">Market identifier</param>
    public async Task SubscribeToMarket(string market)
    {
        try
        {
            if (_marketDataRouter == null)
            {
                Logger.LogWarning("MarketDataRouter not available for client {ConnectionId}", Context.ConnectionId);
                return;
            }

            Logger.LogInformation("Client {ConnectionId} subscribing to market: {Market}",
                Context.ConnectionId, market);

            var groupName = $"Market_{market.ToUpperInvariant()}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            await Clients.Caller.SendAsync("MarketSubscriptionConfirmed", new
            {
                market = market,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to market {Market} for client {ConnectionId}", 
                market, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Unsubscribe from specific market updates
    /// </summary>
    /// <param name="market">Market identifier</param>
    public async Task UnsubscribeFromMarket(string market)
    {
        try
        {
            Logger.LogInformation("Client {ConnectionId} unsubscribing from market: {Market}",
                Context.ConnectionId, market);

            var groupName = $"Market_{market.ToUpperInvariant()}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            await Clients.Caller.SendAsync("MarketUnsubscriptionConfirmed", new
            {
                market = market,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from market {Market} for client {ConnectionId}", 
                market, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Subscribe to all market data updates
    /// </summary>
    public async Task SubscribeToAllMarkets()
    {
        try
        {
            Logger.LogInformation("Client {ConnectionId} subscribing to all markets", Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, "MarketData_All");

            await Clients.Caller.SendAsync("AllMarketsSubscriptionConfirmed", new
            {
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to all markets for client {ConnectionId}", Context.ConnectionId);
        }
    }

    private List<string> ParseSymbolData(object? symbolData)
    {
        // Add comprehensive logging to diagnose parsing issues
        Logger.LogInformation(
            "ParseSymbolData - Type: {TypeName}, Value: {Value}",
            symbolData?.GetType().FullName ?? "null",
            System.Text.Json.JsonSerializer.Serialize(symbolData)
        );

        return symbolData switch
        {
            null => new List<string>(),
            string str when !string.IsNullOrWhiteSpace(str) => new List<string> { str },
            string[] strArray => strArray.Where(s => !string.IsNullOrEmpty(s)).ToList(),
            List<string> list => list.Where(s => !string.IsNullOrEmpty(s)).ToList(),
            IEnumerable<string> symbolEnumerable => symbolEnumerable.ToList(),
            // Handle JSON.NET JArray (from SignalR JSON deserialization)
            Newtonsoft.Json.Linq.JArray jArray => jArray.Select(t => t.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList(),
            // Handle object[] - JavaScript SignalR clients send arrays as object[]
            object[] objArray => objArray.Select(o => o?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList(),
            System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array =>
                jsonElement.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList(),
            _ => new List<string>()
        };
    }
}