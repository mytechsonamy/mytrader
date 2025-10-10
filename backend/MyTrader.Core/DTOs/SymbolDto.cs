using System.Text.Json.Serialization;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Data transfer object for symbol information.
/// Mobile-friendly format with camelCase properties.
/// </summary>
public class SymbolDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("baseCurrency")]
    public string? BaseCurrency { get; set; }

    [JsonPropertyName("quoteCurrency")]
    public string? QuoteCurrency { get; set; }

    [JsonPropertyName("assetClass")]
    public string AssetClass { get; set; } = string.Empty;

    [JsonPropertyName("market")]
    public string? Market { get; set; }

    [JsonPropertyName("broadcastPriority")]
    public int BroadcastPriority { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isPopular")]
    public bool IsPopular { get; set; }

    [JsonPropertyName("currentPrice")]
    public decimal? CurrentPrice { get; set; }

    [JsonPropertyName("priceChange24h")]
    public decimal? PriceChange24h { get; set; }

    [JsonPropertyName("volume24h")]
    public decimal? Volume24h { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("sector")]
    public string? Sector { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

/// <summary>
/// Response wrapper for symbol list queries.
/// </summary>
public class SymbolListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("symbols")]
    public List<SymbolDto> Symbols { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("assetClass")]
    public string? AssetClass { get; set; }

    [JsonPropertyName("market")]
    public string? Market { get; set; }
}

/// <summary>
/// Request for updating user symbol preferences.
/// </summary>
public class UpdateSymbolPreferencesRequest
{
    [JsonPropertyName("symbolIds")]
    public List<string> SymbolIds { get; set; } = new();

    [JsonPropertyName("assetClass")]
    public string? AssetClass { get; set; }
}

/// <summary>
/// Response for symbol preference updates.
/// </summary>
public class UpdateSymbolPreferencesResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("updatedCount")]
    public int UpdatedCount { get; set; }
}

/// <summary>
/// Response for symbol reload operation.
/// </summary>
public class SymbolReloadResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("symbolsReloaded")]
    public int SymbolsReloaded { get; set; }
}
