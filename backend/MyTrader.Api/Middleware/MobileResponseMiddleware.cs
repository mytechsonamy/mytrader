using Microsoft.AspNetCore.Http;
using MyTrader.Core.DTOs;
using System.Text.Json;

namespace MyTrader.Api.Middleware;

/// <summary>
/// Middleware to auto-unwrap ApiResponse<T> for mobile clients while maintaining compatibility with web clients
/// </summary>
public class MobileResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MobileResponseMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MobileResponseMiddleware(RequestDelegate next, ILogger<MobileResponseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is a mobile request by looking for mobile client indicators
        var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();
        var isMobileClient = IsMobileClient(context);

        if (!isMobileClient)
        {
            // Not a mobile client, proceed normally
            await _next(context);
            return;
        }

        // Intercept response for mobile clients
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Only process successful JSON responses
        if (context.Response.StatusCode == 200 &&
            context.Response.ContentType?.Contains("application/json") == true)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();

            try
            {
                var unwrappedResponse = UnwrapApiResponse(responseText);
                var unwrappedBytes = System.Text.Encoding.UTF8.GetBytes(unwrappedResponse);

                context.Response.ContentLength = unwrappedBytes.Length;
                context.Response.Body = originalBodyStream;
                await context.Response.Body.WriteAsync(unwrappedBytes);

                _logger.LogDebug("Unwrapped ApiResponse for mobile client on {Path}", context.Request.Path);
            }
            catch (Exception ex)
            {
                // If unwrapping fails, return original response
                _logger.LogWarning(ex, "Failed to unwrap response for mobile client, returning original");
                responseBody.Seek(0, SeekOrigin.Begin);
                context.Response.Body = originalBodyStream;
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        else
        {
            // Non-JSON or error response, copy as-is
            responseBody.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBodyStream;
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private static bool IsMobileClient(HttpContext context)
    {
        // Check for mobile client indicators
        var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();
        var clientType = context.Request.Headers["X-Client-Type"].ToString().ToLower();
        var apiVersion = context.Request.Headers["X-API-Version"].ToString();

        // Mobile client identification strategies:
        // 1. Check for explicit mobile client header
        if (clientType.Contains("mobile") || clientType.Contains("react-native"))
            return true;

        // 2. Check User-Agent for React Native or mobile app indicators
        if (userAgent.Contains("react-native") ||
            userAgent.Contains("mytrader-mobile") ||
            userAgent.Contains("expo"))
            return true;

        // 3. Check for mobile-specific API version paths
        if (context.Request.Path.StartsWithSegments("/api/v1"))
        {
            // Most v1 API calls from mobile clients expect unwrapped responses
            // But exclude specific endpoints that should keep wrapped format
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Keep wrapped format for these endpoints (used by web client)
            if (path.Contains("/providers/health") ||
                path.Contains("/subscribe") ||
                path.Contains("/unsubscribe"))
                return false;

            return true;
        }

        return false;
    }

    private string UnwrapApiResponse(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            // Check if this looks like an ApiResponse<T> structure
            if (root.TryGetProperty("success", out _) &&
                root.TryGetProperty("data", out var dataProperty))
            {
                // This is an ApiResponse<T>, extract the data
                if (root.TryGetProperty("success", out var successProperty) &&
                    successProperty.GetBoolean())
                {
                    // Success response - return the data part
                    return dataProperty.GetRawText();
                }
                else
                {
                    // Error response - transform to mobile-friendly format
                    var errors = root.TryGetProperty("errors", out var errorsProperty)
                        ? errorsProperty.EnumerateArray().Select(e => e.GetString()).ToArray()
                        : new[] { "Unknown error" };

                    var errorResponse = new
                    {
                        error = true,
                        message = root.TryGetProperty("message", out var messageProperty)
                            ? messageProperty.GetString()
                            : "Request failed",
                        errors = errors
                    };

                    return JsonSerializer.Serialize(errorResponse, _jsonOptions);
                }
            }

            // Not an ApiResponse<T> structure, return as-is
            return responseJson;
        }
        catch (JsonException)
        {
            // Not valid JSON or structure we can't parse, return as-is
            return responseJson;
        }
    }
}

/// <summary>
/// Extension method to register the mobile response middleware
/// </summary>
public static class MobileResponseMiddlewareExtensions
{
    public static IApplicationBuilder UseMobileResponseUnwrapping(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MobileResponseMiddleware>();
    }
}