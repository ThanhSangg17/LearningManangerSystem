using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;

    public SubjectService(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task<PagedResponse<object>> GetSubjectsAsync(ListQueryParameters query)
    {
        var queryable = await _subjectRepository.GetQueryableAsync();

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            queryable = queryable.Where(s =>
                s.SubjectCode.ToLower().Contains(search) ||
                s.SubjectName.ToLower().Contains(search));
        }

        // Expand
        var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(e => e.Trim().ToLower()).ToList();
        bool expandCourses = expandList.Contains("courses");

        if (expandCourses)
        {
            queryable = queryable
                .Include(s => s.Courses)
                .ThenInclude(c => c.Semester);
        }

        // Sort
        if (!string.IsNullOrWhiteSpace(query.Sort))
            queryable = ApplySorting(queryable, query.Sort);

        var totalItems = await queryable.CountAsync();

        var items = await queryable
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .ToListAsync();

        var responses = items.Select(s => MapToSubjectResponse(s, expandCourses)).ToList();

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

    public async Task<ApiResponse<SubjectResponse>> GetSubjectByIdAsync(int id)
    {
        var subject = await _subjectRepository.GetByIdAsync(id);
        if (subject == null)
            return ApiResponse<SubjectResponse>.ErrorResponse($"Subject with ID {id} not found.");

        return ApiResponse<SubjectResponse>.SuccessResponse(MapToSubjectResponse(subject, true));
    }

    public async Task<ApiResponse<SubjectResponse>> CreateSubjectAsync(CreateSubjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectCode))
            return ApiResponse<SubjectResponse>.ErrorResponse("SubjectCode is required.");

        if (string.IsNullOrWhiteSpace(request.SubjectName))
            return ApiResponse<SubjectResponse>.ErrorResponse("SubjectName is required.");

        if (request.Credit <= 0)
            return ApiResponse<SubjectResponse>.ErrorResponse("Credit must be greater than 0.");

        var existing = await _subjectRepository.GetByCodeAsync(request.SubjectCode);
        if (existing != null)
            return ApiResponse<SubjectResponse>.ErrorResponse($"SubjectCode '{request.SubjectCode}' already exists.");

        var subject = new Subject
        {
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            Credit = request.Credit
        };

        await _subjectRepository.AddAsync(subject);
        await _subjectRepository.SaveChangesAsync();

        var created = await _subjectRepository.GetByIdAsync(subject.SubjectId);
        return ApiResponse<SubjectResponse>.SuccessResponse(
            MapToSubjectResponse(created!, true), "Subject created successfully.");
    }

    public async Task<ApiResponse<SubjectResponse>> UpdateSubjectAsync(int id, UpdateSubjectRequest request)
    {
        var subject = await _subjectRepository.GetByIdAsync(id);
        if (subject == null)
            return ApiResponse<SubjectResponse>.ErrorResponse($"Subject with ID {id} not found.");

        if (string.IsNullOrWhiteSpace(request.SubjectCode))
            return ApiResponse<SubjectResponse>.ErrorResponse("SubjectCode is required.");

        if (string.IsNullOrWhiteSpace(request.SubjectName))
            return ApiResponse<SubjectResponse>.ErrorResponse("SubjectName is required.");

        if (request.Credit <= 0)
            return ApiResponse<SubjectResponse>.ErrorResponse("Credit must be greater than 0.");

        var existingCode = await _subjectRepository.GetByCodeAsync(request.SubjectCode);
        if (existingCode != null && existingCode.SubjectId != id)
            return ApiResponse<SubjectResponse>.ErrorResponse($"SubjectCode '{request.SubjectCode}' is already used by another subject.");

        subject.SubjectCode = request.SubjectCode;
        subject.SubjectName = request.SubjectName;
        subject.Credit = request.Credit;

        _subjectRepository.Update(subject);
        await _subjectRepository.SaveChangesAsync();

        var updated = await _subjectRepository.GetByIdAsync(id);
        return ApiResponse<SubjectResponse>.SuccessResponse(
            MapToSubjectResponse(updated!, true), "Subject updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteSubjectAsync(int id)
    {
        var subject = await _subjectRepository.GetByIdAsync(id);
        if (subject == null)
            return ApiResponse<bool>.ErrorResponse($"Subject with ID {id} not found.");

        try
        {
            _subjectRepository.Delete(subject);
            await _subjectRepository.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Subject deleted successfully.");
        }
        catch (Exception)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Cannot delete subject because it has associated courses.");
        }
    }

    // ---- Mapping ----

    private static SubjectResponse MapToSubjectResponse(Subject s, bool includeCourses)
    {
        return new SubjectResponse
        {
            SubjectId = s.SubjectId,
            SubjectCode = s.SubjectCode,
            SubjectName = s.SubjectName,
            Credit = s.Credit,
            Courses = includeCourses
                ? s.Courses.Select(c => new CourseSummaryResponse
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    SemesterId = c.SemesterId,
                    SubjectId = c.SubjectId,
                    Semester = c.Semester != null ? new SemesterSummaryResponse
                    {
                        SemesterId = c.Semester.SemesterId,
                        SemesterName = c.Semester.SemesterName,
                        StartDate = c.Semester.StartDate,
                        EndDate = c.Semester.EndDate
                    } : null,
                    Subject = null
                }).ToList()
                : null
        };
    }

    private static IQueryable<Subject> ApplySorting(IQueryable<Subject> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Subject>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field = trimmed.TrimStart('-').ToLower();

            IOrderedQueryable<Subject> next = (ordered == null)
                ? field switch
                {
                    "subjectid"   => descending ? queryable.OrderByDescending(s => s.SubjectId)   : queryable.OrderBy(s => s.SubjectId),
                    "subjectcode" => descending ? queryable.OrderByDescending(s => s.SubjectCode) : queryable.OrderBy(s => s.SubjectCode),
                    "subjectname" => descending ? queryable.OrderByDescending(s => s.SubjectName) : queryable.OrderBy(s => s.SubjectName),
                    "credit"      => descending ? queryable.OrderByDescending(s => s.Credit)      : queryable.OrderBy(s => s.Credit),
                    _             => queryable.OrderBy(s => s.SubjectId)
                }
                : field switch
                {
                    "subjectid"   => descending ? ordered.ThenByDescending(s => s.SubjectId)   : ordered.ThenBy(s => s.SubjectId),
                    "subjectcode" => descending ? ordered.ThenByDescending(s => s.SubjectCode) : ordered.ThenBy(s => s.SubjectCode),
                    "subjectname" => descending ? ordered.ThenByDescending(s => s.SubjectName) : ordered.ThenBy(s => s.SubjectName),
                    "credit"      => descending ? ordered.ThenByDescending(s => s.Credit)      : ordered.ThenBy(s => s.Credit),
                    _             => ordered.ThenBy(s => s.SubjectId)
                };

            ordered = next;
        }

        return ordered ?? queryable;
    }
}
