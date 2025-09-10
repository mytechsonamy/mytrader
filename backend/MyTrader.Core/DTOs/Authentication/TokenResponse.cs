namespace MyTrader.Core.DTOs.Authentication;

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// JWT ID for linking to session
    /// </summary>
    public string JwtId { get; set; } = string.Empty;
}