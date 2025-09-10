using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Domain.Entities;

[Table("symbols")]
public class Symbol
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("ticker")]
    [MaxLength(50)]
    public string Ticker { get; set; } = default!;

    [Column("venue")]
    [MaxLength(50)]
    public string Venue { get; set; } = default!;

    [Column("asset_class")]
    [MaxLength(20)]
    public string AssetClass { get; set; } = "CRYPTO";

    [Column("base_ccy")]
    [MaxLength(12)]
    public string? BaseCcy { get; set; }

    [Column("quote_ccy")]
    [MaxLength(12)]
    public string? QuoteCcy { get; set; }
}
