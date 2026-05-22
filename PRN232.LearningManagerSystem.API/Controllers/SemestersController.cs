using Microsoft.AspNetCore.Mvc;
using PRN232.LearningManagerSystem.API.Models.Common;
using PRN232.LearningManagerSystem.API.Models.Requests;
using PRN232.LearningManagerSystem.API.Models.Responses;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.API.Controllers;

[Route("api/semesters")]
public class SemestersController : BaseApiController
{
    private readonly ISemesterService _semesterService;

    public SemestersController(ISemesterService semesterService)
    {
        _semesterService = semesterService;
    }

    /// <summary>Get paginated list of semesters with optional search, sort, paging, field selection, and expansion.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSemesters([FromQuery] ListQueryParameters query)
    {
        var serviceQuery = new ServiceListQueryParameters
        {
            Search = query.Search,
            Sort   = query.Sort,
            Page   = query.Page,
            Size   = query.Size,
            Fields = query.Fields,
            Expand = query.Expand
        };

        var result = await _semesterService.GetSemestersAsync(serviceQuery);
        return ToPagedActionResult(result);
    }

    /// <summary>Get a semester by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SemesterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSemesterById(int id)
    {
        var result = await _semesterService.GetSemesterByIdAsync(id);
        return ToActionResult(result, MapSemesterBusinessToResponse);
    }

    /// <summary>Create a new semester.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SemesterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequest request)
    {
        // Map RequestModel → Business Input Model
        var businessInput = new SemesterCreateBusinessModel
        {
            SemesterName = request.SemesterName,
            StartDate    = request.StartDate,
            EndDate      = request.EndDate
        };

        var result = await _semesterService.CreateSemesterAsync(businessInput);

        if (!result.Success)
            return ToActionResult(result, MapSemesterBusinessToResponse);

        var response = MapSemesterBusinessToResponse(result.Data!);

        var apiResponse = new ApiResponse<SemesterResponse>
        {
            Success = true,
            Message = result.Message,
            Data    = response,
            Errors  = null
        };

        return CreatedAtAction(nameof(GetSemesterById), new { id = response.SemesterId }, apiResponse);
    }

    /// <summary>Update an existing semester.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SemesterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSemester(int id, [FromBody] UpdateSemesterRequest request)
    {
        var businessInput = new SemesterUpdateBusinessModel
        {
            SemesterName = request.SemesterName,
            StartDate    = request.StartDate,
            EndDate      = request.EndDate
        };

        var result = await _semesterService.UpdateSemesterAsync(id, businessInput);
        return ToActionResult(result, MapSemesterBusinessToResponse);
    }

    /// <summary>Delete a semester by ID.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSemester(int id)
    {
        var result = await _semesterService.DeleteSemesterAsync(id);
        return ToActionResult(result);
    }

    // ---- Mapping: BusinessModel → ResponseModel ----

    private static SemesterResponse MapSemesterBusinessToResponse(SemesterBusinessModel model)
    {
        return new SemesterResponse
        {
            SemesterId   = model.SemesterId,
            SemesterName = model.SemesterName,
            StartDate    = model.StartDate,
            EndDate      = model.EndDate,
            CourseCount  = model.CourseCount,
            IsCurrent    = model.IsCurrent,
            Courses = model.Courses?.Select(c => new CourseSummaryResponse
            {
                CourseId   = c.CourseId,
                CourseName = c.CourseName,
                SemesterId = c.SemesterId,
                SubjectId  = c.SubjectId,
                Semester   = null,
                Subject    = !string.IsNullOrEmpty(c.SubjectName)
                    ? new SubjectSummaryResponse
                    {
                        SubjectId   = c.SubjectId,
                        SubjectCode = c.SubjectCode,
                        SubjectName = c.SubjectName,
                        Credit      = c.Credit
                    }
                    : null
            }).ToList()
        };
    }
}
