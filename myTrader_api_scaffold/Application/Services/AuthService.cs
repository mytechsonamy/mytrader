using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTrader.Application.Interfaces;
using MyTrader.Contracts;
using MyTrader.Domain.Entities;
using MyTrader.Infrastructure;

namespace MyTrader.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenIssuer _issuer;

    public AuthService(AppDbContext db, ITokenIssuer issuer)
    {
        _db = db;
        _issuer = issuer;
    }

    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    public async Task<(bool ok, TokenResponse? tokens)> RefreshTokenAsync(Guid userId, string refreshTokenRaw, string? userAgent, string? ip)
    {
        var now = DateTimeOffset.UtcNow;
        var hash = Hash(refreshTokenRaw);

        var session = await _db.UserSessions
            .Where(s => s.UserId == userId && s.RefreshTokenHash == hash)
            .FirstOrDefaultAsync();

        if (session is null || session.RevokedAt != null || session.ExpiresAt <= now)
        {
            if (session is not null)
            {
                var family = session.TokenFamilyId;
                var all = _db.UserSessions.Where(s => s.UserId == userId && s.TokenFamilyId == family && s.RevokedAt == null);
                await all.ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now));
                await _db.SaveChangesAsync();
            }
            return (false, null);
        }

        session.RevokedAt = now;
        session.LastUsedAt = now;
        _db.UserSessions.Update(session);

        var (access, refresh, expiresAt, jwtId) = _issuer.IssueTokens(userId, null);
        var newSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JwtId = jwtId,
            RefreshTokenHash = Hash(refresh),
            TokenFamilyId = session.TokenFamilyId,
            RotatedFrom = session.Id,
            UserAgent = userAgent,
            IpAddress = ip,
            CreatedAt = now,
            ExpiresAt = now.AddDays(30)
        };
        await _db.UserSessions.AddAsync(newSession);
        await _db.SaveChangesAsync();

        return (true, new TokenResponse(access, refresh, expiresAt));
    }

    public async Task<IReadOnlyList<SessionDto>> ListSessionsAsync(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var list = await _db.UserSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SessionDto(
                s.Id, s.UserAgent, s.IpAddress, s.CreatedAt, s.ExpiresAt, s.LastUsedAt, s.RevokedAt != null || s.ExpiresAt <= now
            ))
            .ToListAsync();
        return list;
    }

    public async Task RevokeAllAsync(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now));
        await _db.SaveChangesAsync();
    }

    public async Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        var affected = await _db.UserSessions
            .Where(s => s.UserId == userId && s.Id == sessionId && s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now));
        return affected > 0;
    }
}
