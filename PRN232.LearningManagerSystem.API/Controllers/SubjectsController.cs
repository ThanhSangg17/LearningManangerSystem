using Microsoft.AspNetCore.Mvc;
using PRN232.LearningManagerSystem.API.Models.Common;
using PRN232.LearningManagerSystem.API.Models.Requests;
using PRN232.LearningManagerSystem.API.Models.Responses;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.API.Controllers;

[Route("api/subjects")]
public class SubjectsController : BaseApiController
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    /// <summary>Get paginated list of subjects with optional search, sort, paging, field selection, and expansion.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubjects([FromQuery] ListQueryParameters query)
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

        var result = await _subjectService.GetSubjectsAsync(serviceQuery);
        return ToPagedActionResult(result);
    }

    /// <summary>Get a subject by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SubjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubjectById(int id)
    {
        var result = await _subjectService.GetSubjectByIdAsync(id);
        return ToActionResult(result, MapSubjectBusinessToResponse);
    }

    /// <summary>Create a new subject.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SubjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request)
    {
        var businessInput = new SubjectCreateBusinessModel
        {
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            Credit      = request.Credit
        };

        var result = await _subjectService.CreateSubjectAsync(businessInput);

        if (!result.Success)
            return ToActionResult(result, MapSubjectBusinessToResponse);

        var response = MapSubjectBusinessToResponse(result.Data!);

        var apiResponse = new ApiResponse<SubjectResponse>
        {
            Success = true,
            Message = result.Message,
            Data    = response,
            Errors  = null
        };

        return CreatedAtAction(nameof(GetSubjectById), new { id = response.SubjectId }, apiResponse);
    }

    /// <summary>Update an existing subject.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SubjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectRequest request)
    {
        var businessInput = new SubjectUpdateBusinessModel
        {
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            Credit      = request.Credit
        };

        var result = await _subjectService.UpdateSubjectAsync(id, businessInput);
        return ToActionResult(result, MapSubjectBusinessToResponse);
    }

    /// <summary>Delete a subject by ID.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var result = await _subjectService.DeleteSubjectAsync(id);
        return ToActionResult(result);
    }

    // ---- Mapping: BusinessModel → ResponseModel ----

    private static SubjectResponse MapSubjectBusinessToResponse(SubjectBusinessModel model)
    {
        return new SubjectResponse
        {
            SubjectId   = model.SubjectId,
            SubjectCode = model.SubjectCode,
            SubjectName = model.SubjectName,
            Credit      = model.Credit,
            CourseCount = model.CourseCount,
            Courses     = model.Courses?.Select(c => new CourseSummaryResponse
            {
                CourseId   = c.CourseId,
                CourseName = c.CourseName,
                SemesterId = c.SemesterId,
                SubjectId  = c.SubjectId,
                Semester   = !string.IsNullOrEmpty(c.SemesterName)
                    ? new SemesterSummaryResponse
                    {
                        SemesterId   = c.SemesterId,
                        SemesterName = c.SemesterName
                    }
                    : null,
                Subject = null
            }).ToList()
        };
    }
}
