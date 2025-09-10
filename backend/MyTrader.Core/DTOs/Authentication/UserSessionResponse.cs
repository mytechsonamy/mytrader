namespace MyTrader.Core.DTOs.Authentication;

public class UserSessionResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserResponse User { get; set; } = null!;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// JWT ID for session management
    /// </summary>
    public string JwtId { get; set; } = string.Empty;
    
    /// <summary>
    /// Session ID for device management
    /// </summary>
    public Guid SessionId { get; set; }
}