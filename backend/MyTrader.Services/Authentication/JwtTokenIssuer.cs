using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyTrader.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyTrader.Services.Authentication;

public class JwtTokenIssuer : ITokenIssuer
{
    private readonly IConfiguration _configuration;

    public JwtTokenIssuer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string accessToken, string refreshToken, DateTimeOffset expiresAt, string jwtId) IssueTokens(Guid userId, IEnumerable<Claim>? extraClaims = null)
    {
        var jwtId = Guid.NewGuid().ToString();
        var accessToken = GenerateJwtToken(userId, jwtId, extraClaims);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15); // Short-lived access token

        return (accessToken, refreshToken, expiresAt, jwtId);
    }

    private string GenerateJwtToken(Guid userId, string jwtId, IEnumerable<Claim>? extraClaims = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // Standard sub claim
            new Claim("sub", userId.ToString()), // Explicit sub claim
            new Claim("user_id", userId.ToString()),
            new Claim("jti", jwtId), // JWT ID for session linking
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (extraClaims != null)
        {
            claims.AddRange(extraClaims);
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15), // Short-lived access token
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}