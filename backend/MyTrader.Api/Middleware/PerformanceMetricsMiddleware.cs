using System.Diagnostics;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.Middleware;

/// <summary>
/// Middleware for collecting API request performance metrics
/// </summary>
public class PerformanceMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMetricsMiddleware> _logger;

    public PerformanceMetricsMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPerformanceMetricsService metricsService)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            try
            {
                // Record API request metrics
                var endpoint = context.Request.Path.Value ?? "/";
                var method = context.Request.Method;
                var statusCode = context.Response.StatusCode;
                var duration = stopwatch.ElapsedMilliseconds;

                metricsService.RecordApiRequest(endpoint, method, statusCode, duration);

                // Log slow requests
                if (duration > 1000) // > 1 second
                {
                    _logger.LogWarning(
                        "Slow API request: {Method} {Path} took {DurationMs}ms, Status: {StatusCode}",
                        method, endpoint, duration, statusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording API metrics");
            }
        }
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class PerformanceMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceMetricsMiddleware>();
    }
}
