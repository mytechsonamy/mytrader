using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class UserNotificationPreferences
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    // Email notifications
    public bool EnableEmailSignals { get; set; } = true;
    public bool EnableEmailTradeExecutions { get; set; } = true;
    public bool EnableEmailBacktestResults { get; set; } = true;
    public bool EnableEmailAccountUpdates { get; set; } = true;
    public bool EnableEmailMarketing { get; set; } = false;
    
    // Push notifications
    public bool EnablePushSignals { get; set; } = true;
    public bool EnablePushTradeExecutions { get; set; } = true;
    public bool EnablePushBacktestResults { get; set; } = false;
    public bool EnablePushAccountUpdates { get; set; } = true;
    public bool EnablePushMarketing { get; set; } = false;
    
    // SMS notifications
    public bool EnableSmsSignals { get; set; } = false;
    public bool EnableSmsTradeExecutions { get; set; } = true;
    public bool EnableSmsBacktestResults { get; set; } = false;
    public bool EnableSmsAccountUpdates { get; set; } = true;
    
    // Telegram notifications
    public bool EnableTelegramSignals { get; set; } = false;
    public bool EnableTelegramTradeExecutions { get; set; } = false;
    public bool EnableTelegramBacktestResults { get; set; } = false;
    public bool EnableTelegramAccountUpdates { get; set; } = false;
    
    // Signal filtering
    public decimal MinSignalConfidence { get; set; } = 0.7m; // 70%
    public string? PreferredTradingHours { get; set; } // JSON: {"start": "09:00", "end": "17:00", "timezone": "UTC"}
    public string? FilterBySymbols { get; set; } // Comma-separated list
    public string? FilterByStrategies { get; set; } // Comma-separated list of strategy IDs
    
    // Additional alert types
    public bool EnableSignalAlerts { get; set; } = true;
    public bool EnablePortfolioAlerts { get; set; } = true;
    public bool EnableStrategyAlerts { get; set; } = true;
    public bool EnableMarketAlerts { get; set; } = false;
    
    // Alert thresholds and settings
    public decimal PortfolioChangeThreshold { get; set; } = 5m; // 5% change
    public string AlertMethods { get; set; } = "[\"push\"]"; // JSON array of methods
    
    // Quiet hours
    public bool QuietHoursEnabled { get; set; } = false;
    public TimeSpan QuietHoursStart { get; set; } = new(22, 0, 0);
    public TimeSpan QuietHoursEnd { get; set; } = new(8, 0, 0);
    public string QuietHoursDays { get; set; } = "[6,0]"; // JSON array: Saturday, Sunday
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}