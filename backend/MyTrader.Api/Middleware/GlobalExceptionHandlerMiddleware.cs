using System.Net;
using System.Text.Json;
using MyTrader.Core.DTOs;

namespace MyTrader.Api.Middleware;

/// <summary>
/// Global exception handler middleware for consistent error responses
/// Provides mobile-friendly error messages
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        
        // Log the exception with correlation ID
        _logger.LogError(exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId, context.Request.Path, context.Request.Method);

        var errorResponse = CreateErrorResponse(exception, correlationId);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = errorResponse.StatusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(errorResponse, options);
        await context.Response.WriteAsync(json);
    }

    private ErrorResponse CreateErrorResponse(Exception exception, string correlationId)
    {
        var errorResponse = new ErrorResponse
        {
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.ErrorCode = ErrorCodes.Unauthorized;
                errorResponse.Message = ErrorMessages.Unauthorized;
                errorResponse.SuggestedAction = SuggestedActions.Login;
                errorResponse.IsRetryable = false;
                break;

            case ArgumentNullException or ArgumentException:
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.ErrorCode = ErrorCodes.InvalidInput;
                errorResponse.Message = ErrorMessages.ValidationFailed;
                errorResponse.SuggestedAction = SuggestedActions.Retry;
                errorResponse.IsRetryable = false;
                break;

            case KeyNotFoundException:
                errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.ErrorCode = ErrorCodes.NotFound;
                errorResponse.Message = ErrorMessages.NotFound;
                errorResponse.SuggestedAction = SuggestedActions.RefreshPage;
                errorResponse.IsRetryable = false;
                break;

            case TimeoutException:
                errorResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.ErrorCode = ErrorCodes.Timeout;
                errorResponse.Message = ErrorMessages.Timeout;
                errorResponse.SuggestedAction = SuggestedActions.Retry;
                errorResponse.IsRetryable = true;
                break;

            case InvalidOperationException when exception.Message.Contains("database"):
            case Npgsql.NpgsqlException:
                errorResponse.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                errorResponse.ErrorCode = ErrorCodes.DatabaseError;
                errorResponse.Message = ErrorMessages.ServiceUnavailable;
                errorResponse.SuggestedAction = SuggestedActions.WaitAndRetry;
                errorResponse.IsRetryable = true;
                break;

            case HttpRequestException:
                errorResponse.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                errorResponse.ErrorCode = ErrorCodes.NetworkError;
                errorResponse.Message = ErrorMessages.NetworkError;
                errorResponse.SuggestedAction = SuggestedActions.CheckConnection;
                errorResponse.IsRetryable = true;
                break;

            default:
                errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.ErrorCode = ErrorCodes.InternalError;
                errorResponse.Message = ErrorMessages.InternalError;
                errorResponse.SuggestedAction = SuggestedActions.ContactSupport;
                errorResponse.IsRetryable = true;
                break;
        }

        // Include details only in development
        if (_environment.IsDevelopment())
        {
            errorResponse.Details = exception.ToString();
        }

        return errorResponse;
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
