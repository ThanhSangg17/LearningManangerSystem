namespace PRN232.LearningManagerSystem.Services.Models.Common;

public class PagedResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public object? Errors { get; set; }
    public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();

    public static PagedResponse<T> SuccessResponse(T data, PaginationMetadata pagination, string message = "Request processed successfully")
    {
        return new PagedResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = null,
            Pagination = pagination
        };
    }

    public static PagedResponse<T> ErrorResponse(string message, object? errors = null)
    {
        return new PagedResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors,
            Pagination = new PaginationMetadata()
        };
    }
}
