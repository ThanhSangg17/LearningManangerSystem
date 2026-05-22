namespace PRN232.LearningManagerSystem.Services.Models.Common;

/// <summary>
/// Pagination metadata used internally in the Services layer.
/// Kept separate from PaginationMetadata in the API layer to avoid cross-layer dependencies.
/// </summary>
public class ServicePaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
