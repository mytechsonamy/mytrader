using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs.Queue;
using MyTrader.Core.Services;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/queue")]
[Authorize]
[Tags("Backtest Queue Management")]
public class QueueController : ControllerBase
{
    private readonly IBacktestQueueService _queueService;
    private readonly BacktestQueueProcessor _queueProcessor;
    private readonly ILogger<QueueController> _logger;

    public QueueController(
        IBacktestQueueService queueService,
        BacktestQueueProcessor queueProcessor,
        ILogger<QueueController> logger)
    {
        _queueService = queueService;
        _queueProcessor = queueProcessor;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Queue a backtest for execution
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BacktestQueueResponse>> QueueBacktest([FromBody] QueueBacktestRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _queueService.EnqueueAsync(userId, request);
            
            _logger.LogInformation("User {UserId} queued backtest {QueueId}", userId, response.Id);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing backtest");
            return BadRequest(new { error = "Failed to queue backtest", message = ex.Message });
        }
    }

    /// <summary>
    /// Get queue item details by ID
    /// </summary>
    [HttpGet("{queueId}")]
    public async Task<ActionResult<BacktestQueueResponse>> GetQueueItem(Guid queueId)
    {
        try
        {
            var response = await _queueService.GetQueueItemAsync(queueId);
            
            // Check if user owns this queue item or is admin
            if (response.UserId != GetUserId() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue item {QueueId}", queueId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get current user's queue with optional filtering
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<BacktestQueueResponse>>> GetMyQueue([FromQuery] QueueFilterRequest filter)
    {
        try
        {
            var userId = GetUserId();
            var response = await _queueService.GetUserQueueAsync(userId, filter);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user queue for user {UserId}", GetUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get queue statistics and system status
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<QueueStatsResponse>> GetQueueStats()
    {
        try
        {
            var stats = await _queueService.GetQueueStatsAsync();
            
            // Add processor stats
            var processorStats = _queueProcessor.GetStats();
            var enhancedResourceUsage = stats.ResourceUsage with 
            {
                ActiveWorkers = processorStats.RunningTasks,
                MaxWorkers = processorStats.MaxConcurrentTasks
            };
            
            var enhancedStats = stats with { ResourceUsage = enhancedResourceUsage };
            
            return Ok(enhancedStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue stats");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancel a queued or running backtest
    /// </summary>
    [HttpPost("{queueId}/cancel")]
    public async Task<ActionResult> CancelBacktest(Guid queueId, [FromBody] CancelBacktestRequest? request = null)
    {
        try
        {
            // Verify ownership
            var queueItem = await _queueService.GetQueueItemAsync(queueId);
            if (queueItem.UserId != GetUserId() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            
            var success = await _queueService.CancelAsync(queueId, request?.Reason);
            if (!success)
            {
                return BadRequest(new { error = "Cannot cancel backtest in current state" });
            }
            
            _logger.LogInformation("User {UserId} cancelled backtest {QueueId}", GetUserId(), queueId);
            
            return Ok(new { message = "Backtest cancelled successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling backtest {QueueId}", queueId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Retry a failed backtest
    /// </summary>
    [HttpPost("{queueId}/retry")]
    public async Task<ActionResult> RetryBacktest(Guid queueId, [FromBody] RetryBacktestRequest? request = null)
    {
        try
        {
            // Verify ownership
            var queueItem = await _queueService.GetQueueItemAsync(queueId);
            if (queueItem.UserId != GetUserId() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            
            var success = await _queueService.RetryAsync(queueId, request?.NewPriority);
            if (!success)
            {
                return BadRequest(new { error = "Cannot retry backtest in current state" });
            }
            
            _logger.LogInformation("User {UserId} retried backtest {QueueId}", GetUserId(), queueId);
            
            return Ok(new { message = "Backtest queued for retry" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying backtest {QueueId}", queueId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update queue item priority
    /// </summary>
    [HttpPut("{queueId}/priority")]
    public async Task<ActionResult> UpdatePriority(Guid queueId, [FromBody] int priority)
    {
        try
        {
            // Verify ownership
            var queueItem = await _queueService.GetQueueItemAsync(queueId);
            if (queueItem.UserId != GetUserId() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            
            if (priority < 1 || priority > 100)
            {
                return BadRequest(new { error = "Priority must be between 1 and 100" });
            }
            
            var success = await _queueService.UpdatePriorityAsync(queueId, priority);
            if (!success)
            {
                return BadRequest(new { error = "Cannot update priority for backtest in current state" });
            }
            
            return Ok(new { message = "Priority updated successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating priority for backtest {QueueId}", queueId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Reschedule a queued backtest
    /// </summary>
    [HttpPut("{queueId}/schedule")]
    public async Task<ActionResult> RescheduleBacktest(Guid queueId, [FromBody] DateTime scheduledFor)
    {
        try
        {
            // Verify ownership
            var queueItem = await _queueService.GetQueueItemAsync(queueId);
            if (queueItem.UserId != GetUserId() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            
            if (scheduledFor < DateTime.UtcNow)
            {
                return BadRequest(new { error = "Cannot schedule backtest in the past" });
            }
            
            var success = await _queueService.RescheduleAsync(queueId, scheduledFor);
            if (!success)
            {
                return BadRequest(new { error = "Cannot reschedule backtest in current state" });
            }
            
            return Ok(new { message = "Backtest rescheduled successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rescheduling backtest {QueueId}", queueId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Perform bulk operations on multiple queue items
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult> BulkOperation([FromBody] BulkQueueOperation operation)
    {
        try
        {
            var userId = GetUserId();
            var isAdmin = User.IsInRole("Admin");
            
            // Verify ownership for all items if not admin
            if (!isAdmin)
            {
                foreach (var queueId in operation.QueueIds)
                {
                    var item = await _queueService.GetQueueItemAsync(queueId);
                    if (item.UserId != userId)
                    {
                        return Forbid($"Access denied for queue item {queueId}");
                    }
                }
            }
            
            var affectedCount = await _queueService.BulkOperationAsync(operation);
            
            _logger.LogInformation("User {UserId} performed bulk operation {Operation} on {Count} items, {Affected} affected", 
                userId, operation.Operation, operation.QueueIds.Count, affectedCount);
            
            return Ok(new { 
                message = $"Bulk {operation.Operation} completed",
                requestedItems = operation.QueueIds.Count,
                affectedItems = affectedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get queue history for current user
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<BacktestQueueResponse>>> GetQueueHistory([FromQuery] int days = 7)
    {
        try
        {
            if (days < 1 || days > 90)
            {
                return BadRequest(new { error = "Days must be between 1 and 90" });
            }
            
            var userId = GetUserId();
            var history = await _queueService.GetQueueHistoryAsync(userId, days);
            
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue history for user {UserId}", GetUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get queue analytics (admin only)
    /// </summary>
    [HttpGet("analytics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Dictionary<string, object>>> GetQueueAnalytics()
    {
        try
        {
            var analytics = await _queueService.GetQueueAnalyticsAsync();
            
            // Add processor analytics
            var processorStats = _queueProcessor.GetStats();
            analytics["processorStats"] = processorStats;
            
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all queue items (admin only)
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<BacktestQueueResponse>>> GetAllQueueItems([FromQuery] QueueFilterRequest filter)
    {
        try
        {
            // For admin, we can query all users by not specifying userId
            var response = await _queueService.GetUserQueueAsync(Guid.Empty, filter);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all queue items");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Force cleanup of old completed jobs (admin only)
    /// </summary>
    [HttpPost("admin/cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ForceCleanup([FromQuery] int daysToKeep = 30)
    {
        try
        {
            if (daysToKeep < 1 || daysToKeep > 365)
            {
                return BadRequest(new { error = "Days to keep must be between 1 and 365" });
            }
            
            await _queueService.CleanupCompletedJobsAsync(daysToKeep);
            
            _logger.LogInformation("Admin {UserId} forced queue cleanup", GetUserId());
            
            return Ok(new { message = "Queue cleanup completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forced queue cleanup");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}