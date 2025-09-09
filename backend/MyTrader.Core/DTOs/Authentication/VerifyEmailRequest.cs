using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Authentication;

public class VerifyEmailRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string VerificationCode { get; set; } = string.Empty;
}