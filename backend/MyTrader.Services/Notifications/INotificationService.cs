using MyTrader.Core.Models;

namespace MyTrader.Services.Notifications;

public interface INotificationService
{
    // Price Alerts
    Task<PriceAlert> CreatePriceAlertAsync(Guid userId, string symbol, string alertType, decimal targetPrice, decimal? percentageChange = null, string? message = null);
    Task<List<PriceAlert>> GetUserPriceAlertsAsync(Guid userId, bool activeOnly = true);
    Task<PriceAlert?> GetPriceAlertAsync(Guid alertId);
    Task<bool> UpdatePriceAlertAsync(Guid alertId, bool isActive);
    Task<bool> DeletePriceAlertAsync(Guid alertId);
    Task CheckPriceAlertsAsync(string symbol, decimal currentPrice);

    // Notifications
    Task<NotificationHistory> SendNotificationAsync(Guid userId, string type, string title, string message, string channel = "push", string? data = null);
    Task<List<NotificationHistory>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, int limit = 50);
    Task<bool> MarkNotificationAsReadAsync(Guid notificationId);
    Task<bool> MarkAllNotificationsAsReadAsync(Guid userId);
    Task<int> GetUnreadNotificationCountAsync(Guid userId);

    // System notifications
    Task SendAchievementNotificationAsync(Guid userId, UserAchievement achievement);
    Task SendPriceAlertNotificationAsync(Guid userId, PriceAlert alert, decimal triggeredPrice);
    Task SendStrategySignalNotificationAsync(Guid userId, string symbol, string signal, decimal price);
}