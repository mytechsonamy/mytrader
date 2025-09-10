using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Services.Market;

// Temporary entity class for user_alerts since we don't have full EF model yet
public class UserAlert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SymbolId { get; set; }
    public string ConditionJson { get; set; } = string.Empty;
    public string Channels { get; set; } = string.Empty;
    public string? QuietHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AlertService : IAlertService
{
    private readonly TradingDbContext _context;

    public AlertService(TradingDbContext context)
    {
        _context = context;
    }

    public async Task<AlertDto> CreateAsync(Guid userId, CreateAlertRequest req)
    {
        var alertId = Guid.NewGuid();
        
        await _context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO user_alerts (id, user_id, symbol_id, condition_json, channels, quiet_hours, is_active)
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6});
        ", alertId, userId, req.SymbolId, req.ConditionJson, req.Channels, req.QuietHours ?? string.Empty, true);

        return new AlertDto(alertId, req.SymbolId, req.ConditionJson, req.Channels, req.QuietHours, true);
    }

    public async Task<IReadOnlyList<AlertDto>> ListAsync(Guid userId)
    {
        // Use raw SQL since we don't have EF entity for user_alerts yet
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, symbol_id, condition_json, channels, quiet_hours, is_active 
            FROM user_alerts 
            WHERE user_id = @userId";
        
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@userId";
        parameter.Value = userId;
        command.Parameters.Add(parameter);

        var alerts = new List<AlertDto>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            alerts.Add(new AlertDto(
                reader.GetGuid(0), // id
                reader.GetGuid(1), // symbol_id
                reader.GetString(2), // condition_json
                reader.GetString(3), // channels
                reader.IsDBNull(4) ? null : reader.GetString(4), // quiet_hours
                reader.GetBoolean(5) // is_active
            ));
        }

        return alerts;
    }

    public async Task<bool> SetActiveAsync(Guid userId, Guid alertId, bool isActive)
    {
        var affected = await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE user_alerts 
            SET is_active = {0} 
            WHERE id = {1} AND user_id = {2}
        ", isActive, alertId, userId);
        
        return affected > 0;
    }
}