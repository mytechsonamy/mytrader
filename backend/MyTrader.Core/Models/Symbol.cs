using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("symbols")]
public class Symbol
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string Ticker { get; set; } = default!;

    [MaxLength(50)]
    public string Venue { get; set; } = default!;

    [MaxLength(20)]
    public string AssetClass { get; set; } = "CRYPTO";

    [MaxLength(12)]
    public string? BaseCurrency { get; set; }

    [MaxLength(12)]
    public string? QuoteCurrency { get; set; }

    // Optional extended fields expected by services; present if created by migrations
    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(100)]
    [Column("display")]
    public string? Display { get; set; }

    public bool IsActive { get; set; }
    public bool IsTracked { get; set; }

    public int? PricePrecision { get; set; }
    public int? QuantityPrecision { get; set; }

    [Column(TypeName = "numeric(38,18)")]
    public decimal? TickSize { get; set; }

    [Column(TypeName = "numeric(38,18)")]
    public decimal? StepSize { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
