using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MyTrader.Core.Models;

public class UserStrategy
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TemplateId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public JsonDocument Parameters { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument? CustomEntryRules { get; set; }
    public JsonDocument? CustomExitRules { get; set; }
    public JsonDocument? CustomRiskManagement { get; set; }
    public string? TargetSymbols { get; set; }
    public string? Timeframe { get; set; }
    public bool IsActive { get; set; } = false;
    public bool IsCustom { get; set; } = true;
    public bool IsFavorite { get; set; } = false;
    public decimal InitialCapital { get; set; } = 10000; // Non-nullable with default
    public decimal MaxPositionSizePercent { get; set; } = 10; // Non-nullable with default
    public string? TemplateVersion { get; set; }
    public JsonDocument? LastBacktestResults { get; set; }
    public DateTimeOffset? LastBacktestAt { get; set; }
    public JsonDocument? PerformanceStats { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
