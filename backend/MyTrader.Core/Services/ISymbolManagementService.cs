using MyTrader.Core.Models;

namespace MyTrader.Core.Services;

/// <summary>
/// Service for managing symbols dynamically from the database.
/// Provides caching and optimized queries for symbol retrieval and broadcast control.
/// </summary>
public interface ISymbolManagementService
{
    /// <summary>
    /// Get active symbols for WebSocket broadcasting ordered by priority.
    /// Results are cached for performance.
    /// </summary>
    /// <param name="assetClass">Asset class code (e.g., "CRYPTO", "STOCK_BIST")</param>
    /// <param name="market">Market code (e.g., "BINANCE", "BIST")</param>
    /// <returns>List of symbols ordered by broadcast_priority DESC</returns>
    Task<List<Symbol>> GetActiveSymbolsForBroadcastAsync(string assetClass, string market);

    /// <summary>
    /// Get system default symbols for anonymous or new users.
    /// Returns symbols where is_default_symbol=true.
    /// </summary>
    /// <param name="assetClass">Asset class code (optional filter)</param>
    /// <returns>List of default symbols ordered by display_order</returns>
    Task<List<Symbol>> GetDefaultSymbolsAsync(string? assetClass = null);

    /// <summary>
    /// Get user-specific symbol preferences.
    /// Joins with user_dashboard_preferences table.
    /// </summary>
    /// <param name="userId">User ID (GUID)</param>
    /// <param name="assetClass">Asset class code (optional filter)</param>
    /// <returns>List of symbols the user has selected</returns>
    Task<List<Symbol>> GetUserSymbolsAsync(string userId, string? assetClass = null);

    /// <summary>
    /// Update user symbol preferences.
    /// Replaces existing preferences with new list.
    /// </summary>
    /// <param name="userId">User ID (GUID)</param>
    /// <param name="symbolIds">List of symbol IDs to save as preferences</param>
    Task UpdateSymbolPreferencesAsync(string userId, List<string> symbolIds);

    /// <summary>
    /// Reload symbols from database and clear cache.
    /// Use this for hot-reload without service restart.
    /// </summary>
    Task ReloadSymbolsAsync();

    /// <summary>
    /// Get all active symbols for a specific asset class.
    /// Useful for populating dropdowns and symbol selectors.
    /// </summary>
    /// <param name="assetClass">Asset class code</param>
    /// <param name="includeInactive">Include inactive symbols</param>
    /// <returns>List of symbols ordered by display_order</returns>
    Task<List<Symbol>> GetSymbolsByAssetClassAsync(string assetClass, bool includeInactive = false);

    /// <summary>
    /// Get a single symbol by ticker and market.
    /// </summary>
    /// <param name="ticker">Symbol ticker (e.g., "BTCUSDT")</param>
    /// <param name="market">Market code (optional)</param>
    /// <returns>Symbol or null if not found</returns>
    Task<Symbol?> GetSymbolByTickerAsync(string ticker, string? market = null);

    /// <summary>
    /// Update last broadcast timestamp for rate limiting.
    /// </summary>
    /// <param name="symbolId">Symbol ID</param>
    /// <param name="broadcastTime">Broadcast timestamp</param>
    Task UpdateLastBroadcastTimeAsync(Guid symbolId, DateTime broadcastTime);

    /// <summary>
    /// Get symbols that need broadcast update based on priority and last broadcast time.
    /// </summary>
    /// <param name="assetClass">Asset class code</param>
    /// <param name="market">Market code</param>
    /// <param name="minIntervalSeconds">Minimum seconds since last broadcast</param>
    /// <returns>Symbols that should be broadcast now</returns>
    Task<List<Symbol>> GetSymbolsDueBroadcastAsync(string assetClass, string market, int minIntervalSeconds = 1);
}
