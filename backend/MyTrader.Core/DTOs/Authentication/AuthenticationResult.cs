using MyTrader.Core.Models;

namespace MyTrader.Core.DTOs.Authentication;

public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public User? User { get; set; }
}