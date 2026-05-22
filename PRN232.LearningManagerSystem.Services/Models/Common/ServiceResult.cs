namespace PRN232.LearningManagerSystem.Services.Models.Common;

/// <summary>
/// Carries the outcome of a service operation back to the controller.
/// The controller is responsible for translating this into ApiResponse and an HTTP status code.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public object? Errors { get; set; }

    /// <summary>
    /// Intended HTTP status code. The controller uses this value — not string matching — to decide the response.
    /// </summary>
    public int StatusCode { get; set; }

    // ---- Factory helpers ----

    public static ServiceResult<T> Ok(T data, string message = "Request processed successfully")
        => new() { Success = true,  Message = message, Data = data,    Errors = null,   StatusCode = 200 };

    public static ServiceResult<T> Created(T data, string message = "Resource created successfully")
        => new() { Success = true,  Message = message, Data = data,    Errors = null,   StatusCode = 201 };

    public static ServiceResult<T> BadRequest(string message, object? errors = null)
        => new() { Success = false, Message = message, Data = default, Errors = errors, StatusCode = 400 };

    public static ServiceResult<T> NotFound(string message)
        => new() { Success = false, Message = message, Data = default, Errors = null,   StatusCode = 404 };

    public static ServiceResult<T> ServerError(string message, object? errors = null)
        => new() { Success = false, Message = message, Data = default, Errors = errors, StatusCode = 500 };
}

/// <summary>
/// Like ServiceResult, but also carries pagination metadata for list endpoints.
/// Uses ServicePaginationMetadata to avoid dependency on API-layer models.
/// </summary>
public class ServicePagedResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public object? Errors { get; set; }
    public ServicePaginationMetadata Pagination { get; set; } = new();
    public int StatusCode { get; set; }

    // ---- Factory helpers ----

    public static ServicePagedResult<T> Ok(T data, ServicePaginationMetadata pagination, string message = "Request processed successfully")
        => new() { Success = true,  Message = message, Data = data,    Errors = null,   Pagination = pagination, StatusCode = 200 };

    public static ServicePagedResult<T> BadRequest(string message, object? errors = null)
        => new() { Success = false, Message = message, Data = default, Errors = errors, Pagination = new(),       StatusCode = 400 };

    public static ServicePagedResult<T> ServerError(string message, object? errors = null)
        => new() { Success = false, Message = message, Data = default, Errors = errors, Pagination = new(),       StatusCode = 500 };
}
