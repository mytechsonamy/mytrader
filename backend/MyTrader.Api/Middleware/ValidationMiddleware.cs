using MyTrader.Core.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MyTrader.Api.Middleware;

/// <summary>
/// Middleware for handling validation errors and providing consistent error responses
/// </summary>
public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationMiddleware> _logger;

    public ValidationMiddleware(RequestDelegate next, ILogger<ValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error: {Message}", ex.Message);
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argument error: {Message}", ex.Message);
            await HandleArgumentExceptionAsync(context, ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            await HandleInvalidOperationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";

        var response = new ValidationErrorResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = new Dictionary<string, List<string>>
            {
                ["General"] = new List<string> { ex.Message }
            }
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static async Task HandleArgumentExceptionAsync(HttpContext context, ArgumentException ex)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.ErrorResult(ex.Message, 400);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static async Task HandleInvalidOperationExceptionAsync(HttpContext context, InvalidOperationException ex)
    {
        context.Response.StatusCode = 409;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.ErrorResult(ex.Message, 409);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static async Task HandleGenericExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.ErrorResult(
            "An internal server error occurred. Please try again later.", 500);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension method to register the validation middleware
/// </summary>
public static class ValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseValidationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ValidationMiddleware>();
    }
}