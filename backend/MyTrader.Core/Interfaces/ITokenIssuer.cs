using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace MyTrader.Core.Interfaces;

public interface ITokenIssuer
{
    (string accessToken, string refreshToken, DateTimeOffset expiresAt, string jwtId) IssueTokens(Guid userId, IEnumerable<Claim>? extraClaims = null);
}