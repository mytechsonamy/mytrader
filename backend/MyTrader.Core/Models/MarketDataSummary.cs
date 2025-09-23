using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

/// <summary>
/// Pre-aggregated market data summaries for fast querying
/// Contains common statistical measures and performance metrics
/// </summary>
[Table("market_data_summaries")]
public class MarketDataSummary
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("symbol_id")]
    public Guid SymbolId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("symbol_ticker")]
    public string SymbolTicker { get; set; } = string.Empty;

    /// <summary>
    /// Summary period type (WEEKLY, MONTHLY, QUARTERLY, YEARLY)
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("period_type")]
    public string PeriodType { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the period
    /// </summary>
    [Required]
    [Column("period_start")]
    public DateOnly PeriodStart { get; set; }

    /// <summary>
    /// End date of the period
    /// </summary>
    [Required]
    [Column("period_end")]
    public DateOnly PeriodEnd { get; set; }

    /// <summary>
    /// Number of trading days in the period
    /// </summary>
    [Column("trading_days")]
    public int TradingDays { get; set; }

    // === PRICE STATISTICS ===

    [Column("period_open", TypeName = "decimal(18,8)")]
    public decimal? PeriodOpen { get; set; }

    [Column("period_close", TypeName = "decimal(18,8)")]
    public decimal? PeriodClose { get; set; }

    [Column("period_high", TypeName = "decimal(18,8)")]
    public decimal? PeriodHigh { get; set; }

    [Column("period_low", TypeName = "decimal(18,8)")]
    public decimal? PeriodLow { get; set; }

    [Column("period_vwap", TypeName = "decimal(18,8)")]
    public decimal? PeriodVWAP { get; set; }

    /// <summary>
    /// Total return for the period (percentage)
    /// </summary>
    [Column("total_return_percent", TypeName = "decimal(10,4)")]
    public decimal? TotalReturnPercent { get; set; }

    /// <summary>
    /// Average daily return (percentage)
    /// </summary>
    [Column("avg_daily_return_percent", TypeName = "decimal(10,4)")]
    public decimal? AvgDailyReturnPercent { get; set; }

    /// <summary>
    /// Volatility (standard deviation of daily returns)
    /// </summary>
    [Column("volatility", TypeName = "decimal(10,6)")]
    public decimal? Volatility { get; set; }

    /// <summary>
    /// Annualized volatility
    /// </summary>
    [Column("annualized_volatility", TypeName = "decimal(10,6)")]
    public decimal? AnnualizedVolatility { get; set; }

    /// <summary>
    /// Sharpe ratio for the period
    /// </summary>
    [Column("sharpe_ratio", TypeName = "decimal(10,4)")]
    public decimal? SharpeRatio { get; set; }

    /// <summary>
    /// Maximum drawdown during the period
    /// </summary>
    [Column("max_drawdown_percent", TypeName = "decimal(10,4)")]
    public decimal? MaxDrawdownPercent { get; set; }

    /// <summary>
    /// Beta coefficient (vs market index)
    /// </summary>
    [Column("beta", TypeName = "decimal(10,4)")]
    public decimal? Beta { get; set; }

    // === VOLUME STATISTICS ===

    [Column("total_volume", TypeName = "decimal(38,18)")]
    public decimal? TotalVolume { get; set; }

    [Column("avg_daily_volume", TypeName = "decimal(38,18)")]
    public decimal? AvgDailyVolume { get; set; }

    [Column("total_trading_value", TypeName = "decimal(38,18)")]
    public decimal? TotalTradingValue { get; set; }

    [Column("avg_daily_trading_value", TypeName = "decimal(38,18)")]
    public decimal? AvgDailyTradingValue { get; set; }

    [Column("total_transactions")]
    public long? TotalTransactions { get; set; }

    [Column("avg_daily_transactions")]
    public long? AvgDailyTransactions { get; set; }

    // === PRICE LEVELS ===

    /// <summary>
    /// Support level (lowest low of the period)
    /// </summary>
    [Column("support_level", TypeName = "decimal(18,8)")]
    public decimal? SupportLevel { get; set; }

    /// <summary>
    /// Resistance level (highest high of the period)
    /// </summary>
    [Column("resistance_level", TypeName = "decimal(18,8)")]
    public decimal? ResistanceLevel { get; set; }

    /// <summary>
    /// 52-week high (for yearly summaries)
    /// </summary>
    [Column("week_52_high", TypeName = "decimal(18,8)")]
    public decimal? Week52High { get; set; }

    /// <summary>
    /// 52-week low (for yearly summaries)
    /// </summary>
    [Column("week_52_low", TypeName = "decimal(18,8)")]
    public decimal? Week52Low { get; set; }

    // === TECHNICAL INDICATORS SUMMARY ===

    [Column("avg_rsi", TypeName = "decimal(10,4)")]
    public decimal? AvgRSI { get; set; }

    [Column("avg_macd", TypeName = "decimal(18,8)")]
    public decimal? AvgMACD { get; set; }

    /// <summary>
    /// Percentage of days above SMA20
    /// </summary>
    [Column("days_above_sma20_percent", TypeName = "decimal(10,2)")]
    public decimal? DaysAboveSMA20Percent { get; set; }

    /// <summary>
    /// Percentage of days above SMA50
    /// </summary>
    [Column("days_above_sma50_percent", TypeName = "decimal(10,2)")]
    public decimal? DaysAboveSMA50Percent { get; set; }

    // === MARKET COMPARISON ===

    /// <summary>
    /// Performance vs market index (percentage)
    /// </summary>
    [Column("vs_market_percent", TypeName = "decimal(10,4)")]
    public decimal? VsMarketPercent { get; set; }

    /// <summary>
    /// Correlation with market index
    /// </summary>
    [Column("market_correlation", TypeName = "decimal(10,6)")]
    public decimal? MarketCorrelation { get; set; }

    // === RANKING METRICS ===

    /// <summary>
    /// Percentile rank among all symbols in the same asset class
    /// </summary>
    [Column("performance_percentile")]
    public int? PerformancePercentile { get; set; }

    /// <summary>
    /// Volume percentile rank
    /// </summary>
    [Column("volume_percentile")]
    public int? VolumePercentile { get; set; }

    /// <summary>
    /// Quality score based on data completeness and consistency
    /// </summary>
    [Column("quality_score")]
    public int QualityScore { get; set; } = 100;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("calculated_at")]
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // === NAVIGATION PROPERTIES ===

    [ForeignKey("SymbolId")]
    public Symbol? Symbol { get; set; }

    // === HELPER METHODS ===

    /// <summary>
    /// Get risk-adjusted return (return per unit of volatility)
    /// </summary>
    public decimal? GetRiskAdjustedReturn()
    {
        if (!TotalReturnPercent.HasValue || !Volatility.HasValue || Volatility.Value == 0)
            return null;

        return TotalReturnPercent.Value / Volatility.Value;
    }

    /// <summary>
    /// Calculate period length in days
    /// </summary>
    public int GetPeriodLengthDays()
    {
        return PeriodEnd.DayNumber - PeriodStart.DayNumber + 1;
    }

    /// <summary>
    /// Check if this is a positive performance period
    /// </summary>
    public bool IsPositivePeriod()
    {
        return TotalReturnPercent.HasValue && TotalReturnPercent.Value > 0;
    }

    /// <summary>
    /// Get performance rating (1-5 stars based on percentile)
    /// </summary>
    public int GetPerformanceRating()
    {
        if (!PerformancePercentile.HasValue) return 3;

        return PerformancePercentile.Value switch
        {
            >= 90 => 5,
            >= 75 => 4,
            >= 50 => 3,
            >= 25 => 2,
            _ => 1
        };
    }
}