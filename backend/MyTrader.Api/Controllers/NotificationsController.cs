using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Notifications;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Tags("Notifications & Alerts")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Price Alerts
    [HttpPost("price-alerts")]
    public async Task<ActionResult> CreatePriceAlert([FromBody] CreatePriceAlertRequest request)
    {
        try
        {
            var userId = GetUserId();
            var alert = await _notificationService.CreatePriceAlertAsync(
                userId,
                request.Symbol,
                request.AlertType,
                request.TargetPrice,
                request.PercentageChange,
                request.Message
            );

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = alert.Id,
                    symbol = alert.Symbol,
                    alert_type = alert.AlertType,
                    target_price = alert.TargetPrice,
                    percentage_change = alert.PercentageChange,
                    message = alert.Message,
                    is_active = alert.IsActive,
                    created_at = alert.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to create price alert" });
        }
    }

    [HttpGet("price-alerts")]
    public async Task<ActionResult> GetPriceAlerts([FromQuery] bool activeOnly = true)
    {
        try
        {
            var userId = GetUserId();
            var alerts = await _notificationService.GetUserPriceAlertsAsync(userId, activeOnly);

            return Ok(new
            {
                success = true,
                data = alerts.Select(a => new
                {
                    id = a.Id,
                    symbol = a.Symbol,
                    alert_type = a.AlertType,
                    target_price = a.TargetPrice,
                    percentage_change = a.PercentageChange,
                    message = a.Message,
                    is_active = a.IsActive,
                    is_triggered = a.IsTriggered,
                    triggered_at = a.TriggeredAt,
                    triggered_price = a.TriggeredPrice,
                    created_at = a.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get price alerts" });
        }
    }

    [HttpPut("price-alerts/{alertId}")]
    public async Task<ActionResult> UpdatePriceAlert(string alertId, [FromBody] UpdatePriceAlertRequest request)
    {
        try
        {
            if (!Guid.TryParse(alertId, out var alertGuid))
            {
                return BadRequest(new { success = false, message = "Invalid alert ID" });
            }

            var success = await _notificationService.UpdatePriceAlertAsync(alertGuid, request.IsActive);
            if (!success)
            {
                return NotFound(new { success = false, message = "Price alert not found" });
            }

            return Ok(new { success = true, message = "Price alert updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to update price alert" });
        }
    }

    [HttpDelete("price-alerts/{alertId}")]
    public async Task<ActionResult> DeletePriceAlert(string alertId)
    {
        try
        {
            if (!Guid.TryParse(alertId, out var alertGuid))
            {
                return BadRequest(new { success = false, message = "Invalid alert ID" });
            }

            var success = await _notificationService.DeletePriceAlertAsync(alertGuid);
            if (!success)
            {
                return NotFound(new { success = false, message = "Price alert not found" });
            }

            return Ok(new { success = true, message = "Price alert deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to delete price alert" });
        }
    }

    // Notifications
    [HttpGet("history")]
    public async Task<ActionResult> GetNotificationHistory([FromQuery] bool unreadOnly = false, [FromQuery] int limit = 50)
    {
        try
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly, limit);

            return Ok(new
            {
                success = true,
                data = notifications.Select(n => new
                {
                    id = n.Id,
                    type = n.Type,
                    channel = n.Channel,
                    title = n.Title,
                    message = n.Message,
                    data = n.Data,
                    status = n.Status,
                    created_at = n.CreatedAt,
                    read_at = n.ReadAt
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get notification history" });
        }
    }

    [HttpPost("mark-read/{notificationId}")]
    public async Task<ActionResult> MarkNotificationAsRead(string notificationId)
    {
        try
        {
            if (!Guid.TryParse(notificationId, out var notificationGuid))
            {
                return BadRequest(new { success = false, message = "Invalid notification ID" });
            }

            var success = await _notificationService.MarkNotificationAsReadAsync(notificationGuid);
            if (!success)
            {
                return NotFound(new { success = false, message = "Notification not found" });
            }

            return Ok(new { success = true, message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to mark notification as read" });
        }
    }

    [HttpPost("mark-all-read")]
    public async Task<ActionResult> MarkAllNotificationsAsRead()
    {
        try
        {
            var userId = GetUserId();
            await _notificationService.MarkAllNotificationsAsReadAsync(userId);

            return Ok(new { success = true, message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to mark all notifications as read" });
        }
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult> GetUnreadNotificationCount()
    {
        try
        {
            var userId = GetUserId();
            var count = await _notificationService.GetUnreadNotificationCountAsync(userId);

            return Ok(new
            {
                success = true,
                data = new { unread_count = count }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get unread notification count" });
        }
    }
}

public class CreatePriceAlertRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty; // PRICE_ABOVE, PRICE_BELOW, PRICE_CHANGE
    public decimal TargetPrice { get; set; }
    public decimal? PercentageChange { get; set; }
    public string? Message { get; set; }
}

public class UpdatePriceAlertRequest
{
    public bool IsActive { get; set; }
}