using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyTrader.Core.Models;

[Table("transactions")]
public class Transaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("portfolio_id")]
    [Required]
    public Guid PortfolioId { get; set; }

    [Column("symbol_id")]
    [Required]
    public Guid SymbolId { get; set; }

    [Column("transaction_type")]
    [MaxLength(10)]
    [Required]
    public string TransactionType { get; set; } = string.Empty; // BUY, SELL, DIVIDEND, FEE

    [Column("side")]
    [MaxLength(10)]
    [Required]
    public string Side { get; set; } = string.Empty; // LONG, SHORT

    [Column("quantity")]
    [Precision(18, 8)]
    public decimal Quantity { get; set; } = 0m;

    [Column("price")]
    [Precision(18, 8)]
    public decimal Price { get; set; } = 0m;

    [Column("total_amount")]
    [Precision(18, 8)]
    public decimal TotalAmount { get; set; } = 0m;

    [Column("fee")]
    [Precision(18, 8)]
    public decimal Fee { get; set; } = 0m;

    [Column("currency")]
    [MaxLength(12)]
    [Required]
    public string Currency { get; set; } = "USD";

    [Column("status")]
    [MaxLength(20)]
    [Required]
    public string Status { get; set; } = "COMPLETED"; // PENDING, COMPLETED, CANCELLED, FAILED

    [Column("order_id")]
    [MaxLength(100)]
    public string? OrderId { get; set; }

    [Column("execution_id")]
    [MaxLength(100)]
    public string? ExecutionId { get; set; }

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column("executed_at")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public UserPortfolio Portfolio { get; set; } = null!;
    public Symbol Symbol { get; set; } = null!;
}