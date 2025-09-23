using Microsoft.AspNetCore.SignalR;
using MyTrader.Core.Enums;
using MyTrader.Core.Models;
using MyTrader.Services.Market;

namespace MyTrader.Api.Hubs;

/// <summary>
/// Enhanced SignalR Hub for multi-asset real-time market data distribution
/// Supports crypto, stocks (BIST/NASDAQ), and future asset classes
/// </summary>
public class MarketDataHub : Hub
{
    private readonly ILogger<MarketDataHub> _logger;
    private readonly IBinanceWebSocketService? _binanceService;

    public MarketDataHub(ILogger<MarketDataHub> logger, IBinanceWebSocketService? binanceService = null)
    {
        _logger = logger;
        _binanceService = binanceService;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to MarketDataHub: {ConnectionId}", Context.ConnectionId);

        await Clients.Caller.SendAsync("ConnectionStatus", new
        {
            status = "connected",
            message = "Connected to multi-asset real-time market data",
            timestamp = DateTime.UtcNow,
            supportedAssetClasses = Enum.GetNames<AssetClassCode>()
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from MarketDataHub: {ConnectionId}, Exception: {Exception}",
            Context.ConnectionId, exception?.Message);
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
            if (!Enum.TryParse<AssetClassCode>(assetClass, true, out var parsedAssetClass))
            {
                _logger.LogWarning("Invalid asset class '{AssetClass}' from client {ConnectionId}",
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
            if (!symbols.Any())
            {
                _logger.LogWarning("No valid symbols provided from client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("SubscriptionError", new
                {
                    error = "NoSymbols",
                    message = "No valid symbols provided for subscription"
                });
                return;
            }

            _logger.LogInformation("Client {ConnectionId} subscribing to {AssetClass} symbols: {Symbols}",
                Context.ConnectionId, parsedAssetClass, string.Join(", ", symbols));

            // Add client to asset class group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"AssetClass_{parsedAssetClass}");

            // Add client to individual symbol groups within the asset class
            foreach (var symbol in symbols)
            {
                var groupName = $"{parsedAssetClass}_{symbol}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
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
            _logger.LogError(ex, "Error processing subscription request from client {ConnectionId}", Context.ConnectionId);
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
                _logger.LogWarning("Invalid asset class '{AssetClass}' from client {ConnectionId}",
                    assetClass, Context.ConnectionId);
                return;
            }

            _logger.LogInformation("Client {ConnectionId} subscribing to all {AssetClass} symbols",
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
            _logger.LogError(ex, "Error processing asset class subscription from client {ConnectionId}", Context.ConnectionId);
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
                _logger.LogWarning("Invalid asset class '{AssetClass}' from client {ConnectionId}",
                    assetClass, Context.ConnectionId);
                return;
            }

            var symbols = ParseSymbolData(symbolData);

            _logger.LogInformation("Client {ConnectionId} unsubscribing from {AssetClass} symbols: {Symbols}",
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
            _logger.LogError(ex, "Error processing unsubscription request from client {ConnectionId}", Context.ConnectionId);
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
            _logger.LogInformation("Client {ConnectionId} subscribing to market status for: {Markets}",
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
            _logger.LogError(ex, "Error processing market status subscription from client {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Get current market status for all tracked markets
    /// </summary>
    public async Task GetMarketStatus()
    {
        try
        {
            var marketStatuses = new List<object>();

            // Add crypto market (24/7)
            marketStatuses.Add(new
            {
                market = "CRYPTO",
                status = Core.Enums.MarketStatus.OPEN.ToString(),
                nextOpen = (DateTime?)null,
                nextClose = (DateTime?)null,
                timezone = "UTC"
            });

            // TODO: Add real market status calculation for BIST and NASDAQ
            // This would be implemented by the MarketStatusMonitoringService

            await Clients.Caller.SendAsync("CurrentMarketStatus", new
            {
                markets = marketStatuses,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving market status for client {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility with existing clients
    /// </summary>
    /// <param name="symbolData">Symbols to subscribe to (assumes CRYPTO asset class)</param>
    public async Task SubscribeToCrypto(object symbolData)
    {
        _logger.LogInformation("Client {ConnectionId} using legacy subscription method, defaulting to CRYPTO",
            Context.ConnectionId);

        await SubscribeToPriceUpdates("CRYPTO", symbolData);
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    /// <param name="symbols">Symbols to unsubscribe from (assumes CRYPTO asset class)</param>
    public async Task UnsubscribeFromCrypto(List<string> symbols)
    {
        _logger.LogInformation("Client {ConnectionId} using legacy unsubscription method, defaulting to CRYPTO",
            Context.ConnectionId);

        await UnsubscribeFromPriceUpdates("CRYPTO", symbols);
    }

    private List<string> ParseSymbolData(object symbolData)
    {
        return symbolData switch
        {
            string singleSymbol => new List<string> { singleSymbol },
            List<string> symbolList => symbolList,
            string[] symbolArray => symbolArray.ToList(),
            IEnumerable<string> symbolEnumerable => symbolEnumerable.ToList(),
            _ => new List<string>()
        };
    }
}