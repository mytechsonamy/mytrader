using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTrader.Application.Interfaces;
using MyTrader.Contracts;
using MyTrader.Domain.Entities;
using MyTrader.Infrastructure;

namespace MyTrader.Application.Services;

public class StrategyService : IStrategyService
{
    private readonly AppDbContext _db;

    public StrategyService(AppDbContext db) => _db = db;

    public async Task<StrategyTemplateResponse> CreateTemplateAsync(Guid userId, CreateStrategyTemplateRequest request)
    {
        var template = new StrategyTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Version = string.IsNullOrWhiteSpace(request.Version) ? "1.0" : request.Version,
            Parameters = request.Parameters ?? JsonDocument.Parse("{}"),
            ParamSchema = request.ParamSchema,
            CreatedBy = userId,
            IsDefault = request.IsDefault,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.StrategyTemplates.Add(template);
        await _db.SaveChangesAsync();
        return new StrategyTemplateResponse(template.Id, template.Name, template.Version);
    }

    public async Task<UserStrategyResponse> CreateUserStrategyAsync(Guid userId, CreateUserStrategyRequest request)
    {
        string? templateVersion = null;
        if (request.TemplateId.HasValue)
        {
            var t = await _db.StrategyTemplates.FirstOrDefaultAsync(x => x.Id == request.TemplateId.Value);
            if (t == null) throw new InvalidOperationException("Template not found");
            templateVersion = t.Version;
        }

        var us = new UserStrategy
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TemplateId = request.TemplateId,
            Name = request.Name,
            Parameters = request.Parameters ?? JsonDocument.Parse("{}"),
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow,
            TemplateVersion = templateVersion
        };

        _db.UserStrategies.Add(us);
        await _db.SaveChangesAsync();
        return new UserStrategyResponse(us.Id, us.Name, us.TemplateVersion, us.IsActive);
    }
}
