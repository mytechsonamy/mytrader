namespace MyTrader.Core.DTOs.Authentication;

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
}