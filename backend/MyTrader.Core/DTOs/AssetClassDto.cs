using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Data Transfer Object for Asset Class information
/// </summary>
public class AssetClassDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Asset class code (e.g., CRYPTO, STOCK_BIST, STOCK_NASDAQ)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the asset class
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Localized name for Turkish market support
    /// </summary>
    public string? NameTurkish { get; set; }

    /// <summary>
    /// Asset class description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Primary currency for this asset class
    /// </summary>
    public string PrimaryCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Default price precision for display
    /// </summary>
    public int DefaultPricePrecision { get; set; }

    /// <summary>
    /// Default quantity precision for display
    /// </summary>
    public int DefaultQuantityPrecision { get; set; }

    /// <summary>
    /// Whether this asset class supports 24/7 trading
    /// </summary>
    public bool Supports24x7Trading { get; set; }

    /// <summary>
    /// Whether fractional shares/units are supported
    /// </summary>
    public bool SupportsFractional { get; set; }

    /// <summary>
    /// Minimum trade amount in primary currency
    /// </summary>
    public decimal? MinTradeAmount { get; set; }

    /// <summary>
    /// Regulatory classification
    /// </summary>
    public string? RegulatoryClass { get; set; }

    /// <summary>
    /// Whether this asset class is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Display order for UI sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Associated markets count
    /// </summary>
    public int MarketsCount { get; set; }

    /// <summary>
    /// Associated symbols count
    /// </summary>
    public int SymbolsCount { get; set; }
}

/// <summary>
/// Request DTO for creating a new asset class
/// </summary>
public class CreateAssetClassRequest
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NameTurkish { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(12)]
    public string PrimaryCurrency { get; set; } = "USD";

    public int DefaultPricePrecision { get; set; } = 8;
    public int DefaultQuantityPrecision { get; set; } = 8;
    public bool Supports24x7Trading { get; set; } = false;
    public bool SupportsFractional { get; set; } = true;
    public decimal? MinTradeAmount { get; set; }

    [MaxLength(50)]
    public string? RegulatoryClass { get; set; }

    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Request DTO for updating an asset class
/// </summary>
public class UpdateAssetClassRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? NameTurkish { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(12)]
    public string? PrimaryCurrency { get; set; }

    public int? DefaultPricePrecision { get; set; }
    public int? DefaultQuantityPrecision { get; set; }
    public bool? Supports24x7Trading { get; set; }
    public bool? SupportsFractional { get; set; }
    public decimal? MinTradeAmount { get; set; }

    [MaxLength(50)]
    public string? RegulatoryClass { get; set; }

    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}