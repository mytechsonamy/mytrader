using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

public class IndicatorConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    // Bollinger Bands Configuration
    public int BollingerPeriod { get; set; } = 20;
    public decimal BollingerStdDev { get; set; } = 2.2m;
    public decimal BollingerTouchTolerance { get; set; } = 0.002m;
    
    // MACD Configuration
    public int MacdFast { get; set; } = 12;
    public int MacdSlow { get; set; } = 26;
    public int MacdSignal { get; set; } = 9;
    public bool UseMacdFilter { get; set; } = true;
    
    // RSI Configuration
    public int RsiPeriod { get; set; } = 14;
    public bool UseRsiFilter { get; set; } = true;
    public decimal RsiBuyMax { get; set; } = 35;
    public decimal RsiSellMin { get; set; } = 65;
    
    // EMA Trend Filter
    public bool UseEmaTrend { get; set; } = true;
    public int EmaTrendLength { get; set; } = 200;
    public string EmaTrendMode { get; set; } = "long_only_above"; // long_only_above, short_only_below, both
    
    // ATR Configuration
    public bool UseAtr { get; set; } = true;
    public int AtrLength { get; set; } = 14;
    public decimal AtrStopMultiplier { get; set; } = 2.0m;
    public decimal AtrTrailMultiplier { get; set; } = 2.5m;
    
    // Volume Filter
    public bool UseVolumeFilter { get; set; } = false;
    public decimal VolumeMultiplier { get; set; } = 1.5m;
    public int VolumeLookbackPeriod { get; set; } = 20;
    
    // Execution Parameters
    public decimal SlippagePercentage { get; set; } = 0.0005m;
    public decimal FeePercentage { get; set; } = 0.0004m;
    
    // Risk Management
    public decimal MaxPositionSize { get; set; } = 0.1m; // 10% of portfolio
    public decimal StopLossPercentage { get; set; } = 0.02m; // 2%
    public decimal TakeProfitPercentage { get; set; } = 0.04m; // 4%
    
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Additional custom indicators as JSON
    public string? CustomIndicators { get; set; } = "{}";
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Strategy> Strategies { get; set; } = new List<Strategy>();
}