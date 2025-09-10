using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Authentication;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Device information for session tracking
    /// </summary>
    public string? DeviceName { get; set; }
}