namespace MyTrader.Core.DTOs.Authentication;

public class UserSessionResponse
{
    public string SessionToken { get; set; } = string.Empty;
    public UserResponse User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}