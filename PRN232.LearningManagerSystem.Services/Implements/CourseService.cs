using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly ISubjectRepository _subjectRepository;

    public CourseService(
        ICourseRepository courseRepository,
        ISemesterRepository semesterRepository,
        ISubjectRepository subjectRepository)
    {
        _courseRepository = courseRepository;
        _semesterRepository = semesterRepository;
        _subjectRepository = subjectRepository;
    }

    public async Task<PagedResponse<object>> GetCoursesAsync(ListQueryParameters query)
    {
        var queryable = await _courseRepository.GetQueryableAsync();

        // Always include for search
        queryable = queryable
            .Include(c => c.Semester)
            .Include(c => c.Subject);

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            queryable = queryable.Where(c =>
                c.CourseName.ToLower().Contains(search) ||
                c.Subject.SubjectCode.ToLower().Contains(search) ||
                c.Subject.SubjectName.ToLower().Contains(search) ||
                c.Semester.SemesterName.ToLower().Contains(search));
        }

        // Expand
        var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(e => e.Trim().ToLower()).ToList();
        bool expandEnrollments = expandList.Contains("enrollments");

        if (expandEnrollments)
        {
            queryable = queryable
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student);
        }

        // Sort
        if (!string.IsNullOrWhiteSpace(query.Sort))
            queryable = ApplySorting(queryable, query.Sort);

        var totalItems = await queryable.CountAsync();

        var items = await queryable
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .ToListAsync();

        bool includeSemester = expandList.Contains("semester") || expandList.Contains("all");
        bool includeSubject  = expandList.Contains("subject")  || expandList.Contains("all");

        var responses = items.Select(c => MapToCourseResponse(c, true, true, expandEnrollments)).ToList();

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

    public async Task<ApiResponse<CourseResponse>> GetCourseByIdAsync(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null)
            return ApiResponse<CourseResponse>.ErrorResponse($"Course with ID {id} not found.");

        return ApiResponse<CourseResponse>.SuccessResponse(
            MapToCourseResponse(course, true, true, true));
    }

    public async Task<ApiResponse<CourseResponse>> CreateCourseAsync(CreateCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CourseName))
            return ApiResponse<CourseResponse>.ErrorResponse("CourseName is required.");

        if (!await _semesterRepository.ExistsAsync(request.SemesterId))
            return ApiResponse<CourseResponse>.ErrorResponse($"Semester with ID {request.SemesterId} does not exist.");

        if (!await _subjectRepository.ExistsAsync(request.SubjectId))
            return ApiResponse<CourseResponse>.ErrorResponse($"Subject with ID {request.SubjectId} does not exist.");

        var course = new Course
        {
            CourseName = request.CourseName,
            SemesterId = request.SemesterId,
            SubjectId = request.SubjectId
        };

        await _courseRepository.AddAsync(course);
        await _courseRepository.SaveChangesAsync();

        var created = await _courseRepository.GetByIdAsync(course.CourseId);
        return ApiResponse<CourseResponse>.SuccessResponse(
            MapToCourseResponse(created!, true, true, true), "Course created successfully.");
    }

    public async Task<ApiResponse<CourseResponse>> UpdateCourseAsync(int id, UpdateCourseRequest request)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null)
            return ApiResponse<CourseResponse>.ErrorResponse($"Course with ID {id} not found.");

        if (string.IsNullOrWhiteSpace(request.CourseName))
            return ApiResponse<CourseResponse>.ErrorResponse("CourseName is required.");

        if (!await _semesterRepository.ExistsAsync(request.SemesterId))
            return ApiResponse<CourseResponse>.ErrorResponse($"Semester with ID {request.SemesterId} does not exist.");

        if (!await _subjectRepository.ExistsAsync(request.SubjectId))
            return ApiResponse<CourseResponse>.ErrorResponse($"Subject with ID {request.SubjectId} does not exist.");

        course.CourseName = request.CourseName;
        course.SemesterId = request.SemesterId;
        course.SubjectId = request.SubjectId;

        _courseRepository.Update(course);
        await _courseRepository.SaveChangesAsync();

        var updated = await _courseRepository.GetByIdAsync(id);
        return ApiResponse<CourseResponse>.SuccessResponse(
            MapToCourseResponse(updated!, true, true, true), "Course updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteCourseAsync(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null)
            return ApiResponse<bool>.ErrorResponse($"Course with ID {id} not found.");

        try
        {
            _courseRepository.Delete(course);
            await _courseRepository.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Course deleted successfully.");
        }
        catch (Exception)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Cannot delete course because it has associated enrollments.");
        }
    }

    // ---- Mapping ----

    private static CourseResponse MapToCourseResponse(Course c, bool includeSemester, bool includeSubject, bool includeEnrollments)
    {
        return new CourseResponse
        {
            CourseId = c.CourseId,
            CourseName = c.CourseName,
            SemesterId = c.SemesterId,
            SubjectId = c.SubjectId,
            Semester = includeSemester && c.Semester != null ? new SemesterSummaryResponse
            {
                SemesterId = c.Semester.SemesterId,
                SemesterName = c.Semester.SemesterName,
                StartDate = c.Semester.StartDate,
                EndDate = c.Semester.EndDate
            } : null,
            Subject = includeSubject && c.Subject != null ? new SubjectSummaryResponse
            {
                SubjectId = c.Subject.SubjectId,
                SubjectCode = c.Subject.SubjectCode,
                SubjectName = c.Subject.SubjectName,
                Credit = c.Subject.Credit
            } : null,
            Enrollments = includeEnrollments
                ? c.Enrollments.Select(e => new EnrollmentSummaryResponse
                {
                    EnrollmentId = e.EnrollmentId,
                    StudentId = e.StudentId,
                    CourseId = e.CourseId,
                    EnrollDate = e.EnrollDate,
                    Status = e.Status
                }).ToList()
                : null
        };
    }

    private static IQueryable<Course> ApplySorting(IQueryable<Course> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Course>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field = trimmed.TrimStart('-').ToLower();

            IOrderedQueryable<Course> next = (ordered == null)
                ? field switch
                {
                    "courseid"   => descending ? queryable.OrderByDescending(c => c.CourseId)   : queryable.OrderBy(c => c.CourseId),
                    "coursename" => descending ? queryable.OrderByDescending(c => c.CourseName) : queryable.OrderBy(c => c.CourseName),
                    "semesterid" => descending ? queryable.OrderByDescending(c => c.SemesterId) : queryable.OrderBy(c => c.SemesterId),
                    "subjectid"  => descending ? queryable.OrderByDescending(c => c.SubjectId)  : queryable.OrderBy(c => c.SubjectId),
                    _            => queryable.OrderBy(c => c.CourseId)
                }
                : field switch
                {
                    "courseid"   => descending ? ordered.ThenByDescending(c => c.CourseId)   : ordered.ThenBy(c => c.CourseId),
                    "coursename" => descending ? ordered.ThenByDescending(c => c.CourseName) : ordered.ThenBy(c => c.CourseName),
                    "semesterid" => descending ? ordered.ThenByDescending(c => c.SemesterId) : ordered.ThenBy(c => c.SemesterId),
                    "subjectid"  => descending ? ordered.ThenByDescending(c => c.SubjectId)  : ordered.ThenBy(c => c.SubjectId),
                    _            => ordered.ThenBy(c => c.CourseId)
                };

            ordered = next;
        }

        return ordered ?? queryable;
    }
}
