namespace MyTrader.Core.DTOs;

/// <summary>
/// Standardized error response for API endpoints
/// Designed for mobile-friendly error handling
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Unique error code for client-side handling
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly error message (localized if possible)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error message for debugging (only in development)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Correlation ID for tracking errors across services
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Suggested action for the user (optional)
    /// </summary>
    public string? SuggestedAction { get; set; }

    /// <summary>
    /// Whether the operation can be retried
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Additional metadata for client-side handling
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Error severity levels
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational - user should be aware but no action needed
    /// </summary>
    Info,

    /// <summary>
    /// Warning - operation completed but with issues
    /// </summary>
    Warning,

    /// <summary>
    /// Error - operation failed but system is stable
    /// </summary>
    Error,

    /// <summary>
    /// Critical - system stability affected
    /// </summary>
    Critical
}

/// <summary>
/// Common error codes for consistent client-side handling
/// </summary>
public static class ErrorCodes
{
    // Authentication & Authorization
    public const string Unauthorized = "AUTH_UNAUTHORIZED";
    public const string Forbidden = "AUTH_FORBIDDEN";
    public const string TokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";

    // Validation
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidInput = "VALIDATION_INVALID_INPUT";
    public const string MissingRequired = "VALIDATION_MISSING_REQUIRED";

    // Resource
    public const string NotFound = "RESOURCE_NOT_FOUND";
    public const string AlreadyExists = "RESOURCE_ALREADY_EXISTS";
    public const string Conflict = "RESOURCE_CONFLICT";

    // Network & Connectivity
    public const string NetworkError = "NETWORK_ERROR";
    public const string Timeout = "NETWORK_TIMEOUT";
    public const string ServiceUnavailable = "NETWORK_SERVICE_UNAVAILABLE";

    // Data
    public const string DatabaseError = "DATA_DATABASE_ERROR";
    public const string DataCorrupted = "DATA_CORRUPTED";

    // Business Logic
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string InsufficientFunds = "BUSINESS_INSUFFICIENT_FUNDS";
    public const string MarketClosed = "BUSINESS_MARKET_CLOSED";

    // Rate Limiting
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string TooManyRequests = "RATE_LIMIT_TOO_MANY_REQUESTS";

    // System
    public const string InternalError = "SYSTEM_INTERNAL_ERROR";
    public const string MaintenanceMode = "SYSTEM_MAINTENANCE";
    public const string FeatureDisabled = "SYSTEM_FEATURE_DISABLED";
}

/// <summary>
/// User-friendly error messages
/// </summary>
public static class ErrorMessages
{
    // Authentication
    public const string Unauthorized = "Please log in to continue";
    public const string TokenExpired = "Your session has expired. Please log in again";
    public const string InvalidCredentials = "Invalid email or password";

    // Network
    public const string NetworkError = "Unable to connect. Please check your internet connection";
    public const string Timeout = "Request timed out. Please try again";
    public const string ServiceUnavailable = "Service is temporarily unavailable. Please try again later";

    // Generic
    public const string InternalError = "Something went wrong. Please try again";
    public const string NotFound = "The requested resource was not found";
    public const string ValidationFailed = "Please check your input and try again";

    // Market
    public const string MarketClosed = "Market is currently closed";
    public const string InsufficientFunds = "Insufficient funds for this operation";

    // Rate Limiting
    public const string RateLimitExceeded = "Too many requests. Please wait a moment and try again";
}

/// <summary>
/// Suggested actions for users
/// </summary>
public static class SuggestedActions
{
    public const string Retry = "Please try again";
    public const string Login = "Please log in";
    public const string CheckConnection = "Check your internet connection";
    public const string ContactSupport = "Contact support if the problem persists";
    public const string WaitAndRetry = "Wait a moment and try again";
    public const string RefreshPage = "Refresh the page";
    public const string UpdateApp = "Update to the latest version";
}
