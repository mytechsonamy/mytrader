using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyTrader.Contracts;

namespace MyTrader.Application.Interfaces;

public interface IAuthService
{
    Task<(bool ok, TokenResponse? tokens)> RefreshTokenAsync(Guid userId, string refreshTokenRaw, string? userAgent, string? ip);
    Task<IReadOnlyList<SessionDto>> ListSessionsAsync(Guid userId);
    Task RevokeAllAsync(Guid userId);
    Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId);
}
