using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class Signal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid StrategyId { get; set; }
    
    [Required]
    public Guid SymbolId { get; set; }
    
    [Required]
    public string SignalType { get; set; } = string.Empty; // BUY, SELL, NEUTRAL
    
    public decimal Price { get; set; }
    
    public decimal? Rsi { get; set; }
    
    public decimal? Macd { get; set; }
    
    public decimal? BollingerBandUpper { get; set; }
    
    public decimal? BollingerBandLower { get; set; }
    
    public string? BollingerPosition { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Additional indicator values as JSON string
    public string? AdditionalIndicators { get; set; }
    
    // Navigation properties
    public Strategy Strategy { get; set; } = null!;
    public Symbol Symbol { get; set; } = null!;
}