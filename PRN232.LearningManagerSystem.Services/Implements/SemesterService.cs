using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepository;

    public SemesterService(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<PagedResponse<object>> GetSemestersAsync(ListQueryParameters query)
    {
        var queryable = await _semesterRepository.GetQueryableAsync();

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            queryable = queryable.Where(s => s.SemesterName.ToLower().Contains(search));
        }

        // Expand
        var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(e => e.Trim().ToLower()).ToList();
        bool expandCourses = expandList.Contains("courses");

        if (expandCourses)
        {
            queryable = queryable
                .Include(s => s.Courses)
                .ThenInclude(c => c.Subject);
        }

        // Sort
        if (!string.IsNullOrWhiteSpace(query.Sort))
            queryable = ApplySorting(queryable, query.Sort);

        var totalItems = await queryable.CountAsync();

        var items = await queryable
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .ToListAsync();

        var responses = items.Select(s => MapToSemesterResponse(s, expandCourses)).ToList();

        var pagination = new PaginationMetadata
        {
            Page = query.Page,
            PageSize = query.Size,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling((double)totalItems / query.Size)
        };

        if (!string.IsNullOrWhiteSpace(query.Fields))
        {
            var selected = responses.Select(r => FieldSelector.SelectFields(r, query.Fields)).ToList();
            return PagedResponse<object>.SuccessResponse((object)selected, pagination);
        }

        return PagedResponse<object>.SuccessResponse((object)responses, pagination);
    }

    public async Task<ApiResponse<SemesterResponse>> GetSemesterByIdAsync(int id)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        if (semester == null)
            return ApiResponse<SemesterResponse>.ErrorResponse($"Semester with ID {id} not found.");

        return ApiResponse<SemesterResponse>.SuccessResponse(MapToSemesterResponse(semester, true));
    }

    public async Task<ApiResponse<SemesterResponse>> CreateSemesterAsync(CreateSemesterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SemesterName))
            return ApiResponse<SemesterResponse>.ErrorResponse("SemesterName is required.");

        if (request.EndDate <= request.StartDate)
            return ApiResponse<SemesterResponse>.ErrorResponse("EndDate must be greater than StartDate.");

        var semester = new Semester
        {
            SemesterName = request.SemesterName,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        await _semesterRepository.AddAsync(semester);
        await _semesterRepository.SaveChangesAsync();

        var created = await _semesterRepository.GetByIdAsync(semester.SemesterId);
        return ApiResponse<SemesterResponse>.SuccessResponse(
            MapToSemesterResponse(created!, true), "Semester created successfully.");
    }

    public async Task<ApiResponse<SemesterResponse>> UpdateSemesterAsync(int id, UpdateSemesterRequest request)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        if (semester == null)
            return ApiResponse<SemesterResponse>.ErrorResponse($"Semester with ID {id} not found.");

        if (string.IsNullOrWhiteSpace(request.SemesterName))
            return ApiResponse<SemesterResponse>.ErrorResponse("SemesterName is required.");

        if (request.EndDate <= request.StartDate)
            return ApiResponse<SemesterResponse>.ErrorResponse("EndDate must be greater than StartDate.");

        semester.SemesterName = request.SemesterName;
        semester.StartDate = request.StartDate;
        semester.EndDate = request.EndDate;

        _semesterRepository.Update(semester);
        await _semesterRepository.SaveChangesAsync();

        var updated = await _semesterRepository.GetByIdAsync(id);
        return ApiResponse<SemesterResponse>.SuccessResponse(
            MapToSemesterResponse(updated!, true), "Semester updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteSemesterAsync(int id)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        if (semester == null)
            return ApiResponse<bool>.ErrorResponse($"Semester with ID {id} not found.");

        try
        {
            _semesterRepository.Delete(semester);
            await _semesterRepository.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Semester deleted successfully.");
        }
        catch (Exception)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Cannot delete semester because it has associated courses.");
        }
    }

    // ---- Mapping ----

    private static SemesterResponse MapToSemesterResponse(Semester s, bool includeCourses)
    {
        return new SemesterResponse
        {
            SemesterId = s.SemesterId,
            SemesterName = s.SemesterName,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Courses = includeCourses
                ? s.Courses.Select(MapToCourseSummary).ToList()
                : null
        };
    }

    private static CourseSummaryResponse MapToCourseSummary(Course c)
    {
        return new CourseSummaryResponse
        {
            CourseId = c.CourseId,
            CourseName = c.CourseName,
            SemesterId = c.SemesterId,
            SubjectId = c.SubjectId,
            Semester = null,
            Subject = c.Subject != null ? new SubjectSummaryResponse
            {
                SubjectId = c.Subject.SubjectId,
                SubjectCode = c.Subject.SubjectCode,
                SubjectName = c.Subject.SubjectName,
                Credit = c.Subject.Credit
            } : null
        };
    }

    private static IQueryable<Semester> ApplySorting(IQueryable<Semester> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Semester>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field = trimmed.TrimStart('-').ToLower();

            IOrderedQueryable<Semester> next = (ordered == null)
                ? field switch
                {
                    "semesterid"   => descending ? queryable.OrderByDescending(s => s.SemesterId)   : queryable.OrderBy(s => s.SemesterId),
                    "semestername" => descending ? queryable.OrderByDescending(s => s.SemesterName) : queryable.OrderBy(s => s.SemesterName),
                    "startdate"    => descending ? queryable.OrderByDescending(s => s.StartDate)    : queryable.OrderBy(s => s.StartDate),
                    "enddate"      => descending ? queryable.OrderByDescending(s => s.EndDate)      : queryable.OrderBy(s => s.EndDate),
                    _              => queryable.OrderBy(s => s.SemesterId)
                }
                : field switch
                {
                    "semesterid"   => descending ? ordered.ThenByDescending(s => s.SemesterId)   : ordered.ThenBy(s => s.SemesterId),
                    "semestername" => descending ? ordered.ThenByDescending(s => s.SemesterName) : ordered.ThenBy(s => s.SemesterName),
                    "startdate"    => descending ? ordered.ThenByDescending(s => s.StartDate)    : ordered.ThenBy(s => s.StartDate),
                    "enddate"      => descending ? ordered.ThenByDescending(s => s.EndDate)      : ordered.ThenBy(s => s.EndDate),
                    _              => ordered.ThenBy(s => s.SemesterId)
                };

            ordered = next;
        }

        return ordered ?? queryable;
    }
}
