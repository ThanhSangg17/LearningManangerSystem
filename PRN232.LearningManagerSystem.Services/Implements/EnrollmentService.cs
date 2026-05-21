using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active", "Completed", "Dropped", "Waiting"
    };

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        ICourseRepository courseRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
    }

    public async Task<PagedResponse<object>> GetEnrollmentsAsync(ListQueryParameters query)
    {
        var queryable = await _enrollmentRepository.GetQueryableAsync();

        // Expand
        var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(e => e.Trim().ToLower()).ToList();

        bool expandStudent      = expandList.Contains("student");
        bool expandCourse       = expandList.Contains("course");
        bool expandCourseSubject  = expandList.Contains("course.subject");
        bool expandCourseSemester = expandList.Contains("course.semester");

        // Always include for search
        queryable = queryable
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.Subject)
            .Include(e => e.Course)
                .ThenInclude(c => c.Semester);

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            queryable = queryable.Where(e =>
                e.Status.ToLower().Contains(search) ||
                e.Student.FullName.ToLower().Contains(search) ||
                e.Student.Email.ToLower().Contains(search) ||
                e.Course.CourseName.ToLower().Contains(search));
        }

        // Sort
        if (!string.IsNullOrWhiteSpace(query.Sort))
            queryable = ApplySorting(queryable, query.Sort);

        var totalItems = await queryable.CountAsync();

        var items = await queryable
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .ToListAsync();

        bool showStudent = expandStudent;
        bool showCourse  = expandCourse || expandCourseSubject || expandCourseSemester;

        var responses = items.Select(e => MapToEnrollmentResponse(e, showStudent, showCourse)).ToList();

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

    public async Task<ApiResponse<EnrollmentResponse>> GetEnrollmentByIdAsync(int id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id);
        if (enrollment == null)
            return ApiResponse<EnrollmentResponse>.ErrorResponse($"Enrollment with ID {id} not found.");

        return ApiResponse<EnrollmentResponse>.SuccessResponse(
            MapToEnrollmentResponse(enrollment, true, true));
    }

    public async Task<ApiResponse<EnrollmentResponse>> CreateEnrollmentAsync(CreateEnrollmentRequest request)
    {
        if (!await _studentRepository.ExistsAsync(request.StudentId))
            return ApiResponse<EnrollmentResponse>.ErrorResponse($"Student with ID {request.StudentId} does not exist.");

        if (!await _courseRepository.ExistsAsync(request.CourseId))
            return ApiResponse<EnrollmentResponse>.ErrorResponse($"Course with ID {request.CourseId} does not exist.");

        if (!ValidStatuses.Contains(request.Status))
            return ApiResponse<EnrollmentResponse>.ErrorResponse(
                $"Status '{request.Status}' is invalid. Must be one of: Active, Completed, Dropped, Waiting.");

        var duplicate = await _enrollmentRepository.GetByStudentAndCourseAsync(request.StudentId, request.CourseId);
        if (duplicate != null)
            return ApiResponse<EnrollmentResponse>.ErrorResponse(
                $"Student {request.StudentId} is already enrolled in Course {request.CourseId}.");

        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            EnrollDate = request.EnrollDate,
            Status = request.Status
        };

        await _enrollmentRepository.AddAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var created = await _enrollmentRepository.GetByIdAsync(enrollment.EnrollmentId);
        return ApiResponse<EnrollmentResponse>.SuccessResponse(
            MapToEnrollmentResponse(created!, true, true), "Enrollment created successfully.");
    }

    public async Task<ApiResponse<EnrollmentResponse>> UpdateEnrollmentAsync(int id, UpdateEnrollmentRequest request)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id);
        if (enrollment == null)
            return ApiResponse<EnrollmentResponse>.ErrorResponse($"Enrollment with ID {id} not found.");

        if (!await _studentRepository.ExistsAsync(request.StudentId))
            return ApiResponse<EnrollmentResponse>.ErrorResponse($"Student with ID {request.StudentId} does not exist.");

        if (!await _courseRepository.ExistsAsync(request.CourseId))
            return ApiResponse<EnrollmentResponse>.ErrorResponse($"Course with ID {request.CourseId} does not exist.");

        if (!ValidStatuses.Contains(request.Status))
            return ApiResponse<EnrollmentResponse>.ErrorResponse(
                $"Status '{request.Status}' is invalid. Must be one of: Active, Completed, Dropped, Waiting.");

        // Check duplicate only if student/course pair changed
        if (enrollment.StudentId != request.StudentId || enrollment.CourseId != request.CourseId)
        {
            var duplicate = await _enrollmentRepository.GetByStudentAndCourseAsync(request.StudentId, request.CourseId);
            if (duplicate != null)
                return ApiResponse<EnrollmentResponse>.ErrorResponse(
                    $"Student {request.StudentId} is already enrolled in Course {request.CourseId}.");
        }

        enrollment.StudentId = request.StudentId;
        enrollment.CourseId = request.CourseId;
        enrollment.EnrollDate = request.EnrollDate;
        enrollment.Status = request.Status;

        _enrollmentRepository.Update(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var updated = await _enrollmentRepository.GetByIdAsync(id);
        return ApiResponse<EnrollmentResponse>.SuccessResponse(
            MapToEnrollmentResponse(updated!, true, true), "Enrollment updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteEnrollmentAsync(int id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id);
        if (enrollment == null)
            return ApiResponse<bool>.ErrorResponse($"Enrollment with ID {id} not found.");

        try
        {
            _enrollmentRepository.Delete(enrollment);
            await _enrollmentRepository.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Enrollment deleted successfully.");
        }
        catch (Exception)
        {
            return ApiResponse<bool>.ErrorResponse("Cannot delete enrollment.");
        }
    }

    // ---- Mapping ----

    private static EnrollmentResponse MapToEnrollmentResponse(Enrollment e, bool includeStudent, bool includeCourse)
    {
        return new EnrollmentResponse
        {
            EnrollmentId = e.EnrollmentId,
            StudentId = e.StudentId,
            CourseId = e.CourseId,
            EnrollDate = e.EnrollDate,
            Status = e.Status,
            Student = includeStudent && e.Student != null ? new StudentSummaryResponse
            {
                StudentId = e.Student.StudentId,
                FullName = e.Student.FullName,
                Email = e.Student.Email
            } : null,
            Course = includeCourse && e.Course != null ? new CourseSummaryResponse
            {
                CourseId = e.Course.CourseId,
                CourseName = e.Course.CourseName,
                SemesterId = e.Course.SemesterId,
                SubjectId = e.Course.SubjectId,
                Semester = e.Course.Semester != null ? new SemesterSummaryResponse
                {
                    SemesterId = e.Course.Semester.SemesterId,
                    SemesterName = e.Course.Semester.SemesterName,
                    StartDate = e.Course.Semester.StartDate,
                    EndDate = e.Course.Semester.EndDate
                } : null,
                Subject = e.Course.Subject != null ? new SubjectSummaryResponse
                {
                    SubjectId = e.Course.Subject.SubjectId,
                    SubjectCode = e.Course.Subject.SubjectCode,
                    SubjectName = e.Course.Subject.SubjectName,
                    Credit = e.Course.Subject.Credit
                } : null
            } : null
        };
    }

    private static IQueryable<Enrollment> ApplySorting(IQueryable<Enrollment> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Enrollment>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field = trimmed.TrimStart('-').ToLower();

            IOrderedQueryable<Enrollment> next = (ordered == null)
                ? field switch
                {
                    "enrollmentid" => descending ? queryable.OrderByDescending(e => e.EnrollmentId) : queryable.OrderBy(e => e.EnrollmentId),
                    "studentid"    => descending ? queryable.OrderByDescending(e => e.StudentId)    : queryable.OrderBy(e => e.StudentId),
                    "courseid"     => descending ? queryable.OrderByDescending(e => e.CourseId)     : queryable.OrderBy(e => e.CourseId),
                    "enrolldate"   => descending ? queryable.OrderByDescending(e => e.EnrollDate)   : queryable.OrderBy(e => e.EnrollDate),
                    "status"       => descending ? queryable.OrderByDescending(e => e.Status)       : queryable.OrderBy(e => e.Status),
                    _              => queryable.OrderBy(e => e.EnrollmentId)
                }
                : field switch
                {
                    "enrollmentid" => descending ? ordered.ThenByDescending(e => e.EnrollmentId) : ordered.ThenBy(e => e.EnrollmentId),
                    "studentid"    => descending ? ordered.ThenByDescending(e => e.StudentId)    : ordered.ThenBy(e => e.StudentId),
                    "courseid"     => descending ? ordered.ThenByDescending(e => e.CourseId)     : ordered.ThenBy(e => e.CourseId),
                    "enrolldate"   => descending ? ordered.ThenByDescending(e => e.EnrollDate)   : ordered.ThenBy(e => e.EnrollDate),
                    "status"       => descending ? ordered.ThenByDescending(e => e.Status)       : ordered.ThenBy(e => e.Status),
                    _              => ordered.ThenBy(e => e.EnrollmentId)
                };

            ordered = next;
        }

        return ordered ?? queryable;
    }
}
