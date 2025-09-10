using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Contracts;

public record RefreshTokenRequest([Required] string RefreshToken);
public record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public record SessionDto(Guid Id, string? UserAgent, string? IpAddress, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? LastUsedAt, bool Revoked);
