using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository    _studentRepository;
    private readonly ICourseRepository     _courseRepository;

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
        _studentRepository    = studentRepository;
        _courseRepository     = courseRepository;
    }

    public async Task<ServicePagedResult<object>> GetEnrollmentsAsync(ServiceListQueryParameters query)
    {
        try
        {
            var queryable = await _enrollmentRepository.GetQueryableAsync();

            // Expand
            var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(e => e.Trim().ToLower()).ToList();

            bool expandStudent        = expandList.Contains("student");
            bool expandCourse         = expandList.Contains("course");
            bool expandCourseSubject  = expandList.Contains("course.subject");
            bool expandCourseSemester = expandList.Contains("course.semester");

            // Always include for search / mapping
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

            var businessModels = items
                .Select(e => MapEntityToBusiness(e, includeStudent: showStudent, includeCourse: showCourse))
                .ToList();

            var pagination = new ServicePaginationMetadata
            {
                Page       = query.Page,
                PageSize   = query.Size,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / query.Size)
            };

            if (!string.IsNullOrWhiteSpace(query.Fields))
            {
                var selected = businessModels.Select(r => FieldSelector.SelectFields(r, query.Fields)).ToList();
                return ServicePagedResult<object>.Ok((object)selected, pagination);
            }

            return ServicePagedResult<object>.Ok((object)businessModels, pagination);
        }
        catch (Exception ex)
        {
            return ServicePagedResult<object>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<EnrollmentBusinessModel>> GetEnrollmentByIdAsync(int id)
    {
        try
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null)
                return ServiceResult<EnrollmentBusinessModel>.NotFound($"Enrollment with ID {id} not found.");

            return ServiceResult<EnrollmentBusinessModel>.Ok(MapEntityToBusiness(enrollment, includeStudent: true, includeCourse: true));
        }
        catch (Exception ex)
        {
            return ServiceResult<EnrollmentBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<EnrollmentBusinessModel>> CreateEnrollmentAsync(EnrollmentCreateBusinessModel model)
    {
        try
        {
            if (!await _studentRepository.ExistsAsync(model.StudentId))
                return ServiceResult<EnrollmentBusinessModel>.BadRequest($"Student with ID {model.StudentId} does not exist.");

            if (!await _courseRepository.ExistsAsync(model.CourseId))
                return ServiceResult<EnrollmentBusinessModel>.BadRequest($"Course with ID {model.CourseId} does not exist.");

            if (!ValidStatuses.Contains(model.Status))
                return ServiceResult<EnrollmentBusinessModel>.BadRequest(
                    $"Status '{model.Status}' is invalid. Must be one of: Active, Completed, Dropped, Waiting.");

            var duplicate = await _enrollmentRepository.GetByStudentAndCourseAsync(model.StudentId, model.CourseId);
            if (duplicate != null)
                return ServiceResult<EnrollmentBusinessModel>.BadRequest(
                    $"Student {model.StudentId} is already enrolled in Course {model.CourseId}.");

            var enrollment = new Enrollment
            {
                StudentId  = model.StudentId,
                CourseId   = model.CourseId,
                EnrollDate = model.EnrollDate,
                Status     = model.Status
            };

            await _enrollmentRepository.AddAsync(enrollment);
            await _enrollmentRepository.SaveChangesAsync();

            var created = await _enrollmentRepository.GetByIdAsync(enrollment.EnrollmentId);
            return ServiceResult<EnrollmentBusinessModel>.Created(
                MapEntityToBusiness(created!, includeStudent: true, includeCourse: true), "Enrollment created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<EnrollmentBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<EnrollmentBusinessModel>> UpdateEnrollmentAsync(int id, EnrollmentUpdateBusinessModel model)
    {
        try
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null)
                return ServiceResult<EnrollmentBusinessModel>.NotFound($"Enrollment with ID {id} not found.");

            if (!await _studentRepository.ExistsAsync(model.StudentId))
                return ServiceResult<EnrollmentBusinessModel>.BadRequest($"Student with ID {model.StudentId} does not exist.");

            if (!await _courseRepository.ExistsAsync(model.CourseId))
                return ServiceResult<EnrollmentBusinessModel>.BadRequest($"Course with ID {model.CourseId} does not exist.");

            if (!ValidStatuses.Contains(model.Status))
                return ServiceResult<EnrollmentBusinessModel>.BadRequest(
                    $"Status '{model.Status}' is invalid. Must be one of: Active, Completed, Dropped, Waiting.");

            // Check duplicate only if student/course pair changed
            if (enrollment.StudentId != model.StudentId || enrollment.CourseId != model.CourseId)
            {
                var duplicate = await _enrollmentRepository.GetByStudentAndCourseAsync(model.StudentId, model.CourseId);
                if (duplicate != null)
                    return ServiceResult<EnrollmentBusinessModel>.BadRequest(
                        $"Student {model.StudentId} is already enrolled in Course {model.CourseId}.");
            }

            enrollment.StudentId  = model.StudentId;
            enrollment.CourseId   = model.CourseId;
            enrollment.EnrollDate = model.EnrollDate;
            enrollment.Status     = model.Status;

            _enrollmentRepository.Update(enrollment);
            await _enrollmentRepository.SaveChangesAsync();

            var updated = await _enrollmentRepository.GetByIdAsync(id);
            return ServiceResult<EnrollmentBusinessModel>.Ok(
                MapEntityToBusiness(updated!, includeStudent: true, includeCourse: true), "Enrollment updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<EnrollmentBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteEnrollmentAsync(int id)
    {
        try
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null)
                return ServiceResult<bool>.NotFound($"Enrollment with ID {id} not found.");

            _enrollmentRepository.Delete(enrollment);
            await _enrollmentRepository.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Enrollment deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    // ---- Mapping: Entity → BusinessModel ----

    private static EnrollmentBusinessModel MapEntityToBusiness(Enrollment e,
        bool includeStudent = true,
        bool includeCourse = true)
    {
        return new EnrollmentBusinessModel
        {
            EnrollmentId = e.EnrollmentId,
            StudentId    = e.StudentId,
            CourseId     = e.CourseId,
            EnrollDate   = e.EnrollDate,
            Status       = e.Status,
            StudentName  = includeStudent ? e.Student?.FullName   ?? string.Empty : string.Empty,
            StudentEmail = includeStudent ? e.Student?.Email       ?? string.Empty : string.Empty,
            CourseName   = includeCourse  ? e.Course?.CourseName   ?? string.Empty : string.Empty,
            SubjectCode  = includeCourse  ? e.Course?.Subject?.SubjectCode   ?? string.Empty : string.Empty,
            SemesterName = includeCourse  ? e.Course?.Semester?.SemesterName ?? string.Empty : string.Empty
        };
    }

    // ---- Sorting ----

    private static IQueryable<Enrollment> ApplySorting(IQueryable<Enrollment> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Enrollment>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed    = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field      = trimmed.TrimStart('-').ToLower();

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
