using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public string? Phone { get; set; }
    
    public string? TelegramId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsEmailVerified { get; set; } = false;
    
    public DateTime? LastLogin { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // User preferences as JSON
    public string Preferences { get; set; } = "{}";
    
    // Trading settings
    public decimal DefaultInitialCapital { get; set; } = 10000m;
    public decimal DefaultRiskPercentage { get; set; } = 0.02m; // 2%
    
    // Subscription/Plan information
    public string Plan { get; set; } = "free"; // free, basic, pro
    public DateTime? PlanExpiresAt { get; set; }
    
    // Navigation properties
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<Strategy> Strategies { get; set; } = new List<Strategy>();
    public ICollection<IndicatorConfig> IndicatorConfigs { get; set; } = new List<IndicatorConfig>();
    public ICollection<BacktestResults> BacktestResults { get; set; } = new List<BacktestResults>();
    public ICollection<TradeHistory> TradeHistory { get; set; } = new List<TradeHistory>();
    public ICollection<PasswordReset> PasswordResets { get; set; } = new List<PasswordReset>();
    public ICollection<UserDashboardPreferences> DashboardPreferences { get; set; } = new List<UserDashboardPreferences>();
}