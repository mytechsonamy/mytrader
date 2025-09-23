using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

/// <summary>
/// Trading session information for different markets
/// Defines when markets are open for trading, including pre-market and after-hours sessions
/// </summary>
[Table("trading_sessions")]
public class TradingSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Associated market
    /// </summary>
    [Required]
    public Guid MarketId { get; set; }

    /// <summary>
    /// Session name (e.g., "Regular", "Pre-Market", "After-Hours", "Extended")
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("session_name")]
    public string SessionName { get; set; } = string.Empty;

    /// <summary>
    /// Session type (REGULAR, PRE_MARKET, AFTER_HOURS, EXTENDED, WEEKEND, HOLIDAY)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("session_type")]
    public string SessionType { get; set; } = "REGULAR";

    /// <summary>
    /// Day of week (0=Sunday, 1=Monday, ..., 6=Saturday)
    /// NULL means applies to all days
    /// </summary>
    [Column("day_of_week")]
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Session start time in market timezone (24-hour format)
    /// </summary>
    [Required]
    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// Session end time in market timezone (24-hour format)
    /// </summary>
    [Required]
    [Column("end_time")]
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Whether this session spans midnight (end_time < start_time)
    /// </summary>
    [Column("spans_midnight")]
    public bool SpansMidnight { get; set; } = false;

    /// <summary>
    /// Whether this is the primary trading session for the market
    /// </summary>
    [Column("is_primary")]
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Whether trading is allowed during this session
    /// </summary>
    [Column("is_trading_enabled")]
    public bool IsTradingEnabled { get; set; } = true;

    /// <summary>
    /// Volume multiplier for this session (relative to regular session)
    /// </summary>
    [Column("volume_multiplier", TypeName = "decimal(10,4)")]
    public decimal VolumeMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Effective start date for this session configuration
    /// </summary>
    [Column("effective_from")]
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective end date for this session configuration
    /// </summary>
    [Column("effective_to")]
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Session specific configuration as JSON
    /// </summary>
    [Column("session_config", TypeName = "jsonb")]
    public string? SessionConfig { get; set; }

    /// <summary>
    /// Whether this session is currently active
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for UI sorting
    /// </summary>
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("MarketId")]
    public Market Market { get; set; } = null!;

    // Helper methods for trading session logic
    public bool IsActiveAtTime(DateTime utcTime)
    {
        if (!IsActive || !IsTradingEnabled)
            return false;

        // Convert UTC time to market timezone
        var marketTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Market.Timezone);
        var marketTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, marketTimeZone);

        // Check day of week if specified
        if (DayOfWeek.HasValue && (int)marketTime.DayOfWeek != DayOfWeek.Value)
            return false;

        // Check effective date range
        if (EffectiveFrom.HasValue && utcTime < EffectiveFrom.Value)
            return false;
        if (EffectiveTo.HasValue && utcTime > EffectiveTo.Value)
            return false;

        var marketTimeOnly = TimeOnly.FromDateTime(marketTime);

        if (SpansMidnight)
        {
            // Session spans midnight (e.g., 22:00 - 06:00)
            return marketTimeOnly >= StartTime || marketTimeOnly <= EndTime;
        }
        else
        {
            // Normal session (e.g., 09:30 - 16:00)
            return marketTimeOnly >= StartTime && marketTimeOnly <= EndTime;
        }
    }
}

/// <summary>
/// Enum for session types
/// </summary>
public enum SessionType
{
    REGULAR,
    PRE_MARKET,
    AFTER_HOURS,
    EXTENDED,
    WEEKEND,
    HOLIDAY,
    MAINTENANCE
}