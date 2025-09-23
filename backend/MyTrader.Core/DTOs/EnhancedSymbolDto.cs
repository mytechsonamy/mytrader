using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Enhanced Symbol DTO with multi-asset support
/// </summary>
public class EnhancedSymbolDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Symbol ticker/code
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Asset class information
    /// </summary>
    public AssetClassDto? AssetClass { get; set; }

    /// <summary>
    /// Market/exchange information
    /// </summary>
    public MarketSummaryDto? Market { get; set; }

    /// <summary>
    /// Base currency
    /// </summary>
    public string? BaseCurrency { get; set; }

    /// <summary>
    /// Quote currency
    /// </summary>
    public string? QuoteCurrency { get; set; }

    /// <summary>
    /// Full company/asset name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Turkish name for localization
    /// </summary>
    public string? FullNameTurkish { get; set; }

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string? Display { get; set; }

    /// <summary>
    /// Description or category
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Industry sector
    /// </summary>
    public string? Sector { get; set; }

    /// <summary>
    /// Industry group
    /// </summary>
    public string? Industry { get; set; }

    /// <summary>
    /// Country of incorporation/listing
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// ISIN code
    /// </summary>
    public string? ISIN { get; set; }

    /// <summary>
    /// Whether the symbol is active and tradeable
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the symbol is tracked
    /// </summary>
    public bool IsTracked { get; set; }

    /// <summary>
    /// Whether this symbol is popular/featured
    /// </summary>
    public bool IsPopular { get; set; }

    /// <summary>
    /// Price precision
    /// </summary>
    public int? PricePrecision { get; set; }

    /// <summary>
    /// Quantity precision
    /// </summary>
    public int? QuantityPrecision { get; set; }

    /// <summary>
    /// Minimum price movement
    /// </summary>
    public decimal? TickSize { get; set; }

    /// <summary>
    /// Minimum quantity increment
    /// </summary>
    public decimal? StepSize { get; set; }

    /// <summary>
    /// Minimum order value
    /// </summary>
    public decimal? MinOrderValue { get; set; }

    /// <summary>
    /// Maximum order value
    /// </summary>
    public decimal? MaxOrderValue { get; set; }

    /// <summary>
    /// 24h trading volume
    /// </summary>
    public decimal? Volume24h { get; set; }

    /// <summary>
    /// Market capitalization
    /// </summary>
    public decimal? MarketCap { get; set; }

    /// <summary>
    /// Current price
    /// </summary>
    public decimal? CurrentPrice { get; set; }

    /// <summary>
    /// 24h price change percentage
    /// </summary>
    public decimal? PriceChange24h { get; set; }

    /// <summary>
    /// Last price update timestamp
    /// </summary>
    public DateTime? PriceUpdatedAt { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Legacy fields for backward compatibility
    /// </summary>
    public string Venue { get; set; } = string.Empty;

    /// <summary>
    /// Legacy asset class field
    /// </summary>
    public string LegacyAssetClass { get; set; } = string.Empty;

    /// <summary>
    /// Symbol created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Symbol last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Simplified symbol DTO for listings and search results
/// </summary>
public class SymbolSummaryDto
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? Display { get; set; }
    public string? FullName { get; set; }
    public string? FullNameTurkish { get; set; }
    public string AssetClassCode { get; set; } = string.Empty;
    public string MarketCode { get; set; } = string.Empty;
    public string? BaseCurrency { get; set; }
    public string? QuoteCurrency { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? PriceChange24h { get; set; }
    public decimal? Volume24h { get; set; }
    public bool IsActive { get; set; }
    public bool IsTracked { get; set; }
    public bool IsPopular { get; set; }
    public int? PricePrecision { get; set; }
}

/// <summary>
/// Symbol search result DTO
/// </summary>
public class SymbolSearchResultDto
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? FullNameTurkish { get; set; }
    public string AssetClassCode { get; set; } = string.Empty;
    public string AssetClassName { get; set; } = string.Empty;
    public string MarketCode { get; set; } = string.Empty;
    public string MarketName { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public string? Country { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? PriceChange24h { get; set; }
    public bool IsActive { get; set; }
    public bool IsTracked { get; set; }
    public bool IsPopular { get; set; }
    public decimal? MatchScore { get; set; } // For search relevance ranking
}

/// <summary>
/// Request DTO for symbol search
/// </summary>
public class SymbolSearchRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public string Query { get; set; } = string.Empty;

    public string? AssetClass { get; set; }
    public string? Market { get; set; }
    public string? Country { get; set; }
    public string? Sector { get; set; }
    public bool? IsActive { get; set; } = true;
    public bool? IsPopular { get; set; }
    public int? Limit { get; set; } = 50;
    public string? Language { get; set; } = "en";
}

/// <summary>
/// Request DTO for creating a new symbol
/// </summary>
public class CreateSymbolRequest
{
    [Required]
    [MaxLength(50)]
    public string Ticker { get; set; } = string.Empty;

    public Guid? AssetClassId { get; set; }
    public Guid? MarketId { get; set; }

    [MaxLength(12)]
    public string? BaseCurrency { get; set; }

    [MaxLength(12)]
    public string? QuoteCurrency { get; set; }

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(200)]
    public string? FullNameTurkish { get; set; }

    [MaxLength(100)]
    public string? Display { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Sector { get; set; }

    [MaxLength(100)]
    public string? Industry { get; set; }

    [MaxLength(10)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? ISIN { get; set; }

    public bool IsTracked { get; set; } = false;
    public bool IsPopular { get; set; } = false;
    public int? PricePrecision { get; set; }
    public int? QuantityPrecision { get; set; }
    public decimal? TickSize { get; set; }
    public decimal? StepSize { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxOrderValue { get; set; }
    public int DisplayOrder { get; set; } = 0;

    // Legacy fields for backward compatibility
    [MaxLength(50)]
    public string? Venue { get; set; }

    [MaxLength(20)]
    public string? LegacyAssetClass { get; set; } = "CRYPTO";
}

/// <summary>
/// Request DTO for updating a symbol
/// </summary>
public class UpdateSymbolRequest
{
    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(200)]
    public string? FullNameTurkish { get; set; }

    [MaxLength(100)]
    public string? Display { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Sector { get; set; }

    [MaxLength(100)]
    public string? Industry { get; set; }

    [MaxLength(10)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? ISIN { get; set; }

    public bool? IsActive { get; set; }
    public bool? IsTracked { get; set; }
    public bool? IsPopular { get; set; }
    public int? PricePrecision { get; set; }
    public int? QuantityPrecision { get; set; }
    public decimal? TickSize { get; set; }
    public decimal? StepSize { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxOrderValue { get; set; }
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// Bulk update request for symbol tracking status
/// </summary>
public class BulkUpdateSymbolTrackingRequest
{
    [Required]
    public List<Guid> SymbolIds { get; set; } = new();

    [Required]
    public bool IsTracked { get; set; }
}