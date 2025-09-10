using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs.Dashboard;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace MyTrader.Services.Notifications;

public interface INotificationService
{
    Task SendSignalNotificationAsync(Guid userId, SignalNotification signal);
    Task SendPortfolioAlertAsync(Guid userId, PortfolioUpdateDto portfolioUpdate);
    Task SendStrategyAlertAsync(Guid userId, StrategyPerformanceUpdate strategyUpdate);
    Task SendMarketAlertAsync(List<Guid> userIds, MarketEventDto marketEvent);
    Task<bool> RegisterDeviceAsync(Guid userId, DeviceRegistration device);
    Task<bool> UnregisterDeviceAsync(Guid userId, string deviceToken);
    Task<NotificationPreferences> GetUserPreferencesAsync(Guid userId);
    Task UpdateUserPreferencesAsync(Guid userId, NotificationPreferences preferences);
    Task<List<NotificationHistory>> GetNotificationHistoryAsync(Guid userId, int limit = 50);

    // Price Alerts
    Task<PriceAlert> CreatePriceAlertAsync(Guid userId, string symbol, string alertType, decimal targetPrice, decimal? percentageChange = null, string? message = null);
    Task<List<PriceAlert>> GetUserPriceAlertsAsync(Guid userId, bool activeOnly = true);
    Task<PriceAlert?> GetPriceAlertAsync(Guid alertId);
    Task<bool> UpdatePriceAlertAsync(Guid alertId, bool isActive);
    Task<bool> DeletePriceAlertAsync(Guid alertId);
    Task CheckPriceAlertsAsync(string symbol, decimal currentPrice);
}

public class NotificationService : INotificationService
{
    private readonly TradingDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;
    private readonly HttpClient _httpClient;
    
    // Firebase Cloud Messaging settings
    private readonly string? _fcmServerKey;
    private readonly string? _fcmSenderId;
    
    public NotificationService(
        TradingDbContext context,
        IConfiguration configuration,
        ILogger<NotificationService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        
        _fcmServerKey = _configuration["Firebase:ServerKey"];
        _fcmSenderId = _configuration["Firebase:SenderId"];
    }

    public async Task SendSignalNotificationAsync(Guid userId, SignalNotification signal)
    {
        try
        {
            var preferences = await GetUserPreferencesAsync(userId);
            
            if (!preferences.EnableSignalAlerts || signal.Confidence < preferences.MinSignalConfidence)
            {
                _logger.LogDebug("Signal notification skipped for user {UserId} due to preferences", userId);
                return;
            }

            if (IsInQuietHours(preferences.QuietHours))
            {
                _logger.LogDebug("Signal notification delayed due to quiet hours for user {UserId}", userId);
                await QueueNotificationAsync(userId, "signal", signal, DateTime.UtcNow.Add(GetTimeUntilQuietHoursEnd(preferences.QuietHours)));
                return;
            }

            var title = $"{signal.Symbol} {signal.Type} Signal";
            var body = $"{signal.Confidence:F1}% confidence - {signal.Reason}";
            var data = new Dictionary<string, string>
            {
                ["type"] = "signal",
                ["symbol"] = signal.Symbol,
                ["signalType"] = signal.Type,
                ["confidence"] = signal.Confidence.ToString("F1"),
                ["price"] = signal.Price.ToString("F2"),
                ["signalId"] = signal.Id.ToString()
            };

            await SendNotificationToUserAsync(userId, title, body, data, preferences.AlertMethods);
            await LogNotificationAsync(userId, "signal", title, body, JsonSerializer.Serialize(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signal notification to user {UserId}", userId);
        }
    }

    public async Task SendPortfolioAlertAsync(Guid userId, PortfolioUpdateDto portfolioUpdate)
    {
        try
        {
            var preferences = await GetUserPreferencesAsync(userId);
            
            if (!preferences.EnablePortfolioAlerts)
            {
                return;
            }

            // Check if change threshold is exceeded
            var changePercent = Math.Abs(portfolioUpdate.ChangePercent);
            if (changePercent < preferences.PortfolioChangeThreshold)
            {
                return;
            }

            if (IsInQuietHours(preferences.QuietHours))
            {
                await QueueNotificationAsync(userId, "portfolio", portfolioUpdate, DateTime.UtcNow.Add(GetTimeUntilQuietHoursEnd(preferences.QuietHours)));
                return;
            }

            var direction = portfolioUpdate.ChangePercent >= 0 ? "gained" : "lost";
            var title = $"Portfolio Alert";
            var body = $"Your portfolio has {direction} {changePercent:F1}% ({portfolioUpdate.Change:C})";
            var data = new Dictionary<string, string>
            {
                ["type"] = "portfolio",
                ["totalValue"] = portfolioUpdate.TotalValue.ToString("F2"),
                ["change"] = portfolioUpdate.Change.ToString("F2"),
                ["changePercent"] = portfolioUpdate.ChangePercent.ToString("F1")
            };

            await SendNotificationToUserAsync(userId, title, body, data, preferences.AlertMethods);
            await LogNotificationAsync(userId, "portfolio", title, body, JsonSerializer.Serialize(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending portfolio alert to user {UserId}", userId);
        }
    }

    public async Task SendStrategyAlertAsync(Guid userId, StrategyPerformanceUpdate strategyUpdate)
    {
        try
        {
            var preferences = await GetUserPreferencesAsync(userId);
            
            if (!preferences.EnableStrategyAlerts)
            {
                return;
            }

            if (IsInQuietHours(preferences.QuietHours))
            {
                await QueueNotificationAsync(userId, "strategy", strategyUpdate, DateTime.UtcNow.Add(GetTimeUntilQuietHoursEnd(preferences.QuietHours)));
                return;
            }

            // Only send alerts for significant performance changes
            if (Math.Abs(strategyUpdate.DailyReturn) < 2m) // Less than 2% daily change
            {
                return;
            }

            var direction = strategyUpdate.DailyReturn >= 0 ? "up" : "down";
            var title = $"Strategy Performance Alert";
            var body = $"{strategyUpdate.Name} is {direction} {Math.Abs(strategyUpdate.DailyReturn):F1}% today";
            var data = new Dictionary<string, string>
            {
                ["type"] = "strategy",
                ["strategyId"] = strategyUpdate.StrategyId.ToString(),
                ["strategyName"] = strategyUpdate.Name,
                ["dailyReturn"] = strategyUpdate.DailyReturn.ToString("F1"),
                ["totalReturn"] = strategyUpdate.TotalReturn.ToString("F1")
            };

            await SendNotificationToUserAsync(userId, title, body, data, preferences.AlertMethods);
            await LogNotificationAsync(userId, "strategy", title, body, JsonSerializer.Serialize(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending strategy alert to user {UserId}", userId);
        }
    }

    public async Task SendMarketAlertAsync(List<Guid> userIds, MarketEventDto marketEvent)
    {
        try
        {
            if (marketEvent.Impact != "High") // Only send high impact events
            {
                return;
            }

            var title = "Market Alert";
            var body = $"{marketEvent.Title} - {marketEvent.Description}";
            var data = new Dictionary<string, string>
            {
                ["type"] = "market",
                ["eventType"] = marketEvent.Type,
                ["impact"] = marketEvent.Impact,
                ["title"] = marketEvent.Title,
                ["description"] = marketEvent.Description
            };

            foreach (var userId in userIds)
            {
                var preferences = await GetUserPreferencesAsync(userId);
                
                if (!preferences.EnableMarketAlerts)
                {
                    continue;
                }

                if (IsInQuietHours(preferences.QuietHours))
                {
                    await QueueNotificationAsync(userId, "market", marketEvent, DateTime.UtcNow.Add(GetTimeUntilQuietHoursEnd(preferences.QuietHours)));
                    continue;
                }

                await SendNotificationToUserAsync(userId, title, body, data, preferences.AlertMethods);
                await LogNotificationAsync(userId, "market", title, body, JsonSerializer.Serialize(data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending market alert to users");
        }
    }

    public async Task<bool> RegisterDeviceAsync(Guid userId, DeviceRegistration device)
    {
        try
        {
            var existingDevice = await _context.UserDevices
                .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.DeviceToken == device.Token);

            if (existingDevice != null)
            {
                // Update existing device
                existingDevice.DeviceType = device.Type;
                existingDevice.AppVersion = device.AppVersion;
                existingDevice.IsActive = true;
                existingDevice.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new device registration
                var userDevice = new UserDevice
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DeviceToken = device.Token,
                    DeviceType = device.Type,
                    AppVersion = device.AppVersion,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserDevices.Add(userDevice);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Device registered for user {UserId}: {DeviceType}", userId, device.Type);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnregisterDeviceAsync(Guid userId, string deviceToken)
    {
        try
        {
            var device = await _context.UserDevices
                .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.DeviceToken == deviceToken);

            if (device != null)
            {
                device.IsActive = false;
                device.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Device unregistered for user {UserId}", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device for user {UserId}", userId);
            return false;
        }
    }

    public async Task<NotificationPreferences> GetUserPreferencesAsync(Guid userId)
    {
        try
        {
            var userPrefs = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(unp => unp.UserId == userId);

            if (userPrefs != null)
            {
                return new NotificationPreferences
                {
                    EnableSignalAlerts = userPrefs.EnableSignalAlerts,
                    EnablePortfolioAlerts = userPrefs.EnablePortfolioAlerts,
                    EnableStrategyAlerts = userPrefs.EnableStrategyAlerts,
                    EnableMarketAlerts = userPrefs.EnableMarketAlerts,
                    MinSignalConfidence = userPrefs.MinSignalConfidence,
                    PortfolioChangeThreshold = userPrefs.PortfolioChangeThreshold,
                    AlertMethods = JsonSerializer.Deserialize<List<string>>(userPrefs.AlertMethods) ?? new(),
                    QuietHours = new QuietHours
                    {
                        Enabled = userPrefs.QuietHoursEnabled,
                        StartTime = userPrefs.QuietHoursStart,
                        EndTime = userPrefs.QuietHoursEnd,
                        Days = JsonSerializer.Deserialize<List<DayOfWeek>>(userPrefs.QuietHoursDays) ?? new()
                    }
                };
            }

            // Return default preferences
            return new NotificationPreferences();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for user {UserId}", userId);
            return new NotificationPreferences();
        }
    }

    public async Task UpdateUserPreferencesAsync(Guid userId, NotificationPreferences preferences)
    {
        try
        {
            var existingPrefs = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(unp => unp.UserId == userId);

            if (existingPrefs != null)
            {
                existingPrefs.EnableSignalAlerts = preferences.EnableSignalAlerts;
                existingPrefs.EnablePortfolioAlerts = preferences.EnablePortfolioAlerts;
                existingPrefs.EnableStrategyAlerts = preferences.EnableStrategyAlerts;
                existingPrefs.EnableMarketAlerts = preferences.EnableMarketAlerts;
                existingPrefs.MinSignalConfidence = preferences.MinSignalConfidence;
                existingPrefs.PortfolioChangeThreshold = preferences.PortfolioChangeThreshold;
                existingPrefs.AlertMethods = JsonSerializer.Serialize(preferences.AlertMethods);
                existingPrefs.QuietHoursEnabled = preferences.QuietHours.Enabled;
                existingPrefs.QuietHoursStart = preferences.QuietHours.StartTime;
                existingPrefs.QuietHoursEnd = preferences.QuietHours.EndTime;
                existingPrefs.QuietHoursDays = JsonSerializer.Serialize(preferences.QuietHours.Days);
                existingPrefs.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newPrefs = new UserNotificationPreferences
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EnableSignalAlerts = preferences.EnableSignalAlerts,
                    EnablePortfolioAlerts = preferences.EnablePortfolioAlerts,
                    EnableStrategyAlerts = preferences.EnableStrategyAlerts,
                    EnableMarketAlerts = preferences.EnableMarketAlerts,
                    MinSignalConfidence = preferences.MinSignalConfidence,
                    PortfolioChangeThreshold = preferences.PortfolioChangeThreshold,
                    AlertMethods = JsonSerializer.Serialize(preferences.AlertMethods),
                    QuietHoursEnabled = preferences.QuietHours.Enabled,
                    QuietHoursStart = preferences.QuietHours.StartTime,
                    QuietHoursEnd = preferences.QuietHours.EndTime,
                    QuietHoursDays = JsonSerializer.Serialize(preferences.QuietHours.Days),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserNotificationPreferences.Add(newPrefs);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification preferences updated for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<NotificationHistory>> GetNotificationHistoryAsync(Guid userId, int limit = 50)
    {
        try
        {
            return await _context.NotificationHistory
                .Where(nh => nh.UserId == userId)
                .OrderByDescending(nh => nh.SentAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history for user {UserId}", userId);
            return new List<NotificationHistory>();
        }
    }

    // Private helper methods
    private async Task SendNotificationToUserAsync(Guid userId, string title, string body, Dictionary<string, string> data, List<string> alertMethods)
    {
        var tasks = new List<Task>();

        if (alertMethods.Contains("push"))
        {
            tasks.Add(SendPushNotificationAsync(userId, title, body, data));
        }

        if (alertMethods.Contains("email"))
        {
            tasks.Add(SendEmailNotificationAsync(userId, title, body));
        }

        if (alertMethods.Contains("sms"))
        {
            tasks.Add(SendSMSNotificationAsync(userId, body));
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendPushNotificationAsync(Guid userId, string title, string body, Dictionary<string, string> data)
    {
        try
        {
            if (string.IsNullOrEmpty(_fcmServerKey))
            {
                _logger.LogWarning("FCM server key not configured, skipping push notification");
                return;
            }

            var devices = await _context.UserDevices
                .Where(ud => ud.UserId == userId && ud.IsActive)
                .ToListAsync();

            foreach (var device in devices)
            {
                var payload = new
                {
                    to = device.DeviceToken,
                    notification = new
                    {
                        title = title,
                        body = body,
                        sound = "default"
                    },
                    data = data
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"key={_fcmServerKey}");

                var response = await _httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Push notification sent to device {DeviceToken}", device.DeviceToken);
                }
                else
                {
                    _logger.LogWarning("Failed to send push notification to device {DeviceToken}: {StatusCode}", 
                        device.DeviceToken, response.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
        }
    }

    private async Task SendEmailNotificationAsync(Guid userId, string title, string body)
    {
        // Email implementation would go here - integrate with SendGrid, SES, etc.
        _logger.LogInformation("Email notification would be sent to user {UserId}: {Title}", userId, title);
        await Task.CompletedTask;
    }

    private async Task SendSMSNotificationAsync(Guid userId, string body)
    {
        // SMS implementation would go here - integrate with Twilio, etc.
        _logger.LogInformation("SMS notification would be sent to user {UserId}: {Body}", userId, body);
        await Task.CompletedTask;
    }

    private async Task LogNotificationAsync(Guid userId, string type, string title, string body, string? data = null)
    {
        try
        {
            var notification = new NotificationHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                Data = data,
                SentAt = DateTime.UtcNow
            };

            _context.NotificationHistory.Add(notification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging notification for user {UserId}", userId);
        }
    }

    private async Task QueueNotificationAsync(Guid userId, string type, object notificationData, DateTime sendAt)
    {
        // Queue notification for later delivery - would implement with background job service
        _logger.LogInformation("Notification queued for user {UserId} to be sent at {SendAt}", userId, sendAt);
        await Task.CompletedTask;
    }

    private bool IsInQuietHours(QuietHours quietHours)
    {
        if (!quietHours.Enabled)
            return false;

        var now = DateTime.Now;
        var currentDay = now.DayOfWeek;
        
        if (quietHours.Days.Contains(currentDay))
        {
            var currentTime = now.TimeOfDay;
            
            // Handle quiet hours that span midnight
            if (quietHours.StartTime > quietHours.EndTime)
            {
                return currentTime >= quietHours.StartTime || currentTime <= quietHours.EndTime;
            }
            else
            {
                return currentTime >= quietHours.StartTime && currentTime <= quietHours.EndTime;
            }
        }

        return false;
    }

    private TimeSpan GetTimeUntilQuietHoursEnd(QuietHours quietHours)
    {
        var now = DateTime.Now;
        var endTime = now.Date.Add(quietHours.EndTime);
        
        // If end time is earlier than start time, it's next day
        if (quietHours.EndTime < quietHours.StartTime)
        {
            endTime = endTime.AddDays(1);
        }
        
        return endTime - now;
    }

    // Price Alert methods
    public async Task<PriceAlert> CreatePriceAlertAsync(Guid userId, string symbol, string alertType, decimal targetPrice, decimal? percentageChange = null, string? message = null)
    {
        var alert = new PriceAlert
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = symbol.ToUpper(),
            AlertType = alertType.ToUpper(),
            TargetPrice = targetPrice,
            PercentageChange = percentageChange,
            Message = message,
            IsActive = true,
            IsTriggered = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.PriceAlerts.Add(alert);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created price alert {AlertId} for user {UserId}: {Symbol} {AlertType} {TargetPrice}", 
            alert.Id, userId, symbol, alertType, targetPrice);

        return alert;
    }

    public async Task<List<PriceAlert>> GetUserPriceAlertsAsync(Guid userId, bool activeOnly = true)
    {
        var query = _context.PriceAlerts.Where(a => a.UserId == userId);
        
        if (activeOnly)
        {
            query = query.Where(a => a.IsActive && !a.IsTriggered);
        }

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<PriceAlert?> GetPriceAlertAsync(Guid alertId)
    {
        return await _context.PriceAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
    }

    public async Task<bool> UpdatePriceAlertAsync(Guid alertId, bool isActive)
    {
        var alert = await _context.PriceAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
        if (alert == null) return false;

        alert.IsActive = isActive;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeletePriceAlertAsync(Guid alertId)
    {
        var alert = await _context.PriceAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
        if (alert == null) return false;

        _context.PriceAlerts.Remove(alert);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task CheckPriceAlertsAsync(string symbol, decimal currentPrice)
    {
        var alerts = await _context.PriceAlerts
            .Where(a => a.Symbol == symbol.ToUpper() && a.IsActive && !a.IsTriggered)
            .ToListAsync();

        foreach (var alert in alerts)
        {
            bool shouldTrigger = alert.AlertType.ToUpper() switch
            {
                "PRICE_ABOVE" => currentPrice >= alert.TargetPrice,
                "PRICE_BELOW" => currentPrice <= alert.TargetPrice,
                "PRICE_CHANGE" => alert.PercentageChange.HasValue && 
                                Math.Abs((currentPrice - alert.TargetPrice) / alert.TargetPrice * 100) >= Math.Abs(alert.PercentageChange.Value),
                _ => false
            };

            if (shouldTrigger)
            {
                alert.IsTriggered = true;
                alert.TriggeredAt = DateTimeOffset.UtcNow;
                alert.TriggeredPrice = currentPrice;

                _logger.LogInformation("Triggered price alert {AlertId} for {Symbol} at price {Price}", 
                    alert.Id, symbol, currentPrice);
            }
        }

        if (alerts.Any(a => a.IsTriggered))
        {
            await _context.SaveChangesAsync();
        }
    }
}

// Supporting models and DTOs
public class DeviceRegistration
{
    public string Token { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // ios, android, web
    public string AppVersion { get; set; } = string.Empty;
}

