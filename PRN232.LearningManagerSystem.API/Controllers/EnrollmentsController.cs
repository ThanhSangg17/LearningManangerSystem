using Microsoft.AspNetCore.Mvc;
using PRN232.LearningManagerSystem.API.Models.Common;
using PRN232.LearningManagerSystem.API.Models.Requests;
using PRN232.LearningManagerSystem.API.Models.Responses;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.API.Controllers;

[Route("api/enrollments")]
public class EnrollmentsController : BaseApiController
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    /// <summary>Get paginated list of enrollments with optional search, sort, paging, field selection, and expansion.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEnrollments([FromQuery] ListQueryParameters query)
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

        var result = await _enrollmentService.GetEnrollmentsAsync(serviceQuery);
        return ToPagedActionResult(result);
    }

    /// <summary>Get an enrollment by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEnrollmentById(int id)
    {
        var result = await _enrollmentService.GetEnrollmentByIdAsync(id);
        return ToActionResult(result, MapEnrollmentBusinessToResponse);
    }

    /// <summary>Create a new enrollment.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEnrollment([FromBody] CreateEnrollmentRequest request)
    {
        // Map RequestModel → Business Input Model
        var businessInput = new EnrollmentCreateBusinessModel
        {
            StudentId  = request.StudentId,
            CourseId   = request.CourseId,
            EnrollDate = request.EnrollDate,
            Status     = request.Status
        };

        var result = await _enrollmentService.CreateEnrollmentAsync(businessInput);

        if (!result.Success)
            return ToActionResult(result, MapEnrollmentBusinessToResponse);

        var response = MapEnrollmentBusinessToResponse(result.Data!);

        var apiResponse = new ApiResponse<EnrollmentResponse>
        {
            Success = true,
            Message = result.Message,
            Data    = response,
            Errors  = null
        };

        return CreatedAtAction(nameof(GetEnrollmentById), new { id = response.EnrollmentId }, apiResponse);
    }

    /// <summary>Update an existing enrollment.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEnrollment(int id, [FromBody] UpdateEnrollmentRequest request)
    {
        var businessInput = new EnrollmentUpdateBusinessModel
        {
            StudentId  = request.StudentId,
            CourseId   = request.CourseId,
            EnrollDate = request.EnrollDate,
            Status     = request.Status
        };

        var result = await _enrollmentService.UpdateEnrollmentAsync(id, businessInput);
        return ToActionResult(result, MapEnrollmentBusinessToResponse);
    }

    /// <summary>Delete an enrollment by ID.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEnrollment(int id)
    {
        var result = await _enrollmentService.DeleteEnrollmentAsync(id);
        return ToActionResult(result);
    }

    // ---- Mapping: BusinessModel → ResponseModel ----

    private static EnrollmentResponse MapEnrollmentBusinessToResponse(EnrollmentBusinessModel model)
    {
        return new EnrollmentResponse
        {
            EnrollmentId = model.EnrollmentId,
            StudentId    = model.StudentId,
            CourseId     = model.CourseId,
            EnrollDate   = model.EnrollDate,
            Status       = model.Status,
            Student = !string.IsNullOrEmpty(model.StudentName)
                ? new StudentSummaryResponse
                {
                    StudentId = model.StudentId,
                    FullName  = model.StudentName,
                    Email     = model.StudentEmail
                }
                : null,
            Course = !string.IsNullOrEmpty(model.CourseName)
                ? new CourseSummaryResponse
                {
                    CourseId   = model.CourseId,
                    CourseName = model.CourseName,
                    SemesterId = 0,  // SemesterId not stored in EnrollmentBusinessModel directly
                    SubjectId  = 0,  // SubjectId not stored in EnrollmentBusinessModel directly
                    Semester   = !string.IsNullOrEmpty(model.SemesterName)
                        ? new SemesterSummaryResponse { SemesterName = model.SemesterName }
                        : null,
                    Subject = !string.IsNullOrEmpty(model.SubjectCode)
                        ? new SubjectSummaryResponse { SubjectCode = model.SubjectCode }
                        : null
                }
                : null
        };
    }
}
