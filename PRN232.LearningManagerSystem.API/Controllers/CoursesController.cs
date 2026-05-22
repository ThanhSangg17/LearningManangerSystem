using Microsoft.AspNetCore.Mvc;
using PRN232.LearningManagerSystem.API.Models.Common;
using PRN232.LearningManagerSystem.API.Models.Requests;
using PRN232.LearningManagerSystem.API.Models.Responses;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.API.Controllers;

[Route("api/courses")]
public class CoursesController : BaseApiController
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    /// <summary>Get paginated list of courses with optional search, sort, paging, field selection, and expansion.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCourses([FromQuery] ListQueryParameters query)
    {
        // Map API-layer query params to Service-layer query params
        var serviceQuery = new ServiceListQueryParameters
        {
            Search = query.Search,
            Sort   = query.Sort,
            Page   = query.Page,
            Size   = query.Size,
            Fields = query.Fields,
            Expand = query.Expand
        };

        var result = await _courseService.GetCoursesAsync(serviceQuery);
        return ToPagedActionResult(result);
    }

    /// <summary>Get a course by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCourseById(int id)
    {
        var result = await _courseService.GetCourseByIdAsync(id);
        return ToActionResult(result, MapCourseBusinessToResponse);
    }

    /// <summary>Create a new course.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        // Map RequestModel → Business Input Model
        var businessInput = new CourseCreateBusinessModel
        {
            CourseName = request.CourseName,
            SemesterId = request.SemesterId,
            SubjectId  = request.SubjectId
        };

        var result = await _courseService.CreateCourseAsync(businessInput);

        if (!result.Success)
            return ToActionResult(result, MapCourseBusinessToResponse);

        var response = MapCourseBusinessToResponse(result.Data!);

        var apiResponse = new ApiResponse<CourseResponse>
        {
            Success = true,
            Message = result.Message,
            Data    = response,
            Errors  = null
        };

        return CreatedAtAction(nameof(GetCourseById), new { id = response.CourseId }, apiResponse);
    }

    /// <summary>Update an existing course.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
    {
        // Map RequestModel → Business Input Model
        var businessInput = new CourseUpdateBusinessModel
        {
            CourseName = request.CourseName,
            SemesterId = request.SemesterId,
            SubjectId  = request.SubjectId
        };

        var result = await _courseService.UpdateCourseAsync(id, businessInput);
        return ToActionResult(result, MapCourseBusinessToResponse);
    }

    /// <summary>Delete a course by ID.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var result = await _courseService.DeleteCourseAsync(id);
        return ToActionResult(result);
    }

    // ---- Mapping: BusinessModel → ResponseModel ----

    private static CourseResponse MapCourseBusinessToResponse(CourseBusinessModel model)
    {
        return new CourseResponse
        {
            CourseId        = model.CourseId,
            CourseName      = model.CourseName,
            SemesterId      = model.SemesterId,
            SubjectId       = model.SubjectId,
            DisplayName     = model.DisplayName,
            EnrollmentCount = model.EnrollmentCount,
            Semester = !string.IsNullOrEmpty(model.SemesterName)
                ? new SemesterSummaryResponse
                {
                    SemesterId   = model.SemesterId,
                    SemesterName = model.SemesterName
                }
                : null,
            Subject = !string.IsNullOrEmpty(model.SubjectName)
                ? new SubjectSummaryResponse
                {
                    SubjectId   = model.SubjectId,
                    SubjectCode = model.SubjectCode,
                    SubjectName = model.SubjectName,
                    Credit      = model.Credit
                }
                : null,
            Enrollments = model.Enrollments?.Select(e => new EnrollmentSummaryResponse
            {
                EnrollmentId = e.EnrollmentId,
                StudentId    = e.StudentId,
                CourseId     = e.CourseId,
                EnrollDate   = e.EnrollDate,
                Status       = e.Status
            }).ToList()
        };
    }
}
