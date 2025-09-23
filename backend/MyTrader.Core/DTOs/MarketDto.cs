using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Data Transfer Object for Market/Exchange information
/// </summary>
public class MarketDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Market code (e.g., BINANCE, BIST, NASDAQ)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full market name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Localized name for Turkish market support
    /// </summary>
    public string? NameTurkish { get; set; }

    /// <summary>
    /// Market description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Associated asset class information
    /// </summary>
    public AssetClassDto AssetClass { get; set; } = null!;

    /// <summary>
    /// Country code where the market is located
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Market timezone
    /// </summary>
    public string Timezone { get; set; } = string.Empty;

    /// <summary>
    /// Primary trading currency
    /// </summary>
    public string PrimaryCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Market maker identifier
    /// </summary>
    public string? MarketMaker { get; set; }

    /// <summary>
    /// Default commission rate
    /// </summary>
    public decimal? DefaultCommissionRate { get; set; }

    /// <summary>
    /// Market status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Last status update timestamp
    /// </summary>
    public DateTime? StatusUpdatedAt { get; set; }

    /// <summary>
    /// Whether this market is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether real-time data is available
    /// </summary>
    public bool HasRealtimeData { get; set; }

    /// <summary>
    /// Data delay in minutes
    /// </summary>
    public int DataDelayMinutes { get; set; }

    /// <summary>
    /// Display order for UI sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Number of symbols in this market
    /// </summary>
    public int SymbolsCount { get; set; }

    /// <summary>
    /// Number of active data providers
    /// </summary>
    public int DataProvidersCount { get; set; }

    /// <summary>
    /// Trading session information
    /// </summary>
    public List<TradingSessionDto> TradingSessions { get; set; } = new();
}

/// <summary>
/// Simplified market DTO for listings
/// </summary>
public class MarketSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameTurkish { get; set; }
    public string AssetClassCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SymbolsCount { get; set; }
}

/// <summary>
/// Market status DTO for real-time status updates
/// </summary>
public class MarketStatusDto
{
    public Guid MarketId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StatusUpdatedAt { get; set; }
    public string? StatusMessage { get; set; }
    public bool IsActive { get; set; }
    public TimeSpan? NextSessionStart { get; set; }
    public TimeSpan? NextSessionEnd { get; set; }
    public string? CurrentSessionType { get; set; }
}

/// <summary>
/// Trading session DTO
/// </summary>
public class TradingSessionDto
{
    public Guid Id { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? DaysOfWeek { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Request DTO for creating a new market
/// </summary>
public class CreateMarketRequest
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? NameTurkish { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public Guid AssetClassId { get; set; }

    [Required]
    [MaxLength(10)]
    public string CountryCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Timezone { get; set; } = "UTC";

    [Required]
    [MaxLength(12)]
    public string PrimaryCurrency { get; set; } = "USD";

    [MaxLength(50)]
    public string? MarketMaker { get; set; }

    [MaxLength(200)]
    public string? ApiBaseUrl { get; set; }

    [MaxLength(200)]
    public string? WebSocketUrl { get; set; }

    public decimal? DefaultCommissionRate { get; set; }
    public decimal? MinCommission { get; set; }
    public bool HasRealtimeData { get; set; } = true;
    public int DataDelayMinutes { get; set; } = 0;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Request DTO for updating a market
/// </summary>
public class UpdateMarketRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? NameTurkish { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(10)]
    public string? CountryCode { get; set; }

    [MaxLength(50)]
    public string? Timezone { get; set; }

    [MaxLength(12)]
    public string? PrimaryCurrency { get; set; }

    [MaxLength(50)]
    public string? MarketMaker { get; set; }

    [MaxLength(200)]
    public string? ApiBaseUrl { get; set; }

    [MaxLength(200)]
    public string? WebSocketUrl { get; set; }

    public decimal? DefaultCommissionRate { get; set; }
    public decimal? MinCommission { get; set; }
    public bool? IsActive { get; set; }
    public bool? HasRealtimeData { get; set; }
    public int? DataDelayMinutes { get; set; }
    public int? DisplayOrder { get; set; }
}