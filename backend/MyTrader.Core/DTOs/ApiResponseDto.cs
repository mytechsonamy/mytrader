namespace MyTrader.Core.DTOs;

/// <summary>
/// Generic API response wrapper for consistent response format
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? StatusCode { get; set; }
    public string? RequestId { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResult(string error, int? statusCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = new List<string> { error },
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> ErrorResult(List<string> errors, int? statusCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors,
            StatusCode = statusCode
        };
    }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class PaginatedResponse<T>
{
    public bool Success { get; set; }
    public List<T> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static PaginatedResponse<T> SuccessResult(
        List<T> data,
        int page,
        int pageSize,
        int totalCount,
        string? message = null)
    {
        return new PaginatedResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            }
        };
    }

    public static PaginatedResponse<T> ErrorResult(string error)
    {
        return new PaginatedResponse<T>
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Pagination request parameters
/// </summary>
public class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 50;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > 1000 => 1000,
            _ => value
        };
    }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// Base API request with pagination and filtering
/// </summary>
public class BaseListRequest : PaginationRequest
{
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc"; // asc or desc
    public List<string> Filters { get; set; } = new();
    public string? Language { get; set; } = "en"; // en or tr
}

/// <summary>
/// Validation error response
/// </summary>
public class ValidationErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "Validation failed";
    public Dictionary<string, List<string>> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health check response
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

/// <summary>
/// Component health status
/// </summary>
public class ComponentHealth
{
    public string Status { get; set; } = string.Empty; // Healthy, Degraded, Unhealthy
    public string? Description { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}