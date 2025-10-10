using MyTrader.Core.DTOs.Authentication;

namespace MyTrader.Services.Authentication;

public interface IAuthenticationService
{
    // Existing authentication methods
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<RegisterResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task<RegisterResponse> ResendVerificationAsync(string email);
    Task<UserSessionResponse> LoginAsync(LoginRequest request, string? userAgent = null, string? ipAddress = null);
    Task LogoutAsync(Guid userId);
    Task<UserResponse?> GetUserAsync(Guid userId);
    Task<RegisterResponse> UpdateUserAsync(Guid userId, UpdateProfileRequest request);
    Task<RegisterResponse> RequestPasswordResetAsync(string email);
    Task<RegisterResponse> VerifyPasswordResetAsync(string email, string code);
    Task<RegisterResponse> ResetPasswordAsync(string email, string newPassword);
    
    // New session management methods
    
    /// <summary>
    /// Refresh access token using refresh token with rotation and reuse detection
    /// </summary>
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string? userAgent = null, string? ipAddress = null);
    
    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    Task<SessionListResponse> GetUserSessionsAsync(Guid userId, string? currentJwtId = null);
    
    /// <summary>
    /// Logout from all devices (revoke all sessions)
    /// </summary>
    Task LogoutAllAsync(Guid userId);
    
    /// <summary>
    /// Logout from a specific session/device
    /// </summary>
    Task LogoutSessionAsync(Guid userId, Guid sessionId);
    
    /// <summary>
    /// Validate JWT token and get user info (for middleware)
    /// </summary>
    Task<UserResponse?> ValidateTokenAsync(string jwtId, Guid userId);
    
    /// <summary>
    /// Revoke a token family when token reuse is detected
    /// </summary>
    Task RevokeTokenFamilyAsync(Guid tokenFamilyId, string reason = "token_reuse");
}

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? TelegramId { get; set; }
}