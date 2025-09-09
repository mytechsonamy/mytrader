using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class MarketData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public string Timeframe { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
    
    public decimal Open { get; set; }
    
    public decimal High { get; set; }
    
    public decimal Low { get; set; }
    
    public decimal Close { get; set; }
    
    public decimal Volume { get; set; }
}