using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Authentication;

public class PasswordResetVerifyRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 characters")]
    public string VerificationCode { get; set; } = string.Empty;
}