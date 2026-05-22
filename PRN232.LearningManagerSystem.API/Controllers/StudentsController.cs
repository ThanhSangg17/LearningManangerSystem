using Microsoft.AspNetCore.Mvc;
using PRN232.LearningManagerSystem.API.Models.Common;
using PRN232.LearningManagerSystem.API.Models.Requests;
using PRN232.LearningManagerSystem.API.Models.Responses;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;
using System.Linq;

namespace PRN232.LearningManagerSystem.API.Controllers;

[Route("api/students")]
public class StudentsController : BaseApiController
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    /// <summary>Get paginated list of students with optional search, sort, paging, field selection, and expansion.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStudents([FromQuery] ListQueryParameters query)
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

        var result = await _studentService.GetStudentsAsync(serviceQuery);
        return ToPagedActionResult(result);
    }

    /// <summary>Get a student by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStudentById(int id)
    {
        var result = await _studentService.GetStudentByIdAsync(id);
        return ToActionResult(result, MapStudentBusinessToResponse);
    }

    /// <summary>Create a new student.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StudentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
    {
        // Map RequestModel → Business Input Model
        var businessInput = new StudentCreateBusinessModel
        {
            FullName    = request.FullName,
            Email       = request.Email,
            DateOfBirth = request.DateOfBirth
        };

        var result = await _studentService.CreateStudentAsync(businessInput);

        if (!result.Success)
            return ToActionResult(result, MapStudentBusinessToResponse);

        var response = MapStudentBusinessToResponse(result.Data!);

        var apiResponse = new ApiResponse<StudentResponse>
        {
            Success = true,
            Message = result.Message,
            Data    = response,
            Errors  = null
        };

        return CreatedAtAction(nameof(GetStudentById), new { id = response.StudentId }, apiResponse);
    }

    /// <summary>Update an existing student.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentRequest request)
    {
        var businessInput = new StudentUpdateBusinessModel
        {
            FullName    = request.FullName,
            Email       = request.Email,
            DateOfBirth = request.DateOfBirth
        };

        var result = await _studentService.UpdateStudentAsync(id, businessInput);
        return ToActionResult(result, MapStudentBusinessToResponse);
    }

    /// <summary>Delete a student by ID.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var result = await _studentService.DeleteStudentAsync(id);
        return ToActionResult(result);
    }

    // ---- Mapping: BusinessModel → ResponseModel ----

    private static StudentResponse MapStudentBusinessToResponse(StudentBusinessModel model)
    {
        return new StudentResponse
        {
            StudentId       = model.StudentId,
            FullName        = model.FullName,
            Email           = model.Email,
            DateOfBirth     = model.DateOfBirth,
            Age             = model.Age,
            EnrollmentCount = model.EnrollmentCount,
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
