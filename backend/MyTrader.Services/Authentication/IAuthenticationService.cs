using MyTrader.Core.DTOs.Authentication;

namespace MyTrader.Services.Authentication;

public interface IAuthenticationService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<RegisterResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task<RegisterResponse> ResendVerificationAsync(string email);
    Task<UserSessionResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(Guid userId);
    Task<UserResponse?> GetUserAsync(Guid userId);
    Task<RegisterResponse> UpdateUserAsync(Guid userId, UpdateProfileRequest request);
    Task<RegisterResponse> RequestPasswordResetAsync(string email);
    Task<RegisterResponse> VerifyPasswordResetAsync(string email, string code);
    Task<RegisterResponse> ResetPasswordAsync(string email, string newPassword);
}

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? TelegramId { get; set; }
}