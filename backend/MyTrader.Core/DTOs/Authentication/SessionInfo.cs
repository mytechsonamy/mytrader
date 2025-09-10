namespace MyTrader.Core.DTOs.Authentication;

public class SessionInfo
{
    public Guid Id { get; set; }
    public string JwtId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsCurrentSession { get; set; }
}

public class SessionListResponse
{
    public List<SessionInfo> Sessions { get; set; } = new();
}