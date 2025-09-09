using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Authentication;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [MinLength(2)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MinLength(2)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public string Phone { get; set; } = string.Empty;
}