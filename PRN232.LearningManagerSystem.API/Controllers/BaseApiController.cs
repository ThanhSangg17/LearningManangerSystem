using Microsoft.AspNetCore.Mvc;
using PRN232.LearningManagerSystem.API.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.API.Controllers;

/// <summary>
/// Base controller providing shared helpers that convert ServiceResult / ServicePagedResult
/// into proper ApiResponse / PagedResponse with the correct HTTP status code.
/// Controllers are responsible for instantiating ApiResponse and PagedResponse.
/// Services must NOT return ApiResponse or PagedResponse.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Converts a <see cref="ServiceResult{T}"/> into an <see cref="IActionResult"/> with the
    /// correct HTTP status code and an <see cref="ApiResponse{T}"/> body.
    /// Use this overload when no mapping is needed (e.g., for delete operations returning bool).
    /// </summary>
    protected IActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        var apiResponse = new ApiResponse<T>
        {
            Success = result.Success,
            Message = result.Message,
            Data    = result.Data,
            Errors  = result.Errors
        };

        return result.StatusCode switch
        {
            200 => Ok(apiResponse),
            201 => StatusCode(StatusCodes.Status201Created, apiResponse),
            400 => BadRequest(apiResponse),
            404 => NotFound(apiResponse),
            500 => StatusCode(StatusCodes.Status500InternalServerError, apiResponse),
            _   => StatusCode(result.StatusCode, apiResponse)
        };
    }

    /// <summary>
    /// Converts a <see cref="ServiceResult{TBusiness}"/> into an <see cref="IActionResult"/> with
    /// the correct HTTP status code, mapping the business model to a response DTO using the provided mapper.
    /// Use this overload when the controller needs to convert BusinessModel → ResponseModel.
    /// </summary>
    protected IActionResult ToActionResult<TBusiness, TResponse>(
        ServiceResult<TBusiness> result,
        Func<TBusiness, TResponse> mapper)
    {
        if (!result.Success || result.Data == null)
        {
            var errorResponse = new ApiResponse<TResponse>
            {
                Success = result.Success,
                Message = result.Message,
                Data    = default,
                Errors  = result.Errors
            };

            return result.StatusCode switch
            {
                400 => BadRequest(errorResponse),
                404 => NotFound(errorResponse),
                500 => StatusCode(StatusCodes.Status500InternalServerError, errorResponse),
                _   => StatusCode(result.StatusCode, errorResponse)
            };
        }

        var mapped = mapper(result.Data);
        var apiResponse = new ApiResponse<TResponse>
        {
            Success = result.Success,
            Message = result.Message,
            Data    = mapped,
            Errors  = result.Errors
        };

        return result.StatusCode switch
        {
            200 => Ok(apiResponse),
            201 => StatusCode(StatusCodes.Status201Created, apiResponse),
            _   => StatusCode(result.StatusCode, apiResponse)
        };
    }

    /// <summary>
    /// Like <see cref="ToActionResult{T}"/> but wraps a paged result into a
    /// <see cref="PagedResponse{T}"/> body. Maps ServicePaginationMetadata to PaginationMetadata.
    /// </summary>
    protected IActionResult ToPagedActionResult(ServicePagedResult<object> result)
    {
        var pagination = new PaginationMetadata
        {
            Page       = result.Pagination.Page,
            PageSize   = result.Pagination.PageSize,
            TotalItems = result.Pagination.TotalItems,
            TotalPages = result.Pagination.TotalPages
        };

        var pagedResponse = new PagedResponse<object>
        {
            Success    = result.Success,
            Message    = result.Message,
            Data       = result.Data,
            Errors     = result.Errors,
            Pagination = pagination
        };

        return result.StatusCode switch
        {
            200 => Ok(pagedResponse),
            400 => BadRequest(pagedResponse),
            500 => StatusCode(StatusCodes.Status500InternalServerError, pagedResponse),
            _   => StatusCode(result.StatusCode, pagedResponse)
        };
    }
}
