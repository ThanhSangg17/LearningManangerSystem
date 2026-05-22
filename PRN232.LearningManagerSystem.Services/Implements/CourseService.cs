using Microsoft.EntityFrameworkCore;
using System.Linq;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

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

    public async Task<ServicePagedResult<object>> GetCoursesAsync(ServiceListQueryParameters query)
    {
        try
        {
            var queryable = await _courseRepository.GetQueryableAsync();

            // Always include related entities for search / mapping
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

            var businessModels = items
                .Select(c => MapEntityToBusiness(c, includeSemester: true, includeSubject: true, includeEnrollments: expandEnrollments))
                .ToList();

            var pagination = new ServicePaginationMetadata
            {
                Page      = query.Page,
                PageSize  = query.Size,
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

    public async Task<ServiceResult<CourseBusinessModel>> GetCourseByIdAsync(int id)
    {
        try
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
                return ServiceResult<CourseBusinessModel>.NotFound($"Course with ID {id} not found.");

            return ServiceResult<CourseBusinessModel>.Ok(MapEntityToBusiness(course));
        }
        catch (Exception ex)
        {
            return ServiceResult<CourseBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<CourseBusinessModel>> CreateCourseAsync(CourseCreateBusinessModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.CourseName))
                return ServiceResult<CourseBusinessModel>.BadRequest("CourseName is required.");

            if (!await _semesterRepository.ExistsAsync(model.SemesterId))
                return ServiceResult<CourseBusinessModel>.BadRequest($"Semester with ID {model.SemesterId} does not exist.");

            if (!await _subjectRepository.ExistsAsync(model.SubjectId))
                return ServiceResult<CourseBusinessModel>.BadRequest($"Subject with ID {model.SubjectId} does not exist.");

            var course = new Course
            {
                CourseName = model.CourseName,
                SemesterId = model.SemesterId,
                SubjectId  = model.SubjectId
            };

            await _courseRepository.AddAsync(course);
            await _courseRepository.SaveChangesAsync();

            var created = await _courseRepository.GetByIdAsync(course.CourseId);
            return ServiceResult<CourseBusinessModel>.Created(MapEntityToBusiness(created!), "Course created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<CourseBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<CourseBusinessModel>> UpdateCourseAsync(int id, CourseUpdateBusinessModel model)
    {
        try
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
                return ServiceResult<CourseBusinessModel>.NotFound($"Course with ID {id} not found.");

            if (string.IsNullOrWhiteSpace(model.CourseName))
                return ServiceResult<CourseBusinessModel>.BadRequest("CourseName is required.");

            if (!await _semesterRepository.ExistsAsync(model.SemesterId))
                return ServiceResult<CourseBusinessModel>.BadRequest($"Semester with ID {model.SemesterId} does not exist.");

            if (!await _subjectRepository.ExistsAsync(model.SubjectId))
                return ServiceResult<CourseBusinessModel>.BadRequest($"Subject with ID {model.SubjectId} does not exist.");

            course.CourseName = model.CourseName;
            course.SemesterId = model.SemesterId;
            course.SubjectId  = model.SubjectId;

            _courseRepository.Update(course);
            await _courseRepository.SaveChangesAsync();

            var updated = await _courseRepository.GetByIdAsync(id);
            return ServiceResult<CourseBusinessModel>.Ok(MapEntityToBusiness(updated!), "Course updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<CourseBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteCourseAsync(int id)
    {
        try
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
                return ServiceResult<bool>.NotFound($"Course with ID {id} not found.");

            _courseRepository.Delete(course);
            await _courseRepository.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Course deleted successfully.");
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("REFERENCE") == true
                                || ex.Message.Contains("DELETE"))
        {
            return ServiceResult<bool>.BadRequest("Cannot delete course because it has associated enrollments.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    // ---- Mapping: Entity → BusinessModel ----

    private static CourseBusinessModel MapEntityToBusiness(Course course,
        bool includeSemester = true,
        bool includeSubject = true,
        bool includeEnrollments = false)
    {
        return new CourseBusinessModel
        {
            CourseId        = course.CourseId,
            CourseName      = course.CourseName ?? string.Empty,
            SemesterId      = course.SemesterId,
            SubjectId       = course.SubjectId,
            SemesterName    = includeSemester ? course.Semester?.SemesterName ?? string.Empty : string.Empty,
            SubjectCode     = includeSubject  ? course.Subject?.SubjectCode   ?? string.Empty : string.Empty,
            SubjectName     = includeSubject  ? course.Subject?.SubjectName   ?? string.Empty : string.Empty,
            Credit          = includeSubject  ? course.Subject?.Credit        ?? 0             : 0,
            EnrollmentCount = course.Enrollments?.Count ?? 0,
            DisplayName     = $"{course.Subject?.SubjectCode ?? string.Empty} - {course.CourseName}",
            Enrollments     = includeEnrollments
                ? course.Enrollments?.Select(MapEnrollmentEntityToBusiness).ToList()
                : null
        };
    }

    private static EnrollmentBusinessModel MapEnrollmentEntityToBusiness(Enrollment e)
    {
        return new EnrollmentBusinessModel
        {
            EnrollmentId = e.EnrollmentId,
            StudentId    = e.StudentId,
            CourseId     = e.CourseId,
            EnrollDate   = e.EnrollDate,
            Status       = e.Status,
            StudentName  = e.Student?.FullName ?? string.Empty,
            StudentEmail = e.Student?.Email    ?? string.Empty,
            CourseName   = e.Course?.CourseName ?? string.Empty,
            SubjectCode  = e.Course?.Subject?.SubjectCode   ?? string.Empty,
            SemesterName = e.Course?.Semester?.SemesterName ?? string.Empty
        };
    }

    // ---- Sorting ----

    private static IQueryable<Course> ApplySorting(IQueryable<Course> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Course>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed    = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field      = trimmed.TrimStart('-').ToLower();

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
