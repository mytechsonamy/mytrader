using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MyTrader.Contracts;

public record CreateStrategyTemplateRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(20)] string Version,
    JsonDocument Parameters,
    JsonDocument? ParamSchema,
    bool IsDefault
);

public record StrategyTemplateResponse(Guid Id, string Name, string Version);

public record CreateUserStrategyRequest(
    Guid? TemplateId,
    [Required, MaxLength(100)] string Name,
    JsonDocument Parameters
);

public record UserStrategyResponse(Guid Id, string Name, string? TemplateVersion, bool IsActive);
