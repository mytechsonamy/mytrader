using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

public class Candle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // DB schema uses SymbolId (uuid)
    public Guid SymbolId { get; set; }
    
    [NotMapped]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public string Timeframe { get; set; } = string.Empty;
    
    public DateTime OpenTime { get; set; }
    
    public decimal Open { get; set; }
    
    public decimal High { get; set; }
    
    public decimal Low { get; set; }
    
    public decimal Close { get; set; }
    
    public decimal Volume { get; set; }
    
    public decimal? Vwap { get; set; }
    
    public bool IsFinalized { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
