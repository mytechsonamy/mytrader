using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTrader.Services.Market;

public record CreateAlertRequest(Guid SymbolId, string ConditionJson, string Channels, string? QuietHours);
public record AlertDto(Guid Id, Guid SymbolId, string ConditionJson, string Channels, string? QuietHours, bool IsActive);

public interface IAlertService
{
    Task<AlertDto> CreateAsync(Guid userId, CreateAlertRequest req);
    Task<IReadOnlyList<AlertDto>> ListAsync(Guid userId);
    Task<bool> SetActiveAsync(Guid userId, Guid alertId, bool isActive);
}