using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Portfolio;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string SymbolName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Fee { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string? Notes { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class CreateTransactionDto
{
    [Required]
    public Guid PortfolioId { get; set; }
    
    [Required]
    public Guid SymbolId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string TransactionType { get; set; } = string.Empty; // BUY, SELL
    
    [Required]
    [MaxLength(10)]
    public string Side { get; set; } = "LONG"; // LONG, SHORT
    
    [Required]
    [Range(0.00000001, double.MaxValue)]
    public decimal Quantity { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal Fee { get; set; } = 0m;
    
    [MaxLength(12)]
    public string Currency { get; set; } = "USD";
    
    [MaxLength(100)]
    public string? OrderId { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public DateTime? ExecutedAt { get; set; }
}

public class TransactionHistoryRequestDto
{
    public Guid? PortfolioId { get; set; }
    public Guid? SymbolId { get; set; }
    public string? TransactionType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class TransactionHistoryResponseDto
{
    public List<TransactionDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}