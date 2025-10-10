using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

/// <summary>
/// User dashboard customization preferences for asset management
/// Allows users to select which assets they want to see on their dashboard
/// </summary>
[Table("user_dashboard_preferences")]
public class UserDashboardPreferences
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User who owns this dashboard preference
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Symbol/Asset that the user wants to track on their dashboard
    /// </summary>
    [Required]
    [Column("symbol_id")]
    public Guid SymbolId { get; set; }

    /// <summary>
    /// Display order for sorting assets on dashboard (lower numbers appear first)
    /// </summary>
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this asset is currently visible on dashboard
    /// </summary>
    [Column("is_visible")]
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether this asset is pinned/starred by user
    /// </summary>
    [Column("is_pinned")]
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Custom alias/nickname for this asset (user-defined)
    /// </summary>
    [MaxLength(100)]
    [Column("custom_alias")]
    public string? CustomAlias { get; set; }

    /// <summary>
    /// User's personal notes about this asset
    /// </summary>
    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Dashboard layout/widget type preference for this asset
    /// (e.g., "card", "chart", "mini", "detailed")
    /// </summary>
    [MaxLength(50)]
    [Column("widget_type")]
    public string WidgetType { get; set; } = "card";

    /// <summary>
    /// Dashboard-specific configuration as JSON
    /// (chart settings, alert thresholds, etc.)
    /// </summary>
    [Column("widget_config", TypeName = "jsonb")]
    public string? WidgetConfig { get; set; }

    /// <summary>
    /// Dashboard category/group for organizing assets
    /// </summary>
    [MaxLength(50)]
    [Column("category")]
    public string? Category { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("SymbolId")]
    public Symbol Symbol { get; set; } = null!;
}

/// <summary>
/// DTO for creating/updating dashboard preferences
/// </summary>
public class UserDashboardPreferenceDto
{
    public Guid? Id { get; set; }
    public Guid SymbolId { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsVisible { get; set; } = true;
    public bool IsPinned { get; set; } = false;
    public string? CustomAlias { get; set; }
    public string? Notes { get; set; }
    public string WidgetType { get; set; } = "card";
    public string? WidgetConfig { get; set; }
    public string? Category { get; set; }

    // Read-only symbol information
    public string? SymbolTicker { get; set; }
    public string? SymbolName { get; set; }
    public string? AssetClass { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? PriceChange24h { get; set; }
}

/// <summary>
/// Request DTO for bulk updating dashboard preferences
/// </summary>
public class BulkDashboardPreferenceUpdateDto
{
    public List<UserDashboardPreferenceDto> Preferences { get; set; } = new();
    public bool ResetOrder { get; set; } = false;
}

/// <summary>
/// Response DTO with detailed asset information for dashboard
/// </summary>
public class DashboardAssetDetailDto
{
    // User preference data
    public Guid PreferenceId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsPinned { get; set; }
    public string? CustomAlias { get; set; }
    public string? Notes { get; set; }
    public string WidgetType { get; set; } = "card";
    public string? WidgetConfig { get; set; }
    public string? Category { get; set; }

    // Symbol/Asset detailed information
    public Guid SymbolId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Display { get; set; }
    public string? Description { get; set; }
    public string AssetClass { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }

    // Market information
    public Guid? MarketId { get; set; }
    public string? MarketCode { get; set; }
    public string? MarketName { get; set; }
    public string? MarketTimezone { get; set; }
    public string? MarketStatus { get; set; }
    public string? PrimaryCurrency { get; set; }

    // Price information
    public decimal? CurrentPrice { get; set; }
    public decimal? PriceChange24h { get; set; }
    public decimal? MarketCap { get; set; }
    public decimal? Volume24h { get; set; }
    public DateTime? PriceUpdatedAt { get; set; }

    // Trading information
    public bool IsActive { get; set; }
    public bool IsTracked { get; set; }
    public decimal? TickSize { get; set; }
    public int? PricePrecision { get; set; }
    public int? QuantityPrecision { get; set; }

    public DateTime UpdatedAt { get; set; }
}