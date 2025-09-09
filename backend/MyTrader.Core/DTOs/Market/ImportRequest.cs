using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Market;

public class ImportRequest
{
    public string Provider { get; set; } = "yahoo";
    
    [Required]
    public List<string> Symbols { get; set; } = new();
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
}